using System.IO;
using Game.Core;
using Game.Core.Sim;
using Game.World;
using NUnit.Framework;
using UnityEngine;

namespace Game.World.Tests
{
    /// <summary>Sim bước 6 — bố trí demo trên map M1 thật (world_m1.json) chạy được, không đình trệ.</summary>
    public class SimDemoTests
    {
        static string Data(string name) => Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "design", "data", name));

        static Simulation NewSim()
        {
            var p = JsonUtility.FromJson<TerrainParams>(File.ReadAllText(Data("world_m1.json")));
            var world = TerrainGenerator.Generate(p);
            var cfg = JsonUtility.FromJson<SimConfig>(File.ReadAllText(Data("sim_m1.json")));
            var sim = new Simulation(cfg, world);
            SimDemo.Build(sim);
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
        public void NoTwoCreaturesShareCell()
        {
            var sim = NewSim();
            var cs = sim.State.Creatures;
            int overlaps = 0;
            int phasingTicks = 0;
            for (int t = 0; t < 4000; t++)
            {
                SimDemo.KeepFarming(sim);
                sim.Tick();
                sim.State.Events.Clear();
                bool anyPhasing = false;
                for (int i = 0; i < cs.Count; i++)
                {
                    if (cs[i].Phasing) anyPhasing = true;
                    for (int j = i + 1; j < cs.Count; j++)
                    {
                        var a = cs[i];
                        var b = cs[j];
                        if (a.Phasing || b.Phasing) continue;
                        if (Simulation.FramesOverlap(a.Cell, a.SizeX, a.SizeZ, a.SizeY, b.Cell, b.SizeX, b.SizeZ, b.SizeY))
                        {
                            overlaps++;
                            if (overlaps == 1)
                                TestContext.WriteLine("chồng ô đầu tiên ở nhịp " + sim.State.Tick + ": " + DescribeCreature(a) + " | " + DescribeCreature(b));
                        }
                    }
                }
                if (anyPhasing) phasingTicks++;
            }
            TestContext.WriteLine("nhịp có Phasing: " + phasingTicks);
            Assert.AreEqual(0, overlaps);
        }

        [Test]
        public void IdleStaysNearHomeOffPlots()
        {
            var sim = NewSim();
            var cs = sim.State.Creatures;
            var prev = new CreatureState[cs.Count];
            int idleOnPlot = 0, idleOutside = 0, wanderStarts = 0;
            for (int t = 0; t < 4000; t++)
            {
                SimDemo.KeepFarming(sim);
                sim.Tick();
                sim.State.Events.Clear();
                for (int i = 0; i < cs.Count; i++)
                {
                    var c = cs[i];
                    if (c.State == CreatureState.Idle)
                    {
                        if (sim.OnPlot(c.Cell, c.SizeX, c.SizeZ, c.SizeY))
                        {
                            idleOnPlot++;
                            if (idleOnPlot == 1) TestContext.WriteLine("rảnh trên ruộng đầu tiên: nhịp " + sim.State.Tick + ", sinh vật " + c.Id);
                        }
                        if (Simulation.HomeDistance(c.Cell, c.SizeX, c.SizeZ, sim.State.Houses[c.HomeId]) > sim.Config.creature.homeRadius)
                            idleOutside++;
                    }
                    else if (c.State == CreatureState.Moving && prev[i] == CreatureState.Idle && c.StageId == -1)
                    {
                        wanderStarts++;
                    }
                    prev[i] = c.State;
                }
            }
            TestContext.WriteLine("idleOnPlot=" + idleOnPlot + " idleOutside=" + idleOutside + " wanderStarts=" + wanderStarts);
            Assert.AreEqual(0, idleOnPlot);
            Assert.AreEqual(0, idleOutside);
            Assert.Greater(wanderStarts, 0);
        }

        static string DescribeCreature(Creature c) =>
            "id " + c.Id + " ô " + c.Cell + " " + c.State + " stage " + c.StageId;

        [Test]
        public void BuildPlacesLayout()
        {
            var sim = NewSim();
            var s = sim.State;
            Assert.AreEqual(2, s.Houses.Count);
            Assert.AreEqual(SimDemo.HouseA, s.Houses[0].Cell);
            Assert.AreEqual(SimDemo.HouseB, s.Houses[1].Cell);
            Assert.AreEqual(1, s.Storages.Count);
            Assert.AreEqual(new CellPos(34, 18, 17), s.Storages[0].Cell);
            Assert.AreEqual(10, s.Stock.Seeds[0]);
            Assert.AreEqual(3, s.Creatures.Count);
            string[] sp = { "L1", "L2", "L3" };
            CellPos[] cells = { new CellPos(32, 18, 16), new CellPos(33, 18, 16), new CellPos(36, 18, 16) };
            int[] homes = { 0, 0, 1 };
            for (int i = 0; i < 3; i++)
            {
                Assert.AreEqual(sp[i], s.Creatures[i].SpeciesId);
                Assert.AreEqual(cells[i], s.Creatures[i].Cell);
                Assert.AreEqual(homes[i], s.Creatures[i].HomeId);
            }
            Assert.AreEqual(4, s.Plots.Count);
            for (int i = 0; i < 4; i++) Assert.AreEqual(SimDemo.Soil[i], s.Plots[i].Soil);
        }

        [Test]
        public void OneRoundFillsStorage()
        {
            var sim = NewSim();
            Assert.AreEqual(0, Run(sim, 4000, false));
            Assert.AreEqual(4, sim.State.Stock.Produce[0]);
            Assert.AreEqual(10, sim.State.Stock.Seeds[0]);
            foreach (var p in sim.State.Plots)
            {
                Assert.IsTrue(p.Tilled);
                Assert.AreEqual(BlockMaterial.Farmland, sim.World.GetMaterial(p.Soil.X, p.Soil.Y, p.Soil.Z));
            }
        }

        [Test]
        public void KeepFarmingContinues()
        {
            var sim = NewSim();
            Assert.AreEqual(0, Run(sim, 8000, true));
            Assert.GreaterOrEqual(sim.State.Stock.Produce[0], 8);
        }
    }
}
