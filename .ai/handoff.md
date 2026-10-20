# Handoff

> Trạng thái: đang dở
> Cập nhật: 2026-10-20 23:55

<!-- AI ghi khi context lớn / trước khi /clear / khi user yêu cầu (/ai-workflow-kit:handoff).
File này được commit (theo sang máy khác). Phiên sau dùng xong (đã tóm tắt, user xác nhận bước tiếp theo) -> XOÁ file;
nội dung cũ vẫn xem lại được bằng: git log -p -- .ai/handoff.md
Chỉ ghi sự thật đã xác minh. -->

## Mục tiêu
Task [world bước 7](tasks/m1-world-buoc-7-nhieu-map-minimap.md), dòng **7d** theo [0023](../design/decisions/0023-dau-moc-cong-trinh-checkpoint-va-dan-duong.md) (user chốt 2026-10-20): bản đồ cập nhật khi đào/đặt + dấu mốc = công trình đặc trưng của từng map (khối giữ chỗ do bộ sinh đặt) + 1 checkpoint mỗi map (mở sẵn). Dịch chuyển tới checkpoint + đường chỉ dẫn đi bộ → M2. Tách 2 brief: **0017 lõi + cập nhật bản đồ** (đã exec), **0018 hiển thị** (chưa viết).

## Đã làm (kèm kết quả kiểm chứng)
- 7c xong: brief 0015 + 0016 đã duyệt (commit `335ae7d`), user thử Mac + điện thoại "ổn" [user]. FPS Bench 36,1 (< 53,1 lần trước) — điểm theo dõi, chưa rõ nguyên nhân.
- Thiết kế 0023 + cập nhật world-tech 7d, controls-ui, glossary, roadmap (M1 +1–1,5, M2 +3,5–6), task bước 7 — commit `24e085c`; `check_design.py` hợp lệ [run].
- Brief [0017](briefs/0017-world-dau-moc-loi-va-cap-nhat-ban-do.md) (`5d4366d`) → executor Sonnet · medium làm xong: code `fa0d8b9`, kết quả `4bc3a0f`, trạng thái `chờ review` [repo]. Theo executor: EditMode 311/311, PlayMode 28/28; seed mặc định checkpoint cách trọng tâm ≤ 1,3 ô, đặt dấu mốc 26 ms.

## Còn lại
1. **Review 0017** (`/ai-workflow-kit:review-brief 0017`, Opus · medium): đọc diff `5d4366d..4bc3a0f`; user tự test (đào hố → bản đồ đổi màu, lấp lại → về cũ). Ghi giờ 7d phần 1 (executor 23:45–23:51) vào roadmap.
2. **Viết brief 0018** (hiển thị dấu mốc) sau khi 0017 duyệt. Hướng đã chốt/đề xuất:
   - `TerrainChunkView.Build` (MapsV3) gọi `LandmarkPlacer.Place(World, Maps, p)` → giữ danh sách `Landmarks` (property).
   - Trong world: mỗi dấu mốc là vật thể hiển thị đơn giản (không phải ô voxel) đặt trên `Surface + 1` của cột; checkpoint và từng loại công trình có màu/hình riêng; đào dưới chân thì vẫn lơ lửng (chấp nhận ở M1, 0023).
   - Trên bản đồ (`MinimapView`): biểu tượng riêng cho checkpoint và từng loại công trình, đặt bằng `anchoredPosition` như tên map (`Viewport.WorldToPanel(X + 0,5, Z + 0,5)`); có thể thêm chú thích (tên) khi phóng to — Planner chốt khi viết brief.
   - Test PlayMode: số vật thể = số dấu mốc, vị trí đúng; biểu tượng đúng vị trí khung. User xem trên Mac + Bench điện thoại (FPS ≥ 30).
   - API Unity mới phải tra docs 6000.4 (vd primitive mesh/`GameObject.CreatePrimitive`, `MaterialPropertyBlock` nếu dùng) và ghi link.
3. Sau 7d → 7e (mô phỏng trên world lớn, `SimDemo` trong Map 1) → sim 6a.

## Quyết định quan trọng & lý do
- Dấu mốc tính sau khi sinh world, số nguyên + PCG32 (`SimRng(seed, 0x4C414E44)`), không đổi ô → mã băm v3 giữ nguyên, khỏi đo lại điện thoại.
- Tên công trình là tên tạm "Công trình 1A…3B" trong `design/data/world_maps_m1.json`; tên thật user đặt (memory: user đặt mọi tên riêng).

## File liên quan
- `.ai/briefs/0017-world-dau-moc-loi-va-cap-nhat-ban-do.md`, `client/Assets/Game/Core/LandmarkPlacer.cs`, `client/Assets/Game/Client/MinimapView.cs`, `client/Assets/Game/Client/TerrainChunkView.cs`, `design/decisions/0023-dau-moc-cong-trinh-checkpoint-va-dan-duong.md`.

## Prompt tiếp tục
```text
Tiếp tục công việc đang dở trong .ai/handoff.md — world 7d: brief 0017 (lõi dấu mốc + bản đồ cập nhật khi đào/đặt) đã exec, chờ review.
Bước tiếp theo: /ai-workflow-kit:review-brief 0017, rồi viết brief 0018 hiển thị dấu mốc (bản đồ + trong world).
Đọc handoff và các file liên quan trước, tóm tắt lại cho tôi trong 3 dòng rồi mới làm.
Model đề xuất cho bước này: Opus · medium (review + viết brief, AGENTS.md §10.2).
```
