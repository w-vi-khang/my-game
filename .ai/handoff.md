# Handoff

> Trạng thái: đang dở
> Cập nhật: 2026-10-22

<!-- AI ghi khi context lớn / trước khi /clear / khi user yêu cầu (/ai-workflow-kit:handoff).
File này được commit (theo sang máy khác). Phiên sau dùng xong (đã tóm tắt, user xác nhận bước tiếp theo) -> XOÁ file;
nội dung cũ vẫn xem lại được bằng: git log -p -- .ai/handoff.md
Chỉ ghi sự thật đã xác minh. -->

## Mục tiêu
Làm [0028](../design/decisions/0028-nhan-vat-tui-vat-roi-tu-trong-trot.md), [0029](../design/decisions/0029-nhieu-diem-nhin-camera-quan-sat.md), [0030](../design/decisions/0030-tam-voi-cong-cu-10-o.md) (Chốt) theo thứ tự user chọn 2026-10-22:
1. ~~world 7h (a) — nhân vật PC~~ xong (brief 0035 đã duyệt);
2. **sim 7e — vật rơi + túi + nhặt + túi hạt** ← TIẾP THEO; brief đầu của 7e gộp luôn đổi tầm với 10 ô (0030);
3. world 7h (b) — cảm ứng + đo FPS điện thoại;
4. sim 7f — tự trồng trọt + phép Mưa + kho ↔ túi;
5. world 7i–7k (nhiều điểm nhìn, camera quan sát, GPU culling);
6. sim bước 8 — môi trường sống (user tạm hoãn).

## Đã làm (kèm kết quả kiểm chứng)
- `d7d2e3d` task contract 7h user xác nhận 7 đề xuất. Lăn chuột = thu phóng, Alt + lăn = chọn loại (user).
- `0b7cf38` brief 0035; executor `c7519f9` + `eaa4e5b`: EditMode 407/407, PlayMode 112/112 (theo brief); review đầy đủ `5cd63b8`; user xem 10 điểm ổn.
- `0168082`: 0030 (đào/đặt/xới 10 ô; nhặt, gieo, thu hoạch, Mưa 4 ô; mở kho ≤ 3 ô) + mechanics, glossary, world-tech 7h, decisions/README, roadmap giờ 7h(a); brief 0035 **đã duyệt**. `python3 tools/check_design.py` [run]: hợp lệ, 55 file.
- Code hiện có (brief 0035): `Core/PlayerRules.cs` (`Reach = 4f`, `InReach`, `Occupies`, `ClampToWorld`, `InWater`), `Client/PlayerCharacter.cs` (`Active`, `InReach(Vector3Int)`), `Client/FollowCamera.cs`; `TerrainEditInput` kiểm tầm ở `ShowAim` + `UseTool`; đào hiện vẫn cộng thẳng vào `MaterialInventory` (`WorldEditor.TryDig`).

## Còn lại
1. Viết task contract sim 7e (`/ai-workflow-kit:task`): đọc 0028, `design/02-gameplay/mechanics.md` mục "Nhân vật, túi đồ, tự trồng trọt — luật M1", `design/04-tech/simulation-tech.md` bước 7e. Gộp đổi tầm 10 ô (0030: tách tầm công cụ 10 / tầm việc tay 4 trong `PlayerRules`, sửa test biên). Hỏi user xác nhận → brief (số kế: **0036**).
2. Sau 7e: 7h (b) cảm ứng + điện thoại, rồi 7f, 7i–7k, sim 8.
3. Còn treo (nhỏ): `SeaSkyTintPlayTests` comment sai chữ; test kink địa hình sát ngưỡng (0,0106 / 0,0111).

## Quyết định quan trọng & lý do
- Phiên con: clear phiên con bị auto mode chặn → mở phiên con mới (`start_session`, `use_worktree: false`). Review sau exec giao Opus · medium (thẻ own_initiative).
- Điện thoại: Xiaomi 24095PCADG, dò cổng `adb mdns services`; không dùng 192.168.1.7:6060 (TV).
- Dọn sau PlayMode: `git checkout -- client/ProjectSettings/EditorSettings.asset`; sau build APK: `git checkout -- client/ProjectSettings/UnityConnectSettings.asset`.

## File liên quan
- `design/decisions/0028-*.md`, `0030-*.md`; `.ai/tasks/m1-world-7h-nhan-vat.md`; `.ai/briefs/0035-world-7h-nhan-vat-pc.md`.
- `client/Assets/Game/Core/{PlayerRules,WorldEditor,MaterialInventory}.cs`, `client/Assets/Game/Client/{PlayerCharacter,TerrainEditInput,TerrainChunkView,SimView}.cs`.

## Prompt tiếp tục
```text
Đọc .ai/handoff.md. Viết task contract sim 7e (vật rơi + túi + nhặt + túi hạt, gộp tầm với 10 ô theo 0030), hỏi user xác nhận, rồi /ai-workflow-kit:brief.
Model đề xuất: Opus · high (Planner).
```
