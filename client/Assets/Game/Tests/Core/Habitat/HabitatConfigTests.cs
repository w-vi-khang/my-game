using System;
using System.IO;
using Game.Core.Habitat;
using NUnit.Framework;
using UnityEngine;

namespace Game.Core.Tests
{
    public class HabitatConfigTests
    {
        static string SourcePath => Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "design", "data", "habitat_m1.json"));

        [Test]
        public void ConfigFileMatchesDefaults()
        {
            Assert.IsTrue(File.Exists(SourcePath), SourcePath);
            var fromFile = JsonUtility.FromJson<HabitatConfig>(File.ReadAllText(SourcePath));
            Assert.DoesNotThrow(fromFile.Validate);
            Assert.AreEqual(JsonUtility.ToJson(new HabitatConfig()), JsonUtility.ToJson(fromFile));
        }

        static void Bad(Action<HabitatConfig> mut, string why)
        {
            var c = new HabitatConfig();
            mut(c);
            Assert.Throws<ArgumentException>(c.Validate, why);
        }

        static HabitatRequirement Req(HabitatConfig c) => c.species[0].requirements[0];

        [Test]
        public void ValidateRejectsBadConfig()
        {
            Assert.DoesNotThrow(new HabitatConfig().Validate);
            Bad(c => c.configVersion = 2, "configVersion");
            Bad(c => { c.regionX0 = 0; c.regionZ0 = 0; c.regionX1 = 10; }, "region partly -1");
            Bad(c => { c.regionX0 = 5; c.regionZ0 = 0; c.regionX1 = 5; c.regionZ1 = 8; }, "X0 >= X1");
            Bad(c => { c.regionX0 = 0; c.regionZ0 = 8; c.regionX1 = 5; c.regionZ1 = 8; }, "Z0 >= Z1");
            Bad(c => { c.regionX0 = -2; c.regionZ0 = 0; c.regionX1 = 5; c.regionZ1 = 8; }, "negative region");
            Bad(c => c.species[1].id = "L1", "duplicate species id");
            Bad(c => c.species[1].id = "", "empty species id");
            Bad(c => c.unlockedAtStart = new[] { "L9" }, "unknown unlocked id");
            Bad(c => Req(c).kind = "weird", "unknown kind");
            Bad(c => Req(c).min = 0, "min < 1");
            Bad(c => Req(c).material = "Lava", "unknown material");
            Bad(c => Req(c).material = "", "empty material for cells");
            Bad(c => Req(c).material = "3", "numeric material");
            Bad(c => { var r = c.species[2].requirements[1]; r.structure = "castle"; }, "unknown structure");
            Bad(c => { var r = c.species[2].requirements[1]; r.kind = "species"; r.species = ""; }, "empty species for kind species");
            Bad(c => { var r = Req(c); r.kind = "period"; r.periods = new string[0]; }, "empty periods");
            Bad(c => { var r = Req(c); r.kind = "period"; r.periods = new[] { "Noon" }; }, "bad period");
            // min được bỏ qua với period; "Solid" hợp lệ.
            var ok = new HabitatConfig();
            Req(ok).material = "Solid";
            var p = ok.species[0].requirements[1];
            p.kind = "period";
            p.min = 0;
            p.periods = new[] { "Dawn", "Day" };
            Assert.DoesNotThrow(ok.Validate);
        }
    }
}
