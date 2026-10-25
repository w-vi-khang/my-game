using System;
using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace Game.Core.Tests
{
    public class WaterConfigTests
    {
        static string SourcePath => Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "design", "data", "water_m1.json"));

        [Test]
        public void ConfigParsesAndValidates()
        {
            Assert.IsTrue(File.Exists(SourcePath), SourcePath);
            var cfg = JsonUtility.FromJson<WaterConfig>(File.ReadAllText(SourcePath));
            Assert.DoesNotThrow(cfg.Validate);
            Assert.AreEqual(7, cfg.flowMaxDistance);
            Assert.AreEqual(8, cfg.stepsPerSecond);
        }

        [Test]
        public void ConfigFileMatchesDefaults()
        {
            var fromFile = JsonUtility.FromJson<WaterConfig>(File.ReadAllText(SourcePath));
            Assert.AreEqual(JsonUtility.ToJson(new WaterConfig()), JsonUtility.ToJson(fromFile));
        }

        [Test]
        public void ValidateRejectsBadValues()
        {
            Assert.DoesNotThrow(new WaterConfig().Validate);
            var c = new WaterConfig(); c.flowMaxDistance = 0; Assert.Throws<ArgumentException>(c.Validate, "N=0");
            c = new WaterConfig(); c.flowMaxDistance = 8; Assert.Throws<ArgumentException>(c.Validate, "N=8");
            c = new WaterConfig(); c.stepsPerSecond = 0; Assert.Throws<ArgumentException>(c.Validate, "steps=0");
            c = new WaterConfig(); c.stepsPerSecond = 61; Assert.Throws<ArgumentException>(c.Validate, "steps=61");
        }
    }
}
