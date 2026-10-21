# Brief 0026 — Simulation bước 6a: ô ruộng có sau khi xới; đào / đặt khối lên thì huỷ vụ

> Trạng thái: chờ review
> Review: đầy đủ + user xem
> Model thực thi: Sonnet · medium
> Cập nhật: 2026-10-21 17:45

<!-- Planner (Opus) viết, Executor (Sonnet) làm theo ở PHIÊN KHÁC — executor không có lịch sử chat, không được tự quyết.
Trạng thái: chờ làm → đang làm → chờ review → đã duyệt; hoặc → cần Opus (executor dừng, hỏi ở mục Vướng mắc).
Review: nhẹ (đọc mục Kết quả) | đầy đủ (đọc cả diff) | user xem (có phần user tự kiểm bằng mắt/chơi thử) — có thể ghép "đầy đủ + user xem".
Model thực thi: BẮT BUỘC, đúng dạng `<Model> · <effort>` — Model: Opus | Sonnet | Haiku | Fable; effort: low | medium | high | xhigh | max (chọn theo AGENTS.md §10.2).
Executor kiểm tra model + effort của phiên TRƯỚC khi làm (scripts/brief_check.py); lệch hoặc không xác định được → cảnh báo user và DỪNG.
File được commit. Tự kiểm trước khi giao (AGENTS.md §10.3): executor có phải hỏi gì không? còn chỗ nào bỏ ngỏ 2 phương án? -->

## Mục tiêu
Làm bước 6a theo [task contract](../tasks/m1-sim-buoc-6a-luat-o-ruong.md) và quyết định [0020](../../design/decisions/0020-o-ruong-tao-khi-xoi-va-huy-vu.md) (Chốt):
- Sinh vật xới xong → ô đất/cỏ thành vật liệu mới **ô ruộng** (`BlockMaterial.Farmland`). Ô này có màu riêng, không có mép cỏ. Mô phỏng ghi vào world qua `WorldEditor` nên mesh tự dựng lại.
- Đào ô ruộng, hoặc đặt khối/nước lên trên nó → **huỷ vụ**: trả 1 hạt nếu đã trừ hạt mà chưa thu. Ô rời lệnh, hàng đợi bị xoá, phát sự kiện `CropCancelled`.

Ước lượng của cả bước: 2,5–4 ngày-người. Ghi giờ bắt đầu/kết thúc vào mục Kết quả.

## Bối cảnh cần đọc
Đường dẫn tính từ `client/Assets/Game/`.
- `Core/Cell.cs:4-13` — `enum BlockMaterial : byte { Empty = 0, Grass = 1, Dirt = 2, Stone = 3, Sand = 4, Water = 5, Bedrock = 6 }`. `Cell.IsDiggable` (`:63-67`) cho phép đào mọi vật liệu trừ rỗng, nước, đá đáy và khối cốt truyện.
- `Core/WorldEditor.cs`:
  - `IsPlaceable` (`:65-66`).
  - `TryDig` (`:69-84`): `Inventory.Add(material)` ở `:79`.
  - `TryPlace` (`:87-107`): ghi ô rồi gọi `CellChanged` ở `:104-105`.
  - `FillWater` (`:157-162`), được `StepWater` gọi.
  - `public event Action<int, int, int> CellChanged;` (`:53`), `public Func<int, int, int, bool> IsOccupied;` (`:50`), `public readonly WorldData World;` (`:33`).
- `Core/Sim/SimData.cs`:
  - `StageStatus` (`:30`), `SimEventKind` (`:35`).
  - `SimEvent` (`:38-56`): constructor `public SimEvent(long tick, SimEventKind kind, int stageId, int creatureId, CellPos cell, StallReason reason)`.
  - `FarmPlot` (`:200-215`): `Soil`, `Site = Soil + (0,1,0)`, `Queue`, `Phase`, `StageId`, `RipeTick`.
- `Core/Sim/Simulation.cs`:
  - `OnCellChanged` (`:329`): chỉ đặt `terrainDirty = true`. `Tick()` (`:331-352`): `TerrainPhase` chạy trước `WorkPhase`.
  - `OnPlot` (`:388-394`), `TerrainPhase` (`:442-481`), `OnStageDone` (`:632-679`, nhánh xới ở `:639-642`), `CropPhase` (`:681-693`), `AddFarmOrder` (`:695-726`, kiểm trùng ô ở `:714-715`), `EnqueueCrop` (`:729-736`), `Respawn` (`:272-310`, lọc công đoạn ở `:300`), `AssignPhase` (`:939-1026`), `Assign` (`:1028-1052`).
  - **Hạt bị trừ lúc giao công đoạn gieo** (`:1030`). `Unassign` (`:1054-1079`) trả lại hạt nếu công đoạn gieo bị bỏ giữa chừng (`:1059`). Gieo xong thì hạt đã dùng.
  - Công đoạn xới/gieo/tưới/thu hoạch của ô ruộng có `PlotId >= 0` và `StorageId = -1`, nên không giữ chỗ kho. Vận chuyển sau thu hoạch có `PlotId = -1` và `StorageId >= 0` (`:653-658`).
- `Core/Sim/SimHash.cs:96-109` — phần băm `Plots`.
- `Core/Sim/SimDemo.cs:191-196` — `KeepFarming`.
- `Core/View/SiteStatus.cs:21-33` — `PlotText`.
- `World/BlockMesher.cs:91-102` — `TopColor`, `SideColor`. Palette dựng cho đủ 256 vật liệu ở `:278-282`. Mép cỏ chỉ dựng cho `Grass` (`:433`, `:746`).
- `Client/SimView.cs`:
  - Nối editor ở `:110-111` và `:121-122`.
  - Tấm "Ruộng" mỏng màu tối nằm trên mỗi ô ruộng (`:143-149`). Tấm này che màu mặt trên.
  - `UpdateSprouts` (`:202-217`), được gọi ở `:102`.
- Mốc thời gian của bố trí FarmTests (`Tests/Core/Sim/FarmTests.cs:23-33`, `:60-120`), dùng lại cho test mới. Bố trí: `Flat(1)`, `NoWander()`, L1 (8,2,5), L2 (5,2,1), L3 (9,2,5), kho (6,2,9), một ô ruộng soil (5,1,5).

  | Nhịp | Diễn biến |
  |---|---|
  | 15 | L1 bắt đầu xới |
  | 75 | Xới xong; L1 bắt đầu gieo ngay; hạt 10 → 9 |
  | 105 | Gieo xong, chuyển sang tưới |
  | 125–155 | L2 tưới |
  | 155 | Bắt đầu lớn, `RipeTick` = 1355 |
  | 1355 | Chín; L2 thu hoạch |
  | 1395 | Thu xong (`Phase` Idle, `Queue` 0); vận chuyển giao cho L3, `Reserved` 2 |
  | 1417 | L3 đã cầm hàng |
  | 1459 | Hàng vào kho: Produce 1, Seeds 10 |

  Bước 6a **không được làm lệch các mốc này**: xới đổi vật liệu nhưng tìm đường vẫn coi ô đó là khối đặc như trước.

## Quyết định đã chốt (không được đổi)
**Từ 0020 (user Chốt 2026-10-19):**
1. Lệnh trồng cây vẫn đặt lên ô đất/cỏ có ô trống phía trên (giữ luật kiểm của `AddFarmOrder`).
2. Xới xong một ô của lệnh → ô đó thành `Farmland`, ở **đúng nhịp xới xong**. Vụ kế trên ô đã là ô ruộng vẫn bắt đầu lại từ xới; xới lại không đổi vật liệu và không phát `CellChanged`.
3. Đào ô ruộng → ô thành rỗng, kho vật liệu của người chơi **+1 đất** (không cộng ô ruộng).
4. Đặt khối lên ô phía trên ô ruộng → ô ruộng **trở lại đất**. Gỡ khối ra thì thấy ô đất.
5. Huỷ vụ:
   - Đã trừ hạt mà chưa thu → **+1 hạt vào kho ngay**, kể cả khi kho đầy (`Free` có thể âm).
   - Nông sản chưa thu thì mất.
   - Hàng đã thu (nằm tại ô hoặc đang chở) **vẫn chở vào kho**: không đụng công đoạn vận chuyển.
6. Sau khi huỷ vụ: ô rời lệnh, hàng đợi vụ bị xoá. Muốn trồng lại phải đặt lệnh mới.
7. Thứ tự chọn việc giữ nguyên. Không làm phép tưới nước. Không có thông báo HUD riêng: chỉ phát sự kiện trong lõi và ghi chữ "đã huỷ" ở bảng ruộng.

**Planner đề xuất trong 0020, user xác nhận khi duyệt brief này — Executor làm đúng như sau:**

8. **"Bị che" = ô `Site` không còn rỗng**, kể cả khi nước tràn vào. Nước làm giống đặt khối: `FillWater` cũng đổi ô ruộng bên dưới về đất.
9. **Ô của lệnh chưa xới xong** (vẫn là đất/cỏ) bị đào hoặc che → rời lệnh như mục 6, trả 0 hạt, vật liệu giữ nguyên. Tiến độ xới dở mất.
10. **Sinh vật đang làm công đoạn bị huỷ** (đang đi tới hoặc đang làm) → về rảnh ngay ở nhịp đó. Công đoạn của ô ruộng không giữ chỗ kho nên không có gì để nhả. Hạt của công đoạn gieo đã giao được tính vào số hạt trả (mục 12), không trả hai lần.
11. **Hàng đã thu nằm tại ô bị đào/che:** công đoạn vận chuyển giữ nguyên `Target`, không sửa code.
12. **Số hạt trả:**
    - 1 khi `Phase` là `Water`, `Growing` hoặc `Harvest`;
    - 1 khi `Phase` là `Sow` và công đoạn gieo đang `Assigned`;
    - 0 trong mọi trường hợp khác: `Till`, `Sow` chưa giao (Open/Blocked), `Idle`.
13. **Sự kiện** `SimEventKind.CropCancelled` (giá trị 4):
    - `StageId` = công đoạn đang mở của ô (-1 nếu không có);
    - `CreatureId` = sinh vật bị cho về rảnh (-1 nếu không có);
    - `Cell` = `Soil`, `Reason` = `None`;
    - trường mới `Seeds` = số hạt trả.
14. **Cách nối mô phỏng → world (Planner chốt):**
    - Thêm `Simulation.AttachEditor(WorldEditor)`, thay cho hai dòng nối tay trong SimView.
    - Có editor: xới xong gọi `Editor.TryTill(...)`. Hàm này phát `CellChanged`, nhờ đó mesh và minimap cập nhật.
    - Không có editor (test cũ, SimDemoTests): ghi thẳng bằng `WorldEditor.Till(World, ...)` (static, cùng luật, không phát sự kiện).
    - Ô do mô phỏng tự đổi khi xới **không** làm bẩn địa hình của mô phỏng (cờ `writingWorld`). Lý do: độ đặc của ô không đổi; tránh chạy `TerrainPhase` + `HomePhase` (A* mọi sinh vật) sau mỗi lần xới, việc này sẽ tốn ở bước 7 (đo 60 sinh vật).
15. **Phát hiện huỷ:** khi `terrainDirty`, đầu `TerrainPhase` duyệt mọi ô ruộng chưa `Removed` và kiểm `PlotIntact`. Lý do:
    - bắt được cả trường hợp test ghi thẳng `World.Set` rồi gọi `OnCellChanged`;
    - bắt được trường hợp đào rồi đặt lại đất trong cùng một nhịp: ô ruộng thành đất, coi như bị đào.
16. **Hiển thị:**
    - `TopColor(Farmland)` = `Pack(150, 104, 66, PatternDirt)` (nâu sẫm hơn đất).
    - `SideColor(Farmland)` = màu đất.
    - Không có mép cỏ: code hiện tại đã chỉ dựng mép cho `Grass`.
    - SimView ẩn tấm "Ruộng" khi ô đã xới hoặc đã huỷ, để thấy màu ô ruộng.
17. Công đoạn bị huỷ có trạng thái mới `StageStatus.Cancelled` (giá trị 4). Công đoạn này trước đó bị `Blocked` thì `ReportPhase` sẵn có sẽ phát `StallCleared`, nên dấu "!" tự mất. Không sửa `ReportPhase`.

## Phạm vi
- **Được sửa:**
  - `client/Assets/Game/Core/Cell.cs`, `Core/WorldEditor.cs`
  - `Core/Sim/SimData.cs`, `Core/Sim/Simulation.cs`, `Core/Sim/SimHash.cs`, `Core/Sim/SimDemo.cs`
  - `Core/View/SiteStatus.cs`, `World/BlockMesher.cs`, `Client/SimView.cs`
  - Test: `Tests/Core/WorldEditorTests.cs`, `Tests/Core/View/SiteStatusTests.cs`, `Tests/World/BlockMesherTests.cs`, `Tests/World/SimDemoTests.cs`, `Tests/Client/SimViewPlayTests.cs`
- **Được tạo:** `Tests/Core/Sim/PlotRulesTests.cs` (+ `.meta` do Unity sinh khi refresh).
- **Không được sửa:**
  - `design/` (Planner cập nhật khi review), task contract;
  - `GridPathfinder.cs`, `MinimapImage.cs`, bộ sinh địa hình, shader, các test cũ khác (chỉ được thêm assert ở các file test trong danh sách trên);
  - mọi file khác.

## Các bước
Đường dẫn tính từ `client/Assets/Game/`.

1. **`Core/Cell.cs`** — thêm vào cuối `BlockMaterial`:
   ```csharp
   /// <summary>Ô ruộng: ô đất/cỏ sinh vật đã xới xong (0020). Không đặt được; đào ra +1 đất; bị che thì trở lại đất.</summary>
   Farmland = 7,
   ```
   Xong khi: biên dịch được. `IsDiggable(Farmland)` = true (không sửa `IsDiggable`).

2. **`Core/WorldEditor.cs`:**
   - a. `TryDig`: thay `Inventory.Add(material);` (`:79`) bằng `Inventory.Add(material == BlockMaterial.Farmland ? BlockMaterial.Dirt : material);`. Sửa doc comment: "vật liệu +1 vào kho (ô ruộng → đất, 0020)".
   - b. Thêm hai hàm (đặt sau `TryPlace`):
     ```csharp
     /// <summary>Xới (0020): ô đất/cỏ → ô ruộng, cờ đã sửa bật. Chỉ ghi dữ liệu, không phát sự kiện. true khi ô đổi.</summary>
     public static bool Till(WorldData world, int x, int y, int z)
     {
         if (!world.Size.InBounds(x, y, z)) return false;
         ushort cell = world.Get(x, y, z);
         var m = Cell.GetMaterial(cell);
         if (m != BlockMaterial.Dirt && m != BlockMaterial.Grass) return false;
         world.Set(x, y, z, Cell.SetModified(Cell.SetMaterial(cell, BlockMaterial.Farmland), true));
         return true;
     }

     /// <summary>Xới qua editor: như Till, ô đổi thì phát CellChanged (mesh, minimap cập nhật).</summary>
     public bool TryTill(int x, int y, int z)
     {
         if (!Till(World, x, y, z)) return false;
         CellChanged?.Invoke(x, y, z);
         return true;
     }
     ```
   - c. Thêm hàm riêng:
     ```csharp
     /// <summary>Ô (x, y, z) vừa có khối hoặc nước: ô ruộng ngay bên dưới trở lại ô đất (0020).</summary>
     void RevertFarmlandBelow(int x, int y, int z)
     {
         if (y - 1 < 0) return;
         ushort below = World.Get(x, y - 1, z);
         if (Cell.GetMaterial(below) != BlockMaterial.Farmland) return;
         World.Set(x, y - 1, z, Cell.SetModified(Cell.SetMaterial(below, BlockMaterial.Dirt), true));
         CellChanged?.Invoke(x, y - 1, z);
     }
     ```
     Gọi hàm này ở hai chỗ, ngay sau `CellChanged?.Invoke(x, y, z);`: trong `TryPlace` (sau `:105`) và trong `FillWater` (sau `:161`).
   - `IsPlaceable` **không** thêm `Farmland`.

   Xong khi: các test WorldEditor ở "Phần cần test" đạt.

3. **`Core/Sim/SimData.cs`:**
   - `StageStatus`: thêm `Cancelled = 4`.
   - `SimEventKind`: thêm `CropCancelled = 4`.
   - `SimEvent`: thêm `public readonly int Seeds;`. Constructor thêm tham số cuối `int seeds = 0`, gán `Seeds = seeds`. Mọi chỗ gọi cũ giữ nguyên. Thêm vào doc comment của struct: "Huỷ vụ: Cell = Soil, Seeds = số hạt trả, Reason = None."
   - `FarmPlot`: thêm
     ```csharp
     /// <summary>Đã xới xong ít nhất một lần → ô là ô ruộng (0020).</summary>
     public bool Tilled;
     /// <summary>Đã rời lệnh sau khi huỷ vụ (0020) — không còn công đoạn, không nhận vụ mới.</summary>
     public bool Removed;
     ```

4. **`Core/Sim/Simulation.cs` — nối editor và ghi ô ruộng:**
   - a. Thêm property `public WorldEditor Editor { get; private set; }` và field `bool writingWorld;`.
   - b. Thêm hàm:
     ```csharp
     /// <summary>Nối world editor (0020): đào/đặt báo cho mô phỏng, ô có sinh vật/nhà/kho không đặt được; xới xong ghi ô ruộng qua editor.</summary>
     public void AttachEditor(WorldEditor editor)
     {
         if (editor == null) throw new ArgumentNullException(nameof(editor));
         if (editor.World != World) throw new ArgumentException("Editor phải sửa cùng WorldData với mô phỏng.");
         if (Editor != null) throw new InvalidOperationException("Mô phỏng đã nối editor.");
         Editor = editor;
         editor.IsOccupied = IsOccupied;
         editor.CellChanged += OnCellChanged;
     }
     ```
   - c. `OnCellChanged` (`:329`) thành `{ if (!writingWorld) terrainDirty = true; }`. Doc comment thêm: "ô do mô phỏng tự xới không tính".
   - d. Thêm hàm:
     ```csharp
     void WriteFarmland(CellPos c)
     {
         if (Editor == null)
         {
             WorldEditor.Till(World, c.X, c.Y, c.Z);
             return;
         }
         writingWorld = true;
         try { Editor.TryTill(c.X, c.Y, c.Z); }
         finally { writingWorld = false; }
     }
     ```
   - e. `OnStageDone`, nhánh `case WorkAbility.Till:` (`:639`): thêm `if (!p.Tilled) { WriteFarmland(p.Soil); p.Tilled = true; }` trước `p.Phase = PlotPhase.Sow;`.

   Xong khi: `TillTurnsSoilIntoFarmlandOnDoneTick` đạt; mọi test `FarmTests` cũ vẫn đạt, không sửa.

5. **`Core/Sim/Simulation.cs` — huỷ vụ:**
   - a. Tách cuối `Unassign` (`:1070-1078`, từ `c.State = CreatureState.Idle;` tới `c.MoveTarget = c.Cell;`) thành `void ReleaseCreature(Creature c)`. `Unassign` gọi lại hàm này; hành vi không đổi.
   - b. Thêm hai hàm:
     ```csharp
     /// <summary>Ô ruộng còn nguyên: ô trên rỗng; chưa xới → đất/cỏ, đã xới → ô ruộng (0020).</summary>
     bool PlotIntact(FarmPlot p)
     {
         if (World.GetMaterial(p.Site.X, p.Site.Y, p.Site.Z) != BlockMaterial.Empty) return false;
         var m = World.GetMaterial(p.Soil.X, p.Soil.Y, p.Soil.Z);
         return p.Tilled ? m == BlockMaterial.Farmland : m == BlockMaterial.Dirt || m == BlockMaterial.Grass;
     }

     /// <summary>Huỷ vụ (0020): trả hạt nếu đã trừ mà chưa thu, sinh vật đang làm về rảnh, ô rời lệnh, xoá hàng đợi. Vận chuyển hàng đã thu giữ nguyên.</summary>
     void CancelPlot(FarmPlot p)
     {
         int seeds = p.Phase == PlotPhase.Water || p.Phase == PlotPhase.Growing || p.Phase == PlotPhase.Harvest ? 1 : 0;
         int stageId = p.StageId, creatureId = -1;
         if (stageId >= 0)
         {
             var s = State.Stages[stageId];
             if (s.Status == StageStatus.Assigned)
             {
                 if (s.Ability == WorkAbility.Sow) seeds = 1;
                 creatureId = s.AssigneeId;
                 ReleaseCreature(State.Creatures[creatureId]);
             }
             s.Status = StageStatus.Cancelled;
             s.Stall = StallReason.None;
             s.AssigneeKind = AssigneeKind.None;
             s.AssigneeId = -1;
         }
         State.Storages[p.StorageId].Seeds += seeds;
         p.Removed = true;
         p.Queue.Clear();
         p.Phase = PlotPhase.Idle;
         p.StageId = -1;
         State.Events.Add(new SimEvent(State.Tick, SimEventKind.CropCancelled, stageId, creatureId, p.Soil, StallReason.None, seeds));
     }
     ```
   - c. Đầu `TerrainPhase` (trước vòng mở lại `NoPath` ở `:444`):
     ```csharp
     var ps = State.Plots;
     for (int i = 0; i < ps.Count; i++)
         if (!ps[i].Removed && !PlotIntact(ps[i])) CancelPlot(ps[i]);
     ```
   - d. Loại ô đã `Removed` ở:
     - `OnPlot` (`:391`): `if (!ps[i].Removed && FramesOverlap(...))`.
     - Kiểm trùng trong `AddFarmOrder` (`:714-715`): `if (!State.Plots[j].Removed && State.Plots[j].Soil == f)`.
     - `EnqueueCrop` (`:731`): ngay sau khi lấy `p`, thêm `if (p.Removed) return false;`. Doc comment thành "false khi hàng đợi đầy hoặc ô đã rời lệnh".
   - e. `Respawn` (`:300`): thêm `|| g.Status == StageStatus.Cancelled` vào điều kiện `continue`.
   - Doc comment đầu class (`:6`): thêm "Ô ruộng + huỷ vụ (bước 6a, 0020)."

   Xong khi: các test huỷ vụ trong `PlotRulesTests` đạt.

6. **`Core/Sim/SimHash.cs`** — sau vòng `Queue` (`:108`) thêm `h = MixInt(h, p.Tilled ? 1 : 0); h = MixInt(h, p.Removed ? 1 : 0);`. `SimEvent` vẫn không vào mã băm.

7. **`Core/Sim/SimDemo.cs:195`** — `KeepFarming` bỏ qua ô đã huỷ: `if (!ps[i].Removed && ps[i].Queue.Count == 0) sim.EnqueueCrop(ps[i].Id);`. Thêm vào doc comment: "ô đã huỷ vụ thì bỏ qua".

8. **`Core/View/SiteStatus.cs`** — đầu `PlotText` (sau dòng `string head`): `if (p.Removed) return head + "đã huỷ";`.

9. **`World/BlockMesher.cs`:**
   - `TopColor`: thêm dòng `BlockMaterial.Farmland => Pack(150, 104, 66, PatternDirt),`.
   - `SideColor`: `m == BlockMaterial.Grass || m == BlockMaterial.Farmland ? TopColor(BlockMaterial.Dirt) : TopColor(m)`. Doc comment: "ô cỏ và ô ruộng lộ vách đất".
   - Không sửa mép cỏ.

10. **`Client/SimView.cs`:**
    - a. Ở cả hai nhánh `Setup` (`:110-111`, `:121-122`), thay hai dòng `view.Editor.IsOccupied = ...; view.Editor.CellChanged += ...;` bằng `Sim.AttachEditor(view.Editor);`.
    - b. Thêm field `readonly List<GameObject> plotSlabs = new List<GameObject>();`. Ở `:145`, lưu kết quả `Box("Ruộng " + p.Id, ...)` vào `plotSlabs`.
    - c. Thêm `UpdatePlotSlabs()`: với `i < ps.Count && i < plotSlabs.Count`, gọi `plotSlabs[i].SetActive(!ps[i].Tilled && !ps[i].Removed)`. Gọi hàm này ngay sau `UpdateSprouts();` ở `:102` và cuối `Setup` (sau `UpdateBodies();` ở `:150`).
    - d. Doc comment class (`:10-17`): thêm dòng "Xới xong → ô ruộng (màu riêng, tấm 'Ruộng' ẩn); đào/đặt lên ô ruộng → huỷ vụ, bảng ghi 'đã huỷ' (sim 6a, 0020)."

11. **Test.** Viết test theo mục "Phần cần test" rồi chạy theo "Lệnh executor chạy".

## Tiêu chí xong
- EditMode: hết đỏ. Số test = 340 + số test mới. PlayMode: hết đỏ. Số test = 68 + 1.
- Mọi test `FarmTests`, `SimDeterminismTests`, `IdleTests`, `MovementTests`, `HomeTests`, `SimDemoTests` cũ đạt **mà không sửa assert cũ**.
- Code trong `Game.Core` không thêm `using UnityEngine`.
- `git status` sạch ngoài các file trong Phạm vi. Đã dọn `EditorSettings.asset`.

## Test sau khi xong

### Phần cần test
Bố trí "Layout6a" dùng trong `Tests/Core/Sim/PlotRulesTests.cs` (namespace `Game.Core.Tests`, class `PlotRulesTests`):
- `w = TestWorlds.Flat(1)`, `sim = new Simulation(TestWorlds.NoWander(), w)`;
- `inv = new MaterialInventory()`, `editor = new WorldEditor(w, inv, 1)`, `sim.AttachEditor(editor)`;
- thêm L1 (8,2,5), L2 (5,2,1), L3 (9,2,5), kho (6,2,9), `AddFarmOrder({(5,1,5)}, kho)`, `TestWorlds.HomeAll(sim)`.

Soil = (5,1,5), Site = (5,2,5). Mốc thời gian như bảng ở "Bối cảnh".
- Đếm `CellChanged` tại soil bằng một handler riêng gắn vào `editor.CellChanged`.
- Muốn đặt khối thì `inv.Add(BlockMaterial.Stone)` trước, rồi `editor.TryPlace(5, 2, 5, BlockMaterial.Stone)`.

**Mới — WorldEditor** (`WorldEditorTests.cs`, dùng `SetUp` sẵn có, world 32×16×32, đá đáy y = 0):
- `DigFarmlandGivesDirt`: đặt (5,1,5) = `Farmland`, `TryDig` → `Ok`, ô rỗng, `inv.Get(Dirt)` = 1, `inv.Get(Farmland)` = 0.
- `FarmlandNotPlaceable`: `IsPlaceable(Farmland)` = false; `TryPlace(5,1,5, Farmland)` → `NotPlaceable`.
- `TillTurnsDirtAndGrassIntoFarmland`:
  - Dirt (5,1,5) → `TryTill` true, vật liệu `Farmland`, `IsModified` true, `changed` có đúng (5,1,5) một lần.
  - Grass (6,1,5) → true.
  - Gọi lại trên ô đã là Farmland → false, không thêm sự kiện.
  - Stone → false. Ngoài map (-1,1,5) → false.
  - `WorldEditor.Till(world, …)` static: đổi vật liệu, không phát sự kiện.
- `PlaceAboveFarmlandRevertsItToDirt`:
  - (5,1,5) = Farmland, `inv.Add(Stone)`, `TryPlace(5,2,5, Stone)` → `Ok`.
  - (5,1,5) = `Dirt`, `IsModified` true.
  - `changed` = [(5,2,5), (5,1,5)] đúng thứ tự.
- `PlaceAboveGrassLeavesItGrass`: (5,1,5) = Grass, đặt đá lên (5,2,5) → (5,1,5) vẫn Grass, `changed` chỉ có (5,2,5).
- `WaterAboveFarmlandRevertsItToDirt`:
  - Editor riêng `new WorldEditor(world, inv, 2, 2)` (seaLevel 2), gắn một list sự kiện riêng.
  - (6,1,5) = Farmland, (6,2,5) = Dirt, (7,2,5) = `Cell.Make(Water, 7)`.
  - `TryDig(6,2,5)` rồi `StepWater()` → (6,2,5) là Water, (6,1,5) là `Dirt`, list có (6,1,5).

**Mới — mô phỏng** (`PlotRulesTests.cs`, bố trí Layout6a):
- `TillTurnsSoilIntoFarmlandOnDoneTick`:
  - Tới nhịp 74: soil là `Dirt`, `Tilled` false.
  - Nhịp 75: soil `Farmland`, `plot.Tilled` true, đếm `CellChanged` tại soil = 1.
  - Mốc 105 / 155 / 1355 / 1459 giống FarmTests: Phase Water ở 105, Growing ở 155 với `RipeTick` 1355, Harvest ở 1355, kho Produce 1 Seeds 10 ở 1459.
- `RetillKeepsFarmlandWithoutEvent`:
  - `EnqueueCrop(0)` trước nhịp 1 (hàng đợi 2).
  - Chạy tới khi vụ 2 qua xới (Phase = `Sow` lần thứ hai, ≤ nhịp 1700).
  - Soil vẫn `Farmland`, đếm `CellChanged` tại soil vẫn = 1.
- `DigCancelsCropInEachPhase` — `[TestCase(tick, seedsBefore, expectedSeedsAfter, releasedCreature)]`. Đào soil tại nhịp `tick` bằng `editor.TryDig(5,1,5)`, rồi `sim.Tick()` một lần.

  | Pha | tick | seedsBefore | sau | releasedCreature |
  |---|---|---|---|---|
  | xới | 40 | 10 | 10 | 0 (L1) |
  | gieo đang làm | 90 | 9 | 10 | 0 |
  | tưới đang làm | 140 | 9 | 10 | 1 (L2) |
  | lớn | 500 | 9 | 10 | -1 |
  | chín, đang thu | 1370 | 9 | 10 | 1 |

  Sau nhịp đó kiểm:
  - `plot.Removed`, `Queue.Count` 0, `Phase` Idle, `StageId` -1;
  - công đoạn cũ của ô `Cancelled`; sinh vật bị cho về (nếu có) `StageId` -1 và không ở trạng thái `Working`;
  - `inv.Get(Dirt)` = 1, soil rỗng;
  - đúng một sự kiện `CropCancelled`, có `Cell` = soil, `Seeds` = sau − trước, `CreatureId` = releasedCreature, `Tick` = tick + 1 (nhịp ngay sau lần đào);
  - `sim.OnPlot(Site,1,1,1)` false, `EnqueueCrop(0)` false;
  - chạy tiếp 300 nhịp: không có công đoạn nào của ô (`PlotId` 0) chuyển sang `Assigned`.
- `HarvestedLoadStillReachesStorageAfterDig`:
  - Đào tại nhịp 1400 (vận chuyển đã giao cho L3, chưa cầm), rồi `Tick()`: `Seeds` vẫn 9, `CropCancelled.Seeds` = 0, `CreatureId` = -1, vận chuyển vẫn `Assigned` cho L3.
  - Chạy tới 1700: vận chuyển `Done`, Produce 1, Seeds 10, `Reserved` 0.
  - Lặp lại với lần đào ở nhịp 1430 (L3 đang cầm hàng): kết quả cuối như trên.
- `CoverCancelsAndRevertsToDirt`:
  - Đặt đá lên Site ở nhịp 500. Ngay sau `TryPlace` (chưa Tick), soil đã là `Dirt`.
  - Sau `Tick()`: Seeds 9 → 10, `Removed`, có `CropCancelled`.
  - `TryDig(5,2,5)`: soil vẫn `Dirt`.
  - `AddFarmOrder({(5,1,5)}, kho)` không ném lỗi, tạo plot id 1.
- `CoverUntilledLeavesMaterial`: đặt đá lên Site ở nhịp 40. Soil vẫn `Dirt`; sau `Tick()`: `Removed`, Seeds 10, `Seeds` của sự kiện = 0.
- `WaterOnSiteCountsAsCover`:
  - Ở nhịp 500, giả lập nước tràn: `w.Set(5,2,5, Cell.Make(Water, 7))`, `w.Set(5,1,5, Cell.Make(Dirt))`, `sim.OnCellChanged(5,2,5)`.
  - Sau `Tick()`: `Removed`, Seeds 10.
  - Phần world của nước đã có test riêng ở `WaterAboveFarmlandRevertsItToDirt`.
- `RefundExceedsFullStorage`: ở nhịp 500 đặt `store.Produce = 41` (9 + 41 = 50, đầy), rồi đào soil, `Tick()` → Seeds 10, `Free` = -1.
- `CancelClearsStallMarker`:
  - Đặt `store.Seeds = 0` trước nhịp 1.
  - Tới nhịp 76: công đoạn gieo `Blocked` / `NoSeeds`, có `StallStarted` cho công đoạn đó.
  - Xoá `State.Events`, đào soil, `Tick()` → trong `State.Events` có `StallCleared` cho công đoạn gieo và `CropCancelled` với `Seeds` = 0.
- `AttachEditorValidates`:
  - Editor của world khác → `ArgumentException`.
  - Gọi `AttachEditor` lần 2 → `InvalidOperationException`.
  - Sau khi nối: `editor.IsOccupied(6,2,9)` true (kho).
- `SameSeedSameHashWithEdits`:
  - Hai bản Layout6a, mỗi bản có thêm ô ruộng (5,1,6): `AddFarmOrder({(5,1,5),(5,1,6)}, kho)` thay cho một ô.
  - Mỗi nhịp gọi `SimDemo.KeepFarming(sim)` trước `Tick()`.
  - Nhịp 300: đào (5,1,5). Nhịp 900: `inv.Add(Stone)` rồi đặt đá lên (5,2,6).
  - Băm `SimHash.Compute` mỗi nhịp qua 2000 nhịp → hai danh sách bằng nhau; cả hai ô `Removed`; có đúng 2 sự kiện `CropCancelled` (đếm trước khi xoá Events, hoặc không xoá).

**Mới — hiển thị:**
- `SiteStatusTests`: `p.Removed = true` → `"Ruộng 2 · đã huỷ"`, ở bất kỳ `Phase` nào.
- `BlockMesherTests.FarmlandTopOwnColorSidesDirtNoLip`:
  - World 16³, một ô (4,8,4) = Farmland.
  - `Build(w,0,0,0, grassLip: false)`: mỗi tam giác có 3 đỉnh cùng màu; tam giác mặt trên (pháp tuyến hình học y > 0,7, như `GrassSidesUseDirtColor`) có màu `TopColor(Farmland)`, còn lại `TopColor(Dirt)`.
  - `TopColor(Farmland)` khác `TopColor(Dirt)` và khác màu lỗi `Pack(255,0,255,0)`.
  - `Build(w,0,0,0)` (có mép cỏ) có `TriangleCount` bằng bản `grassLip: false`, tức là không có mép.
- `SimDemoTests.OneRoundFillsStorage`: thêm assert sau 4000 nhịp: 4 ô `SimDemo.Soil[i]` đều là `Farmland` và `Tilled`.
- `SimViewPlayTests.TilledPlotBecomesFarmlandAndRebuildsChunk` (PlayMode, dựng như test sẵn có):
  - Sau 2 khung: `p = sv.Sim.State.Plots[0]`, `f0 = view.LastRebuildFrame`.
  - Gọi `sv.Sim.Tick()` trong vòng lặp tới khi `p.Tilled` (tối đa 2000 nhịp), rồi `yield return null`.
  - Kiểm: `view.World.GetMaterial(p.Soil.X, p.Soil.Y, p.Soil.Z)` = `Farmland`; `view.LastRebuildFrame > f0`; `sv.Sim.Editor == view.Editor`.

**Regression (không sửa assert cũ):**
- `FarmTests` (mốc thời gian không đổi, ô vẫn là ruộng sau xới), `SimulationTerrainTests` (đào cắt đường), `HomeTests`, `IdleTests` / `MovementTests` (luật 6b, `OnPlot`), `SimDeterminismTests`.
- `SimDemoTests` (0 đình trệ, đủ sản lượng, 0 chung ô), `WorldEditorTests` / `SeaFloodTests` cũ, `BlockMesherTests` cũ, `MinimapEditTests`.
- PlayMode `SimViewPlayTests.SetsUpDemoAndRestoresFrameRate` (`IsOccupied` vẫn nối), `SimViewMapsPlayTests`, `TerrainEditPlayTests`.

### Lệnh executor chạy
1. Unity Editor đang mở project qua MCP. Gọi `refresh_unity`, rồi `read_console` (Error): không có lỗi biên dịch. Kiểm `PlotRulesTests.cs.meta` đã sinh.
2. `run_tests` EditMode (toàn bộ) → ghi dòng tổng kết pass/fail.
3. `run_tests` PlayMode (toàn bộ) → ghi dòng tổng kết. Sau đó chạy `git checkout -- client/ProjectSettings/EditorSettings.asset`.
4. `grep -n "UnityEngine" client/Assets/Game/Core/Sim/*.cs client/Assets/Game/Core/WorldEditor.cs client/Assets/Game/Core/Cell.cs` → không có kết quả.
5. `git status --short` → chỉ có các file trong Phạm vi.

Editor tắt thì dùng `python3 tools/unity_test.py EditMode` / `PlayMode` (cùng bước dọn).

### User tự test
Mở `client/Assets/Game/Client/Scenes/SimView.unity`, bấm Play:
1. Khi L1 xới xong từng ô trong 4 ô ruộng: mặt trên ô đổi sang **nâu sẫm**, tấm mỏng sẫm trên ô biến mất. Sườn ô (nếu thấy) màu đất, **không có mép cỏ** phủ xuống. Ô chưa xới vẫn còn tấm mỏng.
2. Đào một ô ruộng đang lớn (mầm xanh):
   - bảng góc trái ghi "Ruộng N · đã huỷ", số hạt trong kho +1;
   - mầm mất, kho vật liệu (dòng 1–4 ở HUD) đất +1;
   - không sinh vật nào quay lại làm ô đó.
3. Đặt một khối lên ô ruộng khác → ô đó "đã huỷ". Đào khối vừa đặt → ô bên dưới **màu đất sáng** (không còn nâu sẫm).
4. Đào một ô vừa thu hoạch xong khi L3 đang đi lấy hàng → hàng vẫn về kho (nông sản +1).
5. Xác nhận 10 đề xuất của Planner ở mục "Quyết định đã chốt" 8–17. Nhất là màu ô ruộng (150, 104, 66) và luật "nước tràn = đặt khối". Mục nào muốn khác thì ghi ở Review.

## Dừng và chuyển lại Opus khi
- Brief mơ hồ hoặc mâu thuẫn với code thật. Ví dụ: số dòng lệch đến mức không tìm thấy đoạn code; mốc thời gian FarmTests đổi sau bước 4–5.
- Cần một quyết định thuộc AGENTS.md §3.4 hoặc ngoài "Quyết định đã chốt".
- Cùng một lỗi sửa 2 lần vẫn chưa qua.
- Phải sửa file ngoài "Phạm vi", hoặc phải sửa assert của test cũ.
- Cần API Unity không có trong brief. Brief này không dùng API Unity mới: `SetActive`, `LastRebuildFrame`, `GetMaterial` đã có sẵn trong code.

---
<!-- Từ đây trở xuống: EXECUTOR ghi. -->

## Kết quả
- Base commit: a94333e · Chạy bằng: claude-sonnet-5-5 · medium · Bắt đầu: 2026-10-21 17:37 · Kết thúc: 2026-10-21 17:45
- Commit code: 4730284. File đổi: Core/Cell.cs, Core/WorldEditor.cs, Core/Sim/{SimData,Simulation,SimHash,SimDemo}.cs, Core/View/SiteStatus.cs, World/BlockMesher.cs, Client/SimView.cs; test: WorldEditorTests, SiteStatusTests, BlockMesherTests, SimDemoTests, SimViewPlayTests, mới PlotRulesTests.cs(+.meta).
- Không lệch brief. Lần chạy EditMode đầu có 3 test mới của tôi đỏ do lỗi trong test (index stageId = -1 ở pha lớn; kỳ vọng Seeds cuối 9 thay vì 10); đã sửa test, không sửa code.
<!-- Base commit (trước khi làm) · Chạy bằng: <model id> · <effort> (dòng OK của brief_check.py) · commit của brief · file đã đổi · ghi chú lệch so với brief (nếu có, kèm lý do). Giờ bắt đầu / kết thúc. -->

## Kết quả test
- `run_tests` EditMode toàn bộ: 364 test, 361 đạt + 3 đỏ (lỗi test, đã sửa); chạy lại `PlotRulesTests`: 16/16 đạt. (340 + 24 mới = 364.) Chưa chạy lại toàn bộ sau sửa test.
- `run_tests` PlayMode toàn bộ: 69/69 đạt (68 + 1). Đã `git checkout -- client/ProjectSettings/EditorSettings.asset`.
- grep UnityEngine trong Core/Sim, WorldEditor, Cell: không có kết quả.
- `git status --short`: chỉ file trong Phạm vi.
- Phần cần test: WorldEditor (6 test), PlotRules (16 ca gồm Till/Retill/Dig 5 pha/Haul 2/Cover/Untilled/Water/Refund/Stall/Attach/Hash) đạt; SiteStatus `RemovedPlotShowsCancelled`, BlockMesher `FarmlandTopOwnColorSidesDirtNoLip`, SimDemo `OneRoundFillsStorage` (+assert Farmland), PlayMode `TilledPlotBecomesFarmlandAndRebuildsChunk` đạt. Regression cũ đạt không sửa assert. User tự test (màu, tấm Ruộng, HUD): chờ user.
<!-- Mỗi dòng ở "Phần cần test" → đạt / không đạt / chờ user + bằng chứng (tên test, dòng output).
Mỗi lệnh ở "Lệnh executor chạy": `<lệnh>` → <kết quả thật, dòng tổng kết>. -->

## Vướng mắc
<!-- Khi trạng thái = cần Opus: câu hỏi cụ thể + các phương án (kèm đề xuất). -->

---
<!-- PLANNER ghi khi review. -->

## Review
<!-- Kết luận: đã duyệt | cần sửa (thêm bước sửa vào "Các bước", trạng thái về chờ làm). Điểm đã đối chiếu. -->
