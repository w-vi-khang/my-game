using System.Collections.Generic;
using Game.Core.Sim;
using NUnit.Framework;

namespace Game.Core.Tests
{
    public class SimDeterminismTests
    {
        static Simulation BuildFarm(uint seed)
        {
            var sim = new Simulation(new SimConfig { simSeed = seed });
            sim.AddCreature("L1", new CellPos(0, 0, 0));
            sim.AddCreature("L1", new CellPos(6, 0, 0));
            sim.AddCreature("L2", new CellPos(0, 0, 6));
            sim.AddCreature("L2", new CellPos(6, 0, 6));
            sim.AddCreature("L3", new CellPos(3, 0, 3));
            sim.AddCreature("L3", new CellPos(3, 0, 9));
            sim.AddStorage(new CellPos(10, 0, 10));
            var area = new List<CellPos>();
            for (int x = 2; x <= 4; x++)
                for (int z = 2; z <= 4; z++) area.Add(new CellPos(x, 0, z));
            int o = sim.AddOrder(area);
            for (int i = 0; i < area.Count; i++) sim.AddStage(o, WorkAbility.Till, area[i]);
            sim.AddStage(o, WorkAbility.Water, new CellPos(2, 0, 2));
            sim.AddStage(o, WorkAbility.Water, new CellPos(2, 0, 4));
            sim.AddStage(o, WorkAbility.Water, new CellPos(4, 0, 2));
            sim.AddStage(o, WorkAbility.Water, new CellPos(4, 0, 4));
            sim.AddHaulStage(o, new CellPos(2, 0, 2), new CellPos(10, 0, 10));
            sim.AddHaulStage(o, new CellPos(4, 0, 4), new CellPos(10, 0, 10));
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
    }
}
