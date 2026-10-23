using System.Collections.Generic;
using System.Diagnostics;
using Game.Core;
using NUnit.Framework;
using Unity.Mathematics;

namespace Game.World.Tests
{
    /// <summary>Ghép nhiều chunk (bước 3, giữ cho mesh khối của 0011): không hở, không trùng mặt ở biên, không vách mép map.</summary>
    public class ChunkSeamTests
    {
        static WorldData world;
        static ChunkMesh[] meshes;
        static ChunkMesh[] meshesNoLip;

        [OneTimeSetUp]
        public void BuildOnce()
        {
            world = TerrainGenerator.Generate(new TerrainParams());
            meshes = BlockMesher.BuildAll(world);
            meshesNoLip = BlockMesher.BuildAll(world, grassLip: false);
        }

        static float3 ChunkOrigin(int c)
        {
            var s = world.Size;
            int cx = c % s.ChunksX, cz = c / s.ChunksX % s.ChunksZ, cy = c / (s.ChunksX * s.ChunksZ);
            return new float3(cx, cy, cz) * WorldSize.ChunkSize;
        }

        static long Key(float3 p)
        {
            // hàn đỉnh theo vị trí (lưới 1/1024 ô)
            var q = (int3)math.round(p * 1024f);
            return ((long)(q.x + 100000) * 400000L + (q.y + 100000)) * 400000L + (q.z + 100000);
        }

        [Test]
        public void ParallelMatchesSingleChunkBuild()
        {
            var s = world.Size;
            for (int c = 0; c < meshes.Length; c += 5)
            {
                int cx = c % s.ChunksX, cz = c / s.ChunksX % s.ChunksZ, cy = c / (s.ChunksX * s.ChunksZ);
                var single = BlockMesher.Build(world, cx, cy, cz);
                CollectionAssert.AreEqual(single.Indices, meshes[c].Indices, $"chunk {c}");
                CollectionAssert.AreEqual(single.Vertices, meshes[c].Vertices, $"chunk {c}");
                CollectionAssert.AreEqual(single.Colors, meshes[c].Colors, $"chunk {c}");
            }
        }

        [Test]
        public void ClosedSurfaceAcrossChunks()
        {
            // Hàn cả map theo vị trí đỉnh: mỗi cạnh a→b có đúng chừng ấy cạnh b→a (mặt kín, cùng chiều) — kể cả qua biên chunk
            // và chỗ bo cạnh. Chỉ cho phép hở ở mép map (mặt đất cắt ngang ở biên map, không có vách — 0009).
            // Mép cỏ là bề mặt hở chồng lên khối → kiểm độ kín trên mesh khối không mép cỏ.
            var parts = new List<(float3[], int[], float3)>();
            for (int c = 0; c < meshesNoLip.Length; c++)
                parts.Add((meshesNoLip[c].Vertices, meshesNoLip[c].Indices, ChunkOrigin(c)));
            int bad = MeshChecks.UnbalancedEdges(parts, (a, b) => OnMapRim(a) && OnMapRim(b), TestContext.WriteLine);
            Assert.AreEqual(0, bad, "open edge or wrong winding inside map");
        }

        /// <summary>Điểm nằm trên mép map theo x/z.</summary>
        static bool OnMapRim(float3 p) => p.x < 1e-3f || p.z < 1e-3f || p.x > world.Size.X - 1e-3f || p.z > world.Size.Z - 1e-3f;

        [Test]
        public void NoDuplicateFacesAcrossChunks()
        {
            // Collider = đúng một tứ giác vuông cho mỗi mặt lộ → mỗi mặt (tâm + hướng) chỉ một chunk sinh.
            var seen = new HashSet<(long, long)>();
            int total = 0;
            for (int c = 0; c < meshes.Length; c++)
            {
                var v = meshes[c].ColliderVertices;
                float3 o = ChunkOrigin(c);
                for (int q = 0; q < v.Length; q += 4)
                {
                    float3 center = (v[q] + v[q + 2]) * 0.5f + o;
                    float3 nrm = math.normalize(math.cross(v[q + 1] - v[q], v[q + 3] - v[q]));
                    Assert.IsTrue(seen.Add((Key(center), Key(nrm))), $"duplicate faces at {center} (chunk {c})");
                    total++;
                }
            }
            Assert.Greater(total, 0);
        }

        [Test]
        public void NoOutwardWallsAtMapRim()
        {
            for (int c = 0; c < meshes.Length; c++)
            {
                var m = meshes[c];
                float3 o = ChunkOrigin(c);
                for (int i = 0; i < m.Vertices.Length; i++)
                {
                    float3 p = m.Vertices[i] + o, n = m.Normals[i];
                    if (p.x < 0.6f) Assert.Greater(n.x, -0.9f, $"wall faces outward at {p}");
                    if (p.z < 0.6f) Assert.Greater(n.z, -0.9f, $"wall faces outward at {p}");
                    if (p.x > 63.4f) Assert.Less(n.x, 0.9f, $"wall faces outward at {p}");
                    if (p.z > 63.4f) Assert.Less(n.z, 0.9f, $"wall faces outward at {p}");
                }
            }
        }

        [Test]
        public void MeasureBuildTime()
        {
            BlockMesher.BuildAll(world); // làm nóng
            var sw = Stopwatch.StartNew();
            const int runs = 5;
            for (int r = 0; r < runs; r++)
                BlockMesher.BuildAll(world);
            sw.Stop();
            double allMs = sw.Elapsed.TotalMilliseconds / runs;

            sw.Restart();
            for (int r = 0; r < runs; r++)
                BlockMesher.Build(world, 1, 1, 1);
            sw.Stop();
            double oneMs = sw.Elapsed.TotalMilliseconds / runs;
            TestContext.WriteLine($"MESHTIME 32 chunks block in parallel: {allMs:F1} ms; 1 single chunk: {oneMs:F2} ms (Editor, Burst on)");
            Assert.Pass();
        }
    }
}
