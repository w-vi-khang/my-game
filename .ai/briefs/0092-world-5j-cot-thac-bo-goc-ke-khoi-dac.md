# Brief 0092 — World 5j (lỗi 13): ô nước rơi bo góc cả khi kề khối đặc

> Trạng thái: chờ làm
> Review: đầy đủ + user xem (gộp cuối 5j)
> Model thực thi: Opus · medium
> Cập nhật: 2026-11-01 19:54

## Mục tiêu
Lỗi 13 của task [5j](../tasks/m1-world-5j-sua-loi-choi-thu.md). User chơi thử ngày 2026-11-01 và thấy: chỗ cột thác đi qua khối đất nhô, tiết diện cột đổi từ góc vuông sang góc bo, nhìn thành bậc. Đổi luật bo góc của **ô nước rơi** cho tiết diện cột đồng đều suốt chiều cao. Luật mới: ô rơi bo góc cả khi phía bên là khối đặc, chỉ không bo khi phía bên là nước. User chọn 2026-11-01.

## Bối cảnh cần đọc
**Planner dump world đang chạy (2026-11-01, MCP `execute_code`)** [run]:
- Nguồn thác (140,25,200). Rãnh x 137..139, mức 5. Mép tràn (136,25,200) chảy, mức 4.
- Cột rơi (136, 24..21, 200) đều mức 5. Ao nguồn ở y = 20.
- (137,24,200) là đất (đáy khối nhô). (137, 21..23, 200) rỗng.
- Hệ quả: ô (136,24,200) có `open.x = false`, nên hai góc +x **vuông**. Các ô y 21..23 có hai góc +x **bo**.
- Mức nước không liên quan: mesher không dùng mức của ô rơi.
- Ảnh user (ngoài repo): `/private/tmp/claude-501/-Users-macvn-Documents-User-khang-Game-my-game/23b77818-7a2c-4302-bcb4-01ad28309ad0/images/1.webp`, `images/2.png`.

**Code** — `client/Assets/Game/World/BlockMesher.cs`:
- `void AddWaterCell(int3 c, int3 g, bool airAbove)` (`:932`). Được gọi ở `:454` với `airAbove = above == Empty`.
  - Ô cột ⇔ `IsFallingWater(g) || IsFallingWater(g - new int3(0, 1, 0))`.
  - Dựng `WaterCol`:
    - `open = new bool4(MaterialAt(g + (1,0,0)) == e, MaterialAt(g − (1,0,0)) == e, MaterialAt(g + (0,0,1)) == e, MaterialAt(g − (0,0,1)) == e)` (e = Empty);
    - `lip = airAbove`.
- `bool IsLipCell(int3 p)` (`:1013`).
- `bool ColumnCornerRounded(int3 p, int sx, int sz)` (`:1018`, brief 0090). Hiện là: ô cột, và hai ô ngang của góc đều `Empty`.
- `struct WaterCol` (`:1023`). Có `public bool4 open;` (mặt hở +x, −x, +z, −z) và `public bool Rounded(int sx, int sz) => open[sx == 1 ? 0 : 1] && open[sz == 1 ? 2 : 3];` (`:1039`).
- `void AddWaterColumn(int3 g, in WaterCol col)` (`:1080`):
  - `Rounded` quyết định đoạn rút ngắn mặt bên (`r0`/`r1`), trụ bo đứng, cầu góc (chỉ khi `lip`), `LipTaper`, và nắp vá 0090 (`if (col.Rounded(sx, sz)) continue;` + `ColumnCornerRounded` ô trên/dưới).
  - Mặt bên chỉ vẽ ở cạnh `open[d]`. Giữ nguyên: không vẽ mặt áp vào khối đặc.
  - Mặt trên mép tràn dùng `open` (`u0 = col.open.y ? R : 0`…) — không đổi.
- `R` = `WaterFilletRadius` 0,3; `S` = `WaterFilletSegments` 3.

**Test** — `client/Assets/Game/Tests/World/BlockMesherTests.cs`:
- Helper:
  - `HasWaterVertexNear(ChunkMesh mesh, float3 pos, float3 normal)` (`:1007`);
  - `AssertNoDuplicateWaterVertices(ChunkMesh mesh)` (`:1018`);
  - `CountWaterTris(ChunkMesh mesh, Func<float3,bool> pred)` (`:1085`) — đếm tam giác có **cả 3 đỉnh** thoả `pred`.
- `ColumnCornerStepCapped` (`:1140`) dựng đúng hình lỗi này: ô rơi (5,6,3) kề đất (6,6,3), ô rơi (5,5,3) bên dưới hở. Test đang khẳng định có nắp ở y = 6. Với luật mới, (5,6,3) bo góc +x nên nắp ở y = 6 **mất**. Test này phải đổi kỳ vọng (bước 3), vì luật cũ của nó bị thay theo quyết định user.

## Quyết định đã chốt (không được đổi)
1. **Ô thân cột** = ô nước rơi (`IsFallingWater(g)`) có ô trên **không** `Empty`. Ô mép tràn (`lip`) và ô không rơi nằm trên ô rơi giữ luật cũ.
2. **Phía d bo được** (`round[d]`):
   - ô thân cột: ô kề phía d **không phải nước** (`Empty` hoặc khối đặc);
   - ô cột khác: `round[d] = open[d]` (như cũ).
3. **Góc (sx, sz) bo** ⇔ cả hai phía của góc bo được **và** ít nhất một phía là `Empty`. Góc kẹp giữa hai khối đặc không vẽ trụ bo, vì không nhìn thấy.
4. Mặt bên vẫn chỉ vẽ ở phía `open[d]` (Empty). Phía khối đặc không có mặt. Khe giữa hai trụ bo trên mặt phẳng áp đất được để hở, vì đất che. User xem.
5. `ColumnCornerRounded(p, sx, sz)` (nắp 0090) tính theo cùng luật QĐ 1–3 cho ô `p`, để nắp chỉ còn ở chỗ thật sự đổi vuông ↔ bo. Ví dụ: mép tràn vuông nằm trên ô thân cột bo.
6. Không đổi: mức nước, `WorldEditor`, shader, mặt ao, bo lõm chân thác (`FootCorner`/`FootFillet` vẫn xét `Empty`), mặt trên và đoạn thon mép tràn.

## Phạm vi
- Được sửa/tạo: `client/Assets/Game/World/BlockMesher.cs`, `client/Assets/Game/Tests/World/BlockMesherTests.cs`, mục Kết quả của brief này.
- Không được sửa: file khác.

## Các bước
1. `BlockMesher.cs`:
   - Thêm vào `WaterCol` trường `public bool4 round;` (comment: `Phía bo được (brief 0092): +x, −x, +z, −z.`).
   - Đổi `Rounded`:
     ```csharp
     public bool Rounded(int sx, int sz)
     {
         int a = sx == 1 ? 0 : 1, b = sz == 1 ? 2 : 3;
         return round[a] && round[b] && (open[a] || open[b]);
     }
     ```
   - Thêm hàm cục bộ cạnh `IsLipCell`, áp QĐ 1–2:
     ```csharp
     bool4 ColumnRound(int3 p, bool4 open)
     ```
     - Thân cột: `IsFallingWater(p) && MaterialAt(p + new int3(0, 1, 0)) != (int)BlockMaterial.Empty` → mỗi phía `MaterialAt(p + dir) != (int)BlockMaterial.Water`, thứ tự +x, −x, +z, −z.
     - Ngược lại → trả `open`.
   - Trong `AddWaterCell`, sau khi dựng `col`, gán `col.round = ColumnRound(g, col.open);`.
   - Viết lại `ColumnCornerRounded(p, sx, sz)`:
     - `p` là ô cột (điều kiện cũ);
     - tính `open` của `p` như `AddWaterCell` và `round = ColumnRound(p, open)`;
     - trả `round[a] && round[b] && (open[a] || open[b])` (như `Rounded`).
   - Xong khi: biên dịch, `read_console` Error 0.
2. `BlockMesherTests.cs` — thêm `[Test] FallingColumnRoundsBesideSolid`:
   - World, editor và các khẳng định trạng thái dựng y hệt `ColumnCornerStepCapped` (`:1140–1168`). Chép lại, không gọi chung.
   - `var mesh = BlockMesher.Build(w, 0, 0, 0, true, 5);`
   - Khẳng định:
     - `HasWaterVertexNear(mesh, new float3(6, 7, 3.3f), new float3(1, 0, 0))` và `HasWaterVertexNear(mesh, new float3(6, 7, 3.7f), new float3(1, 0, 0))`: đỉnh trên của trụ bo góc +x của ô (5,6,3);
     - `HasWaterVertexNear(mesh, new float3(5.7f, 7, 3), new float3(0, 0, -1))`: mặt −z của (5,6,3) dừng ở x = 5,7 (đầu cung).
   - Chạy trên code cũ trước bước 1 → phải **fail**, ghi vào Kết quả. Code cũ mà đạt → dừng, cần Opus.
   - Xong khi: đạt sau bước 1.
3. `BlockMesherTests.cs` — đổi `ColumnCornerStepCapped` theo luật mới (QĐ 5):
   - Giữ phần dựng world và khẳng định trạng thái.
   - Thay 4 assert mesh bằng:
     - `CountWaterTris(mesh, p => math.abs(p.y - 6) < 1e-4f && p.x > 5.7f - 1e-4f && p.x < 6 + 1e-4f && p.z > 3 - 1e-4f && p.z < 3.3f + 1e-4f)` == 0: không còn nắp ở y = 6;
     - cùng vùng x, z nhưng `math.abs(p.y - 7) < 1e-4f` → ≥ 3: nắp ở đáy mép tràn (5,7,3). Ô này có +x là nước nên góc +x vuông, nằm trên ô (5,6,3) nay bo;
     - `HasWaterVertexNear(mesh, new float3(6, 7, 3), new float3(0, -1, 0))`.
   - Thêm comment một dòng trên test: `// Brief 0092: ô thân cột bo cả khi kề đất → nắp chuyển lên đáy mép tràn.`
   - Nắp ở y = 7 không có (đếm = 0) → **dừng, cần Opus**. Ghi số đếm và danh sách đỉnh nước trong vùng x 5,7..6, z 3..3,3, y 6..8.
4. Chạy test theo "Lệnh executor chạy". Test cũ khác ngoài `ColumnCornerStepCapped` phải đổi kỳ vọng → **dừng, cần Opus** (ghi tên test, assertion, giá trị mong/đã thấy).

## Tiêu chí xong
- `FallingColumnRoundsBesideSolid` fail trên code cũ, đạt sau khi sửa.
- `ColumnCornerStepCapped` (kỳ vọng mới) đạt.
- Mọi test khác trong `BlockMesherTests` đạt mà không đổi kỳ vọng; EditMode toàn bộ đạt; PlayMode `WaterFlowPlayTests` đạt; `read_console` Error 0.

## Test sau khi xong

### Phần cần test
- Ô thân cột bo góc kề khối đặc (bước 1, QĐ 1–3) · `FallingColumnRoundsBesideSolid` · fail cũ / đạt mới.
- Nắp 0090 theo luật mới (bước 1 `ColumnCornerRounded`, QĐ 5) · `ColumnCornerStepCapped` sau bước 3 · đạt.
- Mép tràn, đoạn thon, cầu góc không đổi (QĐ 1, 6) · test cũ `LipTapersIntoUpstreamCell`, `LipTapersAtChannelSpill` và các test mép tràn khác trong `BlockMesherTests` · đạt, không đổi kỳ vọng.
- Bo lõm chân thác không đổi (QĐ 6) · các test chân thác cũ trong `BlockMesherTests` · đạt.
- Không đỉnh trùng, mặt đúng hướng · `AssertNoDuplicateWaterVertices`, `TrianglesFaceTheirNormals` (test cũ) · đạt.
- Mức nước và mô phỏng không đổi · EditMode toàn bộ · đạt.
- Thác trong Play không lỗi · PlayMode `WaterFlowPlayTests` · đạt.

### Lệnh executor chạy
1. Trước bước 1: MCP `run_tests` EditMode, lọc `FallingColumnRoundsBesideSolid` (sau bước 2, trên code cũ) → ghi fail.
2. Sau bước 3: `run_tests` EditMode lớp `BlockMesherTests` → ghi số đạt/tổng.
3. `run_tests` EditMode toàn bộ → số đạt/tổng.
4. `run_tests` PlayMode `WaterFlowPlayTests` → số đạt/tổng; rồi dọn `git checkout -- client/ProjectSettings/EditorSettings.asset`.
5. `read_console` Error → 0.
6. `git status` → chỉ file trong Phạm vi.

### User tự test
Gộp cuối 5j. SimView, Play:
- Đứng gần chân thác demo, nhìn chỗ cột đi ngang đáy khối đất nhô (như ảnh lỗi 13): tiết diện cột không còn bậc vuông ↔ tròn.
- Sát chỗ cột áp đất: không có khe hở lạ hay mảng sáng.
- Chỗ mép tràn nối cột: không có gai mới.

## Dừng và chuyển lại Opus khi
- Brief mơ hồ hoặc mâu thuẫn với code thật.
- Cần một quyết định thuộc AGENTS.md §3.4 hoặc ngoài "Quyết định đã chốt".
- Cùng một lỗi sửa 2 lần vẫn chưa qua.
- Phải sửa file ngoài "Phạm vi".

---

## Kết quả

## Kết quả test

## Vướng mắc

---

## Review
