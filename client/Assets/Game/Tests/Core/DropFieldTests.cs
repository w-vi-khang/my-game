using System;
using System.Collections.Generic;
using System.Linq;
using Game.Core;
using NUnit.Framework;

namespace Game.Core.Tests
{
    public class DropFieldTests
    {
        WorldData world;
        PlayerBag bag;
        WorldEditor editor;

        static WorldData BuildWorld()
        {
            var w = new WorldData(new WorldSize(32, 16, 32));
            for (int z = 0; z < 32; z++)
            for (int x = 0; x < 32; x++)
                w.Set(x, 0, z, Cell.Make(BlockMaterial.Bedrock));
            w.Set(1, 1, 1, Cell.Make(BlockMaterial.Dirt));
            w.Set(1, 2, 1, Cell.Make(BlockMaterial.Grass));
            w.Set(2, 1, 1, Cell.Make(BlockMaterial.Water, 7));
            w.Set(3, 1, 1, Cell.SetStory(Cell.Make(BlockMaterial.Stone), true));
            return w;
        }

        [SetUp]
        public void SetUp()
        {
            world = BuildWorld();
            bag = new PlayerBag();
            editor = new WorldEditor(world, bag, 2);
        }

        void Set(int x, int y, int z, BlockMaterial m) => world.Set(x, y, z, Cell.Make(m));
        static ItemId B(BlockMaterial m) => ItemId.Block(m);
        DropField Drops => editor.Drops;

        [Test]
        public void DigTwiceSameMaterialMergesIntoOneStack()
        {
            Set(4, 1, 4, BlockMaterial.Dirt);
            Set(4, 2, 4, BlockMaterial.Dirt);
            Assert.AreEqual(EditResult.Ok, editor.TryDig(4, 2, 4));
            Assert.AreEqual(EditResult.Ok, editor.TryDig(4, 1, 4));
            var at = Drops.At(4, 1, 4);
            Assert.AreEqual(1, at.Count);
            Assert.AreEqual(B(BlockMaterial.Dirt), at[0].Item);
            Assert.AreEqual(2, at[0].Count);
            Assert.AreEqual(2, at[0].Id);
            Assert.AreEqual(1, Drops.Stacks.Count);
            Assert.AreEqual(0, bag.Get(BlockMaterial.Dirt));
        }

        [Test]
        public void DigDifferentMaterialsSameCellTwoStacks()
        {
            Set(6, 1, 6, BlockMaterial.Stone);
            Set(6, 2, 6, BlockMaterial.Grass);
            editor.TryDig(6, 2, 6);
            editor.TryDig(6, 1, 6);
            var at = Drops.At(6, 1, 6);
            Assert.AreEqual(2, at.Count);
            Assert.AreEqual(B(BlockMaterial.Stone), at[0].Item);
            Assert.AreEqual(1, at[0].Count);
            Assert.AreEqual(2, at[0].Id);
            Assert.AreEqual(B(BlockMaterial.Grass), at[1].Item);
            Assert.AreEqual(1, at[1].Count);
            Assert.AreEqual(1, at[1].Id);
        }

        [Test]
        public void StackFallsSeveralCellsWhenSupportDug()
        {
            Set(8, 3, 8, BlockMaterial.Dirt);
            Set(8, 4, 8, BlockMaterial.Stone);
            editor.TryDig(8, 4, 8);
            Assert.AreEqual(1, Drops.At(8, 4, 8).Count);
            editor.TryDig(8, 3, 8);
            var at = Drops.At(8, 1, 8);
            Assert.AreEqual(2, at.Count);
            Assert.AreEqual(B(BlockMaterial.Dirt), at[0].Item);
            Assert.AreEqual(2, at[0].Id);
            Assert.AreEqual(B(BlockMaterial.Stone), at[1].Item);
            Assert.AreEqual(1, at[1].Id);
            Assert.AreEqual(0, Drops.At(8, 4, 8).Count);
        }

        [Test]
        public void StackSinksThroughWater()
        {
            Set(10, 1, 10, BlockMaterial.Water);
            Set(10, 2, 10, BlockMaterial.Water);
            Set(10, 3, 10, BlockMaterial.Dirt);
            Set(10, 4, 10, BlockMaterial.Grass);
            editor.TryDig(10, 4, 10);
            editor.TryDig(10, 3, 10);
            var at = Drops.At(10, 1, 10);
            Assert.AreEqual(2, at.Count);
            Assert.AreEqual(B(BlockMaterial.Dirt), at[0].Item);
            Assert.AreEqual(B(BlockMaterial.Grass), at[1].Item);
        }

        [Test]
        public void PlacePushesStackUpPastSolidAndMerges()
        {
            Assert.AreEqual(EditResult.Ok, editor.TryDig(1, 1, 1));
            var existing = Drops.Add(1, 3, 1, B(BlockMaterial.Dirt), 2);
            bag.Add(BlockMaterial.Stone);
            Assert.AreEqual(EditResult.Ok, editor.TryPlace(1, 1, 1, BlockMaterial.Stone));
            Assert.AreEqual(0, Drops.At(1, 1, 1).Count);
            var at = Drops.At(1, 3, 1);
            Assert.AreEqual(1, at.Count);
            Assert.AreEqual(B(BlockMaterial.Dirt), at[0].Item);
            Assert.AreEqual(3, at[0].Count);
            Assert.AreEqual(existing.Id, at[0].Id);
        }

        [Test]
        public void PlaceRejectedWhenNoRoomAbove()
        {
            for (int y = 2; y <= 15; y++) Set(14, y, 14, BlockMaterial.Stone);
            Drops.Add(14, 1, 14, B(BlockMaterial.Dirt), 1);
            bag.Add(BlockMaterial.Stone);
            Assert.AreEqual(EditResult.Occupied, editor.CheckPlace(14, 1, 14, BlockMaterial.Stone));
            Assert.AreEqual(EditResult.Occupied, editor.TryPlace(14, 1, 14, BlockMaterial.Stone));
            Assert.AreEqual(BlockMaterial.Empty, world.GetMaterial(14, 1, 14));
            Assert.AreEqual(1, bag.Get(BlockMaterial.Stone));
            Assert.AreEqual(1, Drops.At(14, 1, 14).Count);

            for (int y = 1; y <= 14; y++) Set(20, y, 20, BlockMaterial.Stone);
            Drops.Add(20, 15, 20, B(BlockMaterial.Dirt), 1);
            Assert.AreEqual(EditResult.Occupied, editor.CheckPlace(20, 15, 20, BlockMaterial.Stone));
        }

        [Test]
        public void CollectInReachTakesOnlyWithin8()
        {
            Drops.Add(8, 1, 0, B(BlockMaterial.Dirt), 2);
            Drops.Add(9, 1, 0, B(BlockMaterial.Stone), 1);
            var got = new List<DropStack>();
            Drops.Collected += s => got.Add(s);
            Assert.AreEqual(1, Drops.CollectInReach(0.5f, 0.5f, 0.5f, bag));
            Assert.AreEqual(2, bag.Get(BlockMaterial.Dirt));
            Assert.AreEqual(0, bag.Get(BlockMaterial.Stone));
            Assert.AreEqual(1, got.Count);
            Assert.AreEqual(B(BlockMaterial.Dirt), got[0].Item);
            Assert.AreEqual(1, Drops.Stacks.Count);
            Assert.AreEqual(B(BlockMaterial.Stone), Drops.Stacks[0].Item);

            Drops.Add(8, 1, 0, B(BlockMaterial.Dirt), 1);
            Assert.AreEqual(0, Drops.CollectInReach(0.4f, 0.5f, 0.5f, bag));
            Assert.AreEqual(2, bag.Get(BlockMaterial.Dirt));
        }

        [Test]
        public void TryCollectSingleStack()
        {
            var dirt = Drops.Add(2, 1, 0, B(BlockMaterial.Dirt), 1);
            Drops.Add(3, 1, 0, B(BlockMaterial.Stone), 1);
            var far = Drops.Add(9, 1, 0, B(BlockMaterial.Sand), 1);
            Assert.IsTrue(Drops.TryCollect(dirt.Id, 0.5f, 0.5f, 0.5f, bag));
            Assert.AreEqual(1, bag.Get(BlockMaterial.Dirt));
            Assert.AreEqual(0, bag.Get(BlockMaterial.Stone));
            Assert.AreEqual(1, Drops.At(3, 1, 0).Count);
            Assert.IsFalse(Drops.TryCollect(9999, 0.5f, 0.5f, 0.5f, bag));
            int v = Drops.Version;
            Assert.IsFalse(Drops.TryCollect(far.Id, 0.5f, 0.5f, 0.5f, bag));
            Assert.AreEqual(v, Drops.Version);
            Assert.AreEqual(0, bag.Get(BlockMaterial.Sand));
            Assert.AreEqual(1, Drops.At(9, 1, 0).Count);
        }

        [Test]
        public void SeedBagsMergeIntoBagSeeds()
        {
            Drops.Add(2, 1, 0, ItemId.SeedBag(0), 4);
            Drops.Add(2, 1, 0, ItemId.SeedBag(1), 1);
            Assert.AreEqual(2, Drops.At(2, 1, 0).Count);
            Drops.Add(2, 1, 0, ItemId.SeedBag(0), 2);
            Assert.AreEqual(2, Drops.At(2, 1, 0).Count);
            Assert.AreEqual(ItemId.SeedBag(0), Drops.At(2, 1, 0)[0].Item);
            Assert.AreEqual(6, Drops.At(2, 1, 0)[0].Count);
            Drops.CollectInReach(0.5f, 0.5f, 0.5f, bag);
            Assert.AreEqual(6, bag.GetSeeds(0));
            Assert.AreEqual(1, bag.GetSeeds(1));
        }

        [Test]
        public void AddValidatesArguments()
        {
            Assert.Throws<ArgumentException>(() => Drops.Add(1, 1, 1, B(BlockMaterial.Dirt), 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => Drops.Add(-1, 1, 0, B(BlockMaterial.Dirt), 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => Drops.Add(4, 1, 4, B(BlockMaterial.Dirt), 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => ItemId.SeedBag(256));
        }

        [Test]
        public void AddSettlesDownToSupport()
        {
            var s = Drops.Add(12, 5, 12, B(BlockMaterial.Sand), 1);
            Assert.AreEqual(1, s.Y);
            Assert.AreEqual(1, Drops.At(12, 1, 12).Count);
        }

        [Test]
        public void VersionIncrementsOnChanges()
        {
            int v = Drops.Version;
            Drops.Add(4, 1, 4, B(BlockMaterial.Dirt), 1);
            Assert.Greater(Drops.Version, v);

            Set(8, 3, 8, BlockMaterial.Dirt);
            Set(8, 4, 8, BlockMaterial.Stone);
            editor.TryDig(8, 4, 8);
            v = Drops.Version;
            editor.TryDig(8, 3, 8); // chồng Stone rơi
            Assert.Greater(Drops.Version, v + 1);

            Set(16, 1, 16, BlockMaterial.Dirt);
            editor.TryDig(16, 1, 16);
            bag.Add(BlockMaterial.Stone);
            v = Drops.Version;
            editor.TryPlace(16, 1, 16, BlockMaterial.Stone);
            Assert.Greater(Drops.Version, v);

            v = Drops.Version;
            Assert.Greater(Drops.CollectInReach(4.5f, 0.5f, 4.5f, bag), 0);
            Assert.Greater(Drops.Version, v);

            v = Drops.Version;
            Assert.AreEqual(0, Drops.CollectInReach(30.5f, 0.5f, 30.5f, bag));
            Assert.AreEqual(v, Drops.Version);
        }

        static (WorldEditor ed, PlayerBag bag) RunSequence()
        {
            var w = BuildWorld();
            var b = new PlayerBag();
            var ed = new WorldEditor(w, b, 2);
            for (int i = 0; i < 3; i++)
            {
                w.Set(5 + i, 1, 5, Cell.Make(BlockMaterial.Dirt));
                w.Set(5 + i, 2, 5, Cell.Make(BlockMaterial.Stone));
            }
            ed.TryDig(5, 2, 5); ed.TryDig(5, 1, 5);
            ed.TryDig(6, 1, 5); ed.TryDig(6, 2, 5);
            ed.TryDig(7, 2, 5); ed.TryDig(7, 1, 5);
            ed.TryDig(1, 1, 1);
            b.Add(BlockMaterial.Stone, 2);
            ed.TryPlace(1, 1, 1, BlockMaterial.Stone);
            ed.TryPlace(5, 1, 5, BlockMaterial.Stone);
            var first = ed.Drops.Stacks.Count > 0 ? ed.Drops.Stacks[0].Id : 0;
            ed.Drops.TryCollect(first, 5.5f, 1.5f, 5.5f, b);
            ed.Drops.CollectInReach(6.5f, 1.5f, 5.5f, b);
            return (ed, b);
        }

        [Test]
        public void DeterministicSameSequence()
        {
            var (a, _) = RunSequence();
            var (b, _) = RunSequence();
            Func<DropField, List<(int, ItemKind, byte, int, int, int, int)>> snap = d =>
                d.Stacks.Select(s => (s.Id, s.Item.Kind, s.Item.Value, s.Count, s.X, s.Y, s.Z)).ToList();
            CollectionAssert.AreEqual(snap(a.Drops), snap(b.Drops));
            Assert.AreEqual(a.Drops.Version, b.Drops.Version);
            Assert.Greater(a.Drops.Version, 6);
        }
    }
}
