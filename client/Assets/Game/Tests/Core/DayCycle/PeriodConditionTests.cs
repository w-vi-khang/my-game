using System;
using Game.Core.DayCycle;
using NUnit.Framework;
using UnityEngine;

namespace Game.Core.Tests
{
    public class PeriodConditionTests
    {
        const int H = 3_600_000;
        long now;

        [Test]
        public void SinglePeriod()
        {
            var c = new PeriodCondition("Night");
            Assert.IsTrue(c.IsMet(DayPeriod.Night));
            Assert.IsFalse(c.IsMet(DayPeriod.Dawn));
            Assert.IsFalse(c.IsMet(DayPeriod.Day));
            Assert.IsFalse(c.IsMet(DayPeriod.Dusk));
        }

        [Test]
        public void MultiplePeriods()
        {
            var c = new PeriodCondition("Dawn", "Dusk");
            Assert.IsTrue(c.IsMet(DayPeriod.Dawn));
            Assert.IsTrue(c.IsMet(DayPeriod.Dusk));
            Assert.IsFalse(c.IsMet(DayPeriod.Day));
            Assert.IsFalse(c.IsMet(DayPeriod.Night));
        }

        [Test]
        public void ValidateRejectsBadValues()
        {
            Assert.Throws<ArgumentException>(new PeriodCondition { periods = null }.Validate, "null");
            Assert.Throws<ArgumentException>(new PeriodCondition(new string[0]).Validate, "empty");
            Assert.Throws<ArgumentException>(new PeriodCondition("night").Validate, "lowercase");
            Assert.Throws<ArgumentException>(new PeriodCondition("Noon").Validate, "Noon");
            Assert.Throws<ArgumentException>(new PeriodCondition("1").Validate, "number");
            Assert.Throws<ArgumentException>(new PeriodCondition("Day", "Day").Validate, "duplicate");
            var c = JsonUtility.FromJson<PeriodCondition>("{\"periods\":[\"Dawn\",\"Night\"]}");
            Assert.DoesNotThrow(c.Validate);
            Assert.IsTrue(c.IsMet(DayPeriod.Night));
        }

        [Test]
        public void TimeOfDaySelectChangesPeriod()
        {
            var tod = new TimeOfDay(new TimeConfig(), () => now);
            now = 20 * H + 24 * 60_000;
            Assert.IsTrue(tod.IsMet(new PeriodCondition("Night")));
            Assert.IsFalse(tod.IsMet(new PeriodCondition("Day")));
            tod.SelectLandmark(TimeLandmark.Noon);
            Assert.IsTrue(tod.IsMet(new PeriodCondition("Day")));
            Assert.IsFalse(tod.IsMet(new PeriodCondition("Night")));
        }
    }
}
