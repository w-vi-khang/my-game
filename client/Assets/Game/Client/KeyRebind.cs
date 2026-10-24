using System;
using System.Collections.Generic;
using UnityEngine.InputSystem;

namespace Game.Client
{
    /// <summary>Bảng đổi phím (7l f, 0033 mục 2 / 0034 mục 8): hành động đổi được, phím hợp lệ, trùng trong cùng ngữ cảnh (= action map), đổi chỗ, lưu/nạp override, đổi phím tương tác. Mọi hành động có đúng 1 binding (index 0).</summary>
    public static class KeyRebind
    {
        public static readonly string[] Contexts = { "Player", "Panels", "Map", "TimeDial" };

        static readonly string[] MouseButtons = { "leftButton", "rightButton", "middleButton", "forwardButton", "backButton" };
        static readonly string[] MouseKeys = { "controls.mouse_left", "controls.mouse_right", "controls.mouse_middle", "controls.mouse_forward", "controls.mouse_back" };

        public static readonly Dictionary<string, string> NameKeys = new Dictionary<string, string>
        {
            ["Player/MoveClick"] = "controls.move_click",
            ["Player/UseTool"] = "controls.use_tool",
            ["Player/RotateCamera"] = "controls.rotate_camera",
            ["Player/Jump"] = "controls.jump",
            ["Player/Dash"] = "controls.dash",
            ["Player/CameraReset"] = "controls.camera_reset",
            ["Player/ToggleFirstPerson"] = "controls.first_person",
            ["Player/ModeDefault"] = "controls.mode_default",
            ["Player/ModeDig"] = "controls.mode_dig",
            ["Player/ModeTill"] = "controls.mode_till",
            ["Player/ModeRain"] = "controls.mode_rain",
            ["Player/Collect"] = "controls.collect",
            ["Player/ToggleBag"] = "controls.bag",
            ["Player/ToggleHotbar"] = "controls.toggle_hotbar",
            ["Player/ToggleSpellMenu"] = "controls.spell_menu",
            ["Player/ToggleObservation"] = "controls.observe",
            ["Player/ToggleMap"] = "controls.map",
            ["Player/ToggleSettings"] = "controls.settings",
            ["Player/Hotbar1"] = "controls.hotbar",
            ["Player/Hotbar2"] = "controls.hotbar",
            ["Player/Hotbar3"] = "controls.hotbar",
            ["Player/Hotbar4"] = "controls.hotbar",
            ["Player/Hotbar5"] = "controls.hotbar",
            ["Player/Hotbar6"] = "controls.hotbar",
            ["Player/Hotbar7"] = "controls.hotbar",
            ["Player/Hotbar8"] = "controls.hotbar",
            ["Player/Hotbar9"] = "controls.hotbar",
            ["Player/Hotbar10"] = "controls.hotbar",
            ["Panels/NextTarget"] = "controls.next_target",
            ["Panels/TransferAll"] = "controls.transfer_all",
            ["Panels/TransferHalf"] = "controls.transfer_half",
            ["Panels/TransferThird"] = "controls.transfer_third",
            ["Map/Press"] = "controls.map_press",
            ["TimeDial/Press"] = "controls.dial_press",
        };

        public static List<InputAction> Rebindable(InputActionAsset asset)
        {
            var r = new List<InputAction>();
            foreach (var ctx in Contexts)
            {
                var map = asset.FindActionMap(ctx, false);
                if (map == null) continue;
                foreach (var a in map.actions)
                    if (a.type == InputActionType.Button && !string.Equals(a.bindings[0].path, "<Keyboard>/escape", StringComparison.OrdinalIgnoreCase))
                        r.Add(a);
            }
            return r;
        }

        public static string DisplayName(InputAction a)
        {
            if (a.name.StartsWith("Hotbar", StringComparison.Ordinal) && int.TryParse(a.name.Substring(6), out int n))
                return GameSettings.Text.Format("controls.hotbar", n);
            return GameSettings.Text.Get(NameKeys[a.actionMap.name + "/" + a.name]);
        }

        public static string DisplayPath(string path)
        {
            if (path != null)
                for (int i = 0; i < MouseButtons.Length; i++)
                    if (string.Equals(path, "<Mouse>/" + MouseButtons[i], StringComparison.OrdinalIgnoreCase))
                        return GameSettings.Text.Get(MouseKeys[i]);
            return InputControlPath.ToHumanReadableString(path, InputControlPath.HumanReadableStringOptions.OmitDevice);
        }

        public static string DisplayKey(InputAction a) => DisplayPath(a.bindings[0].effectivePath);

        public static bool IsAllowedPath(string path)
        {
            if (string.IsNullOrEmpty(path)) return false;
            const StringComparison ic = StringComparison.OrdinalIgnoreCase;
            if (path.StartsWith("<Keyboard>/", ic))
            {
                string k = path.Substring("<Keyboard>/".Length);
                if (k.Length == 0 || k.Contains("/")) return false;
                return !(k.Equals("escape", ic) || k.Equals("f1", ic) || k.Equals("f2", ic) || k.Equals("f3", ic) || k.Equals("anykey", ic));
            }
            if (path.StartsWith("<Mouse>/", ic))
            {
                string b = path.Substring("<Mouse>/".Length);
                foreach (var m in MouseButtons) if (b.Equals(m, ic)) return true;
            }
            return false;
        }

        public static bool SameControl(string a, string b)
        {
            if (a == null || b == null) return false;
            if (string.Equals(a, b, StringComparison.OrdinalIgnoreCase)) return true;
            return Generic(a, b) || Generic(b, a);
        }

        static bool Generic(string generic, string specific)
        {
            foreach (var m in new[] { "shift", "ctrl", "alt" })
            {
                if (!string.Equals(generic, "<Keyboard>/" + m, StringComparison.OrdinalIgnoreCase)) continue;
                if (string.Equals(specific, "<Keyboard>/left" + m, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(specific, "<Keyboard>/right" + m, StringComparison.OrdinalIgnoreCase)) return true;
            }
            return false;
        }

        public static InputAction FindConflict(InputAction action, string path)
        {
            foreach (var other in action.actionMap.actions)
                if (other != action && SameControl(other.bindings[0].effectivePath, path)) return other;
            return null;
        }

        public static void SetPath(InputAction action, string path)
        {
            if (string.Equals(path, action.bindings[0].path, StringComparison.OrdinalIgnoreCase)) action.RemoveBindingOverride(0);
            else action.ApplyBindingOverride(0, path);
        }

        public static void Swap(InputAction a, InputAction b)
        {
            string pa = a.bindings[0].effectivePath, pb = b.bindings[0].effectivePath;
            SetPath(a, pb);
            SetPath(b, pa);
        }

        public static string Save(InputActionAsset asset) => asset.SaveBindingOverridesAsJson();

        /// <summary>Nạp override; false = hỏng / không hợp lệ / trùng → đã về mặc định (người gọi log).</summary>
        public static bool Load(InputActionAsset asset, string json)
        {
            asset.RemoveAllBindingOverrides();
            if (string.IsNullOrEmpty(json)) return true;
            try { asset.LoadBindingOverridesFromJson(json); }
            catch (Exception) { asset.RemoveAllBindingOverrides(); return false; }
            foreach (var a in Rebindable(asset))
                if (!IsAllowedPath(a.bindings[0].effectivePath) || FindConflict(a, a.bindings[0].effectivePath) != null)
                {
                    asset.RemoveAllBindingOverrides();
                    return false;
                }
            return true;
        }

        public static InputActionRebindingExtensions.RebindingOperation Start(InputAction action, Action<bool, string> done)
        {
            var asset = action.actionMap.asset;
            var wasEnabled = new List<InputActionMap>();
            foreach (var m in asset.actionMaps) if (m.enabled) { wasEnabled.Add(m); m.Disable(); }
            string oldPath = action.bindings[0].effectivePath;
            void Finish(InputActionRebindingExtensions.RebindingOperation op, bool ok)
            {
                if (ok && !IsAllowedPath(action.bindings[0].effectivePath)) { SetPath(action, oldPath); ok = false; }
                op.Dispose();
                foreach (var m in wasEnabled) m.Enable();
                done(ok, oldPath);
            }
            return action.PerformInteractiveRebinding(0)
                .WithControlsHavingToMatchPath("<Keyboard>")
                .WithControlsHavingToMatchPath("<Mouse>")
                .WithControlsExcluding("<Keyboard>/anyKey")
                .WithControlsExcluding("<Mouse>/press")
                .WithControlsExcluding("<Mouse>/scroll")
                .WithControlsExcluding("<Keyboard>/f1")
                .WithControlsExcluding("<Keyboard>/f2")
                .WithControlsExcluding("<Keyboard>/f3")
                .WithCancelingThrough("<Keyboard>/escape")
                .OnComplete(op => Finish(op, true))
                .OnCancel(op => Finish(op, false))
                .Start();
        }
    }
}
