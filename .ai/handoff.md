# Handoff

> Trạng thái: đang dở
> Cập nhật: 2026-10-22

<!-- AI ghi khi context lớn / trước khi /clear / khi user yêu cầu (/ai-workflow-kit:handoff).
File này được commit (theo sang máy khác). Phiên sau dùng xong (đã tóm tắt, user xác nhận bước tiếp theo) -> XOÁ file;
nội dung cũ vẫn xem lại được bằng: git log -p -- .ai/handoff.md
Chỉ ghi sự thật đã xác minh. -->

## Mục tiêu
Làm [0028](../design/decisions/0028-nhan-vat-tui-vat-roi-tu-trong-trot.md), [0029](../design/decisions/0029-nhieu-diem-nhin-camera-quan-sat.md) (Chốt) theo thứ tự user chọn 2026-10-22:
1. ~~world 7h (a) — nhân vật PC~~ xong (brief 0035);
2. ~~sim 7e — vật rơi + túi + nhặt + túi hạt~~ xong (brief 0036, 0037, 0038 tầm với 8 ô — đã duyệt);
3. ~~world 7h (b) — đo FPS điện thoại~~ xong (brief 0039 đã duyệt: 49,4 FPS, 0 lỗ); cảm ứng → task "Điều khiển cảm ứng" sau (user 2026-10-22: "làm trên máy tính trước…");
4. sim 7f — tự trồng trọt + phép Mưa + kho ↔ túi ← TIẾP THEO;
5. world 7i–7k (nhiều điểm nhìn, camera quan sát, GPU culling);
6. sim bước 8 — môi trường sống (user tạm hoãn).

## Đã làm (kèm kết quả kiểm chứng)
- Brief 0037 (`35ddfd4` code, `3d0ac0e` kết quả, `69cea00` review): `Client/DropView.cs` (RenderMeshInstanced), F = Thu, click nhặt (`Core/DropPick.cs`), túi hạt Cây A × 4 đầu game (`Core/StarterBag.cs`, `PlayerCharacter.StarterStack`), HUD `Túi hạt Cây A: N`. EditMode 437/437, PlayMode 117/117 (theo brief). User xem: "đa số đều ổn" → đã duyệt (`0b3a415`).
- `0b3a415`: 0031 (mọi thao tác 8 ô, gồm mở kho — user nói "tất cả"; nếu user phản đối mở kho thì sửa 0031), 0030 → bị thay thế; mechanics, glossary, world-tech 7h, decisions/README; roadmap dòng sim 7e (giờ 18 + 10 phút); simulation-tech 7e Xong. `python3 tools/check_design.py` [run]: hợp lệ, 56 file.
- Brief 0038 (`37880e8` code, `64668bb` kết quả, `25da3f3` review nhẹ): `ToolReach = HandReach = 8f`, chữ HUD "8 ô", test biên. EditMode 437/437, PlayMode 117/117 (theo brief).

## Còn lại
1. Sim 7f: task contract (`/ai-workflow-kit:task`) theo 0028, rồi brief (số kế: **0040**).
2. Task "Điều khiển cảm ứng" (joystick, kéo xoay, nút nhảy, chạm dùng công cụ, nút Thu) — làm sau, user chưa chọn lúc nào.
3. Còn treo (nhỏ): `SeaSkyTintPlayTests` comment sai chữ; test kink địa hình sát ngưỡng (0,0106 / 0,0111); `TerrainBench` chế độ đào mỗi khung (mặc định tắt) sẽ dồn vật rơi.

## Quyết định quan trọng & lý do
- Phiên con: clear phiên con bị auto mode chặn → mở phiên con mới (`start_session`, `use_worktree: false`). Review đầy đủ giao Opus · medium (thẻ own_initiative); review nhẹ làm luôn ở phiên Planner cho đỡ tốn.
- Điện thoại: Xiaomi 24095PCADG; 2026-10-22 USB không thấy, user bật gỡ lỗi qua Wi‑Fi → `adb devices` hiện `adb-PFQCRWVWR46HU4GQ-…._adb-tls-connect._tcp` (dò bằng `adb mdns services`); không dùng 192.168.1.7:6060 (TV).
- Dọn sau PlayMode: `git checkout -- client/ProjectSettings/EditorSettings.asset`; sau build APK: `git checkout -- client/ProjectSettings/UnityConnectSettings.asset`.

## File liên quan
- `design/decisions/0028-*.md`, `0031-*.md`; `.ai/briefs/0037-sim-7e-vat-roi-client.md`, `.ai/briefs/0038-tam-voi-8-o.md`; `.ai/tasks/m1-world-7h-nhan-vat.md` (phần b).
- `client/Assets/Game/Core/{PlayerRules,DropField,DropPick,StarterBag,PlayerBag}.cs`, `client/Assets/Game/Client/{PlayerCharacter,TerrainEditInput,DropView}.cs`.

## Prompt tiếp tục
```text
Đọc .ai/handoff.md. 7h + 7e xong. Viết task contract sim 7f (tự trồng trọt + phép Mưa + kho ↔ túi, theo 0028) rồi brief 0040.
Model đề xuất: Opus · high (Planner).
```
