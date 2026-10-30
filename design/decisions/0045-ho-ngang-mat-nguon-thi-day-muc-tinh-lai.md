# 0045 — Nước: vùng kín ngang mặt nguồn thì đầy; mức không phụ thuộc thứ tự đào

> Trạng thái: Chốt
> Ngày: 2026-10-30 (user đồng ý 2026-10-30)
> Thay đổi: [0044](0044-ho-thap-hon-mat-nguon-thi-day.md) mục 2 (lớp ngang mặt nguồn: thêm trường hợp vùng kín thì đầy). Làm rõ cách cài [0043](0043-nuoc-cay-cha-con-tang-muc-boi-tuong.md) mục 3–4 (không đổi luật).

## Bối cảnh
User chơi thử gộp 5j (2026-10-30): rãnh đào cạnh ao, cùng tầng mặt ao, không đầy; thấy ô mức 4 kề ô mức 1, 3 kề 0 (`art-test/shots/user5j_trench_not_full.png`) [user]. Planner dump world đang chạy [run 2026-10-30]: lớp y = 20 (ngang mặt ao) lan giảm 5→0 theo 0044 mục 2; lớp y = 19 đầy. Thử hố 7 × 6 cạnh nguồn, đào từng ô theo 40 thứ tự ngẫu nhiên: 31/40 có hai ô kề lệch ≥ 2 [run 2026-10-30]. Nguyên nhân [repo `client/Assets/Game/Core/WorldEditor.cs`]: phát sóng mức chỉ chạy lúc một ô được tăng mức bởi tường (`WallBoost` → `Propagate`); ô sinh sau không nhận mức từ ô kề cao hơn; mức không bao giờ hạ khi ô mất vách.

## Các lựa chọn
- A1. `y ≤ y nguồn` thì đầy (trong `basinFillRadius`). Nhược: nguồn nằm trên đất trống tràn mức 5 ra cả vùng 16 ô (Planner phát hiện khi viết brief 0088, báo user 2026-10-30).
- **A2. Lớp ngang mặt nguồn chỉ đầy khi là vùng kín** + tính lại mức theo hình dạng hiện tại.
- B. Giữ 0044, chỉ sửa lỗi thứ tự đào.

## Quyết định
Chọn **A2** [user 2026-10-30; lúc đầu đồng ý A1, chọn lại A2 sau khi Planner báo nhược điểm].
1. Lớp thấp hơn nguồn: giữ 0044 mục 1. Lớp **ngang** mặt nguồn (`y` = `y` nguồn gốc): ô nước chảy lên MaxLevel + phát sóng mức khi **vùng kín** — mọi ô rỗng/nước chảy nối liền 4 hướng với nó ở tầng đó (không tính ô nguồn; ô ngoài world tính như vách) đều cách nguồn gốc ≤ `basinFillRadius` (16 ô ngang, Chebyshev) [số tạm, chưa chốt]. Vùng hở (lan ra ngoài bán kính) hoặc cao hơn nguồn: luật 0043 (lan giảm 1 mức/ô, tăng mức bởi tường). Ví dụ: rãnh/hố đào cạnh ao → đầy; nguồn trên đất trống → lan giảm 5→0 như cũ.
2. **Mức là hàm của hình dạng hiện tại, không của thứ tự đào:**
   - Ô mới tạo/tái tạo nhận `lv` = max(ô nước kề ngang) − 1 (nguồn tính MaxLevel); ô kề cao hơn cha hiện tại → đổi cha sang ô kề đó (không tạo vòng — giữ luật tổ tiên của 0043 mục 4), rồi phát sóng mức.
   - Ô có sẵn được phát sóng mỗi khi một ô kề đổi mức hoặc vừa được tạo, không chỉ lúc tăng mức bởi tường.
   - Điều kiện tăng mức bởi tường / lấp hố được xét lại khi ô kề ngang đổi (đào hoặc đặt khối); hết điều kiện → mức hạ về mức tính từ ô kề, cập nhật lan xuống các ô phụ thuộc.
   - Kiểm: cùng một hố, mọi thứ tự đào → cùng mức từng ô sau khi lắng (SettleWater).
3. Không dâng cao hơn mặt nguồn (0043, 0044 mục 4 giữ).

## Hệ quả
- [world-tech](../04-tech/world-tech.md) mục "M1 — cây cha–con" (dòng 0044) và dòng 5j.
- Thành phần: client Unity — `Game.Core` `WorldEditor` (luật nước). Không đổi tham số JSON (`basinFillRadius` giữ 16).
- Test cũ đổi kỳ vọng: test của brief 0085 về lớp ngang mặt nguồn (nếu có) sang "đầy".
- Ước lượng 1–1,5 ngày-người, trong 5j (2–4), không đổi roadmap.
