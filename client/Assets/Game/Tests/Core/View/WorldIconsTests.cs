using Game.Core.Sim;
using Game.Core.View;
using NUnit.Framework;

namespace Game.Core.Tests
{
    public class WorldIconsTests
    {
        static SimState NewState(out Creature c, out WorkStage stage, WorkAbility ability = WorkAbility.Till)
        {
            var st = new SimState(new SimRng(1UL, 1UL));
            c = new Creature(0, "L1", WorkAbility.Till, WorkAbility.Sow, 100, 100, 150, 1, 1, 1, new CellPos(3, 1, 4));
            st.Creatures.Add(c);
            stage = new WorkStage(0, 0, ability, new CellPos(5, 1, 5), false, default, 60, 0);
            st.Stages.Add(stage);
            return st;
        }

        static FarmPlot Plot(PlotPhase phase, bool tilled = true)
        {
            return new FarmPlot(0, 0, new CellPos(5, 0, 5)) { Phase = phase, Tilled = tilled, CurrentCrop = 1 };
        }

        [Test]
        public void PlotRemovedNone()
        {
            var st = NewState(out _, out _);
            var p = Plot(PlotPhase.Growing);
            p.Removed = true;
            Assert.AreEqual(WorldIconKind.None, WorldIcons.ForPlot(p, st, new SimConfig()).Kind);
        }

        [Test]
        public void PlotIdleUntilledNone()
        {
            var st = NewState(out _, out _);
            Assert.AreEqual(WorldIconKind.None, WorldIcons.ForPlot(Plot(PlotPhase.Idle, false), st, new SimConfig()).Kind);
        }

        [Test]
        public void PlotIdleTilledEmpty()
        {
            var st = NewState(out _, out _);
            var i = WorldIcons.ForPlot(Plot(PlotPhase.Idle), st, new SimConfig());
            Assert.AreEqual(WorldIconKind.Empty, i.Kind);
            Assert.AreEqual(-1, i.Percent);
        }

        [Test]
        public void PlotTillPercentAndPausedDim()
        {
            var st = NewState(out _, out var stage);
            stage.WorkDone = 27000;
            var p = Plot(PlotPhase.Till);
            p.StageId = 0;
            var i = WorldIcons.ForPlot(p, st, new SimConfig());
            Assert.AreEqual(WorldIconKind.Till, i.Kind);
            Assert.AreEqual(45, i.Percent);
            Assert.IsFalse(i.Dim);
            p.TillPaused = true;
            Assert.IsTrue(WorldIcons.ForPlot(p, st, new SimConfig()).Dim);
        }

        [Test]
        public void PlotGrowingPercent()
        {
            var st = NewState(out _, out _);
            var cfg = new SimConfig();
            var p = Plot(PlotPhase.Growing);
            p.RipeTick = st.Tick + cfg.GrowTicks(1) * 60 / 100;
            var i = WorldIcons.ForPlot(p, st, cfg);
            Assert.AreEqual(WorldIconKind.Grow, i.Kind);
            Assert.AreEqual(40, i.Percent);
            Assert.AreEqual(1, i.Crop);
        }

        [Test]
        public void PlotHarvestRipe()
        {
            var st = NewState(out _, out _);
            var i = WorldIcons.ForPlot(Plot(PlotPhase.Harvest), st, new SimConfig());
            Assert.AreEqual(WorldIconKind.Ripe, i.Kind);
            Assert.AreEqual(1, i.Crop);
        }

        [Test]
        public void PlotSowWaterKinds()
        {
            var st = NewState(out _, out _);
            Assert.AreEqual(WorldIconKind.Sow, WorldIcons.ForPlot(Plot(PlotPhase.Sow), st, new SimConfig()).Kind);
            Assert.AreEqual(WorldIconKind.Water, WorldIcons.ForPlot(Plot(PlotPhase.Water), st, new SimConfig()).Kind);
        }

        [Test]
        public void CreatureWorkingAbilityPercent()
        {
            var st = NewState(out var c, out var stage);
            c.State = CreatureState.Working; c.StageId = 0; stage.WorkDone = 27000;
            var i = WorldIcons.ForCreature(st, c);
            Assert.AreEqual(WorldIconKind.Till, i.Kind);
            Assert.AreEqual(45, i.Percent);
            st = NewState(out c, out stage, WorkAbility.Haul);
            c.State = CreatureState.Working; c.StageId = 0;
            Assert.AreEqual(WorldIconKind.Haul, WorldIcons.ForCreature(st, c).Kind);
        }

        [Test]
        public void CreatureMovingToStageDim()
        {
            var st = NewState(out var c, out _, WorkAbility.Sow);
            c.State = CreatureState.Moving; c.StageId = 0;
            var i = WorldIcons.ForCreature(st, c);
            Assert.AreEqual(WorldIconKind.Sow, i.Kind);
            Assert.AreEqual(-1, i.Percent);
            Assert.IsTrue(i.Dim);
        }

        [Test]
        public void CreatureWanderNone()
        {
            var st = NewState(out var c, out _);
            c.State = CreatureState.Moving; c.StageId = -1;
            Assert.AreEqual(WorldIconKind.None, WorldIcons.ForCreature(st, c).Kind);
        }

        [Test]
        public void CreatureIdle()
        {
            var st = NewState(out var c, out _);
            c.State = CreatureState.Idle;
            Assert.AreEqual(WorldIconKind.Idle, WorldIcons.ForCreature(st, c).Kind);
        }

        [Test]
        public void IconEquality()
        {
            var a = new WorldIcon(WorldIconKind.Grow, 40, 1);
            Assert.IsTrue(a == new WorldIcon(WorldIconKind.Grow, 40, 1));
            Assert.IsTrue(a != new WorldIcon(WorldIconKind.Grow, 41, 1));
            Assert.AreEqual(WorldIconKind.Stall, WorldIcons.ForStall().Kind);
        }
    }
}
