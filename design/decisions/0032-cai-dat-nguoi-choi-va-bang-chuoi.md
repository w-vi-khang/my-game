# 0032 — Cài đặt người chơi tự chỉnh + bảng chuỗi ngôn ngữ (M1)

> Trạng thái: Chốt
> Ngày: 2026-10-22

## Bối cảnh
User (2026-10-22, sau khi xem sim 7f): các tham số như độ cao camera, tốc độ chuột, vùng hoạt động của đào/đặt… nên là tham số chỉnh được trong một bộ cài đặt; mọi chữ hiển thị cũng nên là tham số, để người chơi tải file ngôn ngữ gốc, dịch và nạp lại là đổi được toàn bộ ngôn ngữ. Hiện nay chữ HUD/bảng viết thẳng trong code client (tiếng Việt); số cảm giác điều khiển (camera, chuột) là hằng/trường trong component; luật game (tầm 8 ô, thời gian xới…) một phần ở `design/data/sim_m1.json`, một phần là hằng trong code (vd `PlayerRules.ToolReach`).

## Các lựa chọn
Planner đưa 3 câu hỏi, user chọn (2026-10-22):
1. **Cài đặt:** (a) M1, gọn — file cài đặt + bảng Cài đặt tối giản trong game ✔; (b) M1 chỉ file sửa tay; (c) để M2.
2. **Luật game** (tầm với, thời gian xới…): (a) chỉ dev chỉnh, nằm trong `design/data/*.json` ✔; (b) người chơi chỉnh được khi chơi đơn.
3. **Ngôn ngữ:** (a) bảng chuỗi từ M1, nạp file ngôn ngữ đặt trong thư mục game (local) ✔; (b) như (a) + upload lên server sau; (c) để sau M1.

## Quyết định
1. **Cài đặt người chơi** (client, M1, sau sim 7f): file cài đặt lưu trên máy + bảng **Cài đặt** tối giản trong game. Chỉ gồm thứ thuộc cảm giác / hiển thị, không gồm luật game. Danh sách cụ thể, chỗ lưu file, phím mở bảng: chốt trong task contract. Danh sách đề xuất (chưa chốt) ghi ở [controls-ui](../02-gameplay/controls-ui.md#cài-đặt-người-chơi--ngôn-ngữ).
2. **Luật game chỉ dev chỉnh:** mọi số luật đưa vào `design/data/*.json` (đọc bằng `ConfigLoader` như `sim_m1.json`), không có trong bảng Cài đặt. Hằng luật còn trong code (vd tầm với 8 ô — [0031](0031-tam-voi-moi-thao-tac-8-o.md)) chuyển sang JSON trong cùng task.
3. **Bảng chuỗi ngôn ngữ từ M1:** mọi chữ hiển thị đọc theo khoá từ file ngôn ngữ; game có sẵn file gốc tiếng Việt. Người chơi xuất được file gốc ra thư mục ngôn ngữ của game, dịch, đặt file mới vào đó rồi chọn ngôn ngữ trong Cài đặt. Thiếu khoá → dùng chữ gốc. Upload bản dịch lên server để người khác tải: **không** ở M1 (cần backend) → [ideas](../ideas.md).

## Hệ quả
- Thành phần: chỉ client Unity (không thêm thành phần mới); luật vẫn ở `Game.Core` + JSON.
- Ước lượng (số tạm): cài đặt 2–3 ngày-người; bảng chuỗi + chuyển chữ hiện có 2–4 → **M1 +4–7**, vượt quỹ M1 thêm (đã vượt, user chấp nhận ở 0028/0029). Cắt trước nếu cần: bảng Cài đặt trong game (giữ file sửa tay).
- Phải tra trước khi viết task contract: Unity Localization package có nạp file người chơi tự thêm lúc chạy không (ưu tiên giải pháp có sẵn) [chưa xác minh — tra docs 6000.4]; chỗ lưu file (`Application.persistentDataPath`) trên Mac / Android [chưa xác minh].
- Từ nay brief client mới: chữ hiển thị mới đi qua bảng chuỗi khi bảng chuỗi đã có (trước đó vẫn viết thẳng, chuyển một lượt trong task này).
- Thứ tự: làm sau sim 7f; trước hay sau world 7i–7k do user chọn khi mở task.
- Cập nhật: `design/05-roadmap.md` (M1), `design/02-gameplay/controls-ui.md` (Cài đặt, ngôn ngữ), `design/ideas.md` (upload bản dịch), `decisions/README.md`.
