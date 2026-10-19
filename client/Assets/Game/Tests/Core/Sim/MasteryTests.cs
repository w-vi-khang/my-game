using System.Collections.Generic;
using Game.Core.Sim;
using NUnit.Framework;

namespace Game.Core.Tests
{
    public class MasteryTests
    {
        static void Run(Simulation sim, int ticks)
        {
            for (int i = 0; i < ticks; i++) sim.Tick();
        }

        static int NewOrder(Simulation sim) => sim.AddOrder(new List<CellPos>());

        static Simulation Build(params SpeciesWork[] species)
        {
            var cfg = TestWorlds.NoWander();
            cfg.species = species;
            return new Simulation(cfg, TestWorlds.Flat(0));
        }

        [TestCase(WorkAbility.Till, 1, 121)]
        [TestCase(WorkAbility.Till, 49, 121)]
        [TestCase(WorkAbility.Till, 50, 81)]
        [TestCase(WorkAbility.Till, 100, 61)]
        [TestCase(WorkAbility.Till, 200, 49)]
        [TestCase(WorkAbility.Till, 399, 49)]
        [TestCase(WorkAbility.Till, 400, 41)]
        [TestCase(WorkAbility.Till, 100000, 41)]
        [TestCase(WorkAbility.Harvest, 50, 55)]
        [TestCase(WorkAbility.Harvest, 200, 33)]
        [TestCase(WorkAbility.Harvest, 400, 28)]
        public void WorkTicksFollowTable(WorkAbility ability, int mastery, int doneTick)
        {
            var sp = ability == WorkAbility.Till
                ? new SpeciesWork("M", "Till", "Harvest", mastery, 100, 1500)
                : new SpeciesWork("M", "Till", "Harvest", 100, mastery, 1500);
            var sim = Build(sp);
            int c = sim.AddCreature("M", new CellPos(2, 1, 2));
            int s = sim.AddStage(NewOrder(sim), ability, new CellPos(2, 1, 2));
            TestWorlds.HomeAll(sim);
            int h = sim.Config.WorkSpeedPermille(mastery);
            var st = sim.State.Stages[s];
            var cr = sim.State.Creatures[c];

            sim.Tick();
            Assert.AreEqual(CreatureState.Working, cr.State);
            Assert.AreEqual(0, st.WorkDone);
            sim.Tick();
            Assert.AreEqual(h, st.WorkDone);
            Run(sim, doneTick - 1 - 2);
            Assert.AreNotEqual(StageStatus.Done, st.Status);
            sim.Tick();
            Assert.AreEqual(StageStatus.Done, st.Status);
            Assert.AreEqual(st.WorkRequired, st.WorkDone);
            Assert.AreEqual(CreatureState.Idle, cr.State);
        }

        [TestCase(50, 63)]
        [TestCase(400, 50)]
        public void HaulWorkFollowsMastery(int mastery, int doneTick)
        {
            var sim = Build(new SpeciesWork("H", "Haul", "Harvest", mastery, 100, 2000));
            int c = sim.AddCreature("H", new CellPos(0, 1, 0));
            int s = sim.AddHaulStage(NewOrder(sim), new CellPos(3, 1, 0), new CellPos(3, 1, 4));
            TestWorlds.HomeAll(sim);
            var st = sim.State.Stages[s];
            var cr = sim.State.Creatures[c];

            Run(sim, 16);
            Assert.IsTrue(st.Carried);
            Assert.AreEqual(new CellPos(3, 1, 0), cr.Cell);
            Run(sim, 20);
            Assert.AreEqual(new CellPos(3, 1, 4), cr.Cell);
            Assert.AreEqual(0, st.WorkDone);
            Run(sim, doneTick - 1 - 36);
            Assert.AreNotEqual(StageStatus.Done, st.Status);
            sim.Tick();
            Assert.AreEqual(StageStatus.Done, st.Status);
            Assert.IsFalse(st.Carried);
        }

        [Test]
        public void TravelPlusWorkUsesMastery()
        {
            var sim = Build(new SpeciesWork("M", "Till", "Sow", 200, 100, 1500));
            int c = sim.AddCreature("M", new CellPos(0, 1, 0));
            int s = sim.AddStage(NewOrder(sim), WorkAbility.Till, new CellPos(5, 1, 3));
            TestWorlds.HomeAll(sim);
            var st = sim.State.Stages[s];
            var cr = sim.State.Creatures[c];

            Run(sim, 55);
            Assert.AreEqual(new CellPos(5, 1, 3), cr.Cell);
            Assert.AreEqual(0, st.WorkDone);
            sim.Tick();
            Assert.AreEqual(1250, st.WorkDone);
            Run(sim, 102 - 56);
            Assert.AreEqual(StageStatus.Assigned, st.Status);
            sim.Tick();
            Assert.AreEqual(StageStatus.Done, st.Status);
        }

        [Test]
        public void SwapMidStageKeepsProgressUsesNewSpeed()
        {
            var sim = Build(
                new SpeciesWork("F", "Till", "Sow", 400, 100, 1500),
                new SpeciesWork("S", "Till", "Harvest", 1, 100, 1500));
            int f = sim.AddCreature("F", new CellPos(2, 1, 2));
            int sl = sim.AddCreature("S", new CellPos(6, 1, 2));
            int o = NewOrder(sim);
            int tillId = sim.AddStage(o, WorkAbility.Till, new CellPos(2, 1, 2));
            TestWorlds.HomeAll(sim);
            var till = sim.State.Stages[tillId];

            sim.Tick();
            Assert.AreEqual(f, till.AssigneeId);
            Assert.AreEqual(CreatureState.Working, sim.State.Creatures[f].State);
            Run(sim, 20);
            Assert.AreEqual(30000, till.WorkDone);

            sim.Unassign(f);
            int sowId = sim.AddStage(o, WorkAbility.Sow, new CellPos(2, 1, 6), 10);
            sim.Tick();
            Assert.AreEqual(StageStatus.Assigned, till.Status);
            Assert.AreEqual(sl, till.AssigneeId);
            Assert.AreEqual(30000, till.WorkDone);
            Assert.AreEqual(f, sim.State.Stages[sowId].AssigneeId);

            Run(sim, 49 - 22);
            Assert.AreEqual(new CellPos(2, 1, 2), sim.State.Creatures[sl].Cell);
            Assert.AreEqual(30000, till.WorkDone);
            sim.Tick();
            Assert.AreEqual(30500, till.WorkDone);
            Run(sim, 108 - 50);
            Assert.AreNotEqual(StageStatus.Done, till.Status);
            Assert.AreEqual(59500, till.WorkDone);
            sim.Tick();
            Assert.AreEqual(StageStatus.Done, till.Status);
        }
    }
}
