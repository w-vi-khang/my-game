# Brief 0098 — World 5k phần 4a: hướng chảy chính (main_flow) + hướng chảy ghi theo ô (FlowDirs)

> Trạng thái: chờ review
> Review: đầy đủ
> Model thực thi: Sonnet · medium
> Cập nhật: 2026-11-02

## Mục tiêu
Phần 4 của task [5k](../tasks/m1-world-5k-nuoc-kieu-anh-tham-khao.md) (vệt chảy) tách làm 2 brief. Brief này lo **dữ liệu** trong `Game.Core`. Brief 0099 lo hiển thị (mesher + shader).
1. **main_flow** (luật do user đưa, 2026-11-02):
   - Ô nước chảy B có cha A. Hướng AB = B − A.
   - Nếu B có con C với BC = AB thì main_flow(B) = C. Không có con như vậy thì null.
   - Ô rơi: cha ở trên, con ở dưới, nên main_flow chỉ xuống dưới.
   - Nguồn (ao, biển) không có cha nên null.
   - Sau này (M2) vật nổi trôi theo main_flow; null thì trôi ngẫu nhiên sang một ô con.
   - Brief này chỉ thêm hàm đọc + test.
2. **FlowDirs**: mảng 1 byte/ô trong `WorldData`, ghi hướng từ ô cha tới ô cho ô chảy ngang có cha là ô kề cùng lớp.
   - Brief 0099 dùng mảng này vẽ vệt chảy theo hướng cha→nó (user chọn 2026-11-02).
   - WorldEditor cập nhật mảng khi tạo nút, đổi cha, xoá nút. Khi hướng đổi do đổi cha, phát `WaterSurfaceChanged` để client dựng lại mesh.

## Bối cảnh cần đọc
**Planner đã chốt thiết kế (2026-11-02), lý do:**
- main_flow **tính khi hỏi** từ cây cha–con, không lưu, nên luôn khớp cây.
- Mesher (Burst job) không đọc được cây trong `Dictionary` của WorldEditor. Vì vậy cần một mảng phẳng.
  - Bit ô còn trống chỉ có bit 15, không đủ cho 5 trạng thái.
  - Nên dùng `byte[]` song song với `Cells`, tạo lười: map 384×48×384 tốn khoảng 7 MB, và chỉ cấp khi có nước chảy.
- Mã hướng = chỉ số trong `D4` + 1: 1 = +z, 2 = +x, 3 = −z, 4 = −x; 0 = không có. "Hướng" là B − A (từ cha tới ô).
  - Cha ở lớp khác (ô rơi, ô chân cột) hoặc cha không kề (ô tái tạo lấy `parent_id` của ô bên cạnh, 0043 mục 5) → 0.
- Chỉ `CreateChild` tạo ô nước chảy (`MakeFlowing` chỉ gọi ở `WorldEditor.cs:621`). Cha chỉ đổi trong `Reparent`. Nút chỉ bị gỡ trong `RemoveNode`. Vậy 3 chỗ ghi là đủ.
  - Giá trị cũ ở ô không còn là nước chảy được để nguyên. Mesher (0099) chỉ đọc FlowDirs ở ô có cờ chảy và không rơi.

**Code** (tại commit `468cc20`):
- `client/Assets/Game/Core/WorldData.cs`:
  - `public sealed class WorldData` `:55`;
  - `public ushort[] Cells => cells;` `:75`, `public void Set(...)` `:79`;
  - `WorldSize.CellCount` `:21`, `WorldSize.Index(int x, int y, int z) => x + X * (z + Z * y)` `:31`.
- `client/Assets/Game/Core/WorldEditor.cs` (đã có `using System;`):
  - `struct FlowNode { public int X, Y, Z, Parent; public bool Source; }` `:98–102`, `const int NoParent = -1;` `:104`;
  - bảng `nodeAt`, `nodes`, `children` `:107–109`;
  - `event Action<int, int, int> WaterSurfaceChanged` `:139`;
  - `static readonly (int dx, int dz)[] D4 = { (0, 1), (1, 0), (0, -1), (-1, 0) };` `:373`;
  - `public bool TryGetFlowNode(int x, int y, int z, out int id, out int parentId)` `:379–386`;
  - `void RemoveNode(int id, bool toCascade)` `:456–475` (dòng `nodes.Remove(id);` `:460`);
  - `void Reparent(int id, int parent)` `:485–494` (dòng `nodes[id] = n;` `:491`);
  - `int CreateChild(int parent, int x, int y, int z, int level, bool falling)` `:618–637` (dòng `nodeAt[(x, y, z)] = id;` `:624`, `CellChanged?.Invoke` `:630`).
- Test `client/Assets/Game/Tests/Core/WaterFlowTests.cs`:
  - helper: `Flat` `:10`, `Editor` `:23`, `Settle` `:30`, `Dig` `:36`, `Place` `:39`, `Corridor()` `:54–66`, `OpenFloor` `:68`, `RandomEdits` `:408–424`, `RandomWorld` `:426–433`, `Node` `:513`, `ParentOf` `:519`;
  - mẫu: `ChannelFlowsFullLength` `:287–298`, `FallingCellCreatesChildBelowOnly` `:714–735`, `WallBoostOnPlaceAndPropagate` `:678–711`.
- Không dùng API Unity nào (Game.Core là C# thuần).

## Quyết định đã chốt (không được đổi)
1. **`WorldData`**, thêm sau `Cells` (`:75`):
   ```csharp
   /// <summary>Hướng chảy theo ô (brief 0098): hướng từ ô cha kề cùng lớp tới ô nước chảy ngang — 0 không có, 1 = +z, 2 = +x, 3 = −z, 4 = −x.
   /// Null cho tới khi WorldEditor ghi lần đầu; chỉ có nghĩa ở ô nước đang chảy, không rơi (ô khác có thể còn giá trị cũ). Không lưu, không băm.</summary>
   public byte[] FlowDirs { get; private set; }

   public byte[] EnsureFlowDirs() => FlowDirs ??= new byte[Size.CellCount];

   public byte FlowDirAt(int x, int y, int z) => FlowDirs == null ? (byte)0 : FlowDirs[Size.Index(x, y, z)];
   ```
2. **`WorldEditor`**, các hàm riêng mới đặt ngay sau `Reparent`:
   ```csharp
   /// <summary>Mã hướng cha → nút (FlowDirs, brief 0098): cha kề ngang cùng lớp → chỉ số D4 + 1; còn lại 0.</summary>
   byte FlowDirCode(int x, int y, int z, int parent)
   {
       if (parent == NoParent || !nodes.TryGetValue(parent, out var p) || p.Y != y)
           return 0;
       for (int i = 0; i < D4.Length; i++)
           if (x - p.X == D4[i].dx && z - p.Z == D4[i].dz)
               return (byte)(i + 1);
       return 0;
   }

   void SetFlowDir(int x, int y, int z, byte code)
   {
       if (code == 0 && World.FlowDirs == null)
           return;
       World.EnsureFlowDirs()[World.Size.Index(x, y, z)] = code;
   }
   ```
   - `CreateChild`: ngay sau `nodeAt[(x, y, z)] = id;` (`:624`), trước `CellChanged`, thêm `SetFlowDir(x, y, z, FlowDirCode(x, y, z, parent));`.
   - `Reparent`: ngay sau `nodes[id] = n;` (`:491`) thêm:
     ```csharp
     byte code = FlowDirCode(n.X, n.Y, n.Z, parent);
     if (World.FlowDirAt(n.X, n.Y, n.Z) != code)
     {
         SetFlowDir(n.X, n.Y, n.Z, code);
         WaterSurfaceChanged?.Invoke(n.X, n.Y, n.Z);
     }
     ```
   - `RemoveNode`: ngay sau `nodes.Remove(id);` (`:460`) thêm `SetFlowDir(node.X, node.Y, node.Z, 0);`.
3. **API công khai**, đặt ngay sau `TryGetFlowNode` (`:386`):
   ```csharp
   /// <summary>Hướng chảy chính (main_flow, user 2026-11-02): ô con C của ô nước chảy B nằm tiếp theo hướng cha → B (C − B = B − cha; cha kề B theo 1 trong 6 hướng).
   /// Không có (không phải nút, nguồn, cha không kề, không có con đúng hướng) → false. Vật nổi (M2) ưu tiên trôi theo hướng này.</summary>
   public bool TryGetMainFlow(int x, int y, int z, out int cx, out int cy, out int cz)
   {
       cx = cy = cz = 0;
       if (!nodeAt.TryGetValue((x, y, z), out int id))
           return false;
       var n = nodes[id];
       if (n.Parent == NoParent || !nodes.TryGetValue(n.Parent, out var p))
           return false;
       int dx = n.X - p.X, dy = n.Y - p.Y, dz = n.Z - p.Z;
       if (Math.Abs(dx) + Math.Abs(dy) + Math.Abs(dz) != 1)
           return false;
       if (!nodeAt.TryGetValue((x + dx, y + dy, z + dz), out int c) || nodes[c].Parent != id)
           return false;
       cx = x + dx; cy = y + dy; cz = z + dz;
       return true;
   }
   ```
4. Không đổi luật mô phỏng (lan, rơi, tăng mức, xoá, tái tạo), thứ tự xử lý, mã băm world.

## Phạm vi
- Được sửa:
  - `client/Assets/Game/Core/WorldData.cs`, `client/Assets/Game/Core/WorldEditor.cs`;
  - `client/Assets/Game/Tests/Core/WaterFlowTests.cs` (chỉ thêm test + helper mới).
- Không được sửa: mọi file khác. Không sửa `BlockMesher`, shader, client, JSON (việc của 0099), test cũ.

## Các bước
1. **Test trước** trong `WaterFlowTests.cs`, gồm 2 helper mới và 5 test:
   - Helper:
     - `static void AssertMain(WorldEditor e, int x, int y, int z, int ex, int ey, int ez)`: `Assert.IsTrue(e.TryGetMainFlow(...))` và toạ độ khớp; message có toạ độ.
     - `static void AssertNoMain(WorldEditor e, int x, int y, int z)`: `Assert.IsFalse(...)`.
   - `MainFlowStraightChannel`:
     - `Corridor()`, `Dig(e, 1, 6, 8)`, `Settle`;
     - x = 1..14: `AssertMain(e, x, 6, 8, x + 1, 6, 8)`;
     - `AssertNoMain(e, 15, 6, 8)` (ô cuối), `AssertNoMain(e, 0, 6, 8)` (nguồn);
     - x = 1..15: `w.FlowDirAt(x, 6, 8) == 2`;
     - `w.FlowDirAt(0, 6, 8) == 0`.
   - `MainFlowBendIsNull` (kênh hình chữ L):
     - `Flat(5)`; lớp y = 6 toàn `Stone` ở mọi (x, z);
     - nguồn `(0, 6, 8) = Cell.Make(BlockMaterial.Water, Cell.MaxWaterLevel)`;
     - các ô kênh `Cell.Make(BlockMaterial.Empty)`: (2..5, 6, 8) và (5, 6, 9..13); giữ `(1, 6, 8)` là đá;
     - `Dig(e, 1, 6, 8)`, `Settle`.
     - Kỳ vọng:
       - `AssertMain(e, 3, 6, 8, 4, 6, 8)`;
       - `AssertNoMain(e, 5, 6, 8)` và `FlowDirAt(5, 6, 8) == 2` (góc rẽ: không có main_flow nhưng vẫn có hướng từ cha);
       - `AssertMain(e, 5, 6, 9, 5, 6, 10)` và `FlowDirAt(5, 6, 9) == 1`;
       - `AssertNoMain(e, 5, 6, 13)` và `FlowDirAt(5, 6, 13) == 1`.
   - `MainFlowFallingColumn`: dựng world giống hệt `FallingCellCreatesChildBelowOnly` (`:716–722`), `Dig(e, 1, 6, 8)`, `Settle`. Kỳ vọng:
     - `AssertNoMain(e, 1, 6, 9)` (mép tràn: con ở dưới, khác hướng cha) và `FlowDirAt(1, 6, 9) == 1`;
     - `AssertMain(e, 1, 5, 9, 1, 4, 9)` (ô rơi chảy xuống) và `FlowDirAt(1, 5, 9) == 0`;
     - `AssertNoMain(e, 1, 4, 9)`;
     - `AssertNoMain(e, 0, 6, 8)`.
   - `FlowDirUpdatesOnReparent`: chép nguyên văn phần dựng của `WallBoostOnPlaceAndPropagate` (`:680–689`: world, `BasinFillRadius = 4`, `Dig(e, 1, 6, 8)`, `Settle`).
     - Kiểm `FlowDirAt(3, 6, 9) == 2` (cha (2, 6, 9));
     - đăng ký `WaterSurfaceChanged` vào `HashSet`; `Place(e, 2, 6, 9)`;
     - kiểm `FlowDirAt(3, 6, 9) == 1` (cha (3, 6, 8)) và `ParentOf(e, 3, 6, 9) == Node(e, 3, 6, 8)`.
   - `FlowDirsAndMainFlowMatchTree` (kiểm bất biến):
     - Chạy hai world: (a) `OpenFloor()` + `BasinFillRadius = 4` + `Dig(e, 1, 6, 8)` + `Settle`; (b) `RandomWorld()` + `RandomEdits(w, e)` (không cần settle).
     - Với mỗi world: quét mọi ô, gom `id → (x, y, z)` của các ô có `TryGetFlowNode` (kèm `parentId`).
     - Với mọi ô có nút mà `Cell.IsFlowing(w.Get(...))` và `parentId` có trong bảng:
       - **FlowDirs:** kỳ vọng = (cha cùng y, |dx| + |dz| = 1 → chỉ số trong `{(0,1),(1,0),(0,-1),(-1,0)}` + 1; còn lại 0). Ô `Cell.IsFalling` cũng theo luật này (cha ở trên → 0). Kiểm `w.FlowDirAt(...)` bằng kỳ vọng.
       - **main_flow:** kỳ vọng true ⇔ cha kề theo 6 hướng (|dx| + |dy| + |dz| = 1) và ô B + (B − cha) có nút với `parentId` = id của B; nếu true thì toạ độ = B + (B − cha).
     - Đếm số ô đã kiểm: (a) > 20, (b) > 0.
   - Xong khi: test được viết. Code cũ không biên dịch được vì thiếu `TryGetMainFlow`/`FlowDirAt` — đó là kết quả fail mong đợi; ghi output lỗi biên dịch (console).
2. **`WorldData.cs`** theo QĐ 1.
3. **`WorldEditor.cs`** theo QĐ 2–3.
   - Xong khi: `refresh_unity` → `read_console` Error = 0; `run_tests` EditMode lọc `WaterFlowTests` đạt (5 test mới + toàn bộ test cũ).
4. **Kiểm tổng** theo "Lệnh executor chạy".

## Tiêu chí xong
- 5 test mới đạt. Toàn bộ `WaterFlowTests` cũ đạt, không sửa test cũ.
- EditMode toàn bộ đạt (gồm `GoldenHashTests`, `IncrementalRebuildTests`, `WorldDataTests`).
- PlayMode `WaterFlowPlayTests` đạt.
- Console 0 lỗi. `git status` chỉ có file trong Phạm vi.

## Test sau khi xong

### Phần cần test
- main_flow (mới) · `WaterFlowTests.MainFlowStraightChannel`, `MainFlowBendIsNull`, `MainFlowFallingColumn` · kênh thẳng có main_flow tới ô kế, góc rẽ / ô cuối / mép tràn / nguồn → null, ô rơi → ô dưới.
- FlowDirs khi tạo nút (mới) · 3 test trên · mã 2 / 1 / 0 đúng như kỳ vọng.
- FlowDirs khi đổi cha + phát sự kiện (mới) · `FlowDirUpdatesOnReparent` · 2 → 1.
- Bất biến với cây (mới) · `FlowDirsAndMainFlowMatchTree` · khớp ở mọi nút chảy, kể cả sau 300 lần đào/đặt ngẫu nhiên.
- Luật nước cũ (regression) · toàn bộ `WaterFlowTests` cũ + `GoldenHashTests` + `IncrementalRebuildTests` · đạt.
- Nước trong Play (regression) · PlayMode `WaterFlowPlayTests` · đạt.

### Lệnh executor chạy
1. Sau bước 1: `refresh_unity` → `read_console` (Error): ghi lỗi biên dịch mong đợi (thiếu `TryGetMainFlow`/`FlowDirAt`).
2. Sau bước 2–3: `refresh_unity` → `read_console` (Error) = 0 → `run_tests` EditMode lọc `WaterFlowTests` → đạt.
3. `run_tests` EditMode toàn bộ → đạt.
4. `run_tests` PlayMode lọc `WaterFlowPlayTests` → đạt; rồi `git checkout -- client/ProjectSettings/EditorSettings.asset` (và `ProjectSettings.asset` nếu `runInBackground` đổi). Xoá `InitTestScene*` nếu có.
5. `git status` / `git diff --stat` → chỉ file trong Phạm vi.

### User tự test
Không có. Brief này chưa đổi gì nhìn thấy được; hình ảnh vệt chảy ở brief 0099.

## Dừng và chuyển lại Opus khi
- Brief mơ hồ hoặc mâu thuẫn với code thật (số dòng lệch nhiều, chữ ký khác).
- Một kỳ vọng ở bước 1 sai mà code đúng QĐ 1–3 (cây cha–con thật khác kỳ vọng của Planner): ghi giá trị thật + `ParentOf` của các ô liên quan, không tự đổi kỳ vọng.
- Test cũ fail.
- Cần sửa file ngoài Phạm vi, hoặc quyết định ngoài "Quyết định đã chốt".
- Cùng một lỗi sửa 2 lần vẫn chưa qua.

---
<!-- Từ đây trở xuống: EXECUTOR ghi. -->

## Kết quả
Base commit: c377095
Chạy bằng: claude-sonnet-5-5 · medium
Commit: 5e47f57
File đổi: WorldData.cs, WorldEditor.cs, WaterFlowTests.cs.

## Kết quả test
- Bước 1 (test trước): biên dịch fail như mong đợi — `CS1061: 'WorldEditor' does not contain a definition for 'TryGetMainFlow'`, `'WorldData' does not contain a definition for 'FlowDirAt'`.
- main_flow / FlowDirs khi tạo nút / đổi cha + sự kiện / bất biến với cây: đạt — `WaterFlowTests` 40/40 (5 test mới + cũ), không cần đổi kỳ vọng nào.
- EditMode toàn bộ: 709/709 đạt (gồm GoldenHashTests, IncrementalRebuildTests, WorldDataTests). Console Error = 0.
- PlayMode `WaterFlowPlayTests`: 4/4 đạt.
- Dọn: checkout EditorSettings.asset; không có InitTestScene*; git status chỉ file trong Phạm vi.
- User tự test: không có.

## Vướng mắc

---
<!-- PLANNER ghi khi review. -->

## Review
