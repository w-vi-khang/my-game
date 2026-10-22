using System;
using System.Collections;
using System.IO;
using System.Linq;
using Game.Core;
using Game.Core.DayCycle;
using Game.Core.Text;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Game.Client.Tests
{
    /// <summary>0032 (B) client — settings.json, bảng chuỗi lúc chạy, bảng Cài đặt.</summary>
    public class SettingsPlayTests
    {
        string dir;

        [Test, Order(0)]
        public void Isolation_RootIsUnderTempPath()
        {
            StringAssert.StartsWith(Path.GetTempPath(), GameSettings.Root);
        }

        [SetUp]
        public void SetUp()
        {
            dir = Path.Combine(Path.GetTempPath(), "mygame-settings-" + Guid.NewGuid().ToString("N"));
            GameSettings.ResetForTests(dir);
        }

        [TearDown]
        public void TearDown()
        {
            try { if (Directory.Exists(dir)) Directory.Delete(dir, true); } catch (Exception) { }
            GameSettings.ResetForTests(SettingsTestRoot.Dir);
        }

        void WriteLang(string name, string content)
        {
            Directory.CreateDirectory(GameSettings.LangDir);
            File.WriteAllText(Path.Combine(GameSettings.LangDir, name), content);
        }

        [Test]
        public void NoFile_Defaults()
        {
            var d = new PlayerSettings();
            Assert.AreEqual(d.uiScale, GameSettings.Current.uiScale);
            Assert.AreEqual(d.targetFps, GameSettings.Current.targetFps);
            Assert.IsFalse(GameSettings.Dirty);
        }

        [Test]
        public void SaveAndReload()
        {
            var s = GameSettings.Current.Clone();
            s.uiScale = 1.5f;
            GameSettings.Apply(s);
            Assert.IsTrue(GameSettings.Dirty);
            GameSettings.Save();
            Assert.IsTrue(File.Exists(GameSettings.SettingsPath));
            GameSettings.ResetForTests(dir);
            Assert.AreEqual(1.5f, GameSettings.Current.uiScale);
        }

        [Test]
        public void BadFile_Defaults()
        {
            Directory.CreateDirectory(dir);
            File.WriteAllText(GameSettings.SettingsPath, "{bad");
            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("SETTINGS"));
            Assert.AreEqual(1f, GameSettings.Current.uiScale);
        }

        [Test]
        public void OutOfRange_Clamped()
        {
            Directory.CreateDirectory(dir);
            File.WriteAllText(GameSettings.SettingsPath, "{\"uiScale\":9,\"targetFps\":45}");
            Assert.AreEqual(2f, GameSettings.Current.uiScale);
            Assert.AreEqual(60, GameSettings.Current.targetFps);
        }

        [Test]
        public void Language_FileWithFallback()
        {
            WriteLang("en.json", "{\"formatVersion\":1,\"language\":\"English\",\"entries\":[{\"key\":\"settings.title\",\"text\":\"Settings\"}]}");
            var s = GameSettings.Current.Clone();
            s.language = "en";
            GameSettings.Apply(s);
            Assert.AreEqual("Settings", GameSettings.Text.Get("settings.title"));
            Assert.AreEqual("Đóng (Esc)", GameSettings.Text.Get("settings.close"));
            Assert.IsNull(GameSettings.LanguageError);
        }

        [Test]
        public void Language_BadFile_FallsBackToBase()
        {
            WriteLang("xx.json", "{bad");
            var s = GameSettings.Current.Clone();
            s.language = "xx";
            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("LANG"));
            GameSettings.Apply(s);
            Assert.AreEqual("Cài đặt", GameSettings.Text.Get("settings.title"));
            Assert.AreEqual("xx", GameSettings.LanguageError);
            Assert.AreEqual("xx", GameSettings.Current.language);
        }

        [Test]
        public void Languages_List()
        {
            Assert.AreEqual(new[] { "vi" }, GameSettings.Languages().ToArray());
            WriteLang("en.json", "{}");
            WriteLang("vi.json", "{}");
            WriteLang("Bad Name.json", "{}");
            WriteLang("x.txt", "");
            Assert.AreEqual(new[] { "en", "vi" }, GameSettings.Languages().ToArray());
        }

        [Test]
        public void ExportBase_RoundTrip()
        {
            string path = GameSettings.ExportBase();
            Assert.IsTrue(File.Exists(path));
            Assert.AreEqual(Path.Combine(GameSettings.LangDir, "vi.json"), path);
            var back = new Strings(JsonUtility.FromJson<StringFile>(File.ReadAllText(path)));
            Assert.AreEqual(GameSettings.BaseText.Count, back.Count);
            foreach (var e in GameSettings.BaseText.ToFile().entries)
                Assert.AreEqual(GameSettings.BaseText.Get(e.key), back.Get(e.key));
        }

        [Test]
        public void BaseFile_HasAllKeys()
        {
            foreach (var k in SettingsView.Keys) Assert.IsTrue(GameSettings.BaseText.Has(k), k);
            Assert.GreaterOrEqual(GameSettings.BaseText.Count, 13);
        }

        [Test]
        public void UiScale_FollowsSetting()
        {
            var s = GameSettings.Current.Clone();
            s.uiScale = 1.5f;
            GameSettings.Apply(s);
            Assert.AreEqual(Mathf.Max(1f, Screen.height / 720f) * 1.5f, UiScale.Gui);
        }
    }

    public class SettingsScenePlayTests
    {
        string dir;
        GameObject camGo, go, playerGo;
        TerrainChunkView view;
        SimView sv;
        SettingsView settings;
        int origFps, origVSync;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            origFps = Application.targetFrameRate;
            origVSync = QualitySettings.vSyncCount;
            dir = Path.Combine(Path.GetTempPath(), "mygame-settings-" + Guid.NewGuid().ToString("N"));
            GameSettings.ResetForTests(dir);
            camGo = new GameObject("Cam");
            camGo.tag = "MainCamera";
            camGo.AddComponent<Camera>();
            camGo.AddComponent<DebugFlyCamera>();
            camGo.AddComponent<FollowCamera>();
            go = new GameObject("Terrain");
            go.SetActive(false);
            view = go.AddComponent<TerrainChunkView>();
            view.source = WorldSource.MapsV3;
            view.streaming = true;
            sv = go.AddComponent<SimView>();
            go.AddComponent<TerrainEditInput>();
            playerGo = new GameObject("Player");
            playerGo.SetActive(false);
            var player = playerGo.AddComponent<PlayerCharacter>();
            player.terrain = view;
            player.readKeyboard = false;
            playerGo.SetActive(true);
            go.SetActive(true);
            for (int i = 0; i < 600 && sv.Sim == null; i++) yield return null;
            Assert.IsNotNull(sv.Sim);
            settings = sv.GetComponent<SettingsView>();
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            UnityEngine.Object.Destroy(playerGo);
            UnityEngine.Object.Destroy(go);
            UnityEngine.Object.Destroy(camGo);
            yield return null;
            Application.targetFrameRate = origFps;
            QualitySettings.vSyncCount = origVSync;
            try { if (Directory.Exists(dir)) Directory.Delete(dir, true); } catch (Exception) { }
            GameSettings.ResetForTests(SettingsTestRoot.Dir);
        }

        [UnityTest]
        public IEnumerator Apply_ReachesScene()
        {
            Assert.IsNotNull(settings);
            var s = GameSettings.Current.Clone();
            s.cameraDistance = 10f; s.lookSensitivity = 0.3f; s.invertY = true; s.targetFps = 30;
            GameSettings.Apply(s);
            var follow = camGo.GetComponent<FollowCamera>();
            var fly = camGo.GetComponent<DebugFlyCamera>();
            Assert.AreEqual(10f, follow.distance);
            Assert.AreEqual(0.3f, follow.lookSensitivity);
            Assert.IsTrue(follow.invertY);
            Assert.AreEqual(0.3f, fly.lookSensitivity);
            Assert.AreEqual(30, Application.targetFrameRate);
            Assert.AreEqual(30, sv.TargetFps);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Keys_OpenClose_AndSave()
        {
            settings.HandleKeys(true, false);
            Assert.IsTrue(settings.IsOpen);
            settings.HandleKeys(true, false);
            Assert.IsFalse(settings.IsOpen);
            settings.HandleKeys(false, true);
            Assert.IsTrue(settings.IsOpen);
            var s = GameSettings.Current.Clone();
            s.uiScale = 1.25f;
            GameSettings.Apply(s);
            Assert.IsTrue(GameSettings.Dirty);
            settings.HandleKeys(false, true);
            Assert.IsFalse(settings.IsOpen);
            Assert.IsFalse(GameSettings.Dirty);
            Assert.IsTrue(File.Exists(GameSettings.SettingsPath));
            sv.ToggleBag();
            settings.HandleKeys(false, true);
            Assert.IsFalse(settings.IsOpen);
            yield return null;
        }

        [UnityTest]
        public IEnumerator LanguageSwitch_UpdatesUgui()
        {
            var map = go.AddComponent<MinimapView>();
            var lightGo = new GameObject("Light");
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            var day = camGo.AddComponent<DayCycleView>();
            day.sun = light;
            var dial = camGo.AddComponent<TimeDial>();
            for (int i = 0; i < 600 && !(map.IsReady && dial.IsReady); i++) yield return null;
            Assert.IsTrue(map.IsReady, "Bản đồ chưa dựng");
            Assert.IsTrue(dial.IsReady, "Vòng chưa dựng");
            Assert.AreEqual("Bình minh", dial.LandmarkLabel(TimeLandmark.Dawn));
            Assert.AreEqual("Đóng (M)", map.CloseButtonText);

            Directory.CreateDirectory(GameSettings.LangDir);
            var file = new StringFile
            {
                language = "English",
                entries = new[] { new StringEntry("time.landmark_dawn", "Dawn"), new StringEntry("map.close", "Close (M)") },
            };
            File.WriteAllText(Path.Combine(GameSettings.LangDir, "en.json"), JsonUtility.ToJson(file));
            var s = GameSettings.Current.Clone();
            s.language = "en";
            GameSettings.Apply(s);
            Assert.IsNull(GameSettings.LanguageError);
            Assert.AreEqual("Dawn", TimeDial.LandmarkName(TimeLandmark.Dawn));
            Assert.AreEqual("Close (M)", map.CloseButtonText);
            Assert.AreEqual("Dawn", dial.LandmarkLabel(TimeLandmark.Dawn));

            s = GameSettings.Current.Clone();
            s.language = PlayerSettings.DefaultLanguage;
            GameSettings.Apply(s);
            Assert.AreEqual("Đóng (M)", map.CloseButtonText);
            UnityEngine.Object.Destroy(lightGo);
            yield return null;
        }

        [UnityTest]
        public IEnumerator BlocksPointer_WhenOpen()
        {
            float sc = UiScale.Gui, gw = Screen.width / sc, gh = Screen.height / sc;
            var c = SettingsView.PanelRect(gw, gh).center;
            var screenPt = new Vector2(c.x * sc, Screen.height - c.y * sc);
            Assert.IsFalse(SimView.BlocksPointer(screenPt));
            settings.SetOpen(true);
            Assert.IsTrue(SimView.BlocksPointer(screenPt));
            settings.SetOpen(false);
            Assert.IsFalse(SimView.BlocksPointer(screenPt));
            yield return null;
        }
    }
}
