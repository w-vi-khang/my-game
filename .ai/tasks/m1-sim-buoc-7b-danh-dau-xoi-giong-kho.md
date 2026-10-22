# Task: M1 simulation bước 7b — đánh dấu xới từng ô, 2 loại cây, kho thông nhau (lõi)

> Trạng thái: **xong** 2026-10-21 (brief 0029 đã duyệt). Trước đó: user quyết 2026-10-21 ([0025](../../design/decisions/0025-nguoi-choi-xoi-dat-chon-giong-hang-doi.md), Chốt) — brief [0029](../briefs/0029-sim-danh-dau-xoi-giong-kho.md) chờ làm; user xác nhận luật 6–20 ngày 2026-10-21. Làm **trước bước 8**; bước 7c (client) làm sau bước này.

## Mục tiêu
Bước 7b của [kế hoạch prototype simulation](../../design/04-tech/simulation-tech.md#kế-hoạch-prototype-m1). Ước lượng 2,5–5 ngày-người (roadmap M1).

Pillar: P2 (sinh vật làm việc trên nông trại); P1 gián tiếp (ô cỏ bị xới không còn tính cho môi trường sống, 0020).

Thành phần phụ trách: **client Unity** — lõi `Game.Core.Sim` (C# thuần, No Engine References) + `design/data/sim_m1.json`. Phần client (`SimView`, `SimBenchMeter`) chỉ sửa cho biên dịch được; công cụ Xới, biển, bảng chọn giống, bảng kho là bước 7c.

Xong khi (cột "Xong khi" của bước 7b):
- đánh dấu → sinh vật L1 xới → ô ruộng; ô không có vụ thì đứng yên;
- vụ đầu sau khi xới vào thẳng gieo, vụ kế xới lại;
- hai loại cây đúng thời gian lớn / hạt / sản lượng;
- hết hạt một loại → đình trệ "thiếu hạt" chỉ ở loại đó;
- hàng giao về kho gần nhất; đầy khi tổng chung đầy;
- luật huỷ vụ 0020 vẫn đúng;
- cùng seed → cùng mã băm.

## Quyết định (user, 2026-10-21 — 0025)
1. Đánh dấu **từng ô** (mặt trên khối đất/cỏ, ô trên trống). Đánh dấu không cần chọn giống. Sinh vật L1 xới xong → ô ruộng.
2. Chọn giống → vụ vào cuối hàng đợi (2 chỗ). Ô vừa xới: vụ đầu từ gieo. Vụ kế sau thu hoạch: xới lại rồi gieo. Hết hàng đợi → đứng yên.
3. Hai loại cây, số liệu tạm; `game-balance` đặt sau. Tên do user đặt, tạm Cây A, Cây B.
4. Bỏ `AddFarmOrder`.
5. Kho gần nhất tới được; các kho thông nhau (hàng tính chung, sức chứa cộng dồn).

## Planner đề xuất trong 0025 (user xác nhận 2026-10-21, brief 0029)
- "Đã xới" là trạng thái của ô ruộng: xới xong bật, thu hoạch xong tắt; vụ bắt đầu khi bật → gieo, tắt → xới.
- Bỏ đánh dấu bằng công cụ Xới; sinh vật đang tới xới về rảnh.
- Đào/che ô có biển → bỏ đánh dấu, không trả gì. Đào/che ô ruộng → luật 0020.
- Xoá vụ: chưa bắt đầu → rời hàng đợi; đang làm → huỷ như 0020 nhưng ô vẫn là ô ruộng.
- Hạt và nông sản theo loại; 10 hạt mỗi loại; sức chứa tính chung.
- Kho gần nhất = đường ngắn nhất từ ô ruộng; không tới được → đình trệ "không có đường".

Thêm khi viết brief (Planner, user xác nhận 2026-10-21): xem mục "Quyết định Planner thêm" trong brief 0029.

## Cách kiểm
Test EditMode (`TestWorlds`, bố trí như `FarmTests`); `SimDemoTests`, `SimDemoMapsTests`, `SimStressTests` vẫn đạt; mã băm trùng 2 lần chạy cùng seed.

## Ngoài phạm vi
- Công cụ Xới, biển canh tác, bảng chọn giống/hàng đợi, nút Kho + bảng kho, thao tác chạm (7c).
- "Chọn thông minh" theo khuôn ([ideas](../../design/ideas.md)); người chơi tự đặt kho (M2); cấp ô ruộng (M2).
- Số liệu cân bằng của 2 loại cây (`game-balance`, sau).
- Môi trường sống (bước 8).
