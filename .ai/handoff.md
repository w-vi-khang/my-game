# Handoff — 2026-10-29 (Planner, task 5j)

## Đang làm
Task [m1-world-5j-sua-loi-choi-thu](tasks/m1-world-5j-sua-loi-choi-thu.md): sửa lỗi sau chơi thử nước 0079–0081. User muốn **làm xong hết 5j rồi chơi thử một lượt** (gộp mọi mục "User tự test" vào một danh sách ở cuối). Chạy nối tiếp: Planner con Opus · medium viết brief → Executor con chạy `/ai-workflow-kit:exec NNNN --child` → phiên cha review.

## Trạng thái
- Đã duyệt: 0081 (bo lõm chân thác), 0082 (depth prepass, lỗi 1), 0083 (mép tràn thon + ẩn thân khi camera gần, lỗi 6, 8; lỗi 7 chờ user).
- **Đang chạy:** brief 0084 (lỗi 3: mặt nước đậm/nhạt theo độ dày; thêm test `WideHolePastLevelRadiusStaysEmpty` ghi hành vi 0043) ở phiên con `child-sonnet-high` = `local_8a548148-62c8-47bd-a761-325e821e9f93` (thực chất Sonnet · medium; brief đã hạ effort xuống medium, commit `89eef6b`). Xong → review đầy đủ (`/ai-workflow-kit:review-brief 0084`), user xem gộp cuối.
- **Quyết định mới:** [0044](../design/decisions/0044-ho-thap-hon-mat-nguon-thi-day.md) (user 2026-10-29): ô nước chảy thấp hơn nguồn gốc của cây → mức 5 + phát sóng, trong `basinFillRadius` = 16 ô ngang [số tạm]. Commit `7bdf0ea`.

## Việc tiếp (theo thứ tự, mỗi lúc một phiên dùng Unity)
1. Review 0084.
2. Brief 0085: cài luật 0044 (`WorldEditor`, `WaterConfig`, `water_m1.json`), đổi kỳ vọng `WideHolePastLevelRadiusStaysEmpty` → hố rộng lớp dưới đầy. Planner con viết, Executor con chạy.
3. Brief lỗi 4: đứng dưới chân thác bị đẩy nổi 1 ô (`PlayerRules.InWater` có thể coi ô nước rơi là nước bơi [chưa xác minh]; ảnh `~/Documents/User/khang/Game/art-test/shots/user0081_under_fall.webp`).
4. Brief lỗi 5: vật rơi nằm trên mặt đất, không lơ lửng/xoay; rơi ngẫu nhiên gần khối bị phá (bán kính [số tạm] trong JSON). Code liên quan: `client/Assets/Game/Client/DropView.cs`.
5. Xong hết → gửi user một danh sách chơi thử gộp (các mục "User tự test" của 0082–0087, gồm lỗi 7 gờ đầu cột) + ghi giờ thực tế vào roadmap.

## Ghi chú
- Phá/đặt khối dưới chân player: giữ như hiện tại (user). Hiệu ứng bọt chân thác + hiển thị dòng chảy = bước 5k [ý tưởng], làm sau 5j.
- Phiên con có effort cao hơn phiên cha bị app hạ → ghi `Model thực thi` ≤ effort phiên cha (medium).
