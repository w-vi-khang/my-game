using System.Collections.Generic;
using Game.Core;
using NUnit.Framework;
using Unity.Mathematics;

namespace Game.World.Tests
{
    public class IncrementalRebuildTests
    {
        static void AssertSameMesh(ChunkMesh expected, ChunkMesh actual, string msg)
        {
            CollectionAssert.AreEqual(expected.Indices, actual.Indices, msg);
            CollectionAssert.AreEqual(expected.Vertices, actual.Vertices, msg);
            CollectionAssert.AreEqual(expected.Normals, actual.Normals, msg);
        }

        [Test]
        public void CellMarginIsTwo()
        {
            Assert.AreEqual(2, SurfaceNetsMesher.CellMargin);
        }

        [Test]
        public void IncrementalRebuildMatchesFullRebuild()
        {
            var world = TerrainGenerator.Generate(new TerrainParams());
            var inv = new MaterialInventory();
            inv.Add(BlockMaterial.Dirt, 10000);
            var editor = new WorldEditor(world, inv, SurfaceNetsMesher.CellMargin);
            var meshes = SurfaceNetsMesher.BuildAll(world);
            var s = world.Size;
            var rng = new System.Random(12345);
            // Nửa số lần sửa rơi sát biên chunk (ô cục bộ 0, 1, 2, 13, 14, 15) để thử vùng đệm.
            int[] nearEdge = { 0, 1, 2, 13, 14, 15 };
            int Coord(int size) => rng.Next(2) == 0
                ? rng.Next(size / 16) * 16 + nearEdge[rng.Next(nearEdge.Length)]
                : rng.Next(size);

            int edits = 0, checks = 0;
            var dirty = new List<int>();
            for (int attempt = 0; edits < 200 && attempt < 20000; attempt++)
            {
                int x = Coord(s.X), z = Coord(s.Z);
                int surface = world.SurfaceHeight(x, z);
                int y = math.clamp(surface + rng.Next(-2, 3), 0, s.Y - 1);
                if (y == 15 || y == 16 || rng.Next(4) == 0)
                    y = rng.Next(14, 18); // thử biên chunk theo y
                var r = rng.Next(2) == 0 ? editor.TryDig(x, y, z) : editor.TryPlace(x, y, z, BlockMaterial.Dirt);
                if (r != EditResult.Ok)
                    continue;
                edits++;
                dirty.Clear();
                editor.AffectedChunks(x, y, z, dirty);
                var rebuilt = SurfaceNetsMesher.BuildChunks(world, dirty);
                for (int k = 0; k < dirty.Count; k++)
                    meshes[dirty[k]] = rebuilt[k];

                if (edits % 10 == 0)
                {
                    checks++;
                    var full = SurfaceNetsMesher.BuildAll(world);
                    for (int c = 0; c < full.Length; c++)
                        AssertSameMesh(full[c], meshes[c], $"chunk {c} sau {edits} lần sửa");
                }
            }
            Assert.AreEqual(200, edits, "không đủ 200 lần sửa hợp lệ");
            Assert.AreEqual(20, checks);
        }

        [Test]
        public void CellTwoAwayChangesNeighborChunk()
        {
            // Chứng minh cần đệm 2 ô (không phải 1): đào ô x = 17 (cục bộ 1 của chunk x = 1) làm đổi mesh chunk x = 0.
            var world = TerrainGenerator.Generate(new TerrainParams());
            var editor = new WorldEditor(world, new MaterialInventory(), SurfaceNetsMesher.CellMargin);
            int tried = 0, changedCases = 0;
            for (int z = 1; z < 15; z += 2)
            {
                int y = world.SurfaceHeight(17, z);
                if (y < 0 || y >= 16)
                    continue;
                var before = SurfaceNetsMesher.Build(world, 0, 0, 0);
                if (editor.TryDig(17, y, z) != EditResult.Ok)
                    continue;
                tried++;
                var after = SurfaceNetsMesher.Build(world, 0, 0, 0);
                bool differs = before.Vertices.Length != after.Vertices.Length;
                for (int i = 0; !differs && i < after.Vertices.Length; i++)
                    differs = !before.Vertices[i].Equals(after.Vertices[i]) || !before.Normals[i].Equals(after.Normals[i]);
                if (differs)
                    changedCases++;
            }
            Assert.Greater(tried, 0, "không có cột nào để thử");
            Assert.Greater(changedCases, 0, $"đào ô cách biên 2 ô không đổi mesh chunk bên cạnh ({tried} lần thử)");
        }
    }
}
