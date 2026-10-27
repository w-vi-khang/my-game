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
            Assert.AreEqual(3, cfg.configVersion);
            Assert.AreEqual(5, cfg.maxLevel);
            Assert.AreEqual(8, cfg.stepsPerSecond);
            Assert.AreEqual(2, cfg.wallBoostMinSolid);
            Assert.AreEqual(0.3f, cfg.filletRadius, 1e-6f);
            Assert.AreEqual(3, cfg.filletSegments);
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
            var c = new WaterConfig(); c.maxLevel = 0; Assert.Throws<ArgumentException>(c.Validate, "max=0");
            c = new WaterConfig(); c.maxLevel = 8; Assert.Throws<ArgumentException>(c.Validate, "max=8");
            c = new WaterConfig(); c.stepsPerSecond = 0; Assert.Throws<ArgumentException>(c.Validate, "steps=0");
            c = new WaterConfig(); c.stepsPerSecond = 61; Assert.Throws<ArgumentException>(c.Validate, "steps=61");
            c = new WaterConfig(); c.wallBoostMinSolid = 0; Assert.Throws<ArgumentException>(c.Validate, "wall=0");
            c = new WaterConfig(); c.wallBoostMinSolid = 5; Assert.Throws<ArgumentException>(c.Validate, "wall=5");
            c = new WaterConfig(); c.filletRadius = 0.04f; Assert.Throws<ArgumentException>(c.Validate, "fillet=0.04");
            c = new WaterConfig(); c.filletRadius = 0.46f; Assert.Throws<ArgumentException>(c.Validate, "fillet=0.46");
            c = new WaterConfig(); c.filletSegments = 0; Assert.Throws<ArgumentException>(c.Validate, "seg=0");
            c = new WaterConfig(); c.filletSegments = 9; Assert.Throws<ArgumentException>(c.Validate, "seg=9");
        }
    }
}
