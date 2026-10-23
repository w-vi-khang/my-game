using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Client.EditTests
{
    /// <summary>0033: bảng hành động + phím mặc định, lớp GameInput khớp asset, không còn đọc thẳng phím trong phạm vi.</summary>
    public class GameControlsTests
    {
        static readonly string[] AllowedDuplicates = { }; // Cặp trùng cố ý trong cùng map: không có (0033)

        static string Path_(params string[] p) => Path.Combine(new[] { Application.dataPath, "Game" }.Concat(p).ToArray());
        static InputActionAsset Load() => InputActionAsset.FromJson(File.ReadAllText(Path_("Client", "Input", "GameControls.inputactions")));

        static (InputActionType type, string ect, string[] paths) E(InputActionType t, string ect, params string[] paths) => (t, ect, paths);
        static (InputActionType, string, string[]) B(string path) => E(InputActionType.Button, "Button", path);

        static Dictionary<string, (InputActionType type, string ect, string[] paths)> Expected() => new Dictionary<string, (InputActionType, string, string[])>
        {
            ["Player/MoveClick"] = B("<Mouse>/leftButton"),
            ["Player/Jump"] = B("<Keyboard>/a"),
            ["Player/Dash"] = B("<Keyboard>/shift"),
            ["Player/UseTool"] = B("<Mouse>/rightButton"),
            ["Player/Back"] = B("<Keyboard>/escape"),
            ["Player/SelectBlock1"] = B("<Keyboard>/1"),
            ["Player/SelectBlock2"] = B("<Keyboard>/2"),
            ["Player/SelectBlock3"] = B("<Keyboard>/3"),
            ["Player/SelectBlock4"] = B("<Keyboard>/4"),
            ["Player/Collect"] = B("<Keyboard>/f"),
            ["Player/ModeDefault"] = B("<Keyboard>/d"),
            ["Player/ModeDig"] = B("<Keyboard>/q"),
            ["Player/ModeTill"] = B("<Keyboard>/e"),
            ["Player/ModeRain"] = B("<Keyboard>/r"),
            ["Player/ToggleBag"] = B("<Keyboard>/b"),
            ["Player/ToggleStock"] = B("<Keyboard>/k"),
            ["Player/ToggleMap"] = B("<Keyboard>/m"),
            ["Player/ToggleTimeDial"] = B("<Keyboard>/t"),
            ["Player/ToggleSettings"] = B("<Keyboard>/o"),
            ["Player/RotateCamera"] = B("<Mouse>/middleButton"),
            ["Player/Look"] = E(InputActionType.Value, "Vector2", "<Mouse>/delta"),
            ["Player/Zoom"] = E(InputActionType.Value, "Axis", "<Mouse>/scroll/y"),
            ["Player/CycleBlockModifier"] = B("<Keyboard>/alt"),
            ["Player/ToggleObservation"] = B("<Keyboard>/v"),
            ["Player/CameraReset"] = B("<Keyboard>/space"),
            ["Player/ToggleFirstPerson"] = B("<Keyboard>/x"),
            ["Pointer/Point"] = E(InputActionType.Value, "Vector2", "<Mouse>/position"),
            ["Panels/Close"] = B("<Keyboard>/escape"),
            ["Panels/TransferAll"] = B("<Keyboard>/shift"),
            ["Map/Press"] = B("<Mouse>/leftButton"),
            ["Map/Zoom"] = E(InputActionType.Value, "Axis", "<Mouse>/scroll/y"),
            ["Map/Close"] = B("<Keyboard>/escape"),
            ["TimeDial/Press"] = B("<Mouse>/leftButton"),
            ["TimeDial/Close"] = B("<Keyboard>/escape"),
            ["Settings/Close"] = B("<Keyboard>/escape"),
        };

        [Test]
        public void AssetMatchesDefaultTable()
        {
            var asset = Load();
            var exp = Expected();
            Assert.AreEqual(35, exp.Count);
            var actual = new HashSet<string>(asset.actionMaps.SelectMany(m => m.actions).Select(a => a.actionMap.name + "/" + a.name));
            CollectionAssert.AreEquivalent(exp.Keys, actual);
            foreach (var kv in exp)
            {
                var a = asset.FindAction(kv.Key, true);
                Assert.AreEqual(kv.Value.type, a.type, kv.Key + " type");
                Assert.AreEqual(kv.Value.ect, a.expectedControlType, kv.Key + " controlType");
                CollectionAssert.AreEqual(kv.Value.paths, a.bindings.Select(b => b.path).ToArray(), kv.Key + " bindings");
            }
        }

        [Test]
        public void NoDuplicateBindingsWithinMap()
        {
            var bad = new List<string>();
            foreach (var m in Load().actionMaps)
                foreach (var g in m.bindings.Where(b => !b.isComposite).GroupBy(b => b.path).Where(g => g.Count() > 1))
                    if (!AllowedDuplicates.Contains(m.name + ":" + g.Key))
                        bad.Add(m.name + " " + g.Key);
            Assert.IsEmpty(bad, "Duplicate keys in the same map: " + string.Join("; ", bad));
        }

        [Test]
        public void GameInputCoversAllActions()
        {
            var text = File.ReadAllText(Path_("Client", "GameInput.cs"));
            var found = new HashSet<string>(Regex.Matches(text, "Get\\(\"([A-Za-z]+/[A-Za-z0-9]+)\"\\)").Cast<Match>().Select(m => m.Groups[1].Value));
            var actual = new HashSet<string>(Load().actionMaps.SelectMany(m => m.actions).Select(a => a.actionMap.name + "/" + a.name));
            Assert.AreEqual(35, actual.Count);
            CollectionAssert.AreEquivalent(actual, found);
        }

        [Test]
        public void NoDirectInputReadsInScope()
        {
            var files = new[] { "SimView.cs", "FollowCamera.cs", "TimeDial.cs", "MinimapView.cs", "SettingsView.cs", "TerrainEditInput.cs", "PlayerCharacter.cs", "PlayerFarmActions.cs", "ObservationCamera.cs", "MoveMarkers.cs" };
            var banned = new Regex(@"Keyboard\.current|Mouse\.current|\bInput\.Get|\bkb\.|\bmouse\.");
            var hits = new List<string>();
            foreach (var f in files)
            {
                var lines = File.ReadAllLines(Path_("Client", f));
                for (int i = 0; i < lines.Length; i++)
                    if (!lines[i].Trim().StartsWith("//") && banned.IsMatch(lines[i])) hits.Add(f + ":" + (i + 1));
            }
            Assert.IsEmpty(hits, "Still reads keys/mouse directly: " + string.Join(", ", hits));
        }

        [Test]
        public void ProjectWideActionsIsGameControls()
        {
            var meta = File.ReadAllText(Path_("Client", "Input", "GameControls.inputactions.meta"));
            var guid = Regex.Match(meta, "guid: ([0-9a-f]{32})").Groups[1].Value;
            Assert.IsNotEmpty(guid);
            var line = File.ReadAllLines(Path.Combine(Application.dataPath, "..", "ProjectSettings", "EditorBuildSettings.asset"))
                .First(l => l.Contains("com.unity.input.settings.actions"));
            StringAssert.Contains(guid, line);
            Assert.IsFalse(File.Exists(Path.Combine(Application.dataPath, "InputSystem_Actions.inputactions")));
        }
    }
}
