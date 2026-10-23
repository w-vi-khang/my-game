using System;
using Game.Core.Sim;
using NUnit.Framework;

namespace Game.Core.Tests
{
    public class ItemTransferTests
    {
        const ItemContainer Bag = ItemContainer.Bag, Hot = ItemContainer.Hotbar, Stock = ItemContainer.Stock;

        [Test]
        public void DefaultAndNextTarget()
        {
            Assert.AreEqual(Hot, ItemTransfer.DefaultTarget(Bag, true));
            Assert.AreEqual(Bag, ItemTransfer.DefaultTarget(Hot, true));
            Assert.AreEqual(Bag, ItemTransfer.DefaultTarget(Stock, true));
            Assert.AreEqual(Hot, ItemTransfer.DefaultTarget(Bag, false));
            Assert.AreEqual(Stock, ItemTransfer.NextTarget(Bag, Hot, true));
            Assert.AreEqual(Hot, ItemTransfer.NextTarget(Bag, Stock, true));
            Assert.AreEqual(Hot, ItemTransfer.NextTarget(Bag, Hot, false));
            Assert.AreEqual(Hot, ItemTransfer.NextTarget(Stock, Bag, true));
            Assert.AreEqual(Bag, ItemTransfer.NextTarget(Stock, Hot, true));
            Assert.AreEqual(Hot, ItemTransfer.NextTarget(Bag, Bag, true));
        }

        [Test]
        public void PartAmount()
        {
            Assert.AreEqual(5, ItemTransfer.PartAmount(5, 1));
            Assert.AreEqual(3, ItemTransfer.PartAmount(5, 2));
            Assert.AreEqual(2, ItemTransfer.PartAmount(5, 3));
            Assert.AreEqual(1, ItemTransfer.PartAmount(1, 2));
            Assert.AreEqual(1, ItemTransfer.PartAmount(1, 3));
            Assert.AreEqual(333, ItemTransfer.PartAmount(999, 3));
            Assert.AreEqual(0, ItemTransfer.PartAmount(0, 2));
            Assert.Throws<ArgumentOutOfRangeException>(() => ItemTransfer.PartAmount(5, 4));
            Assert.Throws<ArgumentOutOfRangeException>(() => ItemTransfer.PartAmount(-1, 1));
        }

        [Test]
        public void FromSlotAndFromStore()
        {
            var seed = ItemId.SeedBag(0);
            var bag = new SlotInventory(10, 10, 999);
            var hot = new SlotInventory(2, 10, 999);
            bag.AddToSlot(0, seed, 10);
            Assert.AreEqual(5, ItemTransfer.FromSlot(bag, 0, hot, ItemTransfer.PartAmount(10, 2)));
            Assert.AreEqual(5, bag.Get(0).Count);
            Assert.AreEqual(5, hot.Count(seed));

            // túi phụ đầy bằng loại khác
            var full = new SlotInventory(2, 10, 999);
            full.Add(ItemId.Produce(1), 1998);
            Assert.AreEqual(0, ItemTransfer.FromSlot(bag, 0, full, 5));
            Assert.AreEqual(5, bag.Get(0).Count);

            // kho chung không nhận khối
            var w = TestWorlds.Flat(1);
            var sim = new Simulation(TestWorlds.NoWander(), w);
            sim.AddStorage(new CellPos(6, 2, 9));
            var stock = new StockStore(sim);
            bag.AddToSlot(1, ItemId.Block(BlockMaterial.Dirt), 5);
            Assert.AreEqual(0, ItemTransfer.FromSlot(bag, 1, stock, 5));
            Assert.AreEqual(5, bag.Get(1).Count);

            int base0 = stock.Count(seed);
            stock.Add(seed, 6);
            Assert.AreEqual(4, ItemTransfer.FromStore(stock, seed, bag, 4));
            Assert.AreEqual(9, bag.Count(seed));
            Assert.AreEqual(base0 + 2, stock.Count(seed));
            Assert.Throws<ArgumentException>(() => ItemTransfer.FromSlot(bag, 0, bag, 1));
        }
    }
}
