# Task: M1 simulation bước 1 — lõi mô phỏng nhịp cố định, số nguyên, seed, ghép việc + đi theo khoảng cách

> Trạng thái: user xác nhận 2026-10-14 (phạm vi theo [0014](../../design/decisions/0014-ghep-viec-o-sim-buoc-1.md), số tạm đồng ý) — giao Executor: [brief 0001](../briefs/0001-sim-loi-mo-phong.md)

## Mục tiêu
Lõi mô phỏng nông trại M1 ([simulation-tech](../../design/04-tech/simulation-tech.md#kế-hoạch-prototype-m1) bước 1, phạm vi theo 0014): nhịp cố định 10/giây, mọi đại lượng là số nguyên, bộ sinh số ngẫu nhiên có seed riêng; dữ liệu sinh vật / lệnh / công đoạn / kho; bộ quản lý nhiệm vụ tự giao công đoạn cho sinh vật rảnh, đúng năng lực, gần + thuần thục cao trước, công đoạn ưu tiên cao xét trước; sinh vật đi tới nơi làm theo khoảng cách ô (tốn thời gian theo tốc độ loài); vận chuyển đi 2 chặng; bỏ người làm giữa chừng giữ tiến độ.

## Ngoài phạm vi
- A\*, địa hình chặn đường, kiểm "tới được" (bước 2 — thay đi theo khoảng cách); chuỗi 5 công đoạn của lệnh trồng cây, cây lớn, hạt/nông sản vào kho (bước 3); đình trệ + thông báo (bước 4); hệ số thuần thục vào thời gian làm (bước 5 — bước 1 thuần thục chỉ dùng để chọn sinh vật); hiển thị, MonoBehaviour chạy nhịp, bản sao config trong Resources (bước 6); đo mobile (bước 7); môi trường sống (bước 8).
- M2: người chơi nhận việc qua tab nhiệm vụ, độ ưu tiên do người chơi đặt (bước 1 chỉ có trường dữ liệu), 2 sinh vật một công đoạn, độ bền làm việc, nghỉ, bệnh/mệt, tua nhanh, lưu trạng thái.
- Nhịp nước lan 8/giây ([0013](../../design/decisions/0013-nuoc-lan-theo-nhip.md), Chốt) giữ nguyên, không gộp vào nhịp mô phỏng.

## Tiêu chí chấp nhận (kiểm chứng được)
- [ ] Code trong `Game.Core` (No Engine References), `client/Assets/Game/Core/Sim/`, namespace `Game.Core.Sim`.
- [ ] `SimRng` (PCG32) cho đúng dãy chuẩn của PCG (seed 42, dãy 54) — test EditMode.
- [ ] `design/data/sim_m1.json` + `SimConfig` (giá trị mặc định trong code = file) + kiểm hợp lệ — test EditMode.
- [ ] Thời gian xong một công đoạn = 1 + nhịp đi (từng chặng, làm tròn lên) + nhịp làm — test EditMode với số cụ thể.
- [ ] Ghép việc: đúng năng lực, điểm khoảng cách ÷ thuần thục, ưu tiên cao trước, hoà điểm bốc thăm theo seed, công đoạn người chơi nhận không giao cho sinh vật — test EditMode.
- [ ] Bỏ người làm giữa chừng: giữ tiến độ; vận chuyển đang mang → hàng rơi tại ô hiện tại — test EditMode.
- [ ] **Tất định:** cùng kịch bản 2 lần → cùng mã băm `SimHash` từng nhịp; khác seed → kết quả bốc thăm khác — test EditMode.
- [ ] EditMode toàn bộ đạt (84 cũ + mới); `python3 tools/check_design.py` exit 0.
- [ ] Planner khi review: hàng bước 1 trong simulation-tech có kết quả, roadmap "Theo dõi M1" ghi giờ (ước lượng 3–5).

## Ràng buộc
- Không `float`/`double`, `System.Random`, `UnityEngine.Random`, `DateTime`, `Dictionary`/`HashSet` (lặp không thứ tự) trong `Game.Core.Sim`.
- Không thêm package/assembly. Không đổi world, `WorldEditor`, bộ sinh địa hình (mã băm world v2 `807d00bd048026a0` giữ nguyên).
- Tên loài L1–L3 tạm — user tự đặt tên riêng.

## Quyết định đã chốt (ai chốt, khi nào)
- Kế hoạch prototype simulation, bước 1 + 3 đổi theo 0014 — user 2026-10-14.
- Số tạm `sim_m1.json` (xới 6 s, gieo 3, tưới 3, thu hoạch 4, vận chuyển 2; thuần thục 100; tốc độ L1/L2 1,5 ô/s, L3 2,0 ô/s; seed mô phỏng riêng 20261014) — user 2026-10-14.
- Luật M1 kho/lệnh trồng cây — mechanics "Luật M1", user 2026-10-13.
- Kỹ thuật (Planner 2026-10-14): PCG32; mã băm FNV-1a như `WorldHash`; đơn vị: nhịp `long`, công = nhịp × 1000, tốc độ = phần nghìn ô/nhịp; đi từng ô theo trục X → Z → Y; thứ tự pha trong nhịp: làm → đi → giao việc.

## Câu hỏi còn mở
- (đã trả lời, user 2026-10-14) Phạm vi bước 1 → 0014 phương án A; người chơi nhận việc → M2; số tạm → đồng ý.

## Kết quả
_(chưa có)_
