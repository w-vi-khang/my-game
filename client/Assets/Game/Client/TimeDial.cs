using System.Collections.Generic;
using Game.Core.DayCycle;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Game.Client
{
    /// <summary>
    /// Vòng chọn thời điểm (0024, brief 0024): nút tròn góc trên-phải dưới ô minimap (brief 0025) (hoặc phím T) mở vòng 4 mốc theo chiều kim đồng hồ = chiều tiến của pha (trên giữa trưa, phải hoàng hôn, dưới giữa đêm, trái bình minh); bấm mốc → DayCycleView.Select, xoay được thì vòng tự đóng. Dòng "Đang ở buổi: …" luôn hiện. Canvas dựng bằng code, không EventSystem; ẩn khi bản đồ mở.
    /// </summary>
    [RequireComponent(typeof(DayCycleView))]
    public class TimeDial : MonoBehaviour
    {
        public bool startOpen;                                  // Bench: mở vòng ngay khi dựng

        public static bool IsAnyOpen => active != null && active.IsOpen;

        public static bool BlocksPointer(Vector2 screen) =>
            active != null && active.IsReady && active.CanvasActive &&
            (active.IsOpen || active.actedFrame == Time.frameCount || active.IsOverButton(screen));

        public bool IsReady { get; private set; }
        public bool IsOpen { get; private set; }
        public int LastSelectResult { get; private set; }
        public bool CanvasActive => canvasGo != null && canvasGo.activeSelf;
        public bool RingActive => ring != null && ring.gameObject.activeSelf;
        public string PeriodText => periodText != null ? periodText.text : null;
        public TimeDialShape ButtonShape => buttonIcon.shape;
        public float ControlsAlpha => controls.alpha;
        public Vector2 ButtonScreenCenter => ScreenCenter(button);
        public Vector2 LandmarkScreenCenter(TimeLandmark m) => ScreenCenter(items[(int)m]);
        public string LandmarkLabel(TimeLandmark m) => itemLabels[(int)m].text;
        public bool IsHighlighted(TimeLandmark m) => itemBgs[(int)m].color == HighlightBg;

        static readonly Color NormalBg = new Color(0.12f, 0.12f, 0.12f, 0.85f);
        static readonly Color HighlightBg = new Color(0.30f, 0.50f, 0.85f, 0.95f);
        static readonly Color ButtonBg = new Color(0.1f, 0.1f, 0.1f, 0.75f);
        static readonly Color RingBg = new Color(0f, 0f, 0f, 0.45f);
        const float RingRadius = 100f;
        static readonly Vector2[] Offsets =
        {
            new Vector2(0f, -RingRadius),   // Midnight
            new Vector2(-RingRadius, 0f),   // Dawn
            new Vector2(0f, RingRadius),    // Noon
            new Vector2(RingRadius, 0f),    // Dusk
        };

        static TimeDial active;

        DayCycleView day;
        GameObject canvasGo;
        CanvasGroup controls;
        RectTransform button, ring;
        TimeDialGraphic buttonIcon, centerIcon;
        Text periodText, hintText;
        readonly RectTransform[] items = new RectTransform[4];
        readonly TimeDialGraphic[] itemBgs = new TimeDialGraphic[4];
        readonly Text[] itemLabels = new Text[4];
        int actedFrame = -1;
        int shownWindow = -1, shownPeriod = -1;
        readonly Dictionary<int, Vector2> touches = new Dictionary<int, Vector2>();
        readonly Dictionary<int, Vector2> lastTouches = new Dictionary<int, Vector2>();

        static string T(string k) => GameSettings.Text.Get(k);

        public static string PeriodName(DayPeriod p) => p switch
        {
            DayPeriod.Dawn => T("time.period_dawn"),
            DayPeriod.Day => T("time.period_day"),
            DayPeriod.Dusk => T("time.period_dusk"),
            DayPeriod.Night => T("time.period_night"),
            _ => p.ToString(),
        };

        public static string LandmarkName(TimeLandmark m) => m switch
        {
            TimeLandmark.Midnight => T("time.landmark_midnight"),
            TimeLandmark.Dawn => T("time.landmark_dawn"),
            TimeLandmark.Noon => T("time.landmark_noon"),
            TimeLandmark.Dusk => T("time.landmark_dusk"),
            _ => m.ToString(),
        };

        public static TimeDialShape ShapeOf(TimeLandmark m) => m switch
        {
            TimeLandmark.Midnight => TimeDialShape.Moon,
            TimeLandmark.Dawn => TimeDialShape.Dawn,
            TimeLandmark.Noon => TimeDialShape.Sun,
            _ => TimeDialShape.Dusk,
        };

        public static Color ColorOf(TimeLandmark m) => m switch
        {
            TimeLandmark.Midnight => DaySky.MoonDiscColor,
            TimeLandmark.Dawn => new Color(1f, 0.62f, 0.38f, 1f),
            TimeLandmark.Noon => new Color(1f, 0.85f, 0.3f, 1f),
            _ => new Color(0.95f, 0.45f, 0.3f, 1f),
        };

        void Awake() => day = GetComponent<DayCycleView>();
        void OnEnable() => active = this;
        void OnDisable() { if (active == this) active = null; }
        /// <summary>Gán lại chữ theo ngôn ngữ hiện tại (0032); dòng buổi vẽ lại ở Refresh kế tiếp.</summary>
        void RefreshText()
        {
            if (hintText != null) hintText.text = T("time.hint");
            for (int i = 0; i < itemLabels.Length; i++)
                if (itemLabels[i] != null) itemLabels[i].text = LandmarkName((TimeLandmark)i);
            shownPeriod = -1;
        }

        void OnDestroy()
        {
            GameSettings.Changed -= RefreshText;
            if (canvasGo != null)
                Destroy(canvasGo);
        }

        void Update()
        {
            if (!IsReady)
            {
                if (day.Day == null)
                    return;
                Build();
            }

            bool show = !MinimapView.IsAnyOpen;
            if (show != canvasGo.activeSelf)
            {
                if (!show)
                    SetOpen(false);
                canvasGo.SetActive(show);
            }
            if (!show)
            {
                lastTouches.Clear();
                return;
            }

            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.tKey.wasPressedThisFrame && actedFrame != Time.frameCount)
                {
                    actedFrame = Time.frameCount;
                    SetOpen(!IsOpen);
                }
                else if (IsOpen && kb.escapeKey.wasPressedThisFrame)
                    SetOpen(false);
            }

            var mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.wasPressedThisFrame)
                PointerDown(mouse.position.ReadValue());

            var ts = Touchscreen.current;
            touches.Clear();
            if (ts != null)
                foreach (var t in ts.touches)
                    if (t.press.isPressed)
                        touches[t.touchId.ReadValue()] = t.position.ReadValue();
            foreach (var kv in touches)
                if (!lastTouches.ContainsKey(kv.Key))
                    PointerDown(kv.Value);
            lastTouches.Clear();
            foreach (var kv in touches)
                lastTouches[kv.Key] = kv.Value;

            Refresh();
        }

        public void SetOpen(bool open)
        {
            if (!IsReady)
                return;
            IsOpen = open;
            ring.gameObject.SetActive(open);
        }

        public void PointerDown(Vector2 screen)
        {
            if (!IsReady || !CanvasActive || actedFrame == Time.frameCount)
                return;
            bool overButton = IsOverButton(screen);
            if (!overButton && !IsOpen)
                return;
            actedFrame = Time.frameCount;
            if (overButton)
            {
                SetOpen(!IsOpen);
                return;
            }
            for (int i = 0; i < 4; i++)
                if (IsOverCircle(items[i], screen))
                {
                    Choose((TimeLandmark)i);
                    return;
                }
            if (!IsOverCircle(ring, screen))
                SetOpen(false);
        }

        void Choose(TimeLandmark m)
        {
            LastSelectResult = day.Select(m);
            if (LastSelectResult > 0)
                SetOpen(false);
        }

        bool IsOverButton(Vector2 screen)
        {
            if (!IsReady)
                return false;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(button, screen, null, out var local);
            return button.rect.Contains(local);
        }

        static bool IsOverCircle(RectTransform rt, Vector2 screen)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rt, screen, null, out var local);
            return (local - rt.rect.center).magnitude <= rt.rect.width * 0.5f;
        }

        static Vector2 ScreenCenter(RectTransform rt) =>
            RectTransformUtility.WorldToScreenPoint(null, rt.TransformPoint(rt.rect.center));

        void Refresh()
        {
            int w = (int)day.Day.CurrentWindow;
            if (w != shownWindow)
            {
                var m = (TimeLandmark)w;
                buttonIcon.SetShape(ShapeOf(m));
                buttonIcon.color = ColorOf(m);
                centerIcon.SetShape(ShapeOf(m));
                centerIcon.color = ColorOf(m);
                for (int i = 0; i < 4; i++)
                    itemBgs[i].color = i == w ? HighlightBg : NormalBg;
                shownWindow = w;
            }
            int p = (int)day.Day.Period;
            if (p != shownPeriod)
            {
                periodText.text = GameSettings.Text.Format("time.current", PeriodName((DayPeriod)p));
                shownPeriod = p;
            }
            float a = day.IsRotating ? 0.4f : 1f;
            if (controls.alpha != a)
                controls.alpha = a;
        }

        void Build()
        {
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            canvasGo = new GameObject("Vòng thời điểm", typeof(RectTransform));
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 9;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 1f;
            var root = (RectTransform)canvasGo.transform;
            var half = new Vector2(0.5f, 0.5f);

            periodText = NewText("Dòng buổi", root, font, 22, TextAnchor.MiddleRight);
            Place(periodText.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 0.5f), new Vector2(-100f, -268f), new Vector2(420f, 40f));
            AddOutline(periodText, new Vector2(2f, -2f));

            var controlsRt = Stretch("Nút và vòng", root, Vector2.zero, Vector2.zero, half);
            controls = controlsRt.gameObject.AddComponent<CanvasGroup>();

            button = Stretch("Nút thời điểm", controlsRt, Vector2.zero, Vector2.zero, half);
            Place(button, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-16f, -232f), new Vector2(72f, 72f));
            AddGraphic(button, TimeDialShape.Disk, ButtonBg);
            var bi = Stretch("Biểu tượng", button, Vector2.zero, Vector2.zero, half);
            Place(bi, half, half, Vector2.zero, new Vector2(56f, 56f));
            buttonIcon = AddGraphic(bi, TimeDialShape.Disk, Color.white);

            ring = Stretch("Vòng", controlsRt, Vector2.zero, Vector2.zero, half);
            Place(ring, new Vector2(1f, 1f), half, new Vector2(-180f, -470f), new Vector2(300f, 300f));
            AddGraphic(ring, TimeDialShape.Disk, RingBg);
            var ci = Stretch("Mốc hiện tại", ring, Vector2.zero, Vector2.zero, half);
            Place(ci, half, half, Vector2.zero, new Vector2(80f, 80f));
            centerIcon = AddGraphic(ci, TimeDialShape.Disk, Color.white);

            for (int i = 0; i < 4; i++)
            {
                var m = (TimeLandmark)i;
                var item = Stretch("Mốc " + LandmarkName(m), ring, Vector2.zero, Vector2.zero, half);
                Place(item, half, half, Offsets[i], new Vector2(72f, 72f));
                items[i] = item;
                itemBgs[i] = AddGraphic(item, TimeDialShape.Disk, NormalBg);
                var icon = Stretch("Biểu tượng", item, Vector2.zero, Vector2.zero, half);
                Place(icon, half, half, Vector2.zero, new Vector2(56f, 56f));
                AddGraphic(icon, ShapeOf(m), ColorOf(m));
                var label = NewText("Tên " + LandmarkName(m), item, font, 18, TextAnchor.UpperCenter);
                Place(label.rectTransform, half, new Vector2(0.5f, 1f), new Vector2(0f, -40f), new Vector2(140f, 24f));
                AddOutline(label, new Vector2(1.5f, -1.5f));
                itemLabels[i] = label;
            }

            hintText = NewText("Gợi ý", ring, font, 18, TextAnchor.MiddleCenter);
            Place(hintText.rectTransform, half, half, new Vector2(0f, -180f), new Vector2(320f, 28f));
            AddOutline(hintText, new Vector2(1.5f, -1.5f));
            RefreshText();
            GameSettings.Changed += RefreshText;

            IsReady = true;
            SetOpen(startOpen);
            Refresh();
            Debug.Log($"TIMEDIAL dựng: 4 mốc, mở={IsOpen}, buổi={day.Day.Period}, font={(font != null ? font.name : "null")}");
        }

        static void AddOutline(Text t, Vector2 distance)
        {
            var o = t.gameObject.AddComponent<Outline>();
            o.effectColor = new Color(0f, 0f, 0f, 0.85f);
            o.effectDistance = distance;
        }

        static TimeDialGraphic AddGraphic(RectTransform rt, TimeDialShape shape, Color c)
        {
            var g = rt.gameObject.AddComponent<TimeDialGraphic>();
            g.shape = shape;
            g.color = c;
            g.raycastTarget = false;
            return g;
        }

        static RectTransform Stretch(string name, Transform parent, Vector2 offsetMin, Vector2 offsetMax, Vector2 pivot)
        {
            var rt = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
            rt.SetParent(parent, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = pivot;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;
            return rt;
        }

        static void Place(RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
        {
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.sizeDelta = size;
            rt.anchoredPosition = position;
        }

        static Text NewText(string name, Transform parent, Font font, int size, TextAnchor align)
        {
            var t = Stretch(name, parent, Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f)).gameObject.AddComponent<Text>();
            t.font = font;
            t.fontSize = size;
            t.alignment = align;
            t.color = Color.white;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            return t;
        }
    }
}
