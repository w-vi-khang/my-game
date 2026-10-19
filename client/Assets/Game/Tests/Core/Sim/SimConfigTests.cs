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
            Assert.AreEqual(1000, cfg.WorkSpeedPermille(100));
            Assert.AreEqual(10, cfg.BlockWaitTicks);
            Assert.AreEqual(50, cfg.PhaseTicks);
            Assert.AreEqual(4, cfg.creature.homeRadius);
            Assert.AreEqual(50, cfg.WanderMinTicks);
            Assert.AreEqual(100, cfg.WanderMaxTicks);
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

            c = new SimConfig();
            c.creature.blockWaitMs = 1050;
            Assert.Throws<ArgumentException>(c.Validate, "blockWaitMs lẻ nhịp");

            c = new SimConfig();
            c.creature.phaseAfterMs = 1000;
            Assert.Throws<ArgumentException>(c.Validate, "phaseAfterMs <= blockWaitMs");

            c = new SimConfig();
            c.creature.homeRadius = 0;
            Assert.Throws<ArgumentException>(c.Validate, "homeRadius 0");

            c = new SimConfig();
            c.creature.homeRadius = 65;
            Assert.Throws<ArgumentException>(c.Validate, "homeRadius 65");

            c = new SimConfig();
            c.creature.wanderMinMs = 5050;
            Assert.Throws<ArgumentException>(c.Validate, "wanderMinMs lẻ nhịp");

            c = new SimConfig();
            c.creature.wanderMaxMs = 4000;
            Assert.Throws<ArgumentException>(c.Validate, "wanderMaxMs < wanderMinMs");

            c = new SimConfig();
            c.masteryLevels = new MasteryLevel[0];
            Assert.Throws<ArgumentException>(c.Validate, "bảng mức rỗng");

            c = new SimConfig();
            c.masteryLevels[0].minMastery = 2;
            Assert.Throws<ArgumentException>(c.Validate, "ngưỡng đầu > 1");

            c = new SimConfig();
            c.masteryLevels[2].minMastery = 50;
            Assert.Throws<ArgumentException>(c.Validate, "ngưỡng không tăng");

            c = new SimConfig();
            c.masteryLevels[0].speedPermille = 0;
            Assert.Throws<ArgumentException>(c.Validate, "hệ số 0");

            c = new SimConfig();
            c.masteryLevels[4].speedPermille = 10001;
            Assert.Throws<ArgumentException>(c.Validate, "hệ số > 10000");

            c = new SimConfig();
            c.masteryLevels[3].speedPermille = 900;
            Assert.Throws<ArgumentException>(c.Validate, "hệ số giảm");
        }

        [TestCase(1, 500)]
        [TestCase(49, 500)]
        [TestCase(50, 750)]
        [TestCase(99, 750)]
        [TestCase(100, 1000)]
        [TestCase(199, 1000)]
        [TestCase(200, 1250)]
        [TestCase(399, 1250)]
        [TestCase(400, 1500)]
        [TestCase(100000, 1500)]
        public void WorkSpeedFollowsTable(int mastery, int expected)
        {
            Assert.AreEqual(expected, new SimConfig().WorkSpeedPermille(mastery));
        }
    }
}
