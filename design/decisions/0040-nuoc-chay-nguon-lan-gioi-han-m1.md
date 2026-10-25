# 0040 — Nước chảy M1: nguồn + lan giới hạn (cao → thấp, hố được lấp)

> Trạng thái: Chốt
> Ngày: 2026-10-25
> Thay đổi:
> - [0013](0013-nuoc-lan-theo-nhip.md): luật biển lan chỉ ở ô ≤ mực nước biển, mỗi ô đầy hoặc rỗng. Nay mở rộng thành nước chảy theo luật dưới đây.
> - [world-tech — Nước chảy](../04-tech/world-tech.md#nước-chảy): M1 dùng nước tĩnh; nước chảy làm ở M2.
> - Ghi chú "nước không có nguồn không cạn — bảo toàn lượng nước" (user, 2026-10-13) **không làm ở M1**. Để M2 xem lại.

## Bối cảnh
User chơi thử sim 8b (brief 0074), ngày 2026-10-25. Đào ô cát cạnh ao 4 × 4 thì để lại một hố trống, nước ao không chảy vào. User yêu cầu:
- nước phải tràn ra;
- nước chảy từ nơi cao xuống nơi thấp;
- nước chảy vào một hố thì hố đầy;
- cần giới hạn lan, để nước từ trên cao chảy xuống không lan ra mãi mãi.

## Các lựa chọn
- **A. Nguồn + lan giới hạn:**
  - ao và biển là nguồn, không cạn;
  - chảy ngang thì mất dần mực, đi tối đa N ô;
  - gặp chỗ thấp hơn thì rơi xuống và lan tiếp.
  - Ước lượng của AI: 2–4 ngày-người.
- **B. Bảo toàn lượng nước** (mực 0–7, vùng động/ngủ; ghi chú 2026-10-13). Ước lượng 8–15 ngày-người.
- Milestone: đưa vào M1 ngay / để M2.

## Quyết định
User, 2026-10-25: **A, làm ngay trong M1.**
1. **Nguồn** không bao giờ cạn:
   - ô nước có sẵn khi sinh world (biển, hồ, ao demo);
   - ô ≤ mực nước biển thông với biển ngoài map (như 0013).
2. **Rơi xuống:** nước ở trên một ô trống thì chảy thẳng xuống. Nước đang rơi được tính lại mức lan đầy đủ khi chạm đáy, nên chảy từ cao xuống thấp được xa.
3. **Lan ngang:** từ nguồn, hoặc từ chỗ nước rơi chạm đáy, nước lan sang 4 ô ngang còn trống. Mỗi ô đi xa thêm thì giảm một mức. Hết mức thì dừng. Số ô lan tối đa là tham số, mặc định **7** [số tạm].
4. **Hố được lấp:** ô trống mà nước chảy tới thì thành ô nước. Hố cạnh ao đầy nước.
5. **Rút nước:** ô nước đang chảy mà không còn đường nối về nguồn (nguồn bị chặn hoặc bị đào/lấp) thì rút dần theo nhịp.
6. **Nhịp:** chạy theo nhịp cố định như 0013 (mặc định 8 nhịp/giây), tất định, không phụ thuộc khung hình. Người chơi đặt khối vào chỗ nước sắp chảy tới thì chặn được.

Đề xuất của Planner, đổi được, ghi [số tạm] / chưa chốt:
- Hiển thị M1: mỗi ô nước vẽ như hiện nay (đầy ô), chưa vẽ mặt nước thấp dần theo mực. Mức lan chỉ dùng cho logic.
- Số ô lan tối đa và số nhịp/giây nằm trong JSON `design/data/`.

## Hệ quả
- Ước lượng (AI): +2–4 ngày-người cho M1. Planner cập nhật [roadmap](../05-roadmap.md).
- Cập nhật: [world-tech](../04-tech/world-tech.md) mục Nước chảy + kế hoạch prototype; [mechanics](../02-gameplay/mechanics.md) luật biển tràn; [glossary](../glossary.md) (nguồn nước, nước chảy) nếu thiếu.
- Code (task mới): `WorldEditor` (thay mặt trước dòng nước của 0013), mô phỏng tìm đường và môi trường sống nhận `CellChanged` của nước như hiện nay. Test: hố cạnh ao đầy; nước rơi từ cao rồi lan đúng N ô; chặn hoặc bỏ nguồn thì rút; biển (0013) vẫn đúng.
