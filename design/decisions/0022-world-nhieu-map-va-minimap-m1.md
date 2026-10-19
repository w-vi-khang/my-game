# 0022 — World liền một khối chia nhiều map bằng ranh giới cong, minimap phóng to được, nạp dần chunk (M1)

> Trạng thái: Chốt
> Ngày: 2026-10-19
> Thay đổi: [0009](0009-dai-duong-va-suong-mu-o-ria-map.md) — "mọi map có đại dương bao quanh" → **đại dương bao quanh cả world**, các map giáp nhau trên đất liền (phần sương mù ở rìa xa giữ nguyên). [0007](0007-m1-toi-gian.md) — M1 thêm hạng mục world nhiều map + minimap + nạp dần chunk; "map nhỏ cố định 64 × 64 × 32" thay bằng world 3 map; "map 128 × 128 × 48 + hiển thị theo khoảng cách" chuyển từ M2 về M1.

## Bối cảnh
User (2026-10-19), sau khi xong simulation bước 6b: "tập trung vào build world, chia ranh giới map, … load được mini map và player có thể phóng to mini map". Minimap không cần dựng toàn bộ world, chỉ cần thấy toàn cảnh địa hình nhìn từ trên cao (tô màu theo tầng độ cao), thấy ranh giới các map, mỗi map có một tên. Ranh giới là đường cong ngẫu nhiên, không phải hình chữ nhật.

Vướng với thiết kế đã Chốt:
- [0007](0007-m1-toi-gian.md): M1 chỉ có một map 64 × 64 × 32 cố định. Bảng "Bản đồ" là M2 ([controls-ui](../02-gameplay/controls-ui.md)), Map 2–3 là M4 ([levels](../02-gameplay/levels.md)).
- [0009](0009-dai-duong-va-suong-mu-o-ria-map.md): mọi map có đại dương bao quanh, tức mỗi map là một đảo.

## Các lựa chọn
Milestone:
- (a) **Thêm vào M1**, làm ngay.
- (b) Chỉ viết thiết kế bây giờ, làm ở M2.

Bố cục world:
- (a) **Liền một khối:** các map giáp nhau bằng đường cong, đại dương chỉ bao quanh cả world. Phải sửa 0009.
- (b) Mỗi map là một đảo (giữ 0009), ranh giới cong chính là bờ đảo.

Kích thước: 3 map trên world 256 × 256 · **3 map, mỗi map khoảng 128 × 128** · 5–7 map nhỏ hơn.

Tên map: **tên tạm trong JSON** · user đặt tên ngay.

Cách làm minimap (đề xuất của AI):
- A. Ảnh cố định tạo từ bộ sinh: rẻ nhất, nhưng đào/đắp không làm minimap đổi.
- B. Chỉ vẽ vùng đã nạp: luôn đúng, nhưng không thấy toàn cảnh và phải lưu ảnh.
- C. **Lai:** nền lấy từ bộ sinh như A, đào/đặt thì vẽ lại đúng cột bị sửa, cộng thêm dấu mốc.

Phần voxel trong M1:
- Dựng một vùng cố định, việc nạp dần để M2.
- **Nạp dần chunk quanh người chơi ngay ở M1.**

## Quyết định
**Chốt (user, 2026-10-19):**
1. **Làm ở M1**, ngay sau simulation bước 6b (trước bước 6a). Thứ tự M1 mới: **world bước 7 → sim 6a → sim 7 → sim 8**.
2. **World liền một khối, gồm 3 map giáp nhau.** Ranh giới giữa các map là **đường cong ngẫu nhiên**, sinh tất định theo seed. Đại dương và tường sương mù ([0009](0009-dai-duong-va-suong-mu-o-ria-map.md)) bao quanh **cả world**, không bao từng map.
3. **Mỗi map khoảng 128 × 128 ô** (đúng [levels](../02-gameplay/levels.md#kích-thước--khung-chung)). Kích thước cụ thể của world (kể cả vành biển) và chiều cao do bộ sinh quyết định: xem đề xuất ở [world-tech](../04-tech/world-tech.md#world-nhiều-map--minimap-m1).
4. **Tên map: tên tạm** ("Map 1", "Map 2", "Map 3") trong file cấu hình JSON. Tên thật do user đặt sau qua [naming](../01-story/naming.md), khi đó chỉ cần sửa JSON.
5. **Minimap theo cách C (lai):**
   - Lớp nền là độ cao mặt đất của từng cột, tính thẳng từ bộ sinh, không cần dựng khối voxel. Màu theo **tầng độ cao**, nước có màu riêng. Ảnh nền tạo lại được từ seed nên không cần lưu.
   - Đào/đặt khối → tính lại độ cao **của cột đó** rồi sửa đúng điểm ảnh tương ứng.
   - Có ranh giới map, tên map, vị trí người chơi và dấu mốc. Người chơi **phóng to và kéo** được.
6. **Nạp dần chunk quanh người chơi ngay ở M1:** chỉ những chunk trong bán kính quanh camera mới có mesh và collider.

## Hệ quả
- Pillar: **P1** (minimap cho thấy thế giới đã được cải tạo), **P3** (nhiều map, mỗi map có câu chuyện riêng, đi lại tự do). Thành phần: client Unity ([components](../04-tech/components.md)); không thêm thành phần mới.
- Ước lượng của AI (chưa đo) — **9–15 ngày-người**, chia bước ở [world-tech](../04-tech/world-tech.md#world-nhiều-map--minimap-m1):
  - bộ sinh world nhiều map (generatorVersion 3): 2–3
  - nạp dần chunk + đo trên điện thoại: 4–6 (chuyển từ M2, nơi ghi "map 128 × 128 × 48 + hiển thị theo khoảng cách 4–6")
  - minimap (nền, ranh giới, tên, phóng to/kéo): 2–3,5
  - minimap cập nhật khi đào/đặt + dấu mốc: 0,5–1
  - mô phỏng chạy trên world lớn: 0,5–1,5
- Roadmap: M1 61,5–99,5 → **70,5–114,5**, M2 326–621 → **322–615**, tổng M1–M4 thêm 5–9 ([roadmap](../05-roadmap.md)).
- Rủi ro [chưa xác minh]:
  - **Bộ nhớ của mô phỏng.** `GridPathfinder` cấp mảng theo số ô của cả world, nên world 7 triệu ô sẽ cần khoảng 85 MB chỉ cho `g`, `parent`, `seenStamp` (3 × 4 byte × 7 triệu). Phải giới hạn tìm đường trong vùng mô phỏng, hoặc cấp mảng theo vùng. Chốt cách làm khi viết brief.
  - **Hiệu năng nạp dần chunk** trên điện thoại (dựng mesh khi người chơi di chuyển): đo ở bước nạp dần, theo [risks](../04-tech/risks.md).
- Để sau, ghi ở [ideas](../ideas.md): sương che vùng chưa khám phá trên minimap.
- Đã cập nhật: [levels](../02-gameplay/levels.md#kích-thước--khung-chung), [controls-ui](../02-gameplay/controls-ui.md), [world-tech](../04-tech/world-tech.md), [roadmap](../05-roadmap.md), [glossary](../glossary.md), [decisions/README](README.md).
