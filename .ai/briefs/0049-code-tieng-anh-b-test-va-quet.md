# Brief 0049 — code tiếng Anh (B): test sang tiếng Anh + test quét toàn bộ

> Trạng thái: đã duyệt
> Review: đầy đủ
> Model thực thi: Sonnet · medium
> Cập nhật: 2026-10-23

<!-- Planner (Opus) viết, Executor (Sonnet) làm theo ở PHIÊN KHÁC — executor không có lịch sử chat, không được tự quyết.
Trạng thái: chờ làm → đang làm → chờ review → đã duyệt; hoặc → cần Opus (executor dừng, hỏi ở mục Vướng mắc).
Review: nhẹ (đọc mục Kết quả) | đầy đủ (đọc cả diff) | user xem (có phần user tự kiểm bằng mắt/chơi thử) — có thể ghép "đầy đủ + user xem".
Model thực thi: BẮT BUỘC, đúng dạng `<Model> · <effort>` — Model: Opus | Sonnet | Haiku | Fable; effort: low | medium | high | xhigh | max (chọn theo AGENTS.md §10.2).
Executor kiểm tra model + effort của phiên TRƯỚC khi làm (scripts/brief_check.py); lệch hoặc không xác định được → cảnh báo user và DỪNG.
File được commit. Tự kiểm trước khi giao (AGENTS.md §10.3): executor có phải hỏi gì không? còn chỗ nào bỏ ngỏ 2 phương án? -->

## Mục tiêu
Task [m1-code-tieng-anh](../tasks/m1-code-tieng-anh.md), phần B (sau brief 0048 đã duyệt):
1. Literal tiếng Việt trong `client/Assets/Game/Tests/**/*.cs` (thông báo assert, `TestContext.WriteLine`, `Debug.Log`, tên object, tên tạm) chuyển sang tiếng Anh. Riêng **chữ hiển thị kỳ vọng** (giá trị người chơi thấy, test so với output) giữ tiếng Việt và gắn dấu `// vi-text`.
2. Mở rộng `HardcodedTextScanTests` ra **mọi `.cs` trong `client/Assets/Game`**, thêm kiểm từ tiếng Việt không dấu, để chặn chữ tiếng Việt quay lại code.

## Bối cảnh cần đọc
- Task contract `.ai/tasks/m1-code-tieng-anh.md`; brief 0048 (QĐ 2–3 về tiền tố log và bảng thuật ngữ — **dùng lại nguyên bảng thuật ngữ QĐ 3 của 0048**).
- Hiện trạng [run 2026-10-22, HEAD 0c6a82c, quét dấu như lệnh bước 2]: **472 dòng / 54 file trong `Tests/`**, + 6 dòng `Core/WorldMapParams.cs:57-62` (dữ liệu, giữ). Nặng nhất `Tests/World/BlockMesherTests.cs` 43, `Tests/World/WorldMapGeneratorTests.cs` 30, `Tests/Core/SeaFloodTests.cs` 24, `Tests/Core/Sim/SimConfigTests.cs` 24, `Tests/Client/PlayerCharacterPlayTests.cs` 24, `Tests/Client/PlayerBagSowPlayTests.cs` 24.
- Test quét hiện tại: `Tests/Core/Text/HardcodedTextScanTests.cs` (assembly `Game.Core.Tests`, EditMode). Nguyên văn phần đầu:
  ```csharp
  static readonly string[] Files = { "Core/View/SiteStatus.cs", … "Client/SettingsView.cs", };
  static readonly Regex Literal = new Regex(@"""(?:[^""\\]|\\.)*""");
  static readonly Regex Vietnamese = new Regex("[À-ỹĐđ]");
  static readonly Regex KeyUse = new Regex(@"(?:\bT|\.Get|Format)\(\s*""([a-z0-9_]+\.[a-z0-9_]+)""");
  static readonly Regex KeyShape = new Regex(@"^[a-z0-9_]+\.[a-z0-9_]+$");
  static string[] Lines(string rel) => File.ReadAllLines(Path.Combine(Application.dataPath, "Game", rel));
  ```
  Các test: `NoVietnameseLiterals` (bỏ dòng bắt đầu `//`), `AllUsedKeysExist`, `BaseFileValid`, `ReachInText`.
- Chữ không dấu đã biết [run 2026-10-22]: `Tests/Client/EditOcclusionPlayTests.cs:53` thông báo `"kho"`; `Tests/Client/SeaSkyTintPlayTests.cs:14` tên pha `("binh-minh", …), ("ngay", …), ("hoang-hon", …), ("dem", …)` (chỉ dùng trong log `[0034] … {name}` và thông báo assert `:166-187`).
- Không có công cụ trong `tools/` grep nội dung log test tiếng Việt [run 2026-10-22].

## Quyết định đã chốt (không được đổi)
1. **Chữ hiển thị kỳ vọng** = literal tiếng Việt mà test **so với chữ người chơi thấy**. Giữ y hệt, thêm ở **cuối dòng** comment ` // vi-text`. Gồm đúng 3 loại:
   - (a) Giá trị kỳ vọng so với output chữ hiển thị: `farming.StatusLine`, `farming.HudLines()`, `input.HudText()`, `sv.StockMessage`, `rows[i].Label`, `dial.PeriodText`, `dial.LandmarkLabel(…)`, `map.CloseButtonText`, `TerrainEditInput.DropLabel/ItemLabel`, `GameSettings.Text.Get/Format`, `Strings.Get/Format`, `ViText.Load().Get/Format`, `SiteStatus.*`, `CreatureStatus.*`. Đã biết: `DropPlayTests`, `MinimapCornerPlayTests`, `PlayerBagSowPlayTests`, `PlayerFarmPlayTests`, `SettingsPlayTests`, `TimeDialPlayTests`, `Core/View/SiteStatusTests`, `Core/View/CreatureStatusTests`, `Core/Text/StringsTests`, `Core/Text/HardcodedTextScanTests` (`"Tiếng Việt"`, `"6 ô"`, `"8 ô"`).
   - (b) Chữ trong bảng chuỗi dựng cho test: `StringsTests` `F(…)` (`"Ä"`, `"Bê"`, `"Mưa {0}"`…), chuỗi JSON `:81`, `StringEntry(…, "<chữ>")`.
   - (c) Tên ngôn ngữ `"Tiếng Việt"`.
   - Câu lệnh nhiều dòng: gắn dấu vào **từng dòng** có literal tiếng Việt (vd `SiteStatusTests.cs:44` và dòng tiếp).
   - Mọi literal tiếng Việt khác trong test (thông báo assert, tham số "nhãn" truyền vào helper như `AssertPanel(…, "tâm world")`, `Close(…, "trời vs khối")`, `AssertSameMesh(…, $"chunk {c} sau …")`, `TestContext.WriteLine`, `Debug.Log`, `log?.Invoke`, tên GameObject, biến chuỗi tạm) → **tiếng Anh**, không gắn dấu.
2. **Dịch:** như 0048 QĐ 1–3: chỉ đổi chữ trong literal; giữ tham số, định dạng số, nội suy; tiền tố đầu chuỗi giữ y hệt (`MESHSTATS`, `MESHTIME`, `MINIMAP`, `SIMMEM`, `UNBALANCED`, `PLAYTEST`, `[0034]`, `…TEST`…); `×` → `x`; comment giữ tiếng Việt. Thuật ngữ theo bảng 0048 QĐ 3.
3. **Tên cố định:**
   - `Tests/Client/SeaSkyTintPlayTests.cs:14`: `"binh-minh"` → `"dawn"`, `"ngay"` → `"day"`, `"hoang-hon"` → `"dusk"`, `"dem"` → `"night"`.
   - `Tests/Client/EditOcclusionPlayTests.cs:53` `"kho"` → `"storage"`; `:51` `"nhà " + h.Id` → `"house " + h.Id`.
   - `Tests/Client/MinimapViewPlayTests.cs:173` `new GameObject("Đảo")` → `"Island"`; `Tests/Client/TerrainEditPlayTests.cs:110` `new GameObject("con khác")` → `"Other Child"`. Trước khi đổi, grep tên cũ trong file đó; có `Find`/so `.name` theo tên cũ thì đổi theo.
   - `Tests/World/ConfigTests.cs:20,31,42,71`: `"Client copy differs from design/data/<file>.json — copy it again from design/data"` (giữ tên file như cũ).
4. **Test quét mới** — viết lại `HardcodedTextScanTests.cs` (cùng file, namespace, assembly):
   - Danh sách file: `Directory.GetFiles(Path.Combine(Application.dataPath, "Game"), "*.cs", SearchOption.AllDirectories)`, đường dẫn tương đối so với thư mục `Game`, đổi `\` thành `/`, sắp Ordinal. Xoá mảng `Files` cũ.
   - Literal regex giữ nguyên. Regex dấu viết bằng escape để file không chứa ký tự tiếng Việt: `new Regex("[À-ỹĐđ]")` (cùng dải `[À-ỹĐđ]` cũ).
   - Từ không dấu: `new Regex(@"\b(kho|tui|nha|ruong|cay|khoi|hinh|bien|huong|khong|duoc|cua|gieo|xoi|tuoi|hoach|buoi|binh|minh|hoang|dem)\b", RegexOptions.IgnoreCase)`, áp lên từng literal. File `Tests/Core/Text/HardcodedTextScanTests.cs` **bỏ qua kiểm từ không dấu** (chứa chính danh sách từ); vẫn kiểm dấu.
   - Mỗi dòng: bỏ nếu `Trim()` bắt đầu `//`. Nếu dòng kết thúc (sau `TrimEnd()`) bằng `// vi-text`: chỉ hợp lệ khi file bắt đầu bằng `Tests/` hoặc là `Core/WorldMapParams.cs`; hợp lệ → bỏ qua dòng; không hợp lệ → lỗi `<file>:<dòng>: vi-text not allowed here`. Dòng có dấu `// vi-text` nhưng không có literal nào khớp dấu hoặc từ không dấu → lỗi `<file>:<dòng>: unused vi-text`.
   - Các dòng còn lại: literal khớp dấu → lỗi `<file>:<dòng>: <literal>`; khớp từ không dấu → lỗi `<file>:<dòng>: unaccented Vietnamese: <literal>`.
   - Gom mọi lỗi, `Assert.IsEmpty(list, string.Join("\n", list))`. Một test `NoVietnameseLiterals` gộp cả hai kiểm.
   - `AllUsedKeysExist`: quét mọi file **không** bắt đầu bằng `Tests/` (cùng danh sách trên), regex `KeyUse` giữ nguyên.
   - `BaseFileValid`, `ReachInText` giữ logic; chuỗi tiếng Việt kỳ vọng gắn `// vi-text`; thông báo `"Không sắp Ordinal: "` → `"Not sorted Ordinal: "`. Summary `///` giữ tiếng Việt, có thể sửa nội dung cho đúng phạm vi mới.
5. `Core/WorldMapParams.cs:57-62`: thêm ` // vi-text` cuối 6 dòng (tên dấu mốc tạm — dữ liệu, task contract). Ngoài 6 dòng này, không sửa file ngoài `Tests/`.
6. Không đổi giá trị kỳ vọng nào, trừ tên ở QĐ 3. Không thêm/xoá test (ngoài viết lại nội dung `NoVietnameseLiterals` / `AllUsedKeysExist`).

## Phạm vi
- Được sửa: mọi `.cs` trong `client/Assets/Game/Tests/**` có trong kết quả quét bước 2 hoặc ở QĐ 3–4; `client/Assets/Game/Core/WorldMapParams.cs` (chỉ `:57-62`, QĐ 5); mục Kết quả của brief này.
- Không được sửa: `.cs` khác ngoài `Tests/`, `vi.json`, scene, `design/`, `tools/`, comment (trừ thêm ` // vi-text` và summary của scan test).

## Các bước
1. `date` (bắt đầu). Chạy lệnh quét (bước 2 "Lệnh executor chạy"), lưu danh sách.
2. **Test quét** (QĐ 4–5): viết lại `HardcodedTextScanTests.cs`, gắn `// vi-text` ở `WorldMapParams.cs:57-62`. Chạy EditMode lớp `HardcodedTextScanTests` → `NoVietnameseLiterals` **fail** liệt kê các dòng test (đúng mong đợi lúc này; ghi số lỗi vào Kết quả); 3 test còn lại qua.
3. **Gắn `// vi-text`** cho chữ hiển thị kỳ vọng (QĐ 1) trong các file test đã biết ở QĐ 1(a)–(c).
4. **Dịch phần còn lại** theo QĐ 2–3, từng thư mục: `Tests/Core` → `Tests/World` → `Tests/Client` → `Tests/ClientEdit`. Sau mỗi thư mục chạy lại lớp `HardcodedTextScanTests` để xem số lỗi giảm.
5. `NoVietnameseLiterals` qua. Chạy toàn bộ EditMode + PlayMode.
6. Ghi Kết quả: số dòng gắn `// vi-text` theo file; bảng tên đổi (QĐ 3 + tên khác nếu có); số lỗi ở bước 2; giờ.

## Tiêu chí xong
- `HardcodedTextScanTests` 4/4 qua, quét toàn bộ `client/Assets/Game/**/*.cs`.
- Lệnh quét bước 2 chỉ còn dòng có ` // vi-text` (cột cuối), và mọi dòng đó nằm trong `Tests/` hoặc `Core/WorldMapParams.cs:57-62`.
- EditMode + PlayMode toàn bộ qua, số test = trước khi làm (498 EditMode + 167 PlayMode tại 0c6a82c), 0 lỗi console.
- `git diff` không đổi giá trị kỳ vọng ngoài QĐ 3; không đổi dòng comment (ngoài summary scan test).
- `git diff --stat` chỉ file trong Phạm vi.

## Test sau khi xong
### Phần cần test
- **Scan bắt dấu toàn repo** (bước 2): thử tạm thêm `var x = "thử";` vào một file `Client/*.cs` → `NoVietnameseLiterals` fail đúng dòng đó; xoá dòng thử. Ghi kết quả.
- **Scan bắt không dấu** (bước 2): thử tạm `"Kho "` trong một file `Core/*.cs` → fail `unaccented Vietnamese`; xoá. Ghi kết quả.
- **Dấu `vi-text` sai chỗ** (bước 2): thử tạm `var y = "thử"; // vi-text` trong `Client/*.cs` → fail `vi-text not allowed here`; xoá. Ghi kết quả.
- **Dấu `vi-text` thừa** (bước 2): thử tạm `int z = 0; // vi-text` trong một file `Tests/` → fail `unused vi-text`; xoá. Ghi kết quả.
- **Scan sạch** (bước 5): `HardcodedTextScanTests` 4/4 qua.
- **Chữ hiển thị không đổi** (bước 3): PlayMode `DropPlayTests`, `PlayerBagSowPlayTests`, `PlayerFarmPlayTests`, `SettingsPlayTests`, `TimeDialPlayTests`, `MinimapCornerPlayTests`; EditMode `SiteStatusTests`, `CreatureStatusTests`, `StringsTests` qua với kỳ vọng cũ.
- **Tên đổi** (QĐ 3): `SeaSkyTintPlayTests`, `EditOcclusionPlayTests`, `MinimapViewPlayTests`, `TerrainEditPlayTests`, `ConfigTests` qua.
- **Hồi quy chung** (bước 4): EditMode + PlayMode toàn bộ, số test như trước, 0 fail.
- **Tiền tố log test**: lệnh bước 3 trước = sau.

### Lệnh executor chạy
1. `date` (bắt đầu).
2. Quét (ở gốc repo; trước và sau khi sửa):
   ```bash
   python3 - <<'EOF'
   import re, pathlib
   lit = re.compile(r'"(?:[^"\\]|\\.)*"'); vi = re.compile('[À-ỹĐđ]')
   n = 0
   for p in sorted(pathlib.Path('client/Assets/Game').rglob('*.cs')):
       for i, l in enumerate(p.read_text(encoding='utf-8').splitlines(), 1):
           if l.strip().startswith('//'): continue
           if any(vi.search(m) for m in lit.findall(l)):
               n += 1; print(f'{p}:{i}: {"[vi-text] " if l.rstrip().endswith("// vi-text") else ""}{l.strip()[:110]}')
   print('TOTAL', n)
   EOF
   ```
3. Tiền tố log test (trước và sau, so 2 output):
   ```bash
   grep -rhoE '(Log|WriteLine|Invoke)\(\$?"[A-Z\[][A-Z0-9_\]]{2,}' client/Assets/Game/Tests --include='*.cs' | grep -oE '[A-Z\[][A-Z0-9_\]]{2,}$' | sort | uniq -c
   ```
4. Unity Editor mở (MCP): `refresh_unity` → `read_console` không lỗi biên dịch.
5. `run_tests` EditMode (lớp `Game.Core.Tests.HardcodedTextScanTests` khi lặp; toàn bộ ở cuối); `run_tests` PlayMode toàn bộ → ghi dòng tổng.
6. `git checkout -- client/ProjectSettings/EditorSettings.asset`.
   - Editor tắt: `python3 tools/unity_test.py EditMode` rồi `python3 tools/unity_test.py PlayMode`.
7. `git status --short` (không còn dòng thử); `git diff --stat`; `date` (kết thúc).

### User tự test
Không.

## Dừng và chuyển lại Opus khi
- Một literal tiếng Việt không rõ thuộc QĐ 1 (chữ hiển thị kỳ vọng) hay không.
- Từ không dấu bắt nhầm literal tiếng Anh / định danh (vd tên shader, khoá) → không tự sửa danh sách từ.
- Phải đổi giá trị kỳ vọng ngoài QĐ 3, hoặc một test fail sau khi chỉ đổi chữ.
- Brief mơ hồ hoặc mâu thuẫn với code thật; cần quyết định thuộc AGENTS.md §3.4; cùng một lỗi sửa 2 lần vẫn chưa qua; phải sửa file ngoài Phạm vi.

---
<!-- Từ đây trở xuống: EXECUTOR ghi. -->

## Kết quả
Base commit: b2dd852
Chạy bằng: claude-sonnet-5-5 · medium
Bắt đầu: Fri Oct 23 09:30 +07 2026 · Kết thúc: 09:43 (~13 phút đồng hồ)
Commit: 0f45dfd — 57 file trong `client/Assets/Game` (Tests/** + `Core/WorldMapParams.cs:57-62` + `HardcodedTextScanTests.cs` viết lại).
- Quét ban đầu: TOTAL 478 (472 dòng Tests + 6 WorldMapParams). Không chạy riêng bước "fail trước" (làm gộp bước 2–4 bằng script) → số lỗi bước 2 suy từ quét Python = 472; sau cùng còn 102 dòng, tất cả `// vi-text`.
- Dòng `// vi-text` theo file: WorldMapParams 6; DropPlayTests 4; MinimapCornerPlayTests 1; PlayerBagSowPlayTests 21; PlayerFarmPlayTests 12; SettingsPlayTests 5; TimeDialPlayTests 7; HardcodedTextScanTests 3; StringsTests 10; CreatureStatusTests 13; SiteStatusTests 20 (tổng 102).
- Tên đổi (QĐ 3): `binh-minh/ngay/hoang-hon/dem` → `dawn/day/dusk/night`; `"kho"` → `"storage"`; `"nhà "` → `"house "`; `"Đảo"` → `"Island"`; `"con khác"` → `"Other Child"`; ConfigTests 4 thông báo → "Client copy differs …". Không có `Find`/so `.name` theo tên cũ (grep).
- Cách làm: dịch bằng bảng 331 literal duy nhất (áp theo regex literal, chỉ đổi literal); đánh dấu vi-text bằng script theo API hiển thị trong file QĐ 1 rồi soát tay. Dạng regex dấu trong scan test viết escape `\u00C0-\u1EF9\u0110\u0111`.
- Lưu ý: literal ghép `"…{string.Join("` / `", landTiers)} · … {string.Join("` dịch giữ nguyên cấu trúc dấu nháy.

## Kết quả test
- Scan bắt dấu toàn repo: đạt — thêm `"thử"` vào `Client/SettingsView.cs` → fail `SettingsView.cs:11: "thử"`.
- Scan bắt không dấu: đạt — `"Kho "` trong `Core/WorldData.cs` → `unaccented Vietnamese: "Kho "`.
- vi-text sai chỗ: đạt — `Client/SettingsView.cs:12: vi-text not allowed here`.
- vi-text thừa: đạt — `Tests/Core/MinimapImageTests.cs:9: unused vi-text`. (Cả 4 thử trong một lần chạy; đã khôi phục file, `git status` sạch ngoài phạm vi.)
- Scan sạch: đạt — HardcodedTextScanTests 4/4.
- Hồi quy: EditMode 498/498 qua, PlayMode 167/167 qua (số test = trước khi làm), console 0 lỗi biên dịch. Gồm các test chữ hiển thị và tên đổi.
- Tiền tố log test: đạt — output lệnh bước 3 trước = sau (diff rỗng).
- Dọn: `git checkout -- client/ProjectSettings/EditorSettings.asset` đã chạy.
- Lệnh quét bước 2 sau cùng: mọi dòng còn lại có `// vi-text`, nằm trong Tests/ hoặc WorldMapParams.cs.

## Vướng mắc

---
<!-- PLANNER ghi khi review. -->

## Review
Review đầy đủ, Planner Opus · medium, 2026-10-23 (code 0f45dfd, kết quả 498f633):
- `Chạy bằng` claude-sonnet-5-5 · medium khớp `Model thực thi` Sonnet · medium. Mọi dòng "Phần cần test" có kết quả (4 thử âm tính, scan sạch, chữ hiển thị, tên đổi, hồi quy, tiền tố log).
- Phạm vi: `git diff --name-only b2dd852 0f45dfd` = 55 file `Tests/**` + `Core/WorldMapParams.cs` (chỉ thêm ` // vi-text` ở :57-62) [run].
- QĐ 1: lệnh quét bước 2 chạy lại → TOTAL 102, 0 dòng thiếu `// vi-text`; soát cả 102 dòng: đều là kỳ vọng so với output hiển thị (`SiteStatus`/`CreatureStatus`/`farming.*`/`sv.StockMessage`/`dial.*`/`map.CloseButtonText`/`GameSettings.Text`/`ViText`), bảng chuỗi test (`StringsTests`), tên ngôn ngữ, hoặc dữ liệu `WorldMapParams` [run].
- QĐ 2/6: so từng dòng cũ ↔ mới sau khi thay literal bằng `""` và bỏ ` // vi-text` → 0 khác biệt ngoài literal (trừ file scan test viết lại); 99 dòng gắn dấu ngoài scan test giữ nguyên chữ cũ; không dòng comment nào đổi ngoài summary scan test [run]. Mẫu `BlockMesherTests` giữ tiền tố `MESHSTATS`, nội suy, định dạng `:F2`.
- QĐ 3: dòng bị xoá không chứa dấu chỉ là `"kho"`, tên pha `binh-minh/ngay/hoang-hon/dem` và mảng `Files` cũ — khớp bảng tên cố định [run].
- QĐ 4: `HardcodedTextScanTests.cs` đọc: `Directory.GetFiles(…, AllDirectories)` + `GetRelativePath` + `\`→`/` + sort Ordinal; regex dấu viết escape `[À-ỹĐđ]` (cùng dải); danh sách từ không dấu đúng QĐ, `IgnoreCase`, file scan bỏ kiểm không dấu; `vi-text not allowed here` / `unused vi-text` đúng điều kiện; `AllUsedKeysExist` bỏ `Tests/`; `"Not sorted Ordinal: "` [repo `client/Assets/Game/Tests/Core/Text/HardcodedTextScanTests.cs:16-105`].
- Số test 498/167 = trước khi làm (roadmap 0048) → không đáng ngờ, không chạy lại.
- Lệch nhỏ (không ảnh hưởng kết quả): executor không chạy riêng bước 2 "fail trước" mà làm gộp bước 2–4; 4 thử âm tính đã chứng minh test quét bắt đúng lỗi.
- Giờ: executor 13 phút (09:30–09:43) — đã ghi roadmap; task `m1-code-tieng-anh` xong (0048 + 0049: 23 phút so với ~1 ngày-người), cập nhật task contract.
- Kết luận: đã duyệt
