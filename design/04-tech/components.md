# Thành phần hệ thống

> Trạng thái: Nháp
> Liên quan: [Kiến trúc](architecture.md) · [Công nghệ](tech-stack.md) · [Cơ chế](../02-gameplay/mechanics.md) · [Lộ trình](../05-roadmap.md)

Game KHÔNG chỉ là client Unity. File này xác định game cần những thành phần nào, mỗi thành phần chịu trách nhiệm gì, và cái nào thật sự cần cho từng milestone. Quyết định ở đây kéo theo phần lớn công sức của một người làm — cân nhắc kỹ.

## Câu hỏi xác định thành phần
<!-- Trả lời từng câu (có/không/chưa biết + lý do từ thiết kế). Mỗi "có" thường kéo theo một thành phần hoặc dịch vụ.
- Chơi offline hoàn toàn được không? Cần lưu tiến trình trên cloud / đồng bộ nhiều thiết bị?
- Có tài khoản, đăng nhập?
- Có chơi nhiều người (realtime / bất đồng bộ), bảng xếp hạng, bạn bè, chat?
- Có nội dung cập nhật sau phát hành (live ops, sự kiện, cấu hình từ xa)?
- Có thanh toán trong game (cần xác thực giao dịch phía server)?
- Có chống gian lận (logic quan trọng phải chạy ở server)?
- Cần công cụ nội bộ (editor level, nhập dữ liệu cân bằng, trang quản trị)?
- Cần thu thập số liệu (analytics), báo lỗi (crash report)?
- Có website / trang cộng đồng?
-->
_(chưa có)_

## Danh sách thành phần
<!-- Bảng: thành phần | trách nhiệm | công nghệ | tự làm hay dùng dịch vụ có sẵn | cần từ milestone | trạng thái (cần / không cần / chưa rõ).
Với một người làm: ưu tiên dịch vụ có sẵn (BaaS) cho thứ không tạo khác biệt; ghi rõ là đề xuất và chi phí. -->

| Thành phần | Trách nhiệm | Công nghệ | Tự làm / dịch vụ | Cần từ | Trạng thái |
|---|---|---|---|---|---|
| Client | Phần người chơi chạy: gameplay, hiển thị, âm thanh, input | Unity 6000.4.8f1 ([0001](../decisions/0001-client-unity.md)) | Tự làm | M1 | Cần |

## Ranh giới trách nhiệm
<!-- Logic nào chạy ở client, logic nào ở server/dịch vụ (vd: tính sát thương, phần thưởng, giá). Quyết định này ảnh hưởng chống gian lận và chơi offline. -->
_(chưa có)_
