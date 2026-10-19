# 0021 — Simulation M1: làm bước 6b (luật đi lại) trước 6a (ô ruộng), thêm test đứng chồng trên map M1

> Trạng thái: Chốt
> Ngày: 2026-10-19
> Thay đổi: thứ tự "6a → 6b" của [0020](0020-o-ruong-tao-khi-xoi-va-huy-vu.md) (mục Hệ quả) → **6b → 6a → 7 → 8**; thêm một tiêu chí test vào [task 6b](../../.ai/tasks/m1-sim-buoc-6b-luat-di-lai.md). Nội dung luật của [0019](0019-chan-nhau-ranh-quanh-nha-khong-dung-tren-ruong.md) và 0020 không đổi.

## Bối cảnh
User xem SimView lần 2 ([brief 0006](../../.ai/briefs/0006-sim-hien-thi.md), 2026-10-19). Ảnh chụp cho thấy hai sinh vật rảnh đứng chồng nhau trên ô ruộng. User đánh giá đây là lỗi quan trọng nhất.

Planner chạy mô phỏng demo trên map M1 4000 nhịp, có `KeepFarming` [run 2026-10-19, MCP `execute_code`]:
- L1 và L2 rảnh cùng ở ô (31, 18, 16) = ô ruộng 1, ở các nhịp 470, 1000 và 4000.
- Trên mọi lần đếm sinh vật rảnh (9.571): **57%** đứng chung ô với con khác, **59%** đứng trên ô ruộng.

Nguyên nhân là luật hiện tại (bước 1–5): xong việc thì sinh vật đứng yên tại chỗ, và không có luật chặn nhau. Bước 6b sửa đúng chỗ này.

## Các lựa chọn
- (a) Giữ 6a → 6b như 0020: định nghĩa ô ruộng ổn định trước, nhưng lỗi đứng chồng còn lâu hơn.
- (b) **6b → 6a:** sửa lỗi user thấy trước. Ở 6b, "ô ruộng" vẫn là ô của lệnh trồng cây (`AddFarmOrder`). Đây cũng chính là phạm vi 0020 chọn cho luật "không dừng trên ô ruộng" (mọi ô của lệnh, kể cả ô chưa xới), nên 6a không phải sửa lại 6b.

## Quyết định
**Chốt (user, 2026-10-19):** chọn (b).
1. **Thứ tự M1:** 6b → 6a → 7 → 8.
2. **Thêm test cho 6b** (EditMode, map M1 thật + `SimDemo`, 4000 nhịp có `KeepFarming`), kiểm ở mọi nhịp:
   - **0 lần** hai sinh vật chiếm chung ô (theo khung), trừ lúc đang dùng phao;
   - **0 lần** sinh vật rảnh dừng trên ô ruộng (đang đi ngang qua thì không tính).
   - Mốc hiện tại: 57% / 59% (số ở trên).

## Hệ quả
- Ước lượng không đổi: 6b 4–6,5, 6a 2,5–4 ngày-người. Bước 7 vẫn đo khi đã có cả 6a lẫn 6b.
- Khi viết brief 6a: giữ test map M1 của 6b xanh. Luật "không dừng trên ô ruộng" vẫn áp cho mọi ô của lệnh trồng cây, kể cả sau khi định nghĩa ô ruộng đổi.
- Cập nhật: [simulation-tech](../04-tech/simulation-tech.md#kế-hoạch-prototype-m1), [mechanics](../02-gameplay/mechanics.md), [roadmap](../05-roadmap.md), task [6a](../../.ai/tasks/m1-sim-buoc-6a-luat-o-ruong.md) / [6b](../../.ai/tasks/m1-sim-buoc-6b-luat-di-lai.md).
