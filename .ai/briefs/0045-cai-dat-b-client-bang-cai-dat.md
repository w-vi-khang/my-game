# Brief 0045 — 0032 (B): client — file cài đặt, bảng Cài đặt, áp cài đặt, xuất file ngôn ngữ

> Trạng thái: chờ làm
> Review: đầy đủ + user xem
> Model thực thi: Sonnet · medium
> Cập nhật: 2026-10-22

<!-- Planner (Opus) viết, Executor (Sonnet) làm theo ở PHIÊN KHÁC — executor không có lịch sử chat, không được tự quyết.
Trạng thái: chờ làm → đang làm → chờ review → đã duyệt; hoặc → cần Opus (executor dừng, hỏi ở mục Vướng mắc).
Review: nhẹ (đọc mục Kết quả) | đầy đủ (đọc cả diff) | user xem (có phần user tự kiểm bằng mắt/chơi thử) — có thể ghép "đầy đủ + user xem".
Model thực thi: BẮT BUỘC, đúng dạng `<Model> · <effort>` — Model: Opus | Sonnet | Haiku | Fable; effort: low | medium | high | xhigh | max (chọn theo AGENTS.md §10.2).
Executor kiểm tra model + effort của phiên TRƯỚC khi làm (scripts/brief_check.py); lệch hoặc không xác định được → cảnh báo user và DỪNG.
File được commit. Tự kiểm trước khi giao (AGENTS.md §10.3): executor có phải hỏi gì không? còn chỗ nào bỏ ngỏ 2 phương án? -->

## Mục tiêu
Phần client của [task 0032](../tasks/m1-cai-dat-va-bang-chuoi.md), dùng lõi brief 0044 (`Game.Core.PlayerSettings`, `Game.Core.Text.Strings`):
1. `settings.json` trong `Application.persistentDataPath`: đọc lúc chạy, ghi khi đóng bảng.
2. Bảng **Cài đặt** (IMGUI) mở bằng `O` / nút `Cài đặt (O)` / `Esc` khi không có bảng nào mở: khoảng cách camera, tốc độ xoay chuột, đảo trục dọc, cỡ chữ, FPS mục tiêu, ngôn ngữ, xuất file gốc.
3. Áp cài đặt: camera theo sau + camera bay, FPS, cỡ chữ IMGUI.
4. Bảng chuỗi lúc chạy: file gốc `Resources/Lang/vi.json` (brief này chỉ có chữ của bảng Cài đặt; brief C chuyển phần còn lại), file người chơi trong `persistentDataPath/lang/<id>.json`.

Ước lượng ~1,5 ngày-người (trong 4–7 của 0032).

## Bối cảnh cần đọc
Đường dẫn tính từ `client/Assets/Game/`. Số dòng theo commit `0e009ca`.
- Lõi (brief 0044): `Core/PlayerSettings.cs` (trường `cameraDistance`, `lookSensitivity`, `invertY`, `uiScale`, `targetFps`, `language`; `Clamp()`, `Clone()`, `IsValidLanguage`, hằng `*Min/*Max`, `DefaultLanguage = "vi"`), `Core/Text/StringFile.cs` (`StringFile { formatVersion, language, entries }`, `StringEntry { key, text }`), `Core/Text/Strings.cs` (`new Strings(file, fallback)`, `Get`, `Format`, `ToFile`, `Count`, `Has`).
- `Client/FollowCamera.cs:14-17` (`distance`, `minDistance`, `maxDistance`, `lookSensitivity`), `:35-40` xoay chuột: `pitch = Mathf.Clamp(pitch - d.y * lookSensitivity, minPitch, maxPitch);`.
- `Client/DebugFlyCamera.cs:17` `lookSensitivity`, `:60-65` xoay: `pitch = Mathf.Clamp(pitch - d.y * lookSensitivity, -89f, 89f);`.
- `Client/SimView.cs`: `Awake()` `:71-78`; `Update()` phím `:119-128` (F1/F2 → `SetFrameRate`, K, B, Esc đóng túi `:126`, Esc đóng bảng ô ruộng `:127`); `Setup()` cuối `:214` `SetFrameRate(60);`; `void SetFrameRate(int fps)` `:217-222`; `OpenPlotId` `:270`, `StockOpen` `:271`, `BagOpen` `:348`; `BagButtonRect` `:383` (`new Rect(gw - 120f, gh - 46f, 110f, 36f)`); `static float GuiScale => Mathf.Max(1f, Screen.height / 720f);` `:445`; `BlocksPointer` `:453`; `OnGUI` `:655` `float scale = Mathf.Max(1f, Screen.height / 720f);`.
- `Client/TerrainEditInput.cs:400` `float scale = Mathf.Max(1f, Screen.height / 720f);`; `Farming` (`PlayerFarmActions`, `SowCrop >= 0` = đang gieo, `Client/PlayerFarmActions.cs:21`).
- `MinimapView.IsAnyOpen` (`Client/MinimapView.cs:21`), `TimeDial.IsAnyOpen` (`Client/TimeDial.cs:17`) — cả hai đóng bằng Esc trong `Update` riêng (`MinimapView.cs:159`, `TimeDial.cs:138`).
- Mọi chỗ chặn con trỏ đi qua `SimView.BlocksPointer` (gọi ở `TerrainEditInput.cs:101`, `FollowCamera.cs:44`).
- Mẫu test PlayMode dựng cảnh: `Tests/Client/PlayerBagSowPlayTests.cs:22-46` (`SetUp` tạo Cam + `FollowCamera` + `DebugFlyCamera`, Terrain + `SimView` + `TerrainEditInput`, Player; chờ `sv.Sim != null`). Test không giả lập phím — gọi hàm public.
- API Unity (6000.4), ngoài API đã dùng trong repo:
  - `Application.persistentDataPath` [docs](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/Application-persistentDataPath.html).
  - `JsonUtility.FromJson` / `ToJson(obj, true)` — trường thiếu giữ giá trị khởi tạo [docs](https://docs.unity3d.com/6000.4/Documentation/Manual/json-serialization.html). JSON hỏng → ném ngoại lệ (bắt `Exception`).
  - `GUI.HorizontalSlider(Rect, float value, float left, float right)` [docs](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/GUI.HorizontalSlider.html), `GUI.Toggle(Rect, bool, string)` [docs](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/GUI.Toggle.html), `GUI.Box(Rect, string)` [docs](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/GUI.Box.html).
  - `[DefaultExecutionOrder(int)]` [docs](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/DefaultExecutionOrder.html).
  - NUnit `[SetUpFixture]` + `[OneTimeSetUp]` / `[OneTimeTearDown]` (NUnit 3, gói Test Framework 1.6.0 có sẵn).
  - `System.IO.File`, `Directory`, `Path` (.NET).

## Quyết định đã chốt (không được đổi)
1. **`Client/GameSettings.cs`** — `public static class GameSettings`, namespace `Game.Client`:
   - `public static string RootOverride;` (test đặt thư mục tạm; `null` → `Application.persistentDataPath`). `public static string Root => RootOverride ?? Application.persistentDataPath;`
   - `public static string SettingsPath => Path.Combine(Root, "settings.json");` `public static string LangDir => Path.Combine(Root, "lang");`
   - `public static PlayerSettings Current` — lazy: lần đầu đọc gọi `Load()`.
   - `public static Strings BaseText` — lazy: `Resources.Load<TextAsset>("Lang/vi")` → `JsonUtility.FromJson<StringFile>` → `new Strings(file)`; thiếu file → `FileNotFoundException("Không thấy Resources/Lang/vi.json")`.
   - `public static Strings Text` — bảng đang dùng (lazy qua `Load()`).
   - `public static string LanguageError { get; private set; }` — `null` khi nạp được; khác `null` khi file ngôn ngữ người chơi hỏng/thiếu.
   - `public static event Action Changed;`
   - `public static void Load()`: `settings.json` không có → `new PlayerSettings()`; có → `JsonUtility.FromJson<PlayerSettings>`; lỗi đọc/JSON → `Debug.LogWarning("SETTINGS không đọc được settings.json: …")` + mặc định. Luôn `Clamp()`. Rồi `LoadLanguage()`.
   - `static void LoadLanguage()`: `language == "vi"` → `Text = BaseText`, `LanguageError = null`. Khác → đọc `LangDir/<id>.json` → `new Strings(file, BaseText)`; mọi lỗi (thiếu file, JSON hỏng, `ArgumentException`) → `Text = BaseText`, `LanguageError = id`, `Debug.LogWarning("LANG không nạp được <id>: …")`. **Không** đổi `Current.language`.
   - `public static void Apply(PlayerSettings s)`: `Current = s.Clone()`, `Current.Clamp()`; nếu `language` khác trước → `LoadLanguage()`; `Dirty = true`; `Changed?.Invoke()`. Không ghi file.
   - `public static bool Dirty { get; private set; }`; `public static void Save()`: `Directory.CreateDirectory(Root)`, ghi `JsonUtility.ToJson(Current, true)`; `Dirty = false`; lỗi IO → `Debug.LogWarning`.
   - `public static List<string> Languages()`: `"vi"` + tên file (bỏ `.json`) của `LangDir/*.json` thoả `PlayerSettings.IsValidLanguage`, trừ `"vi"`; sắp `StringComparer.Ordinal`. Thư mục không có → `["vi"]`.
   - `public static string ExportBase()`: `Directory.CreateDirectory(LangDir)`, ghi `JsonUtility.ToJson(BaseText.ToFile(), true)` vào `LangDir/vi.json`, trả đường dẫn. (`vi.json` trong `lang/` chỉ là bản để dịch — `"vi"` luôn dùng bản trong `Resources`.)
   - `public static void ResetForTests(string root)`: `RootOverride = root`, xoá `Current`/`Text`/`BaseText`/`LanguageError`, `Dirty = false`, `Changed = null`.
2. **`Client/UiScale.cs`** — `public static class UiScale { public static float Gui => Mathf.Max(1f, Screen.height / 720f) * GameSettings.Current.uiScale; }`. Thay `SimView.cs:445` thân `GuiScale` bằng `UiScale.Gui`, `SimView.cs:655` và `TerrainEditInput.cs:400` bằng `float scale = UiScale.Gui;`. Bản đồ góc / vòng thời điểm (uGUI `CanvasScaler`) **không** đổi theo cỡ chữ ở M1.
3. **Camera:** `FollowCamera` và `DebugFlyCamera` thêm `public bool invertY;`; dòng xoay dọc dùng `d.y * lookSensitivity * (invertY ? -1f : 1f)`.
4. **`Client/SettingsView.cs`** — `[DefaultExecutionOrder(-50)] public class SettingsView : MonoBehaviour`, namespace `Game.Client`:
   - `SimView.Awake()` thêm cuối: `if (GetComponent<SettingsView>() == null) gameObject.AddComponent<SettingsView>();`.
   - `public bool IsOpen { get; private set; }`; `static SettingsView active` (gán trong `Awake`, xoá trong `OnDestroy` nếu là mình).
   - `public static bool BlocksPointer(Vector2 screen)` → `active != null && active.Covers(screen)`; `Covers`: nút `ButtonRect` luôn, `PanelRect` khi `IsOpen` (đổi toạ độ như `SimView.Covers` `:457-458`, dùng `UiScale.Gui`). `SimView.BlocksPointer` (`:453`) đổi thành `SettingsView.BlocksPointer(screen) || (active != null && active.Sim != null && active.Covers(screen))`.
   - `public static Rect ButtonRect(float gw, float gh) => new Rect(gw - 240f, gh - 46f, 110f, 36f);` (bên trái nút Túi). `public static Rect PanelRect(float gw, float gh) => new Rect(gw * 0.5f - 230f, gh * 0.5f - 210f, 460f, 420f);`
   - `public bool OtherPanelOpen()`: `SimView` cùng GameObject có `BagOpen || StockOpen || OpenPlotId >= 0`, hoặc `MinimapView.IsAnyOpen`, `TimeDial.IsAnyOpen`, hoặc `TerrainEditInput` cùng GameObject có `Farming != null && Farming.SowCrop >= 0`.
   - `public void HandleKeys(bool oPressed, bool escPressed)`: `oPressed` → `SetOpen(!IsOpen)`; else `escPressed`: `IsOpen` → `SetOpen(false)`; không mở và `!OtherPanelOpen()` → `SetOpen(true)`. `Update()` gọi `HandleKeys(kb.oKey.wasPressedThisFrame, kb.escapeKey.wasPressedThisFrame)` (`Keyboard.current` null → bỏ). Chạy trước các component khác (thứ tự −50) nên Esc khi túi đang mở: `OtherPanelOpen()` = true → không mở Cài đặt, `SimView` đóng túi như cũ.
   - `public void SetOpen(bool open)`: đóng mà `GameSettings.Dirty` → `GameSettings.Save()`.
   - `Start()`: `ApplyToScene()`; `GameSettings.Changed += ApplyToScene` (gỡ trong `OnDestroy`).
   - `public void ApplyToScene()`: `Camera.main` có `FollowCamera` → `distance = Mathf.Clamp(s.cameraDistance, minDistance, maxDistance)`, `lookSensitivity`, `invertY`; có `DebugFlyCamera` → `lookSensitivity`, `invertY`; `SimView` cùng GameObject → `SetFrameRate(s.targetFps)` (đổi `SetFrameRate` thành `public`). (`s = GameSettings.Current`.) Lăn chuột vẫn đổi `distance` lúc chơi, không ghi vào cài đặt.
   - `SimView.Setup()` `:214`: `SetFrameRate(60)` → `SetFrameRate(GameSettings.Current.targetFps)`. F1 / F2 giữ nguyên (phím dev, không ghi cài đặt).
   - `OnGUI()`: `GUI.matrix` theo `UiScale.Gui` (như `SimView.OnGUI`). Bỏ khi `MinimapView.IsAnyOpen`. Luôn vẽ nút `ButtonRect` chữ `T("settings.button")` → bấm = `SetOpen(!IsOpen)`. Khi `IsOpen` vẽ `GUI.Box(PanelRect, "")` và các dòng (x = panel.x + 16, mỗi dòng cao 30, rộng panel − 32):
     1. Nhãn `T("settings.title")` (in đậm: `<b>…</b>`, style `richText`).
     2. `Format("settings.camera_distance", v)` + `GUI.HorizontalSlider` [CameraDistanceMin; CameraDistanceMax].
     3. `Format("settings.look_sensitivity", v)` + slider [LookSensitivityMin; LookSensitivityMax].
     4. `GUI.Toggle` `T("settings.invert_y")`.
     5. `Format("settings.ui_scale", v)` + hai nút `−` / `+` bước 0,25 (không dùng slider — đổi cỡ chữ lúc kéo làm bảng chạy dưới chuột).
     6. `T("settings.fps")` + hai nút `30`, `60` (nút đang chọn in đậm).
     7. `T("settings.language")` + một nút mỗi id trong `GameSettings.Languages()` (đang chọn in đậm); nếu `LanguageError != null` thêm dòng `Format("settings.lang_error", LanguageError)` màu đỏ (`<color=red>`).
     8. Nút `T("settings.export")` → `lastExport = GameSettings.ExportBase()`; dòng `Format("settings.exported", lastExport)` khi đã xuất.
     9. Dòng `Format("settings.lang_dir", GameSettings.LangDir)` (`wordWrap = true`, cao 48).
     10. Nút `T("settings.close")` → `SetOpen(false)`.
     Mỗi thay đổi giá trị: `var s = GameSettings.Current.Clone(); s.x = …; GameSettings.Apply(s);`. `T(k) => GameSettings.Text.Get(k)`, `Format` → `GameSettings.Text.Format`.
   - `public static readonly string[] Keys` = 13 khoá ở QĐ 5.
5. **File gốc `Client/Resources/Lang/vi.json`** (thư mục mới), đúng định dạng brief 0044, đúng 13 khoá:
   | key | text |
   |---|---|
   | `settings.button` | `Cài đặt (O)` |
   | `settings.title` | `Cài đặt` |
   | `settings.camera_distance` | `Khoảng cách camera: {0:F1}` |
   | `settings.look_sensitivity` | `Tốc độ xoay chuột: {0:F2}` |
   | `settings.invert_y` | `Đảo trục dọc` |
   | `settings.ui_scale` | `Cỡ chữ: ×{0:F2}` |
   | `settings.fps` | `FPS mục tiêu` |
   | `settings.language` | `Ngôn ngữ` |
   | `settings.lang_error` | `Không đọc được file ngôn ngữ {0} — dùng tiếng Việt` |
   | `settings.export` | `Xuất file gốc` |
   | `settings.exported` | `Đã xuất: {0}` |
   | `settings.lang_dir` | `Thư mục ngôn ngữ: {0}` |
   | `settings.close` | `Đóng (Esc)` |
   `"language": "Tiếng Việt"`, entries sắp theo khoá (Ordinal).
6. **Cách ly test:** `Tests/Client/SettingsTestRoot.cs` — `[SetUpFixture] public class SettingsTestRoot` trong namespace `Game.Client.Tests`: `[OneTimeSetUp]` → `GameSettings.ResetForTests(<Path.GetTempPath()>/mygame-test-<Guid>)`; `[OneTimeTearDown]` → xoá thư mục đó (bỏ qua lỗi), `GameSettings.ResetForTests(null)`. Test PlayMode không bao giờ đọc/ghi `settings.json` thật của Editor.
7. Đóng bảng Cài đặt bằng Esc khi túi cũng đang mở → cả hai đóng cùng khung: chấp nhận M1. Game vẫn chạy khi bảng mở (không tạm dừng).

## Phạm vi
- Tạo: `Client/GameSettings.cs`, `Client/UiScale.cs`, `Client/SettingsView.cs`, `Client/Resources/Lang/vi.json` (+ `.meta`, thư mục `Lang` + `.meta`); `Tests/Client/SettingsTestRoot.cs`, `Tests/Client/SettingsPlayTests.cs` (+ `.meta`).
- Sửa: `Client/SimView.cs` (chỉ: `Awake` thêm SettingsView, `Setup` `:214`, `SetFrameRate` public, `GuiScale`, `BlocksPointer`, `OnGUI` `:655`), `Client/TerrainEditInput.cs` (chỉ `:400`), `Client/FollowCamera.cs`, `Client/DebugFlyCamera.cs` (chỉ QĐ 3); mục Kết quả của brief này.
- Không được sửa: `Core/**`, scene, `design/`, chữ hiển thị đã có (brief C), test khác, file khác.

## Các bước
1. QĐ 5: `vi.json`.
2. QĐ 1: `GameSettings`. Summary: "Cài đặt người chơi + bảng chuỗi lúc chạy (0032): settings.json và lang/ trong persistentDataPath; tiếng Việt gốc ở Resources/Lang/vi.json."
3. QĐ 2–3: `UiScale`, sửa `SimView`/`TerrainEditInput`/camera.
4. QĐ 4: `SettingsView` + sửa `SimView`.
5. QĐ 6 + test ở "Phần cần test".

## Tiêu chí xong
- PlayMode toàn bộ qua (150 cũ không đổi kỳ vọng + mới), EditMode 488/488, 0 lỗi console.
- `git diff --stat` chỉ có file trong Phạm vi.

## Test sau khi xong
### Phần cần test
PlayMode, `Tests/Client/SettingsPlayTests.cs`. Mỗi test `[SetUp]` gọi `GameSettings.ResetForTests(<thư mục tạm riêng của test>)`, `[TearDown]` xoá thư mục đó và gọi lại `ResetForTests` về thư mục của `SettingsTestRoot` (lưu đường dẫn ở trường static public của `SettingsTestRoot`). Test GameSettings thuần là `[Test]` (không cần cảnh); test cần cảnh dựng như `PlayerBagSowPlayTests.SetUp`.
- **Cách ly:** `GameSettings.Root` bắt đầu bằng `Path.GetTempPath()` khi chạy test (kiểm trong một test bất kỳ, không gọi `ResetForTests` trước) → chứng minh `SetUpFixture` chạy.
- **Đọc / ghi:** không có file → mặc định; `Apply` (uiScale 1,5) → `Dirty`; `Save()` → file có; `ResetForTests(cùng thư mục)` → `Current.uiScale == 1.5f`.
- **File hỏng / ngoài khoảng:** `settings.json` = `{bad` → mặc định, không ném; `{"uiScale":9,"targetFps":45}` → 2 và 60.
- **Ngôn ngữ:** `lang/en.json` có `settings.title` = `Settings` → `Apply(language "en")` → `Text.Get("settings.title") == "Settings"`, `Text.Get("settings.close") == "Đóng (Esc)"` (fallback), `LanguageError == null`; `lang/xx.json` = `{bad` → `Apply("xx")` → `Text.Get("settings.title") == "Cài đặt"`, `LanguageError == "xx"`, `Current.language == "xx"`.
- **Danh sách:** tạo `en.json`, `vi.json`, `Bad Name.json`, `x.txt` trong `lang/` → `Languages()` = `["en", "vi"]`; không có thư mục → `["vi"]`.
- **Xuất:** `ExportBase()` → file `lang/vi.json` có; đọc lại → `Count == BaseText.Count` và mọi khoá cùng `Get`.
- **File gốc:** mọi khoá trong `SettingsView.Keys` có trong `BaseText` (`Has`), `BaseText.Count == 13`.
- **UiScale:** `UiScale.Gui == Mathf.Max(1f, Screen.height / 720f) * 1.5f` sau `Apply(uiScale 1.5)`.
- **Áp vào cảnh** (dựng cảnh): `sv.GetComponent<SettingsView>()` khác null; `Apply` cameraDistance 10, lookSensitivity 0,3, invertY true, targetFps 30 → `FollowCamera.distance == 10`, `lookSensitivity == 0.3f`, `invertY`, `DebugFlyCamera.lookSensitivity == 0.3f`, `Application.targetFrameRate == 30`, `sv.TargetFps == 30`.
- **Phím** (dựng cảnh, gọi `HandleKeys`): `(true,false)` → mở; `(true,false)` → đóng; `(false,true)` khi không có gì mở → mở; `(false,true)` → đóng; mở túi (`sv.ToggleBag()`, `SimView.cs:349`) rồi `(false,true)` → Cài đặt vẫn đóng. Đóng khi `Dirty` → `settings.json` được ghi, `Dirty == false`.
- **Chặn con trỏ:** khi mở, điểm giữa `PanelRect` (đổi sang toạ độ màn hình) → `SimView.BlocksPointer == true`; khi đóng → false (điểm đó không nằm trên nút/bảng khác).
- Cũ: toàn bộ PlayMode (đặc biệt `SimFarmUiPlayTests`, `PlayerBagSowPlayTests`, `PlayerCharacterPlayTests`, `SimViewPlayTests` — dùng `GuiScale`, `BlocksPointer`, FPS 60) qua không đổi kỳ vọng.

### Lệnh executor chạy
1. `date` (bắt đầu).
2. Unity Editor mở (MCP): `refresh_unity` → `read_console` không lỗi biên dịch.
3. `run_tests` PlayMode lọc `SettingsPlayTests` → qua; `run_tests` PlayMode toàn bộ; `run_tests` EditMode toàn bộ → ghi dòng tổng.
4. `git checkout -- client/ProjectSettings/EditorSettings.asset`.
   - Editor tắt: `python3 tools/unity_test.py PlayMode` rồi `python3 tools/unity_test.py EditMode`.
5. `date` (kết thúc).

### User tự test
Trên Mac, scene SimView (Play):
1. Nút `Cài đặt (O)` bên trái nút `Túi (B)`. `O` mở/đóng; `Esc` khi không có bảng nào mở → mở; `Esc` khi túi mở → chỉ đóng túi.
2. Kéo khoảng cách camera, tốc độ xoay → camera đổi ngay; bật Đảo trục dọc → kéo chuột giữa lên/xuống ngược lại.
3. Cỡ chữ `−` / `+` → HUD, nút, bảng to/nhỏ ngay, không đè nhau ở ×0,75 và ×2.
4. FPS 30 / 60 → dòng `mục tiêu` đổi.
5. `Xuất file gốc` → hiện đường dẫn; mở Finder tới thư mục đó thấy `lang/vi.json`. Chép thành `en.json`, sửa `"Cài đặt"` → `"Settings"`, mở lại bảng → có nút `en`, chọn → tiêu đề đổi.
6. Thoát Play, Play lại → cài đặt giữ nguyên.
7. Nhận xét bố cục bảng (đè bản đồ góc / vòng thời điểm không).
Điện thoại (Planner tự làm bằng adb sau review): đường dẫn `Android/data/<package>/files/lang` chép file vào được không.

## Dừng và chuyển lại Opus khi
- Test "Cách ly" không qua (SetUpFixture không chạy trong PlayMode).
- Một test cũ đổi kết quả.
- Brief mơ hồ; cần API Unity không có trong brief; phải sửa file ngoài Phạm vi.

---
<!-- Từ đây trở xuống: EXECUTOR ghi. -->

## Kết quả
Base commit:
Chạy bằng:
Bắt đầu: · Kết thúc: (theo `date`)

## Kết quả test

## Vướng mắc

---
<!-- PLANNER ghi khi review. -->

## Review
