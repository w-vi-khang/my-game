# Task: M1 simulation bước 7d — hàng đợi ô đợi (loại × số lượng), ô tiến trình, tạm dừng xới, bảng ô ruộng dạng ô vuông

> Model thực thi: Sonnet · medium  (AGENTS.md §10.2)
> Trạng thái: **xong** 2026-10-22 (brief 0031 đã duyệt). Trước đó: user xác nhận 2026-10-22 (0027 Chốt, ước lượng 2–3, tên cấu hình). Sau bước 7c ([brief 0030](../briefs/0030-sim-cong-cu-xoi-bang-giong-kho.md), đã duyệt); làm **trước bước 8**.

## Mục tiêu
Đổi hàng đợi vụ của ô ruộng theo [0026](../../design/decisions/0026-hang-doi-o-doi-theo-loai.md) (Chốt) + [0027](../../design/decisions/0027-o-tien-trinh-tam-dung-xoi-khong-tra-hat.md) (Chốt 2026-10-22). Ước lượng 2–3 ngày-người (roadmap M1).

Pillar: P2 (sinh vật làm việc trên nông trại — người chơi giao việc).

Thành phần phụ trách: **client Unity** — lõi `Game.Core.Sim` + `Game.Core.View` (C# thuần, test EditMode) và `Game.Client.SimView` (OnGUI, test PlayMode).

Làm:
1. **Lõi — ô đợi:** `FarmPlot` thay `List<int> Queue` bằng danh sách ô đợi (loại cây + số lượng); mỗi ô ruộng giữ `SlotCount` (N) và `SlotMax` (M) riêng, khởi tạo từ `sim_m1.json` (`farm.queueSlots = 2`, `farm.slotMaxCount = 3`, bỏ `plotQueueCapacity`, `configVersion` 3).
   - Chọn hạt X: ô đợi cuối là X và < M → +1; ngược lại thêm ô đợi mới (nếu còn); không gộp ô cùng loại kề nhau.
   - +/− trên một ô đợi; về 0 → xoá, đôn lên; xoá cả ô đợi.
2. **Lõi — ô tiến trình:** vụ hiện tại tách khỏi ô đợi; vụ rời ô đợi đầu (−1) **khi bắt đầu gieo**; xới lại vẫn thuộc ô đợi đầu; thu hoạch xong → ô tiến trình trống → vụ kế.
3. **Lõi — tạm dừng xới:** khi ô ruộng đang xới lại: tạm dừng (sinh vật đang xới về rảnh, giữ tiến độ, công đoạn không được giao) / tiếp tục. Từ lúc gieo trở đi: huỷ vụ hiện tại, **không trả hạt**; vụ kế bắt đầu nếu còn.
4. **Lõi — huỷ không trả hạt:** đào/che ô ruộng (0020) không trả hạt; hàng đã thu vẫn chở về.
5. **`SimDemo`/`SimStress`/`SimHash`:** `KeepFarming` thêm vụ khi ô đợi trống **và** ô tiến trình trống (giữ nhịp như cũ); mã băm gồm ô đợi, vụ hiện tại, cờ tạm dừng.
6. **`Core/View/SiteStatus`:** chuỗi ô đợi ("2/3"), chuỗi + % ô tiến trình ("Xới 60%", "Cây A · lớn 40%", "Cây A · chín"…).
7. **Bảng ô ruộng (`SimView`, OnGUI):** hàng ô vuông hạt giống (hình tạm: ô màu + chữ, icon hạt góc trên trái); N ô đợi (trống/hình cây, nút x góc trên phải, thanh "− 2/3 +" bên dưới); ô tiến trình + thanh tiến trình + nút xoá (lúc xới = tạm dừng → "Tiếp tục"); đóng bảng khi đang tạm dừng → tiếp tục.

## Ngoài phạm vi
- Nâng cấp tài năng/cấp người chơi đổi N, M; **giới hạn số ô ruộng** → [ideas](../../design/ideas.md) (0027 mục 6).
- Art hình cây / icon hạt / bảng; tên thật 2 loại cây.
- Thao tác chạm (mobile) — theo hạng mục Điều khiển.
- Hiện số hạt trong kho trên ô hạt giống (không yêu cầu).
- Số liệu cân bằng (`game-balance`); môi trường sống (bước 8).

## Tiêu chí chấp nhận (kiểm chứng được)
Cột "Xong khi" của bước 7d trong simulation-tech:
- [ ] 2 ô × 3: chọn A 5 lần → A3, A2; A3 rồi B3; 3 ô × 3: A3, B3, A3 trồng đúng thứ tự; ô đầy → chọn bị từ chối — kiểm bằng: test EditMode `CropQueueTests`.
- [ ] +/−/x: về 0 xoá + đôn lên; không gộp (A3, B2, A1 xoá B → A3, A1); −/+ không đụng vụ đang làm — kiểm bằng: test EditMode.
- [ ] Vụ rời ô đợi lúc bắt đầu gieo (không phải lúc xới); thu hoạch → vụ kế — kiểm bằng: test EditMode.
- [ ] Tạm dừng xới: sinh vật rời việc, tiến độ giữ, không giao lại tới khi tiếp tục; tiếp tục → xới xong đúng số nhịp còn lại — kiểm bằng: test EditMode.
- [ ] Huỷ vụ hiện tại / đào / che ô ở từng pha → kho không được trả hạt; hàng đã thu vẫn về kho — kiểm bằng: test EditMode (sửa kỳ vọng test 0020/0025 cũ).
- [ ] N, M riêng từng ô ruộng (đổi trên một ô không ảnh hưởng ô khác); `sim_m1.json` sai → lỗi khi nạp — kiểm bằng: test EditMode.
- [ ] Cùng seed → cùng mã băm; `FarmTests` mốc thời gian không đổi với `KeepFarming` — kiểm bằng: test EditMode.
- [ ] Chuỗi ô đợi / ô tiến trình đúng — kiểm bằng: test EditMode `SiteStatusTests`.
- [ ] Bảng ô ruộng: chọn hạt, +/−/x, tạm dừng/tiếp tục, đóng bảng → tiếp tục (gọi qua hàm của `SimView`) — kiểm bằng: test PlayMode `SimFarmUiPlayTests`.
- [ ] EditMode + PlayMode hết đỏ; `Game.Core` không thêm `using UnityEngine` — kiểm bằng: `run_tests` qua MCP + grep.
- [ ] User xem SimView: bảng ô ruộng dễ dùng trên PC — kiểm bằng: user chơi thử (mức review `user xem`).

## Ràng buộc
- Unity 6000.4.8f1; OnGUI như bảng hiện có (API mới phải có link `[docs]` 6000.4 trong brief).
- Mô phỏng số nguyên, tất định; `Game.Core` No Engine References.
- Unity Editor dùng chung qua MCP (0010); sau PlayMode `git checkout -- client/ProjectSettings/EditorSettings.asset`.
- Ghi giờ bắt đầu/kết thúc.

## Quyết định đã chốt (ai chốt, khi nào)
- 0026 (user Chốt 2026-10-22).
- User 2026-10-22 (khi viết task này) → ghi ở 0027 (user Chốt 2026-10-22): ô tiến trình; vụ rời ô đợi lúc gieo; nút xoá ô tiến trình lúc xới = tạm dừng, bấm tiếp tục hoặc đóng bảng → tiếp tục; huỷ/đào/che không trả hạt; bảng dạng ô vuông như mục 7; N, M theo từng ô ruộng, nâng cấp + giới hạn số ô ruộng để sau.
- Planner đề xuất, user xác nhận 2026-10-22: tên cấu hình `farm.queueSlots` / `farm.slotMaxCount`, `configVersion` 3; hình tạm OnGUI.

## Câu hỏi còn mở
- (Đã trả lời) Ước lượng: 2–3 ngày-người — roadmap + simulation-tech đã cập nhật.
- Tạm dừng xới khi ô đang xới lần đầu (biển, chưa là ô ruộng): không áp dụng — bấm biển vẫn là bỏ đánh dấu (7c).
