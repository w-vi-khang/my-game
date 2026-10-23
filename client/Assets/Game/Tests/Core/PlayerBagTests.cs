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
            Assert.Throws<NotSupportedException>(() => b.Add(ItemId.Tool(ToolItem.ObservationCamera), 1));
        }
    }
}
