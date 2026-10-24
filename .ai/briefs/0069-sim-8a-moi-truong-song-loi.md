# Brief 0069 — Sim 8a: môi trường sống, lõi (đếm yêu cầu theo sự kiện, loài trở lại)

> Trạng thái: chờ review
> Review: đầy đủ
> Model thực thi: Sonnet · medium
> Cập nhật: 2026-10-24 10:35

Task contract: [m1-sim-8-moi-truong-song](../tasks/m1-sim-8-moi-truong-song.md) (phần 8a).

## Mục tiêu
Thêm lõi môi trường sống vào `Game.Core`, namespace mới `Game.Core.Habitat`:
- dữ liệu yêu cầu của từng loài đọc từ `design/data/habitat_m1.json`;
- bộ đếm ô cập nhật ngay theo `WorldEditor.CellChanged`;
- hàm xét yêu cầu;
- loài đủ yêu cầu → đánh dấu đã trở lại và trả về cho nơi gọi.

Client **chưa** dùng (8b). Không đổi hành vi game, không đổi `Simulation`.

## Bối cảnh cần đọc
- Thiết kế:
  - `design/04-tech/simulation-tech.md`, mục "Môi trường sống" (dòng 82-88).
  - `design/02-gameplay/mechanics.md`, mục "Sinh vật trở lại — luật M1" (dòng 87-90).
- Vật liệu ô: `client/Assets/Game/Core/Cell.cs:4-16`.
  - `enum BlockMaterial : byte { Empty = 0, Grass = 1, Dirt = 2, Stone = 3, Sand = 4, Water = 5, Bedrock = 6, Farmland = 7 }`.
  - Nước là ô có vật liệu `Water`. `WorldEditor` cũng ghi nước lan bằng `Cell.SetMaterial(old, BlockMaterial.Water)` (`WorldEditor.cs:238`).
- World:
  - `client/Assets/Game/Core/WorldData.cs`: `WorldData.Size` (`WorldSize` có `X, Y, Z`, `InBounds(x, y, z)`, `Index(x, y, z) => x + X * (z + Z * y)`, `CellCount`); `GetMaterial(int x, int y, int z)`.
  - Tạo world nhỏ trong test: xem cách `client/Assets/Game/Tests/Core/WorldEditorTests.cs` dựng world (đọc 30 dòng đầu).
- Sự kiện ô đổi: `client/Assets/Game/Core/WorldEditor.cs:61` `public event Action<int, int, int> CellChanged;`. Sự kiện phát sau khi ô đã đổi, khi:
  - đào (`:90`), đặt (`:131`);
  - xới `TryTill` (`:151`, ô thành `Farmland`);
  - mưa `TryGrow` (`:170`, đất → cỏ);
  - ô ruộng bị che trở lại đất (`:181`, ô bên dưới);
  - nước lan (`:239`, trong `StepWater`).
- Mô phỏng (chỉ đọc, không sửa) — `client/Assets/Game/Core/Sim/SimData.cs`:
  - `SimState` (`:291`): `Creatures` (`Creature.SpeciesId` string, `:76`), `Storages` (kho), `Houses`, `Plots`.
  - `FarmPlot` (`:233-260`): `CurrentCrop` (-1 = trống), `Phase` (`enum PlotPhase { Idle, Till, Sow, Water, Growing, Harvest }`, `:62`), `Removed`.
- Buổi:
  - `client/Assets/Game/Core/DayCycle/TimeConfig.cs:9` `enum DayPeriod : byte { Dawn = 0, Day = 1, Dusk = 2, Night = 3 }`.
  - `client/Assets/Game/Core/DayCycle/PeriodCondition.cs`: lớp `PeriodCondition` có `string[] periods`, `Validate()`, `IsMet(DayPeriod)`. Dùng lại lớp này, không viết lại.
- Mẫu cấu hình JSON:
  - `TimeConfig.cs`: lớp `[Serializable]`, field public có giá trị mặc định **bằng đúng** JSON, `Validate()` ném `ArgumentException`.
  - Test đọc JSON: `client/Assets/Game/Tests/Core/DayCycle/TimeConfigTests.cs:11-29` (đọc `design/data/…` bằng `JsonUtility`, so với `new TimeConfig()` bằng `JsonUtility.ToJson`).
  - Bản sao client: `client/Assets/Game/Tests/World/ConfigTests.cs:34-43` (`TimeClientCopyMatchesDesignData`).
  - Nạp phía client: `client/Assets/Game/Client/ConfigLoader.cs:16-17`, `:50-58` (`LoadTimeConfig`).
- Loài hiện có: `design/data/sim_m1.json:13-16` (`L1`, `L2`, `L3`).

## Quyết định đã chốt (không được đổi)
1. **Vị trí code:**
   - Thư mục `client/Assets/Game/Core/Habitat/`, namespace `Game.Core.Habitat`. Hai file `HabitatConfig.cs` và `HabitatTracker.cs`.
   - Test ở `client/Assets/Game/Tests/Core/Habitat/` (asmdef `Game.Core.Tests` có sẵn). Hai test cần bộ sinh world (`TerrainGenerator` / `WorldMapGenerator` nằm ở assembly `Game.World`, `client/Assets/Game/World/`, mà `Game.Core.Tests` không tham chiếu) đặt ở `client/Assets/Game/Tests/World/HabitatWorldTests.cs` (asmdef `Game.World.Tests`, namespace `Game.World.Tests`).
   - `Game.Core` không tham chiếu UnityEngine.
2. **Cấu hình** (`HabitatConfig`, JsonUtility; tên field đúng như dưới; mặc định trong code = JSON):
   ```csharp
   [Serializable] public class HabitatConfig {
       public int configVersion = 1;
       // Vùng đếm: hình chữ nhật cột [regionX0, regionX1) × [regionZ0, regionZ1), đủ chiều cao. Cả 4 = -1 → cả world.
       public int regionX0 = -1, regionZ0 = -1, regionX1 = -1, regionZ1 = -1;
       public string[] unlockedAtStart = { "L1", "L2", "L3" };
       public HabitatSpecies[] species = { /* L1, L2, L3 như JSON bên dưới */ };
       public void Validate();
   }
   [Serializable] public class HabitatSpecies { public string id; public HabitatRequirement[] requirements; }
   [Serializable] public class HabitatRequirement {
       public string kind;        // "cells" | "crops" | "structures" | "species" | "period"
       public int min = 1;        // ngưỡng: đạt khi số đếm >= min (kind "period" bỏ qua min)
       public string material = "";   // cells: tên BlockMaterial, hoặc "Solid" = mọi vật liệu khác Empty và Water
       public bool exposed;       // cells: ô ngay trên là Empty hoặc ngoài world
       public bool nearWater;     // cells: ≥ 1 trong 8 ô (4 ô ngang cùng tầng y + 4 ô ngang tầng y+1) có vật liệu Water
       public int minY;           // cells: chỉ tính ô có y >= minY ("có điểm cao ≥ y")
       public int cropId = -1;    // crops: -1 = mọi loại cây
       public string structure = "";  // structures: "storage" | "house"
       public string species = "";    // species: id loài
       public string[] periods = new string[0]; // period: như PeriodCondition
   }
   ```
   - `Validate()` ném `ArgumentException` khi: `configVersion != 1`; vùng không hợp lệ (không phải cả 4 = -1, mà có giá trị âm hoặc X0 ≥ X1 hoặc Z0 ≥ Z1); id loài trùng hoặc rỗng; `unlockedAtStart` có id không nằm trong `species`; `kind` lạ; `min < 1` (trừ `period`); `material` không phải tên `BlockMaterial` hoặc "Solid" (với `cells`); `structure` lạ; `species` rỗng (với kind `species`); `periods` sai (gọi `new PeriodCondition(periods).Validate()` với kind `period`).
   - Vùng vượt kích thước world → `HabitatTracker` ném `ArgumentException` lúc tạo (config không biết world).
3. **File `design/data/habitat_m1.json`** (chép nguyên văn; bản sao byte-giống ở `client/Assets/Game/Client/Resources/Config/habitat_m1.json`). Số là số tạm, chưa chốt (task contract):
   ```json
   {
       "configVersion": 1,
       "regionX0": -1,
       "regionZ0": -1,
       "regionX1": -1,
       "regionZ1": -1,
       "unlockedAtStart": ["L1", "L2", "L3"],
       "species": [
           { "id": "L1", "requirements": [
               { "kind": "cells", "min": 20, "material": "Grass", "exposed": false, "nearWater": false, "minY": 0, "cropId": -1, "structure": "", "species": "", "periods": [] },
               { "kind": "crops", "min": 1, "material": "", "exposed": false, "nearWater": false, "minY": 0, "cropId": -1, "structure": "", "species": "", "periods": [] }
           ] },
           { "id": "L2", "requirements": [
               { "kind": "cells", "min": 10, "material": "Grass", "exposed": false, "nearWater": true, "minY": 0, "cropId": -1, "structure": "", "species": "", "periods": [] }
           ] },
           { "id": "L3", "requirements": [
               { "kind": "cells", "min": 15, "material": "Stone", "exposed": true, "nearWater": false, "minY": 0, "cropId": -1, "structure": "", "species": "", "periods": [] },
               { "kind": "structures", "min": 1, "material": "", "exposed": false, "nearWater": false, "minY": 0, "cropId": -1, "structure": "storage", "species": "", "periods": [] }
           ] }
       ]
   }
   ```
   JsonUtility ghi lại phải ra đúng nội dung này; test so `JsonUtility.ToJson(new HabitatConfig())` với `JsonUtility.ToJson(đọc từ file)`, như `TimeConfigTests`.
4. **Đếm ô theo sự kiện** (`HabitatTracker`):
   - Mỗi yêu cầu `cells` có một mảng bit (`ulong[]`) trên mọi ô của vùng và một số đếm `int`.
   - Tạo tracker → quét vùng **một lần**: với mỗi ô thoả điều kiện thì bật bit và tăng số đếm.
   - `OnCellChanged(x, y, z)` (chữ ký khớp `Action<int,int,int>`, nơi gọi tự `editor.CellChanged += tracker.OnCellChanged`): tính lại điều kiện cho đúng các ô bị ảnh hưởng, nằm trong vùng và trong world. Bit đổi thì cộng/trừ số đếm. Không quét lại cả vùng.
   - Các ô bị ảnh hưởng: ô (x, y, z); ô dưới (x, y−1, z), vì `exposed` của ô dưới đổi; 4 ô ngang cùng tầng y và 4 ô ngang tầng y−1, vì `nearWater` của chúng đổi. Tổng 10 ô.
   - Ô ruộng là `Farmland`, không phải `Grass`/`Dirt`, nên tự nhiên không tính (0020/0025). Không cần code riêng, nhưng phải có test.
5. **Yêu cầu không phải `cells`:** tính trực tiếp từ `SimState` lúc xét, không cần sự kiện (vì số ô ruộng / kho / sinh vật ít):
   - `crops` = số `FarmPlot` có `!Removed && CurrentCrop >= 0 && (Phase == Water || Phase == Growing || Phase == Harvest)` và (`cropId == -1` hoặc `CurrentCrop == cropId`). Ý nghĩa: vụ đã gieo.
   - `structures`: "storage" = `Storages.Count`, "house" = `Houses.Count`.
   - `species` = số `Creature` có `SpeciesId == species`.
   - `period`: đạt khi `PeriodCondition.IsMet(period hiện tại)`.
6. **API công khai** (đúng tên/chữ ký):
   ```csharp
   public sealed class HabitatTracker {
       public HabitatTracker(HabitatConfig config, WorldData world); // gọi config.Validate(); quét vùng
       public void OnCellChanged(int x, int y, int z);
       public int SpeciesCount { get; }                 // = config.species.Length
       public string SpeciesId(int s);
       public int RequirementCount(int s);
       public int Current(int s, int r, SimState state, DayPeriod period); // số đếm hiện tại; period: 1 = đạt, 0 = chưa
       public int Target(int s, int r);                 // min; period → 1
       public bool IsMet(int s, int r, SimState state, DayPeriod period);
       public bool IsUnlocked(int s);                   // lúc tạo: true nếu id nằm trong unlockedAtStart
       public int Update(SimState state, DayPeriod period, List<string> returned); // xét loài chưa mở theo thứ tự config; loài có MỌI yêu cầu đạt → mở, thêm id vào returned; trả số loài vừa mở
   }
   ```
   - Loài đã mở không bao giờ khoá lại, kể cả khi yêu cầu không còn đạt.
   - `Update` không tạo sinh vật; việc đó làm ở 8b.
   - Loài không có yêu cầu nào → không bao giờ tự mở (tránh mở do config sai). `Validate` cho phép `requirements` rỗng.
7. **Client:** chỉ thêm `ConfigLoader.HabitatM1Path = "Config/habitat_m1"` và `LoadHabitatConfig()` (theo mẫu `LoadTimeConfig`). Không nối vào `SimView` hay scene (8b).
8. Không sửa `Simulation.cs`, `SimData.cs`, `SimHash.cs`, `WorldEditor.cs` → mã băm mô phỏng/world không đổi.

## Phạm vi
- Được sửa/tạo:
  - `client/Assets/Game/Core/Habitat/` (mới: `HabitatConfig.cs`, `HabitatTracker.cs` + `.meta` do Unity tạo);
  - `client/Assets/Game/Tests/Core/Habitat/` (mới);
  - `client/Assets/Game/Tests/World/HabitatWorldTests.cs` (mới);
  - `design/data/habitat_m1.json` (mới);
  - `client/Assets/Game/Client/Resources/Config/habitat_m1.json` (mới, bản sao);
  - `client/Assets/Game/Client/ConfigLoader.cs` (thêm hằng + hàm);
  - `client/Assets/Game/Tests/World/ConfigTests.cs` (thêm 1 test bản sao);
  - brief này (Kết quả trở xuống).
- Không được sửa:
  - mọi file khác, kể cả `design/*.md`: Planner cập nhật tài liệu khi review.
  - Working tree có thay đổi chưa commit của phiên khác (`design/02-gameplay/*.md`, `design/05-roadmap.md`, `design/glossary.md`, `design/ideas.md`, `design/summary.md`, `design/decisions/README.md`, `design/decisions/0038-*`, `tools/terrain_catalog*`, `.claude/skills/local-terrain-gen/`, `.ai/skill-map.local.json`): không sửa, không stage, không khôi phục. Commit bằng `git add <file cụ thể>`.

## Các bước
1. **Cấu hình:**
   - Tạo `HabitatConfig.cs` (QĐ 2) và `design/data/habitat_m1.json` (QĐ 3); chép bản sao client.
   - Thêm `ConfigLoader.HabitatM1Path` + `LoadHabitatConfig()` (QĐ 7).
   - Thêm test `HabitatClientCopyMatchesDesignData` vào `ConfigTests.cs` (theo mẫu `TimeClientCopyMatchesDesignData`).
   - Xong khi: biên dịch được, console 0 lỗi.
2. **Tracker:** tạo `HabitatTracker.cs` (QĐ 4–6).
   - Chỉ số bit của ô (x, y, z) trong vùng: `(x − x0) + w * ((z − z0) + d * y)`, với `w = x1 − x0`, `d = z1 − z0`.
   - Hàm điều kiện của yêu cầu `cells` cho một ô là hàm tĩnh dùng chung cho cả quét đầu lẫn `OnCellChanged`, để hai đường luôn khớp.
3. **Test EditMode**, `client/Assets/Game/Tests/Core/Habitat/HabitatConfigTests.cs` + `HabitatTrackerTests.cs`. Mỗi test một ý, tên như dưới:
   - `ConfigFileMatchesDefaults`: đọc `design/data/habitat_m1.json` → `Validate()` không ném; `ToJson` bằng `ToJson(new HabitatConfig())`.
   - `ValidateRejectsBadConfig`: mỗi lỗi ở QĐ 2 một `Assert.Throws<ArgumentException>`.
   - `InitialScanCountsCells`: world nhỏ dựng tay (vd 16 × 8 × 16), đặt sẵn số ô cỏ / đá lộ thiên / đá bị che / cỏ cạnh nước biết trước → `Current` đúng từng yêu cầu `cells`. Gồm `exposed`, `nearWater` (nước cùng tầng và nước tầng trên), `minY`, "Solid".
   - `RegionLimitsCounting`: vùng hình chữ nhật nhỏ hơn world → ô ngoài vùng không tính; vùng vượt world → tracker ném `ArgumentException`.
   - `DigPlaceUpdatesImmediately`: `WorldEditor` + `editor.CellChanged += tracker.OnCellChanged`. Đặt 1 ô cỏ rồi đào → số đếm đổi ngay sau mỗi lệnh (không cần `Update`/nhịp). Đặt đá lên ô đá lộ thiên → số đá lộ thiên giảm 1.
   - `TilledGrassNotCounted`: `editor.TryTill` trên ô cỏ → số cỏ giảm 1. `editor.TryGrow` trên ô đất trống phía trên → số cỏ tăng 1.
   - `WaterSpreadUpdatesNearWater`: đào thông ra nguồn nước, gọi `editor.SettleWater()` → số "cỏ cạnh nước" khớp quét lại.
   - `IncrementalMatchesFullRescan` (file `Tests/World/HabitatWorldTests.cs`): world từ `TerrainGenerator.Generate(new TerrainParams())` (như `IncrementalRebuildTests`), `System.Random(12345)`, 300 lần đào/đặt/xới/mưa hợp lệ ngẫu nhiên, có `SettleWater()` sau mỗi 20 lần. Sau mỗi 30 lần: với mọi yêu cầu, `Current` của tracker theo sự kiện == `Current` của một tracker mới tạo trên cùng world.
   - `NonCellRequirements`: `SimState` dựng tay hoặc từ một `Simulation` nhỏ như test sim hiện có (đọc `client/Assets/Game/Tests/Core/Sim/FarmTests.cs` 40 dòng đầu để lấy cách dựng). Kiểm `crops` (vụ chưa gieo = 0, đã gieo = 1, `cropId` lọc đúng, `Removed` không tính), `structures`, `species`, `period` (đúng buổi = đạt, buổi khác = chưa).
   - `UnlockOnceInConfigOrder`: config tự dựng có 3 loài, không loài nào mở sẵn. Hai loài đạt cùng lúc → `Update` trả 2, `returned` theo thứ tự config. Gọi `Update` lần nữa → trả 0. Phá yêu cầu → `IsUnlocked` vẫn true. Loài có `requirements` rỗng không bao giờ mở.
   - `UnlockedAtStartRespected`: config mặc định → cả 3 loài `IsUnlocked` true, `Update` trả 0.
   - `Deterministic`: hai world giống nhau + cùng chuỗi lệnh → mọi `Current` giống hệt.
   - `ScanTimeOnM1World` (file `Tests/World/HabitatWorldTests.cs`; đo, không phải tiêu chí cứng): world M1 nhiều map 384 × 48 × 384, dựng như `client/Assets/Game/Tests/World/WorldMapGeneratorTests.cs` (cách gọi `WorldMapGenerator` + nạp `design/data/world_maps_m1.json` trong file đó). Tạo tracker với config mặc định, ghi `Debug.Log` (Game.Core.Tests có UnityEngine) số ms quét và số đếm ban đầu từng yêu cầu. `Assert.Less(ms, 2000)`.
4. `Game.Core` không có `using UnityEngine`: `grep -rn "UnityEngine" client/Assets/Game/Core/Habitat` → rỗng.

## Tiêu chí xong
- Mọi test ở bước 3 qua. EditMode toàn bộ qua (trước brief: 582 test). PlayMode toàn bộ qua (267).
- Console 0 Error sau biên dịch.
- `git diff --stat` chỉ có file trong Phạm vi; không đụng `Simulation.cs` / `SimData.cs` / `SimHash.cs` / `WorldEditor.cs`.
- Kết quả ghi số đo của `ScanTimeOnM1World` (ms + số đếm ban đầu L1/L2/L3). Planner dùng số này để hỏi user câu (a).

## Test sau khi xong

### Phần cần test
- Cấu hình + bản sao (bước 1): `ConfigFileMatchesDefaults`, `ValidateRejectsBadConfig`, `ConfigTests.HabitatClientCopyMatchesDesignData` → qua.
- Quét đầu (QĐ 4): `InitialScanCountsCells`, `RegionLimitsCounting` → qua.
- Đếm theo sự kiện, "đào/đặt → tiến độ đổi ngay": `DigPlaceUpdatesImmediately`, `TilledGrassNotCounted`, `WaterSpreadUpdatesNearWater`, `IncrementalMatchesFullRescan` → qua.
- Yêu cầu từ mô phỏng (QĐ 5): `NonCellRequirements` → qua.
- "Đủ yêu cầu → loài trở lại" (QĐ 6): `UnlockOnceInConfigOrder`, `UnlockedAtStartRespected` → qua.
- Tất định: `Deterministic` → qua.
- Hiệu năng quét đầu: `ScanTimeOnM1World` → qua, ghi số.
- `ConfigLoader.LoadHabitatConfig()` không có test riêng (client chưa dùng). Biên dịch được là đủ; 8b sẽ dùng và test.
- Regression (không đổi sim/world): EditMode toàn bộ (Sim/**, `WorldEditorTests`, `ConfigTests`, `GoldenHashTests`…) + PlayMode toàn bộ → qua hết.
- No Engine References: grep bước 4 → rỗng.

### Lệnh executor chạy
1. MCP `refresh_unity` → `read_console` (Error) = 0.
2. MCP `run_tests` EditMode lọc `Game.Core.Tests.Habitat` (hoặc namespace thực tế) + `Game.World.Tests.HabitatWorldTests` + `Game.World.Tests.ConfigTests`.
3. MCP `run_tests` EditMode toàn bộ; PlayMode toàn bộ. Ghi dòng tổng.
4. `grep -rn "UnityEngine" client/Assets/Game/Core/Habitat`.
5. Dọn: `git checkout -- client/ProjectSettings/EditorSettings.asset`. `git status --short` chỉ còn file của brief + thay đổi của phiên khác.

Ghi giờ bắt đầu/kết thúc vào Kết quả.

### User tự test
Không (8a không có phần nhìn thấy; user xem ở 8b).

## Dừng và chuyển lại Opus khi
- Brief mơ hồ hoặc mâu thuẫn với code thật (vd `CellChanged` không phát ở một đường đổi ô nêu trên, JsonUtility không ghi ra được đúng JSON ở QĐ 3).
- Cần một quyết định thuộc AGENTS.md §3.4 hoặc ngoài "Quyết định đã chốt".
- Cùng một lỗi sửa 2 lần vẫn chưa qua.
- Phải sửa file ngoài "Phạm vi".

---
<!-- Từ đây trở xuống: EXECUTOR ghi. -->

## Kết quả
Base commit: 98fec3c
Chạy bằng: claude-sonnet-5-5 · medium
Giờ: bắt đầu 10:21, kết thúc 10:35 (~14 phút, gồm chờ chạy cả bộ).
Commit: 26b9201. File: `Core/Habitat/HabitatConfig.cs`, `HabitatTracker.cs`; test `Tests/Core/Habitat/HabitatConfigTests.cs`, `HabitatTrackerTests.cs`, `Tests/World/HabitatWorldTests.cs`; `design/data/habitat_m1.json` + bản sao client; `ConfigLoader.cs` (+`HabitatM1Path`, `LoadHabitatConfig`); `ConfigTests.cs` (+1 test). Không đụng Simulation/SimData/SimHash/WorldEditor.
Lệch brief (ghi để Planner biết):
- `DigPlaceUpdatesImmediately`: brief ghi "đặt đá lên ô đá lộ thiên → số đá lộ thiên giảm 1", nhưng ô đá mới đặt cũng lộ thiên nên số đếm giữ nguyên (ô cũ −1, ô mới +1). Test kiểm: đặt **đất** lên đá lộ thiên → −1; đặt đá → giữ nguyên; đào lại → khớp. Hành vi tracker đúng theo QĐ 4.
- Nhà `AddHouse((2,2,2))` đặt được trên `Flat(1)`, dùng trong `NonCellRequirements`.

## Kết quả test
- Cấu hình + bản sao: đạt — `ConfigFileMatchesDefaults`, `ValidateRejectsBadConfig` (19 ca), `ConfigTests.HabitatClientCopyMatchesDesignData`.
- Quét đầu: đạt — `InitialScanCountsCells`, `RegionLimitsCounting`.
- Đếm theo sự kiện: đạt — `DigPlaceUpdatesImmediately`, `TilledGrassNotCounted`, `WaterSpreadUpdatesNearWater` (nước lan >10 ô, khớp quét lại), `IncrementalMatchesFullRescan` (300 sửa, 10 lần so, world sinh thật).
- Yêu cầu từ mô phỏng: đạt — `NonCellRequirements`.
- Loài trở lại: đạt — `UnlockOnceInConfigOrder`, `UnlockedAtStartRespected`.
- Tất định: đạt — `Deterministic`.
- Hiệu năng: đạt — `ScanTimeOnM1World`: **156 ms** trên 384×48×384 (seed 20261013, config mặc định). Số đếm ban đầu: L1[cỏ]=48077/20, L1[vụ gieo]=0/1, L2[cỏ cạnh nước]=0/10, L3[đá lộ thiên]=0/15, L3[kho]=0/1. Lưu ý cho câu (a): trên world M1 mới sinh có rất nhiều cỏ nhưng **0 ô cỏ cạnh nước** và **0 ô đá lộ thiên** theo định nghĩa hiện tại (cạnh nước = 8 ô quanh, đá lộ thiên = ô trên là Empty) — L2/L3 sẽ không đạt cho tới khi người chơi tạo ra, hoặc cần chỉnh ngưỡng/định nghĩa.
- 22 test mới/chạm tới qua (4 lớp lọc). Regression: EditMode cả bộ 596/596 (582 + 14 mới, 284 s); PlayMode cả bộ 267/267 (171 s); console 0 Error.
- `grep UnityEngine Core/Habitat`: rỗng. Dọn: `git checkout -- EditorSettings.asset`; không có InitTestScene*.

## Vướng mắc

---
<!-- PLANNER ghi khi review. -->

## Review
