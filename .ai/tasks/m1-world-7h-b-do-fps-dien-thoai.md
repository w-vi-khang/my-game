# Task: M1 world 7h (b) — đo FPS điện thoại khi nhân vật đi liên tục

> Model thực thi: Sonnet · medium  (AGENTS.md §10.2)
> Trạng thái: user xác nhận 2026-10-22 ("Tiếp tục"). **Xong 2026-10-22** — brief 0039 đã duyệt. Phần (b) của [m1-world-7h-nhan-vat](m1-world-7h-nhan-vat.md), sau brief 0035 (a) và sim 7e (0036–0038). Brief dự kiến: 0039.

## Mục tiêu
Đo trên điện thoại xem SimView có giữ được **≥ 30 FPS** khi nhân vật đi liên tục, và có lộ lỗ chunk trước mặt không. Đây là chỉ tiêu M1 ([roadmap](../../design/05-roadmap.md): ≥ 30 FPS trên một máy Android tầm trung).

- **Pillar:** nền kỹ thuật cho P2 (nhân vật đi trong world, theo 0028). Nếu không đủ FPS thì cảm giác điều khiển hỏng.
- **Thành phần:** client Unity: chế độ đo trong SimView, scene đo, build APK. Không đổi `Game.Core`.

**Hiện trạng [repo]:**
- `PlayerCharacter` có `readKeyboard`, `MoveInput`, `JumpInput` (`client/Assets/Game/Client/PlayerCharacter.cs:18-20`). Có thể cho nhân vật tự đi mà không cần bàn phím.
- `TerrainBench` đã đo `avgFps`, `p50/p99Ms`, `holeFrames` (khung có lỗ trong bán kính sương) và ghi một dòng log `BENCH` (`TerrainBench.cs:120,182-191`). Nhưng nó chỉ dùng camera bay tự động trong scene Bench.
- `SimBenchBuild` chép SimView thành SimBench, bật cờ rồi build APK; có menu và `-executeMethod` (`client/Assets/Game/Client/Editor/SimBenchBuild.cs:12-63`). Có thể làm theo mẫu này.
- Các số đo gần nhất trên điện thoại: 7g có avgFps 35,3 và holeFrames 0; sim bước 7 có 31,9 FPS. Máy đều nóng (thermal 2), xem roadmap.

## Ngoài phạm vi
- **Toàn bộ điều khiển cảm ứng:** joystick ảo, kéo xoay, nút nhảy, chạm để đào/đặt/xới/nhặt, nút Thu. User nói ngày 2026-10-22: "làm trên máy tính trước, các chi tiết các phần điều khiển khác sẽ tính sau". Phần này chuyển sang một task "Điều khiển cảm ứng" riêng, vẫn thuộc hạng mục Điều khiển của M1. Quyết định 0028 không đổi.
- Tối ưu hiệu năng. Nếu đo ra < 30 FPS hoặc có lỗ chunk thì ghi số liệu và báo Planner, không tự sửa trong task này.
- Scene Bench, SimBench, HashCheck: giữ nguyên.

## Tiêu chí chấp nhận (kiểm chứng được)
- [x] **Chế độ đo đi bộ** trong SimView (một cờ, mặc định tắt):
  - Nhân vật tự đi theo một lộ trình cố định, lặp lại được giữa các lần chạy. Lộ trình phải ra khỏi trại, đi qua vùng có chunk phải nạp mới, và có địa hình bậc 1 ô (tự nhảy).
  - Nếu kẹt (không tiến được trong vài giây) thì đổi hướng và đếm số lần kẹt.
  - Camera theo sau lưng như khi chơi.
  - Chạy khởi động 3 giây, rồi đo 40 giây.
  - Kiểm bằng: test PlayMode. Bật cờ và chạy rút ngắn trong Editor: nhân vật đi được quãng đường > 0 ô; có dòng log đủ các trường bên dưới; khi tắt cờ thì SimView chạy như cũ.
- [x] **Dòng log kết quả** (tên dòng do Planner đặt trong brief) gồm:
  - avgFps, p50Ms, p99Ms, maxMs;
  - holeFrames và maxMissing, tính như `TerrainBench`;
  - quãng đường đã đi, số lần kẹt, số chunk đã nạp nhiều nhất;
  - bộ nhớ;
  - pass30fps, passNoHole.
  - Kiểm bằng: test PlayMode, như mục trên.
- [x] **Build APK** cho scene đo, chép từ SimView theo mẫu `SimBenchBuild`, có menu và `-executeMethod`. Kiểm bằng: build thành công qua MCP.
- [x] **Điện thoại Xiaomi 24095PCADG:** avgFps ≥ 30 và holeFrames = 0. Kiểm bằng: AI cài APK qua adb, đọc logcat, ghi số liệu cùng thermal/nhiệt độ máy vào brief và roadmap.
- [x] **Test cũ đạt**, `Game.Core` không tham chiếu UnityEngine. Kiểm bằng: `run_tests` MCP (EditMode + PlayMode).
- [x] **User xem:** chỉ đọc số liệu đo, không cần thử điều khiển trên điện thoại (cảm ứng chưa làm).

## Ràng buộc
- Unity 6000.4.8f1, URP 17.4.0, Input System 1.19.0. API dùng trong brief phải tra docs 6000.4.
- Unity Editor dùng chung qua MCP (0010).
  - Sau PlayMode: `git checkout -- client/ProjectSettings/EditorSettings.asset`.
  - Sau build APK: `git checkout -- client/ProjectSettings/UnityConnectSettings.asset`.
- Điện thoại: dò cổng bằng `adb mdns services`. Không dùng 192.168.1.7:6060 (đó là TV).
- **Ước lượng 0,5–1 ngày-người** [ước lượng của AI]. Đây là dòng "Đo điện thoại + user xem" trong bảng của task 7h. Phần "Cảm ứng cơ bản" (0,5–1) chuyển sang task Điều khiển cảm ứng. Ghi giờ thực tế.

## Quyết định đã chốt (ai chốt, khi nào)
- **User 2026-10-22:**
  - đo bằng lộ trình tự đi cố định, AI tự cài APK và đọc logcat;
  - cảm ứng để sau ("làm trên máy tính trước, các chi tiết các phần điều khiển khác sẽ tính sau").

## Câu hỏi còn mở
- (không)
