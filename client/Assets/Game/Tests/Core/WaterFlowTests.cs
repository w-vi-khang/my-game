using System.Collections.Generic;
using Game.Core;
using NUnit.Framework;

namespace Game.Core.Tests
{
    /// <summary>Nước chảy: nguồn, lan giới hạn, hố được lấp, mất nguồn thì rút (design/decisions/0040, bước 5e).</summary>
    public class WaterFlowTests
    {
        static WorldData Flat(int solidTop, BlockMaterial m = BlockMaterial.Stone)
        {
            var w = new WorldData(new WorldSize(16, 16, 16));
            for (int z = 0; z < 16; z++)
            for (int x = 0; x < 16; x++)
            {
                w.Set(x, 0, z, Cell.Make(BlockMaterial.Bedrock));
                for (int y = 1; y <= solidTop; y++)
                    w.Set(x, y, z, Cell.Make(m));
            }
            return w;
        }

        static WorldEditor Editor(WorldData w, int seaLevel = -1)
        {
            var bag = new PlayerBag();
            bag.Add(BlockMaterial.Stone, 100000);
            return new WorldEditor(w, bag, 1, seaLevel);
        }

        static void Settle(WorldEditor e)
        {
            e.SettleWater(10000);
            Assert.IsFalse(e.HasPendingWater);
        }

        static void Dig(WorldEditor e, int x, int y, int z) =>
            Assert.AreEqual(EditResult.Ok, e.TryDig(x, y, z), $"dig ({x},{y},{z})");

        static void Place(WorldEditor e, int x, int y, int z) =>
            Assert.AreEqual(EditResult.Ok, e.TryPlace(x, y, z, BlockMaterial.Stone), $"place ({x},{y},{z})");

        static bool IsWater(WorldData w, int x, int y, int z) => w.GetMaterial(x, y, z) == BlockMaterial.Water;

        static WorldData PondWorld()
        {
            var w = Flat(3);
            for (int z = 4; z <= 7; z++)
            for (int x = 4; x <= 7; x++)
                w.Set(x, 3, z, Cell.Make(BlockMaterial.Water, Cell.MaxWaterLevel));
            return w;
        }

        /// <summary>Hành lang: nền đá y 1..5, tường đá y = 6 tại z = 7 và 9, nguồn (0,6,8), đá (1,6,8).</summary>
        static WorldData Corridor()
        {
            var w = Flat(5);
            for (int x = 0; x < 16; x++)
            {
                w.Set(x, 6, 7, Cell.Make(BlockMaterial.Stone));
                w.Set(x, 6, 9, Cell.Make(BlockMaterial.Stone));
            }
            w.Set(0, 6, 8, Cell.Make(BlockMaterial.Water, Cell.MaxWaterLevel));
            w.Set(1, 6, 8, Cell.Make(BlockMaterial.Stone));
            return w;
        }

        [Test]
        public void PitNextToPondFills()
        {
            var w = PondWorld();
            var e = Editor(w);
            Dig(e, 8, 3, 5);
            Dig(e, 8, 2, 5);
            Settle(e);
            Assert.IsTrue(IsWater(w, 8, 3, 5));
            Assert.IsTrue(IsWater(w, 8, 2, 5));
            ushort top = w.Get(8, 3, 5);
            Assert.IsTrue(Cell.IsFlowing(top));
            Assert.AreEqual(7, Cell.GetWaterLevel(top));
            Assert.IsTrue(Cell.IsFlowing(w.Get(8, 2, 5)));
            for (int z = 4; z <= 7; z++)
            for (int x = 4; x <= 7; x++)
                Assert.AreEqual(Cell.Make(BlockMaterial.Water, Cell.MaxWaterLevel), w.Get(x, 3, z), $"source ({x},3,{z})");
        }

        [Test]
        public void WideDeepPitFills()
        {
            var w = PondWorld();
            var e = Editor(w);
            for (int y = 3; y >= 2; y--)
            for (int z = 4; z <= 6; z++)
            for (int x = 8; x <= 10; x++)
                Dig(e, x, y, z);
            Settle(e);
            for (int y = 2; y <= 3; y++)
            for (int z = 4; z <= 6; z++)
            for (int x = 8; x <= 10; x++)
                Assert.IsTrue(IsWater(w, x, y, z), $"({x},{y},{z})");
        }

        [Test]
        public void SpreadStopsAfterN()
        {
            var w = Corridor();
            var e = Editor(w);
            Dig(e, 1, 6, 8);
            Settle(e);
            for (int x = 1; x <= 7; x++)
            {
                Assert.IsTrue(IsWater(w, x, 6, 8), $"x={x}");
                Assert.IsTrue(Cell.IsFlowing(w.Get(x, 6, 8)));
                Assert.AreEqual(8 - x, Cell.GetWaterLevel(w.Get(x, 6, 8)), $"level x={x}");
            }
            Assert.AreEqual(BlockMaterial.Empty, w.GetMaterial(8, 6, 8));

            var w3 = Corridor();
            var e3 = Editor(w3);
            e3.FlowMaxDistance = 3;
            Dig(e3, 1, 6, 8);
            Settle(e3);
            for (int x = 1; x <= 3; x++)
                Assert.AreEqual(4 - x, Cell.GetWaterLevel(w3.Get(x, 6, 8)), $"N=3 level x={x}");
            Assert.IsTrue(IsWater(w3, 3, 6, 8));
            Assert.AreEqual(BlockMaterial.Empty, w3.GetMaterial(4, 6, 8));
        }

        [Test]
        public void SpreadAdvancesOneCellPerTick()
        {
            var w = Corridor();
            var e = Editor(w);
            Dig(e, 1, 6, 8);
            for (int k = 1; k <= 7; k++)
            {
                e.StepWater();
                Assert.IsTrue(IsWater(w, k, 6, 8), $"tick {k}: ({k})");
                Assert.AreEqual(BlockMaterial.Empty, w.GetMaterial(k + 1, 6, 8), $"tick {k}: ({k + 1})");
            }
        }

        [Test]
        public void FallsThenSpreadsFullAgain()
        {
            var w = Flat(1);
            for (int z = 0; z <= 2; z++)
            for (int x = 0; x <= 2; x++)
            for (int y = 2; y <= 10; y++)
                w.Set(x, y, z, Cell.Make(BlockMaterial.Stone));
            w.Set(1, 11, 1, Cell.Make(BlockMaterial.Water, Cell.MaxWaterLevel));
            w.Set(1, 11, 2, Cell.Make(BlockMaterial.Stone));
            for (int z = 0; z <= 2; z++)
            {
                w.Set(0, 11, z, Cell.Make(BlockMaterial.Stone));
                w.Set(2, 11, z, Cell.Make(BlockMaterial.Stone));
            }
            w.Set(1, 11, 0, Cell.Make(BlockMaterial.Stone));
            for (int z = 3; z <= 15; z++)
            {
                w.Set(0, 2, z, Cell.Make(BlockMaterial.Stone));
                w.Set(2, 2, z, Cell.Make(BlockMaterial.Stone));
            }
            var e = Editor(w);
            Dig(e, 1, 11, 2);
            Settle(e);

            Assert.IsTrue(IsWater(w, 1, 11, 2));
            Assert.AreEqual(7, Cell.GetWaterLevel(w.Get(1, 11, 2)));
            Assert.IsTrue(IsWater(w, 1, 11, 3));
            Assert.AreEqual(6, Cell.GetWaterLevel(w.Get(1, 11, 3)));
            Assert.IsFalse(Cell.IsFalling(w.Get(1, 11, 3)));
            for (int y = 2; y <= 10; y++)
                Assert.IsTrue(Cell.IsFalling(w.Get(1, y, 3)), $"falling y={y}");
            for (int y = 4; y <= 10; y++)
            {
                Assert.AreEqual(BlockMaterial.Empty, w.GetMaterial(0, y, 3), $"(0,{y},3)");
                Assert.AreEqual(BlockMaterial.Empty, w.GetMaterial(2, y, 3), $"(2,{y},3)");
            }
            for (int d = 1; d <= 7; d++)
                Assert.IsTrue(IsWater(w, 1, 2, 3 + d), $"d={d}");
            Assert.AreEqual(BlockMaterial.Empty, w.GetMaterial(1, 2, 3 + 8));
        }

        [Test]
        public void BlockingSourceDrains()
        {
            var w = Corridor();
            var e = Editor(w);
            Dig(e, 1, 6, 8);
            Settle(e);
            Place(e, 1, 6, 8);
            Settle(e);
            for (int x = 2; x <= 7; x++)
                Assert.AreEqual(BlockMaterial.Empty, w.GetMaterial(x, 6, 8), $"x={x}");
            Assert.IsTrue(IsWater(w, 0, 6, 8));
        }

        [Test]
        public void RemovingSourceDrains()
        {
            var w = PondWorld();
            var e = Editor(w);
            Dig(e, 8, 3, 5);
            Dig(e, 8, 2, 5);
            Settle(e);
            for (int z = 4; z <= 7; z++)
            for (int x = 4; x <= 7; x++)
                Place(e, x, 3, z);
            Settle(e);
            Assert.AreEqual(BlockMaterial.Empty, w.GetMaterial(8, 3, 5));
            Assert.AreEqual(BlockMaterial.Empty, w.GetMaterial(8, 2, 5));
        }

        [Test]
        public void DrainCountsDownGradually()
        {
            var w = Corridor();
            var e = Editor(w);
            Dig(e, 1, 6, 8);
            Settle(e);
            Place(e, 1, 6, 8);
            e.StepWater();
            int left = 0;
            for (int x = 2; x <= 7; x++)
                if (IsWater(w, x, 6, 8)) left++;
            Assert.GreaterOrEqual(left, 1);
        }

        [Test]
        public void PlacingBlockStopsFlowFront()
        {
            var w = Corridor();
            var e = Editor(w);
            Dig(e, 1, 6, 8);
            e.StepWater();
            e.StepWater();
            Place(e, 4, 6, 8);
            Settle(e);
            for (int x = 1; x <= 3; x++)
                Assert.IsTrue(IsWater(w, x, 6, 8), $"x={x}");
            for (int x = 5; x <= 7; x++)
                Assert.AreEqual(BlockMaterial.Empty, w.GetMaterial(x, 6, 8), $"x={x}");
        }

        [Test]
        public void CellChangedOnlyOnMaterialChange()
        {
            var w = Corridor();
            var e = Editor(w);
            int events = 0;
            e.CellChanged += (x, y, z) => events++;
            Dig(e, 1, 6, 8);
            Settle(e);
            Assert.AreEqual(1 + 7, events);
        }

        static List<ushort> RandomEdits(WorldData w, WorldEditor e)
        {
            var rng = new System.Random(11);
            for (int i = 0; i < 300; i++)
            {
                int x = rng.Next(16), y = 3 + rng.Next(6), z = rng.Next(16);
                if (rng.Next(2) == 0) e.TryDig(x, y, z);
                else e.TryPlace(x, y, z, BlockMaterial.Stone);
                e.StepWater();
            }
            var all = new List<ushort>();
            for (int z = 0; z < 16; z++)
            for (int y = 0; y < 16; y++)
            for (int x = 0; x < 16; x++)
                all.Add(w.Get(x, y, z));
            return all;
        }

        static WorldData RandomWorld()
        {
            var w = Flat(6, BlockMaterial.Dirt);
            for (int z = 6; z <= 9; z++)
            for (int x = 6; x <= 9; x++)
                w.Set(x, 6, z, Cell.Make(BlockMaterial.Water, Cell.MaxWaterLevel));
            return w;
        }

        [Test]
        public void FlowIsDeterministic()
        {
            var w1 = RandomWorld();
            var w2 = RandomWorld();
            var a = RandomEdits(w1, Editor(w1));
            var b = RandomEdits(w2, Editor(w2));
            CollectionAssert.AreEqual(a, b);
        }

        [Test]
        public void FlowSettlesWithinBound()
        {
            var w = RandomWorld();
            var e = Editor(w);
            RandomEdits(w, e);
            e.SettleWater(10000);
            Assert.IsFalse(e.HasPendingWater);
        }

        static WorldData Coast()
        {
            const int S = 5;
            var w = new WorldData(new WorldSize(16, 16, 16));
            for (int z = 0; z < 16; z++)
            for (int x = 0; x < 16; x++)
            {
                w.Set(x, 0, z, Cell.Make(BlockMaterial.Bedrock));
                for (int y = 1; y <= 8; y++)
                {
                    if (x <= 3)
                        w.Set(x, y, z, y <= S ? Cell.Make(BlockMaterial.Water, Cell.MaxWaterLevel) : Cell.Make(BlockMaterial.Empty));
                    else
                        w.Set(x, y, z, Cell.Make(BlockMaterial.Stone));
                }
            }
            return w;
        }

        [Test]
        public void SeaFloodStillMakesSources()
        {
            var w = Coast();
            var e = Editor(w, 5);
            Dig(e, 4, 5, 7);
            Dig(e, 5, 5, 7);
            Dig(e, 6, 5, 7);
            Settle(e);
            for (int x = 4; x <= 6; x++)
            {
                Assert.IsTrue(IsWater(w, x, 5, 7), $"x={x}");
                Assert.IsTrue(Cell.IsWaterSource(w.Get(x, 5, 7)), $"source x={x}");
            }
            Place(e, 4, 5, 7);
            Settle(e);
            Assert.IsTrue(IsWater(w, 5, 5, 7));
            Assert.IsTrue(IsWater(w, 6, 5, 7));
        }

        [Test]
        public void FlowingWaterBelowSeaLevelDoesNotStartSeaFlood()
        {
            var w = Flat(7);
            w.Set(4, 8, 4, Cell.Make(BlockMaterial.Water, Cell.MaxWaterLevel));
            w.Set(5, 8, 4, Cell.Make(BlockMaterial.Stone));
            var e = Editor(w, 5);
            for (int y = 8; y >= 4; y--)
                Dig(e, 5, y, 4);
            Settle(e);
            Assert.IsTrue(IsWater(w, 5, 4, 4));
            Assert.IsFalse(Cell.IsWaterSource(w.Get(5, 4, 4)));
            Dig(e, 6, 4, 4);
            Settle(e);
            Assert.IsTrue(IsWater(w, 6, 4, 4));
            Assert.IsTrue(Cell.IsFlowing(w.Get(6, 4, 4)));
            Assert.AreEqual(7, Cell.GetWaterLevel(w.Get(6, 4, 4)));
        }
    }
}
