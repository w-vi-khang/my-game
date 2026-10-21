# Task: M1 world bước 7f — buổi trong ngày + chọn thời điểm

> Trạng thái: user quyết 2026-10-21 ([0024](../../design/decisions/0024-buoi-trong-ngay-va-chon-thoi-diem.md)). Ước lượng 4–6 ngày-người [ý tưởng]. Chia brief; mỗi brief viết sau khi brief trước được duyệt.

## Mục tiêu
Bầu trời đi theo chu kỳ 24 giờ thực; người chơi chọn 1 trong 4 mốc (giữa đêm, bình minh, giữa trưa, hoàng hôn) → bầu trời xoay tới mốc theo chiều tiến; đồng hồ mô phỏng không đổi; điều kiện môi trường sống có loại "buổi". Xong khi: các test ở [world-tech 7f](../../design/04-tech/world-tech.md) đạt + user xem thấy xoay đúng chiều, mượt trên Mac và điện thoại.

## Ngoài phạm vi
Loài ngày/đêm, cây/tốc độ theo buổi, lưu offset, giờ server, thời tiết, nối vào bộ môi trường sống (chưa có code).

## Đề xuất chia brief (Planner chốt khi viết)
1. **7f-1 lõi (EditMode, Sonnet):** `Game.Core` — `TimeOfDay` (nguồn giờ tiêm được, offset, pha số nguyên theo ms, `SelectLandmark`, cung xoay chiều tiến, buổi + biên qua nửa đêm), `time_m1.json`, loại điều kiện buổi; test gồm hash sim không đổi.
2. **7f-2 bầu trời (PlayMode + user xem):** mặt trời/ánh sáng/ambient/sương/đại dương/dưới nước theo pha; xoay 3 giây ease; cần tra docs Unity 6000.4 (URP skybox, `RenderSettings`, `Light`).
3. **7f-3 vòng chọn + nhãn buổi (PlayMode + user xem):** vòng 4 biểu tượng kiểu ảnh tham khảo, phím/nút mở, chuột + cảm ứng; đo FPS điện thoại.
