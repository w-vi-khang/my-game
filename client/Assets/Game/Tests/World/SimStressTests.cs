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
    /// <summary>Sim bước 7 (brief 0028): đồng bằng 48 × 48 giữa Map 1, 60 sinh vật, đo pha không đổi mã băm.</summary>
    public class SimStressTests
    {
        static string Data(string name) => Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "design", "data", name));

        static void Setup(out WorldMapParams p, out WorldData world, out MapTable maps, out List<Landmark> lms, out SimConfig cfg, out StressPlain plain)
        {
            p = new WorldMapParams();
            world = WorldMapGenerator.Generate(p, out maps);
            lms = LandmarkPlacer.Place(world, maps, p);
            cfg = JsonUtility.FromJson<SimConfig>(File.ReadAllText(Data("sim_m1.json")));
            plain = SimStress.FlattenPlain(world, maps, 0, p.bedrockLayers, p.dirtDepth, p.waterLevel, cfg.regionSize);
        }

        static Simulation NewSim(out StressPlain plain)
        {
            Setup(out _, out var world, out _, out _, out var cfg, out plain);
            var sim = new Simulation(cfg, world, plain.Bounds);
            SimStress.Build(sim, plain);
            return sim;
        }

        [Test]
        public void PlainIsFlatDryGrass()
        {
            Setup(out var p, out var world, out var maps, out _, out _, out var plain);
            TestContext.WriteLine(plain.ToString());
            Assert.Greater(plain.Ground, p.waterLevel);
            for (int z = plain.Z0; z < plain.Z0 + plain.Size; z++)
                for (int x = plain.X0; x < plain.X0 + plain.Size; x++)
                {
                    MinimapImage.ColumnInfo(world, x, z, out int surf, out int depth);
                    Assert.AreEqual(plain.Ground, surf, $"({x}, {z})");
                    Assert.AreEqual(0, depth, $"({x}, {z})");
                    Assert.AreEqual(BlockMaterial.Grass, world.GetMaterial(x, plain.Ground, z));
                }
            int xe = plain.X0 + plain.Size - 1, ze = plain.Z0 + plain.Size - 1;
            Assert.IsTrue(plain.Bounds.Contains(plain.X0, plain.Z0));
            Assert.IsTrue(plain.Bounds.Contains(xe, plain.Z0));
            Assert.IsTrue(plain.Bounds.Contains(plain.X0, ze));
            Assert.IsTrue(plain.Bounds.Contains(xe, ze));
            Assert.AreEqual(0, maps.Get(plain.X0 + plain.Size / 2, plain.Z0 + plain.Size / 2));
        }

        [Test]
        public void BuildPlaces60CreaturesWithHomes()
        {
            var sim = NewSim(out var plain);
            var s = sim.State;
            Assert.AreEqual(60, s.Creatures.Count);
            var perSpecies = new Dictionary<string, int>();
            foreach (var c in s.Creatures)
            {
                Assert.GreaterOrEqual(c.HomeId, 0);
                perSpecies[c.SpeciesId] = perSpecies.TryGetValue(c.SpeciesId, out int n) ? n + 1 : 1;
            }
            foreach (var sp in SimDemo.Species)
                Assert.AreEqual(20, perSpecies[sp], sp);
            Assert.AreEqual(30, s.Houses.Count);
            foreach (var h in s.Houses)
                Assert.AreEqual(2, h.Residents.Count);
            Assert.AreEqual(5, s.Storages.Count);
            Assert.AreEqual(60, s.Plots.Count);
            foreach (var h in s.Houses)
                Assert.IsTrue(plain.Bounds.Contains(h.Cell.X, h.Cell.Z));
            foreach (var st in s.Storages)
                Assert.IsTrue(plain.Bounds.Contains(st.Cell.X, st.Cell.Z));
            foreach (var pl in s.Plots)
                Assert.IsTrue(plain.Bounds.Contains(pl.Soil.X, pl.Soil.Z));
        }

        [Test]
        public void WithoutPlainDropsOnlyInside()
        {
            Setup(out _, out _, out _, out var lms, out _, out var plain);
            var kept = SimStress.WithoutPlain(lms, plain);
            int inside = 0;
            foreach (var l in lms)
                if (plain.Contains(l.X, l.Z)) inside++;
            foreach (var l in kept)
                Assert.IsFalse(plain.Contains(l.X, l.Z));
            Assert.AreEqual(lms.Count, kept.Count + inside);
            TestContext.WriteLine($"dấu mốc: {lms.Count}, giữ {kept.Count}, trong đồng bằng {inside}");
        }

        [Test]
        public void Runs4000Ticks()
        {
            var sim = NewSim(out _);
            var cs = sim.State.Creatures;
            int overlaps = 0, idleOnPlot = 0, idleOutside = 0;
            var sw = Stopwatch.StartNew();
            for (int t = 0; t < 4000; t++)
            {
                SimDemo.KeepFarming(sim);
                sim.Tick();
                sim.State.Events.Clear();
                for (int i = 0; i < cs.Count; i++)
                {
                    var a = cs[i];
                    if (a.State == CreatureState.Idle)
                    {
                        if (sim.OnPlot(a.Cell, a.SizeX, a.SizeZ, a.SizeY)) idleOnPlot++;
                        if (Simulation.HomeDistance(a.Cell, a.SizeX, a.SizeZ, sim.State.Houses[a.HomeId]) > sim.Config.creature.homeRadius) idleOutside++;
                    }
                    for (int j = i + 1; j < cs.Count; j++)
                    {
                        var b = cs[j];
                        if (a.Phasing || b.Phasing) continue;
                        if (Simulation.FramesOverlap(a.Cell, a.SizeX, a.SizeZ, a.SizeY, b.Cell, b.SizeX, b.SizeZ, b.SizeY))
                            overlaps++;
                    }
                }
            }
            long produce = sim.State.Stock.ProduceTotal;
            TestContext.WriteLine($"4000 nhịp: {sw.Elapsed.TotalMilliseconds:F0} ms, AStarCount={sim.AStarCount}, BfsCount={sim.BfsCount}, produce={produce}, overlaps={overlaps}, idleOnPlot={idleOnPlot}, idleOutside={idleOutside}");
            Assert.AreEqual(0, overlaps);
            Assert.AreEqual(0, idleOnPlot);
            Assert.AreEqual(0, idleOutside);
            Assert.Greater(produce, 0);
            Assert.Greater(sim.AStarCount, 0);
        }

        [Test]
        public void ProfilingKeepsHash()
        {
            var a = NewSim(out _);
            var b = NewSim(out _);
            a.ProfilePhases = true;
            for (int t = 1; t <= 2000; t++)
            {
                SimDemo.KeepFarming(a);
                SimDemo.KeepFarming(b);
                a.Tick();
                b.Tick();
                a.State.Events.Clear();
                b.State.Events.Clear();
                if (t % 100 == 0)
                    Assert.AreEqual(SimHash.Compute(b.State), SimHash.Compute(a.State), "nhịp " + t);
            }
            Assert.Greater(a.PhaseTicks[2], 0);
            for (int k = 0; k < 9; k++)
                Assert.AreEqual(0, b.PhaseTicks[k], Simulation.PhaseNames[k]);
        }
    }
}
