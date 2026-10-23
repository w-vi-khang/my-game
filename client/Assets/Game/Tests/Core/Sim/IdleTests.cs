using System;
using System.Collections.Generic;
using Game.Core.Sim;
using NUnit.Framework;

namespace Game.Core.Tests
{
    /// <summary>Sim bước 6b phần 2 (0019 mục 2–3): sinh vật rảnh về quanh nhà, lang thang, không dừng trên ô ruộng.</summary>
    public class IdleTests
    {
        static void RunTo(Simulation sim, long tick)
        {
            while (sim.State.Tick < tick) sim.Tick();
        }

        static SimConfig Cfg(int radius, int wanderMs)
        {
            var c = new SimConfig();
            c.creature.homeRadius = radius;
            c.creature.wanderMinMs = wanderMs;
            c.creature.wanderMaxMs = wanderMs;
            return c;
        }

        /// <summary>Flat(1) + nhà id 0 tại (10,2,10) (x 10–11, z 10–11). Cột Stone y 1..3 trên mọi ô (x 9–12, z 9–12) ngoài nhà, trừ các ô (x, z) trong open.</summary>
        static Simulation Ring(SimConfig cfg, params (int x, int z)[] open)
        {
            var w = TestWorlds.Flat(1);
            for (int x = 9; x <= 12; x++)
                for (int z = 9; z <= 12; z++)
                {
                    bool inHouse = x >= 10 && x <= 11 && z >= 10 && z <= 11;
                    if (inHouse || Array.IndexOf(open, (x, z)) >= 0) continue;
                    TestWorlds.Pillar(w, x, z, 3);
                }
            var sim = new Simulation(cfg, w);
            sim.AddHouse(new CellPos(10, 2, 10));
            return sim;
        }

        static int NewOrder(Simulation sim) => sim.AddOrder(new List<CellPos>());

        static int Overlaps(SimState s)
        {
            int n = 0;
            var cs = s.Creatures;
            for (int i = 0; i < cs.Count; i++)
                for (int j = i + 1; j < cs.Count; j++)
                {
                    var a = cs[i];
                    var b = cs[j];
                    if (a.Phasing || b.Phasing) continue;
                    if (Simulation.FramesOverlap(a.Cell, a.SizeX, a.SizeZ, a.SizeY, b.Cell, b.SizeX, b.SizeZ, b.SizeY)) n++;
                }
            return n;
        }

        static bool Done(Simulation sim, int stage) => sim.State.Stages[stage].Status == StageStatus.Done;

        [Test]
        public void FarIdleWalksBackIntoRadius()
        {
            var sim = Ring(Cfg(1, 5000), (12, 10));
            sim.AddCreature("L1", new CellPos(15, 2, 10));
            sim.AssignHome(0, 0);
            var c = sim.State.Creatures[0];
            RunTo(sim, 1);
            Assert.AreEqual(CreatureState.Moving, c.State);
            Assert.AreEqual(new CellPos(12, 2, 10), c.MoveTarget);
            Assert.AreEqual(3, c.Path.Count);
            Assert.AreEqual(-1, c.WanderAt);
            RunTo(sim, 20);
            Assert.AreEqual(new CellPos(13, 2, 10), c.Cell);
            Assert.AreEqual(CreatureState.Moving, c.State);
            RunTo(sim, 21);
            Assert.AreEqual(new CellPos(12, 2, 10), c.Cell);
            Assert.AreEqual(CreatureState.Idle, c.State);
            Assert.AreEqual(71, c.WanderAt);
            RunTo(sim, 70);
            Assert.AreEqual(CreatureState.Idle, c.State);
            Assert.AreEqual(71, c.WanderAt);
            RunTo(sim, 71);
            Assert.AreEqual(CreatureState.Idle, c.State);
            Assert.AreEqual(new CellPos(12, 2, 10), c.Cell);
            Assert.AreEqual(121, c.WanderAt);
        }

        [Test]
        public void WandersOnTimerWithinRadius()
        {
            var sim = Ring(Cfg(1, 5000), (12, 10), (12, 11));
            sim.AddCreature("L1", new CellPos(12, 2, 10));
            sim.AssignHome(0, 0);
            var c = sim.State.Creatures[0];
            RunTo(sim, 50);
            Assert.AreEqual(CreatureState.Idle, c.State);
            Assert.AreEqual(new CellPos(12, 2, 10), c.Cell);
            Assert.AreEqual(51, c.WanderAt);
            RunTo(sim, 51);
            Assert.AreEqual(CreatureState.Moving, c.State);
            Assert.AreEqual(-1, c.StageId);
            Assert.AreEqual(new CellPos(12, 2, 11), c.MoveTarget);
            RunTo(sim, 57);
            Assert.AreEqual(new CellPos(12, 2, 10), c.Cell);
            Assert.AreEqual(CreatureState.Moving, c.State);
            RunTo(sim, 58);
            Assert.AreEqual(new CellPos(12, 2, 11), c.Cell);
            Assert.AreEqual(CreatureState.Idle, c.State);
            Assert.AreEqual(108, c.WanderAt);
            RunTo(sim, 108);
            Assert.AreEqual(CreatureState.Moving, c.State);
            Assert.AreEqual(new CellPos(12, 2, 10), c.MoveTarget);
            RunTo(sim, 115);
            Assert.AreEqual(new CellPos(12, 2, 10), c.Cell);
            Assert.AreEqual(CreatureState.Idle, c.State);
            Assert.AreEqual(165, c.WanderAt);
        }

        [Test]
        public void IdleOnPlotLeavesAtOnce()
        {
            var sim = Ring(Cfg(1, 5000), (12, 10), (12, 11));
            int k = sim.AddStorage(new CellPos(1, 2, 1));
            foreach (var soilCell in new List<CellPos> { new CellPos(12, 1, 11) }) sim.EnqueueCrop(sim.MarkTill(soilCell), 0);
            sim.AddCreature("L2", new CellPos(12, 2, 11));
            sim.AssignHome(0, 0);
            var c = sim.State.Creatures[0];
            while (sim.State.Tick < 58)
            {
                sim.Tick();
                Assert.IsFalse(c.State == CreatureState.Idle && c.Cell == new CellPos(12, 2, 11), "idle on a plot cell at tick " + sim.State.Tick);
                switch (sim.State.Tick)
                {
                    case 1:
                        Assert.AreEqual(CreatureState.Moving, c.State);
                        Assert.AreEqual(new CellPos(12, 2, 10), c.MoveTarget);
                        break;
                    case 8:
                        Assert.AreEqual(new CellPos(12, 2, 10), c.Cell);
                        Assert.AreEqual(CreatureState.Idle, c.State);
                        Assert.AreEqual(58, c.WanderAt);
                        break;
                    case 58:
                        Assert.AreEqual(CreatureState.Idle, c.State);
                        Assert.AreEqual(new CellPos(12, 2, 10), c.Cell);
                        Assert.AreEqual(108, c.WanderAt);
                        break;
                }
            }
        }

        static Simulation WorkWorld(bool blockSouth, out WorkStage till)
        {
            var w = TestWorlds.Flat(1);
            TestWorlds.Pillar(w, 4, 5, 3);
            TestWorlds.Pillar(w, 5, 4, 3);
            if (blockSouth) TestWorlds.Pillar(w, 5, 6, 3);
            var sim = new Simulation(new SimConfig(), w);
            sim.AddCreature("L1", new CellPos(8, 2, 5));
            int k = sim.AddStorage(new CellPos(1, 2, 1));
            foreach (var soilCell in new List<CellPos> { new CellPos(5, 1, 5), new CellPos(6, 1, 5) }) sim.EnqueueCrop(sim.MarkTill(soilCell), 0);
            TestWorlds.HomeAll(sim);
            till = sim.State.Stages[sim.State.Plots[0].StageId];
            return sim;
        }

        [Test]
        public void WorkStandsOffOtherPlot()
        {
            var sim = WorkWorld(false, out var till);
            var c = sim.State.Creatures[0];
            RunTo(sim, 1);
            Assert.AreEqual(new CellPos(5, 2, 6), c.MoveTarget);
            Assert.AreEqual(4, c.Path.Count);
            RunTo(sim, 27);
            Assert.AreEqual(CreatureState.Moving, c.State);
            RunTo(sim, 28);
            Assert.AreEqual(new CellPos(5, 2, 6), c.Cell);
            Assert.AreEqual(CreatureState.Working, c.State);
            RunTo(sim, 87);
            Assert.AreNotEqual(StageStatus.Done, till.Status);
            RunTo(sim, 88);
            Assert.AreEqual(StageStatus.Done, till.Status);
        }

        [Test]
        public void WorkStandsOnOtherPlotWhenOnlyChoice()
        {
            var sim = WorkWorld(true, out var till);
            var c = sim.State.Creatures[0];
            RunTo(sim, 1);
            Assert.AreEqual(new CellPos(6, 2, 5), c.MoveTarget);
            Assert.AreEqual(2, c.Path.Count);
            RunTo(sim, 15);
            Assert.AreEqual(new CellPos(6, 2, 5), c.Cell);
            Assert.AreEqual(CreatureState.Working, c.State);
            RunTo(sim, 74);
            Assert.AreNotEqual(StageStatus.Done, till.Status);
            RunTo(sim, 75);
            Assert.AreEqual(StageStatus.Done, till.Status);
        }

        [Test]
        public void IdleDodgeSkipsPlot()
        {
            var sim = new Simulation(TestWorlds.NoWander(), TestWorlds.Flat(1));
            sim.AddCreature("L1", new CellPos(2, 2, 5));
            sim.AddCreature("L2", new CellPos(5, 2, 5));
            int s0 = sim.AddStage(NewOrder(sim), WorkAbility.Till, new CellPos(8, 2, 5));
            int k = sim.AddStorage(new CellPos(1, 2, 1));
            foreach (var soilCell in new List<CellPos> { new CellPos(5, 1, 6) }) sim.EnqueueCrop(sim.MarkTill(soilCell), 0);
            TestWorlds.HomeAll(sim);
            var m = sim.State.Creatures[0];
            var idle = sim.State.Creatures[1];
            int maxWait = 0;
            while (sim.State.Tick < 109)
            {
                sim.Tick();
                Assert.AreEqual(0, Overlaps(sim.State), "overlapping cells at tick " + sim.State.Tick);
                maxWait = Math.Max(maxWait, m.BlockedTicks);
                switch (sim.State.Tick)
                {
                    case 15:
                        Assert.AreEqual(new CellPos(4, 2, 5), m.Cell);
                        Assert.AreEqual(CreatureState.Idle, idle.State);
                        break;
                    case 16:
                        Assert.AreEqual(CreatureState.Moving, idle.State);
                        Assert.AreEqual(-1, idle.StageId);
                        Assert.AreEqual(new CellPos(5, 2, 4), idle.MoveTarget);
                        Assert.AreEqual(1, m.BlockedTicks);
                        break;
                    case 22:
                        Assert.AreEqual(new CellPos(5, 2, 4), idle.Cell);
                        Assert.AreEqual(CreatureState.Idle, idle.State);
                        Assert.AreEqual(7, m.BlockedTicks);
                        break;
                    case 23:
                        Assert.AreEqual(0, m.BlockedTicks);
                        break;
                    case 49:
                        Assert.AreEqual(new CellPos(8, 2, 5), m.Cell);
                        Assert.AreEqual(CreatureState.Working, m.State);
                        break;
                    case 108:
                        Assert.IsFalse(Done(sim, s0));
                        break;
                    case 109:
                        Assert.IsTrue(Done(sim, s0));
                        break;
                }
            }
            Assert.AreEqual(7, maxWait);
        }

        [Test]
        public void IdleRoamsInsideRadiusOffPlots()
        {
            var sim = new Simulation(new SimConfig(), TestWorlds.Flat(1));
            int h = sim.AddHouse(new CellPos(7, 2, 7));
            int k = sim.AddStorage(new CellPos(1, 2, 14));
            foreach (var soilCell in new List<CellPos> { new CellPos(4, 1, 4), new CellPos(5, 1, 4), new CellPos(4, 1, 5) }) sim.EnqueueCrop(sim.MarkTill(soilCell), 0);
            sim.AddCreature("L2", new CellPos(1, 2, 1));
            sim.AddCreature("L2", new CellPos(2, 2, 1));
            sim.AssignHome(0, h);
            sim.AssignHome(1, h);
            var house = sim.State.Houses[h];
            var cs = sim.State.Creatures;
            var prev = new CreatureState[cs.Count];
            var idleSince = new long[cs.Count];
            var starts = new int[cs.Count];
            var gaps = new HashSet<long>();
            for (int i = 0; i < cs.Count; i++) idleSince[i] = -1;
            for (int t = 0; t < 3000; t++)
            {
                sim.Tick();
                long tick = sim.State.Tick;
                Assert.AreEqual(0, Overlaps(sim.State), "overlapping cells at tick " + tick);
                for (int i = 0; i < cs.Count; i++)
                {
                    var c = cs[i];
                    if (c.State == CreatureState.Idle)
                    {
                        Assert.LessOrEqual(Simulation.HomeDistance(c.Cell, 1, 1, house), 4, "idle outside radius, tick " + tick);
                        Assert.IsFalse(sim.OnPlot(c.Cell, 1, 1, 1), "idle on plot, tick " + tick);
                    }
                    else if (c.State == CreatureState.Moving)
                    {
                        Assert.LessOrEqual(Simulation.HomeDistance(c.MoveTarget, 1, 1, house), 4, "target outside radius, tick " + tick);
                        Assert.IsFalse(sim.OnPlot(c.MoveTarget, 1, 1, 1), "target is a plot, tick " + tick);
                    }
                    if (prev[i] != CreatureState.Idle && c.State == CreatureState.Idle) idleSince[i] = tick;
                    else if (prev[i] == CreatureState.Idle && c.State == CreatureState.Moving && idleSince[i] >= 0)
                    {
                        long gap = tick - idleSince[i];
                        Assert.GreaterOrEqual(gap, 50, "rest too short, tick " + tick);
                        Assert.LessOrEqual(gap, 100, "rest too long, tick " + tick);
                        gaps.Add(gap);
                        starts[i]++;
                    }
                    prev[i] = c.State;
                }
            }
            TestContext.WriteLine("starts: " + starts[0] + ", " + starts[1] + "; different gap values: " + gaps.Count);
            Assert.GreaterOrEqual(starts[0], 10);
            Assert.GreaterOrEqual(starts[1], 10);
            Assert.GreaterOrEqual(gaps.Count, 2);
        }
    }
}
