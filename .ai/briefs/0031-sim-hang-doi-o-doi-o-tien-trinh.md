# Brief 0031 — Simulation bước 7d: hàng đợi ô đợi (loại × số lượng), ô tiến trình, tạm dừng xới, bảng ô ruộng dạng ô vuông

> Trạng thái: chờ review
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
Làm bước 7d theo [task contract](../tasks/m1-sim-buoc-7d-hang-doi-o-doi.md), [0026](../../design/decisions/0026-hang-doi-o-doi-theo-loai.md) và [0027](../../design/decisions/0027-o-tien-trinh-tam-dung-xoi-khong-tra-hat.md) (cả hai Chốt):
- lõi: hàng đợi = các **ô đợi** (loại cây + số lượng), N/M riêng từng ô ruộng; **ô tiến trình** (vụ hiện tại) tách khỏi ô đợi; **tạm dừng/tiếp tục xới**; huỷ vụ / đào / che **không trả hạt**;
- client: **bảng ô ruộng dạng ô vuông** (OnGUI).

Ước lượng 2–3 ngày-người. Ghi giờ bắt đầu/kết thúc (theo `date`) vào mục Kết quả.

## Bối cảnh cần đọc
Đường dẫn tính từ `client/Assets/Game/`. Số dòng theo commit `c7b4c41`.
- `Core/Sim/SimData.cs`:
  - `public sealed class FarmPlot` (`:218-240`): `public readonly List<int> Queue` (`:223`), `Phase`, `StageId`, `RipeTick`, `Tilled`, `SoilReady`, `Removed`; constructor `FarmPlot(int id, int orderId, CellPos soil)`.
  - `enum PlotPhase : byte { Idle = 0, Till = 1, Sow = 2, Water = 3, Growing = 4, Harvest = 5 }` (`:62`).
  - `WorkStage.ProgressPermille => (int)(WorkDone * 1000 / WorkRequired)` (`:149`).
- `Core/Sim/Simulation.cs`:
  - `int CropOf(FarmPlot p) => p.Queue[0];` (`:378`) — dùng ở `AbortActive`, `OnStageDone` (Water/Harvest), `AssignPhase` (`:1137`, `:1161`), `Assign` (`:1220`), `Unassign` (`:1249`).
  - `int AbortActive(FarmPlot p, out int stageId, out int creatureId)` (`:381-402`) — đang trả hạt theo pha (`:383`, `:391`, `:400`).
  - `void CancelPlot(FarmPlot p)` (`:405-413`) — `p.Queue.Clear()`, sự kiện `CropCancelled` mang số hạt đã trả.
  - `OnStageDone` (`:737-799`): `case WorkAbility.Till` (`:744-756`), `case WorkAbility.Harvest` (`:767-787`, `p.Queue.RemoveAt(0)` ở `:778`).
  - `public int MarkTill(CellPos soil)` (`:835-844`), `public bool Unmark(int plotId)` (`:847-857`), `public bool RemoveCrop(int plotId, int index)` (`:860-876`), `public bool EnqueueCrop(int plotId, int cropId)` (`:880-891`), `void StartCrop(FarmPlot p)` (`:893-905`).
  - `AssignPhase` (`:1129-1213`): gom công đoạn mở ở `:1146`; `Assign` chỉ được gọi ở `:1203`. `public void Unassign(int creatureId)` (`:1244-1261`) — đặt công đoạn về `Open`, **giữ `WorkDone`**.
- `Core/Sim/SimConfig.cs`: `FarmConfig { public int plotQueueCapacity = 2; }` (`:97-101`), `configVersion = 2` (`:130`), kiểm `farm` (`:300-301`).
- `Core/Sim/SimHash.cs:103-118` (khối ô ruộng; `Queue` ở `:113-114`).
- `Core/Sim/SimDemo.cs`: vụ đầu demo `:95`; `KeepFarming` `:196-201`.
- `Core/View/SiteStatus.cs`: `CropName` (`:13`), `QueueLine` (`:25-32`), `GrowPercent` (`:35-41`), `PlotText` (`:43-56`).
- `Client/SimView.cs`: `Update` (`:102-140`); `ChooseCrop`/`RemoveQueued`/`ClosePlotPanel` (`:311-315`); `PlotPanelRect` (`:320`); `Covers` (`:327-334`); `DrawFarmGui` (`:336-369`); `UpdateSprouts` (`:372-386`, `Queue[0]` ở `:378`); HUD trái `:437`.
- Cấu hình: `design/data/sim_m1.json` và bản chép `client/Assets/Game/Client/Resources/Config/sim_m1.json` — **hai file phải giống hệt nhau** (hiện `diff` rỗng).
- Test chạm tới hàng đợi/trả hạt: `Tests/Core/Sim/CropQueueTests.cs`, `FarmTests.cs`, `PlotRulesTests.cs`, `SimDeterminismTests.cs`, `SimConfigTests.cs`, `Tests/Core/View/SiteStatusTests.cs`, `Tests/Client/SimFarmUiPlayTests.cs`.
- API Unity dùng mới (6000.4):
  - `GUI.DrawTexture(Rect position, Texture image, ScaleMode scaleMode, bool alphaBlend, float imageAspect, Color color, float borderWidth, float borderRadius)` — `borderWidth` 0 = tô kín, `borderRadius` = bo góc [docs](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/GUI.DrawTexture.html).
  - `Texture2D.whiteTexture` [docs](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/Texture2D-whiteTexture.html).
  - `GUIStyle.none` [docs](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/GUIStyle-none.html), `GUIContent.none` [docs](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/GUIContent-none.html) — nút vô hình phủ lên ô hạt giống.
  - Đã dùng ở 0030: `GUI.Button`, `GUI.Box`, `GUI.Label`, `GUI.enabled`.

## Quyết định đã chốt (không được đổi)
**0026 + 0027 (user Chốt 2026-10-22):**
1. Hàng đợi = tối đa **N ô đợi**, mỗi ô đợi = (loại cây, số lượng 1..M). N, M **riêng từng ô ruộng**, gán lúc `MarkTill` từ `farm.queueSlots` (2) / `farm.slotMaxCount` (3).
2. Chọn hạt X: ô đợi **cuối** là X và < M → +1; ngược lại thêm ô đợi mới X×1 nếu còn chỗ; không thì từ chối. **Không gộp** ô cùng loại kề nhau.
3. **+** tăng số lượng ô đợi (≤ M); **−** giảm 1, về 0 → xoá ô đợi, các ô sau đôn lên; **x** xoá cả ô đợi. Ba thao tác này **không đụng** vụ hiện tại.
4. **Ô tiến trình** (`CurrentCrop`): vụ rời ô đợi đầu (−1) **khi mở công đoạn gieo**; lúc xới lại vụ vẫn ở ô đợi. Thu hoạch xong → `CurrentCrop = -1` → vụ kế.
5. Nút trên ô tiến trình: ô ruộng đã xới đang **xới lại** → **tạm dừng** (sinh vật đang làm về rảnh, giữ tiến độ, không giao lại); khi tạm dừng → **Tiếp tục**. Từ gieo trở đi → **huỷ vụ hiện tại, không trả hạt**; ô vẫn là ô ruộng; còn ô đợi thì bắt đầu vụ kế.
6. **Đóng bảng ô ruộng** (nút Đóng hoặc Esc) khi ô đang tạm dừng → tự tiếp tục.
7. Đào / che ô ruộng (0020): **không trả hạt**; hàng đã thu vẫn chở về. Sự kiện `CropCancelled` giữ nguyên, trường `Seeds` luôn 0.
8. Bảng ô ruộng vẽ OnGUI, hình tạm (ô màu bo góc + chữ); art sau.
9. `sim_m1.json`: `"farm": { "queueSlots": 2, "slotMaxCount": 3 }`, bỏ `plotQueueCapacity`, `configVersion` 3.

**Planner chốt khi viết brief (chi tiết để làm, không đổi luật):**
10. Tạm dừng chỉ có khi `Tilled && Phase == Till`. Ô có biển (xới lần đầu) không mở bảng (7c), nên không tạm dừng.
11. Xoá hết ô đợi lúc đang xới lại → xới vẫn chạy xong rồi ô đứng yên (`Idle`, `SoilReady`) — như luật cũ.
12. `SimDemo.KeepFarming` thêm vụ khi **ô đợi rỗng và `CurrentCrop < 0`** — giữ đúng nhịp cũ của các cảnh đo.
13. Gieo bị tạm dừng? Không: tạm dừng chỉ áp vào công đoạn xới.

## Phạm vi
- **Được sửa:** `Core/Sim/SimData.cs`, `Core/Sim/Simulation.cs`, `Core/Sim/SimConfig.cs`, `Core/Sim/SimHash.cs`, `Core/Sim/SimDemo.cs`, `Core/View/SiteStatus.cs`, `Client/SimView.cs`, `design/data/sim_m1.json`, `Client/Resources/Config/sim_m1.json`, và các test: `Tests/Core/Sim/CropQueueTests.cs`, `FarmTests.cs`, `PlotRulesTests.cs`, `SimDeterminismTests.cs`, `SimConfigTests.cs`, `Tests/Core/View/SiteStatusTests.cs`, `Tests/Client/SimFarmUiPlayTests.cs`.
- **Không được sửa:** `design/` (trừ `design/data/sim_m1.json`), task contract, scene `.unity`, `TerrainEditInput.cs`, các file khác. Test khác đỏ vì đổi này → dừng, ghi Vướng mắc.

## Các bước
Đường dẫn tính từ `client/Assets/Game/`.

1. **Cấu hình.**
   - `Core/Sim/SimConfig.cs`: `FarmConfig` thành
     ```csharp
     [Serializable]
     public class FarmConfig
     {
         /// <summary>Số ô đợi gốc của ô ruộng cấp 1 (0026/0027; nâng cấp cộng sau — ideas).</summary>
         public int queueSlots = 2;
         /// <summary>Số vụ tối đa gốc của một ô đợi.</summary>
         public int slotMaxCount = 3;
     }
     ```
     `configVersion = 3`. Thay kiểm `:300-301` bằng:
     ```csharp
     if (farm == null || farm.queueSlots < 1 || farm.queueSlots > 8)
         throw new ArgumentException("farm.queueSlots phải trong [1, 8].");
     if (farm.slotMaxCount < 1 || farm.slotMaxCount > 99)
         throw new ArgumentException("farm.slotMaxCount phải trong [1, 99].");
     ```
   - Cả hai `sim_m1.json`: `"configVersion": 3`, dòng farm thành `"farm": { "queueSlots": 2, "slotMaxCount": 3 },`. Xong khi `diff design/data/sim_m1.json client/Assets/Game/Client/Resources/Config/sim_m1.json` rỗng.

2. **`Core/Sim/SimData.cs`** — thêm lớp (cạnh `FarmPlot`):
   ```csharp
   /// <summary>Ô đợi (0026): một loại cây + số vụ chờ (1..SlotMax của ô ruộng).</summary>
   public sealed class CropSlot
   {
       public readonly int CropId;
       public int Count;
       public CropSlot(int cropId, int count) { CropId = cropId; Count = count; }
   }
   ```
   Trong `FarmPlot`: thay `public readonly List<int> Queue = new List<int>();` bằng
   ```csharp
   /// <summary>Các ô đợi theo thứ tự trồng (0026). Vụ hiện tại không nằm ở đây (0027).</summary>
   public readonly List<CropSlot> Slots = new List<CropSlot>();
   /// <summary>Số ô đợi tối đa (N) và số vụ tối đa mỗi ô đợi (M) của riêng ô ruộng này — gán ở MarkTill từ farm; nâng cấp sau đổi riêng từng ô (0027).</summary>
   public int SlotCount, SlotMax;
   /// <summary>Ô tiến trình (0027): loại cây của vụ đang làm từ lúc mở gieo tới thu hoạch xong / huỷ; -1 = trống.</summary>
   public int CurrentCrop = -1;
   /// <summary>Xới lại đang tạm dừng (0027): công đoạn xới không được giao.</summary>
   public bool TillPaused;
   ```

3. **`Core/Sim/Simulation.cs`** — luật lõi:
   - `CropOf`: `int CropOf(FarmPlot p) => p.CurrentCrop;` (summary: "Loại cây của vụ hiện tại (ô tiến trình, 0027).").
   - Thêm:
     ```csharp
     /// <summary>Mở công đoạn gieo: vụ rời ô đợi đầu sang ô tiến trình (0027). Gọi khi Slots.Count > 0.</summary>
     void BeginSow(FarmPlot p)
     {
         var head = p.Slots[0];
         p.CurrentCrop = head.CropId;
         if (--head.Count == 0) p.Slots.RemoveAt(0);
         p.Phase = PlotPhase.Sow;
         AddPlotStage(p, WorkAbility.Sow);
     }
     ```
     Dùng ở `StartCrop` (nhánh `SoilReady`, thay hai dòng `p.Phase = PlotPhase.Sow; AddPlotStage(p, WorkAbility.Sow);`) và ở `OnStageDone` `case Till` (thay `p.Queue.Count > 0` bằng `p.Slots.Count > 0`, thân thành `BeginSow(p);`).
   - `OnStageDone` `case Harvest`: thay `p.Queue.RemoveAt(0); if (p.Queue.Count > 0)` bằng `p.CurrentCrop = -1; if (p.Slots.Count > 0)` (biến `crop` đã lấy trước đó nên công đoạn vận chuyển vẫn đúng loại).
   - `AbortActive` → không trả hạt:
     ```csharp
     /// <summary>Bỏ công đoạn đang mở của ô (0020/0027): sinh vật đang làm về rảnh, công đoạn Cancelled. Không trả hạt (0027).</summary>
     void AbortActive(FarmPlot p, out int stageId, out int creatureId)
     {
         stageId = p.StageId;
         creatureId = -1;
         if (stageId < 0) return;
         var s = State.Stages[stageId];
         if (s.Status == StageStatus.Assigned)
         {
             creatureId = s.AssigneeId;
             ReleaseCreature(State.Creatures[creatureId]);
         }
         s.Status = StageStatus.Cancelled;
         s.Stall = StallReason.None;
         s.AssigneeKind = AssigneeKind.None;
         s.AssigneeId = -1;
     }
     ```
   - `CancelPlot`: `AbortActive(p, out int stageId, out int creatureId);` · `p.Slots.Clear(); p.CurrentCrop = -1; p.TillPaused = false;` (thay `p.Queue.Clear()`) · sự kiện truyền `0` thay `seeds`. Sửa summary: "Huỷ vụ (0020, 0027): không trả hạt, …".
   - `MarkTill`: sau `State.Plots.Add(p);` thêm `p.SlotCount = Config.farm.queueSlots; p.SlotMax = Config.farm.slotMaxCount;`.
   - `Unmark`: `p.Queue.Clear()` → `p.Slots.Clear()`.
   - **Xoá** `RemoveCrop`. Thay `EnqueueCrop` và thêm các hàm sau (đặt cùng chỗ):
     ```csharp
     /// <summary>Chọn hạt cropId được không (0026 mục 2): ô ruộng còn, có kho, ô đợi cuối cùng loại chưa đầy hoặc còn ô đợi trống.</summary>
     public bool CanEnqueueCrop(int plotId, int cropId)
     {
         if (cropId < 0 || cropId >= Config.crops.Length)
             throw new ArgumentException("Không có loại cây: " + cropId);
         var p = State.Plots[plotId];
         if (p.Removed || State.Storages.Count == 0) return false;
         int n = p.Slots.Count;
         if (n > 0 && p.Slots[n - 1].CropId == cropId && p.Slots[n - 1].Count < p.SlotMax) return true;
         return n < p.SlotCount;
     }

     /// <summary>Chọn hạt cropId (0026 mục 2): ô đợi cuối cùng loại chưa đầy → +1; ngược lại thêm ô đợi mới ×1. Không gộp ô cùng loại kề nhau. Ô đứng yên → bắt đầu vụ.</summary>
     public bool EnqueueCrop(int plotId, int cropId)
     {
         if (!CanEnqueueCrop(plotId, cropId)) return false;
         var p = State.Plots[plotId];
         int n = p.Slots.Count;
         if (n > 0 && p.Slots[n - 1].CropId == cropId && p.Slots[n - 1].Count < p.SlotMax) p.Slots[n - 1].Count++;
         else p.Slots.Add(new CropSlot(cropId, 1));
         if (p.Phase == PlotPhase.Idle) StartCrop(p);
         return true;
     }

     /// <summary>Nút + của ô đợi index (0026 mục 3).</summary>
     public bool IncreaseSlot(int plotId, int index)
     {
         var p = State.Plots[plotId];
         if (p.Removed || index < 0 || index >= p.Slots.Count || p.Slots[index].Count >= p.SlotMax) return false;
         p.Slots[index].Count++;
         return true;
     }

     /// <summary>Nút − của ô đợi index: về 0 → xoá, các ô sau đôn lên. Không đụng vụ hiện tại (0027).</summary>
     public bool DecreaseSlot(int plotId, int index)
     {
         var p = State.Plots[plotId];
         if (p.Removed || index < 0 || index >= p.Slots.Count) return false;
         if (--p.Slots[index].Count == 0) p.Slots.RemoveAt(index);
         return true;
     }

     /// <summary>Nút x của ô đợi index: xoá cả ô đợi, các ô sau đôn lên (0027).</summary>
     public bool ClearSlot(int plotId, int index)
     {
         var p = State.Plots[plotId];
         if (p.Removed || index < 0 || index >= p.Slots.Count) return false;
         p.Slots.RemoveAt(index);
         return true;
     }

     /// <summary>Huỷ vụ trong ô tiến trình (0027): không trả hạt, ô vẫn là ô ruộng; còn ô đợi → vụ kế. Không phát sự kiện.</summary>
     public bool CancelCurrent(int plotId)
     {
         var p = State.Plots[plotId];
         if (p.Removed || p.CurrentCrop < 0) return false;
         var phase = p.Phase;
         AbortActive(p, out _, out _);
         p.CurrentCrop = -1;
         p.StageId = -1;
         if (phase == PlotPhase.Water || phase == PlotPhase.Growing || phase == PlotPhase.Harvest) p.SoilReady = false;
         if (p.Slots.Count > 0) StartCrop(p);
         else p.Phase = PlotPhase.Idle;
         return true;
     }

     /// <summary>Tạm dừng xới lại (0027): chỉ ô ruộng đã xới đang ở pha xới. Sinh vật đang làm về rảnh qua Unassign (giữ tiến độ).</summary>
     public bool PauseTill(int plotId)
     {
         var p = State.Plots[plotId];
         if (p.Removed || !p.Tilled || p.Phase != PlotPhase.Till || p.TillPaused) return false;
         var s = State.Stages[p.StageId];
         if (s.Status == StageStatus.Assigned) Unassign(s.AssigneeId);
         p.TillPaused = true;
         return true;
     }

     /// <summary>Tiếp tục xới (0027).</summary>
     public bool ResumeTill(int plotId)
     {
         var p = State.Plots[plotId];
         if (p.Removed || !p.TillPaused) return false;
         p.TillPaused = false;
         return true;
     }
     ```
   - `AssignPhase` `:1145-1146`: không gom công đoạn của ô đang tạm dừng — thay vòng `for` hai dòng đó bằng:
     ```csharp
     for (int i = 0; i < stages.Count; i++)
     {
         var g2 = stages[i];
         if (g2.Status == StageStatus.Open && g2.AssigneeKind == AssigneeKind.None
             && !(g2.PlotId >= 0 && State.Plots[g2.PlotId].TillPaused)) open.Add(g2);
     }
     ```
   - Xong khi: biên dịch, `grep -n "Queue\b\|RemoveCrop" Core/Sim/Simulation.cs` không còn dòng nào về hàng đợi ô ruộng.

4. **`Core/Sim/SimHash.cs`** — thay 2 dòng `Queue` (`:113-114`) bằng:
   ```csharp
   h = MixInt(h, p.Slots.Count);
   for (int j = 0; j < p.Slots.Count; j++) { h = MixInt(h, p.Slots[j].CropId); h = MixInt(h, p.Slots[j].Count); }
   h = MixInt(h, p.SlotCount);
   h = MixInt(h, p.SlotMax);
   h = MixInt(h, p.CurrentCrop);
   h = MixInt(h, p.TillPaused ? 1 : 0);
   ```

5. **`Core/Sim/SimDemo.cs`** — `KeepFarming`: điều kiện thành `!ps[i].Removed && ps[i].Slots.Count == 0 && ps[i].CurrentCrop < 0`; summary: "Ô ruộng không còn ô đợi và không có vụ hiện tại → thêm một vụ Cây A …".

6. **`Core/View/SiteStatus.cs`** — xoá `QueueLine`; thêm (C# thuần):
   ```csharp
   /// <summary>Số dưới ô đợi index: "2/3"; ô đợi trống "0/3" (0027).</summary>
   public static string SlotCountText(FarmPlot p, int index) =>
       (index < p.Slots.Count ? p.Slots[index].Count : 0) + "/" + p.SlotMax;

   /// <summary>% ô tiến trình (0027): xới/gieo/tưới/thu theo tiến độ công đoạn, lớn theo GrowPercent; đứng yên / đã huỷ = 0.</summary>
   public static int ProgressPercent(FarmPlot p, SimState s, SimConfig cfg)
   {
       if (p.Removed) return 0;
       switch (p.Phase)
       {
           case PlotPhase.Growing: return GrowPercent(p, s.Tick, cfg.GrowTicks(p.CurrentCrop));
           case PlotPhase.Till:
           case PlotPhase.Sow:
           case PlotPhase.Water:
           case PlotPhase.Harvest:
               return p.StageId >= 0 ? s.Stages[p.StageId].ProgressPermille / 10 : 0;
           default: return 0;
       }
   }

   /// <summary>Chữ ô tiến trình (0027): "Xới 60%", "Xới 60% · tạm dừng", "Cây A · gieo 0%", "Cây A · tưới 50%", "Cây A · lớn 40%", "Cây A · thu hoạch 20%", "Trống", "Đã huỷ".</summary>
   public static string ProgressText(FarmPlot p, SimState s, SimConfig cfg)
   {
       if (p.Removed) return "Đã huỷ";
       int pct = ProgressPercent(p, s, cfg);
       switch (p.Phase)
       {
           case PlotPhase.Till: return "Xới " + pct + "%" + (p.TillPaused ? " · tạm dừng" : "");
           case PlotPhase.Sow: return CropName(cfg, p.CurrentCrop) + " · gieo " + pct + "%";
           case PlotPhase.Water: return CropName(cfg, p.CurrentCrop) + " · tưới " + pct + "%";
           case PlotPhase.Growing: return CropName(cfg, p.CurrentCrop) + " · lớn " + pct + "%";
           case PlotPhase.Harvest: return CropName(cfg, p.CurrentCrop) + " · thu hoạch " + pct + "%";
           default: return "Trống";
       }
   }
   ```
   Kiểm tên kiểu trạng thái: `SiteStatus.IsCarrying(SimState s, …)` đã dùng `SimState` — dùng đúng tên đó.

7. **`Client/SimView.cs` — lớp điều khiển bảng** (thay `ChooseCrop`/`RemoveQueued`/`ClosePlotPanel` ở `:311-315`):
   ```csharp
   /// <summary>Ô hạt giống cropId (0026 mục 2).</summary>
   public bool ChooseCrop(int cropId) => OpenPlotId >= 0 && Sim.EnqueueCrop(OpenPlotId, cropId);
   public bool CanChooseCrop(int cropId) => OpenPlotId >= 0 && Sim.CanEnqueueCrop(OpenPlotId, cropId);
   /// <summary>Nút + / − / x của ô đợi index.</summary>
   public bool IncreaseQueued(int index) => OpenPlotId >= 0 && Sim.IncreaseSlot(OpenPlotId, index);
   public bool DecreaseQueued(int index) => OpenPlotId >= 0 && Sim.DecreaseSlot(OpenPlotId, index);
   public bool ClearQueued(int index) => OpenPlotId >= 0 && Sim.ClearSlot(OpenPlotId, index);

   /// <summary>Nút trên ô tiến trình (0027): xới lại → tạm dừng / tiếp tục; từ gieo trở đi → huỷ vụ (không trả hạt).</summary>
   public bool ProgressAction()
   {
       if (OpenPlotId < 0) return false;
       var p = Sim.State.Plots[OpenPlotId];
       if (p.Tilled && p.Phase == PlotPhase.Till)
           return p.TillPaused ? Sim.ResumeTill(OpenPlotId) : Sim.PauseTill(OpenPlotId);
       return Sim.CancelCurrent(OpenPlotId);
   }

   /// <summary>Đóng bảng; ô đang tạm dừng xới → tiếp tục (0027 mục 2).</summary>
   public void ClosePlotPanel()
   {
       if (OpenPlotId >= 0) Sim.ResumeTill(OpenPlotId);
       OpenPlotId = -1;
   }
   ```
   (`ResumeTill` trả false khi không tạm dừng/đã huỷ — bỏ qua kết quả.) `Update` không đổi (Esc và ô bị huỷ đã gọi `ClosePlotPanel`).
   - `UpdateSprouts` `:378` và HUD `:437`: `p.Queue.Count > 0 ? p.Queue[0] : 0` → `p.CurrentCrop >= 0 ? p.CurrentCrop : 0`.

8. **`Client/SimView.cs` — vẽ bảng ô ruộng** (đơn vị GUI như 0030). `PlotPanelRect` thành `new Rect(gw * 0.5f - 200f, gh - 310f, 400f, 300f)`. Thay phần bảng ô ruộng trong `DrawFarmGui` (từ `if (OpenPlotId < 0) return;` tới hết hàm) bằng:
   ```csharp
   if (OpenPlotId < 0) return;
   var pr = PlotPanelRect(gw, gh);
   GUI.Box(pr, "");
   var p = Sim.State.Plots[OpenPlotId];
   var cfg = Sim.Config;
   float x = pr.x + 10f, y = pr.y + 8f;
   GUI.Label(new Rect(x, y, pr.width - 20f, 24f), SiteStatus.PlotText(p, Sim.State.Tick, cfg.GrowTicks(p.CurrentCrop >= 0 ? p.CurrentCrop : 0)), hudStyle);
   y += 26f;

   // Hạt giống: ô vuông bo góc màu cây + chữ, icon hạt góc trên trái.
   GUI.Label(new Rect(x, y, 200f, 22f), "Hạt giống", hudStyle);
   y += 22f;
   for (int i = 0; i < cfg.crops.Length; i++)
   {
       var r = new Rect(x + i * 64f, y, 56f, 56f);
       bool can = CanChooseCrop(i);
       Tile(r, CropColor(i), cfg.crops[i].id, can ? 1f : 0.35f);
       Tile(new Rect(r.x + 4f, r.y + 4f, 14f, 14f), SeedColor, "", can ? 1f : 0.35f, 7f);
       GUI.enabled = can;
       if (GUI.Button(r, GUIContent.none, GUIStyle.none)) ChooseCrop(i);
       GUI.enabled = true;
   }
   y += 64f;

   // Ô tiến trình (trái) + các ô đợi (phải).
   GUI.Label(new Rect(x, y, 90f, 22f), "Đang làm", hudStyle);
   GUI.Label(new Rect(x + 100f, y, 200f, 22f), "Hàng đợi", hudStyle);
   y += 22f;
   var cr = new Rect(x, y, 72f, 72f);
   bool busy = p.Phase != PlotPhase.Idle;
   string cropText = p.CurrentCrop >= 0 ? cfg.crops[p.CurrentCrop].id : (busy ? "Xới" : "");
   Tile(cr, p.CurrentCrop >= 0 ? CropColor(p.CurrentCrop) : EmptyColor, cropText, 1f);
   if (busy)
   {
       if (p.TillPaused)
       {
           if (GUI.Button(new Rect(cr.x + 4f, cr.y + 25f, 64f, 22f), "Tiếp tục")) ProgressAction();
       }
       else if (GUI.Button(new Rect(cr.xMax - 18f, cr.y, 18f, 18f), "x")) ProgressAction();
   }
   var bar = new Rect(cr.x, cr.yMax + 4f, 72f, 8f);
   Tile(bar, EmptyColor, "", 1f, 3f);
   int pct = SiteStatus.ProgressPercent(p, Sim.State, cfg);
   if (pct > 0) Tile(new Rect(bar.x, bar.y, bar.width * pct / 100f, bar.height), BarColor, "", 1f, 3f);
   GUI.Label(new Rect(cr.x - 4f, bar.yMax + 2f, 96f, 22f), SiteStatus.ProgressText(p, Sim.State, cfg), hudStyle);

   for (int k = 0; k < p.SlotCount; k++)
   {
       var r = new Rect(x + 100f + k * 84f, y, 72f, 72f);
       bool has = k < p.Slots.Count;
       Tile(r, has ? CropColor(p.Slots[k].CropId) : EmptyColor, has ? cfg.crops[p.Slots[k].CropId].id : "", 1f);
       float by = r.yMax + 4f;
       GUI.enabled = has;
       if (GUI.Button(new Rect(r.xMax - 18f, r.y, 18f, 18f), "x")) { ClearQueued(k); GUI.enabled = true; break; }
       if (GUI.Button(new Rect(r.x, by, 20f, 20f), "−")) { DecreaseQueued(k); GUI.enabled = true; break; }
       GUI.enabled = has && p.Slots[k].Count < p.SlotMax;
       if (GUI.Button(new Rect(r.xMax - 20f, by, 20f, 20f), "+")) { IncreaseQueued(k); GUI.enabled = true; break; }
       GUI.enabled = true;
       GUI.Label(new Rect(r.x + 22f, by, 28f, 20f), SiteStatus.SlotCountText(p, k), labelStyle);
   }
   GUI.enabled = true;

   if (GUI.Button(new Rect(pr.xMax - 120f, pr.yMax - 36f, 110f, 28f), "Đóng (Esc)")) ClosePlotPanel();
   ```
   Thêm vào lớp:
   ```csharp
   static readonly Color EmptyColor = new Color(0.25f, 0.25f, 0.25f, 0.9f);
   static readonly Color SeedColor = new Color(0.55f, 0.40f, 0.20f);
   static readonly Color BarColor = new Color(0.35f, 0.80f, 0.35f);

   /// <summary>Màu tạm của loại cây (0027 mục 5 — art sau).</summary>
   static Color CropColor(int cropId) =>
       cropId == 0 ? new Color(0.35f, 0.70f, 0.30f) : cropId == 1 ? new Color(0.85f, 0.55f, 0.20f) : new Color(0.6f, 0.6f, 0.6f);

   /// <summary>Ô màu bo góc + chữ giữa (hình tạm, OnGUI).</summary>
   void Tile(Rect r, Color c, string text, float alpha, float radius = 8f)
   {
       c.a *= alpha;
       GUI.DrawTexture(r, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0f, c, 0f, radius);
       if (text.Length > 0) GUI.Label(r, text, labelStyle);
   }
   ```
   (`labelStyle` căn giữa đã có ở `OnGUI`; `DrawFarmGui` chạy sau khi tạo style.) Xong khi: biên dịch, bảng hiện trong Play không lỗi console.

9. **Test lõi — sửa test cũ** (đúng luật mới, giữ mốc thời gian):
   - `CropQueueTests`:
     - `NextCropRetills` (`:124-137`): `Layout(new[] { 0, 0 })` giờ là một ô đợi A2; ở nhịp 1395 kiểm `p.Slots.Count == 1 && p.Slots[0].Count == 1` (thay `p.Queue.Count == 1`); thêm `Assert.AreEqual(-1, p.CurrentCrop)`. Ở 1455 thêm `Assert.AreEqual(0, p.Slots.Count)` và `Assert.AreEqual(0, p.CurrentCrop)`.
     - `RemoveQueuedCropOnlyLeavesQueue` → đổi tên `ClearSlotOnlyLeavesQueue`: `Layout(new[] { 0, 1 })`, nhịp 500 (vụ A đang lớn, ô đợi [B1]) → `ClearSlot(id, 0)` true; `Slots.Count == 0`; `CurrentCrop == 0`; `Phase == Growing`; hạt A không đổi.
     - `RemoveActiveCropRefundsAndKeepsFarmland` → `CancelCurrentNoRefundKeepsFarmland` (giữ 4 TestCase): `CancelCurrent(id)` true; **`Seeds[0] == 9`** (không trả); `CurrentCrop == -1`; `Slots.Count == 0`; các assert còn lại giữ; cuối: `EnqueueCrop(id, 0)` → `Phase` là `Sow` (soilReady) hoặc `Till`; khi `Sow` thì `CurrentCrop == 0`.
     - `RemoveCropOnMarkedPlotKeepsTill` → `ClearSlotOnMarkedPlotKeepsTill`: `ClearSlot(id, 0)` thay `RemoveCrop(id, 0)`; `Slots.Count == 0`.
     - `EnqueueRules` (`:317-333`): sau `Layout(new int[0])` (N=2, M=3): chọn 0 năm lần → 5 lần true, ô đợi [A3, A2]; lần 6 false; `Throws` cho cropId 2 giữ. Phần `bare` và `Unmark` giữ.
     - `SameSeedSameHashWithQueueOps`: `s.RemoveCrop(0, 1)` → `s.ClearSlot(0, 0)`; thêm ở `t == 650`: `s.EnqueueCrop(0, 1); s.IncreaseSlot(0, 0); s.DecreaseSlot(0, 0);` và ở `t == 660`: `s.CancelCurrent(1);`.
   - `FarmTests`:
     - `:101` `plot.Queue.Count` → `plot.Slots.Count`; thêm `Assert.AreEqual(-1, plot.CurrentCrop)`.
     - `QueueRepeatsAndCapsAtTwo` → `QueueRepeatsInOneSlot`: `EnqueueCrop(0, 0)` true → `Slots.Count == 1`, `Slots[0].Count == 2` (trước khi chạy, ô đang xới lần đầu, chưa gieo); bỏ `Assert.IsFalse(sim.EnqueueCrop(0, 0))`. Nhịp 1395: `Phase == Till`, `Slots[0].Count == 1`, các assert sinh vật giữ. Nhịp 1455: `Phase == Sow`, `Slots.Count == 0`, `Seeds[0] == 8`.
   - `PlotRulesTests`:
     - `DigCancelsCropInEachPhase`: **`after` = `before`** ở cả 5 TestCase (`[TestCase(40, 10, 10, 0)]`, `(90, 9, 9, 0)`, `(140, 9, 9, 1)`, `(500, 9, 9, -1)`, `(1370, 9, 9, 1)`); `plot.Queue.Count` → `plot.Slots.Count`; thêm `Assert.AreEqual(-1, plot.CurrentCrop)` và `Assert.IsFalse(plot.TillPaused)`; `ev[0].Seeds` thành `Assert.AreEqual(0, ev[0].Seeds)`.
     - `RetillKeepsFarmlandWithoutEvent` `:81`: `Assert.AreEqual(2, plot.Queue.Count)` → `Assert.AreEqual(2, plot.Slots[0].Count)`.
     - `RefundExceedsFullStorage` → đổi tên `DigDoesNotRefundSeed`: sau đào, `Assert.AreEqual(9, store.Seeds[0])`; bỏ assert `Free`.
     - `CoverCancelsAndRevertsToDirt` (nhịp 500): `Seeds[0]` 10 → **9**. `WaterOnSiteCountsAsCover` (nhịp 500): 10 → **9**. `CoverUntilledLeavesMaterial` (nhịp 40, chưa gieo): giữ 10.
   - `SimDeterminismTests`, `SimConfigTests`: `c.farm.plotQueueCapacity = 0` → hai khối: `c.farm.queueSlots = 0` ("ô đợi 0") và `c.farm.slotMaxCount = 0` ("tối đa 0"), đều `Throws<ArgumentException>`. `SimDeterminismTests` chỉ sửa nếu biên dịch lỗi / kỳ vọng hạt dựa vào trả hạt.

10. **Test lõi — thêm** (vào `CropQueueTests`, dùng `Layout`/`RunTo` sẵn có):
    - `EnqueueFillsSlotsByRule`: config N=3, M=3 (`var cfg = TestWorlds.NoWander(); cfg.farm.queueSlots = 3;`), `Layout(new int[0], cfg)`: A×3, B×3, A×3 (9 lần true) → ô đợi [A3, B3, A3]; A lần 10 và B lần 10 false. Chạy tối đa 8000 nhịp, mỗi khi `Phase` chuyển sang `Sow` ghi `CurrentCrop`; kỳ vọng 4 lần đầu là `0, 0, 0, 1`.
    - `TwoSlotsAThenB`: N=2, M=3 mặc định: A×3 rồi B×3 → [A3, B3]; B lần 4 false.
    - `SlotPlusMinusClearShift`: dựng [A3, B2, A1] (N=3) → `ClearSlot(1)` → [A3, A1] (không gộp); `DecreaseSlot(1)` → [A3]; `IncreaseSlot(0)` false (đã 3); `DecreaseSlot(0)` ×3 → []; chỉ số sai → false.
    - `SowMovesCropToProgress`: `Layout(new[] { 0, 0 })`; tick từng nhịp, khi `Phase` còn `Till` thì luôn `CurrentCrop == -1`, `Slots[0].Count == 2`; nhịp đầu tiên `Phase == Sow`: `CurrentCrop == 0`, `Slots[0].Count == 1`.
    - `PauseTillKeepsProgressAndBlocksAssign`: `Layout(new[] { 0, 0 })`, chạy tới `Phase == Till && p.Tilled` (xới lại, ~1395) + 10 nhịp; ghi `WorkDone` của công đoạn xới; `PauseTill` true; sinh vật xới về rảnh (`StageId == -1`); chạy 200 nhịp: công đoạn không `Assigned`, `WorkDone` không đổi; `PauseTill` lần 2 false; `ResumeTill` true; chạy tới xới xong → `Phase == Sow`. `PauseTill` trên ô `Idle`/đang gieo/ô có biển → false.
    - `CancelCurrentStartsNext`: `Layout(new[] { 0, 1 })`, nhịp 500 (A đang lớn) → `CancelCurrent` → `Phase == Till` (SoilReady false), `Slots[0]` là B1 tới khi gieo; `Seeds[0] == 9`.
    - `PerPlotSlotLimits`: `Layout(new int[0])`, `int id2 = sim.MarkTill(new CellPos(6, 1, 5));`; cả hai `SlotCount == 2`, `SlotMax == 3`; đặt `sim.State.Plots[id].SlotCount = 3` → ô `id` nhận A, B, A (3 lần true), ô `id2` nhận A, B rồi A false.
    - `KeepFarmingRefillsOnlyWhenEmpty`: `Layout(new[] { 0 })`, `RunTo(sim, 500)` (`CurrentCrop == 0`, `Slots` rỗng) → `SimDemo.KeepFarming(sim)` → `Slots` vẫn rỗng. `RunTo(sim, 1395)` (thu hoạch xong, `Idle`, `CurrentCrop == -1`) → `KeepFarming` → `Slots[0].Count == 1`, `Phase == Till`.
    - `SiteStatusTests`: xoá `QueueLineStates`; thêm `SlotCountTextFormat` ("2/3", ô trống "0/3"), `ProgressTextStates` (`var st = new Simulation(new SimConfig(), TestWorlds.Flat(1)).State;` + `new FarmPlot(0, 0, new CellPos(0, 0, 0))` gán tay, `StageId = -1`: Till → "Xới 0%"; `TillPaused` → "Xới 0% · tạm dừng"; `CurrentCrop = 0`, Growing, `st.Tick = 0`, `RipeTick = GrowTicks(0) / 2` → "Cây A · lớn 50%"; Idle → "Trống"; Removed → "Đã huỷ").

11. **Test PlayMode — `SimFarmUiPlayTests`** (sửa + thêm, dùng `OpenPlot0` sẵn có; sau `OpenPlot0` ô 0 đang gieo: `Slots` rỗng, `CurrentCrop == 0`):
    - `PlotPanelChooseAndRemove` → `PlotPanelSlots`: `ChooseCrop(1)` ×2 → [B2]; `ChooseCrop(0)` → [B2, A1]; `CanChooseCrop(1)` false và `ChooseCrop(1)` false; `ChooseCrop(0)` → [B2, A2]; `IncreaseQueued(0)` → B3; `IncreaseQueued(0)` false; `DecreaseQueued(1)` ×2 → [B3]; `ClearQueued(0)` → []; `ClosePlotPanel` → `ChooseCrop(1)` false.
    - `DemoDoesNotRefillQueue`: thay `sv.RemoveQueued(0)` bằng `sv.ProgressAction()` (huỷ vụ đang gieo, true); kiểm `Slots.Count == 0 && CurrentCrop == -1` trước và sau 3 nhịp.
    - Thêm `ProgressPauseResumeAndCloseResumes`: `OpenPlot0`; tick tới `Phase == Water || Growing`; `ProgressAction()` (huỷ) → `ChooseCrop(0)` → `Phase == Till`; `ProgressAction()` → `TillPaused`; tick 20 nhịp → công đoạn xới không `Assigned`; `ProgressAction()` → hết tạm dừng; `ProgressAction()` lần nữa → tạm dừng; `ClosePlotPanel()` → `TillPaused == false`.
    - `PlotPanelClosesWhenDug`, `StockToggleAndPointerBlock`: giữ (rect mới vẫn chặn con trỏ).

## Tiêu chí xong
- Toàn bộ test EditMode + PlayMode đạt (số mới ghi vào Kết quả).
- `grep -rn "Queue\b" Core/Sim Core/View Client/SimView.cs` không còn chỗ dùng `FarmPlot.Queue`; `grep -rn "RemoveCrop\|plotQueueCapacity\|QueueLine" Core Client Tests` rỗng.
- `grep -rn "using UnityEngine" Core` rỗng.
- Hai `sim_m1.json` giống hệt.

## Test sau khi xong

### Phần cần test
- Cấu hình `farm.queueSlots`/`slotMaxCount`, `configVersion` 3 (bước 1) · `SimConfigTests` (nạp file thật + sai giá trị) · nạp được, 0 → lỗi.
- Ô đợi: chọn hạt theo luật 0026 (bước 3) · `EnqueueRules`, `EnqueueFillsSlotsByRule`, `TwoSlotsAThenB` · A×5 → [A3, A2]; [A3, B3]; [A3, B3, A3] trồng đúng thứ tự.
- +/−/x, đôn lên, không gộp (bước 3) · `SlotPlusMinusClearShift`, `ClearSlotOnlyLeavesQueue`, `ClearSlotOnMarkedPlotKeepsTill` · đúng danh sách.
- N/M riêng từng ô (bước 2–3) · `PerPlotSlotLimits` · ô 1 nhận 3, ô 2 nhận 2.
- Ô tiến trình: vụ rời ô đợi lúc gieo, thu hoạch → vụ kế (bước 3) · `SowMovesCropToProgress`, `NextCropRetills`, `QueueRepeatsInOneSlot` · đúng nhịp cũ (1395 xới lại, 1455 gieo).
- Tạm dừng / tiếp tục xới (bước 3) · `PauseTillKeepsProgressAndBlocksAssign` · tiến độ giữ, không giao, tiếp tục xong.
- Huỷ vụ hiện tại không trả hạt, vụ kế bắt đầu (bước 3) · `CancelCurrentNoRefundKeepsFarmland`, `CancelCurrentStartsNext` · hạt 9.
- Đào/che không trả hạt (bước 3) · `DigCancelsCropInEachPhase`, `DigDoesNotRefundSeed`, `CoverCancelsAndRevertsToDirt`, `CancelClearsStallMarker` · hạt không đổi, sự kiện `Seeds == 0`.
- Mã băm (bước 4) · `SameSeedSameHashWithQueueOps`, `SameSeedSameHashWithEdits`, `SimDeterminismTests` · trùng từng nhịp.
- `KeepFarming` (bước 5) · `KeepFarmingRefillsOnlyWhenEmpty` + regression `FarmTests`, `SimDemoTests`, `SimDemoMapsTests`, `SimBenchPlayTests` · mốc thời gian không đổi.
- Chuỗi hiển thị (bước 6) · `SiteStatusTests` · "2/3", "Xới 60% · tạm dừng"…
- Bảng ô ruộng (bước 7–8) · `PlotPanelSlots`, `ProgressPauseResumeAndCloseResumes`, `DemoDoesNotRefillQueue`, `PlotPanelClosesWhenDug`, `StockToggleAndPointerBlock` · đạt.
- Regression client · `SimViewPlayTests`, `SimViewMapsPlayTests`, `TerrainEditPlayTests` · đạt.

### Lệnh executor chạy
1. `diff design/data/sim_m1.json client/Assets/Game/Client/Resources/Config/sim_m1.json` → rỗng.
2. `run_tests` EditMode qua MCP (Unity Editor đang mở — 0010) → dòng tổng kết.
3. `run_tests` PlayMode qua MCP → dòng tổng kết; sau đó `git checkout -- client/ProjectSettings/EditorSettings.asset`.
4. `read_console` (Error) sau khi biên dịch → 0 lỗi.
5. `grep -rn "using UnityEngine" client/Assets/Game/Core` → rỗng; grep ở "Tiêu chí xong".
6. `python3 tools/check_design.py` → hợp lệ (đã sửa `design/data/sim_m1.json`).

### User tự test
Mở SimView, cầm Xới (Tab tới Xới), bấm ô ruộng của trại demo:
1. Bảng có hàng **Hạt giống** (ô vuông màu bo góc, chữ A/B, chấm nâu góc trên trái), ô **Đang làm** (màu cây + thanh % + chữ), 2 ô **Hàng đợi** có x, −, "số/3", +.
2. Bấm A 5 lần → A3, A2; ô A thành mờ khi đầy. A3 rồi B → A3, B1.
3. −/+/x trên ô đợi đổi số / xoá và ô sau đôn lên; vụ đang làm không đổi.
4. Khi ô đang **xới lại** (sau thu hoạch): bấm x → "Tạm dừng", sinh vật bỏ đi, bấm **Tiếp tục** → làm tiếp; hoặc tạm dừng rồi đóng bảng → làm tiếp.
5. Khi đang gieo/tưới/lớn: bấm x → vụ huỷ, kho **không** được trả hạt; vụ kế (nếu có) bắt đầu xới.
6. Bảng dễ đọc, không che minimap/nút thời điểm.

## Dừng và chuyển lại Opus khi
- Brief mơ hồ hoặc mâu thuẫn với code thật (vd số dòng lệch nhiều, mốc nhịp test cũ đổi).
- Cần một quyết định thuộc AGENTS.md §3.4 hoặc ngoài "Quyết định đã chốt".
- Cùng một lỗi sửa 2 lần vẫn chưa qua.
- Phải sửa file ngoài "Phạm vi".
- Cần API Unity không có trong brief.

---
<!-- Từ đây trở xuống: EXECUTOR ghi. -->

## Kết quả

Base commit: 50b53ed
Chạy bằng: Sonnet · medium
Bắt đầu: 2026-10-22 09:56 · Kết thúc: 2026-10-22 10:04 (~8 phút thực tế, so với ước lượng 2–3 ngày-người)
Commit: 1cfc7f8. File đổi: SimConfig, SimData, Simulation, SimHash, SimDemo, SiteStatus, SimView, hai sim_m1.json, và 6 file test (CropQueueTests, FarmTests, PlotRulesTests, SimConfigTests, SiteStatusTests, SimFarmUiPlayTests).

## Kết quả test
- Cấu hình, ô đợi, +/−/x, N/M riêng ô, ô tiến trình, tạm dừng, huỷ không trả hạt, đào/che, mã băm, KeepFarming, chuỗi hiển thị: đạt (EditMode).
- Bảng ô ruộng (PlotPanelSlots, ProgressPauseResumeAndCloseResumes, DemoDoesNotRefillQueue, PlotPanelClosesWhenDug, StockToggleAndPointerBlock) + regression client: đạt (PlayMode). Hình ảnh bảng: **chờ user** (mục "User tự test").
- Lệnh: diff 2 sim_m1.json → rỗng · EditMode 402/402 đạt (2 test sai kỳ vọng sửa rồi chạy lại đạt) · PlayMode 89/89 đạt · console Error 0 · grep UnityEngine trong Core rỗng, grep RemoveCrop/plotQueueCapacity/QueueLine rỗng · check_design hợp lệ · đã git checkout EditorSettings.asset.

## Vướng mắc
- Brief bước 9 (EnqueueRules) ghi "lần 6 false" sau [A3, A2], nhưng luật 0026 mục 2 (ô đợi cuối cùng loại < M → +1) cho lần 6 → [A3, A3] true, lần 7 mới false. Tôi sửa test theo luật (6 true, 7 false), không đổi code. Planner xác nhận. (UI test PlotPanelSlots khớp luật: B2 → +A1 → +A→A2.)
- PauseTillKeepsProgressAndBlocksAssign: thay RunTo(2200) bằng vòng chờ hết pha Till rồi kiểm Sow (mốc cố định rơi vào Growing).

---
<!-- PLANNER ghi khi review. -->

## Review
