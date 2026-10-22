# Brief 0048 — code tiếng Anh (A): code chạy Core / World / Client / Editor

> Trạng thái: chờ review
> Review: đầy đủ
> Model thực thi: Sonnet · medium
> Cập nhật: 2026-10-22 23:06

<!-- Planner (Opus) viết, Executor (Sonnet) làm theo ở PHIÊN KHÁC — executor không có lịch sử chat, không được tự quyết.
Trạng thái: chờ làm → đang làm → chờ review → đã duyệt; hoặc → cần Opus (executor dừng, hỏi ở mục Vướng mắc).
Review: nhẹ (đọc mục Kết quả) | đầy đủ (đọc cả diff) | user xem (có phần user tự kiểm bằng mắt/chơi thử) — có thể ghép "đầy đủ + user xem".
Model thực thi: BẮT BUỘC, đúng dạng `<Model> · <effort>` — Model: Opus | Sonnet | Haiku | Fable; effort: low | medium | high | xhigh | max (chọn theo AGENTS.md §10.2).
Executor kiểm tra model + effort của phiên TRƯỚC khi làm (scripts/brief_check.py); lệch hoặc không xác định được → cảnh báo user và DỪNG.
File được commit. Tự kiểm trước khi giao (AGENTS.md §10.3): executor có phải hỏi gì không? còn chỗ nào bỏ ngỏ 2 phương án? -->

## Mục tiêu
Task [m1-code-tieng-anh](../tasks/m1-code-tieng-anh.md), phần A: mọi literal tiếng Việt trong **code chạy** (`client/Assets/Game/{Core,World,Client}/**/*.cs`, gồm `Client/Editor`) chuyển sang tiếng Anh — exception, `Debug.Log*`, menu Editor, tên GameObject, chữ bench, `ToString()`. Sửa theo 2 file test đang tìm object theo tên cũ. Thông báo/log trong test và mở rộng test quét để brief B.

## Bối cảnh cần đọc
- Task contract: `.ai/tasks/m1-code-tieng-anh.md` (QĐ 1–3).
- Hiện trạng [run 2026-10-22, HEAD e160632]: lệnh quét ở "Lệnh executor chạy" bước 2 ra **198 dòng / 35 file** (ngoài `Tests/`). Nặng nhất `Core/Sim/SimConfig.cs` 45, `Core/Sim/Simulation.cs` 17, `Core/WorldMapParams.cs` 16, `Client/Editor/TerrainViewCapture.cs` 13, `Client/TerrainChunkView.cs` 10. Executor chạy lại lệnh để lấy danh sách đầy đủ.
- Tên object đang được tìm theo tên:
  - `Client/TerrainChunkView.cs:50` `const string LandmarksName = "Dấu mốc";` — dùng ở `:128` `transform.Find(LandmarksName)`, `:388` `new GameObject(LandmarksName)`.
  - `Tests/Client/LandmarkViewPlayTests.cs:45` `go.transform.Find("Dấu mốc")`, `:91` `if (c.name == "Dấu mốc")`, `:109` `g.transform.Find("Dấu mốc")`.
  - `Client/SimView.cs:205` `Box("Kho " + s.Id, …)` (tiếng Việt không dấu, quét dấu không bắt) — `Tests/Client/PlayerBagSowPlayTests.cs:431` `if (c.name == "Kho 0") storage = c;`. Dòng `:199` cùng file đã dùng `Box("House " + h.Id, …)`.
- Không test nào kiểm nội dung exception hay log tiếng Việt (`LogAssert`, `.Message`) [run grep `Tests/` 2026-10-22] → đổi chữ exception/log không làm hỏng test.
- `tools/` không grep nội dung log tiếng Việt; menu Editor không được gọi trong `tools/` [run grep 2026-10-22].

## Quyết định đã chốt (không được đổi)
1. **Chỉ đổi chữ trong literal**, không đổi logic, kiểu exception, tham số (`nameof(...)`, giá trị), thứ tự, định dạng số (`:F1`, `:F0`…), dấu `{…}` nội suy. Comment `//`, `///` giữ tiếng Việt.
2. **Tiền tố log đầu dòng giữ nguyên y hệt** (chữ hoa, đứng đầu literal): `CHECKPOINT`, `DEMOPLAIN`, `LANDMARKVIEW`, `LANG`, `MINIMAP`, `PLAYER`, `SETTINGS`, `STARTERBAG`, `TERRAINVIEW`, `WALKBENCH` và mọi tiền tố chữ hoa khác đang có. Các cặp `key=value` tiếng Anh sẵn có giữ nguyên.
3. **Dịch:** câu tiếng Anh ngắn, sát nghĩa, viết hoa chữ đầu như câu gốc; giữ dấu câu cuối (`.` có thì giữ, không có thì không thêm). Ký tự `×` → `x` (vd `$"{X}x{Y}x{Z}"` ở `Core/WorldData.cs:51`, `:15`; `Size} x {Size}` ở `Core/Sim/SimStress.cs:13`). `≥`, `≤`, `→`, `–`, `−`, `·`, `…` được giữ (không phải tiếng Việt). Thuật ngữ cố định:

   | Việt | Anh |
   |---|---|
   | ô | cell |
   | cột | column |
   | sinh vật | creature |
   | loài | species |
   | năng lực | ability |
   | kho | storage |
   | nhà | house |
   | túi hạt | seed bag |
   | loại cây / cây | crop |
   | dấu mốc | landmark |
   | mô phỏng / vùng mô phỏng | simulation / simulation region |
   | đồng bằng | plain |
   | nhịp | tick |
   | buổi / mốc (thời gian) | period / landmark time |
   | mực nước | water level |
   | đỉnh / tam giác | vertices / triangles |
   | dựng mesh / ghi / nạp dần | mesh build / write / streaming |

4. **Tên object (cố định):**

   | File:dòng | Cũ | Mới |
   |---|---|---|
   | `Client/TerrainChunkView.cs:50` | `"Dấu mốc"` | `"Landmarks"` |
   | `Client/TerrainChunkView.cs:410` | `"Khối"` | `"Block"` |
   | `Client/PlayerCharacter.cs:106` | `"Hình"` | `"Visual"` |
   | `Client/PlayerCharacter.cs:108` | `"Thân"` | `"Body"` |
   | `Client/PlayerCharacter.cs:109` | `"Đầu"` | `"Head"` |
   | `Client/PlayerCharacter.cs:110` | `"Mũi"` | `"Nose"` |
   | `Client/SimView.cs:205` | `"Kho " + s.Id` | `"Storage " + s.Id` |

   Test đổi theo: `LandmarkViewPlayTests.cs:45,91,109` → `"Landmarks"`; `PlayerBagSowPlayTests.cs:431` → `"Storage 0"`. Đây là đổi kỳ vọng được phép (chỉ tên object).
5. **Menu Editor (cố định)** — chỉ đổi chuỗi trong `[MenuItem("…")]`, không đổi tên hàm:

   | File:dòng | Mới |
   |---|---|
   | `Client/Editor/BenchBuild.cs:23` | `Game/Create Scene Bench` |
   | `Client/Editor/HashCheckBuild.cs:22` | `Game/Create Scene HashCheck` |
   | `Client/Editor/SimBenchBuild.cs:22` | `Game/Create Scene SimBench` |
   | `Client/Editor/WalkBenchBuild.cs:22` | `Game/Create Scene WalkBench` |
   | `Client/Editor/TerrainViewCapture.cs:21` | `Game/Create Scene TerrainView` |
   | `Client/Editor/TerrainViewCapture.cs:40` | `Game/Add Dig-Place to TerrainView` |
   | `Client/Editor/TerrainViewCapture.cs:56` | `Game/Convert TerrainView to World v3` |
   | `Client/Editor/TerrainViewCapture.cs:89` | `Game/Clear Water + Camera Collision for TerrainView` |
   | `Client/Editor/TerrainViewCapture.cs:134` | `Game/World Border for TerrainView` |
   | `Client/Editor/TerrainViewCapture.cs:153` | `Game/Minimap for TerrainView` |
   | `Client/Editor/TerrainViewCapture.cs:166` | `Game/Capture TerrainView` |
   | `Client/Editor/WorldMapPreview.cs:18` | `Game/Multi-Map World Preview` |

   Menu `Game/Build APK …` đã là tiếng Anh, giữ nguyên.
6. **Chữ bench** (`Client/TerrainBench.cs:44,99,126`, `Client/WalkBench.cs:43,157,159`): chỉ người làm game đọc → tiếng Anh viết thẳng trong code, **không** thêm khoá `vi.json`. Ví dụ `"Preparing…"`, `$"Warming up… {…:F0} s"`, `$"Measuring… {…} s · {frameMs.Count} frames · {edits} edits"`; `WALKBENCH` giữ đầu chuỗi.
7. **Giữ nguyên (dữ liệu):** `Core/WorldMapParams.cs:57-62` tên dấu mốc tạm `"Công trình 1A"…"Công trình 3B"` (task contract, Ngoài phạm vi). Đây là 6 dòng duy nhất được còn trong kết quả quét sau brief.
8. Không sửa `vi.json`, không đổi `HardcodedTextScanTests` (brief B).

## Phạm vi
- Được sửa: 35 file `.cs` trong kết quả quét bước 2 (`Core/**`, `World/**`, `Client/**` gồm `Client/Editor/**`), `Client/SimView.cs` (chỉ `:205`), `Tests/Client/LandmarkViewPlayTests.cs` (chỉ `:45,91,109`), `Tests/Client/PlayerBagSowPlayTests.cs` (chỉ `:431`); mục Kết quả của brief này.
- Không được sửa: file khác trong `Tests/`, `vi.json`, scene, `design/`, `tools/`, comment.

## Các bước
1. `date` (bắt đầu). Chạy lệnh quét (Lệnh executor chạy, bước 2), lưu danh sách vào scratchpad.
2. **Core + World** (`Core/**`, `World/ChunkMeshWriter.cs`): dịch exception và `ToString()` theo QĐ 1–3. Xong khi lệnh quét không còn dòng nào trong `Core/`, `World/` trừ `WorldMapParams.cs:57-62`.
3. **Client runtime** (`ConfigLoader`, `DemoPlain`, `GameSettings`, `PlayerCharacter`, `TerrainChunkView`, `TerrainBench`, `WalkBench`): log/exception theo QĐ 1–3, tên object theo QĐ 4, chữ bench theo QĐ 6. Thêm `SimView.cs:205` (QĐ 4). Xong khi lệnh quét không còn dòng trong `Client/` (ngoài `Editor/`).
4. **Editor** (`Client/Editor/*.cs`): menu theo QĐ 5, exception/log theo QĐ 1–3.
5. **Test tìm theo tên:** `LandmarkViewPlayTests.cs:45,91,109`, `PlayerBagSowPlayTests.cs:431` (QĐ 4).
6. Kiểm, chạy test (Lệnh executor chạy 3–7). Ghi Kết quả: bảng menu + object (xác nhận đúng QĐ 4–5), số dòng quét còn lại, tiền tố log trước/sau.

## Tiêu chí xong
- Lệnh quét (bước 2) chỉ còn đúng 6 dòng `Core/WorldMapParams.cs:57-62`.
- `grep -rn '"Kho ' client/Assets/Game/Client` → 0 dòng.
- Danh sách tiền tố log (bước 3) trước = sau.
- EditMode + PlayMode toàn bộ qua, 0 lỗi biên dịch / console.
- `git diff --stat` chỉ có file trong Phạm vi; `git diff` không có dòng comment nào đổi.

## Test sau khi xong
### Phần cần test
- **Exception Core/World** (bước 2): EditMode toàn bộ — các test `Throws<…>` (vd `SimConfigTests`, `PlayerBagTests`, `DropFieldTests`) vẫn qua vì chỉ kiểm kiểu exception. Mong đợi: 0 fail.
- **Tên object** (bước 3, 5): PlayMode `LandmarkViewPlayTests` (tìm `"Landmarks"`), `PlayerBagSowPlayTests.ClickStorageBlock` (tìm `"Storage 0"`), `TerrainEditPlayTests`, `PlayerCharacterPlayTests` (nhân vật dựng hình). Mong đợi: qua.
- **Log tiền tố** (bước 3): lệnh bước 3 — danh sách tiền tố trước = sau.
- **Bench / Editor biên dịch** (bước 3–4): `refresh_unity` + `read_console` 0 lỗi; `grep -n MenuItem client/Assets/Game/Client/Editor/*.cs` khớp bảng QĐ 5.
- **Hồi quy chung:** EditMode + PlayMode toàn bộ, số test = số trước khi làm, 0 fail.

### Lệnh executor chạy
1. `date` (bắt đầu).
2. Quét (chạy ở gốc repo; trước và sau khi sửa):
   ```bash
   python3 - <<'EOF'
   import re, pathlib
   lit = re.compile(r'"(?:[^"\\]|\\.)*"'); vi = re.compile('[À-ỹĐđ]')
   n = 0
   for p in sorted(pathlib.Path('client/Assets/Game').rglob('*.cs')):
       if '/Tests/' in str(p): continue
       for i, l in enumerate(p.read_text(encoding='utf-8').splitlines(), 1):
           if l.strip().startswith('//'): continue
           if any(vi.search(m) for m in lit.findall(l)): n += 1; print(f'{p}:{i}: {l.strip()[:120]}')
   print('TOTAL', n)
   EOF
   ```
3. Tiền tố log (trước và sau, so 2 output):
   ```bash
   grep -rhoE '(Log|LogWarning|LogError)\(\$?"[A-Z][A-Z_]{2,}' client/Assets/Game --include='*.cs' | grep -oE '[A-Z][A-Z_]{2,}$' | sort | uniq -c
   ```
4. Unity Editor mở (MCP): `refresh_unity` → `read_console` không lỗi biên dịch.
5. `run_tests` EditMode toàn bộ; `run_tests` PlayMode toàn bộ → ghi dòng tổng.
6. `git checkout -- client/ProjectSettings/EditorSettings.asset`.
   - Editor tắt: `python3 tools/unity_test.py EditMode` rồi `python3 tools/unity_test.py PlayMode`.
7. `git diff --stat`; `date` (kết thúc).

### User tự test
Không.

## Dừng và chuyển lại Opus khi
- Một test (ngoài 4 dòng ở QĐ 4) phải đổi kỳ vọng.
- Câu không dịch được mà giữ nguyên tham số / định dạng.
- Gặp literal tiếng Việt là chữ hiển thị cho người chơi (vẽ bằng `GUI`, `Text`, `TMP`) ngoài chữ bench ở QĐ 6.
- Lệnh quét ra file ngoài danh sách 35 file, hoặc còn dòng ngoài `WorldMapParams.cs:57-62`.
- Brief mơ hồ hoặc mâu thuẫn với code thật; cần quyết định thuộc AGENTS.md §3.4; cùng một lỗi sửa 2 lần vẫn chưa qua; phải sửa file ngoài Phạm vi.

---
<!-- Từ đây trở xuống: EXECUTOR ghi. -->

## Kết quả
Base commit: 6bee14a
Chạy bằng: Sonnet · medium
Bắt đầu: 2026-10-22 22:56 · Kết thúc: 2026-10-22 23:06 (~10 phút)
Commit: 03fdb38 — 38 file: 35 file quét (Core/World/Client/Editor) + `Client/SimView.cs:205` + `LandmarkViewPlayTests.cs` (:45,91,109) + `PlayerBagSowPlayTests.cs:431`. Comment không đổi (đã khôi phục 3 comment bị replace nhầm; `git diff` 0 dòng comment).
- Quét: 198 dòng/35 file → còn đúng 6 dòng `Core/WorldMapParams.cs:57-62`.
- Tiền tố log: 24 tiền tố trước = sau (diff rỗng).
- Tên object: Landmarks, Block, Visual, Body, Head, Nose, `"Storage " + s.Id` đúng QĐ 4. Menu: 12 `[MenuItem]` khớp QĐ 5 (+ 4 menu Build APK giữ nguyên).
- Lưu ý: tiêu chí `grep '"Kho ' client/Assets/Game/Client` cần `--include=*.cs` — không có `.cs` nào còn; 6 dòng còn lại là chữ hiển thị trong `Resources/Lang/vi.json` (đúng chỗ).

## Kết quả test
- Exception Core/World: EditMode 498/498 qua, 0 fail — đạt.
- Tên object: PlayMode 167/167 qua (gồm LandmarkViewPlayTests, PlayerBagSowPlayTests, TerrainEditPlayTests, PlayerCharacterPlayTests) — đạt.
- Log tiền tố: diff rỗng — đạt.
- Biên dịch Editor/bench: `refresh_unity` (force compile) + `read_console` 0 lỗi — đạt.
- Lệnh: `git checkout -- EditorSettings.asset` đã chạy; `git diff --stat` chỉ file trong Phạm vi. `tools/unity_test.py` không chạy (Editor đang mở).
- Ghi chú: giữa PlayMode MCP mất kết nối vài chục giây (Editor vẫn sống), job vẫn hoàn tất, kết quả lấy lại được.

## Kiểm chứng
- EditMode (MCP `run_tests`, tương đương `unity_test.py EditMode`): 498/498 qua.
- PlayMode (MCP): 167/167 qua. `unity_test.py` không chạy vì Editor đang mở project.
- Console 0 lỗi biên dịch. File thay đổi do test (`InitTestScene*.unity`, `EditorSettings.asset`): đã dọn, `git status` sạch.
- Checklist: Core không tham chiếu UnityEngine — đạt (chỉ đổi chữ literal trong Core). Chỉ làm hạng mục M1 (0007)/phạm vi brief — đạt. Giờ thực tế: ~10 phút, ghi ở Kết quả. Các mục thiết kế/pillar/glossary/nguồn/ước lượng: không áp dụng (không đổi design/).

## Vướng mắc

---
<!-- PLANNER ghi khi review. -->

## Review
