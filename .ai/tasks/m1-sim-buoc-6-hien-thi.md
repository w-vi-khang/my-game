# Task: M1 simulation bước 6 — hiển thị: nội suy vị trí, trạng thái

> Trạng thái: user xác nhận 2026-10-16 (4 điểm mở) — chờ làm, [brief 0006](../briefs/0006-sim-hien-thi.md)

## Mục tiêu
Bước 6 của [kế hoạch prototype simulation](../../design/04-tech/simulation-tech.md#kế-hoạch-prototype-m1). Xong khi: **"Sinh vật đi mượt ở 30 và 60 FPS"**. Ước lượng 1–2 ngày-người.
- Lần đầu chạy mô phỏng (bước 1–5, `Game.Core.Sim`) trong client Unity, trên map M1 thật.
- Phần hiển thị chỉ **đọc** trạng thái và nghe sự kiện ([architecture](../../design/04-tech/architecture.md) "Một chiều dữ liệu"). Vị trí sinh vật được nội suy giữa hai nhịp ([simulation-tech](../../design/04-tech/simulation-tech.md) "Nhịp cố định").

## Quyết định (user chọn 2026-10-16)
1. **Scene:** scene mới `SimView`, dựng trên `TerrainChunkView` (map M1 thật). Bố trí demo: 2 nhà, 1 kho, 4 ô ruộng, 3 sinh vật L1–L3 tự làm.
   - Đào/đặt bằng chuột (`TerrainEditInput`) báo cho mô phỏng qua `WorldEditor.CellChanged` → `Simulation.OnCellChanged`. Ô bị chiếm lấy từ `Simulation.IsOccupied`.
   - Cần chép `sim_m1.json` vào `Resources/Config`, kèm test kiểm hai bản giống nhau.
2. **Hình sinh vật:** khối hộp theo khung chiếm chỗ (X × Z × Y), màu theo loài, có mũi chỉ hướng đi.
3. **Trạng thái hiện:**
   - Idle / Moving / Working, dạng nhãn trên đầu;
   - tiến độ công đoạn khi đang làm;
   - biểu tượng đình trệ tại ô kèm lý do, đọc từ `State.Events` (nơi đọc tự `Clear()`), mất khi hết đình trệ.
   - Không làm HUD kho/ruộng.
4. **Nội suy + cách kiểm:**
   - Lưu vị trí liên tục (ô + `MoveProgress`) ở 2 nhịp gần nhất, vẽ nội suy theo phần nhịp đã trôi. Trễ 1 nhịp (100 ms).
   - Dịch chuyển tức thời (hiện lại, rơi nhiều ô) → đặt thẳng vị trí mới, không nội suy.
   - Bộ đếm nhịp + nội suy là C# thuần, có test EditMode giả lập khung 30, 60 và khung lệch.
   - User xem trong Editor; F1 / F2 khoá 30 / 60 FPS.

## Quyết định của Planner (trong brief)
- Bộ đếm nhịp, nội suy, dấu đình trệ, chữ trạng thái nằm trong `Game.Core` ở thư mục/namespace mới `Core/View` (`Game.Core.View`). Lý do: test EditMode được, và `Core/Sim` giữ luật cấm số thực / `Dictionary`.
  - Phần này **không** thuộc trạng thái mô phỏng: chỉ quyết định mỗi khung chạy mấy nhịp, nên không ảnh hưởng tính tất định.
  - Thời gian khung cộng theo **micro giây số nguyên**, để số nhịp mỗi khung tính tay được.
- Tối đa 5 nhịp mỗi khung. Khung quá dài thì bỏ phần dư, không đuổi theo.
- Bố trí demo cố định trên map M1 (seed 20261013), chọn từ bản đồ độ cao chạy thật [run 2026-10-16]:
  - mặt đất y = 17 (đứng ở y = 18) quanh x 30–36, z 16–20;
  - không có vùng 6 × 6 nào phẳng hoàn toàn trên map M1, vùng phẳng lớn nhất là 4 × 4.
  - Có test tích hợp (map M1 + `sim_m1.json`): 4 vụ vào kho, không đình trệ.

## Ngoài phạm vi
- Model / animation thật, HUD kho/ruộng, UI đặt lệnh (M2).
- Đo trên mobile, 60 sinh vật giả (bước 7).
- Tối ưu chỗ chậm đã ghi chú cho bước 7: `HasHomedAbility` / `ReportPhase`, `WorkSpeedPermille`.
- Đổi luật mô phỏng (`Core/Sim` chỉ thêm `SimDemo.cs`), đổi scene `TerrainView`.

## Tiêu chí chấp nhận (kiểm chứng được)
- [ ] Bộ đếm nhịp: số nhịp từng khung đúng số tính tay ở 30 và 60 FPS; chặn tối đa; bỏ qua dt ≤ 0 / NaN (EditMode).
- [ ] Nội suy: vị trí giữa nhịp đúng; leo 1 ô nội suy; nhảy > 1 ô hoặc sự kiện hiện lại → đặt thẳng; sinh vật thêm sau (EditMode).
- [ ] **Đi mượt:** đi thẳng 12 ô với khung 30 FPS, 60 FPS và khung lệch. Mỗi khung trong đoạn đi, độ dời đúng 1,5 ô/giây × dt (sai ≤ 1e-9), không khung nào đứng yên; tới nơi đúng ô (EditMode).
- [ ] Dấu đình trệ bật/tắt theo sự kiện; chữ trạng thái đúng nguyên văn (EditMode).
- [ ] Demo trên map M1: bố trí đúng; 4000 nhịp → kho 4 nông sản, 10 hạt, 0 đình trệ; giữ trang trại chạy 8000 nhịp → ≥ 8 nông sản (EditMode).
- [ ] `SimView` dựng được trong PlayMode, khoá 60 FPS, trả lại cài đặt FPS khi huỷ.
- [ ] EditMode + PlayMode toàn bộ đạt; `check_design.py` exit 0; grep cấm trong `Core/Sim/` rỗng.
- [ ] User xem: đi mượt ở 30 / 60 FPS, nhãn trạng thái, dấu đình trệ khi vây kho.
- Planner khi review: ghi kết quả vào simulation-tech (hàng 6) + roadmap; thêm `Game.Core.View` vào bảng "Tách assembly" của architecture.

## Kết quả
_(chưa có)_
