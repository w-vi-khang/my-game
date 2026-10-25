# Handoff — 2026-10-25

## Điều phối
- Phiên này là Planner (Opus). Executor là phiên con Sonnet · medium (`/ai-workflow-kit:exec NNNN --child`). Unity Editor dùng chung qua MCP → mỗi lúc chỉ một phiên chạy. Phiên con cũ: exec-0074 (local_563ecba5-9cf0-489c-93a5-daf75d994b4e, do phiên Planner trước mở; phiên mới không clear được thì mở phiên mới).
- **0074 (sim 8b) đã duyệt** 2026-10-25: user xem mục G đạt hết. Sim 8 xong.

## Đang làm
User 2026-10-25: "nước phải tràn ra… giới hạn lan… nước chảy vào hố cũng sẽ đầy, chảy từ cao xuống thấp" → user chọn **nguồn + lan giới hạn, làm ngay trong M1** → quyết định [0040](../design/decisions/0040-nuoc-chay-nguon-lan-gioi-han-m1.md) (Chốt, đã ghi + commit `8de0b26`; README decisions đã thêm dòng).

## Bước tiếp
**Cập nhật 2026-10-25 (phiên Planner sau):** bước 1–3 xong — tài liệu theo 0040 (commit `958c720`, M1 123,5–201), task `m1-world-nuoc-chay`, brief 0075 (`abae049`). Brief 0075 đã giao phiên con exec-0075 (local_6c0ab03f-9253-47b6-9631-26bdfb1af2e4, Sonnet · medium). Tiếp: review 0075 (`/ai-workflow-kit:review-brief 0075`), rồi user xem.

1. ~~Commit 0040~~ (xong).
2. Cập nhật tài liệu theo 0040 mục Hệ quả:
   - world-tech mục Nước chảy (dòng ~111–120) + thêm hạng mục vào kế hoạch prototype;
   - mechanics (luật biển tràn);
   - roadmap: +2–4 ngày-người M1 (tính lại tổng M1);
   - glossary nếu thiếu.
3. Task contract mới `.ai/tasks/m1-world-nuoc-chay.md` → brief 0075 (`/ai-workflow-kit:brief`). Đọc code nước thật trước khi viết:
   - `client/Assets/Game/Core/WorldEditor.cs` :30, :51-52, :91 (chỉ lan khi `y <= SeaLevel`), :209-280 (`IsWaterSource`, `TouchesWater`, `FillWater`, `StepWater`, `SettleWater`);
   - `Cell.cs` (mực nước 0–7: `Cell.SetWaterLevel`, `MaxWaterLevel = 7`);
   - client gọi `StepWater` theo nhịp (tìm nơi gọi);
   - test hiện có `WorldEditorTests` (nước), `HabitatTrackerTests.WaterSpreadUpdatesNearWater`, `DayCycleWaterPlayTests`.
   - Cần quyết khi viết brief: cách đánh dấu nguồn và nước đang chảy (mực 7 = nguồn?, hoặc thêm bit); ô có sẵn khi sinh world = nguồn; ao demo `SimDemo.PlacePond` ghi `MaxWaterLevel`.
   - Test bắt buộc: hố cạnh ao đầy; nước rơi từ cao rồi lan đúng N ô; chặn/bỏ nguồn thì rút; biển 0013 vẫn đúng; tất định; mã băm world của các test cũ (nếu đổi thì nêu rõ).
4. Giao Executor (Sonnet · medium), review, user xem (đào cát cạnh ao → nước tràn vào hố; đổ nước từ trên cao).
5. Còn chờ user: chơi thử gộp bước 7 mục A–F ([playtest-buoc-7.md](playtest-buoc-7.md); mục G đạt). Sau đó: rà tiêu chí xong M1 (roadmap).

## Model đề xuất
Opus · medium (Planner: tài liệu + task contract + brief 0075).
