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

        static WorldEditor Editor(WorldData w)
        {
            var inv = new MaterialInventory();
            inv.Add(BlockMaterial.Stone, 100000);
            return new WorldEditor(w, inv, 1, S);
        }

        // ---------------- Đặt khối ----------------

        [Test]
        public void PlaceNeedsSolidFaceNeighbor()
        {
            var w = OpenSea();
            var e = Editor(w);
            Assert.AreEqual(EditResult.NotAttached, e.TryPlace(8, 9, 8, BlockMaterial.Stone), "giữa không trung");
            Assert.AreEqual(EditResult.Ok, e.TryPlace(8, 1, 8, BlockMaterial.Stone), "trong nước, kề đá đáy");
            Assert.AreEqual(EditResult.Ok, e.TryPlace(8, 2, 8, BlockMaterial.Stone), "chồng lên khối vừa đặt");
            Assert.AreEqual(EditResult.NotAttached, e.TryPlace(9, 3, 9, BlockMaterial.Stone), "chỉ chạm chéo không tính");
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
            Assert.AreEqual(EditResult.Ok, e.TryDig(4, 5, 7));
            Assert.AreEqual(BlockMaterial.Water, w.GetMaterial(4, 5, 7));
            Assert.AreEqual(1, e.LastFloodCount);
            Assert.IsTrue(Cell.IsModified(w.Get(4, 5, 7)));
        }

        [Test]
        public void DigAboveSeaLevelNeverFloods()
        {
            var w = Coast();
            var e = Editor(w);
            Assert.AreEqual(EditResult.Ok, e.TryDig(4, S + 1, 7));
            Assert.AreEqual(BlockMaterial.Empty, w.GetMaterial(4, S + 1, 7));
            Assert.AreEqual(0, e.LastFloodCount);
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
            Assert.AreEqual(EditResult.Ok, e.TryDig(9, 4, 7));
            Assert.AreEqual(0, e.LastFloodCount, "chỉ kề hầm khô, không kề nước → không loang");
            Assert.AreEqual(EditResult.Ok, e.TryDig(4, 5, 7));
            Assert.AreEqual(1, e.LastFloodCount);
            Assert.AreEqual(EditResult.Ok, e.TryDig(5, 5, 7));
            Assert.AreEqual(1 + 27 + 1, e.LastFloodCount, "ô vừa đào + 27 ô hầm + ô (9, 4, 7) đào trước đó");
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
            Assert.AreEqual(EditResult.Ok, e.TryDig(4, 5, 7));
            Assert.AreEqual(BlockMaterial.Empty, w.GetMaterial(4, 5, 7), "(3, 5, 6) và (3, 4, 7) là nước nhưng chỉ chạm chéo");
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
            Assert.AreEqual(EditResult.Ok, e.TryDig(0, 3, 5));
            Assert.AreEqual(BlockMaterial.Water, w.GetMaterial(0, 3, 5));
            Assert.AreEqual(EditResult.Ok, e.TryDig(1, 3, 5));
            Assert.AreEqual(BlockMaterial.Water, w.GetMaterial(1, 3, 5), "kề ô vừa ngập");
            Assert.AreEqual(EditResult.Ok, e.TryDig(5, 3, 5));
            Assert.AreEqual(BlockMaterial.Empty, w.GetMaterial(5, 3, 5), "giữa map, không kề nước");
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
                    Assert.AreEqual(EditResult.Ok, e.TryPlace(x, y, z, BlockMaterial.Stone), $"tường ({x},{y},{z})");
            }
        }

        /// <summary>Lấp mọi ô nước trong lòng (từ dưới lên) rồi đào ra (từ trên xuống) — cách "lấy nước ra" khi không có bơm.</summary>
        static void FillThenDigInside(WorldEditor e)
        {
            for (int y = 1; y <= S; y++)
            for (int z = 6; z <= 8; z++)
            for (int x = 6; x <= 8; x++)
                Assert.AreEqual(EditResult.Ok, e.TryPlace(x, y, z, BlockMaterial.Stone), $"lấp ({x},{y},{z})");
            for (int y = S; y >= 1; y--)
            for (int z = 6; z <= 8; z++)
            for (int x = 6; x <= 8; x++)
                Assert.AreEqual(EditResult.Ok, e.TryDig(x, y, z), $"đào ({x},{y},{z})");
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
            Assert.AreEqual(45, CountInside(w, BlockMaterial.Water), "dựng tường không làm nước rút");
        }

        [Test]
        public void SealedWallToFloorStaysDryAfterFillAndDig()
        {
            var w = OpenSea();
            var e = Editor(w);
            BuildRingWall(e, 1, openSide: false);
            FillThenDigInside(e);
            Assert.AreEqual(45, CountInside(w, BlockMaterial.Empty), "kín tới đáy → khô");
        }

        [Test]
        public void OpenUWallRefills()
        {
            var w = OpenSea();
            var e = Editor(w);
            BuildRingWall(e, 1, openSide: true);
            FillThenDigInside(e);
            Assert.AreEqual(45, CountInside(w, BlockMaterial.Water), "miệng U quay ra biển → ngập lại");
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
                Assert.AreEqual(EditResult.Ok, e.TryDig(5, 1, z));
                Assert.AreEqual(BlockMaterial.Water, w.GetMaterial(5, 1, z), "chân tường kề biển → ngập");
            }
            FillThenDigInside(e);
            Assert.AreEqual(45, CountInside(w, BlockMaterial.Water), "nước luồn dưới chân tường");
        }

        [Test]
        public void BreakingSealedWallFloodsDryInside()
        {
            var w = OpenSea();
            var e = Editor(w);
            BuildRingWall(e, 1, openSide: false);
            FillThenDigInside(e);
            Assert.AreEqual(EditResult.Ok, e.TryDig(7, 3, 9), "phá một khối tường");
            Assert.AreEqual(45, CountInside(w, BlockMaterial.Water), "mở thông ra biển, thấp hơn mực nước → ngập");
        }
    }
}
