# Phong cách hình ảnh

> Trạng thái: Đang xem xét
> Đã chốt phần (user, 2026-10-13) — **hướng phong cách**. Chưa thuộc phần Chốt: bảng màu, độ bão hoà, ánh sáng; các mục còn [ý tưởng] (asset ứng viên, nhóm kích thước, ngân sách polygon tạm, quy trình asset). Mục "Kết quả thử nghiệm art" là nhật ký, cập nhật không cần quyết định mới.
> Liên quan: [Vision](../00-vision.md) · [Công nghệ](../04-tech/tech-stack.md)

Thống nhất art style trước khi bắt đầu M1 (user, 2026-10-12) — xem B7 trong [roadmap](../05-roadmap.md#công-việc-m0-kế-hoạch--thiết-kế--ưu-tiên-hiện-tại).

## Hướng phong cách
<!-- 3–5 tính từ mô tả cảm giác; 2D/3D, góc camera, bảng màu, độ chi tiết. Chọn phong cách vừa sức một người làm. -->
Quyết định của user (2026-10-12):
- **Địa hình: khối bo cạnh, mép cỏ phủ** ([0011](../decisions/0011-dia-hinh-khoi-bo-canh.md), user chốt 2026-10-13 — thay "bề mặt mịn liền, không thấy cạnh khối" chốt 2026-10-12): mặt trên phẳng, cạnh lồi bo nhẹ, sườn ô cỏ màu đất với mép cỏ lượn sóng phủ xuống, vật liệu nhám (không bóng). Dữ liệu bên dưới vẫn là lưới voxel (đào/đặt, chiếm chỗ, tìm đường theo ô).
- **Cây cối, vật phẩm:** model mịn có hình dạng riêng, **không** low-poly khối vuông kiểu Minecraft.
- **Sinh vật và nhân vật có thể low-poly** (user, 2026-10-12) — hiểu là low-poly đa giác phẳng có hình dạng riêng, vẫn không phải khối vuông kiểu Minecraft [chưa xác minh với user].
- **Concept art:** tương tự bộ "Scene Concept" của XING-ART (www.xing-art.com — ảnh user gửi 2026-10-12, không lưu vào repo) **nhưng tươi sáng hơn**. AI quan sát ảnh: 3 mảnh cảnh dạng diorama góc nhìn isometric, cắt lộ lớp đất ở cạnh; vách đá phủ rêu, thác nước đổ xuống hồ xanh ngọc có lá súng, cây cọ, cây lá vàng/hồng/tím, phế tích đá, đường mòn; nét vẽ painterly, hình khối tròn đầy; điểm sáng phát quang (cây/sứa phát sáng). "Tươi sáng hơn" → tăng độ sáng, giảm vùng tối và bóng đậm.
- **Nhân vật và sinh vật không làm chibi** (tỉ lệ cơ thể thường, không đầu to).
- **Biến thể hiển thị:** các cá thể/vật thể cùng loại không render giống hệt nhau — mỗi cái có khác biệt nhỏ ngẫu nhiên.
- Mỗi sinh vật/cây/vật phẩm có **khung chiếm chỗ** gồm nhiều ô voxel nối với nhau; khung chỉ biểu thị không gian chiếm trên lưới, không quyết định hình dạng hiển thị.

Hệ quả cho thiết kế [ý tưởng]:
- Lưới voxel là lưới chung cho địa hình **và** chiếm chỗ: đặt vật phẩm/cây = chiếm các ô trong khung; tìm đường coi các ô đó là vật cản ([tech-stack](../04-tech/tech-stack.md)).
- Model mịn tách khỏi khung chiếm chỗ → đổi model không ảnh hưởng luật chơi ([kiến trúc](../04-tech/architecture.md#client-unity)).
- Biến thể hiển thị: mỗi cá thể/vật thể có một seed riêng → chọn trong khoảng cho phép: tỉ lệ kích thước (vd ±10%), sắc màu, hoa văn/texture phụ, hình dạng phụ (blend shape). Ngoài ra có **biến thể** (user, 2026-10-12): dạng khác của loài, đặc biệt về màu sắc, hình thái — cần art riêng (màu/texture khác, có thể model khác), tách riêng với vượt trội ([cơ chế](../02-gameplay/mechanics.md#nhận-sinh-vật)).
- Câu hỏi mở: bảng màu, độ bão hoà, ánh sáng; góc camera — đã trả lời: góc 3, xoay tự do (user, 2026-10-13, [controls-ui](../02-gameplay/controls-ui.md)).

## Tham khảo
<!-- Ảnh tham khảo: chỉ ghi link (không chép ảnh có bản quyền vào repo) + điều muốn lấy. -->
| Tham khảo | Lấy | Không lấy |
|---|---|---|
| Pokémon Pokopia — Switch 2, phát hành 05/03/2026 ([Nintendo](https://www.nintendo.com/en-gb/Games/Nintendo-Switch-games/Game-2915161.html), [ảnh trên Serebii](https://serebii.net/pokemonpokopia/pics.shtml)) | Cảm giác tươi sáng; vật thể là model mịn đặt trên lưới. AI quan sát ảnh (2026-10-12): địa hình Pokopia là khối lập phương có texture mềm, mép cỏ phủ cạnh. **Từ 2026-10-13 ([0011](../decisions/0011-dia-hinh-khoi-bo-canh.md)) lấy nguyên lý địa hình:** khối bo cạnh, mép cỏ lượn sóng phủ qua cạnh vách, vách đất có chấm, ánh sáng mềm không bóng (ảnh user gửi 2026-10-13, không lưu vào repo) | Texture, asset, bảng màu cụ thể của Pokopia; nhân vật chibi; mọi thiết kế, tên có bản quyền của Pokémon |
| Scene Concept — XING-ART (www.xing-art.com; ảnh user gửi 2026-10-12) | Diorama isometric, vách đá rêu, thác + hồ xanh ngọc, thực vật nhiều màu, điểm phát quang, nét painterly | Độ tối, bóng đậm (user muốn tươi sáng hơn); không chép ảnh/thiết kế cụ thể |
| Minecraft | — | Cây/vật phẩm low-poly khối vuông (user loại) |

## Nguồn asset
<!-- Tự làm / Asset Store / thuê ngoài / AI; giấy phép; chi phí. -->
Quyết định của user (2026-10-12): **địa hình và đồ vật dùng asset có sẵn trước; sinh vật và nhân vật làm bằng AI.**
- Tiêu chí chọn asset: hỗ trợ **URP** ([0005](../decisions/0005-render-pipeline.md)); giấy phép cho dùng thương mại; giá có nguồn; hợp phong cách mịn, không chibi.

**Địa hình & đồ vật — asset có sẵn** (tra 2026-10-12; giá, version, URP phải kiểm lại trên trang trước khi mua) [ý tưởng]:
- Địa hình khối ([0011](../decisions/0011-dia-hinh-khoi-bo-canh.md)) cần **bộ texture** (cỏ, đất, đá, cát...) dán theo mặt lên mesh sinh ra từ code — mặt thẳng trục nên không cần triplanar; vẫn không dùng pack khối dựng sẵn (mesh khối bo cạnh + mép cỏ do code sinh).
- Các pack cube world đã tra trước đó ([KUBIKOS](https://assetstore.unity.com/packages/3d/environments/kubikos-3d-cube-world-117341), [Modular Cube World Vol.1](https://assetstore.unity.com/packages/3d/environments/3d-modular-world-cube-world-vol1-251828)) **không dùng cho địa hình** (mesh do code sinh); chỉ xét phần đồ vật nếu hợp phong cách.
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
- Unity: hệ vật lý coi 1 m trong game = 1 đơn vị trong file nhập ([FBX Importer, docs 6000.4](https://docs.unity3d.com/6000.4/Documentation/Manual/FBXImporter-Model.html)) → 1 ô = 1 m, nhân vật cao 1,5 m. Nếu muốn người cao hơn, đổi tỉ lệ model chứ không đổi lưới.
- [ý tưởng] Nhóm kích thước sinh vật để thiết kế môi trường và tìm đường: nhỏ (1×1×1), vừa (1×2×1 như người), lớn (2×2×2), rất lớn (3×3×3 trở lên). Sinh vật lớn không đi qua rãnh 1 ô → địa hình người chơi làm phải tính đến loài nào đi qua được.

**Quy cách kiểm bằng công cụ** ([0006](../decisions/0006-cong-cu-kiem-tra-asset.md)): `tools/asset_check/rules.json` là nơi duy nhất ghi ngưỡng — pivot ở giữa đáy (chân), transform đã apply, đơn vị mét, chiều cao/khung theo nhóm kích thước, mesh không non-manifold/đỉnh rời/pháp tuyến ngược, UV trong 0–1, ≤ 4 vật liệu, shader Principled, texture ≤ 2048 px luỹ thừa của 2, ≤ 4 xương ảnh hưởng mỗi đỉnh (Unity chỉ có One/Two/Four Bones hoặc Auto — [docs 6000.4](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/QualitySettings-skinWeights.html)), có animation đứng/đi.
- [ý tưởng] Ngân sách tạm trong rules.json (chưa có nguồn, đo trên mobile ở M1 rồi sửa): nhân vật/sinh vật vừa 8.000 tam giác, nhỏ 5.000, lớn 12.000, đồ vật 3.000; tối đa 60 xương.

Còn thiếu: số polygon/texture đo thật trên mobile, quy ước khung chiếm chỗ trong dữ liệu (gắn với model), quy ước đặt tên file — _(chưa có)_

## Quy trình asset [ý tưởng]
Đề xuất 2026-10-12, chưa user duyệt. Mỗi bước phải **đạt công cụ kiểm tra asset** (`check_asset.py`, exit 0) trước khi sang bước sau; AI ghi thời gian từng bước để có số đo cho ước lượng M2.

| # | Bước | Ai / công cụ | Cổng kiểm |
|---|---|---|---|
| 1 | Brief: loài, nhóm kích thước, khung chiếm chỗ, 3–5 tính từ | AI soạn, user duyệt | Gắn pillar; đúng tỉ lệ ở trên |
| 2 | Ảnh concept từ prompt (FLUX.2-klein-4B) → GLB (TRELLIS.2), 1–3 lần | AI — `hf_generate.py`, skill `concept-image` của kit (bảng giải phẫu kỳ vọng + soi bất thường ngoài mô tả) | Ảnh đạt checklist của skill; **user duyệt phong cách** ở ảnh concept và ở GLB |
| 3 | Dọn: tỉ lệ, pivot, hướng, giảm polygon, UV | AI qua Blender (MCP hoặc script) | `check_asset.py --class creature_*` (bỏ qua rig) |
| 4 | Rig: Rigify (có sẵn trong Blender 5.0 [run]) + auto weights | AI | Kiểm rig (mọi đỉnh có weight, ≤ 4 ảnh hưởng) |
| 5 | Animation đứng + đi | AI | Có action `idle`, `walk`; xem dải khung hình |
| 6 | 3 biến thể (kích thước ±10%, sắc màu) | AI | Mỗi biến thể vẫn trong khoảng chiều cao của nhóm |
| 7 | Xuất FBX + GLB | AI | Xuất–nhập lại không đổi tam giác/kích thước/xương |

## Kết quả thử nghiệm art (đang làm)
Kế hoạch: [roadmap](../05-roadmap.md) mục C. File `.blend`, ảnh render và file xuất lưu **ngoài repo** (user, 2026-10-12): `~/Documents/User/khang/Game/art-test/`.

**Công cụ — chốt (user, 2026-10-12):** tạo 3D bằng AI qua **Hugging Face Spaces**, gọi bằng token HF của user (quota ZeroGPU tính cho tài khoản đó — [docs](https://huggingface.co/docs/hub/spaces-agents)).
- Ảnh concept: Space [FLUX.2-klein-4B](https://huggingface.co/spaces/black-forest-labs/FLUX.2-klein-4B) — model **Apache-2.0** ([model](https://huggingface.co/black-forest-labs/FLUX.2-klein-4B)). Không dùng bản 9B: FLUX Non-Commercial License ([model card](https://huggingface.co/black-forest-labs/FLUX.2-klein-9B)).
- Ảnh → GLB: Space [TRELLIS.2](https://huggingface.co/spaces/microsoft/TRELLIS.2) — code + model **MIT** ([repo](https://github.com/microsoft/TRELLIS.2)). API [run]: `/preprocess_image` → `/image_to_3d` (seed, độ phân giải 512/1024/1536) → `/extract_glb` (`decimation_target` mặc định 300.000, `texture_size` mặc định 2048).
- Script: `tools/asset_gen/hf_generate.py` — ghi kèm `<tên>.provenance.json` (model, giấy phép, seed, thời gian). Môi trường Python (gradio_client 2.7.2, huggingface_hub 2.2.0) ở `art-test/.venv`, ngoài repo.
- Quota free [chưa xác minh]: khoảng 3,5 phút GPU/ngày, có thể giới hạn số lượt ([forum HF](https://discuss.huggingface.co/t/what-is-the-free-zerogpu-quota-for-1-space/178610)) → đủ thử nghiệm, chưa đủ sản xuất. Khi cần nhiều hơn (M2): HF PRO hoặc [fal.ai](https://fal.ai/models/fal-ai/trellis-2) (0,25–0,35 $/lần).
- Khâu Blender: blender-mcp (cầu nối) + Rigify (rig) + [công cụ kiểm tra asset](../decisions/0006-cong-cu-kiem-tra-asset.md). Các phương án đã xét và loại → [ideas](../ideas.md).

**Địa hình mịn — xong bản đầu** (lịch sử — hướng bề mặt mịn đã thay bởi [0011](../decisions/0011-dia-hinh-khoi-bo-canh.md) 2026-10-13) [run 2026-10-12]: mảnh 30 × 30 ô, cao tới 12 ô; dữ liệu voxel có/không + loại vật liệu (cỏ, đất, đá, cát); dựng mesh bằng Surface Nets + làm mượt có giới hạn (mỗi đỉnh lệch ≤ 0,2 ô). Có rãnh rộng 1 ô sâu 2 ô và khối tham chiếu nhân vật 1,5 × 0,7.

| Phương án | Tam giác (mảnh 30 × 30) | Rãnh 1 ô | Cảm giác |
|---|---|---|---|
| ×1 — mỗi ô gameplay = 1 ô dựng mesh | ~6.000 | Bị bóp thành chữ V, đáy còn ~½ ô → nhân vật hiển thị cắm vào vách | Đồi lượn mịn, liền |
| ×2 — mỗi ô gameplay = 2 × 2 × 2 ô dựng mesh | ~24.000 | Giữ được vách gần thẳng, nhân vật lọt | Thấy bậc thang bo tròn, gần Pokopia hơn |

Phát hiện:
- Với dữ liệu có/không, Surface Nets (và marching cubes) đặt mặt ở giữa hai tâm ô → chi tiết rộng 1 ô bị mất khoảng nửa ô. Lưu mật độ mỗi ô ([tech-stack](../04-tech/tech-stack.md)) chỉ giúp nếu mật độ có giá trị trung gian; đào/đặt nguyên ô thì vẫn như dữ liệu có/không.
- Vật liệu gán theo mặt → ranh giới vật liệu răng cưa; cần trộn theo đỉnh + triplanar như tech-stack đã ghi.
- **Quyết định của user (2026-10-12): ×2** — mỗi ô gameplay dựng mesh bằng 2 × 2 × 2 ô; giữ được rãnh 1 ô, chấp nhận bậc thang bo tròn và số tam giác ×4 (~24.000 cho mảnh 30 × 30). Phải đo trên mobile ở M1 ([risks](../04-tech/risks.md)). Đã loại: ×1 (rãnh 1 ô bị bóp chữ V); ×1 làm mượt ít hơn ở vách.
- Xuất thử: `terrain_x2.fbx`, `terrain_x2.glb`. Công cụ kiểm tra asset (class `terrain_test`) [run 2026-10-12]: x1 và x2 (.blend), GLB, FBX đều **đạt** (0 lỗi). Chưa nhập thử vào Unity (M0 chưa tạo project) → [chưa xác minh] Unity đọc đúng tỉ lệ/trục.

**Kiểm duyệt ảnh concept — lớp 2–3 xong** [run 2026-10-12]. Lệnh: `tools/asset_gen/concept_check.py` (khai báo trong `concept_image.checkers` của `.ai/project.json`). Kiểm chứng trên bộ ảnh đã biết lỗi (ngoài repo, `art-test/gen/`):

| Ảnh | Lỗi đã biết | Lớp 2 — đếm (OWLv2, CPU, không tốn quota) | Lớp 3 — hỏi–đáp (Qwen3-VL, ZeroGPU ~9 giây/ảnh) |
|---|---|---|---|
| A | 5 chân; lông mày; cười kiểu người | Bắt 5 chân; **báo sai** 2 đuôi | Bắt lông mày, cười; **sót** 5 chân (trả lời 4) |
| B1 | 3 đuôi; cười | Bắt 3 đuôi; **báo sai** 2 chân, 3 tai | Bắt đuôi (trả lời 2 ≠ 1), cười |
| B2 | 3 đuôi; cười | Bắt 3 đuôi | Bắt đuôi (trả lời 2 ≠ 1), cười |

- Gộp 2 lớp: bắt **7/7** lỗi đã biết. Lớp 2 báo sai 3/12 phép đếm → kết quả lệch của lớp 2 là **cảnh báo**, phải xác nhận ở lớp 1 (soi vùng) — đúng quy tắc của skill.
- Lớp 2 cần truy vấn có tiền tố ("animal leg"): nhãn trần ("leg") không thấy chân nào [run].
- Hai Space Qwen chính thức (Qwen2.5-VL-32B, Qwen3-VL-Demo) lỗi phía máy chủ khi gọi qua API → dùng Space bên thứ ba `prithivMLmods/Qwen3-VL-HF-Demo`; có thể bị tắt bất kỳ lúc nào (rủi ro phụ thuộc).

**Bộ kiểm phong cách chung** (giữ đồng bộ giữa các ảnh — chép vào khối `json concept` của mọi spec) [ý tưởng]:
- `forbid`: "chibi proportions with an oversized head", "dark or gloomy lighting", "text or watermark", "a second creature", "eyebrows", "a human-like smile", "a cube-shaped blocky Minecraft style".
- `require`: "bright, fresh colors", "a plain white background" (khi ảnh đi tiếp sang image-to-3D).

**Sinh vật + rig + animation, 3 biến thể:** **tạm dừng** (user, 2026-10-12): trước mắt **chưa làm model hoàn chỉnh**, tập trung dựng mesh world (địa hình). Sinh vật/nhân vật nếu cần thì low-poly. Ảnh concept vẫn làm, và **phần kiểm duyệt** (kiểm ảnh concept lớp 1–3 của skill `concept-image`, công cụ kiểm tra asset) vẫn hoàn thiện để giữ đồng bộ, tránh lỗi vô lý.
