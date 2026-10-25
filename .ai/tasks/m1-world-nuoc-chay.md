# Task: M1 world 5e — nước chảy: nguồn + lan giới hạn (cao → thấp, hố được lấp)

> Model thực thi: Sonnet · medium (AGENTS.md §10.2).
> Trạng thái:
> - Luật: [0040](../../design/decisions/0040-nuoc-chay-nguon-lan-gioi-han-m1.md) (user chốt 2026-10-25); [mechanics "Nước chảy"](../../design/02-gameplay/mechanics.md#voxel-world--luật-m1); [world-tech "Nước chảy"](../../design/04-tech/world-tech.md#nước-chảy) + dòng 5e của kế hoạch prototype.
> - Ước lượng 2–4 ngày-người (0040, roadmap M1).
> - Task contract do Planner lập 2026-10-25 từ 0040 (user đã chọn phương án A, làm ngay ở M1). Brief [0075](../briefs/0075-world-5e-nuoc-chay.md) (5e, đã duyệt); sau chơi thử user đổi luật rãnh → [0041](../../design/decisions/0041-nuoc-chay-trong-ranh-hep-va-thac-nuoc.md), brief [0076](../briefs/0076-world-5f-nuoc-ranh-hep-thac-nuoc.md) (5f, +1–2 ngày-người).

## Mục tiêu
- Nước chảy từ nguồn ra ô trống: rơi thẳng xuống, lan ngang tối đa N ô (mặc định 7), hố được lấp, mất nguồn thì rút dần.
- Trường hợp user gặp (sim 8b, 2026-10-25): đào ô cát cạnh ao 4 × 4 → hố phải đầy nước.
- **Pillar:** P1 (cải tạo thế giới) — mechanics bảng cơ chế dòng "Nước chảy" / "Voxel world".
- **Thành phần:** client Unity. Luật ở `Game.Core` (`WorldEditor`, `Cell`), nhịp ở `Game.Client` (`TerrainChunkView`), số trong `design/data/water_m1.json`.

## Ngoài phạm vi
- Bảo toàn lượng nước, vùng động/ngủ, người chơi tạo nguồn (M2 — 0040).
- Mặt nước thấp dần theo mực (M1 vẽ đầy ô — đề xuất Planner trong 0040).
- Nước gặp vật thể chiếm chỗ (nhà, kho, cây): xử lý như biển loang hiện nay (bỏ qua chiếm chỗ). Sinh vật đứng trong ô bị ngập: luật 0015 có sẵn.
- Hai nguồn kề nhau tạo nguồn mới, nước chảy ưu tiên về chỗ trũng gần nhất (kiểu Minecraft) — không làm.
- Tua nhanh, lưu world.

## Tiêu chí chấp nhận
Cột "Xong khi" dòng 5e của world-tech:
- [ ] EditMode: hố cạnh ao đầy; nước rơi từ cao rồi lan đúng N ô; chặn/bỏ nguồn thì rút hết; biển 0013 vẫn đúng (test cũ `SeaFloodTests` đạt nguyên); tất định; mã băm world sinh ra không đổi (`GoldenHashTests`).
- [ ] PlayMode: trong scene, đào ô cát cạnh ao → hố thành nước sau vài nhịp.
- [ ] User xem: đào cát cạnh ao → nước tràn vào hố; đào hố sâu cạnh ao → đầy; đổ nước từ trên cao → rơi xuống rồi lan có giới hạn; lấp chỗ nối với ao → nước rút.

## Ràng buộc
- Số nguyên, tất định, không phụ thuộc thứ tự duyệt (tính trạng thái mới từ trạng thái đầu nhịp rồi mới ghi).
- Chỉ tính lại ô trong hàng chờ quanh chỗ vừa đổi; không quét cả world; world sinh ra đứng yên tới khi có đào/đặt gần đó.
- Không đổi dữ liệu world sinh ra (nguồn = cách ghi hiện tại) → mã băm world không đổi.
- `Game.Core` không tham chiếu UnityEngine.

## Quyết định đã chốt (ai chốt, khi nào)
- Luật nguồn / rơi / lan ngang / hố được lấp / rút / nhịp: user, 0040 (2026-10-25).
- Đề xuất Planner 2026-10-25, đổi được:
  - Ghi trong ô: bit 13 = nước đang chảy (tắt = nguồn), bit 14 = nước đang rơi; mực 3 bit = mức lan 1–7 (world-tech "Dữ liệu world").
  - Mức lan: ô cách nguồn d ô có mức N + 1 − d; ô mức 1 không lan tiếp. Ô rơi có mức N; khi ô dưới nó là khối đặc hoặc nước thì lan sang ngang với mức N.
  - Rơi: cả cột trống bên dưới thành nước trong cùng nhịp (như 0013). Lan ngang và rút: 1 ô mỗi nhịp.
  - Biển loang 0013 giữ nguyên thuật toán, chạy trước phần nước chảy trong cùng nhịp; chỉ ô nguồn mới kích hoạt biển loang.
  - `CellChanged` chỉ phát khi vật liệu đổi (rỗng ↔ nước); đổi mức/cờ không phát.
  - `water_m1.json`: `flowMaxDistance` 7, `stepsPerSecond` 8 [số tạm, chưa chốt].

## Cập nhật 2026-10-25 — 0041 (user, sau chơi thử 0075)
- Nước trong rãnh hẹp chảy không giới hạn; giới hạn N ô chỉ ở chỗ rộng; có thác nước demo cạnh ao.
- Đề xuất Planner (đổi được): ô hẹp = ≤ 2 phía ngang không đặc; mất nguồn → kiểm khối (≤ 4096 ô) rồi bật cờ đang rút (bit 15); rơi vào nước nguồn thì nhập vào; thác = khối đất cạnh ao, rãnh 1 ô ra mép nhô trên ao.
- Tiêu chí thêm: rãnh hẹp đầy hết, ra chỗ rộng lan đúng N ô; chặn rãnh → phần sau rút; rãnh vòng kín mất nguồn → rút; thác đổ vào ao không tràn; user xem thác.

