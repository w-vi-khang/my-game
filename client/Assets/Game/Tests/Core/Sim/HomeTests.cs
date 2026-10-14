using System;
using System.Collections.Generic;
using Game.Core.Sim;
using NUnit.Framework;

namespace Game.Core.Tests
{
    public class HomeTests
    {
        static void RunTo(Simulation sim, long tick)
        {
            while (sim.State.Tick < tick) sim.Tick();
        }

        static int NewOrder(Simulation sim) => sim.AddOrder(new List<CellPos>());

        static void Stone(WorldData w, int x, int y, int z) => w.Set(x, y, z, Cell.Make(BlockMaterial.Stone));

        /// <summary>Hố: Stone 2 tầng (y = 2, 3) quanh (3,·,3) — sinh vật ở (3,2,3) không đi đâu được.</summary>
        static void Hole(WorldData w)
        {
            foreach (var (x, z) in new[] { (4, 3), (2, 3), (3, 4), (3, 2) })
            {
                Stone(w, x, 2, z);
                Stone(w, x, 3, z);
            }
        }

        static void AssertEvent(SimEvent e, SimEventKind kind, long tick, int stageId, int creatureId, CellPos cell, StallReason reason)
        {
            Assert.AreEqual(kind, e.Kind);
            Assert.AreEqual(tick, e.Tick);
            Assert.AreEqual(stageId, e.StageId);
            Assert.AreEqual(creatureId, e.CreatureId);
            Assert.AreEqual(cell, e.Cell);
            Assert.AreEqual(reason, e.Reason);
        }

        [Test]
        public void HouseBlocksAndValidates()
        {
            var w = TestWorlds.Flat(1);
            var sim = new Simulation(new SimConfig(), w);
            var inv = new MaterialInventory();
            inv.Add(BlockMaterial.Dirt);
            var editor = new WorldEditor(w, inv, 1) { IsOccupied = sim.IsOccupied };

            Assert.AreEqual(0, sim.AddHouse(new CellPos(5, 2, 5)));
            Assert.IsTrue(sim.IsOccupied(5, 2, 5));
            Assert.IsTrue(sim.IsOccupied(6, 3, 6));
            Assert.IsFalse(sim.IsOccupied(7, 2, 5));
            Assert.IsFalse(sim.IsOccupied(5, 4, 5));
            Assert.AreEqual(EditResult.Occupied, editor.TryPlace(6, 2, 6, BlockMaterial.Dirt));
            Assert.Throws<ArgumentException>(() => sim.AddCreature("L1", new CellPos(6, 2, 5)));
            Assert.Throws<ArgumentException>(() => sim.AddHouse(new CellPos(6, 2, 6)));
            Assert.Throws<ArgumentException>(() => sim.AddHouse(new CellPos(5, 3, 9)));
            Assert.Throws<ArgumentException>(() => sim.AddStorage(new CellPos(6, 2, 5)));
            Assert.Throws<ArgumentException>(() => sim.AddHouse(new CellPos(15, 2, 15)));
            Assert.AreEqual(1, sim.State.Houses.Count);

            sim.AddCreature("L1", new CellPos(3, 2, 5));
            sim.AddCreature("L2", new CellPos(3, 2, 7));
            sim.AddCreature("L3", new CellPos(3, 2, 9));
            sim.AssignHome(0, 0);
            sim.AssignHome(1, 0);
            Assert.Throws<ArgumentException>(() => sim.AssignHome(2, 0));
            Assert.Throws<ArgumentException>(() => sim.AssignHome(0, 0));
            Assert.Throws<ArgumentException>(() => sim.AssignHome(2, 5));
            Assert.Throws<ArgumentException>(() => sim.AssignHome(9, 0));
            CollectionAssert.AreEqual(new[] { 0, 1 }, sim.State.Houses[0].Residents);
            Assert.AreEqual(-1, sim.State.Creatures[2].HomeId);

            var stage = sim.State.Stages[sim.AddStage(NewOrder(sim), WorkAbility.Till, new CellPos(8, 2, 5))];
            sim.Tick();
            Assert.AreEqual(CreatureState.Moving, sim.State.Creatures[0].State);
            Assert.AreEqual(7, sim.State.Creatures[0].Path.Count);
            RunTo(sim, 107);
            Assert.AreNotEqual(StageStatus.Done, stage.Status);
            sim.Tick();
            Assert.AreEqual(StageStatus.Done, stage.Status);
        }

        [Test]
        public void NoHomeNoWork()
        {
            var sim = new Simulation(new SimConfig(), TestWorlds.Flat(1));
            sim.AddCreature("L1", new CellPos(2, 2, 2));
            int s = sim.AddStage(NewOrder(sim), WorkAbility.Till, new CellPos(4, 2, 2));
            var stage = sim.State.Stages[s];
            sim.Tick();
            Assert.AreEqual(StageStatus.Blocked, stage.Status);
            Assert.AreEqual(StallReason.NoAbility, stage.Stall);
            Assert.AreEqual(1, sim.State.Events.Count);
            AssertEvent(sim.State.Events[0], SimEventKind.StallStarted, 1, s, -1, new CellPos(4, 2, 2), StallReason.NoAbility);
            RunTo(sim, 10);
            Assert.AreEqual(1, sim.State.Events.Count);
            Assert.AreEqual(CreatureState.Idle, sim.State.Creatures[0].State);

            sim.AddHouse(new CellPos(10, 2, 10));
            sim.AssignHome(0, 0);
            sim.Tick();
            Assert.AreEqual(StageStatus.Assigned, stage.Status);
            Assert.AreEqual(CreatureState.Moving, sim.State.Creatures[0].State);
            Assert.AreEqual(2, sim.State.Events.Count);
            AssertEvent(sim.State.Events[1], SimEventKind.StallCleared, 11, s, -1, new CellPos(4, 2, 2), StallReason.NoAbility);
            RunTo(sim, 84);
            Assert.AreNotEqual(StageStatus.Done, stage.Status);
            sim.Tick();
            Assert.AreEqual(StageStatus.Done, stage.Status);
        }

        [Test]
        public void NoAbilityCountsHomedOwnersOnly()
        {
            var sim = new Simulation(new SimConfig(), TestWorlds.Flat(1));
            sim.AddHouse(new CellPos(10, 2, 10));
            sim.AddCreature("L1", new CellPos(2, 2, 2));
            sim.AssignHome(0, 0);
            int o = NewOrder(sim);
            int s0 = sim.AddStage(o, WorkAbility.Water, new CellPos(4, 2, 2));
            int s1 = sim.AddStage(o, WorkAbility.Till, new CellPos(2, 2, 6));
            int s2 = sim.AddStage(o, WorkAbility.Till, new CellPos(2, 2, 9));
            var st = sim.State.Stages;
            sim.Tick();
            Assert.AreEqual(StageStatus.Blocked, st[s0].Status);
            Assert.AreEqual(StallReason.NoAbility, st[s0].Stall);
            Assert.AreEqual(StageStatus.Assigned, st[s1].Status);
            Assert.AreEqual(0, st[s1].AssigneeId);
            Assert.AreEqual(StageStatus.Open, st[s2].Status);
            Assert.AreEqual(StallReason.None, st[s2].Stall);
            Assert.AreEqual(1, sim.State.Events.Count);
            AssertEvent(sim.State.Events[0], SimEventKind.StallStarted, 1, s0, -1, new CellPos(4, 2, 2), StallReason.NoAbility);
            RunTo(sim, 10);
            Assert.AreEqual(1, sim.State.Events.Count);

            sim.AddCreature("L2", new CellPos(6, 2, 2));
            sim.AssignHome(1, 0);
            sim.Tick();
            Assert.AreEqual(StageStatus.Assigned, st[s0].Status);
            Assert.AreEqual(1, st[s0].AssigneeId);
            Assert.AreEqual(2, sim.State.Events.Count);
            AssertEvent(sim.State.Events[1], SimEventKind.StallCleared, 11, s0, -1, new CellPos(4, 2, 2), StallReason.NoAbility);
        }

        [Test]
        public void StuckIdleRespawnsAtHome()
        {
            var w = TestWorlds.Flat(1);
            Hole(w);
            var sim = new Simulation(new SimConfig(), w);
            sim.AddHouse(new CellPos(10, 2, 10));
            sim.AddCreature("L1", new CellPos(3, 2, 3));
            sim.AddCreature("L2", new CellPos(6, 2, 6));
            sim.AssignHome(0, 0);
            sim.AssignHome(1, 0);
            var l1 = sim.State.Creatures[0];
            var l2 = sim.State.Creatures[1];
            sim.Tick();
            Assert.IsFalse(l1.HomeReachable);
            Assert.AreEqual(1, l1.StuckSince);
            Assert.IsTrue(l2.HomeReachable);
            Assert.AreEqual(-1, l2.StuckSince);
            RunTo(sim, 300);
            Assert.AreEqual(new CellPos(3, 2, 3), l1.Cell);
            Assert.AreEqual(0, sim.State.Events.Count);
            sim.Tick();
            Assert.AreEqual(new CellPos(12, 2, 10), l1.Cell);
            Assert.AreEqual(CreatureState.Idle, l1.State);
            Assert.AreEqual(-1, l1.StuckSince);
            Assert.IsTrue(l1.HomeReachable);
            Assert.AreEqual(0, l1.Path.Count);
            Assert.AreEqual(1, sim.State.Events.Count);
            AssertEvent(sim.State.Events[0], SimEventKind.CreatureRespawned, 301, -1, 0, new CellPos(12, 2, 10), StallReason.None);
            RunTo(sim, 400);
            Assert.AreEqual(new CellPos(6, 2, 6), l2.Cell);
            Assert.AreEqual(-1, l2.StuckSince);
            Assert.AreEqual(1, sim.State.Events.Count);
        }

        [Test]
        public void WorkDoesNotCountAndWorkResets()
        {
            var w = TestWorlds.Flat(1);
            Hole(w);
            var sim = new Simulation(new SimConfig(), w);
            sim.AddHouse(new CellPos(10, 2, 10));
            sim.AddCreature("L1", new CellPos(3, 2, 3));
            sim.AssignHome(0, 0);
            var l1 = sim.State.Creatures[0];
            RunTo(sim, 100);
            Assert.AreEqual(1, l1.StuckSince);
            Assert.AreEqual(new CellPos(3, 2, 3), l1.Cell);
            var stage = sim.State.Stages[sim.AddStage(NewOrder(sim), WorkAbility.Till, new CellPos(3, 2, 3))];
            sim.Tick();
            Assert.AreEqual(CreatureState.Working, l1.State);
            Assert.AreEqual(-1, l1.StuckSince);
            RunTo(sim, 160);
            Assert.AreNotEqual(StageStatus.Done, stage.Status);
            sim.Tick();
            Assert.AreEqual(StageStatus.Done, stage.Status);
            Assert.AreEqual(CreatureState.Idle, l1.State);
            Assert.AreEqual(161, l1.StuckSince);
            RunTo(sim, 460);
            Assert.AreEqual(new CellPos(3, 2, 3), l1.Cell);
            sim.Tick();
            Assert.AreEqual(new CellPos(12, 2, 10), l1.Cell);
        }

        [Test]
        public void RespawnWaitsForStandableCell()
        {
            var w = TestWorlds.Flat(1);
            Hole(w);
            foreach (var (x, z) in new[] { (12, 10), (12, 11), (9, 10), (9, 11), (10, 12), (11, 12), (10, 9), (11, 9) })
            {
                Stone(w, x, 2, z);
                Stone(w, x, 3, z);
            }
            var sim = new Simulation(new SimConfig(), w);
            sim.AddHouse(new CellPos(10, 2, 10));
            sim.AddCreature("L1", new CellPos(3, 2, 3));
            sim.AssignHome(0, 0);
            var l1 = sim.State.Creatures[0];
            RunTo(sim, 305);
            Assert.AreEqual(new CellPos(3, 2, 3), l1.Cell);
            Assert.AreEqual(1, l1.StuckSince);
            Assert.AreEqual(0, sim.State.Events.Count);
            w.Set(12, 2, 10, Cell.Make(BlockMaterial.Empty));
            w.Set(12, 3, 10, Cell.Make(BlockMaterial.Empty));
            sim.OnCellChanged(12, 2, 10);
            sim.Tick();
            Assert.AreEqual(new CellPos(12, 2, 10), l1.Cell);
            Assert.AreEqual(1, sim.State.Events.Count);
            Assert.AreEqual(SimEventKind.CreatureRespawned, sim.State.Events[0].Kind);
            Assert.AreEqual(306, sim.State.Events[0].Tick);
        }

        [Test]
        public void DroppedLoadFollowsCreatureHome()
        {
            var w = TestWorlds.Flat(1);
            foreach (var (x, z) in new[] { (4, 3), (2, 3), (3, 4) })
            {
                Stone(w, x, 2, z);
                Stone(w, x, 3, z);
            }
            var sim = new Simulation(new SimConfig(), w);
            sim.AddHouse(new CellPos(10, 2, 10));
            sim.AddCreature("L3", new CellPos(3, 2, 3));
            sim.AssignHome(0, 0);
            int hid = sim.AddHaulStage(NewOrder(sim), new CellPos(3, 2, 3), new CellPos(3, 2, 0));
            var h = sim.State.Stages[hid];
            var l3 = sim.State.Creatures[0];
            sim.Tick();
            Assert.IsTrue(h.Carried);
            Assert.AreEqual(CreatureState.Moving, l3.State);
            Assert.AreEqual(3, l3.Path.Count);

            Stone(w, 3, 2, 2);
            Stone(w, 3, 3, 2);
            sim.OnCellChanged(3, 2, 2);
            sim.Tick();
            Assert.AreEqual(StageStatus.Blocked, h.Status);
            Assert.AreEqual(StallReason.NoPath, h.Stall);
            Assert.IsFalse(h.Carried);
            Assert.AreEqual(new CellPos(3, 2, 3), h.Target);
            Assert.AreEqual(0, h.DroppedBy);
            Assert.AreEqual(CreatureState.Idle, l3.State);
            Assert.IsFalse(l3.HomeReachable);
            Assert.AreEqual(2, l3.StuckSince);
            Assert.AreEqual(1, sim.State.Events.Count);
            AssertEvent(sim.State.Events[0], SimEventKind.StallStarted, 2, hid, -1, new CellPos(3, 2, 3), StallReason.NoPath);

            RunTo(sim, 301);
            Assert.AreEqual(new CellPos(3, 2, 3), l3.Cell);
            sim.Tick();
            Assert.AreEqual(new CellPos(12, 2, 10), l3.Cell);
            Assert.AreEqual(StageStatus.Open, h.Status);
            Assert.AreEqual(StallReason.None, h.Stall);
            Assert.AreEqual(new CellPos(12, 2, 10), h.Target);
            Assert.AreEqual(-1, h.DroppedBy);
            Assert.AreEqual(3, sim.State.Events.Count);
            AssertEvent(sim.State.Events[1], SimEventKind.CreatureRespawned, 302, -1, 0, new CellPos(12, 2, 10), StallReason.None);
            AssertEvent(sim.State.Events[2], SimEventKind.StallCleared, 302, hid, -1, new CellPos(12, 2, 10), StallReason.NoPath);

            sim.Tick();
            Assert.AreEqual(StageStatus.Assigned, h.Status);
            Assert.IsTrue(h.Carried);
            Assert.AreEqual(19, l3.Path.Count);
            RunTo(sim, 417);
            Assert.AreNotEqual(StageStatus.Done, h.Status);
            sim.Tick();
            Assert.AreEqual(StageStatus.Done, h.Status);
            Assert.AreEqual(new CellPos(3, 2, 0), l3.Cell);
        }
    }
}
