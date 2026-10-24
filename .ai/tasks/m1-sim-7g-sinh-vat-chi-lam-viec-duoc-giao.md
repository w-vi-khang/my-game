# Task: M1 sim 7g — sinh vật chỉ làm công đoạn được giao (0036 mục 1–4)

> Model thực thi: Sonnet · medium (AGENTS.md §10.2).
> Trạng thái: quyết định gốc [0036](../../design/decisions/0036-sinh-vat-chi-lam-cong-doan-duoc-giao.md) mục 1–4 (Chốt, user 2026-10-24); dòng 7g ở [simulation-tech](../../design/04-tech/simulation-tech.md#kế-hoạch-prototype-m1). Ước lượng 1–2 ngày-người [ước lượng Planner]. Dự kiến 1 brief (0067). Task contract do Planner lập 2026-10-24 từ quyết định đã Chốt (user AFK — không có câu hỏi mở).

## Mục tiêu
Sinh vật chỉ làm công đoạn được giao (0036 mục 1–4):
- **Biển canh tác** = giao xới; xới xong mà hàng đợi trống → ô đứng yên.
- **Hàng đợi có hạt** = giao trồng: đủ chuỗi gieo → tưới → thu hoạch → xới lại → vụ kế (như 0025–0027).
- **Vụ tự làm** (người chơi tự gieo ô hàng đợi trống): sinh vật không tự tưới / thu hoạch / xới lại.
- **Nút Tiếp tục** trên ô tiến trình = giao đúng 1 công đoạn kế.
- Người chơi chen vào ô đã giao trồng → công đoạn xong ngay, sinh vật làm tiếp. Người chơi tự xới ô có biển: hàng đợi có hạt → sinh vật gieo tiếp; trống → đứng yên.

- **Pillar:** P2 "Sinh vật là cốt lõi" — giao việc là thao tác chủ động (0036 Hệ quả).
- **Thành phần:** client Unity — luật ở `Game.Core` (`Simulation`, `FarmPlot`), nút ở `SimView` (bảng ô ruộng).

## Ngoài phạm vi
- 0036 mục 5–6 (vòng tầm, F Thu + gió): đã làm ở world 7l c2.
- Đổi hình icon ô ruộng / chữ ô tiến trình cho vụ tự làm (giữ icon / chữ hiện có).
- Giao việc từng ô cho từng sinh vật cụ thể, nhiệm vụ cho người chơi (M2, 0014).
- Số mới: không có (0036 mục 7 chỉ có số của mục 5–6).

## Tiêu chí chấp nhận
Đúng cột "Xong khi" dòng 7g:
- [ ] Test EditMode: tự gieo → không sinh vật nào tưới sau N nhịp; Tiếp tục → tưới xong rồi dừng, cây chín không thu; Tiếp tục → thu hoạch; ô giao trồng người chơi tự thu → sinh vật xới lại + vụ kế; tự xới ô biển có hàng đợi → sinh vật gieo; tất định.
- [ ] Test PlayMode: nút Tiếp tục.
- [ ] User xem (gộp cuối — xem Quyết định).
- [ ] `Game.Core` không tham chiếu UnityEngine; test cũ (`PlayerFarmTests`, `FarmTests`, `SimFarmUiPlayTests`…) còn qua; EditMode + PlayMode qua hết, 0 lỗi console.

## Ràng buộc
- Mô phỏng số nguyên, tất định; lệnh người chơi vào giữa hai nhịp (0028).
- Không đổi mã băm của kịch bản không có vụ tự làm / Tiếp tục (trường mới chỉ trộn vào băm khi khác mặc định).

## Quyết định đã chốt (ai chốt, khi nào)
- 0036 mục 1–4 + 3 câu làm rõ (Tiếp tục = 1 công đoạn kế; chen vào ô đã giao → sinh vật làm tiếp; tự xới ô biển có hạt → sinh vật tự gieo): user, 2026-10-24.
- **"User xem" gộp một lần cuối** cùng 7l d–f (user 2026-10-24, "Hãy hoàn thành mục 7 một lượt", chọn "Gộp cuối cùng"); review kết luận "code ok, chờ user xem gộp cuối".
- Đề xuất Planner (2026-10-24, user xem ở lần chơi thử cuối, đổi được):
  - vụ tự làm chỉ khi người chơi gieo ô **đứng yên** (pha Idle); gieo vào ô đang chờ gieo vụ từ hàng đợi = chen vào (mục 3);
  - chọn hạt vào hàng đợi trong lúc vụ tự làm: vụ hiện tại vẫn là vụ tự làm; vụ kế theo hàng đợi là vụ giao;
  - Tiếp tục cũng dùng cho ô đứng yên chưa xới lại (sau thu hoạch / huỷ vụ, hàng đợi trống) → giao xới lại; người chơi cũng tự xới được ô đó ("Tự xới");
  - huỷ vụ → hết vụ tự làm.

## Câu hỏi còn mở
_(không)_
