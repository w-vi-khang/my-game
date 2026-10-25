using System;
using System.Collections.Generic;
using Game.Core.Habitat;
using Game.Core.Sim;
using NUnit.Framework;

namespace Game.Core.Tests
{
    public class HabitatSpawnTests
    {
        static Simulation NewSim() => new Simulation(TestWorlds.NoWander(), TestWorlds.Flat(1));

        [Test]
        public void SpawnsAtNearestEdgeCells()
        {
            List<CellPos> Run()
            {
                var sim = NewSim();
                var created = new List<int>();
                Assert.AreEqual(2, HabitatSpawn.SpawnAtEdge(sim, 0, 0, 16, 16, 8, 3, "L2", 2, created));
                Assert.AreEqual(2, created.Count);
                var cells = new List<CellPos>();
                foreach (int id in created) cells.Add(sim.State.Creatures[id].Cell);
                return cells;
            }
            var a = Run();
            foreach (var c in a)
            {
                Assert.AreEqual(0, c.Z, "edge z == z0");
                Assert.AreEqual(2, c.Y);
            }
            Assert.AreEqual(8, a[0].X);
            Assert.AreEqual(7, a[1].X);
            CollectionAssert.AreEqual(a, Run(), "deterministic");
        }

        [Test]
        public void SkipsBlockedAndWaterColumns()
        {
            var sim = NewSim();
            sim.World.Set(8, 2, 0, Cell.Make(BlockMaterial.Water, Cell.MaxWaterLevel)); // cột (8, 0): nước trên cùng
            sim.AddCreature("L1", new CellPos(7, 2, 0));                                 // cột (7, 0): đã có sinh vật
            var created = new List<int>();
            Assert.AreEqual(1, HabitatSpawn.SpawnAtEdge(sim, 0, 0, 16, 16, 8, 0, "L2", 1, created));
            var c = sim.State.Creatures[created[0]].Cell;
            Assert.AreEqual(9, c.X);
            Assert.AreEqual(0, c.Z);
        }

        [Test]
        public void AssignsFirstHouseWithRoom()
        {
            var sim = NewSim();
            int h0 = sim.AddHouse(new CellPos(10, 2, 10));
            int h1 = sim.AddHouse(new CellPos(5, 2, 10));
            int cap = sim.State.Houses[h0].Capacity;
            for (int i = 0; i < cap - 1; i++)
            {
                int id = sim.AddCreature("L1", new CellPos(1 + i, 2, 5));
                sim.AssignHome(id, h0);
            }
            var created = new List<int>();
            Assert.AreEqual(2, HabitatSpawn.SpawnAtEdge(sim, 0, 0, 16, 16, 8, 0, "L2", 2, created));
            Assert.AreEqual(h0, sim.State.Creatures[created[0]].HomeId);
            Assert.AreEqual(h1, sim.State.Creatures[created[1]].HomeId);

            var sim2 = NewSim();
            var created2 = new List<int>();
            Assert.AreEqual(2, HabitatSpawn.SpawnAtEdge(sim2, 0, 0, 16, 16, 8, 0, "L2", 2, created2));
            foreach (int id in created2) Assert.AreEqual(-1, sim2.State.Creatures[id].HomeId);
        }

        [Test]
        public void CanPlaceCreatureMatchesAddCreature()
        {
            var sim = NewSim();
            var ok = new CellPos(5, 2, 5);
            Assert.IsTrue(sim.CanPlaceCreature("L1", ok));
            Assert.DoesNotThrow(() => sim.AddCreature("L1", ok));
            Assert.IsFalse(sim.CanPlaceCreature("L1", ok));
            Assert.Throws<ArgumentException>(() => sim.AddCreature("L1", ok));
            var air = new CellPos(6, 8, 6);
            Assert.IsFalse(sim.CanPlaceCreature("L1", air));
            Assert.Throws<ArgumentException>(() => sim.AddCreature("L1", air));
        }
    }
}
