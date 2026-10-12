# Rủi ro

> Trạng thái: Nháp
> Liên quan: [Lộ trình](../05-roadmap.md)

## Danh sách rủi ro
<!-- Bảng: rủi ro | khả năng | ảnh hưởng | cách giảm | kiểm chứng sớm ở milestone nào. Gồm cả rủi ro không kỹ thuật (thời gian, động lực khi làm một mình). -->
Đánh giá của AI, [chưa xác minh] — kiểm chứng bằng prototype.

| Rủi ro | Khả năng | Ảnh hưởng | Cách giảm | Kiểm chứng ở |
|---|---|---|---|---|
| Hiệu năng voxel trên mobile: bề mặt mịn liền (thuật toán dựng mesh mịn, nhiều tam giác hơn khối, trộn vật liệu; dựng mesh ×2 → ~24.000 tam giác mỗi mảnh 30 × 30 ô, gấp 4 lần ×1 — thử nghiệm art 2026-10-12) + model mịn cho sinh vật/cây/vật phẩm + biến thể hiển thị → nhiều polygon/draw call hơn | Cao | Cao | Map có ranh giới; prototype trên máy mobile yếu sớm | M1 |
| Tìm đường trên địa hình đào/đặt được (cập nhật khi đổi khối) | Trung bình | Cao | Chỉ tính lại vùng bị đổi; giới hạn số sinh vật | M1 |
| Kết quả tua nhanh lệch với lúc chơi trực tiếp | Trung bình | Trung bình | Một mô hình mô phỏng chung cho cả hai ([cơ chế](../02-gameplay/mechanics.md)) | M1–M2 |
| Yêu cầu môi trường sống thành việc vặt, không vui | Trung bình | Cao | Graybox thử 2–3 loài trước khi làm nhiều | M1 |
| Công nội dung tăng theo số loài (model, animation, kỹ năng nhập hồn) | Cao | Cao | Chọn art style rẻ; giới hạn loài nhập hồn được | M2 |
| Server là việc vận hành dài hạn cho một người (chi phí, bảo trì, sự cố) | Cao | Cao | Ưu tiên dịch vụ có sẵn; M2 chỉ dùng backend | M2 |
| Game online: backend sập = không ai chơi được | Trung bình | Cao | Dịch vụ có sẵn có SLA; client chịu được mất kết nối ngắn | M2 |
| Nhà cung cấp dịch vụ ngừng sản phẩm (vd Unity ngừng Multiplay Game Server Hosting từ 31/03/2026 — [status.unity.com](https://status.unity.com/info_notices/362941)) | Thấp–Trung bình | Cao | Bọc lớp gọi dịch vụ trong client; ưu tiên lựa chọn tự host được | M2 |
| Dung lượng lưu world voxel trên backend tăng theo người chơi | Trung bình | Trung bình | Chỉ lưu phần khác với world sinh từ seed | M2 |
| Cân bằng chỉ số sinh vật: nhiều tham số (base loài, hệ số quy đổi, trần, phân bố ngẫu nhiên, vượt trội) × số loài | Cao | Cao | Bảng dữ liệu tách khỏi code; thử với 3–5 loài trước; trần cho mọi chỉ số | M2 |
| Ghép trận cần đủ người chơi online cùng cấp độ; ít người thì chờ lâu. Điểm cấp độ/bậc vẫn tính trong PvP → người mới qua ngưỡng chênh với người chơi lâu năm; ghép theo hạng rồi cấp độ trung bình làm nhóm ghép nhỏ hơn | Trung bình | Cao | Mở PvP sau khi có lượng người chơi; [ý tưởng] mở rộng khoảng ghép theo thời gian chờ | Sau M4 |
| Netcode chiến đấu thời gian thực (độ trễ, đồng bộ nhập hồn, phép) trên cả PC và mobile | Cao | Cao | Tách logic chiến đấu khỏi hiển thị từ sớm; prototype trận mạng nhỏ trước khi làm PvP đầy đủ | M3 |
| Sản lượng nông trại do client tính → sửa client để gian lận tài nguyên, đổi ra tiền trong game/rút thưởng | Trung bình | Trung bình | [ý tưởng] Backend kiểm tra hợp lý theo thời gian × số sinh vật × thuần thục ([kiến trúc](architecture.md#dữ-liệu)) | M2 |
| Kiểm duyệt ảnh concept phụ thuộc Space HF bên thứ ba (Qwen3-VL của prithivMLmods) và quota ZeroGPU; Space chính thức Qwen lỗi khi gọi API [run 2026-10-12] | Trung bình | Thấp | Lớp 1 (soi vùng) không phụ thuộc dịch vụ; đổi Space trong `concept_check.py`; khi sản xuất (M2) cân nhắc VLM trả phí hoặc chạy local | M2 |
| Chi phí hosting game server tăng theo số trận | Trung bình | Trung bình | Tra giá hosting trước khi làm PvP; PvP có điều kiện tham gia | Sau M4 |
| Rút thưởng và tăng cơ hội bằng tiền nạp có thể chịu quy định về loot box (công bố tỉ lệ, giới hạn độ tuổi...) ở một số thị trường [chưa xác minh] — thị trường toàn cầu → nhiều quy định khác nhau | Trung bình | Cao | Tra quy định của thị trường mục tiêu và cửa hàng ứng dụng trước khi làm cửa hàng; đã có thiết kế công khai tỉ lệ và mốc bảo hiểm ([cơ chế](../02-gameplay/mechanics.md#nhận-sinh-vật)) — đủ hay chưa thì [chưa xác minh] | M2 |
| Một bộ điều khiển cho cả PC và mobile | Trung bình | Trung bình | Thiết kế điều khiển cảm ứng từ M1 | M1 |
| Trùng ý tưởng với Pokémon Pokopia (Switch 2, 2026 — [Nintendo](https://www.nintendo.com/en-gb/Games/Nintendo-Switch-games/Game-2915161.html)): hồi sinh vùng đất khô héo, sinh vật trở lại, dùng chiêu của sinh vật để đổi thế giới → người chơi thấy game "giống Pokopia" | Cao | Cao | Nhấn mạnh điểm khác biệt user đã chọn ([vision](../00-vision.md)): nhiều cá thể mỗi loài, chiến đấu và nông trại chi tiết hơn; tránh mượn art/tên quá gần (art style tham khảo Pokopia → cần bảng màu, hình dáng sinh vật riêng) | M0 |
| Dự án dài hạn một người: mất động lực. Ở 10 giờ/tuần, phạm vi hiện tại tới M4 ~10,4–19,5 năm ([roadmap](../05-roadmap.md#kiểm-tra-phạm-vi)) | Cao | Cao | Milestone nhỏ có thứ chơi được; công bố tiến độ | Liên tục |
