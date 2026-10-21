# Handoff

> Trạng thái: đang dở
> Cập nhật: 2026-10-21 18:45

<!-- AI ghi khi context lớn / trước khi /clear / khi user yêu cầu (/ai-workflow-kit:handoff).
File này được commit (theo sang máy khác). Phiên sau dùng xong (đã tóm tắt, user xác nhận bước tiếp theo) -> XOÁ file;
nội dung cũ vẫn xem lại được bằng: git log -p -- .ai/handoff.md
Chỉ ghi sự thật đã xác minh. -->

## Mục tiêu
Làm M1 **sim bước 7** (user yêu cầu 2026-10-21). Theo [simulation-tech.md:103](../design/04-tech/simulation-tech.md):
- Đo trên mobile với **60 sinh vật giả**, sinh bằng lệnh debug, có luật 6a và 6b.
- Xong khi: **≤ 5 ms/nhịp**. Ngưỡng này user duyệt 2026-10-13, là số tạm.
- Ước lượng 1 ngày-người.

## Đã làm (kèm kết quả kiểm chứng)
- **Sim 6a** (brief 0026): đã duyệt 2026-10-21. EditMode 364/364 do Planner chạy lại; user xem đạt.
- **Tách công cụ đào/đặt + bản mờ** (brief 0027, task `.ai/tasks/m1-dieu-khien-tach-dao-dat.md`): đã duyệt 2026-10-21. EditMode 366/366, PlayMode 76/76; user xem đạt 6/6. Commit cuối `79cb42f`.
- Giờ thực tế của cả hai việc đã ghi vào `design/05-roadmap.md`.

## Còn lại
1. `/ai-workflow-kit:task` cho sim bước 7. Hỏi user tối đa 2 câu nếu thiếu, ví dụ:
   - 60 sinh vật giả đặt ở đâu: trại `SimDemo` trên world 3 map hay map phẳng riêng; có ô ruộng/lệnh hay chỉ lang thang;
   - đo bằng gì: thời gian `Simulation.Tick()` trung bình / p99 ghi ra logcat, so với ngưỡng 5 ms.
2. Viết task contract → brief → giao Executor (`--child`) → review → user xem trên điện thoại.

## Quyết định quan trọng & lý do
- **Điện thoại** luôn cắm USB ở chế độ dev. AI tự cài APK và đọc logcat, không hỏi user (memory `phone-test-self-install`). Đã từng đo FPS: Mali-G615, Vulkan, khoảng 35 FPS ở SimView (brief 0025).
- **Sau build APK:** `git checkout -- client/ProjectSettings/UnityConnectSettings.asset`. **Sau PlayMode:** `git checkout -- client/ProjectSettings/EditorSettings.asset`.
- **Đã có sẵn:**
  - `Simulation.AttachEditor`;
  - đếm thời gian tìm chỗ: `SimView` log `SIMVIEW trại … tìm chỗ … ms, mảng tìm đường … ô` (`client/Assets/Game/Client/SimView.cs`, ~:126);
  - vùng mô phỏng 64 × 64 (`Simulation(cfg, world, site.Bounds)`).
- **Bước 6a** đã tránh chạy A* mọi sinh vật sau mỗi lần xới (cờ `writingWorld`), một phần để bước 7 nhẹ hơn.

## File liên quan
- `design/04-tech/simulation-tech.md` (bảng bước, dòng 103; dòng 79 nói về 54/60 sinh vật).
- `client/Assets/Game/Core/Sim/{Simulation,SimDemo,SimData}.cs`, `client/Assets/Game/Client/SimView.cs`.
- Brief đo điện thoại mẫu: `.ai/briefs/0025-minimap-tron-o-goc.md`, `.ai/briefs/0010-world-nap-dan-chunk.md`.

## Prompt tiếp tục
```text
Tiếp tục công việc trong .ai/handoff.md — làm M1 sim bước 7 (đo mobile 60 sinh vật giả, ≤ 5 ms/nhịp): /ai-workflow-kit:task sim bước 7
Đọc handoff trước, tóm tắt 3 dòng rồi mới làm.
Model đề xuất: Opus · medium (Planner viết task contract + brief, AGENTS.md §10.2).
```
