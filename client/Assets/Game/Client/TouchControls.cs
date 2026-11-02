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
    /// Logic cử chỉ nằm ở <see cref="TouchGestures"/> (Game.Core, test EditMode).
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public class TouchControls : MonoBehaviour
    {
        public const int JumpButtonId = 1;

        public static bool ForceOn;
        public static bool WantsTouch => Application.isMobilePlatform || ForceOn;
        public static bool Active => instance != null && instance.isActiveAndEnabled && WantsTouch;

        static TouchControls instance;
        static Sprite discSprite;

        /// <summary>Test đặt false rồi tự gọi <see cref="Feed"/>.</summary>
        public bool readTouches = true;

        public TouchGestures Gestures { get; private set; }
        public RectTransform JumpButton { get; private set; }

        readonly List<TouchSample> samples = new List<TouchSample>(8);
        readonly List<TouchRect> rects = new List<TouchRect>(1);
        bool wasActive;
        GameObject canvasGo;
        RectTransform joyBase, joyKnob;
        Image jumpImage;
        Text jumpText;
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
                if (wasActive) ClearOutputs();
                else if (canvasGo != null && canvasGo.activeSelf) canvasGo.SetActive(false);
                return;
            }
            wasActive = true;
            if (!readTouches) return;

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

            float d = TouchGestures.DpToPx(PlayerMotion.TouchButtonDp, dpi);
            Rect s = Screen.safeArea;
            var center = new Vector2(s.xMax - 2.4f * d, s.yMin + 0.9f * d);
            JumpButton.anchoredPosition = center;
            JumpButton.sizeDelta = new Vector2(d, d);
            jumpText.fontSize = Mathf.Max(1, Mathf.RoundToInt(0.3f * d));
            rects.Clear();
            rects.Add(new TouchRect(JumpButtonId, center.x - d * 0.5f, center.y - d * 0.5f, d, d));

            Gestures.Update(input, screenWidth, dpi, rects);

            var p = PlayerCharacter.Active;
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
            jumpImage.color = new Color(1f, 1f, 1f, Gestures.IsHeld(JumpButtonId) ? 0.6f : 0.35f);
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
            JumpButton = NewDisc("Jump Button", root, 0.35f);
            jumpImage = JumpButton.GetComponent<Image>();
            var label = new GameObject("Label", typeof(RectTransform));
            label.transform.SetParent(JumpButton, false);
            var lrt = (RectTransform)label.transform;
            lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one; lrt.offsetMin = lrt.offsetMax = Vector2.zero;
            jumpText = label.AddComponent<Text>();
            jumpText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            jumpText.alignment = TextAnchor.MiddleCenter;
            jumpText.color = Color.white;
            jumpText.raycastTarget = false;
            jumpText.text = GameSettings.Text.Get("touch.jump");
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
