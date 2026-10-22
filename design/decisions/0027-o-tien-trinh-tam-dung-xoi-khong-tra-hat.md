# 0027 — Ô tiến trình, tạm dừng xới, huỷ vụ không trả hạt; bảng ô ruộng dạng ô vuông

> Trạng thái: Chốt
> Ngày: 2026-10-22
> Thay đổi: [0026](0026-hang-doi-o-doi-theo-loai.md) mục 4–5 (vụ đang làm nằm ở ô đợi đầu); [0025](0025-nguoi-choi-xoi-dat-chon-giong-hang-doi.md) và [0020](0020-o-ruong-tao-khi-xoi-va-huy-vu.md) phần trả hạt khi huỷ vụ.

## Bối cảnh
Khi viết task contract sim bước 7d (2026-10-22), user mô tả bảng ô ruộng mong muốn và đổi thêm vài luật của 0026/0025/0020. Ngoài ra user lưu ý: số ô đợi, số tối đa mỗi ô đợi và giới hạn số ô ruộng sau này đổi theo nâng cấp (tài năng, cấp người chơi).

## Các lựa chọn
- Giữ 0026 (vụ đang làm là phần đầu của ô đợi đầu, huỷ trả hạt theo pha): ít đổi lõi, nhưng bảng không tách được "đang trồng" và "đang chờ".
- **Ô tiến trình riêng + tạm dừng xới + huỷ không trả hạt** (user): bảng rõ ràng; thêm việc ở lõi (vụ hiện tại, cờ tạm dừng) và sửa test huỷ vụ của 0020/0025.

## Quyết định
User, 2026-10-22:
1. **Ô tiến trình:** mỗi ô ruộng có một ô tiến trình (vụ hiện tại) tách khỏi các ô đợi. Vụ **rời ô đợi đầu (−1) khi bắt đầu gieo**; lúc xới lại (trước gieo) vụ vẫn nằm trong ô đợi. Thu hoạch xong → ô tiến trình trống → vụ kế bắt đầu (xới lại rồi gieo).
2. **Tạm dừng xới:** khi ô ruộng đang xới lại, nút xoá trên ô tiến trình **tạm dừng** việc xới (không huỷ, không thêm nút riêng); người chơi chỉnh ô đợi rồi bấm **tiếp tục**, hoặc đóng bảng ô ruộng → tự tiếp tục.
3. **Huỷ vụ không trả hạt:** nút xoá trên ô tiến trình (từ lúc gieo trở đi) huỷ vụ hiện tại, **không trả hạt**. Đào ô ruộng / đặt khối (hay nước tràn) lên ô ruộng khi đang có vụ cũng **không trả hạt**. Hàng đã thu vẫn chở về kho (giữ 0020).
4. **Chọn hạt X:** ô đợi cuối là X và chưa đủ M → +1; ngược lại → ô đợi trống gần nhất (giữ 0026 mục 2). **−/+** dưới ô đợi chỉ đổi số lượng ô đợi đó (về 0 → xoá, các ô sau đôn lên); **nút x** góc trên phải ô đợi xoá cả ô đợi, các ô sau đôn lên. Không gộp ô cùng loại kề nhau (giữ 0026 mục 6).
5. **Bảng ô ruộng (popup):** danh sách hạt giống, mỗi loại một ô vuông bo góc có hình cây + icon hạt ở góc trên trái; N ô vuông ô đợi (trống hoặc hình cây), dưới mỗi ô thanh "− số/M +"; một ô tiến trình có hình cây (hoặc trạng thái xới) + thanh tiến trình (vd "Xới 60%", "Cây A · lớn 40%"). Hình tạm (ô màu + chữ), vẽ OnGUI; art sau.
6. **Chừa chỗ cho nâng cấp:** mỗi ô ruộng giữ số ô đợi N và tối đa M **của riêng nó** (giá trị gốc từ `design/data/sim_m1.json`, cấp 1: N = 2, M = 3 [số tạm]). Nâng cấp tài năng/cấp người chơi (+ô đợi, +tối đa) và **giới hạn số ô ruộng** → [ideas](../ideas.md), không làm ở M1.

## Hệ quả
- Sim bước 7d làm theo 0026 + quyết định này; ước lượng 1–2 → **2–3 ngày-người** (user xác nhận 2026-10-22). Cấu hình `farm.queueSlots` / `farm.slotMaxCount`, `configVersion` 3 (user xác nhận 2026-10-22).
- Cập nhật: [glossary](../glossary.md) (Ô tiến trình, Ô đợi), [mechanics](../02-gameplay/mechanics.md), [simulation-tech](../04-tech/simulation-tech.md) (bước 6a/7d), [roadmap](../05-roadmap.md), [ideas](../ideas.md).
- Test lõi của 0020/0025 kiểm trả hạt khi huỷ phải đổi kỳ vọng (không trả).
