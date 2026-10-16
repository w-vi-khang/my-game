# Handoff

> Trạng thái: đang dở
> Cập nhật: 2026-10-16

<!-- AI ghi khi context lớn / trước khi /clear / khi user yêu cầu (/ai-workflow-kit:handoff).
File này được commit (theo sang máy khác). Phiên sau dùng xong (đã tóm tắt, user xác nhận bước tiếp theo) -> XOÁ file;
nội dung cũ vẫn xem lại được bằng: git log -p -- .ai/handoff.md
Chỉ ghi sự thật đã xác minh. -->

## Mục tiêu
M1 simulation bước 6 (hiển thị) — brief 0006 đã chạy (code `487e523`, scene `4f2932f`), phần code Planner duyệt, **user xem → cần sửa**. Đồng thời user quyết 3 luật mô phỏng mới cần đưa vào M1.

## Đã làm (kèm kết quả kiểm chứng)
- Brief 0006 thực thi bằng Sonnet · medium: EditMode 190/190, PlayMode 8/8 [executor, kết quả trong brief]. Review phần code đạt (`a2e0244`).
- User xem scene SimView (60 FPS đạt, ảnh nhịp 1441). Phản hồi + kết luận ghi ở mục Review của `.ai/briefs/0006-sim-hien-thi.md`.
- Planner kiểm lỗi world qua MCP (scene tạm, không lưu; git sạch) [run]: 220 lần đặt cỏ lên mặt + đào lại trên lưới 3 ô khắp map → mesh dựng lại đúng (tam giác mặt trên đúng độ cao, đúng mẫu màu theo vật liệu); 1846 tia từ camera SimView (26, 26, 10)/(38.66, 36.87, 0) → ô đào luôn đặc, ô đặt luôn rỗng và kề; hố 3 × 3 hiện đáy đất. GPU Resident Drawer = Disabled. **Chưa tái hiện được.**

## Còn lại
1. **Hỏi user** dòng thông báo HUD (dòng 3 góc trái của TerrainEditInput) + ảnh lúc "đặt được mà không vẽ" và lúc "cỏ vẽ nâu". Ghi chú để giải thích:
   - kho vật liệu bắt đầu 0 → đặt loại chưa đào báo "kho hết loại này";
   - theo world-tech, sườn ô cỏ vẽ màu đất, và dưới lớp cỏ là 3 lớp đất (`dirtDepth` 3) — đào cỏ lộ đất nâu là đúng thiết kế;
   - hộp nhà/kho/sinh vật không có collider → tia chuột xuyên qua, có thể trúng ô bị hộp che.
2. **Brief 0006 — bước sửa hiển thị** (thêm vào "Các bước", trạng thái `chờ làm`):
   - ruộng hiện giai đoạn (`FarmPlot.Phase`: Idle/Till/Sow/Water/Growing/Harvest) + % sinh trưởng khi Growing (từ `RipeTick` và `Config.GrowTicks`, xem `Simulation.cs:448-450`);
   - sinh vật đang mang hàng (`WorkStage.Carried`, công đoạn Haul của `c.StageId`) có hộp hàng nhỏ;
   - nhãn kho: số nông sản / hạt (`Storage.Produce`, `Seeds`).
   Cần chốt chữ nguyên văn + test EditMode cho phần chữ/tính % (đặt trong `Core/View`).
3. **Luật mô phỏng mới** (user quyết 2026-10-16, chưa có file quyết định):
   - **Sinh vật chặn nhau khi đi** (chọn "chặn hẳn", ước lượng Planner 2–4 ngày-người; ảnh hưởng đo 5 ms/nhịp ở bước 7). Cần thiết kế: đứng chờ bao lâu rồi tìm đường khác, xử lý hai con đối đầu ở lối hẹp, khung nhiều ô, tất định.
   - **Rảnh → về quanh nhà + thỉnh thoảng lang thang** trong bán kính quanh nhà, dùng bộ sinh có seed (ước lượng 1,5–2). Đang xếp M2 theo [0016](design/decisions/0016-o-ruong-kho-nha-va-sinh-vat-ket.md) → quyết định mới kéo vào M1.
   - **Ô hạn chế (ruộng):** chỉ **không dừng** trên ô ruộng khi không có việc ở chính ô đó (đi qua tự do; ước lượng 0,5).
   → Viết quyết định 0019 (hỏi user các điểm mở của luật chặn nhau trước), cập nhật simulation-tech (thêm bước vào kế hoạch M1, hoặc bước 6b), mechanics, roadmap (cộng ước lượng, kiểm quỹ M1), rồi task contract + brief riêng. Thứ tự đề xuất: sửa hiển thị 0006 trước (nhỏ), rồi luật mới.
4. Sau khi xong: ghi kết quả bước 6 vào simulation-tech + roadmap (giờ thực tế executor ≈ 9 phút), thêm `Game.Core.View` vào bảng "Tách assembly" của architecture.

## Quyết định quan trọng & lý do
- Lớp hiển thị C# thuần ở `Core/View` (`Game.Core.View`) — được dùng double/Dictionary; `Core/Sim` giữ grep cấm.
- Bố trí demo cố định trên map M1: nhà (30, 18, 19), (32, 18, 19); kho (34, 18, 17); ruộng x 30–31 × z 16–17 (y 17); L1/L2/L3 ở (32, 18, 16), (33, 18, 16), (36, 18, 16). Map M1 không có vùng phẳng > 4 × 4.
- Unity Editor dùng chung qua MCP (0010). Sau `run_tests` PlayMode: `git checkout -- client/ProjectSettings/EditorSettings.asset`.

## File liên quan
- `.ai/briefs/0006-sim-hien-thi.md` (mục Review), `.ai/tasks/m1-sim-buoc-6-hien-thi.md`.
- `client/Assets/Game/Client/SimView.cs`, `client/Assets/Game/Core/View/*`, `client/Assets/Game/Core/Sim/SimDemo.cs`.
- `client/Assets/Game/Client/TerrainEditInput.cs`, `TerrainChunkView.cs` (lỗi world chưa tái hiện).
- `design/04-tech/simulation-tech.md` (kế hoạch M1), `design/02-gameplay/mechanics.md` (Nhà sinh vật `:127-131`), `design/decisions/0016-*.md`, `design/05-roadmap.md`.

## Prompt tiếp tục
```text
Tiếp tục công việc trong .ai/handoff.md — brief 0006 (sim bước 6 hiển thị) user xem → cần sửa; user đã quyết 3 luật mô phỏng mới (chặn nhau khi đi, rảnh về quanh nhà + lang thang, không dừng trên ruộng). Đọc handoff, tóm tắt 3 dòng; hỏi tôi thông báo HUD của lỗi đặt khối; viết bước sửa hiển thị vào brief 0006; rồi hỏi điểm mở của luật chặn nhau để viết quyết định 0019 + task/brief mới.
```
