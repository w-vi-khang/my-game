# Handoff

> Trạng thái: đang dở
> Cập nhật: 2026-10-21

<!-- AI ghi khi context lớn / trước khi /clear / khi user yêu cầu (/ai-workflow-kit:handoff).
File này được commit (theo sang máy khác). Phiên sau dùng xong (đã tóm tắt, user xác nhận bước tiếp theo) -> XOÁ file;
nội dung cũ vẫn xem lại được bằng: git log -p -- .ai/handoff.md
Chỉ ghi sự thật đã xác minh. -->

## Mục tiêu
Viết task contract + brief cho **sim bước 7b** (lõi của [0025](../design/decisions/0025-nguoi-choi-xoi-dat-chon-giong-hang-doi.md)), sau đó là 7c (client). Bước 8 (môi trường sống) làm sau 7c.

## Đã làm (kèm kết quả kiểm chứng)
- **0025 Chốt** (user chọn 2026-10-21):
  - M1, trước bước 8.
  - Người chơi **đánh dấu** từng ô bằng công cụ Xới → biển canh tác hiện ngay → sinh vật L1 tới xới → biển mất, ô thành ô ruộng.
  - Bấm ô ruộng → chọn giống (2 loại cây). Vụ đầu bắt đầu từ gieo; vụ kế xới lại.
  - Bỏ `AddFarmOrder` (lệnh vùng).
  - Kho gần nhất tới được; các kho thông nhau (sức chứa cộng dồn); nút Kho xem vật phẩm.
  - "Chọn thông minh" theo khuôn → ideas.
- Đã cập nhật: mechanics, simulation-tech (bước 7b 2,5–5 · 7c 3–5), controls-ui, roadmap (M1 84,5–138,5; M2 325–620), glossary, ideas, summary (tên 2 loại cây chờ user đặt), decisions/README.
- `python3 tools/check_design.py` [run]: hợp lệ, 50 file.

## Còn lại
1. **Task contract** `.ai/tasks/m1-sim-buoc-7b-...md` → **brief 7b**.
   - Đọc `Simulation.cs`: `AddFarmOrder` :786, `AddStorage` :101, `FarmPlot` / hàng đợi vụ, `TerrainPhase` (huỷ vụ 0020).
   - Đọc thêm `SimDemo`, `SimStress`, `sim_m1.json`, các test dùng `AddFarmOrder` (`FarmTests`, `PlotRulesTests`, `IdleTests`, `SimBoundsTests`, `SimDeterminismTests`).
   - Brief ghi rõ mốc nhịp `FarmTests` nào được đổi.
2. **Luật Planner đề xuất trong 0025** (trạng thái "đã xới", bỏ đánh dấu, xoá vụ, hạt theo loại 10/loại, kho gần nhất theo đường) → user xác nhận khi duyệt brief.
3. Số liệu 2 loại cây: số tạm trong brief, `game-balance` đặt sau.
4. Sau 4–6 hạng mục M1 thì tính lại hệ số ([roadmap](../design/05-roadmap.md#kiểm-tra-phạm-vi)).

## Quyết định quan trọng & lý do
- **Điện thoại:** Xiaomi 24095PCADG, IP 192.168.1.6.
  - Dò cổng bằng `adb mdns services`.
  - Không dùng `192.168.1.7:6060` — đó là hộp Android TV.
- **Sau build APK:** `git checkout -- client/ProjectSettings/UnityConnectSettings.asset`. **Sau PlayMode:** `git checkout -- client/ProjectSettings/EditorSettings.asset`.

## File liên quan
- `design/decisions/0025-nguoi-choi-xoi-dat-chon-giong-hang-doi.md`, `design/04-tech/simulation-tech.md` (bảng bước 7b/7c), `design/02-gameplay/mechanics.md` (Kho & lệnh trồng cây — luật M1).
- `client/Assets/Game/Core/Sim/{Simulation,SimDemo,SimStress}.cs`, `design/data/sim_m1.json`.

## Prompt tiếp tục
```text
Tiếp tục công việc trong .ai/handoff.md — viết task contract + brief sim bước 7b (lõi của quyết định 0025: đánh dấu xới, 2 loại cây, kho thông nhau).
Đọc handoff trước, tóm tắt 3 dòng rồi bắt đầu.
Model đề xuất: Opus · medium (Planner: task contract, brief — AGENTS.md §10.2).
```
