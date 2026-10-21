using System;
using System.IO;
using Game.Core.DayCycle;
using NUnit.Framework;
using UnityEngine;

namespace Game.Core.Tests
{
    public class TimeConfigTests
    {
        static string SourcePath => Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "design", "data", "time_m1.json"));

        [Test]
        public void ConfigParsesAndValidates()
        {
            Assert.IsTrue(File.Exists(SourcePath), SourcePath);
            var cfg = JsonUtility.FromJson<TimeConfig>(File.ReadAllText(SourcePath));
            Assert.DoesNotThrow(cfg.Validate);
            Assert.AreEqual(21_600_000, cfg.LandmarkMs(TimeLandmark.Dawn));
            Assert.AreEqual(64_800_000, cfg.LandmarkMs(TimeLandmark.Dusk));
            Assert.AreEqual(DayPeriod.Dawn, cfg.PeriodAt(17_280_000));
            Assert.AreEqual(3000, cfg.rotateDurationMs);
        }

        [Test]
        public void ConfigMatchesCodeDefaults()
        {
            var fromFile = JsonUtility.FromJson<TimeConfig>(File.ReadAllText(SourcePath));
            Assert.AreEqual(JsonUtility.ToJson(new TimeConfig()), JsonUtility.ToJson(fromFile));
        }

        [Test]
        public void ValidateRejectsBadValues()
        {
            Assert.DoesNotThrow(new TimeConfig().Validate);
            var c = new TimeConfig(); c.dawnPermille = 0; Assert.Throws<ArgumentException>(c.Validate, "dawn=0");
            c = new TimeConfig(); c.duskPermille = 1000; Assert.Throws<ArgumentException>(c.Validate, "dusk=1000");
            c = new TimeConfig(); c.midnightPermille = -1; Assert.Throws<ArgumentException>(c.Validate, "midnight=-1");
            c = new TimeConfig(); c.noonPermille = 250; Assert.Throws<ArgumentException>(c.Validate, "noon=250");
            c = new TimeConfig(); c.dayStartPermille = 200; Assert.Throws<ArgumentException>(c.Validate, "dayStart=200");
            c = new TimeConfig(); c.nightStartPermille = 1000; Assert.Throws<ArgumentException>(c.Validate, "nightStart=1000");
            c = new TimeConfig(); c.dawnStartPermille = -1; Assert.Throws<ArgumentException>(c.Validate, "dawnStart=-1");
            c = new TimeConfig(); c.rotateDurationMs = 0; Assert.Throws<ArgumentException>(c.Validate, "rotate=0");
        }
    }
}
