---
name: local-game-story
description: Viết hoặc cập nhật thế giới, nhân vật, cốt truyện của game trong design/01-story/ — bối cảnh và tông giọng, nhân vật, cốt truyện chính qua mảnh ký ức, nhiệm vụ chính/phụ theo từng map không bắt buộc tuần tự — và giữ nhất quán với cơ chế, map, glossary. Dùng khi user muốn thiết kế lore, nhân vật, cốt truyện, nhiệm vụ, đối thoại.
---

# Cốt truyện (thế giới, nhân vật, plot, nhiệm vụ)

## Đầu vào cần có (thiếu thì hỏi tuần tự, mỗi lượt 1–2 câu, kèm đề xuất)
1. Phần nào: thế giới (`world.md`), nhân vật (`characters.md`), cốt truyện/nhiệm vụ (`plot.md`)? Viết mới hay sửa?
2. Điều user đã có trong đầu (tông giọng, nhân vật, cái kết, game/truyện tham khảo) — hỏi trước khi đề xuất.
3. Map nào (mỗi map có câu chuyện riêng; cốt truyện chính xuyên mọi map qua mảnh ký ức).

## Quy trình
1. Đọc `design/00-vision.md` (pillars, nhất là P1 hồi sinh và P3 tự do trong khung truyện), `design/glossary.md`, cả 3 file `design/01-story/`, `design/02-gameplay/mechanics.md` (nhiệm vụ & mảnh ký ức, môi trường sống, khối cốt truyện), `design/02-gameplay/levels.md`.
2. Giữ ràng buộc cơ chế:
   - Nhiệm vụ chính/phụ mỗi map **không bắt buộc tuần tự** (P3) → mỗi nhiệm vụ ghi điều kiện mở, không giả định thứ tự.
   - "Nhiệm vụ" (cốt truyện) ≠ "lệnh làm việc" (nông trại).
   - Thành phần cốt truyện đặt trên map là **khối cốt truyện**, không phá/đặt được tới khi phá đảo map → mỗi địa điểm truyện ghi rõ là khối cốt truyện.
   - Loài trở lại nhờ môi trường sống → nếu truyện gắn với loài, nêu loài và map.
3. Viết theo cấu trúc mục sẵn có của file đích. Bảng cho dữ liệu có cấu trúc (nhân vật | vai trò | map | xuất hiện ở nhiệm vụ nào; nhiệm vụ | map | loại | điều kiện mở | phần thưởng | mảnh ký ức). Sơ đồ quan hệ/nhánh truyện bằng Mermaid (`flowchart`, `timeline`). Đề xuất của AI ghi `[ý tưởng]`.
4. Mỗi nhiệm vụ/nội dung mới có ước lượng công (ngày-người: viết + sắp đặt trên map) và milestone; tổng vượt roadmap → cảnh báo (`local-game-roadmap`).
5. Tác động chéo: tên riêng mới → glossary; nhiệm vụ cần cơ chế mới → mechanics (hỏi); địa điểm → levels (`local-level-design`); phần thưởng → economy (`local-game-balance`).
6. Đặt trạng thái file (không tự đặt Chốt), cập nhật bảng `design/README.md`, chạy `python3 tools/check_design.py`.

## Công cụ
- `python3 tools/check_design.py --summary`
- Kiểm tên riêng trùng/khác cách viết: `grep -rn "<tên>" design/`

## Cấm
- Không chép văn bản, nhân vật, thế giới có bản quyền; tham khảo bằng link và nêu điều lấy/không lấy.
- Không thêm cơ chế gameplay mới để phục vụ truyện mà không hỏi user.
- Không tạo nhiệm vụ bắt buộc làm theo thứ tự cố định (trái P3) trừ khi user yêu cầu.

## Tiêu chí hoàn thành
- [ ] Mục được yêu cầu đã có nội dung hoặc ghi rõ còn chờ gì.
- [ ] Mỗi nhiệm vụ có map, điều kiện mở, ước lượng, milestone; không nhiệm vụ nào giả định thứ tự bắt buộc.
- [ ] Tên riêng mới đã vào glossary; tác động chéo (levels, mechanics, economy) đã xử lý hoặc hỏi.
- [ ] Trạng thái file + bảng README cập nhật; `python3 tools/check_design.py` exit 0.
