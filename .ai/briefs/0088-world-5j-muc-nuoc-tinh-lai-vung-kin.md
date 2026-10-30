# Brief 0088 — World 5j (lỗi 12): mức nước tính lại theo hình dạng + vùng kín ngang mặt nguồn thì đầy (0045)

> Trạng thái: chờ làm
> Review: đầy đủ + user xem (gộp cuối 5j)
> Model thực thi: Opus · medium
> Cập nhật: 2026-10-30 22:30

## Mục tiêu
Cài [0045](../../design/decisions/0045-ho-ngang-mat-nguon-thi-day-muc-tinh-lai.md) trong `WorldEditor`: (1) mức nước chảy là hàm của hình dạng hiện tại — đào theo thứ tự nào cũng ra cùng kết quả, hai ô kề không lệch quá 1 mức; (2) lớp ngang mặt nguồn đầy mức MaxLevel khi là vùng kín trong `BasinFillRadius`.

## Bối cảnh cần đọc
- Quyết định: [0045](../../design/decisions/0045-ho-ngang-mat-nguon-thi-day-muc-tinh-lai.md) (đọc hết), [0043](../../design/decisions/0043-nuoc-cay-cha-con-tang-muc-boi-tuong.md) mục 3–5, [0044](../../design/decisions/0044-ho-thap-hon-mat-nguon-thi-day.md).
- Task: [m1-world-5j-sua-loi-choi-thu](../tasks/m1-world-5j-sua-loi-choi-thu.md) lỗi 12 (số đo, nguyên nhân).
- `client/Assets/Game/Core/WorldEditor.cs` (Game.Core, không tham chiếu UnityEngine):
  - `:97` `struct FlowNode { X, Y, Z, Parent, Source }`; `:103` `const int NoParent = -1`; bảng `nodes` (id → FlowNode), `nodeAt` ((x,y,z) → id), `children` (id → List<int>), hàng `active`/`activeSet`, `cascade`/`cascadeSet`.
  - `:122` `public bool HasPendingWater => waterFront.Count > 0 || active.Count > 0 || cascade.Count > 0;`
  - `:151` `TryDig`, `:194` `TryPlace` (đã gọi `WallBoost` cho 4 ô ngang sau khi đặt).
  - `:271` `public static bool IsSolidAt(WorldData world, int x, int y, int z)`.
  - `:375` `int LevelOf(int id)` (nguồn → maxLevel); `:397` `bool TryFlowingNode(int x, int y, int z, out int id)`; `:406` `ActivateId`; `:425`-ish `RemoveNode(int id, bool toCascade)`; `:451` `void SetLevel(int id, int level)` (phát `WaterSurfaceChanged`); `:459` `void Reparent(int id, int parent)`; `IsAncestor`, `IsDoomed`, `RootSourceOf` ngay sau.
  - `:524` `bool InBasin(int id)` — hiện `n.Y < src.Y && Chebyshev ≤ basinFillRadius`.
  - `:537` `void WallBoost(int id)`, `:562` `void Propagate(int start)` — chỉ chạy khi một ô được tăng mức (nguyên nhân lỗi).
  - `:590` `int CreateChild(int parent, int x, int y, int z, int level, bool falling)`.
  - `:619` `int StepFlow()` = `StepCascade(); StepSpread();`; `:627` `StepCascade`; `:702` `StepSpread`.
- Test: `client/Assets/Game/Tests/Core/WaterFlowTests.cs` — helper `Flat`, `Editor`, `Settle`, `Dig`, `Place`, `IsWater`, `PondWorld`, `Corridor`, `OpenFloor`, `Node` (:516), `ParentOf` (:522), `Lv` (:528).

## Quyết định đã chốt (không được đổi)
- Luật 0043 giữ: lan +z/+x/−z/−x 1 ô/nhịp (`StepSpread` không đổi cách tạo ô mới), rơi tạo con mức MaxLevel, xoá dây chuyền 1 tầng/nhịp, tái tạo, nguồn không đổi mức.
- Vùng thấp hơn nguồn: 0044 giữ (`y < y nguồn`, Chebyshev ≤ `basinFillRadius`).
- **Vùng kín** (0045 mục 1): với ô chảy ở tầng `y` = `y` nguồn gốc, BFS 4 hướng ở tầng đó qua các ô `Empty` hoặc nước chảy (không qua ô nguồn, không qua ô đặc; ô ngoài world = vách). Kín ⇔ BFS không gặp ô nào có Chebyshev tới nguồn gốc > `basinFillRadius`. Kín → ô coi như "lấp hố" (lên MaxLevel giống `InBasin`).
- Mức = **điểm bất động nhỏ nhất** (không phụ thuộc thứ tự): trong một thành phần liên thông ngang C (các nút chảy cùng tầng, không thuộc nhánh đang xoá),
  `L(c) = max( seed(c), max_{n kề ngang ∈ C} L(n) − 1, maxLevel−1 nếu kề ngang một ô nguồn )`, rồi nếu `L(c) > 0` và (≥ `WallBoostMinSolid` ô ngang đặc, hoặc lấp hố theo 0044/vùng kín) → `L(c) = maxLevel`. `seed(c) = maxLevel` khi ô c là ô rơi (`Cell.IsFalling`) — cha của nó ở tầng trên. Ô không đạt được (không có đường tới seed/nguồn) → không có mức.
- Cha (để cây không có vòng): dựng kiểu Prim từ các gốc — ô rơi giữ cha ở tầng trên; các ô khác gắn lần lượt (mức cao trước, bằng nhau → id nhỏ trước) vào ô kề **đã gắn** có mức cao nhất (nguồn tính maxLevel; bằng nhau → **giữ cha hiện tại nếu nó nằm trong số đó**, không thì theo thứ tự +z, +x, −z, −x).
- Ô trong C không đạt được: không xoá ngay hàng loạt; chỉ ô có cha hiện tại đã đạt được (hoặc cha không thuộc C và không phải nguồn/ô tầng trên) vào hàng `cascade` — phần còn lại rút dần theo dây chuyền 0043 (giữ test `ChannelDrainsOneCellPerTick`).
- Tham số không đổi: `basinFillRadius` 16, `water_m1.json` không sửa.
- **(Planner 2026-10-30, trả lời Vướng mắc)** Giữ 0045 mục 1 nguyên văn (ô ngoài world = vách) — phương án C. World test 16×16 làm mọi sàn trống ngang mặt nguồn thành vùng kín; test muốn "vùng hở" phải đổi *thiết lập* (bán kính nhỏ hoặc world rộng), **không đổi kỳ vọng**.
- Điều kiện tăng mức trong `Relevel` theo đúng `WallBoost` hiện tại (`WorldEditor.cs:537–558`): lấp hố 0044 / vùng kín → MaxLevel **kể cả khi L = 0** (ô đạt được); tường (≥ `WallBoostMinSolid`) chỉ khi `L > 0`. Hai đường phải ra cùng kết quả.
- Quy tắc cascade cho ô không đạt được trong C (thay câu cũ): ô vào `cascade` khi cha của nó **không** phải một ô không-đạt-được trong C (cha đạt được, cha ngoài C, cha là nguồn — kể cả nguồn không kề do tái tạo `:684`). Ô có cha là ô không-đạt-được khác trong C → để dây chuyền 0043 rút dần.

## Phạm vi
- Được sửa/tạo: `client/Assets/Game/Core/WorldEditor.cs`; `client/Assets/Game/Tests/Core/WaterFlowTests.cs` (gồm đổi *thiết lập* — không đổi kỳ vọng — của `DiamondLevelsOnOpenFloor`, `SpreadStopsAfterN`, `ChannelOpensIntoWideArea`, `WallBoostOnPlaceAndPropagate`, `FallsThenSpreadsFullAgain` theo bước 0); mục Kết quả của brief này.
- `client/Assets/Game/Tests/World/BlockMesherTests.cs`: chỉ thêm `e.BasinFillRadius = 4` sau khi tạo editor trong `LipTapersAtChannelSpill` (Planner 2026-10-30, lượt 3).
- Không được sửa: file khác (mesher, shader, client, JSON, design/).

## Các bước
0. `WaterFlowTests.cs` — helper: thêm tham số `size` (mặc định 16) cho `Flat(solidTop, m, size)` → `WorldSize(size, 16, size)`; helper khác dựng trên `Flat` (vd `OpenFloor`) thêm tham số tương tự nếu cần. Đổi thiết lập (giữ nguyên toạ độ và kỳ vọng): `DiamondLevelsOnOpenFloor`, `SpreadStopsAfterN`, `ChannelOpensIntoWideArea`, `WallBoostOnPlaceAndPropagate` đặt `e.BasinFillRadius = 4` ngay sau khi tạo editor (trước `Dig`/nhịp đầu) — nếu bán kính 4 làm test lấp 0044 trong chính test đó sai, dùng world rộng 40 + bán kính mặc định thay vào; `FallsThenSpreadsFullAgain` dùng world 40×16×40 (vùng ngoài 0..15 dựng đúng như cũ: cùng chiều cao đá/khoảng trống theo tầng), bán kính mặc định. Xong khi: 5 test này đạt với code CŨ (chạy trước bước 1).
1. `WorldEditor.cs` — thêm tập vị trí bẩn `relevelDirty` (HashSet<(int,int,int)> + List giữ thứ tự chèn để tất định). Đánh dấu bẩn: ô của nút vừa `CreateChild`; ô vừa xoá trong `StepCascade` và 4 ô ngang của nó; trong `TryDig` và `TryPlace`: 4 ô ngang của ô vừa sửa; `FillWater` (`:316`, biển loang tạo nguồn): 4 ô ngang của ô vừa thành nguồn. Xong khi: mọi đường tạo/xoá nút/đổi vách đều đánh dấu.
2. `WorldEditor.cs` — thêm `void Relevel()` gọi ở cuối `StepFlow()` (sau `StepSpread`). Với mỗi vị trí bẩn (sắp xếp theo (y, z, x) tăng) còn là nút chảy chưa xử lý: BFS lấy thành phần C (nút chảy cùng tầng, 4 hướng, `!IsDoomed`). Tính mức theo điểm bất động ở mục Quyết định bằng hàng ưu tiên (mức cao trước); một ô có thể được nâng nhiều lần (kể cả ô đã lấy ra) — mức chỉ tăng nên dừng. Kiểm vùng kín một lần cho mỗi C (tâm = `RootSourceOf` của nút id nhỏ nhất trong C; chỉ khi `y` C = `y` nguồn đó). Xoá tập bẩn khi xong. Xong khi: biên dịch, `HasPendingWater` không đổi định nghĩa.
3. Trong `Relevel` — ghi kết quả: ô có mức mới ≠ mức cũ → `SetLevel` + `ActivateId`; dựng cha kiểu Prim (mục Quyết định) và `Reparent` khi khác; ô không đạt được → theo quy tắc cascade ở mục Quyết định. Ô đổi mức trong `Relevel` không đánh dấu bẩn lại (tránh vòng lặp vô hạn trong một nhịp).
4. `InBasin` — giữ điều kiện 0044; thêm nhánh vùng kín (gọi từ `Relevel` qua kết quả kiểm vùng kín của C, không BFS lại cho từng ô). `WallBoost`/`Propagate` cũ: giữ lời gọi hiện có (phản hồi ngay khi `TryPlace` — test `WallBoostOnPlaceAndPropagate` kiểm mức ngay sau `Place` không qua nhịp); `Relevel` ở nhịp sau phải cho cùng kết quả.
5. Cập nhật `<summary>` của lớp `WorldEditor` (dòng ~34) thêm 1 câu: "Mức tính lại theo hình dạng mỗi nhịp (0045): điểm bất động trên thành phần ngang; lớp ngang mặt nguồn đầy khi vùng kín trong BasinFillRadius."
6. `WaterFlowTests.cs` — đổi kỳ vọng `WideHoleLowerLayerFills` (lớp y = 3, x 8–15, z 2–9 là vùng kín trong 16 ô quanh ao) thành: mọi ô y = 3 trong hố là nước mức 5; giữ phần lớp y = 2 và ô rơi; đổi tên thành `WideHoleBothLayersFill`, sửa `<summary>` theo 0045.
7. `WaterFlowTests.cs` — thêm test (mỗi test một `[Test]`):
   - `DigOrderDoesNotChangeLevels`: `Flat(3)`, nguồn `(1,3,1)`, `e.BasinFillRadius = 2` (để vùng hở, không lấp); ô cần đào: x 1–7, z 1–6, y = 3 trừ nguồn. Kết quả tham chiếu: đào hết rồi `Settle`. Với `seed` 0..39: thế giới mới, xáo thứ tự bằng `new System.Random(seed)` (Fisher–Yates), mỗi lần `Dig` rồi `Settle`; so mảng `w.Get(x,3,z)` (mức + vật liệu) với tham chiếu → bằng nhau. Thêm khẳng định: mọi cặp ô nước kề ngang lệch mức ≤ 1 (nguồn tính 5).
   - `TrenchAtSourceLevelFillsWhenEnclosed`: `PondWorld()` (ao x 4–7, z 4–7, y = 3); đào rãnh 1 ô x = 8, z = 5 rồi phòng rộng x 9–13, z 3–7 ở y = 3. `Settle` → mọi ô đào là nước mức 5.
   - `OpenFloorAtSourceLevelStillDecreases`: kịch bản `DiamondLevelsOnOpenFloor` (cùng toạ độ, cùng khẳng định 5 − d) trên world `Flat(..., size: 40)`, `BasinFillRadius` 16 mặc định (sàn lan tới x/z 39 → hở). Test mới, bắt buộc.
   - `OpeningEnclosureLowersLevels`: `PondWorld()`; trước khi tạo editor, `w.Set((15,3,z), Cell.Make(BlockMaterial.Empty))` với z 0–15 (dải trống x = 15, ngăn với phòng bởi đá x = 14). `e.BasinFillRadius = 6`. Đào rãnh `(8,3,5)` + phòng x 9–13, z 3–7 như test trên, `Settle` → mọi ô đào mức 5 (phòng kín, xa nhất Chebyshev 6 từ ao). Rồi `Dig(14,3,5)` (phòng thông ra dải x = 15, có ô cách ao > 6 → hở), `Settle` → mọi cặp nước kề ngang lệch ≤ 1 mức; `(10,3,5)` mức < 5; `HasPendingWater` false.
   - `LosingWallLowersBoostedCell`: `Corridor()` (tường z = 7 và 9), `Dig(1,6,8)`, `Settle` → x 1–15 mức 5 (như `ChannelFlowsFullLength`). `BasinFillRadius = 2`. Đào tường `(8,6,9)` và `(8,6,7)` (mở hai bên) rồi `Settle` → `(8,6,8)` vẫn là nước, mọi cặp nước kề ngang lệch ≤ 1, và kết quả bằng kết quả khi các ô đó được đào trước khi nước chảy tới (thế giới mới, đào trước, `Dig(1,6,8)`, `Settle`, so tầng y = 6).
8a. (lượt 3) `BlockMesherTests.LipTapersAtChannelSpill`: thêm `e.BasinFillRadius = 4` ngay sau khi tạo editor; kỳ vọng giữ nguyên.
8b. (lượt 3) Đặt khối làm phòng thành kín phải kích hoạt tính lại (0045 mục 2 gạch 3): `HasPendingWater` thêm `|| relevelDirty.Count > 0` (đổi định nghĩa được phép); `Relevel` xoá tập bẩn nên vẫn lắng. Thêm test `PlacingWallEnclosesAndFills`: `PondWorld()` + dải trống x = 15 như `OpeningEnclosureLowersLevels`, `BasinFillRadius = 6`, đào rãnh + phòng + `(14,3,5)`, `Settle` (hở, `(10,3,5)` < 5); `Place(14,3,5)` đá, `Settle` → mọi ô trong phòng mức 5, `HasPendingWater` false.
8. Chạy toàn bộ `WaterFlowTests` + `SeaFloodTests`. Test nào khác ngoài `WideHoleLowerLayerFills` phải đổi kỳ vọng (đổi thiết lập ở bước 0 không tính) → **dừng, cần Opus** (ghi assertion fail, giá trị mong/đã thấy).

## Tiêu chí xong
- 5 test mới và `WideHoleBothLayersFill` đạt; mọi test cũ trong `WaterFlowTests`, `SeaFloodTests` đạt không đổi kỳ vọng.
- EditMode toàn bộ đạt; PlayMode `WaterFlowPlayTests`, `UnderFallPlayTests` đạt.
- Không có `using UnityEngine` trong `WorldEditor.cs`.

## Test sau khi xong

### Phần cần test
- Relevel độc lập thứ tự (bước 1–3) · `DigOrderDoesNotChangeLevels` · 40 thứ tự = tham chiếu, lệch kề ≤ 1.
- Vùng kín ngang mặt nguồn đầy (bước 4) · `TrenchAtSourceLevelFillsWhenEnclosed`, `WideHoleBothLayersFill` · mọi ô mức 5.
- Vùng hở giữ 0043 (bước 4) · `DiamondLevelsOnOpenFloor`, `SpreadStopsAfterN`, `ChannelOpensIntoWideArea` · đạt không đổi.
- Hạ mức khi mất kín/mất vách (bước 2–3) · `OpeningEnclosureLowersLevels`, `LosingWallLowersBoostedCell` · đạt.
- Tăng mức ngay khi đặt khối (bước 4) · `WallBoostOnPlaceAndPropagate` · đạt không đổi.
- Rút theo nhịp + tái tạo (regression) · `ChannelDrainsOneCellPerTick`, `RegenerateFromSecondSource`, `CascadeRemovesSubtreeOnly`, `BlockingChannelDrainsDownstream`, `ChannelLoopDrainsWhenSourceRemoved` · đạt.
- Lấp thấp hơn nguồn 0044 (regression) · `BasinFillsWithinRadius`, `BasinFillRadiusIsTunable`, `BasinFillIsDeterministic`, `WaterfallIntoPondDoesNotSpill` · đạt.
- Tất định + dừng (regression) · `FlowIsDeterministic`, `FlowSettlesWithinBound` · đạt.
- Biển (regression) · `SeaFloodTests` · đạt.
- Client (regression) · PlayMode `WaterFlowPlayTests`, `UnderFallPlayTests` · đạt.

### Lệnh executor chạy
1. Qua MCP Unity (Editor đang mở): `run_tests` EditMode, lọc `Game.Core.Tests.WaterFlowTests` rồi `SeaFloodTests` — ghi số đạt/fail.
2. `run_tests` EditMode toàn bộ — dòng tổng kết.
3. `run_tests` PlayMode lọc `WaterFlowPlayTests`, `UnderFallPlayTests` — dòng tổng kết.
4. `read_console` Error — 0.
5. Dọn: `git checkout -- client/ProjectSettings/EditorSettings.asset`; `git status` chỉ còn file trong Phạm vi.
(Editor tắt thì dùng `python3 tools/unity_test.py EditMode` / `PlayMode`.)

### User tự test
Gộp cuối 5j: đào rãnh 1 ô từ ao rồi mở phòng rộng 3–5 ô cùng tầng mặt ao, đào theo thứ tự lung tung → nước đầy, mặt phẳng ngang mặt ao. Đào thông phòng ra đất trống rộng → nước hạ dần đều (không ô 5 lẻ cạnh ô thấp).

## Dừng và chuyển lại Opus khi
- Brief mơ hồ hoặc mâu thuẫn với code thật.
- Cần một quyết định thuộc AGENTS.md §3.4 hoặc ngoài "Quyết định đã chốt".
- Test cũ nào (ngoài `WideHoleLowerLayerFills`) phải đổi kỳ vọng, hoặc phải đổi thiết lập test ngoài 5 test ở bước 0.
- Cùng một lỗi sửa 2 lần vẫn chưa qua.
- Phải sửa file ngoài "Phạm vi".

---

## Kết quả
- Base commit: 2ba7005 (lượt 1: a4fe6bb)
- Chạy bằng: claude-opus-5-5 · medium
- Lượt 1: 2026-10-30 21:15–21:25 (dừng, Vướng mắc). Lượt 2: 2026-10-30 22:00–22:30 (dừng, Vướng mắc 2).
- Code bước 0–7 đã viết, **chưa commit** (để trong working tree): `client/Assets/Game/Core/WorldEditor.cs`, `client/Assets/Game/Tests/Core/WaterFlowTests.cs`.
- Lệch brief (cần Planner xác nhận khi review):
  1. `WorldSize` bắt buộc chia hết cho 16 (`WorldData.cs:19`, [run] `ArgumentException: Size must be positive and divisible by 16: 40x16x40`) → world rộng dùng **48** thay 40 (`FallsThenSpreadsFullAgain`, `OpenFloorAtSourceLevelStillDecreases`).
  2. Tái tạo trong `StepCascade` đổi thành mức `max(ô kề) − 1`, chỉ khi ô kề mức ≥ 1 (trước: mức = ô kề, kể cả 0) — theo 0045 mục 2 ("ô mới tạo/tái tạo nhận lv = max(ô kề) − 1"). Lý do: giữ như cũ thì ô tái tạo cạnh ô mức 0 không đạt được trong `Relevel` → vào hàng xoá → bị xoá → tái tạo lại → không bao giờ lắng.
  3. Vùng kín áp cho cả thành phần C (tâm = `RootSourceOf` nút id nhỏ nhất), qua tham số `InBasin(id, enclosed)`.
- Ghi chú (ngoài phạm vi, chưa làm): đặt khối vào ô rỗng làm phòng thành kín không kích hoạt nước (`HasPendingWater` có thể false) → `Relevel` chỉ chạy khi có nước đang lan/rút; `WallBoost` ở `TryPlace` chỉ xét 0044 + tường, không xét vùng kín.

## Kết quả test
- Bước 0 (code cũ): 5 test đổi thiết lập đạt [run MCP, 5/5 sau khi đổi 40 → 48].
- EditMode `WaterFlowTests` + `SeaFloodTests`: **50/50 đạt** (gồm 5 test mới + `WideHoleBothLayersFill`) [run MCP].
- EditMode toàn bộ: **684/685** — fail `Game.World.Tests.BlockMesherTests.LipTapersAtChannelSpill` (`BlockMesherTests.cs:1109`): `Expected: 4 But was: 5` (mức ô mép `(7,11,7)`).
- PlayMode `WaterFlowPlayTests` + `UnderFallPlayTests`: 7/7 đạt.
- `read_console` Error: 0. Không có `using UnityEngine` trong `WorldEditor.cs`. Đã dọn `EditorSettings.asset`.

## Vướng mắc
**Mâu thuẫn: world test 16×16 + "ô ngoài world = vách" (0045 mục 1) → mọi sàn trống ngang mặt nguồn trong test cũ thành vùng kín với `BasinFillRadius` 16.**
- [repo] `Flat()` tạo world 16×16×16 (`WaterFlowTests.cs:12`); `OpenFloor()` nguồn (0,6,8) (`:71`). Chebyshev xa nhất từ (0,8) trong 16×16 là 15 ≤ 16, ô ngoài world là vách → BFS vùng kín không bao giờ gặp ô > 16 → lớp y = 6 là vùng kín → mọi ô chảy lên mức 5 (không còn hình thoi).
- Test cũ sẽ phải đổi kỳ vọng (điều kiện dừng): `DiamondLevelsOnOpenFloor` (:613, mong 5 − d), `SpreadStopsAfterN` (:139, mong 4..0), `ChannelOpensIntoWideArea` (:306, mong 4..0 ở x 6–10), `WallBoostOnPlaceAndPropagate` (:681, mong (2,6,8) = 3), `FallsThenSpreadsFullAgain` (:179, lớp y = 11 trống 16×16 quanh nguồn (1,11,1) → (1,11,3) thành 5, test mong 4). Bước 7 của brief ("`OpenFloorAtSourceLevelStillDecreases` với `BasinFillRadius` = 16 mặc định — khẳng định như `DiamondLevelsOnOpenFloor`") không thể đạt.
- `FallsThenSpreadsFullAgain` không sửa được chỉ bằng hạ bán kính: phần 0044 của nó (sàn y = 2 đầy mức 5 trên cả 16×16, :211) cần bán kính ≥ 14.

**Phương án**
- A. Giữ 0045; sửa *thiết lập* (không đổi kỳ vọng) của các test trên: `DiamondLevelsOnOpenFloor`, `SpreadStopsAfterN`, `ChannelOpensIntoWideArea`, `WallBoostOnPlaceAndPropagate` đặt `e.BasinFillRadius = 4` trước `Dig`; `FallsThenSpreadsFullAgain` dùng world rộng hơn (vd 40×16×40, toạ độ giữ nguyên — lớp y = 11 trống tới x/z = 39 → hở; sàn y = 2 trong 0..15 vẫn ≤ 16 từ nguồn). `OpenFloorAtSourceLevelStillDecreases` dùng world rộng 40 với bán kính 16 mặc định.
- B. Đổi luật: BFS chạm ô ngoài world (theo x/z) → coi là **hở**. Không phải sửa test cũ, nhưng trái 0045 mục 1 (Chốt) → cần quyết định mới của user; ở game thật, hồ sát mép map sẽ không đầy.
- C. Như A nhưng thêm tham số helper `Flat(solidTop, m, size)` dùng chung cho các world rộng.

**Đề xuất:** A (hoặc C — cùng nội dung, gọn hơn). Không đổi luật đã chốt; chỉ đổi thiết lập test cho đúng ý "vùng hở". Planner cập nhật brief (Phạm vi cho phép đổi thiết lập các test trên; mục "Test cũ phải đổi kỳ vọng" ghi rõ danh sách).

**Điểm phụ (cần Planner xác nhận khi sửa brief):**
1. Brief ghi tăng mức chỉ khi `L(c) > 0` cho cả lấp hố, nhưng `WallBoost` hiện tại lấp hố 0044 cả khi mức 0 (`WorldEditor.cs:545–548`). Đề xuất: `Relevel` theo `WallBoost` (lấp hố/vùng kín không cần `L > 0`; tường cần `L > 0`) để hai đường cho cùng kết quả như bước 4 yêu cầu.
2. Ô cha không phải ô kề (tái tạo trong `StepCascade` lấy `nodes[nid].Parent` của ô kề, `:684`) có thể là nguồn không kề → ô không đạt được nhưng cha là nguồn → theo quy tắc cascade của brief sẽ không bao giờ rút. Đề xuất: ô không đạt được vào `cascade` khi cha **không** là ô không-đạt-được trong C (gồm cả cha ngoài C bất kỳ).
3. `FillWater` (biển loang tạo nguồn mới) nên đánh dấu bẩn 4 ô ngang (bước 1 chưa liệt kê).

**Vướng mắc 2 (lượt 2): `BlockMesherTests.LipTapersAtChannelSpill` phải đổi kỳ vọng hoặc thiết lập — file ngoài Phạm vi.**
- [repo] `BlockMesherTests.cs:1080–1111`: world 16×16×16, ao nguồn y = 11 (x 5–9, z 1–5), đá z ≤ 6 tới y = 11, z ≥ 7 trống; đào `(7,11,6)` → mép `(7,11,7)` rơi xuống. Test mong mép mức 4.
- [run] Lớp y = 11 = mặt nguồn; BFS vùng kín qua ô trống z 7–15 (ngoài world = vách): xa nhất Chebyshev 10 từ nguồn ≤ 16 → **kín** → mép lên 5. Đúng 0045 mục 1 nguyên văn — cùng loại với 5 test ở bước 0.
- Phương án: A. Đổi thiết lập: `e.BasinFillRadius = 4` sau khi tạo editor (vùng trống lan tới Chebyshev 10 > 4 → hở → mép 4; mép không phải lấp 0044 vì cùng tầng nguồn). B. World 32×16×32 (vùng trống lan xa > 16) — đổi thêm vòng dựng world và chỗ build mesh chunk 0 vẫn đúng. C. Đổi kỳ vọng thành 5 + sửa các khẳng định mesh (taper 11.825/11.5025 theo mức 4) — nhiều việc, đổi ý nghĩa test.
- **Đề xuất: A** (1 dòng, giữ kỳ vọng). Planner cho phép sửa `client/Assets/Game/Tests/World/BlockMesherTests.cs` (chỉ thiết lập test này).

---

## Review
- 2026-10-30 (Planner, trả lời Vướng mắc): chọn C — giữ 0045 (ô ngoài world = vách), đổi thiết lập 5 test cũ (bước 0), `OpenFloorAtSourceLevelStillDecreases` dùng world 40. Điểm phụ 1–3 chấp nhận đúng đề xuất của Executor (ghi vào Quyết định đã chốt / bước 1). Trạng thái → chờ làm.
- 2026-10-30 (Planner, trả lời Vướng mắc 2): chọn A — cho sửa thiết lập `LipTapersAtChannelSpill` (bán kính 4, bước 8a). Chấp nhận lệch 1 (world 48, `WorldSize` phải chia hết 16 — `WorldData.cs:19`), lệch 2 (tái tạo `max(kề) − 1` — đúng 0045 mục 2 gạch 1, `design/decisions/0045-…:19`), lệch 3. Ghi chú đặt khối không kích hoạt: thuộc 0045 mục 2 gạch 3 → thêm bước 8b + test. Trạng thái → chờ làm.
