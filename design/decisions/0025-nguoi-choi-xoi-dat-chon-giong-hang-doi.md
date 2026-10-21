# 0025 — Người chơi xới đất tạo ô ruộng, bấm ô ruộng chọn giống cây, hàng đợi nhiều loại cây

> Trạng thái: Đề xuất
> Ngày: 2026-10-21
> Thay đổi (nếu chốt): [0016](0016-o-ruong-kho-nha-va-sinh-vat-ket.md) (M1 một loại cây; nhiều loại cây ở M2; lệnh trồng cây đặt theo vùng), [0020](0020-o-ruong-tao-khi-xoi-va-huy-vu.md) (ô ruộng chỉ có khi **sinh vật** xới xong), [mechanics — Kho & lệnh trồng cây](../02-gameplay/mechanics.md#kho--lệnh-trồng-cây--luật-m1), "người chơi làm công đoạn" đang ở M2 ([simulation-tech](../04-tech/simulation-tech.md#kế-hoạch-prototype-m1)).

## Bối cảnh
User yêu cầu ghi chú tính năng này (2026-10-21, lúc viết brief 0028). Chưa làm.
- Người chơi có thêm công cụ **Xới đất**, cùng cấp với Đào và Đặt ([brief 0027](../../.ai/briefs/0027-tach-dao-dat-ban-mo.md)).
  - Chọn mặt trên của khối cỏ hoặc khối đất → khối thành **ô ruộng**, trồng được.
  - Xới xong **chưa giao việc trồng**.
- Người chơi bấm vào ô ruộng → **chọn giống cây** cần trồng.
- **Hàng đợi vụ** (đã có từ 0016, 2 chỗ) dùng để lên lịch trồng **nhiều loại liên tiếp**.
- Hết hàng đợi → dừng mọi việc của ô ruộng đó. Luật này đã đúng như hiện tại: ô không có vụ thì không mở công đoạn.

Hiện tại [repo]:
- Ô ruộng sinh ra khi **lệnh trồng cây** đặt lên một vùng ô (`Simulation.AddFarmOrder`, `client/Assets/Game/Core/Sim/Simulation.cs:760`). Hàng đợi có sẵn 1 vụ.
- Mỗi vụ bắt đầu bằng công đoạn xới **của sinh vật**. Xới xong ô mới thành `Farmland` (0020).
- Mới có một loại cây: hàng đợi lưu loại cây của từng vụ (giá trị 0) nhưng chưa có bảng giống (0016).

## Các lựa chọn
[ý tưởng — Planner, cần user chọn]
- **Milestone:**
  - (a) M1, làm sau sim bước 7 và trước bước 8;
  - (b) M2, cùng "người chơi làm công đoạn" và "nhiều loại cây + cấp ô ruộng".
- **Ai xới:**
  - (a) Người chơi xới tức thì (đúng lời user);
  - (b) Người chơi chỉ đánh dấu, sinh vật L1 đi xới.
- **Vụ kế trên ô đã là ô ruộng:**
  - (a) bắt đầu từ gieo, bỏ công đoạn xới;
  - (b) vẫn xới lại như 0016.
- **Kho nhận nông sản:**
  - (a) kho gần nhất tới được;
  - (b) người chơi chọn kho khi chọn giống.
- **Số loại cây M1:** 2–3 loại, khác nhau ở thời gian lớn, hạt giống và sản lượng. Số liệu do game-balance đặt sau.
- **Lệnh trồng cây theo vùng** (`AddFarmOrder`):
  - (a) bỏ;
  - (b) giữ làm cách nhanh để xới và lên lịch nhiều ô cùng lúc.

Ước lượng [ý tưởng, AI], tổng **4,5–8 ngày-người**:

| Việc | Ngày-người |
|---|---|
| Công cụ Xới của người chơi | 0,5–1 |
| Ô ruộng không cần lệnh, đổi chuỗi công đoạn | 1–2 |
| Bảng giống cây trong `sim_m1.json` | 1–2 |
| Bảng chọn giống + xem/xoá hàng đợi khi bấm ô ruộng (PC + chạm) | 2–3 |

## Quyết định
_(chờ user)_

## Hệ quả
Khi chốt thì cập nhật:
- mechanics: Kho & lệnh trồng cây, Phép;
- simulation-tech: thêm bước vào kế hoạch prototype;
- controls-ui: công cụ Xới, bảng chọn giống;
- roadmap: ước lượng, milestone;
- glossary: giống cây, công cụ Xới;
- bố trí `SimDemo` / `SimStress`.
