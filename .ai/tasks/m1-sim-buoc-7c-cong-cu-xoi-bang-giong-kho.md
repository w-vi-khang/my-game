# Task: M1 simulation bước 7c — công cụ Xới + biển canh tác, bảng chọn giống/hàng đợi, nút Kho (client)

> Model thực thi: Sonnet · medium  (AGENTS.md §10.2)
> Trạng thái: user xác nhận 2026-10-21 (hai câu còn mở theo đề xuất mặc định). Sau bước 7b ([brief 0029](../briefs/0029-sim-danh-dau-xoi-giong-kho.md), đã duyệt); làm **trước bước 8**.

## Mục tiêu
Bước 7c của [kế hoạch prototype simulation](../../design/04-tech/simulation-tech.md#kế-hoạch-prototype-m1), theo [0025](../../design/decisions/0025-nguoi-choi-xoi-dat-chon-giong-hang-doi.md) (Chốt). Ước lượng 3–5 ngày-người (roadmap M1).

Pillar: P2 (sinh vật làm việc trên nông trại — người chơi giao việc).

Thành phần phụ trách: **client Unity** (`client/Assets/Game/Client/`). Lõi `Game.Core.Sim` đã có API ở 7b (`MarkTill`, `CanMarkTill`, `Unmark`, `PlotAt`, `EnqueueCrop`, `RemoveCrop`, `State.Stock`); chỉ thêm vào lõi/View phần thuần dữ liệu nếu cần cho test (vd chuỗi hiển thị trong `Core/View`).

Làm:
1. **Công cụ Xới** — công cụ thứ ba cùng cấp Đào/Đặt (`TerrainEditInput`).
   - Tab xoay vòng Đào → Đặt → Xới.
   - Cầm Xới, chuột trái lên mặt trên ô đất/cỏ hợp lệ → `MarkTill`; lên ô đang có biển → `Unmark`; lên ô ruộng → mở bảng ô ruộng.
   - Chỉ báo nhắm (khung/bản mờ) cho Xới, ẩn khi ô sai luật — như công cụ Đặt (0027).
2. **Biển canh tác** — hiện ngay khi đánh dấu, mất khi xới xong / bỏ đánh dấu / đào-che ô. Hình tạm (cọc + tấm), chưa làm art.
3. **Bảng ô ruộng** (OnGUI, như SimView hiện có): 2 nút chọn giống (Cây A, Cây B) → `EnqueueCrop`; danh sách hàng đợi (2 chỗ) mỗi vụ có nút xoá → `RemoveCrop`; đóng bảng.
4. **Nút Kho** ở góc màn hình + phím K → bảng kho: nông sản và hạt từng loại cây, sức chứa đã dùng / tổng (`State.Stock`).
5. **SimView bỏ `SimDemo.KeepFarming`**: trại demo dựng sẵn ô ruộng + 1 vụ Cây A lúc đầu, sau đó người chơi tự chọn giống. `SimBench`/`SimStress` giữ `KeepFarming`.

## Ngoài phạm vi
- Thao tác chạm (mobile) — đi cùng phần cảm ứng của hạng mục Điều khiển.
- Art biển canh tác, art bảng UI; tên thật của 2 loại cây (user đặt).
- "Chọn thông minh" theo khuôn ([ideas](../../design/ideas.md)); người chơi tự đặt kho (M2); cấp ô ruộng (M2).
- Số liệu cân bằng 2 loại cây (`game-balance`, sau).
- Môi trường sống (bước 8).

## Tiêu chí chấp nhận (kiểm chứng được)
Theo cột "Xong khi" của bước 7c:
- [ ] Đánh dấu hiện biển ngay; xới xong biển mất; bỏ đánh dấu / đào ô có biển → biển mất — kiểm bằng: test PlayMode mới.
- [ ] Chọn giống trên bảng ô ruộng → vụ vào hàng đợi (đúng loại); xoá vụ → rời hàng đợi — kiểm bằng: test PlayMode (gọi qua lớp điều khiển bảng, không bấm chuột thật) hoặc EditMode cho phần thuần dữ liệu.
- [ ] Bảng kho đúng số (từng loại, đã dùng / tổng) — kiểm bằng: test EditMode chuỗi hiển thị trong `Core/View`.
- [ ] Tab xoay đủ 3 công cụ — kiểm bằng: test PlayMode hoặc user xem.
- [ ] Test EditMode + PlayMode hết đỏ; `SimViewPlayTests`, `SimViewMapsPlayTests`, `SimBenchPlayTests` đạt (kịch bản phụ thuộc `KeepFarming` trong SimView được brief ghi rõ cách đổi) — kiểm bằng: `run_tests` qua MCP.
- [ ] `Game.Core` không thêm `using UnityEngine` — kiểm bằng: grep.
- [ ] User xem SimView: đánh dấu/bỏ đánh dấu, biển, bảng chọn giống + hàng đợi, bảng kho dễ dùng trên PC — kiểm bằng: user chơi thử (mức review `user xem`).

## Ràng buộc
- Unity 6000.4.8f1, Input System 1.19.0 (`Keyboard.current`, `Mouse.current` như `TerrainEditInput`). API Unity mới trong brief phải có link `[docs]` 6000.4.
- Con trỏ trên bảng (bảng ô ruộng, bảng kho, nút Kho) chặn dùng công cụ — như `MinimapView.BlocksPointer`/`TimeDial.BlocksPointer`.
- Unity Editor dùng chung qua MCP (0010); sau PlayMode `git checkout -- client/ProjectSettings/EditorSettings.asset`.
- Ghi giờ bắt đầu/kết thúc.

## Quyết định đã chốt (ai chốt, khi nào)
- 0025 (user Chốt 2026-10-21) + luật 6–20 của brief 0029 (user xác nhận 2026-10-21).
- User 2026-10-21 (khi viết task này):
  - Tab xoay vòng Đào → Đặt → Xới; cầm Xới, chuột trái: đất/cỏ = đánh dấu, ô có biển = bỏ đánh dấu, ô ruộng = mở bảng chọn giống + hàng đợi. Nút Kho ở góc + phím K. Bảng vẽ bằng OnGUI. Biển là hình tạm (cọc + tấm).
  - SimView bỏ `KeepFarming`; trại demo có 1 vụ Cây A ban đầu rồi người chơi tự chọn; SimBench/SimStress giữ `KeepFarming`.

## Câu hỏi còn mở
- Có chọn giống lên ô có biển (chưa xới) từ client không — lõi cho phép (luật 13 brief 0029). Đề xuất mặc định: **không** ở 7c (chuột trái lên ô có biển = bỏ đánh dấu); Planner chốt khi viết brief.
- Có cho bấm ô ruộng khi đang cầm Đào/Đặt không. Đề xuất mặc định: không (chỉ khi cầm Xới, đúng lựa chọn của user).
