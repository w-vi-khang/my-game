# 0023 — Dấu mốc trên bản đồ là công trình đặc trưng của map + checkpoint; dịch chuyển tới checkpoint + đường chỉ dẫn để M2

> Trạng thái: Chốt
> Ngày: 2026-10-20
> Thay đổi: [0022](0022-world-nhieu-map-va-minimap-m1.md) mục 5, phần "dấu mốc": trước đây ghi là nhà, kho, ô ruộng ([controls-ui](../02-gameplay/controls-ui.md), world-tech bước 7d), nay đổi thành **công trình đặc trưng của từng map + checkpoint**. Thêm tính năng **dịch chuyển tới checkpoint + đường chỉ dẫn** vào M2.

## Bối cảnh
Bước 7c (minimap) đã duyệt ngày 2026-10-20 (brief 0015, 0016). Bước tiếp theo trong kế hoạch là 7d: "minimap cập nhật khi đào/đặt + dấu mốc (nhà, kho, ô ruộng)", ước 0,5–1 ngày-người.

User (2026-10-20): "Không phải thêm dấu mốc cho nhà, kho, ô ruộng, mà thêm cho một số loại công trình nhất định đặc trưng cho map đó, và điểm check point trong mỗi map. Khi muốn tìm đường đi tới một điểm trong map, player sẽ được dịch chuyển tới chỗ check point trước nếu điểm cần đến ở gần check point hơn chỗ hiện tại của player, [rồi] hiện một đường chỉ dẫn đường tới chỗ cần đến."

Hiện trạng:
- World M1 chưa có công trình nào [repo] `client/Assets/Game/World/WorldMapGenerator.cs`.
- M1 chưa có nhân vật: "người chơi" là camera bay [repo] `client/Assets/Game/Client/DebugFlyCamera.cs`.
- Bộ tìm đường `GridPathfinder` cấp mảng theo số ô của cả world; với world v3 thì tốn khoảng 85 MB. Đây là rủi ro đã ghi ở 0022.
- Roadmap đang vượt quỹ thời gian ([roadmap](../05-roadmap.md)).

## Các lựa chọn
Ước lượng của AI, chưa đo:
- **Milestone:**
  - (a) Tách: dấu mốc (công trình + checkpoint) làm ở M1 trong bước 7d; dịch chuyển + dẫn đường để M2.
  - (b) Làm hết ở M1, thêm 5–8 ngày-người.
  - (c) Để hết sang M2; 7d chỉ còn phần bản đồ cập nhật khi đào/đặt.
- **Công trình ở M1:**
  - (a) Khối giữ chỗ do bộ sinh đặt.
  - (b) Chỉ có biểu tượng trên bản đồ.
  - (c) Chưa làm công trình.
- **Checkpoint:**
  - (a) Mỗi map 1 checkpoint, mở sẵn.
  - (b) Mỗi map 1 checkpoint, phải đi tới mới mở.
  - (c) Mỗi map nhiều checkpoint, phải đi tới mới mở.
- **Dẫn đường:**
  - (a) Tìm đường đi bộ thật, vẽ cả trong world lẫn trên bản đồ.
  - (b) Chỉ vẽ đường thẳng chỉ hướng; rẻ hơn khoảng 2–3 ngày.
  - (c) Tìm đường đi bộ thật nhưng chỉ vẽ trên bản đồ.

## Quyết định
User chọn (2026-10-20), cả 4 câu đều chọn phương án đầu tiên:
1. **Milestone: tách.** Ở M1, bước 7d gồm: bản đồ cập nhật khi đào/đặt, cộng dấu công trình và checkpoint (trên bản đồ và trong world). Dịch chuyển + đường chỉ dẫn để sang **M2**.
2. **Công trình ở M1 là khối giữ chỗ do bộ sinh đặt.**
   - Mỗi map có vài loại công trình đặc trưng. Danh sách loại, tên tạm và số lượng nằm trong `design/data/world_maps_m1.json`. Tên thật do user đặt sau qua [naming](../01-story/naming.md).
   - Vị trí được chọn theo seed, có tất định.
   - Trong world, mỗi công trình là một khối đánh dấu đơn giản. Trên bản đồ, mỗi loại có biểu tượng riêng.
3. **Checkpoint: mỗi map 1 cái, mở sẵn.** Khi dẫn đường, dùng checkpoint gần điểm đến nhất.
4. **Dẫn đường (M2):**
   - Người chơi chọn điểm đến bằng cách chạm hoặc bấm trên bản đồ.
   - So khoảng cách theo đường chim bay. Nếu điểm đến gần checkpoint gần nó nhất hơn là gần người chơi, người chơi được dịch chuyển tới checkpoint đó trước.
   - Sau đó tìm đường đi bộ thật trên mặt đất (lên/xuống mỗi bước tối đa 1 ô, tránh nước sâu) và vẽ đường đó cả trong world lẫn trên bản đồ.

Cách làm kỹ thuật (Planner đề xuất, chốt từng phần khi viết brief):
- Vị trí công trình và checkpoint được tính **sau khi sinh world**, chỉ từ dữ liệu world, bảng map và seed. Chỉ dùng phép toán số nguyên, bộ số ngẫu nhiên PCG32.
- Không đổi ô của world, nên mã băm world v3 và bảng map giữ nguyên, không cần đo lại trên điện thoại.
- Khối giữ chỗ trong world là vật thể hiển thị của client, không phải ô voxel. Vì vậy chưa có luật "không đào được": đào dưới chân thì khối đánh dấu vẫn nằm nguyên chỗ cũ. Luật này gắn với **khối cốt truyện** ở M2 ([mechanics](../02-gameplay/mechanics.md)).

## Hệ quả
- **Pillar:** **P3** (mỗi map có công trình đặc trưng riêng, đi lại giữa các map dễ hơn). Dẫn đường ở M2 phục vụ P3, đi lại tự do.
- **Thành phần:** client Unity ([components](../04-tech/components.md)); không thêm thành phần mới.
- **Ước lượng** của AI (chưa đo):
  - M1, bước 7d: 0,5–1 → **1,5–2,5**. Gồm: bản đồ cập nhật khi đào/đặt 0,5; chọn vị trí công trình + checkpoint (lõi, test) 0,5–1; hiển thị trên bản đồ và trong world 0,5–1.
  - M2: thêm **3,5–6**. Gồm: chọn điểm trên bản đồ + dịch chuyển 0,5–1; tìm đường đi bộ trên toàn world (cấp bộ nhớ theo vùng, xem rủi ro của 0022) 2–3; vẽ đường chỉ dẫn trong world và trên bản đồ 1–2.
- **Roadmap:** M1 thêm 1–1,5; M2 thêm 3,5–6 ([roadmap](../05-roadmap.md)). Roadmap vẫn đang vượt quỹ.
- **Đã cập nhật:** [world-tech](../04-tech/world-tech.md) (dòng 7d), [controls-ui](../02-gameplay/controls-ui.md) (dòng Bản đồ), [glossary](../glossary.md) (Checkpoint, Công trình đặc trưng), [roadmap](../05-roadmap.md), [decisions/README](README.md), task [world bước 7](../../.ai/tasks/m1-world-buoc-7-nhieu-map-minimap.md).
