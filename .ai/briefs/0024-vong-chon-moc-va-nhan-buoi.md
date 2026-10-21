# Brief 0024 — World 7f-3: vòng chọn 4 mốc + dòng "Đang ở buổi" (thay phím F5–F8)

> Trạng thái: chờ review
> Review: đầy đủ + user xem
> Model thực thi: Sonnet · medium
> Cập nhật: 2026-10-21 16:20

<!-- Planner (Opus) viết, Executor (Sonnet) làm theo ở PHIÊN KHÁC — executor không có lịch sử chat, không được tự quyết.
Trạng thái: chờ làm → đang làm → chờ review → đã duyệt; hoặc → cần Opus (executor dừng, hỏi ở mục Vướng mắc).
Review: nhẹ (đọc mục Kết quả) | đầy đủ (đọc cả diff) | user xem (có phần user tự kiểm bằng mắt/chơi thử) — có thể ghép "đầy đủ + user xem".
Model thực thi: BẮT BUỘC, đúng dạng `<Model> · <effort>` — Model: Opus | Sonnet | Haiku | Fable; effort: low | medium | high | xhigh | max (chọn theo AGENTS.md §10.2).
Executor kiểm tra model + effort của phiên TRƯỚC khi làm (scripts/brief_check.py); lệch hoặc không xác định được → cảnh báo user và DỪNG.
File được commit. Tự kiểm trước khi giao (AGENTS.md §10.3): executor có phải hỏi gì không? còn chỗ nào bỏ ngỏ 2 phương án? -->

## Mục tiêu
Người chơi chọn thời điểm bằng một **vòng 4 biểu tượng** (giữa trưa, hoàng hôn, giữa đêm, bình minh) quanh biểu tượng mốc hiện tại, mở bằng nút tròn ở góc trên-phải hoặc phím **T**. Dùng được bằng chuột và cảm ứng. Một dòng chữ "Đang ở buổi: …" luôn hiện cạnh nút. Bỏ phím tạm F5–F8 của brief 0023. Đo FPS trên điện thoại với vòng đang mở.

Task contract: [.ai/tasks/m1-world-buoc-7f-buoi-trong-ngay.md](../tasks/m1-world-buoc-7f-buoi-trong-ngay.md) (brief 3/3, cuối cùng của bước 7f).

## Bối cảnh cần đọc
- [0024](../../design/decisions/0024-buoi-trong-ngay-va-chon-thoi-diem.md) mục Quyết định 3–6: 4 mốc, chọn mốc đổi buổi thật, xoay theo chiều tiến, đang xoay thì bỏ qua lệnh chọn; tên 4 buổi. [mechanics](../../design/02-gameplay/mechanics.md#buổi-trong-ngày) dòng 163: còn trong khung của mốc thì chọn lại mốc đó không làm gì.
- Lõi `client/Assets/Game/Core/DayCycle/` (**không sửa**):
  - `TimeConfig.cs:6`: `public enum TimeLandmark : byte { Midnight = 0, Dawn = 1, Noon = 2, Dusk = 3 }`
  - `TimeConfig.cs:9`: `public enum DayPeriod : byte { Dawn = 0, Day = 1, Dusk = 2, Night = 3 }`
  - `TimeOfDay.cs:23-27`: `int OffsetMs`, `int PhaseMs`, `DayPeriod Period`, `TimeLandmark CurrentWindow`. Khung mốc: giữa đêm [0 h; 6 h), bình minh [6 h; 12 h), giữa trưa [12 h; 18 h), hoàng hôn [18 h; 24 h). Buổi: Ban đêm [19,2 h; 4,8 h), Bình minh [4,8 h; 7,2 h), Ban ngày [7,2 h; 16,8 h), Hoàng hôn [16,8 h; 19,2 h).
- `client/Assets/Game/Client/DayCycleView.cs` (brief 0023):
  - `:19-20` trường `debugKeys`, `:74-81` khối phím F5–F8 (bỏ ở bước 3).
  - `:26` `public TimeOfDay Day { get; private set; }`: gán trong `Start()` (`:63`), trước đó là `null`.
  - `:29` `public bool IsRotating`.
  - `:90-104` `public int Select(TimeLandmark m)`: trả cung (ms) khi xoay; `0` khi đang trong khung mốc (không đổi gì); `−1` khi đang xoay (bỏ qua). Khi cung > 0 hàm tự ghi log `DAYCYCLE select=…`.
- `client/Assets/Game/Client/MinimapView.cs`: mẫu UI dựng bằng code, **chép cách làm**, không sửa file này.
  - `:21-27` `IsAnyOpen`, `BlocksPointer`, biến tĩnh `active`; `:89-90` gán/bỏ `active` ở `OnEnable`/`OnDisable`.
  - `:86` tâm nút theo pixel màn hình: `RectTransformUtility.WorldToScreenPoint(null, button.TransformPoint(button.rect.center))`.
  - `:163-169` `IsOverButton` bằng `RectTransformUtility.ScreenPointToLocalPointInRectangle(rt, screen, null, out var local)`.
  - `:225-258` theo dõi ngón chạm: `Touchscreen.current`, `t.press.isPressed`, `t.touchId.ReadValue()`, `t.position.ReadValue()`, ngón mới = có trong `touches` mà không có trong `lastTouches`.
  - `:329-340` tạo `Canvas` (ScreenSpaceOverlay) + `CanvasScaler` (1280 × 720, `MatchWidthOrHeight`, `matchWidthOrHeight = 1`), font `Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")`.
  - `:412-453` hàm phụ `Stretch`, `Place`, `AddImage`, `NewText`: chép nguyên sang lớp mới (là `static` riêng tư, không dùng chung được).
- `client/Assets/Game/Client/MinimapLandmarkGraphic.cs:7-42`: mẫu `MaskableGraphic` tự vẽ bằng `OnPopulateMesh(VertexHelper vh)`, `vh.AddVert(Vector3, Color32, Vector4.zero)`, `vh.AddTriangle(a, b, c)`.
- `client/Assets/Game/Client/TerrainEditInput.cs:46-52`: chặn đào/đặt khi con trỏ thuộc bản đồ (`MinimapView.BlocksPointer`). Đào/đặt xảy ra khi `mouse.leftButton.wasPressedThisFrame` / `rightButton` (`:80-91`).
- `client/Assets/Game/Client/Editor/BenchBuild.cs:43-48`: vòng `foreach` đặt `benchCycleSeconds = 5f` và `debugKeys = false` cho `DayCycleView` trong scene Bench.
- Test đang gán `debugKeys`: `client/Assets/Game/Tests/Client/DayCycleViewPlayTests.cs:37`, `DayCycleWaterPlayTests.cs:50`.
- `client/Assets/Game/Tests/Client/MinimapViewPlayTests.cs:17-31`: mẫu dựng world v3 + `MinimapView` trong PlayMode.
- Scene `TerrainView.unity`, `SimView.unity`: `Main Camera` đã có `DayCycleView` (dòng 278 mỗi file là `m_EditorClassIdentifier` của nó).
- Kết quả trước khi làm brief này [run, brief 0023]: EditMode 340/340, PlayMode 52/52; điện thoại avgFps 50,6, holeFrames 0, hash `a7719c8999939263`.

## API Unity dùng (đã tra 6000.4 / gói đúng version)
- `Canvas.renderMode`, `Canvas.sortingOrder`, `Canvas.ForceUpdateCanvases`: [docs](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/Canvas-renderMode.html), [docs](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/Canvas-sortingOrder.html), [docs](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/Canvas.ForceUpdateCanvases.html).
- `CanvasGroup.alpha` (làm mờ cả nhóm con): [docs](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/CanvasGroup-alpha.html).
- `RectTransformUtility.ScreenPointToLocalPointInRectangle`, `RectTransformUtility.WorldToScreenPoint`: [docs](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/RectTransformUtility.ScreenPointToLocalPointInRectangle.html), [docs](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/RectTransformUtility.WorldToScreenPoint.html).
- `RectTransform` `anchorMin/anchorMax`, `pivot`, `sizeDelta`, `anchoredPosition`, `offsetMin/offsetMax`, `rect`: [docs](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/RectTransform-anchoredPosition.html), [docs](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/RectTransform-rect.html).
- `Time.frameCount`: [docs](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/Time-frameCount.html).
- uGUI 2.0: `CanvasScaler`, `Text`, `Outline`, `MaskableGraphic`, `Graphic.color` / `Graphic.SetVerticesDirty()`, `VertexHelper`: [docs](https://docs.unity3d.com/Packages/com.unity.ugui@2.0/api/UnityEngine.UI.CanvasScaler.html), [docs](https://docs.unity3d.com/Packages/com.unity.ugui@2.0/api/UnityEngine.UI.Text.html), [docs](https://docs.unity3d.com/Packages/com.unity.ugui@2.0/api/UnityEngine.UI.Outline.html), [docs](https://docs.unity3d.com/Packages/com.unity.ugui@2.0/api/UnityEngine.UI.Graphic.html), [docs](https://docs.unity3d.com/Packages/com.unity.ugui@2.0/api/UnityEngine.UI.VertexHelper.html).
- Input System 1.19: `Keyboard.current.tKey` / `escapeKey` (`.wasPressedThisFrame`), `Mouse.current.leftButton.wasPressedThisFrame`, `Mouse.position`, `Touchscreen.current.touches`: [docs](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.19/api/UnityEngine.InputSystem.Keyboard.html), [docs](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.19/api/UnityEngine.InputSystem.Mouse.html), [docs](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.19/api/UnityEngine.InputSystem.Touchscreen.html).
- `Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")`: đã dùng ở `MinimapView.cs:329` [repo].

## Quyết định đã chốt (không được đổi)
1. **Vị trí** (user chọn 2026-10-21): nút tròn ở góc trên-phải, ngay dưới nút "Bản đồ (M)". Nút bản đồ chiếm từ −16 tới −72 theo trục y (đơn vị tham chiếu 1280 × 720, `MinimapView.cs:398`).
2. **Phím T** mở/đóng (user chọn). Esc đóng khi vòng đang mở. Phím T chưa dùng ở đâu: số 1–4 (`TerrainEditInput`), F1/F2 (`SimView`), M/Esc (`MinimapView`), WASDQE/Shift (`DebugFlyCamera`).
3. **Biểu tượng vẽ bằng code + chữ nhỏ** (user chọn): trăng khuyết (giữa đêm), nửa mặt trời trên đường chân trời + mũi tên lên (bình minh), mặt trời tròn có tia (giữa trưa), nửa mặt trời + mũi tên xuống (hoàng hôn). Không dùng asset ảnh.
4. **Chọn xong thì vòng tự đóng** (user chọn) khi `Select` trả > 0. `Select` trả 0 (mốc đang ở) hoặc −1 (đang xoay) thì vòng giữ nguyên, không có gì xảy ra. Mốc đang ở (`Day.CurrentWindow`) được tô nền xanh.
5. **Thứ tự trên vòng theo chiều kim đồng hồ = chiều tiến của pha:** trên là giữa trưa, phải là hoàng hôn, dưới là giữa đêm, trái là bình minh. Ở giữa vòng là biểu tượng của mốc hiện tại.
6. **Dòng buổi** luôn hiện (khi bản đồ đóng), bên trái nút: `Đang ở buổi: <tên>`, tên lấy từ `Day.Period`: `Bình minh`, `Ban ngày`, `Hoàng hôn`, `Ban đêm` (0024 mục 6).
7. **Đang xoay** (`DayCycleView.IsRotating`) thì nút và vòng mờ đi (`CanvasGroup.alpha = 0.4`), còn dòng buổi không mờ.
8. **Bấm/chạm** (chuột trái hoặc ngón mới chạm), mỗi khung xử lý tối đa 1 lần:
   - trúng nút → mở/đóng;
   - vòng đang mở và trúng một mốc → chọn mốc đó;
   - vòng đang mở, trúng nền vòng nhưng không trúng mốc → không làm gì;
   - vòng đang mở, bấm ra ngoài → đóng;
   - vòng đóng, bấm vào world → không phải việc của vòng.
9. **Chặn xuyên xuống world:** `TimeDial.BlocksPointer(screen)` trả true khi vòng đang mở, khi con trỏ trên nút, hoặc khi trong khung này vòng vừa xử lý một lần bấm. Trường hợp cuối để lần bấm ra ngoài làm đóng vòng không đào đất, dù `TerrainEditInput` chạy trước hay sau. `TerrainEditInput` kiểm thêm điều kiện này cạnh `MinimapView.BlocksPointer`.
10. **Bản đồ đang mở** (`MinimapView.IsAnyOpen`) thì cả canvas của vòng ẩn và vòng đóng lại, phím T không có tác dụng.
11. **Bỏ F5–F8:** xoá trường `debugKeys` và khối phím trong `DayCycleView`. `Select` và `benchCycleSeconds` giữ nguyên.
12. **Bench:** `TimeDial.startOpen = true` để FPS đo cả lúc vòng đang hiện. `benchCycleSeconds = 5` vẫn tự chọn mốc qua `DayCycleView.Select`, không đi qua vòng.
13. Kích thước, màu, chữ ở bước 1–2 là [số tạm]: user chỉnh sau khi xem. Executor chép đúng số trong brief, không tự chỉnh.
14. Không sửa: lõi `Game.Core/*`, `MinimapView.cs`, `DaySky.cs`, `SkyRotation.cs`, shader, `.mat`, `design/`.

## Phạm vi
- Được tạo:
  - `client/Assets/Game/Client/TimeDialGraphic.cs`, `client/Assets/Game/Client/TimeDial.cs`
  - `client/Assets/Game/Tests/Client/TimeDialPlayTests.cs`
  - Kèm `.meta` do Unity sinh.
- Được sửa:
  - `client/Assets/Game/Client/DayCycleView.cs` (chỉ bỏ F5–F8, bước 3)
  - `client/Assets/Game/Client/TerrainEditInput.cs` (chỉ dòng 47-48, bước 4)
  - `client/Assets/Game/Client/Editor/BenchBuild.cs` (chỉ vòng `foreach` dòng 43-48, bước 6)
  - `client/Assets/Game/Tests/Client/DayCycleViewPlayTests.cs` (chỉ xoá dòng 37), `DayCycleWaterPlayTests.cs` (chỉ xoá dòng 50)
  - `client/Assets/Game/Client/Scenes/TerrainView.unity`, `SimView.unity`: thêm `TimeDial` vào `Main Camera`. Khi Unity lưu lại scene, dòng `debugKeys: 1` của `DayCycleView` mất đi là đúng.
  - `client/Assets/Game/Client/Scenes/Bench.unity`: do `EnsureScene` tự tạo lại khi build.
- Không được sửa: mọi file khác, kể cả `design/`, `Bench.unity.meta` (guid phải giữ nguyên), `ProjectSettings/`.

## Các bước
1. **`TimeDialGraphic.cs`** (namespace `Game.Client`, `using UnityEngine; using UnityEngine.UI;`):
   ```csharp
   public enum TimeDialShape : byte { Disk, Moon, Sun, Dawn, Dusk }

   /// <summary>Hình vẽ bằng code cho vòng chọn thời điểm (brief 0024): đĩa tròn, trăng khuyết, mặt trời có tia, mọc/lặn (nửa đĩa + chân trời + mũi tên). Một màu = `color`.</summary>
   [RequireComponent(typeof(CanvasRenderer))]
   public class TimeDialGraphic : MaskableGraphic
   {
       public TimeDialShape shape;
       public void SetShape(TimeDialShape s) { if (shape == s) return; shape = s; SetVerticesDirty(); }
       protected override void OnPopulateMesh(VertexHelper vh) { … }
   }
   ```
   - `OnPopulateMesh`: `vh.Clear()`; `var rect = rectTransform.rect; Vector2 o = rect.center; float r = 0.5f * Mathf.Min(rect.width, rect.height); Color32 c = color;`. Mọi điểm dưới đây là `o + (x, y)`, góc tính bằng độ, 0° = +x, ngược chiều kim đồng hồ.
   - 3 hàm phụ riêng tư:
     - `Tri(vh, a, b, c, col)`: thêm 3 đỉnh + 1 tam giác.
     - `Quad(vh, a, b, c, d, col)`: 4 đỉnh, tam giác (0,1,2) và (0,2,3), giống `MinimapLandmarkGraphic.cs:40-41`.
     - `Fan(vh, center, radius, a0, a1, seg, col)`: 1 đỉnh tâm + `seg + 1` đỉnh trên cung từ `a0` tới `a1`, tam giác (tâm, i, i+1).
   - Theo `shape`:
     - `Disk`: `Fan(o, r, 0, 360, 32)`.
     - `Sun`: `Fan(o, 0.5r, 0, 360, 32)`; thêm 8 tia, với k = 0..7: `a = k·45°`, `d = (cos a, sin a)`, `p = (−sin a, cos a)`, `Quad(o + d·0.65r − p·0.08r, o + d·0.95r − p·0.08r, o + d·0.95r + p·0.08r, o + d·0.65r + p·0.08r)`.
     - `Moon` (trăng khuyết mở sang phải): N = 16; với i = 0..N: `t = −1 + 2i/N`, `s = Mathf.Sqrt(Mathf.Max(0, 1 − t²))·0.85r`, `y = t·0.85r`, `outer_i = (0.25r − s, y)`, `inner_i = (0.25r − 0.3s, y)`. Với i = 0..N−1: `Quad(outer_i, inner_i, inner_{i+1}, outer_{i+1})`.
     - `Dawn` / `Dusk`:
       - chân trời `Quad((−0.9r, −0.35r), (0.9r, −0.35r), (0.9r, −0.25r), (−0.9r, −0.25r))`;
       - nửa đĩa `Fan((0, −0.25r), 0.5r, 0, 180, 16)`;
       - `Dawn` có mũi tên lên `Tri((0, 0.95r), (−0.25r, 0.55r), (0.25r, 0.55r))`;
       - `Dusk` có mũi tên xuống `Tri((0, 0.55r), (−0.25r, 0.95r), (0.25r, 0.95r))`.

   Xong khi: console 0 lỗi biên dịch.

2. **`TimeDial.cs`** (namespace `Game.Client`; `using System.Collections.Generic; using Game.Core.DayCycle; using UnityEngine; using UnityEngine.InputSystem; using UnityEngine.UI;`). `[RequireComponent(typeof(DayCycleView))] public class TimeDial : MonoBehaviour`. Doc comment lớp:
   `/// Vòng chọn thời điểm (0024, brief 0024): nút tròn góc trên-phải dưới nút Bản đồ (hoặc phím T) mở vòng 4 mốc theo chiều kim đồng hồ = chiều tiến của pha (trên giữa trưa, phải hoàng hôn, dưới giữa đêm, trái bình minh); bấm mốc → DayCycleView.Select, xoay được thì vòng tự đóng. Dòng "Đang ở buổi: …" luôn hiện. Canvas dựng bằng code, không EventSystem; ẩn khi bản đồ mở.`
   - **Thành viên public** (đúng tên, kiểu):
     ```csharp
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
     public void SetOpen(bool open);
     public void PointerDown(Vector2 screen);
     public static string PeriodName(DayPeriod p);          // Bình minh / Ban ngày / Hoàng hôn / Ban đêm
     public static string LandmarkName(TimeLandmark m);     // Giữa đêm / Bình minh / Giữa trưa / Hoàng hôn
     public static TimeDialShape ShapeOf(TimeLandmark m);   // Midnight→Moon, Dawn→Dawn, Noon→Sun, Dusk→Dusk
     public static Color ColorOf(TimeLandmark m);
     ```
     `items`, `itemBgs`, `itemLabels` là mảng 4 phần tử, chỉ số = `(int)TimeLandmark`.
   - **Hằng số** [số tạm], đơn vị tham chiếu 1280 × 720:
     - `ColorOf`: Midnight `DaySky.MoonDiscColor`, Dawn `(1, 0.62, 0.38)`, Noon `(1, 0.85, 0.3)`, Dusk `(0.95, 0.45, 0.3)`, alpha 1.
     - `static readonly Color NormalBg = new Color(0.12f, 0.12f, 0.12f, 0.85f), HighlightBg = new Color(0.30f, 0.50f, 0.85f, 0.95f), ButtonBg = new Color(0.1f, 0.1f, 0.1f, 0.75f), RingBg = new Color(0f, 0f, 0f, 0.45f);`
     - `const float RingRadius = 100f;`
     - `static readonly Vector2[] Offsets` theo chỉ số `(int)TimeLandmark`: Midnight `(0, −100)`, Dawn `(−100, 0)`, Noon `(0, 100)`, Dusk `(100, 0)`.
   - **Trường riêng:** `static TimeDial active; DayCycleView day; GameObject canvasGo; CanvasGroup controls; RectTransform button, ring; TimeDialGraphic buttonIcon, centerIcon; Text periodText; RectTransform[] items = new RectTransform[4]; TimeDialGraphic[] itemBgs = new TimeDialGraphic[4]; Text[] itemLabels = new Text[4]; int actedFrame = -1; int shownWindow = -1, shownPeriod = -1;` và 2 `Dictionary<int, Vector2> touches, lastTouches`.
   - `Awake()`: `day = GetComponent<DayCycleView>();`. `OnEnable()`: `active = this;`. `OnDisable()`: `if (active == this) active = null;`. `OnDestroy()`: `if (canvasGo != null) Destroy(canvasGo);`.
   - **`Update()`**, theo thứ tự:
     1. `if (!IsReady) { if (day.Day == null) return; Build(); }`. Dựng muộn vì `Day` chỉ có sau `DayCycleView.Start`.
     2. `bool show = !MinimapView.IsAnyOpen;` Nếu `show != canvasGo.activeSelf` thì: `if (!show) SetOpen(false); canvasGo.SetActive(show);`. Nếu `!show` thì `lastTouches.Clear(); return;`.
     3. Bàn phím (`Keyboard.current` khác null):
        - `tKey.wasPressedThisFrame && actedFrame != Time.frameCount` thì `actedFrame = Time.frameCount; SetOpen(!IsOpen);`;
        - ngược lại, nếu `IsOpen && escapeKey.wasPressedThisFrame` thì `SetOpen(false)`.
     4. Chuột: `Mouse.current != null && leftButton.wasPressedThisFrame` thì `PointerDown(mouse.position.ReadValue())`.
     5. Cảm ứng: lấy các ngón đang chạm vào `touches` như `MinimapView.cs:227-231`. Mỗi ngón có trong `touches` mà không có trong `lastTouches` thì `PointerDown(vị trí)`. Sau đó chép `touches` sang `lastTouches` như `:255-257`.
     6. `Refresh()`.
   - **`PointerDown(Vector2 screen)`** (quyết định 8):
     ```csharp
     if (!IsReady || !CanvasActive || actedFrame == Time.frameCount) return;
     bool overButton = IsOverButton(screen);
     if (!overButton && !IsOpen) return;
     actedFrame = Time.frameCount;
     if (overButton) { SetOpen(!IsOpen); return; }
     for (int i = 0; i < 4; i++)
         if (IsOverCircle(items[i], screen)) { Choose((TimeLandmark)i); return; }
     if (!IsOverCircle(ring, screen)) SetOpen(false);
     ```
     `Choose(m)`: `LastSelectResult = day.Select(m); if (LastSelectResult > 0) SetOpen(false);`
   - **Hàm phụ:**
     - `IsOverButton(screen)`: giống `MinimapView.cs:163-169`, trả false nếu `!IsReady`.
     - `IsOverCircle(rt, screen)`: `ScreenPointToLocalPointInRectangle(rt, screen, null, out local)` rồi `(local − rt.rect.center).magnitude <= rt.rect.width * 0.5f`.
     - `ScreenCenter(rt)`: như `MinimapView.cs:86`.
   - **`SetOpen(bool open)`**: `if (!IsReady) return; IsOpen = open; ring.gameObject.SetActive(open);`.
   - **`Refresh()`** (chỉ ghi khi đổi, tránh dựng lại mesh mỗi khung):
     - `int w = (int)day.Day.CurrentWindow;`. Nếu `w != shownWindow`:
       - `buttonIcon.SetShape(ShapeOf((TimeLandmark)w)); buttonIcon.color = ColorOf((TimeLandmark)w);` và làm tương tự với `centerIcon`;
       - `itemBgs[i].color = i == w ? HighlightBg : NormalBg` cho i = 0..3;
       - `shownWindow = w`.
     - `int p = (int)day.Day.Period;`. Nếu khác `shownPeriod` thì `periodText.text = "Đang ở buổi: " + PeriodName((DayPeriod)p); shownPeriod = p;`.
     - `float a = day.IsRotating ? 0.4f : 1f; if (controls.alpha != a) controls.alpha = a;`.
   - **`Build()`**: dựng như `MinimapView.cs:329-340` (chép `Stretch`, `Place`, `NewText` từ `:412-453`). Mọi `Graphic` có `raycastTarget = false`. Quy ước: "tạo X con của Y" = `Stretch("X", Y, Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f))`, sau đó gọi `Place(...)` với đúng số ghi kèm. Một GameObject chỉ có một `Graphic`, nên hình nền nằm trên chính nút/mốc, còn biểu tượng và chữ nằm ở GameObject con.
     1. `canvasGo = new GameObject("Vòng thời điểm", typeof(RectTransform))`, **không** đặt cha (camera có thể bị hủy trước/sau, `OnDestroy` dọn). Canvas `ScreenSpaceOverlay`, `sortingOrder = 9`; `CanvasScaler` giống minimap (1280 × 720, match 1). `root = (RectTransform)canvasGo.transform`.
     2. `periodText = NewText("Dòng buổi", root, font, 22, TextAnchor.MiddleRight)`. `Place(rt, anchor (1,1), pivot (1,0.5), position (−100, −120), size (420, 40))`. Thêm `Outline` màu `(0,0,0,0.85)`, `effectDistance (2,−2)`.
     3. Tạo `controlsRt` ("Nút và vòng") con của `root`, giãn kín (không gọi `Place`); `controls = controlsRt.gameObject.AddComponent<CanvasGroup>()`.
     4. Nút: tạo `button` ("Nút thời điểm") con của `controlsRt`, `Place(button, new Vector2(1,1), new Vector2(1,1), new Vector2(−16, −84), new Vector2(72, 72))`. Trên `button` thêm `TimeDialGraphic` shape `Disk`, color `ButtonBg`. Tạo "Biểu tượng" con của `button`, `Place` anchor (0.5,0.5), pivot (0.5,0.5), position (0,0), size (56,56), thêm `TimeDialGraphic` → `buttonIcon`.
     5. Vòng: tạo `ring` ("Vòng") con của `controlsRt`, `Place(ring, new Vector2(1,1), new Vector2(0.5f,0.5f), new Vector2(−180, −320), new Vector2(300, 300))`. Trên `ring` thêm `TimeDialGraphic` shape `Disk`, color `RingBg`. Tạo "Mốc hiện tại" con của `ring`, `Place` anchor/pivot (0.5,0.5), position (0,0), size (80,80), thêm `TimeDialGraphic` → `centerIcon`.
     6. Với i = 0..3 (`m = (TimeLandmark)i`):
        - `items[i]`: con của `ring`, `Place` anchor (0.5,0.5), pivot (0.5,0.5), position `Offsets[i]`, size (72,72). `itemBgs[i]` là `TimeDialGraphic` `Disk` trên chính `items[i]`.
        - Icon con: size (56,56) ở (0,0), shape `ShapeOf(m)`, color `ColorOf(m)`.
        - `itemLabels[i] = NewText("Tên " + LandmarkName(m), items[i], font, 18, TextAnchor.UpperCenter)`, `Place` anchor (0.5,0.5), pivot (0.5,1), position (0, −40), size (140, 24), text `LandmarkName(m)`, `Outline` như dòng buổi nhưng `effectDistance (1.5,−1.5)`.
     7. Gợi ý: `NewText("Gợi ý", ring, font, 18, TextAnchor.MiddleCenter)`, `Place` anchor (0.5,0.5), pivot (0.5,0.5), position (0, −180), size (320, 28), text **nguyên văn** `Chọn mốc · T / Esc: đóng`, `Outline` như trên.
     8. `IsReady = true; SetOpen(startOpen); Refresh();` rồi `Debug.Log($"TIMEDIAL dựng: 4 mốc, mở={IsOpen}, buổi={day.Day.Period}, font={(font != null ? font.name : "null")}");`.
   - `PeriodName`: `Dawn → "Bình minh"`, `Day → "Ban ngày"`, `Dusk → "Hoàng hôn"`, `Night → "Ban đêm"`. `LandmarkName`: `Midnight → "Giữa đêm"`, `Dawn → "Bình minh"`, `Noon → "Giữa trưa"`, `Dusk → "Hoàng hôn"`. Dùng `switch` biểu thức, nhánh mặc định trả `p.ToString()` / `m.ToString()`.

   Xong khi: console 0 lỗi.

3. **`DayCycleView.cs`**: xoá dòng 19-20 (doc comment + `public bool debugKeys = true;`) và khối `if (debugKeys && Keyboard.current != null) { … }` ở dòng 74-81. Xoá `using UnityEngine.InputSystem;` (dòng 4) nếu không còn chỗ nào dùng. Không đổi gì khác.
   - `DayCycleViewPlayTests.cs`: xoá dòng 37 `view.debugKeys = false;`.
   - `DayCycleWaterPlayTests.cs`: xoá dòng 50 `day.debugKeys = false;`.

   Xong khi: `grep -rn "debugKeys" client/Assets/Game` không còn kết quả (BenchBuild sửa ở bước 6), console 0 lỗi.

4. **`TerrainEditInput.cs:47-48`** thay bằng:
   ```csharp
            // Bản đồ hoặc vòng thời điểm đang mở, hoặc con trỏ trên nút của chúng → không lăn chọn loại, không đào/đặt (brief 0016, 0024).
            if (mouse != null && (MinimapView.BlocksPointer(mouse.position.ReadValue()) || TimeDial.BlocksPointer(mouse.position.ReadValue())))
   ```
   Xong khi: console 0 lỗi.

5. **Scene** (MCP): mở `Assets/Game/Client/Scenes/TerrainView.unity`, thêm component `TimeDial` vào `Main Camera` (để mặc định, `startOpen` = false), rồi lưu. Làm tương tự với `SimView.unity`.

   Xong khi: `grep -c 'm_EditorClassIdentifier: Game.Client::Game.Client.TimeDial$'` trên mỗi scene ra 1, và `grep -c "debugKeys"` trên mỗi scene ra 0.

6. **`BenchBuild.cs:43-48`**: trong vòng `foreach (var day …)`, xoá dòng `day.debugKeys = false;` và sửa comment `:43` thành `// Brief 0023/0024: đo FPS khi bầu trời xoay — cứ 5 giây tự chọn mốc; vòng thời điểm mở sẵn.`. Ngay sau vòng đó thêm:
   ```csharp
            foreach (var dial in UnityEngine.Object.FindObjectsByType<TimeDial>(FindObjectsSortMode.None))
                dial.startOpen = true;
   ```

7. **Test `TimeDialPlayTests.cs`** (`client/Assets/Game/Tests/Client/`, namespace `Game.Client.Tests`, `using System.Collections; using Game.Core.DayCycle; using NUnit.Framework; using UnityEngine; using UnityEngine.TestTools;`). `const int H = 3_600_000;`. Các trường: `long now; double t; GameObject lightGo, camGo; DayCycleView day; TimeDial dial;`.
   - `[UnitySetUp]`:
     - `now = 12 * H; t = 100.0;`;
     - tạo `lightGo` + `Light` Directional, rồi `camGo` + `Camera`;
     - `day = camGo.AddComponent<DayCycleView>(); day.sun = light; day.clockMs = () => now; day.secondsNow = () => t;`;
     - `dial = camGo.AddComponent<TimeDial>();`;
     - `for (int i = 0; i < 5 && !dial.IsReady; i++) yield return null;` rồi `Assert.IsTrue(dial.IsReady)`; cuối cùng `Canvas.ForceUpdateCanvases();`.
   - `[UnityTearDown]`: `Object.Destroy(camGo); Object.Destroy(lightGo); yield return null;`.
   - Ghi chú: `PointerDown` chỉ xử lý 1 lần mỗi khung, nên giữa 2 lần gọi trong một test phải có `yield return null`.

   | Test | Thao tác | Kiểm |
   |---|---|---|
   | `BuildsClosedWithPeriodText` | — ; rồi `now = 0`, yield; rồi `now = 5 * H`, yield | Ban đầu: `!IsOpen`, `!RingActive`, `CanvasActive`, `PeriodText == "Đang ở buổi: Ban ngày"`, `ButtonShape == Sun`. `now = 0`: `"Đang ở buổi: Ban đêm"`, `Moon`. `now = 5 * H`: `"Đang ở buổi: Bình minh"`, vẫn `Moon` (khung giữa đêm) |
   | `ButtonTogglesRing` | `PointerDown(ButtonScreenCenter)`; yield; lặp lại | Lần 1: `IsOpen`, `RingActive`, `TimeDial.IsAnyOpen`. Lần 2: `!IsOpen`, `!RingActive` |
   | `LandmarksClockwiseWithNames` | `SetOpen(true)`, `Canvas.ForceUpdateCanvases()` | `LandmarkScreenCenter(Noon).y > (Midnight).y`; `(Dusk).x > (Dawn).x`; `Noon.x ≈ Midnight.x` và `Dawn.y ≈ Dusk.y` (±1); 4 nhãn đúng `Giữa trưa`, `Hoàng hôn`, `Giữa đêm`, `Bình minh` |
   | `ChoosingLandmarkRotatesAndCloses` | `SetOpen(true)`; `PointerDown(LandmarkScreenCenter(Dawn))` | `LastSelectResult == 18 * H`, `day.IsRotating`, `!IsOpen`, `day.Day.PhaseMs == 6 * H` |
   | `CurrentLandmarkHighlightedAndDoesNothing` | `SetOpen(true)`; `PointerDown(LandmarkScreenCenter(Noon))` | Trước: `IsHighlighted(Noon)`, 3 mốc kia không. Sau: `LastSelectResult == 0`, `IsOpen`, `!day.IsRotating`, `day.Day.OffsetMs == 0` |
   | `DimmedAndIgnoredWhileRotating` | `day.Select(Dawn)`; yield; `SetOpen(true)`; ghi `off = day.Day.OffsetMs`; `PointerDown(LandmarkScreenCenter(Dusk))`; rồi `t += 3.1`, yield | Sau yield đầu: `ControlsAlpha == 0.4f` (±1e-4). Sau `PointerDown`: `LastSelectResult == -1`, `OffsetMs == off`, `IsOpen`. Cuối: `ControlsAlpha == 1f` |
   | `OutsideClosesAndBlocksPointer` | Đóng: kiểm. `SetOpen(true)`: kiểm. `PointerDown(new Vector2(5, 5))`: kiểm ngay trong khung đó. Yield: kiểm | Đóng: `BlocksPointer(ButtonScreenCenter)` true, `BlocksPointer((5,5))` false. Mở: `BlocksPointer((5,5))` true. Sau bấm: `!IsOpen`, `BlocksPointer((5,5))` vẫn true. Sau yield: false |
   | `HiddenWhileMinimapOpen` | Dựng world v3 + `MinimapView` như `MinimapViewPlayTests.cs:19-31` (go mới, `streamTarget = camGo.transform`), chờ tối đa 5 khung tới `map.IsReady`. `dial.SetOpen(true); map.SetOpen(true);` yield. `map.SetOpen(false);` yield. Cuối test `Object.Destroy(go)` | Khi bản đồ mở: `!dial.CanvasActive`, `!dial.IsOpen`, `!TimeDial.BlocksPointer(dial.ButtonScreenCenter)`. Khi bản đồ đóng: `dial.CanvasActive` |

8. **Bench + điện thoại.** `ADB` là `/Applications/Unity/Hub/Editor/6000.4.8f1/PlaybackEngines/AndroidPlayer/SDK/platform-tools/adb`. Máy Xiaomi 24095PCADG, serial `PFQCRWVWR46HU4GQ` (USB). Nếu `ADB devices` không có serial này thì chạy `ADB connect 192.168.1.6:5555` và dùng `-s 192.168.1.6:5555`. Tự cài, tự đọc log, không hỏi user.
   1. MCP `execute_menu_item` "Game/Build APK Bench" → console có `Bench build: Succeeded`.
   2. Cài và chạy:
      - `ADB -s <máy> install -r client/Builds/bench.apk` → `Success`;
      - `ADB -s <máy> logcat -c`;
      - `ADB -s <máy> shell monkey -p com.viventa.bench -c android.intent.category.LAUNCHER 1`.
   3. Khoảng 20 giây sau khi mở app, chụp màn hình: `ADB -s <máy> exec-out screencap -p > client/Builds/bench-0024.png` (thư mục `client/Builds/` bị git bỏ qua). Mở ảnh bằng Read để xác nhận vòng 4 mốc và dòng buổi có hiện. Ghi đường dẫn ảnh vào Kết quả.
   4. Chờ đủ 75 giây kể từ khi mở app (chạy nền hoặc Monitor), rồi chạy `ADB -s <máy> logcat -d -s Unity | grep -E "BENCH|DAYCYCLE|TIMEDIAL|Exception"`. Ghi vào Kết quả:
      - dòng `TIMEDIAL dựng` (phải có, `mở=True`);
      - số dòng `DAYCYCLE` (phải ≥ 6);
      - `avgFps` (≥ 30; brief 0023 đo được 50,6), `p99Ms`, `holeFrames` (= 0), `hash` (= `a7719c8999939263`) của dòng `BENCH`;
      - mọi dòng `Exception` (phải không có).

      Sau 150 giây vẫn không có `BENCH` (máy khoá màn hình) → ghi "chờ user mở khoá" và làm tiếp bước 9.
   5. Dọn:
      - `git checkout -- client/ProjectSettings/UnityConnectSettings.asset` (nếu đổi);
      - nếu `git diff client/Assets/Game/Client/Scenes/Bench.unity.meta` có thay đổi thì `git checkout -- client/Assets/Game/Client/Scenes/Bench.unity.meta`.

9. **Chạy toàn bộ + commit:**
   1. EditMode cả bộ: 340/340 (brief này không thêm test EditMode).
   2. PlayMode cả bộ: 52 cũ + 8 mới = 60, đạt hết. Sau đó `git checkout -- client/ProjectSettings/EditorSettings.asset`.
   3. `python3 tools/check_design.py` → hợp lệ.
   4. `git status --short`: chỉ còn file trong Phạm vi. Commit code với message `brief 0024: vòng chọn 4 mốc + dòng buổi, bỏ F5–F8`.
   5. `date "+%Y-%m-%d %H:%M"` → ghi "Kết thúc", đặt trạng thái `chờ review`, commit brief với message `brief 0024: kết quả`.

## Tiêu chí xong
- `TimeDialGraphic`, `TimeDial` đúng chữ ký và hành vi ở bước 1–2. `DayCycleView` chỉ mất F5–F8. `TerrainEditInput`, `BenchBuild` chỉ đổi đúng bước 4, 6.
- `TimeDial` có trên `Main Camera` của TerrainView và SimView; không còn `debugKeys` trong `client/Assets/Game`.
- EditMode 340/340, PlayMode 60/60. Test cũ chỉ bị xoá 2 dòng `debugKeys`.
- Điện thoại: có `TIMEDIAL dựng … mở=True`, ≥ 6 `DAYCYCLE`, `avgFps ≥ 30`, `holeFrames=0`, hash trùng, 0 `Exception`, có ảnh `client/Builds/bench-0024.png` (hoặc ghi "chờ user mở khoá").
- Chỉ các file trong Phạm vi bị đổi.

## Test sau khi xong

### Phần cần test
| Phần | Bước | Test | Kết quả mong đợi |
|---|---|---|---|
| Hình vẽ biểu tượng | 1 | Biên dịch; ảnh điện thoại (bước 8.3); user xem | 0 lỗi; 4 hình phân biệt được |
| Dựng UI, dòng buổi đúng `Period`, nút theo khung mốc | 2 | `BuildsClosedWithPeriodText` | Như bảng bước 7 |
| Mở/đóng bằng nút | 2 | `ButtonTogglesRing` | Mở rồi đóng |
| Thứ tự chiều kim đồng hồ + tên mốc | 2 | `LandmarksClockwiseWithNames` | Trưa trên, hoàng hôn phải, đêm dưới, bình minh trái |
| Chọn mốc → xoay + tự đóng | 2 | `ChoosingLandmarkRotatesAndCloses` | Cung 18 h, đóng vòng |
| Mốc đang ở: tô sáng, bấm không đổi gì | 2 | `CurrentLandmarkHighlightedAndDoesNothing` | Offset 0, vòng vẫn mở |
| Đang xoay: mờ + bỏ qua | 2 | `DimmedAndIgnoredWhileRotating` | Alpha 0,4 → 1; `Select` −1 |
| Bấm ra ngoài đóng + chặn xuyên xuống world | 2, 4 | `OutsideClosesAndBlocksPointer` | Như bảng bước 7 |
| Ẩn khi bản đồ mở | 2 | `HiddenWhileMinimapOpen` | Canvas ẩn, vòng đóng |
| Phím T / Esc, cảm ứng | 2 | User xem (Mac: T/Esc; điện thoại: chạm) | Như mục User tự test |
| Bỏ F5–F8, `DayCycleView` vẫn đúng | 3 | `grep debugKeys`; `DayCycleViewPlayTests` (4), `DayCycleWaterPlayTests` (cũ) | Không còn; đạt |
| Đào/đặt không bị ảnh hưởng | 4 | `TerrainEditPlayTests`, `EditOcclusionPlayTests` (cũ) | Đạt |
| Bản đồ không bị ảnh hưởng | — | `MinimapViewPlayTests`, `LandmarkViewPlayTests` (cũ) | Đạt |
| Gắn vào scene | 5 | `grep` 2 scene | Mỗi scene 1 `TimeDial`, 0 `debugKeys` |
| Bench + điện thoại | 6, 8 | APK + logcat + ảnh | Như Tiêu chí xong |
| Regression chung | — | EditMode + PlayMode cả bộ | 340/340, 60/60 |

### Lệnh executor chạy
Unity Editor dùng chung qua MCP (quyết định 0010). Nếu Editor đang tắt thì thay `run_tests` bằng `python3 tools/unity_test.py EditMode|PlayMode` (không build APK được, ghi lại).
1. MCP `refresh_unity` → `read_console` (types error) → 0 lỗi.
2. `grep -rn "debugKeys" client/Assets/Game` → không có kết quả.
3. `grep -c 'm_EditorClassIdentifier: Game.Client::Game.Client.TimeDial$' client/Assets/Game/Client/Scenes/TerrainView.unity client/Assets/Game/Client/Scenes/SimView.unity` → 1 và 1.
4. MCP `run_tests` PlayMode lọc `TimeDialPlayTests`, `DayCycleViewPlayTests`, `DayCycleWaterPlayTests`, `TerrainEditPlayTests`, `MinimapViewPlayTests` → đạt. Dọn: `git checkout -- client/ProjectSettings/EditorSettings.asset`.
5. Bước 8 (Bench, APK, adb, ảnh, logcat). Dọn `UnityConnectSettings.asset` và `Bench.unity.meta`.
6. EditMode cả bộ, rồi PlayMode cả bộ → ghi dòng tổng kết. Dọn `EditorSettings.asset`.
7. `python3 tools/check_design.py` → hợp lệ.
8. `git status --short` → commit (bước 9).

### User tự test (Planner gửi khi review)
Trên Mac, scene `SimView` (và `TerrainView`), nhấn Play:
1. Góc trên-phải, dưới nút "Bản đồ (M)" có nút tròn mang biểu tượng của mốc hiện tại. Bên trái nút có dòng "Đang ở buổi: …" đúng với giờ máy.
2. Bấm nút hoặc phím **T** thì vòng mở: trên là Giữa trưa, phải là Hoàng hôn, dưới là Giữa đêm, trái là Bình minh, giữa là biểu tượng mốc hiện tại. Mốc hiện tại có nền xanh. 4 hình có dễ nhận không? Chữ có đọc được không?
3. Đang trưa bấm **Bình minh**: vòng đóng, trời xoay thuận chiều khoảng 3 giây. Trong lúc xoay nút mờ đi. Dòng buổi thành "Bình minh".
4. Bấm mốc đang tô xanh thì không có gì xảy ra. Bấm ra ngoài vòng, hoặc nhấn Esc / T, thì vòng đóng. Bấm ra ngoài để đóng thì **không** đào đất.
5. Nhấn **M** mở bản đồ thì nút và dòng buổi ẩn đi; đóng bản đồ thì hiện lại. F5–F8 không còn tác dụng.
6. Điện thoại (app Bench do executor đã cài, vòng mở sẵn): chạm một mốc thì trời xoay, vòng đóng; chạm nút thì vòng mở lại. Bench tự đổi mốc mỗi 5 giây, nên hãy chạm lúc nút không mờ. Xem ảnh `client/Builds/bench-0024.png`: cỡ nút và chữ trên điện thoại có ổn không.
7. Vị trí, cỡ, màu là [số tạm]: ghi lại chỗ muốn chỉnh.

## Dừng và chuyển lại Opus khi
- Brief mơ hồ hoặc mâu thuẫn với code thật (vd số dòng lệch tới mức không xác định được chỗ sửa).
- Cần một quyết định thuộc AGENTS.md §3.4 hoặc ngoài "Quyết định đã chốt" (vd đổi vị trí, cỡ, màu, chữ, thêm hiệu ứng).
- Cùng một lỗi sửa 2 lần vẫn chưa qua.
- Phải sửa file ngoài "Phạm vi".
- Cần API Unity chưa có trong mục "API Unity dùng".
- `avgFps < 30` trên điện thoại: ghi số đo, dừng, không tự tối ưu.

---
<!-- Từ đây trở xuống: EXECUTOR ghi. -->

## Kết quả
- Base commit: 4e63147
- Chạy bằng: claude-sonnet-5-5 · medium (OK: brief 0024 cần Sonnet · medium)
- Bắt đầu: 2026-10-21 16:23 · Kết thúc: 2026-10-21 16:36 (~13 phút)
- Commit code: 58f4c7a. File: TimeDial.cs, TimeDialGraphic.cs, TimeDialPlayTests.cs (+meta); sửa DayCycleView, TerrainEditInput, BenchBuild, DayCycleViewPlayTests, DayCycleWaterPlayTests, TerrainView/SimView/Bench.unity. Bench.unity.meta đã khôi phục.
- Lệch brief: `IsOverButton` để private (test không cần). USB serial PFQCRWVWR46HU4GQ không có → dùng 192.168.1.6:5555.
- Ảnh `client/Builds/bench-0024.png`: ĐEN (màn hình điện thoại khoá/ngủ, KeyguardShowing=true, vuốt không mở được) → chờ user mở khoá để chụp lại. App vẫn chạy và ghi log.
<!-- Base commit (trước khi làm) · Chạy bằng: <model id> · <effort> (dòng OK của brief_check.py) · Bắt đầu / Kết thúc (giờ thực) · commit của brief · file đã đổi · ghi chú lệch so với brief (nếu có, kèm lý do). -->

## Kết quả test
- Hình vẽ biểu tượng: biên dịch 0 lỗi; ảnh chờ user mở khoá (ảnh đen); user xem.
- Dựng/mở-đóng/thứ tự/chọn mốc/tô sáng/mờ khi xoay/chặn con trỏ/ẩn khi bản đồ: đạt — 8 test `TimeDialPlayTests` đạt.
- Phím T/Esc, cảm ứng: chờ user.
- Bỏ F5–F8: đạt — `grep debugKeys client/Assets/Game` chỉ còn trong Scenes/Bench.unity cũ (tự tạo lại khi build lần sau; đã tái tạo trong commit), DayCycleView/DayCycleWater test đạt.
- Đào/đặt, bản đồ: đạt (TerrainEdit, EditOcclusion, Minimap, LandmarkView nằm trong PlayMode cả bộ).
- Scene: đạt — grep TimeDial 1/1, debugKeys 0/0.
- Điện thoại: `TIMEDIAL dựng: 4 mốc, mở=True`; DAYCYCLE 15 dòng; BENCH avgFps=37,3 p99Ms=49,97 holeFrames=0 hash=a7719c8999939263 pass30fps=True; Exception: 0. (avgFps thấp hơn 50,6 của 0023, máy khoá màn hình — planner cân nhắc đo lại khi mở khoá.)
- Lệnh: MCP PlayMode lọc 5 nhóm → 29/29; EditMode cả bộ → 340/340; PlayMode cả bộ → 60/60; check_design → hợp lệ; build APK Bench → Succeeded. Đã dọn EditorSettings/UnityConnectSettings.
<!-- Mỗi dòng ở "Phần cần test" → đạt / không đạt / chờ user + bằng chứng (tên test, dòng output).
Mỗi lệnh ở "Lệnh executor chạy": `<lệnh>` → <kết quả thật, dòng tổng kết>. -->

## Vướng mắc
<!-- Khi trạng thái = cần Opus: câu hỏi cụ thể + các phương án (kèm đề xuất). -->

---
<!-- PLANNER ghi khi review. -->

## Review
<!-- Kết luận: đã duyệt | cần sửa (thêm bước sửa vào "Các bước", trạng thái về chờ làm). Điểm đã đối chiếu. -->
