# Tầm nhìn (Vision)

> Trạng thái: Nháp
> Liên quan: [Lối chơi cốt lõi](02-gameplay/core-loop.md) · [Lộ trình](05-roadmap.md)

## Pitch một câu
<!-- Game là gì, người chơi làm gì, vì sao hấp dẫn — trong 1–2 câu. -->
[ý tưởng] Một pháp sư mất ký ức hồi sinh thế giới phép thuật voxel: cải tạo đất đai thành đúng môi trường sống để gọi các sinh vật thất lạc trở về, rồi cùng chúng vận hành nông trại ngày càng lớn — và nhập vào chúng để chiến đấu.

## Pillars (3–5 trụ cột)
<!-- Những trải nghiệm KHÔNG được đánh mất. Mọi tính năng phải phục vụ ít nhất 1 pillar. Vd: 'Khám phá có thưởng', 'Mỗi trận < 10 phút'. -->
| # | Pillar | Ý nghĩa |
|---|---|---|
| P1 | **Hồi sinh để sưu tầm** | Loài chỉ trở lại khi người chơi cải tạo thế giới thành đúng môi trường sống của nó. |
| P2 | **Sinh vật là cốt lõi** | Mỗi sinh vật có giá trị ở cả nông trại (làm việc) lẫn chiến đấu (phép, nhập hồn), nhờ nội tại riêng. |
| P3 | **Tự do trong khung truyện** | Có cốt truyện và nhiệm vụ, nhưng không ép làm theo thứ tự. |
| P4 | **Chiến đấu bằng bộ phép của sinh vật** | Sức mạnh chiến đấu đến từ việc chọn sinh vật và gán phép cho chúng (nguyên tố, tộc, loài, ô phép); người chơi nhập hồn để dùng phép đó. Áp dụng cả khi chơi một mình lẫn ghép trận. |

## Người chơi mục tiêu
<!-- Ai chơi? Chơi bao lâu mỗi lần? Thiết bị? Kinh nghiệm game? -->
- Thiết bị: PC và mobile.
- Độ tuổi, kinh nghiệm game, thời lượng một phiên: _(chưa có)_

## Thể loại & game tham khảo
<!-- Thể loại; 2–4 game tham khảo kèm link và điều lấy/không lấy từ mỗi game. -->
Thể loại: voxel sandbox + sưu tầm sinh vật + nông trại tự động hoá + phiêu lưu cốt truyện.

| Game | Lấy | Không lấy |
|---|---|---|
| Viva Piñata ([wiki](https://pinatajournal.wiki/wiki/Pinata)) | Loài xuất hiện theo yêu cầu môi trường riêng của từng loài | Khu vườn nhỏ cố định |
| Palworld ([hướng dẫn phân việc](https://primagames.com/tips/how-to-assign-pals-jobs-at-your-base-in-palworld)) | Sinh vật làm việc theo khả năng của loài | AI tự do phức tạp — thay bằng lệnh làm việc chia công đoạn |
| Cassette Beasts ([trang chính thức](https://cassettebeasts.com/about)) | Người chơi mang hình dạng/kỹ năng sinh vật khi chiến đấu | Chiến đấu theo lượt |
| Dragon Quest Builders 2 ([wiki](https://dragon-quest.org/wiki/Dragon_Quest_Builders_2)) | Truyện theo từng vùng đất; nhiều người chơi không chung tiến trình truyện ([review](https://nintendowire.com/news/2019/07/16/review-dragon-quest-builders-2-for-switch/)) | — |

## Nền tảng & mô hình kinh doanh
<!-- PC/mobile/console? Trả phí một lần / F2P / chưa quyết? -->
- Nền tảng: PC và mobile.
- Game online: cần tài khoản và kết nối server. Mỗi người chơi có world riêng; PvE trong world riêng, PvP qua ghép trận.
- Cập nhật thêm map theo phiên bản/mùa; dự kiến 3 map lúc đầu.
- Mô hình kinh doanh: hai loại tiền — **tiền trong game** (kiếm được khi chơi) và **tiền nạp**. Tiền nạp mang lại: làm đẹp (skin), hiệu ứng, tăng cơ hội (rút thưởng), đẩy nhanh tiến trình xây dựng, và ưu đãi giúp tiến trình dễ thở hơn (vật phẩm hỗ trợ, túi đồ +10%, giới hạn sinh vật +10%...). Nguyên tắc (user, 2026-10-12): tiền nạp luôn ảnh hưởng **tốc độ** tiến trình — không tránh được; chỉ chặn ở **mức tối đa**: không mua được thứ người không nạp không đạt được, ở mức tối đa mọi người như nhau trong PvP — xem [0004](decisions/0004-tien-nap.md).
- Free-to-play (tải miễn phí).

## Điểm khác biệt
<!-- Vì sao người chơi chọn game này thay vì game tham khảo? -->
[ý tưởng] Ghép "cải tạo môi trường sống để gọi loài về" với voxel world khám phá được và nông trại do chính các loài đó vận hành. Chưa tra được game nào có đúng tổ hợp này — [chưa xác minh], cần khảo sát thị trường trước khi Chốt.

## Ràng buộc
<!-- Một người làm: quỹ thời gian, ngân sách, kỹ năng mạnh/yếu (code, art, âm thanh). -->
- Một người làm; dự án indie cá nhân, dài hạn, không có timeline cố định.
- Client: Unity 6000.4.8f1 ([0001](decisions/0001-client-unity.md)).
- Quỹ thời gian: ~10 giờ/tuần (user, 2026-10-12) — ước lượng theo phạm vi hiện tại vượt xa quỹ này, xem [roadmap](05-roadmap.md#kiểm-tra-phạm-vi).
- Kỹ năng: mạnh code client (Unity/C#) và backend/server; art, âm thanh, level design không tự làm → asset có sẵn / thuê ngoài (user, 2026-10-12).
- Ngân sách: _(chưa có)_

## Không làm (out of scope)
<!-- Liệt kê rõ những gì game sẽ KHÔNG có, để chống phình phạm vi. -->
- Địa hình sinh ngẫu nhiên vô tận: world mở rộng bằng các map có ranh giới, thêm theo mùa.
- Co-op ở giai đoạn đầu: co-op đảo sandbox là spin-off, tạm bỏ ([ideas](ideas.md)). Mọi mode nhiều người không ảnh hưởng tiến trình chính.
- AI sinh vật tự do phức tạp: sinh vật làm việc theo lệnh làm việc do người chơi đặt.
- Mô phỏng khi game tắt: chỉ tính bù (tua nhanh) khi người chơi vào lại.
- Trả tiền để vượt mức tối đa: tiền nạp giúp đi nhanh hơn, nhưng không mua được chỉ số/sức mạnh vượt mức người không nạp cũng đạt được; phần vượt trội bị bỏ trong PvP.
