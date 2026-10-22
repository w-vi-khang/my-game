# 0026 — Hàng đợi vụ chia thành ô đợi theo loại cây, mỗi ô có số lượng tối đa

> Trạng thái: Chốt
> Ngày: 2026-10-22
> Mục 4–5 được thay một phần bởi [0027](0027-o-tien-trinh-tam-dung-xoi-khong-tra-hat.md) (ô tiến trình, vụ rời ô đợi lúc gieo).
> Thay đổi: [0025](0025-nguoi-choi-xoi-dat-chon-giong-hang-doi.md) mục "Chọn giống" và "Xoá vụ trong bảng hàng đợi" (hàng đợi 2 chỗ, mỗi chỗ 1 vụ).

## Bối cảnh
User chơi thử bảng ô ruộng của brief 0030 (sim bước 7c, 2026-10-22): hàng đợi 2 chỗ, mỗi chỗ 1 vụ → chọn A rồi A là đầy, không lên lịch được nhiều vụ cùng loại. User muốn mỗi chỗ là **một loại cây** kèm **số lượng**.

## Các lựa chọn
- Giữ 0025 (mỗi chỗ 1 vụ): đơn giản, nhưng phải quay lại bấm ô ruộng sau mỗi 2 vụ.
- **Ô đợi theo loại + số lượng** (user): lên lịch nhiều vụ một lần, thứ tự rõ. Thêm việc: đổi cấu trúc hàng đợi trong lõi + bảng.

## Quyết định
User, 2026-10-22:
1. Hàng đợi vụ của ô ruộng có **N ô đợi**; mỗi ô đợi giữ **một loại cây** và **số lượng 1..M**. Ô ruộng cấp 1: **N = 2, M = 3** [số tạm, trong `design/data/sim_m1.json`].
2. **Chọn giống loại X** (nút "+ Cây X"): ô đợi cuối cùng là X và chưa đủ M → số lượng +1; ngược lại → thêm ô đợi mới X với số lượng 1 (nếu còn ô đợi trống). Ví dụ 2 ô × 3: bấm A 5 lần → A3, A2. A3 rồi B3 được. 3 ô × 3: A3, B3, A3, trồng đúng thứ tự đó.
3. Trên mỗi ô đợi đang có cây: nút **+** tăng số lượng của ô đó nếu chưa đủ M; nút **−** giảm 1. Về 0 → xoá ô đợi đó, các ô sau đôn lên.
4. **Trồng theo thứ tự:** vụ đang làm lấy loại của ô đợi đầu; thu hoạch xong (hoặc vụ bị huỷ) → ô đầu −1; về 0 → ô sau đôn lên.
5. **"−" ở ô đầu khi vụ của ô đó đang làm:** còn > 1 → chỉ bớt vụ chờ, vụ đang làm tiếp tục; chỉ còn đúng vụ đang làm → huỷ vụ đó như 0025 (trả hạt theo pha, hàng đã thu vẫn chở về, ô vẫn là ô ruộng).
6. **Không gộp** hai ô đợi cùng loại khi chúng thành kề nhau (A3, B2, A1 → xoá B → A3, A1).
7. Các luật khác của 0025 giữ nguyên: hết hàng đợi → ô đứng yên; đào/che ô ruộng → huỷ vụ, xoá cả hàng đợi (0020).

## Hệ quả
- Lõi `Core/Sim` (`FarmPlot.Queue`, `EnqueueCrop`, `RemoveCrop`, `SimConfig.farm`, `SimDemo`), test lõi, `SiteStatus` và bảng ô ruộng trong `SimView` đổi theo → **M1 simulation bước 7d**, ước lượng **1–2 ngày-người** (user, 2026-10-22). Brief 0030 (7c) đã duyệt theo 0025.
- Cập nhật: [glossary](../glossary.md) (Ô đợi), [mechanics](../02-gameplay/mechanics.md), [roadmap](../05-roadmap.md).
