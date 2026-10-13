# Kho ý tưởng

> Trạng thái: Nháp

<!-- Ý tưởng chưa dùng, bị loại hoặc để sau. Không xoá — chuyển trạng thái. -->

| Ý tưởng | Nguồn | Pillar liên quan | Trạng thái (mới/để sau/loại) | Ghi chú |
|---|---|---|---|---|
| A2 làm dây chuyền tự động hoá phức tạp kiểu factory | AI (brainstorm 2026-10-11) | P2 | loại | User chọn lệnh làm việc chia công đoạn, không AI phức tạp |
| A3 Nhập hồn làm trọng tâm (action RPG) | AI (brainstorm 2026-10-11) | P2 | để sau | Không chọn làm trọng tâm; nhập hồn vẫn là cơ chế phụ trong mechanics.md |
| A4 Ký ức gieo từ hệ sinh thái: mỗi loài hồi sinh trả về một mảnh ký ức | AI (brainstorm 2026-10-11) | P1, P3 | để sau | Chưa chọn; có thể dùng để nối cốt truyện với môi trường sống khi viết plot.md |
| B3 Camera góc nghiêng cố định cho PC + mobile | AI (brainstorm 2026-10-11) | — | để sau | Quyết cùng art-style.md và controls-ui.md |
| Địa hình sinh ngẫu nhiên vô tận | User (mô tả ban đầu) | — | loại | Thay bằng map có ranh giới thêm theo mùa + sinh theo seed |
| Co-op chung tiến trình cốt truyện | AI (brainstorm 2026-10-11) | — | loại | Co-op chỉ ở đảo sandbox hoặc phần truyện không ảnh hưởng tiến trình |
| Co-op đảo sandbox (spin-off) | User (2026-10-11) | — | để sau | User: spin-off, thêm sau; tạm bỏ qua. Cần networking (backend, có thể game server) |
| P4 cũ "Thế giới của riêng mình" (world riêng theo seed, mode nhiều người tách tiến trình) | AI (2026-10-11) | — | loại | User đổi P4 thành chiến đấu; world riêng theo seed vẫn giữ trong mechanics (P3) |
| PvP bất đồng bộ (đấu với đội hình đã lưu, không cần game server) | AI (2026-10-11) | P4 | để sau | User chọn PvP thời gian thực; có thể dùng làm phương án dự phòng khi ít người chơi |
| Ba mức xuất hiện → ghé thăm → định cư (kiểu Viva Piñata) | AI (2026-10-11) | P1 | loại | User thay bằng danh sách yêu cầu của loài (2026-10-12) |
| I1 Chuẩn hoá hoàn toàn chỉ số PvP về chỉ số chuẩn của loài | AI (2026-10-11) | P4 | loại | User chọn giữ phân bố ngẫu nhiên và điểm cấp độ/bậc trong PvP, chỉ bỏ vượt trội (2026-10-12) |
| Chuẩn hoá cấp độ/bậc về ngưỡng PvP (đồng bộ cấp độ) | AI (2026-10-11) | P4 | loại | User: điểm cấp độ/bậc vẫn tính vì ai rồi cũng đạt được (2026-10-12) |
| I3 Trần chỉ số cá thể đạt được bằng chơi/tiền (kiểu Hyper Training) | AI (2026-10-11) | P4 | loại | Mua bằng tiền nạp được mức vượt người không nạp → vi phạm nguyên tắc chặn ở mức tối đa |
| I4 Chỉ số cá thể chỉ cho làm việc, chiến đấu cố định theo loài | AI (2026-10-11) | P2 | loại | User tách hai bảng chỉ số, cả hai đều có phần riêng của cá thể |
| I5 Hai thể thức PvP: xếp hạng chuẩn hoá, tự do dùng đủ chỉ số (cả vượt trội) | AI (2026-10-11) | P4 | để sau | Thể thức tự do cho dùng phần vượt trội → không công bằng nếu tính điểm xếp hạng; có thể làm thể thức giao hữu không xếp hạng |
| I6 "Nét riêng" cân bằng thay cho con số | AI (2026-10-11) | P2, P4 | để sau | Tốn công cân bằng; có thể bổ sung sau cho cá thể vượt trội |
| PvP thời gian thực, người chơi làm host (relay) | AI (2026-10-11) | P4 | loại | Host có thể gian lận; user chọn game server quyết định kết quả |
| Tạo 3D bằng Hyper3D Rodin / Hunyuan3D cloud qua blender-mcp | AI (2026-10-12) | — | loại | Cần API key riêng; user không dùng key dịch vụ (sợ hết quota giữa chừng). Chọn HF Spaces |
| Tạo 3D local bằng AssetFurnace (Pixal3D, TRELLIS.2 port Mac) | AI (2026-10-12) | — | loại | README khuyên 32 GB (máy 16 GB); Pixal3D/TRELLIS cần Xcode đầy đủ + Metal 4 (`xcrun metal -std=metal4.0`), có thể cần macOS 26 [run 2026-10-12]; DINOv3 bị khoá. Xét lại nếu đổi máy |
| Hunyuan3D-2 (local hoặc qua AssetFurnace) | AI (2026-10-12) | — | loại | Giấy phép loại trừ EU, Anh, Hàn Quốc và cấm dùng output ngoài vùng → không hợp game phát hành toàn cầu |
| TRELLIS.2 trên Google Colab / Kaggle miễn phí | User (2026-10-12) | — | loại | GPU free 16 GB (T4/P100) < 24 GB TRELLIS.2 yêu cầu; phải build lại extension mỗi phiên |
| TRELLIS.2 qua fal.ai (0,25–0,35 $/lần) | User (2026-10-12) | — | để sau | Cần tài khoản + credit; dùng khi quota HF free không đủ cho sản xuất (M2) |
| blenderwright (MCP Blender 191 tool, MIT) | User (2026-10-12) | — | để sau | Không thêm khả năng so với blender-mcp + Python; xét lại nếu rig bằng Python hay lỗi |
| blender-open-mcp, TripoSR | User (2026-10-12) | — | loại | LLM local không cần (đã dùng Claude Code); TripoSR cần CUDA, không rig |
| BlenderKit cho đồ vật | User (2026-10-12) | — | để sau | Asset Royalty Free/CC0 dùng thương mại được, không được bán lại model; xét ở M2 |
| Biến cố phép thuật chung (không ai gây ra) | AI (2026-10-13) | P1 | loại | User chọn: nhân vật chính liên quan |
| Con người gây ra biến cố (khai thác, chiến tranh phép thuật) | AI (2026-10-13) | P1 | loại | User chọn: nhân vật chính liên quan |
| Pháp sư: sai lầm có ý tốt / hy sinh ký ức / tự xoá ký ức | AI (2026-10-13) | — | loại | User chọn: bản ngã cũ có mục đích xấu, coi sinh vật là công cụ |
| Kết "Đoàn tụ" (sinh vật thân thiết trở lại từ tinh linh); nhiều kết theo cách đối xử với sinh vật | AI (2026-10-13) | P2 | để sau | User chọn kết "Chấp nhận"; nhiều kết cần cơ chế đo cách đối xử |
| NPC người sống sót dẫn dắt ở mỗi map | AI (2026-10-13) | P3 | loại | User: có NPC nhưng không phải người (sinh vật đặc biệt, máy móc, pho tượng) |
| Địa hình dựng mesh ×1 (hoặc ×1 làm mượt ít hơn ở vách) | AI (2026-10-12) | — | loại | Rãnh 1 ô bị bóp chữ V; user chọn ×2 |
