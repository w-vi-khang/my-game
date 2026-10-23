# Brief 0062 — 7l d2: giao diện túi / túi phụ / kho ô vuông (B, P, kho bằng chuột phải, hình + số, trang, bảng thông tin, chuột phải ô → cầm, 1–0)

> Trạng thái: đã duyệt
> Review: đầy đủ + user xem
> Model thực thi: Sonnet · medium
> Cập nhật: 2026-10-24

<!-- Planner (Opus) viết, Executor (Sonnet) làm theo ở PHIÊN KHÁC — executor không có lịch sử chat, không được tự quyết. -->

## Mục tiêu
Bước **7l d2** (brief 2/3 của 7l d) của task [m1-world-7l-dieu-khien-pc](../tasks/m1-world-7l-dieu-khien-pc.md). Luật: [0034 mục 4, 6](../../design/decisions/0034-dieu-khien-pc-click-di-chuyen.md#6-túi-túi-phụ-kho). Brief 0061 đã làm `PlayerBag` thành túi ô vuông (`Main` 10 ô + `Hotbar` 2 ô). Brief này làm **giao diện uGUI** mới `InventoryView` thay bảng túi + bảng kho IMGUI cũ trong `SimView`:
1. **Túi** (B) lưới ô có trang; **túi phụ** cố định giữa-dưới (P ẩn/hiện; mở túi thì luôn hiện); **kho** lưới ô, mở bằng chuột phải khối kho (đi tới 2 ô — đã có), tự đóng khi xa > 10 ô.
2. Mỗi ô: hình giữ chỗ (ô màu) + số lượng góc dưới-phải.
3. Click trái ô → **bảng thông tin** lề trái (tên, cách kiếm, số trong túi gồm túi phụ, số trong kho).
4. Chuột phải ô → cầm vật đó, túi đóng. Phím **1–0** cầm vật ở ô túi phụ tương ứng (khi túi đóng).
5. Bỏ: bảng túi / kho IMGUI cũ, phím K, phím 1–4 + Alt-lăn chọn khối tạm, Shift = tất cả ở bảng kho cũ; dọn chuỗi không dùng (gồm `bag.empty`).

**Chưa làm** (brief 0063): kéo thả, chọn ô → Tab đổi đích → 1/2/3 chuyển. Trong lúc giữa 0062 và 0063 không chuyển được đồ túi ↔ kho bằng giao diện (chấp nhận — phần user xem gộp cuối task).

Bắt đầu: kiểm brief 0061 có `> Trạng thái: đã duyệt`. Không có → dừng, `cần Opus`.

**File lạ / của phiên khác — KHÔNG commit, KHÔNG sửa:** `client/Assets/InitTestScene*.unity(.meta)`; thay đổi chưa commit trong `design/`, `.claude/skills/local-terrain-gen/`, `tools/terrain_catalog*`, `.ai/skill-map.local.json`. Chỉ `git add` đúng file trong "Phạm vi".

Ước lượng: 1,5 ngày-người (một phần 3–4 ngày của dòng 7l d, M1).

## Bối cảnh cần đọc
Đường dẫn tính từ `client/Assets/Game/` trừ khi ghi khác. Số dòng là trước brief 0061; 0061 chỉ đổi vài dòng ở `TerrainEditInput` (`CanHold`, `CollectNearby`, `CollectDrop`) — tìm theo tên hàm.

**Core (không sửa):**
- `Core/PlayerBag.cs` (sau 0061): `Main`, `Hotbar` (`SlotInventory`), `Count(ItemId)`.
- `Core/SlotInventory.cs`: `Get(slot)` → `ItemStack { Item, Count, IsEmpty }`; `PageCount`, `PageStart(page)`, `PageEnd(page)`, `NextPage(page)`, `PrevPage(page)`, `PageSize`, `Capacity`.
- `Core/ItemTransfer.cs:6` `public enum ItemContainer : byte { Bag = 0, Hotbar = 1, Stock = 2 }` — dùng làm khoá "nơi chứa" trong giao diện.
- `Core/Sim/StockStore.cs` (kho chung nhìn như `IItemStore`, `new StockStore(sim)`); `Core/Sim/SimData.cs:208–223` `Stockpile` (`Capacity`, `Seeds[]`, `Produce[]`, `Free`).
- `Core/View/SiteStatus.cs`: `CropName(Strings t, SimConfig cfg, int crop)`; `StockPanelText(Strings t, Stockpile s, SimConfig cfg)` (dòng đầu = "Kho chung · đã dùng x/y").
- `Core/PlayerRules.cs`: `TargetReach` (10), `InTargetReach(...)`.

**Client (đọc hết các đoạn này):**
- `Client/MinimapView.cs` — **mẫu dựng uGUI lúc chạy, không EventSystem**: Canvas `:409–420` (`RenderMode.ScreenSpaceOverlay`, `CanvasScaler.ScaleMode.ScaleWithScreenSize`, `referenceResolution = (1280, 720)`, `MatchWidthOrHeight`, `matchWidthOrHeight = 1`); hit-test `IsOverButton` `:214–225` (`RectTransformUtility.ScreenPointToLocalPointInRectangle(rt, screen, null, out local)` + `rt.rect.Contains(local)`); helper `Stretch` `:573`, `AddImage` `:595`, `NewText` `:602–612`; font `Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")` `:409`; `GameSettings.Changed += RefreshText` `:488`.
- `Client/SimView.cs` (727 dòng): `Update` `:111–126` (đọc `ToggleStock` / `ToggleBag` / `PanelsClose` `:122–125`); `StockOpen` `:268`; `ToggleStock` `:362`; `BagRow`, `BagOpen`, `ToggleBag`, `bagRows` `:365–368`; `T` / `Format` `:371–372`; `BagRows` `:375–395`; `HoldFromBag` `:398–404`; `BagButtonRect`, `BagPanelRect` `:406–407`; `StorageIdOf` `:410`; `OpenStockAt(int index)` `:413–424`; `CanTransferStock` `:427–436`; `StockMessage` `:439`; `TransferStock` `:442–466`; `StockButtonRect`, `StockPanelRect` `:469–470`; `PlotPanelRect` `:471`; `BlocksPointer` `:476`; `AnyPanelOpen` `:479`; `Covers` `:481–493`; `DrawFarmGui` `:495–…` (phần kho `:499–523`, phần túi `:524–541`, còn lại bảng ô ruộng giữ nguyên).
- `Client/TerrainEditInput.cs` (649 dòng): `Palette` / `PaletteKeys` / `PaletteName` `:19–22`; field `selected` `:29`; `SelectedMaterial` `:51`; `Awake` `:72–82` (mẫu thêm component: `DropView`, `Farming`, `Observation`); `Update` `:84–153` (`SelectBlock1–4` `:100–103`; kiểm `BlocksPointer` `:117`; Alt + lăn `:122–130`); `HoldItem(ItemId item)` `:179–189` (dòng `if (item.Kind == ItemKind.Block) { int i = ...; if (i >= 0) selected = i; }` `:186`); `CanHold` `:191–200`; `SelectMaterial(int i)` `:211–219`; `Execute` → `case RightClickAction.OpenStorage:` gọi `Farm.OpenStockAt(storageId)`; `HudText()` `:525–551` (các dòng túi tạm `hud.bag` … `hud.seed_bag_a` `:537–543`); `ItemLabel(ItemId)` `:484–491`.
- `Client/SettingsView.cs:61–67` `OtherPanelOpen` (dòng `if (sim != null && (sim.BagOpen || sim.StockOpen || sim.OpenPlotId >= 0)) return true;`); `[DefaultExecutionOrder(-50)]` `:8` (chạy trước nên Esc khi túi mở không mở Cài đặt).
- `Client/PlayerCharacter.cs:353` (`bool blocked = ... SimView.BlocksPointer(pos) ...`), `:395` (`if (GameInput.Dash.WasPressedThisFrame() && !SimView.AnyPanelOpen) TryDash();`).
- `Client/FollowCamera.cs:100–102` (`bool alt = GameInput.CycleBlockModifier.IsPressed(); if (!alt && ... ) ApplyZoom(...)`).
- `Client/GameInput.cs` (51 dòng) + `Client/Input/GameControls.inputactions` (map `Player`, `Panels`; mỗi action có `id` GUID, binding có `id`, `path`, `action`).
- `Client/DropView.cs:39–45` `ColorOf(ItemId)` (khối: màu mặt trên; túi hạt: `SeedBagColor`).
- Chuỗi: `Client/Resources/Lang/vi.json`, `loc/en.json`, `loc/context.json`.

**Test mẫu / test sẽ sửa:**
- `Tests/ClientEdit/GameControlsTests.cs`: `Expected()` `:22–59`, `Assert.AreEqual(35, …)` `:66`, `:95`; `NoDirectInputReadsInScope` danh sách file `:102`.
- `Tests/Client/PlayerBagSowPlayTests.cs`: `SetUp` `:22–77`, `GoNearStorage()` / `GoFarFromStorage()` (helper có sẵn), test `BagRowsList` `:191`, `BagPanelBlocksPointer` `:216`, `HoldSeedRules` `:234`, `StockTransferInReach` `:368`, `StockFull` `:399`, `StockOutOfReach` `:416`, `ClickStorageBlock` `:429`.
- `Tests/Client/ControlModePlayTests.cs`: `ModeKeysAndHoldItem` `:139` (đoạn `SelectedMaterial` / `SelectMaterial` `:155–160`), `BagPanelHoldsAndCloses` `:590`.
- `Tests/Client/SimFarmUiPlayTests.cs`: `StockToggleAndPointerBlock` `:238`.
- `Tests/Client/SettingsPlayTests.cs`: `Keys_OpenClose_AndSave` `:230` (dòng `sv.ToggleBag();` `:246`).

**API Unity (6000.4 / uGUI 2.0.0 / Input System 1.19 — đúng bản trong `Packages/manifest.json`). Chỉ dùng các API này:**
- `Canvas` + `RenderMode.ScreenSpaceOverlay`, `Canvas.sortingOrder` ([docs](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/Canvas.html)); `Canvas.ForceUpdateCanvases` ([docs](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/Canvas.ForceUpdateCanvases.html)) — đã dùng ở MinimapView.
- `UnityEngine.UI.CanvasScaler` ([docs](https://docs.unity3d.com/Packages/com.unity.ugui@2.0/api/UnityEngine.UI.CanvasScaler.html)), `UnityEngine.UI.Image` ([docs](https://docs.unity3d.com/Packages/com.unity.ugui@2.0/api/UnityEngine.UI.Image.html)), `UnityEngine.UI.Text` ([docs](https://docs.unity3d.com/Packages/com.unity.ugui@2.0/api/UnityEngine.UI.Text.html)) — đã dùng ở MinimapView.
- `RectTransformUtility.ScreenPointToLocalPointInRectangle` ([docs](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/RectTransformUtility.ScreenPointToLocalPointInRectangle.html)), `RectTransformUtility.RectangleContainsScreenPoint(RectTransform, Vector2)` ([docs](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/RectTransformUtility.RectangleContainsScreenPoint.html)) — canvas Overlay → không truyền camera (như MinimapView truyền `null`).
- `RectTransform.TransformPoint` / `RectTransform.rect` (Transform, [docs](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/RectTransform.html)) để lấy tâm ô ra toạ độ màn hình (Overlay: toạ độ world của RectTransform = pixel màn hình).
- `InputAction.WasPressedThisFrame` ([docs](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.19/api/UnityEngine.InputSystem.InputAction.html)) — đã dùng.
- **Không dùng** `EventSystem` / `IPointerClickHandler` / `IDragHandler` (lý do ở Quyết định 2).

## Quyết định đã chốt (không được đổi)
1. **Thành phần:** lớp mới `Game.Client.InventoryView` (file `Client/InventoryView.cs`), MonoBehaviour trên GameObject của `TerrainChunkView`, được `TerrainEditInput.Awake` thêm nếu chưa có (như `DropView`). Dùng `view.Bag`; phần kho chỉ có khi cùng GameObject có `SimView` đã có `Sim`. Có `public static InventoryView Active` (gán trong `Awake`, xoá trong `OnDestroy` như `SimView.active`).
2. **uGUI dựng bằng code, không EventSystem** (như MinimapView / TimeDial): kéo thả kiểu `IDragHandler` cần EventSystem + input module ([docs SupportedEvents](https://docs.unity3d.com/Packages/com.unity.ugui@2.0/manual/SupportedEvents.html), [docs IDragHandler](https://docs.unity3d.com/Packages/com.unity.ugui@2.0/api/UnityEngine.EventSystems.IDragHandler.html)); `InputSystemUIInputModule` mặc định đọc bộ phím riêng *DefaultInputActions* ([docs](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.19/manual/UISupport.html)) → lệch luật "mọi phím qua `GameControls`" (0033) và bảng đổi phím 7l f. Nên: tự kiểm con trỏ bằng `RectTransformUtility` + đọc nút qua `GameInput` (`MoveClick` = click trái, `UseTool` = chuột phải, `Point`).
3. **Bố cục** (đơn vị tham chiếu 1280 × 720 của CanvasScaler; Canvas `sortingOrder = 12`):
   - Ô: 56 × 56, cách nhau 6. Nền ô `(0, 0, 0, 0.55)`; ô đang chọn nền `(0.95, 0.8, 0.2, 0.9)`. Hình: Image cách mép ô 8 (40 × 40), màu theo Quyết định 4; ô trống ẩn hình. Số lượng: Text cỡ 16, căn dưới-phải, cách mép 3, hiện khi ô không trống (kể cả 1). Nhãn loại cây (chỉ túi hạt / nông sản): Text cỡ 13 căn trên-phải = `crops[crop].id` (vd `A`); không có Sim → số crop. Ô túi phụ thêm nhãn phím (Text cỡ 13 căn trên-trái): `1`…`9`, `0` theo vị trí trong trang (vị trí ≥ 10 không nhãn).
   - **Túi phụ:** neo giữa-dưới (anchor/pivot `(0.5, 0)`), đáy cách mép dưới 12; một hàng `PageSize` ô (M1: 2 ô có thật — trang chỉ vẽ `PageEnd − PageStart` ô). Hiện khi `HotbarShown`.
   - **Túi:** neo giữa-dưới, đáy cách mép dưới 84 (trên túi phụ). Lưới 5 cột, số hàng = ceil(PageSize / 5) (M1: 2 hàng). Trên lưới: tiêu đề `inv.bag_title` (trái) + chữ trang `inv.page` (phải). Dưới lưới: dòng gợi ý `inv.bag_hint` cỡ 14.
   - **Kho:** neo phải-giữa (anchor/pivot `(1, 0.5)`), cách mép phải 12; lưới 5 cột, số ô = `Main.PageSize` của túi (M1: 10); tiêu đề = dòng đầu `SiteStatus.StockPanelText(...)`; chữ trang như túi.
   - **Nút trang** `<` và `>` (Text cỡ 20 trên ô nền 28 × 28) cạnh chữ trang của túi / kho, và hai bên túi phụ; chỉ hiện khi nơi đó có `PageCount > 1` (kho: số trang theo danh sách vật phẩm Quyết định 5).
   - **Bảng thông tin:** neo trái-giữa (anchor/pivot `(0, 0.5)`), cách mép trái 12, rộng 280, nền `(0, 0, 0, 0.7)`; 4 dòng Text cỡ 18: tên (đậm — dùng `FontStyle.Bold`), cách kiếm, số trong túi, số trong kho. Hiện khi có ô đang chọn.
4. **Hình giữ chỗ** (task contract: icon dựng lúc chạy): `IconColor(ItemId)` tĩnh — `Block` → `DropView.ColorOf(item)`; `SeedBag` → `DropView.SeedBagColor`; `Produce` → `(0.95, 0.55, 0.15, 1)`; `Tool` → `(0.35, 0.35, 0.45, 1)`.
5. **Ô kho** = danh sách vật phẩm kho có số > 0, thứ tự cố định: túi hạt cây 0..n−1, rồi nông sản cây 0..n−1 (`n = Sim.Config.crops.Length`); trang `Main.PageSize` ô; phần thiếu là ô trống. Chọn ô kho lưu **`ItemId`** (không lưu chỉ số), nên danh sách co lại vẫn chọn đúng; vật hết → bỏ chọn.
6. **Mở / đóng** (đề xuất Planner ghi `[đề xuất Planner]` trong Kết quả để Planner chép vào danh sách xem cuối):
   - B: bật/tắt túi. Đóng túi → đóng cả kho + bỏ chọn.
   - P: bật/tắt `hotbarHidden`. `HotbarShown = BagOpen || !hotbarHidden` (0034: mở túi thì túi phụ luôn hiện).
   - Chuột phải khối kho → đi tới 2 ô (đã có) → `SimView.OpenStockAt(index)` kiểm tầm 10 ô như cũ rồi gọi `InventoryView.OpenStock(index)`: **mở kho thì mở luôn túi** (đề xuất Planner: chuyển đồ chỉ giữa các nơi đang mở — 0034 — nên kho cần túi mở cùng).
   - Kho đang mở mà nhân vật xa khối kho đó > `PlayerRules.TargetReach` (10, như 0034 "xa kho > 10 ô") → kho tự đóng (túi giữ nguyên).
   - Esc (`GameInput.PanelsClose`): có túi / kho / bảng thông tin đang mở → đóng hết + bỏ chọn.
   - `public static bool AnyOpen` = `BagOpen || StockOpen || HasSelection` (túi phụ đang hiện **không** tính).
7. **Chọn + bảng thông tin:** click trái ô có đồ → chọn (nơi chứa + ô; kho: `ItemId`); click lại đúng ô đó → bỏ chọn; click ô trống → bỏ chọn. Chọn được ô túi phụ khi túi đóng (xem thông tin). Nội dung:
   - tên = `ItemName(item)`: túi hạt `inv.seeds` / nông sản `inv.produce` với `{0}` = `SiteStatus.CropName(GameSettings.Text, cfg, crop)` (không có Sim → số crop); khối / camera = `TerrainEditInput.ItemLabel(item)`;
   - cách kiếm theo loại: `inv.source_block`, `inv.source_seeds`, `inv.source_produce`, `inv.source_camera`;
   - `inv.info_bag` với `view.Bag.Count(item)`; `inv.info_stock` với số trong kho (không có Sim → `inv.info_no_stock`).
   - Cập nhật mỗi frame khi đang chọn.
8. **Chuột phải ô → cầm** (0034 mục 4): `TerrainEditInput.HoldItem(item)`; được → đóng túi (và kho, theo Quyết định 6) + bỏ chọn. Không cầm được (nông sản, hạt khi không có Sim, ô trống) → không đổi gì; nông sản → dòng kết quả `inv.not_holdable` qua hàm mới `TerrainEditInput.SetResult(string)`. Chuột phải ô kho: không làm gì (đồ trong kho không cầm thẳng).
9. **Phím 1–0** (0034 mục 6): action mới `Player/Hotbar1`…`Player/Hotbar10` (`<Keyboard>/1`…`<Keyboard>/9`, `<Keyboard>/0`). Chỉ khi **túi và kho đều đóng** (khi mở, 1–3 dành cho chuyển đồ ở 0063; 4–0 không làm gì). Phím thứ k (0..9) → ô `Hotbar.PageStart(trang) + k` nếu `< PageEnd` → cầm như Quyết định 8 (ô trống → không làm gì). Không cần túi phụ đang hiện.
10. **Chặn con trỏ:** `public static bool BlocksPointer(Vector2 screen)` = con trỏ trên bất kỳ phần đang hiện (túi phụ, túi, kho, bảng thông tin, nút trang) **hoặc** `Time.frameCount == consumedFrame` (frame mà InventoryView vừa xử lý click — tránh cùng frame click rơi xuống world sau khi túi đóng). Thêm vào: `TerrainEditInput.Update` (cạnh `SimView.BlocksPointer`), `PlayerCharacter` `:353`, `FollowCamera` (thay điều kiện `!alt`). Lướt: `PlayerCharacter:395` thêm `&& !InventoryView.AnyOpen`.
11. **Bỏ tạm cũ** (task contract: "7l d thay bảng túi cũ + phím 1–4 tạm"):
    - `SimView`: bảng túi + bảng kho IMGUI, nút "Túi (B)" / "Kho (K)", `BagRow`, `BagOpen`, `ToggleBag`, `BagRows`, `HoldFromBag`, `StockOpen`, `ToggleStock`, `CanTransferStock`, `StockMessage`, `TransferStock`, 4 hàm `*ButtonRect` / `*PanelRect` của túi / kho; phần đọc phím `:122–124`. Giữ: bảng ô ruộng, `OpenPlotId`, `PlotPanelRect`, `StorageIdOf`, `OpenStockAt` (đổi thân), dòng kho trong HUD (`StorageText`).
    - `TerrainEditInput`: `selected`, `SelectedMaterial`, `SelectMaterial`, đọc `SelectBlock1–4`, Alt + lăn, dòng `selected = i` trong `HoldItem`, các dòng túi tạm trong `HudText`. Giữ `Palette` / `PaletteKeys` / `Name` (tên vật liệu).
    - Input: xoá `Player/SelectBlock1–4`, `Player/ToggleStock` (K), `Player/CycleBlockModifier` (Alt), `Panels/TransferAll` (Shift). Thêm `Player/Hotbar1–10`, `Player/ToggleHotbar` (`<Keyboard>/p`). Tổng action: 35 − 7 + 11 = **39**.
    - `Simulation.StoreToStock` / `TakeFromStock` (Core) **giữ** (còn test lõi; không còn ai gọi từ client).
12. **Không làm:** kéo thả, Tab, 1/2/3 (0063); avatar / HUD gọn / bỏ chữ hướng dẫn (7l e); bảng đổi phím (7l f); cảm ứng; art icon thật.

## Phạm vi
- Được tạo: `Client/InventoryView.cs` (+ `.meta` do Unity tạo), `Tests/Client/InventoryPlayTests.cs` (+ `.meta`).
- Được sửa:
  - Client: `Client/SimView.cs`, `Client/TerrainEditInput.cs`, `Client/SettingsView.cs` (chỉ `OtherPanelOpen`), `Client/PlayerCharacter.cs` (chỉ `:353`, `:395`), `Client/FollowCamera.cs` (chỉ `:100–102`), `Client/GameInput.cs`, `Client/Input/GameControls.inputactions`.
  - Chuỗi: `Client/Resources/Lang/vi.json`, `loc/en.json`, `loc/context.json`.
  - Test: `Tests/ClientEdit/GameControlsTests.cs`, `Tests/Client/PlayerBagSowPlayTests.cs`, `Tests/Client/ControlModePlayTests.cs`, `Tests/Client/SimFarmUiPlayTests.cs`, `Tests/Client/SettingsPlayTests.cs`; test cũ khác chỉ theo quy tắc bước 10.
  - Tài liệu: `design/02-gameplay/controls-ui.md` — **chỉ** ô "Bảng kho" và dòng ghi chú PC M1 nếu còn nhắc `K` / nút `Kho (K)` / `Túi (B)` / `1–4`: thêm "(bỏ ở 7l d, brief 0062)". Không sửa chỗ khác của `design/`.
  - File brief này (mục Kết quả / Kết quả test / Vướng mắc).
- Không được sửa: `Core/` (mọi file), `Client/WalkToAct.cs`, `Client/MinimapView.cs`, `Client/DropView.cs`, `design/` ngoài dòng nêu trên, task contract, mọi file khác.

## Các bước
1. **Input** (`GameControls.inputactions` — sửa JSON giữ định dạng; GUID mới cho action / binding: chuỗi 36 ký tự dạng `xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx`, sinh bằng `python3 -c "import uuid; print(uuid.uuid4())"`):
   - Xoá action + binding: `Player/SelectBlock1`…`4`, `Player/ToggleStock`, `Player/CycleBlockModifier`, `Panels/TransferAll`.
   - Thêm vào map `Player` (Button): `Hotbar1` `<Keyboard>/1`, `Hotbar2` `<Keyboard>/2`, …, `Hotbar9` `<Keyboard>/9`, `Hotbar10` `<Keyboard>/0`, `ToggleHotbar` `<Keyboard>/p`. Các trường khác (`expectedControlType: "Button"`, `processors`, `interactions`, `groups`) chép như action Button có sẵn (vd `ToggleBag`).
   - `GameInput.cs`: xoá 7 thuộc tính tương ứng; thêm `ToggleHotbar => Get("Player/ToggleHotbar")` và
     ```csharp
     static readonly string[] HotbarNames = { "Player/Hotbar1", "Player/Hotbar2", "Player/Hotbar3", "Player/Hotbar4", "Player/Hotbar5", "Player/Hotbar6", "Player/Hotbar7", "Player/Hotbar8", "Player/Hotbar9", "Player/Hotbar10" };
     /// <summary>Phím ô túi phụ k = 0..9 (1–9, 0 — 0034 mục 6).</summary>
     public static InputAction Hotbar(int k) => Get(HotbarNames[k]);
     ```
     **Lưu ý test `GameInputCoversAllActions`** tìm `Get("Map/Action")` bằng regex: chuỗi trong mảng không khớp mẫu `Get\("…"\)` → viết 10 dòng thuộc tính riêng `public static InputAction Hotbar1 => Get("Player/Hotbar1");` … `Hotbar10`, và hàm `Hotbar(int k)` trả qua `switch` 10 nhánh thuộc tính đó (bỏ mảng `HotbarNames`).
   - Xong khi: parse JSON được; biên dịch còn lỗi ở chỗ dùng phím cũ (sửa ở bước 4–6).
2. **`Client/InventoryView.cs`** — khung (summary tiếng Việt; chữ hiển thị qua `GameSettings.Text`):
   ```csharp
   /// <summary>
   /// Túi / túi phụ / kho ô vuông (0034 mục 4, 6; world 7l d2, brief 0062). uGUI dựng bằng code, không EventSystem (tự kiểm con trỏ, phím qua GameInput).
   /// B túi, P ẩn/hiện túi phụ (mở túi luôn hiện), kho mở từ SimView.OpenStockAt (mở luôn túi, tự đóng khi xa > 10 ô).
   /// Click trái ô → bảng thông tin lề trái; chuột phải ô → cầm, túi đóng; 1–0 = ô túi phụ khi túi + kho đóng.
   /// </summary>
   [RequireComponent(typeof(TerrainChunkView))]
   public class InventoryView : MonoBehaviour
   {
       public static InventoryView Active { get; private set; }
       public static bool AnyOpen => Active != null && (Active.BagOpen || Active.StockOpen || Active.HasSelection);
       public static bool BlocksPointer(Vector2 screen) => Active != null && Active.Covers(screen);

       public bool BagOpen { get; private set; }
       public bool StockOpen { get; private set; }
       public int StockIndex { get; private set; } = -1;   // khối kho đang mở
       public bool HotbarHidden { get; private set; }
       public bool HotbarShown => BagOpen || !HotbarHidden;
       public int BagPage { get; private set; }
       public int HotbarPage { get; private set; }
       public int StockPage { get; private set; }
       public bool HasSelection { get; private set; }
       public ItemContainer SelectedContainer { get; private set; }
       public int SelectedSlot { get; private set; } = -1;  // Bag / Hotbar: chỉ số ô trong SlotInventory
       public ItemId SelectedStockItem { get; private set; } // Stock
       public bool InfoVisible => HasSelection;
       /// <summary>4 dòng bảng thông tin nối "\n" (cho test); "" khi không chọn.</summary>
       public string InfoText { get; }

       public void ToggleBag();          // B
       public void SetBagOpen(bool open); // đóng → đóng kho + bỏ chọn
       public void ToggleHotbar();       // P
       public bool OpenStock(int storageIndex); // không có Sim → false; mở kho + mở túi, StockPage = 0
       public void CloseAll();           // Esc
       public void NextPage(ItemContainer c); public void PrevPage(ItemContainer c); // vòng (SlotInventory.NextPage / PrevPage; kho: theo danh sách)
       public bool Select(ItemContainer c, int slotOnPage); // chỉ số trong trang đang xem; ô trống → bỏ chọn, false
       public void ClearSelection();
       public ItemStack SlotAt(ItemContainer c, int slotOnPage); // trang đang xem; ngoài trang / trống → default
       public bool HoldFromSlot(ItemContainer c, int slotOnPage); // Quyết định 8
       public bool UseHotbarKey(int k);  // Quyết định 9
       public bool LeftClickAt(Vector2 screen);  // trúng ô → Select (toggle); trúng nút trang → Next/Prev; true nếu trúng phần nào của InventoryView
       public bool RightClickAt(Vector2 screen); // trúng ô túi / túi phụ → HoldFromSlot; true nếu trúng phần nào
       public bool Covers(Vector2 screen);
       /// <summary>Tâm ô (chỉ số trong trang) ra toạ độ màn hình (pixel, gốc dưới-trái). Nơi đó đang ẩn → Vector2(-1, -1).</summary>
       public Vector2 SlotScreenCenter(ItemContainer c, int slotOnPage);
       public string SlotCountText(ItemContainer c, int slotOnPage); // chữ số lượng đang vẽ ("" nếu trống)
       public static Color IconColor(ItemId item);   // Quyết định 4
       public string ItemName(ItemId item);          // Quyết định 7
   }
   ```
   Hành vi:
   - `Awake`: `Active = this`; lấy `TerrainChunkView`; dựng Canvas (mẫu MinimapView, Quyết định 3) — dựng ô theo `PageSize` của từng nơi lúc `view.Bag` có (dựng trễ ở `Update` đầu tiên có `view.Bag != null`); `GameSettings.Changed += RefreshText` (gán lại chữ cố định). `OnDestroy`: gỡ event, `Active = null` nếu là mình.
   - `Update` (thứ tự): chưa có túi → return. Đọc phím: `GameInput.ToggleBag` → `ToggleBag()`; `GameInput.ToggleHotbar` → `ToggleHotbar()`; `GameInput.PanelsClose` và `AnyOpen` → `CloseAll()`; nếu `!BagOpen && !StockOpen`: 10 phím `GameInput.Hotbar(k)` → `UseHotbarKey(k)`. Tự đóng kho (Quyết định 6). Con trỏ (`GameInput.HasPointer`): `GameInput.MoveClick.WasPressedThisFrame()` → `if (LeftClickAt(pos)) consumedFrame = Time.frameCount;`; `GameInput.UseTool.WasPressedThisFrame()` → `if (RightClickAt(pos)) consumedFrame = Time.frameCount;`. Cuối: `Refresh()` cập nhật hiện/ẩn, màu, số, nhãn, trang, bảng thông tin (mỗi frame; ≤ 30 ô, rẻ).
   - Không mở được bảng khi đang xem camera quan sát? → không cần kiểm (ObservationCamera đã chặn công cụ; túi vẫn mở được).
   - Không có `Sim`: kho luôn đóng, `OpenStock` → false, `inv.info_no_stock`.
   - Chọn ô bị trống đi (vd đồ dùng hết) → `Refresh` bỏ chọn.
   - Xong khi: biên dịch không lỗi; `NoDirectInputReadsInScope` (bước 7) qua.
3. **`TerrainEditInput`:**
   - `Awake`: thêm `Inventory = GetComponent<InventoryView>(); if (Inventory == null) Inventory = gameObject.AddComponent<InventoryView>();` + thuộc tính `public InventoryView Inventory { get; private set; }` (summary "Túi / túi phụ / kho ô vuông (brief 0062); thêm trong Awake nếu chưa có.").
   - Xoá theo Quyết định 11 (field `selected`, `SelectedMaterial`, `SelectMaterial`, 4 dòng `SelectBlock`, khối Alt + lăn, dòng `selected = i` trong `HoldItem`, 7 dòng túi tạm trong `HudText` từ `text += T("hud.bag");` tới `if (seeds > 0) ...`). Summary lớp: dòng đầu thay "phím 1–4 / Alt + lăn chọn loại khối" bằng "túi ô vuông ở InventoryView (brief 0062)". Summary `HoldItem` bỏ câu "Khối thuộc bảng 1–4 → đổi loại đang chọn theo".
   - Điều kiện chặn con trỏ `:117`: thêm `|| InventoryView.BlocksPointer(pointer)`.
   - Hàm mới: `/// <summary>Ghi dòng kết quả HUD (InventoryView báo không cầm được).</summary> public void SetResult(string text) => lastResult = text;`.
   - Xong khi: biên dịch không lỗi; `grep -n "SelectBlock\|SelectMaterial\|CycleBlockModifier\|hud.bag" client/Assets/Game/Client/*.cs` rỗng.
4. **`SimView`:** xoá theo Quyết định 11. `OpenStockAt(int index)` giữ kiểm tầm, thay `StockOpen = true; return true;` bằng `var inv = InventoryView.Active; return inv != null && inv.OpenStock(index);`. `AnyPanelOpen` → `active != null && active.OpenPlotId >= 0 || InventoryView.AnyOpen` (đặt ngoặc đúng: `(active != null && active.OpenPlotId >= 0) || InventoryView.AnyOpen`); summary đổi "Bảng ô ruộng, hoặc túi / kho / bảng thông tin (InventoryView)". `Covers`: chỉ còn `return OpenPlotId >= 0 && PlotPanelRect(gw, gh).Contains(g);`. `DrawFarmGui`: bỏ phần kho + túi (giữ từ `if (OpenPlotId < 0) return;`). Summary lớp: thay "nút Kho (K)" bằng "kho mở bằng chuột phải khối kho (InventoryView, brief 0062)".
   - Xong khi: `grep -n "BagOpen\|StockOpen\|ToggleBag\|ToggleStock\|TransferStock\|BagRows\|HoldFromBag" client/Assets/Game/Client/SimView.cs` rỗng.
5. **`SettingsView.OtherPanelOpen`:** dòng `sim.BagOpen || sim.StockOpen || sim.OpenPlotId >= 0` → `if (InventoryView.AnyOpen || (sim != null && sim.OpenPlotId >= 0)) return true;`.
6. **`PlayerCharacter`** `:353` thêm `|| InventoryView.BlocksPointer(pos)`; `:395` thêm `&& !InventoryView.AnyOpen`. **`FollowCamera`** `:100–102`: bỏ biến `alt`; điều kiện thành `if (!MinimapView.BlocksPointer(pos) && !TimeDial.BlocksPointer(pos) && !SimView.BlocksPointer(pos) && !InventoryView.BlocksPointer(pos))`.
7. **`GameControlsTests`:** `Expected()` xoá 7 dòng cũ, thêm `["Player/Hotbar1"] = B("<Keyboard>/1")` … `["Player/Hotbar10"] = B("<Keyboard>/0")`, `["Player/ToggleHotbar"] = B("<Keyboard>/p")`; hai chỗ `35` → `39`; `NoDirectInputReadsInScope` thêm `"InventoryView.cs"` vào danh sách file.
8. **Chuỗi** — **xoá** khỏi cả 3 file: `bag.button`, `bag.empty`, `bag.hint`, `bag.row_block`, `bag.row_produce`, `bag.row_seeds`, `bag.title`, `hold.none`, `hud.bag`, `hud.bag_item`, `hud.bag_sel`, `hud.seed_bag_a`, và mọi khoá `stock.*` (`stock.bag_none`, `stock.button`, `stock.dest_bag`, `stock.dest_stock`, `stock.full`, `stock.hint`, `stock.kind_produce`, `stock.kind_seeds`, `stock.line`, `stock.moved`, `stock.none`, `stock.produce_to_bag`, `stock.produce_to_stock`, `stock.seeds_to_bag`, `stock.seeds_to_stock`, `stock.too_far`). Trước khi xoá: `grep -rn "\"<khoá>\"" client/Assets/Game` từng khoá → chỉ còn trong 3 file chuỗi (còn chỗ dùng khác → giữ khoá, ghi vào Kết quả). Giữ `bag.full` (0061), `hold.empty`, `edit.open_stock`, `edit.stock_far`.
   **Thêm** (thứ tự bảng chữ cái; `inv.*` nằm giữa `hud.*` và `item.*`):

   | key | vi | en | context |
   |---|---|---|---|
   | `inv.bag_hint` | `Chuột phải ô: cầm · Click trái: xem · B / Esc: đóng` | `Right-click a slot: hold · Left-click: details · B / Esc: close` | `Footer hint in the bag panel. Right-clicking a slot makes the character hold that item; left-clicking shows its details. 'B / Esc' are keyboard keys, keep them.` |
   | `inv.bag_title` | `Túi (B)` | `Bag (B)` | `Title of the bag (inventory) panel. '(B)' is the keyboard shortcut, keep the letter B.` |
   | `inv.info_bag` | `Trong túi (gồm túi phụ): {0}` | `In bag (incl. quick bar): {0}` | `Item details panel. {0} = how many of this item the player carries, counting the bag and the quick bar ('túi phụ' = small always-visible bar at the bottom of the screen).` |
   | `inv.info_no_stock` | `Trong kho: —` | `In storage: —` | `Item details panel when there is no farm storage in this scene. Keep the dash.` |
   | `inv.info_stock` | `Trong kho: {0}` | `In storage: {0}` | `Item details panel. {0} = how many of this item are in the farm's shared storage ('kho').` |
   | `inv.not_holdable` | `Không cầm được {0}` | `Can't hold {0}` | `Result line when the player right-clicks a bag slot whose item cannot be held in hand (e.g. harvested produce). {0} = item name.` |
   | `inv.page` | `{0}/{1}` | `{0}/{1}` | `Page indicator of a slot grid: {0} = current page, {1} = page count.` |
   | `inv.produce` | `Nông sản {0}` | `{0} produce` | `Item name of harvested produce. {0} = crop display name from crop.name (e.g. 'Cây A' = 'Crop A'). 'Nông sản' = the crop's yield.` |
   | `inv.seeds` | `Túi hạt {0}` | `{0} seed bag` | `Item name of a seed bag. {0} = crop display name from crop.name (e.g. 'Cây A' = 'Crop A').` |
   | `inv.source_block` | `Cách có: đào khối này (chế độ Đào, Q)` | `How to get: dig this block (Dig mode, Q)` | `Item details panel: how a block item is obtained. 'Q' is the Dig mode key, keep it.` |
   | `inv.source_camera` | `Cách có: có sẵn từ đầu, không mất khi dùng` | `How to get: you start with it; it is not used up` | `Item details panel for the observation camera item.` |
   | `inv.source_produce` | `Cách có: thu hoạch cây chín` | `How to get: harvest a ripe crop` | `Item details panel: how harvested produce is obtained.` |
   | `inv.source_seeds` | `Cách có: thu hoạch cây chín, túi hạt đầu game, lấy từ kho` | `How to get: harvest ripe crops, the starter seed bag, or take from storage` | `Item details panel: ways to obtain seed bags.` |

   Xong khi: parse JSON 3 file; `loc_check.py check loc/en.json` exit 0; `HardcodedTextScanTests` qua.
9. **Test mới** `Tests/Client/InventoryPlayTests.cs` (namespace `Game.Client.Tests`; `SetUp` / `TearDown` / helper chép từ `PlayerBagSowPlayTests` — gồm `GoNearStorage`, `GoFarFromStorage`; lấy `inv = input.Inventory` sau SetUp; chuỗi tiếng Việt trong assert ghi `// vi-text`). Mỗi test bắt đầu `inv.CloseAll()` nếu cần. Ô túi 0 là camera quan sát (0061) — tính chỉ số theo đó:
   - **`BagAndHotbarToggle`**: đầu: `!BagOpen`, `HotbarShown`; `ToggleHotbar()` → `!HotbarShown`; `ToggleBag()` → `BagOpen`, `HotbarShown`; `ToggleBag()` → `!HotbarShown`; `ToggleHotbar()` → `HotbarShown`.
   - **`SlotsShowIconAndCount`**: `view.Bag.Add(BlockMaterial.Dirt, 3)`; `ToggleBag()`; 1 frame; `SlotAt(Bag, 1)` = Dirt × 3; `SlotCountText(Bag, 1) == "3"`; `SlotCountText(Bag, 0) == "1"` (camera); `SlotCountText(Bag, 2) == ""`; `IconColor(ItemId.Block(Dirt)) == DropView.ColorOf(ItemId.Block(Dirt))`; `NextPage(Bag)` → `BagPage == 0` (1 trang, vòng).
   - **`InfoPanelCounts`**: `view.Bag.AddSeeds(0, 4)`; `s0 = sv.Sim.State.Stock.Seeds[0]`; `ToggleBag()`; `LeftClickAt(SlotScreenCenter(Bag, 1))` true → `InfoVisible`; `InfoText` chứa `Túi hạt Cây A`, `Trong túi (gồm túi phụ): 4`, `Trong kho: {s0}` (// vi-text); `view.Bag.Hotbar.AddToSlot(0, ItemId.SeedBag(0), 2)`; 1 frame → chứa `Trong túi (gồm túi phụ): 6`; `LeftClickAt` cùng ô lần nữa → `!InfoVisible`.
   - **`RightClickSlotHoldsAndCloses`**: `view.Bag.Add(Dirt, 2)`; `view.Bag.AddProduce(0, 1)` (ô 2); `ToggleBag()`; 1 frame; `RightClickAt(SlotScreenCenter(Bag, 2))` true → `input.Control.HasHeld == false`, `BagOpen` vẫn true, `input.HudText()` chứa `Không cầm được` (// vi-text); `RightClickAt(SlotScreenCenter(Bag, 1))` → `input.Control.Held == ItemId.Block(Dirt)`, `!BagOpen`, `!HasSelection`.
   - **`HotbarKeysHold`**: `view.Bag.Hotbar.AddToSlot(0, ItemId.Block(Dirt), 2)`; túi đóng → `UseHotbarKey(0)` true, `Held == Dirt`; `input.Control.ClearToDefault()`; `UseHotbarKey(1)` false (ô trống); `UseHotbarKey(5)` false (ngoài 2 ô); `ToggleBag()` → `UseHotbarKey(0)` false; `ToggleBag()`; `ToggleHotbar()` (ẩn) → `UseHotbarKey(0)` true (phím vẫn dùng được khi ẩn).
   - **`StockOpensWithBagAndAutoCloses`**: `yield return GoNearStorage()`; `sv.OpenStockAt(0)` true → `StockOpen`, `BagOpen`; `sv.Sim.State.Stock.Seeds[0] > 0` → `SlotAt(Stock, 0).Item == ItemId.SeedBag(0)`; `ToggleBag()` → `!StockOpen`; mở lại `sv.OpenStockAt(0)`; `yield return GoFarFromStorage()`; 2 frame → `!StockOpen`, `BagOpen`.
   - **`PointerBlockAndConsumedClick`**: `view.Bag.Add(Dirt, 1)`; `ToggleBag()`; 1 frame; `p = SlotScreenCenter(Bag, 1)` → `InventoryView.BlocksPointer(p)`; `ToggleBag()`; 1 frame → `!BlocksPointer(p)` (ô túi ẩn; điểm không trùng túi phụ — nếu trùng, dùng `SlotScreenCenter(Bag, 9)` đo lúc mở); `BlocksPointer(SlotScreenCenter(Hotbar, 0))` true; `ToggleHotbar()` → false; giữa màn hình `(Screen.width / 2, Screen.height / 2)` → false.
   - **`EscAndSettings`**: `settings = go.GetComponent<SettingsView>()`; `ToggleBag()` → `settings.OtherPanelOpen()` true; `CloseAll()` → `!AnyOpen`; `settings.OtherPanelOpen()` false (không cầm gì, không bảng khác).
   - Xong khi: 8 test có trong file, biên dịch không lỗi.
10. **Test cũ** — sửa / xoá đúng danh sách, giữ ý nghĩa:
    - `PlayerBagSowPlayTests.BagRowsList` → **xoá** (thay bằng `SlotsShowIconAndCount`).
    - `PlayerBagSowPlayTests.BagPanelBlocksPointer` → **xoá** (thay bằng `PointerBlockAndConsumedClick`).
    - `PlayerBagSowPlayTests.HoldSeedRules`: `sv.ToggleBag()` → `input.Inventory.ToggleBag()`; `sv.HoldFromBag(ItemId.SeedBag(0))` → `input.Inventory.HoldFromSlot(ItemContainer.Bag, 1)` (ô 1 = túi hạt, ô 0 camera); `sv.BagOpen` → `input.Inventory.BagOpen`; `sv.HoldFromBag(ItemId.SeedBag(1))` (hết hạt loại 1) → `input.HoldItem(ItemId.SeedBag(1))` false.
    - `PlayerBagSowPlayTests.StockTransferInReach`, `StockFull`, `StockOutOfReach` → **xoá** (giao diện chuyển cũ bỏ; 0063 thêm test chuyển mới; luật lõi còn ở `PlayerFarmTests`).
    - `PlayerBagSowPlayTests.ClickStorageBlock`: `sv.StockOpen` → `input.Inventory.StockOpen`; `sv.ToggleStock()` → `input.Inventory.CloseAll()`.
    - `ControlModePlayTests.ModeKeysAndHoldItem`: xoá các dòng dùng `SelectedMaterial` / `SelectMaterial` và assert `Túi không còn cát`; giữ phần `HoldItem`.
    - `ControlModePlayTests.BagPanelHoldsAndCloses`: viết lại — `input.Inventory.ToggleBag()`; `input.Inventory.SlotAt(ItemContainer.Bag, 0).Item == ItemId.Tool(ToolItem.ObservationCamera)`; `input.Inventory.HoldFromSlot(ItemContainer.Bag, 0)` true; `!BagOpen`; `Held.Kind == Tool`; `view.Bag.AddProduce(0, 1)`; `ToggleBag()`; `HoldFromSlot(Bag, 1)` false.
    - `SimFarmUiPlayTests.StockToggleAndPointerBlock`: bỏ phần `ToggleStock` / `StockButtonRect`; đổi tên thành **`PlotPanelBlocksPointer`**, giữ: giữa màn hình không chặn; mở ô ruộng → `PlotPanelRect` chặn.
    - `SettingsPlayTests.Keys_OpenClose_AndSave`: `sv.ToggleBag();` → `go.GetComponent<InventoryView>().ToggleBag();` (nếu SetUp không có `TerrainEditInput` thì `InventoryView` không tự thêm → thêm `go.AddComponent<InventoryView>()` trong test trước dòng đó; ghi vào Kết quả).
    - Test khác hỏng vì: số action 35 / phím cũ; dòng túi tạm trong `HudText`; `SimView.BlocksPointer` không còn chặn nút túi / kho → sửa theo ý mới (ghi tên). Lý do khác → dừng, `cần Opus`.
11. **controls-ui.md:** chỉ chú thích như Phạm vi (vd ô "Bảng kho" đã có "(trước: K)" → giữ; dòng ghi chú PC M1 `:24` có `**B** (hoặc nút \`Túi (B)\`)`, `**K** (hoặc nút \`Kho (K)\`…)` → nối "(bảng + nút cũ bỏ ở 7l d, brief 0062)"). Không đổi trạng thái file.

## Tiêu chí xong
- EditMode + PlayMode qua hết, 0 lỗi console (qua MCP).
- `grep -rn "SelectBlock\|ToggleStock\|CycleBlockModifier\|TransferAll\|BagRows\|HoldFromBag" client/Assets/Game` rỗng.
- `grep -rn "\"bag.empty\"\|\"stock\.\|\"hud.bag" client/Assets/Game/Client/Resources/Lang/vi.json loc/en.json loc/context.json` rỗng.
- `loc_check.py check loc/en.json` exit 0; `python3 tools/check_design.py` exit 0.
- `git diff --stat <base>..HEAD -- client/Assets/Game/Core` rỗng.
- 8 test ở bước 9 có và qua; danh sách test sửa / xoá ở bước 10 ghi trong Kết quả.

## Test sau khi xong

### Phần cần test
- Phím mới / bỏ phím cũ (bước 1, 7) · `GameControlsTests` (`AssetMatchesDefaultTable` 39 action, `NoDuplicateBindingsWithinMap`, `GameInputCoversAllActions`, `NoDirectInputReadsInScope` có `InventoryView.cs`) · qua.
- Túi + túi phụ mở / đóng, P (bước 2) · `BagAndHotbarToggle` · đúng `HotbarShown`.
- Ô hình + số, trang (bước 2) · `SlotsShowIconAndCount` · số đúng, màu đúng, trang vòng.
- Bảng thông tin (bước 2, 8) · `InfoPanelCounts` · tên, số trong túi gồm túi phụ, số trong kho.
- Chuột phải ô → cầm (bước 2, 3) · `RightClickSlotHoldsAndCloses`, `BagPanelHoldsAndCloses`, `HoldSeedRules` · cầm + đóng túi; nông sản báo không cầm được.
- Phím 1–0 (bước 1, 2) · `HotbarKeysHold` · khi túi đóng; không khi túi mở; ẩn vẫn dùng.
- Kho (bước 2, 4) · `StockOpensWithBagAndAutoCloses`, `ClickStorageBlock` · mở kèm túi, đóng túi đóng kho, xa > 10 ô tự đóng.
- Chặn con trỏ + click không rơi xuống world (bước 2, 3, 6) · `PointerBlockAndConsumedClick`, `PlotPanelBlocksPointer` · đúng vùng chặn.
- Esc / Cài đặt (bước 5) · `EscAndSettings`, `SettingsPlayTests` (toàn bộ) · túi mở thì Esc không mở Cài đặt.
- Chuỗi (bước 8) · `HardcodedTextScanTests`, `SettingsPlayTests.LanguageSwitch_UpdatesUgui`, `loc_check.py` · không thiếu / thừa khoá.
- Regression điều khiển (đi, chế độ, cầm, đặt, gieo, F, camera, zoom, lướt) · toàn bộ `ControlModePlayTests`, `PlayerBagSowPlayTests`, `ClickMovePlayTests`, `CameraPcPlayTests`, `PlayerCharacterPlayTests`, `ObservationCameraPlayTests`, `DropPlayTests`, `TerrainEditPlayTests`, `PlayerFarmPlayTests`, `SimFarmUiPlayTests`, `MinimapViewPlayTests`, `MinimapCornerPlayTests`, `TimeDialPlayTests`, `GameControlsPlayTests` · qua.
- Toàn bộ EditMode + PlayMode · qua.

### Lệnh executor chạy
1. Parse JSON (inputactions, vi.json, en.json, context.json) + `python3 "<Plugin root>/skills/translation/scripts/loc_check.py" check loc/en.json` + các `grep` ở Tiêu chí xong + `python3 tools/check_design.py`.
2. Unity Editor mở qua MCP ([0010](../../design/decisions/0010-mcp-for-unity.md)): `refresh_unity` → `read_console` (0 lỗi biên dịch) → `run_tests` EditMode (toàn bộ) → `run_tests` PlayMode (toàn bộ) → `read_console` lỗi. Ghi dòng tổng kết số test thật. Editor tắt → `sh "<Plugin root>/scripts/run.sh" verify_run client_test client_test_play`.
3. **Dọn:** sau `run_tests` PlayMode → `git checkout -- client/ProjectSettings/EditorSettings.asset`. Không commit `client/Assets/InitTestScene*`.
4. `sh "<Plugin root>/scripts/run.sh" verify_run --all` trước commit (client_test / client_test_play exit 1 vì Editor mở → ghi `Chưa kiểm được` + kết quả MCP).
5. Commit code + test + chuỗi + controls-ui (chỉ file trong Phạm vi), message `brief 0062: 7l d2 slot grid UI for bag, hotbar and storage (B, P, info panel, right-click hold, 1-0)`; commit kết quả brief riêng `brief 0062: result`. Không push.
6. Ghi giờ bắt đầu / kết thúc **mọi vòng** vào Kết quả (giờ thực tế M1).

### User tự test
Gom vào danh sách chơi thử cuối của task (user 2026-10-24). Mục cần xem (Planner chép vào danh sách cuối) — scene SimView, Mac:
1. Túi phụ 2 ô giữa-dưới có nhãn phím `1`, `2`; P ẩn/hiện; B mở túi (lưới 5 × 2 phía trên túi phụ), túi phụ luôn hiện khi túi mở.
2. Ô có hình màu + số góc dưới-phải; túi hạt / nông sản có chữ loại cây góc trên-phải; camera quan sát ở ô đầu.
3. Click trái ô → bảng lề trái: tên, cách có, số trong túi (gồm túi phụ), số trong kho.
4. Chuột phải ô khối / túi hạt / camera → cầm, túi đóng; chuột phải ô nông sản → `Không cầm được …`.
5. Phím 1 / 2 cầm vật ở ô túi phụ (khi túi đóng).
6. Chuột phải khối kho → đi tới → kho mở bên phải cùng túi; đi xa > 10 ô → kho tự đóng.
7. Click lên túi / túi phụ / kho không làm nhân vật đi; lăn chuột trên túi không thu phóng camera.
8. Thử trang: sửa `design/data/sim_m1.json` → `player.bagPageSize` = 5 (+ bản sao `client/Assets/Game/Client/Resources/Config/sim_m1.json`) → túi 2 trang, nút `<` `>`, qua trang cuối về trang 1; trả lại 10 sau khi xem.
9. Bố cục / cỡ ô / màu có ổn không (số tạm Planner).

## Dừng và chuyển lại Opus khi
- Brief mơ hồ hoặc mâu thuẫn với code thật (vd tên hàm ở "Bối cảnh" khác, 0061 chưa có `Main` / `Hotbar`).
- Cần API Unity ngoài danh sách ở "Bối cảnh".
- Cần một quyết định thuộc AGENTS.md §3.4 hoặc ngoài "Quyết định đã chốt".
- Cùng một lỗi sửa 2 lần vẫn chưa qua.
- Phải sửa file ngoài "Phạm vi".
- Test cũ hỏng mà không thuộc danh sách / quy tắc bước 10.

---
<!-- Từ đây trở xuống: EXECUTOR ghi. -->

## Kết quả
- Base commit: b9775f7
- Chạy bằng: claude-sonnet-5-5 · medium
- Bắt đầu: 2026-10-24 04:04
- Kết thúc: 2026-10-24 04:23 (một vòng, ≈ 0,5 giờ thực tế; phần lớn là đợi biên dịch + EditMode ~7 phút + PlayMode 2 lần).
- Commit: 14ac2e7 (`brief 0062: 7l d2 slot grid UI ...`).
- File đã đổi: Client/InventoryView.cs (+meta, mới), SimView.cs, TerrainEditInput.cs, SettingsView.cs, PlayerCharacter.cs, FollowCamera.cs, GameInput.cs, Input/GameControls.inputactions; Lang/vi.json, loc/en.json, loc/context.json; design/02-gameplay/controls-ui.md (chỉ chú thích dòng PC M1); Tests: InventoryPlayTests (+meta, mới), GameControlsTests, PlayerBagSowPlayTests, ControlModePlayTests, SimFarmUiPlayTests, SettingsPlayTests, **DropPlayTests** (xem dưới).
- Test cũ sửa / xoá (bước 10): xoá `PlayerBagSowPlayTests.BagRowsList`, `BagPanelBlocksPointer`, `StockTransferInReach`, `StockFull`, `StockOutOfReach`; sửa `PlayerBagSowPlayTests.HoldSeedRules`, `ClickStorageBlock`; `ControlModePlayTests.ModeKeysAndHoldItem`, `BagPanelHoldsAndCloses`; `SimFarmUiPlayTests.StockToggleAndPointerBlock` → `PlotPanelBlocksPointer`; `SettingsPlayTests.Keys_OpenClose_AndSave` (SetUp đã có `TerrainEditInput` nên `InventoryView` tự có, không cần AddComponent); `GameControlsTests` (39 action, `InventoryView.cs` vào danh sách). **Ngoài danh sách nhưng thuộc quy tắc cuối bước 10 ("dòng túi tạm trong HudText"):** `DropPlayTests.StarterBagNearSpawnAndHud` — dòng assert `HudText` chứa `Túi hạt Cây A: 4` hỏng vì HUD không còn dòng túi tạm → đổi thành kiểm `input.Inventory.SlotAt(Bag, 1).Count == 4`.
- Chuỗi: không khoá nào bị xoá còn chỗ dùng khác (đã grep từng khoá trước khi xoá); đã xoá `bag.*` (trừ `bag.full`), `hold.none`, `hud.bag*`, `hud.seed_bag_a`, toàn bộ `stock.*`; thêm 13 khoá `inv.*`.
- **[đề xuất Planner]** (chép vào danh sách xem cuối): mở kho → mở luôn túi; P ẩn/hiện túi phụ, mở túi thì luôn hiện; kho tự đóng khi xa > 10 ô; click ô túi phụ khi túi đóng cũng chọn được để xem thông tin.
- Lưu ý cho Planner (ngoài phạm vi, không sửa): chuỗi hướng dẫn HUD (`hud.help*`) vẫn còn câu "B: túi (bấm vật phẩm để cầm)" và "1–4 / Alt + lăn chuột: chọn loại khối" — đã lỗi thời sau brief này; thuộc 7l e (bỏ chữ hướng dẫn) hay cần sửa sớm hơn thì Planner quyết. `ScreenOf` trong `PlayerBagSowPlayTests` giờ không dùng (để nguyên).
- Lệch so với brief: không có lệch về hành vi. Chi tiết tự chọn trong khung brief: nút trang túi / kho nằm ở hàng tiêu đề (phải) theo Quyết định 3; panel kho / túi có nền `(0,0,0,0.35)` để chặn con trỏ cả khoảng giữa các ô (brief không ghi màu nền panel — số tạm).

## Kết quả test
- Lệnh 1: parse JSON (inputactions, vi.json, en.json, context.json) OK; `loc_check.py check loc/en.json` exit 0 (163/163); `grep UnityEngine Core` rỗng; 2 `grep` Tiêu chí xong rỗng (SelectBlock/ToggleStock/…; bag.empty/stock./hud.bag); `git diff -- Core` rỗng; `check_design.py` exit 0.
- Lệnh 2 (MCP): biên dịch 0 lỗi console. EditMode **542/542 qua**. PlayMode lần 1: 232/233 — `DropPlayTests.StarterBagNearSpawnAndHud` (xem Kết quả, đã sửa); PlayMode lần 2 (toàn bộ, sau sửa): **233/233 qua**. Console 0 lỗi sau chạy.
- Lệnh 3: đã `git checkout -- client/ProjectSettings/EditorSettings.asset`; không commit `InitTestScene*`.
- Lệnh 4 `verify_run --all`: design exit 0, asset_check exit 0; client_test / client_test_play exit 1 vì Editor mở → **Chưa kiểm được** bằng dòng lệnh; kết quả MCP thay thế.
- Phím mới / bỏ phím cũ → **đạt** (`GameControlsTests` 39 action, `NoDirectInputReadsInScope` có `InventoryView.cs`). Túi + túi phụ, P → **đạt** (`BagAndHotbarToggle`). Ô hình + số, trang → **đạt** (`SlotsShowIconAndCount`). Bảng thông tin → **đạt** (`InfoPanelCounts`). Chuột phải ô → cầm → **đạt** (`RightClickSlotHoldsAndCloses`, `BagPanelHoldsAndCloses`, `HoldSeedRules`). Phím 1–0 → **đạt** (`HotbarKeysHold`). Kho → **đạt** (`StockOpensWithBagAndAutoCloses`, `ClickStorageBlock`). Chặn con trỏ → **đạt** (`PointerBlockAndConsumedClick`, `PlotPanelBlocksPointer`). Esc / Cài đặt → **đạt** (`EscAndSettings`, toàn bộ `SettingsPlayTests`). Chuỗi → **đạt** (`HardcodedTextScanTests`, `loc_check`, `SettingsPlayTests.LanguageSwitch_UpdatesUgui` trong 233 test). Regression điều khiển + toàn bộ EditMode / PlayMode → **đạt**.
- Click trái thật bằng chuột lên ô (đường `Update` → `LeftClickAt`), bố cục / màu / cỡ, nút trang `<` `>` với bagPageSize = 5, kéo con trỏ trên màn thật → **chờ user — gộp cuối** (9 mục "User tự test"; test tự động chỉ gọi `LeftClickAt` / `RightClickAt` trực tiếp, chưa mô phỏng chuột thật).

## Vướng mắc

---
<!-- PLANNER ghi khi review. -->

## Review
**Kết luận: code ok, chờ user xem gộp cuối** (user 2026-10-24: "user xem" gộp cuối task; không chờ user) → **đã duyệt**.

Đối chiếu (review đầy đủ, diff `14ac2e7`):
- `InventoryView` đúng Quyết định 1–10: uGUI dựng bằng code, không EventSystem; `Update` đọc B / P / Esc / 1–0 (chỉ khi túi + kho đóng) / click trái / chuột phải qua `GameInput` (`Client/InventoryView.cs:306–324`); `OpenStock` mở kèm túi, `SetBagOpen(false)` đóng kho + bỏ chọn; `AutoCloseStock` theo `TargetReach` (`:327–341`); `Covers` gồm `consumedFrame` (`:352–363`); `HoldFromSlot` cầm → đóng túi, nông sản → `inv.not_holdable`; chọn kho lưu `ItemId`.
- Nối vào client đúng: `BlocksPointer` thêm ở `TerrainEditInput`, `PlayerCharacter`, `FollowCamera` (bỏ Alt); lướt chặn khi `AnyOpen`; `SettingsView.OtherPanelOpen` dùng `InventoryView.AnyOpen`; `SimView.OpenStockAt` → `InventoryView.OpenStock`; bảng túi / kho IMGUI, K, 1–4, Alt-lăn, Shift-tất cả đã bỏ.
- Test: 8 test mới đúng tên; test cũ sửa / xoá đúng danh sách bước 10. `DropPlayTests.StarterBagNearSpawnAndHud` sửa ngoài danh sách nhưng đúng quy tắc cuối bước 10 (dòng túi tạm trong HudText) và giữ ý nghĩa (kiểm 4 hạt trong ô túi) — chấp nhận. EditMode 542/542, PlayMode 233/233, console 0 lỗi [kết quả executor qua MCP]; Planner không chạy lại.
- Chi tiết tự chọn: nền panel `(0, 0, 0, 0.35)` để chặn con trỏ giữa các ô — chấp nhận, thêm vào danh sách xem cuối.

Ghi chú cho bước sau (không sửa ở brief này):
- Chữ hướng dẫn HUD `hud.help*` còn nói "B: túi (bấm vật phẩm để cầm)" và "1–4 / Alt + lăn chuột" — lỗi thời; **7l e** bỏ chữ hướng dẫn khỏi HUD và chuyển vào Cài đặt → Hướng dẫn: brief 7l e phải viết lại nội dung theo phím mới (B, P, 1–0, kéo thả, Tab, 1/2/3).
- Đường click chuột thật (`Update` → `LeftClickAt`) chỉ kiểm khi chơi thử; 0063 thêm `PointerDown/Move/Up` có test.
- Lướt bị chặn cả khi chỉ đang chọn ô túi phụ (túi đóng) vì `AnyOpen` gồm `HasSelection` — đúng chữ brief, đưa vào danh sách xem cuối.

Giờ thực tế: Executor 04:04–04:23 = **19 phút** (1 vòng).

Danh sách user xem gộp cuối (từ brief này): 9 mục "User tự test" + đề xuất Planner ở mục Kết quả (mở kho mở luôn túi; P; kho tự đóng > 10 ô; chọn ô túi phụ khi túi đóng) + nền panel 0,35 + lướt bị chặn khi đang chọn ô.
