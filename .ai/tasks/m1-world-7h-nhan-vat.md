# Task: M1 world bước 7h — nhân vật người chơi đơn giản

> Model thực thi: Sonnet · medium  (AGENTS.md §10.2)
> Trạng thái: user xác nhận 2026-10-22. Brief: 0035 (a). Phần (b) → task [m1-world-7h-b-do-fps-dien-thoai](m1-world-7h-b-do-fps-dien-thoai.md) (chỉ đo FPS); cảm ứng chuyển sang task Điều khiển cảm ứng sau (user 2026-10-22: "làm trên máy tính trước, các chi tiết các phần điều khiển khác sẽ tính sau"). Bước đầu của [0028](../../design/decisions/0028-nhan-vat-tui-vat-roi-tu-trong-trot.md). Sau bước này: sim 7e (vật rơi + túi), 7f (tự trồng trọt + phép Mưa), world 7i–7k ([0029](../../design/decisions/0029-nhieu-diem-nhin-camera-quan-sat.md)), rồi sim bước 8 (môi trường sống, tạm hoãn).

## Mục tiêu
Thay camera bay bằng một **nhân vật đơn giản** ở các scene chơi. Người chơi đi lại, nhảy, nhìn bằng góc nhìn thứ ba. Mọi tương tác với ô (đào, đặt, xới) chỉ làm được trong **tầm với** quanh nhân vật. Camera bay chỉ còn là chế độ debug.

- **Pillar:** P2 (người chơi có vị trí trong world để tự làm việc nhỏ cạnh sinh vật — 0028). Đây cũng là nền cho P1: vật rơi, phép Mưa.
- **Thành phần:** client Unity.
  - Luật thuần trong `Game.Core`, có test EditMode: tầm với, ô bị nhân vật chiếm.
  - Ở client: điều khiển nhân vật, camera theo, chế độ debug.

**Hiện trạng [repo]:**
- `DebugFlyCamera` dùng WASD/Q/E để bay và giữ chuột giữa để xoay. Nó có va chạm khối (`terrain`) và biên world (`FlyCameraBounds`).
- `TerrainEditInput` nhắm ô theo vị trí con trỏ chuột (`TerrainEditInput.cs:89`): chuột trái dùng công cụ, Tab đổi công cụ, phím 1–4 chọn khối. Nó bỏ qua thao tác khi đang giữ phím bay (`:105`).
- `TerrainChunkView` nạp chunk quanh `streamTarget`, rỗng thì lấy `Camera.main` (`:106`).
- Minimap lấy vị trí từ `Camera.main` (`MinimapView.cs:340,355`).
- Các phím đang dùng: F1, F2, K, Esc (SimView); M (bản đồ); T (chọn mốc); Tab và 1–4 (công cụ).

## Ngoài phạm vi
- Vật rơi, túi đồ, nhặt, Thu, túi hạt → sim 7e.
- Tự xới/gieo/thu hoạch, phép Mưa, kho ↔ túi → sim 7f.
- Nhiều điểm nhìn, camera quan sát, GPU culling → world 7i–7k.
- Animation, model art, bơi, chạy nhanh (Shift — M2 theo bảng điều khiển).
- Thao tác công cụ bằng cảm ứng (chạm để đào/đặt/xới). Phần này nằm trong hạng mục "Điều khiển" M1, làm sau.
- Va chạm giữa nhân vật và sinh vật: hai bên đi xuyên nhau. Sinh vật không né nhân vật.
- Scene Bench, SimBench, HashCheck: giữ camera bay, vì đây là scene đo.

## Tiêu chí chấp nhận (kiểm chứng được)
- [ ] **Hình:** nhân vật gồm vài khối (thân + đầu), cao 2 ô, rộng 1 ô. Khi đi thì nhún nhẹ, không có animation — kiểm bằng: user xem.
- [ ] **Điều khiển PC:**
  - WASD đi theo hướng camera; giữ chuột giữa + kéo để xoay camera quanh nhân vật (giống cách xoay hiện nay); lăn chuột để thu phóng khoảng cách camera.
  - Space để nhảy, nhảy lên được bậc cao 1 ô, không lên được bậc 2 ô.
  - Rơi khi hết đất dưới chân.
  - Kiểm bằng: test PlayMode (đi, nhảy lên bậc 1 ô, không lên bậc 2 ô, rơi) qua `run_tests` MCP.
- [ ] **Va chạm:**
  - Nhân vật không xuyên khối khi đi hay nhảy (dùng collider chunk đã có).
  - Không ra khỏi biên world.
  - Đào ô dưới chân thì rơi xuống.
  - Kiểm bằng: test PlayMode.
- [ ] **Camera theo sau lưng** không xuyên khối: có khối chắn giữa nhân vật và camera thì camera kéo lại gần — kiểm bằng: test PlayMode.
- [ ] **Tầm với 4 ô** (khoảng cách Euclid từ tâm thân nhân vật tới tâm ô ≤ 4):
  - Đào, đặt, xới (đánh dấu) chỉ làm được trong tầm.
  - Ngoài tầm: không tô viền hay bản mờ, click không làm gì.
  - Lõi `Game.Core` có hàm thuần cho tầm với.
  - Kiểm bằng: test EditMode (biên đúng 4 / 4,1) + test PlayMode (click ô ngoài tầm không đổi world).
- [ ] **Không đặt khối vào ô nhân vật đang chiếm** (luật M1 "không đặt vào ô đang có người chơi", hai ô thân) — kiểm bằng: test EditMode + PlayMode.
- [ ] **Nước:** nhân vật đi vào nước được, tốc độ ×0,5, không bơi. Camera dưới nước vẫn bật hiệu ứng dưới nước như cũ — kiểm bằng: test PlayMode.
- [ ] **Theo nhân vật:** nạp chunk lấy tâm là nhân vật (`streamTarget`); minimap vẽ vị trí là nhân vật và hướng là hướng camera — kiểm bằng: test PlayMode.
- [ ] **Chế độ debug:** một phím chưa dùng (Planner chọn trong brief) bật/tắt camera bay. Khi bay, nhân vật đứng yên, tầm với tính theo nhân vật — kiểm bằng: test PlayMode.
- [ ] **Scene SimView và TerrainView** có nhân vật:
  - SimView: xuất hiện ở trại demo.
  - TerrainView: xuất hiện ở checkpoint Map 1.
  - Không rơi xuyên đất lúc chunk chưa nạp (chờ chunk dưới chân có collider rồi mới bật trọng lực).
  - Kiểm bằng: test PlayMode.
- [ ] ~~**Cảm ứng cơ bản:**~~ → task Điều khiển cảm ứng (user 2026-10-22). joystick ảo ở nửa trái màn hình để đi, kéo nửa phải để xoay, nút nhảy — kiểm bằng: user thử trên điện thoại (AI tự cài APK, đọc logcat).
- [ ] **Hiệu năng** (→ task 7h b, nhân vật tự đi theo lộ trình cố định): điện thoại ≥ 30 FPS khi đi liên tục trong SimView, không thấy lỗ chunk trước mặt — kiểm bằng: đo trên điện thoại (Bench/log FPS).
- [ ] **Test cũ đạt:** test cũ (EditMode + PlayMode) đạt, các test dùng camera bay chỉnh tối thiểu; `Game.Core` không tham chiếu UnityEngine — kiểm bằng: `run_tests` MCP.
- [ ] **User xem trên Mac + điện thoại:** cảm giác đi, nhảy, xoay, tầm với — kiểm bằng: user xem (mức review `user xem`).

## Ràng buộc
- Unity 6000.4.8f1, URP 17.4.0, Input System 1.19.0.
  - Dùng `CharacterController` có sẵn của Unity, không tự viết vật lý.
  - API trong brief phải tra docs 6000.4.
- Unity Editor dùng chung qua MCP (0010). Sau PlayMode: `git checkout -- client/ProjectSettings/EditorSettings.asset`. Sau build APK: `git checkout -- client/ProjectSettings/UnityConnectSettings.asset`.
- **Ước lượng 3–5 ngày-người** [ước lượng của AI] — M1, dòng "Nhân vật người chơi…" của [roadmap](../../design/05-roadmap.md) (12–20 của 0028).

  | Phần | Ngày-người |
  |---|---|
  | Lõi tầm với + ô chiếm + test | 0,5 |
  | Nhân vật + điều khiển PC + camera theo | 1–1,5 |
  | Nối SimView / TerrainView / chunk / minimap / chế độ debug | 0,5–1 |
  | Cảm ứng cơ bản | 0,5–1 |
  | Đo điện thoại + user xem | 0,5–1 |

  Ghi giờ thực tế.
- Dự kiến tách 2 brief:
  - (a) lõi + nhân vật PC + nối scene;
  - (b) cảm ứng + đo điện thoại.

## Quyết định đã chốt (ai chốt, khi nào)
- **User 2026-10-22 (0028, "theo đề xuất"):**
  - nhân vật khối ghép cao 2 ô, góc nhìn thứ ba;
  - PC: WASD / chuột / Space (bậc 1 ô); cảm ứng: joystick + kéo xoay;
  - camera bay thành chế độ debug;
  - tầm với 4 ô, mở kho ≤ 3 ô (phần kho làm ở 7f).
- **Planner đề xuất, user xác nhận 2026-10-22 ("đồng ý cả 7"):**
  - tầm với tính Euclid từ tâm thân tới tâm ô;
  - nhắm ô vẫn theo con trỏ chuột như hiện nay, xoay bằng giữ chuột giữa (giữ cách đang dùng);
  - được dùng công cụ khi đang đi;
  - nước ×0,5, không bơi;
  - nhân vật và sinh vật đi xuyên nhau;
  - vị trí xuất hiện: SimView ở trại, TerrainView ở checkpoint Map 1;
  - Bench/SimBench/HashCheck giữ camera bay.

## Câu hỏi còn mở
- (không)
