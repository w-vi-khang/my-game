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
        public static InputAction SelectBlock1 => Get("Player/SelectBlock1");
        public static InputAction SelectBlock2 => Get("Player/SelectBlock2");
        public static InputAction SelectBlock3 => Get("Player/SelectBlock3");
        public static InputAction SelectBlock4 => Get("Player/SelectBlock4");
        public static InputAction Collect => Get("Player/Collect");
        public static InputAction ModeDefault => Get("Player/ModeDefault");
        public static InputAction ModeDig => Get("Player/ModeDig");
        public static InputAction ModeTill => Get("Player/ModeTill");
        public static InputAction ModeRain => Get("Player/ModeRain");
        public static InputAction ToggleBag => Get("Player/ToggleBag");
        public static InputAction ToggleStock => Get("Player/ToggleStock");
        public static InputAction ToggleMap => Get("Player/ToggleMap");
        public static InputAction ToggleTimeDial => Get("Player/ToggleTimeDial");
        public static InputAction ToggleSettings => Get("Player/ToggleSettings");
        public static InputAction RotateCamera => Get("Player/RotateCamera");
        public static InputAction Look => Get("Player/Look");
        public static InputAction Zoom => Get("Player/Zoom");
        public static InputAction CycleBlockModifier => Get("Player/CycleBlockModifier");
        public static InputAction ToggleObservation => Get("Player/ToggleObservation");
        public static InputAction CameraReset => Get("Player/CameraReset");
        public static InputAction ToggleFirstPerson => Get("Player/ToggleFirstPerson");
        public static InputAction Point => Get("Pointer/Point");
        public static InputAction PanelsClose => Get("Panels/Close");
        public static InputAction TransferAll => Get("Panels/TransferAll");
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
