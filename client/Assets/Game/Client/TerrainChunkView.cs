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
        /// <summary>Max streaming viewpoints (primary + extra). Read when the plan is created in Build.</summary>
        public int maxViewpoints = 2;

        struct ExtraViewpoint { public Transform Target; public int LoadRadius, UnloadRadius; }
        readonly List<ExtraViewpoint> extraViewpoints = new List<ExtraViewpoint>();
        readonly List<StreamViewpoint> viewpointBuffer = new List<StreamViewpoint>();

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
        const string LandmarksName = "Landmarks";
        const string LandmarkMaterialPath = "Materials/Landmark";

        ChunkSlot[] slots;
        Transform chunksRoot;
        readonly List<int> dirty = new List<int>();
        readonly Stack<ChunkSlot> pool = new Stack<ChunkSlot>();
        readonly List<int> toLoad = new List<int>();
        readonly List<int> toUnload = new List<int>();
        ChunkStreamPlan plan;
        UnityEngine.Material slotMat, slotWater;

        public WorldData World { get; private set; }
        public PlayerBag Bag { get; private set; }
        public WorldEditor Editor { get; private set; }
        /// <summary>Bảng map của world v3; null với v2.</summary>
        public MapTable Maps { get; private set; }
        /// <summary>Dấu mốc của world v3 (0023, brief 0017/0018): checkpoint theo map rồi công trình; null với v2.</summary>
        public IReadOnlyList<Landmark> Landmarks { get; private set; }

        /// <summary>Gọi sau khi sinh world + đặt dấu mốc, trước khi dựng khối dấu mốc, editor và mesh (brief 0028); trả danh sách dấu mốc dùng tiếp.</summary>
        [System.NonSerialized] public System.Func<WorldData, MapTable, WorldMapParams, IReadOnlyList<Landmark>, IReadOnlyList<Landmark>> afterGenerate;
        /// <summary>Thời gian LandmarkPlacer.Place (ms).</summary>
        public double LandmarkMs { get; private set; }
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
        public int MissingColumnsWithin(int radius) => MissingColumnsWithin(StreamCenter(), radius);

        /// <summary>Same as MissingColumnsWithin(int) but centred on the given viewpoint (null, not streaming or no plan gives 0).</summary>
        public int MissingColumnsWithin(Transform viewpoint, int radius)
        {
            if (!streaming || plan == null || viewpoint == null)
                return 0;
            var cell = CellAt(viewpoint.position);
            return plan.MissingWithin(cell.x, cell.z, radius);
        }

        /// <summary>Registers an extra streaming viewpoint. Check order: null, radii, duplicate, maximum.</summary>
        public void AddViewpoint(Transform target, int loadRadius, int unloadRadius)
        {
            if (target == null) throw new System.ArgumentNullException(nameof(target));
            if (loadRadius < 0) throw new System.ArgumentOutOfRangeException(nameof(loadRadius));
            if (unloadRadius < loadRadius) throw new System.ArgumentOutOfRangeException(nameof(unloadRadius));
            foreach (var e in extraViewpoints)
                if (e.Target == target) throw new System.ArgumentException("viewpoint already registered", nameof(target));
            if (extraViewpoints.Count + 1 >= maxViewpoints)
                throw new System.InvalidOperationException($"viewpoint maximum {maxViewpoints} reached");
            extraViewpoints.Add(new ExtraViewpoint { Target = target, LoadRadius = loadRadius, UnloadRadius = unloadRadius });
        }

        /// <summary>Removes an extra viewpoint; returns whether it was registered. The primary viewpoint is untouched.</summary>
        public bool RemoveViewpoint(Transform target)
        {
            for (int i = 0; i < extraViewpoints.Count; i++)
                if (extraViewpoints[i].Target == target)
                {
                    extraViewpoints.RemoveAt(i);
                    return true;
                }
            return false;
        }

        /// <summary>Viewpoints currently in effect: primary (if it has a centre) plus extras whose Transform is alive.</summary>
        public int ViewpointCount
        {
            get
            {
                int n = StreamCenter() != null ? 1 : 0;
                foreach (var e in extraViewpoints)
                    if (e.Target != null) n++;
                return n;
            }
        }

        bool CollectViewpoints()
        {
            viewpointBuffer.Clear();
            var center = StreamCenter();
            if (center != null)
            {
                var cell = CellAt(center.position);
                viewpointBuffer.Add(new StreamViewpoint(cell.x, cell.z, loadRadius, unloadRadius));
            }
            foreach (var e in extraViewpoints)
            {
                if (e.Target == null) continue;
                var cell = CellAt(e.Target.position);
                viewpointBuffer.Add(new StreamViewpoint(cell.x, cell.z, e.LoadRadius, e.UnloadRadius));
            }
            return viewpointBuffer.Count > 0;
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
            var oldLandmarks = transform.Find(LandmarksName);
            if (oldLandmarks != null)
                DestroyImmediate(oldLandmarks.gameObject);

            var gen = Stopwatch.StartNew();
            int waterLevel;
            WorldMapParams mapParams = null;
            if (source == WorldSource.MapsV3)
            {
                var p = ConfigLoader.LoadWorldMapParams();
                World = WorldMapGenerator.Generate(p, out var maps);
                Maps = maps;
                mapParams = p;
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
            Landmarks = null;
            if (Maps != null)
            {
                var lm = Stopwatch.StartNew();
                Landmarks = LandmarkPlacer.Place(World, Maps, mapParams);
                LandmarkMs = lm.Elapsed.TotalMilliseconds;
                if (afterGenerate != null)
                    Landmarks = afterGenerate(World, Maps, mapParams, Landmarks);
                BuildLandmarkMarkers();
                Debug.Log($"LANDMARKVIEW {Landmarks.Count} landmarks, placed in {LandmarkMs:F1} ms");
            }
            Bag = new PlayerBag();
            Editor = new WorldEditor(World, Bag, BlockMesher.CellMargin, waterLevel);
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
                plan = new ChunkStreamPlan(s.ChunksX, s.ChunksZ, loadRadius, unloadRadius, maxViewpoints);
                slots = new ChunkSlot[s.ChunkCount];
                var center = StreamCenter();
                if (center == null)
                {
                    Debug.LogError("TERRAINVIEW streaming: no streamTarget and no Camera.main");
                    return;
                }
                CollectViewpoints();
                var first = Stopwatch.StartNew();
                plan.Update(viewpointBuffer, 0, toLoad, toUnload);
                var build = new List<int>();
                AcquireColumns(toLoad, build);
                Rebuild(build);
                first.Stop();
                InitialLoadMs = first.Elapsed.TotalMilliseconds;
                int streamTris = 0;
                foreach (var c in build)
                    streamTris += slots[c].TriangleCount;
                Debug.Log($"TERRAINVIEW streaming v{GeneratorVersion}: world generation {GenerateMs:F0} ms, initial load {LoadedColumnCount} columns / {LoadedChunkCount} chunks in {InitialLoadMs:F0} ms, {streamTris} triangles");
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
            Debug.Log($"TERRAINVIEW {(allChunks ? "whole map" : "one column")}: {shown.Count} chunks, {tris} triangles, " +
                      $"mesh build {LastMeshMs:F1} ms, write {LastWriteMs:F1} ms, bake {LastBakeMs:F1} ms (total {sw.ElapsedMilliseconds} ms)");
        }

        /// <summary>Collider thuộc chunk địa hình (không phải khối dấu mốc, công trình…) — brief 0021.</summary>
        public bool IsTerrainCollider(Collider c) => c != null && chunksRoot != null && c.transform.IsChildOf(chunksRoot);

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
                if (CollectViewpoints())
                {
                    plan.Update(viewpointBuffer, maxColumnsPerFrame, toLoad, toUnload);
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
                Debug.Log($"TERRAINVIEW cell edit → {LastRebuildChunks} chunks: mesh build {LastMeshMs:F2} ms, write {LastWriteMs:F2} ms, " +
                          $"bake {LastBakeMs:F2} ms, frame {LastRebuildFrame}");
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

        /// <summary>Khối đánh dấu trong world (brief 0018): con "Dấu mốc", mỗi dấu mốc một GameObject đáy ở mặt ô cao nhất; có collider để chặn đào/đặt xuyên qua (brief 0021).</summary>
        void BuildLandmarkMarkers()
        {
            var baseMat = Resources.Load<UnityEngine.Material>(LandmarkMaterialPath);
            if (baseMat == null)
            {
                Debug.LogError($"LANDMARKVIEW missing Resources/{LandmarkMaterialPath}.mat — skipping landmark blocks");
                return;
            }
            var root = new GameObject(LandmarksName).transform;
            root.SetParent(transform, false);
            var mats = new Dictionary<uint, UnityEngine.Material>();
            foreach (var l in Landmarks)
            {
                var t = new GameObject(l.Name).transform;
                t.SetParent(root, false);
                t.localPosition = new Vector3(l.X + 0.5f, l.Surface + 1, l.Z + 0.5f);
                var c = LandmarkStyle.ColorOf(l);
                if (l.Kind == LandmarkKind.Checkpoint)
                {
                    MarkerBox(t, new Vector3(0f, 2f, 0f), new Vector3(0.5f, 4f, 0.5f), c, mats, baseMat);
                    MarkerBox(t, new Vector3(0f, 4.6f, 0f), new Vector3(1.2f, 1.2f, 1.2f), c, mats, baseMat);
                }
                else
                    MarkerBox(t, new Vector3(0f, 1.25f, 0f), new Vector3(3f, 2.5f, 3f), c, mats, baseMat);
            }
        }

        static void MarkerBox(Transform parent, Vector3 localPos, Vector3 scale, Color32 c, Dictionary<uint, UnityEngine.Material> mats, UnityEngine.Material baseMat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "Block";
            // Giữ BoxCollider: dấu mốc che địa hình phía sau → chặn đào/đặt xuyên qua (brief 0021).
            var t = go.transform;
            t.SetParent(parent, false);
            t.localPosition = localPos;
            t.localScale = scale;
            uint key = (uint)(c.r << 16 | c.g << 8 | c.b);
            if (!mats.TryGetValue(key, out var mat))
            {
                mat = new UnityEngine.Material(baseMat);
                mat.SetColor("_BaseColor", c);
                mats[key] = mat;
            }
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;
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
