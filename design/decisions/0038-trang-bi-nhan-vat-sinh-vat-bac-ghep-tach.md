# 0038 — Trang bị nhân vật và trang bị sinh vật; bậc C–S1, ghép bằng phôi, tách có phí

> Trạng thái: Chốt
> Ngày: 2026-10-24
> Thay đổi: [mechanics — Trang bị](../02-gameplay/mechanics.md#trang-bị) "Trang bị dành cho sinh vật" và "ghép trang bị cùng loại, cùng bậc" (user 2026-10-12, file Đang xem xét) — nay có thêm trang bị nhân vật, và từ A1 trở lên ghép bằng phôi.

## Bối cảnh
Ngày 2026-10-24 user mô tả hệ thống trang bị (brainstorm `local-game-ideation`): trang bị nhân vật 6 ô; trang bị sinh vật 3 ô, món lớn ghép từ 2 mảnh nhỏ; công thức bậc chung cho cả hai. AI tính lại công thức và thấy hai chỗ vướng: (1) S0 đi lên từ A1 nên A2 là ngõ cụt; (2) tách không mất gì nên từ A1 trở lên phần đã bỏ vào chuyển được sang loại khác. AI cũng báo "sách phép" trùng tên với "gói sách phép". User trả lời từng điểm.

## Các lựa chọn
- Vướng 1: S0 = A2 + phôi A1 (AI đề xuất, giữ giá 54C) **hay** S0 = A2 + phôi A2 (user chọn, giá 72C).
- Vướng 2: tách miễn phí **hay** thu phí (user chọn thu phí).
- Trang bị nhân vật tính khi chiến đấu PvE (AI ý I3) **hay** không (user chọn không).

## Quyết định
(user, 2026-10-24)
1. **Hai họ trang bị tách biệt:** trang bị nhân vật và trang bị sinh vật. Phôi, nguyên liệu ghép, ghép và tách đều riêng theo từng họ.
2. **Bậc trang bị:** C, B, A0, A1, A2, S0, S1. **S1 là bậc cao nhất.**
3. **Ghép:**
   - B = 3 C, A0 = 3 B. Cả 3 món phải cùng loại, cùng bậc.
   - A1 = A0 + phôi A0. A2 = A1 + phôi A1. **S0 = A2 + phôi A2.** S1 = S0 + phôi S0.
   - **Phôi An** là một phôi bậc An, hoặc một món trang bị bậc An bất kỳ loại nào, miễn cùng họ.
   - Phôi không thuộc loại trang bị nào. Phôi chỉ làm nguyên liệu ghép, không dùng làm món gốc để nâng cấp. **Phôi không ghép với nhau được.**
4. **Tách:** chỉ tách được món từ A1 trở lên. Tách là làm ngược bước ghép: A1 → A0 cùng loại + phôi A0; A2 → A1 + phôi A1; S0 → A2 + phôi A2; S1 → S0 + phôi S0. Mỗi lần tách **thu một ít phí**. Phí trả bằng tài nguyên không phải trang bị (tiền trong game).
5. **Chỉ số cố định** cho cả hai họ: không có chỉ số ngẫu nhiên, không có vượt trội.
6. **Trang bị nhân vật:**
   - 6 ô: mũ, áo, giày, găng tay, **quyền trượng** (đổi từ "sách phép" để khỏi trùng với gói sách phép), nhẫn.
   - Chỉ giúp nhân vật **du hành** tốt hơn: tốc độ di chuyển, mana tối đa, hồi mana, một ít máu tối đa, hồi máu...
   - **Không có tác dụng trong chiến đấu.** Chiến đấu vẫn dùng sinh vật. Trong PvP sinh vật, trang bị nhân vật bị bỏ.
   - Có cộng chỉ số trong **một mode chơi khác**. Mode này user chưa mô tả.
   - Nguồn: rèn, thu thập, rút gói trong cửa hàng.
7. **Trang bị sinh vật:**
   - 3 ô đồng cấp, không chia loại ô (giữ như 2026-10-12).
   - Món lớn ghép từ 2 mảnh nhỏ. Có 8 loại mảnh và 36 món lớn: 28 cặp hai mảnh khác nhau + 8 cặp hai mảnh giống nhau, không tính thứ tự.
   - Vẫn tính trong PvP.

## Hệ quả
- Giá quy về số món C cùng loại (coi phôi An bằng một món An): C 1 · B 3 · A0 9 · A1 18 · A2 36 · S0 72 · S1 144. Với trang bị sinh vật, nhân 2 để ra số mảnh.
- Từ A1 trở lên, phần đã bỏ vào món gần như chuyển được sang loại khác: tách ra phôi rồi ghép với món gốc của loại khác. Chỉ món gốc A0 (cần 3 B cùng loại) và phí tách là mất. Người chơi đổi build PvP mà ít bị thiệt (P4). Phí tách là chỗ tiêu tiền trong game.
- [mechanics](../02-gameplay/mechanics.md#trang-bị): viết lại mục Trang bị, tách bảng cơ chế thành hai dòng. Đã cập nhật [glossary](../glossary.md), [economy](../02-gameplay/economy.md), [progression](../02-gameplay/progression.md), [summary](../summary.md). Ý không dùng đã chuyển vào [ideas](../ideas.md).
- Ước lượng của AI (chưa chốt, đánh dấu ý tưởng ở mechanics và roadmap): trang bị sinh vật 23–42 + trang bị nhân vật 4–7 = 27–49 ngày-người (trước 13–23) → M3 +14–26, ghi ở [roadmap](../05-roadmap.md). Chưa tính art (8 mảnh + 36 món + trang bị nhân vật).
- [0004](0004-tien-nap.md) không đổi: gói trang bị chỉ giúp đi nhanh hơn; bậc S1 người không nạp đạt được bằng ghép, rèn, thu thập. Trang bị nhân vật không tính trong PvP sinh vật nên không ảnh hưởng công bằng PvP.
- Còn mở (ghi ở mechanics): mức phí tách [số tạm]; mảnh nhỏ có bậc không; mảnh nhỏ gắn trực tiếp được không; mỗi ô nhân vật có một hay nhiều loại trang bị; ô nào cho chỉ số nào; mode chơi khác là mode gì; nguồn phôi ngoài việc tách.
