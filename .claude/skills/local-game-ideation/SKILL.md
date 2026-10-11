---
name: local-game-ideation
description: Brainstorm ý tưởng cho game (concept, cơ chế, cốt truyện, nhân vật, tính năng) rồi lọc theo pillars và sức một người làm, ghi kết quả vào design/. Dùng khi user muốn nghĩ ý tưởng, brainstorm, tìm concept hoặc "gợi ý thêm" cho bất kỳ phần nào của game.
---

# Game ideation

Chế độ sáng tạo ĐƯỢC BẬT trong skill này (AGENTS.local.md). Mọi ý tưởng của AI ghi `[ý tưởng]` cho tới khi user chấp nhận.

## Đầu vào cần có (thiếu thì hỏi tuần tự, mỗi lượt 1–2 câu, kèm đề xuất)
1. Brainstorm cho phần nào (concept tổng, cơ chế, cốt truyện, nhân vật, level...)?
2. Ràng buộc: đọc `design/00-vision.md` (pillars, người chơi, phạm vi, ràng buộc). Vision còn trống → hỏi user 1–2 câu về cảm giác muốn tạo cho người chơi trước, không brainstorm mù.

## Quy trình
1. **Phân kỳ:** đưa 5–8 ý tưởng khác nhau rõ rệt (không phải biến thể của nhau). Mỗi ý: 1–2 câu + pillar phục vụ + game tham khảo gần nhất (có link nếu nêu tên game cụ thể).
2. **Hội tụ:** chấm từng ý theo bảng: phục vụ pillar (0–2) | khác biệt (0–2) | khả thi cho một người (0–2, kèm ước lượng thô) | rủi ro chính. Không bịa số liệu thị trường; nếu cần thì tra và dẫn nguồn.
3. Đề xuất 1–2 ý tốt nhất kèm lý do; **user chọn**.
4. Ghi: ý được chọn vào đúng file design/ (đánh dấu `[ý tưởng]` cho tới khi user xác nhận nội dung), các ý còn lại vào `design/ideas.md` (trạng thái "để sau" hoặc "loại" + lý do). Thuật ngữ mới → `design/glossary.md`.
5. Chạy `python3 tools/check_design.py`.

## Công cụ
- `design/00-vision.md`, `design/ideas.md`, `design/glossary.md`
- `python3 tools/check_design.py --summary`

## Cấm
- Không tự chọn thay user. Không xoá ý tưởng cũ (chuyển vào ideas.md).
- Không sửa nội dung đã Chốt; nếu ý tưởng mâu thuẫn với phần đã Chốt → nêu rõ mâu thuẫn và hỏi.
- Không trình bày ý tưởng như sự thật đã kiểm chứng (vd "người chơi sẽ thích...").

## Tiêu chí hoàn thành
- [ ] Mỗi ý tưởng có pillar, chấm điểm, ước lượng khả thi.
- [ ] User đã chọn; ý được chọn ghi vào đúng file, ý còn lại nằm trong ideas.md.
- [ ] Thuật ngữ mới có trong glossary.
- [ ] `python3 tools/check_design.py` exit 0.
