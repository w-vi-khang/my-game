# 0041 — Nước trong rãnh hẹp chảy không giới hạn; thác nước demo

> Trạng thái: Chốt
> Ngày: 2026-10-25
> Thay đổi:
> - [0040](0040-nuoc-chay-nguon-lan-gioi-han-m1.md) mục 3 "Lan ngang": mỗi ô đi xa thêm thì giảm một mức, tối đa N ô. Nay chỉ giảm khi nước ở chỗ rộng; trong rãnh hẹp thì không giảm.

## Bối cảnh
User chơi thử brief 0075 (nước chảy 0040), ngày 2026-10-25. Ảnh chụp: đào rãnh dài từ ao ra, nước dừng giữa rãnh sau 7 ô. User yêu cầu:
- "Nước trong rãnh thì nó vẫn sẽ chảy chứ, chỉ khi nước chảy thoát ra một không gian rộng thì mới tránh nó lan ra khắp nơi."
- "Hãy làm một thác nước."

## Quyết định
User, 2026-10-25:
1. Nước trong **rãnh hẹp** chảy tiếp, không giới hạn số ô.
2. Giới hạn lan (N ô, 0040) chỉ áp dụng khi nước ra **chỗ rộng**.
3. Có một **thác nước** trong world demo.

Đề xuất của Planner, đổi được, ghi [số tạm] / chưa chốt:
- **Ô hẹp:** ô nước có tối đa 2 trong 4 ô ngang không phải khối đặc. Rãnh rộng 1 ô thì mọi ô đều hẹp; chỗ rộng (≥ 3 phía trống) thì giảm mức như 0040. Ngưỡng 2 nằm trong `design/data/water_m1.json`.
- **Rút khi mất nguồn:** trong rãnh nước không giảm mức nên không tự rút được. Khi một ô nước biến mất (bị đặt khối, bị rút), kiểm tra khối nước đang chảy liền kề còn nối với nguồn không. Không còn → đánh dấu cả khối "đang rút", các ô đó giảm mức như chỗ rộng, rút hết trong ≤ N nhịp. Kiểm tra tối đa 4096 ô mỗi khối [số tạm]; khối lớn hơn coi như còn nguồn.
- **Rơi vào nước nguồn:** nước rơi xuống mặt ao/hồ/biển thì nhập vào, không lan ra trên mặt nước. Nếu không có luật này, thác đổ vào ao sẽ tràn lên cỏ quanh ao.
- **Thác nước demo:** cạnh ao trong đồng bằng SimView có một khối đất cao 4 ô. Trên đỉnh có 1 ô nguồn và rãnh 1 ô dẫn ra mép nhô trên ao. Nước rơi thẳng xuống ao. Dùng đất cho khối để không đổi số ô đá lộ thiên của L3.

## Hệ quả
- Ước lượng (AI): +1–2 ngày-người M1. Cập nhật [roadmap](../05-roadmap.md), [world-tech](../04-tech/world-tech.md) (dòng 5f), [mechanics](../02-gameplay/mechanics.md), [glossary](../glossary.md).
- Ô dùng bit 15 (cuối cùng) làm cờ "đang rút"; không còn bit dự phòng.
- Code: brief 0076 (task [m1-world-nuoc-chay](../../.ai/tasks/m1-world-nuoc-chay.md)).
