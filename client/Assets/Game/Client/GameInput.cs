using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Client
{
    /// <summary>Hành động điều khiển (0033): gameplay / UI đọc qua đây, không đọc thẳng phím. Asset project-wide: Input/GameControls.inputactions. Bảng đổi phím: M1 (7l f, 0034).</summary>
    public static class GameInput
    {
        public static InputAction MoveClick => Get("Player/MoveClick");
        public static InputAction Jump => Get("Player/Jump");
        public static InputAction Dash => Get("Player/Dash");
        public static InputAction UseTool => Get("Player/UseTool");
        public static InputAction Back => Get("Player/Back");
        public static InputAction Collect => Get("Player/Collect");
        public static InputAction ModeDefault => Get("Player/ModeDefault");
        public static InputAction ModeDig => Get("Player/ModeDig");
        public static InputAction ModeTill => Get("Player/ModeTill");
        public static InputAction ModeRain => Get("Player/ModeRain");
        public static InputAction ToggleBag => Get("Player/ToggleBag");
        public static InputAction Hotbar1 => Get("Player/Hotbar1");
        public static InputAction Hotbar2 => Get("Player/Hotbar2");
        public static InputAction Hotbar3 => Get("Player/Hotbar3");
        public static InputAction Hotbar4 => Get("Player/Hotbar4");
        public static InputAction Hotbar5 => Get("Player/Hotbar5");
        public static InputAction Hotbar6 => Get("Player/Hotbar6");
        public static InputAction Hotbar7 => Get("Player/Hotbar7");
        public static InputAction Hotbar8 => Get("Player/Hotbar8");
        public static InputAction Hotbar9 => Get("Player/Hotbar9");
        public static InputAction Hotbar10 => Get("Player/Hotbar10");
        public static InputAction ToggleHotbar => Get("Player/ToggleHotbar");
        /// <summary>Phím ô túi phụ k = 0..9 (1–9, 0 — 0034 mục 6); k ngoài khoảng → ArgumentOutOfRangeException.</summary>
        public static InputAction Hotbar(int k)
        {
            switch (k)
            {
                case 0: return Hotbar1;
                case 1: return Hotbar2;
                case 2: return Hotbar3;
                case 3: return Hotbar4;
                case 4: return Hotbar5;
                case 5: return Hotbar6;
                case 6: return Hotbar7;
                case 7: return Hotbar8;
                case 8: return Hotbar9;
                case 9: return Hotbar10;
                default: throw new System.ArgumentOutOfRangeException(nameof(k), k, "Hotbar key must be 0..9");
            }
        }
        public static InputAction ToggleMap => Get("Player/ToggleMap");
        public static InputAction ToggleSpellMenu => Get("Player/ToggleSpellMenu");
        public static InputAction ToggleSettings => Get("Player/ToggleSettings");
        public static InputAction RotateCamera => Get("Player/RotateCamera");
        public static InputAction Look => Get("Player/Look");
        public static InputAction Zoom => Get("Player/Zoom");
        public static InputAction ToggleObservation => Get("Player/ToggleObservation");
        public static InputAction CameraReset => Get("Player/CameraReset");
        public static InputAction ToggleFirstPerson => Get("Player/ToggleFirstPerson");
        public static InputAction Point => Get("Pointer/Point");
        public static InputAction PanelsClose => Get("Panels/Close");
        public static InputAction NextTarget => Get("Panels/NextTarget");
        public static InputAction TransferAll => Get("Panels/TransferAll");
        public static InputAction TransferHalf => Get("Panels/TransferHalf");
        public static InputAction TransferThird => Get("Panels/TransferThird");
        public static InputAction MapPress => Get("Map/Press");
        public static InputAction MapZoom => Get("Map/Zoom");
        public static InputAction MapClose => Get("Map/Close");
        public static InputAction TimeDialPress => Get("TimeDial/Press");
        public static InputAction TimeDialClose => Get("TimeDial/Close");
        public static InputAction SettingsClose => Get("Settings/Close");

        /// <summary>Có thiết bị con trỏ (chuột) gắn vào hành động Point — thay cho kiểm Mouse.current != null.</summary>
        public static bool HasPointer => Point.controls.Count > 0;
        public static Vector2 PointerPosition => Point.ReadValue<Vector2>();

        static InputAction Get(string name) => InputSystem.actions.FindAction(name, true);
    }
}
