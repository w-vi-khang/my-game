# 0029 — Nạp chunk theo nhiều điểm nhìn, camera quan sát, thử GPU occlusion culling

> Trạng thái: Chốt
> Ngày: 2026-10-22
> Thay đổi: world bước 7b ([world-tech](../04-tech/world-tech.md#kế-hoạch-prototype-m1)) — nạp chunk quanh **một** camera → quanh **nhiều điểm nhìn**.

## Bối cảnh
User muốn (2026-10-22):
- Nạp chunk theo điểm nhìn, không nhất thiết là người chơi.
- Ví dụ: đặt một camera ở Map 2 khi đang ở Map 1, rồi nhìn qua camera đó xem Map 2 có gì xảy ra.
- Hỏi thêm về hai lớp culling.

Hiện trạng [repo / run]:
- `ChunkStreamPlan` (brief 0010) chỉ dựng mesh và collider quanh **một** tâm: nạp ≤ 128, gỡ > 160.
- Dữ liệu ô cả world (13,5 MB) nằm trong RAM.
- Đo trên điện thoại: 45,6 FPS khi bay.
- Mô phỏng sinh vật chỉ chạy trong vùng 64 × 64 ở Map 1 (world 7e).

Về culling [docs]:
- **Frustum culling:** Unity tự loại renderer ngoài khung nhìn.
- **Occlusion culling kiểu bake:** không phù hợp với hình học sinh lúc chạy ([docs](https://docs.unity3d.com/6000.4/Documentation/Manual/OcclusionCulling.html)).
- **GPU occlusion culling:** cần GPU Resident Drawer + Forward+, trên API có compute shader, không chạy trên OpenGL ES ([docs](https://docs.unity3d.com/6000.4/Documentation/Manual/urp/gpu-resident-drawer.html), [docs](https://docs.unity3d.com/6000.4/Documentation/Manual/urp/gpu-culling.html)).

## Các lựa chọn
- **Làm đủ camera quan sát + mô phỏng ở map khác ngay M1:** cần tìm đường toàn world (đã xếp M2, 2–3 ngày) và nhiều vùng mô phỏng → vượt xa quỹ M1.
- **M1 làm phần nền + camera quan sát tối giản, mô phỏng map khác để M2** (user chọn theo đề xuất).

## Quyết định
User, 2026-10-22:
1. **Nạp chunk theo nhiều điểm nhìn.**
   - Bộ nạp nhận một danh sách điểm nhìn, mỗi điểm có bán kính nạp/gỡ riêng. Cột cần nạp là hợp các vùng.
   - **M1: tối đa 2 điểm nhìn** (nhân vật + 1 camera quan sát đang được xem).
   - **Sau này nâng lên 4, tối đa 5 điểm nhìn, tính cả người chơi** (user). Số điểm tối đa là cấu hình, không viết cứng. Thiết kế dữ liệu và ngân sách dựng mesh mỗi khung phải chừa chỗ cho 5.
   - Đo điện thoại ở 2 điểm (M1). Đo ở 5 điểm khi nâng cấp.
2. **Camera quan sát tối giản (M1):**
   - Đặt bằng một công cụ: lấy vị trí và hướng đang nhìn. M1 tối đa 1 camera.
   - Bấm nút → màn hình chuyển sang nhìn qua camera đó (toàn màn hình) → bấm lại để quay về.
   - Khi đang xem, camera đó là điểm nhìn thứ hai của bộ nạp.
3. **Mô phỏng sinh vật ở map khác:** không làm ở M1. Nhìn qua camera ở map khác lúc này chỉ thấy địa hình, nước, dấu mốc. Xem nhiều camera cùng lúc (khung nhỏ trong màn hình) cũng không làm ở M1.
4. **Thử GPU occlusion culling.**
   - Bật GPU Resident Drawer + Forward+, đo trên Mac và điện thoại khi có và không có culling.
   - Không lợi, hoặc không chạy được trên điện thoại → tắt và ghi kết quả. Frustum culling giữ như mặc định.

## Hệ quả
- **Pillar:** P3 "Tự do trong khung truyện" — theo dõi nhiều nơi trong world rộng mà không phải đi lại. Phần nền nạp nhiều điểm nhìn phục vụ cả nhân vật (0028).
- **Thành phần:** client Unity. `ChunkStreamPlan` ở `Game.Core`; camera và giao diện ở client.
- **Ước lượng (của AI, chưa xác minh) → M1:**

  | Phần | Ngày-người |
  |---|---|
  | Nạp nhiều điểm nhìn + đo điện thoại | 1–1,5 |
  | Camera quan sát tối giản | 1–2 |
  | Thử GPU occlusion culling + đo | 1–2 |
  | **Tổng** | **3–5,5** |

- **Ước lượng → M2:**
  - nâng lên 4–5 điểm nhìn + đo: 1–2 [ý tưởng ước lượng];
  - mô phỏng ở map khác: nằm trong "tìm đường toàn world" đã có.
- **Phạm vi M1:** cùng [0028](0028-nhan-vat-tui-vat-roi-tu-trong-trot.md), M1 tăng từ 86,5–141,5 lên **101,5–167 ngày-người**, vượt quỹ cũ. User chấp nhận 2026-10-22. Nếu cần cắt: bỏ camera quan sát + GPU culling khỏi M1 trước, giữ nạp nhiều điểm nhìn.
- **Cập nhật:** [world-tech](../04-tech/world-tech.md) bước 7i–7k, [roadmap](../05-roadmap.md), [glossary](../glossary.md), [ideas](../ideas.md).
