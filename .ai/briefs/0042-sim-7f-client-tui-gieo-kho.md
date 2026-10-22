# Brief 0042 — Sim 7f (client B): bảng túi, chế độ gieo, kho ↔ túi

> Trạng thái: chờ review
> Review: đầy đủ + user xem
> Model thực thi: Sonnet · medium
> Cập nhật: 2026-10-22 19:20

<!-- Planner (Opus) viết, Executor (Sonnet) làm theo ở PHIÊN KHÁC — executor không có lịch sử chat, không được tự quyết.
Trạng thái: chờ làm → đang làm → chờ review → đã duyệt; hoặc → cần Opus (executor dừng, hỏi ở mục Vướng mắc).
Review: nhẹ (đọc mục Kết quả) | đầy đủ (đọc cả diff) | user xem (có phần user tự kiểm bằng mắt/chơi thử) — có thể ghép "đầy đủ + user xem".
Model thực thi: BẮT BUỘC, đúng dạng `<Model> · <effort>` — Model: Opus | Sonnet | Haiku | Fable; effort: low | medium | high | xhigh | max (chọn theo AGENTS.md §10.2).
Executor kiểm tra model + effort của phiên TRƯỚC khi làm (scripts/brief_check.py); lệch hoặc không xác định được → cảnh báo user và DỪNG.
File được commit. Tự kiểm trước khi giao (AGENTS.md §10.3): executor có phải hỏi gì không? còn chỗ nào bỏ ngỏ 2 phương án? -->

## Mục tiêu
Phần client B của [task contract sim 7f](../tasks/m1-sim-buoc-7f-tu-trong-trot.md) (đề xuất 4, 5, 8; user xác nhận 2026-10-22). Dùng API lõi của brief 0040 (đã duyệt) và `PlayerFarmActions` của brief 0041:

- **Bảng túi:** phím **B** (hoặc nút `Túi (B)`) mở/đóng; liệt kê khối, túi hạt, nông sản (ẩn dòng 0). Click túi hạt → chế độ gieo loại đó, bảng đóng.
- **Chế độ gieo:** chuột trái lên ô ruộng trong 8 ô → gieo ngay bằng hạt túi; chuột phải / Esc / Tab thoát; hết hạt tự thoát.
- **Kho ↔ túi:** bảng kho có nút chuyển hạt / nông sản hai chiều khi có kho trong 8 ô (mỗi click 1, Shift + click tất cả); click khối kho trong 8 ô mở bảng kho.
- Sửa nhỏ từ review 0041: chuột phải bỏ biển đúng ô đang tự xới → `Huỷ tự xới`.

Chỉ có ở scene có mô phỏng (SimView), như nút Kho và công cụ Xới. Tài liệu `design/` Planner cập nhật sau brief này.

Ước lượng ~2 ngày-người (trong 6–10 của 7f). Ghi giờ bắt đầu/kết thúc (theo `date`) vào mục Kết quả.

## Bối cảnh cần đọc
Đường dẫn tính từ `client/Assets/Game/`. Số dòng theo commit `29dcece`.

**API lõi (brief 0040):** `Core/Sim/Simulation.cs`
- `public PlayerSowResult CheckPlayerSow(int plotId, int cropId, PlayerBag bag)` (`:1001`), `public PlayerSowResult PlayerSow(int plotId, int cropId, PlayerBag bag)` (`:1017`) — `PlayerSowResult { Ok, NoPlot, NotTilled, HasCrop, OtherCrop, NoSeeds }` (`Core/Sim/SimData.cs:68`). Ô đứng yên đã xới → vụ mới, sang pha `Water`; ô pha `Sow` cùng loại → xong gieo; trừ 1 hạt túi.
- `public bool StorageInReach(float feetX, float feetY, float feetZ)` (`:1074`).
- `public int StoreToStock(PlayerBag bag, int cropId, bool produce, int amount)` (`:1083`) — chuyển `min(amount, số trong túi, Stock.Free)`; `public int TakeFromStock(PlayerBag bag, int cropId, bool produce, int amount)` (`:1105`) — `min(amount, số trong kho)`. Trả số đã chuyển. `amount` ≥ 0.
- `public bool EnqueueCrop(int plotId, int cropId)` (`:888`): ô đứng yên → bắt đầu vụ ngay (pha `Sow`, `CurrentCrop` = loại đầu hàng đợi).
- `State.Stock` (`Stockpile`: `Seeds[]`, `Produce[]`, `Capacity`, `Free`, `SeedsTotal`, `ProduceTotal` — `SimData.cs:208-226`), `State.Storages[i].Cell`.
- `Core/PlayerRules.cs:24`: `InHandReach(float feetX, float feetY, float feetZ, int x, int y, int z)` (8 ô, tâm thân tới tâm ô).
- `Core/PlayerBag.cs`: `Get(BlockMaterial)`, `GetSeeds(int)`, `GetProduce(int)`, `AddSeeds`, `AddProduce`, `Add(BlockMaterial, int)`.
- `Core/View/SiteStatus.cs:13` `CropName(cfg, id)` = `"Cây " + id`; `:16` `StockPanelText(Stockpile, SimConfig)` — dòng đầu `Kho chung · đã dùng X/Y`.
- `Core/DropField.cs:14` `ItemId.Block(BlockMaterial)`.

**Client:**
- `Client/PlayerFarmActions.cs` (brief 0041): `TillState`, `TillPlotId`, `StatusLine` (private set), `BeginSelfTill`, `CancelSelfTill()` (`:99`), `Harvest` (`:202`), `HudLines()` (`:214`), trường `view` (`TerrainChunkView`), thuộc tính riêng `Sim`.
- `Client/SimView.cs` (545 dòng):
  - `Update()` (`:110-126`): khối phím `kb` có `K` → `ToggleStock()` (`:124`), Esc đóng bảng ô ruộng (`:125`).
  - `Setup()`: tạo hộp kho `Box("Kho " + s.Id, …, solid: true)` (`:203-204`) — `Box(...)` trả `GameObject` (`:485-499`), có collider khi `solid`.
  - `StockOpen` (`:265`), `ToggleStock()` (`:338`), `StockButtonRect` (`:341`), `StockPanelRect` (`:342`), `PlotPanelRect` (`:343`), `BlocksPointer` (`:348`), `Covers(Vector2)` (`:350-357`).
  - `DrawFarmGui(float scale)` (`:359-…`): nút `Kho (K)` + bảng kho (`:362-368`), rồi bảng ô ruộng. `hudStyle`, `labelStyle` (tạo trong `OnGUI`).
- `Client/TerrainEditInput.cs` (425 dòng):
  - `maxDistance` (`:24`); `Farm` (`:38`), `Farming` (brief 0041).
  - `Update()` (`:74-155`): khối phím (`:79-87`, Tab `:85` → `ToggleTool()`); `var mouse = Mouse.current;` (`:89`) rồi chặn UI (`:91-95`); `var ray = …`, `var player = …` (`:111-112`); nhánh vật rơi (`:114-120`); `Pick` (`:121-125`); Mưa + thu hoạch (`:127-135`); `ShowAim(true, …)` (`:137`); chặn camera bay (`:149`); `UseTool` (`:151`); chuột phải bỏ biển (`:152-154`).
  - `ToggleTool()` (`:158-162`); `UseTool(Vector3Int, Vector3Int)` (`:204-236`), kiểm tầm `:207-208`; `ItemLabel(ItemId)` (`:286-292`); `HelpText()` (`:315-322`); `HudText()` (`:326-…`).
- Test mẫu: `Tests/Client/PlayerFarmPlayTests.cs` — `SetUp` (`:22-77`), `TearDown` (`:79-85`), `FeetCell`, `Candidates`, `MarkReachable`, `PlotToPhase` (`:87-165`).

**API Unity / Input System 1.19** (đã tra):
- `Keyboard.bKey`, `Keyboard.escapeKey` — `public KeyControl …Key { get; }`; `Keyboard.shiftKey` — `public ButtonControl shiftKey { get; protected set; }` (trái hoặc phải) [docs](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.19/api/UnityEngine.InputSystem.Keyboard.html).
- `ButtonControl.isPressed` — `public bool isPressed { get; }`; `wasPressedThisFrame` [docs](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.19/api/UnityEngine.InputSystem.Controls.ButtonControl.html).
- `Mouse.rightButton` [docs](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.19/api/UnityEngine.InputSystem.Mouse.html).
- `Component.GetComponentsInChildren<T>()` (chỉ trong test) [docs](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/Component.GetComponentsInChildren.html).
- Ngoài ra chỉ dùng API đã có trong file đang sửa (`GUI.Button`, `GUI.Label`, `GUI.Box`, `GUI.enabled`, `Physics.Raycast`, `Rect`).

## Quyết định đã chốt (không được đổi)
**Task contract 7f (user xác nhận 2026-10-22):**
1. Bảng túi: phím B mở/đóng; khối, túi hạt, nông sản, ẩn dòng 0; click túi hạt còn hạt → chế độ gieo loại đó, bảng đóng; con trỏ trên bảng không đào/đặt.
2. Chế độ gieo: HUD `Gieo Cây A (còn N) — chuột phải / Esc để thoát`; chuột trái không đào/đặt/xới; Tab cũng thoát. Gieo ngay (không đi tới, không thời gian), trong 8 ô. Ô đứng yên đã xới → vụ mới, hàng đợi giữ nguyên; ô chờ gieo cùng loại → gieo xong; ô chờ gieo khác loại / chưa xới / đã có cây → không gieo, HUD nêu lý do (vd `Ô đang chờ gieo Cây B`). Hết hạt → tự thoát.
3. Kho: giữ nút `Kho (K)`. Có kho trong 8 ô → nút chuyển mỗi loại cây: hạt `→ túi` / `→ kho`, nông sản `→ túi` / `→ kho`; mỗi click 1, Shift + click tất cả. Click khối kho trong 8 ô cũng mở bảng. Không có kho trong 8 ô → bảng chỉ xem, dòng `Lại gần kho (8 ô) để chuyển đồ`. Túi → kho dừng khi kho đầy.
4. Thu hoạch (brief 0041) vẫn ưu tiên trên mọi chế độ, kể cả chế độ gieo.

**Planner chốt khi viết brief:**

5. **Chế độ gieo ở `PlayerFarmActions`**; **bảng túi + nút chuyển kho ở `SimView`** (cùng chỗ với bảng kho, `Covers`). Không có `SimView`/mô phỏng (scene TerrainView) → không có bảng túi, không có chế độ gieo (HUD vẫn hiện số khối như cũ).
6. **Không có nhân vật** (camera bay, không `PlayerCharacter.Active`) → không kiểm tầm (như công cụ, brief 0041 quyết định 9): gieo và chuyển kho luôn được.
7. **Vào chế độ gieo = đổi công cụ** → huỷ tự xới đang chạy (`CancelSelfTill`).
8. **Esc trong chế độ gieo** chỉ thoát chế độ gieo; Esc cũng đóng bảng túi (như bảng ô ruộng). Không xử lý trùng với bản đồ / vòng thời điểm (chúng tự đóng bằng Esc như cũ).
9. **Vị trí:** nút `Túi (B)` góc dưới phải `new Rect(gw - 120f, gh - 46f, 110f, 36f)`; bảng túi ngay trên nó. Bảng kho đổi kích thước (bước 3).
10. Mọi dòng chữ ghi nguyên văn ở các bước; số ô in dạng `Vector3Int.ToString()`.

## Phạm vi
- Được sửa/tạo:
  - `Client/PlayerFarmActions.cs` — chế độ gieo (bước 1);
  - `Client/SimView.cs` — bảng túi, bảng kho có nút chuyển, ánh xạ collider kho (bước 2–3);
  - `Client/TerrainEditInput.cs` — chỉ các chỗ ở bước 4;
  - `Tests/Client/PlayerBagSowPlayTests.cs` (mới, + `.meta`);
  - mục Kết quả của brief này.
- Không được sửa: `Core/**`, scene `.unity`, `design/`, task contract, test khác (gồm `PlayerFarmPlayTests`), file khác.

## Các bước
1. **`Client/PlayerFarmActions.cs`** — chế độ gieo:
   ```csharp
   /// <summary>Loại cây đang gieo (chế độ gieo, brief 0042); -1 = không ở chế độ gieo.</summary>
   public int SowCrop { get; private set; } = -1;
   /// <summary>Vào chế độ gieo loại crop (túi còn hạt). Huỷ tự xới đang chạy. false nếu không có mô phỏng / túi hết hạt loại đó.</summary>
   public bool EnterSowMode(int crop)
   /// <summary>Đang ở chế độ gieo → thoát, StatusLine "Thoát chế độ gieo".</summary>
   public void ExitSowMode()
   /// <summary>Chuột trái ở chế độ gieo: gieo 1 hạt túi vào ô ruộng có Soil = cell. true khi đã gieo.</summary>
   public bool SowAt(Vector3Int cell)
   ```
   - `EnterSowMode(crop)`: `Sim == null` hoặc `view.Bag == null` → false; `view.Bag.GetSeeds(crop) <= 0` → `StatusLine = $"Gieo: túi hết hạt Cây {id}"`, false (`id` = `Sim.Config.crops[crop].id`). Được: `CancelSelfTill()`, `SowCrop = crop`, `StatusLine = ""`, true.
   - `ExitSowMode()`: `SowCrop < 0` → không làm gì; ngược lại `SowCrop = -1`, `StatusLine = "Thoát chế độ gieo"`.
   - `SowAt(cell)` (đang `SowCrop >= 0`, không thì false), `id` = tên loại đang gieo, theo thứ tự:
     1. có nhân vật và `!player.InToolReach(cell)` → `"Gieo: ngoài tầm 8 ô"`, false;
     2. `plotId = Sim.PlotAt(new CellPos(cell.x, cell.y, cell.z))`; `r = plotId < 0 ? PlayerSowResult.NoPlot : Sim.PlayerSow(plotId, SowCrop, view.Bag)`;
     3. `Ok` → `StatusLine = $"Gieo Cây {id} vào ô {cell}"`; nếu `view.Bag.GetSeeds(SowCrop) == 0` → `SowCrop = -1`, `StatusLine += " · hết hạt, thoát chế độ gieo"`; true.
     4. `NoPlot` → `"Gieo: chỉ gieo vào ô ruộng đã xới"`; `NotTilled` → `"Gieo: ô chưa xới xong"`; `HasCrop` → `"Gieo: ô đã có cây"`; `OtherCrop` → `$"Ô đang chờ gieo Cây {Sim.Config.crops[Sim.State.Plots[plotId].CurrentCrop].id}"`; `NoSeeds` → `$"Gieo: túi hết hạt Cây {id}"` và `SowCrop = -1`. Đều false, các trường hợp khác `NoSeeds` vẫn ở chế độ gieo.
   - `HudLines()`: ngay sau dòng Mưa (trước dòng tự xới), `SowCrop >= 0` → `$"\nGieo Cây {id} (còn {view.Bag.GetSeeds(SowCrop)}) — chuột phải / Esc để thoát"`.
   - Sửa summary lớp: thêm "chế độ gieo từ túi hạt (brief 0042)".

2. **`Client/SimView.cs`** — bảng túi:
   ```csharp
   /// <summary>Một dòng bảng túi (brief 0042). SeedCrop ≥ 0 = dòng túi hạt (click → chế độ gieo), -1 = chỉ xem.</summary>
   public struct BagRow { public string Label; public int SeedCrop; }
   public bool BagOpen { get; private set; }
   public void ToggleBag() => BagOpen = !BagOpen;
   /// <summary>Các dòng bảng túi: khối (theo giá trị BlockMaterial tăng dần), túi hạt, nông sản (theo loại cây); bỏ dòng 0.</summary>
   public static void BagRows(PlayerBag bag, SimConfig cfg, List<BagRow> rows)
   /// <summary>Click túi hạt crop: vào chế độ gieo, đóng bảng túi. false nếu không vào được.</summary>
   public bool ChooseSeedFromBag(int crop)
   public static Rect BagButtonRect(float gw, float gh) => new Rect(gw - 120f, gh - 46f, 110f, 36f);
   public static Rect BagPanelRect(float gw, float gh, int rows) { float h = 32f + 26f * Mathf.Max(rows, 1) + 24f; return new Rect(gw - 330f, gh - 56f - h, 320f, h); }
   ```
   - `BagRows`: `rows.Clear()`; `for (int m = 1; m < 256; m++)` có `bag.Get((BlockMaterial)m) > 0` → `Label = $"{TerrainEditInput.ItemLabel(ItemId.Block((BlockMaterial)m))} × {n}"`, `SeedCrop = -1`; rồi mỗi loại cây `i`: `GetSeeds(i) > 0` → `$"Túi hạt Cây {cfg.crops[i].id} × {n}"`, `SeedCrop = i`; rồi mỗi `i`: `GetProduce(i) > 0` → `$"Nông sản Cây {cfg.crops[i].id} × {n}"`, `-1`.
   - `ChooseSeedFromBag(crop)`: `farming = GetComponent<PlayerFarmActions>()`; null hoặc `!farming.EnterSowMode(crop)` → false; `BagOpen = false`, true.
   - Trường riêng `readonly List<BagRow> bagRows = new List<BagRow>();` dùng chung cho `Covers` và vẽ (gọi `BagRows(view.Bag, Sim.Config, bagRows)` trước khi dùng).
   - `Update`: khối `kb` thêm `if (kb.bKey.wasPressedThisFrame) ToggleBag();` và `if (BagOpen && kb.escapeKey.wasPressedThisFrame) BagOpen = false;`.
   - `Covers`: thêm `BagButtonRect(gw, gh)`; `BagOpen` → `BagPanelRect(gw, gh, bagRows.Count)` (tính `bagRows` trước).
   - `DrawFarmGui`, ngay sau phần bảng kho (trước `if (OpenPlotId < 0) return;`): nút `GUI.Button(BagButtonRect(gw, gh), "Túi (B)")` → `ToggleBag()`. `BagOpen`: `GUI.Box(r, "")`; tiêu đề `"Túi"` ở `new Rect(r.x + 8f, r.y + 4f, r.width - 16f, 24f)`; mỗi dòng `k` ở `new Rect(r.x + 8f, r.y + 32f + 26f * k, r.width - 16f, 24f)`: `SeedCrop >= 0` → `GUI.Button(rect, label)` → `ChooseSeedFromBag(SeedCrop)` (rồi `break` khỏi vòng); còn lại `GUI.Label(rect, label, hudStyle)`. Không có dòng → `GUI.Label(dòng 0, "Túi trống", hudStyle)`. Dòng cuối `new Rect(r.x + 8f, r.yMax - 26f, r.width - 16f, 24f)`: `"Click túi hạt để gieo · B / Esc: đóng"`.

3. **`Client/SimView.cs`** — kho ↔ túi:
   - Trường `readonly List<Collider> storageColliders = new List<Collider>();`; trong `Setup` (`:203-204`) lấy `GameObject` trả về của `Box("Kho " + …)` và thêm `GetComponent<Collider>()` vào danh sách (thứ tự = `State.Storages`).
   - Thêm:
     ```csharp
     /// <summary>Chỉ số kho có collider c (hộp kho); -1 nếu không phải.</summary>
     public int StorageIdOf(Collider c)
     /// <summary>Click khối kho index (brief 0042): trong 8 ô (hoặc không có nhân vật) → mở bảng kho, true.</summary>
     public bool OpenStockAt(int index)
     /// <summary>Có kho trong 8 ô (không có nhân vật → true).</summary>
     public bool CanTransferStock { get; }
     /// <summary>Dòng kết quả chuyển gần nhất; "" lúc đầu.</summary>
     public string StockMessage { get; private set; } = "";
     /// <summary>Nút chuyển: toBag = kho → túi, ngược lại túi → kho; all = Shift (tất cả), không thì 1. Trả số đã chuyển.</summary>
     public int TransferStock(int crop, bool produce, bool toBag, bool all)
     public static Rect StockPanelRect(float gw, float gh, int crops) { float h = 64f + 50f * crops; return new Rect(10f, gh - 56f - h, 480f, h); }
     ```
     (`StockPanelRect` thay bản cũ ở `:342`.)
   - `OpenStockAt(index)`: `player = PlayerCharacter.Active`; `player == null` hoặc `PlayerRules.InHandReach(f.x, f.y, f.z, cell.X, cell.Y, cell.Z)` (`f = player.FeetLocal`, `cell = Sim.State.Storages[index].Cell`) → `StockOpen = true`, true; không thì false.
   - `CanTransferStock`: `player == null || Sim.StorageInReach(f.x, f.y, f.z)`.
   - `TransferStock(...)`: `kind = produce ? "nông sản" : "hạt"`, `id = Sim.Config.crops[crop].id`, `amount = all ? int.MaxValue : 1`.
     1. `!CanTransferStock` → `StockMessage = "Lại gần kho (8 ô) để chuyển đồ"`, 0;
     2. `toBag`: `n = Sim.TakeFromStock(view.Bag, crop, produce, amount)`; `n == 0` → `$"Kho không có {kind} Cây {id}"`;
     3. ngược lại: `n = Sim.StoreToStock(...)`; `n == 0` → túi có 0 loại đó → `$"Túi không có {kind} Cây {id}"`, không thì `"Kho đầy"`;
     4. `n > 0` → `$"Đã chuyển {n} {kind} Cây {id} → {(toBag ? "túi" : "kho")}"`. Trả `n`.
   - `DrawFarmGui`, bảng kho (`:363-368`) thay bằng: `GUI.Box(r, "")`; dòng 0 `new Rect(r.x + 8f, r.y + 4f, r.width - 16f, 24f)` = dòng đầu của `SiteStatus.StockPanelText(Sim.State.Stock, Sim.Config).Split('\n')[0]`. Mỗi loại cây `i`, `y = r.y + 32f + 50f * i`:
     - nhãn `new Rect(r.x + 8f, y, r.width - 16f, 24f)`: `$"{SiteStatus.CropName(Sim.Config, i)} · kho: nông sản {Stock.Produce[i]} · hạt {Stock.Seeds[i]} · túi: nông sản {Bag.GetProduce(i)} · hạt {Bag.GetSeeds(i)}"`;
     - 4 nút ở `y + 24f`, cao 22, rộng 110, cách nhau 4, bắt đầu `r.x + 8f`, theo thứ tự `"Hạt → túi"`, `"Hạt → kho"`, `"Nông sản → túi"`, `"Nông sản → kho"`; `GUI.enabled = CanTransferStock`; click → `TransferStock(i, produce, toBag, Keyboard.current != null && Keyboard.current.shiftKey.isPressed)`; `GUI.enabled = true` sau 4 nút.
     - dòng cuối `new Rect(r.x + 8f, r.yMax - 28f, r.width - 16f, 24f)`: `!CanTransferStock` → `"Lại gần kho (8 ô) để chuyển đồ"`; không thì `StockMessage != "" ? StockMessage : "Mỗi click 1 · Shift + click: tất cả"`.
   - Thêm `using UnityEngine.InputSystem;` nếu chưa có; `using Game.Core;` cho `PlayerRules` nếu cần.

4. **`Client/TerrainEditInput.cs`**:
   - Khối phím (`:79-87`): Tab (`:85`) giữ `ToggleTool()`; thêm `if (kb.escapeKey.wasPressedThisFrame && Farming.SowCrop >= 0) Farming.ExitSowMode();`.
   - `ToggleTool()`: đầu hàm `if (Farming != null && Farming.SowCrop >= 0) { Farming.ExitSowMode(); return; }` (Tab ở chế độ gieo chỉ thoát, không đổi công cụ).
   - Ngay sau `var mouse = Mouse.current;` (`:89`), trước chặn UI: `if (mouse != null && Farming.SowCrop >= 0 && mouse.rightButton.wasPressedThisFrame) { Farming.ExitSowMode(); ShowAim(false, default, default); return; }`.
   - Sau nhánh vật rơi (`:114-120`), trước `Pick` (`:121`):
     ```csharp
     if (Farm != null && mouse.leftButton.wasPressedThisFrame && Physics.Raycast(ray, out var sh, maxDistance))
     {
         int sid = Farm.StorageIdOf(sh.collider);
         if (sid >= 0) { lastResult = Farm.OpenStockAt(sid) ? "Mở kho" : "Kho ngoài tầm 8 ô"; ShowAim(false, default, default); return; }
     }
     ```
   - Sau nhánh thu hoạch (`:135`), trước `ShowAim(true, …)` (`:137`): `if (Farming.SowCrop >= 0) { ShowAim(false, default, default); if ((player == null || player.FlyMode) && DebugFlyCamera.IsMoveKeyHeld(kb)) return; if (mouse.leftButton.wasPressedThisFrame) UseTool(digCell, placeCell); return; }`.
   - `UseTool`: đầu hàm (trước kiểm tầm `:207`): `if (Farming != null && Farming.SowCrop >= 0) { lastResult = ""; return Farming.SowAt(digCell) ? EditResult.Ok : EditResult.NotPlaceable; }` (thông báo ở `Farming.StatusLine`).
   - Chuột phải bỏ biển (`:152-154`) chuyển sang hàm mới, `Update` gọi `if (Tool == EditTool.Till && mouse.rightButton.wasPressedThisFrame) UnmarkAimed(digCell);`:
     ```csharp
     /// <summary>Chuột phải khi cầm Xới: bỏ biển ô cell (trong 8 ô); ô đang tự xới → huỷ tự xới. true khi đã bỏ.</summary>
     public bool UnmarkAimed(Vector3Int cell)
     ```
     Thân: `Farm == null` → false; có nhân vật mà `!InToolReach(cell)` → false; `id = Farm.Sim.PlotAt(new CellPos(cell.x, cell.y, cell.z))`; `!Farm.UnmarkAt(cell)` → false; `if (Farming.TillState != SelfTillState.None && Farming.TillPlotId == id) Farming.CancelSelfTill();`; `lastResult = $"Bỏ đánh dấu {cell}"`; true.
   - `HelpText()` dòng có nhân vật không bay: sau `"R: phép Mưa (trong 8 ô) · "` thêm `"B: túi · "`.
   - Sửa summary lớp: thêm "Sim 7f (brief 0042): chế độ gieo — chuột trái gieo, chuột phải / Esc / Tab thoát; click khối kho trong 8 ô mở bảng kho."

5. **`Tests/Client/PlayerBagSowPlayTests.cs`** (mới) — chép `SetUp`/`TearDown` và các hàm trợ giúp (`FeetCell`, `Candidates`, `MarkReachable`, `PlotToPhase`) từ `PlayerFarmPlayTests` (không sửa file đó). Các test ở "Phần cần test". Ô thử "cách > 8": cột cách chân ≥ 10 ô ngang, dùng `World.SurfaceHeight`. Đặt nhân vật gần / xa kho: `player.Place(view.transform.TransformPoint(new Vector3(x + 0.5f, SurfaceHeight(x, z) + 1f, z + 0.5f)), 0f)` rồi chờ `player.Ready` và 5 khung; gần = cột `(kho.X + 2, kho.Z)`, xa = cột cách mọi kho ≥ 12 ô ngang.

## Tiêu chí xong
- EditMode toàn bộ qua (không đổi Core, kỳ vọng như trước: 472/472).
- PlayMode toàn bộ qua (cũ + mới), 0 lỗi console khi chạy.
- `git diff --stat` chỉ có file trong Phạm vi.

## Test sau khi xong
### Phần cần test
**Mới — `PlayerBagSowPlayTests`:**
- **BagRows:** túi lúc đầu → rỗng; thêm `Add(Dirt, 3)`, `AddSeeds(0, 4)`, `AddProduce(1, 2)` → đúng 3 dòng theo thứ tự `"Đất × 3"` (-1), `"Túi hạt Cây A × 4"` (0), `"Nông sản Cây B × 2"` (-1); không có dòng Cây B hạt / Cây A nông sản.
- **Bảng túi chặn con trỏ:** `s = Mathf.Max(1f, Screen.height / 720f)`, `gw = Screen.width / s`, `gh = Screen.height / s`; điểm màn hình của tâm `c` một rect = `new Vector2(c.x * s, Screen.height - c.y * s)`. `ToggleBag()` → `BagOpen`; `sv.Covers(tâm BagPanelRect(gw, gh, số dòng))` → true; `ToggleBag()` lại → điểm đó false (nếu không trùng bảng khác), tâm `BagButtonRect` vẫn true.
- **Vào chế độ gieo:** có hạt Cây A → `ChooseSeedFromBag(0)` true, `Farming.SowCrop == 0`, `BagOpen == false`; Cây B 0 hạt → `ChooseSeedFromBag(1)` false, `StatusLine == "Gieo: túi hết hạt Cây B"`, `SowCrop` không đổi. Đang tự xới (`BeginSelfTill`) rồi `EnterSowMode(0)` → `TillState == None`.
- **Gieo ô đứng yên:** `PlotToPhase(Idle)`, túi 2 hạt Cây A, chế độ gieo, `input.UseTool(cell, cell)` → pha `Water`, `CurrentCrop == 0`, túi còn 1, `Slots.Count` như trước, `StatusLine == $"Gieo Cây A vào ô {cell}"`, vẫn `SowCrop == 0`.
- **Gieo ô chờ gieo cùng loại:** ô đứng yên + `EnqueueCrop(id, 0)` (pha `Sow`) → `SowAt` true, pha `Water`.
- **Từ chối:** ô `EnqueueCrop(id, 1)` (chờ gieo Cây B) → `StatusLine == "Ô đang chờ gieo Cây B"`; ô pha `Water` → `"Gieo: ô đã có cây"`; ô có biển chưa xới → `"Gieo: ô chưa xới xong"`; ô đất trống ngoài ô ruộng (trong tầm) → `"Gieo: chỉ gieo vào ô ruộng đã xới"`; ô ruộng ngoài tầm (tạo ô đứng yên gần bằng `PlotToPhase(Idle)`, rồi đặt nhân vật ở cột cách ô đó ≥ 10 ô ngang, chờ `Ready`) → `"Gieo: ngoài tầm 8 ô"`. Mỗi trường hợp: túi không đổi, vẫn `SowCrop == 0`.
- **Hết hạt tự thoát:** túi 1 hạt, gieo được → `SowCrop == -1`, `StatusLine` kết thúc bằng `" · hết hạt, thoát chế độ gieo"`.
- **Chế độ gieo không dùng công cụ:** Đào, chế độ gieo, `UseTool` lên ô cỏ mặt trong tầm (không phải ô ruộng) → vật liệu ô không đổi, `NotPlaceable`.
- **Thoát:** chế độ gieo, `input.ToggleTool()` → `SowCrop == -1`, `Tool` không đổi, `StatusLine == "Thoát chế độ gieo"`; `ExitSowMode()` khi không ở chế độ gieo → `StatusLine` không đổi.
- **HUD:** chế độ gieo, túi 3 hạt Cây A → `Farming.HudLines()` chứa `"Gieo Cây A (còn 3) — chuột phải / Esc để thoát"`.
- **Kho trong tầm:** nhân vật gần kho → `CanTransferStock`; túi 3 hạt Cây A, `TransferStock(0, false, false, false)` → 1, túi 2, kho +1, `StockMessage == "Đã chuyển 1 hạt Cây A → kho"`; `all = true` → 2, túi 0; `TransferStock(0, false, true, true)` → 3, túi 3, kho về như đầu; nông sản: `AddProduce(0, 2)`, `→ kho` all → 2; kho → túi khi kho 0 của loại đó → 0, `"Kho không có nông sản Cây B"` (Cây B); túi → kho khi túi 0 → `"Túi không có hạt Cây B"`.
- **Kho đầy:** đặt `Sim.State.Stock.Capacity` = đã dùng + `Reserved` + 1, túi 3 hạt, `→ kho` all → 1, lần nữa → 0, `StockMessage == "Kho đầy"`. (Trả `Capacity` như cũ cuối test.)
- **Kho ngoài tầm:** nhân vật xa kho → `CanTransferStock == false`, `TransferStock` → 0, `StockMessage == "Lại gần kho (8 ô) để chuyển đồ"`, túi/kho không đổi.
- **Click khối kho:** collider tên `"Kho 0"` (`go.GetComponentsInChildren<Collider>()`) → `StorageIdOf == 0`; collider địa hình → -1. Gần kho → `OpenStockAt(0)` true, `StockOpen`; xa → false, `StockOpen` không đổi (đóng trước bằng `ToggleStock` nếu đang mở).
- **UnmarkAimed (sửa review 0041):** Xới, ô có biển, `BeginSelfTill` (Walking) → `input.UnmarkAimed(cell)` true, ô `Removed`, `TillState == None`, `StatusLine == "Huỷ tự xới"`; ô có biển khác khi không tự xới → true, `StatusLine` không đổi.

**Cũ có thể bị ảnh hưởng:**
- `PlayerFarmPlayTests` (UseTool, ToggleTool, HudLines, chuột phải đổi chỗ), `SimFarmUiPlayTests` (bảng kho đổi kích thước, `Covers`), `TerrainEditPlayTests`, `DropPlayTests` (HUD, `UseTool`), `SimViewPlayTests`, `SimViewMapsPlayTests`, `MinimapViewPlayTests`, `MinimapCornerPlayTests` (chặn con trỏ) — đều qua không đổi kỳ vọng.
- EditMode — không đổi Core, chạy lại để chắc.

### Lệnh executor chạy
1. `date` (giờ bắt đầu) trước bước 1.
2. Unity Editor mở (MCP): `refresh_unity` → `read_console` không lỗi biên dịch.
3. `run_tests` PlayMode lọc `PlayerBagSowPlayTests` → sửa tới khi qua.
4. `run_tests` PlayMode toàn bộ → ghi dòng tổng; `run_tests` EditMode toàn bộ → ghi dòng tổng.
5. `git checkout -- client/ProjectSettings/EditorSettings.asset`.
   - Editor tắt: `python3 tools/unity_test.py PlayMode` rồi `python3 tools/unity_test.py EditMode`.
6. `date` (giờ kết thúc).

### User tự test
Trên Mac, scene SimView (Play), nhân vật (không bay):
1. Góc dưới phải có nút `Túi (B)`. Bấm B → bảng túi hiện khối / túi hạt / nông sản (không có dòng 0); con trỏ trên bảng không đào. B hoặc Esc đóng.
2. Nhặt túi hạt Cây A đầu game, xới một ô (tự xới), bấm B → click `Túi hạt Cây A × …` → bảng đóng, HUD `Gieo Cây A (còn N) — chuột phải / Esc để thoát`.
3. Chuột trái lên ô ruộng vừa xới → gieo ngay, N giảm 1, ô chuyển chờ tưới; bấm R lên ô đó → cây lớn. Chuột trái lên cỏ/đất → không đào, HUD nêu lý do.
4. Chuột phải (hoặc Esc, Tab) → thoát chế độ gieo; Tab lần sau mới đổi công cụ.
5. Gieo tới hết hạt → tự thoát.
6. Đi lại gần kho (khối xám), click khối kho → bảng kho mở, có 4 nút mỗi loại cây. Click `Hạt → kho` → túi −1, kho +1; Shift + click → chuyển tất cả. Thu hoạch rồi `Nông sản → kho`.
7. Đi xa kho (> 8 ô), mở bảng kho bằng K → nút mờ, dòng `Lại gần kho (8 ô) để chuyển đồ`. Click khối kho từ xa → HUD `Kho ngoài tầm 8 ô`.
8. Đang tự xới một ô, chuột phải lên biển ô đó → HUD `Huỷ tự xới`, biển mất.
9. Nhận xét bố cục: bảng túi / bảng kho có đè lên bản đồ góc, vòng thời điểm, HUD không; chữ có tràn nút không.

Đồng thời user xem 8 mục "User tự test" của brief 0041 nếu chưa xem.

## Dừng và chuyển lại Opus khi
- Brief mơ hồ hoặc mâu thuẫn với code thật (vd số dòng lệch không tìm được đoạn cần sửa).
- Một test cũ đổi kết quả — không tự sửa kỳ vọng test cũ.
- Túi lúc đầu không rỗng trong test (BagRows), hoặc `EnqueueCrop` lên ô đứng yên không sang pha `Sow`.
- Cần API Unity không có trong brief.
- Cần một quyết định thuộc AGENTS.md §3.4 hoặc ngoài "Quyết định đã chốt"; phải sửa file ngoài "Phạm vi".

---
<!-- Từ đây trở xuống: EXECUTOR ghi. -->

## Kết quả
Base commit: 84f1dbd
Chạy bằng: claude-sonnet-5-5 · medium
Bắt đầu: 2026-10-22 18:25 · Kết thúc: 2026-10-22 18:34 (theo `date`; ~0,15 giờ thực tế)
Commit: c90128c — sửa `Client/PlayerFarmActions.cs`, `Client/SimView.cs`, `Client/TerrainEditInput.cs` (thêm `using Game.Core.Sim;` cho `CellPos`); mới `Tests/Client/PlayerBagSowPlayTests.cs` (+ .meta).

## Kết quả test
- Biên dịch: `refresh_unity` + `read_console` 0 lỗi (sau 2 lần sửa: thiếu `using Game.Core.Sim`, test trùng hàm trợ giúp).
- `PlayerBagSowPlayTests` PlayMode: **15/15 đạt** — BagRows, bảng túi chặn con trỏ, vào chế độ gieo, gieo ô đứng yên / ô chờ gieo cùng loại, từ chối (chờ Cây B, đã có cây, chưa xới, ngoài ô ruộng, ngoài tầm), hết hạt tự thoát, không dùng công cụ, thoát (Tab/ExitSowMode), HUD, kho trong tầm, kho đầy, kho ngoài tầm, click khối kho, UnmarkAimed.
- PlayMode toàn bộ: **148/148 đạt** (run_tests MCP). EditMode toàn bộ: **472/472 đạt**. Console 0 lỗi.
- Dọn: `git checkout -- client/ProjectSettings/EditorSettings.asset` đã chạy.
- Phần user tự test (9 mục) + 8 mục 0041: **chờ user**. Click chuột thật (chuột phải/Esc thoát, click khối kho) chỉ kiểm qua hàm, chưa qua chuột thật.
- Test `StockTransferInReach` chuyển "kho → túi tất cả" kiểm kho về 0 (kho ban đầu có thể khác 0), hơi khác văn bản brief nhưng cùng ý.

## Vướng mắc

---
<!-- PLANNER ghi khi review. -->

## Review
Review đầy đủ (Planner, phiên Opus, 2026-10-22):
- `Chạy bằng` (claude-sonnet-5-5 · medium) khớp `Model thực thi` [repo].
- Phạm vi: `git show --stat c90128c` — 5 file, đúng Phạm vi; thêm `using Game.Core.Sim;` ở `TerrainEditInput` (cần cho `CellPos`, trong phạm vi file) [run].
- Phần cần test: 15 test mới phủ đủ các dòng (BagRows, chặn con trỏ, vào/thoát chế độ gieo, gieo ô đứng yên / cùng loại, 5 trường hợp từ chối, hết hạt, không dùng công cụ, HUD, kho trong tầm / đầy / ngoài tầm, click khối kho, UnmarkAimed); PlayMode 148/148, EditMode 472/472 theo executor — không chạy lại. Lệch nhỏ chấp nhận: "kho → túi tất cả" kiểm kho về 0 thay vì "về như đầu" (kho đầu có thể ≠ 0), cùng ý.
- Đối chiếu Quyết định đã chốt 1–10 với diff: `PlayerFarmActions` `EnterSowMode`/`ExitSowMode`/`SowAt` đúng thứ tự kiểm và chữ nguyên văn, HUD dòng gieo sau dòng Mưa; `SimView` `BagRows`, `ChooseSeedFromBag`, rect túi/kho, `Covers`, `StorageIdOf`, `OpenStockAt`, `CanTransferStock`, `TransferStock` + 4 nút + Shift; `TerrainEditInput` Esc / chuột phải / Tab thoát gieo, click khối kho trước `Pick`, nhánh gieo sau thu hoạch, `UseTool` chặn công cụ khi gieo, `UnmarkAimed` sửa lỗi review 0041, HelpText `B: túi`.
- Nhỏ, không chặn: Esc khi đang gieo và bảng ô ruộng/túi mở thì đóng cả hai (đúng quyết định 8); `Covers` dựng lại dòng túi mỗi lần gọi khi bảng mở (chấp nhận ở M1).
- Giờ thực tế: 9 phút (18:25–18:34) so với ước lượng ~2 ngày-người.
- User xem: chờ user chạy 9 mục "User tự test" (+ 8 mục của 0041) — chưa kết luận.
