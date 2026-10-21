# Handoff

> Trạng thái: đang dở
> Cập nhật: 2026-10-21 16:05

<!-- AI ghi khi context lớn / trước khi /clear / khi user yêu cầu (/ai-workflow-kit:handoff).
File này được commit (theo sang máy khác). Phiên sau dùng xong (đã tóm tắt, user xác nhận bước tiếp theo) -> XOÁ file;
nội dung cũ vẫn xem lại được bằng: git log -p -- .ai/handoff.md
Chỉ ghi sự thật đã xác minh. -->

## Mục tiêu
Task [world bước 7f](tasks/m1-world-buoc-7f-buoi-trong-ngay.md) — buổi trong ngày ([0024](../design/decisions/0024-buoi-trong-ngay-va-chon-thoi-diem.md)). Còn brief 3/3: **7f-3 vòng chọn 4 mốc + nhãn buổi** (thay phím tạm F5–F8), PlayMode + user xem, đo FPS điện thoại.

## Đã làm (kèm kết quả kiểm chứng)
- 7f-1 lõi: brief [0022](briefs/0022-buoi-trong-ngay-loi.md) đã duyệt (Executor 14:52–14:57).
- 7f-2 bầu trời: brief [0023](briefs/0023-bau-troi-theo-buoi.md) đã duyệt (`0c47a4a`). Executor 15:33–15:48. EditMode 340/340, PlayMode 52/52. Điện thoại: 10 dòng `DAYCYCLE`, avgFps 50,6, holeFrames 0, hash `a7719c8999939263` [run 15:50]. User xem trên Mac: mục 1–5 đạt, giữ bảng màu [user].
- Giờ 7f chưa ghi vào roadmap. Cộng chung 0022 + 0023 + brief 7f-3 khi xong cả bước.

## Còn lại
1. **Viết brief 0024 (7f-3)** bằng `/ai-workflow-kit:brief 7f-3`.
   - Vòng 4 biểu tượng theo ảnh tham khảo trong 0024 (vòng quanh biểu tượng mặt trăng).
   - Phím hoặc nút mở vòng, chọn được bằng chuột và cảm ứng.
   - Dòng "đang ở buổi …" lấy từ `DayCycleView.Day.Period`.
   - Đo FPS điện thoại.
   - Trước khi viết: kiểm có điểm nào phải hỏi user không (§3.4), vd chữ hiển thị, vị trí trên màn hình, phím mở.
2. Review 0024. Ghi giờ cả 7f vào roadmap + world-tech.
3. Sau 7f → sim 6a.

## Quyết định quan trọng & lý do
- API sẵn có cho 7f-3 [repo `client/Assets/Game/Client/DayCycleView.cs`]:
  - `public int Select(TimeLandmark m)`: trả −1 khi đang xoay (bỏ qua), 0 khi đang trong khung mốc, ngược lại trả cung (ms).
  - `IsRotating`, `Day` (`TimeOfDay`: `Period`, `CurrentWindow`, `PhaseMs`), `debugKeys` (F5–F8), `benchCycleSeconds` (Bench = 5).
- Phím đã dùng:
  - `TerrainEditInput`: 1–4;
  - `SimView`: F1/F2;
  - `MinimapView`: M, Esc;
  - `DebugFlyCamera`: WASDQE, Shift.
  `MinimapView.cs` có sẵn mẫu UI: nút, cảm ứng, chặn chạm xuyên nút.
- Bài học: brief có hiển thị mới phải có bước Bench điện thoại đọc `Exception` trong logcat. Máy hay rớt USB; dùng `adb connect 192.168.1.6:5555`. AI tự cài APK và đọc logcat, không hỏi user.
- Đếm `grep -c` theo tên lớp trong file `.unity` ra 2 cho mỗi component (thêm `m_EditorClassIdentifier`).

## File liên quan
- `.ai/tasks/m1-world-buoc-7f-buoi-trong-ngay.md`, `design/decisions/0024-…md`, `design/02-gameplay/mechanics.md` (mục Buổi trong ngày)
- `client/Assets/Game/Client/DayCycleView.cs`, `MinimapView.cs`, `Editor/BenchBuild.cs`
- `client/Assets/Game/Core/DayCycle/{TimeConfig,TimeOfDay}.cs`

## Prompt tiếp tục
```text
Tiếp tục công việc đang dở trong .ai/handoff.md — world 7f: 0022 (lõi) và 0023 (bầu trời) đã duyệt; bước tiếp theo là viết brief 0024 cho 7f-3 (vòng chọn 4 mốc + nhãn buổi, thay phím F5–F8): /ai-workflow-kit:brief 7f-3
Đọc handoff và các file liên quan trước, tóm tắt lại cho tôi trong 3 dòng rồi mới làm.
Model đề xuất cho bước này: Opus · medium (viết brief, AGENTS.md §10.2).
```
