using Game.Core;
using NUnit.Framework;
using Unity.Mathematics;
using UnityEngine;

namespace Game.World.Tests
{
    public class SurfaceNetsMesherTests
    {
        /// <summary>World 32×16×32, đất đặc dưới y = groundTop.</summary>
        static WorldData Flat(int groundTop)
        {
            var w = new WorldData(new WorldSize(32, 16, 32));
            for (int y = 0; y < groundTop; y++)
            for (int z = 0; z < 32; z++)
            for (int x = 0; x < 32; x++)
                w.Set(x, y, z, Cell.Make(BlockMaterial.Dirt));
            return w;
        }

        [Test]
        public void FlatGroundIsFlatWithUpNormals()
        {
            var mesh = SurfaceNetsMesher.Build(Flat(8), 0, 0, 0);
            Assert.Greater(mesh.TriangleCount, 0);
            int checkedTop = 0;
            for (int i = 0; i < mesh.Vertices.Length; i++)
            {
                float3 p = mesh.Vertices[i];
                // bỏ vùng sát mép map (vách ở x/z = 0 do ngoài map coi là rỗng)
                if (p.y < 7.5f || p.x < 1f || p.z < 1f || p.x > 15f || p.z > 15f)
                    continue;
                Assert.AreEqual(8f, p.y, 1e-4f, $"đỉnh {i} {p}");
                Assert.Greater(mesh.Normals[i].y, 0.99f, $"pháp tuyến {i}");
                checkedTop++;
            }
            Assert.Greater(checkedTop, 500);
        }

        [Test]
        public void TrianglesFaceTheirNormals()
        {
            var mesh = SurfaceNetsMesher.Build(TerrainGenerator.Generate(new TerrainParams()), 1, 0, 1);
            int agree = 0;
            for (int t = 0; t < mesh.Indices.Length; t += 3)
            {
                float3 a = mesh.Vertices[mesh.Indices[t]], b = mesh.Vertices[mesh.Indices[t + 1]], c = mesh.Vertices[mesh.Indices[t + 2]];
                // Unity: mặt trước theo chiều kim đồng hồ → pháp tuyến hình học = cross(b - a, c - a)
                float3 face = math.cross(b - a, c - a);
                float3 vn = mesh.Normals[mesh.Indices[t]] + mesh.Normals[mesh.Indices[t + 1]] + mesh.Normals[mesh.Indices[t + 2]];
                if (math.dot(face, vn) > 0)
                    agree++;
            }
            Assert.Greater(agree, mesh.TriangleCount * 0.99, "thứ tự đỉnh tam giác phải khớp pháp tuyến");
        }

        [Test]
        public void OneCellTrenchKeepsItsWidth()
        {
            // Rãnh rộng 1 ô (x = 8), sâu 2 ô (y = 6, 7) chạy dọc z.
            var w = Flat(8);
            for (int z = 0; z < 32; z++)
            for (int y = 6; y < 8; y++)
                w.Set(8, y, z, Cell.Make(BlockMaterial.Empty));
            var mesh = SurfaceNetsMesher.Build(w, 0, 0, 0);

            float minX = float.MaxValue, maxX = float.MinValue;
            foreach (var p in mesh.Vertices)
            {
                if (p.z < 3f || p.z > 13f || math.abs(p.y - 7f) > 0.3f || p.x < 7f || p.x > 10f)
                    continue;
                minX = math.min(minX, p.x);
                maxX = math.max(maxX, p.x);
            }
            Assert.Less(minX, maxX, "phải có đỉnh trên hai vách rãnh");
            Assert.GreaterOrEqual(maxX - minX, 0.9f, $"bề rộng rãnh ở giữa độ sâu = {maxX - minX}");
        }

        [Test]
        public void SmoothingShiftIsBounded()
        {
            var world = TerrainGenerator.Generate(new TerrainParams());
            for (int cy = 0; cy < world.Size.ChunksY; cy++)
            {
                var mesh = SurfaceNetsMesher.Build(world, 1, cy, 1);
                for (int i = 0; i < mesh.Vertices.Length; i++)
                {
                    float shift = math.distance(mesh.Vertices[i], mesh.RawVertices[i]);
                    Assert.LessOrEqual(shift, SurfaceNetsMesher.MaxShift + 1e-4f, $"đỉnh {i}");
                }
            }
        }

        [Test]
        public void EveryM1ChunkFits16BitIndices()
        {
            var world = TerrainGenerator.Generate(new TerrainParams());
            int maxVerts = 0, maxTris = 0, totalTris = 0, surfaceChunks = 0;
            for (int cy = 0; cy < world.Size.ChunksY; cy++)
            for (int cz = 0; cz < world.Size.ChunksZ; cz++)
            for (int cx = 0; cx < world.Size.ChunksX; cx++)
            {
                var mesh = SurfaceNetsMesher.Build(world, cx, cy, cz);
                Assert.LessOrEqual(mesh.Vertices.Length, ChunkMeshWriter.MaxVertices16, $"chunk {cx},{cy},{cz}");
                maxVerts = math.max(maxVerts, mesh.Vertices.Length);
                maxTris = math.max(maxTris, mesh.TriangleCount);
                totalTris += mesh.TriangleCount;
                if (mesh.TriangleCount > 0)
                    surfaceChunks++;
            }
            TestContext.WriteLine($"MESHSTATS world_m1: {surfaceChunks}/32 chunk có mặt; tam giác tổng {totalTris}, " +
                                  $"lớn nhất {maxTris}/chunk, đỉnh lớn nhất {maxVerts}/chunk");
        }

        [Test]
        public void WritesUnityMeshWith16BitIndices()
        {
            var chunk = SurfaceNetsMesher.Build(Flat(8), 0, 0, 0);
            var mesh = new Mesh();
            try
            {
                ChunkMeshWriter.Write(chunk, mesh);
                Assert.AreEqual(UnityEngine.Rendering.IndexFormat.UInt16, mesh.indexFormat);
                Assert.AreEqual(chunk.Vertices.Length, mesh.vertexCount);
                Assert.AreEqual(chunk.Indices.Length, (int)mesh.GetIndexCount(0));
                Assert.AreEqual(8f, mesh.bounds.max.y, 1e-3f);
            }
            finally
            {
                Object.DestroyImmediate(mesh);
            }
        }
    }
}
