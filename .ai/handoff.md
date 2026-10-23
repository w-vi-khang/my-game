# Handoff — 2026-10-24

## Đang làm
Task `m1-world-7l-dieu-khien-pc` ([task contract](tasks/m1-world-7l-dieu-khien-pc.md)).
- **7l c1** (brief 0058) đã duyệt 2026-10-24.
- **7l c2** (brief 0059: cầm vật phẩm từ túi, bỏ Tab tạm, vòng tầm khi chuột phải ngoài tầm, F vùng + gió) **đã duyệt** 2026-10-24 (review đầy đủ + user xem đạt, commit `431bca3`). Giờ thực tế: brief chỉ ghi vòng 3 (~1,2 giờ); các vòng trước thiếu giờ (đã ghi "≥ 73 phút" ở world-tech + roadmap). Khoá `bag.empty` không còn dùng → dọn ở 7l d.
- **7l c3** (brief [0060](briefs/0060-7l-c3-mau-mana-client.md): máu / mana nối vào client) **đã duyệt** 2026-10-24 (review đầy đủ + user xem đạt). **7l c xong**; giờ thực tế ghi ở world-tech dòng 7l c + roadmap.
  - Số tạm chưa chốt: `manaFlashMs` 600; đề xuất Planner: rơi xuống nước không mất máu, điểm đầu = chân + 1,5.
- File lạ chưa track: `client/Assets/InitTestScene*.unity(.meta)` — scene tạm của Unity Test Runner; xoá khi Editor không chạy test.

- **7l d** chia 3 brief (task contract mục Quyết định; user 2026-10-24: làm hết mục 7, "user xem" gộp một lần cuối task): **d1** [0061](briefs/0061-7l-d1-tui-o-vuong-loi.md) **đã duyệt** 2026-10-24 (code ok, chờ user xem gộp cuối; Executor 16 phút; giờ ghi ở world-tech dòng 7l d — roadmap chưa cộng vì `design/05-roadmap.md` đang có thay đổi chưa commit của phiên khác). Tiếp: [0062](briefs/0062-7l-d2-giao-dien-tui-tui-phu-kho.md) giao diện → [0063](briefs/0063-7l-d3-chuyen-do-keo-tha-tab-123.md) chuyển đồ.
- Danh sách chơi thử gộp cuối: lấy từ mục "User tự test" + Review của từng brief 0061–0063 (và 7l e, f, sim 7g).

## Bước tiếp
1. Executor làm brief **0062** rồi **0063** (Planner review từng brief). ~~Planner (Opus) viết brief **7l d** — túi / túi phụ / kho ô vuông (world-tech dòng 7l d): B túi, P túi phụ giữa-dưới, kho mở bằng chuột phải, hình + số, trang, kéo thả cả chồng, click trái → bảng thông tin, chuột phải ô → cầm, 1–0 túi phụ, Tab đổi đích chuyển; thay bảng túi cũ + phím 1–4 tạm; dọn khoá `bag.empty`. Tra docs 6000.4 kéo thả uGUI (`IDragHandler`…). Có thể chia nhiều brief.~~ (đã viết 0061–0063)
2. **7l e** — HUD gọn (avatar + máu + mana thay thanh tạm c3, menu phép G, icon ô ruộng / sinh vật, ≥ 30 FPS điện thoại).
3. **7l f** — bảng đổi phím.
4. **sim 7g** theo [0036](../design/decisions/0036-sinh-vat-chi-lam-cong-doan-duoc-giao.md) mục 1–4 — task riêng.

## Model đề xuất
Opus · medium (viết brief 7l d).
