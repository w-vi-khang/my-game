# Brief 0088 — World 5j (lỗi 12): mức nước tính lại theo hình dạng + vùng kín ngang mặt nguồn thì đầy (0045)

> Trạng thái: chờ làm
> Review: đầy đủ + user xem (gộp cuối 5j)
> Model thực thi: Opus · medium
> Cập nhật: 2026-10-30

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

## Phạm vi
- Được sửa/tạo: `client/Assets/Game/Core/WorldEditor.cs`; `client/Assets/Game/Tests/Core/WaterFlowTests.cs`; mục Kết quả của brief này.
- Không được sửa: file khác (mesher, shader, client, JSON, design/).

## Các bước
1. `WorldEditor.cs` — thêm tập vị trí bẩn `relevelDirty` (HashSet<(int,int,int)> + List giữ thứ tự chèn để tất định). Đánh dấu bẩn: ô của nút vừa `CreateChild`; ô vừa xoá trong `StepCascade` và 4 ô ngang của nó; trong `TryDig` và `TryPlace`: 4 ô ngang của ô vừa sửa. Xong khi: mọi đường tạo/xoá nút/đổi vách đều đánh dấu.
2. `WorldEditor.cs` — thêm `void Relevel()` gọi ở cuối `StepFlow()` (sau `StepSpread`). Với mỗi vị trí bẩn (sắp xếp theo (y, z, x) tăng) còn là nút chảy chưa xử lý: BFS lấy thành phần C (nút chảy cùng tầng, 4 hướng, `!IsDoomed`). Tính mức theo điểm bất động ở mục Quyết định bằng hàng ưu tiên (mức cao trước); một ô có thể được nâng nhiều lần (kể cả ô đã lấy ra) — mức chỉ tăng nên dừng. Kiểm vùng kín một lần cho mỗi C (tâm = `RootSourceOf` của nút id nhỏ nhất trong C; chỉ khi `y` C = `y` nguồn đó). Xoá tập bẩn khi xong. Xong khi: biên dịch, `HasPendingWater` không đổi định nghĩa.
3. Trong `Relevel` — ghi kết quả: ô có mức mới ≠ mức cũ → `SetLevel` + `ActivateId`; dựng cha kiểu Prim (mục Quyết định) và `Reparent` khi khác; ô không đạt được → theo quy tắc cascade ở mục Quyết định. Ô đổi mức trong `Relevel` không đánh dấu bẩn lại (tránh vòng lặp vô hạn trong một nhịp).
4. `InBasin` — giữ điều kiện 0044; thêm nhánh vùng kín (gọi từ `Relevel` qua kết quả kiểm vùng kín của C, không BFS lại cho từng ô). `WallBoost`/`Propagate` cũ: giữ lời gọi hiện có (phản hồi ngay khi `TryPlace` — test `WallBoostOnPlaceAndPropagate` kiểm mức ngay sau `Place` không qua nhịp); `Relevel` ở nhịp sau phải cho cùng kết quả.
5. Cập nhật `<summary>` của lớp `WorldEditor` (dòng ~34) thêm 1 câu: "Mức tính lại theo hình dạng mỗi nhịp (0045): điểm bất động trên thành phần ngang; lớp ngang mặt nguồn đầy khi vùng kín trong BasinFillRadius."
6. `WaterFlowTests.cs` — đổi kỳ vọng `WideHoleLowerLayerFills` (lớp y = 3, x 8–15, z 2–9 là vùng kín trong 16 ô quanh ao) thành: mọi ô y = 3 trong hố là nước mức 5; giữ phần lớp y = 2 và ô rơi; đổi tên thành `WideHoleBothLayersFill`, sửa `<summary>` theo 0045.
7. `WaterFlowTests.cs` — thêm test (mỗi test một `[Test]`):
   - `DigOrderDoesNotChangeLevels`: `Flat(3)`, nguồn `(1,3,1)`, `e.BasinFillRadius = 2` (để vùng hở, không lấp); ô cần đào: x 1–7, z 1–6, y = 3 trừ nguồn. Kết quả tham chiếu: đào hết rồi `Settle`. Với `seed` 0..39: thế giới mới, xáo thứ tự bằng `new System.Random(seed)` (Fisher–Yates), mỗi lần `Dig` rồi `Settle`; so mảng `w.Get(x,3,z)` (mức + vật liệu) với tham chiếu → bằng nhau. Thêm khẳng định: mọi cặp ô nước kề ngang lệch mức ≤ 1 (nguồn tính 5).
   - `TrenchAtSourceLevelFillsWhenEnclosed`: `PondWorld()` (ao x 4–7, z 4–7, y = 3); đào rãnh 1 ô x = 8, z = 5 rồi phòng rộng x 9–13, z 3–7 ở y = 3. `Settle` → mọi ô đào là nước mức 5.
   - `OpenFloorAtSourceLevelStillDecreases`: chạy lại kịch bản `DiamondLevelsOnOpenFloor` với `BasinFillRadius = 16` mặc định — khẳng định như test đó (đã có; chỉ cần chắc test cũ vẫn đạt, không cần test mới nếu trùng → ghi trong Kết quả).
   - `OpeningEnclosureLowersLevels`: `PondWorld()`; trước khi tạo editor, `w.Set((15,3,z), Cell.Make(BlockMaterial.Empty))` với z 0–15 (dải trống x = 15, ngăn với phòng bởi đá x = 14). `e.BasinFillRadius = 6`. Đào rãnh `(8,3,5)` + phòng x 9–13, z 3–7 như test trên, `Settle` → mọi ô đào mức 5 (phòng kín, xa nhất Chebyshev 6 từ ao). Rồi `Dig(14,3,5)` (phòng thông ra dải x = 15, có ô cách ao > 6 → hở), `Settle` → mọi cặp nước kề ngang lệch ≤ 1 mức; `(10,3,5)` mức < 5; `HasPendingWater` false.
   - `LosingWallLowersBoostedCell`: `Corridor()` (tường z = 7 và 9), `Dig(1,6,8)`, `Settle` → x 1–15 mức 5 (như `ChannelFlowsFullLength`). `BasinFillRadius = 2`. Đào tường `(8,6,9)` và `(8,6,7)` (mở hai bên) rồi `Settle` → `(8,6,8)` vẫn là nước, mọi cặp nước kề ngang lệch ≤ 1, và kết quả bằng kết quả khi các ô đó được đào trước khi nước chảy tới (thế giới mới, đào trước, `Dig(1,6,8)`, `Settle`, so tầng y = 6).
8. Chạy toàn bộ `WaterFlowTests` + `SeaFloodTests`. Test nào khác ngoài `WideHoleLowerLayerFills` phải đổi kỳ vọng → **dừng, cần Opus** (ghi assertion fail, giá trị mong/đã thấy).

## Tiêu chí xong
- 5 test mới (hoặc 4 + ghi chú test trùng) và `WideHoleBothLayersFill` đạt; mọi test cũ trong `WaterFlowTests`, `SeaFloodTests` đạt không đổi kỳ vọng.
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
- Test cũ nào (ngoài `WideHoleLowerLayerFills`) phải đổi kỳ vọng.
- Cùng một lỗi sửa 2 lần vẫn chưa qua.
- Phải sửa file ngoài "Phạm vi".

---

## Kết quả

## Kết quả test

## Vướng mắc

---

## Review
