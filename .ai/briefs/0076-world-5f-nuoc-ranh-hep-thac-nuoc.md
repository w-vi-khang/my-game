# Brief 0076 — World 5f: nước trong rãnh hẹp chảy hết, kiểm khối khi mất nguồn, thác nước demo

> Trạng thái: chờ user xem (code đã duyệt; thác hiện nhờ brief 0077)
> Review: đầy đủ + user xem
> Model thực thi: Sonnet · medium
> Cập nhật: 2026-10-25 — Planner review: code đạt, phát hiện thác không hiển thị

Task contract: [m1-world-nuoc-chay](../tasks/m1-world-nuoc-chay.md) (phần 5f). Quyết định: [0041](../../design/decisions/0041-nuoc-chay-trong-ranh-hep-va-thac-nuoc.md) (sửa mục 3 của [0040](../../design/decisions/0040-nuoc-chay-nguon-lan-gioi-han-m1.md)). Làm tiếp trên code của brief [0075](0075-world-5e-nuoc-chay.md) (commit `db2a3bc`) — đọc mục "Quyết định đã chốt" của 0075 để hiểu luật hiện có.

## Mục tiêu
- Trong rãnh rộng 1 ô, nước chảy hết rãnh, không mất mức. Chỉ ở chỗ rộng mới giảm mức và dừng sau N ô (như 0040).
- Khối nước đang chảy mất đường về nguồn thì rút hết, kể cả trong rãnh (rãnh không mất mức nên không tự rút được).
- Nước rơi xuống mặt nước nguồn (ao, hồ, biển) thì nhập vào, không tràn lên bờ.
- Có thác nước demo cạnh ao trong SimView: nước từ khối đất cao chảy qua rãnh, rơi thẳng xuống ao.

## Bối cảnh cần đọc
- `client/Assets/Game/Core/Cell.cs`:
  - `FlowingFlag = 1 << 13` (:31), `FallingFlag = 1 << 14` (:32), `ReservedShift = 13` (:33, chỉ test cũ dùng `Get/SetReserved` — không đụng);
  - `MakeFlowing(ushort cell, int level, bool falling)` (:83-90) — hiện **giữ** các bit khác, gồm bit 15;
  - `ClearFlow` (:93), `ClearWater` (:96-100).
- `client/Assets/Game/Core/WorldEditor.cs` (sau 0075):
  - `flowMaxDistance` / `FlowMaxDistance` (:56-69); `HasPendingWater` (:71);
  - `TryDig` gọi `AddFlowDependents` (:112); `TryPlace` (:140-155) gọi `AddFlowDependents` (:153);
  - `StepWater() => StepSea() + StepFlow()` (:298); `AddFlow`, `AddFlowRing`, `AddFlowDependents` (:300-323);
  - `IsWaterCell` (:326), `IsStandingWater` (:330), `Emit` (:334-354);
  - `StepFlow` (:357-439): ảnh chụp hàng chờ (:361), tính (:366-404, nhánh QĐ 7a ở :372-381, nhánh rơi :382-385, nhánh lan ngang :386-401), ghi (:406-414), rơi ngay (:415-424), hậu xử lý từng ô đổi (:426-438).
- `client/Assets/Game/Core/WaterConfig.cs` (`flowMaxDistance`, `stepsPerSecond`, `Validate`); `design/data/water_m1.json` + bản sao `client/Assets/Game/Client/Resources/Config/water_m1.json`; `client/Assets/Game/Client/TerrainChunkView.cs:228-230` (gán cấu hình nước sau khi tạo editor).
- Test của 0075: `client/Assets/Game/Tests/Core/WaterFlowTests.cs`:
  - `Flat` (:10), `Editor` (:23), `Settle` (:30), `Dig` / `Place` (:36-40), `PondWorld` (:44, ao nguồn y = 3, x, z ∈ 4..7, nền đá y 1..3), `Corridor` (:54, hành lang rộng 1 ô ở y = 6, z = 8, tường z = 7 và 9, nguồn (0, 6, 8), đá (1, 6, 8));
  - các test dùng `Corridor` mong nước dừng sau 7 ô — luật mới làm sai (xem bước 3).
  - `client/Assets/Game/Tests/Client/WaterFlowPlayTests.cs`: SetUp SimView; `DiggingSandNextToPondFillsHole` (:43-59); `NoFloodOnPlainWithoutEdits` (:62-67, chờ 30 khung rồi mong `HasPendingWater == false`).
- Demo: `client/Assets/Game/Core/Sim/SimDemo.cs`: `PlainPondMin = (10, 0, −12)`, `PondSize = 4` (:68-69), `PlacePond` (:75-86; ao ở `y = plain.Ground`, x ∈ [cx + 10, cx + 13], z ∈ [cz − 12, cz − 9], viền cát x ∈ [cx + 9, cx + 14], z ∈ [cz − 13, cz − 8]; `cx = plain.X0 + plain.Size / 2`, `cz = plain.Z0 + plain.Size / 2`). `CellPos` (`Core/Sim/SimData.cs:6`, field `X, Y, Z`).
- `client/Assets/Game/Client/SimView.cs`: `OnAfterGenerate` (:113-…; gọi `SimDemo.PlacePond` ở :117 khi `!stressBench && habitat`); `Setup`, nhánh `habitat` (:214-228), có `view.Editor`.
- Môi trường sống: L3 đếm ô **đá** lộ thiên, L1 đếm cỏ, L2 cỏ cạnh nước (`design/data/habitat_m1.json`). Khối thác dùng **đất** nên không đổi số L3.

## Quyết định đã chốt (không được đổi)
1. **Cờ đang rút** — `Cell.cs`:
   ```csharp
   const ushort DrainingFlag = 1 << 15; // nước đang rút — 0041
   public static bool IsDraining(ushort cell);
   public static ushort SetDraining(ushort cell, bool value);
   ```
   - `MakeFlowing` thêm tham số cuối `bool draining = false` và ghi đúng bit 15 theo tham số (bật/tắt). Mọi chỗ gọi hiện có giữ nguyên → cờ rút bị xoá khi ô được tính lại có nguồn nuôi.
   - `ClearFlow` và `ClearWater` xoá cả bit 15.
   - Sửa comment bố cục ô: `bit 13 nước đang chảy · bit 14 nước đang rơi · bit 15 nước đang rút`.
2. **Tham số mới** trong `WorldEditor` (cùng kiểu với `FlowMaxDistance`):
   - `public int NarrowMaxOpenSides { get; set; } = 2;` — hợp lệ 0–3, ngoài khoảng ném `ArgumentOutOfRangeException`;
   - `public int SupportCheckMaxCells { get; set; } = 4096;` — hợp lệ 64–65536, ngoài khoảng ném.
   - `WaterConfig` thêm `public int narrowMaxOpenSides = 2;` và `public int supportCheckMaxCells = 4096;` + kiểm cùng khoảng trong `Validate`. JSON (2 file, byte-giống):
     ```json
     {
         "configVersion": 1,
         "flowMaxDistance": 7,
         "stepsPerSecond": 8,
         "narrowMaxOpenSides": 2,
         "supportCheckMaxCells": 4096
     }
     ```
   - `TerrainChunkView` gán thêm `Editor.NarrowMaxOpenSides`, `Editor.SupportCheckMaxCells` cạnh dòng gán `FlowMaxDistance`.
3. **Ô hẹp:** ô (x, y, z) hẹp khi số ô trong 4 ô ngang (x ± 1, z ± 1) **không** đặc (`!IsSolidAt`; ngoài map theo x/z tính là không đặc) ≤ `NarrowMaxOpenSides`.
4. **`Emit` mới** (chỉ đổi đúng hai chỗ, phần "đứng được" giữ nguyên):
   - Mức của ô nước đang chảy ngang (không phải nguồn, không rơi) mực `s`: `s` nếu ô **hẹp và không có cờ rút**; ngược lại `s − 1`. Nguồn và ô rơi vẫn `N`.
   - **Nhập vào nguồn:** ô đang rơi mà ô dưới là **nguồn** (`Cell.IsWaterSource`) → trả 0 (không lan). Ô ngang (không rơi) nằm trên nguồn vẫn đứng được như cũ.
   - `Emit` cần cho biết feeder có cờ rút không: đổi thành `int Emit(int x, int y, int z, out bool fromDraining)` (`fromDraining` = ô (x, y, z) có cờ rút; nguồn và ô rơi luôn false).
5. **Nhánh lan ngang trong `StepFlow`** (:386-401): `m` = max `Emit` như cũ. Kết quả ô X:
   - `m ≥ 1` → `MakeFlowing(old, m, false, draining)` với `draining = true` **chỉ khi** mọi ô ngang có `Emit ≥ 1` đều `fromDraining`; có ít nhất một feeder không rút → `draining = false`.
   - Nhánh rơi (ô trên là nước) → `MakeFlowing(old, N, true)` (không rút). Nhánh QĐ 7a giữ nguyên.
   - So sánh `target != old` giữ nguyên (đổi cờ rút cũng là đổi, nhưng không phát `CellChanged` vì vật liệu không đổi).
6. **Ô phụ thuộc khi đổi đặc ↔ không đặc:** `TryDig` và `TryPlace` (sau `AddFlowDependents`) gọi thêm `AddFlowRing2(x, y, z)` = 8 ô cùng tầng có `|dx| + |dz| == 2` (ô cách 2 theo trục và 4 ô chéo). Lý do: độ hẹp của ô kề X phụ thuộc các ô này.
7. **Kiểm khối khi mất nguồn** (0041):
   - Hàng chờ mới `supportStarts` (List + HashSet, thứ tự chèn cố định). Ô (x, y, z) **từ nước thành không phải nước** — trong `TryPlace` (ô cũ là nước) và trong hậu xử lý `StepFlow` (:426-438, nhánh `oldMat != newMat` khi `newMat != Water`) → thêm 6 ô kề của nó vào `supportStarts`.
   - `HasPendingWater` thêm `|| supportStarts.Count > 0`.
   - **Đầu `StepFlow`** (trước khi chụp `flowQueue`): xử lý `supportStarts` (chụp rồi xoá). Dùng một `HashSet` `visited` chung cho cả lượt. Với mỗi ô bắt đầu trong map, là nước đang chảy (`Cell.IsFlowing`), chưa trong `visited`:
     - BFS 6 hướng (thứ tự `Faces` :180) qua các ô **nước đang chảy** (`IsFlowing`, kể cả ô rơi, ô đang rút); thêm vào `visited`; gom danh sách khối.
     - **Còn nguồn** nếu một ô trong khối có nguồn (`Cell.IsWaterSource`, trong map) ở **ô trên** hoặc **một trong 4 ô ngang**; hoặc ô ngoài map theo x/z ở y ≤ `SeaLevel` (khi `SeaLevel >= 0`) ở 4 ô ngang. Gặp là dừng BFS, coi khối còn nguồn.
     - Số ô khối vượt `SupportCheckMaxCells` → dừng, coi còn nguồn (không làm gì).
     - Khối không còn nguồn → mỗi ô: `World.Set(.., Cell.SetDraining(cell, true))` (không phát `CellChanged`) + `AddFlowDependents`.
   - Không quét theo nhịp; chỉ chạy khi có ô bắt đầu.
8. **Hàm mới khác:** `public void WakeWater(int x, int y, int z) => AddFlowDependents(x, y, z);` — đưa ô vào hàng chờ (dùng cho thác demo có sẵn khi sinh world).
9. **Thác nước demo** — `SimDemo` (Game.Core, ghi thẳng như `PlacePond`, không bật cờ đã sửa):
   ```csharp
   /// <summary>Thác nước demo cạnh ao (0041): khối đất, nguồn trên đỉnh, rãnh 1 ô ra mép nhô trên ao. Gọi sau PlacePond. Trả ô nguồn (toạ độ world) để gọi WorldEditor.WakeWater.</summary>
   public static CellPos PlaceWaterfall(WorldData world, StressPlain plain);
   ```
   Với `cx, cz` như `PlacePond`, `G = plain.Ground`, dùng `Cell.Make(BlockMaterial.Dirt)`:
   - Khối đất: x ∈ [cx + 15, cx + 18], z ∈ [cz − 12, cz − 10], y ∈ [G + 1, G + 5].
   - Nguồn: (cx + 17, G + 5, cz − 11) = `Cell.Make(BlockMaterial.Water, Cell.MaxWaterLevel)`.
   - Rãnh: (cx + 16, G + 5, cz − 11) và (cx + 15, G + 5, cz − 11) = `Empty`.
   - Mép nhô: đất ở (cx + 14, G + 4, cz − 11), (cx + 14, G + 5, cz − 12), (cx + 14, G + 5, cz − 10); (cx + 14, G + 5, cz − 11) để trống.
   - Nước đi: nguồn → rãnh → mép → ô (cx + 13, G + 5, cz − 11) nằm trên ao → rơi cột y = G + 4 … G + 1 → chạm mặt ao (nguồn) → nhập vào (QĐ 4).
   - `SimView`:
     - thêm `public CellPos? WaterfallSource { get; private set; }`;
     - `OnAfterGenerate`: ngay sau `SimDemo.PlacePond` (cùng điều kiện) → `WaterfallSource = SimDemo.PlaceWaterfall(world, plain);`;
     - `Setup`, đầu nhánh `habitat`: nếu `WaterfallSource` có giá trị → `view.Editor.WakeWater(s.X, s.Y, s.Z)`.
   - `habitat == false`, `stressBench`, đảo v2: không có thác (giữ hành vi cũ).
10. **Mã băm:** `GoldenHashTests`, `SimDemoTests`, `SimDemoMapsTests`, `Sim/**` qua không sửa số. Đổi số → dừng, cần Opus.

## Phạm vi
- Được sửa/tạo:
  - `client/Assets/Game/Core/Cell.cs`, `WorldEditor.cs`, `WaterConfig.cs`, `Core/Sim/SimDemo.cs` (chỉ thêm `PlaceWaterfall`);
  - `client/Assets/Game/Client/TerrainChunkView.cs` (chỉ dòng gán cấu hình nước), `SimView.cs` (chỉ QĐ 9);
  - `design/data/water_m1.json` + bản sao client;
  - test: `Tests/Client/SimViewMapsPlayTests.cs` (chỉ thêm bỏ qua vùng thác — Planner 2026-10-25), `Tests/Core/WaterFlowTests.cs`, `Tests/Core/WaterConfigTests.cs`, `Tests/Core/CellTests.cs`, `Tests/Client/WaterFlowPlayTests.cs`;
  - brief này (Kết quả trở xuống).
- Không được sửa: mọi file khác, kể cả `SeaFloodTests`, `HabitatDemoTests`, `HabitatPlayTests`, scene `.unity`, `design/*.md`.

## Các bước
1. **Lõi:** QĐ 1–8 trong `Cell`, `WaterConfig`, `WorldEditor`. Xong khi: biên dịch được, console 0 Error.
2. **Test cờ / cấu hình:** `CellTests` thêm `DrainingFlagRoundTrip` (`SetDraining` bật/tắt; `MakeFlowing(.., draining: true)` bật; `MakeFlowing` mặc định xoá; `ClearWater`/`ClearFlow` xoá). `WaterConfigTests`: default khớp file mới; `ValidateRejectsBadValues` thêm `narrowMaxOpenSides` −1 / 4, `supportCheckMaxCells` 63 / 65537.
3. **Sửa test 0075 theo luật mới** (`WaterFlowTests.cs`):
   - Thêm hàm dựng `OpenFloor()`: `Flat(5)` + nguồn (0, 6, 8) + đá (1, 6, 8), **không** tường (chỗ rộng).
   - `SpreadStopsAfterN`, `SpreadAdvancesOneCellPerTick`: đổi `Corridor()` → `OpenFloor()`; kỳ vọng giữ nguyên (dọc x: mực `8 − x`, x = 8 rỗng; N = 3 tương tự; mỗi nhịp tiến 1 ô).
   - `BlockingSourceDrains` → đổi thành `RemovingOpenSourceDrains`: `OpenFloor`, `Dig(1, 6, 8)`, `Settle`, `Place(0, 6, 8)` (đặt lên ô nguồn), `Settle` → không còn ô nước nào ở y = 6.
   - `DrainCountsDownGradually`: `OpenFloor`, như trên nhưng sau `Place(0, 6, 8)` chỉ `StepWater()` một lần → còn ≥ 1 ô nước ở (x, 6, 8), x = 1..7.
   - `PlacingBlockStopsFlowFront`: giữ `Corridor`; kỳ vọng giữ nguyên (x = 1..3 nước, x = 5..7 rỗng).
   - `CellChangedOnlyOnMaterialChange`: giữ `Corridor`; kỳ vọng đổi thành `1 + 15` (rãnh x = 1..15 đầy).
   - `FallsThenSpreadsFullAgain`: **bỏ** hai tường y = 2 (x = 0 và x = 2, z 3..15) để đáy là chỗ rộng. Đổi một kỳ vọng: (1, 11, 3) mực **7** (trước là 6), vì (1, 11, 2) có vành đá hai bên nên là ô hẹp, truyền nguyên mức. Các kỳ vọng khác giữ nguyên (cột rơi; (0, y, 3), (2, y, 3) rỗng với y = 4..10; (1, 2, 3 + d) nước với d = 1..7, d = 8 rỗng).
4. **Test luật mới** (`WaterFlowTests.cs`):
   - `ChannelFlowsFullLength`: `Corridor`, `Dig(1, 6, 8)`, `Settle` → (x, 6, 8) với x = 1..15 là nước, mực 7.
   - `ChannelOpensIntoWideArea`: như `Corridor` nhưng tường chỉ ở x = 0..5 → (x, 6, 8) x = 1..6 mực 7; x = 7..12 mực `13 − x`; (13, 6, 8) rỗng.
   - `BlockingChannelDrainsDownstream`: `Corridor` đã `Settle`, `Place(8, 6, 8)` → `Settle` → x = 9..15 rỗng; x = 1..7 vẫn nước mực 7.
   - `ChannelLoopDrainsWhenSourceRemoved`: `Flat(5)`; đá y = 6 khắp x, z ∈ 2..10; đào sẵn (ghi `Empty` bằng `w.Set`) vòng 16 ô: các ô (x, 6, z) với x, z ∈ 4..8 và (x ∈ {4, 8} hoặc z ∈ {4, 8}), trừ (4, 6, 4) để đá; nguồn `w.Set(4, 6, 3, Water 7)`. `Dig(4, 6, 4)`, `Settle` → 16 ô vòng là nước. `Place(4, 6, 3)` (lên nguồn), `Settle` → 16 ô vòng rỗng.
   - `WaterfallIntoPondDoesNotSpill`: `PondWorld`; đá x ∈ 9..12, z ∈ 4..6, y ∈ 4..8; ghi tay: nguồn (11, 8, 5), `Empty` ở (10, 8, 5); đá (9, 8, 5) (để đào khởi động); mép: đá (8, 7, 5), (8, 8, 4), (8, 8, 6). `Dig(9, 8, 5)`, `Settle` →
     - (7, y, 5) với y = 4..7 là nước đang rơi; (8, 8, 5) và (7, 8, 5) là nước;
     - không có nước ở (6, 4, 5), (7, 4, 4), (7, 4, 6), (8, 4, 5) và ở mọi ô y = 4 ngoài cột (7, 4, 5) trong x, z ∈ 0..15 (nước không tràn trên mặt ao);
     - 16 ô ao vẫn là nguồn.
   - Regression: `FlowIsDeterministic`, `FlowSettlesWithinBound`, `PitNextToPondFills`, `WideDeepPitFills`, `RemovingSourceDrains`, `SeaFloodStillMakesSources`, `FlowingWaterBelowSeaLevelDoesNotStartSeaFlood` qua không sửa.
5. **Demo + PlayMode:** QĐ 9 (`SimDemo.PlaceWaterfall`, `SimView`). `WaterFlowPlayTests.cs`:
   - `NoFloodOnPlainWithoutEdits` → đổi tên `WaterSettlesAfterStart`: chờ tối đa 5 giây tới khi `!view.Editor.HasPendingWater` → assert đã yên.
   - `WaterfallPoursIntoPond` (mới): chờ yên như trên; với `cx, cz, G` theo QĐ 9: `sv.WaterfallSource` có giá trị; (cx + 13, y, cz − 11) với y = G + 1..G + 4 là nước; (cx + 12, G + 1, cz − 11), (cx + 14, G + 1, cz − 11), (cx + 13, G + 1, cz − 12) rỗng; ô (cx + 10, G, cz − 12) vẫn là nước nguồn.
   - `DiggingSandNextToPondFillsHole` giữ nguyên.
6. Chạy cả bộ, dọn, chụp ảnh.

## Tiêu chí xong
- Mọi test mới/sửa qua; EditMode + PlayMode toàn bộ qua (trước brief: 634 / 274; ghi số mới).
- `SeaFloodTests`, `HabitatDemoTests`, `HabitatPlayTests` qua không sửa; mã băm không đổi số.
- Console 0 Error. `git diff --stat` chỉ có file trong Phạm vi.
- `grep -rn "UnityEngine" client/Assets/Game/Core/Cell.cs client/Assets/Game/Core/WorldEditor.cs client/Assets/Game/Core/WaterConfig.cs client/Assets/Game/Core/Sim/SimDemo.cs` → rỗng.

## Test sau khi xong

### Phần cần test
- Cờ rút + cấu hình (QĐ 1–2): `CellTests.DrainingFlagRoundTrip`, `WaterConfigTests`, `ConfigTests.WaterClientCopyMatchesDesignData`.
- Rãnh hẹp chảy hết, ra chỗ rộng mới giảm (QĐ 3–5): `ChannelFlowsFullLength`, `ChannelOpensIntoWideArea`, `SpreadStopsAfterN`, `SpreadAdvancesOneCellPerTick`, `CellChangedOnlyOnMaterialChange`.
- Ô phụ thuộc khi đào/đặt (QĐ 6): `ChannelOpensIntoWideArea`, `PlacingBlockStopsFlowFront`, `FlowIsDeterministic`.
- Kiểm khối + rút (QĐ 7): `BlockingChannelDrainsDownstream`, `ChannelLoopDrainsWhenSourceRemoved`, `RemovingOpenSourceDrains`, `DrainCountsDownGradually`, `RemovingSourceDrains`.
- Nhập vào nguồn + thác (QĐ 4, 8, 9): `WaterfallIntoPondDoesNotSpill`, `FallsThenSpreadsFullAgain`, PlayMode `WaterfallPoursIntoPond`, `WaterSettlesAfterStart`.
- Biển 0013: `SeaFloodTests`, `SeaFloodStillMakesSources`, `FlowingWaterBelowSeaLevelDoesNotStartSeaFlood`, PlayMode `TerrainEditPlayTests`, `DayCycleWaterPlayTests`.
- Demo + môi trường sống không đổi: `HabitatDemoTests`, `HabitatPlayTests`, `SimViewMapsPlayTests`, `HabitatTrackerTests`, `HabitatWorldTests`.
- Mã băm: `GoldenHashTests`, `SimDemoTests`, `SimDemoMapsTests`, `Sim/**`.
- EditMode + PlayMode toàn bộ.

### Lệnh executor chạy
1. MCP `refresh_unity` → `read_console` (Error) = 0.
2. MCP `run_tests` EditMode lọc: `WaterFlowTests`, `WaterConfigTests`, `CellTests`, `ConfigTests`, `SeaFloodTests`, `WorldEditorTests`, `HabitatTrackerTests`, `HabitatWorldTests`, `HabitatDemoTests`, `GoldenHashTests`, `SimDemoTests`, `SimDemoMapsTests`.
3. MCP `run_tests` PlayMode lọc: `WaterFlowPlayTests`, `HabitatPlayTests`, `SimViewMapsPlayTests`, `TerrainEditPlayTests`, `DayCycleWaterPlayTests`.
4. MCP `run_tests` EditMode toàn bộ; PlayMode toàn bộ. Ghi dòng tổng. MCP rớt khi PlayMode chạy: `sleep 60` rồi `get_test_job`, lặp lại; không kết thúc lượt khi test còn chạy.
5. `grep` ở Tiêu chí xong → rỗng.
6. Dọn: `git checkout -- client/ProjectSettings/EditorSettings.asset`; xoá `client/Assets/InitTestScene*.unity(.meta)` nếu có. `git status --short` chỉ còn file của brief.
7. Play scene SimView (Mac) một lần, chờ 3 giây, chụp **1** ảnh thấy khối đất + thác đổ vào ao (đặt camera nhìn vào `sv.WaterfallSource` qua MCP nếu cần). Ghi đường dẫn ảnh (scratchpad) vào Kết quả.

Ghi giờ bắt đầu/kết thúc (lệnh `date`) vào Kết quả.

### User tự test
Scene **SimView** trên Mac, Play:
1. Cạnh ao (phía +x) có khối đất cao 5 ô; nước từ đỉnh chảy qua rãnh, rơi thẳng xuống ao; không tràn ra cỏ quanh ao.
2. Đào rãnh 1 ô dài từ ao ra (10–15 ô): nước chảy hết rãnh.
3. Cho rãnh mở ra một hố/vùng rộng: nước lan ra tối đa 7 ô quanh cửa rãnh rồi dừng.
4. Đặt khối chặn giữa rãnh: đoạn phía sau rút hết trong ~1 giây; đoạn phía trước vẫn đầy.
5. Đào khối đất dưới rãnh thác (phá mép) → nước rơi xuống chỗ mới; lấp nguồn trên đỉnh → rãnh và thác rút hết.
6. Các mục của brief 0075 vẫn đúng: đào cát cạnh ao → hố đầy; đào bờ biển → biển loang như trước.

## Dừng và chuyển lại Opus khi
- Brief mơ hồ hoặc mâu thuẫn với code thật (số dòng lệch nhiều, chữ ký khác).
- Một test được giữ nguyên kỳ vọng (bước 3–4, regression) fail mà phải đổi kỳ vọng hoặc đổi QĐ mới qua; mã băm đổi.
- Cần API Unity không có trong brief.
- Cần quyết định ngoài "Quyết định đã chốt"; phải sửa file ngoài "Phạm vi".
- Cùng một lỗi sửa 2 lần vẫn chưa qua.

---

## Kết quả

Base commit: 7c258df
Chạy bằng: claude-sonnet-5-5 · medium
Bắt đầu: Sun Oct 25 21:18 +07 2026

## Kết quả kiểm chứng

Commit code: `e6f932d` (lõi + test + demo), + commit sửa `SimViewMapsPlayTests` (QĐ Planner: phương án A). File đổi: Cell, WaterConfig, WorldEditor, SimDemo, SimView, TerrainChunkView, 2 JSON, WaterFlowTests, WaterConfigTests, CellTests, WaterFlowPlayTests, SimViewMapsPlayTests.
- EditMode toàn bộ: **640/640 qua** (trước 634). Gồm `ChannelFlowsFullLength`, `ChannelOpensIntoWideArea`, `BlockingChannelDrainsDownstream`, `ChannelLoopDrainsWhenSourceRemoved`, `WaterfallIntoPondDoesNotSpill`, `DrainingFlagRoundTrip`, `RemovingOpenSourceDrains`, `DrainCountsDownGradually`, regression, `SeaFloodTests`, Golden/SimDemo — đạt.
- PlayMode toàn bộ: **275/275 qua** (trước 274) sau khi sửa test: `WaterSettlesAfterStart`, `WaterfallPoursIntoPond`, `DiggingSandNextToPondFillsHole`, `SimViewMapsPlayTests`, `HabitatPlayTests`, `TerrainEditPlayTests`, `DayCycleWaterPlayTests` — đạt. Mã băm không đổi.
- `grep UnityEngine` 4 file Core → rỗng; console 0 Error; đã dọn `EditorSettings.asset`, thư mục Captures tạm.
- Ảnh Play (SimView, Mac): `/private/tmp/claude-501/-Users-macvn-Documents-User-khang-Game-my-game/17057056-2f13-4289-b1f5-0a1bad6ed731/scratchpad/waterfall0076_c.png` — thấy khối đất, nước ở đỉnh/mép cạnh ao, ao nguyên vẹn; cột nước rơi mảnh/trong nên khó thấy trong ảnh 640px (test PlayMode xác nhận cột nước y = G+1..G+4). Phần nhìn thác thật: **chờ user**.
- Giờ: bắt đầu 21:18, xong 21:45 (~27 phút gồm một lần dừng chờ Planner).

## Vướng mắc

**Trả lời của Planner (2026-10-25): phương án A.** Trong `SimViewMapsPlayTests.SetsUpCampOnPlain`, cạnh dòng bỏ qua ao + viền cát, thêm: khi `sv.habitat`, bỏ qua cột x ∈ [cx + 14, cx + 18], z ∈ [cz − 13, cz − 9] (`cx = plain.X0 + plain.Size / 2`, `cz = plain.Z0 + plain.Size / 2`), comment `// thác nước demo (0041)`. Không đổi kỳ vọng nào khác. File này đã thêm vào Phạm vi.

`SimViewMapsPlayTests.SetsUpCampOnPlain` (file ngoài Phạm vi, brief yêu cầu "qua không sửa") fail: `surface (138,199) Expected: 20 But was: 25`. Nguyên nhân: test quét **mọi** cột của đồng bằng 48×48, chỉ bỏ qua ao + viền cát, và đòi mặt = `plain.Ground`, cỏ, không nước. Khối đất thác (QĐ 9: x ∈ [cx+15, cx+18], z ∈ [cz−12, cz−10], cao tới G+5) nằm trên đồng bằng nên (138,199) = (cx+15, cz−12) có mặt y = 25. Không phải lỗi code thác.
- Phương án A (đề xuất): sửa test, bỏ qua thêm vùng thác x ∈ [cx+14, cx+18], z ∈ [cz−13, cz−9] khi `sv.habitat` (thêm file `SimViewMapsPlayTests.cs` vào Phạm vi). Rủi ro thấp.
- Phương án B: dời thác ra ngoài đồng bằng — cần đổi QĐ 9, ảnh hưởng ao/demo.
Chờ Planner chọn; sau đó chỉ còn: sửa test, chạy lại PlayMode, bước 7 (ảnh), commit/báo.

---

## Review

**2026-10-25 — Planner (Opus), review đầy đủ: code đạt; còn lỗi hiển thị.**
- Đối chiếu diff `97bfa47..ffafdc5` với QĐ 1–10:
  - `IsNarrow`, `Emit` (nhập vào nguồn, ô hẹp không có cờ rút thì giữ mức), `allDraining`, `AddFlowRing2`, `ProcessSupport` (BFS 6 hướng, giới hạn ô, có nguồn ở trên/ngang, biển ngoài map) — đúng.
  - `PlaceWaterfall` đúng toạ độ QĐ 9; `WakeWater` được gọi trong SimView.
  - Sửa `SimViewMapsPlayTests` theo phương án A.
- Test: EditMode 640/640, PlayMode 275/275, mã băm không đổi [executor chạy qua MCP; Planner chỉ đọc kết quả].
- **Lỗi hiển thị (không do brief này gây ra):** trong ảnh của executor thấy khối đất, nước trên đỉnh và ở mép, nhưng **không thấy cột thác**.
  - Nguyên nhân: `World/BlockMesher.cs:414-419` chỉ vẽ mặt trên của ô nước khi ô ngay trên là `Empty`. Ô nước rơi có ô nước ở trên nên không có mặt nào được vẽ.
  - Cần brief **0077**: vẽ mặt bên của ô nước khi ô ngang là `Empty`, để thác và mép nước chảy nhìn thấy được. Đây vẫn là phạm vi "có thác nước" của 0041.
- Giờ: executor 21:18–21:45 (~27 phút, gồm một lần dừng chờ Planner).
- Còn: brief 0077 → user xem gộp mục User tự test của 0076 + thác.

