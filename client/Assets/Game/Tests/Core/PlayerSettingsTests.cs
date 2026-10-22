using NUnit.Framework;
using UnityEngine;

namespace Game.Core.Tests
{
    public class PlayerSettingsTests
    {
        [Test]
        public void DefaultsUnchangedByClamp()
        {
            var s = new PlayerSettings();
            Assert.AreEqual(1, s.formatVersion);
            Assert.AreEqual(6f, s.cameraDistance);
            Assert.AreEqual(0.15f, s.lookSensitivity);
            Assert.IsFalse(s.invertY);
            Assert.AreEqual(1f, s.uiScale);
            Assert.AreEqual(60, s.targetFps);
            Assert.AreEqual("vi", s.language);
            var c = s.Clone();
            c.Clamp();
            Assert.AreEqual(JsonUtility.ToJson(s), JsonUtility.ToJson(c));
        }

        [Test]
        public void ClampRanges()
        {
            var s = new PlayerSettings { cameraDistance = 1, lookSensitivity = 0, uiScale = 0.1f, targetFps = 45 };
            s.Clamp();
            Assert.AreEqual(2.5f, s.cameraDistance);
            Assert.AreEqual(0.05f, s.lookSensitivity);
            Assert.AreEqual(0.75f, s.uiScale);
            Assert.AreEqual(60, s.targetFps);
            s = new PlayerSettings { cameraDistance = 99, lookSensitivity = 1, uiScale = 5, targetFps = 30 };
            s.Clamp();
            Assert.AreEqual(14f, s.cameraDistance);
            Assert.AreEqual(0.5f, s.lookSensitivity);
            Assert.AreEqual(2f, s.uiScale);
            Assert.AreEqual(30, s.targetFps);
        }

        [Test]
        public void NaNAndInfinityGoToDefault()
        {
            foreach (var bad in new[] { float.NaN, float.PositiveInfinity })
            {
                var s = new PlayerSettings { cameraDistance = bad, lookSensitivity = bad, uiScale = bad };
                s.Clamp();
                Assert.AreEqual(6f, s.cameraDistance);
                Assert.AreEqual(0.15f, s.lookSensitivity);
                Assert.AreEqual(1f, s.uiScale);
            }
        }

        [Test]
        public void Language()
        {
            foreach (var ok in new[] { "en", "vi_custom-2" })
            {
                var s = new PlayerSettings { language = ok };
                s.Clamp();
                Assert.AreEqual(ok, s.language);
            }
            foreach (var bad in new[] { null, "", "../x", "EN", "a b", new string('a', 33) })
            {
                var s = new PlayerSettings { language = bad };
                s.Clamp();
                Assert.AreEqual("vi", s.language);
            }
        }

        [Test]
        public void JsonMissingFieldsKeepDefaults()
        {
            var s = JsonUtility.FromJson<PlayerSettings>("{\"uiScale\":1.5}");
            Assert.AreEqual(1.5f, s.uiScale);
            Assert.AreEqual(6f, s.cameraDistance);
            Assert.AreEqual(60, s.targetFps);
            Assert.AreEqual("vi", s.language);
        }

        [Test]
        public void CloneIsIndependent()
        {
            var a = new PlayerSettings();
            var b = a.Clone();
            b.cameraDistance = 10; b.invertY = true; b.language = "en";
            Assert.AreEqual(6f, a.cameraDistance);
            Assert.IsFalse(a.invertY);
            Assert.AreEqual("vi", a.language);
        }
    }
}
