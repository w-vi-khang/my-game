# Phong cách hình ảnh

> Trạng thái: Nháp
> Liên quan: [Vision](../00-vision.md) · [Công nghệ](../04-tech/tech-stack.md)

Thống nhất art style trước khi bắt đầu M1 (user, 2026-10-12) — xem B7 trong [roadmap](../05-roadmap.md#công-việc-m0-kế-hoạch--thiết-kế--ưu-tiên-hiện-tại).

## Hướng phong cách
<!-- 3–5 tính từ mô tả cảm giác; 2D/3D, góc camera, bảng màu, độ chi tiết. Chọn phong cách vừa sức một người làm. -->
Quyết định của user (2026-10-12):
- **Địa hình: bề mặt mịn liền**, không thấy cạnh khối. Dữ liệu bên dưới vẫn là lưới voxel (đào/đặt, chiếm chỗ, tìm đường theo ô) — chỉ cách hiển thị là mịn.
- **Sinh vật, cây cối, vật phẩm:** model mịn có hình dạng riêng, **không** low-poly khối vuông kiểu Minecraft.
- **Nhân vật và sinh vật không làm chibi** (tỉ lệ cơ thể thường, không đầu to).
- **Biến thể hiển thị:** các cá thể/vật thể cùng loại không render giống hệt nhau — mỗi cái có khác biệt nhỏ ngẫu nhiên.
- Mỗi sinh vật/cây/vật phẩm có **khung chiếm chỗ** gồm nhiều ô voxel nối với nhau; khung chỉ biểu thị không gian chiếm trên lưới, không quyết định hình dạng hiển thị.

Hệ quả cho thiết kế [ý tưởng]:
- Lưới voxel là lưới chung cho địa hình **và** chiếm chỗ: đặt vật phẩm/cây = chiếm các ô trong khung; tìm đường coi các ô đó là vật cản ([tech-stack](../04-tech/tech-stack.md)).
- Model mịn tách khỏi khung chiếm chỗ → đổi model không ảnh hưởng luật chơi ([kiến trúc](../04-tech/architecture.md#client-unity)).
- Biến thể hiển thị: mỗi cá thể/vật thể có một seed riêng → chọn trong khoảng cho phép: tỉ lệ kích thước (vd ±10%), sắc màu, hoa văn/texture phụ, hình dạng phụ (blend shape). Câu hỏi mở: biến thể của sinh vật có gắn với chỉ số cá thể không (vd cá thể vượt trội có dấu hiệu nhìn thấy được)?
- Câu hỏi mở: bảng màu, độ bão hoà, ánh sáng; góc camera (tự do hay góc nghiêng cố định — ý tưởng B3 trong [ideas](../ideas.md)).

## Tham khảo
<!-- Ảnh tham khảo: chỉ ghi link (không chép ảnh có bản quyền vào repo) + điều muốn lấy. -->
| Tham khảo | Lấy | Không lấy |
|---|---|---|
| Pokémon Pokopia — Switch 2, phát hành 05/03/2026 ([Nintendo](https://www.nintendo.com/en-gb/Games/Nintendo-Switch-games/Game-2915161.html), [ảnh trên Serebii](https://serebii.net/pokemonpokopia/pics.shtml)) | Cảm giác tươi sáng; vật thể là model mịn đặt trên lưới. AI quan sát ảnh (2026-10-12): địa hình Pokopia là khối lập phương có texture mềm, mép cỏ phủ cạnh | Địa hình khối (user chọn bề mặt mịn liền); nhân vật chibi; mọi thiết kế, tên có bản quyền của Pokémon |
| Minecraft | — | Sinh vật/vật phẩm low-poly khối vuông (user loại) |

## Nguồn asset
<!-- Tự làm / Asset Store / thuê ngoài / AI; giấy phép; chi phí. -->
Quyết định của user (2026-10-12): **địa hình và đồ vật dùng asset có sẵn trước; sinh vật và nhân vật làm bằng AI.**
- Tiêu chí chọn asset: hỗ trợ **URP** ([0005](../decisions/0005-render-pipeline.md)); giấy phép cho dùng thương mại; giá có nguồn; hợp phong cách mịn, không chibi.

**Địa hình & đồ vật — asset có sẵn** (tra 2026-10-12; giá, version, URP phải kiểm lại trên trang trước khi mua) [ý tưởng]:
- Địa hình bề mặt mịn cần **bộ vật liệu/texture** (cỏ, đất, đá, cát...) dán theo 3 hướng (triplanar) lên mesh sinh ra từ code — không dùng được pack khối dựng sẵn.
- Các pack cube world đã tra trước đó ([KUBIKOS](https://assetstore.unity.com/packages/3d/environments/kubikos-3d-cube-world-117341), [Modular Cube World Vol.1](https://assetstore.unity.com/packages/3d/environments/3d-modular-world-cube-world-vol1-251828)) **không hợp** địa hình mịn; chỉ xét phần đồ vật nếu hợp phong cách.
- Danh sách asset vật liệu địa hình và đồ vật: _(chưa có — cần tra tiếp)_

**Sinh vật & nhân vật — làm bằng AI** (user, 2026-10-12):
- [Learn3D AI](https://www.learn3d.ai/en): **khoá học** (không phải công cụ) về quy trình làm nhân vật game bằng AI: concept → tạo 3D bằng AI → sculpt → retopology → texture → rig → nhập vào Unity/Unreal. Giá, giấy phép không ghi trên trang.
- [Mixar](https://www.mixar.app/): trình dựng 3D mã nguồn mở xây trên Blender, có AI agent cho từng việc (UV, retopology, bake, rig, animation); dùng API key của người dùng (OpenAI, Anthropic, Gemini, OpenRouter). Tải miễn phí, đăng nhập để kích hoạt; có gói đăng ký (giá ở trang riêng, chưa tra); định dạng xuất và giấy phép không ghi trên trang chính.
- [mcp-for-blender](https://github.com/ahujasid/blender-mcp) (cộng đồng, MIT): cho AI điều khiển Blender — dùng cho thử nghiệm art (xem [roadmap](../05-roadmap.md)).
- Pack sinh vật bán sẵn (vd [Cute Series](https://assetstore.unity.com/packages/3d/characters/creatures/monsters-ultimate-pack-01-cute-series-167028)) **không dùng**: phong cách chibi/dễ thương, trùng game khác.
- Rủi ro: chất lượng và độ đồng nhất phong cách khi làm hàng chục loài bằng AI [chưa xác minh] → thử nghiệm trước; giấy phép thương mại của model/dịch vụ AI tạo 3D phải kiểm từng dịch vụ.

## Quy cách kỹ thuật
<!-- Độ phân giải sprite / polycount, kích thước texture, palette, quy ước đặt tên file, pivot, scale. -->
**Tỉ lệ** (user, 2026-10-12):
- **1 ô voxel = 1 đơn vị.**
- **Nhân vật cao 1,5 đơn vị, rộng 1 đơn vị** → đi vừa một rãnh rộng 1 ô, cao 2 ô. Khung chiếm chỗ của nhân vật: 1 × 2 × 1 ô (rộng × cao × sâu).
- Sinh vật lấy tỉ lệ theo nhân vật; loài to nhỏ khác nhau → khung chiếm chỗ khác nhau.
- [ý tưởng] Theo quy ước thường dùng 1 đơn vị Unity = 1 m [chưa xác minh trong docs 6000.4] → nhân vật cao 1,5 m. Nếu muốn người cao hơn, đổi tỉ lệ model chứ không đổi lưới.
- [ý tưởng] Nhóm kích thước sinh vật để thiết kế môi trường và tìm đường: nhỏ (1×1×1), vừa (1×2×1 như người), lớn (2×2×2), rất lớn (3×3×3 trở lên). Sinh vật lớn không đi qua rãnh 1 ô → địa hình người chơi làm phải tính đến loài nào đi qua được.

Còn thiếu: số polygon/texture cho mobile, quy ước khung chiếm chỗ trong dữ liệu (gắn với model), quy ước đặt tên file, pivot — _(chưa có)_
