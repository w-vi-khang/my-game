using System;
using System.Collections.Generic;
using Game.Core.Sim;
using NUnit.Framework;

namespace Game.Core.Tests
{
    public class FarmTests
    {
        static readonly CellPos Site = new CellPos(5, 2, 5);
        static readonly CellPos Kho = new CellPos(6, 2, 9);

        static void Run(Simulation sim, int n)
        {
            for (int i = 0; i < n; i++) sim.Tick();
        }

        static void RunTo(Simulation sim, long tick)
        {
            while (sim.State.Tick < tick) sim.Tick();
        }

        static Simulation Layout(SimConfig cfg, bool withL3, WorldData world = null)
        {
            var sim = new Simulation(cfg, world ?? TestWorlds.Flat(1));
            sim.AddCreature("L1", new CellPos(8, 2, 5));
            sim.AddCreature("L2", new CellPos(5, 2, 1));
            if (withL3) sim.AddCreature("L3", new CellPos(9, 2, 5));
            int k = sim.AddStorage(Kho);
            sim.AddFarmOrder(new List<CellPos> { new CellPos(5, 1, 5) }, k);
            TestWorlds.HomeAll(sim);
            return sim;
        }

        static List<SimEvent> EventsOf(Simulation sim, int stageId)
        {
            var list = new List<SimEvent>();
            foreach (var e in sim.State.Events)
                if (e.StageId == stageId) list.Add(e);
            return list;
        }

        static void AssertEvent(SimEvent e, SimEventKind kind, long tick, int stageId, CellPos cell, StallReason reason)
        {
            Assert.AreEqual(kind, e.Kind);
            Assert.AreEqual(tick, e.Tick);
            Assert.AreEqual(stageId, e.StageId);
            Assert.AreEqual(cell, e.Cell);
            Assert.AreEqual(reason, e.Reason);
        }

        static WorkStage Stage(Simulation sim, WorkAbility a)
        {
            WorkStage found = null;
            foreach (var s in sim.State.Stages)
                if (s.Ability == a) found = s;
            return found;
        }

        [Test]
        public void OneHarvestReachesStorage()
        {
            var sim = Layout(new SimConfig(), true);
            var st = sim.State;
            var l1 = st.Creatures[0];
            var l2 = st.Creatures[1];
            var l3 = st.Creatures[2];
            var plot = st.Plots[0];
            var store = st.Storages[0];

            RunTo(sim, 15);
            Assert.AreEqual(new CellPos(6, 2, 5), l1.Cell);
            Assert.AreEqual(CreatureState.Working, l1.State);
            RunTo(sim, 74);
            Assert.AreEqual(PlotPhase.Till, plot.Phase);
            RunTo(sim, 75);
            Assert.AreEqual(PlotPhase.Sow, plot.Phase);
            Assert.AreEqual(9, store.Seeds);
            Assert.AreEqual(CreatureState.Working, l1.State);

            RunTo(sim, 105);
            Assert.AreEqual(PlotPhase.Water, plot.Phase);
            Assert.AreEqual(CreatureState.Moving, l2.State);
            RunTo(sim, 125);
            Assert.AreEqual(new CellPos(5, 2, 4), l2.Cell);
            Assert.AreEqual(CreatureState.Working, l2.State);
            RunTo(sim, 154);
            Assert.AreEqual(PlotPhase.Water, plot.Phase);
            RunTo(sim, 155);
            Assert.AreEqual(PlotPhase.Growing, plot.Phase);
            Assert.AreEqual(1355, plot.RipeTick);

            RunTo(sim, 1354);
            Assert.AreEqual(PlotPhase.Growing, plot.Phase);
            RunTo(sim, 1355);
            Assert.AreEqual(PlotPhase.Harvest, plot.Phase);
            Assert.AreEqual(CreatureState.Working, l2.State);

            RunTo(sim, 1395);
            Assert.AreEqual(PlotPhase.Idle, plot.Phase);
            Assert.AreEqual(0, plot.Queue.Count);
            var haul = st.Stages[st.Stages.Count - 1];
            Assert.AreEqual(WorkAbility.Haul, haul.Ability);
            Assert.AreEqual(StageStatus.Assigned, haul.Status);
            Assert.AreEqual(l3.Id, haul.AssigneeId);
            Assert.AreEqual(2, store.Reserved);

            RunTo(sim, 1417);
            Assert.AreEqual(new CellPos(6, 2, 5), l3.Cell);
            Assert.IsTrue(haul.Carried);

            RunTo(sim, 1451);
            Assert.AreEqual(0, store.Produce);
            RunTo(sim, 1452);
            Assert.AreEqual(StageStatus.Done, haul.Status);
            Assert.AreEqual(1, store.Produce);
            Assert.AreEqual(10, store.Seeds);
            Assert.AreEqual(0, store.Reserved);
            Assert.AreEqual(new CellPos(6, 2, 8), l3.Cell);
        }

        [Test]
        public void WithoutHaulerProduceStaysAtField()
        {
            var sim = Layout(new SimConfig(), false);
            Run(sim, 1500);
            var haul = Stage(sim, WorkAbility.Haul);
            Assert.AreEqual(StageStatus.Blocked, haul.Status);
            Assert.AreEqual(StallReason.NoAbility, haul.Stall);
            Assert.AreEqual(AssigneeKind.None, haul.AssigneeKind);
            Assert.AreEqual(Site, haul.Target);
            Assert.AreEqual(0, sim.State.Storages[0].Produce);
            Assert.AreEqual(9, sim.State.Storages[0].Seeds);
        }

        [Test]
        public void PrefersNeighborOverPlot()
        {
            var sim = new Simulation(new SimConfig(), TestWorlds.Flat(1));
            var l1 = sim.State.Creatures[sim.AddCreature("L1", Site)];
            int k = sim.AddStorage(new CellPos(10, 2, 10));
            sim.AddFarmOrder(new List<CellPos> { new CellPos(5, 1, 5) }, k);
            TestWorlds.HomeAll(sim);
            sim.Tick();
            Assert.AreEqual(CreatureState.Moving, l1.State);
            CollectionAssert.AreEqual(new[] { new CellPos(6, 2, 5) }, l1.Path);
            RunTo(sim, 8);
            Assert.AreEqual(new CellPos(6, 2, 5), l1.Cell);
            Assert.AreEqual(CreatureState.Working, l1.State);
            var till = Stage(sim, WorkAbility.Till);
            RunTo(sim, 67);
            Assert.AreNotEqual(StageStatus.Done, till.Status);
            sim.Tick();
            Assert.AreEqual(StageStatus.Done, till.Status);
        }

        [Test]
        public void StandsOnPlotWhenNoNeighbor()
        {
            var w = TestWorlds.Flat(1);
            foreach (var (x, z) in new[] { (6, 5), (4, 5), (5, 6), (5, 4) })
            {
                w.Set(x, 2, z, Cell.Make(BlockMaterial.Stone));
                w.Set(x, 3, z, Cell.Make(BlockMaterial.Stone));
            }
            var sim = new Simulation(new SimConfig(), w);
            var l1 = sim.State.Creatures[sim.AddCreature("L1", Site)];
            int k = sim.AddStorage(new CellPos(10, 2, 10));
            sim.AddFarmOrder(new List<CellPos> { new CellPos(5, 1, 5) }, k);
            TestWorlds.HomeAll(sim);
            sim.Tick();
            Assert.AreEqual(CreatureState.Working, l1.State);
            Assert.AreEqual(Site, l1.Cell);
            var till = Stage(sim, WorkAbility.Till);
            RunTo(sim, 60);
            Assert.AreNotEqual(StageStatus.Done, till.Status);
            sim.Tick();
            Assert.AreEqual(StageStatus.Done, till.Status);
        }

        [Test]
        public void QueueRepeatsAndCapsAtTwo()
        {
            var sim = Layout(new SimConfig(), true);
            Assert.IsTrue(sim.EnqueueCrop(0));
            Assert.IsFalse(sim.EnqueueCrop(0));
            var plot = sim.State.Plots[0];
            Assert.AreEqual(2, plot.Queue.Count);
            RunTo(sim, 1395);
            Assert.AreEqual(PlotPhase.Till, plot.Phase);
            Assert.AreEqual(1, plot.Queue.Count);
            var l1 = sim.State.Creatures[0];
            Assert.AreEqual(CreatureState.Working, l1.State);
            Assert.AreEqual(new CellPos(6, 2, 5), l1.Cell);
            RunTo(sim, 1455);
            Assert.AreEqual(PlotPhase.Sow, plot.Phase);
            Assert.AreEqual(8, sim.State.Storages[0].Seeds);
        }

        [Test]
        public void NoSeedsWaits()
        {
            var cfg = new SimConfig();
            cfg.storage.startSeeds = 0;
            var sim = Layout(cfg, false);
            RunTo(sim, 75);
            var sow = Stage(sim, WorkAbility.Sow);
            Assert.AreEqual(StageStatus.Blocked, sow.Status);
            Assert.AreEqual(StallReason.NoSeeds, sow.Stall);
            Assert.AreEqual(CreatureState.Idle, sim.State.Creatures[0].State);
            var ev = EventsOf(sim, sow.Id);
            Assert.AreEqual(1, ev.Count);
            AssertEvent(ev[0], SimEventKind.StallStarted, 75, sow.Id, Site, StallReason.NoSeeds);
            sim.State.Storages[0].Seeds = 1;
            sim.Tick();
            ev = EventsOf(sim, sow.Id);
            Assert.AreEqual(2, ev.Count);
            AssertEvent(ev[1], SimEventKind.StallCleared, 76, sow.Id, Site, StallReason.NoSeeds);
            Assert.AreEqual(StageStatus.Assigned, sow.Status);
            Assert.AreEqual(CreatureState.Working, sim.State.Creatures[0].State);
            Assert.AreEqual(0, sim.State.Storages[0].Seeds);
        }

        [Test]
        public void StorageFullWaits()
        {
            var cfg = new SimConfig();
            cfg.storage.capacity = 10;
            var sim = Layout(cfg, true);
            RunTo(sim, 1395);
            var haul = Stage(sim, WorkAbility.Haul);
            Assert.AreEqual(StageStatus.Blocked, haul.Status);
            Assert.AreEqual(StallReason.StorageFull, haul.Stall);
            Assert.AreEqual(CreatureState.Idle, sim.State.Creatures[2].State);
            Assert.AreEqual(0, sim.State.Storages[0].Reserved);
            var ev = EventsOf(sim, haul.Id);
            Assert.AreEqual(1, ev.Count);
            AssertEvent(ev[0], SimEventKind.StallStarted, 1395, haul.Id, Site, StallReason.StorageFull);
            sim.State.Storages[0].Seeds = 8;
            sim.Tick();
            ev = EventsOf(sim, haul.Id);
            Assert.AreEqual(2, ev.Count);
            AssertEvent(ev[1], SimEventKind.StallCleared, 1396, haul.Id, Site, StallReason.StorageFull);
            Assert.AreEqual(StageStatus.Assigned, haul.Status);
            Assert.AreEqual(2, sim.State.Storages[0].Reserved);
        }

        [Test]
        public void SowRefundsSeedWhenUnassigned()
        {
            var sim = Layout(new SimConfig(), true);
            RunTo(sim, 75);
            Assert.AreEqual(9, sim.State.Storages[0].Seeds);
            sim.Unassign(0);
            Assert.AreEqual(10, sim.State.Storages[0].Seeds);
            Assert.AreEqual(StageStatus.Open, Stage(sim, WorkAbility.Sow).Status);
        }

        [Test]
        public void DroppedLoadReleasesReservation()
        {
            var sim = Layout(new SimConfig(), true);
            RunTo(sim, 1422);
            var l3 = sim.State.Creatures[2];
            Assert.AreEqual(new CellPos(6, 2, 6), l3.Cell);
            sim.Unassign(2);
            var haul = Stage(sim, WorkAbility.Haul);
            Assert.AreEqual(0, sim.State.Storages[0].Reserved);
            Assert.AreEqual(StageStatus.Open, haul.Status);
            Assert.IsFalse(haul.Carried);
            Assert.AreEqual(new CellPos(6, 2, 6), haul.Target);
            sim.Tick();
            Assert.AreEqual(StageStatus.Assigned, haul.Status);
            Assert.AreEqual(2, sim.State.Storages[0].Reserved);
            RunTo(sim, 1600);
            Assert.AreEqual(StageStatus.Done, haul.Status);
            Assert.AreEqual(1, sim.State.Storages[0].Produce);
        }

        [Test]
        public void StorageBlocksPathAndPlacement()
        {
            var w = TestWorlds.Flat(1);
            var sim = new Simulation(new SimConfig(), w);
            var inv = new MaterialInventory();
            inv.Add(BlockMaterial.Dirt);
            var editor = new WorldEditor(w, inv, 1) { IsOccupied = sim.IsOccupied };
            sim.AddStorage(new CellPos(6, 2, 5));
            Assert.IsTrue(sim.IsOccupied(6, 2, 5));
            Assert.AreEqual(EditResult.Occupied, editor.TryPlace(6, 2, 5, BlockMaterial.Dirt));
            Assert.Throws<ArgumentException>(() => sim.AddCreature("L1", new CellPos(6, 2, 5)));
            Assert.Throws<ArgumentException>(() => sim.AddStorage(new CellPos(6, 2, 5)));
            var c = sim.State.Creatures[sim.AddCreature("L1", new CellPos(4, 2, 5))];
            var stage = sim.State.Stages[sim.AddStage(sim.AddOrder(new List<CellPos>()), WorkAbility.Till, new CellPos(8, 2, 5))];
            TestWorlds.HomeAll(sim);
            sim.Tick();
            Assert.AreEqual(6, c.Path.Count);
            RunTo(sim, 100);
            Assert.AreNotEqual(StageStatus.Done, stage.Status);
            sim.Tick();
            Assert.AreEqual(StageStatus.Done, stage.Status);
        }

        [Test]
        public void AddFarmOrderValidates()
        {
            var w = TestWorlds.Flat(1);
            var sim = new Simulation(new SimConfig(), w);
            int k = sim.AddStorage(new CellPos(10, 2, 10));
            int orders = sim.State.Orders.Count;
            int plots = sim.State.Plots.Count;

            Assert.Throws<ArgumentException>(() => sim.AddFarmOrder(new List<CellPos> { new CellPos(5, 0, 5) }, k));
            w.Set(5, 2, 5, Cell.Make(BlockMaterial.Stone));
            Assert.Throws<ArgumentException>(() => sim.AddFarmOrder(new List<CellPos> { new CellPos(5, 1, 5) }, k));
            Assert.Throws<ArgumentException>(() => sim.AddFarmOrder(new List<CellPos> { new CellPos(7, 1, 7) }, 5));
            Assert.Throws<ArgumentException>(() => sim.AddFarmOrder(new List<CellPos> { new CellPos(3, 1, 3), new CellPos(3, 1, 3) }, k));
            Assert.AreEqual(orders, sim.State.Orders.Count);
            Assert.AreEqual(plots, sim.State.Plots.Count);

            sim.AddFarmOrder(new List<CellPos> { new CellPos(7, 1, 7) }, k);
            Assert.AreEqual(orders + 1, sim.State.Orders.Count);
            Assert.AreEqual(plots + 1, sim.State.Plots.Count);
            Assert.Throws<ArgumentException>(() => sim.AddFarmOrder(new List<CellPos> { new CellPos(7, 1, 7) }, k));
            Assert.AreEqual(orders + 1, sim.State.Orders.Count);
            Assert.AreEqual(plots + 1, sim.State.Plots.Count);
        }
    }
}
