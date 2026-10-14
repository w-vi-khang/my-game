# Task: M1 simulation bước 3 — lệnh trồng cây 5 công đoạn, ô ruộng có hàng đợi vụ, một kho

> Trạng thái: user xác nhận 2026-10-14 ([0016](../../design/decisions/0016-o-ruong-kho-nha-va-sinh-vat-ket.md)) — giao [brief 0003](../briefs/0003-sim-trong-cay.md)

## Mục tiêu
Lệnh trồng cây trên một vùng ô ruộng ([simulation-tech](../../design/04-tech/simulation-tech.md#kế-hoạch-prototype-m1) bước 3, luật ở [mechanics — Kho & lệnh trồng cây](../../design/02-gameplay/mechanics.md#kho--lệnh-trồng-cây--luật-m1)). Mỗi ô ruộng chạy chuỗi riêng: xới → gieo (trừ 1 hạt trong kho) → tưới → cây lớn 120 giây → thu hoạch → vận chuyển 1 nông sản + 1 hạt vào kho. Mỗi ô có hàng đợi vụ (2 chỗ). Sinh vật ưu tiên đứng ở ô kề để làm, không có thì đứng trên ô. Kho là công trình chiếm ô, giao hàng ở ô kề. Dùng ghép việc (bước 1) và A\* (bước 2) đã có.

## Ngoài phạm vi
- Thông báo đình trệ cho người chơi, lý do "thiếu năng lực" (bước 4). Bước 3 chỉ cần mô phỏng không giao việc sai: gieo chờ khi hết hạt, vận chuyển chờ khi kho không đủ chỗ (ghi lý do vào `Stall`), vận chuyển nằm chờ khi không có loài vận chuyển.
- Nhà sinh vật, sinh vật bị kẹt (bước 4). Thuần thục vào thời gian (bước 5). Hiển thị: hộp hàng, biểu tượng công cụ (bước 6).
- Nhiều loại cây, cấp ô ruộng, nhiều kho, huỷ lệnh hoặc bỏ vụ khỏi hàng đợi (M2). Đào/đặt làm hỏng ô ruộng hoặc ô dưới kho: chưa xử lý (ghi `ideas.md` khi review).
- Khung sinh vật lớn hơn 1 × 1 × 1 khi tính ô kề: M1 chỉ có khung 1 × 1 × 1; luật ô kề tính theo neo.

## Tiêu chí chấp nhận (kiểm chứng được)
- [ ] L1, L2, L3 làm xong một vụ trên một ô ruộng, kho +1 nông sản +1 hạt, đúng số nhịp tính tay — test EditMode.
- [ ] Không có L3 → nông sản nằm tại ruộng, kho không đổi — test EditMode.
- [ ] Ưu tiên ô kề; không có ô kề đứng được thì làm ngay trên ô — test EditMode.
- [ ] Hàng đợi vụ: thêm tối đa 2; thu hoạch xong vụ kế bắt đầu lại từ xới — test EditMode.
- [ ] Hết hạt → gieo chờ, có hạt lại thì giao; kho không đủ chỗ → vận chuyển chờ; bỏ giữa chừng thì trả hạt / trả chỗ đã giữ — test EditMode.
- [ ] Kho chặn đường đi và chặn đặt khối — test EditMode.
- [ ] Cùng đầu vào → cùng mã băm từng nhịp với lệnh trồng cây 3 ô × 2 vụ — test EditMode.
- [ ] Test bước 1–2 giữ nguyên mọi số nhịp; EditMode toàn bộ đạt; `check_design.py` exit 0. Planner khi review: ghi kết quả vào simulation-tech + roadmap (ước lượng 3–5).

## Quyết định đã chốt
- 0016 (user 2026-10-14): ưu tiên ô kề; kho là công trình, giao ở ô kề; mỗi ô ruộng có chuỗi riêng + hàng đợi vụ, ô cấp 1 có 2 chỗ; 1 lượt vận chuyển mang 1 nông sản + 1 hạt; sức chứa 50 tính chung.
- Planner 2026-10-14: hàng đợi gồm cả vụ đang làm, lưu chỉ số loại cây (M1 luôn 0); ô kề = 4 hướng ngang quanh ô phía trên ô ruộng, lệch cao tối đa 1, chọn ô có đường ngắn nhất bằng A\* nhiều đích; kho chặn thân và không đứng lên được; hạt trừ khi giao công đoạn gieo, chỗ trong kho giữ khi giao vận chuyển, trả lại khi bị bỏ giữa chừng; công đoạn mới tạo ngay trong nhịp công đoạn trước xong; cây chín ở nhịp (tưới xong + 1200).

## Kết quả
_(chưa có)_
