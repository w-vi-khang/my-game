# Task: M1 bước 2 — Surface Nets ×2 + làm mượt, 1 chunk

## Mục tiêu
Dựng mesh bề mặt mịn cho một chunk 16³ từ lưới gameplay ([world-tech](../../design/04-tech/world-tech.md#dựng-mesh-bề-mặt-mịn) bước 2): Surface Nets ở độ phân giải ×2 (mỗi ô gameplay = 2 × 2 × 2 ô dựng mesh), làm mượt có giới hạn (mỗi đỉnh lệch ≤ 0,2 ô), thấy được trong Unity.

## Ngoài phạm vi
- Dựng nhiều chunk song song, đo thời gian, kiểm hở biên trên cả map (bước 3) — nhưng thuật toán đã đọc 1 lớp ô đệm của chunk bên cạnh để bước 3 không phải làm lại.
- Màu vật liệu (bước 5), mesh nước (nước tĩnh chưa vẽ), collider, đào/đặt (bước 4), triplanar (M2).

## Tiêu chí chấp nhận (kiểm chứng được)
- [x] Mặt đất phẳng: mọi đỉnh mặt trên nằm đúng độ cao, pháp tuyến hướng lên — kiểm bằng: test EditMode.
- [x] Rãnh rộng 1 ô, sâu 2 ô: ở giữa độ sâu, bề rộng rãnh ≥ 0,9 ô (không bị bóp chữ V như bản ×1) — kiểm bằng: test EditMode.
- [x] Làm mượt có giới hạn: mọi đỉnh lệch ≤ 0,2 ô so với vị trí Surface Nets gốc — kiểm bằng: test EditMode.
- [x] Mỗi chunk của world M1 ≤ 65.535 đỉnh (chỉ số 16 bit) — kiểm bằng: test trên cả 32 chunk.
- [x] Đo số tam giác một chunk của world M1, so với ước tính ~7.000 (world-tech) — ghi số đo [run].
- [x] Nhìn thấy chunk trong Unity: scene TerrainView + ảnh chụp render từ dòng lệnh gửi user.
- [x] Ghi giờ thực tế (ước lượng 4–6 ngày-người).

## Ràng buộc
- Mesher trong `Game.World` (job Burst, Collections, Mathematics); ghi mesh bằng `Mesh.AllocateWritableMeshData` → `ApplyAndDisposeWritableMeshData` (tech-stack), chỉ số 16 bit.
- Ngoài map: dưới y = 0 coi là rắn; ngoài x/z và trên đỉnh coi là rỗng. Nước không phải mặt rắn.

## Quyết định đã chốt
- Surface Nets ×2, làm mượt ≤ 0,2 ô — user, 2026-10-12 (art-style); kế hoạch prototype — user duyệt 2026-10-13.

## Kết quả (2026-10-13)
- `python3 tools/unity_test.py` → 29/29 đạt.
- Số đo world M1: chunk nặng nhất 6.776 tam giác, 3.881 đỉnh; tổng ~76.440 tam giác; 29/32 chunk có mặt.
- Ảnh: `client/Logs/terrain_view.png` (render batchmode, URP) — bậc thang bo tròn như bản thử Blender ×2.
- Thời gian: 15:12–15:17.

## Câu hỏi còn mở
- Bản thử Blender không lưu script → "khớp bản thử" kiểm bằng các phép đo trên, không so từng đỉnh.
