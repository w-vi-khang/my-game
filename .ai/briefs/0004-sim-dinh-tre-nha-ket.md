# Brief 0004 — Simulation bước 4: đình trệ + thông báo, nhà mẫu 2 × 2 × 2, sinh vật bị kẹt

> Trạng thái: đã duyệt
> Review: đầy đủ
> Model thực thi: Sonnet · high
> Cập nhật: 2026-10-14

<!-- Planner (Opus) viết, Executor (Sonnet) làm theo ở PHIÊN KHÁC — executor không có lịch sử chat, không được tự quyết.
Trạng thái: chờ làm → đang làm → chờ review → đã duyệt; hoặc → cần Opus (executor dừng, hỏi ở mục Vướng mắc).
Review: nhẹ (đọc mục Kết quả) | đầy đủ (đọc cả diff) | user xem (có phần user tự kiểm bằng mắt/chơi thử) — có thể ghép "đầy đủ + user xem".
File được commit. Tự kiểm trước khi giao (AGENTS.md §10.3): executor có phải hỏi gì không? còn chỗ nào bỏ ngỏ 2 phương án? -->

## Mục tiêu
Thêm vào mô phỏng:
- Lý do đình trệ **thiếu năng lực** (`NoAbility`).
- **Sự kiện thông báo** khi bắt đầu / hết đình trệ.
- **Nhà mẫu** 2 × 2 × 2: khối đặc, sức chứa 2. Sinh vật có nhà mới được giao việc.
- **Sinh vật bị kẹt**: rảnh + không có đường về nhà liên tục 300 nhịp → hiện lại ở ô kề nhà, kéo theo hàng nó đã làm rơi.

Task contract: `.ai/tasks/m1-sim-buoc-4-dinh-tre-nha-ket.md`. Quyết định: `design/decisions/0016-o-ruong-kho-nha-va-sinh-vat-ket.md`, `design/decisions/0017-dinh-tre-thong-bao-nha-mau-va-dem-ket.md`.

## Bối cảnh cần đọc
Code ở commit `af43ba8`.
- `client/Assets/Game/Core/Sim/Simulation.cs`:
  - Dựng và thêm đối tượng: `:14` field `goals`; `:15` `terrainDirty`; `:27` `AddCreature`; `:70` `AddStorage`; `:83` `IsOccupied`; `:95` `OnCellChanged`.
  - Vòng nhịp: `:97` `Tick()`; `:115` `TerrainPhase`; `:161` `Displace`.
  - Đường đi: `:188` `Repath`; `:206` `AdjacentGoals(Creature c, CellPos p)` (mẫu cho luật ô kề); `:222` `PlanTo`; `:239` `SetPath`; `:247` `BlockNoPath`.
  - Các pha: `:255` `WorkPhase`; `:434` `AssignPhase` (vòng mở lại `NoSeeds`/`StorageFull` ở đầu hàm; vòng duyệt stage kiểm `NoSeeds`, `StorageFull` trước khi lập `eligible`); `:516` `Assign(Creature c, WorkStage s, List<CellPos> path)`; `:538` `Unassign(int creatureId)` (khối `if (s.Carried) { s.Target = c.Cell; s.Carried = false; }`).
- `SimData.cs`: `:33` `public enum StallReason : byte { None = 0, NoPath = 1, NoSeeds = 2, StorageFull = 3 }`; `:43` `Creature` (`:56` `public int StageId = -1;`); `:84` `WorkStage` (`:99` `public StallReason Stall;`); `:144` `Storage`; `:163` `FarmPlot`; `:183` `SimState` (`:191` `Plots`).
- `SimConfig.cs`: `:70-74` `FarmConfig`; `:102` field `farm`; `:112` `MsToTicks(int ms) => ms * ticksPerSecond / 1000`; `:114` `GrowTicks`; `:132` `Validate()`, dòng cuối `:180-181` (kiểm `farm`).
- `SimHash.cs`: `:32` hết phần creature; `:57` dòng cuối phần stage (`LoadSeeds`); `:98-99` hết phần plot; `:100` `return h;`.
- `GridPathfinder.cs` (không sửa): `:43` `SetBlocked(CellPos c, bool value)`; `:50` `IsBodyFree` (false nếu ngoài map, bị chặn, hoặc không `Empty`); `:54` `IsSupport` (chỉ xét vật liệu world: không `Empty`, không `Water`); `:61` `CanStand(CellPos a, int sx, int sz, int sy)` (ngoài map → false); `:102` `FindPathToAny(CellPos start, List<CellPos> goals, int sx, int sz, int sy, List<CellPos> result)` (goals rỗng → false; start trùng một đích → true, đường rỗng).
- `client/Assets/Game/Core/WorldEditor.cs`: `:69` `TryDig(int x, int y, int z)`; `:87` `TryPlace(int x, int y, int z, BlockMaterial material)` (trả `EditResult.Occupied` khi `IsOccupied` true).
- Test hiện có: `client/Assets/Game/Tests/Core/Sim/`, EditMode toàn project **131**.
  - `TestWorlds.Flat(groundTop)`: 16×16×16; y = 0 `Bedrock`; y = 1..groundTop `Dirt`; sinh vật đứng ở y = groundTop + 1.
  - Cách dựng `WorldEditor` trong test: xem `FarmTests.StorageBlocksPathAndPlacement`.
- **Quy ước thời gian** (đã kiểm bằng test bước 1–3):
  - Giao việc ở nhịp t, đường dài n ô, tốc độ v (phần nghìn ô/nhịp; L1, L2 = 150, L3 = 200) → tới nơi ở nhịp t + ⌈1000·n / v⌉. Làm bắt đầu từ nhịp sau; công đoạn B nhịp xong ở nhịp (tới nơi) + B.
  - Giao việc mà đã đứng đúng chỗ → `Working` ngay nhịp t, xong ở t + B.
  - Nhịp làm: xới 60, gieo 30, tưới 30, thu hoạch 40, vận chuyển 20.
  - "Nhịp n" = trạng thái **sau** lần `Tick()` thứ n.

## Quyết định đã chốt (không được đổi)
- (0016) Nhà mẫu 2 × 2 × 2, sức chứa 2, đặt bằng code. **Chỉ sinh vật có nhà mới được giao việc.** Kẹt 30 giây = 300 nhịp.
- (0017) **Thông báo** = danh sách `SimEvent` trong `State.Events`; mô phỏng chỉ thêm vào, nơi đọc tự `Clear()`. Không tính vào mã băm.
  - "Lý do hiện tại" của stage: `Status == Blocked ? Stall : None`. Chỉ phát khi lý do hiện tại khác `ReportedStall`:
    - khác `None` → `StallStarted` với lý do mới (đổi thẳng X → Y cũng chỉ một `StallStarted(Y)`);
    - về `None` → `StallCleared` với lý do cũ.
  - Chỉ so ở **cuối nhịp**. Vì vậy chặn → mở → chặn lại trong cùng nhịp không phát gì.
- (0017) **`NoAbility`**: không sinh vật **có nhà** nào có năng lực của công đoạn. Đang bận vẫn tính là có; sinh vật không nhà không tính. Kiểm sau `NoSeeds`/`StorageFull`, trước khi lập `eligible`. Mở lại ở đầu `AssignPhase` khi đã có sinh vật có nhà mang năng lực đó.
- (0017) **Nhà là khối đặc**: `Cell` = góc nhỏ nhất (x, z nhỏ nhất, y = tầng dưới); chiếm `sizeX × sizeZ × sizeY` ô.
  - Đặt được khi `CanStand(Cell, sizeX, sizeZ, sizeY)` (đủ ô đỡ, mọi ô trống, không bị chặn) **và** không ô nào `IsOccupied`.
  - Mọi ô của nhà `SetBlocked(true)`; `IsOccupied` trả true. Không đứng lên nhà được (ô world vẫn `Empty`).
- (0017) **Gắn nhà**: `AddHouse` trả id; `AssignHome(creatureId, houseId)`. `ArgumentException` khi: id sinh vật/nhà không có, sinh vật đã có nhà, nhà đã đủ sức chứa. Sinh vật mới thêm `HomeId = -1`.
- **Ô kề nhà** (cho sinh vật c, nhà neo H = (hx, hy, hz), kích thước SX × SZ). Duyệt các ô ngang theo thứ tự:
  1. +X: (hx + SX, hy, hz + dz), dz = 0..SZ−1
  2. −X: (hx − 1, hy, hz + dz), dz = 0..SZ−1
  3. +Z: (hx + dx, hy, hz + SZ), dx = 0..SX−1
  4. −Z: (hx + dx, hy, hz − 1), dx = 0..SX−1

  Mỗi ô thử dy theo thứ tự **0, +1, −1**, lấy ô **đầu tiên** `CanStand(ô, c.SizeX, c.SizeZ, c.SizeY)` (giống `AdjacentGoals`). Tối đa 2·SX + 2·SZ ô. **Ô hiện lại** = phần tử đầu tiên; danh sách rỗng → chưa hiện lại, nhịp sau thử lại.
- (0017) **Có đường về nhà** (`HomeReachable`) = `FindPathToAny(c.Cell, ô kề nhà, khung c)`. Chỉ tính lại theo sự kiện: cờ `homeDirty` bật khi địa hình đổi (`TerrainPhase` chạy), `AddStorage`, `AddHouse`, `AssignHome`. Tính lại cho mọi sinh vật có nhà ở đầu nhịp kế, ngay sau `TerrainPhase`.
- (0017) **Đếm kẹt** chỉ khi `HomeId >= 0 && State == Idle && !HomeReachable`:
  - chưa đếm → `StuckSince = Tick`;
  - đã đếm và `Tick − StuckSince >= StuckTicks` (300) → hiện lại;
  - mọi trường hợp khác → `StuckSince = -1`.

  Đang đi / đang làm thì không đếm; có việc lại thì đếm lại từ đầu.
- (0017) **Hiện lại**: đặt sinh vật ở ô hiện lại; xoá đường; `MoveProgress = 0`; `MoveTarget = ô`; `StuckSince = -1`; `HomeReachable = true`. Phát `CreatureRespawned`.
  - **Hàng đã rơi**: mọi stage có `DroppedBy == c.Id`, `Status` khác `Assigned` và `Done` → `Target = ô hiện lại`, `DroppedBy = -1`; nếu `Blocked` + `NoPath` → `Open`, `Stall = None`.
  - `DroppedBy` gán `= c.Id` trong `Unassign` khi hàng rơi (`Carried`); `= -1` trong `Assign`.
- Thứ tự `Tick()`: `Tick++` → (địa hình bẩn) địa hình, rồi bật `homeDirty` → (`homeDirty`) tính lại đường về nhà → làm → cây → đi → giao việc → **kẹt** → **báo sự kiện**.
- Test bước 1–3 **không đổi số nhịp nào**. Chỉ được: gọi `TestWorlds.HomeAll`; đổi 2 kỳ vọng `Open` → `NoAbility` (bước 6); thêm assert sự kiện (bước 7).

## Phạm vi
- Được tạo: `client/Assets/Game/Tests/Core/Sim/HomeTests.cs` (+ `.meta`).
- Được sửa:
  - Code: `client/Assets/Game/Core/Sim/{SimConfig,SimData,Simulation,SimHash}.cs`.
  - Test: `client/Assets/Game/Tests/Core/Sim/{TestWorlds,SimulationTests,SimulationTerrainTests,FarmTests,SimDeterminismTests,SimConfigTests}.cs`.
  - `design/data/sim_m1.json`.
  - Brief này (Kết quả, Kết quả kiểm chứng, Vướng mắc, Trạng thái).
- Không được sửa: mọi file khác, nhất là:
  - `GridPathfinder.cs`, `GridPathfinderTests.cs`;
  - `client/Assets/Game/Core/*.cs` ngoài `Sim/`;
  - asmdef;
  - `design/` (trừ `sim_m1.json`), `.ai/tasks/`.

## Các bước
1. **Config** (`SimConfig.cs`, `sim_m1.json`, `SimConfigTests.cs`):
   - Sau `FarmConfig` thêm:
     ```csharp
     [Serializable]
     public class HouseConfig
     {
         public int capacity = 2;
         public int sizeX = 2;
         public int sizeZ = 2;
         public int sizeY = 2;
     }

     [Serializable]
     public class CreatureConfig
     {
         public int stuckTimeMs = 30000;
     }
     ```
   - Trong `SimConfig`, sau `farm`: `public HouseConfig house = new HouseConfig();` và `public CreatureConfig creature = new CreatureConfig();`. Sau `GrowTicks`: `public int StuckTicks => MsToTicks(creature.stuckTimeMs);`.
   - `Validate()`, cuối hàm:
     ```csharp
     if (house == null || house.capacity < 1 || house.capacity > 16)
         throw new ArgumentException("house.capacity phải trong [1, 16].");
     if (house.sizeX < 1 || house.sizeX > 4 || house.sizeZ < 1 || house.sizeZ > 4 || house.sizeY < 1 || house.sizeY > 4)
         throw new ArgumentException("house: kích thước phải trong [1, 4] mỗi chiều.");
     if (creature == null || creature.stuckTimeMs <= 0 || creature.stuckTimeMs % msPerTick != 0)
         throw new ArgumentException("creature.stuckTimeMs phải > 0 và chia hết cho " + msPerTick + ".");
     ```
   - `sim_m1.json`: sau khối `"farm"` thêm `"house": { "capacity": 2, "sizeX": 2, "sizeZ": 2, "sizeY": 2 },` và `"creature": { "stuckTimeMs": 30000 }` (đúng dấu phẩy).
   - `SimConfigTests.ValidateRejectsBadValues`, cuối hàm:
     - `c = new SimConfig(); c.house.capacity = 0;` → `Throws<ArgumentException>`, thông điệp `"nhà sức chứa 0"`;
     - `c = new SimConfig(); c.creature.stuckTimeMs = 30050;` → `Throws`, thông điệp `"stuckTimeMs lẻ nhịp"`.
   - Xong khi: `ConfigMatchesCodeDefaults`, `ConfigParsesAndValidates` vẫn đạt.

2. **Dữ liệu** (`SimData.cs`):
   - `StallReason` thêm `NoAbility = 4`; comment đổi thành `/// <summary>Lý do đình trệ (0016, 0017).</summary>`.
   - Thêm:
     ```csharp
     public enum SimEventKind : byte { StallStarted = 1, StallCleared = 2, CreatureRespawned = 3 }

     /// <summary>Sự kiện thông báo (0017). Đình trệ: CreatureId = -1, Cell = Target của stage. Hiện lại: StageId = -1, Reason = None, Cell = ô hiện lại.</summary>
     public readonly struct SimEvent
     {
         public readonly long Tick;
         public readonly SimEventKind Kind;
         public readonly int StageId, CreatureId;
         public readonly CellPos Cell;
         public readonly StallReason Reason;

         public SimEvent(long tick, SimEventKind kind, int stageId, int creatureId, CellPos cell, StallReason reason)
         { Tick = tick; Kind = kind; StageId = stageId; CreatureId = creatureId; Cell = cell; Reason = reason; }
     }
     ```
   - `Creature`, sau `StageId`: `public int HomeId = -1; public bool HomeReachable; public long StuckSince = -1;`.
   - `WorkStage`, sau `Stall`: `public StallReason ReportedStall; public int DroppedBy = -1;`.
   - Thêm class (sau `FarmPlot`):
     ```csharp
     /// <summary>Nhà mẫu M1 — khối đặc (0017). Cell = góc nhỏ nhất.</summary>
     public sealed class House
     {
         public readonly int Id;
         public readonly CellPos Cell;
         public readonly int SizeX, SizeZ, SizeY, Capacity;
         public readonly List<int> Residents = new List<int>();

         public House(int id, CellPos cell, int sizeX, int sizeZ, int sizeY, int capacity) { ... }

         public bool Contains(int x, int y, int z) =>
             x >= Cell.X && x < Cell.X + SizeX && z >= Cell.Z && z < Cell.Z + SizeZ && y >= Cell.Y && y < Cell.Y + SizeY;
     }
     ```
   - `SimState`:
     - `public readonly List<House> Houses = new List<House>();`
     - `/// <summary>Sự kiện thông báo — mô phỏng chỉ thêm vào, nơi đọc tự Clear. Không tính vào mã băm.</summary> public readonly List<SimEvent> Events = new List<SimEvent>();`

3. **Simulation — nhà** (`Simulation.cs`):
   - Field: `bool homeDirty;` và `readonly List<CellPos> homePath = new List<CellPos>();`.
   - `AddStorage`: sau `SetBlocked` thêm `homeDirty = true;`.
   - `IsOccupied`: thêm vòng `State.Houses`: `Contains(x, y, z)` → true.
   - `public int AddHouse(CellPos cell)`:
     - `var hc = Config.house;`
     - `!pathfinder.CanStand(cell, hc.sizeX, hc.sizeZ, hc.sizeY)` → `ArgumentException("Nhà không đặt được tại " + cell)`;
     - duyệt mọi ô của khối: `IsOccupied` → `ArgumentException("Ô đã bị chiếm: " + ô)`;
     - rồi `SetBlocked(ô, true)` cho mọi ô; tạo `House(State.Houses.Count, cell, hc.sizeX, hc.sizeZ, hc.sizeY, hc.capacity)`, thêm vào `Houses`; `homeDirty = true`; trả id.
   - `public void AssignHome(int creatureId, int houseId)`, `ArgumentException` khi:
     - `creatureId` ngoài `[0, Creatures.Count)`;
     - `houseId` ngoài `[0, Houses.Count)`;
     - `HomeId >= 0` → `"Sinh vật đã có nhà: " + creatureId`;
     - `Residents.Count >= Capacity` → `"Nhà đã đủ: " + houseId`.

     Rồi `c.HomeId = houseId; h.Residents.Add(creatureId); homeDirty = true;`.
   - Private `void HomeGoals(Creature c, House h)`: điền `goals` theo luật "Ô kề nhà" (dùng lại mảng `DisplaceY` = {0, 1, −1}).
   - Private `void HomePhase()`: mỗi sinh vật `HomeId >= 0`, theo id: `HomeGoals(c, Houses[c.HomeId]); c.HomeReachable = pathfinder.FindPathToAny(c.Cell, goals, c.SizeX, c.SizeZ, c.SizeY, homePath);`.
   - Xong khi: biên dịch được.

4. **Simulation — giao việc, kẹt, sự kiện:**
   - `Tick()` thành:
     ```csharp
     State.Tick++;
     if (terrainDirty) { terrainDirty = false; TerrainPhase(); homeDirty = true; }
     if (homeDirty) { homeDirty = false; HomePhase(); }
     WorkPhase(); CropPhase(); MovePhase(); AssignPhase(); StuckPhase(); ReportPhase();
     ```
   - Private `bool HasHomedAbility(WorkAbility a)`: có sinh vật `HomeId >= 0 && HasAbility(a)`.
   - **AssignPhase**:
     - Vòng mở lại ở đầu hàm thêm điều kiện: `|| g.Stall == StallReason.NoAbility && HasHomedAbility(g.Ability)`.
     - Vòng duyệt stage, ngay sau kiểm `StorageFull`: `if (!HasHomedAbility(s.Ability)) { s.Status = StageStatus.Blocked; s.Stall = StallReason.NoAbility; continue; }`.
     - Lập `eligible`: bỏ qua sinh vật `HomeId < 0`.
   - **Assign**: thêm `s.DroppedBy = -1;`.
   - **Unassign**: trong khối `if (s.Carried)` thêm `s.DroppedBy = c.Id;`.
   - Private `void StuckPhase()`: theo id, áp luật "Đếm kẹt"; hiện lại bằng private `void Respawn(Creature c)` theo luật "Hiện lại".
   - Private `void ReportPhase()`: theo id stage, áp luật "Thông báo". `Cell = s.Target`; `CreatureId = -1`.
   - Thêm sự kiện: `State.Events.Add(new SimEvent(State.Tick, kind, stageId, creatureId, cell, reason))`.
   - Xong khi: biên dịch được.

5. **`SimHash.cs`:**
   - Phần creature, sau `Path`: `HomeId`, `HomeReachable ? 1 : 0`, `MixLong(StuckSince)`.
   - Phần stage, sau `LoadSeeds`: `(int)ReportedStall`, `DroppedBy`.
   - Trước `return h;`: `Houses.Count`, rồi mỗi nhà: `Id`, `Cell` X/Y/Z, `SizeX`, `SizeZ`, `SizeY`, `Capacity`, `Residents.Count`, từng phần tử `Residents`.
   - `Events` **không** vào mã băm.

6. **Gắn nhà cho test cũ** (`TestWorlds.cs` + các file test cũ):
   - `TestWorlds.cs` thêm `using Game.Core.Sim;` và:
     ```csharp
     /// <summary>Gắn nhà cho mọi sinh vật chưa có nhà (theo id, đủ sức chứa thì sang nhà kế). Neo nhà lần lượt (14, y, 14), (14, y, 11), (11, y, 14); y = ô Empty thấp nhất của cột neo.</summary>
     public static void HomeAll(Simulation sim)
     ```
     - Nhà tạo khi cần, bằng `sim.AddHouse`.
     - Quá 3 nhà → `InvalidOperationException`.
     - Nhà dùng tiếp: nhà vừa tạo cuối cùng trong helper (không dùng nhà test tự đặt).
   - Gọi `TestWorlds.HomeAll(sim)` trong **mọi** test có gọi `Tick()`, sau `AddCreature` cuối cùng và trước `Tick()` đầu tiên:
     - `SimulationTests`: mọi test có `Tick` (trong `TiesBrokenBySeed`: trong hàm `First`).
     - `SimulationTerrainTests`: mọi test có `Tick`.
     - `FarmTests`:
       - cuối `Layout` (sau `AddFarmOrder`);
       - `PrefersNeighborOverPlot`, `StandsOnPlotWhenNoNeighbor`: sau `AddFarmOrder`;
       - `StorageBlocksPathAndPlacement`: sau `AddCreature("L1", (4,2,5))`.
     - `SimDeterminismTests`: cuối `BuildFarm`, cuối `BuildCropFarm`.
   - Đổi kỳ vọng (do `NoAbility`, 0017):
     - `SimulationTests.OnlyMatchingAbility`: `Open` → `Blocked`, thêm `Assert.AreEqual(StallReason.NoAbility, …Stall)`.
     - `FarmTests.WithoutHaulerProduceStaysAtField`: `Open` → `Blocked`, thêm `Stall == NoAbility`. Các assert còn lại giữ nguyên.
   - Xong khi: 131 test cũ đạt với **đúng số nhịp cũ**.

7. **Assert sự kiện trong test cũ.** Mọi assert sự kiện kiểm đủ: `Kind`, `Tick`, `StageId`, `Cell`, `Reason`.
   - `SimulationTerrainTests.DigCutsPathThenPlaceRestores`:
     - sau `Tick` ngay sau đào (nhịp 11): `Events.Count == 1`; `[0]` = `StallStarted`, nhịp 11, stage `s`, ô (9,3,3), `NoPath`;
     - sau `Tick` ngay sau đặt lại (nhịp 12): `Count == 2`; `[1]` = `StallCleared`, nhịp 12, stage `s`, ô (9,3,3), `NoPath`.
   - `SimulationTerrainTests.NoPathAtAssignBlocks`: đổi `Build(…, out _, out _)` thành `out var editor, out _`.
     - nhịp 1: `Events.Count == 1`, `StallStarted`, nhịp 1, ô (9,3,3), `NoPath`.
     - Rồi `editor.TryDig(12, 2, 12) == EditResult.Ok`, `Tick()` (nhịp 2): stage vẫn `Blocked` + `NoPath`; `Events.Count == 1`.
   - `FarmTests`: thêm helper `static List<SimEvent> EventsOf(Simulation sim, int stageId)` (lọc `State.Events` theo `StageId`, giữ thứ tự).
     - `NoSeedsWaits`:
       - nhịp 75: `EventsOf(sow.Id)` có 1 phần tử: `StallStarted`, nhịp 75, ô (5,2,5), `NoSeeds`;
       - nhịp 76: 2 phần tử, `[1]` = `StallCleared`, nhịp 76, ô (5,2,5), `NoSeeds`.
     - `StorageFullWaits`:
       - nhịp 1395: `EventsOf(haul.Id)` = [`StallStarted`, 1395, (5,2,5), `StorageFull`];
       - nhịp 1396: thêm `[1]` = `StallCleared`, 1396, (5,2,5), `StorageFull`.

8. **`HomeTests.cs`** (mới; namespace `Game.Core.Tests`; `using System; using System.Collections.Generic; using Game.Core.Sim; using NUnit.Framework;`).
   - **Quy ước chung:**
     - Helper `RunTo(sim, tick)` như `FarmTests`. `NewOrder(sim) = sim.AddOrder(new List<CellPos>())`.
     - World `TestWorlds.Flat(1)` (đứng ở y = 2), config mặc định.
     - **Không** dùng `HomeAll`; nhà đặt tay.
     - Id sinh vật / nhà theo thứ tự thêm.
     - "Đặt `Stone` tại …" = `world.Set(x, y, z, Cell.Make(BlockMaterial.Stone))` **trước** khi tạo `Simulation`. "Xoá … giữa test" = `world.Set(…, Cell.Make(BlockMaterial.Empty))` rồi `sim.OnCellChanged(x, y, z)` một lần.
     - **Hố** = đặt `Stone` 2 tầng (y = 2 và 3) tại (4,·,3), (2,·,3), (3,·,4), (3,·,2). Sinh vật ở (3,2,3) không đi đâu được.
     - **Nhà chuẩn** = `AddHouse((10,2,10))`; ô hiện lại của sinh vật 1 × 1 × 1 là **(12,2,10)**.
   - `HouseBlocksAndValidates`:
     - Phần nhà:
       - `inv = new MaterialInventory(); inv.Add(BlockMaterial.Dirt); editor = new WorldEditor(world, inv, 1); editor.IsOccupied = sim.IsOccupied;`.
       - `AddHouse((5,2,5)) == 0`.
       - `IsOccupied`: (5,2,5) true, (6,3,6) true, (7,2,5) false, (5,4,5) false.
       - `editor.TryPlace(6,2,6, Dirt) == EditResult.Occupied`.
       - `ArgumentException` khi: `AddCreature("L1", (6,2,5))`; `AddHouse((6,2,6))`; `AddHouse((5,3,9))` (lơ lửng); `AddStorage((6,2,5))`; `AddHouse((15,2,15))` (ra ngoài map).
       - `Houses.Count == 1`.
     - Phần gắn nhà:
       - Thêm L1 (3,2,5), L2 (3,2,7), L3 (3,2,9). `AssignHome(0,0)`, `AssignHome(1,0)` được.
       - `ArgumentException` khi: `AssignHome(2,0)` (đủ); `AssignHome(0,0)` (đã có nhà); `AssignHome(2,5)`; `AssignHome(9,0)`.
       - `Residents` = [0, 1]; `Creatures[2].HomeId == -1`.
     - Phần đi vòng nhà: `AddStage(NewOrder, Till, (8,2,5))`.
       - nhịp 1: L1 `Moving`, `Path.Count == 7` (vòng qua nhà).
       - nhịp 107: chưa `Done`; nhịp 108: `Done` (1 + ⌈7000/150⌉ = 48, 48 + 60 = 108).
   - `NoHomeNoWork`:
     - L1 (2,2,2), **chưa có nhà**; `s = AddStage(NewOrder, Till, (4,2,2))`.
     - nhịp 1: `s` `Blocked` + `NoAbility`. `Events.Count == 1`: `StallStarted`, nhịp 1, `s`, (4,2,2), `NoAbility`.
     - nhịp 10: `Events.Count == 1`, L1 `Idle`.
     - Rồi `AddHouse((10,2,10))`, `AssignHome(0,0)`.
     - nhịp 11: `s` `Assigned`, L1 `Moving`. `Events.Count == 2`, `[1]` = `StallCleared`, nhịp 11, `s`, (4,2,2), `NoAbility`.
     - nhịp 84: chưa `Done`; nhịp 85: `Done` (11 + ⌈2000/150⌉ = 25, 25 + 60 = 85).
   - `NoAbilityCountsHomedOwnersOnly`:
     - Nhà chuẩn. L1 (2,2,2), `AssignHome(0,0)`.
     - `s0 = AddStage(o, Water, (4,2,2))`, `s1 = AddStage(o, Till, (2,2,6))`, `s2 = AddStage(o, Till, (2,2,9))` (cùng `o = NewOrder`).
     - nhịp 1: `s0` `Blocked` + `NoAbility`; `s1` `Assigned` cho L1; `s2` `Open` + `Stall == None` (L1 bận vẫn tính là có năng lực). `Events.Count == 1`: `StallStarted`, nhịp 1, `s0`, (4,2,2), `NoAbility`.
     - nhịp 10: `Events.Count == 1`.
     - Rồi thêm L2 (6,2,2), `AssignHome(1,0)`.
     - nhịp 11: `s0` `Assigned`, `AssigneeId == 1`. `Events.Count == 2`, `[1]` = `StallCleared`, nhịp 11, `s0`, (4,2,2), `NoAbility`.
   - `StuckIdleRespawnsAtHome`:
     - Hố. Nhà chuẩn. L1 (3,2,3), L2 (6,2,6); `AssignHome(0,0)`, `AssignHome(1,0)`.
     - nhịp 1: L1 `HomeReachable == false`, `StuckSince == 1`; L2 `HomeReachable == true`, `StuckSince == -1`.
     - nhịp 300: L1 ở (3,2,3), `Events.Count == 0`.
     - nhịp 301:
       - L1 ở (12,2,10), `Idle`, `StuckSince == -1`, `HomeReachable == true`, `Path.Count == 0`;
       - `Events.Count == 1`: `CreatureRespawned`, nhịp 301, `StageId == -1`, `CreatureId == 0`, (12,2,10), `Reason == None`.
     - nhịp 400: L2 ở (6,2,6), `StuckSince == -1`; `Events.Count == 1`.
   - `WorkDoesNotCountAndWorkResets`:
     - Hố. Nhà chuẩn. L1 (3,2,3), `AssignHome(0,0)`.
     - nhịp 100: `StuckSince == 1`, L1 ở (3,2,3).
     - Rồi `s = AddStage(NewOrder, Till, (3,2,3))`.
     - nhịp 101: L1 `Working`, `StuckSince == -1`.
     - nhịp 160: `s` chưa `Done`. nhịp 161: `s` `Done`, L1 `Idle`, `StuckSince == 161`.
     - nhịp 460: L1 ở (3,2,3). nhịp 461: L1 ở (12,2,10).
   - `RespawnWaitsForStandableCell`:
     - Hố. Đặt thêm `Stone` y = 2 và 3 tại 8 cột (12,10), (12,11), (9,10), (9,11), (10,12), (11,12), (10,9), (11,9) (dạng (x, z)). Nhà chuẩn. L1 (3,2,3), `AssignHome(0,0)`.
     - nhịp 305: L1 ở (3,2,3), `StuckSince == 1`, `Events.Count == 0`.
     - Xoá (12,2,10) và (12,3,10) (một lần `OnCellChanged(12,2,10)`).
     - nhịp 306: L1 ở (12,2,10); `Events.Count == 1`: `CreatureRespawned`, nhịp 306.
   - `DroppedLoadFollowsCreatureHome`:
     - Đặt `Stone` y = 2, 3 tại (4,·,3), (2,·,3), (3,·,4) — hố **còn hở** phía (3,·,2).
     - Nhà chuẩn. L3 (3,2,3), `AssignHome(0,0)`. `h = AddHaulStage(NewOrder, (3,2,3), (3,2,0))`.
     - nhịp 1: `h` `Carried`, L3 `Moving`, `Path.Count == 3`.
     - Rồi đặt `Stone` tại (3,2,2) và (3,3,2) giữa test (`world.Set` + `OnCellChanged(3,2,2)`).
     - nhịp 2:
       - `h`: `Blocked` + `NoPath`, `Carried == false`, `Target == (3,2,3)`, `DroppedBy == 0`;
       - L3: `Idle`, `HomeReachable == false`, `StuckSince == 2`;
       - `Events.Count == 1`: `StallStarted`, nhịp 2, `h`, (3,2,3), `NoPath`.
     - nhịp 301: L3 ở (3,2,3).
     - nhịp 302:
       - L3 ở (12,2,10);
       - `h`: `Open`, `Stall == None`, `Target == (12,2,10)`, `DroppedBy == -1`;
       - `Events.Count == 3`: `[1]` = `CreatureRespawned`, nhịp 302, L3, (12,2,10); `[2]` = `StallCleared`, nhịp 302, `h`, (12,2,10), `NoPath`.
     - nhịp 303: `h` `Assigned`, `Carried == true`, L3 `Path.Count == 19`.
     - nhịp 417: `h` chưa `Done`. nhịp 418: `h` `Done`, L3 ở (3,2,0) (303 + ⌈19000/200⌉ = 398, 398 + 20 = 418).

9. **Biên dịch + `.meta`**: MCP `refresh_unity`, `read_console` không có lỗi.

10. **Commit** sau khi Kiểm chứng đạt: message `M1 sim bước 4: đình trệ + thông báo, nhà mẫu, sinh vật bị kẹt (brief 0004)`. Không push.

## Tiêu chí xong
- Mọi test ở bước 1, 6, 7, 8 đạt; EditMode toàn bộ 0 fail. Số test = 131 + 7 (`HomeTests`) = **138**.
- `git diff af43ba8 -- client/Assets/Game/Tests/Core/Sim/` chỉ có:
  - thêm `HomeAll` / gọi `HomeAll`;
  - 2 kỳ vọng `NoAbility` (bước 6);
  - assert sự kiện + `EventsOf` (bước 7);
  - 2 dòng `ValidateRejectsBadValues`;
  - `out var editor` trong `NoPathAtAssignBlocks`;
  - file `HomeTests.cs` mới.

  Không đổi số nhịp, ô hay số lượng cũ nào.
- `grep -rnE "float|double|System\.Random|UnityEngine|DateTime|Dictionary|HashSet|PriorityQueue" client/Assets/Game/Core/Sim/` → rỗng.
- `python3 tools/check_design.py` exit 0; `git status` sạch sau commit, chỉ file trong Phạm vi.

## Kiểm chứng
1. MCP `refresh_unity` → `read_console` không có lỗi biên dịch.
2. MCP `run_tests` EditMode toàn bộ, ghi dòng tổng kết. Nếu Editor tắt thì chạy `python3 tools/unity_test.py EditMode`. Không chạy PlayMode.
3. Lệnh grep ở Tiêu chí xong → rỗng.
4. `python3 tools/check_design.py` → exit 0.
5. `git status --short` + `git diff --stat` trước commit: chỉ file trong Phạm vi.
6. Ghi giờ bắt đầu/kết thúc (đồng hồ máy) vào Kết quả.

## Dừng và chuyển lại Opus khi
- Một số trong test (nhịp, ô, số lượng) không khớp sau khi đã kiểm cài đặt đúng từng quy tắc ở "Quyết định đã chốt" và bước 3–4. **Không sửa số trong test cho khớp code.** Ghi số thực tế + trạng thái liên quan vào Vướng mắc.
- Test bước 1–3 hỏng sau khi gắn nhà (ví dụ nhà ở góc chắn đường, hoặc sinh vật hiện lại giữa test).
- Brief mơ hồ hoặc mâu thuẫn với code thật. Cần quyết định ngoài "Quyết định đã chốt", hoặc cần API ngoài brief.
- Cùng một lỗi sửa 2 lần vẫn chưa qua. Phải sửa file ngoài "Phạm vi".

---
<!-- Từ đây trở xuống: EXECUTOR ghi. -->

## Kết quả
- Base commit: 79bcd82
- Bắt đầu: 2026-10-14 16:47 · Kết thúc: 2026-10-14 16:50 (đồng hồ máy)
- Commit code: 5394feb — mới: Tests/Core/Sim/HomeTests.cs (+ .meta); sửa: Core/Sim/{SimConfig,SimData,Simulation,SimHash}.cs, Tests/Core/Sim/{TestWorlds,SimulationTests,SimulationTerrainTests,FarmTests,SimDeterminismTests,SimConfigTests}.cs, design/data/sim_m1.json
- Lệch brief: không có. `GridPathfinder.cs`/`GridPathfinderTests.cs` không sửa.
<!-- Base commit (trước khi làm) · commit của brief · file đã đổi · ghi chú lệch so với brief (nếu có, kèm lý do). -->

## Kết quả kiểm chứng
- MCP `refresh_unity` + `read_console` (error) → 0 lỗi biên dịch.
- MCP `run_tests` EditMode → 138 total, 138 passed, 0 failed (131 + 7 HomeTests). Không chạy PlayMode.
- grep cấm trong Core/Sim → rỗng. `python3 tools/check_design.py` → exit 0.
- `git status --short` trước commit: chỉ file trong Phạm vi; `GridPathfinder*` không đổi.

## Vướng mắc
<!-- Khi trạng thái = cần Opus: câu hỏi cụ thể + các phương án (kèm đề xuất). -->

---
<!-- PLANNER ghi khi review. -->

## Review
<!-- Kết luận: đã duyệt | cần sửa (thêm bước sửa vào "Các bước", trạng thái về chờ làm). Điểm đã đối chiếu. -->
**Đã duyệt** (Planner, 2026-10-14 16:55, mức đầy đủ — đọc diff `5394feb`).
- Tiêu chí xong:
  - EditMode 138/138 [run, theo Kết quả của Executor];
  - grep cấm rỗng; `check_design.py` exit 0;
  - không chạy lại vì kết quả đủ, không đáng ngờ.
- Quyết định đã chốt:
  - `NoAbility`: kiểm sau `NoSeeds`/`StorageFull`, chỉ tính sinh vật có nhà; mở lại ở đầu `AssignPhase`; `eligible` bỏ sinh vật không nhà.
  - `ReportPhase`: so `ReportedStall` ở cuối nhịp; đổi X → Y chỉ phát `StallStarted(Y)`.
  - Nhà: `AddHouse` kiểm `CanStand` + `IsOccupied` rồi `SetBlocked` 8 ô; `IsOccupied` gồm nhà; `AssignHome` đủ 4 lỗi.
  - Ô kề nhà: thứ tự +X/−X/+Z/−Z, dy 0/+1/−1.
  - `homeDirty`: bật ở `AddStorage`/`AddHouse`/`AssignHome`/sau `TerrainPhase`, tính lại ngay sau địa hình.
  - `StuckPhase`: đếm khi có nhà + rảnh + không đường về; `Respawn` chờ khi không có ô; hàng đã rơi (`DroppedBy`) theo về, mở lại `NoPath`.
  - Thứ tự `Tick()` đúng.
- Test cũ: diff chỉ có `HomeAll`, 2 kỳ vọng `NoAbility`, assert sự kiện, 2 dòng config, `out var editor`; không đổi số nhịp/ô cũ nào. Số trong `HomeTests` đúng brief.
- Phạm vi: 13 file đúng danh sách; `GridPathfinder*` không đổi.
- Ghi chú (không cần sửa):
  - `HasHomedAbility` duyệt toàn bộ sinh vật cho mỗi stage mở, và `ReportPhase` duyệt mọi stage kể cả `Done` → O(stage × sinh vật) mỗi nhịp. Xem lại khi đo ở bước 7.
  - `Events` không giới hạn kích thước; nơi đọc (bước 6) phải `Clear()`.
