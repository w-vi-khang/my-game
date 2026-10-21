using System;
using System.Collections.Generic;
using Game.Core.Sim;
using NUnit.Framework;

namespace Game.Core.Tests
{
    /// <summary>World 7e (brief 0019): vùng mô phỏng — tìm đường và Simulation chỉ làm việc trong hộp cột.</summary>
    public class SimBoundsTests
    {
        [Test]
        public void WholeBoundsMatchesWorld()
        {
            var pf = new GridPathfinder(TestWorlds.Flat(0));
            Assert.AreEqual(new SimBounds(0, 0, 16, 16), pf.Bounds);
            Assert.AreEqual(16 * 16 * 16, pf.CellCount);
        }

        [Test]
        public void RegionLimitsStandAndPath()
        {
            var w = TestWorlds.Flat(0);
            var b = new SimBounds(4, 4, 8, 8);
            var pf = new GridPathfinder(w, b);
            Assert.AreEqual(8 * 8 * 16, pf.CellCount);
            Assert.IsTrue(pf.CanStand(new CellPos(4, 1, 4), 1, 1, 1));
            Assert.IsTrue(pf.CanStand(new CellPos(11, 1, 11), 1, 1, 1));
            Assert.IsFalse(pf.CanStand(new CellPos(3, 1, 5), 1, 1, 1));
            Assert.IsFalse(pf.CanStand(new CellPos(12, 1, 11), 1, 1, 1));

            var path = new List<CellPos>();
            Assert.IsTrue(pf.FindPath(new CellPos(4, 1, 4), new CellPos(11, 1, 11), 1, 1, 1, path));
            foreach (var c in path) Assert.IsTrue(b.Contains(c.X, c.Z), c.ToString());
            Assert.IsFalse(pf.FindPath(new CellPos(5, 1, 5), new CellPos(13, 1, 5), 1, 1, 1, new List<CellPos>()));

            var flood = new List<CellPos>();
            pf.FloodRegion(new List<CellPos> { new CellPos(5, 1, 5) }, 1, 1, 1, p => true, flood);
            Assert.AreEqual(64, flood.Count);
            foreach (var c in flood) Assert.IsTrue(b.Contains(c.X, c.Z), c.ToString());

            Assert.DoesNotThrow(() => pf.AddTempBlock(0, 1, 0));
            Assert.Throws<ArgumentException>(() => pf.SetBlocked(new CellPos(0, 1, 0), true));
        }

        [Test]
        public void RegionPathEqualsWholeWorldPathInside()
        {
            var w = TestWorlds.Flat(0);
            TestWorlds.Pillar(w, 6, 5, 2);
            TestWorlds.Pillar(w, 6, 6, 2);
            TestWorlds.Pillar(w, 6, 7, 2);
            var b = new SimBounds(2, 2, 12, 12);
            var whole = new GridPathfinder(w);
            var part = new GridPathfinder(w, b);
            var pw = new List<CellPos>();
            var pp = new List<CellPos>();
            Assert.IsTrue(whole.FindPath(new CellPos(3, 1, 6), new CellPos(10, 1, 6), 1, 1, 1, pw));
            Assert.IsTrue(part.FindPath(new CellPos(3, 1, 6), new CellPos(10, 1, 6), 1, 1, 1, pp));
            Assert.AreEqual(pw.Count, pp.Count);
            foreach (var c in pp) Assert.IsTrue(b.Contains(c.X, c.Z), c.ToString());
        }

        [Test]
        public void InvalidBoundsThrow()
        {
            var w = TestWorlds.Flat(0);
            Assert.Throws<ArgumentException>(() => new GridPathfinder(w, new SimBounds(-1, 0, 8, 8)));
            Assert.Throws<ArgumentException>(() => new GridPathfinder(w, new SimBounds(0, 0, 17, 8)));
            Assert.Throws<ArgumentException>(() => new GridPathfinder(w, new SimBounds(10, 10, 8, 8)));
            Assert.Throws<ArgumentException>(() => new SimBounds(0, 0, 0, 4));
        }

        [Test]
        public void SimulationRejectsOutsideRegion()
        {
            var w = TestWorlds.Flat(1);
            var sim = new Simulation(new SimConfig(), w, new SimBounds(0, 0, 8, 8));
            Assert.AreEqual(new SimBounds(0, 0, 8, 8), sim.Bounds);
            Assert.AreEqual(8 * 8 * 16, sim.PathfinderCellCount);
            int storage = sim.AddStorage(new CellPos(2, 2, 2));
            Assert.Throws<ArgumentException>(() => sim.AddCreature("L1", new CellPos(10, 2, 10)));
            Assert.Throws<ArgumentException>(() => sim.AddStorage(new CellPos(10, 2, 10)));
            Assert.Throws<ArgumentException>(() => sim.AddHouse(new CellPos(10, 2, 10)));
            Assert.AreEqual(-1, sim.MarkTill(new CellPos(10, 1, 10)));
            Assert.GreaterOrEqual(sim.MarkTill(new CellPos(3, 1, 3)), 0);
        }

        [Test]
        public void RegionSizeValidated()
        {
            Assert.Throws<ArgumentException>(() => new SimConfig { regionSize = 15 }.Validate());
            Assert.Throws<ArgumentException>(() => new SimConfig { regionSize = 257 }.Validate());
            Assert.DoesNotThrow(() => new SimConfig { regionSize = 64 }.Validate());
        }
    }
}
