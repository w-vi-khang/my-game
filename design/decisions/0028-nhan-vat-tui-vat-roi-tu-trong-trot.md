# 0028 — Nhân vật người chơi, túi đồ + vật rơi, người chơi tự trồng trọt, phép Mưa (M1)

> Trạng thái: Chốt
> Ngày: 2026-10-22
> Thay đổi: luật M1 "Đào → kho vật liệu" ([mechanics](../02-gameplay/mechanics.md#voxel-world--luật-m1)) — thay bằng túi đồ; [0025](0025-nguoi-choi-xoi-dat-chon-giong-hang-doi.md) — bổ sung (người chơi tự làm được, sinh vật vẫn làm như cũ); [0020](0020-o-ruong-tao-khi-xoi-va-huy-vu.md) — "phép tưới nước (ô đất → cỏ) giữ M2" chuyển thành phép Mưa ở M1.

## Bối cảnh
User 2026-10-22, trước sim bước 8 (môi trường sống, tạm hoãn):
- Phá khối thì rơi ra bản mini, người chơi tự nhặt vào túi, túi khác kho.
- Người chơi tự làm được cả chuỗi trồng trọt; chỉ cần sinh vật biết tưới (hoặc phép Mưa).

Hiện trạng [repo]:
- Người chơi chỉ là camera bay (`DebugFlyCamera`).
- Vật liệu đào ra vào một bộ đếm "kho vật liệu".
- Sinh vật làm mọi công đoạn trồng trọt.
- Chưa có hệ phép.
- World M1 chưa có dung nham.

## Các lựa chọn
- **Vị trí người chơi:**
  - Giữ camera bay, tính tầm với quanh điểm dưới camera: rẻ, nhưng "đi lại kho", "hút xung quanh" không tự nhiên.
  - **Nhân vật đơn giản** (user chọn): thêm 3–5 ngày-người, nhưng mọi tương tác có vị trí rõ.
- **Phép Mưa:**
  - Có cơ chế học phép: kéo theo hệ phép/tiến trình, M2.
  - **Có sẵn từ đầu, có thời gian hồi** (user chọn theo đề xuất).

## Quyết định
User, 2026-10-22 (chọn "theo đề xuất" cho các mục Planner đề xuất). Số là [số tạm], ghi trong JSON, chỉnh bằng `game-balance`.

1. **Nhân vật đơn giản**
   - Hình: ghép vài khối (thân + đầu), cao 2 ô, rộng 1 ô; không animation, chỉ nhún nhẹ khi đi.
   - Góc nhìn thứ ba sau lưng.
   - Điều khiển PC: WASD đi, chuột xoay, Space nhảy (lên bậc 1 ô).
   - Điều khiển cảm ứng: joystick ảo + kéo để xoay (theo bảng [điều khiển](../02-gameplay/controls-ui.md#điều-khiển)).
   - Va chạm với khối.
   - Camera bay giữ làm **chế độ debug** (phím bật/tắt).
2. **Tầm với** tính quanh nhân vật:
   - nhặt, xới, gieo, thu hoạch, phép Mưa: trong **4 ô**;
   - mở kho: đứng cách kho ≤ **3 ô**.
3. **Vật rơi:**
   - Đào một ô thì ô thành rỗng và **rơi ra bản mini của khối** (vật rơi) tại chỗ; vật liệu không tự vào túi.
   - Vật rơi nằm yên trên mặt đất, không tự mất.
   - Nhiều vật cùng loại trong một ô gộp thành một chồng có số lượng.
4. **Nhặt:**
   - **hành động Thu** hút mọi vật rơi trong tầm vào túi;
   - hoặc **click vào một vật rơi** trong tầm để nhặt riêng nó.
5. **Túi đồ** (của người chơi) **khác kho**:
   - Thay "kho vật liệu" của luật M1: đặt khối thì lấy từ túi.
   - Túi **không có mức tối đa** (theo [0004](0004-tien-nap.md)).
6. **Túi hạt:**
   - Đầu game có **một túi hạt Cây A (4 hạt)** nằm trên mặt đất gần chỗ xuất phát để người chơi nhặt.
   - Túi hạt là vật phẩm trong túi đồ, mỗi túi hạt giữ số hạt của một loại cây.
7. **Người chơi tự xới:** dùng công cụ Xới.
   - Click ô đã cắm biển canh tác → nhân vật **tự đi tới** rồi xới. Thời gian xới bằng công đoạn xới của sinh vật thuần thục mức gốc.
   - Xới xong, ô thành ô ruộng như khi sinh vật xới.
8. **Người chơi tự gieo:**
   - Mở túi → chọn túi hạt → vào **chế độ gieo** → click ô ruộng đã xới và chưa có cây (trong tầm) → gieo, trừ 1 hạt của túi hạt đó.
   - Ngoài chế độ gieo, click ô ruộng vẫn mở **bảng ô ruộng** (chọn giống cho sinh vật, [0027](0027-o-tien-trinh-tam-dung-xoi-khong-tra-hat.md)).
9. **Tưới:**
   - Người chơi không tưới bằng tay; cần sinh vật biết tưới hoặc **phép Mưa**.
   - **Phép Mưa** có sẵn từ đầu, nhắm **1 ô** trong tầm, thời gian hồi **10 giây**:
     - ô ruộng đang chờ tưới → tưới xong;
     - ô đất → thành cỏ.
10. **Thu hoạch:**
    - Cây lớn 100% → người chơi click vào → **nông sản + hạt vào túi**, theo luật M1: 1 nông sản + 1 hạt; hạt cộng vào túi hạt cùng loại.
    - Sinh vật thu hoạch thì vẫn như cũ (hàng nằm tại ô, chở về kho).
11. **Kho ↔ túi:** đứng trong tầm kho → mở kho → chuyển đồ từ túi vào kho hoặc từ kho ra túi. Sức chứa kho giữ như 0025: các kho thông nhau, cộng dồn.
12. **Sinh vật vẫn làm như cũ** (0025–0027): người chơi tự làm là thêm cách, không thay sinh vật.
13. **Thứ tự làm:**
    - nhân vật;
    - vật rơi + túi + nhặt + túi hạt;
    - tự trồng trọt + phép Mưa + kho ↔ túi;
    - nạp nhiều điểm nhìn ([0029](0029-nhieu-diem-nhin-camera-quan-sat.md));
    - quay lại môi trường sống (sim bước 8).

## Hệ quả
- **Để [ideas](../ideas.md):** mưa làm dung nham nguội thành đá (world M1 chưa có dung nham); cơ chế học phép (M2).
- **Pillar:** P2 "Sinh vật là cốt lõi" — người chơi tự làm được từng việc nhỏ, còn quy mô lớn vẫn cần sinh vật. P1 qua phép Mưa (ô đất → cỏ là cải tạo môi trường).
- **Thành phần:** client Unity. Luật ở `Game.Core`: túi, vật rơi, lệnh của người chơi vào mô phỏng. Nhân vật, camera, giao diện ở client.
- **Ước lượng (của AI, chưa xác minh) → M1:**

  | Phần | Ngày-người |
  |---|---|
  | Nhân vật | 3–5 |
  | Vật rơi + túi + nhặt + túi hạt | 3–5 |
  | Tự xới/gieo/thu + chế độ gieo + phép Mưa + kho ↔ túi | 6–10 |
  | **Tổng** | **12–20** |

- **Cập nhật:**
  - [mechanics](../02-gameplay/mechanics.md) (luật M1 Đào/Đặt, mục mới);
  - [controls-ui](../02-gameplay/controls-ui.md);
  - [world-tech](../04-tech/world-tech.md) bước 7h, [simulation-tech](../04-tech/simulation-tech.md) bước 7e, 7f;
  - [roadmap](../05-roadmap.md), [glossary](../glossary.md), [ideas](../ideas.md).
- **Chỗ đổi trong test cũ:** test đào/đặt dùng kho vật liệu phải đổi sang túi + vật rơi.
