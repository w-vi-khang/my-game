using System;
using System.Collections.Generic;
using Game.Core.DayCycle;
using Game.Core.Habitat;
using Game.Core.Sim;
using NUnit.Framework;

namespace Game.Core.Tests
{
    public class HabitatTrackerTests
    {
        const int N = 5;

        static HabitatRequirement Cells(string mat, int min = 1, bool exposed = false, bool nearWater = false, int minY = 0) =>
            new HabitatRequirement { kind = "cells", min = min, material = mat, exposed = exposed, nearWater = nearWater, minY = minY };

        static HabitatConfig Cfg(params HabitatSpecies[] species) =>
            new HabitatConfig { unlockedAtStart = new string[0], species = species };

        static HabitatSpecies Sp(string id, params HabitatRequirement[] r) => new HabitatSpecies(id, r);

        static void Put(WorldData w, int x, int y, int z, BlockMaterial m, int water = 0) => w.Set(x, y, z, Cell.Make(m, water));

        /// <summary>16×8×16, đáy đá y = 0. 6 cỏ (1 ở y = 3), 3 đá (1 bị che), 1 đất, 2 cỏ cạnh nước.</summary>
        static WorldData Layout()
        {
            var w = new WorldData(new WorldSize(16, 16, 16));
            for (int z = 0; z < 16; z++)
            for (int x = 0; x < 16; x++)
                Put(w, x, 0, z, BlockMaterial.Bedrock);
            Put(w, 2, 1, 2, BlockMaterial.Grass);
            Put(w, 3, 1, 2, BlockMaterial.Grass);
            Put(w, 4, 1, 2, BlockMaterial.Grass);
            Put(w, 3, 1, 3, BlockMaterial.Water, 7);      // cùng tầng với cỏ (3,1,2)
            Put(w, 12, 1, 12, BlockMaterial.Grass);
            Put(w, 13, 2, 12, BlockMaterial.Water, 7);    // tầng trên cỏ (12,1,12)
            Put(w, 14, 1, 14, BlockMaterial.Grass);       // không có nước
            Put(w, 1, 3, 1, BlockMaterial.Grass);         // điểm cao y = 3
            Put(w, 8, 1, 8, BlockMaterial.Stone);         // lộ thiên
            Put(w, 9, 1, 9, BlockMaterial.Stone);
            Put(w, 9, 2, 9, BlockMaterial.Stone);         // che (9,1,9); (9,2,9) lộ thiên
            Put(w, 10, 1, 10, BlockMaterial.Dirt);
            return w;
        }

        static SimState EmptyState() => new Simulation(TestWorlds.NoWander(), TestWorlds.Flat(1)).State;

        [Test]
        public void InitialScanCountsCells()
        {
            var w = Layout();
            var t = new HabitatTracker(Cfg(
                Sp("grass", Cells("Grass")),
                Sp("stoneExp", Cells("Stone", exposed: true)),
                Sp("grassWater", Cells("Grass", nearWater: true)),
                Sp("solid", Cells("Solid")),
                Sp("grassHigh", Cells("Grass", minY: 3))), w);
            var st = EmptyState();
            Assert.AreEqual(6, t.Current(0, 0, st, DayPeriod.Day), "grass");
            Assert.AreEqual(2, t.Current(1, 0, st, DayPeriod.Day), "stone exposed (cell covered by stone not counted)");
            Assert.AreEqual(2, t.Current(2, 0, st, DayPeriod.Day), "grass near water (same tier + tier above)");
            Assert.AreEqual(256 + 6 + 3 + 1, t.Current(3, 0, st, DayPeriod.Day), "solid = bedrock + grass + stone + dirt, no water");
            Assert.AreEqual(1, t.Current(4, 0, st, DayPeriod.Day), "grass with y >= 3");
        }

        [Test]
        public void RegionLimitsCounting()
        {
            var w = Layout();
            var cfg = Cfg(Sp("grass", Cells("Grass")));
            cfg.regionX0 = 0; cfg.regionZ0 = 0; cfg.regionX1 = 8; cfg.regionZ1 = 8;
            var t = new HabitatTracker(cfg, w);
            Assert.AreEqual(4, t.Current(0, 0, null, DayPeriod.Day), "(2,3,4 at y=1) + (1,3,1); cells outside region ignored");
            cfg.regionX1 = 17;
            Assert.Throws<ArgumentException>(() => new HabitatTracker(cfg, w));
            cfg.regionX1 = 8; cfg.regionZ1 = 17;
            Assert.Throws<ArgumentException>(() => new HabitatTracker(cfg, w));
        }

        [Test]
        public void DigPlaceUpdatesImmediately()
        {
            var w = Layout();
            var inv = new PlayerBag();
            inv.Add(BlockMaterial.Grass, 5);
            inv.Add(BlockMaterial.Dirt, 5);
            inv.Add(BlockMaterial.Stone, 5);
            var editor = new WorldEditor(w, inv, 2);
            var t = new HabitatTracker(Cfg(Sp("grass", Cells("Grass")), Sp("stoneExp", Cells("Stone", exposed: true))), w);
            editor.CellChanged += t.OnCellChanged;
            Assert.AreEqual(EditResult.Ok, editor.TryPlace(6, 1, 6, BlockMaterial.Grass));
            Assert.AreEqual(7, t.Current(0, 0, null, DayPeriod.Day));
            Assert.AreEqual(EditResult.Ok, editor.TryDig(6, 1, 6));
            Assert.AreEqual(6, t.Current(0, 0, null, DayPeriod.Day));
            // Đặt đất lên đá lộ thiên (8,1,8): ô đá không còn lộ thiên.
            Assert.AreEqual(EditResult.Ok, editor.TryPlace(8, 2, 8, BlockMaterial.Dirt));
            Assert.AreEqual(1, t.Current(1, 0, null, DayPeriod.Day));
            // Đặt đá lên đá lộ thiên: ô cũ hết lộ thiên, ô mới lộ thiên → số đếm giữ nguyên.
            Assert.AreEqual(EditResult.Ok, editor.TryPlace(8, 3, 8, BlockMaterial.Stone));
            Assert.AreEqual(2, t.Current(1, 0, null, DayPeriod.Day), "new stone (8,3,8) exposed; (8,2,8) is dirt now");
            Assert.AreEqual(EditResult.Ok, editor.TryDig(8, 3, 8));
            Assert.AreEqual(1, t.Current(1, 0, null, DayPeriod.Day));
            Assert.AreEqual(EditResult.Ok, editor.TryDig(8, 2, 8));
            Assert.AreEqual(2, t.Current(1, 0, null, DayPeriod.Day), "(8,1,8) exposed again");
        }

        [Test]
        public void TilledGrassNotCounted()
        {
            var w = Layout();
            var editor = new WorldEditor(w, new PlayerBag(), 2);
            var t = new HabitatTracker(Cfg(Sp("grass", Cells("Grass"))), w);
            editor.CellChanged += t.OnCellChanged;
            Assert.IsTrue(editor.TryTill(2, 1, 2));
            Assert.AreEqual(5, t.Current(0, 0, null, DayPeriod.Day), "Farmland is not Grass");
            Assert.IsTrue(editor.TryGrow(10, 1, 10));
            Assert.AreEqual(6, t.Current(0, 0, null, DayPeriod.Day));
        }

        [Test]
        public void WaterSpreadUpdatesNearWater()
        {
            var w = Layout();
            var editor = new WorldEditor(w, new PlayerBag(), 2, 3);
            var cfg = Cfg(Sp("a", Cells("Grass", nearWater: true), Cells("Solid", nearWater: true, exposed: true)), Sp("b", Cells("Stone", nearWater: true)));
            var t = new HabitatTracker(cfg, w);
            editor.CellChanged += t.OnCellChanged;
            Assert.AreEqual(EditResult.Ok, editor.TryDig(3, 1, 2));   // chạm nước (3,1,3) → nước lan
            int filled = editor.SettleWater();
            Assert.Greater(filled, 10, "water should spread");
            var fresh = new HabitatTracker(cfg, w);
            var st = EmptyState();
            for (int s = 0; s < t.SpeciesCount; s++)
                for (int r = 0; r < t.RequirementCount(s); r++)
                    Assert.AreEqual(fresh.Current(s, r, st, DayPeriod.Day), t.Current(s, r, st, DayPeriod.Day), $"species {s} req {r}");
        }

        [Test]
        public void NonCellRequirements()
        {
            var sim = new Simulation(TestWorlds.NoWander(), TestWorlds.Flat(1));
            var cfg = Cfg(
                Sp("anyCrop", new HabitatRequirement { kind = "crops" }),
                Sp("crop1", new HabitatRequirement { kind = "crops", cropId = 1 }),
                Sp("storage", new HabitatRequirement { kind = "structures", structure = "storage" }),
                Sp("house", new HabitatRequirement { kind = "structures", structure = "house" }),
                Sp("l2s", new HabitatRequirement { kind = "species", min = 2, species = "L2" }),
                Sp("dayish", new HabitatRequirement { kind = "period", periods = new[] { "Dawn", "Day" } }));
            var t = new HabitatTracker(cfg, sim.World);
            var st = sim.State;
            int Cur(int s, DayPeriod p = DayPeriod.Night) => t.Current(s, 0, st, p);

            for (int s = 0; s < 5; s++) Assert.AreEqual(0, Cur(s), "species " + s + " starts at 0");
            int plot = sim.MarkTill(new CellPos(5, 1, 5));
            Assert.GreaterOrEqual(plot, 0);
            var p0 = st.Plots[plot];
            foreach (var ph in new[] { PlotPhase.Idle, PlotPhase.Till, PlotPhase.Sow })
            {
                p0.CurrentCrop = 0;
                p0.Phase = ph;
                Assert.AreEqual(0, Cur(0), "not sown yet: " + ph);
            }
            foreach (var ph in new[] { PlotPhase.Water, PlotPhase.Growing, PlotPhase.Harvest })
            {
                p0.CurrentCrop = 0;
                p0.Phase = ph;
                Assert.AreEqual(1, Cur(0), "sown: " + ph);
                Assert.AreEqual(0, Cur(1), "crop filter, plot has crop 0: " + ph);
                p0.CurrentCrop = 1;
                Assert.AreEqual(1, Cur(1), "crop filter, plot has crop 1: " + ph);
            }
            p0.CurrentCrop = -1;
            Assert.AreEqual(0, Cur(0), "no crop");
            p0.CurrentCrop = 0;
            p0.Removed = true;
            Assert.AreEqual(0, Cur(0), "removed plot not counted");

            sim.AddStorage(new CellPos(6, 2, 9));
            Assert.AreEqual(1, Cur(2));
            sim.AddHouse(new CellPos(2, 2, 2));
            Assert.AreEqual(1, Cur(3));
            sim.AddCreature("L2", new CellPos(8, 2, 5));
            Assert.AreEqual(1, Cur(4));
            Assert.IsFalse(t.IsMet(4, 0, st, DayPeriod.Day), "min 2");
            sim.AddCreature("L2", new CellPos(9, 2, 5));
            Assert.IsTrue(t.IsMet(4, 0, st, DayPeriod.Day));

            Assert.AreEqual(1, t.Target(5, 0));
            Assert.IsTrue(t.IsMet(5, 0, st, DayPeriod.Dawn));
            Assert.IsTrue(t.IsMet(5, 0, st, DayPeriod.Day));
            Assert.IsFalse(t.IsMet(5, 0, st, DayPeriod.Dusk));
            Assert.AreEqual(0, t.Current(5, 0, st, DayPeriod.Night));
        }

        [Test]
        public void UnlockOnceInConfigOrder()
        {
            var inv = new PlayerBag();
            inv.Add(BlockMaterial.Grass, 5);
            inv.Add(BlockMaterial.Stone, 5);
            var editor = new WorldEditor(new WorldData(new WorldSize(16, 16, 16)), inv, 2);
            var world = editor.World;
            for (int z = 0; z < 16; z++)
            for (int x = 0; x < 16; x++)
                Put(world, x, 0, z, BlockMaterial.Bedrock);
            var cfg = Cfg(Sp("stoneSp", Cells("Stone")), Sp("never"), Sp("grassSp", Cells("Grass")));
            var t = new HabitatTracker(cfg, world);
            editor.CellChanged += t.OnCellChanged;
            var st = EmptyState();
            var returned = new List<string>();
            Assert.AreEqual(0, t.Update(st, DayPeriod.Day, returned));
            Assert.AreEqual(EditResult.Ok, editor.TryPlace(6, 1, 6, BlockMaterial.Grass));
            Assert.AreEqual(EditResult.Ok, editor.TryPlace(7, 1, 7, BlockMaterial.Stone));
            Assert.AreEqual(2, t.Update(st, DayPeriod.Day, returned));
            CollectionAssert.AreEqual(new[] { "stoneSp", "grassSp" }, returned);
            Assert.IsTrue(t.IsUnlocked(0));
            Assert.IsFalse(t.IsUnlocked(1));
            Assert.IsTrue(t.IsUnlocked(2));
            Assert.AreEqual(0, t.Update(st, DayPeriod.Day, returned));
            Assert.AreEqual(2, returned.Count);
            Assert.AreEqual(EditResult.Ok, editor.TryDig(6, 1, 6));
            Assert.IsFalse(t.IsMet(2, 0, st, DayPeriod.Day));
            Assert.IsTrue(t.IsUnlocked(2), "unlocked species never lock again");
            Assert.AreEqual(0, t.Update(st, DayPeriod.Day, returned));
            Assert.IsFalse(t.IsUnlocked(1), "species without requirements never opens");
        }

        [Test]
        public void UnlockedAtStartRespected()
        {
            var w = Layout();
            var t = new HabitatTracker(new HabitatConfig(), w);
            Assert.IsTrue(t.IsUnlocked(0), "L1 unlocked at start");
            Assert.IsFalse(t.IsUnlocked(1));
            Assert.IsFalse(t.IsUnlocked(2));
            var returned = new List<string>();
            Assert.AreEqual(0, t.Update(EmptyState(), DayPeriod.Day, returned));
            Assert.AreEqual(0, returned.Count);
            Assert.AreEqual(3, t.SpeciesCount);
            Assert.AreEqual("L2", t.SpeciesId(1));
        }

        static int[] RunOps(int seed)
        {
            var w = Layout();
            var inv = new PlayerBag();
            inv.Add(BlockMaterial.Grass, 1000);
            inv.Add(BlockMaterial.Dirt, 1000);
            inv.Add(BlockMaterial.Stone, 1000);
            var editor = new WorldEditor(w, inv, 2, 3);
            var cfg = Cfg(Sp("a", Cells("Grass"), Cells("Grass", nearWater: true)), Sp("b", Cells("Stone", exposed: true), Cells("Solid", minY: 2)));
            var t = new HabitatTracker(cfg, w);
            editor.CellChanged += t.OnCellChanged;
            var rng = new System.Random(seed);
            var mats = new[] { BlockMaterial.Grass, BlockMaterial.Dirt, BlockMaterial.Stone };
            for (int i = 0; i < 300; i++)
            {
                int x = rng.Next(16), y = rng.Next(1, 6), z = rng.Next(16);
                switch (rng.Next(4))
                {
                    case 0: editor.TryDig(x, y, z); break;
                    case 1: editor.TryPlace(x, y, z, mats[rng.Next(3)]); break;
                    case 2: editor.TryTill(x, y, z); break;
                    default: editor.TryGrow(x, y, z); break;
                }
                if (i % 20 == 19) editor.SettleWater();
            }
            var st = EmptyState();
            var list = new List<int>();
            for (int s = 0; s < t.SpeciesCount; s++)
                for (int r = 0; r < t.RequirementCount(s); r++)
                    list.Add(t.Current(s, r, st, DayPeriod.Day));
            return list.ToArray();
        }

        [Test]
        public void Deterministic()
        {
            var a = RunOps(77);
            var b = RunOps(77);
            CollectionAssert.AreEqual(a, b);
            CollectionAssert.AreNotEqual(a, RunOps(78), "different command sequence should differ");
        }
    }
}
