# 0003 — Cần Server: backend và game server (PvP thời gian thực)

> Trạng thái: Chốt
> Ngày chốt: 2026-10-13 (user)
> Ngày: 2026-10-11

## Bối cảnh
- Game online: người chơi có tài khoản, world riêng; PvE trong world riêng, PvP qua ghép trận (user, 2026-10-11).
- Tua nhanh cần giờ server, không dùng đồng hồ thiết bị.
- Rút thưởng ra sinh vật có chỉ số cá thể riêng → có giá trị, cần chống gian lận.
- Một người làm: mỗi thành phần thêm vào là việc vận hành dài hạn.

"Server" gồm hai loại việc khác nhau:
- **Backend:** các dịch vụ gọi theo yêu cầu (tài khoản, lưu dữ liệu, giờ chuẩn, rút thưởng, ghép trận). Không chạy trận đấu.
- **Game server:** chạy mô phỏng trận đấu thời gian thực ở phía server, liên tục trong suốt trận.

## Các lựa chọn
**Backend** — bắt buộc cần, chỉ chọn cách làm:
- Dịch vụ có sẵn, vd Unity Gaming Services: có Authentication, Cloud Save, Cloud Code (code server stateless), Matchmaker ([docs](https://docs.unity.com/en-us/services/reference), [Authentication](https://docs.unity.com/authentication/en-us/manual/use-cases)). Ít việc vận hành; phụ thuộc nhà cung cấp; giá chưa tra.
- Server mã nguồn mở tự host hoặc thuê bản quản lý, vd Nakama: tài khoản, lưu trữ, ghép trận, multiplayer server-authoritative ([docs](https://heroiclabs.com/docs/), [GitHub](https://github.com/heroiclabs/nakama)). Tự chủ hơn; nhiều việc vận hành hơn nếu tự host; giá chưa tra.
- Tự viết: nhiều việc nhất, không đề xuất.

**Game server** — phụ thuộc hình thức PvP:
- **PvP bất đồng bộ** (đấu với đội hình đã lưu của người khác): không cần game server; trận mô phỏng tất định, backend kiểm lại kết quả. Rẻ nhất cho một người.
- **PvP thời gian thực, host là một người chơi (relay):** không cần game server riêng, nhưng host có thể gian lận.
- **PvP thời gian thực, server-authoritative:** cần game server + hosting, tốn công và chi phí nhất. Rủi ro nhà cung cấp: Unity đã ngừng Multiplay Game Server Hosting từ 31/03/2026 ([status.unity.com](https://status.unity.com/info_notices/362941)).

## Quyết định
Chốt (user, 2026-10-13):
1. Cần **Backend** từ M2, ưu tiên dịch vụ có sẵn. Chọn dịch vụ trong `04-tech/tech-stack.md` sau khi tra giá.
2. Cần **Game server**: user chọn PvP thời gian thực, server quyết định kết quả (2026-10-11). PvP là chế độ mở sau (cập nhật sau phát hành), có điều kiện tham gia. Hosting chọn khi tới lúc làm PvP. Build bằng Unity Dedicated Server từ chính project client ([docs 6000.4](https://docs.unity3d.com/6000.4/Documentation/Manual/dedicated-server-build.html)).

## Hệ quả
- [components.md](../04-tech/components.md): dòng Backend (cần) và Game server (chưa rõ); bảng ranh giới trách nhiệm.
- [mechanics.md](../02-gameplay/mechanics.md): tua nhanh, rút thưởng, chế độ nhiều người phụ thuộc backend.
- Logic chiến đấu cần tách khỏi hiển thị để chạy được trên game server (headless) — ảnh hưởng kiến trúc client ngay từ M1–M2 dù PvP làm sau.
- PvP bất đồng bộ và PvP do người chơi làm host: không chọn ([ideas](../ideas.md)).
- M1 prototype chạy không cần backend (giờ thiết bị, dữ liệu cục bộ); backend từ M2.
