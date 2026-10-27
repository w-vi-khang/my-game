# Brief 0079 — World 5h: nước theo cây cha–con (mức 0–5, tăng mức bởi tường, xoá dây chuyền + tái tạo)

> Trạng thái: chờ làm
> Review: đầy đủ (user xem gộp sau brief mesh 5i)
> Model thực thi: Sonnet · high
> Cập nhật: 2026-10-27

## Mục tiêu
Thay toàn bộ logic nước chảy trong `WorldEditor` (0040 lan giới hạn, 0041 ô hẹp + kiểm khối + cờ rút, 0042 ô nối) bằng cây cha–con của [0043](../../design/decisions/0043-nuoc-cay-cha-con-tang-muc-boi-tuong.md). Biển loang (0013) giữ nguyên. Mesh chỉ đổi công thức độ cao; mesh liền + bo góc làm ở brief sau (5i).

## Bối cảnh cần đọc
- Quyết định: `design/decisions/0043-nuoc-cay-cha-con-tang-muc-boi-tuong.md` (đọc hết; mục "Quyết định" 1–6). Task: `.ai/tasks/m1-world-nuoc-cay-cha-con.md`.
- `client/Assets/Game/Core/Cell.cs`:
  - `:20` chú thích bố cục bit; `:24` `public const int MaxWaterLevel = 7;`
  - `:31-33` cờ `FlowingFlag` (bit 13), `FallingFlag` (bit 14), `DrainingFlag` (bit 15).
  - `:79-82` `IsDraining`, `SetDraining`; `:85` `IsWaterSource(ushort)` = Water và cờ chảy tắt.
  - `:89` `public static ushort MakeFlowing(ushort cell, int level, bool falling, bool draining = false)` — hiện chặn `level < 1`.
  - `:100` `ClearFlow`, `:103` `ClearWater` (dùng `DrainingFlag` trong mặt nạ).
- `client/Assets/Game/Core/WorldEditor.cs` (653 dòng):
  - `:48-50` `waterFront` (biển loang, giữ); `:52-54` `flowQueue` (bỏ).
  - `:56-96` `FlowMaxDistance`, `NarrowMaxOpenSides`, `SupportCheckMaxCells` (bỏ, thay bằng `MaxLevel`, `WallBoostMinSolid`).
  - `:98-100` `supportStarts` (bỏ); `:103` `HasPendingWater`.
  - `:111` `event Action<int,int,int> CellChanged`; `:115` `event Action<int,int,int> WaterSurfaceChanged` (giữ cả hai, giữ ý nghĩa: `CellChanged` chỉ khi vật liệu đổi; `WaterSurfaceChanged` khi mức/cờ nước đổi mà vật liệu không đổi).
  - `:132-150` `TryDig`: gọi `AddFlowDependents` + `AddFlowRing2` ở `:147-148`.
  - `:176-195` `TryPlace`: `wasWater` ở `:184`, `AddFlowDependents`/`AddFlowRing2`/`AddSupportNeighbors` ở `:190-193`.
  - `:236` `RevertFarmlandBelow` (giữ). `:245` `Faces`. `:251` `public static bool IsSolidAt(WorldData world, int x, int y, int z)` — y < 0 là đặc, ngoài map theo x/z **không** đặc.
  - `:270` `IsWaterSource(x,y,z)` (vị trí, gồm biển ngoài map), `:279` `TouchesWater`, `:287` `IsFloodable`, `:290` `AddToFront`, `:296` `FillWater` (gọi `AddFlowDependents` ở `:302`), `:310` `StepSea` (giữ).
  - `:338` `public int StepWater() => StepSea() + StepFlow();` (giữ).
  - `:340-620` toàn bộ nước chảy cũ: `AddFlow`, `AddFlowRing`, `AddFlowDependents`, `Ring2`, `AddFlowRing2`, `AddSupportNeighbors`, `IsNarrow`, `IsSupportSource`, `ProcessSupport`, `Sides`, `IsWaterCell`, `IsStandingWater`, `IsBridgeAnchor`, `IsBridge`, `Emit`, `StepFlow` (thay).
  - `:374` `public void WakeWater(int x, int y, int z)` (giữ tên; `SimView.cs:222` gọi cho nguồn thác demo).
  - `:623` `SettleWater` (giữ).
- `client/Assets/Game/Core/WaterConfig.cs` (30 dòng) — `JsonUtility`, tên trường khớp JSON.
- `client/Assets/Game/Client/TerrainChunkView.cs:228-233` nạp config; `:390` `BlockMesher.BuildChunks(World, chunkIndices, true, Editor.FlowMaxDistance)`.
- `client/Assets/Game/World/BlockMesher.cs:957-966` `WaterHeight` (công thức `(1 - WaterSurfaceDrop) * min(L, WaterFullLevel) / WaterFullLevel`); `:43` `WaterSurfaceDrop = 0.1f`; tham số `waterFullLevel` ở `:254,258,267` (giữ tên).
- JSON: `design/data/water_m1.json` và bản sao `client/Assets/Game/Client/Resources/Config/water_m1.json` (hai file phải giống hệt nhau).
- Test hiện có: `Tests/Core/WaterFlowTests.cs` (helper `Flat`, `Editor`, `Settle`, `Dig`, `Place`, `IsWater`, `PondWorld`, `Corridor`, `OpenFloor` ở `:10-74`), `Tests/Core/CellTests.cs:57-82`, `Tests/Core/WaterConfigTests.cs`, `Tests/World/BlockMesherTests.cs:941-969`, `Tests/Client/WaterFlowPlayTests.cs:57`.
- Không dùng API Unity mới (code C# thuần trong `Game.Core`).

## Quyết định đã chốt (không được đổi)
Hướng ngang theo thứ tự **D4 = (dx, dz): (0, +1), (+1, 0), (0, −1), (−1, 0)** — dùng cho mọi chỗ "4 ô ngang" dưới đây.

1. **`Cell`:** bỏ `DrainingFlag`, `IsDraining`, `SetDraining`, tham số `draining`; bit 15 thành dự phòng. `MakeFlowing(cell, level, falling)` nhận `level` 0..`MaxWaterLevel` (mức 0 hợp lệ). Mặt nạ của `ClearFlow`/`ClearWater` chỉ còn `FlowingFlag | FallingFlag`.
2. **Config:** `WaterConfig` = `configVersion = 2`, `maxLevel = 5` (1..7), `stepsPerSecond = 8` (1..60), `wallBoostMinSolid = 2` (1..4), đúng thứ tự này. JSON (cả hai file, giống hệt):
   ```json
   {
       "configVersion": 2,
       "maxLevel": 5,
       "stepsPerSecond": 8,
       "wallBoostMinSolid": 2
   }
   ```
   `WorldEditor.MaxLevel` (mặc định 5, set ngoài 1..7 → `ArgumentOutOfRangeException`), `WorldEditor.WallBoostMinSolid` (mặc định 2, ngoài 1..4 → `ArgumentOutOfRangeException`). `TerrainChunkView` gán hai giá trị này từ config, truyền `Editor.MaxLevel` vào `BuildChunks`.
3. **Nút (node):** bảng phụ trong `WorldEditor`:
   ```csharp
   struct FlowNode { public int X, Y, Z, Parent; public bool Source; }
   const int NoParent = -1;
   readonly Dictionary<(int, int, int), int> nodeAt;   // vị trí → id
   readonly Dictionary<int, FlowNode> nodes;            // id → nút
   readonly Dictionary<int, List<int>> children;        // id cha → id con (thứ tự thêm)
   int nextId;                                          // bắt đầu 0, tăng 1 mỗi nút mới
   ```
   - Ô nước chảy luôn có nút (`Source = false`, `Parent` ≠ `NoParent`).
   - Ô nguồn (`Cell.IsWaterSource`) có nút **lười**: tạo khi ô được kích hoạt hoặc khi làm cha / nguồn tái tạo; `Source = true`, `Parent = NoParent`. Không ghi gì vào ô nguồn trong `World` (giá trị ô nguồn giữ nguyên).
   - Mức của nút: nguồn → `MaxLevel`; ô chảy → `Cell.GetWaterLevel`.
   - `public bool TryGetFlowNode(int x, int y, int z, out int id, out int parentId)` — true nếu có nút ở ô đó (nguồn: `parentId = -1`). Dùng cho test.
4. **Kích hoạt:** `List<int> active` + `HashSet<int> activeSet`. `Activate(x, y, z)`: ô trong map là nước → lấy/tạo nút (nguồn lười), thêm id. Ô chảy không có nút → bỏ qua. Nút mới, nút tái tạo, nút đổi mức đều được kích hoạt. **Ô vừa thành nước (mọi cách) kích hoạt ô nước ngay phía trên nó (y + 1).**
5. **`TryDig`** (thay `:147-148`): sau khi ô thành rỗng, `Activate` 4 ô ngang theo D4 rồi ô phía trên. Biển loang `:145-146` giữ.
6. **`TryPlace`** (thay `:190-193`), sau khi ghi khối:
   - a. Nếu `wasWater` và ô có nút: gỡ nút (khỏi `nodeAt`, `nodes`, danh sách con của cha); con của nó (`children[id]`, theo id tăng) vào **hàng xoá** (`List<int> cascade` + `HashSet<int> cascadeSet`); xoá `children[id]`. Ô nguồn có nút làm y hệt.
   - b. Với 4 ô ngang theo D4: ô là nước chảy có nút → `WallBoost`.
   - c. `Activate` ô phía trên (y + 1).
7. **`FillWater`** (biển loang): bỏ `AddFlowDependents`; sau khi ô thành nước nguồn, `Activate` ô phía trên (quy tắc 4).
8. **`WallBoost(id)`** (nút không phải nguồn): `L = mức`; đếm `IsSolidAt` ở 4 ô ngang; nếu `0 < L < MaxLevel` và đếm ≥ `WallBoostMinSolid` → ghi mức `MaxLevel` (giữ cờ rơi), phát `WaterSurfaceChanged`, `Activate`, rồi `Propagate(id)`. Gọi khi: tạo nút con, tạo nút tái tạo, và ở 6b.
9. **`Propagate(id)`** — chạy hết ngay trong lần gọi (ngăn xếp `Stack<int>`), không tạo ô mới:
   - lấy `e`, `L = mức(e)`; với 4 ô ngang theo D4: ô là nước chảy có nút `n` (không phải nguồn) và `mức(n) < L − 1`:
     - ghi mức `L − 1` (giữ cờ rơi), phát `WaterSurfaceChanged`, `Activate(n)`;
     - nếu `n` **không** phải cha của `e` và **không** phải tổ tiên của `e` (đi chuỗi `Parent` từ `e` lên tới gốc) → đổi cha của `n` thành `e` (cập nhật `children` cả hai phía);
     - đẩy `n` vào ngăn xếp.
10. **Thuộc nhánh đang xoá** `IsDoomed(id)`: đi từ `id` theo `Parent`: gặp id trong `cascadeSet` → true; gặp nút nguồn hoặc `Parent == NoParent` → false; `Parent` không còn trong `nodes` → true. Quá `nodes.Count + 1` bước → `throw new InvalidOperationException("water parent cycle")`.
11. **`StepFlow()`** một nhịp = **A rồi B**, trả số ô đổi (vật liệu hoặc mức):
    - **A. Xoá dây chuyền:** nếu `cascade` khác rỗng: `batch` = `cascade` sắp tăng dần; xoá `cascade`/`cascadeSet`; `next` = rỗng.
      - Với mỗi `id` trong `batch` còn trong `nodes`: thêm `children[id]` vào `next`; gỡ nút (như 6a nhưng **không** đưa con vào hàng xoá); ghi ô `Cell.ClearWater`; phát `CellChanged`; ghi vị trí vào `deleted`.
      - Đưa `next` (bỏ trùng, giữ thứ tự tăng dần) vào `cascade`/`cascadeSet` **trước** bước tái tạo (để `IsDoomed` thấy).
      - Với mỗi vị trí trong `deleted` (theo thứ tự `batch`): nếu `SeaLevel >= 0 && y <= SeaLevel && TouchesWater` → `AddToFront`, không tái tạo. Ngược lại xét 4 ô ngang theo D4, ô là nước:
        - ô nguồn → ứng viên mức `MaxLevel`, cha mới = id nút nguồn (tạo lười);
        - ô chảy có nút, `!IsDoomed` → ứng viên mức của nó, cha mới = `Parent` của nó.
        - Chọn mức cao nhất (bằng nhau → ô đứng trước trong D4). Có → tạo nút mới ở vị trí đó: `MakeFlowing(mức, falling: false)` + `SetModified`, phát `CellChanged`, `RevertFarmlandBelow`, `Activate`, kích hoạt ô trên (quy tắc 4), `WallBoost`.
      - Sau đó với mỗi vị trí trong `deleted`: `Activate` 4 ô ngang theo D4 và ô phía trên.
    - **B. Lan:** `batch` = `active` sắp tăng dần; xoá `active`/`activeSet`. Với mỗi `id`: bỏ qua nếu không còn trong `nodes`, ô không còn là nước, hoặc `IsDoomed(id)`. `L` = mức; `b` = ô (x, y − 1, z):
      - `b` rỗng (trong map, vật liệu `Empty`) → tạo con ở `b`: mức `MaxLevel`, `falling: true`. Không lan ngang.
      - `b` là nước:
        - ô này đang rơi (`Cell.IsFalling`, ô chảy) → không làm gì;
        - ngược lại lan ngang **nếu `b` đứng được**: `b` là nguồn, hoặc ô chảy không rơi, hoặc ô rơi mà ô dưới nó (y − 2) đặc (`IsSolidAt`) / nguồn / ô chảy không rơi. Không đứng được → không làm gì.
      - `b` đặc (`IsSolidAt`) → lan ngang.
      - **Lan ngang** (chỉ khi `L > 0`): 4 ô ngang theo D4, ô trong map và vật liệu `Empty`: nếu `SeaLevel >= 0 && ny <= SeaLevel && TouchesWater(n)` → `AddToFront(n)`; ngược lại tạo con mức `L − 1`, `falling: false`.
      - **Tạo con** (cả rơi và lan): ghi `Cell.SetModified(Cell.MakeFlowing(old, mức, falling), true)`; nút mới `Parent = id`; thêm vào `children[id]`; phát `CellChanged`; `RevertFarmlandBelow`; `Activate(con)`; kích hoạt ô trên con; `WallBoost(con)`. Ghi ngay — con tạo trong nhịp này **không** được xử lý lại trong nhịp này (nó nằm trong `active` cho nhịp sau); ô đã thành nước thì ô xử lý sau (id lớn hơn) thấy là nước và bỏ qua.
12. `HasPendingWater` = `waterFront.Count > 0 || active.Count > 0 || cascade.Count > 0`. `WakeWater(x, y, z)` = `Activate(x, y, z)`.
13. **Mesh:** `BlockMesher.WaterHeight` ô chảy không rơi: `(1f - WaterSurfaceDrop) * (min(L, WaterFullLevel) + 1) / (WaterFullLevel + 1)`. Các nhánh khác giữ. Cập nhật chú thích `<summary>` (0043).
14. Không đổi: `StepSea`, biển loang 0013, `SettleWater`, `AffectedChunks`, `SimDemo`, `SimView`, bộ sinh world.

## Phạm vi
- Được sửa:
  - `client/Assets/Game/Core/Cell.cs`, `client/Assets/Game/Core/WorldEditor.cs`, `client/Assets/Game/Core/WaterConfig.cs`;
  - `client/Assets/Game/Client/TerrainChunkView.cs` (chỉ `:228-233` và `:390`);
  - `client/Assets/Game/World/BlockMesher.cs` (chỉ `WaterHeight` + chú thích của nó);
  - `design/data/water_m1.json` + `client/Assets/Game/Client/Resources/Config/water_m1.json`;
  - test: `Tests/Core/CellTests.cs`, `Tests/Core/WaterFlowTests.cs`, `Tests/Core/WaterConfigTests.cs`, `Tests/World/BlockMesherTests.cs` (chỉ `FlowingWaterSurfaceFollowsLevel`), `Tests/Client/WaterFlowPlayTests.cs` (chỉ `:57`);
  - brief này (Kết quả trở xuống).
- Không được sửa: mọi file khác (`SimDemo`, `SimView`, scene, `design/*.md`, phần mesh khác trong `BlockMesher`).

## Các bước
1. **`Cell` + config:** QĐ 1, 2 (`WaterConfig`, hai JSON, `TerrainChunkView`). Xong khi: biên dịch được (sẽ còn lỗi ở `WorldEditor` tới bước 2).
2. **`WorldEditor`:** QĐ 2–12. Xoá mọi thành phần cũ liệt kê ở "Bối cảnh" `:340-620` + `:52-100`. Cập nhật `<summary>` lớp (`:26-33`) theo 0043. Xong khi: `refresh_unity` + console 0 Error.
3. **Mesh:** QĐ 13.
4. **Sửa test cũ:**
   - `CellTests`: xoá `DrainingFlagRoundTrip`; trong `FlowFlagsRoundTrip` đổi `Assert.Throws(... MakeFlowing(0, 0, false))` thành `Assert.AreEqual(0, Cell.GetWaterLevel(Cell.MakeFlowing(0, 0, false)))` và thêm `Assert.Throws<ArgumentOutOfRangeException>(() => Cell.MakeFlowing(0, -1, false))`; sửa chú thích `LayoutMatchesWorldTech` nếu nhắc cờ rút.
   - `WaterConfigTests`: kiểm `maxLevel` 5, `stepsPerSecond` 8, `wallBoostMinSolid` 2, `configVersion` 2; `ValidateRejectsBadValues`: `maxLevel` 0 và 8, `wallBoostMinSolid` 0 và 5, `stepsPerSecond` 0 và 61 đều ném.
   - `WaterFlowPlayTests.cs:57`: `view.Editor.MaxLevel` = 5.
   - `BlockMesherTests.FlowingWaterSurfaceFollowsLevel` (mức 5 và 3, `waterFullLevel` 5): đỉnh `(4, 5.9, 4)`; `(5, 5.75, 4)`; `(5, 5.75, 5)`; `(6, 5.6, 4)`; kiểm mặt nam dùng `(4, 5.9, 4)` và `(5, 5.75, 4)`.
   - `WaterFlowTests`: xoá `BridgeSetup`, `BridgeFillsOneCellGap`, `BridgeDrainsWhenOneSideGone`, `LevelChangeRaisesWaterSurfaceChanged`. Sửa theo bảng "Phần cần test" (cột "cũ — sửa"). Giữ nguyên các test còn lại.
5. **Test mới** trong `WaterFlowTests` (bảng "Phần cần test", cột "mới"). Dùng helper sẵn có; `e.TryGetFlowNode` để lấy id/cha.
6. Chạy "Lệnh executor chạy".

## Tiêu chí xong
- `refresh_unity` → console 0 Error.
- Mọi test ở "Phần cần test" đạt; EditMode + PlayMode toàn bộ đạt (ghi dòng tổng).
- `grep -rnE "IsDraining|SetDraining|DrainingFlag|NarrowMaxOpenSides|SupportCheckMaxCells|FlowMaxDistance|narrowMaxOpenSides|supportCheckMaxCells|flowMaxDistance|IsBridge|ProcessSupport|AddFlowRing2" client/Assets/Game design/data` → rỗng.
- `diff design/data/water_m1.json client/Assets/Game/Client/Resources/Config/water_m1.json` → không khác.
- `grep -n "UnityEngine" client/Assets/Game/Core/WorldEditor.cs client/Assets/Game/Core/Cell.cs client/Assets/Game/Core/WaterConfig.cs` → rỗng.

## Test sau khi xong

### Phần cần test
Thế giới test dùng helper trong `WaterFlowTests.cs:10-74`. "lv" = `Cell.GetWaterLevel`. Mặc định `MaxLevel` 5, `WallBoostMinSolid` 2.

**cũ — sửa:**
- `PitNextToPondFills` · như cũ · ô (8,3,5) lv **5** (tăng mức bởi tường), (8,2,5) chảy; ô ao giữ nguyên giá trị.
- `WideDeepPitFills` · như cũ · cả 18 ô đều là nước (ô chảy ngang trên nước đứng được vẫn lan).
- `SpreadStopsAfterN` · `OpenFloor`, đào (1,6,8), settle · (x,6,8) với x = 1..5 có lv `5 − x`, (6,6,8) rỗng. Biến thể `e3.MaxLevel = 3`: x = 1..3 lv `3 − x`, (4,6,8) rỗng.
- `SpreadAdvancesOneCellPerTick` · `OpenFloor`, đào (1,6,8); với k = 1..5 sau mỗi `StepWater`: (k,6,8) là nước, (k+1,6,8) rỗng.
- `FallsThenSpreadsFullAgain` · như cũ · (1,11,2) lv 5; (1,11,3) lv **4**, không rơi; (1,y,3) y = 2..10 rơi; (0,y,3), (2,y,3) y = 4..10 rỗng; (1,2,3+d) nước với d = 1..5; (1,2,9) rỗng.
- `ChannelFlowsFullLength` · `Corridor` · x = 1..15 lv **5**.
- `ChannelOpensIntoWideArea` · như cũ · x = 1..5 lv 5; x = 6..10 lv `10 − x`; (11,6,8) rỗng.
- `BlockingChannelDrainsDownstream` · như cũ · x = 9..15 rỗng; x = 1..7 lv **5**.
- `WaterfallIntoPondDoesNotSpill` · như cũ, thêm `e.WakeWater(11, 8, 5);` ngay sau `var e = Editor(w);` · các khẳng định cũ giữ nguyên.
- `FlowingWaterBelowSeaLevelDoesNotStartSeaFlood` · như cũ · (6,4,4) lv **5**.

**cũ — giữ nguyên, phải đạt:** `RemovingOpenSourceDrains`, `RemovingSourceDrains`, `DrainCountsDownGradually`, `PlacingBlockStopsFlowFront`, `CellChangedOnlyOnMaterialChange` (16), `ChannelLoopDrainsWhenSourceRemoved`, `FlowIsDeterministic`, `FlowSettlesWithinBound`, `SeaFloodStillMakesSources`; cả `SeaFloodTests`.

**mới:**
- `DiamondLevelsOnOpenFloor` · `OpenFloor`, đào (1,6,8), settle · mọi ô (x,6,z) với x ≥ 0, `d = x + |z − 8|`, 1 ≤ d ≤ 5 là nước lv `5 − d`; d = 6 rỗng (bỏ (1,6,8) khỏi điều kiện "đá" vì đã đào).
- `ChannelDrainsOneCellPerTick` · `Corridor`, đào (1,6,8), settle, đặt đá (1,6,8) · sau k lần `StepWater` (k = 1..13): x = 2..k+1 rỗng, x = k+2..15 là nước; settle → x = 2..15 rỗng.
- `RegenerateFromSecondSource` · `Corridor`, thêm nguồn `w.Set(15, 6, 8, Water nguồn)` và đá (14,6,8) trước khi tạo editor; đào (1,6,8), settle; đào (14,6,8), settle → (14,6,8) lv 5, cha = nút ở (13,6,8). Đặt đá (1,6,8), settle → x = 2..14 nước lv 5; cha của (14,6,8) = nút (15,6,8) (nguồn, `parentId` −1); cha của (13,6,8) = nút (14,6,8).
- `WallBoostOnPlaceAndPropagate` · `Flat(5)`; đá (x,6,7) mọi x; nguồn (0,6,8); đá (0,6,9); đá (1,6,8). Editor, đào (1,6,8), settle. Trước khi đặt, khẳng định: (2,6,8) lv 3, (3,6,8) lv 2, (3,6,9) lv 1, cha (3,6,9) = nút (2,6,9), cha (2,6,8) = nút (1,6,8). Đăng ký đếm `WaterSurfaceChanged` theo vị trí. Đặt đá (2,6,9) (không `StepWater`) → (2,6,8) lv 5, cha vẫn = nút (1,6,8) [con không đổi cha]; (3,6,8) lv 4, cha = nút (2,6,8); (3,6,9) lv 3, cha = nút (3,6,8) [đổi cha]; (1,6,8) lv 4 [không giảm]; `WaterSurfaceChanged` có (2,6,8), (3,6,8), (3,6,9). Settle không ném, `HasPendingWater` false.
- `FallingCellCreatesChildBelowOnly` · `Flat(3)`; đá (x,4..5,8) mọi x; nguồn (0,6,8); đá (0,6,9), (1,6,8); đào (1,6,8), settle · (1,6,9) nước không rơi; (1,5,9) và (1,4,9) rơi lv 5; (1,6,10) rỗng (ô trên nước rơi chưa chạm đáy không lan).
- `CascadeRemovesSubtreeOnly` · `Corridor` biến thể: bỏ tường (x,6,9) tại x = 5 (ô (5,6,9) rỗng, (5,5,9) đá) → nhánh rẽ (5,6,9). Đào (1,6,8), settle → (5,6,9) nước lv 5, (5,6,10) là nước. Đặt đá (5,6,9), settle → x = 1..15 hàng z = 8 vẫn nước lv 5; mọi ô (x,6,z) với z ≥ 10 rỗng. Đặt đá (3,6,8), settle → x = 4..15 rỗng, x = 1..2 nước.
- **mesh** `FlowingWaterSurfaceFollowsLevel` như bước 4 (mức 0 → 0,15 ô không cần test riêng).

**regression khác:** `CellTests`, `WaterConfigTests`, `ConfigTests`, `WorldEditorTests`, `HabitatTrackerTests`, `HabitatWorldTests`, `HabitatDemoTests`, `GoldenHashTests` (mã băm không đổi), `SimDemoTests`, `SimDemoMapsTests`, `ChunkSeamTests`; PlayMode `WaterFlowPlayTests` (gồm `WaterfallPoursIntoPond`, `WaterfallColumnIsMeshed`), `HabitatPlayTests`, `SimViewMapsPlayTests`, `TerrainEditPlayTests`, `DayCycleWaterPlayTests`, `FlyCameraWaterPlayTests`, `OceanBackdropTests`.

### Lệnh executor chạy
1. MCP `refresh_unity` → `read_console` (Error) = 0.
2. MCP `run_tests` EditMode lọc: `WaterFlowTests`, `CellTests`, `WaterConfigTests`, `BlockMesherTests`, `ConfigTests`, `SeaFloodTests`, `WorldEditorTests`, `HabitatTrackerTests`, `HabitatWorldTests`, `HabitatDemoTests`, `GoldenHashTests`, `SimDemoTests`, `SimDemoMapsTests`, `ChunkSeamTests`.
   - Test mesher fail ngay sau lần biên dịch đầu → `refresh_unity` lại rồi chạy lại một lần (Burst có thể chưa nạp bản mới — brief 0077).
3. MCP `run_tests` PlayMode lọc: `WaterFlowPlayTests`, `HabitatPlayTests`, `SimViewMapsPlayTests`, `TerrainEditPlayTests`, `DayCycleWaterPlayTests`, `FlyCameraWaterPlayTests`, `OceanBackdropTests`.
4. MCP `run_tests` EditMode toàn bộ; PlayMode toàn bộ. Ghi dòng tổng. MCP rớt khi PlayMode chạy: `sleep 60` rồi `get_test_job`, lặp lại; không kết thúc lượt khi test còn chạy.
5. Các lệnh `grep`/`diff` ở "Tiêu chí xong".
6. Dọn: thoát Play; `git checkout -- client/ProjectSettings/EditorSettings.asset`; xoá `client/Assets/InitTestScene*.unity(.meta)` nếu có. `git status --short` chỉ còn file trong Phạm vi.

Ghi giờ bắt đầu/kết thúc (lệnh `date`) vào Kết quả. Không chụp ảnh (kiểm hình ảnh ở brief 5i).

### User tự test
Không ở brief này — gộp sau brief mesh 5i. Ghi sẵn để Planner chuyển sang: SimView, Play — (1) đào bờ cát ao: nước lan tối đa 5 ô, mỏng dần; (2) đào rãnh 1 ô dài: chảy hết rãnh; ra chỗ rộng lan 5 ô; (3) chặn giữa rãnh: đoạn sau rút từng ô; (4) thác demo đổ vào ao không tràn; (5) đào hố rộng 2 lớp cạnh ao: đầy cả hai lớp.

## Dừng và chuyển lại Opus khi
- Brief mơ hồ hoặc mâu thuẫn với code thật (số dòng lệch nhiều, chữ ký khác).
- Một test trong "Phần cần test" ra kết quả khác mô tả mà code đúng QĐ 1–13 (có thể Planner tính sai) — ghi giá trị thật, không tự đổi kỳ vọng.
- `IsDoomed` ném "water parent cycle" trong bất kỳ test nào.
- Mã băm `GoldenHashTests` đổi; cần API Unity; cần quyết định ngoài "Quyết định đã chốt"; phải sửa file ngoài "Phạm vi".
- Cùng một lỗi sửa 2 lần vẫn chưa qua.

---
<!-- Từ đây trở xuống: EXECUTOR ghi. -->

## Kết quả

## Kết quả test

## Vướng mắc

---
<!-- PLANNER ghi khi review. -->

## Review
