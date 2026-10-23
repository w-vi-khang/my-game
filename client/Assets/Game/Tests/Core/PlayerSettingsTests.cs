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
            Assert.AreEqual(3, s.formatVersion);
            Assert.AreEqual(0f, s.cameraDistance);
            Assert.AreEqual(0f, s.cameraPitch);
            Assert.AreEqual(0.3f, s.lookSensitivity);
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
            var s = new PlayerSettings { cameraDistance = 1, cameraPitch = 5, lookSensitivity = 0, uiScale = 0.1f, targetFps = 45 };
            s.Clamp();
            Assert.AreEqual(2.5f, s.cameraDistance);
            Assert.AreEqual(10f, s.cameraPitch);
            Assert.AreEqual(0.05f, s.lookSensitivity);
            Assert.AreEqual(0.75f, s.uiScale);
            Assert.AreEqual(60, s.targetFps);
            s = new PlayerSettings { cameraDistance = 99, cameraPitch = 99, lookSensitivity = 1, uiScale = 5, targetFps = 30 };
            s.Clamp();
            Assert.AreEqual(24f, s.cameraDistance);
            Assert.AreEqual(85f, s.cameraPitch);
            Assert.AreEqual(0.5f, s.lookSensitivity);
            Assert.AreEqual(2f, s.uiScale);
            Assert.AreEqual(30, s.targetFps);
        }

        [Test]
        public void NaNAndInfinityGoToDefault()
        {
            foreach (var bad in new[] { float.NaN, float.PositiveInfinity })
            {
                var s = new PlayerSettings { cameraDistance = bad, cameraPitch = bad, lookSensitivity = bad, uiScale = bad };
                s.Clamp();
                Assert.AreEqual(0f, s.cameraDistance);
                Assert.AreEqual(0f, s.cameraPitch);
                Assert.AreEqual(0.3f, s.lookSensitivity);
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
            Assert.AreEqual(0f, s.cameraDistance);
            Assert.AreEqual(60, s.targetFps);
            Assert.AreEqual("vi", s.language);
        }

        [Test]
        public void CloneIsIndependent()
        {
            var a = new PlayerSettings();
            var b = a.Clone();
            b.cameraDistance = 10; b.cameraPitch = 40; b.invertY = true; b.language = "en";
            Assert.AreEqual(0f, a.cameraDistance);
            Assert.AreEqual(0f, a.cameraPitch);
            Assert.AreEqual(40f, b.cameraPitch);
            Assert.IsFalse(a.invertY);
            Assert.AreEqual("vi", a.language);
        }

        [Test]
        public void MigratesOldVersionsAndOptionalCamera()
        {
            var s = JsonUtility.FromJson<PlayerSettings>("{\"formatVersion\":1,\"cameraDistance\":6}");
            s.Clamp();
            Assert.AreEqual(3, s.formatVersion);
            Assert.AreEqual(0f, s.cameraDistance);
            Assert.AreEqual(0f, s.cameraPitch);
            Assert.AreEqual(0.3f, s.lookSensitivity);
            s = JsonUtility.FromJson<PlayerSettings>("{\"formatVersion\":2,\"cameraDistance\":10,\"cameraPitch\":40,\"lookSensitivity\":0.15}");
            s.Clamp();
            Assert.AreEqual(3, s.formatVersion);
            Assert.AreEqual(0f, s.cameraDistance);
            Assert.AreEqual(0f, s.cameraPitch);
            Assert.AreEqual(0.3f, s.lookSensitivity);
            s = JsonUtility.FromJson<PlayerSettings>("{\"formatVersion\":3,\"cameraDistance\":10,\"cameraPitch\":40,\"lookSensitivity\":0.2}");
            s.Clamp();
            Assert.AreEqual(10f, s.cameraDistance);
            Assert.AreEqual(40f, s.cameraPitch);
            Assert.AreEqual(0.2f, s.lookSensitivity, 1e-5f);
            s = new PlayerSettings { cameraDistance = -3, cameraPitch = -3 };
            s.Clamp();
            Assert.AreEqual(0f, s.cameraDistance);
            Assert.AreEqual(0f, s.cameraPitch);
        }
    }
}
