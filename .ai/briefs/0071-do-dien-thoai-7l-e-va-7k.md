# Brief 0071 — Đo điện thoại: 7l e (20 ô ruộng có icon) + 7k (GPU Resident Drawer / occlusion A-B-C)

> Trạng thái: đã duyệt
> Review: nhẹ
> Model thực thi: Sonnet · medium
> Cập nhật: 2026-10-24

<!-- Không có task contract riêng: phần đo còn treo của brief 0065 (bước 9.4) và brief 0070 (QĐ 4). Không đổi code. -->

## Mục tiêu
Đo trên điện thoại Xiaomi 24095PCADG bằng các APK **đã build sẵn** (không build lại, không sửa code):
- **7l e** (brief 0065 bước 9.4): `client/Builds/walkbench.apk` (E0) và `client/Builds/walkbench-plots.apk` (E20, 20 ô ruộng) → tiêu chí dòng 7l e: E20 `avgFps ≥ 30`.
- **7k** (brief 0070 QĐ 4): `client/Builds/walkbench-gpu-a.apk` (A, GRD tắt, Forward), `-b.apk` (B, GRD, Forward+), `-c.apk` (C, GRD + occlusion, Forward+), rồi A lần nữa (A2) để xem độ ổn định.

## Bối cảnh cần đọc
- `.ai/briefs/0065-7l-e2-icon-o-ruong-sinh-vat-fps.md` bước 9.4 (cách chạy một lần đo) và mục Kết quả (dòng mẫu `WALKBENCH`).
- `.ai/briefs/0070-world-7k-thu-gpu-occlusion-culling.md` QĐ 1, 4, 5 và bảng đo Mac trong Kết quả.
- Số cũ để so: brief 0052, 24095PCADG, WalkBench 1 điểm nhìn `avgFps` 42,0–49,4.
- Máy [run 2026-10-24, phiên điều phối]: điện thoại bật **gỡ lỗi Wi‑Fi**, đã `adb connect 192.168.1.6:35093` → `24095PCADG`. Cổng gỡ lỗi Wi‑Fi có thể đổi: mất kết nối → `ADB mdns services`, lấy dòng `_adb-tls-connect._tcp 192.168.1.6:<cổng>` rồi `ADB connect`. **Không dùng `192.168.1.5:6060`** (ADT_3, Android TV).
- `ADB` = `adb` trên PATH (hoặc `/Applications/Unity/Hub/Editor/6000.4.8f1/PlaybackEngines/AndroidPlayer/SDK/platform-tools/adb`).

## Quyết định đã chốt (không được đổi)
1. Thứ tự: E0, E20, A, B, C, A2. Nghỉ 60 giây giữa hai lần; Thermal trước mỗi lần.
2. Mỗi lần làm đúng chuỗi của 0065 bước 9.4: Thermal → `install -r` → `logcat -c` → `monkey … LAUNCHER 1` → ~25 s kiểm khoá màn hình → chờ tổng 90 s → `logcat -d -s Unity | grep -E "WALKBENCH|Exception|GPU Resident"` (chưa có `device=` → chờ thêm 60 s) → `am force-stop com.viventa.walkbench`.
3. B hoặc C crash / không ra `WALKBENCH device=` sau 150 s → chép 5 dòng lỗi đầu (`logcat -d | grep -iE "Exception|FATAL|GPU Resident|BatchRenderer" | head`), ghi "không chạy được trên điện thoại", **đi tiếp** lần sau. Không thử cách khác.
4. Màn hình khoá / mất kết nối không nối lại được → Vướng mắc, trạng thái `cần Opus`, dừng (ghi các lần đã đo).
5. Không kết luận bật/tắt; Planner kết luận khi review.
6. Xong: gỡ app `ADB -s <máy> uninstall com.viventa.walkbench`.

## Phạm vi
- Được sửa: brief này (Kết quả trở xuống).
- Không được sửa: code, `design/`, `client/`. Không build APK. Không mở/điều khiển Unity Editor.

## Các bước
1. Chọn máy: `ADB devices -l`, `ADB -s <máy> shell getprop ro.product.model` = `24095PCADG`.
2. Chạy 6 lần đo theo QĐ 1–3.
3. Ghi Kết quả:
   - nguyên văn dòng `WALKBENCH device=…` của từng lần (+ `WALKBENCH plots:` của E20);
   - bảng 7l e: cột 0052 (42,0–49,4) · E0 · E20; dòng avgFps, p50Ms, p99Ms, maxMs, plots, plotIconsVisibleAvg, sysMemMaxMB, Thermal;
   - bảng 7k: A · B · C · A2; dòng avgFps, p50Ms, p99Ms, maxMs, gpuMsAvg, gpuMsP99, gpuSamples, grd, occ, holeFrames, Thermal; A/A2 lệch avgFps bao nhiêu %;
   - giờ bắt đầu/kết thúc.
4. Commit brief (`brief 0071: result`), trạng thái `chờ review`.

## Tiêu chí xong
- Có dòng `WALKBENCH device=Xiaomi 24095PCADG …` cho E0, E20, A, A2; B/C có dòng hoặc ghi lỗi theo QĐ 3.
- `grd`/`occ` đúng biến thể (A: Disabled/False; B: InstancedDrawing/False; C: InstancedDrawing/True).
- `git status` chỉ đổi brief này (ngoài các file đang sửa dở của phiên khác).

## Test sau khi xong

### Phần cần test
- Đo 7l e (E0, E20) và 7k (A, B, C, A2) — như "Các bước".

### Lệnh executor chạy
- Như "Các bước" (chỉ `adb`, không Unity).

### User tự test
Không.

## Dừng và chuyển lại Opus khi
- QĐ 4; cần sửa file ngoài Phạm vi; cần build lại APK.

---
<!-- Từ đây trở xuống: EXECUTOR ghi. -->

## Kết quả
Base commit: 0011f4f
Chạy bằng: claude-sonnet-5-5 · medium
Giờ: bắt đầu 13:53, kết thúc 14:09 (đo 13:53–14:08, gỡ app 14:09). Máy 24095PCADG qua `192.168.1.6:35093`; Thermal Status = 2 trước cả 6 lần; không khoá màn hình (isKeyguardShowing=false) ở cả 6 lần. Đã gỡ `com.viventa.walkbench`.
Không sửa code, không build, không mở Unity.

Dòng nguyên văn:
```
E0: WALKBENCH device=Xiaomi 24095PCADG | gpu=Mali-G615 MC2 (Vulkan) | screen=2712x1220 | frames=1747 avgFps=43,7 avgMs=22,90 p50Ms=24,95 p99Ms=33,34 maxMs=50,05 | distance=163,6 stuck=0 waterFrames=0 routeR=32 routeDry=True | viewpoints=1 secondCenter=none | holeFrames=0 secondHoleFrames=0 maxMissing=0 loadedColumnsMax=287 loadedChunksMax=861 | sysMemMaxMB=1091,7 gcReservedMaxMB=171,0 | creatures=3 | plots=4 plotIconsVisibleAvg=0,0 | pass30fps=True passNoHole=True
E20: WALKBENCH plots: 20 (target 20)
E20: WALKBENCH device=Xiaomi 24095PCADG | gpu=Mali-G615 MC2 (Vulkan) | screen=2712x1220 | frames=1768 avgFps=44,2 avgMs=22,62 p50Ms=24,95 p99Ms=33,32 maxMs=49,93 | distance=163,6 stuck=0 waterFrames=0 routeR=32 routeDry=True | viewpoints=1 secondCenter=none | holeFrames=0 secondHoleFrames=0 maxMissing=0 loadedColumnsMax=287 loadedChunksMax=861 | sysMemMaxMB=1103,0 gcReservedMaxMB=171,0 | creatures=3 | plots=20 plotIconsVisibleAvg=0,0 | pass30fps=True passNoHole=True
A: WALKBENCH device=Xiaomi 24095PCADG | gpu=Mali-G615 MC2 (Vulkan) | screen=2712x1220 | frames=1764 avgFps=44,1 avgMs=22,67 p50Ms=24,95 p99Ms=33,33 maxMs=49,93 | distance=163,7 stuck=0 waterFrames=0 routeR=32 routeDry=True | viewpoints=1 secondCenter=none | holeFrames=0 secondHoleFrames=0 maxMissing=0 loadedColumnsMax=287 loadedChunksMax=861 | sysMemMaxMB=1100,9 gcReservedMaxMB=171,0 | creatures=3 | plots=4 plotIconsVisibleAvg=0,0 | gpuMsAvg=21,60 gpuMsP99=27,19 gpuSamples=1738 grd=Disabled occ=False | pass30fps=True passNoHole=True
B: WALKBENCH device=Xiaomi 24095PCADG | gpu=Mali-G615 MC2 (Vulkan) | screen=2712x1220 | frames=2437 avgFps=60,9 avgMs=16,41 p50Ms=16,37 p99Ms=16,53 maxMs=32,79 | distance=163,8 stuck=0 waterFrames=0 routeR=32 routeDry=True | viewpoints=1 secondCenter=none | holeFrames=0 secondHoleFrames=0 maxMissing=0 loadedColumnsMax=287 loadedChunksMax=861 | sysMemMaxMB=794,8 gcReservedMaxMB=171,0 | creatures=3 | plots=4 plotIconsVisibleAvg=0,0 | gpuMsAvg=0,19 gpuMsP99=0,53 gpuSamples=2399 grd=InstancedDrawing occ=False | pass30fps=True passNoHole=True
C: WALKBENCH device=Xiaomi 24095PCADG | gpu=Mali-G615 MC2 (Vulkan) | screen=2712x1220 | frames=1189 avgFps=29,7 avgMs=33,65 p50Ms=33,28 p99Ms=49,92 maxMs=66,59 | distance=164,1 stuck=0 waterFrames=0 routeR=32 routeDry=True | viewpoints=1 secondCenter=none | holeFrames=0 secondHoleFrames=0 maxMissing=0 loadedColumnsMax=287 loadedChunksMax=861 | sysMemMaxMB=1156,1 gcReservedMaxMB=171,0 | creatures=3 | plots=4 plotIconsVisibleAvg=0,0 | gpuMsAvg=32,56 gpuMsP99=40,69 gpuSamples=1175 grd=InstancedDrawing occ=True | pass30fps=False passNoHole=True
A2: WALKBENCH device=Xiaomi 24095PCADG | gpu=Mali-G615 MC2 (Vulkan) | screen=2712x1220 | frames=1768 avgFps=44,2 avgMs=22,61 p50Ms=24,95 p99Ms=33,33 maxMs=49,93 | distance=163,6 stuck=0 waterFrames=0 routeR=32 routeDry=True | viewpoints=1 secondCenter=none | holeFrames=0 secondHoleFrames=0 maxMissing=0 loadedColumnsMax=287 loadedChunksMax=861 | sysMemMaxMB=1087,4 gcReservedMaxMB=171,0 | creatures=3 | plots=4 plotIconsVisibleAvg=0,0 | gpuMsAvg=21,60 gpuMsP99=27,45 gpuSamples=1744 grd=Disabled occ=False | pass30fps=True passNoHole=True
```

### Bảng 7l e (1 điểm nhìn, Thermal 2)
| | 0052 | E0 | E20 |
|---|---|---|---|
| avgFps | 42,0–49,4 | 43,7 | 44,2 |
| p50Ms | | 24,95 | 24,95 |
| p99Ms | | 33,34 | 33,32 |
| maxMs | | 50,05 | 49,93 |
| plots | | 4 | 20 (target 20) |
| plotIconsVisibleAvg | | 0,0 | **0,0** |
| sysMemMaxMB | | 1091,7 | 1103,0 |
- E20 `avgFps` 44,2 ≥ 30 → đạt tiêu chí avgFps. **Nhưng `plotIconsVisibleAvg = 0,0`** (0065 yêu cầu ≥ 10 icon trong khung) — số đo "icon trong khung" bằng 0 ở cả E0/E20: có thể icon không hiện trong WalkBench hoặc chỉ số không đếm đúng; chưa điều tra (ngoài phạm vi). Ghi cho Planner.
- Vì E20 không có icon nào trong khung, 44,2 FPS chưa chứng minh chi phí icon.

### Bảng 7k (Thermal 2 cả 4 lần)
| | A | B | C | A2 |
|---|---|---|---|---|
| avgFps | 44,1 | **60,9** | **29,7** | 44,2 |
| p50Ms | 24,95 | 16,37 | 33,28 | 24,95 |
| p99Ms | 33,33 | 16,53 | 49,92 | 33,33 |
| maxMs | 49,93 | 32,79 | 66,59 | 49,93 |
| gpuMsAvg | 21,60 | **0,19** | 32,56 | 21,60 |
| gpuMsP99 | 27,19 | 0,53 | 40,69 | 27,45 |
| gpuSamples | 1738 | 2399 | 1175 | 1744 |
| grd | Disabled | InstancedDrawing | InstancedDrawing | Disabled |
| occ | False | False | True | False |
| holeFrames | 0 | 0 | 0 | 0 |
| sysMemMaxMB | 1100,9 | 794,8 | 1156,1 | 1087,4 |
- A/A2 lệch avgFps 0,2% (44,1 vs 44,2) → ổn định.
- B và C đều chạy, không crash, không dòng lỗi GPU Resident/BatchRenderer trong log (lọc `grep` chỉ ra các dòng nhiễu của hệ thống Android, không liên quan).
- **Cảnh báo cho Planner (không kết luận, QĐ 5):** B có `gpuMsAvg=0,19 ms` (A 21,6) và `sysMemMaxMB` thấp hơn ~300 MB: nhìn không giống "nhanh hơn thật" mà giống GPU hầu như không vẽ gì (hoặc GRD không vẽ terrain/vật, hoặc GPU timing sai). Số 60,9 FPS có thể là khung gần rỗng. Chưa chụp màn hình để xác nhận (ngoài bước). Nên kiểm hình ảnh thật trước khi tin B. C (occlusion bật) chậm hơn A (29,7 FPS, dưới 30 — `pass30fps=False`), khớp cảnh báo docs "occlusion có thể làm tăng thời gian render".

## Kết quả test
- Đo 7l e: đạt (E0, E20 có dòng `device=`; E20 `plots=20`, avgFps 44,2 ≥ 30; plotIconsVisibleAvg = 0 ghi chú).
- Đo 7k: đạt — 4 dòng có `grd`/`occ` đúng biến thể (A Disabled/False; B InstancedDrawing/False; C InstancedDrawing/True; A2 Disabled/False).
- `git status`: chỉ đổi brief này (ngoài `.ai/handoff.md`, `.claude/settings.json` của phiên khác).

## Vướng mắc
Không có.

---
<!-- PLANNER ghi khi review. -->

## Review
- Model: Sonnet · medium khớp. Đủ dòng `WALKBENCH` cho cả 6 lần, `grd`/`occ` đúng biến thể; A/A2 lệch 0,2% → số ổn định. Đạt tiêu chí brief.
- 7l e: `plotIconsVisibleAvg` = 0 ở E20 — nguyên nhân [repo]: `WorldIconsView.Place` (`WorldIconsView.cs:151-156`) ẩn icon xa camera quá `PlayerMotion.IconMaxCells`; ô ruộng của `AddPlots` nằm 3–16 ô quanh tâm trại, đường đi bán kính 32, camera nhìn theo hướng đi → icon không vào khung. Tiêu chí dòng 7l e ("≥ 30 FPS với 20 ô ruộng có icon") **chưa chứng minh** → brief 0072.
- 7k: C (occlusion) 29,7 FPS < A 44,1 → **tắt occlusion culling** (0029 mục 4). B 60,9 FPS với GPU 0,19 ms và bộ nhớ thấp hơn ~300 MB: không tin được nếu chưa thấy hình → brief 0072 chụp màn hình A/B trên điện thoại.
- Kết luận: đã duyệt
