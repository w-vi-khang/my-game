# Brief 0061 — 7l d1: túi người chơi thành túi ô vuông thật (túi 10 ô + túi phụ 2 ô, 999/ô, túi đầy → vật nằm lại)

> Trạng thái: đã duyệt
> Review: đầy đủ + user xem
> Model thực thi: Sonnet · medium
> Cập nhật: 2026-10-24 (review)

<!-- Planner (Opus) viết, Executor (Sonnet) làm theo ở PHIÊN KHÁC — executor không có lịch sử chat, không được tự quyết. -->

## Mục tiêu
Bước **7l d** của task [m1-world-7l-dieu-khien-pc](../tasks/m1-world-7l-dieu-khien-pc.md) chia 3 brief, làm tuần tự: **0061 (d1, brief này)** lõi → 0062 (d2) giao diện túi / túi phụ / kho → 0063 (d3) chuyển đồ (kéo thả, Tab, 1/2/3). Luật: [0034 mục 6](../../design/decisions/0034-dieu-khien-pc-click-di-chuyen.md#6-túi-túi-phụ-kho).

Hiện `PlayerBag` đếm theo loại, **không giới hạn**. `SlotInventory` (túi ô vuông, brief 0054) đã có và đã test nhưng **chưa ai dùng**. Brief này:
1. `PlayerBag` giữ 2 `SlotInventory`: **túi** (`Main`, 10 ô) và **túi phụ** (`Hotbar`, 2 ô), 999/ô, số đọc từ JSON. API cũ (`Get`, `Add`, `TryTake`, `GetSeeds`, …) giữ tên, đọc / ghi qua các ô.
2. Túi đầy → phần không vào được **nằm lại thành vật rơi** (nhặt, thu hoạch); lấy từ kho chung không vượt chỗ trống của túi.
3. Camera quan sát thành vật phẩm thật nằm trong 1 ô túi.

Không có giao diện mới (bảng túi IMGUI cũ giữ nguyên tới 0062).

Bắt đầu: kiểm brief 0060 có `> Trạng thái: đã duyệt`. Không có → dừng, `cần Opus`.

**File lạ / của phiên khác — KHÔNG commit, KHÔNG sửa:** `client/Assets/InitTestScene*.unity(.meta)` (scene tạm Unity Test Runner); các thay đổi chưa commit trong `design/` (quyết định 0038 …), `.claude/skills/local-terrain-gen/`, `tools/terrain_catalog*`, `.ai/skill-map.local.json`. Chỉ `git add` đúng file trong "Phạm vi".

Ước lượng: 1–1,5 ngày-người (một phần 3–4 ngày của dòng 7l d trong world-tech, M1).

## Bối cảnh cần đọc
Đường dẫn tính từ `client/Assets/Game/` trừ khi ghi khác.

**Game.Core (đọc hết):**
- `Core/SlotInventory.cs` (195 dòng): `ItemStack` `:6–12`; `interface IItemStore { int Count(ItemId); int CanAdd(ItemId, int); int Add(ItemId, int); int Remove(ItemId, int); }` `:15–24`; `SlotInventory(int capacity, int pageSize, int stackMax)` `:32`; `Capacity`, `PageSize`, `StackMax`, `PageCount` `:44–47`; event `SlotChanged` `:49`; `Get(int slot)` `:74`; `Count` `:80`; `CanAdd` `:88` (cộng chỗ trống của ô trống + ô cùng loại); `Add` `:100` (lấp ô cùng loại trước, rồi ô trống, theo chỉ số tăng); `Remove` `:113` (lấy từ ô cuối về đầu); `AddToSlot` `:124`; `RemoveFromSlot` `:133`; `Move` `:141`; `Put` / `Take` riêng `:176–193`.
- `Core/PlayerBag.cs` (117 dòng — **viết lại**): mảng đếm `counts` / `seeds` / `produce` 256 phần tử; event `SeedsChanged (crop, số mới)` `:16`, `ProduceChanged` `:19`, `Changed (BlockMaterial, số mới)` `:21`; `Get` `:23`; `Add(BlockMaterial, int amount = 1)` `:25` (void); `TryTake(BlockMaterial)` `:34`; `GetSeeds` / `AddSeeds` / `GetProduce` / `AddProduce` `:43–71` (void); `TryTakeSeeds` / `TryTakeProduce` `:74–97` (thiếu → false, không đổi; amount 0 → true, không phát event); `Add(ItemId, int)` `:100` (Tool → `NotSupportedException`); `CheckCrop` `:111` (crop ngoài 0..255 → `ArgumentOutOfRangeException`).
- `Core/DropField.cs`: `ItemKind`, `ToolItem`, `ItemId` `:6–47`; `DropStack.Count { get; internal set; }`; `Add(int x, int y, int z, ItemId item, int count)` `:100` (ô đặc → `ArgumentException`; tự hạ xuống mặt khối); event `Collected`; `CollectInReach(float feetX, float feetY, float feetZ, PlayerBag bag)` `:170`; `TryCollect(int stackId, …, PlayerBag bag)` `:187`; `Take(DropStack s, PlayerBag bag)` `:199–204` (`bag.Add(s.Item, s.Count); Collected?.Invoke(s); Remove(s);`).
- `Core/PlayerRules.cs` `Configure(PlayerConfig p)` `:18–27`.
- `Core/Sim/SimConfig.cs` `PlayerConfig`: `bagSlots = 10`, `hotbarSlots = 2`, `bagPageSize = 10`, `hotbarPageSize = 10`, `stackMax = 999` `:148–152`; kiểm biên `:386–…` (đã có, **không sửa**). JSON `design/data/sim_m1.json` dòng 38 đã có đủ 5 số (**không sửa JSON**).
- `Core/Sim/Simulation.cs`:
  - `Editor` (WorldEditor, có thể null) `:15`;
  - `OnStageDone(WorkStage s, PlayerBag bag = null)` `:744`, nhánh `case WorkAbility.Harvest:` `:774–790` (`bag.AddProduce(crop, cd.producePerHarvest); bag.AddSeeds(crop, cd.seedsPerHarvest);` `:778–779`);
  - `FarmPlot.Soil` (`CellPos`, ô đất ruộng) — `Core/Sim/SimData.cs:236`;
  - `StoreToStock` `:1084`; `TakeFromStock(PlayerBag bag, int cropId, bool produce, int amount)` `:1106–1124`.
- `Core/Sim/StockStore.cs` (60 dòng, **không sửa**): kho chung nhìn như `IItemStore`.
- `Core/WorldEditor.cs`: dùng `Bag.Get(material)` `:114`, `Bag.TryTake(material)` `:125` — giữ chạy được, không sửa.

**Client:**
- `Client/TerrainChunkView.cs:225` `Bag = new PlayerBag();`.
- `Client/TerrainBench.cs:93–94` thêm 100000 mỗi khối cho cảnh đo.
- `Client/TerrainEditInput.cs`: `CollectNearby()` (`int n = view.Editor.Drops.CollectInReach(...)`, `lastResult = n > 0 ? Format("edit.collect_ok", n) : Format("edit.collect_none", PlayerRules.CollectReach);`), `CollectDrop(DropStack s)` (ngay dưới `CollectNearby`), `CanHold(ItemId item)` (`case ItemKind.Tool: return item.ToolKind == ToolItem.ObservationCamera;`).
- `Client/SimView.cs:375–395` `BagRows` (dòng cuối luôn thêm camera quan sát × 1) — **giữ nguyên** ở brief này.
- Chuỗi: `Client/Resources/Lang/vi.json`, `loc/en.json`, `loc/context.json` (gốc repo; khoá xếp theo bảng chữ cái).

**Test mẫu:** `Tests/Core/PlayerBagTests.cs` (80 dòng), `Tests/Core/SlotInventoryTests.cs`, `Tests/Core/DropFieldTests.cs` (`SetUp` `:27–33`, `CollectInReachTakesOnlyWithin2` `:137`, `TryCollectSingleStack` `:156`), `Tests/Core/Sim/PlayerFarmTests.cs` (`Layout` `:23`, có `sim.AttachEditor(...)` `:28`; `HarvestIntoBag` `:300`; `TakeFromStock` `:391`), `Tests/Core/PlayerRulesTests.cs` (`TearDown` `:9` gọi `PlayerRules.Configure(new PlayerConfig())`).

API Unity: không dùng API mới (Core là C# thuần; client chỉ đổi lời gọi).

## Quyết định đã chốt (không được đổi)
1. **Số** (0034 mục 6, [số tạm] đã có trong JSON): túi 10 ô, túi phụ 2 ô, 10 ô mỗi trang (cả hai), 999/ô. Không đổi JSON.
2. **Chủ sở hữu:** `PlayerBag` có `public SlotInventory Main { get; }` (túi) và `public SlotInventory Hotbar { get; }` (túi phụ), cùng `StackMax`. `PlayerBag` cài `IItemStore` trên **cả hai** (túi gồm túi phụ — 0034 "số lượng trong túi (gồm túi phụ)").
3. **Kích thước mặc định từ JSON:** `PlayerBag.Configure(PlayerConfig)` (tĩnh) lưu 5 số; `PlayerRules.Configure` gọi nó (ConfigLoader đã gọi `PlayerRules.Configure`). `new PlayerBag()` dùng số đã lưu (mặc định trong code = 10 / 2 / 10 / 10 / 999 như `PlayerConfig`).
4. **Thứ tự thêm** (đề xuất Planner, `[số tạm]` cách làm — user xem ở lần chơi thử cuối): (a) lấp chồng **cùng loại** ở túi phụ, (b) lấp chồng cùng loại ở túi, (c) ô trống ở túi, (d) ô trống ở túi phụ. Lý do: đồ đang để ở túi phụ (dùng nhanh) được cộng dồn; đồ mới vào túi trước, túi phụ chỉ nhận khi túi hết chỗ.
5. **Thứ tự lấy theo loại** (`Remove`, `TryTake…`, đặt khối, gieo, chuyển vào kho theo loại) (đề xuất Planner): túi trước (từ ô cuối về đầu, như `SlotInventory.Remove`), túi phụ sau. Lý do: ô túi phụ giữ đồ lâu nhất, phím 1–0 dùng được lâu.
6. **Event** `Changed` / `SeedsChanged` / `ProduceChanged`: phát một lần sau mỗi lệnh `Add` / `Remove` (và các hàm cũ gọi vào chúng) **khi số thay đổi > 0**, tham số = **tổng mới** (túi + túi phụ). Sửa thẳng `Main` / `Hotbar` (kéo thả 0063) không phát các event này (chỉ `SlotChanged` của `SlotInventory`).
7. **Camera quan sát** là vật phẩm thật: `TerrainChunkView` thêm `ItemId.Tool(ToolItem.ObservationCamera)` × 1 ngay sau khi tạo túi (vào ô túi 0). `PlayerBag.Add` nhận `Tool` (bỏ `NotSupportedException`). Không mất khi dùng (đã như vậy).
8. **Túi đầy khi nhặt** (0034 mục 6 "Túi đầy → vật phẩm không vào được thì nằm lại", đề xuất Planner đã ghi trong 0034):
   - nhặt được bao nhiêu lấy bấy nhiêu; phần còn lại ở nguyên chồng vật rơi (cùng ô);
   - `Collected` phát khi lấy được ≥ 1 (kể cả lấy một phần — vật bay về nhân vật);
   - `DropField.LastBlocked` (mới) = số chồng ở lần nhặt gần nhất còn lại vì túi đầy.
   - Client: F / chuột phải nhặt khi túi đầy → dòng kết quả `Túi đầy` (khoá mới `bag.full`).
9. **Túi đầy khi thu hoạch** (người chơi tự thu hoạch, `PlayerHarvest`): phần nông sản / hạt không vào túi → vật rơi ở ô ngay trên ô đất ruộng (`Soil.Y + 1`), nếu `Editor != null` và ô đó trong world, không đặc. Không có `Editor` → phần dư mất (chỉ xảy ra trong test không gắn editor).
10. **Lấy từ kho chung** (`TakeFromStock`): số lấy = min(amount, số trong kho, `bag.CanAdd`). `StoreToStock` không đổi luật.
11. **Không làm ở brief này:** giao diện túi / túi phụ / kho ô vuông, phím 1–0, P, bỏ phím 1–4 / K / Alt (0062); kéo thả, Tab, 1/2/3 (0063). Không sửa `SimView.cs`, `GameControls.inputactions`, `StockStore.cs`, `ItemTransfer.cs`.

## Phạm vi
- Được sửa:
  - Core: `Core/PlayerBag.cs` (viết lại), `Core/SlotInventory.cs` (chỉ thêm `TopUp`), `Core/DropField.cs` (chỉ `Take`, `CollectInReach`, `TryCollect`, thuộc tính `LastBlocked`), `Core/PlayerRules.cs` (1 dòng trong `Configure`), `Core/Sim/Simulation.cs` (chỉ nhánh Harvest của `OnStageDone`, hàm mới `DropOverflow`, `TakeFromStock`).
  - Client: `Client/TerrainChunkView.cs` (1 dòng sau `Bag = new PlayerBag();`), `Client/TerrainBench.cs` (dòng 94), `Client/TerrainEditInput.cs` (chỉ `CollectNearby`, `CollectDrop`, `CanHold` case Tool).
  - Chuỗi: `Client/Resources/Lang/vi.json`, `loc/en.json`, `loc/context.json` (thêm `bag.full`).
  - Test: `Tests/Core/PlayerBagTests.cs`, `Tests/Core/SlotInventoryTests.cs`, `Tests/Core/DropFieldTests.cs`, `Tests/Core/Sim/PlayerFarmTests.cs`; test cũ khác chỉ theo quy tắc bước 8.
  - File brief này (mục Kết quả / Kết quả test / Vướng mắc).
- Không được sửa: `Core/ItemTransfer.cs`, `Core/Sim/StockStore.cs`, `Core/Sim/SimConfig.cs`, `Core/WorldEditor.cs`, `Client/SimView.cs`, `Client/Input/GameControls.inputactions`, `design/` (cả JSON), task contract, mọi file khác.

## Các bước
1. **`SlotInventory.TopUp`** (`Core/SlotInventory.cs`, ngay sau `Add`):
   ```csharp
   /// <summary>Chỉ lấp các ô đang có cùng loại (không mở ô trống), theo chỉ số tăng; trả số đã thêm.</summary>
   public int TopUp(ItemId item, int amount)
   {
       CheckAmount(amount);
       int left = amount;
       for (int i = 0; i < Capacity && left > 0; i++)
           if (counts[i] > 0 && items[i] == item)
               left -= Put(i, item, left);
       return amount - left;
   }
   ```
   Xong khi: biên dịch không lỗi.
2. **Viết lại `Core/PlayerBag.cs`** — giữ tên lớp, namespace `Game.Core`, thêm `using Game.Core.Sim;`. Nội dung (summary tiếng Việt như file cũ, cập nhật theo 0034):
   ```csharp
   /// <summary>
   /// Túi đồ của người chơi (0028, 0034 mục 6): túi Main + túi phụ Hotbar, ô vuông, StackMax mỗi ô.
   /// Thêm: lấp chồng cùng loại ở túi phụ → ở túi → ô trống túi → ô trống túi phụ. Lấy theo loại: túi trước, túi phụ sau (brief 0061).
   /// Event phát tổng mới (túi + túi phụ) khi số đổi; sửa thẳng Main / Hotbar thì không phát.
   /// </summary>
   public sealed class PlayerBag : IItemStore
   {
       public static int DefaultBagSlots { get; private set; } = 10;
       public static int DefaultHotbarSlots { get; private set; } = 2;
       public static int DefaultBagPageSize { get; private set; } = 10;
       public static int DefaultHotbarPageSize { get; private set; } = 10;
       public static int DefaultStackMax { get; private set; } = 999;

       /// <summary>Lưu kích thước túi từ cấu hình (PlayerRules.Configure gọi). null → ArgumentNullException.</summary>
       public static void Configure(PlayerConfig p) { ... gán 5 số từ p.bagSlots, p.hotbarSlots, p.bagPageSize, p.hotbarPageSize, p.stackMax ... }

       public PlayerBag() : this(DefaultBagSlots, DefaultHotbarSlots, DefaultBagPageSize, DefaultHotbarPageSize, DefaultStackMax) { }
       public PlayerBag(int bagSlots, int hotbarSlots, int bagPageSize, int hotbarPageSize, int stackMax)
       {
           Main = new SlotInventory(bagSlots, bagPageSize, stackMax);
           Hotbar = new SlotInventory(hotbarSlots, hotbarPageSize, stackMax);
       }

       /// <summary>Túi (0034 mục 6).</summary>
       public SlotInventory Main { get; }
       /// <summary>Túi phụ (giữa-dưới màn hình, phím 1–0).</summary>
       public SlotInventory Hotbar { get; }

       public event Action<int, int> SeedsChanged;   // (loại cây, tổng mới)
       public event Action<int, int> ProduceChanged; // (loại cây, tổng mới)
       public event Action<BlockMaterial, int> Changed;

       public int Count(ItemId item) => Main.Count(item) + Hotbar.Count(item);
       public int CanAdd(ItemId item, int amount) => (int)Math.Min((long)Main.CanAdd(item, amount) + Hotbar.CanAdd(item, amount), amount);
       public int Add(ItemId item, int amount)
       {
           int left = amount;            // amount < 0 → SlotInventory ném ArgumentOutOfRangeException
           left -= Hotbar.TopUp(item, left);
           left -= Main.TopUp(item, left);
           left -= Main.Add(item, left);   // lúc này chỉ còn ô trống
           left -= Hotbar.Add(item, left);
           int n = amount - left;
           if (n > 0) Raise(item);
           return n;
       }
       public int Remove(ItemId item, int amount)
       {
           int n = Main.Remove(item, amount);
           n += Hotbar.Remove(item, amount - n);
           if (n > 0) Raise(item);
           return n;
       }
       void Raise(ItemId item) { switch (item.Kind) { Block → Changed?.Invoke(item.Material, Count(item)); SeedBag → SeedsChanged?.Invoke(item.Crop, Count(item)); Produce → ProduceChanged?.Invoke(item.Crop, Count(item)); Tool → không phát } }
   ```
   Hàm cũ giữ tên, đổi kiểu trả của các hàm Add từ `void` → `int` (số đã thêm):
   - `public int Get(BlockMaterial material) => Count(ItemId.Block(material));`
   - `public int Add(BlockMaterial material, int amount = 1)` → `amount < 0` ném `ArgumentOutOfRangeException(nameof(amount), amount, "Amount to add must be ≥ 0")` (giữ thông điệp cũ); trả `Add(ItemId.Block(material), amount)`.
   - `public bool TryTake(BlockMaterial material) => Remove(ItemId.Block(material), 1) == 1;`
   - `GetSeeds(int crop)` / `GetProduce(int crop)`: `CheckCrop(crop)` rồi `Count(...)`.
   - `public int AddSeeds(int crop, int amount)` / `public int AddProduce(int crop, int amount)`: `CheckCrop`, kiểm `amount < 0` như cũ, trả `Add(...)`.
   - `TryTakeSeeds(int crop, int amount)` / `TryTakeProduce`: `CheckCrop`, `amount < 0` ném như cũ; `Count < amount` → false; `amount == 0` → true; còn lại `Remove(..., amount)`, true.
   - `CheckCrop` giữ nguyên.
   - Xoá hẳn 3 mảng đếm và hàm `Add(ItemId, int)` cũ (đã thay bằng hàm `IItemStore` ở trên — cùng chữ ký nhưng trả `int`, nhận `Tool`).
   - Xong khi: biên dịch không lỗi; `grep -n "UnityEngine" client/Assets/Game/Core/PlayerBag.cs` rỗng.
3. **`PlayerRules.Configure`** (`Core/PlayerRules.cs`): sau dòng `StarterBag.Seeds = p.starterSeeds;` thêm `PlayerBag.Configure(p);`. Summary `Configure` nối "; kích thước túi (PlayerBag.Configure)".
4. **`DropField`** (`Core/DropField.cs`):
   - Thuộc tính mới (cạnh event `Collected`):
     ```csharp
     /// <summary>Số chồng còn lại vì túi đầy ở lần CollectInReach / TryCollect gần nhất (0034 mục 6).</summary>
     public int LastBlocked { get; private set; }
     ```
   - `Take` đổi thành trả số đã lấy:
     ```csharp
     int Take(DropStack s, PlayerBag bag)
     {
         int n = bag.Add(s.Item, s.Count);
         if (n <= 0) return 0;
         Collected?.Invoke(s);
         if (n >= s.Count) Remove(s);
         else s.Count -= n;
         return n;
     }
     ```
   - `CollectInReach`: đầu hàm `LastBlocked = 0;`. Trong vòng lặp: `int before = s.Count; int got = Take(s, bag); if (got > 0) n++; if (got < before) LastBlocked++;`. `Version++` khi `n > 0` (như cũ). Trả `n` = số chồng lấy được ≥ 1.
   - `TryCollect`: đầu hàm `LastBlocked = 0;`; sau khi kiểm tầm: `int before = s.Count; int got = Take(s, bag); if (got < before) LastBlocked = 1; if (got == 0) return false; Version++; return true;`.
   - Summary 2 hàm nối "Túi đầy: lấy được bao nhiêu lấy bấy nhiêu, phần còn lại nằm lại (LastBlocked)."
5. **`Simulation`** (`Core/Sim/Simulation.cs`):
   - Nhánh `if (bag != null)` của `case WorkAbility.Harvest:` thay 2 dòng `:778–779` bằng
     ```csharp
     DropOverflow(p.Soil, ItemId.Produce(crop), cd.producePerHarvest - bag.AddProduce(crop, cd.producePerHarvest));
     DropOverflow(p.Soil, ItemId.SeedBag(crop), cd.seedsPerHarvest - bag.AddSeeds(crop, cd.seedsPerHarvest));
     ```
   - Hàm mới (cạnh `CompleteByPlayer`):
     ```csharp
     /// <summary>Túi đầy khi người chơi thu hoạch (0034 mục 6): n > 0 → vật rơi ở ô ngay trên ô đất ruộng; không có Editor / ô đó đặc / ngoài world → bỏ.</summary>
     void DropOverflow(CellPos soil, ItemId item, int n)
     {
         if (n <= 0 || Editor == null) return;
         int y = soil.Y + 1;
         if (!World.Size.InBounds(soil.X, y, soil.Z) || WorldEditor.IsSolidAt(World, soil.X, y, soil.Z)) return;
         Editor.Drops.Add(soil.X, y, soil.Z, item, n);
     }
     ```
     (`World` là `WorldData` của Simulation — kiểm tên thuộc tính trong file; khác tên → dùng đúng tên đang có, ghi vào Kết quả.)
   - `TakeFromStock`: sau dòng `if (n <= 0) return 0;` thêm
     ```csharp
     n = Math.Min(n, bag.CanAdd(produce ? ItemId.Produce(cropId) : ItemId.SeedBag(cropId), n)); // túi giới hạn ô (0034 mục 6)
     if (n <= 0) return 0;
     ```
     Summary nối "; không vượt chỗ trống của túi".
6. **Client:**
   - `Client/TerrainChunkView.cs`: ngay sau `Bag = new PlayerBag();` thêm `Bag.Add(ItemId.Tool(ToolItem.ObservationCamera), 1); // camera quan sát là vật phẩm trong túi (0034 mục 4, brief 0061)`.
   - `Client/TerrainBench.cs:94`: `view.Bag.Add(m, 100000);` → `view.Bag.Add(m, view.Bag.Main.StackMax);` (túi giới hạn ô; bench đặt / đào xen kẽ nên 999 mỗi loại đủ).
   - `Client/TerrainEditInput.cs`:
     - `CanHold` `case ItemKind.Tool:` → `return item.ToolKind == ToolItem.ObservationCamera && view.Bag != null && view.Bag.Count(item) > 0;`.
     - `CollectNearby`: thay dòng `lastResult = ...` bằng
       ```csharp
       bool full = view.Editor.Drops.LastBlocked > 0;
       lastResult = n > 0 ? Format("edit.collect_ok", n) + (full ? " · " + T("bag.full") : "")
           : full ? T("bag.full") : Format("edit.collect_none", PlayerRules.CollectReach);
       ```
     - `CollectDrop`: dòng `if (!view.Editor.Drops.TryCollect(...)) return false;` → `if (!view.Editor.Drops.TryCollect(...)) { if (view.Editor.Drops.LastBlocked > 0) lastResult = T("bag.full"); return false; }`; sau khi lấy được: `lastResult = Format("edit.pick", label) + (view.Editor.Drops.LastBlocked > 0 ? " · " + T("bag.full") : "");`.
   - Xong khi: biên dịch không lỗi.
7. **Chuỗi** — thêm vào cả 3 file (đúng vị trí bảng chữ cái, ngay trước `bag.hint` vì `bag.full` < `bag.hint`):

   | key | vi | en | context |
   |---|---|---|---|
   | `bag.full` | `Túi đầy` | `Bag full` | `Result line when the player's bag (10 slots + 2 quick-bar slots, 999 per slot) has no room: the item stays on the ground as a drop. 'Túi' = inventory bag.` |

   Xong khi: parse JSON 3 file được; `loc_check.py check loc/en.json` exit 0.
8. **Test** (chuỗi tiếng Việt trong assert ghi `// vi-text`):
   - `Tests/Core/SlotInventoryTests.cs` — **`TopUpOnlyExistingStacks`**: `inv = new SlotInventory(3, 10, 5)`; `AddToSlot(1, dirt, 3)`; `TopUp(dirt, 4)` trả 2, ô 1 = 5, ô 0 và 2 trống; `TopUp(stone, 3)` trả 0; `TopUp(dirt, -1)` ném `ArgumentOutOfRangeException`.
   - `Tests/Core/PlayerBagTests.cs` (thêm `[TearDown] PlayerBag.Configure(new Game.Core.Sim.PlayerConfig());`):
     - **`DefaultSizesFromConfig`**: `new PlayerBag()` → `Main.Capacity 10`, `Hotbar.Capacity 2`, `Main.PageSize 10`, `Hotbar.PageSize 10`, `Main.StackMax 999`; `PlayerBag.Configure(new PlayerConfig { bagSlots = 20, hotbarSlots = 4, stackMax = 50 })` → `new PlayerBag()` có 20 / 4 / 50; `Configure(null)` ném `ArgumentNullException`.
     - **`AddFillsBagThenHotbar`**: `b = new PlayerBag(2, 1, 10, 10, 5)`; `Add(Dirt, 7)` trả 7 (Main ô 0 = 5, ô 1 = 2); `Add(Stone, 6)` trả 5 (Hotbar ô 0 = đá 5); `CanAdd(ItemId.Block(Stone), 1)` = 0; `CanAdd(ItemId.Block(Dirt), 9)` = 3; `Add(Dirt, 9)` trả 3; `Get(Dirt)` = 10.
     - **`AddTopsUpHotbarFirst`**: `b = new PlayerBag(3, 1, 10, 10, 999)`; `b.Hotbar.AddToSlot(0, dirt, 5)`; `b.Add(Dirt, 3)` → Hotbar ô 0 = 8, mọi ô Main trống.
     - **`RemoveTakesBagBeforeHotbar`**: `b = new PlayerBag(3, 1, 10, 10, 999)`; `Hotbar.AddToSlot(0, dirt, 2)`; `Main.AddToSlot(0, dirt, 3)`; `TryTake(Dirt)` true → Main ô 0 = 2, Hotbar ô 0 = 2; `Remove(ItemId.Block(Dirt), 4)` trả 4 → `Get(Dirt)` = 0.
     - **`ToolTakesASlot`**: `b = new PlayerBag(1, 1, 10, 10, 999)`; `Add(ItemId.Tool(ToolItem.ObservationCamera), 1)` trả 1; `Main.Get(0).Item` là camera; `Add(Dirt, 2)` trả 2 (vào túi phụ); `Add(Stone, 1)` trả 0.
     - **`EventsOnlyWhenChanged`**: túi đầy (`new PlayerBag(1, 1, 10, 10, 999)`, thêm đá + cát) → `AddSeeds(0, 3)` trả 0, không có event `SeedsChanged`; túi còn chỗ: `AddProduce` phát `(crop, tổng mới)`.
     - **`TryTakeSeedsAcrossBagAndHotbar`**: `Main.AddToSlot(0, ItemId.SeedBag(0), 3)`, `Hotbar.AddToSlot(0, ItemId.SeedBag(0), 2)`; `TryTakeSeeds(0, 4)` true, `GetSeeds(0)` = 1; `TryTakeSeeds(0, 2)` false, vẫn 1.
     - Test cũ trong file giữ nguyên ý nghĩa (chỉ sửa nếu bị lỗi do `void` → `int`).
   - `Tests/Core/DropFieldTests.cs`:
     - **`CollectStopsWhenBagFull`**: thay túi + editor riêng: `bag = new PlayerBag(1, 1, 10, 10, 999); editor = new WorldEditor(world, bag, 2);` rồi `bag.Add(Stone, 1); bag.Add(Sand, 1);`; `Drops.Add(1, 1, 0, B(Dirt), 2)`; đếm `Collected`; `CollectInReach(0.5f, 0.5f, 0.5f, bag)` trả 0; chồng còn 2; `LastBlocked` = 1; không có `Collected`. `TryCollect(id, …)` trả false, `LastBlocked` = 1.
     - **`CollectPartialStack`**: `bag = new PlayerBag(1, 1, 10, 10, 5)` (+ editor mới như trên); `bag.Add(Dirt, 3); bag.Hotbar.AddToSlot(0, B(Sand), 1);` (túi phụ đầy loại khác); `Drops.Add(1, 1, 0, B(Dirt), 4)`; `CollectInReach` trả 1; `bag.Get(Dirt)` = 5; chồng còn 2 ở `(1, 1, 0)`; `LastBlocked` = 1; `Collected` 1 lần.
   - `Tests/Core/Sim/PlayerFarmTests.cs`:
     - **`HarvestOverflowDropsOnPlot`**: `Layout(false)`; `ToHarvest(0)`; `var small = new PlayerBag(1, 1, 10, 10, 999); small.Add(Stone, 1); small.Add(Sand, 1);` → `sim.PlayerHarvest(id, small)` true; `small.GetProduce(0)` = 0; `sim.Editor.Drops.At(Soil.X, Soil.Y + 1, Soil.Z)` có `Produce(0)` × `producePerHarvest` và `SeedBag(0)` × `seedsPerHarvest` của cây 0 (lấy từ `sim.Config.crops[0]`).
     - **`TakeFromStockLimitedByBag`**: `Layout(false)`; `var small = new PlayerBag(1, 1, 10, 10, 5); small.Add(Stone, 1);` (còn 1 ô túi phụ × 5) → `sim.TakeFromStock(small, 0, false, 999)` trả 5; `small.GetSeeds(0)` = 5; kho giảm đúng 5.
   - Xong khi: các test trên có trong file, biên dịch không lỗi.
9. **Test cũ hỏng** — quy tắc (giữ ý nghĩa test):
   - Lỗi biên dịch do `void` → `int`: không xảy ra với lời gọi bỏ giá trị; nếu có (vd gán delegate `Action`) → bọc lambda. Ghi tên.
   - Hỏng vì **túi hết ô** (test thêm > 12 loại vật phẩm hoặc > 999 một loại vào túi mặc định): tạo túi lớn hơn trong test đó (`new PlayerBag(100, 2, 10, 10, 100000)`) **chỉ khi** test không phải về túi. Ghi tên.
   - Hỏng vì camera quan sát giờ chiếm 1 ô túi (PlayMode — đếm ô / đếm dòng túi): sửa số mong đợi +1 ô, ghi tên. `BagRows` vẫn thêm dòng camera cuối như cũ (không sửa SimView) → nếu test thấy 2 dòng camera thì **dừng, `cần Opus`** (không xảy ra vì `BagRows` không đọc Tool từ túi).
   - Lý do khác → dừng, `cần Opus`.

## Tiêu chí xong
- EditMode + PlayMode qua hết, 0 lỗi console (qua MCP).
- `grep -rn "UnityEngine" client/Assets/Game/Core` như trước brief (rỗng).
- `loc_check.py check loc/en.json` exit 0; `python3 tools/check_design.py` exit 0.
- `git diff --stat <base>..HEAD -- design/ client/Assets/Game/Client/SimView.cs client/Assets/Game/Core/ItemTransfer.cs client/Assets/Game/Core/Sim/StockStore.cs` rỗng.
- Mỗi test ở bước 8 có trong file và qua.

## Test sau khi xong

### Phần cần test
- `TopUp` (bước 1) · `SlotInventoryTests.TopUpOnlyExistingStacks` · chỉ lấp ô cùng loại.
- Túi ô vuông qua `PlayerBag` (bước 2, 3) · `PlayerBagTests.DefaultSizesFromConfig`, `AddFillsBagThenHotbar`, `AddTopsUpHotbarFirst`, `RemoveTakesBagBeforeHotbar`, `ToolTakesASlot`, `EventsOnlyWhenChanged`, `TryTakeSeedsAcrossBagAndHotbar` + test cũ của file · đúng thứ tự thêm / lấy, giới hạn ô, event.
- Nhặt khi túi đầy (bước 4, 6) · `DropFieldTests.CollectStopsWhenBagFull`, `CollectPartialStack` + toàn bộ `DropFieldTests` cũ · phần dư nằm lại, `LastBlocked`, `Collected`.
- Thu hoạch dư + lấy từ kho (bước 5) · `PlayerFarmTests.HarvestOverflowDropsOnPlot`, `TakeFromStockLimitedByBag` + toàn bộ `PlayerFarmTests` cũ (`HarvestIntoBag`, `TakeFromStock`, `StoreToStock…`) · vật rơi trên ô ruộng; không vượt chỗ trống.
- Chuỗi `bag.full` (bước 7) · `HardcodedTextScanTests`, `loc_check.py` · không thiếu khoá.
- Regression lõi dùng túi · `WorldEditorTests`, `SeaFloodTests`, `StarterBagTests`, `PlotRulesTests`, `HomeTests`, `FarmTests`, `CropQueueTests`, `ItemTransferTests`, `SlotInventoryTests`, `PlayerRulesTests` · qua.
- Regression client dùng túi (đặt khối, gieo, nhặt, camera quan sát, bảng túi IMGUI cũ, chuyển kho cũ) · toàn bộ `ControlModePlayTests`, `PlayerBagSowPlayTests`, `DropPlayTests`, `TerrainEditPlayTests`, `PlayerFarmPlayTests`, `SimFarmUiPlayTests`, `ObservationCameraPlayTests`, `SettingsPlayTests` · qua (sửa theo bước 9, ghi tên).
- Toàn bộ EditMode + PlayMode · qua.

### Lệnh executor chạy
1. Parse JSON (vi.json, en.json, context.json) + `python3 "<Plugin root>/skills/translation/scripts/loc_check.py" check loc/en.json` + `grep -rn "UnityEngine" client/Assets/Game/Core` + `python3 tools/check_design.py`.
2. Unity Editor mở qua MCP ([0010](../../design/decisions/0010-mcp-for-unity.md)): `refresh_unity` → `read_console` (0 lỗi biên dịch) → `run_tests` EditMode (toàn bộ) → `run_tests` PlayMode (toàn bộ) → `read_console` lỗi. Ghi dòng tổng kết số test thật. Editor tắt → `sh "<Plugin root>/scripts/run.sh" verify_run client_test client_test_play`.
3. **Dọn:** sau `run_tests` PlayMode → `git checkout -- client/ProjectSettings/EditorSettings.asset`. Không commit `client/Assets/InitTestScene*`.
4. `sh "<Plugin root>/scripts/run.sh" verify_run --all` trước commit (client_test / client_test_play exit 1 vì Editor mở → ghi `Chưa kiểm được` + kết quả MCP).
5. Commit code + test + chuỗi (chỉ file trong Phạm vi), message `brief 0061: 7l d1 slot-based player bag (bag + hotbar, stack cap, overflow stays as drops)`; commit kết quả brief riêng `brief 0061: result`. Không push.
6. Ghi giờ bắt đầu / kết thúc **mọi vòng** vào Kết quả (giờ thực tế M1).

### User tự test
Gom vào danh sách chơi thử cuối của task (user 2026-10-24: phần "user xem" của 7l d, e, f + sim 7g làm một lần ở cuối). Mục cần xem cho brief này (Planner chép vào danh sách cuối):
1. Đào nhiều loại khối tới khi túi đầy (10 ô túi + 2 ô túi phụ; camera quan sát chiếm 1 ô) → F không nhặt được nữa, dòng `Túi đầy`, khối còn nằm dưới đất.
2. Thu hoạch khi túi đầy → nông sản / hạt rơi trên ô ruộng.
3. Thứ tự thêm (túi phụ cộng dồn trước) / lấy (túi trước) có hợp lý không — đề xuất Planner, đổi được.

## Dừng và chuyển lại Opus khi
- Brief mơ hồ hoặc mâu thuẫn với code thật (vd chữ ký ở "Bối cảnh" khác file thật).
- Cần một quyết định thuộc AGENTS.md §3.4 hoặc ngoài "Quyết định đã chốt".
- Cùng một lỗi sửa 2 lần vẫn chưa qua.
- Phải sửa file ngoài "Phạm vi".
- Test cũ hỏng mà không thuộc quy tắc bước 9.

---
<!-- Từ đây trở xuống: EXECUTOR ghi. -->

## Kết quả
- Base commit: 45f5369
- Chạy bằng: claude-sonnet-5-5 · medium
- Bắt đầu: 2026-10-24 03:46
- Kết thúc: 2026-10-24 04:02 (một vòng; phần lớn là đợi biên dịch + EditMode ~10 phút).
- Commit: 7d51dc5 (`brief 0061: 7l d1 slot-based player bag ...`).
- File đã đổi: Core/PlayerBag.cs (viết lại), SlotInventory.cs (TopUp), DropField.cs, PlayerRules.cs, Sim/Simulation.cs; Client/TerrainChunkView.cs, TerrainBench.cs, TerrainEditInput.cs; Lang/vi.json, loc/en.json, loc/context.json (`bag.full`); Tests: SlotInventoryTests, PlayerBagTests, DropFieldTests, PlayerFarmTests.
- Lệch so với brief: không. `Simulation.World` đúng tên như brief. Test `PlayerBagTests.TryTakeSeeds` cũ: bỏ dòng `Assert.Throws<NotSupportedException>(Add Tool)` (hành vi đã đổi theo quyết định 7: Tool được nhận). Không test cũ nào khác hỏng; không áp dụng quy tắc bước 9 khác.

## Kết quả test
- Lệnh 1: parse JSON 3 file OK; `loc_check.py check loc/en.json` exit 0 (178/178); `grep UnityEngine Core` rỗng; `check_design.py` exit 0.
- Lệnh 2 (MCP): biên dịch 0 lỗi console. EditMode lần 1: 541/542 — `IncrementalRebuildTests.IncrementalRebuildMatchesFullRebuild` quá timeout 180 s (205 s; Editor không focus, test mesh không liên quan túi; chạy trước 0061 qua trong brief 0060); chạy lại riêng → **qua** (40 s). Như vậy EditMode **542/542 qua** (lần 1 + chạy lại). PlayMode **230/230 qua**. Console 0 lỗi sau chạy.
- Lệnh 3: đã `git checkout -- client/ProjectSettings/EditorSettings.asset`; không commit `InitTestScene*`.
- Lệnh 4 `verify_run --all`: design exit 0, asset_check exit 0; client_test / client_test_play exit 1 vì Editor mở → **Chưa kiểm được** bằng dòng lệnh; kết quả MCP thay thế.
- Phần cần test: TopUp, túi ô vuông qua PlayerBag (7 test mới + cũ), nhặt khi túi đầy (2 test mới + DropFieldTests cũ), thu hoạch dư + lấy từ kho (2 test mới + PlayerFarmTests cũ), chuỗi `bag.full` (HardcodedTextScanTests + loc_check), regression lõi + client + toàn bộ EditMode/PlayMode → **đạt** (trong 542 + 230 test qua).
- User tự test (3 mục trong brief) → **chờ user — gộp cuối** (user AFK, theo điều phối của Planner).

## Vướng mắc

---
<!-- PLANNER ghi khi review. -->

## Review
**Kết luận: code ok, chờ user xem gộp cuối** (user 2026-10-24: phần "user xem" của 7l d–f + sim 7g gộp một lần ở cuối task; không chờ user) → trạng thái **đã duyệt**.

Đối chiếu (review đầy đủ, đọc diff `7d51dc5`):
- `PlayerBag` đúng Quyết định 2–6: `Main` / `Hotbar`, cài `IItemStore`; thứ tự thêm túi phụ lấp → túi lấp → ô trống túi → ô trống túi phụ; lấy túi trước rồi túi phụ; `CanAdd` cộng `long` rồi cắt; event chỉ khi n > 0, tham số tổng mới; Tool không phát event. `Configure` tĩnh, gọi từ `PlayerRules.Configure` (`Core/PlayerRules.cs:27`).
- `SlotInventory.TopUp` đúng mẫu brief. `DropField.Take` (`Core/DropField.cs:212–220`): lấy một phần, `Collected` khi ≥ 1, chồng còn lại giữ ô; `LastBlocked` đặt lại đầu mỗi lần nhặt.
- `Simulation.DropOverflow` + nhánh Harvest + `TakeFromStock` giới hạn `CanAdd` đúng Quyết định 9–10.
- Client: camera quan sát vào ô túi khi tạo túi; `CanHold(Tool)` cần có trong túi; dòng `Túi đầy` ở F / chuột phải nhặt; bench 999 mỗi loại.
- Test: 10 test mới đúng tên brief; một assert cũ (`Add Tool` ném `NotSupportedException`) bỏ đúng theo Quyết định 7. EditMode 542/542 (1 test mesh quá giờ lần đầu do Editor không focus, chạy lại riêng qua — không liên quan túi), PlayMode 230/230, console 0 lỗi [kết quả executor qua MCP]. Planner không chạy lại (kết quả đủ, không đáng ngờ).
- Phạm vi: không đụng `SimView`, `ItemTransfer`, `StockStore`, `design/`.

Ghi chú (không cần sửa): `new PlayerBag()` dùng số đã `Configure`; nếu một scene tạo túi trước khi `ConfigLoader.LoadSimConfig` chạy thì dùng mặc định trong code — hiện trùng JSON (10/2/10/10/999) nên không lệch; khi user đổi `bagSlots` trong JSON để thử, kiểm lại thứ tự này (ghi vào danh sách chơi thử cuối).

Giờ thực tế: Executor 03:46–04:02 = **16 phút** (1 vòng) so với ước lượng 1–1,5 ngày-người cho phần d1.

Danh sách user xem gộp cuối (từ brief này): 3 mục ở "User tự test" + thử đổi `player.bagSlots` trong JSON xem túi nhận đúng số ô.
