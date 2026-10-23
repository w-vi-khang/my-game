using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Game.Core;
using NUnit.Framework;

namespace Game.World.Tests
{
    /// <summary>Minimap trên world nhiều map v3 (brief 0015, world-tech 7c): ảnh nền tất định + đúng màu tầng; ranh giới khớp bảng map; tên nằm trong map.</summary>
    public class MinimapWorldTests
    {
        static readonly uint[] Seeds = { 20261013u, 7u, 123456789u, 42u, 99u };

        static readonly Dictionary<uint, (WorldData world, MapTable maps)> Cache = new Dictionary<uint, (WorldData, MapTable)>();

        static (WorldData world, MapTable maps) Get(uint seed)
        {
            if (!Cache.TryGetValue(seed, out var r))
            {
                var w = WorldMapGenerator.Generate(new WorldMapParams { seed = seed }, out var m);
                r = (w, m);
                Cache[seed] = r;
            }
            return r;
        }

        [OneTimeTearDown]
        public void ClearCache() => Cache.Clear();

        [Test]
        public void ImageIsDeterministic()
        {
            var p = new WorldMapParams();
            var a = WorldMapGenerator.Generate(p, out _);
            var b = WorldMapGenerator.Generate(p, out _);
            var sw = Stopwatch.StartNew();
            var ia = MinimapImage.Build(a);
            sw.Stop();
            var ib = MinimapImage.Build(b);
            TestContext.WriteLine($"MINIMAP build {a.Size.X}x{a.Size.Z} ms={sw.Elapsed.TotalMilliseconds:F1}");
            Assert.AreEqual(a.Size.X * a.Size.Z * 4, ia.Length);
            Assert.AreEqual(ia.Length, ib.Length);
            for (int i = 0; i < ia.Length; i++)
                if (ia[i] != ib[i])
                    Assert.Fail($"byte {i} differs: {ia[i]} ≠ {ib[i]}");
        }

        [Test]
        public void EveryPixelMatchesTierRule()
        {
            var (w, _) = Get(new WorldMapParams().seed);
            var img = MinimapImage.Build(w);
            var landTiers = new SortedSet<int>();
            var waterBands = new SortedSet<int>();
            for (int z = 0; z < w.Size.Z; z++)
                for (int x = 0; x < w.Size.X; x++)
                {
                    // Tính độc lập với MinimapImage.ColumnInfo: mặt đất bằng WorldData.SurfaceHeight, nước = ô Water cao nhất phía trên.
                    int s = w.SurfaceHeight(x, z);
                    int top = -1;
                    for (int y = w.Size.Y - 1; y > s; y--)
                        if (w.GetMaterial(x, y, z) == BlockMaterial.Water)
                        {
                            top = y;
                            break;
                        }
                    uint want;
                    if (top >= 0)
                    {
                        int d = top - s;
                        want = MinimapImage.WaterColor(d);
                        waterBands.Add(Math.Min((d - 1) / 2, 3));
                    }
                    else
                    {
                        want = MinimapImage.LandColor(s);
                        landTiers.Add(Math.Min(Math.Max(s, 0) / MinimapImage.TierHeight, 11));
                    }
                    int i = (x + w.Size.X * z) * 4;
                    uint got = (uint)(img[i] << 16 | img[i + 1] << 8 | img[i + 2]);
                    if (got != want || img[i + 3] != 255)
                        Assert.Fail($"column ({x}, {z}): color {got:x6} (alpha {img[i + 3]}) ≠ {want:x6}");
                }
            TestContext.WriteLine($"MINIMAP land tiers present: {string.Join(",", landTiers)} · water bands: {string.Join(",", waterBands)}");
            Assert.GreaterOrEqual(landTiers.Count, 6, "image must have at least 6 land tiers");
            Assert.GreaterOrEqual(waterBands.Count, 2, "image must have at least 2 water bands");
        }

        [Test]
        public void BordersMatchMapTable([ValueSource(nameof(Seeds))] uint seed)
        {
            var (_, maps) = Get(seed);
            bool Sep(byte a, byte b) => a != b && a != MapTable.Ocean && b != MapTable.Ocean;
            // Cạnh ngăn hai map: (x, z, true) = đoạn dọc (x, z)–(x, z + 1); (x, z, false) = đoạn ngang (x, z)–(x + 1, z).
            var edges = new HashSet<(int x, int z, bool vertical)>();
            for (int z = 0; z < maps.SizeZ; z++)
                for (int x = 0; x < maps.SizeX; x++)
                {
                    if (x + 1 < maps.SizeX && Sep(maps.Get(x, z), maps.Get(x + 1, z)))
                        edges.Add((x + 1, z, true));
                    if (z + 1 < maps.SizeZ && Sep(maps.Get(x, z), maps.Get(x, z + 1)))
                        edges.Add((x, z + 1, false));
                }
            Assert.Greater(edges.Count, 0);

            // Chưa làm mượt: mỗi đoạn là đúng một cạnh ngăn map, mỗi cạnh có đúng một lần.
            var raw = MinimapOverlay.Borders(maps, 0);
            var seen = new HashSet<(int x, int z, bool vertical)>();
            foreach (var line in raw)
                for (int i = 0; i + 3 < line.Length; i += 2)
                {
                    float ax = line[i], az = line[i + 1], bx = line[i + 2], bz = line[i + 3];
                    Assert.AreEqual(1f, Math.Abs(bx - ax) + Math.Abs(bz - az), 1e-6f, "unit segment along axis");
                    var key = ax == bx ? ((int)ax, (int)Math.Min(az, bz), true) : ((int)Math.Min(ax, bx), (int)az, false);
                    Assert.IsTrue(edges.Contains(key), $"seed {seed}: segment {key} does not separate two maps");
                    Assert.IsTrue(seen.Add(key), $"seed {seed}: segment {key} repeated");
                }
            Assert.AreEqual(edges.Count, seen.Count, "every edge separating maps is in the border");

            // Đã làm mượt: mỗi điểm cách một cạnh ngăn map ≤ 0,5 ô; trung điểm mỗi cạnh cách đường làm mượt ≤ 0,5 ô.
            var smooth = MinimapOverlay.Borders(maps);
            var edgeList = edges.ToList();
            double maxDev = 0, maxGap = 0;
            int points = 0;
            foreach (var line in smooth)
                for (int i = 0; i + 1 < line.Length; i += 2)
                {
                    points++;
                    double best = double.MaxValue;
                    foreach (var e in edgeList)
                        best = Math.Min(best, DistToEdge(line[i], line[i + 1], e));
                    maxDev = Math.Max(maxDev, best);
                }
            foreach (var e in edgeList)
            {
                double mx = e.x + (e.vertical ? 0 : 0.5), mz = e.z + (e.vertical ? 0.5 : 0);
                double best = double.MaxValue;
                foreach (var line in smooth)
                    for (int i = 0; i + 3 < line.Length; i += 2)
                        best = Math.Min(best, DistToSegment(mx, mz, line[i], line[i + 1], line[i + 2], line[i + 3]));
                maxGap = Math.Max(maxGap, best);
            }
            TestContext.WriteLine($"seed {seed}: {edges.Count} edges, {raw.Count} lines, {points} points after smoothing, max deviation {maxDev:F3}, max gap {maxGap:F3}");
            Assert.LessOrEqual(maxDev, 0.5, $"seed {seed}: smoothed point far from border");
            Assert.LessOrEqual(maxGap, 0.5, $"seed {seed}: smoothed line missed an edge");
        }

        [Test]
        public void LabelsSitInsideTheirMap([ValueSource(nameof(Seeds))] uint seed)
        {
            var (_, maps) = Get(seed);
            var pos = MinimapOverlay.LabelPositions(maps);
            Assert.AreEqual(maps.MapCount * 2, pos.Length);
            for (int m = 0; m < maps.MapCount; m++)
            {
                float x = pos[2 * m], z = pos[2 * m + 1];
                Assert.IsFalse(float.IsNaN(x) || float.IsNaN(z), $"seed {seed}: map {m} has no name position");
                TestContext.WriteLine($"seed {seed}: {maps.Name(m)} at ({x:F1}, {z:F1})");
                Assert.AreEqual(m, (int)maps.Get((int)Math.Floor(x), (int)Math.Floor(z)), $"seed {seed}: name {maps.Name(m)} is outside the map");
            }
        }

        static double DistToEdge(double px, double pz, (int x, int z, bool vertical) e) =>
            e.vertical ? DistToSegment(px, pz, e.x, e.z, e.x, e.z + 1) : DistToSegment(px, pz, e.x, e.z, e.x + 1, e.z);

        static double DistToSegment(double px, double pz, double ax, double az, double bx, double bz)
        {
            double dx = bx - ax, dz = bz - az, len2 = dx * dx + dz * dz;
            double t = len2 > 0 ? Math.Max(0, Math.Min(1, ((px - ax) * dx + (pz - az) * dz) / len2)) : 0;
            double cx = ax + t * dx - px, cz = az + t * dz - pz;
            return Math.Sqrt(cx * cx + cz * cz);
        }
    }
}
