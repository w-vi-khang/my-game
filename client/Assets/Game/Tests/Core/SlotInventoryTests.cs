using System;
using Game.Core.Sim;
using NUnit.Framework;

namespace Game.Core.Tests
{
    public class SlotInventoryTests
    {
        static readonly ItemId A = ItemId.SeedBag(0);
        static readonly ItemId B = ItemId.Produce(0);

        [Test]
        public void AddSplitsAt999()
        {
            var inv = new SlotInventory(10, 10, 999);
            Assert.AreEqual(1000, inv.Add(A, 1000));
            Assert.AreEqual(999, inv.Get(0).Count);
            Assert.AreEqual(1, inv.Get(1).Count);
            Assert.AreEqual(5, inv.Add(B, 5));
            Assert.AreEqual(B, inv.Get(2).Item);
            Assert.AreEqual(1000, inv.Count(A));
        }

        [Test]
        public void FullInventoryAddsPartially()
        {
            var inv = new SlotInventory(2, 10, 999);
            Assert.AreEqual(1500, inv.Add(A, 1500));
            Assert.AreEqual(498, inv.CanAdd(A, 1000));
            Assert.AreEqual(0, inv.Add(B, 1));
            Assert.AreEqual(498, inv.Add(A, 1000));
        }

        [Test]
        public void RemoveTakesFromHighestSlot()
        {
            var inv = new SlotInventory(10, 10, 999);
            inv.AddToSlot(0, A, 999);
            inv.AddToSlot(1, A, 5);
            Assert.AreEqual(3, inv.Remove(A, 3));
            Assert.AreEqual(2, inv.Get(1).Count);
            Assert.AreEqual(999, inv.Get(0).Count);
            Assert.AreEqual(10, inv.Remove(A, 10));
            Assert.IsTrue(inv.Get(1).IsEmpty);
            Assert.AreEqual(991, inv.Get(0).Count);
            Assert.AreEqual(991, inv.RemoveFromSlot(0, 2000));
            Assert.IsTrue(inv.Get(0).IsEmpty);
        }

        [Test]
        public void PagesWrap()
        {
            var inv = new SlotInventory(25, 10, 999);
            Assert.AreEqual(3, inv.PageCount);
            Assert.AreEqual(20, inv.PageStart(2));
            Assert.AreEqual(25, inv.PageEnd(2));
            Assert.AreEqual(0, inv.NextPage(2));
            Assert.AreEqual(2, inv.PrevPage(0));
            Assert.AreEqual(0, new SlotInventory(10, 10, 999).NextPage(0));
            var small = new SlotInventory(2, 10, 999);
            Assert.AreEqual(1, small.PageCount);
            Assert.Throws<ArgumentOutOfRangeException>(() => small.NextPage(3));
        }

        [Test]
        public void MoveEmptyMergeSwap()
        {
            var inv = new SlotInventory(10, 10, 999);
            inv.AddToSlot(0, A, 50);
            Assert.IsTrue(inv.Move(0, inv, 3));
            Assert.IsTrue(inv.Get(0).IsEmpty);
            Assert.AreEqual(50, inv.Get(3).Count);
            // merge
            inv.AddToSlot(1, A, 900);
            inv.RemoveFromSlot(3, 50);
            inv.AddToSlot(2, A, 200);
            Assert.IsTrue(inv.Move(2, inv, 1));
            Assert.AreEqual(999, inv.Get(1).Count);
            Assert.AreEqual(101, inv.Get(2).Count);
            // swap
            inv.AddToSlot(4, B, 7);
            Assert.IsTrue(inv.Move(4, inv, 2));
            Assert.AreEqual(B, inv.Get(2).Item);
            Assert.AreEqual(7, inv.Get(2).Count);
            Assert.AreEqual(A, inv.Get(4).Item);
            Assert.AreEqual(101, inv.Get(4).Count);
            // same slot / empty source
            Assert.IsFalse(inv.Move(2, inv, 2));
            Assert.IsFalse(inv.Move(9, inv, 8));
            // between inventories
            var hot = new SlotInventory(2, 10, 999);
            Assert.IsTrue(inv.Move(2, hot, 0));
            Assert.AreEqual(7, hot.Get(0).Count);
            Assert.IsTrue(inv.Get(2).IsEmpty);
        }

        [Test]
        public void SlotChangedAndArgs()
        {
            var inv = new SlotInventory(10, 10, 999);
            var seen = new System.Collections.Generic.List<int>();
            inv.SlotChanged += seen.Add;
            inv.Add(A, 1000);
            CollectionAssert.AreEqual(new[] { 0, 1 }, seen);
            seen.Clear();
            Assert.AreEqual(0, inv.Add(A, 0));
            Assert.AreEqual(0, seen.Count);
            Assert.Throws<ArgumentOutOfRangeException>(() => new SlotInventory(0, 10, 999));
            Assert.Throws<ArgumentOutOfRangeException>(() => inv.Get(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => inv.Add(A, -1));
        }

        [Test]
        public void TopUpOnlyExistingStacks()
        {
            var dirt = ItemId.Block(BlockMaterial.Dirt);
            var stone = ItemId.Block(BlockMaterial.Stone);
            var inv = new SlotInventory(3, 10, 5);
            inv.AddToSlot(1, dirt, 3);
            Assert.AreEqual(2, inv.TopUp(dirt, 4));
            Assert.AreEqual(5, inv.Get(1).Count);
            Assert.AreEqual(0, inv.Get(0).Count);
            Assert.AreEqual(0, inv.Get(2).Count);
            Assert.AreEqual(0, inv.TopUp(stone, 3));
            Assert.Throws<ArgumentOutOfRangeException>(() => inv.TopUp(dirt, -1));
        }
    }
}
