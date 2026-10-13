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

        [OneTimeSetUp]
        public void BuildOnce()
        {
            world = TerrainGenerator.Generate(new TerrainParams());
            meshes = BlockMesher.BuildAll(world);
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
            // Hàn cả map theo vị trí đỉnh. Mặt khối trên lưới nguyên → không có đỉnh chữ T; mỗi cạnh dùng bởi 2 tam giác
            // (hoặc 4 khi hai ô chỉ chạm nhau theo cạnh chéo), số chẵn. Cạnh dùng 1 lần chỉ được phép ở mép map (bề mặt hở ra biển).
            var edgeUse = new Dictionary<(long, long), int>();
            var keyPos = new Dictionary<long, float3>();
            for (int c = 0; c < meshes.Length; c++)
            {
                var m = meshes[c];
                float3 o = ChunkOrigin(c);
                for (int t = 0; t < m.Indices.Length; t += 3)
                for (int e = 0; e < 3; e++)
                {
                    float3 a = m.Vertices[m.Indices[t + e]] + o, b = m.Vertices[m.Indices[t + (e + 1) % 3]] + o;
                    long ka = Key(a), kb = Key(b);
                    keyPos[ka] = a;
                    keyPos[kb] = b;
                    var k = ka < kb ? (ka, kb) : (kb, ka);
                    edgeUse.TryGetValue(k, out int n);
                    edgeUse[k] = n + 1;
                }
            }
            int open = 0, odd = 0, four = 0;
            foreach (var kv in edgeUse)
            {
                bool rim = OnMapRim(keyPos[kv.Key.Item1]) && OnMapRim(keyPos[kv.Key.Item2]);
                if (kv.Value == 1 && !rim)
                {
                    open++;
                    if (open <= 5)
                        TestContext.WriteLine($"OPEN {keyPos[kv.Key.Item1]} - {keyPos[kv.Key.Item2]}");
                }
                if (kv.Value == 4)
                    four++;
                if (kv.Value != 1 && kv.Value % 2 == 1)
                    odd++;
            }
            TestContext.WriteLine($"SEAM cạnh {edgeUse.Count}, hở trong map {open}, dùng 4 lần (chạm chéo) {four}");
            Assert.AreEqual(0, open, "cạnh hở bên trong map (thiếu mặt, vd ở biên chunk)");
            Assert.AreEqual(0, odd, "cạnh dùng số lẻ > 1 lần");
        }

        /// <summary>Cạnh nằm trên mép map theo x/z (mặt đất cắt ngang ở biên map, không có vách — 0009).</summary>
        static bool OnMapRim(float3 p) => p.x == 0f || p.z == 0f || p.x == world.Size.X || p.z == world.Size.Z;

        [Test]
        public void NoDuplicateFacesAcrossChunks()
        {
            // Mỗi mặt (tâm + hướng) chỉ một chunk vẽ.
            var seen = new HashSet<(long, int)>();
            for (int c = 0; c < meshes.Length; c++)
            {
                var m = meshes[c];
                float3 o = ChunkOrigin(c);
                for (int q = 0; q < m.Vertices.Length; q += 4)
                {
                    float3 center = (m.Vertices[q] + m.Vertices[q + 2]) * 0.5f + o;
                    float3 nrm = m.Normals[q];
                    int dir = nrm.x > 0.5f ? 0 : nrm.x < -0.5f ? 1 : nrm.y > 0.5f ? 2 : nrm.y < -0.5f ? 3 : nrm.z > 0.5f ? 4 : 5;
                    Assert.IsTrue(seen.Add((Key(center), dir)), $"mặt trùng ở {center} hướng {nrm} (chunk {c})");
                }
            }
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
                    if (p.x < 0.6f) Assert.Greater(n.x, -0.9f, $"vách hướng ra ngoài ở {p}");
                    if (p.z < 0.6f) Assert.Greater(n.z, -0.9f, $"vách hướng ra ngoài ở {p}");
                    if (p.x > 63.4f) Assert.Less(n.x, 0.9f, $"vách hướng ra ngoài ở {p}");
                    if (p.z > 63.4f) Assert.Less(n.z, 0.9f, $"vách hướng ra ngoài ở {p}");
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
            TestContext.WriteLine($"MESHTIME khối 32 chunk song song: {allMs:F1} ms; 1 chunk đơn: {oneMs:F2} ms (Editor, Burst bật)");
            Assert.Pass();
        }
    }
}
