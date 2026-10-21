# Handoff

> Trạng thái: đang dở
> Cập nhật: 2026-10-21 16:55

<!-- AI ghi khi context lớn / trước khi /clear / khi user yêu cầu (/ai-workflow-kit:handoff).
File này được commit (theo sang máy khác). Phiên sau dùng xong (đã tóm tắt, user xác nhận bước tiếp theo) -> XOÁ file;
nội dung cũ vẫn xem lại được bằng: git log -p -- .ai/handoff.md
Chỉ ghi sự thật đã xác minh. -->

## Mục tiêu
Task [world bước 7g](tasks/m1-world-buoc-7g-minimap-goc.md): thay nút "Bản đồ (M)" bằng ô minimap tròn luôn hiện ở góc trên-phải, bấm/chạm để mở bản đồ lớn. Một brief (0025), PlayMode + user xem, đo FPS điện thoại.

## Đã làm (kèm kết quả kiểm chứng)
- World 7f xong: brief 0024 đã duyệt (`9ce4510`), user xem mục 1–7 ổn [user]. EditMode 340/340, PlayMode 60/60 [run, brief 0024]. Điện thoại 35,0–37,3 FPS (máy nóng), hash `a7719c8999939263`.
- 7g: user chốt 5 điểm (ghi trong task contract):
  - góc trên-phải thay nút;
  - cố định Bắc lên trên, có chữ N;
  - hiện nền + người chơi + ranh giới + dấu mốc + tên map;
  - nút thời điểm giữ riêng, dời xuống dưới;
  - biểu tượng thời tiết để sau (đã ghi `design/ideas.md`).
- Đã cập nhật:
  - world-tech dòng 7g, Cộng 44–70;
  - controls-ui (dòng bản đồ);
  - roadmap M1 79–128,5, tổng 554,5–1.035,5.
  - `check_design.py` hợp lệ [run].

## Còn lại
1. **Viết brief 0025 (7g)** bằng `/ai-workflow-kit:brief 7g`. Mục "Planner chốt khi viết brief" của task contract ghi sẵn cỡ ô, vùng hiện, chỗ đặt tên map, nút "Đóng (M)" trong bảng, dời `TimeDial`.
   - Phải **tra docs uGUI 2.0** cho mặt nạ tròn: `Mask` dùng `TimeDialGraphic` hình `Disk` làm stencil có được không, `showMaskGraphic`. Nếu không chắc thì chạy thử bằng MCP `execute_code`.
   - Tính lại vị trí `TimeDial` (hiện ở `TimeDial.cs`): nút (−16, −84) 72, dòng buổi (−100, −120), vòng (−180, −320) 300. Ô minimap mới cao khoảng 200 nên phải dời tất cả xuống.
   - Ô nhỏ dùng chung texture `MinimapView.Texture`; `uvRect` theo vùng quanh camera. Ranh giới và dấu mốc vẽ lại theo vùng nhìn của ô nhỏ (mỗi ô một `MinimapViewport`?): Planner chốt.
   - Test: các test cũ `MinimapViewPlayTests` dùng `ButtonScreenCenter` / `BlocksPointer`, phải sửa theo ô nhỏ (liệt kê rõ trong brief).
2. Giao Executor (Sonnet · medium), review, user xem, ghi giờ.
3. Sau 7g → sim 6a.

## Quyết định quan trọng & lý do
- 7g nằm trong hạng mục minimap M1 của 0022; mục 5 của 0022 không nói "nút", nên không cần quyết định mới.
- Bài học lặp lại:
  - Bench điện thoại phải đọc `Exception` trong logcat.
  - Máy hay rớt USB, dùng `adb connect 192.168.1.6:5555`.
  - Chụp màn hình khi máy đang khoá sẽ ra ảnh đen.
  - Máy nóng làm FPS giảm.
- Clear phiên con bị auto-mode chặn → mở phiên con mới bằng `start_session`.

## File liên quan
- `.ai/tasks/m1-world-buoc-7g-minimap-goc.md`, `design/decisions/0022-…md` mục 5
- `client/Assets/Game/Client/MinimapView.cs`, `TimeDial.cs`, `TimeDialGraphic.cs`, `MinimapLinesGraphic.cs`, `MinimapLandmarkGraphic.cs`, `MinimapMarkerGraphic.cs`
- `client/Assets/Game/Tests/Client/MinimapViewPlayTests.cs`, `TimeDialPlayTests.cs`
- Mẫu brief UI: `.ai/briefs/0024-vong-chon-moc-va-nhan-buoi.md`

## Prompt tiếp tục
```text
Tiếp tục công việc đang dở trong .ai/handoff.md — world 7f đã xong (0024 duyệt); bước tiếp theo là viết brief 0025 cho 7g (ô minimap tròn luôn hiện ở góc trên-phải, bấm để phóng to): /ai-workflow-kit:brief 7g
Đọc handoff và các file liên quan trước, tóm tắt lại cho tôi trong 3 dòng rồi mới làm.
Model đề xuất cho bước này: Opus · medium (viết brief, AGENTS.md §10.2).
```
