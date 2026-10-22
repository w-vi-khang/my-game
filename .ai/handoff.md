# Handoff

> Trạng thái: đang dở
> Cập nhật: 2026-10-22

<!-- AI ghi khi context lớn / trước khi /clear / khi user yêu cầu (/ai-workflow-kit:handoff).
File này được commit (theo sang máy khác). Phiên sau dùng xong (đã tóm tắt, user xác nhận bước tiếp theo) -> XOÁ file;
nội dung cũ vẫn xem lại được bằng: git log -p -- .ai/handoff.md
Chỉ ghi sự thật đã xác minh. -->

## Mục tiêu
Làm [0028](../design/decisions/0028-nhan-vat-tui-vat-roi-tu-trong-trot.md) và [0029](../design/decisions/0029-nhieu-diem-nhin-camera-quan-sat.md) (Chốt, user 2026-10-22) theo thứ tự:
1. world 7h — nhân vật;
2. sim 7e — vật rơi + túi + nhặt + túi hạt;
3. sim 7f — tự trồng trọt + phép Mưa + kho ↔ túi;
4. world 7i — nạp nhiều điểm nhìn (M1 tối đa 2, sau 4–5);
5. world 7j — camera quan sát;
6. world 7k — thử GPU occlusion culling;
7. rồi sim bước 8 — môi trường sống.

## Đã làm (kèm kết quả kiểm chứng)
- Brief 0033, 0034 đã duyệt. 0034 biển nhuộm màu trời: user xem "ok ổn".
- **Đã commit:**
  - `622c2d9`: sổ sách sim 7b/7c xong.
  - `9ca168b`: quyết định 0028 + 0029, cập nhật mechanics, controls-ui, world-tech 7h–7k, simulation-tech 7e/7f, roadmap (M1 101,5–167; M2 324–618), glossary, ideas, decisions/README.
  - `python3 tools/check_design.py` [run]: hợp lệ, 54 file.
- **Sim bước 8:** user tạm hoãn. Đề xuất mặc định đã nêu cho user (chưa trả lời):
  - vùng = vùng mô phỏng 64 × 64; demo đầu chỉ có L1;
  - L2: ≥ 6 ô ruộng + ≥ 3 nông sản + buổi Bình minh;
  - L3: ≥ 15 ô đá lộ thiên + ≥ 6 ô ruộng + Ban đêm;
  - bảng phím H; 2 brief.
- **Task contract `.ai/tasks/m1-world-7h-nhan-vat.md`:** đã viết, **chưa commit, chờ user xác nhận**. Có 7 đề xuất của Planner trong mục "Quyết định đã chốt" cần user duyệt.

## Còn lại
1. User xác nhận task contract 7h → commit → `/ai-workflow-kit:brief` (dự kiến tách 2 brief: (a) lõi + nhân vật PC + nối scene, (b) cảm ứng + đo điện thoại). Số brief kế: **0035**.
2. Sau đó lần lượt task contract sim 7e, 7f, world 7i–7k, rồi sim bước 8.
3. Còn treo (nhỏ):
   - `SeaSkyTintPlayTests` có comment sai chữ (base, `BaselineFilled`);
   - test kink địa hình sát ngưỡng (0,0106 so với 0,0111).

## Quyết định quan trọng & lý do
- **Phiên con:** clear phiên con bị auto mode chặn → mở phiên con mới (`start_session`, `use_worktree: false`).
- **Điện thoại:** Xiaomi 24095PCADG, dò cổng bằng `adb mdns services`; không dùng 192.168.1.7:6060 (TV).
- **Dọn sau khi chạy:** sau PlayMode → `git checkout -- client/ProjectSettings/EditorSettings.asset`; sau build APK → `git checkout -- client/ProjectSettings/UnityConnectSettings.asset`.

## File liên quan
- `design/decisions/0028-*.md`, `0029-*.md`; `design/02-gameplay/mechanics.md` (mục "Nhân vật, túi đồ, tự trồng trọt — luật M1").
- `client/Assets/Game/Client/{DebugFlyCamera,TerrainEditInput,TerrainChunkView,MinimapView,SimView}.cs`.

## Prompt tiếp tục
```text
Đọc .ai/handoff.md. Hỏi user xác nhận task contract .ai/tasks/m1-world-7h-nhan-vat.md (7 đề xuất của Planner), rồi /ai-workflow-kit:brief cho world 7h.
Model đề xuất: Opus · high (Planner).
```
