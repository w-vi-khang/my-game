# Task: M1 world 5j — sửa lỗi sau chơi thử nước 0079–0081

> Model thực thi: theo từng brief (AGENTS.md §10.2).
> Trạng thái: Đang làm — brief [0082](../briefs/0082-world-5j-nuoc-trong-khong-chong-lop.md) (lỗi 1) đã duyệt 2026-10-29; [0083](../briefs/0083-world-5j-mep-tran-thon-camera-an-than.md) (lỗi 6, 8; lỗi 7 chờ user) đã duyệt 2026-10-29. User chơi thử một lượt khi xong cả 5j (user 2026-10-29).
> - Nguồn: user chơi thử gộp 0079–0081 (2026-10-29, mục Review của [brief 0081](../briefs/0081-world-5i-bo-lom-chan-thac.md)); [world-tech](../../design/04-tech/world-tech.md) dòng 5j.
> - Ước lượng 2–4 ngày-người [ý tưởng], roadmap M1.

## Mục tiêu
Sửa 5 lỗi user thấy khi chơi thử, mỗi lỗi một brief (hoặc gộp khi cùng file):
1. Cột thác "đứt khúc", thấy từng mảng (shader nước trong suốt vẽ chồng lớp không theo thứ tự) — brief 0082.
2. Nước không lấp đầy hố đào.
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
- [ ] Mỗi lỗi có test tái hiện (fail trước, đạt sau) hoặc ghi rõ vì sao chỉ kiểm được bằng mắt.
- [ ] User chơi thử lại: không còn 5 lỗi trên.
