# Brief 0081 — World 5i (phần 2): bo lõm chân thác, xén mặt ao quanh chân, vá góc chân cột

> Trạng thái: chờ làm
> Review: đầy đủ + user xem (gộp 0079, 0080, 0081)
> Model thực thi: Opus · medium
> Cập nhật: 2026-10-28 10:17

## Mục tiêu
Làm phần còn lại của [0043](../../design/decisions/0043-nuoc-cay-cha-con-tang-muc-boi-tuong.md) mục 6 trong `BlockMesher`: ở chân thác (cột nước rơi chạm mặt ao), góc giữa mặt đổ xuống và mặt ao được bo lõm theo cung tròn. Mặt ao của các ô quanh chân thác được xén để không chồng lên phần bo. Ngoài ra vá lỗ ở góc chân cột: sau 0080, góc bo đứng của ô rơi thấp nhất để hở một mảnh trên ô ao, không có mặt nước. User chọn "làm đủ" (2026-10-28).

## Bối cảnh cần đọc
- Brief trước: `.ai/briefs/0080-world-5i-mesh-nuoc-lien-bo-mep.md`, mục "Quyết định đã chốt" (ký hiệu `R`, `S`, `Hc`, ô cột, trụ, mép tràn).
- `client/Assets/Game/World/BlockMesher.cs` (1274 dòng, commit `9014a97`):
  - `:413` `public int3 Origin;` — toạ độ đỉnh trong chunk = toạ độ world − `Origin` (`c = g − Origin`).
  - `:931` `void AddWaterCell(int3 c, int3 g, bool airAbove)`: ô không phải ô cột → `AddWaterSurface` (nếu `airAbove`) + `AddWaterSides`; ô cột → dựng `WaterCol` rồi `AddWaterColumn(g, col)`.
  - `:954` `bool IsFallingWater(int3 p)`, `:961` `bool IsLipCell(int3 p)`.
  - `:965` `struct WaterCol` (`c`, `h` = `CornerHeight` theo `sx + 2·sz`, `open` = mặt hở theo thứ tự +x, −x, +z, −z, `lip`, `rv`; `Rounded(sx, sz)`, `Hc`, `H(u, v)` song tuyến, `Pos(u, y, v)`).
  - `:981` `float3 CircleDir(int sx, int sz, float phi)`, `:985` `static float3 DirVec(int d)`.
  - `:1000` `void AddWaterColumn(int3 g, in WaterCol col)`: mặt trên mép tràn, rồi vòng `d` 0..3 (mặt bên: đáy `b0`/`b1` ở `c.y` tại `:1035-1036`, cung, nắp), rồi `:1079` vòng góc: trụ (`:1087` `float yt = …`; đáy trụ ở `c.y` `:1091-1092`) và cầu góc.
  - `:1121` `void AddWaterSurface(int3 c, int3 g)` (một tứ giác, 4 góc theo `CornerHeight`), `:1129` `AddWaterSides`.
  - `:1153` `ushort CellAt(int3 p)` (x/z ngoài map kẹp về biên), `:1165` `float WaterHeight(int3 p)`, `:1178` `float CornerHeight(int3 g, int dx, int dz)` (ô có nước phía trên → góc = 1), `:1248` `int MaterialAt(int3 p)`.
  - `AddWaterVertex(float3 p, float3 n)` (hàn theo vị trí + pháp tuyến ×1024), `AddWaterTri(int a, int b, int c)` (bỏ tam giác trùng chỉ số; hướng theo tổng pháp tuyến 3 đỉnh), `AddWaterQuad`.
- Test: `client/Assets/Game/Tests/World/BlockMesherTests.cs` (1121 dòng): `StoneFloor16()` `:921`, `HasWaterVertexNear` `:941`, `AssertNoDuplicateWaterVertices` `:952`, `Water()` `:835`; test 0080 ở `:976`, `:1019`, `:1055`.
- Không dùng API Unity mới: chỉ `Unity.Mathematics` (`math.*`) và các container đã có trong file.

## Quyết định đã chốt (không được đổi)
Ký hiệu: `R` = `WaterFilletRadius`, `S` = `WaterFilletSegments` (mặc định 0,3 và 3). θ_k = k·90°/S, φ_j = j·90°/S. Bốn hướng ngang `d` ∈ {+x, −x, +z, −z} (thứ tự chỉ số như `DirVec`). `up` = (0, 1, 0). Mọi toạ độ dưới đây là toạ độ world, chỉ trừ `Origin` khi tạo đỉnh.

1. **Định nghĩa ô:**
   - `IsPondSurface(p)`: `p` trong map (0 ≤ x < SizeX, 0 ≤ z < SizeZ, 0 ≤ y < SizeY), `MaterialAt(p)` = Water, `MaterialAt(p + up)` = Empty, `!IsFallingWater(p)`, `!IsFallingWater(p − up)`.
   - `IsFootBase(b)`: `MaterialAt(b)` = Water, `!IsFallingWater(b)`, `!IsFallingWater(b − up)`, `IsFallingWater(b + up)`. Ô chân thác là `F = b + up`; `F` là ô cột (0080 QĐ 4).
   - `FootFillet(b, d)`: `IsFootBase(b)` **và** `MaterialAt(b + up + d)` = Empty **và** `IsPondSurface(b + d)`. Đây là "cạnh bo lõm" `d` của chân `F`.
   - Góc (sx, sz) của chân `F` (`dA` = ±x theo sx, `dB` = ±z theo sz, như 0080): **bo** khi `MaterialAt(F + dA)` và `MaterialAt(F + dB)` đều là Empty (giống `Rounded` của 0080). Góc bo là **góc đủ** khi thêm `FootFillet(b, dA)`, `FootFillet(b, dB)` và `IsPondSurface(b + dA + dB)`. Góc bo không đủ là **góc lửng**.
   - `PondY(x, z, yb)` = `yb` + nội suy song tuyến 4 giá trị `CornerHeight` của ô `(floor x, yb, floor z)` tại (x − floor x, z − floor z). Ô đó không phải nước → trả `yb + 1`. (Ô chân `b` có nước phía trên nên mọi góc = 1 → `PondY` = `yb + 1` = đáy ô `F`.)
2. **Hình bo:** điểm bo lõm ở bước k cách mặt cột `s_k = R·(1 − cos θ_k)` theo phương ngang `ŝ` (hướng ra xa cột). Điểm ở độ cao `PondY(điểm) + R·(1 − sin θ_k)`, pháp tuyến `cos θ_k·ŝ + sin θ_k·up`. k = 0 là điểm trên mặt cột, cao `F.y + R`, pháp tuyến `ŝ`. k = S là điểm trên mặt ao, pháp tuyến `up`.
3. **Ô chân `F`, thay đổi trong `AddWaterColumn`:** khi `IsFootBase(g − up)`:
   - Mặt bên hở `d` có `FootFillet(b, d)`: đáy mặt bên ở `c.y + R` thay cho `c.y`. Khoảng t (`t0`, `t1`) giữ như 0080.
   - Trụ ở góc đủ: đáy trụ ở `c.y + R`. Ở góc lửng: đáy trụ ở `c.y` như cũ.
   - **Vá góc lửng:** ở mỗi góc lửng, vẽ quạt phẳng ở độ cao `c.y`, pháp tuyến `up`, từ góc ô `(sx, sz)` tới các điểm đáy trụ φ_j (j = 0..S): tam giác (góc ô, j, j+1).
   - Mặt bên hở không có `FootFillet`, mặt trên/cung/cầu của mép tràn: không đổi.
4. **Dải bo** trên mỗi cạnh `d` có `FootFillet(b, d)` (ô ao `N = b + d`; trục t chạy dọc cạnh, t ∈ [0, 1] trong ô). Hai đầu dải xác định theo góc ở đầu đó:
   - Góc **đủ** → đầu dải ở t = R (hoặc 1 − R); xuyến (QĐ 5) nối tiếp.
   - Góc **lửng** → đầu dải ở t = R (hoặc 1 − R), thêm **nắp** (QĐ 4c).
   - Góc **không bo**. Gọi `e` = hướng dọc t đi ra phía đầu đó:
     - a. `FootFillet(b + e, d)` → đầu dải ở mặt phẳng ô (t = 0 hoặc 1), không nắp (dải của chân bên cạnh nối tiếp).
     - b. Ngược lại, nếu `FootFillet(b + d + e, −e)` (góc trong, chân chữ L) → **đầu vát**: điểm k của profile đầu nằm ở t = t_đầu + σ·s_k, với σ hướng vào trong cạnh. Hai dải vát gặp nhau trên đường chéo, cùng vị trí; pháp tuyến khác nhau thì là đỉnh riêng.
     - c. Ngược lại → đầu dải ở mặt phẳng ô, thêm **nắp**.
   - Profile (k = 0..S) đặt tại hai đầu và tại mọi giá trị t ∈ {R, 1 − R} nằm giữa hai đầu. Profile thẳng tại t: điểm k = điểm cạnh tại t + `ŝ`·s_k (QĐ 2). Hai profile liền nhau nối bằng S tứ giác (k, k+1). Đầu vát nối với profile thẳng kế tiếp như thường; ở k = S hai điểm trùng nên tam giác suy biến bị bỏ.
   - c (nắp). Nắp nằm trong mặt phẳng t = t_đầu: quạt từ `Qc` = điểm cạnh tại t_đầu, cao `c.y`, tới các điểm profile k = 0..S. Tam giác (Qc, k, k+1), pháp tuyến nắp = `e` (hướng ra phía đầu dải).
5. **Xuyến ở góc đủ:** trục `A` = (sx ? 1 − R : R, sz ? 1 − R : R) trong ô `F` (giống trụ 0080). Điểm (i, j), i, j = 0..S: xz = `A + R·(2 − cos θ_i)·CircleDir(sx, sz, φ_j)`, y = `PondY(xz) + R·(1 − sin θ_i)`, pháp tuyến `cos θ_i·CircleDir + sin θ_i·up`. Các tứ giác (i, j), (i+1, j), (i+1, j+1), (i, j+1). Vòng i = 0 trùng đáy trụ (`c.y + R`). Profile j = 0 và j = S trùng profile đầu dải t = R / 1 − R của hai cạnh.
6. **Ô vuông góc `Q` của góc đủ** = hình vuông cạnh 2R tâm tại góc lưới `X` của góc đó. Trong toạ độ `(a, b)` từ `A` theo `dA`, `dB`, `Q` = [0, 2R]². Phần ngoài xuyến (|(a, b)| ≥ 2R) do chân `F` vẽ, là mặt ao phẳng theo `PondY`, pháp tuyến `up`. Các đỉnh: `Qf` = (2R, 2R), `M1` = (2R, R), `M2` = (R, 2R), vòng ngoài xuyến `φ_j` = (2R·cos φ_j, 2R·sin φ_j) (chính các đỉnh i = S của QĐ 5). Tam giác:
   - S ≥ 2: (M1, φ_0, φ_1), (Qf, M1, φ_1); (Qf, φ_j, φ_{j+1}) với j = 1..S−2; (Qf, φ_{S−1}, M2), (M2, φ_{S−1}, φ_S).
   - S = 1: (M1, φ_0, φ_1), (M1, φ_1, M2), (M1, M2, Qf).
7. **Xén mặt ao:** ô `P` có `IsPondSurface(P)` chia lưới phụ 3×3 bởi các đường u, v ∈ {R, 1 − R} (u, v ∈ [0, 1] trong ô). Ô phụ giữa luôn vẽ. Ô phụ cạnh `E_e` (cạnh hướng `e`) **bỏ** khi `FootFillet(P + e, −e)`. Ô phụ góc `K_X` (góc lưới `X`, nằm giữa hai cạnh `e1`, `e2` của `P`) **bỏ** khi có ít nhất một điều sau:
   - `FootFillet(P + e1, −e1)` và góc của chân đó ở `X` không phải góc lửng (tức là góc đủ, hoặc góc không bo);
   - như trên với `e2`;
   - `IsFootBase(P + e1 + e2)` và góc của chân đó ở `X` là góc đủ.
   - **Không bỏ ô phụ nào** → vẽ như cũ (`AddWaterSurface`, không đổi). **Có ô phụ bị bỏ** (gọi là "ô ao bị xén") → vẽ phần còn lại:
     - a. Gộp: trong mỗi cột u (3 ô phụ dọc v), gộp các ô phụ liền nhau còn lại thành đoạn v liên tục. Sau đó gộp các cột liền nhau có cùng danh sách đoạn v.
     - b. Mỗi hình chữ nhật sau khi gộp: đỉnh biên = mọi điểm của lưới 4×4 (u, v ∈ {0, R, 1 − R, 1}) nằm trên biên hình chữ nhật, theo vòng. Riêng điểm nằm trên cạnh ô `P` mà không phải góc hình chữ nhật thì chỉ lấy khi ô bên kia cạnh đó (cùng y) là ô ao bị xén hoặc `IsFootBase`. Thêm một đỉnh ở tâm hình chữ nhật và vẽ quạt (tâm, p_i, p_{i+1}). Mọi đỉnh cao `PondY`, pháp tuyến `up`.
   - Để nhanh: chỉ tính các luật trên khi ít nhất một trong 8 ô quanh `P` (cùng y) có `IsFallingWater(ô + up)`. Ngược lại vẽ như cũ.
8. **Chunk:** chân `F` vẽ dải, xuyến, `Q` và nắp vào chunk của `F`, kể cả phần nằm trong ô thuộc chunk bên cạnh. Mặt ao bị xén do chunk của ô ao vẽ. Đỉnh được phép nằm ngoài [0, 16] của chunk.
9. **Chấp nhận:** chỗ nối chữ T trên cạnh giữa ô ao bị xén (hoặc nắp) và ô ao không bị xén (chỉ sai số float, nằm ở biên vùng chân thác). Không xử lý thêm.
10. Không đổi: địa hình, mép cỏ, collider, `WaterHeight`, `CornerHeight`, ô cột không phải chân, mép tràn, ô nước không phải ao bị xén, `water_m1.json`, tham số `R`/`S`, bộ sinh world, `WorldEditor`.

## Phạm vi
- Được sửa: `client/Assets/Game/World/BlockMesher.cs` (phần nước của `MeshJob`: `AddWaterCell`, `AddWaterColumn`, `WaterCol`, `AddWaterSurface` / hàm mới cho ao bị xén, hàm trợ giúp mới); `client/Assets/Game/Tests/World/BlockMesherTests.cs` (thêm test, helper mới); brief này (từ Kết quả trở xuống).
- Không được sửa: mọi file khác (`WaterConfig`, JSON, `TerrainChunkView`, `WorldEditor`, `Cell`, scene, `design/*.md`, test PlayMode, test 0080).

## Các bước
1. Hàm trợ giúp theo QĐ 1: `IsPondSurface`, `IsFootBase`, `FootFillet`, loại góc (đủ / lửng / không bo), `PondY`. Xong khi biên dịch được.
2. Ô chân (QĐ 3): đáy mặt bên trên cạnh bo lõm, đáy trụ ở góc đủ, vá góc lửng. Thêm các trường cần thiết vào `WaterCol` và gán trong `AddWaterCell`.
3. Dải bo + nắp + đầu vát (QĐ 4), xuyến (QĐ 5), `Q` (QĐ 6). Viết trong `AddWaterColumn` hoặc trong hàm riêng mà nó gọi.
4. Xén mặt ao (QĐ 7) trong nhánh không phải ô cột của `AddWaterCell`.
5. Test mới (bảng "Phần cần test") và helper:
   - `static int CountUpHits(ChunkMesh m, float x, float z, float yMin, float yMax)`: đếm tam giác nước có hình chiếu xz diện tích > 1e-9, chứa (x, z) theo toạ độ trọng tâm với sai số 1e-6, có y giao trong [yMin, yMax] và (tổng pháp tuyến 3 đỉnh).y > 0.
   - `static void AssertWatertight(ChunkMesh m, float3 lo, float3 hi)`: với mọi cạnh tam giác nước có cả hai đầu trong hộp [lo, hi] (sai số 1e-4), khoá vị trí là `round(p·10000)`. Mỗi cạnh vô hướng phải xuất hiện đúng 2 lần, một lần mỗi chiều. Lỗi in toạ độ cạnh.
6. Chạy "Lệnh executor chạy".

## Tiêu chí xong
- `refresh_unity` → console 0 Error.
- Mọi dòng "Phần cần test" đạt. EditMode + PlayMode toàn bộ đạt (ghi dòng tổng).

## Test sau khi xong

### Phần cần test
Mọi test dùng `StoneFloor16()` (đá ở y = 4) và `BlockMesher.Build(w, 0, 0, 0, true, 5)` (R 0,3, S 3). "Ao" = `Water()` ở y = 5, x, z ∈ [5, 11], trừ các ô ghi riêng. "Cột tại (x, z)" = (x, y, z) với y = 6..8 là `Cell.MakeFlowing(0, 5, true)`, (x, 9, z) là `Water()`. Sai số vị trí và pháp tuyến 1e-3. Giá trị đã tính sẵn: cos 30° = 0.8660, s_1 = 0.040192, s_2 = 0.15.

**mới (`BlockMesherTests`):**
- `FootFilletFullPond` (QĐ 1–7, mọi góc đủ) · ao + cột tại (8, 8) ·
  - đỉnh tường +x: `(9, 6.3, 8.3)` và `(9, 6.3, 8.7)` pháp tuyến (1, 0, 0). Không đỉnh nước nào có x = 9, z ∈ [8.3, 8.7], y ∈ [6, 6.29];
  - dải +x tại z = 8.3: k = 1 `(9.0402, 6.1460, 8.3)` pháp tuyến (0.8660, 0.5, 0); k = 2 `(9.15, 6.0252, 8.3)` pháp tuyến (0.5, 0.8660, 0); k = 3 `(9.3, 5.97, 8.3)` pháp tuyến (0, 1, 0);
  - đáy trụ góc (1,1) φ = 30°: `(8.9598, 6.3, 8.85)` pháp tuyến (0.8660, 0, 0.5);
  - xuyến góc (1,1): (i 1, j 1) `(8.9946, 6.15, 8.8701)` pháp tuyến (0.75, 0.5, 0.4330); (i 2, j 1) `(9.0897, 6.0312, 8.925)` pháp tuyến (0.4330, 0.8660, 0.25); (i 3, j 1) `(9.2196, 5.9780, 9.0)` pháp tuyến (0, 1, 0);
  - `Q` góc (1,1): `Qf (9.3, 5.949, 9.3)`, `M1 (9.3, 5.97, 9.0)`, `M2 (9.0, 5.97, 9.3)`, đều pháp tuyến (0, 1, 0);
  - ô ao bị xén (9,5,8): tâm hình chữ nhật `(9.65, 5.935, 8.5)` pháp tuyến (0, 1, 0);
  - phủ kín: điểm mẫu x = 7.0131 + 0.05·i, z = 7.0077 + 0.05·j (i, j = 0..59). `d` = khoảng cách có dấu tới chân cột (hình vuông [8, 9]² bo 4 góc bán kính 0,3; âm là bên trong): `q = abs((x, z) − (8.5, 8.5)) − 0.2`, `d = length(max(q, 0)) + min(max(q.x, q.y), 0) − 0.3`. Bỏ qua điểm có |d| < 0.003. Kỳ vọng `CountUpHits(m, x, z, 5.5, 6.5)` = 1 khi d > 0 và = 0 khi d < 0;
  - `AssertWatertight(m, (7, 5.5, 7), (10, 6.5, 10))`;
  - `AssertNoDuplicateWaterVertices`.
- `FootMixedCornerAndCaps` (góc lửng, vá góc, nắp ở góc không bo, đầu dải lửng) · ao, trong đó (7,5,8) và (9,5,9) là Stone; thêm Stone ở (7, y, 8) với y = 6..9 và ở (9,6,9); cột tại (8, 8) ·
  - vá góc lửng (1,1): đỉnh `(9, 6, 9)` pháp tuyến (0, 1, 0); đáy trụ góc (1,1) φ = 30°: `(8.9598, 6, 8.85)` pháp tuyến (0.8660, 0, 0.5);
  - góc đủ (1,0): đáy trụ φ = 30°: `(8.9598, 6.3, 8.15)` pháp tuyến (0.8660, 0, −0.5);
  - nắp đầu lửng của dải +x (t = 8.7): `(9, 6, 8.7)` và `(9.0402, 6.1460, 8.7)`, cả hai pháp tuyến (0, 0, 1);
  - nắp đầu không bo của dải +z (x = 8): `(8, 6, 9)` pháp tuyến (−1, 0, 0);
  - ô ao bị xén (9,5,8) giữ ô phụ góc ở (9, 9): tâm `(9.15, 5.985, 8.85)` pháp tuyến (0, 1, 0);
  - phủ kín: lưới mẫu như trên trong [7, 10]². Bỏ điểm trong ô (7..8, 8..9) và (9..10, 9..10) (đá), và điểm cách biên các ô đó hoặc biên chân cột < 0.003. Chân cột ở đây = [8, 9]², trừ phần ngoài trụ ở góc (1,0) và (1,1): trong ô góc [8.7, 9]×[8, 8.3] hoặc [8.7, 9]×[8.7, 9], điểm cách trục (8.7, 8.3) / (8.7, 8.7) > 0.3 là ngoài chân. Kỳ vọng 1 hit ngoài chân, 0 hit trong chân (y ∈ [5.5, 6.5]);
  - `AssertNoDuplicateWaterVertices`.
- `FootInnerCornerMiter` (đầu vát, QĐ 4b) · ao + cột tại (8, 8), (8, 9), (9, 8) ·
  - có hai đỉnh cùng vị trí `(9.0402, 6.1498, 9.0402)`: một pháp tuyến (0.8660, 0.5, 0), một pháp tuyến (0, 0.5, 0.8660);
  - phủ kín trong ô phụ góc của (9,5,9): x = 9.0131 + 0.05·i, z = 9.0077 + 0.05·j (i, j = 0..5; không rơi đúng đường vát): `CountUpHits(…, 5.5, 6.5)` = 1;
  - `AssertNoDuplicateWaterVertices`.

**cũ — giữ nguyên, phải đạt:** toàn bộ `BlockMesherTests`, gồm `FallingColumnIsRoundedAndContinuous`, `LipCapsMeetUpstreamCell` (cột rơi xuống đá → không phải chân, không đổi), `WaterVerticesAreWelded`, `FallingWaterColumnHasSideFaces`, `WaterSurfaceOnlyWhereAirIsAbove`, `FlowingWaterSurfaceFollowsLevel`. Thêm `ChunkSeamTests`, `GoldenHashTests`, `WaterFlowTests`, `SeaFloodTests`, `WaterConfigTests`; PlayMode `WaterFlowPlayTests` (`WaterfallColumnIsMeshed`, `WaterfallPoursIntoPond`), `TerrainEditPlayTests`, `DayCycleWaterPlayTests`, `FlyCameraWaterPlayTests`, `OceanBackdropTests`.

### Lệnh executor chạy
1. MCP `refresh_unity` → `read_console` (Error) = 0.
2. MCP `run_tests` EditMode lọc `BlockMesherTests`, `ChunkSeamTests`, `GoldenHashTests`, `WaterFlowTests`, `SeaFloodTests`.
   - Nếu test mesher ra giá trị cũ ngay sau biên dịch (Burst chưa nạp bản mới, như brief 0079): thêm rồi gỡ một dòng chú thích trong `BlockMesher.cs`, `refresh_unity`, chạy lại. Vẫn cũ → xoá `client/Library/BurstCache/JIT` rồi `refresh_unity`.
3. MCP `run_tests` EditMode toàn bộ; PlayMode toàn bộ. Ghi dòng tổng. Nếu MCP rớt khi đang chạy: `sleep 60` rồi `get_test_job`, lặp lại. Không kết thúc lượt khi test còn chạy.
4. Dọn: thoát Play; `git checkout -- client/ProjectSettings/EditorSettings.asset`; xoá `client/Assets/InitTestScene*.unity(.meta)` nếu có. `git status --short` chỉ còn file trong Phạm vi.
5. Chụp **1** ảnh Game view (SimView, Play), nhìn chân thác demo chếch từ trên. Lưu ngoài repo, ghi đường dẫn vào Kết quả.

Ghi giờ bắt đầu/kết thúc bằng lệnh `date`.

### User tự test
SimView, Play (gộp 0079 + 0080 + 0081):
1. Thác demo: cột nước liền, 4 cạnh đứng bo tròn, mép tràn bo lồi. Chân thác bo lõm êm vào mặt ao, không có khe, lỗ, nhấp nháy hay hai lớp nước chồng nhau. Nhìn kỹ cả 4 góc chân cột.
2. Đào rãnh ra vách: mép tràn bo lồi. Nhìn từ trên không thấy lỗ ở chỗ nối với ô phía trước. Thác mới đổ xuống ao cũng có bo lõm ở chân.
3. Đào bờ cát ao: nước lan tối đa 5 ô, mỏng dần.
4. Đào rãnh 1 ô dài: nước chảy hết rãnh; ra chỗ rộng thì lan 5 ô.
5. Chặn giữa rãnh: đoạn sau rút từng ô.
6. Thác demo đổ vào ao không tràn.
7. Đào hố rộng 2 lớp cạnh ao: nước đầy cả hai lớp.

## Dừng và chuyển lại Opus khi
- Brief mơ hồ hoặc mâu thuẫn với code thật (số dòng lệch nhiều, chữ ký khác).
- Một giá trị trong "Phần cần test" ra khác mô tả trong khi code đúng QĐ 1–9 (Planner có thể tính sai): ghi giá trị thật, không tự đổi kỳ vọng.
- `AssertWatertight` hoặc phủ kín không đạt sau 2 lần sửa: ghi toạ độ cạnh/điểm lỗi.
- Test ở mục "cũ — giữ nguyên" fail; mã băm `GoldenHashTests` đổi.
- Cần API Unity mới; cần quyết định ngoài "Quyết định đã chốt"; phải sửa file ngoài "Phạm vi".

---
<!-- Từ đây trở xuống: EXECUTOR ghi. -->

## Kết quả

## Kết quả test

## Vướng mắc

---
<!-- PLANNER ghi khi review. -->

## Review
