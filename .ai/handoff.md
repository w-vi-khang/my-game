# Handoff

> Trạng thái: đang dở
> Cập nhật: 2026-10-13 21:27

<!-- AI ghi khi context lớn / trước khi /clear / khi user yêu cầu (/ai-workflow-kit:handoff).
File này được commit (theo sang máy khác). Phiên sau dùng xong (đã tóm tắt, user xác nhận bước tiếp theo) -> XOÁ file;
nội dung cũ vẫn xem lại được bằng: git log -p -- .ai/handoff.md
Chỉ ghi sự thật đã xác minh. -->

## Mục tiêu
M1 — xong phần **world** (bước 1–6 của world-tech, gồm 5.1–5.4, 5b, 5c, 5d); bước tiếp theo là **kế hoạch prototype simulation** (8 bước, `design/04-tech/simulation-tech.md` mục "Kế hoạch prototype M1"), chưa bắt đầu.

## Đã làm (kèm kết quả kiểm chứng)
- Commit cuối: `5cd1877` (bước 6). Chuỗi phiên này: `f05f917` bước 4 → `48b340c` 5.1 → `9190eb0` 5.2 (user commit) → `33d068a` 5.2b+5.3 → `ba08027` 5.4+5b+0012 → `895b222` 5c → `1cf17ec` 5d → `5cd1877` 6.
- Quyết định mới (đều Chốt bởi user 2026-10-13): **0010** MCP for Unity; **0011** địa hình khối bo cạnh + mép cỏ (thay bề mặt mịn); **0012** biển tràn theo mực nước + đặt khối kề khối đặc, đặt được vào nước; **0013** nước lan theo nhịp (rơi xuống ngay, ngang/dâng 1 ô/nhịp, 8 nhịp/giây).
- World hiện tại [run]: `BlockMesher` (bảng bo cạnh 256 tổ hợp, mesh kín, mép cỏ lượn sóng, AO theo đỉnh, mặt nước submesh 1), shader `Game/Terrain` (hoa văn bằng code, băm số nguyên), `Game/Ocean`, `Game/SkyClouds`; bộ sinh **generatorVersion 2** (đảo + vành biển), mã băm v2 `807d00bd048026a0` trùng Mac và Android; `WorldEditor` (luật đặt `NotAttached`, `StepWater/SettleWater`).
- Kiểm chứng cuối [run 21:2x]: EditMode 84/84, PlayMode 7/7 (MCP `run_tests`), `check_design.py` exit 0, Blender selftest ĐẠT. Điện thoại Xiaomi 24095PCADG (Mali-G615, Vulkan): **60,1 FPS** khi đào/đặt mỗi khung, mesh 2,81 ms/chunk, 82.740 tam giác; đường lưới mặt biển trên Mali đã hết sau khi đổi băm số nguyên.
- Ước lượng sau 0011–0013: M1 **51–85** ngày-người; tổng tới M4 ~12,0–22,7 năm (roadmap). Giờ thực tế mỗi bước ghi ở roadmap "Theo dõi M1".

## Còn lại
1. Bắt đầu simulation bước 1 (lõi mô phỏng: nhịp cố định 10/giây, số nguyên, seed) — theo đúng bảng kế hoạch trong `design/04-tech/simulation-tech.md`; viết task contract trước (`.ai/tasks/`), hỏi user các điểm chưa rõ.
2. Sau 4–6 hạng mục M1: tính lại hệ số ước lượng (AGENTS.local) — đã có 9 dòng "Theo dõi M1", thực tế ≪ ước lượng.
3. Việc nhỏ còn treo: chưa có hiệu ứng mặt nước dâng mượt trong một ô (mỗi ô đầy/rỗng); khối kính trong ideas.

## Quyết định quan trọng & lý do
- Thử trong Unity qua MCP; user tự thử bằng chuột thật. Unity phải TẮT nếu dùng `tools/unity_test.py`.
- Sau mỗi `run_tests` PlayMode qua MCP: `git checkout -- client/ProjectSettings/EditorSettings.asset`. Sau mỗi build APK: `git checkout -- client/ProjectSettings/UnityConnectSettings.asset` (build tự bật Unity Cloud; M1 không dùng dịch vụ Unity).
- Editor không focus → Play mode gần như đứng khung hình: dùng `EditorApplication.Step()` trong `execute_code` trước khi chụp.
- Đo điện thoại: `adb` ở `/Applications/Unity/Hub/Editor/6000.4.8f1/PlaybackEngines/AndroidPlayer/SDK/platform-tools/adb`; menu `Game/Build APK Bench` → `client/Builds/bench.apk`; đọc dòng `BENCH` trong logcat. Điện thoại phải mở khoá, màn hình sáng (adb không bấm phím được trên Xiaomi).
- Không tự đổi nội dung đã Chốt; thay đổi luật → quyết định mới trong `design/decisions/`.

## File liên quan
- `design/04-tech/world-tech.md` (kế hoạch + kết quả bước 1–6), `design/04-tech/simulation-tech.md` (kế hoạch tiếp), `design/05-roadmap.md`, `design/decisions/0010`–`0013`, `design/02-gameplay/mechanics.md` (luật M1).
- `client/Assets/Game/{Core,World,Client,Tests}/**`, `client/Assets/Game/Client/Shaders/`, `client/Assets/Game/Client/Resources/Shaders/Terrain.shader`, `client/Assets/Game/Client/Scenes/{TerrainView,Bench}.unity`, `design/data/world_m1.json`.

## Prompt tiếp tục
```text
Tiếp tục công việc trong .ai/handoff.md — M1 đã xong phần world (bước 1–6), bắt đầu kế hoạch prototype simulation (design/04-tech/simulation-tech.md, bước 1: lõi mô phỏng nhịp cố định, số nguyên, seed).
Đọc handoff trước, tóm tắt 3 dòng, rồi viết task contract cho simulation bước 1 và hỏi tôi các điểm chưa rõ trước khi code.
```
