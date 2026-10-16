# 0019 — Simulation M1: sinh vật chặn nhau khi đi, rảnh thì về quanh nhà + lang thang, không dừng trên ô ruộng

> Trạng thái: Chốt
> Ngày: 2026-10-16
> Thay đổi: thêm bước 6b vào [kế hoạch prototype simulation](../04-tech/simulation-tech.md#kế-hoạch-prototype-m1); kéo "sinh vật rảnh đi quanh nhà" từ M2 ([0016](0016-o-ruong-kho-nha-va-sinh-vat-ket.md)) vào M1; cập nhật [mechanics — Nhà sinh vật](../02-gameplay/mechanics.md#nhà-sinh-vật), [roadmap](../05-roadmap.md).

## Bối cảnh
User xem scene SimView (simulation bước 6, [brief 0006](../../.ai/briefs/0006-sim-hien-thi.md), 2026-10-16) và thấy ba điểm khiến mô phỏng trông sai:
- sinh vật đi xuyên qua nhau;
- sinh vật rảnh đứng yên tại chỗ vừa làm xong;
- sinh vật rảnh đứng trên ô ruộng.

Hiện tại (bước 1–5): A\* chỉ tính địa hình + công trình (nhà, kho); sinh vật không chiếm ô của nhau. Sinh vật rảnh không làm gì. "Sinh vật rảnh hoạt động trong một bán kính quanh nhà" đã có trong [mechanics](../02-gameplay/mechanics.md#nhà-sinh-vật) nhưng xếp M2 (0016).

## Các lựa chọn
- **Chặn nhau:** (a) đi xuyên như cũ — 0 công, nhưng user thấy sai. (b) Đi xuyên chỉ khi đối đầu — ~1–2 ngày-người, vẫn thấy hai hộp chồng nhau. (c) **Chặn hẳn**: chờ, tìm đường khác, nhường ở lối hẹp, kèm phao chống kẹt — 2–4 ngày-người. Phải đo lại 5 ms/nhịp ở bước 7.
- **Rảnh:** (a) đứng yên — 0 công. (b) **Về quanh nhà + lang thang** — 1,5–2 ngày-người (M2 ước 1–2).
- **Ô ruộng:** (a) cấm đi qua — đường vòng dài, khu demo chật. (b) **Đi qua tự do, chỉ không dừng** — 0,5 ngày-người.

## Quyết định
**Chốt (user, 2026-10-16):** cả ba vào M1, làm ở **simulation bước 6b**, trước bước 7 (đo 60 sinh vật).

1. **Chặn nhau khi đi** (chọn (c)):
   - **A\* không tính sinh vật** khi tìm đường. Chỉ kiểm khi sinh vật sắp bước vào ô (theo cả khung chiếm chỗ X × Z × Y) đang có sinh vật khác.
   - **Gặp sinh vật đang làm việc hoặc đang đi chắn ô kế tiếp:** đứng chờ **1 giây** (10 nhịp). Hết chờ mà vẫn chắn → tìm đường khác, coi các sinh vật đang chắn là vật cản.
   - **Đối đầu ở lối hẹp** (không có đường vòng): con **ưu tiên thấp hơn** nhường. Ưu tiên thấp hơn = công đoạn có độ ưu tiên thấp hơn; bằng nhau thì id lớn hơn nhường. Con nhường lùi vào ô trống gần nhất ngoài đường của con kia. Vẫn kẹt quá **5 giây** → cho đi xuyên nhau (phao, không bao giờ kẹt vĩnh viễn).
   - **Sinh vật rảnh** đứng trên đường của con đang đi phải **tự né** ra khỏi đường đó. Con đang đi không phải chờ con rảnh quá 1 giây.
2. **Rảnh → về quanh nhà + lang thang** (chọn (b)):
   - Sinh vật rảnh ở ngoài **bán kính 4 ô** quanh nhà thì đi về trong bán kính.
   - Trong bán kính: cứ **5–10 giây** (bộ sinh có seed của mô phỏng) chọn ngẫu nhiên một ô đứng được trong bán kính rồi đi tới đó.
   - Có việc → bỏ lang thang ngay. Số liệu là số tạm, đặt trong `sim_m1.json`, chỉnh bằng `game-balance`.
   - Luật "kẹt" của [0017](0017-dinh-tre-thong-bao-nha-mau-va-dem-ket.md) giữ nguyên.
3. **Ô ruộng** (chọn (b)): sinh vật **đi qua** ô ruộng tự do, nhưng **không dừng** trên ô ruộng khi không có việc ở chính ô đó: không làm đích lang thang, không làm chỗ né, không làm chỗ đứng làm việc cho ô khác.

**Planner đề xuất, user xác nhận khi duyệt brief bước 6b:**
- **Đo bán kính:** khoảng cách Chebyshev theo X/Z, tính từ ô gần nhất của khung nhà; không tính Y.
- **Đứng chờ** (chờ 1 giây / nhường) trên ô ruộng được tính là đang đi, không phải "dừng".
- **Ô kề của ô ruộng:** nếu chỉ còn ô ruộng khác để đứng làm việc thì vẫn cho đứng, để không tạo đình trệ mới.
- **Tất định:** xét sinh vật theo thứ tự id. Ô kế tiếp được giữ chỗ trong nhịp.

## Hệ quả
- [simulation-tech](../04-tech/simulation-tech.md#kế-hoạch-prototype-m1): thêm bước 6b (4–6,5 ngày-người). "Khung nhiều ô" trong ghi chú chuyển sang M2 vẫn là phần tìm đường (vùng liên thông, ngân sách A\*); chặn nhau theo khung làm ngay ở 6b.
- [mechanics — Nhà sinh vật](../02-gameplay/mechanics.md#nhà-sinh-vật): sinh vật rảnh đi quanh nhà chuyển từ M2 sang M1; bán kính hoạt động = 4 ô (số tạm).
- [roadmap](../05-roadmap.md): M1 +4–6,5 (chặn nhau 2–4, rảnh quanh nhà 1,5–2, ô ruộng 0,5); M2 −1–2 (phần "sinh vật rảnh đi quanh nhà" đã chuyển sang M1).
- Bước 7: đo 5 ms/nhịp **có** luật chặn nhau và lang thang (60 sinh vật).
- Task contract + brief riêng cho bước 6b, làm sau bước sửa hiển thị (brief 0006 bước 9).
