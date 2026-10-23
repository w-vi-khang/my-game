using System.Collections.Generic;
using Game.Core;
using UnityEngine;

namespace Game.Client
{
    /// <summary>Bảng Cài đặt (0032, IMGUI): O / nút / Esc khi không có bảng nào mở. Áp cài đặt vào camera, FPS, cỡ chữ; ghi settings.json khi đóng.</summary>
    [DefaultExecutionOrder(-50)]
    public class SettingsView : MonoBehaviour
    {
        public static readonly string[] Keys =
        {
            "settings.button", "settings.title", "settings.camera_distance", "settings.camera_pitch", "settings.camera_default", "settings.look_sensitivity", "settings.invert_y",
            "settings.ui_scale", "settings.fps", "settings.language", "settings.lang_error", "settings.export",
            "settings.exported", "settings.lang_dir", "settings.close",
        };

        static SettingsView active;

        public static bool IsAnyOpen => active != null && active.IsOpen;

        SimView sim;
        TerrainEditInput editInput;
        GUIStyle labelStyle;
        string lastExport;

        public bool IsOpen { get; private set; }
        public SimView Sim => sim;

        public static Rect ButtonRect(float gw, float gh) => new Rect(gw - 240f, gh - 46f, 110f, 36f);
        public static Rect PanelRect(float gw, float gh) => new Rect(gw * 0.5f - 230f, gh * 0.5f - 240f, 460f, 480f);

        public static bool BlocksPointer(Vector2 screen) => active != null && active.Covers(screen);

        public bool Covers(Vector2 screen)
        {
            float s = UiScale.Gui, gw = Screen.width / s, gh = Screen.height / s;
            var g = new Vector2(screen.x / s, (Screen.height - screen.y) / s);
            if (ButtonRect(gw, gh).Contains(g)) return true;
            return IsOpen && PanelRect(gw, gh).Contains(g);
        }

        void Awake()
        {
            active = this;
            sim = GetComponent<SimView>();
            editInput = GetComponent<TerrainEditInput>();
        }

        void Start()
        {
            ApplyToScene();
            GameSettings.Changed += ApplyToScene;
        }

        void OnDestroy()
        {
            GameSettings.Changed -= ApplyToScene;
            if (active == this) active = null;
        }

        public bool OtherPanelOpen()
        {
            if (ObservationCamera.BlocksBack) return true;
            if (sim != null && (sim.BagOpen || sim.StockOpen || sim.OpenPlotId >= 0)) return true;
            if (MinimapView.IsAnyOpen || TimeDial.IsAnyOpen) return true;
            return editInput != null && editInput.Control.HasHeld;
        }

        public void HandleKeys(bool oPressed, bool escPressed)
        {
            if (oPressed) SetOpen(!IsOpen);
            else if (escPressed)
            {
                if (IsOpen) SetOpen(false);
                else if (!OtherPanelOpen()) SetOpen(true);
            }
        }

        public void SetOpen(bool open)
        {
            if (!open && IsOpen && GameSettings.Dirty) GameSettings.Save();
            IsOpen = open;
        }

        void Update()
        {
            HandleKeys(GameInput.ToggleSettings.WasPressedThisFrame(), GameInput.SettingsClose.WasPressedThisFrame());
        }

        public void ApplyToScene()
        {
            var s = GameSettings.Current;
            var cam = Camera.main;
            if (cam != null)
            {
                var follow = cam.GetComponent<FollowCamera>();
                if (follow != null)
                {
                    follow.ApplyDefaults(s.cameraPitch > 0f ? s.cameraPitch : PlayerMotion.CameraPitch,
                                         s.cameraDistance > 0f ? s.cameraDistance : PlayerMotion.CameraDistance);
                    follow.lookSensitivity = s.lookSensitivity;
                    follow.invertY = s.invertY;
                }
                var fly = cam.GetComponent<DebugFlyCamera>();
                if (fly != null)
                {
                    fly.lookSensitivity = s.lookSensitivity;
                    fly.invertY = s.invertY;
                }
            }
            if (sim != null) sim.SetFrameRate(s.targetFps);
        }

        static string T(string k) => GameSettings.Text.Get(k);
        static string Format(string k, params object[] a) => GameSettings.Text.Format(k, a);

        void OnGUI()
        {
            if (MinimapView.IsAnyOpen) return;
            float scale = UiScale.Gui;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            float gw = Screen.width / scale, gh = Screen.height / scale;
            labelStyle ??= new GUIStyle(GUI.skin.label) { richText = true };
            if (GUI.Button(ButtonRect(gw, gh), T("settings.button"))) SetOpen(!IsOpen);
            if (!IsOpen) return;

            var p = PanelRect(gw, gh);
            GUI.Box(p, "");
            float x = p.x + 16f, w = p.width - 32f, y = p.y + 8f;
            const float row = 30f;
            GUI.Label(new Rect(x, y, w, row), "<b>" + T("settings.title") + "</b>", labelStyle);
            y += row;

            var s = GameSettings.Current.Clone();
            bool changed = false;

            float effD = s.cameraDistance > 0f ? s.cameraDistance : PlayerMotion.CameraDistance;
            GUI.Label(new Rect(x, y, w * 0.55f, row), Format("settings.camera_distance", effD), labelStyle);
            float v = GUI.HorizontalSlider(new Rect(x + w * 0.58f, y + 8f, w * 0.42f, row), effD, PlayerSettings.CameraDistanceMin, PlayerSettings.CameraDistanceMax);
            if (v != effD) { s.cameraDistance = v; changed = true; }
            y += row;

            float effP = s.cameraPitch > 0f ? s.cameraPitch : PlayerMotion.CameraPitch;
            GUI.Label(new Rect(x, y, w * 0.55f, row), Format("settings.camera_pitch", effP), labelStyle);
            v = GUI.HorizontalSlider(new Rect(x + w * 0.58f, y + 8f, w * 0.42f, row), effP, PlayerSettings.CameraPitchMin, PlayerSettings.CameraPitchMax);
            if (v != effP) { s.cameraPitch = v; changed = true; }
            y += row;

            if (GUI.Button(new Rect(x, y, 200f, row - 4f), T("settings.camera_default"))) { s.cameraDistance = 0f; s.cameraPitch = 0f; changed = true; }
            y += row;

            GUI.Label(new Rect(x, y, w * 0.55f, row), Format("settings.look_sensitivity", s.lookSensitivity), labelStyle);
            v = GUI.HorizontalSlider(new Rect(x + w * 0.58f, y + 8f, w * 0.42f, row), s.lookSensitivity, PlayerSettings.LookSensitivityMin, PlayerSettings.LookSensitivityMax);
            if (v != s.lookSensitivity) { s.lookSensitivity = v; changed = true; }
            y += row;

            bool inv = GUI.Toggle(new Rect(x, y, w, row), s.invertY, T("settings.invert_y"));
            if (inv != s.invertY) { s.invertY = inv; changed = true; }
            y += row;

            GUI.Label(new Rect(x, y, w * 0.55f, row), Format("settings.ui_scale", s.uiScale), labelStyle);
            if (GUI.Button(new Rect(x + w * 0.58f, y, 40f, row - 4f), "−")) { s.uiScale -= 0.25f; changed = true; }
            if (GUI.Button(new Rect(x + w * 0.58f + 48f, y, 40f, row - 4f), "+")) { s.uiScale += 0.25f; changed = true; }
            y += row;

            GUI.Label(new Rect(x, y, w * 0.55f, row), T("settings.fps"), labelStyle);
            foreach (int fps in new[] { 30, 60 })
            {
                float bx = x + w * 0.58f + (fps == 30 ? 0f : 48f);
                if (GUI.Button(new Rect(bx, y, 40f, row - 4f), Bold(fps.ToString(), s.targetFps == fps), labelBtn()) && s.targetFps != fps)
                { s.targetFps = fps; changed = true; }
            }
            y += row;

            GUI.Label(new Rect(x, y, w * 0.3f, row), T("settings.language"), labelStyle);
            float lx = x + w * 0.3f;
            foreach (string id in GameSettings.Languages())
            {
                if (GUI.Button(new Rect(lx, y, 56f, row - 4f), Bold(id, s.language == id), labelBtn()) && s.language != id)
                { s.language = id; changed = true; }
                lx += 60f;
            }
            y += row;
            if (GameSettings.LanguageError != null)
            {
                GUI.Label(new Rect(x, y, w, row), "<color=red>" + Format("settings.lang_error", GameSettings.LanguageError) + "</color>", labelStyle);
                y += row;
            }

            if (GUI.Button(new Rect(x, y, 160f, row - 4f), T("settings.export"))) lastExport = GameSettings.ExportBase();
            y += row;
            if (lastExport != null)
            {
                GUI.Label(new Rect(x, y, w, 48f), Format("settings.exported", lastExport), WrapStyle());
                y += 48f;
            }
            GUI.Label(new Rect(x, y, w, 48f), Format("settings.lang_dir", GameSettings.LangDir), WrapStyle());
            y += 48f;
            if (GUI.Button(new Rect(x, p.yMax - 40f, 140f, 30f), T("settings.close"))) SetOpen(false);

            if (changed) GameSettings.Apply(s);
        }

        GUIStyle wrapStyle, btnStyle;
        GUIStyle WrapStyle() => wrapStyle ??= new GUIStyle(GUI.skin.label) { richText = true, wordWrap = true };
        GUIStyle labelBtn() => btnStyle ??= new GUIStyle(GUI.skin.button) { richText = true };
        static string Bold(string t, bool on) => on ? "<b>" + t + "</b>" : t;
    }
}
