using System.Collections.Generic;
using Game.Core.Sim;
using NUnit.Framework;

namespace Game.Core.Tests
{
    public class SimDeterminismTests
    {
        internal static Simulation BuildFarm(uint seed)
        {
            var sim = new Simulation(new SimConfig { simSeed = seed }, TestWorlds.Flat(0));
            sim.AddCreature("L1", new CellPos(0, 1, 0));
            sim.AddCreature("L1", new CellPos(6, 1, 0));
            sim.AddCreature("L2", new CellPos(0, 1, 6));
            sim.AddCreature("L2", new CellPos(6, 1, 6));
            sim.AddCreature("L3", new CellPos(3, 1, 3));
            sim.AddCreature("L3", new CellPos(3, 1, 9));
            sim.AddStorage(new CellPos(12, 1, 12));
            var area = new List<CellPos>();
            for (int x = 2; x <= 4; x++)
                for (int z = 2; z <= 4; z++) area.Add(new CellPos(x, 1, z));
            int o = sim.AddOrder(area);
            for (int i = 0; i < area.Count; i++) sim.AddStage(o, WorkAbility.Till, area[i]);
            sim.AddStage(o, WorkAbility.Water, new CellPos(2, 1, 2));
            sim.AddStage(o, WorkAbility.Water, new CellPos(2, 1, 4));
            sim.AddStage(o, WorkAbility.Water, new CellPos(4, 1, 2));
            sim.AddStage(o, WorkAbility.Water, new CellPos(4, 1, 4));
            sim.AddHaulStage(o, new CellPos(2, 1, 2), new CellPos(10, 1, 10));
            sim.AddHaulStage(o, new CellPos(4, 1, 4), new CellPos(10, 1, 10));
            TestWorlds.HomeAll(sim);
            return sim;
        }

        static List<ulong> RunHashes(Simulation sim, int ticks)
        {
            var list = new List<ulong>(ticks);
            for (int i = 0; i < ticks; i++)
            {
                sim.Tick();
                list.Add(SimHash.Compute(sim.State));
            }
            return list;
        }

        [Test]
        public void SameInputSameHashEveryTick()
        {
            var a = BuildFarm(20261014);
            var b = BuildFarm(20261014);
            var ha = RunHashes(a, 2000);
            var hb = RunHashes(b, 2000);
            CollectionAssert.AreEqual(ha, hb);
            foreach (var s in a.State.Stages) Assert.AreEqual(StageStatus.Done, s.Status, "stage " + s.Id);
        }

        [Test]
        public void HashChangesWhenStateChanges()
        {
            var a = BuildFarm(20261014);
            var b = BuildFarm(20261014);
            Assert.AreEqual(SimHash.Compute(a.State), SimHash.Compute(b.State));
            a.Tick();
            Assert.AreNotEqual(SimHash.Compute(a.State), SimHash.Compute(b.State));
        }

        static Simulation BuildCropFarm()
        {
            var sim = new Simulation(new SimConfig(), TestWorlds.Flat(1));
            sim.AddCreature("L1", new CellPos(1, 2, 1));
            sim.AddCreature("L1", new CellPos(9, 2, 1));
            sim.AddCreature("L2", new CellPos(5, 2, 1));
            sim.AddCreature("L3", new CellPos(9, 2, 9));
            int k = sim.AddStorage(new CellPos(5, 2, 8));
            foreach (var soilCell in new List<CellPos> { new CellPos(4, 1, 4), new CellPos(5, 1, 4), new CellPos(6, 1, 4) }) sim.EnqueueCrop(sim.MarkTill(soilCell), 0);
            for (int p = 0; p < 3; p++) sim.EnqueueCrop(p, 0);
            TestWorlds.HomeAll(sim);
            return sim;
        }

        [Test]
        public void FarmOrderSameHashEveryTick()
        {
            var a = BuildCropFarm();
            var b = BuildCropFarm();
            CollectionAssert.AreEqual(RunHashes(a, 4000), RunHashes(b, 4000));
            foreach (var p in a.State.Plots) Assert.AreEqual(PlotPhase.Idle, p.Phase, "plot " + p.Id);
            Assert.AreEqual(6, a.State.Stock.Produce[0]);
            Assert.AreEqual(10, a.State.Stock.Seeds[0]);
            Assert.AreEqual(0, a.State.Stock.Reserved);
        }
    }
}
