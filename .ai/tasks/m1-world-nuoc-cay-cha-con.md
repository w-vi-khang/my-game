# Task: M1 world 5h + 5i — nước theo cây cha–con, mesh nước liền + bo góc

> Model thực thi: Sonnet · medium (AGENTS.md §10.2).
> Trạng thái: Đang làm — 5h brief [0079](../briefs/0079-world-5h-nuoc-cay-cha-con.md) đã duyệt 2026-10-28; 5i brief 0080.
> - Luật: [0043](../../design/decisions/0043-nuoc-cay-cha-con-tang-muc-boi-tuong.md) (user chốt 2026-10-27); [world-tech "Nước chảy"](../../design/04-tech/world-tech.md#nước-chảy) mục "M1 — cây cha–con" + dòng 5h, 5i; [mechanics "Nước chảy"](../../design/02-gameplay/mechanics.md#voxel-world--luật-m1).
> - Ước lượng 3,5–5,5 ngày-người (5h 2–3, 5i 1,5–2,5), roadmap M1.
> - Task contract do Planner lập 2026-10-27 từ bộ quy tắc user đưa + 4 câu trả lời của user.

## Mục tiêu
- Thay logic nước chảy (0040–0042) bằng cây cha–con của 0043: mức 0–5, lan theo nhịp, rơi, tăng mức bởi tường, phát sóng mức, xoá dây chuyền + tái tạo.
- Mesh nước một bề mặt liền, bo góc chỗ nước đổi hướng (mép tràn, chân thác).
- **Pillar:** P1 (cải tạo thế giới) — mechanics bảng cơ chế dòng "Nước chảy" / "Voxel world".
- **Thành phần:** client Unity. Luật ở `Game.Core` (`WorldEditor`, `Cell`, `WaterConfig`), nhịp ở `Game.Client` (`TerrainChunkView`), mesh ở `Game.World` (`BlockMesher`), số trong `design/data/water_m1.json`.

## Ngoài phạm vi
- Bảo toàn lượng nước, người chơi tạo nguồn (M2).
- Ô nối 0042 (đã chuyển vào ideas).
- Lưu world (id/parent_id không lưu), tua nhanh.
- Nước gặp vật thể chiếm chỗ: như hiện nay (bỏ qua chiếm chỗ).

## Tiêu chí chấp nhận
- [x] 5h (0079, 2026-10-28; user xem gộp sau 5i): cột "Xong khi" dòng 5h của world-tech (test EditMode) + PlayMode nước cũ vẫn đạt.
- [ ] 5i: cột "Xong khi" dòng 5i.
- [ ] User xem: đào bờ ao, đào rãnh 1 ô, đổ nước từ trên cao, lấp nguồn, thác demo — nước chảy/rút đúng luật, nhìn liền, có bo góc.

## Ràng buộc
- Tất định: xử lý theo `id` tăng dần, thứ tự hướng +z, +x, −z, −x.
- Chỉ xử lý ô được kích hoạt; không quét cả world; world sinh ra đứng yên tới khi có đào/đặt gần đó.
- Mã băm world sinh ra không đổi (không đụng bộ sinh world).
