# Task: M1 world 5j — sửa lỗi sau chơi thử nước 0079–0081

> Model thực thi: theo từng brief (AGENTS.md §10.2).
> Trạng thái: Đang làm — brief [0082](../briefs/0082-world-5j-nuoc-trong-khong-chong-lop.md) (lỗi 1) đã duyệt 2026-10-29; [0083](../briefs/0083-world-5j-mep-tran-thon-camera-an-than.md) (lỗi 6, 8; lỗi 7 chờ user) đã duyệt 2026-10-29; [0084](../briefs/0084-world-5j-do-day-nuoc-ho-rong.md) (lỗi 3 sửa; lỗi 2: code đúng 0043, luật không lấp hố rộng — chờ user chọn luật) chờ làm; [0085](../briefs/0085-world-5j-ho-thap-hon-nguon-thi-day.md) (lỗi 2) đã duyệt; [0086](../briefs/0086-world-5j-dung-duoi-chan-thac.md) (lỗi 4: tia click xuyên nước + tự nhảy bậc khi đích gần) và [0087](../briefs/0087-world-5j-vat-roi-nam-yen-lech-ngau-nhien.md) (lỗi 5) chờ làm. User chơi thử một lượt khi xong cả 5j (user 2026-10-29).
> - Nguồn: user chơi thử gộp 0079–0081 (2026-10-29, mục Review của [brief 0081](../briefs/0081-world-5i-bo-lom-chan-thac.md)); [world-tech](../../design/04-tech/world-tech.md) dòng 5j.
> - Ước lượng 2–4 ngày-người [ý tưởng], roadmap M1.

## Mục tiêu
Sửa 5 lỗi user thấy khi chơi thử, mỗi lỗi một brief (hoặc gộp khi cùng file):
1. Cột thác "đứt khúc", thấy từng mảng (shader nước trong suốt vẽ chồng lớp không theo thứ tự) — brief 0082.
2. Nước không lấp đầy hố đào. → user chọn luật [0044](../../design/decisions/0044-ho-thap-hon-mat-nguon-thi-day.md) (2026-10-29): ô thấp hơn mặt nguồn thì đầy; brief sau 0084.
3. Mặt nước mức ≥ 2 trông như nhau (chỉ thấy mỏng dần từ mức 1 về 0).
4. Đứng dưới chân thác bị đẩy nổi 1 ô.
5. Vật rơi nằm trên mặt đất, không lơ lửng/xoay; rơi ngẫu nhiên gần khối bị phá.
6. (user 2026-10-29, sau 0082) Gai nước nhô ra ở chỗ mép tràn nối cột thác (`art-test/shots/user0082_lip_spike.webp`).
7. Gờ mỏng ở đầu cột, ngay dưới mép tràn (`user0082_column_side.png`).
8. Camera dưới nước nhìn lên: một mảng xanh đậm phẳng, cạnh thẳng (`user0082_underwater.png`).
- **Pillar:** P1 (cải tạo thế giới). **Thành phần:** client Unity.

## Ngoài phạm vi
- Hiển thị dòng chảy (5k), hiệu ứng bọt chân thác.
- Phá/đặt khối dưới chân player: giữ như hiện tại (user 2026-10-29).

## Tiêu chí chấp nhận
- [x] Mỗi lỗi có test tái hiện (fail trước, đạt sau) hoặc ghi rõ vì sao chỉ kiểm được bằng mắt — brief 0082–0087 đã duyệt 2026-10-29 (lỗi 7 chỉ kiểm bằng mắt).
- [ ] User chơi thử lại: không còn 5 lỗi trên.
