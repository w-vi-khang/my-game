# Handoff

> Trạng thái: đang dở
> Cập nhật: 2026-10-22

<!-- AI ghi khi context lớn / trước khi /clear / khi user yêu cầu (/ai-workflow-kit:handoff).
File này được commit (theo sang máy khác). Phiên sau dùng xong (đã tóm tắt, user xác nhận bước tiếp theo) -> XOÁ file;
nội dung cũ vẫn xem lại được bằng: git log -p -- .ai/handoff.md
Chỉ ghi sự thật đã xác minh. -->

## Mục tiêu
Làm [0028](../design/decisions/0028-nhan-vat-tui-vat-roi-tu-trong-trot.md), [0029](../design/decisions/0029-nhieu-diem-nhin-camera-quan-sat.md) (Chốt) theo thứ tự user chọn 2026-10-22:
1. ~~world 7h (a)~~, ~~sim 7e~~, ~~world 7h (b)~~ — xong (brief 0035–0039).
2. **sim 7f — tự trồng trọt + phép Mưa + kho ↔ túi ← ĐANG LÀM** ([task contract](tasks/m1-sim-buoc-7f-tu-trong-trot.md), user xác nhận đề xuất 1–11 + xới 6 giây).
3. world 7i–7k (nhiều điểm nhìn, camera quan sát, GPU culling).
4. sim bước 8 — môi trường sống (user tạm hoãn).

## Đã làm (kèm kết quả kiểm chứng)
- Task contract 7f + brief 0040 (`d373626`). Brief 0040 lõi Game.Core: code `2f713c4`, kết quả `67a8881`, review đầy đủ (phiên con Opus) `32f0656` — **đã duyệt**, EditMode 472/472 [run, phiên review], PlayMode 121/121 (theo executor). Giờ thực tế 11 phút, đã ghi roadmap.
- Brief 0041 client A (`745a00e`): tự đi tới + xới 6 giây (`PlayerCharacter.AutoWalk`, `Client/PlayerFarmActions.cs`), chuột phải bỏ biển, `TillResult.SelfTill`, nút `Tự xới` trong bảng ô ruộng, phép Mưa (R), click thu hoạch, HUD. Review `đầy đủ + user xem`. Executor xong: code `f4138f5`, kết quả `43a2198`; theo executor PlayerFarmPlayTests 12/12, PlayMode 133/133, EditMode 472/472. Đã đặt thẻ review đầy đủ (Opus · medium, task_6ed6fa73) — chưa chạy lúc ghi.

## Còn lại
1. Review đầy đủ 0041 (thẻ đã đặt) → liệt kê cho user mục "User tự test" của brief 0041 (8 bước), chờ phản hồi.
2. Brief **0042** client B: bảng túi (phím B; khối / túi hạt / nông sản, ẩn dòng 0), chế độ gieo (`Sim.CheckPlayerSow`/`PlayerSow`; HUD `Gieo Cây A (còn N) — chuột phải / Esc để thoát`; Tab cũng thoát; hết hạt tự thoát; con trỏ trên bảng không đào/đặt), bảng kho có nút chuyển (`Sim.StorageInReach`, `StoreToStock`, `TakeFromStock`; mỗi click 1, Shift + click tất cả; ngoài tầm dòng `Lại gần kho (8 ô) để chuyển đồ`; click khối kho trong 8 ô mở bảng). Review `user xem`.
3. Sau 0042: cập nhật tài liệu — mechanics (luật tự trồng), controls-ui (B, R, chuột phải bỏ biển, chế độ gieo, click biển tự xới, click cây chín thu hoạch), glossary nếu có từ mới, simulation-tech 7f trạng thái, roadmap giờ; `python3 tools/check_design.py`.
4. Task "Điều khiển cảm ứng" — làm sau, user chưa chọn lúc nào.
5. Còn treo (nhỏ): `SeaSkyTintPlayTests` comment sai chữ; test kink địa hình sát ngưỡng (0,0106 / 0,0111); `TerrainBench` chế độ đào mỗi khung (mặc định tắt) sẽ dồn vật rơi.

## Quyết định quan trọng & lý do
- Lệnh người chơi = hàm `Simulation` gọi giữa hai nhịp, `CompleteByPlayer` đánh dấu công đoạn Done/Player (theo simulation-tech "Người chơi làm công đoạn", user 2026-10-13).
- Thu hoạch người chơi: số theo `sim_m1.json` (Cây A 1+1, Cây B 3+2), không chở. Khối không cất kho (kho M1 chỉ hạt + nông sản).
- Phiên con: mở mới bằng `start_session` (`use_worktree: false`); review đầy đủ giao Opus · medium qua thẻ own_initiative; review nhẹ làm ở phiên Planner.
- Dọn sau PlayMode: `git checkout -- client/ProjectSettings/EditorSettings.asset`; sau build APK: `git checkout -- client/ProjectSettings/UnityConnectSettings.asset`.
- Điện thoại Xiaomi 24095PCADG, gỡ lỗi Wi‑Fi (`adb mdns services`); không dùng 192.168.1.7:6060 (TV).

## File liên quan
- `.ai/tasks/m1-sim-buoc-7f-tu-trong-trot.md`; `.ai/briefs/0040-sim-7f-loi-tu-trong-trot.md`, `.ai/briefs/0041-sim-7f-client-tu-xoi-mua-thu.md`.
- Core: `client/Assets/Game/Core/Sim/Simulation.cs:983-1150` (API người chơi), `Core/PlayerTimers.cs`, `Core/PlayerBag.cs`, `Core/Sim/SimConfig.cs:191-192`.
- Client: `Client/{PlayerCharacter,TerrainEditInput,SimView,PlayerFarmActions}.cs`; test `Tests/Client/{PlayerFarmPlayTests,SimFarmUiPlayTests}.cs`.

## Prompt tiếp tục
```text
Đọc .ai/handoff.md. Sim 7f: brief 0040 đã duyệt; brief 0041 executor xong (f4138f5, 43a2198). Review đầy đủ 0041 (git log xem đã có commit review chưa) → liệt kê user xem; sau đó viết brief 0042 (bảng túi, chế độ gieo, kho ↔ túi).
Model đề xuất: Opus · high (Planner).
```
