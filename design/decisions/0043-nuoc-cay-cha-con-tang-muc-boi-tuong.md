# 0043 — Nước theo cây cha–con: mức lan, tăng mức bởi tường, xoá dây chuyền + tái tạo; mesh nước liền, bo góc chỗ giao hướng

> Trạng thái: Chốt
> Ngày: 2026-10-27 (user chốt 2026-10-27)
> Thay đổi:
> - [0041](0041-nuoc-chay-trong-ranh-hep-va-thac-nuoc.md): ô hẹp, kiểm khối khi mất nguồn, cờ "đang rút" (bit 15) → thay bằng tăng mức bởi tường + xoá dây chuyền. Giữ thác nước demo.
> - [0042](0042-mat-nuoc-theo-muc-lan-5-o-o-noi.md): bỏ ô nối (chuyển vào [ideas](../ideas.md)); công thức độ cao mặt nước đổi theo mức 0–5.
> - [0040](0040-nuoc-chay-nguon-lan-gioi-han-m1.md) mục 2, 3, 5: rơi, lan, rút viết lại theo luật dưới. Giữ mục 1 (nguồn), 4 (hố được lấp), 6 (nhịp).

## Bối cảnh
User chơi thử gộp 0076–0078 đạt (2026-10-27), rồi đưa bộ quy tắc mô phỏng nước trên lưới 2D (đã thử ở nơi khác) và yêu cầu làm lại logic theo đó [user]:
- khối nước có `id`, `parent_id`, `lv` (tối đa 5); lan Trên → Phải → Dưới → Trái, mỗi bước `lv − 1`, `lv = 0` thì dừng;
- tăng mức bởi tường: kề ≥ 2 khối đặc và `0 < lv < 5` → `lv = 5`, rồi phát sóng mức (`propagateLV`);
- xoá dây chuyền theo tầng con (Stack 1, 2, …) và tái tạo ô vừa xoá từ ô nước lân cận;
- thêm cho 3D: dưới ô nước không phải khối đặc thì không lan ngang mà tạo ô con phía dưới, ô con mức tối đa; nước chảy xuống nước thì không lan;
- hiển thị: khối giao giữa chảy ngang và chảy xuống được bo góc kiểu fillet; các khối vẽ chung, không rời rạc tách lớp như hiện tại.

User trả lời 4 câu hỏi (2026-10-27): tái tạo không tính ô trong nhánh đang xoá; lan và xoá theo nhịp; bỏ ô nối; ao/hồ/biển là nguồn gốc cố định.

## Quyết định
Phần user đưa [user]; phần Planner chuyển sang 3D ghi (đề xuất Planner), số ghi [số tạm], chưa chốt, nằm trong `design/data/water_m1.json`.

### 1. Dữ liệu
- **Nguồn gốc:** ô nước có sẵn khi sinh world (biển, hồ, ao demo) và ô biển loang theo 0013. `lv = 5`, `parent_id = null`, không bị xoá dây chuyền; chỉ mất khi bị đặt khối đè (0013 giữ nguyên) [user].
- **Ô nước chảy:** có `id` (tăng dần theo thứ tự sinh, tất định), `parent_id`, `lv` 0–5. `lv` và cờ rơi nằm trong ô như hiện nay; `id`/`parent_id` lưu ở bảng phụ trong `WorldEditor`, chỉ cho ô nước chảy (đề xuất Planner). Bit 15 ("đang rút") được trả lại.
- **Mức tối đa** `maxLv = 5` [số tạm].

### 2. Lan (mỗi nhịp một bước, 8 nhịp/giây như hiện nay)
Mỗi nhịp, xử lý các ô được kích hoạt ở nhịp trước theo `id` tăng dần (đề xuất Planner, để tất định khi hai ô cùng nhắm một ô trống — ô `id` nhỏ thắng):
1. **Dưới là ô trống** → không lan ngang; tạo ô con phía dưới, `lv = maxLv` [user]. Áp dụng cả ô `lv = 0` (nước vẫn rơi — đề xuất Planner).
2. **Dưới là nước** → ô nước chảy không lan, không tạo gì ("nước chảy xuống nước không lan") [user]. Riêng **nguồn gốc** đứng trên nước (lớp mặt ao/hồ sâu) vẫn lan ngang như đứng trên khối đặc (đề xuất Planner — nếu không, đào bờ hồ sâu sẽ không có nước chảy vào).
- Ô trống ≤ mực nước biển chạm nguồn: để biển loang (0013) lấp thành nguồn, không tạo ô chảy (giữ 0040 QĐ 7a).
3. **Dưới là khối đặc** và `lv > 0` → lan sang 4 ô ngang còn trống theo thứ tự **+z, +x, −z, −x** (Trên, Phải, Dưới, Trái nhìn từ trên xuống, +z là "trên" của minimap — đề xuất Planner), ô con `lv − 1`, `parent_id` = ô này [user].
- Không lan vào khối đặc hoặc ô đã có nước. Không dâng lên [user].
- Ô mới, ô tái tạo, ô vừa tăng mức, và ô nước kề một ô vừa trống được kích hoạt cho nhịp sau (đề xuất Planner).

### 3. Tăng mức bởi tường [user]
- Xét khi ô nước được tạo, được tái tạo, hoặc khi có khối đặc mới đặt vào ô kề.
- Đếm khối đặc ở **4 ô ngang** (đề xuất Planner: chỉ ngang, giống lưới 2D). Nếu ≥ 2 và `0 < lv < maxLv` → `lv = maxLv`, rồi phát sóng mức.
- Ngưỡng 2 [số tạm].
- Hệ quả: rãnh rộng 1 ô giữ mức tối đa nên nước chảy hết rãnh (thay ô hẹp của 0041); chỗ rộng vẫn lan tối đa 5 ô.

### 4. Phát sóng mức (`propagateLV`) [user]
Ô có mức mới `L` xét 4 ô nước ngang. Ô kề có `lv < L − 1`:
- không phải cha của ô phát → `lv = L − 1`, `parent_id` = ô phát;
- là cha → `lv = L − 1`, giữ `parent_id`.
- Ô kề là **tổ tiên** của ô phát (cha của cha…) cũng chỉ đổi mức, giữ `parent_id` (đề xuất Planner — đổi cha khi đó tạo vòng cha–con khép kín, vòng không còn nối về nguồn nên không bao giờ rút).
Lặp tiếp từ ô vừa đổi. Phát sóng chạy hết trong cùng nhịp (đề xuất Planner: chỉ đổi mức, không sinh ô mới; ô đổi mức được kích hoạt để nhịp sau lan tiếp). Nguồn gốc không bị đổi.

### 5. Xoá dây chuyền và tái tạo [user]
Khi một ô nước chảy bị xoá (đặt khối đè lên) hoặc mất cha:
- **Nhịp 1:** xoá ô đó; con trực tiếp của nó vào Stack 1.
- **Nhịp K + 1:** con của các ô trong Stack K vào Stack K + 1, rồi xoá các ô trong Stack K.
- **Nhánh đang xoá:** ô có chuỗi cha đi tới một ô đã xoá hoặc đang chờ xoá. Ô thuộc nhánh đang xoá không lan, không làm nguồn tái tạo (đề xuất Planner).
- **Tái tạo:** sau mỗi lần xoá, ở từng vị trí vừa xoá (không phải khối đặc), xét 4 ô nước ngang **không thuộc nhánh đang xoá** (mọi hậu duệ của ô bị xoá ban đầu) [user]. Có → tạo lại ô nước, `lv` lấy theo ô có `lv` cao nhất (bằng nhau → theo thứ tự +z, +x, −z, −x), `parent_id` lấy theo `parent_id` của ô đó; nếu ô đó là nguồn gốc thì `parent_id` = ô nguồn (đề xuất Planner — nếu chép `null` thì ô tái tạo thành nguồn không bao giờ cạn).
- Ô tái tạo có `id` mới, được xét tăng mức bởi tường và kích hoạt để lan lại.
- Ô ở chỗ đã đặt khối thì không tái tạo.
- Đặt khối đè lên ô nguồn gốc: các ô chảy có cha là ô đó bắt đầu xoá dây chuyền như trên.

### 6. Hiển thị (đề xuất Planner, số [số tạm])
- **Độ cao mặt nước** ô chảy mức `L`: `0,9 × (L + 1) / (maxLv + 1)` (mức 5 → 0,9, mức 0 → 0,15). Nguồn 0,9; ô có nước phía trên 1. Góc vẫn lấy trung bình như 0042.
- **Bo góc chỗ giao hướng (fillet, theo hình user gửi):**
  - mép trên: ô chảy ngang có ô kề là cột nước đang rơi xuống (nước tràn qua mép) → cạnh giữa mặt nước và mặt đổ xuống bo lồi cung tròn;
  - chân thác: cột nước rơi chạm mặt nước bên dưới → góc giữa mặt đổ và mặt nước bên dưới bo lõm cung tròn;
  - bán kính 0,3 ô [số tạm], 3 đoạn cung [số tạm].
- **Vẽ liền:** mesh nước một chunk là một bề mặt liền: không mặt nào giữa hai ô nước (kể cả giữa ô rơi và ô dưới nó), đỉnh trùng nhau được gộp, không hai mặt nước chồng hay trùng nhau, cột nước rơi là một khối liền chứ không thành từng hộp chồng.

## Ngoài phạm vi
- Bảo toàn lượng nước, người chơi tạo nguồn: vẫn M2.
- Ô nối 0042 → [ideas](../ideas.md).

## Hệ quả
- Ước lượng (AI): +3,5–5,5 ngày-người M1 — lõi cây cha–con + tăng mức + xoá dây chuyền 2–3; mesh liền + bo góc 1,5–2,5. Phần ô hẹp, kiểm khối, ô nối đã làm ở 5f/5g bị thay. Vượt quỹ M1 thêm; cắt trước: bo góc chân thác (giữ bo mép trên).
- Khi Chốt cập nhật: [world-tech](../04-tech/world-tech.md) mục Nước chảy + bước 5h; [mechanics](../02-gameplay/mechanics.md) luật nước chảy; [glossary](../glossary.md) (mức nước, tăng mức bởi tường, xoá dây chuyền; bỏ ô nối, ô hẹp); [roadmap](../05-roadmap.md); [ideas](../ideas.md); `design/data/water_m1.json` (`maxLevel`, `wallBoostMinSolid`, `filletRadius`; bỏ `narrowMaxOpenSides`, `supportCheckMaxCells`).
- Code: task mới `m1-world-nuoc-cay-cha-con`, 2 brief (lõi `WorldEditor`; mesh `BlockMesher`). Test giữ: biển 0013, mã băm world sinh ra, thác demo đổ vào ao không tràn.
