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
    /// Ô minimap tròn góc trên-phải (brief 0025) luôn hiện vùng 64 × 64 cột quanh người chơi, Bắc lên trên, chữ N ở đỉnh; bấm/chạm ô hoặc phím M mở bản đồ lớn, nút "Đóng (M)" / M / Esc đóng. Khi mở: bảng phủ màn hình — ảnh nền tầng độ cao, ranh giới, tên map,
    /// dấu vị trí + hướng camera; lăn chuột phóng quanh con trỏ, giữ chuột trái kéo để di chuyển; cảm ứng: 1 ngón kéo, 2 ngón chụm.
    /// Canvas dựng bằng code lúc chạy, không EventSystem (tự kiểm con trỏ). World không có bảng map (đảo v2) → không làm gì.
    /// </summary>
    [RequireComponent(typeof(TerrainChunkView))]
    public class MinimapView : MonoBehaviour
    {
        /// <summary>Bản đồ đang mở — TerrainEditInput, DebugFlyCamera, TerrainBench bỏ chuột/HUD khi true.</summary>
        public static bool IsAnyOpen => active != null && active.IsOpen;

        /// <summary>Điểm màn hình (pixel) thuộc về bản đồ: bản đồ đang mở, nằm trên ô tròn / nút Đóng, hoặc khung này vừa bật/tắt (brief 0025).</summary>
        public static bool BlocksPointer(Vector2 screen) =>
            active != null && active.IsReady && (active.IsOpen || active.toggledFrame == Time.frameCount || active.IsOverButton(screen));

        static MinimapView active;

        // Ô nhỏ góc trên-phải (brief 0025), đơn vị tham chiếu 1280 × 720.
        const float CornerSize = 200f, CornerRim = 6f, CornerColumns = 64f;
        const float CornerLineThickness = 2f, CornerLandmarkSize = 14f, CornerMarkerSize = 18f;
        static readonly Color CornerRimColor = new Color(0.08f, 0.08f, 0.08f, 0.9f);
        static readonly Color CornerMaskColor = new Color(0.05f, 0.05f, 0.05f, 1f);

        public float zoomStep = 1.15f;
        public float borderThickness = 3f;
        public Color borderColor = new Color32(30, 30, 30, 235);
        public Color markerColor = new Color32(235, 60, 50, 255);
        public int labelFontSize = 28;
        public float landmarkIconSize = 22f;
        public int landmarkFontSize = 18;
        /// <summary>Tên dấu mốc chỉ hiện khi Zoom ≥ giá trị này (brief 0018).</summary>
        public float landmarkLabelZoom = 2f;

        TerrainChunkView view;
        GameObject panel;
        RectTransform frame, button, marker;
        RectTransform corner, cornerContent, cornerMarker;
        RawImage cornerImage;
        MinimapLinesGraphic cornerLines;
        Mask cornerMask;
        Text cornerName, cornerNorth, closeText, hintText;
        readonly List<RectTransform> cornerLandmarks = new List<RectTransform>();
        float cornerK;
        int cornerShownMap = -2;
        RawImage image;
        MinimapLinesGraphic lines;
        readonly List<Text> labels = new List<Text>();
        readonly List<RectTransform> landmarkIcons = new List<RectTransform>();
        readonly List<Text> landmarkLabels = new List<Text>();
        float[] labelWorld;
        bool mouseDragging;
        Vector2 lastMouse;
        int toggledFrame = -1;
        readonly Dictionary<int, Vector2> touches = new Dictionary<int, Vector2>();
        readonly Dictionary<int, Vector2> lastTouches = new Dictionary<int, Vector2>();
        readonly HashSet<int> buttonTouches = new HashSet<int>();
        readonly List<int> common = new List<int>();
        float lastZoom = -1f, lastCx, lastCz, lastW, lastH;
        byte[] rgba;
        bool pixelsDirty;

        public bool IsReady { get; private set; }
        public bool IsOpen { get; private set; }
        public Texture2D Texture { get; private set; }
        public MinimapViewport Viewport { get; private set; }
        public int BorderCount { get; private set; }
        public double BuildMs { get; private set; }
        /// <summary>Số lần một điểm ảnh đổi màu do đào/đặt/nước (brief 0017).</summary>
        public int UpdatedPixelCount { get; private set; }
        public bool PanelActive => panel != null && panel.activeSelf;
        public Rect UvRect => image != null ? image.uvRect : default;
        public int BorderVertexCount => lines != null ? lines.LastVertexCount : 0;
        public int LabelCount => labels.Count;
        public string LabelText(int i) => labels[i].text;
        public bool LabelVisible(int i) => labels[i].enabled;
        public Vector2 LabelPanelPosition(int i) => labels[i].rectTransform.anchoredPosition;
        public int LandmarkIconCount => landmarkIcons.Count;
        public Vector2 LandmarkIconPosition(int i) => landmarkIcons[i].anchoredPosition;
        public bool LandmarkIconIsDiamond(int i) => landmarkIcons[i].GetComponent<MinimapLandmarkGraphic>().diamond;
        public Color LandmarkIconColor(int i) => landmarkIcons[i].GetComponent<MinimapLandmarkGraphic>().color;
        public string LandmarkLabelText(int i) => landmarkLabels[i].text;
        public bool LandmarkLabelVisible(int i) => landmarkLabels[i].enabled;
        public Vector2 LandmarkLabelPosition(int i) => landmarkLabels[i].rectTransform.anchoredPosition;
        public Vector2 MarkerPanelPosition => marker != null ? marker.anchoredPosition : default;
        /// <summary>Góc z của dấu người chơi (độ, 0 = nhìn theo +z world, 270 = nhìn theo +x).</summary>
        public float MarkerAngle => marker != null ? marker.localEulerAngles.z : 0f;
        /// <summary>Tâm vùng bấm theo pixel màn hình: ô tròn khi đóng, nút Đóng khi mở.</summary>
        public Vector2 ButtonScreenCenter => IsOpen
            ? RectTransformUtility.WorldToScreenPoint(null, button.TransformPoint(button.rect.center))
            : CornerScreenPoint(Vector2.zero);
        public bool CornerActive => corner != null && corner.gameObject.activeSelf;
        public MinimapViewport CornerViewport { get; private set; }
        public Texture CornerTexture => cornerImage != null ? cornerImage.texture : null;
        public Rect CornerUvRect => cornerImage != null ? cornerImage.uvRect : default;
        public Vector2 CornerContentPosition => cornerContent != null ? cornerContent.anchoredPosition : default;
        public float CornerMarkerAngle => cornerMarker != null ? cornerMarker.localEulerAngles.z : 0f;
        /// <summary>Tên map ở cột dưới người chơi; null khi đang ẩn (biển / ngoài world).</summary>
        public string CornerMapName => cornerName != null && cornerName.enabled ? cornerName.text : null;
        public string CornerNorthText => cornerNorth != null ? cornerNorth.text : null;
        public bool CornerMaskIsDisk => cornerMask != null && cornerMask.enabled && cornerMask.showMaskGraphic
            && cornerMask.graphic is TimeDialGraphic g && g.shape == TimeDialShape.Disk;
        public int CornerBorderVertexCount => cornerLines != null ? cornerLines.LastVertexCount : 0;
        public int CornerLandmarkCount => cornerLandmarks.Count;
        public Vector2 CornerLandmarkPosition(int i) => cornerLandmarks[i].anchoredPosition;
        /// <summary>Điểm (local, tính từ tâm ô, đơn vị canvas) → pixel màn hình.</summary>
        public Vector2 CornerScreenPoint(Vector2 local) => RectTransformUtility.WorldToScreenPoint(null, corner.TransformPoint(corner.rect.center + local));
        public string CloseButtonText => closeText != null ? closeText.text : null;

        void Awake() => view = GetComponent<TerrainChunkView>();
        void OnEnable() => active = this;
        void OnDisable() { if (active == this) active = null; }
        /// <summary>Gán lại chữ cố định theo ngôn ngữ hiện tại (0032); gọi lúc dựng và khi đổi ngôn ngữ.</summary>
        void RefreshText()
        {
            if (hintText != null) hintText.text = GameSettings.Text.Get("map.hint");
            if (closeText != null) closeText.text = GameSettings.Text.Get("map.close");
        }

        void OnDestroy()
        {
            GameSettings.Changed -= RefreshText;
            if (IsReady && view != null && view.Editor != null)
                view.Editor.CellChanged -= OnCellChanged;
            if (Texture != null)
                Destroy(Texture);
        }

        /// <summary>Ô (x, y, z) đổi → tính lại màu cột (x, z); đổi thì ghi vào texture, Apply một lần cuối Update (brief 0017).</summary>
        void OnCellChanged(int x, int y, int z)
        {
            if (!MinimapImage.UpdateColumn(view.World, rgba, x, z))
                return;
            int i = (x + view.World.Size.X * z) * 4;
            Texture.SetPixel(x, z, new Color32(rgba[i], rgba[i + 1], rgba[i + 2], 255));
            pixelsDirty = true;
            UpdatedPixelCount++;
        }

        void Update()
        {
            if (!IsReady)
            {
                if (view.World == null || view.Maps == null)
                    return;
                Build();
            }
            if (pixelsDirty)
            {
                Texture.Apply(false);
                pixelsDirty = false;
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
            else
                RefreshCorner();
        }

        public void SetOpen(bool open)
        {
            if (!IsReady)
                return;
            IsOpen = open;
            panel.SetActive(open);
            corner.gameObject.SetActive(!open);
            mouseDragging = false;
            lastZoom = -1f; // buộc cập nhật uv, ranh giới, tên khi mở
            if (open)
                Refresh();
            else
                RefreshCorner();
        }

        void Toggle()
        {
            if (toggledFrame == Time.frameCount)
                return;
            toggledFrame = Time.frameCount;
            SetOpen(!IsOpen);
        }

        /// <summary>Một lần bấm/chạm tại điểm màn hình: trúng ô tròn (đóng) hoặc nút Đóng (mở) → bật/tắt, trả true (brief 0025).</summary>
        public bool PressAt(Vector2 screen)
        {
            if (!IsReady || !IsOverButton(screen))
                return false;
            Toggle();
            return true;
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
            if (IsOpen)
            {
                RectTransformUtility.ScreenPointToLocalPointInRectangle(button, screen, null, out var local);
                return button.rect.Contains(local);
            }
            RectTransformUtility.ScreenPointToLocalPointInRectangle(corner, screen, null, out var c);
            return (c - corner.rect.center).magnitude <= corner.rect.width * 0.5f;
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
            if (mouse.leftButton.wasPressedThisFrame && PressAt(pos))
                return;
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

            // Ngón mới chạm lên ô tròn / nút Đóng → mở/đóng; ngón đó không dùng để kéo/chụm.
            foreach (var kv in touches)
                if (!lastTouches.ContainsKey(kv.Key) && PressAt(kv.Value))
                    buttonTouches.Add(kv.Key);
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
                bool showNames = Viewport.Zoom >= landmarkLabelZoom;
                for (int i = 0; i < landmarkIcons.Count; i++)
                {
                    var l = view.Landmarks[i];
                    Viewport.WorldToPanel(l.X + 0.5f, l.Z + 0.5f, out float ix, out float iy);
                    landmarkIcons[i].anchoredPosition = new Vector2(ix, iy);
                    landmarkLabels[i].rectTransform.anchoredPosition = new Vector2(ix, iy - landmarkIconSize * 0.5f - 2f);
                    landmarkLabels[i].enabled = showNames;
                }
            }

            var cam = Camera.main;
            marker.gameObject.SetActive(cam != null);
            if (cam == null)
                return;
            // World 7h (brief 0035): có nhân vật → vị trí là nhân vật, hướng vẫn là camera.
            var p = view.transform.InverseTransformPoint(PlayerCharacter.Active != null ? PlayerCharacter.Active.transform.position : cam.transform.position);
            Viewport.WorldToPanel(p.x, p.z, out float mx, out float my);
            marker.anchoredPosition = new Vector2(mx, my);
            var f = view.transform.InverseTransformDirection(cam.transform.forward);
            if (f.x * f.x + f.z * f.z > 1e-6f)
                marker.localEulerAngles = new Vector3(0f, 0f, -Mathf.Atan2(f.x, f.z) * Mathf.Rad2Deg);
        }

        /// <summary>Ô nhỏ (brief 0025): người chơi ở tâm — dời "Nội dung" + đổi uvRect ảnh nền, xoay mũi tên, tên map ở cột dưới người chơi.</summary>
        void RefreshCorner()
        {
            var cam = Camera.main;
            cornerMarker.gameObject.SetActive(cam != null);
            if (cam == null)
                return;
            // World 7h (brief 0035): có nhân vật → vị trí là nhân vật, hướng vẫn là camera.
            var p = view.transform.InverseTransformPoint(PlayerCharacter.Active != null ? PlayerCharacter.Active.transform.position : cam.transform.position);
            CornerViewport.WorldToPanel(p.x, p.z, out float px, out float py);
            var pos = new Vector2(-px, -py);
            if (cornerContent.anchoredPosition != pos)
                cornerContent.anchoredPosition = pos;
            float W = view.World.Size.X, H = view.World.Size.Z, half = CornerColumns * 0.5f;
            var uv = new Rect((p.x - half) / W, (p.z - half) / H, CornerColumns / W, CornerColumns / H);
            if (cornerImage.uvRect != uv)
                cornerImage.uvRect = uv;
            var f = view.transform.InverseTransformDirection(cam.transform.forward);
            if (f.x * f.x + f.z * f.z > 1e-6f)
                cornerMarker.localEulerAngles = new Vector3(0f, 0f, -Mathf.Atan2(f.x, f.z) * Mathf.Rad2Deg);
            int cx = Mathf.FloorToInt(p.x), cz = Mathf.FloorToInt(p.z);
            int id = cx >= 0 && cz >= 0 && cx < view.Maps.SizeX && cz < view.Maps.SizeZ ? view.Maps.Get(cx, cz) : MapTable.Ocean;
            if (id >= view.Maps.MapCount) id = -1; // Ocean hoặc id lạ → ẩn
            if (id != cornerShownMap)
            {
                cornerShownMap = id;
                cornerName.enabled = id >= 0;
                if (id >= 0) cornerName.text = view.Maps.Name(id);
            }
        }

        void Build()
        {
            var sw = Stopwatch.StartNew();
            var s = view.World.Size;
            rgba = MinimapImage.Build(view.World);
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
            view.Editor.CellChanged += OnCellChanged;
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
            hintText = NewText("Hướng dẫn", panelRt, font, 22, TextAnchor.MiddleLeft);
            Place(hintText.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(16f, -16f), new Vector2(900f, 56f));

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
            var lms = view.Landmarks;
            if (lms != null)
                for (int i = 0; i < lms.Count; i++)
                {
                    var l = lms[i];
                    var icon = Stretch("Dấu mốc " + i, frame, Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f));
                    Place(icon, Vector2.zero, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(landmarkIconSize, landmarkIconSize));
                    var g = icon.gameObject.AddComponent<MinimapLandmarkGraphic>();
                    g.diamond = l.Kind == LandmarkKind.Checkpoint;
                    g.color = LandmarkStyle.ColorOf(l);
                    g.raycastTarget = false;
                    var t = NewText("Tên dấu mốc " + i, frame, font, landmarkFontSize, TextAnchor.UpperCenter);
                    Place(t.rectTransform, Vector2.zero, new Vector2(0.5f, 1f), Vector2.zero, new Vector2(300f, 30f));
                    t.text = l.Name;
                    var outline = t.gameObject.AddComponent<Outline>();
                    outline.effectColor = new Color(0f, 0f, 0f, 0.85f);
                    outline.effectDistance = new Vector2(1.5f, -1.5f);
                    landmarkIcons.Add(icon);
                    landmarkLabels.Add(t);
                }
            marker = Stretch("Người chơi", frame, Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f));
            Place(marker, Vector2.zero, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(26f, 26f));
            var mg = marker.gameObject.AddComponent<MinimapMarkerGraphic>();
            mg.color = markerColor;
            mg.raycastTarget = false;

            // Nút Đóng trong bảng (brief 0025): chỗ nút cũ, chỉ hiện khi bản đồ mở.
            button = Stretch("Nút đóng", panelRt, Vector2.zero, Vector2.zero, new Vector2(1f, 1f));
            Place(button, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-16f, -16f), new Vector2(180f, 56f));
            AddImage(button, new Color(0.1f, 0.1f, 0.1f, 0.75f));
            closeText = NewText("Chữ", button, font, 24, TextAnchor.MiddleCenter);
            BuildCorner(root, font, borders);

            Canvas.ForceUpdateCanvases();
            Viewport = new MinimapViewport(s.X, s.Z, Mathf.Max(1f, frame.rect.width), Mathf.Max(1f, frame.rect.height));
            lines.Viewport = Viewport;
            IsReady = true;
            SetOpen(false);
            RefreshText();
            GameSettings.Changed += RefreshText;
            sw.Stop();
            BuildMs = sw.Elapsed.TotalMilliseconds;
            Debug.Log($"MINIMAP dựng: ảnh {s.X}x{s.Z}, {BorderCount} đường ranh giới, {labels.Count} tên, {landmarkIcons.Count} dấu mốc, ô nhỏ {CornerColumns:F0} cột {cornerK:F3} px/cột, font={(font != null ? font.name : "null")}, khung {frame.rect.width:F0}x{frame.rect.height:F0}, {BuildMs:F1} ms");
        }

        void BuildCorner(RectTransform root, Font font, List<float[]> borders)
        {
            var half = new Vector2(0.5f, 0.5f);
            float d = CornerSize - 2f * CornerRim;            // đường kính mặt nạ
            cornerK = d / CornerColumns;                       // px canvas mỗi cột
            float W = view.World.Size.X, H = view.World.Size.Z;
            CornerViewport = new MinimapViewport(W, H, W * cornerK, H * cornerK);

            corner = Stretch("Ô bản đồ", root, Vector2.zero, Vector2.zero, half);
            Place(corner, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-16f, -16f), new Vector2(CornerSize, CornerSize));
            var rim = corner.gameObject.AddComponent<TimeDialGraphic>();
            rim.shape = TimeDialShape.Disk;
            rim.color = CornerRimColor;
            rim.raycastTarget = false;

            var maskRt = Stretch("Mặt nạ", corner, Vector2.zero, Vector2.zero, half);
            Place(maskRt, half, half, Vector2.zero, new Vector2(d, d));
            var maskG = maskRt.gameObject.AddComponent<TimeDialGraphic>();
            maskG.shape = TimeDialShape.Disk;
            maskG.color = CornerMaskColor;
            maskG.raycastTarget = false;
            cornerMask = maskRt.gameObject.AddComponent<Mask>();
            cornerMask.showMaskGraphic = true;

            cornerImage = Stretch("Ảnh nền", maskRt, Vector2.zero, Vector2.zero, half).gameObject.AddComponent<RawImage>();
            cornerImage.texture = Texture;
            cornerImage.raycastTarget = false;

            cornerContent = Stretch("Nội dung", maskRt, Vector2.zero, Vector2.zero, half);
            Place(cornerContent, half, Vector2.zero, Vector2.zero, new Vector2(W * cornerK, H * cornerK));

            cornerLines = Stretch("Ranh giới", cornerContent, Vector2.zero, Vector2.zero, Vector2.zero).gameObject.AddComponent<MinimapLinesGraphic>();
            cornerLines.Lines = borders;
            cornerLines.Thickness = CornerLineThickness;
            cornerLines.color = borderColor;
            cornerLines.Viewport = CornerViewport;
            cornerLines.raycastTarget = false;

            var lms = view.Landmarks;
            if (lms != null)
                for (int i = 0; i < lms.Count; i++)
                {
                    var l = lms[i];
                    var icon = Stretch("Dấu mốc " + i, cornerContent, Vector2.zero, Vector2.zero, half);
                    CornerViewport.WorldToPanel(l.X + 0.5f, l.Z + 0.5f, out float ix, out float iy);
                    Place(icon, Vector2.zero, half, new Vector2(ix, iy), new Vector2(CornerLandmarkSize, CornerLandmarkSize));
                    var g = icon.gameObject.AddComponent<MinimapLandmarkGraphic>();
                    g.diamond = l.Kind == LandmarkKind.Checkpoint;
                    g.color = LandmarkStyle.ColorOf(l);
                    g.raycastTarget = false;
                    cornerLandmarks.Add(icon);
                }

            cornerMarker = Stretch("Người chơi", corner, Vector2.zero, Vector2.zero, half);
            Place(cornerMarker, half, half, Vector2.zero, new Vector2(CornerMarkerSize, CornerMarkerSize));
            var mg = cornerMarker.gameObject.AddComponent<MinimapMarkerGraphic>();
            mg.color = markerColor;
            mg.raycastTarget = false;

            cornerName = NewText("Tên map", corner, font, 16, TextAnchor.MiddleCenter);
            Place(cornerName.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 18f), new Vector2(160f, 22f));
            var outline = cornerName.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.85f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);
            cornerName.enabled = false;

            var nRt = Stretch("Nền N", corner, Vector2.zero, Vector2.zero, half);
            Place(nRt, new Vector2(0.5f, 1f), half, new Vector2(0f, -3f), new Vector2(28f, 28f));
            var nG = nRt.gameObject.AddComponent<TimeDialGraphic>();
            nG.shape = TimeDialShape.Disk;
            nG.color = CornerRimColor;
            nG.raycastTarget = false;
            cornerNorth = NewText("N", nRt, font, 20, TextAnchor.MiddleCenter);
            cornerNorth.text = "N";

            cornerShownMap = -2;
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
