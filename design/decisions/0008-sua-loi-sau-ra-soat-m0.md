# 0008 — Sửa lời văn trong file đã Chốt sau rà soát M0

> Trạng thái: Chốt
> Ngày: 2026-10-13

## Bối cảnh
Rà soát thiết kế (skill `local-design-review`, 2026-10-13) tìm ra chỗ lời văn trong file đã Chốt còn sót từ thiết kế cũ hoặc mâu thuẫn với quyết định đã Chốt. Đây là sửa cho đúng, không đổi thiết kế.

## Các lựa chọn
- Giữ nguyên, ghi chú ở file khác — dễ gây hiểu sai.
- Sửa thẳng, ghi lại ở quyết định này — chọn.

## Quyết định
Chốt (user, 2026-10-13: "sửa hết"):

| File | Trước | Sau | Lý do |
|---|---|---|---|
| [core-loop.md](../02-gameplay/core-loop.md) | Phiên kết thúc khi một loài mới **định cư** | … khi một loài mới **trở lại** | Mô hình ba mức (xuất hiện → ghé thăm → định cư) đã thay bằng danh sách yêu cầu của loài (user, 2026-10-12) |
| [tech-stack.md](../04-tech/tech-stack.md) | Mỗi ô lưu **mật độ** + loại vật liệu | Mỗi ô lưu có/không + loại vật liệu | Đào/đặt theo nguyên ô (user, 2026-10-13) — [world-tech](../04-tech/world-tech.md#dữ-liệu-world) |
| [0003](0003-server.md) (Hệ quả) | Game server "(chưa rõ)" | Game server (cần, sau phát hành) | Phần Quyết định của 0003 đã ghi là cần |
| [00-vision.md](../00-vision.md) | Không ghi lý do hai cơ chế không gắn pillar; mục "Không làm" không nhắc ngoại lệ 54/60 | Ghi hai ngoại lệ (tiền tệ & cửa hàng, điều khiển PC + mobile) và ngoại lệ giới hạn sinh vật của [0004](0004-tien-nap.md) | Khớp bảng cơ chế và 0004 |
| [00-vision.md](../00-vision.md) (Ràng buộc) | ~11,2–21,0 năm | ~11,7–22,2 năm (sau đó ~11,8–22,3 theo [0009](0009-dai-duong-va-suong-mu-o-ria-map.md)) | Số dẫn từ roadmap, cập nhật theo [0007](0007-m1-toi-gian.md) và các ước lượng thêm khi rà soát |

## Hệ quả
Không đổi số liệu hay phạm vi. Các sửa khác của cùng đợt rà soát nằm ở file chưa Chốt (Nháp / Đang xem xét), không cần quyết định.
