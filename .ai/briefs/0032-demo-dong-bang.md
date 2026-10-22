# Brief 0032 — Demo trên đồng bằng san phẳng (SimView + TerrainView), giãn bố trí trại

> Trạng thái: chờ làm
> Review: đầy đủ + user xem
> Model thực thi: Sonnet · medium
> Cập nhật: 2026-10-22

<!-- Planner (Opus) viết, Executor (Sonnet) làm theo ở PHIÊN KHÁC — executor không có lịch sử chat, không được tự quyết.
Trạng thái: chờ làm → đang làm → chờ review → đã duyệt; hoặc → cần Opus (executor dừng, hỏi ở mục Vướng mắc).
Review: nhẹ (đọc mục Kết quả) | đầy đủ (đọc cả diff) | user xem (có phần user tự kiểm bằng mắt/chơi thử) — có thể ghép "đầy đủ + user xem".
Model thực thi: BẮT BUỘC, đúng dạng `<Model> · <effort>` — Model: Opus | Sonnet | Haiku | Fable; effort: low | medium | high | xhigh | max (chọn theo AGENTS.md §10.2).
Executor kiểm tra model + effort của phiên TRƯỚC khi làm (scripts/brief_check.py); lệch hoặc không xác định được → cảnh báo user và DỪNG.
File được commit. Tự kiểm trước khi giao (AGENTS.md §10.3): executor có phải hỏi gì không? còn chỗ nào bỏ ngỏ 2 phương án? -->

## Mục tiêu
Làm theo [task contract](../tasks/m1-demo-dong-bang.md): scene SimView và TerrainView (world nhiều map) diễn ra trên **đồng bằng san phẳng 48 × 48** giữa Map 1 (dùng lại `SimStress.FlattenPlain` của SimBench); trại demo đặt giữa đồng bằng theo **bố trí giãn**; camera nhìn vào trại. Bỏ `SimDemo.FindSite` / `DemoSite`.

Ước lượng 0,5–1 ngày-người. Ghi giờ bắt đầu/kết thúc (theo `date`) vào mục Kết quả.

## Bối cảnh cần đọc
Đường dẫn tính từ `client/Assets/Game/`. Số dòng theo commit `abd836c`.
- `Core/Sim/SimStress.cs`: `StressPlain` (`:7-14`: `X0, Z0, Size, Ground, Bounds`, `Contains`); `FlattenPlain(world, maps, map, bedrockLayers, dirtDepth, waterLevel, regionSize)` (`:22-72`); `WithoutPlain(landmarks, plain)` (`:75-82`).
- `Core/Sim/SimDemo.cs`: `DemoSite` (`:7-46`); hằng + bố trí đảo v2 (`:55-68`); `Build(sim)` / `Build(sim, DemoSite)` / `Build(sim, Func<CellPos, CellPos>)` (`:71-95`); `FindSite` + `IsSiteWalkable` + `FlatSquare` + `NearLandmark` (`:97-193`); `KeepFarming` (`:195-201`).
- `Client/SimView.cs`: summary lớp (`:13-22`); `Plain` (`:31`); `Site` (`:65-66`); `Awake` (`:73-79`, `afterGenerate` chỉ khi `stressBench`); `BenchCameraPos` (`:81`); `OnAfterGenerate` (`:83-93`); `Setup` (`:142-178`, nhánh world nhiều map `:167-177`); `CameraPosFor` / `CampCenter` / `PlaceCamera` (`:201-224`).
- `Client/TerrainChunkView.cs`: `afterGenerate` (`:71`) — gọi trong `Build()` sau khi đặt dấu mốc, trước khi dựng khối dấu mốc/editor/mesh (`:159-160`); `Build()` chạy trong `Start`, nên gán `afterGenerate` trong `Awake` của component cùng GameObject là kịp.
- `Client/DebugFlyCamera.cs`: `Start` đọc `yaw`/`pitch` từ `transform.eulerAngles` (`:25-30`), chỉ ghi lại rotation khi kéo chuột giữa (`:58-63`) → camera đặt từ ngoài sau `Start` sẽ giật góc ở lần kéo đầu.
- Scene `Client/Scenes/TerrainView.unity`: GameObject `Terrain` có `TerrainChunkView` (source MapsV3), không có `SimView`.
- Test chạm tới: `Tests/World/SimDemoMapsTests.cs` (toàn bộ dựa vào `FindSite`), `Tests/Client/SimViewMapsPlayTests.cs` (`:43-89`), `Tests/Client/EditOcclusionPlayTests.cs` (`:86-94`).
- API Unity dùng (đã có trong code, không mới): `Camera.main`, `Transform.SetPositionAndRotation`, `Quaternion.Euler`, `Component.GetComponent<T>`, `RequireComponent`.

## Quyết định đã chốt (không được đổi)
**User 2026-10-22 (task contract):**
1. Đồng bằng = `SimStress.FlattenPlain(world, maps, 0, p.bedrockLayers, p.dirtDepth, p.waterLevel, regionSize)` + `SimStress.WithoutPlain` — như SimBench. Không đổi `FlattenPlain`.
2. Áp cho SimView (world nhiều map, cả demo lẫn bench) và TerrainView (qua component mới `DemoPlain`). Đảo v2 (`view.Maps == null`) giữ bố trí cũ `SimDemo.Build(sim)`.
3. Bố trí giãn, (dx, y, dz) tính từ tâm đồng bằng `(X0 + Size / 2, Z0 + Size / 2)`, y: 0 = ô mặt đất (`Ground`), 1 = ô ngay trên mặt đất:
   - Ruộng: (−6, 0, −2), (−5, 0, −2), (−6, 0, −1), (−5, 0, −1).
   - Kho: (5, 1, −1). Nhà A: (−4, 1, 4). Nhà B: (1, 1, 4).
   - Sinh vật L1 (−3, 1, 2), L2 (−2, 1, 2) ở nhà A; L3 (2, 1, 2) ở nhà B. Mỗi ô ruộng 1 vụ Cây A ban đầu.
4. Camera demo: (tâm x + 0,5, Ground + 16, tâm z − 21), xoay `Euler(35, 0, 0)`. Bench giữ `BenchCameraPos` + `Euler(50, 0, 0)`.
5. Bỏ `DemoSite`, `FindSite`, `IsSiteWalkable`, `FlatSquare`, `NearLandmark`, `SiteSizeX/Z`, `LandmarkClearance`, `Build(sim, DemoSite)`, `SimView.Site`/`CameraPosFor`/`CampCenter`/`PlaceCamera`.

**Planner chốt khi viết brief:**
6. Code dùng chung đặt ở component mới `Client/DemoPlain.cs` (static `Flatten`, `PlaceCamera`, `CameraPos`, `CampCenter`); SimView gọi các hàm static này, **không** gắn `DemoPlain` vào scene SimView/SimBench (SimView tự gán `afterGenerate`).
7. `DebugFlyCamera` thêm `SyncAngles()`; `PlaceCamera` gọi nó nếu camera có `DebugFlyCamera`.
8. Log: SimView ghi `SIMBENCH …` (bench) / `SIMVIEW …` (demo); `DemoPlain` ghi `DEMOPLAIN …`. Các log `SIMBENCH t=…` / `SIMBENCH TONG` của bộ đo không đổi.

## Phạm vi
- **Được sửa:** `Core/Sim/SimDemo.cs`, `Client/SimView.cs`, `Client/DebugFlyCamera.cs`, **tạo** `Client/DemoPlain.cs` (+ `.meta` do Unity sinh), scene `Client/Scenes/TerrainView.unity` (chỉ thêm component `DemoPlain` vào GameObject `Terrain`), test: `Tests/World/SimDemoMapsTests.cs`, `Tests/Client/SimViewMapsPlayTests.cs`, `Tests/Client/EditOcclusionPlayTests.cs`, **tạo** `Tests/Client/DemoPlainPlayTests.cs` (+ `.meta`).
- **Không được sửa:** `design/`, task contract, `SimStress.cs`, `TerrainChunkView.cs`, scene SimView/SimBench, các file khác. Test khác đỏ vì đổi này → dừng, ghi Vướng mắc.

## Các bước
Đường dẫn tính từ `client/Assets/Game/`.

1. **`Core/Sim/SimDemo.cs`.**
   - Xoá `DemoSite` (`:7-46`), `FindSite`, `IsSiteWalkable`, `FlatSquare`, `NearLandmark`, hằng `SiteSizeX`, `SiteSizeZ`, `LandmarkClearance`, `Build(Simulation sim, DemoSite site)`. Giữ `BaseX/BaseZ/BaseGround` và bố trí đảo v2, `Species`, `KeepFarming`. Xoá `using System.Collections.Generic;` nếu không còn dùng.
   - Summary lớp: thay câu "World nhiều map: FindSite + Build(sim, site) …" bằng "World nhiều map: Build(sim, plain) đặt bố trí giãn giữa đồng bằng san phẳng (task m1-demo-dong-bang)."
   - Thêm (sau bố trí đảo v2):
     ```csharp
     /// <summary>Bố trí giãn trên đồng bằng (task m1-demo-dong-bang): (dx, y, dz) tính từ tâm đồng bằng; y 0 = ô mặt đất, 1 = ô trên mặt đất.</summary>
     public static readonly CellPos PlainHouseA = new CellPos(-4, 1, 4);
     public static readonly CellPos PlainHouseB = new CellPos(1, 1, 4);
     public static readonly CellPos PlainStorage = new CellPos(5, 1, -1);
     public static readonly CellPos[] PlainSoil =
         { new CellPos(-6, 0, -2), new CellPos(-5, 0, -2), new CellPos(-6, 0, -1), new CellPos(-5, 0, -1) };
     public static readonly CellPos[] PlainCreatureCells =
         { new CellPos(-3, 1, 2), new CellPos(-2, 1, 2), new CellPos(2, 1, 2) };

     /// <summary>Ô thế giới của ô tương đối rel trong bố trí giãn.</summary>
     public static CellPos OnPlain(StressPlain plain, CellPos rel) =>
         new CellPos(plain.X0 + plain.Size / 2 + rel.X, plain.Ground + rel.Y, plain.Z0 + plain.Size / 2 + rel.Z);
     ```
   - Thay ba hàm `Build` bằng:
     ```csharp
     /// <summary>Bố trí đảo v2. Gọi một lần trên mô phỏng mới (chưa Tick). Trả id kho.</summary>
     public static int Build(Simulation sim) => Build(sim, c => c, HouseA, HouseB, StorageCell, CreatureCells, Soil);

     /// <summary>Bố trí giãn giữa đồng bằng (task m1-demo-dong-bang). Trả id kho.</summary>
     public static int Build(Simulation sim, StressPlain plain) =>
         Build(sim, c => OnPlain(plain, c), PlainHouseA, PlainHouseB, PlainStorage, PlainCreatureCells, PlainSoil);

     static int Build(Simulation sim, Func<CellPos, CellPos> at, CellPos houseA, CellPos houseB, CellPos storageCell, CellPos[] creatureCells, CellPos[] soilCells)
     ```
     Thân hàm private giữ nguyên logic cũ, chỉ thay `HouseA/HouseB/StorageCell/CreatureCells/Soil` bằng tham số tương ứng.
   - Xong khi: `grep -rn "DemoSite\|FindSite\|SiteSize\|LandmarkClearance" client/Assets/Game` chỉ còn trong file test sẽ sửa ở bước 5–6.

2. **`Client/DebugFlyCamera.cs`** — thêm và cho `Start` gọi nó:
   ```csharp
   /// <summary>Đọc lại yaw/pitch từ transform — gọi sau khi camera bị đặt từ ngoài (task m1-demo-dong-bang), tránh giật góc ở lần kéo chuột đầu.</summary>
   public void SyncAngles()
   {
       var e = transform.eulerAngles;
       yaw = e.y;
       pitch = e.x > 180f ? e.x - 360f : e.x;
   }
   ```
   `Start()` thân thành `SyncAngles();`.

3. **Tạo `Client/DemoPlain.cs`:**
   ```csharp
   using System.Collections.Generic;
   using Game.Core;
   using Game.Core.Sim;
   using UnityEngine;

   namespace Game.Client
   {
       /// <summary>
       /// Đồng bằng demo (task m1-demo-dong-bang): san phẳng 48 × 48 cột giữa Map 1 (SimStress.FlattenPlain) trước khi dựng mesh, bỏ dấu mốc trong đồng bằng,
       /// đặt camera nhìn vào tâm. Gắn vào GameObject có TerrainChunkView (scene TerrainView). SimView dùng các hàm static, không gắn component này.
       /// </summary>
       [RequireComponent(typeof(TerrainChunkView))]
       public class DemoPlain : MonoBehaviour
       {
           /// <summary>Góc nghiêng camera demo (độ).</summary>
           public const float Pitch = 35f;

           public StressPlain? Plain { get; private set; }

           void Awake() => GetComponent<TerrainChunkView>().afterGenerate = OnAfterGenerate;

           IReadOnlyList<Landmark> OnAfterGenerate(WorldData world, MapTable maps, WorldMapParams p, IReadOnlyList<Landmark> lms)
           {
               var kept = Flatten(world, maps, p, lms, out var plain);
               Plain = plain;
               var pos = CameraPos(plain);
               PlaceCamera(pos, Pitch);
               Debug.Log($"DEMOPLAIN {plain}, bỏ {lms.Count - kept.Count} dấu mốc, camera ({pos.x:F0}, {pos.y:F0}, {pos.z:F0})");
               return kept;
           }

           /// <summary>San đồng bằng giữa Map 1 (ghi thẳng vào world) và trả dấu mốc ngoài đồng bằng.</summary>
           public static IReadOnlyList<Landmark> Flatten(WorldData world, MapTable maps, WorldMapParams p, IReadOnlyList<Landmark> lms, out StressPlain plain)
           {
               plain = SimStress.FlattenPlain(world, maps, 0, p.bedrockLayers, p.dirtDepth, p.waterLevel, ConfigLoader.LoadSimConfig().regionSize);
               return SimStress.WithoutPlain(lms, plain);
           }

           /// <summary>Camera demo: tâm x + 0,5, cao hơn mặt đồng bằng 16, lùi 21 theo −z.</summary>
           public static Vector3 CameraPos(StressPlain p) =>
               new Vector3(p.X0 + p.Size / 2 + 0.5f, p.Ground + 16f, p.Z0 + p.Size / 2 + 0.5f - 21f);

           /// <summary>Điểm giữa trại (bố trí giãn dz −2..5), trên mặt đất.</summary>
           public static Vector3 CampCenter(StressPlain p) =>
               new Vector3(p.X0 + p.Size / 2 + 0.5f, p.Ground + 1f, p.Z0 + p.Size / 2 + 1.5f);

           /// <summary>Đặt Camera.main (nếu có) và đồng bộ góc DebugFlyCamera.</summary>
           public static void PlaceCamera(Vector3 pos, float pitch)
           {
               var cam = Camera.main;
               if (cam == null) return;
               cam.transform.SetPositionAndRotation(pos, Quaternion.Euler(pitch, 0f, 0f));
               var fly = cam.GetComponent<DebugFlyCamera>();
               if (fly != null) fly.SyncAngles();
           }
       }
   }
   ```
   Nếu `Landmark` / `WorldData` / `MapTable` / `WorldMapParams` nằm ở namespace khác `Game.Core` → thêm `using` đúng như `SimView.cs` đang dùng cho chữ ký `OnAfterGenerate` (không đổi chữ ký).

4. **`Client/SimView.cs`.**
   - Summary lớp `:21`: "World 3 map (task m1-demo-dong-bang): san đồng bằng 48 × 48 giữa Map 1 (DemoPlain.Flatten), trại SimDemo bố trí giãn ở giữa, mô phỏng trong vùng 64 × 64, camera nhìn vào trại."
   - Summary `Plain` (`:31` — thêm nếu chưa có): "Đồng bằng đã san (world nhiều map, cả demo lẫn bench); null khi chạy đảo v2."
   - Xoá `Site` (`:65-66`).
   - `Awake`: `if (stressBench) view.afterGenerate = OnAfterGenerate;` → `view.afterGenerate = OnAfterGenerate;` (chỉ được gọi với world nhiều map).
   - `OnAfterGenerate` thành:
     ```csharp
     IReadOnlyList<Landmark> OnAfterGenerate(WorldData world, MapTable maps, WorldMapParams p, IReadOnlyList<Landmark> lms)
     {
         var kept = DemoPlain.Flatten(world, maps, p, lms, out var plain);
         Plain = plain;
         if (stressBench) DemoPlain.PlaceCamera(BenchCameraPos(plain), 50f);
         else DemoPlain.PlaceCamera(DemoPlain.CameraPos(plain), DemoPlain.Pitch);
         Debug.Log($"{(stressBench ? "SIMBENCH" : "SIMVIEW")} {plain}, bỏ {lms.Count - kept.Count} dấu mốc trong đồng bằng");
         return kept;
     }
     ```
   - `Setup` nhánh world nhiều map (`:167-177`) thành:
     ```csharp
     else
     {
         if (!Plain.HasValue)
         {
             Debug.LogError("SIMVIEW thiếu đồng bằng (afterGenerate bị ghi đè?)");
             enabled = false;
             return;
         }
         var plain = Plain.Value;
         Sim = new Simulation(cfg, view.World, plain.Bounds);
         Sim.AttachEditor(view.Editor);
         SimDemo.Build(Sim, plain);
         Debug.Log($"SIMVIEW trại giữa {plain}, mảng tìm đường {Sim.PathfinderCellCount} ô");
     }
     ```
   - Xoá `CameraPosFor`, `CampCenter`, `PlaceCamera` (`:201-224`).
   - Xong khi: biên dịch, console 0 lỗi.

5. **`Tests/World/SimDemoMapsTests.cs`** — viết lại trên đồng bằng (giữ tên lớp, `Data`, `Run`):
   - Summary lớp: "Task m1-demo-dong-bang: trại demo bố trí giãn giữa đồng bằng san phẳng của Map 1, mô phỏng trong vùng 64 × 64."
   - `Setup(out WorldData world, out SimConfig cfg, out StressPlain plain)`: `var p = new WorldMapParams(); world = WorldMapGenerator.Generate(p, out var maps); cfg = JsonUtility.FromJson<SimConfig>(File.ReadAllText(Data("sim_m1.json"))); plain = SimStress.FlattenPlain(world, maps, 0, p.bedrockLayers, p.dirtDepth, p.waterLevel, cfg.regionSize);`
   - `NewSim(out WorldData world, out StressPlain plain)`: `Setup` → `new Simulation(cfg, world, plain.Bounds)` → `SimDemo.Build(sim, plain)`.
   - Xoá `SiteIsFlatDryInMap1AwayFromLandmarks`, `SiteIsDeterministic`.
   - `BuildPlacesShiftedLayout` → `BuildPlacesPlainLayout`: như cũ nhưng kỳ vọng `SimDemo.OnPlain(plain, SimDemo.PlainHouseA / PlainHouseB / PlainStorage / PlainCreatureCells[i] / PlainSoil[i])`; thêm: mọi `Plots[i].Soil.Y == plain.Ground`; `Houses[0].Cell.Y == plain.Ground + 1`; mọi ô bố trí nằm trong đồng bằng (`plain.Contains(x, z)` cho nhà — cả 2 × 2 —, kho, ruộng, sinh vật); `sim.Bounds == plain.Bounds`; giữ assert `PathfinderCellCount` + dòng log `SIMMEM`.
   - `OneRoundFillsStorageOnMaps` → `OneRoundFillsStorageOnPlain`: giữ kỳ vọng (0 kẹt, `Produce[0] == 4`, `Seeds[0] == 10`) với 4000 nhịp. Nếu 4000 nhịp chưa đủ cho bố trí giãn → được tăng lên 6000; vẫn chưa đạt → dừng, ghi Vướng mắc.
   - `KeepFarmingNoOverlapIdleNearHomeOnMaps` → `…OnPlain`: giữ nguyên thân (8000 nhịp, 0 kẹt, `produce >= 8`, 0 chồng ô, 0 rảnh trên ruộng, 0 rảnh ngoài bán kính nhà); đổi log `SIMMAPS` → `SIMPLAIN`.
   - Xoá `using` không còn dùng (vd `System.Diagnostics` nếu bỏ `Stopwatch`).

6. **Test PlayMode.**
   - `SimViewMapsPlayTests`:
     - `SetsUpCampInMap1` → `SetsUpCampOnPlain`: `sv.Plain.HasValue`; `plain.Bounds == sv.Sim.Bounds`; giữ assert `PathfinderCellCount`, 3 sinh vật, `BodyCount == 3`, `SproutCount == 4`; `Houses[0].Cell == SimDemo.OnPlain(plain, SimDemo.PlainHouseA)`; ô kho `SimDemo.OnPlain(plain, SimDemo.PlainStorage)` bị chiếm (`view.Editor.IsOccupied`); mọi cột trong đồng bằng: `MinimapImage.ColumnInfo(view.World, x, z, out surf, out depth)` → `surf == plain.Ground`, `depth == 0`, vật liệu `Grass`; không dấu mốc nào trong đồng bằng (`plain.Contains(l.X, l.Z)` false với mọi `view.Landmarks`).
     - `CameraLooksAtCamp`: `want = DemoPlain.CameraPos(plain)`; góc x `DemoPlain.Pitch` (35); điểm nhìn `DemoPlain.CampCenter(plain)` nằm trong viewport; phần còn lại giữ.
   - `EditOcclusionPlayTests.OpenGroundStillPicks`: `var plain = sv.Plain.Value; int x = plain.X0 + plain.Size / 2, z = plain.Z0 + plain.Size / 2;` (tâm, không có nhà/kho/ruộng) → pick trúng, `dig == new Vector3Int(x, plain.Ground, z)`.
   - **Tạo** `Tests/Client/DemoPlainPlayTests.cs` (namespace `Game.Client.Tests`, SetUp/TearDown như `SimViewMapsPlayTests` nhưng thêm `camGo.AddComponent<DebugFlyCamera>()` và gắn `DemoPlain` thay cho `SimView`):
     - `FlattensPlainAndRemovesLandmarks`: `dp.Plain.HasValue`; mọi cột trong đồng bằng `surf == Ground`, `depth == 0`, `Grass`; không dấu mốc trong đồng bằng.
     - `PlacesCameraAndSyncsFly`: vị trí camera == `DemoPlain.CameraPos(plain)` (sai số 1e-4), góc x == 35 (1e-3); `DemoPlain.CampCenter(plain)` trong viewport; camera ở ô `Empty`.
   - `SimBenchPlayTests`, `LandmarkViewPlayTests`, `MinimapViewPlayTests`, `TerrainStreamingPlayTests`: không sửa, phải đạt.

7. **Scene `TerrainView.unity`** (qua MCP, Editor không ở Play): mở scene `Assets/Game/Client/Scenes/TerrainView.unity` → thêm component `Game.Client.DemoPlain` vào GameObject `Terrain` (`manage_components` add) → lưu scene (`manage_scene` save). Kiểm `git diff --stat` chỉ đổi scene TerrainView (không đổi SimView/SimBench). Mở lại scene SimView nếu trước đó Editor đang mở SimView.

## Tiêu chí xong
- Toàn bộ test EditMode + PlayMode đạt (số mới ghi vào Kết quả).
- `grep -rn "DemoSite\|FindSite\|SiteSize\|LandmarkClearance\|CameraPosFor" client/Assets/Game` rỗng.
- `grep -rn "using UnityEngine" client/Assets/Game/Core` rỗng.
- Scene TerrainView có `DemoPlain` trên `Terrain`; scene SimView/SimBench không đổi.

## Test sau khi xong

### Phần cần test
- Bố trí giãn đúng toạ độ trên đồng bằng (bước 1) · `SimDemoMapsTests.BuildPlacesPlainLayout` · đúng ô, trong đồng bằng.
- Một vòng trồng–thu–chở + `KeepFarming` trên đồng bằng (bước 1) · `OneRoundFillsStorageOnPlain`, `KeepFarmingNoOverlapIdleNearHomeOnPlain` · 0 kẹt, 0 chồng ô.
- Bố trí đảo v2 không đổi (bước 1) · `SimDemoTests`, `SimViewPlayTests`, `SimFarmUiPlayTests` · đạt, không sửa.
- SimView trên đồng bằng (bước 4) · `SimViewMapsPlayTests` · đồng bằng phẳng, không dấu mốc, trại đúng, camera nhìn thấy trại.
- Chặn tia đào/đặt trên đồng bằng (bước 4) · `EditOcclusionPlayTests` · đạt.
- `DemoPlain` + `SyncAngles` (bước 2–3) · `DemoPlainPlayTests` · đạt.
- Bench không đổi (bước 4) · `SimBenchPlayTests`, `SimStressTests` · đạt, không sửa.
- World không gắn component không đổi · `LandmarkViewPlayTests`, `MinimapViewPlayTests`, `TerrainStreamingPlayTests` · đạt, không sửa.
- Scene TerrainView (bước 7) · `git diff --stat` + user xem · chỉ scene TerrainView đổi.

### Lệnh executor chạy
1. `read_console` (Error) sau khi biên dịch → 0 lỗi.
2. `run_tests` EditMode qua MCP (Unity Editor đang mở — 0010) → dòng tổng kết.
3. `run_tests` PlayMode qua MCP → dòng tổng kết; sau đó `git checkout -- client/ProjectSettings/EditorSettings.asset`.
4. grep ở "Tiêu chí xong".
5. `git diff --stat` trước commit: chỉ file trong Phạm vi.

### User tự test
1. Mở scene **SimView**, Play: trại nằm giữa một đồng bằng cỏ phẳng rộng; thấy rõ 2 nhà (hàng sau), kho (phải), 4 ô ruộng (trái), 3 sinh vật; camera nhìn bao được cả trại. Kéo chuột giữa: góc không giật.
2. Cầm Xới đánh dấu thêm ô trên đất trống quanh trại → sinh vật tới xới; đào/đặt trên đồng bằng chạy bình thường.
3. Mở scene **TerrainView**, Play: camera nhìn vào đồng bằng phẳng giữa Map 1; xung quanh vẫn là địa hình tự nhiên; minimap có vùng phẳng.
4. Sau đó xem tiếp bảng ô ruộng của brief 0031 trên SimView.

## Dừng và chuyển lại Opus khi
- Brief mơ hồ hoặc mâu thuẫn với code thật (vd số dòng lệch nhiều, `afterGenerate` không được gọi, `OneRoundFillsStorageOnPlain` cần > 6000 nhịp).
- Cần một quyết định thuộc AGENTS.md §3.4 hoặc ngoài "Quyết định đã chốt".
- Cùng một lỗi sửa 2 lần vẫn chưa qua.
- Phải sửa file ngoài "Phạm vi".
- Cần API Unity không có trong brief.

---
<!-- Từ đây trở xuống: EXECUTOR ghi. -->

## Kết quả

## Kết quả test

## Vướng mắc

---
<!-- PLANNER ghi khi review. -->

## Review
