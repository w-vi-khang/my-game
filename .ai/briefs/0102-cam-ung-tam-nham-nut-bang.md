# Brief 0102 — Cảm ứng (b): tâm nhắm, nút hành động chính, nút chế độ/Thu/Túi/Huỷ, chạm túi phụ và bảng túi

> Trạng thái: đã duyệt
> Review: đầy đủ
> Model thực thi: Sonnet · medium
> Cập nhật: 2026-11-03

<!-- Planner (Opus) viết, Executor (Sonnet) làm theo ở PHIÊN KHÁC — executor không có lịch sử chat, không được tự quyết.
Trạng thái: chờ làm → đang làm → chờ review → đã duyệt; hoặc → cần Opus (executor dừng, hỏi ở mục Vướng mắc). -->

## Mục tiêu
Phần (b) của task [m1-dieu-khien-cam-ung](../tasks/m1-dieu-khien-cam-ung.md). Brief 0101 (a) đã làm chế độ cảm ứng, joystick, vuốt/chụm camera, nút Nhảy. Brief này làm:
- **Tâm nhắm giữa màn hình:** ở chế độ cảm ứng, đích lấy theo tia qua tâm màn hình (bỏ qua collider nhân vật) thay cho con trỏ. Lớp đỏ / bản mờ / vòng tầm dùng lại như PC. Có chấm tâm nhắm.
- **Nút hành động chính:** làm đúng việc chuột phải với đích dưới tâm nhắm. Thêm: chế độ Mặc định, không cầm gì, đích là ô ruộng chưa chín → mở bảng ô ruộng.
- **Nút:** 4 chế độ (dọc), Thu, Túi, Huỷ/Menu (giống Esc).
- **Chạm trong bảng túi/kho/túi phụ:** chạm = click, kéo = kéo thả. Chạm ô túi phụ khi túi đóng = cầm vật (như phím 1–0).

**Chưa làm** (brief 0103): build APK, chạy trên điện thoại, tài liệu. Bảng ô ruộng / Cài đặt / Môi trường sống là IMGUI; trên điện thoại có thể không nhận chạm (xem Bối cảnh "IMGUI"). Brief này không đụng vào chúng.

## Bối cảnh cần đọc
- **Task contract:** `.ai/tasks/m1-dieu-khien-cam-ung.md`, gồm "Quyết định đã chốt" (đề xuất 3–5).
- **Brief 0101** (đã duyệt): `.ai/briefs/0101-cam-ung-joystick-camera-nhay.md`, mục "Quyết định đã chốt" 2–3 và 11.
- **TouchControls** (`client/Assets/Game/Client/TouchControls.cs`):
  - `:19` `[DefaultExecutionOrder(-50)]`.
  - `:22` `public const int JumpButtonId = 1;`. `:25` `WantsTouch`, `:26` `Active`.
  - `:81-88` `ClearOutputs()`.
  - `:90-123` `Update()`: F4; chưa `Active` thì ẩn; đọc `ETouch.activeTouches` → `TouchSample` (OverUi = 5 `BlocksPointer` + `SettingsView.IsAnyOpen`, `:118-119`); gọi `Feed`.
  - `:125-172` `Feed(...)`: bố cục nút Nhảy (`:131-138`), `Gestures.Update(input, screenWidth, dpi, rects)` (`:140`), áp lên nhân vật/camera, vẽ joystick, màu nút Nhảy.
  - `:174-198` `BuildCanvas()`; `:200-212` `NewDisc(name, parent, alpha)`.
- **Lõi cử chỉ** `client/Assets/Game/Core/TouchGestures.cs` (không sửa): `TouchSample(int id, float x, float y, TouchSamplePhase phase, bool overUi = false)`; `TouchRect(int id, float x, float y, float w, float h)` (góc dưới-trái, px, y lên), `Contains(px, py)`; `TouchGestures.DpToPx(dp, dpi)`; `IsHeld(id)`, `WasPressed(id)` (Began trong ô nút ở lần `Update` này). Chạm Began trong ô nút → thuộc nút; Began với `OverUi` → bị lõi bỏ qua (không joystick, không camera).
- **TerrainEditInput** (`client/Assets/Game/Client/TerrainEditInput.cs`):
  - `:71-84` `Awake()`. Chưa có `OnDestroy`.
  - `:86-142` `Update()`. Đoạn phải sửa là `:114-141`:
    - `:114-121`: `hasPointer`/`pointer` từ `GameInput`; con trỏ trên bảng thì `HideAim(); return;`.
    - `:126-131`: tia, `PickDrop`, `sid` (`Physics.Raycast` + `Farm.StorageIdOf`), `Pick`, `AimedHarvest`.
    - `:132-136`: `MoveClick` → `ViewPlotAt`.
    - `:137-138`: `Preview` + `ShowAim`.
    - `:141`: `UseTool` → `RightClickAt(hasCell, digCell, placeCell, AimedDrop, sid)`.
  - `:101` đang xem camera quan sát → `HideAim(); return`. `:104-110` phím chế độ chỉ khi `player != null && !player.FlyMode`. `:111` `Back` + đang cầm → `Control.ClearToDefault()`. `:112` `Collect` → `CollectNearby()`.
  - `:158` `public void ToggleMode(ControlMode m)`. `:256` `public RightClickAction RightClickAt(bool hasCell, Vector3Int digCell, Vector3Int placeCell, DropStack drop = null, int storageId = -1)` (tự `FlashReachRing` khi `OutOfReach`). `:357` `public bool ViewPlotAt(Vector3Int cell)`. `:417` `public bool Pick(Ray ray, out Vector3Int digCell, out Vector3Int placeCell)` (`:420` `Physics.Raycast`). `:428` `public DropStack PickDrop(Ray ray)` (`:432` `Physics.Raycast`). `:440` `public int CollectNearby()`.
  - Thuộc tính `Control`, `Walk`, `Observation`, `Inventory`, `HighlightVisible`, `HighlightCell`, `ReachRingVisible` (`:42-67`).
- **Luật chuột phải:** `client/Assets/Game/Core/ControlState.cs:52-73` `RightClick.Resolve`. Có đích mà ngoài 10 ô → `OutOfReach`. Mặc định + ô ruộng chưa chín + không vật rơi/kho/vật cầm → `None`. `ControlMode`: `Default = 0, Dig = 1, Till = 2, Rain = 3` (`:5`).
- **Nhân vật:** `PlayerCharacter.Controller` (`CharacterController`, là một `Collider`) — `client/Assets/Game/Client/PlayerCharacter.cs:52`. Các khối hình của nhân vật đã bỏ collider (`:244`). Biển ô ruộng cũng không có collider (`SimView.cs:613-614`).
- **InventoryView** (`client/Assets/Game/Client/InventoryView.cs`):
  - `:24` `public static InventoryView Active`. `:108` `ToggleBag()`. `:137` `CloseAll()`.
  - `:252` `HoldFromSlot(ItemContainer c, int slotOnPage)`. `:270` `UseHotbarKey(int k)` (chỉ khi túi + kho đóng).
  - `:322-328` `void CancelDrag()` (private).
  - `:331` `PointerDown(Vector2)`; `:359` `PointerMove(Vector2)`; `:373` `PointerUp(Vector2)` (không kéo → `LeftClickAt(điểm nhấn)` = chọn ô).
  - `:496-523` `Update()`. `:514-520` là nhánh chuột (`GameInput.HasPointer` + `MoveClick`/`UseTool`).
  - `:552` `Covers(Vector2)`. `:568` `SlotScreenCenter(ItemContainer c, int slotOnPage)`. Fields `pressActive`, `pressPos`, `hotbarGrid`, `Grid.Cells[i].Rt`, `ShownCount(c)`, `Visible(c)`, `Hit(rt, screen)` (`:79-86`, `:540-550`).
- **Chỗ đọc Esc** (8 chỗ; Esc gắn vào 5 action: `Player/Back`, `Panels/Close`, `Map/Close`, `TimeDial/Close`, `Settings/Close`):

  | File:dòng | Đoạn hiện có |
  |---|---|
  | `TerrainEditInput.cs:111` | `GameInput.Back.WasPressedThisFrame() && Control.HasHeld` |
  | `ObservationCamera.cs:70` | `HandleKeys(GameInput.ToggleObservation.WasPressedThisFrame(), GameInput.Back.WasPressedThisFrame())` |
  | `InventoryView.cs:502` | `GameInput.PanelsClose.WasPressedThisFrame() && AnyOpen` |
  | `SimView.cs:160` | `OpenPlotId >= 0 && GameInput.PanelsClose.WasPressedThisFrame()` |
  | `HabitatPanelView.cs:86` | `IsOpen && GameInput.PanelsClose.WasPressedThisFrame()` |
  | `MinimapView.cs:164` | `IsOpen && GameInput.MapClose.WasPressedThisFrame()` |
  | `TimeDial.cs:151` | `GameInput.TimeDialClose.WasPressedThisFrame()` |
  | `SettingsView.cs:135` | `HandleKeys(GameInput.ToggleSettings.WasPressedThisFrame(), GameInput.SettingsClose.WasPressedThisFrame())` |

  - `SettingsView` có `[DefaultExecutionOrder(-50)]` (`SettingsView.cs:9`). Nó chạy trước các bảng khác nên thấy trạng thái trước khi chúng đóng. `HandleKeys` (`:77-85`): Esc khi Cài đặt đang mở → đóng; không thì mở Cài đặt nếu `!OtherPanelOpen()` (`:69-75`, tính cả vật đang cầm).
- **Minimap + Phép đã chạm được** (không sửa): `MinimapView.cs:166` `HandleTouches(Touchscreen.current)`, `TimeDial.cs:160-168` đọc `Touchscreen.current`.
- **Vị trí UI có sẵn** (để tránh chồng nút):
  - Minimap: góc trên-phải, 200×200 cách mép 16 (canvas `CanvasScaler` mốc 1280×720), `MinimapView.cs:503`.
  - Nút Phép: dưới minimap, 72×72 ở (−16, −232) tính từ góc trên-phải, `TimeDial.cs:332`.
  - Avatar + máu/mana: IMGUI góc trên-trái, rộng tới ~426 đơn vị × `UiScale.Gui`, `TerrainEditInput.cs:549-555`. `UiScale.Gui` ở `UiScale.cs`.
- **Config:** `PlayerConfig` có `touchButtonDp` ở `client/Assets/Game/Core/Sim/SimConfig.cs:176`; kiểm khoảng mẫu `:445-446`. `PlayerMotion.TouchButtonDp` ở `client/Assets/Game/Core/PlayerMotion.cs:46`, gán ở `:79`. Hai `sim_m1.json` (dòng `"player"`, `design/data/sim_m1.json:39`) phải giống hệt (`ConfigTests`). Test config: `client/Assets/Game/Tests/Core/Sim/SimConfigTests.cs:126-140`.
- **Chuỗi:** `client/Assets/Game/Client/Resources/Lang/vi.json` (mảng `entries` `{ "key", "text" }` xếp theo key; đã có `touch.jump` và `controls.collect` = "Thu").
- **Mẫu test:**
  - `client/Assets/Game/Tests/Client/ControlModePlayTests.cs`: `SetUp` `:23-64` (SimView + TerrainEditInput + nhân vật đứng ở dải phẳng 8 ô theo +x), `Stand` `:123`, `Ahead(dx)` `:127`, `WaitWalk` `:129-137`, `DigWalksToTwoCellsThenDigs` `:198-209`, `OutOfReachRightClickFlashesRing` `:212-235`, `CollectTakesOnlyWithin2` `:453-461`, `LeftClickPlotOpensPanelWithUnmark` `:476-490`.
  - `client/Assets/Game/Tests/Client/InventoryPlayTests.cs`: `DragBagToHotbarMovesStack` `:276-285`, `Frames()` `:87-90`, `view.Bag.Hotbar.AddToSlot(0, item, n)` `:198`.
  - `client/Assets/Game/Tests/Client/TouchControlsPlayTests.cs`: dùng `touch.Feed(...)` với dpi 160.
  - `client/Assets/Game/Tests/Client/MinimapCornerPlayTests.cs:162-180`: `map.PressAt(map.ButtonScreenCenter)`.
  - `client/Assets/Game/Tests/Client/FlyCameraMoveKeyPlayTests.cs:13-23`: `InputSystem.AddDevice<Keyboard>()` + `QueueStateEvent` + `InputSystem.Update()`.
- **API (đã tra):**
  - `Physics.RaycastNonAlloc(Ray ray, RaycastHit[] results, float maxDistance)` → số hit; thứ tự không bảo đảm [docs](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/Physics.RaycastNonAlloc.html).
  - `Camera.ScreenPointToRay(Vector3 pos)` [docs](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/Camera.ScreenPointToRay.html).
  - Giả lập chạm: `InputSystem.QueueStateEvent(touchscreen, new TouchState { touchId = 1, phase = TouchPhase.Began, position = ... })`. `touchId` khác 0; `TouchState` ở `UnityEngine.InputSystem.LowLevel` [docs](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.19/manual/Events.html). `InputDevice.MakeCurrent()`, `InputSystem.AddDevice<Touchscreen>()`, `InputSystem.RemoveDevice(device)`.
  - `TouchSimulation.Enable()` tắt thiết bị Mouse (`InputSystem.DisableDevice(pointer, keepSendingEvents: true)`): `client/Library/PackageCache/com.unity.inputsystem@21a28c3a6c83/InputSystem/Plugins/EnhancedTouch/TouchSimulation.cs:89`.
  - **IMGUI:** Input System không cấp input cho IMGUI lúc chạy. Khi Active Input Handling = "Input System Package" thì `OnGUI` của game không nhận sự kiện input [docs](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.19/manual/UISupport.html#immediate-mode-gui). Project đang đặt 1 (`client/ProjectSettings/ProjectSettings.asset:926` `activeInputHandler: 1`). Brief này **không** đổi; brief 0103 kiểm trên điện thoại.

## Quyết định đã chốt (không được đổi)
1. **Không thêm binding `<Touchscreen>` vào `GameControls.inputactions`.** TouchControls đưa chạm thẳng tới chỗ cần (nút, bảng túi).
   - Lý do: `primaryTouch` thường là ngón joystick. Nếu gắn vào `Point`/`MoveClick`, mọi chạm thế giới thành click PC (đi-theo-click, mở bảng ô ruộng, bấm bản đồ), phải chặn ở nhiều chỗ.
   - Minimap và nút Phép đã đọc `Touchscreen` sẵn.
2. **Tâm nhắm = tâm màn hình** `(Screen.width / 2, Screen.height / 2)`, chỉ khi `TouchControls.Active`. PC giữ nguyên con trỏ.
3. **Tia tâm nhắm bỏ qua `PlayerCharacter.Active.Controller`.** Chỉ ở chế độ cảm ứng. PC giữ `Physics.Raycast` như cũ: chuột trên người nhân vật vẫn không có ô.
4. **Nút hành động chính** = `RightClickAt` với đích dưới tâm nhắm. Ngoài ra: kết quả `None` + `hasCell` + không vật rơi + không kho (`sid < 0`) + `Control.Mode == Default` + `!Control.HasHeld` → `ViewPlotAt(digCell)`.
   - Ô ruộng chín: `Resolve` đã trả `Harvest`.
   - Ngoài 10 ô: `Resolve` trả `OutOfReach`, nên không mở bảng.
5. **Nút gọi thẳng hàm có sẵn** ngay trong `Feed`, cùng điều kiện như phím PC:
   - Chế độ: `ToggleMode`, chỉ khi có nhân vật và `!FlyMode`.
   - Thu: `CollectNearby`, không khi đang xem camera quan sát.
   - Túi: `InventoryView.Active.ToggleBag()`.
6. **Huỷ/Menu = Esc**, qua cờ một khung `GameInput.TouchBack`. 8 chỗ đọc Esc OR thêm cờ này. Hành vi trùng PC: đóng mọi bảng đang mở + thôi cầm + thoát xem camera quan sát; không còn gì → mở Cài đặt; Cài đặt đang mở → đóng.
   - Thời điểm: `Feed` chỉ ghi "đã bấm". `TouchControls.Update` đổi thành `TouchBackFrame = Time.frameCount` **sau** khi gọi `Feed`, và cả khi `readTouches == false`. Nhờ vậy test gọi `Feed` từ coroutine thì khung kế mới có hiệu lực, đúng một khung.
   - `TouchControls` đổi sang `[DefaultExecutionOrder(-60)]` để chạy trước `SettingsView` (−50).
7. **Chạm trong bảng túi/kho/túi phụ:** một "ngón UI" mỗi lúc.
   - Began có `OverUi`, không trong ô nút, và `InventoryView.Active.PointerDown(pos)` trả true → ngón đó là ngón UI.
   - Moved/Stationary → `PointerMove`. Ended hoặc biến mất → `TouchUp(vị trí cuối)`.
   - `TouchUp`: nhả không kéo, điểm nhấn trên ô túi phụ, túi + kho đóng → cầm ô đó (`HoldFromSlot(Hotbar, i)`). Còn lại như `PointerUp`.
   - Cầm vật từ túi chính bằng cảm ứng: kéo sang túi phụ rồi chạm (giới hạn M1, không thêm cử chỉ).
8. **InventoryView bỏ nhánh chuột khi `TouchControls.Active`** để không xử lý hai lần trong Editor. Trong Editor, `TouchSimulation` cũng đã tắt Mouse.
9. **Số tạm mới** trong `sim_m1.json` → `player` (cả hai file), ngay sau `"touchButtonDp"`:

   | Trường | Giá trị | Khoảng hợp lệ |
   |---|---|---|
   | `touchActionButtonDp` | 96 | [48, 160] |
   | `touchModeButtonDp` | 52 | [48, 120] |
   | `touchCrosshairDp` | 10 | [4, 32] |

10. **Id nút:** `JumpButtonId = 1` (giữ), `ActionButtonId = 2`, `CollectButtonId = 3`, `ModeButtonId0 = 4` (id = 4 + (int)ControlMode: Default 4, Dig 5, Till 6, Rain 7), `BagButtonId = 8`, `BackButtonId = 9`.
11. **Bố cục (px).** Ký hiệu:
    - `d` = DpToPx(`TouchButtonDp`), `a` = DpToPx(`TouchActionButtonDp`), `m` = DpToPx(`TouchModeButtonDp`), `s = Screen.safeArea`.
    - Mọi nút là hình tròn. Ô chạm là hình vuông bao đường tròn.

    | Nút | Đường kính | Tâm |
    |---|---|---|
    | Hành động chính (2) | a | `ax = s.xMax − 0.15d − a/2`, `ay = s.yMin + 0.15d + a/2` |
    | Thu (3) | d | `(ax, ay + a/2 + 0.2d + d/2)` |
    | Nhảy (1) | d | `jx = ax − a/2 − 0.9d`, `jy = s.yMin + d` |
    | Chế độ, i = 0..3 từ dưới lên: Tưới (7), Xới (6), Đào (5), Mặc định (4) | m | `(jx, jy + d/2 + 0.2d + m/2 + i·1.1m)` |
    | Túi (8) | d | `bx = Mathf.Max(s.xMin + 0.15d + d/2, 440f·UiScale.Gui + 0.15d + d/2)`, `by = s.yMax − 0.15d − d/2` |
    | Huỷ (9) | d | `(bx + 1.15d, by)` |
    | Chấm tâm nhắm (không phải nút) | c = DpToPx(`TouchCrosshairDp`) | `(Screen.width/2, Screen.height/2)` |

    - Lý do: thanh chế độ dọc nằm cột trong, cạnh cột minimap + nút Phép ở mép phải. Mép phải không đủ cao cho 4 nút ≥ 48 dp bên dưới nút Phép. Thu nằm trên nút chính, thấp hơn nút Phép. Túi + Huỷ nằm cạnh phải dải máu/mana.
12. **Hình:**
    - Mọi nút: đĩa trắng, chữ ở giữa (font `LegacyRuntime.ttf`, cỡ ≈ 0,3 × đường kính; nút chế độ ≈ 0,22·m, `horizontalOverflow = Overflow`).
    - Alpha 0,35; khi đang giữ → 0,6. Nút chế độ đang bật (`Control.Mode`) → 0,6.
    - Chấm tâm nhắm: đĩa trắng alpha 0,7. Hiện khi `Active` và không có bảng mở: không `InventoryView.AnyOpen`, `SimView.AnyPanelOpen`, `MinimapView.IsAnyOpen`, `SettingsView.IsAnyOpen`, `TimeDial.IsAnyOpen`, `HabitatPanelView.IsAnyOpen`, và không đang xem camera quan sát.
13. **Chữ trên nút** (thêm vào `vi.json`, đúng thứ tự key):

    | Key | Text |
    |---|---|
    | `touch.action` | Làm |
    | `touch.back` | Huỷ |
    | `touch.bag` | Túi |
    | `touch.mode_default` | Mặc định |
    | `touch.mode_dig` | Đào |
    | `touch.mode_rain` | Tưới |
    | `touch.mode_till` | Xới |

    - Nút Thu dùng key có sẵn `controls.collect`.

## Phạm vi
- **Được sửa/tạo:**
  - `client/Assets/Game/Core/PlayerMotion.cs`, `client/Assets/Game/Core/Sim/SimConfig.cs`
  - `design/data/sim_m1.json`, `client/Assets/Game/Client/Resources/Config/sim_m1.json` (chỉ thêm 3 trường ở quyết định 9)
  - `client/Assets/Game/Client/TouchControls.cs`, `GameInput.cs`, `TerrainEditInput.cs`, `InventoryView.cs`
  - Chỉ đổi dòng đọc Esc trong bảng ở mục Bối cảnh: `ObservationCamera.cs`, `SimView.cs`, `HabitatPanelView.cs`, `MinimapView.cs`, `TimeDial.cs`, `SettingsView.cs`
  - `client/Assets/Game/Client/Resources/Lang/vi.json` (7 key mới)
  - Test mới:
    - `client/Assets/Game/Tests/Client/TouchActionPlayTests.cs` (mới)
    - Một test method mới trong `MinimapCornerPlayTests.cs`
    - Hai test method mới trong `SimConfigTests.cs`
  - Các file `.meta` Unity tự tạo
- **Không được sửa:**
  - `GameControls.inputactions`, `TouchGestures.cs`, `PlayerCharacter.cs`, `FollowCamera.cs`
  - `client/ProjectSettings/` (gồm `activeInputHandler`)
  - Phần IMGUI của `SimView`/`SettingsView`/`HabitatPanelView`
  - `loc/`, `design/` (trừ `design/data/sim_m1.json`)
  - Test hiện có (không sửa thân test hay `[TestCase]` cũ). Test cũ fail → dừng, ghi "cần Opus".

## Các bước
1. **Config:**
   - `SimConfig.PlayerConfig`, sau `touchButtonDp`: `public float touchActionButtonDp = 96f; public float touchModeButtonDp = 52f; public float touchCrosshairDp = 10f;`.
   - 3 kiểm khoảng theo mẫu `:445-446`, thông báo `"player.<tên> must be in [a, b]."` (khoảng ở quyết định 9).
   - Thêm 3 trường vào cả hai `sim_m1.json`, ngay sau `"touchButtonDp": 64`.
   - `PlayerMotion`: `TouchActionButtonDp` (96), `TouchModeButtonDp` (52), `TouchCrosshairDp` (10), mẫu `:46` kèm `/// <summary>` "(task cảm ứng (b), số tạm)". Gán trong `Configure`.
   - **Xong khi:** biên dịch được; `ConfigTests` đạt.
2. **GameInput** — thêm:
   ```csharp
   /// <summary>Khung nút Huỷ/Menu cảm ứng có hiệu lực (TouchControls ghi; -1 = chưa). Task cảm ứng (b), brief 0102.</summary>
   public static int TouchBackFrame { get; set; } = -1;
   /// <summary>Nút Huỷ/Menu cảm ứng ở khung này — mọi chỗ đọc Esc OR thêm cờ này.</summary>
   public static bool TouchBack => TouchBackFrame == Time.frameCount;
   ```
3. **8 chỗ đọc Esc** (bảng ở Bối cảnh): thay `X.WasPressedThisFrame()` của action Esc bằng `(X.WasPressedThisFrame() || GameInput.TouchBack)`. Ví dụ `SettingsView.cs:135` → `HandleKeys(GameInput.ToggleSettings.WasPressedThisFrame(), GameInput.SettingsClose.WasPressedThisFrame() || GameInput.TouchBack);`. Không đổi gì khác trong các file đó.
4. **TerrainEditInput:**
   1. Thêm `public static TerrainEditInput Active { get; private set; }`. Gán `Active = this` đầu `Awake`. Thêm `void OnDestroy() { if (Active == this) Active = null; }`.
   2. Thêm `public static Vector2 AimScreenPoint => new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);` (summary: tâm nhắm cảm ứng).
   3. Thêm `static bool PointerBlocked(Vector2 p)` = biểu thức 5 `BlocksPointer` ở `:117`. Dòng `:117` gọi hàm này (`hasPointer && PointerBlocked(pointer)`).
   4. **Tia bỏ qua nhân vật:**
      - Field `Collider aimIgnore;` và `static readonly RaycastHit[] hitBuf = new RaycastHit[16];`.
      - Hàm `bool Raycast(Ray ray, out RaycastHit hit)`:
        - `aimIgnore == null` → `return Physics.Raycast(ray, out hit, maxDistance);`.
        - Ngược lại `n = Physics.RaycastNonAlloc(ray, hitBuf, maxDistance)`, chọn hit có `distance` nhỏ nhất mà `collider != aimIgnore`. Không có → false.
      - Thay `Physics.Raycast(ray, out …, maxDistance)` ở `Pick` (`:420`), `PickDrop` (`:432`) và dòng `sid` (`:129`) bằng `Raycast(ray, out …)`.
   5. Thêm `void AimTargets(Ray ray, out DropStack drop, out int sid, out bool hasCell, out Vector3Int digCell, out Vector3Int placeCell)`: đúng thứ tự `:127-130` (drop, sid, Pick).
   6. **Sửa `Update` `:114-141`:**
      - `bool touch = TouchControls.Active;`
      - `bool hasPointer = touch || GameInput.HasPointer;`
      - `var pointer = touch ? AimScreenPoint : GameInput.PointerPosition;`
      - Giữ đoạn chặn bảng và `cam == null` như cũ.
      - `aimIgnore = touch && player != null ? player.Controller : null;` → `AimTargets(...)` (gán `AimedDrop = drop`) → `aimIgnore = null;`.
      - `AimedHarvest` như cũ.
      - Khối `MoveClick` (`:132-136`) chỉ chạy khi `!touch`.
      - `Preview` + `ShowAim` như cũ.
      - Dòng `UseTool` (`:141`) chỉ khi `!touch`.
   7. **Thêm hàm nút hành động chính:**
      ```csharp
      /// <summary>Nút hành động chính (task cảm ứng (b), brief 0102): đích dưới tâm nhắm (tia bỏ qua nhân vật) → RightClickAt; Mặc định, không cầm, trúng ô (không vật rơi / kho) mà ra None → ViewPlotAt (ô ruộng chưa chín → bảng ô ruộng). Đang xem camera quan sát, không camera / world, hoặc tâm nhắm trên bảng → None.</summary>
      public RightClickAction TouchAction()
      ```
      - Thân hàm:
        - `Observation != null && Observation.Engaged` → `None`.
        - `cam = Camera.main`; `cam == null || view.Editor == null` → `None`.
        - `PointerBlocked(AimScreenPoint)` → `None`.
        - `ray = cam.ScreenPointToRay(AimScreenPoint)`; `aimIgnore` = collider nhân vật (nếu có); `AimTargets`; `aimIgnore = null`.
        - `a = RightClickAt(hasCell, digCell, placeCell, drop, sid)`.
        - Quyết định 4 → `ViewPlotAt(digCell)`.
        - `return a`.
      - **Xong khi:** test "Tâm nhắm / nút chính" ở Phần cần test đạt.
   8. Cập nhật `<summary>` lớp: thêm 1 dòng "Cảm ứng (brief 0102): tâm nhắm = tâm màn hình, tia bỏ qua nhân vật; TouchAction cho nút hành động chính."
5. **InventoryView:**
   - Đổi `void CancelDrag()` thành `public void CancelDrag()`, summary "Huỷ nhấn/kéo đang dở (không chuyển, không chọn)".
   - Thêm `public bool HotbarSlotAt(Vector2 screen, out int slotOnPage)`: túi phụ đang hiện, trúng `hotbarGrid.Cells[i].Rt` với i < `ShownCount(Hotbar)` → true.
   - Thêm hàm chạm:
     ```csharp
     /// <summary>Nhả ngón chạm (task cảm ứng (b)): không kéo, điểm nhấn trên ô túi phụ, túi + kho đóng → cầm ô đó (như phím 1–0); còn lại như PointerUp. true nếu đã xử lý.</summary>
     public bool TouchUp(Vector2 screen)
     ```
     - `pressActive && !Dragging && !BagOpen && !StockOpen && HotbarSlotAt(pressPos, out i)` → `CancelDrag(); HoldFromSlot(ItemContainer.Hotbar, i); return true;`.
     - Còn lại → `return PointerUp(screen);`.
   - `Update` `:514`: `if (GameInput.HasPointer)` → `if (GameInput.HasPointer && !TouchControls.Active)`.
6. **TouchControls:**
   1. `[DefaultExecutionOrder(-60)]`. Thêm const id theo quyết định 10.
   2. **Quản lý nút:**
      - Gộp thành danh sách nút (lớp lồng nhỏ: `Id`, `RectTransform Rt`, `Image Img`, `Text Label`, `string Key`, đường kính).
      - `JumpButton` vẫn trả `RectTransform` của nút Nhảy.
      - Thêm `public Rect ButtonRect(int id)`: ô vuông px của lần bố cục gần nhất; id lạ → `Rect.zero`.
      - Thêm `public RectTransform Crosshair { get; private set; }`.
   3. **`BuildCanvas`:** dựng 9 nút + chấm tâm nhắm bằng `NewDisc` và chữ như nút Nhảy hiện có. Chữ lấy `GameSettings.Text.Get(key)` theo quyết định 13.
   4. **`Feed` — bố cục:** thay `:131-138` bằng bố cục quyết định 11. Gán `anchoredPosition`/`sizeDelta`/cỡ chữ từng nút. `rects` gồm cả 9 ô.
   5. **`Feed` — sau `Gestures.Update`:**
      1. Ngón UI (quyết định 7), làm trong hàm riêng `RouteUiTouch(input)`. Field `int uiTouchId = -1; Vector2 uiLast;`. Ô nút loại trừ bằng `rects[k].Contains`.
      2. Nút (quyết định 5–6), `edit = TerrainEditInput.Active`, `player = PlayerCharacter.Active`:
         - `WasPressed(ActionButtonId)` → `edit?.TouchAction()`.
         - `WasPressed(CollectButtonId)` → nếu `edit != null && !(edit.Observation != null && edit.Observation.Engaged)` → `edit.CollectNearby()`.
         - 4 id chế độ → nếu `edit != null && player != null && !player.FlyMode` → `edit.ToggleMode((ControlMode)(id − ModeButtonId0))`.
         - `WasPressed(BagButtonId)` → `InventoryView.Active?.ToggleBag()`.
         - `WasPressed(BackButtonId)` → `backRequested = true`.
      3. Phần nhân vật/camera/joystick như cũ.
      4. Màu: mọi nút theo quyết định 12. Chấm tâm nhắm hiện/ẩn theo quyết định 12, cỡ c, ở tâm màn hình.
   6. **`Update`:**
      - Nhánh `!Active` (`:93-98`): thêm `backRequested = false;` trước `return`.
      - Thay `if (!readTouches) return;` (`:100`) bằng khối `if (readTouches) { …dựng samples… Feed(...); }`.
      - Ngay sau khối đó (chạy cả khi `readTouches == false`): `if (backRequested) { backRequested = false; GameInput.TouchBackFrame = Time.frameCount; }`.
   7. **`ClearOutputs`:** thêm `backRequested = false`. Nếu `uiTouchId >= 0` → `InventoryView.Active?.CancelDrag(); uiTouchId = -1;`.
   8. Cập nhật `<summary>` lớp: thêm (b) brief 0102 — nút chính / chế độ / Thu / Túi / Huỷ, ngón UI cho bảng túi, thứ tự thực thi −60 (chạy trước SettingsView để cờ Huỷ có hiệu lực cùng khung).
7. **Chuỗi** — thêm 7 key ở quyết định 13 vào `vi.json`, đúng thứ tự key.
8. **Test** — viết các test ở "Phần cần test".

## Tiêu chí xong
- Biên dịch sạch, console không lỗi.
- Toàn bộ EditMode và PlayMode đạt, gồm test mới. Số test tăng đúng bằng số test thêm.
- Với `ForceOn = false` (Editor, không phải mobile): các test PC cũ đạt nguyên, đặc biệt `ControlModePlayTests`, `InventoryPlayTests`, `TerrainEditPlayTests`, `SettingsPlayTests`, `ObservationCameraPlayTests`, `MinimapCornerPlayTests`, `TimeDialPlayTests`, `TouchControlsPlayTests`, `DropPlayTests`, `ClickMovePlayTests`.
- Hai `sim_m1.json` giống hệt nhau.
- `git diff --stat` chỉ gồm file trong Phạm vi; `GameControls.inputactions` và `ProjectSettings` không đổi.

## Test sau khi xong

### Phần cần test
**EditMode** — `SimConfigTests`, 2 method mới (theo mẫu `:126-140`):
- `TouchButtonsBDefaults`: 3 trường mới đọc đúng 96 / 52 / 10 từ `design/data/sim_m1.json`.
- `TouchButtonsBOutOfRange`: mỗi trường ra ngoài khoảng (dưới và trên, 6 `[TestCase]`) → `ArgumentException`, thông báo chứa khoảng.

**PlayMode** — `TouchActionPlayTests` (file mới):
- **SetUp:** chép `ControlModePlayTests.SetUp` (`:23-64`) và các helper `Stand`, `Ahead`, `WaitWalk`, `Wait`; thêm `inv = input.Inventory` và `Frames()` như `InventoryPlayTests`. Sau đó:
  - `touch = go.GetComponent<TouchControls>()` (SimView đã thêm), `touch.readTouches = false`, `TouchControls.SetForceOn(true)`.
  - `follow = camGo.GetComponent<FollowCamera>(); follow.enabled = false;`.
  - Gọi `Feed()` rỗng một lần (dpi 160) để dựng bố cục, rồi `yield return null`.
- **TearDown:** `TouchControls.SetForceOn(false)` trước, rồi dọn như mẫu.
- **Helper:**
  - `Surface(x, z)` = ô `(x, view.World.SurfaceHeight(x, z), z)`.
  - `AimAt(cell, offset)`: `top = view.transform.TransformPoint(cell.x + 0.5f, cell.y + 1f, cell.z + 0.5f)`; `camGo.transform.position = top + offset; camGo.transform.LookAt(top);`. Offset mặc định `(0, 5, −1.5)`.
  - `Tap(id)`: lấy tâm `touch.ButtonRect(id).center`; Feed Began (id ngón 9) → `yield return null` → Feed Ended → `yield return null`.

| # | Thao tác | Mong đợi |
|---|---|---|
| 1 | `Tap(5)` (Đào). `AimAt(Ahead(2))`, `yield return null` | `HighlightVisible`, `HighlightCell == Ahead(2)` |
| 1b | Tiếp test 1: Feed Began trên nút 2 (chưa yield) | Ngay sau Feed: `input.Walk.State == Done`, ô `Ahead(2)` thành `Empty` (đào ngay). Rồi Feed Ended |
| 2 | `Tap(5)`, `AimAt(Ahead(6))`, `yield return null`, `Tap(2)` | Ngay sau `Tap`: `Walk.State == Walking`, ô chưa đổi; sau `WaitWalk()` → `Done`, ô thành `Empty` |
| 3 | `Tap(5)`, `c = Surface(spot.x + 12, spot.z)`, `AimAt(c)`, `yield return null`, gọi `input.TouchAction()` | Trả `OutOfReach`; `input.ReachRingVisible`; vật liệu ô `c` không đổi; `player.ClickMove == ClickMoveState.None` |
| 4 | Tia xuyên nhân vật: `Tap(5)`; `camGo.transform.position = player.transform.position + new Vector3(−2f, 1.2f, 0f)`, `LookAt` mặt trên `Ahead(3)`, `yield return null` | `HighlightCell == Ahead(3)` (cảm ứng bỏ qua nhân vật). Rồi gọi thẳng `input.Pick(Camera.main.ScreenPointToRay(TerrainEditInput.AimScreenPoint), out _, out _)` → false (hàm public dùng `Physics.Raycast` như PC vì `aimIgnore` đã về null) |
| 5 | Mặc định, không cầm. `cell = Ahead(2)`, `sv.UseTill(cell) == Marked` (chưa xới, chưa chín). `AimAt(cell)`, bấm nút chính | Trong ≤ 5 s: `sv.OpenPlotId == id` của ô đó |
| 6 | Nút chế độ: Tap 5 → Tap 5 → Tap 7 → Tap 4 | `Control.Mode`: Dig → Default → Rain → Default |
| 7 | Nút Thu: vật rơi ở `(spot.x + 1, spot.y + 1, spot.z)` và `(spot.x + 3, …)` như `CollectTakesOnlyWithin2`; Tap 3 | Chồng gần bị nhặt, chồng xa còn |
| 8 | Nút Túi: Tap 8, Tap 8 | `inv.BagOpen` true rồi false |
| 9 | Huỷ: `inv.ToggleBag()`; Tap 9 | Bag đóng; `SettingsView.IsAnyOpen` false |
| 10 | Huỷ khi cầm: `view.Bag.Add(Dirt, 2)`, `input.HoldItem(Dirt)`; Tap 9 | `!input.Control.HasHeld`; `SettingsView.IsAnyOpen` false |
| 11 | Huỷ khi không còn gì: Tap 9 → Tap 9 | Cài đặt mở rồi đóng |
| 12 | Cờ Huỷ chỉ một khung: Feed Began trên nút 9, rồi `yield return null` 3 lần, **không** Feed thêm | Cài đặt mở sau khung đầu và vẫn mở sau 3 khung (không bật/tắt lặp) |
| 13 | Chạm ô túi phụ khi túi đóng: `view.Bag.Hotbar.AddToSlot(0, Dirt, 2)`, `yield return Frames`; Feed Began ở `inv.SlotScreenCenter(Hotbar, 0)` với `overUi: true` → khung → Ended cùng chỗ | `input.Control.HasHeld`, `Held == Dirt`; `touch.Gestures.JoystickActive` false trong suốt |
| 14 | Kéo túi → túi phụ: chép `DragBagToHotbarMovesStack` (InventoryPlayTests `:276-285`), thay `Drag` bằng Feed (`overUi: true` lúc Began; Moved giữa đường + tới đích; Ended ở đích; mỗi bước một khung) | Như test gốc: `Hotbar.Get(1)` = Dirt × 5, `Main.Get(1)` trống |
| 15 | Chạm (không kéo) ô túi khi túi mở | `inv.HasSelection`; không cầm (`!input.Control.HasHeld`) |
| 16 | Bố cục: sau Feed rỗng | 9 ô `ButtonRect` nằm trong `Screen.safeArea`; cạnh mỗi ô ≥ `DpToPx(48, 160)`; không hai ô nào giao nhau (`Rect.Overlaps`); `ButtonRect(2).width` lớn nhất |
| 17 | Chấm tâm nhắm | `touch.Crosshair.gameObject.activeInHierarchy` true khi không mở bảng; `inv.ToggleBag()` + Feed rỗng → false |
| 18 | Tắt cảm ứng: `SetForceOn(false)`; ghi `f0 = GameInput.TouchBackFrame`; Feed Began ở tâm nút 9 rồi nút 5; `yield return null` 2 lần | `TouchControls.Active` false; `GameInput.TouchBackFrame == f0`; `SettingsView.IsAnyOpen` false; `input.Control.Mode == Default`; `touch.Crosshair` không hiện |

**PlayMode** — `MinimapCornerPlayTests`, method mới `TouchOnCornerOpensMapNotCamera`:
- `TouchControls.SetForceOn(true)`; `TouchSimulation.Disable()` (tránh thiết bị giả lập của Editor).
- `ts = InputSystem.AddDevice<Touchscreen>(); ts.MakeCurrent();`.
- GameObject mới có `TouchControls` (`readTouches` = true).
- `QueueStateEvent(ts, new TouchState { touchId = 1, phase = UnityEngine.InputSystem.TouchPhase.Began, position = map.ButtonScreenCenter })` + `InputSystem.Update()` → `yield return null`.
- Mong đợi:
  - `map.IsOpen`;
  - `tc.Gestures.JoystickActive` false;
  - `tc.Gestures.LookYaw == 0`.
- Rồi Ended + Update + `yield return null`.
- `finally`/cuối test: `InputSystem.RemoveDevice(ts)`, `TouchControls.SetForceOn(false)`, huỷ GameObject.

**Regression:**
- Toàn bộ EditMode + PlayMode hiện có (danh sách ở Tiêu chí xong).
- Mã băm world/sim không đổi (test mã băm hiện có đạt).

### Lệnh executor chạy
1. Ghi giờ bắt đầu (`date "+%H:%M"`).
2. Unity Editor (MCP): `refresh_unity`, rồi `read_console` (Error) → không lỗi biên dịch.
3. MCP `run_tests` EditMode (toàn bộ) → ghi dòng tổng kết (đạt/tổng).
4. MCP `run_tests` PlayMode (toàn bộ) → ghi dòng tổng kết.
5. Dọn: `git checkout -- client/ProjectSettings/EditorSettings.asset`.
6. `python3 tools/check_design.py` → exit 0.
7. `git diff --stat` chỉ gồm file trong Phạm vi. Commit code: `brief 0102: touch aim, main action button, mode/collect/bag/back buttons, touch in inventory` (kèm dòng Co-Authored-By theo AGENTS.md §11).
8. Ghi giờ kết thúc vào mục Kết quả.

### User tự test
Không ở brief này. User thử cảm giác trên điện thoại gộp ở brief 0103.

Nếu muốn xem sớm trong Editor: mở SimView, Play, bấm **F4**, rồi dùng chuột thay ngón tay:
- Bấm nút "Đào" bên phải, xoay camera cho chấm giữa màn hình rơi lên một ô gần: ô đó có lớp đỏ. Bấm "Làm" thì đào.
- Bấm "Túi" mở túi, kéo một ô sang túi phụ, bấm "Huỷ" đóng. Chạm ô túi phụ thì cầm vật.

## Dừng và chuyển lại Opus khi
- Brief mơ hồ hoặc mâu thuẫn với code thật (dòng trích dẫn đã dời quá xa, chữ ký khác).
- Cần một quyết định thuộc AGENTS.md §3.4 hoặc ngoài "Quyết định đã chốt".
- Cùng một lỗi sửa 2 lần vẫn chưa qua.
- Phải sửa file ngoài "Phạm vi", hoặc test cũ fail.
- Bố cục quyết định 11 làm test 16 fail (nút chồng nhau / ra ngoài safeArea) trên màn hình test: ghi kích thước `Screen.width × Screen.height` và các `ButtonRect` rồi dừng, không tự đổi hệ số.
- Test 13 hoặc 14 cho thấy `InventoryView.Covers` false tại tâm ô (nên `OverUi` sai).

---
<!-- Từ đây trở xuống: EXECUTOR ghi. -->

## Kết quả
- Base commit: 603b593
- Chạy bằng: claude-sonnet-5-5 · medium
- Bắt đầu: 20:00 · Kết thúc: 20:30 (giờ máy; phần lớn là chờ chạy test)
- Commit: 151a9bb — file: GameInput, TouchControls, TerrainEditInput, InventoryView, 6 file đọc Esc, PlayerMotion, SimConfig, 2 sim_m1.json, vi.json, SimConfigTests, MinimapCornerPlayTests, TouchActionPlayTests (mới)
- Lệch brief (cần Planner xem): (1) Test 1b: Ahead(2) cách tâm thân ≈2,5 ô > DigReach 2 nên đào ngay không xảy ra; test 1b dùng Ahead(1) (highlight vẫn kiểm Ahead(2)). (2) Test 4: nhân vật đã ở layer Ignore Raycast (PlayerCharacter.cs:204) nên tia PC cũng không trúng nhân vật; `aimIgnore` vô hại nhưng tiền đề quyết định 3 không đúng. Test 4 thay vế 'Pick → false' bằng kiểm layer + Pick trả Ahead(3). (3) Test minimap rớt 1 lần ở lần chạy đầy đủ đầu, đạt ở 2 lần sau (không tái hiện, chưa rõ nguyên nhân).

## Kết quả test
- SimConfigTests (2 method mới): đạt (EditMode 745/745).
- Test 1–18 TouchActionPlayTests + TouchOnCornerOpensMapNotCamera: đạt (xem lệch brief 1–3 ở trên).
- Lệnh: refresh_unity + console error: 0 lỗi; EditMode 745/745; PlayMode 318/318; check_design exit 0; hai sim_m1.json giống hệt; đã `git checkout` EditorSettings.asset.
- Chờ user: không (gộp brief 0103).

## Vướng mắc

---
<!-- PLANNER ghi khi review. -->

## Review

Planner, 2026-11-03, đọc Kết quả + diff `151a9bb`.

- **Chạy bằng:** `claude-sonnet-5-5 · medium`, khớp `Model thực thi`.
- **Kết quả test:**
  - EditMode 745/745 (738 + 7 = 1 + 6 `[TestCase]`), PlayMode 318/318 (299 + 19 = 18 test `TouchActionPlayTests` + `TouchOnCornerOpensMapNotCamera`). Số test tăng đúng.
  - Kết quả ghi gộp "Test 1–18 đạt". Đã đối chiếu tên test: 18 method khớp 18 dòng (1b nằm trong `AimHighlightsCenterCellAndActionDigsNow`). Đủ.
- **Quyết định đã chốt:**
  - QĐ1: `GameControls.inputactions` không đổi.
  - QĐ2–3: tâm nhắm = tâm màn hình chỉ khi `TouchControls.Active`; `Raycast` dùng `RaycastNonAlloc` chọn hit gần nhất ≠ `aimIgnore`, `aimIgnore` về null ngay sau `AimTargets`.
  - QĐ4: `TouchAction` đúng thứ tự chặn (quan sát → camera/world → bảng) và điều kiện mở bảng ô ruộng.
  - QĐ5: nút chế độ cần nhân vật + `!FlyMode`; Thu bỏ khi đang quan sát.
  - QĐ6: 8 chỗ đọc Esc OR `GameInput.TouchBack`; `TouchBackFrame` ghi sau `ReadAndFeed`, cả khi `readTouches == false`; thứ tự −60.
  - QĐ7–8: `RouteUiTouch` một ngón UI, bỏ ô nút; `TouchUp` cầm ô túi phụ khi túi + kho đóng; InventoryView bỏ nhánh chuột khi cảm ứng; `ClearOutputs` huỷ kéo dở.
  - QĐ10–12: id, bố cục và màu đúng bảng; nút chế độ từ dưới lên Tưới → Xới → Đào → Mặc định.
- **Lệch so với brief:**
  1. Test 1b dùng `Ahead(1)`: lỗi của brief (`Ahead(2)` ngoài tầm đào 2 tính từ tâm thân). Chấp nhận.
  2. Tiền đề QĐ3 sai: nhân vật đã ở layer `Ignore Raycast` (`PlayerCharacter.cs:204`), nên tia PC cũng xuyên qua nhân vật. `aimIgnore` thừa nhưng vô hại. Test 4 đổi vế cuối sang kiểm layer + `Pick` trả `Ahead(3)`. Chấp nhận. Không cần gỡ code.
  3. Test minimap rớt 1 lần ở lần chạy đầu, đạt 2 lần sau, không tái hiện được. Chấp nhận có theo dõi: lần sau rớt nữa thì sửa theo `bug-fix`. Nghi vấn [chưa xác minh]: test gọi `TouchSimulation.Disable()` mà không bật lại.
- **Phạm vi:** 19 file, đều trong Phạm vi; `ProjectSettings` và `GameControls.inputactions` không đổi; hai `sim_m1.json` giống hệt [run `diff`].
- **Giờ:** Executor 20:00–20:30 (~30 phút).
- Kết luận: đã duyệt
