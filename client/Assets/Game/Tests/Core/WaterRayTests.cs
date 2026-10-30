using System;
using NUnit.Framework;

namespace Game.Core.Tests
{
    public class WaterRayTests
    {
        static WorldData Empty() => new WorldData(new WorldSize(16, 16, 16));

        static WorldData WithWater(ushort cell)
        {
            var w = Empty();
            w.Set(5, 3, 5, cell);
            return w;
        }

        static readonly ushort Water = Cell.Make(BlockMaterial.Water, Cell.MaxWaterLevel);

        [Test]
        public void HitsWaterAlongX()
        {
            var w = WithWater(Water);
            Assert.IsTrue(WaterRay.FirstWater(w, 0.5f, 3.5f, 5.5f, 1, 0, 0, 100f, out float t, out int nx, out int ny, out int nz));
            Assert.AreEqual(4.5f, t, 1e-5f);
            Assert.AreEqual((-1, 0, 0), (nx, ny, nz));
        }

        [Test]
        public void MaxTStops()
        {
            var w = WithWater(Water);
            Assert.IsFalse(WaterRay.FirstWater(w, 0.5f, 3.5f, 5.5f, 1, 0, 0, 4f, out _, out _, out _, out _));
        }

        [Test]
        public void HitsFromAbove()
        {
            var w = WithWater(Water);
            Assert.IsTrue(WaterRay.FirstWater(w, 5.5f, 10.5f, 5.5f, 0, -1, 0, 100f, out float t, out int nx, out int ny, out int nz));
            Assert.AreEqual(6.5f, t, 1e-5f);
            Assert.AreEqual((0, 1, 0), (nx, ny, nz));
        }

        [Test]
        public void OriginInsideWater()
        {
            var w = WithWater(Water);
            Assert.IsTrue(WaterRay.FirstWater(w, 5.5f, 3.5f, 5.5f, 1, 0, 0, 100f, out float t, out int nx, out int ny, out int nz));
            Assert.AreEqual(0f, t);
            Assert.AreEqual((0, 1, 0), (nx, ny, nz));
        }

        [Test]
        public void FallingWaterCounts()
        {
            var w = WithWater(Cell.MakeFlowing(Cell.Make(BlockMaterial.Water), 5, true));
            Assert.IsTrue(WaterRay.FirstWater(w, 0.5f, 3.5f, 5.5f, 1, 0, 0, 100f, out float t, out int nx, out int ny, out int nz));
            Assert.AreEqual(4.5f, t, 1e-5f);
            Assert.AreEqual((-1, 0, 0), (nx, ny, nz));
        }

        [Test]
        public void SolidDoesNotStop()
        {
            var w = WithWater(Water);
            w.Set(3, 3, 5, Cell.Make(BlockMaterial.Stone));
            Assert.IsTrue(WaterRay.FirstWater(w, 0.5f, 3.5f, 5.5f, 1, 0, 0, 100f, out float t, out _, out _, out _));
            Assert.AreEqual(4.5f, t, 1e-5f);
        }

        [Test]
        public void Diagonal()
        {
            var w = Empty();
            w.Set(3, 1, 0, Water);
            float len = (float)Math.Sqrt(1.25);
            Assert.IsTrue(WaterRay.FirstWater(w, 0.5f, 0.5f, 0.5f, 1f / len, 0.5f / len, 0f, 100f, out float t, out int nx, out int ny, out int nz));
            Assert.AreEqual(2.5f * len, t, 1e-4f);
            Assert.AreEqual((-1, 0, 0), (nx, ny, nz));
        }

        [Test]
        public void ZeroDirAndNull()
        {
            var w = WithWater(Water);
            Assert.IsFalse(WaterRay.FirstWater(w, 0.5f, 3.5f, 5.5f, 0, 0, 0, 100f, out _, out _, out _, out _));
            Assert.Throws<ArgumentNullException>(() => WaterRay.FirstWater(null, 0, 0, 0, 1, 0, 0, 10f, out _, out _, out _, out _));
        }

        static WorldData Row(int x0, int x1)
        {
            var w = Empty();
            for (int x = x0; x <= x1; x++)
                w.Set(x, 3, 5, Water);
            return w;
        }

        [Test]
        public void ReachesEndThroughWaterToSolid()
        {
            var w = Row(5, 7);
            w.Set(8, 3, 5, Cell.Make(BlockMaterial.Stone));
            Assert.IsTrue(WaterRay.FirstWater(w, 0.5f, 3.5f, 5.5f, 1, 0, 0, 7.5f, out float t, out _, out _, out _, out bool reachesEnd));
            Assert.AreEqual(4.5f, t, 1e-5f);
            Assert.IsTrue(reachesEnd);
        }

        [Test]
        public void AirGapBreaksRun()
        {
            var w = Row(5, 5);
            w.Set(7, 3, 5, Cell.Make(BlockMaterial.Stone));
            Assert.IsTrue(WaterRay.FirstWater(w, 0.5f, 3.5f, 5.5f, 1, 0, 0, 6.5f, out float t, out _, out _, out _, out bool reachesEnd));
            Assert.AreEqual(4.5f, t, 1e-5f);
            Assert.IsFalse(reachesEnd);
        }

        [Test]
        public void OriginInWaterReachesFloor()
        {
            var w = Empty();
            for (int y = 2; y <= 4; y++)
                w.Set(5, y, 5, Water);
            w.Set(5, 1, 5, Cell.Make(BlockMaterial.Stone));
            Assert.IsTrue(WaterRay.FirstWater(w, 5.5f, 4.5f, 5.5f, 0, -1, 0, 2.5f, out float t, out _, out _, out _, out bool reachesEnd));
            Assert.AreEqual(0f, t, 1e-6f);
            Assert.IsTrue(reachesEnd);
        }

        [Test]
        public void WaterToMaxTOnlyReachesEnd()
        {
            var w = Row(5, 9);
            Assert.IsTrue(WaterRay.FirstWater(w, 0.5f, 3.5f, 5.5f, 1, 0, 0, 7f, out _, out _, out _, out _, out bool reachesEnd));
            Assert.IsTrue(reachesEnd);
        }

        [Test]
        public void OutOfWorldBreaksRun()
        {
            var w = Row(14, 15);
            Assert.IsTrue(WaterRay.FirstWater(w, 12.5f, 3.5f, 5.5f, 1, 0, 0, 10f, out _, out _, out _, out _, out bool reachesEnd));
            Assert.IsFalse(reachesEnd);
        }
    }
}
