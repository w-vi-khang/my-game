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
1. Brief **0043** (sửa HUD trái đè chữ trạng thái SimView + bỏ dòng "đang đi tới" khi đã xới; Sonnet · low, review nhẹ) — đã giao phiên child-sonnet-low (local_7b6e9939) 2026-10-22 → review nhẹ.
2. **Dồn cho user xem** (chưa xem): 8 mục "User tự test" brief 0041 + 9 mục brief 0042 + 1 mục 0043. 0041/0042 đã review đầy đủ đạt (`29dcece`, `bee80be`), chỉ chờ user. Planner đã tự chạy qua MCP trong scene SimView (2026-10-22): tự đi + xới 6 giây, gieo từ túi, từ chối gieo lên cỏ, Tab thoát gieo, Mưa tưới, thu hoạch +1/+1, chuyển kho trong tầm / ngoài tầm, bảng túi + kho không đè minimap / vòng thời điểm — đều đúng. Chưa kiểm được bằng MCP: cảm giác tự đi, chuột phải / Esc thật, click khối kho bằng chuột, Shift + click.
3. Sau khi user xem đạt: đặt 0041/0042/0043 `đã duyệt`, 7f xong (simulation-tech, roadmap). Tài liệu design 7f đã cập nhật (`a9ec042`).
4. Task "Điều khiển cảm ứng" — làm sau, user chưa chọn lúc nào.
5. Còn treo (nhỏ): `SeaSkyTintPlayTests` comment sai chữ; test kink địa hình sát ngưỡng (0,0106 / 0,0111); `TerrainBench` chế độ đào mỗi khung (mặc định tắt) sẽ dồn vật rơi; R trùng phím Nhập hồn M2 (ghi [ý tưởng] ở controls-ui).

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
Đọc .ai/handoff.md. Sim 7f: 0040 duyệt; 0041/0042 review đầy đủ đạt, chờ user xem; 0043 (sửa HUD) đã giao executor → review nhẹ, rồi đưa user danh sách xem gộp 0041–0043.
Model đề xuất: Opus · high (Planner).
```
