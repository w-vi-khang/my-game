# Handoff

> Trạng thái: đang dở
> Cập nhật: 2026-10-21 17:48

<!-- AI ghi khi context lớn / trước khi /clear / khi user yêu cầu (/ai-workflow-kit:handoff).
File này được commit (theo sang máy khác). Phiên sau dùng xong (đã tóm tắt, user xác nhận bước tiếp theo) -> XOÁ file;
nội dung cũ vẫn xem lại được bằng: git log -p -- .ai/handoff.md
Chỉ ghi sự thật đã xác minh. -->

## Mục tiêu
M1 simulation bước 6a ([task contract](tasks/m1-sim-buoc-6a-luat-o-ruong.md), [0020](../design/decisions/0020-o-ruong-tao-khi-xoi-va-huy-vu.md)). Brief [0026](briefs/0026-sim-o-ruong-va-huy-vu.md) đã làm xong, đang **chờ review** (mức đầy đủ + user xem).

## Đã làm (kèm kết quả kiểm chứng)
- Planner viết brief 0026 (commit `ecccd3c`). Executor Sonnet · medium (phiên con `local_2c9926bd-e40c-49d8-af65-56178df8e5a2`) làm từ 17:37 đến 17:45.
  - Code ở commit `4730284`, kết quả ghi ở commit `28e0ccf`.
- Kết quả executor tự ghi trong brief, Planner chưa kiểm:
  - EditMode 364 test: lần đầu 3 đỏ do lỗi test, executor sửa test rồi chỉ chạy lại `PlotRulesTests` (16/16). **Chưa chạy lại toàn bộ EditMode.**
  - PlayMode 69/69 đạt.

## Còn lại
1. `/ai-workflow-kit:review-brief 0026`:
   - Đọc diff `4730284`. Đối chiếu Quyết định 1–17 và Phạm vi. Kiểm 3 test đã sửa không làm yếu assert.
   - Chạy lại toàn bộ EditMode (Unity MCP, executor đã xong nên không đụng nhau).
2. User xem (mục "User tự test" trong brief, 5 điểm). Gồm xác nhận các đề xuất Planner 8–17: màu ô ruộng (150, 104, 66), nước tràn = đặt khối…
3. Duyệt xong, cập nhật tài liệu:
   - Giờ thực tế 6a (8 phút executor + thời gian Planner) vào `design/05-roadmap.md` và dòng 6a của `design/04-tech/simulation-tech.md` ("Xong …").
   - Màu ô ruộng vào `design/03-art-audio/art-style.md:82`.
   - Bỏ đánh dấu `[ý tưởng]` ở `design/02-gameplay/mechanics.md:88` nếu user xác nhận.
4. Sau 6a: sim bước 7 (đo mobile 60 sinh vật giả, ≤ 5 ms/nhịp).

## Quyết định quan trọng & lý do
- Mô phỏng ghi ô ruộng qua `Simulation.AttachEditor` → `WorldEditor.TryTill`. Ô do mô phỏng tự xới không làm bẩn địa hình (cờ `writingWorld`), để tránh A* toàn bộ sau mỗi lần xới.
- Phát hiện huỷ vụ ở đầu `TerrainPhase` bằng `PlotIntact`. `WorldEditor` tự đổi ô ruộng về đất khi bị đặt khối hoặc nước.
- Phiên con: mở mới bằng `start_session`; clear phiên con cũ bị chặn vì chúng do phiên Planner trước mở.

## File liên quan
- `.ai/briefs/0026-sim-o-ruong-va-huy-vu.md`
- `client/Assets/Game/Core/{Cell,WorldEditor}.cs`, `Core/Sim/{SimData,Simulation,SimHash,SimDemo}.cs`, `Core/View/SiteStatus.cs`, `World/BlockMesher.cs`, `Client/SimView.cs`
- `client/Assets/Game/Tests/Core/Sim/PlotRulesTests.cs` (mới)

## Prompt tiếp tục
```text
Tiếp tục công việc trong .ai/handoff.md — brief 0026 (sim 6a: ô ruộng + huỷ vụ) đã làm xong, đang chờ review: /ai-workflow-kit:review-brief 0026
Đọc handoff trước, tóm tắt 3 dòng rồi mới làm. Nhớ chạy lại toàn bộ EditMode (executor chưa chạy lại sau khi sửa 3 test).
Model đề xuất: Opus · medium (review, AGENTS.md §10.2).
```
