# Handoff

> Trạng thái: đang dở
> Cập nhật: 2026-10-20 19:25

<!-- AI ghi khi context lớn / trước khi /clear / khi user yêu cầu (/ai-workflow-kit:handoff).
File này được commit (theo sang máy khác). Phiên sau dùng xong (đã tóm tắt, user xác nhận bước tiếp theo) -> XOÁ file;
nội dung cũ vẫn xem lại được bằng: git log -p -- .ai/handoff.md
Chỉ ghi sự thật đã xác minh. -->

## Mục tiêu
Task [world bước 7](tasks/m1-world-buoc-7-nhieu-map-minimap.md). Bước 7b2 = brief [0011](briefs/0011-world-nuoc-trong-camera-va-cham.md): nước trong + camera bay không xuyên khối + hiệu ứng dưới nước (scene TerrainView).

## Đã làm (kèm kết quả kiểm chứng)
- Brief 0011 viết `011d8a1`. Executor (Sonnet · medium) làm `39c906b`, kết quả `2dc5558`: EditMode 260/260, PlayMode 15/15 [run, theo brief]. Executor tự sửa công thức kẹp `FlyCameraCollision.MoveAxis` (brief sai) — review chấp nhận.
- Review lần 1 (Opus · medium) `5d2111d`: diff đạt, đúng phạm vi.
- Planner chạy bench điện thoại 19:09 (USB): 43,0 FPS, holeFrames=0, pass30fps=True [run adb logcat], ghi vào brief.
- User xem (2026-10-20): dùng **chuột** (không phải trackpad); nước trong + va chạm khá ổn; **từ dưới nước nhìn lên tối quá**.
- Đã thêm "Sửa sau review 1" (QĐ 10, bước 12–17) vào brief, trạng thái `chờ làm`, commit `64b796f`: mặt dưới mặt nước chiếu sáng theo pháp tuyến hướng lên; sương dưới nước (0,14; 0,44; 0,56); nền dưới nước riêng (0,42; 0,70; 0,82).
- Đã đề xuất thẻ phiên con exec (task_90bbd7b6, Sonnet · medium, `/ai-workflow-kit:exec 0011 --child`) — chờ user bấm.

## Còn lại
1. **Bước tiếp theo:** sửa lần 1 đã exec xong (code `5976a6a`, kết quả `b2b752e`, EditMode 260/260, PlayMode 15/15; trạng thái `chờ review`). Còn: review (`/ai-workflow-kit:review-brief 0011`, Opus · medium; đối chiếu bước 12–17, xem 2 ảnh `client/Logs/water_below.png`, `water_below_up.png`) → user xem lại độ sáng khi nhìn lên.
2. Khi duyệt: ghi dòng 7b2 trong `design/04-tech/world-tech.md` (bảng kế hoạch, "Xong 2026-10-20 [run]" + số liệu bench) và giờ thực tế vào `design/05-roadmap.md` (ước lượng 1–2; brief commit 17:55, executor 17:59–18:32, bench 19:09, sửa lần 1 …).
3. Sau 7b2: 7c minimap (brief mới).

## Quyết định quan trọng & lý do
- Điện thoại luôn cắm USB: AI tự `adb -s PFQCRWVWR46HU4GQ install -r`, mở app, đọc logcat. ADB: `/Applications/Unity/Hub/Editor/6000.4.8f1/PlaybackEngines/AndroidPlayer/SDK/platform-tools/adb`.
- Build APK qua MCP lỗi nếu Editor đang Play (user vừa thử) → `manage_editor stop` trước. Menu Build APK Bench tạo lại Bench → trả `Bench.unity.meta` (guid) + `UnityConnectSettings.asset`.
- `clear_session` phiên con bị auto mode chặn → mỗi lần giao việc mở phiên con mới.

## File liên quan
- `.ai/briefs/0011-world-nuoc-trong-camera-va-cham.md`, `client/Assets/Game/Client/Shaders/Ocean.shader`, `client/Assets/Game/Client/UnderwaterView.cs`, `client/Assets/Game/Core/FlyCameraCollision.cs`, `client/Assets/Game/Client/Editor/TerrainViewCapture.cs`

## Prompt tiếp tục
```text
Tiếp tục công việc đang dở trong .ai/handoff.md — brief 0011 (world 7b2), sửa lần 1 "từ dưới nước nhìn lên tối" đang chờ exec/review.
Bước tiếp theo: nếu brief 0011 ở "chờ review" thì /ai-workflow-kit:review-brief 0011; nếu "chờ làm" thì giao exec.
Đọc handoff và các file liên quan trước, tóm tắt lại cho tôi trong 3 dòng rồi mới làm.
Model đề xuất cho bước này: Opus · medium (AGENTS.md §10.2).
```
