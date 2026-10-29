# Brief 0085 — World 5j (lỗi 2): cài luật 0044, ô thấp hơn nguồn gốc thì đầy

> Trạng thái: chờ review
> Review: đầy đủ + user xem (gộp cuối 5j)
> Model thực thi: Sonnet · medium
> Cập nhật: 2026-10-29 19:48

## Mục tiêu
Lỗi 2 của task [5j](../tasks/m1-world-5j-sua-loi-choi-thu.md): đào hố rộng 2 lớp cạnh ao thì nước không lấp đầy. User đã chọn luật [0044](../../design/decisions/0044-ho-thap-hon-mat-nguon-thi-day.md) (2026-10-29). Brief này cài luật đó vào `WorldEditor`, thêm tham số `basinFillRadius` vào cấu hình nước, rồi đổi/thêm test.

## Bối cảnh cần đọc
- **Luật 0044:**
  - Ô nước chảy có `y` nhỏ hơn `y` của **nguồn gốc ở gốc cây** của nó → mức `MaxLevel` (5), rồi phát sóng mức, giống "tăng mức bởi tường" của 0043.
  - Luật chỉ áp khi khoảng cách ngang Chebyshev từ ô tới nguồn gốc ≤ `basinFillRadius` = 16 [số tạm].
  - Lớp có `y` ≥ `y` nguồn: giữ luật 0043 (lan giảm 1 mức/ô). Nước không dâng lên.
- **Code hiện tại — `client/Assets/Game/Core/WorldEditor.cs`:**
  - `int wallBoostMinSolid = 2;` + property `WallBoostMinSolid` có kiểm 1..4 (`:69–81`). Property mới viết theo mẫu này.
  - `struct FlowNode { public int X, Y, Z, Parent; public bool Source; }` (`:83–87`), `const int NoParent = -1;` (`:89`).
  - Bảng `nodes` (`:93`). Nút nguồn có `Source = true` và `Parent = NoParent`.
  - `int LevelOf(int id)` (`:361`).
  - `bool IsAncestor(int ancestor, int id)` (`:458–474`): mẫu đi theo chuỗi `Parent` có chặn vòng (`step <= nodes.Count`, vượt thì `throw new InvalidOperationException("water parent cycle")`).
  - `void WallBoost(int id)` (`:492–511`): nguồn thì bỏ qua. Nếu `level <= 0 || level >= maxLevel` thì bỏ qua. Đếm khối đặc ở 4 ô ngang; đủ `wallBoostMinSolid` thì `SetLevel(id, maxLevel); ActivateId(id); Propagate(id);`.
  - `void Propagate(int start)` (`:513–537`): với ô chảy kề có `LevelOf(n) < level − 1`, làm lần lượt `SetLevel(n, level - 1); ActivateId(n);`. Ô đó không phải cha/tổ tiên thì `Reparent(n, e)`. Cuối cùng `stack.Push(n)`.
  - `CreateChild` (`:539–557`) gọi `WallBoost(id)` ở cuối. Mọi ô mới đều đi qua đây: ô lan, ô rơi, ô tái tạo trong `StepCascade`.
- **Cấu hình:**
  - `client/Assets/Game/Core/WaterConfig.cs`: `configVersion = 3`, các trường, `Validate()`.
  - `design/data/water_m1.json` và bản copy `client/Assets/Game/Client/Resources/Config/water_m1.json` hiện giống hệt nhau [run `diff`].
  - Client nạp cấu hình ở `client/Assets/Game/Client/TerrainChunkView.cs:230–232`: `Editor.MaxLevel = …; Editor.WallBoostMinSolid = waterCfg.wallBoostMinSolid;`.
  - `client/Assets/Game/Tests/Core/WaterConfigTests.cs`:
    - `ConfigParsesAndValidates` khẳng định `configVersion` 3 (`:18`);
    - `ConfigFileMatchesDefaults` so file với giá trị mặc định;
    - `ValidateRejectsBadValues` (`:34–47`).
- **Test — `client/Assets/Game/Tests/Core/WaterFlowTests.cs`:**
  - helper `Flat`, `Editor`, `Settle`, `Dig`, `IsWater`, `PondWorld()` (`:10–51`), `Lv` (`:524`);
  - `WideHolePastLevelRadiusStaysEmpty` (`:112–135`, brief 0084) ghi hành vi 0043, nay phải đổi;
  - `FallsThenSpreadsFullAgain` (`:177–212`): nguồn (1,11,1) đổ thác xuống sàn y = 2. Dòng cuối (`:209–211`) khẳng định nước chỉ lan 5 ô (d = 1..5) và (1,2,9) trống. Theo 0044, sàn y = 2 thấp hơn nguồn và nằm trong 16 ô, nên phải đầy mức 5. **Planner đã xét: test này phải đổi kỳ vọng** (bước 4).
- **Planner đã xét, không đổi:**
  - `WaterfallIntoPondDoesNotSpill` (cột rơi xuống ao: ô rơi không lan);
  - `FallingCellCreatesChildBelowOnly`, `FlowingWaterBelowSeaLevelDoesNotStartSeaFlood` (chỉ khẳng định ô rơi mức 5, ô mức 5);
  - thác demo `SimDemo.PlaceWaterfall` (`client/Assets/Game/Core/Sim/SimDemo.cs:89–106`): cột rơi xuống ao, mép tràn cùng `y` với nguồn.

  Planner chưa chạy các test này với code mới [chưa xác minh]. Executor chạy để xác nhận.
- `WorldEditor.IsSolidAt`: ô ngoài biên ngang **không** đặc (`:257–265`).
- `WorldSize(x, y, z)` phải là bội của 16 (`client/Assets/Game/Core/WorldData.cs:12–15`).
- Không dùng API Unity nào mới. `Game.Core` là C# thuần.

## Quyết định đã chốt (không được đổi)
1. **Nguồn gốc của cây:** từ nút đang xét, đi theo `Parent` tới nút có `Source = true`. Gặp `NoParent` mà chưa tới nguồn thì không có nguồn gốc. Chặn vòng giống `IsAncestor`.
2. **Ô thuộc vùng lấp (`InBasin(id)`)** khi đủ cả 4 điều kiện:
   - nút không phải nguồn;
   - có nguồn gốc `s`;
   - `node.Y < s.Y`;
   - `Math.Max(|node.X − s.X|, |node.Z − s.Z|) <= BasinFillRadius`.
3. **`WallBoost(id)` mới:**
   - nguồn, hoặc `level >= maxLevel` → bỏ qua;
   - `InBasin(id)` → tăng mức, **kể cả mức 0**;
   - ngược lại → giữ nguyên luật cũ (`level <= 0` thì bỏ qua; đếm khối đặc, đủ `wallBoostMinSolid` mới tăng);
   - "tăng mức" = `SetLevel(id, maxLevel); ActivateId(id); Propagate(id);` như hiện nay.
4. **`Propagate`:** sau khối `Reparent` (tức sau khi ô `n` đã có cha mới, nếu có), thêm `if (InBasin(n)) SetLevel(n, maxLevel);` trước `stack.Push(n)`. Mục đích: ô cũ trong vùng lấp được phát sóng tới cũng lên 5. Mức chỉ tăng nên vòng lặp vẫn dừng.
5. **Tham số:**
   - `basinFillRadius` = 16 [số tạm], chưa chốt; hợp lệ 0..64;
   - nằm trong `WaterConfig` + cả 2 file `water_m1.json`; `configVersion` 3 → 4;
   - `WorldEditor.BasinFillRadius` mặc định 16, setter kiểm 0..64, sai thì `ArgumentOutOfRangeException`.
6. Không đổi: thứ tự lan, luật rơi, xoá dây chuyền, tái tạo, biển 0013, mesher.

## Phạm vi
- Được sửa:
  - `client/Assets/Game/Core/WorldEditor.cs`;
  - `client/Assets/Game/Core/WaterConfig.cs`;
  - `design/data/water_m1.json`, `client/Assets/Game/Client/Resources/Config/water_m1.json`;
  - `client/Assets/Game/Client/TerrainChunkView.cs` (1 dòng);
  - `client/Assets/Game/Tests/Core/WaterFlowTests.cs`: đổi 2 test nêu ở bước 4, thêm test mới;
  - `client/Assets/Game/Tests/Core/WaterConfigTests.cs`;
  - `design/04-tech/world-tech.md` (1 dòng, bước 6).
- Không được sửa:
  - mọi test cũ khác;
  - `BlockMesher.cs`, shader, `SimDemo.cs`, `SeaFloodTests.cs`.

## Các bước
1. **`WaterConfig.cs` + 2 file JSON.**
   - Thêm trường, ngay sau `wallBoostMinSolid`:
     ```csharp
     /// <summary>Ô chảy thấp hơn nguồn gốc của cây, cách ≤ basinFillRadius ô ngang (Chebyshev), lên mức tối đa (design/decisions/0044).</summary>
     public int basinFillRadius = 16;
     ```
   - `configVersion = 4`.
   - `Validate()` thêm: `if (basinFillRadius < 0 || basinFillRadius > 64) throw new ArgumentException("basinFillRadius must be in 0..64.");`
   - Hai file JSON: `"configVersion": 4`, và thêm `"basinFillRadius": 16` ngay sau `"wallBoostMinSolid": 2`. Sau khi sửa, hai file phải giống hệt nhau (`diff` rỗng).
   - Xong khi: biên dịch không lỗi.
2. **`WorldEditor.cs`.**
   - Sau property `WallBoostMinSolid` (`:81`), thêm `int basinFillRadius = 16;` và property `BasinFillRadius` theo mẫu `WallBoostMinSolid`:
     - kiểm `0..64`, message `"BasinFillRadius must be in 0..64"`;
     - summary: `/// <summary>Bán kính ngang (Chebyshev) quanh nguồn gốc mà ô chảy thấp hơn nguồn lên mức MaxLevel (0044), hợp lệ 0–64.</summary>`.
   - Cạnh `IsDoomed`, thêm 2 hàm theo QĐ 1–2:
     - `int RootSourceOf(int id)`: trả id nguồn gốc, không có thì −1;
     - `bool InBasin(int id)`.
   - Sửa `WallBoost` theo QĐ 3. Summary đổi thành: `/// <summary>Ô chảy trong vùng lấp (0044) hoặc kề ≥ WallBoostMinSolid ô ngang đặc (0043 QĐ 8) → mức MaxLevel rồi phát sóng mức.</summary>`.
   - Sửa `Propagate` theo QĐ 4.
   - Summary lớp (`:31–35`): thêm 1 câu sau câu tăng mức bởi tường: `Ô chảy thấp hơn nguồn gốc của cây, trong BasinFillRadius ô ngang, cũng lên MaxLevel (0044).`
   - Xong khi: biên dịch không lỗi.
3. **`TerrainChunkView.cs`:** ngay sau dòng `Editor.WallBoostMinSolid = waterCfg.wallBoostMinSolid;` (`:232`), thêm `Editor.BasinFillRadius = waterCfg.basinFillRadius;`.
4. **`WaterFlowTests.cs` — đổi 2 test cũ.**
   - `WideHolePastLevelRadiusStaysEmpty` → **đổi tên** thành `WideHoleLowerLayerFills`.
     - Chú thích: `/// <summary>0044: hố rộng 2 lớp cạnh ao — lớp dưới (thấp hơn mặt ao) đầy mức 5, lớp ngang mặt ao vẫn lan giảm 1 mức/ô (0043), không dâng.</summary>`.
     - Phần dựng giữ nguyên.
     - Khẳng định:
       - lớp trên, hàng z = 5: (8..12, 3, 5) là nước, mức 4, 3, 2, 1, 0; (13..15, 3, 5) Empty. Giống 0084.
       - lớp dưới: **mọi** ô (x, 2, z), x 8..15, z 2..9 là nước, `Lv` = 5.
       - (8, 2, 5) `Cell.IsFalling`.
   - `FallsThenSpreadsFullAgain`:
     - giữ mọi khẳng định trừ 2 dòng cuối (`:209–211`);
     - thay bằng: mọi ô (x, 2, z) với x 0..15, z 3..15 là nước, `Lv` = 5. Lý do: sàn thấp hơn nguồn (1,11,1), toàn bộ trong 16 ô;
     - thêm chú thích 1 dòng ngay trên các khẳng định mới: `// 0044: sàn y = 2 thấp hơn nguồn, trong basinFillRadius → đầy mức 5.`
5. **`WaterFlowTests.cs` — thêm test mới.**
   - Helper `static WorldData BasinWorld()`, chú thích: `/// <summary>World 32×16×16: đá y 0..1 (y = 0 Bedrock), cột đá (0, 2..8, 8), nguồn (0, 9, 8) — nước tràn 3 hướng rồi rơi xuống sàn y = 2.</summary>`.
     - `new WorldData(new WorldSize(32, 16, 16))`;
     - x 0..31, z 0..15: (x,0,z) Bedrock, (x,1,z) Stone;
     - (0, y, 8) Stone với y 2..8;
     - (0, 9, 8) = `Cell.Make(BlockMaterial.Water, Cell.MaxWaterLevel)`.
   - Helper `static void AssertBasin(WorldData w, int r)`, khẳng định:
     - mọi (x, 2, z) với x 0..min(r, 31), z 8−r..8+r (giới hạn trong 0..15), trừ (0, 2, 8), là nước, `Lv` = 5;
     - hàng z = 8: (r+1+i, 2, 8), i = 0..4, là nước, không rơi, `Lv` = 4 − i;
     - mọi (x, 2, z) với x ≥ r + 6 (≤ 31), z 0..15 là Empty.
   - `BasinFillsWithinRadius`:
     - `BasinWorld()`, `Editor(w)`;
     - `e.WakeWater(0, 9, 8)`;
     - `Settle(e)`;
     - `AssertBasin(w, 16)`.
     - Chú thích: `/// <summary>0044: vùng thấp rộng dưới thác chỉ ngập mức 5 trong basinFillRadius = 16 ô ngang từ nguồn; ngoài đó lan giảm như 0043.</summary>`.
   - `BasinFillRadiusIsTunable`: như trên, nhưng đặt `e.BasinFillRadius = 4` trước `WakeWater`, rồi `AssertBasin(w, 4)`. Thêm khẳng định:
     - `Assert.Throws<System.ArgumentOutOfRangeException>(() => e.BasinFillRadius = -1)`;
     - tương tự với `65`.
   - `BasinFillIsDeterministic`:
     - chạy kịch bản `BasinFillsWithinRadius` 2 lần trên 2 world riêng;
     - gom mọi ô `w.Get(x, y, z)` (thứ tự z, y, x như `RandomEdits`);
     - `CollectionAssert.AreEqual`.
   - Xong khi: 3 test mới đạt.
6. **`WaterConfigTests.cs`:**
   - `ConfigParsesAndValidates`: `configVersion` 4, thêm `Assert.AreEqual(16, cfg.basinFillRadius);`;
   - `ValidateRejectsBadValues`: thêm `basinFillRadius = -1` và `= 65` → `Throws`.
7. **`design/04-tech/world-tech.md:137`** (dòng bắt đầu `- **Ô thấp hơn mặt nguồn thì đầy**`): thay `trong 16 ô ngang từ nguồn [số tạm]` bằng `trong \`basinFillRadius\` = 16 ô ngang (Chebyshev) từ nguồn gốc [số tạm], chưa chốt, \`water_m1.json\` v4`. Cuối dòng thêm ` Code + test: brief 0085.`

## Tiêu chí xong
- Đạt: `WideHoleLowerLayerFills`, `FallsThenSpreadsFullAgain` (đã đổi), 3 test mới, `WaterConfigTests`.
- Trên code cũ, test hố rộng đã đổi kỳ vọng fail. Ghi output fail ở Kết quả test.
- EditMode toàn bộ đạt, gồm `GoldenHashTests` (mã băm không đổi) và `SeaFloodTests` (0013).
- PlayMode các lớp nước đạt.
- Console 0 lỗi.
- `git status` chỉ có file trong Phạm vi.

## Test sau khi xong

### Phần cần test
- **Hố rộng 2 lớp** (đổi kỳ vọng) · `WaterFlowTests.WideHoleLowerLayerFills` · lớp dưới 64 ô mức 5; lớp trên hàng z = 5 vẫn là 4..0 rồi trống.
- **Sàn dưới thác** (đổi kỳ vọng) · `WaterFlowTests.FallsThenSpreadsFullAgain` · cột rơi như cũ; sàn y = 2 đầy mức 5.
- **Giới hạn bán kính** (mới) · `BasinFillsWithinRadius` · trong 16 ô mức 5; ô 17..21 trên hàng z = 8 mức 4..0; từ ô 22 trở đi trống.
- **Tham số chỉnh được** (mới) · `BasinFillRadiusIsTunable` · bán kính 4 cho đúng mẫu; setter từ chối −1 và 65.
- **Tất định** (mới + regression) · `BasinFillIsDeterministic`, `FlowIsDeterministic`, `FlowSettlesWithinBound` · đạt.
- **Cấu hình** (đổi) · `WaterConfigTests` (3 test) · v4, `basinFillRadius` 16, file khớp mặc định, từ chối giá trị sai.
- **Luật 0043 cùng lớp với nguồn** (regression) · toàn bộ `WaterFlowTests`, trong đó có `DiamondLevelsOnOpenFloor`, `ChannelOpensIntoWideArea`, `WaterfallIntoPondDoesNotSpill`, các test xoá dây chuyền · đạt.
- **Biển 0013, mã băm world** (regression) · `SeaFloodTests`, `GoldenHashTests` · đạt, mã băm không đổi.
- **Nơi khác dùng nước** (regression) · `WorldEditorTests`, `HabitatTrackerTests`, `HabitatWorldTests`, `BlockMesherTests` (trong EditMode toàn bộ) · đạt.
- **Client nạp tham số; thác demo vẫn đổ vào ao** (regression) · PlayMode `WaterFlowPlayTests`, `WaterLayerPlayTests`, `TerrainEditPlayTests` · đạt.

### Lệnh executor chạy
1. Trước bước 2: làm xong bước 1 và đổi `WideHoleLowerLayerFills` (bước 4). Chạy `run_tests` EditMode, lọc `WaterFlowTests.WideHoleLowerLayerFills`, rồi ghi lại output fail.
2. Sau bước 1–7: `refresh_unity` → `read_console` (Error) = 0.
3. `run_tests` EditMode, lọc `WaterFlowTests`, `WaterConfigTests`, `SeaFloodTests` → đạt.
4. `run_tests` EditMode toàn bộ → đạt.
5. `run_tests` PlayMode, lọc `Game.Client.Tests.WaterFlowPlayTests`, `Game.Client.Tests.WaterLayerPlayTests`, `Game.Client.Tests.TerrainEditPlayTests` → đạt. Sau đó:
   - `git checkout -- client/ProjectSettings/EditorSettings.asset`;
   - nếu `ProjectSettings.asset` đổi (`runInBackground`), cũng `git checkout`.
6. `diff design/data/water_m1.json client/Assets/Game/Client/Resources/Config/water_m1.json` → rỗng.
7. `git status` / `git diff --stat` → chỉ file trong Phạm vi. Có `InitTestScene*` thì xoá.

### User tự test
Gộp cuối 5j; Planner chuyển sang danh sách chơi thử. Play, scene SimView:
- (a) Đào hố rộng 2 lớp (ví dụ 6×6) cạnh ao. Lớp dưới đầy nước; lớp trên ngang mặt ao vẫn mỏng dần ra xa.
- (b) Đào hố sâu 3–4 lớp cạnh ao. Các lớp dưới mặt ao đầy, mặt nước không cao hơn mặt ao.
- (c) Thác demo vẫn đổ vào ao, không làm ngập đồng bằng quanh ao.
- (d) Đặt khối chặn chỗ hố nối với ao. Nước trong hố rút dần như trước.

## Dừng và chuyển lại Opus khi
- Brief mơ hồ hoặc mâu thuẫn với code thật (số dòng lệch nhiều, chữ ký khác).
- Một test cũ **ngoài 2 test ở bước 4** fail sau khi sửa. Ghi tên test và output, không tự đổi kỳ vọng.
- Giá trị thật khác kỳ vọng ở bước 4–5 (ví dụ lớp trên hố rộng khác 4..0, hoặc hàng z = 8 ngoài bán kính khác 4..0). Ghi giá trị thật, không tự đổi kỳ vọng.
- `GoldenHashTests` đổi mã băm, hoặc `SettleWater` không dừng (`HasPendingWater` còn true).
- Cần quyết định ngoài "Quyết định đã chốt", cần sửa file ngoài "Phạm vi", hoặc cần API Unity.
- Cùng một lỗi sửa 2 lần vẫn chưa qua.

---
<!-- Từ đây trở xuống: EXECUTOR ghi. -->

## Kết quả

- Base commit: 3cff735
- Chạy bằng: claude-sonnet-5-5 · medium
- Bắt đầu: 19:46 · kết thúc: ~20:10 (phần lớn là chờ EditMode toàn bộ ~7,5 phút)
- Commit: 181acbd
- File đổi: WorldEditor.cs, WaterConfig.cs, TerrainChunkView.cs, 2 water_m1.json (v4, giống hệt), WaterFlowTests.cs, WaterConfigTests.cs, world-tech.md:138

## Kết quả test
- `WideHoleLowerLayerFills`, `FallsThenSpreadsFullAgain` (đã đổi), `BasinFillsWithinRadius`, `BasinFillRadiusIsTunable`, `BasinFillIsDeterministic`: đạt (chạy riêng, 5/5).
- WaterConfigTests, SeaFloodTests, GoldenHashTests, toàn bộ WaterFlowTests: đạt trong EditMode toàn bộ 666/666, 0 fail.
- PlayMode WaterFlowPlayTests + WaterLayerPlayTests + TerrainEditPlayTests: 18/18 đạt.
- Console Error = 0 sau refresh. `diff` 2 file JSON rỗng. Đã `git checkout` EditorSettings.asset + ProjectSettings.asset; `git status` chỉ file trong Phạm vi, không có InitTestScene.
- **Chưa làm:** bước "chạy test hố rộng trên code cũ để ghi output fail" (Lệnh 1) — tôi viết code mới trước khi chạy, không có output fail; không dùng stash/reset vì phiên dùng chung thư mục. Planner quyết định có cần.
- User tự test (a)–(d): chờ user (gộp cuối 5j).

## Vướng mắc

---
<!-- PLANNER ghi khi review. -->

## Review
