# Brief 0087 — World 5j (lỗi 5): vật rơi nằm yên trên mặt đất, lệch ngẫu nhiên trong ô

> Trạng thái: đã duyệt
> Review: đầy đủ + user xem (gộp cuối 5j)
> Model thực thi: Sonnet · medium
> Cập nhật: 2026-10-29 20:45

## Mục tiêu
Lỗi 5 của task [5j](../tasks/m1-world-5j-sua-loi-choi-thu.md), theo user (2026-10-29): vật rơi khi phá khối phải nằm yên trên mặt đất, không xoay tròn, và nằm ở vị trí ngẫu nhiên gần khối bị phá thay vì đúng tâm ô.

Cách làm:
- bỏ xoay;
- mỗi chồng có độ lệch ngang + góc quay cố định, tính tất định từ Id chồng;
- hộp chọn bằng tia đi theo cùng độ lệch;
- bán kính lệch là tham số JSON [số tạm].

## Bối cảnh cần đọc
- **`client/Assets/Game/Client/DropView.cs`:**
  - `SpinDegreesPerSecond = 45f` (`:19`);
  - `public static Vector3 LocalCenter(DropStack s) => new Vector3(s.X + 0.5f, s.Y + SizeOf(s.Item).y * 0.5f, s.Z + 0.5f);` (`:39`). Đáy hình đã ở mặt ô, tức là trên mặt khối bên dưới;
  - vẽ ở `:94–95`:
    ```csharp
    foreach (var s in editor.Drops.Stacks)
        AddInstance(s.Item, toWorld * Matrix4x4.TRS(LocalCenter(s), Quaternion.Euler(0f, now * SpinDegreesPerSecond + s.Id * 37f, 0f), SizeOf(s.Item)));
    ```
  - vật đang bay về nhân vật (`:97–104`) bay từ `LocalCenter(s)`, nên tự theo độ lệch mới. Không đổi gì thêm.
- **Ô của chồng** (`client/Assets/Game/Core/DropField.cs`):
  - `DropField.Add` hạ chồng xuống ô ngay trên khối đặc (`Settle`, `:222–228`). Chồng cùng loại trong cùng ô được gộp (`:112–118`);
  - `DropStack` có `Id`, `X`, `Y`, `Z` (`:51–63`).

  **Brief này không đổi `DropField`.** Ô, tầm nhặt, gộp chồng, hash mô phỏng giữ nguyên.
- **Chọn bằng tia:** `client/Assets/Game/Core/DropPick.cs:15–33`.
  - Hộp cạnh `BoxSize` = 0,5, giữa ô theo x/z (`:26–27`):
    ```csharp
    s.X + 0.5f - h, s.Y, s.Z + 0.5f - h, s.X + 0.5f + h, s.Y + BoxSize, s.Z + 0.5f + h
    ```
  - Tầm thao tác vẫn xét theo ô (`:25`), giữ nguyên.
- **Bo cạnh khối:** `BlockMesher.BevelRadius` = 0,125 (`client/Assets/Game/World/BlockMesher.cs:29`). Mặt trên phẳng trong khoảng 0,375 quanh tâm ô.
  - Hình vật rơi rộng 0,3 × 0,3 (khối 0,3; túi hạt 0,3 × 0,4 × 0,3 — `DropView.cs:16–17`). Khi xoay, nửa đường chéo đáy = 0,212.
  - Vì vậy bán kính lệch ≤ 0,375 − 0,212 ≈ 0,16 thì vật không chìa ra phần bo, không trông lơ lửng. Từ đó chọn **0,15 [số tạm]**, giới hạn hợp lệ 0..0,16.
- **Cấu hình người chơi (mẫu brief 0083 thêm `cameraHideBodyDistance`, không tăng `configVersion`):**
  - `client/Assets/Game/Core/Sim/SimConfig.cs`: `PlayerConfig` có `public float cameraHideBodyDistance = 1f;` (`:170`); kiểm ở `Validate` (`:427–428`):
    ```csharp
    if (!(player.cameraHideBodyDistance >= 0.3f && player.cameraHideBodyDistance <= 3f))
        throw new ArgumentException("player.cameraHideBodyDistance must be in [0.3, 3].");
    ```
  - `client/Assets/Game/Core/PlayerMotion.cs`:
    - property dạng `public static float CameraHideBodyDistance { get; private set; } = 1f;` (`:34`);
    - `Configure` gán `CameraHideBodyDistance = p.cameraHideBodyDistance;` (`:61`).
  - `design/data/sim_m1.json:39` và `client/Assets/Game/Client/Resources/Config/sim_m1.json`: hiện giống hệt nhau [run `diff`]. Dòng `player` kết thúc bằng `"cameraHideBodyDistance": 1`.
  - `client/Assets/Game/Tests/Core/Sim/SimConfigTests.cs`:
    - `ConfigParsesAndValidates` khẳng định `cameraHideBodyDistance` (`:68`);
    - `ConfigMatchesCodeDefaults` (`:73–78`): file phải khớp mặc định code;
    - `ValidateRejectsBadPlayerRules` có dòng `Bad(c => c.player.cameraHideBodyDistance = 0.2f, "cameraHideBodyDistance 0.2");` (`:114`).
- **Test đang có:**
  - `client/Assets/Game/Tests/Core/DropPickTests.cs`: các test giả định hộp ở giữa ô. Ví dụ `MissBesideBox`, z = 5,8 (`:55–57`), sẽ hỏng nếu có độ lệch.
  - `client/Assets/Game/Tests/Client/DropPlayTests.cs`:
    - `DropViewDrawsGroupsWithInstancedMaterials` (`:180–203`) khẳng định `LocalCenter(d).x == A.x + 2.5`, `.z == A.z + 0.5` (`:196–199`);
    - `ClickPicksOnlyInReachAndFarDoesNotBlockDig` (`:130–150`) bắn tia thẳng xuống giữa ô. Hộp 0,5 lệch ≤ 0,15 vẫn chứa tâm ô, nên vẫn đạt.
- Không dùng API Unity mới. Chỉ dùng `Quaternion.Euler`, `Matrix4x4.TRS`, đều đã có trong file. `Game.Core` là C# thuần.

## Quyết định đã chốt (không được đổi)
1. **Vật rơi vẫn thuộc ô cũ.** Độ lệch chỉ dùng cho hiển thị + hộp chọn. Không đổi `DropField`, tầm nhặt, gộp chồng.
2. **Độ lệch tất định theo `DropStack.Id`** bằng hàm băm số nguyên (bước 1). Không dùng `UnityEngine.Random` hay `System.Random`. Không ảnh hưởng mô phỏng, `GoldenHashTests` không đổi.
3. **Nằm yên:**
   - góc quay quanh trục Y = góc băm cố định (0..360°), không theo thời gian;
   - đáy hình ở mặt ô (`s.Y`), như hiện nay;
   - bỏ hằng `SpinDegreesPerSecond`.
4. **Tham số:**
   - `player.dropScatterRadius` = 0,15 [số tạm], chưa chốt; hợp lệ [0, 0,16];
   - `PlayerMotion.DropScatterRadius` mặc định 0,15;
   - không tăng `configVersion`.
5. **Phân bố:** đều trong hình tròn bán kính r quanh tâm ô (`r·√u1`, góc `2π·u2`).

## Phạm vi
- Được sửa/tạo:
  - `client/Assets/Game/Core/DropScatter.cs` (mới) + `.meta` do Unity tạo;
  - `client/Assets/Game/Core/DropPick.cs`;
  - `client/Assets/Game/Core/PlayerMotion.cs`;
  - `client/Assets/Game/Core/Sim/SimConfig.cs`;
  - `design/data/sim_m1.json`, `client/Assets/Game/Client/Resources/Config/sim_m1.json`;
  - `client/Assets/Game/Client/DropView.cs`;
  - `client/Assets/Game/Tests/Core/DropScatterTests.cs` (mới);
  - `client/Assets/Game/Tests/Core/DropPickTests.cs` (SetUp/TearDown + 1 test mới);
  - `client/Assets/Game/Tests/Core/Sim/SimConfigTests.cs`;
  - `client/Assets/Game/Tests/Client/DropPlayTests.cs` (chỉ `DropViewDrawsGroupsWithInstancedMaterials`).
- Không được sửa: `DropField.cs`, `WorldEditor.cs`, `TerrainEditInput.cs`, `PlayerCharacter.cs`, mọi test khác.

## Các bước
1. **Tạo `client/Assets/Game/Core/DropScatter.cs`:**
   ```csharp
   using System;

   namespace Game.Core
   {
       /// <summary>Độ lệch hiển thị của chồng vật rơi trong ô (brief 0087, task 5j lỗi 5): tất định theo Id chồng; chỉ dùng cho vẽ và chọn bằng tia, không đổi ô của chồng.</summary>
       public static class DropScatter
       {
           /// <summary>Lệch ngang (dx, dz) đều trong hình tròn bán kính radius quanh tâm ô, góc quay yawDeg trong [0, 360). radius &lt; 0 → ArgumentOutOfRangeException.</summary>
           public static void Offset(int stackId, float radius, out float dx, out float dz, out float yawDeg)

           /// <summary>Băm 32 bit (lowbias32).</summary>
           public static uint Hash(uint x)
       }
   }
   ```
   - `Hash`: `x ^= x >> 16; x *= 0x7feb352dU; x ^= x >> 15; x *= 0x846ca68bU; x ^= x >> 16; return x;`, trong khối `unchecked`.
   - `Offset`:
     - `uint b = unchecked((uint)stackId * 3u)`;
     - `u1 = Hash(b) / 4294967296.0`, `u2 = Hash(b + 1u) / 4294967296.0`, `u3 = Hash(b + 2u) / 4294967296.0` (kiểu `double`);
     - `r = radius * Math.Sqrt(u1)`, `a = 2π·u2`;
     - `dx = (float)(r * Math.Cos(a))`, `dz = (float)(r * Math.Sin(a))`, `yawDeg = (float)(360.0 * u3)`;
     - nếu `yawDeg >= 360f` thì `yawDeg = 0f`.
   - Xong khi: biên dịch không lỗi.
2. **Tham số `dropScatterRadius`.**
   - `SimConfig.cs`:
     - ngay sau `cameraHideBodyDistance` (`:170`), thêm `public float dropScatterRadius = 0.15f;`;
     - trong `Validate`, ngay sau khối kiểm `cameraHideBodyDistance`, thêm:
       ```csharp
       if (!(player.dropScatterRadius >= 0f && player.dropScatterRadius <= 0.16f))
           throw new ArgumentException("player.dropScatterRadius must be in [0, 0.16].");
       ```
   - `PlayerMotion.cs`:
     - sau `CameraHideBodyDistance` thêm `/// <summary>Vật rơi lệch ngẫu nhiên trong ô tối đa chừng này (ô) quanh tâm (brief 0087, số tạm).</summary>` + `public static float DropScatterRadius { get; private set; } = 0.15f;`;
     - trong `Configure` thêm `DropScatterRadius = p.dropScatterRadius;`.
   - Hai file `sim_m1.json`: ở dòng `player`, sau `"cameraHideBodyDistance": 1` thêm `, "dropScatterRadius": 0.15`. Sau khi sửa, `diff` hai file rỗng.
3. **`DropView.cs`:**
   - Bỏ dòng `public const float SpinDegreesPerSecond = 45f;`.
   - `LocalCenter`:
     ```csharp
     /// <summary>Tâm hình của chồng (toạ độ terrain): tâm ô + độ lệch DropScatter theo x/z, đáy ở mặt ô.</summary>
     public static Vector3 LocalCenter(DropStack s)
     {
         DropScatter.Offset(s.Id, PlayerMotion.DropScatterRadius, out float dx, out float dz, out _);
         return new Vector3(s.X + 0.5f + dx, s.Y + SizeOf(s.Item).y * 0.5f, s.Z + 0.5f + dz);
     }
     ```
   - Thêm:
     ```csharp
     /// <summary>Góc nằm yên của chồng (quanh trục Y, cố định theo Id — brief 0087).</summary>
     public static Quaternion RestRotation(DropStack s)
     {
         DropScatter.Offset(s.Id, PlayerMotion.DropScatterRadius, out _, out _, out float yaw);
         return Quaternion.Euler(0f, yaw, 0f);
     }
     ```
   - Dòng vẽ `:95`: thay `Quaternion.Euler(0f, now * SpinDegreesPerSecond + s.Id * 37f, 0f)` bằng `RestRotation(s)`.
   - Summary lớp (`:9`): thay `, xoay chậm;` bằng `, nằm yên trên mặt ô, lệch ngẫu nhiên trong ô và quay một góc cố định theo Id chồng (DropScatter, brief 0087);`.
   - Xong khi: `grep -n "SpinDegreesPerSecond" -r client/Assets/Game` không còn kết quả.
4. **`DropPick.cs`:**
   - Trong vòng lặp, trước `RayBox`: `DropScatter.Offset(s.Id, PlayerMotion.DropScatterRadius, out float ox2, out float oz2, out _);`.
   - Hộp đổi thành `s.X + 0.5f + ox2 ± h`, `s.Z + 0.5f + oz2 ± h`. `y` giữ nguyên.
   - Summary lớp: thay `giữa ô theo x/z` bằng `giữa ô + độ lệch DropScatter theo x/z (brief 0087)`.
5. **`DropPickTests.cs`:**
   - Đầu `SetUp`, thêm `PlayerMotion.Configure(new Game.Core.Sim.PlayerConfig { dropScatterRadius = 0f });`, để các test cũ giữ hộp giữa ô.
   - Thêm `[TearDown] public void TearDown() => PlayerMotion.Configure(new Game.Core.Sim.PlayerConfig());`.
   - Thêm test `BoxFollowsScatter`:
     - `PlayerMotion.Configure(new Game.Core.Sim.PlayerConfig());` (bán kính 0,15);
     - `DropScatter.Offset(dirt.Id, 0.15f, out float dx, out float dz, out _)`;
     - tia từ `(5.5f + dx, 5f, 5.5f + dz)` hướng (0,−1,0), chân (5.5, 1, 5.5) → trả `dirt`;
     - tia từ `(5.5f + dx + 0.3f * Math.Sign(dx == 0 ? 1 : dx), 5f, 5.5f + dz)` (ngoài hộp đã lệch, phía xa tâm) → null.
6. **`DropScatterTests.cs`** (namespace `Game.Core.Tests`):
   - `Deterministic`: id 7, r 0,15, gọi 2 lần → cùng `dx`, `dz`, `yaw`.
   - `WithinRadiusAndSpread`: id 1..1000, r 0,15.
     - Mọi `√(dx²+dz²) ≤ 0.15f + 1e-6f`; `0 ≤ yaw < 360`.
     - Số id có bán kính > 0,075 nằm trong [650, 850] (kỳ vọng 75 %).
     - Trung bình `dx`, `dz` có trị tuyệt đối < 0,01.
     - `max(yaw) − min(yaw) > 300`.
   - `ZeroRadius`: r 0 → `dx == 0`, `dz == 0`.
   - `NegativeRadiusThrows`: r −0,1 → `ArgumentOutOfRangeException`.
7. **`SimConfigTests.cs`:**
   - `ConfigParsesAndValidates`: thêm `Assert.AreEqual(0.15f, cfg.player.dropScatterRadius, 1e-6f);` cạnh dòng `:68`;
   - `ValidateRejectsBadPlayerRules`: thêm `Bad(c => c.player.dropScatterRadius = -0.01f, "dropScatterRadius -0.01");` và `Bad(c => c.player.dropScatterRadius = 0.2f, "dropScatterRadius 0.2");`.
8. **`DropPlayTests.DropViewDrawsGroupsWithInstancedMaterials`:**
   - Thay 3 dòng khẳng định `lc.x`, `lc.y`, `lc.z` (`:197–199`) bằng:
     ```csharp
     DropScatter.Offset(d.Id, PlayerMotion.DropScatterRadius, out float sdx, out float sdz, out float syaw);
     Assert.AreEqual(A.x + 2.5f + sdx, lc.x, 1e-4f);
     Assert.AreEqual(G + 1.15f, lc.y, 1e-4f);
     Assert.AreEqual(A.z + 0.5f + sdz, lc.z, 1e-4f);
     Assert.LessOrEqual(new Vector2(lc.x - (A.x + 2.5f), lc.z - (A.z + 0.5f)).magnitude, PlayerMotion.DropScatterRadius + 1e-4f);
     var r0 = DropView.RestRotation(d);
     Assert.AreEqual(syaw, r0.eulerAngles.y, 0.01f);
     ```
   - Thêm ngay trước dòng `Assert.AreEqual("Đất × 3", …)`:
     ```csharp
     yield return new WaitForSeconds(0.5f);
     Assert.AreEqual(0f, Quaternion.Angle(r0, DropView.RestRotation(d)), 1e-4f); // nằm yên, không xoay theo thời gian
     Assert.AreEqual(lc, DropView.LocalCenter(d));
     ```
   - Dòng `DropView.LocalCenter(s).y` (`:200`) giữ nguyên.
   - Lưu ý góc Euler: Unity có thể trả `eulerAngles.y` = 360 − ε cho `syaw` ≈ 0. Nếu test fail đúng vì lý do đó (chênh ≈ 360), so `Mathf.DeltaAngle(syaw, r0.eulerAngles.y)` với 0 (±0,01). Ghi lại ở Kết quả.

## Tiêu chí xong
- `DropScatterTests` (4 test), `DropPickTests` (cũ + `BoxFollowsScatter`), `SimConfigTests`, `DropPlayTests` (cả lớp) đạt.
- Trên code cũ, `DropViewDrawsGroupsWithInstancedMaterials` đã sửa fail ở khẳng định nằm yên hoặc độ lệch. Chạy trước khi sửa `DropView.cs`, ghi output fail vào Kết quả test.
- EditMode toàn bộ đạt, gồm `GoldenHashTests` (không đổi) và `DropFieldTests`.
- PlayMode: `DropPlayTests`, `ControlModePlayTests`, `InventoryPlayTests` đạt.
- Console 0 lỗi. `grep SpinDegreesPerSecond` rỗng. `diff` hai file `sim_m1.json` rỗng.
- `git status` chỉ có file trong Phạm vi (sau bước dọn).

## Test sau khi xong

### Phần cần test
- Băm + độ lệch (bước 1) · `DropScatterTests` · như bước 6.
- Tham số JSON (bước 2) · `SimConfigTests.ConfigParsesAndValidates`, `ConfigMatchesCodeDefaults`, `ValidateRejectsBadPlayerRules` · đạt; giá trị 0,15; −0,01 và 0,2 bị từ chối.
- Vẽ nằm yên + lệch (bước 3) · `DropPlayTests.DropViewDrawsGroupsWithInstancedMaterials` (bước 8) · tâm lệch đúng theo `DropScatter`, trong bán kính, đáy ở `G + 1`, góc không đổi sau 0,5 s.
- Hộp chọn theo độ lệch (bước 4) · `DropPickTests.BoxFollowsScatter` · trúng ở tâm đã lệch, trượt ở ngoài.
- Regression chọn bằng tia · `DropPickTests` cũ (bán kính 0), `DropPlayTests.ClickPicksOnlyInReachAndFarDoesNotBlockDig` · đạt.
- Regression ô, gộp chồng, nhặt · `DropFieldTests`, `DropPlayTests` (cả lớp), `ControlModePlayTests`, `InventoryPlayTests` · đạt.
- Regression mô phỏng · `GoldenHashTests` · mã băm không đổi.

### Lệnh executor chạy
1. Ghi base commit (`git rev-parse HEAD`) + giờ bắt đầu.
2. Làm bước 8 trước, chạy `DropPlayTests.DropViewDrawsGroupsWithInstancedMaterials` trên code cũ (MCP `run_tests` PlayMode, lọc theo tên) → ghi fail. Sau đó làm bước 1–7.
3. MCP `run_tests` EditMode toàn bộ → dòng tổng kết.
4. MCP `run_tests` PlayMode lọc `DropPlayTests`, `ControlModePlayTests`, `InventoryPlayTests` → tổng kết từng lớp.
5. MCP `read_console` (Error) → 0 lỗi.
6. `diff design/data/sim_m1.json client/Assets/Game/Client/Resources/Config/sim_m1.json` → rỗng. `grep -rn SpinDegreesPerSecond client/Assets/Game` → rỗng.
7. Dọn: `git checkout -- client/ProjectSettings/EditorSettings.asset`, rồi `git status --short`.
8. Commit (code + test + `.meta` + brief): `brief 0087: drops rest on ground with deterministic in-cell scatter`.

### User tự test
Gộp cuối 5j (user chơi thử một lượt). SimView, Play:
1. Đào vài khối cỏ/đất/đá liền nhau. Mỗi vật rơi nằm yên trên mặt đất (không xoay, không lơ lửng), lệch khỏi tâm ô mỗi cái một kiểu, quay các góc khác nhau.
2. Click nhặt một vật rơi đã lệch: trỏ trúng đúng hình vật đó.
3. Đào khối ngay dưới một vật rơi: vật rơi xuống ô dưới, vẫn nằm yên.
4. Nhận xét độ lệch 0,15 ô có đủ thấy không. Muốn lệch hơn thì cần thêm cách (vd vật rơi sang ô kề) → báo Planner. Hiện tối đa 0,16, vì lớn hơn thì vật chìa ra phần bo cạnh khối.

## Dừng và chuyển lại Opus khi
- Brief mơ hồ hoặc mâu thuẫn với code thật.
- Phải sửa file ngoài Phạm vi.
- Một test regression fail.
- Cùng một lỗi sửa 2 lần vẫn chưa qua.

---

## Kết quả

Base commit: 6c63a5c
Chạy bằng: Sonnet · medium (claude-sonnet-5-5)
Bắt đầu: 2026-10-29 20:18 · Kết thúc: ~20:45 (≈ 0,5 giờ)
Commit: 5706a55 — DropScatter.cs(+meta), DropPick.cs, PlayerMotion.cs, SimConfig.cs, DropView.cs, 2 file sim_m1.json, DropScatterTests(+meta), DropPickTests, SimConfigTests, DropPlayTests.

## Kết quả test
- Test trên code cũ (bước 8 trước): KHÔNG chạy được fail kiểu khẳng định — test mới tham chiếu `DropScatter`/`RestRotation` chưa tồn tại nên trên code cũ không biên dịch (fail ở mức biên dịch). Không chạy PlayMode trên code cũ để tránh lặp reload.
- DropScatterTests (4), DropPickTests (cũ + BoxFollowsScatter), SimConfigTests: đạt (EditMode 680/680, 0 fail, GoldenHashTests + DropFieldTests nằm trong đó).
- PlayMode DropPlayTests + ControlModePlayTests + InventoryPlayTests: 53/53 đạt (gồm DropViewDrawsGroupsWithInstancedMaterials, ClickPicksOnlyInReachAndFarDoesNotBlockDig). Không gặp vấn đề eulerAngles 360.
- read_console Error: 0. `diff` hai sim_m1.json rỗng. `grep SpinDegreesPerSecond` rỗng. Đã `git checkout` EditorSettings.asset; git status chỉ file trong Phạm vi.
- Phần nhìn bằng mắt: chờ user (gộp cuối 5j).

## Vướng mắc

---

## Review
- Chạy bằng Sonnet · medium khớp Model thực thi; ~27 phút.
- Mọi dòng Phần cần test có kết quả: EditMode 680/680 (DropScatter, DropPick, SimConfig, GoldenHash); PlayMode 3 lớp 53/53; hai `sim_m1.json` giống hệt; không còn `SpinDegreesPerSecond` [run trong brief].
- `git show --stat 5706a55`: 13 file, đúng Phạm vi.
- Lệch: test mới không chạy được trên code cũ (không biên dịch vì `DropScatter` chưa có) — chấp nhận, hành vi cũ (vật ở tâm, xoay) thấy trực tiếp trong code bị thay.
- Kết luận: đã duyệt
