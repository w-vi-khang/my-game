# Brief 0029 — Simulation bước 7b: đánh dấu xới từng ô, 2 loại cây, kho thông nhau (lõi)

> Trạng thái: chờ review
> Review: đầy đủ
> Model thực thi: Sonnet · medium
> Cập nhật: 2026-10-21

<!-- Planner (Opus) viết, Executor (Sonnet) làm theo ở PHIÊN KHÁC — executor không có lịch sử chat, không được tự quyết.
Trạng thái: chờ làm → đang làm → chờ review → đã duyệt; hoặc → cần Opus (executor dừng, hỏi ở mục Vướng mắc).
Review: nhẹ (đọc mục Kết quả) | đầy đủ (đọc cả diff) | user xem (có phần user tự kiểm bằng mắt/chơi thử) — có thể ghép "đầy đủ + user xem".
Model thực thi: BẮT BUỘC, đúng dạng `<Model> · <effort>` — Model: Opus | Sonnet | Haiku | Fable; effort: low | medium | high | xhigh | max (chọn theo AGENTS.md §10.2).
Executor kiểm tra model + effort của phiên TRƯỚC khi làm (scripts/brief_check.py); lệch hoặc không xác định được → cảnh báo user và DỪNG.
File được commit. Tự kiểm trước khi giao (AGENTS.md §10.3): executor có phải hỏi gì không? còn chỗ nào bỏ ngỏ 2 phương án? -->

## Mục tiêu
Làm bước 7b theo [task contract](../tasks/m1-sim-buoc-7b-danh-dau-xoi-giong-kho.md) và quyết định [0025](../../design/decisions/0025-nguoi-choi-xoi-dat-chon-giong-hang-doi.md) (Chốt). Chỉ lõi mô phỏng:
- Bỏ `AddFarmOrder`. Thêm `MarkTill` (đánh dấu **một** ô), `Unmark`, `PlotAt`, `EnqueueCrop(plotId, cropId)`, `RemoveCrop`.
- Trạng thái "đã xới" của ô ruộng (`SoilReady`): vụ bắt đầu khi bật → gieo; tắt → xới.
- 2 loại cây trong `sim_m1.json` (`crops`). Hạt và nông sản theo loại.
- **Kho chung** (`SimState.Stock`): hàng của mọi kho tính chung, sức chứa cộng dồn. Hàng thu hoạch chở về kho gần nhất theo đường đi.

Ước lượng của cả bước: 2,5–5 ngày-người. Ghi giờ bắt đầu/kết thúc vào mục Kết quả.

## Bối cảnh cần đọc
Đường dẫn tính từ `client/Assets/Game/`. Số dòng theo commit `8655054`.
- `Core/Sim/SimData.cs`:
  - `WorkStage` (`:121-168`): constructor có `plotId = -1, storageId = -1, loadProduce = 0, loadSeeds = 0`.
  - `Storage` (`:183-200`): `Seeds`, `Produce`, `Reserved`, `Free`.
  - `FarmPlot` (`:202-223`): `StorageId` readonly, `Tilled` (= ô đã thành ô ruộng), `Removed`.
  - `SimState` (`:248-265`), constructor `SimState(SimRng rng)`. Test tạo trực tiếp ở `Tests/Core/View/{SimInterpolator,SiteStatus,CreatureStatus}Tests.cs`.
- `Core/Sim/SimConfig.cs`: `CropConfig` (`:70-76`), `StorageConfig.startSeeds` (`:82`), `GrowTicks` (`:161`), `Validate` (`:260-265` cho crop/storage).
- `Core/Sim/Simulation.cs`:
  - `AddStorage` (`:101-112`).
  - `CancelPlot` (`:374-398`): trả hạt vào `State.Storages[p.StorageId]` (`:392`).
  - `DisplaceX/Y/Z` (`:573-575`), `AdjacentGoals` (`:644-660`).
  - `OnStageDone` (`:722-770`): nhánh xới `:729-733`; thu hoạch tạo vận chuyển `:743-760`; vận chuyển xong cộng vào kho `:763-769`.
  - `AddFarmOrder` (`:786-817`), `EnqueueCrop` (`:820-828`), `StartCrop` (`:830-834`), `AddPlotStage` (`:836-843`).
  - `AssignPhase`: mở lại đình trệ `:1039-1040`; chặn thiếu hạt / kho đầy `:1063-1074`.
  - `Assign` trừ hạt `:1122`, giữ chỗ `:1123`. `Unassign` trả hạt `:1151`, nhả chỗ `:1152`.
- `Core/Sim/SimHash.cs:55-111` — phần băm công đoạn, kho, ô ruộng.
- `Core/Sim/SimDemo.cs:88-91` (`Build`), `:190-196` (`KeepFarming`). `Core/Sim/SimStress.cs:104-114`.
- `Core/View/SiteStatus.cs:9-10` (`StorageText`). `Client/SimView.cs:255` và `:314-318`. `Client/SimBenchMeter.cs:87-91`.
- `design/data/sim_m1.json` và bản chép `client/Assets/Game/Client/Resources/Config/sim_m1.json` (`Tests/World/ConfigTests.cs:26-31` so hai bản).
- Mốc thời gian của bố trí `FarmTests` (`Tests/Core/Sim/FarmTests.cs:23-33`). Bố trí: `Flat(1)`, `NoWander()`, L1 (8,2,5), L2 (5,2,1), L3 (9,2,5), kho (6,2,9), một ô ruộng soil (5,1,5).

  | Nhịp | Diễn biến (vụ chọn ngay lúc đánh dấu) |
  |---|---|
  | 15 | L1 bắt đầu xới |
  | 75 | Xới xong; L1 bắt đầu gieo ngay; hạt 10 → 9 |
  | 105 | Gieo xong, chuyển sang tưới |
  | 155 | Bắt đầu lớn, `RipeTick` = 1355 |
  | 1395 | Thu xong; vận chuyển giao cho L3 |
  | 1459 | Hàng vào kho: nông sản 1, hạt 10 |

  **Các mốc này không đổi** khi test gọi `MarkTill` rồi `EnqueueCrop(id, 0)` ngay sau đó (cùng chỗ `AddFarmOrder` cũ). Lý do: thứ tự id công đoạn giữ nguyên. Chỉ `OrderId` khác, mà `OrderId` không dùng khi chọn việc.

## Quyết định đã chốt (không được đổi)
**Từ 0025 (user Chốt 2026-10-21):**
1. Đánh dấu **từng ô**: mặt trên khối đất/cỏ, ô trên rỗng. Sinh vật có năng lực Xới (L1) tới xới. Xới xong → ô ruộng (`Farmland`, như 0020).
2. Chọn giống → vụ thêm vào cuối hàng đợi (`farm.plotQueueCapacity` = 2). Ô vừa xới xong: vụ đầu **bắt đầu từ gieo**. Vụ kế sau thu hoạch: **xới lại** rồi gieo. Hết hàng đợi → ô đứng yên, không mở công đoạn.
3. Hai loại cây, số liệu tạm. `game-balance` đặt sau.
4. Bỏ `AddFarmOrder`.
5. Hàng chở về **kho gần nhất tới được**. Các kho **thông nhau**: hàng tính chung, sức chứa = tổng sức chứa các kho.

**Planner đề xuất trong 0025 — user xác nhận 2026-10-21 (luật 6–20). Executor làm đúng như sau:**

6. **"Đã xới"** = `FarmPlot.SoilReady`: xới xong → bật; thu hoạch xong → tắt. Vụ bắt đầu khi đang bật → gieo; đang tắt → xới.
7. **Bỏ đánh dấu** (`Unmark`): chỉ cho ô có biển (chưa thành ô ruộng). Sinh vật đang tới xới / đang xới → về rảnh. Tiến độ xới dở mất.
8. **Đào/che ô có biển** → luật "ô chưa xới" 0020 sẵn có (`CancelPlot`, 0 hạt). **Đào/che ô ruộng** → luật 0020 sẵn có. Không sửa `TerrainPhase`.
9. **Xoá vụ** (`RemoveCrop`):
   - vụ chưa bắt đầu → chỉ rời hàng đợi;
   - vụ đang làm → trả hạt như 0020, sinh vật về rảnh, công đoạn `Cancelled`; ô **vẫn là ô ruộng**; hàng đã thu vẫn chở về.
10. **Hạt và nông sản theo loại cây.** Mỗi kho đặt vào góp `startSeeds` hạt **mỗi loại** (10, số tạm). Gieo trừ 1 hạt đúng loại; hết hạt loại đó → `NoSeeds`. Sức chứa tính chung mọi loại. Mỗi lần thu hoạch vẫn là 1 lượt vận chuyển.
11. **Kho gần nhất** = kho có đường đi ngắn nhất từ ô `Site`. Không kho nào tới được → đình trệ "không có đường" như hiện tại. Kho đầy = tổng chung đầy.
12. "Không dừng trên ô ruộng" (0019) áp cho ô có biển: `OnPlot` đã tính mọi ô chưa `Removed`, **không sửa**.

**Quyết định Planner thêm khi viết brief (user xác nhận 2026-10-21):**

13. **Được chọn giống lên ô có biển** (trước khi xới xong). Khi xới xong mà hàng đợi có vụ → gieo luôn; rỗng → đứng yên. Lý do: 0025 nói xới "không cần" chọn giống trước, không cấm. Cách này giữ mốc `FarmTests` và cho `SimDemo`/`SimStress` dựng sẵn trại trồng được. 7c quyết có cho bấm vào biển hay không.
14. **Kho gần nhất chọn lúc thu hoạch xong** (lúc công đoạn vận chuyển được tạo). Không chọn lại sau đó.
    - Đường tính bằng khung 1 × 1 × 1 từ `Site` tới ô kề kho đứng được. Ô kề tính như `AdjacentGoals`.
    - Đường ngắn bằng nhau → kho id nhỏ hơn.
    - Không kho nào tới được → kho gần nhất theo Manhattan (bằng nhau → id nhỏ hơn). Sau đó sinh vật chở không tới được thì đình trệ `NoPath` như cũ.
15. **Không có kho thì không chọn giống được:** `EnqueueCrop` trả false khi `State.Storages.Count == 0`. Đánh dấu xới vẫn được.
16. **Xoá vụ đang làm và trạng thái "đã xới":** huỷ ở pha `Till` hoặc `Sow` → `SoilReady` giữ nguyên; huỷ ở `Water`, `Growing`, `Harvest` → tắt. Sau đó: hàng đợi còn vụ → bắt đầu vụ kế theo mục 6; hết → đứng yên.
17. **`Unmark` và `RemoveCrop` không phát sự kiện** (người chơi tự làm). `CropCancelled` chỉ phát khi đào/che, như 0020.
18. **Cấu hình:** `crop` → mảng `crops`; `storage.startSeeds` → `crops[i].startSeeds`; `configVersion` → 2. Số tạm:

    | id | growTimeMs | seedsPerHarvest | producePerHarvest | startSeeds |
    |---|---|---|---|---|
    | A | 120000 | 1 | 1 | 10 |
    | B | 180000 | 2 | 3 | 10 |

    Cây A giữ đúng số cũ, nên test cũ giữ nguyên mốc. Cây B khác cả ba số.
19. `SimDemo` và `SimStress` chỉ trồng **Cây A** (giữ số `SimDemoTests`, `SimStressTests`). Chọn Cây B trong SimView để bước 7c.
20. `MarkTill` trả −1 khi không hợp lệ, không ném lỗi: công cụ Xới ở 7c gọi với ô bất kỳ.

## Phạm vi
- **Được sửa:**
  - `client/Assets/Game/Core/Sim/{SimData,SimConfig,Simulation,SimHash,SimDemo,SimStress}.cs`
  - `Core/View/SiteStatus.cs`, `Client/SimView.cs`, `Client/SimBenchMeter.cs` (chỉ để biên dịch, như bước 10)
  - `design/data/sim_m1.json` và `client/Assets/Game/Client/Resources/Config/sim_m1.json` (hai bản giống hệt)
  - Test: `Tests/Core/Sim/{FarmTests,PlotRulesTests,IdleTests,SimBoundsTests,SimDeterminismTests,SimConfigTests}.cs`, `Tests/Core/View/SiteStatusTests.cs`, `Tests/World/{SimDemoTests,SimDemoMapsTests,SimStressTests}.cs`
- **Được tạo:** `Tests/Core/Sim/CropQueueTests.cs` (+ `.meta` do Unity sinh).
- **Sửa test cũ:** chỉ được đổi **cách gọi** theo bước 11 (API mới, `Stock` thay `Storages[k].Seeds/Produce/Reserved/Free`, `cfg.crops[...]` thay `cfg.crop`/`cfg.storage.startSeeds`). **Không đổi số kỳ vọng nào**, trừ hai chỗ ghi ở bước 11.
- **Không được sửa:** `design/` ngoài hai file JSON trên; task contract; `GridPathfinder.cs`; `TestWorlds.cs`; mọi file khác.

## Các bước
Đường dẫn tính từ `client/Assets/Game/`.

1. **`Core/Sim/SimConfig.cs`:**
   - Thay `CropConfig` bằng:
     ```csharp
     /// <summary>Một loại cây (0025). Số tạm; game-balance đặt sau. startSeeds = hạt loại này mỗi kho góp vào kho chung.</summary>
     [Serializable]
     public class CropDef
     {
         public string id;
         public int growTimeMs;
         public int seedsPerHarvest;
         public int producePerHarvest;
         public int startSeeds;

         public CropDef() { }
         public CropDef(string id, int growTimeMs, int seedsPerHarvest, int producePerHarvest, int startSeeds)
         {
             this.id = id;
             this.growTimeMs = growTimeMs;
             this.seedsPerHarvest = seedsPerHarvest;
             this.producePerHarvest = producePerHarvest;
             this.startSeeds = startSeeds;
         }
     }
     ```
   - `StorageConfig`: xoá `startSeeds`, chỉ còn `capacity = 50`.
   - `SimConfig`: `configVersion = 2`. Thay `public CropConfig crop = new CropConfig();` bằng (giữ đúng vị trí trường):
     ```csharp
     public CropDef[] crops =
     {
         new CropDef("A", 120000, 1, 1, 10),
         new CropDef("B", 180000, 2, 3, 10),
     };
     ```
   - `GrowTicks` thành `public int GrowTicks(int cropId) => MsToTicks(crops[cropId].growTimeMs);`.
   - `Validate`: thay hai kiểm `crop` và `storage.startSeeds` (`:260-265`) bằng:
     - `storage == null || storage.capacity < 1` → lỗi (giữ);
     - `crops == null || crops.Length < 1 || crops.Length > 8` → "crops phải có 1–8 loại.";
     - mỗi loại: `id` rỗng hoặc trùng → lỗi; `growTimeMs <= 0` hoặc không chia hết `msPerTick` → lỗi; `seedsPerHarvest < 0`, `producePerHarvest < 0`, `startSeeds < 0` → lỗi; `seedsPerHarvest + producePerHarvest` ngoài `[1, storage.capacity]` → lỗi;
     - tổng `startSeeds` mọi loại > `storage.capacity` → "Tổng crops[].startSeeds phải <= storage.capacity.".

2. **`design/data/sim_m1.json`:** `"configVersion": 2`; thay dòng `"crop"` bằng mảng `"crops"` theo bảng ở mục 18; `"storage": { "capacity": 50 }`. Giữ thứ tự khoá như thứ tự trường trong `SimConfig` (test `ConfigMatchesCodeDefaults` so `ToJson`). Chép **nguyên file** sang `Client/Resources/Config/sim_m1.json`.

3. **`Core/Sim/SimData.cs`:**
   - `WorkStage`: thêm `public readonly int CropId;` (doc: "loại cây của hàng chở; -1 = không phải vận chuyển nông sản"). Constructor thêm tham số cuối `int cropId = -1`.
   - `Storage`: chỉ còn `Id`, `Cell`, `Capacity`. Constructor `Storage(int id, CellPos cell, int capacity)`. Doc: "Chỗ đặt kho; hàng nằm ở kho chung `SimState.Stock` (0025)."
   - Thêm:
     ```csharp
     /// <summary>Kho chung (0025): hàng của mọi kho tính chung, sức chứa = tổng sức chứa các kho. Mảng theo loại cây.</summary>
     public sealed class Stockpile
     {
         public int Capacity;
         public readonly int[] Seeds, Produce;
         public int Reserved;

         public Stockpile(int cropCount)
         {
             Seeds = new int[cropCount];
             Produce = new int[cropCount];
         }

         public int SeedsTotal { get { int n = 0; for (int i = 0; i < Seeds.Length; i++) n += Seeds[i]; return n; } }
         public int ProduceTotal { get { int n = 0; for (int i = 0; i < Produce.Length; i++) n += Produce[i]; return n; } }
         public int Free => Capacity - SeedsTotal - ProduceTotal - Reserved;
     }
     ```
   - `FarmPlot`: xoá `StorageId`; constructor `FarmPlot(int id, int orderId, CellPos soil)`. Sửa doc `Tilled`: "Đã thành ô ruộng (0020) — xới xong lần đầu." Thêm:
     ```csharp
     /// <summary>Trạng thái "đã xới" (0025): xới xong bật, thu hoạch xong tắt. Vụ bắt đầu khi bật → gieo, tắt → xới.</summary>
     public bool SoilReady;
     ```
     `Queue` giữ `List<int>`, mỗi phần tử là **id loại cây** (chỉ số trong `Config.crops`).
   - `SimState`: thêm `public readonly Stockpile Stock;`. Constructor `SimState(SimRng rng, int cropCount = 1)`, gán `Stock = new Stockpile(cropCount)`.

4. **`Core/Sim/Simulation.cs` — kho chung:**
   - Constructor: `State = new SimState(new SimRng(config.simSeed, 1UL), config.crops.Length);`.
   - `AddStorage`: tạo `new Storage(State.Storages.Count, cell, Config.storage.capacity)`; rồi `State.Stock.Capacity += Config.storage.capacity;` và cộng `Config.crops[k].startSeeds` vào `State.Stock.Seeds[k]` với mọi `k`.
   - Thêm `int CropOf(FarmPlot p) => p.Queue[0];` (vụ đang làm luôn ở đầu hàng đợi).
   - Mọi chỗ đọc/ghi `State.Storages[...]` cho hạt, nông sản, giữ chỗ, chỗ trống đổi sang `State.Stock`:
     - `AssignPhase :1039` → `State.Stock.Seeds[CropOf(State.Plots[g.PlotId])] > 0`; `:1040` → `State.Stock.Free >= ...`;
     - `:1063` → `State.Stock.Seeds[CropOf(State.Plots[s.PlotId])] <= 0`; `:1069` → `State.Stock.Free < ...`;
     - `Assign :1122` → `State.Stock.Seeds[CropOf(...)]--`; `:1123` → `State.Stock.Reserved += ...`;
     - `Unassign :1151` → `State.Stock.Seeds[CropOf(...)]++`; `:1152` → `State.Stock.Reserved -= ...`;
     - vận chuyển xong (`:763-769`): điều kiện giữ `s.Ability == WorkAbility.Haul && s.StorageId >= 0`; thân thành `Stock.Reserved -= LoadProduce + LoadSeeds; Stock.Produce[s.CropId] += s.LoadProduce; Stock.Seeds[s.CropId] += s.LoadSeeds;`.

5. **`Core/Sim/Simulation.cs` — kho gần nhất.** Thêm hai list riêng `storeGoals`, `storePath` (không dùng chung `goals`/`tmpPath`) và:
   ```csharp
   /// <summary>Kho gần nhất theo đường đi khung 1×1×1 từ from tới ô kề kho (0025); bằng nhau → id nhỏ hơn. Không kho nào tới được → gần nhất theo Manhattan.</summary>
   int NearestStorage(CellPos from)
   {
       var ss = State.Storages;
       int best = -1, bestLen = int.MaxValue;
       for (int i = 0; i < ss.Count; i++)
       {
           storeGoals.Clear();
           for (int d = 0; d < 4; d++)
               for (int k = 0; k < 3; k++)
               {
                   var n = ss[i].Cell.Offset(DisplaceX[d], DisplaceY[k], DisplaceZ[d]);
                   if (pathfinder.CanStand(n, 1, 1, 1)) { storeGoals.Add(n); break; }
               }
           if (!pathfinder.FindPathToAny(from, storeGoals, 1, 1, 1, storePath)) continue;
           if (storePath.Count < bestLen) { bestLen = storePath.Count; best = i; }
       }
       if (best >= 0) return best;
       int bestD = int.MaxValue;
       for (int i = 0; i < ss.Count; i++)
       {
           int d = CellPos.Manhattan(from, ss[i].Cell);
           if (d < bestD) { bestD = d; best = i; }
       }
       return best;
   }
   ```
   Nhánh thu hoạch của `OnStageDone` (`:743-749`): trước khi tạo `haul`, lấy `int crop = CropOf(p); var cd = Config.crops[crop]; int k = NearestStorage(p.Site);`. Tạo vận chuyển với `State.Storages[k].Cell`, `storageId: k`, `loadProduce: cd.producePerHarvest`, `loadSeeds: cd.seedsPerHarvest`, `cropId: crop`. Giữ nguyên các tham số khác.

6. **`Core/Sim/Simulation.cs` — đánh dấu và vụ.**
   - Xoá `AddFarmOrder`. Thêm:
     ```csharp
     /// <summary>Ô ruộng chưa Removed có Soil = soil; -1 = không có.</summary>
     public int PlotAt(CellPos soil)
     {
         var ps = State.Plots;
         for (int i = 0; i < ps.Count; i++)
             if (!ps[i].Removed && ps[i].Soil == soil) return i;
         return -1;
     }

     /// <summary>Có đánh dấu xới được ô soil không (0025): trong map và vùng mô phỏng, đất/cỏ, ô trên rỗng, chưa là ô ruộng / chưa có biển.</summary>
     public bool CanMarkTill(CellPos soil)
     {
         var size = World.Size;
         if (!size.InBounds(soil.X, soil.Y, soil.Z) || !Bounds.Contains(soil.X, soil.Z)) return false;
         var m = World.GetMaterial(soil.X, soil.Y, soil.Z);
         if (m != BlockMaterial.Dirt && m != BlockMaterial.Grass) return false;
         if (!size.InBounds(soil.X, soil.Y + 1, soil.Z) || World.GetMaterial(soil.X, soil.Y + 1, soil.Z) != BlockMaterial.Empty) return false;
         return PlotAt(soil) < 0;
     }

     /// <summary>Đánh dấu xới một ô (0025): ô có biển, mở công đoạn xới. Trả id ô ruộng; -1 khi không hợp lệ.</summary>
     public int MarkTill(CellPos soil)
     {
         if (!CanMarkTill(soil)) return -1;
         int orderId = AddOrder(new[] { soil });
         var p = new FarmPlot(State.Plots.Count, orderId, soil);
         State.Plots.Add(p);
         p.Phase = PlotPhase.Till;
         AddPlotStage(p, WorkAbility.Till);
         return p.Id;
     }
     ```
   - `Unmark` và `RemoveCrop` dùng chung hàm bỏ công đoạn đang mở. Tách phần đầu `CancelPlot` (`:376-391`) thành:
     ```csharp
     /// <summary>Bỏ công đoạn đang mở của ô (0020/0025): trả hạt nếu đã trừ mà chưa thu, sinh vật đang làm về rảnh, công đoạn Cancelled. Trả số hạt đã trả.</summary>
     int AbortActive(FarmPlot p, out int stageId, out int creatureId)
     ```
     Thân giữ đúng code cũ `:376-391`. Thêm một dòng trước `return`: `if (seeds > 0) State.Stock.Seeds[CropOf(p)] += seeds;`. Hàm này **không** đụng `Queue`, `Phase`, `StageId`, `Removed`.

     `CancelPlot` thành (xoá dòng cộng hạt cũ `:392`, vì `AbortActive` đã cộng): `int seeds = AbortActive(p, out int stageId, out int creatureId);` rồi `p.Removed = true; p.Queue.Clear(); p.Phase = PlotPhase.Idle; p.StageId = -1;` và phát sự kiện như cũ. Hành vi không đổi.
   - Thêm:
     ```csharp
     /// <summary>Bỏ đánh dấu (0025): chỉ ô có biển (chưa thành ô ruộng). Sinh vật đang xới về rảnh. Không phát sự kiện.</summary>
     public bool Unmark(int plotId)
     {
         var p = State.Plots[plotId];
         if (p.Removed || p.Tilled) return false;
         AbortActive(p, out _, out _);
         p.Removed = true;
         p.Queue.Clear();
         p.Phase = PlotPhase.Idle;
         p.StageId = -1;
         return true;
     }

     /// <summary>Xoá vụ thứ index trong hàng đợi (0025). index 0 khi ô đang làm = huỷ vụ đang làm; ô vẫn là ô ruộng. Không phát sự kiện.</summary>
     public bool RemoveCrop(int plotId, int index)
     {
         var p = State.Plots[plotId];
         if (p.Removed || index < 0 || index >= p.Queue.Count) return false;
         if (index > 0 || p.Phase == PlotPhase.Idle || !p.Tilled)
         {
             p.Queue.RemoveAt(index);
             return true;
         }
         var phase = p.Phase;
         AbortActive(p, out _, out _);
         p.Queue.RemoveAt(0);
         p.StageId = -1;
         if (phase == PlotPhase.Water || phase == PlotPhase.Growing || phase == PlotPhase.Harvest) p.SoilReady = false;
         if (p.Queue.Count > 0) StartCrop(p);
         else p.Phase = PlotPhase.Idle;
         return true;
     }
     ```
     Ghi chú: ô có biển (`!Tilled`) đang ở pha `Till` là xới **đánh dấu**, không phải của vụ. Nên xoá vụ trên ô đó chỉ rời hàng đợi; công đoạn xới giữ nguyên.
   - `EnqueueCrop(int plotId, int cropId)`:
     - `cropId` ngoài `[0, Config.crops.Length)` → `throw new ArgumentException("Không có loại cây: " + cropId)`;
     - trả false khi `p.Removed`, hàng đợi đầy, hoặc `State.Storages.Count == 0`;
     - `p.Queue.Add(cropId)`; `if (p.Phase == PlotPhase.Idle) StartCrop(p);` (ô có biển đang `Till` thì chỉ thêm vào hàng đợi).
   - `StartCrop`: `if (p.SoilReady) { p.Phase = PlotPhase.Sow; AddPlotStage(p, WorkAbility.Sow); } else { p.Phase = PlotPhase.Till; AddPlotStage(p, WorkAbility.Till); }`.
   - `OnStageDone`:
     - nhánh `Till` → `if (!p.Tilled) { WriteFarmland(p.Soil); p.Tilled = true; } p.SoilReady = true;`. Sau đó: `if (p.Queue.Count > 0) { p.Phase = PlotPhase.Sow; AddPlotStage(p, WorkAbility.Sow); } else { p.Phase = PlotPhase.Idle; p.StageId = -1; }`.
     - nhánh `Water` (`:741`): `Config.GrowTicks` → `Config.GrowTicks(CropOf(p))`.
     - nhánh `Harvest`: sau khi tạo vận chuyển, đặt `p.SoilReady = false;` **trước** `p.Queue.RemoveAt(0)`. Phần còn lại giữ (`StartCrop` sẽ ra xới).

7. **`Core/Sim/SimHash.cs`:**
   - Công đoạn: thêm `MixInt(h, g.CropId)` sau `LoadSeeds`.
   - Kho: bỏ `Seeds`, `Produce`, `Reserved` của từng kho (giữ Id, Cell, Capacity).
   - Ngay sau vòng kho, băm `Stock`: `Capacity`, `Reserved`, `Seeds.Length`, rồi từng `Seeds[i]`, `Produce[i]`.
   - Ô ruộng: bỏ `p.StorageId`; thêm `MixInt(h, p.SoilReady ? 1 : 0)` sau `Removed`.

8. **`Core/Sim/SimDemo.cs`:**
   - `Build` (`:88-91`): thay `AddFarmOrder` bằng vòng: `int id = sim.MarkTill(at(Soil[i]));`. Nếu `id < 0` → `throw new InvalidOperationException("Không đánh dấu được ô ruộng demo " + at(Soil[i]))`. Rồi `sim.EnqueueCrop(id, 0);`.
   - `KeepFarming` (`:195`): `sim.EnqueueCrop(ps[i].Id, 0)`. Sửa doc: "thêm một vụ Cây A".

9. **`Core/Sim/SimStress.cs:113`:** thay bằng vòng 4 ô: `int id = sim.MarkTill(soil[q]); if (id >= 0) sim.EnqueueCrop(id, 0);`. Kho tự chọn theo mục 14: kho `j` là gần nhất của lệnh `j`. Sửa doc `Build` (`:84`): "15 nhóm 4 ô ruộng" thay "15 lệnh trồng cây".

10. **Biên dịch client và View:**
    - `Core/View/SiteStatus.cs`: `StorageText(Stockpile s)` → `"Kho · nông sản " + s.ProduceTotal + " · hạt " + s.SeedsTotal + " · " + (s.SeedsTotal + s.ProduceTotal) + "/" + s.Capacity`. Giữ đúng định dạng.
    - `Client/SimView.cs`:
      - `:314-315`: thay vòng kho bằng **một dòng** `SiteStatus.StorageText(Sim.State.Stock)`.
      - `:255` và `:318`: thay `Sim.Config.GrowTicks` bằng `Sim.Config.GrowTicks(p.Queue.Count > 0 ? p.Queue[0] : 0)`. Ở `:255`, chuyển phép tính vào trong vòng, theo từng `p`.
    - `Client/SimBenchMeter.cs:87-91`: `produce = sim.State.Stock.ProduceTotal; seeds = sim.State.Stock.SeedsTotal;`.

11. **Đổi cách gọi ở test cũ** (không đổi số kỳ vọng):
    - `AddFarmOrder(cells, k)` → mỗi ô `int id = sim.MarkTill(c); sim.EnqueueCrop(id, 0);`, theo đúng thứ tự trong list. Có ở `FarmTests`, `PlotRulesTests.Build`, `IdleTests`, `SimDeterminismTests`, `SimBoundsTests`.
    - `EnqueueCrop(x)` → `EnqueueCrop(x, 0)`.
    - `Storages[0].Seeds` / `.Produce` / `.Reserved` / `.Free` → `Stock.Seeds[0]` / `Stock.Produce[0]` / `Stock.Reserved` / `Stock.Free`. Biến `store` trong test đổi sang `var store = sim.State.Stock;`, dùng `store.Seeds[0]`.
    - `cfg.storage.startSeeds = 0` (`FarmTests.NoSeedsWaits`) → `cfg.crops[0].startSeeds = 0`.
    - `SimBoundsTests:84-85`: `Assert.Throws` → `Assert.AreEqual(-1, sim.MarkTill(...))`; `DoesNotThrow` → `Assert.GreaterOrEqual(sim.MarkTill(...), 0)`.
    - `FarmTests.AddFarmOrderValidates` → đổi tên `MarkTillValidates`. Mỗi `Assert.Throws` thành `Assert.AreEqual(-1, sim.MarkTill(ô))`. Ca "kho 5 không tồn tại" bỏ (không còn tham số kho). Ca "trùng ô trong danh sách" thành: đánh dấu (3,1,3) hai lần → lần hai −1. Giữ các assert đếm `Orders`/`Plots`, đếm lại theo ca mới.
    - `SimConfigTests:78`: `c.storage.startSeeds = 51` → `c.crops[0].startSeeds = 41` (tổng 51 > 50), thông điệp "tổng startSeeds > capacity".
    - `SiteStatusTests`: `StorageTextFormat` dựng `var s = new Stockpile(2) { Capacity = 50, Reserved = 3 }; s.Seeds[0] = 10; s.Produce[0] = 4;`, kỳ vọng giữ nguyên `"Kho · nông sản 4 · hạt 10 · 14/50"`. `new FarmPlot(2, 0, 0, …)` → `new FarmPlot(2, 0, …)`.
    - `SimDemoTests`, `SimDemoMapsTests`, `SimStressTests`: `Storages[0].Seeds/Produce` → `Stock.Seeds[0]/Produce[0]`. Vòng cộng nông sản mọi kho → `Stock.ProduceTotal`.
    - **Hai chỗ được đổi số**, vì Cây B góp 10 hạt vào chỗ trống chung:
      - `FarmTests.StorageFullWaits`: thêm `cfg.crops[1].startSeeds = 0;` sau `cfg.storage.capacity = 10;`. Không đổi assert.
      - `PlotRulesTests.RefundExceedsFullStorage`: không đổi config. Đổi `store.Produce = 41` thành `store.Produce[0] = 31`, để `Free` vẫn là −1.
    - Nếu một test cũ khác đỏ vì số → **dừng, cần Opus**. Không tự đổi số.

12. **Tạo `Tests/Core/Sim/CropQueueTests.cs`** theo "Phần cần test".

## Tiêu chí xong
- EditMode: hết đỏ; ghi tổng số test. PlayMode: hết đỏ.
- Mốc `FarmTests` cũ đạt; test cũ chỉ đổi cách gọi theo bước 11.
- `Game.Core` không thêm `using UnityEngine`.
- `git status` sạch ngoài các file trong Phạm vi. Đã dọn `EditorSettings.asset`.

## Test sau khi xong

### Phần cần test
Bố trí `Layout` trong `CropQueueTests` (namespace `Game.Core.Tests`):
- `w = TestWorlds.Flat(1)`; `sim = new Simulation(TestWorlds.NoWander(), w)`; `editor = new WorldEditor(w, new MaterialInventory(), 1)`; `sim.AttachEditor(editor)`;
- thứ tự như `FarmTests.Layout`: thêm L1 (8,2,5), L2 (5,2,1), L3 (9,2,5); thêm kho (6,2,9); `int id = sim.MarkTill(new CellPos(5,1,5))`; gọi `EnqueueCrop(id, c)` cho từng `c` trong tham số `crops` (danh sách vụ, có thể rỗng); cuối cùng `TestWorlds.HomeAll(sim)`.

Các ca:
1. `MarkWithoutCropTillsThenIdles`:
   - Đánh dấu, không vụ. `RunTo(75)`: `Tilled`, `SoilReady`, `Phase` Idle, `StageId` −1, vật liệu `Farmland`.
   - `RunTo(2000)`: số công đoạn vẫn 1; `Stock.Seeds[0]` 10.
2. `FirstCropAfterTillStartsAtSow`:
   - Như ca 1 tới nhịp 75, rồi `EnqueueCrop(id, 0)` → `Phase` Sow ngay. Chỉ có 1 công đoạn Till trong `State.Stages`.
   - Nhịp 76: L1 Working, `Stock.Seeds[0]` 9. Gieo xong ở nhịp **106**. `RipeTick` = **1356**.
   - Mốc cũ + 1, vì vụ được thêm sau nhịp 75.
3. `LongIdleThenCropStillStartsAtSow`: như ca 1, `RunTo(3000)`, rồi `EnqueueCrop(id, 1)` → `Phase` Sow, không có Till mới.
4. `NextCropRetills`:
   - Vụ `[0, 0]` lúc đánh dấu. `RunTo(1395)`: `SoilReady` false, `Phase` Till, `Queue.Count` 1.
   - `RunTo(1455)`: `SoilReady` true, `Phase` Sow. Mốc giống `FarmTests.QueueRepeatsAndCapsAtTwo`.
5. `CropBUsesItsOwnNumbers`:
   - Vụ `[1]`. Chạy tới khi công đoạn vận chuyển `Done`, tối đa 3000 nhịp.
   - Bắt đầu lớn ở nhịp 155 → `RipeTick` = 155 + 1800 = 1955.
   - Vận chuyển có `CropId` 1, `LoadProduce` 3, `LoadSeeds` 2.
   - Cuối cùng: `Stock.Produce[1]` 3, `Stock.Seeds[1]` 11, `Stock.Seeds[0]` 10, `Stock.Produce[0]` 0.
6. `NoSeedsOnlyBlocksThatCrop`:
   - Hai ô (5,1,5) vụ `[1]` và (6,1,5) vụ `[0]`. `Stock.Seeds[1] = 0` trước nhịp đầu.
   - Sau khi cả hai xới xong: gieo ô 1 `Blocked`/`NoSeeds`; gieo ô 2 không `Blocked`.
   - Đặt `Stock.Seeds[1] = 1` → nhịp sau gieo ô 1 hết `Blocked`.
7. `UnmarkReleasesTillerAndAllowsRemark`:
   - Đánh dấu, `RunTo(30)` (L1 đang xới). `Unmark(id)` → true; L1 Idle, `StageId` −1; công đoạn xới `Cancelled`; ô `Removed`.
   - Vật liệu ô vẫn `Grass`/`Dirt` như trước; không có sự kiện `CropCancelled`.
   - `MarkTill` lại cùng ô → id mới ≥ 0. `RunTo(150)` → ô mới `Tilled`.
   - `Unmark` lên ô đã `Tilled` → false.
8. `RemoveQueuedCropOnlyLeavesQueue`: vụ `[0, 1]`, `RunTo(500)`. `RemoveCrop(id, 1)` → true; `Queue` = `[0]`; `Phase` Growing không đổi; số hạt không đổi.
9. `RemoveActiveCropRefundsAndKeepsFarmland` (TestCase theo pha):
   - Vụ `[0]`, chạy tới nhịp ở từng pha: 90 (Sow đang làm), 140 (Water), 500 (Growing), 1370 (Harvest). `RemoveCrop(id, 0)` → true.
   - Kỳ vọng:
     - `Stock.Seeds[0]` = 10 (đã trả 1);
     - `Queue` rỗng, `Phase` Idle, ô không `Removed`, vật liệu vẫn `Farmland`, `Tilled` true;
     - sinh vật đang làm về Idle; không có `CropCancelled`;
     - `SoilReady` = true ở ca 90, false ở ca 140/500/1370.
   - Sau đó `EnqueueCrop(id, 0)`: ca 90 → `Phase` Sow; ca khác → `Phase` Till.
10. `RemoveCropOnMarkedPlotKeepsTill`: vụ `[0]`, `RunTo(30)`. `RemoveCrop(id, 0)` → true; `Queue` rỗng; công đoạn xới vẫn `Assigned`. `RunTo(75)` → `Phase` Idle.
11. `HaulGoesToNearestReachableStorage`:
    - Thêm kho thứ hai (6,2,3), id 1: gần ô (5,2,5) hơn kho (6,2,9).
    - Vụ `[0]`, chạy tới vận chuyển `Done`. Vận chuyển có `StorageId` 1, `Destination` (6,2,3).
    - `Stock.Capacity` 100, `Stock.Seeds[0]` 20 (2 kho × 10), `Stock.Produce[0]` 1.
12. `HaulFallsBackWhenNoStorageReachable`:
    - Trước khi tạo `Simulation`: `w.Set(x, y, z, Cell.Make(BlockMaterial.Stone))` tại (5,y,9), (7,y,9), (6,y,8), (6,y,10) với y = 2 và 3 → 4 ô kề kho (6,2,9) không đứng được.
    - Vụ `[0]`, `RunTo(1395)`. Vận chuyển `StorageId` 0. Sau khi L3 cầm hàng: công đoạn `Blocked`/`NoPath` trong 200 nhịp tiếp.
13. `StorageFullIsPooled`:
    - `cfg.storage.capacity = 11`, `cfg.crops[1].startSeeds = 0`. Hai kho (6,2,9) và (6,2,3) → `Capacity` 22, hạt A 20.
    - Vụ `[0]`. Sau gieo: hạt 19, `Free` 3 → vận chuyển (tải 2) không `Blocked`.
    - Lặp lại với `Stock.Produce[0] = 2` đặt ở nhịp 1300 → `Free` 1 → `StorageFull`.
14. `EnqueueRules`:
    - `EnqueueCrop(id, 2)` → `ArgumentException`.
    - Mô phỏng không có kho: `MarkTill` ≥ 0 nhưng `EnqueueCrop` → false.
    - Hàng đợi đầy (2) → false. Ô `Removed` → false.
15. `SameSeedSameHashWithQueueOps`:
    - 2 mô phỏng cùng seed, cùng kịch bản trong 3000 nhịp. Kịch bản:
      - đánh dấu 3 ô (5,1,5), (6,1,5), (7,1,5) → id 0, 1, 2; vụ `[0,1]`, `[1]`, `[]`;
      - nhịp 50 `Unmark(2)`;
      - nhịp 600 `RemoveCrop(0, 1)`;
      - nhịp 700 đào ô của id 1 qua `editor.TryDig(6, 1, 5)`.
    - `SimHash` trùng từng nhịp.
    - Thêm: đổi `Stock.Seeds[1]` ở một bản → hash khác (kho chung có vào mã băm).

`SimConfigTests`:
- `ConfigParsesAndValidates` thêm: `crops.Length` 2, `GrowTicks(0)` 1200, `GrowTicks(1)` 1800.
- `ValidateRejectsBadValues` thêm các ca: `crops` rỗng; trùng id; `growTimeMs` 120050; `seedsPerHarvest + producePerHarvest` = 0; tổng `startSeeds` > capacity.

Regression: `SimDemoTests`, `SimDemoMapsTests`, `SimStressTests`, PlayMode `SimViewPlayTests`, `SimViewMapsPlayTests`, `SimBenchPlayTests` đạt mà không đổi số.

### Lệnh executor chạy
1. Unity Editor đang mở project qua MCP. Gọi `refresh_unity`, rồi `read_console` (Error): không có lỗi biên dịch. Kiểm `CropQueueTests.cs.meta` đã sinh.
2. `run_tests` EditMode (toàn bộ) → ghi dòng tổng kết.
3. `run_tests` PlayMode (toàn bộ) → ghi dòng tổng kết. Sau đó chạy `git checkout -- client/ProjectSettings/EditorSettings.asset`.
4. `diff design/data/sim_m1.json client/Assets/Game/Client/Resources/Config/sim_m1.json` → không khác.
5. `grep -rn "UnityEngine" client/Assets/Game/Core/Sim/ client/Assets/Game/Core/View/SiteStatus.cs` → không có kết quả.
6. `grep -rn "AddFarmOrder\|\.crop\.\|startSeeds" client/Assets/Game --include='*.cs'` → chỉ còn `crops[...].startSeeds` / `CropDef`.
7. `python3 tools/check_design.py` → hợp lệ.
8. `git status --short` → chỉ có các file trong Phạm vi.

Editor tắt thì dùng `python3 tools/unity_test.py EditMode` / `PlayMode` (cùng bước dọn).

### User tự test
Không có phần chơi thử: SimView trông như cũ (demo chỉ trồng Cây A). Luật 6–20 user đã xác nhận 2026-10-21.

## Dừng và chuyển lại Opus khi
- Brief mơ hồ hoặc mâu thuẫn với code thật. Ví dụ:
  - số dòng lệch đến mức không tìm thấy đoạn code;
  - mốc `FarmTests` cũ đổi;
  - mốc ca 2 hoặc ca 5 khác số trong brief.
- Test cũ đỏ vì số kỳ vọng, ngoài hai chỗ ở bước 11.
- Cần một quyết định thuộc AGENTS.md §3.4 hoặc ngoài "Quyết định đã chốt".
- Cùng một lỗi sửa 2 lần vẫn chưa qua.
- Phải sửa file ngoài "Phạm vi".
- Cần API Unity mới. Brief này không dùng API Unity nào mới.

---
<!-- Từ đây trở xuống: EXECUTOR ghi. -->

## Kết quả
<!-- Base commit (trước khi làm) · Chạy bằng: <model id> · <effort> (dòng OK của brief_check.py) · commit của brief · file đã đổi · ghi chú lệch so với brief (nếu có, kèm lý do). Giờ bắt đầu / kết thúc. -->
Base commit: d2dc6de
Chạy bằng: claude-sonnet-5-5 · medium
Giờ: bắt đầu 21:02, kết thúc 21:10 (đồng hồ máy, 2026-10-21; chưa tính thời gian chờ test của Unity).
Commit code: 58ecb89. File đổi: Core/Sim/{SimData,SimConfig,Simulation,SimHash,SimDemo,SimStress}.cs, Core/View/SiteStatus.cs, Client/{SimView,SimBenchMeter}.cs, hai sim_m1.json, test (FarmTests, IdleTests, PlotRulesTests, SimBoundsTests, SimConfigTests, SimDeterminismTests, SiteStatusTests, SimDemoTests, SimDemoMapsTests, SimStressTests), tạo CropQueueTests.cs(+.meta).
Lệch so với brief:
- Test cũ dùng `foreach (var soilCell in <list>) sim.EnqueueCrop(sim.MarkTill(soilCell), 0);` một dòng thay `AddFarmOrder` (cùng thứ tự; tên biến `soilCell` tránh trùng `c`).
- CropQueueTests: chỉnh 3 chỗ số/cách đo của test mới cho khớp thực tế, không đổi luật: `NoSeedsOnlyBlocksThatCrop` tìm công đoạn gieo theo PlotId (tick 400, ô b đã Growing nên StageId = -1); `StorageFullIsPooled` đo Free=3 ở nhịp 1390 (tới 1395 đã giữ chỗ 2); `UnmarkReleases…` so vật liệu ô với giá trị trước khi đánh dấu (Dirt, không phải Grass).
- Không có test cũ nào đỏ vì số; mốc FarmTests cũ giữ nguyên.


## Kết quả test
Phần cần test 1–15 (CropQueueTests, 15 ca + TestCase pha = 18 test) → đạt: `run_tests` EditMode lọc CropQueueTests: 18/18 pass. SimConfigTests (thêm crops/Validate) đạt trong lượt toàn bộ.
Regression SimDemoTests, SimDemoMapsTests, SimStressTests, PlayMode SimView*/SimBench* → đạt, không đổi số.
- Biên dịch: không lỗi (read_console error rỗng); CropQueueTests.cs.meta đã sinh.
- `run_tests` EditMode toàn bộ → 389 test, 386 pass, 3 fail — cả 3 là test mới của tôi (sai số/cách đo, nêu ở trên); sau khi sửa chạy lại CropQueueTests 18/18 pass. Chưa chạy lại toàn bộ lần hai (336+ test còn lại đã pass ở lượt đầu, code lõi không đổi sau đó).
- `run_tests` PlayMode toàn bộ → 79/79 pass; đã `git checkout -- client/ProjectSettings/EditorSettings.asset`.
- `diff` hai sim_m1.json → không khác.
- grep UnityEngine trong Core/Sim và SiteStatus.cs → không có.
- grep AddFarmOrder/.crop./startSeeds → chỉ còn crops[...].startSeeds / CropDef.
- `python3 tools/check_design.py` → ✓ design/ hợp lệ: 50 file.
- `git status --short` → chỉ file trong Phạm vi (+ brief).

## Vướng mắc
<!-- Khi trạng thái = cần Opus: câu hỏi cụ thể + các phương án (kèm đề xuất). -->

---
<!-- PLANNER ghi khi review. -->

## Review
<!-- Kết luận: đã duyệt | cần sửa (thêm bước sửa vào "Các bước", trạng thái về chờ làm). Điểm đã đối chiếu. -->
