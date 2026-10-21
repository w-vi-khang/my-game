# 0024 — Buổi trong ngày: chu kỳ 24 giờ thực + chọn thời điểm (4 mốc) ở M1

> Trạng thái: Chốt
> Ngày: 2026-10-21
> Thay đổi: mở rộng phạm vi M1 của [0007](0007-m1-toi-gian.md) thêm hạng mục "buổi trong ngày" (user chọn "Thêm vào M1", 2026-10-21).

## Bối cảnh
User (2026-10-21, kèm ảnh game tham khảo: vòng chọn 4 mốc quanh biểu tượng mặt trăng, dòng "đang ở buổi Ban đêm"): world có chu kỳ ngày đêm; một chu kỳ = 24 giờ thực. Người chơi chọn được thời điểm trong world (giữa trưa, giữa đêm, bình minh, hoàng hôn) mà **không** làm đổi thời gian của world. Chọn xong, cả bầu trời xoay tới mốc theo đúng chiều (trưa → bình minh phải xoay cung dài thuận chiều mặt trời lặn rồi mọc, không quay ngược). Sinh vật/điều kiện tương tác với buổi.

Hiện trạng [repo]: chưa có ngày đêm, ánh sáng, bầu trời trong design/ hay client; "môi trường sống" của M1 chưa có code (chỉ có sim lệnh trồng cây, `design/data/sim_m1.json`).

## Các lựa chọn
Giờ world khi chọn mốc (user chọn 2026-10-21, kèm điều kiện):
- (a) Chỉ đổi góc nhìn bầu trời tạm thời.
- (b) Chọn mốc đổi luôn **buổi** (đẩy độ lệch), buổi tiếp tục trôi từ mốc đó. ← chọn.
- (c) Đóng băng bầu trời ở mốc.

Mức tương tác cho lần đầu (user chọn): **điều kiện môi trường sống theo buổi**. Không làm: loài ngày/đêm, cây trồng theo buổi, tốc độ làm việc theo buổi.

## Quyết định
1. **Hai đồng hồ tách rời** (user: "không ảnh hưởng cây trồng, công trình… những thứ đếm thời gian vẫn vậy"):
   - **Đồng hồ mô phỏng** (nhịp sim, thời gian lớn cây, xây dựng, nước lan…): không đổi, không đọc buổi.
   - **Buổi trong ngày**: chỉ để hiển thị bầu trời và để xét điều kiện môi trường sống.
2. **Chu kỳ:** 1 ngày world = 24 giờ thực (86.400.000 ms). Pha trong ngày `phase` ∈ [0, 1) = ((giờ đồng hồ máy theo ngày + `offset`) mod 24 giờ) / 24 giờ. M1 lấy giờ đồng hồ máy làm nguồn (không có server); nguồn giờ tiêm được để test. Đổi sang giờ server khi có server (cùng chỗ với tua nhanh) là việc M2+, chưa chốt.
3. **4 mốc** (pha): giữa đêm 0,00 · bình minh 0,25 · giữa trưa 0,50 · hoàng hôn 0,75.
4. **Chọn mốc:** đặt `offset` sao cho pha hiện tại bằng pha mốc ngay lúc chọn; sau đó buổi tiếp tục trôi theo giờ thực. `offset` chỉ trong phiên (M1 không lưu world), mở lại → theo giờ máy.
5. **Hướng xoay:** luôn theo chiều tiến của pha (mặt trời mọc → lên → lặn → đêm → mọc), không bao giờ đi ngược. Cung xoay = `(pha mốc − pha hiện tại) mod 1` của một ngày (trưa → bình minh = 0,75 ngày = 270°). Chọn đúng mốc đang ở (cung 0) → không làm gì. Thời gian xoay: cố định 3 giây, ease in-out [số tạm]; trong lúc xoay, chọn mốc khác bị bỏ qua (số tạm).
6. **Buổi (cho điều kiện):** 4 buổi theo pha, biên trong JSON [số tạm]: Bình minh [0,20; 0,30) · Ban ngày [0,30; 0,70) · Hoàng hôn [0,70; 0,80) · Ban đêm [0,80; 0,20) (qua nửa đêm). Điều kiện môi trường sống có thể ghi "chỉ tính khi đang ở buổi X" (một hoặc nhiều buổi). Vì chọn mốc đổi buổi thật (mục 4), người chơi chủ động chọn buổi để đạt điều kiện — đây là ý định, không phải kẽ hở: không ảnh hưởng đồng hồ mô phỏng.
7. **M1 làm:** lõi `Game.Core` (đồng hồ, offset, cung xoay, buổi; số nguyên; test EditMode), hiển thị bầu trời + ánh sáng + sương theo pha, vòng chọn 4 mốc + dòng "đang ở buổi …" trong scene, loại điều kiện "buổi" trong dữ liệu môi trường sống (kiểm trong lõi; việc nối vào bộ môi trường sống làm khi hạng mục đó có code). Cấu hình `design/data/time_m1.json`.
8. Ngoài phạm vi (M2+): loài ngày/đêm, hành vi sinh vật theo buổi, cây trồng theo buổi, lưu offset, giờ server, thời tiết.

## Hệ quả
- **Ước lượng** ước lượng của AI, chưa xác minh: lõi + test 1–1,5 · bầu trời/ánh sáng/sương theo pha 1,5–2,5 · vòng chọn + nhãn buổi 1–2 · loại điều kiện buổi 0,5 → **4–6 ngày-người** vào M1 ([roadmap](../05-roadmap.md)). Pillar: P1 (điều kiện môi trường sống). Thành phần: client Unity (lõi trong `Game.Core`).
- Cập nhật cùng lần: [glossary](../glossary.md), [mechanics](../02-gameplay/mechanics.md#buổi-trong-ngày), [world-tech](../04-tech/world-tech.md) bước 7f, [roadmap](../05-roadmap.md), [decisions/README](README.md).
- Rủi ro: ánh sáng URP + sương/đại dương/mặt nước đang có (`OceanBackdrop`, `WorldEdgeFog`, `UnderwaterView`) phải đổi màu theo pha; kiểm trên điện thoại (FPS).
