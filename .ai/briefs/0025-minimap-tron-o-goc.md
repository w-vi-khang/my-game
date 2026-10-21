# Brief 0025 — World 7g: ô minimap tròn luôn hiện ở góc trên-phải, bấm để mở bản đồ lớn

> Trạng thái: chờ review
> Review: đầy đủ + user xem
> Model thực thi: Sonnet · medium
> Cập nhật: 2026-10-21 17:14

<!-- Planner (Opus) viết, Executor (Sonnet) làm theo ở PHIÊN KHÁC — executor không có lịch sử chat, không được tự quyết.
Trạng thái: chờ làm → đang làm → chờ review → đã duyệt; hoặc → cần Opus (executor dừng, hỏi ở mục Vướng mắc).
Review: nhẹ (đọc mục Kết quả) | đầy đủ (đọc cả diff) | user xem (có phần user tự kiểm bằng mắt/chơi thử) — có thể ghép "đầy đủ + user xem".
Model thực thi: BẮT BUỘC, đúng dạng `<Model> · <effort>` — Model: Opus | Sonnet | Haiku | Fable; effort: low | medium | high | xhigh | max (chọn theo AGENTS.md §10.2).
Executor kiểm tra model + effort của phiên TRƯỚC khi làm (scripts/brief_check.py); lệch hoặc không xác định được → cảnh báo user và DỪNG.
File được commit. Tự kiểm trước khi giao (AGENTS.md §10.3): executor có phải hỏi gì không? còn chỗ nào bỏ ngỏ 2 phương án? -->

## Mục tiêu
Bỏ nút "Bản đồ (M)". Thay bằng một **ô minimap tròn luôn hiện** ở góc trên-phải, Bắc cố định lên trên, có chữ "N" ở đỉnh vòng. Ô hiện vùng 64 × 64 cột quanh người chơi: ảnh nền, ranh giới map, dấu mốc (chỉ biểu tượng), mũi tên người chơi và tên map đang đứng. Bấm/chạm vào ô thì mở bản đồ lớn như cũ. Trong bảng có nút "Đóng (M)" ở góc trên-phải. Phím M / Esc giữ nguyên. Nút chọn thời điểm (`TimeDial`) dời xuống dưới ô. Đo FPS trên điện thoại.

Task contract: [.ai/tasks/m1-world-buoc-7g-minimap-goc.md](../tasks/m1-world-buoc-7g-minimap-goc.md) (một brief duy nhất của bước 7g).

## Bối cảnh cần đọc
- `client/Assets/Game/Client/MinimapView.cs` (brief 0016–0018). File chính phải sửa:
  - `:21-25` `IsAnyOpen`, `BlocksPointer(screen) => active != null && active.IsReady && (active.IsOpen || active.IsOverButton(screen))`.
  - `:39-58` trường riêng, trong đó `RectTransform frame, button, marker; Text buttonText;` (`:41-42`) và `int toggledFrame = -1;` (`:51`).
  - `:86` `ButtonScreenCenter` tính tâm `button` theo pixel màn hình.
  - `:110-133` `Update()`: dựng muộn → `Texture.Apply` khi `pixelsDirty` → phím M/Esc → `HandleMouse` → `HandleTouches` → `if (IsOpen) Refresh();`.
  - `:135-146` `SetOpen` (`:141` đổi chữ nút). `:148-154` `Toggle()` có chặn 1 lần/khung bằng `toggledFrame`.
  - `:163-169` `IsOverButton`. `:196-205` chuột trái bấm trúng nút → `Toggle()`. `:233-240` ngón mới chạm trúng nút → `buttonTouches.Add` + `Toggle()`.
  - `:260-306` `Refresh()` của bản đồ lớn. Cách đặt dấu người chơi và tính góc (`:296-305`) được chép sang ô nhỏ.
  - `:308-410` `Build()`. Nút cũ ở `:396-400`: `Place(button, (1,1), (1,1), (−16,−16), (180,56))` + `AddImage` + `buttonText`.
  - `:412-453` các hàm phụ `Stretch`, `Place`, `AddImage`, `NewText`.
- `client/Assets/Game/Core/MinimapViewport.cs` (**không sửa**):
  - `:22` `new MinimapViewport(float worldWidth, float worldHeight, float panelWidth, float panelHeight)`.
  - `:34` `PixelsPerColumn = min(PanelWidth/WorldWidth, PanelHeight/WorldHeight) · Zoom`.
  - `:55-60` `WorldToPanel(wx, wz, out px, out py)`.

  Nếu khung có cỡ đúng bằng `(W·k, H·k)` thì zoom là 1, tâm ở giữa world, `PixelsPerColumn = k` và `WorldToPanel(wx, wz) = (wx·k, wz·k)`. Ô nhỏ dùng một viewport như vậy để vẽ ranh giới và đặt dấu mốc.
- `client/Assets/Game/Client/MinimapLinesGraphic.cs:13-52`: `public List<float[]> Lines; public MinimapViewport Viewport; public float Thickness; public int LastVertexCount`. Graphic này cần pivot (0, 0).
- `MinimapLandmarkGraphic.cs:8-10` (`diamond`), `MinimapMarkerGraphic.cs:8` (tam giác chĩa +y).
- `client/Assets/Game/Client/TimeDialGraphic.cs`: `TimeDialShape.Disk` là đĩa tròn 32 đoạn phủ rect, một màu `color`, `public TimeDialShape shape`. Ô nhỏ dùng nó làm viền, mặt nạ và nền chữ N.
- `client/Assets/Game/Client/TimeDial.cs` (brief 0024):
  - `:10` doc comment lớp;
  - `:258` dòng buổi `(−100, −120)`;
  - `:265` nút `(−16, −84)` cỡ 72;
  - `:272` vòng `(−180, −320)` cỡ 300.
- `TerrainEditInput.cs:46-52` gọi `MinimapView.BlocksPointer(...)`. **Không sửa**: hành vi chặn đi theo `BlocksPointer`.
- World v3 (`design/data/world_maps_m1.json`): 384 × 384 cột, `seaRing` 24, tức mép world là biển. Texture `Clamp` (`MinimapView.cs:320`), nên khi `uvRect` ra ngoài [0, 1] thì ô nhỏ kéo dài màu biển ở mép, đây là hành vi chấp nhận được.
- Cột (x, z) chiếm [x, x+1) × [z, z+1): dấu mốc đặt ở `l.X + 0.5f` (`MinimapView.cs:289`), nên cột dưới người chơi là `FloorToInt`.
- `MapTable` (`Core/MapTable.cs:9, 28-30`): `Ocean = 255`, `MapCount`, `Name(int)`, `Get(x, z)`. Lấy qua `view.Maps`.
- Bench (`client/Assets/Game/Client/Editor/BenchBuild.cs:24-53`) chép `TerrainView.unity`, nên đã có `MinimapView` và `TimeDial`. **Không sửa** BenchBuild: ô nhỏ tự hiện trong Bench.
- Mẫu test: `client/Assets/Game/Tests/Client/MinimapViewPlayTests.cs:16-40` (SetUp/TearDown world v3), `:187-213` (`DigUpdatesTexturePixel`: tìm cột đào được, `view.Dig`).
- Kết quả trước khi làm [run, brief 0024]: EditMode 340/340, PlayMode 60/60; điện thoại avgFps 35,0–37,3 (máy nóng), holeFrames 0, hash `a7719c8999939263`.

## API Unity dùng (đã tra 6000.4 / uGUI 2.0)
- `Mask` (`showMaskGraphic`, `graphic`): [docs API](https://docs.unity3d.com/Packages/com.unity.ugui@2.0/api/UnityEngine.UI.Mask.html), [docs manual](https://docs.unity3d.com/Packages/com.unity.ugui@2.0/manual/script-Mask.html). Manual: Mask giới hạn các phần tử con theo hình của cha và dùng stencil buffer của GPU. Hình mặt nạ là lưới của `Graphic` trên cùng GameObject (ở đây là `TimeDialGraphic` `Disk`). Các con phải là `MaskableGraphic`: `RawImage`, `MinimapLinesGraphic`, `MinimapLandmarkGraphic` đều là loại này.
- `RawImage.texture`, `RawImage.uvRect`: [docs](https://docs.unity3d.com/Packages/com.unity.ugui@2.0/api/UnityEngine.UI.RawImage.html) (đã dùng ở `MinimapView.cs:273, 352-353`).
- `RectTransformUtility.WorldToScreenPoint`, `ScreenPointToLocalPointInRectangle`: [docs](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/RectTransformUtility.WorldToScreenPoint.html), [docs](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/RectTransformUtility.ScreenPointToLocalPointInRectangle.html) (đã dùng `:86`, `:159`, `:167`).
- `RectTransform.anchoredPosition`, `Transform.localEulerAngles`, `Text`, `Outline`, `Canvas.ForceUpdateCanvases`, `Time.frameCount`: đã dùng trong `MinimapView.cs` / `TimeDial.cs`, link ở brief 0024 mục API.
- `Mathf.FloorToInt`: [docs](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/Mathf.FloorToInt.html).

## Quyết định đã chốt (không được đổi)
1. **Vị trí** (user 2026-10-21): ô tròn ở góc trên-phải, chỗ của nút cũ. Anchor/pivot (1, 1), vị trí (−16, −16), cỡ 200 × 200 (đơn vị tham chiếu 1280 × 720).
2. **Bắc cố định lên trên** (user): +z world hướng lên trong ô, ô không xoay. Mũi tên người chơi ở **tâm ô**, xoay theo hướng nhìn ngang của camera giống bản đồ lớn. Chữ "N" cố định ở đỉnh vòng.
3. **Nội dung** (user): ảnh nền (chung `Texture` với bản đồ lớn, nên đào/đặt đổi điểm ảnh thì ô nhỏ thấy ngay), ranh giới map, biểu tượng dấu mốc (không tên), mũi tên người chơi, tên map ở cột dưới người chơi. Cột biển hoặc ngoài world thì ẩn tên (không tự đặt chữ nào cho biển).
4. **Vùng hiện 64 × 64 cột, người chơi luôn ở tâm** (Planner chốt, [số tạm]). Không chặn ở mép world: ra ngoài thì ảnh kéo dài màu biển ở mép.
5. **Cách dựng** (Planner chốt):
   - ảnh nền là `RawImage` cỡ đúng bằng mặt nạ, đổi `uvRect` theo người chơi;
   - ranh giới + dấu mốc nằm trong một RectTransform "Nội dung" phủ cả world, dựng đỉnh **một lần**, mỗi khung chỉ dời `anchoredPosition`;
   - cắt tròn bằng `Mask` trên `TimeDialGraphic` `Disk`.

   Lý do: không dựng lại lưới ranh giới mỗi khung, và ảnh nền không vẽ ra ngoài ô (đỡ fill-rate trên điện thoại).
6. **Bấm/chạm vào ô** (hình tròn bán kính 100, không phải hình vuông) thì mở bản đồ lớn. Khi bản đồ lớn mở, ô nhỏ ẩn và trong bảng có nút **"Đóng (M)"** ở chỗ nút cũ: (−16, −16), cỡ 180 × 56, nền như nút cũ. Bấm nút này thì đóng. Phím M bật/tắt, Esc đóng: giữ nguyên.
7. **Chặn xuyên xuống world:** `BlocksPointer` trả true khi bản đồ mở, khi con trỏ trên ô tròn (lúc đóng) hoặc trên nút Đóng (lúc mở), hoặc khi trong khung này bản đồ vừa bật/tắt (`toggledFrame == Time.frameCount`). Trường hợp cuối giúp lần bấm "Đóng (M)" không đào đất, dù `TerrainEditInput` chạy trước hay sau.
8. **`TimeDial` dời xuống** (user: giữ riêng, dời xuống dưới ô), [số tạm]:
   - nút (−16, −84) → (−16, −232);
   - dòng buổi (−100, −120) → (−100, −268);
   - tâm vòng (−180, −320) → (−180, −470).

   Không đổi gì khác trong `TimeDial`.
9. **Không dùng EventSystem** (như cũ). Thêm hàm public `PressAt(screen)` để chuột, cảm ứng và test cùng đi một đường.
10. Kích thước, màu, cỡ chữ ở bước 1 là [số tạm]: user chỉnh sau khi xem. Executor chép đúng số trong brief.
11. Không sửa: lõi `Game.Core/*`, `TerrainEditInput.cs`, `BenchBuild.cs`, các file `Minimap*Graphic.cs`, `TimeDialGraphic.cs`, scene, `design/`. **Không sửa test cũ**: nếu một test cũ hỏng thì dừng, ghi "cần Opus".

## Phạm vi
- Được sửa:
  - `client/Assets/Game/Client/MinimapView.cs` (bước 1)
  - `client/Assets/Game/Client/TimeDial.cs` (chỉ 4 dòng ở bước 2)
  - `client/Assets/Game/Client/Scenes/Bench.unity` (do `EnsureScene` tự tạo lại khi build)
- Được tạo: `client/Assets/Game/Tests/Client/MinimapCornerPlayTests.cs` (+ `.meta` do Unity sinh).
- Không được sửa: mọi file khác, kể cả `Bench.unity.meta` (guid phải giữ nguyên), `ProjectSettings/`, `design/`.

## Các bước
1. **`MinimapView.cs`**
   1. **Doc comment lớp** (`:11-16`): thay dòng 13 bằng
      `/// Ô minimap tròn góc trên-phải (brief 0025) luôn hiện vùng 64 × 64 cột quanh người chơi, Bắc lên trên, chữ N ở đỉnh; bấm/chạm ô hoặc phím M mở bản đồ lớn, nút "Đóng (M)" / M / Esc đóng. Khi mở: bảng phủ màn hình — ảnh nền tầng độ cao, ranh giới, tên map,`
      Giữ nguyên các dòng còn lại.
   2. **`BlocksPointer`** (`:23-25`) đổi thành:
      ```csharp
      /// <summary>Điểm màn hình (pixel) thuộc về bản đồ: bản đồ đang mở, nằm trên ô tròn / nút Đóng, hoặc khung này vừa bật/tắt (brief 0025).</summary>
      public static bool BlocksPointer(Vector2 screen) =>
          active != null && active.IsReady && (active.IsOpen || active.toggledFrame == Time.frameCount || active.IsOverButton(screen));
      ```
   3. **Hằng số** (đặt ngay sau `static MinimapView active;` ở `:27`), [số tạm]:
      ```csharp
      // Ô nhỏ góc trên-phải (brief 0025), đơn vị tham chiếu 1280 × 720.
      const float CornerSize = 200f, CornerRim = 6f, CornerColumns = 64f;
      const float CornerLineThickness = 2f, CornerLandmarkSize = 14f, CornerMarkerSize = 18f;
      static readonly Color CornerRimColor = new Color(0.08f, 0.08f, 0.08f, 0.9f);
      static readonly Color CornerMaskColor = new Color(0.05f, 0.05f, 0.05f, 1f);
      ```
   4. **Trường riêng:**
      - Xoá `Text buttonText;` (`:42`). `button` giữ tên, từ nay là **nút Đóng** nằm trong bảng.
      - Thêm:
        ```csharp
        RectTransform corner, cornerContent, cornerMarker;
        RawImage cornerImage;
        MinimapLinesGraphic cornerLines;
        Mask cornerMask;
        Text cornerName, cornerNorth, closeText;
        readonly List<RectTransform> cornerLandmarks = new List<RectTransform>();
        float cornerK;
        int cornerShownMap = -2;
        ```
   5. **Thành viên public mới** (đặt sau `ButtonScreenCenter`), đúng tên và kiểu:
      ```csharp
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
      ```
      Sửa `ButtonScreenCenter` (`:85-86`) thành:
      ```csharp
      /// <summary>Tâm vùng bấm theo pixel màn hình: ô tròn khi đóng, nút Đóng khi mở.</summary>
      public Vector2 ButtonScreenCenter => IsOpen
          ? RectTransformUtility.WorldToScreenPoint(null, button.TransformPoint(button.rect.center))
          : CornerScreenPoint(Vector2.zero);
      ```
   6. **`Update()`** (`:131-132`): thay `if (IsOpen) Refresh();` bằng
      ```csharp
      if (IsOpen) Refresh();
      else RefreshCorner();
      ```
   7. **`SetOpen`** (`:135-146`): xoá dòng `buttonText.text = …` (`:141`). Ngay sau `panel.SetActive(open);` thêm `corner.gameObject.SetActive(!open);`. Đổi `if (open) Refresh();` (`:144-145`) thành `if (open) Refresh(); else RefreshCorner();`.
   8. **`PressAt`** (public, đặt sau `Toggle()`):
      ```csharp
      /// <summary>Một lần bấm/chạm tại điểm màn hình: trúng ô tròn (đóng) hoặc nút Đóng (mở) → bật/tắt, trả true (brief 0025).</summary>
      public bool PressAt(Vector2 screen)
      {
          if (!IsReady || !IsOverButton(screen))
              return false;
          Toggle();
          return true;
      }
      ```
   9. **`IsOverButton`** (`:163-169`) đổi thân thành:
      ```csharp
      if (!IsReady)
          return false;
      if (IsOpen)
      {
          RectTransformUtility.ScreenPointToLocalPointInRectangle(button, screen, null, out var local);
          return button.rect.Contains(local);
      }
      RectTransformUtility.ScreenPointToLocalPointInRectangle(corner, screen, null, out var c);
      return (c - corner.rect.center).magnitude <= corner.rect.width * 0.5f;
      ```
   10. **`HandleMouse`** (`:201-205`): đổi `if (mouse.leftButton.wasPressedThisFrame && IsOverButton(pos)) { Toggle(); return; }` thành `if (mouse.leftButton.wasPressedThisFrame && PressAt(pos)) return;`.
   11. **`HandleTouches`** (`:234-239`): đổi thân vòng thành `if (!lastTouches.ContainsKey(kv.Key) && PressAt(kv.Value)) buttonTouches.Add(kv.Key);`. Sửa comment `:233` thành `// Ngón mới chạm lên ô tròn / nút Đóng → mở/đóng; ngón đó không dùng để kéo/chụm.`
   12. **`RefreshCorner()`** (riêng, đặt sau `Refresh()`). Chỉ ghi khi giá trị đổi:
       ```csharp
       /// <summary>Ô nhỏ (brief 0025): người chơi ở tâm — dời "Nội dung" + đổi uvRect ảnh nền, xoay mũi tên, tên map ở cột dưới người chơi.</summary>
       void RefreshCorner()
       {
           var cam = Camera.main;
           cornerMarker.gameObject.SetActive(cam != null);
           if (cam == null)
               return;
           var p = view.transform.InverseTransformPoint(cam.transform.position);
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
       ```
   13. **`Build()`**:
       1. **Nút cũ `:396-400`** thay bằng nút Đóng **con của `panelRt`** (tạo sau `marker`, nên vẽ đè lên khung):
          ```csharp
          // Nút Đóng trong bảng (brief 0025): chỗ nút cũ, chỉ hiện khi bản đồ mở.
          button = Stretch("Nút đóng", panelRt, Vector2.zero, Vector2.zero, new Vector2(1f, 1f));
          Place(button, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-16f, -16f), new Vector2(180f, 56f));
          AddImage(button, new Color(0.1f, 0.1f, 0.1f, 0.75f));
          closeText = NewText("Chữ", button, font, 24, TextAnchor.MiddleCenter);
          closeText.text = "Đóng (M)";
          ```
       2. **Ô nhỏ.** Ngay sau nút Đóng, gọi `BuildCorner(root, font, borders);`. Đây là hàm riêng mới (bước 1.14).
       3. **Log** (`:409`): thêm vào cuối chuỗi, trước `"`, đoạn `, ô nhỏ {CornerColumns:F0} cột {cornerK:F3} px/cột`.
   14. **`BuildCorner(RectTransform root, Font font, List<float[]> borders)`** (riêng, đặt sau `Build()`). Quy ước như brief 0024: "tạo X con của Y" = `Stretch("X", Y, Vector2.zero, Vector2.zero, half)` với `half = new Vector2(0.5f, 0.5f)`, sau đó gọi `Place` với số ghi kèm. Mọi `Graphic` có `raycastTarget = false`. Thứ tự tạo cũng là thứ tự vẽ.
       ```csharp
       var half = new Vector2(0.5f, 0.5f);
       float d = CornerSize - 2f * CornerRim;            // 188: đường kính mặt nạ
       cornerK = d / CornerColumns;                       // px canvas mỗi cột
       float W = view.World.Size.X, H = view.World.Size.Z;
       CornerViewport = new MinimapViewport(W, H, W * cornerK, H * cornerK);
       ```
       1. `corner` ("Ô bản đồ") con của `root`: `Place(corner, (1,1), (1,1), (−16,−16), (CornerSize, CornerSize))`. Trên `corner` thêm `TimeDialGraphic` với `shape = Disk`, `color = CornerRimColor`.
       2. `maskRt` ("Mặt nạ") con của `corner`: `Place(maskRt, half, half, Vector2.zero, (d, d))`. Trên `maskRt` thêm `TimeDialGraphic` (`shape = Disk`, `color = CornerMaskColor`) **trước**, rồi mới thêm `cornerMask = maskRt.gameObject.AddComponent<Mask>(); cornerMask.showMaskGraphic = true;`.
       3. `cornerImage`: `Stretch("Ảnh nền", maskRt, Vector2.zero, Vector2.zero, half)` (giãn kín mặt nạ, không `Place`), thêm `RawImage`, `texture = Texture`, `raycastTarget = false`.
       4. `cornerContent` ("Nội dung") con của `maskRt`: `Place(cornerContent, half, Vector2.zero, Vector2.zero, new Vector2(W * cornerK, H * cornerK))`, pivot (0, 0).
       5. `cornerLines`: `Stretch("Ranh giới", cornerContent, Vector2.zero, Vector2.zero, Vector2.zero)` (pivot 0,0), thêm `MinimapLinesGraphic`. Gán `Lines = borders; Thickness = CornerLineThickness; color = borderColor; Viewport = CornerViewport; raycastTarget = false;`.
       6. Dấu mốc: `var lms = view.Landmarks; if (lms != null)`, với mỗi `i`:
          - `icon` ("Dấu mốc " + i) con của `cornerContent`;
          - `CornerViewport.WorldToPanel(l.X + 0.5f, l.Z + 0.5f, out ix, out iy)`;
          - `Place(icon, Vector2.zero, half, new Vector2(ix, iy), (CornerLandmarkSize, CornerLandmarkSize))`;
          - thêm `MinimapLandmarkGraphic` (`diamond = l.Kind == LandmarkKind.Checkpoint`, `color = LandmarkStyle.ColorOf(l)`);
          - `cornerLandmarks.Add(icon)`.
       7. `cornerMarker` ("Người chơi") con của `corner` (ngoài mặt nạ): `Place(cornerMarker, half, half, Vector2.zero, (CornerMarkerSize, CornerMarkerSize))`. Thêm `MinimapMarkerGraphic`, `color = markerColor`.
       8. `cornerName = NewText("Tên map", corner, font, 16, TextAnchor.MiddleCenter)`, `Place(cornerName.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 18f), new Vector2(160f, 22f))`. Thêm `Outline` màu `(0, 0, 0, 0.85)`, `effectDistance (1.5, −1.5)`. Ban đầu `enabled = false`.
       9. `nRt` ("Nền N") con của `corner`: `Place(nRt, new Vector2(0.5f, 1f), half, new Vector2(0f, -3f), new Vector2(28f, 28f))`. Thêm `TimeDialGraphic` (`Disk`, `CornerRimColor`). Sau đó `cornerNorth = NewText("N", nRt, font, 20, TextAnchor.MiddleCenter); cornerNorth.text = "N";` (giãn kín `nRt`, không `Place`).
       10. `cornerShownMap = -2;`. Không gọi `RefreshCorner` ở đây: `SetOpen(false)` ở `:406` sẽ gọi.

   Xong khi: `read_console` 0 lỗi biên dịch, và `grep -n "buttonText" client/Assets/Game/Client/MinimapView.cs` không còn kết quả.

2. **`TimeDial.cs`**: chỉ sửa 4 dòng.
   - `:10`: đổi `nút tròn góc trên-phải dưới nút Bản đồ` thành `nút tròn góc trên-phải dưới ô minimap (brief 0025)`.
   - `:258`: `new Vector2(-100f, -120f)` → `new Vector2(-100f, -268f)`.
   - `:265`: `new Vector2(-16f, -84f)` → `new Vector2(-16f, -232f)`.
   - `:272`: `new Vector2(-180f, -320f)` → `new Vector2(-180f, -470f)`.

   Xong khi: console 0 lỗi; `git diff --stat client/Assets/Game/Client/TimeDial.cs` cho thấy 4 dòng thêm và 4 dòng xoá.

3. **Test `MinimapCornerPlayTests.cs`** (`client/Assets/Game/Tests/Client/`, namespace `Game.Client.Tests`, `using System.Collections; using Game.Core; using NUnit.Framework; using UnityEngine; using UnityEngine.TestTools;`).
   - Doc lớp: `/// <summary>World-tech bước 7g (brief 0025): ô minimap tròn góc trên-phải — mặt nạ tròn, chung texture, theo người chơi, tên map, dấu mốc, bấm mở/đóng, chặn con trỏ.</summary>`.
   - `SetUp`/`TearDown` và các trường chép nguyên từ `MinimapViewPlayTests.cs:12-40`. Thêm vào cuối `SetUp`: `yield return null; Canvas.ForceUpdateCanvases();`.
   - Hằng số trong lớp: `const float K = 188f / 64f;`.
   - Hàm phụ `static int FirstColumnOf(MapTable maps, int m, out int z)`: duyệt `z` rồi `x` từ 0, trả `x` đầu tiên có `Get(x, z) == m`, gán `z` theo cột đó.

   | Test | Thao tác | Kiểm |
   |---|---|---|
   | `CornerBuiltWithCircleMaskAndSharedTexture` | — | `!map.IsOpen`, `map.CornerActive`, `!map.PanelActive`; `CornerMaskIsDisk`; `ReferenceEquals(map.CornerTexture, map.Texture)`; `CornerNorthText == "N"`; `CornerViewport.PixelsPerColumn ≈ K` (±1e-4); `CornerBorderVertexCount` > 0 và < 65000; `CornerLandmarkCount == view.Landmarks.Count`. Log `MINIMAPCORNER đỉnh ranh giới {CornerBorderVertexCount}` |
   | `CornerFollowsCameraPositionAndHeading` | cam `(100.5, 40, 250.5)`, `LookRotation(Vector3.right)`, yield; rồi cam `(192.5, 40, 192.5)`, `LookRotation(Vector3.forward)`, yield | Lần 1: `CornerContentPosition ≈ (−100.5K, −250.5K)` (±0.01); `CornerUvRect ≈ ((100.5−32)/384, (250.5−32)/384, 64/384, 64/384)` (±1e-4); `DeltaAngle(270, CornerMarkerAngle)` < 0.5. Lần 2: `CornerContentPosition ≈ (−192.5K, −192.5K)`; `DeltaAngle(0, CornerMarkerAngle)` < 0.5 |
   | `CornerShowsMapNameUnderPlayer` | với m = 0..`MapCount−1`: `x = FirstColumnOf(view.Maps, m, out z)`, cam `(x + 0.5, 40, z + 0.5)`, yield. Rồi `Assert.AreEqual(MapTable.Ocean, view.Maps.Get(2, 2))`, cam `(2.5, 40, 2.5)`, yield. Rồi cam `(−10, 40, −10)`, yield | Mỗi m: `CornerMapName == view.Maps.Name(m)`. Biển: `CornerMapName` null. Ngoài world: null |
   | `CornerLandmarkIconsAtLandmarkPositions` | — | Với mỗi i: `CornerViewport.WorldToPanel(l.X + 0.5f, l.Z + 0.5f)` ≈ `CornerLandmarkPosition(i)` (±0.01) |
   | `DigVisibleInCorner` | chép phần tìm cột + `view.Dig` từ `MinimapViewPlayTests.cs:189-207` | `((Texture2D)map.CornerTexture).GetPixels32()[idx]` = `ColumnColor` (alpha 255) và khác `before` |
   | `PressCornerOpensAndCloseButtonCloses` | `PressAt(ButtonScreenCenter)`; yield; `Canvas.ForceUpdateCanvases()`; `PressAt(ButtonScreenCenter)`; yield; `PressAt(new Vector2(5, 5))` | Lần 1 trả true, `IsOpen`, `PanelActive`, `!CornerActive`, `CloseButtonText == "Đóng (M)"`. Lần 2 trả true, `!IsOpen`, `CornerActive`. `(5,5)` trả false, vẫn đóng |
   | `CornerHitTestIsCircular` | đóng, không bấm | `BlocksPointer(CornerScreenPoint((0,0)))` true; `CornerScreenPoint((0,−95))` true; `CornerScreenPoint((−95,−95))` **false** (trong hình vuông, ngoài hình tròn); `(5,5)` false |
   | `ToggleFrameBlocksPointer` | `PressAt(ButtonScreenCenter)` (mở), `map.SetOpen(false)` trong cùng khung; kiểm; yield; kiểm | Cùng khung: `BlocksPointer((5,5))` true. Sau yield: false |

   Xong khi: 8 test đạt.

4. **Bench + điện thoại.** `ADB` là `/Applications/Unity/Hub/Editor/6000.4.8f1/PlaybackEngines/AndroidPlayer/SDK/platform-tools/adb`. Máy Xiaomi 24095PCADG, serial USB `PFQCRWVWR46HU4GQ`. Nếu `ADB devices` không thấy serial này thì chạy `ADB connect 192.168.1.6:5555` rồi dùng `-s 192.168.1.6:5555`. Tự cài, tự đọc log, không hỏi user.
   1. MCP `execute_menu_item` "Game/Build APK Bench" → console có `Bench build: Succeeded`.
   2. Cài và chạy:
      - `ADB -s <máy> install -r client/Builds/bench.apk` → `Success`;
      - `ADB -s <máy> logcat -c`;
      - `ADB -s <máy> shell monkey -p com.viventa.bench -c android.intent.category.LAUNCHER 1`.
   3. Khoảng 20 giây sau khi mở app, kiểm màn hình bằng `ADB -s <máy> shell dumpsys window | grep -E "mDreamingLockscreen|KeyguardShowing"`. Máy đang khoá thì chụp sẽ ra ảnh đen: ghi "chờ user mở khoá" và bỏ qua phần chụp. Không khoá thì chụp: `ADB -s <máy> exec-out screencap -p > client/Builds/bench-0025.png`, rồi mở ảnh bằng Read và xác nhận:
      - ô tròn ở góc trên-phải, **cắt tròn, không vuông**, có chữ N và mũi tên;
      - nút thời điểm và vòng nằm dưới ô, không chồng lên ô.

      Ghi kết quả vào Kết quả. Ô hiện **vuông** thì dừng, ghi "cần Opus".
   4. Chờ đủ 75 giây kể từ khi mở app (chạy nền hoặc Monitor), rồi chạy `ADB -s <máy> logcat -d -s Unity | grep -E "BENCH|MINIMAP|TIMEDIAL|Exception"`. Ghi vào Kết quả:
      - dòng `MINIMAP dựng` (phải có `ô nhỏ 64 cột`);
      - dòng `TIMEDIAL dựng`;
      - `avgFps` (≥ 30), `p99Ms`, `holeFrames` (= 0), `hash` (= `a7719c8999939263`);
      - mọi dòng `Exception` (phải không có).

      Ghi cả nhiệt độ: `ADB -s <máy> shell dumpsys thermalservice | grep -m1 "Thermal Status"`. Sau 150 giây vẫn không có `BENCH` thì ghi "chờ user mở khoá" và làm tiếp bước 5.
   5. Dọn:
      - `git checkout -- client/ProjectSettings/UnityConnectSettings.asset` (nếu đổi);
      - nếu `git diff client/Assets/Game/Client/Scenes/Bench.unity.meta` có thay đổi thì `git checkout -- client/Assets/Game/Client/Scenes/Bench.unity.meta`.

5. **Chạy toàn bộ + commit:**
   1. EditMode cả bộ: 340/340 (brief này không thêm test EditMode).
   2. PlayMode cả bộ: 60 cũ + 8 mới = 68, đạt hết. Sau đó `git checkout -- client/ProjectSettings/EditorSettings.asset`.
   3. `python3 tools/check_design.py` → hợp lệ.
   4. `git status --short`: chỉ còn file trong Phạm vi. Commit code với message `brief 0025: ô minimap tròn góc trên-phải, nút Đóng trong bảng, dời vòng thời điểm`.
   5. `date "+%Y-%m-%d %H:%M"` → ghi "Kết thúc", đặt trạng thái `chờ review`, commit brief với message `brief 0025: kết quả`.

## Tiêu chí xong
- `MinimapView` đúng chữ ký và hành vi ở bước 1; không còn `buttonText`. `TimeDial` chỉ đổi 4 dòng.
- `MinimapCornerPlayTests` 8/8. Test cũ (`MinimapViewPlayTests`, `TimeDialPlayTests`, `LandmarkViewPlayTests`, `TerrainEditPlayTests`, …) không bị sửa và đạt hết.
- EditMode 340/340, PlayMode 68/68.
- Điện thoại:
  - có dòng `MINIMAP dựng … ô nhỏ 64 cột`;
  - `avgFps ≥ 30`, `holeFrames=0`, hash trùng, 0 `Exception`;
  - có ảnh `bench-0025.png` thấy ô tròn (hoặc ghi "chờ user mở khoá").
- Chỉ các file trong Phạm vi bị đổi.

## Test sau khi xong

### Phần cần test
| Phần | Bước | Test | Kết quả mong đợi |
|---|---|---|---|
| Ô dựng, mặt nạ tròn, chung texture, chữ N, cỡ 64 cột | 1.3–1.5, 1.14 | `CornerBuiltWithCircleMaskAndSharedTexture` | Như bảng bước 3 |
| Người chơi ở tâm: dời nội dung, `uvRect`, xoay mũi tên | 1.12 | `CornerFollowsCameraPositionAndHeading` | Như bảng bước 3 |
| Tên map dưới người chơi; biển / ngoài world thì ẩn | 1.12 | `CornerShowsMapNameUnderPlayer` | Đúng tên 3 map; null 2 lần |
| Dấu mốc trên ô nhỏ | 1.14.6 | `CornerLandmarkIconsAtLandmarkPositions` | Khớp `WorldToPanel` |
| Đào thì ô nhỏ thấy | 1.14.3 | `DigVisibleInCorner` | Điểm ảnh đổi |
| Bấm ô mở, nút Đóng đóng; `PressAt` / chuột / cảm ứng | 1.6–1.11, 1.13.1 | `PressCornerOpensAndCloseButtonCloses`; user xem (chuột Mac, chạm điện thoại) | Mở rồi đóng; ô ẩn khi mở |
| Vùng bấm hình tròn | 1.9 | `CornerHitTestIsCircular` | Góc vuông ngoài tròn không chặn |
| Chặn xuyên trong khung bật/tắt | 1.2 | `ToggleFrameBlocksPointer`; user xem (bấm "Đóng (M)" không đào đất) | true rồi false |
| Bản đồ lớn không đổi (uv, ranh giới, tên, dấu mốc, kéo/phóng, chặn chuột) | 1 | `MinimapViewPlayTests` (10, cũ, không sửa) | Đạt |
| Vòng thời điểm sau khi dời | 2 | `TimeDialPlayTests` (8, cũ); ảnh điện thoại; user xem | Đạt; không chồng ô |
| Đào/đặt không bị ảnh hưởng | — | `TerrainEditPlayTests`, `EditOcclusionPlayTests` (cũ) | Đạt |
| Dấu mốc 3D, bản đồ trên SimView | — | `LandmarkViewPlayTests`, PlayMode cả bộ | Đạt |
| Bench + điện thoại (mặt nạ trên GPU thật, FPS) | 4 | APK + logcat + ảnh | Như Tiêu chí xong |
| Regression chung | — | EditMode + PlayMode cả bộ | 340/340, 68/68 |

### Lệnh executor chạy
Unity Editor dùng chung qua MCP (quyết định 0010). Nếu Editor đang tắt thì thay `run_tests` bằng `python3 tools/unity_test.py EditMode|PlayMode`, và ghi lại rằng không build APK được.
1. MCP `refresh_unity` → `read_console` (types error) → 0 lỗi.
2. `grep -n "buttonText" client/Assets/Game/Client/MinimapView.cs` → không có kết quả.
3. `git diff --stat client/Assets/Game/Client/TimeDial.cs` → `4 insertions(+), 4 deletions(-)`.
4. MCP `run_tests` PlayMode lọc `MinimapCornerPlayTests`, `MinimapViewPlayTests`, `TimeDialPlayTests`, `TerrainEditPlayTests` → đạt. Dọn: `git checkout -- client/ProjectSettings/EditorSettings.asset`.
5. Bước 4 (Bench, APK, adb, ảnh, logcat). Dọn `UnityConnectSettings.asset` và `Bench.unity.meta`.
6. EditMode cả bộ, rồi PlayMode cả bộ → ghi dòng tổng kết. Dọn `EditorSettings.asset`.
7. `python3 tools/check_design.py` → hợp lệ.
8. `git status --short` → commit (bước 5).

### User tự test (Planner gửi khi review)
Trên Mac, scene `SimView` (và `TerrainView`), nhấn Play:
1. Góc trên-phải có ô tròn (không còn nút "Bản đồ (M)"). Trong ô có ảnh nền quanh chỗ đứng, mũi tên đỏ ở tâm, chữ **N** ở đỉnh vòng, tên map ở sát mép dưới.
2. Bay đi (WASD) thì nền trượt theo, mũi tên luôn ở tâm. Xoay camera thì mũi tên xoay, còn ô không xoay (Bắc luôn lên trên). Bay qua ranh giới thì tên map đổi; ra biển thì tên ẩn. Thấy được đường ranh giới và dấu mốc khi lại gần.
3. Đào/đặt một khối gần chỗ đứng: điểm ảnh trong ô đổi theo.
4. Bấm vào ô thì bản đồ lớn mở (phóng/kéo như cũ), ô nhỏ ẩn, góc trên-phải có nút "Đóng (M)". Bấm nút này, hoặc nhấn M / Esc, thì đóng. Bấm "Đóng (M)" không đào đất. Bấm sát góc vuông ngoài vòng tròn của ô thì **có** đào (vùng bấm hình tròn).
5. Nút thời điểm, dòng "Đang ở buổi: …" và vòng 4 mốc (phím T) nằm dưới ô, không chồng lên ô.
6. Điện thoại (app Bench do executor đã cài): chạm ô thì mở bản đồ lớn; chạm "Đóng (M)" thì đóng. Xem ảnh `client/Builds/bench-0025.png`: ô có tròn không, cỡ ô và chữ có ổn không.
7. Cỡ ô (200), số cột (64), màu viền, cỡ chữ, vị trí nút thời điểm là [số tạm]: ghi lại chỗ muốn chỉnh.

## Dừng và chuyển lại Opus khi
- Brief mơ hồ hoặc mâu thuẫn với code thật (vd số dòng lệch tới mức không xác định được chỗ sửa).
- Cần một quyết định thuộc AGENTS.md §3.4 hoặc ngoài "Quyết định đã chốt" (vd đổi cỡ, màu, chữ, vị trí, thêm chữ cho biển).
- Một test cũ hỏng (không được sửa test cũ).
- Ảnh điện thoại cho thấy ô bị cắt vuông hoặc không cắt (mặt nạ không hoạt động).
- Cùng một lỗi sửa 2 lần vẫn chưa qua.
- Phải sửa file ngoài "Phạm vi".
- Cần API Unity chưa có trong mục "API Unity dùng".
- `avgFps < 30` trên điện thoại: ghi số đo và nhiệt độ, dừng, không tự tối ưu.

---
<!-- Từ đây trở xuống: EXECUTOR ghi. -->

## Kết quả
Base commit: 07a8345
Chạy bằng: claude-sonnet-5-5 · medium
Bắt đầu 2026-10-21 17:06 · Kết thúc 2026-10-21 17:14
Commit code: c36c48f — MinimapView.cs, TimeDial.cs (4 dòng), Tests/Client/MinimapCornerPlayTests.cs (+.meta). Bench.unity không đổi (EnsureScene tạo lại giống hệt).
Lệch brief: adb thấy máy qua 192.168.1.6:5555 (serial USB không hiện). Điện thoại KHOÁ (isKeyguardShowing=true) → không chụp màn hình, không có log BENCH/MINIMAP sau ~140 s: **chờ user mở khoá** (app đã cài và đang chạy, nhiệt Thermal Status 2).
<!-- Base commit (trước khi làm) · Chạy bằng: <model id> · <effort> (dòng OK của brief_check.py) · Bắt đầu / Kết thúc (giờ thực) · commit của brief · file đã đổi · ghi chú lệch so với brief (nếu có, kèm lý do). -->

## Kết quả test
<!-- Mỗi dòng ở "Phần cần test" → đạt / không đạt / chờ user + bằng chứng (tên test, dòng output).
Mỗi lệnh ở "Lệnh executor chạy": `<lệnh>` → <kết quả thật, dòng tổng kết>. -->
- Ô dựng/mặt nạ/texture/N/64 cột: đạt — CornerBuiltWithCircleMaskAndSharedTexture
- Theo người chơi: đạt — CornerFollowsCameraPositionAndHeading
- Tên map: đạt — CornerShowsMapNameUnderPlayer
- Dấu mốc: đạt — CornerLandmarkIconsAtLandmarkPositions
- Đào thấy ở ô nhỏ: đạt — DigVisibleInCorner
- Bấm mở/Đóng: đạt (test PressCornerOpensAndCloseButtonCloses); chuột/chạm thật: chờ user
- Vùng bấm tròn: đạt — CornerHitTestIsCircular
- Chặn trong khung bật/tắt: đạt — ToggleFrameBlocksPointer; bấm "Đóng (M)" không đào: chờ user
- Test cũ (MinimapViewPlayTests, TimeDialPlayTests, TerrainEdit…, Landmark…): đạt trong PlayMode cả bộ
- Bench + điện thoại (mặt nạ GPU, FPS, ảnh): chờ user mở khoá
Lệnh:
1. refresh_unity + read_console error → 0 lỗi
2. grep buttonText → 0 kết quả
3. git diff --stat TimeDial.cs → 4 insertions(+), 4 deletions(-)
4. PlayMode MinimapCornerPlayTests → 8/8 (đã chạy riêng); cả bộ nằm ở mục 6
5. Build APK Bench → "Bench build: Succeeded", cài Success, chạy; ảnh/log: chờ user mở khoá. Dọn xong.
6. EditMode 340/340; PlayMode 68/68. Dọn EditorSettings.asset xong.
7. check_design.py → ✓ design/ hợp lệ
8. git status chỉ file trong Phạm vi → đã commit


## Vướng mắc
<!-- Khi trạng thái = cần Opus: câu hỏi cụ thể + các phương án (kèm đề xuất). -->

---
<!-- PLANNER ghi khi review. -->

## Review
<!-- Kết luận: đã duyệt | cần sửa (thêm bước sửa vào "Các bước", trạng thái về chờ làm). Điểm đã đối chiếu. -->
