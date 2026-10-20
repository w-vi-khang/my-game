# Brief 0016 — World 7c (phần 2/2): giao diện bản đồ — uGUI, phím M, chuột, chụm 2 ngón, điện thoại

> Trạng thái: chờ review
> Review: đầy đủ + user xem
> Model thực thi: Sonnet · medium
> Cập nhật: 2026-10-20 23:30

<!-- Planner (Opus) viết, Executor (Sonnet) làm theo ở PHIÊN KHÁC — executor không có lịch sử chat, không được tự quyết.
Trạng thái: chờ làm → đang làm → chờ review → đã duyệt; hoặc → cần Opus (executor dừng, hỏi ở mục Vướng mắc).
File được commit. -->

## Mục tiêu
Task [world bước 7](../tasks/m1-world-buoc-7-nhieu-map-minimap.md), dòng **7c** của [world-tech](../../design/04-tech/world-tech.md#kế-hoạch-prototype-m1) ("minimap: nền tầng độ cao, ranh giới cong, tên map, vị trí người chơi; phóng to/kéo (chuột + chụm 2 ngón)"). Brief [0015](0015-world-minimap-loi.md) (đã duyệt) làm lõi `MinimapImage` / `MinimapOverlay` / `MinimapViewport` trong Game.Core. Brief này làm **giao diện**: component `MinimapView` tự dựng Canvas uGUI lúc chạy, nút "Bản đồ (M)" góc trên-phải + phím M mở bảng bản đồ phủ màn hình; chuột lăn/kéo, cảm ứng 1 ngón kéo / 2 ngón chụm; gắn vào scene TerrainView (và Bench); chặn đào/đặt + xoay camera khi bản đồ mở; thử trên điện thoại bằng APK Bench.

## Bối cảnh cần đọc
- Lõi (brief 0015), namespace `Game.Core`:
  - `client/Assets/Game/Core/MinimapImage.cs`: `public static byte[] Build(WorldData world)` — RGBA32, 4 byte/cột, cột (x, z) ở byte `(x + Size.X · z) · 4`, hàng z = 0 trước (khớp `SetPixels32`).
  - `client/Assets/Game/Core/MinimapOverlay.cs:21`: `public static List<float[]> Borders(MapTable maps, int smoothIterations = SmoothIterations)` — mỗi đường `[x0, z0, x1, z1, …]` toạ độ góc ô. `:114`: `public static float[] LabelPositions(MapTable maps)` — `[x0, z0, x1, z1, …]`, map không có cột → NaN.
  - `client/Assets/Game/Core/MinimapViewport.cs`: constructor `MinimapViewport(float worldWidth, float worldHeight, float panelWidth, float panelHeight)` (`:22`); `PanelWidth`, `PanelHeight`, `Zoom`, `CenterX`, `CenterZ` (`:15-20`); `PixelsPerColumn` (`:34`); `SetPanelSize(float, float)` (`:37`); `WorldToPanel(float wx, float wz, out float px, out float py)` (`:55`); `PanelToWorld(float px, float py, out float wx, out float wz)` (`:62`); `ZoomAt(float px, float py, float factor)` (`:70`); `Pan(float dx, float dy)` (`:83`); `Pinch(float a0x, float a0y, float b0x, float b0y, float a1x, float a1y, float b1x, float b1y)` (`:95`); `VisibleUv(out float u, out float v, out float w, out float h)` (`:109`). Toạ độ khung: gốc dưới-trái, y lên; z world lên trên màn hình. Constructor/`SetPanelSize` ném lỗi khi kích thước ≤ 0.
- `client/Assets/Game/Core/MapTable.cs`: `public int MapCount` (`:28`), `public string Name(int map)` (`:29`).
- `client/Assets/Game/Client/TerrainChunkView.cs`: `public WorldData World` (`:60`), `public MapTable Maps` (`:64`, **null khi source = IslandV2**), `Start()` gọi `Build()` (`:106-109`). Toạ độ cục bộ của transform terrain = toạ độ ô (`CellAt` `:201-205`).
- `client/Assets/Game/Client/TerrainEditInput.cs`: `var mouse = Mouse.current;` + lăn chuột chọn loại `:45-52`; đào/đặt `:54-87`; `OnGUI` HUD `:108-128` (dòng đầu `:110-111`).
- `client/Assets/Game/Client/DebugFlyCamera.cs:57`: `if (mouse != null && mouse.middleButton.isPressed)` — xoay camera bằng chuột giữa.
- `client/Assets/Game/Client/TerrainBench.cs:200-208`: `OnGUI` vẽ hộp kết quả nửa dưới màn hình.
- `client/Assets/Game/Client/Editor/TerrainViewCapture.cs:133-150`: mẫu menu `SetupWorldEdge` (mở scene, thêm component, lưu).
- `client/Assets/Game/Client/Editor/BenchBuild.cs`: menu `Game/Tạo scene Bench` (`:23`, chép TerrainView → Bench, tắt TerrainEditInput + DebugFlyCamera, thêm TerrainBench), `Game/Build APK Bench` (`:48`, ra `client/Builds/bench.apk`, gói `com.viventa.bench`).
- `client/Assets/Game/Client/Game.Client.asmdef`: `"references": ["Game.Core", "Game.World", "Unity.Mathematics", "Unity.InputSystem"]` — chưa có uGUI. Asmdef của uGUI 2.0 tên `UnityEngine.UI` (`client/Library/PackageCache/com.unity.ugui@a9ea81766fbd/Runtime/UGUI/UnityEngine.UI.asmdef`) [run].
- Mẫu PlayMode test world v3 nạp dần: `client/Assets/Game/Tests/Client/TerrainStreamingPlayTests.cs:12-34` (asmdef `Game.Client.Tests` đã tham chiếu `Game.Core`, `Game.World`, `Game.Client`, `Unity.InputSystem`).

### API Unity dùng (đã tra cho 6000.4 / uGUI 2.0 / Input System 1.19; thành viên đã đối chiếu mã nguồn package trong `client/Library/PackageCache/` [run])
- Canvas: `Canvas.renderMode = RenderMode.ScreenSpaceOverlay` ([docs](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/Canvas-renderMode.html)), `sortingOrder` ([docs](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/Canvas-sortingOrder.html)), `Canvas.ForceUpdateCanvases()` ([docs](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/Canvas.ForceUpdateCanvases.html)).
- `CanvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize`, `referenceResolution`, `screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight`, `matchWidthOrHeight` ([docs](https://docs.unity3d.com/Packages/com.unity.ugui@2.0/api/UnityEngine.UI.CanvasScaler.html)).
- RectTransform: `anchorMin/anchorMax` ([docs](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/RectTransform-anchorMin.html)), `offsetMin/offsetMax` ([docs](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/RectTransform-offsetMin.html)), `pivot` ([docs](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/RectTransform-pivot.html)), `sizeDelta` ([docs](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/RectTransform-sizeDelta.html)), `anchoredPosition` ([docs](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/RectTransform-anchoredPosition.html)), `rect` ([docs](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/RectTransform-rect.html)).
- `RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, screenPoint, null, out local)` — Overlay → camera null ([docs](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/RectTransformUtility.ScreenPointToLocalPointInRectangle.html)); `RectTransformUtility.WorldToScreenPoint(null, worldPoint)` ([docs](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/RectTransformUtility.WorldToScreenPoint.html)); `Rect.Contains` ([docs](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/Rect.Contains.html)).
- uGUI: `Image.color` ([docs](https://docs.unity3d.com/Packages/com.unity.ugui@2.0/api/UnityEngine.UI.Image.html)), `RawImage.texture` / `RawImage.uvRect` ([docs](https://docs.unity3d.com/Packages/com.unity.ugui@2.0/api/UnityEngine.UI.RawImage.html)), `RectMask2D` cắt mọi `MaskableGraphic` con ([docs](https://docs.unity3d.com/Packages/com.unity.ugui@2.0/api/UnityEngine.UI.RectMask2D.html)), `Text.font/fontSize/alignment/horizontalOverflow/verticalOverflow` ([docs](https://docs.unity3d.com/Packages/com.unity.ugui@2.0/api/UnityEngine.UI.Text.html)), `Outline.effectColor/effectDistance` ([docs](https://docs.unity3d.com/Packages/com.unity.ugui@2.0/api/UnityEngine.UI.Outline.html)), `MaskableGraphic` + `protected override void OnPopulateMesh(VertexHelper vh)`, `SetVerticesDirty()`, `raycastTarget` ([docs](https://docs.unity3d.com/Packages/com.unity.ugui@2.0/api/UnityEngine.UI.MaskableGraphic.html)), `VertexHelper.Clear/AddVert(Vector3, Color32, Vector4)/AddTriangle/currentVertCount` ([docs](https://docs.unity3d.com/Packages/com.unity.ugui@2.0/api/UnityEngine.UI.VertexHelper.html)).
- Font: `Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")` — **không có trang docs 6000.4** (404); đã chạy được trong Editor (trả font động) và tên có trong tài nguyên mặc định của AndroidPlayer [run, phiên Planner 2026-10-20]. Code log `font=` để kiểm trên điện thoại (bước 9).
- Texture: `new Texture2D(w, h, TextureFormat.RGBA32, false)` ([docs](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/Texture2D-ctor.html)), `SetPixels32` ([docs](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/Texture2D.SetPixels32.html)), `Apply(false)` ([docs](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/Texture2D.Apply.html)), `GetPixels32` ([docs](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/Texture2D.GetPixels32.html)), `filterMode` / `wrapMode` ([docs](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/Texture-filterMode.html), [docs](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/Texture-wrapMode.html)).
- Input System 1.19: `Keyboard.current.mKey/escapeKey.wasPressedThisFrame` ([docs](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.19/api/UnityEngine.InputSystem.Keyboard.html)); `Mouse.current.position/leftButton/scroll` ([docs](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.19/api/UnityEngine.InputSystem.Mouse.html)); `Touchscreen.current.touches` ([docs](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.19/api/UnityEngine.InputSystem.Touchscreen.html)), `TouchControl.press.isPressed`, `touchId.ReadValue()`, `position.ReadValue()` ([docs](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.19/api/UnityEngine.InputSystem.Controls.TouchControl.html)).
- `Transform.InverseTransformDirection` ([docs](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/Transform.InverseTransformDirection.html)).
- Ngoài danh sách này không dùng API Unity nào khác. Cần API khác → dừng, `cần Opus`.

## Quyết định đã chốt (không được đổi)
1. **Dạng hiển thị** = bảng phủ cả màn hình bật/tắt (controls-ui: bản đồ là "bảng phủ lên world", phím M / nút menu). Không có minimap thu nhỏ ở góc. Mở lại giữ zoom/kéo của lần trước.
2. **Canvas dựng bằng code lúc chạy** (không prefab, không EventSystem, không GraphicRaycaster); vị trí con trỏ tự kiểm bằng `RectTransformUtility`. Overlay, `sortingOrder` 10, CanvasScaler theo màn hình mốc 1280 × 720, khớp chiều cao (`matchWidthOrHeight = 1`).
3. **Bố cục** (đơn vị canvas, mốc 720 cao): nút 180 × 56 cách góc trên-phải 16; bảng = nền đen α 0,6 phủ hết; dòng hướng dẫn trên-trái 900 × 56 cách 16; khung bản đồ cách mép trái/phải/dưới 16, trên 88; khung có `RectMask2D`, pivot (0, 0) → toạ độ cục bộ trong khung = toạ độ khung của `MinimapViewport`.
4. **Nội dung khung**: `RawImage` (texture 384 × 384 RGBA32 từ `MinimapImage.Build`, Point, Clamp; `uvRect` = `VisibleUv`); ranh giới = `MinimapLinesGraphic` dày 3, màu (30, 30, 30, 235), chỉ dựng lại đỉnh khi vùng nhìn đổi; tên map = `Text` cỡ 28 trắng + `Outline` đen α 0,85 lệch (2, −2), đặt bằng `anchoredPosition`; dấu người chơi = tam giác 26 × 26 (viền đen, lõi đỏ (235, 60, 50)) ở vị trí `Camera.main` (chưa có nhân vật), xoay theo hướng nhìn ngang của camera.
5. **Điều khiển**: phím M hoặc bấm/chạm nút → mở/đóng; Esc đóng. Khi mở: lăn chuột trên khung → zoom ×1,15 / ÷1,15 quanh con trỏ (chỉ lấy dấu của `scroll.y`, ngưỡng 0,01 như `TerrainEditInput`); giữ chuột trái bắt đầu trong khung + kéo → kéo bản đồ; cảm ứng: ngón có mặt ở cả khung trước và khung này, không bắt đầu trên nút — 1 ngón → kéo, ≥ 2 ngón → chụm (2 id nhỏ nhất). Mỗi khung chỉ mở/đóng tối đa 1 lần (phòng chuột ảo + chạm cùng lúc).
6. **Khi bản đồ mở**: `TerrainEditInput` không đào/đặt, không lăn chọn loại, ẩn khung chọn ô và HUD; con trỏ nằm trên nút (kể cả khi đóng) cũng không đào/đặt. `DebugFlyCamera` bỏ xoay bằng chuột giữa, **vẫn bay bằng phím** (để thấy dấu người chơi di chuyển). `TerrainBench` ẩn hộp kết quả.
7. **World không có bảng map** (`Maps == null`, đảo v2 — scene SimView, test cũ) → `MinimapView` không dựng gì, không có nút.
8. Gắn `MinimapView` lên GameObject có `TerrainChunkView` của TerrainView bằng menu Editor mới; Bench có qua "Game/Tạo scene Bench". Không sửa lõi Game.Core, bộ sinh, `design/`. Cập nhật bản đồ khi đào/đặt + dấu mốc là bước 7d (không làm ở đây).
9. Giờ bắt đầu/kết thúc lấy bằng `date "+%Y-%m-%d %H:%M"` lúc đó, không ước đoán.

## Phạm vi
- Được tạo: `client/Assets/Game/Client/MinimapView.cs`, `client/Assets/Game/Client/MinimapLinesGraphic.cs`, `client/Assets/Game/Client/MinimapMarkerGraphic.cs`, `client/Assets/Game/Tests/Client/MinimapViewPlayTests.cs` (+ `.meta` Unity tự tạo).
- Được sửa: `client/Assets/Game/Client/Game.Client.asmdef` (bước 1), `TerrainEditInput.cs`, `DebugFlyCamera.cs`, `TerrainBench.cs` (bước 5), `Editor/TerrainViewCapture.cs` (bước 6), scene `TerrainView.unity` + `Bench.unity` (qua menu, bước 7 và 9); mục Kết quả / Kết quả test / Vướng mắc của brief này.
- Không được sửa: mọi file khác (Game.Core, Game.World, `design/`, `ProjectSettings/` — trả lại ở bước dọn, `Bench.unity.meta` — trả lại nếu đổi guid).

## Các bước
0. `date "+%Y-%m-%d %H:%M"` → ghi "Bắt đầu" vào Kết quả; ghi base commit (`git rev-parse --short HEAD`). Unity Editor phải đang mở project `client/` có MCP (kiểm bằng MCP `read_console`); không có → `cần Opus`.

1. **`client/Assets/Game/Client/Game.Client.asmdef`** — thêm `"UnityEngine.UI"` vào cuối mảng `references`:
   ```json
       "references": [
           "Game.Core",
           "Game.World",
           "Unity.Mathematics",
           "Unity.InputSystem",
           "UnityEngine.UI"
       ],
   ```
   Xong khi: MCP `refresh_unity` → `read_console` (error) không có lỗi.

2. **`client/Assets/Game/Client/MinimapLinesGraphic.cs`** — tạo đúng nội dung:
   ```csharp
   using System.Collections.Generic;
   using Game.Core;
   using UnityEngine;
   using UnityEngine.UI;

   namespace Game.Client
   {
       /// <summary>
       /// Đường ranh giới map trên bản đồ (world-tech bước 7c, brief 0016): mỗi đường [x0, z0, x1, z1, …] toạ độ góc ô (MinimapOverlay.Borders)
       /// → dải tứ giác dày Thickness (đơn vị canvas), nối dài nửa bề dày ở hai đầu để khớp chỗ gấp. Pivot (0, 0) nên toạ độ cục bộ = toạ độ khung của Viewport.
       /// </summary>
       public class MinimapLinesGraphic : MaskableGraphic
       {
           public List<float[]> Lines;
           public MinimapViewport Viewport;
           public float Thickness = 3f;
           /// <summary>Số đỉnh lần dựng gần nhất (UI mesh tối đa 65.000 đỉnh).</summary>
           public int LastVertexCount { get; private set; }

           protected override void OnPopulateMesh(VertexHelper vh)
           {
               vh.Clear();
               LastVertexCount = 0;
               if (Lines == null || Viewport == null)
                   return;
               Color32 c = color;
               float half = Thickness * 0.5f;
               foreach (var line in Lines)
               {
                   for (int i = 0; i + 3 < line.Length; i += 2)
                   {
                       Viewport.WorldToPanel(line[i], line[i + 1], out float ax, out float ay);
                       Viewport.WorldToPanel(line[i + 2], line[i + 3], out float bx, out float by);
                       float dx = bx - ax, dy = by - ay;
                       float len = Mathf.Sqrt(dx * dx + dy * dy);
                       if (len < 1e-4f)
                           continue;
                       float ex = dx / len * half, ey = dy / len * half; // dọc đoạn
                       float nx = -ey, ny = ex;                          // vuông góc
                       ax -= ex; ay -= ey; bx += ex; by += ey;
                       int v = vh.currentVertCount;
                       vh.AddVert(new Vector3(ax + nx, ay + ny), c, Vector4.zero);
                       vh.AddVert(new Vector3(bx + nx, by + ny), c, Vector4.zero);
                       vh.AddVert(new Vector3(bx - nx, by - ny), c, Vector4.zero);
                       vh.AddVert(new Vector3(ax - nx, ay - ny), c, Vector4.zero);
                       vh.AddTriangle(v, v + 1, v + 2);
                       vh.AddTriangle(v, v + 2, v + 3);
                   }
               }
               LastVertexCount = vh.currentVertCount;
           }
       }
   }
   ```

3. **`client/Assets/Game/Client/MinimapMarkerGraphic.cs`** — tạo đúng nội dung:
   ```csharp
   using UnityEngine;
   using UnityEngine.UI;

   namespace Game.Client
   {
       /// <summary>Dấu người chơi trên bản đồ (brief 0016): tam giác chĩa lên (+y) phủ rect — viền tối, lõi màu `color` thu 0,6. Xoay/dời bằng RectTransform, không dựng lại đỉnh.</summary>
       public class MinimapMarkerGraphic : MaskableGraphic
       {
           public Color32 outlineColor = new Color32(20, 20, 20, 255);

           protected override void OnPopulateMesh(VertexHelper vh)
           {
               vh.Clear();
               var r = rectTransform.rect;
               AddTriangle(vh, r, 1f, outlineColor);
               AddTriangle(vh, r, 0.6f, color);
           }

           static void AddTriangle(VertexHelper vh, Rect r, float scale, Color32 c)
           {
               var center = r.center;
               float hw = r.width * 0.5f * scale, hh = r.height * 0.5f * scale;
               int v = vh.currentVertCount;
               vh.AddVert(new Vector3(center.x, center.y + hh), c, Vector4.zero);
               vh.AddVert(new Vector3(center.x + hw, center.y - hh), c, Vector4.zero);
               vh.AddVert(new Vector3(center.x - hw, center.y - hh), c, Vector4.zero);
               vh.AddTriangle(v, v + 1, v + 2);
           }
       }
   }
   ```
   Xong bước 2–3 khi: `refresh_unity` → `read_console` không lỗi.

4. **`client/Assets/Game/Client/MinimapView.cs`** — tạo đúng nội dung:
   ```csharp
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
   ```
   Xong khi: `refresh_unity` → `read_console` không lỗi.

5. **Chặn input khi bản đồ mở** (QĐ 6) — 4 chỗ sửa:
   - `client/Assets/Game/Client/TerrainEditInput.cs:45-46` — thay
     ```csharp
                 var mouse = Mouse.current;
                 if (mouse != null)
     ```
     bằng
     ```csharp
                 var mouse = Mouse.current;
                 // Bản đồ đang mở hoặc con trỏ trên nút bản đồ → không lăn chọn loại, không đào/đặt (brief 0016).
                 if (mouse != null && MinimapView.BlocksPointer(mouse.position.ReadValue()))
                 {
                     outline.enabled = false;
                     return;
                 }
                 if (mouse != null)
     ```
   - `TerrainEditInput.cs:110` — `if (view.Inventory == null)` → `if (view.Inventory == null || MinimapView.IsAnyOpen)`.
   - `client/Assets/Game/Client/DebugFlyCamera.cs:57` — `if (mouse != null && mouse.middleButton.isPressed)` → `if (mouse != null && mouse.middleButton.isPressed && !MinimapView.IsAnyOpen)`.
   - `client/Assets/Game/Client/TerrainBench.cs:200-201` — thêm dòng đầu thân `OnGUI()`: `if (MinimapView.IsAnyOpen) return; // Bản đồ mở → ẩn hộp kết quả (brief 0016)`.
   Xong khi: `refresh_unity` → `read_console` không lỗi; `git diff --stat` chỉ 3 file này + asmdef.

6. **Menu gắn bản đồ** — `client/Assets/Game/Client/Editor/TerrainViewCapture.cs`, thêm ngay sau hàm `SetupWorldEdge` (sau `:150`):
   ```csharp
           /// <summary>Bước 7c (brief 0016): bản đồ (MinimapView) trên TerrainChunkView của TerrainView.</summary>
           [MenuItem("Game/Bản đồ cho TerrainView")]
           public static void SetupMinimap()
           {
               EnsureScene();
               var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
               var view = Object.FindAnyObjectByType<TerrainChunkView>();
               if (view.GetComponent<MinimapView>() == null)
                   view.gameObject.AddComponent<MinimapView>();
               EditorSceneManager.MarkSceneDirty(scene);
               EditorSceneManager.SaveScene(scene);
               Debug.Log("TERRAINVIEW đã thêm bản đồ");
           }
   ```

7. **Gắn vào scene** — sau khi biên dịch xong: MCP `execute_menu_item` "Game/Bản đồ cho TerrainView". Xong khi: console có `TERRAINVIEW đã thêm bản đồ`; `git diff --stat` có `TerrainView.unity`; `grep -c "m_Script: {fileID: 11500000, guid: $(grep guid: client/Assets/Game/Client/MinimapView.cs.meta | awk '{print $2}')" client/Assets/Game/Client/Scenes/TerrainView.unity` = 1.

8. **PlayMode test** — `client/Assets/Game/Tests/Client/MinimapViewPlayTests.cs`, tạo đúng nội dung:
   ```csharp
   using System.Collections;
   using Game.Core;
   using NUnit.Framework;
   using UnityEngine;
   using UnityEngine.TestTools;

   namespace Game.Client.Tests
   {
       /// <summary>World-tech bước 7c (brief 0016): bản đồ dựng từ world v3 — ảnh nền, vùng nhìn, tên map, dấu người chơi, chặn chuột.</summary>
       public class MinimapViewPlayTests
       {
           GameObject camGo, go;
           TerrainChunkView view;
           MinimapView map;

           [UnitySetUp]
           public IEnumerator SetUp()
           {
               camGo = new GameObject("Cam");
               camGo.tag = "MainCamera";
               camGo.AddComponent<Camera>();
               camGo.transform.position = new Vector3(192, 40, 60);
               go = new GameObject("Terrain");
               view = go.AddComponent<TerrainChunkView>();
               view.source = WorldSource.MapsV3;
               view.streaming = true;
               view.streamTarget = camGo.transform;
               map = go.AddComponent<MinimapView>();
               for (int i = 0; i < 5 && !map.IsReady; i++)
                   yield return null;
               Assert.IsTrue(map.IsReady, "Bản đồ chưa dựng");
           }

           [UnityTearDown]
           public IEnumerator TearDown()
           {
               Object.Destroy(go);
               Object.Destroy(camGo);
               yield return null;
           }

           static void AssertUvMatches(MinimapView map)
           {
               map.Viewport.VisibleUv(out float u, out float v, out float w, out float h);
               var r = map.UvRect;
               Assert.AreEqual(u, r.x, 1e-4f, "u");
               Assert.AreEqual(v, r.y, 1e-4f, "v");
               Assert.AreEqual(w, r.width, 1e-4f, "w");
               Assert.AreEqual(h, r.height, 1e-4f, "h");
           }

           [UnityTest]
           public IEnumerator TextureMatchesCoreImage()
           {
               var tex = map.Texture;
               Assert.AreEqual(384, tex.width);
               Assert.AreEqual(384, tex.height);
               Assert.AreEqual(FilterMode.Point, tex.filterMode);
               Assert.AreEqual(TextureWrapMode.Clamp, tex.wrapMode);
               var px = tex.GetPixels32();
               var rgba = MinimapImage.Build(view.World);
               Assert.AreEqual(rgba.Length, px.Length * 4);
               for (int i = 0; i < px.Length; i++)
                   if (px[i].r != rgba[i * 4] || px[i].g != rgba[i * 4 + 1] || px[i].b != rgba[i * 4 + 2] || px[i].a != 255)
                       Assert.Fail($"điểm ảnh {i} (x={i % 384}, z={i / 384}) lệch");
               yield return null;
           }

           [UnityTest]
           public IEnumerator ClosedByDefaultAndToggles()
           {
               Assert.IsFalse(map.IsOpen);
               Assert.IsFalse(map.PanelActive);
               Assert.IsFalse(MinimapView.IsAnyOpen);
               map.SetOpen(true);
               Assert.IsTrue(map.IsOpen);
               Assert.IsTrue(map.PanelActive);
               Assert.IsTrue(MinimapView.IsAnyOpen);
               yield return null;
               map.SetOpen(false);
               Assert.IsFalse(map.PanelActive);
               Assert.IsFalse(MinimapView.IsAnyOpen);
           }

           [UnityTest]
           public IEnumerator UvRectAndBordersFollowViewport()
           {
               map.SetOpen(true);
               yield return null;
               Canvas.ForceUpdateCanvases();
               AssertUvMatches(map);
               Assert.AreEqual(1f, map.Viewport.Zoom, 1e-5f);
               Assert.Greater(map.BorderCount, 0);
               Assert.Greater(map.BorderVertexCount, 0, "ranh giới chưa dựng đỉnh");
               Assert.Less(map.BorderVertexCount, 65000, "vượt giới hạn đỉnh UI");
               Debug.Log($"MINIMAPTEST ranh giới {map.BorderCount} đường, {map.BorderVertexCount} đỉnh, khung {map.Viewport.PanelWidth:F0}x{map.Viewport.PanelHeight:F0}, dựng {map.BuildMs:F1} ms");

               var vp = map.Viewport;
               vp.ZoomAt(vp.PanelWidth * 0.5f, vp.PanelHeight * 0.5f, 4f);
               yield return null;
               Assert.AreEqual(4f, vp.Zoom, 1e-4f);
               AssertUvMatches(map);
           }

           [UnityTest]
           public IEnumerator LabelsShowMapNamesAtLabelPositions()
           {
               map.SetOpen(true);
               yield return null;
               Assert.AreEqual(view.Maps.MapCount, map.LabelCount);
               var pos = MinimapOverlay.LabelPositions(view.Maps);
               for (int m = 0; m < map.LabelCount; m++)
               {
                   Assert.AreEqual(view.Maps.Name(m), map.LabelText(m));
                   Assert.IsTrue(map.LabelVisible(m), $"tên map {m} bị ẩn");
                   map.Viewport.WorldToPanel(pos[2 * m], pos[2 * m + 1], out float px, out float py);
                   Assert.AreEqual(px, map.LabelPanelPosition(m).x, 0.5f, $"map {m} x");
                   Assert.AreEqual(py, map.LabelPanelPosition(m).y, 0.5f, $"map {m} y");
               }
           }

           [UnityTest]
           public IEnumerator MarkerFollowsCameraPositionAndHeading()
           {
               map.SetOpen(true);
               camGo.transform.position = new Vector3(100.5f, 40f, 250.5f);
               camGo.transform.rotation = Quaternion.LookRotation(Vector3.right);
               yield return null;
               map.Viewport.WorldToPanel(100.5f, 250.5f, out float px, out float py);
               Assert.AreEqual(px, map.MarkerPanelPosition.x, 0.5f);
               Assert.AreEqual(py, map.MarkerPanelPosition.y, 0.5f);
               Assert.Less(Mathf.Abs(Mathf.DeltaAngle(270f, map.MarkerAngle)), 0.5f, "nhìn +x → dấu quay về phải");
           }

           [UnityTest]
           public IEnumerator ScreenPanAndZoomUsePanelCoordinates()
           {
               map.SetOpen(true);
               yield return null;
               var vp = map.Viewport;
               vp.ZoomAt(vp.PanelWidth * 0.5f, vp.PanelHeight * 0.5f, 4f);
               var a = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
               var b = a + new Vector2(40f, 0f);
               Vector2 pa = map.ScreenToPanel(a), pb = map.ScreenToPanel(b);
               Assert.Greater(pb.x - pa.x, 0f, "sang phải trên màn hình = sang phải trong khung");
               float expected = vp.CenterX - (pb.x - pa.x) / vp.PixelsPerColumn;
               map.PanScreen(a, b);
               Assert.AreEqual(expected, vp.CenterX, 1e-3f);

               var pc = map.ScreenToPanel(a);
               vp.PanelToWorld(pc.x, pc.y, out float wx0, out float wz0);
               map.ZoomAtScreen(a, 1.5f);
               vp.PanelToWorld(pc.x, pc.y, out float wx1, out float wz1);
               Assert.AreEqual(6f, vp.Zoom, 1e-4f);
               Assert.AreEqual(wx0, wx1, 1e-3f);
               Assert.AreEqual(wz0, wz1, 1e-3f);
           }

           [UnityTest]
           public IEnumerator BlocksPointerOnlyOverButtonWhenClosed()
           {
               Canvas.ForceUpdateCanvases();
               Assert.IsTrue(MinimapView.BlocksPointer(map.ButtonScreenCenter), "tâm nút");
               Assert.IsFalse(MinimapView.BlocksPointer(new Vector2(5f, 5f)), "góc dưới-trái khi đóng");
               map.SetOpen(true);
               Assert.IsTrue(MinimapView.BlocksPointer(new Vector2(5f, 5f)), "góc dưới-trái khi mở");
               yield return null;
           }

           [UnityTest]
           public IEnumerator IslandWorldHasNoMap()
           {
               var go2 = new GameObject("Đảo");
               go2.AddComponent<TerrainChunkView>(); // mặc định IslandV2, Maps = null
               var map2 = go2.AddComponent<MinimapView>();
               for (int i = 0; i < 3; i++)
                   yield return null;
               Assert.IsFalse(map2.IsReady);
               map2.SetOpen(true);
               Assert.IsFalse(map2.IsOpen);
               Assert.IsNull(go2.GetComponentInChildren<Canvas>(true));
               Object.Destroy(go2);
               yield return null;
           }
       }
   }
   ```
   Xong khi: MCP `run_tests` PlayMode lọc `MinimapViewPlayTests` → 8/8 đạt; ghi dòng `MINIMAPTEST …` (đọc console) vào Kết quả.

9. **Bench + điện thoại** (adb: `/Applications/Unity/Hub/Editor/6000.4.8f1/PlaybackEngines/AndroidPlayer/SDK/platform-tools/adb`, gọi là `ADB`; máy Xiaomi 24095PCADG serial `PFQCRWVWR46HU4GQ` qua USB — nếu `ADB devices` không có serial này thì `ADB connect 192.168.1.6:5555` và dùng `-s 192.168.1.6:5555`):
   1. MCP `execute_menu_item` "Game/Tạo scene Bench" → nếu `git status --short` có `Bench.unity.meta` thì `git checkout -- client/Assets/Game/Client/Scenes/Bench.unity.meta`. Kiểm `Bench.unity` có MinimapView (grep guid như bước 7, = 1).
   2. MCP `execute_menu_item` "Game/Build APK Bench" → console có `Bench build: Succeeded`.
   3. `ADB -s <máy> install -r client/Builds/bench.apk` → `Success`. `ADB -s <máy> logcat -c`. `ADB -s <máy> shell monkey -p com.viventa.bench -c android.intent.category.LAUNCHER 1`.
   4. Chờ 75 giây (lệnh `sleep` chạy nền hoặc Monitor), rồi `ADB -s <máy> logcat -d -s Unity | grep -E "BENCH|MINIMAP"` → ghi nguyên dòng `MINIMAP dựng: …` (phải có `font=` khác `null`) và các trường `avgFps`, `p99Ms`, `holeFrames`, `hash` của dòng `BENCH` vào Kết quả. Không có dòng `BENCH` sau 150 giây (máy khoá màn hình làm app tạm dừng) → ghi "chờ user mở khoá" vào Kết quả test, làm tiếp bước 10 (không chờ).
   5. Dọn: `git checkout -- client/ProjectSettings/UnityConnectSettings.asset` (nếu đổi).

10. Commit code + scene: `git status --short` chỉ còn file trong Phạm vi (+ brief); commit `brief 0016: giao diện bản đồ — uGUI, phím M, chuột, chụm 2 ngón`. Rồi `date "+%Y-%m-%d %H:%M"` → ghi "Kết thúc" vào Kết quả, đặt trạng thái `chờ review`, commit brief `brief 0016: kết quả`.

## Tiêu chí xong
- Biên dịch không lỗi; `MinimapViewPlayTests` 8/8; EditMode cả bộ đạt (≥ 301, không giảm); PlayMode cả bộ đạt (số cũ + 8).
- `TerrainView.unity` và `Bench.unity` mỗi file có đúng 1 `MinimapView`.
- Điện thoại: dòng `MINIMAP dựng` có `font=` khác `null`; `BENCH … avgFps` ≥ 30, `holeFrames=0`, `hash=a7719c8999939263` (hoặc ghi "chờ user mở khoá").
- `git status` sạch sau commit (không còn `EditorSettings.asset`, `UnityConnectSettings.asset`, `Bench.unity.meta`).

## Test sau khi xong

### Phần cần test
- Asmdef + 3 file mới biên dịch (bước 1–4) · `refresh_unity` + `read_console` · 0 lỗi.
- Ảnh nền trên texture (bước 4) · PlayMode `TextureMatchesCoreImage` · 384 × 384, Point, Clamp, từng điểm ảnh = `MinimapImage.Build`.
- Mở/đóng + cờ toàn cục (bước 4) · `ClosedByDefaultAndToggles` · mặc định đóng; `SetOpen` đổi `IsOpen`, `PanelActive`, `IsAnyOpen`.
- Vùng nhìn → `uvRect` + ranh giới (bước 2, 4) · `UvRectAndBordersFollowViewport` · uv khớp `VisibleUv` ở zoom 1 và 4; ranh giới có đỉnh, < 65.000.
- Tên map (bước 4) · `LabelsShowMapNamesAtLabelPositions` · đủ `MapCount` tên, đúng chữ, đúng vị trí ±0,5.
- Dấu người chơi (bước 3–4) · `MarkerFollowsCameraPositionAndHeading` · vị trí ±0,5; nhìn +x → góc 270° ±0,5.
- Đổi toạ độ màn hình → khung, kéo, phóng quanh con trỏ (bước 4) · `ScreenPanAndZoomUsePanelCoordinates` · tâm dời đúng; điểm dưới con trỏ giữ chỗ.
- Chặn chuột (bước 4–5) · `BlocksPointerOnlyOverButtonWhenClosed` · khi đóng chỉ chặn trên nút; khi mở chặn mọi chỗ.
- World đảo v2 không có bản đồ (bước 4) · `IslandWorldHasNoMap` · không dựng, không Canvas.
- Regression đào/đặt, camera bay, nạp dần, Bench, SimView (bước 5): PlayMode cả bộ (`TerrainEditPlayTests`, `FlyCameraMoveKeyPlayTests`, `FlyCameraWaterPlayTests`, `WorldBoundsPlayTests`, `TerrainStreamingPlayTests`, `SimViewPlayTests`, …) · đạt.
- Regression lõi (không sửa nhưng dùng chung): EditMode cả bộ (gồm `MinimapImageTests`, `MinimapOverlayTests`, `MinimapViewportTests`, `MinimapWorldTests`) · đạt.
- Scene (bước 7, 9.1) · grep guid MinimapView trong `TerrainView.unity` và `Bench.unity` · mỗi file = 1.
- Điện thoại (bước 9) · logcat · `MINIMAP dựng` có font khác null + ms dựng; `BENCH` avgFps ≥ 30, holeFrames=0, mã băm v3 trùng.
- Thao tác thật (phím M, chuột, chụm 2 ngón) · user tự test bên dưới · chờ user.

### Lệnh executor chạy
1. MCP `refresh_unity` → `read_console` (types error) — 0 lỗi biên dịch.
2. MCP `run_tests` PlayMode lọc `MinimapViewPlayTests` → 8/8; đọc console dòng `MINIMAPTEST`.
3. MCP `run_tests` EditMode (cả bộ) → ghi dòng tổng kết.
4. MCP `run_tests` PlayMode (cả bộ) → ghi dòng tổng kết. Dọn: `git checkout -- client/ProjectSettings/EditorSettings.asset`.
5. Bước 9 (Bench, APK, adb, logcat) → ghi dòng `MINIMAP` + `BENCH`. Dọn: `git checkout -- client/ProjectSettings/UnityConnectSettings.asset`; `Bench.unity.meta` nếu đổi.
6. `git status --short` → chỉ file trong Phạm vi; commit (bước 10).
7. `date "+%Y-%m-%d %H:%M"` → "Kết thúc"; commit brief.

### User tự test
**Trên Mac** — mở scene TerrainView, Play:
1. Góc trên-phải có nút **"Bản đồ (M)"**. Bấm nút (hoặc phím M): bảng tối phủ màn hình, thấy cả world — đảo 3 map màu theo độ cao, nước xanh quanh, **đường ranh giới đen cong** giữa các map, tên **"Map 1/2/3"** chữ trắng viền đen giữa mỗi map, **tam giác đỏ** ở vị trí camera, mũi chỉ hướng camera đang nhìn.
2. Lăn chuột trên bản đồ: phóng to/thu nhỏ quanh con trỏ (tối đa ×8, không nhỏ hơn cả world); giữ chuột trái kéo: bản đồ đi theo tay, không kéo ra ngoài world được. Ảnh nền rõ từng ô (không nhoè), ranh giới và tên đi đúng theo ảnh.
3. Khi bản đồ mở: bấm chuột không đào/đặt, giữ chuột giữa không xoay camera; giữ W/A/S/D: tam giác đỏ di chuyển trên bản đồ.
4. Esc hoặc M hoặc nút "Đóng (M)": bảng tắt, HUD hiện lại; bấm vào nút "Bản đồ (M)" không đào ô phía sau nút.

**Trên điện thoại** — executor đã cài và mở app Bench (gói `com.viventa.bench`). Chờ hộp kết quả hiện ở nửa dưới (~45 giây), rồi:
5. Chạm nút **"Bản đồ (M)"** góc trên-phải: bảng bản đồ mở (hộp kết quả ẩn). Chữ tiếng Việt và tên map hiện đúng (không ô vuông).
6. **Chụm 2 ngón** để phóng to/thu nhỏ quanh giữa hai ngón; **kéo 1 ngón** để di chuyển; cảm giác mượt, không giật.
7. Chạm "Đóng (M)": bảng tắt, hộp kết quả hiện lại.

## Dừng và chuyển lại Opus khi
- Brief mơ hồ hoặc mâu thuẫn với code thật (vd số dòng không khớp nội dung trích).
- Cần một quyết định thuộc AGENTS.md §3.4 hoặc ngoài "Quyết định đã chốt", hoặc cần API Unity ngoài danh sách ở "Bối cảnh cần đọc".
- Cùng một lỗi sửa 2 lần vẫn chưa qua (vd test PlayMode hỏng vì kích thước khung/Screen trong Editor).
- Phải sửa file ngoài "Phạm vi".

---
<!-- Từ đây trở xuống: EXECUTOR ghi. -->

## Kết quả
- Bắt đầu: 2026-10-20 23:09
- Base commit: d7863e2
- Chạy bằng: claude-sonnet-5-5 · medium
- Kết thúc: 2026-10-20 23:23 (~14 phút thực tế)
- Commit: c02650c. File đổi: Game.Client.asmdef, MinimapLinesGraphic.cs, MinimapMarkerGraphic.cs, MinimapView.cs, MinimapViewPlayTests.cs (+ .meta), TerrainEditInput.cs, DebugFlyCamera.cs, TerrainBench.cs, Editor/TerrainViewCapture.cs, Scenes/TerrainView.unity, Scenes/Bench.unity.
- **Lệch so với brief (1 chỗ):** thêm `[RequireComponent(typeof(CanvasRenderer))]` vào `MinimapLinesGraphic` và `MinimapMarkerGraphic` — code mẫu thiếu, `MaskableGraphic` không tự thêm CanvasRenderer → lần chạy đầu 8/8 test hỏng `MissingComponentException ... "Ranh giới"`. Sau sửa 8/8 đạt.
- Máy Xiaomi nối Wi‑Fi `192.168.1.6:5555` (serial USB không có) — dùng phương án dự phòng của brief.
- `MINIMAPTEST ranh giới 3 đường, 8804 đỉnh, khung 1374x616, dựng 46.1 ms`
- Điện thoại: `MINIMAP dựng: ảnh 384x384, 3 đường ranh giới, 3 tên, font=LegacyRuntime, khung 1569x616, 121,9 ms`; BENCH `avgFps=36,1 p99Ms=49,94 holeFrames=0 hash=a7719c8999939263`

## Kết quả test
- Biên dịch (bước 1–4) · đạt · `read_console` error = 0.
- Texture / mở-đóng / uv+ranh giới / tên map / dấu người chơi / kéo-phóng / chặn chuột / đảo v2 · đạt · `MinimapViewPlayTests` 8/8.
- Regression PlayMode cả bộ · đạt · 27/27.
- Regression EditMode cả bộ · đạt · 301/301.
- Scene · đạt · grep guid MinimapView = 1 trong TerrainView.unity và Bench.unity.
- Điện thoại · đạt · font=LegacyRuntime (≠ null), avgFps 36,1 ≥ 30, holeFrames=0, hash khớp.
- Thao tác thật (phím M, chuột, chụm 2 ngón, PC + điện thoại) · chờ user.
- Dọn: EditorSettings.asset, UnityConnectSettings.asset, Bench.unity.meta đã checkout lại; git status sạch sau commit code.

## Vướng mắc
(không)

---
<!-- PLANNER ghi khi review. -->

## Review
- Chạy bằng `claude-sonnet-5-5 · medium` khớp Model thực thi.
- `git show --stat c02650c`: 15 file, đúng Phạm vi (3 file mới + test + .meta, asmdef, TerrainEditInput/DebugFlyCamera/TerrainBench, TerrainViewCapture, 2 scene); không đụng Game.Core/Game.World/`design/`/ProjectSettings.
- Diff đối chiếu: code trùng brief; sửa 4 chỗ chặn input đúng bước 5; menu đúng bước 6. Lệch duy nhất: `[RequireComponent(typeof(CanvasRenderer))]` trên `MinimapLinesGraphic`/`MinimapMarkerGraphic` — chấp nhận: lỗi ở code mẫu của Planner (graphic tạo bằng `AddComponent` trên GameObject trống không có CanvasRenderer), sửa tối thiểu, không đổi hành vi; executor lẽ ra nên dừng `cần Opus` nhưng thuộc loại sửa lỗi hiển nhiên.
- Mọi dòng "Phần cần test" có kết quả: 8/8 test mới; PlayMode 27/27 = 19 cũ + 8; EditMode 301/301 (không giảm); scene mỗi file 1 MinimapView; điện thoại font=LegacyRuntime, holeFrames=0, hash a7719c8999939263 khớp.
- FPS điện thoại 36,1 (đạt ≥ 30) nhưng thấp hơn 53,1 của 7b4 (0014). Nguyên nhân chưa xác minh: Canvas overlay luôn vẽ nút, hoặc nhiệt độ máy (7b3→7b4 từng lệch ngược chiều 42,2→53,1 khi tam giác tăng). Cách xác minh: chạy lại Bench khi máy nguội, so với bản tắt MinimapView.
- Giờ thực tế: 23:09 → 23:23 (14 phút); 7c gồm 0015 (4 phút) — đã ghi roadmap.
- User xem (thao tác thật Mac + điện thoại): chờ.

## Kiểm chứng
- `client_test` EditMode: đạt 301/301 — chạy qua MCP `run_tests` (Editor đang mở nên không dùng `tools/unity_test.py`, theo AGENTS.local.md).
- `client_test_play` PlayMode: đạt 27/27 (gồm `MinimapViewPlayTests` 8/8) — qua MCP.
- Không chạy `design`, `asset_check` (file đổi ngoài phạm vi).
- Checklist: pillar/thành phần/glossary/nhất quán thiết kế/nguồn/ước lượng — không áp dụng (không đổi `design/`; bản đồ là client Unity, hạng mục 7c M1). Game.Core không sửa — đạt. Bước M1 7c có test đúng "Xong khi" — đạt (xem Kết quả test). Chỉ làm hạng mục M1 — đạt. Giờ thực tế ~14 phút — đã ghi ở Kết quả.
- Dọn: `Bench.unity.meta`, `UnityConnectSettings.asset`, `EditorSettings.asset` do Unity ghi lại đã `git checkout`; `git status` sạch.
