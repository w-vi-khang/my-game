# Tóm tắt game

> Trạng thái: Nháp
> Liên quan: [Vision](00-vision.md) · [Cơ chế](02-gameplay/mechanics.md) · [Đặt tên](01-story/naming.md) · [Thuật ngữ](glossary.md)

Bản tóm tắt một trang (2026-10-12) để nhìn nhanh toàn bộ game và làm **brief đặt tên**. Nguồn chuẩn vẫn là các file chi tiết — khi khác nhau, file chi tiết đúng. Tên trong ngoặc kép (vd "thẻ bài") là tên thiết kế nội bộ, chưa phải tên trong game.

## Game là gì
Một pháp sư mất ký ức hồi sinh một thế giới phép thuật: cải tạo đất đai cho đúng **môi trường sống** của từng loài để gọi các **sinh vật** thất lạc trở về, rồi cùng chúng vận hành **nông trại tự động**, và chiến đấu bằng **bộ phép của sinh vật** — kể cả **nhập hồn** vào chúng.

- **Thể loại:** thế giới voxel (đào/đặt) + sưu tầm sinh vật + nông trại tự động hoá + phiêu lưu cốt truyện.
- **Nền tảng, thị trường:** PC + mobile; toàn cầu; online, miễn phí (F2P) có nạp tiền.
- **Câu chuyện:** chính pháp sư — trước khi mất ký ức — đã coi sinh vật là công cụ và gây ra việc chúng thất lạc. Một **tinh linh** (mảnh linh hồn của sinh vật từng thân thiết) đi theo để cảm hoá. Lấy lại ký ức → biết trân trọng sinh vật → trận chiến cuối với **bản ngã cũ**. Tông ấm áp, hy vọng.
- **Khác Pokémon Pokopia:** nhiều cá thể khác nhau trong mỗi loài; chiến đấu và nông trại chi tiết hơn; Pokopia ưu tiên khám phá cốt truyện.

## 4 trụ cột (pillars)
| | Trụ cột | Nghĩa |
|---|---|---|
| P1 | Hồi sinh để sưu tầm | Loài chỉ trở lại khi người chơi cải tạo thế giới đúng môi trường sống của nó |
| P2 | Sinh vật là cốt lõi | Mỗi sinh vật có giá trị ở cả nông trại lẫn chiến đấu |
| P3 | Tự do trong khung truyện | Có cốt truyện, nhiệm vụ nhưng không ép thứ tự |
| P4 | Chiến đấu bằng bộ phép của sinh vật | Sức mạnh đến từ chọn sinh vật và gán phép; nhập hồn để dùng phép |

## Vòng lặp chính
Khám phá, thu thập → cải tạo địa hình → hoàn thành **danh sách yêu cầu của loài** → loài trở lại → bắt bằng **thẻ bài** → giao **lệnh làm việc** ở nông trại → sản lượng → mở rộng → cần thêm loài. Song song: chiến đấu (nhập hồn, phép), cốt truyện (**mảnh ký ức**, nhiệm vụ theo map), PvP sau khi đạt ngưỡng.

## Các hệ thống chính
**Thế giới**
- World riêng của mỗi người chơi, chia **map** có ranh giới (3 map lúc đầu, thêm theo mùa). Phần cốt truyện sắp đặt tay (**khối cốt truyện**), phần còn lại sinh theo **seed**.
- Map 128 × 128 × 48 ô, có **hang động** (sinh vật, tài nguyên riêng), **nước chảy** (không cạn; người chơi tạo được nguồn nước); đào/đặt theo nguyên ô.
- Hình ảnh: địa hình **dạng khối bo cạnh, mép cỏ phủ** (dữ liệu là ô voxel — [0011](decisions/0011-dia-hinh-khoi-bo-canh.md)), màu tươi sáng, concept art kiểu diorama; sinh vật/nhân vật có thể low-poly, không chibi. 1 ô = 1 đơn vị; nhân vật cao 1,5.

**Sinh vật**
- Mỗi **loài** có **nội tại**, **tộc**, **nguyên tố** (đơn/đôi), 2 **năng lực làm việc**, **danh sách yêu cầu** (mở khoá loài + tăng **mức đáp ứng**; quyết định cả cấp độ sinh vật ngoài tự nhiên).
- Mỗi **cá thể** khác nhau: **phân bố ngẫu nhiên** chỉ số, **điểm cộng cá thể** → **phân hạng cá thể** (cao nhất là **vượt trội**: ≥ 20% base), **biến thể** (màu sắc, hình thái hiếm), **biến thể hiển thị** (khác biệt nhỏ ở mọi cá thể).
- Hai bảng chỉ số: **làm việc** (tốc độ, sức khoẻ, độ thuần thục ×2) và **chiến đấu** (máu, giáp, kháng phép, tốc độ đánh, sát thương...). Điểm từ **cấp độ**/**bậc** người chơi tự phân bổ.
- **Tiến hoá** tăng bậc. **Nhu cầu sinh vật** đáp ứng tốt → ít **bệnh**, ít **mệt**, làm tốt hơn, đôi khi tặng quà.

**Thẻ bài & bắt sinh vật**
- Mỗi cá thể gắn với một **thẻ bài**; **kho sinh vật** chứa thẻ. Có **giới hạn sinh vật** hoạt động (tối đa 54; 60 nếu nạp).
- **Ném thẻ** để bắt: ném hụt → tỉ lệ nhỏ mất thẻ; bắt không thành → mất thẻ (thẻ vỡ). Tỉ lệ bắt theo chênh lệch cấp độ, bậc sinh vật, **bậc thẻ bài**.
- 4 bậc thẻ: bậc 1–3 khác nhau tỉ lệ bắt và tỉ lệ vượt trội; **bậc 4 chắc chắn bắt, chắc chắn vượt trội, có hiệu ứng** (hào quang, mây...). Ấp nở: gắn thẻ vào trứng trước khi nở.

**Nông trại**
- **Lệnh làm việc** chia **công đoạn** (xới, gieo, tưới, thu hoạch, vận chuyển); sinh vật rảnh có năng lực phù hợp nhận việc. Không tới được → **đình trệ**. Vắng mặt → **tua nhanh** theo giờ server.

**Phép, chiến đấu, trang bị**
- Hai bộ sưu tầm: **phép của pháp sư** (khám phá, phát triển) và **phép sinh vật** (chiến đấu). **Nhóm phép**: phổ biến, tộc, nguyên tố, loài. **Ô phép**: 3 (bậc 1) / 4 (bậc 2+, có **ô đặc biệt**). **Mana** dùng chung.
- **Nhập hồn**: người chơi nhập vào sinh vật, dùng bộ phép của nó.
- **Trang bị**: 3 món mỗi sinh vật; **bậc trang bị**; **ghép trang bị** cùng loại cùng bậc → bậc cao hơn; **rèn** ở **lò rèn**.

**PvP & kinh tế**
- PvP 1v1 thời gian thực (sau phát hành), 3–5 sinh vật theo **thể thức**; ghép theo **hạng** rồi **cấp độ trung bình**. Trong PvP bỏ điểm cộng cá thể (vượt trội) — ở mức tối đa mọi người như nhau.
- Hai loại tiền: **tiền trong game**, **tiền nạp**. Tiền nạp ảnh hưởng tốc độ, không vượt mức tối đa người không nạp đạt được ([0004](decisions/0004-tien-nap.md)). **Rút thưởng gói** (sinh vật, sách phép, trang bị) có **bảo hiểm** công khai; **chặn quá may**; phép trùng đổi thành **"ma năng"**.

**Trạng thái:** M1 — prototype client tối giản (từ 2026-10-13, [0007](decisions/0007-m1-toi-gian.md)). Ước lượng tới M4 ~12,3–23,0 năm ở 10 giờ/tuần, chưa tính tác động của AI ([roadmap](05-roadmap.md)).

## Danh sách cần đặt tên
**User tự đặt tên; AI chỉ duyệt** (kiểm trùng web + `check_names.py`, cách đọc tiếng Việt/tiếng Anh). Nguyên tắc chung đã chốt: tên riêng **tự tạo**, dễ đọc cả tiếng Việt và tiếng Anh, chữ a–z, thị trường toàn cầu ([naming](01-story/naming.md#nguyên-tắc-chung)). Cột "Loại" là gợi ý của AI [ý tưởng]: **tự tạo** = giữ nguyên mọi ngôn ngữ; **dịch** = tên mô tả, dịch theo ngôn ngữ người chơi.

**Ưu tiên trước M1**
| Mục | Là gì / ngữ cảnh để đặt tên | Số lượng | Loại |
|---|---|---|---|
| Tên game | Hồi sinh thế giới phép thuật, sinh vật trở về; **đã chọn: Viventa** (user, 2026-10-13); còn tra trùng store/nhãn hiệu ([đặt tên](01-story/naming.md#tên-game)) | 1 | Tự tạo (≤ 12 ký tự) |
| Tên tạm 3 loài cho prototype | M1 dùng L1, L2, L3 của Map 1 ([levels](02-gameplay/levels.md#map-1--đảo-vách-đá-biển), [0007](decisions/0007-m1-toi-gian.md)) — chưa cần tên chính thức | 3 | Tạm |

**M2 — vertical slice**
| Mục | Là gì / ngữ cảnh để đặt tên | Số lượng | Loại |
|---|---|---|---|
| Thế giới | Toàn bộ world; thế giới phép thuật đã khô héo, sinh vật thất lạc | 1 | Tự tạo |
| Map / vùng đất | Mỗi map có câu chuyện riêng, khí hậu riêng; map của vertical slice (Map 1 — đảo vách đá, biển, gồm Mở đầu) đặt trước ([levels](02-gameplay/levels.md)) | 3 lúc đầu | Tự tạo (≤ 16) |
| Vùng, địa danh trong map | Vùng môi trường sống, khối cốt truyện | Nhiều | Tự tạo hoặc dịch |
| Tên gọi chung cho "sinh vật" | Từ người chơi dùng để gọi mọi sinh vật (như "Pokémon", "Pal") | 1 | Tự tạo |
| Tộc | Nhóm nhiều loài; gợi ý: mỗi tộc một âm đuôi dùng lại trong tên loài | Chưa định số | Tự tạo (≤ 10) |
| Nguyên tố | Bộ nguyên tố chưa định (lửa, nước, cỏ...?) | Chưa định | Dịch |
| Loài | Theo tộc, nguyên tố, nội tại, map | ~10 (slice) | Tự tạo (≤ 12) |
| Phân hạng cá thể | 4 hạng chất lượng cá thể (3 hạng thường + "vượt trội" ≥ 20% base), khớp 4 bậc thẻ | 4 | Dịch |
| "Phép sinh vật", "phép của pháp sư", "nhập hồn" | Tên hiển thị của 2 loại phép và hành động nhập vào sinh vật | 3 | Dịch hoặc tự tạo |
| Từng phép | ~20 phép sinh vật + ~8 phép pháp sư (slice) | ~28 | Dịch (≤ 16) |
| Mức độ hiếm, nhóm phép | Các mức hiếm của phép; nhóm: phổ biến, tộc, nguyên tố, loài | Chưa định | Dịch |
| Danh xưng người chơi | "Pháp sư"; người chơi tự đặt tên nhân vật? | 1 | Dịch |
| Tinh linh | Đồng hành luôn đi theo người chơi; mảnh linh hồn của sinh vật từng thân thiết với pháp sư ([characters](01-story/characters.md)) | 1 | Tự tạo |
| Sinh vật thân thiết | Người bạn duy nhất của bản ngã cũ, nguồn gốc của tinh linh; xuất hiện qua mảnh ký ức | 1 | Tự tạo |
| Biến cố | Sự kiện bản ngã cũ rút sinh khí làm thế giới khô héo ([world](01-story/world.md)) | 1 | Tự tạo hoặc dịch |
| NPC | Không phải người: sinh vật đặc biệt, máy móc, pho tượng; 1–2 mỗi map (ý tưởng) | ~3–6 | Tự tạo |
| Mana | Năng lượng dùng phép, dùng chung khi nhập hồn | 1 | Tự tạo hoặc dịch |
| **"Ma năng" — cần đổi** | Tài nguyên đổi từ phép trùng; dễ nhầm với mana | 1 | Tự tạo (≤ 10) |
| Thẻ bài + 4 bậc thẻ | Vật chứa sinh vật, ném để bắt; bậc 4 chắc chắn bắt + vượt trội + hiệu ứng | 1 + 4 | Dịch hoặc tự tạo |
| Cơ chế hiển thị | Lệnh làm việc, năng lực làm việc, độ thuần thục, danh sách yêu cầu của loài, nhu cầu sinh vật, mức đáp ứng, vượt trội, biến thể, bảo hiểm, mảnh ký ức, kho sinh vật, túi đồ, tiến hoá / bậc | ~14 | Dịch (có thể giữ tên thiết kế) |
| Vật phẩm | Nguyên liệu rèn, vật phẩm phân bổ lại, vật phẩm hỗ trợ, quà từ sinh vật | 4+ | Dịch |
| Công trình | Kho, lò rèn, công trình khác (chưa có danh sách) | Chưa định | Dịch |

**M3 trở đi**
| Mục | Là gì / ngữ cảnh để đặt tên | Số lượng | Loại |
|---|---|---|---|
| Tiền trong game, tiền nạp | Hai loại tiền | 2 | Tự tạo (≤ 10) |
| Gói cửa hàng | Gói sinh vật, gói sách phép, gói trang bị | 3 | Dịch |
| Trang bị | Tên chung, các bậc trang bị, từng trang bị | Chưa định | Dịch |
| Biến thể | Dạng đặc biệt về màu, hình thái của từng loài | Vài mỗi loài | Dịch (tên loài + tính từ) |
| Hạng PvP | Các hạng xếp hạng | Chưa định | Dịch hoặc tự tạo |
