# 0009 — Map luôn có đại dương bao quanh, rìa xa là tường sương mù

> Trạng thái: Chốt
> Ngày: 2026-10-13

## Bối cảnh
- Khi xem mesh bước 2 (M1), ngoài map đang coi là rỗng → mesh sinh vách đứng quanh mép map.
- [levels](../02-gameplay/levels.md#kích-thước--khung-chung) đã có ý tưởng "biển sâu + sương mù ở rìa" nhưng chưa chốt.

## Các lựa chọn
- **A. Vách đứng / tường vô hình ở mép map:** rẻ nhất, nhưng người chơi có cảm giác đụng tường.
- **B. Đại dương bao quanh + tường sương mù ở rìa xa:** mềm mại hơn; tốn thêm phần mặt nước và sương mù.

## Quyết định
Chốt (user, 2026-10-13): **B.**
- **Mọi map có đại dương bao quanh** phần đất liền; vách/mép map nằm **xa đất liền**.
- **Phần xa phủ tường sương mù** thay vì vách đứng — không có cảm giác đụng tường.
- Cách làm kỹ thuật là đề xuất của AI, chưa chốt — xem [world-tech](../04-tech/world-tech.md) mục "Dựng mesh bề mặt mịn" (mép map).

## Hệ quả
- **M1** (thêm vào [0007](0007-m1-toi-gian.md)) (ước lượng của AI): đảo + vành biển trong bộ sinh (generatorVersion 2), mặt nước tĩnh, sương mù có sẵn của Unity theo khoảng cách camera + màu nền trùng màu sương → **2–3 ngày-người**. M1: 39–67 → **41–70**.
- **M2** (ước lượng của AI): "tường sương mù" theo khoảng cách tới rìa map (shader, cùng lúc với shader địa hình triplanar) → **2–4 ngày-người**.
- [levels](../02-gameplay/levels.md), [world-tech](../04-tech/world-tech.md), [roadmap](../05-roadmap.md) cập nhật theo.
- Còn mở: người chơi bơi ra xa thì bị chặn thế nào (dòng chảy đẩy lại? — ý tưởng cũ trong levels); bơi là cơ chế M2.
