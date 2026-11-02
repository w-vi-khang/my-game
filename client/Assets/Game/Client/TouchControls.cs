using System.Collections.Generic;
using Game.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.UI;
using ETouch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using ETouchPhase = UnityEngine.InputSystem.TouchPhase;

namespace Game.Client
{
    /// <summary>
    /// Điều khiển cảm ứng (task cảm ứng (a), brief 0101): joystick ảo nửa trái, vuốt nửa phải xoay camera, chụm 2 ngón để zoom, nút Nhảy.
    /// Đọc chạm bằng EnhancedTouch, không qua action (ngoại lệ của 0033): joystick và vuốt camera chạy cùng lúc bằng hai ngón,
    /// phải theo dõi riêng từng ngón theo touchId; action &lt;Touchscreen&gt; gộp ngón nên không tách được.
    /// Tự bật trên điện thoại; F4 bật/tắt (mọi build), trong Editor kèm TouchSimulation để thử bằng chuột.
    /// (b) brief 0102: nút chính (Làm) / chế độ / Thu / Túi / Huỷ, chấm tâm nhắm giữa màn hình, ngón UI cho bảng túi/kho/túi phụ;
    /// thứ tự thực thi −60 (chạy trước SettingsView để cờ Huỷ có hiệu lực cùng khung).
    /// Logic cử chỉ nằm ở <see cref="TouchGestures"/> (Game.Core, test EditMode).
    /// </summary>
    [DefaultExecutionOrder(-60)]
    public class TouchControls : MonoBehaviour
    {
        public const int JumpButtonId = 1;
        public const int ActionButtonId = 2;
        public const int CollectButtonId = 3;
        /// <summary>Id nút chế độ = ModeButtonId0 + (int)ControlMode.</summary>
        public const int ModeButtonId0 = 4;
        public const int BagButtonId = 8;
        public const int BackButtonId = 9;

        sealed class Btn
        {
            public int Id;
            public RectTransform Rt;
            public Image Img;
            public Text Label;
            public string Key;
            public Rect Rect;
        }

        public static bool ForceOn;
        public static bool WantsTouch => Application.isMobilePlatform || ForceOn;
        public static bool Active => instance != null && instance.isActiveAndEnabled && WantsTouch;

        static TouchControls instance;
        static Sprite discSprite;

        /// <summary>Test đặt false rồi tự gọi <see cref="Feed"/>.</summary>
        public bool readTouches = true;

        public TouchGestures Gestures { get; private set; }
        public RectTransform JumpButton { get; private set; }
        public RectTransform Crosshair { get; private set; }

        readonly List<TouchSample> samples = new List<TouchSample>(8);
        readonly List<TouchRect> rects = new List<TouchRect>(9);
        readonly List<Btn> buttons = new List<Btn>(9);
        bool backRequested;
        int uiTouchId = -1;
        Vector2 uiLast;
        bool wasActive;
        GameObject canvasGo;
        RectTransform joyBase, joyKnob;
        FollowCamera follow;

        public static void SetForceOn(bool on)
        {
            ForceOn = on;
#if UNITY_EDITOR
            if (on) TouchSimulation.Enable(); else TouchSimulation.Disable();
#endif
            if (instance != null)
            {
                if (!WantsTouch) instance.ClearOutputs();
                var settings = instance.GetComponent<SettingsView>();
                if (settings != null) { settings.ApplyToScene(); return; }
            }
            var cam = Camera.main;
            var f = cam != null ? cam.GetComponent<FollowCamera>() : null;
            if (f != null) f.ApplyDefaults(WantsTouch ? PlayerMotion.TouchCameraPitch : PlayerMotion.CameraPitch, PlayerMotion.CameraDistance);
        }

        void Awake()
        {
            Gestures = new TouchGestures(PlayerMotion.TouchJoystickRadiusDp, PlayerMotion.TouchJoystickDeadZone, PlayerMotion.TouchLookDegPerDp);
        }

        void OnEnable()
        {
            instance = this;
            EnhancedTouchSupport.Enable();
        }

        void OnDisable()
        {
            EnhancedTouchSupport.Disable();
            ClearOutputs();
            if (instance == this) instance = null;
        }

        void ClearOutputs()
        {
            Gestures.Reset();
            backRequested = false;
            if (uiTouchId >= 0) { InventoryView.Active?.CancelDrag(); uiTouchId = -1; }
            var p = PlayerCharacter.Active;
            if (p != null) { p.MoveInput = Vector2.zero; p.TouchJump = false; }
            if (canvasGo != null) canvasGo.SetActive(false);
            wasActive = false;
        }

        void Update()
        {
            if (DebugKeys.WasPressed(Key.F4)) SetForceOn(!ForceOn);
            if (!Active)
            {
                backRequested = false;
                if (wasActive) ClearOutputs();
                else if (canvasGo != null && canvasGo.activeSelf) canvasGo.SetActive(false);
                return;
            }
            wasActive = true;
            if (readTouches) ReadAndFeed();
            if (backRequested) { backRequested = false; GameInput.TouchBackFrame = Time.frameCount; }
        }

        void ReadAndFeed()
        {
            samples.Clear();
            var touches = ETouch.activeTouches;
            for (int i = 0; i < touches.Count; i++)
            {
                var t = touches[i];
                TouchSamplePhase ph;
                switch (t.phase)
                {
                    case ETouchPhase.Began: ph = TouchSamplePhase.Began; break;
                    case ETouchPhase.Moved: ph = TouchSamplePhase.Moved; break;
                    case ETouchPhase.Stationary: ph = TouchSamplePhase.Stationary; break;
                    case ETouchPhase.Ended:
                    case ETouchPhase.Canceled: ph = TouchSamplePhase.Ended; break;
                    default: continue;
                }
                Vector2 pos = t.screenPosition;
                bool overUi = MinimapView.BlocksPointer(pos) || TimeDial.BlocksPointer(pos) || SimView.BlocksPointer(pos)
                              || InventoryView.BlocksPointer(pos) || ObservationCamera.BlocksPointer(pos) || SettingsView.IsAnyOpen;
                samples.Add(new TouchSample(t.touchId, pos.x, pos.y, ph, overUi));
            }
            Feed(samples, Screen.width, Screen.dpi);
        }

        public void Feed(IReadOnlyList<TouchSample> input, float screenWidth, float dpi)
        {
            if (!Active) return;
            if (canvasGo == null) BuildCanvas();
            canvasGo.SetActive(true);

            LayoutButtons(dpi);
            rects.Clear();
            foreach (var b in buttons) rects.Add(new TouchRect(b.Id, b.Rect.x, b.Rect.y, b.Rect.width, b.Rect.height));

            Gestures.Update(input, screenWidth, dpi, rects);
            RouteUiTouch(input);

            var edit = TerrainEditInput.Active;
            var p = PlayerCharacter.Active;
            bool observing = edit != null && edit.Observation != null && edit.Observation.Engaged;
            if (Gestures.WasPressed(ActionButtonId)) edit?.TouchAction();
            if (Gestures.WasPressed(CollectButtonId) && edit != null && !observing) edit.CollectNearby();
            for (int k = 0; k < 4; k++)
                if (Gestures.WasPressed(ModeButtonId0 + k) && edit != null && p != null && !p.FlyMode) edit.ToggleMode((ControlMode)k);
            if (Gestures.WasPressed(BagButtonId)) InventoryView.Active?.ToggleBag();
            if (Gestures.WasPressed(BackButtonId)) backRequested = true;

            if (p != null)
            {
                p.MoveInput = new Vector2(Gestures.MoveX, Gestures.MoveY);
                p.TouchJump = Gestures.IsHeld(JumpButtonId);
            }

            if (follow == null)
            {
                var cam = Camera.main;
                if (cam != null) follow = cam.GetComponent<FollowCamera>();
            }
            if (follow != null && !MinimapView.IsAnyOpen)
            {
                if (Gestures.LookYaw != 0f || Gestures.LookPitch != 0f) follow.ApplyLookDegrees(Gestures.LookYaw, Gestures.LookPitch);
                if (Gestures.ZoomFactor != 1f) follow.ApplyZoomFactor(Gestures.ZoomFactor);
            }

            bool joy = Gestures.JoystickActive;
            joyBase.gameObject.SetActive(joy);
            joyKnob.gameObject.SetActive(joy);
            if (joy)
            {
                float r = Gestures.JoystickRadiusPx;
                joyBase.anchoredPosition = new Vector2(Gestures.JoystickCenterX, Gestures.JoystickCenterY);
                joyBase.sizeDelta = new Vector2(2f * r, 2f * r);
                joyKnob.anchoredPosition = new Vector2(Gestures.KnobX, Gestures.KnobY);
                joyKnob.sizeDelta = new Vector2(1.6f * r, 1.6f * r);
            }
            var mode = edit != null ? edit.Control.Mode : ControlMode.Default;
            foreach (var b in buttons)
            {
                bool on = Gestures.IsHeld(b.Id) || (b.Id >= ModeButtonId0 && b.Id < ModeButtonId0 + 4 && (int)mode == b.Id - ModeButtonId0);
                b.Img.color = new Color(1f, 1f, 1f, on ? 0.6f : 0.35f);
            }
            bool panel = InventoryView.AnyOpen || SimView.AnyPanelOpen || MinimapView.IsAnyOpen || SettingsView.IsAnyOpen
                         || TimeDial.IsAnyOpen || HabitatPanelView.IsAnyOpen || observing;
            Crosshair.gameObject.SetActive(!panel);
            float c = TouchGestures.DpToPx(PlayerMotion.TouchCrosshairDp, dpi);
            Crosshair.anchoredPosition = TerrainEditInput.AimScreenPoint;
            Crosshair.sizeDelta = new Vector2(c, c);
        }

        public Rect ButtonRect(int id)
        {
            foreach (var b in buttons) if (b.Id == id) return b.Rect;
            return Rect.zero;
        }

        void Place(int id, float cx, float cy, float diam, float fontScale, float dpi)
        {
            foreach (var b in buttons)
            {
                if (b.Id != id) continue;
                b.Rt.anchoredPosition = new Vector2(cx, cy);
                b.Rt.sizeDelta = new Vector2(diam, diam);
                b.Label.fontSize = Mathf.Max(1, Mathf.RoundToInt(fontScale * diam));
                b.Rect = new Rect(cx - diam * 0.5f, cy - diam * 0.5f, diam, diam);
                return;
            }
        }

        void LayoutButtons(float dpi)
        {
            float d = TouchGestures.DpToPx(PlayerMotion.TouchButtonDp, dpi);
            float a = TouchGestures.DpToPx(PlayerMotion.TouchActionButtonDp, dpi);
            float m = TouchGestures.DpToPx(PlayerMotion.TouchModeButtonDp, dpi);
            Rect s = Screen.safeArea;
            float ax = s.xMax - 0.15f * d - a * 0.5f, ay = s.yMin + 0.15f * d + a * 0.5f;
            Place(ActionButtonId, ax, ay, a, 0.3f, dpi);
            Place(CollectButtonId, ax, ay + a * 0.5f + 0.2f * d + d * 0.5f, d, 0.3f, dpi);
            float jx = ax - a * 0.5f - 0.9f * d, jy = s.yMin + d;
            Place(JumpButtonId, jx, jy, d, 0.3f, dpi);
            for (int i = 0; i < 4; i++)
                Place(ModeButtonId0 + (3 - i), jx, jy + d * 0.5f + 0.2f * d + m * 0.5f + i * 1.1f * m, m, 0.22f, dpi);
            float bx = Mathf.Max(s.xMin + 0.15f * d + d * 0.5f, 440f * UiScale.Gui + 0.15f * d + d * 0.5f);
            float by = s.yMax - 0.15f * d - d * 0.5f;
            Place(BagButtonId, bx, by, d, 0.3f, dpi);
            Place(BackButtonId, bx + 1.15f * d, by, d, 0.3f, dpi);
        }

        /// <summary>Một ngón UI mỗi lúc: Began trên bảng túi (ngoài ô nút) → PointerDown; Moved → PointerMove; nhả → TouchUp.</summary>
        void RouteUiTouch(IReadOnlyList<TouchSample> input)
        {
            var inv = InventoryView.Active;
            if (inv == null) { uiTouchId = -1; return; }
            if (uiTouchId >= 0)
            {
                int idx = -1;
                for (int i = 0; i < input.Count; i++) if (input[i].Id == uiTouchId) { idx = i; break; }
                if (idx >= 0)
                {
                    uiLast = new Vector2(input[idx].X, input[idx].Y);
                    if (input[idx].Phase != TouchSamplePhase.Ended) { inv.PointerMove(uiLast); return; }
                }
                inv.TouchUp(uiLast);
                uiTouchId = -1;
                return;
            }
            for (int i = 0; i < input.Count; i++)
            {
                var s = input[i];
                if (s.Phase != TouchSamplePhase.Began || !s.OverUi) continue;
                bool inButton = false;
                foreach (var r in rects) if (r.Contains(s.X, s.Y)) { inButton = true; break; }
                if (inButton) continue;
                var pos = new Vector2(s.X, s.Y);
                if (inv.PointerDown(pos)) { uiTouchId = s.Id; uiLast = pos; return; }
            }
        }

        void BuildCanvas()
        {
            canvasGo = new GameObject("Touch Controls", typeof(RectTransform));
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 8;
            var root = (RectTransform)canvasGo.transform;
            joyBase = NewDisc("Joystick Base", root, 0.25f);
            joyKnob = NewDisc("Joystick Knob", root, 0.5f);
            joyBase.gameObject.SetActive(false);
            joyKnob.gameObject.SetActive(false);
            AddButton(root, JumpButtonId, "Jump Button", "touch.jump");
            AddButton(root, ActionButtonId, "Action Button", "touch.action");
            AddButton(root, CollectButtonId, "Collect Button", "controls.collect");
            AddButton(root, ModeButtonId0 + (int)ControlMode.Default, "Mode Default Button", "touch.mode_default");
            AddButton(root, ModeButtonId0 + (int)ControlMode.Dig, "Mode Dig Button", "touch.mode_dig");
            AddButton(root, ModeButtonId0 + (int)ControlMode.Till, "Mode Till Button", "touch.mode_till");
            AddButton(root, ModeButtonId0 + (int)ControlMode.Rain, "Mode Rain Button", "touch.mode_rain");
            AddButton(root, BagButtonId, "Bag Button", "touch.bag");
            AddButton(root, BackButtonId, "Back Button", "touch.back");
            foreach (var b in buttons) if (b.Id == JumpButtonId) JumpButton = b.Rt;
            Crosshair = NewDisc("Crosshair", root, 0.7f);
        }

        void AddButton(RectTransform root, int id, string name, string key)
        {
            var rt = NewDisc(name, root, 0.35f);
            var label = new GameObject("Label", typeof(RectTransform));
            label.transform.SetParent(rt, false);
            var lrt = (RectTransform)label.transform;
            lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one; lrt.offsetMin = lrt.offsetMax = Vector2.zero;
            var text = label.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.text = GameSettings.Text.Get(key);
            buttons.Add(new Btn { Id = id, Rt = rt, Img = rt.GetComponent<Image>(), Label = text, Key = key });
        }

        static RectTransform NewDisc(string name, RectTransform parent, float alpha)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0.5f);
            var img = go.AddComponent<Image>();
            img.sprite = DiscSprite();
            img.color = new Color(1f, 1f, 1f, alpha);
            img.raycastTarget = false;
            return rt;
        }

        static Sprite DiscSprite()
        {
            if (discSprite != null) return discSprite;
            const int size = 128;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            var px = new Color32[size * size];
            float c = (size - 1) * 0.5f, r = size * 0.5f;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float dist = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c));
                    float a = Mathf.Clamp01((r - dist) / 2f);
                    px[y * size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * 255f));
                }
            tex.SetPixels32(px);
            tex.Apply();
            discSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
            return discSprite;
        }
    }
}
