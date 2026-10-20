# Handoff

> Trạng thái: đang dở
> Cập nhật: 2026-10-20 22:12

<!-- AI ghi khi context lớn / trước khi /clear / khi user yêu cầu (/ai-workflow-kit:handoff).
File này được commit (theo sang máy khác). Phiên sau dùng xong (đã tóm tắt, user xác nhận bước tiếp theo) -> XOÁ file;
nội dung cũ vẫn xem lại được bằng: git log -p -- .ai/handoff.md
Chỉ ghi sự thật đã xác minh. -->

## Mục tiêu
Task [world bước 7](tasks/m1-world-buoc-7-nhieu-map-minimap.md): xong 7b2–7b4, tiếp theo là **7c minimap** (dòng 7c trong `design/04-tech/world-tech.md:177`).

## Đã làm (kèm kết quả kiểm chứng)
- Brief 0012, 0013 (world 7b3) và 0014 (world 7b4, mép cỏ bo tròn ôm khối) đều đã duyệt [repo]. Review 0014 ở commit `aeea55e`: EditMode 272/272, PlayMode 19/19 (theo executor); Bench điện thoại 53,1 FPS, holeFrames=0, tris=3.889.524 [run]; user xem ổn [user].
- Roadmap đã ghi giờ 7b4 (Executor 8 phút); world-tech dòng "Mép cỏ" đã cập nhật hình mới; `check_design.py` hợp lệ [run].

## Còn lại
1. **Viết brief 7c minimap** (`/ai-workflow-kit:brief`, Planner): đọc dòng 7c ở `design/04-tech/world-tech.md:177` (nền tầng độ cao từ bộ sinh, ranh giới cong, tên map, vị trí người chơi; phóng to/kéo bằng chuột + chụm 2 ngón; test: ảnh nền tất định, đúng màu tầng, ranh giới khớp bảng map, user xem; ước 2–3,5), task contract world bước 7, quyết định [0022](../design/decisions/0022-world-nhieu-map-va-minimap-m1.md). API Unity (UI, texture, input chụm 2 ngón) phải tra docs 6000.4 và ghi link vào brief. Có thể cần tách nhiều brief.
2. Exec → review (đầy đủ + user xem; đo FPS điện thoại bằng Bench).
3. Sau 7c: sim 6a (theo [0021](../design/decisions/0021-lam-6b-truoc-6a.md) / [0022](../design/decisions/0022-world-nhieu-map-va-minimap-m1.md)).

## Quyết định quan trọng & lý do
- Đo FPS điện thoại: menu "Game/Tạo scene Bench" → `git checkout -- client/Assets/Game/Client/Scenes/Bench.unity.meta` → "Game/Build APK Bench" → adb (`/Applications/Unity/Hub/Editor/6000.4.8f1/PlaybackEngines/AndroidPlayer/SDK/platform-tools/adb`, không dây `192.168.1.6:5555`, package `com.viventa.bench`, activity `com.unity3d.player.UnityPlayerGameActivity`) → logcat dòng `BENCH`. Sau đó trả `Bench.unity.meta` + `client/ProjectSettings/UnityConnectSettings.asset`. Máy khoá màn hình thì app tạm dừng → nhờ user mở khoá (không nhập mã).
- Executor phải lấy giờ bằng `date` (0012, 0013 ghi sai giờ; 0014 đã đúng).
- Ước số tam giác: so với tổng thực tế, không chỉ phần đang sửa (0014 ước "+3%", thực tế +68%).

## File liên quan
- `.ai/tasks/m1-world-buoc-7-nhieu-map-minimap.md`, `design/04-tech/world-tech.md`, `design/decisions/0022-world-nhieu-map-va-minimap-m1.md`, `design/05-roadmap.md`, `.ai/briefs/0014-world-mep-co-bo-tron.md` (mẫu brief gần nhất).

## Prompt tiếp tục
```text
Tiếp tục công việc đang dở trong .ai/handoff.md — world bước 7: 7b2–7b4 đã duyệt, tiếp theo 7c minimap.
Bước tiếp theo: /ai-workflow-kit:brief cho world 7c minimap (đọc dòng 7c world-tech, task contract world bước 7, quyết định 0022).
Đọc handoff và các file liên quan trước, tóm tắt lại cho tôi trong 3 dòng rồi mới làm.
Model đề xuất cho bước này: Opus · medium (viết brief, AGENTS.md §10.2).
```
