# 0007 — M1 tối giản (áp dụng đề xuất C1)

> Trạng thái: Chốt
> Ngày: 2026-10-13

## Bối cảnh
- Rà soát thiết kế (2026-10-13) thấy M1 kéo dài ~2,2–3,8 năm ở 10 giờ/tuần (95–163 ngày-người) trước khi biết core loop có vui không — trái mục tiêu của prototype ([roadmap](../05-roadmap.md)).
- Danh sách việc của M1 lệch nhau giữa roadmap, cơ chế, kiến trúc và hai kế hoạch prototype (tua nhanh, sinh map theo seed, khối cốt truyện, độ bền làm việc, người chơi làm công đoạn).
- Roadmap M1–M2 đã chốt một phần (user, 2026-10-13) → đổi phạm vi M1 cần quyết định mới.

## Các lựa chọn
- **A. Giữ M1 như cũ (95–163):** đủ hệ thống, nhưng tới ~2–4 năm mới biết game có vui không.
- **B. C1 — M1 tối giản (39–67):** chỉ phần cần để chơi được vòng lặp 30 giây; phần còn lại chuyển sang M2. Tổng không đổi.
- **C. C2 — prototype giấy/HTML 3–5 ngày trước M1:** không được chọn lần này (vẫn để trong đề xuất cắt).

## Quyết định
Chốt (user, 2026-10-13): **B — áp dụng C1.** Phạm vi M1 duy nhất:

| Hạng mục M1 | Gồm | Ngày-người |
|---|---|---|
| Voxel world tối giản | Map nhỏ cố định (vd 64 × 64 × 32 ô, sinh noise với một seed cố định, không hang), chunk 16³, dựng mesh mịn ×2, nhiều chunk + Jobs/Burst, đào/đặt nguyên ô + collider, màu vật liệu đơn giản, **không lưu world** | 17–28 |
| Sinh vật & loài | 3 loài (L1, L2, L3 của [levels](../02-gameplay/levels.md#map-1--đảo-vách-đá-biển)), khung chiếm chỗ 1 × 1, model khối thô | 2–4 |
| Chỉ số làm việc | Chỉ thuần thục + tốc độ | 1–2 |
| Môi trường sống | Một vùng = cả map; mỗi loài 2–3 yêu cầu; đạt hết → loài trở lại | 3–6 |
| Lệnh làm việc | Một lệnh trồng cây (xới, gieo, tưới, thu hoạch, vận chuyển), ghép việc theo năng lực + thuần thục, đình trệ, một kho, nhịp cố định | 7–11 |
| Tìm đường | A\* đi bộ trên lưới, khung 1 × 1, tính lại khi đào/đặt | 4–7 |
| Điều khiển | PC trước, cảm ứng cơ bản; camera góc 3, tâm nhắm, thanh công cụ | 3–5 |
| Thử chơi + đo hiệu năng | Graybox, đo trên mobile, thử chơi | 2–4 |
| **Cộng** | | **39–67** → ×1,5 = 47–80 tuần (~11–19 tháng) |

Chuyển sang M2 (56–96): voxel world đầy đủ (map 128 × 128 × 48, hang, lưu chunk, triplanar, vật thể nhiều ô) 18–27 · sinh vật & loài phần còn lại 3–6 · chỉ số làm việc phần còn lại 4–6 · môi trường sống theo vùng, nhiều loài 7–14 · lệnh làm việc phần còn lại (nhiều lệnh, vật tư, 2 sinh vật cùng làm một công đoạn) 13–19 · tìm đường phần còn lại (khung nhiều ô, vùng liên thông, ngân sách A\*) 6–13 · điều khiển 2–5 · đo hiệu năng map đầy đủ 3–6.

Không thuộc M1: tua nhanh, sinh map theo seed, khối cốt truyện, nước chảy, độ bền làm việc, người chơi làm công đoạn, bay/bơi, thẻ bài, backend.

## Hệ quả
- [05-roadmap.md](../05-roadmap.md): M1 39–67, M2 cộng 56–96; tổng M1–M4 không đổi vì C1.
- [mechanics.md](../02-gameplay/mechanics.md): cột milestone ghi "M1 (tối giản), M2"; luật M1 (nhận sinh vật, đào/đặt, kho) viết thêm.
- [world-tech.md](../04-tech/world-tech.md), [simulation-tech.md](../04-tech/simulation-tech.md): kế hoạch prototype viết lại theo bảng trên.
- [architecture.md](../04-tech/architecture.md), [risks.md](../04-tech/risks.md): cột milestone, bảng kiểm chứng sớm.
- C2 vẫn để trong đề xuất cắt.
