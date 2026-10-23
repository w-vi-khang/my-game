using Game.Core;
using NUnit.Framework;

namespace Game.Core.Tests
{
    /// <summary>Biển tràn + luật đặt khối (design/decisions/0012, bước 5c).</summary>
    public class SeaFloodTests
    {
        const int S = 5; // mực nước biển

        /// <summary>World 16 × 16 × 16: đá đáy y = 0, nước y 1..S ở x ≤ 3 (biển), đá y 1..8 ở x ≥ 4 (đất liền).</summary>
        static WorldData Coast()
        {
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

        /// <summary>Toàn biển: đá đáy y = 0, nước y 1..S khắp map.</summary>
        static WorldData OpenSea()
        {
            var w = new WorldData(new WorldSize(16, 16, 16));
            for (int z = 0; z < 16; z++)
            for (int x = 0; x < 16; x++)
            {
                w.Set(x, 0, z, Cell.Make(BlockMaterial.Bedrock));
                for (int y = 1; y <= S; y++)
                    w.Set(x, y, z, Cell.Make(BlockMaterial.Water, Cell.MaxWaterLevel));
            }
            return w;
        }

        /// <summary>Đào rồi cho nước lan tới khi yên; trả về số ô ngập.</summary>
        static int DigSettle(WorldEditor e, int x, int y, int z)
        {
            Assert.AreEqual(EditResult.Ok, e.TryDig(x, y, z), $"dig ({x},{y},{z})");
            return e.SettleWater();
        }

        static WorldEditor Editor(WorldData w)
        {
            var inv = new PlayerBag();
            inv.Add(BlockMaterial.Stone, 100000);
            return new WorldEditor(w, inv, 1, S);
        }

        // ---------------- Đặt khối ----------------

        [Test]
        public void PlaceNeedsSolidFaceNeighbor()
        {
            var w = OpenSea();
            var e = Editor(w);
            Assert.AreEqual(EditResult.NotAttached, e.TryPlace(8, 9, 8, BlockMaterial.Stone), "mid-air");
            Assert.AreEqual(EditResult.Ok, e.TryPlace(8, 1, 8, BlockMaterial.Stone), "in water, next to bottom rock");
            Assert.AreEqual(EditResult.Ok, e.TryPlace(8, 2, 8, BlockMaterial.Stone), "overlaps the block just placed");
            Assert.AreEqual(EditResult.NotAttached, e.TryPlace(9, 3, 9, BlockMaterial.Stone), "diagonal contact does not count");
        }

        [Test]
        public void PlaceIntoWaterRemovesOnlyThatWater()
        {
            var w = OpenSea();
            var e = Editor(w);
            Assert.AreEqual(EditResult.Ok, e.TryPlace(8, 1, 8, BlockMaterial.Stone));
            ushort c = w.Get(8, 1, 8);
            Assert.AreEqual(BlockMaterial.Stone, Cell.GetMaterial(c));
            Assert.AreEqual(0, Cell.GetWaterLevel(c));
            Assert.AreEqual(BlockMaterial.Water, w.GetMaterial(9, 1, 8));
            Assert.AreEqual(BlockMaterial.Water, w.GetMaterial(8, 2, 8));
        }

        // ---------------- Biển loang khi đào ----------------

        [Test]
        public void DigNextToSeaFloods()
        {
            var w = Coast();
            var e = Editor(w);
            Assert.AreEqual(1, DigSettle(e, 4, 5, 7));
            Assert.AreEqual(BlockMaterial.Water, w.GetMaterial(4, 5, 7));
            Assert.IsTrue(Cell.IsModified(w.Get(4, 5, 7)));
        }

        [Test]
        public void DigAboveSeaLevelNeverFloods()
        {
            var w = Coast();
            var e = Editor(w);
            Assert.AreEqual(0, DigSettle(e, 4, S + 1, 7));
            Assert.AreEqual(BlockMaterial.Empty, w.GetMaterial(4, S + 1, 7));
        }

        [Test]
        public void FloodFillsConnectedDryChamber()
        {
            // Hầm khô kín trong đá (x 6..8, y 3..S, z 6..8). Đào thông từ biển → biển loang vào cả hầm.
            var w = Coast();
            for (int y = 3; y <= S; y++)
            for (int z = 6; z <= 8; z++)
            for (int x = 6; x <= 8; x++)
                w.Set(x, y, z, Cell.Make(BlockMaterial.Empty));
            var e = Editor(w);
            Assert.AreEqual(0, DigSettle(e, 9, 4, 7), "only next to a dry tunnel, not next to water → no spread");
            Assert.AreEqual(1, DigSettle(e, 4, 5, 7));
            Assert.AreEqual(1 + 27 + 1, DigSettle(e, 5, 5, 7), "just-dug cell + 27 tunnel cells + cell (9, 4, 7) dug earlier");
            for (int y = 3; y <= S; y++)
            for (int z = 6; z <= 8; z++)
            for (int x = 6; x <= 8; x++)
                Assert.AreEqual(BlockMaterial.Water, w.GetMaterial(x, y, z), $"({x},{y},{z})");
        }

        [Test]
        public void DiagonalGapDoesNotLeak()
        {
            var w = Coast();
            var e = Editor(w);
            // (4, 5, 7) chỉ chạm nước theo cạnh chéo nếu bốn ô kề mặt đều đặc: đặt đá ở (3, 5, 7) trước.
            Assert.AreEqual(EditResult.Ok, e.TryPlace(3, 5, 7, BlockMaterial.Stone));
            DigSettle(e, 4, 5, 7);
            Assert.AreEqual(BlockMaterial.Empty, w.GetMaterial(4, 5, 7), "(3, 5, 6) and (3, 4, 7) are water but only touch diagonally");
        }

        [Test]
        public void OceanBeyondMapRimFloods()
        {
            // Map toàn đá: đào ô ở biên x = 0, y ≤ S → kề biển ngoài map → ngập.
            var w = new WorldData(new WorldSize(16, 16, 16));
            for (int z = 0; z < 16; z++)
            for (int x = 0; x < 16; x++)
            for (int y = 0; y <= 8; y++)
                w.Set(x, y, z, Cell.Make(y == 0 ? BlockMaterial.Bedrock : BlockMaterial.Stone));
            var e = Editor(w);
            DigSettle(e, 0, 3, 5);
            Assert.AreEqual(BlockMaterial.Water, w.GetMaterial(0, 3, 5));
            DigSettle(e, 1, 3, 5);
            Assert.AreEqual(BlockMaterial.Water, w.GetMaterial(1, 3, 5), "next to just-flooded cell");
            DigSettle(e, 5, 3, 5);
            Assert.AreEqual(BlockMaterial.Empty, w.GetMaterial(5, 3, 5), "mid-map, not next to water");
        }

        // ---------------- Tường chữ U (ví dụ user hỏi khi duyệt 0012) ----------------

        /// <summary>Tường vòng quanh x 5..9, z 5..9 từ y = fromY tới S + 1 (xây từ đáy lên — luật đặt cần kề khối đặc); bên trong 3 × 3 (x 6..8, z 6..8).</summary>
        static void BuildRingWall(WorldEditor e, int fromY, bool openSide)
        {
            for (int y = fromY; y <= S + 1; y++)
            for (int z = 5; z <= 9; z++)
            for (int x = 5; x <= 9; x++)
            {
                bool rim = x == 5 || x == 9 || z == 5 || z == 9;
                bool gap = openSide && z == 9 && x >= 6 && x <= 8; // miệng U quay ra biển
                if (rim && !gap)
                    Assert.AreEqual(EditResult.Ok, e.TryPlace(x, y, z, BlockMaterial.Stone), $"wall ({x},{y},{z})");
            }
        }

        /// <summary>Lấp mọi ô nước trong lòng (từ dưới lên) rồi đào ra (từ trên xuống) — cách "lấy nước ra" khi không có bơm.</summary>
        static void FillThenDigInside(WorldEditor e)
        {
            for (int y = 1; y <= S; y++)
            for (int z = 6; z <= 8; z++)
            for (int x = 6; x <= 8; x++)
                Assert.AreEqual(EditResult.Ok, e.TryPlace(x, y, z, BlockMaterial.Stone), $"fill ({x},{y},{z})");
            for (int y = S; y >= 1; y--)
            for (int z = 6; z <= 8; z++)
            for (int x = 6; x <= 8; x++)
                DigSettle(e, x, y, z);
        }

        static int CountInside(WorldData w, BlockMaterial m)
        {
            int n = 0;
            for (int y = 1; y <= S; y++)
            for (int z = 6; z <= 8; z++)
            for (int x = 6; x <= 8; x++)
                if (w.GetMaterial(x, y, z) == m) n++;
            return n;
        }

        [Test]
        public void WallDoesNotDrainByItself()
        {
            var w = OpenSea();
            var e = Editor(w);
            BuildRingWall(e, 1, openSide: false);
            Assert.AreEqual(45, CountInside(w, BlockMaterial.Water), "building a wall does not drain water");
        }

        [Test]
        public void SealedWallToFloorStaysDryAfterFillAndDig()
        {
            var w = OpenSea();
            var e = Editor(w);
            BuildRingWall(e, 1, openSide: false);
            FillThenDigInside(e);
            Assert.AreEqual(45, CountInside(w, BlockMaterial.Empty), "sealed to the bottom → dry");
        }

        [Test]
        public void OpenUWallRefills()
        {
            var w = OpenSea();
            var e = Editor(w);
            BuildRingWall(e, 1, openSide: true);
            FillThenDigInside(e);
            Assert.AreEqual(45, CountInside(w, BlockMaterial.Water), "U mouth faces the sea → floods again");
        }

        [Test]
        public void WallNotReachingFloorLeaksUnderneath()
        {
            var w = OpenSea();
            var e = Editor(w);
            BuildRingWall(e, 1, openSide: false);
            // Đào bỏ chân tường phía x = 5 (y = 1): tường phía trên vẫn đứng (không có trọng lực), ô chân tường ngập → khe dưới tường.
            for (int z = 6; z <= 8; z++)
            {
                DigSettle(e, 5, 1, z);
                Assert.AreEqual(BlockMaterial.Water, w.GetMaterial(5, 1, z), "wall foot next to sea → floods");
            }
            FillThenDigInside(e);
            Assert.AreEqual(45, CountInside(w, BlockMaterial.Water), "water slips under the wall foot");
        }

        [Test]
        public void BreakingSealedWallFloodsDryInside()
        {
            var w = OpenSea();
            var e = Editor(w);
            BuildRingWall(e, 1, openSide: false);
            FillThenDigInside(e);
            DigSettle(e, 7, 3, 9);
            Assert.AreEqual(45, CountInside(w, BlockMaterial.Water), "open to the sea, below water level → floods");
        }

        // ---------------- Lan theo nhịp (0013) ----------------

        /// <summary>Hành lang khô dài 6 ô ở y = S (x 5..10, z 7) trong đất liền của Coast(), mở ra biển khi đào ô (4, S, 7).</summary>
        static WorldData CoastWithCorridor()
        {
            var w = Coast();
            for (int x = 5; x <= 10; x++)
                w.Set(x, S, 7, Cell.Make(BlockMaterial.Empty));
            return w;
        }

        [Test]
        public void WaterSpreadsOneCellPerStepSideways()
        {
            var w = CoastWithCorridor();
            var e = Editor(w);
            Assert.AreEqual(EditResult.Ok, e.TryDig(4, S, 7));
            Assert.AreEqual(BlockMaterial.Empty, w.GetMaterial(4, S, 7), "not flooded after digging — waiting for tick");
            Assert.IsTrue(e.HasPendingWater);
            for (int step = 1; step <= 7; step++)
            {
                Assert.AreEqual(1, e.StepWater(), $"tick {step}: each tick floods exactly 1 cell along the corridor");
                for (int x = 4; x <= 10; x++)
                    Assert.AreEqual(x - 4 < step ? BlockMaterial.Water : BlockMaterial.Empty, w.GetMaterial(x, S, 7), $"tick {step}, x = {x}");
            }
            Assert.AreEqual(0, e.StepWater());
            Assert.IsFalse(e.HasPendingWater);
        }

        [Test]
        public void WaterFallsStraightDownWithinOneStep()
        {
            // Giếng khô x = 4, z = 7 từ y = 1 tới S − 1 dưới ô (4, S, 7): đào ô trên cùng → một nhịp ngập cả giếng.
            var w = Coast();
            for (int y = 1; y < S; y++)
                w.Set(4, y, 7, Cell.Make(BlockMaterial.Empty));
            var e = Editor(w);
            Assert.AreEqual(EditResult.Ok, e.TryDig(4, S, 7));
            Assert.AreEqual(S, e.StepWater(), "just-dug cell + whole shaft below in the same tick");
            for (int y = 1; y <= S; y++)
                Assert.AreEqual(BlockMaterial.Water, w.GetMaterial(4, y, 7));
        }

        [Test]
        public void PlacingBlockInFrontStopsTheFlow()
        {
            var w = CoastWithCorridor();
            var e = Editor(w);
            Assert.AreEqual(EditResult.Ok, e.TryDig(4, S, 7));
            e.StepWater(); // (4, S, 7)
            e.StepWater(); // (5, S, 7)
            Assert.AreEqual(EditResult.Ok, e.TryPlace(7, S, 7, BlockMaterial.Stone), "blocked before water arrives");
            e.SettleWater();
            Assert.AreEqual(BlockMaterial.Water, w.GetMaterial(6, S, 7));
            Assert.AreEqual(BlockMaterial.Stone, w.GetMaterial(7, S, 7));
            for (int x = 8; x <= 10; x++)
                Assert.AreEqual(BlockMaterial.Empty, w.GetMaterial(x, S, 7), "still dry behind the block");
        }

        [Test]
        public void SteppedResultEqualsInstantRule()
        {
            // Trạng thái cuối của lan theo nhịp = mọi ô rỗng ≤ S thông với nước (luật 0012), so với BFS tức thời viết riêng trong test.
            var w = Coast();
            var rng = new System.Random(13);
            for (int k = 0; k < 120; k++)
                w.Set(rng.Next(4, 16), rng.Next(1, S + 3), rng.Next(0, 16), Cell.Make(BlockMaterial.Empty));
            w.Set(4, S, 7, Cell.Make(BlockMaterial.Stone)); // ô sẽ đào
            var expected = (ushort[])w.Cells.Clone();
            var e = Editor(w);
            Assert.AreEqual(EditResult.Ok, e.TryDig(4, S, 7));
            e.SettleWater();
            // BFS tức thời trên bản sao.
            var ew = new WorldData(w.Size, expected);
            ew.Set(4, S, 7, Cell.Make(BlockMaterial.Water, Cell.MaxWaterLevel)); // ô vừa đào kề biển (3, S, 7)
            var q = new System.Collections.Generic.Queue<(int, int, int)>();
            q.Enqueue((4, S, 7));
            int[,] d = { { 1, 0, 0 }, { -1, 0, 0 }, { 0, 1, 0 }, { 0, -1, 0 }, { 0, 0, 1 }, { 0, 0, -1 } };
            while (q.Count > 0)
            {
                var (x, y, z) = q.Dequeue();
                for (int i = 0; i < 6; i++)
                {
                    int nx = x + d[i, 0], ny = y + d[i, 1], nz = z + d[i, 2];
                    if (ny <= S && ew.Size.InBounds(nx, ny, nz) && ew.GetMaterial(nx, ny, nz) == BlockMaterial.Empty)
                    {
                        ew.Set(nx, ny, nz, Cell.Make(BlockMaterial.Water, Cell.MaxWaterLevel));
                        q.Enqueue((nx, ny, nz));
                    }
                }
            }
            for (int y = 0; y < w.Size.Y; y++)
            for (int z = 0; z < w.Size.Z; z++)
            for (int x = 0; x < w.Size.X; x++)
                Assert.AreEqual(ew.GetMaterial(x, y, z), w.GetMaterial(x, y, z), $"({x},{y},{z})");
        }
    }
}
