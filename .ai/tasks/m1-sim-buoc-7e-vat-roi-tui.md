# Task: M1 sim bước 7e — vật rơi + túi đồ + nhặt + túi hạt (gộp tầm với 10 ô)

> Model thực thi: Sonnet · medium  (AGENTS.md §10.2)
> Trạng thái: user xác nhận 10 đề xuất 2026-10-22. Brief: 0036 (lõi + tầm 10 ô), 0037 (client). Bước 2 của [0028](../../design/decisions/0028-nhan-vat-tui-vat-roi-tu-trong-trot.md), gộp [0030](../../design/decisions/0030-tam-voi-cong-cu-10-o.md). Trước: world 7h (a) — brief 0035 đã duyệt. Sau: world 7h (b), sim 7f.

## Mục tiêu
- **Đào không còn cộng thẳng vào kho vật liệu.** Ô đào thành rỗng và rơi ra **vật rơi**: bản mini của khối, nằm tại chỗ.
- Người chơi nhặt vật rơi vào **túi đồ** bằng hành động **Thu** hoặc **click** vào từng vật rơi, trong tầm 4 ô.
- Đặt khối lấy vật liệu từ túi đồ.
- Đầu game có một **túi hạt Cây A (4 hạt)** nằm gần chỗ xuất phát để nhặt.
- Gộp [0030](../../design/decisions/0030-tam-voi-cong-cu-10-o.md): đào/đặt/xới trong 10 ô; nhặt trong 4 ô.

Thông tin chung:
- **Pillar:** P2 — người chơi tự làm việc nhỏ, quy mô lớn vẫn cần sinh vật. Đây cũng là nền cho 7f: tự gieo dùng túi hạt.
- **Thành phần:** client Unity.
  - Luật thuần trong `Game.Core` (có test EditMode): túi đồ, vật rơi theo ô, rơi xuống, gộp chồng, nhặt trong tầm, tầm công cụ/tầm việc tay.
  - Ở client: hiển thị vật rơi, phím Thu, click nhặt, HUD túi, đặt túi hạt đầu game.
- **Ước lượng:** 3–5 ngày-người (0028) + ≤ 0,1 (0030). Thuộc M1.

## Ngoài phạm vi
- Thuộc 7f, không làm ở 7e:
  - giao diện **mở túi**, chọn túi hạt, chế độ gieo, tự gieo;
  - tự xới (nhân vật tự đi tới), tự thu hoạch;
  - phép Mưa;
  - kho ↔ túi, mở kho ≤ 3 ô.
- Cảm ứng (nút Thu trên điện thoại, chạm để nhặt) → world 7h (b).
- Nông sản sinh vật thu hoạch ("hàng nằm tại ô") **không** thành vật rơi, người chơi không nhặt được; giữ như cũ (0028 mục 12).
- Kho chung của sinh vật (`startSeeds`, hạt trong kho) giữ nguyên.
- Lưu/nạp vật rơi và túi: M1 chưa có lưu game.
- Hiệu ứng âm thanh, hạt bụi khi nhặt.

## Đề xuất của Planner — user xác nhận 2026-10-22
1. **Tầm với (0030):** `PlayerRules` tách thành hai hằng:
   - **tầm công cụ 10**: đào, đặt, xới đánh dấu, click ô ruộng mở bảng;
   - **tầm việc tay 4**: nhặt.
   - Cách đo giữ như brief 0035: Euclid từ tâm thân tới tâm ô. Tầm mở kho 3 để 7f.
2. **Phím Thu = F** (nhấn một lần): mọi chồng vật rơi có tâm ô trong 4 ô vào túi ngay. Hình ảnh: vật bay về nhân vật 0,2 giây rồi biến mất (chỉ hiển thị, túi đã cộng ngay).
3. **Click nhặt:** với mọi công cụ, tia chuột trúng một chồng vật rơi **trong 4 ô** → nhặt cả chồng đó, không đào/đặt/xới. Chồng ngoài 4 ô không chặn tia → đào/đặt khối phía sau như thường.
4. **Vật rơi rơi xuống:** ô dưới một chồng thành rỗng hoặc nước → chồng rơi thẳng xuống tới ô ngay trên khối đặc, gộp với chồng cùng loại ở đó. Làm ngay trong lần sửa đó, không có thời gian rơi. Nước không đẩy, không cuốn vật rơi; vật rơi chìm xuống đáy và vẫn nhặt được.
5. **Đặt khối vào ô có vật rơi** → vật rơi bị đẩy lên ô không đặc đầu tiên phía trên, gộp nếu cùng loại. Không còn ô nào tới đỉnh world → không cho đặt (`Occupied`).
6. **Túi hạt đầu game:**
   - Là một vật rơi loại "túi hạt Cây A × 4", đặt cách chỗ xuất phát 3 ô theo hướng nhân vật nhìn lúc đầu, trên mặt đất (trong 4 ô → nhấn F là nhặt được ngay).
   - Ô đó là nước hoặc không có mặt đất → chọn ô gần nhất đứng được trong vùng 5 × 5 quanh chỗ xuất phát.
   - Áp dụng ở mọi scene có nhân vật.
   - Nhặt thêm túi hạt cùng loại → cộng hạt vào túi hạt đã có.
7. **Hiển thị vật rơi:**
   - Khối mini cạnh 0,3 ô, màu như khối, xoay chậm; túi hạt là hộp nhỏ màu riêng.
   - Một chồng vẽ một hình; số lượng hiện trong dòng gợi ý khi ngắm, vd `Đất × 3`.
   - Số chồng không giới hạn → vẽ gộp (instancing), không tạo một GameObject cho mỗi chồng.
8. **HUD túi:** thanh công cụ vẫn hiện số khối như cũ (giờ là số trong túi), thêm một dòng `Túi hạt Cây A: N` khi có.
9. **Code:** `MaterialInventory` đổi thành túi đồ (giữ khối theo vật liệu + hạt theo loại cây). Test cũ dùng kho vật liệu → sang túi + vật rơi (0028 "Chỗ đổi trong test cũ").
10. **Tách 2 brief:**
    - 0036 — lõi + tầm 10 ô: test EditMode.
    - 0037 — client: hiển thị, Thu, click, túi hạt đầu game, HUD, PlayMode; mức review `user xem`.

## Tiêu chí chấp nhận (kiểm chứng được)
- [ ] Tầm công cụ: biên 10 trong / 10,1 ngoài cho đào, đặt, xới. Tầm việc tay: biên 4 / 4,1 cho nhặt. Kiểm bằng: test EditMode `PlayerRules`.
- [ ] Đào → ô rỗng, túi không đổi, có chồng vật rơi đúng vật liệu tại ô (ô ruộng → đất). Đào 2 lần cùng loại vào cùng một ô → một chồng × 2. Kiểm bằng: test EditMode.
- [ ] Rơi xuống (đề xuất 4) và đẩy lên khi đặt (đề xuất 5), gồm biên đỉnh world. Kiểm bằng: test EditMode.
- [ ] Nhặt: Thu lấy mọi chồng trong 4 ô, bỏ chồng ngoài 4 ô; nhặt một chồng; túi cộng đúng; túi hạt cùng loại cộng hạt. Kiểm bằng: test EditMode.
- [ ] Đặt khối trừ túi; túi hết → `NoMaterial`. Kiểm bằng: test EditMode (test đào/đặt cũ đã đổi).
- [ ] Tất định: cùng chuỗi đào/đặt/nhặt → cùng danh sách chồng (thứ tự, vị trí, số lượng). Kiểm bằng: test EditMode.
- [ ] PlayMode:
  - nhấn F hút chồng trong tầm, chồng ngoài tầm không;
  - click chồng trong 4 ô nhặt được, chồng ngoài 4 ô không chặn đào;
  - túi hạt Cây A × 4 có gần chỗ xuất phát, nhặt xong HUD hiện 4;
  - đào ở 10 ô được, ở > 10 ô không.
  - Kiểm bằng: `run_tests` PlayMode (MCP).
- [ ] Toàn bộ EditMode + PlayMode cũ vẫn qua. Kiểm bằng: `run_tests` (MCP); Editor tắt thì `python3 tools/unity_test.py EditMode|PlayMode`.
- [ ] User xem trên Mac: vật rơi hiện đúng màu/chỗ, F hút, click nhặt, đặt khối trừ túi, túi hạt đầu game. Kiểm bằng: review `user xem` của brief 0037.
- [ ] Tài liệu: simulation-tech bước 7e ghi trạng thái; roadmap ghi giờ thực tế; mechanics/glossary nếu đề xuất 2–7 thêm luật mới. Kiểm bằng: `python3 tools/check_design.py`.

## Ràng buộc
- `Game.Core` không tham chiếu UnityEngine/UnityEditor; luật vật rơi/túi/nhặt nằm ở Core, client chỉ gọi và vẽ.
- Sinh vật làm như cũ (0025–0027), không đổi hành vi sim.
- Unity Editor dùng chung qua MCP ([0010](../../design/decisions/0010-mcp-for-unity.md)). Sau PlayMode: `git checkout -- client/ProjectSettings/EditorSettings.asset`.
- API Unity trong brief 0037 (vd vẽ instancing): Planner tra docs 6000.4 và ghi link.

## Quyết định đã chốt (ai chốt, khi nào)
- 0028 (user, 2026-10-22): vật rơi, túi khác kho, không mức tối đa, Thu + click, túi hạt Cây A 4 hạt, tầm nhặt 4 ô, thứ tự làm.
- 0030 (user, 2026-10-22): đào/đặt/xới 10 ô; làm trong brief đầu của 7e.
- Thứ tự 7e trước 7h (b) (user, 2026-10-22).
- Đề xuất 1–10 của Planner (user "ok chốt", 2026-10-22).

## Câu hỏi còn mở
- (không còn)
