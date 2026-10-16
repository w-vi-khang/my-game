# Task: M1 simulation bước 5 — thuần thục vào thời gian công đoạn (bảng mức → tốc độ làm mỗi nhịp)

> Trạng thái: user xác nhận 2026-10-16 ([0018](../../design/decisions/0018-thuan-thuc-bang-muc.md)) — giao [brief 0005](../briefs/0005-sim-thuan-thuc.md)

## Mục tiêu
Bước 5 của [kế hoạch prototype simulation](../../design/04-tech/simulation-tech.md#kế-hoạch-prototype-m1):
- Thời gian một công đoạn phụ thuộc **độ thuần thục** của sinh vật đang làm. Thuần thục tra qua **bảng mức thuần thục** (ngưỡng → hệ số tốc độ làm, phần nghìn) trong `design/data/sim_m1.json`.
- Mỗi nhịp làm việc, tiến độ tăng đúng hệ số của sinh vật đang làm → số nhịp làm = ⌈thời gian gốc × 1000 ÷ hệ số⌉; đổi sinh vật giữa chừng thì giữ tiến độ.

## Ngoài phạm vi
- Thuần thục đổi tốc độ di chuyển (không làm; tốc độ đi giữ như bước 1).
- Sức chở theo thuần thục vận chuyển (M2).
- Cân bằng số liệu bảng bằng `game-balance`, `design/data/stats.json` (M2).
- Hệ số nội tại, mệt/bệnh, người làm thứ 2 (M2, đã ghi ở "Chuyển sang M2").
- Đổi điểm ghép việc (giữ khoảng cách ÷ thuần thục).
- Đổi thuần thục của L1–L3 (giữ 100).

## Tiêu chí chấp nhận (kiểm chứng được)
- [ ] Tra bảng đúng ở mọi ngưỡng biên (1, 49, 50, 99, 100, 199, 200, 399, 400, rất lớn) — test EditMode.
- [ ] Bảng sai bị từ chối khi nạp (rỗng, ngưỡng đầu > 1, ngưỡng không tăng, hệ số ngoài [1, 10000], hệ số giảm) — test EditMode.
- [ ] Đổi thuần thục → số nhịp làm = ⌈B × 1000 ÷ hệ số⌉ cho cả năng lực 1 và 2, có trường hợp làm tròn lên — test EditMode.
- [ ] Đi + làm: nhịp xong = nhịp tới nơi + số nhịp làm theo thuần thục — test EditMode.
- [ ] Đổi sinh vật giữa chừng: giữ tiến độ, phần còn lại theo hệ số của sinh vật mới — test EditMode.
- [ ] Test bước 1–4 giữ nguyên mọi số nhịp; EditMode toàn bộ đạt; `check_design.py` exit 0. Planner khi review: ghi kết quả vào simulation-tech + roadmap (ước lượng 1).

## Quyết định đã chốt
- 0018 (user 2026-10-16): bảng mức theo ngưỡng giá trị; áp theo tốc độ mỗi nhịp; "tốc độ" = tốc độ làm; sức chở để M2; số tạm, L1–L3 giữ 100.
- 0018 (Planner): bảng tạm 0 → 500, 50 → 750, 100 → 1000, 200 → 1250, 400 → 1500 (‰); luật kiểm bảng khi nạp.

## Kết quả
_(chưa có)_
