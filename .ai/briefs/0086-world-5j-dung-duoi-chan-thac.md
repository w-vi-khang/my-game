# Brief 0086 — World 5j (lỗi 4): đứng dưới chân thác không bị đẩy lên 1 ô

> Trạng thái: chờ làm
> Review: đầy đủ + user xem (gộp cuối 5j)
> Model thực thi: Sonnet · medium
> Cập nhật: 2026-10-29 20:02

## Mục tiêu
Lỗi 4 của task [5j](../tasks/m1-world-5j-sua-loi-choi-thu.md): user click để đi tới dưới chân thác, nhưng nhân vật bị đẩy lên 1 ô thay vì đứng trên đáy ao (ảnh `~/Documents/User/khang/Game/art-test/shots/user0081_under_fall.webp`, ngoài repo; vòng dấu click nằm ở chân cột thác).

Brief gồm 2 giai đoạn:
1. Viết test tái hiện, chạy trên code cũ để xác nhận nguyên nhân.
2. Sửa theo 2 nguyên nhân Planner đã tìm.

## Bối cảnh cần đọc
- **Giả thuyết cũ đã loại** [repo]: `PlayerRules.InWater` (`client/Assets/Game/Core/PlayerRules.cs:68–72`) chỉ dùng để giảm tốc (`PlayerCharacter.cs:404`) và reset theo dõi rơi (`:280`). Không có code nổi hay bơi nào. Nước không có collider: `BlockMesher.IsSolidMaterial` loại Water (`client/Assets/Game/World/BlockMesher.cs:1641`).
- **Nguyên nhân Planner tìm được** [repo, chưa chạy]:
  - **N1 — tia click xuyên qua nước.** `PlayerCharacter.PointOnWorld` (`client/Assets/Game/Client/PlayerCharacter.cs:118–136`) chỉ dùng `Physics.Raycast`, mà nước không có collider. Click vào cột thác thì tia đi xuyên qua cột, trúng mặt tây của ô cát viền ao phía sau, tại (cx+14, ~g+0,6). Đích đi khi đó là x = cx+14, nằm trên viền cát. Nhân vật tự nhảy bậc lên viền cát.
  - **N2 — tự nhảy bậc khi đích ở gần.** Nhánh click (`PlayerCharacter.cs:443`):
    ```csharp
    autoJump = Controller.isGrounded && PlayerMotion.ShouldStepUp(terrain.World, f.x, f.y, f.z, dx / dist, dz / dist);
    ```
    `ShouldStepUp` (`client/Assets/Game/Core/PlayerMotion.cs:64–75`) nhìn trước `StepLookAhead` = 0,7 ô (`:38`) mà không xét đích. Đích ở giữa ô chân thác (cx+13,5) thì khi chân tới x ≥ cx+13,3, điểm nhìn trước rơi vào ô cát (cx+14, g). Ô đó đặc, 2 ô trên trống, nên nhân vật nhảy dù đích ngay trước mặt (`ArriveDistance` 0,05, `:36`).
- **Hình học thác demo** (`client/Assets/Game/Core/Sim/SimDemo.cs:75–106`), với `cx = plain.X0 + plain.Size / 2`, `cz = plain.Z0 + plain.Size / 2`, `g = plain.Ground`:
  - ao: nước ở y = g, x cx+10..cx+13, z cz−12..cz−9. Viền cát ở y = g quanh ao (gồm x = cx+14). Đáy ao: mặt trên = g.
  - khối đất: x cx+15..18, y g+1..g+5. Ô đất nhô (cx+14, g+4).
  - cột thác: (cx+13, g+1..g+4, cz−11), xem test `WaterFallPoursIntoPond` ở `client/Assets/Game/Tests/Client/WaterFlowPlayTests.cs:78–92`.
  - Chân người trên cỏ: y = g+1. Trong ao: y = g.
- **Mẫu dựng PlayMode có SimView + nhân vật:** `client/Assets/Game/Tests/Client/PlayerFarmPlayTests.cs:22–87`. Lưu ý:
  - mẫu đó đặt `sv.habitat = false`. Brief này **giữ mặc định `true`** để có ao + thác;
  - không cần `TerrainEditInput`.
- **Chờ nước ổn định:** `WaitSettled` ở `WaterFlowPlayTests.cs:61–70`.
- **API nhân vật dùng trong test:**
  - `player.Place(Vector3 feetWorld, float yawDeg)` (`PlayerCharacter.cs:251`);
  - `player.Ready`, `player.FeetLocal`, `player.MoveTo(Vector3 targetLocal)` (`:106`), `player.ClickMoveRay(Ray ray, bool mark)` (`:139`);
  - `player.ClickTargetLocal`, `player.ClickMove` (`ClickMoveState.Walking/Arrived/Blocked`), `player.VerticalSpeed`, `player.Controller.isGrounded`, `player.InWater`.
- **Ô:** `Cell.MakeFlowing(ushort cell, int level, bool falling)` (`client/Assets/Game/Core/Cell.cs:83`); `world.Size.InBounds(x, y, z)`; `world.GetMaterial(x, y, z)`.
- Test bước nhảy đang có: `PlayerMotionTests.ShouldStepUp` (`client/Assets/Game/Tests/Core/PlayerMotionTests.cs:47–61`), `ClickMovePlayTests.ClimbsOneHighStep` (`client/Assets/Game/Tests/Client/ClickMovePlayTests.cs:186–203`).
- Không dùng API Unity mới. Chỉ dùng `Ray`, `Transform.InverseTransformPoint/InverseTransformDirection/TransformPoint/TransformDirection`, `Physics.Raycast`, đều đã có trong file. `Game.Core` là C# thuần.

## Quyết định đã chốt (không được đổi)
1. **Sửa N1:** điểm click = chỗ tia **đi vào ô nước đầu tiên**, nếu ô đó nằm trước điểm trúng collider (hoặc trong `ClickRayDistance` khi không trúng collider). Áp cho mọi ô nước: ao, biển, ô chảy, ô rơi.
   - Pháp tuyến trả về = pháp tuyến mặt ô mà tia đi vào.
   - Gốc tia nằm trong nước: điểm = gốc tia, pháp tuyến = lên.
2. **Sửa N2:** không tự nhảy bậc khi đồng thời:
   - khoảng cách ngang tới đích < `StepLookAhead`;
   - ô chứa đích ở tầng chân `(floor(tx), floor(feetY + 0.05), floor(tz))` **không** đặc.

   Ngoài 2 điều kiện đó: giữ luật cũ.
3. Hàm dò ô nước nằm trong `Game.Core` (C# thuần, test EditMode), đi từng ô theo Amanatides–Woo (bước 3).
4. Không đổi: `PlayerRules.InWater`, `HeadUnderwater`, tốc độ trong nước, `TerrainEditInput.Pick` (đào/đặt vẫn theo collider), nhánh tự đi theo đường ô (`autoJump = t.Y > …`, `:426`).
5. **Cổng giai đoạn 1 → 2:** chạy test PlayMode mới trên code cũ.
   - Chỉ sang giai đoạn 2 khi `UnderFallClickThroughColumn` **và** `UnderFallClickMoveNoStepUp` fail.
   - Ngược lại → dừng, `cần Opus`. Trường hợp: một trong hai test đạt, hoặc `UnderFallStandStill` fail (nghĩa là còn nguyên nhân khác). Ghi toàn bộ dòng log `UNDERFALL` vào Vướng mắc.

## Phạm vi
- Được sửa/tạo:
  - `client/Assets/Game/Core/WaterRay.cs` (mới) + `.meta` do Unity tạo;
  - `client/Assets/Game/Core/PlayerMotion.cs`;
  - `client/Assets/Game/Client/PlayerCharacter.cs` (chỉ `PointOnWorld` và dòng `:443`);
  - `client/Assets/Game/Tests/Core/WaterRayTests.cs` (mới);
  - `client/Assets/Game/Tests/Core/PlayerMotionTests.cs` (thêm test);
  - `client/Assets/Game/Tests/Client/UnderFallPlayTests.cs` (mới).
- Không được sửa: mọi test cũ khác, `PlayerRules.cs`, `WorldEditor.cs`, `BlockMesher.cs`, `SimDemo.cs`, `TerrainEditInput.cs`.

## Các bước

### Giai đoạn 1 — tái hiện (code game chưa đổi)
1. **Ghi base commit** (`git rev-parse HEAD`) và giờ bắt đầu vào Kết quả.
2. **Tạo `client/Assets/Game/Tests/Client/UnderFallPlayTests.cs`.**
   - Namespace `Game.Client.Tests`, lớp `UnderFallPlayTests`, chú thích `/// <summary>World 5j lỗi 4 (brief 0086): đứng / click đi tới dưới chân thác demo không bị đẩy lên 1 ô.</summary>`.
   - **SetUp:** chép `PlayerFarmPlayTests.SetUp` từ đầu tới `Assert.IsNotNull(sv.Sim);` (`:25–46`), với 2 thay đổi:
     - bỏ dòng `sv.habitat = false;`;
     - không thêm `TerrainEditInput`.

     Sau đó `yield return WaitSettled();`. Chép `WaitSettled` từ `WaterFlowPlayTests.cs:61–70`.
   - **TearDown:** như `PlayerFarmPlayTests.cs:80–87`.
   - **Biến:**
     - `cx = plain.X0 + plain.Size / 2`, `cz = plain.Z0 + plain.Size / 2`, `g = plain.Ground`, với `plain = sv.Plain.Value`;
     - ô chân thác `(cx+13, g, cz−11)`.
   - **Helper `IEnumerator PlaceAt(Vector3 feetLocal)`:**
     - `player.Place(view.transform.TransformPoint(feetLocal), 0f)`;
     - chờ `player.Ready` tối đa 600 frame;
     - thêm 10 frame;
     - `Assert.IsTrue(player.Ready)`.
   - **Helper `IEnumerator Track(float seconds, string tag)`:**
     - mỗi frame: ghi `maxY = Max(maxY, FeetLocal.y)`;
     - lần đầu `FeetLocal.y > g + 0.3f`, `Debug.Log` **một** dòng bắt đầu bằng `UNDERFALL <tag>`. Dòng này gồm: `FeetLocal` (F2), `VerticalSpeed`, `Controller.isGrounded`, `InWater`, `ClickMove`, `ClickTargetLocal` (F2), vật liệu các ô `(floor x, floor(y−0.05), floor z)` và `(floor x, floor y, floor z)`. Thêm tên collider + `y` local của điểm trúng khi `Physics.Raycast` từ chân + 0,5 lên, hướng xuống 4 m;
     - cuối: lưu `maxY` vào trường `lastMaxY`, `Debug.Log($"UNDERFALL {tag} maxY={lastMaxY:F2} target={player.ClickTargetLocal:F2} state={player.ClickMove}")`.
   - **3 test** (`[UnityTest]`):
     - `UnderFallStandStill`:
       - `PlaceAt((cx+13.5, g, cz−10.5))`;
       - `Track(3f, "stand")`;
       - `Assert.Less(lastMaxY, g + 0.3f)`.
     - `UnderFallClickMoveNoStepUp`:
       - `PlaceAt((cx+11.5, g, cz−10.5))`;
       - `player.MoveTo(new Vector3(cx + 13.5f, 0f, cz - 10.5f))`;
       - `Track(4f, "move")`;
       - `Assert.Less(lastMaxY, g + 0.3f)`, `Assert.AreEqual(ClickMoveState.Arrived, player.ClickMove)`.
     - `UnderFallClickThroughColumn`:
       - `PlaceAt((cx+11.5, g, cz−10.5))`;
       - tia local: gốc `o = (cx+8.5, g+4, cz−10.5)`, đích `(cx+13.5, g+0.9, cz−10.5)`;
       - đổi sang world: `new Ray(view.transform.TransformPoint(o), view.transform.TransformDirection(đích − o).normalized)`;
       - `Assert.IsTrue(player.ClickMoveRay(ray, false))`;
       - `Assert.That(player.ClickTargetLocal.x, Is.GreaterThanOrEqualTo(cx + 13 - 0.01f).And.LessThan(cx + 14f))`;
       - `Track(4f, "ray")`;
       - `Assert.Less(lastMaxY, g + 0.3f)`.
3. **Chạy 3 test trên code cũ** (MCP `run_tests` PlayMode, lọc `UnderFallPlayTests`). Chép từng kết quả và mọi dòng `UNDERFALL` (từ `read_console`) vào Kết quả test, mục "Trước sửa". Áp **QĐ 5**: đủ điều kiện mới làm tiếp, không thì dừng (`cần Opus`, vẫn commit file test + brief).

### Giai đoạn 2 — sửa
4. **Tạo `client/Assets/Game/Core/WaterRay.cs`:**
   ```csharp
   using System;

   namespace Game.Core
   {
       /// <summary>Ô nước đầu tiên mà tia đi vào (brief 0086, task 5j lỗi 4): đi từng ô theo Amanatides–Woo, toạ độ ô (terrain). Chỉ tìm nước — chặn bởi khối đặc là việc của người gọi (giới hạn maxT).</summary>
       public static class WaterRay
       {
           /// <summary>
           /// Tia o + t·d (0 ≤ t ≤ maxT). Trúng ô Water (mọi loại: nguồn, chảy, rơi) → true, t = lúc đi vào ô, (nx, ny, nz) = pháp tuyến mặt đi vào;
           /// gốc trong ô nước → t = 0, pháp tuyến (0, 1, 0). Ô ngoài world bỏ qua. d gần 0 (|d|² &lt; 1e-12) hoặc không trúng → false, t = 0, pháp tuyến 0.
           /// </summary>
           public static bool FirstWater(WorldData world, float ox, float oy, float oz, float dx, float dy, float dz, float maxT,
               out float t, out int nx, out int ny, out int nz)
       }
   }
   ```
   Thuật toán:
   - `world == null` → `ArgumentNullException`.
   - Ô hiện tại `x = floor(ox)`, `y = floor(oy)`, `z = floor(oz)` (`Math.Floor`).
   - Mỗi trục:
     - `step` = dấu của d (−1, 0, 1);
     - `tDelta` = `step != 0 ? 1/|d| : +∞`;
     - `tMax` = `step > 0 ? (x + 1 − o)/d : step < 0 ? (o − x)/(−d) : +∞`.
   - Lặp:
     - nếu `InBounds(x, y, z)` và vật liệu là Water → trả true với `t` hiện tại và pháp tuyến:
       - chưa bước lần nào: (0, 1, 0);
       - vừa bước trục X: (−stepX, 0, 0); tương tự Y, Z.
     - chọn trục có `tMax` nhỏ nhất; bằng nhau thì ưu tiên X, rồi Y, rồi Z;
     - `t = tMax` của trục đó; `t > maxT` → trả false;
     - bước ô theo trục đó, `tMax += tDelta`.
   - Chặn vòng: tối đa `3 * ((int)Math.Ceiling(maxT) + 2)` lần bước. Vượt → false.
   - Xong khi: biên dịch không lỗi.
5. **`PlayerMotion.cs`:** sau `ShouldStepUp`, thêm hàm:
   ```csharp
   /// <summary>Tự nhảy bậc khi đi tới đích (tx, tz): như ShouldStepUp theo hướng tới đích, nhưng không nhảy khi đích gần hơn StepLookAhead và ô chứa đích ở tầng chân không đặc (brief 0086). Đích trùng chân → false.</summary>
   public static bool ShouldStepUpToward(WorldData world, float feetX, float feetY, float feetZ, float targetX, float targetZ)
   ```
   Thân hàm:
   - `world == null` → `ArgumentNullException`;
   - `dx = targetX − feetX`, `dz = targetZ − feetZ`, `dist = sqrt(dx² + dz²)`;
   - `dist < 1e-4f` → false;
   - nếu `dist < StepLookAhead` và `!WorldEditor.IsSolidAt(world, floor(targetX), floor(feetY + 0.05f), floor(targetZ))` → false;
   - ngược lại → `ShouldStepUp(world, feetX, feetY, feetZ, dx / dist, dz / dist)`.
6. **`PlayerCharacter.cs:443`:** thay bằng
   ```csharp
   autoJump = Controller.isGrounded && PlayerMotion.ShouldStepUpToward(terrain.World, f.x, f.y, f.z, ClickTargetLocal.x, ClickTargetLocal.z);
   ```
7. **`PlayerCharacter.PointOnWorld`:** viết lại theo QĐ 1:
   - `bool hit = Physics.Raycast(…)`, giữ nguyên tham số; `float limit = hit ? hitInfo.distance : ClickRayDistance`;
   - nếu `terrain != null && terrain.World != null`:
     - đổi tia sang local: `o = terrain.transform.InverseTransformPoint(ray.origin)`, `d = terrain.transform.InverseTransformDirection(ray.direction)`;
     - `WaterRay.FirstWater(terrain.World, o…, d…, limit, out t, out nx, out ny, out nz)` true → `pointWorld = terrain.transform.TransformPoint(o + d * t)`, `normalWorld = terrain.transform.TransformDirection(new Vector3(nx, ny, nz))`, `return true`.
   - còn lại: như cũ (trúng collider → điểm trúng; không thì mặt phẳng ngang qua chân; không nữa → false);
   - summary đổi thành: `/// <summary>Điểm world dưới tia: ô nước đầu tiên tia đi vào trước collider (brief 0086), không thì điểm trúng collider, không thì mặt phẳng ngang qua chân; còn lại false.</summary>`.
8. **`client/Assets/Game/Tests/Core/WaterRayTests.cs`** (namespace `Game.Core.Tests`, lớp `WaterRayTests`).
   - World `new WorldData(new WorldSize(16, 16, 16))` toàn Empty.
   - Test:
     - `HitsWaterAlongX`: Water ở (5,3,5) (`Cell.Make(BlockMaterial.Water, Cell.MaxWaterLevel)`). Tia (0.5, 3.5, 5.5), d (1,0,0), maxT 100 → true, t = 4.5 (±1e-5), n = (−1,0,0).
     - `MaxTStops`: như trên, maxT 4 → false.
     - `HitsFromAbove`: tia (5.5, 10.5, 5.5), d (0,−1,0) → t = 6 (±1e-5), n = (0,1,0).
     - `OriginInsideWater`: gốc (5.5, 3.5, 5.5) → t = 0, n = (0,1,0).
     - `FallingWaterCounts`: ô (5,3,5) = `Cell.MakeFlowing(Cell.Make(BlockMaterial.Water), 5, true)` → như `HitsWaterAlongX`.
     - `SolidDoesNotStop`: thêm Stone ở (3,3,5) → vẫn t = 4.5.
     - `Diagonal`: Water ở (3,1,0); gốc (0.5, 0.5, 0.5); d = (1, 0.5, 0) chuẩn hoá → true, t = 2.5·√1.25 (±1e-4), n = (−1,0,0).
     - `ZeroDirAndNull`: d (0,0,0) → false; `world` null → `Assert.Throws<ArgumentNullException>`.
9. **`PlayerMotionTests.cs`:** thêm `[Test] ShouldStepUpTowardSkipsNearOpenTarget`, dùng `Flat()` + `Solid(w, 5, 1, 6)`:
   - đích (5.5, 5.8), chân (5.5, 1, 5.5) → false (đích gần 0,3 ô, ô (5,1,5) trống);
   - đích (5.5, 6.5) → true (đích trong ô đặc);
   - đích (5.5, 9.5) → true (xa);
   - đích trùng chân → false;
   - `null` world → `ArgumentNullException`.

   `ShouldStepUp` cũ giữ nguyên.
10. **Chạy lại** `UnderFallPlayTests` → cả 3 đạt. Ghi vào "Sau sửa". Rồi chạy các lệnh ở "Lệnh executor chạy".

## Tiêu chí xong
- Trước sửa: `UnderFallClickThroughColumn` và `UnderFallClickMoveNoStepUp` fail, `UnderFallStandStill` đạt. Có log `UNDERFALL`.
- Sau sửa: 3 test `UnderFallPlayTests` đạt; `WaterRayTests` (8 test) và `ShouldStepUpTowardSkipsNearOpenTarget` đạt.
- EditMode toàn bộ đạt. PlayMode: `ClickMovePlayTests`, `ControlModePlayTests`, `WaterFlowPlayTests`, `DropPlayTests` đạt.
- Console 0 lỗi. `git status` chỉ có file trong Phạm vi (sau bước dọn).

## Test sau khi xong

### Phần cần test
- Đứng yên dưới chân thác · `UnderFallStandStill` · chân không lên quá g + 0,3 trong 3 s (trước và sau sửa đều đạt).
- Click đi tới giữa ô chân thác (N2) · `UnderFallClickMoveNoStepUp` · trước sửa fail (nhảy); sau sửa đạt, `Arrived`.
- Click xuyên cột thác (N1) · `UnderFallClickThroughColumn` · trước sửa fail (đích x ≥ cx+14); sau sửa đích trong ô chân thác, không bị đẩy lên.
- `WaterRay.FirstWater` (bước 4) · `WaterRayTests` 8 test · đúng t và pháp tuyến như bước 8.
- `ShouldStepUpToward` (bước 5) · `PlayerMotionTests.ShouldStepUpTowardSkipsNearOpenTarget` · như bước 9.
- Regression nhảy bậc khi click · `ClickMovePlayTests` (cả lớp, nhất là `ClimbsOneHighStep`, `TwoHighWallStops`, `ClickHeightFlattensAndMarksPoint`) · đạt.
- Regression `ShouldStepUp` cũ · `PlayerMotionTests.ShouldStepUp` · đạt.
- Regression click/đào/đặt · `ControlModePlayTests`, `DropPlayTests` · đạt.
- Regression nước · `WaterFlowPlayTests`, EditMode `WaterFlowTests`, `SeaFloodTests`, `GoldenHashTests` · đạt.

### Lệnh executor chạy
1. MCP `run_tests` PlayMode lọc `UnderFallPlayTests`: trước sửa (bước 3) và sau sửa (bước 10).
2. MCP `run_tests` EditMode toàn bộ → dòng tổng kết.
3. MCP `run_tests` PlayMode lọc `ClickMovePlayTests`, `ControlModePlayTests`, `WaterFlowPlayTests`, `DropPlayTests`, `UnderFallPlayTests` → tổng kết từng lớp.
4. MCP `read_console` (Error) → 0 lỗi.
5. Dọn: `git checkout -- client/ProjectSettings/EditorSettings.asset`, rồi `git status --short`.
6. Commit (code + test + `.meta` + brief): `brief 0086: click through water targets water cell; no step-up toward near open target`.

### User tự test
Gộp cuối 5j (user chơi thử một lượt). SimView, Play:
1. Click vào cột thác demo (chỗ nước đổ xuống ao). Nhân vật đi xuống ao, đứng dưới thác, không nhảy lên viền cát.
2. Click lên mặt ao (chỗ không có thác). Nhân vật đi tới đúng chỗ thấy trên mặt nước.
3. Click lên bậc cao 1 ô trên cỏ: vẫn tự nhảy lên như trước.

## Dừng và chuyển lại Opus khi
- Cổng QĐ 5 không qua.
- Sau sửa, test `UnderFall*` vẫn fail sau 2 lần sửa.
- Một test regression ở mục trên fail.
- Brief mơ hồ hoặc mâu thuẫn với code thật; phải sửa file ngoài Phạm vi.

---

## Kết quả

## Kết quả test

## Vướng mắc

---

## Review
