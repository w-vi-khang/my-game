using System;
using System.Collections.Generic;
using Game.Core.Sim;
using NUnit.Framework;

namespace Game.Core.Tests
{
    /// <summary>Lệnh trồng trọt của người chơi, phép Mưa, kho ↔ túi, tìm đường nhân vật (sim 7f lõi, 0028).</summary>
    public class PlayerFarmTests
    {
        static readonly CellPos Soil = new CellPos(5, 1, 5);

        WorldData w;
        Simulation sim;
        PlayerBag bag;
        int id;

        static void RunTo(Simulation s, long tick)
        {
            while (s.State.Tick < tick) s.Tick();
        }

        void Layout(bool creatures, Action<WorldData> tweak = null, int[] crops = null)
        {
            w = TestWorlds.Flat(1);
            tweak?.Invoke(w);
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

        void ToHarvest(int crop)
        {
            Assert.IsTrue(sim.PlayerTill(id));
            bag.AddSeeds(crop, 1);
            Assert.AreEqual(PlayerSowResult.Ok, sim.PlayerSow(id, crop, bag));
            Assert.AreEqual(RainResult.Watered, sim.Rain(Soil));
            RunTo(sim, sim.State.Tick + sim.Config.GrowTicks(crop));
            Assert.AreEqual(PlotPhase.Harvest, Plot.Phase);
        }

        // ---------- tự xới ----------

        [Test]
        public void PlayerTillNoCreatures()
        {
            Layout(false);
            Assert.IsTrue(sim.CanPlayerTill(id));
            Assert.IsTrue(sim.PlayerTill(id));
            Assert.AreEqual(BlockMaterial.Farmland, w.GetMaterial(5, 1, 5));
            Assert.IsTrue(Plot.Tilled);
            Assert.IsTrue(Plot.SoilReady);
            Assert.AreEqual(PlotPhase.Idle, Plot.Phase);
            var st = LastStage(WorkAbility.Till);
            Assert.AreEqual(StageStatus.Done, st.Status);
            Assert.AreEqual(AssigneeKind.Player, st.AssigneeKind);
            Assert.IsFalse(sim.PlayerTill(id));
        }

        [Test]
        public void PlayerTillTakesOverFromCreature()
        {
            Layout(true);
            int guard = 0;
            Creature worker = null;
            while (worker == null && guard++ < 400)
            {
                sim.Tick();
                foreach (var c in sim.State.Creatures)
                    if (c.State == CreatureState.Working && c.StageId >= 0 && sim.State.Stages[c.StageId].Ability == WorkAbility.Till)
                        worker = c;
            }
            Assert.NotNull(worker, "no creature has tilled yet");
            Assert.IsTrue(sim.PlayerTill(id));
            Assert.AreEqual(CreatureState.Idle, worker.State);
            Assert.AreEqual(-1, worker.StageId);
            RunTo(sim, sim.State.Tick + 200);
            Assert.AreEqual(1, CountStages(WorkAbility.Till));
        }

        [Test]
        public void PlayerTillClearsPause()
        {
            Layout(false);
            ToHarvest(0);
            Assert.IsTrue(sim.EnqueueCrop(id, 0));
            Assert.IsTrue(sim.PlayerHarvest(id, bag));
            Assert.AreEqual(PlotPhase.Till, Plot.Phase);
            Assert.IsTrue(sim.PauseTill(id));
            Assert.IsTrue(Plot.TillPaused);
            Assert.IsTrue(sim.PlayerTill(id));
            Assert.IsFalse(Plot.TillPaused);
        }

        // ---------- gieo ----------

        [Test]
        public void SowOnTilledIdlePlot()
        {
            Layout(false);
            sim.PlayerTill(id);
            bag.AddSeeds(0, 2);
            Assert.AreEqual(PlayerSowResult.Ok, sim.PlayerSow(id, 0, bag));
            Assert.AreEqual(1, bag.GetSeeds(0));
            Assert.AreEqual(0, Plot.CurrentCrop);
            Assert.AreEqual(PlotPhase.Water, Plot.Phase);
            var sow = LastStage(WorkAbility.Sow);
            Assert.AreEqual(StageStatus.Done, sow.Status);
            Assert.AreEqual(AssigneeKind.Player, sow.AssigneeKind);
            var water = LastStage(WorkAbility.Water);
            Assert.AreEqual(StageStatus.Open, water.Status);
            Assert.AreEqual(0, Plot.Slots.Count);
        }

        [Test]
        public void SowTakesOverFromCreatureAndReturnsStockSeed()
        {
            Layout(true, crops: new[] { 0 });
            int guard = 0;
            while (sim.State.Stock.Seeds[0] != 9 && guard++ < 600) sim.Tick();
            Assert.AreEqual(9, sim.State.Stock.Seeds[0], "sowing step not assigned yet");
            var sow = LastStage(WorkAbility.Sow);
            Assert.AreEqual(StageStatus.Assigned, sow.Status);
            var worker = sim.State.Creatures[sow.AssigneeId];
            bag.AddSeeds(0, 1);
            Assert.AreEqual(PlayerSowResult.Ok, sim.PlayerSow(id, 0, bag));
            Assert.AreEqual(10, sim.State.Stock.Seeds[0]);
            Assert.AreEqual(-1, worker.StageId);
            Assert.AreEqual(PlotPhase.Water, Plot.Phase);
            Assert.AreEqual(0, bag.GetSeeds(0));
        }

        [Test]
        public void SowRejections()
        {
            Layout(false, crops: new[] { 1 });
            bag.AddSeeds(0, 1);
            bag.AddSeeds(1, 1);
            // pha Till, còn 1 vụ trong hàng đợi → hàng đợi trước
            Assert.AreEqual(PlotPhase.Till, Plot.Phase);
            Assert.AreEqual(PlayerSowResult.QueueNotEmpty, sim.PlayerSow(id, 0, bag));
            Assert.AreEqual(1, Plot.Slots.Count);
            sim.PlayerTill(id);
            Assert.AreEqual(PlotPhase.Sow, Plot.Phase);
            Assert.AreEqual(PlayerSowResult.OtherCrop, sim.PlayerSow(id, 0, bag));
            Assert.AreEqual(1, bag.GetSeeds(0));
            Assert.AreEqual(PlotPhase.Sow, Plot.Phase);
            Assert.AreEqual(PlayerSowResult.Ok, sim.PlayerSow(id, 1, bag));
            Assert.AreEqual(PlayerSowResult.HasCrop, sim.PlayerSow(id, 1, bag));
            sim.Rain(Soil);
            Assert.AreEqual(PlotPhase.Growing, Plot.Phase);
            Assert.AreEqual(PlayerSowResult.HasCrop, sim.PlayerSow(id, 1, bag));
            Assert.AreEqual(PlayerSowResult.NoPlot, sim.PlayerSow(99, 0, bag));
            Assert.Throws<ArgumentException>(() => sim.PlayerSow(id, 5, bag));
            // pha Till, hàng đợi trống → chưa xới
            Layout(false);
            bag.AddSeeds(0, 1);
            Assert.AreEqual(PlotPhase.Till, Plot.Phase);
            Assert.AreEqual(PlayerSowResult.NotTilled, sim.PlayerSow(id, 0, bag));
            Assert.AreEqual(1, bag.GetSeeds(0));
        }

        [Test]
        public void SowRejectsWhenQueueNotEmpty()
        {
            Layout(false);
            Assert.IsTrue(sim.PlayerTill(id));
            Assert.IsTrue(sim.EnqueueCrop(id, 0)); // mở vụ hiện tại
            Assert.IsTrue(sim.EnqueueCrop(id, 0)); // nằm trong hàng đợi
            Assert.Greater(Plot.Slots.Count, 0);
            bag.AddSeeds(0, 2);
            int slots = Plot.Slots.Count;
            Assert.AreEqual(PlayerSowResult.QueueNotEmpty, sim.CheckPlayerSow(id, 0, bag));
            Assert.AreEqual(PlayerSowResult.QueueNotEmpty, sim.PlayerSow(id, 0, bag));
            Assert.AreEqual(2, bag.GetSeeds(0));
            Assert.AreEqual(slots, Plot.Slots.Count);
            while (Plot.Slots.Count > 0) Assert.IsTrue(sim.ClearSlot(id, 0));
            Assert.AreEqual(PlayerSowResult.Ok, sim.PlayerSow(id, 0, bag));
        }

        [Test]
        public void SowOnRemovedPlot()
        {
            Layout(false);
            bag.AddSeeds(0, 1);
            Assert.IsTrue(sim.Unmark(id));
            Assert.AreEqual(PlayerSowResult.NoPlot, sim.PlayerSow(id, 0, bag));
        }

        [Test]
        public void SowNoSeeds()
        {
            Layout(false);
            sim.PlayerTill(id);
            Assert.AreEqual(PlayerSowResult.NoSeeds, sim.PlayerSow(id, 0, bag));
            Assert.AreEqual(PlotPhase.Idle, Plot.Phase);
        }

        // ---------- Mưa ----------

        [Test]
        public void RainWatersPlot()
        {
            Layout(false);
            sim.PlayerTill(id);
            bag.AddSeeds(0, 1);
            sim.PlayerSow(id, 0, bag);
            RunTo(sim, 7);
            Assert.AreEqual(RainResult.Watered, sim.Rain(Soil));
            Assert.AreEqual(PlotPhase.Growing, Plot.Phase);
            Assert.AreEqual(sim.State.Tick + sim.Config.GrowTicks(0), Plot.RipeTick);
        }

        [Test]
        public void RainTakesOverFromWateringCreature()
        {
            Layout(true, crops: new[] { 0 });
            int guard = 0;
            while (guard++ < 1000)
            {
                sim.Tick();
                if (Plot.Phase == PlotPhase.Water && Plot.StageId >= 0 && sim.State.Stages[Plot.StageId].Status == StageStatus.Assigned) break;
            }
            Assert.AreEqual(PlotPhase.Water, Plot.Phase);
            var worker = sim.State.Creatures[sim.State.Stages[Plot.StageId].AssigneeId];
            Assert.AreEqual(RainResult.Watered, sim.Rain(Soil));
            Assert.AreEqual(-1, worker.StageId);
            Assert.AreEqual(PlotPhase.Growing, Plot.Phase);
        }

        [Test]
        public void RainGrassesBareDirt()
        {
            Layout(false);
            Assert.AreEqual(RainResult.Grassed, sim.Rain(new CellPos(2, 1, 2)));
            Assert.AreEqual(BlockMaterial.Grass, w.GetMaterial(2, 1, 2));
        }

        [Test]
        public void RainOnSignedSoilKeepsPlot()
        {
            Layout(false);
            Assert.AreEqual(RainResult.Grassed, sim.Rain(Soil));
            Assert.AreEqual(BlockMaterial.Grass, w.GetMaterial(5, 1, 5));
            Assert.GreaterOrEqual(sim.PlotAt(Soil), 0);
            sim.Tick();
            Assert.GreaterOrEqual(sim.PlotAt(Soil), 0);
            Assert.IsFalse(Plot.Removed);
        }

        [Test]
        public void RainNoEffect()
        {
            Layout(false, ww => TestWorlds.Pillar(ww, 3, 3, 1));
            sim.PlayerTill(id);
            bag.AddSeeds(0, 1);
            sim.PlayerSow(id, 0, bag);
            sim.Rain(Soil); // → Growing
            sim.Rain(new CellPos(2, 1, 2)); // → cỏ
            var before = (ushort[])w.Cells.Clone();
            ulong hash = SimHash.Compute(sim.State);
            Assert.AreEqual(RainResult.NoEffect, sim.Rain(new CellPos(3, 1, 3)), "rock");
            Assert.AreEqual(RainResult.NoEffect, sim.Rain(new CellPos(2, 1, 2)), "grass");
            Assert.AreEqual(RainResult.NoEffect, sim.Rain(Soil), "growing plot cell");
            CollectionAssert.AreEqual(before, w.Cells);
            Assert.AreEqual(hash, SimHash.Compute(sim.State));
        }

        // ---------- thu hoạch ----------

        [Test]
        public void HarvestIntoBag()
        {
            Layout(false);
            ToHarvest(0);
            int reserved = sim.State.Stock.Reserved;
            int seeds = bag.GetSeeds(0);
            Assert.IsTrue(sim.CanPlayerHarvest(id));
            Assert.IsTrue(sim.PlayerHarvest(id, bag));
            Assert.AreEqual(1, bag.GetProduce(0));
            Assert.AreEqual(seeds + 1, bag.GetSeeds(0));
            Assert.AreEqual(0, CountStages(WorkAbility.Haul));
            Assert.AreEqual(reserved, sim.State.Stock.Reserved);
            Assert.IsFalse(Plot.SoilReady);
            Assert.AreEqual(PlotPhase.Idle, Plot.Phase);
            Assert.IsFalse(sim.PlayerHarvest(id, bag));
        }

        [Test]
        public void HarvestOverflowDropsOnPlot()
        {
            Layout(false);
            ToHarvest(0);
            var small = new PlayerBag(1, 1, 10, 10, 999);
            small.Add(BlockMaterial.Stone, 1);
            small.Add(BlockMaterial.Sand, 1);
            Assert.IsTrue(sim.PlayerHarvest(id, small));
            Assert.AreEqual(0, small.GetProduce(0));
            var cd = sim.Config.crops[0];
            var drops = sim.Editor.Drops.At(Soil.X, Soil.Y + 1, Soil.Z);
            int produce = 0, seeds = 0;
            foreach (var d in drops)
            {
                if (d.Item == ItemId.Produce(0)) produce += d.Count;
                if (d.Item == ItemId.SeedBag(0)) seeds += d.Count;
            }
            Assert.AreEqual(cd.producePerHarvest, produce);
            Assert.AreEqual(cd.seedsPerHarvest, seeds);
        }

        [Test]
        public void TakeFromStockLimitedByBag()
        {
            Layout(false);
            int s0 = sim.State.Stock.Seeds[0];
            var small = new PlayerBag(1, 1, 10, 10, 5);
            small.Add(BlockMaterial.Stone, 1);
            Assert.AreEqual(5, sim.TakeFromStock(small, 0, false, 999));
            Assert.AreEqual(5, small.GetSeeds(0));
            Assert.AreEqual(s0 - 5, sim.State.Stock.Seeds[0]);
        }

        [Test]
        public void HarvestWithQueueStartsNextCrop()
        {
            Layout(false);
            sim.PlayerTill(id);
            bag.AddSeeds(1, 1);
            sim.PlayerSow(id, 1, bag);
            sim.Rain(Soil);
            Assert.IsTrue(sim.EnqueueCrop(id, 0));
            RunTo(sim, sim.State.Tick + sim.Config.GrowTicks(1));
            Assert.IsTrue(sim.PlayerHarvest(id, bag));
            Assert.AreEqual(3, bag.GetProduce(1));
            Assert.AreEqual(2, bag.GetSeeds(1));
            Assert.AreEqual(PlotPhase.Till, Plot.Phase);
        }

        [Test]
        public void HarvestWrongPhase()
        {
            Layout(false);
            Assert.IsFalse(sim.CanPlayerHarvest(id));
            Assert.IsFalse(sim.PlayerHarvest(id, bag));
            Assert.IsFalse(sim.PlayerHarvest(99, bag));
        }

        [Test]
        public void ManualChainNoCreatures()
        {
            Layout(false);
            int stockSeeds = sim.State.Stock.Seeds[0];
            bag.AddSeeds(0, 3);
            sim.PlayerTill(id);
            sim.PlayerSow(id, 0, bag);
            sim.Rain(Soil);
            RunTo(sim, sim.State.Tick + sim.Config.GrowTicks(0));
            Assert.AreEqual(PlotPhase.Harvest, Plot.Phase);
            sim.PlayerHarvest(id, bag);
            Assert.AreEqual(1, bag.GetProduce(0));
            Assert.AreEqual(3, bag.GetSeeds(0));
            Assert.AreEqual(stockSeeds, sim.State.Stock.Seeds[0]);
        }

        // ---------- kho ↔ túi ----------

        [Test]
        public void StoreToStockMovesItems()
        {
            Layout(false);
            bag.AddSeeds(0, 5);
            bag.AddProduce(1, 4);
            int s0 = sim.State.Stock.Seeds[0];
            Assert.AreEqual(3, sim.StoreToStock(bag, 0, false, 3));
            Assert.AreEqual(2, bag.GetSeeds(0));
            Assert.AreEqual(s0 + 3, sim.State.Stock.Seeds[0]);
            Assert.AreEqual(4, sim.StoreToStock(bag, 1, true, 10));
            Assert.AreEqual(0, bag.GetProduce(1));
            Assert.AreEqual(4, sim.State.Stock.Produce[1]);
        }

        [Test]
        public void StoreToStockStopsWhenFull()
        {
            Layout(false);
            bag.AddSeeds(0, 100);
            int free = sim.State.Stock.Free;
            Assert.Greater(free, 0);
            Assert.AreEqual(free, sim.StoreToStock(bag, 0, false, 100));
            Assert.AreEqual(100 - free, bag.GetSeeds(0));
            Assert.AreEqual(0, sim.State.Stock.Free);
            Assert.AreEqual(0, sim.StoreToStock(bag, 0, false, 5));
            Assert.AreEqual(100 - free, bag.GetSeeds(0));
        }

        [Test]
        public void TakeFromStock()
        {
            Layout(false);
            int s0 = sim.State.Stock.Seeds[0];
            Assert.AreEqual(4, sim.TakeFromStock(bag, 0, false, 4));
            Assert.AreEqual(4, bag.GetSeeds(0));
            Assert.AreEqual(s0 - 4, sim.State.Stock.Seeds[0]);
            Assert.AreEqual(s0 - 4, sim.TakeFromStock(bag, 0, false, 999));
            Assert.AreEqual(0, sim.State.Stock.Seeds[0]);
            Assert.AreEqual(0, sim.TakeFromStock(bag, 0, false, 1));
            Assert.AreEqual(0, sim.TakeFromStock(bag, 1, true, 1));
        }

        [Test]
        public void StoringSeedsUnblocksSowStage()
        {
            Layout(true, crops: new[] { 0 });
            sim.State.Stock.Seeds[0] = 0;
            int guard = 0;
            WorkStage sow = null;
            while (guard++ < 600)
            {
                sim.Tick();
                sow = LastStage(WorkAbility.Sow);
                if (sow != null && sow.Status == StageStatus.Blocked && sow.Stall == StallReason.NoSeeds) break;
            }
            Assert.NotNull(sow);
            Assert.AreEqual(StallReason.NoSeeds, sow.Stall);
            bag.AddSeeds(0, 1);
            Assert.AreEqual(1, sim.StoreToStock(bag, 0, false, 1));
            sim.Tick();
            Assert.AreNotEqual(StageStatus.Blocked, sow.Status);
        }

        [Test]
        public void StorageInReach()
        {
            Layout(false);
            Assert.IsTrue(sim.StorageInReach(6.5f, 1.5f, -0.5f));
            Assert.IsFalse(sim.StorageInReach(6.5f, 1.5f, -0.6f));
            var empty = new Simulation(TestWorlds.NoWander(), TestWorlds.Flat(1));
            Assert.IsFalse(empty.StorageInReach(6.5f, 1.5f, 9.5f));
        }

        // ---------- tìm đường ----------

        [Test]
        public void PathToPlotEndsNextToSite()
        {
            Layout(false);
            var path = new List<CellPos>();
            var start = new CellPos(5, 2, 9);
            Assert.IsTrue(sim.FindPlayerPathToPlot(start, id, path));
            Assert.Greater(path.Count, 0);
            var site = Plot.Site;
            var last = path[path.Count - 1];
            Assert.AreEqual(1, Math.Abs(last.X - site.X) + Math.Abs(last.Z - site.Z));
            Assert.LessOrEqual(Math.Abs(last.Y - site.Y), 1);
            var prev = start;
            foreach (var c in path)
            {
                Assert.LessOrEqual(Math.Abs(c.X - prev.X) + Math.Abs(c.Z - prev.Z), 1);
                Assert.LessOrEqual(Math.Abs(c.Y - prev.Y), 1);
                prev = c;
            }
        }

        [Test]
        public void PathAlreadyAdjacentIsEmpty()
        {
            Layout(false);
            var path = new List<CellPos> { new CellPos(0, 0, 0) };
            Assert.IsTrue(sim.FindPlayerPathToPlot(new CellPos(5, 2, 6), id, path));
            Assert.AreEqual(0, path.Count);
        }

        [Test]
        public void PathBlockedAndRemoved()
        {
            Layout(false, ww =>
            {
                TestWorlds.Pillar(ww, 4, 5, 3);
                TestWorlds.Pillar(ww, 6, 5, 3);
                TestWorlds.Pillar(ww, 5, 4, 3);
                TestWorlds.Pillar(ww, 5, 6, 3);
            });
            var path = new List<CellPos>();
            Assert.IsFalse(sim.FindPlayerPathToPlot(new CellPos(5, 2, 9), id, path));
            Assert.IsTrue(sim.Unmark(id));
            Assert.IsFalse(sim.FindPlayerPathToPlot(new CellPos(5, 2, 9), id, path));
        }

        // ---------- tất định ----------

        [Test]
        public void SameCommandsSameHash()
        {
            var sims = new Simulation[2];
            var bags = new PlayerBag[2];
            for (int i = 0; i < 2; i++)
            {
                Layout(true, crops: new[] { 0 });
                sims[i] = sim;
                bags[i] = bag;
                bag.AddSeeds(0, 2);
            }
            for (int t = 1; t <= 500; t++)
            {
                for (int i = 0; i < 2; i++)
                {
                    var s = sims[i];
                    s.Tick();
                    if (t == 5) s.PlayerTill(0);
                    if (t == 6) s.PlayerSow(0, 0, bags[i]);
                    if (t == 7) s.Rain(Soil);
                    if (t == 8) s.StoreToStock(bags[i], 0, false, 1);
                }
                Assert.AreEqual(SimHash.Compute(sims[0].State), SimHash.Compute(sims[1].State), "tick " + t);
            }
            Assert.AreEqual(bags[0].GetSeeds(0), bags[1].GetSeeds(0));
            Assert.AreEqual(bags[0].GetProduce(0), bags[1].GetProduce(0));
        }
    }
}
