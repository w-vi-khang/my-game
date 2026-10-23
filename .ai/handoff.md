# Handoff — 2026-10-23

## Đang làm
Task `m1-world-7l-dieu-khien-pc` ([task contract](tasks/m1-world-7l-dieu-khien-pc.md)) — điều khiển PC theo 0034.
- 7l b xong: brief [0055](briefs/0055-dieu-khien-pc-b1-click-di-chuyen.md) (di chuyển, code 0ce293a, 17 phút) và [0056](briefs/0056-dieu-khien-pc-b2-camera.md) (camera, code f268ce9, 13 phút) **đã duyệt**. verify_run (Editor tắt): EditMode 532/532, PlayMode 196/196.
- Chưa ghi giờ 7l b (≈30 phút Executor) vào world-tech dòng 7l b + roadmap.
- `client/Assets/Game/Tests/Client/TerrainEditPlayTests.cs` có thay đổi dở từ phiên khác — không thuộc 7l, không commit.

## Phản hồi user sau khi chơi thử 7l b (2026-10-23)
1. Số: `sim_m1.json` player `cameraDistance` 14 → **9**, `cameraPitch` 55 → **10**; `PlayerSettings.lookSensitivity` mặc định 0,15 → **0,3** (lưu ý settings.json cũ của user có thể đã lưu 0,15 — cân nhắc nâng formatVersion 2 → 3 để đặt lại). Sửa test liên quan (SimConfigTests, PlayerSettingsTests, CameraPcPlayTests, SettingsPlayTests…).
2. **Xoay camera quan sát khi đang xem** (user chọn): giữ chuột giữa + di chuột để xoay; hướng mới được giữ cho lần xem sau (tới khi đặt lại camera). Quyết định mới của user → ghi vào design (0034 mục 4 / 0029, controls-ui, world-tech 7j/7l) trước khi viết brief.
3. **Lỗi:** đặt camera quan sát rồi bấm xem (V / nút) → "không thấy gì cả". Chưa rõ nguyên nhân (có thể do camera đặt từ góc mới 55°/14 hoặc lỗi khác) → Planner điều tra (Opus · high hoặc bug-fix) trước khi viết brief.
4. Đúng lịch, KHÔNG sửa trong bản sửa: chế độ mặc định (chuột phải không đào), camera là vật phẩm trong túi, vòng tầm thao tác khi chuột phải ngoài 10 ô → 7l c; chữ hướng dẫn trên màn hình vào Cài đặt → 7l e.
5. User: "sửa hết rồi mới nhờ tôi chơi thử" → không mời user xem sau từng bước nữa; lần xem tiếp theo sau 7l e.

## Bước tiếp
1. Điều tra lỗi 3, ghi quyết định 2 vào design, viết brief sửa **0057** (mục 1–3), giao Executor.
2. Sau đó brief 7l c (ghi chú: nối máu/mana vào client — AddMoved, rơi, ngập — chưa bước nào nhận, đưa vào 7l c; nút "Bỏ đánh dấu" vì phím Cancel đã bỏ ở 0055; công cụ đang tạm ở chuột phải; giữ chuột trái vào tường không chuyển Blocked — review 0055).

## Model đề xuất
Opus · high (điều tra lỗi + ghi quyết định), rồi viết brief.
