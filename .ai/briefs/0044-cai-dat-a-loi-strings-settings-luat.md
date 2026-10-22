# Brief 0044 — 0032 (A): lõi bảng chuỗi + dữ liệu cài đặt + tầm với / túi hạt đầu game vào JSON

> Trạng thái: chờ làm
> Review: đầy đủ
> Model thực thi: Sonnet · medium
> Cập nhật: 2026-10-22

<!-- Planner (Opus) viết, Executor (Sonnet) làm theo ở PHIÊN KHÁC — executor không có lịch sử chat, không được tự quyết.
Trạng thái: chờ làm → đang làm → chờ review → đã duyệt; hoặc → cần Opus (executor dừng, hỏi ở mục Vướng mắc).
Review: nhẹ (đọc mục Kết quả) | đầy đủ (đọc cả diff) | user xem (có phần user tự kiểm bằng mắt/chơi thử) — có thể ghép "đầy đủ + user xem".
Model thực thi: BẮT BUỘC, đúng dạng `<Model> · <effort>` — Model: Opus | Sonnet | Haiku | Fable; effort: low | medium | high | xhigh | max (chọn theo AGENTS.md §10.2).
Executor kiểm tra model + effort của phiên TRƯỚC khi làm (scripts/brief_check.py); lệch hoặc không xác định được → cảnh báo user và DỪNG.
File được commit. Tự kiểm trước khi giao (AGENTS.md §10.3): executor có phải hỏi gì không? còn chỗ nào bỏ ngỏ 2 phương án? -->

## Mục tiêu
Phần lõi (Game.Core) của [task 0032](../tasks/m1-cai-dat-va-bang-chuoi.md), chưa có giao diện:
1. **Bảng chuỗi** `Strings`: khoá → chữ, có bảng dự phòng (file gốc tiếng Việt), thay tham số `{0}`, xuất lại ra dữ liệu file.
2. **Dữ liệu cài đặt** `PlayerSettings`: các trường cài đặt + kẹp giá trị.
3. **Luật vào JSON:** tầm công cụ, tầm việc tay (8 ô), túi hạt đầu game (Cây A × 4) đọc từ `sim_m1.json` (configVersion 4 → 5), không còn `const` trong code.

Không đổi hành vi game (số giữ nguyên 8 / 8 / 0 / 4). Brief sau: B (client: file cài đặt, bảng Cài đặt, xuất file ngôn ngữ), C (chuyển chữ hiện có sang khoá). Ước lượng ~1 ngày-người (trong 4–7 của 0032).

## Bối cảnh cần đọc
Đường dẫn tính từ `client/Assets/Game/` (trừ `design/`). Số dòng theo commit `e544d7c`.
- `Core/PlayerRules.cs:12-25` — `public const float ToolReach = 8f;`, `public const float HandReach = 8f;`, `InToolReach(...)`, `InHandReach(...)` gọi `Within(reach, …)`.
- `Core/StarterBag.cs:10-11` — `public const int Crop = 0;`, `public const int Seeds = 4;` (giữ nguyên `Distance`, `SearchRadius` — vị trí demo, không phải luật). Dùng ở `StarterBag.Place` (`:51`), client `PlayerFarmActions.cs:298`, `TerrainEditInput.cs:324`, `:375`, `PlayerCharacter.cs:191-192` — các chỗ này **không phải sửa** (đổi const → thuộc tính tĩnh vẫn biên dịch).
- Nơi dùng tầm (không phải sửa, chỉ để biết): `Core/DropField.cs:158,173`, `Core/DropPick.cs:25`, `Core/Sim/Simulation.cs:1078`, `Core/StarterBag.cs:66`, `Client/SimView.cs:397`, `Client/PlayerCharacter.cs:173`.
- `Core/Sim/SimConfig.cs:127-131` (`PlayerConfig { public int rainCooldownMs = 10000; }`), `:140` (`configVersion = 4`), `:232-…` `Validate()` (mẫu `throw new ArgumentException("…")`).
- `design/data/sim_m1.json:33` và bản sao `Client/Resources/Config/sim_m1.json:33`: `"player": { "rainCooldownMs": 10000 }`. Test `Tests/World/ConfigTests.cs` bắt hai bản giống hệt; `Tests/Core/Sim/SimConfigTests.cs:32` kiểm `configVersion == 4`, `ConfigMatchesCodeDefaults` bắt JSON = giá trị mặc định trong code.
- `Client/ConfigLoader.cs:40-48` `LoadSimConfig()`: `Resources.Load` → `JsonUtility.FromJson<SimConfig>` → `Validate()`.
- `Client/PlayerCharacter.cs:70-82` `Awake()`.
- `Game.Core.asmdef`: `noEngineReferences: true` — Core không dùng `UnityEngine`. Test Core (`Tests/Core/`, asmdef `Game.Core.Tests`) được dùng `UnityEngine.JsonUtility` (như `SimConfigTests`).
- API Unity: chỉ `JsonUtility.FromJson` / `ToJson(obj, prettyPrint)` trong test, đã dùng sẵn trong repo. `JsonUtility` không đọc được `Dictionary`, đọc được mảng `[Serializable]` lớp; trường thiếu trong JSON giữ giá trị khởi tạo của lớp [docs](https://docs.unity3d.com/6000.4/Documentation/Manual/json-serialization.html).

## Quyết định đã chốt (không được đổi)
1. **Định dạng file ngôn ngữ** (lệch nhỏ so với task contract — `JsonUtility` không đọc `Dictionary`): 
   ```json
   { "formatVersion": 1, "language": "Tiếng Việt", "entries": [ { "key": "hud.rain_ready", "text": "Mưa (R): sẵn sàng" } ] }
   ```
   Lớp trong `Core/Text/StringFile.cs`, namespace `Game.Core.Text`:
   ```csharp
   [Serializable] public class StringEntry { public string key; public string text; public StringEntry() { } public StringEntry(string key, string text) { this.key = key; this.text = text; } }
   [Serializable] public class StringFile { public int formatVersion = 1; public string language = ""; public StringEntry[] entries = new StringEntry[0]; }
   ```
2. **`Strings`** — `Core/Text/Strings.cs`, `public sealed class Strings`, namespace `Game.Core.Text`:
   - `public Strings(StringFile file, Strings fallback = null)`: `file == null` → `ArgumentNullException`; `file.entries == null` coi như rỗng; entry `null` hoặc `key` rỗng/null → `ArgumentException("Khoá rỗng ở dòng " + i + ".")`; trùng khoá → `ArgumentException("Trùng khoá: " + key + ".")`; `text == null` → `""`. Lưu vào `Dictionary<string, string>(StringComparer.Ordinal)`.
   - `public string Language { get; }` (= `file.language ?? ""`), `public Strings Fallback { get; }`, `public int Count { get; }` (số khoá của chính nó).
   - `public bool Has(string key)` — chỉ bảng của chính nó.
   - `public string Get(string key)`: có trong bảng mình → chữ đó; không → `Fallback?.Get(key)`; không có ở đâu → `"[" + key + "]"`.
   - `public string Format(string key, params object[] args)`: `string.Format(CultureInfo.InvariantCulture, Get(key), args)`; nếu `FormatException` (bản dịch hỏng tham số) và `Fallback != null` → `Fallback.Format(key, args)`; không có fallback → trả chữ mẫu chưa thay (`Get(key)`). Không ném lỗi.
   - `public StringFile ToFile()`: `formatVersion = 1`, `language = Language`, `entries` = khoá của **chính nó** sắp `StringComparer.Ordinal`.
   - `public List<string> MissingKeys()`: khoá có trong `Fallback` (chỉ cấp ngay trên) mà mình không có, sắp Ordinal; `Fallback == null` → danh sách rỗng.
3. **`PlayerSettings`** — `Core/PlayerSettings.cs`, namespace `Game.Core`, `[Serializable] public class PlayerSettings`, trường public (cho `JsonUtility`):
   | Trường | Mặc định | Khoảng / luật kẹp |
   |---|---|---|
   | `int formatVersion` | 1 | không kẹp |
   | `float cameraDistance` | 6 | [2,5; 14] |
   | `float lookSensitivity` | 0,15 | [0,05; 0,5] |
   | `bool invertY` | false | — |
   | `float uiScale` | 1 | [0,75; 2] |
   | `int targetFps` | 60 | chỉ 30 hoặc 60; khác → 60 |
   | `string language` | `"vi"` | 1–32 ký tự, chỉ `a-z`, `0-9`, `_`, `-`; sai/null → `"vi"` |
   - Hằng `public const float CameraDistanceMin = 2.5f, CameraDistanceMax = 14f, LookSensitivityMin = 0.05f, LookSensitivityMax = 0.5f, UiScaleMin = 0.75f, UiScaleMax = 2f; public const string DefaultLanguage = "vi";`.
   - `public void Clamp()`: kẹp theo bảng; float `NaN` hoặc vô cực → giá trị mặc định của trường đó.
   - `public PlayerSettings Clone()` (sao từng trường).
   - `public static bool IsValidLanguage(string id)` (luật cột cuối).
4. **Luật vào JSON:** `PlayerConfig` thêm `public float toolReach = 8f; public float handReach = 8f; public int starterCrop = 0; public int starterSeeds = 4;` (giữ `rainCooldownMs`, thứ tự: `rainCooldownMs, toolReach, handReach, starterCrop, starterSeeds`). `SimConfig.configVersion = 5`. JSON (cả hai bản, giống hệt nhau):
   `"player": { "rainCooldownMs": 10000, "toolReach": 8, "handReach": 8, "starterCrop": 0, "starterSeeds": 4 }`, và `"configVersion": 5`.
   - `Validate()` thêm (sau kiểm `crops`): `toolReach`, `handReach` trong [1; 32] → nếu không `ArgumentException("player.toolReach phải trong [1, 32].")` / `handReach` tương tự; `starterCrop` trong [0; crops.Length) → `"player.starterCrop phải trong [0, số loại cây)."`; `starterSeeds` trong [0; 99] → `"player.starterSeeds phải trong [0, 99]."`. `player == null` → `"player không được thiếu."`.
5. **`PlayerRules`:** `ToolReach`, `HandReach` thành `public static float ToolReach { get; private set; } = 8f;` (tương tự `HandReach`), giữ summary cũ + thêm "(đọc từ sim_m1.json player.toolReach — Configure)". Thêm
   ```csharp
   /// <summary>Đặt luật người chơi từ cấu hình (ConfigLoader gọi sau Validate). null → ArgumentNullException.</summary>
   public static void Configure(PlayerConfig p)
   ```
   đặt `ToolReach = p.toolReach; HandReach = p.handReach; StarterBag.Crop = p.starterCrop; StarterBag.Seeds = p.starterSeeds;`. (`PlayerConfig` ở namespace `Game.Core.Sim` → `using Game.Core.Sim;`.) Không đổi chữ ký `InToolReach` / `InHandReach`.
6. **`StarterBag`:** `Crop`, `Seeds` thành `public static int Crop { get; internal set; } = 0;`, `Seeds { get; internal set; } = 4;` (giữ summary).
7. **Client:** `ConfigLoader.LoadSimConfig()` sau `c.Validate();` thêm `PlayerRules.Configure(c.player);`. `PlayerCharacter.Awake()` cuối hàm thêm `ConfigLoader.LoadSimConfig();` (để scene không có `SimView` vẫn đọc luật; bỏ kết quả). Không sửa gì khác ở client.
8. Test nào gọi `PlayerRules.Configure` với số khác mặc định phải `[TearDown] PlayerRules.Configure(new PlayerConfig());`.

## Phạm vi
- Được sửa / tạo: `Core/Text/StringFile.cs`, `Core/Text/Strings.cs`, `Core/PlayerSettings.cs` (mới, kèm `.meta` Unity tự tạo; thư mục `Core/Text/` + `.meta`); `Core/PlayerRules.cs`, `Core/StarterBag.cs`, `Core/Sim/SimConfig.cs` (chỉ `PlayerConfig`, `configVersion`, `Validate`); `design/data/sim_m1.json` + `Client/Resources/Config/sim_m1.json` (chỉ `configVersion` và khối `player`); `Client/ConfigLoader.cs`, `Client/PlayerCharacter.cs` (chỉ 1 dòng mỗi file, QĐ 7); test: `Tests/Core/Text/StringsTests.cs`, `Tests/Core/PlayerSettingsTests.cs` (mới), `Tests/Core/PlayerRulesTests.cs`, `Tests/Core/Sim/SimConfigTests.cs` (sửa `4` → `5` ở `:32` + thêm test), `Tests/Core/StarterBagTests.cs` (thêm test); mục Kết quả của brief này.
- Không được sửa: chữ hiển thị (brief C), file client khác, scene, `design/` khác, test khác.

## Các bước
1. QĐ 1–2: `StringFile`, `Strings`. Summary lớp `Strings`: "Bảng chuỗi ngôn ngữ (0032): khoá → chữ; thiếu khoá → bảng dự phòng (file gốc tiếng Việt) → `[khoá]`."
2. QĐ 3: `PlayerSettings`. Summary: "Cài đặt người chơi (0032): chỉ cảm giác / hiển thị, lưu ở settings.json (client). Không chứa luật game."
3. QĐ 4–6: `PlayerConfig`, `configVersion`, `Validate`, hai file JSON, `PlayerRules.Configure`, `StarterBag`.
4. QĐ 7: `ConfigLoader`, `PlayerCharacter`.
5. Test ở "Phần cần test".

## Tiêu chí xong
- EditMode toàn bộ qua (472 + mới), PlayMode toàn bộ qua (150/150, không đổi kỳ vọng), 0 lỗi console.
- `grep -n "const" Core/PlayerRules.cs Core/StarterBag.cs` không còn `ToolReach`, `HandReach`, `Crop`, `Seeds`.
- `git diff --stat` chỉ có file trong Phạm vi.

## Test sau khi xong
### Phần cần test
EditMode, file mới `Tests/Core/Text/StringsTests.cs` (namespace `Game.Core.Tests`):
- **Get:** bảng `{a:"A"}` → `Get("a") == "A"`; khoá không có, không fallback → `"[x]"`.
- **Fallback:** gốc `{a:"A", b:"B"}`, dịch `{a:"Ä"}` (fallback = gốc) → `Get("a") == "Ä"`, `Get("b") == "B"`, `Get("c") == "[c]"`; `MissingKeys()` = `["b"]`; `Has("b") == false`.
- **Format:** `"Mưa: còn {0} giây"`, `Format(k, 7) == "Mưa: còn 7 giây"`; số thực dùng InvariantCulture (`"{0:F1}"`, 2.5 → `"2.5"`).
- **Format hỏng:** dịch `"Rain {1}"` (thiếu tham số) với fallback `"Mưa {0}"` → `Format(k, 3) == "Mưa 3"`; không fallback → trả `"Rain {1}"`, không ném.
- **Lỗi file:** `null` → `ArgumentNullException`; khoá rỗng, entry null → `ArgumentException`; trùng khoá → `ArgumentException`; `entries == null` → `Count == 0`; `text == null` → `Get == ""`.
- **ToFile / JSON:** dựng từ entries lộn thứ tự → `ToFile().entries` sắp Ordinal; `JsonUtility.ToJson(file, true)` → `FromJson<StringFile>` → `new Strings(...)` cho cùng `Get` mọi khoá + cùng `Language`; JSON mẫu ở QĐ 1 (viết tay trong test, có chữ tiếng Việt có dấu) đọc ra đúng.

EditMode, file mới `Tests/Core/PlayerSettingsTests.cs`:
- **Mặc định:** `new PlayerSettings()` đúng bảng QĐ 3; `Clamp()` không đổi gì.
- **Kẹp:** cameraDistance 1 → 2,5, 99 → 14; lookSensitivity 0 → 0,05, 1 → 0,5; uiScale 0,1 → 0,75, 5 → 2; targetFps 45 → 60, 30 → 30; `float.NaN` / `PositiveInfinity` ở mỗi trường float → mặc định.
- **Ngôn ngữ:** `"en"`, `"vi_custom-2"` hợp lệ; `null`, `""`, `"../x"`, `"EN"`, `"a b"`, chuỗi 33 ký tự → `Clamp()` đặt `"vi"`.
- **JSON thiếu trường:** `JsonUtility.FromJson<PlayerSettings>("{\"uiScale\":1.5}")` → uiScale 1,5, các trường khác mặc định.
- **Clone:** sửa bản sao không đổi bản gốc.

EditMode, sửa / thêm:
- `SimConfigTests`: `:32` → `5`; thêm assert `cfg.player.toolReach == 8`, `handReach == 8`, `starterCrop == 0`, `starterSeeds == 4`; `ValidateRejectsBadValues` (hoặc test mới) thêm: toolReach 0 / 33, handReach 0.5f, starterCrop 2 (có 2 loại cây) / −1, starterSeeds −1 / 100, `player = null` → `ArgumentException`. `ConfigMatchesCodeDefaults` phải qua.
- `PlayerRulesTests` (thêm `[TearDown]` QĐ 8): `Configure(new PlayerConfig { toolReach = 10, handReach = 3 })` → `InToolReach(0.5f, -0.5f, 0.5f, 10, 0, 0)` true, `(…, 11, 0, 0)` false; `InHandReach(…, 3, 0, 0)` true, `(…, 4, 0, 0)` false; `Configure(null)` → `ArgumentNullException`. Test cũ `*_BoundaryExactly8` vẫn qua.
- `StarterBagTests` (thêm `[TearDown]`): `Configure(new PlayerConfig { starterCrop = 1, starterSeeds = 2 })` → `Place` trả chồng `ItemId.SeedBag(1)` × 2.
- `ConfigTests.SimClientCopyMatchesDesignData` qua (hai file JSON giống hệt).

PlayMode (không thêm test): toàn bộ 150 qua — chứng minh tầm 8 ô và túi hạt đầu game không đổi sau khi đọc từ JSON (`PlayerCharacterPlayTests`, `DropPlayTests`, `PlayerFarmPlayTests`, `PlayerBagSowPlayTests`).

### Lệnh executor chạy
1. `date` (bắt đầu).
2. Unity Editor mở (MCP): `refresh_unity` → `read_console` không lỗi biên dịch.
3. `run_tests` EditMode lọc `StringsTests`, `PlayerSettingsTests`, `PlayerRulesTests`, `SimConfigTests`, `StarterBagTests`, `ConfigTests` → qua; `run_tests` EditMode toàn bộ; `run_tests` PlayMode toàn bộ → ghi dòng tổng.
4. `git checkout -- client/ProjectSettings/EditorSettings.asset`.
   - Editor tắt: `python3 tools/unity_test.py EditMode` rồi `python3 tools/unity_test.py PlayMode`.
5. `python3 tools/check_design.py` (đổi `design/data/`).
6. `date` (kết thúc).

### User tự test
- Không có (lõi, không đổi hành vi). Gộp vào lượt xem brief B.

## Dừng và chuyển lại Opus khi
- Một test cũ đổi kết quả (ngoài `SimConfigTests:32`).
- `ToolReach` / `HandReach` / `Crop` / `Seeds` được dùng ở chỗ cần hằng (`const`, `case`, giá trị mặc định tham số) → không biên dịch.
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
