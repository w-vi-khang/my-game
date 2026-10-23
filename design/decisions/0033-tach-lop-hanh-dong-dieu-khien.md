# 0033 — Tách lớp hành động điều khiển khỏi phím (M1), đổi phím vẫn M3

> Trạng thái: Chốt
> Ngày: 2026-10-22
> Thay đổi bởi [0034](0034-dieu-khien-pc-click-di-chuyen.md) (user, 2026-10-23): bảng đổi phím (mục 2) chuyển từ M3 về M1; luật mục 2 giữ nguyên.

## Bối cảnh
User (2026-10-22, khi duyệt brief 0046): tách thao tác của người dùng (phím, chuột) khỏi hành động trong game; người chơi gán chức năng cho phím khác, đổi chuột trái/phải cho nhau, miễn là không gây mâu thuẫn khi chơi. Đổi phím đang xếp ở M3 ([controls-ui](../02-gameplay/controls-ui.md), 5–10 ngày-người). Code client hiện đọc phím trực tiếp (`kb.wKey`, `mouse.leftButton`…) ở 8 file, khoảng 50 chỗ [repo, grep 2026-10-22]. Asset `Assets/InputSystem_Actions.inputactions` của template có sẵn nhưng không được dùng [repo].

## Các lựa chọn
Planner hỏi, user chọn (2026-10-22):
1. **Milestone:** (a) M1 cả tách hành động + bảng đổi phím, M1 +3,5–5,5; (b) M1 chỉ tách hành động, M1 +2–3, bảng đổi phím vẫn M3 ✔; (c) giữ M3.
2. **Chỗ lưu phím đã đổi:** trong `settings.json` ✔ / file riêng.
3. **Trùng phím trong cùng ngữ cảnh:** hỏi đổi chỗ ✔ / tự đổi chỗ / chặn.
4. **Phạm vi:** chuột + bàn phím, trừ phím debug, Esc cố định ✔ / gồm cả cảm ứng.

## Quyết định
1. **M1 (client, sau khi xong 0032):** mọi chỗ đọc chuột/bàn phím trong gameplay và UI người chơi đọc qua **hành động** của Input System 1.19 (bảng hành động theo ngữ cảnh: đi lại/công cụ, bảng Túi/Kho, bản đồ, vòng thời điểm, Cài đặt), không đọc thẳng phím. Phím mặc định giữ y như hiện nay. Ước lượng 2–3 ngày-người.
2. **M3 (giữ):** bảng đổi phím trong Cài đặt. Đã chốt trước cho M3:
   - Lưu phím đã đổi trong `settings.json` (chuỗi của `SaveBindingOverridesAsJson`, [docs](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.19/manual/ActionBindings.html)), có nút "Mặc định".
   - Gán phím đã dùng cho hành động khác **trong cùng ngữ cảnh** → báo "phím đã dùng cho X", chọn Đổi chỗ (X nhận phím cũ) hoặc Huỷ. Đổi chuột trái/phải là một lần đổi chỗ. Hai ngữ cảnh không bao giờ cùng lúc được trùng phím.
   - Chỉ chuột + bàn phím; phím debug/bench (F1–F3…) không vào bảng; Esc cố định.
3. Cảm ứng không thuộc quyết định này (bố cục nút cảm ứng vẫn M3).

## Hệ quả
- Thành phần: chỉ client Unity; `Game.Core` không đổi (Input System là API Unity).
- [05-roadmap](../05-roadmap.md): M1 +2–3, vượt quỹ M1 thêm; M3 "Đổi phím…" giữ 5–10 (không trừ, để dư).
- [controls-ui](../02-gameplay/controls-ui.md), [architecture](../04-tech/architecture.md): ghi M1 tách hành động, M3 đổi phím theo luật ở mục 2.
- Task contract: `.ai/tasks/m1-tach-hanh-dong-dieu-khien.md`.
