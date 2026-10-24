# Brief 0065 — 7l e2: icon + vòng tiến trình trên ô ruộng, icon trạng thái sinh vật / đình trệ (thay nhãn chữ), đo ≥ 30 FPS điện thoại với 20 ô ruộng

> Trạng thái: cần Opus
> Review: đầy đủ + user xem
> Model thực thi: Sonnet · medium
> Cập nhật: 2026-10-24

<!-- Planner (Opus) viết, Executor (Sonnet) làm theo ở PHIÊN KHÁC — executor không có lịch sử chat, không được tự quyết. -->

## Mục tiêu
Bước **7l e2** (brief 2/2, cuối của 7l e) của task [m1-world-7l-dieu-khien-pc](../tasks/m1-world-7l-dieu-khien-pc.md). Luật: [0034 mục 7](../../design/decisions/0034-dieu-khien-pc-click-di-chuyen.md#7-hud-và-menu) — "Ô ruộng: phía trên mỗi ô ruộng có icon (nông sản / giai đoạn) với vòng tiến trình bao quanh"; "Sinh vật: trạng thái hiện bằng icon nhỏ trên đầu thay cho chữ".
1. Thêm lớp icon uGUI (một canvas, không EventSystem) đặt theo vị trí world: **icon ô ruộng** + **vòng tiến trình**, **icon sinh vật**, **icon đình trệ** tại ô.
2. Bỏ nhãn chữ trên đầu sinh vật và nhãn đỏ "! lý do" ở `SimView.OnGUI`.
3. Đo điện thoại **≥ 30 FPS với 20 ô ruộng có icon** (APK WalkBench biến thể 20 ô ruộng).

Bắt đầu: kiểm brief 0064 có `> Trạng thái: đã duyệt`. Không có → dừng, `cần Opus`.

**File lạ / của phiên khác — KHÔNG commit, KHÔNG sửa:** `client/Assets/InitTestScene*.unity(.meta)`; thay đổi chưa commit trong `design/` (economy, mechanics, progression, 05-roadmap, summary, glossary, ideas, decisions/README, decisions/0038-*), `.claude/skills/local-terrain-gen/`, `tools/terrain_catalog*`, `.ai/skill-map.local.json`. Chỉ `git add` đúng file trong "Phạm vi". Không commit `client/Builds/`.

Ước lượng: 1–1,5 ngày-người (nửa sau của 2–3 ngày dòng 7l e, M1).

## Bối cảnh cần đọc
Đường dẫn tính từ `client/Assets/Game/` trừ khi ghi khác. Số dòng theo commit `6844059` (trước 0064) — sau 0064 có thể lệch, tìm theo tên.

**Dữ liệu mô phỏng (Core, không sửa luật):**
- `Core/Sim/SimData.cs`: `enum PlotPhase : byte { Idle = 0, Till = 1, Sow = 2, Water = 3, Growing = 4, Harvest = 5 }` `:62`; `enum StallReason` `:33`; `SimEvent` `:37–55` (đình trệ: `CreatureId = -1`, `Cell` = Target của công đoạn); `Creature` `:73` (`State`, `StageId`, `SizeY`, `SpeciesId`); `WorkStage` `:125` (`Ability`, `ProgressPermille`). `FarmPlot`: `Phase`, `Removed`, `Tilled`, `TillPaused`, `CurrentCrop`, `Soil`, `Id`.
- `Core/View/SiteStatus.cs`: `public static int ProgressPercent(FarmPlot p, SimState s, SimConfig cfg)` `:31` (xới/gieo/tưới/thu theo tiến độ công đoạn; lớn theo `GrowPercent`; đứng yên / huỷ = 0).
- `Core/View/CreatureStatus.cs` — cách đọc trạng thái sinh vật (`Working` + `StageId` → `s.Stages[c.StageId]`; `Moving`; còn lại rảnh). **Giữ nguyên file** (test cũ dùng).
- `Core/View/StallMarkers.cs`: `IEnumerable<SimEvent> Active`.
- Mẫu dựng trạng thái cho test EditMode: `Tests/Core/View/CreatureStatusTests.cs:9–16` (`new SimState(new SimRng(1UL, 1UL))`, `new Creature(...)`, `new WorkStage(...)`), `Tests/Core/View/SiteStatusTests.cs` (dựng `FarmPlot`).

**Client:**
- `Client/SimView.cs`:
  - `Awake()` `:73–80` (mẫu thêm component: `if (GetComponent<SettingsView>() == null) gameObject.AddComponent<SettingsView>();`);
  - `bodies` (Transform từng sinh vật), `markers` (`StallMarkers`), `CropColor(int)` `:476`, `SproutColor`, `BarColor`, `LoadColor`, `SpeciesColor`;
  - `UpdateSprouts()` (mầm 3D — giữ), `EnsurePlotVisuals()` (biển: tấm ở `Soil.Y + 1 + 0,65`);
  - `public static bool WorldLabelsVisible => !SettingsView.IsAnyOpen;` `:539`;
  - cuối `OnGUI()` `:565–575`: vòng `DrawLabel` nhãn sinh vật (`CreatureStatus.Text`) + nhãn đình trệ (`markers.Active`) — **xoá** ở brief này; `DrawLabel` `:577–584` xoá nếu không còn dùng.
- `Client/TimeDialGraphic.cs` (sau 0064 có `Star`, `Drop`): `MaskableGraphic`, `shape`, `SetShape`, `OnPopulateMesh` + `Fan` / `Quad` / `Tri`.
- `Client/TimeDial.cs` `Build()`: mẫu dựng canvas bằng code (`Canvas` ScreenSpaceOverlay, `CanvasScaler` 1280×720 match height, `Stretch`, `Place`, `AddGraphic`). `sortingOrder`: TimeDial 9, Minimap 10, Inventory 12.
- `Client/MinimapView.cs`: `IsAnyOpen`.
- `Client/WalkBench.cs` (235 dòng) — đọc hết: `Update()` khởi động `:113–150` (`sv.Plain`, `DemoPlain.CampCenter`), `Finish()` `:200–224` (dòng `WALKBENCH device=…`).
- `Client/Editor/WalkBenchBuild.cs` (95 dòng) — đọc hết: `EnsureScene(bool secondViewpoint)`, `Build(bool, string)`, menu `Game/Build APK WalkBench`, `Game/Build APK WalkBench 2 Viewpoints` (mẫu cho biến thể mới, có `finally { EnsureScene(false); }`).
- `Core/Sim/Simulation.cs`: `public bool CanMarkTill(CellPos soil)` `:838`, `public int MarkTill(CellPos soil)` `:849` (trả id ô ruộng, −1 khi không hợp lệ); `World.GetMaterial(x, y, z)`, `World.Size`.
- Số cấu hình: `design/data/sim_m1.json` khối `player` ↔ `Core/Sim/SimConfig.cs` (lớp player) ↔ `Core/PlayerMotion.cs` (`public static class`, `Configure(PlayerConfig p)`; mẫu `ManaFlashSeconds` / `HudMessageSeconds` của 0064).
- Test mẫu: `Tests/Client/SimViewPlayTests.cs` (dựng `TerrainChunkView` + `SimView`), `Tests/Client/SettingsPlayTests.cs:250–262` (`WorldLabelsVisible` khi mở Cài đặt), `Tests/Client/WalkBenchPlayTests.cs` (`SetUp` `:17–` dựng `WalkBench`).

**Số đo cũ (so sánh):** brief 0052, Xiaomi 24095PCADG, WalkBench 1 điểm nhìn: `avgFps` 42,0–49,4; 2 điểm nhìn 41,5 [repo `.ai/briefs/0052-nhieu-diem-nhin-b-do-dien-thoai.md:299`].

**API Unity (6000.4 / uGUI 2.0.0). Chỉ dùng:**
- `Camera.WorldToScreenPoint(Vector3)` [docs](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/Camera.WorldToScreenPoint.html) — trả `z` = khoảng cách theo hướng nhìn; `z <= 0` → sau camera.
- `RectTransformUtility.ScreenPointToLocalPointInRectangle(RectTransform, Vector2, Camera, out Vector2)` [docs](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/RectTransformUtility.ScreenPointToLocalPointInRectangle.html) — canvas Overlay → `cam = null`.
- `Graphic.OnPopulateMesh(VertexHelper)`, `Graphic.SetVerticesDirty()`, `Graphic.color` [docs](https://docs.unity3d.com/Packages/com.unity.ugui@2.0/api/UnityEngine.UI.Graphic.html); `VertexHelper.AddVert` / `AddTriangle` [docs](https://docs.unity3d.com/Packages/com.unity.ugui@2.0/api/UnityEngine.UI.VertexHelper.html); `CanvasScaler` [docs](https://docs.unity3d.com/Packages/com.unity.ugui@2.0/api/UnityEngine.UI.CanvasScaler.html).
- `GameObject.SetActive`, `RectTransform.anchoredPosition`.
- API khác → dừng, `cần Opus`.

## Quyết định đã chốt (không được đổi)
Từ 0034 (user, 2026-10-23) và task contract:
1. Icon (nông sản / giai đoạn) + vòng tiến trình **phía trên mỗi ô ruộng**; chi tiết vẫn xem bằng click trái (bảng ô ruộng — không đổi).
2. Trạng thái sinh vật = **icon nhỏ trên đầu**, thay nhãn chữ. Bỏ nhãn chữ sinh vật.
3. Đình trệ không còn trên HUD; hiện tại chỗ (controls-ui dòng "Đình trệ").
4. Điện thoại ≥ 30 FPS với 20 ô ruộng có icon (task contract, tiêu chí chung). Điện thoại luôn cắm USB chế độ dev — AI tự build + cài + đọc logcat, không hỏi user.
5. Icon giữ chỗ (hình vẽ bằng code) — art thật sau (task contract, Ngoài phạm vi).
6. "User xem" gộp một lần cuối task (user 2026-10-24).

**Đề xuất Planner (user xem ở lần chơi thử cuối, đổi được):**
7. Một canvas Overlay chung (`WorldIconsView`, `sortingOrder = 4` — dưới TimeDial / Minimap / Inventory), pool phần tử, chỉ gọi `SetShape` / đổi màu / đổi `fill` khi giá trị đổi (tránh dựng lại mesh mỗi khung).
8. Số [số tạm] trong `sim_m1.json` khối `player`: `"plotIconPx": 40`, `"creatureIconPx": 28`, `"iconMaxCells": 30` (xa hơn 30 ô tính từ camera → ẩn icon).
9. Ánh xạ icon (bảng ở bước 2). Sinh vật rảnh = icon "…" xám; đi lang thang không công đoạn = không icon; đi tới công đoạn = icon công đoạn mờ, không vòng. Đình trệ = "!" đỏ tại ô (event `CreatureId = -1`, nên không gắn lên sinh vật).
10. Icon ẩn khi Cài đặt mở (`SimView.WorldLabelsVisible`, giữ hành vi brief 0050) và khi bản đồ mở.
11. Mầm 3D (`UpdateSprouts`) và biển canh tác giữ nguyên.
12. Biến thể đo: `WalkBench.targetPlots` (0 = như cũ); APK `walkbench-plots.apk` đánh dấu xới tới khi có 20 ô ruộng quanh trại.

## Phạm vi
- Được sửa/tạo:
  - `client/Assets/Game/Core/View/WorldIcons.cs` (mới), `Core/Sim/SimConfig.cs`, `Core/PlayerMotion.cs`;
  - `client/Assets/Game/Client/WorldIconsView.cs` (mới), `Client/TimeDialGraphic.cs`, `Client/SimView.cs`, `Client/WalkBench.cs`, `Client/Editor/WalkBenchBuild.cs`, `client/Assets/Game/Client/Scenes/WalkBench.unity` (do menu tạo lại);
  - `design/data/sim_m1.json` (chỉ thêm 3 số ở bước 1);
  - `client/Assets/Game/Client/Resources/Config/sim_m1.json` — bản sao client, phải giống hệt `design/data/sim_m1.json` (`ConfigTests.SimClientCopyMatchesDesignData`; phát hiện ở review 0064);
  - test: `Tests/Core/View/WorldIconsTests.cs` (mới), `Tests/Client/WorldIconsPlayTests.cs` (mới), `Tests/Client/WalkBenchPlayTests.cs`, `Tests/Client/SettingsPlayTests.cs` (chỉ nếu test `WorldLabelsVisible` cần thêm assert icon);
  - các `.meta` Unity tự sinh cho file mới; mục Kết quả của brief này.
- Không được sửa: luật mô phỏng (`Simulation.cs` trừ đọc), `CreatureStatus.cs`, `SiteStatus.cs`, HUD / menu phép của 0064, `InventoryView.cs`, `SimView.unity`, file của phiên khác.

## Các bước
1. **Số tạm.** `sim_m1.json` khối `player`: thêm `"plotIconPx": 40, "creatureIconPx": 28, "iconMaxCells": 30` sau `"hudMessageMs"`. `SimConfig` lớp player: `public int plotIconPx = 40; public int creatureIconPx = 28; public float iconMaxCells = 30f;`. `PlayerMotion`: `public static float PlotIconPx { get; private set; } = 40f;`, `CreatureIconPx` = 28f, `IconMaxCells` = 30f; gán trong `Configure`. Xong khi: compile, `ConfigTests` qua.

2. **`Core/View/WorldIcons.cs`** (C# thuần, namespace `Game.Core.View`):
   ```csharp
   public enum WorldIconKind : byte { None, Till, Sow, Water, Grow, Ripe, Empty, Harvest, Haul, Idle, Stall }
   /// <summary>Icon trên world (0034 mục 7, brief 0065). Percent: 0..100 → vòng tiến trình; -1 → không vòng. Crop: loại cây (-1 không có). Dim: vẽ mờ.</summary>
   public readonly struct WorldIcon : System.IEquatable<WorldIcon>
   {
       public readonly WorldIconKind Kind; public readonly int Percent, Crop; public readonly bool Dim;
       public WorldIcon(WorldIconKind kind, int percent = -1, int crop = -1, bool dim = false);
       // Equals / GetHashCode / ==, != theo 4 trường
   }
   public static class WorldIcons
   {
       public static WorldIcon ForPlot(FarmPlot p, SimState s, SimConfig cfg);
       public static WorldIcon ForCreature(SimState s, Creature c);   // không đặt tên Creature — trùng tên kiểu
       public static WorldIcon ForStall() => new WorldIcon(WorldIconKind.Stall);
   }
   ```
   `ForPlot` (pct = `SiteStatus.ProgressPercent(p, s, cfg)`):
   | Điều kiện (theo thứ tự) | Kết quả |
   |---|---|
   | `p.Removed` | `None` |
   | `Phase == Till` | `Till`, pct, `dim = p.TillPaused` |
   | `Phase == Sow` | `Sow`, pct, crop = `p.CurrentCrop` |
   | `Phase == Water` | `Water`, pct, crop |
   | `Phase == Growing` | `Grow`, pct, crop |
   | `Phase == Harvest` | `Ripe`, pct, crop |
   | `Phase == Idle && p.Tilled` | `Empty`, `-1` |
   | còn lại (Idle chưa xới) | `None` |

   `ForCreature`:
   | Điều kiện | Kết quả |
   |---|---|
   | `State == Working && StageId >= 0` | theo `s.Stages[StageId].Ability`: Till→`Till`, Sow→`Sow`, Water→`Water`, Harvest→`Harvest`, Haul→`Haul`; percent = `ProgressPermille / 10` |
   | `State == Moving && StageId >= 0` | cùng kind theo Ability, `-1`, `dim = true` |
   | `State == Moving` (không công đoạn) | `None` |
   | còn lại | `Idle`, `-1` |
   (Ability khác 5 loại trên → `Idle`.) Xong khi: test bước 7 qua.

3. **`TimeDialGraphic` — thêm hình** (thêm vào **cuối** enum, sau `Star`, `Drop` của 0064): `Ring, Hoe, Seed, Sprout, Crate, Bang, Dots`. Thêm `public float fill = 1f;` + `public void SetFill(float f)` (chặn 0..1; đổi mới `SetVerticesDirty`). Vẽ (r = nửa cạnh ngắn, o = tâm; hình giữ chỗ):
   - `Ring`: cung dày `0,16r` từ đỉnh (90°) **theo chiều kim đồng hồ** tới `90° − 360°·fill`; 48 đoạn × fill (tối thiểu 1); mỗi đoạn một `Quad` giữa bán kính `0,84r` và `r`. `fill <= 0` → không vẽ.
   - `Hoe` (cuốc): cán `Quad` chéo từ `(-0,6r, -0,7r)` tới `(0,4r, 0,6r)` rộng `0,14r`; lưỡi `Quad` ngang `(0,1r..0,8r, 0,45r..0,7r)`.
   - `Seed`: `Fan` tròn bán kính `0,45r` tại `(0, -0,1r)` + `Tri` đỉnh `(0, 0,7r)`, chân `(±0,35r, 0,05r)`.
   - `Sprout`: thân `Quad` `(-0,06r..0,06r, -0,8r..0,3r)`; hai lá `Fan` bán kính `0,32r` tại `(±0,3r, 0,35r)`.
   - `Crate` (thùng hàng): `Quad` vuông `±0,6r` (màu theo `color`); không cần nét.
   - `Bang` ("!"): `Quad` `(-0,12r..0,12r, -0,15r..0,85r)` + `Fan` bán kính `0,13r` tại `(0, -0,6r)`.
   - `Dots` ("…"): 3 `Fan` bán kính `0,14r` tại `(-0,5r, 0)`, `(0, 0)`, `(0,5r, 0)`.
   - Xong khi: compile; hình cũ (`Disk`…`Drop`) không đổi số thứ tự.

4. **`Client/WorldIconsView.cs`** (mới, `[RequireComponent(typeof(SimView))]`):
   - Summary: `/// <summary>Icon trên world (0034 mục 7, brief 0065): icon + vòng tiến trình trên ô ruộng, icon trạng thái sinh vật, "!" đình trệ tại ô. Một canvas Overlay dựng bằng code, pool phần tử, chỉ dựng lại hình khi icon đổi.</summary>`
   - `SimView.Awake()` thêm `if (GetComponent<WorldIconsView>() == null) gameObject.AddComponent<WorldIconsView>();` (cạnh dòng SettingsView).
   - Dựng canvas khi `sim.Sim != null` lần đầu (trong `LateUpdate`, sau khi SimView cập nhật thân sinh vật): như `TimeDial.Build` (Overlay, `CanvasScaler` 1280×720 match height = 1), `sortingOrder = 4`, tên `"World Icons"`. `OnDestroy` → `Destroy(canvasGo)`.
   - Phần tử (lớp lồng `Item`): `RectTransform root` (cỡ `PlayerMotion.PlotIconPx` hoặc `CreatureIconPx`), con: nền `Disk` màu `(0, 0, 0, 0.55)`, vòng nền `Ring` fill 1 màu `(1, 1, 1, 0.2)`, vòng tiến trình `Ring` màu `(0.35, 0.8, 0.35, 1)`, icon `68%` cỡ. Lưu `WorldIcon last` (giá trị gán gần nhất) — chỉ khi icon mới `!= last` mới đổi hình / màu / fill / alpha.
   - Hình + màu theo kind: Till→`Hoe` `(0.80,0.65,0.45)`; Sow→`Seed` `(0.55,0.40,0.20)`; Water→`Drop` `(0.35,0.65,1)`; Grow→`Sprout` `SproutColor`; Ripe→`Disk` màu `SimView.CropColor(crop)`; Empty→`Dots` `(0.7,0.7,0.7)`; Harvest→`Disk` `(0.95,0.85,0.25)`; Haul→`Crate` `LoadColor`; Idle→`Dots` `(0.8,0.8,0.8)`; Stall→`Bang` đỏ `(1,0.25,0.2)` (nền đỏ đậm `(0.35,0,0,0.75)`). `CropColor` / `SproutColor` / `LoadColor` trong `SimView` là `static` riêng → đổi thành `internal static` (không đổi giá trị). Percent `-1` → ẩn 2 vòng. `Dim` → `CanvasGroup` hoặc alpha icon `0.5`.
   - Ba pool: `plots` (theo chỉ số `Sim.State.Plots`), `creatures` (theo chỉ số `Sim.State.Creatures`), `stalls` (theo thứ tự duyệt `markers.Active` mỗi khung; dư → `SetActive(false)`). `SimView` thêm `internal StallMarkers Markers => markers;` và `internal Transform Body(int i) => bodies[i];` (hoặc tương đương) để đọc.
   - Điểm world:
     - ô ruộng: `sim.transform.TransformPoint(new Vector3(Soil.X + 0.5f, Soil.Y + 2.3f, Soil.Z + 0.5f))` (trên biển 1,8 và mầm cao nhất ~1,7);
     - sinh vật: `Body(i).position + Vector3.up * (c.SizeY * 0.4f + 0.6f)`;
     - đình trệ: `TransformPoint(Cell + (0.5, 1.6, 0.5))`.
   - Mỗi khung (`LateUpdate`): `bool show = SimView.WorldLabelsVisible && !MinimapView.IsAnyOpen && Camera.main != null`. Không show → tắt cả canvas (`canvasGo.SetActive(false)`), return. Với mỗi phần tử có icon ≠ `None`: `sp = cam.WorldToScreenPoint(p)`; ẩn nếu `sp.z <= 0`, hoặc `Vector3.Distance(cam.transform.position, p) > PlayerMotion.IconMaxCells`, hoặc `sp.x/sp.y` ngoài màn hình quá 64 px; còn lại `ScreenPointToLocalPointInRectangle(canvasRect, sp, null, out local)` → `anchoredPosition = local` (neo/pivot giữa). Chỉ gọi `SetActive` khi trạng thái hiện đổi.
   - Thuộc tính cho test: `public bool IsReady`, `public int PlotItemCount`, `public int CreatureItemCount`, `public int VisiblePlotIcons` (đếm ở khung gần nhất), `public int VisibleStallIcons`, `public WorldIcon PlotIcon(int plotId)` (giá trị `last`), `public float PlotRingFill(int plotId)`, `public TimeDialShape PlotShape(int plotId)`, `public WorldIcon CreatureIcon(int i)`, `public bool CanvasActive`.
   - Xong khi: compile; test bước 8 qua.

5. **`SimView.OnGUI` — bỏ nhãn chữ.** Xoá vòng `DrawLabel` cho sinh vật và cho `markers.Active` (cùng `return` / biến `cam` chỉ dùng cho chúng), xoá `DrawLabel` nếu không còn dùng. Giữ `WorldLabelsVisible` (dùng ở bước 4 + test cũ). Cập nhật summary lớp `SimView` (dòng "Nhãn trên đầu = trạng thái; "!" đỏ = đình trệ tại ô") → "Icon trạng thái trên đầu + icon ô ruộng + '!' đình trệ: WorldIconsView (brief 0065)". Xong khi: `grep -n "CreatureStatus.Text\|DrawLabel" Client/SimView.cs` rỗng.

6. **WalkBench 20 ô ruộng.**
   - `WalkBench.cs`: `public int targetPlots;` (0 = không thêm). Trong khối khởi động (khi `started` chuyển true, có `sv.Sim` và `sv.Plain`): nếu `targetPlots > 0` gọi `AddPlots(targetPlots)`:
     - `c = DemoPlain.CampCenter(sv.Plain.Value)`; duyệt ô theo vòng vuông bán kính `k = 3, 4, …, 16`, bước 2 ô (`x = cx + dx`, `z = cz + dz` với `dx, dz` chẵn trong `[-k, k]`, chỉ ô trên viền vòng `k`); với mỗi cột tìm `y` = ô đặc cao nhất (duyệt từ `World.Size.Y - 2` xuống, `GetMaterial != Empty`); `sv.Sim.MarkTill(new CellPos(x, y, z))`; dừng khi `sv.Sim.State.Plots.Count >= targetPlots`.
     - `Debug.Log($"WALKBENCH plots: {sv.Sim.State.Plots.Count} (target {targetPlots})");`.
     - (Không dùng `TerrainEditInput` — scene WalkBench tắt nó.)
   - `Finish()`: thêm vào dòng `WALKBENCH device=…`, ngay trước `pass30fps=`: `plots={…Plots.Count} plotIconsVisibleAvg={trung bình VisiblePlotIcons trên các khung đo:F1} | `. Lấy `WorldIconsView` bằng `GetComponent` (cùng GameObject với SimView); null → ghi `-1`. Cộng dồn `VisiblePlotIcons` mỗi khung đo (cùng chỗ thêm `frameMs`).
   - `WalkBenchBuild.cs`: `EnsureScene(bool secondViewpoint, int targetPlots = 0)` gán `wb.targetPlots = targetPlots`; `Build(bool secondViewpoint, string outputPath, int targetPlots = 0)` truyền xuống; thêm `const string OutputPath3 = "Builds/walkbench-plots.apk";` và
     ```csharp
     /// <summary>Same APK with 20 farm plots around the camp (world 7l e2, brief 0065) to measure plot icons; the saved scene goes back to defaults afterwards.</summary>
     [MenuItem("Game/Build APK WalkBench 20 Plots")]
     public static void BuildAndroidPlots()
     {
         try { Build(false, OutputPath3, 20); }
         finally { EnsureScene(false); }
     }
     ```
   - Xong khi: test bước 8 (WalkBench) qua.

7. **Test EditMode `Tests/Core/View/WorldIconsTests.cs`** (namespace / asmdef như `CreatureStatusTests`; dựng state theo mẫu `CreatureStatusTests.cs:9–16`, ô ruộng theo `SiteStatusTests`):
   - `PlotRemovedNone`, `PlotIdleUntilledNone`, `PlotIdleTilledEmpty` (Percent −1).
   - `PlotTillPercentAndPausedDim`: Phase Till, công đoạn 450‰ → `Till`, 45; `TillPaused = true` → `Dim`.
   - `PlotGrowingPercent`: Growing, `RipeTick` sao cho `GrowPercent` = 40 → `Grow`, 40, crop đúng.
   - `PlotHarvestRipe`: Harvest → `Ripe`, crop đúng.
   - `PlotSowWaterKinds`: Sow → `Sow`; Water → `Water`.
   - `CreatureWorkingAbilityPercent`: Working + stage Till 450‰ → `Till`, 45; Haul → `Haul`.
   - `CreatureMovingToStageDim`: Moving + StageId ≥ 0 → kind theo ability, −1, `Dim`.
   - `CreatureWanderNone`, `CreatureIdle`.
   - `IconEquality`: hai `WorldIcon` cùng 4 trường `==`; khác Percent `!=`.

8. **Test PlayMode.**
   - `Tests/Client/WorldIconsPlayTests.cs` (fixture như `SimViewPlayTests` — `TerrainChunkView` + `SimView` + camera `MainCamera` nhìn vào trại; đọc `SimViewPlayTests` để dựng đúng, gồm cách chờ `sv.Sim != null`):
     - `BuildsOneItemPerPlotAndCreature`: chờ `icons.IsReady` → `PlotItemCount == Sim.State.Plots.Count`, `CreatureItemCount == Sim.State.Creatures.Count`.
     - `PlotIconMatchesStageAndProgress`: mọi ô ruộng: `icons.PlotIcon(i) == WorldIcons.ForPlot(p, State, Config)` (so sau 1 khung); với icon có Percent ≥ 0: `Mathf.Abs(PlotRingFill(i) - Percent / 100f) < 0.011f`; `PlotShape(i)` khớp bảng bước 4 cho kind đó. Chạy sim thêm ~2 giây (theo cách test SimView cũ cho sim chạy) rồi kiểm lại → vẫn khớp, và có ít nhất một ô Percent đổi hoặc Kind đổi (ô trại demo có vụ Cây A ban đầu — nếu không đổi trong 2 giây, dùng thời gian test cũ dùng để thấy % lớn tăng; ghi vào Kết quả).
     - `CreatureIconMatchesState`: mọi sinh vật `icons.CreatureIcon(i) == WorldIcons.ForCreature(State, c)`.
     - `HiddenWhileSettingsOpen`: mở Cài đặt (như `SettingsPlayTests:250–262`) → `CanvasActive == false`; đóng → true.
     - `FarIconsHidden`: đưa camera ra xa trại > 30 ô (vd `+60` theo y) → `VisiblePlotIcons == 0` sau 1 khung.
   - `WalkBenchPlayTests`: thêm `TargetPlotsMarksTwenty` (fixture sẵn có): `wb.targetPlots = 20` trước khi bật → sau khi `started` (chờ dòng log `WALKBENCH plots:` hoặc `Plots.Count >= 20`, tối đa vài giây như test cũ chờ) → `sv.Sim.State.Plots.Count >= 20`; chờ `Done` → `wb.Summary` chứa `"plots="` và `"plotIconsVisibleAvg="`. Test cũ vẫn qua (targetPlots = 0 → Summary có `plots=4` hoặc số ô demo).
   - Regression: `SettingsPlayTests` (`WorldLabelsVisible`), `SimViewPlayTests`, `SimFarmUiPlayTests`, `TimeDialPlayTests`, `InventoryPlayTests` — qua không sửa (InventoryView `sortingOrder` 12 > 4 nên icon không che túi).

9. **Build + đo điện thoại** (sau khi mọi test qua):
   1. MCP `execute_menu_item` `Game/Build APK WalkBench` → console `WalkBench build: Succeeded … → Builds/walkbench.apk`.
   2. MCP `execute_menu_item` `Game/Build APK WalkBench 20 Plots` → `… → Builds/walkbench-plots.apk`.
   3. Dọn: `git checkout -- client/ProjectSettings/UnityConnectSettings.asset`; `grep -c "targetPlots: 20" client/Assets/Game/Client/Scenes/WalkBench.unity` → 0; `grep -c "secondViewpoint: 1" …` → 0. Ghi `git diff --stat -- client/Assets/Game/Client/Scenes/WalkBench.unity`.
   4. Điện thoại (AI tự làm; `ADB` = `/Applications/Unity/Hub/Editor/6000.4.8f1/PlaybackEngines/AndroidPlayer/SDK/platform-tools/adb`):
      - Chọn máy: `ADB devices` → serial USB `PFQCRWVWR46HU4GQ`; không có → `ADB mdns services` / `ADB connect 192.168.1.6:5555`. **Không dùng `192.168.1.7:6060`** (Android TV). `ADB -s <máy> shell getprop ro.product.model` phải là `24095PCADG`. Không thấy máy → Vướng mắc "chờ user cắm lại điện thoại", `cần Opus`, dừng.
      - **Lần A** (`walkbench.apk`) rồi nghỉ 60 giây, **lần B** (`walkbench-plots.apk`), mỗi lần: Thermal (`ADB -s <máy> shell dumpsys thermalservice | grep -m1 "Thermal Status"`) → `ADB -s <máy> install -r <apk>` (`Success`) → `ADB -s <máy> logcat -c` → `ADB -s <máy> shell monkey -p com.viventa.walkbench -c android.intent.category.LAUNCHER 1` → ~25 giây sau `ADB -s <máy> shell dumpsys window | grep -E "mDreamingLockscreen|KeyguardShowing"` (khoá → Vướng mắc "chờ user mở khoá", `cần Opus`, dừng) → chờ tổng 90 giây → `ADB -s <máy> logcat -d -s Unity | grep -E "WALKBENCH|Exception"` (chưa có `device=` → chờ thêm 60 giây) → `ADB -s <máy> shell am force-stop com.viventa.walkbench`.
      - Chép nguyên văn vào Kết quả: Thermal, `WALKBENCH started`, `WALKBENCH plots:` (lần B), `WALKBENCH device=…` của cả hai lần. Kẻ bảng: cột 0052 (1 điểm nhìn, 42,0–49,4) · A · B; dòng avgFps, p50Ms, p99Ms, maxMs, plots, plotIconsVisibleAvg, sysMemMaxMB, gcReservedMaxMB, Thermal.
      - Lần B `avgFps < 30` hoặc `plotIconsVisibleAvg < 10` → ghi số liệu, ghi "dưới 30 FPS" / "ít icon trong khung" ở đầu Kết quả, đặt `chờ review`. **Không** tối ưu.
   - Không commit `client/Builds/`.

10. **Kết quả:** giờ bắt đầu / kết thúc, file đổi, test thêm / sửa, bảng FPS, lệch so với brief. Đây là brief cuối của 7l e → thêm mục **"User tự test gộp 7l e"** = mục "User tự test" của 0064 + của brief này (chép nguyên văn hai danh sách).

## Tiêu chí xong
- Icon + vòng trên mọi ô ruộng (trừ huỷ / chưa đánh dấu), icon trên sinh vật, "!" tại ô đình trệ; không còn nhãn chữ sinh vật / nhãn đình trệ.
- `WorldIcons` (Core) không tham chiếu UnityEngine; test EditMode + PlayMode qua hết qua MCP, 0 lỗi console.
- Hai APK build thành công; lần B có `plots=20`(hoặc hơn) và `avgFps ≥ 30` (hoặc ghi rõ dưới 30 như bước 9).
- `UnityConnectSettings.asset`, `EditorSettings.asset` đã dọn; scene WalkBench lưu với `targetPlots: 0`.

## Test sau khi xong

### Phần cần test
- Số tạm (bước 1) · `ConfigTests` · qua; `PlayerMotion.IconMaxCells` = 30 sau `Configure`.
- Ánh xạ icon (bước 2) · `WorldIconsTests` (13 test) · đúng bảng.
- Hình mới (bước 3) · gián tiếp `PlotShape` trong `PlotIconMatchesStageAndProgress`; hình cũ không đổi · `TimeDialPlayTests` qua (dùng `Sun`/`Moon`/`Star`).
- Lớp icon (bước 4) · `WorldIconsPlayTests` 5 test · 1 phần tử / ô ruộng + sinh vật; icon + vòng khớp giai đoạn / tiến trình, cập nhật khi sim chạy; ẩn khi Cài đặt mở; ẩn khi xa > 30 ô.
- Bỏ nhãn chữ (bước 5) · `grep` ở bước 5 rỗng; `SettingsPlayTests` (`WorldLabelsVisible`) qua.
- WalkBench (bước 6) · `TargetPlotsMarksTwenty` + test WalkBench cũ · ≥ 20 ô; Summary có `plots=` / `plotIconsVisibleAvg=`; cũ vẫn qua.
- Regression client · `SimViewPlayTests`, `SimFarmUiPlayTests`, `InventoryPlayTests`, `TimeDialPlayTests`, `ControlModePlayTests` · qua.
- Hiệu năng (bước 9) · 2 APK trên 24095PCADG · lần B avgFps ≥ 30.
- Toàn bộ · EditMode + PlayMode · qua hết.

### Lệnh executor chạy
1. MCP `refresh_unity` → `read_console` (Error) = 0.
2. MCP `run_tests` EditMode (toàn bộ) → dòng tổng.
3. MCP `run_tests` PlayMode (toàn bộ) → dòng tổng.
4. Dọn: `git checkout -- client/ProjectSettings/EditorSettings.asset`.
5. Bước 9 (2 APK + điện thoại) → dọn `git checkout -- client/ProjectSettings/UnityConnectSettings.asset`.
6. `python3 tools/check_design.py` → exit 0 (vì sửa `design/data/sim_m1.json`).
7. `git status --short` — chỉ file trong Phạm vi; commit code + test + scene WalkBench + brief (vd `brief 0065: 7l e2 plot/creature icons, 20-plot walkbench`).

### User tự test
(Gộp vào danh sách chơi thử cuối task — Planner chép; không chờ user.)
- Scene SimView, Play: trên mỗi ô ruộng có một icon tròn nền tối + vòng xanh: ô đang xới = cuốc, gieo = hạt, tưới = giọt nước, đang lớn = mầm (vòng đầy dần), chín = đĩa màu cây; ô ruộng trống = "…". Vòng chạy mượt, không nhấp nháy.
- Sinh vật: không còn chữ "L1 · …" trên đầu; có icon nhỏ: rảnh = "…", đang làm = icon công đoạn + vòng, đang đi tới việc = icon mờ; mang hàng = thùng vàng.
- Đình trệ (vd kho đầy / không có đường): "!" đỏ tại chỗ, không còn chữ đỏ.
- Icon nằm đúng trên ô / sinh vật khi xoay + thu phóng camera; không đè lên túi / menu phép / bản đồ; mở Cài đặt thì ẩn; đi xa trại thì icon ẩn.
- Đánh giá: cỡ icon (40 / 28 px), khoảng cách ẩn 30 ô, hình giữ chỗ có đọc được giai đoạn không.

## Dừng và chuyển lại Opus khi
- Brief mơ hồ hoặc mâu thuẫn với code thật (vd fixture `SimViewPlayTests` không có camera nhìn vào trại; `MarkTill` quanh trại không đủ 20 ô trong bán kính 16).
- Cần một quyết định thuộc AGENTS.md §3.4 hoặc ngoài "Quyết định đã chốt".
- Cùng một lỗi sửa 2 lần vẫn chưa qua.
- Phải sửa file ngoài "Phạm vi".
- Cần API Unity không có trong "Bối cảnh cần đọc".

---
<!-- Từ đây trở xuống: EXECUTOR ghi. -->

## Kết quả
- Base commit: b85ffc8
- Chạy bằng: Sonnet 5.5 (claude-sonnet-5-5) · medium
- Bắt đầu: 2026-10-24 07:25
- **Chưa đo điện thoại (bước 9.4): không thấy máy qua adb → mục `cần Opus` / chờ user cắm lại.** Mọi bước còn lại xong.
- Commit code: 74ec39c
- File đổi: Core/View/WorldIcons.cs (mới), Core/{PlayerMotion,Sim/SimConfig}.cs, Client/{WorldIconsView (mới),TimeDialGraphic,SimView,WalkBench,Editor/WalkBenchBuild}.cs, Scenes/WalkBench.unity(+.meta; diff 7 dòng: thêm trường targetPlots: 0 + component WorldIconsView), Resources/Config/sim_m1.json, design/data/sim_m1.json; test: Tests/Core/View/WorldIconsTests (12 test), Tests/Client/WorldIconsPlayTests (5), WalkBenchPlayTests (+TargetPlotsMarksTwenty; SetUp gán targetPlots theo tên test).
- Lệch brief: `WorldIconsTests` có 12 test (brief ghi "13"); SettingsPlayTests không sửa (HiddenWhileSettingsOpen nằm trong WorldIconsPlayTests). `PlotIconMatchesStageAndProgress` chạy sim bằng `sv.Sim.Tick()` ×3000 (như SimFarmUiPlayTests) — ít nhất một icon ô ruộng đổi.
- APK: `Builds/walkbench.apk` (07:41) và `Builds/walkbench-plots.apk` (07:42) đều `Succeeded` (860648547 / 860648555 byte). Không commit client/Builds/.

## Kết quả test
- Số tạm: đạt — ConfigTests (kể cả SimClientCopyMatchesDesignData) trong EditMode toàn bộ.
- Ánh xạ icon: đạt — `WorldIconsTests` 12/12.
- Hình mới + lớp icon: đạt — `WorldIconsPlayTests` 5/5 (BuildsOneItemPerPlotAndCreature, PlotIconMatchesStageAndProgress, CreatureIconMatchesState, HiddenWhileSettingsOpen, FarIconsHidden).
- Bỏ nhãn chữ: đạt — `grep -n "CreatureStatus.Text\|DrawLabel" Client/SimView.cs` rỗng.
- WalkBench: đạt — `TargetPlotsMarksTwenty` + `WalksRouteAndLogsSummary`.
- Hiệu năng điện thoại: **chưa đo** — `adb devices` rỗng; `adb connect 192.168.1.6:5555` bị từ chối; mdns rỗng; serial `PFQCRWVWR46HU4GQ` not found.
- Lệnh: EditMode toàn bộ 560/560 qua. PlayMode toàn bộ 254 test, 253 qua; `SeaSkyTintPlayTests.DayCycleOffNearSeaUnchanged` lỗi (0,071 > 0,002) khi chạy cả bộ — cùng lỗi như lúc 0064 (chạy riêng thì qua) → có vẻ phụ thuộc thứ tự, không liên quan icon. Console Error = 0. Đã `git checkout` EditorSettings.asset + UnityConnectSettings.asset; `targetPlots: 20` = 0, `secondViewpoint: 1` = 0 trong WalkBench.unity. `check_design.py` exit 0.
- User xem: chờ user — gộp cuối.

## User tự test gộp 7l e
Từ 0064:
- Mở scene SimView, Play: góc trên-trái chỉ còn avatar ô vuông chữ "B" + thanh máu + thanh mana + số; không còn đoạn chữ hướng dẫn dài, không còn dòng kho / ô ruộng.
- Bấm Q: hiện "Chế độ: Đào"; bấm D: dòng chế độ biến mất. Đào một khối: thông báo hiện rồi tự ẩn sau ~4 giây.
- Trên cùng giữa: "xx FPS · mục tiêu 60 · nhịp N" (đọc được, không che minimap).
- O → Cài đặt → tab Hướng dẫn: đọc chữ phím, đúng với phím đang dùng (B, P, 1–0, kéo thả, Tab, 1/2/3, G…); cuộn được nếu dài.
- Không còn dòng "Đang ở buổi"; nút "Phép (G)" dưới minimap; G mở menu 2 mục; Đổi buổi → vòng 4 mốc như cũ; Mưa → vào chế độ Tưới, chuột phải ô ruộng tưới được; sau khi tưới, mở lại menu thấy "10 giây" đếm lùi. T không còn làm gì.
- Đánh giá: vị trí / cỡ avatar, thanh, menu phép có dễ dùng không; 4 giây ẩn thông báo có hợp không.
Từ 0065:
- Scene SimView, Play: trên mỗi ô ruộng có một icon tròn nền tối + vòng xanh: ô đang xới = cuốc, gieo = hạt, tưới = giọt nước, đang lớn = mầm (vòng đầy dần), chín = đĩa màu cây; ô ruộng trống = "…". Vòng chạy mượt, không nhấp nháy.
- Sinh vật: không còn chữ "L1 · …" trên đầu; có icon nhỏ: rảnh = "…", đang làm = icon công đoạn + vòng, đang đi tới việc = icon mờ; mang hàng = thùng vàng.
- Đình trệ (vd kho đầy / không có đường): "!" đỏ tại chỗ, không còn chữ đỏ.
- Icon nằm đúng trên ô / sinh vật khi xoay + thu phóng camera; không đè lên túi / menu phép / bản đồ; mở Cài đặt thì ẩn; đi xa trại thì icon ẩn.
- Đánh giá: cỡ icon (40 / 28 px), khoảng cách ẩn 30 ô, hình giữ chỗ có đọc được giai đoạn không.

## Vướng mắc
- Bước 9.4 (đo FPS điện thoại 24095PCADG) chưa làm được: adb không thấy máy nào (USB lẫn Wi‑Fi). Cần user cắm lại điện thoại (USB, bật debug, mở khoá màn hình). Khi có máy: chạy lại bước 9.4 với `Builds/walkbench.apk` rồi `Builds/walkbench-plots.apk` (hai APK đã build xong từ commit 74ec39c — không cần build lại).

---
<!-- PLANNER ghi khi review. -->

## Review
