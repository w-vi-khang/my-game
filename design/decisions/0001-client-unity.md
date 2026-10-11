# 0001 — Client dùng Unity

> Trạng thái: Chốt
> Ngày: 2026-10-11

## Bối cảnh
Cần chọn công nghệ cho client (phần người chơi chạy). Một người làm. Game có thể gồm nhiều thành phần khác ngoài client (server, công cụ, dịch vụ...) — xác định trong [04-tech/components.md](../04-tech/components.md).

## Quyết định
Client làm bằng Unity. Bản đã cài trên máy: 6000.4.8f1. Lựa chọn của chủ project.
Quyết định này CHỈ áp dụng cho client; công nghệ của các thành phần khác quyết định riêng.

## Hệ quả
- Thiết kế client trong `04-tech/` theo Unity; tính năng Unity phải tra docs đúng version 6000.4.
- Render pipeline chưa chọn → quyết định riêng trong M0.
