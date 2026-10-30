# Brief 0091 — World 5j (lỗi 10): đầu dải mép cỏ thon dần vào vách thay vì cắt phẳng

> Trạng thái: đã duyệt
> Review: đầy đủ + user xem (gộp cuối 5j)
> Model thực thi: Opus · medium
> Cập nhật: 2026-10-30 22:38

## Mục tiêu
Lỗi 10 của task [5j](../tasks/m1-world-5j-sua-loi-choi-thu.md). Ảnh lỗi: `~/Documents/User/khang/Game/art-test/shots/user5j_grass_cap_end.png` (ngoài repo). Ở bậc thang, dải mép cỏ (váy cỏ rủ) chạy tới chỗ ô bên cạnh dọc cạnh là khối đặc (vách bậc cao hơn) thì bị cắt phẳng: hiện "nắp" mặt cắt, trông cụt.

Sửa theo lựa chọn của user (2026-10-31, "Thon dần"): ở những đầu đó, mặt cắt mép cỏ **thu nhỏ dần về 0** trong `LipTaperLength` cuối, nối êm vào vách. Bỏ nắp.

## Bối cảnh cần đọc
`client/Assets/Game/World/BlockMesher.cs`:
- Hằng (`:30–55`): `LipOverhang` 0,12, `LipTopRound`, `LipBottomRound`, `LipDepth`, `LipFloor` 0,16, `LipProfilePoints` 8, `LipSegments` 4 (số đoạn dọc mỗi cạnh ô), `LipCornerSegments` 3.
- `public static float LipDepthAt(float w, int sideAxis, int sideSign, int plane, int top)` (`:62`).
- `public static void LipProfile(int i, float depth, out float2 p, out float2 n)` (`:75`): `p = (t ra ngoài mặt sườn, dy so với mặt trên)`. A0..A3 cuộn trên, C0..C2 cuộn dưới, W = (0, −LipFloor).
- `void AddGrassLip(int3 c, int3 g, uint color)` (`:753`).
- `bool HasLip(int3 cell, int sideAxis, int sideSign)` (`:817`).
- `void AddLipEdge(int3 c, int3 g, int sideAxis, int sideSign, uint color)` (`:826–890`):
  - lưới đỉnh `i = 0..LipProfilePoints−1`, `k = 0..LipSegments`, `sigma = k / LipSegments`, `depth = LipDepthAt(...)`, `LipProfile(i, depth, out p, out n)`, `AddVertex(LipPoint(c, sideAxis, sideSign, p.x, p.y, sigma), outward·n.x + up·n.y, color)`;
  - mặt trên = quạt từ `p0a/p0b` (t = −R);
  - mặt cong;
  - đáy trong = quạt từ `f0/f1` (t = −R, dy = −LipFloor);
  - **nắp hai đầu** (`:866–889`): điều kiện vẽ nắp ở đầu `end` (e = ∓1, `next = g` dịch `e` theo trục cạnh) là `!HasLip(next, sideAxis, sideSign) && IsSolid(next)`.
- `static float3 LipPoint(int3 c, int sideAxis, int sideSign, float t, float dy, float sigma)` (`:806`).
- `AddLipCorner` (`:893`), `AddLipConcave` (`:778`): không đổi.

`client/Assets/Game/Tests/World/BlockMesherTests.cs`:
- `LipEndCapMatchesClosedCrossSection` (`:466–486`): ô cỏ (4,8,4) kề đá (4,8,5); khẳng định có 2 nắp × (LipProfilePoints + 2) đỉnh pháp tuyến (0,0,1) ở z = 5. **Test này phải viết lại** (bước 3); được phép đổi kỳ vọng.
- Test mép cỏ khác: `LipDepthStaysInRange` :313, `LipProfileIsRoundedPillow` :323, `LipOnlyOnGrassWithOpenTopAndSide` :356, `LipIsClosedFromBelowAndUnchangedFromAbove` :447, `LipIsDeterministicAndContinuousAcrossCellsAndChunks` :488. Các test này giữ nguyên kỳ vọng.

## Quyết định đã chốt (không được đổi)
1. **Đầu thon** = đúng những đầu trước đây vẽ nắp: `!HasLip(next, sideAxis, sideSign) && IsSolid(next)`. Đầu khác (nối tiếp ô có mép, hoặc ô kề rỗng → góc lồi) giữ nguyên.
2. **Hệ số thon** tại vị trí `sigma` dọc cạnh: `s = khoảng cách từ sigma tới đầu thon gần nhất` (đầu 0 thon → `sigma`; đầu 1 thon → `1 − sigma`; cả hai → min).
   - `f = smoothstep(0, LipTaperLength, s)` = `math.smoothstep(0f, LipTaperLength, s)`.
   - Không đầu nào thon → `f = 1`.
3. Áp `f` lên điểm mặt cắt: `p' = p · f` (cả t và dy, W cũng vậy). Pháp tuyến giữ như `LipProfile` trả về. Ở đầu thon (f = 0), mọi điểm mặt cắt trùng điểm mép trên của mặt sườn `(t, dy) = (0, 0)`.
4. Hằng mới: `public const float LipTaperLength = 0.5f;` [số tạm, chưa chốt], kèm `<summary>`: "Độ dài đoạn thon ở đầu dải mép cỏ chạm khối đặc dọc cạnh (brief 0091, lỗi 10) [số tạm]." Hằng này là hằng hình học mesh như `LipOverhang`, không đưa vào JSON.
5. **Xoá hẳn code nắp hai đầu** trong `AddLipEdge`. Mặt trên (quạt `p0a/p0b`) và đáy trong (quạt `f0/f1`) giữ cách vẽ. Đỉnh hàng A0 và hàng W dùng điểm đã nhân `f`.
6. Lưới dọc cạnh giữ `LipSegments` = 4 đoạn. Không thêm đỉnh: thon lấy mẫu tại sigma 0; 0,25; 0,5; 0,75; 1. Với `LipTaperLength` 0,5: f(0) = 0, f(0,25) = 0,5, f(0,5) = 1.
7. **(Planner 2026-10-31, trả lời Vướng mắc — phương án A)** Mặt cong: bỏ tam giác suy biến ở cột thon — cột k = 0 khi `taper0` bỏ `(a, b2, a2)`; cột k = LipSegments − 1 khi `taper1` bỏ `(a, b, b2)`. Không kẹp f, không sửa `TrianglesFaceTheirNormals`.
8. Không đổi: góc lồi, góc lõm, collider, shader, mép cỏ khi không có đầu thon.

## Phạm vi
- Được sửa/tạo:
  - `client/Assets/Game/World/BlockMesher.cs` (hằng mới + `AddLipEdge` + `<summary>` của `AddLipEdge`/`AddGrassLip` nếu nhắc tới nắp);
  - `client/Assets/Game/Tests/World/BlockMesherTests.cs`;
  - mục Kết quả của brief này.
- Không được sửa: file khác (`design/`, glossary, shader).

## Các bước
1. `BlockMesher.cs` — thêm `LipTaperLength` (QĐ 4) cạnh `LipSegments`.
   - Trong `AddLipEdge`, trước vòng lưới: tính `bool taper0`, `taper1` theo QĐ 1 (dùng cùng `next` như code nắp cũ: đầu 0 → `next[edgeAxis] -= 1`, đầu 1 → `+= 1`).
   - Thêm hàm cục bộ `float TaperAt(float sigma)` theo QĐ 2.
   - Trong vòng lưới nhân `p` với `f` (QĐ 3). Xoá khối "Nắp hai đầu" (QĐ 5). Sửa comment của khối đó thành 1 dòng: `// Đầu chạm khối đặc dọc cạnh: mặt cắt thon về 0 (brief 0091, lỗi 10) — không vẽ nắp.`
   - `<summary>` của `AddGrassLip` không nhắc nắp → giữ.
   - Xong khi: biên dịch.
1b. (lượt 2) `AddLipEdge` — bỏ tam giác suy biến theo QĐ 7. Xong khi: `TrianglesFaceTheirNormals` đạt.
2. Chạy `BlockMesherTests` trên code mới **trước khi** sửa test. Ghi vào Kết quả test nào fail. Chỉ `LipEndCapMatchesClosedCrossSection` được phép fail. Test khác fail → dừng, cần Opus.
3. `BlockMesherTests.cs` — thay `LipEndCapMatchesClosedCrossSection` bằng `[Test] LipEndTapersIntoSolidNeighbour` (cùng world: cỏ (4,8,4), đá (4,8,5); `BlockMesher.Build(w, 0, 0, 0)`):
   - không còn đỉnh nào có pháp tuyến (0, 0, 1) ở z = 5 (`math.abs(p.z − 5) < 1e-4`), tức hết nắp;
   - không có đỉnh nào (của toàn mesh) ở z = 5 với x > 5 + 1e-4 hoặc x < 4 − 1e-4 — đầu dải không nhô ra ngoài mặt sườn (mesh khối cỏ/đá nằm trong x 4..5 nên không ảnh hưởng);
   - ở z = 4,5 (f = 1) có đỉnh mép phồng nhất: tồn tại đỉnh với `x ≥ 5 + LipOverhang − 1e-3` (sườn +x) và đỉnh với `x ≤ 4 − LipOverhang + 1e-3` (sườn −x);
   - ở z = 4,75 (f = 0,5): x lớn nhất của đỉnh trên sườn +x trong khoảng `5 + 0,5·LipOverhang ± 1e-3`.
   - `<summary>`/comment test ghi "brief 0091: đầu dải chạm khối đặc thon về 0, không nắp".
   - Xong khi: test đạt.
4. Chạy test theo "Lệnh executor chạy".

## Tiêu chí xong
- `LipEndTapersIntoSolidNeighbour` đạt.
- Mọi test `BlockMesherTests` khác đạt không đổi kỳ vọng; EditMode toàn bộ đạt; `read_console` Error 0.

## Test sau khi xong

### Phần cần test
- Đầu thon, không nắp (bước 1, QĐ 1–5) · `LipEndTapersIntoSolidNeighbour` · đạt.
- Mép cỏ giữa dải, góc lồi, liên tục qua ô/chunk (regression) · `LipDepthStaysInRange`, `LipProfileIsRoundedPillow`, `LipOnlyOnGrassWithOpenTopAndSide`, `LipIsClosedFromBelowAndUnchangedFromAbove`, `LipIsDeterministicAndContinuousAcrossCellsAndChunks` · đạt không đổi.
- Mesh khối/nước khác (regression) · `BlockMesherTests` toàn bộ · đạt.
- Toàn bộ EditMode (regression) · đạt.

### Lệnh executor chạy
1. Qua MCP Unity (Editor đang mở; đang Play Mode → `manage_editor` stop): `run_tests` EditMode lọc `Game.World.Tests.BlockMesherTests`. Chạy 2 lần: sau bước 1 (ghi test fail) và sau bước 3 → số đạt/fail.
2. `run_tests` EditMode toàn bộ → dòng tổng kết.
3. `read_console` Error → 0.
4. Dọn: `git status` chỉ còn file trong Phạm vi. Nếu `client/ProjectSettings/EditorSettings.asset` đổi thì `git checkout -- client/ProjectSettings/EditorSettings.asset`.
(Editor tắt thì dùng `python3 tools/unity_test.py EditMode`.)

### User tự test
Gộp cuối 5j: tới bậc thang như ảnh `user5j_grass_cap_end.png`, nhìn chỗ dải cỏ rủ chạm vách bậc cao bên cạnh.
- Đầu dải thu nhỏ dần, không còn mặt cắt phẳng.
- Không có khe hở hay mảng lạ ở chỗ nối.
- Nếu đoạn thon trông quá dài hoặc quá ngắn → báo để chỉnh `LipTaperLength` (0,5 ô) [số tạm].

## Dừng và chuyển lại Opus khi
- Brief mơ hồ hoặc mâu thuẫn với code thật.
- Cần một quyết định thuộc AGENTS.md §3.4 hoặc ngoài "Quyết định đã chốt".
- Test cũ nào ngoài `LipEndCapMatchesClosedCrossSection` phải đổi kỳ vọng.
- Cùng một lỗi sửa 2 lần vẫn chưa qua.
- Phải sửa file ngoài "Phạm vi".

---

## Kết quả
- Base commit: eb14624
- Chạy bằng: claude-opus-5-5 · medium
- Bắt đầu: 2026-10-30 22:24 · Kết thúc: 2026-10-30 22:38 (gồm thời gian chờ Planner trả lời Vướng mắc; làm thực ~25 phút, chạy EditMode toàn bộ ~7 phút)
- Commit: `146dec2` — `client/Assets/Game/World/BlockMesher.cs` (hằng `LipTaperLength` 0,5 [số tạm]; `AddLipEdge`: `taper0/taper1`, `TaperAt` smoothstep, `p *= TaperAt(sigma)`, bỏ tam giác suy biến ở cột thon (QĐ 7), xoá nắp hai đầu), `client/Assets/Game/Tests/World/BlockMesherTests.cs` (`LipEndCapMatchesClosedCrossSection` → `LipEndTapersIntoSolidNeighbour`).

## Kết quả test
- Bước 2 (`run_tests` EditMode `Game.World.Tests.BlockMesherTests`, code bước 1, chưa sửa test): 50/52 đạt, 2 fail [run]:
  - `LipEndCapMatchesClosedCrossSection` — "Expected: 20, But was: 0" (được phép fail: hết nắp).
  - `TrianglesFaceTheirNormals` — "triangle 961, Expected: greater than 0.0f, But was: 0.0f" (BlockMesherTests.cs:262) → **ngoài danh sách được fail → dừng**.
- Biên dịch: `read_console` Error 0 [run].
- Lượt 2 (sau bước 1b + 3), `run_tests` EditMode `Game.World.Tests.BlockMesherTests`: 52/52 đạt [run] (gồm `TrianglesFaceTheirNormals`).
- Đầu thon, không nắp · `LipEndTapersIntoSolidNeighbour` (chạy riêng theo tên) · **đạt** [run].
- Mép cỏ regression · `LipDepthStaysInRange`, `LipProfileIsRoundedPillow`, `LipOnlyOnGrassWithOpenTopAndSide`, `LipIsClosedFromBelowAndUnchangedFromAbove`, `LipIsDeterministicAndContinuousAcrossCellsAndChunks` · **đạt** không đổi kỳ vọng (trong 52/52).
- `BlockMesherTests` toàn bộ · **đạt** 52/52.
- EditMode toàn bộ · **đạt** — total 692, passed 692, failed 0, resultState Passed [run].
- `read_console` Error: 0 [run].
- Dọn: `git status` chỉ còn file trong Phạm vi; `EditorSettings.asset` không đổi (không chạy PlayMode).
- User tự test (gộp cuối 5j): **chờ user** — đầu dải cỏ rủ chạm vách bậc cao thon dần, không khe/mảng lạ, độ dài thon 0,5 ô.

## Vướng mắc
Code bước 1 đã sửa trong working tree (chưa commit): `client/Assets/Game/World/BlockMesher.cs` (`LipTaperLength`, `taper0/taper1`, `TaperAt`, `p *= TaperAt(sigma)`, xoá nắp).

**Vấn đề:** QĐ 3 cho f = 0 ở đầu thon → cả cột k = 0 (hoặc k = LipSegments) của lưới mặt cắt trùng một điểm (0, 0). Mặt cong vẽ mỗi tứ giác (a, b, b2) + (a, b2, a2); ở cột thon đầu 0 thì a ≡ a2 → tam giác (a, b2, a2) diện tích 0; đầu 1 thì b ≡ b2 → (a, b, b2) diện tích 0. `TrianglesFaceTheirNormals` yêu cầu dot(pháp tuyến hình học, pháp tuyến đỉnh) > 0 nên fail với tam giác suy biến (thấy trên địa hình sinh mặc định, chunk (1,0,1)). Mặt trên (quạt p0a) và đáy trong (quạt f0) không suy biến vì P0/F0 ở t = −R ≠ 0.

**Câu hỏi:** xử lý tam giác suy biến ở cột thon thế nào?
- A. Trong vòng mặt cong, bỏ tam giác suy biến: cột k = 0 khi `taper0` → bỏ (a, b2, a2); cột k = LipSegments − 1 khi `taper1` → bỏ (a, b, b2). Hình không đổi, không đổi test cũ. ~5 dòng.
- B. Kẹp f tối thiểu nhỏ (vd 0,02) → không suy biến nhưng đầu dải còn mặt cắt rất nhỏ, có khe hở hở (không nắp) — lệch QĐ 3.
- C. Sửa `TrianglesFaceTheirNormals` bỏ qua tam giác diện tích 0 — đổi kỳ vọng test cũ.

**Đề xuất:** A (đúng QĐ 3, không đổi test cũ, không tam giác thừa).
→ Planner chọn A (commit 73e5d09); đã làm ở bước 1b.

---

## Review
- 2026-10-31 (Planner, trả lời Vướng mắc): chọn A — bỏ 2 tam giác diện tích 0 ở cột thon (QĐ 7, bước 1b); hình không đổi, đúng QĐ 3, không đổi test cũ. Trạng thái → chờ làm.
- 2026-10-31 (Planner, review đầy đủ): `Chạy bằng` claude-opus-5-5 · medium khớp; mọi dòng "Phần cần test" có kết quả (user tự test gộp cuối 5j).
- `git show --stat 146dec2`: 2 file trong Phạm vi. Diff: `taper0/taper1` đúng điều kiện nắp cũ (QĐ 1); `TaperAt` = smoothstep(0, LipTaperLength, min khoảng cách tới đầu thon) (QĐ 2); `p *= f` (QĐ 3); bỏ đúng 2 tam giác suy biến ở cột k = 0 / LipSegments − 1 (QĐ 7); khối nắp xoá hẳn (QĐ 5).
- Test: BlockMesherTests 52/52 (gồm `TrianglesFaceTheirNormals`, `LipEndTapersIntoSolidNeighbour`), EditMode 692/692, console 0 lỗi. Chỉ test nắp cũ đổi kỳ vọng — được phép. Không chạy lại.
- Giờ: ~25 phút → roadmap dòng 5j.
- Kết luận: đã duyệt
