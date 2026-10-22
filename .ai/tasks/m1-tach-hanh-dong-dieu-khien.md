# Task: M1 — tách lớp hành động điều khiển khỏi phím (0033)

> Model thực thi: Sonnet · medium  (AGENTS.md §10.2)
> Trạng thái: Xong 2026-10-22 — brief [0047](../briefs/0047-tach-lop-hanh-dong-dieu-khien.md) đã duyệt (review đầy đủ + user xem đạt). Quyết định gốc: [0033](../../design/decisions/0033-tach-lop-hanh-dong-dieu-khien.md) (Chốt).

## Mục tiêu
Mọi chỗ đọc chuột/bàn phím trong gameplay và UI người chơi đọc qua hành động (Input System 1.19 `InputAction`), không đọc thẳng `Keyboard.current.xKey` / `Mouse.current.xButton`. Phím mặc định y như hiện nay; chơi không đổi gì. Chuẩn bị cho bảng đổi phím M3 (luật đã chốt ở 0033 mục 2).

- **Pillar:** không thêm cơ chế chơi; phục vụ điều khiển dễ chịu cho mọi pillar ([00-vision](../../design/00-vision.md)), như 0032.
- **Thành phần:** client Unity. `Game.Core` không đổi.
- **Ước lượng:** 2–3 ngày-người, M1 (0033).

## Ngoài phạm vi
- Bảng đổi phím, lưu phím vào `settings.json`, xử lý trùng phím (M3, 0033 mục 2).
- Cảm ứng, tay cầm.
- Phím debug/bench (`DebugFlyCamera` F1–F3…, `WalkBench`, `TerrainBench`, `HashCheckView`, `Client/Editor/*`): giữ đọc thẳng.
- Đổi phím mặc định hay hành vi.

## Tiêu chí chấp nhận (kiểm chứng được)
- [x] Không còn `Keyboard.current` / `Mouse.current` / `kb.<x>Key` / `mouse.<x>Button` trong các file client thuộc phạm vi (SimView, FollowCamera, TimeDial, MinimapView, SettingsView, TerrainEditInput, PlayerCharacter, PlayerFarmActions nếu có). Kiểm bằng: test EditMode quét mã nguồn (như `HardcodedTextScanTests`).
- [x] Mỗi hành động có đúng phím mặc định như bảng hiện nay trong [controls-ui](../../design/02-gameplay/controls-ui.md). Kiểm bằng: test EditMode đọc bảng hành động, so với bảng mặc định.
- [x] Không có hai hành động trùng phím trong cùng ngữ cảnh (trừ cặp được liệt kê là cố ý). Kiểm bằng: test EditMode.
- [x] Đổi phím lúc chạy có tác dụng (chuẩn bị M3): test PlayMode `ApplyBindingOverride` đổi chuột trái ↔ phải cho "dùng công cụ" / "đặt khối" → gửi phím ảo (`InputSystem.AddDevice<Mouse>`, `QueueStateEvent`, như `FlyCameraMoveKeyPlayTests`) → hành động đúng chạy. — *Đã làm khác (brief 0047 QĐ 10):* phím ảo không tới được hành động khi Game View không focus, nên test kiểm `effectivePath` + control đã resolve sau override; phím thật do user thử.
- [x] Không vỡ gì: EditMode + PlayMode toàn bộ qua với kỳ vọng cũ, 0 lỗi console. User chơi thử trên Mac: mọi phím như trước.

## Ràng buộc
- Input System 1.19 (đã cài). API dùng: `InputActionAsset` / `InputActionMap` / `InputAction`, `ApplyBindingOverride`, `SaveBindingOverridesAsJson` (M3) — [docs](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.19/manual/ActionBindings.html). Planner tra lại docs 1.19 và ghi link vào brief.
- Ngữ cảnh = một `InputActionMap`; bật/tắt map theo bảng đang mở, thay cho các cờ "bảng nào đang mở" rải rác (nếu đơn giản hơn) — Planner chốt trong brief.
- Mỗi hành động có tên tiếng Anh (code tiếng Anh, user 2026-10-22); tên hiển thị trong M3 đi qua bảng chuỗi.

## Quyết định đã chốt (ai chốt, khi nào)
- M1 chỉ tách hành động, bảng đổi phím M3; lưu trong `settings.json`; trùng phím cùng ngữ cảnh → hỏi đổi chỗ; chỉ chuột + bàn phím, trừ debug, Esc cố định: user, 2026-10-22 (0033).
- Khai báo bằng asset mới `Assets/Game/Client/Input/GameControls.inputactions`, xoá asset template `Assets/InputSystem_Actions.inputactions` không dùng; làm ngay sau 0032, trước world 7i–7k; một brief Sonnet · medium, review đầy đủ + user xem: user, 2026-10-22.

## Câu hỏi còn mở
_(không)_
