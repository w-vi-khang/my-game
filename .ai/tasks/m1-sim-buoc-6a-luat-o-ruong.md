# Task: M1 simulation bước 6a — ô ruộng chỉ có sau khi xới; đặt khối lên / đào ô ruộng thì huỷ vụ, trả hạt

> Trạng thái: user quyết 2026-10-19 ([0020](../../design/decisions/0020-o-ruong-tao-khi-xoi-va-huy-vu.md)) — chờ viết brief. Làm **trước** [bước 6b](m1-sim-buoc-6b-luat-di-lai.md).

## Mục tiêu
Bước 6a của [kế hoạch prototype simulation](../../design/04-tech/simulation-tech.md#kế-hoạch-prototype-m1). Thêm sau khi user chơi thử SimView (brief 0006, 2026-10-19): đặt khối cỏ lên ô ruộng đã xới được, mà mô phỏng vẫn làm ruộng như không có gì. Ước lượng 2,5–4 ngày-người.

Xong khi:
- sinh vật xới xong → ô đổi sang vật liệu ô ruộng đúng nhịp, mesh cập nhật;
- đào / đặt khối lên ô ở từng pha (đang xới, đang gieo, đang lớn, chín chưa thu, đã thu chờ chở) → số hạt trong kho, công đoạn, sinh vật, hàng đợi đúng luật;
- cùng seed → cùng mã băm từng nhịp;
- `SimDemoTests` (map M1) vẫn 0 đình trệ, đủ sản lượng.

## Quyết định (user, 2026-10-19 — 0020)
1. Lệnh trồng cây vẫn đặt lên ô đất/cỏ có ô trống phía trên. **Xới xong → ô thành vật liệu ô ruộng** (vật liệu mới của world, màu riêng). Từ đó mới gieo được.
2. **Đặt khối lên ô ruộng hoặc đào ô ruộng → huỷ vụ:**
   - đã trừ hạt mà chưa thu hoạch → trả 1 hạt vào kho ngay, kho đầy vẫn trả;
   - nông sản chưa thu thì mất;
   - hàng đã thu (nằm tại ô hoặc đang chở) vẫn chở vào kho.
3. **Sau khi huỷ vụ, ô rời lệnh, hàng đợi vụ bị xoá:**
   - đào → ô rỗng, +1 đất vào kho vật liệu;
   - đặt khối lên → ô ruộng bên dưới trở lại đất.
4. Thứ tự chọn việc **giữ nguyên** (công đoạn cũ nhất trước).
5. Phép tưới nước (ô đất → cỏ) để **M2**, không làm ở đây.

## Planner đề xuất (user xác nhận khi duyệt brief)
- Nước tràn lên ô ruộng tính như đặt khối.
- Ô của lệnh chưa xới xong bị đào/che → rời lệnh, không trả gì, vật liệu giữ nguyên.
- Vụ kế trên ô ruộng còn nguyên vẫn bắt đầu từ xới (0016); xới lại không đổi vật liệu.
- Sinh vật đang làm công đoạn bị huỷ → về rảnh ngay. Chỗ kho đã giữ thì nhả ra.
- Hàng đã thu nằm tại ô bị đào/che: điểm lấy hàng giữ nguyên, lấy từ ô kề như thường.
- Sự kiện "vụ bị huỷ" (ô, số hạt trả) trong `State.Events`.
- Mô phỏng đổi vật liệu qua cùng đường sửa ô của world. Cách nối chốt trong brief.
- Ô ruộng không có mép cỏ, sườn màu đất.

## Cách kiểm
Test EditMode, dùng `TestWorlds`:
- Xới xong → `GetMaterial` của ô = ô ruộng ở đúng nhịp; trước nhịp đó vẫn là đất/cỏ.
- Đào ô ở mỗi pha → hạt trong kho đúng (+1 khi đã trừ mà chưa thu, +0 khi chưa trừ); ô rời lệnh; sinh vật đang làm về rảnh; +1 đất.
- Đặt khối lên ô ruộng → huỷ vụ như trên; gỡ khối ra → ô là đất.
- Hàng đã thu vẫn vào kho sau khi ô bị đào.
- Kho đầy vẫn nhận hạt trả.
- Mã băm trùng qua 2000 nhịp, 2 lần chạy cùng seed.

Test PlayMode hoặc user xem: ô ruộng hiện màu riêng trong SimView, mesh cập nhật khi xới xong.

## Ngoài phạm vi
- Phép tưới nước, người chơi tự làm công đoạn (M2).
- Ô dưới kho / nhà bị đào, công trình lơ lửng ([ideas](../../design/ideas.md), để sau).
- Đổi thứ tự chọn việc.
- Đếm ô cỏ / ô ruộng cho môi trường sống (bước 8).
- Thông báo trên HUD cho sự kiện "vụ bị huỷ": chỉ phát sự kiện trong lõi, trừ khi brief ghi khác.
