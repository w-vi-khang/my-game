# Chơi thử gộp — bước 7 (7l d, e, f + sim 7g) + sim 8b

> Gom từ mục "User tự test" của brief 0061–0067 (user 2026-10-24: "user xem" gộp một lần ở cuối). Tất cả đã duyệt phần code; chưa user xem.
> Scene **SimView** trên Mac, Play. Ghi đạt / lỗi theo số mục; góp ý → Planner viết brief sửa.
> Số tạm chỉnh trong `design/data/sim_m1.json` (+ bản sao `client/Assets/Game/Client/Resources/Config/sim_m1.json`).

## Còn treo, cần bạn
- ~~Đo FPS điện thoại (7l e)~~ xong 2026-10-24 (brief 0071–0072: khung có ≥ 10 icon ~59 FPS).

## A. Túi / túi phụ / kho (7l d — 0061, 0062, 0063)
1. Đào tới khi túi đầy (10 ô túi + 2 ô túi phụ; camera quan sát chiếm 1 ô) → F không nhặt nữa, dòng `Túi đầy`, khối còn dưới đất.
2. Thu hoạch khi túi đầy → nông sản / hạt rơi trên ô ruộng.
3. Thứ tự thêm (túi phụ cộng dồn trước) / lấy (túi trước) có hợp lý không.
4. Túi phụ 2 ô giữa-dưới, nhãn `1` `2`; P ẩn/hiện; B mở túi (lưới 5 × 2 trên túi phụ), túi phụ luôn hiện khi túi mở.
5. Ô có hình màu + số góc dưới-phải; túi hạt / nông sản có chữ loại cây góc trên-phải; camera quan sát ở ô đầu.
6. Click trái ô → bảng lề trái: tên, cách có, số trong túi (gồm túi phụ), số trong kho.
7. Chuột phải ô khối / túi hạt / camera → cầm, túi đóng; ô nông sản → `Không cầm được …`.
8. Phím 1 / 2 cầm vật ở túi phụ (khi túi đóng).
9. Chuột phải khối kho → đi tới → kho mở bên phải cùng túi; đi xa > 10 ô → kho tự đóng.
10. Click lên túi / túi phụ / kho không làm nhân vật đi; lăn chuột trên túi không thu phóng camera.
11. Trang: `player.bagPageSize` = 5 → túi 2 trang, nút `<` `>`, qua trang cuối về trang 1 (trả lại 10 sau khi xem).
12. Kéo ô sang túi phụ → cả chồng; lên ô khác loại → đổi chỗ; cùng loại → gộp. Kéo ra ngoài → không đổi; lúc kéo nhân vật không đi theo chuột.
13. Kéo túi hạt / nông sản túi ↔ kho; kéo khối vào kho → `Kho không nhận …`.
14. Click chọn ô → khung đích sáng xanh + `Chuyển tới: …`; Tab đổi đích; 1 / 2 / 3 = chuyển hết / nửa / một phần ba.
15. Đánh giá: bố cục, cỡ ô, màu, ngưỡng kéo 8 px, màu khung đích.

## B. HUD gọn + menu phép (7l e1 — 0064)
16. Góc trên-trái chỉ còn avatar "B" + thanh máu + mana + số; không còn chữ hướng dẫn dài, dòng kho / ô ruộng.
17. Q → "Chế độ: Đào"; D → dòng chế độ biến mất. Thông báo tự ẩn sau ~4 giây.
18. Trên cùng giữa: "xx FPS · mục tiêu 60 · nhịp N", không che minimap.
19. O → Cài đặt → tab Hướng dẫn: chữ phím đúng phím đang dùng (B, P, 1–0, kéo thả, Tab, 1/2/3, G…), cuộn được.
20. Không còn dòng "Đang ở buổi"; nút "Phép (G)" dưới minimap; G mở menu 2 mục: Đổi buổi → vòng 4 mốc; Mưa → chế độ Tưới, chuột phải ô ruộng tưới được, mở lại menu thấy đếm lùi 10 giây. T không còn làm gì.
21. Đánh giá: vị trí / cỡ avatar, thanh, menu phép; 4 giây ẩn thông báo.

## C. Icon ô ruộng + sinh vật (7l e2 — 0065)
22. Mỗi ô ruộng có icon tròn + vòng xanh: xới = cuốc, gieo = hạt, tưới = giọt nước, đang lớn = mầm (vòng đầy dần), chín = đĩa màu cây, trống = "…"; vòng mượt.
23. Sinh vật: không còn chữ "L1 · …"; icon nhỏ: rảnh "…", đang làm = icon công đoạn + vòng, đang đi tới = icon mờ, mang hàng = thùng vàng.
24. Đình trệ: "!" đỏ tại chỗ, không còn chữ đỏ.
25. Icon đúng chỗ khi xoay / thu phóng camera; không đè túi / menu / bản đồ; mở Cài đặt thì ẩn; đi xa trại thì ẩn.
26. Đánh giá: cỡ icon (40 / 28 px), khoảng cách ẩn 30 ô, hình giữ chỗ đọc được giai đoạn không.

## D. Bảng đổi phím (7l f — 0066)
27. O → tab **Điều khiển**: 4 nhóm, 34 dòng, tên tiếng Việt + phím hiện tại; cuộn được; chú thích Esc / F1–F3.
28. "Đổi" ở "Đi tới chỗ click" → bấm chuột phải → hỏi trùng "Thao tác chọn…" → **Đổi chỗ** → chuột phải đi, chuột trái thao tác. Đóng mở lại game → vẫn đổi. **Mặc định** → về như cũ.
29. Đổi "Nhảy" sang Q → hỏi trùng "Chế độ Đào" → **Huỷ** → Nhảy vẫn A. Đang chờ phím bấm Esc → huỷ, bảng Cài đặt không đóng.
30. Đổi "Túi phụ ô 1" sang phím khác rồi thử; "Chuyển toàn bộ" (1) vẫn dùng được khi túi mở.
31. Gán Esc / F1 → không nhận.

## E. Sinh vật chỉ làm công đoạn được giao (sim 7g — 0067)
32. Đánh dấu ô (biển), hàng đợi trống → sinh vật xới xong rồi ô đứng yên.
33. Tự gieo hạt lên ô đó → không ai tới tưới. Bảng ô ruộng → nút **Tiếp tục** → một sinh vật tới tưới rồi thôi.
34. Cây chín → không ai thu. Tiếp tục → sinh vật thu, chở về kho. Ô đứng yên → Tiếp tục (hoặc "Tự xới") → xới lại.
35. Ô có hạt trong hàng đợi: sinh vật làm đủ chuỗi như cũ. Tự thu / Mưa giữa chừng → sinh vật làm tiếp các bước sau.
36. Đánh giá: vị trí / chữ nút Tiếp tục; có cần chữ "chờ Tiếp tục" trên ô tiến trình không.

## F. Mép cỏ ở góc lõm (sửa lỗi — 0073)
37. Đi quanh bậc thang cỏ và các góc lõm chữ L (PC và điện thoại): không còn tam giác tối nhỏ ở chỗ hai khối cỏ gặp nhau; mặt cỏ góc lõm không nhấp nháy khi nhìn sát.

## G. Môi trường sống (sim 8b — 0074)
38. Lúc đầu chỉ có 1 sinh vật cam (L1) và 3 nhà. Có ao 4 × 4 viền cát phía trước-phải trại.
39. H → bảng "Môi trường sống" bên phải dưới minimap:
    - L1 "đã ở trại"; L2 và L3 "chưa trở lại";
    - các dòng `Ô cỏ cạnh nước: 0/10`, `Ô đá lộ thiên: 0/15`, `Kho: 1/1 (đạt)`.
    - H hoặc Esc đóng bảng; Esc không mở Cài đặt.
40. Bảng đang mở, đào một ô cỏ → số `Ô cỏ` của L1 giảm ngay.
41. L1 xới, gieo, rồi ô đứng chờ tưới ("!" thiếu năng lực). Tự tưới (Mưa) / tự thu được như trước.
42. Đào cát quanh ao, đặt đất, dùng Mưa cho đất thành cỏ (hoặc đặt khối cỏ) tới 10 ô cạnh nước → thông báo "L2 đã trở lại!". 2 sinh vật xanh xuất hiện ở rìa đồng bằng gần bạn, tự về trại, rồi tưới các ô đang chờ.
43. Đào lộ đá (hoặc đặt khối đá lên mặt đất) đủ 15 ô → "L3 đã trở lại!", 2 sinh vật tím.
44. Cài đặt → Điều khiển: có dòng "Bảng môi trường sống — H", đổi được phím. Tab Hướng dẫn có dòng H.
45. Đánh giá:
    - vị trí và cỡ bảng; vị trí ao;
    - ngưỡng: 10 cỏ cạnh nước, 15 đá lộ thiên;
    - 4 giây thông báo;
    - có cần nút bấm mở bảng không;
    - đào cát cạnh ao để lại hố trống, nước không chảy vào — có chấp nhận ở M1 không.

