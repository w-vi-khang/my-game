# Handoff

> Trạng thái: đang dở
> Cập nhật: 2026-10-16

<!-- AI ghi khi context lớn / trước khi /clear / khi user yêu cầu (/ai-workflow-kit:handoff).
File này được commit (theo sang máy khác). Phiên sau dùng xong (đã tóm tắt, user xác nhận bước tiếp theo) -> XOÁ file;
nội dung cũ vẫn xem lại được bằng: git log -p -- .ai/handoff.md
Chỉ ghi sự thật đã xác minh. -->

## Mục tiêu
M1 simulation: brief 0006 bước 9 (sửa hiển thị) chờ executor; viết brief cho bước 6b (luật đi lại, quyết định 0019).

## Đã làm (kèm kết quả kiểm chứng)
- Brief 0006 thêm **bước 9**: `SiteStatus` (HUD kho/ruộng, % lớn, `IsCarrying`), sửa `SimView` (mầm xanh theo %, hộp vàng mang hàng, HUD dưới dòng FPS), 4 test EditMode (→ 194), assert `SproutCount == 4` trong PlayMode. Trạng thái `chờ làm`, Sonnet · medium. Commit `ba30e1a`.
- Lỗi world: thử lại trong Play SimView [run] — 63/63 đặt cỏ `Ok`, 59 thấy mặt trên, 3 chỉ sườn, 1 bị che; ảnh thấy rõ cột cỏ (sườn nâu đúng thiết kế). Chưa tái hiện "đặt được mà không vẽ" → để ngoài 0006, ghi ở mục Review của brief.
- Quyết định **0019** (Chốt, user 2026-10-16) + cập nhật simulation-tech (bước 6b, 4–6,5; cộng 23–36,5), mechanics (chặn nhau; nhà sinh vật bán kính 4), roadmap (M1 59–95,5; M2 326–621; tổng 535–1.002,5, ~12,3–23,1 năm), decisions/README. `check_design.py` exit 0 [run].
- Task contract `.ai/tasks/m1-sim-buoc-6b-luat-di-lai.md`.

## Còn lại
1. User mở phiên executor chạy brief 0006 bước 9 → Planner review (đầy đủ + user xem mục 6).
2. **Viết brief 0007** cho bước 6b từ task contract: đọc kỹ `Simulation.cs` (`MovePhase :551-570`, `OnArrive :574`, `Unassign :703`, `ReportPhase`, đếm kẹt/`Respawn :198`), `GridPathfinder`, `SimHash`, `TestWorlds`. Cần thiết kế: giữ chỗ ô kế tiếp, bộ đếm chờ, chọn ô nhường/né, bộ đếm lang thang (`SimRng`), số mới trong `sim_m1.json` + bản chép client; test có số nhịp tính tay. Có thể tách 2 brief (chặn nhau | rảnh + ô ruộng).
3. Sau 0006 xong: ghi kết quả bước 6 vào simulation-tech + roadmap (giờ executor ≈ 9 phút + lần 2), thêm `Game.Core.View` vào bảng "Tách assembly" của architecture.

## Prompt tiếp tục
```text
Tiếp tục .ai/handoff.md — viết brief 0007 cho sim bước 6b (luật đi lại, quyết định 0019, task contract .ai/tasks/m1-sim-buoc-6b-luat-di-lai.md); hoặc review brief 0006 bước 9 nếu executor đã chạy xong.
```
