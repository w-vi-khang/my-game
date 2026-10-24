# Brief 0071 — Đo điện thoại: 7l e (20 ô ruộng có icon) + 7k (GPU Resident Drawer / occlusion A-B-C)

> Trạng thái: chờ làm
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

## Kết quả test

## Vướng mắc

---
<!-- PLANNER ghi khi review. -->

## Review
