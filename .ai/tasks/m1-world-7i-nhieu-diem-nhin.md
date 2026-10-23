# Task: M1 world 7i — nạp chunk theo nhiều điểm nhìn

> Model thực thi: Sonnet · medium  (AGENTS.md §10.2)
> Trạng thái: user xác nhận 2026-10-23. Bước đầu của [0029](../../design/decisions/0029-nhieu-diem-nhin-camera-quan-sat.md) (Chốt), dòng 7i trong [world-tech](../../design/04-tech/world-tech.md#kế-hoạch-prototype-m1). Ước lượng 1–1,5 ngày-người. Dự kiến 2 brief: A lõi + client, B đo điện thoại.

## Mục tiêu
Bộ nạp chunk nhận **một danh sách điểm nhìn** thay vì một tâm. Mỗi điểm nhìn có bán kính nạp/gỡ riêng; cột cần nạp là hợp các vùng. Đây là phần nền cho camera quan sát (7j) và cho việc nâng lên 4–5 điểm nhìn ở M2.

- **Pillar:** P3 "Tự do trong khung truyện" (0029, mục Hệ quả).
- **Thành phần:** client Unity. `ChunkStreamPlan` nằm ở `Game.Core` (C# thuần), `TerrainChunkView` ở client.

**Hiện trạng [repo]:**
- `ChunkStreamPlan.Update(px, pz, maxLoad, toLoad, toUnload)` chỉ nhận một tâm, cùng một cặp `LoadRadius`/`UnloadRadius` cho cả plan (`client/Assets/Game/Core/ChunkStreamPlan.cs:13,50`).
- `TerrainChunkView` lấy tâm từ `streamTarget` hoặc `Camera.main` (`TerrainChunkView.cs:106`), rồi gọi `plan.Update` mỗi `LateUpdate` với ngân sách `maxColumnsPerFrame` (`:270`).
- Test hiện có: `ChunkStreamPlanTests` (EditMode) và `TerrainStreamingPlayTests`. Có 6 test PlayMode khác gán `view.streamTarget`.

## Ngoài phạm vi
- **Camera quan sát:** công cụ đặt camera và nút xem qua camera thuộc 7j. Ở 7i, điểm nhìn thứ hai chỉ là một vị trí đặt bằng code/cờ đo.
- **GPU occlusion culling:** thuộc 7k.
- **Mô phỏng sinh vật ở map khác:** để M2 (0029 mục 3).
- **Nâng lên 4–5 điểm nhìn và đo ở 5 điểm:** để M2. 7i chỉ cần số tối đa là cấu hình và cấu trúc dữ liệu không giả định 2.
- **Collider và tương tác ở điểm nhìn phụ:** giữ nguyên cách hiện tại. Cột nạp được dựng như nhau, không tách loại.

## Tiêu chí chấp nhận (kiểm chứng được)
- [ ] **Lõi `ChunkStreamPlan` nhận danh sách điểm nhìn.**
  - Mỗi điểm nhìn gồm (x, z, bán kính nạp, bán kính gỡ).
  - Có số điểm nhìn tối đa, truyền vào khi tạo plan (không viết cứng). Vượt số tối đa → exception.
  - **Nạp:** cột chưa nạp có khoảng cách ≤ bán kính nạp của *ít nhất một* điểm nhìn.
  - **Gỡ:** cột đang nạp có khoảng cách > bán kính gỡ của *mọi* điểm nhìn.
  - **Thứ tự nạp:** sắp theo khoảng cách nhỏ nhất tới bất kỳ điểm nhìn nào, rồi theo chỉ số cột; vẫn dùng chung ngân sách `maxLoad` mỗi khung. (user chốt 2026-10-23).
  - `MissingWithin` vẫn dùng được cho từng điểm nhìn.
  - Giữ hàm một tâm hiện có (gọi qua danh sách 1 điểm) để test và scene cũ không đổi hành vi.
  - Kiểm bằng: test EditMode mới. Phải có: hợp vùng của 2 điểm ở xa nhau; một cột trong vùng của cả 2 chỉ nạp 1 lần; điểm nhìn rời đi thì gỡ đúng phần không còn ai giữ; 1 điểm cho kết quả giống hệt bản cũ; chạy 2 lần cùng dữ liệu cho cùng kết quả (tất định); vượt số tối đa thì ném exception. Toàn bộ `ChunkStreamPlanTests` cũ vẫn qua.
- [ ] **Client `TerrainChunkView` nạp theo danh sách điểm nhìn.**
  - Điểm nhìn chính vẫn là `streamTarget`/`Camera.main` như hiện tại.
  - Thêm API để client đăng ký/gỡ thêm điểm nhìn (Transform kèm bán kính). 7j sẽ dùng API này.
  - Số tối đa lấy từ cấu hình; M1 = 2 (0029 mục 1).
  - Kiểm bằng: test PlayMode. Thêm một điểm nhìn thứ hai ở map khác thì cột quanh đó được nạp, và `MissingColumnsWithin` tại đó về 0 sau đủ khung. Gỡ điểm nhìn đó thì cột được gỡ. Các test PlayMode cũ vẫn qua.
- [ ] **Đo trên điện thoại với 2 điểm nhìn ở hai map: ≥ 30 FPS** (dòng 7i, cột "Xong khi").
  - Dùng chế độ đo đi bộ có sẵn (`WalkBench`, brief 0039), bật thêm một cờ để đặt điểm nhìn thứ hai cố định ở Map 2. (user chốt 2026-10-23).
  - Ghi avgFps, p50/p99 ms, holeFrames, số cột đang nạp. So với lần đo WalkBench trước.
  - Dưới 30 FPS → ghi số liệu và báo Planner, không tự tối ưu trong task này.
  - Kiểm bằng: AI tự cài APK và đọc logcat (máy luôn ở chế độ USB dev).
- [ ] Toàn bộ EditMode + PlayMode qua; `git status` sạch sau bước dọn.

## Ràng buộc
- `Game.Core` không tham chiếu UnityEngine. Dùng số nguyên, tất định.
- Code và comment mới viết bằng tiếng Anh: `HardcodedTextScanTests` chặn chữ tiếng Việt.
- Chừa chỗ cho 5 điểm nhìn: không tạo mảng cứng 2 phần tử, không tách ngân sách theo từng điểm.
- Unity MCP dùng chung (0010). Sau PlayMode: `git checkout -- client/ProjectSettings/EditorSettings.asset`. Sau build APK: `git checkout -- client/ProjectSettings/UnityConnectSettings.asset`.

## Quyết định đã chốt (ai chốt, khi nào)
- Nạp theo hợp vùng; bán kính riêng cho từng điểm nhìn; M1 tối đa 2; số tối đa là cấu hình; đo điện thoại ở 2 điểm nhìn — user, 0029, 2026-10-22.
- Làm 7i tiếp theo sau 0032 — user, 2026-10-23.

- Thứ tự nạp: sắp chung theo khoảng cách nhỏ nhất tới bất kỳ điểm nhìn nào, ngân sách chung — user, 2026-10-23.
- Đo 2 điểm nhìn: cờ trong WalkBench, điểm cố định ở tâm Map 2, đo ngay trong 7i — user, 2026-10-23.

## Câu hỏi còn mở
_(không còn)_
