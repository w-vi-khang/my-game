# Brief 0003 — Simulation bước 3: lệnh trồng cây 5 công đoạn, ô ruộng có hàng đợi vụ, một kho

> Trạng thái: chờ làm
> Review: đầy đủ
> Model thực thi: Sonnet · high
> Cập nhật: 2026-10-14 (trả lời Vướng mắc)

<!-- Planner (Opus) viết, Executor (Sonnet) làm theo ở PHIÊN KHÁC — executor không có lịch sử chat, không được tự quyết.
Trạng thái: chờ làm → đang làm → chờ review → đã duyệt; hoặc → cần Opus (executor dừng, hỏi ở mục Vướng mắc).
Review: nhẹ (đọc mục Kết quả) | đầy đủ (đọc cả diff) | user xem (có phần user tự kiểm bằng mắt/chơi thử) — có thể ghép "đầy đủ + user xem".
File được commit. Tự kiểm trước khi giao (AGENTS.md §10.3): executor có phải hỏi gì không? còn chỗ nào bỏ ngỏ 2 phương án? -->

## Mục tiêu
Thêm lệnh trồng cây vào mô phỏng. Mỗi ô ruộng tự chạy chuỗi xới → gieo (trừ 1 hạt trong kho) → tưới → cây lớn 1200 nhịp → thu hoạch → vận chuyển 1 nông sản + 1 hạt vào kho. Mỗi ô có hàng đợi vụ tối đa 2. Sinh vật ưu tiên đứng ở ô kề để làm. Kho chiếm ô (chặn đường), giao hàng ở ô kề kho.
Task contract: `.ai/tasks/m1-sim-buoc-3-trong-cay.md`. Quyết định: `design/decisions/0016-o-ruong-kho-nha-va-sinh-vat-ket.md`.

## Bối cảnh cần đọc
- `client/Assets/Game/Core/Sim/Simulation.cs` (commit `9834dee`): `:16` constructor; `:51` `AddStage`; `:61` `AddHaulStage`; `:69` `AddStorage`; `:77` `IsOccupied`; `:88` `Tick()`; `:101` `CanStand(Creature)`; `:103` `FindPath(Creature, from, to)`; `:105` `TerrainPhase`; `:178` `Repath`; `:191` `CopyPath`; `:198` `BlockNoPath`; `:206` `WorkPhase`; `:226` `MovePhase`; `:249` `OnArrive`; `:275` `AssignPhase`; `:340` `Assign(Creature c, WorkStage s, List<CellPos> path)`; `:363` `Unassign`.
- `SimData.cs`: `:30` `StageStatus`; `:33` `StallReason { None = 0, NoPath = 1 }`; `:79` `WorkStage` (constructor `(int id, int orderId, WorkAbility ability, CellPos target, bool hasDestination, CellPos destination, int baseTicks, int priority)`); `:112` `WorkOrder`; `:125` `Storage` (constructor `(int id, CellPos cell, int capacity, int seeds)`); `:143` `SimState`.
- `SimConfig.cs`: `:56` `CropConfig` (`growTimeMs = 120000`, `seedsPerHarvest = 1`, `producePerHarvest = 1`); `:64` `StorageConfig` (`capacity = 50`, `startSeeds = 10`); `:94-95` field `crop`, `storage`; `:105` `MsToTicks`; `:123` `Validate()`, kết thúc ở `:170`.
- `SimHash.cs`: `:32` hết phần creature; `:51` dòng cuối phần stage (`Stall`); `:76` dòng cuối phần storage (`Produce`); `:78` `return h;`.
- `GridPathfinder.cs`: `:29` constructor; `:40` `IsBodyFree`; `:44` `IsSupport`; `:51` `CanStand`; `:63` `CanStep`; `:82` `FindPath(CellPos start, CellPos goal, int sx, int sz, int sy, List<CellPos> result)`; heap `Push`/`Pop`/`Less` ở cuối file.
- World (không sửa): `WorldData.GetMaterial(x, y, z)`, `WorldSize.InBounds`, `WorldSize.Index` (`WorldData.cs:27`, `:31`); `Cell.cs:4-14` `BlockMaterial` (`Grass = 1`, `Dirt = 2`, `Stone = 3`, `Bedrock = 6`). `WorldEditor(WorldData world, MaterialInventory inventory, int meshMargin, int seaLevel = -1)`; `TryPlace(int x, int y, int z, BlockMaterial material)` trả `EditResult.Occupied` khi `IsOccupied` true; `public Func<int, int, int, bool> IsOccupied;`.
- Test hiện có: `client/Assets/Game/Tests/Core/Sim/` — 118 EditMode toàn project. `TestWorlds.Flat(groundTop)`: y = 0 `Bedrock`, y = 1..groundTop `Dirt`, sinh vật đứng ở y = groundTop + 1. `SimDeterminismTests.cs:18` có `sim.AddStorage(new CellPos(10, 1, 10))` và `:28-29` haul tới `(10, 1, 10)`.
- Quy ước thời gian hiện có (đã kiểm bằng test bước 1): giao việc ở nhịp t, đường dài n ô, tốc độ v (phần nghìn ô/nhịp; L1, L2 = 150, L3 = 200) → tới nơi ở nhịp t + ⌈1000·n / v⌉; làm bắt đầu nhịp sau, công đoạn B nhịp xong ở nhịp (tới nơi) + B. Giao việc mà đã đứng đúng chỗ → `Working` ngay nhịp t, xong ở t + B. Nhịp làm: xới 60, gieo 30, tưới 30, thu hoạch 40, vận chuyển 20.

## Quyết định đã chốt (không được đổi)
- (user, 0016) Ô ruộng: ưu tiên đứng ở **ô kề**; không có ô kề nào tới được thì đứng ngay trên ô. Kho là công trình chiếm ô; giao hàng ở **ô kề kho**. Mỗi ô ruộng có chuỗi riêng + hàng đợi vụ, **tối đa 2** (cấu hình). Thu hoạch ra 1 nông sản + 1 hạt, **một** công đoạn vận chuyển mang cả hai. Sức chứa kho tính **chung** hạt + nông sản.
- **Ô ruộng** = ô `Dirt` hoặc `Grass` (gọi là ô đất `F`) có ô ngay trên `S = F + (0, 1, 0)` trong map và `Empty`. `S` là **chỗ** của ô ruộng: công đoạn ruộng có `Target = S`.
- **Cách tiếp cận** (`Approach`): `Exact` — đứng đúng ô đích (như bước 1–2; `AddStage`/`AddHaulStage` cũ luôn dùng `Exact`). `Adjacent` — đứng ở một ô kề. `AdjacentOrOn` — thử `Adjacent` trước; không có đường tới ô kề nào thì thử `Exact`.
- **Ô kề của chỗ P** (với sinh vật c): duyệt hướng (dx, dz) theo thứ tự **(1,0), (−1,0), (0,1), (0,−1)**; mỗi hướng thử dy theo thứ tự **0, +1, −1**, lấy ô `P.Offset(dx, dy, dz)` **đầu tiên** mà `pathfinder.CanStand(ô, c.SizeX, c.SizeZ, c.SizeY)`. Tối đa 4 ô, theo thứ tự hướng.
- **Chọn ô kề**: A\* nhiều đích (`FindPathToAny`) từ ô sinh vật tới tập ô kề → đường ngắn nhất; hoà thì theo luật hàng đợi đã có (f, h, số thứ tự). Đang đứng trên một ô kề → đường rỗng, làm ngay.
- **Kho chặn thân**: ô kho không cho thân (`IsBodyFree` = false). Không đứng lên kho được (ô kho trong world vẫn `Empty` → không là ô đỡ). `IsOccupied` trả true cho ô kho → `WorldEditor` không đặt khối vào.
- **Hạt** trừ 1 khi **giao** công đoạn gieo cho sinh vật; trả lại khi `Unassign` công đoạn gieo chưa xong. Kho còn 0 hạt → công đoạn gieo `Blocked`, `Stall = NoSeeds`, không giao.
- **Chỗ trong kho**: `Free = Capacity − Seeds − Produce − Reserved`. Giao công đoạn vận chuyển của ruộng → `Reserved += LoadProduce + LoadSeeds`; giao xong → `Reserved −= …`, `Produce += LoadProduce`, `Seeds += LoadSeeds`; `Unassign` → `Reserved −= …`. `Free` < tải → `Blocked`, `Stall = StorageFull`, không giao.
- Kiểm hạt / chỗ kho **trước** khi tìm sinh vật. `NoSeeds`, `StorageFull` được kiểm lại ở **đầu mỗi** `AssignPhase` (đủ điều kiện thì về `Open`, `Stall = None`). `NoPath` vẫn chỉ mở lại khi địa hình đổi (như bước 2).
- **Hàng đợi vụ** = `List<int>` chỉ số loại cây (M1 luôn `0`, chỉ có `Config.crop`). Gồm cả vụ đang làm. Thu hoạch xong → `RemoveAt(0)`; còn vụ → bắt đầu lại từ xới; hết → ô `Idle`.
- **Công đoạn mới tạo ngay trong `WorkPhase`** ở nhịp công đoạn trước xong (giao được ngay `AssignPhase` cùng nhịp). Tưới xong ở nhịp T → `RipeTick = T + MsToTicks(crop.growTimeMs)` (= T + 1200); `CropPhase` ở nhịp `Tick >= RipeTick` chuyển sang thu hoạch và tạo công đoạn thu hoạch.
- Thứ tự `Tick()`: `Tick++` → (địa hình bẩn) địa hình → làm → **cây** (`CropPhase`, mới) → đi → giao việc.
- Test bước 1–2 **không đổi số nhịp nào**. Chỉ được đổi vị trí kho trong `SimDeterminismTests` (bước 8).

## Phạm vi
- Được tạo: `client/Assets/Game/Tests/Core/Sim/FarmTests.cs` (+ `.meta`).
- Được sửa: `client/Assets/Game/Core/Sim/{SimConfig,SimData,Simulation,SimHash,GridPathfinder}.cs`, `client/Assets/Game/Tests/Core/Sim/{GridPathfinderTests,SimDeterminismTests,SimConfigTests}.cs`, `design/data/sim_m1.json`, brief này (Kết quả, Kết quả kiểm chứng, Vướng mắc, Trạng thái).
- Không được sửa: mọi file khác — nhất là `client/Assets/Game/Core/*.cs` ngoài `Sim/`, asmdef, `SimulationTests.cs`, `SimulationTerrainTests.cs`, `design/` trừ `sim_m1.json`, `.ai/tasks/`.

## Các bước
1. **Config** (`SimConfig.cs`, `sim_m1.json`):
   - Thêm class `[Serializable] public class FarmConfig { public int plotQueueCapacity = 2; }` (sau `StorageConfig`) và field `public FarmConfig farm = new FarmConfig();` sau `storage`.
   - `Validate()`, cuối hàm: `if (farm == null || farm.plotQueueCapacity < 1 || farm.plotQueueCapacity > 8) throw new ArgumentException("farm.plotQueueCapacity phải trong [1, 8].");`
   - Thêm `public int GrowTicks => MsToTicks(crop.growTimeMs);`.
   - `sim_m1.json`: sau khối `"storage"` thêm `"farm": { "plotQueueCapacity": 2 }` (nhớ dấu phẩy).
   - `SimConfigTests.ValidateRejectsBadValues`: thêm `c = new SimConfig(); c.farm.plotQueueCapacity = 0; Assert.Throws<ArgumentException>(c.Validate, "hàng đợi 0");`.
   - Xong khi: `ConfigMatchesCodeDefaults` vẫn đạt.

2. **Dữ liệu** (`SimData.cs`):
   - `StallReason` thêm `NoSeeds = 2, StorageFull = 3` (comment đổi thành "Lý do đình trệ — bước 4 thêm thông báo và thiếu năng lực.").
   - Thêm `public enum Approach : byte { Exact = 0, Adjacent = 1, AdjacentOrOn = 2 }` và `public enum PlotPhase : byte { Idle = 0, Till = 1, Sow = 2, Water = 3, Growing = 4, Harvest = 5 }`.
   - `WorkStage`: thêm field `public Approach TargetApproach; public readonly Approach DestinationApproach; public readonly int PlotId; public readonly int StorageId; public readonly int LoadProduce, LoadSeeds;`. Constructor thêm tham số cuối, có mặc định: `Approach targetApproach = Approach.Exact, Approach destinationApproach = Approach.Exact, int plotId = -1, int storageId = -1, int loadProduce = 0, int loadSeeds = 0`. `PlotId ≥ 0` chỉ cho công đoạn xới/gieo/tưới/thu hoạch của ruộng; vận chuyển của ruộng có `PlotId = -1`, `StorageId ≥ 0`.
   - `Storage`: thêm `public int Reserved;` và `public int Free => Capacity - Seeds - Produce - Reserved;`.
   - Thêm class:
     ```csharp
     public sealed class FarmPlot
     {
         public readonly int Id, OrderId, StorageId;
         public readonly CellPos Soil, Site;
         public readonly List<int> Queue = new List<int>();
         public PlotPhase Phase;
         public int StageId = -1;
         public long RipeTick;
         public FarmPlot(int id, int orderId, int storageId, CellPos soil) { ... Site = soil.Offset(0, 1, 0); }
     }
     ```
   - `SimState`: thêm `public readonly List<FarmPlot> Plots = new List<FarmPlot>();`.

3. **`GridPathfinder.cs`:**
   - Thêm `readonly bool[] blocked;` (cấp theo `CellCount` trong constructor) và `public void SetBlocked(CellPos c, bool value)` (ngoài map → `ArgumentException`). `IsBodyFree` trả `false` nếu `blocked[Index]`. `IsSupport` không đổi.
   - Thêm `public bool FindPathToAny(CellPos start, List<CellPos> goals, int sx, int sz, int sy, List<CellPos> result)`: `result.Clear()`. Bỏ các đích không `CanStand` (lọc vào list nội bộ của class, không cấp mới mỗi lần). Không còn đích → `false`. `start` trùng một đích → `true` (rỗng). `start` ngoài map → `false`. Heuristic = **min** `|dx| + |dz|` tới các đích. Đích kiểm khi **lấy ra** khỏi heap (như `FindPath`). Còn lại giống hệt `FindPath` (thứ tự hướng, dy, so heap, dựng đường).
   - `FindPath` phải cho **đúng kết quả cũ** (test `GridPathfinderTests` hiện có không đổi). Được viết cả hai qua một lõi chung.
   - Test mới trong `GridPathfinderTests.cs`, `FindPathToAnyPicksNearest` (world `Flat(0)`, khung 1×1×1): (a) start (1,1,1), goals [(9,1,1), (3,1,4)] → `true`, 5 ô, ô cuối (3,1,4); (b) goals [(3,5,4), (9,1,1)] ((3,5,4) lơ lửng) → `true`, 8 ô, ô cuối (9,1,1); (c) goals [(1,1,1)] → `true`, rỗng; (d) goals rỗng → `false`.

4. **Simulation — kho, cách tiếp cận** (`Simulation.cs`):
   - `AddStorage(CellPos cell)`: thêm kiểm, sai thì `ArgumentException`: `pathfinder.CanStand(cell, 1, 1, 1)`; `!IsOccupied(cell.X, cell.Y, cell.Z)` (gồm sinh vật và kho khác). Rồi `pathfinder.SetBlocked(cell, true)`.
   - `IsOccupied`: thêm true nếu trùng `Cell` của một kho.
   - Thêm `readonly List<CellPos> goals = new List<CellPos>();` và hàm private `void AdjacentGoals(Creature c, CellPos p)` điền `goals` theo luật "Ô kề" ở Quyết định đã chốt.
   - Thêm hàm private `bool PlanTo(Creature c, CellPos site, Approach a)` — kết quả đường trong `tmpPath`:
     - `Exact`: `c.Cell == site` → `tmpPath.Clear()`, `true`; ngược lại `FindPath(c, c.Cell, site)`.
     - `Adjacent`: `AdjacentGoals(c, site)`; `pathfinder.FindPathToAny(c.Cell, goals, c.SizeX, c.SizeZ, c.SizeY, tmpPath)`.
     - `AdjacentOrOn`: thử `Adjacent`; `false` thì trả kết quả `Exact`.
   - Thêm hàm private `void SetPath(Creature c)`: chép `tmpPath` vào `c.Path`, `c.PathIndex = 0`, `c.MoveTarget = tmpPath.Count > 0 ? tmpPath[tmpPath.Count - 1] : c.Cell`.
   - **AssignPhase**: thay `if (pick.Cell == s.Target) … if (FindPath(pick, pick.Cell, s.Target)) …` bằng một nhánh: `if (PlanTo(pick, s.Target, s.TargetApproach)) { Assign(pick, s, tmpPath); assigned = true; break; }`.
   - **Assign**: nhánh đứng sẵn đổi điều kiện thành `path.Count == 0` (không còn `c.Cell == s.Target`); nhánh còn lại dùng `SetPath(c)` (path truyền vào chính là `tmpPath`). Bỏ tham số `null` ở chỗ gọi.
   - **OnArrive** (vận chuyển chưa mang): `s.Carried = true;` rồi `PlanTo(c, s.Destination, s.DestinationApproach)`: `false` → `BlockNoPath(c)`; `true` và `tmpPath.Count == 0` → `Working`; còn lại → `SetPath(c)` (vẫn `Moving`).
   - **Repath(c)**: chặng hiện tại là `(s.Destination, s.DestinationApproach)` nếu `s.HasDestination && s.Carried`, ngược lại `(s.Target, s.TargetApproach)`. `PlanTo` được → `SetPath(c)`, `c.MoveProgress = 0` (đường rỗng thì `MovePhase` nhịp sau gọi `OnArrive`); không được → `BlockNoPath(c)`.
   - Xong khi: 118 test cũ vẫn đạt (trừ `SimDeterminismTests.SameInputSameHashEveryTick` cần bước 8).

5. **Simulation — lệnh trồng cây:**
   - `public int AddFarmOrder(IReadOnlyList<CellPos> soilCells, int storageId)`. `ArgumentException` khi: `storageId` không có; một ô ngoài map, không phải `Dirt`/`Grass`, hoặc ô trên ngoài map / khác `Empty`; một ô trùng ô khác trong danh sách hoặc trùng `Soil` của ô ruộng đã có. Kiểm hết trước khi thêm gì. Rồi: `AddOrder(soilCells)`; với từng ô theo thứ tự: tạo `FarmPlot` (id = `Plots.Count`), `Queue.Add(0)`, `StartCrop(plot)`. Trả id lệnh.
   - `public bool EnqueueCrop(int plotId)`: `Queue.Count >= Config.farm.plotQueueCapacity` → `false`. `Queue.Add(0)`; nếu `Phase == Idle` thì `StartCrop(plot)`. `true`.
   - Private `void StartCrop(FarmPlot p)`: `p.Phase = Till; AddPlotStage(p, WorkAbility.Till);`.
   - Private `void AddPlotStage(FarmPlot p, WorkAbility a)`: `new WorkStage(Stages.Count, p.OrderId, a, p.Site, false, default, Config.BaseTicks(a), 0, Approach.AdjacentOrOn, Approach.Exact, p.Id)`, thêm vào `Stages` và `Orders[p.OrderId].StageIds`; `p.StageId` = id.
   - **WorkPhase**, ngay sau khi một công đoạn chuyển `Done` (sau `c.StageId = -1`), gọi private `OnStageDone(WorkStage s)`:
     - `s.PlotId >= 0`, theo `s.Ability`: `Till` → `Phase = Sow`, `AddPlotStage(p, Sow)`. `Sow` → `Phase = Water`, `AddPlotStage(p, Water)`. `Water` → `Phase = Growing`, `p.StageId = -1`, `p.RipeTick = State.Tick + Config.GrowTicks`. `Harvest` → tạo vận chuyển: `new WorkStage(Stages.Count, p.OrderId, WorkAbility.Haul, p.Site, true, Storages[p.StorageId].Cell, Config.BaseTicks(Haul), 0, Approach.AdjacentOrOn, Approach.Adjacent, -1, p.StorageId, Config.crop.producePerHarvest, Config.crop.seedsPerHarvest)` (thêm vào `Stages` + `StageIds`); rồi `p.Queue.RemoveAt(0)`; `Queue.Count > 0` → `StartCrop(p)`, ngược lại `Phase = Idle`, `StageId = -1`.
     - `s.Ability == Haul && s.StorageId >= 0`: `Reserved −= LoadProduce + LoadSeeds`, `Produce += LoadProduce`, `Seeds += LoadSeeds`.
   - **CropPhase** (private, gọi trong `Tick()` giữa `WorkPhase` và `MovePhase`): duyệt `Plots` theo id; `Phase == Growing && State.Tick >= RipeTick` → `Phase = Harvest`, `AddPlotStage(p, Harvest)`.
   - **AssignPhase**:
     - Đầu hàm: mọi stage `Blocked` có `Stall == NoSeeds` mà kho của ô (`Storages[Plots[s.PlotId].StorageId]`) có `Seeds > 0`, hoặc `Stall == StorageFull` mà `Storages[s.StorageId].Free >= LoadProduce + LoadSeeds` → `Open`, `Stall = None`.
     - Trong vòng duyệt stage, **trước** khi lập `eligible`: gieo của ruộng (`PlotId >= 0 && Ability == Sow`) mà kho `Seeds == 0` → `Blocked`, `NoSeeds`, `continue`. Vận chuyển có `StorageId >= 0` mà `Free < tải` → `Blocked`, `StorageFull`, `continue`.
   - **Assign**: gieo của ruộng → `Seeds--`; vận chuyển có `StorageId >= 0` → `Reserved += tải`.
   - **Unassign**: (trước khi xoá `c.StageId`) gieo của ruộng → `Seeds++`; vận chuyển có `StorageId >= 0` → `Reserved −= tải`. Phần hàng rơi tại chỗ (`Target = c.Cell`) giữ nguyên.

6. **`SimHash.cs`:** phần stage sau `Stall` thêm `(int)TargetApproach`, `(int)DestinationApproach`, `PlotId`, `StorageId`, `LoadProduce`, `LoadSeeds`. Phần storage sau `Produce` thêm `Reserved`. Trước `return h;` thêm phần plot: `Plots.Count`, rồi mỗi plot: `Id`, `OrderId`, `StorageId`, `Soil` X/Y/Z, `(int)Phase`, `StageId`, `MixLong(RipeTick)`, `Queue.Count`, từng phần tử `Queue`.

7. **`FarmTests.cs`** (namespace `Game.Core.Tests`, `using System; using System.Collections.Generic; using Game.Core.Sim; using NUnit.Framework;`). Helper `Run(sim, n)` như `SimulationTests`. World `TestWorlds.Flat(1)` (đất ở y = 1, đứng ở y = 2) trừ khi ghi khác. Config mặc định trừ khi ghi khác. **Bố trí chuẩn** `Layout(cfg, withL3)`: ô ruộng `F = (5,1,5)` → `S = (5,2,5)`; kho `K = (6,2,9)`; L1 tại (8,2,5); L2 tại (5,2,1); nếu `withL3` thì L3 tại (9,2,5); thêm sinh vật theo thứ tự L1, L2, L3, rồi `AddStorage(K)`, rồi `AddFarmOrder([F], kho)`. "Nhịp n" = trạng thái **sau** lần `Tick()` thứ n. Id sinh vật theo thứ tự thêm (Layout: L1 = 0, L2 = 1, L3 = 2); kho đầu tiên id 0, ô ruộng đầu tiên id 0. "Đặt `Stone` tại …" = `world.Set(x, y, z, Cell.Make(BlockMaterial.Stone))` **trước** khi tạo `Simulation`. "Gieo"/"haul"/"xới" = công đoạn có `Ability` tương ứng trong `State.Stages` (với Layout chỉ có một công đoạn mỗi loại cho tới nhịp được hỏi, trừ khi ghi khác).
   - `OneHarvestReachesStorage` (Layout, có L3):
     - nhịp 15: L1 ở (6,2,5), `Working`. nhịp 74: plot `Till`. nhịp 75: plot `Sow`, `Seeds == 9`, L1 `Working`.
     - nhịp 105: plot `Water`, L2 `Moving`. nhịp 125: L2 ở (5,2,4), `Working`. nhịp 154: plot `Water`. nhịp 155: plot `Growing`, `RipeTick == 1355`.
     - nhịp 1354: plot `Growing`. nhịp 1355: plot `Harvest`, L2 `Working`.
     - nhịp 1395: plot `Idle`, `Queue.Count == 0`; công đoạn cuối trong `Stages` là `Haul`, `Assigned` cho L3; `Reserved == 2`.
     - nhịp 1410: L3 ở (6,2,5), haul `Carried == true`.
     - nhịp 1444: `Produce == 0`. nhịp 1445: haul `Done`, `Produce == 1`, `Seeds == 10`, `Reserved == 0`, L3 ở (6,2,8).
   - `WithoutHaulerProduceStaysAtField` (Layout, không L3): chạy 1500 nhịp → haul `Open`, `AssigneeKind == None`, `Target == (5,2,5)`, `Produce == 0`, `Seeds == 9`.
   - `PrefersNeighborOverPlot`: L1 tại (5,2,5), kho (10,2,10), `AddFarmOrder([(5,1,5)])`. nhịp 1: L1 `Moving`, `Path` = [(6,2,5)]. nhịp 8: L1 ở (6,2,5), `Working`. nhịp 67: xới chưa `Done`; nhịp 68: xới `Done`.
   - `StandsOnPlotWhenNoNeighbor`: đặt `Stone` tại (6,2,5), (6,3,5), (4,2,5), (4,3,5), (5,2,6), (5,3,6), (5,2,4), (5,3,4). L1 tại (5,2,5), kho (10,2,10), `AddFarmOrder([(5,1,5)])`. nhịp 1: L1 `Working`, ở (5,2,5). nhịp 60: xới chưa `Done`; nhịp 61: `Done` (sửa sau Vướng mắc: 1 + 60 = 61).
   - `QueueRepeatsAndCapsAtTwo` (Layout, có L3): trước nhịp 1: `EnqueueCrop(0) == true`, `EnqueueCrop(0) == false`, `Queue.Count == 2`. nhịp 1395: plot `Till`, `Queue.Count == 1`, L1 `Working` ở (6,2,5). nhịp 1455: plot `Sow`, `Seeds == 9`.
   - `NoSeedsWaits`: config `storage.startSeeds = 0`. Layout không L3, chỉ cần L1 (vẫn thêm L2). nhịp 75: gieo `Blocked`, `Stall == NoSeeds`, L1 `Idle`. Gán `Storages[0].Seeds = 1`. nhịp 76: gieo `Assigned`, L1 `Working`, `Seeds == 0`.
   - `StorageFullWaits`: config `storage.capacity = 10` (startSeeds 10). Layout có L3. nhịp 1395: haul `Blocked`, `Stall == StorageFull`, L3 `Idle`, `Reserved == 0`. Gán `Storages[0].Seeds = 8`. nhịp 1396: haul `Assigned`, `Reserved == 2`.
   - `SowRefundsSeedWhenUnassigned` (Layout, có L3): nhịp 75: `Seeds == 9`. `Unassign(L1)` → `Seeds == 10`, gieo `Open`.
   - `DroppedLoadReleasesReservation` (Layout, có L3): nhịp 1415: L3 ở (6,2,6). `Unassign(L3)` → `Reserved == 0`, haul `Open`, `Carried == false`, `Target == (6,2,6)`. nhịp 1416: haul `Assigned`, `Reserved == 2`. Chạy tới nhịp 1600: haul `Done`, `Produce == 1`.
   - `StorageBlocksPathAndPlacement`: `Flat(1)`, `inv = new MaterialInventory(); inv.Add(BlockMaterial.Dirt); editor = new WorldEditor(world, inv, 1); editor.IsOccupied = sim.IsOccupied;`. Kho tại (6,2,5). `sim.IsOccupied(6,2,5) == true`; `editor.TryPlace(6,2,5, Dirt) == EditResult.Occupied`; `AddCreature("L1", (6,2,5))` → `ArgumentException`; `AddStorage((6,2,5))` lần nữa → `ArgumentException`. L1 tại (4,2,5), `AddStage(AddOrder(rỗng), Till, (8,2,5))`. nhịp 1: `Path.Count == 6`. nhịp 100: chưa `Done`; nhịp 101: `Done`.
   - `AddFarmOrderValidates`: kho (10,2,10). Mỗi trường hợp `ArgumentException`: ô (5,0,5) (đá đáy); `Stone` tại (5,2,5) rồi ô (5,1,5); `storageId = 5`; [(3,1,3), (3,1,3)]; sau một `AddFarmOrder([(7,1,7)])` hợp lệ, gọi lại với [(7,1,7)]. Trường hợp lỗi không thêm lệnh nào (`Orders.Count`, `Plots.Count` không đổi).

8. **`SimDeterminismTests.cs`:**
   - `BuildFarm`: đổi kho `(10, 1, 10)` → `(12, 1, 12)` (kho giờ chặn ô; haul ở dòng 28-29 vẫn tới (10,1,10)). Không đổi gì khác.
   - Test mới `FarmOrderSameHashEveryTick`: `Flat(1)`; L1 (1,2,1), L1 (9,2,1), L2 (5,2,1), L3 (9,2,9); kho (5,2,8); `AddFarmOrder([(4,1,4), (5,1,4), (6,1,4)])`; `EnqueueCrop` cho cả 3 ô. Hai mô phỏng cùng seed chạy 4000 nhịp, mã băm từng nhịp trùng. Cuối cùng: mọi plot `Idle`, `Produce == 6`, `Seeds == 10`, `Reserved == 0`.

9. **Biên dịch + `.meta`**: MCP `refresh_unity`, `read_console` không có lỗi.

10. **(Sửa sau Vướng mắc 2026-10-14)** `FarmTests.StandsOnPlotWhenNoNeighbor`: Planner tính sai — đúng là chưa `Done` ở nhịp **60**, `Done` ở nhịp **61** (giao + `Working` ở nhịp 1, xới 60 nhịp → 1 + 60). Chỉ sửa 2 số này trong test; không đổi code. Chạy lại Kiểm chứng đầy đủ.

11. **Commit** sau khi Kiểm chứng đạt: message `M1 sim bước 3: lệnh trồng cây, ô ruộng có hàng đợi vụ, một kho (brief 0003)`. Không push.

## Tiêu chí xong
- Mọi test ở bước 3, 7, 8 đạt; EditMode toàn bộ 0 fail. Số test = 118 + 1 (`FindPathToAnyPicksNearest`) + 11 (`FarmTests`) + 1 (`FarmOrderSameHashEveryTick`) = **131**.
- `SimulationTests.cs`, `SimulationTerrainTests.cs` không bị sửa (`git diff --stat` không có hai file này).
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
- Một số trong test (nhịp, ô, số lượng) không khớp sau khi đã kiểm cài đặt đúng từng quy tắc ở "Quyết định đã chốt" và bước 4–5. **Không sửa số trong test cho khớp code**; ghi số thực tế + trạng thái liên quan vào Vướng mắc.
- Test bước 1–2 hỏng mà không phải do vị trí kho ở bước 8.
- Brief mơ hồ hoặc mâu thuẫn với code thật. Cần quyết định ngoài "Quyết định đã chốt", hoặc cần API ngoài brief.
- Cùng một lỗi sửa 2 lần vẫn chưa qua. Phải sửa file ngoài "Phạm vi".

---
<!-- Từ đây trở xuống: EXECUTOR ghi. -->

## Kết quả
- Base commit: 7d602c3
- Bắt đầu: 2026-10-14 15:32

## Kết quả kiểm chứng
- `refresh_unity` + `read_console` → 0 lỗi biên dịch.
- `run_tests` EditMode toàn bộ → 131 total, 130 passed, **1 failed**: `FarmTests.StandsOnPlotWhenNoNeighbor` (FarmTests.cs:157, `Expected: Done But was: Assigned`).
- grep cấm → rỗng. Chưa chạy `check_design.py` / commit vì đang dừng.

## Vướng mắc
Code bước 1–8 đã viết xong, **chưa commit** (nằm trong working tree: Core/Sim/*, Tests/Core/Sim/*, sim_m1.json). 130/131 test đạt; chỉ 1 test lệch.

**Câu hỏi:** `StandsOnPlotWhenNoNeighbor` (bước 7) nói "nhịp 1: L1 `Working` ở (5,2,5). nhịp 59: xới chưa `Done`; nhịp 60: `Done`". Số thực tế: xới `Done` ở **nhịp 61**, chưa `Done` ở nhịp 60.
- Lý do (code đúng quy ước brief): L1 đứng sẵn tại chỗ → giao việc ở nhịp 1, `Working` ngay nhịp 1, công đoạn xới 60 nhịp → xong ở 1 + 60 = 61. Đúng như "Quy ước thời gian hiện có": "đã đứng đúng chỗ → Working ngay nhịp t, xong ở t + B" và test bước 1 `StageAtCurrentCellStartsImmediately` (xong ở nhịp 61).
- Đã kiểm lại: tạm đổi test thành "chưa Done ở nhịp 60, Done ở nhịp 61" → test đạt (đã khôi phục về số trong brief, không giữ sửa đổi). Test cùng loại `PrefersNeighborOverPlot` (tới ở nhịp 8, xong ở nhịp 68 = 8 + 60) khớp.
- Phương án: (A) sửa số trong brief/test thành 60 → 61 (chưa Done ở nhịp 60, Done ở nhịp 61). (B) giữ 60 và đổi code để làm bắt đầu sớm 1 nhịp — **không** nên, mâu thuẫn test bước 1.
- Đề xuất: **A**. Sau khi Planner xác nhận, Executor chỉ cần sửa 2 số trong `FarmTests.cs` (`RunTo(sim, 59)` → `RunTo(sim, 60)`), chạy lại, commit.

**Trả lời Planner (2026-10-14): chọn A.** Lỗi tính tay của Planner (quên cộng nhịp giao việc). Đã sửa số ở bước 7 và thêm bước 10. Các chỗ "làm ngay" khác trong brief đã dùng t + B, không đổi.

---
<!-- PLANNER ghi khi review. -->

## Review
