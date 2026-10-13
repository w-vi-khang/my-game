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

        sealed class ChunkSlot
        {
            public Mesh Mesh;
            /// <summary>Mặt khối vuông chưa bo (decision 0011) — bake cho MeshCollider.</summary>
            public Mesh ColliderMesh;
            public MeshCollider Collider;
            public int TriangleCount;
        }

        const string ChunksName = "Chunks";

        ChunkSlot[] slots;
        Transform chunksRoot;
        readonly List<int> dirty = new List<int>();

        public WorldData World { get; private set; }
        public MaterialInventory Inventory { get; private set; }
        public WorldEditor Editor { get; private set; }

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

            var p = ConfigLoader.LoadTerrainParams();
            World = TerrainGenerator.Generate(p);
            Inventory = new MaterialInventory();
            Editor = new WorldEditor(World, Inventory, BlockMesher.CellMargin, p.waterLevel);
            Editor.CellChanged += OnCellChanged;
            dirty.Clear();

            // Mặc định: shader Game/Terrain (màu đỉnh theo vật liệu, nhám — decision 0011); color là sắc nhân thêm.
            var mat = material != null ? material : CreateTerrainMaterial();
            mat.SetColor("_BaseColor", color);
            var water = waterMaterial != null ? waterMaterial : CreateWaterMaterial();

            var s = World.Size;
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

        void OnCellChanged(int x, int y, int z) => Editor.AffectedChunks(x, y, z, dirty);

        void LateUpdate()
        {
            StepWater();
            if (dirty.Count == 0)
                return;
            // Bỏ chunk không hiển thị (chế độ một cột).
            dirty.RemoveAll(c => slots[c] == null);
            Rebuild(dirty);
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
            return new ChunkSlot { Mesh = mesh, ColliderMesh = colliderMesh, Collider = col };
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
