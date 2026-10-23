using System;
using NUnit.Framework;

namespace Game.Core.Tests
{
    public class PlayerBagTests
    {
        [Test]
        public void ProduceAddsAndRaises()
        {
            var b = new PlayerBag();
            (int, int)? got = null;
            b.ProduceChanged += (c, n) => got = (c, n);
            b.AddProduce(0, 3);
            Assert.AreEqual(3, b.GetProduce(0));
            Assert.AreEqual((0, 3), got);
            Assert.Throws<ArgumentOutOfRangeException>(() => b.AddProduce(0, -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => b.GetProduce(256));
        }

        [Test]
        public void TryTakeSeeds()
        {
            var b = new PlayerBag();
            b.AddSeeds(0, 3);
            var events = new System.Collections.Generic.List<(int, int)>();
            b.SeedsChanged += (c, n) => events.Add((c, n));
            Assert.IsTrue(b.TryTakeSeeds(0, 2));
            Assert.AreEqual(1, b.GetSeeds(0));
            CollectionAssert.AreEqual(new[] { (0, 1) }, events);
            Assert.IsFalse(b.TryTakeSeeds(0, 2));
            Assert.AreEqual(1, b.GetSeeds(0));
            Assert.AreEqual(1, events.Count);
            Assert.IsTrue(b.TryTakeSeeds(0, 0));
            Assert.AreEqual(1, events.Count);
            Assert.Throws<ArgumentOutOfRangeException>(() => b.TryTakeSeeds(0, -1));
        }

        [Test]
        public void TryTakeProduce()
        {
            var b = new PlayerBag();
            b.AddProduce(1, 3);
            var events = new System.Collections.Generic.List<(int, int)>();
            b.ProduceChanged += (c, n) => events.Add((c, n));
            Assert.IsTrue(b.TryTakeProduce(1, 2));
            Assert.AreEqual(1, b.GetProduce(1));
            CollectionAssert.AreEqual(new[] { (1, 1) }, events);
            Assert.IsFalse(b.TryTakeProduce(1, 2));
            Assert.AreEqual(1, b.GetProduce(1));
            Assert.AreEqual(1, events.Count);
            Assert.IsTrue(b.TryTakeProduce(1, 0));
            Assert.AreEqual(1, events.Count);
        }

        [Test]
        public void ItemIdProduceAndTool()
        {
            Assert.AreNotEqual(ItemId.Produce(1), ItemId.SeedBag(1));
            Assert.AreEqual(1, ItemId.Produce(1).Crop);
            Assert.Throws<ArgumentOutOfRangeException>(() => ItemId.Produce(256));
            Assert.AreEqual(ToolItem.ObservationCamera, ItemId.Tool(ToolItem.ObservationCamera).ToolKind);
            Assert.AreEqual("Produce:1", ItemId.Produce(1).ToString());
            Assert.AreEqual("Tool:ObservationCamera", ItemId.Tool(ToolItem.ObservationCamera).ToString());
            Assert.AreEqual("SeedBag:0", ItemId.SeedBag(0).ToString());
            Assert.AreEqual("Block:Dirt", ItemId.Block(BlockMaterial.Dirt).ToString());
        }

        [Test]
        public void AddItemByKind()
        {
            var b = new PlayerBag();
            b.Add(ItemId.Produce(1), 3);
            Assert.AreEqual(3, b.GetProduce(1));
            b.Add(ItemId.SeedBag(0), 2);
            Assert.AreEqual(2, b.GetSeeds(0));
        }

        [TearDown]
        public void TearDown() => PlayerBag.Configure(new Game.Core.Sim.PlayerConfig());

        static ItemId Blk(BlockMaterial m) => ItemId.Block(m);

        [Test]
        public void DefaultSizesFromConfig()
        {
            var b = new PlayerBag();
            Assert.AreEqual(10, b.Main.Capacity);
            Assert.AreEqual(2, b.Hotbar.Capacity);
            Assert.AreEqual(10, b.Main.PageSize);
            Assert.AreEqual(10, b.Hotbar.PageSize);
            Assert.AreEqual(999, b.Main.StackMax);
            PlayerBag.Configure(new Game.Core.Sim.PlayerConfig { bagSlots = 20, hotbarSlots = 4, stackMax = 50 });
            b = new PlayerBag();
            Assert.AreEqual(20, b.Main.Capacity);
            Assert.AreEqual(4, b.Hotbar.Capacity);
            Assert.AreEqual(50, b.Main.StackMax);
            Assert.Throws<ArgumentNullException>(() => PlayerBag.Configure(null));
        }

        [Test]
        public void AddFillsBagThenHotbar()
        {
            var b = new PlayerBag(2, 1, 10, 10, 5);
            Assert.AreEqual(7, b.Add(BlockMaterial.Dirt, 7));
            Assert.AreEqual(5, b.Main.Get(0).Count);
            Assert.AreEqual(2, b.Main.Get(1).Count);
            Assert.AreEqual(5, b.Add(BlockMaterial.Stone, 6));
            Assert.AreEqual(Blk(BlockMaterial.Stone), b.Hotbar.Get(0).Item);
            Assert.AreEqual(5, b.Hotbar.Get(0).Count);
            Assert.AreEqual(0, b.CanAdd(Blk(BlockMaterial.Stone), 1));
            Assert.AreEqual(3, b.CanAdd(Blk(BlockMaterial.Dirt), 9));
            Assert.AreEqual(3, b.Add(BlockMaterial.Dirt, 9));
            Assert.AreEqual(10, b.Get(BlockMaterial.Dirt));
        }

        [Test]
        public void AddTopsUpHotbarFirst()
        {
            var b = new PlayerBag(3, 1, 10, 10, 999);
            var dirt = Blk(BlockMaterial.Dirt);
            b.Hotbar.AddToSlot(0, dirt, 5);
            b.Add(BlockMaterial.Dirt, 3);
            Assert.AreEqual(8, b.Hotbar.Get(0).Count);
            for (int i = 0; i < 3; i++) Assert.AreEqual(0, b.Main.Get(i).Count);
        }

        [Test]
        public void RemoveTakesBagBeforeHotbar()
        {
            var b = new PlayerBag(3, 1, 10, 10, 999);
            var dirt = Blk(BlockMaterial.Dirt);
            b.Hotbar.AddToSlot(0, dirt, 2);
            b.Main.AddToSlot(0, dirt, 3);
            Assert.IsTrue(b.TryTake(BlockMaterial.Dirt));
            Assert.AreEqual(2, b.Main.Get(0).Count);
            Assert.AreEqual(2, b.Hotbar.Get(0).Count);
            Assert.AreEqual(4, b.Remove(dirt, 4));
            Assert.AreEqual(0, b.Get(BlockMaterial.Dirt));
        }

        [Test]
        public void ToolTakesASlot()
        {
            var b = new PlayerBag(1, 1, 10, 10, 999);
            var cam = ItemId.Tool(ToolItem.ObservationCamera);
            Assert.AreEqual(1, b.Add(cam, 1));
            Assert.AreEqual(cam, b.Main.Get(0).Item);
            Assert.AreEqual(2, b.Add(BlockMaterial.Dirt, 2));
            Assert.AreEqual(0, b.Add(BlockMaterial.Stone, 1));
        }

        [Test]
        public void EventsOnlyWhenChanged()
        {
            var full = new PlayerBag(1, 1, 10, 10, 999);
            full.Add(BlockMaterial.Stone, 1);
            full.Add(BlockMaterial.Sand, 1);
            int seedEvents = 0;
            full.SeedsChanged += (c, n) => seedEvents++;
            Assert.AreEqual(0, full.AddSeeds(0, 3));
            Assert.AreEqual(0, seedEvents);
            var b = new PlayerBag();
            (int, int)? got = null;
            b.ProduceChanged += (c, n) => got = (c, n);
            b.AddProduce(2, 4);
            Assert.AreEqual((2, 4), got);
        }

        [Test]
        public void TryTakeSeedsAcrossBagAndHotbar()
        {
            var b = new PlayerBag(3, 1, 10, 10, 999);
            b.Main.AddToSlot(0, ItemId.SeedBag(0), 3);
            b.Hotbar.AddToSlot(0, ItemId.SeedBag(0), 2);
            Assert.IsTrue(b.TryTakeSeeds(0, 4));
            Assert.AreEqual(1, b.GetSeeds(0));
            Assert.IsFalse(b.TryTakeSeeds(0, 2));
            Assert.AreEqual(1, b.GetSeeds(0));
        }
    }
}
