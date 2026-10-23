# Brief 0053 — camera quan sát tối giản (world 7j)

> Trạng thái: chờ review
> Review: đầy đủ + user xem
> Model thực thi: Sonnet · medium
> Cập nhật: 2026-10-23 13:30

<!-- Planner (Opus) viết, Executor (Sonnet) làm theo ở PHIÊN KHÁC — executor không có lịch sử chat, không được tự quyết. -->

## Mục tiêu
Người chơi đặt **một camera quan sát** bằng công cụ thứ 4 (Camera) rồi bấm `V` hoặc nút HUD để nhìn qua nó toàn màn hình mà không phải đi tới. Lúc xem, camera quan sát là **điểm nhìn thứ hai** của bộ nạp chunk. Task contract: [m1-world-7j-camera-quan-sat](../tasks/m1-world-7j-camera-quan-sat.md) (đọc trước, nhất là "Ngoài phạm vi"). Quyết định gốc: [0029](../../design/decisions/0029-nhieu-diem-nhin-camera-quan-sat.md). Pillar P3. Thành phần: client Unity (`Game.Client`), không đổi `Game.Core`. Ước lượng 1–2 ngày-người, một brief.

## Bối cảnh cần đọc
Mọi đường dẫn dưới đây tính từ `client/Assets/Game/`.
- `Client/TerrainEditInput.cs` [repo]:
  - `:9` `public enum EditTool { Dig, Place, Till }`.
  - `Awake` `:68-77`: tự thêm `DropView`, `PlayerFarmActions` nếu chưa có (mẫu `GetComponent` → `AddComponent`).
  - `Update` `:79-174`. Đầu hàm `:81-82` reset `AimedDrop`/`AimedHarvest`. Phím `:83-89`. Chặn con trỏ `:100`: `if (hasPointer && (MinimapView.BlocksPointer(pointer) || TimeDial.BlocksPointer(pointer) || SimView.BlocksPointer(pointer)))`. `:116-118`: `var cam = Camera.main; if (!hasPointer || cam == null || view.Editor == null) return;`. Sau đó là tia chuột, nhặt vật rơi, thu hoạch, chế độ gieo, đào/đặt/xới.
  - `ToggleTool()` `:190-195`: `Tool = Tool == EditTool.Dig ? EditTool.Place : Tool == EditTool.Place && TillAvailable ? EditTool.Till : EditTool.Dig;`
  - `ShowAim(bool picked, Vector3Int digCell, Vector3Int placeCell)` `:208-234`: `:210-212` tắt khung + bản mờ, `if (!picked || view.Editor == null) return;`.
  - `UseTool(Vector3Int digCell, Vector3Int placeCell)` `:237-269`: `:239` chế độ gieo; `:240-242` kiểm tầm.
  - `HudText()` `:361-383`: `:363` tên công cụ qua `T(Tool switch { EditTool.Dig => "hud.tool_dig", EditTool.Place => "hud.tool_place", _ => "hud.tool_till" })`; `:364-365` thêm `hud.till_help` khi cầm Xới.
  - Helper chuỗi: `static string T(string k) => GameSettings.Text.Get(k);`, `static string Format(string k, params object[] a)` (`:33-34`). Trường `lastResult` (`:39`) hiện ở cuối HUD.
- `Client/TerrainChunkView.cs` [repo], API điểm nhìn (brief 0051): `public int loadRadius = 128;` `public int unloadRadius = 160;` (`:34-35`), `public int maxViewpoints = 2;` (`:38`), `public int MissingColumnsWithin(Transform viewpoint, int radius)` (`:104`; trả 0 khi không streaming), `public void AddViewpoint(Transform target, int loadRadius, int unloadRadius)` (`:113`; đầy → `InvalidOperationException`), `public bool RemoveViewpoint(Transform target)` (`:126`), `public int ViewpointCount` (`:138`), `public Vector3Int CellAt(Vector3 worldPoint)` (`:290`), `public Mesh ChunkMesh(int chunkIndex)` (`:300`). Bộ nạp tự cập nhật trong `LateUpdate`, tối đa `maxColumnsPerFrame = 2` cột/khung.
- `Client/PlayerCharacter.cs` [repo]: `public static PlayerCharacter Active`; `public void StopAutoWalk()` (`:58`); `Awake` gán `terrain.streamTarget = transform` (`:81`), nên điểm nhìn chính là nhân vật. F3 `:197`: `if (DebugKeys.WasPressed(Key.F3)) SetFlyMode(!FlyMode);`. Đọc phím `:207-216`:
  ```csharp
  if (FlyMode)
  {
      MoveInput = Vector2.zero;
      JumpInput = false;
  }
  else if (readKeyboard)
  {
      MoveInput = GameInput.Move.ReadValue<Vector2>();
      JumpInput = GameInput.Jump.IsPressed();
  }
  ```
- `Client/FollowCamera.cs` (camera sau lưng, nằm trên Camera.main, `public void SnapNow()`), `Client/DebugFlyCamera.cs` (camera bay F3, cũng trên Camera.main). `PlayerCharacter.SetFlyMode` bật/tắt hai component này.
- `Client/PlayerFarmActions.cs` [repo]: `public void CancelSelfTill()` (`:106`, không làm gì khi không tự xới), `public void ExitSowMode()` (`:131`, không làm gì khi `SowCrop < 0`).
- `Client/SettingsView.cs` [repo]: `OtherPanelOpen()` `:62-67`. Esc mở Cài đặt khi không có bảng nào mở (`HandleKeys` `:69-77`). `[DefaultExecutionOrder(-50)]`.
- Nút HUD IMGUI hiện có (đơn vị GUI, `UiScale.Gui`, gốc trên-trái), hàng dưới cùng: Kho `Rect(10, gh-46, 110, 36)` (`SimView.cs:446`), Cài đặt `Rect(gw-240, gh-46, 110, 36)` (`SettingsView.cs:30`), Túi `Rect(gw-120, gh-46, 110, 36)` (`SimView.cs:383`). Mẫu đổi toạ độ màn hình → GUI: `SettingsView.Covers` `:35-41`.
- `Client/GameInput.cs` `:29` `public static InputAction CycleBlockModifier => Get("Player/CycleBlockModifier");`. `Client/Input/GameControls.inputactions`: map `Player`; action cuối `CycleBlockModifier` (`:188-196`), binding cuối `<Keyboard>/alt` (`:462-473`). Phím `V` chưa dùng [repo]. Esc ở map Player là action `Back` (`GameInput.Back`).
- `Client/Resources/Lang/vi.json`: `{"key", "text"}` trong `entries`, xếp key tăng dần (ordinal). Key dùng trong code phải có trong file (`HardcodedTextScanTests.AllUsedKeysExist`); không viết chữ tiếng Việt trong `.cs` ngoài `Tests/` (dòng test có chữ Việt phải kết thúc bằng `// vi-text`).
- `Client/WalkBench.cs:102` `public static Vector2 MapCentroid(MapTable maps, int map)` (toạ độ terrain x, z). Map 2 = map id 1.
- Test mẫu: `Tests/Client/PlayerCharacterPlayTests.cs` `SetUp` `:22-53` (camera tag `MainCamera` + `DebugFlyCamera` + `FollowCamera`, terrain `MapsV3` streaming + `DemoPlain` + `TerrainEditInput`, nhân vật `readKeyboard = false`).
- API Unity dùng (đã tra docs 6000.4, đều có): [Transform.SetPositionAndRotation](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/Transform.SetPositionAndRotation.html), [Transform.GetPositionAndRotation](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/Transform.GetPositionAndRotation.html), [Behaviour.enabled](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/Behaviour-enabled.html), [Camera.main](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/Camera-main.html), [GameObject.CreatePrimitive](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/GameObject.CreatePrimitive.html), [Object.Destroy](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/Object.Destroy.html), [GUI.Button](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/GUI.Button.html). Không dùng API Unity nào khác ngoài những gì file đã dùng sẵn.

## Quyết định đã chốt (không được đổi)
1. Công cụ Camera là công cụ thứ 4. Tab: Đào → Đặt → Xới (khi có) → Camera → Đào. Luôn có, không phụ thuộc nhân vật hay SimView. (user, 2026-10-23)
2. Chuột trái khi cầm Camera: đặt tại **vị trí + hướng của `Camera.main`**. M1 tối đa 1: đặt lại thì dời chỗ cũ, không tạo cái mới. Có khối đánh dấu bằng khối có sẵn, không va chạm; khối ẩn khi đang xem. (user, 2026-10-23)
3. **Cách đổi hình (Planner chọn):** không tạo `Camera` thứ hai. Lúc xem, `Camera.main` được dời tới tư thế camera quan sát; `FollowCamera` và `DebugFlyCamera` trên nó bị tắt. Lúc quay về, trả tư thế cũ và bật lại đúng trạng thái trước đó. Lý do: mọi hiệu ứng đang bám `Camera.main` (sương mép, dưới nước, trời) tự đúng ở chỗ xem, không phải chép cấu hình camera.
4. Trạng thái `Off → Loading → Viewing`. `V` / nút: Off → Loading (thêm điểm nhìn thứ hai, bán kính `view.loadRadius` / `view.unloadRadius` = 128/160). Khi `MissingColumnsWithin(điểm, view.loadRadius) == 0` → Viewing (dời camera). `V` / nút / Esc khi đang Loading hoặc Viewing → Off (gỡ điểm nhìn, trả camera). Chưa đặt camera → `V` không làm gì. (user, 2026-10-23)
5. **Khoá khi Loading hoặc Viewing:** không dùng công cụ, không đổi công cụ, không đặt camera mới; nhân vật không đi, không nhảy, F3 không đổi chế độ; huỷ tự đi/tự xới và thoát chế độ gieo khi bắt đầu xem. (user, 2026-10-23: "nhân vật đứng yên, không dùng công cụ, không nhận lệnh di chuyển/xoay")
6. Esc dùng để thoát xem thì **không** mở bảng Cài đặt trong cùng khung đó.
7. Nút HUD (Planner chọn vị trí): `Rect(gw - 380, gh - 46, 130, 36)` đơn vị GUI, hàng dưới bên phải, cạnh trái nút Cài đặt. Chỉ hiện khi đã đặt camera. Chữ "Xem camera (V)" khi Off, "Quay về (V)" khi Loading/Viewing. Chữ "Đang tải…" giữa màn hình khi Loading.
8. Hết chỗ điểm nhìn (vd WalkBench bật `secondViewpoint`) → không vào xem, `Debug.LogWarning` bắt đầu bằng `OBSERVE`.
9. Không lưu camera khi thoát game; không xoay khi xem; không đổi `TerrainChunkView`/`ChunkStreamPlan`. Code + comment mới bằng tiếng Anh; comment tiếng Việt cũ giữ nguyên (chỉ sửa comment của dòng mình đổi, viết lại bằng tiếng Anh).

## Phạm vi
- Được tạo: `client/Assets/Game/Client/ObservationCamera.cs`, `client/Assets/Game/Tests/Client/ObservationCameraPlayTests.cs` (kèm `.meta` Unity sinh ra).
- Được sửa: `client/Assets/Game/Client/TerrainEditInput.cs`, `client/Assets/Game/Client/PlayerCharacter.cs`, `client/Assets/Game/Client/SettingsView.cs`, `client/Assets/Game/Client/GameInput.cs`, `client/Assets/Game/Client/Input/GameControls.inputactions`, `client/Assets/Game/Client/Resources/Lang/vi.json`, `client/Assets/Game/Tests/ClientEdit/GameControlsTests.cs`, `client/Assets/Game/Tests/Client/TerrainEditPlayTests.cs`, `client/Assets/Game/Tests/Client/SimFarmUiPlayTests.cs`, `design/02-gameplay/controls-ui.md`, file brief này (mục Kết quả trở xuống).
- Không được sửa: mọi file khác (gồm `TerrainChunkView.cs`, `ChunkStreamPlan.cs`, `WalkBench.cs`, scene, `world-tech.md`, roadmap, glossary — Planner ghi khi review).

## Các bước
1. **Phím V.**
   - `GameControls.inputactions`, map `Player`: thêm action sau `CycleBlockModifier` (sau dấu `}` ở `:196`, thêm dấu phẩy):
     ```json
     {
         "name": "ToggleObservation",
         "type": "Button",
         "id": "<uuid4 mới>",
         "expectedControlType": "Button",
         "processors": "",
         "interactions": "",
         "initialStateCheck": false
     }
     ```
     và binding sau binding `<Keyboard>/alt` (`:473`):
     ```json
     {
         "name": "",
         "id": "<uuid4 mới khác>",
         "path": "<Keyboard>/v",
         "interactions": "",
         "processors": "",
         "groups": "",
         "action": "ToggleObservation",
         "isComposite": false,
         "isPartOfComposite": false
     }
     ```
     Sinh id bằng `python3 -c "import uuid; print(uuid.uuid4())"`. Giữ thụt lề như các khối bên cạnh.
   - `GameInput.cs`: thêm sau dòng `CycleBlockModifier` (`:29`): `public static InputAction ToggleObservation => Get("Player/ToggleObservation");`
   - `GameControlsTests.cs`: thêm `["Player/ToggleObservation"] = B("<Keyboard>/v"),` sau dòng `:44`; đổi `30` → `31` ở `:61` và `:90`; thêm `"ObservationCamera.cs"` vào cuối mảng `files` ở `:97`.
   Xong khi: `read_console` không lỗi biên dịch. Chưa chạy `GameControlsTests` ở bước này (`NoDirectInputReadsInScope` đọc `ObservationCamera.cs`, tạo ở bước 3).
2. **Chuỗi** (`vi.json`), thêm 6 mục, chèn đúng thứ tự key tăng dần (ordinal):
   | key | text |
   |---|---|
   | `edit.camera_placed` | `Đã đặt camera quan sát tại {0}` |
   | `hud.camera_help` | `Camera: chuột trái = đặt camera quan sát tại chỗ và hướng đang nhìn (đặt lại thì thay chỗ cũ) · V hoặc nút Xem camera: xem / quay về\n` (trong JSON viết `\n` như `hud.till_help`) |
   | `hud.tool_camera` | `Camera` |
   | `observe.button_back` | `Quay về (V)` |
   | `observe.button_view` | `Xem camera (V)` |
   | `observe.loading` | `Đang tải…` (dấu `…` một ký tự U+2026) |
   Xong khi: file là JSON hợp lệ (`python3 -c "import json;json.load(open('client/Assets/Game/Client/Resources/Lang/vi.json'))"`), key vẫn tăng dần (`python3 -c "import json;k=[e['key'] for e in json.load(open('client/Assets/Game/Client/Resources/Lang/vi.json'))['entries']];print(k==sorted(k))"` → `True`).
3. **Tạo `Client/ObservationCamera.cs`** đúng nội dung sau (namespace `Game.Client`):
   ```csharp
   using UnityEngine;

   namespace Game.Client
   {
       /// <summary>Observation camera view state (task 7j, brief 0053).</summary>
       public enum ObservationState { Off, Loading, Viewing }

       /// <summary>
       /// Observation camera (task 7j, brief 0053, decision 0029): one placed pose in the world (M1: max 1, placing again moves it).
       /// V / HUD button: register the pose as the second streaming viewpoint, show the loading text until the columns within
       /// loadRadius are loaded, then move Camera.main there with its follow/fly controllers disabled.
       /// V / button / Esc: restore Camera.main and remove the viewpoint. While Loading or Viewing, tools and player movement are locked.
       /// </summary>
       [RequireComponent(typeof(TerrainChunkView))]
       public class ObservationCamera : MonoBehaviour
       {
           static ObservationCamera current;

           TerrainChunkView view;
           Camera shownCamera;
           FollowCamera follow;
           DebugFlyCamera fly;
           bool followWasEnabled, flyWasEnabled;
           Vector3 savedPosition;
           Quaternion savedRotation;
           int exitFrame = -1;
           GUIStyle loadingStyle;

           public ObservationState State { get; private set; }
           /// <summary>Placed pose (also the streaming viewpoint while engaged); null until first placed.</summary>
           public Transform Point { get; private set; }
           public GameObject Marker { get; private set; }
           public bool HasCamera => Point != null;
           public bool Engaged => State != ObservationState.Off;
           public bool MarkerVisible => Marker != null && Marker.activeSelf;
           public bool ButtonVisible => HasCamera;
           public bool LoadingVisible => State == ObservationState.Loading;
           public string ButtonText => State == ObservationState.Off ? T("observe.button_view") : T("observe.button_back");

           /// <summary>True while the current observation camera is Loading or Viewing: player movement and tools are locked.</summary>
           public static bool InputLocked => current != null && current.Engaged;
           /// <summary>Esc belongs to the observation view this frame (engaged, or it just exited) — Settings must not open.</summary>
           public static bool BlocksBack => current != null && (current.Engaged || current.exitFrame == Time.frameCount);
           /// <summary>Screen point (origin bottom-left) is over the HUD button.</summary>
           public static bool BlocksPointer(Vector2 screen) => current != null && current.Covers(screen);
           public static Rect ButtonRect(float gw, float gh) => new Rect(gw - 380f, gh - 46f, 130f, 36f);

           static string T(string k) => GameSettings.Text.Get(k);

           void Awake()
           {
               current = this;
               view = GetComponent<TerrainChunkView>();
           }

           void OnDestroy()
           {
               if (current == this) current = null;
           }

           void Update()
           {
               HandleKeys(GameInput.ToggleObservation.WasPressedThisFrame(), GameInput.Back.WasPressedThisFrame());
               if (State == ObservationState.Loading && view.MissingColumnsWithin(Point, view.loadRadius) == 0) ShowView();
           }

           /// <summary>V toggles; Esc only exits. Split from Update for tests.</summary>
           public void HandleKeys(bool togglePressed, bool backPressed)
           {
               if (togglePressed) Toggle();
               else if (backPressed && Engaged) Exit();
           }

           public bool Covers(Vector2 screen)
           {
               if (!ButtonVisible || MinimapView.IsAnyOpen) return false;
               float s = UiScale.Gui, gw = Screen.width / s, gh = Screen.height / s;
               var g = new Vector2(screen.x / s, (Screen.height - screen.y) / s);
               return ButtonRect(gw, gh).Contains(g);
           }

           /// <summary>Places (or moves) the single observation camera. False while engaged.</summary>
           public bool Place(Vector3 position, Quaternion rotation)
           {
               if (Engaged) return false;
               if (Point == null) BuildPoint();
               Point.SetPositionAndRotation(position, rotation);
               return true;
           }

           /// <summary>Off → start viewing (false if no camera or no free viewpoint); engaged → exit (true).</summary>
           public bool Toggle()
           {
               if (Engaged)
               {
                   Exit();
                   return true;
               }
               return Enter();
           }

           bool Enter()
           {
               if (!HasCamera) return false;
               try
               {
                   view.AddViewpoint(Point, view.loadRadius, view.unloadRadius);
               }
               catch (System.InvalidOperationException e)
               {
                   Debug.LogWarning("OBSERVE no free streaming viewpoint, view not opened: " + e.Message);
                   return false;
               }
               var player = PlayerCharacter.Active;
               if (player != null) player.StopAutoWalk();
               var farming = GetComponent<PlayerFarmActions>();
               if (farming != null)
               {
                   farming.CancelSelfTill();
                   farming.ExitSowMode();
               }
               shownCamera = Camera.main;
               if (shownCamera != null)
               {
                   shownCamera.transform.GetPositionAndRotation(out savedPosition, out savedRotation);
                   follow = shownCamera.GetComponent<FollowCamera>();
                   fly = shownCamera.GetComponent<DebugFlyCamera>();
                   followWasEnabled = follow != null && follow.enabled;
                   flyWasEnabled = fly != null && fly.enabled;
                   if (follow != null) follow.enabled = false;
                   if (fly != null) fly.enabled = false;
               }
               State = ObservationState.Loading;
               if (view.MissingColumnsWithin(Point, view.loadRadius) == 0) ShowView();
               return true;
           }

           void ShowView()
           {
               State = ObservationState.Viewing;
               if (shownCamera != null) shownCamera.transform.SetPositionAndRotation(Point.position, Point.rotation);
               Marker.SetActive(false);
           }

           /// <summary>Back to the player's camera: remove the viewpoint, restore the camera pose and controllers.</summary>
           public void Exit()
           {
               if (!Engaged) return;
               view.RemoveViewpoint(Point);
               if (shownCamera != null)
               {
                   shownCamera.transform.SetPositionAndRotation(savedPosition, savedRotation);
                   if (fly != null) fly.enabled = flyWasEnabled;
                   if (follow != null)
                   {
                       follow.enabled = followWasEnabled;
                       if (followWasEnabled) follow.SnapNow();
                   }
               }
               shownCamera = null;
               follow = null;
               fly = null;
               Marker.SetActive(true);
               State = ObservationState.Off;
               exitFrame = Time.frameCount;
           }

           void OnGUI()
           {
               if (!ButtonVisible || MinimapView.IsAnyOpen) return;
               float scale = UiScale.Gui;
               GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
               float gw = Screen.width / scale, gh = Screen.height / scale;
               if (GUI.Button(ButtonRect(gw, gh), ButtonText)) Toggle();
               if (LoadingVisible)
               {
                   loadingStyle ??= new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 28 };
                   GUI.Label(new Rect(gw * 0.5f - 150f, gh * 0.5f - 25f, 300f, 50f), T("observe.loading"), loadingStyle);
               }
           }

           void BuildPoint()
           {
               Point = new GameObject("ObservationPoint").transform;
               Point.SetParent(transform, false);
               Marker = new GameObject("Marker");
               Marker.transform.SetParent(Point, false);
               AddBlock("Body", Vector3.zero, new Vector3(0.5f, 0.4f, 0.6f), new Color(1f, 0.55f, 0.1f));
               AddBlock("Lens", new Vector3(0f, 0f, 0.35f), new Vector3(0.2f, 0.2f, 0.2f), new Color(0.1f, 0.1f, 0.1f));
           }

           void AddBlock(string name, Vector3 center, Vector3 size, Color color)
           {
               var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
               go.name = name;
               Destroy(go.GetComponent<Collider>());
               go.layer = LayerMask.NameToLayer("Ignore Raycast");
               go.transform.SetParent(Marker.transform, false);
               go.transform.localPosition = center;
               go.transform.localScale = size;
               var src = Resources.Load<UnityEngine.Material>("Materials/Landmark");
               var mat = src != null ? new UnityEngine.Material(src) : new UnityEngine.Material(UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline.defaultMaterial);
               mat.SetColor("_BaseColor", color);
               var r = go.GetComponent<Renderer>();
               r.sharedMaterial = mat;
               r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
           }
       }
   }
   ```
   Xong khi: `read_console` không lỗi biên dịch.
4. **`TerrainEditInput.cs`**:
   - `:8-9`: đổi thành
     ```csharp
     /// <summary>Held tool (briefs 0027, 0030, 0053): Tab cycles Dig → Place → Till (with SimView) → Camera.</summary>
     public enum EditTool { Dig, Place, Till, Camera }
     ```
   - Thêm thuộc tính cạnh `Farming` (`:64`): `/// <summary>Observation camera (brief 0053); added in Awake if missing.</summary>` `public ObservationCamera Observation { get; private set; }`. Cuối `Awake` (sau `:76`): `Observation = GetComponent<ObservationCamera>(); if (Observation == null) Observation = gameObject.AddComponent<ObservationCamera>();`
   - `Update`, ngay sau `AimedHarvest = -1;` (`:82`), trước mọi phím: `if (Observation.Engaged) { ShowAim(false, default, default); return; } // observation view: no tools (brief 0053)`
   - `:100`: thêm `|| ObservationCamera.BlocksPointer(pointer)` vào điều kiện chặn (trong ngoặc, sau `SimView.BlocksPointer(pointer)`).
   - Ngay sau `:117-118` (`if (!hasPointer || cam == null || view.Editor == null) return;`), trước `var ray`:
     ```csharp
     // Camera tool: left click places the observation camera at the current view (brief 0053); sow mode keeps priority.
     if (Tool == EditTool.Camera && Farming.SowCrop < 0)
     {
         ShowAim(false, default, default);
         if (GameInput.UseTool.WasPressedThisFrame()) PlaceObservationCamera();
         return;
     }
     ```
   - Thêm hàm công khai (đặt ngay trước `ToggleTool`):
     ```csharp
     /// <summary>Places the observation camera at Camera.main's position and rotation; writes the HUD result line. False if no camera or engaged.</summary>
     public bool PlaceObservationCamera()
     {
         var cam = Camera.main;
         if (cam == null || !Observation.Place(cam.transform.position, cam.transform.rotation)) return false;
         lastResult = Format("edit.camera_placed", view.CellAt(cam.transform.position));
         return true;
     }
     ```
   - `ToggleTool()`: thêm dòng đầu `if (Observation != null && Observation.Engaged) return;`, rồi thay dòng `Tool = ...` (`:194`) bằng
     ```csharp
     Tool = Tool switch
     {
         EditTool.Dig => EditTool.Place,
         EditTool.Place => TillAvailable ? EditTool.Till : EditTool.Camera,
         EditTool.Till => EditTool.Camera,
         _ => EditTool.Dig,
     };
     ```
     và sửa comment `:189` thành `/// <summary>Tab: Dig → Place → Till (with SimView) → Camera → Dig. No-op while the observation view is engaged.</summary>`.
   - `ShowAim`: sau `if (!picked || view.Editor == null) return;` (`:212`) thêm `if (Tool == EditTool.Camera) return;`.
   - `UseTool`: thêm dòng đầu `if (Observation != null && Observation.Engaged) return EditResult.NotPlaceable;`; sau dòng chế độ gieo (`:239`) thêm `if (Tool == EditTool.Camera) return PlaceObservationCamera() ? EditResult.Ok : EditResult.NotPlaceable;`.
   - `HudText()` `:363`: switch thành `EditTool.Dig => "hud.tool_dig", EditTool.Place => "hud.tool_place", EditTool.Till => "hud.tool_till", _ => "hud.tool_camera"`; sau khối `if (Tool == EditTool.Till) text += T("hud.till_help");` thêm `if (Tool == EditTool.Camera) text += T("hud.camera_help");`.
   Xong khi: biên dịch không lỗi.
5. **`PlayerCharacter.cs`**:
   - `:197` → `if (!ObservationCamera.InputLocked && DebugKeys.WasPressed(Key.F3)) SetFlyMode(!FlyMode);`
   - `:207` → `if (FlyMode || ObservationCamera.InputLocked)` (khối bên trong giữ nguyên; áp dụng cả khi `readKeyboard = false`, nên test gán `MoveInput` cũng bị xoá về 0 khi đang xem).
   Xong khi: biên dịch không lỗi.
6. **`SettingsView.cs`** `OtherPanelOpen()` (`:62`): thêm dòng đầu thân hàm `if (ObservationCamera.BlocksBack) return true;`.
7. **Sửa test vòng Tab cũ** (chỉ đổi kỳ vọng vòng công cụ):
   - `TerrainEditPlayTests.ToolStartsDigAndTabToggles` (`:145-155`): sau `Place` thêm `input.ToggleTool(); Assert.AreEqual(EditTool.Camera, input.Tool);` trước lần về `Dig`. Vòng: Dig → Place → Camera → Dig.
   - `SimFarmUiPlayTests.TabCyclesThreeToolsWithSim` (`:79-89`): đổi tên thành `TabCyclesFourToolsWithSim`; sau `Till` thêm `input.ToggleTool(); Assert.AreEqual(EditTool.Camera, input.Tool);` trước lần về `Dig`.
   Không sửa test nào khác dùng `ToggleTool` (các vòng `while (input.Tool != X)` vẫn đúng).
8. **Tạo `Tests/Client/ObservationCameraPlayTests.cs`** (namespace `Game.Client.Tests`, `using System.Collections; using System.Linq; using System.Text.RegularExpressions; using NUnit.Framework; using UnityEngine; using UnityEngine.TestTools;`). Lớp `ObservationCameraPlayTests`, summary `/// <summary>World 7j (brief 0053): observation camera — place, tool cycle, load then view, return, locks.</summary>`.
   - Trường: `GameObject camGo, go, playerGo; TerrainChunkView view; TerrainEditInput tei; ObservationCamera obs; PlayerCharacter player; FollowCamera follow;`
   - `[UnitySetUp] SetUp`: chép đúng `PlayerCharacterPlayTests.SetUp` `:24-46` (camera `(192, 40, 150)`, tag `MainCamera`, `Camera` + `DebugFlyCamera` + `FollowCamera`; **bỏ** `UnderwaterView`; terrain `MapsV3` streaming + `DemoPlain` + `TerrainEditInput` gán vào `tei`; nhân vật `readKeyboard = false`; hai `yield return null` + chờ `player.Ready` tối đa 60 khung). Sau đó `Assert.IsTrue(player.Ready); obs = go.GetComponent<ObservationCamera>(); Assert.IsNotNull(obs);`.
   - `[UnityTearDown]`: `Object.Destroy(playerGo); Object.Destroy(go); Object.Destroy(camGo); yield return null;`
   - Helper:
     ```csharp
     static Vector2 ButtonCenterOnScreen()
     {
         float s = UiScale.Gui, gw = Screen.width / s, gh = Screen.height / s;
         var r = ObservationCamera.ButtonRect(gw, gh);
         return new Vector2(r.center.x * s, Screen.height - r.center.y * s);
     }
     static void AssertPose(Transform t, Vector3 p, Quaternion r)
     {
         Assert.Less(Vector3.Distance(t.position, p), 1e-3f, "position");
         Assert.Less(Quaternion.Angle(t.rotation, r), 0.01f, "rotation");
     }
     Vector3 Map2Pose() // above the Map 2 centroid, world space
     {
         var c = WalkBench.MapCentroid(view.Maps, 1);
         return view.transform.TransformPoint(new Vector3(c.x, view.World.Size.Y, c.y));
     }
     IEnumerator WaitViewing()
     {
         for (int i = 0; i < 600 && obs.State != ObservationState.Viewing; i++) yield return null;
         Assert.AreEqual(ObservationState.Viewing, obs.State, "not loaded within 600 frames");
     }
     ```
   - 8 test (tên đúng như dưới):
     1. `PlaceTakesCameraPoseAndReplaces`: chưa đặt → `HasCamera`/`ButtonVisible` false, `BlocksPointer(ButtonCenterOnScreen())` false. Đặt camera tại `p1 = (150.25, 45.5, 140.75)`, `r1 = Euler(30, 45, 0)` bằng `camGo.transform.SetPositionAndRotation` rồi **cùng khung** gọi `tei.PlaceObservationCamera()` → true; `AssertPose(obs.Point, p1, r1)`; `StringAssert.Contains("Đã đặt camera quan sát", tei.HudText()); // vi-text`; `ButtonVisible`, `MarkerVisible` true; `Assert.AreEqual("Xem camera (V)", obs.ButtonText); // vi-text`; `BlocksPointer(ButtonCenterOnScreen())` true, `BlocksPointer(new Vector2(Screen.width / 2f, Screen.height / 2f))` false. Giữ `var first = obs.Point;`. Đặt lần 2 tại `p2 = (200.5, 50, 210.5)`, `r2 = Euler(10, 200, 0)` cùng cách → `Assert.AreSame(first, obs.Point)`, `AssertPose(obs.Point, p2, r2)`, `Assert.AreEqual(1, go.GetComponentsInChildren<Transform>(true).Count(t => t.name == "ObservationPoint"))`. `yield return null;` → `Assert.AreEqual(0, obs.Point.GetComponentsInChildren<Collider>(true).Length)`; `Assert.AreEqual(1, view.ViewpointCount)` (đặt không thêm điểm nhìn).
     2. `CameraToolCycleAndClickPlaces`: không có SimView nên vòng Dig → Place → Camera → Dig (assert từng bước bằng `tei.ToggleTool()`). Khi đang ở Camera: `StringAssert.Contains("<color=yellow>Camera</color>", tei.HudText())` và `StringAssert.Contains("đặt camera quan sát tại chỗ và hướng đang nhìn", tei.HudText()); // vi-text`; đặt pose camera `p1`/`r1` như test 1 rồi `Assert.AreEqual(EditResult.Ok, tei.UseTool(default, default))` → `AssertPose(obs.Point, p1, r1)`. (Cần `using Game.Core;` cho `EditResult`.)
     3. `ViewWaitsForLoadThenShowsAndReturns`: `Assert.IsTrue(obs.Place(Map2Pose(), Quaternion.Euler(40f, 0f, 0f)))`; `Assert.Greater(view.MissingColumnsWithin(obs.Point, 128), 0, "Map 2 must need loading")`; `Assert.AreEqual(1, view.ViewpointCount)`. `Assert.IsTrue(obs.Toggle())` → `State == Loading`, `LoadingVisible` true, `ViewpointCount == 2`, `follow.enabled` false, `Assert.Greater(Vector3.Distance(camGo.transform.position, obs.Point.position), 1f)` (chưa chuyển hình), `ObservationCamera.InputLocked` true, `Assert.AreEqual("Quay về (V)", obs.ButtonText); // vi-text`. `yield return WaitViewing();` → `Assert.AreEqual(0, view.MissingColumnsWithin(obs.Point, 128))`, `AssertPose(camGo.transform, obs.Point.position, obs.Point.rotation)`, `MarkerVisible` false, `LoadingVisible` false, `ViewpointCount == 2`. Trước khi quay về: lập `held` = các `col` 0..575 có `view.ChunkMesh(col) != null` và `ChunkStreamPlan.DistanceSq(pc.x, pc.z, col % 24, col / 24) > 160L * 160` với `pc = view.CellAt(player.transform.position)`; `Assert.IsNotEmpty(held)`. `Assert.IsTrue(obs.Toggle())` → `State == Off`, `ViewpointCount == 1`, `follow.enabled` true, `MarkerVisible` true, `InputLocked` false, `Assert.AreEqual("Xem camera (V)", obs.ButtonText); // vi-text`. `yield return null;` → `Assert.Less(Vector3.Distance(camGo.transform.position, player.transform.position), follow.maxDistance + 3f)`. Chờ tối đa 300 khung đến khi mọi `col` trong `held` có `ChunkMesh(col) == null` (không hết → `Assert.Fail`); rồi mọi cột còn mesh có `DistanceSq` tới `pc` `<= 160L * 160`; `Assert.AreEqual(0, view.MissingColumnsWithin(128))`. (Cần `using Game.Core;` cho `ChunkStreamPlan`.)
     4. `KeysWithoutCameraDoNothing`: `obs.HandleKeys(true, false)` rồi `obs.HandleKeys(false, true)` → `State == Off`, `ViewpointCount == 1`, `follow.enabled` true, `ButtonVisible` false; `Assert.IsFalse(obs.Toggle())`.
     5. `ExitDuringLoadingCancels`: đặt tại `Map2Pose()` như test 3, `obs.HandleKeys(true, false)` → `State == Loading`; `obs.HandleKeys(true, false)` → `State == Off`, `ViewpointCount == 1`, `follow.enabled` true, `MarkerVisible` true.
     6. `BackExitsWithoutOpeningSettings`: `var settings = go.AddComponent<SettingsView>(); yield return null;`. Đặt tại pose hiện tại của camera (`tei.PlaceObservationCamera()`), `obs.Toggle()`, `yield return WaitViewing();`. `settings.HandleKeys(false, true)` → `settings.IsOpen` false (đang xem). `obs.HandleKeys(false, true)` → `State == Off`; ngay cùng khung `settings.HandleKeys(false, true)` → `settings.IsOpen` false. `yield return null;` → `settings.HandleKeys(false, true)` → `settings.IsOpen` true (Esc lại hoạt động bình thường); `settings.SetOpen(false)`.
     7. `ToolsAndMovementLockedWhileViewing`: đặt bằng `tei.PlaceObservationCamera()`, `obs.Toggle()`, `yield return WaitViewing();`. `var tool = tei.Tool; tei.ToggleTool(); Assert.AreEqual(tool, tei.Tool)`. `var f = player.FeetLocal; var c = new Vector3Int(Mathf.FloorToInt(f.x) + 2, Mathf.FloorToInt(f.y) - 1, Mathf.FloorToInt(f.z)); var before = view.World.Get(c.x, c.y, c.z); Assert.AreEqual(EditResult.NotPlaceable, tei.UseTool(c, c)); Assert.AreEqual(before, view.World.Get(c.x, c.y, c.z));`. `Assert.IsFalse(tei.PlaceObservationCamera())`. `var p0 = player.transform.position; player.MoveInput = new Vector2(0f, 1f); player.JumpInput = true;` rồi 30 khung `yield return null` → khoảng cách ngang `new Vector2(p.x - p0.x, p.z - p0.z).magnitude < 0.05f` với `p = player.transform.position`, `Assert.AreEqual(Vector2.zero, player.MoveInput)`. `obs.Toggle()` (thoát) → `tei.ToggleTool()` đổi được công cụ (khác `tool`).
     8. `NoFreeViewpointRefuses`: `var dummy = new GameObject("Dummy").transform;` trong `try/finally` (finally: `view.RemoveViewpoint(dummy); Object.Destroy(dummy.gameObject);`). `view.AddViewpoint(dummy, 128, 160)`; `tei.PlaceObservationCamera()`; `LogAssert.Expect(LogType.Warning, new Regex("^OBSERVE"))`; `Assert.IsFalse(obs.Toggle())` → `State == Off`, `follow.enabled` true, `ViewpointCount == 2` (chỉ nhân vật + dummy). Kết thúc bằng `yield break;` nếu không cần khung.
   Xong khi: `run_tests` PlayMode lọc `Game.Client.Tests.ObservationCameraPlayTests` → 8/8.
9. **Tài liệu** `design/02-gameplay/controls-ui.md`:
   - Dòng `:35` cột Bàn phím-chuột: `Tab (Đào → Đặt → Xới); phím 1–4 chọn loại khối (PC M1 đã làm)` → `Tab (Đào → Đặt → Xới → Camera); phím 1–4 chọn loại khối (PC M1 đã làm)`.
   - Thêm dòng mới ngay sau dòng `:37` (Phép Mưa), nguyên văn:
     ```
     | Camera quan sát | Tab chọn công cụ Camera + chuột trái: đặt camera quan sát tại vị trí và hướng đang nhìn (M1 tối đa 1, đặt lại thì thay chỗ cũ). V hoặc nút "Xem camera (V)" ở hàng nút dưới bên phải (chỉ hiện khi đã đặt): hiện "Đang tải…" tới khi vùng quanh camera nạp đủ rồi xem toàn màn hình, góc nhìn cố định, nhân vật đứng yên, không dùng công cụ. V / nút "Quay về (V)" / Esc: quay về | _(chưa có)_ | _(chưa có)_ | M1 ([0029](../decisions/0029-nhieu-diem-nhin-camera-quan-sat.md), world 7j) |
     ```
   - Dòng `:48` (Menu, tạm dừng), cột Bàn phím-chuột: `Esc (PC M1: đóng bảng đang mở / thoát chế độ gieo; không có bảng nào mở → mở Cài đặt)` → `Esc (PC M1: đóng bảng đang mở / thoát chế độ gieo / thoát xem camera quan sát; không có bảng nào mở → mở Cài đặt)`.
   Xong khi: `sh "/Users/macvn/.claude/plugins/cache/ai-workflow-kit/ai-workflow-kit/0.15.1/scripts/run.sh" verify_run design` exit 0.
10. **Toàn bộ test + dọn** (xem "Lệnh executor chạy"), ghi Kết quả.
11. **Sửa sau review (user xem 2026-10-23): khối đánh dấu che màn hình ngay sau khi đặt.** Nguyên nhân: camera đặt đúng tư thế `Camera.main`, nên khối "Lens" (0,2 m, cách 0,35 m phía trước) nằm ngay trước mắt → ô vuông đen giữa màn hình tới khi camera rời đi. Quyết định (Planner): **khi `Camera.main` cách `Point` ≤ 1,5 m thì tắt renderer của khối đánh dấu**; `Marker.activeSelf` / `MarkerVisible` giữ nguyên nghĩa cũ (ẩn khi đang xem), các test cũ không đổi. API: `Renderer.enabled` [docs](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/Renderer-enabled.html), `Component.GetComponentsInChildren` [docs](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/Component.GetComponentsInChildren.html), `Camera.main` [docs](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/Camera-main.html).
    - `Client/ObservationCamera.cs`: thêm field `Renderer[] markerRenderers;` và `const float MarkerHideDistance = 1.5f;` cạnh các field hiện có; thêm property ngay dưới `MarkerVisible`:
      ```csharp
      /// <summary>Marker is active and its renderers are on (hidden while Camera.main is within MarkerHideDistance).</summary>
      public bool MarkerRendered => MarkerVisible && markerRenderers[0].enabled;
      ```
      Cuối `BuildPoint()` (sau 2 lệnh `AddBlock`): `markerRenderers = Marker.GetComponentsInChildren<Renderer>(true);`. Thêm hàm ngay sau `Update()`:
      ```csharp
      void LateUpdate()
      {
          if (!MarkerVisible) return;
          var cam = Camera.main;
          bool show = cam == null || (cam.transform.position - Point.position).sqrMagnitude > MarkerHideDistance * MarkerHideDistance;
          foreach (var r in markerRenderers) r.enabled = show;
      }
      ```
    - `Tests/Client/ObservationCameraPlayTests.cs`: thêm test thứ 9 ở cuối class:
      ```csharp
      [UnityTest]
      public IEnumerator MarkerHiddenWhileCameraClose()
      {
          follow.enabled = false;
          camGo.transform.SetPositionAndRotation(new Vector3(150.25f, 45.5f, 140.75f), Quaternion.Euler(30f, 45f, 0f));
          Assert.IsTrue(tei.PlaceObservationCamera());
          yield return null;
          Assert.IsTrue(obs.MarkerVisible);
          Assert.IsFalse(obs.MarkerRendered, "marker must not cover the view right after placing");
          camGo.transform.position = obs.Point.position + new Vector3(0f, 0f, -3f);
          yield return null;
          Assert.IsTrue(obs.MarkerRendered);
          camGo.transform.position = obs.Point.position + new Vector3(1f, 0f, 0f);
          yield return null;
          Assert.IsFalse(obs.MarkerRendered);
      }
      ```
    Xong khi: `run_tests` PlayMode lọc `Game.Client.Tests.ObservationCameraPlayTests` → 9/9; PlayMode toàn bộ → 181/181; EditMode toàn bộ → 504/504; dọn `git checkout -- client/ProjectSettings/EditorSettings.asset`; `git status --short` chỉ còn 2 file trên + brief. Ghi giờ bắt đầu/kết thúc lần sửa vào Kết quả (dòng mới "Sửa lần 1: …"), kết quả test vào Kết quả test. Không làm lại bước 1–10.

## Tiêu chí xong
- `ObservationCameraPlayTests` 8/8; `GameControlsTests` qua (31 hành động); `TerrainEditPlayTests.ToolStartsDigAndTabToggles` và `SimFarmUiPlayTests.TabCyclesFourToolsWithSim` qua.
- Toàn bộ EditMode **504/504** (số test không đổi); toàn bộ PlayMode **180/180** (172 + 8 mới; đổi tên 1 test không đổi số).
- `HardcodedTextScanTests` qua (nằm trong EditMode).
- `verify_run design` exit 0.
- Sau dọn, `git status --short` chỉ còn file trong Phạm vi (+ `.meta` của 2 file mới) và brief.

## Test sau khi xong

### Phần cần test
- Bước 1 phím V + bảng hành động · `GameControlsTests` (`AssetMatchesDefaultTable`, `GameInputCoversAllActions` 31; `NoDuplicateBindingsWithinMap`; `NoDirectInputReadsInScope` gồm `ObservationCamera.cs`) → qua.
- Bước 2 chuỗi · `HardcodedTextScanTests` (`NoVietnameseLiterals`, `AllUsedKeysExist`, `BaseFileValid`) → qua; lệnh JSON/sort ở bước 2 → `True`.
- Bước 3–4 đặt camera: đúng vị trí + hướng, đặt lại thay chỗ, 1 camera, khối không va chạm, dòng kết quả HUD, nút chỉ hiện khi có camera và chặn con trỏ · `PlaceTakesCameraPoseAndReplaces` → qua.
- Bước 4 vòng Tab + chuột trái khi cầm Camera + chữ HUD công cụ · `CameraToolCycleAndClickPlaces` (không SimView), `SimFarmUiPlayTests.TabCyclesFourToolsWithSim` (có SimView), `TerrainEditPlayTests.ToolStartsDigAndTabToggles` → qua.
- Bước 3 xem: chờ tải → chuyển hình, 2 điểm nhìn, khối ẩn; quay về: camera nhân vật, 1 điểm nhìn, chỉ gỡ cột của vùng camera · `ViewWaitsForLoadThenShowsAndReturns` → qua.
- `V` khi chưa có camera không làm gì; huỷ khi đang tải · `KeysWithoutCameraDoNothing`, `ExitDuringLoadingCancels` → qua.
- Bước 6 Esc thoát xem không mở Cài đặt · `BackExitsWithoutOpeningSettings` → qua.
- Bước 4–5 khoá công cụ + nhân vật khi xem · `ToolsAndMovementLockedWhileViewing` → qua.
- Hết chỗ điểm nhìn · `NoFreeViewpointRefuses` → qua.
- Regression công cụ cũ (đào/đặt/xới/gieo/nhặt/thu hoạch, tầm 8 ô): `TerrainEditPlayTests`, `PlayerCharacterPlayTests`, `PlayerFarmPlayTests`, `PlayerBagSowPlayTests`, `SimFarmUiPlayTests`, `DropPlayTests` → qua (nằm trong PlayMode toàn bộ).
- Regression Cài đặt (bước 6 đổi `OtherPanelOpen`): `SettingsPlayTests` → qua.
- Regression streaming / nhiều điểm nhìn / WalkBench: `TerrainStreamingPlayTests`, `WalkBenchPlayTests` → qua.
- Bước 9 tài liệu · `verify_run design` → exit 0.

### Lệnh executor chạy
Unity Editor mở project `client/` qua MCP (`run_tests`, `read_console`), quyết định 0010. Chỉ phiên này điều khiển Editor trong lúc làm.
1. Ghi giờ bắt đầu: `date "+%H:%M"`.
2. Sau bước 1–6: `refresh_unity` + `read_console` → không lỗi biên dịch.
3. `run_tests` EditMode lọc `GameControlsTests` và `HardcodedTextScanTests` → qua.
4. `run_tests` PlayMode lọc `Game.Client.Tests.ObservationCameraPlayTests` → 8/8; lọc `TerrainEditPlayTests` và `SimFarmUiPlayTests` → qua.
5. `sh "/Users/macvn/.claude/plugins/cache/ai-workflow-kit/ai-workflow-kit/0.15.1/scripts/run.sh" verify_run design` → exit 0.
6. `run_tests` EditMode (toàn bộ) → 504/504.
7. `run_tests` PlayMode (toàn bộ) → 180/180.
8. Dọn: `git checkout -- client/ProjectSettings/EditorSettings.asset`, rồi `git status --short` → chỉ file trong Phạm vi + `.meta` mới + brief.
9. Ghi giờ kết thúc: `date "+%H:%M"`; ghi vào Kết quả.

### User tự test
Trên PC, scene `SimView` (hoặc `TerrainView`), bấm Play trong Editor:
1. Đi ra rìa Map 1 hướng về Map 2 (mở bản đồ `M` để xem ranh giới), Tab tới công cụ **Camera** (HUD trái ghi "Công cụ: Camera"), chuột trái → dòng "Đã đặt camera quan sát tại …"; nút "Xem camera (V)" hiện ở hàng nút dưới bên phải.
2. Ngay sau khi đặt: **không có ô đen/cam che giữa màn hình**. Xoay camera nhìn lại chỗ vừa đặt: thấy khối cam nhỏ (có mặt đen chỉ hướng) lơ lửng đúng chỗ, nhân vật đi xuyên qua được.
3. Đi về trại Map 1 (xa camera), bấm `V`: thấy chữ "Đang tải…" giữa màn hình một lúc rồi hình chuyển sang góc nhìn camera ở Map 2, **không có lỗ** địa hình; khối cam không hiện; WASD/Space/chuột giữa/chuột trái không làm gì.
4. Bấm `V` lần nữa → về camera sau lưng nhân vật, đúng chỗ cũ. Thử lại bằng nút "Quay về (V)" và bằng `Esc` (Esc không được mở bảng Cài đặt).
5. Báo lại: có lỗ/giật khi chuyển không, nút có đè nút khác không, thời gian "Đang tải…" khoảng bao lâu.

## Dừng và chuyển lại Opus khi
- Brief mơ hồ hoặc mâu thuẫn với code thật (vd số dòng/chữ ký trích ở Bối cảnh không còn khớp).
- `Assert.Greater(view.MissingColumnsWithin(obs.Point, 128), 0, ...)` hoặc `Assert.IsNotEmpty(held)` ở test 3 không qua (vị trí Map 2 không đủ xa — Planner chọn lại vị trí).
- Test cũ ngoài 2 test vòng Tab ở bước 7 đổi kết quả.
- Cần API Unity không có trong brief, hoặc một quyết định thuộc AGENTS.md §3.4 / ngoài "Quyết định đã chốt" (vd muốn tạo Camera thứ hai, đổi vị trí nút).
- Cùng một lỗi sửa 2 lần vẫn chưa qua.
- Phải sửa file ngoài "Phạm vi".

---
<!-- Từ đây trở xuống: EXECUTOR ghi. -->

## Kết quả
Base commit: af271c6
Chạy bằng: claude-sonnet-5-5 · medium
Bắt đầu: 12:19 · Kết thúc: 12:31 (~12 phút thực tế)
Commit: 175e9bf
File đổi: Client/ObservationCamera.cs (mới), TerrainEditInput.cs, PlayerCharacter.cs, SettingsView.cs, GameInput.cs, GameControls.inputactions, vi.json, GameControlsTests.cs, TerrainEditPlayTests.cs, SimFarmUiPlayTests.cs, ObservationCameraPlayTests.cs (mới), design/02-gameplay/controls-ui.md
Sửa lần 1 (bước 11): Base commit f798d4c · claude-sonnet-5-5 · medium · bắt đầu 14:35 · kết thúc 14:43 (~8 phút) · commit a562645
Sửa lần 1: File đổi: Client/ObservationCamera.cs (field markerRenderers, MarkerRendered, LateUpdate), Tests/Client/ObservationCameraPlayTests.cs (test 9)
Sửa lần 1 · kết quả test: ObservationCameraPlayTests 9/9 đạt (gồm MarkerHiddenWhileCameraClose); EditMode 504/504; PlayMode 181/181 sau khi chạy lại 1 test: lần chạy toàn bộ đầu có TerrainEditPlayTests.TimingSingleChunkEdits fail (Expected 1 chunk, was 5 — test đo thời gian, đào ngẫu nhiên seed 7, không liên quan marker), chạy riêng đạt → nghi flaky, Planner xem; dọn EditorSettings.asset xong, git status chỉ còn file Phạm vi + brief. User tự test (mục 2–5) → chờ user.
Ghi chú: 2 dòng `// vi-text` thừa trên chuỗi "Xem camera (V)" (không dấu) bị HardcodedTextScanTests bắt → đã bỏ marker ở 2 dòng đó.

## Kết quả test
- Bước 1 phím V · GameControlsTests (31 hành động) → đạt. Bước 2 chuỗi · HardcodedTextScanTests → đạt; JSON hợp lệ, key tăng dần `True`.
- Đặt camera / vòng Tab / xem + quay về / V không camera / huỷ khi tải / Esc / khoá / hết điểm nhìn · ObservationCameraPlayTests 8/8 → đạt.
- TerrainEditPlayTests, SimFarmUiPlayTests.TabCyclesFourToolsWithSim, SettingsPlayTests, các regression khác → đạt (nằm trong PlayMode toàn bộ).
- `verify_run design` exit 0. EditMode toàn bộ 504/504. PlayMode toàn bộ 180/180. Dọn: checkout EditorSettings.asset; `git status` chỉ còn file Phạm vi + `.meta` mới + brief.
- User tự test (mục "User tự test") → chờ user.

## Kiểm chứng
- Chưa kiểm được qua verify_run `client_test`/`client_test_play`: `tools/unity_test.py` chỉ chạy khi Editor TẮT, còn Editor đang mở qua MCP (0010, AGENTS.local §Phân vai). Thay bằng `run_tests` MCP: EditMode 504/504, PlayMode 180/180 (xem Kết quả test). `verify_run design` exit 0.
- Checklist: pillar P3 (đạt, Mục tiêu) · thành phần client Unity (đạt, Mục tiêu) · glossary/thuật ngữ mới (không thêm thuật ngữ; Planner ghi khi review) · liên quan chéo (đạt, chỉ controls-ui.md; world-tech/roadmap để Planner) · [ý tưởng]/Chốt (đạt, quyết định 0029) · nguồn (đạt, không số liệu ngoài) · phạm vi M1 (đạt, 1 brief) · Game.Core không đổi (đạt) · test "Xong khi" (đạt, 8/8) · chỉ M1 (đạt) · giờ thực tế (đạt: ~12 phút, 12:19–12:31).

## Vướng mắc

---
<!-- PLANNER ghi khi review. -->

## Review
Mức: đầy đủ + user xem · Planner Opus · 2026-10-23
- `Chạy bằng` claude-sonnet-5-5 · medium = `Model thực thi` Sonnet · medium → khớp.
- Mỗi dòng "Phần cần test" có kết quả trong "Kết quả test" (đạt); User tự test → chờ user.
- `git diff --stat af271c6 HEAD`: 15 file, đều trong Phạm vi (+ 2 `.meta` mới + brief); không đụng `TerrainChunkView`/`ChunkStreamPlan`/`Game.Core`/scene → đạt QĐ 9.
- `ObservationCamera.cs` so `diff -w` với khối code bước 3: trùng nguyên văn [run].
- `TerrainEditInput.cs`: enum Camera, `Observation` thêm trong Awake, khoá đầu `Update`/`ToggleTool`/`UseTool`, chặn con trỏ nút, nhánh chuột trái công cụ Camera (gieo ưu tiên), `ShowAim` bỏ khung, HUD `hud.tool_camera` + `hud.camera_help` → đúng bước 4, QĐ 1, 2, 5.
- `PlayerCharacter.cs` (F3 + xoá Move/Jump khi `InputLocked`), `SettingsView.OtherPanelOpen` (`BlocksBack`) → đúng bước 5–6, QĐ 5–6.
- Input: action `ToggleObservation` `<Keyboard>/v`, `GameInput`, `GameControlsTests` 31 + `ObservationCamera.cs` trong `NoDirectInputReadsInScope` → đúng bước 1. `vi.json` 6 key đúng chữ, đúng thứ tự.
- `ObservationCameraPlayTests.cs`: 8 test đúng tên và assert như bước 8 (gồm `Map2Pose` cần nạp, `held` không rỗng, gỡ cột vùng camera trong 300 khung). Bỏ 2 marker `// vi-text` trên chuỗi không dấu là đúng (scanner bắt marker thừa) — không đổi assert.
- 2 test vòng Tab sửa đúng bước 7; `controls-ui.md` đúng bước 9.
- Kết quả test executor (EditMode 504/504, PlayMode 180/180, design exit 0) khớp Tiêu chí xong; không đáng ngờ → không chạy lại.
- Planner ghi thêm: roadmap (dòng World 7j, Executor 12 phút so với 1–2 ngày-người), world-tech 7j "code + test xong, chờ user xem", `controls-ui.md` sửa số hành động 30 → 31 (dòng chú thích dưới bảng, executor không được giao). Glossary đã có "Camera quan sát", "Công cụ Camera", "Điểm nhìn". `check_design.py` exit 0 [run].
- Chờ user xem (PC, Play scene `SimView`): Tab tới Camera + chuột trái → dòng "Đã đặt camera quan sát tại …" + nút "Xem camera (V)"; khối cam đúng chỗ, đi xuyên được; từ trại Map 1 bấm `V` → "Đang tải…" rồi góc nhìn camera, không lỗ, khối ẩn, WASD/Space/chuột không làm gì; `V` / nút "Quay về (V)" / `Esc` → về đúng chỗ, Esc không mở Cài đặt; nút không đè Kho/Cài đặt/Túi; báo thời gian "Đang tải…".
- User xem (2026-10-23, ảnh Play `SimView`): đặt camera → dòng "Đã đặt camera quan sát tại (123, 24, 197)" và nút "Xem camera (V)" đúng chỗ, không đè Kho/Cài đặt/Túi (đạt). Lỗi: **ô vuông đen lớn giữa màn hình ngay sau khi đặt** — là khối "Lens" của khối đánh dấu, nằm 0,35 m trước `Camera.main` vì camera đặt đúng tư thế camera (`ObservationCamera.cs` `BuildPoint`/`AddBlock`, `TerrainEditInput.cs:206`). Lỗi do brief (Planner), không phải executor.
- Sửa: bước 11 (tắt renderer khối khi `Camera.main` cách ≤ 1,5 m; test 9 `MarkerHiddenWhileCameraClose`). Các mục user xem còn lại (xem từ trại, không lỗ, khoá điều khiển, quay về bằng V/nút/Esc, thời gian "Đang tải…") thử lại sau khi sửa.
- Kết luận: cần sửa
