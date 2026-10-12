# Quy ước đặt tên

> Trạng thái: Nháp
> Liên quan: [Thuật ngữ](../glossary.md) · [Thế giới](world.md) · [Nhân vật](characters.md) · [Cơ chế](../02-gameplay/mechanics.md)

Tên riêng trong game (tên game, vùng đất, loài, tộc, phép, nhân vật, người chơi...). Thuật ngữ cơ chế (lệnh làm việc, nhập hồn...) nằm ở [glossary](../glossary.md), không ở đây. Viết bằng skill `local-naming`; kiểm bằng `python3 .claude/skills/local-naming/scripts/check_names.py`.

## Cần đặt tên
Tổng hợp 2026-10-12 từ toàn bộ `design/`. Chưa có tên nào được chốt.

**Tên thiết kế ≠ tên trong game** [ý tưởng]: thuật ngữ trong [glossary](../glossary.md) (vd "lệnh làm việc", "vượt trội") là tên **nội bộ** để viết thiết kế. Tên người chơi thấy trong game có thể khác và đặt ở file này. Khi đã có tên trong game, glossary ghi thêm cột/ghi chú tên hiển thị.

Ưu tiên: **Trước M1** = cần cho B8 (roadmap) / prototype dùng tên tạm được; **M2** = vertical slice; **M3+** = sau.

| Nhóm | Mục cần tên | Số lượng | Ưu tiên | Ghi chú |
|---|---|---|---|---|
| Thương hiệu | Tên game | 1 | Trước M1 (tên tạm được) | Tra trùng store, nhãn hiệu trước khi dùng chính thức |
| Thế giới | Tên thế giới | 1 | M2 | [world](world.md) chưa có tông giọng |
| Thế giới | Tên map / vùng đất | 3 lúc đầu, thêm theo mùa | M2 (map của vertical slice trước) | Mỗi map có câu chuyện riêng |
| Thế giới | Tên vùng trong map, địa danh, khối cốt truyện | Nhiều | M2 | Theo [levels](../02-gameplay/levels.md) |
| Nhân vật | Danh xưng / tên mặc định của pháp sư (người chơi) | 1 | M2 | Người chơi có tự đặt tên không? |
| Nhân vật | NPC, nhân vật cốt truyện | _(chưa có danh sách)_ | M2 | [characters](characters.md) đang trống |
| Sinh vật | Tên gọi chung cho "sinh vật" trong thế giới | 1 | M2 | Tên người chơi dùng để gọi cả nhóm sinh vật |
| Sinh vật | Tộc | _(chưa định số)_ | M2 | Tên cùng tộc nên nhận ra là cùng nhóm |
| Sinh vật | Nguyên tố | _(bộ nguyên tố chưa định)_ | M2 | |
| Sinh vật | Loài | 3 (M1, tên tạm) → ~10 (M2) → nhiều hơn | M1 tạm, M2 chính thức | |
| Sinh vật | Biến thể | Vài biến thể mỗi loài | M3 | |
| Phép | Tên hiển thị của "phép sinh vật", "phép của pháp sư", "nhập hồn" | 3 | M2 | |
| Phép | Từng phép | ~20 phép sinh vật + ~8 phép pháp sư (slice) | M2 | |
| Phép | Các mức độ hiếm, nhóm phép | _(chưa định số mức)_ | M2 | |
| Trang bị | Tên chung, các bậc trang bị, từng trang bị | _(chưa định)_ | M3 | |
| Tài nguyên | Tiền trong game, tiền nạp | 2 | M3 | |
| Tài nguyên | **"Ma năng"** — cần đổi tên | 1 | M2–M3 | Dễ nhầm với "mana" (user đồng ý đổi, 2026-10-12) |
| Tài nguyên | Mana | 1 | M2 | |
| Tài nguyên | Nguyên liệu rèn, vật phẩm phân bổ lại, vật phẩm hỗ trợ, quà từ sinh vật | 4+ | M2–M3 | |
| Cửa hàng | Gói sinh vật, gói sách phép, gói trang bị | 3 | M3 | |
| Công trình | Kho, lò rèn, công trình khác | _(chưa có danh sách)_ | M1 (kho, tên tạm) → M3 | |
| Cơ chế hiển thị | Lệnh làm việc, năng lực làm việc, độ thuần thục, danh sách yêu cầu của loài, nhu cầu sinh vật / mức đáp ứng, vượt trội, biến thể, bảo hiểm, mảnh ký ức, kho sinh vật, túi đồ, tiến hoá / bậc | ~14 | M2 | Có thể giữ tên glossary nếu đủ dễ hiểu |
| PvP | Tên các hạng | _(chưa định số hạng)_ | Sau phát hành | |

## Tên game
<!-- Tên chính thức, các phương án, kết quả kiểm tra trùng (store, nhãn hiệu, tên miền) kèm link. -->
_(chưa có)_

## Nguyên tắc chung
<!-- Ngôn ngữ gốc (tiếng Việt, tiếng Anh, ngôn ngữ tự tạo), thị trường mục tiêu, độ dài, dễ đọc trên cả PC và mobile, không trùng game/thương hiệu có sẵn. -->
Quyết định của user (2026-10-12):
- **Tên riêng là từ tự tạo** kiểu fantasy, **dễ đọc ở cả tiếng Việt lẫn tiếng Anh**.
- **Thị trường mục tiêu: toàn cầu.**

Hệ quả [ý tưởng]:
- Tên riêng giữ nguyên qua mọi bản dịch; giao diện và tên cơ chế dịch theo ngôn ngữ người chơi.
- Chỉ dùng chữ Latin không dấu (a–z), không ký tự đặc biệt → gõ, tìm kiếm, hiển thị được ở mọi ngôn ngữ.
- 2–3 âm tiết; tránh cụm phụ âm khó với người Việt (vd "str", "-nds") và cách viết dễ đọc sai với người Anh (vd "nh", "ngh", "gi").
- Không trùng từ có nghĩa xấu/nhạy cảm trong tiếng Việt, tiếng Anh (kiểm thêm các ngôn ngữ lớn khi chốt tên game).
- Độ dài: loài, phép ≤ 12 ký tự; map, vùng đất ≤ 16 ký tự (để vừa UI mobile).
- Tên game: tra trùng toàn cầu (Steam, App Store, Google Play, nhãn hiệu) trước khi dùng chính thức.

## Quy ước theo nhóm
<!-- Mỗi nhóm: cách ghép tên (gốc từ, âm tiết, hậu tố theo nguyên tố/tộc...), độ dài tối đa, ví dụ. -->
| Nhóm | Quy ước | Độ dài tối đa | Ví dụ |
|---|---|---|---|
| Vùng đất / map | _(chưa có)_ | | |
| Tộc | _(chưa có)_ | | |
| Loài | _(chưa có)_ | | |
| Phép | _(chưa có)_ | | |
| Nhân vật | _(chưa có)_ | | |
| Người chơi (pháp sư) | _(chưa có)_ | | |
| Vật phẩm | _(chưa có)_ | | |

## Danh sách tên
<!-- Mọi tên riêng đã đặt. Script check_names.py đọc bảng này: cột 1 = tên, cột 2 = nhóm. Trạng thái: đề xuất / dùng / bỏ. -->
| Tên | Nhóm | Ý nghĩa / gốc | Trạng thái |
|---|---|---|---|
