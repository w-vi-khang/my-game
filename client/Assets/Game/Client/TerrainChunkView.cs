using Game.Core;
using Game.World;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game.Client
{
    /// <summary>
    /// Hiển thị một cột chunk của world M1 (world-tech bước 2). Bấm Play trong scene TerrainView, hoặc gọi Build() từ editor.
    /// </summary>
    public class TerrainChunkView : MonoBehaviour
    {
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

            for (int cy = 0; cy < world.Size.ChunksY; cy++)
            {
                var chunk = SurfaceNetsMesher.Build(world, chunkX, cy, chunkZ);
                if (chunk.TriangleCount == 0)
                    continue;
                var mesh = new Mesh { name = $"chunk {chunkX},{cy},{chunkZ}" };
                ChunkMeshWriter.Write(chunk, mesh);
                var go = new GameObject(mesh.name);
                go.transform.SetParent(transform, false);
                go.transform.localPosition = new Vector3(chunkX, cy, chunkZ) * WorldSize.ChunkSize;
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                go.AddComponent<MeshRenderer>().sharedMaterial = mat;
                Debug.Log($"TERRAINVIEW {mesh.name}: {chunk.Vertices.Length} đỉnh, {chunk.TriangleCount} tam giác");
            }
        }
    }
}
