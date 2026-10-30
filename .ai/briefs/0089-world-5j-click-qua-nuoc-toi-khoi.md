# Brief 0089 — World 5j (lỗi 11): click qua nước liền tới khối đặc thì đích là khối

> Trạng thái: chờ review
> Review: đầy đủ + user xem (gộp cuối 5j)
> Model thực thi: Opus · medium
> Cập nhật: 2026-10-30 23:55

## Mục tiêu
Lỗi 11 của task [5j](../tasks/m1-world-5j-sua-loi-choi-thu.md). Khi click qua mặt ao, click lúc camera ở dưới nước, hay click vào khối đặt trong cột thác, đích đi hiện là chỗ tia **đi vào nước**. Gốc tia trong nước thì đích là gốc tia (t = 0). Sửa để đích là **mặt khối đặc** khi tia đi trong nước liền một mạch tới khối đó. Giữ cách sửa lỗi 4 (brief 0086): click xuyên cột thác có khoảng không phía sau thì vẫn lấy chỗ vào nước.

## Bối cảnh cần đọc
- `client/Assets/Game/Client/PlayerCharacter.cs:117–150` — `public bool PointOnWorld(Ray ray, out Vector3 pointWorld, out Vector3 normalWorld)`:
  - `Physics.Raycast(ray, out var hitInfo, ClickRayDistance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)`;
  - `limit = hit ? hitInfo.distance : ClickRayDistance`;
  - đổi tia sang toạ độ terrain (`terrain.transform.InverseTransformPoint/InverseTransformDirection`), gọi `WaterRay.FirstWater(...)`; trúng nước → điểm vào nước;
  - không trúng nước thì lấy điểm collider; không có collider thì lấy mặt phẳng ngang qua chân.
- `:153` `ClickMoveRay(Ray ray, bool mark)` → `MoveTo(InverseTransformPoint(p))`. `MoveTo` (`:106`) bỏ y, đích quy về độ cao chân.
- `client/Assets/Game/Core/WaterRay.cs` — `public static bool FirstWater(WorldData world, float ox, float oy, float oz, float dx, float dy, float dz, float maxT, out float t, out int nx, out int ny, out int nz)`:
  - đi từng ô theo Amanatides–Woo, trả ô Water đầu tiên;
  - gốc trong nước → t = 0, pháp tuyến (0,1,0);
  - giới hạn bước `maxSteps = 3 * (ceil(maxT) + 2)`.
- `client/Assets/Game/Core/Cell.cs:4` — `enum BlockMaterial { Empty, Grass, Dirt, Stone, Sand, Water, Bedrock, Farmland }`. `world.GetMaterial(x,y,z)`, `world.Size.InBounds(x,y,z)`.
- Test có sẵn:
  - `client/Assets/Game/Tests/Core/WaterRayTests.cs`: helper `Empty()` world 16³, `WithWater(cell)`, `Water`;
  - `client/Assets/Game/Tests/Client/UnderFallPlayTests.cs`: SetUp có SimView, ao + thác demo; `cx, cz, g`; `PlaceAt`, `Track`; `UnderFallClickThroughColumn` :125.
- Hình học thác demo (brief 0086, `client/Assets/Game/Core/Sim/SimDemo.cs:75–106`):
  - ao: nước y = g, x cx+10..cx+13, z cz−12..cz−9; mặt đáy ao = g;
  - viền cát y = g quanh ao (gồm x = cx+14);
  - cột thác (cx+13, g+1..g+4, cz−11).
- Tính toán của Planner cho tia trong `UnderFallClickThroughColumn` (o = (cx+8,5, g+4), hướng tới (cx+13,5, g+0,9)):
  - tia vào cột ở x = cx+13 (y ≈ g+1,21);
  - xuống ô ao (cx+13, g) ở x ≈ cx+13,34;
  - chạm mặt tây ô cát (cx+14, g) ở y ≈ g+0,59 → nước **liền** tới khối.
  - Vì vậy quyết định 3 (lùi 0,5 ô khi trúng mặt bên) là bắt buộc để test này vẫn đạt.

## Quyết định đã chốt (không được đổi)
1. **Đoạn nước liền:** đi từ ô nước đầu tiên dọc tia tới `maxT`.
   - Mọi ô đi vào lúc t < `maxT` − 1e-3 đều là Water, **hoặc** gặp ô đặc (khác Empty, khác Water) trước khi gặp ô Empty/ngoài world → `reachesEnd = true`.
   - Gặp ô Empty hoặc ngoài world trước → `reachesEnd = false`.
   - Đi hết tới `maxT` mà toàn nước → `true`.
2. `PointOnWorld`: có trúng nước **và** có collider **và** `reachesEnd` → lấy điểm collider (khối đặc trong nước). Các trường hợp khác giữ như cũ:
   - trúng nước không có collider → điểm vào nước;
   - `reachesEnd = false` → điểm vào nước (lỗi 4);
   - không trúng nước → điểm collider / mặt phẳng.
3. Ở nhánh mới (quyết định 2), pháp tuyến collider tính theo toạ độ terrain có `|n.y| < 0,5` (trúng mặt bên khối) → điểm = điểm trúng + `n.normalized × 0,5` (toạ độ terrain): đứng ở ô nước trước mặt khối. Pháp tuyến trả về = pháp tuyến collider.
4. Hàm mới nằm trong `Game.Core` (C# thuần, không `using UnityEngine`). Không đổi chữ ký `FirstWater` (test 0086 dùng). Không đổi `TerrainEditInput.Pick`, `ShouldStepUpToward`, `MoveTo`.

## Phạm vi
- Được sửa/tạo: `client/Assets/Game/Core/WaterRay.cs`, `client/Assets/Game/Client/PlayerCharacter.cs` (chỉ `PointOnWorld` + `<summary>` của nó), `client/Assets/Game/Tests/Core/WaterRayTests.cs`, `client/Assets/Game/Tests/Client/UnderFallPlayTests.cs`, mục Kết quả của brief này.
- Không được sửa: file khác.

## Các bước
1. `WaterRay.cs` — thêm overload:
   ```csharp
   public static bool FirstWater(WorldData world, float ox, float oy, float oz, float dx, float dy, float dz, float maxT,
       out float t, out int nx, out int ny, out int nz, out bool reachesEnd)
   ```
   - Cùng thuật toán. Sau khi gặp ô nước đầu tiên, đi tiếp theo quyết định 1 để tính `reachesEnd`, cùng giới hạn `maxSteps`. Hết bước mà chưa kết luận → `reachesEnd = true` nếu mọi ô đã đi đều là nước.
   - Overload cũ gọi overload mới, bỏ `reachesEnd`.
   - `<summary>` thêm 1 câu: "reachesEnd: từ ô nước đầu tiên tới maxT tia đi liền trong nước (hoặc tới một ô đặc) — không qua ô rỗng / ngoài world (brief 0089)."
   - Xong khi: biên dịch, test cũ `WaterRayTests` đạt.
2. `PlayerCharacter.PointOnWorld` — dùng overload mới, thêm nhánh theo quyết định 2–3. Sửa `<summary>`: "Điểm world dưới tia: ô nước đầu tiên tia đi vào trước collider (brief 0086) — trừ khi tia đi liền trong nước tới collider thì lấy điểm collider, trúng mặt bên thì lùi 0,5 ô theo pháp tuyến (brief 0089); không thì điểm trúng collider, không thì mặt phẳng ngang qua chân; còn lại false." Xong khi: biên dịch.
3. `WaterRayTests.cs` — thêm test (mỗi test một `[Test]`, dùng overload mới):
   - `ReachesEndThroughWaterToSolid`: nước (5..7, 3, 5), đá (8, 3, 5); tia gốc (0,5; 3,5; 5,5) hướng +x, `maxT` = 7,5 → true, t = 4,5, `reachesEnd` true.
   - `AirGapBreaksRun`: nước (5,3,5), ô (6,3,5) rỗng, đá (7,3,5); tia như trên, `maxT` = 6,5 → true, t = 4,5, `reachesEnd` false.
   - `OriginInWaterReachesFloor`: nước (5, 2..4, 5), đá (5,1,5); gốc (5,5; 4,5; 5,5) hướng −y, `maxT` = 2,5 → t = 0, `reachesEnd` true.
   - `WaterToMaxTOnlyReachesEnd`: nước (5..9, 3, 5), không đá; tia +x từ (0,5; 3,5; 5,5), `maxT` = 7 → `reachesEnd` true.
   - `OutOfWorldBreaksRun`: nước (14..15, 3, 5); tia +x từ (12,5; 3,5; 5,5), `maxT` = 10 → `reachesEnd` false.
   - Xong khi: 5 test đạt.
4. `UnderFallPlayTests.cs` — thêm test (giữ SetUp/helper hiện có):
   - `ClickThroughPondSurfaceTargetsBottom`: `PlaceAt(cx+8,5, g+1, cz−10,5)`, rồi ray từ o = (cx+8,5, g+4, cz−10,5) tới (cx+11,5, g, cz−10,5), cả hai đổi sang world bằng `view.transform` như test `UnderFallClickThroughColumn`. `ClickMoveRay(ray, false)` true; `ClickTargetLocal.x` trong [cx+11,3; cx+11,7].
     - Nếu chân ở (cx+8,5, g+1) không đứng được (vd ô đó là cát/cỏ y = g, chân g+1 hợp lệ) mà `PlaceAt` fail → dừng, cần Opus.
   - `ClickFromUnderwaterTargetsBottom`: `PlaceAt(cx+11,5, g, cz−10,5)`; ray gốc (cx+11,5, g+0,5, cz−10,5), trong ao, tới (cx+12,7, g, cz−10,5). `ClickMoveRay` true; `ClickTargetLocal.x` trong [cx+12,5; cx+12,9] (code cũ cho cx+11,5).
   - Xong khi: 2 test mới đạt; `UnderFallClickThroughColumn` vẫn đạt (`ClickTargetLocal.x` trong [cx+13; cx+14)).
5. Chạy test theo "Lệnh executor chạy". Ghi kết quả. Test cũ nào phải đổi kỳ vọng → **dừng, cần Opus** (ghi assertion, giá trị mong/đã thấy, log `UNDERFALL`).

## Tiêu chí xong
- 5 test EditMode mới + 2 test PlayMode mới đạt; mọi test `WaterRayTests`, `UnderFallPlayTests`, `ClickMovePlayTests` cũ đạt không đổi kỳ vọng.
- EditMode toàn bộ đạt; `read_console` Error 0.
- `WaterRay.cs` không có `using UnityEngine`.

## Test sau khi xong

### Phần cần test
- Đoạn nước liền (bước 1) · `ReachesEndThroughWaterToSolid`, `OriginInWaterReachesFloor`, `WaterToMaxTOnlyReachesEnd` · `reachesEnd` true.
- Khoảng không / ngoài world cắt đoạn (bước 1) · `AirGapBreaksRun`, `OutOfWorldBreaksRun` · `reachesEnd` false.
- Overload cũ không đổi (bước 1) · `WaterRayTests` cũ · đạt.
- Click qua mặt ao tới đáy (bước 2) · `ClickThroughPondSurfaceTargetsBottom` · x ≈ cx+11,5.
- Click khi camera dưới nước (bước 2) · `ClickFromUnderwaterTargetsBottom` · x ≈ cx+12,7.
- Click xuyên cột thác, lỗi 4 (bước 2–3, regression) · `UnderFallClickThroughColumn`, `UnderFallClickMoveNoStepUp`, `UnderFallStandStill` · đạt không đổi.
- Click di chuyển trên đất (regression) · `ClickMovePlayTests` · đạt.
- Toàn bộ EditMode (regression) · đạt.

### Lệnh executor chạy
1. Qua MCP Unity (Editor đang mở): `run_tests` EditMode lọc `Game.Core.Tests.WaterRayTests` → số đạt/fail.
2. `run_tests` PlayMode lọc `UnderFallPlayTests`, `ClickMovePlayTests` → dòng tổng kết.
3. `run_tests` EditMode toàn bộ → dòng tổng kết.
4. `read_console` Error → 0.
5. Dọn: `git checkout -- client/ProjectSettings/EditorSettings.asset`; `git status` chỉ còn file trong Phạm vi.
(Editor tắt thì dùng `python3 tools/unity_test.py EditMode` / `PlayMode`.)

### User tự test
Gộp cuối 5j:
- Đặt một khối vào giữa cột thác rồi click lên khối → vòng dấu và đích ở khối, không ở mặt nước phía trước.
- Click qua mặt ao xuống đáy → nhân vật đi tới đúng chỗ dưới đáy.
- Lặn xuống ao (camera dưới nước) rồi click đáy → nhân vật đi tới chỗ click, không đứng yên/đi về phía camera.
- Click vào cột thác (sau là khoảng không) → vẫn đi tới chân thác như sau 0086, không nhảy lên viền cát.

## Dừng và chuyển lại Opus khi
- Brief mơ hồ hoặc mâu thuẫn với code thật.
- Cần một quyết định thuộc AGENTS.md §3.4 hoặc ngoài "Quyết định đã chốt".
- Test cũ nào phải đổi kỳ vọng.
- Cùng một lỗi sửa 2 lần vẫn chưa qua.
- Phải sửa file ngoài "Phạm vi".

---

## Kết quả
- Base commit: 0a6a779
- Chạy bằng: claude-opus-5-5 · medium
- Bắt đầu 2026-10-30 23:25, xong 23:55 (gồm ~5 phút chờ Editor thoát Play Mode).
- Commit: `8cc61fe` — `client/Assets/Game/Core/WaterRay.cs` (overload `FirstWater(..., out bool reachesEnd)`, `WaterRunReachesEnd`; overload cũ gọi overload mới), `client/Assets/Game/Client/PlayerCharacter.cs` (`PointOnWorld` nhánh QĐ 2–3 + `<summary>`), `client/Assets/Game/Tests/Core/WaterRayTests.cs` (5 test), `client/Assets/Game/Tests/Client/UnderFallPlayTests.cs` (helper `ClickRay` + 2 test).
- Không lệch brief.

## Kết quả test
- Đoạn nước liền · `ReachesEndThroughWaterToSolid`, `OriginInWaterReachesFloor`, `WaterToMaxTOnlyReachesEnd` · đạt.
- Khoảng không / ngoài world cắt đoạn · `AirGapBreaksRun`, `OutOfWorldBreaksRun` · đạt.
- Overload cũ không đổi · `WaterRayTests` cũ · đạt.
- Click qua mặt ao · `ClickThroughPondSurfaceTargetsBottom` · đạt (log `UNDERFALL surface target=(134.50, …)` = cx+11,5).
- Click dưới nước · `ClickFromUnderwaterTargetsBottom` · đạt (`UNDERFALL underwater target=(135.70, …)` = cx+12,7).
- Lỗi 4 regression · `UnderFallClickThroughColumn`, `UnderFallClickMoveNoStepUp`, `UnderFallStandStill` · đạt (`ray target=(136.50, …)` = cx+13,5).
- Lệnh 1: EditMode `WaterRayTests` 13/13 đạt.
- Lệnh 2: PlayMode `UnderFallPlayTests` + `ClickMovePlayTests` 13/13 đạt.
- Lệnh 3: EditMode toàn bộ 691/691 đạt (329 s).
- Lệnh 4: `read_console` Error 0.
- Lệnh 5: đã dọn `EditorSettings.asset`; `git status` sạch ngoài brief. `WaterRay.cs` không có `using UnityEngine`.
- User tự test (gộp cuối 5j): chờ user.

## Vướng mắc

---

## Review
