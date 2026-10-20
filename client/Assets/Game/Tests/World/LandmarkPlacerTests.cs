using System;
using System.Collections.Generic;
using System.Diagnostics;
using Game.Core;
using NUnit.Framework;

namespace Game.World.Tests
{
    /// <summary>Dấu mốc bản đồ (decision 0023, world-tech 7d, brief 0017): checkpoint + công trình đặc trưng đặt theo seed sau khi sinh world.</summary>
    public class LandmarkPlacerTests
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

        static List<Landmark> Place(uint seed)
        {
            var (w, m) = Get(seed);
            return LandmarkPlacer.Place(w, m, new WorldMapParams { seed = seed });
        }

        [Test]
        public void PlacesCheckpointPerMapThenStructuresInConfigOrder()
        {
            var p = new WorldMapParams();
            foreach (var seed in Seeds)
            {
                var (w, m) = Get(seed);
                var sw = Stopwatch.StartNew();
                var list = LandmarkPlacer.Place(w, m, new WorldMapParams { seed = seed });
                sw.Stop();
                int expected = m.MapCount;
                foreach (var t in p.landmarkTypes)
                    expected += t.count;
                Assert.AreEqual(expected, list.Count, $"seed {seed}");
                for (int i = 0; i < m.MapCount; i++)
                {
                    Assert.AreEqual(LandmarkKind.Checkpoint, list[i].Kind);
                    Assert.AreEqual(i, list[i].Map);
                    Assert.AreEqual(-1, list[i].Type);
                    Assert.AreEqual("Checkpoint " + m.Name(i), list[i].Name);
                }
                int k = m.MapCount;
                for (int t = 0; t < p.landmarkTypes.Length; t++)
                    for (int c = 0; c < p.landmarkTypes[t].count; c++, k++)
                    {
                        Assert.AreEqual(LandmarkKind.Structure, list[k].Kind);
                        Assert.AreEqual(t, list[k].Type);
                        Assert.AreEqual(p.landmarkTypes[t].map, list[k].Map);
                        Assert.AreEqual(p.landmarkTypes[t].name, list[k].Name);
                    }
                UnityEngine.Debug.Log($"LANDMARKS seed={seed} ms={sw.Elapsed.TotalMilliseconds:F1} | " + string.Join(" | ", list.ConvertAll(l => l.ToString())));
            }
        }

        [Test]
        public void IsDeterministic()
        {
            var a = Place(20261013u);
            var b = Place(20261013u);
            var fresh = WorldMapGenerator.Generate(new WorldMapParams(), out var freshMaps);
            var c = LandmarkPlacer.Place(fresh, freshMaps, new WorldMapParams());
            CollectionAssert.AreEqual(a.ConvertAll(l => l.ToString()), b.ConvertAll(l => l.ToString()));
            CollectionAssert.AreEqual(a.ConvertAll(l => l.ToString()), c.ConvertAll(l => l.ToString()));
        }

        [Test]
        public void OnDryFlatLandDeepInsideOwnMap()
        {
            var p = new WorldMapParams();
            int margin = p.landmarkBorderMargin;
            foreach (var seed in Seeds)
            {
                var (w, m) = Get(seed);
                foreach (var l in Place(seed))
                {
                    Assert.AreEqual(l.Map, m.Get(l.X, l.Z), $"seed {seed} {l}");
                    MinimapImage.ColumnInfo(w, l.X, l.Z, out int surf, out int depth);
                    Assert.AreEqual(surf, l.Surface, $"seed {seed} {l}");
                    Assert.AreEqual(0, depth, $"seed {seed} {l} có nước");
                    Assert.GreaterOrEqual(surf, p.waterLevel, $"seed {seed} {l} thấp hơn mực nước");
                    for (int dz = -1; dz <= 1; dz++)
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            MinimapImage.ColumnInfo(w, l.X + dx, l.Z + dz, out int s2, out int d2);
                            Assert.AreEqual(0, d2, $"seed {seed} {l} láng giềng có nước");
                            Assert.LessOrEqual(Math.Abs(s2 - surf), 1, $"seed {seed} {l} không phẳng");
                        }
                    for (int dz = -margin; dz <= margin; dz++)
                        for (int dx = -margin; dx <= margin; dx++)
                            if (Math.Abs(dx) + Math.Abs(dz) <= margin)
                                Assert.AreEqual(l.Map, m.Get(l.X + dx, l.Z + dz), $"seed {seed} {l} gần biên ({dx},{dz})");
                }
            }
        }

        [Test]
        public void LandmarksAreSpacedApart()
        {
            long min2 = (long)new WorldMapParams().landmarkSpacing * new WorldMapParams().landmarkSpacing;
            foreach (var seed in Seeds)
            {
                var list = Place(seed);
                for (int i = 0; i < list.Count; i++)
                    for (int j = i + 1; j < list.Count; j++)
                    {
                        long dx = list[i].X - list[j].X, dz = list[i].Z - list[j].Z;
                        Assert.GreaterOrEqual(dx * dx + dz * dz, min2, $"seed {seed}: {list[i]} và {list[j]}");
                    }
            }
        }

        [Test]
        public void CheckpointIsNearMapCentroid()
        {
            foreach (var seed in Seeds)
            {
                var (_, m) = Get(seed);
                var list = Place(seed);
                for (int map = 0; map < m.MapCount; map++)
                {
                    double sx = 0, sz = 0;
                    int n = 0;
                    for (int z = 0; z < m.SizeZ; z++)
                        for (int x = 0; x < m.SizeX; x++)
                            if (m.Get(x, z) == map)
                            {
                                sx += x + 0.5;
                                sz += z + 0.5;
                                n++;
                            }
                    double cx = sx / n, cz = sz / n;
                    var cp = list[map];
                    double d = Math.Sqrt((cp.X + 0.5 - cx) * (cp.X + 0.5 - cx) + (cp.Z + 0.5 - cz) * (cp.Z + 0.5 - cz));
                    UnityEngine.Debug.Log($"CHECKPOINT seed={seed} {cp} cách trọng tâm {d:F1} ô");
                    Assert.LessOrEqual(d, 48.0, $"seed {seed} {cp}");
                }
            }
        }

        [Test]
        public void DoesNotChangeWorldOrMapHashes()
        {
            var w = WorldMapGenerator.Generate(new WorldMapParams(), out var m);
            LandmarkPlacer.Place(w, m, new WorldMapParams());
            Assert.AreEqual(GoldenHashTests.ExpectedV3World, WorldHash.ToHex(WorldHash.Compute(w)));
            Assert.AreEqual(GoldenHashTests.ExpectedV3Maps, WorldHash.ToHex(WorldHash.Compute(m)));
        }

        [Test]
        public void InvalidLandmarkConfigThrows()
        {
            Assert.DoesNotThrow(() => new WorldMapParams().Validate());
            Assert.Throws<ArgumentException>(() => new WorldMapParams { landmarkTypes = null }.Validate());
            Assert.Throws<ArgumentException>(() => new WorldMapParams { landmarkTypes = new[] { new LandmarkType(3, "x", 1) } }.Validate());
            Assert.Throws<ArgumentException>(() => new WorldMapParams { landmarkTypes = new[] { new LandmarkType(0, "", 1) } }.Validate());
            Assert.Throws<ArgumentException>(() => new WorldMapParams { landmarkTypes = new[] { new LandmarkType(0, "x", 0) } }.Validate());
            Assert.Throws<ArgumentException>(() => new WorldMapParams { landmarkBorderMargin = 1 }.Validate());
            Assert.Throws<ArgumentException>(() => new WorldMapParams { landmarkSpacing = 0 }.Validate());
        }

        [Test]
        public void BorderDistanceIsZeroAtBordersAndGrowsInside()
        {
            var (_, m) = Get(20261013u);
            var dist = LandmarkPlacer.BorderDistance(m);
            for (int z = 1; z < m.SizeZ - 1; z++)
                for (int x = 1; x < m.SizeX - 1; x++)
                {
                    int i = x + m.SizeX * z;
                    byte id = m.Get(x, z);
                    bool border = id == MapTable.Ocean || m.Get(x - 1, z) != id || m.Get(x + 1, z) != id || m.Get(x, z - 1) != id || m.Get(x, z + 1) != id;
                    if (border)
                    {
                        Assert.AreEqual(0, dist[i], $"({x},{z})");
                        continue;
                    }
                    Assert.Greater(dist[i], 0, $"({x},{z})");
                    int minN = Math.Min(Math.Min(dist[i - 1], dist[i + 1]), Math.Min(dist[i - m.SizeX], dist[i + m.SizeX]));
                    Assert.AreEqual(minN + 1, dist[i], $"({x},{z})");
                }
        }
    }
}
