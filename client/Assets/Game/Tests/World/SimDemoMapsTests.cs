using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Game.Core;
using Game.Core.Sim;
using Game.World;
using NUnit.Framework;
using UnityEngine;

namespace Game.World.Tests
{
    /// <summary>World 7e (brief 0019): trại demo tự tìm chỗ trong Map 1 của world v3, mô phỏng trong vùng 64 × 64.</summary>
    public class SimDemoMapsTests
    {
        static string Data(string name) => Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "design", "data", name));

        static void Setup(out WorldData world, out MapTable maps, out List<Landmark> lms, out SimConfig cfg, out DemoSite site, out double ms)
        {
            var p = new WorldMapParams();
            world = WorldMapGenerator.Generate(p, out maps);
            lms = LandmarkPlacer.Place(world, maps, p);
            cfg = JsonUtility.FromJson<SimConfig>(File.ReadAllText(Data("sim_m1.json")));
            var sw = Stopwatch.StartNew();
            site = SimDemo.FindSite(world, maps, lms, 0, p.waterLevel, cfg.regionSize);
            ms = sw.Elapsed.TotalMilliseconds;
        }

        static Simulation NewSim(out WorldData world, out DemoSite site)
        {
            Setup(out world, out _, out _, out var cfg, out site, out _);
            var sim = new Simulation(cfg, world, site.Bounds);
            SimDemo.Build(sim, site);
            return sim;
        }

        static int Run(Simulation sim, int ticks, bool keepFarming)
        {
            int stalls = 0;
            for (int i = 0; i < ticks; i++)
            {
                if (keepFarming) SimDemo.KeepFarming(sim);
                sim.Tick();
                var ev = sim.State.Events;
                for (int k = 0; k < ev.Count; k++)
                    if (ev[k].Kind == SimEventKind.StallStarted) stalls++;
                ev.Clear();
            }
            return stalls;
        }

        [Test]
        public void SiteIsFlatDryInMap1AwayFromLandmarks()
        {
            Setup(out var world, out var maps, out var lms, out _, out var site, out double ms);
            int waterLevel = new WorldMapParams().waterLevel;
            int wx = site.Dx + SimDemo.BaseX - 1, wz = site.Dz + SimDemo.BaseZ - 1;
            for (int dz = 0; dz < SimDemo.SiteSizeZ; dz++)
                for (int dx = 0; dx < SimDemo.SiteSizeX; dx++)
                {
                    int x = wx + dx, z = wz + dz;
                    Assert.AreEqual(0, maps.Get(x, z), $"map ({x},{z})");
                    MinimapImage.ColumnInfo(world, x, z, out int surf, out int depth);
                    Assert.AreEqual(surf, site.Height(x, z), $"height ({x},{z})");
                    Assert.AreEqual(0, depth, $"nước ({x},{z})");
                    Assert.GreaterOrEqual(surf, waterLevel);
                    var m = world.GetMaterial(x, surf, z);
                    Assert.IsTrue(m == BlockMaterial.Grass || m == BlockMaterial.Dirt, $"vật liệu ({x},{z}) = {m}");
                }
            for (int dz = 0; dz < SimDemo.SiteSizeZ; dz++)
                for (int dx = 0; dx < SimDemo.SiteSizeX; dx++)
                {
                    if (dx + 1 < SimDemo.SiteSizeX)
                        Assert.LessOrEqual(System.Math.Abs(site.Height(wx + dx, wz + dz) - site.Height(wx + dx + 1, wz + dz)), 1);
                    if (dz + 1 < SimDemo.SiteSizeZ)
                        Assert.LessOrEqual(System.Math.Abs(site.Height(wx + dx, wz + dz) - site.Height(wx + dx, wz + dz + 1)), 1);
                }
            foreach (int hx in new[] { wx + 1, wx + 3 })
            {
                int h = site.Height(hx, wz + 4);
                Assert.AreEqual(h, site.Height(hx + 1, wz + 4));
                Assert.AreEqual(h, site.Height(hx, wz + 5));
                Assert.AreEqual(h, site.Height(hx + 1, wz + 5));
            }
            foreach (var c in SimDemo.Soil)
            {
                var q = site.Shift(c);
                Assert.AreEqual(site.Height(q.X, q.Z), q.Y);
            }
            var ha = site.Shift(SimDemo.HouseA);
            Assert.AreEqual(site.Height(ha.X, ha.Z) + 1, ha.Y);
            foreach (var l in lms)
                Assert.IsFalse(l.X >= wx - SimDemo.LandmarkClearance && l.X < wx + SimDemo.SiteSizeX + SimDemo.LandmarkClearance
                               && l.Z >= wz - SimDemo.LandmarkClearance && l.Z < wz + SimDemo.SiteSizeZ + SimDemo.LandmarkClearance,
                    "dấu mốc " + l.Name + " gần cửa sổ");
            Assert.AreEqual(64, site.Bounds.SizeX);
            Assert.AreEqual(64, site.Bounds.SizeZ);
            Assert.IsTrue(site.Bounds.FitsIn(world.Size));
            Assert.IsTrue(site.Bounds.Contains(wx, wz));
            Assert.IsTrue(site.Bounds.Contains(wx + SimDemo.SiteSizeX - 1, wz));
            Assert.IsTrue(site.Bounds.Contains(wx, wz + SimDemo.SiteSizeZ - 1));
            Assert.IsTrue(site.Bounds.Contains(wx + SimDemo.SiteSizeX - 1, wz + SimDemo.SiteSizeZ - 1));
            var cp = lms[0];
            double ddx = wx + 4.5 - (cp.X + 0.5), ddz = wz + 3.5 - (cp.Z + 0.5);
            double dist = System.Math.Sqrt(ddx * ddx + ddz * ddz);
            string line = $"SIMSITE {site}, cách checkpoint Map 1 {dist:F1} ô, tìm {ms:F1} ms";
            TestContext.WriteLine(line);
            UnityEngine.Debug.Log(line);
        }

        [Test]
        public void SiteIsDeterministic()
        {
            Setup(out _, out _, out _, out _, out var a, out _);
            Setup(out _, out _, out _, out _, out var b, out _);
            Assert.AreEqual(a.Dx, b.Dx);
            Assert.AreEqual(a.Dz, b.Dz);
            Assert.AreEqual(a.Bounds, b.Bounds);
            int wx = a.Dx + SimDemo.BaseX - 1, wz = a.Dz + SimDemo.BaseZ - 1;
            for (int dz = 0; dz < SimDemo.SiteSizeZ; dz++)
                for (int dx = 0; dx < SimDemo.SiteSizeX; dx++)
                    Assert.AreEqual(a.Height(wx + dx, wz + dz), b.Height(wx + dx, wz + dz));
        }

        [Test]
        public void BuildPlacesShiftedLayout()
        {
            var sim = NewSim(out var world, out var site);
            var s = sim.State;
            Assert.AreEqual(2, s.Houses.Count);
            Assert.AreEqual(site.Shift(SimDemo.HouseA), s.Houses[0].Cell);
            Assert.AreEqual(site.Shift(SimDemo.HouseB), s.Houses[1].Cell);
            Assert.AreEqual(1, s.Storages.Count);
            Assert.AreEqual(site.Shift(SimDemo.StorageCell), s.Storages[0].Cell);
            Assert.AreEqual(10, s.Storages[0].Seeds);
            Assert.AreEqual(3, s.Creatures.Count);
            int[] homes = { 0, 0, 1 };
            for (int i = 0; i < 3; i++)
            {
                Assert.AreEqual(SimDemo.Species[i], s.Creatures[i].SpeciesId);
                Assert.AreEqual(site.Shift(SimDemo.CreatureCells[i]), s.Creatures[i].Cell);
                Assert.AreEqual(homes[i], s.Creatures[i].HomeId);
            }
            Assert.AreEqual(4, s.Plots.Count);
            for (int i = 0; i < 4; i++) Assert.AreEqual(site.Shift(SimDemo.Soil[i]), s.Plots[i].Soil);
            Assert.AreEqual(site.Bounds, sim.Bounds);
            Assert.AreEqual(64 * 64 * world.Size.Y, sim.PathfinderCellCount);
            string line = $"SIMMEM {sim.PathfinderCellCount} ô ≈ {sim.PathfinderCellCount * 18 / 1048576.0:F1} MB (cả world {world.Size.CellCount * 18 / 1048576.0:F1} MB)";
            TestContext.WriteLine(line);
            UnityEngine.Debug.Log(line);
        }

        [Test]
        public void OneRoundFillsStorageOnMaps()
        {
            var sim = NewSim(out _, out _);
            Assert.AreEqual(0, Run(sim, 4000, false));
            Assert.AreEqual(4, sim.State.Storages[0].Produce);
            Assert.AreEqual(10, sim.State.Storages[0].Seeds);
        }

        [Test]
        public void KeepFarmingNoOverlapIdleNearHomeOnMaps()
        {
            var sim = NewSim(out _, out _);
            var cs = sim.State.Creatures;
            int stalls = 0, overlaps = 0, idleOnPlot = 0, idleOutside = 0;
            for (int t = 0; t < 8000; t++)
            {
                SimDemo.KeepFarming(sim);
                sim.Tick();
                var ev = sim.State.Events;
                for (int k = 0; k < ev.Count; k++)
                    if (ev[k].Kind == SimEventKind.StallStarted) stalls++;
                ev.Clear();
                for (int i = 0; i < cs.Count; i++)
                {
                    var a = cs[i];
                    for (int j = i + 1; j < cs.Count; j++)
                    {
                        var b = cs[j];
                        if (a.Phasing || b.Phasing) continue;
                        if (Simulation.FramesOverlap(a.Cell, a.SizeX, a.SizeZ, a.SizeY, b.Cell, b.SizeX, b.SizeZ, b.SizeY))
                            overlaps++;
                    }
                    if (a.State == CreatureState.Idle)
                    {
                        if (sim.OnPlot(a.Cell, a.SizeX, a.SizeZ, a.SizeY)) idleOnPlot++;
                        if (Simulation.HomeDistance(a.Cell, a.SizeX, a.SizeZ, sim.State.Houses[a.HomeId]) > sim.Config.creature.homeRadius)
                            idleOutside++;
                    }
                }
            }
            int produce = sim.State.Storages[0].Produce;
            string line = $"SIMMAPS produce={produce} stalls={stalls} overlaps={overlaps} idleOnPlot={idleOnPlot} idleOutside={idleOutside}";
            TestContext.WriteLine(line);
            UnityEngine.Debug.Log(line);
            Assert.AreEqual(0, stalls);
            Assert.GreaterOrEqual(produce, 8);
            Assert.AreEqual(0, overlaps);
            Assert.AreEqual(0, idleOnPlot);
            Assert.AreEqual(0, idleOutside);
        }
    }
}
