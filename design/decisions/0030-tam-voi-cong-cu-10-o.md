# 0030 — Tầm với của công cụ đào/đặt/xới: 10 ô

> Trạng thái: Chốt
> Ngày: 2026-10-22

## Bối cảnh
[0028](0028-nhan-vat-tui-vat-roi-tu-trong-trot.md) chốt tầm với 4 ô quanh nhân vật. Brief 0035 (world 7h a) làm đào/đặt/xới chỉ trong 4 ô (Euclid từ tâm thân tới tâm ô). User thử trên Mac (2026-10-22): khoảng cách đào/đặt ngắn quá.

## Các lựa chọn
1. **Đào/đặt/xới 10 ô, việc tay khác giữ 4 ô** — ba công cụ nhắm bằng chuột với xa; nhặt, gieo, thu hoạch, phép Mưa vẫn phải đi tới gần. Sửa một hằng số + test.
2. Chỉ đào/đặt 10 ô, xới giữ 4 ô — xới khác đào/đặt dù cùng nhắm bằng chuột.
3. Mọi thao tác 10 ô — người chơi ít phải đi lại, giảm vai trò của nhân vật (0028).

## Quyết định
Lựa chọn 1 (user, 2026-10-22):
- **Đào, đặt, xới (đánh dấu, click ô ruộng mở bảng): tầm với 10 ô**, cùng cách đo của brief 0035 (Euclid từ tâm thân — chân + 1 ô — tới tâm ô, ≤ 10).
- Nhặt, gieo, thu hoạch, phép Mưa: giữ 4 ô. Mở kho: giữ ≤ 3 ô (0028).

## Hệ quả
- Code: `PlayerRules` tách tầm công cụ (10) và tầm việc tay (4); test EditMode biên 10 / 10,1; test PlayMode tầm với dời ô theo. Làm trong brief đầu của sim 7e (user chọn làm 7e trước 7h phần b, 2026-10-22).
- Cập nhật: `design/02-gameplay/mechanics.md` (luật nhân vật M1), `design/glossary.md` (Tầm với), `design/04-tech/world-tech.md` (bước 7h). Ước lượng không đổi (≤ 0,1 ngày-người, trong 3–5 của 7h).
