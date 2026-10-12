---
name: local-level-design
description: Thiết kế map/level trong design/02-gameplay/levels.md — danh sách map, vùng và yêu cầu môi trường sống của từng loài, khối cốt truyện, phần sinh theo seed và phần sắp đặt tay, điều kiện mở map, phân bố loài/tài nguyên, nội dung tái sử dụng. Dùng khi user muốn thiết kế map, màn chơi, vùng, bố trí nội dung hoặc điều kiện mở map.
---

# Thiết kế map / level

## Đầu vào cần có (thiếu thì hỏi tuần tự, mỗi lượt 1–2 câu, kèm đề xuất)
1. Map nào (vision: dự kiến 3 map lúc đầu, thêm theo mùa)? Viết mới hay sửa?
2. Ý tưởng user đã có: khí hậu/địa hình, loài trở lại ở map này, câu chuyện của map.
3. Milestone của map (vd map của vertical slice M2).

## Quy trình
1. Đọc `design/00-vision.md`, `design/glossary.md`, `design/02-gameplay/levels.md`, `design/02-gameplay/mechanics.md` (sinh map theo seed & khối cốt truyện, môi trường sống, nhu cầu sinh vật, mở map), `design/02-gameplay/progression.md`, `design/01-story/plot.md`, `design/04-tech/tech-stack.md` (voxel, tìm đường).
2. Với mỗi map, viết bảng:
   - **Vùng** | địa hình/khối chủ đạo | loài có thể trở lại | yêu cầu môi trường sống chính | tài nguyên.
   - **Phần sắp đặt tay** (khối cốt truyện, đường đi tới chúng) và **phần sinh theo seed** — phần sinh theo seed chỉ mô tả luật/tham số, không vẽ tay.
   - **Điều kiện mở map** và điều kiện phá đảo map.
3. Kiểm ràng buộc:
   - Đường đi tới khối cốt truyện phải luôn tới được (sinh vật tìm đường được, không phá được tới khi phá đảo map).
   - Yêu cầu môi trường sống của loài có thể đạt được bằng tài nguyên trên map (hoặc ghi rõ nguồn từ map khác).
   - Nhiệm vụ trên map không bắt buộc tuần tự (P3).
   - Kích thước map hợp hiệu năng voxel mobile (rủi ro trong `design/04-tech/risks.md`) — ghi kích thước ước tính, đánh `[chưa xác minh]` tới khi prototype đo.
4. Sơ đồ: Mermaid `flowchart` cho quan hệ vùng ↔ điều kiện mở; không dùng ảnh. Đề xuất của AI ghi `[ý tưởng]`.
5. Ước lượng công mỗi map (sắp đặt, tham số sinh seed, thử nghiệm) + milestone; so với roadmap (`local-game-roadmap`).
6. Tác động chéo: loài/tài nguyên mới → mechanics, economy (`local-game-balance`); địa điểm truyện → `local-game-story`; thuật ngữ → glossary. Đặt trạng thái (không tự Chốt), cập nhật `design/README.md`, chạy `python3 tools/check_design.py`.

## Công cụ
- `python3 tools/check_design.py --summary`
- Prototype giấy/HTML để thử bố cục vùng — chỉ khi user yêu cầu (M0 chưa tạo project Unity).

## Cấm
- Không tạo project/scene Unity trong M0.
- Không vẽ tay phần đã quy định sinh theo seed; không thêm cơ chế mới cho map mà không hỏi user.
- Không khẳng định số liệu hiệu năng (kích thước map chạy được trên mobile) khi chưa đo.

## Tiêu chí hoàn thành
- [ ] Mỗi map có bảng vùng, phần sắp đặt tay / sinh theo seed, điều kiện mở và phá đảo.
- [ ] Ràng buộc ở bước 3 đã kiểm, ghi rõ chỗ chưa xác minh.
- [ ] Mỗi map có ước lượng công và milestone.
- [ ] Tác động chéo đã xử lý hoặc hỏi; trạng thái + README cập nhật; `python3 tools/check_design.py` exit 0.
