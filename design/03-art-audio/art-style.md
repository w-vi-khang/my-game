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
- Câu hỏi mở:
  - **Địa hình** (đất, đá, nước) hiển thị thế nào: khối vuông có bo/vát cạnh và texture mềm, hay bề mặt mịn liền (vd kỹ thuật marching cubes — đổi lớn cho phần dựng mesh)?
  - Bảng màu, độ bão hoà, ánh sáng (ấm/dịu kiểu "cozy"?); góc camera (tự do, góc nghiêng cố định — ý tưởng B3 trong [ideas](../ideas.md)).
  - Tỉ lệ sinh vật so với ô voxel (1 ô ≈ bao nhiêu mét; sinh vật nhỏ nhất/lớn nhất chiếm mấy ô).

## Tham khảo
<!-- Ảnh tham khảo: chỉ ghi link (không chép ảnh có bản quyền vào repo) + điều muốn lấy. -->
| Tham khảo | Lấy | Không lấy |
|---|---|---|
| Pokémon Pokopia — Switch 2, phát hành 05/03/2026 ([Nintendo](https://www.nintendo.com/en-gb/Games/Nintendo-Switch-games/Game-2915161.html), [Bulbapedia](https://bulbapedia.bulbagarden.net/wiki/Pok%C3%A9mon_Pokopia)) | Thế giới voxel mịn; sinh vật, cây, vật phẩm là model mịn trên lưới (theo mô tả của user — AI chưa xem được hình ảnh để xác minh chi tiết) | Nhân vật, sinh vật, tên, thiết kế có bản quyền của Pokémon |
| Minecraft | — | Sinh vật/vật phẩm low-poly khối vuông (user loại) |

## Nguồn asset
<!-- Tự làm / Asset Store / thuê ngoài / AI; giấy phép; chi phí. -->
- User không tự làm art (2026-10-12) → asset có sẵn, thuê ngoài, hoặc AI hỗ trợ (Blender MCP — chờ thử nghiệm, xem đề xuất trong [roadmap](../05-roadmap.md)).
- Tiêu chí chọn asset: hỗ trợ **URP** ([0005](../decisions/0005-render-pipeline.md)); giấy phép cho phép dùng thương mại; giá có nguồn; phong cách mịn hợp bảng màu chung.
- Lưu ý chi phí: sinh vật mịn (có rig + animation) tốn công/tiền hơn sinh vật voxel khối — ảnh hưởng ước lượng "tích hợp asset" ở M2 ([roadmap](../05-roadmap.md)). Chi tiết _(chưa có)_.

## Quy cách kỹ thuật
<!-- Độ phân giải sprite / polycount, kích thước texture, palette, quy ước đặt tên file, pivot, scale. -->
_(chưa có)_ — cần sau khi chốt câu hỏi địa hình và tỉ lệ ô voxel: kích thước ô, số polygon/texture cho mobile, quy ước khung chiếm chỗ (định nghĩa trong dữ liệu, gắn với model).
