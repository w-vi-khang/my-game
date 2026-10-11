# Rủi ro

> Trạng thái: Nháp
> Liên quan: [Lộ trình](../05-roadmap.md)

## Danh sách rủi ro
<!-- Bảng: rủi ro | khả năng | ảnh hưởng | cách giảm | kiểm chứng sớm ở milestone nào. Gồm cả rủi ro không kỹ thuật (thời gian, động lực khi làm một mình). -->
Đánh giá của AI, [chưa xác minh] — kiểm chứng bằng prototype.

| Rủi ro | Khả năng | Ảnh hưởng | Cách giảm | Kiểm chứng ở |
|---|---|---|---|---|
| Hiệu năng voxel (meshing, chunk) trên mobile | Cao | Cao | Map có ranh giới; prototype trên máy mobile yếu sớm | M1 |
| Tìm đường trên địa hình đào/đặt được (cập nhật khi đổi khối) | Trung bình | Cao | Chỉ tính lại vùng bị đổi; giới hạn số sinh vật | M1 |
| Kết quả tua nhanh lệch với lúc chơi trực tiếp | Trung bình | Trung bình | Một mô hình mô phỏng chung cho cả hai ([cơ chế](../02-gameplay/mechanics.md)) | M1–M2 |
| Yêu cầu môi trường sống thành việc vặt, không vui | Trung bình | Cao | Graybox thử 2–3 loài trước khi làm nhiều | M1 |
| Công nội dung tăng theo số loài (model, animation, kỹ năng nhập hồn) | Cao | Cao | Chọn art style rẻ; giới hạn loài nhập hồn được | M2 |
| Server là việc vận hành dài hạn cho một người (chi phí, bảo trì, sự cố) | Cao | Cao | Ưu tiên dịch vụ có sẵn; M2 chỉ dùng backend | M2 |
| Game online: backend sập = không ai chơi được | Trung bình | Cao | Dịch vụ có sẵn có SLA; client chịu được mất kết nối ngắn | M2 |
| Nhà cung cấp dịch vụ ngừng sản phẩm (vd Unity ngừng Multiplay Game Server Hosting từ 31/03/2026 — [status.unity.com](https://status.unity.com/info_notices/362941)) | Thấp–Trung bình | Cao | Bọc lớp gọi dịch vụ trong client; ưu tiên lựa chọn tự host được | M2 |
| Dung lượng lưu world voxel trên backend tăng theo người chơi | Trung bình | Trung bình | Chỉ lưu phần khác với world sinh từ seed | M2 |
| Ghép trận cần đủ người chơi online cùng cấp độ; ít người thì chờ lâu | Trung bình | Cao | Mở PvP sau khi có lượng người chơi; [ý tưởng] mở rộng khoảng ghép theo thời gian chờ | Sau M4 |
| Netcode chiến đấu thời gian thực (độ trễ, đồng bộ nhập hồn, phép) trên cả PC và mobile | Cao | Cao | Tách logic chiến đấu khỏi hiển thị từ sớm; prototype trận mạng nhỏ trước khi làm PvP đầy đủ | M3 |
| Chi phí hosting game server tăng theo số trận | Trung bình | Trung bình | Tra giá hosting trước khi làm PvP; PvP có điều kiện tham gia | Sau M4 |
| Rút thưởng và tăng cơ hội bằng tiền nạp có thể chịu quy định về loot box (công bố tỉ lệ, giới hạn độ tuổi...) ở một số thị trường [chưa xác minh] | Trung bình | Cao | Tra quy định của thị trường mục tiêu và cửa hàng ứng dụng trước khi làm cửa hàng | M2 |
| Một bộ điều khiển cho cả PC và mobile | Trung bình | Trung bình | Thiết kế điều khiển cảm ứng từ M1 | M1 |
| Dự án dài hạn một người: mất động lực | Trung bình | Cao | Milestone nhỏ có thứ chơi được; công bố tiến độ | Liên tục |
