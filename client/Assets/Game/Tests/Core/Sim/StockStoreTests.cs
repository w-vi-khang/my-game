using System;
using Game.Core.Sim;
using NUnit.Framework;

namespace Game.Core.Tests
{
    public class StockStoreTests
    {
        [Test]
        public void SeedsProduceOnlyAndFreeLimit()
        {
            var w = TestWorlds.Flat(1);
            var sim = new Simulation(TestWorlds.NoWander(), w);
            sim.AddStorage(new CellPos(6, 2, 9));
            var st = new StockStore(sim);
            var stock = sim.State.Stock;
            int s0 = stock.Seeds[0];
            Assert.AreEqual(3, st.Add(ItemId.SeedBag(0), 3));
            Assert.AreEqual(s0 + 3, stock.Seeds[0]);
            Assert.AreEqual(s0 + 3, st.Count(ItemId.SeedBag(0)));
            Assert.AreEqual(0, st.CanAdd(ItemId.Block(BlockMaterial.Dirt), 5));
            int free = stock.Free;
            Assert.AreEqual(free, st.Add(ItemId.SeedBag(0), free + 10));
            Assert.AreEqual(0, stock.Free);
            Assert.AreEqual(0, st.Remove(ItemId.Produce(1), 5));
            Assert.Throws<ArgumentNullException>(() => new StockStore(null));
        }
    }
}
