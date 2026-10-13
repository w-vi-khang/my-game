# 0011 — Địa hình hiển thị dạng khối bo cạnh, mép cỏ phủ (thay bề mặt mịn liền)

> Trạng thái: Chốt
> Ngày: 2026-10-13
> Thay thế: dòng "Địa hình: bề mặt mịn liền" trong [art-style](../03-art-audio/art-style.md#hướng-phong-cách) (phần đã Chốt 2026-10-12) và chữ "dựng mesh mịn" ở dòng Voxel world của [0007](0007-m1-toi-gian.md).

## Bối cảnh
- Sau bước 4, user xem scene TerrainView: mesh "quá bóng, nhìn không tự nhiên"; muốn tham khảo khối cỏ của Pokémon Pokopia, "làm được vậy càng tốt" (user, 2026-10-13, kèm 2 ảnh chụp Pokopia — không lưu vào repo).
- AI đo [run 2026-10-13, Play TerrainView qua MCP]:
  - **Bóng:** địa hình dùng material mặc định của URP (`Universal Render Pipeline/Lit`, `_Smoothness` 0,5, bật specular highlight + phản chiếu môi trường) → vệt sáng dọc sườn. Đặt smoothness 0, tắt specular + phản chiếu → hết vệt (ảnh `client/Temp/McpShots/09_matte-1.png`, không commit).
  - **Lỗ chỗ:** dù hết bóng, mặt vẫn lồi lõm thành chấm theo từng ô — do Surface Nets ×2 làm mượt có giới hạn ≤ 0,2 ô ([world-tech](../04-tech/world-tech.md#dựng-mesh-bề-mặt-mịn)) trên bậc 1 ô. Đổi material không sửa được.
- Khối cỏ Pokopia trong ảnh user gửi [user]: mặt trên phẳng; vách đứng màu đất có chấm; lớp cỏ dày, mép dưới lượn sóng phủ qua cạnh vách; cạnh khối bo nhẹ; ánh sáng mềm, không bóng. Tức là **địa hình khối** — trái với phần đã Chốt "bề mặt mịn liền, không thấy cạnh khối" ([art-style](../03-art-audio/art-style.md#hướng-phong-cách)), và bảng tham khảo đang ghi *không lấy* "địa hình khối" của Pokopia.
- Số liệu world M1 hiện tại (seed cố định, 64 × 32 × 64) nếu dựng dạng khối [run 2026-10-13]: 7.725 mặt lộ (4.096 mặt trên, 3.629 mặt sườn) → **15.450 tam giác** cho cả map khi chưa bo cạnh (Surface Nets ×2 hiện là 61.800); **4.520 cạnh lồi**; mặt sườn lộ ra phần lớn là ô cỏ (2.635 cỏ, 826 cát, 168 đất) vì 3.298/8.064 cặp cột kề nhau lệch đúng 1 ô (158 lệch 2 ô, 5 lệch 3 ô).

## Các lựa chọn
- **A. Giữ bề mặt mịn, làm đẹp hơn:** bỏ bóng, màu theo hướng mặt (trên cỏ, sườn đất/đá), tối ở chỗ lõm — gộp vào bước 5 (1–2 → 2–3 ngày-người). Không đụng phần Chốt; vẫn còn mặt lỗ chỗ.
- **B. Đổi sang khối bo cạnh kiểu Pokopia:** viết lại phần dựng mesh; gần ảnh mẫu nhất; nhìn là biết từng ô nên đào/đặt dễ hiểu hơn; dựng mesh đơn giản và nhẹ hơn. Ngược hướng đã Chốt; dễ bị xem là bắt chước Pokopia (cùng thể loại); thêm ~6–9 ngày-người vào M1 đang vượt quỹ ([roadmap](../05-roadmap.md#kiểm-tra-phạm-vi)).

## Quyết định
**Chốt (user, 2026-10-13): B**, cùng cách làm và kế hoạch 5.1–5.4 dưới đây.

- **Dữ liệu không đổi:** lưới gameplay 1 ô = 1 đơn vị, 2 byte/ô, luật đào/đặt, kho vật liệu, mã băm bộ sinh `86b0747d4b2be65b`. Chỉ đổi cách **hiển thị**.
- **Dựng mesh trên lưới gameplay (×1)**, mỗi mặt ô đặc giáp ô rỗng/nước là một mặt khối; cạnh lồi được bo; mặt trong (cạnh lõm) để vuông.
- **Màu/vật liệu theo kiểu Pokopia:** mặt trên theo vật liệu; sườn ô cỏ hiện màu **đất** với một **mép cỏ** phủ xuống (mép dưới lượn sóng, tất định theo toạ độ ô); vật liệu nhám (không bóng).
- Không chép texture, asset, bảng màu cụ thể của Pokopia — chỉ lấy nguyên lý (khối bo, mép cỏ phủ). Bảng màu theo hướng "tươi sáng" + painterly của XING-ART đã có trong art-style. Mức giống bao nhiêu là có rủi ro pháp lý: [chưa xác minh] — đây không phải ý kiến pháp lý.

## Kế hoạch prototype M1
Thay bước 5 "màu vật liệu theo đỉnh" (1–2) của [world-tech](../04-tech/world-tech.md#kế-hoạch-prototype-m1); bước 5b (đảo + biển), 6 (đo mobile) giữ nguyên.

| # | Bước | Xong khi | Ước lượng (ngày-người) |
|---|---|---|---|
| 5.1 | Mesher khối (Jobs + Burst) thay Surface Nets: mặt lộ, màu đỉnh theo vật liệu (sườn ô cỏ = đất), đệm chunk 1 ô; material nhám | Test: dựng lại tăng dần = dựng toàn bộ sau 200 lần sửa; sửa ô → cập nhật ≤ 2 khung hình; không mặt thừa giữa hai ô đặc; số tam giác ≈ 2 × số mặt lộ | 2–3 |
| 5.2 | Bo cạnh lồi (dải vát + góc, pháp tuyến nội suy) | Test: mesh kín (không cạnh hở) ở mọi tổ hợp 26 ô lân cận; đo tam giác/chunk; ảnh so sánh | 2–3 |
| 5.3 | Mép cỏ: mặt trên ô cỏ nhô ra ~0,1 ô, dải phủ xuống sườn với mép dưới lượn sóng theo toạ độ | Test: tất định (cùng ô → cùng hình); mép liền giữa hai ô kề và qua biên chunk | 2–3 |
| 5.4 | Texture/ánh sáng: chấm/vân cho vách đất và mặt cỏ (UV theo mặt — mặt thẳng trục nên không cần triplanar), ánh sáng mềm | User duyệt ảnh chụp so với hướng art | 1–2 |
| | **Cộng** | | **7–11** (thay 1–2 → **thêm 6–9**) |

- **Collider:** bake từ mesh khối chưa bo/chưa mép cỏ (mặt vuông) — khớp đúng lưới, nhẹ hơn (cách làm, chỉnh được khi code).
- **Địa hình bậc thang** [ý tưởng, chưa tính vào ước lượng]: noise hiện tại cho bậc 1 ô dày đặc (số liệu ở Bối cảnh) → dạng khối sẽ trông như bậc thang lắt nhắt, chưa giống các thềm phẳng rộng trong ảnh Pokopia. Có thể lượng tử hoá độ cao (thềm 2–3 ô, mặt phẳng rộng hơn) trong bộ sinh phiên bản 2 cùng bước 5b (+1–2) — quyết định sau khi xem ảnh bước 5.1–5.3.

## Hệ quả
Cập nhật trong cùng lần sửa (2026-10-13):
- [art-style](../03-art-audio/art-style.md): dòng địa hình ở "Hướng phong cách"; ô "Lấy / Không lấy" của Pokopia; mục nguồn asset (texture theo mặt thay cho triplanar); ghi chú ở "Kết quả thử nghiệm art" (bản Surface Nets là lịch sử).
- [world-tech](../04-tech/world-tech.md): mục "Dựng mesh bề mặt mịn" → dựng mesh khối; đệm chunk 2 → 1 ô; kế hoạch bước 5 → 5.1–5.4; bước 2–3 giữ "Xong" kèm ghi chú đã thay.
- [tech-stack](../04-tech/tech-stack.md) (độ phân giải ×2, bề mặt mịn), [risks](../04-tech/risks.md) (tam giác mesh mịn, non-manifold của Surface Nets), [architecture](../04-tech/architecture.md) ("dựng mesh mịn"), [mechanics](../02-gameplay/mechanics.md) (dòng Voxel world), [summary](../summary.md), [glossary](../glossary.md) (bỏ "lưới dựng mesh ×2", thêm "mép cỏ"), [ideas](../ideas.md) (bề mặt mịn → loại, lý do), [roadmap](../05-roadmap.md) (M1 +6–9 ngày-người, theo dõi M1), [README](../README.md) (mô tả world-tech). Tổng ước lượng: M1 41–70 → **47–79** (~13–22 tháng), M1–M4 ~11,8–22,3 → **~12,0–22,5 năm** — sửa số trong [roadmap](../05-roadmap.md), [vision](../00-vision.md) (dòng quỹ thời gian, đã Chốt — chỉ đổi con số theo quyết định này), [summary](../summary.md), [risks](../04-tech/risks.md).
- Code: thay `SurfaceNetsMesher` bằng mesher khối trong `Game.World`; `WorldEditor` dùng đệm mới; test mesher cũ chuyển/bỏ. Mã Surface Nets còn trong lịch sử git (commit `f05f917` trở về trước).
