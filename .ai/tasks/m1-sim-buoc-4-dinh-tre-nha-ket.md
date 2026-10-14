# Task: M1 simulation bước 4 — đình trệ + thông báo, nhà mẫu 2 × 2 × 2, sinh vật bị kẹt

> Trạng thái: user xác nhận 2026-10-14 ([0016](../../design/decisions/0016-o-ruong-kho-nha-va-sinh-vat-ket.md), [0017](../../design/decisions/0017-dinh-tre-thong-bao-nha-mau-va-dem-ket.md)) — giao [brief 0004](../briefs/0004-sim-dinh-tre-nha-ket.md)

## Mục tiêu
Bước 4 của [kế hoạch prototype simulation](../../design/04-tech/simulation-tech.md#kế-hoạch-prototype-m1):
- Đình trệ có đủ lý do: không có đường, kho đầy, hết hạt (đã có từ bước 2–3) và thêm **thiếu năng lực**.
- Mô phỏng phát **sự kiện thông báo** khi bắt đầu và khi hết đình trệ (UI đọc ở bước 6).
- Thêm **nhà mẫu** 2 × 2 × 2 (khối đặc, sức chứa 2). Sinh vật có nhà mới được giao việc.
- **Sinh vật bị kẹt** (rảnh + không có đường về nhà liên tục 300 nhịp) → hiện lại ở ô kề nhà, kéo theo hàng nó đã làm rơi.

## Ngoài phạm vi
- UI/hiển thị thông báo, bấm để xem tại chỗ (bước 6). Log trong Unity.
- Nhà người chơi tự xây, cửa, nội thất, nhiều kích thước, sinh vật rảnh đi quanh nhà, đổi nhà, xoá nhà (M2).
- Đào/đặt làm hỏng ô dưới nhà/kho ([ideas](../../design/ideas.md)). Ô kề tính theo neo cho khung lớn hơn 1 × 1 × 1 (như bước 3).
- Thuần thục vào thời gian (bước 5). Đo hiệu năng (bước 7).

## Tiêu chí chấp nhận (kiểm chứng được)
- [ ] Nhà chiếm 8 ô, chặn đi qua và chặn đặt khối, kiểm vị trí hợp lệ; sức chứa 2; sinh vật không nhà không được giao việc — test EditMode.
- [ ] `NoAbility`: chỉ tính sinh vật có nhà; đang bận vẫn tính là có; mở lại khi thêm sinh vật / gắn nhà — test EditMode.
- [ ] Sự kiện bắt đầu / hết đình trệ đúng nhịp, đúng vị trí cho `NoPath`, `StorageFull`, `NoSeeds`, `NoAbility`; không lặp lại — test EditMode.
- [ ] Kẹt: rảnh + không có đường về nhà 300 nhịp → hiện lại ở ô kề nhà đầu tiên; đang làm không đếm, có việc lại thì đếm lại từ 0; không có ô hiện lại thì chờ; hàng đã rơi theo về nhà — test EditMode.
- [ ] Test bước 1–3 giữ nguyên mọi số nhịp (chỉ thêm gắn nhà + 2 kỳ vọng đổi sang `NoAbility`); EditMode toàn bộ đạt; `check_design.py` exit 0. Planner khi review: ghi kết quả vào simulation-tech + roadmap (ước lượng 3–4).

## Quyết định đã chốt
- 0016 (user): nhà mẫu 2 × 2 × 2 sức chứa 2, đặt bằng code; có nhà mới làm việc; kẹt 30 giây → hiện lại ở nhà.
- 0017 (user 2026-10-14):
  - Thông báo = sự kiện trong lõi mô phỏng.
  - Thiếu năng lực chỉ tính sinh vật có nhà.
  - Nhà là khối đặc, hiện lại ở ô kề.
  - Gắn nhà bằng `AddHouse` + `AssignHome`.
  - Kiểm đường về nhà theo sự kiện.
  - Chỉ đếm kẹt khi rảnh.
  - Hàng đã rơi theo sinh vật về nhà.
