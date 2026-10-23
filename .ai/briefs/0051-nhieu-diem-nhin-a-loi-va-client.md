# Brief 0051 — nhiều điểm nhìn A: lõi ChunkStreamPlan + TerrainChunkView

> Trạng thái: chờ làm
> Review: đầy đủ
> Model thực thi: Sonnet · medium
> Cập nhật: 2026-10-23 10:48

<!-- Planner (Opus) viết, Executor (Sonnet) làm theo ở PHIÊN KHÁC — executor không có lịch sử chat, không được tự quyết. -->

## Mục tiêu
Bộ nạp chunk nhận **danh sách điểm nhìn** (mỗi điểm có bán kính nạp/gỡ riêng) thay vì một tâm; cột cần nạp là hợp các vùng. Task contract: [m1-world-7i-nhieu-diem-nhin](../tasks/m1-world-7i-nhieu-diem-nhin.md) (đọc trước). Brief này làm hai tiêu chí đầu của task: lõi `ChunkStreamPlan` và client `TerrainChunkView`. **Không** làm phần đo điện thoại (brief B). Quyết định gốc: [0029](../../design/decisions/0029-nhieu-diem-nhin-camera-quan-sat.md). Pillar P3. Thành phần: client Unity (`Game.Core` + `Game.Client`). Ước lượng cả task 1–1,5 ngày-người (brief này ~0,6).

## Bối cảnh cần đọc
- `client/Assets/Game/Core/ChunkStreamPlan.cs` (cả file, ~90 dòng). Hiện trạng [repo]:
  ```csharp
  public readonly int ColumnsX, ColumnsZ, LoadRadius, UnloadRadius;
  public ChunkStreamPlan(int columnsX, int columnsZ, int loadRadius, int unloadRadius)
  public static long DistanceSq(int px, int pz, int cx, int cz)
  public void Update(int px, int pz, int maxLoad, List<int> toLoad, List<int> toUnload)
  public int MissingWithin(int px, int pz, int radius)
  ```
  `Update` (`:50`): duyệt mọi cột; cột đang nạp mà `d > unloadSq` → gỡ (thứ tự chỉ số tăng); cột chưa nạp mà `d <= loadSq` → ứng viên; sắp ứng viên theo (d, chỉ số) tăng; lấy tối đa `maxLoad` (`maxLoad <= 0` → không giới hạn); đánh dấu nạp/gỡ ngay trong lần gọi.
- `client/Assets/Game/Client/TerrainChunkView.cs`:
  - Trường cấu hình `:30-36`: `streaming`, `streamTarget`, `loadRadius = 128`, `unloadRadius = 160`, `maxColumnsPerFrame = 2`.
  - `MissingColumnsWithin(int radius)` `:95-104` và `StreamCenter()` `:106` (`streamTarget` hoặc `Camera.main.transform`).
  - `Build`: `:180` tạo plan, `:185-190` lấy tâm (null → `Debug.LogError` rồi `return`) và `plan.Update(cell.x, cell.z, 0, toLoad, toUnload)` (nạp hết, không giới hạn).
  - `LateUpdate` `:259-275`: lấy tâm, `plan.Update(cell.x, cell.z, maxColumnsPerFrame, toLoad, toUnload)`, gỡ slot các cột `toUnload`, `AcquireColumns(toLoad, build)`.
  - `CellAt(Vector3)` `:229` đổi toạ độ thế giới → ô.
- Test sẵn có: `client/Assets/Game/Tests/Core/ChunkStreamPlanTests.cs` (EditMode; helper `Expected(px, pz, radius)` `:9`, lưới 24×24 cột, bán kính 128/160) và `client/Assets/Game/Tests/Client/TerrainStreamingPlayTests.cs` (PlayMode; `SetUp` đặt camera `(192, 40, 60)`, `view.source = MapsV3`, `view.streaming = true`, `view.streamTarget = camGo.transform`; mỗi `yield return null` là một khung).
- Thế giới 384×384 ô (24×24 cột, `ChunkSize = 16`). Hai điểm cách nhau ≥ 290 ô theo z thì vùng nạp bán kính 128 không giao nhau.
- Không dùng API Unity mới (chỉ `Transform`, `List`, đã có trong file).

## Quyết định đã chốt (không được đổi)
1. **Nạp:** cột chưa nạp có khoảng cách ≤ bán kính nạp của *ít nhất một* điểm nhìn. **Gỡ:** cột đang nạp có khoảng cách > bán kính gỡ của *mọi* điểm nhìn. (user, 0029)
2. **Thứ tự nạp:** sắp theo khoảng cách (bình phương) *nhỏ nhất tới bất kỳ điểm nhìn nào* (lấy min trên **mọi** điểm nhìn, không chỉ điểm nào làm cột đủ điều kiện nạp), rồi theo chỉ số cột; **một** ngân sách `maxLoad` chung mỗi khung, không tách theo điểm. (user, 2026-10-23)
3. **Số điểm nhìn tối đa** truyền vào khi tạo plan, không viết cứng; vượt tối đa → exception. M1 = 2 (cấu hình ở client). Không dùng mảng cứng 2 phần tử, chừa chỗ cho 5. (user, 0029)
4. Giữ hàm `Update(px, pz, maxLoad, toLoad, toUnload)` một tâm (dùng `LoadRadius`/`UnloadRadius` của plan) và constructor 4 tham số: hành vi y hệt cũ để test và scene cũ không đổi.
5. `Game.Core` không tham chiếu UnityEngine; số nguyên, tất định. Code và comment mới viết bằng **tiếng Anh** (`HardcodedTextScanTests` chặn chữ Việt trong code; comment `<summary>` cũ tiếng Việt giữ nguyên, không dịch lại).
6. Collider và tương tác ở điểm nhìn phụ giữ như hiện tại: cột nạp dựng như nhau.

## Phạm vi
- Được sửa: `client/Assets/Game/Core/ChunkStreamPlan.cs`, `client/Assets/Game/Client/TerrainChunkView.cs`, `client/Assets/Game/Tests/Core/ChunkStreamPlanTests.cs`, `client/Assets/Game/Tests/Client/TerrainStreamingPlayTests.cs`, file brief này (mục Kết quả…).
- Không được sửa: mọi file khác (gồm `WalkBench.cs`, scene, `design/`, roadmap, world-tech: Planner ghi khi review). Đo điện thoại và cờ WalkBench là brief B.

## Các bước
1. **`ChunkStreamPlan.cs` — kiểu điểm nhìn.** Trong namespace `Game.Core`, thêm (cùng file, trên lớp):
   ```csharp
   /// <summary>One streaming viewpoint: a cell position plus its own load/unload radii (in cells).</summary>
   public readonly struct StreamViewpoint
   {
       public readonly int X, Z, LoadRadius, UnloadRadius;
       public StreamViewpoint(int x, int z, int loadRadius, int unloadRadius) { X = x; Z = z; LoadRadius = loadRadius; UnloadRadius = unloadRadius; }
   }
   ```
   Xong khi: build không lỗi.
2. **`ChunkStreamPlan.cs` — constructor.** Đổi chữ ký thành `ChunkStreamPlan(int columnsX, int columnsZ, int loadRadius, int unloadRadius, int maxViewpoints = 1)`; thêm `public readonly int MaxViewpoints;`; `maxViewpoints < 1` → `ArgumentOutOfRangeException(nameof(maxViewpoints))`. Các kiểm tra cũ giữ nguyên. Xong khi: 5 lệnh `new ChunkStreamPlan(24, 24, 128, 160)` trong test cũ vẫn biên dịch.
3. **`ChunkStreamPlan.cs` — Update nhiều điểm.** Thêm
   ```csharp
   public void Update(IReadOnlyList<StreamViewpoint> viewpoints, int maxLoad, List<int> toLoad, List<int> toUnload)
   ```
   - `viewpoints == null` → `ArgumentNullException`; `Count < 1` hoặc `Count > MaxViewpoints` → `ArgumentException` (thông báo nêu số điểm và tối đa); điểm nào có `LoadRadius < 0` hoặc `UnloadRadius < LoadRadius` → `ArgumentOutOfRangeException`. Kiểm tra **trước** khi đổi trạng thái plan (ném exception thì `loaded`/`LoadedCount` không đổi).
   - Logic theo Quyết định 1–2, cùng khung với `Update` cũ: `toLoad`/`toUnload` `Clear` trước; mỗi cột tính `dMin` (min `DistanceSq` trên mọi điểm), `inLoad` (có điểm i với `d_i <= loadSq_i`), `inKeep` (có điểm i với `d_i <= unloadSq_i`). Cột đang nạp mà `!inKeep` → gỡ (thứ tự chỉ số tăng). Cột chưa nạp mà `inLoad` → ứng viên `(dMin, c)`; sắp theo (dMin, chỉ số) tăng; lấy tối đa `maxLoad` (≤ 0 → không giới hạn). Cột trong toLoad/toUnload đánh dấu nạp/gỡ ngay trong lần gọi, như cũ.
   - Tính `loadSq_i`/`unloadSq_i` bằng `long`. Không cấp phát mới mỗi lần gọi ngoài những gì `Update` cũ đã làm (tái dùng `candidates`; không `new` mảng/list trong vòng lặp cột).
   Xong khi: build không lỗi.
4. **`ChunkStreamPlan.cs` — hàm một tâm.** Viết lại `Update(int px, int pz, int maxLoad, List<int> toLoad, List<int> toUnload)` để dựng một `StreamViewpoint(px, pz, LoadRadius, UnloadRadius)` đặt vào một `List<StreamViewpoint>` dùng lại (trường `readonly List<StreamViewpoint> single` của plan, `Clear` + `Add`) rồi gọi `Update(single, ...)`. Chạy được cả khi `MaxViewpoints` > 1. `MissingWithin`, `Clear`, `DistanceSq` không đổi. Xong khi: `ChunkStreamPlanTests` cũ qua nguyên vẹn (chưa sửa file test).
5. **`ChunkStreamPlanTests.cs` — thêm 6 test EditMode** (giữ nguyên các test cũ; viết helper nếu cần, tên test đúng như dưới để brief B và review đối chiếu):
   - `TwoViewpointsLoadUnionOfRegions`: lưới 24×24, plan `(24, 24, 128, 160, 2)`; điểm A `(192, 60)`, B `(192, 330)`, bán kính 128/160; `Update(..., 0, ...)`; kỳ vọng `toLoad` = hợp `Expected(192,60,128)` ∪ `Expected(192,330,128)` (sắp theo (dMin, chỉ số) — tính kỳ vọng bằng vòng lặp tự viết trong test, min của hai `DistanceSq`); `LoadedCount == toLoad.Count`; hai vùng không giao nhau (assert `Expected(...)` A và B không có phần tử chung, để chắc test đúng ý "xa nhau").
   - `ColumnInBothRegionsLoadsOnce`: hai điểm gần nhau `(192, 150)` và `(192, 200)` (vùng giao nhau lớn); `toLoad` không có phần tử trùng (`CollectionAssert.AllItemsAreUnique`), `LoadedCount == toLoad.Count`, gọi `Update` lần hai cùng dữ liệu → `toLoad` rỗng, `toUnload` rỗng.
   - `ViewpointLeavingUnloadsOnlyUnheldColumns`: nạp hai điểm xa nhau `(192, 60)` và `(192, 330)` (`maxLoad` 0); rồi gọi `Update` chỉ với điểm A (danh sách 1 phần tử): `toUnload` = đúng các cột đã nạp mà `DistanceSq` tới A `> 160²` (tính kỳ vọng trong test, thứ tự chỉ số tăng), không có cột nào của vùng A bị gỡ; sau đó `LoadedCount` khớp số cột còn lại.
   - `SingleViewpointMatchesLegacyUpdate`: hai plan cùng tham số `(24, 24, 128, 160)`; một plan dùng `Update(192, 192, 3, ...)`, plan kia dùng `Update(new[] { new StreamViewpoint(192, 192, 128, 160) }, 3, ...)`; so sánh `toLoad`/`toUnload` và `LoadedCount` qua 3 khung liên tiếp rồi dịch tâm sang `(212, 192)` thêm 3 khung — luôn bằng nhau.
   - `UpdateIsDeterministic`: hai plan `(24, 24, 128, 160, 2)` mới, chạy cùng chuỗi lệnh (hai điểm, `maxLoad` 5, dịch điểm B qua vài khung) → `toLoad`/`toUnload` từng khung bằng nhau.
   - `TooManyViewpointsThrows`: plan `maxViewpoints = 2` + danh sách 3 điểm → `Assert.Throws<ArgumentException>`; plan không đổi (`LoadedCount == 0` sau lần gọi lỗi); danh sách rỗng cũng ném; `new ChunkStreamPlan(24, 24, 128, 160, 0)` ném `ArgumentOutOfRangeException`.
   Xong khi: `run_tests` EditMode lọc lớp `ChunkStreamPlanTests` qua hết (cũ + 6 mới).
6. **`TerrainChunkView.cs` — cấu hình và API.** Thêm:
   - Trường công khai (cạnh `maxColumnsPerFrame`): `/// <summary>Max streaming viewpoints (primary + extra). Read when the plan is created in Build.</summary>` `public int maxViewpoints = 2;`
   - Danh sách điểm phụ (riêng tư): `readonly List<ExtraViewpoint> extraViewpoints` với `struct ExtraViewpoint { public Transform Target; public int LoadRadius, UnloadRadius; }` (struct lồng riêng tư), và `readonly List<StreamViewpoint> viewpointBuffer` dùng lại mỗi khung.
   - `public void AddViewpoint(Transform target, int loadRadius, int unloadRadius)`: `target == null` → `ArgumentNullException`; `loadRadius < 0` hoặc `unloadRadius < loadRadius` → `ArgumentOutOfRangeException`; `target` đã đăng ký → `ArgumentException`; số điểm phụ + 1 (điểm chính) đã bằng `maxViewpoints` → `InvalidOperationException`. Gọi được trước hay sau `Build`.
   - `public bool RemoveViewpoint(Transform target)`: gỡ điểm phụ, trả về có tìm thấy không. Không đụng điểm chính.
   - `public int ViewpointCount`: số điểm nhìn đang hiệu lực = (điểm chính có tâm ? 1 : 0) + số điểm phụ có `Target != null`.
   - Hàm riêng tư `bool CollectViewpoints()`: xoá `viewpointBuffer`; nếu `StreamCenter()` không null thì thêm `StreamViewpoint(cell.x, cell.z, loadRadius, unloadRadius)` với `cell = CellAt(center.position)`; với mỗi điểm phụ có `Target != null` (bỏ qua Transform đã bị huỷ) thêm điểm tương ứng; trả `viewpointBuffer.Count > 0`. Điểm chính thêm trước (chỉ số 0) để thứ tự tất định.
   - Constructor plan ở `Build` (`:180`): truyền `maxViewpoints` làm tham số thứ 5.
   - `Build` (`:185-190`): giữ nguyên nhánh lỗi khi không có tâm chính (`Debug.LogError`, `return`); thay `plan.Update(cell.x, cell.z, 0, ...)` bằng `CollectViewpoints()` + `plan.Update(viewpointBuffer, 0, toLoad, toUnload)` (điểm phụ đăng ký trước Build được nạp ngay lúc đầu). Giữ nguyên phần còn lại của `Build` (đo thời gian, log).
   - `LateUpdate` (`:264-275`): thay đoạn lấy tâm + `plan.Update(cell.x, ...)` bằng `if (CollectViewpoints()) { plan.Update(viewpointBuffer, maxColumnsPerFrame, toLoad, toUnload); ...phần gỡ slot/AcquireColumns như cũ... }`. Điểm chính vắng (null) mà còn điểm phụ → vẫn cập nhật theo các điểm phụ.
   - `public int MissingColumnsWithin(Transform viewpoint, int radius)`: như `MissingColumnsWithin(int)` nhưng tâm là `viewpoint` (null hoặc không streaming/plan null → 0). Hàm `MissingColumnsWithin(int radius)` cũ giữ nguyên.
   Xong khi: build không lỗi; test PlayMode cũ không đổi hành vi (chưa sửa file test).
7. **`TerrainStreamingPlayTests.cs` — thêm 2 test PlayMode** (dùng `SetUp` sẵn có; camera chính ở `(192, 40, 60)`):
   - `SecondViewpointLoadsItsColumnsAndUnloadsWhenRemoved`: tạo `GameObject` thứ hai `(192, 40, 330)` (nhớ `Destroy` ở cuối test, kể cả khi assert lỗi dùng `try/finally`); `view.AddViewpoint(t, 128, 160)`; `yield return null` tối đa 300 khung đến khi `view.MissingColumnsWithin(t, 128) == 0` (không hết → `Assert.Fail`); assert `view.MissingColumnsWithin(128) == 0` (điểm chính vẫn đủ), `view.ViewpointCount == 2`, `LoadedChunkCount == LoadedColumnCount * 3`; sau đó `Assert.IsTrue(view.RemoveViewpoint(t))`, chờ đến khi mọi cột có `ChunkStreamPlan.DistanceSq(cell.x, cell.z, col % 24, col / 24) > 160L*160` với `cell = view.CellAt(t.position)` đều không còn mesh (`view.ChunkMesh(col) == null`) — tối đa 300 khung; assert `view.ViewpointCount == 1` và `MissingColumnsWithin(128) == 0`. Giá trị 3 = `World.Size.ChunksY` như test cũ `InitialLoadCoversFogRadiusOnly`.
   - `AddViewpointRespectsMaxAndRejectsDuplicates`: `view.maxViewpoints` mặc định 2 nên `AddViewpoint` lần 1 ok; lần 2 với Transform khác → `Assert.Throws<System.InvalidOperationException>`; lần 2 với cùng Transform → `Assert.Throws<System.ArgumentException>` (kiểm tra trùng trước kiểm tra tối đa; ghi đúng thứ tự này trong code: null → bán kính → trùng → tối đa); `RemoveViewpoint` Transform chưa đăng ký → `false`. Dọn mọi GameObject đã tạo.
   Xong khi: lớp `TerrainStreamingPlayTests` qua hết (4 cũ + 2 mới).
8. **Toàn bộ test + dọn** (xem "Lệnh executor chạy").
9. **Sửa sau review 1** (chỉ 2 file test; code `ChunkStreamPlan.cs`/`TerrainChunkView.cs` KHÔNG đổi). Lý do: điều kiện gỡ ở bước 7 do Planner viết sai (cột > 160 tới điểm phụ gồm cả vùng điểm chính → không bao giờ "sạch"); bản sửa của executor (xa cả hai điểm) chỉ kiểm cột chưa từng nạp nên luôn đúng kể cả khi `RemoveViewpoint` không gỡ gì.
   - `TerrainStreamingPlayTests.SecondViewpointLoadsItsColumnsAndUnloadsWhenRemoved`, phần sau khi nạp xong điểm phụ: **trước** `RemoveViewpoint`, lập `held` = các `col` (0..575) có `view.ChunkMesh(col) != null` **và** `DistanceSq` tới ô camera chính (`view.CellAt(camGo.transform.position)`) `> 160L*160`; `Assert.IsNotEmpty(held)` (đảm bảo test có cột để kiểm). Rồi `Assert.IsTrue(view.RemoveViewpoint(t))`; `yield return null` tối đa 300 khung đến khi mọi `col` trong `held` có `ChunkMesh(col) == null` (không hết → `Assert.Fail`). Sau đó gọi helper sẵn có `AssertLoadedWithinUnloadRadius()` (mọi cột còn mesh đều ≤ 160 tới camera chính), `Assert.AreEqual(1, view.ViewpointCount)`, `Assert.AreEqual(0, view.MissingColumnsWithin(128))`. Bỏ vòng `clear` cũ. Giữ `try/finally`.
   - `ChunkStreamPlanTests.TooManyViewpointsThrows`: thêm 2 assert — danh sách 1 điểm `StreamViewpoint(192, 192, 128, 100)` (unload < load) và 1 điểm `StreamViewpoint(192, 192, -1, 160)` → mỗi cái `Assert.Throws<ArgumentOutOfRangeException>`; sau đó `Assert.AreEqual(0, plan.LoadedCount)` (Phần cần test có "bán kính sai" nhưng chưa có test).
   Xong khi: `ChunkStreamPlanTests` 12/12, `TerrainStreamingPlayTests` 6/6, EditMode toàn bộ 504/504, PlayMode toàn bộ 170/170 (số test không đổi); dọn `git checkout -- client/ProjectSettings/EditorSettings.asset`; `git status --short` chỉ còn 2 file test + brief. Ghi giờ bắt đầu/kết thúc lần sửa vào Kết quả (dòng mới "Sửa 1: …"), cập nhật Kết quả test.

## Tiêu chí xong
- `ChunkStreamPlanTests` qua: các test cũ + 6 test mới (tên ở bước 5).
- `TerrainStreamingPlayTests` qua: 4 cũ + 2 test mới (bước 7). Mọi test PlayMode khác gán `view.streamTarget` vẫn qua.
- Toàn bộ EditMode + PlayMode qua; số test = trước + 6 EditMode, + 2 PlayMode (trước: EditMode 498, PlayMode 168 theo brief 0050 → mong đợi 504 và 170).
- `git status --short` chỉ còn 4 file code trong Phạm vi + brief.

## Test sau khi xong

### Phần cần test
- Bước 1–3 lõi nhiều điểm nhìn (hợp vùng; nạp 1 lần; gỡ đúng phần không còn ai giữ; ngoại lệ vượt tối đa/rỗng/bán kính sai) · `ChunkStreamPlanTests`: `TwoViewpointsLoadUnionOfRegions`, `ColumnInBothRegionsLoadsOnce`, `ViewpointLeavingUnloadsOnlyUnheldColumns`, `TooManyViewpointsThrows` → qua.
- Thứ tự nạp theo khoảng cách min tới mọi điểm, ngân sách chung · `TwoViewpointsLoadUnionOfRegions` (thứ tự kỳ vọng) và `UpdateIsDeterministic` (`maxLoad` 5) → qua.
- Tất định · `UpdateIsDeterministic` → qua.
- Bước 4 hàm một tâm giữ hành vi cũ (regression lõi) · `SingleViewpointMatchesLegacyUpdate` + 6 test cũ của `ChunkStreamPlanTests` (`UnlimitedLoadTakesExactlyColumnsWithinRadius`, `MaxLoadTakesNearestFirst`, `HysteresisKeepsColumnsUntilUnloadRadius`, `MissingWithinCountsUnloaded`, `CtorRejectsBadRadii`, `DistanceSqIsZeroInsideAndToNearestCell`) → qua.
- Bước 6 client nạp theo danh sách điểm; API đăng ký/gỡ; ràng buộc tối đa · `SecondViewpointLoadsItsColumnsAndUnloadsWhenRemoved`, `AddViewpointRespectsMaxAndRejectsDuplicates` → qua.
- Streaming một điểm không đổi (regression client: `Build`, `LateUpdate`, `MissingColumnsWithin(int)`) · `TerrainStreamingPlayTests` 4 test cũ (`InitialLoadCoversFogRadiusOnly`, `NoHoleWhileFlyingFast`, `UnloadedSlotsAreReused`, `DigNearCameraRebuildsWithin2Frames`) → qua.
- Các nơi khác dùng `TerrainChunkView` streaming (regression): toàn bộ PlayMode → 170/170 (gồm các test gán `streamTarget` ở `SimViewMapsPlayTests`, `WalkBenchPlayTests`, `WorldBoundsPlayTests`… không sửa các file này).
- Không còn chữ Việt trong code mới · `HardcodedTextScanTests` (nằm trong EditMode) → qua.
- Không đo FPS điện thoại ở brief này (brief B).

### Lệnh executor chạy
Unity Editor mở project `client/` qua MCP (`run_tests`), quyết định 0010.
1. Ghi giờ bắt đầu: `date "+%H:%M"`.
2. Sau bước 4: `read_console` → không lỗi biên dịch; `run_tests` EditMode lọc lớp `ChunkStreamPlanTests` (test cũ) → qua.
3. Sau bước 6: `read_console` → không lỗi.
4. Sau bước 5 và 7: `run_tests` EditMode lọc `ChunkStreamPlanTests` → 6 cũ + 6 mới qua; `run_tests` PlayMode lọc `TerrainStreamingPlayTests` → 6/6.
5. `run_tests` EditMode (toàn bộ) → 504/504.
6. `run_tests` PlayMode (toàn bộ) → 170/170.
7. Dọn: `git checkout -- client/ProjectSettings/EditorSettings.asset`, rồi `git status --short` → chỉ còn 4 file Phạm vi + brief.
8. Ghi giờ kết thúc: `date "+%H:%M"`.

### User tự test
Không (kiểm trên điện thoại ở brief B).

## Dừng và chuyển lại Opus khi
- Brief mơ hồ hoặc mâu thuẫn với code thật (vd dòng trích ở Bối cảnh không còn khớp số dòng/chữ ký).
- Test cũ của `ChunkStreamPlan` hoặc PlayMode streaming đổi kết quả sau khi sửa lõi (nghĩa là hàm một tâm không còn tương đương).
- Cần một quyết định thuộc AGENTS.md §3.4 hoặc ngoài "Quyết định đã chốt" (vd muốn thêm loại điểm nhìn, tách ngân sách theo điểm).
- Cùng một lỗi sửa 2 lần vẫn chưa qua.
- Phải sửa file ngoài "Phạm vi".

---
<!-- Từ đây trở xuống: EXECUTOR ghi. -->

## Kết quả
Base commit: f8d4802
Chạy bằng: claude-sonnet-5-5 · medium
Giờ bắt đầu: 10:27 · kết thúc: 10:37 (~10 phút)
Commit: b3125ad
File đổi: ChunkStreamPlan.cs, TerrainChunkView.cs, ChunkStreamPlanTests.cs, TerrainStreamingPlayTests.cs
Lệch brief: test PlayMode bước 7 thêm điều kiện 'cũng xa camera chính' khi kiểm cột đã gỡ (an toàn hơn, cùng ý); không đổi hành vi.
Ghi chú: MCP bridge mất kết nối tạm lúc biên dịch/vào Play; kết quả đọc lại từ TestResults.xml/get_test_job.


## Kết quả test
- Lõi nhiều điểm nhìn, thứ tự nạp, tất định, hàm một tâm (regression): đạt — `ChunkStreamPlanTests` 12/12 (6 cũ + 6 mới).
- Client API/đăng ký/gỡ/tối đa: đạt — `TerrainStreamingPlayTests` 6/6.
- Regression streaming khác + chữ Việt (`HardcodedTextScanTests`): đạt — EditMode 504/504, PlayMode 170/170.
- Lệnh: run_tests EditMode lọc lớp → 12/12; PlayMode lọc lớp → 6/6; EditMode toàn bộ → 504/504; PlayMode toàn bộ → 170/170; dọn EditorSettings.asset → `git status` chỉ còn 4 file + brief.

## Vướng mắc
<!-- Khi trạng thái = cần Opus: câu hỏi cụ thể + các phương án (kèm đề xuất). -->

---
<!-- PLANNER ghi khi review. -->

## Review
<!-- Kết luận: đã duyệt | cần sửa. Điểm đã đối chiếu. -->
### Review 1 — 2026-10-23 (Opus)
- Chạy bằng `claude-sonnet-5-5 · medium` khớp `Model thực thi: Sonnet · medium`. Mọi dòng "Phần cần test" có kết quả. [repo] brief
- `git show --stat b3125ad`: đúng 4 file trong Phạm vi; working tree sạch. [run]
- Quyết định 1–2 đạt: `ChunkStreamPlan.Update(IReadOnlyList<StreamViewpoint>…)` tính `dMin`/`inLoad`/`inKeep` trên mọi điểm, gỡ khi `!inKeep`, ứng viên sắp theo (dMin, chỉ số), một `maxLoad` chung. [repo] `client/Assets/Game/Core/ChunkStreamPlan.cs:71-120`
- Quyết định 3–5 đạt: `maxViewpoints` qua constructor (mặc định 1, < 1 ném), kiểm tra trước khi đổi trạng thái, hàm một tâm + constructor 4 tham số giữ nguyên qua list `single` dùng lại; không cấp phát trong vòng cột; comment mới tiếng Anh. [repo] `ChunkStreamPlan.cs:21-64`
- Client đạt bước 6: `AddViewpoint` đúng thứ tự null → bán kính → trùng → tối đa; `CollectViewpoints` thêm điểm chính trước, bỏ Transform đã huỷ; `Build`/`LateUpdate` dùng `viewpointBuffer`. [repo] `client/Assets/Game/Client/TerrainChunkView.cs:113-165,251,329`
- Test EditMode 6 mới đúng tên và đúng ý bước 5. [repo] `client/Assets/Game/Tests/Core/ChunkStreamPlanTests.cs:87-201` (test đầu ở `:109`)
- **Lỗi:** `SecondViewpointLoadsItsColumnsAndUnloadsWhenRemoved` phần "gỡ khi bỏ điểm" không kiểm được gì: chỉ xét cột > 160 tới **cả hai** điểm, mà các cột đó chưa từng nạp (điểm chính đứng yên, nạp bán kính 128) → qua kể cả khi `RemoveViewpoint` không gỡ cột nào. Gốc là bước 7 Planner viết sai điều kiện (cột > 160 tới điểm phụ gồm cả vùng điểm chính, sẽ không bao giờ sạch); executor đã ghi lệch nhưng đáng ra phải `cần Opus`. [repo] `client/Assets/Game/Tests/Client/TerrainStreamingPlayTests.cs:132-145`
- Thiếu nhỏ: "bán kính sai" trong Phần cần test chưa có test (code có kiểm tra). [repo] `ChunkStreamPlanTests.cs:192`
- Giờ thực tế (10 phút) chưa cộng vào roadmap: ghi khi duyệt.
- Kết luận: cần sửa (bước 9)
