# CLAUDE.md — my-game

Quy tắc chung, skill và hooks đến từ plugin `ai-workflow-kit` (khai báo trong `.claude/settings.json`).
File này chỉ chứa thông tin RIÊNG của project.

- Cấu hình kiểm chứng + checklist: @.ai/project.json
- Quy tắc riêng: @AGENTS.local.md
- Mục lục thiết kế và trạng thái từng phần: @design/README.md

## Tổng quan
- Project game gồm **nhiều thành phần**. **Client** làm bằng Unity (6000.4.8f1 đã cài trên máy); các thành phần khác (server, công cụ, dịch vụ...) **chưa xác định** — xem `design/04-tech/components.md`.
- **Một người làm** (solo).
- **Giai đoạn hiện tại: M1 — prototype client tối giản** (từ 2026-10-13, user; phạm vi: [0007](design/decisions/0007-m1-toi-gian.md)). M0 xong.

## Cấu trúc
```
design/        kế hoạch & thiết kế — nguồn chuẩn duy nhất cho mọi quyết định
  decisions/   quyết định đã chốt (mỗi quyết định một file)
  ideas.md     kho ý tưởng chưa dùng
tools/         script kiểm tra (check_design.py)
client/        client Unity (M1 — template Universal 3D, URP)
<khác>/        thành phần khác, mỗi thành phần một thư mục (khi đã xác định trong components.md)
```
