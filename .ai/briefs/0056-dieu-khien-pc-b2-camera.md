# Brief 0056 — điều khiển PC B2: camera kiểu LoL, Space về mặc định, X góc nhìn thứ nhất, Cài đặt góc/khoảng cách

> Trạng thái: chờ review
> Review: đầy đủ + user xem
> Model thực thi: Sonnet · medium
> Cập nhật: 2026-10-23

<!-- Planner (Opus) viết, Executor (Sonnet) làm theo ở PHIÊN KHÁC — executor không có lịch sử chat, không được tự quyết. -->

## Mục tiêu
Phần **camera** của bước world **7l b** ([task contract](../tasks/m1-world-7l-dieu-khien-pc.md), quyết định [0034](../../design/decisions/0034-dieu-khien-pc-click-di-chuyen.md) mục 2):
- mặc định nhìn chéo từ trên cao (55°, 14 ô), luôn bám nhân vật, hướng không tự xoay theo nhân vật;
- giữ chuột giữa xoay, lăn chuột thu phóng (như cũ);
- **Space** = camera về mặc định sau lưng nhân vật;
- **X** = bật/tắt góc nhìn thứ nhất (camera ở mắt, theo hướng nhân vật, không xoay tự do);
- Cài đặt chỉnh góc cúi + khoảng cách mặc định.

**Làm sau brief 0055** (cần `PlayerMotion.CameraPitch/CameraDistance/FirstPersonPitch`, Space đã rời `Jump`). Bắt đầu: kiểm brief 0055 có trạng thái `đã duyệt` hoặc `chờ review` và commit của nó có trong `git log`; không có → dừng, `cần Opus`.

## Bối cảnh cần đọc
- `client/Assets/Game/Client/FollowCamera.cs` (66 dòng) — đọc hết:
  - `:13–18` trường `distance = 6f, minDistance = 2.5f, maxDistance = 14f; pitch = 20f, minPitch = -20f, maxPitch = 70f; yaw; lookSensitivity; invertY; pivotHeight = 1.6f, castRadius = 0.2f`;
  - `:23` `public void SetAngles(float yawDeg, float pitchDeg)`;
  - `:29–51` `LateUpdate()` (xoay `:34–39`, thu phóng `:40–48`, gọi `SnapNow()`);
  - `:54–64` `public void SnapNow()` (SphereCast chống xuyên khối).
- `client/Assets/Game/Client/PlayerCharacter.cs` (sau brief 0055): `Start()` gọi `follow.SetAngles(pendingYaw, 20f)`; `Place()` gọi `follow.SetAngles(yawDeg, 20f)`; `SetFlyMode(bool on)`; `Visual` (Transform, các khối con có Renderer); `BuildVisual()`.
- `client/Assets/Game/Core/PlayerSettings.cs` (57 dòng) — đọc hết: hằng `CameraDistanceMin = 2.5f, CameraDistanceMax = 14f` (`:9`); `formatVersion = 1` (`:14`); `cameraDistance = 6f` (`:15`); `Clamp()` `:22–29`; `Clone()` `:31–40`; `ClampF` `:51–55`.
- `client/Assets/Game/Client/SettingsView.cs`:
  - `:11–16` mảng `Keys`;
  - `:31` `public static Rect PanelRect(float gw, float gh) => new Rect(gw * 0.5f - 230f, gh * 0.5f - 210f, 460f, 420f);`;
  - `:49–53` `Start()` gọi `ApplyToScene()` (lớp có `[DefaultExecutionOrder(-50)]` → chạy trước `FollowCamera.Start`);
  - `:91–112` `ApplyToScene()` — `:100` `follow.distance = Mathf.Clamp(s.cameraDistance, follow.minDistance, follow.maxDistance);`;
  - `:137–140` hàng "Khoảng cách camera" (label + `GUI.HorizontalSlider`).
- `client/Assets/Game/Client/GameSettings.cs:50–66` `Load()` — đọc JSON rồi `s.Clamp()`.
- `client/Assets/Game/Core/PlayerMotion.cs` (brief 0055): `PlayerMotion.CameraPitch` (55), `CameraDistance` (14), `FirstPersonPitch` (15).
- `client/Assets/Game/Client/ObservationCamera.cs:134–173` — khi xem camera quan sát thì `follow.enabled = false`, quay về thì bật lại + `SnapNow()`.
- Input: `GameControls.inputactions`, `GameInput.cs`, `GameControlsTests.cs` như brief 0055 (sau 0055: 31 action, `Space` và `X` chưa dùng trong map `Player`).
- Test: `Tests/Core/PlayerSettingsTests.cs` (dòng có `6f` / `14f`: `:13`, `:35`, `:48`, `:76`, `:87`; `formatVersion` `:12`); `Tests/Client/SettingsPlayTests.cs:203–220` `Apply_ReachesScene`; mẫu dựng cảnh `Tests/Client/PlayerCharacterPlayTests.cs:22–103`.
- Chuỗi: `client/Assets/Game/Client/Resources/Lang/vi.json` (khoá `settings.*` xếp theo ABC, `:482–533`).
- Docs API (Unity 6000.4 / Input System 1.19) [docs]:
  - [Renderer.enabled](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/Renderer-enabled.html)
  - [InputAction](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.19/api/UnityEngine.InputSystem.InputAction.html) — `WasPressedThisFrame()`
  - `MonoBehaviour.OnEnable/OnDisable`, `Quaternion.Euler`, `Transform.SetPositionAndRotation`, `GUI.HorizontalSlider`, `GUI.Button` — đã dùng trong repo (`FollowCamera.cs`, `SettingsView.cs`), dùng y như vậy.

## Quyết định đã chốt (không được đổi)
- 0034 mục 2 (user 2026-10-23): mặc định nhìn chéo từ trên cao kiểu LoL, luôn bám nhân vật, không kéo màn hình theo mép; góc cúi + khoảng cách mặc định [số tạm] 55° / 14 ô, chỉnh trong Cài đặt (0032); giữ chuột giữa + di chuột xoay (ngang + dọc), lăn chuột thu phóng; **Space** → camera về mặc định: sau lưng, trên đầu nhân vật, theo hướng nhân vật đang nhìn; **X** bật/tắt góc nhìn thứ nhất: camera trước mắt, gắn theo hướng nhân vật, không xoay tự do; click trái vẫn để đi.
- Task contract: số mặc định ở `sim_m1.json` (đã thêm ở 0055); số cảm giác camera qua Cài đặt.
- Planner (brief này):
  - Cài đặt lưu **giá trị riêng của người chơi**; `0` = "theo mặc định của game" (`sim_m1.json`). `formatVersion` 1 → 2; file cũ (v1) → `cameraDistance` về 0 (khoảng cách 6 cũ là của camera sau lưng cũ), `cameraPitch` 0.
  - Khoảng thu phóng 2,5–24 ô (trước: 2,5–14, vì mặc định giờ là 14). Góc cúi chỉnh trong Cài đặt 10°–85°. Xoay bằng chuột giữa: pitch −20°…85°.
  - Góc nhìn thứ nhất: mắt cao 1,5 ô trên chân, lệch 0,25 ô về phía trước theo hướng mặt; góc cúi cố định `FirstPersonPitch` (15°); ẩn thân nhân vật; lăn chuột không thu phóng; chuột giữa không xoay. Tắt X → trả yaw/pitch như trước khi bật.
  - Space khi đang góc nhìn thứ nhất → thoát góc nhìn thứ nhất rồi về mặc định.
  - Bật camera bay (F3) → thoát góc nhìn thứ nhất. Khi `FollowCamera` bị tắt (xem camera quan sát) → hiện lại thân; bật lại mà vẫn góc nhìn thứ nhất → ẩn lại.
  - Space / X bị bỏ qua khi bản đồ hoặc Cài đặt đang mở.
- Ngoài phạm vi: di chuyển (0055); chế độ, HUD, đổi phím (7l c–f); cảm ứng.

## Phạm vi
- Được sửa/tạo:
  - `client/Assets/Game/Client/FollowCamera.cs`, `client/Assets/Game/Client/PlayerCharacter.cs` (chỉ các chỗ ở bước 3)
  - `client/Assets/Game/Core/PlayerSettings.cs`, `client/Assets/Game/Client/SettingsView.cs`
  - `client/Assets/Game/Client/Input/GameControls.inputactions`, `client/Assets/Game/Client/GameInput.cs`
  - `client/Assets/Game/Client/Resources/Lang/vi.json` (2 khoá mới + `hud.help_walk`)
  - Test: `Tests/Core/PlayerSettingsTests.cs`, `Tests/ClientEdit/GameControlsTests.cs`, `Tests/Client/SettingsPlayTests.cs` (chỉ `Apply_ReachesScene`), `Tests/Client/CameraPcPlayTests.cs` (mới) + `.meta`
  - Test cũ hỏng **chỉ vì** giả định camera mặc định pitch 20° / khoảng cách 6 / max 14: được thêm `follow.SetAngles(<yaw cũ>, 20f)` hoặc `follow.distance = 6f` ở đầu test đó; ghi tên test vào Kết quả.
  - File brief này (mục Kết quả trở xuống)
- Không được sửa: `design/` (kể cả `sim_m1.json`), `PlayerMotion.cs`, `ObservationCamera.cs`, `TerrainEditInput.cs`, `client/Assets/Game/Tests/Client/TerrainEditPlayTests.cs` (thay đổi dở của phiên khác — để nguyên, không commit).

## Các bước
1. **Phím.** `GameControls.inputactions` map `Player`: thêm action `CameraReset` và `ToggleFirstPerson` (cuối danh sách action, `"type": "Button"`, `"expectedControlType": "Button"`, `"initialStateCheck": false`), binding `<Keyboard>/space` và `<Keyboard>/x`; `"id"` mới sinh bằng `python3 -c "import uuid;print(uuid.uuid4())"`. `GameInput.cs`: thêm `public static InputAction CameraReset => Get("Player/CameraReset");` và `public static InputAction ToggleFirstPerson => Get("Player/ToggleFirstPerson");`. `GameControlsTests`: `Expected()` thêm `["Player/CameraReset"] = B("<Keyboard>/space")`, `["Player/ToggleFirstPerson"] = B("<Keyboard>/x")`; hai số `31` → `33`.
   - Xong khi: `GameControlsTests` qua.

2. **`PlayerSettings.cs`.**
   - `:9`: `CameraDistanceMax = 14f` → `24f`. Thêm `public const float CameraPitchMin = 10f, CameraPitchMax = 85f;`.
   - `formatVersion = 2`; `cameraDistance = 0f`; thêm `public float cameraPitch = 0f;` ngay sau `cameraDistance`. Doc hai trường: `/// <summary>0 = theo mặc định của game (sim_m1.json player.cameraDistance / cameraPitch).</summary>`.
   - `Clamp()`: dòng đầu `if (formatVersion < 2) { cameraDistance = 0f; cameraPitch = 0f; formatVersion = 2; }`; thay dòng clamp `cameraDistance` bằng `cameraDistance = ClampOptional(cameraDistance, CameraDistanceMin, CameraDistanceMax);` và thêm `cameraPitch = ClampOptional(cameraPitch, CameraPitchMin, CameraPitchMax);`. Hàm mới `static float ClampOptional(float v, float min, float max)`: NaN / vô cực / ≤ 0 → 0; còn lại kẹp [min, max].
   - `Clone()`: thêm `cameraPitch = cameraPitch,`.
   - `PlayerSettingsTests`: `:12` → 2; `:13` → `0f` và thêm `Assert.AreEqual(0f, s.cameraPitch);`; `:35` `14f` → `24f`; `:48` `6f` → `0f` (thêm `cameraPitch = bad` vào khởi tạo `:46` và assert `0f`); `:76` và `:87` `6f` → `0f`. Thêm test ở bảng dòng 2.
   - Xong khi: `PlayerSettingsTests` qua.

3. **`FollowCamera.cs` + `PlayerCharacter.cs`.**
   - `FollowCamera` trường: `distance = 14f, minDistance = 2.5f, maxDistance = 24f`; `pitch = 55f, minPitch = -20f, maxPitch = 85f`; thêm `public float defaultPitch = 55f, defaultDistance = 14f, firstPersonPitch = 15f, eyeHeight = 1.5f, eyeForward = 0.25f;`, `public bool FirstPerson { get; private set; }`, riêng `bool defaultsApplied; float savedYaw, savedPitch;`.
   - Hàm mới / đổi:
     ```csharp
     void Start() { if (!defaultsApplied) ApplyDefaults(PlayerMotion.CameraPitch, PlayerMotion.CameraDistance); firstPersonPitch = PlayerMotion.FirstPersonPitch; }

     /// <summary>Góc cúi + khoảng cách mặc định (Space về đây). Cài đặt gọi; không gọi thì Start lấy từ sim_m1.json. Kẹp theo min/max; chỉ đổi góc/khoảng cách đang dùng khi mặc định thật sự đổi hoặc lần đầu.</summary>
     public void ApplyDefaults(float pitchDeg, float dist)
     // p = Clamp(pitchDeg, minPitch, maxPitch); d = Clamp(dist, minDistance, maxDistance);
     // bool changed = !defaultsApplied || p != defaultPitch || d != defaultDistance;
     // defaultPitch = p; defaultDistance = d; defaultsApplied = true; if (changed) { pitch = p; distance = d; }

     /// <summary>Space: thoát góc nhìn thứ nhất, yaw = hướng mặt nhân vật, pitch/distance = mặc định, SnapNow.</summary>
     public void ResetBehind()

     /// <summary>X. Bật: lưu yaw/pitch, ẩn thân. Tắt: trả yaw/pitch đã lưu, hiện thân. Cùng trạng thái → không làm gì. SnapNow.</summary>
     public void SetFirstPerson(bool on)

     /// <summary>Chuột giữa + di chuột: như :37–38 cũ; FirstPerson → không làm gì.</summary>
     public void ApplyLook(Vector2 delta)

     /// <summary>Lăn chuột: wheel > 0,01 → distance × 0,9; < −0,01 → × 1,1; kẹp; FirstPerson → không làm gì.</summary>
     public void ApplyZoom(float wheel)

     void OnEnable()  { if (FirstPerson && target != null) target.SetBodyVisible(false); }
     void OnDisable() { if (target != null) target.SetBodyVisible(true); }
     ```
   - `LateUpdate()`: thay `:37–38` bằng `ApplyLook(d)`, `:44–47` bằng `ApplyZoom(wheel)` (giữ mọi điều kiện bao quanh). Trước `SnapNow();` thêm:
     ```csharp
     if (!MinimapView.IsAnyOpen && !SettingsView.IsAnyOpen)
     {
         if (GameInput.CameraReset.WasPressedThisFrame()) ResetBehind();
         else if (GameInput.ToggleFirstPerson.WasPressedThisFrame()) SetFirstPerson(!FirstPerson);
     }
     ```
   - `SnapNow()`: đầu hàm sau `if (target == null) return;` thêm nhánh `FirstPerson`: `float fy = target.FacingYaw;` vị trí = `target.transform.position + Vector3.up * eyeHeight + Quaternion.Euler(0f, fy, 0f) * Vector3.forward * eyeForward`, xoay = `Quaternion.Euler(firstPersonPitch, fy, 0f)`, `CurrentDistance = 0f`, `SetPositionAndRotation`, `return`.
   - Doc lớp `:5–8`: "Camera PC (0034, world 7l b): mặc định chéo từ trên cao (sim_m1.json / Cài đặt), bám nhân vật; giữ chuột giữa xoay, lăn chuột thu phóng (Alt + lăn là chọn loại khối); Space về sau lưng; X góc nhìn thứ nhất. SphereCast chống xuyên khối."
   - `PlayerCharacter`: thêm `public float FacingYaw => Visual.eulerAngles.y;`, `public bool BodyVisible { get; private set; } = true;`, `public void SetBodyVisible(bool on)` (gán `enabled` cho mọi `Renderer` trong `Visual.GetComponentsInChildren<Renderer>(true)`, lưu `BodyVisible`). `Start()` và `Place()`: `SetAngles(…, 20f)` → `SetAngles(…, follow.defaultPitch)`. `SetFlyMode`: khi `on && follow != null && follow.FirstPerson` → `follow.SetFirstPerson(false)` trước khi tắt `follow`.
   - Xong khi: biên dịch không lỗi; `PlayerCharacterPlayTests` qua.

4. **`SettingsView.cs`.**
   - `Keys`: thêm `"settings.camera_pitch", "settings.camera_default"`.
   - `PanelRect`: `gh * 0.5f - 210f` → `gh * 0.5f - 240f`, cao `420f` → `480f`.
   - `ApplyToScene()` `:100`: thay bằng
     ```csharp
     follow.ApplyDefaults(s.cameraPitch > 0f ? s.cameraPitch : PlayerMotion.CameraPitch,
                          s.cameraDistance > 0f ? s.cameraDistance : PlayerMotion.CameraDistance);
     ```
   - Hàng khoảng cách (`:137–140`): giá trị hiển thị + slider dùng `float effD = s.cameraDistance > 0f ? s.cameraDistance : PlayerMotion.CameraDistance;`; `if (v != effD) { s.cameraDistance = v; changed = true; }`. Ngay sau, hàng góc cúi giống hệt với `effP` (`cameraPitch` / `PlayerMotion.CameraPitch`), khoá `settings.camera_pitch`, slider `[PlayerSettings.CameraPitchMin, PlayerSettings.CameraPitchMax]`. Rồi một hàng nút `GUI.Button(new Rect(x, y, 200f, row - 4f), T("settings.camera_default"))` → `s.cameraDistance = 0f; s.cameraPitch = 0f; changed = true;`. Mỗi hàng `y += row;`.
   - `vi.json` (giữ thứ tự ABC trong nhóm `settings.*`): thêm `{ "key": "settings.camera_default", "text": "Camera về mặc định" }` trước `settings.camera_distance`, và `{ "key": "settings.camera_pitch", "text": "Góc cúi camera: {0:F0}°" }` sau `settings.camera_distance`. Đổi `hud.help_walk` thành nguyên văn:
     ` · Chuột trái: đi tới chỗ click (giữ để đi theo con trỏ) · A: nhảy · Shift: lướt · Chuột phải: dùng công cụ (trong {0:0.#} ô quanh nhân vật) · F: thu vật rơi (trong {1:0.#} ô) · R: phép Mưa (trong {0:0.#} ô) · B: túi · chuột phải lên cây chín: thu hoạch · 1–4 / Alt + lăn chuột: chọn loại · giữ chuột giữa: xoay · lăn chuột: gần/xa · Space: camera về sau lưng · X: góc nhìn thứ nhất · F3: camera bay\n`
   - `SettingsPlayTests.Apply_ReachesScene`: thêm `s.cameraPitch = 40f;` vào dòng gán; thêm assert `follow.pitch == 40f`, `follow.defaultPitch == 40f`, `follow.defaultDistance == 10f`; sau đó gán `s2 = GameSettings.Current.Clone(); s2.cameraDistance = 0f; s2.cameraPitch = 0f; GameSettings.Apply(s2);` → `follow.distance == PlayerMotion.CameraDistance`, `follow.pitch == PlayerMotion.CameraPitch`.
   - Xong khi: `python3 -m json.tool` vi.json exit 0; `SettingsPlayTests` qua.

5. **Test PlayMode `Tests/Client/CameraPcPlayTests.cs`** (namespace `Game.Client.Tests`): `SetUp`/`TearDown`/`Run`/`Teleport`/`Build` chép từ `PlayerCharacterPlayTests.cs:22–103`. Các test ở bảng dòng 4–10.

6. **Commit** sau khi mọi test qua: file trong Phạm vi + `.meta` mới + brief; **không** gồm `TerrainEditPlayTests.cs`. Message: `brief 0056: PC camera — high default, Space reset, X first person, settings (7l b2)` + dòng `Co-Authored-By` theo hướng dẫn phiên.

## Tiêu chí xong
- Biên dịch không lỗi; `read_console` 0 lỗi sau test.
- EditMode + PlayMode toàn bộ 0 fail (số = sau brief 0055 + test mới).
- Mọi dòng "Phần cần test" có kết quả thật; danh sách test cũ phải sửa vì giả định camera (nếu có) ghi trong Kết quả.

## Test sau khi xong

### Phần cần test
| # | Phần (bước) | Cách test | Kết quả mong đợi |
|---|---|---|---|
| 1 | Phím (1) | `GameControlsTests` (4 test), `GameControlsPlayTests` | 33 action; `CameraReset` = space, `ToggleFirstPerson` = x; không trùng phím trong map `Player` |
| 2 | Cài đặt: mặc định, kẹp, chuyển v1 (2) | `PlayerSettingsTests` (sửa) + `PlayerSettingsTests.MigratesV1AndOptionalCamera` (mới) | mặc định `formatVersion` 2, `cameraDistance` 0, `cameraPitch` 0; kẹp: distance 1 → 2,5, 99 → 24; pitch 5 → 10, 99 → 85; −3 / NaN → 0; `{"formatVersion":1,"cameraDistance":6}` → sau Clamp: version 2, distance 0, pitch 0; `{"formatVersion":2,"cameraDistance":10,"cameraPitch":40}` → giữ 10 / 40; Clone chép `cameraPitch` |
| 3 | Cài đặt → cảnh (4) | `SettingsPlayTests.Apply_ReachesScene` (sửa) | distance 10, pitch 40, default 40/10; về 0/0 → distance 14, pitch 55 (= `PlayerMotion`) |
| 4 | Mặc định kiểu LoL (3) | `CameraPcPlayTests.DefaultHighAngleFollows`: sau SetUp, Teleport A; `player.MoveTo(A + (4,5; 0; 0,5))`, chờ 1,5 giây | trước khi đi: `follow.pitch` 55, `follow.distance` 14, `defaultPitch` 55, `defaultDistance` 14; sau khi đi: `follow.yaw` không đổi; khoảng cách camera → pivot (chân + 1,6) ≤ 14,01 |
| 5 | Space → sau lưng (3) | `CameraPcPlayTests.ResetBehindUsesFacing`: Teleport; `follow.SetAngles(200, 10)`; `follow.distance = 5`; `player.Visual.rotation = Quaternion.Euler(0, 90, 0)`; `follow.ResetBehind()` | yaw 90, pitch 55, distance 14; vector camera − pivot chiếu ngang chuẩn hoá có dot với (−1, 0, 0) > 0,9; camera y > pivot y |
| 6 | X → mắt, không xoay, không thu phóng (3) | `CameraPcPlayTests.FirstPersonAtEyes`: Teleport; `Visual.rotation = Euler(0, 90, 0)`; `follow.SetFirstPerson(true)`; một frame; ghi pose; `follow.ApplyLook(new Vector2(100, 50))`; `follow.ApplyZoom(1)`; một frame | vị trí camera ≈ chân + (0,25; 1,5; 0) (±0,05); euler camera ≈ (15, 90, 0) (±0,5); `player.BodyVisible` false; sau ApplyLook/ApplyZoom pose không đổi (±0,01), `follow.yaw`/`pitch`/`distance` không đổi |
| 7 | X tắt trả góc; Space thoát góc thứ nhất (3) | `CameraPcPlayTests.ToggleAndResetFromFirstPerson`: `SetAngles(30, 40)`; X bật rồi tắt; rồi X bật, `ResetBehind()` | sau tắt: yaw 30, pitch 40, `BodyVisible` true; sau ResetBehind: `FirstPerson` false, `BodyVisible` true, pitch 55 |
| 8 | Góc thứ nhất theo hướng đi (3) | `CameraPcPlayTests.FirstPersonFollowsWalking`: Teleport (mặt +z); X bật; `MoveTo(A + (6,5; 0; 0,5))`; chờ 1 giây | euler y camera ≈ 90 (±10) |
| 9 | Ẩn/hiện thân khi tắt follow, bay (3) | `CameraPcPlayTests.BodyVisibilityOnDisableAndFly`: X bật; `follow.enabled = false`; frame; `follow.enabled = true`; frame; `player.SetFlyMode(true)`; frame | tắt follow → `BodyVisible` true; bật lại → false; bay → `follow.FirstPerson` false, `BodyVisible` true |
| 10 | Thu phóng / xoay ngoài góc thứ nhất (3) | `CameraPcPlayTests.LookAndZoomThirdPerson`: `SetAngles(0, 55)`; `ApplyLook(new Vector2(100, 0))`; `ApplyZoom(-1)` ×20 | yaw = 100 × `lookSensitivity` (±0,01); distance = 24 (kẹp max) |
| 11 | Regression camera / nhân vật / quan sát / bench | `PlayerCharacterPlayTests`, `ClickMovePlayTests`, `ObservationCameraPlayTests`, `SettingsPlayTests`, `WalkBenchPlayTests`, `EditOcclusionPlayTests`, `MinimapCornerPlayTests` | qua (test chỉ hỏng vì giả định pitch 20 / distance 6 → sửa theo Phạm vi, ghi tên) |
| 12 | Regression toàn bộ | EditMode + PlayMode toàn bộ | 0 fail |

### Lệnh executor chạy
Unity Editor mở project `client/` qua MCP (`run_tests`, quyết định 0010). Nếu Editor tắt thì dùng `python3 tools/unity_test.py EditMode` / `PlayMode`.
1. Ghi giờ bắt đầu `date "+%H:%M"`; base commit `git log --oneline -1`; kiểm brief 0055 (xem Mục tiêu).
2. Sau bước 1–2: `refresh_unity` → `read_console` 0 lỗi → `run_tests` EditMode lọc `GameControlsTests`, `PlayerSettingsTests` → qua.
3. Sau bước 3–4: `read_console` 0 lỗi; `python3 -m json.tool client/Assets/Game/Client/Resources/Lang/vi.json > /dev/null`; `run_tests` PlayMode lọc `SettingsPlayTests`, `PlayerCharacterPlayTests` → qua.
4. Sau bước 5: `run_tests` PlayMode lọc `CameraPcPlayTests` → qua.
5. `run_tests` EditMode toàn bộ, PlayMode toàn bộ → 0 fail; `read_console` 0 lỗi.
6. Dọn: `git checkout -- client/ProjectSettings/EditorSettings.asset`; `git status --short` chỉ còn file trong Phạm vi, `.meta` mới, brief, và `client/Assets/Game/Tests/Client/TerrainEditPlayTests.cs` (có từ trước, để nguyên).
7. Ghi giờ kết thúc; commit (bước 6).

### User tự test
Trên Mac, mở scene `SimView`, Play (gộp cho 0055 + 0056):
1. Click trái lên mặt đất xa → nhân vật đi thẳng tới, dừng đúng chỗ; dấu tròn trắng hiện tại chỗ click rồi mờ trong ~0,5 giây; vòng mờ dưới chân, mũi tên chỉ hướng đi, mũi tên mất khi dừng.
2. Click lên đỉnh một cột cao / xuống hố → dấu ở đúng chỗ click, nhân vật đi theo hướng đó trên mặt đất của mình.
3. Giữ chuột trái và rê → nhân vật đi theo con trỏ.
4. Đi vào bậc 1 ô → tự nhảy lên; vào tường 2 ô → dừng sau chừng 0,3 giây.
5. A khi đang đi → nhảy vẫn tiến về đích. Shift → lao 4 ô; bấm Shift lại ngay → không lao (hồi 3 giây).
6. Camera đầu game nhìn chéo từ trên cao; đi không làm camera xoay; giữ chuột giữa xoay; lăn chuột gần/xa.
7. Space → camera về sau lưng nhân vật. X → nhìn từ mắt, không thấy thân, chuột giữa không xoay; click trái vẫn đi; X lần nữa → về như cũ.
8. Cài đặt (O): kéo "Góc cúi camera" và "Khoảng cách camera" → camera đổi ngay; nút "Camera về mặc định" → 55° / 14.
9. Chuột phải: dùng công cụ như trước (Tab đổi Đào/Đặt/Xới/Camera); click trái lên bảng / minimap / nút → không làm nhân vật đi.
Cảm giác (tốc độ lướt, góc 55°, 14 ô, dấu click, vòng chân) chưa ổn → báo số muốn đổi; Planner sửa `sim_m1.json`.

## Dừng và chuyển lại Opus khi
- Brief 0055 chưa có commit, hoặc `PlayerMotion` / `PlayerCharacter` sau 0055 khác mô tả ở "Bối cảnh".
- Brief mơ hồ hoặc mâu thuẫn với code thật.
- Cần một quyết định thuộc AGENTS.md §3.4 hoặc ngoài "Quyết định đã chốt".
- Cùng một lỗi sửa 2 lần vẫn chưa qua.
- Phải sửa file ngoài "Phạm vi", hoặc test cũ hỏng vì lý do khác giả định camera.
- Cần API Unity không có trong "Bối cảnh cần đọc".

---

## Kết quả
- Base commit: 47d0584
- Chạy bằng: Sonnet · medium
- Bắt đầu: 18:04 · Kết thúc: 18:17 (~13 phút)
- Commit: f268ce9 — FollowCamera, PlayerCharacter, PlayerSettings, SettingsView, GameInput, GameControls.inputactions, vi.json, test (PlayerSettingsTests, GameControlsTests, SettingsPlayTests.Apply_ReachesScene, CameraPcPlayTests mới + .meta).
- Test cũ sửa vì giả định camera: không có.
- Editor Unity đang tắt (MCP không thấy instance) → chạy bằng tools/unity_test.py; đã `git checkout` EditorSettings.asset. TerrainEditPlayTests.cs để nguyên, không commit.

## Kết quả test
| # | Kết quả | Bằng chứng |
|---|---|---|
| 1 | đạt | EditMode toàn bộ 532/532 (gồm GameControlsTests 33 action) |
| 2 | đạt | PlayerSettingsTests 7/7 gồm MigratesV1AndOptionalCamera |
| 3 | đạt | SettingsPlayTests.Apply_ReachesScene |
| 4–10 | đạt | CameraPcPlayTests 7/7 (lần đầu FirstPersonAtEyes fail do tôi viết sai DeltaAngle trong test; đã sửa test, code không đổi) |
| 11 | đạt | PlayMode toàn bộ gồm các lớp regression nêu trong brief |
| 12 | đạt | EditMode 532/0 fail; PlayMode 196/196, 0 fail. read_console không kiểm được (Editor tắt); biên dịch sạch vì test chạy được |
- `python3 -m json.tool vi.json` exit 0.
- Chờ user: mục "User tự test" (cảm giác, hình ảnh).

## Kiểm chứng
- client_test (qua verify_run): exit 0 — EditMode 532/532.
- client_test_play (qua verify_run): exit 0 — PlayMode 196/196.
- design / asset_check: không chạy (file đổi ngoài phạm vi).
- Checklist: Game.Core (PlayerSettings.cs) không tham chiếu UnityEngine/UnityEditor — đạt (dùng `System` thôi). Chỉ làm hạng mục M1 (0007), quyết định 0034 — đạt. Giờ thực tế ghi ở mục Kết quả (~13 phút) — đạt. Các mục design (pillar, glossary, nguồn, ước lượng...) không đổi file design/ → không áp dụng; chưa đối chiếu thuật ngữ mới (không thêm thuật ngữ).


---

## Review
