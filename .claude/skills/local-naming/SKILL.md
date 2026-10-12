---
name: local-naming
description: Đặt tên riêng cho game theo quy ước thống nhất — tên game, vùng đất/map, tộc, loài, phép, nhân vật, người chơi, vật phẩm — đề xuất phương án, kiểm trùng (trong game, glossary, game/thương hiệu có sẵn) và ghi vào design/01-story/naming.md. Dùng khi user muốn đặt tên, đổi tên, tìm tên game hoặc lập quy ước đặt tên.
---

# Đặt tên (tên game, vùng đất, loài, nhân vật, người chơi...)

## Đầu vào cần có (thiếu thì hỏi tuần tự, mỗi lượt 1–2 câu, kèm đề xuất)
1. Đặt tên cho nhóm nào (tên game, vùng đất/map, tộc, loài, phép, nhân vật, người chơi, vật phẩm)? Bao nhiêu tên?
2. Nguyên tắc chung đã có chưa (mục "Nguyên tắc chung" trong `design/01-story/naming.md`): ngôn ngữ gốc (tiếng Việt / tiếng Anh / ngôn ngữ tự tạo), thị trường mục tiêu, cảm giác (tông giọng thế giới). Chưa có → hỏi và chốt nguyên tắc trước khi đặt tên lẻ.
3. Thông tin của thứ cần đặt tên: vd loài thì tộc, nguyên tố, nội tại, map; vùng đất thì khí hậu, câu chuyện.

## Quy trình
1. Đọc `design/01-story/naming.md`, `design/glossary.md`, `design/01-story/world.md`, và phần liên quan (vd mục loài/phép trong `design/02-gameplay/mechanics.md`).
2. Nếu nhóm chưa có quy ước trong bảng "Quy ước theo nhóm" → đề xuất quy ước trước (cách ghép tên, gốc từ/âm tiết, hậu tố theo tộc/nguyên tố, độ dài tối đa), user đồng ý rồi mới đặt tên lẻ. Tên cùng tộc/nguyên tố nên nhận ra được là cùng nhóm.
3. Đề xuất 3–5 phương án mỗi tên, kèm ý nghĩa/gốc. Ghi chú đọc được trên cả PC và mobile (ngắn, không ký tự khó gõ).
4. Kiểm trùng:
   - Trong game: `python3 .claude/skills/local-naming/scripts/check_names.py --candidates "A,B,C"` (trùng/gần giống tên đã có, trùng thuật ngữ glossary, quá dài).
   - **Tên game** (và tên có thể thành thương hiệu): tra web — store (Steam, App Store, Google Play), tên game có sẵn, nhãn hiệu nếu tra được. Ghi kết quả kèm link `[docs]`; không tra được → `[chưa xác minh]`, không khẳng định "chưa ai dùng".
5. Ghi vào bảng "Danh sách tên" (trạng thái `đề xuất` cho tới khi user chọn → `dùng`; tên bị bỏ → `bỏ`, không xoá). Tên do AI đề xuất là `[ý tưởng]` trong phần mô tả.
6. Tên mới dùng ở nơi khác → cập nhật nơi đó (world, characters, plot, levels, mechanics) cho đúng cách viết; đổi tên → `grep -rn "<tên cũ>" design/` và sửa hết.
7. Chạy `check_names.py` (exit 0) và `python3 tools/check_design.py`.

## Công cụ
- `python3 .claude/skills/local-naming/scripts/check_names.py [--candidates "A,B"] [--similar 0.85] [--max-len 16]`
- `grep -rn "<tên>" design/`

## Cấm
- Không dùng tên riêng của game/tác phẩm có bản quyền (tên sinh vật, nhân vật, địa danh của game khác) — kể cả biến thể rất gần.
- Không khẳng định tên game "chưa bị dùng" hoặc "đăng ký nhãn hiệu được" khi chưa tra có nguồn; đây không phải tư vấn pháp lý.
- Không đổi tên đã có trạng thái `dùng` trong file Chốt mà không hỏi user.

## Tiêu chí hoàn thành
- [ ] Nhóm được đặt tên có quy ước trong bảng "Quy ước theo nhóm".
- [ ] Tên mới có trong "Danh sách tên" với nhóm, ý nghĩa, trạng thái.
- [ ] `check_names.py` exit 0; tên game có kết quả tra trùng kèm link hoặc ghi `[chưa xác minh]`.
- [ ] Nơi dùng tên đã cập nhật; `python3 tools/check_design.py` exit 0.
