# Task: M1 sim bước 7f — người chơi tự trồng trọt + phép Mưa + kho ↔ túi

> Model thực thi: Sonnet · medium  (AGENTS.md §10.2)
> Trạng thái: **xong 2026-10-22** (user xem đạt). User xác nhận đề xuất 1–11 + xới 6 giây (2026-10-22). Brief: 0040 (lõi), 0041 (client A), 0042 (client B), 0043 (sửa HUD) — đã duyệt. Bước 3 của [0028](../../design/decisions/0028-nhan-vat-tui-vat-roi-tu-trong-trot.md) mục 13; tầm với theo [0031](../../design/decisions/0031-tam-voi-moi-thao-tac-8-o.md). Trước: sim 7e (brief 0036–0038), world 7h (b) (brief 0039) — đã duyệt. Sau: world 7i–7k ([0029](../../design/decisions/0029-nhieu-diem-nhin-camera-quan-sat.md)).

## Mục tiêu
Người chơi tự làm được cả chuỗi trồng trọt, sinh vật vẫn làm như cũ (0028 mục 7–12):
- **Tự xới:** cầm Xới, click ô có biển → nhân vật tự đi tới rồi xới.
- **Tự gieo:** mở túi → chọn túi hạt → chế độ gieo → click ô ruộng đã xới → gieo, trừ 1 hạt.
- **Phép Mưa:** có sẵn, nhắm 1 ô trong tầm, hồi 10 giây. Ô ruộng chờ tưới → tưới xong; ô đất → cỏ.
- **Tự thu hoạch:** cây chín → click → nông sản + hạt vào túi.
- **Kho ↔ túi:** đứng trong tầm kho → mở kho → chuyển đồ hai chiều.

Thông tin chung:
- **Pillar:** P2 "Sinh vật là cốt lõi" — người chơi làm việc nhỏ, quy mô lớn cần sinh vật. P1 qua phép Mưa (ô đất → cỏ).
- **Thành phần:** client Unity.
  - Luật ở `Game.Core` (test EditMode): lệnh người chơi vào mô phỏng, túi (thêm nông sản), chuyển kho ↔ túi, luật Mưa + thời gian hồi, bộ đếm xới, tìm đường cho nhân vật.
  - Ở client: tự đi tới, bảng túi, chế độ gieo, phím Mưa, click thu hoạch, bảng kho có nút chuyển, HUD.
- **Ước lượng:** 6–10 ngày-người (0028). Thuộc M1.

## Ngoài phạm vi
- Cảm ứng (nút Mưa, chạm gieo/thu, joystick) → task "Điều khiển cảm ứng" sau.
- Người chơi tưới bằng tay; cơ chế học phép (M2); mưa làm dung nham nguội (ideas).
- Người chơi nhận việc qua tab nhiệm vụ (M2, 0014); tự xới ô **chưa** cắm biển (phải đánh dấu trước).
- Cất khối (đất, đá…) vào kho: kho M1 chỉ chứa hạt + nông sản (0025).
- Dùng nông sản (bán, ăn, nấu…): chưa có luật.
- Lưu/nạp túi, kho: M1 chưa có lưu game.
- Âm thanh, animation nhân vật khi làm việc (chỉ có chữ HUD + hiệu ứng hình tối giản ở đề xuất 6).
- Sinh vật đổi hành vi: không đổi (0028 mục 12).

## Đề xuất của Planner — user xác nhận 2026-10-22
1. **Lệnh người chơi vào mô phỏng** (theo [simulation-tech](../../design/04-tech/simulation-tech.md) "Người chơi làm công đoạn", user 2026-10-13):
   - Là hàm của `Simulation`, gọi giữa hai nhịp như `MarkTill`/`EnqueueCrop`; có tác dụng từ nhịp kế.
   - Công đoạn đang mở của ô → `Done`, `AssigneeKind = Player`, chạy đúng hậu quả như khi sinh vật làm xong (sang pha kế).
   - Sinh vật đang làm đúng công đoạn đó → về rảnh, mất tiến độ. Nếu là gieo: hạt kho đã giữ cho nó trả lại kho.
   - Công đoạn đã `Done` không bao giờ được giao lại cho sinh vật.
2. **Tự xới:**
   - Cầm Xới, click ô có biển (chưa xới) trong 8 ô → nhân vật **tự đi tới ô kề** ô đó (như sinh vật: đường do bộ tìm đường của mô phỏng, khung 1 × 1 × 2), tới nơi đứng xới.
   - **Thời gian xới 6 giây** = `baseTimeMs` của Xới trong `sim_m1.json` (tốc độ 1000‰ — mức của mọi loài M1 hiện nay, thuần thục 100; user chọn 2026-10-22).
   - Đang đi hoặc đang xới mà bấm WASD/Space, đổi công cụ, hay dùng công cụ khác → huỷ, không giữ tiến độ dở.
   - Không có đường → HUD `Không tới được ô này`. Sinh vật xới xong trước → người chơi dừng, không lỗi.
   - Ô ruộng **xới lại** (đã xới, đang ở pha xới): bảng ô ruộng thêm nút `Tự xới` (cùng cách đi tới + xới).
   - **Bỏ đánh dấu** chuyển từ chuột trái sang **chuột phải** vào ô có biển (chuột phải = Huỷ, [controls-ui](../../design/02-gameplay/controls-ui.md)).
   - Đồng hồ xới chạy theo thời gian thực ở client; luật đếm nằm ở Core (lớp thuần, test được).
3. **Túi đồ thêm nông sản** theo loại cây (cạnh khối và túi hạt); có lấy ra (hạt, nông sản) để gieo và cất kho.
4. **Bảng túi:** phím **B** mở/đóng.
   - Liệt kê khối, túi hạt, nông sản (dòng nào 0 thì ẩn).
   - Click một túi hạt còn hạt → vào chế độ gieo loại đó, bảng đóng.
   - Con trỏ trên bảng không đào/đặt (như bảng ô ruộng).
5. **Chế độ gieo:**
   - HUD: `Gieo Cây A (còn N) — chuột phải / Esc để thoát`. Trong chế độ gieo, chuột trái không đào/đặt/xới; Tab cũng thoát.
   - Click ô ruộng trong 8 ô, gieo ngay (không đi tới, không có thời gian):
     - ô đã xới, chưa có vụ (đứng yên) → vụ mới loại đã chọn, sang pha tưới. Hàng đợi của ô giữ nguyên, vụ kế lấy từ hàng đợi như cũ;
     - ô đang chờ sinh vật gieo **cùng loại** → gieo xong bằng hạt túi (đề xuất 1);
     - ô đang chờ gieo **khác loại**, ô chưa xới, ô đã có cây → không gieo, HUD nêu lý do (vd `Ô đang chờ gieo Cây B`).
   - Hết hạt → tự thoát chế độ gieo.
6. **Phép Mưa:**
   - Phím **R**, nhắm ô dưới con trỏ (ô sẽ đào), trong 8 ô.
   - Ô ruộng đang chờ tưới → tưới xong (đề xuất 1), cây bắt đầu lớn.
   - Ô đất có ô trên rỗng → thành cỏ (sửa qua `WorldEditor` để mesh, mô phỏng, minimap cập nhật như khi đào/đặt). Ô có biển trên ô đất vẫn giữ (ô cỏ vẫn đánh dấu được).
   - Ô khác → không có tác dụng, **không tính hồi**, HUD nêu lý do.
   - Hồi 10 giây thời gian thực, số trong `sim_m1.json` (số tạm). HUD: `Mưa (R): sẵn sàng` / `Mưa (R): 7 giây`.
   - Hình: một hộp xanh nhạt trong suốt trên ô, 0,5 giây.
7. **Tự thu hoạch:**
   - Cây chín 100%, click ô ruộng đó trong 8 ô với **mọi công cụ / chế độ** → thu hoạch thay cho công cụ (như click nhặt vật rơi; không đào nhầm cây chín).
   - Ngắm ô đó: dòng gợi ý `Thu hoạch Cây A`.
   - Túi + `producePerHarvest` nông sản + `seedsPerHarvest` hạt theo `sim_m1.json` (Cây A: 1 + 1 — đúng "1 nông sản + 1 hạt" của 0028; Cây B: 3 + 2). Hạt cộng vào túi hạt cùng loại.
   - Không có công đoạn chở về kho. Ô ruộng sau đó như khi sinh vật thu xong: thôi "đã xới", còn hàng đợi thì sang vụ kế, không thì đứng yên.
8. **Kho ↔ túi:**
   - Giữ nút `Kho (K)`. Có kho trong 8 ô (tâm thân tới tâm ô kho) → bảng kho có nút chuyển mỗi loại cây: hạt `→ túi` / `→ kho`, nông sản `→ túi` / `→ kho`. Mỗi click 1, Shift + click tất cả.
   - Click vào khối kho trong 8 ô cũng mở bảng.
   - Không có kho trong 8 ô → bảng chỉ xem, dòng `Lại gần kho (8 ô) để chuyển đồ`.
   - Túi → kho dừng khi kho chung đầy (chỗ trống trừ phần đã giữ cho hàng đang chở). Kho → túi lấy được mọi hạt/nông sản đang có.
   - Cất hạt vào kho → công đoạn gieo đang kẹt "hết hạt" tự mở lại như cũ.
9. **Tất định:** lệnh người chơi và chuyển kho đi vào trạng thái có trong mã băm mô phỏng → cùng chuỗi lệnh ở cùng nhịp → cùng mã băm.
10. **Số trong JSON:** thêm thời gian hồi Mưa vào `sim_m1.json` (tăng `configVersion`). Thời gian xới lấy từ `stageTimes` có sẵn.
11. **Tách 3 brief:**
    - **0040 — lõi Core:** API lệnh người chơi, túi thêm nông sản, kho ↔ túi, luật Mưa + hồi, bộ đếm xới, tìm đường cho nhân vật. Test EditMode. Review `đầy đủ`.
    - **0041 — client A:** tự đi tới + xới (gồm nút `Tự xới`, chuột phải bỏ biển), phép Mưa, click thu hoạch, HUD. PlayMode. Review `user xem`.
    - **0042 — client B:** bảng túi (B), chế độ gieo, bảng kho có nút chuyển. PlayMode. Review `user xem`.

## Tiêu chí chấp nhận (kiểm chứng được)
- [ ] Mỗi lệnh người chơi đổi đúng trạng thái ô ruộng / túi / kho, gồm các trường hợp bị từ chối ở đề xuất 5–7. Kiểm bằng: test EditMode.
- [ ] Sinh vật không làm lại công đoạn người chơi đã làm; sinh vật đang làm công đoạn đó về rảnh; hạt kho giữ cho gieo được trả. Kiểm bằng: test EditMode (chạy thêm nhiều nhịp sau lệnh).
- [ ] Chuỗi bằng tay không cần sinh vật: đánh dấu → tự xới → gieo → Mưa → chờ lớn → thu → túi +1 nông sản +1 hạt Cây A. Kiểm bằng: test EditMode.
- [ ] Mưa: ô đất → cỏ; ô khác không tác dụng và không tính hồi; biên hồi 10 giây. Kiểm bằng: test EditMode.
- [ ] Kho ↔ túi: chuyển hai chiều đúng số; túi → kho dừng khi đầy; kho → túi không âm. Kiểm bằng: test EditMode.
- [ ] Bộ đếm xới: xong đúng 6 giây, huỷ thì về 0. Tìm đường nhân vật: tới ô kề được; không có đường → báo không tới. Kiểm bằng: test EditMode.
- [ ] Tất định: cùng chuỗi lệnh → cùng mã băm. Kiểm bằng: test EditMode.
- [ ] PlayMode: tự đi tới + xới; chuột phải bỏ biển; gieo trong chế độ gieo; Mưa tưới + đất → cỏ + hồi; click thu hoạch; chuyển kho ↔ túi trong tầm, ngoài tầm không. Kiểm bằng: `run_tests` PlayMode (MCP).
- [ ] Toàn bộ EditMode + PlayMode cũ vẫn qua. Kiểm bằng: `run_tests` (MCP); Editor tắt thì `python3 tools/unity_test.py EditMode|PlayMode`.
- [ ] User xem trên Mac: trồng bằng tay trọn một vụ, Mưa, bảng túi, chế độ gieo, chuyển kho. Kiểm bằng: review `user xem` của brief 0041, 0042.
- [ ] Tài liệu: mechanics (luật người chơi tự trồng), controls-ui (B, R, chuột phải bỏ biển, chế độ gieo), glossary nếu có thuật ngữ mới, simulation-tech bước 7f trạng thái, roadmap giờ thực tế. Kiểm bằng: `python3 tools/check_design.py`.

## Ràng buộc
- `Game.Core` không tham chiếu UnityEngine/UnityEditor; luật ở Core, client chỉ gọi và vẽ.
- Mô phỏng dùng số nguyên (architecture "nền tảng code M1"); đồng hồ xới/Mưa ở client dùng thời gian thực nhưng luật đếm ở Core.
- Sinh vật làm như cũ (0025–0027); test sim cũ không đổi kỳ vọng.
- Unity Editor dùng chung qua MCP ([0010](../../design/decisions/0010-mcp-for-unity.md)). Sau PlayMode: `git checkout -- client/ProjectSettings/EditorSettings.asset`.
- API Unity dùng trong brief 0041/0042: Planner tra docs 6000.4 và ghi link.

## Quyết định đã chốt (ai chốt, khi nào)
- 0028 (user, 2026-10-22): tự xới (đi tới, thời gian như sinh vật mức gốc), chế độ gieo từ túi hạt, không tưới tay, phép Mưa 1 ô hồi 10 giây, thu hoạch vào túi, kho ↔ túi, sinh vật vẫn làm như cũ.
- 0031 (user, 2026-10-22): mọi thao tác 8 ô, gồm mở kho.
- simulation-tech "Người chơi làm công đoạn" (user, 2026-10-13): lệnh người chơi là "công đoạn xong tại ô", ghép việc bỏ qua công đoạn đó.
- Thứ tự: 7f sau 7h (b), trước 7i–7k (user, 2026-10-22).
- Đề xuất 1–11 của Planner, thời gian tự xới 6 giây (user "ok 6 giây", 2026-10-22).

## Câu hỏi còn mở
- (không còn)
