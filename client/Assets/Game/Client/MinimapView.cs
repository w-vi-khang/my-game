using System.Collections.Generic;
using System.Diagnostics;
using Game.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;

namespace Game.Client
{
    /// <summary>
    /// Bản đồ của world nhiều map (world-tech bước 7c, brief 0016; lõi ở brief 0015).
    /// Nút "Bản đồ (M)" góc trên-phải hoặc phím M mở/đóng, Esc đóng. Khi mở: bảng phủ màn hình — ảnh nền tầng độ cao, ranh giới, tên map,
    /// dấu vị trí + hướng camera; lăn chuột phóng quanh con trỏ, giữ chuột trái kéo để di chuyển; cảm ứng: 1 ngón kéo, 2 ngón chụm.
    /// Canvas dựng bằng code lúc chạy, không EventSystem (tự kiểm con trỏ). World không có bảng map (đảo v2) → không làm gì.
    /// </summary>
    [RequireComponent(typeof(TerrainChunkView))]
    public class MinimapView : MonoBehaviour
    {
        /// <summary>Bản đồ đang mở — TerrainEditInput, DebugFlyCamera, TerrainBench bỏ chuột/HUD khi true.</summary>
        public static bool IsAnyOpen => active != null && active.IsOpen;

        /// <summary>Điểm màn hình (pixel) thuộc về bản đồ: bản đồ đang mở, hoặc nằm trên nút mở/đóng.</summary>
        public static bool BlocksPointer(Vector2 screen) =>
            active != null && active.IsReady && (active.IsOpen || active.IsOverButton(screen));

        static MinimapView active;

        public float zoomStep = 1.15f;
        public float borderThickness = 3f;
        public Color borderColor = new Color32(30, 30, 30, 235);
        public Color markerColor = new Color32(235, 60, 50, 255);
        public int labelFontSize = 28;

        TerrainChunkView view;
        GameObject panel;
        RectTransform frame, button, marker;
        Text buttonText;
        RawImage image;
        MinimapLinesGraphic lines;
        readonly List<Text> labels = new List<Text>();
        float[] labelWorld;
        bool mouseDragging;
        Vector2 lastMouse;
        int toggledFrame = -1;
        readonly Dictionary<int, Vector2> touches = new Dictionary<int, Vector2>();
        readonly Dictionary<int, Vector2> lastTouches = new Dictionary<int, Vector2>();
        readonly HashSet<int> buttonTouches = new HashSet<int>();
        readonly List<int> common = new List<int>();
        float lastZoom = -1f, lastCx, lastCz, lastW, lastH;

        public bool IsReady { get; private set; }
        public bool IsOpen { get; private set; }
        public Texture2D Texture { get; private set; }
        public MinimapViewport Viewport { get; private set; }
        public int BorderCount { get; private set; }
        public double BuildMs { get; private set; }
        public bool PanelActive => panel != null && panel.activeSelf;
        public Rect UvRect => image != null ? image.uvRect : default;
        public int BorderVertexCount => lines != null ? lines.LastVertexCount : 0;
        public int LabelCount => labels.Count;
        public string LabelText(int i) => labels[i].text;
        public bool LabelVisible(int i) => labels[i].enabled;
        public Vector2 LabelPanelPosition(int i) => labels[i].rectTransform.anchoredPosition;
        public Vector2 MarkerPanelPosition => marker != null ? marker.anchoredPosition : default;
        /// <summary>Góc z của dấu người chơi (độ, 0 = nhìn theo +z world, 270 = nhìn theo +x).</summary>
        public float MarkerAngle => marker != null ? marker.localEulerAngles.z : 0f;
        /// <summary>Tâm nút mở/đóng theo pixel màn hình.</summary>
        public Vector2 ButtonScreenCenter => RectTransformUtility.WorldToScreenPoint(null, button.TransformPoint(button.rect.center));

        void Awake() => view = GetComponent<TerrainChunkView>();
        void OnEnable() => active = this;
        void OnDisable() { if (active == this) active = null; }
        void OnDestroy() { if (Texture != null) Destroy(Texture); }

        void Update()
        {
            if (!IsReady)
            {
                if (view.World == null || view.Maps == null)
                    return;
                Build();
            }
            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.mKey.wasPressedThisFrame) Toggle();
                else if (IsOpen && kb.escapeKey.wasPressedThisFrame) SetOpen(false);
            }
            HandleMouse(Mouse.current);
            HandleTouches(Touchscreen.current);
            if (IsOpen)
                Refresh();
        }

        public void SetOpen(bool open)
        {
            if (!IsReady)
                return;
            IsOpen = open;
            panel.SetActive(open);
            buttonText.text = open ? "Đóng (M)" : "Bản đồ (M)";
            mouseDragging = false;
            lastZoom = -1f; // buộc cập nhật uv, ranh giới, tên khi mở
            if (open)
                Refresh();
        }

        void Toggle()
        {
            if (toggledFrame == Time.frameCount)
                return;
            toggledFrame = Time.frameCount;
            SetOpen(!IsOpen);
        }

        /// <summary>Pixel màn hình → toạ độ khung của Viewport (khung pivot (0, 0) nên toạ độ cục bộ = toạ độ khung).</summary>
        public Vector2 ScreenToPanel(Vector2 screen)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(frame, screen, null, out var local);
            return local;
        }

        public bool IsOverButton(Vector2 screen)
        {
            if (!IsReady)
                return false;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(button, screen, null, out var local);
            return button.rect.Contains(local);
        }

        bool IsOverFrame(Vector2 screen)
        {
            var p = ScreenToPanel(screen);
            return p.x >= 0f && p.y >= 0f && p.x <= frame.rect.width && p.y <= frame.rect.height;
        }

        public void PanScreen(Vector2 from, Vector2 to)
        {
            var a = ScreenToPanel(from);
            var b = ScreenToPanel(to);
            Viewport.Pan(b.x - a.x, b.y - a.y);
        }

        public void ZoomAtScreen(Vector2 screen, float factor)
        {
            var p = ScreenToPanel(screen);
            Viewport.ZoomAt(p.x, p.y, factor);
        }

        public void PinchScreen(Vector2 a0, Vector2 b0, Vector2 a1, Vector2 b1)
        {
            Vector2 pa0 = ScreenToPanel(a0), pb0 = ScreenToPanel(b0), pa1 = ScreenToPanel(a1), pb1 = ScreenToPanel(b1);
            Viewport.Pinch(pa0.x, pa0.y, pb0.x, pb0.y, pa1.x, pa1.y, pb1.x, pb1.y);
        }

        void HandleMouse(Mouse mouse)
        {
            if (mouse == null)
                return;
            var pos = mouse.position.ReadValue();
            if (mouse.leftButton.wasPressedThisFrame && IsOverButton(pos))
            {
                Toggle();
                return;
            }
            if (!IsOpen)
                return;
            if (mouse.leftButton.wasPressedThisFrame && IsOverFrame(pos))
            {
                mouseDragging = true;
                lastMouse = pos;
            }
            if (!mouse.leftButton.isPressed)
                mouseDragging = false;
            if (mouseDragging && pos != lastMouse)
            {
                PanScreen(lastMouse, pos);
                lastMouse = pos;
            }
            float wheel = mouse.scroll.ReadValue().y;
            if ((wheel > 0.01f || wheel < -0.01f) && IsOverFrame(pos))
                ZoomAtScreen(pos, wheel > 0f ? zoomStep : 1f / zoomStep);
        }

        void HandleTouches(Touchscreen ts)
        {
            touches.Clear();
            if (ts != null)
                foreach (var t in ts.touches)
                    if (t.press.isPressed)
                        touches[t.touchId.ReadValue()] = t.position.ReadValue();

            // Ngón mới chạm lên nút → mở/đóng; ngón đó không dùng để kéo/chụm.
            foreach (var kv in touches)
                if (!lastTouches.ContainsKey(kv.Key) && IsOverButton(kv.Value))
                {
                    buttonTouches.Add(kv.Key);
                    Toggle();
                }
            buttonTouches.RemoveWhere(id => !touches.ContainsKey(id));

            if (IsOpen)
            {
                common.Clear();
                foreach (var kv in touches)
                    if (lastTouches.ContainsKey(kv.Key) && !buttonTouches.Contains(kv.Key))
                        common.Add(kv.Key);
                common.Sort();
                if (common.Count == 1)
                    PanScreen(lastTouches[common[0]], touches[common[0]]);
                else if (common.Count >= 2)
                    PinchScreen(lastTouches[common[0]], lastTouches[common[1]], touches[common[0]], touches[common[1]]);
            }

            lastTouches.Clear();
            foreach (var kv in touches)
                lastTouches[kv.Key] = kv.Value;
        }

        void Refresh()
        {
            float w = Mathf.Max(1f, frame.rect.width), h = Mathf.Max(1f, frame.rect.height);
            if (w != Viewport.PanelWidth || h != Viewport.PanelHeight)
                Viewport.SetPanelSize(w, h);
            if (Viewport.Zoom != lastZoom || Viewport.CenterX != lastCx || Viewport.CenterZ != lastCz || w != lastW || h != lastH)
            {
                lastZoom = Viewport.Zoom;
                lastCx = Viewport.CenterX;
                lastCz = Viewport.CenterZ;
                lastW = w;
                lastH = h;
                Viewport.VisibleUv(out float u, out float v, out float uw, out float vh);
                image.uvRect = new Rect(u, v, uw, vh);
                lines.SetVerticesDirty();
                for (int m = 0; m < labels.Count; m++)
                {
                    float lx = labelWorld[2 * m], lz = labelWorld[2 * m + 1];
                    bool has = !float.IsNaN(lx);
                    labels[m].enabled = has;
                    if (!has)
                        continue;
                    Viewport.WorldToPanel(lx, lz, out float px, out float py);
                    labels[m].rectTransform.anchoredPosition = new Vector2(px, py);
                }
            }

            var cam = Camera.main;
            marker.gameObject.SetActive(cam != null);
            if (cam == null)
                return;
            var p = view.transform.InverseTransformPoint(cam.transform.position);
            Viewport.WorldToPanel(p.x, p.z, out float mx, out float my);
            marker.anchoredPosition = new Vector2(mx, my);
            var f = view.transform.InverseTransformDirection(cam.transform.forward);
            if (f.x * f.x + f.z * f.z > 1e-6f)
                marker.localEulerAngles = new Vector3(0f, 0f, -Mathf.Atan2(f.x, f.z) * Mathf.Rad2Deg);
        }

        void Build()
        {
            var sw = Stopwatch.StartNew();
            var s = view.World.Size;
            var rgba = MinimapImage.Build(view.World);
            var px = new Color32[s.X * s.Z];
            for (int i = 0; i < px.Length; i++)
                px[i] = new Color32(rgba[i * 4], rgba[i * 4 + 1], rgba[i * 4 + 2], rgba[i * 4 + 3]);
            Texture = new Texture2D(s.X, s.Z, TextureFormat.RGBA32, false)
            {
                name = "Minimap",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
            };
            Texture.SetPixels32(px);
            Texture.Apply(false);
            var borders = MinimapOverlay.Borders(view.Maps);
            BorderCount = borders.Count;
            labelWorld = MinimapOverlay.LabelPositions(view.Maps);

            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var canvasGo = new GameObject("Bản đồ", typeof(RectTransform));
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 1f;
            var root = (RectTransform)canvasGo.transform;

            // Bảng phủ (ẩn khi đóng): nền tối + dòng hướng dẫn + khung bản đồ.
            var panelRt = Stretch("Bảng bản đồ", root, Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f));
            panel = panelRt.gameObject;
            AddImage(panelRt, new Color(0f, 0f, 0f, 0.6f));
            var hint = NewText("Hướng dẫn", panelRt, font, 22, TextAnchor.MiddleLeft);
            Place(hint.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(16f, -16f), new Vector2(900f, 56f));
            hint.text = "Lăn chuột / chụm 2 ngón: phóng to · kéo: di chuyển · M / Esc / nút Đóng: đóng";

            frame = Stretch("Khung", panelRt, new Vector2(16f, 16f), new Vector2(-16f, -88f), Vector2.zero);
            frame.gameObject.AddComponent<RectMask2D>();
            image = Stretch("Ảnh nền", frame, Vector2.zero, Vector2.zero, Vector2.zero).gameObject.AddComponent<RawImage>();
            image.texture = Texture;
            image.raycastTarget = false;
            lines = Stretch("Ranh giới", frame, Vector2.zero, Vector2.zero, Vector2.zero).gameObject.AddComponent<MinimapLinesGraphic>();
            lines.Lines = borders;
            lines.Thickness = borderThickness;
            lines.color = borderColor;
            lines.raycastTarget = false;
            for (int m = 0; m < view.Maps.MapCount; m++)
            {
                var t = NewText("Tên map " + m, frame, font, labelFontSize, TextAnchor.MiddleCenter);
                Place(t.rectTransform, Vector2.zero, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(400f, 60f));
                t.text = view.Maps.Name(m);
                var outline = t.gameObject.AddComponent<Outline>();
                outline.effectColor = new Color(0f, 0f, 0f, 0.85f);
                outline.effectDistance = new Vector2(2f, -2f);
                labels.Add(t);
            }
            marker = Stretch("Người chơi", frame, Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f));
            Place(marker, Vector2.zero, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(26f, 26f));
            var mg = marker.gameObject.AddComponent<MinimapMarkerGraphic>();
            mg.color = markerColor;
            mg.raycastTarget = false;

            // Nút mở/đóng: luôn hiện, tạo sau bảng → vẽ đè lên bảng.
            button = Stretch("Nút bản đồ", root, Vector2.zero, Vector2.zero, new Vector2(1f, 1f));
            Place(button, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-16f, -16f), new Vector2(180f, 56f));
            AddImage(button, new Color(0.1f, 0.1f, 0.1f, 0.75f));
            buttonText = NewText("Chữ", button, font, 24, TextAnchor.MiddleCenter);

            Canvas.ForceUpdateCanvases();
            Viewport = new MinimapViewport(s.X, s.Z, Mathf.Max(1f, frame.rect.width), Mathf.Max(1f, frame.rect.height));
            lines.Viewport = Viewport;
            IsReady = true;
            SetOpen(false);
            sw.Stop();
            BuildMs = sw.Elapsed.TotalMilliseconds;
            Debug.Log($"MINIMAP dựng: ảnh {s.X}x{s.Z}, {BorderCount} đường ranh giới, {labels.Count} tên, font={(font != null ? font.name : "null")}, khung {frame.rect.width:F0}x{frame.rect.height:F0}, {BuildMs:F1} ms");
        }

        /// <summary>RectTransform con giãn theo cha (anchor 0..1) với lề offsetMin/offsetMax.</summary>
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

        /// <summary>Đặt cỡ cố định: anchor một điểm, pivot, vị trí, kích thước.</summary>
        static void Place(RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
        {
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.sizeDelta = size;
            rt.anchoredPosition = position;
        }

        static void AddImage(RectTransform rt, Color c)
        {
            var img = rt.gameObject.AddComponent<Image>();
            img.color = c;
            img.raycastTarget = false;
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
