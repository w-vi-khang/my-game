# Brief 0090 — World 5j (lỗi 9): vá chỗ cột thác đổi từ góc vuông sang góc bo

> Trạng thái: chờ làm
> Review: đầy đủ + user xem (gộp cuối 5j)
> Model thực thi: Opus · medium
> Cập nhật: 2026-10-31 00:20

## Mục tiêu
Lỗi 9 của task [5j](../tasks/m1-world-5j-sua-loi-choi-thu.md): gai nước vẫn còn chỗ mép tràn nối cột thác (ảnh `~/Documents/User/khang/Game/art-test/shots/user5j_lip_spike_again.webp`, ngoài repo). Gai nằm ở đáy khối đất nhô, sát cột. Sửa bằng một "nắp" phẳng vá chỗ một ô cột có góc vuông nằm ngay trên (hoặc dưới) một ô cột có góc bo.

## Bối cảnh cần đọc
**Planner chẩn đoán (2026-10-31, MCP `execute_code`, không commit)** [run]:
- World 16³ dựng lại hình thác demo:
  - đá y 0..2; ao nước nguồn y = 2, x 2..5, z 2..5, viền cát x/z 1..6;
  - khối đất x 7..10, y 3..7, z 2..4;
  - nguồn (9,7,3); rỗng (8,7,3), (7,7,3), (6,7,3); đất (6,6,3), (6,7,2), (6,7,4);
  - `WorldEditor(w, bag, 1, -1)`, `WakeWater(9,7,3)`, `SettleWater(20000)`.
- Sau khi lắng: mép tràn (5,7,3) mức 5; cột rơi (5,6..3,3); ô (6,7,3) chảy.
- Mesh nước:
  - ô cột (5,5,3), y 5..6: 4 góc đều bo, vì mọi mặt ngang đều hở. Mặt z = 3 chạy x 5,3..5,7; trụ bo từ (5,7; 3) qua (5,96; 3,15) tới (6; 3,3).
  - ô cột (5,6,3), y 6..7: phía +x là đất (6,6,3), nên `col.open.x` = false. Hai góc +x **vuông**: mặt z = 3 chạy x 5,3..**6,0**, mặt z = 4 cũng vậy, không có trụ bo +x.
  - Ở y = 6, phần góc vuông của ô trên (vùng giữa góc (6, 3) và cung bo của ô dưới) **không có mặt đáy**. Vùng này hở, nên nhìn thấy mảnh tam giác cong = "gai".
- Lỗi 7 ("gờ mỏng ở đầu cột, ngay dưới mép tràn") nhiều khả năng cũng là chỗ chuyển này [chưa xác minh]. Brief này không làm gì riêng cho lỗi 7.

**Code** — `client/Assets/Game/World/BlockMesher.cs`:
- `void AddWaterCell(int3 c, int3 g, bool airAbove)` (`:936`):
  - ô cột ⇔ `IsFallingWater(g) || IsFallingWater(g - new int3(0, 1, 0))`;
  - `open = new bool4(MaterialAt(g + (1,0,0)) == Empty, MaterialAt(g − (1,0,0)) == Empty, MaterialAt(g + (0,0,1)) == Empty, MaterialAt(g − (0,0,1)) == Empty)`.
- `struct WaterCol` (`:1021`): `public bool Rounded(int sx, int sz) => open[sx == 1 ? 0 : 1] && open[sz == 1 ? 2 : 3];`, `Pos(u, y, v)`, `foot`, `fc`.
- `float3 CircleDir(int sx, int sz, float phi)` (`:1043`).
- `void AddWaterColumn(int3 g, in WaterCol col)` (`:1079`):
  - vòng trụ bo `:1192–1225`: tâm `ax = (sx == 1 ? 1 − R : R, sz == 1 ? 1 − R : R)`, điểm cung `ax + R·CircleDir(sx, sz, j·90°/S)`, j = 0..S;
  - đáy trụ `yb = c.y + (fc == FootFull ? R : 0)`; đỉnh `yt`.
- `int AddWaterVertex(float3 p, float3 n)`, `void AddWaterTri(int a, int b, int c)`: tự quay mặt trước theo tổng pháp tuyến, bỏ tam giác trùng chỉ số.
- `R` = `WaterFilletRadius` (0,3), `S` = `WaterFilletSegments` (3) (`:51–52`).

**Test** — `client/Assets/Game/Tests/World/BlockMesherTests.cs`:
- helper `CountWaterTris(mesh, pred)` (`:1076`, đếm tam giác có cả 3 đỉnh thoả `pred`), `HasWaterVertexNear(mesh, pos, normal)` (`:941`);
- mẫu dựng world + `WorldEditor` + `SettleWater` + `BlockMesher.Build(w, 0, 0, 0, true, 5)`: `LipTapersAtChannelSpill` (`:1086`).

## Quyết định đã chốt (không được đổi)
1. Hình cột giữ nguyên: góc cạnh khối đặc vẫn vuông, góc hở vẫn bo. Chỉ **thêm nắp**, không đổi `Rounded`, `open`, trụ bo, mặt bên, mép tràn, bo lõm chân thác.
2. **Nắp đáy:** ô cột `g` có góc `(sx, sz)` **không** bo, và ô `g − (0,1,0)` là ô cột có góc đó **bo** → vẽ quạt phẳng ở `y = c.y`, pháp tuyến (0, −1, 0). Đỉnh quạt `K = Pos(sx, c.y, sz)`. Tam giác `(K, a_j, a_{j+1})`, với `a_j = Pos(ax.x + R·d_j.x, c.y, ax.y + R·d_j.z)`, `d_j = CircleDir(sx, sz, j·90°/S)`, j = 0..S−1. `ax` tính như trụ bo.
3. **Nắp đỉnh** (trường hợp ngược): ô cột `g` có góc `(sx, sz)` không bo, và ô `g + (0,1,0)` là ô cột có góc đó bo → quạt như trên ở `y = c.y + 1`, pháp tuyến (0, 1, 0).
4. "Ô cột có góc bo" của ô kề `p` tính lại như `AddWaterCell`: `MaterialAt(p) == Water && (IsFallingWater(p) || IsFallingWater(p − (0,1,0)))`, và hai mặt ngang của góc đó ở `p` là `Empty`. Thêm hàm cục bộ:
   ```csharp
   bool ColumnCornerRounded(int3 p, int sx, int sz)
   ```
5. Không đổi shader, ô không phải ô cột, ao.

## Phạm vi
- Được sửa/tạo: `client/Assets/Game/World/BlockMesher.cs`, `client/Assets/Game/Tests/World/BlockMesherTests.cs`, mục Kết quả của brief này.
- Không được sửa: file khác.

## Các bước
1. `BlockMesher.cs` — thêm `ColumnCornerRounded` (QĐ 4), đặt cạnh `IsLipCell`.
   - Trong `AddWaterColumn`, sau vòng trụ bo (trước `if (col.foot)`): với mỗi góc `(sx, sz)` mà `!col.Rounded(sx, sz)`, vẽ nắp đáy (QĐ 2) nếu `ColumnCornerRounded(g − (0,1,0), sx, sz)`, vẽ nắp đỉnh (QĐ 3) nếu `ColumnCornerRounded(g + (0,1,0), sx, sz)`.
   - Thêm comment một dòng: `// Nắp vá chỗ góc vuông nối góc bo của ô cột trên/dưới (brief 0090, lỗi 9).`
   - Xong khi: biên dịch.
2. `BlockMesherTests.cs` — thêm `[Test] ColumnCornerStepCapped`:
   - Dựng world như mục Bối cảnh (hai vòng `for` cho đá + ao/cát, khối đất, các ô đặt tay), `new WorldEditor(w, new PlayerBag(), 1, -1)`, `WakeWater(9, 7, 3)`, `SettleWater(20000)`, `Assert.IsFalse(e.HasPendingWater)`.
   - Khẳng định trạng thái: `(5,6,3)` và `(5,5,3)` là nước rơi (`Cell.IsFalling`); `(6,6,3)` là Dirt.
   - `var mesh = BlockMesher.Build(w, 0, 0, 0, true, 5);`
   - Khẳng định:
     - `CountWaterTris(mesh, p => math.abs(p.y - 6) < 1e-4f && p.x > 5.7f - 1e-4f && p.x < 6 + 1e-4f && p.z > 3 - 1e-4f && p.z < 3.3f + 1e-4f)` ≥ 3 — có nắp ở góc (+x, −z);
     - tương tự ở góc (+x, +z) với `p.z` trong [3,7; 4];
     - `HasWaterVertexNear(mesh, new float3(6, 6, 3), new float3(0, -1, 0))` và `HasWaterVertexNear(mesh, new float3(6, 6, 4), new float3(0, -1, 0))`.
   - Chạy test trên code cũ trước bước 1 → phải **fail** (đếm = 0). Ghi vào Kết quả. Code cũ mà test đạt → dừng, cần Opus.
   - Xong khi: test đạt sau bước 1.
3. Chạy test theo "Lệnh executor chạy". Test cũ nào phải đổi kỳ vọng → **dừng, cần Opus** (ghi tên test, assertion, giá trị mong/đã thấy).

## Tiêu chí xong
- `ColumnCornerStepCapped` fail trên code cũ, đạt sau khi sửa.
- Mọi test cũ `BlockMesherTests` đạt không đổi kỳ vọng; EditMode toàn bộ đạt; PlayMode `WaterFlowPlayTests` đạt; `read_console` Error 0.

## Test sau khi xong

### Phần cần test
- Nắp đáy ở góc vuông trên góc bo (bước 1, QĐ 2) · `ColumnCornerStepCapped` · fail cũ / đạt mới.
- Nắp đỉnh (bước 1, QĐ 3) · chỉ có code. Trong world của `ColumnCornerStepCapped` không có trường hợp này; đủ khi test toàn bộ không lỗi và mesh không sinh tam giác trùng (`AssertNoDuplicateWaterVertices` trong các test cũ) · đạt.
- Mép tràn, đoạn thon, trụ bo, cầu góc, bo lõm chân thác (regression) · `BlockMesherTests` toàn bộ (gồm `LipTapersAtChannelSpill`, các test 0081 chân thác) · đạt không đổi.
- Thác demo trong Play (regression) · PlayMode `WaterFlowPlayTests` · đạt.
- Toàn bộ EditMode (regression) · đạt.

### Lệnh executor chạy
1. Qua MCP Unity (Editor đang mở; nếu đang Play Mode → `manage_editor` stop): `run_tests` EditMode lọc `Game.World.Tests.BlockMesherTests` → số đạt/fail (trước bước 1: chỉ `ColumnCornerStepCapped` fail).
2. `run_tests` EditMode toàn bộ → dòng tổng kết.
3. `run_tests` PlayMode lọc `WaterFlowPlayTests` → dòng tổng kết.
4. `read_console` Error → 0.
5. Dọn: `git checkout -- client/ProjectSettings/EditorSettings.asset`; `git status` chỉ còn file trong Phạm vi.
(Editor tắt thì dùng `python3 tools/unity_test.py EditMode` / `PlayMode`.)

### User tự test
Gộp cuối 5j: đứng gần chân thác demo, nhìn lên chỗ đáy khối đất nhô sát cột thác (như ảnh `user5j_lip_spike_again.webp`) → không còn gai nước. Nhìn nghiêng ngay dưới mép tràn → không còn gờ mỏng (lỗi 7).

## Dừng và chuyển lại Opus khi
- Brief mơ hồ hoặc mâu thuẫn với code thật (vd mesh ở bước 2 không giống mô tả chẩn đoán).
- Cần một quyết định thuộc AGENTS.md §3.4 hoặc ngoài "Quyết định đã chốt".
- Test cũ nào phải đổi kỳ vọng.
- Cùng một lỗi sửa 2 lần vẫn chưa qua.
- Phải sửa file ngoài "Phạm vi".

---

## Kết quả

## Kết quả test

## Vướng mắc

---

## Review
