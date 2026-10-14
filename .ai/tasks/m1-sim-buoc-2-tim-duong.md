# Task: M1 simulation bước 2 — A\* đi bộ theo khung chiếm chỗ, tính lại khi đào/đặt, đình trệ "không có đường"

> Trạng thái: user xác nhận 2026-10-14 ([0015](../../design/decisions/0015-tim-duong-khung-va-dao-duoi-chan.md)) — giao Executor: [brief 0002](../briefs/0002-sim-tim-duong.md)

## Mục tiêu
Thay "đi theo khoảng cách ô" của bước 1 bằng A\* đi bộ trên `WorldData` ([simulation-tech](../../design/04-tech/simulation-tech.md#kế-hoạch-prototype-m1) bước 2). A\* kiểm đủ khung chiếm chỗ 3 chiều của loài và không đi vào nước. Khi đào/đặt: kiểm lại đường đang đi, tìm lại đường; không tìm được thì công đoạn đình trệ "không có đường". Đào dưới chân thì sinh vật chạy ra ô bên cạnh, không có ô nào thì rơi xuống.

## Ngoài phạm vi
- Vùng liên thông, ngân sách A\* mỗi nhịp, bộ đệm đường (M2). Thông báo đình trệ cho người chơi, các lý do khác như kho đầy hay thiếu năng lực (bước 4). Hiển thị, nối `WorldEditor` ↔ `Simulation` trong scene (bước 6). Bay, bơi (M2).
- Không đổi luật đào/đặt trong `WorldEditor`: đào dưới chân vẫn được phép, mô phỏng tự xử lý.

## Tiêu chí chấp nhận (kiểm chứng được)
- [ ] `GridPathfinder` (Game.Core.Sim): đi 4 hướng, leo/xuống 1 ô, đủ khung X × Z × Y, không vào nước, tất định — kiểm bằng test EditMode với khung 1 × 1 × 1, 1 × 1 × 2, 2 × 2 × 1.
- [ ] Mô phỏng dùng A\*: test bước 1 chạy trên world phẳng, giữ nguyên mọi số nhịp.
- [ ] Đào cắt đường đang đi → nhịp sau công đoạn `Blocked` / `NoPath`, sinh vật rảnh. Đặt lại khối → nhịp sau công đoạn được giao lại — kiểm bằng test EditMode qua `WorldEditor`.
- [ ] Ứng viên gần nhất không có đường → thử ứng viên kế tiếp — kiểm bằng test EditMode.
- [ ] Đào dưới chân: chạy ra ô bên cạnh; không có ô nào thì rơi; đang làm thì giữ tiến độ — kiểm bằng test EditMode.
- [ ] `Simulation.IsOccupied` phủ đủ khung, nối được vào `WorldEditor.IsOccupied` (đặt khối vào khung → `Occupied`) — kiểm bằng test EditMode.
- [ ] EditMode toàn bộ đạt; `check_design.py` exit 0. Planner khi review: ghi kết quả vào simulation-tech + roadmap (ước lượng 4–6).

## Quyết định đã chốt
- 0015 (user 2026-10-14): khung 3 chiều theo loài, M1 đều 1 × 1 × 1; không vào nước; đào dưới chân → chạy ra / rơi.
- Planner 2026-10-14: 4 hướng; mỗi bước tốn 1 ô thời gian (kể cả leo/xuống); sinh vật không chặn nhau; vẫn chọn sinh vật theo khoảng cách ước tính rồi mới chạy A\*; đình trệ `NoPath` được thử lại khi địa hình đổi; chạy ra/rơi diễn ra tức thì ở đầu nhịp sau khi đổi ô, hiển thị bước 6 sẽ làm mượt.
