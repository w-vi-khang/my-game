# Kinh tế game

> Trạng thái: Nháp
> Liên quan: [Tiến trình](progression.md) · [Cơ chế](mechanics.md)

## Tài nguyên
<!-- Bảng: tài nguyên | nguồn (faucet) | nơi tiêu (sink) | giới hạn. -->
Mới liệt kê các dòng chảy đã biết từ cơ chế; số liệu, giới hạn chưa có.

| Tài nguyên | Nguồn (faucet) | Nơi tiêu (sink) | Giới hạn |
|---|---|---|---|
| Tài nguyên thế giới (khối, vật liệu) | Khám phá, thu thập | Cải tạo môi trường sống, xây dựng | _(?)_ |
| Sản lượng nông trại | Lệnh làm việc, tua nhanh | _(chưa có)_ | Sức chứa kho |
| Sinh vật (cá thể) | Hồi sinh loài, bắt, ấp nở, rút thưởng (chỉ loài đã hồi sinh; backend quyết định kết quả) | Nhân công lệnh làm việc, chiến đấu; thả đi | Giới hạn sinh vật hoạt động theo cấp độ người chơi và công trình (+10% nếu nạp); dư cất vào kho sinh vật |
| Trang bị | Rèn ở lò rèn, ghép trang bị, gói trang bị | Gắn cho sinh vật; ghép lên bậc cao hơn (tiêu trang bị bậc thấp) | Bậc tối đa người không nạp đạt được |
| Thẻ bài (bậc 1–4) | _(chưa có)_ | Bắt sinh vật (chứa sinh vật) | Bậc 4 chắc chắn bắt được |
| Ma năng | Rút gói sách phép ra phép đã sở hữu | Đổi phép trong cửa hàng; chế tác một số trang bị | _(?)_ |
| Nguyên liệu rèn | _(chưa có)_ | Rèn trang bị | _(?)_ |
| Vật phẩm phân bổ lại | _(chưa có)_ | Phân bổ lại điểm chỉ số của sinh vật | _(?)_ |
| Vật phẩm hỗ trợ | Ưu đãi nạp; nguồn khác _(chưa có)_ | _(chưa có)_ | _(?)_ |
| Quà từ sinh vật | Đôi khi, khi mức đáp ứng nhu cầu sinh vật cao | _(chưa có)_ | _(?)_ |
| Phép sinh vật | Sinh vật mới, nghiên cứu, gói sách phép | Gán vào ô phép (3 ô ở bậc 1, 4 ô từ bậc 2) | Cấp độ, bậc, nhóm phép, ô đặc biệt |
| Phép của pháp sư | Tỉ lệ nhỏ khi sở hữu sinh vật mới | Pháp sư dùng | _(?)_ |
| Mana | _(chưa có)_ | Phép, nhập hồn | _(?)_ |
| Tiền trong game | Kiếm khi chơi (nguồn cụ thể _(chưa có)_) | Rút thưởng, _(nơi tiêu khác chưa có)_ | _(?)_ |
| Tiền nạp | Nạp tiền thật | Làm đẹp (skin), hiệu ứng, tăng cơ hội rút thưởng, đẩy nhanh xây dựng, ưu đãi nạp: vật phẩm hỗ trợ, túi đồ +10%, giới hạn sinh vật +10%, phụ trợ khác | Ảnh hưởng tốc độ, không vượt được mức tối đa người không nạp cũng đạt được |
| Điểm xếp hạng | Thắng trận PvP (thể thức nhiều sinh vật cho nhiều điểm hơn) | _(chưa có — có giảm khi thua?)_ | _(?)_ |

## Cân bằng
<!-- Tốc độ kiếm/tiêu dự kiến; bảng số liệu hoặc link bảng tính trong design/. -->
_(chưa có)_

## Rủi ro
<!-- Lạm phát, cày quá dễ/khó, phá game. -->
- Nguyên tắc (user, 2026-10-12): tiền nạp ảnh hưởng **tốc độ** tiến trình là không tránh được; chặn ở **mức tối đa** — người không nạp cũng đạt được mọi mức tối đa, phần vượt trội bị bỏ trong PvP (xem [cơ chế](mechanics.md#chế-độ-nhiều-người), [0004](../decisions/0004-tien-nap.md)). Ghép trận theo hạng rồi cấp độ trung bình giảm chênh lệch khi đang lên.
- Ưu đãi nạp (+10% túi đồ, +10% giới hạn sinh vật) làm người nạp có nhiều nhân công hơn → nông trại nhanh hơn. Chấp nhận theo nguyên tắc trên; cần giữ mức % nhỏ.
- Tăng cơ hội rút thưởng bằng tiền nạp: người không nạp cảm thấy thiệt khi sưu tầm — cần cân bằng tỉ lệ (chưa có số liệu). Bảo hiểm gói (công khai mốc) giới hạn mức xui tệ nhất cho mọi người chơi ([cơ chế](mechanics.md#nhận-sinh-vật)).
- Đẩy nhanh xây dựng bằng tiền nạp có thể làm vòng lặp lệnh làm việc mất ý nghĩa — cần giới hạn.
