using System.Collections.Generic;
using System.Diagnostics;
using Game.Core;
using Game.Core.DayCycle;
using Game.Core.Habitat;
using Game.Core.Sim;
using NUnit.Framework;

namespace Game.World.Tests
{
    /// <summary>Môi trường sống lõi (sim 8a, brief 0069) trên world sinh thật.</summary>
    public class HabitatWorldTests
    {
        static HabitatRequirement Cells(string mat, int min = 1, bool exposed = false, bool nearWater = false, int minY = 0) =>
            new HabitatRequirement { kind = "cells", min = min, material = mat, exposed = exposed, nearWater = nearWater, minY = minY };

        static HabitatConfig Cfg() => new HabitatConfig
        {
            unlockedAtStart = new string[0],
            species = new[]
            {
                new HabitatSpecies("a", Cells("Grass"), Cells("Grass", nearWater: true), Cells("Grass", exposed: true)),
                new HabitatSpecies("b", Cells("Stone", exposed: true), Cells("Solid", nearWater: true), Cells("Solid", minY: 12)),
                new HabitatSpecies("c", Cells("Sand", nearWater: true), Cells("Dirt", exposed: true, nearWater: true), Cells("Farmland")),
            },
        };

        [Test]
        public void IncrementalMatchesFullRescan()
        {
            var p = new TerrainParams();
            var world = TerrainGenerator.Generate(p);
            var inv = new PlayerBag();
            inv.Add(BlockMaterial.Grass, 10000);
            inv.Add(BlockMaterial.Dirt, 10000);
            inv.Add(BlockMaterial.Stone, 10000);
            inv.Add(BlockMaterial.Sand, 10000);
            var editor = new WorldEditor(world, inv, BlockMesher.CellMargin, p.waterLevel);
            var cfg = Cfg();
            var tracker = new HabitatTracker(cfg, world);
            editor.CellChanged += tracker.OnCellChanged;
            var st = new Simulation(TestSimConfig(), new WorldData(new WorldSize(16, 16, 16))).State;
            var s = world.Size;
            var rng = new System.Random(12345);
            var mats = new[] { BlockMaterial.Grass, BlockMaterial.Dirt, BlockMaterial.Stone, BlockMaterial.Sand };

            int edits = 0, checks = 0;
            for (int attempt = 0; edits < 300 && attempt < 40000; attempt++)
            {
                int x = rng.Next(s.X), z = rng.Next(s.Z);
                int surface = world.SurfaceHeight(x, z);
                int y = System.Math.Clamp(surface + rng.Next(-2, 3), 1, s.Y - 2);
                bool ok;
                switch (rng.Next(4))
                {
                    case 0: ok = editor.TryDig(x, y, z) == EditResult.Ok; break;
                    case 1: ok = editor.TryPlace(x, y, z, mats[rng.Next(mats.Length)]) == EditResult.Ok; break;
                    case 2: ok = editor.TryTill(x, y, z); break;
                    default: ok = editor.TryGrow(x, y, z); break;
                }
                if (!ok) continue;
                edits++;
                if (edits % 20 == 0) editor.SettleWater();
                if (edits % 30 == 0)
                {
                    checks++;
                    var fresh = new HabitatTracker(cfg, world);
                    for (int sp = 0; sp < tracker.SpeciesCount; sp++)
                        for (int r = 0; r < tracker.RequirementCount(sp); r++)
                            Assert.AreEqual(fresh.Current(sp, r, st, DayPeriod.Day), tracker.Current(sp, r, st, DayPeriod.Day),
                                $"species {sp} req {r} after {edits} edits");
                }
            }
            Assert.AreEqual(300, edits, "fewer than 300 valid edits");
            Assert.AreEqual(10, checks);
        }

        static SimConfig TestSimConfig() => new SimConfig();

        [Test]
        public void ScanTimeOnM1World()
        {
            var world = WorldMapGenerator.Generate(new WorldMapParams { seed = 20261013 }, out _);
            Assert.AreEqual(new WorldSize(384, 48, 384), world.Size);
            var sw = Stopwatch.StartNew();
            var t = new HabitatTracker(new HabitatConfig(), world);
            sw.Stop();
            var st = new Simulation(TestSimConfig(), new WorldData(new WorldSize(16, 16, 16))).State;
            var lines = new List<string>();
            for (int s = 0; s < t.SpeciesCount; s++)
                for (int r = 0; r < t.RequirementCount(s); r++)
                    lines.Add($"{t.SpeciesId(s)}[{r}]={t.Current(s, r, st, DayPeriod.Day)}/{t.Target(s, r)}");
            UnityEngine.Debug.Log($"[0069] habitat initial scan on {world.Size}: {sw.ElapsedMilliseconds} ms; counts: {string.Join(", ", lines)}");
            Assert.Less(sw.ElapsedMilliseconds, 2000);
        }
    }
}
