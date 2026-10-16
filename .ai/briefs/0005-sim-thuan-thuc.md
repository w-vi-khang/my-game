# Brief 0005 — Simulation bước 5: thuần thục vào thời gian công đoạn (bảng mức → tốc độ làm mỗi nhịp)

> Trạng thái: đã duyệt
> Review: đầy đủ
> Model thực thi: Sonnet · medium
> Cập nhật: 2026-10-16 (xong)

<!-- Planner (Opus) viết, Executor (Sonnet) làm theo ở PHIÊN KHÁC — executor không có lịch sử chat, không được tự quyết.
Trạng thái: chờ làm → đang làm → chờ review → đã duyệt; hoặc → cần Opus (executor dừng, hỏi ở mục Vướng mắc).
Review: nhẹ (đọc mục Kết quả) | đầy đủ (đọc cả diff) | user xem (có phần user tự kiểm bằng mắt/chơi thử) — có thể ghép "đầy đủ + user xem".
Model thực thi: BẮT BUỘC, đúng dạng `<Model> · <effort>` — Model: Opus | Sonnet | Haiku | Fable; effort: low | medium | high | xhigh | max (chọn theo AGENTS.md §10.2).
Executor kiểm tra model + effort của phiên TRƯỚC khi làm (scripts/brief_check.py); lệch hoặc không xác định được → cảnh báo user và DỪNG.
File được commit. Tự kiểm trước khi giao (AGENTS.md §10.3): executor có phải hỏi gì không? còn chỗ nào bỏ ngỏ 2 phương án? -->

## Mục tiêu
Thời gian làm một công đoạn phụ thuộc **độ thuần thục** của sinh vật đang làm:
- tra **bảng mức thuần thục** (ngưỡng → hệ số tốc độ, phần nghìn) trong config;
- mỗi nhịp làm việc, `WorkDone` tăng đúng hệ số đó (thay cho hằng số 1000).

Task contract: `.ai/tasks/m1-sim-buoc-5-thuan-thuc.md`. Quyết định: `design/decisions/0018-thuan-thuc-bang-muc.md`.

## Bối cảnh cần đọc
Code ở commit `6249e95`.
- `client/Assets/Game/Core/Sim/SimConfig.cs`:
  - `:22-53` class `SpeciesWork` (mẫu cho class `[Serializable]` có constructor rỗng + constructor đủ trường);
  - `:109-114` field `species`; `:115` field `crop`;
  - `:149` `public int SpeedPerTick(SpeciesWork s) => s.moveSpeedMilliCellsPerSecond / ticksPerSecond;`
  - `:151` `public void Validate()`; `:170-191` kiểm `species` (`:182-183`: `mastery1`, `mastery2` phải `>= 1`); `:193` dòng đầu kiểm `crop`.
- `client/Assets/Game/Core/Sim/SimData.cs`:
  - `:105` `public int MasteryFor(WorkAbility a) => a == Ability1 ? Mastery1 : a == Ability2 ? Mastery2 : 0;`
  - `:134` `public long WorkRequired => (long)BaseTicks * 1000;`
- `client/Assets/Game/Core/Sim/Simulation.cs`:
  - `:411-430` `WorkPhase()`; `:419` `s.WorkDone += 1000;` (dòng duy nhất phải đổi trong file này);
  - `:609-613` `AssignPhase` xếp công đoạn mở theo `Priority` giảm dần, rồi `Id` tăng dần;
  - `:647` điểm ghép việc `Manhattan × 100000 / MasteryFor` — **giữ nguyên**;
  - `:702` `public void Unassign(int creatureId)` (giữ `WorkDone`, công đoạn về `Open`).
- `design/data/sim_m1.json` — mảng `"species"` dòng `:12-16`.
- Test hiện có: `client/Assets/Game/Tests/Core/Sim/`, EditMode toàn project **138**.
  - `SimConfigTests.cs`: `:15` `ConfigParsesAndValidates`; `:26` `ConfigMatchesCodeDefaults` (so `JsonUtility.ToJson` của `new SimConfig()` với file JSON); `:33` `ValidateRejectsBadValues` (dòng cuối hàm `:85`).
  - `SimulationTests.cs`: `:9` helper `Run(sim, ticks)`; `:14` `NewOrder(sim)`; `:17` `StageDoneAfterTravelPlusWork`; `:34` `StageAtCurrentCellStartsImmediately`; `:50` `HaulTakesTwoLegs`; `:109` `ScoreIsDistanceOverMastery` (mẫu tạo loài riêng: `cfg.species = …`); `:152` `UnassignKeepsProgress`.
  - `TestWorlds.Flat(0)`: 16 × 16 × 16, sinh vật đứng ở y = 1. `TestWorlds.HomeAll(sim)`: gắn nhà cho mọi sinh vật, gọi sau `AddCreature` cuối cùng và trước `Tick()` đầu tiên. Nhà đặt ở góc (14, ·, 14), không chắn các ô dùng trong brief này.
- **Quy ước thời gian** (đã kiểm bằng test bước 1–4):
  - "Nhịp n" = trạng thái **sau** lần `Tick()` thứ n. Thứ tự trong một nhịp: làm → cây → đi → giao việc.
  - Giao việc ở nhịp t, đường dài n ô, tốc độ đi v (phần nghìn ô/nhịp; tốc độ 1500 → 150, 2000 → 200) → tới nơi ở nhịp t + ⌈1000·n / v⌉. Nhịp tới nơi chưa cộng tiến độ; cộng từ nhịp sau.
  - Giao việc mà đã đứng đúng chỗ → `Working` ngay nhịp t, `WorkDone == 0` ở nhịp t, cộng từ nhịp t + 1.
  - Vận chuyển: tới nơi lấy hàng → mang hàng đi tiếp (chặng 2) → làm ở nơi giao.
  - Nhịp gốc B: xới 60, gieo 30, tưới 30, thu hoạch 40, vận chuyển 20.
- **Công thức mới** (0018): hệ số h = `WorkSpeedPermille(thuần thục)`. Số nhịp làm = ⌈B × 1000 ÷ h⌉. Vậy "làm ngay" ở nhịp t → xong ở t + ⌈B × 1000 ÷ h⌉; đi + làm → xong ở (nhịp tới nơi) + ⌈B × 1000 ÷ h⌉.

## Quyết định đã chốt (không được đổi)
- (0018, user) Bảng mức theo **ngưỡng giá trị**: dùng dòng có `minMastery` **lớn nhất** mà `≤` thuần thục.
- (0018, user) Áp theo **tốc độ mỗi nhịp**: `WorkDone += h` của sinh vật đang làm, `WorkRequired` giữ `BaseTicks × 1000`. Đổi sinh vật giữa chừng → giữ `WorkDone`, phần còn lại theo h của con mới. Không cần code riêng cho việc này.
- (0018, user) Thuần thục **không** đổi tốc độ di chuyển. Sức chở theo thuần thục vận chuyển **không** làm (M2).
- (0018) Bảng tạm, theo đúng thứ tự: `(0, 500)`, `(50, 750)`, `(100, 1000)`, `(200, 1250)`, `(400, 1500)`. L1–L3 giữ thuần thục 100.
- (0018) Kiểm bảng trong `Validate()`: không null/rỗng; `masteryLevels[0].minMastery <= 1`; `minMastery` tăng **ngặt**; mỗi `speedPermille` trong `[1, 10000]`; `speedPermille` **không giảm**.
- Điểm ghép việc (`Simulation.cs:647`) giữ nguyên. `SimHash.cs` không đổi (bảng là config, không phải trạng thái).
- Test bước 1–4 **không đổi dòng nào**. Chỉ được thêm vào `SimConfigTests.cs` và tạo `MasteryTests.cs`.

## Phạm vi
- Được tạo: `client/Assets/Game/Tests/Core/Sim/MasteryTests.cs` (+ `.meta` do Unity sinh).
- Được sửa:
  - `client/Assets/Game/Core/Sim/SimConfig.cs`, `client/Assets/Game/Core/Sim/Simulation.cs` (chỉ dòng `:419`);
  - `client/Assets/Game/Tests/Core/Sim/SimConfigTests.cs`;
  - `design/data/sim_m1.json`;
  - brief này (Trạng thái, Kết quả, Kết quả test, Vướng mắc).
- Không được sửa: mọi file khác, nhất là `SimData.cs`, `SimHash.cs`, `GridPathfinder.cs`, các file test khác, asmdef, `design/` (trừ `sim_m1.json`), `.ai/tasks/`.

## Các bước
1. **Config** (`SimConfig.cs`):
   - Sau class `SpeciesWork` (sau `:53`) thêm:
     ```csharp
     /// <summary>Một dòng của bảng mức thuần thục (0018): thuần thục >= minMastery → tốc độ làm speedPermille (1000 = thời gian gốc).</summary>
     [Serializable]
     public class MasteryLevel
     {
         public int minMastery;
         public int speedPermille;

         public MasteryLevel() { }
         public MasteryLevel(int minMastery, int speedPermille)
         {
             this.minMastery = minMastery;
             this.speedPermille = speedPermille;
         }
     }
     ```
   - Trong `SimConfig`, ngay sau field `species` (trước `crop`):
     ```csharp
     public MasteryLevel[] masteryLevels =
     {
         new MasteryLevel(0, 500),
         new MasteryLevel(50, 750),
         new MasteryLevel(100, 1000),
         new MasteryLevel(200, 1250),
         new MasteryLevel(400, 1500),
     };
     ```
   - Sau `SpeedPerTick` (`:149`) thêm:
     ```csharp
     /// <summary>Tốc độ làm (phần nghìn mỗi nhịp) theo bảng mức thuần thục: dòng có minMastery lớn nhất <= mastery (0018).</summary>
     public int WorkSpeedPermille(int mastery)
     {
         int v = masteryLevels[0].speedPermille;
         for (int i = 1; i < masteryLevels.Length; i++)
         {
             if (masteryLevels[i].minMastery > mastery) break;
             v = masteryLevels[i].speedPermille;
         }
         return v;
     }
     ```
   - `Validate()`: ngay sau vòng kiểm `species` (sau dấu `}` đóng vòng ở `:191`, trước kiểm `crop`) thêm:
     ```csharp
     if (masteryLevels == null || masteryLevels.Length == 0)
         throw new ArgumentException("masteryLevels không được rỗng.");
     if (masteryLevels[0].minMastery > 1)
         throw new ArgumentException("masteryLevels[0].minMastery phải <= 1.");
     for (int i = 0; i < masteryLevels.Length; i++)
     {
         var m = masteryLevels[i];
         if (m.speedPermille < 1 || m.speedPermille > 10000)
             throw new ArgumentException("masteryLevels[" + i + "].speedPermille phải trong [1, 10000].");
         if (i > 0 && m.minMastery <= masteryLevels[i - 1].minMastery)
             throw new ArgumentException("masteryLevels: minMastery phải tăng dần (dòng " + i + ").");
         if (i > 0 && m.speedPermille < masteryLevels[i - 1].speedPermille)
             throw new ArgumentException("masteryLevels: speedPermille không được giảm (dòng " + i + ").");
     }
     ```
   - Xong khi: biên dịch được.

2. **JSON** (`design/data/sim_m1.json`): ngay sau dòng `:16` `    ],` (đóng mảng `"species"`, đã có dấu phẩy) và trước dòng `"crop"` chèn:
   ```json
       "masteryLevels": [
           { "minMastery": 0, "speedPermille": 500 },
           { "minMastery": 50, "speedPermille": 750 },
           { "minMastery": 100, "speedPermille": 1000 },
           { "minMastery": 200, "speedPermille": 1250 },
           { "minMastery": 400, "speedPermille": 1500 }
       ],
   ```
   Xong khi: `python3 -m json.tool design/data/sim_m1.json > /dev/null` exit 0.

3. **Làm việc** (`Simulation.cs:419`): đổi `s.WorkDone += 1000;` thành
   ```csharp
   s.WorkDone += Config.WorkSpeedPermille(c.MasteryFor(s.Ability));
   ```
   Không đổi dòng nào khác. Xong khi: biên dịch được.

4. **`SimConfigTests.cs`**:
   - Trong `ConfigParsesAndValidates`, sau dòng `:22` thêm `Assert.AreEqual(1000, cfg.WorkSpeedPermille(100));`.
   - Cuối `ValidateRejectsBadValues` (sau `:85`) thêm 6 khối, mỗi khối `c = new SimConfig();` + một thay đổi + `Assert.Throws<ArgumentException>(c.Validate, "<thông điệp>");`:

     | Thay đổi | Thông điệp |
     |---|---|
     | `c.masteryLevels = new MasteryLevel[0];` | `"bảng mức rỗng"` |
     | `c.masteryLevels[0].minMastery = 2;` | `"ngưỡng đầu > 1"` |
     | `c.masteryLevels[2].minMastery = 50;` | `"ngưỡng không tăng"` |
     | `c.masteryLevels[0].speedPermille = 0;` | `"hệ số 0"` |
     | `c.masteryLevels[4].speedPermille = 10001;` | `"hệ số > 10000"` |
     | `c.masteryLevels[3].speedPermille = 900;` | `"hệ số giảm"` |
   - Thêm test mới:
     ```csharp
     [TestCase(1, 500)]
     [TestCase(49, 500)]
     [TestCase(50, 750)]
     [TestCase(99, 750)]
     [TestCase(100, 1000)]
     [TestCase(199, 1000)]
     [TestCase(200, 1250)]
     [TestCase(399, 1250)]
     [TestCase(400, 1500)]
     [TestCase(100000, 1500)]
     public void WorkSpeedFollowsTable(int mastery, int expected)
     {
         Assert.AreEqual(expected, new SimConfig().WorkSpeedPermille(mastery));
     }
     ```
   - Xong khi: `ConfigMatchesCodeDefaults` đạt (JSON khớp code).

5. **`MasteryTests.cs`** (mới; namespace `Game.Core.Tests`; `using System.Collections.Generic; using Game.Core.Sim; using NUnit.Framework;`; class `public class MasteryTests`).
   - Helper:
     ```csharp
     static void Run(Simulation sim, int ticks)
     {
         for (int i = 0; i < ticks; i++) sim.Tick();
     }

     static int NewOrder(Simulation sim) => sim.AddOrder(new List<CellPos>());

     static Simulation Build(params SpeciesWork[] species)
     {
         var cfg = new SimConfig();
         cfg.species = species;
         return new Simulation(cfg, TestWorlds.Flat(0));
     }
     ```
   - **`WorkTicksFollowTable`** — làm ngay ở nhịp 1, xong ở 1 + ⌈B × 1000 ÷ h⌉:
     ```csharp
     [TestCase(WorkAbility.Till, 1, 121)]       // h 500: 60000/500 = 120
     [TestCase(WorkAbility.Till, 49, 121)]
     [TestCase(WorkAbility.Till, 50, 81)]       // h 750: 80
     [TestCase(WorkAbility.Till, 100, 61)]      // h 1000: 60 (như bước 1)
     [TestCase(WorkAbility.Till, 200, 49)]      // h 1250: 48
     [TestCase(WorkAbility.Till, 399, 49)]
     [TestCase(WorkAbility.Till, 400, 41)]      // h 1500: 40
     [TestCase(WorkAbility.Till, 100000, 41)]
     [TestCase(WorkAbility.Harvest, 50, 55)]    // năng lực 2, h 750: ⌈40000/750⌉ = ⌈53,3⌉ = 54
     [TestCase(WorkAbility.Harvest, 200, 33)]   // h 1250: 32
     [TestCase(WorkAbility.Harvest, 400, 28)]   // h 1500: ⌈26,7⌉ = 27
     public void WorkTicksFollowTable(WorkAbility ability, int mastery, int doneTick)
     ```
     - Loài: `ability == Till` → `new SpeciesWork("M", "Till", "Harvest", mastery, 100, 1500)`; ngược lại `new SpeciesWork("M", "Till", "Harvest", 100, mastery, 1500)`.
     - `c = AddCreature("M", (2,1,2))`; `s = AddStage(NewOrder(sim), ability, (2,1,2))`; `HomeAll`.
     - `h = sim.Config.WorkSpeedPermille(mastery)` (`Simulation.Config` là field public, `Simulation.cs:9`).
     - nhịp 1: sinh vật `c` `Working`, `WorkDone == 0`.
     - nhịp 2: `WorkDone == h`.
     - nhịp `doneTick − 1`: `Status != Done`.
     - nhịp `doneTick`: `Status == Done`, `WorkDone == WorkRequired`, sinh vật `Idle`.
   - **`HaulWorkFollowsMastery`** — vận chuyển (năng lực 1), tốc độ đi 2000 (200/nhịp), như `HaulTakesTwoLegs`:
     ```csharp
     [TestCase(50, 63)]    // tới nơi giao ở nhịp 36; h 750: ⌈20000/750⌉ = 27
     [TestCase(400, 50)]   // h 1500: ⌈13,3⌉ = 14
     public void HaulWorkFollowsMastery(int mastery, int doneTick)
     ```
     - Loài `new SpeciesWork("H", "Haul", "Harvest", mastery, 100, 2000)`.
     - `c = AddCreature("H", (0,1,0))`; `s = AddHaulStage(NewOrder(sim), (3,1,0), (3,1,4))`; `HomeAll`.
     - nhịp 16: `Carried == true`, sinh vật ở (3,1,0).
     - nhịp 36: sinh vật ở (3,1,4), `WorkDone == 0`.
     - nhịp `doneTick − 1`: `Status != Done`. nhịp `doneTick`: `Done`, `Carried == false`.
   - **`TravelPlusWorkUsesMastery`** — đi 8 ô rồi làm, như `StageDoneAfterTravelPlusWork`:
     - Loài `new SpeciesWork("M", "Till", "Sow", 200, 100, 1500)`.
     - `c = AddCreature("M", (0,1,0))`; `s = AddStage(NewOrder(sim), Till, (5,1,3))`; `HomeAll`.
     - nhịp 55 (1 + ⌈8000/150⌉): sinh vật ở (5,1,3), `WorkDone == 0`.
     - nhịp 56: `WorkDone == 1250`.
     - nhịp 102: `Status == Assigned`. nhịp 103 (55 + 48): `Status == Done`.
   - **`SwapMidStageKeepsProgressUsesNewSpeed`** — con nhanh làm dở, con chậm làm tiếp:
     - Loài `F = new SpeciesWork("F", "Till", "Sow", 400, 100, 1500)` (Till h 1500), `S = new SpeciesWork("S", "Till", "Harvest", 1, 100, 1500)` (Till h 500). `Build(F, S)`.
     - `f = AddCreature("F", (2,1,2))`; `sl = AddCreature("S", (6,1,2))`; `o = NewOrder(sim)`; `till = AddStage(o, Till, (2,1,2))`; `HomeAll`.
     - nhịp 1: `till.AssigneeId == f`, F `Working` (điểm F = 0 < điểm S).
     - nhịp 21: `till.WorkDone == 30000` (20 nhịp × 1500).
     - Rồi `sim.Unassign(f)`; `sow = AddStage(o, Sow, (2,1,6), 10)` (ưu tiên cao → F nhận Sow trước, chỉ còn S cho Till).
     - nhịp 22: `till.Status == Assigned`, `till.AssigneeId == sl`, `till.WorkDone == 30000`; `sow.AssigneeId == f`.
     - nhịp 49 (22 + ⌈4000/150⌉): S ở (2,1,2), `till.WorkDone == 30000`.
     - nhịp 50: `till.WorkDone == 30500`.
     - nhịp 108: `till.Status != Done`, `till.WorkDone == 59500` (30000 + 59 × 500).
     - nhịp 109 (49 + 30000/500): `till.Status == Done`.
   - Truy cập stage/sinh vật qua `sim.State.Stages[id]`, `sim.State.Creatures[id]` như các test cũ.

6. **Biên dịch + `.meta`**: MCP `refresh_unity`, `read_console` không có lỗi.

7. **Commit** sau khi mọi test đạt: message `M1 sim bước 5: thuần thục vào thời gian công đoạn — bảng mức (brief 0005)`. Không push. Commit kết quả brief riêng: `brief 0005: kết quả`.

## Tiêu chí xong
- EditMode toàn bộ 0 fail. Số test = 138 + 25 = **163**: mỗi `TestCase` tính 1 → `WorkSpeedFollowsTable` 10 + `WorkTicksFollowTable` 11 + `HaulWorkFollowsMastery` 2 + `TravelPlusWorkUsesMastery` 1 + `SwapMidStageKeepsProgressUsesNewSpeed` 1. `ValidateRejectsBadValues` vẫn là 1 test. Nếu công cụ chạy test đếm khác (vd gộp `TestCase`), ghi số thật và cách đếm vào Kết quả test; tiêu chí chính là 0 fail và đủ tên test.
- `git diff 6249e95 -- client/Assets/Game/Tests/` chỉ có: các dòng thêm ở `SimConfigTests.cs` (bước 4) và file `MasteryTests.cs` mới.
- `git diff 6249e95 -- client/Assets/Game/Core/Sim/Simulation.cs` đúng 1 dòng đổi (`:419`).
- `grep -rnE "float|double|System\.Random|UnityEngine|DateTime|Dictionary|HashSet|PriorityQueue" client/Assets/Game/Core/Sim/` → rỗng.
- `python3 tools/check_design.py` exit 0; `git status` sạch sau commit.

## Test sau khi xong

### Phần cần test
- **Tra bảng** (bước 1) · `SimConfigTests.WorkSpeedFollowsTable` 10 ca · đúng hệ số ở mọi ngưỡng biên.
- **Kiểm bảng khi nạp** (bước 1) · 6 khối mới trong `SimConfigTests.ValidateRejectsBadValues` · mỗi khối ném `ArgumentException`.
- **JSON khớp code** (bước 2) · `SimConfigTests.ConfigMatchesCodeDefaults`, `ConfigParsesAndValidates` (thêm assert `WorkSpeedPermille(100) == 1000`) · đạt.
- **Thời gian làm theo thuần thục, năng lực 1 và 2, làm tròn lên** (bước 3) · `MasteryTests.WorkTicksFollowTable` 11 ca · đúng nhịp xong, không xong sớm 1 nhịp.
- **Vận chuyển dùng thuần thục, đi giữ tốc độ cũ** (bước 3) · `MasteryTests.HaulWorkFollowsMastery` 2 ca · nhịp tới nơi giao 36 như cũ, nhịp xong đúng.
- **Đi + làm** (bước 3) · `MasteryTests.TravelPlusWorkUsesMastery` · tới nơi 55, xong 103.
- **Đổi sinh vật giữa chừng** (bước 3) · `MasteryTests.SwapMidStageKeepsProgressUsesNewSpeed` · giữ 30000, phần còn lại 500/nhịp, xong 109.
- **Regression bước 1–4** (thuần thục 100 → 1000‰): toàn bộ test cũ, nhất là `SimulationTests` (`StageDoneAfterTravelPlusWork`, `UnassignKeepsProgress`, `ScoreIsDistanceOverMastery`), `FarmTests`, `HomeTests`, `SimDeterminismTests` · đạt, không sửa dòng nào.
- **Không dùng số thực / API cấm trong lõi** · lệnh grep ở Tiêu chí xong · rỗng.

### Lệnh executor chạy
1. `python3 -m json.tool design/data/sim_m1.json > /dev/null` → exit 0.
2. MCP `refresh_unity` → `read_console` (Error) không có lỗi biên dịch.
3. MCP `run_tests` EditMode toàn bộ; ghi dòng tổng kết (tổng / đạt / fail). Editor tắt thì dùng `python3 tools/unity_test.py EditMode`. Không chạy PlayMode (không cần bước dọn `EditorSettings.asset`).
4. `grep -rnE "float|double|System\.Random|UnityEngine|DateTime|Dictionary|HashSet|PriorityQueue" client/Assets/Game/Core/Sim/` → rỗng.
5. `python3 tools/check_design.py` → exit 0.
6. `git status --short` + `git diff --stat` trước commit: chỉ file trong Phạm vi.
7. Ghi giờ bắt đầu/kết thúc (đồng hồ máy) vào Kết quả.

### User tự test
Không.

## Dừng và chuyển lại Opus khi
- Một số trong test (nhịp, `WorkDone`, ô) không khớp sau khi đã kiểm cài đặt đúng bước 1–3. **Không sửa số trong test cho khớp code.** Ghi số thực tế + trạng thái liên quan vào Vướng mắc.
- Test cũ nào hỏng.
- Brief mơ hồ hoặc mâu thuẫn với code thật. Cần quyết định ngoài "Quyết định đã chốt", hoặc cần API ngoài brief.
- Cùng một lỗi sửa 2 lần vẫn chưa qua. Phải sửa file ngoài "Phạm vi".

---
<!-- Từ đây trở xuống: EXECUTOR ghi. -->

## Kết quả
- Base commit: 2636e37
- Chạy bằng: Sonnet · medium
- Giờ thực tế: bắt đầu 21:32, kết thúc 21:34 (đồng hồ máy; phần lớn thời gian là chạy test, Editor tắt)
- Commit: f86a6dc
- File đổi: SimConfig.cs, Simulation.cs (1 dòng :419), SimConfigTests.cs, MasteryTests.cs (+.meta), design/data/sim_m1.json
- Không gặp vướng mắc; không sửa số nào trong test.

## Kết quả test
- Tra bảng · đạt · `WorkSpeedFollowsTable` 10 ca.
- Kiểm bảng khi nạp · đạt · 6 khối mới trong `ValidateRejectsBadValues`.
- JSON khớp code · đạt · `ConfigMatchesCodeDefaults`, `ConfigParsesAndValidates`.
- Thời gian làm theo thuần thục · đạt · `WorkTicksFollowTable` 11 ca.
- Vận chuyển · đạt · `HaulWorkFollowsMastery` 2 ca.
- Đi + làm · đạt · `TravelPlusWorkUsesMastery`.
- Đổi sinh vật giữa chừng · đạt · `SwapMidStageKeepsProgressUsesNewSpeed`.
- Regression bước 1–4 · đạt · toàn bộ test cũ không sửa.
- Grep API cấm trong Core/Sim · rỗng (grep exit 1).
- Lệnh: json.tool OK; Editor tắt (MCP không thấy instance) → `python3 tools/unity_test.py EditMode`: 163 test, đạt 163, lỗi 0, bỏ qua 0 (đúng 163 mong đợi); `check_design.py` exit 0; `git diff` Simulation.cs đúng 1 dòng; `git status` chỉ file trong Phạm vi.

## Vướng mắc

---
<!-- PLANNER ghi khi review. -->

## Review
**Đã duyệt** (Planner, 2026-10-16, mức đầy đủ).
- Chạy bằng `Sonnet · medium` = Model thực thi. Mọi dòng ở "Phần cần test" có kết quả (đạt).
- Diff `f86a6dc`: `SimConfig.cs` đúng mã trong bước 1 (class, bảng, `WorkSpeedPermille`, 5 luật kiểm); `Simulation.cs` đúng 1 dòng `:419`; `sim_m1.json` đúng vị trí; `SimConfigTests.cs` chỉ thêm dòng (40+/0−); `MasteryTests.cs` khớp từng số nhịp / `WorkDone` trong bước 5. Không đụng `SimData.cs`, `SimHash.cs`, test cũ.
- Phạm vi: chỉ file được phép. Quyết định 0018 giữ nguyên (điểm ghép việc, tốc độ đi không đổi).
- Không chạy lại test: kết quả đủ, 163 = 138 + 25 khớp kỳ vọng.
- Ghi chú cho bước 7 (đo hiệu năng): `WorkSpeedPermille` duyệt bảng (5 dòng) mỗi nhịp cho mỗi sinh vật đang làm — rẻ; nếu cần thì lưu sẵn hệ số trong `Creature`.
- Đã ghi kết quả vào simulation-tech hàng 5 và roadmap "Theo dõi M1".
