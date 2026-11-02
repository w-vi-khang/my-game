# Handoff — 2026-11-02 (Planner, task cảm ứng + chờ chơi thử 5j/5k)

## Đang làm — task cảm ứng (ưu tiên)
Task [m1-dieu-khien-cam-ung](tasks/m1-dieu-khien-cam-ung.md). User xác nhận ngày 2026-11-02, gồm kiểu joystick + tâm nhắm, phạm vi đủ vòng lặp 30 giây, và cả 5 đề xuất của Planner. World-tech có dòng 7m.
- **Brief [0101](briefs/0101-cam-ung-joystick-camera-nhay.md) (a) đã duyệt.** Code `910c1b9`, review `ee9e223`, Executor ~18 phút.
  - Đã có: `Game.Core.TouchGestures` (`Update(samples, screenWidth, dpi, buttons)`, `IsHeld` / `WasPressed(buttonId)`) và `Game.Client.TouchControls` (`WantsTouch`, `Active`, `SetForceOn`, `Feed`, `JumpButtonId = 1`, canvas sortingOrder 8, không scaler, đơn vị px).
  - Nút Nhảy ở (safe.xMax − 2,4·d, safe.yMin + 0,9·d), chừa góc dưới-phải cho nút hành động chính.
  - Số tạm trong `sim_m1.json` player: `touch*`.
- **Việc tiếp: viết brief 0102 (b)** theo contract mục Mục tiêu và đề xuất 3–5:
  - **Tâm nhắm giữa màn hình:**
    - Tia từ camera qua tâm màn hình, bỏ qua collider nhân vật.
    - Lớp đỏ / bản mờ / vòng tầm như PC.
    - Nút hành động chính gọi `TerrainEditInput.RightClickAt(hasCell, digCell, placeCell, drop, storageId)` (`client/Assets/Game/Client/TerrainEditInput.cs:256`). Hiện tại đích lấy theo con trỏ ở `:115-141`, cần tách phần tính đích ra để dùng tâm màn hình khi `TouchControls.WantsTouch`.
    - Ô ruộng ở chế độ Mặc định: chín thì thu hoạch, chưa chín thì mở bảng ô ruộng.
  - **Nút** (thêm `TouchRect` id 2+ vào `TouchControls`): thanh 4 chế độ dọc mép phải (`ToggleMode`), Thu (`CollectNearby`), Túi, Huỷ/Menu (như Esc).
  - **Chạm trong bảng:** túi phụ, túi/kho kéo thả, bảng ô ruộng, bản đồ, menu phép. Các bảng hiện đọc `GameInput.Point` / `MoveClick` (chỉ bind `<Mouse>`), nên trên điện thoại không có con trỏ.
    - Cách đề xuất: thêm binding `<Touchscreen>/primaryTouch/position` và `/press` vào action map `Pointer` / `Map` / `TimeDial` và `MoveClick`. Planner tra docs Input System 1.19 rồi mới quyết.
    - Lưu ý xung đột: chạm thế giới không được thành click PC. `PlayerCharacter` đã bỏ `ReadPointerMove` khi `WantsTouch`; `TerrainEditInput` thì chưa.
  - Rồi brief 0103 (c): build APK, cài qua USB, logcat, `adb screencap`, cập nhật controls-ui (cột Cảm ứng), glossary (dòng "Hành động chính": PC là chuột phải), ideas (phần ngoài phạm vi), roadmap (giờ thực tế).
- Phiên Executor `exec-0101` = `local_138af040-4e52-4925-8f6f-73763c50442b` (Sonnet · medium), con của phiên này. Phiên mới không clear được phiên này, nên mở phiên con mới.

## Chờ user chơi thử (5j, 5k)
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
     - Brief [0098](briefs/0098-world-5k-huong-chay-main-flow.md) (Core: `TryGetMainFlow` + `WorldData.FlowDirs`) **đã duyệt** (code `5e47f57`, 2026-11-02).
     - Brief [0099](briefs/0099-world-5k-vet-chay.md) (mesher luồng TEXCOORD0 + shader vệt) **đã duyệt** (code `6c93a1c`, ~1 giờ). User xem (a)–(e) gộp cuối 5k.
   - **Chơi thử lần 1 (user 2026-11-02): lỗi 5k-1 (bọt gấp khúc theo bậc ô) + 5k-2 (quá bóng)**, ghi ở task 5k mục "Lỗi chơi thử". User chọn "giảm mạnh, giữ nhẹ" → brief [0100](briefs/0100-world-5k-bot-mem-giam-bong.md) **đã duyệt** (code `eec15c7`, ~27 phút): bọt dùng soft-min khoảng cách bờ ở kênh B (`WaterFoamSoftK` 0,5), 1 dải mềm; glint 0,3, halo 0,25, sparkle 0,4/0,2, vệt chảy 0,18. User xem lại theo "User tự test" của 0100 (a)–(e), kèm các mục 0095–0099 còn lại.
   - **Code 4 phần 5k đã xong.** Việc tiếp: user chơi thử gộp 5k (mục "User tự test" của 0095, 0096, 0097, 0099), kèm chơi thử gộp 5j còn lại. Đạt → đóng 5k, cập nhật world-tech dòng 5k + roadmap (giờ thực tế: 0095 ~20 phút, 0096 ~16 phút, 0097 ~11 phút, 0098 ?, 0099 ~1 giờ, 0100 ~27 phút).
   - Ảnh Executor chụp SimView chỉ có góc mặc định → khi user chơi thử nên tự xem bờ biển; hoặc brief sau thêm cách đặt camera.
   - Phiên Executor `child-sonnet-medium` = `local_9e128054-0b43-4e81-9baa-a03a3d793a17` (Sonnet · medium, làm 0099).

## Ghi chú
- Phiên Executor `exec-0088` = `local_b7728311-1023-4b48-a428-fa77c2f00dbe` (Opus · medium). Dọn bằng `clear_session` trước mỗi brief mới.
- Số tạm: `basinFillRadius` 16, `LipTaperLength` 0,5. User chưa nhận xét hai số này.
- Ảnh tham khảo nước (5k) là game của người khác. Chưa có tên/nguồn, không ghi tên game khi chưa có nguồn.
