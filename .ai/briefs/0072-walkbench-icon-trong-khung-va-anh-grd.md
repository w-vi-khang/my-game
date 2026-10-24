# Brief 0072 — Đo lại 7l e với icon trong khung + chụp màn hình kiểm bản GRD (7k B) trên điện thoại

> Trạng thái: chờ làm
> Review: đầy đủ
> Model thực thi: Sonnet · medium
> Cập nhật: 2026-10-24

<!-- Không có task contract riêng: phần còn lại của dòng 7l e và 7k (world-tech), theo review brief 0071. -->

## Mục tiêu
1. **7k B:** bản `walkbench-gpu-b.apk` (GPU Resident Drawer) ra 60,9 FPS nhưng GPU 0,19 ms, bộ nhớ ít hơn A ~300 MB (brief 0071). Chụp màn hình A và B trên điện thoại để Planner xem B có vẽ world thật không. **Không build lại.**
2. **7l e:** brief 0071 đo 20 ô ruộng nhưng `plotIconsVisibleAvg = 0`. Nguyên nhân [repo]:
   - `WalkBench.AddPlots` (`client/Assets/Game/Client/WalkBench.cs:219-236`) xới các ô cách tâm trại 3–16 ô;
   - nhân vật đi vòng bán kính `RouteRadius` (32) quanh tâm, camera nhìn theo hướng đi;
   - `WorldIconsView.Place` (`WorldIconsView.cs:151-156`) ẩn icon cách camera quá `iconMaxCells` = 30 (`design/data/sim_m1.json:39`).
   - Vì vậy không icon nào vào khung. Sửa bench để ô ruộng nằm **trước mặt trên đường đi**, thêm số đo khung có icon, build lại `walkbench-plots.apk`, đo lại.

## Bối cảnh cần đọc
- `.ai/briefs/0071-do-dien-thoai-7l-e-va-7k.md`: Kết quả (dòng `WALKBENCH` mẫu, cách kết nối máy) + Review.
- `.ai/briefs/0065-7l-e2-icon-o-ruong-sinh-vat-fps.md` bước 8–9 (`targetPlots`, test `TargetPlotsMarksTwenty`, chuỗi đo điện thoại 9.4).
- `client/Assets/Game/Client/WalkBench.cs`:
  - `Update` (121-216): `started`, `RouteCenter`, `RouteRadius`, `Carrot(...)` (hướng đi), `AddPlots` gọi trước khi lấy `lastPos`;
  - `Finish` (264-289).
- `client/Assets/Game/Client/Editor/WalkBenchBuild.cs`: menu `Game/Build APK WalkBench 20 Plots` (dòng 78), `EnsureScene(..., targetPlots)`.
- Máy: Xiaomi 24095PCADG, gỡ lỗi Wi‑Fi, `adb connect 192.168.1.6:<cổng>`. Cổng lấy từ `adb mdns services`, dòng `_adb-tls-connect._tcp`; lúc 13:5x là 35093. **Không dùng `192.168.1.5:6060`** (Android TV).

## Quyết định đã chốt (không được đổi)
1. **Chụp màn hình (mục tiêu 1)**, bằng APK có sẵn: `walkbench-gpu-a.apk` rồi `walkbench-gpu-b.apk`.
   - Mỗi bản làm chuỗi như brief 0071 QĐ 2. Thêm `adb -s <máy> exec-out screencap -p > client/Builds/0072-{a,b}-{20,40}.png` lúc khoảng 20 s và 40 s sau khi mở app.
   - `client/Builds/` bị git ignore, không commit ảnh.
   - Executor mở từng ảnh (Read), tả ngắn trong Kết quả: có địa hình / nhân vật / bầu trời không; B khác A ở đâu.
   - Không kết luận bật/tắt GRD.
2. **Vị trí ô ruộng khi `targetPlots > 0`** (thay cách xới 3–16 ô quanh tâm):
   - xới thành **khối 2 hàng ở phía trong vòng**, trên các cột ở bán kính `RouteRadius − 3` và `RouteRadius − 5`;
   - dọc cung, mỗi ô cách nhau ~2 ô; khối bắt đầu ~15 ô (theo cung) **phía trước** chỗ nhân vật xuất phát, theo chiều đi của `Carrot`;
   - đủ `targetPlots` ô; cột không xới được thì bỏ qua và lấy cột kế tiếp trên cung.
   - Không xới trên chính đường đi, để không đổi cách đi/nhảy.
   - `targetPlots == 0`: không đổi gì.
3. **Số đo mới** chèn vào đoạn `plots=… plotIconsVisibleAvg=…` (ngay sau `plotIconsVisibleAvg`):
   - `plotIconsVisibleMax`;
   - `iconFrames` = số khung đo có `VisiblePlotIcons ≥ 10`;
   - `iconFramesAvgMs` = ms trung bình của các khung đó (−1 nếu 0 khung).
   - Các trường khác giữ nguyên thứ tự.
4. **Tiêu chí 7l e** (Planner xét khi review): `iconFrames ≥ 100` và `1000 / iconFramesAvgMs ≥ 30`. Executor chỉ ghi số.
5. Đo 7l e trên điện thoại: chạy `walkbench-plots.apk` (bản mới) **2 lần**, nghỉ 60 s giữa hai lần, mỗi lần theo chuỗi brief 0071 QĐ 2. Chụp thêm 1 ảnh lúc khoảng 30 s của lần 1 (`client/Builds/0072-plots-30.png`), tả có thấy icon ô ruộng không.
6. Cấu hình repo không đổi:
   - sau build: `git checkout -- client/ProjectSettings/UnityConnectSettings.asset`, và các file build làm bẩn như brief 0070 QĐ 10;
   - `WalkBench.unity` không còn `targetPlots: 20`.

## Phạm vi
- Được sửa:
  - `client/Assets/Game/Client/WalkBench.cs` (QĐ 2–3);
  - test WalkBench trong `client/Assets/Game/Tests/PlayMode/` (file chứa `TargetPlotsMarksTwenty`);
  - brief này (Kết quả trở xuống).
- Được tạo, không commit: `client/Builds/walkbench-plots.apk` (build đè), ảnh `client/Builds/0072-*.png`.
- Không được sửa: code game khác, `design/`, `client/Assets/Settings/*`, `client/ProjectSettings/*`.
- `.ai/handoff.md`, `.claude/settings.json` có thay đổi của phiên khác: không stage, không khôi phục.

## Các bước
1. **Chụp màn hình A/B** (QĐ 1). Làm trước, vì chỉ cần adb.
2. **Sửa `AddPlots`** (QĐ 2). Gọi nó sau khi biết `RouteRadius` và vị trí chân nhân vật (hiện đã đúng thứ tự).
   - Chiều đi: lấy `Carrot(RouteCenter, RouteRadius, feet, leadDegrees) - feet` tại lúc xuất phát, so với vector bán kính để biết chiều tăng hay giảm của góc.
   - Log `WALKBENCH plots: N (target M)` giữ nguyên dạng.
3. **Thêm số đo** (QĐ 3) trong vòng đo và `Finish`.
4. **Test PlayMode**:
   - `TargetPlotsMarksTwenty` vẫn qua;
   - thêm assert `Summary` chứa `plotIconsVisibleMax=`, `iconFrames=`, `iconFramesAvgMs=`;
   - thêm test `TargetPlotsAheadOnRoute`: sau khi xới, mọi ô ruộng cách `RouteCenter` trong khoảng `[RouteRadius − 6, RouteRadius − 2]`.
   - Chạy riêng các test WalkBench trước, rồi cả bộ PlayMode + EditMode.
5. **Build** `Game/Build APK WalkBench 20 Plots` qua MCP. Dọn theo QĐ 6.
6. **Đo** (QĐ 5).
7. **Kết quả:**
   - giờ bắt đầu/kết thúc;
   - mô tả ảnh A/B và ảnh ô ruộng;
   - 2 dòng `WALKBENCH device=…` nguyên văn;
   - bảng: lần 1 · lần 2 × (avgFps, p99Ms, plots, plotIconsVisibleAvg, plotIconsVisibleMax, iconFrames, iconFramesAvgMs → FPS, Thermal);
   - file đổi, test.
   - Commit code (`brief 0072: …`) và brief (`brief 0072: result`), trạng thái `chờ review`.

## Tiêu chí xong
- 4 ảnh A/B có trong `client/Builds/`, có mô tả.
- Bench mới: hai lần đo có `plotIconsVisibleMax ≥ 10` và `iconFrames > 0`.
- EditMode + PlayMode cả bộ qua (596 + 267 trước brief, cộng test mới), console 0 Error. Sau PlayMode: `git checkout -- client/ProjectSettings/EditorSettings.asset`.
- `git diff --stat client/Assets/Settings client/ProjectSettings` rỗng; `git status` không có `.apk` / `.png` bị track.

## Test sau khi xong

### Phần cần test
- Ô ruộng nằm trước mặt trên đường đi (bước 2): test `TargetPlotsAheadOnRoute` + ảnh `0072-plots-30.png`.
- Số đo mới (bước 3): `TargetPlotsMarksTwenty` + 2 dòng `WALKBENCH` điện thoại.
- Ảnh A/B (bước 1).
- Regression: cả bộ EditMode + PlayMode; `targetPlots == 0` không đổi (các test WalkBench cũ qua).

### Lệnh executor chạy
1. adb như brief 0071; `exec-out screencap -p`.
2. MCP `refresh_unity` → `read_console` Error = 0 → `run_tests` (PlayMode lọc WalkBench, rồi EditMode + PlayMode toàn bộ). PlayMode làm rớt MCP → `sleep 60` rồi `get_test_job`.
3. MCP `execute_menu_item` `Game/Build APK WalkBench 20 Plots`; dọn theo QĐ 6.
4. adb đo 2 lần; `adb uninstall com.viventa.walkbench` khi xong.

### User tự test
Không (Planner xem ảnh).

## Dừng và chuyển lại Opus khi
- Máy mất kết nối không nối lại được, hoặc màn hình khoá → ghi phần đã làm, `cần Opus`.
- Không tìm đủ 20 cột xới được trên cung (QĐ 2) → ghi số đã xới, `cần Opus`.
- Cần sửa file ngoài Phạm vi; cùng một lỗi sửa 2 lần vẫn chưa qua.

---
<!-- Từ đây trở xuống: EXECUTOR ghi. -->

## Kết quả

## Kết quả test

## Vướng mắc

---
<!-- PLANNER ghi khi review. -->

## Review
