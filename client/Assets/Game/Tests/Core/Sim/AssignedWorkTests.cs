using System;
using Game.Core.Sim;
using NUnit.Framework;

namespace Game.Core.Tests
{
    /// <summary>Sinh vật chỉ làm công đoạn được giao (sim 7g, 0036): vụ tự làm, Tiếp tục, xới lại khi được giao.</summary>
    public class AssignedWorkTests
    {
        static readonly CellPos Soil = new CellPos(5, 1, 5);
        const int Idle600 = 600;

        WorldData w;
        Simulation sim;
        PlayerBag bag;
        int id;

        static void RunTo(Simulation s, long tick)
        {
            while (s.State.Tick < tick) s.Tick();
        }

        void Run(int n) => RunTo(sim, sim.State.Tick + n);

        void Until(Func<bool> cond, int max, string what)
        {
            for (int i = 0; i < max && !cond(); i++) sim.Tick();
            Assert.IsTrue(cond(), "Not reached: " + what + " (tick " + sim.State.Tick + ")");
        }

        void Layout(bool creatures, int[] crops = null)
        {
            w = TestWorlds.Flat(1);
            sim = new Simulation(TestWorlds.NoWander(), w);
            sim.AttachEditor(new WorldEditor(w, new PlayerBag(), 1));
            if (creatures)
            {
                sim.AddCreature("L1", new CellPos(8, 2, 5));
                sim.AddCreature("L2", new CellPos(5, 2, 1));
                sim.AddCreature("L3", new CellPos(9, 2, 5));
            }
            sim.AddStorage(new CellPos(6, 2, 9));
            id = sim.MarkTill(Soil);
            if (crops != null) foreach (int c in crops) sim.EnqueueCrop(id, c);
            if (creatures) TestWorlds.HomeAll(sim);
            bag = new PlayerBag();
        }

        FarmPlot Plot => sim.State.Plots[id];

        int CountStages(WorkAbility a)
        {
            int n = 0;
            foreach (var s in sim.State.Stages)
                if (s.Ability == a) n++;
            return n;
        }

        WorkStage LastStage(WorkAbility a)
        {
            WorkStage found = null;
            foreach (var s in sim.State.Stages)
                if (s.Ability == a) found = s;
            return found;
        }

        /// <summary>Người chơi xới + gieo ô đứng yên (vụ tự làm) → pha Water.</summary>
        void SelfSow(int crop = 0)
        {
            Assert.IsTrue(sim.PlayerTill(id));
            bag.AddSeeds(crop, 1);
            Assert.AreEqual(PlayerSowResult.Ok, sim.PlayerSow(id, crop, bag));
            Assert.IsTrue(Plot.SelfMade);
            Assert.AreEqual(PlotPhase.Water, Plot.Phase);
        }

        void SelfToHarvest(int crop = 0)
        {
            SelfSow(crop);
            Assert.AreEqual(RainResult.Watered, sim.Rain(Soil));
            RunTo(sim, sim.State.Tick + sim.Config.GrowTicks(crop));
            Assert.AreEqual(PlotPhase.Harvest, Plot.Phase);
        }

        [Test]
        public void SelfSow_NoCreatureWaters()
        {
            Layout(true);
            SelfSow();
            Run(Idle600);
            Assert.AreEqual(PlotPhase.Water, Plot.Phase);
            var st = sim.State.Stages[Plot.StageId];
            Assert.AreEqual(WorkAbility.Water, st.Ability);
            Assert.AreEqual(StageStatus.Open, st.Status);
            Assert.AreEqual(AssigneeKind.None, st.AssigneeKind);
            foreach (var c in sim.State.Creatures) Assert.AreNotEqual(st.Id, c.StageId);
        }

        [Test]
        public void Continue_WatersThenStops_RipeNotHarvested()
        {
            Layout(true);
            SelfSow();
            Run(Idle600);
            Assert.IsTrue(sim.CanContinue(id));
            var water = sim.State.Stages[Plot.StageId];
            Assert.IsTrue(sim.Continue(id));
            Assert.IsFalse(sim.Continue(id));
            Until(() => Plot.Phase == PlotPhase.Growing, 2000, "Growing");
            Assert.AreEqual(StageStatus.Done, water.Status);
            Assert.AreEqual(AssigneeKind.Creature, water.AssigneeKind);
            Assert.IsFalse(Plot.Granted);
            RunTo(sim, Plot.RipeTick + Idle600);
            Assert.AreEqual(PlotPhase.Harvest, Plot.Phase);
            var harvest = sim.State.Stages[Plot.StageId];
            Assert.AreEqual(StageStatus.Open, harvest.Status);
            Assert.AreEqual(AssigneeKind.None, harvest.AssigneeKind);
            Assert.IsTrue(sim.CanContinue(id));
        }

        [Test]
        public void Continue_Harvests_ThenIdleUntilContinue()
        {
            Layout(true);
            SelfSow();
            Assert.IsTrue(sim.Continue(id));
            Until(() => Plot.Phase == PlotPhase.Growing, 2000, "Growing");
            RunTo(sim, Plot.RipeTick + Idle600);
            Assert.AreEqual(PlotPhase.Harvest, Plot.Phase);
            int hauls = CountStages(WorkAbility.Haul);
            Assert.IsTrue(sim.Continue(id));
            Until(() => Plot.Phase == PlotPhase.Idle, 3000, "Idle after harvest");
            Assert.AreEqual(hauls + 1, CountStages(WorkAbility.Haul));
            Assert.IsFalse(Plot.SelfMade);
            Assert.IsFalse(Plot.SoilReady);
            int tills = CountStages(WorkAbility.Till);
            Run(Idle600);
            Assert.AreEqual(PlotPhase.Idle, Plot.Phase);
            Assert.AreEqual(tills, CountStages(WorkAbility.Till));
            Assert.IsTrue(sim.CanContinue(id));
            Assert.IsTrue(sim.Continue(id));
            Assert.AreEqual(PlotPhase.Till, Plot.Phase);
            Until(() => Plot.SoilReady, 2000, "SoilReady");
            Assert.AreEqual(PlotPhase.Idle, Plot.Phase);
            Assert.AreEqual(AssigneeKind.Creature, LastStage(WorkAbility.Till).AssigneeKind);
            Assert.IsFalse(sim.CanContinue(id));
            Assert.IsFalse(Plot.Granted);
        }

        [Test]
        public void SelfMade_PlayerHarvest_StaysIdle()
        {
            Layout(true);
            SelfToHarvest();
            Assert.IsTrue(sim.PlayerHarvest(id, bag));
            Assert.AreEqual(PlotPhase.Idle, Plot.Phase);
            Assert.IsFalse(Plot.SelfMade);
            int tills = CountStages(WorkAbility.Till);
            Run(Idle600);
            Assert.AreEqual(PlotPhase.Idle, Plot.Phase);
            Assert.AreEqual(tills, CountStages(WorkAbility.Till));
        }

        [Test]
        public void AssignedPlot_PlayerHarvests_CreaturesRetillAndNextCrop()
        {
            Layout(true, new[] { 0, 0 });
            Until(() => Plot.Phase == PlotPhase.Harvest, 8000, "Harvest");
            Assert.IsFalse(Plot.SelfMade);
            Assert.IsTrue(sim.PlayerHarvest(id, bag));
            Assert.AreEqual(PlotPhase.Till, Plot.Phase);
            Until(() => Plot.Phase == PlotPhase.Water || Plot.Phase == PlotPhase.Growing, 4000, "second crop watered");
            Assert.AreEqual(AssigneeKind.Creature, LastStage(WorkAbility.Till).AssigneeKind);
            Assert.AreEqual(AssigneeKind.Creature, LastStage(WorkAbility.Sow).AssigneeKind);
            Assert.IsFalse(Plot.SelfMade);
        }

        [Test]
        public void PlayerTill_SignWithQueue_CreatureSows()
        {
            Layout(true, new[] { 0 });
            Assert.IsTrue(sim.PlayerTill(id));
            Assert.AreEqual(PlotPhase.Sow, Plot.Phase);
            Until(() => Plot.Phase == PlotPhase.Growing, 4000, "Growing");
            Assert.AreEqual(AssigneeKind.Creature, LastStage(WorkAbility.Sow).AssigneeKind);
        }

        [Test]
        public void PlayerTill_SignNoQueue_StaysIdle()
        {
            Layout(true);
            Assert.IsTrue(sim.PlayerTill(id));
            Assert.AreEqual(PlotPhase.Idle, Plot.Phase);
            Assert.IsTrue(Plot.SoilReady);
            int stages = sim.State.Stages.Count;
            Run(Idle600);
            Assert.AreEqual(PlotPhase.Idle, Plot.Phase);
            Assert.AreEqual(stages, sim.State.Stages.Count);
            Assert.IsFalse(sim.CanContinue(id));
        }

        [Test]
        public void PlayerTill_IdleUnready_Retills()
        {
            Layout(true);
            SelfToHarvest();
            Assert.IsTrue(sim.PlayerHarvest(id, bag));
            Assert.IsFalse(Plot.SoilReady);
            Assert.IsTrue(sim.CanPlayerTill(id));
            Assert.IsTrue(sim.PlayerTill(id));
            Assert.IsTrue(Plot.SoilReady);
            Assert.AreEqual(PlotPhase.Idle, Plot.Phase);
        }

        [Test]
        public void EnqueueDuringSelfMade_KeepsHeld_NextCropAssigned()
        {
            Layout(true);
            SelfSow();
            Assert.IsTrue(sim.EnqueueCrop(id, 0));
            Run(Idle600);
            var water = sim.State.Stages[Plot.StageId];
            Assert.AreEqual(StageStatus.Open, water.Status);
            Assert.AreEqual(AssigneeKind.None, water.AssigneeKind);
            Assert.AreEqual(RainResult.Watered, sim.Rain(Soil));
            RunTo(sim, sim.State.Tick + sim.Config.GrowTicks(0));
            Assert.AreEqual(PlotPhase.Harvest, Plot.Phase);
            Assert.IsTrue(sim.PlayerHarvest(id, bag));
            Assert.AreEqual(PlotPhase.Till, Plot.Phase);
            Assert.IsFalse(Plot.SelfMade);
            Until(() => Plot.Phase == PlotPhase.Water || Plot.Phase == PlotPhase.Growing, 4000, "next crop watered");
            Assert.AreEqual(AssigneeKind.Creature, LastStage(WorkAbility.Till).AssigneeKind);
        }

        [Test]
        public void CancelCurrent_ClearsSelfMade()
        {
            Layout(true);
            SelfSow();
            Assert.IsTrue(sim.Continue(id));
            Assert.IsTrue(Plot.Granted);
            Assert.IsTrue(sim.CancelCurrent(id));
            Assert.IsFalse(Plot.SelfMade);
            Assert.IsFalse(Plot.Granted);
            Assert.AreEqual(PlotPhase.Idle, Plot.Phase);
        }

        [Test]
        public void CanContinue_FalseCases()
        {
            Layout(true, new[] { 0, 0 });
            Until(() => Plot.Phase == PlotPhase.Water, 4000, "Water");
            Assert.IsFalse(sim.CanContinue(id));
            Until(() => Plot.Phase == PlotPhase.Growing, 2000, "Growing");
            Assert.IsFalse(sim.CanContinue(id));
            Until(() => Plot.Phase == PlotPhase.Harvest, 4000, "Harvest");
            Assert.IsTrue(sim.PlayerHarvest(id, bag));
            Assert.AreEqual(PlotPhase.Till, Plot.Phase);
            Assert.IsTrue(sim.PauseTill(id));
            Assert.IsFalse(sim.CanContinue(id));
            Assert.IsFalse(sim.CanContinue(-1));
            Assert.IsFalse(sim.CanContinue(99));
            Layout(true); // ô mới đánh dấu (chưa xới) mới bỏ đánh dấu được
            Assert.IsTrue(sim.Unmark(id));
            Assert.IsTrue(Plot.Removed);
            Assert.IsFalse(sim.CanContinue(id));
        }

        Simulation Scenario()
        {
            Layout(true);
            return sim;
        }

        [Test]
        public void Deterministic()
        {
            var a = Scenario(); var ab = bag; var ai = id;
            var b = Scenario(); var bb = bag; var bi = id;
            for (int t = 1; t <= 4000; t++)
            {
                foreach (var (s, pb, pid) in new[] { (a, ab, ai), (b, bb, bi) })
                {
                    if (t == 1) { s.PlayerTill(pid); pb.AddSeeds(0, 1); s.PlayerSow(pid, 0, pb); }
                    if (s.CanContinue(pid)) s.Continue(pid);
                    s.Tick();
                }
                Assert.AreEqual(SimHash.Compute(a.State), SimHash.Compute(b.State), "tick " + t);
            }
        }
    }
}
