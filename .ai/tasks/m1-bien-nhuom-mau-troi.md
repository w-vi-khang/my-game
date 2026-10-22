# Task: M1 — biển nhuộm màu trời theo buổi, sương chuyển mềm ở chân trời

> Model thực thi: Sonnet · medium  (AGENTS.md §10.2)
> Trạng thái: **xong** 2026-10-22 (brief 0034 đã duyệt, user xem SimView ổn). Trước đó: user xác nhận 2026-10-22. Chưa có brief. Tiếp theo task [m1-sua-mau-chan-troi](m1-sua-mau-chan-troi.md) (brief 0033, đã duyệt 2026-10-22, commit `e15b86a`).

## Hiện tượng (user báo 2026-10-22, khi xem brief 0033)
Scene SimView, camera trại demo nhìn ra biển: "màn sương cao quá, không hoà màu với bầu trời ở các buổi". Sau 0033, màu sương và trời dưới chân trời đã trùng nhau [run: 244,168,117 lúc hoàng hôn] — phần còn lệch nằm **dưới** dải sương.

## Nguyên nhân (Planner đo 2026-10-22)
- Hình học: camera cao 26 m so với mặt biển (`Ocean` y = 12,9), sương tuyến tính 64–120, far clip 128 [run]. Mọi thứ xa hơn 120 m đều kín sương → từ chân trời xuống khoảng atan(26/120) ≈ 12° là một dải phẳng đúng màu chân trời. Phía trên chân trời trời đổi màu nhanh (`lerp(horizon, zenith, pow(up, 0.55))` — [repo] `client/Assets/Game/Client/Shaders/SkyClouds.shader:113`), nên dải phẳng dưới chân trời trông như một "màn" cao.
- Ngay dưới dải đó, mặt biển ở tầm 64–120 m có màu xám (110,98,96) [run] **không đổi theo buổi**: `baseCol = lerp(_DeepColor, _ShallowColor, saturate(0.25 + fres))` dùng màu cố định của material ([repo] `client/Assets/Game/Client/Shaders/Ocean.shader:111-112`) → biển và dải sương lệch màu rõ ở bình minh/hoàng hôn/đêm.
- Sương tuyến tính (`ComputeFogFactorZ0ToFar` + `MixFog`, [repo] `Ocean.shader:121-122`, `client/Assets/Game/Client/Resources/Shaders/Terrain.shader:152`) đạt kín đúng ở 120 m với độ dốc khác 0 → có chỗ gãy (đổi độ dốc đột ngột) trên màn hình tại mốc 120 m, đọc thành mép dưới của "màn sương".

## Mục tiêu
Nhìn ra biển ở mọi buổi, mặt biển xa chuyển dần sang màu chân trời rồi tan vào dải sương và trời, không còn mép/gãy ở mốc 120 m — mà **giữ far clip 128 và khoảng sương 64–120**.

Pillar: P1 "Hồi sinh để sưu tầm" ([00-vision](../../design/00-vision.md)) qua world 7f — buổi trong ngày là điều kiện môi trường sống ([0024](../../design/decisions/0024-buoi-trong-ngay-va-chon-thoi-diem.md)), người chơi phải nhìn ra buổi. Đây là hoàn thiện hiển thị của 7f, không phải tính năng mới.

Thành phần phụ trách: **client Unity** — shader `Game/Ocean` (dùng cho cả `OceanBackdrop` và `Water.mat` của chunk), shader `Game/Terrain` (đường sương), test PlayMode.

Làm (hướng user chọn 2026-10-22; cách cụ thể Planner chốt trong brief):
1. **Biển nhuộm màu trời:** trong `Ocean.shader`, mặt biển (mặt trên) lerp dần về `_DayHorizon` (màu chân trời theo buổi, đã là linear sau 0033 — cùng giá trị sương nhận) theo góc nhìn xiên (fresnel) và theo khoảng cách, trước bước `MixFog`. Có hiệu lực chỉ khi `_DayCycleOn = 1`; `_DayCycleOn = 0` → kết quả như cũ.
2. **Sương chuyển mềm:** trong `Ocean.shader` và `Terrain.shader`, nắn hệ số sương tuyến tính bằng đường cong mềm ở hai đầu (vd smoothstep trên cùng khoảng 64–120) để không còn chỗ gãy độ dốc ở 120 m. `RenderSettings.fogStartDistance/EndDistance`, far clip không đổi.
3. Test PlayMode render (dựa trên `client/Assets/Game/Tests/Client/HorizonFogMatchPlayTests.cs`).

## Ngoài phạm vi
- Far clip 128, khoảng sương 64–120, bán kính nạp chunk; vẽ mặt biển tới chân trời thật (user chốt giữ 2026-10-22).
- Độ cao/góc camera trại demo; dải màu phẳng 0–12° dưới chân trời **vẫn còn** (hình học do camera cao 26 m) — task này chỉ làm nó liền màu với biển và trời, không làm nó thấp đi.
- Gradient trời phía trên chân trời (`SkyClouds.shader`), bảng màu 8 khoá `DaySky`, `DayCycleView.cs` (đã gửi `_DayHorizon` linear).
- `UnderwaterView`, `WorldEdgeFog`; màu nước khi camera dưới nước (mặt dưới mặt nước không nhuộm).
- `GhostBlock.shader`, `SelectionOverlay.shader`.
- Gợn sóng, lấp lánh, độ đục `Water.mat`, màu `_DeepColor/_ShallowColor`.

## Tiêu chí chấp nhận (kiểm chứng được)
Đo ở camera kiểu trại demo (cao 26 m trên mặt biển, nhìn ra biển thấy chân trời), 4 mốc bình minh / ngày / hoàng hôn / đêm; Executor đo số liệu **trước khi sửa** làm mốc so sánh, Planner ghi ngưỡng cụ thể trong brief.
- [ ] Biển nhuộm màu trời: điểm ảnh mặt biển ở khoảng cách ~90 m lệch màu sương (`RenderSettings.fogColor.linear`) ≤ một nửa độ lệch đo trước khi sửa, ở cả 4 mốc — kiểm bằng: test PlayMode render (`run_tests` qua MCP).
- [ ] Không còn gãy ở 120 m: theo một cột ảnh từ chân trời xuống qua mặt biển, độ đổi màu giữa hai hàng liền kề quanh hàng ứng với 120 m không nhảy (sai phân bậc hai tại đó nhỏ hơn đo trước khi sửa, ngưỡng trong brief) — kiểm bằng: test PlayMode render.
- [ ] Chân trời vẫn trùng màu: vật kín sương sát far clip ≈ trời ngay trên (≤ 2/255 mỗi kênh, như 0033) — kiểm bằng: `HorizonFogMatchPlayTests` vẫn xanh.
- [ ] `_DayCycleOn = 0`: mặt biển render như trước khi sửa ở vùng gần (< 64 m, chưa có sương) — kiểm bằng: test PlayMode so điểm ảnh.
- [ ] Mặt nước trong map nhìn gần (camera thấp, nhìn xuống) gần như không đổi; từ dưới nước nhìn lên không đổi — kiểm bằng: test PlayMode hoặc ảnh trước/sau trong brief.
- [ ] Các test cũ xanh (EditMode + PlayMode, gồm `DayCycleViewPlayTests`, `DayCycleWaterPlayTests`, `HorizonFogMatchPlayTests`); `Game.Core` không đổi — kiểm bằng: `run_tests` qua MCP.
- [ ] FPS điện thoại không giảm đáng kể (≥ 30 FPS Bench, như 7f) — kiểm bằng: Bench trên điện thoại (Planner quyết trong brief có cần đo không, vì chỉ thêm vài phép tính trong fragment shader).
- [ ] User xem SimView (Mac) cả 4 buổi: biển xa chuyển dần sang màu trời, không còn mép "màn sương", màu chấp nhận được — kiểm bằng: user xem (mức review `user xem`).

## Ràng buộc
- Unity 6000.4.8f1; Linear color space; URP 17.4.0. Hàm/macro URP dùng trong brief phải tra đúng bản 17.4 (Planner ghi `[docs]`/`[repo]` đường dẫn package).
- Unity Editor dùng chung qua MCP (0010); sau PlayMode `git checkout -- client/ProjectSettings/EditorSettings.asset`.
- Ghi giờ bắt đầu/kết thúc. Ước lượng **0,5 ngày-người** [chưa xác minh] (shader biển 0,15 · đường sương 2 shader 0,1 · test render + đo trước/sau 0,15 · user xem + chỉnh 0,1) — M1, tính trong dòng "Buổi trong ngày" 4–6 của roadmap.

## Quyết định đã chốt (ai chốt, khi nào)
- User 2026-10-22: biển nhuộm dần sang `_DayHorizon` theo buổi ở xa và góc xiên (fresnel); sương chuyển mềm, bỏ chỗ gãy ở mốc 120 m; giữ far clip 128, không tăng khoảng sương.
- Scene không có `DayCycleView` (`_DayCycleOn = 0`) giữ màu biển như cũ (background từ phiên Planner).
- User 2026-10-22 (theo đề xuất): sương mềm áp cho cả `Ocean.shader` và `Terrain.shader`, cùng một đường cong, ở mọi scene có sương (mọi scene có sương hiện đều có `DayCycleView`: Bench, SimBench, SimView, TerrainView; HashCheck tắt sương [repo grep]).
- User 2026-10-22 (theo đề xuất): mặt nước trong map (`Water.mat`) cũng nhuộm, chỉ mặt trên, cùng công thức với `OceanBackdrop`.

## Câu hỏi còn mở
- (không)
