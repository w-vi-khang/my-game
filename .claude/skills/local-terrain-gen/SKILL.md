---
name: local-terrain-gen
description: Thiết kế và kiểm bộ sinh địa hình voxel của my-game — danh mục dạng địa hình tự nhiên (đồng cỏ, đồi, núi, tuyết, vách đá, bờ biển, vịnh, sông hồ, đầm lầy, hang...), khối và cách xếp cho từng dạng, vùng chuyển tiếp giữa hai cảnh quan, khối trung gian (dốc cắt chéo, khối có lớp nước mặt 1/5–1/3); chỉnh tham số bộ sinh theo seed; kiểm/duyệt kết quả sinh (tất định, đo nhiều seed, ảnh xem trước). Dùng khi user muốn thêm/sửa dạng địa hình, khối địa hình, chuyển tiếp cảnh quan, tham số bộ sinh world, hoặc kiểm tra world sinh ra có đúng/đẹp không.
---

# Sinh địa hình (local-terrain-gen)

Hai chế độ: **A. Thiết kế / chỉnh bộ sinh** và **B. Kiểm / duyệt kết quả sinh**. Một yêu cầu có thể cần cả hai (A rồi B).
Phân việc với skill khác: nội dung map (vùng nào, loài nào, khối cốt truyện, điều kiện mở) → `local-level-design`; skill này lo **hình dạng đất, khối, luật sinh và kiểm chứng**. Code bộ sinh/mesher → viết brief (`ai-workflow-kit:brief`), không tự code trong skill này.

## Đầu vào cần có (thiếu thì hỏi tuần tự, mỗi lượt 1–2 câu, kèm đề xuất)
1. Chế độ A, B hay cả hai? Dạng địa hình / chuyển tiếp / khối nào? Thêm mới hay sửa?
2. Ý tưởng user đã có (ảnh tham khảo, cảm giác muốn có). Số cụ thể user chưa đưa → **không hỏi**: tự ước lượng, ghi `[số tạm]` (AGENTS.local "Tham số chưa chốt").
3. Milestone: đang M1 (0007) — dạng địa hình/khối mới ngoài M1 chỉ ghi thiết kế + roadmap/ideas, không đưa vào brief M1.

## Đọc trước
`design/00-vision.md` (P1–P4), `design/glossary.md`, `design/04-tech/world-tech.md` (Dữ liệu world, Sinh world theo seed, Dựng mesh khối bo cạnh, Nước, Tìm đường, Lưu trữ, Kế hoạch prototype), `design/decisions/0011` (khối bo cạnh — Chốt), `0012`/`0013` (luật đặt khối, nước loang — Chốt), `0022` (world nhiều map), `design/02-gameplay/levels.md` (preset vùng), `design/data/world_maps_m1.json`, `client/Assets/Game/Core/Cell.cs` (vật liệu hiện có), `design/data/terrain_catalog.json` + `design/02-gameplay/terrain.md` nếu đã có.

## A. Thiết kế / chỉnh bộ sinh
Thứ tự **từ lớn tới nhỏ**: dáng tổng thể (silhouette) → dạng địa hình → chuyển tiếp → khối trung gian → chi tiết bề mặt.

1. **Danh mục dạng địa hình tự nhiên.** Rà đủ các nhóm, đánh dấu cái nào cần cho game (gắn pillar, thường P1 — môi trường sống của loài):
   - Đất thấp: đồng cỏ/đồng bằng, đồi thoải, thung lũng, cao nguyên/thềm, sa mạc–cồn cát.
   - Đất cao: núi (chân–sườn–đỉnh), đỉnh tuyết/băng, vách đá (vách đứng, thềm, rêu), hẻm núi, mũi đá.
   - Nước: bờ biển cát, bờ đá, vịnh, mũi đất, đảo nhỏ, sông/suối, hồ, thác, đầm lầy/bãi bồi.
   - Dưới đất: hang (lối ra mặt đất, theo độ sâu).
   Mỗi dạng ghi: tên + id ổn định, **khối theo lớp** (mặt / dưới mặt / lõi / đáy, độ dày), **luật xếp** (theo độ cao, độ dốc, hướng mặt, khoảng cách tới nước, noise), **dáng** (độ dốc tối đa, bậc thềm, độ cao tương đối), **hình khối dùng** (cube, dốc...), **tham số** (JSON, `[số tạm]`), **gameplay** (đi được không, tài nguyên, môi trường sống), **mặt cắt ASCII** để user xem nhanh.
   Giữ **logic nhân quả**: nước chảy từ cao xuống thấp, tuyết trên cao và mặt phẳng, cát cạnh nước, đá lộ ở sườn dốc. Sai tự nhiên có chủ đích (kiểu fantasy) → ghi rõ lý do.
2. **Chuyển tiếp (vùng giao nhau).** Với mỗi cặp dạng địa hình kề nhau: độ rộng, luật trộn (theo độ cao / độ dốc / noise méo đường biên — không cắt thẳng), khối và hình trung gian dùng ở dải đó. Các cặp thường gặp: núi ↔ tuyết (đường tuyết lệch theo noise, sườn dốc lộ đá), đồng cỏ ↔ bờ cát, vách đá ↔ biển (chân vách đá vụn, bờ đá), sông/hồ ↔ đồng bằng (bãi bồi dùng khối nước mặt), đồi ↔ vách (thềm), rừng/cỏ ↔ đầm lầy.
3. **Khối trung gian (hình khối khác lập phương).** Ví dụ:
   - **Dốc cắt chéo** (lập phương cắt chéo thành nêm): phải có đủ **dốc thẳng + góc ngoài + góc trong** (4 hướng xoay), thiếu thì góc dốc bị thủng/bậc.
   - **Khối có nước trên mặt** (lớp nước 1/5–1/3 chiều cao ô, phần dưới là đất/cát): mép nước, bãi bồi, đầm lầy, ruộng ngập.
   - Ý tưởng khác chỉ ghi `[ý tưởng]`: nửa khối (thềm thấp), lớp tuyết mỏng.
   Mỗi hình mới phải trả lời **đủ** các ảnh hưởng sau (ghi vào tài liệu + `walkable`/`collider`/`dig_result` trong JSON):
   - Dữ liệu ô: 2 byte/ô của world-tech — hình + hướng xoay lấy bit nào (bit dự phòng, hay mã vật liệu riêng)? → động tới phần Chốt của 0011 ("dữ liệu không đổi").
   - Mesher (0011): bảng bo cạnh 256 tổ hợp chỉ cho lập phương; mép cỏ trên dốc; AO; mesh kín qua biên chunk.
   - Collider, tìm đường (dốc = lên/xuống 1 ô mượt hơn?), camera va chạm.
   - Đào/đặt (0012): đào ra vật liệu gì, đặt được dốc không, xoay thế nào.
   - Nước (0012/0013): khối nước mặt có loang/chặn nước không, có tính vào tỉ lệ nước của môi trường sống không.
   - Mã băm golden + phiên bản bộ sinh (lưu trữ: chunk chưa sửa sinh lại).
   - Ước lượng công (ngày-người) + milestone.
4. **Vật liệu mới** (tuyết, băng, sỏi, bùn, đá rêu...): ghi `exists: false`; kèm việc phải làm (thêm vào `Cell.cs`, màu, texture 5.4, tài nguyên rơi ra khi đào → economy).
5. **Ghi kết quả:**
   - Văn bản: `design/02-gameplay/terrain.md` (chưa có thì tạo, `> Trạng thái: Nháp`, thêm vào bảng `design/README.md`); liên kết qua lại với `levels.md` (preset vùng) và `world-tech.md`.
   - Dữ liệu: `design/data/terrain_catalog.json` (cấu trúc theo `tools/terrain_catalog.example.json`); tham số sinh thật nằm ở `design/data/world_*.json` — không hard-code.
   - Hình khối/vật liệu mới, đổi bố cục ô 2 byte, đổi phần Chốt → **đề xuất quyết định mới** trong `design/decisions/` (user chốt), không tự sửa.
   - Thuật ngữ mới (dốc, khối nước mặt, chuyển tiếp...) → `design/glossary.md` cùng lần sửa.
   - Mọi đề xuất của AI mang `[ý tưởng]`; mọi số chưa chốt mang `[số tạm]`.
6. Cần code → viết brief, gồm: tham số JSON, test "Xong khi" (mục B), tăng `generatorVersion` nếu output đổi, cập nhật mã băm golden có chủ đích.

## B. Kiểm / duyệt kết quả sinh
Mỗi mục kiểm ghi: mức (`chặn | lớn | nhỏ | ghi chú`), cách kiểm (`tự động | user xem | thử chơi`), kỳ vọng, kết quả (`đạt | lỗi | chưa chạy`), bằng chứng. Không ghi "đạt" khi chưa chạy hoặc chưa xem.
1. **Tất định:** cùng seed + cùng `generatorVersion` → cùng world. Test EditMode `client/Assets/Game/Tests/World/` (`GoldenHashTests`, `WorldMapGeneratorTests`). Mã băm trùng giữa Mac và điện thoại (build APK bench, tự cài + đọc logcat).
2. **Đo nhiều seed** (≥ 5 seed): mỗi dạng địa hình đo chỉ số kiểm được — diện tích/tỉ lệ, phân bố độ cao và độ dốc, độ rộng dải chuyển tiếp, đường biên có bị cắt thẳng không, khối lơ lửng/lỗ hổng, đường tuyết, tỉ lệ nước. So với mục tiêu trong `terrain_catalog.json`; lệch → chỉnh tham số, chạy lại.
3. **Ràng buộc gameplay:** đường tới khối cốt truyện đi được (tìm đường), dạng địa hình không chặn đường bắt buộc, môi trường sống của loài đạt được.
4. **Hình khối trung gian:** mesh kín ở mọi tổ hợp lân cận + qua biên chunk; số tam giác/chunk vẫn dùng index 16 bit; hiệu năng đo trên điện thoại (bench), không đoán.
5. **Ảnh để user duyệt** (mức `user xem`): menu `Game/Capture TerrainView`, minimap. Liệt kê cụ thể cần nhìn gì (dáng đọc được từ góc camera chơi, chuyển tiếp tự nhiên, dốc nối góc liền, mép nước).
6. Unity dùng chung qua MCP (0010): chỉ chạy `run_tests`/Play khi không có Executor đang chạy brief; Editor tắt thì dùng `tools/unity_test.py`. Sau PlayMode → `git checkout -- client/ProjectSettings/EditorSettings.asset`.

## Công cụ
- `python3 tools/terrain_catalog.py check [design/data/terrain_catalog.json]` — kiểm id trùng, tham chiếu khối/hình/dạng địa hình, hình mới đủ `walkable`/`collider`/`dig_result`, `water_fraction` hợp lệ, hình dốc đủ góc (`needs`), tham số có cờ `tmp`, mặt cắt đều dòng + ký tự có trong `legend`. Exit 1 nếu sai.
- `python3 tools/terrain_catalog.py summary [catalog]` — bảng dạng địa hình × khối, danh sách vật liệu/hình mới phải làm, số tham số `[số tạm]`.
- `python3 tools/terrain_catalog.py section <catalog> <id>` — in mặt cắt ASCII cho user xem nhanh.
- `python3 tools/check_design.py` — cấu trúc design/.
- Test EditMode/PlayMode (MCP `run_tests` hoặc `tools/unity_test.py`), menu `Game/Capture TerrainView`, `Game/Minimap for TerrainView`.

## Cấm
- Không tự đổi nội dung **Chốt** (0011 dữ liệu ô/mesher, 0012/0013 luật nước, 0022) — chỉ đề xuất quyết định mới.
- Không đưa dạng địa hình/khối mới ngoài phạm vi M1 vào brief M1; ghi roadmap/ideas kèm ước lượng.
- Không bịa số hiệu năng, kích thước chunk, mã vật liệu, API Unity — đọc repo hoặc tra docs 6000.4 (ghi link).
- Không hard-code tham số trong code; không đổi output bộ sinh mà không tăng `generatorVersion` + cập nhật mã băm golden có chủ đích.
- Không sao chép khối/texture/bảng màu của game tham khảo (0011) — chỉ lấy nguyên lý.
- Không vẽ tay phần đã quy định sinh theo seed.

## Tiêu chí hoàn thành
- [ ] (A) Mỗi dạng địa hình mới/sửa có: id, khối theo lớp, luật xếp, tham số `[số tạm]`, gameplay, pillar, mặt cắt; mỗi chuyển tiếp có độ rộng + luật trộn.
- [ ] (A) Mỗi hình/vật liệu mới trả lời đủ các ảnh hưởng ở bước A3/A4, có ước lượng công + milestone; phần đụng Chốt đã thành đề xuất quyết định.
- [ ] (A) `python3 tools/terrain_catalog.py check` exit 0; glossary, README, levels/world-tech đã cập nhật liên kết; `python3 tools/check_design.py` exit 0.
- [ ] (B) Mỗi mục kiểm có mức, cách kiểm, kết quả thật + bằng chứng (lệnh/test/ảnh); mục chưa chạy ghi "chưa chạy" kèm lý do.
- [ ] (B) Ảnh cần user xem đã gửi kèm danh sách điểm cần nhìn.
