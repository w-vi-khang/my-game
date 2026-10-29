using System;
using NUnit.Framework;

namespace Game.Core.Tests
{
    public class DropScatterTests
    {
        [Test]
        public void Deterministic()
        {
            DropScatter.Offset(7, 0.15f, out float dx, out float dz, out float yaw);
            DropScatter.Offset(7, 0.15f, out float dx2, out float dz2, out float yaw2);
            Assert.AreEqual(dx, dx2);
            Assert.AreEqual(dz, dz2);
            Assert.AreEqual(yaw, yaw2);
        }

        [Test]
        public void WithinRadiusAndSpread()
        {
            int far = 0;
            double sx = 0, sz = 0;
            float minYaw = 1000f, maxYaw = -1f;
            for (int id = 1; id <= 1000; id++)
            {
                DropScatter.Offset(id, 0.15f, out float dx, out float dz, out float yaw);
                float m = (float)Math.Sqrt(dx * dx + dz * dz);
                Assert.LessOrEqual(m, 0.15f + 1e-6f);
                Assert.GreaterOrEqual(yaw, 0f);
                Assert.Less(yaw, 360f);
                if (m > 0.075f) far++;
                sx += dx; sz += dz;
                minYaw = Math.Min(minYaw, yaw); maxYaw = Math.Max(maxYaw, yaw);
            }
            Assert.That(far, Is.InRange(650, 850));
            Assert.Less(Math.Abs(sx / 1000), 0.01);
            Assert.Less(Math.Abs(sz / 1000), 0.01);
            Assert.Greater(maxYaw - minYaw, 300f);
        }

        [Test]
        public void ZeroRadius()
        {
            DropScatter.Offset(5, 0f, out float dx, out float dz, out _);
            Assert.AreEqual(0f, dx);
            Assert.AreEqual(0f, dz);
        }

        [Test]
        public void NegativeRadiusThrows()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => DropScatter.Offset(1, -0.1f, out _, out _, out _));
        }
    }
}
