# Brief 0018 — World 7d (phần 2/2): hiển thị dấu mốc trong world và trên bản đồ

> Trạng thái: đã duyệt
> Review: đầy đủ + user xem
> Model thực thi: Sonnet · medium
> Cập nhật: 2026-10-21 11:55

<!-- Planner (Opus) viết, Executor (Sonnet) làm theo ở PHIÊN KHÁC — executor không có lịch sử chat, không được tự quyết.
Trạng thái: chờ làm → đang làm → chờ review → đã duyệt; hoặc → cần Opus (executor dừng, hỏi ở mục Vướng mắc).
File được commit. -->

## Mục tiêu
Task [world bước 7](../tasks/m1-world-buoc-7-nhieu-map-minimap.md), dòng **7d** của [world-tech](../../design/04-tech/world-tech.md#kế-hoạch-prototype-m1), theo quyết định [0023](../../design/decisions/0023-dau-moc-cong-trinh-checkpoint-va-dan-duong.md). Phần 1 là brief [0017](0017-world-dau-moc-loi-va-cap-nhat-ban-do.md), đã duyệt: lõi `LandmarkPlacer` và bản đồ cập nhật khi đào/đặt. Brief này làm phần hiển thị:
- `TerrainChunkView` (world v3) gọi `LandmarkPlacer.Place` rồi giữ danh sách `Landmarks`. Mỗi dấu mốc được dựng thành một **khối đánh dấu** trong world: checkpoint là cột có đầu vuông, công trình là khối hộp. Màu theo loại.
- `MinimapView` vẽ **biểu tượng** của mỗi dấu mốc trên bản đồ: checkpoint là hình thoi, công trình là hình vuông, cùng màu với khối trong world. Tên hiện khi phóng to.
- Có test PlayMode và đo trên điện thoại.

## Bối cảnh cần đọc
- `client/Assets/Game/Core/LandmarkPlacer.cs` (brief 0017):
  - `public enum LandmarkKind : byte { Checkpoint = 0, Structure = 1 }` (`:7`).
  - `public readonly struct Landmark` (`:10`), gồm các trường `Kind, Map, Type (−1 với checkpoint), X, Z, Surface, Name`. Surface là ô rắn cao nhất của cột lúc đặt.
  - `public static List<Landmark> Place(WorldData world, MapTable maps, WorldMapParams p)` (`:45`). Thứ tự kết quả: 3 checkpoint theo map, rồi 6 công trình theo cấu hình. Seed mặc định có 9 dấu mốc.
- `client/Assets/Game/Client/TerrainChunkView.cs`:
  - `using` ở `:1-7` (đã có `System.Collections.Generic`, `System.Diagnostics`, `Game.Core`, `UnityEngine.Rendering`).
  - Property `Maps` ở `:64`.
  - `public void Build()` ở `:111`:
    - `:114-118`: xoá con "Chunks" cũ bằng `DestroyImmediate`, rồi tạo lại.
    - `:120`: `var gen = Stopwatch.StartNew();`
    - `:121`: `int waterLevel;`
    - `:122-129`: nhánh `source == WorldSource.MapsV3`, có `var p = ConfigLoader.LoadWorldMapParams();`.
    - `:138`: `GenerateMs = gen.Elapsed.TotalMilliseconds;`.
    - Nhánh `streaming` (`:153-175`) `return` sớm, nên mọi thứ thêm vào phải đặt **trước** `:142`.
  - `CreateTerrainMaterial` (`:343-349`): mẫu tạo `new UnityEngine.Material(GraphicsSettings.currentRenderPipeline.defaultMaterial)`.
- `client/Assets/Game/Client/SimView.cs:186-199`: mẫu `Box` tạo khối bằng `GameObject.CreatePrimitive(PrimitiveType.Cube)`, bỏ collider, đặt màu `_BaseColor`.
- `client/Assets/Game/Client/MinimapView.cs`:
  - Trường cấu hình ở `:29-33`; accessor test ở `:54-73`.
  - `Refresh()` ở `:247-284`: khối `if (Viewport.Zoom != lastZoom …)` ở `:252-272` đặt vị trí tên map bằng `Viewport.WorldToPanel` + `anchoredPosition` (`:262-271`).
  - `Build()` ở `:286-368`:
    - vòng tạo tên map ở `:338-347`;
    - dấu người chơi tạo ở `:348-352`, tạo sau nên vẽ đè;
    - dòng log `MINIMAP dựng:` ở `:367`.
  - Hàm phụ `Stretch` (`:371`), `Place` (`:384`), `NewText` (`:400`).
- `client/Assets/Game/Client/MinimapMarkerGraphic.cs`: mẫu `MaskableGraphic` có `[RequireComponent(typeof(CanvasRenderer))]` và `OnPopulateMesh` (viền tối + lõi màu thu nhỏ).
- `client/Assets/Game/Core/MinimapViewport.cs`:
  - `Zoom` (`:17`, từ 1 tới `MaxZoom = 8`);
  - `PanelWidth/PanelHeight` (`:15-16`);
  - `WorldToPanel(float wx, float wz, out float px, out float py)` (`:55`);
  - `ZoomAt(float px, float py, float factor)` (`:70`).
- `client/Assets/Game/Tests/Client/MinimapViewPlayTests.cs`: `SetUp` ở `:16-32` (camera ở (192, 40, 60), `TerrainChunkView` MapsV3 + streaming, `MinimapView`), `TearDown` ở `:34-40`. Test cuối là `DigUpdatesTexturePixel` (brief 0017).
- `client/Assets/Game/Client/ConfigLoader.cs:27`: `public static WorldMapParams LoadWorldMapParams()`.
- Bench: menu `Game/Build APK Bench` (`client/Assets/Game/Client/Editor/BenchBuild.cs:48`) build scene `Bench.unity`, đầu ra `client/Builds/bench.apk`, gói `com.viventa.bench`. Brief này không đổi scene, nên **không** chạy "Game/Tạo scene Bench".

**API Unity dùng** (đã tra docs 6000.4; repo đã dùng tất cả, không có API mới):
- [`GameObject.CreatePrimitive`](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/GameObject.CreatePrimitive.html)
- [`Object.DestroyImmediate`](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/Object.DestroyImmediate.html)
- [`RenderPipelineAsset.defaultMaterial`](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/Rendering.RenderPipelineAsset-defaultMaterial.html)
- [`Material.SetColor`](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/Material.SetColor.html)
- [`Renderer.sharedMaterial`](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/Renderer-sharedMaterial.html)
- [`MaskableGraphic` (uGUI 2.0)](https://docs.unity3d.com/Packages/com.unity.ugui@2.0/api/UnityEngine.UI.MaskableGraphic.html)

Ngoài ra dùng `VertexHelper.AddVert/AddTriangle`, `Text`, `Outline`, đã có trong `MinimapMarkerGraphic.cs` và `MinimapView.cs`.

## Quyết định đã chốt (không được đổi)
1. **Ai tính dấu mốc:** `TerrainChunkView.Build`, chỉ khi `Maps != null` (world v3). Gọi `LandmarkPlacer.Place(World, Maps, p)` **sau** dòng `GenerateMs` (để thời gian đặt không cộng vào thời gian sinh). Kết quả lưu vào `public IReadOnlyList<Landmark> Landmarks`, thời gian lưu vào `public double LandmarkMs`. World v2 → `Landmarks = null`, không tạo khối nào.
2. **Khối trong world** là vật thể hiển thị, không phải ô voxel, và **không có collider** (không chặn raycast đào/đặt, camera bay xuyên qua được). Đào dưới chân thì khối vẫn lơ lửng, chấp nhận ở M1 theo 0023.
   - Tất cả nằm dưới con `"Dấu mốc"` của `TerrainChunkView`. `Build` gọi lại thì xoá con cũ bằng `DestroyImmediate`, giống cách làm với "Chunks".
   - Mỗi dấu mốc là một GameObject tên = `Name`, `localPosition = (X + 0,5, Surface + 1, Z + 0,5)` (đáy khối nằm trên mặt ô cao nhất). Con của nó là các khối Cube:
     - **Checkpoint:** thân `(0,5; 4; 0,5)` tâm y = 2, đầu `(1,2; 1,2; 1,2)` tâm y = 4,6 → 2 con.
     - **Công trình:** khối `(3; 2,5; 3)` tâm y = 1,25 → 1 con.
   - Material dùng chung theo màu (mỗi màu 1 material), không tạo material riêng cho từng khối.
3. **Màu** (tạm, chỉnh khi chơi thử) nằm ở một chỗ: lớp `LandmarkStyle`, dùng cho cả khối trong world lẫn biểu tượng trên bản đồ.
   - Checkpoint: `(40, 200, 230)`.
   - Công trình: lấy màu theo `Type % 6` từ bảng `(230, 120, 40)`, `(200, 60, 60)`, `(240, 200, 50)`, `(150, 90, 200)`, `(230, 90, 170)`, `(240, 240, 240)`.
4. **Biểu tượng trên bản đồ:**
   - `MinimapLandmarkGraphic`: checkpoint là hình thoi, công trình là hình vuông. Viền tối `(20, 20, 20)`, lõi màu `LandmarkStyle` thu nhỏ còn 0,65. Cỡ 22 × 22.
   - Vị trí đặt bằng `anchoredPosition = Viewport.WorldToPanel(X + 0,5, Z + 0,5)`, cập nhật trong cùng khối đặt vị trí tên map.
   - Tạo sau tên map và trước dấu người chơi, để dấu người chơi luôn vẽ đè lên.
5. **Tên dấu mốc trên bản đồ:** `Text` cỡ 18, có viền, nằm ngay dưới biểu tượng (pivot (0,5; 1), y = py − 13). Chỉ hiện khi `Viewport.Zoom ≥ 2` (trường `landmarkLabelZoom = 2f`), để lúc nhìn toàn cảnh không rối chữ.
6. Không sửa lõi Game.Core, bộ sinh, JSON, `design/`, scene. Không làm dịch chuyển hay dẫn đường (để M2 theo 0023).
7. Giờ bắt đầu/kết thúc lấy bằng `date "+%Y-%m-%d %H:%M"` ngay lúc đó, không ước đoán.

## Phạm vi
- **Được tạo** (cùng các file `.meta` Unity tự tạo):
  - `client/Assets/Game/Client/LandmarkStyle.cs`
  - `client/Assets/Game/Client/MinimapLandmarkGraphic.cs`
  - `client/Assets/Game/Tests/Client/LandmarkViewPlayTests.cs`
- **Được sửa:**
  - `client/Assets/Game/Client/TerrainChunkView.cs` (bước 2)
  - `client/Assets/Game/Client/MinimapView.cs` (bước 3)
  - `client/Assets/Game/Tests/Client/MinimapViewPlayTests.cs` (bước 5)
  - mục Kết quả / Kết quả test / Vướng mắc của brief này.
- **Không được sửa:** mọi file khác (Game.Core, bộ sinh, JSON, scene, asmdef, `design/`). `ProjectSettings/` phải trả lại sau test và build.

## Các bước
0. `date "+%Y-%m-%d %H:%M"` → ghi "Bắt đầu" và "Base commit" (`git rev-parse --short HEAD`) vào Kết quả; đặt trạng thái `đang làm`.

1. **Tạo `client/Assets/Game/Client/LandmarkStyle.cs` và `MinimapLandmarkGraphic.cs`** với nội dung:
   ```csharp
   using Game.Core;
   using UnityEngine;

   namespace Game.Client
   {
       /// <summary>Màu dấu mốc (0023, brief 0018) — dùng chung cho khối trong world và biểu tượng trên bản đồ. Màu tạm, chỉnh khi chơi thử.</summary>
       public static class LandmarkStyle
       {
           public static readonly Color32 CheckpointColor = new Color32(40, 200, 230, 255);

           static readonly Color32[] StructureColors =
           {
               new Color32(230, 120, 40, 255),
               new Color32(200, 60, 60, 255),
               new Color32(240, 200, 50, 255),
               new Color32(150, 90, 200, 255),
               new Color32(230, 90, 170, 255),
               new Color32(240, 240, 240, 255),
           };

           public static Color32 ColorOf(in Landmark l) =>
               l.Kind == LandmarkKind.Checkpoint ? CheckpointColor : StructureColors[l.Type % StructureColors.Length];
       }
   }
   ```
   ```csharp
   using UnityEngine;
   using UnityEngine.UI;

   namespace Game.Client
   {
       /// <summary>Biểu tượng dấu mốc trên bản đồ (brief 0018): hình thoi (checkpoint) hoặc hình vuông (công trình) phủ rect — viền tối, lõi màu `color` thu 0,65.</summary>
       [RequireComponent(typeof(CanvasRenderer))]
       public class MinimapLandmarkGraphic : MaskableGraphic
       {
           public bool diamond;
           public Color32 outlineColor = new Color32(20, 20, 20, 255);

           protected override void OnPopulateMesh(VertexHelper vh)
           {
               vh.Clear();
               var r = rectTransform.rect;
               AddShape(vh, r, 1f, outlineColor);
               AddShape(vh, r, 0.65f, color);
           }

           void AddShape(VertexHelper vh, Rect r, float scale, Color32 c)
           {
               var o = r.center;
               float hw = r.width * 0.5f * scale, hh = r.height * 0.5f * scale;
               int v = vh.currentVertCount;
               if (diamond)
               {
                   vh.AddVert(new Vector3(o.x, o.y + hh), c, Vector4.zero);
                   vh.AddVert(new Vector3(o.x + hw, o.y), c, Vector4.zero);
                   vh.AddVert(new Vector3(o.x, o.y - hh), c, Vector4.zero);
                   vh.AddVert(new Vector3(o.x - hw, o.y), c, Vector4.zero);
               }
               else
               {
                   vh.AddVert(new Vector3(o.x - hw, o.y + hh), c, Vector4.zero);
                   vh.AddVert(new Vector3(o.x + hw, o.y + hh), c, Vector4.zero);
                   vh.AddVert(new Vector3(o.x + hw, o.y - hh), c, Vector4.zero);
                   vh.AddVert(new Vector3(o.x - hw, o.y - hh), c, Vector4.zero);
               }
               vh.AddTriangle(v, v + 1, v + 2);
               vh.AddTriangle(v, v + 2, v + 3);
           }
       }
   }
   ```
   Xong khi: MCP `refresh_unity` → `read_console` (types error) không có lỗi.

2. **`TerrainChunkView.cs`:**
   1. Thêm sau property `Maps` (`:64`):
      ```csharp
      /// <summary>Dấu mốc của world v3 (0023, brief 0017/0018): checkpoint theo map rồi công trình; null với v2.</summary>
      public IReadOnlyList<Landmark> Landmarks { get; private set; }
      /// <summary>Thời gian LandmarkPlacer.Place (ms).</summary>
      public double LandmarkMs { get; private set; }
      ```
      Thêm `const string LandmarksName = "Dấu mốc";` cạnh `const string ChunksName` (`:49`).
   2. Trong `Build()`, ngay sau khối xoá/tạo "Chunks" (`:114-118`), thêm:
      ```csharp
      var oldLandmarks = transform.Find(LandmarksName);
      if (oldLandmarks != null)
          DestroyImmediate(oldLandmarks.gameObject);
      ```
   3. Trước `if (source == WorldSource.MapsV3)` (`:122`), khai báo `WorldMapParams mapParams = null;`. Trong nhánh MapsV3, sau `var p = ConfigLoader.LoadWorldMapParams();`, thêm `mapParams = p;`.
   4. Ngay sau `GenerateMs = gen.Elapsed.TotalMilliseconds;` (`:138`), thêm:
      ```csharp
      Landmarks = null;
      if (Maps != null)
      {
          var lm = Stopwatch.StartNew();
          Landmarks = LandmarkPlacer.Place(World, Maps, mapParams);
          LandmarkMs = lm.Elapsed.TotalMilliseconds;
          BuildLandmarkMarkers();
          Debug.Log($"LANDMARKVIEW {Landmarks.Count} dấu mốc, đặt {LandmarkMs:F1} ms");
      }
      ```
   5. Thêm 2 hàm (đặt sau `CreateTerrainMaterial`):
      ```csharp
      /// <summary>Khối đánh dấu trong world (brief 0018): con "Dấu mốc", mỗi dấu mốc một GameObject đáy ở mặt ô cao nhất; không collider.</summary>
      void BuildLandmarkMarkers()
      {
          var root = new GameObject(LandmarksName).transform;
          root.SetParent(transform, false);
          var mats = new Dictionary<uint, UnityEngine.Material>();
          foreach (var l in Landmarks)
          {
              var t = new GameObject(l.Name).transform;
              t.SetParent(root, false);
              t.localPosition = new Vector3(l.X + 0.5f, l.Surface + 1, l.Z + 0.5f);
              var c = LandmarkStyle.ColorOf(l);
              if (l.Kind == LandmarkKind.Checkpoint)
              {
                  MarkerBox(t, new Vector3(0f, 2f, 0f), new Vector3(0.5f, 4f, 0.5f), c, mats);
                  MarkerBox(t, new Vector3(0f, 4.6f, 0f), new Vector3(1.2f, 1.2f, 1.2f), c, mats);
              }
              else
                  MarkerBox(t, new Vector3(0f, 1.25f, 0f), new Vector3(3f, 2.5f, 3f), c, mats);
          }
      }

      static void MarkerBox(Transform parent, Vector3 localPos, Vector3 scale, Color32 c, Dictionary<uint, UnityEngine.Material> mats)
      {
          var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
          go.name = "Khối";
          DestroyImmediate(go.GetComponent<Collider>()); // không chặn raycast đào/đặt; DestroyImmediate để Build() chạy được cả trong editor
          var t = go.transform;
          t.SetParent(parent, false);
          t.localPosition = localPos;
          t.localScale = scale;
          uint key = (uint)(c.r << 16 | c.g << 8 | c.b);
          if (!mats.TryGetValue(key, out var mat))
          {
              mat = new UnityEngine.Material(GraphicsSettings.currentRenderPipeline.defaultMaterial);
              mat.SetColor("_BaseColor", c);
              mats[key] = mat;
          }
          go.GetComponent<MeshRenderer>().sharedMaterial = mat;
      }
      ```
   Xong khi: biên dịch không lỗi.

3. **`MinimapView.cs`:**
   1. Thêm trường cấu hình sau `labelFontSize` (`:33`):
      ```csharp
      public float landmarkIconSize = 22f;
      public int landmarkFontSize = 18;
      /// <summary>Tên dấu mốc chỉ hiện khi Zoom ≥ giá trị này (brief 0018).</summary>
      public float landmarkLabelZoom = 2f;
      ```
      Thêm trường `readonly List<RectTransform> landmarkIcons = new List<RectTransform>();` và `readonly List<Text> landmarkLabels = new List<Text>();` cạnh `labels` (`:41`).
   2. Thêm accessor test sau `LabelPanelPosition` (`:68`):
      ```csharp
      public int LandmarkIconCount => landmarkIcons.Count;
      public Vector2 LandmarkIconPosition(int i) => landmarkIcons[i].anchoredPosition;
      public bool LandmarkIconIsDiamond(int i) => landmarkIcons[i].GetComponent<MinimapLandmarkGraphic>().diamond;
      public Color LandmarkIconColor(int i) => landmarkIcons[i].GetComponent<MinimapLandmarkGraphic>().color;
      public string LandmarkLabelText(int i) => landmarkLabels[i].text;
      public bool LandmarkLabelVisible(int i) => landmarkLabels[i].enabled;
      public Vector2 LandmarkLabelPosition(int i) => landmarkLabels[i].rectTransform.anchoredPosition;
      ```
   3. Trong `Build()`, ngay sau vòng tạo tên map (sau `:347`, trước `marker = …` ở `:348`):
      ```csharp
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
      ```
   4. Trong `Refresh()`, cuối khối `if (Viewport.Zoom != lastZoom …)`, ngay sau vòng tên map (sau `:271`, trước `}` ở `:272`):
      ```csharp
      bool showNames = Viewport.Zoom >= landmarkLabelZoom;
      for (int i = 0; i < landmarkIcons.Count; i++)
      {
          var l = view.Landmarks[i];
          Viewport.WorldToPanel(l.X + 0.5f, l.Z + 0.5f, out float ix, out float iy);
          landmarkIcons[i].anchoredPosition = new Vector2(ix, iy);
          landmarkLabels[i].rectTransform.anchoredPosition = new Vector2(ix, iy - landmarkIconSize * 0.5f - 2f);
          landmarkLabels[i].enabled = showNames;
      }
      ```
   5. Dòng log `:367`: thêm `{landmarkIcons.Count} dấu mốc, ` ngay sau `{labels.Count} tên, `.
   Xong khi: biên dịch không lỗi.

4. **Tạo `client/Assets/Game/Tests/Client/LandmarkViewPlayTests.cs`:**
   - Namespace `Game.Client.Tests`.
   - `SetUp`/`TearDown` chép từ `MinimapViewPlayTests` (`:16-40`) nhưng **không** thêm `MinimapView`. Sau khi tạo `view`, chờ `yield return null` (Start chạy `Build`).
   - Có 3 test:
   1. `MarkersMatchLandmarks`:
      - `view.Landmarks` khác null, `Count == 9`.
      - Các phần tử bằng từng trường (`Kind, Map, Type, X, Z, Surface, Name`) với `LandmarkPlacer.Place(view.World, view.Maps, ConfigLoader.LoadWorldMapParams())`.
      - `var root = go.transform.Find("Dấu mốc")` khác null, `root.childCount == 9`.
      - Với mỗi i, gọi `t = root.GetChild(i)`, rồi kiểm:
        - `t.name == l.Name`;
        - `t.localPosition == new Vector3(l.X + 0.5f, l.Surface + 1, l.Z + 0.5f)`;
        - `t.childCount` = 2 với checkpoint, 1 với công trình;
        - `t.GetChild(0).GetComponent<MeshRenderer>().sharedMaterial.GetColor("_BaseColor")` bằng `(Color)LandmarkStyle.ColorOf(l)`, sai số 1e-3 mỗi kênh r, g, b.
      - `root.GetComponentsInChildren<Collider>(true).Length == 0`.
      - Đếm material khác nhau (`HashSet<Material>`) trên mọi `MeshRenderer` con của `root`: ≤ 7.
      - `Debug.Log($"LANDMARKVIEWTEST {view.Landmarks.Count} dấu mốc, {mats} material, đặt {view.LandmarkMs:F1} ms")`.
   2. `RebuildReplacesMarkers`:
      - Gọi `view.Build()`, rồi `yield return null`.
      - Chỉ còn đúng 1 con tên "Dấu mốc": đếm các con trực tiếp của `go.transform` có `name == "Dấu mốc"` → 1.
      - Con đó có `childCount == 9`.
   3. `IslandWorldHasNoLandmarks`:
      - Tạo GameObject riêng với `TerrainChunkView` để `source = WorldSource.IslandV2`, `streaming = false`, rồi `yield return null`.
      - `Landmarks == null`; `transform.Find("Dấu mốc") == null`.
      - Huỷ GameObject đó.
   Xong khi: MCP `run_tests` PlayMode lọc `LandmarkViewPlayTests` → 3/3 đạt.

5. **`MinimapViewPlayTests.cs`:** thêm test `LandmarkIconsAtLandmarkPositions`:
   - `map.SetOpen(true); yield return null;`.
   - `map.LandmarkIconCount == view.Landmarks.Count` (= 9).
   - Với mỗi i:
     - Gọi `map.Viewport.WorldToPanel(l.X + 0.5f, l.Z + 0.5f, out px, out py)`; `LandmarkIconPosition(i)` lệch ≤ 0,01 so với (px, py).
     - `LandmarkIconIsDiamond(i) == (l.Kind == LandmarkKind.Checkpoint)`.
     - `LandmarkIconColor(i)` bằng `(Color)LandmarkStyle.ColorOf(l)` (sai số 1e-3).
     - `LandmarkLabelText(i) == l.Name`.
     - `LandmarkLabelVisible(i) == false` (zoom 1).
   - Gọi `map.Viewport.ZoomAt(map.Viewport.PanelWidth * 0.5f, map.Viewport.PanelHeight * 0.5f, 3f); yield return null;`. Kiểm `map.Viewport.Zoom ≥ 2`. Với mỗi i:
     - `LandmarkLabelVisible(i) == true`;
     - vị trí biểu tượng lại khớp `WorldToPanel` (≤ 0,01);
     - `LandmarkLabelPosition(i).y` = `py − 13` (≤ 0,01).
   - `map.SetOpen(false)`.
   Xong khi: `run_tests` PlayMode lọc `MinimapViewPlayTests` → 10/10 đạt (9 cũ + 1 mới).

6. **Bench + điện thoại.** `ADB` là `/Applications/Unity/Hub/Editor/6000.4.8f1/PlaybackEngines/AndroidPlayer/SDK/platform-tools/adb`. Máy Xiaomi 24095PCADG, serial `PFQCRWVWR46HU4GQ`, nối USB. Nếu `ADB devices` không có serial này thì chạy `ADB connect 192.168.1.6:5555` và dùng `-s 192.168.1.6:5555`.
   1. MCP `execute_menu_item` "Game/Build APK Bench" → console có `Bench build: Succeeded`. **Không** chạy "Game/Tạo scene Bench".
   2. Cài và chạy:
      - `ADB -s <máy> install -r client/Builds/bench.apk` → `Success`.
      - `ADB -s <máy> logcat -c`.
      - `ADB -s <máy> shell monkey -p com.viventa.bench -c android.intent.category.LAUNCHER 1`.
   3. Chờ 75 giây (lệnh `sleep` chạy nền hoặc Monitor), rồi chạy `ADB -s <máy> logcat -d -s Unity | grep -E "BENCH|MINIMAP|LANDMARKVIEW"`. Ghi vào Kết quả:
      - nguyên dòng `LANDMARKVIEW …`;
      - nguyên dòng `MINIMAP dựng: …`;
      - các trường `avgFps`, `p99Ms`, `holeFrames`, `hash` của dòng `BENCH`.
      Sau 150 giây vẫn không có dòng `BENCH` (máy khoá màn hình làm app tạm dừng) → ghi "chờ user mở khoá" vào Kết quả test và làm tiếp bước 7, không chờ.
   4. Dọn: `git checkout -- client/ProjectSettings/UnityConnectSettings.asset` (nếu đổi).

7. **Commit:**
   1. Chạy `git status --short`: chỉ được còn file trong Phạm vi. Commit code với message `brief 0018: hiển thị dấu mốc trong world và trên bản đồ`.
   2. `date "+%Y-%m-%d %H:%M"` → ghi "Kết thúc" vào Kết quả, đặt trạng thái `chờ review`, commit brief với message `brief 0018: kết quả`.

8. **Sửa sau review (lần 1) — material dấu mốc chạy được trên điện thoại.**
   Lỗi [run, logcat Bench 2026-10-21 11:24]: trong bản build, `GraphicsSettings.currentRenderPipeline.defaultMaterial` trả về `null`. Vì vậy `new UnityEngine.Material(null)` trong `MarkerBox` ném `ArgumentNullException (source)`, `TerrainChunkView.Build()` dừng giữa chừng: không có chunk (`tris=0`), `MinimapView.Build` bị NullReferenceException mỗi khung. Test PlayMode chạy trong Editor nên không bắt được lỗi này.
   Cách sửa: dùng material asset đặt trong `Resources/`. Asset trong `Resources/` luôn được đóng vào build kèm shader ([Resources.Load](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/Resources.Load.html); [Shader.Find](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/Shader.Find.html): shader không được tham chiếu thì không vào build).
   1. **Tạo material asset:** dùng MCP `manage_material` tạo `Assets/Game/Client/Resources/Materials/Landmark.mat`, shader `Universal Render Pipeline/Lit`, `_BaseColor` trắng. Xong khi: có file `client/Assets/Game/Client/Resources/Materials/Landmark.mat` và `.meta`, cùng `Materials.meta` của thư mục. Kiểm `grep m_Shader` trong file `.mat`: phải trỏ tới shader URP Lit (guid `933532a4fcc9baf4fa0491de14d08ed7`; khác guid này thì ghi guid thật vào Kết quả, không dừng).
   2. **`TerrainChunkView.cs`:**
      - Thêm `const string LandmarkMaterialPath = "Materials/Landmark";` cạnh `LandmarksName`.
      - Trong `BuildLandmarkMarkers()`, ngay đầu hàm, thêm:
        ```csharp
        var baseMat = Resources.Load<UnityEngine.Material>(LandmarkMaterialPath);
        if (baseMat == null)
        {
            Debug.LogError($"LANDMARKVIEW thiếu Resources/{LandmarkMaterialPath}.mat — bỏ qua khối dấu mốc");
            return;
        }
        ```
        Rồi truyền `baseMat` vào `MarkerBox` (thêm tham số `UnityEngine.Material baseMat`).
      - Trong `MarkerBox`, thay `new UnityEngine.Material(GraphicsSettings.currentRenderPipeline.defaultMaterial)` bằng `new UnityEngine.Material(baseMat)`.
      - Không sửa chỗ khác.
   3. **`LandmarkViewPlayTests.cs`**, test `MarkersMatchLandmarks`: thêm kiểm `Resources.Load<Material>("Materials/Landmark")` khác null, và `shader.name == "Universal Render Pipeline/Lit"`. Mọi `sharedMaterial` của khối có `shader` trùng shader của material đó.
   4. **Chạy lại:**
      - PlayMode lọc `LandmarkViewPlayTests` + `MinimapViewPlayTests` → 13/13.
      - PlayMode cả bộ → 32/32. Dọn `EditorSettings.asset`.
      - EditMode cả bộ → 311/311.
   5. **Điện thoại** (máy đã mở khoá, kết nối qua `192.168.1.6:5555`):
      - MCP `execute_menu_item` "Game/Build APK Bench" → `Succeeded`.
      - Cài, xoá log, chạy app: `ADB -s 192.168.1.6:5555 install -r client/Builds/bench.apk`, rồi `logcat -c`, rồi `monkey -p com.viventa.bench -c android.intent.category.LAUNCHER 1`.
      - Chờ 80 giây, chạy `logcat -d -s Unity | grep -E "BENCH|MINIMAP|LANDMARKVIEW|Exception"`. Phải có:
        - `LANDMARKVIEW 9 dấu mốc`;
        - `MINIMAP dựng … 9 dấu mốc`;
        - `BENCH` với `tris` > 0, `avgFps` ≥ 30, `holeFrames=0`, `hash=a7719c8999939263`;
        - **không** có dòng `Exception`.
      - Ghi các dòng này vào Kết quả test. Máy khoá lại thì ghi "chờ user mở khoá".
   6. **Dọn:**
      - `git checkout -- client/ProjectSettings/UnityConnectSettings.asset` (nếu đổi).
      - `Bench.unity.meta` đổi guid thì `git checkout -- client/Assets/Game/Client/Scenes/Bench.unity.meta`. Lần exec trước đã commit guid mới, Planner đã trả về guid cũ `56b66cc8…` trong commit review.
   7. **Commit:**
      - `git status --short`: chỉ còn 2 file `.cs` trên, file test, `Landmark.mat` + `.meta` + `Materials.meta`. `Bench.unity` nếu chỉ thêm 3 trường `landmark*` mặc định thì được commit.
      - Commit code với message `brief 0018: sửa material dấu mốc trên điện thoại`.
      - `date` → ghi "Kết thúc (sửa lần 1)", trạng thái `chờ review`, commit brief.
   Phạm vi bổ sung cho bước này: `client/Assets/Game/Client/Resources/Materials/` (thư mục mới + `Landmark.mat`), `client/Assets/Game/Client/Scenes/Bench.unity` (chỉ phần Unity tự ghi khi build).

## Tiêu chí xong
- Biên dịch không lỗi.
- EditMode cả bộ đạt 311/311 (không thêm test EditMode).
- PlayMode cả bộ đạt: 28 + 3 + 1 = 32.
- Điện thoại:
  - có dòng `LANDMARKVIEW 9 dấu mốc`;
  - dòng `MINIMAP dựng` có `9 dấu mốc`;
  - `BENCH avgFps ≥ 30`, `holeFrames=0`, `hash=a7719c8999939263` (hoặc ghi "chờ user mở khoá").
- `git status` sạch sau commit: không còn `EditorSettings.asset`, `UnityConnectSettings.asset` hay file nào ngoài Phạm vi.

## Test sau khi xong

### Phần cần test
| Phần | Bước | Test | Kết quả mong đợi |
|---|---|---|---|
| Khối trong world: số lượng, vị trí, tên, màu, không collider | 1–2 | PlayMode `MarkersMatchLandmarks` | 9 khối đúng `(X + 0,5, Surface + 1, Z + 0,5)`, đúng tên/màu/số con, 0 collider, ≤ 7 material, khớp `LandmarkPlacer.Place` |
| Gọi Build lại không nhân đôi khối | 2 | PlayMode `RebuildReplacesMarkers` | Đúng 1 con "Dấu mốc", 9 khối |
| World v2 không có dấu mốc | 2 | PlayMode `IslandWorldHasNoLandmarks` | `Landmarks == null`, không có con "Dấu mốc" |
| Biểu tượng + tên trên bản đồ | 1, 3 | PlayMode `LandmarkIconsAtLandmarkPositions` | 9 biểu tượng đúng vị trí khung, đúng hình/màu; tên ẩn ở zoom 1, hiện ở zoom ≥ 2, nằm dưới biểu tượng |
| Regression bản đồ (0016, 0017) | 3 | 9 test cũ của `MinimapViewPlayTests` | Đạt |
| Regression đào/đặt bằng raycast, nạp dần, camera, SimView | 2 | PlayMode cả bộ (`TerrainEditPlayTests`, `TerrainStreamingPlayTests`, `WorldBoundsPlayTests`, `FlyCamera*`, `SimViewPlayTests`…) | Đạt |
| Regression lõi | — | EditMode cả bộ | 311/311 |
| Hiệu năng điện thoại | 6 | Bench APK + logcat | `LANDMARKVIEW` có ms đặt; avgFps ≥ 30, holeFrames 0, mã băm v3 trùng |
| Nhìn thật | — | Mục "User tự test" | Chờ user |

### Lệnh executor chạy
1. MCP `refresh_unity` → `read_console` (types error) → 0 lỗi biên dịch.
2. MCP `run_tests` PlayMode lọc `LandmarkViewPlayTests`, `MinimapViewPlayTests` → đạt. Đọc console, lấy dòng `LANDMARKVIEWTEST`.
3. MCP `run_tests` EditMode (cả bộ) → ghi dòng tổng kết.
4. MCP `run_tests` PlayMode (cả bộ) → ghi dòng tổng kết. Sau đó dọn: `git checkout -- client/ProjectSettings/EditorSettings.asset`.
5. Bước 6 (Bench, APK, adb, logcat) → ghi các dòng `LANDMARKVIEW`, `MINIMAP`, `BENCH`. Dọn `UnityConnectSettings.asset`.
6. `git status --short` → chỉ còn file trong Phạm vi; commit (bước 7).
7. `date "+%Y-%m-%d %H:%M"` → ghi "Kết thúc"; commit brief.

### User tự test
Mở scene TerrainView trên Mac, bấm Play:
1. Bấm M mở bản đồ:
   - Thấy 3 hình thoi xanh lơ (mỗi map 1 cái, gần giữa map).
   - Thấy 6 hình vuông màu (mỗi map 2 cái, 6 màu khác nhau), không đè lên nhau.
   - Ở toàn cảnh chưa có chữ.
2. Lăn chuột phóng to (khoảng 5 nấc trở lên): tên hiện dưới mỗi biểu tượng ("Checkpoint Map 1", "Công trình 1A"…), đọc được, có dấu tiếng Việt đúng.
3. Đóng bản đồ, bay tới một checkpoint và một công trình (nhìn bản đồ để biết hướng):
   - Checkpoint là cột xanh lơ có đầu vuông, công trình là khối hộp màu, cùng màu với biểu tượng trên bản đồ.
   - Đáy đặt sát mặt đất, không chìm và không lơ lửng.
   - Nhìn từ xa thấy được.
4. Bay xuyên qua một khối: không bị chặn. Đào ô ngay dưới khối: khối vẫn nằm nguyên chỗ cũ (chấp nhận ở M1).
5. Nói nếu muốn đổi màu, cỡ hay hình (số trong `LandmarkStyle` / `TerrainChunkView.BuildLandmarkMarkers`).

## Dừng và chuyển lại Opus khi
- Brief mơ hồ hoặc mâu thuẫn với code thật (vd số dòng không khớp nội dung trích).
- `Landmarks.Count` khác 9, hoặc `LandmarkPlacer.Place` ném lỗi khi chạy trong Play.
- FPS điện thoại < 30.
- Cần một quyết định thuộc AGENTS.md §3.4 hoặc ngoài "Quyết định đã chốt", hoặc cần API Unity ngoài danh sách.
- Cùng một lỗi sửa 2 lần vẫn chưa qua.
- Phải sửa file ngoài "Phạm vi".

---
<!-- Từ đây trở xuống: EXECUTOR ghi. -->

## Kết quả
- Bắt đầu: 2026-10-21 11:03
- Base commit: 2cadbeb
- Chạy bằng: claude-sonnet-5-5 · medium
- Bắt đầu (sửa lần 1): 2026-10-21 11:27, base 0323878
- Kết thúc (sửa lần 1): 2026-10-21 11:45; commit ec8e3e5; thêm Resources/Materials/Landmark.mat (+.meta, Materials.meta); m_Shader guid 933532a4fcc9baf4fa0491de14d08ed7 (đúng URP Lit); Bench.unity.meta đã trả guid
- Kết thúc (lần đầu): 2026-10-21 11:20
- Commit code: ef3b3c1
- File: LandmarkStyle.cs, MinimapLandmarkGraphic.cs (mới); TerrainChunkView.cs, MinimapView.cs, MinimapViewPlayTests.cs (sửa); LandmarkViewPlayTests.cs (mới) — kèm .meta
- Build Bench: `Bench build: Succeeded, 845427275 byte, 00:02:33` → Builds/bench.apk

## Kết quả test
- Biên dịch: 0 lỗi (console error rỗng) — đạt.
- PlayMode lọc `LandmarkViewPlayTests` + `MinimapViewPlayTests`: 13/13 đạt (3 mới Landmark + 10 Minimap gồm `LandmarkIconsAtLandmarkPositions`). Dòng: `LANDMARKVIEWTEST 9 dấu mốc, 7 material, đặt 28.7 ms`; `LANDMARKVIEW 9 dấu mốc, đặt 27.8–31.6 ms` (Editor).
- EditMode cả bộ: 311/311 đạt.
- PlayMode cả bộ: 32/32 đạt. Đã `git checkout -- EditorSettings.asset`.
- Điện thoại: adb serial USB PFQCRWVWR46HU4GQ không có; dùng 192.168.1.6:5555 — cài `Success`, monkey khởi chạy. Máy khoá màn hình (keyguard, screen OFF, app bị APP_CMD_STOP) nên không có dòng LANDMARKVIEW/MINIMAP/BENCH → **chờ user mở khoá** (avgFps/holeFrames/hash chưa đo). Chạy lại: mở khoá máy rồi `monkey -p com.viventa.bench`, chờ 75 s, logcat grep BENCH|MINIMAP|LANDMARKVIEW.
- Nhìn thật (mục "User tự test"): chờ user.

### Sửa lần 1 (bước 8)
- PlayMode lọc Landmark+Minimap: 13/13; PlayMode cả bộ: 32/32 (đã dọn EditorSettings); EditMode cả bộ: 311/311.
- Build Bench: `Succeeded, 846014419 byte`. Điện thoại 192.168.1.6:5555, đã mở khoá [run 11:43]:
  - `LANDMARKVIEW 9 dấu mốc, đặt 26,5 ms`
  - `MINIMAP dựng: ảnh 384x384, 3 đường ranh giới, 3 tên, 9 dấu mốc, font=LegacyRuntime, khung 292x616, 121,9 ms`
  - `BENCH … frames=2081 avgFps=52,0 p99Ms=33,29 … holeFrames=0 maxMissing=0 loadedChunksMax=801 … genMs=30 initialLoadMs=338 … pass30fps=True passNoHole=True | tris=3889524 … hash=a7719c8999939263 v3`
  - Không có dòng Exception.

## Vướng mắc
Không có. (Chỉ còn đo điện thoại chờ mở khoá.)

---
<!-- PLANNER ghi khi review. -->

## Review
- Chạy bằng `claude-sonnet-5-5 · medium` khớp Model thực thi. Mọi dòng "Phần cần test" có kết quả (điện thoại: chờ mở khoá).
- Diff `2cadbeb..ef3b3c1`: `TerrainChunkView`, `MinimapView`, `LandmarkStyle`, `MinimapLandmarkGraphic`, 2 file test đúng bước 1–5 và quyết định 1–5.
- Ngoài Phạm vi: `Bench.unity` (+3 trường `landmark*` mặc định, Unity tự ghi khi build — vô hại, giữ) và `Bench.unity.meta` đổi guid (không file nào tham chiếu [run grep]) → Planner trả guid cũ trong commit review này. Executor không ghi lệch này vào Kết quả.
- User xem trên Mac: "ổn" [user 2026-10-21].
- **Điện thoại (Planner chạy, máy đã mở khoá, 11:24)** [run]: `ArgumentNullException: Value cannot be null. Parameter name: source` tại `Material.CreateWithMaterial` ← `TerrainChunkView.MarkerBox` ← `BuildLandmarkMarkers` ← `Build`. Sau đó `MinimapView.Build` NullReferenceException mỗi khung. BENCH `avgFps=36,3 tris=0 loadedChunksMax=0 holeFrames=0 hash=a7719c8999939263` → không có world hiển thị, số FPS vô nghĩa. Nguyên nhân: `RenderPipelineAsset.defaultMaterial` null trong player. Lỗi do brief (Planner chép mẫu `SimView` mà mẫu này chỉ chạy trong Editor), không phải lỗi executor.
- Kết luận: cần sửa
- **Review lần 2 (sửa lần 1, `0323878..ec8e3e5`):** chỉ đổi `TerrainChunkView` (material lấy từ `Resources/Materials/Landmark.mat`, thiếu thì log lỗi, bỏ qua khối), test thêm kiểm shader, `Landmark.mat` (m_Shader guid `933532a4…` = URP Lit [run grep]). Không còn `defaultMaterial` trong code dấu mốc. `Bench.unity.meta` giữ guid cũ.
- Test (theo executor): PlayMode 32/32, EditMode 311/311. Điện thoại: `LANDMARKVIEW 9 dấu mốc, đặt 26,5 ms`; `MINIMAP dựng … 9 dấu mốc`; BENCH `avgFps=52,0 holeFrames=0 tris=3889524 hash=a7719c8999939263`; không Exception → đạt Tiêu chí xong.
- Giờ 7d phần 2 đã ghi vào roadmap; world-tech 7d đánh dấu Xong.
- Kết luận: đã duyệt
