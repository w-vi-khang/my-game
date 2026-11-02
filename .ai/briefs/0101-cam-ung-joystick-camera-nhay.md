# Brief 0101 — Cảm ứng (a): chế độ cảm ứng, joystick, vuốt/chụm camera, nút Nhảy

> Trạng thái: chờ làm
> Review: đầy đủ
> Model thực thi: Sonnet · medium
> Cập nhật: 2026-11-02 07:43

<!-- Planner (Opus) viết, Executor (Sonnet) làm theo ở PHIÊN KHÁC — executor không có lịch sử chat, không được tự quyết.
Trạng thái: chờ làm → đang làm → chờ review → đã duyệt; hoặc → cần Opus (executor dừng, hỏi ở mục Vướng mắc). -->

## Mục tiêu
Phần (a) của task [m1-dieu-khien-cam-ung](../tasks/m1-dieu-khien-cam-ung.md). Brief này làm:
- chế độ cảm ứng: tự bật trên điện thoại, bật bằng F4 trong Editor;
- joystick ảo ở nửa trái màn hình;
- vuốt nửa phải để xoay camera, chụm 2 ngón để phóng to/thu nhỏ;
- nút Nhảy.

Logic cử chỉ viết thuần trong `Game.Core` để test EditMode. Client chỉ đọc chạm, đưa vào lõi, rồi áp kết quả lên nhân vật và camera.

**Chưa làm** (brief 0102): tâm nhắm, nút hành động chính, thanh chế độ, Thu/Túi/Huỷ, túi phụ, chạm trong các bảng.

## Bối cảnh cần đọc
- Task contract: `.ai/tasks/m1-dieu-khien-cam-ung.md`, gồm mục "Quyết định đã chốt" (đề xuất 1–3, 5).
- **Nhân vật:** `client/Assets/Game/Client/PlayerCharacter.cs`
  - `:22-24`: `public bool readKeyboard = true;` `public Vector2 MoveInput;` `public bool JumpInput;`
  - `:34`: `public static PlayerCharacter Active { get; private set; }`
  - `:90`: `public ClickMoveState ClickMove { get; private set; }`
  - `:370-380`: `void ReadPointerMove()`, đi-theo-click bằng chuột trái.
  - `:414-419`: nhánh `else if (readKeyboard)`. Đoạn này gán `JumpInput = GameInput.Jump.IsPressed();` rồi gọi `ReadPointerMove();`.
  - `:424-429`: auto-walk bị huỷ khi `HasMoveIntent`. Hướng đi = `Quaternion.Euler(0f, yaw, 0f) * new Vector3(MoveInput.x, 0f, MoveInput.y)`, với yaw lấy từ `follow.yaw`.
  - `:453`: nhánh `else if (ClickMove == ClickMoveState.Walking)` ghi đè `dir`. Joystick vì vậy phải tự huỷ đi-theo-click (bước 5).
- **Camera:** `client/Assets/Game/Client/FollowCamera.cs`
  - `:35-38`: `Start()` gọi `ApplyDefaults(PlayerMotion.CameraPitch, PlayerMotion.CameraDistance)`.
  - `:42`: `public void ApplyDefaults(float pitchDeg, float dist)`.
  - `:73-79`: `public void ApplyLook(Vector2 delta)`. Hàm này nhân với `lookSensitivity` và có `invertY`.
  - `:81-87`: `public void ApplyZoom(float wheel)`.
  - `:92-110`: `LateUpdate`.
- **Cài đặt camera:** `client/Assets/Game/Client/SettingsView.cs:139-151`, `public void ApplyToScene()`. Hàm gọi `follow.ApplyDefaults(s.cameraPitch > 0f ? s.cameraPitch : PlayerMotion.CameraPitch, s.cameraDistance > 0f ? s.cameraDistance : PlayerMotion.CameraDistance);`.
- **Số đọc từ JSON:**
  - `client/Assets/Game/Core/PlayerMotion.cs:28-30` (statics `CameraPitch`…) và `:41-66` (`Configure(PlayerConfig p)`).
  - `client/Assets/Game/Core/Sim/SimConfig.cs:128-172`: `public class PlayerConfig`, trường cuối là `public float dropScatterRadius = 0.15f;`.
  - `client/Assets/Game/Core/Sim/SimConfig.cs:420-431`: kiểm khoảng. Mẫu: `if (!(player.cameraPitch >= 10f && player.cameraPitch <= 85f)) throw new ArgumentException("player.cameraPitch must be in [10, 85].");`
- **Hai file JSON phải giống hệt nhau:** `design/data/sim_m1.json` (mục `"player"`) và `client/Assets/Game/Client/Resources/Config/sim_m1.json`. Test `client/Assets/Game/Tests/World/ConfigTests.cs:26-31` kiểm điều này.
- **Các bảng chặn con trỏ:** `MinimapView.BlocksPointer`, `TimeDial.BlocksPointer`, `SimView.BlocksPointer`, `InventoryView.BlocksPointer`, `ObservationCamera.BlocksPointer`. Tất cả có chữ ký `public static bool BlocksPointer(Vector2 screen)` và đã được dùng ở `PlayerCharacter.cs:376`. Ngoài ra có `MinimapView.IsAnyOpen`, `SettingsView.IsAnyOpen`.
- **Thêm component:** `client/Assets/Game/Client/SimView.cs:108-110` thêm `SettingsView`, `WorldIconsView`, `HabitatPanelView` vào cùng GameObject.
- **Mẫu dựng canvas bằng code:** `client/Assets/Game/Client/InventoryView.cs:728-740` (ScreenSpaceOverlay, `sortingOrder`, font `Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")`). Thứ tự hiện có: WorldIcons 4, TimeDial 9, Minimap 10, Inventory 12.
- **Phím debug:** `client/Assets/Game/Client/DebugKeys.cs`, `DebugKeys.WasPressed(Key key)`. F1–F3 đã dùng; F4 chưa dùng (Planner grep `Key.F4` → không có).
- **Chuỗi hiển thị:** `client/Assets/Game/Client/Resources/Lang/vi.json` (`entries` là mảng `{ "key", "text" }`, xếp theo key). Đọc chữ bằng `GameSettings.Text.Get(key)` (mẫu ở `SimView.cs:447`).
- **Mẫu test PlayMode:** `client/Assets/Game/Tests/Client/CameraPcPlayTests.cs:21-67` (camera + `FollowCamera` + `TerrainChunkView` MapsV3 streaming + `DemoPlain` + `PlayerCharacter` với `readKeyboard = false`, `Time.captureFramerate = 60`).
- **API Input System 1.19 / Unity 6000.4:**
  - `UnityEngine.InputSystem.EnhancedTouch.EnhancedTouchSupport.Enable()` / `.Disable()`. Phải gọi `Enable()` trước khi dùng `Touch` [docs](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.19/manual/Touch.html).
  - `UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches`: mỗi `Touch` có `touchId`, `screenPosition`, `phase` (`UnityEngine.InputSystem.TouchPhase`: Began, Moved, Stationary, Ended, Canceled) — cùng trang docs.
  - `UnityEngine.InputSystem.EnhancedTouch.TouchSimulation.Enable()` / `.Disable()`: giả lập chạm từ chuột — cùng trang docs.
  - `Screen.dpi` trả 0 khi không xác định được [docs](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/Screen-dpi.html).
  - `Screen.safeArea` [docs](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/Screen-safeArea.html).
  - `Application.isMobilePlatform` true trên Android, iOS, WSA [docs](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/Application-isMobilePlatform.html).
  - Quy đổi dp: 1 dp = 1 px ở 160 dpi, tức px = dp × dpi / 160 [docs](https://developer.android.com/training/multiscreen/screendensities).

## Quyết định đã chốt (không được đổi)
1. **Lõi cử chỉ thuần C#:** `Game.Core.TouchGestures`, không tham chiếu UnityEngine (asmdef Game.Core No Engine References). Client chỉ đổi `Touch` → `TouchSample`.
   - Lý do: test EditMode được mà không cần Input System test fixture (repo chưa có `testables`).
2. **Đọc chạm bằng EnhancedTouch, không qua action** (ngoại lệ của 0033).
   - Lý do: joystick và vuốt camera chạy cùng lúc bằng hai ngón, phải theo dõi riêng từng ngón theo `touchId`. Action `<Touchscreen>` gộp ngón nên không tách được.
   - Ghi lý do này trong `<summary>` của `TouchControls`.
3. **Mỗi chạm gán chủ một lần lúc Began**, giữ tới Ended/Canceled. Thứ tự xét:
   1. Trong ô nút → nút.
   2. Đè lên bảng/UI (`OverUi`) → bỏ qua.
   3. Nửa trái (x < width/2) và chưa có joystick → joystick, tâm = điểm Began.
   4. Nửa trái khi đã có joystick → bỏ qua.
   5. Còn lại (nửa phải) → camera.
4. **Joystick:**
   - Vector = độ lệch (dp) ÷ bán kính (dp), kẹp độ dài ≤ 1.
   - Độ dài < vùng chết → (0, 0). Trục y màn hình hướng lên = tiến.
   - Núm vẽ ở tâm + độ lệch đã kẹp theo bán kính.
5. **Camera:**
   - Đúng 1 ngón camera: yaw += dx(dp) × `touchLookDegPerDp`; pitch += −dy(dp) × `touchLookDegPerDp`. Dấu giống chuột: ngón đi lên → pitch giảm.
   - Từ 2 ngón camera trở lên: lấy 2 ngón có `Id` nhỏ nhất, không xoay; `ZoomFactor` = khoảng cách hiện tại ÷ khoảng cách khung trước. Hệ số > 1 (hai ngón tách ra) → camera lại gần: `distance /= factor`.
   - Không áp camera khi `MinimapView.IsAnyOpen`.
6. **dp → px:** `px = dp × dpi / 160`. Khi `dpi ≤ 0` dùng 160.
7. **Số tạm** trong `sim_m1.json` → `player` (thêm cả hai file):

   | Trường | Giá trị | Khoảng hợp lệ |
   |---|---|---|
   | `touchCameraPitch` | 20 | [10, 85] |
   | `touchJoystickRadiusDp` | 60 | [30, 150] |
   | `touchJoystickDeadZone` | 0.15 | [0, 0.5] |
   | `touchLookDegPerDp` | 0.4 | [0.05, 2] |
   | `touchButtonDp` | 64 | [48, 120] (≥ 48 dp theo controls-ui) |

8. **Bật chế độ cảm ứng:** `TouchControls.WantsTouch = Application.isMobilePlatform || TouchControls.ForceOn`.
   - F4 đổi `ForceOn` (mọi build).
   - Trong Editor, khi `ForceOn` bật thì gọi `TouchSimulation.Enable()` (tắt thì `Disable()`), để thử bằng chuột.
   - Khi đang ở chế độ cảm ứng, `PlayerCharacter` **không** gọi `ReadPointerMove()`.
   - Các đường PC khác giữ nguyên.
9. **Camera mặc định khi ở chế độ cảm ứng:** pitch = `PlayerMotion.TouchCameraPitch`, khi người chơi chưa đặt `cameraPitch` trong Cài đặt (= 0). Khoảng cách giữ như PC. Đổi F4 lúc chạy thì gọi lại `SettingsView.ApplyToScene()`.
10. **Bố cục nút Nhảy (px):**
    - Đường kính d = dp→px(`touchButtonDp`). Gọi `s = Screen.safeArea`.
    - Tâm = (`s.xMax − 2.4·d`, `s.yMin + 0.9·d`). Chừa góc dưới-phải cho nút hành động chính của brief 0102.
    - Chữ trên nút: `GameSettings.Text.Get("touch.jump")` = "Nhảy".
11. **Canvas:**
    - "Touch Controls", ScreenSpaceOverlay, `sortingOrder = 8`, **không** CanvasScaler (vẽ theo px).
    - Hình tròn: Sprite tạo lúc chạy từ Texture2D 128×128 (đĩa trắng, mép alpha mềm 2 px), cache tĩnh.
    - Đế joystick: đường kính 2R, trắng alpha 0,25. Núm: đường kính 0,8R, alpha 0,5. Chỉ hiện khi joystick đang hoạt động.
    - Nút Nhảy: alpha 0,35; khi đang giữ thì 0,6.
    - Ẩn cả canvas khi không ở chế độ cảm ứng.

## Phạm vi
- **Được sửa/tạo:**
  - `client/Assets/Game/Core/TouchGestures.cs` (mới)
  - `client/Assets/Game/Core/PlayerMotion.cs`
  - `client/Assets/Game/Core/Sim/SimConfig.cs`
  - `design/data/sim_m1.json` và `client/Assets/Game/Client/Resources/Config/sim_m1.json` (chỉ thêm 5 trường ở mục 7)
  - `client/Assets/Game/Client/TouchControls.cs` (mới)
  - `client/Assets/Game/Client/PlayerCharacter.cs`
  - `client/Assets/Game/Client/FollowCamera.cs`
  - `client/Assets/Game/Client/SettingsView.cs` (chỉ `ApplyToScene`)
  - `client/Assets/Game/Client/SimView.cs` (chỉ `Awake`)
  - `client/Assets/Game/Client/Resources/Lang/vi.json` (thêm 1 key)
  - `client/Assets/Game/Tests/Core/TouchGesturesTests.cs` (mới)
  - `client/Assets/Game/Tests/Client/TouchControlsPlayTests.cs` (mới)
  - Test kiểm khoảng config: thêm vào file test hiện có của `SimConfig` (`client/Assets/Game/Tests/Core/Sim/SimConfigTests.cs`).
  - Các file `.meta` Unity tự tạo.
- **Không được sửa:**
  - `GameControls.inputactions`, `GameInput.cs`
  - `TerrainEditInput.cs`, `InventoryView.cs`, `MinimapView.cs`, `TimeDial.cs`
  - `loc/`, `design/` (trừ `design/data/sim_m1.json`)
  - Test hiện có. Test cũ fail thì dừng, ghi "cần Opus".

## Các bước
1. **Config** — `SimConfig.cs` `PlayerConfig`:
   - Thêm sau `dropScatterRadius`: `public float touchCameraPitch = 20f; public float touchJoystickRadiusDp = 60f; public float touchJoystickDeadZone = 0.15f; public float touchLookDegPerDp = 0.4f; public float touchButtonDp = 64f;`
   - Thêm 5 kiểm khoảng theo mẫu `:420-431`, với khoảng và thông báo `"player.<tên> must be in [a, b]."` của mục 7.
   - Thêm 5 trường vào cả hai `sim_m1.json`, ngay sau `"dropScatterRadius"`, với giá trị ở mục 7. Hai file phải giống hệt nhau.
   - **Xong khi:** biên dịch được; `ConfigTests` đạt.
2. **PlayerMotion** — thêm `public static float TouchCameraPitch { get; private set; } = 20f;` cùng `TouchJoystickRadiusDp` (60), `TouchJoystickDeadZone` (0,15), `TouchLookDegPerDp` (0,4), `TouchButtonDp` (64), mỗi dòng có `/// <summary>` ghi "(task cảm ứng, số tạm)". Gán trong `Configure`.
3. **Lõi** — `Game.Core/TouchGestures.cs`, namespace `Game.Core`. API đúng như sau (tên, kiểu):
   ```csharp
   public enum TouchSamplePhase { Began, Moved, Stationary, Ended }
   public struct TouchSample
   {
       public int Id; public float X, Y; public TouchSamplePhase Phase; public bool OverUi;
       public TouchSample(int id, float x, float y, TouchSamplePhase phase, bool overUi = false)
   }
   public struct TouchRect
   {
       public int Id; public float X, Y, W, H;   // góc dưới-trái, px màn hình (y lên)
       public TouchRect(int id, float x, float y, float w, float h)
       public bool Contains(float px, float py)  // X <= px < X+W && Y <= py < Y+H
   }
   public sealed class TouchGestures
   {
       public TouchGestures(float joystickRadiusDp, float deadZone, float lookDegPerDp)
       public static float DpToPx(float dp, float dpi)            // dpi <= 0 → 160
       public void Update(IReadOnlyList<TouchSample> samples, float screenWidth, float dpi, IReadOnlyList<TouchRect> buttons)
       public float MoveX { get; } public float MoveY { get; }
       public bool JoystickActive { get; }
       public float JoystickCenterX { get; } public float JoystickCenterY { get; }
       public float KnobX { get; } public float KnobY { get; }   // tâm + độ lệch kẹp theo bán kính (px)
       public float JoystickRadiusPx { get; }                     // của lần Update gần nhất
       public float LookYaw { get; } public float LookPitch { get; } // độ, chỉ của khung này
       public float ZoomFactor { get; }                           // 1 = không chụm
       public bool IsHeld(int buttonId)
       public bool WasPressed(int buttonId)                       // Began trong ô nút ở khung này
       public void Reset()                                        // xoá mọi ngón, mọi đầu ra về mặc định
   }
   ```
   - Mỗi `Update`:
     - Đặt `LookYaw = LookPitch = 0`, `ZoomFactor = 1`, xoá `WasPressed`.
     - Xử lý mẫu theo quyết định 3–6. `buttons` chỉ xét lúc Began.
   - Sample có Id chưa biết mà không phải Began → bỏ qua.
   - Ngón đã biết nhưng không có trong `samples` của khung này → coi như Ended (thả).
   - Joystick thả → `MoveX = MoveY = 0`, `JoystickActive = false`.
   - Vị trí cuối của mọi ngón camera cập nhật mỗi khung, để chuyển 2 ngón → 1 ngón không bị giật.
   - Chụm chỉ tính khi cả 2 ngón đã có vị trí ở khung trước và khoảng cách khung trước > 1 px.
   - Không cấp phát mỗi khung (dùng Dictionary/List dựng sẵn).
   - **Xong khi:** test EditMode ở bước 9 đạt.
4. **FollowCamera:**
   - Thêm `public void ApplyLookDegrees(float yawDeg, float pitchDeg)`: góc nhìn thứ nhất → không làm gì; `yaw += yawDeg; pitch = Mathf.Clamp(pitch + pitchDeg * (invertY ? -1f : 1f), minPitch, maxPitch);`.
   - Thêm `public void ApplyZoomFactor(float factor)`: góc nhìn thứ nhất hoặc `factor <= 0` → không làm gì; `distance = Mathf.Clamp(distance / factor, minDistance, maxDistance);`.
   - `Start()`: pitch mặc định = `TouchControls.WantsTouch ? PlayerMotion.TouchCameraPitch : PlayerMotion.CameraPitch`.
   - Không đổi phần còn lại.
5. **PlayerCharacter:**
   - Thêm `public bool TouchJump;`.
   - Trong nhánh `readKeyboard` (`:414-419`): `JumpInput = GameInput.Jump.IsPressed() || TouchJump;`; gọi `ReadPointerMove()` chỉ khi `!TouchControls.WantsTouch`.
   - Ngay sau khối `if (FlyMode || …) … else if (readKeyboard) {…}`, thêm: `if (MoveInput.sqrMagnitude > 0.01f && ClickMove == ClickMoveState.Walking) ClickMove = ClickMoveState.None;` (joystick huỷ đi-theo-click).
   - Không đổi phần khác.
6. **SettingsView.ApplyToScene** — thay `PlayerMotion.CameraPitch` trong lời gọi `ApplyDefaults` bằng `(TouchControls.WantsTouch ? PlayerMotion.TouchCameraPitch : PlayerMotion.CameraPitch)`.
7. **TouchControls** — `client/Assets/Game/Client/TouchControls.cs`, namespace `Game.Client`, `[DefaultExecutionOrder(-50)] public class TouchControls : MonoBehaviour`:
   - **Thành viên:**
     - `public static bool ForceOn;`
     - `public static bool WantsTouch => Application.isMobilePlatform || ForceOn;`
     - `public static bool Active => instance != null && instance.isActiveAndEnabled && WantsTouch;`
     - `public bool readTouches = true;`: test đặt false rồi tự gọi `Feed`.
     - `public const int JumpButtonId = 1;`
     - `public TouchGestures Gestures { get; private set; }`
     - `public RectTransform JumpButton { get; private set; }`
     - `public static void SetForceOn(bool on)`
   - **Vòng đời:**
     - `OnEnable`: gán `instance = this`, gọi `EnhancedTouchSupport.Enable()`.
     - `OnDisable`: gọi `EnhancedTouchSupport.Disable()`; nếu `instance == this` thì `instance = null`; trả `MoveInput`/`TouchJump` của nhân vật về 0/false.
     - `Gestures` tạo trong `Awake` từ các số `PlayerMotion.Touch*`.
   - **`SetForceOn(on)`:**
     - Gán `ForceOn`.
     - Trong Editor (`#if UNITY_EDITOR`): `on ? TouchSimulation.Enable() : TouchSimulation.Disable()`.
     - Gọi `ApplyToScene()` của `SettingsView` trên cùng GameObject với `instance` nếu có. Không có thì `Camera.main?.GetComponent<FollowCamera>()?.ApplyDefaults(WantsTouch ? PlayerMotion.TouchCameraPitch : PlayerMotion.CameraPitch, PlayerMotion.CameraDistance)`.
   - **`Update()`:**
     - `if (DebugKeys.WasPressed(Key.F4)) SetForceOn(!ForceOn);`
     - Chưa `Active` → ẩn canvas. Nếu khung trước đang `Active` thì `Gestures.Reset()` và trả `MoveInput`/`TouchJump` về 0/false. Rồi `return`.
     - Đã `Active` và `readTouches` → dựng danh sách `TouchSample` từ `Touch.activeTouches`:
       - `Began`/`Moved`/`Stationary` giữ nguyên; `Ended`/`Canceled` → `Ended`; các phase khác bỏ qua.
       - `OverUi` = bất kỳ `BlocksPointer` nào trong 5 hàm ở mục Bối cảnh trả true, hoặc `SettingsView.IsAnyOpen`.
     - Rồi gọi `Feed(list, Screen.width, Screen.dpi)`.
   - **`public void Feed(IReadOnlyList<TouchSample> samples, float screenWidth, float dpi)`:**
     - Không `Active` → return.
     - Cập nhật bố cục; dựng `TouchRect` của nút Nhảy theo quyết định 10, hình vuông bao đường tròn.
     - `Gestures.Update(...)`.
     - Áp lên `PlayerCharacter.Active` (nếu có): `MoveInput = new Vector2(MoveX, MoveY)`, `TouchJump = IsHeld(JumpButtonId)`.
     - Áp lên `FollowCamera` của `Camera.main`, chỉ khi `!MinimapView.IsAnyOpen`: `LookYaw`/`LookPitch` khác 0 → `ApplyLookDegrees`; `ZoomFactor != 1` → `ApplyZoomFactor`.
     - Cập nhật hình: đế/núm theo `JoystickCenter`/`Knob`/`JoystickRadiusPx`; độ đục nút theo trạng thái giữ.
   - **Canvas:** dựng theo quyết định 11, lần đầu khi `Active`. Nút Nhảy có `Text` chữ "touch.jump", font `LegacyRuntime.ttf`, cỡ ≈ 0,3·d.
   - `<summary>` của lớp ghi: task cảm ứng (a), brief 0101, lý do dùng EnhancedTouch (quyết định 2), F4.
8. **SimView.Awake** (`:108-110`) — thêm `if (GetComponent<TouchControls>() == null) gameObject.AddComponent<TouchControls>();`.
9. **Chuỗi** — `vi.json`: thêm `{ "key": "touch.jump", "text": "Nhảy" }`, đúng thứ tự key.
10. **Test** — viết các test ở "Phần cần test".

## Tiêu chí xong
- Biên dịch sạch, console không lỗi.
- Toàn bộ EditMode và PlayMode đạt, gồm test mới; số test tăng đúng bằng số test thêm.
- Với `ForceOn = false` trong Editor (không phải mobile): `TouchControls.Active == false`, canvas ẩn, test PC cũ (`CameraPcPlayTests`, `ClickMovePlayTests`, …) đạt nguyên.
- Hai `sim_m1.json` giống hệt nhau (`ConfigTests`).

## Test sau khi xong

### Phần cần test
**EditMode** — `TouchGesturesTests` (màn hình rộng 1000 px, dpi 320 nên 1 dp = 2 px, bán kính 60 dp = 120 px, vùng chết 0,15, 0,4°/dp):

| # | Thao tác | Mong đợi |
|---|---|---|
| 1 | Began (200, 300) | `JoystickActive`, tâm (200, 300), Move (0, 0) |
| 2 | Tiếp test 1: Moved (260, 300) | MoveX = 0,5 (±1e-4), MoveY = 0 |
| 3 | Moved (200, 540) (lệch 240 px = 2R) | Move (0, 1); Knob (200, 420) |
| 4 | Lệch 10 px (< 0,15·R) | Move (0, 0) |
| 5 | Ended | Move (0, 0), `JoystickActive` false |
| 6 | Ngón camera Began (700, 300) rồi Moved (800, 300) | `LookYaw` = 20 (100 px = 50 dp × 0,4); `LookPitch` = 0; khung kế không di chuyển → `LookYaw` = 0 |
| 7 | Ngón camera Moved lên (700, 400) | `LookPitch` = −20 |
| 8 | Hai ngón camera, khoảng cách 100 → 200 px | `ZoomFactor` = 2, `LookYaw` = 0 |
| 9 | Began trong `TouchRect` id 1 ở nửa trái | `IsHeld(1)` và `WasPressed(1)`; joystick không bật; khung sau `WasPressed(1)` false, `IsHeld(1)` vẫn true; Ended → `IsHeld(1)` false |
| 10 | Began với `OverUi = true` ở nửa trái và nửa phải, rồi Moved | Không joystick, `LookYaw` = 0 |
| 11 | Đang có joystick, ngón thứ hai Began ở nửa trái rồi Moved | Bị bỏ qua: `LookYaw` = 0, joystick giữ tâm cũ |
| 12 | Ngón joystick biến mất khỏi `samples`, không có Ended | Thả: `JoystickActive` false |
| 13 | `DpToPx(48, 0)` / `DpToPx(48, 320)` | 48 / 96 |
| 14 | `Reset()` | Mọi đầu ra về mặc định |

**EditMode** — `SimConfigTests`:
- 5 trường mới đọc đúng giá trị từ `design/data/sim_m1.json`.
- Mỗi trường ra ngoài khoảng (ví dụ `touchButtonDp` = 40) → `ArgumentException` đúng thông báo. Dùng `[TestCase]`.

**PlayMode** — `TouchControlsPlayTests`:
- Dựng theo `CameraPcPlayTests.cs:21-67`. Thêm GameObject có `TouchControls` (`readTouches = false`). Gọi `TouchControls.SetForceOn(true)` trong SetUp, `SetForceOn(false)` trong TearDown.
- Gọi `Feed(..., Screen.width, 160f)` mỗi khung để dp = px.

| # | Thao tác | Mong đợi |
|---|---|---|
| 1 | Joystick đi tới: Began ở (0,25·W, 0,3·H), mỗi khung Moved lệch lên đúng R px, trong 60 khung | Nhân vật đi > 1 ô theo hướng `Quaternion.Euler(0, follow.yaw, 0) * Vector3.forward` (tích vô hướng độ dời ngang với hướng đó > 1). Sau Ended, 10 khung sau: độ dời ngang < 0,05 |
| 2 | `player.MoveTo` tới đích cách 10 ô (`ClickMove == Walking`), rồi joystick lệch | `ClickMove != Walking` |
| 3 | Vuốt nửa phải 100 px sang phải trong 1 khung | `follow.yaw` tăng đúng 100 × `PlayerMotion.TouchLookDegPerDp` (±0,01) |
| 4 | Gọi `Feed` với danh sách rỗng một lần để dựng bố cục. Lấy tâm nút Nhảy = `JumpButton.position` (canvas overlay không scaler nên đây là px màn hình). Vuốt bắt đầu ở đó rồi Moved 100 px | `follow.yaw` không đổi; `player.TouchJump` true khi giữ; nhân vật nhảy (y chân tăng > 0,3 trong 30 khung) |
| 5 | Chụm 2 ngón nửa phải, 100 → 200 px | `follow.distance` giảm còn ≈ 1/2 (kẹp `minDistance`) |
| 6 | Mặc định camera | Khi `ForceOn` bật, `follow.defaultPitch == PlayerMotion.TouchCameraPitch`; `SetForceOn(false)` → `== PlayerMotion.CameraPitch` (Cài đặt mặc định `cameraPitch` = 0) |
| 7 | `SetForceOn(false)` rồi `Feed` joystick | `TouchControls.Active` false, `player.MoveInput == Vector2.zero`, canvas ẩn |
| 8 | Bố cục | `JumpButton` nằm trong `Screen.safeArea`; cạnh ≥ `DpToPx(48, dpi)` |

**Regression:**
- Toàn bộ EditMode + PlayMode hiện có, đặc biệt `CameraPcPlayTests`, `ClickMovePlayTests`, `ControlModePlayTests`, `SettingsPlayTests`, `ConfigTests`, `SimConfigTests`.
- Mã băm world/sim không đổi (test mã băm hiện có đạt).

### Lệnh executor chạy
1. Ghi giờ bắt đầu (`date "+%H:%M"`).
2. Unity Editor (MCP): `refresh_unity`, rồi `read_console` (Error): không lỗi biên dịch.
3. MCP `run_tests` EditMode (toàn bộ) → ghi dòng tổng kết (đạt/tổng).
4. MCP `run_tests` PlayMode (toàn bộ) → ghi dòng tổng kết.
5. Dọn: `git checkout -- client/ProjectSettings/EditorSettings.asset`.
6. `python3 tools/check_design.py` → exit 0.
7. `git diff --stat` chỉ gồm file trong Phạm vi. Commit code: `brief 0101: touch mode, virtual joystick, swipe/pinch camera, jump button` (kèm dòng Co-Authored-By theo AGENTS.md §11).
8. Ghi giờ kết thúc vào mục Kết quả.

### User tự test
Không ở brief này. User thử cảm giác trên điện thoại gộp ở brief 0103.

Nếu muốn xem sớm trong Editor: mở SimView, Play, bấm **F4**, rồi dùng chuột thay ngón tay:
- kéo nửa trái → joystick hiện ở chỗ bấm, nhân vật đi;
- kéo nửa phải → camera xoay;
- bấm nút "Nhảy" góc dưới-phải → nhân vật nhảy.

Chụm hai ngón không thử được bằng chuột.

## Dừng và chuyển lại Opus khi
- Brief mơ hồ hoặc mâu thuẫn với code thật (ví dụ dòng trích dẫn đã dời, hay `Touch.activeTouches` / `TouchSimulation` khác chữ ký trong 1.19).
- Cần một quyết định thuộc AGENTS.md §3.4 hoặc ngoài "Quyết định đã chốt".
- Cùng một lỗi sửa 2 lần vẫn chưa qua.
- Phải sửa file ngoài "Phạm vi", hoặc test cũ fail.
- F4 hoá ra đã dùng ở đâu đó.

---
<!-- Từ đây trở xuống: EXECUTOR ghi. -->

## Kết quả

## Kết quả test

## Vướng mắc

---
<!-- PLANNER ghi khi review. -->

## Review
