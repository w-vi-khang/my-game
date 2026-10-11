---
name: local-game-roadmap
description: Lập hoặc cập nhật lộ trình phát triển game trong design/05-roadmap.md — milestone có tiêu chí xong đo được, ước lượng công, phụ thuộc, rủi ro, sơ đồ gantt — và kiểm tra phạm vi có vừa sức một người làm. Dùng khi user hỏi về lộ trình, kế hoạch, milestone, ước lượng, deadline hoặc phạm vi.
---

# Lộ trình game (solo)

## Đầu vào cần có (thiếu thì hỏi tuần tự, mỗi lượt 1–2 câu, kèm đề xuất)
1. Quỹ thời gian: bao nhiêu giờ/tuần, có mốc mong muốn không?
2. Kỹ năng mạnh/yếu (code, art, âm thanh, level design) — ảnh hưởng ước lượng và chọn nguồn asset.

## Quy trình
1. Đọc `design/05-roadmap.md`, `design/00-vision.md` (phạm vi, "không làm"), `design/02-gameplay/mechanics.md`, `design/02-gameplay/levels.md`, `design/04-tech/components.md`, `design/04-tech/risks.md`.
2. Gom hạng mục từ các bảng cơ chế/level/art (có ước lượng). Hạng mục thiếu ước lượng → đề xuất ước lượng `[ý tưởng]` kèm cách tính, hỏi user.
3. Hạng mục ghi theo thành phần (client Unity, server, công cụ...); công sức dựng và vận hành mỗi thành phần (hosting, bảo trì) cũng là hạng mục. Xếp vào milestone theo thứ tự giảm rủi ro: rủi ro lớn nhất (core loop có vui không, kỹ thuật chưa chắc) phải được kiểm chứng ở M1/M2.
4. **Kiểm tra phạm vi:** tổng ước lượng × 1.5 (hệ số dự phòng cho người làm một mình — nêu rõ đây là giả định) so với quỹ thời gian. Vượt → cảnh báo và đề xuất cắt theo ưu tiên (có thì tốt → nên có), không tự cắt.
5. Cập nhật bảng milestone, danh sách công việc, sơ đồ Mermaid `gantt`. Đổi phạm vi lớn → ghi quyết định trong `design/decisions/`.
6. Chạy `python3 tools/check_design.py`.

## Công cụ
- `design/05-roadmap.md`, `design/decisions/_template.md`
- `python3 tools/check_design.py --summary` (xem phần nào còn trống trước khi ước lượng)

## Cấm
- Không bịa ước lượng như sự thật: mọi số do AI đề xuất ghi `[ý tưởng]` và cách tính.
- Không tự cắt/thêm tính năng; chỉ đề xuất.
- Không đặt mốc ngày cụ thể khi user chưa cho quỹ thời gian.

## Tiêu chí hoàn thành
- [ ] Mỗi milestone có tiêu chí xong đo được và tổng ước lượng.
- [ ] Mọi hạng mục "phải có" nằm trong một milestone.
- [ ] Đã so tổng ước lượng với quỹ thời gian và báo kết quả cho user.
- [ ] Rủi ro lớn có milestone kiểm chứng sớm.
- [ ] `python3 tools/check_design.py` exit 0.
