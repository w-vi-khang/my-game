# Task: M1 world 7j — camera quan sát tối giản

> Model thực thi: Sonnet · medium  (AGENTS.md §10.2)
> Trạng thái: **gộp vào [7l](m1-world-7l-dieu-khien-pc.md)** (user 2026-10-23, [0034](../../design/decisions/0034-dieu-khien-pc-click-di-chuyen.md)) — code brief 0053 giữ, cách đặt camera đổi thành vật phẩm trong túi ở 7l c. Trước đó: user xác nhận 2026-10-23. Bước 2 của [0029](../../design/decisions/0029-nhieu-diem-nhin-camera-quan-sat.md) (Chốt), dòng 7j trong [world-tech](../../design/04-tech/world-tech.md#kế-hoạch-prototype-m1). Ước lượng 1–2 ngày-người. Dự kiến 1–2 brief.

## Mục tiêu
Người chơi đặt được **một camera quan sát** ở một chỗ trong world, rồi nhìn qua nó toàn màn hình mà không phải đi tới. Ví dụ: đặt ở Map 2, đi về Map 1, bấm xem để thấy Map 2.

- **Pillar:** P3 "Tự do trong khung truyện": theo dõi nhiều nơi trong world rộng mà không phải đi lại (0029, mục Hệ quả).
- **Thành phần:** client Unity (`Game.Client`). Không cần lõi `Game.Core` mới: bộ nạp nhiều điểm nhìn đã có từ 7i.

**Hiện trạng [repo]:**
- Bộ nạp đã nhận điểm nhìn thứ hai: `TerrainChunkView.AddViewpoint(Transform, int, int)`, `RemoveViewpoint`, `MissingColumnsWithin(Transform, int)`, `maxViewpoints = 2` (brief 0051; `client/Assets/Game/Client/TerrainChunkView.cs:38,104,113`).
- Công cụ: `enum EditTool { Dig, Place, Till }`, Tab đổi vòng Đào → Đặt → Xới (Xới chỉ khi có), chuột trái dùng công cụ (`client/Assets/Game/Client/TerrainEditInput.cs:9,87,194`).
- Phím đọc qua `GameInput` / `GameControls.inputactions` (0033). Phím `V` chưa dùng.
- Chữ hiển thị nằm trong bảng chuỗi `Resources/Lang` (0032). `HardcodedTextScanTests` chặn chữ tiếng Việt viết thẳng trong code.

## Ngoài phạm vi
- **Lưu camera khi thoát game:** không làm ở M1 (user, 2026-10-23).
- **Xoay hướng nhìn khi đang xem:** không làm (user, 2026-10-23).
- **Nhiều camera, xem nhiều camera cùng lúc (khung nhỏ):** để sau ([ideas](../../design/ideas.md), 0029 mục 3).
- **Mô phỏng sinh vật ở map có camera:** M2 (0029 mục 3). Nhìn qua camera chỉ thấy địa hình, nước, dấu mốc, và sinh vật nếu nằm trong vùng mô phỏng sẵn có.
- **Đổi công cụ bằng cảm ứng:** thuộc task Điều khiển cảm ứng. Ở đây chỉ cần nút HUD bấm/chạm được.
- **Đánh dấu camera trên minimap.**
- **GPU occlusion culling:** 7k.

## Tiêu chí chấp nhận (kiểm chứng được)
- [ ] **Công cụ Camera (công cụ thứ 4).**
  - Tab đổi vòng Đào → Đặt → Xới (khi có) → Camera → Đào.
  - Chuột trái khi cầm Camera: đặt camera quan sát tại **vị trí và hướng của camera đang nhìn**.
  - M1 tối đa 1 camera: đặt lần nữa thì thay chỗ cũ.
  - Chỗ đặt có một **khối nhỏ đánh dấu**: hình khối có sẵn, không cần art, không va chạm. Khối ẩn khi đang xem qua chính camera đó.
  - Kiểm bằng: test PlayMode. Đặt → có camera đúng vị trí và hướng. Đặt lần 2 → vẫn 1 camera, ở chỗ mới. Vòng Tab đúng thứ tự. Các test Tab/công cụ cũ vẫn qua (thêm công cụ chỉ đổi những test kiểm đúng vòng cũ).
- [ ] **Xem qua camera.**
  - Bấm `V`, hoặc nút trên HUD (nút chỉ hiện khi đã có camera) → camera quan sát thành **điểm nhìn thứ hai** của bộ nạp (bán kính 128/160).
  - Hiện chữ "Đang tải…" tới khi vùng 128 ô quanh camera nạp đủ (`MissingColumnsWithin(cam, 128) == 0`). Sau đó mới chuyển màn hình sang camera quan sát, toàn màn hình, góc nhìn cố định.
  - Khi đang xem: nhân vật đứng yên, không dùng công cụ, không nhận lệnh di chuyển/xoay.
  - `V`, nút HUD hoặc `Esc` → quay về camera nhân vật, gỡ điểm nhìn thứ hai. Cột chỉ thuộc vùng camera được gỡ như brief 0051.
  - Chưa có camera → `V` không làm gì.
  - Kiểm bằng: test PlayMode. Đặt camera xa (Map 2), quay về gần trại, bấm xem → chờ tải rồi camera quan sát đang hiển thị, `ViewpointCount == 2`, `MissingColumnsWithin(cam, 128) == 0`. Quay về → camera nhân vật hiển thị, `ViewpointCount == 1`, cột chỉ thuộc vùng camera đã gỡ. Chưa có camera thì `V` không đổi gì.
- [ ] **Chữ và phím theo quy ước.**
  - Tên công cụ, chữ "Đang tải…", nhãn nút nằm trong bảng chuỗi (0032).
  - Phím `V` thêm vào `GameControls.inputactions` (0033).
  - `HardcodedTextScanTests` qua.
- [ ] **Tài liệu:** thêm dòng "Camera quan sát" vào bảng điều khiển trong `design/02-gameplay/controls-ui.md` (Tab chọn Camera + chuột trái để đặt; V / nút để xem và quay về).
- [ ] **User xem (PC):**
  - đặt camera ở Map 2, đi về Map 1, bấm `V`;
  - thấy "Đang tải…" rồi thấy Map 2 không có lỗ;
  - bấm lại để quay về;
  - khối đánh dấu đúng chỗ.
- [ ] Toàn bộ EditMode + PlayMode qua; `git status` sạch sau bước dọn.

## Ràng buộc
- Chỉ dùng API điểm nhìn có sẵn của `TerrainChunkView`. Không đổi `ChunkStreamPlan`.
- Code và comment mới viết bằng tiếng Anh. Chữ người chơi đọc nằm trong bảng chuỗi.
- API Unity dùng trong brief phải tra docs 6000.4 (bật/tắt Camera, đổi camera hiển thị).
- Unity MCP dùng chung (0010). Sau PlayMode: `git checkout -- client/ProjectSettings/EditorSettings.asset`.

## Quyết định đã chốt (ai chốt, khi nào)
- Camera quan sát tối giản: đặt bằng công cụ, lấy vị trí + hướng đang nhìn, M1 tối đa 1; bấm nút xem toàn màn hình / bấm lại quay về; khi xem là điểm nhìn thứ hai — user, 0029, 2026-10-22.
- Công cụ thứ 4 trong vòng Tab + chuột trái để đặt; đặt lại thì thay; khối nhỏ đánh dấu; phím `V` + nút HUD (chỉ hiện khi có camera) để xem; `V` / nút / `Esc` để quay về; nhân vật đứng yên khi xem; góc nhìn cố định — user, 2026-10-23.
- Chờ vùng 128 ô quanh camera nạp đủ (hiện "Đang tải…") rồi mới chuyển hình; không lưu camera khi thoát game — user, 2026-10-23.

## Câu hỏi còn mở
_(không còn)_ — vị trí nút HUD và cách đổi camera hiển thị do Planner chọn khi viết brief (không thuộc §3.4).
