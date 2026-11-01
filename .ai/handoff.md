# Handoff — 2026-11-01 (Planner, task 5j + 5k, cập nhật sau khi duyệt brief 0096)

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
   - **Brief [0096](briefs/0096-world-5k-bot-bo.md) (phần 2, bọt bờ) đã duyệt** (code `4e8c10d`, ~16 phút). User xem (a)–(d) gộp cuối 5k.
   - **User 2026-11-01: gác đo FPS điện thoại**, hoàn thiện 4 phần trên máy tính trước.
   - **Brief [0097](briefs/0097-world-5k-lap-lanh.md) (phần 3, lấp lánh) đã duyệt** (code `41c40ad`, ~11 phút). User xem (a)–(e) gộp cuối 5k.
   - **Phần 4 (vệt chảy)** — user 2026-11-02: luật main_flow (con C cùng hướng cha→B, không có thì null; vật nổi M2), vệt vẽ theo hướng cha→nó. Đã ghi world-tech `:141`, glossary, ideas.
     - Brief [0098](briefs/0098-world-5k-huong-chay-main-flow.md) (Core: `TryGetMainFlow` + `WorldData.FlowDirs`) — Executor xong (code `5e47f57`, kết quả `289623e`), **chờ Planner review**.
     - Brief [0099](briefs/0099-world-5k-vet-chay.md) (mesher luồng TEXCOORD0 + shader vệt) — làm **sau khi 0098 duyệt**.
   - Ảnh Executor chụp SimView chỉ có góc mặc định → khi user chơi thử nên tự xem bờ biển; hoặc brief sau thêm cách đặt camera.
   - Phiên Executor `exec-0095` = `local_0ec3ce58-e317-4e1d-a15a-196acea4f127` (Sonnet · medium).

## Ghi chú
- Phiên Executor `exec-0088` = `local_b7728311-1023-4b48-a428-fa77c2f00dbe` (Opus · medium). Dọn bằng `clear_session` trước mỗi brief mới.
- Số tạm: `basinFillRadius` 16, `LipTaperLength` 0,5. User chưa nhận xét hai số này.
- Ảnh tham khảo nước (5k) là game của người khác. Chưa có tên/nguồn, không ghi tên game khi chưa có nguồn.
