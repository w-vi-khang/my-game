# Task: M1 simulation bước 6b — luật đi lại: chặn nhau, rảnh quanh nhà + lang thang, không dừng trên ô ruộng

> Trạng thái: user quyết 2026-10-16 ([0019](../../design/decisions/0019-chan-nhau-ranh-quanh-nha-khong-dung-tren-ruong.md)) — chờ viết brief

## Mục tiêu
Bước 6b của [kế hoạch prototype simulation](../../design/04-tech/simulation-tech.md#kế-hoạch-prototype-m1), thêm sau khi user xem SimView (brief 0006). Ước lượng 4–6,5 ngày-người. Xong khi:
- hai sinh vật không bao giờ chiếm cùng ô, trừ lúc dùng phao 5 giây;
- đối đầu ở lối hẹp 1 ô tự gỡ được;
- sinh vật rảnh luôn trở về trong bán kính quanh nhà;
- không sinh vật nào dừng trên ô ruộng mà nó không có việc;
- cùng seed → cùng mã băm từng nhịp.

## Quyết định (user, 2026-10-16 — 0019)
1. **Chặn nhau khi đi:**
   - A\* không tính sinh vật; chỉ kiểm khi sắp bước vào ô (theo cả khung X × Z × Y) có sinh vật khác.
   - Chắn bởi sinh vật đang làm / đang đi → chờ **10 nhịp** → tìm đường khác, coi các con đang chắn là vật cản.
   - Đối đầu ở lối hẹp: ưu tiên công đoạn thấp hơn nhường (bằng nhau → id lớn hơn nhường); con nhường lùi vào ô trống gần nhất ngoài đường của con kia. Kẹt quá **50 nhịp** → đi xuyên (phao).
   - Sinh vật rảnh trên đường của con đang đi → tự né.
2. **Rảnh:** ngoài **4 ô** quanh nhà → đi về. Trong bán kính: mỗi **50–100 nhịp** (`SimRng`) đi tới một ô đứng được ngẫu nhiên trong bán kính. Có việc → bỏ lang thang. Số trong `sim_m1.json`.
3. **Ô ruộng:** đi qua tự do; không dùng làm đích lang thang / chỗ né / chỗ đứng làm việc cho ô khác.

## Planner đề xuất (user xác nhận khi duyệt brief)
- Bán kính = Chebyshev X/Z tính từ ô gần nhất của khung nhà, không tính Y.
- Đứng chờ / nhường trên ô ruộng không tính là "dừng".
- Chỉ còn ô ruộng khác để đứng làm việc thì vẫn cho đứng (không tạo đình trệ mới).
- Tất định: xét sinh vật theo id; ô kế tiếp được giữ chỗ trong nhịp.

## Cách kiểm (test EditMode, `TestWorlds`)
- Hai sinh vật đi ngược chiều trên hành lang 1 ô có chỗ tránh → không chung ô, cả hai tới đích.
- Hành lang 1 ô không chỗ tránh → phao kích hoạt đúng nhịp 50, cả hai tới đích.
- Gặp con đang làm chắn đường → chờ đúng 10 nhịp rồi đi vòng.
- Con rảnh đứng giữa đường → tự né, con đang đi không chờ quá 10 nhịp.
- Rảnh ở xa nhà → về trong bán kính 4; lang thang không bao giờ ra ngoài bán kính, không chọn ô ruộng.
- Mã băm trùng qua 2000 nhịp với 2 lần chạy cùng seed; `SimDemoTests` (map M1) vẫn 0 đình trệ, đủ sản lượng.

## Ngoài phạm vi
- Vùng liên thông, ngân sách A\*, tối ưu (bước 7 đo).
- Hiển thị mới ngoài những gì SimView đã có (trạng thái chờ / nhường chỉ hiện qua nhãn sẵn có nếu cần).
- Nhà tự xây, cửa, nội thất (M2).
