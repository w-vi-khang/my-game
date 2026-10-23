# Brief 0054 — điều khiển PC A: luật lõi (tầm, máu/mana, túi ô vuông, chuyển đồ, chuột phải)

> Trạng thái: đã duyệt
> Review: đầy đủ
> Model thực thi: Sonnet · medium
> Cập nhật: 2026-10-23

<!-- Planner (Opus) viết, Executor (Sonnet) làm theo ở PHIÊN KHÁC — executor không có lịch sử chat, không được tự quyết. -->

## Mục tiêu
Thêm vào `Game.Core` (C# thuần) phần luật của bước world **7l a** ([task contract](../tasks/m1-world-7l-dieu-khien-pc.md), quyết định [0034](../../design/decisions/0034-dieu-khien-pc-click-di-chuyen.md)). Gồm:
- tầm thao tác 10 / tầm thực hiện 2;
- máu + mana của nhân vật;
- túi ô vuông (giới hạn ô, 999/ô, trang);
- chuyển đồ túi ↔ túi phụ ↔ kho;
- trạng thái chế độ / vật cầm;
- thứ tự ưu tiên của chuột phải.

Các số đặt trong `design/data/sim_m1.json`. **Brief này chỉ thêm luật + test EditMode. Client chưa dùng luật mới**: chơi không đổi gì; nối vào client là việc của 7l b–d.

## Bối cảnh cần đọc
- `client/Assets/Game/Core/PlayerRules.cs:10-45`: tầm hiện có. Mẫu cần theo:
  - `public static float ToolReach { get; private set; } = 8f;` (dòng 14);
  - `public static void Configure(PlayerConfig p)` (dòng 20–27);
  - `static bool Within(float reach, float feetX, float feetY, float feetZ, int x, int y, int z)` (dòng 41–45): Euclid từ tâm thân (chân + 1) tới tâm ô, `<= reach*reach + 1e-4f`.
- `client/Assets/Game/Core/Sim/SimConfig.cs`:
  - `PlayerConfig` ở dòng 126–135: `[Serializable] public class PlayerConfig { public int rainCooldownMs = 10000; public float toolReach = 8f; public float handReach = 8f; public int starterCrop = 0; public int starterSeeds = 4; }`;
  - kiểm số của player trong `Validate()` ở dòng 319–328 (mẫu `throw new ArgumentException("player.toolReach must be in [1, 32].")`).
- `design/data/sim_m1.json:33`: dòng `"player": { "rainCooldownMs": 10000, "toolReach": 8, "handReach": 8, "starterCrop": 0, "starterSeeds": 4 }`.
  - Bản sao ở `client/Assets/Game/Client/Resources/Config/sim_m1.json` phải **giống hệt** (test `Game.World.Tests.ConfigTests` so hai file).
  - `configVersion` giữ **5** (`SimConfigTests` dòng 32 kiểm 5).
- `client/Assets/Game/Core/DropField.cs:6-29`:
  - `public enum ItemKind : byte { Block = 0, SeedBag = 1 }`;
  - `public readonly struct ItemId : IEquatable<ItemId>`, gồm `Kind`, `Value`, `ItemId.Block(BlockMaterial)`, `ItemId.SeedBag(int crop)` (kiểm 0..255), `Material`, `Crop`, `ToString()` dòng 28.
- `client/Assets/Game/Core/PlayerBag.cs:99-106`: `public void Add(ItemId item, int amount)`. Hiện chỉ tách Block / còn lại = hạt.
- `client/Assets/Game/Core/Sim/SimData.cs:208-223`: `Stockpile` (`Capacity`, `int[] Seeds`, `int[] Produce`, `Reserved`, `Free => Capacity - SeedsTotal - ProduceTotal - Reserved`). `Simulation.State.Stock` là kho chung (`public readonly SimState State;`, `Simulation.cs:10`).
- Mẫu luật chuyển kho hiện có: `Simulation.StoreToStock` / `TakeFromStock` (`Simulation.cs:1083-1125`).
- Mẫu test có Simulation + kho: `client/Assets/Game/Tests/Core/Sim/PlayerFarmTests.cs:23-40` (`Layout(false)`: `TestWorlds.Flat(1)`, `new Simulation(TestWorlds.NoWander(), w)`, `sim.AddStorage(new CellPos(6, 2, 9))`).
- Mẫu test tầm: `client/Assets/Game/Tests/Core/PlayerRulesTests.cs`. Có `TearDown` gọi `PlayerRules.Configure(new PlayerConfig())`. Chân `(0.5f, -0.5f, 0.5f)` → tâm thân `(0.5, 0.5, 0.5)`, ô `(n, 0, 0)` cách đúng `n`.
- `client/Assets/Game/Tests/Core/Text/HardcodedTextScanTests.cs`: chuỗi trong code không thuộc `Tests/` không được có chữ Việt, kể cả không dấu (`kho`, `tui`, `ruong`…). **Mọi chuỗi (thông báo exception) viết tiếng Anh**; comment tiếng Việt vẫn được.

## Quyết định đã chốt (không được đổi)
- Tầm thao tác 10, tầm thực hiện 2, tầm đào 2, tầm Thu 2. Cùng cách đo `Within`. (0034 mục 3)
- **Giữ** `ToolReach` / `HandReach` / `InToolReach` / `InHandReach` và `toolReach` / `handReach` trong JSON (= 8), vì client hiện tại còn dùng; 7l c sẽ gỡ.
- Mana: tối đa 100; đào 5, đặt 3, Mưa 15; +2 mana cho mỗi 1 ô đi ngang. Máu: tối đa 100; rơi `f` ô mất `floor(f − 4) × 10` khi `f > 4`; ngập đầu mất 5 máu mỗi 1000 ms; sau 5000 ms không mất máu và không ngập đầu thì hồi 1 máu mỗi 1000 ms; máu về 0 → `Died` một lần; `Respawn()` đầy máu + mana. (0034 mục 5, số tạm)
- Túi 10 ô, túi phụ 2 ô, 10 ô mỗi trang (cả hai), tối đa 999/ô. Thêm vào: lấp chồng cùng loại trước (theo thứ tự ô), rồi ô trống đầu tiên. Lấy ra (theo loại): từ chồng có chỉ số ô **lớn nhất** trước. Trang vòng: sau trang cuối là trang 0. (0034 mục 6)
- Đích chuyển mặc định theo ưu tiên Bag > Hotbar > Stock, bỏ nơi nguồn; Stock chỉ là đích khi bảng kho đang mở. Tab = đích kế tiếp theo vòng ưu tiên đó. Phần chuyển: 1 → toàn bộ, 2 → `ceil(n/2)`, 3 → `ceil(n/3)`. (0034 mục 6)
- Kho chỉ nhận hạt + nông sản, giới hạn bởi `Stockpile.Free`. (0028, 0034)
- Kéo thả (`Move`): ô đích trống → chuyển cả chồng; cùng loại → gộp tới tối đa, phần dư ở lại; khác loại → đổi chỗ. (0034 mục 6)
- Chế độ và vật cầm loại trừ nhau. Bấm lại chế độ đang bật → về Mặc định. Cầm vật → chế độ về Mặc định. (0034 mục 3–4)
- Thứ tự chuột phải (0034 mục 3):

  | Thứ tự | Điều kiện | Kết quả |
  |---|---|---|
  | 1 | Ngoài tầm thao tác | `OutOfReach` |
  | 2 | Đang cầm vật **không phải khối** và dùng được ở chỗ click | `UseHeld` |
  | 3 | Chế độ khác Mặc định và có tác dụng ở chỗ click | Hành động của chế độ |
  | 4 | Hành động riêng của chỗ click | Thu hoạch > mở kho > nhặt |
  | 5 | Đang cầm **khối** và đặt được | `PlaceBlock` |
  | 6 | Còn lại | `None` |

  `Resolve` không kiểm mana; client kiểm sau.
- Không bump `configVersion` (thêm trường có mặc định, JSON cũ vẫn đọc được).

## Phạm vi
- **Được sửa:**
  - `client/Assets/Game/Core/PlayerRules.cs`
  - `client/Assets/Game/Core/Sim/SimConfig.cs`
  - `client/Assets/Game/Core/DropField.cs`
  - `client/Assets/Game/Core/PlayerBag.cs`
  - `design/data/sim_m1.json`
  - `client/Assets/Game/Client/Resources/Config/sim_m1.json`
  - `client/Assets/Game/Tests/Core/PlayerRulesTests.cs`
  - `client/Assets/Game/Tests/Core/PlayerBagTests.cs`
  - `client/Assets/Game/Tests/Core/Sim/SimConfigTests.cs`
- **Được tạo** (kèm file `.meta` do Unity sinh):
  - `client/Assets/Game/Core/PlayerVitals.cs`
  - `client/Assets/Game/Core/SlotInventory.cs`
  - `client/Assets/Game/Core/ItemTransfer.cs`
  - `client/Assets/Game/Core/ControlState.cs`
  - `client/Assets/Game/Core/Sim/StockStore.cs`
  - `client/Assets/Game/Tests/Core/PlayerVitalsTests.cs`
  - `client/Assets/Game/Tests/Core/SlotInventoryTests.cs`
  - `client/Assets/Game/Tests/Core/ItemTransferTests.cs`
  - `client/Assets/Game/Tests/Core/ControlStateTests.cs`
  - `client/Assets/Game/Tests/Core/Sim/StockStoreTests.cs`
- **Không được sửa:**
  - mọi file trong `client/Assets/Game/Client/` (trừ bản sao JSON) và `client/Assets/Game/World/`;
  - `Simulation.cs`, `SimData.cs`;
  - `design/` (trừ `sim_m1.json`);
  - `client/Assets/Game/Tests/Client/TerrainEditPlayTests.cs` — đang có thay đổi dở **không thuộc brief này**: không commit, không revert.

## Các bước
1. **Cấu hình.**
   - `SimConfig.cs` → `PlayerConfig`: thêm (giữ trường cũ) đúng tên, kiểu, mặc định:
     ```csharp
     public float targetReach = 10f;
     public float actReach = 2f;
     public float digReach = 2f;
     public float collectReach = 2f;
     public int manaMax = 100;
     public int manaDig = 5;
     public int manaPlace = 3;
     public int manaRain = 15;
     public int manaRegenPerCell = 2;
     public int hpMax = 100;
     public int fallSafeCells = 4;
     public int fallDamagePerCell = 10;
     public int drownDamagePerSecond = 5;
     public int hpRegenPerSecond = 1;
     public int hpRegenDelayMs = 5000;
     public int bagSlots = 10;
     public int hotbarSlots = 2;
     public int bagPageSize = 10;
     public int hotbarPageSize = 10;
     public int stackMax = 999;
     ```
   - Trong `Validate()`, ngay sau kiểm `starterSeeds` (dòng 327–328), thêm kiểm theo mẫu `ArgumentException("player.<tên> must be in [a, b].")`:
     - `targetReach` [1, 32];
     - `actReach`, `digReach` [0.5, targetReach];
     - `collectReach` [0.5, 32];
     - `manaMax`, `hpMax` [1, 10000];
     - `manaDig`, `manaPlace`, `manaRain`, `manaRegenPerCell` [0, manaMax];
     - `fallSafeCells` [0, 64];
     - `fallDamagePerCell`, `drownDamagePerSecond`, `hpRegenPerSecond` [0, hpMax];
     - `hpRegenDelayMs` [0, 600000];
     - `bagSlots` [1, 1000];
     - `hotbarSlots`, `bagPageSize`, `hotbarPageSize` [1, 100];
     - `stackMax` [1, 99999].

     Với float dùng dạng `!(x >= a && x <= b)` như dòng 321.
   - Hai file `sim_m1.json`: thay dòng `"player"` bằng đúng nội dung sau. Hai file phải giống nhau từng byte (sửa `design/data/` rồi chép đè bản client):
     ```json
         "player": {
             "rainCooldownMs": 10000, "toolReach": 8, "handReach": 8, "starterCrop": 0, "starterSeeds": 4,
             "targetReach": 10, "actReach": 2, "digReach": 2, "collectReach": 2,
             "manaMax": 100, "manaDig": 5, "manaPlace": 3, "manaRain": 15, "manaRegenPerCell": 2,
             "hpMax": 100, "fallSafeCells": 4, "fallDamagePerCell": 10, "drownDamagePerSecond": 5, "hpRegenPerSecond": 1, "hpRegenDelayMs": 5000,
             "bagSlots": 10, "hotbarSlots": 2, "bagPageSize": 10, "hotbarPageSize": 10, "stackMax": 999
         }
     ```
     File dùng 4 dấu cách cho khoá cấp 1 (đã kiểm), giữ đúng như trên. Không có dấu phẩy sau `}` vì `player` là khoá cuối (xem dòng 34 là `}` đóng file).
   - **Xong khi:** biên dịch sạch; `SimConfigTests` cũ qua.

2. **Tầm mới** (`PlayerRules.cs`).
   - Thêm 4 thuộc tính, cùng kiểu với `ToolReach`, mặc định 10 / 2 / 2 / 2:
     ```csharp
     public static float TargetReach { get; private set; } = 10f; // tầm thao tác (0034)
     public static float ActReach { get; private set; } = 2f;     // tầm thực hiện
     public static float DigReach { get; private set; } = 2f;     // tầm đào (nâng cấp sau)
     public static float CollectReach { get; private set; } = 2f; // Thu (F)
     ```
   - `Configure` gán thêm 4 trường từ `p`.
   - Thêm 4 hàm cùng chữ ký như `InToolReach`, gọi `Within`: `InTargetReach`, `InActReach`, `InDigReach`, `InCollectReach`.
   - Sửa comment của `ToolReach` / `HandReach` thành `/// <summary>... Client hiện tại còn dùng; 7l c (0034) chuyển sang TargetReach/ActReach rồi gỡ.</summary>`.
   - **Xong khi:** biên dịch sạch.

3. **Loại vật phẩm** (`DropField.cs`).
   - `public enum ItemKind : byte { Block = 0, SeedBag = 1, Produce = 2, Tool = 3 }`.
   - Thêm `public enum ToolItem : byte { ObservationCamera = 0 }` ngay trên `ItemId`.
   - Thêm vào `ItemId`:
     - `public static ItemId Produce(int crop)`: kiểm 0..255 y như `SeedBag`, cùng thông báo;
     - `public static ItemId Tool(ToolItem t) => new ItemId(ItemKind.Tool, (byte)t);`;
     - `public ToolItem ToolKind => (ToolItem)Value;`.
   - `ToString()` đổi sang `switch` trên `Kind`: `"Block:" + Material`, `"SeedBag:" + Crop`, `"Produce:" + Crop`, `"Tool:" + ToolKind`.
   - `PlayerBag.Add(ItemId, int)` (dòng 99–106) đổi sang `switch`:
     - `Block` → `Add(item.Material, amount)`;
     - `SeedBag` → `AddSeeds`;
     - `Produce` → `AddProduce(item.Crop, amount)`;
     - `Tool` → `throw new NotSupportedException("Tool items are not stored in PlayerBag (bag slots come in 7l d).")`.
   - **Xong khi:** biên dịch sạch; test cũ `DropFieldTests`, `PlayerBagTests` qua.

4. **`PlayerVitals.cs`** (namespace `Game.Core`; `using Game.Core.Sim;` để dùng `PlayerConfig`):
   ```csharp
   public enum ManaAction : byte { Dig = 0, Place = 1, Rain = 2 }

   /// Máu + mana của nhân vật (0034 mục 5). Số lấy từ PlayerConfig lúc tạo.
   public sealed class PlayerVitals
   {
       public PlayerVitals(PlayerConfig c);          // null → ArgumentNullException; Hp = HpMax, Mana = ManaMax
       public int Hp { get; }
       public int HpMax { get; }
       public int Mana { get; }
       public int ManaMax { get; }
       public bool IsDead => Hp <= 0;
       public event Action Changed;                  // sau mỗi lần Hp hoặc Mana đổi giá trị
       public event Action Died;                     // đúng 1 lần khi Hp từ > 0 về 0
       public int Cost(ManaAction a);                // manaDig / manaPlace / manaRain
       public bool CanSpend(ManaAction a);           // !IsDead && Mana >= Cost(a)
       public bool TrySpend(ManaAction a);           // CanSpend → Mana -= cost, Changed (nếu cost > 0), true; không → false, không đổi
       public void AddMoved(float horizontalCells);  // < 0 → ArgumentOutOfRangeException; IsDead → bỏ qua
       public int Land(float fallCells);             // < 0 → ArgumentOutOfRangeException; trả số máu đã mất
       public void Tick(int ms, bool headUnderwater); // ms < 0 → ArgumentOutOfRangeException; IsDead → bỏ qua
       public void Respawn();                        // Hp = HpMax, Mana = ManaMax, xoá mọi bộ đếm, Changed
   }
   ```
   Thuật toán (trường riêng: `float movedAcc`, `int drownMs`, `int sinceDamageMs`, `int regenMs`):
   - **Ngấm mana khi đi** (`AddMoved`): `movedAcc += d`. Lặp khi `movedAcc >= 1f`: `movedAcc -= 1f; Mana = Math.Min(ManaMax, Mana + manaRegenPerCell)`. Mana đổi → `Changed` một lần.
   - **Mất máu chung** (`Damage(int n)`, private): `n <= 0` → không gì. Ngược lại `Hp = Math.Max(0, Hp - n)`, `sinceDamageMs = 0`, `regenMs = 0`, `Changed`; nếu Hp vừa về 0 từ > 0 → `Died`.
   - **Rơi** (`Land`): `int over = (int)Math.Floor(fallCells - fallSafeCells + 1e-4f)`; `dmg = over > 0 ? over * fallDamagePerCell : 0`; `Damage(dmg)`; trả `dmg`.
   - **Ngập đầu** (`Tick`, `headUnderwater` true):
     - `drownMs += ms`;
     - lặp khi `drownMs >= 1000`: `drownMs -= 1000; Damage(drownDamagePerSecond)`, dừng lặp nếu chết;
     - sau đó `sinceDamageMs = 0; regenMs = 0`.
   - **Không ngập** (`Tick`, `headUnderwater` false):
     - `drownMs = 0` (lên mặt nước thì bỏ phần lẻ);
     - `int before = sinceDamageMs; sinceDamageMs += ms`;
     - nếu `sinceDamageMs > hpRegenDelayMs`: `regenMs += sinceDamageMs - Math.Max(before, hpRegenDelayMs)`; lặp khi `regenMs >= 1000`: `regenMs -= 1000`; nếu `Hp < HpMax` thì `Hp = Math.Min(HpMax, Hp + hpRegenPerSecond)` và `Changed`.
   - **Xong khi:** biên dịch sạch.

5. **`SlotInventory.cs`** (namespace `Game.Core`):
   ```csharp
   /// Một ô: vật phẩm + số lượng; Count 0 = ô trống.
   public readonly struct ItemStack
   {
       public readonly ItemId Item;
       public readonly int Count;
       public ItemStack(ItemId item, int count) { Item = item; Count = count; }
       public bool IsEmpty => Count <= 0;
   }

   /// Kho chứa đếm theo loại vật phẩm (túi, túi phụ: SlotInventory; kho chung: StockStore).
   public interface IItemStore
   {
       int Count(ItemId item);
       int CanAdd(ItemId item, int amount);   // số thêm được, 0..amount
       int Add(ItemId item, int amount);      // thêm min(amount, CanAdd); trả số đã thêm
       int Remove(ItemId item, int amount);   // lấy min(amount, Count); trả số đã lấy
   }

   /// Túi ô vuông (0034 mục 6): Capacity ô, mỗi ô một loại, tối đa StackMax; trang PageSize ô, trang vòng.
   public sealed class SlotInventory : IItemStore
   {
       public SlotInventory(int capacity, int pageSize, int stackMax); // mỗi số < 1 → ArgumentOutOfRangeException
       public int Capacity { get; }
       public int PageSize { get; }
       public int StackMax { get; }
       public int PageCount => (Capacity + PageSize - 1) / PageSize;
       public int PageStart(int page);           // page * PageSize; page ngoài [0, PageCount) → ArgumentOutOfRangeException
       public int PageEnd(int page);             // Math.Min(Capacity, (page + 1) * PageSize) (không gồm)
       public int NextPage(int page);            // (page + 1) % PageCount; kiểm page như trên
       public int PrevPage(int page);            // (page + PageCount - 1) % PageCount
       public ItemStack Get(int slot);           // slot ngoài [0, Capacity) → ArgumentOutOfRangeException
       public event Action<int> SlotChanged;     // chỉ số ô vừa đổi (mỗi ô đổi một lần mỗi lệnh)
       public int Count(ItemId item);
       public int CanAdd(ItemId item, int amount);
       public int Add(ItemId item, int amount);
       public int Remove(ItemId item, int amount);
       public int AddToSlot(int slot, ItemId item, int amount);  // ô trống hoặc cùng loại: thêm min(amount, StackMax − Count); khác loại → 0
       public int RemoveFromSlot(int slot, int amount);          // lấy min(amount, Count); hết → ô trống
       public bool Move(int fromSlot, SlotInventory dest, int destSlot); // kéo thả
   }
   ```
   - Mọi `amount < 0` → `ArgumentOutOfRangeException`; `amount == 0` → trả 0, không phát sự kiện.
   - `CanAdd(item, amount)` = `min(amount, Σ(StackMax − Count) trên các ô cùng loại + StackMax × số ô trống)`.
   - `Add`: lấp các ô cùng loại theo thứ tự ô 0 → Capacity−1, rồi ô trống theo thứ tự đó.
   - `Remove`: lấy từ ô cùng loại có chỉ số lớn nhất trước.
   - `Move`:
     - ô nguồn trống, hoặc `dest == this && destSlot == fromSlot` → `false`;
     - ô đích trống → chuyển cả chồng, `true`;
     - cùng loại → `n = min(Count nguồn, dest.StackMax − Count đích)`; `n == 0` → `false`; chuyển `n`, phần dư ở lại, `true`;
     - khác loại → đổi chỗ nếu `Count nguồn <= dest.StackMax && Count đích <= this.StackMax`, `true`; không → `false`.
     - Kiểm chỉ số ô cả hai bên; `dest == null` → `ArgumentNullException`.
   - **Xong khi:** biên dịch sạch.

6. **`Sim/StockStore.cs`** (namespace `Game.Core.Sim`):
   ```csharp
   /// Kho chung (Stockpile) nhìn như IItemStore: chỉ hạt (SeedBag) + nông sản (Produce), giới hạn Stock.Free (0028, 0034).
   public sealed class StockStore : IItemStore
   {
       public StockStore(Simulation sim);   // null → ArgumentNullException
       // Count: SeedBag → Stock.Seeds[crop], Produce → Stock.Produce[crop]; loại khác hoặc crop >= Seeds.Length → 0
       // CanAdd: SeedBag/Produce với crop hợp lệ → Math.Min(amount, Math.Max(0, Stock.Free)); khác → 0
       // Add: n = CanAdd → cộng vào mảng; trả n.  Remove: n = Math.Min(amount, Count) → trừ; trả n
   }
   ```
   Đọc `sim.State.Stock` mỗi lần gọi, không giữ bản sao. `amount < 0` → `ArgumentOutOfRangeException`.
   - **Xong khi:** biên dịch sạch.

7. **`ItemTransfer.cs`** (namespace `Game.Core`):
   ```csharp
   public enum ItemContainer : byte { Bag = 0, Hotbar = 1, Stock = 2 }

   /// Chuyển đồ túi / túi phụ / kho (0034 mục 6).
   public static class ItemTransfer
   {
       // Thứ tự ưu tiên Bag > Hotbar > Stock, bỏ source; Stock chỉ có khi stockOpen.
       public static ItemContainer DefaultTarget(ItemContainer source, bool stockOpen);
       // Đích kế tiếp sau current trong danh sách trên (vòng); current không có trong danh sách → phần tử đầu.
       public static ItemContainer NextTarget(ItemContainer source, ItemContainer current, bool stockOpen);
       // part 1 → count; 2 → ceil(count/2); 3 → ceil(count/3). part khác hoặc count < 0 → ArgumentOutOfRangeException.
       public static int PartAmount(int count, int part);
       // Chuyển min(amount, số ở ô, to.CanAdd) từ ô slot sang to; trả số đã chuyển. ReferenceEquals(from, to) → ArgumentException.
       public static int FromSlot(SlotInventory from, int slot, IItemStore to, int amount);
       // Chuyển min(amount, from.Count(item), to.CanAdd) theo loại; trả số đã chuyển. ReferenceEquals(from, to) → ArgumentException.
       public static int FromStore(IItemStore from, ItemId item, IItemStore to, int amount);
   }
   ```
   - Source = Stock và `stockOpen` false vẫn hợp lệ: danh sách là Bag, Hotbar.
   - Danh sách luôn có ≥ 1 phần tử vì Bag/Hotbar luôn có.
   - `PartAmount` dùng `(count + part - 1) / part`.
   - Null tham số → `ArgumentNullException`; `amount < 0` → `ArgumentOutOfRangeException`.
   - **Xong khi:** biên dịch sạch.

8. **`ControlState.cs`** (namespace `Game.Core`):
   ```csharp
   public enum ControlMode : byte { Default = 0, Dig = 1, Till = 2, Rain = 3 }

   /// Chế độ / vật cầm trên PC (0034 mục 3–4): loại trừ nhau.
   public sealed class ControlState
   {
       public ControlMode Mode { get; }      // bắt đầu Default
       public bool HasHeld { get; }
       public ItemId Held { get; }           // chỉ có nghĩa khi HasHeld
       public event Action Changed;          // chỉ khi Mode / HasHeld / Held thật sự đổi
       public void ToggleMode(ControlMode m); // m == Default hoặc m == Mode → Default; khác → Mode = m. Luôn bỏ vật cầm.
       public void Hold(ItemId item);        // Mode = Default, HasHeld = true, Held = item
       public void ClearToDefault();         // Mode = Default, bỏ vật cầm
   }

   public enum RightClickAction : byte { None = 0, OutOfReach, UseHeld, Dig, Till, Rain, Harvest, OpenStorage, PickDrop, PlaceBlock }

   /// Dữ kiện về chỗ chuột phải; client tính (luật của từng vật / chế độ nằm ở nơi khác).
   public struct RightClickTarget
   {
       public bool InTargetReach;  // ô đích trong tầm thao tác (PlayerRules.InTargetReach)
       public bool HeldUsable;     // vật đang cầm dùng được ở chỗ này
       public bool ModeUsable;     // hành động của chế độ đang bật có tác dụng ở chỗ này
       public bool RipeCrop;       // ô ruộng có cây chín
       public bool Storage;        // khối kho
       public bool Drop;           // có vật rơi
   }

   public static class RightClick
   {
       public static RightClickAction Resolve(ControlState state, in RightClickTarget t); // state null → ArgumentNullException
   }
   ```
   `Resolve` theo đúng thứ tự ở "Quyết định đã chốt":
   1. `!t.InTargetReach` → `OutOfReach`.
   2. `state.HasHeld && state.Held.Kind != ItemKind.Block && t.HeldUsable` → `UseHeld`.
   3. `state.Mode != Default && t.ModeUsable` → `Dig` / `Till` / `Rain` theo `Mode`.
   4. `t.RipeCrop` → `Harvest`; `t.Storage` → `OpenStorage`; `t.Drop` → `PickDrop`.
   5. `state.HasHeld && state.Held.Kind == ItemKind.Block && t.HeldUsable` → `PlaceBlock`.
   6. → `None`.
   - **Xong khi:** biên dịch sạch.

9. **Test EditMode** (namespace `Game.Core.Tests`, NUnit như `PlayerRulesTests`): viết các test ở bảng "Phần cần test", tên test đúng như ghi.

10. **Kết quả:** ghi mục Kết quả + Kết quả test của brief. Commit code + test + JSON + brief bằng message `brief 0054: core rules for PC controls (reach, vitals, slot bag, transfer, right-click)`. **Không** add `TerrainEditPlayTests.cs`.

## Tiêu chí xong
- Biên dịch sạch, `read_console` không lỗi / không cảnh báo mới từ file trong Phạm vi.
- Mọi test ở "Phần cần test" có và qua. EditMode toàn bộ qua (504 cũ + mới, 0 fail). PlayMode toàn bộ qua (180/180).
- `git diff --stat` của commit chỉ chứa file trong Phạm vi + brief.

## Test sau khi xong

### Phần cần test
| Phần (bước) | Test · cách | Kết quả mong đợi |
|---|---|---|
| 1 cấu hình đọc từ JSON nguồn | `SimConfigTests` — thêm assert vào test đọc `design/data/sim_m1.json` hiện có (dòng 18–39) cho mọi trường mới | `targetReach` 10, `actReach` 2, `digReach` 2, `collectReach` 2, `manaMax` 100, `manaDig` 5, `manaPlace` 3, `manaRain` 15, `manaRegenPerCell` 2, `hpMax` 100, `fallSafeCells` 4, `fallDamagePerCell` 10, `drownDamagePerSecond` 5, `hpRegenPerSecond` 1, `hpRegenDelayMs` 5000, `bagSlots` 10, `hotbarSlots` 2, `bagPageSize` 10, `hotbarPageSize` 10, `stackMax` 999; `toolReach`/`handReach` vẫn 8, `configVersion` vẫn 5 |
| 1 kiểm biên | `SimConfigTests.ValidateRejectsBadPlayerRules` — thêm các dòng `Bad(...)` vào hàm này (hàm cục bộ `Bad` ở dòng 51) | `targetReach` 0,5 và 33; `actReach` 11 (> targetReach 10); `manaDig` 101; `bagSlots` 0; `stackMax` 0; `hpRegenDelayMs` −1 → đều ném |
| 1 hai bản JSON giống nhau + mặc định C# = JSON | `ConfigTests` cũ (Game.World.Tests); test `SimConfigTests` dòng 44–45 (so `JsonUtility.ToJson(new SimConfig())` với file) | qua — mặc định trong `PlayerConfig` phải đúng bằng số trong JSON |
| 2 tầm mới | `PlayerRulesTests.NewReaches_Boundary`: chân (0.5, −0.5, 0.5) | `InTargetReach` ô (10,0,0) true, (11,0,0) false; `InActReach` / `InDigReach` / `InCollectReach` ô (2,0,0) true, (3,0,0) false, (1,0,1) true, (2,0,1) false |
| 2 Configure tầm mới | `PlayerRulesTests.Configure_ChangesNewReaches`: targetReach 6, actReach 3 | (6,0,0) in / (7,0,0) out; (3,0,0) in / (4,0,0) out; TearDown trả mặc định |
| 2 tầm cũ không đổi | test cũ `InToolReach_BoundaryExactly8`, `Configure_ChangesReach` | qua |
| 3 ItemId mới | `PlayerBagTests.ItemIdProduceAndTool` | `Produce(1) != SeedBag(1)`; `Produce(1).Crop == 1`; `Produce(256)` ném; `Tool(ObservationCamera).ToolKind == ObservationCamera`; `ToString` = `"Produce:1"`, `"Tool:ObservationCamera"`, `"SeedBag:0"`, `"Block:Dirt"` |
| 3 PlayerBag.Add theo loại | `PlayerBagTests.AddItemByKind` | Add(Produce(1), 3) → `GetProduce(1) == 3`; Add(SeedBag(0), 2) → `GetSeeds(0) == 2`; Add(Tool(...), 1) → `NotSupportedException` |
| 4 mana | `PlayerVitalsTests.SpendMana` | mới tạo Hp 100 / Mana 100; TrySpend(Dig) → 95; TrySpend(Rain) ×6 → 5 rồi lần 7 false, Mana vẫn 5; Cost(Place) == 3 |
| 4 hồi mana theo ô | `PlayerVitalsTests.ManaRegenPerCell` | TrySpend(Rain) → Mana 85; AddMoved(0.6) → 85; AddMoved(0.6) → 87; AddMoved(10) → 100 (chặn ManaMax); AddMoved(−1) ném |
| 4 rơi | `PlayerVitalsTests.FallDamage` | Land(4) → 0; Land(4.9f) → 0; Land(5) → 10; Land(6.5f) → 20 (Hp 70 sau ba lần); Land(−0.1f) ném |
| 4 ngập đầu | `PlayerVitalsTests.DrownDamage` | Tick(999, true) → Hp 100; Tick(1, true) → 95; Tick(500, true), Tick(100, false), Tick(500, true) → vẫn 95 (lên mặt nước bỏ phần lẻ) |
| 4 hồi máu | `PlayerVitalsTests.HpRegenAfterDelay` | Land(5) → 90; Tick(5000, false) → 90; Tick(1000, false) → 91; Tick(3000, false) → 94; Land(6) → 74 rồi Tick(5999, false) → 74, Tick(1, false) → 75 |
| 4 chết + hồi sinh | `PlayerVitalsTests.DiesOnceAndRespawns` | Land(20) → Hp 0, `Died` đếm 1; Land(20) nữa → `Died` vẫn 1; TrySpend false; AddMoved / Tick không đổi gì; Respawn → Hp 100, Mana 100, IsDead false; `Changed` có phát |
| 4 tham số sai | `PlayerVitalsTests.InvalidArgs` | ctor(null) → ArgumentNullException; Tick(−1, false) → ArgumentOutOfRangeException |
| 5 chồng 999 + ô mới | `SlotInventoryTests.AddSplitsAt999` | (10, 10, 999): Add(SeedBag(0), 1000) → 1000; ô 0 = 999, ô 1 = 1; Add(Produce(0), 5) → ô 2; Count(SeedBag(0)) == 1000 |
| 5 đầy | `SlotInventoryTests.FullInventoryAddsPartially` | (2, 10, 999): Add(A, 1500) → 1500; CanAdd(A, 1000) == 498; Add(B, 1) → 0; Add(A, 1000) → 498 |
| 5 lấy ra | `SlotInventoryTests.RemoveTakesFromHighestSlot` | ô 0 = 999, ô 1 = 5 (cùng loại): Remove(A, 3) → ô 1 = 2, ô 0 = 999; Remove(A, 10) → ô 1 trống, ô 0 = 991; RemoveFromSlot(0, 2000) → 991, ô 0 trống |
| 5 trang vòng | `SlotInventoryTests.PagesWrap` | (25, 10, 999): PageCount 3, PageStart(2) 20, PageEnd(2) 25, NextPage(2) 0, PrevPage(0) 2; (10, 10, 999): NextPage(0) 0; (2, 10, 999): PageCount 1; NextPage(3) ném |
| 5 kéo thả | `SlotInventoryTests.MoveEmptyMergeSwap` | sang ô trống → cả chồng; cùng loại 900 + 200 (max 999) → đích 999, nguồn 101; khác loại → đổi chỗ; cùng ô / nguồn trống → false; giữa hai túi khác nhau (túi → túi phụ) chạy đúng |
| 5 sự kiện + tham số | `SlotInventoryTests.SlotChangedAndArgs` | Add 1000 → SlotChanged với ô 0 và 1, mỗi ô 1 lần; Add(x, 0) không phát; ctor(0, 10, 999) ném; Get(−1) ném; amount −1 ném |
| 6 kho | `StockStoreTests.SeedsProduceOnlyAndFreeLimit`: dựng như `PlayerFarmTests.Layout(false)` | Add(SeedBag(0), 3) → Stock.Seeds[0] +3; Count đúng; CanAdd(Block(Dirt), 5) == 0; Add(SeedBag(0), Free + 10) → Free; Free == 0; Remove(Produce(1), 5) khi kho có 0 → 0 |
| 7 đích ưu tiên + Tab | `ItemTransferTests.DefaultAndNextTarget` | Default(Bag, true) Hotbar; Default(Hotbar, true) Bag; Default(Stock, true) Bag; Default(Bag, false) Hotbar; Next(Bag, Hotbar, true) Stock; Next(Bag, Stock, true) Hotbar; Next(Bag, Hotbar, false) Hotbar; Next(Stock, Bag, true) Hotbar; Next(Stock, Hotbar, true) Bag; Next(Bag, Bag, true) Hotbar |
| 7 chia phần | `ItemTransferTests.PartAmount` | (5,1)=5; (5,2)=3; (5,3)=2; (1,2)=1; (1,3)=1; (999,3)=333; (0,2)=0; (5,4) ném; (−1,1) ném |
| 7 chuyển | `ItemTransferTests.FromSlotAndFromStore` | túi ô 0 = 10 hạt → FromSlot(bag, 0, hotbar, PartAmount(10, 2)) = 5 (túi 5, túi phụ 5); túi phụ đầy (2 ô × 999 khác loại) → 0, túi không đổi; túi → StockStore khối Dirt → 0; FromStore(stock, SeedBag(0), bag, 4) = 4; FromSlot(bag, 0, bag, 1) → ArgumentException |
| 8 chế độ / vật cầm | `ControlStateTests.ToggleAndHold` | đầu Default; Toggle(Dig) → Dig; Toggle(Dig) → Default; Toggle(Till) rồi Hold(SeedBag(0)) → Default + HasHeld; Toggle(Rain) → Rain, HasHeld false; ClearToDefault → Default, không cầm; Changed không phát khi Toggle(Default) lúc đang Default không cầm |
| 8 ưu tiên chuột phải | `ControlStateTests.RightClickPriority` | ngoài tầm → OutOfReach (kể cả mọi cờ khác true); cầm hạt + HeldUsable + RipeCrop → UseHeld; cầm hạt, HeldUsable false, RipeCrop → Harvest; cầm khối + HeldUsable + RipeCrop → Harvest; cầm khối + HeldUsable, không gì khác → PlaceBlock; Dig + ModeUsable + RipeCrop → Dig; Rain, ModeUsable false, RipeCrop → Harvest; Default + Storage + Drop → OpenStorage; Default + Drop → PickDrop; Till + ModeUsable → Till; không gì → None |
| Regression lõi | toàn bộ EditMode | 504 cũ + mới qua (gồm `DropFieldTests`, `PlayerBagTests`, `PlayerFarmTests`, `HardcodedTextScanTests`, `ConfigTests`) |
| Regression client (đổi `ItemId`, `PlayerBag.Add`, JSON) | toàn bộ PlayMode | 180/180 |

### Lệnh executor chạy
Unity Editor mở project `client/` qua MCP (`run_tests`, quyết định 0010). Nếu Editor tắt thì dùng `python3 tools/unity_test.py EditMode` / `PlayMode` thay `run_tests`.
1. Ghi giờ bắt đầu: `date "+%H:%M"`. Ghi base commit: `git log --oneline -1`.
2. Sau bước 1–3: `refresh_unity` → `read_console` → không lỗi biên dịch. `run_tests` EditMode lọc `SimConfigTests`, `PlayerRulesTests`, `PlayerBagTests`, `DropFieldTests`, `ConfigTests` → qua.
3. Sau bước 4–8: `read_console` → không lỗi.
4. Sau bước 9: `run_tests` EditMode lọc 5 lớp test mới (`PlayerVitalsTests`, `SlotInventoryTests`, `StockStoreTests`, `ItemTransferTests`, `ControlStateTests`) → qua.
5. `run_tests` EditMode (toàn bộ) → 504 cũ + mới, 0 fail.
6. `run_tests` PlayMode (toàn bộ) → 180/180.
7. Dọn: `git checkout -- client/ProjectSettings/EditorSettings.asset`, rồi `git status --short`. Chỉ còn:
   - file trong Phạm vi + `.meta` mới + brief;
   - `client/Assets/Game/Tests/Client/TerrainEditPlayTests.cs` (có từ trước, để nguyên).
8. Ghi giờ kết thúc: `date "+%H:%M"`. Commit (bước 10).

### User tự test
Không. Client chưa dùng luật mới; chơi không đổi.

## Dừng và chuyển lại Opus khi
- Brief mơ hồ hoặc mâu thuẫn với code thật (vd chữ ký ở "Bối cảnh" khác code, `sim_m1.json` dòng `player` không phải khoá cuối).
- Cần một quyết định thuộc AGENTS.md §3.4 hoặc ngoài "Quyết định đã chốt".
- Cùng một lỗi sửa 2 lần vẫn chưa qua.
- Phải sửa file ngoài "Phạm vi" (vd test cũ hỏng vì `ItemKind` mới mà nằm ngoài Phạm vi).

---
<!-- Từ đây trở xuống: EXECUTOR ghi. -->

## Kết quả
Base commit: 06c27b9
Chạy bằng: Sonnet · medium (claude-sonnet-5-5)
Bắt đầu: 17:12 · Kết thúc: 17:21 (~9 phút thực tế)
Commit: 9c993e6 — 10 file mới (+.meta) trong Core/Tests, sửa DropField, PlayerBag, PlayerRules, SimConfig, hai sim_m1.json, PlayerBagTests, PlayerRulesTests, SimConfigTests.

## Kết quả test
- Biên dịch: `read_console` không lỗi (chỉ cảnh báo cũ CS0618 ở file Client ngoài phạm vi).
- Các phần ở bảng 1–8: đạt — mọi test mới có tên đúng bảng và qua (EditMode).
- EditMode toàn bộ: 527 tổng, lần đầu 526 qua + 1 lỗi do test của chính mình (`ItemTransferTests.FromSlotAndFromStore`: kho chung có sẵn hạt đầu game nên phải so với mốc đầu); sửa test, chạy lại lớp đó 3/3 qua.
- PlayMode toàn bộ: 181/181 qua (brief ghi 180; chênh 1 do thay đổi dở của TerrainEditPlayTests.cs).
- `python3 tools/check_design.py`: đạt. Hai sim_m1.json giống nhau từng byte.
- Dọn: `git checkout -- client/ProjectSettings/EditorSettings.asset` đã chạy. Editor đang ở Play Mode khi bắt đầu → đã Stop để chạy test.

## Vướng mắc

---
<!-- PLANNER ghi khi review. -->

## Review
Mức: đầy đủ · Planner Opus · medium (phiên con), 2026-10-23.
- `Chạy bằng` Sonnet · medium khớp `Model thực thi` [repo brief:419].
- Phạm vi: `git show --stat 9c993e6` — 29 file, đều trong "Được sửa / Được tạo" (+ `.meta`); không đụng `Client/` (trừ bản sao JSON), `World/`, `Simulation.cs`, `SimData.cs`; `TerrainEditPlayTests.cs` không commit [run].
- Game.Core không tham chiếu UnityEngine/UnityEditor: grep 5 file mới rỗng [run]. Chuỗi exception tiếng Anh.
- Quyết định đã chốt, đối chiếu diff: 20 trường `PlayerConfig` + kiểm biên đúng tên/khoảng; `toolReach`/`handReach` giữ; `configVersion` không đổi; hai `sim_m1.json` giống từng byte (`cmp`) [run]; `PlayerRules` 4 tầm mới dùng `Within`; `ItemKind.Produce/Tool`, `ToString` switch, `PlayerBag.Add` switch + `NotSupportedException`; `PlayerVitals` đúng thuật toán (rơi `floor(f−4+1e-4)×10`, ngập 5/1000 ms, hồi sau 5000 ms, `Died` một lần, Respawn); `SlotInventory` lấp chồng trước → ô trống, lấy từ ô lớn nhất, trang vòng; `StockStore` chỉ hạt + nông sản, giới hạn `Free`; `ItemTransfer` ưu tiên Bag > Hotbar > Stock, Stock chỉ khi mở, `ceil`; `RightClick.Resolve` đúng 6 bậc.
- Lệch nhỏ (chấp nhận): `Move` sang ô trống trả `false` khi chồng nguồn > `dest.StackMax` (`SlotInventory.cs` khối `Move`) — brief không nói ca này; chặn tràn, cùng tinh thần nhánh đổi chỗ. Hiện mọi túi dùng chung `stackMax` nên không xảy ra.
- Phần cần test: 23 hàm test đúng tên bảng (bước 2–8) + assert thêm vào `SimConfigTests` (bước 1) [run `git show`]; Kết quả test ghi "đạt" gộp cho bảng 1–8, EditMode 527 (1 test tự sửa vì kho có hạt đầu game — sửa test, không sửa luật), PlayMode 181/181 (chênh +1 do `TerrainEditPlayTests.cs` dở). Không chạy lại: kết quả đủ, không đáng ngờ.
- Giờ thực tế: 9 phút (17:12–17:21) so với ước lượng 2–3 ngày-người — đã ghi `design/05-roadmap.md` (bảng giờ M1) + `design/04-tech/world-tech.md` dòng 7l a; `check_design.py` đạt [run].
- Kết luận: đã duyệt
