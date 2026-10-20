# Handoff

> Trạng thái: đang dở
> Cập nhật: 2026-10-20 19:45

<!-- AI ghi khi context lớn / trước khi /clear / khi user yêu cầu (/ai-workflow-kit:handoff).
File này được commit (theo sang máy khác). Phiên sau dùng xong (đã tóm tắt, user xác nhận bước tiếp theo) -> XOÁ file;
nội dung cũ vẫn xem lại được bằng: git log -p -- .ai/handoff.md
Chỉ ghi sự thật đã xác minh. -->

## Mục tiêu
Task [world bước 7](tasks/m1-world-buoc-7-nhieu-map-minimap.md). Brief [0011](briefs/0011-world-nuoc-trong-camera-va-cham.md) (7b2 nước trong + camera va chạm) đang được chốt. Việc tiếp: **viết brief 0012** cho 3 yêu cầu mới của user (2026-10-20, khi xem 0011).

## Đã làm (kèm kết quả kiểm chứng)
- Brief 0011 xong 2 vòng: code `39c906b` + sửa lần 1 `5976a6a` (mặt dưới mặt nước sáng hơn). EditMode 260/260, PlayMode 15/15 [run, theo brief]. Bench Xiaomi 43,0 FPS, holeFrames=0 [run adb 19:09].
- User xem độ sáng: "Độ sáng ổn rồi". Đã nhắn phiên review `local_2f0adf0a-…` (child-opus-medium) chốt 0011 = đã duyệt + ghi dòng 7b2 vào world-tech + giờ vào roadmap. **Kiểm lại** trong `git log` / trạng thái brief 0011 trước khi làm tiếp; nếu chưa có thì tự chốt (review-brief 0011).

## Còn lại — brief 0012 (user 2026-10-20, đã trả lời câu hỏi)
1. **Biên world (user chốt):** camera không ra khỏi world. Trong **2 ô cuối** trước mép world theo x/z (0 và Size.X/Size.Z), tốc độ bay hướng ra ngoài **giảm dần về 0** + **sương dày lên che hết** → không có cảm giác đụng vách cứng. **Trần bay = đỉnh world + 16 ô** (world v3 cao 48 → y tối đa 64), cũng chậm dần ở 2 ô cuối. Chỗ cần Planner quyết: cách làm sương dày lên (vd UnderwaterView-like: fog end lerp theo khoảng cách tới mép), vị trí logic (Game.Core `FlyCameraCollision` thêm giới hạn?), test.
2. **Góc nhìn trên HUD (user chốt):** chỉ thêm UI hiện góc ngẩng/cúi của camera: 0° = ngang mặt biển, nhìn lên dương, nhìn xuống âm. Chuột giữa vẫn chỉnh góc như hiện tại (không đổi điều khiển). User dùng **chuột** (không phải trackpad). Gợi ý: thêm vào dòng HUD của `TerrainEditInput` (dòng hướng dẫn ~`:110`), lấy góc = −pitch Euler của camera.
3. **Mép cỏ nhìn từ dưới bị hở (user xác nhận):** từ dưới nước nhìn lên, dải mép cỏ / váy cỏ (`BlockMesher`, hằng `LipOverhang 0.08`, `LipDepth 0.22` quanh `World/BlockMesher.cs:30-45`) trông như tờ giấy mỏng. Sửa: **đóng mặt dưới của mép cỏ** để nhìn từ dưới thấy như khối có độ dày; nhìn từ trên không đổi. Ảnh: `client/Logs/water_below_up.png`. Cần Planner đọc code mép cỏ trong `BlockMesher` (Burst job) để viết bước; chú ý test mesh cũ (seam, incremental rebuild) và số tam giác/hiệu năng điện thoại.
4. Ghi vào design: dòng **7b3** trong bảng kế hoạch `design/04-tech/world-tech.md` (ước lượng công, [ý tưởng] tới khi xong) + roadmap M1 cộng ước lượng (AGENTS.local: mọi tính năng mới có ước lượng + milestone).
5. **Ý tưởng user (để sau, chưa làm):** world như hành tinh tròn — đi tận cùng phía bắc ra phía nam, tận cùng phía đông ra phía tây và ngược lại; world tăng kích thước thì điểm tận cùng đổi theo; **chỉ áp dụng khi người chơi đã hoàn thành hết các map**. Ghi vào `design/ideas.md` (đánh dấu ý tưởng của user, ảnh hưởng: bộ sinh world, nạp dần chunk, minimap, tìm đường, mô phỏng nước) — không có ước lượng/milestone tới khi user chốt.
6. Sau 0012: 7c minimap.

## Quyết định quan trọng & lý do
- Điện thoại luôn cắm USB: AI tự `adb -s PFQCRWVWR46HU4GQ install -r`, mở app, đọc logcat. ADB: `/Applications/Unity/Hub/Editor/6000.4.8f1/PlaybackEngines/AndroidPlayer/SDK/platform-tools/adb`.
- Build APK qua MCP lỗi nếu Editor đang Play → `manage_editor stop` trước. Menu Build APK Bench / Tạo scene Bench tạo lại Bench → trả `Bench.unity.meta` (guid) + `UnityConnectSettings.asset`.
- `clear_session` phiên con bị auto mode chặn → mỗi lần giao việc mở phiên con mới.
- Brief 0011 bài học: công thức kẹp của Planner sai (camera tới đúng mặt ô trước bước bị chặn) — khi viết công thức số học trong brief, tự chạy tay một ví dụ biên.

## File liên quan
- `client/Assets/Game/Core/FlyCameraCollision.cs`, `client/Assets/Game/Client/DebugFlyCamera.cs`, `client/Assets/Game/Client/UnderwaterView.cs`, `client/Assets/Game/Client/TerrainEditInput.cs`, `client/Assets/Game/World/BlockMesher.cs`, `client/Assets/Game/Client/Editor/TerrainViewCapture.cs`

## Prompt tiếp tục
```text
Tiếp tục công việc đang dở trong .ai/handoff.md — brief 0011 (world 7b2) đang chốt; việc tiếp là viết brief 0012 cho 3 yêu cầu mới của user (biên world chậm dần + sương, góc nhìn trên HUD, đóng mặt dưới mép cỏ) và ghi ý tưởng world hành tinh vào ideas.md.
Bước tiếp theo: kiểm 0011 đã "đã duyệt" chưa, rồi /ai-workflow-kit:brief world 7b3 — biên world + góc nhìn HUD + mép cỏ kín.
Đọc handoff và các file liên quan trước, tóm tắt lại cho tôi trong 3 dòng rồi mới làm.
Model đề xuất cho bước này: Opus · high (thiết kế cách làm sương biên + mép cỏ, AGENTS.md §10.2).
```
