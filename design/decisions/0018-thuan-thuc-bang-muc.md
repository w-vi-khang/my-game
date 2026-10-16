# 0018 — Simulation bước 5: thuần thục vào thời gian công đoạn qua bảng mức, áp theo tốc độ mỗi nhịp

> Trạng thái: Chốt
> Ngày: 2026-10-16
> Thay đổi: làm rõ "hệ số thuần thục" trong công thức thời gian công đoạn của [simulation-tech — Lệnh làm việc](../04-tech/simulation-tech.md) và [mechanics — Lệnh làm việc](../02-gameplay/mechanics.md#lệnh-làm-việc); hàng bước 5 trong [kế hoạch prototype simulation](../04-tech/simulation-tech.md#kế-hoạch-prototype-m1).

## Bối cảnh
Viết task contract simulation bước 5 ("Thuần thục + tốc độ vào thời gian công đoạn", xong khi "đổi thuần thục → thời gian đổi đúng công thức"). Công thức đã có: `thời gian gốc ÷ hệ số thuần thục × …`, các hệ số là bảng dữ liệu. Còn mở: thuần thục (giá trị thật, cả 3 loài M1 đang là 100) ra hệ số thế nào, làm tròn, giới hạn; "tốc độ" là gì; thuần thục 2 "vận chuyển nhiều hàng hơn mỗi lượt" làm khi nào.

## Quyết định
**Chốt (user, 2026-10-16):**
- **Bảng mức theo ngưỡng giá trị**: thuần thục vẫn là giá trị thật của chỉ số. Bảng dữ liệu gồm các dòng (ngưỡng thuần thục tối thiểu → hệ số tốc độ làm); dùng dòng có ngưỡng lớn nhất không vượt giá trị của sinh vật.
- **Áp theo tốc độ mỗi nhịp**: mỗi nhịp làm việc, tiến độ công đoạn tăng theo hệ số tốc độ của sinh vật đang làm. Đổi sinh vật giữa chừng → giữ tiến độ, phần còn lại theo hệ số của sinh vật mới.
- **"Tốc độ" ở bước 5 = tốc độ làm công đoạn.** Tốc độ di chuyển đã có từ bước 1, thuần thục không đổi tốc độ đi.
- **Thuần thục vận chuyển → nhiều hàng hơn mỗi lượt: để M2.** M1 mỗi lượt chở như hiện nay; thuần thục vận chuyển chỉ làm nhanh hơn.
- **Số liệu M1 là số tạm**; cân bằng thật bằng `game-balance` khi đã có trần và hệ số quy đổi chỉ số (M2). L1–L3 giữ thuần thục 100.

**Planner (2026-10-16), chi tiết cài đặt:**
- Bảng tạm `[số tạm]` (ngưỡng → hệ số tốc độ, phần nghìn): 0 → 500, 50 → 750, 100 → 1000, 200 → 1250, 400 → 1500. Thuần thục 100 → 1000‰ nên mọi số nhịp cũ giữ nguyên.
- Số nhịp làm = ⌈thời gian gốc (nhịp) × 1000 ÷ hệ số⌉ (làm tròn lên, ít nhất 1 nhịp) — kết quả tự nhiên của cách cộng tiến độ mỗi nhịp.
- Bảng nằm trong `design/data/sim_m1.json`. Kiểm khi nạp: không rỗng, ngưỡng đầu ≤ 1 (mọi sinh vật đều có mức), ngưỡng tăng dần, hệ số trong [1, 10000] và không giảm.
- Ghép việc giữ điểm = khoảng cách ÷ thuần thục như bước 1.

## Hệ quả
- Ước lượng bước 5 giữ **1** ngày-người; M1 không đổi.
- Test cũ không đổi số nhịp (thuần thục 100 → hệ số 1000‰).
- "Sức chở theo thuần thục vận chuyển" thêm vào "Chuyển sang M2" của [simulation-tech](../04-tech/simulation-tech.md#kế-hoạch-prototype-m1).
- Cập nhật: [mechanics](../02-gameplay/mechanics.md#lệnh-làm-việc), [simulation-tech](../04-tech/simulation-tech.md), [glossary](../glossary.md).
