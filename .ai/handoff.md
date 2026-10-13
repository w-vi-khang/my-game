# Handoff

> Trạng thái: đang dở
> Cập nhật: 2026-10-13 15:35

<!-- AI ghi khi context lớn / trước khi /clear / khi user yêu cầu (/ai-workflow-kit:handoff).
File này được commit (theo sang máy khác). Phiên sau dùng xong (đã tóm tắt, user xác nhận bước tiếp theo) -> XOÁ file;
nội dung cũ vẫn xem lại được bằng: git log -p -- .ai/handoff.md
Chỉ ghi sự thật đã xác minh. -->

## Mục tiêu
M1 — prototype client tối giản ([0007](../design/decisions/0007-m1-toi-gian.md), user chuyển M0 → M1 ngày 2026-10-13). Đang làm kế hoạch prototype world ([world-tech](../design/04-tech/world-tech.md#kế-hoạch-prototype-m1)): xong bước 1–3, tiếp theo bước 4.

## Đã làm (kèm kết quả kiểm chứng)
- M0 xong: rà soát thiết kế (skill `local-design-review`), quyết định 0007 (M1 tối giản), 0008 (sửa lời văn file Chốt), 0009 (đại dương + tường sương mù ở rìa map); user duyệt luật M1, nền tảng code M1, hai kế hoạch prototype.
- Client Unity 6000.4.8f1 trong `client/` (template Universal 3D = urp-blank, URP 17.4.0, Input System 1.19.0, Test Framework 1.6.0, Burst 1.8.29, Collections 6.4.0, Mathematics 1.3.3). Product Name "Viventa"; tên gói Android vẫn là mặc định template (user đặt sau).
- Assembly: `Game.Core` (No Engine References — ô 2 byte, WorldSize/WorldData, WorldHash FNV-1a, TerrainParams), `Game.World` (TerrainGenerator Burst FloatMode.Deterministic, SurfaceNetsMesher, ChunkMeshWriter), `Game.Client` (ConfigLoader, HashCheckView, TerrainChunkView), `Game.Client.Editor` (HashCheckBuild, TerrainViewCapture), test `Game.Core.Tests`, `Game.World.Tests`.
- Bước 1 (`08c32c7`): map 64×32×64, 32 chunk; mã băm chuẩn `86b0747d4b2be65b` trùng giữa macOS ARM64 và Android 14 ARM64 (24095PCADG) [run].
- Bước 2 (`f17eb5f`): Surface Nets ×2 + làm mượt ≤ 0,2 ô; rãnh 1 ô giữ bề rộng; chỉ số 16 bit [run].
- Bước 3 (`f7e0ac9`): dựng 32 chunk song song; không hở ở biên; không vách mép map (lấy mẫu cột biên — 0009); Mac (Editor, Burst) 32 chunk ~29 ms, 1 chunk ~4,3 ms; cả map 61.800 tam giác; 4/93.058 cạnh không đa tạp (tính chất Surface Nets, nằm gọn trong chunk) [run].
- `python3 tools/unity_test.py` → 34/34 đạt; `python3 tools/check_design.py` → exit 0 [run 2026-10-13 15:33]. Repo sạch ở `f7e0ac9` (trước khi ghi handoff này).
- Giờ thực tế ghi ở `design/05-roadmap.md` mục "Theo dõi M1": bước 1 ~0,25 giờ, bước 2 ~0,1 giờ, bước 3 ~0,05 giờ (ước lượng 3–5 / 4–6 / 4–7 ngày-người).

## Còn lại
1. **Bước 4 world-tech:** đào/đặt nguyên ô + dựng lại chunk bẩn (và chunk bên cạnh nếu ô ở biên) + bake collider; kho vật liệu của người chơi. Luật: `design/02-gameplay/mechanics.md` mục "Luật M1 → Voxel world — luật M1" (đào → +1 kho vật liệu; đặt tiêu 1; không đào đá đáy/nước/khối cốt truyện — `Cell.IsDiggable` đã có). Tiêu chí xong: sửa ô → mesh cập nhật ≤ 2 khung hình trên PC. Ước lượng 3–5. Theo skill `feature-implementation`: viết task contract `.ai/tasks/m1-buoc-4-dao-dat.md`, user xác nhận, rồi làm.
2. Bước 5 (màu vật liệu theo đỉnh), 5b (đảo + vành biển generatorVersion 2 → mã băm mới, mặt nước, sương mù có sẵn — kiểm sương mù chạy với URP bằng render thật), 6 (đo trên mobile: ≥ 30 FPS).
3. Kế hoạch mô phỏng (simulation-tech, 8 bước) chưa bắt đầu.
4. Chưa xác minh: máy x64 (chỉ có Mac ARM64); thời gian dựng mesh trên điện thoại.

## Quyết định quan trọng & lý do
- C1 / 0007: M1 tối giản 41–70 ngày-người (gồm +2–3 của 0009) để biết sớm game có vui không.
- 0009 (user): mọi map có đại dương bao quanh, rìa xa là tường sương mù — không vách đứng.
- Nền tảng code: Core C# thuần + test EditMode; sinh địa hình trong Burst FloatMode.Deterministic (chỉ 64-bit → Android ARM64); cấu hình JSON nguồn ở `design/data/`, client giữ bản sao trong `Resources/Config` (test so khớp).
- Unity không định nghĩa exit code cho `-runTests` → `tools/unity_test.py` đọc XML kết quả; kết quả ở `client/Logs/tests/` (Unity báo lỗi với thư mục bắt đầu bằng "."). **Unity phải tắt** khi chạy test/build dòng lệnh.
- `UnityConnectSettings.m_Enabled` bị Unity bật khi build → đã trả về 0 (M1 không dùng dịch vụ Unity).
- Ghi giờ: lấy `date` thật, không đoán (đã hai lần ghi sai rồi sửa).

## File liên quan
- `design/04-tech/world-tech.md` (kế hoạch prototype, culling, mép map), `design/04-tech/architecture.md#nền-tảng-code-m1`, `design/02-gameplay/mechanics.md` (Luật M1), `design/05-roadmap.md` (Theo dõi M1), `design/decisions/0007`, `0009`
- `.ai/tasks/m1-buoc-1..3-*.md` (task contract đã xong)
- `client/Assets/Game/**`, `design/data/world_m1.json`, `tools/unity_test.py`
- Lệnh: test `python3 tools/unity_test.py`; ảnh `Unity -batchmode -quit -projectPath client -executeMethod Game.Client.Editor.TerrainViewCapture.Capture -logFile client/Logs/capture.log` → `client/Logs/terrain_view.png`; APK `... -buildTarget Android -executeMethod Game.Client.Editor.HashCheckBuild.BuildAndroid` → `client/Builds/hashcheck.apk`; adb ở `/Applications/Unity/Hub/Editor/6000.4.8f1/PlaybackEngines/AndroidPlayer/SDK/platform-tools/adb`.

## Prompt tiếp tục
```text
Tiếp tục công việc đang dở trong .ai/handoff.md — M1 prototype client (world: xong bước 1–3).
Bước tiếp theo: bước 4 của design/04-tech/world-tech.md "Kế hoạch prototype (M1)" — đào/đặt nguyên ô, dựng lại chunk bẩn, bake collider, kho vật liệu; theo skill feature-implementation (task contract → user xác nhận → làm).
Đọc handoff và các file liên quan trước, tóm tắt lại cho tôi trong 3 dòng rồi mới làm.
```
