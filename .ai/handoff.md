# Handoff

> Trạng thái: đang dở
> Cập nhật: 2026-10-21 12:35

<!-- AI ghi khi context lớn / trước khi /clear / khi user yêu cầu (/ai-workflow-kit:handoff).
File này được commit (theo sang máy khác). Phiên sau dùng xong (đã tóm tắt, user xác nhận bước tiếp theo) -> XOÁ file;
nội dung cũ vẫn xem lại được bằng: git log -p -- .ai/handoff.md
Chỉ ghi sự thật đã xác minh. -->

## Mục tiêu
Task [world bước 7](tasks/m1-world-buoc-7-nhieu-map-minimap.md), dòng **7e**: mô phỏng trên world lớn. User chốt 2026-10-21:
- vùng mô phỏng 64 × 64 cột quanh trại, tìm đường cấp mảng theo vùng;
- `SimDemo` tự tìm chỗ trong Map 1;
- scene SimView chuyển sang world 3 map.

Tách 2 brief: **0019 lõi** (đang exec), **0020 scene SimView** (chưa viết).

## Đã làm (kèm kết quả kiểm chứng)
- 7d xong: brief 0017 và 0018 đã duyệt. 0018 sửa 1 lần: material dấu mốc lấy từ `Resources/Materials/Landmark.mat`, vì `defaultMaterial` null trong bản build. Điện thoại 52,0 FPS, 0 lỗ, mã băm v3 trùng [run logcat 11:43]. Roadmap và world-tech đã ghi giờ.
- Design 7e (task, glossary "Vùng mô phỏng", world-tech) — commit `a6ab0aa`.
- Brief [0019](briefs/0019-world-vung-mo-phong-va-cho-dat-trai.md) (`a6acf32`), Sonnet · medium, giao cho phiên exec-0018 (`local_4fe2396f-5ff0-4fb6-99a0-0691e03f2268`).
  - Executor làm bước 1–7, code **chưa commit**. Kết quả: `SimBoundsTests` 6/6, test cũ không sửa vẫn đạt (22/22 lọc). `SimDemoMapsTests` 0/5 vì không có cửa sổ 9 × 7 cùng độ cao; tốt nhất 49/63 cột [run, theo executor].
  - Planner chốt (`c889c69`, bước 9): cửa sổ gần phẳng (cột kề chênh ≤ 1, nền 2 nhà phẳng), mỗi ô bố trí lấy độ cao cột của nó, không san phẳng world. Đã gửi lại cho executor lúc 12:31.

## Còn lại
1. Chờ phiên exec làm xong bước 9 → **review 0019** (`/ai-workflow-kit:review-brief 0019`, Opus · medium), mức đầy đủ. Ghi giờ 7e phần 1 vào roadmap (bắt đầu 11:59).
   - Kiểm diff `GridPathfinder`: chỉ đổi InBounds/Index/Coords.
   - Kiểm test cũ không bị sửa (`git diff` trên `client/Assets/Game/Tests/` chỉ có file mới).
   - Đọc các dòng `SIMSITE`, `SIMMEM`, `SIMMAPS`.
2. **Viết brief 0020:** scene SimView chuyển sang MapsV3 + streaming.
   - `SimView.Setup`: `LandmarkPlacer` đã có `view.Landmarks`; gọi `SimDemo.FindSite(view.World, view.Maps, view.Landmarks, 0, waterLevel, cfg.regionSize)`, rồi `new Simulation(cfg, view.World, site.Bounds)`, rồi `SimDemo.Build(Sim, site)`.
   - Camera đặt ở trại.
   - Có thể thêm `MinimapView` vào scene.
   - **Lưu ý:** `SimView.Box` dùng `GraphicsSettings.currentRenderPipeline.defaultMaterial`, sẽ null trong bản build (bài học 0018). Nên đổi sang `Resources/Materials/Landmark.mat` nếu có build SimView.
   - Có PlayMode test + user xem trên Mac.
3. Sau 7e → sim 6a.

## Quyết định quan trọng & lý do
- Vùng mặc định = cả world → mô phỏng đảo v2 y hệt, test cũ không sửa.
- Không san phẳng world cho trại: giữ mã băm, bản đồ và mesh không lệch.
- Bài học: test PlayMode chạy trong Editor không bắt được lỗi asset/shader của bản build → brief có hiển thị mới phải có bước Bench điện thoại đọc `Exception` trong logcat.

## File liên quan
- `.ai/briefs/0019-world-vung-mo-phong-va-cho-dat-trai.md`
- `client/Assets/Game/Core/Sim/{SimBounds,GridPathfinder,Simulation,SimDemo,SimConfig}.cs`
- `client/Assets/Game/Tests/World/SimDemoMapsTests.cs`
- `client/Assets/Game/Client/SimView.cs`

## Prompt tiếp tục
```text
Tiếp tục công việc đang dở trong .ai/handoff.md — world 7e: brief 0019 (vùng mô phỏng 64×64 + SimDemo tự tìm chỗ) đã giao lại executor sau khi Planner chốt vướng mắc (bước 9).
Bước tiếp theo: khi 0019 ở `chờ review` → /ai-workflow-kit:review-brief 0019, rồi viết brief 0020 (scene SimView sang world 3 map).
Đọc handoff và các file liên quan trước, tóm tắt lại cho tôi trong 3 dòng rồi mới làm.
Model đề xuất cho bước này: Opus · medium (review + viết brief, AGENTS.md §10.2).
```
