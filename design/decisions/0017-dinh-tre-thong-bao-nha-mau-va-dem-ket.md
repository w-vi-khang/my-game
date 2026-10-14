# 0017 — Simulation bước 4: thông báo là sự kiện trong lõi, thiếu năng lực, nhà mẫu khối đặc, chỉ đếm kẹt khi rảnh

> Trạng thái: Chốt
> Ngày: 2026-10-14
> Thay đổi: làm rõ định nghĩa "bị kẹt" của [0016](0016-o-ruong-kho-nha-va-sinh-vat-ket.md) và [mechanics — Tìm đường sinh vật & đình trệ](../02-gameplay/mechanics.md#tìm-đường-sinh-vật--đình-trệ); chi tiết nhà mẫu M1 trong [mechanics — Nhà sinh vật](../02-gameplay/mechanics.md#nhà-sinh-vật); hàng bước 4 trong [kế hoạch prototype simulation](../04-tech/simulation-tech.md#kế-hoạch-prototype-m1).

## Bối cảnh
Viết task contract simulation bước 4 (đình trệ + thông báo, nhà mẫu 2 × 2 × 2, sinh vật bị kẹt — 0016). Còn mở: thông báo gồm những gì, khi nào tính "thiếu năng lực", nhà mẫu chiếm ô và sinh vật hiện lại ở đâu, gắn sinh vật vào nhà ra sao, kiểm "có đường về nhà" lúc nào và đếm kẹt trong những trạng thái nào.

## Quyết định
**Chốt (user, 2026-10-14):**
- **Thông báo ở M1 = danh sách sự kiện trong lõi mô phỏng** (`Game.Core.Sim`): nhịp, công đoạn, vị trí, lý do; phát khi **bắt đầu** và khi **hết** đình trệ, không lặp lại mỗi nhịp. UI đọc ở bước 6.
- **Thiếu năng lực** = không có sinh vật **có nhà** nào sở hữu năng lực đó (đang bận vẫn tính là có). Sinh vật chưa có nhà không tính. Mở lại khi thêm sinh vật hoặc gắn nhà.
- **Nhà mẫu 2 × 2 × 2 là khối đặc**: chiếm 8 ô, chặn đi qua và chặn đặt khối (như kho), không có cửa. Sinh vật hiện lại ở **ô kề nhà** đứng được đầu tiên theo thứ tự cố định; không còn ô nào đứng được thì chờ (vẫn tính là kẹt).
- **Gắn nhà** bằng lệnh riêng: đặt nhà (trả id), gắn sinh vật vào nhà (báo lỗi khi nhà đã đủ 2). Sinh vật mới thêm chưa có nhà.
- **Kiểm "có đường về nhà" theo sự kiện**: chỉ tìm đường lại khi địa hình đổi, đặt kho/nhà, sinh vật bị đẩy/rơi, gắn nhà — không kiểm mỗi nhịp.
- **Chỉ đếm kẹt khi có chuyện bất thường**: sinh vật **rảnh** (không có việc, hoặc việc vừa bị cắt đường giữa chừng) **và** không có đường về nhà. Đang đi theo đường hợp lệ hoặc đang làm việc thì không đếm; có việc lại thì bộ đếm về 0. Đủ 300 nhịp liên tục → hiện lại ở nhà.
- **Hàng đang mang khi bị cắt đường** rơi tại chỗ; khi sinh vật đó hiện lại ở nhà mà chưa ai nhặt hàng → hàng **theo sinh vật về ô hiện lại**.

**Planner (2026-10-14), chi tiết cài đặt:**
- Lý do đình trệ đổi trực tiếp từ lý do này sang lý do khác → chỉ một sự kiện "bắt đầu" với lý do mới. Đường bị mở lại rồi chặn lại trong cùng nhịp (địa hình đổi nhưng vẫn không có đường) → không phát sự kiện.
- Thêm sự kiện "sinh vật hiện lại ở nhà" (vị trí mới) để UI bước 6 báo cho người chơi.
- Ô kề nhà: 4 cạnh của chân nhà, mỗi cạnh theo từng ô (+X, −X, +Z, −Z), mỗi ô thử lệch cao 0, +1, −1 — cùng luật ô kề của kho/ô ruộng (0016).
- Kiểm theo sự kiện đúng vì bước đi bộ có tính đối xứng (leo lên A→B và xuống B→A cùng kiểm khoảng trống phía trên A); khi thêm bay/bơi (M2) phải xem lại.
- Kích thước, sức chứa nhà và thời gian kẹt nằm trong `design/data/sim_m1.json` (số tạm).

## Hệ quả
- Ước lượng bước 4 giữ **3–4** ngày-người; M1 không đổi.
- Mọi test mô phỏng cũ phải gắn nhà cho sinh vật (helper trong test). Hai test cũ đổi kỳ vọng từ "công đoạn nằm chờ" sang `NoAbility`.
- Đào/đặt làm hỏng ô dưới nhà: chưa xử lý (như kho) → [ideas](../ideas.md).
- Cập nhật: [mechanics](../02-gameplay/mechanics.md), [simulation-tech](../04-tech/simulation-tech.md#kế-hoạch-prototype-m1), [glossary](../glossary.md).
