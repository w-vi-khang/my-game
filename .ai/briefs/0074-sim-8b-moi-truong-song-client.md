# Brief 0074 — Sim 8b: môi trường sống phía client (vùng 48 × 48, chỉ mở L1, ao, bảng yêu cầu, loài trở lại)

> Trạng thái: chờ làm
> Review: đầy đủ + user xem
> Model thực thi: Sonnet · medium
> Cập nhật: 2026-10-25 — Planner viết

Task contract: [m1-sim-8-moi-truong-song](../tasks/m1-sim-8-moi-truong-song.md) (phần 8b). Quyết định: [0039](../../design/decisions/0039-moi-truong-song-m1-vung-dem-loai-mo-san.md).

## Mục tiêu
Nối lõi môi trường sống (brief 0069, `Game.Core.Habitat`) vào scene SimView trên world nhiều map:
- vùng đếm = đồng bằng demo 48 × 48;
- demo bắt đầu chỉ có L1; trại có thêm nhà thứ ba; có ao 4 × 4 viền cát;
- bảng yêu cầu mở bằng phím H (đổi được trong bảng đổi phím);
- loài đạt đủ yêu cầu → 2 cá thể xuất hiện ở ô rìa vùng gần người chơi nhất, tự về nhà, nhận việc; có thông báo.

Đúng cột "Xong khi" dòng 8: "Đào/đặt → tiến độ đổi ngay; đủ yêu cầu → loài trở lại".

## Bối cảnh cần đọc
- Lõi đã có (brief 0069, đã duyệt):
  - `client/Assets/Game/Core/Habitat/HabitatConfig.cs`: field `regionX0/regionZ0/regionX1/regionZ1` (−1 = cả world), `unlockedAtStart` (dòng 98), `species[]`, `HabitatRequirement` (`kind` "cells" | "crops" | "structures" | "species" | "period"; `material`, `exposed`, `nearWater`, `minY`, `cropId`, `structure`, `species`, `periods`; hằng `HabitatRequirement.KindPeriod`…).
  - `client/Assets/Game/Core/Habitat/HabitatTracker.cs`:
    ```csharp
    public HabitatTracker(HabitatConfig config, WorldData world);
    public void OnCellChanged(int x, int y, int z);
    public int SpeciesCount { get; }
    public string SpeciesId(int s);
    public int RequirementCount(int s);
    public int Current(int s, int r, SimState state, DayPeriod period);
    public int Target(int s, int r);
    public bool IsMet(int s, int r, SimState state, DayPeriod period);
    public bool IsUnlocked(int s);
    public int Update(SimState state, DayPeriod period, List<string> returned);
    ```
    Tracker không trả cấu hình của từng yêu cầu. Bảng cần `HabitatRequirement` để ghi nhãn, nên lấy từ `config.species[s].requirements[r]` (giữ tham chiếu `HabitatConfig` ở SimView).
  - `client/Assets/Game/Client/ConfigLoader.cs:63-71` `LoadHabitatConfig()`.
  - `design/data/habitat_m1.json` + bản sao `client/Assets/Game/Client/Resources/Config/habitat_m1.json`. Test `HabitatConfigTests.ConfigFileMatchesDefaults` so file với `new HabitatConfig()`; `ConfigTests.HabitatClientCopyMatchesDesignData` so hai file.
  - Test cần sửa: `client/Assets/Game/Tests/Core/Habitat/HabitatTrackerTests.cs:229-239` `UnlockedAtStartRespected` (đang mong cả 3 loài mở sẵn).
- Scene SimView: `client/Assets/Game/Client/SimView.cs`:
  - `Awake` :74-83 (thêm component phụ như `WorldIconsView` ở :82);
  - `OnAfterGenerate` :87-105 (san đồng bằng, chạy **trước** khi dựng mesh — chỗ đặt ao);
  - `OnDestroy` :107;
  - vòng nhịp trong `Update` :130-140 (`SimDemo.KeepFarming` → `Sim.Tick()` → `interp.Capture` …);
  - `Setup`, nhánh world nhiều map :176-187 (`SimDemo.Build(Sim, plain)` ở :185);
  - `BlocksPointer` :393;
  - sinh vật mới thêm giữa chừng đã được xử lý sẵn: `UpdateBodies` (:217) tạo thân, `SimInterpolator.Capture` (`Core/View/SimInterpolator.cs:41-50`) thêm track, `WorldIconsView` (:198) thêm icon.
- Bố trí demo: `client/Assets/Game/Core/Sim/SimDemo.cs`:
  - `PlainHouseA/B`, `PlainStorage`, `PlainSoil`, `PlainCreatureCells` (:24-31); `OnPlain(plain, rel)` (:34-35);
  - `Build(sim, plain)` (:41-42) và hàm `Build` riêng (:44-63).
  - Nhà: `sim_m1.json` `"house": { "capacity": 2, "sizeX": 2, "sizeZ": 2, "sizeY": 2 }`; `House.Residents`, `House.Capacity` (`Core/Sim/SimData.cs:269-284`).
- Đồng bằng: `client/Assets/Game/Core/Sim/SimStress.cs`:
  - `StressPlain` (:7-14): `X0, Z0, Size` (= 48), `Ground` (y của ô cỏ mặt), `Bounds` (vùng mô phỏng 64 × 64).
  - `FlattenPlain` (:22-70): ghi thẳng `world.Set(x, y, z, Cell.Make(...))`, không bật cờ đã sửa.
- Ô: `client/Assets/Game/Core/Cell.cs`: `Cell.Make(BlockMaterial material, int waterLevel = 0)` (:34), `Cell.MaxWaterLevel = 7` (:24).
- Nước: `WorldEditor` chỉ cho nước lan khi đào ô có `y <= SeaLevel` (`Core/WorldEditor.cs:91`). Mặt đồng bằng cao hơn mực nước ít nhất 2 ô (`SimStress.cs:52`), nên ao trên mặt đồng bằng đứng yên, không lan.
- Mô phỏng:
  - `client/Assets/Game/Core/Sim/Simulation.cs`: `AddCreature` :55-75 (ném `ArgumentException` khi không đứng được hoặc trùng sinh vật); `AssignHome` :155; `FramesOverlap` :476.
  - Sinh vật rảnh có nhà, ở ngoài `homeRadius` (= 4) → tự đi về quanh nhà (`IdlePhase` :215-238). Đây là "tự về nhà" của QĐ 5.
- Đặt / đào trong test: `WorldEditor.TryDig(x, y, z)` (:78), `TryPlace(x, y, z, material)` (:120) — cần khối trong `editor.Bag` (`PlayerBag.Add(BlockMaterial, int)`, `Core/PlayerBag.cs:89`); `TryGrow(x, y, z)` (:167, đất → cỏ).
- Buổi hiện tại: `DayCycleView.Day` (`Client/DayCycleView.cs:23`, kiểu class `TimeOfDay`, có thể null) → `.Period` (`Core/DayCycle/TimeOfDay.cs:26`). Tìm view bằng `Object.FindAnyObjectByType<DayCycleView>()` (mẫu `Client/TimeDial.cs:225`).
- Phím và bảng đổi phím (7l f):
  - `client/Assets/Game/Client/Input/GameControls.inputactions`: map `Player`, action `ToggleSpellMenu` (:117-124) + binding `<Keyboard>/g` (:420-431).
  - `client/Assets/Game/Client/GameInput.cs:48-49` (`ToggleMap`, `ToggleSpellMenu`).
  - `client/Assets/Game/Client/KeyRebind.cs:14-50` `NameKeys`.
  - Test: `client/Assets/Game/Tests/ClientEdit/GameControlsTests.cs:46-47` (bảng binding mong đợi), `:74` và `:103` (`43`); `client/Assets/Game/Tests/ClientEdit/KeyRebindTests.cs:16-20` (`Rebindable_Is34_ButtonsOnly_NoEscape`, `34`), `:33` (`34`).
  - Phím đang dùng: 0–9, a, b, d, e, f, g, m, o, p, q, r, shift, space, tab, v, x, escape. **H còn trống.**
- Bảng IMGUI mẫu: `client/Assets/Game/Client/SettingsView.cs`:
  - `BlocksPointer` / `Covers` :39-47; `OtherPanelOpen` :69-75 (Esc mở Cài đặt khi không có bảng nào mở);
  - `GuideText` :165-172; `OnGUI` :175-185 (`UiScale.Gui`, `GUI.matrix`, ẩn khi `MinimapView.IsAnyOpen`);
  - `T(k)` / `Format(k, …)` :162-163 (`GameSettings.Text`).
  - [docs] IMGUI 6000.4: [MonoBehaviour.OnGUI](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/MonoBehaviour.OnGUI.html), [GUI.Box](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/GUI.Box.html), [GUI.Label](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/GUI.Label.html). Input System 1.19: [InputAction](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.19/api/UnityEngine.InputSystem.InputAction.html), [Actions](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.19/manual/Actions.html). Chỉ dùng các API project đã dùng: `GUI.Box`, `GUI.Label`, `GUIStyle`, `InputAction.WasPressedThisFrame()`.
- Tên năng lực: `CreatureStatus.AbilityName(Strings t, WorkAbility a)` (`Core/View/CreatureStatus.cs:9-17`); `SimConfig.ParseAbility(string)`; `sim.Config.FindSpecies(id)` (`ability1`, `ability2`).
- Chuỗi giao diện: `client/Assets/Game/Client/Resources/Lang/vi.json`.
  - Khoá phải **xếp theo thứ tự Ordinal** (`HardcodedTextScanTests.BaseFileValid`).
  - Code client không được có chữ tiếng Việt (`NoVietnameseLiterals`).
  - Mọi khoá dùng dạng `T("…")` / `Format("…")` phải có trong vi.json (`AllUsedKeysExist`).
  - Khoá tên vật liệu có sẵn: `item.grass`, `item.dirt`, `item.stone`, `item.sand`.
- Test PlayMode dựng SimView trên world nhiều map: `client/Assets/Game/Tests/Client/SimViewMapsPlayTests.cs:17-38` (SetUp mẫu), `:42-60` (`SetsUpCampOnPlain`, đang mong 3 sinh vật).
- Test EditMode trên world sinh thật: `client/Assets/Game/Tests/World/SimDemoMapsTests.cs:13-31` (`Setup` → `WorldMapGenerator.Generate` + `SimStress.FlattenPlain`; `NewSim`).

## Quyết định đã chốt (không được đổi)
1. **Chỉ nhánh world nhiều map, không phải cảnh đo:** môi trường sống chỉ chạy khi `view.Maps != null && !stressBench && habitat`.
   - Thêm field `public bool habitat = true;` vào SimView.
   - `habitat == false` → giữ **đúng hành vi cũ**: `SimDemo.Build(Sim, plain)` có L1–L3, không ao, không tracker (`Habitat == null`).
   - Đảo v2 (`view.Maps == null`) và `stressBench` không đổi.
2. **Vùng đếm** = đồng bằng (0039 mục 1). Sau `LoadHabitatConfig()`, gán `regionX0 = plain.X0`, `regionZ0 = plain.Z0`, `regionX1 = plain.X0 + plain.Size`, `regionZ1 = plain.Z0 + plain.Size` rồi mới tạo `HabitatTracker`.
3. **`habitat_m1.json`:** chỉ đổi `"unlockedAtStart": ["L1"]` (cả file design lẫn bản sao client, byte-giống). Default trong `HabitatConfig.cs:98` đổi thành `{ "L1" }` cho khớp. Vùng giữ −1 (QĐ 2 gán lúc chạy).
4. **Ao** — hàm mới trong `SimDemo` (Game.Core, ghi thẳng như `FlattenPlain`, không bật cờ đã sửa):
   ```csharp
   /// <summary>Ao 4 × 4 viền cát (0039 mục 4): nước ở y = plain.Ground, vòng cát 1 ô quanh ao cùng tầng. Gọi sau FlattenPlain, trước khi dựng mesh.</summary>
   public static readonly CellPos PlainPondMin = new CellPos(10, 0, -12); // (dx, 0, dz) tính từ tâm đồng bằng như PlainSoil; [số tạm]
   public const int PondSize = 4;
   public static void PlacePond(WorldData world, StressPlain plain);
   ```
   - Ô nước: mọi (x, z) với `x ∈ [cx + 10, cx + 13]`, `z ∈ [cz − 12, cz − 9]`, trong đó `cx = plain.X0 + plain.Size / 2`, `cz = plain.Z0 + plain.Size / 2`. Tại `y = plain.Ground` ghi `Cell.Make(BlockMaterial.Water, Cell.MaxWaterLevel)`.
   - Vòng cát: các ô trong hình chữ nhật `[cx + 9, cx + 14] × [cz − 13, cz − 8]` không phải ô nước (20 ô). Tại `y = plain.Ground` ghi `Cell.Make(BlockMaterial.Sand)`.
   - Dưới ao vẫn là đất như cũ.
   - Có 16 ô cát kề ngang với nước (không tính 4 góc). L2 cần ≥ 10.
5. **Trại khi có môi trường sống** — hàm mới trong `SimDemo`; `Build(sim, plain)` cũ giữ nguyên:
   ```csharp
   public static readonly CellPos PlainHouseC = new CellPos(6, 1, 4); // [số tạm] (0039: nhà thứ ba)
   /// <summary>Bố trí giãn + nhà C; chỉ tạo sinh vật cho loài trong startSpecies (theo thứ tự Species, ô PlainCreatureCells tương ứng); nhà = nhà đầu tiên (A, B, C) còn chỗ. 4 ô ruộng + vụ Cây A như Build. Trả id kho.</summary>
   public static int BuildHabitat(Simulation sim, StressPlain plain, IReadOnlyList<string> startSpecies);
   ```
   Với `["L1"]`: 3 nhà (A, B, C), 1 kho, 1 sinh vật L1 ở `PlainCreatureCells[0]`, ở nhà A, 4 ô ruộng mỗi ô 1 vụ Cây A.
6. **Hàm sinh cá thể** — file mới `client/Assets/Game/Core/Habitat/HabitatSpawn.cs` (namespace `Game.Core.Habitat`, không UnityEngine):
   ```csharp
   public static class HabitatSpawn
   {
       public const int CountPerSpecies = 2; // luật M1: 2 cá thể
       /// <summary>Thêm tối đa count sinh vật speciesId ở ô rìa của vùng [x0, x1) × [z0, z1) gần (px, pz) nhất; mỗi con vào nhà đầu tiên còn chỗ (id tăng dần), hết chỗ thì không nhà. Ghi id mới vào created; trả số con đã thêm.</summary>
       public static int SpawnAtEdge(Simulation sim, int x0, int z0, int x1, int z1, int px, int pz, string speciesId, int count, List<int> created);
   }
   ```
   - Ô rìa = các cột (x, z) trong vùng có `x == x0 || x == x1 − 1 || z == z0 || z == z1 − 1`.
   - Xếp theo `(x − px)² + (z − pz)²` tăng dần; bằng nhau thì theo x rồi z (tất định).
   - Với mỗi cột: quét y từ trên xuống, tìm ô đầu tiên có vật liệu ≠ `Empty`. Nếu ô đó là `Water` thì bỏ cột. Ngược lại, ô đứng = (x, y + 1, z).
   - Đứng được và không trùng sinh vật khác → `sim.AddCreature`. Dừng khi đủ `count`.
   - Kiểm "đứng được" bằng method mới trong `Simulation`, đặt ngay trên `AddCreature`, dùng đúng hai điều kiện của `AddCreature` (`pathfinder.CanStand` + `FramesOverlap` với mọi sinh vật). **Không** đổi logic có sẵn trong `Simulation`.
     ```csharp
     /// <summary>AddCreature(speciesId, cell) có thành công không (sim 8b) — không đổi trạng thái.</summary>
     public bool CanPlaceCreature(string speciesId, CellPos cell)
     ```
   - Nhà: duyệt `sim.State.Houses` theo id, nhà đầu tiên có `Residents.Count < Capacity` → `sim.AssignHome(id, houseId)`.
7. **Nối vào SimView** (nhánh QĐ 1):
   - `OnAfterGenerate`, nhánh không phải bench: sau `Flatten`, nếu `habitat` → `SimDemo.PlacePond(world, plain)`.
   - `Setup`, nhánh nhiều map: `habitat` → `LoadHabitatConfig` → gán vùng (QĐ 2) → `SimDemo.BuildHabitat(Sim, plain, cfg.unlockedAtStart)` → `Habitat = new HabitatTracker(cfg, view.World)` → `view.Editor.CellChanged += Habitat.OnCellChanged`. Gỡ đăng ký trong `OnDestroy`. `Debug.Log` một dòng `HABITAT …` (vùng, loài mở sẵn, số đếm ban đầu từng yêu cầu).
   - Thuộc tính công khai: `public HabitatTracker Habitat { get; private set; }`, `public HabitatConfig HabitatConfig { get; private set; }`, `public IReadOnlyList<string> ReturnedSpecies` (loài đã trở lại trong phiên, theo thứ tự).
   - Vòng nhịp (:130-140): **trước** `Sim.Tick()` (sau dòng `KeepFarming`), nếu `Habitat != null` → `Habitat.Update(Sim.State, period, list)`. Với mỗi id trả về:
     - `HabitatSpawn.SpawnAtEdge(Sim, vùng, px, pz, id, HabitatSpawn.CountPerSpecies, …)`;
     - thêm id vào `ReturnedSpecies`;
     - gọi `HabitatPanelView.ShowReturned(id)`;
     - `Debug.Log("HABITAT returned …")` kèm số con và ô.
     - Sinh vật được thêm trước Tick, nên `interp.Capture` sau Tick tạo track cho chúng.
   - `(px, pz)` = `floor` của `PlayerCharacter.Active.FeetLocal` x/z. Không có người chơi → tâm đồng bằng.
   - `period` = `DayCycleView.Day.Period` nếu có view và `Day != null`; không có → `DayPeriod.Day`. Cache tham chiếu view, không tìm mỗi nhịp.
   - `BlocksPointer` (:393): thêm `|| HabitatPanelView.BlocksPointer(screen)`.
   - `Awake`: thêm `HabitatPanelView` như `WorldIconsView` (:82).
8. **Bảng yêu cầu** — file mới `client/Assets/Game/Client/HabitatPanelView.cs`, IMGUI theo mẫu SettingsView:
   - `public static bool IsAnyOpen`, `public bool IsOpen { get; private set; }`, `public void SetOpen(bool open)`, `public static bool BlocksPointer(Vector2 screen)`, `public static void ShowReturned(string speciesId)`, `public string Notice` (chữ thông báo đang hiện, "" khi không có).
   - `Update`:
     - `sim.Habitat == null` → đóng, return.
     - `GameInput.ToggleHabitat.WasPressedThisFrame()` và không mở Cài đặt (`SettingsView.IsAnyOpen`), không mở bản đồ (`MinimapView.IsAnyOpen`) → đảo `IsOpen`.
     - Đang mở và `GameInput.PanelsClose.WasPressedThisFrame()` → đóng.
   - Ẩn (không vẽ, `BlocksPointer` false) khi bản đồ hoặc Cài đặt đang mở.
   - Khung `PanelRect(gw, gh) = new Rect(gw − 376f, 230f, 360f, h)`, với `h = 44f + 24f * (số loài + tổng số yêu cầu) + 40f` (chỗ cho nút Đóng). [số tạm]
   - Nội dung, từng dòng:
     - tiêu đề `T("habitat.title")`;
     - mỗi loài: `Format("habitat.species", id, năng lực)` + `" · "` + trạng thái. Năng lực = hai tên `CreatureStatus.AbilityName` nối bằng `", "`. Trạng thái = `T("habitat.status_unlocked")` (màu xanh lá) hoặc `T("habitat.status_locked")`;
     - mỗi yêu cầu (thụt vào 16 px): `RowText(s, r)`, có màu xanh lá khi đạt;
     - nút `T("settings.close")` → đóng.
   - `public string RowText(int s, int r)` (không có thẻ màu):
     - `Format("habitat.row", RequirementLabel(req), Current, Target)`;
     - nếu đạt thì nối `" "` + `T("habitat.met")`.
     - Đọc `Current` / `IsMet` của tracker **ngay lúc gọi**, không cache → đào/đặt đổi số ngay cùng khung.
   - `public static string RequirementLabel(HabitatRequirement r)` — ghép bằng dấu cách, mỗi phần là một khoá:
     - `cells`: `Format("habitat.req_cells", tên vật liệu)`, rồi thêm `T("habitat.near_water")` nếu `nearWater`, `T("habitat.exposed")` nếu `exposed`, `Format("habitat.min_y", minY)` nếu `minY > 0`.
       - Tên vật liệu: `Grass` → `item.grass`, `Dirt` → `item.dirt`, `Stone` → `item.stone`, `Sand` → `item.sand`, `Water` → `habitat.mat_water`, `"Solid"` → `habitat.mat_solid`; còn lại → chuỗi `material`.
     - `crops`: `cropId < 0` → `T("habitat.req_crops")`; ngược lại → `Format("habitat.req_crops_n", cropId)`.
     - `structures`: `"storage"` → `T("habitat.req_storage")`; `"house"` → `T("habitat.req_house")`.
     - `species`: `Format("habitat.req_species", r.species)`.
     - `period`: `Format("habitat.req_period", string.Join(", ", r.periods))`.
   - Thông báo `Format("habitat.returned", id)`:
     - giữa trên màn hình, ngay dưới dòng FPS (y ≈ 40), hiện 4 giây [số tạm];
     - hiện cả khi bảng đóng; ẩn khi bản đồ mở;
     - nhiều loài cùng lúc → nối bằng `" · "`.
   - `SettingsView.OtherPanelOpen` (:69-75): thêm `|| HabitatPanelView.IsAnyOpen`, để Esc khi bảng đang mở chỉ đóng bảng, không mở Cài đặt.
9. **Phím H** (0039; đổi được trong bảng đổi phím):
   - action `ToggleHabitat` (Button) trong map `Player` của `GameControls.inputactions`, đặt ngay sau `ToggleSpellMenu`; binding `<Keyboard>/h`. `id` là GUID mới, không trùng id nào trong file.
   - `GameInput.ToggleHabitat => Get("Player/ToggleHabitat")`.
   - `KeyRebind.NameKeys["Player/ToggleHabitat"] = "controls.habitat"`.
   - Test: thêm `["Player/ToggleHabitat"] = B("<Keyboard>/h")` vào bảng ở `GameControlsTests`; `43` → `44` (2 chỗ). Trong `KeyRebindTests`, `34` → `35` (2 chỗ) và đổi tên test thành `Rebindable_Is35_ButtonsOnly_NoEscape`.
   - Hướng dẫn: `SettingsView.GuideText` thêm `T("guide.habitat")` ngay sau `T("guide.spells")`.
10. **Chuỗi mới trong `vi.json`** (chép nguyên văn; chèn đúng thứ tự Ordinal):

    | key | text |
    |---|---|
    | `controls.habitat` | `Bảng môi trường sống` |
    | `guide.habitat` | `H: bảng môi trường sống — yêu cầu để từng loài trở lại` |
    | `habitat.exposed` | `lộ thiên` |
    | `habitat.mat_solid` | `khối đặc` |
    | `habitat.mat_water` | `nước` |
    | `habitat.met` | `(đạt)` |
    | `habitat.min_y` | `(cao ≥ {0})` |
    | `habitat.near_water` | `cạnh nước` |
    | `habitat.req_cells` | `Ô {0}` |
    | `habitat.req_crops` | `Vụ đã gieo` |
    | `habitat.req_crops_n` | `Vụ đã gieo loại {0}` |
    | `habitat.req_house` | `Nhà` |
    | `habitat.req_period` | `Buổi: {0}` |
    | `habitat.req_species` | `Loài {0} đang ở` |
    | `habitat.req_storage` | `Kho` |
    | `habitat.returned` | `{0} đã trở lại!` |
    | `habitat.row` | `{0}: {1}/{2}` |
    | `habitat.species` | `{0} ({1})` |
    | `habitat.status_locked` | `chưa trở lại` |
    | `habitat.status_unlocked` | `đã ở trại` |
    | `habitat.title` | `Môi trường sống` |

    `loc/en.json` và `loc/context.json` không sửa (việc dịch để sau, như 43 khoá của 7l f).
11. **Test PlayMode cũ phụ thuộc đủ L1–L3** — thêm `sv.habitat = false;` ngay sau `AddComponent<SimView>()` trong SetUp của `PlayerFarmPlayTests`, `ControlModePlayTests`, `PlayerBagSowPlayTests`. Đây là các test tưới / thu / Tiếp tục trên world nhiều map.
    - Test khác trên world nhiều map hỏng chỉ vì số sinh vật / nhà / loài → được thêm `habitat = false` tương tự. Ghi tên test vào Kết quả.
    - Hỏng vì lý do khác → không che bằng cách này; dừng, `cần Opus`.
    - `SimViewMapsPlayTests.SetsUpCampOnPlain` kiểm bố trí mặc định mới: 1 sinh vật, 1 thân, 3 nhà. Sửa số mong đợi ở :50-51; giữ các assert khác.
12. Không sửa `SimHash.cs`, `WorldEditor.cs`, `SimData.cs`. Trong `Simulation.cs` chỉ **thêm** `CanPlaceCreature`. `SimDemo.Build` cũ giữ nguyên → `GoldenHashTests` và `SimDemoMapsTests` cũ không đổi.

## Phạm vi
- Được sửa/tạo:
  - `client/Assets/Game/Core/Habitat/HabitatSpawn.cs` (mới), `HabitatConfig.cs` (chỉ default `unlockedAtStart`);
  - `client/Assets/Game/Core/Sim/SimDemo.cs` (thêm `PlainHouseC`, `PlainPondMin`, `PondSize`, `PlacePond`, `BuildHabitat`);
  - `client/Assets/Game/Core/Sim/Simulation.cs` (chỉ thêm `CanPlaceCreature`);
  - `client/Assets/Game/Client/SimView.cs`, `HabitatPanelView.cs` (mới), `SettingsView.cs` (`OtherPanelOpen`, `GuideText`), `GameInput.cs`, `KeyRebind.cs`, `Input/GameControls.inputactions`, `Resources/Lang/vi.json`;
  - `design/data/habitat_m1.json` + `client/Assets/Game/Client/Resources/Config/habitat_m1.json`;
  - test:
    - mới: `client/Assets/Game/Tests/Core/Habitat/HabitatSpawnTests.cs`, `client/Assets/Game/Tests/World/HabitatDemoTests.cs`, `client/Assets/Game/Tests/Client/HabitatPlayTests.cs`;
    - sửa: `HabitatTrackerTests.cs` (`UnlockedAtStartRespected`), `GameControlsTests.cs`, `KeyRebindTests.cs`, `SimViewMapsPlayTests.cs`, các file ở QĐ 11;
  - `.meta` do Unity tạo cho file mới;
  - brief này (Kết quả trở xuống).
- Không được sửa: mọi file khác, kể cả `design/*.md` (Planner cập nhật khi review), `loc/`, scene `.unity`.

## Các bước
1. **Lõi** (QĐ 3–6, 12):
   - đổi `unlockedAtStart` ở JSON (2 file) + `HabitatConfig.cs`; sửa `UnlockedAtStartRespected` (mặc định → L1 mở, L2 và L3 chưa; `Update` trên `SimState` rỗng trả 0);
   - thêm `Simulation.CanPlaceCreature`, `SimDemo.PlacePond` / `BuildHabitat` / hằng, `HabitatSpawn`.
   - Xong khi: biên dịch được, console 0 Error.
2. **Test EditMode lõi** — `Tests/Core/Habitat/HabitatSpawnTests.cs`; world phẳng dựng tay + `Simulation` như `NonCellRequirements` trong `HabitatTrackerTests`:
   - `SpawnsAtNearestEdgeCells`: vùng 16 × 16, điểm (px, pz) ở gần cạnh −z → 2 con ở `z == z0`, gần (px, pz) nhất; chạy 2 lần → cùng ô (tất định).
   - `SkipsBlockedAndWaterColumns`: cột rìa gần nhất có nước trên cùng, hoặc đã có sinh vật → bỏ qua, lấy cột kế.
   - `AssignsFirstHouseWithRoom`: 2 nhà, nhà 0 còn 1 chỗ → con 1 vào nhà 0, con 2 vào nhà 1; hết chỗ → `HomeId == -1`, vẫn thêm được.
   - `CanPlaceCreatureMatchesAddCreature`: ô đứng được → true và `AddCreature` không ném; ô trên không / ô có sinh vật → false và `AddCreature` ném.
3. **Test EditMode trên world thật** — `Tests/World/HabitatDemoTests.cs`, dựng như `SimDemoMapsTests.Setup`:
   - `PondPlacedWithSandRing`: sau `PlacePond`, 16 ô nước ở `y = Ground`, 20 ô cát, ô ngay ngoài vòng vẫn là cỏ; tracker vùng = đồng bằng + `habitat_m1.json`: L2 (cỏ cạnh nước) = 0.
   - `BuildHabitatL1Only`: 3 nhà, 1 sinh vật L1 ở nhà A, 4 ô ruộng.
   - `L2ReturnsAndJoinsWork`:
     - `BuildHabitat` + `PlacePond`; `WorldEditor` gắn sim + tracker; chạy 1500 nhịp (L1 xới, gieo; ô chờ tưới).
     - Thay 10 ô cát kề nước bằng cỏ (`TryDig` + `Bag.Add(Grass)` + `TryPlace`) → `Update` trả `["L2"]`.
     - `SpawnAtEdge` (điểm = tâm đồng bằng) → 2 con L2, `HomeId >= 0`.
     - Chạy tiếp tối đa 4000 nhịp → ít nhất một ô ruộng tới `Growing` hoặc `Harvest`. Chỉ L2 tưới được, nên điều này chứng tỏ L2 đã vào đội làm việc. Khoảng cách tới nhà của mỗi con L2 giảm ≥ 10 ô so với lúc xuất hiện.
4. **Client** (QĐ 7–10): SimView, HabitatPanelView, SettingsView, phím H, vi.json. Xong khi: console 0 Error; `HardcodedTextScanTests` qua.
5. **Test PlayMode** — `Tests/Client/HabitatPlayTests.cs`, SetUp như `SimViewMapsPlayTests` (`habitat` mặc định true):
   - `StartsWithL1PondAndPanel`: 1 sinh vật L1; L1 mở, L2 và L3 khoá; ô ao là nước; có `HabitatPanelView`; bảng đóng.
   - `DigUpdatesRowSameFrame`: đọc `RowText` của L1 / yêu cầu ô cỏ → `view.Editor.TryDig` một ô cỏ trong đồng bằng (xa trại, không phải ao) → gọi `RowText` lại **không yield** → số giảm 1.
   - `SpeciesReturnsInScene`: thay 10 ô cát kề ao bằng cỏ qua `view.Editor` → chờ tối đa 30 khung → `ReturnedSpecies` có "L2", `Sim.State.Creatures.Count == 3`, `BodyCount == 3`, `Notice` chứa "L2".
   - `ToggleKeyAndEscClose`: `SetOpen(true)` → `IsAnyOpen`; mô phỏng Esc như các test bảng hiện có (đọc `KeyRebindPlayTests` / `SettingsPlayTests` để lấy cách giả phím) → bảng đóng, Cài đặt **không** mở.
6. Sửa test cũ theo QĐ 9 và 11. Chạy cả bộ.

## Tiêu chí xong
- Mọi test mới qua. EditMode và PlayMode toàn bộ qua (trước brief: 596 / 267; ghi số mới).
- Console 0 Error.
- `git diff --stat` chỉ có file trong Phạm vi. `Simulation.cs` chỉ thêm `CanPlaceCreature`. `SimDemo.Build` cũ không đổi.
- `grep -rn "UnityEngine" client/Assets/Game/Core/Habitat client/Assets/Game/Core/Sim/SimDemo.cs` → rỗng.

## Test sau khi xong

### Phần cần test
- JSON + default (QĐ 3): `ConfigFileMatchesDefaults`, `ConfigTests.HabitatClientCopyMatchesDesignData`, `UnlockedAtStartRespected` (đã sửa) → qua.
- `CanPlaceCreature` + `HabitatSpawn` (QĐ 6): 4 test bước 2 → qua.
- Ao + trại (QĐ 4–5): `PondPlacedWithSandRing`, `BuildHabitatL1Only` → qua.
- "Đủ yêu cầu → loài trở lại" + vào đội làm việc: `L2ReturnsAndJoinsWork` (EditMode), `SpeciesReturnsInScene` (PlayMode) → qua.
- "Đào/đặt → tiến độ đổi ngay" trên bảng: `DigUpdatesRowSameFrame` → qua.
- Bảng + phím + Esc (QĐ 8–9): `StartsWithL1PondAndPanel`, `ToggleKeyAndEscClose`, `GameControlsTests`, `KeyRebindTests` (35), `KeyRebindPlayTests` → qua.
- Chuỗi (QĐ 10): `HardcodedTextScanTests` (`NoVietnameseLiterals`, `AllUsedKeysExist`, `BaseFileValid`), `SettingsPlayTests` (kiểm `GuideText`) → qua.
- Regression:
  - mã băm: `GoldenHashTests`, `SimDemoTests`, `SimDemoMapsTests`, toàn bộ `Sim/**` → qua (không đổi);
  - demo cũ khi `habitat = false`: các file ở QĐ 11 → qua;
  - `SimViewMapsPlayTests` (số mới), `SimViewPlayTests` (đảo v2, không đổi), `SimBenchPlayTests` (60 sinh vật, không đổi), `WorldIconsPlayTests`, `InventoryPlayTests`, `SettingsPlayTests` → qua;
  - EditMode + PlayMode toàn bộ → qua.

### Lệnh executor chạy
1. MCP `refresh_unity` → `read_console` (Error) = 0.
2. MCP `run_tests` EditMode lọc: `HabitatSpawnTests`, `HabitatDemoTests`, `HabitatTrackerTests`, `HabitatConfigTests`, `ConfigTests`, `GameControlsTests`, `KeyRebindTests`, `HardcodedTextScanTests`, `GoldenHashTests`, `SimDemoMapsTests`.
3. MCP `run_tests` PlayMode lọc: `HabitatPlayTests`, `SimViewMapsPlayTests`, `KeyRebindPlayTests`, `SettingsPlayTests`, các file ở QĐ 11.
4. MCP `run_tests` EditMode toàn bộ; PlayMode toàn bộ. Ghi dòng tổng. Nếu PlayMode chưa xong khi MCP rớt: `sleep 60` rồi `get_test_job`, lặp lại; không kết thúc lượt khi test còn chạy.
5. `grep -rn "UnityEngine" client/Assets/Game/Core/Habitat client/Assets/Game/Core/Sim/SimDemo.cs` → rỗng.
6. Dọn: `git checkout -- client/ProjectSettings/EditorSettings.asset`; xoá `client/Assets/InitTestScene*.unity(.meta)` nếu có. `git status --short` chỉ còn file của brief.
7. Play scene SimView (Mac) một lần, chụp **1** ảnh màn hình có ao và bảng H đang mở. Ghi đường dẫn ảnh (trong scratchpad, không commit) vào Kết quả.

Ghi giờ bắt đầu/kết thúc vào Kết quả.

### User tự test
Scene **SimView** trên Mac, Play:
1. Lúc đầu chỉ có 1 sinh vật cam (L1) và 3 nhà. Có ao 4 × 4 viền cát phía trước-phải trại.
2. Bấm H → bảng "Môi trường sống" bên phải dưới minimap:
   - L1 "đã ở trại"; L2 và L3 "chưa trở lại";
   - các dòng `Ô cỏ cạnh nước: 0/10`, `Ô đá lộ thiên: 0/15`, `Kho: 1/1 (đạt)`.
   - H hoặc Esc đóng bảng; Esc không mở Cài đặt.
3. Bảng đang mở, đào một ô cỏ → số `Ô cỏ` của L1 giảm ngay.
4. Ô ruộng: L1 xới, gieo, rồi đứng chờ tưới ("!" thiếu năng lực). Tự tưới (Mưa) / tự thu được như trước.
5. Đào cát quanh ao, đặt đất, dùng Mưa cho đất thành cỏ (hoặc đặt khối cỏ) tới 10 ô cạnh nước → thông báo "L2 đã trở lại!". 2 sinh vật xanh xuất hiện ở rìa đồng bằng gần bạn, tự đi về trại, rồi tưới các ô đang chờ.
6. Đào lộ đá (hoặc đặt khối đá lên mặt đất) đủ 15 ô → "L3 đã trở lại!", 2 sinh vật tím.
7. Cài đặt → Điều khiển: có dòng "Bảng môi trường sống — H", đổi được phím. Tab Hướng dẫn có dòng H.
8. Đánh giá: vị trí và cỡ bảng, vị trí ao, ngưỡng (10 cỏ cạnh nước, 15 đá lộ thiên), 4 giây thông báo. Có cần nút bấm mở bảng không.

## Dừng và chuyển lại Opus khi
- Brief mơ hồ hoặc mâu thuẫn với code thật. Ví dụ:
  - ô ao ở `PlainPondMin` đè trại / ô ruộng / chỗ người chơi xuất hiện;
  - L2 thêm vào giữa chừng nhưng ô chờ tưới vẫn đình trệ "thiếu năng lực" mãi;
  - nước ao bị lan hoặc biến mất.
- Cần một quyết định thuộc AGENTS.md §3.4 hoặc ngoài "Quyết định đã chốt".
- Test cũ hỏng mà không thuộc trường hợp ở QĐ 11.
- Cùng một lỗi sửa 2 lần vẫn chưa qua.
- Phải sửa file ngoài "Phạm vi".

---
<!-- Từ đây trở xuống: EXECUTOR ghi. -->

## Kết quả

## Kết quả test

## Vướng mắc

---
<!-- PLANNER ghi khi review. -->

## Review
