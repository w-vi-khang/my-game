# Brief 0001 — Simulation bước 1: lõi mô phỏng nhịp cố định, ghép việc, đi theo khoảng cách

> Trạng thái: đã duyệt
> Review: đầy đủ
> Model thực thi: Sonnet · medium
> Cập nhật: 2026-10-14

<!-- Planner (Opus) viết, Executor (Sonnet) làm theo ở PHIÊN KHÁC — executor không có lịch sử chat, không được tự quyết.
Trạng thái: chờ làm → đang làm → chờ review → đã duyệt; hoặc → cần Opus (executor dừng, hỏi ở mục Vướng mắc).
Review: nhẹ (đọc mục Kết quả) | đầy đủ (đọc cả diff) | user xem (có phần user tự kiểm bằng mắt/chơi thử) — có thể ghép "đầy đủ + user xem".
File được commit. Tự kiểm trước khi giao (AGENTS.md §10.3): executor có phải hỏi gì không? còn chỗ nào bỏ ngỏ 2 phương án? -->

## Mục tiêu
Tạo lõi mô phỏng nông trại M1 bằng C# thuần trong `Game.Core`. Lõi gồm nhịp cố định 10/giây, chỉ dùng số nguyên, bộ sinh số ngẫu nhiên PCG32 có seed riêng, và dữ liệu sinh vật / lệnh / công đoạn / kho. Mỗi nhịp, bộ quản lý nhiệm vụ giao công đoạn cho sinh vật phù hợp; sinh vật đi tới nơi làm theo khoảng cách ô rồi làm việc. Tất cả kiểm bằng test EditMode, trong đó có test tất định: cùng đầu vào → cùng mã băm mỗi nhịp.
Task contract: `.ai/tasks/m1-sim-buoc-1-loi-mo-phong.md`. Quyết định phạm vi: `design/decisions/0014-ghep-viec-o-sim-buoc-1.md`.

## Bối cảnh cần đọc
- `client/Assets/Game/Core/Game.Core.asmdef:13` — `"noEngineReferences": true`: code trong Core **không** được `using UnityEngine`.
- `client/Assets/Game/Tests/Core/Game.Core.Tests.asmdef:23` — `"noEngineReferences": false`: test Core **được** dùng `UnityEngine.JsonUtility`, `Application.dataPath`.
- `client/Assets/Game/Core/TerrainParams.cs:9-10, 47` — mẫu class config: `[Serializable] public class TerrainParams`, field `public` chữ thường khớp tên JSON, có giá trị mặc định, `public void Validate()` ném `ArgumentException` (thông báo tiếng Việt).
- `client/Assets/Game/Core/WorldHash.cs:9-10, 30-37` — FNV-1a 64 bit: `const ulong OffsetBasis = 14695981039346656037UL;`, `const ulong Prime = 1099511628211UL;`, `static ulong MixInt(ulong h, int v)` trộn 4 byte thấp → cao. `SimHash` dùng đúng cách này.
- `client/Assets/Game/Tests/World/ConfigTests.cs:11, 24-37` — mẫu test config: đường dẫn `Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "design", "data", "<file>.json"))`; `ConfigMatchesCodeDefaults` so `JsonUtility.ToJson(new X())` với `JsonUtility.ToJson(fromFile)`.
- `client/Assets/Game/Tests/Core/WorldEditorTests.cs:1-7` — mẫu test: `using NUnit.Framework;`, namespace `Game.Core.Tests`, `[Test]`.
- Comment trong code viết tiếng Việt, ngắn, như các file Core hiện có.

## Quyết định đã chốt (không được đổi)
- Phạm vi: ghép việc + đi theo khoảng cách ô làm ngay ở bước 1; **chưa** có A\*, địa hình, đình trệ, chuỗi công đoạn trồng cây, cây lớn, kho nhận hàng, hệ số thuần thục vào thời gian làm (user 2026-10-14, 0014).
- Người chơi nhận việc + độ ưu tiên do người chơi đặt là M2; bước này chỉ có trường `AssigneeKind.Player` và `Priority` (user 2026-10-14, 0014).
- Số tạm trong `sim_m1.json` đúng như bước 2 bên dưới (user 2026-10-14).
- Bộ sinh số: PCG32 (pcg-c-basic, [pcg-random.org](https://www.pcg-random.org/download.html)). Dãy chuẩn seed 42, dãy 54 = `0xa15c02b7 0x7b47f409 0xba1d3330 0x83d2f293 0xbfa4784b 0xcbed606e` (Planner đã chạy lại bằng Python 2026-10-14, khớp). Mô phỏng dùng `new SimRng(config.simSeed, 1)`.
- Đơn vị: nhịp `long`; công cần = `BaseTicks × 1000`; mỗi nhịp làm cộng **1000**; tốc độ = phần nghìn ô / nhịp; tiến độ ‰ = `WorkDone × 1000 / WorkRequired`.
- Đi: mỗi lần đủ 1000 thì bước 1 ô, ưu tiên trục **X → Z → Y**. Khoảng cách = Manhattan 3 trục. Tới nơi thì bỏ phần dư (`MoveProgress = 0`).
- Thứ tự pha trong `Tick()`: `Tick++` → **làm** → **đi** → **giao việc**. Mọi vòng lặp theo id tăng dần.
- Nhịp nước lan 8/giây (0013) giữ nguyên, không đụng.

## Phạm vi
- Được tạo: `client/Assets/Game/Core/Sim/` (các file `.cs` ở bước 1–6 + `.meta` do Unity sinh, gồm `Sim.meta` của thư mục), `client/Assets/Game/Tests/Core/Sim/` (các file test + `.meta`), `design/data/sim_m1.json`.
- Được sửa: brief này (mục Kết quả, Kết quả kiểm chứng, Vướng mắc, dòng Trạng thái).
- Không được sửa: mọi file khác. Cụ thể: `Game.Core.asmdef`, file asmdef test, `WorldEditor.cs`, `WorldHash.cs`, `TerrainParams.cs`, `design/` (trừ `design/data/sim_m1.json`), `.ai/tasks/`, `client/Assets/Game/Client/**`. Không tạo bản sao config trong `Resources`.

## Các bước
1. **`client/Assets/Game/Core/Sim/SimRng.cs`** — chép đúng:
   ```csharp
   using System;

   namespace Game.Core.Sim
   {
       /// <summary>PCG32 (pcg-c-basic, pcg-random.org) — bộ sinh số ngẫu nhiên riêng của mô phỏng, chỉ phép toán số nguyên.</summary>
       public sealed class SimRng
       {
           public ulong State;
           public ulong Inc;

           public SimRng(ulong initState, ulong initSeq)
           {
               State = 0UL;
               Inc = (initSeq << 1) | 1UL;
               NextUInt();
               State = unchecked(State + initState);
               NextUInt();
           }

           public uint NextUInt()
           {
               ulong old = State;
               State = unchecked(old * 6364136223846793005UL + Inc);
               uint xorshifted = (uint)(((old >> 18) ^ old) >> 27);
               int rot = (int)(old >> 59);
               return (xorshifted >> rot) | (xorshifted << ((-rot) & 31));
           }

           /// <summary>Số nguyên đều trong [0, bound), không lệch (loại bỏ phần dư như pcg32_boundedrand_r).</summary>
           public int NextInt(int bound)
           {
               if (bound <= 0) throw new ArgumentOutOfRangeException(nameof(bound));
               uint b = (uint)bound;
               uint threshold = unchecked(0u - b) % b;
               while (true)
               {
                   uint r = NextUInt();
                   if (r >= threshold) return (int)(r % b);
               }
           }
       }
   }
   ```
   Xong khi: biên dịch được.

2. **`design/data/sim_m1.json`** — chép nguyên văn:
   ```json
   {
       "configVersion": 1,
       "simSeed": 20261014,
       "ticksPerSecond": 10,
       "stageTimes": [
           { "ability": "Till", "baseTimeMs": 6000 },
           { "ability": "Sow", "baseTimeMs": 3000 },
           { "ability": "Water", "baseTimeMs": 3000 },
           { "ability": "Harvest", "baseTimeMs": 4000 },
           { "ability": "Haul", "baseTimeMs": 2000 }
       ],
       "species": [
           { "id": "L1", "ability1": "Till", "ability2": "Sow", "mastery1": 100, "mastery2": 100, "moveSpeedMilliCellsPerSecond": 1500 },
           { "id": "L2", "ability1": "Water", "ability2": "Harvest", "mastery1": 100, "mastery2": 100, "moveSpeedMilliCellsPerSecond": 1500 },
           { "id": "L3", "ability1": "Haul", "ability2": "Harvest", "mastery1": 100, "mastery2": 100, "moveSpeedMilliCellsPerSecond": 2000 }
       ],
       "crop": { "growTimeMs": 120000, "seedsPerHarvest": 1, "producePerHarvest": 1 },
       "storage": { "capacity": 50, "startSeeds": 10 }
   }
   ```

3. **`client/Assets/Game/Core/Sim/SimConfig.cs`** — `using System;`, namespace `Game.Core.Sim`. Summary của class: "Cấu hình mô phỏng M1. Nguồn chuẩn: design/data/sim_m1.json (đọc bằng JsonUtility — tên trường phải khớp JSON). Số là số tạm (user 2026-10-14), chỉnh khi chơi thử."
   - `public enum WorkAbility : byte { Till = 0, Sow = 1, Water = 2, Harvest = 3, Haul = 4 }` (xới, gieo, tưới, thu hoạch, vận chuyển). Đặt trong file này.
   - `[Serializable] public class StageTime { public string ability; public int baseTimeMs; }` — có constructor không tham số và constructor `(string ability, int baseTimeMs)`.
   - `[Serializable] public class SpeciesWork { public string id; public string ability1; public string ability2; public int mastery1; public int mastery2; public int moveSpeedMilliCellsPerSecond; }` — có constructor không tham số và constructor đủ 6 tham số theo đúng thứ tự trên.
   - `[Serializable] public class CropConfig { public int growTimeMs = 120000; public int seedsPerHarvest = 1; public int producePerHarvest = 1; }`
   - `[Serializable] public class StorageConfig { public int capacity = 50; public int startSeeds = 10; }`
   - `[Serializable] public class SimConfig`, các field theo **đúng thứ tự** của JSON, có mặc định bằng file: `public int configVersion = 1; public uint simSeed = 20261014; public int ticksPerSecond = 10; public StageTime[] stageTimes = { ...5 phần tử như JSON... }; public SpeciesWork[] species = { ...3 phần tử như JSON... }; public CropConfig crop = new CropConfig(); public StorageConfig storage = new StorageConfig();`
   - Hàm của `SimConfig`:
     - `public static WorkAbility ParseAbility(string s)`: `Enum.TryParse(s, false, out WorkAbility a) && Enum.IsDefined(typeof(WorkAbility), a)`, nếu không được thì ném `ArgumentException("Năng lực không hợp lệ: " + s)`. Chuỗi số như "3" phải bị từ chối, nên kiểm thêm `s` khác rỗng và ký tự đầu là chữ cái.
     - `public int MsToTicks(int ms) => ms * ticksPerSecond / 1000;`
     - `public int BaseTicks(WorkAbility a)`: tìm trong `stageTimes` phần tử có `ParseAbility(ability) == a`, trả `MsToTicks(baseTimeMs)`.
     - `public SpeciesWork FindSpecies(string id)`: tìm theo `id` (so sánh `string.Equals` ordinal), không có thì ném `ArgumentException("Không có loài: " + id)`.
     - `public int SpeedPerTick(SpeciesWork s) => s.moveSpeedMilliCellsPerSecond / ticksPerSecond;`
     - `public void Validate()` ném `ArgumentException` (thông báo tiếng Việt, mỗi điều kiện một câu) khi: `ticksPerSecond <= 0` hoặc `1000 % ticksPerSecond != 0`; `stageTimes` không chứa **đúng một lần** mỗi giá trị của `WorkAbility` (5 phần tử); `baseTimeMs <= 0` hoặc `baseTimeMs % (1000 / ticksPerSecond) != 0`; `species` rỗng; `id` rỗng/null hoặc trùng; `ability1 == ability2`; năng lực không parse được; `mastery1 < 1` hoặc `mastery2 < 1`; `moveSpeedMilliCellsPerSecond % ticksPerSecond != 0` hoặc `SpeedPerTick` ngoài `[1, 1000]`; `crop.growTimeMs <= 0` hoặc không chia hết cho `1000 / ticksPerSecond`; `storage.capacity < 1` hoặc `storage.startSeeds` ngoài `[0, capacity]`.

4. **`client/Assets/Game/Core/Sim/SimData.cs`** — kiểu dữ liệu (namespace `Game.Core.Sim`):
   - `public readonly struct CellPos : IEquatable<CellPos>`: `public readonly int X, Y, Z;` constructor `(int x, int y, int z)`; `Equals`, `GetHashCode`, `==`, `!=`, `ToString()` dạng `"(x, y, z)"`; `public static int Manhattan(CellPos a, CellPos b)` = tổng `Math.Abs` 3 trục.
   - `public enum CreatureState : byte { Idle = 0, Moving = 1, Working = 2 }`
   - `public enum StageStatus : byte { Open = 0, Assigned = 1, Done = 2 }`
   - `public enum AssigneeKind : byte { None = 0, Creature = 1, Player = 2 }` — comment: "Player: người chơi nhận việc qua tab nhiệm vụ — M2 (0014); M1 chỉ có trường."
   - `public sealed class Creature`: `public readonly int Id; public readonly string SpeciesId; public readonly WorkAbility Ability1, Ability2; public readonly int Mastery1, Mastery2; public readonly int SpeedPerTick;` cùng các field có thể sửa: `public CellPos Cell; public CreatureState State; public int StageId = -1; public CellPos MoveTarget; public int MoveProgress;`. Constructor `(int id, string speciesId, WorkAbility ability1, WorkAbility ability2, int mastery1, int mastery2, int speedPerTick, CellPos cell)`. Hàm `public bool HasAbility(WorkAbility a)` và `public int MasteryFor(WorkAbility a)` (trả 0 khi không có năng lực đó).
   - `public sealed class WorkStage`: `public readonly int Id, OrderId; public readonly WorkAbility Ability; public readonly bool HasDestination; public readonly CellPos Destination; public readonly int BaseTicks; public readonly int Priority;` cùng các field có thể sửa: `public CellPos Target; public long WorkDone; public StageStatus Status; public AssigneeKind AssigneeKind; public int AssigneeId = -1; public bool Carried;`. Property `public long WorkRequired => (long)BaseTicks * 1000;` và `public int ProgressPermille => (int)(WorkDone * 1000 / WorkRequired);`. Constructor `(int id, int orderId, WorkAbility ability, CellPos target, bool hasDestination, CellPos destination, int baseTicks, int priority)`. Comment: "Target = nơi bắt đầu (vận chuyển: nơi lấy hàng, hoặc nơi hàng đang nằm). Carried = hàng đang được mang."
   - `public sealed class WorkOrder`: `public readonly int Id; public readonly List<CellPos> Area; public readonly List<int> StageIds = new List<int>();` Constructor `(int id, List<CellPos> area)`.
   - `public sealed class Storage`: `public readonly int Id; public readonly CellPos Cell; public readonly int Capacity; public int Seeds; public int Produce;` Constructor `(int id, CellPos cell, int capacity, int seeds)`.
   - `public sealed class SimState`: `public long Tick; public readonly SimRng Rng; public readonly List<Creature> Creatures = new(); public readonly List<WorkStage> Stages = new(); public readonly List<WorkOrder> Orders = new(); public readonly List<Storage> Storages = new();` Constructor `(SimRng rng)`. Quy ước: id = vị trí trong list.

5. **`client/Assets/Game/Core/Sim/Simulation.cs`** — `public sealed class Simulation`:
   - `public readonly SimConfig Config; public readonly SimState State;`
   - `public Simulation(SimConfig config)`: gọi `config.Validate()`, rồi `State = new SimState(new SimRng(config.simSeed, 1UL))`.
   - `public int AddCreature(string speciesId, CellPos cell)`: lấy `FindSpecies`, `ParseAbility` 2 năng lực, `SpeedPerTick`; tạo `Creature` với Id = `Creatures.Count`, `State = Idle`, `MoveTarget = cell`; trả Id.
   - `public int AddOrder(IReadOnlyList<CellPos> area)`: Id = `Orders.Count`, chép `area` vào list mới.
   - `public int AddStage(int orderId, WorkAbility ability, CellPos target, int priority = 0)`: `ability == Haul` thì ném `ArgumentException` (vận chuyển dùng `AddHaulStage`). Tạo stage `HasDestination = false`, `BaseTicks = Config.BaseTicks(ability)`, `Status = Open`, `AssigneeKind = None`; thêm Id vào `Orders[orderId].StageIds`; trả Id.
   - `public int AddHaulStage(int orderId, CellPos pickup, CellPos destination, int priority = 0)`: như trên với `Ability = Haul`, `Target = pickup`, `HasDestination = true`, `Destination = destination`.
   - `public int AddStorage(CellPos cell)`: `Capacity = Config.storage.capacity`, `Seeds = Config.storage.startSeeds`, `Produce = 0`.
   - `public void Tick()`: `State.Tick++; WorkPhase(); MovePhase(); AssignPhase();`
   - **WorkPhase** (private): duyệt creature theo id; chỉ xử lý `State == Working`. `s = Stages[c.StageId]; s.WorkDone += 1000;` Nếu `s.WorkDone >= s.WorkRequired` thì: `s.WorkDone = s.WorkRequired; s.Status = Done; s.Carried = false;` (giữ `AssigneeKind`/`AssigneeId` làm lịch sử), rồi `c.State = Idle; c.StageId = -1;`.
   - **MovePhase** (private): duyệt theo id; chỉ xử lý `State == Moving`. `c.MoveProgress += c.SpeedPerTick;` rồi `while (c.MoveProgress >= 1000 && c.Cell != c.MoveTarget) { c.Cell = StepToward(c.Cell, c.MoveTarget); c.MoveProgress -= 1000; }`. Nếu `c.Cell == c.MoveTarget` thì `c.MoveProgress = 0; OnArrive(c);`.
   - `static CellPos StepToward(CellPos from, CellPos to)`: nếu X khác thì X tiến 1 về phía `to`; nếu không, Z khác thì Z tiến 1; nếu không, Y tiến 1.
   - **OnArrive(c)** (private): `s = Stages[c.StageId]`. Nếu `s.HasDestination && !s.Carried` thì `s.Carried = true;` và: nếu `c.Cell == s.Destination` thì `c.State = Working`, ngược lại `c.MoveTarget = s.Destination` (vẫn `Moving`, chặng sau bắt đầu từ nhịp kế). Ngược lại (`!HasDestination`, hoặc đã mang tới nơi giao) thì `c.State = Working`.
   - **AssignPhase** (private):
     1. Lập danh sách stage có `Status == Open && AssigneeKind == None`, sắp theo `Priority` **giảm dần**, cùng Priority thì `Id` tăng dần (dùng `List.Sort` với comparison đủ 2 khoá).
     2. Với từng stage: duyệt creature theo id, chỉ lấy `State == Idle && HasAbility(stage.Ability)`. Tính `score = (long)CellPos.Manhattan(c.Cell, stage.Target) * 100000 / c.MasteryFor(stage.Ability)`. Giữ danh sách ứng viên có score nhỏ nhất (gặp score nhỏ hơn thì xoá danh sách cũ; bằng thì thêm vào).
     3. Không có ứng viên thì bỏ qua stage (đình trệ để bước 4). Có 1 ứng viên thì chọn luôn, **không** gọi Rng. Có ≥ 2 thì chọn `candidates[State.Rng.NextInt(candidates.Count)]`.
     4. `Assign(c, s)`.
   - **Assign(c, s)** (private): `s.Status = Assigned; s.AssigneeKind = Creature; s.AssigneeId = c.Id; c.StageId = s.Id; c.MoveProgress = 0;`. Sau đó: nếu `c.Cell == s.Target` thì gọi `OnArrive(c)` (đứng sẵn tại chỗ: vào làm ngay, hoặc vận chuyển thì nhận hàng luôn), nhưng trước khi gọi phải đặt `c.State = Moving` để `OnArrive` có thể giữ `Moving` khi còn chặng sau. Nếu `c.Cell != s.Target` thì `c.State = Moving; c.MoveTarget = s.Target;`.
   - `public void Unassign(int creatureId)`: `c = Creatures[creatureId]`; nếu `c.StageId < 0` thì return. `s = Stages[c.StageId]`. Nếu `s.Carried` thì `s.Target = c.Cell; s.Carried = false;` (hàng rơi tại chỗ). Tiếp theo: `s.Status = Open; s.AssigneeKind = None; s.AssigneeId = -1;` — **giữ `WorkDone`**. Cuối cùng: `c.State = Idle; c.StageId = -1; c.MoveProgress = 0;`.
   - Summary class: "Mô phỏng nông trại M1 — nhịp cố định, số nguyên, tất định (simulation-tech bước 1, 0014). Đi theo khoảng cách ô, chưa có A* (bước 2)."

6. **`client/Assets/Game/Core/Sim/SimHash.cs`** — `public static class SimHash { public static ulong Compute(SimState s) }`, FNV-1a 64 bit, cùng hằng số với `WorldHash`. Tự viết `MixInt` (giống `WorldHash.cs:30-37`) và `MixLong` (8 byte thấp → cao); `bool` trộn thành int 0/1; `ulong` trộn như long. Thứ tự trộn:
   - `Tick`, `Rng.State`, `Rng.Inc`;
   - `Creatures.Count`, rồi từng creature: `Id, Cell.X, Cell.Y, Cell.Z, (int)State, StageId, MoveTarget.X, MoveTarget.Y, MoveTarget.Z, MoveProgress`;
   - `Stages.Count`, rồi từng stage: `Id, OrderId, (int)Ability, Target.X/Y/Z, HasDestination, Destination.X/Y/Z, BaseTicks, WorkDone, Priority, (int)Status, (int)AssigneeKind, AssigneeId, Carried`;
   - `Orders.Count`, rồi từng order: `Id, Area.Count`, từng ô `X/Y/Z`, `StageIds.Count`, từng id;
   - `Storages.Count`, rồi từng kho: `Id, Cell.X/Y/Z, Capacity, Seeds, Produce`.

7. **Test `client/Assets/Game/Tests/Core/Sim/`** (namespace `Game.Core.Tests`, NUnit). Các tên test dưới đây là bắt buộc; số trong test là số tính trước từ config mặc định (L1/L2: 150 phần nghìn ô/nhịp, L3: 200; xới 60 nhịp, vận chuyển 20 nhịp).
   - `SimRngTests.cs`
     - `MatchesPcgReferenceSequence`: `new SimRng(42, 54)` cho 6 số đầu đúng dãy chuẩn trong "Quyết định đã chốt".
     - `SameSeedSameSequence`: 2 bộ sinh `(20261014, 1)` cho cùng 1000 số.
     - `NextIntStaysInRange`: 10000 lần `NextInt(7)` đều nằm trong `[0, 7)`, và cả 7 giá trị đều xuất hiện.
   - `SimConfigTests.cs` (dùng `JsonUtility`, `Application.dataPath`, đường dẫn tới `design/data/sim_m1.json` như `ConfigTests.cs:11`)
     - `ConfigParsesAndValidates`: đọc file, `Assert.DoesNotThrow(cfg.Validate)`; kiểm `BaseTicks(Till) == 60`, `BaseTicks(Haul) == 20`, `SpeedPerTick(FindSpecies("L3")) == 200`.
     - `ConfigMatchesCodeDefaults`: `JsonUtility.ToJson(new SimConfig()) == JsonUtility.ToJson(fromFile)`.
     - `ValidateRejectsBadValues`: mỗi trường hợp dùng một `new SimConfig()` sửa đúng một chỗ rồi `Assert.Throws<ArgumentException>(cfg.Validate)`. Các trường hợp: thiếu Haul trong `stageTimes` (mảng 4 phần tử); `baseTimeMs = 6050`; loài trùng id; `ability1 == ability2`; năng lực `"Fly"`; năng lực `"3"`; `mastery1 = 0`; tốc độ `5` (SpeedPerTick = 0); `storage.startSeeds = 51`.
   - `SimulationTests.cs` (dùng `new SimConfig()` mặc định, trừ khi ghi khác)
     - `StageDoneAfterTravelPlusWork`: L1 ở (0,0,0); `AddStage(order, Till, (5,0,3))` (d = 8 → đi `ceil(8000/150) = 54` nhịp). Sau 114 lần `Tick()` stage còn `Assigned`; sau lần thứ 115 stage `Done`, creature `Idle` tại (5,0,3), `ProgressPermille == 1000`. Công thức: xong ở nhịp `1 + 54 + 60`.
     - `StageAtCurrentCellStartsImmediately`: L1 ở (2,0,2), Till tại (2,0,2). Sau nhịp 1 creature `Working`, `WorkDone == 0`; xong đúng ở nhịp 61.
     - `HaulTakesTwoLegs`: L3 ở (0,0,0); `AddHaulStage(order, (3,0,0), (3,0,4))` (chặng 1: 15 nhịp, chặng 2: 20 nhịp). Sau nhịp 16: `Carried == true`, creature ở (3,0,0) và đang `Moving`. Xong đúng ở nhịp `1 + 15 + 20 + 20 = 56`, sau khi xong `Carried == false`.
     - `OnlyMatchingAbility`: chỉ có L1, stage Water. Sau 10 nhịp stage vẫn `Open`, creature vẫn `Idle`.
     - `PlayerAssignedStageIsSkipped`: L1 + stage Till, đặt `stage.AssigneeKind = AssigneeKind.Player` trước khi Tick. Sau 10 nhịp stage vẫn `Open`, creature `Idle`.
     - `HigherPriorityFirst`: L1 ở (0,0,0); stage A Till (1,0,0) priority 0 (tạo trước), stage B Till (9,0,0) priority 5. Sau nhịp 1: `creature.StageId == B`, A vẫn `Open`.
     - `ScoreIsDistanceOverMastery`: config = `new SimConfig()`, thêm loài thứ 4 `new SpeciesWork("L1b", "Till", "Sow", 200, 100, 1500)` vào cuối `species`. A = L1 ở (4,0,0) (score 4000), B = L1b ở (0,0,6) (score 3000), stage Till tại (0,0,0). Sau nhịp 1: B nhận việc, A vẫn `Idle`.
     - `TiesBrokenBySeed`: 4 con L1 ở (2,0,0), (0,0,2), (4,0,2), (2,0,4); Till tại (2,0,2) (cả 4 đều d = 2). Với mỗi seed 1..20 (`cfg.simSeed = seed`): chạy 1 nhịp, ghi `AssigneeId`. Kiểm: chạy lại cùng seed cho cùng `AssigneeId`; 20 seed cho ít nhất 2 giá trị `AssigneeId` khác nhau.
     - `UnassignKeepsProgress`: L1 ở (0,0,0), Till tại (0,0,0). Sau 31 nhịp `WorkDone == 30000` (`ProgressPermille == 500`). `Unassign` → stage `Open`, `WorkDone == 30000`, creature `Idle`. Stage xong đúng ở nhịp 62 (nhịp 32 nhận lại việc, nhịp 33..62 làm 30 nhịp).
     - `UnassignWhileCarryingDropsItem`: L3 ở (0,0,0); haul từ (0,0,0) tới (10,0,0). Sau 11 nhịp creature ở (2,0,0), `Carried == true`. `Unassign` → `stage.Target == (2,0,0)`, `Carried == false`, `Status == Open`. Stage xong đúng ở nhịp 72 (nhịp 12 nhận lại + nhặt hàng, 40 nhịp đi 8 ô, 20 nhịp dỡ).
   - `SimDeterminismTests.cs`
     - Hàm dựng kịch bản `static Simulation BuildFarm(uint seed)`: config mặc định với `simSeed = seed`. Creature: L1 (0,0,0), L1 (6,0,0), L2 (0,0,6), L2 (6,0,6), L3 (3,0,3), L3 (3,0,9). Kho tại (10,0,10). Một order với vùng là 9 ô (x 2..4, z 2..4, y 0). Stage: Till cho từng ô trong vùng (duyệt x ngoài, z trong); Water tại 4 góc (2,0,2), (2,0,4), (4,0,2), (4,0,4); haul (2,0,2)→(10,0,10) và (4,0,4)→(10,0,10).
     - `SameInputSameHashEveryTick`: 2 lần `BuildFarm(20261014)`, mỗi lần chạy 2000 nhịp, ghi `SimHash.Compute` sau mỗi nhịp; 2 danh sách giống hệt. Sau 2000 nhịp mọi stage đều `Done`.
     - `HashChangesWhenStateChanges`: 2 kịch bản giống nhau có cùng mã băm; `Tick()` một bên thì mã băm khác.

8. **Unity sinh `.meta`**: sau khi tạo file, cho Unity import (Editor đang mở: dùng MCP `refresh_unity`, hoặc để Editor tự refresh) để có `.meta` cho thư mục `Core/Sim`, `Tests/Core/Sim` và từng file `.cs`. Xem console (MCP `read_console`) phải không có lỗi biên dịch.

9. **Commit** (sau khi Kiểm chứng đạt): các file code, test, `.meta`, `design/data/sim_m1.json` và brief. Message: `M1 sim bước 1: lõi mô phỏng nhịp cố định, ghép việc, đi theo khoảng cách (brief 0001)`. Không push.

## Tiêu chí xong
- Tất cả test trong bước 7 đạt. Toàn bộ EditMode đạt: 84 test cũ + test mới, 0 fail.
- `grep -rnE "float|double|System\.Random|UnityEngine|DateTime|Dictionary|HashSet" client/Assets/Game/Core/Sim/` không ra dòng nào.
- `python3 tools/check_design.py` exit 0.
- `git status` sạch sau commit. Không có file ngoài "Phạm vi" bị đổi.

## Kiểm chứng
1. Biên dịch: MCP `refresh_unity`, rồi `read_console` không có lỗi.
2. Test EditMode qua MCP `run_tests` (mode EditMode, toàn bộ), ghi dòng tổng kết pass/fail. Nếu Editor đang tắt thì chạy `python3 tools/unity_test.py EditMode` thay thế. Không cần PlayMode (không đổi code client), nên không có bước dọn `EditorSettings.asset`.
3. `grep -rnE "float|double|System\.Random|UnityEngine|DateTime|Dictionary|HashSet" client/Assets/Game/Core/Sim/` → rỗng.
4. `python3 tools/check_design.py` → exit 0.
5. `git status --short` trước commit: chỉ có file trong "Phạm vi".
6. Ghi thời gian bắt đầu/kết thúc vào mục Kết quả (Planner cộng vào roadmap).

## Dừng và chuyển lại Opus khi
- Brief mơ hồ hoặc mâu thuẫn với code thật, hoặc một số trong test (nhịp xong) không khớp sau khi đã kiểm cài đặt đúng từng dòng của bước 5. **Không sửa số trong test cho khớp code**; ghi số thực tế vào Vướng mắc.
- Cần một quyết định thuộc AGENTS.md §3.4 hoặc ngoài "Quyết định đã chốt".
- Cùng một lỗi sửa 2 lần vẫn chưa qua.
- Phải sửa file ngoài "Phạm vi", hoặc cần API Unity không có trong brief.

---
<!-- Từ đây trở xuống: EXECUTOR ghi. -->

## Kết quả
<!-- Base commit (trước khi làm) · commit của brief · file đã đổi · ghi chú lệch so với brief (nếu có, kèm lý do). -->
- Base commit: cd49972
- Bắt đầu: 2026-10-14 11:05 · Kết thúc: 2026-10-14 11:08 (đồng hồ máy; phần lớn thời gian là ghi file)
- Commit code: 5c17c4a — file mới: client/Assets/Game/Core/Sim/{SimRng,SimConfig,SimData,Simulation,SimHash}.cs, client/Assets/Game/Tests/Core/Sim/{SimRngTests,SimConfigTests,SimulationTests,SimDeterminismTests}.cs (+ .meta), design/data/sim_m1.json
- Lệch brief: không có.

## Kết quả kiểm chứng
- MCP `refresh_unity` + `read_console` (error) → 0 lỗi biên dịch.
- MCP `run_tests` EditMode → 102 total, 102 passed, 0 failed (84 cũ + 18 mới).
- `grep -rnE "float|double|System\.Random|UnityEngine|DateTime|Dictionary|HashSet" client/Assets/Game/Core/Sim/` → rỗng.
- `python3 tools/check_design.py` → exit 0.
- `git status --short` trước commit: chỉ file trong Phạm vi (+ brief).

## Vướng mắc
<!-- Khi trạng thái = cần Opus: câu hỏi cụ thể + các phương án (kèm đề xuất). -->

---
<!-- PLANNER ghi khi review. -->

## Review
<!-- Kết luận: đã duyệt | cần sửa (thêm bước sửa vào "Các bước", trạng thái về chờ làm). Điểm đã đối chiếu. -->
**Đã duyệt** (Planner, 2026-10-14, mức đầy đủ).
- `git show --stat 5c17c4a`: 21 file, đều trong Phạm vi (Core/Sim, Tests/Core/Sim, `.meta`, `design/data/sim_m1.json`).
- `Simulation.cs`: thứ tự pha làm → đi → giao việc; `StepToward` X → Z → Y; điểm `d × 100000 / thuần thục`; chỉ gọi Rng khi ≥ 2 ứng viên; `Assign` đứng sẵn tại chỗ thì gọi `OnArrive`; `Unassign` giữ `WorkDone`, hàng đang mang rơi tại ô hiện tại — khớp bước 5.
- `SimConfig.ParseAbility` từ chối chuỗi số; `Validate` đủ các điều kiện của bước 3. `SimHash` trộn đúng thứ tự bước 6.
- Test: đủ 18 tên, giữ nguyên số tính trước (115, 61, 16/56, 62, 72; 2000 nhịp mọi stage Done). Kết quả EditMode 102/102 do Executor ghi — không chạy lại vì đầy đủ, khớp số test.
- Ghi chú nhỏ, không cần sửa: khi nhận việc tại chỗ (không đi), `MoveTarget` giữ giá trị cũ — không ảnh hưởng hành vi; bước 2 (A\*) sẽ thay phần đi lại.
- Đã cập nhật: simulation-tech hàng bước 1, roadmap "Theo dõi M1", task contract.
