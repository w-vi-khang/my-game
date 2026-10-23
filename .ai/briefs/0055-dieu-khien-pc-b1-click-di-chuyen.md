# Brief 0055 — điều khiển PC B1: click trái để đi, dấu click, vòng chân, nhảy A, lướt Shift (bỏ WASD)

> Trạng thái: đã duyệt
> Review: đầy đủ
> Model thực thi: Sonnet · medium
> Cập nhật: 2026-10-23

<!-- Planner (Opus) viết, Executor (Sonnet) làm theo ở PHIÊN KHÁC — executor không có lịch sử chat, không được tự quyết. -->

## Mục tiêu
Phần **di chuyển** của bước world **7l b** ([task contract](../tasks/m1-world-7l-dieu-khien-pc.md), quyết định [0034](../../design/decisions/0034-dieu-khien-pc-click-di-chuyen.md) mục 1). Trên PC:
- chuột trái lên world = đi thẳng tới đó (đích quy về độ cao chân), giữ chuột trái = đích bám theo con trỏ;
- tự nhảy bậc 1 ô; kẹt 0,3 giây thì dừng;
- dấu click tại đúng điểm click, mờ dần 0,5 giây;
- vòng mờ dưới chân + mũi tên hướng đi (ẩn khi đứng yên);
- **A** nhảy, **Shift** lướt 4 ô; bỏ WASD.

Phần camera (góc mặc định kiểu LoL, Space, X, Cài đặt) là brief **0056** (7l b2), làm sau brief này.

**Tạm thời giữa 7l b và 7l c:** công cụ hiện có (Tab đổi Đào/Đặt/Xới/Camera) chuyển từ chuột trái sang **chuột phải**; hành động Cancel (chuột phải) bị bỏ. 7l c sẽ thay bằng chế độ D/Q/E/R.

## Bối cảnh cần đọc
- `client/Assets/Game/Client/PlayerCharacter.cs` (270 dòng) — đọc hết. Chỗ sẽ sửa:
  - `:13` `public enum AutoWalkState { None, Walking, Arrived, Cancelled, Failed }`;
  - `:20–22` `public bool readKeyboard = true; public Vector2 MoveInput; public bool JumpInput;` — **giữ** `MoveInput`/`JumpInput` làm API lập trình (test + `WalkBench.cs:177–179` gán trực tiếp);
  - `:48` `public void StartAutoWalk(IReadOnlyList<Game.Core.Sim.CellPos> path)`;
  - `:70–84` `Awake()` (gọi `ConfigLoader.LoadSimConfig()` ở `:83`);
  - `:145` `public void Place(Vector3 feetWorld, float yawDeg)`; `:158` `public void SetFlyMode(bool on)`;
  - `:183–268` `Update()`: `:207–216` đọc input, `:221–222` huỷ tự đi, `:223–267` di chuyển.
- `client/Assets/Game/Client/WalkBench.cs:177–179` — mẫu tự nhảy bậc: `int px = Mathf.FloorToInt(f.x + dir.x * 0.7f), py = Mathf.FloorToInt(f.y + 0.05f), pz = ...; player.JumpInput = WorldEditor.IsSolidAt(view.World, px, py, pz);`
- `client/Assets/Game/Core/WorldEditor.cs:190` `public static bool IsSolidAt(WorldData world, int x, int y, int z)` (y < 0 → true; ngoài map x/z → false).
- `client/Assets/Game/Core/PlayerTimers.cs:46` `public sealed class Cooldown` — `Cooldown(float durationSeconds)` (≤ 0 → ném), `Ready`, `Start()`, `Advance(float dt)`.
- `client/Assets/Game/Core/PlayerRules.cs:24` `public static void Configure(PlayerConfig p)`; `client/Assets/Game/Client/ConfigLoader.cs:39–47` `LoadSimConfig()` gọi `PlayerRules.Configure(c.player);` ở `:45`.
- `client/Assets/Game/Core/Sim/SimConfig.cs:128–155` `class PlayerConfig` (trường cuối `public int stackMax = 999;` `:154`); kiểm biên player trong `Validate()` `:341–388` (cuối: `stackMax` `:387–388`).
- `design/data/sim_m1.json:33–39` khối `"player"`; dòng cuối `:38` `"bagSlots": 10, "hotbarSlots": 2, "bagPageSize": 10, "hotbarPageSize": 10, "stackMax": 999`. Bản sao `client/Assets/Game/Client/Resources/Config/sim_m1.json` phải **giống từng byte** (test `ConfigTests` so hai file; `SimConfigTests.ConfigMatchesCodeDefaults` `:62` so JSON với giá trị mặc định trong code).
- `client/Assets/Game/Tests/Core/Sim/SimConfigTests.cs`: `ConfigParsesAndValidates` (assert player ở `:51–56`), `ValidateRejectsBadPlayerRules` (`Bad(...)` ở `:71–91`).
- `client/Assets/Game/Client/Input/GameControls.inputactions` — map `Player`: action `Move` (`:9`, Value/Vector2) + 5 binding (composite `WASD` `:209` và 4 phần `w/s/a/d`), `Jump` (`<Keyboard>/space`, binding `:265–273`), `UseTool` (`<Mouse>/leftButton` `:276–283`), `Cancel` (action `:36`, binding `<Mouse>/rightButton` `:286–293`). Mỗi action/binding có `"id"` UUID riêng.
- `client/Assets/Game/Client/GameInput.cs:9–39` — mỗi action một dòng `public static InputAction X => Get("Map/X");`.
- `client/Assets/Game/Tests/ClientEdit/GameControlsTests.cs`: bảng `Expected()` `:22–55`, `Assert.AreEqual(31, ...)` ở `:62` và `:91`, danh sách file `NoDirectInputReadsInScope` `:98`.
- `client/Assets/Game/Tests/Client/GameControlsPlayTests.cs:24–55` `SwapMouseButtonsByOverride` (dùng `UseTool` + `Cancel`).
- `client/Assets/Game/Client/TerrainEditInput.cs`: doc `:8–21`; `:98–103` khối "chuột phải thoát gieo" dùng `GameInput.Cancel`; `:186` `if (Tool == EditTool.Till && GameInput.Cancel.WasPressedThisFrame()) UnmarkAimed(digCell);`. Gieo vẫn thoát được bằng Esc (`:93`) và Tab (`ToggleTool` `:212–216`).
- `client/Assets/Game/Client/PlayerFarmActions.cs:216` `if (player != null && (player.FlyMode || player.MoveInput.sqrMagnitude > 0.01f || player.JumpInput))` — huỷ tự xới khi người chơi tự đi.
- `client/Assets/Game/Client/SimView.cs`: `:345` `public bool BagOpen { get; private set; }`, `:268` `public bool StockOpen { get; private set; }`, `:267` `public int OpenPlotId { get; private set; } = -1;`, `:450` `static SimView active;`, `:453` `public static bool BlocksPointer(Vector2 screen)`.
- Con trỏ đang bị UI che: mẫu `TerrainEditInput.cs:105` — `MinimapView.BlocksPointer(pointer) || TimeDial.BlocksPointer(pointer) || SimView.BlocksPointer(pointer) || ObservationCamera.BlocksPointer(pointer)` (SimView đã gộp SettingsView).
- Vẽ đường: `TerrainEditInput.cs:440–456` `CreateOutline()` — LineRenderer, shader `Shader.Find("Game/SelectionOverlay")` (dự phòng `"Universal Render Pipeline/Unlit"`), `mat.SetColor("_BaseColor", ...)`. Shader nhân `_BaseColor * vertex color`, `Blend SrcAlpha OneMinusSrcAlpha`, `ZTest Always`.
- Chuỗi hiển thị: `client/Assets/Game/Client/Resources/Lang/vi.json` (mảng `entries` `{ "key", "text" }`).
- Test PlayMode mẫu dựng cảnh nhân vật: `client/Assets/Game/Tests/Client/PlayerCharacterPlayTests.cs:22–103` (`SetUp`, `TearDown`, `Run`, `Teleport`, `Build`, `Feet`).
- Docs API (Unity 6000.4 / Input System 1.19) [docs]:
  - [Camera.ScreenPointToRay](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/Camera.ScreenPointToRay.html) — `public Ray ScreenPointToRay(Vector3 pos);`
  - [Physics.Raycast](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/Physics.Raycast.html) — dạng `(Ray ray, out RaycastHit hitInfo, float maxDistance, int layerMask, QueryTriggerInteraction queryTriggerInteraction)`
  - [Plane.Raycast](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/Plane.Raycast.html) — `public bool Raycast(Ray ray, out float enter);`
  - [CharacterController.Move](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/CharacterController.Move.html) — `public CollisionFlags Move(Vector3 motion);`
  - [LineRenderer](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/LineRenderer.html) — `loop`, `positionCount`, `SetPositions`, `startColor`/`endColor`, `widthMultiplier`, `useWorldSpace`
  - [InputAction](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.19/api/UnityEngine.InputSystem.InputAction.html) — `WasPressedThisFrame()`, `IsPressed()`

## Quyết định đã chốt (không được đổi)
- 0034 mục 1 (user 2026-10-23): chuột trái lên world = đi tới, tầm click vô hạn; đích và hướng **quy về độ cao chân** (chỉ x, z); đi **thẳng**, không tìm đường; tự nhảy bậc 1 ô; click mới → đổi hướng ngay; giữ chuột trái → đích bám con trỏ; dấu click tại **đúng điểm click** (kể cả cao/thấp hơn nhân vật); vòng mờ dưới chân + mũi tên (ẩn khi đứng yên); **A** nhảy, đang đi mà nhảy vẫn bay về đích; **Shift** lướt 4 ô trong 0,15 giây theo hướng đang đi (đứng yên → hướng mặt), dừng khi chạm khối, hồi 3 giây, không tốn mana, trên không dùng được 1 lần mỗi lần rời đất, lướt xong đi tiếp về đích; bỏ WASD.
- Task contract: kẹt = không tiến được 0,3 giây → dừng; số [số tạm] nằm trong `design/data/sim_m1.json`; phím đi qua `GameControls.inputactions`; `Game.Core` không tham chiếu UnityEngine.
- Planner (brief này) — số tạm, user sửa trong JSON khi xem:
  - JSON mới trong `player`: `dashCells` 4, `dashMs` 150, `dashCooldownMs` 3000, `stuckMs` 300, `clickMarkMs` 500, `cameraPitch` 55, `cameraDistance` 14, `firstPersonPitch` 15. Ba số camera chỉ thêm vào JSON ở brief này (để chỉ sửa JSON một lần); brief 0056 dùng.
  - Tới đích khi khoảng cách ngang ≤ 0,05 ô.
  - "Tiến được" trong một frame = quãng ngang thực đi ≥ 25 % quãng ngang muốn đi.
  - Tự nhảy bậc: nhìn trước 0,7 ô theo hướng đi (như WalkBench); ô ở tầng chân đặc **và** 2 ô phía trên trống → nhảy (thân cao 1,8).
  - Tia chuột không trúng collider nào → lấy giao với mặt phẳng ngang qua chân; không giao → bỏ qua click. Tia trúng nhà/kho/dấu mốc → vẫn đi tới điểm trúng (quy về độ cao chân).
  - Dấu click chỉ hiện lúc **bấm** (không hiện liên tục khi giữ). Giữ chuột chỉ đổi đích nếu lần bấm bắt đầu trên world (không bắt đầu trên UI).
  - Lướt: trọng lực tạm ngưng trong lúc lướt (vy = 0); hồi 3 giây tính từ lúc **bắt đầu** lướt; chạm khối = quãng ngang thực đi < 50 % quãng muốn đi → dừng lướt. Shift bị bỏ qua khi bảng túi / kho / ô ruộng đang mở (Shift còn là `Panels/TransferAll` tới 7l d).
  - Tạm (tới 7l c): `UseTool` sang chuột phải; bỏ action `Cancel` — thoát gieo bằng Esc / Tab; bỏ đánh dấu xới bằng phím tạm thời không có (7l c thêm nút "Bỏ đánh dấu"; hàm `UnmarkAimed` giữ nguyên).
- Ngoài phạm vi brief này: camera (0056); nối máu/mana vào client (`AddMoved`, rơi, ngập — 7l c/e); chế độ D/Q/E/R, tầm 10/2 (7l c); "Không tới được" cho hành động (7l c); cảm ứng.

## Phạm vi
- Được sửa/tạo:
  - `client/Assets/Game/Core/PlayerMotion.cs` (mới) + `.meta`
  - `client/Assets/Game/Core/Sim/SimConfig.cs` (chỉ `PlayerConfig` + `Validate()` phần player)
  - `design/data/sim_m1.json`, `client/Assets/Game/Client/Resources/Config/sim_m1.json`
  - `client/Assets/Game/Client/ConfigLoader.cs` (một dòng)
  - `client/Assets/Game/Client/Input/GameControls.inputactions`, `client/Assets/Game/Client/GameInput.cs`
  - `client/Assets/Game/Client/PlayerCharacter.cs`, `client/Assets/Game/Client/MoveMarkers.cs` (mới) + `.meta`
  - `client/Assets/Game/Client/TerrainEditInput.cs` (chỉ các chỗ ở bước 7), `client/Assets/Game/Client/PlayerFarmActions.cs` (dòng 216), `client/Assets/Game/Client/SimView.cs` (thêm 1 thuộc tính tĩnh)
  - `client/Assets/Game/Client/Resources/Lang/vi.json` (9 chuỗi ở bước 8)
  - Test: `Tests/Core/PlayerMotionTests.cs` (mới) + `.meta`, `Tests/Core/Sim/SimConfigTests.cs`, `Tests/ClientEdit/GameControlsTests.cs`, `Tests/Client/GameControlsPlayTests.cs`, `Tests/Client/ClickMovePlayTests.cs` (mới) + `.meta`, `Tests/Client/PlayerBagSowPlayTests.cs` (chỉ dòng 361: chuỗi `farm.hud_sow` mới — Planner bổ sung 2026-10-23)
  - File brief này (mục Kết quả trở xuống)
- Không được sửa:
  - `FollowCamera.cs`, `SettingsView.cs`, `PlayerSettings.cs` (brief 0056);
  - `design/` ngoài `design/data/sim_m1.json`;
  - `client/Assets/Game/Tests/Client/TerrainEditPlayTests.cs` (đang có thay đổi dở của phiên khác — để nguyên, không commit);
  - test cũ khác ngoài danh sách trên.

## Các bước
1. **Số trong JSON + PlayerConfig.**
   - `SimConfig.cs` `PlayerConfig`, sau `public int stackMax = 999;` thêm:
     ```csharp
     public float dashCells = 4f;
     public int dashMs = 150;
     public int dashCooldownMs = 3000;
     public int stuckMs = 300;
     public int clickMarkMs = 500;
     public float cameraPitch = 55f;
     public float cameraDistance = 14f;
     public float firstPersonPitch = 15f;
     ```
   - `Validate()`, ngay sau kiểm `stackMax` (`:387–388`), thêm theo mẫu `throw new ArgumentException("player.<tên> must be in [a, b].")`:
     `dashCells` [0.5, 16] · `dashMs` [10, 2000] · `dashCooldownMs` [100, 60000] · `stuckMs` [50, 5000] · `clickMarkMs` [50, 5000] · `cameraPitch` [10, 85] · `cameraDistance` [2.5, 24] · `firstPersonPitch` [-30, 60]. Số thực viết dạng `if (!(x >= a && x <= b))` như `targetReach` `:349`.
   - Hai file `sim_m1.json`: dòng `:38` thêm `,` ở cuối rồi thêm một dòng ngay sau (8 dấu cách đầu dòng như các dòng trên):
     `        "dashCells": 4, "dashMs": 150, "dashCooldownMs": 3000, "stuckMs": 300, "clickMarkMs": 500, "cameraPitch": 55, "cameraDistance": 14, "firstPersonPitch": 15`
     Sửa `design/data/` rồi chép đè bản client; `cmp` hai file → giống nhau.
   - `SimConfigTests`: `ConfigParsesAndValidates` thêm assert 8 giá trị trên; `ValidateRejectsBadPlayerRules` thêm `Bad(c => c.player.dashCells = 0.4f, ...)`, `dashMs = 5`, `dashCooldownMs = 50`, `stuckMs = 10`, `clickMarkMs = 6000`, `cameraPitch = 90`, `cameraDistance = 2`, `firstPersonPitch = -40`.
   - Xong khi: biên dịch không lỗi; `SimConfigTests` + `ConfigTests` qua.

2. **`Core/PlayerMotion.cs` (mới, namespace `Game.Core`, chỉ `using System;`).** Viết đúng API sau (thân hàm theo mô tả):
   ```csharp
   /// <summary>Di chuyển PC (0034 mục 1–2, world 7l b): số đọc từ sim_m1.json player, luật tự nhảy bậc. Client gọi.</summary>
   public static class PlayerMotion
   {
       public static float DashCells { get; private set; } = 4f;
       public static float DashSeconds { get; private set; } = 0.15f;
       public static float DashCooldownSeconds { get; private set; } = 3f;
       public static float StuckSeconds { get; private set; } = 0.3f;
       public static float ClickMarkSeconds { get; private set; } = 0.5f;
       public static float CameraPitch { get; private set; } = 55f;
       public static float CameraDistance { get; private set; } = 14f;
       public static float FirstPersonPitch { get; private set; } = 15f;
       /// <summary>Tới đích khi khoảng cách ngang ≤ chừng này (ô).</summary>
       public const float ArriveDistance = 0.05f;
       /// <summary>Nhìn trước chừng này (ô) theo hướng đi khi xét tự nhảy bậc.</summary>
       public const float StepLookAhead = 0.7f;

       /// <summary>null → ArgumentNullException. ms → giây (/1000f).</summary>
       public static void Configure(PlayerConfig p);

       /// <summary>Tự nhảy bậc 1 ô: ô (floor(feetX + dirX·0,7), floor(feetY + 0,05), floor(feetZ + dirZ·0,7)) đặc và hai ô phía trên nó không đặc (WorldEditor.IsSolidAt). dirX² + dirZ² &lt; 1e-6 → false. world null → ArgumentNullException.</summary>
       public static bool ShouldStepUp(WorldData world, float feetX, float feetY, float feetZ, float dirX, float dirZ);
   }

   /// <summary>Đếm thời gian liền không tiến được (kẹt).</summary>
   public sealed class StuckTimer
   {
       public StuckTimer(float seconds);   // ≤ 0 → ArgumentOutOfRangeException
       public float Seconds { get; }
       public float Elapsed { get; private set; }
       /// <summary>dt &lt; 0 → ném. progressed → Elapsed = 0, trả false. Không → Elapsed += dt, trả Elapsed ≥ Seconds.</summary>
       public bool Update(float dt, bool progressed);
       public void Reset();               // Elapsed = 0
   }

   /// <summary>Lướt (Shift): quãng Cells trong Seconds, hồi Cooldown tính từ lúc bắt đầu, trên không 1 lần mỗi lần rời đất.</summary>
   public sealed class DashState
   {
       public DashState(float cells, float seconds, float cooldownSeconds); // cells ≤ 0 hoặc seconds ≤ 0 → ArgumentOutOfRangeException; cooldown theo Cooldown (≤ 0 → ném)
       public float Cells { get; }
       public float Seconds { get; }
       public float Speed => Cells / Seconds;
       public Cooldown Cooldown { get; }
       public bool Active { get; private set; }
       public bool AirUsed { get; private set; }
       /// <summary>Số ô còn phải lướt.</summary>
       public float Remaining { get; private set; }
       /// <summary>Được khi !Active, Cooldown.Ready và (grounded hoặc !AirUsed). Được → Active, Remaining = Cells, Cooldown.Start(), !grounded → AirUsed = true; trả true. Không → false, không đổi gì.</summary>
       public bool TryStart(bool grounded);
       /// <summary>dt &lt; 0 → ném. Active: d = min(Speed·dt, Remaining); Remaining −= d; Remaining ≤ 1e-4 → Active = false, Remaining = 0; trả d. Không Active → 0.</summary>
       public float Step(float dt);
       /// <summary>Chạm khối: Active = false, Remaining = 0.</summary>
       public void Stop();
       /// <summary>Mỗi frame: Cooldown.Advance(dt); grounded và !Active → AirUsed = false.</summary>
       public void Tick(float dt, bool grounded);
   }
   ```
   - `ConfigLoader.cs:45`: sau `PlayerRules.Configure(c.player);` thêm `PlayerMotion.Configure(c.player);`.
   - Xong khi: biên dịch không lỗi.

3. **Test EditMode `Tests/Core/PlayerMotionTests.cs`** (namespace `Game.Core.Tests`; dựng world như `DropFieldTests.cs:15–20`: `new WorldData(new WorldSize(32, 16, 32))`, `w.Set(x, y, z, Cell.Make(BlockMaterial.Stone))`). Các test ở bảng "Phần cần test" dòng 3–6. Mỗi test đổi `PlayerMotion` phải trả lại bằng `PlayerMotion.Configure(new PlayerConfig())` (dùng `try/finally` hoặc `[TearDown]`).

4. **Phím.** `GameControls.inputactions` map `Player`:
   - Xoá action `Move` và 5 binding của nó (composite `WASD` + `w`, `s`, `a`, `d`).
   - Xoá action `Cancel` và binding `<Mouse>/rightButton` của nó.
   - Binding `Jump`: `<Keyboard>/space` → `<Keyboard>/a`.
   - Binding `UseTool`: `<Mouse>/leftButton` → `<Mouse>/rightButton`.
   - Thêm action `MoveClick` (đặt ở vị trí cũ của `Move`, đầu danh sách) và `Dash` (ngay sau `Jump`), cả hai `"type": "Button"`, `"expectedControlType": "Button"`, `"initialStateCheck": false`, các trường khác như `Jump`. Binding: `MoveClick` → `<Mouse>/leftButton`, `Dash` → `<Keyboard>/shift`. `"id"` mới cho 2 action + 2 binding: sinh bằng `python3 -c "import uuid;print(uuid.uuid4())"`.
   - `GameInput.cs`: xoá dòng `Move`, `Cancel`; thêm `public static InputAction MoveClick => Get("Player/MoveClick");` và `public static InputAction Dash => Get("Player/Dash");`. Doc `:6` sửa "Bảng đổi phím: M3." → "Bảng đổi phím: M1 (7l f, 0034)."
   - `GameControlsTests.Expected()`: bỏ `Player/Move`, `Player/Cancel`; `Player/Jump` = `B("<Keyboard>/a")`; `Player/UseTool` = `B("<Mouse>/rightButton")`; thêm `["Player/MoveClick"] = B("<Mouse>/leftButton")`, `["Player/Dash"] = B("<Keyboard>/shift")`. Hai số `31` giữ nguyên (bớt 2, thêm 2). `NoDirectInputReadsInScope` thêm `"MoveMarkers.cs"` vào mảng file.
   - `GameControlsPlayTests.SwapMouseButtonsByOverride`: thay mọi `GameInput.Cancel` bằng `GameInput.MoveClick` và đảo kỳ vọng: mặc định `UseTool` = `<Mouse>/rightButton`, `MoveClick` = `<Mouse>/leftButton`; override `UseTool` → `<Mouse>/leftButton`, `MoveClick` → `<Mouse>/rightButton`; kiểm controls tên `leftButton` / `rightButton` tương ứng; bỏ override → `UseTool` về `rightButton`. Doc lớp: "(chuẩn bị bảng đổi phím 7l f)".
   - Xong khi: `GameControlsTests` (EditMode) qua.

5. **`PlayerCharacter.cs` — click di chuyển, tự nhảy bậc, kẹt, lướt.**
   - Ngay dưới `AutoWalkState` (`:13`) thêm `public enum ClickMoveState { None, Walking, Arrived, Blocked }`.
   - Thành viên mới:
     ```csharp
     /// <summary>Đi tới chỗ click (0034 mục 1).</summary>
     public ClickMoveState ClickMove { get; private set; }
     /// <summary>Đích click, toạ độ terrain; y = độ cao chân lúc click.</summary>
     public Vector3 ClickTargetLocal { get; private set; }
     public DashState Dash { get; private set; }
     public bool Dashing => Dash != null && Dash.Active;
     /// <summary>Hướng đi ngang (world) frame vừa rồi; zero khi đứng yên. Mũi tên dưới chân dùng.</summary>
     public Vector3 MoveDirection { get; private set; }
     public MoveMarkers Markers { get; private set; }
     /// <summary>Người chơi đang tự di chuyển → huỷ tự đi theo đường ô / tự xới.</summary>
     public bool HasMoveIntent => FlyMode || MoveInput.sqrMagnitude > 0.01f || JumpInput || ClickMove == ClickMoveState.Walking || Dashing;
     public const float ClickRayDistance = 500f;
     StuckTimer stuck;
     Vector3 dashDir;
     bool holdFromWorld;
     ```
   - `Awake()`: sau `ConfigLoader.LoadSimConfig();` thêm `stuck = new StuckTimer(PlayerMotion.StuckSeconds); Dash = new DashState(PlayerMotion.DashCells, PlayerMotion.DashSeconds, PlayerMotion.DashCooldownSeconds); Markers = gameObject.AddComponent<MoveMarkers>();`.
   - Hàm public mới:
     ```csharp
     /// <summary>Đặt đích đi (toạ độ terrain); y bỏ qua — đích quy về độ cao chân hiện tại.</summary>
     public void MoveTo(Vector3 targetLocal)   // ClickTargetLocal = (x, FeetLocal.y, z); ClickMove = Walking; stuck.Reset()
     public void StopClickMove()               // Walking → None
     /// <summary>Điểm world dưới tia: Physics.Raycast(ray, out hit, ClickRayDistance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore) → hit.point/hit.normal; không trúng → new Plane(Vector3.up, transform.position).Raycast(ray, out enter) với enter > 0 → ray.GetPoint(enter)/Vector3.up; còn lại false.</summary>
     public bool PointOnWorld(Ray ray, out Vector3 pointWorld, out Vector3 normalWorld)
     /// <summary>Click trái: PointOnWorld → MoveTo(terrain.transform.InverseTransformPoint(point)); mark → Markers.ShowClick(point, normal). Trả false khi tia không cho điểm.</summary>
     public bool ClickMoveRay(Ray ray, bool mark)
     /// <summary>Shift. false khi FlyMode, ObservationCamera.InputLocked, !Ready, hoặc Dash.TryStart(Controller.isGrounded) false. Hướng: đang Walking → hướng ngang từ chân tới ClickTargetLocal (đổi sang world bằng terrain.transform.TransformDirection); không thì Visual.forward; bỏ y, chuẩn hoá; vẫn ≈ 0 → Visual.forward bỏ y.</summary>
     public bool TryDash()
     ```
   - `StartAutoWalk`: thêm `ClickMove = ClickMoveState.None;` ở đầu. `Place`: thêm `ClickMove = ClickMoveState.None; Dash?.Stop(); stuck?.Reset();`. `SetFlyMode`: đầu hàm thêm `if (on) { ClickMove = ClickMoveState.None; Dash?.Stop(); }`.
   - `Update()`:
     - Nhánh khoá (`:207–211`, `FlyMode || ObservationCamera.InputLocked`): thêm `if (ClickMove == ClickMoveState.Walking) ClickMove = ClickMoveState.None; Dash.Stop();`.
     - Nhánh `readKeyboard` (`:212–216`): **bỏ** dòng đọc `GameInput.Move`; giữ `JumpInput = GameInput.Jump.IsPressed();`; thêm `ReadPointerMove();` và `if (GameInput.Dash.WasPressedThisFrame() && !SimView.AnyPanelOpen) TryDash();`.
     - Hàm riêng `void ReadPointerMove()`:
       ```csharp
       if (!GameInput.HasPointer || MinimapView.IsAnyOpen || cam == null) return;
       bool pressed = GameInput.MoveClick.WasPressedThisFrame();
       if (!pressed && !(holdFromWorld && GameInput.MoveClick.IsPressed())) return;
       var pos = GameInput.PointerPosition;
       bool blocked = MinimapView.BlocksPointer(pos) || TimeDial.BlocksPointer(pos) || SimView.BlocksPointer(pos) || ObservationCamera.BlocksPointer(pos);
       if (pressed) holdFromWorld = !blocked;
       if (blocked) return;
       ClickMoveRay(cam.ScreenPointToRay(pos), pressed);
       ```
     - `:221` đổi điều kiện huỷ tự đi thành `if (AutoWalk == AutoWalkState.Walking && HasMoveIntent)`.
     - Sau khối `if (AutoWalk == AutoWalkState.Walking) { ... }` (`:228–249`) thêm `else if (ClickMove == ClickMoveState.Walking)`: `dx = ClickTargetLocal.x − f.x`, `dz = ClickTargetLocal.z − f.z`, `dist = √(dx²+dz²)`; `dist ≤ PlayerMotion.ArriveDistance` → `ClickMove = Arrived`, `dir = Vector3.zero`; không thì `dir = terrain.transform.TransformDirection(new Vector3(dx/dist, 0, dz/dist))`, `step = Mathf.Min(step, dist)`, `autoJump = Controller.isGrounded && PlayerMotion.ShouldStepUp(terrain.World, f.x, f.y, f.z, dx/dist, dz/dist)`.
     - Đầu phần di chuyển (trước `:252`): `bool grounded = Controller.isGrounded; Dash.Tick(Time.deltaTime, grounded);`.
     - Thay `:252–254` bằng: nếu `Dashing` → `h = dashDir * Dash.Step(Time.deltaTime); vy = 0f;` không thì giữ nguyên 3 dòng trọng lực/nhảy cũ (dùng `grounded`).
     - Quanh `Controller.Move` (`:261`): lưu `var before = transform.position;` trước; sau đó `movedH` = độ dài ngang (x, z) của `transform.position − before`, `wantH` = độ dài ngang của `h` (sau ClampToWorld).
       - `if (Dashing && movedH < wantH * 0.5f) Dash.Stop();`
       - `if (ClickMove == ClickMoveState.Walking && !Dashing) { if (stuck.Update(Time.deltaTime, wantH > 1e-5f && movedH >= wantH * 0.25f)) ClickMove = ClickMoveState.Blocked; } else stuck.Reset();`
     - Hướng mặt + mũi tên: đang lướt → `Visual.rotation = Quaternion.LookRotation(dashDir)`, `MoveDirection = dashDir`; không thì giữ khối slerp `:263–264` và `MoveDirection = dir.sqrMagnitude > 0.01f ? new Vector3(dir.x, 0f, dir.z).normalized : Vector3.zero`. Nhún (`:265–267`) giữ nguyên.
   - Doc lớp (`:9–12`): thay "WASD đi theo hướng camera, Space nhảy" bằng "chuột trái đi tới chỗ click (0034, 7l b), A nhảy, Shift lướt; MoveInput/JumpInput vẫn gán được từ code (test, WalkBench)".
   - Xong khi: biên dịch không lỗi; `PlayerCharacterPlayTests` qua.

6. **`Client/MoveMarkers.cs` (mới, namespace `Game.Client`)** — component trên GameObject nhân vật (PlayerCharacter thêm trong Awake):
   ```csharp
   /// <summary>Dấu click + vòng dưới chân + mũi tên hướng đi (0034 mục 1, world 7l b). LineRenderer, shader Game/SelectionOverlay.</summary>
   public class MoveMarkers : MonoBehaviour
   {
       public float clickRadius = 0.35f, ringRadius = 0.6f;
       public bool ClickVisible => click.enabled;
       /// <summary>Điểm click (world) lần cuối.</summary>
       public Vector3 ClickPoint { get; private set; }
       public float ClickAlpha { get; private set; }
       public bool RingVisible => ring.enabled;
       public bool ArrowVisible => arrow.enabled;
       public void ShowClick(Vector3 pointWorld, Vector3 normalWorld);
   }
   ```
   - Tạo 3 LineRenderer trong `Awake`, vật liệu như `TerrainEditInput.CreateOutline` (`:449–456`), `_BaseColor` trắng, màu đường qua `startColor`/`endColor`, `useWorldSpace = false`, `shadowCastingMode = Off`:
     - `click`: GameObject gốc riêng `"ClickMark"` (không làm con nhân vật; huỷ trong `OnDestroy`), `loop = true`, 24 điểm vòng tròn bán kính `clickRadius` trong mặt phẳng XZ, `widthMultiplier` 0,06, tắt sẵn. `ShowClick`: `ClickPoint = pointWorld`; đặt `position = pointWorld + normalWorld * 0.03f`, `rotation = Quaternion.FromToRotation(Vector3.up, normalWorld)`; bật; tuổi = 0.
     - `ring`: con của nhân vật, `localPosition (0, 0.03, 0)`, `loop = true`, 32 điểm bán kính `ringRadius`, rộng 0,05, màu trắng alpha 0,35.
     - `arrow`: con của nhân vật, `localPosition (0, 0.03, 0)`, 3 điểm `(-0.25, 0, 0.55)`, `(0, 0, 0.85)`, `(0.25, 0, 0.55)`, rộng 0,06, màu trắng alpha 0,6.
   - `LateUpdate`:
     - Dấu click đang bật: tuổi += `Time.deltaTime`; `ClickAlpha = 1 − tuổi / PlayerMotion.ClickMarkSeconds`; ≤ 0 → tắt, `ClickAlpha = 0`; gán màu (1, 1, 1, ClickAlpha).
     - `var p = GetComponent<PlayerCharacter>()` (lưu sẵn trong Awake): `ring.enabled = p.Ready && !p.FlyMode`; `arrow.enabled = ring.enabled && p.MoveDirection.sqrMagnitude > 0.01f`; khi bật, `arrow.transform.rotation = Quaternion.LookRotation(p.MoveDirection)`. Vòng không xoay theo `Visual`.
   - Xong khi: biên dịch không lỗi.

7. **Chỗ khác dùng input cũ.**
   - `SimView.cs`, ngay sau `:453` thêm `public static bool AnyPanelOpen => active != null && (active.BagOpen || active.StockOpen || active.OpenPlotId >= 0);`.
   - `TerrainEditInput.cs`: xoá khối `:98–103` (Cancel thoát gieo) và dòng `:186` (Cancel bỏ đánh dấu). Doc lớp `:11–21`: thêm một dòng `/// World 7l b (brief 0055): chuột trái = đi (PlayerCharacter); công cụ tạm dùng chuột phải tới 7l c; thoát gieo bằng Esc / Tab; bỏ đánh dấu xới tạm không có phím (7l c: nút trong bảng).` — không sửa các dòng doc cũ.
   - `PlayerFarmActions.cs:216`: điều kiện thành `if (player != null && player.HasMoveIntent)`.
   - Xong khi: `grep -n "GameInput.Cancel\|GameInput.Move\b" client/Assets/Game -r` không còn kết quả; biên dịch không lỗi.

8. **Chuỗi `vi.json`** — thay nguyên văn `"text"` của 9 key (giữ nguyên `{0}`, `{1}`, `\n`):
   | key | text mới |
   |---|---|
   | `edit.explain_no_material` | `túi hết loại này (nhặt vật rơi bằng F hoặc chuột phải, hoặc chọn loại khác bằng 1–4 / Alt + lăn chuột)` |
   | `farm.hud_sow` | `\nGieo {0} (còn {1}) — Esc / Tab để thoát` |
   | `hud.aimed_drop` | `\nNhặt (chuột phải): {0}` |
   | `hud.aimed_harvest` | `\nThu hoạch (chuột phải): {0}` |
   | `hud.camera_help` | `Camera: chuột phải = đặt camera quan sát tại chỗ và hướng đang nhìn (đặt lại thì thay chỗ cũ) · V hoặc nút Xem camera: xem / quay về\n` |
   | `hud.help_fly` | ` · Chuột phải: dùng công cụ (trong {0:0.#} ô quanh nhân vật) · F: thu vật rơi (trong {1:0.#} ô) · 1–4 / Alt + lăn chuột: chọn loại · Camera bay: WASD, Q/E, giữ chuột giữa · dừng bay mới đào/đặt được · F3: về nhân vật\n` |
   | `hud.help_idle` | ` · Chuột phải: dùng công cụ · 1–4 / Alt + lăn chuột: chọn loại · WASD, Q/E, giữ chuột giữa: camera · dừng bay mới đào/đặt được\n` |
   | `hud.help_walk` | ` · Chuột trái: đi tới chỗ click (giữ để đi theo con trỏ) · A: nhảy · Shift: lướt · Chuột phải: dùng công cụ (trong {0:0.#} ô quanh nhân vật) · F: thu vật rơi (trong {1:0.#} ô) · R: phép Mưa (trong {0:0.#} ô) · B: túi · chuột phải lên cây chín: thu hoạch · 1–4 / Alt + lăn chuột: chọn loại · giữ chuột giữa: xoay · lăn chuột: gần/xa · F3: camera bay\n` |
   | `hud.till_help` | `Xới: chuột phải lên đất/cỏ = đánh dấu · lên biển = tự đi tới xới · lên ô ruộng = chọn giống · K: kho\n` |
   - Xong khi: JSON hợp lệ (`python3 -m json.tool` exit 0); `HardcodedTextScanTests` + test chuỗi trong `Tests/Core/Text` qua.

9. **Test PlayMode `Tests/Client/ClickMovePlayTests.cs`** (namespace `Game.Client.Tests`). `SetUp`/`TearDown`/`Teleport`/`Build`/`Feet` chép từ `PlayerCharacterPlayTests.cs:22–103` (giữ `Time.captureFramerate = 60`, `player.readKeyboard = false`). Thêm helper `IEnumerator WaitWhile(Func<bool> cond, float maxSeconds)` (chờ từng frame, cộng `Time.deltaTime`). Các test ở bảng "Phần cần test" dòng 7–15. Tia click trong test dựng tay: `new Ray(view.transform.TransformPoint(new Vector3(x, G + 20, z)), Vector3.down)` rồi `player.ClickMoveRay(ray, true)`.

10. **Commit** (sau khi mọi test qua): một commit gồm file trong Phạm vi + `.meta` mới + brief; **không** gồm `TerrainEditPlayTests.cs`. Message: `brief 0055: click-to-move, dash, jump on A, move markers (7l b1)` + dòng `Co-Authored-By` theo hướng dẫn phiên.

## Tiêu chí xong
- Biên dịch không lỗi, `read_console` 0 lỗi sau khi chạy test.
- EditMode toàn bộ: 527 cũ + test mới, 0 fail. PlayMode toàn bộ: 181 cũ + test mới, 0 fail.
- Hai `sim_m1.json` giống từng byte; `grep` ở bước 7 rỗng.
- Mọi dòng "Phần cần test" có kết quả thật trong mục Kết quả test.

## Test sau khi xong

### Phần cần test
| # | Phần (bước) | Cách test | Kết quả mong đợi |
|---|---|---|---|
| 1 | JSON + PlayerConfig (1) | `SimConfigTests.ConfigParsesAndValidates` (thêm assert) | `dashCells` 4, `dashMs` 150, `dashCooldownMs` 3000, `stuckMs` 300, `clickMarkMs` 500, `cameraPitch` 55, `cameraDistance` 14, `firstPersonPitch` 15; `ConfigMatchesCodeDefaults` + `ConfigTests` qua |
| 2 | Kiểm biên (1) | `SimConfigTests.ValidateRejectsBadPlayerRules` (thêm 8 `Bad`) | 8 giá trị sai ở bước 1 đều ném |
| 3 | Configure (2) | `PlayerMotionTests.ConfigureConvertsMs` | Configure(PlayerConfig{dashMs 200, dashCooldownMs 4000, stuckMs 500, clickMarkMs 700, dashCells 5, cameraPitch 40, cameraDistance 10, firstPersonPitch 0}) → DashSeconds 0,2, DashCooldownSeconds 4, StuckSeconds 0,5, ClickMarkSeconds 0,7, DashCells 5, CameraPitch 40, CameraDistance 10, FirstPersonPitch 0; Configure(null) ném; trả lại mặc định |
| 4 | Tự nhảy bậc (2) | `PlayerMotionTests.ShouldStepUp` — nền đá y = 0; chân (5,5; 1; 5,5), hướng (0, 1) | không có gì trước mặt → false; đá ở (5,1,6) → true; thêm đá (5,2,6) → false (tường 2 ô); bỏ (5,2,6), thêm (5,3,6) → false (không đủ chỗ đầu); hướng (0,0) → false; world null ném |
| 5 | Kẹt (2) | `PlayerMotionTests.StuckTimer` (0,3 giây) | Update(0,2, false) → false; Update(0,2, false) → true; Update(0,2, true) → false và Elapsed 0; ctor(0) ném; Update(−1, false) ném |
| 6 | Lướt (2) | `PlayerMotionTests.DashState` (4; 0,15; 3) | TryStart(true) → true, Active; Step(0,05) ≈ 1,3333 (±1e-3); Step(0,05) ≈ 1,3333; Step(0,1) ≈ 1,3333 và Active false; TryStart(true) → false (hồi); Tick(2,5, true) → vẫn false; Tick(0,5, true) → TryStart(true) true. Mới: TryStart(false) → true, AirUsed; Stop(); Tick(3, false) → TryStart(false) false; Tick(0, true) → TryStart(false) true. Step khi không Active = 0. ctor(0, 0,15, 3), (4, 0, 3), (4, 0,15, 0) đều ném |
| 7 | Phím (4) | `GameControlsTests` (4 test) + `GameControlsPlayTests` (2 test) | bảng mặc định khớp (Jump `a`, UseTool `rightButton`, MoveClick `leftButton`, Dash `shift`, không Move/Cancel), 31 action, không trùng phím trong cùng map, GameInput khớp asset, đổi chuột trái ↔ phải lúc chạy được |
| 8 | Tới đích ±0,1 (5) | `ClickMovePlayTests.ArrivesWithinTenth`: Teleport A; `MoveTo(A + (5,5; 0; 3,5))`; chờ ≤ 4 giây tới khi `ClickMove != Walking` | `Arrived`; khoảng cách ngang chân → đích ≤ 0,1; `MoveDirection` = zero sau khi tới |
| 9 | Click cao/thấp + dấu (5, 6) | `ClickMovePlayTests.ClickHeightFlattensAndMarksPoint`: (a) dựng cột đá 3 ô (A.x+4, G+1..G+3, A.z); tia thẳng xuống tâm cột; (b) dựng sàn 5×5 cao 1 ô quanh A, Teleport lên sàn (chân y = G+2), tia xuống đất tại (A.x+6,5; ·; A.z+0,5) | (a) `ClickMoveRay` true; `ClickTargetLocal.y` = chân y (±0,01), x/z = tâm cột (±0,05); `Markers.ClickVisible`; `Markers.ClickPoint` y ≈ G+4 (±0,15). (b) đích y = G+2 (±0,05); `ClickPoint` y ≈ G+1 (±0,15). Sau 0,6 giây: `ClickVisible` false |
| 10 | Tường 2 ô → dừng (5) | `ClickMovePlayTests.TwoHighWallStops`: tường đá x A.x−2..A.x+2, y G+1..G+2, z A.z+3; `MoveTo(A + (0,5; 0; 6,5))`; chờ ≤ 3 giây | `Blocked`; chân z < A.z + 3 − 0,3; chân y < G + 2 |
| 11 | Bậc 1 ô tự nhảy (5) | `ClickMovePlayTests.ClimbsOneHighStep`: hàng đá x A.x−1..A.x+1, y G+1, z A.z+2; `MoveTo(A + (0,5; 0; 5,5))`; chờ ≤ 4 giây, ghi y lớn nhất | `Arrived`; y lớn nhất ≥ G + 2 − 0,1; z cuối ≈ A.z + 5,5 (±0,1) |
| 12 | Click mới đổi hướng (5) | `ClickMovePlayTests.NewClickChangesDirection`: `MoveTo(A + (6,5; 0; 0,5))`; sau 0,3 giây ghi chân; `MoveTo(A + (0,5; 0; −5,5))`; sau 0,3 giây | z giảm ≥ 0,8 so với lúc đổi; vẫn `Walking`; rồi `Arrived` tại đích mới ±0,1 trong ≤ 4 giây |
| 13 | Nhảy khi đi vẫn tiến (5) | `ClickMovePlayTests.JumpWhileWalkingKeepsHeading`: `MoveTo(A + (0,5; 0; 9,5))`; sau 0,2 giây `JumpInput = true` tới khi `!isGrounded`, rồi `false`; ghi z lúc rời đất và lúc chạm đất | z chạm đất − z rời đất ≥ 1,0; `ClickMove` không phải `None`/`Blocked` |
| 14 | Lướt 4 ô, chạm khối, hồi, đi tiếp (5) | `ClickMovePlayTests.Dash`: (a) Teleport (mặt +z), `TryDash()`; sau 0,4 giây; (b) gọi `TryDash()` ngay; (c) chờ 3,1 giây, `TryDash()`; (d) Teleport khác, tường 2 ô ở z A.z+2 (x A.x−1..A.x+1), `TryDash()`, sau 0,4 giây; (e) Teleport, `MoveTo(+z 9 ô)`, sau 0,1 giây `TryDash()`, chờ ≤ 3 giây | (a) true, Δz ∈ [3,85; 4,15], Δx < 0,1; (b) false; (c) true; (d) chân z ≤ A.z + 2 − 0,35, `Dashing` false sau 0,4 giây; (e) cuối cùng `Arrived` tại đích ±0,1 |
| 15 | Huỷ tự đi, bay, vòng + mũi tên (5, 6, 7) | `ClickMovePlayTests.IntentAndMarkers`: `StartAutoWalk` đường 5 ô `new CellPos(A.x + i, G + 1, A.z)` với i = 1..5 (`Game.Core.Sim.CellPos`), một frame, `MoveTo(-x 3 ô)`, một frame; rồi `SetFlyMode(true)`, một frame; `SetFlyMode(false)`; `MoveTo(+z 5 ô)`, 0,2 giây; chờ tới `Arrived` | `AutoWalk == Cancelled`; sau bay `ClickMove == None`; đang đi: `Markers.RingVisible` và `ArrowVisible` true; sau `Arrived`: `RingVisible` true, `ArrowVisible` false |
| 16 | Regression nhân vật / tự xới / bench (5, 7) | `PlayerCharacterPlayTests`, `PlayerFarmPlayTests`, `PlayerBagSowPlayTests`, `DropPlayTests`, `ObservationCameraPlayTests`, `WalkBenchPlayTests` | qua (đi bằng `MoveInput` gán từ code vẫn chạy; tự xới bị huỷ khi đi) |
| 17 | Regression toàn bộ | EditMode + PlayMode toàn bộ | 0 fail |

### Lệnh executor chạy
Unity Editor mở project `client/` qua MCP (`run_tests`, quyết định 0010). Nếu Editor tắt thì dùng `python3 tools/unity_test.py EditMode` / `PlayMode`.
1. Ghi giờ bắt đầu `date "+%H:%M"`; base commit `git log --oneline -1`.
2. Sau bước 1–3: `refresh_unity` → `read_console` 0 lỗi → `run_tests` EditMode lọc `SimConfigTests`, `ConfigTests`, `PlayerMotionTests` → qua.
3. Sau bước 4: `run_tests` EditMode lọc `GameControlsTests` → qua.
4. Sau bước 5–8: `read_console` 0 lỗi; `python3 -m json.tool client/Assets/Game/Client/Resources/Lang/vi.json > /dev/null`; `grep -rn "GameInput.Cancel\|GameInput.Move\b" client/Assets/Game` rỗng; `cmp design/data/sim_m1.json client/Assets/Game/Client/Resources/Config/sim_m1.json`.
5. Sau bước 9: `run_tests` PlayMode lọc `ClickMovePlayTests`, `PlayerCharacterPlayTests`, `GameControlsPlayTests` → qua.
6. `run_tests` EditMode toàn bộ → 0 fail. `run_tests` PlayMode toàn bộ → 0 fail. `read_console` 0 lỗi.
7. Dọn: `git checkout -- client/ProjectSettings/EditorSettings.asset`; `git status --short` chỉ còn file trong Phạm vi, `.meta` mới, brief, và `client/Assets/Game/Tests/Client/TerrainEditPlayTests.cs` (có từ trước, để nguyên).
8. Ghi giờ kết thúc; commit (bước 10).

### User tự test
Không riêng cho brief này — user xem cảm giác di chuyển + camera một lần sau brief 0056.

## Dừng và chuyển lại Opus khi
- Brief mơ hồ hoặc mâu thuẫn với code thật (vd số dòng / chữ ký ở "Bối cảnh" khác code).
- Cần một quyết định thuộc AGENTS.md §3.4 hoặc ngoài "Quyết định đã chốt" (vd muốn đổi số ngưỡng 0,25 / 0,5 / 0,05 để test qua).
- Cùng một lỗi sửa 2 lần vẫn chưa qua (đặc biệt test vật lý ở dòng 10–14).
- Phải sửa file ngoài "Phạm vi", hoặc test cũ ngoài danh sách bị hỏng.
- Cần API Unity không có trong "Bối cảnh cần đọc".

---

## Kết quả
Base commit: d38e0db
Chạy bằng: claude-sonnet-5-5 · medium
Bắt đầu: 17:41 · Kết thúc: 17:58 (giờ máy)
Commit: 0ce293a (code, test, .meta; không gồm TerrainEditPlayTests.cs)

## Kết quả test
Bước 1–9 đã code xong, **chưa commit** (file nằm trong working tree). Biên dịch 0 lỗi.
- EditMode toàn bộ: 531/531 qua (527 + 4 PlayerMotionTests) → #1–#7, #17 (EditMode) đạt.
- PlayMode `ClickMovePlayTests`: 8/8 qua → #8–#15 đạt.
- PlayMode toàn bộ (189 test): 186 qua, 3 fail lần chạy đầu; 2 fail do test mới của tôi (đã sửa, 8/8 qua). Còn 1 fail: `PlayerBagSowPlayTests.SowHud` (xem Vướng mắc). Chưa chạy lại PlayMode toàn bộ sau khi sửa.
- `grep GameInput.Cancel|GameInput.Move\b` rỗng; `cmp` hai sim_m1.json giống; `json.tool vi.json` ok; đã `git checkout EditorSettings.asset`.

- **Cập nhật cuối:** sửa `PlayerBagSowPlayTests.cs:361` theo Planner; EditMode 531/531, PlayMode 189/189 (0 fail), `read_console` 0 lỗi, đã `git checkout EditorSettings.asset`. Mọi dòng #1–#17 đạt.

## Vướng mắc
(đã giải quyết: sửa dòng 361 theo Planner)
Test cũ ngoài Phạm vi bị hỏng bởi chuỗi bước 8 → dừng theo điều kiện "test cũ ngoài danh sách bị hỏng".
- `client/Assets/Game/Tests/Client/PlayerBagSowPlayTests.cs:361`: `StringAssert.Contains("Gieo Cây A (còn 3) — chuột phải / Esc để thoát", farming.HudLines())`, trong khi bước 8 đặt `farm.hud_sow` = `\nGieo {0} (còn {1}) — Esc / Tab để thoát`.
- Đề xuất: cho phép sửa đúng dòng 361 thành `"Gieo Cây A (còn 3) — Esc / Tab để thoát"` (1 dòng, không đổi logic), rồi tôi chạy lại PlayMode toàn bộ và commit. Không còn việc nào khác bị chặn.
- **Trả lời Planner (2026-10-23):** đồng ý. Brief sót test này (lỗi của Planner). `PlayerBagSowPlayTests.cs` thêm vào Phạm vi, chỉ dòng 361, sửa đúng như đề xuất. Sau đó chạy lại PlayMode toàn bộ → 0 fail, dọn `EditorSettings.asset`, ghi giờ kết thúc, commit (bước 10). Trạng thái về `đang làm`.

---

## Kiểm chứng
- design: `check_design.py` exit 0 (verify_run).
- Chưa kiểm được qua verify_run: client_test / client_test_play — `tools/unity_test.py` cần Editor tắt, Editor đang mở qua MCP (0010). Thay bằng `run_tests` MCP trên code đã commit 0ce293a: EditMode 531/531, PlayMode 189/189, `read_console` 0 lỗi.
- Checklist: Game.Core/PlayerMotion.cs chỉ `using System; Game.Core.Sim` (đạt); không đổi `design/` ngoài sim_m1.json (đạt); giờ thực tế 17:41–17:58 (~17 phút).

## Review
- Chạy bằng `claude-sonnet-5-5 · medium` khớp Model thực thi `Sonnet · medium`.
- Phần cần test #1–#17 đều có kết quả (cập nhật cuối: EditMode 531/531, PlayMode 189/189, `read_console` 0 lỗi); đoạn "chưa commit" ở đầu Kết quả test là ghi chú cũ, đã được thay bằng dòng Cập nhật cuối.
- `git show --stat 0ce293a`: 23 file, đều trong Phạm vi (+ `PlayerBagSowPlayTests.cs:361` đúng phần Planner bổ sung); `TerrainEditPlayTests.cs` không nằm trong commit (vẫn là thay đổi dở của phiên khác).
- Quyết định đã chốt — đối chiếu diff: đích quy về độ cao chân (`PlayerCharacter.MoveTo`), đi thẳng, tới đích ≤ `ArriveDistance` 0,05; kẹt = quãng thực < 25 % trong `stuckMs` → `Blocked`; tự nhảy bậc nhìn trước 0,7 ô, ô tầng chân đặc + 2 ô trên trống (`PlayerMotion.ShouldStepUp`); tia không trúng → mặt phẳng ngang qua chân; dấu click chỉ khi bấm, giữ chỉ khi bấm bắt đầu trên world (`holdFromWorld`); lướt vy = 0, hồi tính từ lúc bắt đầu, dừng khi < 50 %, trên không 1 lần, bỏ qua khi bảng mở (`SimView.AnyPanelOpen`); A = Jump, Shift = Dash, UseTool → chuột phải, bỏ Move/Cancel; 8 số mới trong JSON + kiểm biên; hai `sim_m1.json` cùng diff.
- `Game.Core/PlayerMotion.cs` chỉ `using System; Game.Core.Sim` (No Engine References đạt).
- Ghi nhận nhỏ, không chặn (để ý khi user xem sau 0056 / 7l c): (a) giữ chuột trái gọi `MoveTo` mỗi frame → `stuck.Reset()` mỗi frame, nên đang giữ chuột vào tường sẽ không chuyển `Blocked` (nhân vật cứ đẩy vào tường tới khi nhả); (b) tia `Physics.Raycast` có thể trúng chính capsule của nhân vật khi click lên nhân vật → đích ≈ chỗ đứng (vô hại).
- Giờ thực tế: 17:41–17:58 (~17 phút); cộng vào roadmap khi xong cả 7l b (sau 0056).
- Kết luận: đã duyệt
