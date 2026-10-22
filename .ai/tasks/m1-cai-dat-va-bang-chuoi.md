# Task: M1 — cài đặt người chơi + bảng chuỗi ngôn ngữ (0032)

> Model thực thi: Sonnet · medium  (AGENTS.md §10.2)
> Trạng thái: user xác nhận câu hỏi 1–2 theo mặc định (2026-10-22). Brief: 0044 (A — lõi). Quyết định gốc: [0032](../../design/decisions/0032-cai-dat-nguoi-choi-va-bang-chuoi.md) (Chốt). Trước: sim 7f (xong 2026-10-22). Sau: world 7i–7k ([0029](../../design/decisions/0029-nhieu-diem-nhin-camera-quan-sat.md)).

## Mục tiêu
1. **Cài đặt người chơi:** file `settings.json` lưu trên máy + bảng **Cài đặt** tối giản trong game; chỉ cảm giác / hiển thị (0032 mục 1).
2. **Luật game vào JSON:** hằng luật còn trong code chuyển sang `design/data/sim_m1.json` (0032 mục 2).
3. **Bảng chuỗi ngôn ngữ:** mọi chữ hiển thị đọc theo khoá; có sẵn file gốc tiếng Việt; người chơi xuất file gốc, dịch, đặt file mới vào thư mục ngôn ngữ, chọn trong Cài đặt; thiếu khoá → chữ gốc (0032 mục 3).

Thông tin chung:
- **Pillar:** không thêm cơ chế chơi. Phục vụ trải nghiệm chơi của mọi pillar trong [00-vision](../../design/00-vision.md) (đọc được chữ, điều khiển dễ chịu). Ghi theo 0032.
- **Thành phần:** client Unity. Không thêm thành phần mới.
  - `Game.Core` (C# thuần, test EditMode): bảng chuỗi (nạp JSON khoá → chữ, thay tham số, thiếu khoá → gốc), dữ liệu cài đặt + kẹp giá trị, luật mới trong `SimConfig`. `SiteStatus` / `CreatureStatus` lấy chữ từ bảng chuỗi.
  - Client: đọc/ghi file trong `Application.persistentDataPath`, bảng Cài đặt, áp cài đặt vào camera/FPS/cỡ chữ, chuyển chữ hiện có sang khoá.
- **Ước lượng:** 4–7 ngày-người (0032), thuộc M1. Cắt trước nếu cần: bảng Cài đặt trong game (giữ file sửa tay).

## Ngoài phạm vi
- Upload / tải bản dịch qua server (0032 → ideas).
- Gán lại phím, cài đặt âm thanh, đồ hoạ (chất lượng, độ phân giải), cảm ứng (task "Điều khiển cảm ứng").
- Font cho chữ ngoài Latin (Trung, Nhật…): M1 chỉ đảm bảo font hiện có hiện đủ dấu tiếng Việt.
- Chữ trong công cụ dev / bench / Editor (`WalkBench`, `TerrainBench`, `HashCheckView`, `SimBenchMeter`, `Client/Editor/*`) và thông báo exception.
- Kích thước thân nhân vật, biên camera bay, số vật lý khác (`PlayerRules` BodyHeight…, `FlyCameraBounds`): không phải luật chơi dev cần chỉnh, giữ trong code.
- Lưu game (túi, kho, world).

## Tiêu chí chấp nhận (kiểm chứng được)
- [ ] Bảng chuỗi: nạp JSON khoá → chữ; tham số `{0}`, `{1}`; thiếu khoá → chữ của file gốc; file hỏng → báo lỗi, dùng gốc, không crash. Kiểm bằng: test EditMode.
- [ ] Không còn chữ hiển thị viết thẳng trong các file client trong phạm vi (trừ ngoài phạm vi ở trên). Kiểm bằng: test EditMode quét mã nguồn (chuỗi có chữ cái tiếng Việt ngoài danh sách cho phép) + chạy SimView với file ngôn ngữ thử (mọi chữ có tiền tố `[T]`) → không còn chữ thiếu tiền tố (Planner/user xem).
- [ ] Xuất file gốc: nút trong Cài đặt ghi `vi.json` vào thư mục ngôn ngữ; file xuất ra nạp lại cho kết quả giống hệt. Kiểm bằng: test PlayMode.
- [ ] Cài đặt: đọc/ghi `settings.json`; thiếu file / thiếu trường → mặc định; giá trị ngoài khoảng → kẹp. Đổi trong bảng → áp ngay + lưu; mở lại game → giữ. Kiểm bằng: test EditMode (dữ liệu) + PlayMode (áp vào camera/FPS/cỡ chữ).
- [ ] Luật: tầm với 8 ô và túi hạt đầu game đọc từ `sim_m1.json` (configVersion 5), không còn hằng trong code; chữ "8 ô" trên HUD lấy từ số cấu hình. Kiểm bằng: test EditMode (ConfigTests) + test cũ qua không đổi kỳ vọng.
- [ ] Không vỡ gì: EditMode + PlayMode toàn bộ qua, 0 lỗi console.
- [ ] Điện thoại: thư mục ngôn ngữ trên Android tìm được và chép file vào được (USB) — Planner thử bằng adb + user xác nhận cách làm.

## Ràng buộc
- `Game.Core` không tham chiếu UnityEngine (bảng chuỗi, dữ liệu cài đặt nằm ở Core).
- **Không dùng Unity Localization package** (user xác nhận 2026-10-22): bản cho 6000.4 là 1.5.12 [docs](https://docs.unity3d.com/6000.4/Documentation/Manual/com.unity.localization.html); muốn nạp file người chơi lúc chạy vẫn phải tự viết `ITableProvider` [docs](https://docs.unity3d.com/Packages/com.unity.localization@1.4/api/UnityEngine.Localization.Settings.ITableProvider.html); CSV chỉ nhập/xuất qua cửa sổ Editor [docs](https://docs.unity3d.com/Packages/com.unity.localization@1.5/manual/CSV.html); UI hiện là IMGUI + chuỗi nội suy, không dùng `LocalizedString`. Tự viết loader JSON nhỏ ít việc hơn.
- Chỗ lưu: `Application.persistentDataPath` — Mac `~/Library/Application Support/unity.<company>.<product>`, Android `.../Android/data/<package>/files` [docs](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/Application-persistentDataPath.html). Android 11+ có thể chặn trình quản lý file mở `Android/data` [chưa xác minh — thử trên máy]. Bảng Cài đặt hiện đường dẫn thư mục ngôn ngữ (không dựa vào `Application.OpenURL` mở thư mục — docs chỉ bảo đảm HTTP/HTTPS [docs](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/Application.OpenURL.html)).
- Cỡ chữ: `GUIStyle.fontSize` chỉ có tác dụng với font dynamic [docs](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/GUIStyle-fontSize.html) → dùng hệ số nhân `GUI.matrix` hiện có (`Screen.height / 720`), gom về một chỗ (đang lặp ở `SimView` và `TerrainEditInput`).
- Từ nay brief client mới: chữ hiển thị mới đi qua bảng chuỗi (0032 Hệ quả).

## Quyết định đã chốt (ai chốt, khi nào)
- Phạm vi 3 phần, luật chỉ dev chỉnh, ngôn ngữ nạp file local, không upload: user, 2026-10-22 (0032).
- Làm ngay sau sim 7f, trước world 7i–7k: user, 2026-10-22 ("duyệt 7f rồi làm 0032").
- Tự viết loader JSON (không dùng Unity Localization) + các mục dưới: đề xuất của Planner, **user xác nhận 2026-10-22**:
  - **Danh sách cài đặt:** khoảng cách camera mặc định (2,5–14, mặc định 6), tốc độ xoay chuột (0,05–0,5, mặc định 0,15), đảo trục dọc (tắt), cỡ chữ UI (×0,75–×2, mặc định ×1 trên mốc 720p), FPS mục tiêu (30 / 60, mặc định 60), ngôn ngữ (danh sách file trong thư mục ngôn ngữ, mặc định `vi`). Bỏ "độ cao / góc nhìn camera" (đã chỉnh bằng chuột).
  - **Mở bảng:** phím `O`; `Esc` khi không có bảng nào mở cũng mở Cài đặt; `Esc` khi bảng Cài đặt mở thì đóng. Nút `Cài đặt (O)` cạnh nút `Túi (B)`.
  - **File:** `settings.json` ở `persistentDataPath`; thư mục ngôn ngữ `persistentDataPath/lang/`. File ngôn ngữ gốc `vi.json` nằm trong `Resources` (đi kèm build); JSON `{ "formatVersion": 1, "language": "Tiếng Việt", "entries": [ { "key": "…", "text": "chữ {0}" } ] }` (mảng thay object vì `JsonUtility` không đọc `Dictionary` [docs](https://docs.unity3d.com/6000.4/Documentation/Manual/json-serialization.html) — Planner chỉnh khi viết brief 0044). Khoá dạng `nhóm.tên` (vd `hud.rain_ready`).
  - **Core sinh chữ:** `SiteStatus` / `CreatureStatus` nhận bảng chuỗi (lớp `Strings` trong Core) thay vì chữ viết thẳng.
  - **Luật vào JSON:** khối `player` thêm `toolReach` 8, `handReach` 8, `starterCrop` 0, `starterSeeds` 4. Khoảng cách / bán kính đặt túi hạt đầu game giữ trong code (vị trí demo, không phải luật).
  - **Tách brief:** (A) Core: `Strings` + `PlayerSettings` + luật vào JSON — Sonnet · medium; (B) client: file cài đặt, bảng Cài đặt, áp cài đặt, xuất file ngôn ngữ — Sonnet · medium; (C) chuyển ~200–260 chữ hiện có sang khoá + test quét — Sonnet · medium; review đầy đủ A/B, C review đầy đủ + user xem.

## Câu hỏi còn mở
_(không — câu hỏi 1–2 user chọn mặc định, 2026-10-22)_
