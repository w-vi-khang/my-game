using Game.Core;
using NUnit.Framework;

namespace Game.World.Tests
{
    public class TerrainGeneratorTests
    {
        static TerrainParams Params(uint seed = 20261013) => new TerrainParams { seed = seed };

        [Test]
        public void RunsWithBurst()
        {
            TerrainGenerator.Generate(Params(), out bool ranWithBurst);
            Assert.IsTrue(ranWithBurst, "Job sinh địa hình phải chạy bằng Burst (FloatMode.Deterministic)");
        }

        [Test]
        public void SameSeedSameHash()
        {
            ulong a = WorldHash.Compute(TerrainGenerator.Generate(Params()));
            ulong b = WorldHash.Compute(TerrainGenerator.Generate(Params()));
            Assert.AreEqual(a, b);
        }

        [Test]
        public void DifferentSeedDifferentHash()
        {
            ulong a = WorldHash.Compute(TerrainGenerator.Generate(Params(1)));
            ulong b = WorldHash.Compute(TerrainGenerator.Generate(Params(2)));
            Assert.AreNotEqual(a, b);
        }

        [Test]
        public void M1SizeIs64x32x64()
        {
            var w = TerrainGenerator.Generate(Params());
            Assert.AreEqual(new WorldSize(64, 32, 64), w.Size);
            Assert.AreEqual(32, w.Size.ChunkCount);
        }

        [Test]
        public void LayerRules([Values(20261013u, 7u, 123456789u)] uint seed)
        {
            var p = Params(seed);
            var w = TerrainGenerator.Generate(p);
            int waterCells = 0;
            for (int x = 0; x < p.sizeX; x++)
            for (int z = 0; z < p.sizeZ; z++)
            {
                for (int y = 0; y < p.bedrockLayers; y++)
                    Assert.AreEqual(BlockMaterial.Bedrock, w.GetMaterial(x, y, z), $"đá đáy ({x},{y},{z})");

                int s = w.SurfaceHeight(x, z);
                Assert.That(s, Is.InRange(p.bedrockLayers, p.maxSurface), $"mặt đất ({x},{z})");
                if (s < p.minSurface)
                    Assert.AreEqual(BlockMaterial.Water, w.GetMaterial(x, s + 1, z), $"dưới minSurface chỉ được ở lòng hồ ({x},{z})");

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
            Assert.Greater(waterCells, 0, "phải có ít nhất một hồ");
        }

        [Test]
        public void NoFlagsSetByGenerator()
        {
            foreach (ushort c in TerrainGenerator.Generate(Params()).Cells)
            {
                Assert.IsFalse(Cell.IsStory(c));
                Assert.IsFalse(Cell.IsModified(c));
                Assert.AreEqual(0, Cell.GetReserved(c));
            }
        }

        [Test]
        public void Version2HasOceanRingAroundIsland()
        {
            // 0009: vành biển quanh map — cột cách biên < islandMargin ở đúng đáy biển, nước phủ tới mặt nước; mép map dưới mặt nước.
            var p = new TerrainParams();
            Assert.AreEqual(2, p.generatorVersion);
            var w = TerrainGenerator.Generate(p);
            int seaFloor = p.waterLevel - p.seaFloorDepth, ring = 0;
            for (int z = 0; z < p.sizeZ; z++)
            for (int x = 0; x < p.sizeX; x++)
            {
                int edge = System.Math.Min(System.Math.Min(x, z), System.Math.Min(p.sizeX - 1 - x, p.sizeZ - 1 - z));
                if (edge >= p.islandMargin)
                    continue;
                ring++;
                Assert.AreEqual(seaFloor, w.SurfaceHeight(x, z), $"đáy biển ({x},{z})");
                Assert.AreEqual(BlockMaterial.Water, w.GetMaterial(x, p.waterLevel, z), $"nước ở mặt biển ({x},{z})");
                Assert.AreEqual(BlockMaterial.Empty, w.GetMaterial(x, p.waterLevel + 1, z), $"trên mặt biển ({x},{z})");
            }
            Assert.Greater(ring, 0);
        }
    }
}
