using System.Collections.Generic;
using System.Diagnostics;
using Game.Core;
using Game.World;
using UnityEngine;
using UnityEngine.Rendering;
using Debug = UnityEngine.Debug;

namespace Game.Client
{
    /// <summary>
    /// Hiển thị world M1: cả map (bước 3) hoặc một cột chunk (bước 2). Bấm Play trong scene TerrainView, hoặc gọi Build() từ editor.
    /// Bước 4: đào/đặt qua Dig/Place → chunk bẩn dựng lại mesh + bake collider trong LateUpdate của cùng khung hình.
    /// Bước 7b (brief 0010): source = MapsV3 + streaming → chỉ cột chunk trong loadRadius quanh camera có mesh + collider; gỡ khi > unloadRadius.
    /// </summary>
    public class TerrainChunkView : MonoBehaviour
    {
        public bool allChunks = true;
        public int chunkX = 1;
        public int chunkZ = 1;
        public UnityEngine.Material material;
        /// <summary>Material mặt nước theo ô (submesh 1, 0012) — nên dùng cùng material với mặt biển nền để liền màu.</summary>
        public UnityEngine.Material waterMaterial;
        /// <summary>Nhịp nước lan (design/decisions/0013): số lần WorldEditor.StepWater mỗi giây.</summary>
        public float waterStepsPerSecond = 8f;
        float waterTimer;
        public Color color = Color.white;
        /// <summary>Nguồn world (brief 0010). Mặc định đảo v2 — SimView và test cũ không đổi.</summary>
        public WorldSource source = WorldSource.IslandV2;
        /// <summary>Chỉ dựng cột chunk quanh streamTarget (world-tech bước 7b). Tắt → dựng như cũ (allChunks/chunkX/chunkZ).</summary>
        public bool streaming;
        /// <summary>Tâm nạp dần; null → Camera.main.</summary>
        public Transform streamTarget;
        public int loadRadius = 128;
        public int unloadRadius = 160;
        public int maxColumnsPerFrame = 2;

        sealed class ChunkSlot
        {
            public Mesh Mesh;
            /// <summary>Mặt khối vuông chưa bo (decision 0011) — bake cho MeshCollider.</summary>
            public Mesh ColliderMesh;
            public MeshCollider Collider;
            public int TriangleCount;
            public int WaterTriangleCount;
            public GameObject Go;
        }

        const string ChunksName = "Chunks";

        ChunkSlot[] slots;
        Transform chunksRoot;
        readonly List<int> dirty = new List<int>();
        readonly Stack<ChunkSlot> pool = new Stack<ChunkSlot>();
        readonly List<int> toLoad = new List<int>();
        readonly List<int> toUnload = new List<int>();
        ChunkStreamPlan plan;
        UnityEngine.Material slotMat, slotWater;

        public WorldData World { get; private set; }
        public MaterialInventory Inventory { get; private set; }
        public WorldEditor Editor { get; private set; }
        /// <summary>Bảng map của world v3; null với v2.</summary>
        public MapTable Maps { get; private set; }
        public int GeneratorVersion { get; private set; }
        /// <summary>Thời gian sinh world (ms).</summary>
        public double GenerateMs { get; private set; }
        /// <summary>Thời gian dựng lần đầu (ms).</summary>
        public double InitialLoadMs { get; private set; }
        public int LoadedColumnCount => plan != null ? plan.LoadedCount : 0;
        public int LoadedChunkCount
        {
            get
            {
                int n = 0;
                if (slots != null)
                    foreach (var s in slots)
                        if (s != null) n++;
                return n;
            }
        }
        /// <summary>Số cột nạp trong khung gần nhất.</summary>
        public int LastLoadedColumns { get; private set; }

        /// <summary>Số cột có khoảng cách ≤ radius tới camera mà chưa nạp; 0 nếu không streaming.</summary>
        public int MissingColumnsWithin(int radius)
        {
            if (!streaming || plan == null)
                return 0;
            var center = StreamCenter();
            if (center == null)
                return 0;
            var cell = CellAt(center.position);
            return plan.MissingWithin(cell.x, cell.z, radius);
        }

        Transform StreamCenter() => streamTarget != null ? streamTarget : (Camera.main != null ? Camera.main.transform : null);

        /// <summary>Khung hình (Time.frameCount) của lần dựng lại gần nhất; -1 nếu chưa có.</summary>
        public int LastRebuildFrame { get; private set; } = -1;
        public int LastRebuildChunks { get; private set; }
        public double LastMeshMs { get; private set; }
        public double LastWriteMs { get; private set; }
        public double LastBakeMs { get; private set; }

        void Start()
        {
            Build();
        }

        public void Build()
        {
            // Chỉ xoá chunk cũ của view này — con khác (vd khung chọn ô của TerrainEditInput) phải giữ nguyên.
            var oldRoot = transform.Find(ChunksName);
            if (oldRoot != null)
                DestroyImmediate(oldRoot.gameObject);
            chunksRoot = new GameObject(ChunksName).transform;
            chunksRoot.SetParent(transform, false);

            var gen = Stopwatch.StartNew();
            int waterLevel;
            if (source == WorldSource.MapsV3)
            {
                var p = ConfigLoader.LoadWorldMapParams();
                World = WorldMapGenerator.Generate(p, out var maps);
                Maps = maps;
                GeneratorVersion = p.generatorVersion;
                waterLevel = p.waterLevel;
            }
            else
            {
                var p = ConfigLoader.LoadTerrainParams();
                World = TerrainGenerator.Generate(p);
                Maps = null;
                GeneratorVersion = p.generatorVersion;
                waterLevel = p.waterLevel;
            }
            GenerateMs = gen.Elapsed.TotalMilliseconds;
            Inventory = new MaterialInventory();
            Editor = new WorldEditor(World, Inventory, BlockMesher.CellMargin, waterLevel);
            Editor.CellChanged += OnCellChanged;
            dirty.Clear();

            // Mặc định: shader Game/Terrain (màu đỉnh theo vật liệu, nhám — decision 0011); color là sắc nhân thêm.
            var mat = material != null ? material : CreateTerrainMaterial();
            mat.SetColor("_BaseColor", color);
            var water = waterMaterial != null ? waterMaterial : CreateWaterMaterial();
            slotMat = mat;
            slotWater = water;
            pool.Clear();

            var s = World.Size;
            if (streaming)
            {
                plan = new ChunkStreamPlan(s.ChunksX, s.ChunksZ, loadRadius, unloadRadius);
                slots = new ChunkSlot[s.ChunkCount];
                var center = StreamCenter();
                if (center == null)
                {
                    Debug.LogError("TERRAINVIEW streaming: không có streamTarget và Camera.main");
                    return;
                }
                var cell = CellAt(center.position);
                var first = Stopwatch.StartNew();
                plan.Update(cell.x, cell.z, 0, toLoad, toUnload);
                var build = new List<int>();
                AcquireColumns(toLoad, build);
                Rebuild(build);
                first.Stop();
                InitialLoadMs = first.Elapsed.TotalMilliseconds;
                int streamTris = 0;
                foreach (var c in build)
                    streamTris += slots[c].TriangleCount;
                Debug.Log($"TERRAINVIEW nạp dần v{GeneratorVersion}: sinh world {GenerateMs:F0} ms, nạp đầu {LoadedColumnCount} cột / {LoadedChunkCount} chunk trong {InitialLoadMs:F0} ms, {streamTris} tam giác");
                return;
            }
            plan = null;
            slots = new ChunkSlot[s.ChunkCount];
            var shown = new List<int>();
            for (int c = 0; c < s.ChunkCount; c++)
            {
                ChunkCoords(c, out int cx, out int cy, out int cz);
                if (!allChunks && (cx != chunkX || cz != chunkZ))
                    continue;
                slots[c] = CreateSlot(cx, cy, cz, mat, water);
                shown.Add(c);
            }

            var sw = Stopwatch.StartNew();
            Rebuild(shown);
            sw.Stop();
            int tris = 0;
            foreach (var c in shown)
                tris += slots[c].TriangleCount;
            InitialLoadMs = sw.Elapsed.TotalMilliseconds;
            Debug.Log($"TERRAINVIEW {(allChunks ? "cả map" : "một cột")}: {shown.Count} chunk, {tris} tam giác, " +
                      $"dựng mesh {LastMeshMs:F1} ms, ghi {LastWriteMs:F1} ms, bake {LastBakeMs:F1} ms (tổng {sw.ElapsedMilliseconds} ms)");
        }

        /// <summary>Ô gameplay chứa điểm (toạ độ thế giới).</summary>
        public Vector3Int CellAt(Vector3 worldPoint)
        {
            var p = transform.InverseTransformPoint(worldPoint);
            return Vector3Int.FloorToInt(p);
        }

        public EditResult Dig(Vector3Int cell) => Editor.TryDig(cell.x, cell.y, cell.z);

        public EditResult Place(Vector3Int cell, BlockMaterial m) => Editor.TryPlace(cell.x, cell.y, cell.z, m);

        public Mesh ChunkMesh(int chunkIndex) => slots?[chunkIndex]?.Mesh;

        /// <summary>Tổng tam giác đang hiển thị: địa hình (kể cả mép cỏ) + mặt nước theo ô (đo mobile, bước 6).</summary>
        public int TotalTriangles(out int water)
        {
            int terrain = 0;
            water = 0;
            if (slots == null)
                return 0;
            foreach (var s in slots)
            {
                if (s == null) continue;
                terrain += s.TriangleCount;
                water += s.WaterTriangleCount;
            }
            return terrain + water;
        }

        void OnCellChanged(int x, int y, int z) => Editor.AffectedChunks(x, y, z, dirty);

        void LateUpdate()
        {
            StepWater();
            var build = new List<int>();
            LastLoadedColumns = 0;
            if (streaming && plan != null)
            {
                var center = StreamCenter();
                if (center != null)
                {
                    var cell = CellAt(center.position);
                    plan.Update(cell.x, cell.z, maxColumnsPerFrame, toLoad, toUnload);
                    int cy = World.Size.ChunksY, cols = plan.ColumnCount;
                    foreach (var col in toUnload)
                        for (int k = 0; k < cy; k++)
                        {
                            int c = col + cols * k;
                            ReleaseSlot(slots[c]);
                            slots[c] = null;
                        }
                    AcquireColumns(toLoad, build);
                    LastLoadedColumns = toLoad.Count;
                }
            }
            if (dirty.Count == 0 && build.Count == 0)
                return;
            // Bỏ chunk không hiển thị (chế độ một cột / chưa nạp).
            dirty.RemoveAll(c => slots[c] == null);
            bool hadDirty = dirty.Count > 0;
            foreach (var c in dirty)
                if (!build.Contains(c))
                    build.Add(c);
            if (build.Count == 0)
            {
                dirty.Clear();
                return;
            }
            Rebuild(build);
            if (hadDirty)
                Debug.Log($"TERRAINVIEW sửa ô → {LastRebuildChunks} chunk: dựng mesh {LastMeshMs:F2} ms, ghi {LastWriteMs:F2} ms, " +
                          $"bake {LastBakeMs:F2} ms, khung hình {LastRebuildFrame}");
            dirty.Clear();
        }

        /// <summary>Nước lan theo nhịp cố định (không phụ thuộc khung hình); tối đa 4 nhịp mỗi khung để khung chậm không dồn việc.</summary>
        void StepWater()
        {
            if (Editor == null || !Editor.HasPendingWater || waterStepsPerSecond <= 0f)
            {
                waterTimer = 0f;
                return;
            }
            float interval = 1f / waterStepsPerSecond;
            waterTimer += Time.deltaTime;
            for (int k = 0; k < 4 && waterTimer >= interval; k++)
            {
                Editor.StepWater();
                waterTimer -= interval;
            }
            waterTimer = Mathf.Min(waterTimer, interval);
        }

        void Rebuild(List<int> chunkIndices)
        {
            var sw = Stopwatch.StartNew();
            var built = BlockMesher.BuildChunks(World, chunkIndices);
            LastMeshMs = sw.Elapsed.TotalMilliseconds;

            sw.Restart();
            var meshes = new List<Mesh>(chunkIndices.Count);
            foreach (var c in chunkIndices)
                meshes.Add(slots[c].Mesh);
            ChunkMeshWriter.WriteAll(built, meshes);
            var colliderMeshes = new List<Mesh>(chunkIndices.Count);
            foreach (var c in chunkIndices)
                colliderMeshes.Add(slots[c].ColliderMesh);
            ChunkMeshWriter.WriteColliders(built, colliderMeshes);
            LastWriteMs = sw.Elapsed.TotalMilliseconds;

            sw.Restart();
            var toBake = new List<Mesh>();
            for (int k = 0; k < chunkIndices.Count; k++)
            {
                slots[chunkIndices[k]].TriangleCount = built[k].TriangleCount;
                slots[chunkIndices[k]].WaterTriangleCount = built[k].WaterTriangleCount;
                if (built[k].TriangleCount > 0)
                    toBake.Add(colliderMeshes[k]);
            }
            ChunkColliderBaker.BakeAll(toBake);
            foreach (var c in chunkIndices)
            {
                var slot = slots[c];
                // Gán lại để collider nhận hình mới (đã bake sẵn, cookingOptions khớp → không bake lại).
                slot.Collider.sharedMesh = null;
                slot.Collider.enabled = slot.TriangleCount > 0;
                if (slot.TriangleCount > 0)
                    slot.Collider.sharedMesh = slot.ColliderMesh;
            }
            LastBakeMs = sw.Elapsed.TotalMilliseconds;

            LastRebuildChunks = chunkIndices.Count;
            LastRebuildFrame = Time.frameCount;
        }

        static UnityEngine.Material CreateWaterMaterial()
        {
            var shader = Shader.Find("Game/Ocean");
            return shader != null
                ? new UnityEngine.Material(shader)
                : new UnityEngine.Material(GraphicsSettings.currentRenderPipeline.defaultMaterial);
        }

        static UnityEngine.Material CreateTerrainMaterial()
        {
            var shader = Shader.Find("Game/Terrain");
            return shader != null
                ? new UnityEngine.Material(shader)
                : new UnityEngine.Material(GraphicsSettings.currentRenderPipeline.defaultMaterial);
        }

        void AcquireColumns(List<int> columns, List<int> build)
        {
            int cols = plan.ColumnCount;
            foreach (var col in columns)
                for (int cy = 0; cy < World.Size.ChunksY; cy++)
                {
                    int c = col + cols * cy;
                    slots[c] = AcquireSlot(col % plan.ColumnsX, cy, col / plan.ColumnsX);
                    build.Add(c);
                }
        }

        ChunkSlot AcquireSlot(int cx, int cy, int cz)
        {
            if (pool.Count == 0)
                return CreateSlot(cx, cy, cz, slotMat, slotWater);
            var slot = pool.Pop();
            slot.Go.name = slot.Mesh.name = $"chunk {cx},{cy},{cz}";
            slot.Go.transform.localPosition = new Vector3(cx, cy, cz) * WorldSize.ChunkSize;
            slot.Go.SetActive(true);
            return slot;
        }

        void ReleaseSlot(ChunkSlot slot)
        {
            slot.Collider.sharedMesh = null;
            slot.Collider.enabled = false;
            slot.Mesh.Clear();
            slot.ColliderMesh.Clear();
            slot.TriangleCount = slot.WaterTriangleCount = 0;
            slot.Go.SetActive(false);
            pool.Push(slot);
        }

        ChunkSlot CreateSlot(int cx, int cy, int cz, UnityEngine.Material mat, UnityEngine.Material water)
        {
            var mesh = new Mesh { name = $"chunk {cx},{cy},{cz}" };
            var go = new GameObject(mesh.name);
            go.transform.SetParent(chunksRoot, false);
            go.transform.localPosition = new Vector3(cx, cy, cz) * WorldSize.ChunkSize;
            // Collider thêm trước MeshFilter: thêm sau thì Unity tự lấy mesh của MeshFilter và có thể bake ngay (docs Physics.BakeMesh).
            var col = go.AddComponent<MeshCollider>();
            col.cookingOptions = ChunkColliderBaker.CookingOptions;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterials = new[] { mat, water }; // submesh 0 địa hình, 1 mặt nước
            var colliderMesh = new Mesh { name = mesh.name + " collider" };
            return new ChunkSlot { Mesh = mesh, ColliderMesh = colliderMesh, Collider = col, Go = go };
        }

        void ChunkCoords(int c, out int cx, out int cy, out int cz)
        {
            var s = World.Size;
            cx = c % s.ChunksX;
            cz = c / s.ChunksX % s.ChunksZ;
            cy = c / (s.ChunksX * s.ChunksZ);
        }
    }
}
