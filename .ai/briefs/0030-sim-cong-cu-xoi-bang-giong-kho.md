# Brief 0030 — Simulation bước 7c: công cụ Xới + biển canh tác, bảng chọn giống/hàng đợi, nút Kho (client)

> Trạng thái: đã duyệt
> Review: đầy đủ + user xem
> Model thực thi: Sonnet · medium
> Cập nhật: 2026-10-22

<!-- Planner (Opus) viết, Executor (Sonnet) làm theo ở PHIÊN KHÁC — executor không có lịch sử chat, không được tự quyết.
Trạng thái: chờ làm → đang làm → chờ review → đã duyệt; hoặc → cần Opus (executor dừng, hỏi ở mục Vướng mắc).
Review: nhẹ (đọc mục Kết quả) | đầy đủ (đọc cả diff) | user xem (có phần user tự kiểm bằng mắt/chơi thử) — có thể ghép "đầy đủ + user xem".
Model thực thi: BẮT BUỘC, đúng dạng `<Model> · <effort>` — Model: Opus | Sonnet | Haiku | Fable; effort: low | medium | high | xhigh | max (chọn theo AGENTS.md §10.2).
Executor kiểm tra model + effort của phiên TRƯỚC khi làm (scripts/brief_check.py); lệch hoặc không xác định được → cảnh báo user và DỪNG.
File được commit. Tự kiểm trước khi giao (AGENTS.md §10.3): executor có phải hỏi gì không? còn chỗ nào bỏ ngỏ 2 phương án? -->

## Mục tiêu
Làm bước 7c theo [task contract](../tasks/m1-sim-buoc-7c-cong-cu-xoi-bang-giong-kho.md) và [0025](../../design/decisions/0025-nguoi-choi-xoi-dat-chon-giong-hang-doi.md) (Chốt). Phần client cho lõi đã làm ở [brief 0029](0029-sim-danh-dau-xoi-giong-kho.md):
- công cụ **Xới** (Tab xoay Đào → Đặt → Xới) và **biển canh tác**;
- **bảng ô ruộng**: chọn giống, xem/xoá hàng đợi;
- **nút Kho** + bảng kho;
- SimView thôi tự thêm vụ (`KeepFarming`).

Ước lượng 3–5 ngày-người. Ghi giờ bắt đầu/kết thúc vào mục Kết quả.

## Bối cảnh cần đọc
Đường dẫn tính từ `client/Assets/Game/`. Số dòng theo commit `1e161f0`.
- `Client/TerrainEditInput.cs`:
  - `enum EditTool { Dig, Place }` (`:9`); `Update` (`:53-103`): phím (`:55-63`), chặn con trỏ bằng `MinimapView.BlocksPointer`/`TimeDial.BlocksPointer` (`:67-71`), `Pick` (`:85`), `ShowAim` (`:91`), chuột trái → `UseTool` (`:102`).
  - `ToggleTool` (`:106`), `ShowAim(bool picked, Vector3Int digCell, Vector3Int placeCell)` (`:119-136`), `public EditResult UseTool(Vector3Int digCell, Vector3Int placeCell)` (`:139-152`), `OnGUI` (`:185-205`).
- `Client/SimView.cs`:
  - `Update` (`:96-130`): `SimDemo.KeepFarming(Sim)` ở `:117`.
  - `Setup` (`:132-195`): vòng ô ruộng tạo tấm "Ruộng" + mầm (`:185-191`) — chỉ cho ô có lúc dựng.
  - `UpdatePlotSlabs` (`:245-250`): tấm hiện khi `!Tilled && !Removed`. `UpdateSprouts` (`:252-267`).
  - `Box(...)` (`:284-298`): `solid: false` → bỏ collider (không chặn raycast).
  - `OnGUI` (`:300-330`): `scale = Mathf.Max(1f, Screen.height / 720f)`, `GUI.matrix` phóng theo `scale`.
- Lõi (brief 0029), `Core/Sim/Simulation.cs`: `public int PlotAt(CellPos soil)`, `public bool CanMarkTill(CellPos soil)`, `public int MarkTill(CellPos soil)` (−1 khi sai), `public bool Unmark(int plotId)`, `public bool RemoveCrop(int plotId, int index)`, `public bool EnqueueCrop(int plotId, int cropId)`. `State.Stock` (`Stockpile`: `Capacity`, `Seeds[]`, `Produce[]`, `SeedsTotal`, `ProduceTotal`). `Config.crops[i].id` ("A", "B"), `Config.farm.plotQueueCapacity` (2).
- `Core/Sim/SimDemo.cs:196-201` (`KeepFarming`). `Core/View/SiteStatus.cs` (`StorageText`, `PlotText`).
- Màn hình: minimap góc trên-phải (`MinimapView.cs:13,29`), nút + vòng thời điểm bên phải (`TimeDial.cs:258-272`), HUD chữ góc trên-trái (`TerrainEditInput.OnGUI`, `SimView.OnGUI`). Góc **dưới-trái** và **dưới-giữa** đang trống.
- Phím đang dùng: 1–4, Tab, WASD/Q/E, Shift, F1/F2, M, T, Esc (MinimapView `:159`, TimeDial `:138` — chỉ khi chúng đang mở). **K** chưa dùng.
- Test: `Tests/Client/TerrainEditPlayTests.cs` (`ToolStartsDigAndTabToggles` `:143-153`, `FindColumn` `:120-140`), `Tests/Client/SimViewPlayTests.cs`, `Tests/Core/View/SiteStatusTests.cs`.
- API Unity dùng mới trong brief (đều 6000.4): `GUI.Button` [docs](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/GUI.Button.html), `GUI.Box` [docs](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/GUI.Box.html), `GUI.enabled` [docs](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/GUI-enabled.html), `Rect.Contains` [docs](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/Rect.Contains.html), `Screen.width` [docs](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/Screen-width.html); Input System 1.19 `Keyboard.kKey`, `Keyboard.escapeKey` [docs](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.19/api/UnityEngine.InputSystem.Keyboard.html). `Mouse.position` gốc dưới-trái như code hiện có (`TerrainEditInput.cs:84`); GUI gốc trên-trái.

## Quyết định đã chốt (không được đổi)
**User, 2026-10-21 (task contract):**
1. Tab xoay vòng **Đào → Đặt → Xới → Đào**. Xới chỉ có khi trên cùng GameObject có `SimView` đã dựng `Sim`; không có (scene TerrainView) thì Tab vẫn Đào ⇄ Đặt như cũ.
2. Cầm Xới, chuột trái lên ô `digCell`:
   - ô ruộng chưa xới (có biển) → `Unmark`;
   - ô ruộng đã xới (`Tilled`) → mở bảng ô ruộng;
   - không phải ô ruộng → `MarkTill`; −1 → báo "không được".
3. Nút Kho ở góc **dưới-trái** + phím **K** bật/tắt bảng kho.
4. Bảng vẽ bằng **OnGUI** (như SimView). Biển canh tác là hình tạm: cọc + tấm.
5. SimView **bỏ `KeepFarming`** ở trại demo; chỉ cảnh đo (`stressBench`) còn gọi.

**Planner chốt khi viết brief (task contract "Câu hỏi còn mở", theo đề xuất mặc định user đã xác nhận):**
6. **Không** chọn giống lên ô có biển từ client (chuột trái lên biển = bỏ đánh dấu). Lõi vẫn cho phép (`SimDemo` dùng).
7. Bảng ô ruộng chỉ mở khi cầm **Xới**.

**Planner chốt thêm (chi tiết để làm, không đổi luật):**
8. Ô nhắm của Xới = `digCell` của `Pick` (không xét pháp tuyến). Hợp lệ khi `CanMarkTill(ô)` hoặc `PlotAt(ô) >= 0`. Lý do: `CanMarkTill` đã đòi ô trên trống, nên trỏ mặt bên vẫn ra đúng ô có mặt trên lộ ra.
9. Chỉ báo nhắm của Xới dùng **khung trắng** như Đào, ẩn khi ô không hợp lệ.
10. Biển + mầm tạo **dần** theo `State.Plots` (ô mới đánh dấu có biển ngay trong cùng khung hình, không chờ `Update`).
11. Bảng ô ruộng tự đóng khi ô đó `Removed` (bị đào/che). **Esc** đóng bảng ô ruộng khi bảng đang mở.
12. Con trỏ nằm trên nút Kho / bảng kho / bảng ô ruộng → không dùng công cụ, không nhắm (như minimap).

## Phạm vi
- **Được sửa:** `Client/TerrainEditInput.cs`, `Client/SimView.cs`, `Core/View/SiteStatus.cs`, `Tests/Client/TerrainEditPlayTests.cs`, `Tests/Core/View/SiteStatusTests.cs`.
- **Được tạo:** `Tests/Client/SimFarmUiPlayTests.cs` (+ `.meta` do Unity sinh).
- **Không được sửa:** `Core/Sim/*` (lõi đã duyệt ở 0029), scene `.unity`, `design/`, task contract, các test khác, mọi file khác.

## Các bước
Đường dẫn tính từ `client/Assets/Game/`.

1. **`Core/View/SiteStatus.cs`** — thêm (C# thuần, không `using UnityEngine`):
   ```csharp
   /// <summary>"Cây A" — tên tạm theo id trong sim_m1.json (0025; user đặt tên thật sau).</summary>
   public static string CropName(SimConfig cfg, int cropId) => "Cây " + cfg.crops[cropId].id;

   /// <summary>Bảng kho (0025): dòng đầu "Kho chung · đã dùng 14/100", mỗi loại một dòng "Cây A · nông sản 4 · hạt 10". Đã dùng không tính Reserved (như StorageText).</summary>
   public static string StockPanelText(Stockpile s, SimConfig cfg)
   {
       var sb = new System.Text.StringBuilder();
       sb.Append("Kho chung · đã dùng ").Append(s.SeedsTotal + s.ProduceTotal).Append('/').Append(s.Capacity);
       for (int i = 0; i < cfg.crops.Length; i++)
           sb.Append('\n').Append(CropName(cfg, i)).Append(" · nông sản ").Append(s.Produce[i]).Append(" · hạt ").Append(s.Seeds[i]);
       return sb.ToString();
   }

   /// <summary>Dòng hàng đợi thứ index: "1. Cây A — đang làm" / "2. Cây B — chờ" / "2. (trống)". Vụ đầu "đang làm" khi ô đã xới và Phase khác Idle (cùng điều kiện huỷ vụ đang làm của RemoveCrop).</summary>
   public static string QueueLine(FarmPlot p, SimConfig cfg, int index)
   {
       string head = (index + 1) + ". ";
       if (index >= p.Queue.Count) return head + "(trống)";
       bool active = index == 0 && p.Tilled && p.Phase != PlotPhase.Idle;
       return head + CropName(cfg, p.Queue[index]) + (active ? " — đang làm" : " — chờ");
   }
   ```
   Xong khi: biên dịch, test bước 6 (EditMode) đạt.

2. **`Client/SimView.cs` — biển + mầm tạo dần:**
   - Đổi `plotSlabs` thành `readonly List<GameObject> signs`. Xoá vòng `:185-191` trong `Setup`; thay bằng gọi `EnsurePlotVisuals();` (giữ thứ tự: trước `UpdateBodies()`).
   - Thêm:
     ```csharp
     /// <summary>Biển canh tác (cọc + tấm, hình tạm — 0025) và mầm cho mọi ô ruộng chưa có; gọi mỗi khung và ngay sau MarkTill.</summary>
     void EnsurePlotVisuals()
     {
         var ps = Sim.State.Plots;
         while (signs.Count < ps.Count)
         {
             var p = ps[signs.Count];
             var sign = new GameObject("Biển " + p.Id);
             sign.transform.SetParent(root, false);
             sign.transform.localPosition = new Vector3(p.Soil.X + 0.5f, p.Soil.Y + 1f, p.Soil.Z + 0.5f);
             Box("Cọc", new Vector3(0f, 0.35f, 0f), new Vector3(0.08f, 0.7f, 0.08f), PostColor, sign.transform);
             Box("Tấm", new Vector3(0f, 0.65f, 0f), new Vector3(0.5f, 0.3f, 0.05f), SignColor, sign.transform);
             signs.Add(sign);
             var sprout = Box("Mầm " + p.Id, Vector3.zero, Vector3.one, SproutColor, root);
             sprout.SetActive(false);
             sprouts.Add(sprout.transform);
         }
     }
     ```
     Màu: `static readonly Color PostColor = new Color(0.45f, 0.30f, 0.15f);`, `static readonly Color SignColor = new Color(0.90f, 0.80f, 0.55f);`. Xoá `PlotColor` nếu không còn dùng.
   - `UpdatePlotSlabs` đổi tên `UpdatePlotSigns`, thân: `signs[i].SetActive(!ps[i].Tilled && !ps[i].Removed)` (vòng `i < ps.Count && i < signs.Count`).
   - `Update`: trước `UpdateBodies()` (`:127`) gọi `EnsurePlotVisuals();`. `Setup` cuối: `UpdatePlotSigns()` thay `UpdatePlotSlabs()`.
   - Thêm `public int SignCount => signs.Count;` và `public bool SignVisible(int plotId) => plotId < signs.Count && signs[plotId].activeSelf;`.
   Xong khi: `SimViewPlayTests` cũ đạt (`SproutCount` vẫn 4).

3. **`Client/SimView.cs` — bỏ `KeepFarming` ở demo:** `:117` thành `if (Meter != null) SimDemo.KeepFarming(Sim); // chỉ cảnh đo (brief 0028); demo: người chơi tự chọn giống (0025, brief 0030)`. Sửa doc lớp (`:11-19`): thêm dòng "Xới (brief 0030): biển canh tác, bảng ô ruộng chọn giống/hàng đợi, nút Kho (K); trại demo có 1 vụ Cây A ban đầu, sau đó người chơi tự chọn."

4. **`Client/SimView.cs` — thao tác Xới và bảng:**
   ```csharp
   public enum TillResult { Invalid, Marked, Unmarked, OpenedPlot }
   ```
   (khai báo trong namespace `Game.Client`, ngoài lớp). Thêm vào `SimView`:
   ```csharp
   /// <summary>Ô ruộng đang mở bảng; -1 = đóng.</summary>
   public int OpenPlotId { get; private set; } = -1;
   public bool StockOpen { get; private set; }

   static CellPos ToCell(Vector3Int c) => new CellPos(c.x, c.y, c.z);

   /// <summary>Ô Xới nhắm được (brief 0030 mục 8): đánh dấu được, hoặc đang là ô ruộng / ô có biển.</summary>
   public bool CanTillAt(Vector3Int cell) => Sim != null && (Sim.CanMarkTill(ToCell(cell)) || Sim.PlotAt(ToCell(cell)) >= 0);

   /// <summary>Chuột trái khi cầm Xới (0025, brief 0030 mục 2).</summary>
   public TillResult UseTill(Vector3Int cell)
   {
       if (Sim == null) return TillResult.Invalid;
       var c = ToCell(cell);
       int id = Sim.PlotAt(c);
       if (id >= 0)
       {
           if (!Sim.State.Plots[id].Tilled)
           {
               Sim.Unmark(id);
               UpdatePlotSigns();
               return TillResult.Unmarked;
           }
           OpenPlotId = id;
           return TillResult.OpenedPlot;
       }
       id = Sim.MarkTill(c);
       if (id < 0) return TillResult.Invalid;
       EnsurePlotVisuals();
       UpdatePlotSigns();
       return TillResult.Marked;
   }

   /// <summary>Nút "+ Cây X" của bảng ô ruộng.</summary>
   public bool ChooseCrop(int cropId) => OpenPlotId >= 0 && Sim.EnqueueCrop(OpenPlotId, cropId);
   /// <summary>Nút "Xoá" ở dòng index của hàng đợi.</summary>
   public bool RemoveQueued(int index) => OpenPlotId >= 0 && Sim.RemoveCrop(OpenPlotId, index);
   public void ClosePlotPanel() => OpenPlotId = -1;
   public void ToggleStock() => StockOpen = !StockOpen;
   ```
   - `Update`, trong khối `kb != null` (`:106-110`): `if (kb.kKey.wasPressedThisFrame) ToggleStock();` và `if (OpenPlotId >= 0 && kb.escapeKey.wasPressedThisFrame) ClosePlotPanel();`. Sau vòng nhịp (`:125`): `if (OpenPlotId >= 0 && Sim.State.Plots[OpenPlotId].Removed) ClosePlotPanel();`.

5. **`Client/SimView.cs` — vị trí bảng, chặn con trỏ, vẽ OnGUI.**
   - Vị trí (đơn vị GUI, đã chia `scale`; `gw = Screen.width / scale`, `gh = Screen.height / scale`, `scale = Mathf.Max(1f, Screen.height / 720f)`):
     ```csharp
     static float GuiScale => Mathf.Max(1f, Screen.height / 720f);
     public static Rect StockButtonRect(float gw, float gh) => new Rect(10f, gh - 46f, 110f, 36f);
     public static Rect StockPanelRect(float gw, float gh, int crops) { float h = 32f + 24f * crops; return new Rect(10f, gh - 56f - h, 320f, h); }
     public static Rect PlotPanelRect(float gw, float gh) => new Rect(gw * 0.5f - 170f, gh - 250f, 340f, 240f);
     ```
   - Chặn con trỏ:
     ```csharp
     static SimView active;   // Awake: active = this; OnDestroy: if (active == this) active = null;

     /// <summary>Con trỏ (toạ độ màn hình, gốc dưới-trái) trên nút Kho / bảng kho / bảng ô ruộng → công cụ không dùng (brief 0030 mục 12).</summary>
     public static bool BlocksPointer(Vector2 screen) => active != null && active.Sim != null && active.Covers(screen);

     public bool Covers(Vector2 screen)
     {
         float s = GuiScale, gw = Screen.width / s, gh = Screen.height / s;
         var g = new Vector2(screen.x / s, (Screen.height - screen.y) / s);
         if (StockButtonRect(gw, gh).Contains(g)) return true;
         if (StockOpen && StockPanelRect(gw, gh, Sim.Config.crops.Length).Contains(g)) return true;
         return OpenPlotId >= 0 && PlotPanelRect(gw, gh).Contains(g);
     }
     ```
   - `OnGUI`, sau dòng ô ruộng (`:317`) và trước `var cam = Camera.main;` (`:321`); chỉ vẽ khi `!MinimapView.IsAnyOpen`:
     - `GUI.Button(StockButtonRect(gw, gh), "Kho (K)")` → `ToggleStock()`.
     - `StockOpen` → `GUI.Box(StockPanelRect(...), "")` rồi `GUI.Label` trong khung (lề 8) với `SiteStatus.StockPanelText(Sim.State.Stock, Sim.Config)`, `hudStyle`.
     - `OpenPlotId >= 0` → `GUI.Box(PlotPanelRect(gw, gh), "")`; trong khung, lề 8, từ trên xuống, mỗi dòng cao 26:
       1. `GUI.Label`: `SiteStatus.PlotText(p, Sim.State.Tick, Sim.Config.GrowTicks(p.Queue.Count > 0 ? p.Queue[0] : 0))`;
       2. `GUI.Label` "Chọn giống:" và mỗi loại `i` một `GUI.Button` rộng 100 "+ " + `SiteStatus.CropName(Sim.Config, i)` → `ChooseCrop(i)`. Khi `p.Queue.Count >= Sim.Config.farm.plotQueueCapacity` thì `GUI.enabled = false` cho các nút này (trả `true` ngay sau);
       3. `GUI.Label` "Hàng đợi (" + `p.Queue.Count` + "/" + `plotQueueCapacity` + "):";
       4. mỗi `k` trong `[0, plotQueueCapacity)`: `GUI.Label` `SiteStatus.QueueLine(p, Sim.Config, k)`; nếu `k < p.Queue.Count` thêm `GUI.Button` "Xoá" rộng 60 bên phải → `RemoveQueued(k)`;
       5. `GUI.Button` "Đóng (Esc)" → `ClosePlotPanel()`.
     - Vẽ trong cùng `GUI.matrix` đang có (`:305`).
   - Chữ hiển thị chép nguyên văn như trên.

6. **`Client/TerrainEditInput.cs`:**
   - `:9` → `public enum EditTool { Dig, Place, Till }`; doc: "Công cụ đang cầm (brief 0027, 0030): Tab xoay Đào → Đặt → Xới (Xới khi có SimView)."
   - Thêm:
     ```csharp
     SimView simView;
     /// <summary>SimView cùng GameObject đã dựng Sim; null → không có công cụ Xới (scene TerrainView). Tìm lại mỗi lần vì SimView có thể thêm sau Awake.</summary>
     SimView Farm { get { if (simView == null) simView = GetComponent<SimView>(); return simView != null && simView.Sim != null ? simView : null; } }
     public bool TillAvailable => Farm != null;
     public TillResult LastTill { get; private set; }
     ```
   - `ToggleTool`: `Tool = Tool == EditTool.Dig ? EditTool.Place : Tool == EditTool.Place && TillAvailable ? EditTool.Till : EditTool.Dig;`. Doc: "Tab: Đào → Đặt → Xới (khi có SimView) → Đào."
   - `:67` thêm `|| SimView.BlocksPointer(mouse.position.ReadValue())` vào điều kiện chặn; sửa chú thích dòng `:66` thêm "bảng trại (brief 0030)".
   - `ShowAim`: sau `if (!picked || view.Editor == null) return;` thêm:
     ```csharp
     if (Tool == EditTool.Till)
     {
         if (Farm == null || !Farm.CanTillAt(digCell)) return;
         outline.enabled = true;
         outline.transform.localPosition = digCell;
         return;
     }
     ```
   - `UseTool`: đầu hàm thêm:
     ```csharp
     if (Tool == EditTool.Till)
     {
         LastTill = Farm != null ? Farm.UseTill(digCell) : TillResult.Invalid;
         lastResult = LastTill switch
         {
             TillResult.Marked => $"Đánh dấu xới {digCell}",
             TillResult.Unmarked => $"Bỏ đánh dấu {digCell}",
             TillResult.OpenedPlot => $"Mở ruộng {Farm.OpenPlotId}",
             _ => $"Xới {digCell}: không được — chỉ mặt trên ô đất/cỏ, phía trên trống, trong vùng trại",
         };
         // Giữ chữ ký EditResult cho Đào/Đặt; kết quả Xới đọc ở LastTill.
         return LastTill == TillResult.Invalid ? EditResult.NotPlaceable : EditResult.Ok;
     }
     ```
   - `OnGUI` `:189`: tên công cụ `Tool switch { EditTool.Dig => "Đào", EditTool.Place => "Đặt", _ => "Xới" }`. Khi `Tool == EditTool.Till`, chèn thêm một dòng sau dòng đầu: `"Xới: chuột trái lên đất/cỏ = đánh dấu · lên biển = bỏ đánh dấu · lên ô ruộng = chọn giống · K: kho\n"`. Tăng chiều cao `Rect` ở `:204` từ 115 lên 140.

7. **`Tests/Client/TerrainEditPlayTests.cs`:** giữ nguyên `ToolStartsDigAndTabToggles` (không có SimView → Dig ⇄ Place). Không sửa test khác trong file.

8. **Tạo `Tests/Client/SimFarmUiPlayTests.cs`** và thêm test EditMode vào `SiteStatusTests.cs` theo "Phần cần test".

## Tiêu chí xong
- EditMode và PlayMode hết đỏ; ghi tổng số test.
- Test cũ không đổi (kể cả `SimViewPlayTests`, `SimViewMapsPlayTests`, `SimBenchPlayTests`, `TerrainEditPlayTests`).
- `Core/View/SiteStatus.cs` không thêm `using UnityEngine`.
- `git status` sạch ngoài Phạm vi; đã dọn `EditorSettings.asset`.

## Test sau khi xong

### Phần cần test
**EditMode — `SiteStatusTests` (bước 1):**
- `CropNameUsesConfigId`: `new SimConfig()` → `CropName(cfg, 0)` = "Cây A", `CropName(cfg, 1)` = "Cây B".
- `StockPanelTextFormat`: `var s = new Stockpile(2) { Capacity = 100, Reserved = 5 }; s.Seeds[0] = 9; s.Produce[0] = 4; s.Seeds[1] = 10; s.Produce[1] = 3;` → `"Kho chung · đã dùng 26/100\nCây A · nông sản 4 · hạt 9\nCây B · nông sản 3 · hạt 10"`.
- `QueueLineStates`: `new FarmPlot(0, 0, new CellPos(0, 0, 0))`, `Queue = [0, 1]`:
  - `Tilled = true`, `Phase = Sow` → dòng 0 "1. Cây A — đang làm", dòng 1 "2. Cây B — chờ";
  - `Tilled = false`, `Phase = Till` → dòng 0 "1. Cây A — chờ";
  - `Tilled = true`, `Phase = Idle` → dòng 0 "1. Cây A — chờ";
  - `Queue` chỉ còn `[1]` (`Queue` là list readonly: `Clear()` rồi `Add(1)`) → dòng 1 "2. (trống)".

**PlayMode — `SimFarmUiPlayTests` (namespace `Game.Client.Tests`; bước 2–6).** Dựng như `SimViewPlayTests`: `go = new GameObject("Terrain")`, `AddComponent<TerrainChunkView>()`, `AddComponent<SimView>()`, `AddComponent<TerrainEditInput>()`, `yield return null` hai lần. `TearDown`: `Object.Destroy(go); yield return null;`. Helper tìm ô đánh dấu được:
```csharp
Vector3Int FindMarkable(SimView sv, TerrainChunkView view)
{
    var s0 = sv.Sim.State.Plots[0].Soil;
    for (int dz = -8; dz <= 8; dz++)
        for (int dx = -8; dx <= 8; dx++)
        {
            int x = s0.X + dx, z = s0.Z + dz;
            if (!view.World.Size.InBounds(x, 0, z)) continue;
            int h = view.World.SurfaceHeight(x, z);
            if (sv.Sim.CanMarkTill(new CellPos(x, h, z))) return new Vector3Int(x, h, z);
        }
    Assert.Fail("Không tìm được ô đánh dấu được quanh trại");
    return default;
}
```
1. `TabCyclesThreeToolsWithSim`: `TillAvailable` true; `ToggleTool` ×3 → Place, Till, Dig. (Test cũ `ToolStartsDigAndTabToggles` giữ Dig ⇄ Place khi không có SimView.)
2. `MarkShowsSignSameFrame`: Tab tới Till; `cell = FindMarkable`; `UseTool(cell, cell)` → `LastTill` Marked; `id = sv.Sim.PlotAt(cell)` ≥ 4; **không yield**: `SignCount` > id, `SignVisible(id)` true.
3. `SignHidesWhenTilled`: như ca 2, rồi `sv.Sim.Tick()` tới khi `Plots[id].Tilled` (tối đa 3000 nhịp); `yield return null`; `SignVisible(id)` false. Ô đó `Queue` rỗng, `Phase` Idle (không có `KeepFarming`).
4. `UnmarkAndDigHideSign`:
   - Đánh dấu ô `a` (`UseTill(a)` → Marked), rồi `UseTill(a)` lần nữa → Unmarked; `SignVisible(idA)` false ngay; `Plots[idA].Removed`.
   - Đánh dấu lại `a` → id mới `idB`; `view.Dig(a)` → `Ok`; `sv.Sim.Tick()` (lõi xử lý đổi địa hình trong nhịp); `yield return null`; `Plots[idB].Removed`, `SignVisible(idB)` false.
5. `ShowAimTillOutline`: Tab tới Till; `ShowAim(true, cell, cell)` với `cell = FindMarkable` → `OutlineVisible` true, `GhostVisible` false; với ô (0, 0, 0) (đá đáy, không đánh dấu được) → `OutlineVisible` false.
6. `PlotPanelChooseAndRemove`:
   - Ô 0 của demo: `sv.Sim.Tick()` tới khi `Plots[0].Tilled` (tối đa 3000). `UseTill(Soil của ô 0)` → OpenedPlot, `OpenPlotId` 0.
   - Lúc này `Queue` = `[0]` (vụ ban đầu). `ChooseCrop(1)` → true, `Queue` = `[0, 1]`. `ChooseCrop(0)` → false (đầy).
   - `RemoveQueued(1)` → true, `Queue` = `[0]`. `ClosePlotPanel()` → `OpenPlotId` −1; `ChooseCrop(1)` → false.
7. `PlotPanelClosesWhenDug`: mở bảng ô 0 như ca 6; `view.Dig(Soil ô 0)` → `Ok`; `sv.Sim.Tick()`; `yield return null` (đợi `Update` đóng bảng); `OpenPlotId` −1.
8. `DemoDoesNotRefillQueue`: ô 0 đã xới (như ca 6), mở bảng, `RemoveQueued(0)` → `Queue` rỗng. `yield` tới khi `Sim.State.Tick` tăng ít nhất 3 (tối đa 300 khung). `Queue.Count` vẫn 0.
9. `StockToggleAndPointerBlock`:
   - `StockOpen` false; `ToggleStock()` → true; lại → false.
   - Tâm nút Kho (đổi sang toạ độ màn hình: `s = Mathf.Max(1, Screen.height / 720f)`, `r = SimView.StockButtonRect(Screen.width / s, Screen.height / s)`, `screen = new Vector2(r.center.x * s, Screen.height - r.center.y * s)`) → `SimView.BlocksPointer(screen)` true.
   - Tâm màn hình (`Screen.width / 2, Screen.height / 2`) khi không mở bảng ô ruộng → false.
   - Mở bảng ô ruộng (như ca 6) → tâm `PlotPanelRect` (đổi như trên) → true.

**Regression:** `SimViewPlayTests` (`SproutCount` 4, Farmland), `SimViewMapsPlayTests`, `SimBenchPlayTests`, `TerrainEditPlayTests` (toàn bộ), `MinimapViewPlayTests`, `TimeDialPlayTests`; EditMode `SimDemoTests`, `SimDemoMapsTests`, `SimStressTests` — đạt, không đổi số.

### Lệnh executor chạy
1. Unity Editor đang mở project qua MCP. `refresh_unity`, rồi `read_console` (Error): không lỗi biên dịch. Kiểm `SimFarmUiPlayTests.cs.meta` đã sinh.
2. `run_tests` EditMode (toàn bộ) → ghi dòng tổng kết.
3. `run_tests` PlayMode (toàn bộ) → ghi dòng tổng kết. Sau đó `git checkout -- client/ProjectSettings/EditorSettings.asset`.
4. Sửa test mới xong thì chạy lại **toàn bộ** EditMode/PlayMode lần nữa (không chỉ lọc file mới) → ghi dòng tổng kết.
5. `grep -n "UnityEngine" client/Assets/Game/Core/View/SiteStatus.cs` → không có.
6. `grep -n "KeepFarming" client/Assets/Game/Client/SimView.cs` → chỉ còn một dòng, có `Meter != null`.
7. `git status --short` → chỉ file trong Phạm vi (+ brief).

Editor tắt thì dùng `python3 tools/unity_test.py EditMode` / `PlayMode` (cùng bước dọn).

### User tự test
Mở scene `SimView`, bấm Play (PC). Cần xem:
1. **Tab** xoay Đào → Đặt → Xới; nhãn góc trái ghi đúng công cụ, có dòng hướng dẫn Xới.
2. Cầm Xới, trỏ vào mặt cỏ/đất trống gần trại: **khung trắng** hiện; trỏ vào đá/nước: không có khung.
3. Chuột trái → **biển** (cọc + tấm) hiện ngay. L1 tới xới, xới xong biển mất, ô đổi màu ô ruộng, sinh vật **không** gieo (chưa chọn giống).
4. Chuột trái lên biển của ô khác chưa xới → biển mất, L1 (nếu đang tới) về rảnh.
5. Chuột trái lên ô ruộng → **bảng ô ruộng** ở dưới giữa: chọn "+ Cây B" → hàng đợi hiện dòng mới; khi đủ 2 vụ nút chọn mờ đi; "Xoá" bỏ vụ; "Đóng (Esc)" và phím Esc đóng bảng. Bấm nút trên bảng **không** đào/đánh dấu ô phía sau.
6. Nút **"Kho (K)"** góc dưới-trái và phím K: bảng kho hiện đã dùng/tổng và từng loại cây; số đổi khi hàng được chở về.
7. Các ô ruộng demo **không tự trồng lại** sau khi thu hoạch vụ Cây A đầu tiên.
8. Chữ, vị trí bảng không đè minimap/vòng thời điểm; đủ dễ đọc.

## Dừng và chuyển lại Opus khi
- Brief mơ hồ hoặc mâu thuẫn với code thật (số dòng lệch tới mức không tìm thấy đoạn code; chữ ký API lõi khác bước "Bối cảnh").
- Test cũ đỏ (kể cả vì bỏ `KeepFarming`).
- Ca 6 không đúng `Queue` = `[0]` lúc ô 0 vừa xới xong.
- Cần sửa `Core/Sim/*` hoặc file ngoài "Phạm vi".
- Cần API Unity ngoài danh sách ở "Bối cảnh cần đọc".
- Cần một quyết định thuộc AGENTS.md §3.4 hoặc ngoài "Quyết định đã chốt".
- Cùng một lỗi sửa 2 lần vẫn chưa qua.

---
<!-- Từ đây trở xuống: EXECUTOR ghi. -->

## Kết quả
Base commit: 8497f6f · Chạy bằng: claude-sonnet-5-5 · medium · Bắt đầu 2026-10-21 22:25 · kết thúc 22:33
Commit: bc0f01f. File đổi: Client/SimView.cs, Client/TerrainEditInput.cs, Core/View/SiteStatus.cs, Tests/Core/View/SiteStatusTests.cs, Tests/Client/SimFarmUiPlayTests.cs(+.meta). Không lệch brief.
<!-- Base commit (trước khi làm) · Chạy bằng: <model id> · <effort> (dòng OK của brief_check.py) · commit của brief · file đã đổi · ghi chú lệch so với brief (nếu có, kèm lý do). Giờ bắt đầu / kết thúc. -->

## Kết quả test
- SiteStatusTests (CropName, StockPanelText, QueueLine): đạt (EditMode).
- SimFarmUiPlayTests 9 ca (Tab 3 công cụ, biển hiện cùng khung, biển ẩn khi xới, Unmark/Dig, khung nhắm Xới, chọn/xoá giống, bảng đóng khi đào, demo không refill, Kho + chặn con trỏ): đạt.
- Phần User tự test (1–8): chờ user.
- run_tests EditMode → 392/392 đạt. PlayMode → 88/88 đạt (chạy sau khi test mới đã có, một lần). Đã `git checkout` EditorSettings.asset.
- grep UnityEngine SiteStatus.cs → không có; grep KeepFarming SimView.cs → 1 dòng có `Meter != null`; git status chỉ file trong Phạm vi.
<!-- Mỗi dòng ở "Phần cần test" → đạt / không đạt / chờ user + bằng chứng (tên test, dòng output).
Mỗi lệnh ở "Lệnh executor chạy": `<lệnh>` → <kết quả thật, dòng tổng kết>. -->

## Vướng mắc
<!-- Khi trạng thái = cần Opus: câu hỏi cụ thể + các phương án (kèm đề xuất). -->

---
<!-- PLANNER ghi khi review. -->

## Review
<!-- Kết luận: đã duyệt | cần sửa (thêm bước sửa vào "Các bước", trạng thái về chờ làm). Điểm đã đối chiếu. -->
- `Chạy bằng` claude-sonnet-5-5 · medium khớp `Model thực thi` (Sonnet · medium).
- Diff `bc0f01f` (6 file, đúng Phạm vi) khớp bước 1–6 và quyết định 1–12: Tab 3 công cụ chỉ khi có Sim; UseTill Unmark/OpenedPlot/MarkTill; biển tạo dần, hiện cùng khung; chặn con trỏ; K/Esc; bảng đóng khi Removed; `KeepFarming` chỉ khi `Meter != null`; `SiteStatus` không `UnityEngine`. Lệch nhỏ chấp nhận: `break` sau nút "Xoá" (không vẽ tiếp list vừa đổi).
- Phần cần test: 3 ca EditMode + 9 ca PlayMode có trong code, executor báo đạt; EditMode 392/392, PlayMode 88/88 (không chạy lại — kết quả đủ, không đáng ngờ). `git status` sạch.
- User xem 2026-10-22: mục 1–8 ổn, trừ luật hàng đợi (muốn ô đợi theo loại × số lượng). Đây là đổi luật 0025, không phải lỗi của brief → quyết định mới [0026](../../design/decisions/0026-hang-doi-o-doi-theo-loai.md), làm ở sim bước 7d.
- Giờ: Executor 8 phút so với ước lượng 1,5–2,5 ngày-người; đã ghi vào roadmap.
- Kết luận: đã duyệt
