using Game.Core;
using NUnit.Framework;

namespace Game.Core.Tests
{
    public class CellTests
    {
        [Test]
        public void FieldsAreIndependent()
        {
            ushort c = Cell.Make(BlockMaterial.Stone, 5);
            c = Cell.SetStory(c, true);
            c = Cell.SetModified(c, true);
            c = Cell.SetReserved(c, 6);

            Assert.AreEqual(BlockMaterial.Stone, Cell.GetMaterial(c));
            Assert.AreEqual(5, Cell.GetWaterLevel(c));
            Assert.IsTrue(Cell.IsStory(c));
            Assert.IsTrue(Cell.IsModified(c));
            Assert.AreEqual(6, Cell.GetReserved(c));

            c = Cell.SetMaterial(c, BlockMaterial.Sand);
            c = Cell.SetStory(c, false);
            Assert.AreEqual(BlockMaterial.Sand, Cell.GetMaterial(c));
            Assert.AreEqual(5, Cell.GetWaterLevel(c));
            Assert.IsFalse(Cell.IsStory(c));
            Assert.IsTrue(Cell.IsModified(c));
            Assert.AreEqual(6, Cell.GetReserved(c));
        }

        [Test]
        public void FlowFlagsRoundTrip()
        {
            ushort source = Cell.Make(BlockMaterial.Water, 7);
            Assert.IsTrue(Cell.IsWaterSource(source));
            Assert.IsFalse(Cell.IsFlowing(source));
            Assert.IsFalse(Cell.IsFalling(source));

            ushort basis = Cell.SetModified(Cell.SetStory(Cell.Make(BlockMaterial.Empty), true), true);
            ushort f = Cell.MakeFlowing(basis, 4, false);
            Assert.AreEqual(BlockMaterial.Water, Cell.GetMaterial(f));
            Assert.AreEqual(4, Cell.GetWaterLevel(f));
            Assert.IsTrue(Cell.IsFlowing(f));
            Assert.IsFalse(Cell.IsFalling(f));
            Assert.IsFalse(Cell.IsWaterSource(f));
            Assert.IsTrue(Cell.IsStory(f));
            Assert.IsTrue(Cell.IsModified(f));

            ushort fall = Cell.MakeFlowing(f, 7, true);
            Assert.IsTrue(Cell.IsFalling(fall));
            Assert.IsFalse(Cell.IsFalling(Cell.MakeFlowing(fall, 3, false)));

            Assert.Throws<System.ArgumentOutOfRangeException>(() => Cell.MakeFlowing(0, 0, false));
            Assert.Throws<System.ArgumentOutOfRangeException>(() => Cell.MakeFlowing(0, 8, false));
        }

        [Test]
        public void LayoutMatchesWorldTech()
        {
            // world-tech: vật liệu 8 bit thấp, cờ cốt truyện bit 8, cờ đã sửa bit 9, mực nước bit 10–12, dự phòng 13–15.
            Assert.AreEqual(0x0003, Cell.Make(BlockMaterial.Stone));
            Assert.AreEqual(1 << 8, Cell.SetStory(0, true));
            Assert.AreEqual(1 << 9, Cell.SetModified(0, true));
            Assert.AreEqual(7 << 10, Cell.SetWaterLevel(0, 7));
            Assert.AreEqual(7 << 13, Cell.SetReserved(0, 7));
        }

        [Test]
        public void DrainingFlagRoundTrip()
        {
            ushort c = Cell.Make(BlockMaterial.Stone);
            Assert.IsFalse(Cell.IsDraining(c));
            c = Cell.SetDraining(c, true);
            Assert.IsTrue(Cell.IsDraining(c));
            Assert.AreEqual(1 << 15, c & (1 << 15));
            Assert.IsFalse(Cell.IsDraining(Cell.SetDraining(c, false)));
            ushort f = Cell.MakeFlowing(Cell.Make(BlockMaterial.Empty), 3, false, draining: true);
            Assert.IsTrue(Cell.IsDraining(f));
            Assert.IsFalse(Cell.IsDraining(Cell.MakeFlowing(f, 3, false)));
            Assert.IsFalse(Cell.IsDraining(Cell.ClearFlow(f)));
            Assert.IsFalse(Cell.IsDraining(Cell.ClearWater(f)));
        }

        [Test]
        public void WaterLevelOutOfRangeThrows()
        {
            Assert.Throws<System.ArgumentOutOfRangeException>(() => Cell.SetWaterLevel(0, 8));
            Assert.Throws<System.ArgumentOutOfRangeException>(() => Cell.SetWaterLevel(0, -1));
        }

        [Test]
        public void DiggableRules()
        {
            Assert.IsTrue(Cell.IsDiggable(Cell.Make(BlockMaterial.Dirt)));
            Assert.IsFalse(Cell.IsDiggable(Cell.Make(BlockMaterial.Empty)));
            Assert.IsFalse(Cell.IsDiggable(Cell.Make(BlockMaterial.Water, 7)));
            Assert.IsFalse(Cell.IsDiggable(Cell.Make(BlockMaterial.Bedrock)));
            Assert.IsFalse(Cell.IsDiggable(Cell.SetStory(Cell.Make(BlockMaterial.Dirt), true)));
        }
    }
}
