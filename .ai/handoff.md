# Handoff — 2026-11-01 (Planner, task 5j + 5k, cập nhật sau khi duyệt brief 0095)

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
   - **Brief [0095](briefs/0095-world-5k-mau-theo-do-sau-vien-bo.md) (phần 1) đã duyệt** (code `57d63af` + `038fce4`, ~20 phút). User xem (a)–(e) gộp cuối 5k.
   - Tiếp: Planner Opus thiết kế + viết brief **phần 2 (bọt bờ)**, dùng `shoreD` (kênh R, ≤ 4 ô) trong `Ocean.shader`; thêm tham số vào `water_m1.json` "look". Sau phần 2 (hoặc cuối 5k): đo FPS điện thoại.
   - Phiên Executor `exec-0095` = `local_0ec3ce58-e317-4e1d-a15a-196acea4f127` (Sonnet · medium).

## Ghi chú
- Phiên Executor `exec-0088` = `local_b7728311-1023-4b48-a428-fa77c2f00dbe` (Opus · medium). Dọn bằng `clear_session` trước mỗi brief mới.
- Số tạm: `basinFillRadius` 16, `LipTaperLength` 0,5. User chưa nhận xét hai số này.
- Ảnh tham khảo nước (5k) là game của người khác. Chưa có tên/nguồn, không ghi tên game khi chưa có nguồn.
