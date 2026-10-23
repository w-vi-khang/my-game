# Brief 0063 — 7l d3: chuyển đồ túi / túi phụ / kho (kéo thả cả chồng; chọn ô → Tab đổi đích → 1 / 2 / 3)

> Trạng thái: chờ review
> Review: đầy đủ + user xem
> Model thực thi: Sonnet · medium
> Cập nhật: 2026-10-24

<!-- Planner (Opus) viết, Executor (Sonnet) làm theo ở PHIÊN KHÁC — executor không có lịch sử chat, không được tự quyết. -->

## Mục tiêu
Bước **7l d3** (brief 3/3, cuối của 7l d) của task [m1-world-7l-dieu-khien-pc](../tasks/m1-world-7l-dieu-khien-pc.md). Luật: [0034 mục 6 "Chuyển đồ"](../../design/decisions/0034-dieu-khien-pc-click-di-chuyen.md#6-túi-túi-phụ-kho). Brief 0062 đã có `InventoryView` (túi / túi phụ / kho ô vuông, chọn ô, bảng thông tin, chuột phải → cầm, 1–0). Brief này thêm **chuyển đồ** (đồ chuyển hẳn sang nơi khác):
1. **Kéo thả** một ô sang nơi khác: chuyển cả chồng (ô → ô: gộp / đổi chỗ; vào kho / từ kho).
2. **Bằng phím:** click trái chọn ô → đích tự chọn theo ưu tiên túi > túi phụ > kho (bỏ nơi đang chứa, chỉ nơi đang mở) → **Tab** đổi đích → **1** toàn bộ, **2** một nửa, **3** một phần ba (làm tròn lên, tối thiểu 1).

Luật lõi đã có, đã test (brief 0054): `ItemTransfer.DefaultTarget`, `NextTarget`, `PartAmount`, `FromSlot`, `FromStore`; `SlotInventory.Move`. Brief này thêm 1 hàm lõi nhỏ (`ToSlot`) + nối vào giao diện.

Bắt đầu: kiểm brief 0062 có `> Trạng thái: đã duyệt`. Không có → dừng, `cần Opus`.

**File lạ / của phiên khác — KHÔNG commit, KHÔNG sửa:** `client/Assets/InitTestScene*.unity(.meta)`; thay đổi chưa commit trong `design/`, `.claude/skills/local-terrain-gen/`, `tools/terrain_catalog*`, `.ai/skill-map.local.json`. Chỉ `git add` đúng file trong "Phạm vi".

Ước lượng: 1 ngày-người (phần cuối 3–4 ngày của dòng 7l d, M1).

## Bối cảnh cần đọc
Đường dẫn tính từ `client/Assets/Game/` trừ khi ghi khác. Đọc file thật sau 0062 (số dòng có thể lệch — tìm theo tên).

**Core:**
- `Core/ItemTransfer.cs` (70 dòng): `enum ItemContainer { Bag, Hotbar, Stock }` `:6`; `Targets(source, stockOpen)` riêng `:12–19` (thứ tự Bag > Hotbar > Stock, bỏ source, Stock chỉ khi `stockOpen`); `DefaultTarget(ItemContainer source, bool stockOpen)` `:21`; `NextTarget(ItemContainer source, ItemContainer current, bool stockOpen)` `:25`; `PartAmount(int count, int part)` `:33` (part 1 → count; 2 → ceil(count/2); 3 → ceil(count/3)); `FromSlot(SlotInventory from, int slot, IItemStore to, int amount)` `:41`; `FromStore(IItemStore from, ItemId item, IItemStore to, int amount)` `:57`.
- `Core/SlotInventory.cs`: `Move(int fromSlot, SlotInventory dest, int destSlot)` (ô đích trống → cả chồng; cùng loại → gộp, dư ở lại; khác loại → đổi chỗ; trả bool), `AddToSlot(int slot, ItemId item, int amount)` (khác loại → 0), `Get`, `PageStart`.
- `Core/Sim/StockStore.cs`: `CanAdd` = 0 với khối / camera (kho chỉ nhận hạt + nông sản, 0028).
- `Core/PlayerBag.cs`: `Main`, `Hotbar`.
- `Tests/Core/ItemTransferTests.cs` (mẫu test lõi; dựng `StockStore` thế nào — xem test dùng `stock`).

**Client (sau 0062):**
- `Client/InventoryView.cs` — đọc hết: `Update` (đọc phím, `LeftClickAt` khi `MoveClick.WasPressedThisFrame`, `RightClickAt`, `consumedFrame`), `Select`, `SelectedContainer` / `SelectedSlot` / `SelectedStockItem`, `SlotAt`, `SlotScreenCenter`, `Covers`, `BlocksPointer`, danh sách ô kho (túi hạt cây 0..n−1 rồi nông sản), `Refresh`, bảng thông tin (`InfoText`), `ItemName`, `IconColor`, chữ `inv.bag_hint`.
- `Client/TerrainEditInput.cs`: `SetResult(string)` (0062), `HudText()`.
- `Client/GameInput.cs` + `Client/Input/GameControls.inputactions` (map `Panels`: còn `Close` Esc sau 0062).
- `Tests/Client/InventoryPlayTests.cs` (0062): `SetUp`, `GoNearStorage`, `GoFarFromStorage`, cách lấy `inv`.
- `Tests/ClientEdit/GameControlsTests.cs`: `Expected()`, hai chỗ đếm `39`.

**API Unity (6000.4 / uGUI 2.0.0 / Input System 1.19). Chỉ dùng:**
- Như brief 0062 (Canvas, CanvasScaler, Image, Text, `RectTransformUtility.ScreenPointToLocalPointInRectangle` / `RectangleContainsScreenPoint`) — [docs RectTransformUtility](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/RectTransformUtility.html), [docs Image](https://docs.unity3d.com/Packages/com.unity.ugui@2.0/api/UnityEngine.UI.Image.html).
- `InputAction.WasPressedThisFrame`, `InputAction.WasReleasedThisFrame`, `InputAction.IsPressed` ([docs](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.19/api/UnityEngine.InputSystem.InputAction.html)).
- `RectTransform.position` (Overlay canvas: toạ độ = pixel màn hình) để đặt hình đang kéo theo con trỏ ([docs](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/Transform-position.html)).
- **Không dùng** EventSystem / `IBeginDragHandler` / `IDragHandler` / `IDropHandler` (lý do như 0062 Quyết định 2: cần EventSystem + input module đọc phím ngoài `GameControls` — [docs SupportedEvents](https://docs.unity3d.com/Packages/com.unity.ugui@2.0/manual/SupportedEvents.html), [docs UISupport](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.19/manual/UISupport.html)). Kéo thả tự làm: nhấn → di quá ngưỡng → thả.

## Quyết định đã chốt (không được đổi)
1. **Kéo thả tự làm** (đề xuất Planner): nhấn chuột trái trên ô có đồ → ghi ô nguồn; con trỏ đi xa điểm nhấn > **8 px** màn hình (`[số tạm]`, hằng `DragThresholdPx` trong `InventoryView`, chỉnh được trong code — là cảm giác UI, không phải số luật game) trong lúc giữ → bắt đầu kéo: hình ô nguồn (Image 40 × 40, alpha 0,8, đặt sau mọi phần khác để nằm trên cùng) bám con trỏ. Thả:
   - **trúng ô** của túi / túi phụ / kho, hoặc **trúng nền** một nơi (khung lưới của nơi đó) → chuyển (Quyết định 2);
   - ngoài mọi nơi → huỷ, không đổi gì.
   Nhả mà chưa quá ngưỡng → là **click** (chọn / bỏ chọn như 0062; nút trang như 0062). Nghĩa là **chọn ô chuyển từ lúc nhấn sang lúc nhả**. Chuột phải trong lúc kéo: bỏ qua.
2. **Chuyển cả chồng khi thả** (0034 "chuyển cả chồng"):
   - ô túi / túi phụ → ô túi / túi phụ (cùng hay khác nơi): `SlotInventory.Move(from, destInv, destSlot)` (gộp / đổi chỗ; cùng ô → không làm gì);
   - ô túi / túi phụ → kho (ô kho bất kỳ hoặc nền kho): `ItemTransfer.FromSlot(src, slot, stock, count)`;
   - ô kho → ô túi / túi phụ: `ItemTransfer.ToSlot(stock, item, destInv, destSlot, count)` (mới, bước 1); ô đích chứa loại khác (ToSlot trả 0) → `ItemTransfer.FromStore(stock, item, destInv, count)` (vào nơi đó theo thứ tự `SlotInventory.Add`);
   - ô túi / túi phụ → nền túi / túi phụ (không trúng ô): `ItemTransfer.FromSlot(src, slot, destInv, count)` (cùng nơi → không làm gì);
   - ô kho → nền túi / túi phụ: `ItemTransfer.FromStore(stock, item, destInv, count)`;
   - kho → kho: không làm gì.
   - Chỉ thả vào nơi **đang hiện** (túi phụ hiện khi `HotbarShown`; kho khi `StockOpen`).
3. **Chuyển bằng phím** (0034): chỉ khi **`BagOpen && HasSelection`** (kho mở thì túi cũng mở — 0062). Đích:
   - `CurrentTarget` đặt = `ItemTransfer.DefaultTarget(SelectedContainer, StockOpen)` mỗi khi chọn ô mới, mở / đóng kho;
   - **Tab** (`Panels/NextTarget`) → `CurrentTarget = ItemTransfer.NextTarget(SelectedContainer, CurrentTarget, StockOpen)`;
   - **1 / 2 / 3** (`Panels/TransferAll` / `TransferHalf` / `TransferThird`, phím `<Keyboard>/1` `/2` `/3`) → `amount = ItemTransfer.PartAmount(số ở ô chọn, part)`; nguồn ô túi / túi phụ → `FromSlot(src, slot, đíchStore, amount)`; nguồn kho → `FromStore(stock, item, đíchStore, amount)` (đíchStore: Bag → `Main`, Hotbar → `Hotbar`, Stock → `StockStore`).
   - Sau khi chuyển: ô chọn còn đồ → giữ chọn; hết → bỏ chọn.
   - Phím 1–3 khi túi mở chỉ để chuyển (0062 đã chặn phím túi phụ khi túi mở).
4. **Đích đang chọn hiện ra:** khung nền (padding 6, sau các ô) của nơi `CurrentTarget` đổi màu `(0.25, 0.5, 0.9, 0.45)`; nơi khác khung trong suốt (alpha 0). Bảng thông tin thêm dòng 5: `inv.target` với tên đích (`inv.target_bag` / `inv.target_hotbar` / `inv.target_stock`) — chỉ khi `BagOpen && HasSelection`.
5. **Dòng kết quả** (qua `TerrainEditInput.SetResult`): chuyển được n → `inv.moved` (n, tên vật, tên đích); 0 vì đích là kho mà vật là khối / camera → `inv.stock_rejects` (tên vật); 0 vì lý do khác (đích đầy) → `inv.move_none` (tên đích). Kéo thả đổi chỗ / gộp trong túi ↔ túi phụ: không ghi dòng (thấy ngay trên ô).
6. **Chặn con trỏ:** đang kéo → `BlocksPointer` trả true ở mọi điểm (nhân vật không đi theo chuột trái đang giữ). Frame nhấn và frame thả mà InventoryView xử lý → `consumedFrame` như 0062.
7. **Không làm:** chia chồng bằng kéo chuột phải, Shift + click, kéo ra ngoài để vứt đồ; nhiều kho riêng (các kho thông nhau — 0025); HUD 7l e; đổi phím 7l f.

## Phạm vi
- Được sửa:
  - Core: `Core/ItemTransfer.cs` (chỉ thêm `ToSlot`).
  - Client: `Client/InventoryView.cs`, `Client/GameInput.cs`, `Client/Input/GameControls.inputactions`.
  - Chuỗi: `Client/Resources/Lang/vi.json`, `loc/en.json`, `loc/context.json`.
  - Test: `Tests/Core/ItemTransferTests.cs`, `Tests/Client/InventoryPlayTests.cs`, `Tests/ClientEdit/GameControlsTests.cs`; test cũ khác chỉ theo quy tắc bước 7.
  - File brief này (mục Kết quả / Kết quả test / Vướng mắc).
- Không được sửa: `Core/` ngoài `ItemTransfer.cs`, `Client/` ngoài 3 file trên, `design/`, task contract, mọi file khác.

## Các bước
1. **Core `ItemTransfer.ToSlot`** (sau `FromStore`):
   ```csharp
   /// <summary>Kéo thả từ kho vào một ô (0034 mục 6): ô trống hoặc cùng loại → chuyển min(amount, from.Count(item), chỗ trống của ô); ô chứa loại khác → 0. Trả số đã chuyển.</summary>
   public static int ToSlot(IItemStore from, ItemId item, SlotInventory to, int slot, int amount)
   {
       if (from == null) throw new ArgumentNullException(nameof(from));
       if (to == null) throw new ArgumentNullException(nameof(to));
       if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount), amount, "Amount must be >= 0");
       if (ReferenceEquals(from, to)) throw new ArgumentException("Source and target must differ.");
       int n = Math.Min(amount, from.Count(item));
       if (n <= 0) return 0;
       n = to.AddToSlot(slot, item, n);
       if (n > 0) from.Remove(item, n);
       return n;
   }
   ```
   Xong khi: biên dịch không lỗi.
2. **Input:** map `Panels` thêm 4 action Button (GUID mới `python3 -c "import uuid; print(uuid.uuid4())"`, trường khác chép như `Panels/Close`): `NextTarget` `<Keyboard>/tab`, `TransferAll` `<Keyboard>/1`, `TransferHalf` `<Keyboard>/2`, `TransferThird` `<Keyboard>/3`. `GameInput.cs` thêm `NextTarget`, `TransferAll`, `TransferHalf`, `TransferThird` (`Get("Panels/…")`). `GameControlsTests`: thêm 4 dòng vào `Expected()`; `39` → `43` (2 chỗ).
   - Xong khi: `NoDuplicateBindingsWithinMap` không báo trùng (1/2/3 ở `Player` và `Panels` là 2 map khác nhau — đúng ý: 0062 chỉ dùng phím túi phụ khi túi đóng, brief này chỉ dùng phím chuyển khi túi mở).
3. **`InventoryView` — con trỏ:** thay đoạn `MoveClick.WasPressedThisFrame → LeftClickAt` của 0062 bằng 3 hàm công khai (test gọi thẳng) và gọi chúng từ `Update`:
   ```csharp
   public const float DragThresholdPx = 8f; // [số tạm] cảm giác kéo
   public bool Dragging { get; private set; }
   /// <summary>Nhấn chuột trái: trúng phần nào của InventoryView → ghi điểm nhấn (+ ô nguồn nếu ô có đồ), true.</summary>
   public bool PointerDown(Vector2 screen);
   /// <summary>Giữ chuột: quá DragThresholdPx từ điểm nhấn và có ô nguồn → Dragging; hình kéo bám con trỏ.</summary>
   public void PointerMove(Vector2 screen);
   /// <summary>Nhả: Dragging → DropAt(screen) (Quyết định 2); không → LeftClickAt(điểm nhấn) như 0062. Trả true nếu đã xử lý.</summary>
   public bool PointerUp(Vector2 screen);
   ```
   - `Update`: `if (MoveClick.WasPressedThisFrame() && PointerDown(pos)) consumedFrame = Time.frameCount;` → `if (pressActive && MoveClick.IsPressed()) PointerMove(pos);` → `if (pressActive && MoveClick.WasReleasedThisFrame() && PointerUp(pos)) consumedFrame = Time.frameCount;` (`pressActive` = đã có `PointerDown` trúng, xoá khi `PointerUp`). Chuột phải: chỉ xử lý khi `!Dragging`.
   - `Covers(screen)`: thêm `Dragging ||` ở đầu (Quyết định 6).
   - Đóng túi / `CloseAll` trong lúc kéo → huỷ kéo.
   - Hàm riêng `int DropAt(Vector2 screen)` theo Quyết định 2: xác định nơi + ô dưới con trỏ (ô → (nơi, chỉ số trong trang); nền → (nơi, −1)); chuyển chỉ số trong trang → chỉ số ô thật (`PageStart(trang) + i`; kho → `ItemId` theo danh sách); gọi hàm lõi; ghi dòng kết quả (Quyết định 5); trả số đã chuyển (`Move` thành công → số của chồng nguồn trước khi chuyển).
   - Thêm khung nền mỗi nơi (Quyết định 4) — `RectTransform` công khai cho test: `public bool CoversPanelFrame(ItemContainer c, Vector2 screen)` (true nếu điểm trong khung nơi đó, nơi đó đang hiện).
4. **`InventoryView` — phím chuyển:**
   ```csharp
   public ItemContainer CurrentTarget { get; private set; }
   public bool TransferActive => BagOpen && HasSelection;
   /// <summary>Tab: đích kế tiếp (Quyết định 3). Không TransferActive → không làm gì.</summary>
   public void NextTarget();
   /// <summary>1 / 2 / 3: chuyển PartAmount(số ở ô chọn, part) sang CurrentTarget; trả số đã chuyển (không TransferActive → 0).</summary>
   public int TransferPart(int part);
   ```
   - `CurrentTarget` đặt lại khi `Select` chọn ô mới, `OpenStock`, kho tự đóng / đóng.
   - `Update` (sau phím B/P/Esc của 0062): `if (TransferActive) { if (GameInput.NextTarget.WasPressedThisFrame()) NextTarget(); if (GameInput.TransferAll.WasPressedThisFrame()) TransferPart(1); else if (TransferHalf…) TransferPart(2); else if (TransferThird…) TransferPart(3); }`.
   - `InfoText`: thêm dòng 5 khi `TransferActive` (Quyết định 4). Khung nền đích đổi màu trong `Refresh`.
   - Summary lớp: nối "Brief 0063: kéo thả cả chồng (tự làm, ngưỡng 8 px); chọn ô → Tab đổi đích (túi > túi phụ > kho) → 1 / 2 / 3."
5. **Chuỗi** — **sửa** `inv.bag_hint` (cả 3 file) và **thêm** (thứ tự bảng chữ cái):

   | key | vi | en | context |
   |---|---|---|---|
   | `inv.bag_hint` (sửa) | `Kéo thả: chuyển cả chồng · Click: chọn · Tab: đổi đích · 1 / 2 / 3: hết / nửa / một phần ba · Chuột phải: cầm · B / Esc: đóng` | `Drag: move the whole stack · Click: select · Tab: change target · 1 / 2 / 3: all / half / a third · Right-click: hold · B / Esc: close` | `Footer hint in the bag panel. Dragging a slot moves the whole stack to another place (bag, quick bar, storage). Clicking selects a slot; Tab picks where the selected stack goes; 1 / 2 / 3 move all, half or a third of it. Right-click makes the character hold the item. Keep the key names B, Esc, Tab, 1, 2, 3.` |
   | `inv.move_none` | `Không chuyển được: {0} đầy` | `Nothing moved: {0} is full` | `Result line when moving items fails because the target has no room. {0} = target name (inv.target_bag / inv.target_hotbar / inv.target_stock).` |
   | `inv.moved` | `Đã chuyển {0} {1} → {2}` | `Moved {0} {1} → {2}` | `Result line after moving items. {0} = quantity, {1} = item name, {2} = target name (bag, quick bar or storage). Keep the arrow.` |
   | `inv.stock_rejects` | `Kho không nhận {0} (chỉ hạt + nông sản)` | `Storage doesn't take {0} (seeds and produce only)` | `Result line when the player tries to put a block or the camera into the farm storage, which only accepts seeds and harvested produce. {0} = item name.` |
   | `inv.target` | `Chuyển tới: {0} (Tab đổi)` | `Move to: {0} (Tab to change)` | `Line in the item details panel showing where keys 1 / 2 / 3 will move the selected stack. {0} = target name. Keep 'Tab'.` |
   | `inv.target_bag` | `túi` | `bag` | `Target name inserted into inv.target / inv.moved / inv.move_none: the inventory bag. Lowercase.` |
   | `inv.target_hotbar` | `túi phụ` | `quick bar` | `Target name: the small always-visible quick bar at the bottom of the screen ('túi phụ' = secondary bag). Lowercase.` |
   | `inv.target_stock` | `kho` | `storage` | `Target name: the farm's shared storage building. Lowercase. Not 'warehouse stock' in the trading sense.` |

   Xong khi: parse JSON 3 file; `loc_check.py check loc/en.json` exit 0.
6. **Test** (chuỗi tiếng Việt trong assert ghi `// vi-text`; ô túi 0 = camera quan sát):
   - `Tests/Core/ItemTransferTests.cs` — **`ToSlotFromStock`**: dựng `stock` như test cũ trong file; kho có hạt 0 × 6; `bag = new SlotInventory(3, 10, 5)`; `ToSlot(stock, seed, bag, 1, 999)` trả 5 (ô 1 = 5, kho còn 1); `bag.AddToSlot(2, dirt, 1)`; `ToSlot(stock, seed, bag, 2, 1)` trả 0 (ô khác loại); `ToSlot(stock, seed, bag, 1, 1)` trả 0 (ô đầy); `ToSlot` cùng object → `ArgumentException`.
   - `Tests/Client/InventoryPlayTests.cs` (helper mới `IEnumerator Drag(Vector2 from, Vector2 to)`: `inv.PointerDown(from)`; 1 frame; `inv.PointerMove(Vector2.Lerp(from, to, 0.5f))`; `inv.PointerMove(to)`; 1 frame; `inv.PointerUp(to)`; 1 frame):
     - **`DragBagToHotbarMovesStack`**: `view.Bag.Add(Dirt, 5)` (ô túi 1); `ToggleBag()`; 1 frame; `Drag(SlotScreenCenter(Bag, 1), SlotScreenCenter(Hotbar, 1))` → `Hotbar.Get(1)` = Dirt × 5, `Main.Get(1)` trống; `!inv.Dragging`.
     - **`DragSwapsAndMerges`**: túi ô 1 đất 3; `Hotbar.AddToSlot(0, Stone, 2)`; kéo túi 1 → túi phụ 0 → túi phụ 0 = đất 3, túi 1 = đá 2 (đổi chỗ); `Main.AddToSlot(2, Dirt, 4)`; kéo túi 2 → túi phụ 0 → túi phụ 0 = đất 7, túi 2 trống (gộp).
     - **`DragBetweenBagAndStock`**: `GoNearStorage()`; `sv.OpenStockAt(0)`; `s0 = Stock.Seeds[0]`; `view.Bag.AddSeeds(0, 3)` (túi 1); kéo túi 1 → `SlotScreenCenter(Stock, 0)` → `Stock.Seeds[0] == s0 + 3`, túi 1 trống, `HudText` chứa `Đã chuyển 3` (// vi-text); kéo kho ô 0 (túi hạt 0) → túi ô 2 → túi 2 = túi hạt 0 × min(s0 + 3, 999), kho giảm đúng số đó; `view.Bag.Add(Dirt, 2)`; kéo ô đất → kho ô 0 → không đổi, `HudText` chứa `Kho không nhận` (// vi-text). (Kho đầy: `Stock.Free` < số kéo → chuyển `Free`, kiểm số khớp `ItemTransfer.FromSlot`.)
     - **`ClickIsNotDrag`**: túi 1 đất 1; `PointerDown(p)`; `PointerMove(p + (3, 0))`; `PointerUp(p + (3, 0))` → `InfoVisible`, `!Dragging`, đồ không đổi chỗ.
     - **`DropOutsideCancels`**: kéo túi 1 → giữa màn hình (`(Screen.width / 2, Screen.height / 2)`; đảm bảo không trùng phần nào: `!inv.Covers` trước khi nhấn) → không đổi; trong lúc kéo `InventoryView.BlocksPointer(giữa màn hình)` true.
     - **`KeysDefaultTargetTabAndParts`**: `GoNearStorage()`; `sv.OpenStockAt(0)`; túi 1 = túi hạt 0 × 9 (`view.Bag.AddSeeds(0, 9)`; nếu kho đã có hạt 0 thì vẫn đúng); `Select(Bag, 1)` → `CurrentTarget == Hotbar`; `TransferPart(2)` trả 5 → túi phụ có 5, túi 1 còn 4; `NextTarget()` → `Stock`; `TransferPart(3)` trả 2 (ceil(4/3)); `TransferPart(1)` trả 2 → túi 1 trống, `!HasSelection`; `InfoText` khi còn chọn chứa `Chuyển tới: kho` (kiểm trước lần chuyển cuối) (// vi-text).
     - **`TabWithoutStock`**: kho đóng; túi 1 đất 2; `ToggleBag()`; `Select(Bag, 1)` → `CurrentTarget == Hotbar`; `NextTarget()` → vẫn `Hotbar`; `Select(Hotbar, …)` sau khi chuyển 1 → `CurrentTarget == Bag`.
     - **`TransferNeedsBagOpen`**: túi đóng; `Hotbar.AddToSlot(0, Dirt, 2)`; `Select(Hotbar, 0)` → `!TransferActive`; `TransferPart(1)` trả 0, đồ không đổi.
     - **`TargetFullMessage`**: `ToggleBag()`; túi phụ 2 ô đầy loại khác (`Hotbar.AddToSlot(0, Stone, 999)`, `(1, Sand, 999)`); túi 1 đất 3; `Select(Bag, 1)`; `TransferPart(1)` trả 0; `HudText` chứa `Không chuyển được: túi phụ đầy` (// vi-text).
   - Xong khi: test trên có trong file, biên dịch không lỗi.
7. **Test cũ hỏng** — quy tắc: test 0062 hỏng vì click giờ chọn lúc **nhả** (gọi `LeftClickAt` vẫn chọn ngay — giữ `LeftClickAt` công khai cho test cũ, không đổi chữ ký) → sửa test gọi `PointerDown` + `PointerUp`, ghi tên. Số action 39 → 43: sửa. Lý do khác → dừng, `cần Opus`.

## Tiêu chí xong
- EditMode + PlayMode qua hết, 0 lỗi console (qua MCP).
- `loc_check.py check loc/en.json` exit 0; `python3 tools/check_design.py` exit 0; `grep -rn "UnityEngine" client/Assets/Game/Core` rỗng.
- `git diff --stat <base>..HEAD -- client/Assets/Game/Core` chỉ có `ItemTransfer.cs`.
- Mỗi test ở bước 6 có và qua.

## Test sau khi xong

### Phần cần test
- `ToSlot` (bước 1) · `ItemTransferTests.ToSlotFromStock` + test cũ của file · đúng giới hạn ô, khác loại → 0.
- Phím mới (bước 2) · `GameControlsTests` (43 action, không trùng trong map, `GameInputCoversAllActions`) · qua.
- Kéo thả túi ↔ túi phụ (bước 3) · `DragBagToHotbarMovesStack`, `DragSwapsAndMerges` · chuyển / đổi chỗ / gộp cả chồng.
- Kéo thả túi ↔ kho (bước 3) · `DragBetweenBagAndStock` · vào / ra kho, kho không nhận khối, dòng kết quả.
- Click ≠ kéo, thả ngoài, chặn con trỏ khi kéo (bước 3) · `ClickIsNotDrag`, `DropOutsideCancels` · đúng.
- Phím Tab + 1/2/3 (bước 4) · `KeysDefaultTargetTabAndParts`, `TabWithoutStock`, `TransferNeedsBagOpen`, `TargetFullMessage` · đích mặc định đúng ưu tiên, Tab vòng, chia làm tròn lên, chỉ khi túi mở, báo đầy.
- Chuỗi (bước 5) · `HardcodedTextScanTests`, `loc_check.py` · qua.
- Regression 0062 + điều khiển · toàn bộ `InventoryPlayTests` cũ, `ControlModePlayTests`, `PlayerBagSowPlayTests`, `ClickMovePlayTests`, `CameraPcPlayTests`, `SettingsPlayTests`, `SimFarmUiPlayTests`, `GameControlsPlayTests` · qua (phím 1–3 túi phụ khi túi đóng vẫn chạy: `HotbarKeysHold`).
- Toàn bộ EditMode + PlayMode · qua.

### Lệnh executor chạy
1. Parse JSON (inputactions, vi.json, en.json, context.json) + `python3 "<Plugin root>/skills/translation/scripts/loc_check.py" check loc/en.json` + `grep -rn "UnityEngine" client/Assets/Game/Core` + `python3 tools/check_design.py`.
2. Unity Editor mở qua MCP ([0010](../../design/decisions/0010-mcp-for-unity.md)): `refresh_unity` → `read_console` (0 lỗi biên dịch) → `run_tests` EditMode (toàn bộ) → `run_tests` PlayMode (toàn bộ) → `read_console` lỗi. Ghi dòng tổng kết số test thật. Editor tắt → `sh "<Plugin root>/scripts/run.sh" verify_run client_test client_test_play`.
3. **Dọn:** sau `run_tests` PlayMode → `git checkout -- client/ProjectSettings/EditorSettings.asset`. Không commit `client/Assets/InitTestScene*`.
4. `sh "<Plugin root>/scripts/run.sh" verify_run --all` trước commit (client_test / client_test_play exit 1 vì Editor mở → ghi `Chưa kiểm được` + kết quả MCP).
5. Commit code + test + chuỗi (chỉ file trong Phạm vi), message `brief 0063: 7l d3 item transfer (drag whole stack, Tab target, 1/2/3 split)`; commit kết quả brief riêng `brief 0063: result`. Không push.
6. Ghi giờ bắt đầu / kết thúc **mọi vòng** vào Kết quả (giờ thực tế M1).

### User tự test
Gom vào danh sách chơi thử cuối của task (user 2026-10-24). Mục cần xem (Planner chép vào danh sách cuối) — scene SimView, Mac:
1. Mở túi (B), kéo một ô sang túi phụ → cả chồng sang; kéo lên ô có đồ khác → đổi chỗ; cùng loại → gộp.
2. Mở kho (chuột phải khối kho), kéo túi hạt / nông sản từ túi sang kho và ngược lại; kéo khối vào kho → `Kho không nhận …`.
3. Click chọn ô → khung nơi đích sáng xanh + dòng `Chuyển tới: …`; Tab đổi đích; 1 / 2 / 3 chuyển hết / nửa / một phần ba.
4. Kéo ra ngoài → không đổi gì; trong lúc kéo nhân vật không đi theo chuột.
5. Cảm giác ngưỡng kéo 8 px, màu khung đích có ổn không (số tạm).

## Dừng và chuyển lại Opus khi
- Brief mơ hồ hoặc mâu thuẫn với code thật (vd `InventoryView` 0062 khác tên hàm ở "Bối cảnh").
- Cần API Unity ngoài danh sách ở "Bối cảnh".
- Cần một quyết định thuộc AGENTS.md §3.4 hoặc ngoài "Quyết định đã chốt".
- Cùng một lỗi sửa 2 lần vẫn chưa qua.
- Phải sửa file ngoài "Phạm vi".
- Test cũ hỏng mà không thuộc quy tắc bước 7.

---
<!-- Từ đây trở xuống: EXECUTOR ghi. -->

## Kết quả
- Base commit: 748f81e
- Chạy bằng: claude-sonnet-5-5 · medium
- Bắt đầu: 2026-10-24 04:25
- Kết thúc: 2026-10-24 04:43 (một vòng; ≈ 0,5 giờ thực tế, phần lớn đợi EditMode ~10 phút + PlayMode).
- Commit: f2c75a7 (`brief 0063: 7l d3 item transfer ...`).
- File đã đổi: Core/ItemTransfer.cs (`ToSlot`); Client/InventoryView.cs, GameInput.cs, Input/GameControls.inputactions; Lang/vi.json, loc/en.json, loc/context.json (sửa `inv.bag_hint`, thêm 8 khoá); Tests: ItemTransferTests, InventoryPlayTests, GameControlsTests (43 action).
- Lệch so với brief: không về hành vi. Chi tiết tự chọn: khung nền đích (`Frame`) bao quanh lưới ô với padding 6, thả "nền" dùng cả panel của nơi đó (khung hẹp hơn) cho dễ trúng; `CoversPanelFrame` dùng khung hẹp theo đúng tên. Kéo thả túi ↔ túi phụ (cả "nền") không ghi dòng khi thành công; ghi `inv.move_none` khi đích đầy. Khi nguồn kéo hết đồ / đổi loại giữa lúc nhấn và nhả → bỏ qua (không chuyển). Lỗi tự sửa 1 lần trong lúc làm: `PointerUp` xoá nguồn kéo trước `DropAt` — đã sửa, test qua.
- Test cũ: không cần sửa theo bước 7 (test 0062 gọi `LeftClickAt` trực tiếp, vẫn chạy).

## Kết quả test
- Lệnh 1: parse JSON (inputactions, vi.json, en.json, context.json) OK; `loc_check.py check loc/en.json` exit 0; `grep UnityEngine Core` rỗng; `check_design.py` exit 0 (trong `verify_run --all`); `git diff --stat 748f81e..HEAD -- Core` chỉ `ItemTransfer.cs`.
- Lệnh 2 (MCP): biên dịch 0 lỗi console. EditMode lần 1: 542/543 — `IncrementalRebuildMatchesFullRebuild` quá timeout 180 s (như brief 0061, Editor không focus); chạy lại riêng → qua (38 s) ⇒ **543/543 qua**. PlayMode **242/242 qua** (gồm 17 test `InventoryPlayTests`). Console 0 lỗi sau chạy.
- Lệnh 3: đã `git checkout -- client/ProjectSettings/EditorSettings.asset`; không commit `InitTestScene*`.
- Lệnh 4 `verify_run --all`: design exit 0, asset_check exit 0; client_test / client_test_play exit 1 vì Editor mở → **Chưa kiểm được** bằng dòng lệnh; kết quả MCP thay thế.
- `ToSlot` → **đạt** (`ItemTransferTests.ToSlotFromStock`). Phím mới → **đạt** (`GameControlsTests` 43 action). Kéo thả túi ↔ túi phụ → **đạt** (`DragBagToHotbarMovesStack`, `DragSwapsAndMerges`). Túi ↔ kho → **đạt** (`DragBetweenBagAndStock`). Click ≠ kéo, thả ngoài, chặn con trỏ khi kéo → **đạt** (`ClickIsNotDrag`, `DropOutsideCancels`). Tab + 1/2/3 → **đạt** (`KeysDefaultTargetTabAndParts`, `TabWithoutStock`, `TransferNeedsBagOpen`, `TargetFullMessage`). Chuỗi → **đạt** (`HardcodedTextScanTests`, `loc_check`). Regression 0062 + điều khiển (gồm `HotbarKeysHold`) + toàn bộ EditMode / PlayMode → **đạt**.
- Kéo chuột thật qua `Update` (nhấn / giữ / nhả bằng `GameInput`), hình kéo bám con trỏ, màu khung đích, ngưỡng 8 px → **chờ user — gộp cuối** (test gọi `PointerDown/Move/Up` trực tiếp, chưa mô phỏng chuột thật).

## Vướng mắc

---
<!-- PLANNER ghi khi review. -->

## Review
