# 0015 — Tìm đường M1: khung chiếm chỗ 3 chiều, không đi vào nước, đào dưới chân thì sinh vật chạy ra

> Trạng thái: Chốt
> Ngày: 2026-10-14
> Thay đổi: dòng "Tìm đường … khung 1 × 1, 4–7" của [0007](0007-m1-toi-gian.md) (→ khung theo loài, 5–8); hàng bước 2 trong [kế hoạch prototype simulation](../04-tech/simulation-tech.md#kế-hoạch-prototype-m1) ("khung 1 × 1" → khung theo loài); thêm luật vào [mechanics — Voxel world luật M1](../02-gameplay/mechanics.md#voxel-world--luật-m1).

## Bối cảnh
Viết task contract simulation bước 2 (A\* đi bộ, 2026-10-14). Kế hoạch đã duyệt ghi "khung 1 × 1", khung nhiều ô để M2; luật M1 chưa nói đào ô ngay dưới chân sinh vật thì sao.

## Quyết định
**Chốt (user, 2026-10-14):**
- Loài M1 đều cao 1 ô (khung 1 × 1 × 1). Sinh vật **không đi vào/qua ô nước** — bơi là M2.
- Nhưng **mỗi loài có khung chiếm chỗ 3 chiều** (vd 1 × 1 × 2, 1 × 1 × 1) — user nhấn mạnh không phải sinh vật nào cũng một ô. → Dữ liệu loài có khung (rộng X, rộng Z, cao Y) ngay từ bước 2 và A\* kiểm đủ khung (Planner: chi phí nhỏ, tránh làm lại ở M2). Vùng liên thông theo từng cỡ khung vẫn là M2.
- **Đào ô dưới chân sinh vật được phép; sinh vật chạy ra ô bên cạnh** đứng được. Không có ô bên cạnh nào → rơi thẳng xuống tới chỗ có đáy. Cũng áp dụng khi nước lan vào ô sinh vật đang đứng.

## Hệ quả
- Ước lượng (AI): khung 3 chiều + chạy ra/rơi +1 ngày-người → bước 2 **3–5 → 4–6**; kế hoạch simulation 15–26 → **16–27**. M1 51–85 → **52–86**; tổng M1–M4 ~12,0 → **~12,1**–22,7 năm ([roadmap](../05-roadmap.md)).
- Đặt khối vào ô thuộc khung của sinh vật vẫn bị từ chối (luật đặt cũ).
