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
- [ ] User xem trên Mac sau mỗi bước có "user xem" (từ 7l d: gộp một lần ở cuối — user 2026-10-24).

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
- 7l c chia 3 brief, làm lần lượt (user, 2026-10-23): **c1** chế độ D/Q/E/R + chuột phải tự đi tới 2 ô rồi làm + tô đỏ khối + vòng tầm + F Thu 2 ô + click trái ô ruộng (nút Bỏ đánh dấu); **c2** cầm vật phẩm (khối, hạt, camera quan sát); **c3** nối máu/mana vào client.
- c1 (brief 0058): Đặt khối + camera quan sát giữ tạm bằng Tab vòng Không → cầm khối → cầm camera → Không; Đào/Xới bỏ khỏi Tab (thành Q/E); c2 xoá Tab tạm (user, 2026-10-23).
- Sau chơi thử c1 (user, 2026-10-24, [0036](../../design/decisions/0036-sinh-vat-chi-lam-cong-doan-duoc-giao.md) mục 5–6): c2 thêm vòng tầm chỉ hiện khi chuột phải ngoài tầm (mờ dần) và F hiện vùng 2 ô + gió hút vật rơi về nhân vật. Luật "sinh vật chỉ làm công đoạn được giao" (0036 mục 1–4) là sim 7g, task riêng sau 7l c — ngoài phạm vi task này.
- Trước 7l d, cầm vật phẩm tạm qua bảng túi cũ (B → bấm vật phẩm); khối tạm chọn loại bằng phím 1–4 như hiện nay; 7l d thay bằng túi ô vuông (user, 2026-10-23).
- **Làm hết mục 7 một lượt, chơi thử gộp cuối** (user, 2026-10-24, nguyên văn: "Hãy hoàn thành mục 7 một lượt"; chọn phạm vi 7l d, e, f + sim 7g; chọn "Gộp cuối cùng"): phần "user xem" của mọi bước còn lại gom thành **một danh sách chơi thử ở cuối**. Mỗi brief vẫn ghi mức review "đầy đủ + user xem", nhưng mục "User tự test" chỉ liệt kê điều cần xem để Planner chép vào danh sách cuối; review từng brief kết luận "code ok, chờ user xem gộp cuối", không chờ user.
- 7l d chia 3 brief, làm tuần tự (Planner, 2026-10-24): **d1** [0061](../briefs/0061-7l-d1-tui-o-vuong-loi.md) `PlayerBag` thành túi ô vuông thật (túi 10 + túi phụ 2, 999/ô, túi đầy → vật nằm lại, camera quan sát chiếm 1 ô); **d2** [0062](../briefs/0062-7l-d2-giao-dien-tui-tui-phu-kho.md) giao diện uGUI túi / túi phụ / kho (B, P, kho mở kèm túi + tự đóng > 10 ô, hình + số, trang, bảng thông tin, chuột phải ô → cầm, 1–0; bỏ bảng IMGUI cũ, K, 1–4, Alt-lăn; dọn `bag.empty` + chuỗi cũ); **d3** [0063](../briefs/0063-7l-d3-chuyen-do-keo-tha-tab-123.md) kéo thả cả chồng + chọn ô → Tab → 1/2/3.
- Đề xuất Planner trong 7l d (user xem ở lần chơi thử cuối, đổi được): thêm vào túi phụ cộng dồn trước, đồ mới vào túi trước; lấy theo loại từ túi trước, túi phụ sau; thu hoạch khi túi đầy → vật rơi trên ô ruộng; mở kho tự mở túi; kéo thả + bắt con trỏ tự làm (không EventSystem — để mọi phím qua `GameControls`, 0033); ngưỡng kéo 8 px; bố cục / cỡ ô / màu giữ chỗ.

## Câu hỏi còn mở
_(không)_ — Xới: bỏ đánh dấu bằng nút "Bỏ đánh dấu" trong bảng khi click trái lên biển (user đồng ý 2026-10-23).
