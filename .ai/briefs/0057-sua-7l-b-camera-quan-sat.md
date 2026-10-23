# Brief 0057 — bản sửa 7l b: số camera 10°/9 ô + độ nhạy 0,3; camera quan sát đặt trên ô; xoay camera quan sát khi xem

> Trạng thái: đã duyệt
> Review: đầy đủ
> Model thực thi: Sonnet · medium
> Cập nhật: 2026-10-23 20:20

<!-- Planner (Opus) viết, Executor (Sonnet) làm theo ở PHIÊN KHÁC — executor không có lịch sử chat, không được tự quyết. -->

## Mục tiêu
Bản sửa sau khi user chơi thử bước world **7l b** ([task contract](../tasks/m1-world-7l-dieu-khien-pc.md)), theo quyết định [0035](../../design/decisions/0035-camera-quan-sat-dat-tren-o-xoay-khi-xem.md):
1. Số camera PC: góc cúi mặc định 10°, khoảng cách 9 ô, độ nhạy xoay 0,3; file cài đặt cũ đặt lại một lần (formatVersion 3).
2. **Sửa lỗi "bấm xem camera quan sát mà hình không đổi"**: camera quan sát đặt trên ô được chuột phải (cao hơn mặt trên khối 2 ô), không còn đặt đúng chỗ camera chính.
3. Đang xem camera quan sát: giữ chuột giữa + di chuột để xoay; hướng mới được giữ tới khi đặt lại.

Bắt đầu: kiểm brief 0056 có trạng thái `đã duyệt` và commit `f268ce9` có trong `git log`; không có → dừng, `cần Opus`.

## Bối cảnh cần đọc
Nguyên nhân lỗi (Planner điều tra trong Play, [run] 2026-10-23): `TerrainEditInput.PlaceObservationCamera()` đặt camera quan sát **đúng bằng vị trí + hướng của `Camera.main`** → bấm xem ngay sau khi đặt thì hình y hệt. Xem / quay về / nạp vùng xa đều chạy đúng; không sửa bộ nạp.

Code (đọc hết các đoạn này):
- `client/Assets/Game/Client/ObservationCamera.cs` (222 dòng):
  - `:65–69` `void Update()` — `HandleKeys(GameInput.ToggleObservation.WasPressedThisFrame(), GameInput.Back.WasPressedThisFrame());` rồi kiểm Loading xong → `ShowView()`;
  - `:94–101` `public bool Place(Vector3 position, Quaternion rotation)` — `if (Engaged) return false; if (Point == null) BuildPoint(); Point.SetPositionAndRotation(position, rotation); return true;`;
  - `:114–148` `bool Enter()` — lưu `follow = shownCamera.GetComponent<FollowCamera>()` (`:138`), tắt follow/fly;
  - `:150–155` `void ShowView()` — `shownCamera.transform.SetPositionAndRotation(Point.position, Point.rotation)`;
  - `:158–178` `public void Exit()`.
- `client/Assets/Game/Client/TerrainEditInput.cs`:
  - `:116–126` — nhánh công cụ Camera trong `Update` (hiện: `if (GameInput.UseTool.WasPressedThisFrame()) PlaceObservationCamera(); return;`). `UseTool` = chuột phải ([repo] `Input/GameControls.inputactions:262`);
  - `:196–203` `public bool PlaceObservationCamera()` (không tham số, lấy pose `Camera.main`);
  - `:261–265` `public EditResult UseTool(Vector3Int digCell, Vector3Int placeCell)` — dòng `if (Tool == EditTool.Camera) return PlaceObservationCamera() ? EditResult.Ok : EditResult.NotPlaceable;`, ngay sau đó là kiểm tầm `player.InToolReach(...)` → `EditResult.OutOfReach`;
  - `:300–308` `public bool Pick(Ray ray, out Vector3Int digCell, out Vector3Int placeCell)` — `digCell` = khối bị tia trúng (toạ độ ô local của địa hình);
  - `:392` `if (Tool == EditTool.Camera) text += T("hud.camera_help");`.
- `client/Assets/Game/Client/TerrainChunkView.cs:290–292` `public Vector3Int CellAt(Vector3 worldPoint)` dùng `transform.InverseTransformPoint` → ô local ↔ world: `view.transform.TransformPoint(...)`.
- `client/Assets/Game/Client/PlayerCharacter.cs:267` `public bool InToolReach(Vector3Int cell)`; `:52` `public Vector3 FeetLocal`.
- `client/Assets/Game/Client/FollowCamera.cs`: `:14–20` trường mặc định (`distance = 14f`, `pitch = 55f`, `lookSensitivity = 0.15f`, `invertY`, `defaultPitch = 55f, defaultDistance = 14f`); `:72–78` `ApplyLook` (công thức xoay: `yaw += delta.x * lookSensitivity; pitch = Mathf.Clamp(pitch - delta.y * lookSensitivity * (invertY ? -1f : 1f), minPitch, maxPitch);`); `:95–98` điều kiện đọc chuột giữa (`GameInput.HasPointer`, `GameInput.RotateCamera.IsPressed() && !MinimapView.IsAnyOpen`, `GameInput.Look.ReadValue<Vector2>()`).
- `client/Assets/Game/Client/DebugFlyCamera.cs:17` `lookSensitivity = 0.15f`.
- `client/Assets/Game/Core/PlayerMotion.cs:14–16` (`CameraPitch = 55f`, `CameraDistance = 14f`, `FirstPersonPitch`), `:22–34` `Configure(PlayerConfig p)`.
- `client/Assets/Game/Core/Sim/SimConfig.cs:160–162` (`PlayerConfig.cameraPitch = 55f; cameraDistance = 14f; firstPersonPitch = 15f;`), `:407–412` kiểm biên camera.
- `client/Assets/Game/Core/PlayerSettings.cs:15–31` (`formatVersion = 2`, `lookSensitivity = 0.15f`, `Clamp()` với dòng `if (formatVersion < 2) { ... }` và `ClampF(lookSensitivity, ..., 0.15f)`).
- `design/data/sim_m1.json:39` và bản sao `client/Assets/Game/Client/Resources/Config/sim_m1.json` (phải giống hệt — `ConfigTests` kiểm; `SimConfigTests.ConfigMatchesCodeDefaults` kiểm JSON = mặc định trong code).
- `client/Assets/Game/Client/Resources/Lang/vi.json:317–319` khoá `hud.camera_help`.
- Test: `client/Assets/Game/Tests/Client/ObservationCameraPlayTests.cs` (đọc hết, 295 dòng), `CameraPcPlayTests.cs:100–130, 165–175`, `SettingsPlayTests.cs:204–226`, `Tests/Core/PlayerSettingsTests.cs`, `Tests/Core/PlayerMotionTests.cs:23–40`, `Tests/Core/Sim/SimConfigTests.cs:55–65, 100–110`.

API Unity dùng (đều đã có trong code, 6000.4): `Transform.TransformPoint` ([docs](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/Transform.TransformPoint.html)), `Transform.SetPositionAndRotation` ([docs](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/Transform.SetPositionAndRotation.html)), `Quaternion.Euler` ([docs](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/Quaternion.Euler.html)), `Quaternion.Angle` ([docs](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/Quaternion.Angle.html)), `Mathf.Clamp`. Không dùng API nào khác.

## Quyết định đã chốt (không được đổi)
- Số mới (0035 mục 1): `cameraPitch` **10**, `cameraDistance` **9**, độ nhạy mặc định **0.3** — ở mọi nơi có mặc định (JSON 2 bản, `PlayerConfig`, `PlayerMotion`, `FollowCamera`, `DebugFlyCamera`, `PlayerSettings`).
- `PlayerSettings.formatVersion` = **3**. Đọc file có `formatVersion < 3` → đặt `cameraDistance = 0`, `cameraPitch = 0` (theo game), `lookSensitivity = 0.3`, `formatVersion = 3`. Thay hẳn dòng `< 2` hiện có (v1 cũng đi qua nhánh này).
- Độ cao camera quan sát là số tạm trong JSON: `player.observationHeight` = **2** (ô, tính từ **mặt trên** khối được click), kiểm biên **[1, 10]**, đọc qua `PlayerMotion.ObservationHeight`.
- Vị trí đặt = `view.transform.TransformPoint(new Vector3(cell.x + 0.5f, cell.y + 1f + PlayerMotion.ObservationHeight, cell.z + 0.5f))`, `cell` = `digCell` của `Pick`. Hướng = **cả góc ngang + góc cúi** của `Camera.main` lúc đặt (`cam.transform.rotation`, chuẩn hoá qua yaw/pitch ở bước 5).
- Đặt chỉ khi ô trong tầm (`player.InToolReach(cell)`; không có nhân vật → bỏ qua kiểm tầm). Ngoài tầm: `UseTool` trả `EditResult.OutOfReach`, không đổi dòng kết quả HUD, không dời camera. **Không** tự đi tới 2 ô, không thêm vòng tầm (7l c).
- Xoay khi xem: chỉ khi `State == Viewing`. Công thức giống `FollowCamera.ApplyLook`; độ nhạy + đảo trục lấy từ `FollowCamera` đã lưu ở `Enter()` (`follow.lookSensitivity`, `follow.invertY` — Cài đặt đã đồng bộ vào đây); `follow == null` → `0.3f`, `false`. Góc cúi kẹp **[−80, 85]**. Không thu phóng, không di chuyển. Hướng ghi vào `Point` → giữ cho lần xem sau; `Place` mới → hướng theo camera chính lúc đặt.
- Space / X khi đang xem: không làm gì (FollowCamera đã bị tắt khi xem — không cần code thêm).
- Chuỗi người chơi đọc (vi.json) — chép nguyên văn ở bước 7.

## Phạm vi
- Được sửa:
  - `design/data/sim_m1.json`, `client/Assets/Game/Client/Resources/Config/sim_m1.json`;
  - `client/Assets/Game/Core/Sim/SimConfig.cs`, `client/Assets/Game/Core/PlayerMotion.cs`, `client/Assets/Game/Core/PlayerSettings.cs`;
  - `client/Assets/Game/Client/FollowCamera.cs`, `DebugFlyCamera.cs`, `ObservationCamera.cs`, `TerrainEditInput.cs`, `Resources/Lang/vi.json`;
  - test: `Tests/Core/Sim/SimConfigTests.cs`, `Tests/Core/PlayerMotionTests.cs`, `Tests/Core/PlayerSettingsTests.cs`, `Tests/Client/CameraPcPlayTests.cs`, `Tests/Client/SettingsPlayTests.cs`, `Tests/Client/ObservationCameraPlayTests.cs`.
- Không được sửa: mọi file khác, đặc biệt `design/*.md` (Planner đã cập nhật), `SettingsView.cs`, `PlayerCharacter.cs`, `TerrainChunkView.cs`. `client/Assets/Game/Tests/Client/TerrainEditPlayTests.cs` đang có thay đổi dở của phiên khác — **không sửa, không commit**.

## Các bước
1. **JSON + PlayerConfig.**
   - `design/data/sim_m1.json` dòng 39: `"cameraPitch": 55, "cameraDistance": 14, "firstPersonPitch": 15` → `"cameraPitch": 10, "cameraDistance": 9, "firstPersonPitch": 15, "observationHeight": 2`. Chép **cả file** sang `client/Assets/Game/Client/Resources/Config/sim_m1.json` (giống hệt; `diff` không ra gì).
   - `SimConfig.cs:160–162`: `cameraPitch = 10f; cameraDistance = 9f;` + thêm sau `firstPersonPitch`: `public float observationHeight = 2f;`.
   - `SimConfig.cs` sau kiểm `firstPersonPitch` (`:411–412`): thêm
     `if (!(player.observationHeight >= 1f && player.observationHeight <= 10f)) throw new ArgumentException("player.observationHeight must be in [1, 10].");`
   - Xong khi: hai file JSON giống nhau; biên dịch được.
2. **PlayerMotion.** `CameraPitch = 10f`, `CameraDistance = 9f`; thêm `public static float ObservationHeight { get; private set; } = 2f;` (doc: `/// <summary>Camera quan sát: cao hơn mặt trên khối được click chừng này ô (0035).</summary>`); trong `Configure` thêm `ObservationHeight = p.observationHeight;`.
3. **PlayerSettings.** `formatVersion = 3`; `lookSensitivity = 0.3f`; dòng `if (formatVersion < 2) { ... }` → `if (formatVersion < 3) { cameraDistance = 0f; cameraPitch = 0f; lookSensitivity = 0.3f; formatVersion = 3; }` (đặt trước các dòng kẹp như cũ); fallback của `ClampF(lookSensitivity, ...)` `0.15f` → `0.3f`. Sửa comment nếu có nhắc v2.
4. **FollowCamera / DebugFlyCamera.** `FollowCamera.cs:14–20`: `distance = 9f`, `pitch = 10f`, `lookSensitivity = 0.3f`, `defaultPitch = 10f, defaultDistance = 9f` (min/max giữ nguyên). `DebugFlyCamera.cs:17`: `lookSensitivity = 0.3f`.
5. **ObservationCamera — hướng + xoay.**
   - Thêm `public const float LookPitchMin = -80f, LookPitchMax = 85f;` và trường `float lookYaw, lookPitch;`.
   - `Place(...)`: sau kiểm `Engaged`/`BuildPoint`, tách hướng: `var e = rotation.eulerAngles; lookYaw = e.y; lookPitch = Mathf.Clamp(e.x > 180f ? e.x - 360f : e.x, LookPitchMin, LookPitchMax);` rồi `Point.SetPositionAndRotation(position, Quaternion.Euler(lookPitch, lookYaw, 0f));`.
   - Thêm (doc comment tiếng Anh):
     ```csharp
     /// <summary>Viewing only: rotate the observation pose and the shown camera by a mouse delta (decision 0035). The new direction is kept for later views until Place. False when not Viewing.</summary>
     public bool ApplyLook(Vector2 delta, float sensitivity, bool invertY)
     ```
     Thân: `if (State != ObservationState.Viewing) return false;` · `lookYaw += delta.x * sensitivity;` · `lookPitch = Mathf.Clamp(lookPitch - delta.y * sensitivity * (invertY ? -1f : 1f), LookPitchMin, LookPitchMax);` · `Point.rotation = Quaternion.Euler(lookPitch, lookYaw, 0f);` · `if (shownCamera != null) shownCamera.transform.rotation = Point.rotation;` · `return true;`.
   - `Update()`: sau `HandleKeys(...)` thêm: khi `State == ObservationState.Viewing && GameInput.HasPointer && GameInput.RotateCamera.IsPressed() && !MinimapView.IsAnyOpen && !SettingsView.IsAnyOpen` → `ApplyLook(GameInput.Look.ReadValue<Vector2>(), follow != null ? follow.lookSensitivity : 0.3f, follow != null && follow.invertY);`. (Giữ dòng kiểm Loading → `ShowView()` như cũ.)
   - Sửa summary class (`:8–13`): thêm câu "Placed by right-clicking a block: ObservationHeight cells above its top face, facing like Camera.main. While viewing, middle mouse + move rotates the view; the direction is kept until placed again (decision 0035)."
   - Xong khi: biên dịch được; `Exit()` **không** đụng `lookYaw/lookPitch`.
6. **TerrainEditInput — đặt trên ô.**
   - `PlaceObservationCamera()` → `public bool PlaceObservationCamera(Vector3Int cell)`; summary: `/// <summary>Places the observation camera ObservationHeight cells above the top face of cell, facing like Camera.main (decision 0035); writes the HUD result line. False if no camera, engaged, or cell out of tool reach.</summary>`. Thân: `var cam = Camera.main; if (cam == null) return false; var player = PlayerCharacter.Active; if (player != null && !player.InToolReach(cell)) return false; var pos = view.transform.TransformPoint(new Vector3(cell.x + 0.5f, cell.y + 1f + PlayerMotion.ObservationHeight, cell.z + 0.5f)); if (!Observation.Place(pos, cam.transform.rotation)) return false; lastResult = Format("edit.camera_placed", cell); return true;`
   - `UseTool` dòng Camera → `if (Tool == EditTool.Camera) { var p = PlayerCharacter.Active; if (p != null && !p.InToolReach(digCell)) return EditResult.OutOfReach; return PlaceObservationCamera(digCell) ? EditResult.Ok : EditResult.NotPlaceable; }` (kiểm `Observation.Engaged` ở đầu `UseTool` giữ nguyên).
   - Nhánh `Update` `:120–126`: comment → `// Camera tool: right click on a block places the observation camera above it (decision 0035); sow mode keeps priority.`; thân → `ShowAim(false, default, default); if (GameInput.UseTool.WasPressedThisFrame() && Pick(cam.ScreenPointToRay(pointer), out var camCell, out _)) UseTool(camCell, camCell); return;`.
   - Xong khi: không còn chỗ gọi `PlaceObservationCamera()` không tham số (`grep -rn "PlaceObservationCamera()" client/Assets` rỗng).
7. **vi.json.** Đổi `text` của khoá `hud.camera_help` (giữ vị trí khoá) thành nguyên văn:
   `"Camera: chuột phải lên khối = đặt camera quan sát cao hơn khối 2 ô, hướng theo camera đang nhìn (đặt lại thì thay chỗ cũ) · V hoặc nút Xem camera: xem / quay về · đang xem: giữ chuột giữa để xoay\n"`
   `edit.camera_placed` giữ nguyên.
8. **Test Core (EditMode).**
   - `SimConfigTests.cs:62–63`: 55 → 10, 14 → 9; thêm `Assert.AreEqual(2f, cfg.player.observationHeight);`; trong khối `Bad(...)` (`:105–106`) thêm `Bad(c => c.player.observationHeight = 0.5f, "observationHeight 0.5");` và `Bad(c => c.player.observationHeight = 11f, "observationHeight 11");`.
   - `PlayerMotionTests.ConfigureConvertsMs`: thêm `observationHeight = 3f` vào `new PlayerConfig { ... }` và `Assert.AreEqual(3f, PlayerMotion.ObservationHeight);`; sau `PlayerMotion.Configure(new PlayerConfig());` thêm `Assert.AreEqual(2f, PlayerMotion.ObservationHeight);`.
   - `PlayerSettingsTests`: `DefaultsUnchangedByClamp` → `formatVersion` 3, `lookSensitivity` 0.3f; dòng `:53` 0.15f → 0.3f; `MigratesV1AndOptionalCamera` → đổi tên `MigratesOldVersionsAndOptionalCamera`: v1 `{"formatVersion":1,"cameraDistance":6}` → formatVersion 3, distance 0, pitch 0, lookSensitivity 0.3; v2 `{"formatVersion":2,"cameraDistance":10,"cameraPitch":40,"lookSensitivity":0.15}` → 3, 0, 0, 0.3; v3 `{"formatVersion":3,"cameraDistance":10,"cameraPitch":40,"lookSensitivity":0.2}` → giữ 10, 40, 0.2; giữ đoạn số âm → 0.
9. **Test Client (PlayMode).**
   - `CameraPcPlayTests`: mọi `55f` ở `:104, 106, 125, 173` → `10f`; `14f` ở `:105, 107, 126` → `9f`; `:112` `14.01f` → `9.01f`; `ResetBehindUsesFacing` dòng `follow.SetAngles(200f, 10f);` → `follow.SetAngles(200f, 30f);` (để thấy pitch đổi về 10 khi reset). `LookAndZoomThirdPerson` giữ nguyên.
   - `SettingsPlayTests.Apply_ReachesScene`: `s.lookSensitivity = 0.3f` → `0.4f`, hai assert `0.3f` → `0.4f`.
   - `ObservationCameraPlayTests` — thêm helper:
     ```csharp
     Vector3Int GroundAhead(int dx) { var f = player.FeetLocal; return new Vector3Int(Mathf.FloorToInt(f.x) + dx, Mathf.FloorToInt(f.y) - 1, Mathf.FloorToInt(f.z)); }
     Vector3 AboveCell(Vector3Int c) => view.transform.TransformPoint(new Vector3(c.x + 0.5f, c.y + 1f + PlayerMotion.ObservationHeight, c.z + 0.5f));
     ```
     Sửa / thêm:
     - `PlaceTakesCameraPoseAndReplaces` → đổi tên `PlaceOnCellUsesCameraAnglesAndReplaces`: `follow.enabled = false;` đầu test; `c1 = GroundAhead(2)`, camGo rotation `Euler(30, 45, 0)` → `Assert.IsTrue(tei.PlaceObservationCamera(c1))`, `AssertPose(obs.Point, AboveCell(c1), Euler(30,45,0))`, HUD chứa "Đã đặt camera quan sát"; giữ các assert nút/marker/BlocksPointer; `c2 = GroundAhead(-3)`, rotation `Euler(10, 200, 0)` → cùng `Point`, pose mới; ô ngoài tầm `GroundAhead(30)` → `Assert.IsFalse(tei.PlaceObservationCamera(far))`, pose giữ `c2`; giữ các assert cuối (1 ObservationPoint, 0 Collider, ViewpointCount 1).
     - `CameraToolCycleAndClickPlaces`: chuỗi assert `"đặt camera quan sát tại chỗ và hướng đang nhìn"` → `"chuột phải lên khối = đặt camera quan sát"`; `UseTool(default, default)` → `var c = GroundAhead(2); Assert.AreEqual(EditResult.Ok, tei.UseTool(c, c)); AssertPose(obs.Point, AboveCell(c), r1);` (thêm `follow.enabled = false;` đầu test); thêm `var far = GroundAhead(30); Assert.AreEqual(EditResult.OutOfReach, tei.UseTool(far, far));` và pose vẫn ở `c`.
     - **Mới** `ViewRightAfterPlacingMovesCamera` (regression lỗi "hình không đổi"): follow bật (mặc định); `yield return null;` để follow đặt camera; lưu `before = camGo.transform.position`; `Assert.IsTrue(tei.PlaceObservationCamera(GroundAhead(2)))`; `obs.Toggle()`; `yield return WaitViewing();` → `Assert.Greater(Vector3.Distance(camGo.transform.position, before), 1f)` và `AssertPose(camGo.transform, obs.Point.position, obs.Point.rotation)`; `obs.Toggle()`.
     - **Mới** `LookRotatesWhileViewingAndIsKept`: `follow.enabled = false;` camGo rotation `Euler(20, 0, 0)`; đặt `GroundAhead(2)`; trước khi xem `Assert.IsFalse(obs.ApplyLook(new Vector2(100f, 0f), 0.3f, false))` và `Point.rotation` vẫn `Euler(20,0,0)`; Toggle + `WaitViewing()`; `Assert.IsTrue(obs.ApplyLook(new Vector2(100f, 0f), 0.3f, false))` → góc giữa `Point.rotation` và `Euler(20, 30, 0)` < 0.01 và `camGo.transform.rotation` = `Point.rotation`; `ApplyLook(new Vector2(0f, 1000f), 0.3f, false)` → `Euler(-80, 30, 0)` (kẹp); `ApplyLook(new Vector2(0f, 10f), 0.3f, true)` → `Euler(-77, 30, 0)`; Toggle (về) → `Point.rotation` vẫn `Euler(-77, 30, 0)`; Toggle + `WaitViewing()` → `camGo.transform.rotation` ≈ `Euler(-77, 30, 0)`; Toggle (về); camGo rotation `Euler(25, 90, 0)` + đặt lại `GroundAhead(-3)` → `Point.rotation` ≈ `Euler(25, 90, 0)`. So góc bằng `Quaternion.Angle(...) < 0.01f`.
     - `ExitDuringLoadingCancels`: sau lần `HandleKeys(true, false)` đầu (đang Loading) thêm `Assert.IsFalse(obs.ApplyLook(new Vector2(100f, 0f), 0.3f, false));` và `Quaternion.Angle(obs.Point.rotation, Quaternion.Euler(40f, 0f, 0f)) < 0.01f`.
     - `BackExitsWithoutOpeningSettings`, `NoFreeViewpointRefuses`: `tei.PlaceObservationCamera()` → `tei.PlaceObservationCamera(GroundAhead(2))`.
     - `ToolsAndMovementLockedWhileViewing`: lần đặt đầu → `tei.PlaceObservationCamera(GroundAhead(2))`; `Assert.IsFalse(tei.PlaceObservationCamera())` → `Assert.IsFalse(tei.PlaceObservationCamera(GroundAhead(2)))`.
     - `MarkerHiddenWhileCameraClose`: đặt bằng `PlaceObservationCamera(GroundAhead(2))` (follow đã tắt); `yield return null;` → `Assert.IsTrue(obs.MarkerRendered)` (camera ở xa); `camGo.transform.position = obs.Point.position + new Vector3(1f, 0f, 0f)` → frame sau `IsFalse(MarkerRendered)`; `+ (0, 0, -3)` → `IsTrue`.
     - Sửa summary class test: thêm "brief 0057: place on a cell, look while viewing".
10. **Chạy test + dọn** (mục Lệnh executor chạy). Ghi giờ bắt đầu/kết thúc (`date`) vào Kết quả. Commit code + test (không gồm `TerrainEditPlayTests.cs`, không gồm `EditorSettings.asset`), message `brief 0057: camera defaults 10/9, observation camera on cell + look while viewing`.

## Tiêu chí xong
- EditMode **532/532**, PlayMode **198/198** (196 + 2 test mới), 0 lỗi biên dịch.
- `diff design/data/sim_m1.json client/Assets/Game/Client/Resources/Config/sim_m1.json` rỗng.
- `grep -rn "PlaceObservationCamera()" client/Assets` rỗng; `grep -rn "0.15f" client/Assets/Game/Core/PlayerSettings.cs client/Assets/Game/Client/FollowCamera.cs client/Assets/Game/Client/DebugFlyCamera.cs` rỗng.
- `Game.Core` không thêm `using UnityEngine`.
- Commit chỉ gồm file trong Phạm vi.

## Test sau khi xong

### Phần cần test
1. Số mặc định JSON/code (bước 1–2) · `SimConfigTests` (đọc 10 / 9 / 2, `ConfigMatchesCodeDefaults`, biên observationHeight 0.5 / 11 lỗi), `ConfigTests` (2 bản JSON giống) · đạt.
2. `PlayerMotion.ObservationHeight` (bước 2) · `PlayerMotionTests.ConfigureConvertsMs` (3 rồi về 2) · đạt.
3. Cài đặt v3 + đặt lại file cũ (bước 3) · `PlayerSettingsTests` (mặc định 3 / 0.3; v1, v2 → 0, 0, 0.3; v3 giữ; số âm → 0) · đạt.
4. Camera chính 10° / 9 ô (bước 4) · `CameraPcPlayTests` (DefaultHighAngleFollows, ResetBehindUsesFacing, LookAndZoomThirdPerson…) · đạt.
5. Cài đặt vẫn tới scene (bước 3–4) · `SettingsPlayTests.Apply_ReachesScene` với 0.4 · đạt.
6. Đặt trên ô, hướng theo camera chính, thay chỗ, ngoài tầm (bước 5–6) · `PlaceOnCellUsesCameraAnglesAndReplaces`, `CameraToolCycleAndClickPlaces` · vị trí = `AboveCell`, góc lệch < 0,01°, ngoài tầm `OutOfReach` + không dời.
7. **Lỗi "hình không đổi"** (bước 6) · `ViewRightAfterPlacingMovesCamera` · camera dời > 1 ô khi xem ngay sau đặt.
8. Xoay khi xem, kẹp, đảo trục, giữ hướng, đặt lại thì reset, không xoay khi Off/Loading (bước 5) · `LookRotatesWhileViewingAndIsKept`, `ExitDuringLoadingCancels` · đạt.
9. Chuỗi HUD (bước 7) · `CameraToolCycleAndClickPlaces` chứa chuỗi mới · đạt.
10. Regression camera quan sát cũ · `ViewWaitsForLoadThenShowsAndReturns`, `KeysWithoutCameraDoNothing`, `BackExitsWithoutOpeningSettings`, `ToolsAndMovementLockedWhileViewing`, `NoFreeViewpointRefuses`, `MarkerHiddenWhileCameraClose` · đạt.
11. Regression toàn bộ · cả bộ EditMode + PlayMode · 532 + 198, không test nào khác đổi kết quả.

### Lệnh executor chạy
1. `python3 tools/check_design.py` → exit 0 (đổi `design/data/`).
2. Unity Editor **đang mở** (kiểm `mcpforunity://editor/state`): nếu đang Play → `manage_editor stop`; `read_console` (lọc error) sau biên dịch = 0 lỗi; `run_tests` EditMode rồi PlayMode (MCP, [0010](../../design/decisions/0010-mcp-for-unity.md)); ghi dòng tổng kết. Sau PlayMode: `git checkout -- client/ProjectSettings/EditorSettings.asset`. Ghi `Chưa kiểm được: verify_run client_test/client_test_play — Editor đang mở` cho hook.
   Unity Editor **tắt**: `sh "/Users/macvn/.claude/plugins/cache/ai-workflow-kit/ai-workflow-kit/0.15.1/scripts/run.sh" verify_run design client_test client_test_play`.
3. `git status --short` → chỉ file trong Phạm vi (+ `TerrainEditPlayTests.cs` dở, không stage).

### User tự test
Không (user: lần chơi thử tiếp theo gộp sau 7l e). Planner tự kiểm bằng hình qua MCP khi review nếu cần.

## Dừng và chuyển lại Opus khi
- Brief mơ hồ hoặc mâu thuẫn với code thật (vd số dòng lệch nhiều, chữ ký khác).
- Cần một quyết định thuộc AGENTS.md §3.4 hoặc ngoài "Quyết định đã chốt".
- Cùng một lỗi sửa 2 lần vẫn chưa qua.
- Phải sửa file ngoài "Phạm vi" (vd test khác hỏng vì số camera mới).

---
<!-- Từ đây trở xuống: EXECUTOR ghi. -->

## Kết quả
- Base commit: 333abca · Chạy bằng: Sonnet 5.5 · medium
- Giờ: bắt đầu 19:48, kết thúc 20:02 (đồng hồ máy; thực tế dài hơn do test ~10 phút — EditMode chạy 2 lần vì lần đầu dùng DLL cũ).
- Commit: 999cdfa — 16 file trong Phạm vi (không gồm TerrainEditPlayTests.cs).
- Lệch nhỏ: trong UseTool đặt tên biến `cp` thay `p` (trùng biến `player`/`p` ở phạm vi bao); `MarkerHiddenWhileCameraClose` đổi thứ tự bước (xa → gần → xa) nhưng cùng ý.
- Chưa kiểm được: verify_run client_test/client_test_play — Editor đang mở (đã chạy bằng MCP).

## Kết quả test
1. Số mặc định (SimConfigTests, ConfigTests) · đạt (EditMode 532/532).
2. PlayerMotion.ObservationHeight · đạt. 3. PlayerSettings v3 · đạt.
4. Camera 10°/9 ô (CameraPcPlayTests) · đạt. 5. SettingsPlayTests 0.4 · đạt.
6–9. PlaceOnCellUsesCameraAnglesAndReplaces, CameraToolCycleAndClickPlaces, ViewRightAfterPlacingMovesCamera, LookRotatesWhileViewingAndIsKept, ExitDuringLoadingCancels · đạt (PlayMode 198/198).
10–11. Regression · đạt.
Lệnh: check_design.py → ✓ hợp lệ; diff 2 JSON → rỗng; grep PlaceObservationCamera() / 0.15f → rỗng; EditorSettings.asset đã checkout; git status chỉ còn TerrainEditPlayTests.cs (dở, không stage).

## Vướng mắc
Không.

---
<!-- PLANNER ghi khi review. -->

## Review
- Chạy bằng: Sonnet 5.5 · medium = Model thực thi · đạt. 11/11 dòng Phần cần test có kết quả.
- Diff 999cdfa (16 file, đều trong Phạm vi; `TerrainEditPlayTests.cs` không commit): số 10/9/0.3 ở JSON 2 bản, PlayerConfig, PlayerMotion, FollowCamera, DebugFlyCamera, PlayerSettings · đạt; `formatVersion` 3, nhánh `< 3` thay `< 2`, đặt lại 0/0/0.3 · đạt; `observationHeight` 2, biên [1, 10] · đạt.
- `PlaceObservationCamera(cell)`: vị trí `TransformPoint(cell + (0.5, 1 + ObservationHeight, 0.5))`, hướng theo `Camera.main`, kiểm tầm; `UseTool` trả `OutOfReach` trước khi đặt, không đổi HUD · đúng Quyết định đã chốt. Nhánh Update dùng `Pick` → `digCell`.
- `ApplyLook`: chỉ khi Viewing, công thức như FollowCamera, kẹp [−80, 85], ghi vào `Point` + camera đang hiện; `Place` tách yaw/pitch; `Exit` không đụng · đạt. FollowCamera bị tắt khi xem → không xoay đôi.
- Lệch nhỏ chấp nhận: biến `cp`; `MarkerHiddenWhileCameraClose` đổi thứ tự (assert "không che ngay sau khi đặt" không còn nghĩa vì camera quan sát không còn ở chỗ camera chính).
- Test: EditMode 532/532, PlayMode 198/198 (MCP) — không chạy lại (đủ, khớp số dự kiến). `check_design.py` ✓ sau khi Planner ghi giờ.
- Giờ: Executor 14 phút; ghi vào roadmap dòng 7l b + world-tech 7l b (tổng 7l b Executor 44 phút).
- Kết luận: đã duyệt
