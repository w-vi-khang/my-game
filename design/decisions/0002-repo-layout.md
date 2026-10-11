# 0002 — Thiết kế trước, cùng repo với các thành phần

> Trạng thái: Chốt
> Ngày: 2026-10-11

## Bối cảnh
Cần nơi lưu toàn bộ kế hoạch và thiết kế (ý tưởng → lộ trình) trước khi làm bất kỳ thành phần nào, AI hỗ trợ được và giữ nhất quán.

## Các lựa chọn
- Artifact/trang web: dễ chia sẻ, nhưng một trang, không có lịch sử thay đổi, kit ai-workflow-kit không hoạt động.
- Thư mục `design/` trong repo project (markdown + Mermaid): có git, kit kiểm chứng được, AI đọc trực tiếp khi làm từng thành phần.

## Quyết định
- `design/` là nguồn chuẩn cho mọi quyết định. Artifact chỉ dùng để trình bày (pitch, roadmap), tạo lại từ `design/`.
- Client Unity nằm trong `client/`.
- Mặc định: mỗi thành phần khác (nếu có) là một thư mục cấp gốc của repo này (vd `server/`, `tools/`). Thành phần nào cần repo riêng → ghi quyết định mới.

## Hệ quả
- Ảnh/model/âm thanh dùng Git LFS (`.gitattributes`).
- `.gitignore` cho Unity tính theo thư mục `client/`; thành phần mới thêm phần ignore riêng khi tạo.
