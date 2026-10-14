# 0014 — Ghép việc + di chuyển theo khoảng cách làm ngay ở simulation bước 1; người chơi nhận việc để M2

> Trạng thái: Chốt
> Ngày: 2026-10-14
> Thay đổi: thứ tự bước 1 và 3 trong [kế hoạch prototype simulation](../04-tech/simulation-tech.md#kế-hoạch-prototype-m1) (user duyệt 2026-10-13). Tổng ước lượng không đổi.

## Bối cảnh
- Khi viết task contract simulation bước 1 (2026-10-14), user mô tả cách giao việc mong muốn: người chơi chỉ đặt việc; bộ quản lý nhiệm vụ tự phân công sinh vật phù hợp (đúng năng lực, rảnh, gần, thuần thục cao; việc ưu tiên cao được chọn trước); sinh vật phải đi tới nơi làm và việc đi tốn thời gian, kể cả việc cần đi nhiều nơi (vận chuyển vào kho); người chơi mở tab nhiệm vụ, nhận việc chưa được giao rồi tự đi tới làm thay sinh vật; việc dừng giữa chừng vẫn giữ tiến độ.
- Kế hoạch đã duyệt để ghép việc ở bước 3, đi lại ở bước 2; người chơi làm công đoạn và độ ưu tiên giữa các lệnh là M2 ([mechanics](../02-gameplay/mechanics.md#lệnh-làm-việc)).

## Các lựa chọn
- **A.** Bước 1 gồm luôn ghép việc + di chuyển theo khoảng cách ô (chưa A\*); bước 2 thay khoảng cách bằng A\*. Người chơi nhận việc + độ ưu tiên vẫn M2, nhưng dữ liệu công đoạn có sẵn chỗ cho người làm là người chơi và trường độ ưu tiên.
- **B.** Giữ đúng kế hoạch (bước 1 chỉ dữ liệu + nhịp + seed).
- **C.** Đưa người chơi nhận việc + độ ưu tiên vào M1 (+2–4 ngày-người theo mechanics, cộng UI tab).

## Quyết định
**Chốt (user, 2026-10-14): A.**
- Bước 1: nhịp 10/giây, số nguyên, seed; dữ liệu sinh vật / lệnh / công đoạn / kho; ghép việc (sinh vật rảnh, đúng năng lực; điểm = khoảng cách ÷ thuần thục; hoà điểm bốc thăm bằng bộ sinh có seed của mô phỏng; công đoạn ưu tiên cao xét trước); đi theo khoảng cách ô với tốc độ của loài; vận chuyển đi 2 chặng (nơi lấy → nơi giao); bỏ người làm giữa chừng giữ tiến độ.
- Người chơi nhận việc qua tab nhiệm vụ + độ ưu tiên giữa các lệnh: **M2** như đã chốt. M1 chỉ có trường dữ liệu (người làm = sinh vật hoặc người chơi; độ ưu tiên, M1 mọi công đoạn bằng nhau).

## Hệ quả
- Ước lượng: bước 1 2–4 → **3–5**; bước 3 3–5 → **2–4** (bớt phần ghép việc). Cộng kế hoạch vẫn 15–26; M1 và roadmap không đổi.
- Cập nhật: [simulation-tech](../04-tech/simulation-tech.md#kế-hoạch-prototype-m1) (bảng kế hoạch), [mechanics](../02-gameplay/mechanics.md#lệnh-làm-việc) (chi tiết tab nhiệm vụ, M2).
