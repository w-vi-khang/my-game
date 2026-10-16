using Game.Core.View;
using NUnit.Framework;

namespace Game.Core.Tests
{
    public class SimClockTests
    {
        [Test]
        public void Fps60TicksEverySixthFrame()
        {
            var c = new SimClock(10);
            int total = 0;
            for (int f = 1; f <= 60; f++)
            {
                int n = c.Advance(1.0 / 60);
                Assert.AreEqual(f % 6 == 0 ? 1 : 0, n, "khung " + f);
                total += n;
            }
            Assert.AreEqual(10, total);
            Assert.AreEqual(20, c.AccumulatorMicros);
        }

        [Test]
        public void Fps30TicksOnFrames4_7_10()
        {
            var c = new SimClock(10);
            int total = 0;
            for (int f = 1; f <= 31; f++)
            {
                int n = c.Advance(1.0 / 30);
                bool expectTick = f >= 4 && (f - 4) % 3 == 0;
                Assert.AreEqual(expectTick ? 1 : 0, n, "khung " + f);
                total += n;
                if (f == 30) Assert.AreEqual(9, total);
            }
            Assert.AreEqual(10, total);
            Assert.AreEqual(33323, c.AccumulatorMicros);
        }

        [Test]
        public void LongFrameIsCapped()
        {
            var c = new SimClock(10);
            Assert.AreEqual(5, c.Advance(1.0));
            Assert.AreEqual(0, c.AccumulatorMicros);
            Assert.AreEqual(0.0, c.Alpha);
            Assert.AreEqual(2, c.Advance(0.25));
            Assert.AreEqual(50000, c.AccumulatorMicros);
            Assert.AreEqual(0.5, c.Alpha, 1e-9);
        }

        [Test]
        public void NonPositiveOrNaNIgnored()
        {
            var c = new SimClock(10);
            Assert.AreEqual(0, c.Advance(0));
            Assert.AreEqual(0, c.Advance(-1));
            Assert.AreEqual(0, c.Advance(double.NaN));
            Assert.AreEqual(0, c.AccumulatorMicros);
        }

        [Test]
        public void ConstructorRejectsBadValues()
        {
            Assert.Throws<System.ArgumentException>(() => new SimClock(0));
            Assert.Throws<System.ArgumentException>(() => new SimClock(3));
            Assert.Throws<System.ArgumentException>(() => new SimClock(10, 0));
        }
    }
}
