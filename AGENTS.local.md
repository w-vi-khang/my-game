# Quy tắc riêng của my-game

## Giai đoạn & phạm vi project
- Hiện tại là **M1 — prototype client tối giản** (user chuyển 2026-10-13). Chỉ làm hạng mục trong [0007](design/decisions/0007-m1-toi-gian.md) và hai kế hoạch prototype ([world-tech](design/04-tech/world-tech.md#kế-hoạch-prototype-m1), [simulation-tech](design/04-tech/simulation-tech.md#kế-hoạch-prototype-m1)). KHÔNG làm backend, game server hay hạng mục M2+; việc ngoài M1 → ghi vào roadmap/ideas, không tự làm.
- Code client theo [nền tảng code M1](design/04-tech/architecture.md#nền-tảng-code-m1): `Game.Core` là C# thuần (No Engine References) + test EditMode; mô phỏng dùng số nguyên; sinh địa hình trong Burst `FloatMode.Deterministic`; cấu hình JSON trong `design/data/`.
- Ghi giờ thực tế từng hạng mục M1 so với ước lượng ([roadmap](design/05-roadmap.md#kiểm-tra-phạm-vi)) — sau 4–6 hạng mục tính lại hệ số.
- **Ngoại lệ** ([0006](design/decisions/0006-cong-cu-kiem-tra-asset.md), Chốt 2026-10-12): được viết **công cụ của quy trình art** trong `tools/` (kiểm tra asset, kiểm/tạo ảnh concept) trước M1. Không phải code game; không tạo project Unity.
- **Unity chỉ là client**, không phải toàn bộ project. Không mặc định mọi tính năng chạy trong Unity: mỗi tính năng phải ghi thành phần phụ trách; thành phần mới chỉ thêm qua `design/04-tech/components.md` + quyết định trong `design/decisions/`.
- Thiết kế kỹ thuật đi từ cấp hệ thống (thành phần, ranh giới, dữ liệu) xuống từng thành phần.

## Sáng tạo (nới §1.5 cho project này)
- Trong `design/`, được chủ động đề xuất ý tưởng khi user brainstorm hoặc khi viết phần thiết kế còn trống — nhưng mọi đề xuất của AI phải đánh dấu `[ý tưởng]` cho tới khi user chấp nhận.
- KHÔNG tự đổi nội dung đã có trạng thái **Chốt**. Muốn đổi → đề xuất, user đồng ý → ghi quyết định mới trong `design/decisions/`.
- Ý tưởng bị loại hoặc để sau → chuyển vào `design/ideas.md`, không xoá.

## Trạng thái tài liệu
- Mỗi file trong `design/` có dòng `> Trạng thái: Nháp | Đang xem xét | Chốt` ngay dưới tiêu đề. Cập nhật bảng trạng thái ở `design/README.md` khi đổi.
- Chỉ user được chuyển một file sang **Chốt**.

## Nhất quán
- Dùng thuật ngữ trong `design/glossary.md`. Thuật ngữ mới → thêm vào glossary trong cùng lần sửa.
- Sửa một phần → liệt kê các phần bị ảnh hưởng (gameplay ↔ economy ↔ progression ↔ story ↔ thành phần/tech ↔ roadmap) và cập nhật, hoặc hỏi user nếu ảnh hưởng tới phần đã Chốt.
- Sơ đồ dùng Mermaid trong markdown (không dùng ảnh) để sửa và diff được.

## Phạm vi cho một người làm
- Mỗi tính năng/nội dung mới phải có ước lượng công (ngày-người) và thuộc một milestone. Nếu tổng ước lượng vượt quỹ thời gian trong roadmap → cảnh báo user và đề xuất cắt, không lặng lẽ thêm.
- Ưu tiên giải pháp có sẵn (Unity/asset store cho client, dịch vụ có sẵn cho server/backend) hơn tự làm, khi không ảnh hưởng pillar — ghi rõ là đề xuất, kèm chi phí có nguồn.
- Mỗi thành phần thêm vào là thêm việc dài hạn cho một người: phải nêu vì sao cần và có cách không cần nó không.

## Nguồn
- Game tham khảo, số liệu thị trường, tính năng/giới hạn/giá của công nghệ và dịch vụ: phải có link `[docs]`. Tính năng Unity phải đúng version 6000.4 — tra docs, không dựa vào trí nhớ.
