using System;
using Game.Core.DayCycle;
using NUnit.Framework;

namespace Game.Client.Tests
{
    public class SkyRotationTests
    {
        const int H = 3_600_000;
        const int Day = TimeConfig.DayMs;

        [Test]
        public void EaseEnds()
        {
            Assert.AreEqual(0.0, SkyRotation.Ease(0));
            Assert.AreEqual(1.0, SkyRotation.Ease(1));
            Assert.AreEqual(0.5, SkyRotation.Ease(0.5));
            Assert.AreEqual(0.0, SkyRotation.Ease(-1));
            Assert.AreEqual(1.0, SkyRotation.Ease(2));
        }

        [Test]
        public void RotatesForwardOverDuration()
        {
            var r = new SkyRotation(3.0);
            Assert.IsFalse(r.IsRotating(0));
            r.Start(18 * H, 10.0);
            Assert.AreEqual(12 * H, r.DisplayPhaseMs(6 * H, 10.0));
            Assert.AreEqual(21 * H, r.DisplayPhaseMs(6 * H, 11.5));
            Assert.IsTrue(r.IsRotating(12.999));
            Assert.IsFalse(r.IsRotating(13.0));
            Assert.AreEqual(6 * H, r.DisplayPhaseMs(6 * H, 13.0));

            int prev = r.DisplayPhaseMs(6 * H, 10.0);
            long total = 0;
            for (int i = 1; i <= 300; i++)
            {
                int cur = r.DisplayPhaseMs(6 * H, 10.0 + i * 0.01);
                int step = TimeOfDay.ForwardArc(prev, cur);
                Assert.Less(step, H, "step " + i);
                total += step;
                prev = cur;
            }
            Assert.AreEqual(18L * H, total);
        }

        [Test]
        public void ZeroArcAndBadArgs()
        {
            var r = new SkyRotation(3.0);
            r.Start(0, 5.0);
            Assert.IsFalse(r.IsRotating(5.0));
            Assert.AreEqual(777, r.DisplayPhaseMs(777, 5.0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new SkyRotation(0));
            Assert.Throws<ArgumentOutOfRangeException>(() => r.Start(-1, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => r.Start(Day, 0));
        }
    }
}
