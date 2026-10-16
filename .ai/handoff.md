# Handoff

> Trạng thái: đang dở
> Cập nhật: 2026-10-16

<!-- AI ghi khi context lớn / trước khi /clear / khi user yêu cầu (/ai-workflow-kit:handoff).
File này được commit (theo sang máy khác). Phiên sau dùng xong (đã tóm tắt, user xác nhận bước tiếp theo) -> XOÁ file;
nội dung cũ vẫn xem lại được bằng: git log -p -- .ai/handoff.md
Chỉ ghi sự thật đã xác minh. -->

## Mục tiêu
M1: kế hoạch prototype simulation (`design/04-tech/simulation-tech.md`, mục "Kế hoạch prototype (M1)"). Bước 1–5 đã xong. Tiếp theo là **bước 6 — hiển thị: nội suy vị trí, trạng thái**:
- Xong khi: "Sinh vật đi mượt ở 30 và 60 FPS".
- Ước lượng 1–2 ngày-người.
- Mức review: `user xem`.

## Đã làm (kèm kết quả kiểm chứng)
- Cách làm: Planner (Opus) viết task contract + brief, Executor (Sonnet) chạy `/ai-workflow-kit:exec NNNN`, Planner review.
- **Bước 5** (quyết định 0018; brief 0005; code `f86a6dc`; review `93e98a8`):
  - bảng `masteryLevels` (ngưỡng → `speedPermille`, số tạm) trong `sim_m1.json`;
  - `SimConfig.WorkSpeedPermille`;
  - `WorkPhase` cộng hệ số của sinh vật đang làm.

  [run] EditMode **163/163**.
- Roadmap "Theo dõi M1" có 17 dòng giờ thực tế, đủ để tính lại hệ số ước lượng khi user muốn.

## Còn lại
1. Bước 6: đọc code lõi + phần client hiện có, hỏi user các điểm mở, rồi viết task contract + brief 0006. Điểm mở tạm nghĩ ra [chưa xác minh với code]:
   - dùng scene nào (TerrainView có sẵn từ world bước 2–6?);
   - sinh vật hiển thị bằng gì (khối placeholder?);
   - "trạng thái" gồm những gì (Idle / Moving / Working, tiến độ, đình trệ từ `State.Events`);
   - cách đo "đi mượt" ở 30 / 60 FPS (`Application.targetFrameRate`? test PlayMode?).
2. API Unity dùng trong brief phải tra docs 6000.4 và ghi link `[docs]`.

## Quyết định quan trọng & lý do
- Brief phải có test cho trường hợp biên nhiều bước. Số nhịp do Planner tính tay; Executor không sửa số trong test cho khớp code.
- Unity Editor dùng chung qua MCP (0010).
  - `tools/unity_test.py` chỉ dùng khi Editor tắt.
  - Sau `run_tests` PlayMode: `git checkout -- client/ProjectSettings/EditorSettings.asset`.
- Ghi chú để xem ở bước 7:
  - `HasHomedAbility` / `ReportPhase` tốn O(stage × sinh vật) mỗi nhịp.
  - `State.Events` không giới hạn kích thước. Nơi đọc ở bước 6 phải tự `Clear()`.
  - `WorkSpeedPermille` duyệt bảng mỗi nhịp.

## File liên quan
- `design/04-tech/simulation-tech.md` (hàng 6 kế hoạch), `design/04-tech/architecture.md` (nền tảng code M1).
- `client/Assets/Game/Core/Sim/{Simulation,SimData}.cs`: `Creature.Cell`, `MoveProgress`, `Path`, `State`; `SimEvent`.
- `.ai/briefs/0005*`, `.ai/tasks/m1-sim-buoc-5-thuan-thuc.md`.

## Prompt tiếp tục
```text
Tiếp tục công việc trong .ai/handoff.md — M1 simulation xong bước 1–5 (brief 0001–0005 đã duyệt). Đọc handoff, tóm tắt 3 dòng; hỏi tôi các điểm mở của bước 6 (hiển thị: nội suy vị trí, trạng thái), rồi viết task contract + brief 0006 (/ai-workflow-kit:brief).
```
