# Handoff

> Trạng thái: đang dở
> Cập nhật: 2026-10-14 15:50

<!-- AI ghi khi context lớn / trước khi /clear / khi user yêu cầu (/ai-workflow-kit:handoff).
File này được commit (theo sang máy khác). Phiên sau dùng xong (đã tóm tắt, user xác nhận bước tiếp theo) -> XOÁ file;
nội dung cũ vẫn xem lại được bằng: git log -p -- .ai/handoff.md
Chỉ ghi sự thật đã xác minh. -->

## Mục tiêu
M1 — kế hoạch prototype simulation (`design/04-tech/simulation-tech.md` "Kế hoạch prototype (M1)"): xong bước 1–3, tiếp **bước 4** — đình trệ + thông báo (không có đường / kho đầy / thiếu năng lực), nhà mẫu 2 × 2 × 2 (có nhà mới làm việc), sinh vật bị kẹt (không có đường về nhà 300 nhịp → hiện lại ở nhà). Ước lượng 3–4 ([0016](../design/decisions/0016-o-ruong-kho-nha-va-sinh-vat-ket.md)).

## Đã làm (kèm kết quả kiểm chứng)
- Cách làm: Planner (Opus) viết task contract `.ai/tasks/` + brief `.ai/briefs/NNNN`; Executor (Sonnet) `/ai-workflow-kit:exec NNNN` ở phiên khác; Planner `/ai-workflow-kit:review-brief NNNN` (mức "đầy đủ" = đọc diff).
- Bước 1 (brief 0001, 0014), bước 2 (brief 0002, 0015) — xem simulation-tech.
- **Bước 3 (brief 0003, code `88ee00f`, review `da653ff`)**: `AddFarmOrder(soilCells, storageId)`, `EnqueueCrop(plotId)`, `FarmPlot` (hàng đợi vụ, `PlotPhase`, `CropPhase` chín ở nhịp tưới xong + 1200), `Approach` Exact/Adjacent/AdjacentOrOn + `GridPathfinder.FindPathToAny`, kho chặn ô (`SetBlocked`, `IsOccupied`), giữ hạt / chỗ kho khi giao việc (`StallReason.NoSeeds`, `StorageFull`, mở lại đầu `AssignPhase`). [run] EditMode **131/131**.
- Quyết định **0016** (user 2026-10-14): ô kề, kho công trình, hàng đợi vụ 2 chỗ, 1 lượt vận chuyển mang 1 nông sản + 1 hạt, sức chứa tính chung; nhà sinh vật (cơ chế mới trong `mechanics.md` mục "Nhà sinh vật"); kẹt = không có đường về nhà 30 giây. M1 55–89; tổng ~12,3–23,0 năm.
- Roadmap "Theo dõi M1" có 15 dòng giờ thực tế → đủ để tính lại hệ số ước lượng khi user muốn.

## Còn lại
1. Hỏi user các điểm mở của bước 4 rồi viết task contract + brief 0004:
   - "Thông báo" ở bước 4 là gì: danh sách sự kiện trong `Game.Core.Sim` (vị trí + lý do) để UI đọc sau, hay cần cả UI tối giản? (Hiển thị là bước 6.)
   - "Thiếu năng lực": không có sinh vật nào **sở hữu** năng lực (kể cả đang bận) → `StallReason.NoAbility`? Mở lại khi thêm sinh vật.
   - Nhà mẫu 2 × 2 × 2: chiếm 8 ô (chặn thân như kho)? Có cửa không (ở M1)? Sinh vật hiện lại ở ô nào (ô kề nhà đứng được, thứ tự cố định)? Một nhà sức chứa 2 → gắn sinh vật vào nhà thế nào (`AddHouse`, `AssignHome`)? Sinh vật không có nhà → không được giao việc.
   - Kẹt: kiểm A\* về nhà mỗi nhịp hay mỗi N nhịp (chi phí với 60 sinh vật, ngưỡng ≤ 5 ms/nhịp ở bước 7)? Đang làm việc/đang đi mà không có đường về nhà có tính là kẹt không? Hiện lại thì bỏ việc đang làm (`Unassign`, giữ tiến độ, hàng rơi tại chỗ)?
2. Đặt cách tính số nhịp trong brief: "làm ngay" = t + B với t là nhịp giao (bài học brief 0003).

## Quyết định quan trọng & lý do
- Brief phải có test cho trường hợp biên nhiều bước; số nhịp Planner tính tay; Executor không sửa số trong test cho khớp code.
- Unity Editor dùng chung qua MCP (0010); `tools/unity_test.py` chỉ khi Editor tắt.
- Không tự đổi nội dung đã Chốt; đổi kế hoạch → quyết định mới (0014–0016).
- Việc để sau: đào/đặt làm hỏng ô ruộng hoặc ô dưới kho → `design/ideas.md`.

## File liên quan
- `design/04-tech/simulation-tech.md`, `design/02-gameplay/mechanics.md` (Tìm đường & đình trệ, Nhà sinh vật, Kho & lệnh trồng cây), `design/05-roadmap.md`, `design/decisions/0016*`, `design/data/sim_m1.json`.
- `client/Assets/Game/Core/Sim/*.cs`, `client/Assets/Game/Tests/Core/Sim/*.cs`, `.ai/briefs/0003*`, `.ai/tasks/m1-sim-buoc-3-trong-cay.md`.

## Prompt tiếp tục
```text
Tiếp tục công việc trong .ai/handoff.md — M1 simulation xong bước 1–3 (brief 0001–0003 đã duyệt). Đọc handoff, tóm tắt 3 dòng; hỏi tôi các điểm mở của bước 4 (đình trệ + thông báo, nhà mẫu, sinh vật bị kẹt), rồi viết task contract + brief 0004 (/ai-workflow-kit:brief).
```
