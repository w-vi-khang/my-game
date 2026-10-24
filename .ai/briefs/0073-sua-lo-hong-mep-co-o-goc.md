# Brief 0073 — Sửa lỗ hổng mép cỏ ở góc giao nhau (bug, user 2026-10-24)

> Trạng thái: chờ làm
> Review: đầy đủ
> Model thực thi: Opus · medium
> Cập nhật: 2026-10-24

<!-- Bug của world 7b4 (mép cỏ bo tròn, brief 0014) / 5.3. Không có task contract riêng: sửa lỗi, không đổi thiết kế. Quy trình theo skill bug-fix: tái hiện → khoanh vùng → nguyên nhân gốc → sửa tối thiểu → test hồi quy. -->

## Mục tiêu
Theo user: lớp mép cỏ có lỗ hổng ở chỗ các góc giao nhau. Ảnh lấy từ điện thoại khi chạy WalkBench, khoảng giây 17. Ảnh gốc nằm trong scratch của phiên điều phối, Executor không cần mở. Planner đã soi ảnh và thấy:
- **Khe tam giác tối nhỏ ở góc lõm**: chỗ hai khối cỏ gặp nhau theo hình chữ L, vùng giữa–dưới ảnh, cạnh nhân vật. Dải mép cỏ của khối trước kết thúc sát mặt cỏ của khối sau. Ở đó lộ một khe tối, nhìn xuyên được.
- **Khe giữa hai dải mép cỏ** ở bậc thang bên trái ảnh: chỗ dải của khối thấp gặp vách hoặc dải của khối cao hơn.

Cần tìm nguyên nhân gốc, sửa để không còn nhìn xuyên ở các góc, và thêm test hồi quy.

## Bối cảnh cần đọc
- `design/04-tech/world-tech.md:68` (mô tả mép cỏ) và các dòng 7b3, 7b4 (yêu cầu "mép cỏ kín", "kín từ dưới", "liền giữa ô kề").
- `client/Assets/Game/World/BlockMesher.cs`:
  - mép cỏ ở 716–870: `AddGrassLip`, `LipPoint`, `HasLip`, `AddLipEdge`, nắp hai đầu 795–819, `AddLipCorner` (chỉ cho góc lồi);
  - khối bo cạnh: `BevelRadius`, `R`, `ClipEars`, bảng `config`, AO góc lõm 620–640.
- `client/Assets/Game/Tests/World/BlockMesherTests.cs`:
  - helper `FirstHit(mesh, origin, dir, out dist, out normal)`;
  - các test mép cỏ 313–530 (`LipIsClosedFromBelowAndUnchangedFromAbove`, `LipEndCapMatchesClosedCrossSection`, `ConcaveCornerIsDarkerByVertexAo`…).
- Giả thuyết của Planner, **chưa kiểm**:
  - **(H1) góc lõm cùng độ cao.** Ô `g` có sườn +x lộ; `g+z` và `g+x+z` đặc. Dải +x của `g` và dải −z của `g+x+z` đều chỉ bịt nắp phẳng ở đầu. Không có mảnh nối góc lõm. Mặt trên mép cỏ bắt đầu ở −R và có cuộn bo tròn, nên quanh trục đứng của góc lõm có thể hở một khe.
  - **(H2) hai ô chéo nhau.** `g` và `g+x+z` đặc, `g+x` và `g+z` rỗng. Hai mảnh góc lồi cùng trục đứng, cộng với bo cạnh của hai khối, có thể để hở.
  - **(H3) dải kết thúc gặp khối cao hơn hoặc ô không phải cỏ.** Nắp bịt đúng mặt phẳng, nhưng vách bo cạnh của khối kia lõm vào trong R, nên hở giữa nắp và vách.
  - **(H4)** nguyên nhân khác.

## Quyết định đã chốt (không được đổi)
1. Không đổi hình dạng mép cỏ đã được user duyệt: mặt cắt, độ nhô, độ sâu, lượn sóng, mảnh góc lồi. Chỉ thêm hoặc chỉnh hình ở chỗ đang hở.
2. Mesh vẫn tất định, vẫn liền qua biên chunk. Collider không đổi (vẫn là mặt khối vuông).
3. Số đỉnh/tam giác tăng thêm ít. Ghi số đỉnh của world M1 (hoặc một vùng chuẩn có sẵn trong test/bench) trước và sau. Tăng > 5% thì ghi rõ lý do.
4. Không đụng `Game.Core`, không đụng dữ liệu `design/data/`.

## Phạm vi
- Được sửa:
  - `client/Assets/Game/World/BlockMesher.cs`;
  - `client/Assets/Game/Tests/World/BlockMesherTests.cs` (thêm test; test cũ chỉ được sửa nếu nó kiểm đúng chỗ hở cũ, và phải ghi lý do);
  - brief này (Kết quả trở xuống).
- Được tạo, không commit: ảnh chụp kiểm trong scratch.
- Không được sửa: file khác. `.ai/handoff.md`, `.claude/settings.json` có thay đổi của phiên khác: không stage.

## Các bước
1. **Tái hiện bằng test (đỏ trước khi sửa).** Viết test bắn tia "không nhìn xuyên" cho từng cấu hình H1–H3:
   - dựng world nhỏ 16³, mỗi cấu hình một test case;
   - với điểm đích trong khối đặc, lùi vào trong mỗi mặt ≥ `BevelRadius` + 0,05 (gần góc/cạnh cần soi), bắn tia từ ngoài khối theo nhiều hướng nhìn từ trên xiên xuống: 8–16 góc phương vị × độ cao 20°, 45°, 70°;
   - tia phải chạm một tam giác mặt quay về phía tia trước khi tới điểm đích. Không chạm, hoặc chạm mặt sau, là lỗ.
   - Cấu hình nào đỏ thì đó là chỗ hở. Ghi bảng cấu hình → số tia hở.
2. **(Tuỳ chọn, nên làm) Xem bằng mắt.** Dựng cấu hình đỏ trong Editor qua MCP, chụp 1–2 ảnh gần góc để chắc test bắt đúng lỗi user thấy. Ghi mô tả.
3. **Sửa tối thiểu** ở `BlockMesher` theo nguyên nhân tìm được. Ví dụ, nếu là H1: thêm mảnh nối góc lõm, hoặc kéo dài/vát đầu dải tới mặt phẳng của dải kia.
4. Test bước 1 xanh. Chạy lại các test mép cỏ và bo cạnh cũ, rồi cả bộ EditMode + PlayMode.
5. Ghi số đỉnh trước/sau (QĐ 3).
6. **Build lại `client/Builds/walkbench.apk`** (menu `Game/Build APK WalkBench`), dọn file như brief 0070 QĐ 10. Cài lên điện thoại 24095PCADG qua `adb connect 192.168.1.6:<cổng>` (cổng xem bằng `adb mdns services`, dòng `_adb-tls-connect`; không dùng `192.168.1.5:6060`). Chụp `exec-out screencap` lúc 17 s và 30 s → `client/Builds/0073-{17,30}.png`. Tả ngắn có còn khe ở góc không. Đọc dòng `WALKBENCH device=`: FPS không được tụt quá ~1 so với 44.
7. Kết quả: nguyên nhân gốc (1–3 câu), cách sửa, bảng tia hở trước/sau, số đỉnh, ảnh, test, giờ bắt đầu/kết thúc. Commit code (`fix: …`) + brief (`brief 0073: result`), trạng thái `chờ review`.

## Tiêu chí xong
- Test "không nhìn xuyên" cho H1–H3 (và cấu hình mới tìm thấy) đều xanh, và trước khi sửa có ít nhất một cấu hình đỏ. Nếu không cấu hình nào đỏ trước khi sửa → xem "Dừng".
- Các test mép cỏ cũ vẫn qua. EditMode + PlayMode cả bộ qua (596 + 268 trước brief, cộng test mới), console 0 Error. Sau PlayMode: `git checkout -- client/ProjectSettings/EditorSettings.asset`.
- `git diff --stat client/Assets/Settings client/ProjectSettings` rỗng; `.apk` / `.png` không track.

## Test sau khi xong

### Phần cần test
- Không nhìn xuyên ở góc lõm, ô chéo, đầu dải gặp vách (test mới, bước 1).
- Hình mép cỏ cũ không đổi: `LipProfileIsRoundedPillow`, `LipIsClosedFromBelowAndUnchangedFromAbove`, `LipEndCapMatchesClosedCrossSection`, `LipIsDeterministicAndContinuousAcrossCellsAndChunks`, `ConcaveCornerIsDarkerByVertexAo`.
- Hiệu năng: số đỉnh (QĐ 3) + FPS WalkBench điện thoại (bước 6).
- Regression chung: cả bộ EditMode + PlayMode.

### Lệnh executor chạy
1. MCP `refresh_unity` → `read_console` Error = 0 → `run_tests` EditMode lọc `BlockMesherTests` (trước và sau khi sửa).
2. `run_tests` EditMode + PlayMode toàn bộ. PlayMode làm rớt MCP → `sleep 60` rồi `get_test_job`.
3. MCP `execute_menu_item` `Game/Build APK WalkBench`; `git checkout --` các file bị build làm bẩn; adb cài, chạy, chụp, đọc log, `uninstall`.

### User tự test
Gộp vào lần chơi thử cuối: nhìn các góc lõm và bậc thang cỏ trong game (PC và điện thoại), kiểm không còn khe tối ở chỗ giao nhau.

## Dừng và chuyển lại Opus khi
- Không cấu hình nào đỏ trước khi sửa (test chưa bắt được lỗi user thấy). Khi đó ghi các cấu hình đã thử và ảnh Editor nếu có, rồi dừng.
- Muốn sửa phải đổi hình dạng mép cỏ đã duyệt (QĐ 1), hoặc số đỉnh tăng > 15%.
- Cần sửa file ngoài Phạm vi; cùng một lỗi sửa 2 lần vẫn chưa qua.

---
<!-- Từ đây trở xuống: EXECUTOR ghi. -->

## Kết quả

## Kết quả test

## Vướng mắc

---
<!-- PLANNER ghi khi review. -->

## Review
