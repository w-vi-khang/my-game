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
