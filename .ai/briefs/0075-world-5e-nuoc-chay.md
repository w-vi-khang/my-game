# Brief 0075 — World 5e: nước chảy (nguồn + lan giới hạn, hố được lấp, mất nguồn thì rút)

> Trạng thái: cần Opus
> Review: đầy đủ + user xem
> Model thực thi: Sonnet · medium
> Cập nhật: 2026-10-25 20:45

Task contract: [m1-world-nuoc-chay](../tasks/m1-world-nuoc-chay.md). Quyết định: [0040](../../design/decisions/0040-nuoc-chay-nguon-lan-gioi-han-m1.md). Luật biển loang cũ: [0013](../../design/decisions/0013-nuoc-lan-theo-nhip.md).

## Mục tiêu
Thêm nước chảy vào `WorldEditor`:
- nước từ nguồn rơi thẳng xuống, lan ngang tối đa N ô (mặc định 7);
- ô trống nước chảy tới thì thành nước (hố cạnh ao đầy);
- ô nước đang chảy mất đường về nguồn thì rút dần;
- biển loang 0013 giữ nguyên.

Đúng cột "Xong khi" dòng 5e (world-tech): hố cạnh ao đầy; nước rơi từ cao rồi lan đúng N ô; chặn/bỏ nguồn thì rút hết; biển 0013 vẫn đúng; tất định; mã băm world sinh ra không đổi.

## Bối cảnh cần đọc
- `client/Assets/Game/Core/Cell.cs` (79 dòng):
  - bố cục: bit 0–7 vật liệu · bit 8 cốt truyện · bit 9 đã sửa · bit 10–12 mực nước · bit 13–15 dự phòng (:18-21);
  - `MaxWaterLevel = 7` (:24); `Make(BlockMaterial, int waterLevel = 0)` (:34); `GetWaterLevel` / `SetWaterLevel` (:54-61, ném khi ngoài 0–7);
  - `GetReserved` / `SetReserved` (:63-70) — 3 bit dự phòng dạng số; brief này thay bằng hai cờ riêng (QĐ 1).
- `client/Assets/Game/Core/WorldEditor.cs` (306 dòng):
  - doc class :26-32; `SeaLevel` :44; mặt trước biển loang `waterFront` / `waterFrontSet` :47-48; `HasPendingWater` :52; `CellChanged` :61; ctor :63 `WorldEditor(WorldData world, PlayerBag bag, int meshMargin, int seaLevel = -1)`;
  - `TryDig` :78-93 — :91-92 `if (SeaLevel >= 0 && y <= SeaLevel && TouchesWater(x, y, z)) AddToFront(x, y, z);`;
  - `TryPlace` :120-133 (ghi khối, mực 0, phát `CellChanged`, `RevertFarmlandBelow`);
  - `RevertFarmlandBelow` :171-178; `Faces` :180; `IsSolidAt` :186-194 (y < 0 coi là đặc; ngoài map theo x/z không đặc);
  - `IsWaterSource` :207-216 (ô nước trong map ở y ≤ SeaLevel, hoặc ngoài map theo x/z ở y ≤ SeaLevel); `TouchesWater` :218-224; `IsFloodable` :226-227; `AddToFront` :229-233; `FillWater` :235-241;
  - `StepWater` :248-273 (biển loang 0013, trả số ô ngập); `SettleWater(int maxSteps = 100000)` :276-282.
- Ai gọi:
  - `client/Assets/Game/Client/TerrainChunkView.cs`: field `waterStepsPerSecond = 8f` :24-25; `Build()` :181, tạo editor :227 `Editor = new WorldEditor(World, Bag, BlockMesher.CellMargin, waterLevel);`; `LateUpdate` → `StepWater()` :323; `StepWater()` :364-379 (chỉ chạy khi `Editor.HasPendingWater`, tối đa 4 nhịp/khung).
  - Các scene lưu `waterStepsPerSecond: 8` (WalkBench, Bench, SimView, TerrainView, SimBench) — **không sửa scene**; giá trị bị JSON ghi đè trong `Build()` (QĐ 8).
  - `client/Assets/Game/Tests/Client/TerrainEditPlayTests.cs:312` đặt `view.waterStepsPerSecond = 0f` **sau** khi view đã `Build()` — vẫn đúng.
- Dữ liệu world sinh ra ghi nước = vật liệu Water + mực 7, không bit nào khác: `World/WorldMapGenerator.cs:190`, `World/TerrainGenerator.cs:137`; ao demo `Core/Sim/SimDemo.cs:75-85` (`PlacePond`, `Cell.Make(BlockMaterial.Water, Cell.MaxWaterLevel)`, ao ở `y = plain.Ground`, viền cát cùng tầng, dưới là đất).
- Cấu hình JSON mẫu: `Core/DayCycle/TimeConfig.cs` (`[Serializable] class TimeConfig`, field public có default, `Validate()` :64 ném `ArgumentException`); `Client/ConfigLoader.cs:53-61` `LoadTimeConfig()` (đọc `Resources/Config/time_m1`); test `Tests/Core/DayCycle/TimeConfigTests.cs` (`ConfigParsesAndValidates`, so file với default, `ValidateRejectsBadValues`); `Tests/World/ConfigTests.cs:35-43` `HabitatClientCopyMatchesDesignData` (so `design/data/*.json` với bản sao `client/Assets/Game/Client/Resources/Config/`).
- Test hiện có dính tới nước (phải qua **không sửa**): `Tests/Core/SeaFloodTests.cs` (toàn bộ, gồm `StepWater()` đúng 1 ô/nhịp ở :266-274 và cả cột trong 1 nhịp ở :286), `Tests/Core/WorldEditorTests.cs:266-279` `WaterAboveFarmlandRevertsItToDirt`, `Tests/Core/Habitat/HabitatTrackerTests.cs:121` `WaterSpreadUpdatesNearWater` (+ chuỗi sửa ngẫu nhiên :255-267), `Tests/World/HabitatWorldTests.cs:63`, `Tests/Client/TerrainEditPlayTests.cs:339-366`, `Tests/Client/DayCycleWaterPlayTests.cs`, `Tests/World/GoldenHashTests.cs`.
- Test PlayMode mẫu dựng SimView có ao: `Tests/Client/HabitatPlayTests.cs:17-47` (SetUp, `PondOrigin(out wx0, out wz0)`).

## Quyết định đã chốt (không được đổi)
1. **Ô nước** (`Cell.cs`, thêm; giữ nguyên mọi hàm cũ):
   ```csharp
   const ushort FlowingFlag = 1 << 13; // nước đang chảy (tắt = nguồn) — 0040
   const ushort FallingFlag = 1 << 14; // nước đang rơi — 0040
   public static bool IsFlowing(ushort cell);
   public static bool IsFalling(ushort cell);
   /// <summary>Ô vật liệu Water và cờ đang chảy tắt (mực bất kỳ).</summary>
   public static bool IsWaterSource(ushort cell);
   /// <summary>Ô nước đang chảy: Water, cờ chảy bật, mực = level (1–7), cờ rơi theo falling; giữ cờ cốt truyện/đã sửa của cell.</summary>
   public static ushort MakeFlowing(ushort cell, int level, bool falling);
   ```
   - Sửa comment bố cục :18-21 thành: `bit 13 nước đang chảy · bit 14 nước đang rơi · bit 15 dự phòng`.
   - `SetReserved` / `GetReserved` không còn ai dùng thì giữ nguyên (không xoá — ngoài phạm vi); nếu có nơi dùng (grep) → dừng, cần Opus.
   - Nước có sẵn (world sinh ra, ao, test cũ dùng `Cell.Make(Water, 7)` hoặc `Cell.Make(Water)`) = **nguồn**. Không đổi generator → mã băm world không đổi.
2. **Tham số:** `public int FlowMaxDistance { get; set; } = 7;` trong `WorldEditor` (N, hợp lệ 1–7; ngoài khoảng → `ArgumentOutOfRangeException` ở setter).
3. **Mức lan** (ô nước đang chảy, mực 1–7). Gọi `emit(nb)` = mức mà ô nước `nb` truyền sang ô ngang:
   - `nb` là nguồn hoặc đang rơi → `N`;
   - `nb` đang chảy ngang, mực `s` → `s − 1`;
   - `nb` chỉ truyền ngang khi **"đứng được"**. Gọi B = ô dưới `nb`:
     - B đặc (`IsSolidAt`, gồm y − 1 < 0) → đứng được;
     - B là nguồn, hoặc nước đang chảy ngang (cờ rơi tắt) → đứng được;
     - B là nước đang rơi → `nb` đứng được **chỉ khi** `nb` không phải nước đang rơi **và** B "đã chạm đáy" (ô dưới B đặc, hoặc là nước có cờ rơi tắt);
     - B rỗng → không đứng được.
   - Lý do: cột thác (các ô rơi chồng nhau) không lan ngang giữa không trung; ô rơi dưới cùng chạm đáy thì lan; tầng trên của hố rộng vẫn được lấp vì ô ngang phía trên nằm trên ô rơi đã chạm đáy.
   - Kết quả: ô cách nguồn d ô có mực `N + 1 − d`; d = N có mực 1 và không truyền tiếp.
4. **Trạng thái mong muốn** của ô X trong map, X **không** đặc và **không** là nguồn (ô rỗng hoặc nước đang chảy):
   - ô ngay trên X (trong map) là nước (bất kỳ) → X = nước đang rơi, mực `N`;
   - ngược lại `m = max emit(nb)` qua 4 ô ngang `nb` trong map là nước và đứng được (ngoài map theo x/z bỏ qua); `m ≥ 1` → X = nước đang chảy ngang mực `m` (cờ rơi tắt); `m < 1` (không có) → X = rỗng.
   - Ô đặc và ô nguồn không bao giờ bị phần nước chảy đổi. Ô ngoài map không ghi.
5. **Nhịp nước chảy** — hàng chờ `flowQueue` (List + HashSet như `waterFront`, thứ tự chèn cố định), method riêng `int StepFlow()`:
   1. Lấy ảnh chụp hàng chờ, xoá hàng chờ.
   2. **Tính** trạng thái mong muốn (QĐ 4) cho từng ô trong ảnh chụp, đọc từ world **chưa ghi gì** trong nhịp này; lưu kết quả vào danh sách tạm.
   3. **Ghi** các ô có giá trị khác hiện tại (so cả vật liệu, mực, cờ). Ô thành nước: `MakeFlowing(old, level, falling)` + bật cờ đã sửa; ô thành rỗng: `Cell.SetModified(Cell.SetWaterLevel(Cell.SetMaterial(old, Empty), 0) & ~cờ chảy/rơi, true)` (viết helper trong `Cell` nếu cần, ví dụ `ClearWater(ushort cell)`).
   4. **Rơi ngay:** với mỗi ô vừa thành nước ở bước 3 (theo thứ tự ghi), đi xuống trong khi ô dưới trong map và **rỗng** → ghi ô đó thành nước đang rơi mực `N` (như 0013 rơi cả cột trong một nhịp).
   5. Với mọi ô đã đổi ở bước 3–4:
      - đổi vật liệu (rỗng ↔ nước) → phát `CellChanged(x, y, z)`; ô thành nước → `RevertFarmlandBelow` (0020). Chỉ đổi mực/cờ → **không** phát.
      - Đưa vào `flowQueue` các ô phụ thuộc (QĐ 6).
      - Ô vừa thành rỗng mà `SeaLevel >= 0 && y <= SeaLevel && TouchesWater(x, y, z)` → `AddToFront` (biển loang lấp lại như 0013).
   6. Trả số ô đã đổi.
6. **Ô phụ thuộc** của ô P vừa đổi (bất kỳ vì sao — đào, đặt, nước chảy, biển loang): P; ô dưới P; 4 ô ngang của P; 4 ô ngang của ô trên P; 4 ô ngang của ô cao hơn P hai ô. Chỉ thêm ô trong map. (X phụ thuộc ô trên X, 4 ô ngang X, ô dưới mỗi ô ngang đó và ô dưới nữa — tập trên là phép ngược.)
   - `TryDig` thành công → thêm ô phụ thuộc của ô đào (ngoài phần 0013 có sẵn).
   - `TryPlace` thành công → thêm ô phụ thuộc của ô đặt.
   - `FillWater` (biển loang) → thêm ô phụ thuộc của ô vừa ngập.
   - `TryTill`, `TryGrow`, `RevertFarmlandBelow` không đổi đặc ↔ không đặc → không thêm.
7. **Biển loang 0013 giữ nguyên thuật toán**, chỉ đổi:
   - `IsWaterSource(x, y, z)` (WorldEditor :207): trong map → `World.GetMaterial == Water && Cell.IsWaterSource(World.Get(x, y, z))` (nước đang chảy **không** kích hoạt biển loang); phần ngoài map giữ nguyên.
   - `FillWater` ghi nguồn như cũ (mực 7, không cờ).
   - `StepWater()` = phần biển loang hiện có (đổi tên thành `StepSea()` private, giữ nguyên thân) **rồi** `StepFlow()`; trả tổng hai số. Thứ tự này bảo đảm ô ≤ SeaLevel cạnh biển được biển loang lấp (thành nguồn) trước khi phần chảy xét tới.
   - `HasPendingWater => waterFront.Count > 0 || flowQueue.Count > 0`.
   - `SettleWater` giữ nguyên.
8. **JSON** `design/data/water_m1.json` + bản sao byte-giống `client/Assets/Game/Client/Resources/Config/water_m1.json`, nội dung đúng:
   ```json
   {
       "configVersion": 1,
       "flowMaxDistance": 7,
       "stepsPerSecond": 8
   }
   ```
   - `client/Assets/Game/Core/WaterConfig.cs` (namespace `Game.Core`, không UnityEngine): `[Serializable] public class WaterConfig { public int configVersion = 1; public int flowMaxDistance = 7; public int stepsPerSecond = 8; public void Validate(); }` — `Validate` ném `ArgumentException` khi `flowMaxDistance` ngoài 1–7 hoặc `stepsPerSecond` ngoài 1–60.
   - `ConfigLoader`: `public const string WaterM1Path = "Config/water_m1";` + `public static WaterConfig LoadWaterConfig()` theo mẫu `LoadTimeConfig`; thêm `water_m1` vào comment class.
   - `TerrainChunkView.Build()` ngay sau dòng tạo editor (:227): `var water = ConfigLoader.LoadWaterConfig(); Editor.FlowMaxDistance = water.flowMaxDistance; waterStepsPerSecond = water.stepsPerSecond;`. Sửa summary field :24 thành "Nhịp nước (0013, 0040): số lần WorldEditor.StepWater mỗi giây — Build() ghi đè từ water_m1.json."
9. **Doc class** `WorldEditor` (:26-32): thêm một câu: "Nước chảy (design/decisions/0040): nguồn (ô nước có sẵn, ô biển loang) không cạn; nước rơi thẳng xuống cả cột, lan ngang tối đa FlowMaxDistance ô (1 ô/nhịp) khi đứng trên khối đặc/nước; mất nguồn thì rút dần." Bỏ câu "Nước không tự rút." thay bằng "Nước biển (nguồn) không tự rút."
10. **Mã băm:** `GoldenHashTests` và mọi test mã băm mô phỏng (`SimDemoTests`, `SimDemoMapsTests`, `Sim/**`) phải qua **không sửa số**. Nếu một mã băm đổi → dừng, cần Opus (ghi test + số cũ/mới).

## Phạm vi
- Được sửa/tạo:
  - `client/Assets/Game/Core/Cell.cs`, `WorldEditor.cs`, `WaterConfig.cs` (mới);
  - `client/Assets/Game/Client/ConfigLoader.cs`, `TerrainChunkView.cs` (chỉ QĐ 8);
  - `design/data/water_m1.json` (mới) + `client/Assets/Game/Client/Resources/Config/water_m1.json` (mới);
  - test mới: `client/Assets/Game/Tests/Core/WaterFlowTests.cs`, `client/Assets/Game/Tests/Core/WaterConfigTests.cs`, `client/Assets/Game/Tests/Client/WaterFlowPlayTests.cs`; thêm test `WaterClientCopyMatchesDesignData` vào `Tests/World/ConfigTests.cs`; thêm test cờ vào `Tests/Core/CellTests.cs`;
  - `.meta` do Unity tạo cho file mới;
  - brief này (Kết quả trở xuống).
- Không được sửa: mọi file khác — kể cả test cũ dính tới nước (danh sách ở Bối cảnh), scene `.unity`, generator, `SimDemo`, `design/*.md` (Planner cập nhật khi review).

## Các bước
1. **Lõi** — `Cell.cs` (QĐ 1), `WorldEditor.cs` (QĐ 2–7, 9), `WaterConfig.cs` (QĐ 8). Xong khi: biên dịch được, console 0 Error.
2. **Test EditMode** — `Tests/Core/WaterFlowTests.cs` (namespace `Game.Core.Tests`). World tự dựng 16 × 16 × 16, đá đáy y = 0; editor `new WorldEditor(w, bag, 1, seaLevel: -1)` (không có biển loang) trừ khi ghi khác; túi có 1000 đá. Hàm phụ `Settle(e)` = `e.SettleWater()`, assert `!e.HasPendingWater`.
   - `PitNextToPondFills`: nền đá y 1..3; ao nguồn 4 × 4 ở y = 3 (x, z ∈ 4..7, `Cell.Make(Water, 7)`); đào (8, 3, 5) và (8, 2, 5) → sau `Settle` cả hai ô là Water; (8, 3, 5) đang chảy mực 7, (8, 2, 5) đang rơi hoặc chảy; ô nguồn không đổi.
   - `WideDeepPitFills`: như trên, đào hình hộp x 8..10, z 4..6, y 2..3 (18 ô) → sau `Settle` cả 18 ô là Water.
   - **Hành lang** (dùng cho nhiều test): nền đá y 1..5 toàn map; tường đá ở y = 6 tại z = 7 và z = 9, x = 0..15; nguồn đặt tay `w.Set(0, 6, 8, Cell.Make(Water, 7))`; đá đặt tay ở (1, 6, 8); các ô (x, 6, 8) với x ≥ 2 rỗng. Bắt đầu bằng `TryDig(1, 6, 8)`.
   - `SpreadStopsAfterN`: hành lang, N = 7, `Settle` → (x, 6, 8) với x = 1..7 là nước đang chảy, mực `8 − x`; x = 8 rỗng. Lặp lại trên world mới với `FlowMaxDistance = 3` → x = 1..3 nước (mực 3, 2, 1), x = 4 rỗng.
   - `SpreadAdvancesOneCellPerTick`: hành lang, N = 7, sau `TryDig`: lần `StepWater()` thứ k (k = 1..7) → (k, 6, 8) là nước, (k + 1, 6, 8) rỗng.
   - `FallsThenSpreadsFullAgain`: nền đá y = 1 toàn map (y = 0 đá đáy). Tháp đá x 0..2, z 0..2, y 2..10. Trên đỉnh tháp đặt tay: nguồn (1, 11, 1), đá (1, 11, 2). Tường đá y = 2 tại x = 0 và x = 2, z 3..15 (hành lang dưới chân tháp theo +z). `TryDig(1, 11, 2)` → `Settle`. Kỳ vọng:
     - (1, 11, 2) nước mực 7; (1, 11, 3) nước mực 6, cờ rơi tắt;
     - cột (1, y, 3) với y = 2..10 là nước đang rơi (`IsFalling`);
     - (0, y, 3) và (2, y, 3) với y = 4..10 rỗng (thác không lan ngang giữa không trung; y = 3 nằm trên tường nên được phép có nước);
     - (1, 2, 3 + d) với d = 1..7 là nước, d = 8 rỗng (chạm đáy lan lại đủ N).
   - `BlockingSourceDrains`: sau `SpreadStopsAfterN` (N = 7), `TryPlace(1, 6, 8, Stone)` (chặn ngay cạnh nguồn) → `Settle` → mọi ô x = 2..7 rỗng; nguồn (0, 6, 8) còn.
   - `RemovingSourceDrains`: như `PitNextToPondFills`, rồi đặt đá vào toàn bộ 16 ô ao (`TryPlace`, ô nước đặt được) → `Settle` → (8, 3, 5), (8, 2, 5) rỗng.
   - `DrainCountsDownGradually`: hành lang đã `Settle`, `TryPlace(1, 6, 8, Stone)`, gọi `StepWater()` một lần → còn ít nhất một ô nước trong x = 2..7 (rút dần, không biến mất ngay trong 1 nhịp).
   - `PlacingBlockStopsFlowFront`: hành lang, `TryDig(1, 6, 8)`, 2 lần `StepWater()`, `TryPlace(4, 6, 8, Stone)`, `Settle` → x = 1..3 nước, x = 5..7 rỗng.
   - `CellChangedOnlyOnMaterialChange`: đếm `CellChanged` trong `SpreadStopsAfterN` → đúng số ô đổi vật liệu (7 cho N = 7, cộng ô đào 1).
   - `FlowIsDeterministic`: đất y 1..6 toàn map; ao nguồn 4 × 4 ở y = 6, x, z ∈ 6..9; túi 1000 đá. Chuỗi 300 thao tác `System.Random(11)`: mỗi bước chọn x ∈ 0..15, y ∈ 3..8, z ∈ 0..15, `rng.Next(2) == 0` → `TryDig`, ngược lại `TryPlace(.., Stone)`; sau mỗi thao tác `StepWater()` một lần. Chạy 2 lần trên 2 world mới → mọi ô `World.Get` giống nhau.
   - `FlowSettlesWithinBound`: sau chuỗi trên, `SettleWater(10000)` → `HasPendingWater == false`.
   - `SeaFloodStillMakesSources` (seaLevel = 5; chép hàm dựng `Coast()` của `SeaFloodTests.cs:12-28` vào file này): `TryDig` lần lượt (4, 5, 7), (5, 5, 7), (6, 5, 7), `Settle` → ba ô là nước và `Cell.IsWaterSource` true; `TryPlace(4, 5, 7, Stone)` (tường ngăn biển), `Settle` → (5, 5, 7), (6, 5, 7) vẫn là nước (nước biển không rút — 0013).
   - `FlowingWaterBelowSeaLevelDoesNotStartSeaFlood`: seaLevel = 5, không có biển: đá y 1..7 toàn map; nguồn đặt tay (4, 8, 4), đá đặt tay (5, 8, 4). `TryDig` lần lượt (5, 8, 4), (5, 7, 4), (5, 6, 4), (5, 5, 4), (5, 4, 4), `Settle` → (5, 4, 4) là nước, `Cell.IsWaterSource` false. Rồi `TryDig(6, 4, 4)`, `Settle` → (6, 4, 4) là nước, `Cell.IsFlowing` true, mực 7 (lấp bởi phần chảy, không thành nguồn).
   - Test cờ: `Tests/Core/CellTests.cs` thêm `FlowFlagsRoundTrip` — `MakeFlowing` giữ cờ đã sửa/cốt truyện; `IsWaterSource(Make(Water, 7))` true; `IsWaterSource(MakeFlowing(..))` false; `IsFalling` đúng; level 0 và 8 ném.
3. **Test cấu hình** — `Tests/Core/WaterConfigTests.cs` theo mẫu `TimeConfigTests`: `ConfigParsesAndValidates`, `ConfigFileMatchesDefaults` (file = `new WaterConfig()`), `ValidateRejectsBadValues` (flowMaxDistance 0 / 8, stepsPerSecond 0 / 61). `ConfigTests.WaterClientCopyMatchesDesignData` theo mẫu :35-43.
4. **Client** — `ConfigLoader`, `TerrainChunkView` (QĐ 8). Xong khi: console 0 Error.
5. **Test PlayMode** — `Tests/Client/WaterFlowPlayTests.cs`, SetUp chép theo `HabitatPlayTests.cs:17-47` (SimView trên world nhiều map, có ao):
   - `DiggingSandNextToPondFillsHole`: lấy ô cát viền ao `(wx0 − 1, Ground, wz0 + 1)`; `view.Editor.TryDig` → chờ tối đa 2 giây (`yield return null`) → ô đó là Water; `view.Editor.FlowMaxDistance == 7`; `view.waterStepsPerSecond == 8`.
   - `NoFloodOnPlainWithoutEdits`: sau SetUp chờ 30 khung → `view.Editor.HasPendingWater == false` (world đứng yên).
6. Chạy cả bộ, dọn, chụp ảnh.

## Tiêu chí xong
- Mọi test mới qua; toàn bộ EditMode + PlayMode qua (trước brief: 615 EditMode / 272 PlayMode theo brief 0074; ghi số mới).
- Test cũ ở Bối cảnh qua **không sửa**; `GoldenHashTests` và test mã băm mô phỏng không đổi số.
- Console 0 Error.
- `git diff --stat` chỉ có file trong Phạm vi.
- `grep -rn "UnityEngine" client/Assets/Game/Core/Cell.cs client/Assets/Game/Core/WorldEditor.cs client/Assets/Game/Core/WaterConfig.cs` → rỗng.

## Test sau khi xong

### Phần cần test
- Cờ ô (QĐ 1): `CellTests.FlowFlagsRoundTrip` + `CellTests` cũ.
- Hố cạnh ao đầy (QĐ 3–5): `PitNextToPondFills`, `WideDeepPitFills`, PlayMode `DiggingSandNextToPondFillsHole`.
- Lan đúng N ô + 1 ô/nhịp + tham số N (QĐ 2–3, 5): `SpreadStopsAfterN`, `SpreadAdvancesOneCellPerTick`.
- Rơi rồi lan lại đủ (QĐ 4, 5.4): `FallsThenSpreadsFullAgain`.
- Rút (QĐ 4–6): `BlockingSourceDrains`, `RemovingSourceDrains`, `DrainCountsDownGradually`; chặn dòng: `PlacingBlockStopsFlowFront`.
- `CellChanged` chỉ khi đổi vật liệu (QĐ 5.5): `CellChangedOnlyOnMaterialChange`; regression `HabitatTrackerTests` (`WaterSpreadUpdatesNearWater`, chuỗi sửa ngẫu nhiên), `HabitatWorldTests`.
- Tất định + dừng (ràng buộc): `FlowIsDeterministic`, `FlowSettlesWithinBound`.
- Biển 0013 (QĐ 7): toàn bộ `SeaFloodTests` (không sửa), `SeaFloodStillMakesSources`, `FlowingWaterBelowSeaLevelDoesNotStartSeaFlood`, `WorldEditorTests` (gồm `WaterAboveFarmlandRevertsItToDirt`), PlayMode `TerrainEditPlayTests`, `DayCycleWaterPlayTests`.
- JSON (QĐ 8): `WaterConfigTests`, `ConfigTests.WaterClientCopyMatchesDesignData`.
- World đứng yên + mã băm (QĐ 1, 10): `NoFloodOnPlainWithoutEdits`, `GoldenHashTests`, `SimDemoTests`, `SimDemoMapsTests`, `Sim/**`.
- Regression khác: `HabitatPlayTests`, `SimViewMapsPlayTests`, `PlayerRulesTests`, `GridPathfinderTests`, `DropFieldTests`; EditMode + PlayMode toàn bộ.

### Lệnh executor chạy
1. MCP `refresh_unity` → `read_console` (Error) = 0.
2. MCP `run_tests` EditMode lọc: `WaterFlowTests`, `WaterConfigTests`, `CellTests`, `ConfigTests`, `SeaFloodTests`, `WorldEditorTests`, `HabitatTrackerTests`, `HabitatWorldTests`, `GoldenHashTests`, `SimDemoTests`, `SimDemoMapsTests`.
3. MCP `run_tests` PlayMode lọc: `WaterFlowPlayTests`, `TerrainEditPlayTests`, `DayCycleWaterPlayTests`, `HabitatPlayTests`.
4. MCP `run_tests` EditMode toàn bộ; PlayMode toàn bộ. Ghi dòng tổng. Nếu PlayMode chưa xong khi MCP rớt: `sleep 60` rồi `get_test_job`, lặp lại; không kết thúc lượt khi test còn chạy.
5. `grep -rn "UnityEngine" client/Assets/Game/Core/Cell.cs client/Assets/Game/Core/WorldEditor.cs client/Assets/Game/Core/WaterConfig.cs` → rỗng.
6. Dọn: `git checkout -- client/ProjectSettings/EditorSettings.asset`; xoá `client/Assets/InitTestScene*.unity(.meta)` nếu có. `git status --short` chỉ còn file của brief.
7. Play scene SimView (Mac) một lần: đào 2 ô cát viền ao + ô đất dưới một trong hai bằng `view.Editor.TryDig` qua MCP `execute_code` (hoặc bằng tay), chờ 2 giây, chụp **1** ảnh có ao và hố đã đầy nước. Ghi đường dẫn ảnh (scratchpad, không commit) vào Kết quả.

Ghi giờ bắt đầu/kết thúc (lệnh `date`) vào Kết quả.

### User tự test
Scene **SimView** trên Mac, Play:
1. Đào một ô cát viền ao → trong ~1 giây hố thành nước.
2. Đào sâu thêm 1–2 ô ngay dưới / cạnh hố đó → các ô cũng đầy nước.
3. Đào một rãnh dài 10 ô từ ao ra ngoài (mặt đất) → nước chỉ chạy tới ô thứ 7 rồi dừng.
4. Đặt một khối vào đầu rãnh sát ao → nước trong rãnh rút dần trong ~1 giây.
5. Lên đồi/khối cao cạnh nước (hoặc xây tháp cạnh ao rồi đào thông) → nước rơi xuống rồi lan ra tối đa 7 ô ở dưới.
6. Đào bờ biển (TerrainView hoặc mép đảo) → biển vẫn loang vào như trước, đặt tường không làm khô.

## Dừng và chuyển lại Opus khi
- Brief mơ hồ hoặc mâu thuẫn với code thật (vd số dòng lệch nhiều, chữ ký khác).
- Một test cũ ở Bối cảnh fail mà phải sửa test hoặc đổi QĐ mới qua; mã băm nào đổi (QĐ 10).
- Cần API Unity không có trong brief.
- Cần một quyết định thuộc AGENTS.md §3.4 hoặc ngoài "Quyết định đã chốt".
- Cùng một lỗi sửa 2 lần vẫn chưa qua.
- Phải sửa file ngoài "Phạm vi".

---

## Kết quả

- Base commit: 46ad460
- Chạy bằng: claude-sonnet-5-5 · medium
- Bắt đầu: 2026-10-25 20:41 (+07)
- Ghi chú: `GetReserved/SetReserved` chỉ test cũ dùng (CellTests, WorldMapGeneratorTests, TerrainGeneratorTests), không có code production → giữ nguyên, tiếp tục.

## Kết quả kiểm chứng

## Vướng mắc

Code lõi + test mới đã viết (chưa commit, còn trong working tree: Cell.cs, WorldEditor.cs, WaterConfig.cs, ConfigLoader, TerrainChunkView, JSON ×2, test mới). Biên dịch OK, 0 Error. EditMode toàn bộ: 627/634 qua, **7 fail** [run]:

**A. Test cũ `SeaFloodTests` fail (5) — không sửa được theo luật brief:** `BreakingSealedWallFloodsDryInside` (kỳ vọng 45, thực 11), `FloodFillsConnectedDryChamber` (29 → 12), `OpenUWallRefills` (45 → 33), `WallNotReachingFloorLeaksUnderneath` (45 → 27), `WaterSpreadsOneCellPerStepSideways` (1 ô/nhịp → 2).
Nguyên nhân: `StepWater()` = `StepSea()` + `StepFlow()` trả tổng hai số, và `StepFlow` chạy trên các ô đã đào ≤ SeaLevel cạnh biển: ô được "nước chảy" lấp (không phải nguồn, tối đa N ô, có ô thành nước ngang cả khi biển chưa tới) → (1) số trả về lệch (test đếm bằng giá trị trả về), (2) ô thành nước chảy không phải nguồn nên không kích hoạt biển loang tiếp (`IsWaterSource` bỏ nước chảy) và nhịp loang đổi (2 ô/nhịp). QĐ 7 giả định "biển loang lấp trước" chỉ đúng với ô sát biển; ô thứ hai trong hành lang đào lấp bởi phần chảy trước khi front biển tới.
Câu hỏi: biển loang ≤ SeaLevel phải loại trừ phần chảy thế nào? Phương án: (a) `StepFlow` bỏ qua ô `y <= SeaLevel` khi `SeaLevel >= 0` (biển độc quyền vùng ≤ SeaLevel; nước chảy chỉ trên mực biển) — nhưng test `FlowingWaterBelowSeaLevelDoesNotStartSeaFlood` trong brief lại cần nước chảy ở y = 4 < SeaLevel 5 → mâu thuẫn; (b) `StepWater()` trả riêng số ô biển loang, tách `StepFlow` khỏi giá trị trả (test cũ đếm đúng) + `StepFlow` không lấp ô đang nằm trong `waterFront`/ô mà `TouchesWater`-biển sẽ ngập; (c) cho `StepFlow` chỉ chạy khi `waterFront` rỗng (biển xong rồi mới chảy). Đề xuất: (b)+(c) kết hợp; cần Opus chọn và cập nhật QĐ 7.

**B. `FallsThenSpreadsFullAgain` fail (hình học brief):** đỉnh tháp (y = 11) hở nên nước từ (1,11,2) cũng lan sang (0,11,2),(2,11,2) rồi rơi qua mép tháp thành cột thác ở x = 0 và x = 2 (z = 3), nên (0,y,3), (2,y,3) y = 4..10 có nước (cột rơi, đúng luật QĐ 3–4). Kỳ vọng "rỗng" trong brief mâu thuẫn với hình học. Cần Opus sửa test (vd bọc tường đá quanh đỉnh tháp, hoặc bỏ kỳ vọng đó). Phần còn lại của test (cột (1,y,3) rơi, lan d = 1..7 dưới chân) chưa kiểm vì assert dừng sớm.

**C. `SeaFloodStillMakesSources` fail (thứ tự thao tác trong test brief):** đào 3 ô rồi mới `Settle`: ô (5,5,7) bị phần chảy lấp (mực 7 nhưng cờ chảy bật → không phải nguồn) trước khi front biển tới → kỳ vọng `IsWaterSource` sai. Gộp với A: cùng gốc (nước chảy lấp vùng ≤ SeaLevel trước biển). Nếu chọn (c) ở A thì có thể tự qua.

Chưa chạy: PlayMode (chưa có ý nghĩa khi EditMode còn đỏ), ảnh chụp, bước dọn.

---

## Review
