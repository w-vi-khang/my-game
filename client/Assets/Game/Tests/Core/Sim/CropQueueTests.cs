using System;
using System.Collections.Generic;
using Game.Core.Sim;
using NUnit.Framework;

namespace Game.Core.Tests
{
    /// <summary>Đánh dấu xới từng ô, hàng đợi vụ theo loại cây, kho chung (sim bước 7b, 0025).</summary>
    public class CropQueueTests
    {
        static readonly CellPos Soil = new CellPos(5, 1, 5);

        WorldData w;
        WorldEditor editor;
        Simulation sim;
        int id;

        static void RunTo(Simulation s, long tick)
        {
            while (s.State.Tick < tick) s.Tick();
        }

        void Layout(int[] crops, SimConfig cfg = null, bool secondStorage = false, bool blockStorage = false)
        {
            w = TestWorlds.Flat(1);
            if (blockStorage)
                foreach (int y in new[] { 2, 3 })
                    foreach (var c in new[] { new CellPos(5, y, 9), new CellPos(7, y, 9), new CellPos(6, y, 8), new CellPos(6, y, 10) })
                        w.Set(c.X, c.Y, c.Z, Cell.Make(BlockMaterial.Stone));
            sim = new Simulation(cfg ?? TestWorlds.NoWander(), w);
            editor = new WorldEditor(w, new PlayerBag(), 1);
            sim.AttachEditor(editor);
            sim.AddCreature("L1", new CellPos(8, 2, 5));
            sim.AddCreature("L2", new CellPos(5, 2, 1));
            sim.AddCreature("L3", new CellPos(9, 2, 5));
            sim.AddStorage(new CellPos(6, 2, 9));
            if (secondStorage) sim.AddStorage(new CellPos(6, 2, 3));
            id = sim.MarkTill(Soil);
            foreach (int c in crops) sim.EnqueueCrop(id, c);
            TestWorlds.HomeAll(sim);
        }

        WorkStage Stage(WorkAbility a)
        {
            WorkStage found = null;
            foreach (var s in sim.State.Stages)
                if (s.Ability == a) found = s;
            return found;
        }

        int CountStages(WorkAbility a)
        {
            int n = 0;
            foreach (var s in sim.State.Stages)
                if (s.Ability == a) n++;
            return n;
        }

        int Cancelled()
        {
            int n = 0;
            foreach (var e in sim.State.Events)
                if (e.Kind == SimEventKind.CropCancelled) n++;
            return n;
        }

        void RunUntilHaulDone(int max)
        {
            for (int i = 0; i < max; i++)
            {
                sim.Tick();
                var h = Stage(WorkAbility.Haul);
                if (h != null && h.Status == StageStatus.Done) return;
            }
            Assert.Fail("haul not finished after " + max + " ticks");
        }

        [Test]
        public void MarkWithoutCropTillsThenIdles()
        {
            Layout(new int[0]);
            RunTo(sim, 75);
            var p = sim.State.Plots[id];
            Assert.IsTrue(p.Tilled);
            Assert.IsTrue(p.SoilReady);
            Assert.AreEqual(PlotPhase.Idle, p.Phase);
            Assert.AreEqual(-1, p.StageId);
            Assert.AreEqual(BlockMaterial.Farmland, w.GetMaterial(5, 1, 5));
            RunTo(sim, 2000);
            Assert.AreEqual(1, sim.State.Stages.Count);
            Assert.AreEqual(10, sim.State.Stock.Seeds[0]);
        }

        [Test]
        public void FirstCropAfterTillStartsAtSow()
        {
            Layout(new int[0]);
            RunTo(sim, 75);
            Assert.IsTrue(sim.EnqueueCrop(id, 0));
            var p = sim.State.Plots[id];
            Assert.AreEqual(PlotPhase.Sow, p.Phase);
            Assert.AreEqual(1, CountStages(WorkAbility.Till));
            sim.Tick();
            Assert.AreEqual(CreatureState.Working, sim.State.Creatures[0].State);
            Assert.AreEqual(9, sim.State.Stock.Seeds[0]);
            RunTo(sim, 105);
            Assert.AreEqual(PlotPhase.Sow, p.Phase);
            sim.Tick();
            Assert.AreEqual(PlotPhase.Water, p.Phase);
            RunTo(sim, 1356);
            Assert.AreEqual(1356, p.RipeTick);
        }

        [Test]
        public void LongIdleThenCropStillStartsAtSow()
        {
            Layout(new int[0]);
            RunTo(sim, 3000);
            Assert.IsTrue(sim.EnqueueCrop(id, 1));
            Assert.AreEqual(PlotPhase.Sow, sim.State.Plots[id].Phase);
            Assert.AreEqual(1, CountStages(WorkAbility.Till));
        }

        [Test]
        public void NextCropRetills()
        {
            Layout(new[] { 0, 0 });
            RunTo(sim, 1395);
            var p = sim.State.Plots[id];
            Assert.IsFalse(p.SoilReady);
            Assert.AreEqual(PlotPhase.Till, p.Phase);
            Assert.AreEqual(1, p.Slots.Count);
            Assert.AreEqual(1, p.Slots[0].Count);
            Assert.AreEqual(-1, p.CurrentCrop);
            RunTo(sim, 1455);
            Assert.IsTrue(p.SoilReady);
            Assert.AreEqual(PlotPhase.Sow, p.Phase);
            Assert.AreEqual(0, p.Slots.Count);
            Assert.AreEqual(0, p.CurrentCrop);
        }

        [Test]
        public void CropBUsesItsOwnNumbers()
        {
            Layout(new[] { 1 });
            RunTo(sim, 155);
            Assert.AreEqual(1955, sim.State.Plots[id].RipeTick);
            RunUntilHaulDone(3000);
            var haul = Stage(WorkAbility.Haul);
            Assert.AreEqual(1, haul.CropId);
            Assert.AreEqual(3, haul.LoadProduce);
            Assert.AreEqual(2, haul.LoadSeeds);
            var st = sim.State.Stock;
            Assert.AreEqual(3, st.Produce[1]);
            Assert.AreEqual(11, st.Seeds[1]);
            Assert.AreEqual(10, st.Seeds[0]);
            Assert.AreEqual(0, st.Produce[0]);
        }

        [Test]
        public void NoSeedsOnlyBlocksThatCrop()
        {
            w = TestWorlds.Flat(1);
            sim = new Simulation(TestWorlds.NoWander(), w);
            sim.AddCreature("L1", new CellPos(8, 2, 5));
            sim.AddCreature("L2", new CellPos(5, 2, 1));
            sim.AddCreature("L3", new CellPos(9, 2, 5));
            sim.AddStorage(new CellPos(6, 2, 9));
            int a = sim.MarkTill(new CellPos(5, 1, 5));
            int b = sim.MarkTill(new CellPos(6, 1, 5));
            sim.EnqueueCrop(a, 1);
            sim.EnqueueCrop(b, 0);
            TestWorlds.HomeAll(sim);
            sim.State.Stock.Seeds[1] = 0;
            RunTo(sim, 400);
            WorkStage sowA = null, sowB = null;
            foreach (var g in sim.State.Stages)
            {
                if (g.Ability != WorkAbility.Sow) continue;
                if (g.PlotId == a) sowA = g;
                if (g.PlotId == b) sowB = g;
            }
            Assert.AreEqual(WorkAbility.Sow, sowA.Ability);
            Assert.AreEqual(StageStatus.Blocked, sowA.Status);
            Assert.AreEqual(StallReason.NoSeeds, sowA.Stall);
            Assert.AreNotEqual(StageStatus.Blocked, sowB.Status);
            sim.State.Stock.Seeds[1] = 1;
            sim.Tick();
            Assert.AreNotEqual(StageStatus.Blocked, sowA.Status);
        }

        [Test]
        public void UnmarkReleasesTillerAndAllowsRemark()
        {
            Layout(new int[0]);
            var before = w.GetMaterial(5, 1, 5);
            RunTo(sim, 30);
            var till = Stage(WorkAbility.Till);
            Assert.AreEqual(StageStatus.Assigned, till.Status);
            Assert.IsTrue(sim.Unmark(id));
            Assert.AreEqual(CreatureState.Idle, sim.State.Creatures[0].State);
            Assert.AreEqual(-1, sim.State.Creatures[0].StageId);
            Assert.AreEqual(StageStatus.Cancelled, till.Status);
            Assert.IsTrue(sim.State.Plots[id].Removed);
            Assert.AreEqual(before, w.GetMaterial(5, 1, 5));
            Assert.AreEqual(0, Cancelled());
            int again = sim.MarkTill(Soil);
            Assert.GreaterOrEqual(again, 0);
            Assert.AreNotEqual(id, again);
            RunTo(sim, 150);
            Assert.IsTrue(sim.State.Plots[again].Tilled);
            Assert.IsFalse(sim.Unmark(again));
        }

        [Test]
        public void ClearSlotOnlyLeavesQueue()
        {
            Layout(new[] { 0, 1 });
            RunTo(sim, 500);
            var p = sim.State.Plots[id];
            int seeds = sim.State.Stock.Seeds[0];
            Assert.IsTrue(sim.ClearSlot(id, 0));
            Assert.AreEqual(0, p.Slots.Count);
            Assert.AreEqual(0, p.CurrentCrop);
            Assert.AreEqual(PlotPhase.Growing, p.Phase);
            Assert.AreEqual(seeds, sim.State.Stock.Seeds[0]);
        }

        [TestCase(90, PlotPhase.Sow, true)]
        [TestCase(140, PlotPhase.Water, false)]
        [TestCase(500, PlotPhase.Growing, false)]
        [TestCase(1370, PlotPhase.Harvest, false)]
        public void CancelCurrentNoRefundKeepsFarmland(int tick, PlotPhase expected, bool soilReady)
        {
            Layout(new[] { 0 });
            RunTo(sim, tick);
            var p = sim.State.Plots[id];
            Assert.AreEqual(expected, p.Phase);
            Assert.IsTrue(sim.CancelCurrent(id));
            Assert.AreEqual(9, sim.State.Stock.Seeds[0]);
            Assert.AreEqual(-1, p.CurrentCrop);
            Assert.AreEqual(0, p.Slots.Count);
            Assert.AreEqual(PlotPhase.Idle, p.Phase);
            Assert.IsFalse(p.Removed);
            Assert.IsTrue(p.Tilled);
            Assert.AreEqual(BlockMaterial.Farmland, w.GetMaterial(5, 1, 5));
            foreach (var c in sim.State.Creatures)
                if (c.StageId >= 0) Assert.AreNotEqual(CreatureState.Working, c.State);
            Assert.AreEqual(0, Cancelled());
            Assert.AreEqual(soilReady, p.SoilReady);
            Assert.IsTrue(sim.EnqueueCrop(id, 0));
            Assert.AreEqual(soilReady ? PlotPhase.Sow : PlotPhase.Till, p.Phase);
            if (soilReady) Assert.AreEqual(0, p.CurrentCrop);
        }

        [Test]
        public void ClearSlotOnMarkedPlotKeepsTill()
        {
            Layout(new[] { 0 });
            RunTo(sim, 30);
            Assert.IsTrue(sim.ClearSlot(id, 0));
            Assert.AreEqual(0, sim.State.Plots[id].Slots.Count);
            Assert.AreEqual(StageStatus.Assigned, Stage(WorkAbility.Till).Status);
            RunTo(sim, 75);
            Assert.AreEqual(PlotPhase.Idle, sim.State.Plots[id].Phase);
        }

        [Test]
        public void HaulGoesToNearestReachableStorage()
        {
            Layout(new[] { 0 }, null, true);
            RunUntilHaulDone(3000);
            var haul = Stage(WorkAbility.Haul);
            Assert.AreEqual(1, haul.StorageId);
            Assert.AreEqual(new CellPos(6, 2, 3), haul.Destination);
            var st = sim.State.Stock;
            Assert.AreEqual(100, st.Capacity);
            Assert.AreEqual(20, st.Seeds[0]);
            Assert.AreEqual(1, st.Produce[0]);
        }

        [Test]
        public void HaulFallsBackWhenNoStorageReachable()
        {
            Layout(new[] { 0 }, null, false, true);
            RunTo(sim, 1395);
            var haul = Stage(WorkAbility.Haul);
            Assert.AreEqual(0, haul.StorageId);
            RunTo(sim, 1460);
            for (int i = 0; i < 200; i++)
            {
                sim.Tick();
                Assert.AreEqual(StageStatus.Blocked, haul.Status);
                Assert.AreEqual(StallReason.NoPath, haul.Stall);
            }
        }

        [Test]
        public void StorageFullIsPooled()
        {
            var cfg = TestWorlds.NoWander();
            cfg.storage.capacity = 11;
            cfg.crops[1].startSeeds = 0;
            Layout(new[] { 0 }, cfg, true);
            Assert.AreEqual(22, sim.State.Stock.Capacity);
            Assert.AreEqual(20, sim.State.Stock.Seeds[0]);
            RunTo(sim, 1390);
            Assert.AreEqual(19, sim.State.Stock.Seeds[0]);
            Assert.AreEqual(3, sim.State.Stock.Free);
            RunTo(sim, 1400);
            Assert.AreNotEqual(StageStatus.Blocked, Stage(WorkAbility.Haul).Status);

            Layout(new[] { 0 }, cfg, true);
            RunTo(sim, 1300);
            sim.State.Stock.Produce[0] = 2;
            RunTo(sim, 1400);
            Assert.AreEqual(1, sim.State.Stock.Free);
            var haul = Stage(WorkAbility.Haul);
            Assert.AreEqual(StageStatus.Blocked, haul.Status);
            Assert.AreEqual(StallReason.StorageFull, haul.Stall);
        }

        [Test]
        public void EnqueueRules()
        {
            Layout(new int[0]);
            Assert.Throws<ArgumentException>(() => sim.EnqueueCrop(id, 2));
            for (int i = 0; i < 5; i++) Assert.IsTrue(sim.EnqueueCrop(id, 0), "run " + (i + 1));
            var slots = sim.State.Plots[id].Slots;
            Assert.AreEqual(2, slots.Count);
            Assert.AreEqual(3, slots[0].Count);
            Assert.AreEqual(2, slots[1].Count);
            Assert.IsTrue(sim.EnqueueCrop(id, 0));
            Assert.AreEqual(3, slots[1].Count);
            Assert.IsFalse(sim.EnqueueCrop(id, 0));

            var bare = new Simulation(TestWorlds.NoWander(), TestWorlds.Flat(1));
            int pid = bare.MarkTill(Soil);
            Assert.GreaterOrEqual(pid, 0);
            Assert.IsFalse(bare.EnqueueCrop(pid, 0));

            Layout(new int[0]);
            Assert.IsTrue(sim.Unmark(id));
            Assert.IsFalse(sim.EnqueueCrop(id, 0));
        }

        static int[] SlotList(FarmPlot p)
        {
            var l = new List<int>();
            foreach (var sl in p.Slots) { l.Add(sl.CropId); l.Add(sl.Count); }
            return l.ToArray();
        }

        [Test]
        public void EnqueueFillsSlotsByRule()
        {
            var cfg = TestWorlds.NoWander();
            cfg.farm.queueSlots = 3;
            Layout(new int[0], cfg);
            for (int i = 0; i < 3; i++) Assert.IsTrue(sim.EnqueueCrop(id, 0));
            for (int i = 0; i < 3; i++) Assert.IsTrue(sim.EnqueueCrop(id, 1));
            for (int i = 0; i < 3; i++) Assert.IsTrue(sim.EnqueueCrop(id, 0));
            var p = sim.State.Plots[id];
            Assert.AreEqual(new[] { 0, 3, 1, 3, 0, 3 }, SlotList(p));
            Assert.IsFalse(sim.EnqueueCrop(id, 0));
            Assert.IsFalse(sim.EnqueueCrop(id, 1));
            var sown = new List<int>();
            var last = p.Phase;
            for (int t = 0; t < 8000 && sown.Count < 4; t++)
            {
                sim.Tick();
                if (p.Phase == PlotPhase.Sow && last != PlotPhase.Sow) sown.Add(p.CurrentCrop);
                last = p.Phase;
            }
            Assert.AreEqual(new[] { 0, 0, 0, 1 }, sown.ToArray());
        }

        [Test]
        public void TwoSlotsAThenB()
        {
            Layout(new int[0]);
            for (int i = 0; i < 3; i++) Assert.IsTrue(sim.EnqueueCrop(id, 0));
            for (int i = 0; i < 3; i++) Assert.IsTrue(sim.EnqueueCrop(id, 1));
            Assert.AreEqual(new[] { 0, 3, 1, 3 }, SlotList(sim.State.Plots[id]));
            Assert.IsFalse(sim.EnqueueCrop(id, 1));
        }

        [Test]
        public void SlotPlusMinusClearShift()
        {
            var cfg = TestWorlds.NoWander();
            cfg.farm.queueSlots = 3;
            Layout(new int[0], cfg);
            var p = sim.State.Plots[id];
            for (int i = 0; i < 3; i++) sim.EnqueueCrop(id, 0);
            for (int i = 0; i < 2; i++) sim.EnqueueCrop(id, 1);
            sim.EnqueueCrop(id, 0);
            Assert.AreEqual(new[] { 0, 3, 1, 2, 0, 1 }, SlotList(p));
            Assert.IsTrue(sim.ClearSlot(id, 1));
            Assert.AreEqual(new[] { 0, 3, 0, 1 }, SlotList(p));
            Assert.IsTrue(sim.DecreaseSlot(id, 1));
            Assert.AreEqual(new[] { 0, 3 }, SlotList(p));
            Assert.IsFalse(sim.IncreaseSlot(id, 0));
            for (int i = 0; i < 3; i++) Assert.IsTrue(sim.DecreaseSlot(id, 0));
            Assert.AreEqual(0, p.Slots.Count);
            Assert.IsFalse(sim.DecreaseSlot(id, 0));
            Assert.IsFalse(sim.IncreaseSlot(id, 5));
            Assert.IsFalse(sim.ClearSlot(id, -1));
        }

        [Test]
        public void SowMovesCropToProgress()
        {
            Layout(new[] { 0, 0 });
            var p = sim.State.Plots[id];
            for (int t = 0; t < 400 && p.Phase != PlotPhase.Sow; t++)
            {
                Assert.AreEqual(-1, p.CurrentCrop);
                Assert.AreEqual(2, p.Slots[0].Count);
                sim.Tick();
            }
            Assert.AreEqual(PlotPhase.Sow, p.Phase);
            Assert.AreEqual(0, p.CurrentCrop);
            Assert.AreEqual(1, p.Slots[0].Count);
        }

        [Test]
        public void PauseTillKeepsProgressAndBlocksAssign()
        {
            Layout(new[] { 0, 0 });
            var p = sim.State.Plots[id];
            Assert.IsFalse(sim.PauseTill(id));
            RunTo(sim, 1395);
            Assert.IsTrue(p.Tilled);
            Assert.AreEqual(PlotPhase.Till, p.Phase);
            RunTo(sim, 1405);
            var st = sim.State.Stages[p.StageId];
            Assert.AreEqual(StageStatus.Assigned, st.Status);
            long done = st.WorkDone;
            Assert.IsTrue(sim.PauseTill(id));
            Assert.IsTrue(p.TillPaused);
            Assert.AreNotEqual(StageStatus.Assigned, st.Status);
            RunTo(sim, 1605);
            Assert.AreNotEqual(StageStatus.Assigned, st.Status);
            Assert.AreEqual(done, st.WorkDone);
            Assert.IsFalse(sim.PauseTill(id));
            Assert.IsTrue(sim.ResumeTill(id));
            for (int t = 0; t < 1000 && p.Phase == PlotPhase.Till; t++) sim.Tick();
            Assert.AreEqual(PlotPhase.Sow, p.Phase);
        }

        [Test]
        public void PauseTillRejectedOutsideRetill()
        {
            Layout(new[] { 0 });
            Assert.IsFalse(sim.PauseTill(id));
            RunTo(sim, 90);
            Assert.AreEqual(PlotPhase.Sow, sim.State.Plots[id].Phase);
            Assert.IsFalse(sim.PauseTill(id));
            Layout(new int[0]);
            Assert.IsFalse(sim.PauseTill(id));
        }

        [Test]
        public void CancelCurrentStartsNext()
        {
            Layout(new[] { 0, 1 });
            RunTo(sim, 500);
            var p = sim.State.Plots[id];
            Assert.IsTrue(sim.CancelCurrent(id));
            Assert.AreEqual(PlotPhase.Till, p.Phase);
            Assert.IsFalse(p.SoilReady);
            Assert.AreEqual(1, p.Slots[0].CropId);
            Assert.AreEqual(9, sim.State.Stock.Seeds[0]);
        }

        [Test]
        public void PerPlotSlotLimits()
        {
            Layout(new int[0]);
            int id2 = sim.MarkTill(new CellPos(6, 1, 5));
            var p1 = sim.State.Plots[id];
            var p2 = sim.State.Plots[id2];
            Assert.AreEqual(2, p1.SlotCount); Assert.AreEqual(3, p1.SlotMax);
            Assert.AreEqual(2, p2.SlotCount); Assert.AreEqual(3, p2.SlotMax);
            p1.SlotCount = 3;
            Assert.IsTrue(sim.EnqueueCrop(id, 0));
            Assert.IsTrue(sim.EnqueueCrop(id, 1));
            Assert.IsTrue(sim.EnqueueCrop(id, 0));
            Assert.IsTrue(sim.EnqueueCrop(id2, 0));
            Assert.IsTrue(sim.EnqueueCrop(id2, 1));
            Assert.IsFalse(sim.EnqueueCrop(id2, 0));
        }

        [Test]
        public void KeepFarmingRefillsOnlyWhenEmpty()
        {
            Layout(new[] { 0 });
            RunTo(sim, 500);
            var p = sim.State.Plots[id];
            Assert.AreEqual(0, p.CurrentCrop);
            SimDemo.KeepFarming(sim);
            Assert.AreEqual(0, p.Slots.Count);
            RunTo(sim, 1395);
            Assert.AreEqual(PlotPhase.Idle, p.Phase);
            Assert.AreEqual(-1, p.CurrentCrop);
            SimDemo.KeepFarming(sim);
            Assert.AreEqual(1, p.Slots[0].Count);
            Assert.AreEqual(PlotPhase.Till, p.Phase);
        }

        Simulation Scenario(out WorldEditor ed)
        {
            var world = TestWorlds.Flat(1);
            var s = new Simulation(TestWorlds.NoWander(), world);
            ed = new WorldEditor(world, new PlayerBag(), 1);
            s.AttachEditor(ed);
            s.AddCreature("L1", new CellPos(8, 2, 5));
            s.AddCreature("L2", new CellPos(5, 2, 1));
            s.AddCreature("L3", new CellPos(9, 2, 5));
            s.AddStorage(new CellPos(6, 2, 9));
            int a = s.MarkTill(new CellPos(5, 1, 5));
            int b = s.MarkTill(new CellPos(6, 1, 5));
            s.MarkTill(new CellPos(7, 1, 5));
            s.EnqueueCrop(a, 0); s.EnqueueCrop(a, 1);
            s.EnqueueCrop(b, 1);
            TestWorlds.HomeAll(s);
            return s;
        }

        [Test]
        public void SameSeedSameHashWithQueueOps()
        {
            var a = Scenario(out var ea);
            var b = Scenario(out var eb);
            for (int t = 1; t <= 3000; t++)
            {
                foreach (var pair in new[] { (a, ea), (b, eb) })
                {
                    var s = pair.Item1;
                    if (t == 50) s.Unmark(2);
                    if (t == 600) s.ClearSlot(0, 0);
                    if (t == 650) { s.EnqueueCrop(0, 1); s.IncreaseSlot(0, 0); s.DecreaseSlot(0, 0); }
                    if (t == 660) s.CancelCurrent(1);
                    if (t == 700) pair.Item2.TryDig(6, 1, 5);
                    s.Tick();
                }
                Assert.AreEqual(SimHash.Compute(a.State), SimHash.Compute(b.State), "tick " + t);
            }
            b.State.Stock.Seeds[1]++;
            Assert.AreNotEqual(SimHash.Compute(a.State), SimHash.Compute(b.State));
        }
    }
}
