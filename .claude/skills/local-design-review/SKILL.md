---
name: local-design-review
description: Rà soát toàn bộ thiết kế game trong design/ — mâu thuẫn giữa các phần, lỗ hổng, tính năng không phục vụ pillar, phình phạm vi, mức sẵn sàng chuyển milestone (vd M0 → M1 bắt đầu code). Dùng khi user muốn review, kiểm tra nhất quán, tìm mâu thuẫn hoặc hỏi đã sẵn sàng làm bước tiếp chưa.
---

# Rà soát thiết kế

## Đầu vào cần có
1. Phạm vi rà soát: toàn bộ hay một nhóm phần? Mục đích: rà định kỳ hay kiểm tra sẵn sàng milestone nào?

## Quy trình
1. Chạy `python3 tools/check_design.py --summary` — lỗi cấu trúc báo trước.
2. Đọc các file trong phạm vi. Đối chiếu theo các trục:
   - **Pillar:** mỗi cơ chế/tính năng phục vụ pillar nào; cái không phục vụ → ứng viên cắt.
   - **Nhất quán:** tên gọi (so glossary), số liệu (economy ↔ progression ↔ levels), cốt truyện ↔ level ↔ cơ chế, tech ↔ cơ chế.
   - **Thành phần:** mỗi tính năng có thành phần phụ trách; ranh giới client/server hợp lý (chống gian lận, chơi offline); có thành phần nào thừa hoặc thiếu so với tính năng.
   - **Lỗ hổng:** phần "phải có" còn trống; cơ chế thiếu quy tắc/trường hợp biên; màn hình thiếu trong luồng UI.
   - **Phạm vi:** tổng ước lượng so với roadmap; mục "không làm" có bị vi phạm.
   - **Sẵn sàng milestone:** đối chiếu tiêu chí xong của milestone trong roadmap.
3. Báo cáo theo mức độ: 🔴 chặn (mâu thuẫn, thiếu thứ bắt buộc cho milestone) · 🟡 nên sửa · 🟢 gợi ý. Mỗi phát hiện kèm vị trí `path:line` hoặc mục, và đề xuất sửa.
4. KHÔNG tự sửa. Hỏi user muốn sửa mục nào → dùng skill `local-gdd-section` để sửa.

## Công cụ
- `python3 tools/check_design.py --summary`
- Checklist trong `.ai/project.json`

## Cấm
- Không sửa tài liệu trong lúc review. Không đánh giá "vui/không vui" như sự thật — chỉ nêu rủi ro và cách kiểm chứng (prototype).

## Tiêu chí hoàn thành
- [ ] Đã chạy check_design.py và ghi kết quả.
- [ ] Mỗi trục đối chiếu có kết luận (kể cả "không thấy vấn đề").
- [ ] Mỗi phát hiện có vị trí và mức độ.
- [ ] Có kết luận sẵn sàng/chưa sẵn sàng cho milestone được hỏi, kèm lý do.
