# Brief 0066 — 7l f: bảng đổi phím trong Cài đặt → Điều khiển (đổi lúc chạy, lưu settings.json, trùng → Đổi chỗ / Huỷ, Mặc định, Esc cố định)

> Trạng thái: chờ làm
> Review: đầy đủ + user xem
> Model thực thi: Sonnet · medium
> Cập nhật: 2026-10-24

<!-- Planner (Opus) viết, Executor (Sonnet) làm theo ở PHIÊN KHÁC — executor không có lịch sử chat, không được tự quyết. -->

## Mục tiêu
Bước **7l f** (brief duy nhất, bước cuối) của task [m1-world-7l-dieu-khien-pc](../tasks/m1-world-7l-dieu-khien-pc.md). Dòng 7l f ở [world-tech](../../design/04-tech/world-tech.md) (dòng 191): "Bảng đổi phím trong Cài đặt → Điều khiển (luật 0033 mục 2): mọi hành động 7l, cả nút chuột; lưu `settings.json`; Mặc định; trùng cùng ngữ cảnh → hỏi đổi chỗ; Esc cố định". Xong khi: "Test PlayMode: đổi → lưu → nạp lại đúng; trùng → hỏi, Đổi chỗ / Huỷ; Mặc định trả về bảng gốc; user thử đổi chuột trái ↔ phải".
1. Lớp logic `KeyRebind` (client): danh sách hành động đổi được, kiểm phím hợp lệ, tìm trùng trong cùng ngữ cảnh, đổi chỗ, lưu / nạp override, đổi phím tương tác.
2. `PlayerSettings.bindingOverrides` lưu trong `settings.json`; nạp lúc khởi động.
3. Tab thứ ba **Điều khiển** trong bảng Cài đặt (IMGUI).

## Bối cảnh cần đọc
- Luật: [0033 mục 2](../../design/decisions/0033-tach-lop-hanh-dong-dieu-khien.md) (lưu `SaveBindingOverridesAsJson` trong `settings.json`, nút "Mặc định"; trùng **trong cùng ngữ cảnh** → báo "phím đã dùng cho X", Đổi chỗ / Huỷ; đổi chuột trái/phải = một lần đổi chỗ; chỉ chuột + bàn phím; phím debug F1–F3 không vào bảng; Esc cố định) và [0034 mục 8](../../design/decisions/0034-dieu-khien-pc-click-di-chuyen.md) (mọi phím đổi được, gồm nút chuột).
- `client/Assets/Game/Client/Input/GameControls.inputactions` — asset project-wide. **Ngữ cảnh = action map**: `Player`, `Pointer`, `Panels`, `Map`, `TimeDial`, `Settings`. Mỗi hành động có đúng **1 binding (index 0)**, không composite (kiểm bởi `GameControlsTests.AssetMatchesDefaultTable`, `client/Assets/Game/Tests/ClientEdit/GameControlsTests.cs:58`). Phím mặc định: bảng `Expected()` ở `GameControlsTests.cs:22-56`. Lưu ý: `Player/Hotbar1` và `Panels/TransferAll` cùng phím `1` — khác map nên **không** tính là trùng (cố ý, brief 0063).
- `client/Assets/Game/Client/GameInput.cs:8-69` — truy cập hành động: `static InputAction Get(string name) => InputSystem.actions.FindAction(name, true);` (dòng 75).
- `client/Assets/Game/Core/PlayerSettings.cs` — `[Serializable] class PlayerSettings` (`formatVersion = 3`, `Clamp()`, `Clone()`); `Game.Core` không được tham chiếu UnityEngine.
- `client/Assets/Game/Client/GameSettings.cs` — `Load()` (dòng 46-63), `Apply(PlayerSettings)` (dòng 87-96, đặt `Dirty`), `Save()` (dòng 98-110), `ResetForTests(string)` (dòng 144-153).
- `client/Assets/Game/Client/SettingsView.cs` — bảng IMGUI: `Keys` (dòng 11-16), `SetOpen` (dòng 84-88, lưu khi đóng nếu `Dirty`), `Update` (dòng 90-93), tab `GUI.Toolbar` 2 tab rộng 240 (dòng 149), tab Hướng dẫn dùng scroll view (dòng 152-161), `GuideText()` (dòng 123-130), `PanelRect` 460×480 (dòng 35).
- `client/Assets/Game/Client/DebugKeys.cs` — F1, F2, F3 đọc thẳng bàn phím (không vào bảng).
- `client/Assets/Game/Tests/Client/GameControlsPlayTests.cs:6-20` — **dưới test runner project-wide actions không tự bật** (0047 QĐ 9); test tự bật rồi trả lại. `FlyCameraMoveKeyPlayTests.cs:13-27` — mẫu bàn phím ảo: `InputSystem.AddDevice<Keyboard>()`, `InputSystem.QueueStateEvent(kb, new KeyboardState(Key.X)); InputSystem.Update();`, `finally InputSystem.RemoveDevice(kb)`.
- `client/Assets/Game/Tests/Client/SettingsPlayTests.cs:17-38` — mẫu thư mục tạm: `GameSettings.ResetForTests(dir)` ở SetUp, `ResetForTests(SettingsTestRoot.Dir)` ở TearDown.
- Bảng chuỗi: `client/Assets/Game/Client/Resources/Lang/vi.json` (`{"formatVersion","language","entries":[{"key","text"}]}`, các key xếp theo chữ cái — thêm key mới đúng chỗ theo thứ tự đó).

### API Input System 1.19 (đã tra docs — chỉ dùng những API này)
- [ActionBindings manual](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.19/manual/ActionBindings.html): `asset.SaveBindingOverridesAsJson()`, `asset.LoadBindingOverridesFromJson(json)` (mặc định xoá override cũ trước), `asset.RemoveAllBindingOverrides()`, `action.ApplyBindingOverride(int bindingIndex, string path)`, RebindingOperation **phải `Dispose()`**.
- [InputControlPath](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.19/api/UnityEngine.InputSystem.InputControlPath.html): `public static string ToHumanReadableString(string path, InputControlPath.HumanReadableStringOptions options = HumanReadableStringOptions.None, InputControl control = null)`; [`HumanReadableStringOptions`](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.19/api/UnityEngine.InputSystem.InputControlPath.HumanReadableStringOptions.html): `None`, `OmitDevice`, `UseShortNames`.
- Test: [`MouseState.WithButton(MouseButton button, bool state = true)`](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.19/api/UnityEngine.InputSystem.LowLevel.MouseState.html), [`MouseButton`](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.19/api/UnityEngine.InputSystem.LowLevel.MouseButton.html) `Left/Right/Middle/Forward/Back`; `KeyboardState`, `InputSystem.AddDevice/RemoveDevice/QueueStateEvent/Update` như `FlyCameraMoveKeyPlayTests`.
- [InputActionRebindingExtensions](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.19/api/UnityEngine.InputSystem.InputActionRebindingExtensions.html): `action.PerformInteractiveRebinding(int bindingIndex = -1)` — mặc định đã loại pointer position/delta, chờ 0.05 s tìm khớp tốt hơn, nén sự kiện khớp; **Esc KHÔNG tự huỷ khi hành động là Button** → phải gọi `WithCancelingThrough`. `action.RemoveBindingOverride(int bindingIndex)`.
- [RebindingOperation](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.19/api/UnityEngine.InputSystem.InputActionRebindingExtensions.RebindingOperation.html): **hành động phải đang tắt** (`WithAction` ném `InvalidOperationException` nếu enabled); `WithControlsHavingToMatchPath(string)` gọi nhiều lần = OR; `WithControlsExcluding(string)` gọi nhiều lần; `WithCancelingThrough(string)`; `WithExpectedControlType(string)` (WithAction tự đặt theo action); `OnComplete(Action<RebindingOperation>)` (binding đã được áp khi gọi), `OnCancel(Action<RebindingOperation>)`; `Start()`; `Dispose()`; `completed`, `canceled`. Đường dẫn chọn được mặc định được tổng quát hoá theo loại thiết bị (vd `<Keyboard>/k`, `<Mouse>/rightButton`).

## Quyết định đã chốt (không được đổi)
- Luật 0033 mục 2 + 0034 mục 8 (user 2026-10-22/23) như ở trên.
- **Hành động đổi được** (Planner): mọi hành động `type == Button` trong các map `Player`, `Panels`, `Map`, `TimeDial` có phím mặc định khác `<Keyboard>/escape` → **34 hành động** (Player 28: mọi Button trừ `Back`; Panels 4: `NextTarget`, `TransferAll`, `TransferHalf`, `TransferThird`; `Map/Press`; `TimeDial/Press`). Không vào bảng: `Look`, `Zoom`, `Map/Zoom`, `Pointer/Point` (trục chuột), mọi `Close`/`Back` (Esc cố định), map `Settings`.
- **Phím hợp lệ** (Planner): bàn phím mọi phím trừ `escape`, `f1`, `f2`, `f3`, `anyKey`; chuột chỉ 5 nút `leftButton`, `rightButton`, `middleButton`, `forwardButton`, `backButton`. Kiểm bằng hàm `IsAllowedPath` sau khi chọn (không tin riêng bộ lọc của RebindingOperation): không hợp lệ → trả phím cũ, coi như huỷ.
- **Trùng** = cùng map, hành động khác, `SameControl` đúng. `SameControl`: so không phân biệt hoa thường; thêm: `<Keyboard>/shift` trùng `leftShift` và `rightShift` (tương tự `ctrl`, `alt`); `leftShift` không trùng `rightShift`.
- Trong lúc chờ bấm phím: **tắt mọi action map đang bật** của asset, xong (hoàn tất / huỷ) **bật lại đúng các map đã bật trước đó** (không bật map vốn đang tắt — dưới test runner asset không tự bật).
- Lưu: `PlayerSettings.bindingOverrides` (string, mặc định `""`) = chuỗi `SaveBindingOverridesAsJson()`. Không tăng `formatVersion` (JsonUtility để `""` khi thiếu trường). Ghi file theo cơ chế cũ (đóng bảng Cài đặt khi `Dirty`).
- Nạp: `GameSettings.Load()` áp override lên `InputSystem.actions`; JSON hỏng, có phím không hợp lệ, hoặc có trùng trong cùng map → bỏ toàn bộ override (về mặc định) + `Debug.LogWarning("KEYS ...")` + đặt `bindingOverrides = ""`.
- Tên phím hiển thị: chuột → chuỗi `controls.mouse_*`; bàn phím → `InputControlPath.ToHumanReadableString(path, OmitDevice)` (hàm `KeyRebind.DisplayPath`).
- Đang chờ phím hoặc đang hỏi trùng mà bảng Cài đặt bị đóng → huỷ (trả phím cũ).
- Chữ hướng dẫn (`guide.*`) và nhãn nút có phím trong ngoặc (`Túi (B)`, `Cài đặt (O)`, `Phép (G)`, `Đóng (M)`, `Xem camera (V)`, `Quay về (V)`) **giữ phím mặc định**; chỉ thêm một dòng chú thích ở đầu tab Hướng dẫn (bước 5). Hiện phím theo bảng đổi phím trong các nhãn này: ngoài phạm vi (Planner ghi vào handoff).
- Review kết luận "code ok, chờ user xem gộp cuối" (user 2026-10-24); mục User tự test chỉ để Planner chép vào danh sách cuối.

## Phạm vi
- Được sửa/tạo:
  - `client/Assets/Game/Client/KeyRebind.cs` (mới) + `.meta` do Unity tạo
  - `client/Assets/Game/Client/GameSettings.cs`, `client/Assets/Game/Client/SettingsView.cs`
  - `client/Assets/Game/Core/PlayerSettings.cs`
  - `client/Assets/Game/Client/Resources/Lang/vi.json`
  - `client/Assets/Game/Tests/ClientEdit/KeyRebindTests.cs` (mới), `client/Assets/Game/Tests/ClientEdit/GameControlsTests.cs` (chỉ thêm `"KeyRebind.cs"` vào mảng `files` của `NoDirectInputReadsInScope`)
  - `client/Assets/Game/Tests/Core/PlayerSettingsTests.cs`
  - `client/Assets/Game/Tests/Client/KeyRebindPlayTests.cs` (mới)
  - `.ai/briefs/0066-7l-f-bang-doi-phim.md` (mục Kết quả trở xuống)
- Không được sửa: `GameControls.inputactions` (phím mặc định giữ nguyên), `GameInput.cs`, `loc/*` (dịch làm sau), `design/*`, mọi file đang có thay đổi chưa commit của phiên khác (`design/decisions/0038-*`, `design/02-gameplay/*`, `design/05-roadmap.md`, `design/summary.md`, `design/glossary.md`, `design/ideas.md`, `design/decisions/README.md`, `tools/terrain_catalog*`, `.claude/skills/local-terrain-gen/`, `.ai/skill-map.local.json`).

## Các bước
1. **`PlayerSettings`** (`Core/PlayerSettings.cs`):
   - thêm `/// <summary>Chuỗi SaveBindingOverridesAsJson (7l f, 0033 mục 2); "" = phím mặc định.</summary> public string bindingOverrides = "";`
   - `Clamp()`: `if (bindingOverrides == null) bindingOverrides = "";` (đặt sau dòng `language`).
   - `Clone()`: chép `bindingOverrides`.
   - Xong khi: biên dịch, test bước 7 (Core) qua.

2. **`KeyRebind`** (`Client/KeyRebind.cs`, `namespace Game.Client`, `public static class KeyRebind`). Chú thích lớp: `/// <summary>Bảng đổi phím (7l f, 0033 mục 2 / 0034 mục 8): hành động đổi được, phím hợp lệ, trùng trong cùng ngữ cảnh (= action map), đổi chỗ, lưu/nạp override, đổi phím tương tác. Mọi hành động có đúng 1 binding (index 0).</summary>`. Thành viên (chữ ký đúng như sau):
   - `public static readonly string[] Contexts = { "Player", "Panels", "Map", "TimeDial" };`
   - `public static List<InputAction> Rebindable(InputActionAsset asset)` — duyệt map theo thứ tự `Contexts`, trong map theo thứ tự `map.actions`; lấy action có `type == InputActionType.Button` và `bindings[0].path != "<Keyboard>/escape"`.
   - `public static readonly Dictionary<string, string> NameKeys` — `"Map/Action"` → key chuỗi, đủ 34 mục, đúng bảng ở bước 4 (Hotbar1–10 cùng key `controls.hotbar`).
   - `public static string DisplayName(InputAction a)` — `Hotbar1`..`Hotbar10` → `GameSettings.Text.Format("controls.hotbar", n)` với n = 1..10; còn lại `GameSettings.Text.Get(NameKeys[map/name])`.
   - `public static string DisplayPath(string path)` — `<Mouse>/leftButton|rightButton|middleButton|forwardButton|backButton` (không phân biệt hoa thường) → `GameSettings.Text.Get("controls.mouse_left|mouse_right|mouse_middle|mouse_forward|mouse_back")`; còn lại `InputControlPath.ToHumanReadableString(path, InputControlPath.HumanReadableStringOptions.OmitDevice)`.
   - `public static string DisplayKey(InputAction a) => DisplayPath(a.bindings[0].effectivePath);`
   - `public static bool IsAllowedPath(string path)` — null/rỗng → false; so không phân biệt hoa thường; `<Keyboard>/x` → true trừ `escape`, `f1`, `f2`, `f3`, `anykey`; `<Mouse>/` + một trong 5 nút ở trên → true; còn lại (gamepad, touch, `<Mouse>/position`, `<Mouse>/scroll...`, `<Mouse>/press`, `<Pointer>/...`) → false.
   - `public static bool SameControl(string a, string b)` — theo mục Quyết định.
   - `public static InputAction FindConflict(InputAction action, string path)` — action khác trong `action.actionMap` có `bindings[0].effectivePath` `SameControl` với `path`; không có → null.
   - `public static void SetPath(InputAction action, string path)` — `path` == `bindings[0].path` (không phân biệt hoa thường) → `RemoveBindingOverride(0)`; khác → `ApplyBindingOverride(0, path)`.
   - `public static void Swap(InputAction a, InputAction b)` — đọc hai `effectivePath` trước, rồi `SetPath(a, pb)`, `SetPath(b, pa)`.
   - `public static string Save(InputActionAsset asset)` → `asset.SaveBindingOverridesAsJson()`.
   - `public static bool Load(InputActionAsset asset, string json)` — `asset.RemoveAllBindingOverrides()`; json rỗng → true; còn lại `try { asset.LoadBindingOverridesFromJson(json); } catch (Exception)` → `RemoveAllBindingOverrides()`, false. Sau khi nạp: duyệt `Rebindable(asset)`: có `effectivePath` không `IsAllowedPath`, hoặc có `FindConflict` khác null → `RemoveAllBindingOverrides()`, false. Còn lại true. (Không tự log — người gọi log.)
   - `public static InputActionRebindingExtensions.RebindingOperation Start(InputAction action, Action<bool, string> done)` — `done(completed, oldPath)`:
     ```csharp
     var asset = action.actionMap.asset;
     var wasEnabled = new List<InputActionMap>();
     foreach (var m in asset.actionMaps) if (m.enabled) { wasEnabled.Add(m); m.Disable(); }
     string oldPath = action.bindings[0].effectivePath;
     void Finish(InputActionRebindingExtensions.RebindingOperation op, bool ok)
     {
         if (ok && !IsAllowedPath(action.bindings[0].effectivePath)) { SetPath(action, oldPath); ok = false; }
         op.Dispose();
         foreach (var m in wasEnabled) m.Enable();
         done(ok, oldPath);
     }
     return action.PerformInteractiveRebinding(0)
         .WithControlsHavingToMatchPath("<Keyboard>")
         .WithControlsHavingToMatchPath("<Mouse>")
         .WithControlsExcluding("<Keyboard>/anyKey")
         .WithControlsExcluding("<Mouse>/press")
         .WithControlsExcluding("<Mouse>/scroll")
         .WithControlsExcluding("<Keyboard>/f1")
         .WithControlsExcluding("<Keyboard>/f2")
         .WithControlsExcluding("<Keyboard>/f3")
         .WithCancelingThrough("<Keyboard>/escape")
         .OnComplete(op => Finish(op, true))
         .OnCancel(op => Finish(op, false))
         .Start();
     ```
     (Không gọi `Dispose` ở chỗ khác. Huỷ từ ngoài: gọi `op.Cancel()` → `OnCancel` chạy.)
   - Xong khi: test EditMode bước 7 qua.

3. **`GameSettings`** (`Client/GameSettings.cs`):
   - thêm `using UnityEngine.InputSystem;`.
   - `Load()`: sau `current = s;` gọi `ApplyBindings();`.
   - thêm `static void ApplyBindings()`: `var asset = InputSystem.actions; if (asset == null) return; if (!KeyRebind.Load(asset, current.bindingOverrides)) { Debug.LogWarning("KEYS invalid binding overrides in settings.json, using defaults"); current.bindingOverrides = ""; }` (json rỗng thì `Load` trả true, không log).
   - thêm `public static void SetBindingOverrides(string json)`: `var s = Current.Clone(); s.bindingOverrides = json ?? ""; Apply(s);` (không áp lại lên asset — asset đã đổi trước khi gọi).
   - `ResetForTests`: thêm `InputSystem.actions?.RemoveAllBindingOverrides();` để test không rò phím đã đổi.
   - Xong khi: test `KeyRebindPlayTests.SaveAndReload`, `BadOverrides_FallBackToDefaults` qua.

4. **Chuỗi** (`Resources/Lang/vi.json`) — thêm đúng nguyên văn, xếp theo chữ cái:

   | key | text |
   |---|---|
   | `settings.tab_controls` | `Điều khiển` |
   | `guide.keys_note` | `Phím ghi trong hướng dẫn là phím mặc định; phím đang dùng xem ở tab Điều khiển.` |
   | `controls.ctx_player` | `Đi lại, công cụ, camera` |
   | `controls.ctx_panels` | `Túi, túi phụ, kho (khi đang mở)` |
   | `controls.ctx_map` | `Bản đồ (khi đang mở)` |
   | `controls.ctx_timedial` | `Vòng thời điểm (khi đang mở)` |
   | `controls.change` | `Đổi` |
   | `controls.waiting` | `Bấm phím hoặc nút chuột mới cho "{0}"… (Esc: huỷ)` |
   | `controls.conflict` | `Phím {0} đã dùng cho "{1}". Đổi chỗ: "{1}" nhận phím {2}.` |
   | `controls.swap` | `Đổi chỗ` |
   | `controls.cancel` | `Huỷ` |
   | `controls.defaults` | `Mặc định` |
   | `controls.fixed_note` | `Esc cố định (đóng bảng / huỷ). Phím debug F1–F3 không đổi được.` |
   | `controls.mouse_left` | `Chuột trái` |
   | `controls.mouse_right` | `Chuột phải` |
   | `controls.mouse_middle` | `Chuột giữa` |
   | `controls.mouse_forward` | `Chuột tiến` |
   | `controls.mouse_back` | `Chuột lùi` |
   | `controls.hotbar` | `Túi phụ ô {0}` |
   | `controls.move_click` (Player/MoveClick) | `Đi tới chỗ click` |
   | `controls.use_tool` (Player/UseTool) | `Thao tác chọn (chế độ / vật cầm)` |
   | `controls.rotate_camera` (Player/RotateCamera) | `Xoay camera (giữ)` |
   | `controls.jump` (Player/Jump) | `Nhảy` |
   | `controls.dash` (Player/Dash) | `Lướt` |
   | `controls.camera_reset` (Player/CameraReset) | `Camera về mặc định` |
   | `controls.first_person` (Player/ToggleFirstPerson) | `Góc nhìn thứ nhất / thứ ba` |
   | `controls.mode_default` (Player/ModeDefault) | `Chế độ Mặc định` |
   | `controls.mode_dig` (Player/ModeDig) | `Chế độ Đào` |
   | `controls.mode_till` (Player/ModeTill) | `Chế độ Xới` |
   | `controls.mode_rain` (Player/ModeRain) | `Chế độ Tưới` |
   | `controls.collect` (Player/Collect) | `Thu` |
   | `controls.bag` (Player/ToggleBag) | `Túi đồ` |
   | `controls.toggle_hotbar` (Player/ToggleHotbar) | `Ẩn/hiện túi phụ` |
   | `controls.spell_menu` (Player/ToggleSpellMenu) | `Menu phép` |
   | `controls.observe` (Player/ToggleObservation) | `Xem camera quan sát` |
   | `controls.map` (Player/ToggleMap) | `Bản đồ` |
   | `controls.settings` (Player/ToggleSettings) | `Cài đặt` |
   | `controls.next_target` (Panels/NextTarget) | `Đổi đích chuyển đồ` |
   | `controls.transfer_all` (Panels/TransferAll) | `Chuyển toàn bộ` |
   | `controls.transfer_half` (Panels/TransferHalf) | `Chuyển một nửa` |
   | `controls.transfer_third` (Panels/TransferThird) | `Chuyển một phần ba` |
   | `controls.map_press` (Map/Press) | `Bấm trên bản đồ` |
   | `controls.dial_press` (TimeDial/Press) | `Chọn mốc trên vòng thời điểm` |

   `NameKeys` (bước 2) ánh xạ đúng cột ngoặc → key; `Player/Hotbar1`..`Hotbar10` → `controls.hotbar`. Thêm vào `SettingsView.Keys` 19 key UI ở đầu bảng (từ `settings.tab_controls` tới `controls.hotbar`). Xong khi: `SettingsPlayTests.BaseFile_HasAllKeys` + `KeyRebindTests.NameKeysExistInBaseText` qua.

5. **`SettingsView`** (`Client/SettingsView.cs`):
   - `Tab` comment: `0 = Chung, 1 = Hướng dẫn, 2 = Điều khiển`. Toolbar 3 tab: `new[] { T("settings.tab_general"), T("settings.tab_guide"), T("settings.tab_controls") }`, rộng 360.
   - `GuideText()`: thêm `T("guide.keys_note")` làm phần tử **đầu tiên** của mảng.
   - Trạng thái + API công khai (để test gọi, không cần click):
     - `InputActionRebindingExtensions.RebindingOperation rebindOp; InputAction rebinding;` — `public bool IsWaiting => rebindOp != null;`
     - `public InputAction PendingAction { get; private set; }`, `public InputAction PendingConflict { get; private set; }`, `string pendingOld;`
     - `public void BeginRebind(InputAction a)` — đang chờ hoặc đang hỏi trùng → bỏ qua; `rebinding = a; rebindOp = KeyRebind.Start(a, (ok, old) => { rebindOp = null; rebinding = null; if (ok) ApplyChosen(a, old); });`
     - `public void ApplyChosen(InputAction a, string oldPath)` — `var other = KeyRebind.FindConflict(a, a.bindings[0].effectivePath);` null → `SaveBindings()`; khác null → `PendingAction = a; PendingConflict = other; pendingOld = oldPath;` (binding mới vẫn giữ trên `a` trong lúc hỏi).
     - `public void ResolveConflict(bool swap)` — không có pending → bỏ qua. `swap`: `KeyRebind.SetPath(PendingConflict, pendingOld); SaveBindings();` — không swap: `KeyRebind.SetPath(PendingAction, pendingOld);`. Cả hai: xoá pending.
     - `public void ResetBindings()` — huỷ chờ / pending nếu có; `InputSystem.actions.RemoveAllBindingOverrides(); SaveBindings();`
     - `void SaveBindings() => GameSettings.SetBindingOverrides(KeyRebind.Save(InputSystem.actions));`
     - `SetOpen(bool open)`: khi `!open` — trước dòng lưu hiện có: `rebindOp?.Cancel(); ResolveConflict(false);`.
     - `OnDestroy`: `rebindOp?.Cancel();`.
   - Vẽ tab 2 trong `OnGUI` (sau khối `tab == 1`, trước khối Chung, dạng `if (tab == 2) { …; return; }`):
     - Dòng ghi chú `controls.fixed_note` (WrapStyle, cao 40).
     - Nếu `IsWaiting`: dòng `Format("controls.waiting", KeyRebind.DisplayName(rebinding))` (màu vàng `<color=yellow>`).
     - Nếu `PendingConflict != null`: dòng `Format("controls.conflict", KeyRebind.DisplayKey(PendingAction), KeyRebind.DisplayName(PendingConflict), <tên phím cũ>)` — tên phím cũ = `KeyRebind.DisplayPath(pendingOld)` — rồi hai nút `controls.swap` (→ `ResolveConflict(true)`), `controls.cancel` (→ `ResolveConflict(false)`).
     - Scroll view (giống tab Hướng dẫn) từ dưới các dòng trên tới `p.yMax - 48f`: với từng ngữ cảnh trong `KeyRebind.Contexts`: tiêu đề in đậm `controls.ctx_<player|panels|map|timedial>`; mỗi hành động của ngữ cảnh đó trong `KeyRebind.Rebindable(InputSystem.actions)`: tên (`DisplayName`, rộng 55%), phím (`DisplayKey`, in đậm, rộng 25%), nút `controls.change` rộng 60 (`GUI.enabled = !IsWaiting && PendingConflict == null` khi vẽ nút) → `BeginRebind(action)`. Hàng cao 26.
     - Dưới cùng: nút `settings.close` như tab khác (x, `p.yMax - 40f`, 140×30) và nút `controls.defaults` cạnh nó (x + 150, cùng hàng, 140×30) → `ResetBindings()`.
   - Xong khi: biên dịch, 0 lỗi console; test PlayMode bước 7 qua.

6. **`GameControlsTests.NoDirectInputReadsInScope`**: thêm `"KeyRebind.cs"` vào mảng `files` (`GameControlsTests.cs:96`).

7. **Test** — viết đúng các test ở "Phần cần test" (tên test như ghi). Mọi test PlayMode đụng `InputSystem.actions` phải trả lại trạng thái trong `finally`/TearDown: `InputSystem.actions.RemoveAllBindingOverrides()`, bật/tắt map như trước, `RemoveDevice` thiết bị ảo. Test EditMode dùng asset đọc từ file (`InputActionAsset.FromJson(File.ReadAllText(...GameControls.inputactions))`, như `GameControlsTests.Load()`), không dùng `InputSystem.actions`.

## Tiêu chí xong
- Tab Điều khiển liệt kê 34 hành động theo 4 ngữ cảnh; đổi phím tương tác chạy; trùng cùng ngữ cảnh → hỏi Đổi chỗ / Huỷ; Mặc định trả bảng gốc; lưu/nạp qua `settings.json`.
- `Game.Core` không tham chiếu UnityEngine; EditMode + PlayMode qua hết qua MCP, 0 lỗi console.
- Phím mặc định (`GameControlsTests`) không đổi.

## Test sau khi xong

### Phần cần test
- `PlayerSettings` (bước 1) · `PlayerSettingsTests`: thêm `BindingOverrides_DefaultEmpty_CloneCopies_NullClamped` (mặc định `""`; Clone chép; `null` + Clamp → `""`; JsonUtility `{"uiScale":1.5}` → `bindingOverrides == ""`) · qua; test cũ qua.
- `KeyRebind` (bước 2) · `KeyRebindTests` (EditMode):
  - `Rebindable_Is34_ButtonsOnly_NoEscape` — 34; mọi action Button; không path escape; có `Player/MoveClick`, `Player/UseTool`, `Map/Press`, `TimeDial/Press`, `Panels/TransferThird`; không có `Player/Look`, `Player/Back`, `Settings/Close`.
  - `NameKeysCoverRebindable` — mỗi action trong `Rebindable` có trong `NameKeys`; `NameKeys.Count == 34`.
  - `NameKeysExistInBaseText` — mọi giá trị `NameKeys` + `controls.mouse_*` có trong `vi.json` (đọc file `Application.dataPath/Game/Client/Resources/Lang/vi.json`).
  - `IsAllowedPath_Cases` — true: `<Keyboard>/k`, `<keyboard>/K`, `<Keyboard>/space`, `<Mouse>/leftButton`, `<Mouse>/backButton`; false: null, `""`, `<Keyboard>/escape`, `<Keyboard>/f1`, `<Keyboard>/f3`, `<Keyboard>/anyKey`, `<Mouse>/position`, `<Mouse>/scroll/up`, `<Mouse>/press`, `<Gamepad>/buttonSouth`, `<Touchscreen>/press`.
  - `SameControl_Cases` — `shift`/`leftShift` true; `leftShift`/`rightShift` false; `<Keyboard>/A`/`<keyboard>/a` true; `ctrl`/`rightCtrl` true; `a`/`b` false.
  - `FindConflict_SameMapOnly` — `SetPath(Jump, "<Keyboard>/q")` → `FindConflict(Jump, "<Keyboard>/q") == ModeDig`; `FindConflict(Player/Hotbar1, "<Keyboard>/1")` null (Panels/TransferAll khác map); `FindConflict(ModeDig, "<Keyboard>/k")` null.
  - `Swap_MouseButtons_SaveLoadRoundTrip` — `Swap(MoveClick, UseTool)` → MoveClick `<Mouse>/rightButton`, UseTool `<Mouse>/leftButton`; `Save` → `Load` vào asset mới đọc từ file → cùng kết quả, `Load` trả true.
  - `SetPath_ToDefault_RemovesOverride` — đặt khác rồi đặt lại phím gốc → `string.IsNullOrEmpty(bindings[0].overridePath)`.
  - `Load_Invalid_ResetsToDefaults` — (a) json rác `"{bad"` → false, mọi effectivePath = mặc định; (b) json từ asset có Jump = `<Keyboard>/escape` (tạo bằng `ApplyBindingOverride` trực tiếp rồi `Save`) → false, mặc định; (c) json có Jump = `<Keyboard>/q` (trùng ModeDig) → false, mặc định; (d) `""` → true.
- `GameSettings` + đổi tương tác + `SettingsView` (bước 3, 5) · `KeyRebindPlayTests` (PlayMode, thư mục tạm như `SettingsPlayTests`):
  - `SaveAndReload` — swap MoveClick ↔ UseTool trên `InputSystem.actions`, `GameSettings.SetBindingOverrides(KeyRebind.Save(InputSystem.actions))`, `Save()`; `InputSystem.actions.RemoveAllBindingOverrides()`; `GameSettings.Load()` → `GameInput.MoveClick.bindings[0].effectivePath == "<Mouse>/rightButton"`, UseTool `leftButton`; `settings.json` chứa `bindingOverrides`.
  - `BadOverrides_FallBackToDefaults` — ghi `settings.json` có `"bindingOverrides":"{bad"`, `Load()` → mặc định, `Current.bindingOverrides == ""`, `LogAssert.Expect(LogType.Warning, new Regex("^KEYS"))`.
  - `Interactive_KeyboardKey` — bàn phím ảo; `KeyRebind.Start(GameInput.Jump, cb)`; bấm `Key.K` (QueueStateEvent + Update), thả, chờ `WaitForSecondsRealtime(0.2f)`, `InputSystem.Update()` vài lần → cb `(true, "<Keyboard>/a")`, Jump = `<Keyboard>/k`; các map bật trước đó bật lại, map tắt vẫn tắt.
  - `Interactive_EscCancels` — bấm `Key.Escape` → cb `(false, …)`, Jump vẫn `<Keyboard>/a`.
  - `Interactive_MouseRightButton` — chuột ảo (`InputSystem.AddDevice<Mouse>()`, `QueueStateEvent(mouse, new MouseState().WithButton(MouseButton.Right))`); `Start(GameInput.MoveClick, …)` → MoveClick = `<Mouse>/rightButton`; `FindConflict` = UseTool.
  - `View_Conflict_CancelRestores` / `View_Conflict_SwapSwaps` — `GameObject` + `AddComponent<SettingsView>()`; `KeyRebind.SetPath(GameInput.MoveClick, "<Mouse>/rightButton")`; `view.ApplyChosen(GameInput.MoveClick, "<Mouse>/leftButton")` → `PendingConflict == GameInput.UseTool`; `ResolveConflict(false)` → MoveClick `leftButton`, UseTool `rightButton`, không Dirty; `ResolveConflict(true)` → MoveClick `rightButton`, UseTool `leftButton`, `GameSettings.Dirty`, `Current.bindingOverrides` khác `""`.
  - `View_CloseWhilePending_Cancels` — như trên rồi `SetOpen(true); SetOpen(false)` → về như cũ, pending null.
  - `View_ResetBindings_RestoresDefaults` — swap + lưu, `ResetBindings()` → mọi effectivePath = path gốc; `Current.bindingOverrides` không chứa `rightButton` (chuỗi rỗng hoặc json không còn override).
  - `View_NoConflict_SavesImmediately` — `SetPath(Jump, "<Keyboard>/k")`, `ApplyChosen(Jump, "<Keyboard>/a")` → không pending, `Dirty`.
- Chữ (bước 4–5) · `SettingsPlayTests.BaseFile_HasAllKeys`; `GuideText` bắt đầu bằng chữ của `guide.keys_note` (thêm assert vào test hiện có dùng `GuideText()` ở `SettingsPlayTests.cs:254`, hoặc test mới `GuideStartsWithKeysNote`) · qua.
- Regression · `GameControlsTests` (5 test, gồm `NoDirectInputReadsInScope` có `KeyRebind.cs`), `GameControlsPlayTests`, `SettingsPlayTests`, `SettingsScenePlayTests`, `InventoryPlayTests`, `ControlModePlayTests`, `FlyCameraMoveKeyPlayTests` · qua.
- Toàn bộ · EditMode + PlayMode · qua hết, 0 lỗi console.

### Lệnh executor chạy
1. MCP `refresh_unity` → `read_console` (Error) = 0.
2. MCP `run_tests` EditMode (toàn bộ) → dòng tổng.
3. MCP `run_tests` PlayMode (toàn bộ) → dòng tổng.
4. Dọn: `git checkout -- client/ProjectSettings/EditorSettings.asset`.
5. `git status --short` — chỉ file trong Phạm vi (bỏ qua file chưa commit của phiên khác, không stage chúng); commit code + test + brief, vd `brief 0066: 7l f key rebinding tab (swap on conflict, defaults, saved in settings.json)`. Không push.

### User tự test
(Gộp vào danh sách chơi thử cuối task — Planner chép; không chờ user.)
- O → tab **Điều khiển**: 4 nhóm, 34 dòng, tên tiếng Việt + phím hiện tại; cuộn được; chú thích Esc / F1–F3.
- Bấm "Đổi" ở "Đi tới chỗ click" → dòng vàng chờ → bấm **chuột phải** → hỏi "Phím Chuột phải đã dùng cho "Thao tác chọn…"" → **Đổi chỗ** → vào game: chuột phải đi, chuột trái thao tác. Đóng game, mở lại → vẫn đổi. Bấm **Mặc định** → về như cũ.
- Đổi "Nhảy" sang Q → hỏi trùng "Chế độ Đào" → **Huỷ** → Nhảy vẫn A. Trong lúc chờ phím bấm Esc → huỷ, bảng Cài đặt không đóng.
- Đổi "Túi phụ ô 1" sang một phím khác rồi thử; "Chuyển toàn bộ" (1) vẫn dùng được khi túi mở.
- Thử gán Esc / F1 → không nhận (Esc huỷ; F1 bị bỏ qua, vẫn chờ).

## Dừng và chuyển lại Opus khi
- Brief mơ hồ hoặc mâu thuẫn với code thật (vd action có hơn 1 binding; `Rebindable` không ra 34).
- RebindingOperation trả path khác dạng `<Keyboard>/…` / `<Mouse>/…Button` (vd `<Pointer>/press`) khiến `Interactive_MouseRightButton` hỏng — không tự đổi `IsAllowedPath`.
- Test tương tác phụ thuộc thời gian chạy chập chờn sau 2 lần sửa.
- Cần một quyết định thuộc AGENTS.md §3.4 hoặc ngoài "Quyết định đã chốt".
- Cùng một lỗi sửa 2 lần vẫn chưa qua.
- Phải sửa file ngoài "Phạm vi".
- Cần API Unity không có trong "Bối cảnh cần đọc".

---
<!-- Từ đây trở xuống: EXECUTOR ghi. -->

## Kết quả
<!-- Base commit · Chạy bằng: <model id> · <effort> · giờ bắt đầu / kết thúc · commit · file đã đổi · lệch so với brief (kèm lý do). -->

## Kết quả test
<!-- Mỗi dòng ở "Phần cần test" → đạt / không đạt + bằng chứng. Mỗi lệnh → kết quả thật. -->

## Vướng mắc

---
<!-- PLANNER ghi khi review. -->

## Review
