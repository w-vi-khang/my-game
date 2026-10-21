using System.Collections.Generic;
using Game.Core.Sim;
using NUnit.Framework;

namespace Game.Core.Tests
{
    /// <summary>Ô ruộng có sau khi xới; đào/đặt/nước lên ô ruộng thì huỷ vụ (sim 6a, 0020).</summary>
    public class PlotRulesTests
    {
        static readonly CellPos Soil = new CellPos(5, 1, 5);
        static readonly CellPos Site = new CellPos(5, 2, 5);
        static readonly CellPos Kho = new CellPos(6, 2, 9);

        WorldData w;
        MaterialInventory inv;
        WorldEditor editor;
        Simulation sim;
        int soilChanges;

        static void RunTo(Simulation s, long tick)
        {
            while (s.State.Tick < tick) s.Tick();
        }

        void Build(bool withEditor = true, IReadOnlyList<CellPos> cells = null)
        {
            w = TestWorlds.Flat(1);
            sim = new Simulation(TestWorlds.NoWander(), w);
            inv = new MaterialInventory();
            editor = new WorldEditor(w, inv, 1);
            soilChanges = 0;
            editor.CellChanged += (x, y, z) => { if (x == 5 && y == 1 && z == 5) soilChanges++; };
            if (withEditor) sim.AttachEditor(editor);
            sim.AddCreature("L1", new CellPos(8, 2, 5));
            sim.AddCreature("L2", new CellPos(5, 2, 1));
            sim.AddCreature("L3", new CellPos(9, 2, 5));
            int k = sim.AddStorage(Kho);
            sim.AddFarmOrder(cells ?? new List<CellPos> { Soil }, k);
            TestWorlds.HomeAll(sim);
        }

        List<SimEvent> Cancels()
        {
            var l = new List<SimEvent>();
            foreach (var e in sim.State.Events)
                if (e.Kind == SimEventKind.CropCancelled) l.Add(e);
            return l;
        }

        [Test]
        public void TillTurnsSoilIntoFarmlandOnDoneTick()
        {
            Build();
            var plot = sim.State.Plots[0];
            var store = sim.State.Storages[0];
            RunTo(sim, 74);
            Assert.AreEqual(BlockMaterial.Dirt, w.GetMaterial(5, 1, 5));
            Assert.IsFalse(plot.Tilled);
            RunTo(sim, 75);
            Assert.AreEqual(BlockMaterial.Farmland, w.GetMaterial(5, 1, 5));
            Assert.IsTrue(plot.Tilled);
            Assert.AreEqual(1, soilChanges);
            RunTo(sim, 105);
            Assert.AreEqual(PlotPhase.Water, plot.Phase);
            RunTo(sim, 155);
            Assert.AreEqual(PlotPhase.Growing, plot.Phase);
            Assert.AreEqual(1355, plot.RipeTick);
            RunTo(sim, 1355);
            Assert.AreEqual(PlotPhase.Harvest, plot.Phase);
            RunTo(sim, 1459);
            Assert.AreEqual(1, store.Produce);
            Assert.AreEqual(10, store.Seeds);
        }

        [Test]
        public void RetillKeepsFarmlandWithoutEvent()
        {
            Build();
            var plot = sim.State.Plots[0];
            sim.EnqueueCrop(0);
            Assert.AreEqual(2, plot.Queue.Count);
            bool sawSow = false;
            while (sim.State.Tick < 1700)
            {
                sim.Tick();
                if (plot.Phase == PlotPhase.Sow && sim.State.Tick > 1400) { sawSow = true; break; }
            }
            Assert.IsTrue(sawSow);
            Assert.AreEqual(BlockMaterial.Farmland, w.GetMaterial(5, 1, 5));
            Assert.AreEqual(1, soilChanges);
        }

        [TestCase(40, 10, 10, 0)]
        [TestCase(90, 9, 10, 0)]
        [TestCase(140, 9, 10, 1)]
        [TestCase(500, 9, 10, -1)]
        [TestCase(1370, 9, 10, 1)]
        public void DigCancelsCropInEachPhase(int tick, int before, int after, int released)
        {
            Build();
            var plot = sim.State.Plots[0];
            var store = sim.State.Storages[0];
            RunTo(sim, tick);
            Assert.AreEqual(before, store.Seeds);
            int stageId = plot.StageId;
            Assert.AreEqual(EditResult.Ok, editor.TryDig(5, 1, 5));
            sim.Tick();
            Assert.IsTrue(plot.Removed);
            Assert.AreEqual(0, plot.Queue.Count);
            Assert.AreEqual(PlotPhase.Idle, plot.Phase);
            Assert.AreEqual(-1, plot.StageId);
            if (stageId >= 0) Assert.AreEqual(StageStatus.Cancelled, sim.State.Stages[stageId].Status);
            if (released >= 0)
            {
                var c = sim.State.Creatures[released];
                Assert.AreEqual(-1, c.StageId);
                Assert.AreNotEqual(CreatureState.Working, c.State);
            }
            Assert.AreEqual(after, store.Seeds);
            Assert.AreEqual(1, inv.Get(BlockMaterial.Dirt));
            Assert.AreEqual(BlockMaterial.Empty, w.GetMaterial(5, 1, 5));
            var ev = Cancels();
            Assert.AreEqual(1, ev.Count);
            Assert.AreEqual(Soil, ev[0].Cell);
            Assert.AreEqual(after - before, ev[0].Seeds);
            Assert.AreEqual(released, ev[0].CreatureId);
            Assert.AreEqual(tick + 1, ev[0].Tick);
            Assert.IsFalse(sim.OnPlot(Site, 1, 1, 1));
            Assert.IsFalse(sim.EnqueueCrop(0));
            RunTo(sim, sim.State.Tick + 300);
            foreach (var s in sim.State.Stages)
                if (s.PlotId == 0) Assert.AreNotEqual(StageStatus.Assigned, s.Status);
        }

        [TestCase(1400)]
        [TestCase(1430)]
        public void HarvestedLoadStillReachesStorageAfterDig(int tick)
        {
            Build();
            var st = sim.State;
            var store = st.Storages[0];
            RunTo(sim, tick);
            Assert.AreEqual(EditResult.Ok, editor.TryDig(5, 1, 5));
            sim.Tick();
            var haul = st.Stages[st.Stages.Count - 1];
            Assert.AreEqual(WorkAbility.Haul, haul.Ability);
            Assert.AreEqual(StageStatus.Assigned, haul.Status);
            Assert.AreEqual(2, haul.AssigneeId);
            Assert.AreEqual(9, store.Seeds);
            var ev = Cancels();
            Assert.AreEqual(1, ev.Count);
            Assert.AreEqual(0, ev[0].Seeds);
            Assert.AreEqual(-1, ev[0].CreatureId);
            RunTo(sim, 1700);
            Assert.AreEqual(StageStatus.Done, haul.Status);
            Assert.AreEqual(1, store.Produce);
            Assert.AreEqual(10, store.Seeds);
            Assert.AreEqual(0, store.Reserved);
        }

        [Test]
        public void CoverCancelsAndRevertsToDirt()
        {
            Build();
            var plot = sim.State.Plots[0];
            var store = sim.State.Storages[0];
            RunTo(sim, 500);
            inv.Add(BlockMaterial.Stone);
            Assert.AreEqual(EditResult.Ok, editor.TryPlace(5, 2, 5, BlockMaterial.Stone));
            Assert.AreEqual(BlockMaterial.Dirt, w.GetMaterial(5, 1, 5));
            sim.Tick();
            Assert.AreEqual(10, store.Seeds);
            Assert.IsTrue(plot.Removed);
            Assert.AreEqual(1, Cancels().Count);
            Assert.AreEqual(EditResult.Ok, editor.TryDig(5, 2, 5));
            Assert.AreEqual(BlockMaterial.Dirt, w.GetMaterial(5, 1, 5));
            sim.AddFarmOrder(new List<CellPos> { Soil }, 0);
            Assert.AreEqual(2, sim.State.Plots.Count);
            Assert.AreEqual(1, sim.State.Plots[1].Id);
        }

        [Test]
        public void CoverUntilledLeavesMaterial()
        {
            Build();
            var store = sim.State.Storages[0];
            RunTo(sim, 40);
            inv.Add(BlockMaterial.Stone);
            Assert.AreEqual(EditResult.Ok, editor.TryPlace(5, 2, 5, BlockMaterial.Stone));
            Assert.AreEqual(BlockMaterial.Dirt, w.GetMaterial(5, 1, 5));
            sim.Tick();
            Assert.IsTrue(sim.State.Plots[0].Removed);
            Assert.AreEqual(10, store.Seeds);
            Assert.AreEqual(0, Cancels()[0].Seeds);
        }

        [Test]
        public void WaterOnSiteCountsAsCover()
        {
            Build();
            RunTo(sim, 500);
            w.Set(5, 2, 5, Cell.Make(BlockMaterial.Water, 7));
            w.Set(5, 1, 5, Cell.Make(BlockMaterial.Dirt));
            sim.OnCellChanged(5, 2, 5);
            sim.Tick();
            Assert.IsTrue(sim.State.Plots[0].Removed);
            Assert.AreEqual(10, sim.State.Storages[0].Seeds);
        }

        [Test]
        public void RefundExceedsFullStorage()
        {
            Build();
            var store = sim.State.Storages[0];
            RunTo(sim, 500);
            store.Produce = 41;
            Assert.AreEqual(EditResult.Ok, editor.TryDig(5, 1, 5));
            sim.Tick();
            Assert.AreEqual(10, store.Seeds);
            Assert.AreEqual(-1, store.Free);
        }

        [Test]
        public void CancelClearsStallMarker()
        {
            Build();
            var store = sim.State.Storages[0];
            store.Seeds = 0;
            RunTo(sim, 76);
            var plot = sim.State.Plots[0];
            var sow = sim.State.Stages[plot.StageId];
            Assert.AreEqual(StageStatus.Blocked, sow.Status);
            Assert.AreEqual(StallReason.NoSeeds, sow.Stall);
            bool started = false;
            foreach (var e in sim.State.Events)
                if (e.Kind == SimEventKind.StallStarted && e.StageId == sow.Id) started = true;
            Assert.IsTrue(started);
            sim.State.Events.Clear();
            Assert.AreEqual(EditResult.Ok, editor.TryDig(5, 1, 5));
            sim.Tick();
            bool cleared = false;
            foreach (var e in sim.State.Events)
                if (e.Kind == SimEventKind.StallCleared && e.StageId == sow.Id) cleared = true;
            Assert.IsTrue(cleared);
            var ev = Cancels();
            Assert.AreEqual(1, ev.Count);
            Assert.AreEqual(0, ev[0].Seeds);
        }

        [Test]
        public void AttachEditorValidates()
        {
            Build();
            var other = new WorldEditor(TestWorlds.Flat(1), new MaterialInventory(), 1);
            Assert.Throws<System.ArgumentException>(() => sim.AttachEditor(other));
            Assert.Throws<System.InvalidOperationException>(() => sim.AttachEditor(editor));
            Assert.IsTrue(editor.IsOccupied(6, 2, 9));
        }

        static (Simulation sim, WorldEditor ed, MaterialInventory inv) Pair()
        {
            var w = TestWorlds.Flat(1);
            var sim = new Simulation(TestWorlds.NoWander(), w);
            var inv = new MaterialInventory();
            var ed = new WorldEditor(w, inv, 1);
            sim.AttachEditor(ed);
            sim.AddCreature("L1", new CellPos(8, 2, 5));
            sim.AddCreature("L2", new CellPos(5, 2, 1));
            sim.AddCreature("L3", new CellPos(9, 2, 5));
            int k = sim.AddStorage(Kho);
            sim.AddFarmOrder(new List<CellPos> { Soil, new CellPos(5, 1, 6) }, k);
            TestWorlds.HomeAll(sim);
            return (sim, ed, inv);
        }

        [Test]
        public void SameSeedSameHashWithEdits()
        {
            var a = Pair();
            var b = Pair();
            var ha = new List<ulong>();
            var hb = new List<ulong>();
            int cancels = 0;
            for (int t = 1; t <= 2000; t++)
            {
                foreach (var p in new[] { a, b })
                {
                    SimDemo.KeepFarming(p.sim);
                    if (t == 300) p.ed.TryDig(5, 1, 5);
                    if (t == 900)
                    {
                        p.inv.Add(BlockMaterial.Stone);
                        p.ed.TryPlace(5, 2, 6, BlockMaterial.Stone);
                    }
                    p.sim.Tick();
                }
                ha.Add(SimHash.Compute(a.sim.State));
                hb.Add(SimHash.Compute(b.sim.State));
            }
            CollectionAssert.AreEqual(ha, hb);
            foreach (var pl in a.sim.State.Plots) Assert.IsTrue(pl.Removed);
            foreach (var e in a.sim.State.Events)
                if (e.Kind == SimEventKind.CropCancelled) cancels++;
            Assert.AreEqual(2, cancels);
        }
    }
}
