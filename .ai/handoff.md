# Handoff

> Trạng thái: đang dở
> Cập nhật: 2026-10-14 14:45

<!-- AI ghi khi context lớn / trước khi /clear / khi user yêu cầu (/ai-workflow-kit:handoff).
File này được commit (theo sang máy khác). Phiên sau dùng xong (đã tóm tắt, user xác nhận bước tiếp theo) -> XOÁ file;
nội dung cũ vẫn xem lại được bằng: git log -p -- .ai/handoff.md
Chỉ ghi sự thật đã xác minh. -->

## Mục tiêu
M1 — kế hoạch prototype simulation (`design/04-tech/simulation-tech.md` "Kế hoạch prototype (M1)"): xong bước 1–2, tiếp **bước 3** (lệnh trồng cây 5 công đoạn + một kho; ghép việc đã có từ bước 1).

## Đã làm (kèm kết quả kiểm chứng)
- Cách làm mới: Planner (Opus) viết task contract `.ai/tasks/` + brief `.ai/briefs/NNNN`; Executor (Sonnet) làm ở phiên khác bằng `/ai-workflow-kit:exec NNNN`; Planner `/ai-workflow-kit:review-brief NNNN` (mức "đầy đủ" = đọc diff).
- Bước 1 (brief 0001, code `5c17c4a`): `Game.Core.Sim` — `SimRng` PCG32, `SimConfig` ← `design/data/sim_m1.json`, `Simulation.Tick()` (địa hình → làm → đi → giao việc), `SimHash`, ghép việc (Manhattan ÷ thuần thục, hoà → bốc thăm theo seed, ưu tiên cao trước), vận chuyển 2 chặng, `Unassign` giữ tiến độ. Quyết định **0014**.
- Bước 2 (brief 0002, code `0b7d120` + sửa `9834dee`): `GridPathfinder` A\* (4 hướng, leo/xuống 1, khung X×Z×Y, không vào nước), đình trệ `Blocked/NoPath` mở lại khi địa hình đổi, `OnCellChanged`/`IsOccupied` để nối `WorldEditor`, đào dưới chân → chạy ra / rơi tới đáy. Quyết định **0015** (khung chiếm chỗ theo loài; M1 đều 1×1×1).
- [run] EditMode **118/118** (MCP `run_tests`), `check_design.py` exit 0. Review commit cuối `8a787f9`.
- Ước lượng: M1 **52–86**; tổng M1–M4 ~12,1–22,7 năm. Giờ thực tế bước 1–2 trong roadmap "Theo dõi M1".
- User vừa thêm (2026-10-14, chưa chốt chi tiết): **sinh vật bị kẹt** vì bất kỳ lý do gì quá một khoảng thời gian → biến mất, hiện lại ở nơi ở trong vườn — đã ghi `mechanics.md` (mục Tìm đường sinh vật & đình trệ) + câu hỏi mở trong simulation-tech.

## Còn lại
1. Hỏi user các điểm mở của "sinh vật bị kẹt" (định nghĩa kẹt, ngưỡng thời gian, nơi ở ở M1, làm ở bước nào/M1 hay M2) — có thể gộp vào bước 3 hoặc 4.
2. Simulation bước 3: task contract + brief 0003 (lệnh trồng cây: xới → gieo (trừ hạt kho) → tưới → cây lớn 120 s → thu hoạch → vận chuyển vào kho; kho 50, 10 hạt; luật ở `mechanics.md` "Kho & lệnh trồng cây — luật M1"). Cần chốt: kho chiếm 1 ô → sinh vật đứng ở đâu để giao hàng (ô cạnh kho?); ô ruộng = ô đất/cỏ, sinh vật đứng trên ô đó (y + 1)?
3. Sau 4–6 hạng mục M1: tính lại hệ số ước lượng (đã có 11 dòng "Theo dõi M1").

## Quyết định quan trọng & lý do
- Brief phải có test cho trường hợp biên nhiều bước (bài học 0002: test chỉ rơi 1 ô nên sót lỗi).
- Executor không được sửa số trong test cho khớp code; số nhịp Planner tính tay trong brief.
- Unity Editor dùng chung qua MCP (0010); `tools/unity_test.py` chỉ khi Editor tắt. Sau `run_tests` PlayMode: `git checkout -- client/ProjectSettings/EditorSettings.asset`.
- Không tự đổi nội dung đã Chốt; đổi kế hoạch → quyết định mới (0014, 0015 là ví dụ).

## File liên quan
- `design/04-tech/simulation-tech.md`, `design/02-gameplay/mechanics.md`, `design/05-roadmap.md`, `design/decisions/0014*`, `0015*`, `design/data/sim_m1.json`.
- `client/Assets/Game/Core/Sim/*.cs`, `client/Assets/Game/Tests/Core/Sim/*.cs`, `.ai/briefs/0001*`, `0002*`, `.ai/tasks/m1-sim-buoc-{1,2}-*.md`.

## Prompt tiếp tục
```text
Tiếp tục công việc trong .ai/handoff.md — M1 simulation xong bước 1–2 (brief 0001, 0002 đã duyệt). Đọc handoff, tóm tắt 3 dòng; hỏi tôi các điểm mở của "sinh vật bị kẹt" và của bước 3, rồi viết task contract + brief 0003 (/ai-workflow-kit:brief).
```
