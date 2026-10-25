# 0042 — Mặt nước thấp dần theo mức, lan tối đa 5 ô, ô nối

> Trạng thái: Chốt
> Ngày: 2026-10-25
> Thay đổi:
> - [0040](0040-nuoc-chay-nguon-lan-gioi-han-m1.md) mục "Hiển thị M1" (mỗi ô nước vẽ đầy ô) và N mặc định 7 → 5.
> - Giữ nguyên luật rãnh hẹp và luật rơi của [0041](0041-nuoc-chay-trong-ranh-hep-va-thac-nuoc.md).

## Bối cảnh
User chơi thử brief 0076 + 0077 (thác nước, mặt bên ô nước), ngày 2026-10-25. Ảnh chụp: nước lan trên địa hình bậc thang nhìn như các hộp xanh trong suốt chồng lên nhau. Ý kiến của user:
- "nước bị phân tầng, khi lan ra thì ko đều, nhìn không đúng logic";
- ở chân thác, chỗ nước rơi chạm mặt nước, player bị nổi lên và đứng trên mặt nước.

Nguyên nhân phần hiển thị: M1 vẽ đầy ô ở mọi mức. Từ brief 0077, chỗ giáp ô trống có thêm mặt bên, nên mỗi ô nước thành một hộp.

## Các lựa chọn
- **A. Mặt nước theo mức:** ô đang chảy có mặt nước thấp dần theo mức; góc mặt nước lấy trung bình của các ô chung góc; mặt bên chỉ vẽ ở chỗ giáp ô trống. Khoảng 1–1,5 ngày-người. Trước đây ghi là để M2 (0040).
- **B. Giữ đầy ô, bớt mặt bên:** chỉ ô rơi có mặt bên. Khoảng 0,3 ngày-người, nhưng mép nước vẫn thành bậc.

## Quyết định
User, 2026-10-25 (Planner viết lại logic, user xác nhận "Đúng rồi"):
1. **Lan trên mặt bằng tối đa 5 ô.** Mực nước giảm dần từng ô, không tắt đột ngột ở ô thứ 5. Ô thứ 6 không có nước.
2. **Trong rãnh hẹp hoặc khi rơi xuống**, giới hạn ô được đặt lại từ đầu rồi nước lan tiếp. Giữ nguyên 0041; user chọn giữ luật rơi.
3. **Ô nối:** mép lan dừng như mục 1. Nếu ngay sau ô trống thứ 6 (tức ô thứ 7) có nước, ô 6 thành nước để hai vùng nối liền. Nếu ô 6 đã có nước, hai vùng nhìn liền với nhau.
4. **Vẽ lại:** khi mức nước của một ô đổi thì chunk chứa ô đó được dựng lại.

Planner viết lại logic, user xác nhận. Số là [số tạm], chưa chốt, nằm trong `design/data/water_m1.json`:
- **Mức:** nguồn, ô đang rơi và ô hẹp có mức N = 5. Lan ngang mỗi ô giảm 1 mức: ô 1 mức 5 … ô 5 mức 1.
- **Độ cao mặt nước** (tính từ đáy ô):
  - ô đang chảy, không rơi, mức L: 0,9 × L / N;
  - nguồn và ô đang rơi: 0,9 (1 − độ hạ mặt nước 0,1, như hiện nay);
  - ô có nước phía trên: 1.
- **Góc mặt nước:** mỗi góc lấy trung bình độ cao của các ô nước cùng tầng chung góc đó. Nếu một trong các ô đó có nước phía trên thì góc lấy 1. Hai ô nước kề nhau dùng chung góc nên mặt nước liền, không có vách bên trong.
- **Mặt bên:** chỉ vẽ ở chỗ giáp ô trống, từ đáy ô lên tới góc mặt nước. Mặt đáy giữ như brief 0077.
- **Luật ô nối:** một ô trống, đứng được (dưới là khối đặc hoặc nước đứng), có nước đứng không đang rút ở **hai phía đối diện** (trái–phải hoặc trước–sau) thì thành nước đang chảy mức 1. Ô nối mất nước ở một phía thì rút. Mọi tập ô nối hữu hạn đều có một ô ở góc ngoài thiếu cặp đối diện, nên ô nối không tự nuôi nhau. Khối không còn nguồn thì vẫn rút qua kiểm khối của 0041.
- **Vẽ lại:** đổi mức hoặc đổi cờ của ô nước (vật liệu không đổi) phát sự kiện riêng cho client dựng lại chunk. `CellChanged` vẫn chỉ phát khi vật liệu đổi, để mô phỏng, môi trường sống và minimap không bị ảnh hưởng.
- **Lỗi đứng trên mặt nước ở chân thác:** chưa rõ nguyên nhân. Collider địa hình không chứa nước, nhân vật không có lực nổi. Tái hiện qua MCP rồi sửa.

## Hệ quả
- Ước lượng (AI): +1,5–2 ngày-người M1 (mặt nước theo mức + ô nối + đổi N: 1–1,5; lỗi đứng trên mặt nước: ~0,5). Cập nhật [roadmap](../05-roadmap.md), [world-tech](../04-tech/world-tech.md) (bước 5g), [mechanics](../02-gameplay/mechanics.md), [glossary](../glossary.md).
- "Mặt nước theo mực" chuyển từ M2 về M1. Bảo toàn lượng nước vẫn ở M2.
- Code: task [m1-world-nuoc-chay](../../.ai/tasks/m1-world-nuoc-chay.md), brief 0078.
