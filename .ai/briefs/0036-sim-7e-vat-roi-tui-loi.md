# Brief 0036 — Sim 7e (lõi): túi đồ + vật rơi + nhặt trong Game.Core, tầm với công cụ 10 ô / việc tay 4 ô

> Trạng thái: chờ review
> Review: đầy đủ
> Model thực thi: Sonnet · medium
> Cập nhật: 2026-10-22 15:25

<!-- Planner (Opus) viết, Executor (Sonnet) làm theo ở PHIÊN KHÁC — executor không có lịch sử chat, không được tự quyết.
Trạng thái: chờ làm → đang làm → chờ review → đã duyệt; hoặc → cần Opus (executor dừng, hỏi ở mục Vướng mắc).
Review: nhẹ (đọc mục Kết quả) | đầy đủ (đọc cả diff) | user xem (có phần user tự kiểm bằng mắt/chơi thử) — có thể ghép "đầy đủ + user xem".
Model thực thi: BẮT BUỘC, đúng dạng `<Model> · <effort>` — Model: Opus | Sonnet | Haiku | Fable; effort: low | medium | high | xhigh | max (chọn theo AGENTS.md §10.2).
Executor kiểm tra model + effort của phiên TRƯỚC khi làm (scripts/brief_check.py); lệch hoặc không xác định được → cảnh báo user và DỪNG.
File được commit. Tự kiểm trước khi giao (AGENTS.md §10.3): executor có phải hỏi gì không? còn chỗ nào bỏ ngỏ 2 phương án? -->

## Mục tiêu
Phần lõi của [task contract sim 7e](../tasks/m1-sim-buoc-7e-vat-roi-tui.md) ([0028](../../design/decisions/0028-nhan-vat-tui-vat-roi-tu-trong-trot.md), [0030](../../design/decisions/0030-tam-voi-cong-cu-10-o.md), Chốt):

- **Tầm với:** `PlayerRules` tách tầm công cụ 10 ô (đào/đặt/xới) và tầm việc tay 4 ô (nhặt). Client đổi sang tầm công cụ 10.
- **Túi đồ:** `MaterialInventory` đổi thành `PlayerBag`, giữ khối theo vật liệu và hạt theo loại cây.
- **Vật rơi** (`DropField` trong `Game.Core`):
  - đào → chồng vật rơi tại ô, túi không đổi;
  - gộp chồng cùng loại trong một ô;
  - rơi xuống khi mất khối bên dưới;
  - đẩy lên khi đặt khối vào ô có vật rơi;
  - nhặt trong 4 ô (Thu cả vùng, hoặc một chồng).
- Sửa test cũ dựa vào "đào → cộng kho" và thêm test EditMode mới.

**Không có trong brief này** (brief 0037 sau):
- hiển thị vật rơi;
- phím F (Thu), click nhặt;
- túi hạt đầu game;
- dòng HUD túi hạt.

Vì vậy sau brief này, trong game đào ra sẽ **không thấy vật rơi** và chưa nhặt được. Đây là chuyện dự kiến, brief 0037 bù lại.

Ước lượng ~1,5 ngày-người (trong 3–5 của 7e). Ghi giờ bắt đầu/kết thúc (theo `date`) vào mục Kết quả.

## Bối cảnh cần đọc
Đường dẫn tính từ `client/Assets/Game/`. Số dòng theo commit `36fdf96`.

**Task contract:** `.ai/tasks/m1-sim-buoc-7e-vat-roi-tui.md` — đọc mục "Đề xuất của Planner" (user xác nhận 2026-10-22) và "Tiêu chí chấp nhận".

**Tầm với:** `Core/PlayerRules.cs`
- `public const float Reach = 4f;` (`:11`).
- `public static bool InReach(float feetX, float feetY, float feetZ, int x, int y, int z)` (`:20-24`): Euclid từ tâm thân (chân + `BodyCenterHeight` = 1) tới tâm ô, `<= Reach * Reach + 1e-4f`.

**Kho vật liệu hiện tại:** `Core/MaterialInventory.cs` (toàn file, 37 dòng)
- `readonly int[] counts = new int[256];`
- `public event Action<BlockMaterial, int> Changed;`
- `public int Get(BlockMaterial material)`.
- `public void Add(BlockMaterial material, int amount = 1)` — `amount < 0` → `ArgumentOutOfRangeException`.
- `public bool TryTake(BlockMaterial material)`.

**Đào/đặt:** `Core/WorldEditor.cs`
- `enum EditResult` (`:6-24`): có `Occupied`, `NoMaterial`, `OutOfReach`.
- `public readonly MaterialInventory Inventory;` (`:36`).
- Constructor `public WorldEditor(WorldData world, MaterialInventory inventory, int meshMargin, int seaLevel = -1)` (`:60-68`).
- `public EditResult TryDig(int x, int y, int z)` (`:74-89`):
  - ô thành rỗng (`:83`);
  - `Inventory.Add(...)` ô ruộng → đất (`:84`);
  - `CellChanged` (`:85`);
  - nước biển (`:86-87`).
- `public EditResult CheckPlace(...)` (`:92-110`):
  - thứ tự kiểm: OutOfBounds → NotPlaceable → NotEmpty → NotAttached → IsOccupied → IsPlayerAt (`:105-106`) → NoMaterial (`:107-108`).
- `public EditResult TryPlace(...)` (`:113-126`):
  - `Inventory.TryTake` (`:118`);
  - `World.Set` (`:122`), `CellChanged` (`:123`), `RevertFarmlandBelow` (`:124`).
- `bool IsSolid(int x, int y, int z)` (`:160-168`): `y < 0` → đặc; ngoài map → không đặc; ô không rỗng và không nước → đặc.

**Hạt theo loại cây:** chỉ số loại cây = chỉ số trong `SimConfig.crops` (`Core/Sim/SimConfig.cs:160`), vd `store.Seeds[0]` ở `Tests/Core/Sim/PlotRulesTests.cs:121`.

**Client dùng kho/tầm:**
- `Client/TerrainChunkView.cs`:
  - `public MaterialInventory Inventory { get; private set; }` (`:63`);
  - tạo ở `:164-165`.
- `Client/TerrainEditInput.cs`:
  - comment lớp `:16`;
  - `player.InReach(...)` ở `:136` (ShowAim) và `:162` (UseTool);
  - `HelpText()` `:221-229`, chữ "(trong 4 ô quanh nhân vật)" ở `:227`, `:228`;
  - `OnGUI` dùng `view.Inventory` ở `:233`, `:244`, `:245`;
  - chữ `"Kho: "` ở `:241`.
- `Client/PlayerCharacter.cs`: `public bool InReach(Vector3Int cell)` (`:137-141`).
- `Client/TerrainBench.cs:94`: `view.Inventory.Add(m, 100000);`.

**Test dùng kho** (grep `MaterialInventory\|Inventory` trong `Tests/`):
- Core: `WorldEditorTests`, `SeaFloodTests`, `PlayerRulesTests`; `Sim/PlotRulesTests`, `Sim/SimulationTerrainTests`, `Sim/FarmTests`, `Sim/CropQueueTests`, `Sim/HomeTests`.
- World: `IncrementalRebuildTests`, `MinimapEditTests`.
- Client: `TerrainEditPlayTests`, `PlayerCharacterPlayTests`.

**Mẫu test world dựng tay:** `Tests/Core/WorldEditorTests.cs:14-30`
- world 32 × 16 × 32, đá đáy y = 0;
- (1,1,1) Dirt, (1,2,1) Grass, (2,1,1) Water, (3,1,1) khối cốt truyện.

**Tài liệu:** `design/02-gameplay/mechanics.md:69` (dòng "**Vật rơi:** …" trong mục "Nhân vật, túi đồ, tự trồng trọt — luật M1").

Không dùng API Unity mới.

## Quyết định đã chốt (không được đổi)
**Task contract 7e (user xác nhận 2026-10-22):**
1. **Tầm công cụ 10:** đào, đặt, xới, click ô ruộng mở bảng. **Tầm việc tay 4:** nhặt. Cách đo giữ như cũ: Euclid từ tâm thân (chân + 1) tới tâm ô, `≤ tầm` (sai số `1e-4f` như cũ).
2. **Đào:** ô thành rỗng, **túi không đổi**, có 1 vật rơi đúng vật liệu tại ô (ô ruộng → đất). Cùng loại trong một ô gộp thành một chồng có số lượng.
3. **Rơi xuống:** ô dưới một chồng thành rỗng hoặc nước → chồng rơi thẳng xuống tới ô ngay trên khối đặc, gộp với chồng cùng loại ở đó. Rơi ngay trong lần sửa, không có thời gian rơi. Nước không cuốn vật rơi; vật rơi chìm xuống đáy.
4. **Đặt khối vào ô có vật rơi** → các chồng trong ô bị đẩy lên **ô không đặc đầu tiên phía trên** (có thể vượt qua nhiều khối đặc), gộp nếu cùng loại. Không còn ô nào tới đỉnh world → đặt bị từ chối với `EditResult.Occupied`.
5. **Nhặt:**
   - Thu = mọi chồng có tâm ô trong tầm việc tay;
   - nhặt riêng = một chồng theo id, phải trong tầm việc tay;
   - nhặt thì cả chồng vào túi.
6. Túi không có mức tối đa (0004). Túi hạt cùng loại cây cộng hạt vào nhau.
7. `Game.Core` không tham chiếu UnityEngine. Sinh vật không đổi hành vi.

**Planner chốt khi viết brief:**

8. **Tên:**
   - `MaterialInventory` → `PlayerBag`: dùng `git mv` cả `.cs` và `.cs.meta` để giữ GUID;
   - `WorldEditor.Inventory` → `WorldEditor.Bag`;
   - `TerrainChunkView.Inventory` → `TerrainChunkView.Bag`;
   - `PlayerRules.Reach` / `InReach` → `ToolReach` / `InToolReach` và `HandReach` / `InHandReach`;
   - `PlayerCharacter.InReach` → `InToolReach`.

   Không giữ tên cũ (không alias).
9. **`WorldEditor` tự tạo `DropField`** (`public readonly DropField Drops`) trong constructor. Không truyền từ ngoài vào.
10. **Thứ tự trong `TryDig`:**
    - `World.Set` (ô rỗng);
    - `Drops.Add(x, y, z, item, 1)`;
    - `Drops.OnCellCleared(x, y, z)`;
    - `CellChanged`;
    - nước biển.

    **Thứ tự trong `TryPlace`:**
    - `World.Set`;
    - `Drops.PushUp(x, y, z)`;
    - `CellChanged`;
    - `RevertFarmlandBelow`.
11. **`CheckPlace`:** thêm `if (!Drops.CanPushUp(x, y, z)) return EditResult.Occupied;` ngay **sau** kiểm `IsPlayerAt`, **trước** kiểm túi `NoMaterial`.
12. **Thứ tự chồng:**
    - `Stacks` theo thứ tự tạo; gỡ một chồng thì các chồng còn lại giữ thứ tự.
    - Id chồng bắt đầu 1, tăng 1 mỗi chồng mới, không dùng lại.
    - Gộp thì **chồng có sẵn ở ô đích giữ id**, chồng đang di chuyển bị gỡ.
    - Nhiều chồng cùng rơi hoặc cùng bị đẩy: xử lý theo thứ tự trong `Stacks`.
13. **Ô ngoài phạm vi các sửa đổi trên** (xới, ô ruộng trở lại đất, nước lan vào ô rỗng): đều không đổi ô đặc ↔ không đặc nên **không** gọi `DropField`.
14. HUD: chữ `"Kho: "` → `"Túi: "`; "(trong 4 ô quanh nhân vật)" → "(trong 10 ô quanh nhân vật)". Dòng túi hạt để brief 0037.

## Phạm vi
- Được sửa/tạo:
  - `Core/PlayerRules.cs`;
  - `Core/MaterialInventory.cs` (+ `.meta`) → `Core/PlayerBag.cs` (+ `.meta`);
  - `Core/DropField.cs` (mới, + `.meta` do Unity tạo);
  - `Core/WorldEditor.cs`;
  - `Client/TerrainChunkView.cs`, `Client/TerrainEditInput.cs`, `Client/PlayerCharacter.cs`, `Client/TerrainBench.cs` — **chỉ** các chỗ nêu ở bước 5;
  - các test trong `Tests/` có dùng `MaterialInventory` / `Inventory` / `InReach`;
  - `Tests/Core/DropFieldTests.cs` (mới, + `.meta`);
  - `design/02-gameplay/mechanics.md` — chỉ dòng `:69`;
  - mục Kết quả của brief này.
- Không được sửa:
  - `Core/Sim/**` (hành vi sinh vật);
  - scene `.unity`;
  - `design/` ngoài dòng trên;
  - task contract;
  - file khác.

## Các bước
1. **`Core/PlayerRules.cs`**:
   - Thay `Reach` / `InReach` bằng:
     ```csharp
     /// <summary>Tầm công cụ: đào, đặt, xới, click ô ruộng (0030).</summary>
     public const float ToolReach = 10f;
     /// <summary>Tầm việc tay: nhặt (sau này gieo, thu hoạch, phép Mưa — 0028).</summary>
     public const float HandReach = 4f;

     public static bool InToolReach(float feetX, float feetY, float feetZ, int x, int y, int z) =>
         Within(ToolReach, feetX, feetY, feetZ, x, y, z);
     public static bool InHandReach(float feetX, float feetY, float feetZ, int x, int y, int z) =>
         Within(HandReach, feetX, feetY, feetZ, x, y, z);

     /// <summary>Tâm ô (x, y, z) cách tâm thân (chân + BodyCenterHeight) ≤ reach (Euclid).</summary>
     static bool Within(float reach, float feetX, float feetY, float feetZ, int x, int y, int z)
     ```
   - Thân `Within` = thân `InReach` cũ, thay `Reach` bằng `reach`.
   - Sửa summary lớp: "tầm với 4 ô" → "tầm công cụ 10 ô / việc tay 4 ô (0030)".
   - Xong khi: biên dịch được (sau bước 5).

2. **`Core/PlayerBag.cs`** (`git mv Core/MaterialInventory.cs Core/PlayerBag.cs` và `git mv Core/MaterialInventory.cs.meta Core/PlayerBag.cs.meta`):
   - Đổi tên lớp thành `PlayerBag`.
   - Summary lớp: "Túi đồ của người chơi (0028): khối theo vật liệu + hạt theo loại cây (chỉ số trong SimConfig.crops). Khác kho của sinh vật, không có mức tối đa (0004)."
   - Giữ nguyên `Get`, `Add(BlockMaterial, int)`, `TryTake`, event `Changed`.
   - Thêm:
     ```csharp
     readonly int[] seeds = new int[256];
     /// <summary>(loại cây, số hạt mới).</summary>
     public event Action<int, int> SeedsChanged;
     public int GetSeeds(int crop);              // crop ngoài 0..255 → ArgumentOutOfRangeException
     public void AddSeeds(int crop, int amount); // crop ngoài 0..255 hoặc amount < 0 → ArgumentOutOfRangeException; phát SeedsChanged
     /// <summary>Thêm vật phẩm: Block → Add(Material, amount); SeedBag → AddSeeds(Crop, amount).</summary>
     public void Add(ItemId item, int amount);
     ```
   - Xong khi: không còn chữ `MaterialInventory` trong `Core/`.

3. **`Core/DropField.cs`** (mới, namespace `Game.Core`, chỉ `using System; using System.Collections.Generic;`):
   - **Kiểu vật phẩm:**
     ```csharp
     public enum ItemKind : byte { Block = 0, SeedBag = 1 }

     /// <summary>Loại vật phẩm: khối (Value = BlockMaterial) hoặc túi hạt (Value = chỉ số loại cây).</summary>
     public readonly struct ItemId : IEquatable<ItemId>
     {
         public readonly ItemKind Kind;
         public readonly byte Value;
         public ItemId(ItemKind kind, byte value) { Kind = kind; Value = value; }
         public static ItemId Block(BlockMaterial m) => new ItemId(ItemKind.Block, (byte)m);
         public static ItemId SeedBag(int crop) // crop ngoài 0..255 → ArgumentOutOfRangeException
         public BlockMaterial Material => (BlockMaterial)Value;
         public int Crop => Value;
         // Equals(ItemId), Equals(object), GetHashCode, ==, !=
         public override string ToString() => Kind == ItemKind.Block ? "Block:" + Material : "SeedBag:" + Crop;
     }
     ```
   - **Chồng vật rơi:**
     ```csharp
     /// <summary>Một chồng vật rơi: một loại vật phẩm, số lượng ≥ 1, nằm ở ô (X, Y, Z); ô bên dưới luôn đặc.</summary>
     public sealed class DropStack
     {
         public int Id { get; }
         public ItemId Item { get; }
         public int Count { get; internal set; }
         public int X { get; internal set; }
         public int Y { get; internal set; }
         public int Z { get; internal set; }
         internal DropStack(int id, ItemId item, int count, int x, int y, int z)
     }
     ```
   - **Lớp `DropField`:** summary "Vật rơi theo ô (0028, task contract 7e): gộp chồng cùng loại trong ô, rơi khi mất khối bên dưới, đẩy lên khi ô bị đặt khối, nhặt trong tầm việc tay. Tất định: thứ tự theo Stacks."

     Dữ liệu:
     - `readonly WorldData world`;
     - `readonly List<DropStack> stacks`;
     - `readonly Dictionary<(int, int, int), List<DropStack>> byCell` (danh sách trong ô theo **thứ tự vào ô**: chồng mới tạo hoặc chồng chuyển tới đều thêm cuối; ô hết chồng → gỡ khoá);
     - `int nextId = 1`.

     API:
     ```csharp
     public DropField(WorldData world)                 // null → ArgumentNullException
     public IReadOnlyList<DropStack> Stacks { get; }
     /// <summary>Tăng 1 mỗi lần gọi công khai làm đổi chồng (thêm, gộp, rơi, đẩy, nhặt).</summary>
     public int Version { get; private set; }
     /// <summary>Phát cho mỗi chồng được nhặt, sau khi cộng vào túi, trước khi gỡ khỏi Stacks.</summary>
     public event Action<DropStack> Collected;
     public IReadOnlyList<DropStack> At(int x, int y, int z) // danh sách ô theo thứ tự vào ô; không có → Array.Empty<DropStack>()
     public DropStack Find(int id)                           // không có → null
     public DropStack Add(int x, int y, int z, ItemId item, int count)
     public void OnCellCleared(int x, int y, int z)
     public bool CanPushUp(int x, int y, int z)
     public void PushUp(int x, int y, int z)
     public int CollectInReach(float feetX, float feetY, float feetZ, PlayerBag bag)
     public bool TryCollect(int stackId, float feetX, float feetY, float feetZ, PlayerBag bag)
     ```

     Luật (đặc = `WorldEditor.IsSolidAt`, xem bước 4):
     - **`Add`:**
       - kiểm tham số:
         - ngoài map → `ArgumentOutOfRangeException`;
         - `count < 1` → `ArgumentOutOfRangeException`;
         - ô đặc → `ArgumentException`;
       - hạ ô xuống: `while (!IsSolidAt(x, y - 1, z)) y--;` (`y < 0` là đặc nên luôn dừng);
       - ô đích có chồng cùng `Item` → cộng `Count`, trả chồng đó;
       - không có → tạo chồng mới (id kế), thêm cuối `stacks` và cuối danh sách ô;
       - `Version++`.
     - **`OnCellCleared(x, y, z)`** — gọi khi ô vừa thành không đặc:
       - lấy các chồng ở `(x, y + 1, z)`; không có hoặc `y + 1` ngoài map → không làm gì;
       - với mỗi chồng theo thứ tự `Stacks`, tính ô đích bằng cách hạ từ `y + 1` như trong `Add`, rồi chuyển (xem dưới);
       - có chồng đổi thì `Version++` (một lần).
     - **`CanPushUp(x, y, z)`:** `true` khi ô không có chồng, hoặc có ít nhất một ô `y' ∈ [y + 1, Size.Y − 1]` không đặc.
     - **`PushUp(x, y, z)`** — gọi khi ô vừa thành đặc:
       - các chồng ở ô đó chuyển lên ô `y'` nhỏ nhất `> y` không đặc;
       - không có ô như vậy → `InvalidOperationException` (`CheckPlace` đã chặn);
       - có chồng đổi thì `Version++`.
     - **Chuyển chồng:**
       - ô đích có chồng cùng `Item` → cộng `Count` vào chồng đó, gỡ chồng đang chuyển khỏi `stacks` và ô cũ;
       - không có → gỡ khỏi danh sách ô cũ, đổi `Y`, thêm cuối danh sách ô đích; vị trí trong `stacks` giữ nguyên.
     - **`CollectInReach`:**
       - duyệt bản sao `stacks` theo thứ tự;
       - chồng có `PlayerRules.InHandReach(feetX, feetY, feetZ, s.X, s.Y, s.Z)` → `bag.Add(s.Item, s.Count)`, `Collected?.Invoke(s)`, gỡ;
       - trả số chồng đã nhặt; có nhặt thì `Version++`.
       - `bag == null` → `ArgumentNullException`.
     - **`TryCollect`:**
       - không tìm thấy id, hoặc ngoài tầm việc tay → `false`, không đổi gì;
       - được → như trên cho một chồng, `Version++`, trả `true`.
   - Xong khi: biên dịch được; `Game.Core` không `using UnityEngine`.

4. **`Core/WorldEditor.cs`:**
   - Field và constructor:
     - `public readonly MaterialInventory Inventory;` → `public readonly PlayerBag Bag;`;
     - thêm `public readonly DropField Drops;`;
     - constructor `public WorldEditor(WorldData world, PlayerBag bag, int meshMargin, int seaLevel = -1)`;
     - `bag == null` → `ArgumentNullException(nameof(bag))`;
     - `Drops = new DropField(World);` sau khi gán `World`.
   - Thêm `public static bool IsSolidAt(WorldData world, int x, int y, int z)` với đúng thân của `IsSolid` cũ (`:160-168`). `IsSolid` instance gọi `IsSolidAt(World, x, y, z)`.
   - `TryDig`:
     - thay `Inventory.Add(...)` ở `:84` bằng:
       ```csharp
       Drops.Add(x, y, z, ItemId.Block(material == BlockMaterial.Farmland ? BlockMaterial.Dirt : material), 1);
       Drops.OnCellCleared(x, y, z);
       ```
       giữ thứ tự QĐ 10;
     - summary: "Đào: ô thành rỗng, rơi ra 1 vật rơi cùng vật liệu tại ô (ô ruộng → đất, 0020; vật rơi 0028); túi không đổi."
   - `CheckPlace`:
     - thêm kiểm `CanPushUp` theo QĐ 11;
     - `Inventory.Get` → `Bag.Get`.
   - `TryPlace`:
     - `Inventory.TryTake` → `Bag.TryTake`;
     - thêm `Drops.PushUp(x, y, z);` ngay sau `World.Set(...)` (`:122`), trước `CellChanged`;
     - summary: "…tiêu 1 vật liệu từ túi; vật rơi trong ô bị đẩy lên (task contract 7e)".
   - Comment `NoMaterial` (`:16`): "kho hết" → "túi hết". Comment `Occupied` (`:18`): thêm "hoặc có vật rơi không còn chỗ đẩy lên".
   - Xong khi: không còn chữ `Inventory` trong `Core/`.

5. **Client** (chỉ các chỗ sau):
   - `TerrainChunkView.cs`:
     - `:63` → `public PlayerBag Bag { get; private set; }`;
     - `:164-165` → `Bag = new PlayerBag(); Editor = new WorldEditor(World, Bag, BlockMesher.CellMargin, waterLevel);`.
   - `PlayerCharacter.cs:137-141`: đổi tên thành `InToolReach`, gọi `PlayerRules.InToolReach`.
   - `TerrainEditInput.cs`:
     - `:136`, `:162`: `player.InReach` → `player.InToolReach`; comment "ngoài tầm 4 ô" → "ngoài tầm 10 ô" và "(0028)" → "(0030)";
     - `:16`: "tầm 4 ô" → "tầm 10 ô (0030)";
     - `:227`, `:228`: "(trong 4 ô quanh nhân vật)" → "(trong 10 ô quanh nhân vật)";
     - `:233`, `:244`, `:245`: `view.Inventory` → `view.Bag`;
     - `:241`: `"Kho: "` → `"Túi: "`.
   - `TerrainBench.cs:94`: `view.Inventory` → `view.Bag`.
   - Xong khi: grep `Inventory\b\|InReach(` trong `Client/` không còn kết quả, trừ `InToolReach(`.

6. **Đổi test cũ:**
   - **Đổi tên cơ học** trong `Tests/`:
     - `MaterialInventory` → `PlayerBag`;
     - `.Inventory` → `.Bag` (vd `view.Inventory`, `editor.Inventory`);
     - `player.InReach` → `player.InToolReach` (`PlayerCharacterPlayTests.cs:306`).

     Biến cục bộ tên `inv` giữ nguyên.
   - **`Tests/Core/PlayerRulesTests.cs:8-21`** thay 2 test bằng:
     - `InToolReach_BoundaryExactly10`, chân (0.5, −0.5, 0.5):
       - ô (10,0,0) true; (11,0,0) false;
       - (0,10,0) true; (0,0,−10) true.
     - `InToolReach_10Point1Outside`: chân (0.4, −0.5, 0.5), ô (10,0,0) → false.
     - `InHandReach_BoundaryExactly4`: như test cũ `:9-15`, dùng `InHandReach`.
     - `InHandReach_4Point1Outside`: như test cũ `:18-21`, dùng `InHandReach`.
   - **`Tests/Core/WorldEditorTests.cs`:**
     - `InventoryStartsEmpty` (`:45-49`) → `BagStartsEmpty`: thêm `GetSeeds(0..3) == 0` và `editor.Drops.Stacks` rỗng.
     - `DigEmptiesCellAndAddsSameMaterial` (`:52-61`) → `DigEmptiesCellAndDropsSameMaterial`:
       - túi Grass = 0, Dirt = 0;
       - `editor.Drops.At(1, 2, 1)` có đúng 1 chồng `ItemId.Block(Grass)` × 1;
       - giữ assert ô rỗng, cờ sửa, `changed`.
     - `DigRejected` (`:68-76`): thêm `Assert.IsEmpty(editor.Drops.Stacks)`.
     - `DigThenPlaceRoundTrip` (`:90-96`) viết lại:
       - đào (1,1,1);
       - `TryPlace(1,1,1, Dirt)` → `NoMaterial`;
       - `editor.Drops.CollectInReach(1.5f, 0.5f, 1.5f, inv)` → 1;
       - túi Dirt = 1;
       - `TryPlace(1,1,1, Dirt)` → `Ok`;
       - túi Dirt = 0, `Drops.Stacks` rỗng, ô (1,1,1) là Dirt.
     - `DigFarmlandGivesDirt` (`:189-197`):
       - túi Dirt = 0;
       - `Drops.At(5,1,5)` có 1 chồng `Block(Dirt)` × 1;
       - không có chồng `Block(Farmland)` nào.
   - **`Tests/Core/Sim/PlotRulesTests.cs:122`** (`Assert.AreEqual(1, inv.Get(BlockMaterial.Dirt))` sau khi đào ô ruộng (5,1,5) trong `DigCancelsCropInEachPhase`) → `Assert.AreEqual(0, inv.Get(BlockMaterial.Dirt));` và `editor.Drops.At(5, 1, 5)` có đúng 1 chồng `Block(Dirt)` × 1 (`editor` là field của lớp test, `:16`).
   - **`Tests/Client/TerrainEditPlayTests.cs`:**
     - `:84` → `Assert.AreEqual(0, view.Bag.Get(material)); Assert.AreEqual(1, view.Editor.Drops.At(x, y, z).Count);`;
     - ngay trước `view.Place(cell, material)` (`:91`) thêm `view.Bag.Add(material);`;
     - `:93` giữ `0`;
     - `:240-243`: `before + 1` → `before`, và thêm `Assert.AreEqual(1, view.Editor.Drops.At(x, h, z).Count);`.
   - **`Tests/Client/PlayerCharacterPlayTests.cs` `ReachLimitsTools` (`:254-276`):**
     - `far` = `C(A.x + 10, G, A.z)` (cách tâm thân ≈ 10,11 → ngoài tầm);
     - `near` = `C(A.x + 9, G, A.z)` (≈ 9,12 → trong);
     - `pc` = `C(A.x + 11, G + 1, A.z)`.
   - **Test khác** fail vì trước đây dựa vào "đào → cộng kho" (vd đào rồi đặt lại):
     - sửa theo đúng mẫu trên: thêm `Bag.Add` trước khi đặt, hoặc đổi assert túi thành assert vật rơi;
     - ghi tên test đã sửa vào Kết quả.
     - Test fail vì lý do khác → dừng, `cần Opus`.

7. **`Tests/Core/DropFieldTests.cs`** (mới, namespace `Game.Core.Tests`).
   - **SetUp** như `WorldEditorTests.cs:17-29`: world 32 × 16 × 32, đá đáy y = 0, cùng 4 ô đặt sẵn; `bag = new PlayerBag()`; `editor = new WorldEditor(world, bag, 2)`.
   - **Test** (mỗi test tự đặt thêm ô cần dùng bằng `world.Set(..., Cell.Make(...))`):
     1. `DigTwiceSameMaterialMergesIntoOneStack`:
        - Dirt (4,1,4), Dirt (4,2,4);
        - đào (4,2,4) rồi (4,1,4);
        - đúng 1 chồng ở (4,1,4): `Block(Dirt)` × 2, **id 2** (chồng tạo khi đào (4,1,4) có sẵn ở ô đích nên giữ id; chồng id 1 rơi xuống gộp vào và bị gỡ);
        - `Stacks.Count == 1`; túi Dirt = 0.
     2. `DigDifferentMaterialsSameCellTwoStacks`:
        - Stone (6,1,6), Grass (6,2,6);
        - đào grass rồi stone;
        - `At(6,1,6)` = [Stone × 1 (id 2), Grass × 1 (id 1)] theo thứ tự đó (Stone vào ô trước, Grass rơi vào sau).
     3. `StackFallsSeveralCellsWhenSupportDug`:
        - Dirt (8,3,8), Stone (8,4,8); (8,1,8), (8,2,8) rỗng;
        - đào (8,4,8) → chồng Stone ở (8,4,8);
        - đào (8,3,8) → `At(8,1,8)` = [Dirt × 1 (id 2), Stone × 1 (id 1)]; `At(8,4,8)` rỗng.
     4. `StackSinksThroughWater`:
        - Water (10,1,10), (10,2,10); Dirt (10,3,10), Grass (10,4,10);
        - đào grass rồi dirt;
        - `At(10,1,10)` = [Dirt × 1, Grass × 1].
     5. `PlacePushesStackUpPastSolidAndMerges`:
        - đào (1,1,1) (có Grass ở (1,2,1));
        - `editor.Drops.Add(1,3,1, ItemId.Block(Dirt), 2)`;
        - `bag.Add(Stone)`; `TryPlace(1,1,1, Stone)` → `Ok`;
        - `At(1,1,1)` rỗng;
        - `At(1,3,1)` = 1 chồng Dirt × 3; giữ id của chồng tạo bằng `Add` (chồng có sẵn ở ô đích).
     6. `PlaceRejectedWhenNoRoomAbove`:
        - Stone ở (14, y, 14) với y = 2..15;
        - `Drops.Add(14,1,14, Block(Dirt), 1)`; `bag.Add(Stone)`;
        - `CheckPlace` và `TryPlace(14,1,14, Stone)` → `Occupied`;
        - ô (14,1,14) vẫn rỗng, túi Stone = 1, chồng vẫn ở (14,1,14).
        - Thêm ô đỉnh: Stone (20, y, 20) với y = 1..14; `Drops.Add(20,15,20, Block(Dirt), 1)`; `CheckPlace(20,15,20, Stone)` → `Occupied`.
     7. `CollectInReachTakesOnlyWithin4`:
        - `Add` Dirt × 2 ở (4,1,0) và Stone × 1 ở (5,1,0);
        - đăng ký `Collected`;
        - `CollectInReach(0.5f, 0.5f, 0.5f, bag)` → 1;
        - túi Dirt = 2, Stone = 0; `Collected` phát 1 lần với chồng Dirt;
        - còn đúng chồng Stone.
        - Cùng test, sau đó: `Add` lại Dirt × 1 ở (4,1,0); `CollectInReach(0.4f, 0.5f, 0.5f, bag)` → 0 (cách 4,1); túi Dirt vẫn 2.
     8. `TryCollectSingleStack`:
        - 2 chồng trong tầm: Dirt × 1 ở (2,1,0), Stone × 1 ở (3,1,0); chân (0.5f, 0.5f, 0.5f);
        - `TryCollect(id chồng Dirt)` → true, túi Dirt = 1, Stone = 0, chồng Stone còn;
        - id không tồn tại → false;
        - chồng ngoài tầm (vd (9,1,0)) → false, không đổi.
     9. `SeedBagsMergeIntoBagSeeds`:
        - `Add(2,1,0, ItemId.SeedBag(0), 4)` và `Add(2,1,0, ItemId.SeedBag(1), 1)` → 2 chồng trong ô;
        - `Add(2,1,0, SeedBag(0), 2)` → gộp, chồng SeedBag(0) × 6;
        - Thu → `GetSeeds(0) == 6`, `GetSeeds(1) == 1`.
     10. `AddValidatesArguments`:
         - ô đặc (1,1,1) → `ArgumentException`;
         - (−1,1,0) → `ArgumentOutOfRangeException`;
         - `count` 0 → `ArgumentOutOfRangeException`;
         - `ItemId.SeedBag(256)` → `ArgumentOutOfRangeException`.
     11. `AddSettlesDownToSupport`: `Add(12,5,12, Block(Sand), 1)` trên cột rỗng → chồng ở (12,1,12).
     12. `VersionIncrementsOnChanges`:
         - `Version` tăng sau mỗi lần: `Add`, đào làm chồng rơi, `PushUp`, `CollectInReach` có nhặt;
         - không tăng khi `CollectInReach` không nhặt được gì.
     13. `DeterministicSameSequence`:
         - hàm dựng world + chạy một chuỗi cố định: ≥ 6 lần đào, 2 lần đặt (có đẩy), 1 lần `TryCollect`, 1 lần `CollectInReach`;
         - chạy 2 lần trên 2 world mới;
         - so danh sách `(Id, Item.Kind, Item.Value, Count, X, Y, Z)` của `Stacks` và `Version` → bằng nhau.
   - Xong khi: 13 test qua.

8. **`design/02-gameplay/mechanics.md:69`** — thay cả dòng bằng (chép nguyên văn):
   ```
   - **Vật rơi:** đào → bản mini của khối rơi tại chỗ, nằm yên, không tự mất; cùng loại trong một ô gộp thành chồng. Nhặt bằng **hành động Thu** (hút mọi vật rơi trong tầm) hoặc click từng vật. Mất khối bên dưới → vật rơi rơi thẳng xuống mặt đặc, gộp nếu cùng loại; nước không cuốn, vật rơi chìm xuống đáy. Đặt khối vào ô có vật rơi → vật rơi bị đẩy lên ô không đặc đầu tiên phía trên; không còn ô nào → không đặt được (user, 2026-10-22, task contract sim 7e).
   ```
   - Xong khi: `python3 tools/check_design.py` hợp lệ.

## Tiêu chí xong
- `grep -rn "MaterialInventory\|\.Inventory\b\|PlayerRules.Reach\b\|InReach(" client/Assets/Game` không còn kết quả, trừ `InToolReach(` / `InHandReach(`.
- `grep -n "UnityEngine" client/Assets/Game/Core/DropField.cs client/Assets/Game/Core/PlayerBag.cs` → không có.
- EditMode: toàn bộ qua, gồm 13 test `DropFieldTests` và 4 test `PlayerRulesTests` mới/đổi.
- PlayMode: toàn bộ qua.
- `python3 tools/check_design.py` → hợp lệ.
- Sau PlayMode, `git status` không có `client/ProjectSettings/EditorSettings.asset`.

## Test sau khi xong

### Phần cần test
- **Tầm công cụ 10** · `PlayerRulesTests.InToolReach_*` · biên 10 trong, 10,1 ngoài.
- **Tầm việc tay 4** · `PlayerRulesTests.InHandReach_*` · biên 4 trong, 4,1 ngoài.
- **Đào → vật rơi, túi không đổi** · `WorldEditorTests.DigEmptiesCellAndDropsSameMaterial`, `DigFarmlandGivesDirt` · 1 chồng đúng vật liệu, túi 0.
- **Đào bị từ chối không tạo vật rơi** · `WorldEditorTests.DigRejected` · `Drops.Stacks` rỗng.
- **Gộp chồng cùng loại / tách chồng khác loại** · `DropFieldTests` 1, 2, 9 · đúng số chồng, số lượng, id.
- **Rơi xuống, chìm qua nước** · `DropFieldTests` 3, 4, 11 · chồng ở ô ngay trên khối đặc.
- **Đẩy lên khi đặt, vượt khối đặc, gộp** · `DropFieldTests` 5 · chồng × 3 ở (1,3,1), giữ id cũ.
- **Không còn chỗ đẩy → Occupied, cả ô đỉnh** · `DropFieldTests` 6 · `Occupied`, túi và ô không đổi.
- **Thu trong 4 ô** · `DropFieldTests` 7 · nhặt chồng trong tầm, bỏ chồng ngoài tầm; `Collected` phát đúng.
- **Nhặt một chồng** · `DropFieldTests` 8 · đúng chồng, id sai/ngoài tầm → false.
- **Túi hạt cộng hạt** · `DropFieldTests` 9 · `GetSeeds` đúng.
- **Kiểm tham số** · `DropFieldTests` 10 · đúng loại exception.
- **Version** · `DropFieldTests` 12 · tăng khi đổi, đứng yên khi không.
- **Tất định** · `DropFieldTests` 13 · hai lần chạy bằng nhau.
- **Đặt trừ túi, hết túi → NoMaterial** (phần cũ) · `WorldEditorTests.PlaceConsumesOneAndSetsCell`, `PlaceWithoutMaterialRejected`, `DigThenPlaceRoundTrip` · qua.
- **Luật đặt cũ không đổi** (kề khối đặc, ô chiếm, nước, ô ruộng trở lại đất) · toàn bộ `WorldEditorTests`, `SeaFloodTests` · qua.
- **Sim không đổi hành vi** · toàn bộ `Tests/Core/Sim/**` (gồm `PlotRulesTests` đã sửa dòng 122) · qua.
- **Mesh/minimap sau sửa ô** · `IncrementalRebuildTests`, `MinimapEditTests` · qua.
- **Client tầm 10 ô** · `PlayerCharacterPlayTests.ReachLimitsTools` · ô +10 ngoài tầm, ô +9 trong tầm, đặt +11 ngoài tầm.
- **Client đào/đặt qua view** · `TerrainEditPlayTests.DigAndPlaceUpdateMeshAndColliderWithinTwoFrames`, `UseToolDigsOrPlacesByTool` · qua với assert vật rơi mới.
- **Toàn bộ PlayMode còn lại** (nhân vật, minimap, sim UI, streaming) · `run_tests` PlayMode · qua.

### Lệnh executor chạy
1. Unity Editor đang mở project qua MCP: `run_tests` mode `EditMode` (toàn bộ) → ghi dòng tổng kết pass/fail.
2. `run_tests` mode `PlayMode` (toàn bộ) → ghi dòng tổng kết.
3. `git checkout -- client/ProjectSettings/EditorSettings.asset`.
4. `python3 tools/check_design.py` → dòng cuối.
5. Grep ở "Tiêu chí xong" → ghi kết quả (rỗng).
6. Editor tắt (không có MCP): thay 1–2 bằng `python3 tools/unity_test.py EditMode` và `python3 tools/unity_test.py PlayMode`.

### User tự test
Không. Brief 0037 có phần user xem (hiển thị vật rơi, F, click, túi hạt).

## Dừng và chuyển lại Opus khi
- Brief mơ hồ hoặc mâu thuẫn với code thật (vd số dòng lệch làm không chắc sửa chỗ nào).
- Cần một quyết định thuộc AGENTS.md §3.4 hoặc ngoài "Quyết định đã chốt".
- Cùng một lỗi sửa 2 lần vẫn chưa qua.
- Phải sửa file ngoài "Phạm vi" (vd `Core/Sim/**`).
- Test sim hoặc test PlayMode fail vì lý do khác "đào → cộng kho".

---
<!-- Từ đây trở xuống: EXECUTOR ghi. -->

## Kết quả
<!-- Base commit (trước khi làm) · Chạy bằng: <model id> · <effort> (dòng OK của brief_check.py) · commit của brief · file đã đổi · giờ bắt đầu/kết thúc · ghi chú lệch so với brief (nếu có, kèm lý do). -->
Base commit: 81b1158
Chạy bằng: claude-sonnet-5-5 · medium
Bắt đầu: 2026-10-22 15:07 · Kết thúc: 2026-10-22 15:25 (~18 phút thực, gồm ~6 phút chạy EditMode; ước lượng 1,5 ngày-người)
Commit code: e6ddd5d (26 file: Core/PlayerRules, PlayerBag (đổi tên từ MaterialInventory, giữ .meta), DropField (mới), WorldEditor; Client/PlayerCharacter, TerrainBench, TerrainChunkView, TerrainEditInput; 12 file test cũ đổi tên; DropFieldTests mới; mechanics.md:69)
Test cũ sửa ngoài danh sách brief (dựa vào "đào → cộng kho"): `SimulationTerrainTests.DigCutsPathThenPlaceRestores` (thêm `inv.Add(Dirt)` trước TryPlace, đổi `out _` → `out var inv`), `MinimapEditTests.DigAndPlaceChangeExactlyOnePixel` (thêm `editor.Bag.Add(material)` trước TryPlace). Không lệch brief khác.

## Kết quả test
Tất cả phần ở "Phần cần test" → **đạt** (EditMode 422/422 gồm 13 DropFieldTests, 4 PlayerRulesTests, WorldEditorTests, SeaFloodTests, Sim/**, IncrementalRebuildTests, MinimapEditTests; PlayMode 112/112 gồm ReachLimitsTools, TerrainEditPlayTests). Không phần nào chờ user.
- `run_tests EditMode` → Passed 422/422, 0 fail (323 s). (Lần chạy đầu có 2 fail do test cũ nêu trên, đã sửa rồi chạy lại.)
- `run_tests PlayMode` → Passed 112/112, 0 fail (39 s).
- `git checkout -- client/ProjectSettings/EditorSettings.asset` → đã chạy, git status sạch file đó.
- `python3 tools/check_design.py` → ✓ design/ hợp lệ: 55 file, 35 chỗ trống, 170 ý tưởng chưa chốt.
- grep MaterialInventory / .Inventory / PlayerRules.Reach / InReach( (trừ InToolReach/InHandReach/CollectInReach) → rỗng; grep UnityEngine trong DropField.cs, PlayerBag.cs → rỗng.

## Vướng mắc
<!-- Khi trạng thái = cần Opus: câu hỏi cụ thể + các phương án (kèm đề xuất). -->

---
<!-- PLANNER ghi khi review. -->

## Review
<!-- Kết luận: đã duyệt | cần sửa (thêm bước sửa vào "Các bước", trạng thái về chờ làm). Điểm đã đối chiếu. -->
