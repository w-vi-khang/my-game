using System;
using System.Collections.Generic;
using Game.Core.Sim;
using NUnit.Framework;

namespace Game.Core.Tests
{
    /// <summary>Sim bước 6b phần 1 (0019): sinh vật chặn nhau khi đi, nhường, đi xuyên.</summary>
    public class MovementTests
    {
        static int NewOrder(Simulation sim) => sim.AddOrder(new List<CellPos>());

        /// <summary>Số cặp sinh vật có khung (tại Cell) chồng nhau, bỏ qua cặp có con đang Phasing.</summary>
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

        /// <summary>Tick tới nhịp tick, sau mỗi nhịp assert không chồng ô.</summary>
        static void RunTo(Simulation sim, long tick)
        {
            while (sim.State.Tick < tick)
            {
                sim.Tick();
                Assert.AreEqual(0, Overlaps(sim.State), "chồng ô ở nhịp " + sim.State.Tick);
            }
        }

        /// <summary>Hành lang x 1..11, z = 5 (Flat(1)); A = L1 (id 0) ở (1,2,5) xới tại (11,2,5); B = L2 (id 1) ở (11,2,5) tưới tại (1,2,5).</summary>
        static Simulation HeadOn(int pocketX)
        {
            var w = TestWorlds.Flat(1);
            TestWorlds.Corridor(w, 1, 11, 5, pocketX);
            var sim = new Simulation(TestWorlds.NoWander(), w);
            sim.AddCreature("L1", new CellPos(1, 2, 5));
            sim.AddCreature("L2", new CellPos(11, 2, 5));
            int o = NewOrder(sim);
            sim.AddStage(o, WorkAbility.Till, new CellPos(11, 2, 5));
            sim.AddStage(o, WorkAbility.Water, new CellPos(1, 2, 5));
            TestWorlds.HomeAll(sim);
            return sim;
        }

        static bool Done(Simulation sim, int stage) => sim.State.Stages[stage].Status == StageStatus.Done;

        [Test]
        public void HeadOnWithPocketYields()
        {
            var sim = HeadOn(8);
            var a = sim.State.Creatures[0];
            var b = sim.State.Creatures[1];
            RunTo(sim, 37);
            Assert.AreEqual(-1, b.YieldTo);
            Assert.AreEqual(9, b.BlockedTicks);
            Assert.AreEqual(2, a.BlockedTicks);
            RunTo(sim, 38);
            Assert.AreEqual(new CellPos(6, 2, 5), a.Cell);
            Assert.AreEqual(new CellPos(7, 2, 5), b.Cell);
            Assert.AreEqual(0, b.YieldTo);
            Assert.AreEqual(new CellPos(8, 2, 6), b.MoveTarget);
            Assert.AreEqual(0, b.BlockedTicks);
            RunTo(sim, 52);
            Assert.AreEqual(new CellPos(8, 2, 6), b.Cell);
            Assert.IsTrue(b.YieldHold);
            Assert.AreEqual(new CellPos(7, 2, 5), a.Cell);
            Assert.IsFalse(a.Phasing || b.Phasing);
            RunTo(sim, 65);
            Assert.IsTrue(b.YieldHold);
            RunTo(sim, 66);
            Assert.AreEqual(-1, b.YieldTo);
            Assert.IsFalse(b.YieldHold);
            Assert.AreEqual(new CellPos(9, 2, 5), a.Cell);
            Assert.IsFalse(a.Phasing || b.Phasing);
            RunTo(sim, 138);
            Assert.IsFalse(Done(sim, 0));
            RunTo(sim, 139);
            Assert.IsTrue(Done(sim, 0));
            Assert.AreEqual(new CellPos(11, 2, 5), a.Cell);
            RunTo(sim, 148);
            Assert.IsFalse(Done(sim, 1));
            RunTo(sim, 149);
            Assert.IsTrue(Done(sim, 1));
            Assert.AreEqual(new CellPos(1, 2, 5), b.Cell);
            Assert.IsFalse(a.Phasing || b.Phasing);
        }

        [Test]
        public void HeadOnNoPocketPhasesAtFifty()
        {
            var sim = HeadOn(-1);
            var a = sim.State.Creatures[0];
            var b = sim.State.Creatures[1];
            RunTo(sim, 77);
            Assert.AreEqual(49, b.BlockedTicks);
            Assert.IsFalse(b.Phasing);
            Assert.AreEqual(42, a.BlockedTicks);
            RunTo(sim, 78);
            Assert.IsTrue(b.Phasing);
            Assert.IsFalse(a.Phasing);
            RunTo(sim, 84);
            Assert.AreEqual(a.Cell, b.Cell);
            Assert.AreEqual(new CellPos(6, 2, 5), b.Cell);
            Assert.AreEqual(49, a.BlockedTicks);
            Assert.IsFalse(a.Phasing);
            RunTo(sim, 91);
            Assert.IsFalse(b.Phasing);
            Assert.AreEqual(new CellPos(7, 2, 5), a.Cell);
            Assert.AreEqual(new CellPos(5, 2, 5), b.Cell);
            RunTo(sim, 146);
            Assert.IsFalse(Done(sim, 1));
            RunTo(sim, 147);
            Assert.IsTrue(Done(sim, 1));
            Assert.AreEqual(new CellPos(1, 2, 5), b.Cell);
            RunTo(sim, 177);
            Assert.IsFalse(Done(sim, 0));
            RunTo(sim, 178);
            Assert.IsTrue(Done(sim, 0));
            Assert.AreEqual(new CellPos(11, 2, 5), a.Cell);
        }

        [Test]
        public void WorkerBlockingWaitsTenThenDetours()
        {
            var sim = new Simulation(TestWorlds.NoWander(), TestWorlds.Flat(1));
            sim.AddCreature("L1", new CellPos(2, 2, 5));
            sim.AddCreature("L2", new CellPos(5, 2, 5));
            int o = NewOrder(sim);
            int s0 = sim.AddStage(o, WorkAbility.Till, new CellPos(8, 2, 5));
            int s1 = sim.AddStage(o, WorkAbility.Water, new CellPos(5, 2, 5));
            TestWorlds.HomeAll(sim);
            var m = sim.State.Creatures[0];
            var w = sim.State.Creatures[1];
            RunTo(sim, 1);
            Assert.AreEqual(CreatureState.Working, w.State);
            RunTo(sim, 15);
            Assert.AreEqual(new CellPos(4, 2, 5), m.Cell);
            RunTo(sim, 24);
            Assert.AreEqual(new CellPos(4, 2, 5), m.Cell);
            Assert.AreEqual(9, m.BlockedTicks);
            Assert.AreEqual(new CellPos(5, 2, 5), m.Path[m.PathIndex]);
            RunTo(sim, 25);
            Assert.AreEqual(0, m.BlockedTicks);
            Assert.AreEqual(0, m.PathIndex);
            CollectionAssert.AreEqual(new[]
            {
                new CellPos(4, 2, 6), new CellPos(5, 2, 6), new CellPos(6, 2, 6),
                new CellPos(7, 2, 6), new CellPos(8, 2, 6), new CellPos(8, 2, 5),
            }, m.Path);
            Assert.AreEqual(CreatureState.Working, w.State);
            RunTo(sim, 64);
            Assert.AreEqual(new CellPos(8, 2, 6), m.Cell);
            Assert.AreEqual(CreatureState.Moving, m.State);
            RunTo(sim, 65);
            Assert.AreEqual(new CellPos(8, 2, 5), m.Cell);
            Assert.AreEqual(CreatureState.Working, m.State);
            RunTo(sim, 124);
            Assert.IsFalse(Done(sim, s0));
            RunTo(sim, 125);
            Assert.IsTrue(Done(sim, s0));
        }

        [Test]
        public void IdleInPathDodges()
        {
            var sim = new Simulation(TestWorlds.NoWander(), TestWorlds.Flat(1));
            sim.AddCreature("L1", new CellPos(2, 2, 5));
            sim.AddCreature("L2", new CellPos(5, 2, 5));
            int s0 = sim.AddStage(NewOrder(sim), WorkAbility.Till, new CellPos(8, 2, 5));
            TestWorlds.HomeAll(sim);
            var m = sim.State.Creatures[0];
            var idle = sim.State.Creatures[1];
            int maxWait = 0;
            while (sim.State.Tick < 109)
            {
                sim.Tick();
                Assert.AreEqual(0, Overlaps(sim.State), "chồng ô ở nhịp " + sim.State.Tick);
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
                        Assert.AreEqual(new CellPos(5, 2, 6), idle.MoveTarget);
                        Assert.AreEqual(1, m.BlockedTicks);
                        break;
                    case 22:
                        Assert.AreEqual(new CellPos(5, 2, 6), idle.Cell);
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
        public void AddCreatureRejectsOverlap()
        {
            var sim = new Simulation(TestWorlds.NoWander(), TestWorlds.Flat(0));
            sim.AddCreature("L1", new CellPos(3, 1, 3));
            Assert.Throws<ArgumentException>(() => sim.AddCreature("L2", new CellPos(3, 1, 3)));
            Assert.DoesNotThrow(() => sim.AddCreature("L2", new CellPos(4, 1, 3)));
        }
    }
}
