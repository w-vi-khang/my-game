# Handoff — 2026-10-24

## Điều phối (phiên Opus điều phối, user AFK)
- User 2026-10-24: "Hãy hoàn thành mục 7 một lượt" → 7l d, e, f + sim 7g, "user xem" gộp cuối; rồi "làm tiếp các phần khác, bỏ qua các phần cần user, nếu không thể dùng lại phiên hãy mở phiên mới, không cần hỏi".
- Vòng: Planner (Opus · medium) viết brief → Executor (Sonnet · medium) `/ai-workflow-kit:exec NNNN --child` → Planner `/ai-workflow-kit:review-brief NNNN` (kết luận "code ok, chờ user xem gộp cuối"). Unity Editor dùng chung → tuần tự.
- Phiên hiện dùng (từ 2026-10-24 chiều): phiên điều phối mới tự làm Planner (phiên Planner/Executor cũ không nhận tin được); Executor [exec-0070](#local_90d07a05-1772-4d2c-a99e-6da3dd1fe9fa). 0070 vòng 1 bị chặn → Planner trả lời QĐ 7–11 (commit `cb506a8`: màn hình Mac tắt khi AFK → `caffeinate`; B crash → build lại riêng B một lần). Phiên mới chỉ mở được trong lượt user gõ (lượt do sự kiện phiên khác bị từ chối start_session).
- Executor hay kết thúc lượt khi PlayMode đang chạy (MCP rớt khi vào Play) → nhắc "sleep 60 rồi get_test_job, chỉ dừng khi chờ review / cần Opus".
- Danh sách chơi thử gộp bước 7: [playtest-buoc-7.md](playtest-buoc-7.md).
- Đã duyệt: 0068 (ổn định test), 0069 (sim 8a lõi), **0070** (world 7k: Mac đo xong — không thấy lợi, GPU ~10 ms cả A/B/C; 3 APK `walkbench-gpu-{a,b,c}.apk` sẵn). **Hết hạng mục M1 làm được mà không cần user** → đang dừng, chờ user (xem "Bước tiếp" mục 5).

## Đang làm
Task `m1-world-7l-dieu-khien-pc` ([task contract](tasks/m1-world-7l-dieu-khien-pc.md)).
- **7l c1** (brief 0058) đã duyệt 2026-10-24.
- **7l c2** (brief 0059: cầm vật phẩm từ túi, bỏ Tab tạm, vòng tầm khi chuột phải ngoài tầm, F vùng + gió) **đã duyệt** 2026-10-24 (review đầy đủ + user xem đạt, commit `431bca3`). Giờ thực tế: brief chỉ ghi vòng 3 (~1,2 giờ); các vòng trước thiếu giờ (đã ghi "≥ 73 phút" ở world-tech + roadmap). Khoá `bag.empty` không còn dùng → dọn ở 7l d.
- **7l c3** (brief [0060](briefs/0060-7l-c3-mau-mana-client.md): máu / mana nối vào client) **đã duyệt** 2026-10-24 (review đầy đủ + user xem đạt). **7l c xong**; giờ thực tế ghi ở world-tech dòng 7l c + roadmap.
  - Số tạm chưa chốt: `manaFlashMs` 600; đề xuất Planner: rơi xuống nước không mất máu, điểm đầu = chân + 1,5.
- File lạ chưa track: `client/Assets/InitTestScene*.unity(.meta)` — scene tạm của Unity Test Runner; xoá khi Editor không chạy test.

- **7l d** chia 3 brief (task contract mục Quyết định; user 2026-10-24: làm hết mục 7, "user xem" gộp một lần cuối task): **d1** [0061](briefs/0061-7l-d1-tui-o-vuong-loi.md) **đã duyệt** 2026-10-24 (code ok, chờ user xem gộp cuối; Executor 16 phút; giờ ghi ở world-tech dòng 7l d — roadmap chưa cộng vì `design/05-roadmap.md` đang có thay đổi chưa commit của phiên khác). **d2** [0062](briefs/0062-7l-d2-giao-dien-tui-tui-phu-kho.md) **đã duyệt** 2026-10-24 (Executor 19 phút; ghi chú cho 7l e: viết lại chữ hướng dẫn `hud.help*` theo phím mới). **d3** [0063](briefs/0063-7l-d3-chuyen-do-keo-tha-tab-123.md) **đã duyệt** 2026-10-24 (Executor 18 phút). **7l d xong phần code**, tổng 53 phút (world-tech + roadmap bảng giờ đã ghi).
- **7l e** chia 2 brief: **e1** [0064](briefs/0064-7l-e1-hud-gon-huong-dan-menu-phep.md) **đã duyệt** 2026-10-24 (code ok, chờ user xem gộp cuối; Executor 21 phút; world-tech + roadmap đã ghi). **e2** [0065](briefs/0065-7l-e2-icon-o-ruong-sinh-vat-fps.md) **đã duyệt phần code** 2026-10-24 (Executor ~17 phút). **7l e xong phần code** (~38 phút). **Còn treo: đo ≥ 30 FPS điện thoại với 20 ô ruộng có icon** — adb không thấy máy lúc 07:4x (user AFK). Khi user cắm máy: làm bước 9.4 brief 0065 với `client/Builds/walkbench.apk` + `walkbench-plots.apk` (đã build từ `74ec39c`, không cần build lại), ghi nối vào Kết quả 0065. Ghi chú review 0064: `SeaSkyTintPlayTests.DayCycleOffNearSeaUnchanged` lỗi khi chạy cả bộ PlayMode, chạy riêng qua (nghi phụ thuộc thứ tự — theo dõi); Esc khi menu phép mở có thể vừa đóng menu vừa mở Cài đặt cùng khung (như vòng buổi trước đây) — kiểm ở lần chơi thử cuối.
- **7l f** [0066](briefs/0066-7l-f-bang-doi-phim.md) **đã duyệt** 2026-10-24 (code ok, chờ user xem gộp cuối; Executor ~23 phút; world-tech + roadmap đã ghi). **Task 7l xong phần code** (a–f). Ngoài phạm vi 0066, để sau: nhãn nút có phím trong ngoặc (`Túi (B)`, `Cài đặt (O)`, `Phép (G)`, `Đóng (M)`, `Xem camera (V)`, `Quay về (V)`) và chữ `guide.*` vẫn ghi phím mặc định (có dòng chú thích `guide.keys_note`); `loc/en.json` + `loc/context.json` chưa có 43 key mới.
- **Sim 7g** [0067](briefs/0067-sim-7g-sinh-vat-chi-lam-viec-duoc-giao.md) (task [m1-sim-7g](tasks/m1-sim-7g-sinh-vat-chi-lam-viec-duoc-giao.md)) **đã duyệt** 2026-10-24 (code ok, chờ user xem gộp cuối; Executor ~51 phút; simulation-tech + roadmap đã ghi). **Mục 7 (7l d–f + sim 7g) xong phần code.**
- **Việc treo — test chỉ hỏng khi chạy cả bộ** (chạy riêng qua; không do 0066): `IncrementalRebuildMatchesFullRebuild` (timeout khi Editor không focus, từ 0061), `SeaSkyTintPlayTests.DayCycleOffNearSeaUnchanged` (từ 0064), `WorldIconsPlayTests.CreatureIconMatchesState` (lần đầu ở 0066; nghi `SimView` chạy thêm nhịp giữa lúc icon cập nhật và lúc test đọc `State`). Cần một brief nhỏ ổn định test (sau sim 7g).
- Danh sách chơi thử gộp cuối: lấy từ mục "User tự test" + Review của từng brief 0061–0063 (và 7l e, f, sim 7g).

## Bước tiếp
1. ~~7l a–f, sim 7g~~ (xong phần code). Còn treo: đo FPS điện thoại 7l e (khi user cắm máy — xem trên).
2. **Bước tiếp: user chơi thử gộp cuối** — danh sách lấy từ mục "User tự test" + Review của brief 0061–0067 (0066: tab Điều khiển, đổi chuột trái ↔ phải, trùng → Đổi chỗ / Huỷ, Mặc định, Esc / F1 không nhận; 0067: biển xới rồi đứng yên, tự gieo → không ai tưới, Tiếp tục tưới / thu / xới lại, hàng đợi làm đủ chuỗi, chen vào thì sinh vật làm tiếp). Góp ý của user → brief sửa.
3. Brief nhỏ ổn định 3 test chỉ hỏng khi chạy cả bộ → **brief [0068](briefs/0068-on-dinh-3-test-chap-chon.md) đã duyệt** 2026-10-24: Executor ~25 phút; cả bộ 582/582 + 267/267 ×2. Nguyên nhân test biển: Light thừa do `ControlModePlayTests` để lại.
4. **Hạng mục M1 còn lại** (rà 2026-10-24, Planner):
   - sim 6: đã xong từ brief 0006, chỉ thiếu ghi ở bảng → đã ghi bù ở simulation-tech.
   - **sim 8 môi trường sống** (3–6 ngày; task contract [m1-sim-8](tasks/m1-sim-8-moi-truong-song.md), 3 câu hỏi mở cho user; **8a = brief [0069](briefs/0069-sim-8a-moi-truong-song-loi.md) đã duyệt 2026-10-24** (Executor ~14 phút; 596 + 267 test)): chia **8a lõi** (`Game.Core`: dữ liệu yêu cầu, đếm theo sự kiện, sự kiện "loài trở lại"; vùng đếm + loài mở sẵn là tham số JSON [số tạm]) và **8b client**. Làm được 8a từ phần đã duyệt (simulation-tech "Môi trường sống", mechanics "Sinh vật trở lại — luật M1", 0020/0025: ô ruộng không tính cỏ, 0024: điều kiện "buổi"). **8b cần user quyết trước:**
     - (a) vùng đếm M1: cả Map 1 (theo text 2026-10-13, nhưng Map 1 có sẵn rất nhiều cỏ → L1 đạt ngay) hay vùng mô phỏng 48 × 48;
     - (b) demo bắt đầu với loài nào đã mở (hiện L1–L3 có sẵn);
     - (c) chỗ hiện danh sách yêu cầu trên PC, vì 0034 bỏ "loài ghim" khỏi HUD: bảng riêng mở bằng phím/nút?
   - **world 7k** thử GPU occlusion culling: brief [0070](briefs/0070-world-7k-thu-gpu-occlusion-culling.md) **đã duyệt** (Mac: không lợi; build sẵn `client/Builds/walkbench-gpu-{a,b,c}.apk`); đo điện thoại chờ máy (adb trống lúc 09:3x, 11:00, 13:42).
5. **Chờ user** (sáng 2026-10-25), theo thứ tự đề xuất:
   1. **Chơi thử gộp bước 7**: [playtest-buoc-7.md](playtest-buoc-7.md) (7l d–f, sim 7g). Góp ý → brief sửa.
   2. **Trả lời 3 câu hỏi sim 8b** ([task contract m1-sim-8](tasks/m1-sim-8-moi-truong-song.md) mục Câu hỏi còn mở: vùng đếm, loài mở sẵn, chỗ hiện danh sách yêu cầu; số đo world M1: 48.077 cỏ, 0 cỏ cạnh nước, 0 đá lộ thiên) → Planner viết brief 8b.
   3. **Cắm điện thoại (USB debug)** → Executor tự đo (không cần user thao tác thêm): (i) 7l e: bước 9.4 brief 0065 với `walkbench.apk` + `walkbench-plots.apk`; (ii) 7k: 3 APK `walkbench-gpu-{a,b,c}.apk` (brief 0070) → Planner kết luận bật/tắt GPU occlusion.
   - Sau đó M1 còn: sim 8b (1,5–3,5 ngày) rồi rà tiêu chí xong M1 (roadmap).

## Model đề xuất
Opus · medium (tổng hợp danh sách chơi thử, viết brief sửa theo góp ý).
