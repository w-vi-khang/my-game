using System;
using System.Collections.Generic;
using Game.Core.DayCycle;
using Game.Core.Sim;
using NUnit.Framework;

namespace Game.Core.Tests
{
    public class TimeOfDayTests
    {
        const int H = 3_600_000;
        const int Day = TimeConfig.DayMs;
        long now;

        TimeOfDay Make() => new TimeOfDay(new TimeConfig(), () => now);

        [Test]
        public void SameClockSameOffsetSamePhase()
        {
            now = 5 * H + 123;
            var a = Make(); var b = Make();
            Assert.AreEqual(a.PhaseMs, b.PhaseMs);
            Assert.AreEqual(5 * H + 123, a.PhaseMs);
            a.SelectLandmark(TimeLandmark.Noon); b.SelectLandmark(TimeLandmark.Noon);
            Assert.AreEqual(a.OffsetMs, b.OffsetMs);
            Assert.AreEqual(a.PhaseMs, b.PhaseMs);
            now += 7 * H;
            Assert.AreEqual(a.PhaseMs, b.PhaseMs);
            now = 20000L * Day + 1000;
            Assert.AreEqual(1000, Make().PhaseMs);
            now = -1000;
            Assert.AreEqual(Day - 1000, Make().PhaseMs);
        }

        [Test]
        public void SelectSetsPhaseThenKeepsFlowing()
        {
            now = 3 * H;
            var t = Make();
            Assert.AreEqual(9 * H, t.SelectLandmark(TimeLandmark.Noon));
            Assert.AreEqual(12 * H, t.PhaseMs);
            now += 5000;
            Assert.AreEqual(12 * H + 5000, t.PhaseMs);
            now += 12 * H;
            Assert.AreEqual(5000, t.PhaseMs);
            Assert.AreEqual(DayPeriod.Night, t.Period);
        }

        [Test]
        public void SameWindowDoesNothing()
        {
            now = 0;
            var t = Make();
            t.SelectLandmark(TimeLandmark.Noon);
            int off = t.OffsetMs;
            now += 5000;
            Assert.AreEqual(0, t.SelectLandmark(TimeLandmark.Noon));
            Assert.AreEqual(off, t.OffsetMs);
            Assert.AreEqual(12 * H + 5000, t.PhaseMs);
            now = 6 * H - 1;
            Assert.AreEqual(18 * H - 1, t.PhaseMs);
            Assert.AreEqual(0, t.SelectLandmark(TimeLandmark.Noon));
            now = 6 * H;
            Assert.AreEqual(18 * H, t.PhaseMs);
            Assert.AreEqual(18 * H, t.SelectLandmark(TimeLandmark.Noon));
            Assert.AreEqual(12 * H, t.PhaseMs);
        }

        [Test]
        public void ArcAlwaysForward4x4()
        {
            var cfg = new TimeConfig();
            for (int s = 0; s < 4; s++)
                for (int t = 0; t < 4; t++)
                {
                    now = cfg.LandmarkMs((TimeLandmark)s);
                    var tod = new TimeOfDay(cfg, () => now);
                    int arc = tod.SelectLandmark((TimeLandmark)t);
                    Assert.AreEqual((t - s + 4) % 4 * 6 * H, arc, s + "→" + t);
                    Assert.AreEqual(cfg.LandmarkMs((TimeLandmark)t), tod.PhaseMs, s + "→" + t);
                    if (s == t) Assert.AreEqual(0, tod.OffsetMs);
                }
            int Arc(TimeLandmark a, TimeLandmark b)
            {
                now = cfg.LandmarkMs(a);
                return new TimeOfDay(cfg, () => now).SelectLandmark(b);
            }
            Assert.AreEqual(18 * H, Arc(TimeLandmark.Noon, TimeLandmark.Dawn));
            Assert.AreEqual(6 * H, Arc(TimeLandmark.Dawn, TimeLandmark.Noon));
            Assert.AreEqual(18 * H, Arc(TimeLandmark.Midnight, TimeLandmark.Dusk));
            Assert.AreEqual(6 * H, Arc(TimeLandmark.Dusk, TimeLandmark.Midnight));
        }

        [Test]
        public void ForwardArcValues()
        {
            Assert.AreEqual(18 * H, TimeOfDay.ForwardArc(12 * H, 6 * H));
            Assert.AreEqual(6 * H, TimeOfDay.ForwardArc(6 * H, 12 * H));
            Assert.AreEqual(0, TimeOfDay.ForwardArc(777, 777));
            Assert.AreEqual(1, TimeOfDay.ForwardArc(Day - 1, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => TimeOfDay.ForwardArc(-1, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => TimeOfDay.ForwardArc(0, Day));
        }

        [Test]
        public void PeriodBoundsIncludingMidnight()
        {
            var cfg = new TimeConfig();
            Assert.AreEqual(DayPeriod.Night, cfg.PeriodAt(0));
            Assert.AreEqual(DayPeriod.Night, cfg.PeriodAt(17_279_999));
            Assert.AreEqual(DayPeriod.Dawn, cfg.PeriodAt(17_280_000));
            Assert.AreEqual(DayPeriod.Dawn, cfg.PeriodAt(25_919_999));
            Assert.AreEqual(DayPeriod.Day, cfg.PeriodAt(25_920_000));
            Assert.AreEqual(DayPeriod.Day, cfg.PeriodAt(60_479_999));
            Assert.AreEqual(DayPeriod.Dusk, cfg.PeriodAt(60_480_000));
            Assert.AreEqual(DayPeriod.Dusk, cfg.PeriodAt(69_119_999));
            Assert.AreEqual(DayPeriod.Night, cfg.PeriodAt(69_120_000));
            Assert.AreEqual(DayPeriod.Night, cfg.PeriodAt(Day - 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => cfg.PeriodAt(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => cfg.PeriodAt(Day));
            var t = Make();
            now = 69_120_000; Assert.AreEqual(DayPeriod.Night, t.Period);
            now = Day; Assert.AreEqual(DayPeriod.Night, t.Period);
            now = Day + 17_280_000; Assert.AreEqual(DayPeriod.Dawn, t.Period);
        }

        [Test]
        public void WindowBounds()
        {
            var cfg = new TimeConfig();
            Assert.AreEqual(TimeLandmark.Midnight, cfg.WindowAt(0));
            Assert.AreEqual(TimeLandmark.Midnight, cfg.WindowAt(6 * H - 1));
            Assert.AreEqual(TimeLandmark.Dawn, cfg.WindowAt(6 * H));
            Assert.AreEqual(TimeLandmark.Dawn, cfg.WindowAt(12 * H - 1));
            Assert.AreEqual(TimeLandmark.Noon, cfg.WindowAt(12 * H));
            Assert.AreEqual(TimeLandmark.Noon, cfg.WindowAt(18 * H - 1));
            Assert.AreEqual(TimeLandmark.Dusk, cfg.WindowAt(18 * H));
            Assert.AreEqual(TimeLandmark.Dusk, cfg.WindowAt(Day - 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => cfg.WindowAt(Day));
        }

        [Test]
        public void ClockJumpFollowsClockKeepsOffset()
        {
            now = 0;
            var t = Make();
            t.SelectLandmark(TimeLandmark.Noon);
            Assert.AreEqual(12 * H, t.OffsetMs);
            now = 5 * H; Assert.AreEqual(17 * H, t.PhaseMs);
            now = -2 * H; Assert.AreEqual(10 * H, t.PhaseMs);
            Assert.AreEqual(12 * H, t.OffsetMs);
        }

        [Test]
        public void ConstructorRejectsBadArgs()
        {
            Assert.Throws<ArgumentNullException>(() => new TimeOfDay(null, () => 0));
            Assert.Throws<ArgumentNullException>(() => new TimeOfDay(new TimeConfig(), null));
            var c = new TimeConfig { dawnPermille = 0 };
            Assert.Throws<ArgumentException>(() => new TimeOfDay(c, () => 0));
        }

        [Test]
        public void SelectingLandmarkDoesNotChangeSimHash()
        {
            var a = SimDeterminismTests.BuildFarm(20261014);
            var b = SimDeterminismTests.BuildFarm(20261014);
            now = 3 * H;
            var tod = Make();
            var ha = new List<ulong>(); var hb = new List<ulong>();
            int changed = 0;
            for (int i = 0; i < 600; i++)
            {
                if (i % 50 == 0)
                {
                    now += 5 * H;
                    if (tod.SelectLandmark((TimeLandmark)(i / 50 % 4)) != 0) changed++;
                }
                a.Tick(); b.Tick();
                ha.Add(SimHash.Compute(a.State));
                hb.Add(SimHash.Compute(b.State));
            }
            CollectionAssert.AreEqual(ha, hb);
            Assert.Greater(changed, 0);
        }
    }
}
