# Brief 0046 — 0032 (C): chuyển chữ hiển thị sang khoá + test quét

> Trạng thái: chờ review
> Review: đầy đủ + user xem
> Model thực thi: Sonnet · medium
> Cập nhật: 2026-10-22 21:35

<!-- Planner (Opus) viết, Executor (Sonnet) làm theo ở PHIÊN KHÁC — executor không có lịch sử chat, không được tự quyết.
Trạng thái: chờ làm → đang làm → chờ review → đã duyệt; hoặc → cần Opus (executor dừng, hỏi ở mục Vướng mắc).
Review: nhẹ (đọc mục Kết quả) | đầy đủ (đọc cả diff) | user xem (có phần user tự kiểm bằng mắt/chơi thử) — có thể ghép "đầy đủ + user xem".
Model thực thi: BẮT BUỘC, đúng dạng `<Model> · <effort>` — Model: Opus | Sonnet | Haiku | Fable; effort: low | medium | high | xhigh | max (chọn theo AGENTS.md §10.2).
Executor kiểm tra model + effort của phiên TRƯỚC khi làm (scripts/brief_check.py); lệch hoặc không xác định được → cảnh báo user và DỪNG.
File được commit. Tự kiểm trước khi giao (AGENTS.md §10.3): executor có phải hỏi gì không? còn chỗ nào bỏ ngỏ 2 phương án? -->

## Mục tiêu
Phần C của [task 0032](../tasks/m1-cai-dat-va-bang-chuoi.md): mọi chữ hiển thị trong các file thuộc phạm vi đọc từ bảng chuỗi (`GameSettings.Text`, brief 0045), chữ gốc nằm trong `Client/Resources/Lang/vi.json`. Khi chơi bằng tiếng Việt, mọi chữ **giữ y hệt** như bây giờ. Có test quét mã nguồn để không còn chữ viết thẳng. Chữ "8 ô" lấy từ số trong cấu hình.

Ước lượng ~2 ngày-người (nằm trong 4–7 ngày của 0032).

## Bối cảnh cần đọc
Đường dẫn tính từ `client/Assets/Game/`. Số dòng theo commit `4a61bdb`.
- Lõi: `Core/Text/Strings.cs` (`Get(key)`, `Format(key, params object[])` dùng InvariantCulture, khoá thiếu → fallback → `[khoá]`). `Client/GameSettings.cs` (`Text`, `BaseText`, `event Changed`). `Client/SettingsView.cs` (mẫu: `static string T(string k) => GameSettings.Text.Get(k);`, `Format(...)`). File gốc: `Client/Resources/Lang/vi.json` (13 khoá `settings.*`, `entries` sắp Ordinal).
- `Core/PlayerRules.cs:14` `ToolReach`, `:17` `HandReach` (float, mặc định 8, nạp từ `sim_m1.json`).
- Chữ cần chuyển (đã kiểm kê bằng grep, có thể sót. Executor tự grep lại các file ở QĐ 3):
  - `Core/View/SiteStatus.cs`: `:10` `StorageText`, `:13` `CropName`, `:19-21` `StockPanelText`, `:48-57` `ProgressText`, `:72-81` `PlotText`.
  - `Core/View/CreatureStatus.cs`: `:10-14` `AbilityName`, `:20-23` `ReasonText`, `:30-38` `Text`.
  - `Client/SimView.cs`: `:361-371` dòng túi; `:422-442` `TransferStock` (`kind`, `StockMessage`); `:474` nút Kho; `:486`, `:488` `names`, `:497`; `:499` nút Túi; `:505-516` bảng túi; `:528`, `:543-544`, `:548` `"Xới"`, `:554` `"Tiếp tục"`, `:581` `"Tự xới"`, `:583` `"Đóng (Esc)"`; `:665` dòng FPS; `:685` lý do đình trệ (dùng `CreatureStatus.ReasonText`). Nút `"x"` (`:556`, `:571`) giữ nguyên, vì là ký hiệu chứ không phải chữ.
  - `Client/TerrainEditInput.cs`: `:32` `PaletteNames`; `:135`, `:186`, `:250-253`, `:264`, `:268`, `:304`, `:316` (`lastResult`); `:324`, `:330` `ItemLabel`/`DropLabel`; `:340-346` `Explain`; `:350-358` `HelpText`; `:364-382` `HudText`.
  - `Client/PlayerFarmActions.cs`: `StatusLine` `:72-274`; `HudLines` `:281-298`.
  - `Client/MinimapView.cs`: `:424` `hint.text`, `:477` `closeText.text`. Tên map / tên dấu mốc (`view.Maps.Name`, `l.Name`) và `"N"` (`:563`) giữ nguyên, vì là dữ liệu / ký hiệu.
  - `Client/TimeDial.cs`: `:66-80` `PeriodName`/`LandmarkName`; `:234` `"Đang ở buổi: "`; `:297` `hint.text`. `:290` `label.text = LandmarkName(m)`.
- Không phải chữ hiển thị: tên GameObject / mesh (`Box("Nhà "…)`, `new GameObject(…)`, `Stretch(...)`, `NewText("…")`, `AddBlock(...)`, `.name = …`, `new Mesh { name = … }`), `Debug.Log*`, thông báo exception, comment. Không đưa vào `vi.json`; trong 8 file ở QĐ 3 phải đổi sang tiếng Anh (QĐ 9), trừ comment.
- Test cũ so khớp chữ: `Tests/Core/View/SiteStatusTests.cs`, `Tests/Core/View/CreatureStatusTests.cs` (gọi hàm Core, xem QĐ 1); PlayMode `DropPlayTests.cs:103,106,196`, `PlayerBagSowPlayTests.cs:204,392`, `MinimapCornerPlayTests.cs:171`, `TimeDialPlayTests.cs:63,97`, `SettingsPlayTests.cs:136` (`Count == 13`). Các test này phải qua với kỳ vọng giữ nguyên, trừ `SettingsPlayTests.cs:136` (QĐ 6).
- Test EditMode đọc file trong repo: mẫu `Tests/Core/Sim/SimConfigTests.cs:12` (`Path.Combine(Application.dataPath, …)`, `JsonUtility.FromJson`).
- API: chỉ dùng API đã có trong repo (`JsonUtility.FromJson`, `System.IO.File`, `System.Text.RegularExpressions.Regex` của .NET). Không cần API Unity mới.

## Quyết định đã chốt (không được đổi)
1. **Core nhận bảng chuỗi qua tham số.** Mọi hàm sinh chữ trong `SiteStatus` / `CreatureStatus` thêm tham số **đầu tiên** `Strings t` (`using Game.Core.Text;`): `StorageText(t, s)`, `CropName(t, cfg, id)`, `StockPanelText(t, s, cfg)`, `ProgressText(t, p, s, cfg)`, `PlotText(t, p, tick, growTicks)`, `AbilityName(t, a)`, `ReasonText(t, r)`, `Text(t, s, c)`. Các hàm trả số / bool (`SlotCountText`, `ProgressPercent`, `GrowPercent`, `IsCarrying`) giữ nguyên. Không dùng biến static trong Core. Client gọi với `GameSettings.Text`.
   - Test Core: thêm `Tests/Core/View/ViText.cs`, gồm `static class ViText { public static Strings Load() }`. Hàm này đọc `Path.Combine(Application.dataPath, "Game", "Client", "Resources", "Lang", "vi.json")` bằng `JsonUtility.FromJson<StringFile>` rồi trả `new Strings(file)`, cache trong trường static. `SiteStatusTests` / `CreatureStatusTests` chỉ thêm đối số `ViText.Load()` vào lời gọi, **không đổi chuỗi kỳ vọng**.
2. **Khoá.**
   - Dạng `<nhóm>.<tên>`, chỉ chữ thường a–z, số, `_`, đúng một dấu `.` đầu. Nhóm theo nơi dùng: `site.` (SiteStatus), `creature.` (CreatureStatus), `crop.name` = `Cây {0}` (dùng chung), `item.` (tên khối, túi hạt), `stock.` (bảng / nút kho), `bag.` (bảng / nút túi), `plot.` (bảng ô ruộng), `hud.` (HUD và chú thích TerrainEditInput, dòng FPS của SimView), `edit.` (`lastResult`, `Explain`), `farm.` (PlayerFarmActions), `map.` (MinimapView), `time.` (TimeDial). Phần `<tên>` là tiếng Anh snake_case ngắn, do executor đặt.
   - Chữ trong file = chữ hiện tại **y hệt**, kể cả dấu `·`, `→`, `×`, `…`, `—`, khoảng trắng đầu/cuối và `\n`. Phần ghép động thay bằng `{0}`, `{1}`… theo thứ tự. Định dạng số giữ như cũ (`{1:F1}`).
   - Thẻ rich text (`<b>`, `<color=…>`) bọc giá trị động thì để trong chữ của khoá. Ví dụ `hud.tool` = `Công cụ: <b><color=yellow>{0}</color></b> (Tab đổi)`.
   - Tách / gộp câu tuỳ executor, miễn chữ hiển thị bằng tiếng Việt không đổi.
   - Không thêm khoá không dùng. Không đổi 13 khoá `settings.*`. `vi.json` giữ `entries` sắp Ordinal, không trùng khoá.
3. **File thuộc phạm vi quét / chuyển:** `Core/View/SiteStatus.cs`, `Core/View/CreatureStatus.cs`, `Client/SimView.cs`, `Client/TerrainEditInput.cs`, `Client/PlayerFarmActions.cs`, `Client/MinimapView.cs`, `Client/TimeDial.cs`, `Client/SettingsView.cs`. File khác ngoài phạm vi (bench, Editor, HashCheck, log).
4. **"8 ô" lấy từ cấu hình:**
   - Khoá chứa tầm với dùng `{n:0.#}` và đối số `PlayerRules.ToolReach` hoặc `PlayerRules.HandReach`.
   - **ToolReach:** gieo / mưa ngoài tầm (`PlayerFarmActions.cs:142,230`), "dùng công cụ (trong 8 ô…)" và "R: phép Mưa (trong 8 ô)" trong `HelpText`.
   - **HandReach:** kho (`SimView.cs:425,497`, `TerrainEditInput.cs:135`), "F: thu vật rơi (trong 8 ô)" trong `HelpText`, `TerrainEditInput.cs:304`.
   - Với giá trị mặc định 8, chữ ra vẫn là `8 ô`.
5. **Đổi ngôn ngữ lúc chạy:**
   - IMGUI tự cập nhật vì mỗi khung đều đọc lại `GameSettings.Text`.
   - Mảng static chứa chữ (`TerrainEditInput.PaletteNames`, `SimView.cs:488` `names`) đổi thành đọc khoá lúc dùng. `Palette` (mảng vật liệu) giữ nguyên.
   - `MinimapView` và `TimeDial` (uGUI) mỗi bên thêm `void RefreshText()`. Hàm này gán lại `hint.text`, `closeText.text` (Minimap); `hint.text`, nhãn mốc `itemLabels[i].text = LandmarkName(m)`, rồi `shownPeriod = -1` để `periodText` vẽ lại (TimeDial). Gọi hàm ở cuối `Build()`, đăng ký `GameSettings.Changed += RefreshText` ngay sau đó, và gỡ (`-=`) trong `OnDestroy` hiện có (`MinimapView.cs:123`, `TimeDial.cs:102`).
   - `PeriodName` / `LandmarkName` vẫn là `public static string`, đọc `GameSettings.Text`.
6. **Test file gốc cũ:** `SettingsPlayTests.cs:136` `Assert.AreEqual(13, GameSettings.BaseText.Count)` đổi thành `Assert.GreaterOrEqual(GameSettings.BaseText.Count, 13)`. Đây là dòng duy nhất được sửa trong test cũ, ngoài các lời gọi ở QĐ 1.
7. **Test quét:** `Tests/Core/Text/HardcodedTextScanTests.cs` (EditMode, `Game.Core.Tests`, namespace `Game.Core.Tests`). Đọc từng file ở QĐ 3 bằng `File.ReadAllLines(Path.Combine(Application.dataPath, "Game", <đường dẫn>))`.
   - **`NoVietnameseLiterals`:** chỉ bỏ các dòng (sau `Trim()`) bắt đầu bằng `//` (gồm `///`). **Không** có danh sách bỏ dòng nào khác (sửa 2026-10-22 theo QĐ 9: bỏ `SkipLines` cũ `Box(`, `Stretch(`, … ). Với mọi dòng, regex `"(?:[^"\\]|\\.)*"` tìm literal. Literal nào chứa ký tự trong `[À-ỹĐđ]` (dải Latin-1 Supplement → Latin Extended Additional, có dấu tiếng Việt) thì báo lỗi `<file>:<dòng>: <literal>`. Gom hết rồi `Assert.IsEmpty(list, string.Join("\n", list))`. Lưu ý: `×` (U+00D7) cũng nằm trong dải này, nên chữ có `×` phải chuyển vào khoá.
   - **`AllUsedKeysExist`:** regex `(?:\bT|\.Get|Format)\(\s*"([a-z0-9_]+\.[a-z0-9_]+)"` trên toàn bộ nội dung từng file. Mọi khoá bắt được phải có trong `ViText.Load()` (`Has`).
   - **`BaseFileValid`:** đọc `vi.json` → `new Strings(file)` không ném lỗi. `entries` sắp Ordinal. Mọi khoá khớp `^[a-z0-9_]+\.[a-z0-9_]+$`. `language == "Tiếng Việt"`.
   - Nếu `NoVietnameseLiterals` bắt nhầm một dòng không phải chữ hiển thị mà quy tắc trên không loại được → dừng, chuyển `cần Opus`. Không tự nới quy tắc.
8. Không đổi hành vi, bố cục hay toạ độ UI. Không đổi chữ tiếng Việt. Tên map, tên dấu mốc, id cây (`A`, `B`), id loài (`L1`) là dữ liệu, giữ nguyên.
9. **Mọi phần thuộc code dùng tiếng Anh** (user, 2026-10-22). Chỉ chữ hiển thị cho người chơi được là tiếng Việt, và chỉ nằm trong `vi.json`. Trong 8 file ở QĐ 3:
   - Tên GameObject / mesh / `.name` (`new GameObject`, `Box`, `Stretch`, `NewText`, `AddBlock`, `new Mesh { name }`) → tiếng Anh, PascalCase có khoảng trắng như `"Chunks"` đã có, ví dụ `"Sign " + p.Id`, `"Creature " + c.Id + " " + c.SpeciesId`, `"Facing"`, `"Selection"`, `"Ghost"`, `"Rain"`, `"Map"`, `"Time Dial"`. Executor tự đặt, ghi bảng cũ → mới vào Kết quả.
   - `TimeDial.cs:293`, `:300`: tên object dùng `m.ToString()` (tên enum), **không** dùng `LandmarkName(m)` (tên object không được đổi theo ngôn ngữ).
   - `Debug.Log*`, thông báo exception → tiếng Anh. Giữ nguyên tiền tố đầu dòng (`SIMBENCH`, `SIMVIEW`, `PLAYER`, `MINIMAP`, `TIMEDIAL`) và các số / định dạng, vì log được đọc bằng grep/logcat.
   - Comment giữ tiếng Việt (là tài liệu, không phải code chạy).
   - Test tìm object theo tên cũ đổi theo tên mới: `Tests/Client/TerrainEditPlayTests.cs:230` (`"Bản mờ"`), `:259` (`"Ô đang chọn"`, kèm thông báo assert sang tiếng Anh). Đây là thay đổi kỳ vọng được phép, ngoài QĐ 6.
   - File ngoài QĐ 3 (vd `LandmarkView.cs` `"Dấu mốc"`, log/thông báo test khác) **không** sửa trong brief này — Planner tạo task riêng.

## Phạm vi
- Sửa: 8 file ở QĐ 3; `Client/Resources/Lang/vi.json`; `Tests/Client/TerrainEditPlayTests.cs` (chỉ `:230`, `:259`, QĐ 9); `Tests/Core/View/SiteStatusTests.cs`, `CreatureStatusTests.cs` (chỉ thêm đối số); `Tests/Client/SettingsPlayTests.cs` (chỉ `:136`); mục Kết quả của brief này.
- Tạo: `Tests/Core/View/ViText.cs`, `Tests/Core/Text/HardcodedTextScanTests.cs`, thư mục `Tests/Core/Text` (+ `.meta`).
- Không được sửa: `Core/**` ngoài 2 file View, scene, `design/`, file khác, kỳ vọng của test khác.

## Các bước
1. QĐ 1: Core `SiteStatus` / `CreatureStatus` + `ViText` + sửa 2 test Core. Thêm khoá `site.*`, `creature.*`, `crop.name` vào `vi.json`.
2. QĐ 2, 4: SimView, TerrainEditInput, PlayerFarmActions, rồi thêm khoá.
3. QĐ 5: MinimapView, TimeDial (`RefreshText`).
4. QĐ 6–7: test quét + sửa `SettingsPlayTests.cs:136`. Chạy `NoVietnameseLiterals` và sửa tới khi sạch.
5. (Sửa 2026-10-22, sau Vướng mắc QĐ 7) QĐ 9: đổi tên GameObject / mesh, `Debug.Log*`, exception trong 8 file sang tiếng Anh; sửa `TerrainEditPlayTests.cs:230,259`; xoá danh sách bỏ dòng trong `HardcodedTextScanTests` (chỉ còn bỏ comment). Chạy lại EditMode + PlayMode toàn bộ.
6. Ghi vào Kết quả: bảng tên object cũ → mới; số khoá trong `vi.json`, danh sách nhóm khoá kèm số khoá mỗi nhóm.

## Tiêu chí xong
- EditMode toàn bộ qua (488 cũ không đổi kỳ vọng + mới), PlayMode 164/164 không đổi kỳ vọng, 0 lỗi console.
- Kết quả grep `[À-ỹĐđ]` trong literal của 8 file: 0 dòng ngoài comment (QĐ 9).
- `git diff --stat` chỉ có file trong Phạm vi.

## Test sau khi xong
### Phần cần test
- **Core tiếng Việt không đổi** (bước 1): `SiteStatusTests`, `CreatureStatusTests` qua với chuỗi kỳ vọng cũ, chỉ thêm `ViText.Load()`.
- **Core đổi ngôn ngữ** (bước 1): thêm vào `SiteStatusTests` test `CropNameUsesTable`. Dựng `new Strings(new StringFile { language = "x", entries = new[] { new StringEntry("crop.name", "Plant {0}") } }, ViText.Load())` → `CropName(t, cfg, 0) == "Plant A"`; `StorageText` vẫn ra tiếng Việt (fallback).
- **Quét** (bước 4): `NoVietnameseLiterals`, `AllUsedKeysExist`, `BaseFileValid` qua.
- **Client tiếng Việt không đổi** (bước 2–3): PlayMode toàn bộ, đặc biệt `DropPlayTests`, `PlayerBagSowPlayTests`, `MinimapCornerPlayTests`, `TimeDialPlayTests`, `SimFarmUiPlayTests`, `PlayerFarmPlayTests`, qua với kỳ vọng cũ.
- **Đổi ngôn ngữ lúc chạy** (bước 3), thêm vào `Tests/Client/SettingsPlayTests.cs` lớp cảnh hiện có (`SettingsScenePlayTests`) test `LanguageSwitch_UpdatesUgui`:
  - Ghi `lang/en.json` vào thư mục tạm của test, chỉ 2 entry: khoá mà executor đặt cho mốc Bình minh của `TimeDial.LandmarkName` = `Dawn`; khoá chữ nút đóng bản đồ (`MinimapView.cs:477`) = `Close (M)`.
  - `Apply(language "en")` → `TimeDial.LandmarkName(TimeLandmark.Dawn) == "Dawn"`.
  - Nếu cảnh test có `MinimapView` / `TimeDial` đã `Build`: `map.CloseButtonText == "Close (M)"`, `dial.LandmarkLabel(TimeLandmark.Dawn) == "Dawn"`. Nếu cảnh không có thì dựng như `MinimapCornerPlayTests` / `TimeDialPlayTests` `SetUp`.
  - Được phép thêm test này vào file `SettingsPlayTests.cs`.
- **"8 ô" từ cấu hình** (bước 2): test EditMode trong `HardcodedTextScanTests`, `ReachInText`: `ViText.Load().Format(<khoá gieo ngoài tầm>, 6f)` chứa `6 ô`.
- `SettingsPlayTests` (`:136` đã sửa) qua.

### Lệnh executor chạy
1. `date` (bắt đầu).
2. Unity Editor mở (MCP): `refresh_unity` → `read_console` không lỗi biên dịch.
3. `run_tests` EditMode toàn bộ; `run_tests` PlayMode toàn bộ → ghi dòng tổng.
4. `git checkout -- client/ProjectSettings/EditorSettings.asset`.
   - Editor tắt: `python3 tools/unity_test.py EditMode` rồi `python3 tools/unity_test.py PlayMode`.
5. `date` (kết thúc).

### User tự test
Planner chuẩn bị file `lang/tt.json`: lấy bản xuất `vi.json`, thêm tiền tố `[T]` vào mọi chữ (script python trong scratchpad), đặt vào thư mục ngôn ngữ của Editor.
Trên Mac, scene SimView (Play):
1. Tiếng Việt: HUD, nút Kho/Túi, bảng kho, bảng túi, bảng ô ruộng, chữ trên đầu sinh vật, bản đồ (M), vòng thời điểm (T) đều y như trước.
2. Cài đặt → chọn `tt`. Mọi chữ ở mục 1 có tiền tố `[T]`, trừ tên map, tên dấu mốc, `N`, `x`. Chữ nào thiếu `[T]` → báo Planner.
3. Chọn lại `vi` → chữ về như cũ.

Điện thoại (Planner tự làm bằng adb sau user xem, dời từ brief 0045):
- Build APK (menu `Game/Build APK WalkBench`, bản sao SimView). `adb push tt.json /sdcard/Android/data/<package>/files/lang/`. Mở game → Cài đặt có `tt`, chọn được, chữ đổi.
- Dọn: `git checkout -- client/ProjectSettings/UnityConnectSettings.asset`.

## Dừng và chuyển lại Opus khi
- Một test cũ phải đổi kỳ vọng (ngoài QĐ 6).
- `NoVietnameseLiterals` bắt nhầm dòng không phải chữ hiển thị mà quy tắc QĐ 7 không loại được.
- Chữ không tách được thành khoá + tham số mà vẫn giữ y hệt.
- Brief mơ hồ; cần API Unity không có trong brief; phải sửa file ngoài Phạm vi.

---
<!-- Từ đây trở xuống: EXECUTOR ghi. -->

## Kết quả
Base commit: 19f0d2f (lượt 2, sau Opus QĐ 9; lượt 1 base 003cfc1)
Chạy bằng: Sonnet · medium
Bắt đầu lượt 2: 2026-10-22 21:23 · kết thúc 21:33 (lượt 1: 21:06–21:20)
Commit: lượt 1 `brief 0046: WIP` (QĐ 1–6); lượt 2 `brief 0046: 0032 (C) chuyển chữ hiển thị sang khoá + test quét` (QĐ 9).
File đổi lượt 2: `Client/{SimView,TerrainEditInput,PlayerFarmActions,MinimapView,TimeDial}.cs`, `Tests/Client/TerrainEditPlayTests.cs` (:226-230 `Ghost`, :259 `Selection`), `Tests/Core/Text/HardcodedTextScanTests.cs` (bỏ `SkipLines`, chỉ còn bỏ comment).
`vi.json`: 163 khoá (bag 7, creature 14, crop 1, edit 20, farm 30, hud 17, item 7, map 2, plot 15, settings 13, site 11, stock 16, time 10).

Bảng tên object cũ → mới:
- SimView: `Nhà `→`House `, `Biển `→`Sign `, `Cọc`→`Post`, `Tấm`→`Board`, `Mầm `→`Sprout `, `Sinh vật `→`Creature `, `Hướng`→`Facing`, `Thân`→`Body`, `Mũi`→`Nose`, `Hàng`→`Load`
- TerrainEditInput: `Ô đang chọn`→`Selection`, `Bản mờ`→`Ghost` (GameObject + mesh)
- PlayerFarmActions: `Mưa`→`Rain`
- MinimapView: `Bản đồ`→`Map`, `Bảng bản đồ`→`Map Panel`, `Hướng dẫn`→`Hint`, `Ảnh nền`→`Background Image`, `Ranh giới`→`Borders`, `Tên map `→`Map Name `, `Dấu mốc `→`Landmark `, `Tên dấu mốc `→`Landmark Name `, `Người chơi`→`Player`, `Nút đóng`→`Close Button`, `Chữ`→`Text`, `Ô bản đồ`→`Corner`, `Mặt nạ`→`Mask`, `Nội dung`→`Content`, `Tên map`→`Map Name`, `Nền N`→`North Background`
- TimeDial: `Vòng thời điểm`→`Time Dial`, `Dòng buổi`→`Period Line`, `Nút và vòng`→`Controls`, `Nút thời điểm`→`Time Button`, `Biểu tượng`→`Icon`, `Vòng`→`Ring`, `Mốc hiện tại`→`Current Landmark`, `Mốc `+tên→`Landmark `+`m` (enum), `Tên `+tên→`Name `+`m`, `Gợi ý`→`Hint`
- Log: đã đổi sang tiếng Anh ở SimView (7 dòng), MinimapView (`MINIMAP built: …`), TimeDial (`TIMEDIAL built: …`), giữ tiền tố và số.
Ghi chú: tên không dấu (vd `Kho `+id, `Chunks`) không bị test quét bắt; `PlayerBagSowPlayTests.cs:431` tìm `"Kho 0"` nên giữ nguyên (cần task riêng nếu muốn đổi). `Core/View/SimClock.cs:18,20` có exception tiếng Việt nhưng ngoài 8 file QĐ 3, không sửa.

## Kết quả test
- EditMode toàn bộ: 493/493 qua (gồm `NoVietnameseLiterals` sạch, `AllUsedKeysExist`, `BaseFileValid`, `ReachInText`, `CropNameUsesTable`). [run]
- PlayMode toàn bộ: 165/165 qua (gồm `LanguageSwitch_UpdatesUgui`, `TerrainEditPlayTests`). [run]
- Console lỗi: 0. Đã `git checkout` EditorSettings.asset.
- grep `[À-ỹĐđ]` trong literal 8 file ngoài comment: 0 dòng [run].
- User tự xem (`tt.json`, 3 bước): chờ user; `tt.json` do Planner chuẩn bị. Thử điện thoại: chờ Planner.

## Vướng mắc
(đã giải quyết bằng QĐ 9)

---
<!-- PLANNER ghi khi review. -->

## Review
- 2026-10-22 (Opus, trả lời Vướng mắc QĐ 7): không thêm `GameObject(` vào danh sách bỏ. Theo user: mọi thứ thuộc code dùng tiếng Anh → thêm QĐ 9, bỏ hẳn danh sách bỏ dòng (chỉ còn comment), thêm bước 5. Đã kiểm `Find("…")` theo tên tiếng Việt: chỉ `TerrainEditPlayTests.cs:230,259` thuộc 8 file; `LandmarkViewPlayTests` "Dấu mốc" thuộc `LandmarkView.cs` ngoài phạm vi. Không có công cụ trong `tools/` grep nội dung log tiếng Việt.
- 2026-10-22 (Opus, review lượt 2): `Chạy bằng` Sonnet · medium khớp. Mọi dòng "Phần cần test" có kết quả (EditMode 493/493, PlayMode 165/165, console 0 lỗi); phần user xem chờ user. Diff `003cfc1..5be4128`: 16 file, đều trong Phạm vi. Đã đọc `SiteStatus` (tham số `Strings t` đầu tiên, QĐ 1), `RefreshText` + `Changed +=/-=` ở MinimapView/TimeDial (QĐ 5), `HardcodedTextScanTests` chỉ bỏ comment (QĐ 7/9), TimeDial dùng `m` (enum) cho tên object (QĐ 9). Bảng tên object cũ → mới đủ.
  - Còn sót theo chỉ đạo user: `SimView.cs:209` `Box("Kho " + s.Id)`, tiếng Việt không dấu nên test quét không bắt; `PlayerBagSowPlayTests.cs:431` tìm `"Kho 0"` (ngoài Phạm vi). Chuyển sang task code tiếng Anh toàn client, không bắt sửa ở đây.
  - `tt.json` (163 khoá, tiền tố `[T]`) đã đặt ở `~/Library/Application Support/DefaultCompany/Viventa/lang/`. Chờ user xem 3 bước.
