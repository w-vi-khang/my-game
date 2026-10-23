# 0031 — Tầm với của mọi thao tác: 8 ô

> Trạng thái: Bị thay thế bởi [0034](0034-dieu-khien-pc-click-di-chuyen.md) (user, 2026-10-23: tầm thao tác 10 ô / tầm thực hiện 2 ô)
> Ngày: 2026-10-22

## Bối cảnh
[0030](0030-tam-voi-cong-cu-10-o.md) chốt đào/đặt/xới 10 ô, nhặt (F, click), gieo, thu hoạch, phép Mưa 4 ô, mở kho ≤ 3 ô ([0028](0028-nhan-vat-tui-vat-roi-tu-trong-trot.md)). Brief 0036 + 0037 (sim 7e) làm xong tầm 10/4. User thử trên Mac (2026-10-22): đa số ổn, muốn mọi thao tác cùng 8 ô.

## Các lựa chọn
1. **Mọi thao tác 8 ô** — một tầm duy nhất, dễ nhớ; nhặt xa hơn (ít phải đi lại), đào/đặt gần hơn một chút. Sửa hằng số + test biên.
2. Giữ 10/4 — user thấy nhặt 4 ô ngắn, hai tầm khác nhau khó nhớ.

## Quyết định
Lựa chọn 1 (user, 2026-10-22): **đào, đặt, xới (đánh dấu, click ô ruộng mở bảng), nhặt (F, click), gieo, thu hoạch, phép Mưa, mở kho: tầm với 8 ô**. Cách đo giữ như brief 0035: Euclid từ tâm thân (chân + 1 ô) tới tâm ô, ≤ 8.

## Hệ quả
- Thay [0030](0030-tam-voi-cong-cu-10-o.md) và phần tầm với (4 ô, mở kho ≤ 3 ô) của [0028](0028-nhan-vat-tui-vat-roi-tu-trong-trot.md).
- Code (brief 0038): `PlayerRules.ToolReach` 10 → 8, `HandReach` 4 → 8 (giữ hai hằng để sau tách lại nếu cần); chữ HUD; test biên 8 / 8,1 và test PlayMode dời ô theo. Ước lượng ≤ 0,1 ngày-người (trong 3–5 của sim 7e).
- Túi hạt đầu game giữ cách chỗ xuất phát 3 ô (vẫn trong tầm).
- Cập nhật: `design/02-gameplay/mechanics.md` (luật nhân vật M1), `design/glossary.md` (Tầm với), `design/04-tech/world-tech.md` (bước 7h), `design/04-tech/simulation-tech.md` (bước 7e), `decisions/README.md`.
