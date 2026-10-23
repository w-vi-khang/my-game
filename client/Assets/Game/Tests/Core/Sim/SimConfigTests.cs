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
            Assert.AreEqual(2, cfg.crops.Length);
            Assert.AreEqual(1200, cfg.GrowTicks(0));
            Assert.AreEqual(1800, cfg.GrowTicks(1));
            Assert.AreEqual(5, cfg.configVersion);
            Assert.AreEqual(0, cfg.player.starterCrop);
            Assert.AreEqual(4, cfg.player.starterSeeds);
            Assert.AreEqual(10f, cfg.player.targetReach);
            Assert.AreEqual(2f, cfg.player.actReach);
            Assert.AreEqual(2f, cfg.player.digReach);
            Assert.AreEqual(2f, cfg.player.collectReach);
            Assert.AreEqual(100, cfg.player.manaMax);
            Assert.AreEqual(5, cfg.player.manaDig);
            Assert.AreEqual(3, cfg.player.manaPlace);
            Assert.AreEqual(15, cfg.player.manaRain);
            Assert.AreEqual(2, cfg.player.manaRegenPerCell);
            Assert.AreEqual(100, cfg.player.hpMax);
            Assert.AreEqual(4, cfg.player.fallSafeCells);
            Assert.AreEqual(10, cfg.player.fallDamagePerCell);
            Assert.AreEqual(5, cfg.player.drownDamagePerSecond);
            Assert.AreEqual(1, cfg.player.hpRegenPerSecond);
            Assert.AreEqual(5000, cfg.player.hpRegenDelayMs);
            Assert.AreEqual(10, cfg.player.bagSlots);
            Assert.AreEqual(2, cfg.player.hotbarSlots);
            Assert.AreEqual(10, cfg.player.bagPageSize);
            Assert.AreEqual(10, cfg.player.hotbarPageSize);
            Assert.AreEqual(999, cfg.player.stackMax);
            Assert.AreEqual(4f, cfg.player.dashCells);
            Assert.AreEqual(150, cfg.player.dashMs);
            Assert.AreEqual(3000, cfg.player.dashCooldownMs);
            Assert.AreEqual(300, cfg.player.stuckMs);
            Assert.AreEqual(500, cfg.player.clickMarkMs);
            Assert.AreEqual(1000, cfg.player.reachRingFadeMs);
            Assert.AreEqual(500, cfg.player.collectShowMs);
            Assert.AreEqual(300, cfg.player.collectFlyMs);
            Assert.AreEqual(10f, cfg.player.cameraPitch);
            Assert.AreEqual(9f, cfg.player.cameraDistance);
            Assert.AreEqual(15f, cfg.player.firstPersonPitch);
            Assert.AreEqual(2f, cfg.player.observationHeight);
            Assert.AreEqual(6f, cfg.PlayerTillSeconds);
            Assert.AreEqual(10f, cfg.RainCooldownSeconds);
        }

        [Test]
        public void ConfigMatchesCodeDefaults()
        {
            var fromFile = JsonUtility.FromJson<SimConfig>(File.ReadAllText(SourcePath));
            Assert.AreEqual(JsonUtility.ToJson(new SimConfig()), JsonUtility.ToJson(fromFile));
        }

        [Test]
        public void ValidateRejectsBadPlayerRules()
        {
            void Bad(System.Action<SimConfig> mut, string why)
            {
                var c = new SimConfig();
                mut(c);
                Assert.Throws<ArgumentException>(c.Validate, why);
            }
            Bad(c => c.player.starterCrop = 2, "starterCrop 2");
            Bad(c => c.player.starterCrop = -1, "starterCrop -1");
            Bad(c => c.player.starterSeeds = -1, "starterSeeds -1");
            Bad(c => c.player.starterSeeds = 100, "starterSeeds 100");
            Bad(c => c.player.targetReach = 0.5f, "targetReach 0.5");
            Bad(c => c.player.targetReach = 33, "targetReach 33");
            Bad(c => c.player.actReach = 11, "actReach 11");
            Bad(c => c.player.manaDig = 101, "manaDig 101");
            Bad(c => c.player.bagSlots = 0, "bagSlots 0");
            Bad(c => c.player.stackMax = 0, "stackMax 0");
            Bad(c => c.player.hpRegenDelayMs = -1, "hpRegenDelayMs -1");
            Bad(c => c.player.dashCells = 0.4f, "dashCells 0.4");
            Bad(c => c.player.dashMs = 5, "dashMs 5");
            Bad(c => c.player.dashCooldownMs = 50, "dashCooldownMs 50");
            Bad(c => c.player.stuckMs = 10, "stuckMs 10");
            Bad(c => c.player.clickMarkMs = 6000, "clickMarkMs 6000");
            Bad(c => c.player.reachRingFadeMs = 6000, "reachRingFadeMs 6000");
            Bad(c => c.player.collectShowMs = 6000, "collectShowMs 6000");
            Bad(c => c.player.collectFlyMs = 6000, "collectFlyMs 6000");
            Bad(c => c.player.cameraPitch = 90f, "cameraPitch 90");
            Bad(c => c.player.cameraDistance = 2f, "cameraDistance 2");
            Bad(c => c.player.firstPersonPitch = -40f, "firstPersonPitch -40");
            Bad(c => c.player.observationHeight = 0.5f, "observationHeight 0.5");
            Bad(c => c.player.observationHeight = 11f, "observationHeight 11");
            Bad(c => c.player = null, "player null");
        }

        [Test]
        public void ValidateRejectsBadValues()
        {
            var c = new SimConfig();
            c.stageTimes = new[] { c.stageTimes[0], c.stageTimes[1], c.stageTimes[2], c.stageTimes[3] };
            Assert.Throws<ArgumentException>(c.Validate, "missing Haul");

            c = new SimConfig();
            c.stageTimes[0].baseTimeMs = 6050;
            Assert.Throws<ArgumentException>(c.Validate, "baseTimeMs not a multiple of tick");

            c = new SimConfig();
            c.species[1].id = "L1";
            Assert.Throws<ArgumentException>(c.Validate, "duplicate species id");

            c = new SimConfig();
            c.species[0].ability2 = c.species[0].ability1;
            Assert.Throws<ArgumentException>(c.Validate, "two identical abilities");

            c = new SimConfig();
            c.species[0].ability1 = "Fly";
            Assert.Throws<ArgumentException>(c.Validate, "ability Fly");

            c = new SimConfig();
            c.species[0].ability1 = "3";
            Assert.Throws<ArgumentException>(c.Validate, "numeric ability");

            c = new SimConfig();
            c.species[0].mastery1 = 0;
            Assert.Throws<ArgumentException>(c.Validate, "mastery1 = 0");

            c = new SimConfig();
            c.species[0].moveSpeedMilliCellsPerSecond = 5;
            Assert.Throws<ArgumentException>(c.Validate, "speed too small");

            c = new SimConfig();
            c.species[0].sizeX = 0;
            Assert.Throws<ArgumentException>(c.Validate, "sizeX = 0");

            c = new SimConfig();
            c.player.rainCooldownMs = 0;
            Assert.Throws<ArgumentException>(c.Validate, "rainCooldownMs = 0");

            c = new SimConfig();
            c.player = null;
            Assert.Throws<ArgumentException>(c.Validate, "player = null");

            c = new SimConfig();
            c.crops[0].startSeeds = 41;
            Assert.Throws<ArgumentException>(c.Validate, "total startSeeds > capacity");

            c = new SimConfig();
            c.crops = new CropDef[0];
            Assert.Throws<ArgumentException>(c.Validate, "empty crops");

            c = new SimConfig();
            c.crops[1].id = c.crops[0].id;
            Assert.Throws<ArgumentException>(c.Validate, "duplicate crop id");

            c = new SimConfig();
            c.crops[0].growTimeMs = 120050;
            Assert.Throws<ArgumentException>(c.Validate, "growTimeMs not divisible by msPerTick");

            c = new SimConfig();
            c.crops[0].seedsPerHarvest = 0;
            c.crops[0].producePerHarvest = 0;
            Assert.Throws<ArgumentException>(c.Validate, "each harvest yields nothing");

            c = new SimConfig();
            c.farm.queueSlots = 0;
            Assert.Throws<ArgumentException>(c.Validate, "wait cells 0");

            c = new SimConfig();
            c.farm.slotMaxCount = 0;
            Assert.Throws<ArgumentException>(c.Validate, "max 0");

            c = new SimConfig();
            c.house.capacity = 0;
            Assert.Throws<ArgumentException>(c.Validate, "house capacity 0");

            c = new SimConfig();
            c.creature.stuckTimeMs = 30050;
            Assert.Throws<ArgumentException>(c.Validate, "stuckTimeMs not a multiple of tick");

            c = new SimConfig();
            c.creature.blockWaitMs = 1050;
            Assert.Throws<ArgumentException>(c.Validate, "blockWaitMs not a multiple of tick");

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
            Assert.Throws<ArgumentException>(c.Validate, "wanderMinMs not a multiple of tick");

            c = new SimConfig();
            c.creature.wanderMaxMs = 4000;
            Assert.Throws<ArgumentException>(c.Validate, "wanderMaxMs < wanderMinMs");

            c = new SimConfig();
            c.masteryLevels = new MasteryLevel[0];
            Assert.Throws<ArgumentException>(c.Validate, "empty level table");

            c = new SimConfig();
            c.masteryLevels[0].minMastery = 2;
            Assert.Throws<ArgumentException>(c.Validate, "first threshold > 1");

            c = new SimConfig();
            c.masteryLevels[2].minMastery = 50;
            Assert.Throws<ArgumentException>(c.Validate, "thresholds not increasing");

            c = new SimConfig();
            c.masteryLevels[0].speedPermille = 0;
            Assert.Throws<ArgumentException>(c.Validate, "factor 0");

            c = new SimConfig();
            c.masteryLevels[4].speedPermille = 10001;
            Assert.Throws<ArgumentException>(c.Validate, "factor > 10000");

            c = new SimConfig();
            c.masteryLevels[3].speedPermille = 900;
            Assert.Throws<ArgumentException>(c.Validate, "factor decreasing");
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
