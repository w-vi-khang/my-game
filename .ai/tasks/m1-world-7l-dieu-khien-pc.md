# Task: M1 world 7l — điều khiển PC kiểu click di chuyển (0034)

> Model thực thi: Sonnet · medium (AGENTS.md §10.2); 7l a (luật lõi) và 7l b (camera) có thể cần Opus khi viết brief.
> Trạng thái: user xác nhận 2026-10-23. Quyết định gốc: [0034](../../design/decisions/0034-dieu-khien-pc-click-di-chuyen.md) (Chốt, user 2026-10-23). Dòng 7l a–f trong [world-tech](../../design/04-tech/world-tech.md#kế-hoạch-prototype-m1). Gộp phần còn lại của [7j](m1-world-7j-camera-quan-sat.md) (brief 0053). Ước lượng 14–20 ngày-người. Dự kiến 6 brief, mỗi brief một bước a–f, làm tuần tự.

## Mục tiêu
Làm lại điều khiển PC:
- chuột trái để đi; camera kiểu LoL;
- phím chế độ + chuột phải để làm việc trong tầm, nhân vật tự đi tới;
- cầm vật phẩm từ túi;
- máu + mana;
- túi / túi phụ / kho ô vuông, chuyển đồ qua lại;
- HUD gọn;
- đổi mọi phím.

- **Pillar:** không thêm cơ chế chơi mới, phục vụ ràng buộc nền tảng PC; đào/đặt tốn mana gắn P1 (0034 Hệ quả).
- **Thành phần:** client Unity. Luật mới ở `Game.Core`, số ở `design/data/*.json`.

## Ngoài phạm vi
- Cảm ứng, tay cầm: giữ joystick + nhắm theo tâm.
- Phím M2: nhập hồn, ném thẻ, khoá mục tiêu, phép khi nhập hồn.
- Nâng cấp tầm đào, mở thêm ô túi ([ideas](../../design/ideas.md)).
- Ưu đãi nạp "túi +10%" (đã chốt là ngoại lệ, 0034): không làm ở M1 (chưa có tiền nạp).
- Art thật cho icon: M1 dùng icon giữ chỗ dựng lúc chạy (ô màu vật liệu, hình đơn giản).
- Camera bay debug (F1–F3…): giữ như cũ.

## Tiêu chí chấp nhận
Mỗi bước đạt đúng cột "Xong khi" của dòng 7l a–f trong world-tech. Chung cho mọi bước:
- [ ] Mọi phím mới đi qua `GameControls.inputactions` (0033); test EditMode bảng phím mặc định khớp bảng ở [controls-ui](../../design/02-gameplay/controls-ui.md); không trùng phím trong cùng ngữ cảnh.
- [ ] Số [số tạm] của 0034 nằm trong JSON (`design/data/`), không viết cứng.
  - Gồm: tầm 10/2, mana, máu, lướt, túi 10/2, 999/ô, góc + khoảng cách camera mặc định.
  - Số cảm giác camera đi qua Cài đặt (0032).
- [ ] Chữ hiển thị mới đi qua bảng chuỗi (0032).
- [ ] `Game.Core` không tham chiếu UnityEngine.
- [ ] Test cũ còn đúng ý nghĩa: test nào dùng WASD / Tab / phím cũ thì sửa theo phím mới, ghi danh sách trong brief. EditMode + PlayMode qua hết, 0 lỗi console.
- [ ] Điện thoại ≥ 30 FPS sau 7l e (icon ô ruộng + sinh vật).
- [ ] User xem trên Mac sau mỗi bước có "user xem".

## Ràng buộc
- Unity 6000.4.8f1, Input System 1.19, URP 17.4.
- Planner tra docs đúng version cho từng API và ghi link vào brief. Các API cần tra:
  - raycast con trỏ;
  - `PerformInteractiveRebinding`;
  - kéo thả uGUI (`IDragHandler`…);
  - Cinemachine nếu dùng cho camera mới.
- Raycast con trỏ lên world dùng lưới voxel (như tia đào/đặt hiện có), không cần collider mới.
- Đi thẳng, không tìm đường (user: "đi về hướng đó tới khi không đi được nữa"). Kẹt = không tiến được [số tạm] 0,3 giây → dừng.
- Mô phỏng nông trại vẫn theo nhịp; lệnh người chơi vào giữa hai nhịp như 0028.
- Thứ tự brief: a → b → c → d → e → f.
  - a là nền cho c, d.
  - b, c thay `PlayerCharacter` / `PlayerFarmActions` / `TerrainEditInput`.
  - f làm cuối, khi danh sách hành động đã đủ.

## Quyết định đã chốt (ai chốt, khi nào)
- Toàn bộ mục Quyết định của 0034: user, 2026-10-23 (13 câu + 3 câu bổ sung).
- Số [số tạm] và các điểm ghi "Planner" trong 0034 do Planner đề xuất, user sửa được khi xem từng bước:
  - phím G mở menu phép, Shift lướt;
  - lướt 4 ô / hồi 3 giây;
  - mana 100 (đào 5, đặt 3, Mưa 15, hồi 2 mỗi ô);
  - máu 100, rơi > 4 ô;
  - ngập đầu 5/giây;
  - hồi máu, hồi sinh khi máu về 0;
  - kho tự đóng khi xa > 10 ô;
  - xới: chuột phải lên biển = tự đi tới xới;
  - camera 55° / 14 ô → 10° / 9 ô sau chơi thử 7l b ([0035](../../design/decisions/0035-camera-quan-sat-dat-tren-o-xoay-khi-xem.md), brief 0057);
  - làm tròn lên khi chia chồng.
- Brief 0053 (camera quan sát) không duyệt riêng, gộp vào 7l c: user, 2026-10-23.
- Bản sửa 7l b (brief 0057, 0035): camera quan sát đặt trên ô + xoay khi xem làm sớm; 7l c chỉ còn đổi thành vật phẩm trong túi + tự đi tới 2 ô.
- Ghi chú cho brief 7l c (từ review 0055/0056): nối máu/mana vào client (AddMoved, rơi, ngập — chưa bước nào nhận); nút "Bỏ đánh dấu" (phím Cancel đã bỏ ở 0055); công cụ đang tạm ở chuột phải; giữ chuột trái vào tường không chuyển Blocked.

## Câu hỏi còn mở
_(không)_ — Xới: bỏ đánh dấu bằng nút "Bỏ đánh dấu" trong bảng khi click trái lên biển (user đồng ý 2026-10-23).
