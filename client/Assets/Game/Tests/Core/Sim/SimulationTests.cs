using System.Collections.Generic;
using Game.Core.Sim;
using NUnit.Framework;

namespace Game.Core.Tests
{
    public class SimulationTests
    {
        static void Run(Simulation sim, int ticks)
        {
            for (int i = 0; i < ticks; i++) sim.Tick();
        }

        static int NewOrder(Simulation sim) => sim.AddOrder(new List<CellPos>());

        [Test]
        public void StageDoneAfterTravelPlusWork()
        {
            var sim = new Simulation(new SimConfig());
            int c = sim.AddCreature("L1", new CellPos(0, 0, 0));
            int s = sim.AddStage(NewOrder(sim), WorkAbility.Till, new CellPos(5, 0, 3));
            Run(sim, 114);
            Assert.AreEqual(StageStatus.Assigned, sim.State.Stages[s].Status);
            sim.Tick();
            var st = sim.State.Stages[s];
            Assert.AreEqual(StageStatus.Done, st.Status);
            Assert.AreEqual(CreatureState.Idle, sim.State.Creatures[c].State);
            Assert.AreEqual(new CellPos(5, 0, 3), sim.State.Creatures[c].Cell);
            Assert.AreEqual(1000, st.ProgressPermille);
        }

        [Test]
        public void StageAtCurrentCellStartsImmediately()
        {
            var sim = new Simulation(new SimConfig());
            int c = sim.AddCreature("L1", new CellPos(2, 0, 2));
            int s = sim.AddStage(NewOrder(sim), WorkAbility.Till, new CellPos(2, 0, 2));
            sim.Tick();
            Assert.AreEqual(CreatureState.Working, sim.State.Creatures[c].State);
            Assert.AreEqual(0, sim.State.Stages[s].WorkDone);
            Run(sim, 59);
            Assert.AreNotEqual(StageStatus.Done, sim.State.Stages[s].Status);
            sim.Tick();
            Assert.AreEqual(StageStatus.Done, sim.State.Stages[s].Status);
        }

        [Test]
        public void HaulTakesTwoLegs()
        {
            var sim = new Simulation(new SimConfig());
            int c = sim.AddCreature("L3", new CellPos(0, 0, 0));
            int s = sim.AddHaulStage(NewOrder(sim), new CellPos(3, 0, 0), new CellPos(3, 0, 4));
            Run(sim, 16);
            var st = sim.State.Stages[s];
            Assert.IsTrue(st.Carried);
            Assert.AreEqual(new CellPos(3, 0, 0), sim.State.Creatures[c].Cell);
            Assert.AreEqual(CreatureState.Moving, sim.State.Creatures[c].State);
            Run(sim, 39);
            Assert.AreNotEqual(StageStatus.Done, st.Status);
            sim.Tick();
            Assert.AreEqual(StageStatus.Done, st.Status);
            Assert.IsFalse(st.Carried);
        }

        [Test]
        public void OnlyMatchingAbility()
        {
            var sim = new Simulation(new SimConfig());
            int c = sim.AddCreature("L1", new CellPos(0, 0, 0));
            int s = sim.AddStage(NewOrder(sim), WorkAbility.Water, new CellPos(1, 0, 0));
            Run(sim, 10);
            Assert.AreEqual(StageStatus.Open, sim.State.Stages[s].Status);
            Assert.AreEqual(CreatureState.Idle, sim.State.Creatures[c].State);
        }

        [Test]
        public void PlayerAssignedStageIsSkipped()
        {
            var sim = new Simulation(new SimConfig());
            int c = sim.AddCreature("L1", new CellPos(0, 0, 0));
            int s = sim.AddStage(NewOrder(sim), WorkAbility.Till, new CellPos(1, 0, 0));
            sim.State.Stages[s].AssigneeKind = AssigneeKind.Player;
            Run(sim, 10);
            Assert.AreEqual(StageStatus.Open, sim.State.Stages[s].Status);
            Assert.AreEqual(CreatureState.Idle, sim.State.Creatures[c].State);
        }

        [Test]
        public void HigherPriorityFirst()
        {
            var sim = new Simulation(new SimConfig());
            int c = sim.AddCreature("L1", new CellPos(0, 0, 0));
            int o = NewOrder(sim);
            int a = sim.AddStage(o, WorkAbility.Till, new CellPos(1, 0, 0), 0);
            int b = sim.AddStage(o, WorkAbility.Till, new CellPos(9, 0, 0), 5);
            sim.Tick();
            Assert.AreEqual(b, sim.State.Creatures[c].StageId);
            Assert.AreEqual(StageStatus.Open, sim.State.Stages[a].Status);
        }

        [Test]
        public void ScoreIsDistanceOverMastery()
        {
            var cfg = new SimConfig();
            var list = new List<SpeciesWork>(cfg.species) { new SpeciesWork("L1b", "Till", "Sow", 200, 100, 1500) };
            cfg.species = list.ToArray();
            var sim = new Simulation(cfg);
            int a = sim.AddCreature("L1", new CellPos(4, 0, 0));
            int b = sim.AddCreature("L1b", new CellPos(0, 0, 6));
            sim.AddStage(NewOrder(sim), WorkAbility.Till, new CellPos(0, 0, 0));
            sim.Tick();
            Assert.AreNotEqual(-1, sim.State.Creatures[b].StageId);
            Assert.AreEqual(CreatureState.Idle, sim.State.Creatures[a].State);
        }

        [Test]
        public void TiesBrokenBySeed()
        {
            int First(uint seed)
            {
                var cfg = new SimConfig { simSeed = seed };
                var sim = new Simulation(cfg);
                sim.AddCreature("L1", new CellPos(2, 0, 0));
                sim.AddCreature("L1", new CellPos(0, 0, 2));
                sim.AddCreature("L1", new CellPos(4, 0, 2));
                sim.AddCreature("L1", new CellPos(2, 0, 4));
                int s = sim.AddStage(NewOrder(sim), WorkAbility.Till, new CellPos(2, 0, 2));
                sim.Tick();
                return sim.State.Stages[s].AssigneeId;
            }

            var distinct = new HashSet<int>();
            for (uint seed = 1; seed <= 20; seed++)
            {
                int id = First(seed);
                Assert.AreEqual(id, First(seed), "seed " + seed);
                distinct.Add(id);
            }
            Assert.GreaterOrEqual(distinct.Count, 2);
        }

        [Test]
        public void UnassignKeepsProgress()
        {
            var sim = new Simulation(new SimConfig());
            int c = sim.AddCreature("L1", new CellPos(0, 0, 0));
            int s = sim.AddStage(NewOrder(sim), WorkAbility.Till, new CellPos(0, 0, 0));
            Run(sim, 31);
            var st = sim.State.Stages[s];
            Assert.AreEqual(30000, st.WorkDone);
            Assert.AreEqual(500, st.ProgressPermille);
            sim.Unassign(c);
            Assert.AreEqual(StageStatus.Open, st.Status);
            Assert.AreEqual(30000, st.WorkDone);
            Assert.AreEqual(CreatureState.Idle, sim.State.Creatures[c].State);
            Run(sim, 30);
            Assert.AreNotEqual(StageStatus.Done, st.Status);
            sim.Tick();
            Assert.AreEqual(StageStatus.Done, st.Status);
            Assert.AreEqual(62, sim.State.Tick);
        }

        [Test]
        public void UnassignWhileCarryingDropsItem()
        {
            var sim = new Simulation(new SimConfig());
            int c = sim.AddCreature("L3", new CellPos(0, 0, 0));
            int s = sim.AddHaulStage(NewOrder(sim), new CellPos(0, 0, 0), new CellPos(10, 0, 0));
            Run(sim, 11);
            var st = sim.State.Stages[s];
            Assert.AreEqual(new CellPos(2, 0, 0), sim.State.Creatures[c].Cell);
            Assert.IsTrue(st.Carried);
            sim.Unassign(c);
            Assert.AreEqual(new CellPos(2, 0, 0), st.Target);
            Assert.IsFalse(st.Carried);
            Assert.AreEqual(StageStatus.Open, st.Status);
            while (st.Status != StageStatus.Done && sim.State.Tick < 200) sim.Tick();
            Assert.AreEqual(72, sim.State.Tick);
        }
    }
}
