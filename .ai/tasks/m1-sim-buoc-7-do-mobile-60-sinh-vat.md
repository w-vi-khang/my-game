# Task: M1 simulation bước 7 — đo trên điện thoại với 60 sinh vật giả (≤ 5 ms/nhịp)

> Model thực thi: Sonnet · medium  (AGENTS.md §10.2 — Executor làm theo brief; Planner Opus viết brief từ contract này)
> Trạng thái: brief [0028](../briefs/0028-sim-do-mobile-60-sinh-vat.md) — điện thoại đạt (p95 0,956 ms), chờ user xem SimBench trên Mac. Ước lượng 1 ngày-người ([simulation-tech](../../design/04-tech/simulation-tech.md#kế-hoạch-prototype-m1) bước 7), nằm trong hạng mục "Lệnh làm việc / Tìm đường" của M1 ([roadmap](../../design/05-roadmap.md)). Dự kiến một brief.

## Mục tiêu
Làm bước 7 của [kế hoạch prototype simulation](../../design/04-tech/simulation-tech.md#kế-hoạch-prototype-m1): đo mô phỏng trên điện thoại khi số sinh vật hoạt động là tối đa, **60** (có nạp, [Hiệu năng](../../design/04-tech/simulation-tech.md#hiệu-năng)). Đo ở mức tải giống lúc chơi thật, có cả luật 6a và 6b.

Pillar: vận hành trang trại bằng sinh vật. Phần mô phỏng phải đủ nhẹ để 60 con chạy trên máy tầm trung.

Thành phần phụ trách: **client Unity**.
- Sinh bố trí: `Game.Core.Sim`, C# thuần.
- Đo, scene, build APK: `Game.Client` và Editor.

## Quyết định đã chốt (user, 2026-10-21)
1. **Bố trí: trại lớn có việc.** Một lệnh debug sinh thêm sinh vật cho đủ **60** con, mỗi loài L1, L2, L3 có **20** con. Bố trí nằm trên trại SimView ở Map 1, trong vùng mô phỏng 64 × 64.
   - Thêm nhà cho đủ chỗ ở. Nhà chứa 2 con (`sim_m1.json` `house.capacity`), nên cần khoảng 30 nhà.
   - Thêm lệnh trồng cây và kho để mọi con có việc, khoảng 15 lệnh × 4 ô và vài kho. Số cụ thể chốt trong brief.
   - Chỗ nào không đặt được thì bỏ qua và ghi log số đã đặt.
   - Luật 6a (ô ruộng, huỷ vụ) và 6b (chặn nhau, rảnh quanh nhà) vẫn bật như lúc chơi.
2. **Đo: p95 ≤ 5 ms/nhịp, task này chỉ đo.**
   - Dùng một APK riêng: scene SimView, tự sinh 60 con khi mở app, không cần chạm.
   - Bấm `Stopwatch` quanh mỗi `Simulation.Tick()`. Chạy ít nhất 3 phút, tức ít nhất 1800 nhịp.
   - Cứ 10 giây ghi logcat: trung bình, p95, max ms/nhịp, số lần tìm đường mỗi giây và FPS.
   - **Đạt khi p95 ≤ 5 ms.**
   - Không đạt thì ghi số liệu và pha/chỗ tốn nhất (đo theo pha, hoặc Profiler nếu brief chọn). Việc tối ưu tách thành **task sau**, không làm ở đây.

## Planner đề xuất (user xác nhận khi duyệt brief)
- Lệnh debug là một hàm trong `Game.Core.Sim`, ví dụ `SimDemo.BuildStress(sim, site, 60)`, có test EditMode. SimView gọi hàm này bằng phím tắt trên PC (ví dụ F3) và tự gọi khi chạy trong APK đo.
- Số lần tìm đường mỗi giây đếm bằng bộ đếm trong lõi, chỉ đọc, không ảnh hưởng mã băm.
- Mỗi giây chạy 10 nhịp cố định, nên ngân sách khung hình là 5 ms × số nhịp mỗi khung. Brief ghi luôn số nhịp lớn nhất trong một khung (`maxTicksPerFrame`) đo được.
- Build APK theo mẫu `BenchBuild`: thêm menu `Game/Build APK SimBench` và scene tạo tự động. Không sửa scene Bench hiện có.

## Ngoài phạm vi
- Tối ưu khi chưa đạt ngưỡng (task sau, nếu cần).
- Đo thời gian tua nhanh 8 giờ vắng mặt: thuộc M2, theo "Chuyển sang M2" trong simulation-tech.
- Ngân sách A\*, vùng liên thông (M2).
- Đổi luật mô phỏng, `sim_m1.json` hoặc ngưỡng 5 ms.
- Hiển thị đẹp cho 60 con: vẫn dùng khối hộp như SimView.

## Tiêu chí chấp nhận (kiểm chứng được)
- [ ] Hàm sinh bố trí tạo đúng 60 sinh vật, mỗi loài 20 con, con nào cũng có nhà. Mọi lệnh và kho đều nằm trong vùng mô phỏng. — kiểm bằng: test EditMode mới trên world 3 map (`run_tests` EditMode)
- [ ] Chạy bố trí 60 con trong 4000 nhịp trên Editor:
  - không lần nào hai con chung ô, trừ lúc đang dùng phao 5 giây;
  - không con rảnh nào dừng trên ô ruộng;
  - kho có thêm nông sản;
  - cùng seed thì cùng mã băm.
  — kiểm bằng: test EditMode, theo mẫu test `SimDemo` của 6b
- [ ] Bộ đếm tìm đường không làm đổi mã băm của test cũ. — kiểm bằng: EditMode và PlayMode cả bộ đều xanh
- [ ] Có APK đo; trên điện thoại chạy ít nhất 3 phút; logcat có dòng đo mỗi 10 giây (trung bình / p95 / max ms, tìm đường/giây, FPS, số sinh vật = 60). — kiểm bằng: `adb logcat -d -s Unity | grep SIMBENCH`, kết quả chép vào brief
- [ ] Có kết luận: p95 ≤ 5 ms là **đạt** hoặc **không đạt**, ghi kèm số liệu vào cột "Xong khi" bước 7 của simulation-tech. Nếu không đạt thì ghi thêm pha tốn nhất và đề xuất task tối ưu. — kiểm bằng: Planner review
- [ ] Ghi giờ thực tế so với 1 ngày-người vào roadmap.

## Ràng buộc
- `Game.Core` không tham chiếu UnityEngine. Mô phỏng chỉ dùng số nguyên và phải tất định.
- Không sửa test cũ. Test cũ hỏng → `cần Opus`.
- Unity Editor dùng chung qua MCP (0010). Phải dọn sau khi chạy:
  - sau PlayMode → `git checkout -- client/ProjectSettings/EditorSettings.asset`;
  - sau build APK → `git checkout -- client/ProjectSettings/UnityConnectSettings.asset`.
- Điện thoại luôn cắm USB ở chế độ dev. AI tự cài APK và đọc logcat, không hỏi user.

## Câu hỏi còn mở
- _(không có)_
