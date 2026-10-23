# Handoff — 2026-10-24

## Đang làm
Task `m1-world-7l-dieu-khien-pc` ([task contract](tasks/m1-world-7l-dieu-khien-pc.md)).
- **7l c1** (brief 0058) đã duyệt 2026-10-24.
- **7l c2** (brief 0059: cầm vật phẩm từ túi, bỏ Tab tạm, vòng tầm khi chuột phải ngoài tầm, F vùng + gió) **đã duyệt** 2026-10-24 (review đầy đủ + user xem đạt, commit `431bca3`). Giờ thực tế: brief chỉ ghi vòng 3 (~1,2 giờ); các vòng trước thiếu giờ → chưa cộng vào world-tech dòng 7l c. Khoá `bag.empty` không còn dùng → dọn ở 7l d.
- **7l c3** (brief [0060](briefs/0060-7l-c3-mau-mana-client.md), commit `84208af`): nối `PlayerVitals` vào client — tốn mana khi đào / đặt / Mưa (thiếu → không làm + thanh nháy), hồi mana theo ô đi, rơi cao, ngập đầu, hồi sinh ở chỗ Place lần đầu, 2 thanh máu / mana tạm ở trên-trái. Trạng thái `chờ làm`, Sonnet · medium, review đầy đủ + user xem.
  - Số mới `manaFlashMs` 600 `[số tạm]`; đề xuất Planner: rơi xuống nước không mất máu, điểm đầu = chân + 1,5.
- File lạ chưa track: `client/Assets/InitTestScene*.unity(.meta)` — scene tạm của Unity Test Runner; xoá khi Editor không chạy test.

## Bước tiếp
1. Executor làm brief 0060 (`/ai-workflow-kit:exec 0060`), rồi Planner review (`/ai-workflow-kit:review-brief 0060`) + user chơi thử.
2. Xong c3 → cộng giờ 7l c (c1 + c2 + c3) vào world-tech, đánh dấu 7l c xong.
3. **7l d** — túi / túi phụ / kho ô vuông (B, P, kéo thả, Tab đổi đích, 1–0; thay bảng túi cũ + phím 1–4 tạm).
4. **7l e** — HUD gọn (avatar + máu + mana thay thanh tạm của c3, menu phép G, icon ô ruộng / sinh vật, ≥ 30 FPS điện thoại).
5. **7l f** — bảng đổi phím.
6. **sim 7g** theo [0036](../design/decisions/0036-sinh-vat-chi-lam-cong-doan-duoc-giao.md) mục 1–4 (sinh vật chỉ làm công đoạn được giao, nút Tiếp tục) — task riêng sau 7l c.

## Model đề xuất
Sonnet · medium (exec 0060); Opus · medium (review 0060, viết brief 7l d).
