using System;
using System.IO;
using Game.Core.Sim;
using NUnit.Framework;
using UnityEngine;

namespace Game.Core.Tests
{
    public class SimConfigTests
    {
        // Application.dataPath = <repo>/client/Assets
        static string SourcePath => Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "design", "data", "sim_m1.json"));

        [Test]
        public void ConfigParsesAndValidates()
        {
            Assert.IsTrue(File.Exists(SourcePath), SourcePath);
            var cfg = JsonUtility.FromJson<SimConfig>(File.ReadAllText(SourcePath));
            Assert.DoesNotThrow(cfg.Validate);
            Assert.AreEqual(60, cfg.BaseTicks(WorkAbility.Till));
            Assert.AreEqual(20, cfg.BaseTicks(WorkAbility.Haul));
            Assert.AreEqual(200, cfg.SpeedPerTick(cfg.FindSpecies("L3")));
        }

        [Test]
        public void ConfigMatchesCodeDefaults()
        {
            var fromFile = JsonUtility.FromJson<SimConfig>(File.ReadAllText(SourcePath));
            Assert.AreEqual(JsonUtility.ToJson(new SimConfig()), JsonUtility.ToJson(fromFile));
        }

        [Test]
        public void ValidateRejectsBadValues()
        {
            var c = new SimConfig();
            c.stageTimes = new[] { c.stageTimes[0], c.stageTimes[1], c.stageTimes[2], c.stageTimes[3] };
            Assert.Throws<ArgumentException>(c.Validate, "thiếu Haul");

            c = new SimConfig();
            c.stageTimes[0].baseTimeMs = 6050;
            Assert.Throws<ArgumentException>(c.Validate, "baseTimeMs lẻ nhịp");

            c = new SimConfig();
            c.species[1].id = "L1";
            Assert.Throws<ArgumentException>(c.Validate, "trùng id loài");

            c = new SimConfig();
            c.species[0].ability2 = c.species[0].ability1;
            Assert.Throws<ArgumentException>(c.Validate, "hai năng lực giống nhau");

            c = new SimConfig();
            c.species[0].ability1 = "Fly";
            Assert.Throws<ArgumentException>(c.Validate, "năng lực Fly");

            c = new SimConfig();
            c.species[0].ability1 = "3";
            Assert.Throws<ArgumentException>(c.Validate, "năng lực dạng số");

            c = new SimConfig();
            c.species[0].mastery1 = 0;
            Assert.Throws<ArgumentException>(c.Validate, "mastery1 = 0");

            c = new SimConfig();
            c.species[0].moveSpeedMilliCellsPerSecond = 5;
            Assert.Throws<ArgumentException>(c.Validate, "tốc độ quá nhỏ");

            c = new SimConfig();
            c.species[0].sizeX = 0;
            Assert.Throws<ArgumentException>(c.Validate, "sizeX = 0");

            c = new SimConfig();
            c.storage.startSeeds = 51;
            Assert.Throws<ArgumentException>(c.Validate, "startSeeds > capacity");

            c = new SimConfig();
            c.farm.plotQueueCapacity = 0;
            Assert.Throws<ArgumentException>(c.Validate, "hàng đợi 0");

            c = new SimConfig();
            c.house.capacity = 0;
            Assert.Throws<ArgumentException>(c.Validate, "nhà sức chứa 0");

            c = new SimConfig();
            c.creature.stuckTimeMs = 30050;
            Assert.Throws<ArgumentException>(c.Validate, "stuckTimeMs lẻ nhịp");
        }
    }
}
