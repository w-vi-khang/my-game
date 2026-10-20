using System;
using System.Collections.Generic;
using System.Diagnostics;
using Game.Core;
using NUnit.Framework;

namespace Game.World.Tests
{
    /// <summary>Bộ sinh world nhiều map v3 (brief 0009, decision 0022).</summary>
    public class WorldMapGeneratorTests
    {
        static readonly uint[] Seeds = { 20261013u, 7u, 123456789u, 42u, 99u };

        static readonly Dictionary<uint, (WorldData world, MapTable maps)> Cache = new Dictionary<uint, (WorldData, MapTable)>();

        static WorldMapParams Params(uint seed) => new WorldMapParams { seed = seed };

        static (WorldData world, MapTable maps) Get(uint seed)
        {
            if (!Cache.TryGetValue(seed, out var r))
            {
                var w = WorldMapGenerator.Generate(Params(seed), out var m);
                r = (w, m);
                Cache[seed] = r;
            }
            return r;
        }

        [OneTimeTearDown]
        public void ClearCache() => Cache.Clear();

        static bool IsLandColumn(WorldData w, WorldMapParams p, int x, int z) => w.SurfaceHeight(x, z) >= p.waterLevel;

        [Test]
        public void RunsWithBurst()
        {
            WorldMapGenerator.Generate(Params(20261013), out _, out bool burst);
            Assert.IsTrue(burst, "Job sinh world nhiều map phải chạy bằng Burst (FloatMode.Deterministic)");
        }

        [Test]
        public void SameSeedSameWorldAndMaps()
        {
            var w1 = WorldMapGenerator.Generate(Params(20261013), out var m1);
            var sw = Stopwatch.StartNew();
            var w2 = WorldMapGenerator.Generate(Params(20261013), out var m2);
            sw.Stop();
            TestContext.WriteLine($"sinh world v3 lần 2: {sw.ElapsedMilliseconds} ms");
            Assert.AreEqual(WorldHash.Compute(w1), WorldHash.Compute(w2));
            Assert.AreEqual(WorldHash.Compute(m1), WorldHash.Compute(m2));
        }

        [Test]
        public void DifferentSeedDifferentWorldAndMaps()
        {
            var wa = WorldMapGenerator.Generate(Params(1), out var ma);
            var wb = WorldMapGenerator.Generate(Params(2), out var mb);
            Assert.AreNotEqual(WorldHash.Compute(wa), WorldHash.Compute(wb));
            Assert.AreNotEqual(WorldHash.Compute(ma), WorldHash.Compute(mb));
        }

        [Test]
        public void SizeIs384x48x384()
        {
            var (w, maps) = Get(20261013);
            Assert.AreEqual(new WorldSize(384, 48, 384), w.Size);
            Assert.AreEqual(1728, w.Size.ChunkCount);
            Assert.AreEqual(384, maps.SizeX);
            Assert.AreEqual(384, maps.SizeZ);
            Assert.AreEqual(3, maps.MapCount);
            Assert.AreEqual("Map 1", maps.Name(0));
        }

        [Test]
        public void LayerRules()
        {
            var p = Params(20261013);
            var (w, _) = Get(20261013);
            int waterCells = 0;
            for (int x = 0; x < p.sizeX; x++)
            for (int z = 0; z < p.sizeZ; z++)
            {
                for (int y = 0; y < p.bedrockLayers; y++)
                    Assert.AreEqual(BlockMaterial.Bedrock, w.GetMaterial(x, y, z), $"đá đáy ({x},{y},{z})");

                int s = w.SurfaceHeight(x, z);
                Assert.That(s, Is.InRange(p.bedrockLayers, p.maxSurface), $"mặt đất ({x},{z})");
                if (s < p.minSurface)
                    Assert.AreEqual(BlockMaterial.Water, w.GetMaterial(x, s + 1, z), $"dưới minSurface phải có nước ngay trên ({x},{z})");

                var top = w.GetMaterial(x, s, z);
                if (s <= p.waterLevel + p.sandBand)
                    Assert.AreEqual(BlockMaterial.Sand, top, $"ô thấp gần nước là cát ({x},{z})");
                else
                    Assert.AreEqual(BlockMaterial.Grass, top, $"trên cùng là cỏ ({x},{z})");

                for (int y = p.bedrockLayers; y < s; y++)
                {
                    var expected = s - y <= p.dirtDepth ? BlockMaterial.Dirt : BlockMaterial.Stone;
                    Assert.AreEqual(expected, w.GetMaterial(x, y, z), $"lớp ({x},{y},{z})");
                }

                for (int y = s + 1; y < p.sizeY; y++)
                {
                    ushort c = w.Get(x, y, z);
                    if (y <= p.waterLevel)
                    {
                        Assert.AreEqual(BlockMaterial.Water, Cell.GetMaterial(c), $"nước tĩnh ({x},{y},{z})");
                        Assert.AreEqual(Cell.MaxWaterLevel, Cell.GetWaterLevel(c));
                        waterCells++;
                    }
                    else
                        Assert.AreEqual(BlockMaterial.Empty, Cell.GetMaterial(c), $"không khí ({x},{y},{z})");
                }
            }
            TestContext.WriteLine($"ô nước: {waterCells}");
            Assert.Greater(waterCells, 0, "phải có nước");
        }

        [Test]
        public void SeaRingIsOcean()
        {
            var p = Params(20261013);
            var (w, maps) = Get(20261013);
            int seaFloor = p.waterLevel - p.seaFloorDepth, ring = 0;
            for (int z = 0; z < p.sizeZ; z++)
            for (int x = 0; x < p.sizeX; x++)
            {
                int edge = Math.Min(Math.Min(x, z), Math.Min(p.sizeX - 1 - x, p.sizeZ - 1 - z));
                if (edge >= p.seaRing)
                    continue;
                ring++;
                Assert.AreEqual(seaFloor, w.SurfaceHeight(x, z), $"đáy biển ({x},{z})");
                Assert.AreEqual(BlockMaterial.Water, w.GetMaterial(x, p.waterLevel, z), $"nước ở mặt biển ({x},{z})");
                Assert.AreEqual(BlockMaterial.Empty, w.GetMaterial(x, p.waterLevel + 1, z), $"trên mặt biển ({x},{z})");
                Assert.AreEqual(MapTable.Ocean, maps.Get(x, z), $"bảng map ở vành biển ({x},{z})");
            }
            TestContext.WriteLine($"cột vành biển: {ring}");
            Assert.Greater(ring, 0);
        }

        [Test]
        public void OceanColumnsAreSeaFloorAndLandHasAMap([ValueSource(nameof(Seeds))] uint seed)
        {
            var p = Params(seed);
            var (w, maps) = Get(seed);
            int seaFloor = p.waterLevel - p.seaFloorDepth;
            for (int z = 0; z < p.sizeZ; z++)
            for (int x = 0; x < p.sizeX; x++)
            {
                byte id = maps.Get(x, z);
                if (id == MapTable.Ocean)
                    Assert.AreEqual(seaFloor, w.SurfaceHeight(x, z), $"cột biển phải ở đáy biển ({x},{z})");
                else
                    Assert.Less(id, maps.MapCount, $"id map ngoài phạm vi ({x},{z})");
                if (IsLandColumn(w, p, x, z))
                    Assert.AreNotEqual(MapTable.Ocean, id, $"cột đất phải thuộc một map ({x},{z})");
            }
        }

        [Test]
        public void EachMapLandAreaWithin25Percent([ValueSource(nameof(Seeds))] uint seed)
        {
            var p = Params(seed);
            var (w, maps) = Get(seed);
            var count = new int[maps.MapCount];
            for (int z = 0; z < p.sizeZ; z++)
            for (int x = 0; x < p.sizeX; x++)
            {
                byte id = maps.Get(x, z);
                if (id != MapTable.Ocean && IsLandColumn(w, p, x, z))
                    count[id]++;
            }
            TestContext.WriteLine($"seed {seed}: cột đất theo map = [{string.Join(", ", count)}]");
            for (int i = 0; i < count.Length; i++)
                Assert.That(count[i], Is.InRange(12288, 20480), $"diện tích map {i} (seed {seed})");
        }

        [Test]
        public void EachMapIsOneRegion([ValueSource(nameof(Seeds))] uint seed)
        {
            var p = Params(seed);
            var (_, maps) = Get(seed);
            for (int i = 0; i < maps.MapCount; i++)
            {
                int total = 0, startX = -1, startZ = -1;
                for (int z = 0; z < p.sizeZ; z++)
                for (int x = 0; x < p.sizeX; x++)
                {
                    if (maps.Get(x, z) != i)
                        continue;
                    total++;
                    if (startX < 0)
                    {
                        startX = x;
                        startZ = z;
                    }
                }
                Assert.Greater(total, 0, $"map {i} rỗng (seed {seed})");

                var seen = new bool[p.sizeX * p.sizeZ];
                var stack = new Stack<int>();
                stack.Push(startX + p.sizeX * startZ);
                seen[startX + p.sizeX * startZ] = true;
                int reached = 0;
                while (stack.Count > 0)
                {
                    int c = stack.Pop();
                    reached++;
                    int cx = c % p.sizeX, cz = c / p.sizeX;
                    TryPush(cx + 1, cz);
                    TryPush(cx - 1, cz);
                    TryPush(cx, cz + 1);
                    TryPush(cx, cz - 1);
                }
                TestContext.WriteLine($"seed {seed}: map {i} có {total} cột, tới được {reached}");
                Assert.AreEqual(total, reached, $"map {i} bị đứt thành nhiều mảnh (seed {seed})");

                void TryPush(int nx, int nz)
                {
                    if (nx < 0 || nz < 0 || nx >= p.sizeX || nz >= p.sizeZ)
                        return;
                    int idx = nx + p.sizeX * nz;
                    if (seen[idx] || maps.Get(nx, nz) != i)
                        return;
                    seen[idx] = true;
                    stack.Push(idx);
                }
            }
        }

        [Test]
        public void EachBorderBendsAtLeast8([ValueSource(nameof(Seeds))] uint seed)
        {
            var p = Params(seed);
            var (_, maps) = Get(seed);
            for (int a = 0; a < maps.MapCount; a++)
            for (int b = a + 1; b < maps.MapCount; b++)
            {
                var border = new List<(int x, int z)>();
                for (int z = 0; z < p.sizeZ; z++)
                for (int x = 0; x < p.sizeX; x++)
                {
                    if (maps.Get(x, z) != a)
                        continue;
                    if ((x + 1 < p.sizeX && maps.Get(x + 1, z) == b) || (x > 0 && maps.Get(x - 1, z) == b)
                        || (z + 1 < p.sizeZ && maps.Get(x, z + 1) == b) || (z > 0 && maps.Get(x, z - 1) == b))
                        border.Add((x, z));
                }
                if (border.Count == 0)
                {
                    Assert.Fail($"map {a} và {b} không giáp nhau (seed {seed})");
                    return;
                }

                long best = -1;
                int bi = 0, bj = 0;
                for (int i = 0; i < border.Count; i++)
                for (int j = i + 1; j < border.Count; j++)
                {
                    long dx = border[j].x - border[i].x, dz = border[j].z - border[i].z;
                    long d = dx * dx + dz * dz;
                    if (d > best)
                    {
                        best = d;
                        bi = i;
                        bj = j;
                    }
                }
                var P = border[bi];
                var Q = border[bj];
                double len = Math.Sqrt(best);
                double qx = Q.x - P.x, qz = Q.z - P.z;
                double dev = 0;
                foreach (var R in border)
                {
                    double rx = R.x - P.x, rz = R.z - P.z;
                    double cross = Math.Abs(qx * rz - qz * rx) / len;
                    if (cross > dev)
                        dev = cross;
                }
                TestContext.WriteLine($"seed {seed}: cặp {a}-{b}: {border.Count} cột biên, dài {len:F1}, dev = {dev:F2}");
                Assert.GreaterOrEqual(len, 32.0, $"ranh giới {a}-{b} quá ngắn (seed {seed})");
                Assert.GreaterOrEqual(dev, 8.0, $"ranh giới {a}-{b} quá thẳng (seed {seed})");
            }
        }

        [Test]
        public void NoFlagsSetByGenerator()
        {
            var (w, _) = Get(20261013);
            foreach (ushort c in w.Cells)
            {
                Assert.IsFalse(Cell.IsStory(c));
                Assert.IsFalse(Cell.IsModified(c));
                Assert.AreEqual(0, Cell.GetReserved(c));
            }
        }
    }
}
