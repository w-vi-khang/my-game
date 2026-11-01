# Handoff — 2026-11-01 (Planner, task 5j + 5k, cập nhật sau khi viết brief 0095)

## Đang làm
Task [m1-world-5j-sua-loi-choi-thu](tasks/m1-world-5j-sua-loi-choi-thu.md). User chơi thử ngày 2026-11-01: **phần nước đạt**. Lỗi 13 (cột thác bo góc kề khối đặc) đã xong qua brief 0092, review đạt ở commit `263509e`. Còn lỗi 14–16, ghi trong task ở commit `b1101d3`.

## Việc tiếp
1. **5j:** lỗi 14–16 đã duyệt (0093 `945b006`, 0094 `b87d5df`).
   - Còn: **user chơi thử gộp** mép cỏ / mesh nhọn (lỗi 10, 14, 15). Danh sách cần xem ở mục "User tự test" của brief 0094.
   - Review 0094 lưu ý: mấu nhọn nhỏ tối ở chỗ dải ô hẹp gặp vách (`art-test/shots/b0094_after_2.png`). Có từ trước 0094; nếu user còn thấy thì ghi thành lỗi mới.
   - User đạt → đóng task 5j, cập nhật world-tech dòng 5j + roadmap (giờ thực tế).
2. **5k** — task contract [.ai/tasks/m1-world-5k-nuoc-kieu-anh-tham-khao.md](tasks/m1-world-5k-nuoc-kieu-anh-tham-khao.md) **đã xác nhận** (user 2026-11-01):
   - (a) đúng bản nháp; (b) cắt phần 5 (bọt chân thác + mép tràn) sang M2, còn 4 phần 5–9 ngày; (c) "nét mềm" đúng như Planner hiểu, chỉ ở shader.
   - Đã sửa roadmap (5k 5–9, M1 136,5–223,5), world-tech `:141`/`:200`, ideas `:71`, glossary (+5 thuật ngữ).
   - **Brief [0095](briefs/0095-world-5k-mau-theo-do-sau-vien-bo.md) (phần 1, Sonnet · medium) chờ Executor.** Khoảng cách tới bờ ở kênh R màu đỉnh, lề 4 khi đào/đặt (`SolidChanged`), shader smoothstep + viền mờ, tham số `water_m1.json` "look".
   - Tiếp: Executor làm 0095 → Planner review → brief phần 2 (bọt bờ, dùng `shoreD`). FPS điện thoại đo gộp cuối 5k (hoặc sau phần 2).

## Ghi chú
- Phiên Executor `exec-0088` = `local_b7728311-1023-4b48-a428-fa77c2f00dbe` (Opus · medium). Dọn bằng `clear_session` trước mỗi brief mới.
- Số tạm: `basinFillRadius` 16, `LipTaperLength` 0,5. User chưa nhận xét hai số này.
- Ảnh tham khảo nước (5k) là game của người khác. Chưa có tên/nguồn, không ghi tên game khi chưa có nguồn.
