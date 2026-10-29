# 0044 — Nước: ô thấp hơn mặt nguồn thì đầy (lấp hố rộng)

> Trạng thái: Chốt
> Ngày: 2026-10-29 (user chọn phương án 2026-10-29)
> Thay đổi: [0043](0043-nuoc-cay-cha-con-tang-muc-boi-tuong.md) mục 2–3 (lan giảm 1 mức/ô, tăng mức bởi tường) — thêm một trường hợp tăng mức. Phần còn lại của 0043 giữ nguyên.

## Bối cảnh
User chơi thử gộp 0079–0081 (2026-10-29): đào hố rộng 2 lớp cạnh ao, nước không lấp đầy [user]. Planner đo (brief 0084, mục Bối cảnh) [run]: code đúng luật 0043. Mỗi ô lan giảm 1 mức, nên trong hố rộng nước chỉ tới tối đa 5 ô tính từ chỗ vào, đáy xa chỉ có nước mức 0–1.

## Phương án
- **A. Ô thấp hơn mặt nguồn thì đầy:** ô nước chảy nằm thấp hơn ô nguồn gốc của cây thì lên mức 5. Lớp ngang mặt ao vẫn lan giảm dần.
- B. Như A, và lớp ngang mặt ao trong vùng kín cũng đầy.
- C. Giữ 0043.

## Quyết định
Chọn **A** [user].
1. Ô nước chảy có `y` < `y` của ô nguồn gốc ở gốc cây của nó → mức 5, phát sóng mức như "tăng mức bởi tường" (0043 mục 3). Vì ô con lan từ ô mức 5 cũng thấp hơn nguồn, cả lớp dưới của hố đầy mức 5 [user].
2. Lớp có `y` ≥ `y` nguồn gốc: luật 0043 không đổi (lan giảm 1 mức/ô, tăng mức bởi tường) [user].
3. **Giới hạn** (đề xuất Planner, chưa chốt): chỉ áp mục 1 cho ô cách ô nguồn gốc của cây ≤ `basinFillRadius` = 16 ô theo phương ngang (khoảng cách Chebyshev) [số tạm], trong `design/data/water_m1.json`. Lý do: nước đổ từ vách cao xuống vùng đất thấp rộng không làm ngập vô hạn.
4. Không dâng lên (0043 giữ): hố không đầy cao hơn mặt nguồn.

## Ảnh hưởng
- [world-tech](../04-tech/world-tech.md) mục "M1 — cây cha–con", dòng 5j (lỗi 2).
- Thành phần: client Unity — `Game.Core` `WorldEditor` (luật nước), `WaterConfig` + `water_m1.json` (tham số). Ước lượng 0,5–1 ngày-người, trong 5j (2–4), không đổi roadmap.
- Test cũ ghi hành vi 0043 cho hố rộng (`WideHolePastLevelRadiusStaysEmpty`, brief 0084) phải đổi kỳ vọng theo quyết định này.
