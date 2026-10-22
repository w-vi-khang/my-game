# Brief 0040 — Sim 7f (lõi): lệnh trồng trọt của người chơi, phép Mưa, kho ↔ túi trong Game.Core

> Trạng thái: chờ review
> Review: đầy đủ
> Model thực thi: Sonnet · medium
> Cập nhật: 2026-10-22 17:35

<!-- Planner (Opus) viết, Executor (Sonnet) làm theo ở PHIÊN KHÁC — executor không có lịch sử chat, không được tự quyết.
Trạng thái: chờ làm → đang làm → chờ review → đã duyệt; hoặc → cần Opus (executor dừng, hỏi ở mục Vướng mắc).
Review: nhẹ (đọc mục Kết quả) | đầy đủ (đọc cả diff) | user xem (có phần user tự kiểm bằng mắt/chơi thử) — có thể ghép "đầy đủ + user xem".
Model thực thi: BẮT BUỘC, đúng dạng `<Model> · <effort>` — Model: Opus | Sonnet | Haiku | Fable; effort: low | medium | high | xhigh | max (chọn theo AGENTS.md §10.2).
Executor kiểm tra model + effort của phiên TRƯỚC khi làm (scripts/brief_check.py); lệch hoặc không xác định được → cảnh báo user và DỪNG.
File được commit. Tự kiểm trước khi giao (AGENTS.md §10.3): executor có phải hỏi gì không? còn chỗ nào bỏ ngỏ 2 phương án? -->

## Mục tiêu
Phần lõi của [task contract sim 7f](../tasks/m1-sim-buoc-7f-tu-trong-trot.md) ([0028](../../design/decisions/0028-nhan-vat-tui-vat-roi-tu-trong-trot.md), [0031](../../design/decisions/0031-tam-voi-moi-thao-tac-8-o.md), Chốt). Thêm vào `Game.Core`, có test EditMode:

- **Lệnh người chơi vào mô phỏng:** tự xới, tự gieo, tự thu hoạch (vào túi), phép Mưa (tưới ô ruộng / ô đất → cỏ).
- **Túi đồ** thêm nông sản; lấy hạt / nông sản ra được.
- **Kho ↔ túi:** chuyển hạt và nông sản hai chiều; hỏi có kho trong tầm 8 ô.
- **Đồng hồ** cho thời gian xới của người chơi (6 giây) và thời gian hồi Mưa (10 giây).
- **Tìm đường cho nhân vật** (khung 1 × 1 × 2) tới ô kề một ô ruộng.
- Cấu hình: thời gian hồi Mưa trong `sim_m1.json`.

**Không có trong brief này** (brief 0041, 0042 sau): mọi phần client — nhân vật tự đi tới, phím R, click thu hoạch, bảng túi, chế độ gieo, nút chuyển kho, HUD. Sau brief này trong game **chưa thấy gì mới**; đó là chuyện dự kiến.

Ước lượng ~2 ngày-người (trong 6–10 của 7f). Ghi giờ bắt đầu/kết thúc (theo `date`) vào mục Kết quả.

## Bối cảnh cần đọc
Đường dẫn tính từ `client/Assets/Game/`. Số dòng theo commit `baf0e70`.

**Task contract:** `.ai/tasks/m1-sim-buoc-7f-tu-trong-trot.md` — đọc "Đề xuất của Planner" (user xác nhận 2026-10-22) và "Tiêu chí chấp nhận".

**Túi đồ:** `Core/PlayerBag.cs` (toàn file, 69 dòng)
- `readonly int[] counts = new int[256];` (`:11`), `readonly int[] seeds = new int[256];` (`:12`).
- `public event Action<int, int> SeedsChanged;` (`:15`) — (loại cây, số hạt mới).
- `public int GetSeeds(int crop)` (`:39`), `public void AddSeeds(int crop, int amount)` (`:45`) — `amount < 0` → `ArgumentOutOfRangeException`.
- `static void CheckCrop(int crop)` (`:63`) — ngoài 0..255 → `ArgumentOutOfRangeException`.

**Tầm với:** `Core/PlayerRules.cs`
- `public static bool InHandReach(float feetX, float feetY, float feetZ, int x, int y, int z)` (`:24-25`) — tầm 8 ô (0031).

**Sửa ô:** `Core/WorldEditor.cs`
- `public static bool Till(WorldData world, int x, int y, int z)` (`:137-145`): đất/cỏ → ô ruộng, `Cell.SetModified(Cell.SetMaterial(cell, BlockMaterial.Farmland), true)`.
- `public bool TryTill(int x, int y, int z)` (`:148-153`): gọi `Till`, đổi thì phát `CellChanged`.

**Cấu hình:** `Core/Sim/SimConfig.cs`
- `public enum WorkAbility : byte { Till = 0, Sow = 1, Water = 2, Harvest = 3, Haul = 4 }` (`:6`).
- Các lớp cấu hình con `[Serializable]`, vd `CreatureConfig` (`:116-124`).
- `SimConfig`: `public int configVersion = 3;` (`:133`), `public CreatureConfig creature = new CreatureConfig();` (`:168`, trường cuối).
- `public int BaseTicks(WorkAbility a)` (`:192`): Till = 60 nhịp (6000 ms, 10 nhịp/giây).
- `public void Validate()`: kiểm cuối cùng là `creature.wanderMaxMs` (`:321-322`, cuối hàm); `int msPerTick = 1000 / ticksPerSecond;` ở đầu hàm.
- JSON: `design/data/sim_m1.json` (nguồn chuẩn) và bản sao `Client/Resources/Config/sim_m1.json` — hai file phải **giống hệt** (test `Tests/World/ConfigTests.cs`); `Tests/Core/Sim/SimConfigTests.cs:35-39` `ConfigMatchesCodeDefaults` đòi JSON = giá trị mặc định trong code.

**Mô phỏng:** `Core/Sim/SimData.cs`, `Core/Sim/Simulation.cs`
- `public enum StageStatus : byte { Open = 0, Assigned = 1, Done = 2, Blocked = 3, Cancelled = 4 }` (`SimData.cs:30`).
- `public enum PlotPhase : byte { Idle = 0, Till = 1, Sow = 2, Water = 3, Growing = 4, Harvest = 5 }` (`SimData.cs:62`).
- `public enum AssigneeKind : byte { None = 0, Creature = 1, Player = 2 }` (`SimData.cs:65`).
- `WorkStage` (`SimData.cs:121-171`): `WorkDone`, `Status`, `AssigneeKind`, `AssigneeId`, `Carried`, `Stall`, `WorkRequired`.
- `FarmPlot` (`SimData.cs:227-257`): `Soil`, `Site` (= Soil + (0,1,0)), `Slots`, `CurrentCrop`, `TillPaused`, `Phase`, `StageId`, `Tilled`, `SoilReady`, `Removed`.
- `Stockpile` (`SimData.cs:202-218`): `Capacity`, `Seeds[]`, `Produce[]`, `Reserved`, `Free => Capacity - SeedsTotal - ProduceTotal - Reserved`.
- `Simulation`:
  - `readonly GridPathfinder pathfinder;` (`:12`), `bool writingWorld;` (`:13`), `public WorldEditor Editor` (`:15`);
  - `public bool OnPlot(CellPos p, int sx, int sz, int sy)` (`:496-502`);
  - `public void OnCellChanged(...)` (`:344`): `if (!writingWorld) terrainDirty = true;`;
  - `void WriteFarmland(CellPos c)` (`:357-367`) — **mẫu** ghi ô qua editor có `writingWorld`;
  - `void BeginSow(FarmPlot p)` (`:381-389`);
  - `static readonly int[] DisplaceX/DisplaceZ/DisplaceY` (`:594-596`);
  - `int AdjacentGoals(Creature c, CellPos p, bool skipPlots)` (`:665-682`) và `bool PlanTo(...)` (`:684-705`) — **mẫu** tìm ô kề + hai lượt (bỏ ô ruộng trước, rồi cho phép), cuối cùng đứng trên ô;
  - `void OnStageDone(WorkStage s)` (`:743-803`); nhánh `case WorkAbility.Harvest:` (`:772-791`) tạo công đoạn chở (`NearestStorage`, `new WorkStage(... WorkAbility.Haul ...)`) rồi `p.SoilReady = false; p.CurrentCrop = -1;` và vụ kế / đứng yên;
  - `public int PlotAt(CellPos soil)` (`:820-826`);
  - `void AddPlotStage(FarmPlot p, WorkAbility a)` (`:992-999`);
  - `AssignPhase` mở lại công đoạn kẹt `NoSeeds` khi kho có hạt (`:1195`);
  - `Assign` trừ hạt kho khi giao công đoạn gieo (`:1282`); `public void Unassign(int creatureId)` (`:1306-1323`) trả hạt (`:1311`), công đoạn về `Open`, sinh vật về rảnh.
- `GridPathfinder` (`Core/Sim/GridPathfinder.cs`):
  - `public bool CanStand(CellPos a, int sx, int sz, int sy)` (`:118`);
  - `public bool FindPath(CellPos start, CellPos goal, int sx, int sz, int sy, List<CellPos> result)` (`:149`);
  - `public bool FindPathToAny(CellPos start, List<CellPos> goals, int sx, int sz, int sy, List<CellPos> result)` (`:159-171`) — start ngoài vùng → false; start là đích → true, đường rỗng. Đường trả về **không gồm ô xuất phát, gồm ô đích** (`:297-303`).
- Mã băm `Core/Sim/SimHash.cs` đã gồm `Status`, `AssigneeKind`, `WorkDone` của công đoạn, kho chung, mọi trường ô ruộng → **không cần sửa**.

**Mẫu test:** `Tests/Core/Sim/CropQueueTests.cs:17-40`
- `Layout(...)`: `TestWorlds.Flat(1)` (mặt đất y = 1, đứng ở y = 2), `new Simulation(cfg ?? TestWorlds.NoWander(), w)`, `new WorldEditor(w, new PlayerBag(), 1)`, `sim.AttachEditor(editor)`, sinh vật L1 (Till, Sow), L2 (Water, Harvest), L3 (Haul, Harvest), kho (6,2,9), `MarkTill(new CellPos(5,1,5))`, `TestWorlds.HomeAll(sim)`.
- `RunTo(sim, tick)` (`:17-20`).

Không dùng API Unity (chỉ Core + test NUnit).

## Quyết định đã chốt (không được đổi)
**Task contract 7f (user xác nhận 2026-10-22):**
1. Lệnh người chơi là hàm của `Simulation`, gọi giữa hai nhịp (như `MarkTill`), **có tác dụng ngay** khi gọi. Công đoạn đang mở của ô → `Done`, `AssigneeKind = Player`, chạy cùng hậu quả như sinh vật làm xong. Sinh vật đang làm đúng công đoạn đó → về rảnh, mất tiến độ; công đoạn gieo thì hạt kho đã giữ được trả. Công đoạn `Done` không bao giờ được giao lại.
2. Thời gian tự xới **6 giây** = `BaseTicks(Till) / ticksPerSecond`.
3. Gieo: ô đã xới, chưa có vụ (pha `Idle`, `SoilReady`) → vụ mới; ô pha `Sow` cùng loại → gieo xong bằng hạt túi; pha `Sow` khác loại / chưa xới / đã có cây → từ chối. Gieo trừ 1 hạt túi. Hàng đợi (`Slots`) giữ nguyên.
4. Thu hoạch của người chơi: túi + `producePerHarvest` nông sản + `seedsPerHarvest` hạt; **không** tạo công đoạn chở; ô ruộng như khi sinh vật thu xong.
5. Mưa: ô ruộng pha `Water` → tưới xong; ô **đất** có ô trên rỗng → cỏ; khác → không tác dụng. Hồi 10 giây, số trong JSON.
6. Kho ↔ túi: chỉ hạt và nông sản (khối không vào kho). Túi → kho dừng khi `Stock.Free` hết. Kho → túi lấy được mọi thứ đang có.
7. `Game.Core` không tham chiếu UnityEngine. Hành vi sinh vật không đổi khi người chơi không ra lệnh.

**Planner chốt khi viết brief:**

8. **Hàm chung** trong `Simulation` (private):
   ```csharp
   /// <summary>Người chơi làm xong công đoạn s (0028, simulation-tech "Người chơi làm công đoạn"). bag != null chỉ dùng cho thu hoạch: hàng vào túi, không chở.</summary>
   void CompleteByPlayer(WorkStage s, PlayerBag bag = null)
   ```
   Thân, đúng thứ tự:
   - `if (s.Status == StageStatus.Assigned) Unassign(s.AssigneeId);` (trả hạt gieo, sinh vật về rảnh);
   - `s.WorkDone = s.WorkRequired; s.Status = StageStatus.Done; s.Stall = StallReason.None; s.Carried = false; s.AssigneeKind = AssigneeKind.Player; s.AssigneeId = -1;`
   - `OnStageDone(s, bag);`
   Không đụng `s.ReportedStall` (ReportPhase tự phát `StallCleared`).
9. **`OnStageDone(WorkStage s)` → `OnStageDone(WorkStage s, PlayerBag bag = null)`.** Chỉ nhánh `Harvest` đổi: `bag != null` → `bag.AddProduce(crop, cd.producePerHarvest); bag.AddSeeds(crop, cd.seedsPerHarvest);` **thay cho** đoạn `NearestStorage` + tạo công đoạn chở (4 dòng `int k = …` tới `State.Orders[p.OrderId].StageIds.Add(haul.Id);`). Phần sau (`SoilReady = false`, `CurrentCrop = -1`, vụ kế / đứng yên) giữ nguyên cho cả hai. Lời gọi cũ ở `WorkPhase` giữ nguyên (không truyền bag).
10. **Tự xới xoá `TillPaused`** (`p.TillPaused = false`) trước khi hoàn thành — nếu không, lần xới lại sau sẽ kẹt.
11. **Gieo vào ô đứng yên:** `p.CurrentCrop = cropId; p.Phase = PlotPhase.Sow; AddPlotStage(p, WorkAbility.Sow);` rồi `CompleteByPlayer(State.Stages[p.StageId])` — có bản ghi công đoạn gieo `Done` của người chơi, sang pha tưới qua `OnStageDone`.
12. **Mưa sang cỏ** ghi qua editor như `WriteFarmland`, có `writingWorld = true` (cỏ không đổi đường đi → không làm bẩn địa hình). Editor null → ghi thẳng bằng hàm tĩnh.
13. **Đồng hồ dùng `float` giây** (thời gian thực ở client, không phải mô phỏng). Không vào mã băm.
14. **Tìm đường nhân vật** không tránh sinh vật (người chơi không bị sinh vật chặn ở M1), cách chọn đích như `PlanTo(..., Approach.AdjacentOrOn)` với khung 1 × 1 × 2.
15. `configVersion` 3 → 4.

## Phạm vi
- Được sửa/tạo:
  - `Core/PlayerBag.cs`;
  - `Core/WorldEditor.cs` — chỉ thêm `Grow` / `TryGrow` (bước 2);
  - `Core/PlayerTimers.cs` (mới, + `.meta` do Unity tạo);
  - `Core/Sim/SimConfig.cs`, `Core/Sim/Simulation.cs`, `Core/Sim/SimData.cs` (chỉ thêm enum ở bước 5);
  - `design/data/sim_m1.json` và `Client/Resources/Config/sim_m1.json` — chỉ `configVersion` và khối `player`;
  - test: `Tests/Core/PlayerBagTests.cs` (mới), `Tests/Core/PlayerTimersTests.cs` (mới), `Tests/Core/Sim/PlayerFarmTests.cs` (mới), `Tests/Core/WorldEditorTests.cs` (thêm test), `Tests/Core/Sim/SimConfigTests.cs` (thêm assert) — kèm `.meta` do Unity tạo;
  - mục Kết quả của brief này.
- Không được sửa:
  - `Client/**` (trừ file JSON nói trên), scene `.unity`;
  - `Core/Sim/SimHash.cs`, `Core/Sim/GridPathfinder.cs`;
  - hành vi sinh vật (`AssignPhase`, `WorkPhase`, `MovePhase`… ngoài lời gọi `OnStageDone` đã nêu);
  - `design/` ngoài hai file JSON; task contract; file khác.

## Các bước
1. **`Core/PlayerBag.cs`** — thêm nông sản và lấy ra:
   ```csharp
   readonly int[] produce = new int[256];
   /// <summary>(loại cây, số nông sản mới).</summary>
   public event Action<int, int> ProduceChanged;

   public int GetProduce(int crop);                 // CheckCrop
   public void AddProduce(int crop, int amount);    // CheckCrop; amount < 0 → ArgumentOutOfRangeException; phát ProduceChanged
   /// <summary>Đủ thì trừ amount hạt, phát SeedsChanged, true; thiếu → false, không đổi. amount < 0 → ArgumentOutOfRangeException; amount = 0 → true, không phát.</summary>
   public bool TryTakeSeeds(int crop, int amount);
   /// <summary>Như TryTakeSeeds cho nông sản (phát ProduceChanged).</summary>
   public bool TryTakeProduce(int crop, int amount);
   ```
   Sửa summary lớp: "khối theo vật liệu + hạt + nông sản theo loại cây". Xong khi: biên dịch được.

2. **`Core/WorldEditor.cs`** — thêm ngay sau `TryTill` (`:153`):
   ```csharp
   /// <summary>Phép Mưa (0028): ô đất có ô trên rỗng (trong map) → cỏ, cờ đã sửa bật. Chỉ ghi dữ liệu, không phát sự kiện. true khi ô đổi.</summary>
   public static bool Grow(WorldData world, int x, int y, int z)
   /// <summary>Như Grow qua editor: ô đổi thì phát CellChanged.</summary>
   public bool TryGrow(int x, int y, int z)
   ```
   `Grow`: `(x,y,z)` và `(x,y+1,z)` đều trong map; vật liệu `(x,y,z)` là `BlockMaterial.Dirt` (không gồm cỏ, ô ruộng); `(x,y+1,z)` là `BlockMaterial.Empty` → `world.Set(x, y, z, Cell.SetModified(Cell.SetMaterial(cell, BlockMaterial.Grass), true))`, true. Ngược lại false. `TryGrow` viết như `TryTill`.

3. **`Core/PlayerTimers.cs`** (mới, namespace `Game.Core`, chỉ `using System;`):
   ```csharp
   /// <summary>Đồng hồ việc tay của người chơi (tự xới 7f): chạy theo thời gian thực ở client.</summary>
   public sealed class WorkTimer
   {
       public WorkTimer(float durationSeconds);   // ≤ 0 → ArgumentOutOfRangeException
       public float Duration { get; }
       public float Elapsed { get; }              // 0 khi chưa chạy / đã huỷ
       public bool Running { get; }
       public void Start();                       // Elapsed = 0, Running = true (gọi lại khi đang chạy → bắt đầu lại từ 0)
       public void Cancel();                      // Elapsed = 0, Running = false
       /// <summary>Đang chạy: Elapsed += dt (dt < 0 → ArgumentOutOfRangeException). Elapsed ≥ Duration → Elapsed = Duration, Running = false, trả true (đúng một lần). Không chạy → false.</summary>
       public bool Advance(float dt);
   }

   /// <summary>Thời gian hồi của phép (Mưa 7f): chạy theo thời gian thực ở client.</summary>
   public sealed class Cooldown
   {
       public Cooldown(float durationSeconds);    // ≤ 0 → ArgumentOutOfRangeException
       public float Duration { get; }
       public float Remaining { get; }            // 0 lúc tạo
       public bool Ready => Remaining <= 0f;
       public void Start();                       // Remaining = Duration
       public void Advance(float dt);             // dt < 0 → ArgumentOutOfRangeException; Remaining = Math.Max(0f, Remaining - dt)
       /// <summary>Số giây còn lại làm tròn lên (HUD "Mưa (R): 7 giây"); 0 khi sẵn sàng.</summary>
       public int RemainingWholeSeconds => (int)Math.Ceiling(Remaining);
   }
   ```

4. **`Core/Sim/SimConfig.cs`**:
   - Lớp mới, đặt sau `CreatureConfig`:
     ```csharp
     /// <summary>Số của người chơi (0028, số tạm). rainCooldownMs = thời gian hồi phép Mưa.</summary>
     [Serializable]
     public class PlayerConfig
     {
         public int rainCooldownMs = 10000;
     }
     ```
   - Trong `SimConfig`: `configVersion = 4`; thêm trường cuối `public PlayerConfig player = new PlayerConfig();` (sau `creature`).
   - Thêm sau `GrowTicks`:
     ```csharp
     /// <summary>Thời gian người chơi tự xới (0028: bằng công đoạn xới ở tốc độ gốc 1000‰) — giây.</summary>
     public float PlayerTillSeconds => BaseTicks(WorkAbility.Till) / (float)ticksPerSecond;
     public float RainCooldownSeconds => player.rainCooldownMs / 1000f;
     ```
   - Cuối `Validate()`:
     ```csharp
     if (player == null || player.rainCooldownMs <= 0)
         throw new ArgumentException("player.rainCooldownMs phải > 0.");
     ```
   - **Hai file JSON** (giống hệt nhau): `"configVersion": 4,` và sau dòng `"creature": {...}` thêm dấu phẩy + dòng `"player": { "rainCooldownMs": 10000 }` (thụt 4 dấu cách như các dòng khác).
   - Xong khi: `SimConfigTests` và `ConfigTests` qua.

5. **`Core/Sim/SimData.cs`** — thêm sau `enum AssigneeKind` (`:65`):
   ```csharp
   /// <summary>Kết quả người chơi gieo (7f).</summary>
   public enum PlayerSowResult : byte { Ok = 0, NoPlot = 1, NotTilled = 2, HasCrop = 3, OtherCrop = 4, NoSeeds = 5 }

   /// <summary>Kết quả phép Mưa (7f).</summary>
   public enum RainResult : byte { NoEffect = 0, Watered = 1, Grassed = 2 }
   ```

6. **`Core/Sim/Simulation.cs`** — `OnStageDone` + `CompleteByPlayer` theo quyết định 8, 9. Thêm sau `ResumeTill` (`:944-950`) các hàm công khai, mỗi hàm có summary tiếng Việt một dòng:
   ```csharp
   /// <summary>Người chơi tự xới được ô plotId không: ô còn, đang ở pha xới (lần đầu hoặc xới lại), có công đoạn.</summary>
   public bool CanPlayerTill(int plotId)
   /// <summary>Người chơi xới xong (0028 mục 7): bỏ tạm dừng, hoàn thành công đoạn xới. false nếu !CanPlayerTill.</summary>
   public bool PlayerTill(int plotId)

   public PlayerSowResult CheckPlayerSow(int plotId, int cropId, PlayerBag bag)
   /// <summary>Gieo bằng hạt túi (0028 mục 8). Ok → trừ 1 hạt túi, vụ sang pha tưới.</summary>
   public PlayerSowResult PlayerSow(int plotId, int cropId, PlayerBag bag)

   /// <summary>Cây chín (pha thu hoạch) và ô còn.</summary>
   public bool CanPlayerHarvest(int plotId)
   /// <summary>Thu hoạch vào túi (0028 mục 10), không chở. false nếu !CanPlayerHarvest.</summary>
   public bool PlayerHarvest(int plotId, PlayerBag bag)

   /// <summary>Phép Mưa lên ô cell (0028 mục 9): ô ruộng chờ tưới → tưới xong; ô đất → cỏ; khác → NoEffect, không đổi gì.</summary>
   public RainResult Rain(CellPos cell)

   /// <summary>Có kho nào trong tầm việc tay của nhân vật (0031: 8 ô).</summary>
   public bool StorageInReach(float feetX, float feetY, float feetZ)
   /// <summary>Túi → kho chung: chuyển min(amount, số trong túi, Stock.Free) hạt (produce = false) hoặc nông sản; trả số đã chuyển.</summary>
   public int StoreToStock(PlayerBag bag, int cropId, bool produce, int amount)
   /// <summary>Kho chung → túi: chuyển min(amount, số trong kho); trả số đã chuyển.</summary>
   public int TakeFromStock(PlayerBag bag, int cropId, bool produce, int amount)

   /// <summary>Đường cho nhân vật (khung 1 × 1 × 2, không tránh sinh vật) từ ô chân feet tới ô kề ô ruộng plotId — như sinh vật (AdjacentOrOn). result không gồm feet; đã đứng ở đích → true, result rỗng. Không tới được / ô không còn → false.</summary>
   public bool FindPlayerPathToPlot(CellPos feet, int plotId, List<CellPos> result)
   ```
   Luật chi tiết:
   - **Chung:** `plotId` ngoài `0..Plots.Count-1` → coi như ô không còn (false / `NoPlot`), không ném. `cropId` ngoài `0..Config.crops.Length-1` → `ArgumentException` (như `CanEnqueueCrop`). `bag == null` → `ArgumentNullException`. `amount < 0` → `ArgumentOutOfRangeException`.
   - **`CanPlayerTill`:** `!p.Removed && p.Phase == PlotPhase.Till && p.StageId >= 0`. `PlayerTill`: `p.TillPaused = false;` rồi `CompleteByPlayer(State.Stages[p.StageId])`.
   - **`CheckPlayerSow`** theo thứ tự:
     1. ô không còn → `NoPlot`;
     2. `p.Phase == PlotPhase.Sow`: `p.CurrentCrop != cropId` → `OtherCrop`;
     3. `p.Phase == Water / Growing / Harvest` → `HasCrop`;
     4. `p.Phase == Till`, hoặc `p.Phase == Idle && !p.SoilReady` → `NotTilled`;
     5. `bag.GetSeeds(cropId) <= 0` → `NoSeeds`;
     6. còn lại (`Sow` cùng loại, hoặc `Idle && SoilReady`) → `Ok`.
   - **`PlayerSow`:** `CheckPlayerSow` khác `Ok` → trả luôn, không đổi gì. `Ok` → `bag.TryTakeSeeds(cropId, 1)`; pha `Idle` → làm theo quyết định 11; pha `Sow` → `CompleteByPlayer(State.Stages[p.StageId])`.
   - **`CanPlayerHarvest`:** `!p.Removed && p.Phase == PlotPhase.Harvest && p.StageId >= 0`. `PlayerHarvest`: `CompleteByPlayer(State.Stages[p.StageId], bag)`.
   - **`Rain`:**
     1. `int id = PlotAt(cell);` `id >= 0 && Plots[id].Phase == PlotPhase.Water && Plots[id].StageId >= 0` → `CompleteByPlayer(...)`, `Watered`;
     2. ngược lại, ghi cỏ (quyết định 12): Editor khác null → `writingWorld = true; try { changed = Editor.TryGrow(...); } finally { writingWorld = false; }`; Editor null → `changed = WorldEditor.Grow(World, ...)`. `changed` → `Grassed`;
     3. còn lại → `NoEffect`.
   - **`StorageInReach`:** có `Storage s` với `PlayerRules.InHandReach(feetX, feetY, feetZ, s.Cell.X, s.Cell.Y, s.Cell.Z)`.
   - **`StoreToStock`:** `n = Math.Min(amount, produce ? bag.GetProduce(cropId) : bag.GetSeeds(cropId)); n = Math.Min(n, Math.Max(0, State.Stock.Free));` `n > 0` → trừ túi (`TryTakeProduce` / `TryTakeSeeds`), cộng `State.Stock.Produce[cropId]` / `Seeds[cropId]`. Trả `n`.
   - **`TakeFromStock`:** `n = Math.Min(amount, kho)`; `n > 0` → trừ kho, `bag.AddProduce` / `bag.AddSeeds`. Trả `n`.
   - **`FindPlayerPathToPlot`:** ô không còn → false. `site = p.Site`. Dùng list riêng `playerGoals` (field mới `readonly List<CellPos> playerGoals = new List<CellPos>();`):
     1. lượt 1: với mỗi `d` 0..3, `k` 0..2: `n = site.Offset(DisplaceX[d], DisplaceY[k], DisplaceZ[d])`; ô đầu tiên `pathfinder.CanStand(n, 1, 1, 2)` → nếu `OnPlot(n, 1, 1, 2)` thì bỏ (đếm `skipped`), không thì thêm; `break` sang hướng kế (giống `AdjacentGoals`);
     2. `pathfinder.FindPathToAny(feet, playerGoals, 1, 1, 2, result)` → true thì trả true;
     3. `skipped > 0` → lượt 2 không bỏ ô ruộng, thử lại;
     4. cuối: `feet == site` → `result.Clear()`, true; không thì `pathfinder.FindPath(feet, site, 1, 1, 2, result)`.

   Xong khi: biên dịch được, test cũ qua.

7. **Test** (namespace `Game.Core.Tests`, NUnit như test cũ):
   - `Tests/Core/PlayerBagTests.cs`, `Tests/Core/PlayerTimersTests.cs`, `Tests/Core/Sim/PlayerFarmTests.cs`: các dòng ở "Phần cần test".
   - `PlayerFarmTests` dựng như `CropQueueTests.Layout` (`:22-40`), có tham số chọn có/không sinh vật; nhân vật đứng tại ô `(5, 2, 7)` khi cần chân (feet float = `5.5f, 2f, 7.5f`).
   - Thêm vào `WorldEditorTests`, `SimConfigTests` như "Phần cần test".

## Tiêu chí xong
- Toàn bộ EditMode qua (số test cũ + test mới), 0 lỗi biên dịch.
- PlayMode cũ vẫn qua (code client không đổi, nhưng JSON và `SimConfig` đổi).
- `grep -n "UnityEngine" Core/PlayerTimers.cs Core/PlayerBag.cs Core/Sim/Simulation.cs` → không có dòng nào.
- Hai file `sim_m1.json` giống hệt (`diff` không in gì).

## Test sau khi xong
### Phần cần test
**Mới — `PlayerBagTests`:**
- Nông sản: `AddProduce(0, 3)` → `GetProduce(0) == 3`, `ProduceChanged` phát `(0, 3)`. `AddProduce(0, -1)` và `GetProduce(256)` → `ArgumentOutOfRangeException`.
- `TryTakeSeeds(0, 2)` khi có 3 → true, còn 1, `SeedsChanged (0, 1)`; khi có 1 → false, vẫn 1, không phát. `amount = 0` → true, không phát. Tương tự `TryTakeProduce`.

**Mới — `PlayerTimersTests`:**
- `WorkTimer(6f)`: `Start`; `Advance(5.9f)` → false, `Running`; `Advance(0.2f)` → true, `Elapsed == 6`, `!Running`; `Advance(1f)` lần nữa → false.
- `Cancel` giữa chừng → `Elapsed == 0`, `!Running`, `Advance` → false. `Start` lại → từ 0.
- `Cooldown(10f)`: lúc tạo `Ready`; `Start` → `!Ready`, `RemainingWholeSeconds == 10`; `Advance(3.5f)` → `RemainingWholeSeconds == 7`; `Advance(6.5f)` → `Ready`, `Remaining == 0`; `Advance(1f)` không âm.
- Tham số sai (`≤ 0` lúc tạo, `dt < 0`) → `ArgumentOutOfRangeException`.

**Mới — `WorldEditorTests`:**
- `TryGrow` trên ô đất mặt (trên rỗng) → true, ô thành cỏ, `CellChanged` phát 1 lần.
- `TryGrow` trên cỏ, ô ruộng, đá, ô đất có khối bên trên, ô ngoài map → false, không phát.

**Mới — `SimConfigTests`:**
- `ConfigParsesAndValidates`: thêm `Assert.AreEqual(4, cfg.configVersion)`, `PlayerTillSeconds == 6f`, `RainCooldownSeconds == 10f`.
- `ValidateRejectsBadValues`: `player.rainCooldownMs = 0` → `ArgumentException`; `player = null` → `ArgumentException`.

**Mới — `PlayerFarmTests` (lệnh người chơi):**
- **Tự xới, không sinh vật:** `MarkTill` → `CanPlayerTill` true → `PlayerTill` true → ô thành `Farmland`, `Tilled`, `SoilReady`, pha `Idle` (không hàng đợi); công đoạn xới `Done`, `AssigneeKind.Player`. Gọi lại → false.
- **Tự xới khi sinh vật đang xới:** chạy tới lúc L1 `Working` trên công đoạn xới → `PlayerTill` → L1 về `Idle`, `StageId == -1`; chạy thêm 200 nhịp → không có công đoạn xới mới nào được tạo cho ô (đếm công đoạn Till = 1, nếu hàng đợi trống).
- **Tự xới ô đang tạm dừng xới lại** (`PauseTill`) → `TillPaused == false` sau `PlayerTill`.
- **Gieo:**
  - ô `Idle && SoilReady`, túi 2 hạt cây 0 → `Ok`, túi còn 1, `CurrentCrop == 0`, pha `Water`, có công đoạn gieo `Done` của người chơi và công đoạn tưới mở; `Slots` không đổi;
  - ô pha `Sow` cây 0 (đã `EnqueueCrop(0)`), L1 đang mang công đoạn gieo (đã trừ 1 hạt kho) → `PlayerSow(…, 0, bag)` `Ok`, hạt kho trở lại như trước khi giao, L1 rảnh, pha `Water`;
  - pha `Sow` cây 1, gieo cây 0 → `OtherCrop`, không đổi gì (túi, pha);
  - ô chưa xới (pha `Till`) → `NotTilled`; ô pha `Growing` → `HasCrop`; ô đã huỷ → `NoPlot`; túi 0 hạt → `NoSeeds`; `plotId = 99` → `NoPlot`; `cropId = 5` → `ArgumentException`.
- **Mưa:**
  - ô ruộng pha `Water` → `Watered`, pha `Growing`, `RipeTick == Tick + GrowTicks(crop)`;
  - L2 đang tưới ô đó → về rảnh;
  - ô đất mặt (không phải ô ruộng) → `Grassed`, ô thành cỏ; ô có biển trên đất → `Grassed`, ô có biển vẫn còn (`PlotAt` ≥ 0), chạy 1 nhịp không bị huỷ;
  - ô đá, ô cỏ, ô ruộng pha `Growing` → `NoEffect`, world và mã băm không đổi.
- **Thu hoạch:** ô pha `Harvest` cây 0 → `PlayerHarvest` true, túi +1 nông sản, +1 hạt cây 0; không có công đoạn `Haul` mới; `Stock.Reserved` không đổi; `SoilReady == false`; còn hàng đợi → pha `Till` (vụ kế), không → `Idle`. Cây 1 → +3 nông sản, +2 hạt. Pha khác → false.
- **Chuỗi bằng tay không sinh vật, không kho:** `MarkTill` → `PlayerTill` → `PlayerSow(0)` → `Rain` → chạy `GrowTicks(0)` nhịp → pha `Harvest` → `PlayerHarvest` → túi: nông sản cây 0 = 1, hạt = (số đầu − 1 + 1).
- **Kho ↔ túi:**
  - `StoreToStock` hạt / nông sản: số đúng, túi trừ, kho cộng; kho gần đầy → chỉ chuyển tới `Free`, trả số đã chuyển; `Free == 0` → 0, không đổi;
  - `TakeFromStock` nhiều hơn có → chỉ lấy số có; kho 0 → 0;
  - cất hạt vào kho khi công đoạn gieo đang `Blocked` `NoSeeds` → sau 1 nhịp công đoạn được mở lại / giao.
- **`StorageInReach`:** kho (6,2,9), tâm ô (6.5, 2.5, 9.5). Chân `(6.5f, 1.5f, 1.5f)` (tâm thân cách đúng 8) → true; chân `(6.5f, 1.5f, 1.4f)` (8,1) → false; mô phỏng không có kho → false.
- **`FindPlayerPathToPlot`:** từ (5,2,9) tới ô ruộng (5,1,5) → true, ô cuối của đường kề `Site` (Manhattan ngang = 1, |dy| ≤ 1), mỗi bước `CanStep` hợp lệ (độ chênh ≤ 1); chân đã ở ô kề → true, đường rỗng; vây ô ruộng bằng 4 cột đá cao 3 (`TestWorlds.Pillar` tại (4,5), (6,5), (5,4), (5,6)) → false (không ô kề đứng được, không leo lên được 2 ô để vào `Site`); ô đã huỷ → false.
- **Tất định:** hai mô phỏng cùng `Layout`, cùng chuỗi lệnh (`PlayerTill` ở nhịp 5, `PlayerSow` nhịp 6, `Rain` nhịp 7, `StoreToStock` nhịp 8) → mã băm `SimHash.Compute` bằng nhau ở mọi nhịp tới 500, túi bằng nhau.

**Cũ có thể bị ảnh hưởng:**
- Toàn bộ test sim (`FarmTests`, `CropQueueTests`, `PlotRulesTests`, `SimDeterminismTests`, `SimulationTerrainTests`, `HomeTests`…) — `OnStageDone` đổi chữ ký; kỳ vọng không đổi.
- `SimConfigTests.ConfigMatchesCodeDefaults`, `Tests/World/ConfigTests` (hai bản JSON giống nhau), `SimDemo*Tests`, `SimStressTests` — JSON đổi.
- `WorldEditorTests`, `DropFieldTests` — `WorldEditor` thêm hàm.
- PlayMode toàn bộ — client đọc `sim_m1.json` mới.

### Lệnh executor chạy
1. `date` (giờ bắt đầu) trước bước 1.
2. `diff design/data/sim_m1.json client/Assets/Game/Client/Resources/Config/sim_m1.json` → không in gì.
3. `grep -n "UnityEngine" client/Assets/Game/Core/PlayerTimers.cs client/Assets/Game/Core/PlayerBag.cs client/Assets/Game/Core/Sim/Simulation.cs` → không có dòng.
4. Unity Editor mở (MCP): `refresh_unity` → `read_console` không lỗi biên dịch → `run_tests` EditMode (toàn bộ) → ghi dòng tổng (pass/fail/total).
5. `run_tests` PlayMode (toàn bộ) → ghi dòng tổng. Sau đó `git checkout -- client/ProjectSettings/EditorSettings.asset`.
   - Editor tắt: `python3 tools/unity_test.py EditMode` rồi `python3 tools/unity_test.py PlayMode`.
6. `date` (giờ kết thúc).

### User tự test
Không (brief không đổi gì nhìn thấy được; user xem ở brief 0041, 0042).

## Dừng và chuyển lại Opus khi
- Brief mơ hồ hoặc mâu thuẫn với code thật (vd số dòng lệch làm không tìm được đoạn cần sửa, `OnStageDone` có lời gọi khác ngoài `WorkPhase`).
- Một test sim cũ đổi kết quả sau khi sửa `OnStageDone` — không tự sửa kỳ vọng test cũ.
- Cần một quyết định thuộc AGENTS.md §3.4 hoặc ngoài "Quyết định đã chốt".
- Cùng một lỗi sửa 2 lần vẫn chưa qua.
- Phải sửa file ngoài "Phạm vi".

---
<!-- Từ đây trở xuống: EXECUTOR ghi. -->

## Kết quả
Base commit: d373626
Chạy bằng: claude-sonnet-5-5 · medium
Giờ bắt đầu: 2026-10-22 17:22

Giờ kết thúc: 2026-10-22 17:33 (≈ 11 phút đồng hồ, phần lớn chờ Unity chạy test; ước lượng ~2 ngày-người).
Commit: 2f713c4 — file đổi: PlayerBag.cs, PlayerTimers.cs (mới), WorldEditor.cs (Grow/TryGrow), Sim/SimConfig.cs, Sim/SimData.cs, Sim/Simulation.cs, hai sim_m1.json (configVersion 4 + player); test: PlayerBagTests, PlayerTimersTests, Sim/PlayerFarmTests (mới), WorldEditorTests, SimConfigTests.

## Kết quả test
- `diff` hai sim_m1.json: không in gì — đạt. `grep UnityEngine` ba file Core: không dòng — đạt.
- EditMode toàn bộ (lần 1): 469/471 qua; 2 lỗi là lỗi test tôi viết (PlayerTillClearsPause: PauseTill đòi ô đã xới; SowRejections: Unmark không áp cho ô đã xới). Test cũ không đổi kết quả. Đã sửa 2 test (không sửa code); chạy lại riêng PlayerFarmTests: 26/26 qua. Chưa chạy lại toàn bộ EditMode sau lần sửa test (mất ~7 phút do Editor không focus).
- PlayMode toàn bộ: 121/121 qua. Đã `git checkout -- client/ProjectSettings/EditorSettings.asset`.
- Phần cần test: PlayerBag (nông sản, TryTake*), PlayerTimers, TryGrow (đạt/từ chối), SimConfig (v4, 6 s, 10 s, Validate), tự xới (không sinh vật / giành từ sinh vật / bỏ tạm dừng), gieo (Idle, giành từ sinh vật + trả hạt kho, từ chối), Mưa (tưới, giành, cỏ, biển vẫn còn, NoEffect hash không đổi), thu hoạch (túi, vụ kế, sai pha), chuỗi tay, kho ↔ túi, StorageInReach, đường, tất định 500 nhịp — đều có test và qua.

## Vướng mắc
Không.

---
<!-- PLANNER ghi khi review. -->

## Review
