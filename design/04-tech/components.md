# Thành phần hệ thống

> Trạng thái: Chốt
> Chốt: 2026-10-13 (user) — kèm yêu cầu **bảo mật và kiểm tra (validate) tốt để chống gian lận**. Chọn dịch vụ backend, hosting: quyết định mới khi tới lúc.
> Liên quan: [Kiến trúc](architecture.md) · [Công nghệ](tech-stack.md) · [Cơ chế](../02-gameplay/mechanics.md) · [Lộ trình](../05-roadmap.md)

Game KHÔNG chỉ là client Unity. File này xác định game cần những thành phần nào, mỗi thành phần chịu trách nhiệm gì, và cái nào thật sự cần cho từng milestone. Quyết định ở đây kéo theo phần lớn công sức của một người làm — cân nhắc kỹ.

## Câu hỏi xác định thành phần
<!-- Trả lời từng câu (có/không/chưa biết + lý do từ thiết kế). Mỗi "có" thường kéo theo một thành phần hoặc dịch vụ.
- Chơi offline hoàn toàn được không? Cần lưu tiến trình trên cloud / đồng bộ nhiều thiết bị?
- Có tài khoản, đăng nhập?
- Có chơi nhiều người (realtime / bất đồng bộ), bảng xếp hạng, bạn bè, chat?
- Có nội dung cập nhật sau phát hành (live ops, sự kiện, cấu hình từ xa)?
- Có thanh toán trong game (cần xác thực giao dịch phía server)?
- Có chống gian lận (logic quan trọng phải chạy ở server)?
- Cần công cụ nội bộ (editor level, nhập dữ liệu cân bằng, trang quản trị)?
- Cần thu thập số liệu (analytics), báo lỗi (crash report)?
- Có website / trang cộng đồng?
-->
| Câu hỏi | Trả lời | Hệ quả |
|---|---|---|
| Chơi offline được không? | Không — game online | Mất kết nối là không chơi được; backend phải ổn định |
| Lưu cloud / nhiều thiết bị? | Có — game online, PC và mobile | Tiến trình và world lưu ở backend; chỉ lưu phần khác với world sinh từ seed để giảm dung lượng |
| Tài khoản, đăng nhập? | Có | Backend |
| Chơi nhiều người? | Có — PvP thời gian thực qua ghép trận; PvE trong world riêng. Co-op đảo sandbox tạm bỏ | Backend (ghép trận) + Game server ([0003](../decisions/0003-server.md)) |
| Nội dung cập nhật sau phát hành? | Có — thêm map theo phiên bản/mùa | Phát qua bản cập nhật client, hay tải nội dung từ xa? Chưa rõ |
| Thanh toán trong game? | Có — tiền nạp | Backend xác thực giao dịch (biên lai từ cửa hàng ứng dụng/Steam) và giữ số dư tiền tệ |
| Chống gian lận? | **Có — bắt buộc** (user, 2026-10-13): bảo mật và kiểm tra tốt | Không tin client: mọi thứ có giá trị do backend/game server quyết định; dữ liệu client tính phải qua kiểm tra hợp lý ở backend. Cách làm: [kiến trúc](architecture.md#bảo-mật--chống-gian-lận) |
| Công cụ nội bộ? | Có — sắp đặt thành phần cốt truyện trên map; sinh địa hình theo seed; kiểm tra asset 3D trước khi đưa vào client | Sắp đặt/sinh địa hình: có thể là Unity Editor tool trong client, chưa quyết. Kiểm tra asset: script Blender headless ([0006](../decisions/0006-cong-cu-kiem-tra-asset.md)) |
| Analytics / crash report? | Chưa rõ | — |
| Website / cộng đồng? | Chưa rõ | — |

## Danh sách thành phần
<!-- Bảng: thành phần | trách nhiệm | công nghệ | tự làm hay dùng dịch vụ có sẵn | cần từ milestone | trạng thái (cần / không cần / chưa rõ).
Với một người làm: ưu tiên dịch vụ có sẵn (BaaS) cho thứ không tạo khác biệt; ghi rõ là đề xuất và chi phí. -->

| Thành phần | Trách nhiệm | Công nghệ | Tự làm / dịch vụ | Cần từ | Trạng thái |
|---|---|---|---|---|---|
| Client | Phần người chơi chạy: gameplay, hiển thị, âm thanh, input | Unity 6000.4.8f1 ([0001](../decisions/0001-client-unity.md)) | Tự làm | M1 | Cần |
| Backend | Tài khoản, lưu tiến trình và world, giờ chuẩn, rút thưởng, kho/tiền tệ, ghép trận | Chưa quyết | Dịch vụ có sẵn (BaaS); chi phí chưa tra | M2 | Cần — [0003](../decisions/0003-server.md) Chốt |
| Game server | Chạy trận PvP thời gian thực ở phía server (chống gian lận) | Unity Dedicated Server build ([docs 6000.4](https://docs.unity3d.com/6000.4/Documentation/Manual/dedicated-server-build.html)); hosting chưa quyết | Tự làm (build từ client) + thuê hosting | Khi làm PvP (sau M4) | Cần — [0003](../decisions/0003-server.md) Chốt |
| Công cụ kiểm tra asset | Kiểm model 3D (topology, tỉ lệ, pivot, UV, vật liệu, rig, xuất–nhập lại FBX/GLB) theo `tools/asset_check/rules.json`; exit 0/1 + báo cáo JSON. Kèm script tạo model thô qua HF Spaces (`tools/asset_gen/`). Không chạy trong game | Python trong Blender 5.0.1 headless; gradio_client gọi HF Spaces (FLUX.2-klein-4B, TRELLIS.2) | Tự làm (không có công cụ miễn phí đủ mục — xem 0006). Cách không cần nó: xem ảnh từng asset bằng tay | M0 (thử nghiệm art, ngoại lệ) → dùng lại M2 | Cần — Chốt [0006](../decisions/0006-cong-cu-kiem-tra-asset.md); selftest trong `verify` |

## Ranh giới trách nhiệm
<!-- Logic nào chạy ở client, logic nào ở server/dịch vụ (vd: tính sát thương, phần thưởng, giá). Quyết định này ảnh hưởng chống gian lận và chơi offline. -->
**Nguyên tắc** (user, 2026-10-13): client không được tin — backend kiểm mọi dữ liệu client gửi lên trước khi nhận.


| Logic | Chạy ở | Lý do |
|---|---|---|
| World voxel, PvE, lệnh làm việc, mô phỏng tua nhanh | Client | Nặng, liên tục; chỉ ảnh hưởng world riêng của người chơi |
| Thời gian vắng mặt cho tua nhanh | Backend | Chống chỉnh giờ thiết bị |
| Kiểm tra hợp lý dữ liệu client gửi lên (sản lượng, world đã sửa, trạng thái mô phỏng, tiến độ yêu cầu của loài) | Backend | Chống gian lận (user, 2026-10-13) — client tính, backend kiểm |
| Rút thưởng, tiền tệ, thanh toán, sở hữu sinh vật/phép, sinh chỉ số cá thể (phân bố ngẫu nhiên, vượt trội) | Backend | Có giá trị, dễ bị gian lận; game server lấy chỉ số từ backend khi chạy PvP |
| Điểm xếp hạng | Backend | Nhận kết quả từ game server |
| Lưu tiến trình và world | Backend | Game online, nhiều thiết bị |
| Ghép trận | Backend | Cần thấy mọi người chơi |
| Trận PvP và kết quả | Game server | Thời gian thực, ảnh hưởng người chơi khác |

