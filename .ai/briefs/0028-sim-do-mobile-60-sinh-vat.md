# Brief 0028 — Simulation bước 7: đo trên điện thoại với 60 sinh vật trên đồng bằng giữa Map 1

> Trạng thái: chờ review
> Review: đầy đủ + user xem
> Model thực thi: Sonnet · medium
> Cập nhật: 2026-10-21 20:30

<!-- Planner (Opus) viết, Executor (Sonnet) làm theo ở PHIÊN KHÁC — executor không có lịch sử chat, không được tự quyết.
Trạng thái: chờ làm → đang làm → chờ review → đã duyệt; hoặc → cần Opus (executor dừng, hỏi ở mục Vướng mắc).
Review: nhẹ (đọc mục Kết quả) | đầy đủ (đọc cả diff) | user xem (có phần user tự kiểm bằng mắt/chơi thử) — có thể ghép "đầy đủ + user xem".
Model thực thi: BẮT BUỘC, đúng dạng `<Model> · <effort>` — Model: Opus | Sonnet | Haiku | Fable; effort: low | medium | high | xhigh | max (chọn theo AGENTS.md §10.2).
Executor kiểm tra model + effort của phiên TRƯỚC khi làm (scripts/brief_check.py); lệch hoặc không xác định được → cảnh báo user và DỪNG.
File được commit. Tự kiểm trước khi giao (AGENTS.md §10.3): executor có phải hỏi gì không? còn chỗ nào bỏ ngỏ 2 phương án? -->

## Mục tiêu
Làm bước 7 theo [task contract](../tasks/m1-sim-buoc-7-do-mobile-60-sinh-vat.md): đo mô phỏng trên điện thoại khi có **60 sinh vật** đang làm việc. Ngưỡng đạt: **p95 ≤ 5 ms/nhịp**. Task này **chỉ đo**, không tối ưu.

Phần mới:
- Bố trí đo nằm trên một **đồng bằng 48 × 48** do code làm phẳng ở giữa Map 1, để dễ quan sát (user, 2026-10-21). Bố trí gồm 30 nhà, 60 sinh vật, 5 kho và 15 lệnh trồng cây × 4 ô.
- Scene `SimBench` và APK riêng. Cứ 10 giây ghi một dòng `SIMBENCH` ra logcat; sau 240 giây ghi một dòng tổng.

Ước lượng: 1 ngày-người. Ghi giờ bắt đầu/kết thúc vào mục Kết quả.

## Bối cảnh cần đọc
Đường dẫn tính từ `client/Assets/Game/`.
- `Core/Sim/SimDemo.cs` — mẫu cho file mới:
  - `DemoSite` (`:7-45`);
  - `public static readonly string[] Species = { "L1", "L2", "L3" };` (`:64`);
  - `FindSite` (`:91-148`): cách dùng `MinimapImage.ColumnInfo(world, x, z, out int surf, out int depth)` và cách kẹp vùng `regionSize`;
  - `KeepFarming` (`:191-196`).
- `Core/Sim/Simulation.cs`:
  - Constructor `public Simulation(SimConfig config, WorldData world, SimBounds bounds)` (`:29`). `PathfinderCellCount` (`:42`).
  - `AddCreature(string speciesId, CellPos cell)` (`:44`), `AddStorage(CellPos cell)` (`:93`), `AddHouse(CellPos cell)` (`:122`), `AssignHome(int creatureId, int houseId)` (`:142`), `AddFarmOrder(IReadOnlyList<CellPos> soilCells, int storageId)` (`:760`).
  - `Tick()` (`:392-413`) gọi lần lượt 9 pha: `TerrainPhase` (khi `terrainDirty`), `HomePhase` (khi `homeDirty`), `WorkPhase`, `CropPhase`, `MovePhase`, `AssignPhase`, `StuckPhase`, `IdlePhase`, `ReportPhase`.
- `Core/Sim/GridPathfinder.cs`:
  - `FindPathToNearest` (`:172`) và `FloodRegion` (`:222`) là BFS.
  - `bool Search(CellPos start, List<CellPos> goals, int sx, int sz, int sy, List<CellPos> result)` (`:269`) là A\*. `FindPath` và `FindPathToAny` đều gọi hàm này.
- `Core/Sim/SimData.cs:183-191` — `Storage`: `Seeds`, `Produce`, `Reserved`, `Free`.
- `Core/Cell.cs`: `public static ushort Make(BlockMaterial material, int waterLevel = 0)` (`:34`). `BlockMaterial` (`:4-16`).
- `Core/WorldData.cs`: `Set(int x, int y, int z, ushort cell)` (`:79`), `Size` (`:57`).
- `Core/WorldMapParams.cs`: `bedrockLayers` (`:27`), `dirtDepth` (`:28`), `waterLevel` (`:29`).
- `Core/LandmarkPlacer.cs`:
  - `Landmark` có `X`, `Z` (`:16`);
  - `LandmarkPlacer.Place(world, maps, p)` trả `List<Landmark>`;
  - checkpoint là ô phẳng gần tâm map nhất (`:79-104`), nên sẽ rơi vào đồng bằng.
- `World/WorldMapGenerator.cs:174-191` — cách sinh một cột: dưới `bedrockLayers` là đá đáy; mặt là cỏ; `dirtDepth` ô đất dưới mặt; sâu hơn là đá; trên mặt là rỗng.
- `Client/TerrainChunkView.cs`:
  - `Build()` (`:117`): sinh world (`:131-149`);
  - dấu mốc (`:150-158`): `Landmarks = LandmarkPlacer.Place(...)` ở `:154`, `BuildLandmarkMarkers()` ở `:156`;
  - tạo `Editor` (`:160`); chế độ nạp dần lấy tâm từ camera (`StreamCenter()`, `:177`).
- `Client/SimView.cs`:
  - `Awake` (`:61-66`), `Update` (`:74-105`); vòng nhịp ở `:91-100`, `Sim.Tick()` ở `:96`;
  - `Setup` (`:107-154`): nhánh `view.Maps == null` ở `:110`;
  - `OnGUI` (`:259-284`): dòng HUD đầu ở `:270`, vòng `foreach (var p in Sim.State.Plots)` ở `:275`.
- `Client/Editor/BenchBuild.cs` — mẫu cho `SimBenchBuild`: `EnsureScene` (`:23-52`), `BuildAndroid` (`:54-85`).
- `Tests/World/SimDemoMapsTests.cs:17-26` — cách tạo world 3 map + dấu mốc trong test. `Tests/World/SimDemoTests.cs:41-111` — cách đếm chung ô (bỏ qua `Phasing`), rảnh trên ô ruộng, rảnh ngoài bán kính.
- `Tests/Client/SimViewMapsPlayTests.cs:17-31` — cách dựng SimView trong PlayMode.

## Quyết định đã chốt (không được đổi)
**User (2026-10-21, task contract):**
1. 60 sinh vật, mỗi loài L1, L2, L3 có 20 con; con nào cũng có nhà và có việc. Luật 6a và 6b vẫn chạy như lúc chơi.
2. Đạt khi **p95 ≤ 5 ms/nhịp**. Đo bằng `Stopwatch` quanh `Sim.Tick()`, chạy **240 giây** (yêu cầu ít nhất 180 giây). Không đạt thì chỉ ghi số liệu, không tối ưu.
3. **Đồng bằng ở giữa Map 1** để dễ quan sát.

**Planner chốt:**

4. **Đồng bằng chỉ có trong scene đo** (`SimView.stressBench = true`): code làm phẳng world ngay sau khi sinh, trước khi dựng mesh. Bộ sinh world, `world_maps_m1.json` và mã băm world của game **không đổi**.
5. Đồng bằng:
   - cạnh 48;
   - tâm = tâm các cột thuộc Map 1, tính như `LandmarkPlacer` nhưng làm tròn xuống (`(int)(sumX / n)`), kẹp trong world;
   - độ cao mặt `Ground` = trung bình mặt đất cũ của 48 × 48 cột (chia nguyên), kẹp trong `[waterLevel + 2, sizeY − 12]`;
   - mỗi cột: cỏ ở `Ground`, đất từ `Ground − dirtDepth` tới `Ground − 1`, đá từ `bedrockLayers` tới dưới đó, rỗng ở trên; ô dưới `bedrockLayers` giữ nguyên. Ghi bằng `Cell.Make(...)`, không bật cờ đã sửa.
   - Mép đồng bằng có thể thành vách. Chấp nhận.
6. **Dấu mốc nằm trong đồng bằng bị bỏ** (chỉ trong scene đo), nên không chồng lên bố trí.
7. Vùng mô phỏng 64 × 64 (`cfg.regionSize`): tâm là tâm đồng bằng, kẹp trong world như `FindSite`.
8. Bố trí, toạ độ tương đối (ox, oz) tính từ góc đồng bằng `(X0, Z0)`, `y = Ground + 1`:

   | Thứ | Số | Vị trí |
   |---|---|---|
   | Nhà 2 × 2 × 2 (id `h` = 0..29) | 30 | ox = 4 + 4·(h % 10), oz = 4 + 6·(h / 10) |
   | Sinh vật (id `i` = 0..59) | 60 | nhà `h = i / 2`; ô (ox_nhà + i % 2, oz_nhà − 1); loài `SimDemo.Species[i % 3]`; `AssignHome(i, h)` |
   | Kho (id `s` = 0..4) | 5 | ox = 6 + 8·s, oz = 23 |
   | Lệnh trồng cây (`o` = 0..14) | 15 × 4 ô | `j = o % 5`, `r = o / 5`; góc ox = 4 + 8·j, oz = 28 + 6·r; 4 ô theo thứ tự (0,0), (1,0), (0,1), (1,1); ô ruộng ở `y = Ground`; kho `j` |

   Tạo theo thứ tự: nhà → sinh vật (mỗi con `AddCreature` rồi `AssignHome`) → kho → lệnh.
9. **Không có phím F3.** Trên PC, mở scene `SimBench` rồi Play là có bố trí đo. Trong APK, bố trí đo tự chạy khi mở app.
10. **Đếm tìm đường:** `GridPathfinder.AStarCount` (+1 mỗi lần gọi `Search`) và `BfsCount` (+1 mỗi lần gọi `FindPathToNearest` / `FloodRegion`). Hai bộ đếm này không vào `SimState`, không vào `SimHash`.
11. **Đo theo pha:** khi `Simulation.ProfilePhases = true`, `Tick()` cộng thời gian (`System.Diagnostics.Stopwatch.GetTimestamp()`) của từng pha vào `PhaseTicks[0..8]`. Thứ tự tên pha: `Terrain, Home, Work, Crop, Move, Assign, Stuck, Idle, Report`. Pha `Terrain` / `Home` không chạy ở nhịp nào thì phần đo của nhịp đó gần 0. Bật hay tắt đo không được làm đổi mã băm.
12. **Dòng log**, chép nguyên văn định dạng (số theo `CultureInfo` mặc định, như các log khác):
    - Mỗi 10 giây:
      `SIMBENCH t={giây từ lúc bắt đầu:F0}s ticks={số nhịp trong cửa sổ} avgMs={:F3} p95Ms={:F3} maxMs={:F3} astarPerSec={:F1} bfsPerSec={:F1} fps={:F1} maxTicksFrame={số nhịp nhiều nhất trong một khung của cửa sổ} creatures={số sinh vật}`
    - Một lần, khi đủ `benchSeconds` (240):
      `SIMBENCH TONG device={SystemInfo.deviceModel} cpu={SystemInfo.processorType} x{SystemInfo.processorCount} seconds={:F0} ticks={tổng nhịp} avgMs={:F3} p95Ms={:F3} p99Ms={:F3} maxMs={:F3} astarPerSec={:F1} bfsPerSec={:F1} fps={:F1} maxTicksFrame={} pass5ms={p95 <= 5.0} | phaseUs Terrain={:F1} Home={:F1} Work={:F1} Crop={:F1} Move={:F1} Assign={:F1} Stuck={:F1} Idle={:F1} Report={:F1} | produce={tổng Produce các kho} seeds={tổng Seeds các kho}`
      `phaseUs` = `PhaseTicks[k] × 1e6 ÷ Stopwatch.Frequency ÷ tổng nhịp`.
    - Percentile: sắp xếp tăng dần, lấy phần tử thứ `max(0, ⌈q·n⌉ − 1)`.
13. **Camera trong scene đo** đặt tại `(X0 + 24, Ground + 34, Z0 − 6)`, xoay `Euler(50, 0, 0)`. Đặt **trong hook**, trước khi nạp chunk đầu tiên.
14. HUD khi đo: không in 60 dòng ô ruộng; thêm một dòng hiện dòng `SIMBENCH` mới nhất. Nhãn trên đầu sinh vật giữ nguyên.

## Phạm vi
- **Được tạo:**
  - `Core/Sim/SimStress.cs`
  - `Client/SimBenchMeter.cs`
  - `Client/Editor/SimBenchBuild.cs`
  - `Client/Scenes/SimBench.unity`, do `EnsureScene` tạo
  - `Tests/World/SimStressTests.cs`, `Tests/Client/SimBenchPlayTests.cs`
  - các `.meta` do Unity sinh
- **Được sửa:** `Core/Sim/GridPathfinder.cs`, `Core/Sim/Simulation.cs`, `Client/TerrainChunkView.cs`, `Client/SimView.cs`. Chỉ sửa những chỗ ghi trong Các bước.
- **Không được sửa:**
  - `design/`, task contract;
  - `SimHash.cs`, `SimDemo.cs`, `BenchBuild.cs`, `Bench.unity`, `SimView.unity`;
  - bộ sinh world, file JSON cấu hình;
  - mọi test cũ;
  - mọi file khác.

## Các bước
Đường dẫn tính từ `client/Assets/Game/`.

1. **`Core/Sim/GridPathfinder.cs`:** thêm
   ```csharp
   /// <summary>Số lần chạy A* / BFS từ lúc tạo (đo sim bước 7, brief 0028) — không vào trạng thái mô phỏng.</summary>
   public long AStarCount { get; private set; }
   public long BfsCount { get; private set; }
   ```
   - Dòng đầu thân `Search` (`:269`): `AStarCount++;`.
   - Dòng đầu thân `FindPathToNearest` (`:172`) và `FloodRegion` (`:222`): `BfsCount++;`.

   Xong khi: biên dịch được.

2. **`Core/Sim/Simulation.cs`:**
   - a. Sau `PathfinderCellCount` (`:42`):
     ```csharp
     public long AStarCount => pathfinder.AStarCount;
     public long BfsCount => pathfinder.BfsCount;

     /// <summary>Đo thời gian từng pha của Tick (brief 0028); không vào mã băm.</summary>
     public bool ProfilePhases;
     public static readonly string[] PhaseNames = { "Terrain", "Home", "Work", "Crop", "Move", "Assign", "Stuck", "Idle", "Report" };
     public readonly long[] PhaseTicks = new long[9];
     ```
   - b. Viết lại `Tick()`. Giữ nguyên thứ tự và điều kiện các pha:
     ```csharp
     public void Tick()
     {
         State.Tick++;
         long t = ProfilePhases ? System.Diagnostics.Stopwatch.GetTimestamp() : 0;
         if (terrainDirty)
         {
             terrainDirty = false;
             TerrainPhase();
             homeDirty = true;
         }
         t = Lap(0, t);
         if (homeDirty)
         {
             homeDirty = false;
             HomePhase();
         }
         t = Lap(1, t);
         WorkPhase();
         t = Lap(2, t);
         CropPhase();
         t = Lap(3, t);
         MovePhase();
         t = Lap(4, t);
         AssignPhase();
         t = Lap(5, t);
         StuckPhase();
         t = Lap(6, t);
         IdlePhase();
         t = Lap(7, t);
         ReportPhase();
         Lap(8, t);
     }

     long Lap(int phase, long start)
     {
         if (!ProfilePhases) return 0;
         long now = System.Diagnostics.Stopwatch.GetTimestamp();
         PhaseTicks[phase] += now - start;
         return now;
     }
     ```
   Xong khi: EditMode cả bộ cũ vẫn xanh (mốc thời gian, mã băm không đổi).

3. **`Core/Sim/SimStress.cs`** (mới, `namespace Game.Core.Sim`, chỉ `using System; using System.Collections.Generic;`):
   ```csharp
   /// <summary>Đồng bằng cho bố trí đo (sim bước 7, brief 0028): góc (X0, Z0), cạnh Size, mặt cỏ ở y = Ground; Bounds = vùng mô phỏng.</summary>
   public readonly struct StressPlain
   {
       public readonly int X0, Z0, Size, Ground;
       public readonly SimBounds Bounds;
       public StressPlain(int x0, int z0, int size, int ground, SimBounds bounds) { X0 = x0; Z0 = z0; Size = size; Ground = ground; Bounds = bounds; }
       public bool Contains(int x, int z) => x >= X0 && x < X0 + Size && z >= Z0 && z < Z0 + Size;
       public override string ToString() => $"đồng bằng ({X0}, {Z0}) {Size} × {Size}, mặt y {Ground}, vùng {Bounds}";
   }

   /// <summary>Bố trí đo sim bước 7 (brief 0028): đồng bằng 48 × 48 giữa một map, 30 nhà, 60 sinh vật (20 mỗi loài), 5 kho, 15 lệnh × 4 ô.</summary>
   public static class SimStress
   {
       public const int PlainSize = 48, HouseCount = 30, CreatureCount = 60, StorageCount = 5, OrderCount = 15;

       public static StressPlain FlattenPlain(WorldData world, MapTable maps, int map, int bedrockLayers, int dirtDepth, int waterLevel, int regionSize) { ... }
       public static List<Landmark> WithoutPlain(IReadOnlyList<Landmark> landmarks, StressPlain plain) { ... }
       public static void Build(Simulation sim, StressPlain plain) { ... }
   }
   ```
   - `FlattenPlain` theo quyết định 5 và 7:
     - `regionSize` ngoài `[PlainSize, min(sizeX, sizeZ)]` → `ArgumentException`. Map không có cột nào → `InvalidOperationException`.
     - `X0 = min(max(cx − 24, 0), sizeX − 48)`; `Z0` tương tự.
     - Trung bình mặt đất: dùng `MinimapImage.ColumnInfo(world, x, z, out int surf, out _)`; `surf < 0` thì tính là `waterLevel`.
     - Vùng: `cxp = X0 + 24`, `x0r = min(max(cxp − regionSize / 2, 0), sizeX − regionSize)`, `z0r` tương tự, `new SimBounds(x0r, z0r, regionSize, regionSize)`.
   - `WithoutPlain`: giữ các dấu mốc có `!plain.Contains(l.X, l.Z)`, theo thứ tự cũ.
   - `Build` theo quyết định 8. Gọi khi `sim.State.Creatures.Count != 0` → `InvalidOperationException`.

   Xong khi: biên dịch được; test bước 8 đạt.

4. **`Client/TerrainChunkView.cs`:**
   - Thêm trường:
     ```csharp
     /// <summary>Gọi sau khi sinh world + đặt dấu mốc, trước khi dựng khối dấu mốc, editor và mesh (brief 0028); trả danh sách dấu mốc dùng tiếp.</summary>
     [System.NonSerialized] public System.Func<WorldData, MapTable, WorldMapParams, IReadOnlyList<Landmark>, IReadOnlyList<Landmark>> afterGenerate;
     ```
   - Trong `Build()`, ngay trước `BuildLandmarkMarkers();` (`:156`), thêm:
     ```csharp
     if (afterGenerate != null)
         Landmarks = afterGenerate(World, Maps, mapParams, Landmarks);
     ```

   Xong khi: SimView / TerrainView chạy như cũ (hook null).

5. **`Client/SimBenchMeter.cs`** (mới, `namespace Game.Client`, lớp thường, không phải MonoBehaviour):
   ```csharp
   public sealed class SimBenchMeter
   {
       public const float WindowSeconds = 10f;
       public SimBenchMeter(float totalSeconds)
       public bool Done { get; }
       public string LastLine { get; }      // dòng SIMBENCH gần nhất (cửa sổ hoặc TONG)
       public string Summary { get; }       // dòng TONG; null tới khi Done
       public void AddTick(long stopwatchTicks)   // Done thì bỏ qua; đổi sang ms = ticks × 1000.0 / Stopwatch.Frequency, thêm vào cửa sổ và danh sách tổng
       public void AddFrame(float now, float dt, int ticksThisFrame, Simulation sim)
       public static double Percentile(List<double> sorted, double q)   // quyết định 12
   }
   ```
   - `AddFrame`:
     - Lần gọi đầu tiên: ghi `start = windowStart = now` và số `AStarCount` / `BfsCount` lúc đó. Khung này **có** tính (khung, `dt`, `ticksThisFrame`).
     - Mỗi lần gọi: cộng số khung, cộng `dt`, cập nhật `maxTicksFrame` của cửa sổ và của tổng.
     - `now − windowStart ≥ 10`: tạo dòng cửa sổ (quyết định 12). `astarPerSec` = chênh `AStarCount` ÷ `(now − windowStart)`. `fps` = số khung ÷ tổng `dt` của cửa sổ. Cửa sổ trống thì các số ms = 0. Gán `LastLine`, gọi `Debug.Log`, đặt lại cửa sổ (`windowStart = now`).
     - `now − start ≥ totalSeconds`: tạo dòng TONG (quyết định 12, `phaseUs` lấy từ `sim.PhaseTicks`). Gán `Summary` và `LastLine`, gọi `Debug.Log`, đặt `Done = true`.
     - Sau `Done` thì không làm gì nữa.

6. **`Client/SimView.cs`:**
   - Thêm `using Game.Core;` nếu chưa có.
   - Trường:
     ```csharp
     /// <summary>Scene đo sim bước 7 (brief 0028): đồng bằng giữa Map 1 + 60 sinh vật, log SIMBENCH.</summary>
     public bool stressBench;
     public float benchSeconds = 240f;
     public StressPlain? Plain { get; private set; }
     public SimBenchMeter Meter { get; private set; }
     ```
   - `Awake` (`:61-66`): thêm cuối `if (stressBench) view.afterGenerate = OnAfterGenerate;`.
   - Thêm:
     ```csharp
     public static Vector3 BenchCameraPos(StressPlain p) => new Vector3(p.X0 + p.Size * 0.5f, p.Ground + 34f, p.Z0 - 6f);

     IReadOnlyList<Landmark> OnAfterGenerate(WorldData world, MapTable maps, WorldMapParams p, IReadOnlyList<Landmark> lms)
     {
         var plain = SimStress.FlattenPlain(world, maps, 0, p.bedrockLayers, p.dirtDepth, p.waterLevel, ConfigLoader.LoadSimConfig().regionSize);
         Plain = plain;
         var cam = Camera.main;
         if (cam != null)
             cam.transform.SetPositionAndRotation(BenchCameraPos(plain), Quaternion.Euler(50f, 0f, 0f));
         var kept = SimStress.WithoutPlain(lms, plain);
         Debug.Log($"SIMBENCH {plain}, bỏ {lms.Count - kept.Count} dấu mốc trong đồng bằng");
         return kept;
     }
     ```
   - `Setup` (`:107`): nhánh `if (view.Maps == null)` ở `:110` thành `else if`. Đứng trước nó thêm:
     ```csharp
     if (stressBench)
     {
         if (!Plain.HasValue)
         {
             Debug.LogError("SIMBENCH cần TerrainChunkView source = MapsV3");
             enabled = false;
             return;
         }
         var plain = Plain.Value;
         Sim = new Simulation(cfg, view.World, plain.Bounds);
         Sim.AttachEditor(view.Editor);
         SimStress.Build(Sim, plain);
         Sim.ProfilePhases = true;
         Meter = new SimBenchMeter(benchSeconds);
         Debug.Log($"SIMBENCH bố trí: {Sim.State.Creatures.Count} sinh vật, {Sim.State.Houses.Count} nhà, {Sim.State.Storages.Count} kho, {Sim.State.Plots.Count} ô ruộng, mảng tìm đường {Sim.PathfinderCellCount} ô");
     }
     ```
   - `Update`:
     - `Sim.Tick();` (`:96`) thay bằng:
       ```csharp
       long t0 = System.Diagnostics.Stopwatch.GetTimestamp();
       Sim.Tick();
       Meter?.AddTick(System.Diagnostics.Stopwatch.GetTimestamp() - t0);
       ```
     - Ngay sau vòng `for`: `Meter?.AddFrame(Time.unscaledTime, Time.unscaledDeltaTime, n, Sim);`
   - `OnGUI`:
     - vòng `foreach (var p in Sim.State.Plots)` (`:275`) chỉ chạy khi `Meter == null`;
     - khi `Meter != null && Meter.LastLine != null`, in thêm một dòng `Meter.LastLine` ở `new Rect(10, 100 + 24 * line++, 1400, 24)`.
   - Các nhánh cũ giữ nguyên.

   Xong khi: `SimViewPlayTests`, `SimViewMapsPlayTests` vẫn xanh.

7. **`Client/Editor/SimBenchBuild.cs`** (mới, chép cấu trúc `BenchBuild.cs`):
   - `SourceScene = "Assets/Game/Client/Scenes/SimView.unity"`, `ScenePath = "Assets/Game/Client/Scenes/SimBench.unity"`, `OutputPath = "Builds/simbench.apk"`, `TempAppId = "com.viventa.simbench"`.
   - `[MenuItem("Game/Tạo scene SimBench")] EnsureScene()`:
     - chép scene như `BenchBuild.EnsureScene` (`:26-35`);
     - mọi `SimView` → `stressBench = true`;
     - mọi `TerrainEditInput` → `enabled = false`;
     - **giữ** `DebugFlyCamera` để user bay quan sát trên PC;
     - lưu, mở lại scene cũ như `:49-51`.
   - `[MenuItem("Game/Build APK SimBench")] BuildAndroid()`: như `BenchBuild.BuildAndroid` (`:54-85`), log `"SimBench build: {result}, ..."`.
   - Chạy menu "Game/Tạo scene SimBench" một lần để tạo `SimBench.unity` (+ `.meta`).

   Xong khi: có `SimBench.unity`, mở ra thấy `SimView.stressBench` = true.

8. **`Tests/World/SimStressTests.cs`** (EditMode, `namespace Game.World.Tests`):
   - Hàm dựng chung:
     - `p = new WorldMapParams()`; `world = WorldMapGenerator.Generate(p, out maps)`; `lms = LandmarkPlacer.Place(world, maps, p)`;
     - `cfg` đọc `sim_m1.json` như `SimDemoMapsTests`;
     - `plain = SimStress.FlattenPlain(world, maps, 0, p.bedrockLayers, p.dirtDepth, p.waterLevel, cfg.regionSize)`;
     - `sim = new Simulation(cfg, world, plain.Bounds)`; `SimStress.Build(sim, plain)`.

   | Test | Kiểm |
   |---|---|
   | `PlainIsFlatDryGrass` | Mọi cột trong đồng bằng: `ColumnInfo` cho `surf == Ground`, `depth == 0`; `GetMaterial(x, Ground, z) == Grass`; `Ground > waterLevel`; 4 góc đồng bằng nằm trong `plain.Bounds`; cột tâm thuộc map 0. In `plain.ToString()` |
   | `BuildPlaces60CreaturesWithHomes` | 60 sinh vật; mỗi loài 20; mọi `HomeId >= 0`; 30 nhà, mỗi nhà `Residents.Count == 2`; 5 kho; 60 ô ruộng; mọi ô ruộng và kho có `Bounds.Contains` |
   | `WithoutPlainDropsOnlyInside` | Kết quả không có dấu mốc nào trong đồng bằng; số giữ + số trong đồng bằng = `lms.Count` |
   | `Runs4000Ticks` | 4000 nhịp, mỗi nhịp `SimDemo.KeepFarming(sim)`, `Tick()`, `Events.Clear()`. Kiểm như `SimDemoTests:41-111`: 0 lần chung ô (bỏ qua `Phasing`); 0 lần rảnh trên ô ruộng; 0 lần rảnh ngoài `homeRadius`. Tổng `Produce` các kho > 0; `sim.AStarCount > 0`. In thời gian chạy (ms), `AStarCount`, `BfsCount` |
   | `ProfilingKeepsHash` | Hai bố trí mới: A có `ProfilePhases = true`, B để false. Chạy 2000 nhịp như trên; mỗi 100 nhịp `SimHash.Compute(A.State) == SimHash.Compute(B.State)`. Cuối: `A.PhaseTicks[2] > 0`; mọi `B.PhaseTicks[k] == 0` |

9. **`Tests/Client/SimBenchPlayTests.cs`** (PlayMode, `namespace Game.Client.Tests`). SetUp như `SimViewMapsPlayTests:17-31`, có hai khác biệt:
   - **`go.SetActive(false)` trước khi AddComponent.** Gán `sv.stressBench = true` rồi mới `go.SetActive(true)`, vì `Awake` chạy ngay khi AddComponent lên object đang bật.
   - camera không cần đặt vị trí trước.

   | Test | Kiểm |
   |---|---|
   | `SetsUpStressFarmOnPlain` | `sv.Plain.HasValue`; `sv.Sim.State.Creatures.Count == 60`; `sv.Meter != null`; camera ≈ `SimView.BenchCameraPos(plain)` (±0.01); không dấu mốc nào trong `view.Landmarks` nằm trong đồng bằng; `sv.Sim.ProfilePhases` |
   | `MeterWindowAndSummary` | `m = new SimBenchMeter(20f)`; `AddTick` 100 lần (giá trị tuỳ ý > 0); `AddFrame(0f, 0.016f, 5, sv.Sim)`; `AddFrame(10f, 0.016f, 3, sv.Sim)` → `LastLine` bắt đầu bằng `"SIMBENCH t=10s ticks=100"`, chứa `"maxTicksFrame=5"`; `AddFrame(20f, 0.016f, 1, sv.Sim)` → `Done`, `Summary` bắt đầu bằng `"SIMBENCH TONG"`, chứa `"pass5ms="` và `"phaseUs Terrain="` |
   | `PercentileMatchesRule` | Danh sách 1..100 đã sắp xếp: p95 = 95, p99 = 99, q 0.5 = 50; danh sách `[5]`: mọi q → 5 |

10. **Điện thoại.**
    - `ADB` = `/Applications/Unity/Hub/Editor/6000.4.8f1/PlaybackEngines/AndroidPlayer/SDK/platform-tools/adb`.
    - Máy: serial USB `PFQCRWVWR46HU4GQ`. `ADB devices` không thấy thì `ADB connect 192.168.1.6:5555` rồi dùng `-s 192.168.1.6:5555`.
    - Tự cài, tự đọc log, không hỏi user.

    1. MCP `execute_menu_item` "Game/Build APK SimBench" → console có `SimBench build: Succeeded`.
    2. Cài và chạy:
       - `ADB -s <máy> install -r client/Builds/simbench.apk` → `Success`;
       - `ADB -s <máy> logcat -c`;
       - `ADB -s <máy> shell monkey -p com.viventa.simbench -c android.intent.category.LAUNCHER 1`.
    3. Khoảng 30 giây sau, kiểm khoá màn hình: `ADB -s <máy> shell dumpsys window | grep -E "mDreamingLockscreen|KeyguardShowing"`.
       - Đang khoá: ghi "chờ user mở khoá".
       - Không khoá: chụp `ADB -s <máy> exec-out screencap -p > client/Builds/simbench-0028.png`, mở bằng Read rồi ghi: thấy đồng bằng phẳng, nhiều hộp màu (sinh vật), hàng nhà nâu, dòng HUD `SIMBENCH`.
    4. Chờ đủ **260 giây** kể từ lúc mở app (chạy nền hoặc Monitor). Sau đó chạy `ADB -s <máy> logcat -d -s Unity | grep -E "SIMBENCH|Exception"`. Chép vào Kết quả:
       - dòng `SIMBENCH đồng bằng`, dòng `bố trí` (phải có `60 sinh vật`);
       - 24 dòng cửa sổ;
       - dòng `TONG` (nguyên văn);
       - mọi dòng `Exception` (phải không có).

       Ghi thêm nhiệt độ: `ADB -s <máy> shell dumpsys thermalservice | grep -m1 "Thermal Status"`. Sau 400 giây vẫn không có `TONG` thì ghi "chờ user mở khoá" và làm tiếp.
    5. Dọn:
       - `git checkout -- client/ProjectSettings/UnityConnectSettings.asset` (nếu đổi);
       - `git status` không còn file ngoài Phạm vi.

11. **Chạy toàn bộ + commit:**
    - EditMode cả bộ, PlayMode cả bộ (lệnh ở dưới). Sau PlayMode: `git checkout -- client/ProjectSettings/EditorSettings.asset`.
    - Commit code + test + scene (một commit, message `brief 0028: đồng bằng + 60 sinh vật + đo SIMBENCH`).
    - Commit brief (Kết quả) riêng.
    - Không push.

## Tiêu chí xong
- EditMode cả bộ xanh, có 5 test mới trong `SimStressTests`. PlayMode cả bộ xanh, có 3 test mới trong `SimBenchPlayTests`.
- `Runs4000Ticks`: 0 chung ô, 0 rảnh trên ô ruộng, 0 rảnh ngoài bán kính, có nông sản.
- `ProfilingKeepsHash` đạt: đo pha không đổi mã băm.
- Logcat điện thoại có dòng `SIMBENCH TONG` với `ticks ≥ 1800`. Kết quả ghi rõ `pass5ms=True` hoặc `False`; không đạt **không** phải lỗi của brief.
- Không sửa file ngoài Phạm vi.

## Test sau khi xong

### Phần cần test
| Phần | Cách test | Kết quả mong đợi |
|---|---|---|
| Bộ đếm tìm đường (bước 1, 2a) | `Runs4000Ticks` | `AStarCount > 0`; mã băm test cũ không đổi |
| Đo pha trong `Tick` (bước 2b) | `ProfilingKeepsHash` + EditMode cả bộ | Hash A = B mỗi 100 nhịp; mốc thời gian `FarmTests`, `PlotRulesTests`, `SimDemoTests` không đổi |
| Làm phẳng đồng bằng (bước 3) | `PlainIsFlatDryGrass` | Mặt cỏ phẳng, khô, trên mực nước, nằm trong vùng mô phỏng |
| Bố trí 60 con (bước 3) | `BuildPlaces60CreaturesWithHomes` | 60 / 20-20-20 / 30 nhà / 5 kho / 60 ô |
| Bỏ dấu mốc trong đồng bằng (bước 3) | `WithoutPlainDropsOnlyInside` | Không còn dấu mốc trong đồng bằng |
| Luật 6a + 6b với 60 con | `Runs4000Ticks` | 0 chung ô, 0 rảnh trên ô ruộng, 0 rảnh ngoài bán kính, có nông sản |
| Hook sau khi sinh (bước 4) + SimView đo (bước 6) | `SetsUpStressFarmOnPlain` | Như bảng bước 9 |
| SimView / TerrainView cũ không đổi (bước 4, 6) | PlayMode cả bộ (`SimViewPlayTests`, `SimViewMapsPlayTests`, `TerrainChunkView*`, `Minimap*`, `LandmarkView*`) | Xanh |
| Bộ đo (bước 5) | `MeterWindowAndSummary`, `PercentileMatchesRule` | Như bảng bước 9 |
| Scene + build (bước 7) | menu tạo scene; menu build | `SimBench.unity` có `stressBench` = true; `SimBench build: Succeeded` |
| Điện thoại (bước 10) | APK + logcat | Dòng `TONG`, `ticks ≥ 1800`, có `pass5ms`, 0 `Exception` |

### Lệnh executor chạy
1. MCP `refresh_unity` → `read_console` không có lỗi biên dịch.
2. MCP `run_tests` EditMode (cả bộ) → ghi dòng tổng. Ghi riêng 5 test `SimStressTests`, kèm thời gian và số `AStarCount` mà `Runs4000Ticks` in ra.
3. MCP `run_tests` PlayMode (cả bộ) → ghi dòng tổng. Sau đó `git checkout -- client/ProjectSettings/EditorSettings.asset`.
4. Bước 10 (điện thoại), rồi dọn.
5. `git status --short` → chỉ còn file trong Phạm vi trước khi commit.

Unity Editor dùng chung qua MCP (0010). Editor đang tắt thì thay bằng `python3 tools/unity_test.py EditMode|PlayMode` và ghi rằng không build APK được.

### User tự test
Trên Mac:
1. Mở scene `Assets/Game/Client/Scenes/SimBench.unity`, nhấn Play.
2. Nhìn thấy:
   - đồng bằng cỏ phẳng 48 × 48;
   - 3 hàng nhà nâu ở phía gần camera, hàng 5 kho xám ở giữa, 3 hàng ruộng 2 × 2 ở phía xa;
   - 60 sinh vật màu cam / xanh / tím đi làm, không con nào đứng chồng lên con khác.
3. Bay quanh bằng WASD, Q/E xuống/lên, giữ chuột giữa để xoay (DebugFlyCamera) để xem gần.
4. Sau khoảng 1 phút: các ô ruộng đổi màu sau khi xới, có mầm xanh mọc, hộp vàng được chở về kho.
5. Mỗi 10 giây, dòng `SIMBENCH ...` trên HUD đổi số.

Trên điện thoại: Planner đọc logcat và ảnh, user không cần thao tác. Nếu `pass5ms=False`, Planner đề xuất task tối ưu riêng.

## Dừng và chuyển lại Opus khi
- Brief mơ hồ hoặc mâu thuẫn với code thật, ví dụ `AddHouse` / `AddCreature` / `AddFarmOrder` ném lỗi ở toạ độ trong bảng quyết định 8.
- Một test cũ hỏng, hoặc `Runs4000Ticks` có chung ô / rảnh trên ô ruộng. Không tự sửa luật mô phỏng.
- Cần một quyết định thuộc AGENTS.md §3.4 hoặc ngoài "Quyết định đã chốt".
- Cùng một lỗi sửa 2 lần vẫn chưa qua.
- Phải sửa file ngoài "Phạm vi".

---
<!-- Từ đây trở xuống: EXECUTOR ghi. -->

## Kết quả
- Base commit: 9ac160c
- Chạy bằng: claude-sonnet-5-5 · medium (OK: brief 0028 cần Sonnet · medium)
- Bắt đầu: 2026-10-21 19:03
<!-- Base commit (trước khi làm) · Chạy bằng: <model id> · <effort> (dòng OK của brief_check.py) · commit của brief · file đã đổi · giờ bắt đầu/kết thúc · ghi chú lệch so với brief (nếu có, kèm lý do). -->

- Code commit: 64ecb06 (SimStress.cs, SimBenchMeter.cs, SimBenchBuild.cs, SimBench.unity, 2 file test, sửa GridPathfinder/Simulation/TerrainChunkView/SimView). Không lệch brief.
- APK đã build: `SimBench build: Succeeded, 848101811 byte` → client/Builds/simbench.apk (không commit).
- **Dừng ở bước 10:** điện thoại không hiện trong `adb devices` (USB, serial PFQCRWVWR46HU4GQ không thấy; `adb connect 192.168.1.6:5555` → Connection refused; đã kill/start adb server). Chưa cài APK, chưa có số đo. Cần user cắm lại cáp/bật USB debugging, rồi chạy lại từ bước 10.2.
- **Lần 2 (user bảo thử Wi‑Fi):** USB vẫn không thấy. `adb mdns services` chỉ thấy `192.168.1.7:6060` = model **ADT_3, đang chạy YouTube TV → là Android TV box, KHÔNG phải điện thoại**. Đã cài APK + chạy trên đó (ảnh: đồng bằng, nhà, 60 sinh vật, HUD SIMBENCH đúng). Số đo 81 giây đầu (thiết bị TV, chỉ để tham khảo): fps ≈ 6, ticks ≈ 101/10 s (sim chạy chậm hơn thời gian thực vì khung hình), avgMs ≈ 0,4–0,6, p95Ms ≈ 1,1–2,1, maxMs ≤ 3,4, 0 Exception, thermal 0. Sau đó TV chuyển sang app YouTube nên Unity bị tạm dừng, không có dòng TONG. Log: side-session-notes/0028-logcat-androidtv.txt. Chưa đo trên điện thoại thật.
- **Lần 3 — ĐO TRÊN ĐIỆN THOẠI THẬT (Xiaomi 24095PCADG, ADB Wi‑Fi 192.168.1.6:5555, màn hình sáng, app foreground):** đủ 240 s, 0 Exception, Thermal Status 2 sau khi chạy. Log đủ 24 dòng cửa sổ + dòng đầu: side-session-notes/…/0028-logcat-phone.txt. Ảnh chụp (client/Builds/simbench-0028.png, không commit): đồng bằng phẳng, hàng nhà nâu, 60 sinh vật màu xanh/tím/cam có nhãn, 5 dòng kho, HUD `SIMBENCH t=20s …` đúng, 32,5 FPS.
  - `SIMBENCH đồng bằng (99, 187) 48 × 48, mặt y 20, vùng x 91..154, z 179..242, bỏ 1 dấu mốc trong đồng bằng`
  - `SIMBENCH bố trí: 60 sinh vật, 30 nhà, 5 kho, 60 ô ruộng, mảng tìm đường 196608 ô`
  - Cửa sổ: avgMs 0,24–0,46, p95Ms 0,58–1,13, maxMs ≤ 1,86, fps ≈ 32,4 (cửa sổ đầu 22,2), maxTicksFrame 1–2, astarPerSec 3,8–8,9, bfsPerSec 2,7–5,1.
  - `SIMBENCH TONG device=Xiaomi 24095PCADG cpu=ARM64 FP ASIMD AES x8 seconds=240 ticks=2386 avgMs=0,394 p95Ms=0,956 p99Ms=1,347 maxMs=1,864 astarPerSec=6,8 bfsPerSec=3,6 fps=31,9 maxTicksFrame=2 pass5ms=True | phaseUs Terrain=0,7 Home=0,4 Work=13,0 Crop=3,2 Move=130,8 Assign=50,4 Stuck=2,3 Idle=188,5 Report=3,8 | produce=10 seeds=1`
  - **pass5ms=True** (p95 0,956 ms ≪ 5 ms; ticks 2386 ≥ 1800). Pha tốn nhất: Idle 188,5 µs, Move 130,8 µs, Assign 50,4 µs.
  - Lưu ý cho Planner: produce=10 trong 240 s trên 15 lệnh × 4 ô — ít nông sản (cây lớn chậm hơn 240 s); FPS ~32 (không phải 60) do dựng hình, không do sim.
- Giờ kết thúc: 2026-10-21 19:39.

## Kết quả test
- EditMode cả bộ: 371/371 xanh (235 s). 5 test SimStressTests xanh. Runs4000Ticks: `4000 nhịp: 409 ms, AStarCount=3515, BfsCount=1290, produce=46, overlaps=0, idleOnPlot=0, idleOutside=0`. Đồng bằng: `(99, 187) 48 × 48, mặt y 20, vùng x 91..154, z 179..242`; dấu mốc 9, giữ 8, bỏ 1.
- PlayMode cả bộ: 79/79 xanh (có 3 test SimBenchPlayTests). Đã `git checkout` EditorSettings.asset.
- Scene: SimBench.unity có `stressBench: 1`. Build: Succeeded.
- Điện thoại: đạt — `SIMBENCH TONG … pass5ms=True`, ticks 2386, 0 Exception (xem Kết quả, lần 3).
<!-- Mỗi dòng ở "Phần cần test" → đạt / không đạt / chờ user + bằng chứng (tên test, dòng output).
Mỗi lệnh ở "Lệnh executor chạy": `<lệnh>` → <kết quả thật, dòng tổng kết>. -->

## Vướng mắc
<!-- Khi trạng thái = cần Opus: câu hỏi cụ thể + các phương án (kèm đề xuất). -->

---
<!-- PLANNER ghi khi review. -->

## Review
<!-- Kết luận: đã duyệt | cần sửa (thêm bước sửa vào "Các bước", trạng thái về chờ làm). Điểm đã đối chiếu. -->
