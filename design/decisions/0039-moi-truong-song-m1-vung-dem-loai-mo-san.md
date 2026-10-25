# 0039 — Môi trường sống M1: vùng đếm 48 × 48, chỉ mở sẵn L1, bảng yêu cầu mở bằng phím, ao viền cát

> Trạng thái: Chốt
> Ngày: 2026-10-25
> Thay đổi: [simulation-tech — Môi trường sống](../04-tech/simulation-tech.md#môi-trường-sống) "Vùng: M1 = cả map" và [mechanics — Sinh vật trở lại — luật M1](../02-gameplay/mechanics.md#sinh-vật-trở-lại--luật-m1) "xuất hiện ở rìa map, đi về phía người chơi" (user duyệt 2026-10-13, cả hai file đang ở trạng thái Đang xem xét).

## Bối cảnh
Brief [0069](../../.ai/briefs/0069-sim-8a-moi-truong-song-loi.md) (sim 8a, lõi) đo trên world M1 (seed 20261013, đếm cả world): 48.077 ô cỏ, 0 ô cỏ cạnh nước, 0 ô đá lộ thiên. Nếu đếm cả map, L1 (≥ 20 ô cỏ) đạt ngay từ đầu, nên việc mở khoá không còn là thử thách. Trước khi viết brief 8b, Planner hỏi user 3 câu ([task contract m1-sim-8](../../.ai/tasks/m1-sim-8-moi-truong-song.md)).

Sau khi user chọn vùng 48 × 48, Planner kiểm thêm: đồng bằng demo được san phẳng thành cỏ, đất và đá. Mặt đất cao hơn mực nước biển ít nhất 2 ô (`SimStress.FlattenPlain`), nên đào xuống cũng không ra nước. Như vậy L2 (≥ 10 ô cỏ cạnh nước) không bao giờ đạt. Planner hỏi thêm 2 câu.

## Các lựa chọn
- **Vùng đếm:** vùng 48 × 48 (Planner đề xuất) / cả map nhưng tăng ngưỡng / cả map, giữ ngưỡng.
- **Loài mở sẵn:** chỉ L1 (Planner đề xuất) / L1 + L2 / giữ cả L1–L3.
- **Chỗ hiện danh sách yêu cầu trên PC:** bảng riêng mở bằng phím (Planner đề xuất) / một tab trong Cài đặt / một nút dưới minimap.
- **Nước cho L2:** ao 4 × 4 viền cát (Planner đề xuất) / đổi yêu cầu của L2 / thêm cách cho người chơi tạo nước.
- **"Đi về phía người chơi":** xuất hiện ở rìa gần người chơi rồi tự về nhà (Planner đề xuất) / thêm lệnh mô phỏng "đi tới người chơi" (thêm khoảng 0,5 ngày).

## Quyết định
User chọn ngày 2026-10-25. Cả 5 câu đều theo đề xuất của Planner.
1. **Vùng đếm M1 = đồng bằng demo 48 × 48** giữa Map 1, đủ chiều cao. Vị trí đồng bằng được tính lúc sinh world, nên client gán vùng lúc chạy, không ghi cứng trong `habitat_m1.json`.
2. **Lúc bắt đầu demo chỉ có L1** (xới, gieo). L2 và L3 bị khoá cho tới khi đạt yêu cầu. Trong lúc đó người chơi tự tưới và tự thu hoạch (0028).
3. **Danh sách yêu cầu trên PC** nằm trong một bảng riêng, mở và đóng bằng một phím. Phím đổi được trong bảng đổi phím (7l f). Bảng liệt kê từng loài và từng yêu cầu dạng `đã/cần`, kèm dấu đạt.
4. **Ao 4 × 4 viền cát** đặt sẵn trong đồng bằng, xa trại. Vòng cát giống bờ hồ tự nhiên trong world. Muốn L2 đạt, người chơi đào cát, đặt đất rồi dùng phép Mưa để đất thành cỏ.
5. **Cá thể trở lại** xuất hiện ở ô rìa vùng đếm gần người chơi nhất. Sau đó chúng tự đi về nhà theo luật mô phỏng sẵn có (sinh vật rảnh ở ngoài bán kính nhà thì đi về), rồi nhận việc. Không thêm lệnh mô phỏng mới.

Đề xuất của Planner, đổi được, ghi [số tạm] / chưa chốt:
- Phím mở bảng mặc định là **H**.
- Trại demo có thêm nhà thứ ba, để 5 sinh vật (1 L1 + 2 L2 + 2 L3) đều có chỗ ở. Mỗi nhà chứa 2.
- Vị trí ao và vị trí, cỡ bảng: số tạm, user chỉnh sau khi chơi thử.

## Hệ quả
- simulation-tech "Môi trường sống" → dòng Vùng: M1 = đồng bằng demo 48 × 48 (0039).
- mechanics "Sinh vật trở lại — luật M1" → xuất hiện ở rìa vùng đếm gần người chơi rồi tự về nhà; demo bắt đầu chỉ có L1; có ao viền cát cho L2.
- `design/data/habitat_m1.json`: `unlockedAtStart` = `["L1"]`. Vùng trong JSON vẫn là −1 (cả world); client gán vùng = đồng bằng lúc chạy.
- Task contract m1-sim-8: 3 câu hỏi đã có trả lời. Sim 8b làm theo brief 0074.
