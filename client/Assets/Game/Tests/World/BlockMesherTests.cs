using Game.Core;
using NUnit.Framework;
using Unity.Mathematics;
using UnityEngine;

namespace Game.World.Tests
{
    /// <summary>Bước 5.1 (decision 0011): mesh khối — mặt lộ, màu theo vật liệu, chiều tam giác, 16 bit.</summary>
    public class BlockMesherTests
    {
        /// <summary>World 32×16×32, cỏ ở lớp trên cùng, đất bên dưới, mặt đất ở y = groundTop.</summary>
        static WorldData Flat(int groundTop)
        {
            var w = new WorldData(new WorldSize(32, 16, 32));
            for (int y = 0; y < groundTop; y++)
            for (int z = 0; z < 32; z++)
            for (int x = 0; x < 32; x++)
                w.Set(x, y, z, Cell.Make(y == groundTop - 1 ? BlockMaterial.Grass : BlockMaterial.Dirt));
            return w;
        }

        /// <summary>Đếm mặt lộ bằng cách duyệt thẳng ô (cùng luật biên với mesher: dưới đáy đặc, trên trời rỗng, ngoài x/z lấy cột biên).</summary>
        static int CountExposedFaces(WorldData w, int cx, int cy, int cz)
        {
            var s = w.Size;
            bool Solid(int x, int y, int z)
            {
                if (y < 0) return true;
                if (y >= s.Y) return false;
                x = math.clamp(x, 0, s.X - 1);
                z = math.clamp(z, 0, s.Z - 1);
                var m = w.GetMaterial(x, y, z);
                return m != BlockMaterial.Empty && m != BlockMaterial.Water;
            }
            int faces = 0;
            for (int y = cy * 16; y < cy * 16 + 16; y++)
            for (int z = cz * 16; z < cz * 16 + 16; z++)
            for (int x = cx * 16; x < cx * 16 + 16; x++)
            {
                if (!Solid(x, y, z) || !s.InBounds(x, y, z))
                    continue;
                if (!Solid(x + 1, y, z)) faces++;
                if (!Solid(x - 1, y, z)) faces++;
                if (!Solid(x, y + 1, z)) faces++;
                if (!Solid(x, y - 1, z)) faces++;
                if (!Solid(x, y, z + 1)) faces++;
                if (!Solid(x, y, z - 1)) faces++;
            }
            return faces;
        }

        [Test]
        public void FlatGroundIsOneQuadPerColumnOnTop()
        {
            var mesh = BlockMesher.Build(Flat(8), 0, 0, 0);
            // Mặt phẳng, không vách (mép map lấy cột biên) → đúng 16 × 16 mặt trên ở y = 8.
            Assert.AreEqual(16 * 16 * 2, mesh.TriangleCount);
            for (int i = 0; i < mesh.Vertices.Length; i++)
            {
                Assert.AreEqual(8f, mesh.Vertices[i].y, 1e-6f, $"đỉnh {i}");
                Assert.AreEqual(new float3(0, 1, 0), mesh.Normals[i]);
                Assert.AreEqual(BlockMesher.TopColor(BlockMaterial.Grass), mesh.Colors[i]);
            }
        }

        [Test]
        public void TriangleCountIsTwicePerExposedFaceOnM1World()
        {
            var world = TerrainGenerator.Generate(new TerrainParams());
            var s = world.Size;
            int total = 0;
            for (int cy = 0; cy < s.ChunksY; cy++)
            for (int cz = 0; cz < s.ChunksZ; cz++)
            for (int cx = 0; cx < s.ChunksX; cx++)
            {
                var mesh = BlockMesher.Build(world, cx, cy, cz);
                int faces = CountExposedFaces(world, cx, cy, cz);
                Assert.AreEqual(faces * 2, mesh.TriangleCount, $"chunk {cx},{cy},{cz}");
                Assert.AreEqual(faces * 4, mesh.Vertices.Length, $"chunk {cx},{cy},{cz}");
                total += mesh.TriangleCount;
            }
            TestContext.WriteLine($"MESHSTATS khối world_m1: {total} tam giác cả map");
        }

        [Test]
        public void NoFaceBetweenTwoSolidCells()
        {
            // Hai ô đặc kề nhau giữa không trung: 10 mặt lộ, không có mặt chung ở x = 5.
            var w = new WorldData(new WorldSize(16, 16, 16));
            w.Set(4, 8, 4, Cell.Make(BlockMaterial.Stone));
            w.Set(5, 8, 4, Cell.Make(BlockMaterial.Stone));
            var mesh = BlockMesher.Build(w, 0, 0, 0);
            Assert.AreEqual(10 * 2, mesh.TriangleCount);
            for (int t = 0; t < mesh.Indices.Length; t += 3)
            {
                float3 a = mesh.Vertices[mesh.Indices[t]], b = mesh.Vertices[mesh.Indices[t + 1]], c = mesh.Vertices[mesh.Indices[t + 2]];
                bool onSharedPlane = a.x == 5f && b.x == 5f && c.x == 5f;
                Assert.IsFalse(onSharedPlane, $"mặt thừa ở x = 5: {a} {b} {c}");
            }
        }

        [Test]
        public void GrassSidesUseDirtColor()
        {
            var w = new WorldData(new WorldSize(16, 16, 16));
            w.Set(4, 8, 4, Cell.Make(BlockMaterial.Grass));
            var mesh = BlockMesher.Build(w, 0, 0, 0);
            Assert.AreEqual(6 * 4, mesh.Vertices.Length);
            for (int i = 0; i < mesh.Vertices.Length; i++)
            {
                uint expected = mesh.Normals[i].y > 0.5f
                    ? BlockMesher.TopColor(BlockMaterial.Grass)
                    : BlockMesher.TopColor(BlockMaterial.Dirt);
                Assert.AreEqual(expected, mesh.Colors[i], $"đỉnh {i} pháp tuyến {mesh.Normals[i]}");
            }
        }

        [Test]
        public void WaterIsNotSolid()
        {
            // Ô đất dưới ô nước có mặt trên (nước là rỗng với mesher — mặt nước là bước 5b).
            var w = new WorldData(new WorldSize(16, 16, 16));
            w.Set(4, 8, 4, Cell.Make(BlockMaterial.Dirt));
            w.Set(4, 9, 4, Cell.Make(BlockMaterial.Water));
            var mesh = BlockMesher.Build(w, 0, 0, 0);
            Assert.AreEqual(6 * 2, mesh.TriangleCount);
        }

        [Test]
        public void TrianglesFaceTheirNormals()
        {
            var mesh = BlockMesher.Build(TerrainGenerator.Generate(new TerrainParams()), 1, 0, 1);
            Assert.Greater(mesh.TriangleCount, 0);
            for (int t = 0; t < mesh.Indices.Length; t += 3)
            {
                float3 a = mesh.Vertices[mesh.Indices[t]], b = mesh.Vertices[mesh.Indices[t + 1]], c = mesh.Vertices[mesh.Indices[t + 2]];
                // Quy ước như mesher cũ: pháp tuyến hình học = cross(b - a, c - a) cùng hướng pháp tuyến đỉnh.
                float3 face = math.cross(b - a, c - a);
                Assert.Greater(math.dot(face, mesh.Normals[mesh.Indices[t]]), 0f, $"tam giác {t / 3}");
            }
        }

        [Test]
        public void EveryM1ChunkFits16BitIndices()
        {
            var world = TerrainGenerator.Generate(new TerrainParams());
            int maxVerts = 0, maxTris = 0, surfaceChunks = 0;
            foreach (var mesh in BlockMesher.BuildAll(world))
            {
                Assert.LessOrEqual(mesh.Vertices.Length, ChunkMeshWriter.MaxVertices16);
                maxVerts = math.max(maxVerts, mesh.Vertices.Length);
                maxTris = math.max(maxTris, mesh.TriangleCount);
                if (mesh.TriangleCount > 0)
                    surfaceChunks++;
            }
            TestContext.WriteLine($"MESHSTATS khối: {surfaceChunks}/32 chunk có mặt; lớn nhất {maxTris} tam giác, {maxVerts} đỉnh/chunk");
        }

        [Test]
        public void WritesUnityMeshWithColorsAnd16BitIndices()
        {
            var chunk = BlockMesher.Build(Flat(8), 0, 0, 0);
            var mesh = new Mesh();
            try
            {
                ChunkMeshWriter.Write(chunk, mesh);
                Assert.AreEqual(UnityEngine.Rendering.IndexFormat.UInt16, mesh.indexFormat);
                Assert.AreEqual(chunk.Vertices.Length, mesh.vertexCount);
                Assert.AreEqual(chunk.Indices.Length, (int)mesh.GetIndexCount(0));
                Assert.AreEqual(8f, mesh.bounds.max.y, 1e-3f);
                var colors = mesh.colors32;
                Assert.AreEqual(chunk.Vertices.Length, colors.Length);
                Assert.AreEqual(new Color32(118, 186, 72, 255), colors[0]);
            }
            finally
            {
                Object.DestroyImmediate(mesh);
            }
        }
    }
}
