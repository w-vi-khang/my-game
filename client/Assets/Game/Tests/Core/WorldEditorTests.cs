using System.Collections.Generic;
using Game.Core;
using NUnit.Framework;

namespace Game.Core.Tests
{
    public class WorldEditorTests
    {
        WorldData world;
        MaterialInventory inv;
        WorldEditor editor;
        List<(int, int, int)> changed;

        [SetUp]
        public void SetUp()
        {
            world = new WorldData(new WorldSize(32, 16, 32));
            // Nền đá đáy y = 0 → ô y = 1 luôn kề khối đặc (luật đặt 0012).
            for (int z = 0; z < 32; z++)
            for (int x = 0; x < 32; x++)
                world.Set(x, 0, z, Cell.Make(BlockMaterial.Bedrock));
            world.Set(1, 1, 1, Cell.Make(BlockMaterial.Dirt));
            world.Set(1, 2, 1, Cell.Make(BlockMaterial.Grass));
            world.Set(2, 1, 1, Cell.Make(BlockMaterial.Water, 7));
            world.Set(3, 1, 1, Cell.SetStory(Cell.Make(BlockMaterial.Stone), true));
            inv = new MaterialInventory();
            editor = new WorldEditor(world, inv, 2);
            changed = new List<(int, int, int)>();
            editor.CellChanged += (x, y, z) => changed.Add((x, y, z));
        }

        [Test]
        public void InventoryStartsEmpty()
        {
            foreach (BlockMaterial m in System.Enum.GetValues(typeof(BlockMaterial)))
                Assert.AreEqual(0, inv.Get(m), m.ToString());
        }

        [Test]
        public void DigEmptiesCellAndAddsSameMaterial()
        {
            Assert.AreEqual(EditResult.Ok, editor.TryDig(1, 2, 1));
            ushort c = world.Get(1, 2, 1);
            Assert.AreEqual(BlockMaterial.Empty, Cell.GetMaterial(c));
            Assert.IsTrue(Cell.IsModified(c));
            Assert.AreEqual(1, inv.Get(BlockMaterial.Grass));
            Assert.AreEqual(0, inv.Get(BlockMaterial.Dirt));
            CollectionAssert.AreEqual(new[] { (1, 2, 1) }, changed);
        }

        [TestCase(1, 0, 1, EditResult.NotDiggable, TestName = "Dig bedrock")]
        [TestCase(2, 1, 1, EditResult.NotDiggable, TestName = "Dig water")]
        [TestCase(3, 1, 1, EditResult.NotDiggable, TestName = "Dig story block")]
        [TestCase(5, 5, 5, EditResult.NotDiggable, TestName = "Dig empty")]
        [TestCase(-1, 1, 1, EditResult.OutOfBounds, TestName = "Dig out of bounds x")]
        [TestCase(1, 16, 1, EditResult.OutOfBounds, TestName = "Dig out of bounds y")]
        public void DigRejected(int x, int y, int z, EditResult expected)
        {
            var before = (ushort[])world.Cells.Clone();
            Assert.AreEqual(expected, editor.TryDig(x, y, z));
            CollectionAssert.AreEqual(before, world.Cells);
            foreach (BlockMaterial m in System.Enum.GetValues(typeof(BlockMaterial)))
                Assert.AreEqual(0, inv.Get(m));
            Assert.IsEmpty(changed);
        }

        [Test]
        public void PlaceConsumesOneAndSetsCell()
        {
            inv.Add(BlockMaterial.Sand, 2);
            Assert.AreEqual(EditResult.Ok, editor.TryPlace(4, 1, 4, BlockMaterial.Sand));
            ushort c = world.Get(4, 1, 4);
            Assert.AreEqual(BlockMaterial.Sand, Cell.GetMaterial(c));
            Assert.IsTrue(Cell.IsModified(c));
            Assert.AreEqual(1, inv.Get(BlockMaterial.Sand));
            CollectionAssert.AreEqual(new[] { (4, 1, 4) }, changed);
        }

        [Test]
        public void DigThenPlaceRoundTrip()
        {
            Assert.AreEqual(EditResult.Ok, editor.TryDig(1, 1, 1));
            Assert.AreEqual(EditResult.Ok, editor.TryPlace(1, 1, 1, BlockMaterial.Dirt));
            Assert.AreEqual(BlockMaterial.Dirt, world.GetMaterial(1, 1, 1));
            Assert.AreEqual(0, inv.Get(BlockMaterial.Dirt));
        }

        [Test]
        public void PlaceWithoutMaterialRejected()
        {
            Assert.AreEqual(EditResult.NoMaterial, editor.TryPlace(4, 1, 4, BlockMaterial.Dirt));
            Assert.AreEqual(BlockMaterial.Empty, world.GetMaterial(4, 1, 4));
            Assert.IsEmpty(changed);
        }

        [Test]
        public void PlaceIntoNonEmptyRejected()
        {
            inv.Add(BlockMaterial.Dirt, 5);
            Assert.AreEqual(EditResult.NotEmpty, editor.TryPlace(1, 1, 1, BlockMaterial.Dirt));
            Assert.AreEqual(EditResult.NotEmpty, editor.TryPlace(1, 0, 5, BlockMaterial.Dirt), "đá đáy");
            Assert.AreEqual(5, inv.Get(BlockMaterial.Dirt));
            Assert.IsEmpty(changed);
        }

        [TestCase(BlockMaterial.Water)]
        [TestCase(BlockMaterial.Bedrock)]
        [TestCase(BlockMaterial.Empty)]
        public void PlaceUnplaceableMaterialRejected(BlockMaterial m)
        {
            inv.Add(m, 5);
            Assert.AreEqual(EditResult.NotPlaceable, editor.TryPlace(4, 1, 4, m));
            Assert.AreEqual(5, inv.Get(m));
        }

        [Test]
        public void PlaceOutOfBoundsRejected()
        {
            inv.Add(BlockMaterial.Dirt, 1);
            Assert.AreEqual(EditResult.OutOfBounds, editor.TryPlace(32, 1, 1, BlockMaterial.Dirt));
            Assert.AreEqual(1, inv.Get(BlockMaterial.Dirt));
        }

        [Test]
        public void PlaceOnOccupiedRejected()
        {
            inv.Add(BlockMaterial.Dirt, 1);
            editor.IsOccupied = (x, y, z) => x == 4 && y == 1 && z == 4;
            Assert.AreEqual(EditResult.Occupied, editor.TryPlace(4, 1, 4, BlockMaterial.Dirt));
            Assert.AreEqual(1, inv.Get(BlockMaterial.Dirt));
            Assert.AreEqual(EditResult.Ok, editor.TryPlace(5, 1, 4, BlockMaterial.Dirt));
        }

        static List<int> Affected(WorldSize s, int x, int y, int z)
        {
            var r = new List<int>();
            WorldEditor.AffectedChunks(s, 2, x, y, z, r);
            r.Sort();
            return r;
        }

        [Test]
        public void AffectedChunksByMargin()
        {
            var s = new WorldSize(64, 32, 64); // 4 × 2 × 4 chunk
            int C(int cx, int cy, int cz) => cx + 4 * (cz + 4 * cy);
            // Giữa chunk (cách biên ≥ 3 ô) → chỉ 1 chunk.
            CollectionAssert.AreEqual(new[] { C(1, 0, 1) }, Affected(s, 24, 8, 24));
            // Cách biên dưới 2 ô (ô cục bộ 1) và biên trên 2 ô (ô cục bộ 14) → thêm chunk bên cạnh.
            CollectionAssert.AreEqual(new[] { C(0, 0, 1), C(1, 0, 1) }, Affected(s, 17, 8, 24));
            CollectionAssert.AreEqual(new[] { C(1, 0, 1), C(2, 0, 1) }, Affected(s, 30, 8, 24));
            // Cách biên 3 ô (ô cục bộ 2 và 13) → chỉ chunk chứa ô.
            CollectionAssert.AreEqual(new[] { C(1, 0, 1) }, Affected(s, 18, 8, 24));
            CollectionAssert.AreEqual(new[] { C(1, 0, 1) }, Affected(s, 29, 8, 24));
            // Góc 3 trục → 8 chunk (kể cả chéo).
            Assert.AreEqual(8, Affected(s, 16, 16, 16).Count);
            // Mép map không ra ngoài.
            CollectionAssert.AreEqual(new[] { C(0, 0, 0) }, Affected(s, 0, 0, 0));
            CollectionAssert.AreEqual(new[] { C(3, 1, 3) }, Affected(s, 63, 31, 63));
        }
    }
}
