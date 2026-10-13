using System.Collections.Generic;
using System.Diagnostics;
using Game.Core;
using NUnit.Framework;
using Unity.Mathematics;

namespace Game.World.Tests
{
    public class ChunkSeamTests
    {
        static WorldData world;
        static ChunkMesh[] meshes;

        [OneTimeSetUp]
        public void BuildOnce()
        {
            world = TerrainGenerator.Generate(new TerrainParams());
            meshes = SurfaceNetsMesher.BuildAll(world);
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
                var single = SurfaceNetsMesher.Build(world, cx, cy, cz);
                CollectionAssert.AreEqual(single.Indices, meshes[c].Indices, $"chunk {c}");
                CollectionAssert.AreEqual(single.Vertices, meshes[c].Vertices, $"chunk {c}");
            }
        }

        [Test]
        public void NoHolesAcrossChunks()
        {
            // Ghép cả map theo vị trí đỉnh; mỗi cạnh tam giác phải dùng chung đúng 2 tam giác, trừ cạnh ở mép map.
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
            int interiorOpen = 0, overused = 0;
            foreach (var kv in edgeUse)
            {
                if (kv.Value > 2)
                {
                    overused++;
                    if (overused <= 5)
                        TestContext.WriteLine($"OVERUSED {keyPos[kv.Key.Item1]} - {keyPos[kv.Key.Item2]} x{kv.Value}");
                }
                if (kv.Value == 1 && !OnMapRim(keyPos[kv.Key.Item1]) && !OnMapRim(keyPos[kv.Key.Item2]))
                    interiorOpen++;
            }
            int inChunkOverused = 0;
            for (int c = 0; c < meshes.Length; c++)
            {
                var m = meshes[c];
                var use = new Dictionary<(int, int), int>();
                for (int t = 0; t < m.Indices.Length; t += 3)
                for (int e = 0; e < 3; e++)
                {
                    int a = m.Indices[t + e], b = m.Indices[t + (e + 1) % 3];
                    var k = a < b ? (a, b) : (b, a);
                    use.TryGetValue(k, out int n);
                    use[k] = n + 1;
                }
                foreach (var v in use.Values)
                    if (v > 2) inChunkOverused++;
            }
            TestContext.WriteLine($"OVERUSED tổng {overused}, trong cùng chunk {inChunkOverused}, tổng cạnh {edgeUse.Count}");
            // Surface Nets có thể sinh cạnh không đa tạp khi hai cột chỉ chạm nhau ở góc chéo (0fps, xem risks.md) —
            // cho phép, nhưng mọi cạnh như vậy phải nằm gọn trong một chunk (không do nối biên sinh ra).
            Assert.AreEqual(inChunkOverused, overused, "cạnh dùng > 2 tam giác do nối biên chunk");
            Assert.AreEqual(0, interiorOpen, "cạnh hở bên trong map (hở ở biên chunk)");
        }

        static bool OnMapRim(float3 p) => p.x < 0.6f || p.z < 0.6f || p.x > 63.4f || p.z > 63.4f;

        [Test]
        public void SharedSeamVerticesHaveSameNormal()
        {
            var normalAt = new Dictionary<long, float3>();
            int shared = 0;
            for (int c = 0; c < meshes.Length; c++)
            {
                var m = meshes[c];
                float3 o = ChunkOrigin(c);
                var used = new HashSet<int>(m.Indices);
                foreach (int i in used)
                {
                    long k = Key(m.Vertices[i] + o);
                    if (normalAt.TryGetValue(k, out var n))
                    {
                        shared++;
                        Assert.Less(math.distance(n, m.Normals[i]), 1e-4f, $"pháp tuyến lệch ở {m.Vertices[i] + o}");
                    }
                    else
                        normalAt[k] = m.Normals[i];
                }
            }
            Assert.Greater(shared, 100, "phải có đỉnh dùng chung ở biên chunk");
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
            SurfaceNetsMesher.BuildAll(world); // làm nóng
            var sw = Stopwatch.StartNew();
            const int runs = 5;
            for (int r = 0; r < runs; r++)
                SurfaceNetsMesher.BuildAll(world);
            sw.Stop();
            double allMs = sw.Elapsed.TotalMilliseconds / runs;

            sw.Restart();
            for (int r = 0; r < runs; r++)
                SurfaceNetsMesher.Build(world, 1, 1, 1);
            sw.Stop();
            double oneMs = sw.Elapsed.TotalMilliseconds / runs;
            TestContext.WriteLine($"MESHTIME 32 chunk song song: {allMs:F1} ms; 1 chunk đơn: {oneMs:F2} ms (Editor, Burst bật)");
            Assert.Pass();
        }
    }
}
