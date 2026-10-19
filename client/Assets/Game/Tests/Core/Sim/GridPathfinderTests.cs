using System.Collections.Generic;
using Game.Core.Sim;
using NUnit.Framework;

namespace Game.Core.Tests
{
    public class GridPathfinderTests
    {
        [Test]
        public void TempBlockBlocksUntilCleared()
        {
            var pf = new GridPathfinder(TestWorlds.Flat(0));
            Assert.IsTrue(pf.CanStand(new CellPos(3, 1, 3), 1, 1, 1));
            pf.AddTempBlock(3, 1, 3);
            Assert.IsFalse(pf.CanStand(new CellPos(3, 1, 3), 1, 1, 1));
            pf.ClearTempBlocks();
            Assert.IsTrue(pf.CanStand(new CellPos(3, 1, 3), 1, 1, 1));
        }

        [Test]
        public void NearestFollowsBfsOrder()
        {
            var pf = new GridPathfinder(TestWorlds.Flat(0));
            var result = new List<CellPos>();
            var start = new CellPos(5, 1, 5);
            Assert.IsTrue(pf.FindPathToNearest(start, 1, 1, 1, 8, p => p.X == 7, result));
            CollectionAssert.AreEqual(new[] { new CellPos(6, 1, 5), new CellPos(7, 1, 5) }, result);
            Assert.IsFalse(pf.FindPathToNearest(start, 1, 1, 1, 1, p => p.X == 7, result));
            Assert.AreEqual(0, result.Count);
        }

        [Test]
        public void FlatPathIsManhattan()
        {
            var w = TestWorlds.Flat(0);
            var pf = new GridPathfinder(w);
            var path = new List<CellPos>();
            var start = new CellPos(1, 1, 1);
            Assert.IsTrue(pf.FindPath(start, new CellPos(9, 1, 6), 1, 1, 1, path));
            Assert.AreEqual(13, path.Count);
            Assert.AreEqual(new CellPos(9, 1, 6), path[path.Count - 1]);
            var prev = start;
            foreach (var p in path)
            {
                Assert.IsTrue(pf.CanStep(prev, p, 1, 1, 1), prev + " -> " + p);
                prev = p;
            }
        }

        [Test]
        public void ClimbsAndDropsOneCell()
        {
            var w = TestWorlds.Flat(0);
            TestWorlds.Wall(w, 2, 1);
            var pf = new GridPathfinder(w);
            var path = new List<CellPos>();
            Assert.IsTrue(pf.FindPath(new CellPos(1, 1, 1), new CellPos(3, 1, 1), 1, 1, 1, path));
            CollectionAssert.AreEqual(new[] { new CellPos(2, 2, 1), new CellPos(3, 1, 1) }, path);
        }

        [Test]
        public void CannotClimbTwo()
        {
            var w = TestWorlds.Flat(0);
            TestWorlds.Wall(w, 2, 2);
            var pf = new GridPathfinder(w);
            Assert.IsFalse(pf.FindPath(new CellPos(1, 1, 1), new CellPos(3, 1, 1), 1, 1, 1, new List<CellPos>()));
        }

        [Test]
        public void WaterIsNotWalkable()
        {
            var w = TestWorlds.Flat(0);
            for (int z = 0; z < 16; z++) w.Set(2, 1, z, Cell.Make(BlockMaterial.Water, 7));
            var pf = new GridPathfinder(w);
            Assert.IsFalse(pf.FindPath(new CellPos(1, 1, 1), new CellPos(3, 1, 1), 1, 1, 1, new List<CellPos>()));
        }

        [Test]
        public void TallFrameNeedsHeadroom()
        {
            var w = TestWorlds.Flat(0);
            for (int z = 0; z < 16; z++) w.Set(5, 2, z, Cell.Make(BlockMaterial.Stone));
            var pf = new GridPathfinder(w);
            var path = new List<CellPos>();
            Assert.IsTrue(pf.FindPath(new CellPos(1, 1, 1), new CellPos(9, 1, 1), 1, 1, 1, path));
            Assert.AreEqual(8, path.Count);
            Assert.IsFalse(pf.FindPath(new CellPos(1, 1, 1), new CellPos(9, 1, 1), 1, 1, 2, path));
        }

        [Test]
        public void WideFrameNeedsWideGap()
        {
            var w = TestWorlds.Flat(0);
            TestWorlds.Wall(w, 5, 2);
            w.Set(5, 1, 7, Cell.Make(BlockMaterial.Empty));
            w.Set(5, 2, 7, Cell.Make(BlockMaterial.Empty));
            var pf = new GridPathfinder(w);
            var path = new List<CellPos>();
            Assert.IsTrue(pf.FindPath(new CellPos(1, 1, 7), new CellPos(9, 1, 7), 1, 1, 1, path));
            Assert.IsFalse(pf.FindPath(new CellPos(1, 1, 6), new CellPos(8, 1, 6), 2, 2, 1, path));
            w.Set(5, 1, 8, Cell.Make(BlockMaterial.Empty));
            w.Set(5, 2, 8, Cell.Make(BlockMaterial.Empty));
            Assert.IsTrue(pf.FindPath(new CellPos(1, 1, 6), new CellPos(8, 1, 6), 2, 2, 1, path));
        }

        [Test]
        public void SameQuerySamePath()
        {
            var w = TestWorlds.Flat(0);
            TestWorlds.Wall(w, 5, 1);
            var pf = new GridPathfinder(w);
            var a = new List<CellPos>();
            var b = new List<CellPos>();
            Assert.IsTrue(pf.FindPath(new CellPos(1, 1, 1), new CellPos(9, 1, 9), 1, 1, 1, a));
            Assert.IsTrue(pf.FindPath(new CellPos(1, 1, 1), new CellPos(9, 1, 9), 1, 1, 1, b));
            CollectionAssert.AreEqual(a, b);
        }

        [Test]
        public void FindPathToAnyPicksNearest()
        {
            var pf = new GridPathfinder(TestWorlds.Flat(0));
            var path = new List<CellPos>();
            var start = new CellPos(1, 1, 1);
            Assert.IsTrue(pf.FindPathToAny(start, new List<CellPos> { new CellPos(9, 1, 1), new CellPos(3, 1, 4) }, 1, 1, 1, path));
            Assert.AreEqual(5, path.Count);
            Assert.AreEqual(new CellPos(3, 1, 4), path[path.Count - 1]);

            Assert.IsTrue(pf.FindPathToAny(start, new List<CellPos> { new CellPos(3, 5, 4), new CellPos(9, 1, 1) }, 1, 1, 1, path));
            Assert.AreEqual(8, path.Count);
            Assert.AreEqual(new CellPos(9, 1, 1), path[path.Count - 1]);

            Assert.IsTrue(pf.FindPathToAny(start, new List<CellPos> { start }, 1, 1, 1, path));
            Assert.AreEqual(0, path.Count);

            Assert.IsFalse(pf.FindPathToAny(start, new List<CellPos>(), 1, 1, 1, path));
        }
    }
}
