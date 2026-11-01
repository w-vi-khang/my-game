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
            Assert.AreEqual(5, cfg.configVersion);
            Assert.AreEqual(5, cfg.maxLevel);
            Assert.AreEqual(8, cfg.stepsPerSecond);
            Assert.AreEqual(2, cfg.wallBoostMinSolid);
            Assert.AreEqual(16, cfg.basinFillRadius);
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
            c = new WaterConfig(); c.basinFillRadius = -1; Assert.Throws<ArgumentException>(c.Validate, "basin=-1");
            c = new WaterConfig(); c.basinFillRadius = 65; Assert.Throws<ArgumentException>(c.Validate, "basin=65");
            c = new WaterConfig(); c.filletRadius = 0.04f; Assert.Throws<ArgumentException>(c.Validate, "fillet=0.04");
            c = new WaterConfig(); c.filletRadius = 0.46f; Assert.Throws<ArgumentException>(c.Validate, "fillet=0.46");
            c = new WaterConfig(); c.filletSegments = 0; Assert.Throws<ArgumentException>(c.Validate, "seg=0");
            c = new WaterConfig(); c.filletSegments = 9; Assert.Throws<ArgumentException>(c.Validate, "seg=9");
        }

        [Test]
        public void LookParsesAndValidates()
        {
            var cfg = JsonUtility.FromJson<WaterConfig>(File.ReadAllText(SourcePath));
            Assert.DoesNotThrow(cfg.Validate);
            Assert.AreEqual(new[] { 0.45f, 0.88f, 0.85f }, cfg.look.shallowColor);
            Assert.AreEqual(0.35f, cfg.look.shoreSoftWidth, 1e-6f);
            Assert.AreEqual(2f, cfg.look.depthFull, 1e-6f);
        }

        [Test]
        public void LookRejectsOutOfRange()
        {
            var c = new WaterConfig(); c.look.shoreSoftWidth = 0f; Assert.Throws<ArgumentException>(c.Validate, "shore=0");
            c = new WaterConfig(); c.look.deepColor = new[] { 0.1f, 0.2f }; Assert.Throws<ArgumentException>(c.Validate, "deepColor 2 elements");
            c = new WaterConfig(); c.look.deepAlpha = 1.5f; Assert.Throws<ArgumentException>(c.Validate, "deepAlpha=1.5");
        }
    }
}
