# 0005 — Render pipeline của client: URP

> Trạng thái: Đề xuất
> Ngày: 2026-10-12

## Bối cảnh
- [0001](0001-client-unity.md): client Unity 6000.4.8f1; render pipeline phải quyết riêng trong M0.
- Game chạy trên PC và mobile ([vision](../00-vision.md)); voxel world, nhiều sinh vật; một người làm, không tự làm art (dùng asset có sẵn).

## Các lựa chọn
- **URP:** dành cho "mobile to high-end consoles and PCs" ([docs 6000.4](https://docs.unity3d.com/6000.4/Documentation/Manual/urp/urp-introduction.html)); hiệu quả trên GPU mobile ([docs 6000.4](https://docs.unity3d.com/6000.4/Documentation/Manual/choose-a-render-pipeline.html)). Có template Universal 3D ([docs](https://docs.unity3d.com/6000.4/Documentation/Manual/urp/creating-a-new-project-with-urp.html)).
- **Built-in:** manual 6000.4 vẫn gọi là "the default render pipeline" ([docs](https://docs.unity3d.com/6000.4/Documentation/Manual/render-pipelines.html)); Shader Graph chỉ còn sửa lỗi cho Built-in ([docs](https://docs.unity3d.com/6000.4/Documentation/Manual/choose-a-render-pipeline.html)); từ 6.5 bị deprecated ([upgrade guide 6.5](https://docs.unity.com/en-us/engine/6000.5/manual/upgrade-guides/upgrade-guide-unity65)) → phải chuyển sau, asset mua phải hợp với cả hai.
- **HDRP:** không xét vì game cần chạy trên mobile; giới hạn nền tảng của HDRP ở 6000.4 [chưa xác minh] — tra [trang chọn pipeline](https://docs.unity3d.com/6000.4/Documentation/Manual/choose-a-render-pipeline.html) nếu muốn xét lại.

## Quyết định
Đề xuất (chờ user Chốt): **URP**, tạo project từ template **Universal 3D**.

## Hệ quả
- [tech-stack.md](../04-tech/tech-stack.md): mục Client.
- Asset art/shader mua ngoài phải hỗ trợ URP — thêm vào tiêu chí chọn asset ở [art-style.md](../03-art-audio/art-style.md) khi viết phần đó.
- Thử hiệu năng voxel trên mobile (M1, [risks](../04-tech/risks.md)) dùng URP ngay từ đầu.
