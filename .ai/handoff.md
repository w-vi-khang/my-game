# Handoff

> Trạng thái: đang dở
> Cập nhật: 2026-10-20 15:40

<!-- AI ghi khi context lớn / trước khi /clear / khi user yêu cầu (/ai-workflow-kit:handoff).
File này được commit (theo sang máy khác). Phiên sau dùng xong (đã tóm tắt, user xác nhận bước tiếp theo) -> XOÁ file;
nội dung cũ vẫn xem lại được bằng: git log -p -- .ai/handoff.md
Chỉ ghi sự thật đã xác minh. -->

## Mục tiêu
Task [world bước 7](tasks/m1-world-buoc-7-nhieu-map-minimap.md) (quyết định [0022](../design/decisions/0022-world-nhieu-map-va-minimap-m1.md)): world 3 map liền nhau, minimap, nạp dần chunk. Mỗi bước 7a → 7e một brief, brief sau viết khi brief trước đã duyệt.

## Đã làm (kèm kết quả kiểm chứng)
- **7a xong** — brief [0009](briefs/0009-world-bo-sinh-nhieu-map-v3.md) đã duyệt (commit `2c15eb0`). Có `WorldMapGenerator` (generatorVersion 3), `WorldMapParams` (`design/data/world_maps_m1.json`) và `MapTable` (thêm `MergeFragments` để gộp mảnh rời). World 384 × 48 × 384.
  - Test: EditMode 246/246, PlayMode 8/8 [run, theo brief].
  - Mã băm v3: world `a7719c8999939263`, bảng map `ab2074e6196e5f51`. Trùng trên Xiaomi 24095PCADG, 30 ms [run adb logcat].
  - User đã duyệt ảnh xem trước.
- `design/04-tech/world-tech.md` (dòng 7a) và `design/05-roadmap.md` (giờ thực tế 7a) đã cập nhật; `check_design.py` exit 0 [run].

## Còn lại
1. **Bước tiếp theo:** viết brief **7b** bằng `/ai-workflow-kit:brief`. Nội dung 7b:
   - nạp dần chunk quanh camera (mesh + collider); dữ liệu world vẫn giữ cả trong RAM;
   - xong khi: đi/bay qua world không thấy lỗ trước mặt, ≥ 30 FPS trên điện thoại khi di chuyển liên tục, có ghi bộ nhớ và ms dựng mỗi khung;
   - nguồn: dòng 7b trong `design/04-tech/world-tech.md` (bảng kế hoạch prototype) và mục "World nhiều map + minimap (M1)".
   - Các scene TerrainView / SimView / Bench hiện vẫn dùng world v2 64 × 32 × 64. 7b là lúc chuyển scene hiển thị sang v3.
2. Sau đó lần lượt 7c (minimap), 7d (minimap cập nhật khi đào/đặt + dấu mốc), 7e (mô phỏng trong Map 1).

## Quyết định quan trọng & lý do
- QĐ 10 của brief 0009: sau job luôn chạy `MapTable.MergeFragments`. Lý do: ranh giới Voronoi méo có thể cắt bờ biển 2 lần, để lại dải nước nông rời (seed 7: 25 cột); chỉnh biên độ méo không chữa được [run MCP].
- Cài APK lên điện thoại: adb nằm ở `/Applications/Unity/Hub/Editor/6000.4.8f1/PlaybackEngines/AndroidPlayer/SDK/platform-tools/adb` (không có trong PATH). Kết nối không dây: Xiaomi ở `192.168.1.6:5555`; trên máy đã bật "Cài đặt qua USB" [run]. App HashCheck: `com.viventa.hashcheck`.
- Quy ước tiết kiệm usage: mỗi phiên một bước M1.

## File liên quan
- `.ai/tasks/m1-world-buoc-7-nhieu-map-minimap.md`, `design/decisions/0022-world-nhieu-map-va-minimap-m1.md`
- `design/04-tech/world-tech.md` (bảng kế hoạch prototype, dòng 7a–7e)
- `client/Assets/Game/World/WorldMapGenerator.cs`, `client/Assets/Game/Core/MapTable.cs`, `client/Assets/Game/Core/WorldMapParams.cs`
- Các file chunk/mesh hiện có: `client/Assets/Game/World/` (BlockMesher…), `client/Assets/Game/Client/` (TerrainChunkView, TerrainBench)

## Prompt tiếp tục
```text
Tiếp tục công việc đang dở trong .ai/handoff.md — task world bước 7 (0022); 7a đã duyệt.
Bước tiếp theo: viết brief world bước 7b (nạp dần chunk quanh camera, chuyển scene hiển thị sang world v3) bằng /ai-workflow-kit:brief.
Đọc handoff và các file liên quan trước, tóm tắt lại cho tôi trong 3 dòng rồi mới làm.
Model đề xuất cho bước này: Opus · high (AGENTS.md §10.2).
```
