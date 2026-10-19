# 0020 — Ô ruộng chỉ có sau khi xới; đặt khối lên / đào ô ruộng thì huỷ vụ, trả hạt (simulation bước 6a)

> Trạng thái: Chốt
> Ngày: 2026-10-19
> Thay đổi: định nghĩa "ô ruộng" của [0016](0016-o-ruong-kho-nha-va-sinh-vat-ket.md) (ô đất/cỏ có ô trống phía trên → ô đã xới); kéo phần "đào/đặt làm hỏng ô ruộng" trong [ideas](../ideas.md) từ M2 vào M1; thêm bước 6a, làm **trước** bước 6b ([0019](0019-chan-nhau-ranh-quanh-nha-khong-dung-tren-ruong.md)), vào [kế hoạch prototype simulation](../04-tech/simulation-tech.md#kế-hoạch-prototype-m1); cập nhật [mechanics — Kho & lệnh trồng cây](../02-gameplay/mechanics.md#kho--lệnh-trồng-cây--luật-m1), [mechanics — Phép](../02-gameplay/mechanics.md#phép), [roadmap](../05-roadmap.md), [glossary](../glossary.md).

## Bối cảnh
User chơi thử scene SimView ([brief 0006](../../.ai/briefs/0006-sim-hien-thi.md), duyệt 2026-10-19) và đặt được khối cỏ lên ô ruộng đã xới; mô phỏng vẫn làm ruộng như không có gì.

Hiện tại (bước 1–6):
- `AddFarmOrder` tạo ô ruộng ngay khi đặt lệnh (ô đất/cỏ có ô trống phía trên). Xới không đổi gì trên world [repo] `client/Assets/Game/Core/Sim/Simulation.cs:495`.
- Đổi ô chỉ làm mô phỏng tính lại đường [repo] `Simulation.cs:243`. Công đoạn của ô bị đào/che vẫn còn nguyên.
- Lỗ hổng này đã ghi ở [ideas](../ideas.md) (Planner 2026-10-14, M2, 1 ngày-người).

User đưa ra 4 luật (2026-10-19):
1. Đặt khối lên ô ruộng, hoặc đào ô đất của ruộng → huỷ vụ, trả hạt về kho.
2. Ô đất thường hay ô cỏ chỉ thành ô ruộng (trồng được) khi sinh vật xới xong ở ô đó.
3. Người chơi dùng phép tưới nước để biến ô đất thành ô cỏ.
4. Mỗi ô ruộng có chuỗi riêng: xới xong → đăng ký gieo → hệ thống tìm sinh vật rảnh phù hợp → gieo xong → đăng ký tưới → làm ngay. Chạy thật cho thấy mô phỏng đã làm đúng như vậy.

## Các lựa chọn
- **Luật 1–2:**
  - (a) Làm ở M1, trước 6b: 2,5–4 ngày-người.
  - (b) Làm ở M1, sau 6b: cùng công.
  - (c) M1 chỉ làm luật 1: 1–1,5.
  - (d) Để cả hai ở M2: lỗi vẫn thấy khi thử chơi.
- **Luật 3:**
  - (a) Giữ M2, trong phép của pháp sư.
  - (b) M1 thay đặt cỏ trực tiếp: +0,5–1.
  - (c) M1 thêm bên cạnh đặt cỏ: +0,5–1.
- **Thứ tự chọn việc** (sinh vật làm được cả xới lẫn gieo, như L1): hiện tại chọn công đoạn cũ nhất (ưu tiên rồi id, [repo] `Simulation.cs:610`) nên L1 xới hết các ô rồi mới gieo.
  - (a) Giữ như hiện tại: 0 công.
  - (b) Nối tiếp công đoạn của cùng ô: ~0,5.
  - (c) Ưu tiên công đoạn sau trong chuỗi: ~0,5.
- **Huỷ vụ trả gì:**
  - (a) Trả 1 hạt; hàng đã thu giữ nguyên.
  - (b) Như (a), nhưng cây chín thì trả cả nông sản.
  - (c) Chỉ trả hạt khi chưa gieo xong.
- **Ô bị che:**
  - (a) Ô rời lệnh.
  - (b) Ô giữ trong lệnh, tạm dừng: +0,5.
  - (c) Ô rời lệnh và trở lại đất.

## Quyết định
**Chốt (user, 2026-10-19):**
1. **Luật 1 và luật 2 làm ở M1**, thành **simulation bước 6a**, làm **trước bước 6b**.
2. **Ô ruộng có sau khi xới:**
   - Người chơi vẫn đặt lệnh trồng cây lên vùng ô đất/cỏ có ô trống phía trên.
   - Sinh vật xới xong một ô → ô đó đổi sang **vật liệu ô ruộng** (vật liệu mới của world, màu riêng). Từ đó mới gieo/tưới/thu hoạch được.
   - Hệ quả AI đã nêu khi hỏi: xới ô cỏ làm giảm số ô cỏ của môi trường sống (L1 cần ≥ 20 ô cỏ).
3. **Huỷ vụ:** đặt khối lên ô ruộng, hoặc đào ô ruộng → vụ đang làm bị huỷ.
   - Đã trừ hạt (từ lúc giao công đoạn gieo) tới khi cây chín mà chưa thu → **trả 1 hạt vào kho ngay**. Nông sản chưa thu hoạch thì mất. Kho đầy vẫn trả (tạm vượt 50).
   - **Hàng đã thu hoạch** (nằm tại ô hoặc đang chở) **vẫn chở vào kho như thường**.
4. **Sau khi huỷ vụ:**
   - **Đào ô ruộng:** ô thành rỗng; ô rời lệnh; hàng đợi vụ của ô bị xoá. Vật liệu đào được là **+1 đất** (AI đề xuất trong câu hỏi, user giữ nguyên).
   - **Đặt khối lên trên:** ô rời lệnh; hàng đợi bị xoá. Ô ruộng bên dưới **trở lại ô đất**: gỡ khối ra thì thấy ô đất, không phải ô ruộng. Muốn trồng lại phải đặt lệnh lại.
5. **Luật 3 (phép tưới nước: ô đất → ô cỏ) giữ M2**, nằm trong phép của pháp sư (ước lượng chung 10–20 + 1–2/phép, không đổi). M1 vẫn đặt cỏ trực tiếp.
6. **Thứ tự chọn việc giữ như hiện tại:** công đoạn cũ nhất trước, nên L1 xới hết rồi mới gieo. Chuỗi riêng từng ô (luật 4) đã đúng, không sửa.

**Planner đề xuất, user xác nhận khi duyệt brief bước 6a:**
- **"Đặt khối lên" = ô phía trên ô ruộng không còn rỗng**, gồm cả nước biển tràn vào. Nước làm giống đặt khối: huỷ vụ, ô ruộng trở lại đất.
- **Ô của lệnh chưa xới xong** (vẫn là đất/cỏ) bị đào hoặc che → ô rời lệnh, hàng đợi xoá, không có gì để trả. Ô giữ vật liệu như cũ. Tiến độ xới dở mất.
- **Vụ kế trong hàng đợi trên ô ruộng còn nguyên** vẫn bắt đầu lại từ xới, như [0016](0016-o-ruong-kho-nha-va-sinh-vat-ket.md). Ô đã là ô ruộng vẫn được xới lại, không đổi vật liệu.
- **Sinh vật đang làm công đoạn bị huỷ** (đang đi tới hoặc đang làm) → về rảnh ở nhịp đó. Chỗ kho đã giữ cho công đoạn đó được nhả.
- **Hàng đã thu nằm tại ô bị đào/che:** điểm lấy hàng giữ nguyên ô cũ; sinh vật lấy từ ô kề như thường. Không tới được → đình trệ "không có đường" như mọi công đoạn.
- **Sự kiện:** lõi phát sự kiện "vụ bị huỷ" (ô, số hạt trả) trong `State.Events`, giống thông báo ở bước 4 ([0017](0017-dinh-tre-thong-bao-nha-mau-va-dem-ket.md)).
- **Mô phỏng ghi vào world:** xới xong → mô phỏng đổi vật liệu ô qua cùng đường sửa ô của world → mesh dựng lại như đào/đặt. Cách nối cụ thể (sự kiện hay gọi trực tiếp) chốt trong brief.
- **Hiển thị ô ruộng:** một màu đỉnh mới trong mesher khối. Ô ruộng **không có mép cỏ**; sườn hiện màu đất.
- **"Không dừng trên ô ruộng" của [0019](0019-chan-nhau-ranh-quanh-nha-khong-dung-tren-ruong.md)** áp cho mọi ô của lệnh trồng cây, kể cả ô chưa xới, để 6b không phải sửa lại khi 6a đổi định nghĩa.

## Hệ quả
- **Ước lượng (AI) bước 6a: 2,5–4 ngày-người:**
  - vật liệu ô ruộng + màu hiển thị + đào ra đất: 0,5–1;
  - xới xong đổi vật liệu, mô phỏng ghi vào world, mesh cập nhật: 1–1,5;
  - huỷ vụ khi ô bị đào/che, trả hạt, ô rời lệnh, trở lại đất, sự kiện: 1–1,5.
- **Kế hoạch simulation** 23–36,5 → **25,5–40,5**. **M1** 59–95,5 → **61,5–99,5**. M2 không đổi: phần ô ruộng trong ideas chưa được cộng vào M2. Phần "công trình lơ lửng khi đào dưới kho/nhà" vẫn để sau. **Tổng M1–M4** 535–1.002,5 → **537,5–1.006,5** ngày-người (~12,4–23,2 năm ở 10 giờ/tuần) — [roadmap](../05-roadmap.md).
- **Thứ tự M1:** 6a → 6b → 7 → 8. Bước 7 đo hiệu năng có luật 6a.
- **Bước 8 (môi trường sống):** đếm ô cỏ phải đổi khi xới (cỏ → ô ruộng); ô ruộng không tính là đất hay cỏ.
- **Test hiện có:** test bước 3–6 đặt lệnh trên ô đất/cỏ vẫn chạy. Test nào kiểm vật liệu sau xới, hoặc mã băm world sau khi chạy mô phỏng, phải cập nhật. Bộ sinh địa hình không đổi.
- Cập nhật: [mechanics](../02-gameplay/mechanics.md), [simulation-tech](../04-tech/simulation-tech.md#kế-hoạch-prototype-m1), [world-tech](../04-tech/world-tech.md) (bảng vật liệu), [roadmap](../05-roadmap.md), [ideas](../ideas.md), [glossary](../glossary.md); task contract [m1-sim-buoc-6a-luat-o-ruong](../../.ai/tasks/m1-sim-buoc-6a-luat-o-ruong.md).
