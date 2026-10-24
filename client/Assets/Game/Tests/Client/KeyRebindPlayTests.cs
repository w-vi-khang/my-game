using System;
using System.Collections;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;

namespace Game.Client.Tests
{
    /// <summary>7l f: lưu/nạp phím, đổi phím tương tác, hỏi trùng trong bảng Cài đặt.</summary>
    public class KeyRebindPlayTests
    {
        string dir;
        GameObject go;

        [SetUp]
        public void SetUp()
        {
            dir = Path.Combine(Path.GetTempPath(), "mygame-keys-" + Guid.NewGuid().ToString("N"));
            GameSettings.ResetForTests(dir);
            GameSettings.Load(); // nạp trước: lần đầu truy cập Current sẽ Load() và xoá override đã đặt trong test
        }

        [TearDown]
        public void TearDown()
        {
            if (go != null) UnityEngine.Object.DestroyImmediate(go);
            InputSystem.actions.RemoveAllBindingOverrides();
            try { if (Directory.Exists(dir)) Directory.Delete(dir, true); } catch (Exception) { }
            GameSettings.ResetForTests(SettingsTestRoot.Dir);
        }

        static string P(InputAction a) => a.bindings[0].effectivePath;

        SettingsView MakeView()
        {
            go = new GameObject("settings-view");
            return go.AddComponent<SettingsView>();
        }

        [Test]
        public void SaveAndReload()
        {
            KeyRebind.Swap(GameInput.MoveClick, GameInput.UseTool);
            GameSettings.SetBindingOverrides(KeyRebind.Save(InputSystem.actions));
            GameSettings.Save();
            InputSystem.actions.RemoveAllBindingOverrides();
            GameSettings.Load();
            Assert.AreEqual("<Mouse>/rightButton", P(GameInput.MoveClick));
            Assert.AreEqual("<Mouse>/leftButton", P(GameInput.UseTool));
            StringAssert.Contains("bindingOverrides", File.ReadAllText(GameSettings.SettingsPath));
        }

        [Test]
        public void BadOverrides_FallBackToDefaults()
        {
            Directory.CreateDirectory(dir);
            File.WriteAllText(GameSettings.SettingsPath, "{\"bindingOverrides\":\"{bad\"}");
            LogAssert.Expect(LogType.Warning, new Regex("^KEYS"));
            GameSettings.Load();
            Assert.AreEqual("<Mouse>/leftButton", P(GameInput.MoveClick));
            Assert.AreEqual("", GameSettings.Current.bindingOverrides);
        }

        static IEnumerator Pump(float seconds = 0.2f)
        {
            InputSystem.Update();
            yield return new WaitForSecondsRealtime(seconds);
            for (int i = 0; i < 3; i++) { InputSystem.Update(); yield return null; }
        }

        [UnityTest]
        public IEnumerator Interactive_KeyboardKey()
        {
            var kb = InputSystem.AddDevice<Keyboard>();
            var map = InputSystem.actions.FindActionMap("Panels", true);
            bool panelsWere = map.enabled, playerWere = InputSystem.actions.FindActionMap("Player", true).enabled;
            InputSystem.actions.FindActionMap("Player", true).Enable();
            if (map.enabled) map.Disable();
            try
            {
                bool? ok = null; string old = null;
                KeyRebind.Start(GameInput.Jump, (c, o) => { ok = c; old = o; });
                Assert.IsFalse(InputSystem.actions.FindActionMap("Player", true).enabled);
                InputSystem.QueueStateEvent(kb, new KeyboardState(Key.K)); InputSystem.Update();
                yield return null;
                InputSystem.QueueStateEvent(kb, new KeyboardState()); InputSystem.Update();
                yield return Pump();
                Assert.IsTrue(ok == true);
                Assert.AreEqual("<Keyboard>/a", old);
                Assert.AreEqual("<Keyboard>/k", P(GameInput.Jump));
                Assert.IsTrue(InputSystem.actions.FindActionMap("Player", true).enabled);
                Assert.IsFalse(map.enabled);
            }
            finally
            {
                InputSystem.RemoveDevice(kb);
                if (!playerWere) InputSystem.actions.FindActionMap("Player", true).Disable();
                if (panelsWere) map.Enable();
            }
        }

        [UnityTest]
        public IEnumerator Interactive_EscCancels()
        {
            var kb = InputSystem.AddDevice<Keyboard>();
            try
            {
                bool? ok = null;
                KeyRebind.Start(GameInput.Jump, (c, o) => ok = c);
                InputSystem.QueueStateEvent(kb, new KeyboardState(Key.Escape)); InputSystem.Update();
                yield return null;
                InputSystem.QueueStateEvent(kb, new KeyboardState()); InputSystem.Update();
                yield return Pump();
                Assert.IsTrue(ok == false);
                Assert.AreEqual("<Keyboard>/a", P(GameInput.Jump));
            }
            finally { InputSystem.RemoveDevice(kb); }
        }

        [UnityTest]
        public IEnumerator Interactive_MouseRightButton()
        {
            var mouse = InputSystem.AddDevice<Mouse>();
            try
            {
                bool? ok = null;
                KeyRebind.Start(GameInput.MoveClick, (c, o) => ok = c);
                InputSystem.QueueStateEvent(mouse, new MouseState().WithButton(MouseButton.Right)); InputSystem.Update();
                yield return null;
                InputSystem.QueueStateEvent(mouse, new MouseState()); InputSystem.Update();
                yield return Pump();
                Assert.IsTrue(ok == true);
                Assert.AreEqual("<Mouse>/rightButton", P(GameInput.MoveClick));
                Assert.AreEqual(GameInput.UseTool, KeyRebind.FindConflict(GameInput.MoveClick, P(GameInput.MoveClick)));
            }
            finally { InputSystem.RemoveDevice(mouse); }
        }

        [Test]
        public void View_Conflict_CancelRestores()
        {
            var view = MakeView();
            KeyRebind.SetPath(GameInput.MoveClick, "<Mouse>/rightButton");
            view.ApplyChosen(GameInput.MoveClick, "<Mouse>/leftButton");
            Assert.AreEqual(GameInput.UseTool, view.PendingConflict);
            view.ResolveConflict(false);
            Assert.AreEqual("<Mouse>/leftButton", P(GameInput.MoveClick));
            Assert.AreEqual("<Mouse>/rightButton", P(GameInput.UseTool));
            Assert.IsFalse(GameSettings.Dirty);
            Assert.IsNull(view.PendingConflict);
        }

        [Test]
        public void View_Conflict_SwapSwaps()
        {
            var view = MakeView();
            KeyRebind.SetPath(GameInput.MoveClick, "<Mouse>/rightButton");
            view.ApplyChosen(GameInput.MoveClick, "<Mouse>/leftButton");
            view.ResolveConflict(true);
            Assert.AreEqual("<Mouse>/rightButton", P(GameInput.MoveClick));
            Assert.AreEqual("<Mouse>/leftButton", P(GameInput.UseTool));
            Assert.IsTrue(GameSettings.Dirty);
            Assert.AreNotEqual("", GameSettings.Current.bindingOverrides);
        }

        [Test]
        public void View_CloseWhilePending_Cancels()
        {
            var view = MakeView();
            KeyRebind.SetPath(GameInput.MoveClick, "<Mouse>/rightButton");
            view.ApplyChosen(GameInput.MoveClick, "<Mouse>/leftButton");
            view.SetOpen(true);
            view.SetOpen(false);
            Assert.AreEqual("<Mouse>/leftButton", P(GameInput.MoveClick));
            Assert.AreEqual("<Mouse>/rightButton", P(GameInput.UseTool));
            Assert.IsNull(view.PendingConflict);
        }

        [Test]
        public void View_ResetBindings_RestoresDefaults()
        {
            var view = MakeView();
            KeyRebind.Swap(GameInput.MoveClick, GameInput.UseTool);
            GameSettings.SetBindingOverrides(KeyRebind.Save(InputSystem.actions));
            view.ResetBindings();
            Assert.AreEqual("<Mouse>/leftButton", P(GameInput.MoveClick));
            Assert.AreEqual("<Mouse>/rightButton", P(GameInput.UseTool));
            StringAssert.DoesNotContain("rightButton", GameSettings.Current.bindingOverrides);
        }

        [Test]
        public void View_NoConflict_SavesImmediately()
        {
            var view = MakeView();
            KeyRebind.SetPath(GameInput.Jump, "<Keyboard>/k");
            view.ApplyChosen(GameInput.Jump, "<Keyboard>/a");
            Assert.IsNull(view.PendingConflict);
            Assert.IsTrue(GameSettings.Dirty);
        }

        [Test]
        public void GuideStartsWithKeysNote()
        {
            StringAssert.StartsWith(GameSettings.Text.Get("guide.keys_note"), SettingsView.GuideText());
        }
    }
}
