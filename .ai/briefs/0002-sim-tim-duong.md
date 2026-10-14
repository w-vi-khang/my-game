# Brief 0002 — Simulation bước 2: A\* đi bộ theo khung chiếm chỗ, tính lại khi đào/đặt

> Trạng thái: chờ làm
> Review: đầy đủ
> Model thực thi: Sonnet · medium
> Cập nhật: 2026-10-14

<!-- Planner (Opus) viết, Executor (Sonnet) làm theo ở PHIÊN KHÁC — executor không có lịch sử chat, không được tự quyết.
Trạng thái: chờ làm → đang làm → chờ review → đã duyệt; hoặc → cần Opus (executor dừng, hỏi ở mục Vướng mắc).
Review: nhẹ (đọc mục Kết quả) | đầy đủ (đọc cả diff) | user xem (có phần user tự kiểm bằng mắt/chơi thử) — có thể ghép "đầy đủ + user xem".
File được commit. Tự kiểm trước khi giao (AGENTS.md §10.3): executor có phải hỏi gì không? còn chỗ nào bỏ ngỏ 2 phương án? -->

## Mục tiêu
Thay cách "đi theo khoảng cách ô" của brief 0001 bằng tìm đường A\* trên `WorldData`. A\* kiểm đủ khung chiếm chỗ 3 chiều của loài và không cho đi vào nước. Khi đào/đặt ô: sinh vật đứng trên ô bị đào thì chạy ra ô bên cạnh hoặc rơi xuống; đường đang đi bị cắt thì tìm lại; không tìm được thì công đoạn chuyển sang đình trệ "không có đường".
Task contract: `.ai/tasks/m1-sim-buoc-2-tim-duong.md`. Quyết định: `design/decisions/0015-tim-duong-khung-va-dao-duoi-chan.md`.

## Bối cảnh cần đọc
- Code của brief 0001 (commit `5c17c4a`), trong `client/Assets/Game/Core/Sim/`:
  - `Simulation.cs:12` `public Simulation(SimConfig config)`; `:19` `AddCreature`; `:67` `Tick()`; `:95` `MovePhase`; `:116` `StepToward` (sẽ xoá); `:123` `OnArrive`; `:138` `AssignPhase`; `:180` `Assign`; `:199` `Unassign`.
  - `SimData.cs:27-32` enum `CreatureState`, `StageStatus`, `AssigneeKind`; `:34-60` `Creature` (constructor ở `:48`: `public Creature(int id, string speciesId, WorkAbility ability1, WorkAbility ability2, int mastery1, int mastery2, int speedPerTick, CellPos cell)`); `:66-85` `WorkStage`.
  - `SimConfig.cs:23-41` `SpeciesWork` (constructor 6 tham số ở `:33`); `:77-82` mặc định `species`; `:112` `Validate()`; `:148` kiểm `spt`.
  - `SimHash.cs:25` (dòng cuối phần creature: `MixInt(h, c.MoveProgress)`), `:44` (dòng cuối phần stage: `MixInt(h, g.Carried ? 1 : 0)`).
- World (không sửa): `client/Assets/Game/Core/WorldData.cs:12-19` (kích thước phải chia hết cho 16), `:27` `InBounds(x, y, z)`, `:31` `Index(x, y, z)`, `:77` `Get`, `:79` `Set`. `Cell.cs:4-14` enum `BlockMaterial` (`Empty = 0`, `Water = 5`, `Bedrock = 6`); `Cell.GetMaterial(ushort)`; `Cell.Make(BlockMaterial, int waterLevel = 0)`.
- `WorldEditor.cs:50` `public Func<int, int, int, bool> IsOccupied;`, `:53` `public event Action<int, int, int> CellChanged;`, `:55` constructor `(WorldData world, MaterialInventory inventory, int meshMargin, int seaLevel = -1)`, `:69` `TryDig`, `:87` `TryPlace` (cần ô có mặt kề một khối đặc; kiểm `IsOccupied`, trả `EditResult.Occupied`). `MaterialInventory.Add(BlockMaterial, int amount = 1)`.
- Test bước 1: `client/Assets/Game/Tests/Core/Sim/{SimulationTests,SimDeterminismTests,SimConfigTests}.cs`. Có 11 chỗ gọi `new Simulation(`.

## Quyết định đã chốt (không được đổi)
- (user, 0015) Mỗi loài có khung chiếm chỗ `sizeX × sizeZ × sizeY`; loài M1 đều 1 × 1 × 1. Không đi vào hoặc qua ô nước. Đào dưới chân sinh vật được phép; sinh vật chạy ra ô bên cạnh, không có ô nào thì rơi thẳng xuống.
- `Creature.Cell` = **neo** của khung: góc X nhỏ nhất, Z nhỏ nhất, Y = ô chân. Khung chiếm các ô `(Cell.X + dx, Cell.Y + dy, Cell.Z + dz)` với `0 ≤ dx < SizeX`, `0 ≤ dz < SizeZ`, `0 ≤ dy < SizeY`.
- **Ô cho thân** (`IsBodyFree`): trong map và vật liệu `Empty`. **Ô đỡ** (`IsSupport`): trong map và vật liệu khác `Empty` và khác `Water`.
- **Đứng được** (`CanStand(a, sx, sz, sy)`): với mọi cột của khung, ô `y = a.Y − 1` là ô đỡ, và các ô `a.Y … a.Y + sy − 1` là ô cho thân.
- **Bước được** (`CanStep(from, to, …)`): `|to.X − from.X| + |to.Z − from.Z| == 1` và `|to.Y − from.Y| ≤ 1` và `CanStand(to)`. Thêm điều kiện khoảng trống: leo (`to.Y == from.Y + 1`) cần mọi cột của khung ở `from` có ô `from.Y + sy` cho thân; xuống (`to.Y == from.Y − 1`) cần mọi cột của khung ở `to` có ô `to.Y + sy` cho thân. Ô xuất phát không cần đứng được.
- A\*: 4 hướng theo thứ tự **+X, −X, +Z, −Z**; mỗi hướng thử `dy` theo thứ tự **0, +1, −1**, lấy cái đầu tiên `CanStep`. Mỗi bước tốn 1. Heuristic `|dx| + |dz|` tới đích. Hàng đợi ưu tiên so theo `f`, rồi `h`, rồi số thứ tự lúc thêm vào (nhỏ trước). Chỉ dùng mảng và `List`, không `Dictionary`/`HashSet`/`PriorityQueue`.
- Đường đi = danh sách ô từ sau ô xuất phát tới đích (gồm đích). Xuất phát == đích → danh sách rỗng, trả `true`.
- Thứ tự trong `Tick()`: `Tick++` → (nếu địa hình bẩn) **địa hình** → **làm** → **đi** → **giao việc**.
- Sinh vật không chặn nhau. Vẫn chọn sinh vật theo điểm Manhattan ÷ thuần thục như bước 1, rồi mới chạy A\* cho con được chọn; không có đường thì loại con đó và chọn lại trong số còn lại.
- Đình trệ `NoPath` mở lại (về `Open`) mỗi khi địa hình đổi. Chạy ra / rơi xảy ra tức thì ở pha địa hình, không tốn nhịp.
- Không sửa `WorldEditor` và các file world. Mô phỏng nhận tin đổi ô qua `OnCellChanged`, test tự nối.

## Phạm vi
- Được tạo: `client/Assets/Game/Core/Sim/GridPathfinder.cs`, `client/Assets/Game/Tests/Core/Sim/{TestWorlds,GridPathfinderTests,SimulationTerrainTests}.cs` (+ `.meta`).
- Được sửa: `client/Assets/Game/Core/Sim/{SimConfig,SimData,Simulation,SimHash}.cs`, `client/Assets/Game/Tests/Core/Sim/{SimulationTests,SimDeterminismTests,SimConfigTests}.cs`, `design/data/sim_m1.json`, brief này (Kết quả, Kết quả kiểm chứng, Vướng mắc, Trạng thái).
- Không được sửa: mọi file khác, nhất là `client/Assets/Game/Core/*.cs` ngoài `Sim/`, asmdef, `design/` trừ `sim_m1.json`, `.ai/tasks/`.

## Các bước
1. **Config — khung chiếm chỗ.**
   - `SimConfig.cs` / `SpeciesWork`: thêm 3 field **sau** `moveSpeedMilliCellsPerSecond`: `public int sizeX = 1; public int sizeZ = 1; public int sizeY = 1;`. Giữ constructor 6 tham số (khung mặc định 1). Thêm constructor 9 tham số `(string id, string ability1, string ability2, int mastery1, int mastery2, int moveSpeedMilliCellsPerSecond, int sizeX, int sizeZ, int sizeY)`.
   - `Validate()`: thêm điều kiện mỗi `sizeX`, `sizeZ`, `sizeY` phải trong `[1, 4]`, thông báo `"Loài " + sp.id + ": khung phải trong [1, 4] mỗi chiều."`.
   - `design/data/sim_m1.json`: thêm `, "sizeX": 1, "sizeZ": 1, "sizeY": 1` vào cuối mỗi loài, sau `moveSpeedMilliCellsPerSecond`.
   - Xong khi: `ConfigMatchesCodeDefaults` vẫn đạt.

2. **Dữ liệu** (`SimData.cs`):
   - `StageStatus` thêm `Blocked = 3`. Thêm `public enum StallReason : byte { None = 0, NoPath = 1 }` (comment: "Lý do đình trệ — bước 4 thêm kho đầy, thiếu năng lực.").
   - `WorkStage`: thêm field `public StallReason Stall;`.
   - `Creature`: thêm `public readonly int SizeX, SizeZ, SizeY;` và `public readonly List<CellPos> Path = new List<CellPos>(); public int PathIndex;`. Constructor đổi thành `(int id, string speciesId, WorkAbility ability1, WorkAbility ability2, int mastery1, int mastery2, int speedPerTick, int sizeX, int sizeZ, int sizeY, CellPos cell)`. Thêm `public bool Contains(int x, int y, int z)`: ô nằm trong khung.
   - `CellPos`: thêm `public CellPos Offset(int dx, int dy, int dz) => new CellPos(X + dx, Y + dy, Z + dz);`.

3. **`GridPathfinder.cs`** (namespace `Game.Core.Sim`, `using System; using System.Collections.Generic;`), `public sealed class GridPathfinder`:
   - `public GridPathfinder(WorldData world)` (null → `ArgumentNullException`); giữ `world`, cấp mảng theo `world.Size.CellCount`: `int[] g`, `int[] parent`, `int[] seenStamp`, `int[] closedStamp`, và `int stamp` tăng mỗi lần tìm (tránh xoá mảng).
   - `public bool IsBodyFree(int x, int y, int z)`, `public bool IsSupport(int x, int y, int z)`, `public bool CanStand(CellPos a, int sx, int sz, int sy)`, `public bool CanStep(CellPos from, CellPos to, int sx, int sz, int sy)` — đúng định nghĩa ở "Quyết định đã chốt".
   - `public bool FindPath(CellPos start, CellPos goal, int sx, int sz, int sy, List<CellPos> result)`: `result.Clear()` trước. `start == goal` → `true`. `start` ngoài map hoặc `!CanStand(goal)` → `false`. Mỗi nút là chỉ số `world.Size.Index` của neo. Hàng đợi là min-heap tự viết trong `List` các phần tử `(int f, int h, int seq, int index)`, so theo "Quyết định đã chốt". Bỏ qua nút đã đóng khi lấy ra. Lấy ra đích thì dựng đường ngược qua `parent`, đảo lại, ghi vào `result`. Hết hàng đợi thì trả `false`.
   - Comment class: "A* đi bộ trên lưới ô theo khung chiếm chỗ (simulation-tech bước 2, 0015). 4 hướng, leo/xuống 1 ô, không vào nước. Tất định: thứ tự hướng và cách so trong hàng đợi cố định."

4. **`Simulation.cs`:**
   - Constructor đổi thành `public Simulation(SimConfig config, WorldData world)` (world null → `ArgumentNullException`). Thêm field `public readonly WorldData World;`, `readonly GridPathfinder pathfinder;`, `bool terrainDirty;`, và `readonly List<CellPos> tmpPath = new List<CellPos>();`.
   - `AddCreature`: truyền `sp.sizeX, sp.sizeZ, sp.sizeY`. Nếu `!pathfinder.CanStand(cell, …)` thì ném `ArgumentException("Sinh vật không đứng được tại " + cell)`.
   - `public bool IsOccupied(int x, int y, int z)`: có creature nào `Contains(x, y, z)` (duyệt theo id).
   - `public void OnCellChanged(int x, int y, int z) => terrainDirty = true;`
   - `Tick()`: `State.Tick++; if (terrainDirty) { terrainDirty = false; TerrainPhase(); } WorkPhase(); MovePhase(); AssignPhase();`
   - **TerrainPhase** (private):
     1. Mọi stage `Status == Blocked && Stall == NoPath` → `Status = Open; Stall = None;`.
     2. Duyệt creature theo id. Nếu `!CanStand(c.Cell)` thì gọi `Displace(c)`, sau đó: nếu `c.State == Working` thì `Unassign(c.Id)`; nếu `c.State == Moving` thì `Repath(c)`. Đánh dấu con này đã xử lý (biến cục bộ, không lưu).
     3. Duyệt creature theo id, chỉ con `Moving` chưa xử lý ở bước 2: kiểm đường còn lại (`prev = c.Cell`; với `i` từ `PathIndex` tới hết: `CanStep(prev, Path[i])`, `prev = Path[i]`). Có bước hỏng thì `Repath(c)`.
   - **Displace(c)** (private): với hướng `(1,0) (−1,0) (0,1) (0,−1)` (dx, dz), mỗi hướng `dy` theo thứ tự 0, +1, −1: `n = c.Cell.Offset(dx, dy, dz)`; nếu `CanStep(c.Cell, n)` thì `c.Cell = n; c.MoveProgress = 0; return;`. Không có ô nào thì rơi: khi `c.Cell.Y − 1 ≥ 0` và mọi cột khung ở `y = c.Cell.Y − 1` đều `IsBodyFree`, giảm `Y` đi 1. Rơi dừng trên ô đặc hoặc ô nước.
   - **Repath(c)** (private): `FindPath(c.Cell, c.MoveTarget, …, tmpPath)`. Được thì chép vào `c.Path`, `c.PathIndex = 0`, `c.MoveProgress = 0`. Không được thì `BlockNoPath(c)`.
   - **BlockNoPath(c)** (private): `s = Stages[c.StageId]; Unassign(c.Id); s.Status = StageStatus.Blocked; s.Stall = StallReason.NoPath;`.
   - **MovePhase**: thay vòng `StepToward` bằng: `c.MoveProgress += c.SpeedPerTick; while (c.MoveProgress >= 1000 && c.PathIndex < c.Path.Count) { c.Cell = c.Path[c.PathIndex++]; c.MoveProgress -= 1000; } if (c.PathIndex >= c.Path.Count) { c.MoveProgress = 0; c.Path.Clear(); c.PathIndex = 0; OnArrive(c); }`. Xoá `StepToward`.
   - **OnArrive** — nhánh vận chuyển chưa mang: `s.Carried = true;` rồi: nếu `c.Cell == s.Destination` thì `Working`. Ngược lại, nếu `FindPath(c.Cell, s.Destination, …, tmpPath)` được thì `c.MoveTarget = s.Destination`, chép đường, `PathIndex = 0` (vẫn `Moving`); không được thì `BlockNoPath(c)` (hàng rơi tại chỗ qua `Unassign`). Nhánh còn lại giữ nguyên.
   - **AssignPhase** — thay bước chọn ứng viên: lập danh sách `eligible` (Idle, đúng năng lực, theo id) kèm `score` như cũ. Nếu rỗng thì bỏ qua stage, giữ `Open`. Ngược lại lặp: tìm `best` = score nhỏ nhất trong `eligible`; `group` = các con có score bằng `best` (giữ thứ tự id); `pick = group.Count == 1 ? group[0] : group[State.Rng.NextInt(group.Count)]`. Nếu `pick.Cell == stage.Target` thì `Assign(pick, stage, null)` rồi dừng. Nếu `FindPath(pick.Cell, stage.Target, khung của pick, tmpPath)` được thì `Assign(pick, stage, tmpPath)` rồi dừng. Không được thì bỏ `pick` khỏi `eligible` và lặp tiếp. `eligible` hết mà chưa giao thì `stage.Status = Blocked; stage.Stall = NoPath;`.
   - **Assign(c, s, List<CellPos> path)**: như cũ. Nhánh `c.Cell != s.Target`: chép `path` vào `c.Path`, `c.PathIndex = 0`. Nhánh `c.Cell == s.Target`: `c.Path.Clear(); c.PathIndex = 0;` rồi `OnArrive` như cũ.
   - **Unassign**: thêm `c.Path.Clear(); c.PathIndex = 0; c.MoveTarget = c.Cell;`.
   - Summary class đổi đuôi thành: "A* đi bộ theo khung chiếm chỗ (bước 2, 0015)."
   - Mọi chỗ gọi `CanStand`/`CanStep`/`FindPath` cho creature dùng khung `c.SizeX, c.SizeZ, c.SizeY`.

5. **`SimHash.cs`**: sau `MixInt(h, c.MoveProgress)` (dòng 25) thêm `PathIndex`, `Path.Count`, rồi `X/Y/Z` của từng ô trong `Path`. Sau `Carried` (dòng 44) thêm `(int)g.Stall`.

6. **`Tests/Core/Sim/TestWorlds.cs`** (namespace `Game.Core.Tests`): `public static class TestWorlds { public static WorldData Flat(int groundTop) }` tạo `new WorldData(new WorldSize(16, 16, 16))`, đặt `y = 0` là `Bedrock` và `y = 1..groundTop` là `Dirt` trên toàn mặt bằng. Sinh vật đứng ở `y = groundTop + 1`.

7. **Sửa test bước 1** (`SimulationTests.cs`, `SimDeterminismTests.cs`, `SimConfigTests.cs` nếu có gọi): mọi `new Simulation(cfg)` → `new Simulation(cfg, TestWorlds.Flat(0))`. Mọi `CellPos` có `y = 0` → `y = 1`. **Không đổi số nhịp hay số khác.** Thêm vào `ValidateRejectsBadValues` trường hợp `species[0].sizeX = 0`.

8. **`GridPathfinderTests.cs`** — trừ khi ghi khác, world là `TestWorlds.Flat(0)` và đứng ở y = 1. "Tường tại x = k cao 1..m" nghĩa là đặt `Stone` tại `(k, y, z)` với mọi `z` 0..15 và `y` 1..m.
   - `FlatPathIsManhattan`: (1,1,1) → (9,1,6), khung 1×1×1: `true`, 13 ô, ô cuối là đích, mọi cặp liên tiếp (bắt đầu từ start) đều `CanStep`.
   - `ClimbsAndDropsOneCell`: tường x = 2 cao 1..1. (1,1,1) → (3,1,1): `true`, đường = [(2,2,1), (3,1,1)].
   - `CannotClimbTwo`: tường x = 2 cao 1..2. (1,1,1) → (3,1,1): `false`.
   - `WaterIsNotWalkable`: `Cell.Make(BlockMaterial.Water, 7)` tại (2,1,z) với mọi z. (1,1,1) → (3,1,1): `false`.
   - `TallFrameNeedsHeadroom`: `Stone` tại (5,2,z) với mọi z, ô (5,1,z) để trống. (1,1,1) → (9,1,1): khung 1×1×1 → `true`, 8 ô; khung 1×1×2 → `false`.
   - `WideFrameNeedsWideGap`: tường x = 5 cao 1..2, chừa khe z = 7 (để trống (5,1,7), (5,2,7)). Khung 1×1×1 (1,1,7) → (9,1,7): `true`. Khung 2×2×1 (1,1,6) → (8,1,6): `false`. Mở thêm khe z = 8 (để trống (5,1,8), (5,2,8)): khung 2×2×1 → `true`.
   - `SameQuerySamePath`: world `Flat(0)` có tường x = 5 cao 1..1; tìm (1,1,1) → (9,1,9) hai lần → hai danh sách giống hệt.

9. **`SimulationTerrainTests.cs`** — dùng `WorldEditor editor = new WorldEditor(world, inv, 1)` với `inv = new MaterialInventory()`, nối `editor.CellChanged += sim.OnCellChanged; editor.IsOccupied = sim.IsOccupied;`. Config mặc định trừ khi ghi khác.
   - World "hào" `Trench(bool bridge)`: `TestWorlds.Flat(2)` (đứng ở y = 3), xoá (đặt `Empty`) mọi ô (5,1,z), (5,2,z) với z 0..15. Nếu `bridge`, đặt `Dirt` tại (5,2,3), giữ (5,1,3) rỗng.
   - `DigCutsPathThenPlaceRestores`: `Trench(true)`. L1 tại (1,3,3), Till tại (9,3,3). Chạy 10 nhịp, creature ở (2,3,3) và `Moving`. `editor.TryDig(5,2,3)` == `Ok`. Nhịp 11: stage `Blocked`, `Stall == NoPath`, creature `Idle`, `StageId == -1`. `editor.TryPlace(5,2,3, BlockMaterial.Dirt)` == `Ok`. Nhịp 12: stage `Assigned`, creature `Moving`.
   - `NoPathAtAssignBlocks`: `Trench(false)`. L1 tại (1,3,3), Till tại (9,3,3). Sau nhịp 1: stage `Blocked` / `NoPath`, creature `Idle`.
   - `TriesNextCandidateWhenNoPath`: `Trench(false)`. A = L1 tại (4,3,3) (score 400000), B = L1 tại (8,3,9) (score 600000), Till tại (8,3,3). Sau nhịp 1: B nhận việc, A `Idle`.
   - `DigUnderCreatureStepsAside`: `Flat(1)` (đứng ở y = 2). L1 `Idle` tại (5,2,5). `TryDig(5,1,5)`. Sau 1 nhịp: creature ở (6,2,5).
   - `FallsWhenNoNeighbor`: `Flat(0)`, `Dirt` tại (5,1,5), (5,2,5), (5,3,5). L1 tại (5,4,5). `TryDig(5,3,5)`. Sau 1 nhịp: creature ở (5,3,5).
   - `DisplacedWorkerKeepsProgress`: `Flat(1)`. L1 tại (5,2,5), Till tại (5,2,5). Chạy 11 nhịp → `WorkDone == 10000`. `TryDig(5,1,5)`. Sau nhịp 12: creature ở (6,2,5) và `Idle`; stage `Blocked` / `NoPath`; `WorkDone == 10000`.
   - `OccupiedCoversFrame`: config thêm loài `new SpeciesWork("Tall", "Till", "Sow", 100, 100, 1500, 1, 1, 2)`, world `Flat(0)`. Creature Tall tại (3,1,3). `IsOccupied(3,1,3)` và `(3,2,3)` là `true`, `(3,3,3)` là `false`. `inv.Add(BlockMaterial.Dirt)`; `editor.TryPlace(3,1,3, Dirt)` == `EditResult.Occupied` (ô (3,1,3) kề đá đáy nên qua được luật "kề khối đặc" trước khi tới kiểm chiếm chỗ).
   - `AddCreatureRejectsFloating`: `Flat(0)`, `AddCreature("L1", (3,5,3))` → `ArgumentException`.

10. **Biên dịch + `.meta`**: MCP `refresh_unity`, `read_console` không có lỗi.

11. **Commit** sau khi Kiểm chứng đạt: message `M1 sim bước 2: A* đi bộ theo khung chiếm chỗ, tính lại khi đào/đặt (brief 0002)`. Không push.

## Tiêu chí xong
- Mọi test ở bước 7–9 đạt; toàn bộ EditMode 0 fail. Số test = 102 + 7 (`GridPathfinderTests`) + 8 (`SimulationTerrainTests`) = **117**.
- Test bước 1 không đổi số nhịp nào (chỉ đổi world, `y` và constructor).
- `grep -rnE "float|double|System\.Random|UnityEngine|DateTime|Dictionary|HashSet|PriorityQueue" client/Assets/Game/Core/Sim/` → rỗng.
- `python3 tools/check_design.py` exit 0; `git status` sạch sau commit, chỉ file trong Phạm vi.

## Kiểm chứng
1. MCP `refresh_unity` → `read_console` không có lỗi biên dịch.
2. MCP `run_tests` EditMode toàn bộ, ghi dòng tổng kết. Nếu Editor tắt thì chạy `python3 tools/unity_test.py EditMode`. Không chạy PlayMode.
3. Lệnh grep ở Tiêu chí xong → rỗng.
4. `python3 tools/check_design.py` → exit 0.
5. `git status --short` trước commit: chỉ file trong Phạm vi.
6. Ghi giờ bắt đầu/kết thúc vào Kết quả.

## Dừng và chuyển lại Opus khi
- Một số trong test (nhịp, ô, độ dài đường) không khớp sau khi đã kiểm cài đặt đúng từng quy tắc ở "Quyết định đã chốt" và bước 4. **Không sửa số trong test cho khớp code**; ghi số thực tế vào Vướng mắc.
- Brief mơ hồ hoặc mâu thuẫn với code thật. Cần quyết định ngoài "Quyết định đã chốt".
- Cùng một lỗi sửa 2 lần vẫn chưa qua. Phải sửa file ngoài "Phạm vi".

---
<!-- Từ đây trở xuống: EXECUTOR ghi. -->

## Kết quả

## Kết quả kiểm chứng

## Vướng mắc

---
<!-- PLANNER ghi khi review. -->

## Review
