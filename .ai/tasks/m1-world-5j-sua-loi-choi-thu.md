# Task: M1 world 5j — sửa lỗi sau chơi thử nước 0079–0081

> Model thực thi: theo từng brief (AGENTS.md §10.2).
> Trạng thái: Đang làm — brief [0082](../briefs/0082-world-5j-nuoc-trong-khong-chong-lop.md) (lỗi 1) đã duyệt 2026-10-29; [0083](../briefs/0083-world-5j-mep-tran-thon-camera-an-than.md) (lỗi 6, 8; lỗi 7 chờ user) đã duyệt 2026-10-29; [0084](../briefs/0084-world-5j-do-day-nuoc-ho-rong.md) (lỗi 3 sửa; lỗi 2: code đúng 0043, luật không lấp hố rộng — chờ user chọn luật) chờ làm; [0085](../briefs/0085-world-5j-ho-thap-hon-nguon-thi-day.md) (lỗi 2) đã duyệt; [0086](../briefs/0086-world-5j-dung-duoi-chan-thac.md) (lỗi 4: tia click xuyên nước + tự nhảy bậc khi đích gần) và [0087](../briefs/0087-world-5j-vat-roi-nam-yen-lech-ngau-nhien.md) (lỗi 5) chờ làm. User chơi thử một lượt khi xong cả 5j (user 2026-10-29).
> - Nguồn: user chơi thử gộp 0079–0081 (2026-10-29, mục Review của [brief 0081](../briefs/0081-world-5i-bo-lom-chan-thac.md)); [world-tech](../../design/04-tech/world-tech.md) dòng 5j.
> - Ước lượng 2–4 ngày-người [ý tưởng], roadmap M1.

## Mục tiêu
Sửa 5 lỗi user thấy khi chơi thử, mỗi lỗi một brief (hoặc gộp khi cùng file):
1. Cột thác "đứt khúc", thấy từng mảng (shader nước trong suốt vẽ chồng lớp không theo thứ tự) — brief 0082.
2. Nước không lấp đầy hố đào. → user chọn luật [0044](../../design/decisions/0044-ho-thap-hon-mat-nguon-thi-day.md) (2026-10-29): ô thấp hơn mặt nguồn thì đầy; brief sau 0084.
3. Mặt nước mức ≥ 2 trông như nhau (chỉ thấy mỏng dần từ mức 1 về 0).
4. Đứng dưới chân thác bị đẩy nổi 1 ô.
5. Vật rơi nằm trên mặt đất, không lơ lửng/xoay; rơi ngẫu nhiên gần khối bị phá.
6. (user 2026-10-29, sau 0082) Gai nước nhô ra ở chỗ mép tràn nối cột thác (`art-test/shots/user0082_lip_spike.webp`).
7. Gờ mỏng ở đầu cột, ngay dưới mép tràn (`user0082_column_side.png`).
8. Camera dưới nước nhìn lên: một mảng xanh đậm phẳng, cạnh thẳng (`user0082_underwater.png`).
9. (user 2026-10-30, chơi thử gộp 5j) Gai nước vẫn còn chỗ mép tràn nối cột thác — lỗi 6 chưa hết (`art-test/shots/user5j_lip_spike_again.webp`). Nguyên nhân [run 2026-10-31]: ô cột cạnh khối nhô có góc vuông nằm trên ô cột góc bo → chỗ chuyển không có mặt đáy. Brief [0090](../briefs/0090-world-5j-va-goc-cot-thac.md).
10. Mép tấm cỏ (lớp phủ + viền rủ) chỗ kết thúc ở bậc thang bị cắt cụt, thiếu tự nhiên (`user5j_grass_cap_end.png`). User chọn (2026-10-31): đầu dải chạm khối đặc thon dần về 0, bỏ nắp. Brief [0091](../briefs/0091-world-5j-mep-co-thon-dau.md).
11. Click di chuyển khi ở dưới nước/nhìn qua mặt ao: đích là mặt nước thay vì mặt khối đặc trong nước (`user5j_block_in_fall.webp`). Planner dump world đang chạy [run 2026-10-30]: dữ liệu nước quanh khối đặt vào cột thác (136,23,200) đúng — nước tràn trên khối, rơi 3 phía. Nguyên nhân khả dĩ: `PlayerCharacter.PointOnWorld` dùng `WaterRay.FirstWater` cho mọi ô nước (0086), gốc tia trong nước → t = 0 [repo `client/Assets/Game/Core/WaterRay.cs`]. Brief [0089](../briefs/0089-world-5j-click-qua-nuoc-toi-khoi.md): tia đi liền trong nước tới khối đặc → đích là khối.
12. Rãnh đào cạnh ao không đầy (`user5j_trench_not_full.png`). Dump [run 2026-10-30]: ao nguồn y = 20; rãnh 1 ô (x = 134) mức 5 nhờ tăng mức bởi tường; vùng rộng x 136–141, z 191–195 cùng tầng y = 20 lan giảm 5→0, ô (141,20,191) rỗng; lớp y = 19 đầy mức 5 (0044 mục 1). Code đúng 0044 mục 2; user muốn lớp cùng tầng nguồn cũng đầy → quyết định [0045](../../design/decisions/0045-ho-ngang-mat-nguon-thi-day-muc-tinh-lai.md) (user 2026-10-30): vùng kín ngang mặt nguồn thì đầy + mức không phụ thuộc thứ tự đào. Brief [0088](../briefs/0088-world-5j-muc-nuoc-tinh-lai-vung-kin.md).
    - **Lỗi code kèm theo** (user hỏi "sao 4 kề 1, 3 kề 0", 2026-10-30): mức nước phụ thuộc thứ tự đào. Thử [run 2026-10-30, execute_code]: hố 7×6 cạnh nguồn, đào cùng lúc → dốc đều 5…0; đào từng ô theo 40 thứ tự ngẫu nhiên → 31/40 có hai ô kề lệch ≥ 2 (tệ nhất 0 kề 5). Nguyên nhân [repo `client/Assets/Game/Core/WorldEditor.cs`]: (a) `WallBoost` chỉ xét lúc tạo ô/đặt khối, mức không bao giờ giảm (`SetLevel` chỉ gọi để nâng, dòng 556, 578, 583) → ô từng có 2 vách giữ mức 5 sau khi vách bị đào; (b) `StepSpread` tạo ô con mức cha − 1 từ ô hoạt động id nhỏ nhất, không xét ô kề mức cao hơn. Hướng sửa: mức ô chảy tính lại theo hình dạng hiện tại (max ô kề − 1, hoặc MaxLevel khi điều kiện tăng mức/lấp hố đang đúng), kể cả hạ mức; test: cùng hố, mọi thứ tự đào → cùng kết quả.
- **Pillar:** P1 (cải tạo thế giới). **Thành phần:** client Unity.

## Ngoài phạm vi
- Hiển thị dòng chảy (5k), hiệu ứng bọt chân thác.
- Phá/đặt khối dưới chân player: giữ như hiện tại (user 2026-10-29).

## Tiêu chí chấp nhận
- [x] Mỗi lỗi có test tái hiện (fail trước, đạt sau) hoặc ghi rõ vì sao chỉ kiểm được bằng mắt — brief 0082–0087 đã duyệt 2026-10-29 (lỗi 7 chỉ kiểm bằng mắt).
- [x] Lỗi 9–12 có brief đã duyệt (0088–0091, 2026-10-31).
- [ ] User chơi thử lại: không còn lỗi 1–12.

## Kiểm chứng
- 2026-10-30 (Planner): thêm lỗi 9–12 — đọc lại, số liệu khớp dump/thử [run]; checklist: pillar P1, client Unity, thuật ngữ glossary, chỉ M1 (5j) đạt; quyết định 0045 ghi sau khi user đồng ý; `check_design.py` hợp lệ [run]; code/test/giờ: chưa áp dụng.
