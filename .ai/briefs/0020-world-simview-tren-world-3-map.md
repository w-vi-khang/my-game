# Brief 0020 — World 7e (phần 2/2): scene SimView chạy trên world 3 map (nạp dần), trại demo tự đặt trong Map 1, camera ở trại

> Trạng thái: chờ review
> Review: đầy đủ + user xem
> Model thực thi: Sonnet · medium
> Cập nhật: 2026-10-21 12:30

<!-- Planner (Opus) viết, Executor (Sonnet) làm theo ở PHIÊN KHÁC — executor không có lịch sử chat, không được tự quyết.
Trạng thái: chờ làm → đang làm → chờ review → đã duyệt; hoặc → cần Opus (executor dừng, hỏi ở mục Vướng mắc).
File được commit. -->

## Mục tiêu
Task [world bước 7](../tasks/m1-world-buoc-7-nhieu-map-minimap.md), dòng **7e** của [world-tech](../../design/04-tech/world-tech.md#kế-hoạch-prototype-m1), phần 2/2. Phần 1 (lõi, đã duyệt) là brief [0019](0019-world-vung-mo-phong-va-cho-dat-trai.md).

Brief này làm 2 việc:
- Scene SimView chuyển sang world 3 map (v3, nạp dần quanh camera, có bản đồ), giống scene TerrainView.
- `SimView` tự tìm chỗ đặt trại trong Map 1 (`SimDemo.FindSite`), mô phỏng trong vùng 64 × 64 (`Simulation(cfg, world, site.Bounds)`), đặt camera nhìn vào trại.

Khi chạy trên đảo v2, `SimView` phải giữ nguyên hành vi cũ (test `SimViewPlayTests` không sửa).

## Bối cảnh cần đọc
- `client/Assets/Game/Client/SimView.cs` (240 dòng):
  - `using UnityEngine.Rendering;` ở `:6`, chỉ dùng cho `GraphicsSettings` ở `Box`.
  - `Update()` gọi `Setup()` khi `view.World != null && view.Editor != null` (`:70-74`).
  - `Setup()` ở `:100-128`:
    - `:102` `var cfg = ConfigLoader.LoadSimConfig();`
    - `:103` `Sim = new Simulation(cfg, view.World);`
    - `:104-105` gắn `view.Editor.IsOccupied` và `CellChanged`
    - `:106` `SimDemo.Build(Sim);`
    - sau đó dựng hộp, `UpdateBodies()`, `SetFrameRate(60)`.
  - `Box(...)` ở `:208-221`, dòng `:218`: `var mat = new UnityEngine.Material(GraphicsSettings.currentRenderPipeline.defaultMaterial);`. **Lỗi đã biết** (bài học brief 0018): `defaultMaterial` là null trong bản build.
- `client/Assets/Game/Client/TerrainChunkView.cs`:
  - Trường public: `source` (`:29`, kiểu `WorldSource { IslandV2, MapsV3 }`), `streaming` (`:31`), `streamTarget` (`:33`).
  - Property: `World` (`:62`), `Editor` (`:64`), `Maps` (`:66`, null khi IslandV2), `Landmarks` (`:68`, `IReadOnlyList<Landmark>`, null khi IslandV2).
  - `MissingColumnsWithin(int radius)` (`:92`): số cột chunk trong bán kính (ô) quanh tâm nạp mà chưa nạp.
  - `Build()` chạy trong `Start` (`:112-115`): sinh world, đặt dấu mốc, rồi nạp đầu quanh `streamTarget` (hoặc `Camera.main`).
  - `LateUpdate` (`:250-`) nạp thêm `maxColumnsPerFrame` cột mỗi khung, **gần tâm trước** (`ChunkStreamPlan.Update` sắp theo khoảng cách, `Core/ChunkStreamPlan.cs:49-80`).
  - Material dấu mốc: `Resources.Load<UnityEngine.Material>("Materials/Landmark")` (`:51`, `:374`). File là `client/Assets/Game/Client/Resources/Materials/Landmark.mat`, shader `Universal Render Pipeline/Lit`, có `_BaseColor`.
- `client/Assets/Game/Client/ConfigLoader.cs`: `LoadWorldMapParams()` (`:27`, có `waterLevel`), `LoadSimConfig()` (`:37`, có `regionSize` = 64).
- `client/Assets/Game/Core/Sim/SimDemo.cs` (brief 0019):
  - Hằng `BaseX = 30, BaseZ = 16, BaseGround = 17`, `SiteSizeX = 9, SiteSizeZ = 7`.
  - Hàm:
    - `public static DemoSite FindSite(WorldData world, MapTable maps, IReadOnlyList<Landmark> landmarks, int map, int waterLevel, int regionSize)`;
    - `public static int Build(Simulation sim)`;
    - `public static int Build(Simulation sim, DemoSite site)`.
  - `DemoSite`:
    - trường `Dx, Dz`, `Bounds`;
    - `int Height(int x, int z)` (cột trong cửa sổ 9 × 7);
    - `CellPos Shift(CellPos c)`.
  - Cửa sổ có góc `wx = site.Dx + SimDemo.BaseX − 1`, `wz = site.Dz + SimDemo.BaseZ − 1`.
- `client/Assets/Game/Core/Sim/Simulation.cs`:
  - `public Simulation(SimConfig config, WorldData world, SimBounds bounds)`;
  - `Bounds`, `PathfinderCellCount`.
- Scene:
  - `client/Assets/Game/Client/Scenes/TerrainView.unity` đã là world v3. Thành phần:
    - Terrain: `source: 1`, `streaming: 1`, `loadRadius: 128`, `TerrainEditInput`, `MinimapView`.
    - Camera: `DebugFlyCamera` có va chạm, `UnderwaterView`, `WorldEdgeFog`; far clip 128; sương 64–120; vị trí (192, 40, 150), xoay `Euler(25, 0, 0)` (`:210-211`).
    - Ocean: `source: 1`.
  - GameObject Terrain trong TerrainView là `&1663237066` (`:265`), danh sách component ở `:272-276`: `1663237068` Transform, `1663237067` TerrainChunkView, `1663237069` TerrainEditInput, `1663237070` MinimapView.
  - `client/Assets/Game/Client/Scenes/SimView.unity` hiện là đảo v2. Script `SimView` có guid `cdbf1e5920cdf45b7814fd3978fe3828` (`SimView.cs.meta`), `maxTicksPerFrame: 5`.
- `DebugFlyCamera` (`Client/DebugFlyCamera.cs:25-29`) đọc `yaw`/`pitch` từ góc xoay camera **một lần trong `Start`**. Vì vậy `SimView` chỉ được đặt góc xoay đúng bằng góc của scene (`Euler(25, 0, 0)`), để kéo chuột nhìn quanh không bị giật.
- Test mẫu:
  - `client/Assets/Game/Tests/Client/SimViewPlayTests.cs`: đảo v2, **không sửa**.
  - `client/Assets/Game/Tests/Client/LandmarkViewPlayTests.cs:15-36`: SetUp camera `MainCamera` + Terrain MapsV3 + streaming, TearDown.
  - asmdef `Game.Client.Tests`, namespace `Game.Client.Tests`.

API Unity dùng (đều đã có trong code của project, không thêm API mới):
- `Resources.Load<T>` [docs](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/Resources.Load.html);
- `Camera.main` [docs](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/Camera-main.html);
- `Camera.WorldToViewportPoint` [docs](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/Camera.WorldToViewportPoint.html);
- `Quaternion.Euler` [docs](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/Quaternion.Euler.html).

## Quyết định đã chốt (không được đổi)
1. `SimView.Setup` chọn nhánh theo `view.Maps`:
   - **`view.Maps == null` (đảo v2):** giữ y hệt cũ, gồm `new Simulation(cfg, view.World)`, `SimDemo.Build(Sim)`, không động vào camera.
   - **`view.Maps != null` (world v3):**
     - `Site = SimDemo.FindSite(view.World, view.Maps, view.Landmarks, 0, ConfigLoader.LoadWorldMapParams().waterLevel, cfg.regionSize)`;
     - `Sim = new Simulation(cfg, view.World, Site.Bounds)`;
     - `SimDemo.Build(Sim, Site)`;
     - đặt camera (QĐ 2).
     - Map 1 = chỉ số map 0.
2. **Camera ở trại:** cột giữa cửa sổ `ccx = wx + 4`, `ccz = wz + 3`, `h = Site.Height(ccx, ccz)`.
   - `Camera.main.transform.position = new Vector3(ccx + 0.5f, h + 12f, ccz + 0.5f − 26f)`;
   - `rotation = Quaternion.Euler(25f, 0f, 0f)`.
   - Đây là góc của scene, nên `DebugFlyCamera` không lệch.
   - Không có `Camera.main` → bỏ qua, không lỗi.
   - Không ép nạp lại chunk: `TerrainChunkView` tự nạp dần quanh camera mới, gần trước.
3. `Box` lấy material gốc từ `Resources.Load<UnityEngine.Material>("Materials/Landmark")` (nạp 1 lần trong `Setup`), rồi tạo `new Material(gốc)` + `_BaseColor` như cũ. Nạp được null → `Debug.LogError("SIMVIEW thiếu Resources/Materials/Landmark.mat")` và dùng `GraphicsSettings.currentRenderPipeline.defaultMaterial` như cũ. Lý do: bản build không có `defaultMaterial` (0018).
4. Scene `SimView.unity` = nội dung của `TerrainView.unity`, thêm component `SimView` trên GameObject Terrain. Giữ nguyên `SimView.unity.meta` (guid scene không đổi). `TerrainView.unity` không đổi.
5. Không đổi `TerrainChunkView`, `DebugFlyCamera`, `MinimapView`, Game.Core, bộ sinh, JSON. Không build APK (SimView chưa có trong build).
6. Log 1 dòng khi Setup xong ở nhánh v3: `SIMVIEW trại {Site}, camera ({x:F0}, {y:F0}, {z:F0}), tìm chỗ {ms:F1} ms, mảng tìm đường {Sim.PathfinderCellCount} ô`.
7. Giờ bắt đầu/kết thúc lấy bằng `date "+%Y-%m-%d %H:%M"` ngay lúc đó.

## Phạm vi
- **Được sửa:**
  - `client/Assets/Game/Client/SimView.cs`
  - `client/Assets/Game/Client/Scenes/SimView.unity`
  - mục Kết quả / Kết quả test / Vướng mắc / Kiểm chứng của brief này.
- **Được tạo:** `client/Assets/Game/Tests/Client/SimViewMapsPlayTests.cs` (+ `.meta` Unity tự tạo).
- **Không được sửa:** mọi file khác, kể cả `SimViewPlayTests.cs` và `TerrainView.unity`. `ProjectSettings/` phải trả lại sau test.

## Các bước
0. `date "+%Y-%m-%d %H:%M"` → ghi "Bắt đầu" và "Base commit" (`git rev-parse --short HEAD`) vào Kết quả; trạng thái `đang làm`.

1. **`SimView.cs`:**
   1. Không thêm `using` nào. `DemoSite` nằm trong `Game.Core.Sim`, đã import. Giữ `using UnityEngine.Rendering;` vì nhánh dự phòng của QĐ 3 vẫn dùng.
   2. Thêm trường và property cạnh `public Simulation Sim { get; private set; }`:
      ```csharp
      /// <summary>Chỗ đặt trại trên world nhiều map (world 7e, brief 0020); null khi chạy đảo v2.</summary>
      public DemoSite? Site { get; private set; }
      UnityEngine.Material boxBase;
      ```
   3. Trong `Setup()`, thay 2 dòng `Sim = new Simulation(cfg, view.World);` (`:103`) và `SimDemo.Build(Sim);` (`:106`) như sau. Giữ nguyên 2 dòng gắn `view.Editor` (`:104-105`), đặt chúng **sau** khi tạo `Sim` như cũ.
      ```csharp
      var cfg = ConfigLoader.LoadSimConfig();
      if (view.Maps == null)
      {
          Sim = new Simulation(cfg, view.World);
          view.Editor.IsOccupied = Sim.IsOccupied;
          view.Editor.CellChanged += Sim.OnCellChanged;
          SimDemo.Build(Sim);
      }
      else
      {
          var sw = System.Diagnostics.Stopwatch.StartNew();
          var site = SimDemo.FindSite(view.World, view.Maps, view.Landmarks, 0, ConfigLoader.LoadWorldMapParams().waterLevel, cfg.regionSize);
          double ms = sw.Elapsed.TotalMilliseconds;
          Site = site;
          Sim = new Simulation(cfg, view.World, site.Bounds);
          view.Editor.IsOccupied = Sim.IsOccupied;
          view.Editor.CellChanged += Sim.OnCellChanged;
          SimDemo.Build(Sim, site);
          var camPos = PlaceCamera(site);
          Debug.Log($"SIMVIEW trại {site}, camera ({camPos.x:F0}, {camPos.y:F0}, {camPos.z:F0}), tìm chỗ {ms:F1} ms, mảng tìm đường {Sim.PathfinderCellCount} ô");
      }
      boxBase = Resources.Load<UnityEngine.Material>("Materials/Landmark");
      if (boxBase == null)
          Debug.LogError("SIMVIEW thiếu Resources/Materials/Landmark.mat");
      ```
      Phần còn lại của `Setup()` (clock, interp, markers, dựng hộp, `UpdateBodies()`, `SetFrameRate(60)`) giữ nguyên.
   4. Thêm hàm (đặt sau `Setup()`):
      ```csharp
      /// <summary>Camera nhìn vào trại (brief 0020): cột giữa cửa sổ, cao hơn mặt đất 12, lùi 26 theo −z, xoay Euler(25, 0, 0) như scene.</summary>
      public static Vector3 CameraPosFor(DemoSite site)
      {
          int ccx = site.Dx + SimDemo.BaseX - 1 + 4, ccz = site.Dz + SimDemo.BaseZ - 1 + 3;
          int h = site.Height(ccx, ccz);
          return new Vector3(ccx + 0.5f, h + 12f, ccz + 0.5f - 26f);
      }

      /// <summary>Tâm cửa sổ 9 × 7 (toạ độ thế giới, trên mặt đất cột giữa).</summary>
      public static Vector3 CampCenter(DemoSite site)
      {
          int ccx = site.Dx + SimDemo.BaseX - 1 + 4, ccz = site.Dz + SimDemo.BaseZ - 1 + 3;
          return new Vector3(ccx + 0.5f, site.Height(ccx, ccz) + 1f, ccz + 0.5f);
      }

      Vector3 PlaceCamera(DemoSite site)
      {
          var pos = CameraPosFor(site);
          var cam = Camera.main;
          if (cam == null)
              return pos;
          cam.transform.SetPositionAndRotation(pos, Quaternion.Euler(25f, 0f, 0f));
          return pos;
      }
      ```
   5. Trong `Box(...)`, thay dòng `:218` bằng:
      ```csharp
      var mat = new UnityEngine.Material(boxBase != null ? boxBase : GraphicsSettings.currentRenderPipeline.defaultMaterial);
      ```
   6. Cập nhật `<summary>` của lớp: thêm 1 dòng "World 3 map (world 7e, brief 0020): Terrain dùng MapsV3 → SimDemo.FindSite chọn chỗ trong Map 1, mô phỏng trong vùng 64 × 64, camera đặt ở trại."
   Xong khi: MCP `refresh_unity` → `read_console` (types error) → 0 lỗi; `SimViewPlayTests` (đảo v2, không sửa) đạt.

2. **Scene `SimView.unity`** (sửa bằng văn bản, rồi kiểm bằng Editor):
   1. `cp client/Assets/Game/Client/Scenes/TerrainView.unity client/Assets/Game/Client/Scenes/SimView.unity`. Không đụng `SimView.unity.meta`.
   2. Trong `SimView.unity`, ở GameObject `&1663237066` (Terrain), thêm dòng `  - component: {fileID: 1663237071}` ngay sau dòng `  - component: {fileID: 1663237070}`.
   3. Thêm khối sau ngay **trước** dòng `--- !u!1660057539 &9223372036854775807` (khối `SceneRoots`, cuối file). Dòng `m_Name: ` có 1 dấu cách ở cuối như các khối khác.
      ```yaml
      --- !u!114 &1663237071
      MonoBehaviour:
        m_ObjectHideFlags: 0
        m_CorrespondingSourceObject: {fileID: 0}
        m_PrefabInstance: {fileID: 0}
        m_PrefabAsset: {fileID: 0}
        m_GameObject: {fileID: 1663237066}
        m_Enabled: 1
        m_EditorHideFlags: 0
        m_Script: {fileID: 11500000, guid: cdbf1e5920cdf45b7814fd3978fe3828, type: 3}
        m_Name: 
        m_EditorClassIdentifier: Game.Client::Game.Client.SimView
        maxTicksPerFrame: 5
      ```
   4. Kiểm:
      - `grep -c "cdbf1e5920cdf45b7814fd3978fe3828" client/Assets/Game/Client/Scenes/SimView.unity` → 1;
      - `grep -n "source: 1\|streaming: 1" client/Assets/Game/Client/Scenes/SimView.unity` → có cả hai;
      - `git diff --stat client/Assets/Game/Client/Scenes/TerrainView.unity` → rỗng.
   5. MCP `refresh_unity`, rồi MCP `manage_scene` mở `Assets/Game/Client/Scenes/SimView.unity`, rồi `read_console` → không có lỗi "missing script" hay lỗi đọc scene.
      Bấm Play bằng MCP `manage_editor`, chờ khoảng 3 giây, `read_console` → có dòng `SIMVIEW trại …`, không có `Exception`. Thoát Play.
      Không lưu scene từ Editor sau khi đã chạy Play. Nếu `git diff` của scene đổi thêm ngoài bước 2.2–2.3, ghi lại trong Kết quả.
   Xong khi: các kiểm ở 2.4 và 2.5 đều đạt.

3. **Tạo `client/Assets/Game/Tests/Client/SimViewMapsPlayTests.cs`** (namespace `Game.Client.Tests`; `using System.Collections; using Game.Core; using Game.Core.Sim; using NUnit.Framework; using UnityEngine; using UnityEngine.TestTools;`).
   - Trường: `GameObject camGo, go; TerrainChunkView view; SimView sv;`.
   - SetUp/TearDown như `LandmarkViewPlayTests.cs:15-36`:
     - camera `MainCamera` ở (192, 40, 150), xoay `Quaternion.Euler(25f, 0f, 0f)`;
     - `go` có `TerrainChunkView` (`source = MapsV3`, `streaming = true`, `streamTarget = camGo.transform`);
     - **thêm `sv = go.AddComponent<SimView>()`**;
     - `yield return null; yield return null;` (Start dựng world, rồi Update đầu tiên gọi Setup).
   - Có 3 test:
   1. `SetsUpCampInMap1`:
      - `sv.Sim` khác null; `sv.Site.HasValue`.
      - `var expect = SimDemo.FindSite(view.World, view.Maps, view.Landmarks, 0, ConfigLoader.LoadWorldMapParams().waterLevel, ConfigLoader.LoadSimConfig().regionSize);` → `sv.Site.Value.Dx == expect.Dx`, `Dz == expect.Dz`, `Bounds == expect.Bounds`.
      - `sv.Sim.Bounds == sv.Site.Value.Bounds`; `sv.Sim.PathfinderCellCount == 64 * 64 * view.World.Size.Y`.
      - 3 sinh vật, `sv.BodyCount == 3`, `sv.SproutCount == 4`.
      - `sv.Sim.State.Houses[0].Cell == sv.Site.Value.Shift(SimDemo.HouseA)`.
      - `view.Editor.IsOccupied(k.X, k.Y, k.Z)` với `k = sv.Site.Value.Shift(SimDemo.StorageCell)`.
   2. `CameraLooksAtCamp`:
      - `camGo.transform.position == SimView.CameraPosFor(sv.Site.Value)` (so từng trục, sai số 1e-4).
      - `camGo.transform.rotation.eulerAngles.x` ≈ 25 (sai số 1e-3).
      - `var vp = camGo.GetComponent<Camera>().WorldToViewportPoint(SimView.CampCenter(sv.Site.Value));` → `vp.z > 0`, `0 < vp.x < 1`, `0 < vp.y < 1`.
      - Ô camera đang ở là rỗng: `view.World.GetMaterial(Mathf.FloorToInt(p.x), Mathf.FloorToInt(p.y), Mathf.FloorToInt(p.z)) == BlockMaterial.Empty`.
      - Chờ tối đa 600 khung tới khi `view.MissingColumnsWithin(48) == 0`, rồi assert điều đó. Ghi `Debug.Log($"SIMVIEWTEST nạp quanh trại sau {frames} khung")`.
   3. `SimRunsAndBoxesUseLandmarkMaterial`:
      - Chờ 120 khung → `sv.Sim.State.Tick > 0`.
      - `var baseMat = Resources.Load<Material>("Materials/Landmark");` khác null.
      - Mọi `MeshRenderer` dưới `go.transform.Find("Sim")` có `sharedMaterial.shader == baseMat.shader`.
      - Không có `LogAssert` lỗi (mặc định Unity Test Framework báo fail khi có `Debug.LogError`).
   Xong khi: MCP `run_tests` PlayMode lọc `SimViewMapsPlayTests` → 3/3, và `SimViewPlayTests` → 1/1.

4. **Chạy toàn bộ + commit:**
   1. EditMode cả bộ (322) và PlayMode cả bộ (32 + 3 = 35). Dọn: `git checkout -- client/ProjectSettings/EditorSettings.asset`.
   2. `git status --short`: chỉ còn file trong Phạm vi. Commit code với message `brief 0020: scene SimView trên world 3 map, trại tự đặt trong Map 1`.
   3. `date "+%Y-%m-%d %H:%M"` → ghi "Kết thúc", trạng thái `chờ review`, commit brief với message `brief 0020: kết quả`.

## Tiêu chí xong
- Biên dịch không lỗi; không đổi file ngoài Phạm vi.
- `SimViewPlayTests` (đảo v2) đạt **không sửa**; `SimViewMapsPlayTests` 3/3.
- EditMode 322/322; PlayMode 35/35.
- Mở scene SimView trong Editor và Play: có dòng `SIMVIEW trại …`, không có `Exception` hay lỗi.
- `TerrainView.unity` không đổi; `git status` sạch sau commit.

## Test sau khi xong

### Phần cần test
| Phần | Bước | Test | Kết quả mong đợi |
|---|---|---|---|
| Đảo v2 không đổi | 1 | `SimViewPlayTests.SetsUpDemoAndRestoresFrameRate` (không sửa) | Đạt: kho ở (34, 18, 17), 60 FPS, trả cài đặt FPS |
| Trại trong Map 1, vùng 64 × 64 | 1 | `SimViewMapsPlayTests.SetsUpCampInMap1` | Site trùng `FindSite` chạy riêng; mảng = 64·64·48; 3 sinh vật, 4 mầm; nhà/kho đúng ô dời |
| Camera ở trại + nạp dần | 1 | `CameraLooksAtCamp` | Đúng vị trí/góc; tâm trại trong khung nhìn; camera không trong khối; cột quanh trại nạp đủ ≤ 600 khung |
| Mô phỏng chạy + material hộp | 1 | `SimRunsAndBoxesUseLandmarkMaterial` | Nhịp > 0; mọi hộp dùng shader của `Landmark.mat`; không log lỗi |
| Scene SimView | 2 | grep + mở scene + Play qua MCP | 1 component SimView; `source: 1`, `streaming: 1`; dòng `SIMVIEW trại …`; 0 Exception; TerrainView không đổi |
| Regression chung | — | EditMode + PlayMode cả bộ | 322/322, 35/35 |
| Hình ảnh, cảm giác | — | User xem trên Mac (mục dưới) | User xác nhận |

### Lệnh executor chạy
1. MCP `refresh_unity` → `read_console` (types error) → 0 lỗi.
2. MCP `run_tests` PlayMode lọc `SimViewPlayTests`, `SimViewMapsPlayTests` → 4/4. Ghi dòng `SIMVIEW`, `SIMVIEWTEST`.
3. Kiểm scene theo bước 2.4–2.5.
4. MCP `run_tests` EditMode cả bộ → ghi dòng tổng kết.
5. MCP `run_tests` PlayMode cả bộ → ghi dòng tổng kết. Sau đó dọn: `git checkout -- client/ProjectSettings/EditorSettings.asset`.
6. `git status --short` → commit (bước 4).
7. `date "+%Y-%m-%d %H:%M"` → "Kết thúc"; commit brief.

### User tự test
Trong Unity Editor trên Mac, mở `Assets/Game/Client/Scenes/SimView.unity`, bấm Play và xem:
1. Ngay khi vào: camera nhìn xuống trại. Thấy 2 nhà nâu, 1 kho xám, 4 ô ruộng, 3 sinh vật màu cam/xanh/tím. Địa hình quanh trại hiện trong khoảng 1–2 giây, không còn lỗ trước mặt.
2. Trại nằm trên đất hơi gồ ghề (chênh ≤ 1 khối giữa các cột kề). Ô ruộng, kho và sinh vật bám đúng mặt đất từng cột, không lơ lửng, không chìm. 2 nhà đứng trên nền phẳng.
3. Xem 1–2 phút: sinh vật xới, gieo, tưới, thu hoạch rồi mang về kho. HUD góc trái hiện số hàng trong kho tăng dần. Không có "!" đỏ kéo dài.
4. Mở bản đồ: trại nằm trong Map 1, gần checkpoint Map 1, không đè dấu mốc nào.
5. Bay ra xa khoảng 40 ô rồi bay lại: địa hình nạp/gỡ bình thường. Sinh vật vẫn chạy (mô phỏng không phụ thuộc chunk đã nạp).
6. Đào/đặt khối gần ruộng: sinh vật đi vòng hoặc huỷ vụ như ở scene cũ. Không đặt được khối lên sinh vật, nhà, kho.
7. Nhấn F1/F2: FPS khoá 30/60 như cũ.

## Dừng và chuyển lại Opus khi
- Brief mơ hồ hoặc mâu thuẫn với code thật (số dòng không khớp nội dung trích).
- `FindSite` ném lỗi trong PlayMode, hoặc camera nằm trong khối (test 2 hỏng). Không tự đổi công thức camera.
- Scene mở ra có lỗi "missing script" hay lỗi đọc YAML sau bước 2.
- `SimViewPlayTests` (đảo v2) hỏng.
- Cần quyết định ngoài "Quyết định đã chốt", hoặc phải sửa file ngoài "Phạm vi".
- Cùng một lỗi sửa 2 lần vẫn chưa qua.

---
<!-- Từ đây trở xuống: EXECUTOR ghi. -->

## Kết quả
- Bắt đầu: 2026-10-21 12:16
- Base commit: 8af4a2f
- Chạy bằng: claude-sonnet-5-5 · medium
- Kết thúc: 2026-10-21 12:23; commit code 3a06dbb
- File: SimView.cs, Scenes/SimView.unity (sửa); Tests/Client/SimViewMapsPlayTests.cs (+.meta, mới). Editor không ghi thêm gì vào scene (không lưu sau Play).

## Kết quả test
- Biên dịch: 0 lỗi. Scene: `grep -c` guid SimView = 1; `source: 1`, `streaming: 1` có; `TerrainView.unity` không đổi.
- PlayMode lọc `SimViewPlayTests` + `SimViewMapsPlayTests`: 4/4 đạt (đảo v2 không sửa test → đạt; 3 test mới đạt).
- Dòng log: `SIMVIEW trại dời (98, 200), mặt y 17–20, vùng x 99..162, z 186..249, camera (132, 30, 193), tìm chỗ 17–22 ms, mảng tìm đường 196608 ô`; `SIMVIEWTEST nạp quanh trại sau 0 khung`.
- Mở scene SimView bằng MCP `manage_scene`: không lỗi missing script/YAML. Play: có `SIMVIEW trại …`, `LANDMARKVIEW 9`, `MINIMAP … 9 dấu mốc`; 0 Exception/lỗi trong console.
- EditMode cả bộ: 322/322. PlayMode cả bộ: 35/35. Đã `git checkout -- EditorSettings.asset`.
- Hình ảnh/cảm giác: **chờ user** (mục "User tự test").

## Vướng mắc
Không có.

---
<!-- PLANNER ghi khi review. -->

## Review
