---
name: local-gdd-section
description: Viết hoặc cập nhật một phần tài liệu thiết kế game trong design/ (vision, cốt truyện, nhân vật, thế giới, core loop, cơ chế, UI, tiến trình, kinh tế, level, art, âm thanh, tech stack, kiến trúc, sơ đồ Mermaid) và giữ nhất quán với các phần liên quan. Dùng khi user muốn thiết kế hoặc chỉnh một phần cụ thể của game.
---

# Viết một phần thiết kế (GDD section)

Phần có skill chuyên thì dùng skill đó: cốt truyện/nhân vật/nhiệm vụ → `local-game-story`; map/level → `local-level-design`; con số cân bằng (chỉ số, tỉ lệ, kinh tế, tiến trình) → `game-balance` (kit). Skill này dùng cho các phần còn lại và cho quy tắc (không phải con số) của economy/progression.

## Đầu vào cần có (thiếu thì hỏi tuần tự, mỗi lượt 1–2 câu, kèm đề xuất)
1. Phần nào (file nào trong `design/`)? Mục tiêu lần này: viết mới, mở rộng hay sửa?
2. Thông tin user đã có trong đầu cho phần đó — hỏi trước khi tự đề xuất.

## Quy trình
1. **Đọc bối cảnh:** `design/00-vision.md` (pillars), `design/glossary.md`, file đích, và các file trong dòng "Liên quan" của file đích. Đọc `design/decisions/` nếu phần này có quyết định liên quan.
2. **Hỏi** những gì chưa đọc được từ tài liệu, theo thứ tự các mục `##` trong file đích (bao quát → chi tiết). Đề xuất của AI ghi `[ý tưởng]`.
3. **Viết:** giữ nguyên cấu trúc mục của file; thay `_(chưa có)_` bằng nội dung; bảng cho dữ liệu có cấu trúc; sơ đồ bằng Mermaid; tính năng/nội dung có thành phần phụ trách (client Unity, server...), ước lượng công (ngày) và milestone. Tính năng cần thành phần chưa có trong `04-tech/components.md` → nêu ra và hỏi, không tự thêm.
4. **Tác động chéo:** liệt kê phần khác bị ảnh hưởng (tên, số liệu, cơ chế, ước lượng roadmap). Phần Nháp → cập nhật luôn; phần Chốt → hỏi user, nếu đồng ý thì ghi quyết định mới trong `design/decisions/`.
5. Đặt trạng thái file (Nháp hoặc Đang xem xét — KHÔNG tự đặt Chốt), cập nhật bảng ở `design/README.md`, thuật ngữ mới vào glossary.
6. Chạy `python3 tools/check_design.py`; đối chiếu checklist trong `.ai/project.json`.

## Công cụ
- `design/README.md` (mục lục, thứ tự làm), `design/decisions/_template.md`
- `python3 tools/check_design.py --summary`
- Công nghệ/dịch vụ của từng thành phần: tra docs đúng version (client Unity 6000.4) bằng skill `dependency-docs-lookup` của kit, dẫn link.

## Cấm
- Không đổi nội dung Chốt khi chưa có quyết định mới. Không tự đặt trạng thái Chốt.
- Không thêm tính năng ngoài yêu cầu; ý hay ngoài phạm vi → `design/ideas.md`.
- Không chép ảnh/văn bản có bản quyền vào repo; tham khảo bằng link.

## Tiêu chí hoàn thành
- [ ] Mọi mục được yêu cầu của file đích đã có nội dung hoặc ghi rõ còn chờ gì.
- [ ] Mỗi cơ chế/tính năng gắn pillar, có ước lượng và milestone.
- [ ] Tác động chéo đã liệt kê và xử lý (cập nhật hoặc đã hỏi user).
- [ ] Trạng thái file + bảng README + glossary đã cập nhật.
- [ ] `python3 tools/check_design.py` exit 0; checklist trong .ai/project.json đã đối chiếu.
