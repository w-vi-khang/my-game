# Brief 0035 — World bước 7h (a): nhân vật người chơi trên PC, camera theo sau lưng, tầm với 4 ô, camera bay thành chế độ debug

> Trạng thái: chờ làm
> Review: đầy đủ + user xem
> Model thực thi: Sonnet · medium
> Cập nhật: 2026-10-22

<!-- Planner (Opus) viết, Executor (Sonnet) làm theo ở PHIÊN KHÁC — executor không có lịch sử chat, không được tự quyết.
Trạng thái: chờ làm → đang làm → chờ review → đã duyệt; hoặc → cần Opus (executor dừng, hỏi ở mục Vướng mắc).
Review: nhẹ (đọc mục Kết quả) | đầy đủ (đọc cả diff) | user xem (có phần user tự kiểm bằng mắt/chơi thử) — có thể ghép "đầy đủ + user xem".
Model thực thi: BẮT BUỘC, đúng dạng `<Model> · <effort>` — Model: Opus | Sonnet | Haiku | Fable; effort: low | medium | high | xhigh | max (chọn theo AGENTS.md §10.2).
Executor kiểm tra model + effort của phiên TRƯỚC khi làm (scripts/brief_check.py); lệch hoặc không xác định được → cảnh báo user và DỪNG.
File được commit. Tự kiểm trước khi giao (AGENTS.md §10.3): executor có phải hỏi gì không? còn chỗ nào bỏ ngỏ 2 phương án? -->

## Mục tiêu
Làm phần (a) của [task contract world 7h](../tasks/m1-world-7h-nhan-vat.md) ([0028](../../design/decisions/0028-nhan-vat-tui-vat-roi-tu-trong-trot.md), Chốt):
- Lõi `Game.Core`:
  - luật tầm với 4 ô;
  - ô bị nhân vật chiếm;
  - giữ nhân vật trong biên world;
  - nhận biết nhân vật đang trong nước.
- Client:
  - nhân vật khối ghép (`CharacterController`), điều khiển PC;
  - camera theo sau lưng, không xuyên khối;
  - F3 bật/tắt camera bay (chế độ debug);
  - đào/đặt/xới chỉ trong tầm;
  - nạp chunk và minimap lấy vị trí nhân vật;
  - nhân vật có trong scene SimView và TerrainView.

**Không có trong brief này** (brief 0036 sau): cảm ứng (joystick ảo, kéo xoay, nút nhảy), đo FPS trên điện thoại.

Ước lượng 2–3 ngày-người (phần a của 3–5). Ghi giờ bắt đầu/kết thúc (theo `date`) vào mục Kết quả.

## Bối cảnh cần đọc
Đường dẫn tính từ `client/Assets/Game/`. Số dòng theo commit `d7d2e3d`.

**Task contract:** `.ai/tasks/m1-world-7h-nhan-vat.md` — đọc mục "Tiêu chí chấp nhận" và "Quyết định đã chốt".

**Camera bay — giữ nguyên, chỉ bật/tắt từ ngoài:** `Client/DebugFlyCamera.cs`
- `public void SyncAngles()` (`:28-33`).
- `public static bool IsMoveKeyHeld(Keyboard kb)` (`:36`).
- Xoay bằng giữ chuột giữa, `lookSensitivity = 0.15f`, bỏ qua khi `MinimapView.IsAnyOpen` (`:60-66`).

**Nhắm ô, đào/đặt/xới:** `Client/TerrainEditInput.cs`
- `Update` (`:58-108`):
  - lăn chuột chọn loại khối ở `:77-83`;
  - nhắm theo `Camera.main.ScreenPointToRay(mouse.position)` ở `:85-90`;
  - đang giữ phím bay thì không dùng công cụ (`:105`);
  - click chuột trái gọi `UseTool` (`:107`).
- `public void ShowAim(bool picked, Vector3Int digCell, Vector3Int placeCell)` (`:124-148`).
- `public EditResult UseTool(Vector3Int digCell, Vector3Int placeCell)` (`:151-177`).
- `public bool Pick(Ray ray, out Vector3Int digCell, out Vector3Int placeCell)` (`:182-190`) — `Physics.Raycast` với layerMask mặc định.
- `OnGUI` (`:210-232`): dòng hướng dẫn ở `:214`.

**Chunk:** `Client/TerrainChunkView.cs`
- `public Transform streamTarget;` (`:33`).
- `Transform StreamCenter()` (`:106`) — tâm nạp; rỗng thì lấy `Camera.main`.
- `public bool IsTerrainCollider(Collider c)` (`:226`).
- `public Vector3Int CellAt(Vector3 worldPoint)` (`:229-233`).
- `Build()` (`:120-223`):
  - gọi `afterGenerate` ở `:159-160`, tức là trước khi nạp cột chunk đầu (`:178-199`);
  - collider được bake xong ngay trong `Build`.
- Collider chunk là mặt khối vuông chưa bo (`:41-42`).
- `public WorldEditor Editor`, `World`, `Inventory`, `Landmarks` (`:62-68`).

**Luật đặt khối:** `Core/WorldEditor.cs`
- `enum EditResult` (`:6-22`) — đã có `Occupied`.
- `public Func<int, int, int, bool> IsOccupied;` (`:50`). `Simulation.AttachEditor` **ghi đè** móc này (`Core/Sim/Simulation.cs:353`), nên nhân vật **không** được dùng chung móc này.
- `public EditResult CheckPlace(int x, int y, int z, BlockMaterial material)` (`:87-104`) — kiểm `IsOccupied` ở `:98-99`.

**Đồng bằng demo:** `Client/DemoPlain.cs`
- `OnAfterGenerate` (`:22-30`, scene TerrainView): san phẳng rồi **bỏ các dấu mốc nằm trong đồng bằng**. Vì vậy checkpoint Map 1 có thể đã bị bỏ, xem QĐ 14.
- `public static void PlaceCamera(Vector3 pos, float pitch)` (`:48-55`).
- `StressPlain` (`Core/Sim/SimStress.cs:7-14`): `X0, Z0, Size, Ground`; `Ground` = y của ô mặt (ô đặc cao nhất).
- Bố trí trại (`Core/Sim/SimDemo.cs:24-31`): dz từ −2 tới 5 quanh tâm đồng bằng.

**Scene SimView:** `Client/SimView.cs`
- `OnAfterGenerate` (`:82-90`) đặt camera.
- Bảng phím `:108-115`: F1, F2, K, Esc.
- `public static bool BlocksPointer(Vector2)` dùng ở `TerrainEditInput.cs:72`.
- Material khối: `Resources.Load<UnityEngine.Material>("Materials/Landmark")` (`:178`), đổi màu bằng `mat.SetColor("_BaseColor", color)` (`:463-464`).

**Minimap:** `Client/MinimapView.cs`
- Dấu người chơi trên bản đồ lớn: `:340-349`.
- Ô góc: `RefreshCorner` `:353-360+`.
- Cả hai lấy vị trí **và** hướng từ `Camera.main`.

**Dấu mốc:** `Core/LandmarkPlacer.cs`
- `readonly struct Landmark` (`:10-25`): `Kind`, `Map`, `X`, `Z`, `Surface`, `Name`.
- `LandmarkKind.Checkpoint` (`:7`); checkpoint Map 1 có `Map == 0`.

**Cột world:** `MinimapImage.ColumnInfo(WorldData world, int x, int z, out int surface, out int waterDepth)` (`Core/MinimapImage.cs:44`).

**Kho:** `MaterialInventory`
- `public void Add(BlockMaterial material, int amount = 1)` (`Core/MaterialInventory.cs:17`).
- `public int Get(BlockMaterial material)` (`:15`).

**Dưới nước:** `Client/UnderwaterView.cs`
- `public TerrainChunkView terrain;`, `public bool IsUnderwater { get; private set; }`.
- Tính theo vị trí camera; giữ nguyên.

**Mẫu test:**
- `Tests/Client/DemoPlainPlayTests.cs:17-41`: dựng camera + `TerrainChunkView` (MapsV3, streaming) + `DemoPlain`.
- `Tests/Core/WorldEditorTests.cs:14-30`: world 32×16×32 dựng tay.

**Scene:** `Client/Scenes/SimView.unity`, `Client/Scenes/TerrainView.unity`
- GameObject gốc: `Main Camera`, `Terrain`, `Directional Light`, `Ocean`.
- `Main Camera` có `DebugFlyCamera`, `UnderwaterView`, `WorldEdgeFog`.
- `Terrain` có `TerrainChunkView` (streaming 1), `TerrainEditInput`, `MinimapView`… TerrainView có thêm `DemoPlain`; SimView có `SimView`.

**API Unity mới dùng (6000.4, Input System 1.19) — chỉ dùng những API này:**
- `CharacterController`: `Move(Vector3 motion)` trả `CollisionFlags`, không tự áp trọng lực; `isGrounded`, `height`, `radius`, `center`, `stepOffset`, `slopeLimit`, `skinWidth`, `enableOverlapRecovery`. Docs: [CharacterController](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/CharacterController.html), [Move](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/CharacterController.Move.html).
- `Physics.SphereCast(Vector3 origin, float radius, Vector3 direction, out RaycastHit hitInfo, float maxDistance, int layerMask, QueryTriggerInteraction q)`. Collider đã chồng lên hình cầu ngay điểm đầu thì không bắt được. Docs: [SphereCast](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/Physics.SphereCast.html).
- `Physics.DefaultRaycastLayers` = mọi layer trừ "Ignore Raycast". Docs: [DefaultRaycastLayers](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/Physics.DefaultRaycastLayers.html).
- `Physics.Raycast(Vector3 origin, Vector3 direction, out RaycastHit hitInfo, float maxDistance)` — đã dùng trong test cũ (`TerrainEditPlayTests.cs:46`).
- `LayerMask.NameToLayer("Ignore Raycast")`. Docs: [NameToLayer](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/LayerMask.NameToLayer.html).
- `Quaternion.Euler`, `Quaternion.LookRotation`, `Quaternion.Slerp`, `Mathf.Clamp`, `Mathf.Sin`.
- `GameObject.CreatePrimitive(PrimitiveType.Cube)` — đã dùng ở SimView.
- `Keyboard.f3Key`, `Keyboard.spaceKey`, `Keyboard.altKey` (`ButtonControl`, gộp Alt trái/phải). Docs: [Keyboard 1.19](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.19/api/UnityEngine.InputSystem.Keyboard.html).
- `Mouse.scroll`, `Mouse.delta`, `Mouse.middleButton` — đã dùng.

## Quyết định đã chốt (không được đổi)
**Task contract 7h (user xác nhận 2026-10-22):**
1. Nhân vật khối ghép, cao 2 ô, rộng 1 ô, góc nhìn thứ ba. Khi đi thì nhún nhẹ, không có animation.
2. Điều khiển PC:
   - WASD đi theo hướng camera;
   - giữ chuột giữa + kéo để xoay camera quanh nhân vật;
   - lăn chuột để thu phóng khoảng cách camera;
   - Space để nhảy: lên được bậc 1 ô, không lên được bậc 2 ô.
3. Tầm với 4 ô, đo Euclid từ **tâm thân** (chân + 1,0 theo trục y) tới tâm ô, `≤ 4`. Ngoài tầm thì không tô viền/bản mờ, click không làm gì. Áp cho cả Đào, Đặt và Xới — kể cả click lên ô ruộng để mở bảng.
4. Nhắm ô vẫn theo con trỏ chuột như hiện nay. Được dùng công cụ khi đang đi.
5. Nước: tốc độ ×0,5, không bơi (vẫn chịu trọng lực, chìm xuống đáy). Hiệu ứng dưới nước vẫn tính theo camera như cũ.
6. Nhân vật và sinh vật đi xuyên nhau: sinh vật không có collider, mô phỏng không biết nhân vật.
7. Bench, SimBench, HashCheck **không** có nhân vật, giữ camera bay.
8. **Lăn chuột = thu phóng camera; Alt (Option) + lăn = chọn loại khối** (user 2026-10-22). Phím 1–4 giữ nguyên.

**Planner chốt khi viết brief:**

9. **Phím camera bay: F3.**
   - Bật: tắt `FollowCamera`, bật `DebugFlyCamera` (gọi `SyncAngles()`). Nhân vật bỏ qua input nhưng vẫn chịu trọng lực.
   - Tầm với vẫn tính theo nhân vật. Chỉ trong chế độ bay mới giữ luật cũ "đang giữ phím bay thì không dùng công cụ".
10. **Thông số `CharacterController`:**

    | Thông số | Giá trị |
    |---|---|
    | `height` | 1,8 |
    | `radius` | 0,4 |
    | `center` | (0; 0,9; 0) — gốc transform = chân |
    | `stepOffset` | 0,3 — đi bộ không tự lên bậc 1 ô |
    | `slopeLimit` | 45 |
    | `skinWidth` | 0,04 |

    Thông số di chuyển:

    | Thông số | Giá trị |
    |---|---|
    | Tốc độ đi | 5 ô/giây |
    | Trọng lực | 25 ô/giây² |
    | Vận tốc nhảy | 8 ô/giây (đỉnh ≈ 1,28 ô) |
    | Vận tốc rơi tối đa | 30 ô/giây |
    | Bám đất khi `isGrounded` | vy = −2 |

    Tất cả là `public` field để chỉnh sau user xem.
11. **Ô bị chiếm** = ô giao với hộp `[x − 0,4; x + 0,4] × [chân; chân + 1,8] × [z − 0,4; z + 0,4]`, hộp co vào 0,001 mỗi phía để chạm mặt không tính.
    - Đứng giữa ô → 2 ô thân.
    - Đứng giữa 2 cột → 4 ô.
    - Nối qua móc **mới** `WorldEditor.IsPlayerAt`, không dùng `IsOccupied` (lý do ở mục Bối cảnh).
12. **Biên world:** chặn cứng `x ∈ [0,4; Size.X − 0,4]`, `z ∈ [0,4; Size.Z − 0,4]` bằng cách cắt `dx, dz` trước khi gọi `Move`.
13. **Camera theo `FollowCamera`:**
    - Tâm xoay = chân + (0; 1,6; 0). Góc ngẩng kẹp trong [−20°; 70°], ban đầu 20°.
    - Khoảng cách ban đầu 6, kẹp trong [2,5; 14]. Mỗi khung có lăn chuột (`|scroll.y| > 0,01`, không giữ Alt): lăn lên ×0,9, lăn xuống ×1,1.
    - Chống xuyên khối: `SphereCast` bán kính 0,2 từ tâm xoay ra sau với layerMask `Physics.DefaultRaycastLayers` và `QueryTriggerInteraction.Ignore`. Trúng thì khoảng cách thực = `max(hit.distance − 0,1; 0,3)`.
    - Nhân vật nằm ở layer "Ignore Raycast", nên cả tia nhắm ô lẫn `SphereCast` đều bỏ qua nhân vật.
14. **Vị trí xuất hiện (chân), hướng mặt +z (yaw 0):**
    - SimView: `PlainSpawn(plain)` = `(X0 + Size/2 + 0,5; Ground + 1; Z0 + Size/2 + 0,5 − 8)`, cách tâm trại 8 ô về phía −z, nhìn vào trại.
    - TerrainView:
      - checkpoint Map 1 (`Kind == Checkpoint && Map == 0`) còn trong danh sách dấu mốc sau khi san → đứng ở cột `(X; Z − 2)`, chân = `surface + 1` của cột đó (`ColumnInfo`);
      - bị bỏ vì nằm trong đồng bằng → dùng `PlainSpawn(plain)`.
      - Ghi một dòng log `PLAYER xuất hiện ...` cho biết dùng cách nào.
15. **Chờ chunk:** sau `Place(...)`, `Ready = false`. Chưa `Ready` thì không áp input và trọng lực. Mỗi `Update`: `Physics.Raycast` từ chân + (0; 0,5; 0) xuống dưới, dài 64, trúng collider có `terrain.IsTerrainCollider(hit.collider)` → `Ready = true`.
16. **Minimap:** có `PlayerCharacter.Active` → vị trí lấy từ nhân vật, hướng vẫn lấy từ `Camera.main.transform.forward`. Không có (Bench, SimBench, test cũ) → như cũ.
17. **Tham chiếu tĩnh `PlayerCharacter.Active`:** đặt trong `Awake`, xoá trong `OnDestroy`, giống mẫu `SimView.active`. `TerrainEditInput`, `MinimapView`, `DemoPlain`, `SimView` đọc qua đây, không cần nối thêm trong scene ngoài trường `terrain`.
18. **Input:**
    - `PlayerCharacter` có `public bool readKeyboard = true;`, `public Vector2 MoveInput;`, `public bool JumpInput;`.
    - `readKeyboard` bật thì mỗi `Update` ghi `MoveInput`/`JumpInput` từ WASD/Space.
    - Test tắt `readKeyboard` rồi tự gán. Brief cảm ứng sau cũng ghi vào hai trường này.
19. Sương biên (`WorldEdgeFog`) và dưới nước (`UnderwaterView`) vẫn theo camera. Camera luôn ở gần nhân vật nên không đổi gì.

## Phạm vi
- **Được tạo:**
  - `Core/PlayerRules.cs`;
  - `Client/PlayerCharacter.cs`, `Client/FollowCamera.cs`;
  - `Tests/Core/PlayerRulesTests.cs`, `Tests/Client/PlayerCharacterPlayTests.cs`;
  - kèm file `.meta` Unity tự sinh.
- **Được sửa:**
  - `Core/WorldEditor.cs` (chỉ thêm móc `IsPlayerAt` + kiểm trong `CheckPlace`, và thêm `EditResult.OutOfReach` vào **cuối** enum);
  - `Client/TerrainEditInput.cs`, `Client/MinimapView.cs` (chỉ hai chỗ lấy vị trí), `Client/DemoPlain.cs`, `Client/SimView.cs` (chỉ `OnAfterGenerate`);
  - `Tests/Core/WorldEditorTests.cs` (thêm test);
  - scene `Client/Scenes/SimView.unity`, `Client/Scenes/TerrainView.unity` (qua MCP).
- **Không được sửa:**
  - `DebugFlyCamera.cs`, `TerrainChunkView.cs`, `UnderwaterView.cs`, `WorldEdgeFog.cs`, `Core/Sim/*`;
  - scene Bench / SimBench / HashCheck;
  - `design/`, task contract, `ProjectSettings/` (ngoài bước dọn).
- Test cũ khác đỏ vì thay đổi này → dừng, ghi Vướng mắc.

## Các bước
Đường dẫn tính từ `client/Assets/Game/`.

1. **Lõi `Core/PlayerRules.cs`** (namespace `Game.Core`, không `using UnityEngine`):
   ```csharp
   using System;

   namespace Game.Core
   {
       /// <summary>
       /// Luật nhân vật người chơi M1 (world 7h, 0028): tầm với 4 ô từ tâm thân, ô bị thân chiếm, biên world, trong nước.
       /// Gốc nhân vật = chân (giữa đáy); thân cao BodyHeight, nửa rộng HalfWidth.
       /// </summary>
       public static class PlayerRules
       {
           public const float Reach = 4f;
           /// <summary>Tâm thân cao hơn chân chừng này (nhân vật 2 ô).</summary>
           public const float BodyCenterHeight = 1f;
           public const float HalfWidth = 0.4f;
           public const float BodyHeight = 1.8f;
           public const float WaterSpeedFactor = 0.5f;
           const float Shrink = 0.001f;

           /// <summary>Tâm ô (x, y, z) cách tâm thân (chân + BodyCenterHeight) ≤ Reach (Euclid).</summary>
           public static bool InReach(float feetX, float feetY, float feetZ, int x, int y, int z)
           {
               float dx = x + 0.5f - feetX, dy = y + 0.5f - (feetY + BodyCenterHeight), dz = z + 0.5f - feetZ;
               return dx * dx + dy * dy + dz * dz <= Reach * Reach + 1e-4f;
           }

           /// <summary>Hộp thân (co Shrink mỗi phía) giao ô (x, y, z).</summary>
           public static bool Occupies(float feetX, float feetY, float feetZ, int x, int y, int z) =>
               feetX + HalfWidth - Shrink > x && feetX - HalfWidth + Shrink < x + 1 &&
               feetZ + HalfWidth - Shrink > z && feetZ - HalfWidth + Shrink < z + 1 &&
               feetY + BodyHeight - Shrink > y && feetY + Shrink < y + 1;

           /// <summary>Cắt bước đi ngang để chân luôn trong [HalfWidth, Size − HalfWidth] theo x và z.</summary>
           public static void ClampToWorld(WorldSize s, float feetX, float feetZ, ref float dx, ref float dz)
           {
               dx = Math.Min(Math.Max(feetX + dx, HalfWidth), s.X - HalfWidth) - feetX;
               dz = Math.Min(Math.Max(feetZ + dz, HalfWidth), s.Z - HalfWidth) - feetZ;
           }

           /// <summary>Ô chứa điểm chân + 0,5 là nước (ngoài world → false).</summary>
           public static bool InWater(WorldData world, float feetX, float feetY, float feetZ)
           {
               int x = (int)Math.Floor(feetX), y = (int)Math.Floor(feetY + 0.5f), z = (int)Math.Floor(feetZ);
               return world.Size.InBounds(x, y, z) && world.GetMaterial(x, y, z) == BlockMaterial.Water;
           }
       }
   }
   ```
   Xong khi biên dịch không lỗi (`read_console`).

2. **`Core/WorldEditor.cs`:**
   - Thêm **cuối** `enum EditResult`:
     ```csharp
         /// <summary>Ô ngoài tầm với của nhân vật (world 7h) — client trả, editor không dùng.</summary>
         OutOfReach,
     ```
   - Sau `IsOccupied` (`:50`) thêm:
     ```csharp
     /// <summary>Móc ô bị nhân vật người chơi chiếm (world 7h) — tách khỏi IsOccupied vì Simulation.AttachEditor ghi đè móc đó.</summary>
     public Func<int, int, int, bool> IsPlayerAt;
     ```
   - Trong `CheckPlace`, ngay sau khối `IsOccupied` (`:98-99`):
     ```csharp
     if (IsPlayerAt != null && IsPlayerAt(x, y, z))
         return EditResult.Occupied;
     ```
   Xong khi biên dịch không lỗi.

3. **Test EditMode** — `Tests/Core/PlayerRulesTests.cs` (namespace như các test Core cũ; xem dòng `namespace` ở đầu `Tests/Core/WorldEditorTests.cs`):
   - `InReach_BoundaryExactly4`: chân (0,5; −0,5; 0,5), nên tâm thân (0,5; 0,5; 0,5).
     - Ô (4,0,0) → true (khoảng cách 4).
     - Ô (5,0,0) → false.
     - Ô (0,4,0) → true.
     - Ô (0,0,−4) → true.
   - `InReach_4Point1Outside`: chân (0,4; −0,5; 0,5), ô (4,0,0) → false (khoảng cách 4,1).
   - `Occupies_TwoCellsWhenCentered`: chân (5,5; 3,0; 5,5).
     - Chiếm (5,3,5) và (5,4,5).
     - Không chiếm (5,2,5), (5,5,5), (6,3,5), (4,3,5).
   - `Occupies_StraddlingColumns`: chân (5,95; 3,0; 5,5) → chiếm (5,3,5) và (6,3,5); chân (5,5; 3,0; 5,5) → không chiếm (6,3,5).
   - `ClampToWorld`: `WorldSize(32,16,32)`.
     - Chân x = 0,5, dx = −1 → x + dx = 0,4.
     - Chân x = 31,5, dx = +1 → 31,6.
     - dx = 0,05 giữa map → giữ nguyên. Tương tự với z.
   - `InWater`: world 8×8×8, ô (2,1,2) là `Cell.Make(BlockMaterial.Water, 7)`.
     - Chân (2,5; 1,0; 2,5) → true.
     - Chân (2,5; 2,0; 2,5) → false.
     - Chân (−1; 1; 2) → false.
   - Thêm vào `Tests/Core/WorldEditorTests.cs`: `PlayerAtBlocksPlace`.
     - `editor.IsPlayerAt = (x, y, z) => x == 4 && y == 1 && z == 4;`, kho có 1 `Dirt`.
     - `CheckPlace(4,1,4,Dirt)` và `TryPlace(4,1,4,Dirt)` đều ra `EditResult.Occupied`.
     - Kho vẫn 1, `changed` rỗng.
     - Ô (5,1,4) → `Ok`.

   Xong khi các test này đạt (`run_tests` EditMode lọc theo tên lớp).

4. **`Client/FollowCamera.cs`** — `[RequireComponent(typeof(Camera))] public class FollowCamera : MonoBehaviour`.
   - Field `public`:
     - `PlayerCharacter target;`
     - `float distance = 6f, minDistance = 2.5f, maxDistance = 14f;`
     - `float pitch = 20f, minPitch = -20f, maxPitch = 70f;`
     - `float yaw;`
     - `float lookSensitivity = 0.15f;`
     - `float pivotHeight = 1.6f, castRadius = 0.2f;`
   - Thuộc tính `public float CurrentDistance { get; private set; }`.
   - `public void SetAngles(float yawDeg, float pitchDeg)`: gán `yaw`, gán `pitch` đã kẹp.
   - `LateUpdate` (bỏ qua nếu `target == null`):
     1. Xoay: `Mouse.current` có, giữ `middleButton`, không `MinimapView.IsAnyOpen` → `yaw += d.x * lookSensitivity; pitch = Clamp(pitch - d.y * lookSensitivity, minPitch, maxPitch)` (giống `DebugFlyCamera.cs:62-64`).
     2. Thu phóng: đọc `scroll.y` khi **không** giữ `Keyboard.current.altKey` và con trỏ không bị chặn — cùng ba điều kiện `MinimapView.BlocksPointer`/`TimeDial.BlocksPointer`/`SimView.BlocksPointer` như `TerrainEditInput.cs:72`. `> 0.01` → `distance *= 0.9f`; `< -0.01` → `distance *= 1.1f`; rồi kẹp vào [min, max].
     3. `rot = Quaternion.Euler(pitch, yaw, 0)`; `pivot = target.transform.position + Vector3.up * pivotHeight`; `back = rot * Vector3.back`.
     4. `CurrentDistance = distance`. Nếu `Physics.SphereCast(pivot, castRadius, back, out var hit, distance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)` trúng → `CurrentDistance = Mathf.Max(hit.distance - 0.1f, 0.3f)`.
     5. `transform.SetPositionAndRotation(pivot + back * CurrentDistance, rot)`.
   - Thêm `public void SnapNow()` chạy bước 3–5 ngay; test và `PlayerCharacter.SetFlyMode(false)` dùng hàm này.

   Xong khi biên dịch không lỗi.

5. **`Client/PlayerCharacter.cs`** — `[RequireComponent(typeof(CharacterController))] public class PlayerCharacter : MonoBehaviour`.
   - **Field:**
     - `public TerrainChunkView terrain;`
     - `public float walkSpeed = 5f, gravity = 25f, jumpSpeed = 8f, maxFallSpeed = 30f, bobHeight = 0.06f, bobFrequency = 12f;`
     - `public bool readKeyboard = true; public Vector2 MoveInput; public bool JumpInput;`
   - **Thuộc tính:**
     - `public static PlayerCharacter Active { get; private set; }`
     - `public bool Ready { get; private set; }`, `public bool FlyMode { get; private set; }`
     - `public bool InWater { get; private set; }`, `public float VerticalSpeed => vy;`
     - `public CharacterController Controller { get; private set; }`
     - `public Transform Visual { get; private set; }`
     - `public Vector3 FeetLocal => terrain.transform.InverseTransformPoint(transform.position);`
   - **`Awake`:**
     - `Active = this`.
     - Lấy `Controller`, gán thông số QĐ 10.
     - `gameObject.layer = LayerMask.NameToLayer("Ignore Raycast")`.
     - Có `terrain` → `terrain.streamTarget = transform`.
     - Dựng hình (xem mục "Hình" dưới).
   - **`OnDestroy`:** `if (Active == this) Active = null;`.
   - **`Start`:**
     - `cam = Camera.main`; lấy `follow = cam?.GetComponent<FollowCamera>()`, `fly = cam?.GetComponent<DebugFlyCamera>()`.
     - `follow != null` → `follow.target = this`.
     - Gọi `SetFlyMode(false)`.
   - **`public void Place(Vector3 feetWorld, float yawDeg)`:**
     - `Controller.enabled = false; transform.SetPositionAndRotation(feetWorld, Quaternion.identity); Controller.enabled = true;`
     - Hình (`Visual`) quay theo `yawDeg`; `vy = 0; Ready = false`.
     - `follow` đã có → `follow.SetAngles(yawDeg, 20f)`; `follow` chưa có (gọi trước `Start`) → lưu `yawDeg`, `Start` gọi `SetAngles` sau.
   - **`public void SetFlyMode(bool on)`:**
     - Không có `fly` → `FlyMode = false; return`.
     - `FlyMode = on; fly.enabled = on;`; `on` → `fly.SyncAngles()`.
     - `follow != null` → `follow.enabled = !on`; `!on` → `follow.SnapNow()`.
   - **`public bool InReach(Vector3Int cell)`:** `var f = FeetLocal; return PlayerRules.InReach(f.x, f.y, f.z, cell.x, cell.y, cell.z);`
   - **`public bool OccupiesCell(int x, int y, int z)`:** như trên với `PlayerRules.Occupies`.
   - **Nối editor:** trong `Update`, khi `terrain.Editor != null` và `terrain.Editor.IsPlayerAt == null` → `terrain.Editor.IsPlayerAt = OccupiesCell`. `TerrainChunkView.Build` tạo editor mới nên phải nối lại mỗi khi `IsPlayerAt` rỗng.
   - **`Update`:**
     1. `terrain == null || terrain.World == null` → return.
     2. `Keyboard.current?.f3Key.wasPressedThisFrame` → `SetFlyMode(!FlyMode)`.
     3. Chưa `Ready` → `Physics.Raycast(transform.position + Vector3.up * 0.5f, Vector3.down, out var hit, 64f)` và `terrain.IsTerrainCollider(hit.collider)` → `Ready = true`; vẫn chưa thì return.
     4. `readKeyboard && !FlyMode` → `MoveInput` = (D − A, W − S) từ `Keyboard.current`, `JumpInput = spaceKey.isPressed`. `FlyMode` → `MoveInput = Vector2.zero; JumpInput = false`.
     5. `var f = FeetLocal; InWater = PlayerRules.InWater(terrain.World, f.x, f.y, f.z);`
     6. Hướng: `yaw` = `follow.yaw` nếu có `follow`, không thì 0. `dir = Quaternion.Euler(0, yaw, 0) * new Vector3(MoveInput.x, 0, MoveInput.y)`; độ dài > 1 thì chuẩn hoá. `h = dir * walkSpeed * (InWater ? PlayerRules.WaterSpeedFactor : 1f) * Time.deltaTime`.
     7. Trọng lực/nhảy:
        - `Controller.isGrounded && vy < 0` → `vy = -2f`;
        - `Controller.isGrounded && JumpInput` → `vy = jumpSpeed`;
        - `vy = Mathf.Max(vy - gravity * Time.deltaTime, -maxFallSpeed)`.
     8. Biên: đổi `h` sang toạ độ terrain (`terrain.transform.InverseTransformVector`), `PlayerRules.ClampToWorld(terrain.World.Size, f.x, f.z, ref lx, ref lz)`, đổi lại thế giới.
     9. `Controller.Move(h + Vector3.up * (vy * Time.deltaTime))`.
     10. Hình:
         - `dir.sqrMagnitude > 0.01f` → `Visual.rotation = Quaternion.Slerp(Visual.rotation, Quaternion.LookRotation(dir), 15f * Time.deltaTime)`;
         - nhún: `isGrounded` và đang đi → `Visual.localPosition = new Vector3(0, bobHeight * Mathf.Abs(Mathf.Sin(Time.time * bobFrequency)), 0)`, không thì `Vector3.zero`.
   - **Hình** (`Visual` = con "Hình" của nhân vật; mỗi khối là con của `Visual`, **xoá Collider** của primitive):
     - Material: `new Material(Resources.Load<UnityEngine.Material>("Materials/Landmark"))` + `SetColor("_BaseColor", c)`; không có material thì dùng `GraphicsSettings.currentRenderPipeline.defaultMaterial`, như `SimView.cs:463`.
     - Các khối:

       | Khối | Tâm (local) | Kích thước | Màu |
       |---|---|---|---|
       | Thân | (0; 0,55; 0) | (0,8; 1,1; 0,6) | (0,20; 0,45; 0,85) |
       | Đầu | (0; 1,48; 0) | (0,7; 0,7; 0,7) | (0,95; 0,80; 0,62) |
       | Mũi (chỉ hướng) | (0; 1,48; 0,4) | (0,15; 0,15; 0,15) | (0,15; 0,15; 0,15) |

   - **Static helper** (dùng ở bước 7):
     ```csharp
     /// <summary>Chỗ xuất hiện SimView / dự phòng TerrainView (QĐ 14): cách tâm đồng bằng 8 ô về −z, chân trên mặt đất.</summary>
     public static Vector3 PlainSpawn(StressPlain p) => new Vector3(p.X0 + p.Size / 2 + 0.5f, p.Ground + 1f, p.Z0 + p.Size / 2 + 0.5f - 8f);

     /// <summary>TerrainView (QĐ 14): cạnh checkpoint Map 1 (2 ô về −z) nếu còn; không thì PlainSpawn.</summary>
     public static Vector3 TerrainViewSpawn(WorldData world, IReadOnlyList<Landmark> kept, StressPlain plain, out bool atCheckpoint)
     ```
     `TerrainViewSpawn`:
     - tìm `l` trong `kept` với `l.Kind == LandmarkKind.Checkpoint && l.Map == 0`;
     - có → `ColumnInfo(world, l.X, l.Z - 2, out s, out _)` → `atCheckpoint = true`, trả `(l.X + 0.5f, s + 1f, l.Z - 2 + 0.5f)`;
     - không → `atCheckpoint = false`, trả `PlainSpawn(plain)`.
     - Toạ độ trả về là **toạ độ terrain**. Nơi gọi đổi sang thế giới bằng `terrain.transform.TransformPoint` (scene hiện để terrain ở gốc).

   Xong khi biên dịch không lỗi.

6. **`Client/TerrainEditInput.cs`:**
   - **Lăn chọn loại** (`:77-83`): chỉ đổi `selected` khi `kb != null && kb.altKey.isPressed`. Comment thay bằng: `// Alt + lăn chuột đổi loại (lăn không Alt = thu phóng camera nhân vật, user 2026-10-22).`
   - **`:105`** đổi thành:
     ```csharp
     var player = PlayerCharacter.Active;
     // Camera bay (không có nhân vật, hoặc chế độ debug F3): chỉ đào/đặt khi dừng (user 2026-10-20). Có nhân vật: được dùng công cụ khi đang đi (0028).
     if ((player == null || player.FlyMode) && DebugFlyCamera.IsMoveKeyHeld(kb)) return;
     ```
   - **`ShowAim`:** sau `if (!picked || view.Editor == null) return;` thêm:
     ```csharp
     var player = PlayerCharacter.Active;
     if (player != null && !player.InReach(Tool == EditTool.Place ? placeCell : digCell)) return; // ngoài tầm 4 ô: không khung, không bản mờ (0028)
     ```
   - **`UseTool`:** đầu hàm thêm:
     ```csharp
     var player = PlayerCharacter.Active;
     if (player != null && !player.InReach(Tool == EditTool.Place ? placeCell : digCell))
         return EditResult.OutOfReach; // click ngoài tầm: không làm gì, không đổi dòng kết quả (0028)
     ```
   - **`OnGUI` dòng `:214`** thay bằng ba trường hợp (giữ nguyên phần `Công cụ: …(Tab đổi)` ở đầu):
     - Có nhân vật, không bay:
       `" · Chuột trái: dùng công cụ (trong 4 ô quanh nhân vật) · 1–4 / Alt + lăn chuột: chọn loại · WASD: đi · Space: nhảy · giữ chuột giữa: xoay · lăn chuột: gần/xa · F3: camera bay\n"`
     - Có nhân vật, đang bay:
       `" · Chuột trái: dùng công cụ (trong 4 ô quanh nhân vật) · 1–4 / Alt + lăn chuột: chọn loại · Camera bay: WASD, Q/E, giữ chuột giữa · dừng bay mới đào/đặt được · F3: về nhân vật\n"`
     - Không có nhân vật:
       `" · Chuột trái: dùng công cụ · 1–4 / Alt + lăn chuột: chọn loại · WASD, Q/E, giữ chuột giữa: camera · dừng bay mới đào/đặt được\n"`
   - Summary đầu lớp (`:12-17`): thêm một dòng `/// World 7h (brief 0035): có nhân vật (PlayerCharacter.Active) → đào/đặt/xới chỉ trong tầm 4 ô, được dùng khi đang đi; Alt + lăn chọn loại.`

   Xong khi biên dịch không lỗi.

7. **Đặt nhân vật lúc sinh world:**
   - `Client/DemoPlain.cs` `OnAfterGenerate`, sau `PlaceCamera(pos, Pitch);`:
     ```csharp
     var player = PlayerCharacter.Active;
     if (player != null)
     {
         var feet = PlayerCharacter.TerrainViewSpawn(world, kept, plain, out bool atCp);
         player.Place(transform.TransformPoint(feet), 0f);
         Debug.Log($"PLAYER xuất hiện {(atCp ? "cạnh checkpoint Map 1" : "giữa đồng bằng (checkpoint Map 1 đã bỏ khi san)")} ({feet.x:F1}, {feet.y:F1}, {feet.z:F1})");
     }
     ```
   - `Client/SimView.cs` `OnAfterGenerate`, nhánh **không** `stressBench` (sau `DemoPlain.PlaceCamera(DemoPlain.CameraPos(plain), DemoPlain.Pitch);`): nếu `PlayerCharacter.Active != null` → `Place(transform.TransformPoint(PlayerCharacter.PlainSpawn(plain)), 0f)` + log `PLAYER xuất hiện ở trại (...)`. Đổi nhánh `if/else` hiện tại thành khối `{ }` cho rõ.

   Xong khi biên dịch không lỗi.

8. **`Client/MinimapView.cs`:**
   - Ở `:344` và ở dòng tương ứng trong `RefreshCorner` (`var p = view.transform.InverseTransformPoint(cam.transform.position);`), thay `cam.transform.position` bằng:
     `(PlayerCharacter.Active != null ? PlayerCharacter.Active.transform.position : cam.transform.position)`
   - Hướng (`cam.transform.forward`) giữ nguyên.
   - Thêm comment `// World 7h (brief 0035): có nhân vật → vị trí là nhân vật, hướng vẫn là camera.`

9. **Test PlayMode** — `Tests/Client/PlayerCharacterPlayTests.cs`.
   - **Fixture** (giống `DemoPlainPlayTests.cs:17-41`):
     - `camGo` có tag `MainCamera`, các component `Camera`, `DebugFlyCamera`, `FollowCamera`, `UnderwaterView`.
     - `go` "Terrain" có `TerrainChunkView` (`source = WorldSource.MapsV3`, `streaming = true`), `DemoPlain`, `TerrainEditInput`.
     - Nhân vật: `playerGo = new GameObject("Player"); playerGo.SetActive(false); player = playerGo.AddComponent<PlayerCharacter>(); player.terrain = view; player.readKeyboard = false; playerGo.SetActive(true);`. Làm vậy để `Awake` thấy `terrain`.
     - Gán `camGo.GetComponent<UnderwaterView>().terrain = view`.
     - `yield return null` hai lần, rồi chờ tối đa 60 khung tới khi `player.Ready`.
     - `TearDown`: huỷ `playerGo`, `go`, `camGo`; `yield return null`.
   - **Helper:**
     - `IEnumerator Run(Vector2 move, bool jump, float seconds)`: gán input, chạy khung tới khi hết `seconds` theo `Time.deltaTime`, rồi trả input về 0.
     - `IEnumerator Teleport(Vector3 feetLocal)`: `player.Place(view.transform.TransformPoint(feetLocal), 0f)`, chờ tới khi `Ready` (tối đa 300 khung — chỗ xa phải chờ nạp chunk), rồi `Run(0, false, 0.3f)` cho đứng yên.
     - `Vector3Int C(...)`: tiện tạo ô.
     - Gốc thử A = `(plain.X0 + 6, plain.Ground, plain.Z0 + 6)` — góc đồng bằng, xa trại TerrainView. Lấy `plain` từ `go.GetComponent<DemoPlain>().Plain.Value`.
     - Xây khối: `view.Inventory.Add(BlockMaterial.Stone, n)` rồi `view.Place(cell, BlockMaterial.Stone)`, assert `Ok`. Chờ 2 khung để collider bake.
   - **Các test:**
     1. `SpawnsAndStandsOnGround`:
        - `player.Ready`;
        - sau 1 giây đứng yên `Controller.isGrounded`;
        - nếu log ghi xuất hiện giữa đồng bằng thì `FeetLocal` ≈ `PlainSpawn(plain)`, sai số 0,1 theo x/z; nếu cạnh checkpoint thì `|y − (surface + 1)| < 0,1`;
        - `view.streamTarget == player.transform`.
     2. `WalksAlongCameraYaw`:
        - teleport tới A + (0,5; 1; 0,5);
        - `follow.SetAngles(90, 20)`, `Run((0,1), false, 1)`;
        - Δx ≥ 3,5, |Δz| < 0,5 (đi theo +x vì yaw 90).
     3. `JumpsOneBlockStepButNotTwo`:
        - teleport tới A + (0,5; 1; 0,5); đặt bậc 1 ô ở các ô `(A.x−1..A.x+1, G+1, A.z+2)`;
        - `SetAngles(0,20)`, `Run((0,1), true, 1,5)` → chân y ≥ G + 2 − 0,1 và z > A.z + 2;
        - teleport tới A + (6,5; 1; 0,5); đặt tường 2 ô ở `(A.x+5..A.x+7, G+1..G+2, A.z+2)`;
        - `Run((0,1), true, 1,5)` → z < A.z + 2 − 0,3 và chân y < G + 2.
        - `G = plain.Ground`.
     4. `DoesNotWalkThroughBlocks`: teleport tới A + (0,5; 1; 0,5); tường 2 ô như trên ở `z = A.z + 2`; `Run((0,1), false, 1,5)` → z ≤ A.z + 2 − 0,4 + 0,05.
     5. `FallsWhenGroundDugBelow`:
        - teleport tới A + (0,5; 1; 0,5);
        - `view.Dig` ba ô `(A.x, G, A.z)`, `(A.x, G−1, A.z)`, `(A.x, G−2, A.z)`, assert `Ok`;
        - `Run(0, false, 1)` → chân y < G − 1.
     6. `StaysInsideWorldBounds`:
        - cột `x = 1, z = A.z`, `ColumnInfo` → mặt `s` và độ sâu nước `w`;
        - teleport tới `(1,5; s + w + 1; A.z + 0,5)`; nhân vật có thể chìm, vẫn được;
        - `SetAngles(270,20)` (đi về −x), `Run((0,1), false, 2)` → `FeetLocal.x ≥ 0,4 − 0,01`.
     7. `WaterHalvesSpeed`:
        - tìm cột nước sâu: quét `x` từ 1 tới 60 ở `z = A.z`, lấy cột đầu có `waterDepth ≥ 4`; không có thì `Assert.Inconclusive`;
        - teleport tới `(x + 0,5; s + 1; A.z + 0,5)`, chờ chạm đáy (`isGrounded`, tối đa 3 giây);
        - `player.InWater == true`;
        - `SetAngles(90,20)`, đo quãng đi được trong 1 giây ở nước, tính `dWater`; đo `dLand` ở A + (0,5; 1; 0,5) giống vậy;
        - `dWater / dLand` trong [0,4; 0,6].
        - Rồi ở chỗ nước: `follow.pitch = 0`, `follow.distance = follow.minDistance`, `follow.SnapNow()`, chờ 2 khung (thứ tự `LateUpdate` giữa FollowCamera và UnderwaterView không cố định) → `camGo.GetComponent<UnderwaterView>().IsUnderwater == true`. Mặt nước cao hơn chân ≥ 4 ô, camera cao 1,6.
     8. `CameraPullsInWhenBlocked`:
        - teleport tới A + (0,5; 1; 0,5); `SetAngles(0,10)`, `distance = 6`, chờ 1 khung → `CurrentDistance ≈ 6` (±0,01);
        - đặt cột đá cao 3 ô ở `(A.x, G+1..G+3, A.z − 2)` (sau lưng), chờ 2 khung → `CurrentDistance < 2` và ô chứa camera (`view.CellAt(camGo.transform.position)`) không phải ô đặc.
     9. `ReachLimitsTools`:
        - teleport tới A + (0,5; 1; 0,5); `tei = go.GetComponent<TerrainEditInput>()`;
        - **Đào:**
          - ô `(A.x+4, G, A.z)`, khoảng cách từ tâm thân ≈ 4,3 (chân ≈ G + 1,04 do skinWidth) → `tei.UseTool(cell, cell)` (Tool đang là Đào) ra `OutOfReach`, vật liệu ô không đổi;
          - `tei.ShowAim(true, cell, cell)` → `OutlineVisible == false`;
          - ô `(A.x+3, G, A.z)` (≈ 3,4) → `Ok`, ô thành `Empty`.
        - **Đặt:**
          - `ToggleTool()` sang Đặt, kho +1 `Dirt` (`SelectedMaterial` mặc định là `Dirt` = `Palette[0]`, không cần chọn);
          - placeCell `(A.x+5, G+1, A.z)` → `OutOfReach`, ô vẫn `Empty`;
          - `ShowAim(true, c, c)` → `GhostVisible == false`.
     10. `CannotPlaceIntoPlayerCells`:
         - teleport tới A + (0,5; 1; 0,5), kho +2 `Dirt`;
         - `view.Place((A.x, G+1, A.z))` → `Occupied`;
         - `player.OccupiesCell(A.x, G+2, A.z)` true, `(A.x, G+3, A.z)` false. Không thử `Place` ở G+2: ô đó không kề khối đặc nên `CheckPlace` trả `NotAttached` trước khi kiểm chiếm;
         - `(A.x+1, G+1, A.z)` → `Ok`.
     11. `FlyModeToggle`:
         - `player.SetFlyMode(true)` → `DebugFlyCamera.enabled`, `!FollowCamera.enabled`;
         - gán `MoveInput = (0,1)` rồi chạy 0,5 giây với `readKeyboard = false` → vị trí nhân vật không đổi (±0,05). `FlyMode` phải xoá input ở bước 5.4, kể cả khi `readKeyboard = false`;
         - dời camera ra xa 20 ô bằng `camGo.transform.position += Vector3.up * 20` → `player.InReach` với ô `(A.x+3, G, A.z)` vẫn true;
         - `SetFlyMode(false)` → `FollowCamera.enabled`, khoảng cách camera tới tâm xoay ≤ `maxDistance`.
     12. `MinimapFollowsPlayer`:
         - thêm `MinimapView` vào `go`, theo cách fixture của `Tests/Client/MinimapViewPlayTests.cs` (đọc file đó, làm giống phần SetUp);
         - teleport nhân vật; `map.SetOpen(true)`; dời camera lệch 10 ô theo x; chờ 1 khung;
         - `map.MarkerPanelPosition` ≈ `Viewport.WorldToPanel` của nhân vật (±0,5), **không** phải của camera.

   - **Lưu ý:**
     - Mọi chỗ chờ có giới hạn khung/giây, hết hạn thì `Assert.Fail` kèm thông tin.
     - Test 9 bước Đặt: `UseTool` kiểm tầm trước khi gọi editor, nên ô xa luôn ra `OutOfReach`, không ra `NotAttached`.

10. **Scene (qua MCP; mở từng scene, sửa, lưu):** làm với `SimView.unity` và `TerrainView.unity`.
    - `Main Camera`: thêm `FollowCamera`. Giữ `DebugFlyCamera`; nhân vật tự tắt nó lúc `Start`.
    - Tạo GameObject gốc `Người chơi` có `PlayerCharacter`; `CharacterController` tự thêm. Gán `terrain` = `TerrainChunkView` của `Terrain`. Vị trí bất kỳ, `Place` sẽ đặt lại.
    - **Không** đụng Bench / SimBench / HashCheck.
    - Xong khi:
      - `git diff --stat` chỉ có 2 scene này;
      - vào Play từng scene, console có dòng `PLAYER xuất hiện`, không có lỗi;
      - chụp **1** ảnh Game view mỗi scene: thấy nhân vật đứng trên đất, camera sau lưng.

## Tiêu chí xong
- Biên dịch không lỗi, không cảnh báo mới trong code đã sửa.
- Test EditMode mới (`PlayerRulesTests`, `WorldEditorTests.PlayerAtBlocksPlace`) đạt. **Toàn bộ** EditMode đạt.
- 12 test `PlayerCharacterPlayTests` đạt (test 7 được phép `Inconclusive` nếu không có cột nước sâu ≥ 4 — ghi rõ). **Toàn bộ** PlayMode đạt.
- `Game.Core` không `using UnityEngine` (`grep -L "UnityEngine" Core/PlayerRules.cs` in ra tên file).
- Hai scene có nhân vật, log `PLAYER xuất hiện`. Bench/SimBench/HashCheck không đổi.

## Test sau khi xong

### Phần cần test
- **Tầm với (lõi):** `PlayerRulesTests.InReach_*` → đúng 4 thì trong, 4,1 thì ngoài.
- **Ô chiếm (lõi + editor):** `PlayerRulesTests.Occupies_*`, `WorldEditorTests.PlayerAtBlocksPlace` → `Occupied`, kho và ô không đổi.
- **Biên, nước (lõi):** `PlayerRulesTests.ClampToWorld`, `PlayerRulesTests.InWater`.
- **Xuất hiện + chờ chunk + nạp chunk theo nhân vật:** PlayMode test 1.
- **Đi theo hướng camera:** test 2.
- **Nhảy bậc 1 ô, không lên bậc 2 ô:** test 3.
- **Không xuyên khối:** test 4.
- **Rơi khi đào dưới chân:** test 5.
- **Biên world:** test 6.
- **Nước ×0,5 + camera dưới nước:** test 7.
- **Camera không xuyên khối:** test 8.
- **Tầm với khi đào/đặt, không khung/bản mờ ngoài tầm:** test 9.
- **Không đặt vào ô nhân vật:** test 10.
- **F3 camera bay, nhân vật đứng yên, tầm theo nhân vật:** test 11.
- **Minimap theo nhân vật:** test 12.
- **Regression:**
  - `TerrainEditPlayTests` (đào/đặt khi không có nhân vật — `Active == null`, phải như cũ);
  - `SimFarmUiPlayTests`, `SimViewPlayTests`, `SimViewMapsPlayTests` (`OnAfterGenerate` đổi);
  - `DemoPlainPlayTests` (`OnAfterGenerate` đổi);
  - `MinimapViewPlayTests`, `MinimapCornerPlayTests` (không có nhân vật → theo camera như cũ);
  - `FlyCameraMoveKeyPlayTests`, `FlyCameraWaterPlayTests`, `WorldBoundsPlayTests`;
  - `SimBenchPlayTests`.
  - Tất cả nằm trong lần chạy toàn bộ PlayMode.
- **Lăn chuột:** không có test tự động (cần chuột thật) → user xem.

### Lệnh executor chạy
1. `run_tests` (MCP) EditMode, lọc `PlayerRulesTests` và `WorldEditorTests` → đạt.
2. `run_tests` EditMode toàn bộ → ghi số đạt/tổng.
3. `run_tests` PlayMode lọc `PlayerCharacterPlayTests` → đạt.
4. `run_tests` PlayMode toàn bộ → ghi số đạt/tổng.
5. **Dọn sau PlayMode:** `git checkout -- client/ProjectSettings/EditorSettings.asset`.
6. `grep -L "UnityEngine" client/Assets/Game/Core/PlayerRules.cs` → in ra đường dẫn (không chứa UnityEngine).
7. Play SimView rồi TerrainView qua MCP: `read_console` có `PLAYER xuất hiện`, không có lỗi; 1 ảnh mỗi scene. Thoát Play.
8. `git status --short` → chỉ các file trong Phạm vi (+ `.meta` mới). `EditorSettings.asset` đã dọn.

Kết quả ghi gọn: dòng tổng kết + tên test hỏng (nếu có), không dán log dài.

### User tự test
Trên Mac, mở scene **SimView** rồi **TerrainView**, bấm Play:
1. Nhân vật (thân xanh, đầu màu da, mũi đen) đứng trên đất.
   - SimView: trại ở phía trước mặt.
   - TerrainView: cạnh cột checkpoint hoặc giữa đồng bằng — console ghi chỗ nào.
2. WASD đi theo hướng camera, khi đi thì nhún nhẹ. Giữ chuột giữa kéo để xoay quanh nhân vật. Lăn chuột thì camera lại gần/ra xa. **Cảm giác tốc độ đi (5 ô/giây) và xoay có ổn không?**
3. Space nhảy lên bậc 1 ô; bậc 2 ô không lên được. **Độ cao/thời gian nhảy có ổn không?**
4. Đi sát vách hoặc đứng sau cột: camera kéo lại gần, không nhìn xuyên vào trong khối.
5. Đào/đặt (Tab, chuột trái):
   - chỉ ô trong khoảng 4 ô quanh nhân vật có khung/bản mờ; xa hơn không có, click không làm gì;
   - đặt khối vào chỗ nhân vật đứng thì không được;
   - đào ô dưới chân thì rơi xuống.
6. Alt + lăn chuột (hoặc phím 1–4) đổi loại khối; lăn chuột không Alt thì không đổi loại.
7. Đi xuống nước: chậm hơn rõ, chìm xuống đáy, camera dưới nước có màu nước.
8. F3: camera bay (WASD/Q/E), nhân vật đứng yên. F3 lần nữa: camera về sau lưng nhân vật.
9. Bản đồ (M) và ô góc: dấu người chơi nằm ở chỗ nhân vật, mũi tên theo hướng camera.
10. SimView: Xới (Tab tới Xới) trong tầm vẫn đánh dấu được ô, click ô ruộng gần vẫn mở bảng chọn giống.

## Dừng và chuyển lại Opus khi
- Brief mơ hồ hoặc mâu thuẫn với code thật (số dòng lệch nhẹ do commit mới thì tìm theo nội dung; nội dung khác thì dừng).
- Cần một quyết định thuộc AGENTS.md §3.4 hoặc ngoài "Quyết định đã chốt" — ví dụ thông số QĐ 10 làm test 3 không đạt (nhảy không lên bậc 1 ô, hoặc lên được bậc 2 ô). **Không tự đổi thông số**; ghi số đo (đỉnh nhảy thực tế) vào Vướng mắc.
- Cần API Unity không có trong danh sách ở mục Bối cảnh.
- Cùng một lỗi sửa 2 lần vẫn chưa qua.
- Phải sửa file ngoài "Phạm vi".

---
<!-- Từ đây trở xuống: EXECUTOR ghi. -->

## Kết quả

## Kết quả test

## Vướng mắc

---
<!-- PLANNER ghi khi review. -->

## Review
