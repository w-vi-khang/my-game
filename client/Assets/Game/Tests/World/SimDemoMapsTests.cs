using System.IO;
using Game.Core;
using Game.Core.Sim;
using Game.World;
using NUnit.Framework;
using UnityEngine;

namespace Game.World.Tests
{
    /// <summary>Task m1-demo-dong-bang: trại demo bố trí giãn giữa đồng bằng san phẳng của Map 1, mô phỏng trong vùng 64 × 64.</summary>
    public class SimDemoMapsTests
    {
        static string Data(string name) => Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "design", "data", name));

        static void Setup(out WorldData world, out SimConfig cfg, out StressPlain plain)
        {
            var p = new WorldMapParams();
            world = WorldMapGenerator.Generate(p, out var maps);
            cfg = JsonUtility.FromJson<SimConfig>(File.ReadAllText(Data("sim_m1.json")));
            plain = SimStress.FlattenPlain(world, maps, 0, p.bedrockLayers, p.dirtDepth, p.waterLevel, cfg.regionSize);
        }

        static Simulation NewSim(out WorldData world, out StressPlain plain)
        {
            Setup(out world, out var cfg, out plain);
            var sim = new Simulation(cfg, world, plain.Bounds);
            SimDemo.Build(sim, plain);
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
        public void BuildPlacesPlainLayout()
        {
            var sim = NewSim(out var world, out var plain);
            var s = sim.State;
            Assert.AreEqual(2, s.Houses.Count);
            Assert.AreEqual(SimDemo.OnPlain(plain, SimDemo.PlainHouseA), s.Houses[0].Cell);
            Assert.AreEqual(SimDemo.OnPlain(plain, SimDemo.PlainHouseB), s.Houses[1].Cell);
            Assert.AreEqual(plain.Ground + 1, s.Houses[0].Cell.Y);
            foreach (var h in s.Houses)
                for (int dx = 0; dx < 2; dx++)
                    for (int dz = 0; dz < 2; dz++)
                        Assert.IsTrue(plain.Contains(h.Cell.X + dx, h.Cell.Z + dz), "house " + h.Id);
            Assert.AreEqual(1, s.Storages.Count);
            Assert.AreEqual(SimDemo.OnPlain(plain, SimDemo.PlainStorage), s.Storages[0].Cell);
            Assert.IsTrue(plain.Contains(s.Storages[0].Cell.X, s.Storages[0].Cell.Z));
            Assert.AreEqual(10, s.Stock.Seeds[0]);
            Assert.AreEqual(3, s.Creatures.Count);
            int[] homes = { 0, 0, 1 };
            for (int i = 0; i < 3; i++)
            {
                Assert.AreEqual(SimDemo.Species[i], s.Creatures[i].SpeciesId);
                Assert.AreEqual(SimDemo.OnPlain(plain, SimDemo.PlainCreatureCells[i]), s.Creatures[i].Cell);
                Assert.IsTrue(plain.Contains(s.Creatures[i].Cell.X, s.Creatures[i].Cell.Z));
                Assert.AreEqual(homes[i], s.Creatures[i].HomeId);
            }
            Assert.AreEqual(4, s.Plots.Count);
            for (int i = 0; i < 4; i++)
            {
                Assert.AreEqual(SimDemo.OnPlain(plain, SimDemo.PlainSoil[i]), s.Plots[i].Soil);
                Assert.AreEqual(plain.Ground, s.Plots[i].Soil.Y);
                Assert.IsTrue(plain.Contains(s.Plots[i].Soil.X, s.Plots[i].Soil.Z));
            }
            Assert.AreEqual(plain.Bounds, sim.Bounds);
            Assert.AreEqual(64 * 64 * world.Size.Y, sim.PathfinderCellCount);
            string line = $"SIMMEM {sim.PathfinderCellCount} cells ≈ {sim.PathfinderCellCount * 18 / 1048576.0:F1} MB (whole world {world.Size.CellCount * 18 / 1048576.0:F1} MB)";
            TestContext.WriteLine(line);
            UnityEngine.Debug.Log(line);
        }

        [Test]
        public void OneRoundFillsStorageOnPlain()
        {
            var sim = NewSim(out _, out _);
            Assert.AreEqual(0, Run(sim, 4000, false));
            Assert.AreEqual(4, sim.State.Stock.Produce[0]);
            Assert.AreEqual(10, sim.State.Stock.Seeds[0]);
        }

        [Test]
        public void KeepFarmingNoOverlapIdleNearHomeOnPlain()
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
            int produce = sim.State.Stock.Produce[0];
            string line = $"SIMPLAIN produce={produce} stalls={stalls} overlaps={overlaps} idleOnPlot={idleOnPlot} idleOutside={idleOutside}";
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
