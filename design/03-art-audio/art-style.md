# Phong cách hình ảnh

> Trạng thái: Nháp
> Liên quan: [Vision](../00-vision.md) · [Công nghệ](../04-tech/tech-stack.md)

Thống nhất art style trước khi bắt đầu M1 (user, 2026-10-12) — xem B7 trong [roadmap](../05-roadmap.md#công-việc-m0-kế-hoạch--thiết-kế--ưu-tiên-hiện-tại).

## Hướng phong cách
<!-- 3–5 tính từ mô tả cảm giác; 2D/3D, góc camera, bảng màu, độ chi tiết. Chọn phong cách vừa sức một người làm. -->
Mô tả của user (2026-10-12):
- **Voxel mịn**, tham khảo Pokémon Pokopia (xem Tham khảo).
- **Sinh vật, cây cối, vật phẩm KHÔNG làm low-poly khối vuông kiểu Minecraft.** Chúng là model có hình dạng riêng (mịn).
- Mỗi sinh vật/cây/vật phẩm có một **khung chiếm chỗ** gồm nhiều ô voxel nối với nhau. Khung chỉ biểu thị **lượng không gian** nó chiếm trên lưới, không quyết định hình dạng hiển thị.

Hệ quả cho thiết kế [ý tưởng]:
- Lưới voxel là lưới chung cho địa hình **và** chiếm chỗ: đặt vật phẩm/cây = chiếm các ô trong khung; tìm đường coi các ô đó là vật cản ([tech-stack](../04-tech/tech-stack.md)).
- Phần hiển thị (model mịn) tách khỏi khung chiếm chỗ → đổi model không ảnh hưởng luật chơi; hợp nguyên tắc tách logic khỏi hiển thị ([kiến trúc](../04-tech/architecture.md#client-unity)).
- **Địa hình: theo Pokopia** (user, 2026-10-12). AI quan sát ảnh Pokopia trên [Serebii](https://serebii.net/pokemonpokopia/pics.shtml) (2026-10-12):
  - Địa hình là **khối lập phương trên lưới**, thấy rõ đường nối giữa các khối — vẫn là voxel khối, **không** phải bề mặt mịn liền.
  - Mỗi mặt khối có texture mềm, vẽ tay (cát, gạch đá, đất); khối đá hơi vát cạnh.
  - Khối cỏ có **mép cỏ gợn sóng phủ xuống** cạnh khối → bớt cảm giác vuông cứng.
  - Nước: khối phẳng trong suốt màu xanh sáng.
  - Sinh vật, người chơi (chibi đầu to), cây (cọ), đồ vật (giỏ, chai, nhà mái cong) là **model mịn** đặt trên lưới.
  - Ánh sáng sáng, bóng mềm, bảng màu tươi/pastel.
  → Hệ quả kỹ thuật: dựng mesh chunk dạng khối vẫn đúng hướng (Jobs + Burst, [tech-stack](../04-tech/tech-stack.md)); thêm **lớp trang trí** cho mép khối (mép cỏ, vát cạnh) và shader texture mềm.
- Câu hỏi mở:
  - Bảng màu, độ bão hoà, ánh sáng (ấm/dịu kiểu "cozy"?); góc camera (tự do, góc nghiêng cố định — ý tưởng B3 trong [ideas](../ideas.md)).
  - Tỉ lệ sinh vật so với ô voxel (1 ô ≈ bao nhiêu mét; sinh vật nhỏ nhất/lớn nhất chiếm mấy ô).

## Tham khảo
<!-- Ảnh tham khảo: chỉ ghi link (không chép ảnh có bản quyền vào repo) + điều muốn lấy. -->
| Tham khảo | Lấy | Không lấy |
|---|---|---|
| Pokémon Pokopia — Switch 2, phát hành 05/03/2026 ([Nintendo](https://www.nintendo.com/en-gb/Games/Nintendo-Switch-games/Game-2915161.html), [Bulbapedia](https://bulbapedia.bulbagarden.net/wiki/Pok%C3%A9mon_Pokopia)) | Địa hình khối có texture mềm, mép cỏ phủ cạnh; sinh vật, cây, vật phẩm là model mịn trên lưới (AI đã xem ảnh trên [Serebii](https://serebii.net/pokemonpokopia/pics.shtml)) | Nhân vật, sinh vật, tên, thiết kế có bản quyền của Pokémon |
| Minecraft | — | Sinh vật/vật phẩm low-poly khối vuông (user loại) |

## Nguồn asset
<!-- Tự làm / Asset Store / thuê ngoài / AI; giấy phép; chi phí. -->
- User không tự làm art (2026-10-12) → asset có sẵn, thuê ngoài, hoặc AI hỗ trợ (Blender MCP — chờ thử nghiệm, xem đề xuất trong [roadmap](../05-roadmap.md)).
- Tiêu chí chọn asset: hỗ trợ **URP** ([0005](../decisions/0005-render-pipeline.md)); giấy phép cho phép dùng thương mại; giá có nguồn; phong cách mịn hợp bảng màu chung.
- Lưu ý chi phí: sinh vật mịn (có rig + animation) tốn công/tiền hơn sinh vật voxel khối — ảnh hưởng ước lượng "tích hợp asset" ở M2 ([roadmap](../05-roadmap.md)).

**Ứng viên trên Unity Asset Store** (tra 2026-10-12 qua tìm kiếm web; giá, version, URP phải kiểm lại trên trang trước khi mua) [ý tưởng]:

| Asset | Dùng cho | Ghi chú từ trang/kết quả tìm kiếm |
|---|---|---|
| [KUBIKOS – 3D Cube World](https://assetstore.unity.com/packages/3d/environments/kubikos-3d-cube-world-117341) | Khối địa hình + cây/đồ vật kiểu cube world dễ thương | Ghi hỗ trợ URP, Unity 6; giá $24.99 (theo kết quả tìm kiếm) |
| [3D Modular World – Cube World Vol.1](https://assetstore.unity.com/packages/3d/environments/3d-modular-world-cube-world-vol1-251828) | Khối địa hình | Làm trên URP; khối khớp mỗi 2 đơn vị; có khối đất phủ cỏ, nước động |
| [Monsters Ultimate Pack 01 – Cute Series](https://assetstore.unity.com/packages/3d/characters/creatures/monsters-ultimate-pack-01-cute-series-167028) | Sinh vật mịn dễ thương | 25 quái, mỗi con 3 dạng tiến hoá, có rig + animation — hợp cơ chế tiến hoá; giao ở Built-in, cần nâng shader lên URP |
| [Monster Pack Vol1](https://assetstore.unity.com/packages/3d/characters/creatures/monster-pack-vol1-261883) | Sinh vật | 10 quái, ghi tương thích Built-in/URP/HDRP |

Rủi ro khi dùng asset có sẵn: nhiều tác giả → **lệch phong cách**; sinh vật trong pack bán rộng rãi → **game khác cũng dùng**, giảm nét riêng; số loài cần nhiều hơn số con trong một pack. [ý tưởng] Dùng asset cho địa hình/đồ vật; sinh vật chủ lực đặt làm riêng hoặc làm bằng AI + Blender (chờ thử nghiệm).

## Quy cách kỹ thuật
<!-- Độ phân giải sprite / polycount, kích thước texture, palette, quy ước đặt tên file, pivot, scale. -->
_(chưa có)_ — cần sau khi chốt tỉ lệ ô voxel: kích thước ô, số polygon/texture cho mobile, quy ước khung chiếm chỗ (định nghĩa trong dữ liệu, gắn với model).
