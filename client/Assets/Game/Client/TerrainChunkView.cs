using Game.Core;
using Game.World;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game.Client
{
    /// <summary>
    /// Hiển thị world M1: cả map (bước 3) hoặc một cột chunk (bước 2). Bấm Play trong scene TerrainView, hoặc gọi Build() từ editor.
    /// </summary>
    public class TerrainChunkView : MonoBehaviour
    {
        public bool allChunks = true;
        public int chunkX = 1;
        public int chunkZ = 1;
        public UnityEngine.Material material;
        public Color color = new Color(0.45f, 0.72f, 0.35f);

        void Start()
        {
            Build();
        }

        public void Build()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
                DestroyImmediate(transform.GetChild(i).gameObject);

            var p = ConfigLoader.LoadTerrainParams();
            var world = TerrainGenerator.Generate(p);
            var mat = material != null ? material : new UnityEngine.Material(GraphicsSettings.currentRenderPipeline.defaultMaterial);
            mat.color = color;

            var s = world.Size;
            if (allChunks)
            {
                var sw = System.Diagnostics.Stopwatch.StartNew();
                var all = SurfaceNetsMesher.BuildAll(world);
                sw.Stop();
                int tris = 0;
                for (int c = 0; c < all.Length; c++)
                {
                    int cx = c % s.ChunksX, cz = c / s.ChunksX % s.ChunksZ, cy = c / (s.ChunksX * s.ChunksZ);
                    AddChunk(all[c], cx, cy, cz, mat);
                    tris += all[c].TriangleCount;
                }
                Debug.Log($"TERRAINVIEW cả map: {all.Length} chunk, {tris} tam giác, dựng mesh {sw.ElapsedMilliseconds} ms");
            }
            else
            {
                for (int cy = 0; cy < s.ChunksY; cy++)
                    AddChunk(SurfaceNetsMesher.Build(world, chunkX, cy, chunkZ), chunkX, cy, chunkZ, mat);
            }
        }

        void AddChunk(ChunkMesh chunk, int cx, int cy, int cz, UnityEngine.Material mat)
        {
            if (chunk.TriangleCount == 0)
                return;
            var mesh = new Mesh { name = $"chunk {cx},{cy},{cz}" };
            ChunkMeshWriter.Write(chunk, mesh);
            var go = new GameObject(mesh.name);
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(cx, cy, cz) * WorldSize.ChunkSize;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = mat;
        }
    }
}
