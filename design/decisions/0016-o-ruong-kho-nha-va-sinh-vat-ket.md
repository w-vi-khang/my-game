# 0016 — Ô ruộng có hàng đợi, đứng ô kề để làm, kho là công trình; nhà sinh vật + sinh vật bị kẹt ở simulation bước 4

> Trạng thái: Chốt
> Ngày: 2026-10-14
> Thay đổi: dòng "Lệnh làm việc … 7–11" (→ 8–12) và "Tìm đường" (sau 0015: 5–8 → 6–9) của [0007](0007-m1-toi-gian.md), thêm "Nhà sinh vật tối thiểu 1"; hàng bước 3 và 4 trong [kế hoạch prototype simulation](../04-tech/simulation-tech.md#kế-hoạch-prototype-m1) (user duyệt 2026-10-13); thêm luật vào [mechanics — Kho & lệnh trồng cây](../02-gameplay/mechanics.md#kho--lệnh-trồng-cây--luật-m1) và [Tìm đường sinh vật & đình trệ](../02-gameplay/mechanics.md#tìm-đường-sinh-vật--đình-trệ); thêm cơ chế [Nhà sinh vật](../02-gameplay/mechanics.md#nhà-sinh-vật).

## Bối cảnh
Viết task contract simulation bước 3 (lệnh trồng cây + một kho, 2026-10-14). Luật M1 chưa nói sinh vật đứng ở đâu khi làm ruộng và giao hàng vào kho, các ô của một lệnh chạy theo nhịp nào, thu hoạch xong thì sao, sức chứa kho tính gì. User vừa thêm cơ chế "sinh vật bị kẹt → hiện lại ở nơi ở" nhưng M1 chưa có nơi ở.

## Quyết định
**Chốt (user, 2026-10-14):**
- **Ô ruộng: ưu tiên đứng ở ô kề** để xới/gieo/tưới/thu hoạch; không có ô kề nào tới được thì đứng ngay trên ô ruộng.
- **Kho là công trình có sức chứa**; sinh vật vận chuyển tới **một ô kề kho** là giao hàng được.
- **Mỗi ô ruộng có chuỗi công đoạn riêng và một hàng đợi vụ.** Người chơi xếp lịch, vd chọn lúa, lúa nữa (hàng đợi 2), rồi cà (sau 2 lúa), rồi lúa (sau cà). Ô được giao việc liên tục tới khi hết hàng đợi hoặc không đủ điều kiện sản xuất (vd hết hạt). **Ô ruộng cấp 1 có 2 chỗ trong hàng đợi.**
- Mỗi lần thu hoạch: **1 lượt vận chuyển mang cả 1 nông sản + 1 hạt**; sức chứa kho 50 **tính chung** nông sản và hạt.
- Khi vận chuyển, sinh vật mang một hộp nhỏ; khi xới, tưới... có biểu tượng nhỏ bên cạnh — hiển thị ở bước 6.
- **Sinh vật bị kẹt** làm ở **bước 4** (cùng đình trệ): kẹt = **không có đường về nhà** liên tục **30 giây** (300 nhịp, số tạm trong `sim_m1.json`) → biến mất, hiện lại ở nhà.
- **Nhà sinh vật:** mỗi sinh vật làm việc có nhà — một công trình, hoặc nơi người chơi tự xây (có tường bao quanh, cao 1 ô là đủ; có khoảng hở để lắp cửa — cửa là vật phẩm công trình nhiều loại, nhiều kích thước; có 2 nội thất). Sinh vật có nhà mới làm việc được; sinh vật rảnh hoạt động trong một bán kính quanh nhà. Phần mở đầu: sinh vật có nhà sẵn. Nhà có nhiều kích thước; mẫu ban đầu 2 × 2 × 2, sức chứa 2; sức chứa nhà người chơi xây theo diện tích và tường bao.
- **M1 chỉ làm nhà tối thiểu ở bước 4:** nhà mẫu 2 × 2 × 2 đặt bằng code/lệnh debug, mỗi sinh vật gắn một nhà; dùng làm nơi hiện lại khi kẹt + điều kiện "có nhà mới được giao việc". Nhà người chơi tự xây, nhiều kích thước, sinh vật rảnh đi lang thang quanh nhà → M2.

**Planner (2026-10-14), chi tiết cho bước 3:**
- M1 một loại cây, nhưng hàng đợi lưu loại cây của từng vụ (đổi sang nhiều loại ở M2 không phải làm lại). Hàng đợi gồm cả vụ đang làm; thu hoạch xong thì vụ rời hàng đợi, vụ kế bắt đầu lại từ xới.
- Ô kề = 4 hướng ngang quanh ô phía trên ô ruộng, lệch cao tối đa 1. Chọn ô kề có đường ngắn nhất (A\* nhiều đích, tất định). Kho cũng dùng luật ô kề, nhưng không đứng lên kho được.
- Kho chiếm ô: sinh vật không đi qua, không đặt khối vào được.
- Hạt trừ khi giao công đoạn gieo (trả lại nếu bị bỏ giữa chừng); kho hết hạt → công đoạn gieo chờ. Chỗ trong kho giữ trước khi giao công đoạn vận chuyển; không đủ chỗ → vận chuyển chờ. Thông báo các lý do này ở bước 4.

## Hệ quả
- Ước lượng (AI): bước 3 **2–4 → 3–5** (hàng đợi theo ô, ô kề, kho chặn đường); bước 4 **1–2 → 3–4** (sinh vật bị kẹt ~1, nhà tối thiểu ~1). Kế hoạch simulation 16–27 → **19–30**; M1 52–86 → **55–89**.
- M2 thêm [ý tưởng ước lượng]: nhà người chơi tự xây + nhiều kích thước + sức chứa theo diện tích 4–7, sinh vật rảnh đi quanh nhà 1–2, hàng đợi nhiều loại cây + cấp ô ruộng 1–2 → M2 321–612 → **327–623**. Tổng M1–M4 523–984 → **532–998** ngày-người (~12,3–23,0 năm ở 10 giờ/tuần) — [roadmap](../05-roadmap.md).
- Cập nhật: [mechanics](../02-gameplay/mechanics.md), [simulation-tech](../04-tech/simulation-tech.md#kế-hoạch-prototype-m1), [roadmap](../05-roadmap.md), [glossary](../glossary.md), [summary](../summary.md), [risks](../04-tech/risks.md).
