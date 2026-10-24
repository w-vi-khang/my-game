using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Client.EditTests
{
    /// <summary>7l f: logic bảng đổi phím (KeyRebind) trên asset đọc từ file.</summary>
    public class KeyRebindTests
    {
        static InputActionAsset Load() => InputActionAsset.FromJson(File.ReadAllText(Path.Combine(Application.dataPath, "Game", "Client", "Input", "GameControls.inputactions")));
        static string P(InputActionAsset a, string name) => a.FindAction(name, true).bindings[0].effectivePath;

        [Test]
        public void Rebindable_Is34_ButtonsOnly_NoEscape()
        {
            var asset = Load();
            var list = KeyRebind.Rebindable(asset);
            Assert.AreEqual(34, list.Count);
            Assert.IsTrue(list.All(a => a.type == InputActionType.Button));
            Assert.IsFalse(list.Any(a => a.bindings[0].path == "<Keyboard>/escape"));
            var names = list.Select(a => a.actionMap.name + "/" + a.name).ToList();
            foreach (var n in new[] { "Player/MoveClick", "Player/UseTool", "Map/Press", "TimeDial/Press", "Panels/TransferThird" }) CollectionAssert.Contains(names, n);
            foreach (var n in new[] { "Player/Look", "Player/Back", "Settings/Close" }) CollectionAssert.DoesNotContain(names, n);
        }

        [Test]
        public void NameKeysCoverRebindable()
        {
            var list = KeyRebind.Rebindable(Load());
            foreach (var a in list) Assert.IsTrue(KeyRebind.NameKeys.ContainsKey(a.actionMap.name + "/" + a.name), a.name);
            Assert.AreEqual(34, KeyRebind.NameKeys.Count);
        }

        [Test]
        public void NameKeysExistInBaseText()
        {
            string json = File.ReadAllText(Path.Combine(Application.dataPath, "Game", "Client", "Resources", "Lang", "vi.json"));
            var keys = KeyRebind.NameKeys.Values.Concat(new[] { "controls.mouse_left", "controls.mouse_right", "controls.mouse_middle", "controls.mouse_forward", "controls.mouse_back" }).Distinct();
            foreach (var k in keys) StringAssert.Contains("\"key\": \"" + k + "\"", json, k);
        }

        [Test]
        public void IsAllowedPath_Cases()
        {
            foreach (var t in new[] { "<Keyboard>/k", "<keyboard>/K", "<Keyboard>/space", "<Mouse>/leftButton", "<Mouse>/backButton" }) Assert.IsTrue(KeyRebind.IsAllowedPath(t), t);
            foreach (var f in new[] { null, "", "<Keyboard>/escape", "<Keyboard>/f1", "<Keyboard>/f3", "<Keyboard>/anyKey", "<Mouse>/position", "<Mouse>/scroll/up", "<Mouse>/press", "<Gamepad>/buttonSouth", "<Touchscreen>/press" }) Assert.IsFalse(KeyRebind.IsAllowedPath(f), f);
        }

        [Test]
        public void SameControl_Cases()
        {
            Assert.IsTrue(KeyRebind.SameControl("<Keyboard>/shift", "<Keyboard>/leftShift"));
            Assert.IsFalse(KeyRebind.SameControl("<Keyboard>/leftShift", "<Keyboard>/rightShift"));
            Assert.IsTrue(KeyRebind.SameControl("<Keyboard>/A", "<keyboard>/a"));
            Assert.IsTrue(KeyRebind.SameControl("<Keyboard>/ctrl", "<Keyboard>/rightCtrl"));
            Assert.IsFalse(KeyRebind.SameControl("<Keyboard>/a", "<Keyboard>/b"));
        }

        [Test]
        public void FindConflict_SameMapOnly()
        {
            var asset = Load();
            var jump = asset.FindAction("Player/Jump", true);
            KeyRebind.SetPath(jump, "<Keyboard>/q");
            Assert.AreEqual(asset.FindAction("Player/ModeDig", true), KeyRebind.FindConflict(jump, "<Keyboard>/q"));
            Assert.IsNull(KeyRebind.FindConflict(asset.FindAction("Player/Hotbar1", true), "<Keyboard>/1"));
            Assert.IsNull(KeyRebind.FindConflict(asset.FindAction("Player/ModeDig", true), "<Keyboard>/k"));
        }

        [Test]
        public void Swap_MouseButtons_SaveLoadRoundTrip()
        {
            var asset = Load();
            KeyRebind.Swap(asset.FindAction("Player/MoveClick", true), asset.FindAction("Player/UseTool", true));
            Assert.AreEqual("<Mouse>/rightButton", P(asset, "Player/MoveClick"));
            Assert.AreEqual("<Mouse>/leftButton", P(asset, "Player/UseTool"));
            string json = KeyRebind.Save(asset);
            var other = Load();
            Assert.IsTrue(KeyRebind.Load(other, json));
            Assert.AreEqual("<Mouse>/rightButton", P(other, "Player/MoveClick"));
            Assert.AreEqual("<Mouse>/leftButton", P(other, "Player/UseTool"));
        }

        [Test]
        public void SetPath_ToDefault_RemovesOverride()
        {
            var asset = Load();
            var jump = asset.FindAction("Player/Jump", true);
            KeyRebind.SetPath(jump, "<Keyboard>/k");
            Assert.AreEqual("<Keyboard>/k", jump.bindings[0].effectivePath);
            KeyRebind.SetPath(jump, "<Keyboard>/a");
            Assert.IsTrue(string.IsNullOrEmpty(jump.bindings[0].overridePath));
        }

        [Test]
        public void Load_Invalid_ResetsToDefaults()
        {
            var asset = Load();
            Assert.IsFalse(KeyRebind.Load(asset, "{bad"));
            Assert.AreEqual("<Keyboard>/a", P(asset, "Player/Jump"));

            var src = Load();
            src.FindAction("Player/Jump", true).ApplyBindingOverride(0, "<Keyboard>/escape");
            Assert.IsFalse(KeyRebind.Load(asset, src.SaveBindingOverridesAsJson()));
            Assert.AreEqual("<Keyboard>/a", P(asset, "Player/Jump"));

            src = Load();
            src.FindAction("Player/Jump", true).ApplyBindingOverride(0, "<Keyboard>/q");
            Assert.IsFalse(KeyRebind.Load(asset, src.SaveBindingOverridesAsJson()));
            Assert.AreEqual("<Keyboard>/a", P(asset, "Player/Jump"));

            Assert.IsTrue(KeyRebind.Load(asset, ""));
        }
    }
}
