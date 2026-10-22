# Task: M1 — sửa lệch màu sương ↔ bầu trời ở chân trời (khối phẳng bên trái, sao không xuống tới chân trời)

> Model thực thi: Sonnet · medium  (AGENTS.md §10.2)
> Trạng thái: user xác nhận 2026-10-22. Brief [0033](../briefs/0033-sua-mau-chan-troi.md). Skill: `bug-fix`. Lỗi user báo 2026-10-22 khi xem brief 0031/0032; có từ brief 0023 (world 7f).

## Hiện tượng (user báo 2026-10-22)
Scene SimView (MapsV3), camera trại demo trên đồng bằng Map 1 nhìn về +z, ngẩng lên thấy chân trời:
1. Ban ngày: phía trái, trên mặt biển xa có một mảng phẳng xanh nhạt hình thang, mép trên cạnh cứng, khác màu trời.
2. Ban đêm: cùng vùng đó là khối tối che trời; sao bên trái không phủ xuống tới chân trời (bên phải có đồi che nên không lộ).

## Nguyên nhân (Planner chẩn đoán 2026-10-22)
- Project ở Linear color space [run: `PlayerSettings.colorSpace = Linear`].
- `DayCycleView.Apply` ghi cùng một màu chân trời `s.horizon` theo hai đường ([repo] `client/Assets/Game/Client/DayCycleView.cs:115-118`):
  - sương: `RenderSettings.fogColor = s.horizon` → Unity đổi gamma → linear khi đưa lên shader [run: `unity_FogColor` = (0,01; 0,01; 0,04) = `fogColor.linear`, trong khi `fogColor` = (0,087; 0,115; 0,206)];
  - bầu trời: `Shader.SetGlobalColor("_DayHorizon", s.horizon)` → **không** đổi [docs: https://docs.unity3d.com/6000.4/Documentation/ScriptReference/Shader.SetGlobalColor.html — "unlike Material.SetColor, this function doesn't do color space conversion"]; [run: `_DayHorizon` = (0,087; 0,115; 0,206)].
- Vì vậy vật ở xa bị sương phủ kín (mặt nước trong map, đáy biển, đồi xa) có màu khác phần trời dưới chân trời của skybox `Game/SkyClouds` (trời dưới chân trời = màu chân trời, không sao — [repo] `client/Assets/Game/Client/Shaders/SkyClouds.shader:113-120`). Ranh giới cứng là chỗ far clip 128 cắt mặt nước [run: tia qua mép khối chạm mặt nước ở depth = 128]. Ban đêm: khối (22,29,52) sRGB so với trời (83,95,124) [run, đo điểm ảnh].
- Loại trừ [run]: tắt `Ocean` (OceanBackdrop) → khối vẫn còn; ẩn mặt nước trong chunk (`Water.mat`) → khối vẫn còn, chỉ hạ mép theo đáy biển. `WorldEdgeFog` không liên quan (`_EdgeFogBlend` = 0 ở giữa map).
- Trước 0023, skybox lấy màu từ thuộc tính material (Unity đổi sang linear như sương) nên hai bên khớp — đúng ý thiết kế "màu chân trời trùng màu sương để biển tan vào trời" ([repo] `SkyClouds.shader:2`, [0009](../../design/decisions/0009-dai-duong-va-suong-mu-o-ria-map.md); world-tech dòng 209 "skybox dưới chân trời cùng màu sương").
- Thử cách sửa lúc chạy (không sửa file), camera trại demo ngẩng 8°, ngày (pha 0,5) và đêm: gửi màu `_Day*` dạng `.linear` → khối biến mất cả ngày lẫn đêm, đêm tối hẳn, sao xuống sát chân trời [run, ảnh `horizon-diag/diag_{day,night}_{before,A1}.png` trong thư mục ghi chú phiên].

## Mục tiêu
Màu sương trên cạn và màu bầu trời ở chân trời trùng nhau ở mọi pha trong ngày → vật xa tan vào trời, không còn mảng/khối cạnh cứng; ban đêm sao phủ tới chân trời.

Pillar: P1 "Hồi sinh để sưu tầm" ([00-vision](../../design/00-vision.md)) qua world 7f — buổi trong ngày là điều kiện môi trường sống ([0024](../../design/decisions/0024-buoi-trong-ngay-va-chon-thoi-diem.md)), người chơi phải nhìn ra buổi. Đây là sửa lỗi của 7f, không phải tính năng mới.

Thành phần phụ trách: **client Unity** — `Game.Client.DayCycleView` (test PlayMode).

Làm (cách A1, user chốt 2026-10-22):
1. `DayCycleView.Apply` gửi các màu bầu trời `_DayZenith`, `_DayHorizon`, `_DayCloud`, `_DayCloudShade`, `_DaySunColor`, `_DayMoonColor` dạng `Color.linear` (cùng cách Unity đổi màu sương). `RenderSettings.fogColor` giữ nguyên `s.horizon`. Bảng màu khoá trong `DaySky` giữ nguyên (giá trị gamma, như màu material cũ).
2. Test PlayMode: biến global `_DayHorizon` bằng `RenderSettings.fogColor.linear` (cùng giá trị sương nhận) ở pha ngày và đêm; và một test hình ảnh: điểm ảnh vật bị sương phủ kín sát far clip ≈ điểm ảnh trời ngay trên nó.

## Ngoài phạm vi
- Far clip, khoảng sương 64–120, bán kính nạp chunk; vẽ mặt biển tới chân trời thật.
- Sao/mặt trăng dưới chân trời (ngưỡng `above` trong shader), mật độ sao, bảng màu 8 khoá của `DaySky`.
- `UnderwaterView`, `WorldEdgeFog`, `OceanBackdrop`, `Ocean.shader`, `Terrain.shader` (đều đúng — màu sương và màu nền camera cùng đi qua đường có đổi màu).
- Scene không có `DayCycleView` (dùng thuộc tính material — đã đúng).

## Tiêu chí chấp nhận (kiểm chứng được)
- [ ] `_DayHorizon` (và các màu `_Day*` khác) = `.linear` của màu `DaySky` tương ứng; `RenderSettings.fogColor` = `s.horizon` như cũ — kiểm bằng: test PlayMode (`run_tests` qua MCP).
- [ ] Ban ngày và ban đêm, ở camera trại demo ngẩng nhìn chân trời trong SimView: điểm ảnh vật bị sương phủ kín và trời ngay trên mép far clip lệch ≤ 2/255 mỗi kênh — kiểm bằng: test PlayMode render.
- [ ] Các test cũ hết đỏ (EditMode + PlayMode, gồm `DayCycleViewPlayTests`, `DayCycleWaterPlayTests`, `TimeDialPlayTests`); `Game.Core` không đổi — kiểm bằng: `run_tests` qua MCP.
- [ ] User xem SimView (Mac): ngày không còn mảng xanh phẳng, đêm không còn khối tối, sao xuống tới chân trời; màu trời các buổi (bình minh, ngày, hoàng hôn, đêm) vẫn chấp nhận được — kiểm bằng: user xem (mức review `user xem`). Trời sẽ đậm hơn hiện nay một chút (về đúng màu trước 0023).

## Ràng buộc
- Unity 6000.4.8f1; Linear color space; URP 17.4.0.
- Unity Editor dùng chung qua MCP (0010); sau PlayMode `git checkout -- client/ProjectSettings/EditorSettings.asset`.
- Ghi giờ bắt đầu/kết thúc. Ước lượng **0,25 ngày-người** (M1, sửa lỗi world 7f — tính trong dòng "Buổi trong ngày" 4–6 của roadmap).

## Quyết định đã chốt (ai chốt, khi nào)
- User 2026-10-22: sửa theo A1 (bầu trời gửi màu dạng linear), không theo A2 (đổi màu sương sang gamma, giữ trời hiện tại).

## Câu hỏi còn mở
- (không)
