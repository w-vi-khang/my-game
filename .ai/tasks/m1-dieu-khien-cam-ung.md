# Task: M1 điều khiển cảm ứng cơ bản — joystick + tâm nhắm

> Model thực thi: Sonnet · medium  (AGENTS.md §10.2 — Executor làm theo brief; Planner Opus viết brief từ contract này)
> Trạng thái: **user xác nhận 2026-11-02** (cả 5 đề xuất của Planner). Dòng 7m trong [world-tech](../../design/04-tech/world-tech.md#kế-hoạch-prototype-m1). Brief dự kiến: 0101 (a), 0102 (b), 0103 (c). User chọn 2026-11-02: kiểu "joystick + tâm nhắm", phạm vi "đủ vòng lặp 30 giây". Thuộc hạng mục "Điều khiển (PC trước, cảm ứng cơ bản) 3–5" của M1 ([roadmap](../../design/05-roadmap.md)). Phần cảm ứng đã chuyển ra khỏi [7h](m1-world-7h-b-do-fps-dien-thoai.md) (user 2026-10-22: "làm trên máy tính trước").

## Mục tiêu
Chơi được vòng lặp 30 giây của M1 trên điện thoại Android bằng cảm ứng: đi, xoay camera, đào/đặt, xới, tưới, thu hoạch, nhặt, mở túi/kho, cầm vật.

Kiểu điều khiển theo [controls-ui](../../design/02-gameplay/controls-ui.md#điều-khiển) (bảng cột "Cảm ứng" + nguyên tắc "Nhắm theo tâm màn hình"):
- **Joystick ảo nửa trái:** joystick hiện tại chỗ ngón cái chạm xuống. Đi theo hướng camera.
- **Vuốt nửa phải:** xoay camera. **Chụm 2 ngón:** phóng to/thu nhỏ.
- **Tâm nhắm giữa màn hình:**
  - Ô hoặc vật nằm dưới tâm nhắm là đích.
  - Đích được tô bằng lớp đỏ nhạt như PC. Khi cầm khối thì hiện bản mờ.
  - Đích ngoài tầm thao tác 10 ô thì hiện vòng tầm (0034).
- **Nút hành động chính:** làm đúng việc chuột phải trên PC làm với đích dưới tâm nhắm (0034 mục 2): hành động của chế độ, dùng vật đang cầm, hoặc hành động riêng của chỗ đó. Nhân vật tự đi tới cách đích 2 ô rồi mới làm.
- **Nút:**
  - Nhảy.
  - Thanh chế độ Mặc định / Đào / Xới / Tưới.
  - Thu.
  - Túi.
  - Huỷ/Menu: giống Esc — đóng bảng đang mở, hoặc thoát chế độ/vật cầm. Không còn gì để đóng thì mở Cài đặt.
  - Chạm ô túi phụ để cầm vật, giống phím 1–0.
- **Giữ như cũ:** ô minimap và nút Phép chạm được; trong các bảng túi/kho, chạm và kéo bằng ngón tay giống click và kéo chuột.

- **Pillar:** không có pillar riêng. Task phục vụ ràng buộc nền tảng PC + mobile trong [vision](../../design/00-vision.md). Nhờ vậy vòng lặp P1/P2 chơi được trên điện thoại, và có thể thử chơi M1 trên mobile.
- **Thành phần:** chỉ client Unity: `Game.Client`, `GameControls.inputactions`. Số chỉnh được nằm trong `design/data/sim_m1.json` (chép sang `Resources/Config`). Không cần `Game.Core`, trừ khi có phép tính thuần cần test EditMode (ví dụ joystick vector → hướng đi); khi đó viết trong Core, không tham chiếu UnityEngine.

**Hiện trạng [repo]:**
- `PlayerCharacter` có `MoveInput` / `JumpInput` gán được từ code. Hướng đi theo yaw camera: `client/Assets/Game/Client/PlayerCharacter.cs:23-24,429`. Có `readKeyboard` (`:22`).
- `FollowCamera.ApplyLook(Vector2)` / `ApplyZoom(float)` gọi được từ ngoài (`client/Assets/Game/Client/FollowCamera.cs:73,81`).
- Nhánh chuột phải: `TerrainEditInput.RightClickAt(hasCell, digCell, placeCell, drop, storageId)` (`client/Assets/Game/Client/TerrainEditInput.cs:256`). Hiện đích lấy từ tia qua vị trí con trỏ (`:115-141`).
- Các bảng (`InventoryView`, `MinimapView`, `TimeDial`) dựng bằng code, không dùng EventSystem. Chúng tự kiểm con trỏ qua `GameInput` (`InventoryView.cs:11`, `MinimapView.cs:15`, `TimeDial.cs:11`). Chỉ `TimeDial` đọc `Touchscreen` (`TimeDial.cs:160`).
- Các action map: `Player`, `Pointer`, `Panels`, `Map`, `TimeDial`, `Settings` (`client/Assets/Game/Client/Input/GameControls.inputactions`). Chưa có control scheme cảm ứng.

## Ngoài phạm vi
Các mục dưới đây ghi vào [ideas](../../design/ideas.md) hoặc controls-ui (dòng M2/M3), không làm trong task này:
- Nút cảm ứng cho: lướt (Shift), góc nhìn thứ nhất (X), camera về mặc định (Space), xem/đặt camera quan sát (V), ẩn/hiện túi phụ (P), bảng Môi trường sống (H), chia đồ 1/2 – 1/3 (Tab + 1/2/3).
- Đổi phím / đổi vị trí, cỡ nút cảm ứng (M3, controls-ui).
- Tay cầm (sau phát hành).
- Khoá mục tiêu, nút ngữ cảnh khi nhập hồn, nhắm phép (M2).
- Chọn vùng 2 điểm (chưa có ở PC 0034).
- Đổi điều khiển PC: PC giữ nguyên 0034, chỉ thêm cảm ứng.
- Đo và đạt ngưỡng FPS trên điện thoại: user gác ngày 2026-11-01. Chỉ ghi số FPS nếu đo được, không sửa hiệu năng.
- iOS: chỉ thử Android (máy 24095PCADG).

## Tiêu chí chấp nhận (kiểm chứng được)
- [ ] **Bật chế độ cảm ứng:** giao diện cảm ứng hiện khi chạy trên điện thoại. Trong Editor bật bằng một cờ debug.
  - Khi chế độ cảm ứng tắt, PC chạy như cũ.
  - Kiểm bằng: test PlayMode (bật/tắt cờ). Bộ EditMode + PlayMode hiện có vẫn đạt.
- [ ] **Joystick:**
  - Chạm nửa trái thì joystick hiện tại chỗ chạm. Kéo thì nhân vật đi theo hướng camera; thả thì dừng.
  - Joystick huỷ đi-theo-click và tự-đi đang chạy, như WASD trước đây.
  - Kiểm bằng: test PlayMode, giả lập chạm qua Input System test fixture hoặc gọi hàm nhận vector. Nhân vật đi > 0 ô đúng hướng; thả thì dừng.
- [ ] **Camera:**
  - Vuốt nửa phải xoay yaw/pitch. Chụm 2 ngón thì phóng/thu.
  - Vuốt hoặc chụm bắt đầu trên nút/bảng thì không xoay camera.
  - Kiểm bằng: test PlayMode (yaw đổi khi vuốt nửa phải; không đổi khi chạm nút).
- [ ] **Tâm nhắm + nút hành động chính:**
  - Đích = ô/vật dưới tâm màn hình. Hiển thị (lớp đỏ, bản mờ, vòng tầm) giống PC.
  - Bấm nút hành động chính cho kết quả giống `RightClickAt` với cùng đích.
  - Kiểm bằng: test PlayMode, ở chế độ Đào, trên một ô biết trước:
    - đích trong 2 ô → đào ngay;
    - đích trong 10 ô → tự đi tới rồi đào;
    - đích ngoài 10 ô → không làm.
- [ ] **Nút:** Nhảy, 4 chế độ, Thu, Túi, Huỷ/Menu, ô túi phụ.
  - Mỗi nút gọi đúng hành động như phím PC tương ứng.
  - Cỡ ≥ 48 dp theo `Screen.dpi`. Nút nằm trong `Screen.safeArea`.
  - Kiểm bằng: test PlayMode, mỗi nút một kiểm; kiểm cỡ và vị trí theo dpi/safeArea giả lập.
- [ ] **Bảng chạm được:** túi/kho (chạm ô, kéo thả chồng), bảng ô ruộng (chọn giống, hàng đợi), bản đồ (mở, kéo, chụm), menu phép.
  - Kiểm bằng: test PlayMode với chạm giả lập cho túi (kéo thả) và minimap (chạm mở). Phần còn lại kiểm trên điện thoại.
- [ ] **Chạy trên điện thoại:**
  - AI tự build APK, cài qua USB, đọc logcat (memory: điện thoại luôn cắm USB).
  - SimView mở được, giao diện cảm ứng hiện, logcat không có exception.
  - Có ảnh `adb screencap` ở hai khung: lúc chưa mở bảng và lúc đang mở túi.
- [ ] **User xem trên điện thoại** (mức `user xem`; gộp chung đợt chơi thử 5j/5k nếu tiện):
  - Cảm giác joystick và tốc độ xoay camera.
  - Tâm nhắm có dễ chọn ô không.
  - Đủ một vòng: đào → đặt → xới → gieo → tưới (phép Mưa) → thu hoạch → nhặt → bỏ vào kho.
  - Nút có che tầm nhìn không.
- [ ] **Tài liệu:**
  - controls-ui: điền cột "Cảm ứng" cho các dòng M1 đã làm; dòng chưa làm ghi "ngoài M1".
  - world-tech hoặc roadmap: thêm dòng bước, ghi giờ thực tế.
  - glossary: "Tâm nhắm" và "Hành động chính" đã có (`design/glossary.md:107,109`). Sửa dòng "Hành động chính" vì vẫn ghi "chuột trái", trong khi PC theo 0034 là chuột phải.

## Ràng buộc
- Đọc input qua lớp hành động `GameInput` / `GameControls.inputactions` ([0033](../../design/decisions/0033-tach-lop-hanh-dong-dieu-khien.md)). Không đọc thẳng `Touchscreen` ở chỗ mới, trừ khi brief ghi rõ lý do.
- Tái dùng nhánh `RightClickAt`, `ToggleMode`, `CollectNearby`, `ApplyLook`, `ApplyZoom`; không viết lại luật.
- Mọi số chưa chốt (cỡ nút, bán kính joystick, độ nhạy vuốt/chụm, vùng chết joystick) đặt trong `sim_m1.json`, ghi `[số tạm]` (AGENTS.local "Tham số chưa chốt").
- Giao diện dựng bằng code như các bảng hiện có (uGUI, không EventSystem). Chữ hiển thị nằm trong `Lang/vi.json` (0032).
- API Unity/Input System dùng trong brief: Planner tra docs đúng bản 6000.4 / Input System 1.19 và ghi link `[docs]`. Gồm: chạm nhiều ngón (EnhancedTouch hoặc binding `<Touchscreen>`), `Screen.dpi`, `Screen.safeArea`, giả lập chạm trong test.
- Chạy Unity qua MCP: mỗi lúc chỉ một phiên điều khiển Editor. Sau `run_tests` PlayMode chạy `git checkout -- client/ProjectSettings/EditorSettings.asset`. Sau build APK chạy `git checkout -- client/ProjectSettings/UnityConnectSettings.asset`.

## Quyết định đã chốt (ai chốt, khi nào)
- Kiểu cảm ứng **joystick + tâm nhắm + nút hành động chính**, không dùng chạm-để-đi (user, 2026-11-02).
- Phạm vi **đủ vòng lặp 30 giây**: Nhảy, 4 chế độ, Thu, Túi, Huỷ/Menu, ô túi phụ. Các nút còn lại để sau (user, 2026-11-02).
- **Ước lượng 3–4,5 ngày-người** [ý tưởng, ước lượng của AI]. Hạng mục "Điều khiển 3–5" đã dùng 0,5–1 cho tách đào/đặt, còn 2–4,5. Đầu trên có thể vượt tối đa 0,5 → báo user khi ghi roadmap.
- **Đề xuất của Planner** [ý tưởng, chưa hỏi user — user xác nhận cùng contract, hoặc chỉnh khi chơi thử]:
  1. **Khi nào bật giao diện cảm ứng:** `Application.isMobilePlatform`, hoặc cờ debug trong Editor. Không tự chuyển theo thiết bị dùng gần nhất: cách đó phức tạp hơn và M1 chưa cần.
  2. **Camera cảm ứng:** góc mặc định theo sau lưng, pitch khoảng 20° [số tạm], để tâm màn hình rơi trên mặt đất trước nhân vật. Không dùng góc trên cao kiểu LoL của PC.
  3. **Tia tâm nhắm bỏ qua collider của nhân vật** để không nhắm trúng chính mình.
  4. **Nút hành động chính khi đích là ô ruộng ở chế độ Mặc định:** chín thì thu hoạch, chưa chín thì mở bảng ô ruộng. Trên PC việc này chia cho chuột trái/phải; cảm ứng chỉ có một nút.
  5. **Bố cục:**
     - Joystick: nửa trái.
     - Nút hành động chính (to nhất) + Nhảy: góc dưới-phải.
     - Thu: cạnh nút chính.
     - Thanh 4 chế độ: dọc mép phải, trên nút chính.
     - Túi phụ: giữa-dưới như PC.
     - Túi + Huỷ/Menu: góc trên-trái, cạnh avatar.
     - Minimap + nút Phép: giữ chỗ cũ.
  6. **Chia brief** (dự kiến 3):
     - (a) Chế độ cảm ứng + joystick + vuốt/chụm camera + Nhảy.
     - (b) Tâm nhắm + nút hành động chính + thanh chế độ + Thu/Túi/Huỷ/túi phụ + chạm trong bảng.
     - (c) Build APK, chạy trên điện thoại, ảnh + logcat, tài liệu.

## Câu hỏi còn mở
- Không có. User xác nhận đề xuất 1–5 ngày 2026-11-02; số tạm chỉnh khi chơi thử.

## Kiểm chứng
Đọc lại contract và đối chiếu checklist (2026-11-02, Planner). Chưa có code nên chưa chạy lệnh verify.
- Pillar: đạt. Mục "Mục tiêu" ghi ràng buộc nền tảng PC + mobile của vision; vòng lặp P1/P2 chạy được trên điện thoại.
- Thành phần: đạt. Chỉ client Unity, không thêm thành phần mới.
- Thuật ngữ: đạt. "Tâm nhắm" và "Hành động chính" đã có trong glossary (`:107,109`); dòng "Hành động chính" lỗi thời đã đưa vào tiêu chí "Tài liệu".
- Rà phần liên quan: đạt. Không đổi economy/progression; ghi roadmap khi xong.
- `[ý tưởng]`: đạt. Ước lượng và 5 đề xuất của Planner đều đánh dấu.
- Nguồn: chưa áp dụng. Docs của Input System, `Screen.dpi` và `safeArea` sẽ tra khi viết brief (đã ghi ở mục Ràng buộc).
- Ước lượng + milestone: đạt. 3–4,5 ngày-người, thuộc M1; đầu trên có thể vượt hạng mục tối đa 0,5, đã ghi.
- Game.Core: chưa áp dụng (chưa có code).
- Test theo cột "Xong khi": đạt. Mỗi tiêu chí có cách kiểm; khi ghi dòng bước vào world-tech sẽ thêm cột "Xong khi".
- Chỉ M1: đạt. Phần ngoài M1 nằm trong "Ngoài phạm vi", sẽ ghi vào ideas.
- Giờ thực tế: chưa áp dụng (hạng mục chưa xong).
