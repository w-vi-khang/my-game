using NUnit.Framework;

namespace Game.Core.Tests
{
    public class FlyCameraCollisionTests
    {
        const float R = 0.25f, Eps = 1e-4f;

        static WorldData MakeWorld(int floorTop, int waterTop)
        {
            var w = new WorldData(new WorldSize(16, 16, 16));
            for (int z = 0; z < 16; z++)
            for (int x = 0; x < 16; x++)
            {
                for (int y = 0; y <= floorTop; y++)
                    w.Set(x, y, z, Cell.Make(BlockMaterial.Stone));
                for (int y = floorTop + 1; y <= waterTop; y++)
                    w.Set(x, y, z, Cell.Make(BlockMaterial.Water));
            }
            return w;
        }

        static WorldData MakeWall()
        {
            var w = new WorldData(new WorldSize(16, 16, 16));
            for (int z = 0; z < 16; z++)
            for (int y = 0; y < 16; y++)
                w.Set(10, y, z, Cell.Make(BlockMaterial.Stone));
            return w;
        }

        [Test]
        public void IsSolidFollowsMesherRules()
        {
            var w = MakeWorld(3, 7);
            Assert.IsTrue(FlyCameraCollision.IsSolid(w, 8, 2, 8));
            Assert.IsFalse(FlyCameraCollision.IsSolid(w, 8, 5, 8));
            Assert.IsFalse(FlyCameraCollision.IsSolid(w, 8, 10, 8));
            Assert.IsTrue(FlyCameraCollision.IsSolid(w, 0, -1, 0));
            Assert.IsFalse(FlyCameraCollision.IsSolid(w, 0, 16, 0));
            Assert.IsTrue(FlyCameraCollision.IsSolid(w, -5, 2, 3));
            Assert.IsTrue(FlyCameraCollision.IsSolid(w, 20, 2, 3));
            Assert.IsFalse(FlyCameraCollision.IsSolid(w, -5, 5, 3));
        }

        [Test]
        public void MoveStopsOnFloor()
        {
            var w = MakeWorld(3, 3);
            float x = 8.5f, y = 6.5f, z = 8.5f;
            FlyCameraCollision.Move(w, ref x, ref y, ref z, 0, -10, 0, R);
            Assert.AreEqual(4 + R + FlyCameraCollision.Skin, y, Eps);
            Assert.AreEqual(8.5f, x, Eps);
            Assert.AreEqual(8.5f, z, Eps);
        }

        [Test]
        public void MoveEntersWater()
        {
            var w = MakeWorld(3, 7);
            float x = 8.5f, y = 10.5f, z = 8.5f;
            FlyCameraCollision.Move(w, ref x, ref y, ref z, 0, -5, 0, R);
            Assert.AreEqual(5.5f, y, Eps);
        }

        [Test]
        public void MoveSlidesAlongWall()
        {
            var w = MakeWall();
            float x = 9f, y = 8.5f, z = 5.5f;
            FlyCameraCollision.Move(w, ref x, ref y, ref z, 2, 0, 3, R);
            Assert.AreEqual(10 - R - FlyCameraCollision.Skin, x, Eps);
            Assert.AreEqual(8.5f, z, Eps);
            Assert.AreEqual(8.5f, y, Eps);
        }

        [Test]
        public void FastMoveDoesNotTunnelThinWall()
        {
            var w = MakeWall();
            float x = 8.5f, y = 8.5f, z = 8.5f;
            FlyCameraCollision.Move(w, ref x, ref y, ref z, 30, 0, 0, R);
            Assert.AreEqual(10 - R - FlyCameraCollision.Skin, x, Eps);
        }

        [Test]
        public void StuckInsideSolidMovesFreely()
        {
            var w = MakeWorld(3, 3);
            float x = 8.5f, y = 2.5f, z = 8.5f;
            FlyCameraCollision.Move(w, ref x, ref y, ref z, 0, 3, 0, R);
            Assert.AreEqual(5.5f, y, Eps);
        }

        [Test]
        public void IsUnderwaterUsesSurfaceDrop()
        {
            var w = MakeWorld(3, 7);
            const float drop = 0.1f;
            Assert.IsTrue(FlyCameraCollision.IsUnderwater(w, 8.5f, 7.5f, 8.5f, drop));
            Assert.IsFalse(FlyCameraCollision.IsUnderwater(w, 8.5f, 7.95f, 8.5f, drop));
            Assert.IsTrue(FlyCameraCollision.IsUnderwater(w, 8.5f, 5.5f, 8.5f, drop));
            Assert.IsFalse(FlyCameraCollision.IsUnderwater(w, 8.5f, 8.5f, 8.5f, drop));
            Assert.IsFalse(FlyCameraCollision.IsUnderwater(w, 8.5f, 2.5f, 8.5f, drop));
            w.Set(8, 8, 8, Cell.Make(BlockMaterial.Stone));
            Assert.IsTrue(FlyCameraCollision.IsUnderwater(w, 8.5f, 7.95f, 8.5f, drop));
        }
    }
}
