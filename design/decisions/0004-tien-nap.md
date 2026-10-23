# 0004 — Tiền nạp: ảnh hưởng tốc độ, chặn ở mức tối đa

> Trạng thái: Chốt
> Ngày chốt: 2026-10-13 (user)
> Ngày: 2026-10-12
> Thay đổi bởi [0034](0034-dieu-khien-pc-click-di-chuyen.md) (user, 2026-10-23): túi đồ **có giới hạn ô** (ban đầu 10 + túi phụ 2) — ý "túi đồ không cần mức tối đa" ở Hệ quả không còn đúng; ưu đãi "túi đồ +10%" thành **ngoại lệ thứ hai** như giới hạn sinh vật (user, 2026-10-23, 0034 Hệ quả).

## Bối cảnh
- Game F2P, hai loại tiền: tiền trong game và tiền nạp ([vision](../00-vision.md)).
- Quy tắc tiền nạp đã đổi hai lần trong M0:
  1. (2026-10-11) Tiền nạp chỉ cho làm đẹp, hiệu ứng, tăng cơ hội, đẩy nhanh xây dựng; không ảnh hưởng chỉ số/sức mạnh PvP; phát triển sau ngưỡng PvP không ảnh hưởng PvP.
  2. (2026-10-12) Điểm cấp độ/bậc vẫn tính trong PvP; chỉ bỏ phần vượt trội ([cơ chế](../02-gameplay/mechanics.md#chế-độ-nhiều-người)).
- User (2026-10-12): tiền nạp luôn ảnh hưởng tiến trình theo cách này hay cách khác; chặn được ở mức tối đa, còn tốc độ là không tránh được.

## Các lựa chọn
- **A. Tiền nạp không ảnh hưởng gì tới sức mạnh** (quy tắc 1): khó giữ thật — mọi thứ giúp đi nhanh hơn (rút thưởng nhiều hơn, xây nhanh hơn) đều gián tiếp làm mạnh sớm hơn; nếu chặn hết thì gần như chỉ còn bán skin → doanh thu hẹp.
- **B. Ảnh hưởng tốc độ, chặn ở mức tối đa:** người nạp đi nhanh hơn, nhưng không mua được thứ người không nạp không đạt được. Thật thà với người chơi, dễ thiết kế cửa hàng. Nhược: người mới nạp nhiều có thể mạnh hơn người không nạp cùng thời gian chơi → giảm bằng ghép trận.
- **C. Bán thẳng sức mạnh vượt mức tối đa:** trái pillar P4 (sức mạnh đến từ chọn sinh vật và phép) — không xét.

## Quyết định
Chốt (user, 2026-10-13) — lựa chọn **B**:
1. Tiền nạp được ảnh hưởng **tốc độ** tiến trình.
2. **Mức tối đa** (chỉ số, cấp độ, bậc, sở hữu phép/sinh vật, bậc trang bị) người không nạp cũng đạt được; tiền nạp không mua được vượt mức đó. **Ngoại lệ** (user, 2026-10-13): giới hạn sinh vật — không nạp tối đa 54, nạp tối đa 60 (+10%); chỉ làm nông trại nhanh hơn, không ảnh hưởng PvP.
3. Trong PvP: bỏ phần vượt trội; điểm cấp độ/bậc và trang bị vẫn tính → ở mức tối đa mọi người như nhau, chỉ khác cách phân bố, sinh vật, phép và trang bị mang theo.
4. **Sửa 2026-10-12 (user chọn phương án b):** tiền nạp **được** tăng tỉ lệ vượt trội (vd mua thẻ bài bậc cao; thẻ bậc 4 chắc chắn vượt trội), vì vượt trội không tính trong PvP. Rút gói có bảo hiểm vượt trội, công khai mốc.
5. Tiền nạp mang lại: làm đẹp (skin), hiệu ứng, tăng cơ hội ra loài khi rút thưởng, đẩy nhanh xây dựng, ưu đãi (vật phẩm hỗ trợ, túi đồ +10%, giới hạn sinh vật +10%, phụ trợ khác).
6. Chênh lệch khi đang lên được giảm bằng ghép trận theo hạng rồi cấp độ trung bình.

Thay thế quy tắc 1 ở Bối cảnh (quy tắc đó chưa từng Chốt).

## Hệ quả
- Điểm 4 sửa 2026-10-12: thẻ bài bậc cao bán bằng tiền nạp ([cơ chế](../02-gameplay/mechanics.md#thẻ-bài--bắt-sinh-vật)). Vẫn giữ: không mua được thứ ảnh hưởng PvP vượt mức tối đa.
- [00-vision.md](../00-vision.md): mô hình kinh doanh, mục "Không làm" — đã cập nhật theo B.
- [economy.md](../02-gameplay/economy.md): tài nguyên tiền nạp, rủi ro — đã cập nhật.
- [mechanics.md](../02-gameplay/mechanics.md): nhận sinh vật, chế độ nhiều người, giới hạn sinh vật, tiền tệ & cửa hàng — đã cập nhật.
- Mọi món trong cửa hàng sau này đối chiếu điểm 2: có bán thứ người không nạp không đạt được không.
- Ngoại lệ giới hạn sinh vật (54/60) đã ghi ở [mechanics.md](../02-gameplay/mechanics.md#giới-hạn-sinh-vật--kho-sinh-vật), [economy.md](../02-gameplay/economy.md) (2026-10-13). Túi đồ +10%: **không phải ngoại lệ** — túi đồ không cần mức tối đa (user, 2026-10-13), nên không có "mức tối đa" để vượt; vẫn là ưu đãi tốc độ.
- Ưu đãi % (túi đồ, giới hạn sinh vật) phải giữ nhỏ; con số cuối cùng tính bằng skill cân bằng.
- Rút thưởng bằng tiền nạp vẫn có rủi ro quy định loot box [chưa xác minh] — xem [risks.md](../04-tech/risks.md).
