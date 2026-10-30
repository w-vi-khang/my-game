using System.Collections.Generic;
using Game.Core;
using NUnit.Framework;

namespace Game.Core.Tests
{
    /// <summary>Nước chảy theo cây cha–con: mức 0–5, tăng mức bởi tường, xoá dây chuyền + tái tạo (design/decisions/0043, bước 5h).</summary>
    public class WaterFlowTests
    {
        static WorldData Flat(int solidTop, BlockMaterial m = BlockMaterial.Stone, int size = 16)
        {
            var w = new WorldData(new WorldSize(size, 16, size));
            for (int z = 0; z < size; z++)
            for (int x = 0; x < size; x++)
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

        /// <summary>Sàn rộng: nền đá y 1..5, nguồn (0,6,8), đá (1,6,8), không tường.</summary>
        static WorldData OpenFloor(int size = 16)
        {
            var w = Flat(5, size: size);
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
            Assert.AreEqual(5, Cell.GetWaterLevel(top));
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

        /// <summary>0044 + 0045: hố rộng 2 lớp cạnh ao — lớp dưới (thấp hơn mặt ao) đầy mức 5; lớp ngang mặt ao là vùng kín trong BasinFillRadius → cũng đầy mức 5, không dâng.</summary>
        [Test]
        public void WideHoleBothLayersFill()
        {
            var w = PondWorld();
            var e = Editor(w);
            for (int y = 3; y >= 2; y--)
            for (int z = 2; z <= 9; z++)
            for (int x = 8; x <= 15; x++)
                Dig(e, x, y, z);
            Settle(e);
            for (int y = 2; y <= 3; y++)
            for (int z = 2; z <= 9; z++)
            for (int x = 8; x <= 15; x++)
            {
                Assert.IsTrue(IsWater(w, x, y, z), $"({x},{y},{z})");
                Assert.AreEqual(5, Lv(w, x, y, z), $"level ({x},{y},{z})");
            }
            Assert.IsTrue(Cell.IsFalling(w.Get(8, 2, 5)));
        }

        [Test]
        public void SpreadStopsAfterN()
        {
            var w = OpenFloor();
            var e = Editor(w);
            e.BasinFillRadius = 4;
            Dig(e, 1, 6, 8);
            Settle(e);
            for (int x = 1; x <= 5; x++)
            {
                Assert.IsTrue(IsWater(w, x, 6, 8), $"x={x}");
                Assert.IsTrue(Cell.IsFlowing(w.Get(x, 6, 8)));
                Assert.AreEqual(5 - x, Cell.GetWaterLevel(w.Get(x, 6, 8)), $"level x={x}");
            }
            Assert.AreEqual(BlockMaterial.Empty, w.GetMaterial(6, 6, 8));

            var w3 = OpenFloor();
            var e3 = Editor(w3);
            e3.MaxLevel = 3;
            e3.BasinFillRadius = 4;
            Dig(e3, 1, 6, 8);
            Settle(e3);
            for (int x = 1; x <= 3; x++)
                Assert.AreEqual(3 - x, Cell.GetWaterLevel(w3.Get(x, 6, 8)), $"N=3 level x={x}");
            Assert.IsTrue(IsWater(w3, 3, 6, 8));
            Assert.AreEqual(BlockMaterial.Empty, w3.GetMaterial(4, 6, 8));
        }

        [Test]
        public void SpreadAdvancesOneCellPerTick()
        {
            var w = OpenFloor();
            var e = Editor(w);
            Dig(e, 1, 6, 8);
            for (int k = 1; k <= 5; k++)
            {
                e.StepWater();
                Assert.IsTrue(IsWater(w, k, 6, 8), $"tick {k}: ({k})");
                Assert.AreEqual(BlockMaterial.Empty, w.GetMaterial(k + 1, 6, 8), $"tick {k}: ({k + 1})");
            }
        }

        [Test]
        public void FallsThenSpreadsFullAgain()
        {
            var w = Flat(1, size: 48);
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
            var e = Editor(w);
            Dig(e, 1, 11, 2);
            Settle(e);

            Assert.IsTrue(IsWater(w, 1, 11, 2));
            Assert.AreEqual(5, Cell.GetWaterLevel(w.Get(1, 11, 2)));
            Assert.IsTrue(IsWater(w, 1, 11, 3));
            Assert.AreEqual(4, Cell.GetWaterLevel(w.Get(1, 11, 3)));
            Assert.IsFalse(Cell.IsFalling(w.Get(1, 11, 3)));
            for (int y = 2; y <= 10; y++)
                Assert.IsTrue(Cell.IsFalling(w.Get(1, y, 3)), $"falling y={y}");
            for (int y = 4; y <= 10; y++)
            {
                Assert.AreEqual(BlockMaterial.Empty, w.GetMaterial(0, y, 3), $"(0,{y},3)");
                Assert.AreEqual(BlockMaterial.Empty, w.GetMaterial(2, y, 3), $"(2,{y},3)");
            }
            // 0044: sàn y = 2 thấp hơn nguồn, trong basinFillRadius → đầy mức 5.
            for (int z = 3; z <= 15; z++)
            for (int x = 0; x <= 15; x++)
            {
                Assert.IsTrue(IsWater(w, x, 2, z), $"floor ({x},{z})");
                Assert.AreEqual(5, Lv(w, x, 2, z), $"floor level ({x},{z})");
            }
        }

        [Test]
        public void RemovingOpenSourceDrains()
        {
            var w = OpenFloor();
            var e = Editor(w);
            Dig(e, 1, 6, 8);
            Settle(e);
            Place(e, 0, 6, 8);
            Settle(e);
            for (int x = 1; x < 16; x++)
                Assert.AreEqual(BlockMaterial.Empty, w.GetMaterial(x, 6, 8), $"x={x}");
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
            var w = OpenFloor();
            var e = Editor(w);
            Dig(e, 1, 6, 8);
            Settle(e);
            Place(e, 0, 6, 8);
            e.StepWater();
            int left = 0;
            for (int x = 1; x <= 7; x++)
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
            Assert.AreEqual(1 + 15, events);
        }

        [Test]
        public void ChannelFlowsFullLength()
        {
            var w = Corridor();
            var e = Editor(w);
            Dig(e, 1, 6, 8);
            Settle(e);
            for (int x = 1; x <= 15; x++)
            {
                Assert.IsTrue(IsWater(w, x, 6, 8), $"x={x}");
                Assert.AreEqual(5, Cell.GetWaterLevel(w.Get(x, 6, 8)), $"level x={x}");
            }
        }

        [Test]
        public void ChannelOpensIntoWideArea()
        {
            var w = Flat(5);
            for (int x = 0; x <= 5; x++)
            {
                w.Set(x, 6, 7, Cell.Make(BlockMaterial.Stone));
                w.Set(x, 6, 9, Cell.Make(BlockMaterial.Stone));
            }
            w.Set(0, 6, 8, Cell.Make(BlockMaterial.Water, Cell.MaxWaterLevel));
            w.Set(1, 6, 8, Cell.Make(BlockMaterial.Stone));
            var e = Editor(w);
            e.BasinFillRadius = 4;
            Dig(e, 1, 6, 8);
            Settle(e);
            for (int x = 1; x <= 5; x++)
                Assert.AreEqual(5, Cell.GetWaterLevel(w.Get(x, 6, 8)), $"narrow x={x}");
            for (int x = 6; x <= 10; x++)
                Assert.AreEqual(10 - x, Cell.GetWaterLevel(w.Get(x, 6, 8)), $"wide x={x}");
            Assert.AreEqual(BlockMaterial.Empty, w.GetMaterial(11, 6, 8));
        }

        [Test]
        public void BlockingChannelDrainsDownstream()
        {
            var w = Corridor();
            var e = Editor(w);
            Dig(e, 1, 6, 8);
            Settle(e);
            Place(e, 8, 6, 8);
            Settle(e);
            for (int x = 9; x <= 15; x++)
                Assert.AreEqual(BlockMaterial.Empty, w.GetMaterial(x, 6, 8), $"x={x}");
            for (int x = 1; x <= 7; x++)
            {
                Assert.IsTrue(IsWater(w, x, 6, 8), $"x={x}");
                Assert.AreEqual(5, Cell.GetWaterLevel(w.Get(x, 6, 8)), $"level x={x}");
            }
        }

        static bool OnLoop(int x, int z) =>
            x >= 4 && x <= 8 && z >= 4 && z <= 8 && (x == 4 || x == 8 || z == 4 || z == 8);

        [Test]
        public void ChannelLoopDrainsWhenSourceRemoved()
        {
            var w = Flat(5);
            for (int z = 2; z <= 10; z++)
            for (int x = 0; x < 16; x++)
                w.Set(x, 6, z, Cell.Make(BlockMaterial.Stone));
            for (int z = 4; z <= 8; z++)
            for (int x = 4; x <= 8; x++)
                if (OnLoop(x, z) && !(x == 4 && z == 4))
                    w.Set(x, 6, z, Cell.Make(BlockMaterial.Empty));
            w.Set(4, 6, 3, Cell.Make(BlockMaterial.Water, Cell.MaxWaterLevel));
            var e = Editor(w);
            Dig(e, 4, 6, 4);
            Settle(e);
            for (int z = 4; z <= 8; z++)
            for (int x = 4; x <= 8; x++)
                if (OnLoop(x, z))
                    Assert.IsTrue(IsWater(w, x, 6, z), $"loop ({x},{z})");
            Place(e, 4, 6, 3);
            Settle(e);
            for (int z = 4; z <= 8; z++)
            for (int x = 4; x <= 8; x++)
                if (OnLoop(x, z))
                    Assert.AreEqual(BlockMaterial.Empty, w.GetMaterial(x, 6, z), $"loop ({x},{z})");
        }

        [Test]
        public void WaterfallIntoPondDoesNotSpill()
        {
            var w = PondWorld();
            for (int y = 4; y <= 8; y++)
            for (int z = 4; z <= 6; z++)
            for (int x = 9; x <= 12; x++)
                w.Set(x, y, z, Cell.Make(BlockMaterial.Stone));
            w.Set(11, 8, 5, Cell.Make(BlockMaterial.Water, Cell.MaxWaterLevel));
            w.Set(10, 8, 5, Cell.Make(BlockMaterial.Empty));
            w.Set(8, 7, 5, Cell.Make(BlockMaterial.Stone));
            w.Set(8, 8, 4, Cell.Make(BlockMaterial.Stone));
            w.Set(8, 8, 6, Cell.Make(BlockMaterial.Stone));
            var e = Editor(w);
            e.WakeWater(11, 8, 5);
            Dig(e, 9, 8, 5);
            Settle(e);
            for (int y = 4; y <= 7; y++)
            {
                Assert.IsTrue(IsWater(w, 7, y, 5), $"fall y={y}");
                Assert.IsTrue(Cell.IsFalling(w.Get(7, y, 5)), $"falling y={y}");
            }
            Assert.IsTrue(IsWater(w, 8, 8, 5));
            Assert.IsTrue(IsWater(w, 7, 8, 5));
            for (int z = 0; z < 16; z++)
            for (int x = 0; x < 16; x++)
                if (!(x == 7 && z == 5) && !(x >= 4 && x <= 7 && z >= 4 && z <= 7) && !(x >= 9 && x <= 12 && z >= 4 && z <= 6))
                    Assert.IsFalse(IsWater(w, x, 4, z), $"spill ({x},4,{z})");
            Assert.IsFalse(IsWater(w, 6, 4, 5));
            Assert.IsFalse(IsWater(w, 7, 4, 4));
            Assert.IsFalse(IsWater(w, 7, 4, 6));
            Assert.IsFalse(IsWater(w, 8, 4, 5));
            for (int z = 4; z <= 7; z++)
            for (int x = 4; x <= 7; x++)
                Assert.IsTrue(Cell.IsWaterSource(w.Get(x, 3, z)), $"pond ({x},3,{z})");
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
            Assert.AreEqual(5, Cell.GetWaterLevel(w.Get(6, 4, 4)));
        }

        static int Node(WorldEditor e, int x, int y, int z)
        {
            Assert.IsTrue(e.TryGetFlowNode(x, y, z, out int id, out _), $"node ({x},{y},{z})");
            return id;
        }

        static int ParentOf(WorldEditor e, int x, int y, int z)
        {
            Assert.IsTrue(e.TryGetFlowNode(x, y, z, out _, out int p), $"node ({x},{y},{z})");
            return p;
        }

        static int Lv(WorldData w, int x, int y, int z) => Cell.GetWaterLevel(w.Get(x, y, z));

        /// <summary>World 32×16×16: đá y 0..1 (y = 0 Bedrock), cột đá (0, 2..8, 8), nguồn (0, 9, 8) — nước tràn 3 hướng rồi rơi xuống sàn y = 2.</summary>
        static WorldData BasinWorld()
        {
            var w = new WorldData(new WorldSize(32, 16, 16));
            for (int z = 0; z < 16; z++)
            for (int x = 0; x < 32; x++)
            {
                w.Set(x, 0, z, Cell.Make(BlockMaterial.Bedrock));
                w.Set(x, 1, z, Cell.Make(BlockMaterial.Stone));
            }
            for (int y = 2; y <= 8; y++)
                w.Set(0, y, 8, Cell.Make(BlockMaterial.Stone));
            w.Set(0, 9, 8, Cell.Make(BlockMaterial.Water, Cell.MaxWaterLevel));
            return w;
        }

        static void AssertBasin(WorldData w, int r)
        {
            for (int z = System.Math.Max(0, 8 - r); z <= System.Math.Min(15, 8 + r); z++)
            for (int x = 0; x <= System.Math.Min(r, 31); x++)
            {
                if (x == 0 && z == 8) continue;
                Assert.IsTrue(IsWater(w, x, 2, z), $"in ({x},{z})");
                Assert.AreEqual(5, Lv(w, x, 2, z), $"in level ({x},{z})");
            }
            for (int i = 0; i < 5; i++)
            {
                int x = r + 1 + i;
                if (x > 31) break;
                Assert.IsTrue(IsWater(w, x, 2, 8), $"edge x={x}");
                Assert.IsFalse(Cell.IsFalling(w.Get(x, 2, 8)), $"edge falling x={x}");
                Assert.AreEqual(4 - i, Lv(w, x, 2, 8), $"edge level x={x}");
            }
            for (int z = 0; z < 16; z++)
            for (int x = r + 6; x < 32; x++)
                Assert.AreEqual(BlockMaterial.Empty, w.GetMaterial(x, 2, z), $"out ({x},{z})");
        }

        /// <summary>0044: vùng thấp rộng dưới thác chỉ ngập mức 5 trong basinFillRadius = 16 ô ngang từ nguồn; ngoài đó lan giảm như 0043.</summary>
        [Test]
        public void BasinFillsWithinRadius()
        {
            var w = BasinWorld();
            var e = Editor(w);
            e.WakeWater(0, 9, 8);
            Settle(e);
            AssertBasin(w, 16);
        }

        [Test]
        public void BasinFillRadiusIsTunable()
        {
            var w = BasinWorld();
            var e = Editor(w);
            e.BasinFillRadius = 4;
            e.WakeWater(0, 9, 8);
            Settle(e);
            AssertBasin(w, 4);
            Assert.Throws<System.ArgumentOutOfRangeException>(() => e.BasinFillRadius = -1);
            Assert.Throws<System.ArgumentOutOfRangeException>(() => e.BasinFillRadius = 65);
        }

        static List<ushort> RunBasin()
        {
            var w = BasinWorld();
            var e = Editor(w);
            e.WakeWater(0, 9, 8);
            Settle(e);
            var all = new List<ushort>();
            for (int z = 0; z < 16; z++)
            for (int y = 0; y < 16; y++)
            for (int x = 0; x < 32; x++)
                all.Add(w.Get(x, y, z));
            return all;
        }

        [Test]
        public void BasinFillIsDeterministic()
        {
            CollectionAssert.AreEqual(RunBasin(), RunBasin());
        }

        [Test]
        public void DiamondLevelsOnOpenFloor()
        {
            var w = OpenFloor();
            var e = Editor(w);
            e.BasinFillRadius = 4;
            Dig(e, 1, 6, 8);
            Settle(e);
            for (int z = 0; z < 16; z++)
            for (int x = 0; x < 16; x++)
            {
                int d = x + System.Math.Abs(z - 8);
                if (d == 0) continue;
                if (d <= 5)
                {
                    Assert.IsTrue(IsWater(w, x, 6, z), $"({x},{z})");
                    Assert.AreEqual(5 - d, Lv(w, x, 6, z), $"level ({x},{z})");
                }
                else
                    Assert.AreEqual(BlockMaterial.Empty, w.GetMaterial(x, 6, z), $"({x},{z})");
            }
        }

        [Test]
        public void ChannelDrainsOneCellPerTick()
        {
            var w = Corridor();
            var e = Editor(w);
            Dig(e, 1, 6, 8);
            Settle(e);
            Place(e, 1, 6, 8);
            for (int k = 1; k <= 13; k++)
            {
                e.StepWater();
                for (int x = 2; x <= k + 1; x++)
                    Assert.AreEqual(BlockMaterial.Empty, w.GetMaterial(x, 6, 8), $"k={k} x={x}");
                for (int x = k + 2; x <= 15; x++)
                    Assert.IsTrue(IsWater(w, x, 6, 8), $"k={k} x={x}");
            }
            Settle(e);
            for (int x = 2; x <= 15; x++)
                Assert.AreEqual(BlockMaterial.Empty, w.GetMaterial(x, 6, 8), $"x={x}");
        }

        [Test]
        public void RegenerateFromSecondSource()
        {
            var w = Corridor();
            w.Set(15, 6, 8, Cell.Make(BlockMaterial.Water, Cell.MaxWaterLevel));
            w.Set(14, 6, 8, Cell.Make(BlockMaterial.Stone));
            var e = Editor(w);
            Dig(e, 1, 6, 8);
            Settle(e);
            Dig(e, 14, 6, 8);
            Settle(e);
            Assert.AreEqual(5, Lv(w, 14, 6, 8));
            Assert.AreEqual(Node(e, 13, 6, 8), ParentOf(e, 14, 6, 8));
            Place(e, 1, 6, 8);
            Settle(e);
            for (int x = 2; x <= 14; x++)
            {
                Assert.IsTrue(IsWater(w, x, 6, 8), $"x={x}");
                Assert.AreEqual(5, Lv(w, x, 6, 8), $"level x={x}");
            }
            Assert.AreEqual(Node(e, 15, 6, 8), ParentOf(e, 14, 6, 8));
            Assert.AreEqual(-1, ParentOf(e, 15, 6, 8));
            Assert.AreEqual(Node(e, 14, 6, 8), ParentOf(e, 13, 6, 8));
        }

        [Test]
        public void WallBoostOnPlaceAndPropagate()
        {
            var w = Flat(5);
            for (int x = 0; x < 16; x++)
                w.Set(x, 6, 7, Cell.Make(BlockMaterial.Stone));
            w.Set(0, 6, 8, Cell.Make(BlockMaterial.Water, Cell.MaxWaterLevel));
            w.Set(0, 6, 9, Cell.Make(BlockMaterial.Stone));
            w.Set(1, 6, 8, Cell.Make(BlockMaterial.Stone));
            var e = Editor(w);
            e.BasinFillRadius = 4;
            Dig(e, 1, 6, 8);
            Settle(e);
            Assert.AreEqual(3, Lv(w, 2, 6, 8));
            Assert.AreEqual(2, Lv(w, 3, 6, 8));
            Assert.AreEqual(1, Lv(w, 3, 6, 9));
            Assert.AreEqual(Node(e, 2, 6, 9), ParentOf(e, 3, 6, 9));
            Assert.AreEqual(Node(e, 1, 6, 8), ParentOf(e, 2, 6, 8));
            var surface = new HashSet<(int, int, int)>();
            e.WaterSurfaceChanged += (x, y, z) => surface.Add((x, y, z));
            Place(e, 2, 6, 9);
            Assert.AreEqual(5, Lv(w, 2, 6, 8));
            Assert.AreEqual(Node(e, 1, 6, 8), ParentOf(e, 2, 6, 8));
            Assert.AreEqual(4, Lv(w, 3, 6, 8));
            Assert.AreEqual(Node(e, 2, 6, 8), ParentOf(e, 3, 6, 8));
            Assert.AreEqual(3, Lv(w, 3, 6, 9));
            Assert.AreEqual(Node(e, 3, 6, 8), ParentOf(e, 3, 6, 9));
            Assert.AreEqual(4, Lv(w, 1, 6, 8));
            Assert.IsTrue(surface.Contains((2, 6, 8)));
            Assert.IsTrue(surface.Contains((3, 6, 8)));
            Assert.IsTrue(surface.Contains((3, 6, 9)));
            Assert.DoesNotThrow(() => e.SettleWater(10000));
            Assert.IsFalse(e.HasPendingWater);
        }

        [Test]
        public void FallingCellCreatesChildBelowOnly()
        {
            var w = Flat(3);
            for (int x = 0; x < 16; x++)
            for (int y = 4; y <= 5; y++)
                w.Set(x, y, 8, Cell.Make(BlockMaterial.Stone));
            w.Set(0, 6, 8, Cell.Make(BlockMaterial.Water, Cell.MaxWaterLevel));
            w.Set(0, 6, 9, Cell.Make(BlockMaterial.Stone));
            w.Set(1, 6, 8, Cell.Make(BlockMaterial.Stone));
            var e = Editor(w);
            Dig(e, 1, 6, 8);
            Settle(e);
            Assert.IsTrue(IsWater(w, 1, 6, 9));
            Assert.IsFalse(Cell.IsFalling(w.Get(1, 6, 9)));
            for (int y = 4; y <= 5; y++)
            {
                Assert.IsTrue(Cell.IsFalling(w.Get(1, y, 9)), $"falling y={y}");
                Assert.AreEqual(5, Lv(w, 1, y, 9), $"level y={y}");
            }
            Assert.AreEqual(BlockMaterial.Empty, w.GetMaterial(1, 6, 10));
        }

        [Test]
        public void CascadeRemovesSubtreeOnly()
        {
            var w = Corridor();
            w.Set(5, 6, 9, Cell.Make(BlockMaterial.Empty));
            var e = Editor(w);
            Dig(e, 1, 6, 8);
            Settle(e);
            Assert.IsTrue(IsWater(w, 5, 6, 9));
            Assert.AreEqual(5, Lv(w, 5, 6, 9));
            Assert.IsTrue(IsWater(w, 5, 6, 10));
            Place(e, 5, 6, 9);
            Settle(e);
            for (int x = 1; x <= 15; x++)
            {
                Assert.IsTrue(IsWater(w, x, 6, 8), $"x={x}");
                Assert.AreEqual(5, Lv(w, x, 6, 8), $"level x={x}");
            }
            for (int z = 10; z < 16; z++)
            for (int x = 0; x < 16; x++)
                Assert.AreEqual(BlockMaterial.Empty, w.GetMaterial(x, 6, z), $"({x},6,{z})");
            Place(e, 3, 6, 8);
            Settle(e);
            for (int x = 4; x <= 15; x++)
                Assert.AreEqual(BlockMaterial.Empty, w.GetMaterial(x, 6, 8), $"x={x}");
            for (int x = 1; x <= 2; x++)
                Assert.IsTrue(IsWater(w, x, 6, 8), $"x={x}");
        }

        /// <summary>Mọi cặp ô nước kề ngang ở tầng y lệch mức ≤ 1 (nguồn tính MaxLevel = 5).</summary>
        static void AssertSmooth(WorldData w, int y)
        {
            int Level(int x, int z)
            {
                ushort c = w.Get(x, y, z);
                return Cell.IsWaterSource(c) ? 5 : Cell.GetWaterLevel(c);
            }
            for (int z = 0; z < w.Size.Z; z++)
            for (int x = 0; x < w.Size.X; x++)
            {
                if (!IsWater(w, x, y, z)) continue;
                if (x + 1 < w.Size.X && IsWater(w, x + 1, y, z))
                    Assert.LessOrEqual(System.Math.Abs(Level(x, z) - Level(x + 1, z)), 1, $"({x},{y},{z})-({x + 1},{y},{z})");
                if (z + 1 < w.Size.Z && IsWater(w, x, y, z + 1))
                    Assert.LessOrEqual(System.Math.Abs(Level(x, z) - Level(x, z + 1)), 1, $"({x},{y},{z})-({x},{y},{z + 1})");
            }
        }

        static List<ushort> Layer(WorldData w, int y)
        {
            var all = new List<ushort>();
            for (int z = 0; z < w.Size.Z; z++)
            for (int x = 0; x < w.Size.X; x++)
                all.Add(w.Get(x, y, z));
            return all;
        }

        static (WorldData, WorldEditor) DigOrderWorld()
        {
            var w = Flat(3);
            w.Set(1, 3, 1, Cell.Make(BlockMaterial.Water, Cell.MaxWaterLevel));
            var e = Editor(w);
            e.BasinFillRadius = 2;
            return (w, e);
        }

        /// <summary>0045 mục 2: cùng một hố, 40 thứ tự đào → cùng mức từng ô sau khi lắng.</summary>
        [Test]
        public void DigOrderDoesNotChangeLevels()
        {
            var cells = new List<(int x, int z)>();
            for (int z = 1; z <= 6; z++)
            for (int x = 1; x <= 7; x++)
                if (!(x == 1 && z == 1))
                    cells.Add((x, z));
            var (rw, re) = DigOrderWorld();
            foreach (var (x, z) in cells)
                Dig(re, x, 3, z);
            Settle(re);
            var reference = Layer(rw, 3);
            AssertSmooth(rw, 3);
            for (int seed = 0; seed < 40; seed++)
            {
                var order = new List<(int x, int z)>(cells);
                var rng = new System.Random(seed);
                for (int i = order.Count - 1; i > 0; i--)
                {
                    int j = rng.Next(i + 1);
                    (order[i], order[j]) = (order[j], order[i]);
                }
                var (w, e) = DigOrderWorld();
                foreach (var (x, z) in order)
                {
                    Dig(e, x, 3, z);
                    Settle(e);
                }
                CollectionAssert.AreEqual(reference, Layer(w, 3), $"seed {seed}");
                AssertSmooth(w, 3);
            }
        }

        static void DigTrenchAndRoom(WorldEditor e)
        {
            Dig(e, 8, 3, 5);
            for (int z = 3; z <= 7; z++)
            for (int x = 9; x <= 13; x++)
                Dig(e, x, 3, z);
        }

        static void AssertTrenchAndRoomFull(WorldData w)
        {
            Assert.IsTrue(IsWater(w, 8, 3, 5));
            Assert.AreEqual(5, Lv(w, 8, 3, 5), "trench");
            for (int z = 3; z <= 7; z++)
            for (int x = 9; x <= 13; x++)
            {
                Assert.IsTrue(IsWater(w, x, 3, z), $"({x},{z})");
                Assert.AreEqual(5, Lv(w, x, 3, z), $"level ({x},{z})");
            }
        }

        /// <summary>0045 mục 1: rãnh + phòng ngang mặt ao, kín trong BasinFillRadius → đầy mức 5.</summary>
        [Test]
        public void TrenchAtSourceLevelFillsWhenEnclosed()
        {
            var w = PondWorld();
            var e = Editor(w);
            DigTrenchAndRoom(e);
            Settle(e);
            AssertTrenchAndRoomFull(w);
        }

        /// <summary>0045: sàn trống ngang mặt nguồn lan ra ngoài BasinFillRadius (world 48) → vẫn lan giảm 5 − d như 0043.</summary>
        [Test]
        public void OpenFloorAtSourceLevelStillDecreases()
        {
            var w = OpenFloor(48);
            var e = Editor(w);
            Dig(e, 1, 6, 8);
            Settle(e);
            for (int z = 0; z < 48; z++)
            for (int x = 0; x < 48; x++)
            {
                int d = x + System.Math.Abs(z - 8);
                if (d == 0) continue;
                if (d <= 5)
                {
                    Assert.IsTrue(IsWater(w, x, 6, z), $"({x},{z})");
                    Assert.AreEqual(5 - d, Lv(w, x, 6, z), $"level ({x},{z})");
                }
                else
                    Assert.AreEqual(BlockMaterial.Empty, w.GetMaterial(x, 6, z), $"({x},{z})");
            }
        }

        /// <summary>0045: phòng kín đầy mức 5; đào thông ra dải trống xa hơn BasinFillRadius → hở, mức hạ đều.</summary>
        [Test]
        public void OpeningEnclosureLowersLevels()
        {
            var w = PondWorld();
            for (int z = 0; z < 16; z++)
                w.Set(15, 3, z, Cell.Make(BlockMaterial.Empty));
            var e = Editor(w);
            e.BasinFillRadius = 6;
            DigTrenchAndRoom(e);
            Settle(e);
            AssertTrenchAndRoomFull(w);
            Dig(e, 14, 3, 5);
            Settle(e);
            AssertSmooth(w, 3);
            Assert.IsTrue(IsWater(w, 10, 3, 5));
            Assert.Less(Lv(w, 10, 3, 5), 5);
            Assert.IsFalse(e.HasPendingWater);
        }

        /// <summary>0045 mục 2: đặt khối bịt lối ra làm phòng thành kín → tính lại, phòng đầy mức 5.</summary>
        [Test]
        public void PlacingWallEnclosesAndFills()
        {
            var w = PondWorld();
            for (int z = 0; z < 16; z++)
                w.Set(15, 3, z, Cell.Make(BlockMaterial.Empty));
            var e = Editor(w);
            e.BasinFillRadius = 6;
            DigTrenchAndRoom(e);
            Dig(e, 14, 3, 5);
            Settle(e);
            Assert.Less(Lv(w, 10, 3, 5), 5);
            Place(e, 14, 3, 5);
            Settle(e);
            AssertTrenchAndRoomFull(w);
            Assert.IsFalse(e.HasPendingWater);
        }

        /// <summary>0045: mất vách → ô được tăng mức bởi tường hạ về mức tính từ ô kề; kết quả như khi đào vách trước.</summary>
        [Test]
        public void LosingWallLowersBoostedCell()
        {
            var w = Corridor();
            var e = Editor(w);
            Dig(e, 1, 6, 8);
            Settle(e);
            for (int x = 1; x <= 15; x++)
                Assert.AreEqual(5, Lv(w, x, 6, 8), $"level x={x}");
            e.BasinFillRadius = 2;
            Dig(e, 8, 6, 9);
            Dig(e, 8, 6, 7);
            Settle(e);
            Assert.IsTrue(IsWater(w, 8, 6, 8));
            AssertSmooth(w, 6);

            var w2 = Corridor();
            var e2 = Editor(w2);
            e2.BasinFillRadius = 2;
            Dig(e2, 8, 6, 9);
            Dig(e2, 8, 6, 7);
            Dig(e2, 1, 6, 8);
            Settle(e2);
            CollectionAssert.AreEqual(Layer(w2, 6), Layer(w, 6));
        }
    }
}
