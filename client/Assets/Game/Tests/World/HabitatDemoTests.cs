using System.Collections.Generic;
using System.IO;
using Game.Core;
using Game.Core.DayCycle;
using Game.Core.Habitat;
using Game.Core.Sim;
using NUnit.Framework;
using UnityEngine;

namespace Game.World.Tests
{
    /// <summary>Sim 8b (brief 0074): ao, trại L1 và loài trở lại trên đồng bằng của world sinh thật.</summary>
    public class HabitatDemoTests
    {
        static string Data(string name) => Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "design", "data", name));

        static void Setup(out WorldData world, out SimConfig cfg, out StressPlain plain, out HabitatConfig hab)
        {
            var p = new WorldMapParams();
            world = WorldMapGenerator.Generate(p, out var maps);
            cfg = JsonUtility.FromJson<SimConfig>(File.ReadAllText(Data("sim_m1.json")));
            plain = SimStress.FlattenPlain(world, maps, 0, p.bedrockLayers, p.dirtDepth, p.waterLevel, cfg.regionSize);
            hab = JsonUtility.FromJson<HabitatConfig>(File.ReadAllText(Data("habitat_m1.json")));
            hab.regionX0 = plain.X0;
            hab.regionZ0 = plain.Z0;
            hab.regionX1 = plain.X0 + plain.Size;
            hab.regionZ1 = plain.Z0 + plain.Size;
        }

        static void PondOrigin(StressPlain plain, out int wx0, out int wz0)
        {
            wx0 = plain.X0 + plain.Size / 2 + SimDemo.PlainPondMin.X;
            wz0 = plain.Z0 + plain.Size / 2 + SimDemo.PlainPondMin.Z;
        }

        [Test]
        public void PondPlacedWithSandRing()
        {
            Setup(out var world, out _, out var plain, out var hab);
            SimDemo.PlacePond(world, plain);
            PondOrigin(plain, out int wx0, out int wz0);
            int water = 0, sand = 0;
            for (int z = wz0 - 2; z <= wz0 + SimDemo.PondSize + 1; z++)
                for (int x = wx0 - 2; x <= wx0 + SimDemo.PondSize + 1; x++)
                {
                    var m = Cell.GetMaterial(world.Get(x, plain.Ground, z));
                    bool inPond = x >= wx0 && x < wx0 + 4 && z >= wz0 && z < wz0 + 4;
                    bool ring = !inPond && x >= wx0 - 1 && x <= wx0 + 4 && z >= wz0 - 1 && z <= wz0 + 4;
                    if (inPond) { Assert.AreEqual(BlockMaterial.Water, m); water++; }
                    else if (ring) { Assert.AreEqual(BlockMaterial.Sand, m); sand++; }
                    else Assert.AreEqual(BlockMaterial.Grass, m, $"outside ring ({x},{z})");
                }
            Assert.AreEqual(16, water);
            Assert.AreEqual(20, sand);
            var sim = new Simulation(JsonUtility.FromJson<SimConfig>(File.ReadAllText(Data("sim_m1.json"))), world, plain.Bounds);
            var t = new HabitatTracker(hab, world);
            Assert.AreEqual(0, t.Current(1, 0, sim.State, DayPeriod.Day), "L2 grass near water");
        }

        [Test]
        public void BuildHabitatL1Only()
        {
            Setup(out var world, out var cfg, out var plain, out var hab);
            SimDemo.PlacePond(world, plain);
            var sim = new Simulation(cfg, world, plain.Bounds);
            SimDemo.BuildHabitat(sim, plain, hab.unlockedAtStart);
            var s = sim.State;
            Assert.AreEqual(3, s.Houses.Count);
            Assert.AreEqual(SimDemo.OnPlain(plain, SimDemo.PlainHouseC), s.Houses[2].Cell);
            Assert.AreEqual(1, s.Storages.Count);
            Assert.AreEqual(1, s.Creatures.Count);
            Assert.AreEqual("L1", s.Creatures[0].SpeciesId);
            Assert.AreEqual(SimDemo.OnPlain(plain, SimDemo.PlainCreatureCells[0]), s.Creatures[0].Cell);
            Assert.AreEqual(0, s.Creatures[0].HomeId);
            Assert.AreEqual(4, s.Plots.Count);
        }

        [Test]
        public void L2ReturnsAndJoinsWork()
        {
            Setup(out var world, out var cfg, out var plain, out var hab);
            SimDemo.PlacePond(world, plain);
            var sim = new Simulation(cfg, world, plain.Bounds);
            SimDemo.BuildHabitat(sim, plain, hab.unlockedAtStart);
            var bag = new PlayerBag();
            var editor = new WorldEditor(world, bag, 2);
            var tracker = new HabitatTracker(hab, world);
            editor.CellChanged += tracker.OnCellChanged;
            editor.CellChanged += sim.OnCellChanged;
            var returned = new List<string>();

            void Run(int ticks)
            {
                for (int i = 0; i < ticks; i++)
                {
                    SimDemo.KeepFarming(sim);
                    sim.Tick();
                    sim.State.Events.Clear();
                }
            }
            Run(1500);
            Assert.AreEqual(0, tracker.Update(sim.State, DayPeriod.Day, returned));

            PondOrigin(plain, out int wx0, out int wz0);
            bag.Add(BlockMaterial.Grass, 10);
            int done = 0;
            for (int i = 0; i < 4 && done < 10; i++, done++)
            {
                int x = wx0 - 1, z = wz0 + i;
                Assert.AreEqual(EditResult.Ok, editor.TryDig(x, plain.Ground, z));
                Assert.AreEqual(EditResult.Ok, editor.TryPlace(x, plain.Ground, z, BlockMaterial.Grass));
            }
            for (int i = 0; i < 4 && done < 10; i++, done++)
            {
                int x = wx0 + 4, z = wz0 + i;
                Assert.AreEqual(EditResult.Ok, editor.TryDig(x, plain.Ground, z));
                Assert.AreEqual(EditResult.Ok, editor.TryPlace(x, plain.Ground, z, BlockMaterial.Grass));
            }
            for (int i = 0; i < 2 && done < 10; i++, done++)
            {
                int x = wx0 + i, z = wz0 - 1;
                Assert.AreEqual(EditResult.Ok, editor.TryDig(x, plain.Ground, z));
                Assert.AreEqual(EditResult.Ok, editor.TryPlace(x, plain.Ground, z, BlockMaterial.Grass));
            }
            Assert.AreEqual(10, done);
            Assert.AreEqual(1, tracker.Update(sim.State, DayPeriod.Day, returned));
            Assert.AreEqual("L2", returned[0]);

            int cx = plain.X0 + plain.Size / 2, cz = plain.Z0 + plain.Size / 2;
            var created = new List<int>();
            Assert.AreEqual(2, HabitatSpawn.SpawnAtEdge(sim, hab.regionX0, hab.regionZ0, hab.regionX1, hab.regionZ1, cx, cz, "L2", 2, created));
            var start = new Dictionary<int, int>();
            foreach (int id in created)
            {
                var c = sim.State.Creatures[id];
                Assert.GreaterOrEqual(c.HomeId, 0);
                start[id] = Mathf.Abs(c.Cell.X - sim.State.Houses[c.HomeId].Cell.X) + Mathf.Abs(c.Cell.Z - sim.State.Houses[c.HomeId].Cell.Z);
            }

            bool grown = false;
            for (int i = 0; i < 4000 && !grown; i++)
            {
                Run(1);
                foreach (var p in sim.State.Plots)
                    if (!p.Removed && (p.Phase == PlotPhase.Growing || p.Phase == PlotPhase.Harvest)) grown = true;
            }
            Assert.IsTrue(grown, "a plot should reach Growing/Harvest once L2 can water");
            foreach (int id in created)
            {
                var c = sim.State.Creatures[id];
                var h = sim.State.Houses[c.HomeId].Cell;
                int d = Mathf.Abs(c.Cell.X - h.X) + Mathf.Abs(c.Cell.Z - h.Z);
                Assert.LessOrEqual(d, start[id] - 10, "creature " + id + " went home");
            }
        }
    }
}
