# Brief 0073 — Sửa lỗ hổng mép cỏ ở góc giao nhau (bug, user 2026-10-24)

> Trạng thái: đã duyệt
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
5. **(Trả lời vướng mắc, Planner 2026-10-24) Chọn phương án A.**
   - Lỗi user thấy là **vết lõm bo cạnh ở góc lõm chưa được mép cỏ che**. Mesh vẫn kín, nhưng vết lõm hiện thành tam giác tối: pháp tuyến nghiêng, có AO. Ảnh gốc khớp vị trí góc lõm.
   - Tiêu chí test bước 1 đổi thành **"không lộ lõm bo cạnh ở góc lõm"**. Tia từ trên xuống, thẳng đứng và xiên 45° theo 4 hướng chéo, nhắm vào điểm trên mặt trên ô cỏ trong vùng ≤ R quanh trục góc lõm. Tia phải chạm mặt quay lên ở y ≥ mặt trên − 0,02. Giữ cả test nhìn xuyên (đang xanh) làm hồi quy.
   - Sửa: thêm **mảnh góc lõm** trong `AddGrassLip`, đúng điều kiện Executor đề xuất:
     - ô cỏ `g` có sườn `s` lộ;
     - ô `g+e` (dọc cạnh) đặc, ô `g+s+e` đặc;
     - trên `g+e` không đặc.
   - Hình mảnh: quạt phủ góc phần tư quanh trục góc ở y = mặt trên. Được nâng ≤ 0,002 để tránh z-fighting. Màu cỏ, pháp tuyến lên.
   - Đếm mỗi góc lõm một lần, tránh trùng khi duyệt từ ô `g` của hai sườn.
   - Ô cỏ có ô đặc phía trên → không thêm (không có mép cỏ).
   - Chỉ áp dụng cho ô cỏ có mép cỏ; vật liệu khác giữ nguyên bo cạnh.
6. **Bước 2 (xem bằng mắt) thành bắt buộc**, làm sau khi sửa. Dựng H1 trong Editor qua MCP, chụp gần góc lõm trước/sau, kiểm cả z-fighting. Bước 6 (ảnh điện thoại) giữ nguyên.

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
- (Vòng 2) Test mới (QĐ 5) không đỏ trước khi sửa → dừng. Mảnh góc lõm gây z-fighting thấy được mà nâng ≤ 0,002 không hết → dừng.
- Không cấu hình nào đỏ trước khi sửa (test chưa bắt được lỗi user thấy). Khi đó ghi các cấu hình đã thử và ảnh Editor nếu có, rồi dừng.
- Muốn sửa phải đổi hình dạng mép cỏ đã duyệt (QĐ 1), hoặc số đỉnh tăng > 15%.
- Cần sửa file ngoài Phạm vi; cùng một lỗi sửa 2 lần vẫn chưa qua.

---
<!-- Từ đây trở xuống: EXECUTOR ghi. -->

## Kết quả
- Base commit: e9a8a06
- Chạy bằng: claude-opus-5-5 · medium
- Bắt đầu: 2026-10-24 14:37 · dừng 14:44 (cần Opus, chưa sửa code)
- Vòng 2 (QĐ 5–6): base 572fcef, bắt đầu 2026-10-24 14:47, kết thúc 15:13. Tổng thời gian thực tế: vòng 1 0:07 + vòng 2 0:26 ≈ 0,55 giờ.
- **Commit code: `b739d0a`.** File đổi:
  - `client/Assets/Game/World/BlockMesher.cs` (+`AddLipConcave`, 2 hằng);
  - `client/Assets/Game/Tests/World/BlockMesherTests.cs` (+`CornerWorld`, `RayMesh`, `ClearanceToSolid`, `SeeThroughRays`, 2 test × 6 cấu hình). Test cũ không sửa.
- **Nguyên nhân gốc:**
  - Ở góc lõm của mặt trên ô cỏ (hai ô kề theo x và theo z đặc, ô chéo rỗng), phép bo cạnh (lăn cầu R) làm mặt trên ô đó lõm xuống tới R quanh trục góc, trong góc phần tư R × R.
  - Hai dải mép cỏ bên cạnh thì dừng hoặc bắt đầu đúng ở trục, nên không dải nào phủ vết lõm. Nó hiện thành tam giác tối nhỏ: pháp tuyến nghiêng, có AO.
  - Mesh vẫn kín, nên không có chỗ nhìn xuyên theo nghĩa hình học.
- **Cách sửa:**
  - `AddGrassLip` gọi `AddLipConcave` cho 4 góc của mặt trên ô cỏ hở trên. Điều kiện: ô kề x đặc, ô kề z đặc, ô chéo rỗng.
  - Mảnh là lục giác phẳng phủ góc phần tư [0, R]², nâng `LipConcaveLift` = 0,002. Mảnh chồm `LipConcaveOverlap` = 0,01 qua hai đường trục (không chồm sang ô chéo rỗng) để không lọt khe pixel với mặt trên dải bên cạnh. Màu cỏ, pháp tuyến lên, 6 đỉnh / 4 tam giác.
  - Mỗi trục góc chỉ một trong 4 ô quanh nó thoả điều kiện, nên tự nhiên thêm đúng một lần, không cần khử trùng.
  - **Lệch so với chữ trong QĐ 5:** điều kiện xét từ ô có vết lõm (g+e), không đòi ô g có dải mép cỏ và không đòi hở phía trên g+s+e. Với H1 hai cách cho cùng kết quả. Chỉ cách này phủ được 2 dạng H4 còn sót ở lần sửa đầu: góc lõm có khối cao hơn đè lên ô bên kia, ví dụ quanh (4, 8, 3); và ô cỏ trong hốc giữa hai vách cao, ví dụ (8, 7, 2).
  - Hình dải, mảnh góc lồi, collider: không đổi.
- **Số đỉnh** (world M1, `BlockMesher.BuildAll`) [run execute_code]: 150 025 → **151 201 (+0,78 %)**. Tam giác 165 578 → **166 362 (+0,47 %)**, tức 196 góc lõm × 4. Số "trước" lấy bằng cách bỏ tam giác/đỉnh của mảnh (y lẻ 0,002); số tam giác trước khớp MESHSTATS cũ 165 578.
- **Ảnh Editor (bước 2, scratch, không commit):** render H1 trong preview scene bằng shader Game/Terrain.
  - Trước (`0073-editor-before-near.png`): vết lõm hình thoi khác màu ngay ở trục góc lõm.
  - Sau, lần 1 (mảnh vuông chỉ chạm mép): hết vết lõm nhưng còn vệt sáng lấm tấm hình chữ V dọc hai đường trục (khe rasterize) → thêm phần chồm 0,01.
  - Sau, lần 2 (`0073-editor-after2-{near,far,grazing}.png`): mặt cỏ liền; phóng to 4× chỉ còn 1 điểm dưới 1 pixel ở trục. Nhìn sát mặt (grazing) và nhìn xa: không thấy z-fighting.
- **Điện thoại 24095PCADG** (192.168.1.6:35093, Thermal Status 2 từ đầu tới cuối):
  - `0073-17.png`: bậc thang cỏ + nhiều góc lõm quanh nhân vật, **không thấy khe/tam giác tối ở góc**. HUD ghi "measuring… 32 s" (lệch với 17 s mở app: đồng hồ WalkBench khác mốc, chưa kiểm).
  - `0073-30.png`: hành lang giữa hai vách cỏ, không thấy khe ở góc.
- File bị build làm bẩn, đã `git checkout --`: `client/Assets/Game/Client/Scenes/WalkBench.unity.meta`, `client/ProjectSettings/UnityConnectSettings.asset`. Sau PlayMode: `client/ProjectSettings/EditorSettings.asset`. `.apk`/`.png` bị ignore (không track). Đã `adb uninstall com.viventa.walkbench`.

## Kết quả test
- **Vòng 2, test mới** [run 2026-10-24]:
  - `NoBevelDentShowsAtGrassCornersFromAbove(H1…H4)`: tia trên xuống thẳng + xiên 45° × 4 hướng chéo, vào 6 điểm ≤ 0,1 quanh mỗi góc mặt trên ô cỏ. Phải chạm mặt trước ở y ≥ mặt trên − 0,02. Bỏ tia có gốc cách ô đặc < R.

    | Cấu hình | Trước sửa | Sau sửa |
    |---|---|---|
    | H1 | **13** / 14 148 (đỏ) | 0 / 14 148 |
    | H2 | 0 | 0 |
    | H3 | 0 | 0 |
    | H3b | 0 | 0 |
    | H3c | 0 | 0 |
    | H4 | **136** / 9 780 (đỏ) | 0 / 9 780 |

    Lần chạy đầu chưa có bộ lọc gốc tia: H2 cũng đỏ 2 tia. Gốc tia khi đó nằm trong phần bo của chỗ thắt hai cột chéo, chạm mặt sau. Đó là lỗi của test, không phải chỗ hở → thêm bộ lọc gốc tia cách ô đặc ≥ R. Kết quả "trước sửa" ở bảng là sau khi đã lọc: chạy lại trên `BlockMesher.cs` gốc (đã khôi phục tạm bằng `git checkout`).
  - `NoSeeThroughAtCorners(H1…H4)`: 0 tia hở cả trước lẫn sau sửa (hồi quy).
- Hình mép cỏ cũ không đổi: `LipProfileIsRoundedPillow`, `LipIsClosedFromBelowAndUnchangedFromAbove`, `LipEndCapMatchesClosedCrossSection`, `LipIsDeterministicAndContinuousAcrossCellsAndChunks`, `ConcaveCornerIsDarkerByVertexAo` → **đạt** (`BlockMesherTests` 34/34).
- Regression chung: EditMode **608/608** (596 + 12 mới), PlayMode **268/268**, console 0 Error → **đạt**.
- `git diff --stat client/Assets/Settings client/ProjectSettings`: rỗng → **đạt**.
- Hiệu năng:
  - Số đỉnh +0,78 % (< 5 %) → **đạt**.
  - FPS WalkBench điện thoại, cùng máy, Thermal Status 2 cả hai lần: lần 1 avgFps **41,3** (p99 41,6 ms); lần 2 **44,5** (p50 24,95, p99 33,30 ms). Mốc brief 0072: 44.
  - Lần 1 tụt 2,7, có thể do nhiệt (status 2 trước khi mở app); lần 2 đạt. Đánh giá: **đạt (lần 2), lần 1 cần lưu ý**.
  - `plots=4` (0072 là 20), chưa kiểm vì sao, không thuộc phạm vi brief.
- Chờ user: nhìn góc lõm, bậc thang cỏ trên PC và điện thoại (gộp lần chơi thử cuối).
- Vòng 1: chưa đổi file code nào. Test viết ở bước 1 lưu thành patch (không commit) ở `side-session-notes/local_78cbbfd7-…/0073-test-seethrough-diag.patch` → `git apply` để dùng lại.

### Vòng 1 (giữ để đối chiếu)
Bước 1, test `NoSeeThroughAtCorners(config)` (EditMode) [run 2026-10-24]. Dựng world 16³ bằng các cột địa hình. Ở mỗi ô lộ mặt trên lấy 8 điểm đích, cách mặt ô 0,18 (= R + 0,055). Bắn 12 phương vị × độ cao 20°/45°/70°. Gốc tia cách đích 2,5, cách ô đặc ≥ 0,4.

| Cấu hình | Tia hở / tổng | Chẩn đoán "lõm" (xem dưới) |
|---|---|---|
| H1 góc lõm chữ L cùng độ cao | 0 / 32 842 | **2** / 29 040 — tại góc lõm (7, 8, 7) |
| H2 hai ô chéo | 0 / 30 892 | 0 |
| H3 dải gặp bậc cao hơn | 0 / 32 288 | 0 |
| H3b dải gặp ô đá cùng độ cao | 0 / 32 697 | 0 |
| H3c bậc thang dọc sườn | 0 / 31 961 | 0 |
| H4 địa hình ngẫu nhiên 6–9 (seed 73) | 0 / 22 703 | **26** / 29 040 — mẫu đều ở góc lõm |

Cả bộ `BlockMesherTests` 28/28 qua [run]. **Không cấu hình nào đỏ theo tiêu chí nhìn xuyên** → dừng theo brief.

Chẩn đoán thêm (không assert). Bắn tia từ trên xuống, thẳng đứng và xiên 45° theo 4 hướng chéo, vào điểm trên mặt trên ô cỏ sát mép. Nếu chạm thấp hơn mặt trên > 0,04 thì tính là lộ lõm bo cạnh không có mép cỏ che. Mẫu:
- H1: tia thẳng xuống (6.98, 8, 7.02) chạm y = 7.943; tia xiên vào (6.94, 8, 7.06) chạm đúng trục góc (7, 7.94, 7).
- H4: (3.98, 8, 3.98) → 7.943; (11.98, 8, 3.02) → 7.943; (2.98, 9, 4.02) → 8.943.

## Vướng mắc
_(Vòng 1 — đã trả lời: QĐ 5–6, phương án A.)_
**Câu hỏi:** test nhìn xuyên không đỏ. Có chuyển tiêu chí sang "không lộ lõm bo cạnh ở góc lõm" rồi sửa không?

**Phân tích** [suy luận từ code + số đo DIAG, chưa xem bằng mắt]:
- Mesh khối bo cạnh kín (`AllVertexConfigurationsAreClosed`, `RandomShapesAreClosed` qua). Vì vậy tia từ ngoài không thể tới điểm trong khối mà không chạm mặt trước. Chỗ "nhìn xuyên" như user thấy không tồn tại theo nghĩa hình học.
- Điều tìm thấy là H1, góc lõm. Ô `g` lộ sườn +x; `g+z` và `g+x+z` đặc, cùng cao. Mặt trên của `g+z` bị bo lõm quanh đỉnh góc (x = 7, z = 7). Đây là cạnh lồi rẽ góc lõm, sâu tới ~R = 0,125 ngay tại trục góc, đo được 0,057 ở cách 0,02/0,02.
- Vùng lõm là góc phần tư x ∈ [7−R, 7], z ∈ [7, 7+R], tức phía trong của góc lõm. Dải +x của `g` chỉ phủ tới z = 7 (nắp phẳng, `BlockMesher.cs:796-819`). Dải −z của `g+x+z` chỉ phủ từ x = 7. Không dải nào phủ góc phần tư này. `AddGrassLip` (`:719-731`) chỉ có mảnh góc lồi.
- Nhìn từ trên xiên xuống, vết lõm này hiện thành mảnh tam giác nhỏ tối: pháp tuyến nghiêng, AO góc lõm, màu cỏ tối, kẹp giữa hai nắp dải. Rất có thể đây là "khe tam giác tối" user thấy. Không thấy dấu hiệu nào khác cho H2/H3.

**Phương án:**
- **A (đề xuất):**
  - Tiêu chí test bước 1 đổi thành: tia từ trên xuống (thẳng + xiên) vào mặt trên ô cỏ phải chạm ở y ≥ mặt trên − 0,02. Không lộ lõm bo cạnh ở chỗ mép cỏ.
  - Sửa: thêm mảnh "góc lõm" trong `AddGrassLip`. Điều kiện: ô cỏ `g` có sườn `s` lộ, ô `g+e` (dọc cạnh) đặc, ô `g+s+e` đặc, và trên `g+e` không đặc.
  - Mảnh là quạt phẳng ở y = top phủ góc phần tư [−R, 0] × [0, R] quanh trục góc. Có thể kéo nắp/mặt trên dải thêm R qua đầu dải.
  - Không đổi mặt cắt, độ nhô, lượn sóng. Tăng ~2–4 tam giác mỗi góc lõm.
  - Kiểm DIAG về 0. Kiểm thêm không có z-fighting: mảnh nằm ở y = top, trùng mặt trên phẳng của ô ngoài vùng lõm.
- **B:** sửa bảng bo cạnh: cạnh lồi rẽ góc lõm thì không bo phần mặt trên trong góc lõm. Rủi ro đổi hình khối đã duyệt, ảnh hưởng mọi vật liệu → không đề xuất.
- **C:** Planner xem ảnh gốc / chụp Editor trước để xác nhận đúng chỗ lõm này. Bước 2 tuỳ chọn chưa làm (tiết kiệm lượt).


---
<!-- PLANNER ghi khi review. -->

## Review
- Model: Opus · medium khớp. Diff `b739d0a`: `AddLipConcave` (6 đỉnh / 4 tam giác, nâng 0,002, chồm 0,01 không sang ô chéo), gọi từ `AddGrassLip` cho 4 góc; dải, mảnh góc lồi, collider không đổi → QĐ 1–2, 4 đạt. Trong Phạm vi.
- Lệch QĐ 5 chấp nhận: điều kiện xét từ ô có vết lõm (vẫn chỉ ô cỏ hở trên) thay vì từ ô có dải — phủ thêm 2 dạng H4 (khối cao đè ô bên kia, hốc giữa hai vách); mỗi trục góc đúng một lần.
- Tiêu chí: test mới đỏ trước (H1 13, H4 136 tia) → xanh sau; test nhìn xuyên 0 cả trước/sau (hồi quy); test mép cỏ cũ qua; 608 + 268; cấu hình sạch; số đỉnh +0,78% (< 5%). Ảnh Editor trước/sau + điện thoại không còn khe, không z-fighting.
- FPS điện thoại 41,3 / 44,5 (lần 1 nghi nhiệt, Thermal 2); +0,47% tam giác không đủ giải thích 2,7 FPS → chấp nhận, theo dõi ở lần đo sau.
- "measuring… 32 s" là số giây **còn lại** (`WalkBench.cs:212`: `warmupSeconds + measureSeconds − t`), không phải thời gian đã chạy — không lỗi; `plots=4` đúng với `walkbench.apk` (không phải bản 20 ô).
- User xem: thêm mục F vào `.ai/playtest-buoc-7.md`.
- Kết luận: đã duyệt
