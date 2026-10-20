# Handoff

> Trạng thái: đang dở
> Cập nhật: 2026-10-20 16:45

<!-- AI ghi khi context lớn / trước khi /clear / khi user yêu cầu (/ai-workflow-kit:handoff).
File này được commit (theo sang máy khác). Phiên sau dùng xong (đã tóm tắt, user xác nhận bước tiếp theo) -> XOÁ file;
nội dung cũ vẫn xem lại được bằng: git log -p -- .ai/handoff.md
Chỉ ghi sự thật đã xác minh. -->

## Mục tiêu
Task [world bước 7](tasks/m1-world-buoc-7-nhieu-map-minimap.md) (quyết định 0022). Bước 7b = brief [0010](briefs/0010-world-nap-dan-chunk.md): nạp dần chunk quanh camera, scene TerrainView/Bench sang world v3.

## Đã làm (kèm kết quả kiểm chứng)
- Brief 0010 viết (`e6f4bf2`). User chốt: nạp ≤ 128 ô, gỡ > 160, 2 cột/khung; sương 64–120, far clip 128; SimView giữ v2 tới 7e; bench bay tự động không đào; ghim mảng ô thay vì chép 14 MB.
- Executor (Sonnet · medium) làm xong, trạng thái **chờ review**: code `0565002`, kết quả `d80cdf3`. EditMode 253/253, PlayMode 12/12 [run, theo brief].
- Planner tự đo điện thoại qua USB (`9e32836`): 42,8 FPS trung bình, p99 41,6 ms, 0 khung lỗ, buildMsMax 240 ms, 1,5 triệu tam giác, sysMem 810 MB, mã băm v3 trùng golden [run adb logcat].
- Planner tự test TerrainView qua MCP (`b0b530d`): bay 25 s ở 30 ô/s → 9.691 khung, 5,3 ms/khung, 0 lỗ; ảnh `client/Logs/screenshot-20261020-164151.png` ổn [run MCP]. Bench Editor 6,3 FPS là do Unity không ở foreground (khung đứng), không phải code.

## Còn lại
1. **Bước tiếp theo:** review brief 0010 bằng `/ai-workflow-kit:review-brief 0010` (đầy đủ: đọc diff `0565002`, đối chiếu quyết định, checklist; ghi dòng 7b + giờ thực tế vào `design/04-tech/world-tech.md` và `design/05-roadmap.md`).
2. User báo "không điều khiển được" camera bay trong TerrainView. Planner kiểm: phím W chạy khi Game view có focus. Nguyên nhân khả dĩ [chưa xác minh với user]: phải bấm vào Game view trước (Input System `PointersAndKeyboardsRespectGameViewFocus`); xoay cần chuột giữa — trackpad Mac không có. Hỏi user dùng chuột hay trackpad; nếu cần đổi điều khiển (vd chuột phải/trackpad để xoay) → việc riêng, ngoài brief 0010.
3. Ghi vào review (không chặn): buildMsMax 240 ms (giật lẻ), 1,5 triệu tam giác.
4. Sau 7b: 7c minimap (brief mới).

## Quyết định quan trọng & lý do
- Điện thoại luôn cắm USB, chế độ nhà phát triển: AI tự `adb -s PFQCRWVWR46HU4GQ install -r`, tự mở app, tự đọc logcat; không nhờ user cài (user 2026-10-20). Không dây 192.168.1.6:5555 bị chặn cài. Trong zsh dùng `export ANDROID_SERIAL=...` thay cho biến chứa `-s <serial>`.
- Unity Editor chỉ chạy khung Play khi ở foreground: `osascript -e 'tell application "System Events" to set frontmost of (first process whose unix id is <pid>) to true'`. Bài đo qua MCP phải đếm theo khung, không theo thời gian thực.
- Phiên con: child-sonnet-medium (`local_f4cf8e44-…`), child-opus-medium (`local_aa562a99-…`) đang idle.

## File liên quan
- `.ai/briefs/0010-world-nap-dan-chunk.md`, `client/Assets/Game/Client/TerrainChunkView.cs`, `client/Assets/Game/Core/ChunkStreamPlan.cs`, `client/Assets/Game/Client/TerrainBench.cs`, `client/Assets/Game/World/BlockMesher.cs`, `client/Assets/Game/Client/DebugFlyCamera.cs`

## Prompt tiếp tục
```text
Tiếp tục công việc đang dở trong .ai/handoff.md — task world bước 7 (0022); brief 0010 (7b) chờ review, đã đo điện thoại + tự test MCP đạt.
Bước tiếp theo: /ai-workflow-kit:review-brief 0010, rồi hỏi user về vụ không điều khiển được camera bay (chuột hay trackpad).
Đọc handoff và các file liên quan trước, tóm tắt lại cho tôi trong 3 dòng rồi mới làm.
Model đề xuất cho bước này: Opus · medium (AGENTS.md §10.2).
```
