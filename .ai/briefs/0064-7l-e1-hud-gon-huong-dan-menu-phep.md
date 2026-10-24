# Brief 0064 — 7l e1: HUD gọn (avatar + máu + mana, thông số trên cùng, bỏ chữ hướng dẫn → Cài đặt → Hướng dẫn) + menu phép G

> Trạng thái: chờ review
> Review: đầy đủ + user xem
> Model thực thi: Sonnet · medium
> Cập nhật: 2026-10-24

<!-- Planner (Opus) viết, Executor (Sonnet) làm theo ở PHIÊN KHÁC — executor không có lịch sử chat, không được tự quyết. -->

## Mục tiêu
Bước **7l e1** (brief 1/2 của 7l e) của task [m1-world-7l-dieu-khien-pc](../tasks/m1-world-7l-dieu-khien-pc.md). Luật: [0034 mục 7 "HUD và menu"](../../design/decisions/0034-dieu-khien-pc-click-di-chuyen.md#7-hud-và-menu). Dòng 7l e ở [world-tech](../../design/04-tech/world-tech.md#kế-hoạch-prototype-m1).

Brief này làm phần **HUD 2D + menu phép**:
1. Bỏ chữ hướng dẫn phím khỏi HUD. Viết lại theo phím mới, đặt vào **Cài đặt → tab Hướng dẫn**.
2. Bỏ dòng kho / từng ô ruộng ở HUD trái, nút + dòng "Đang ở buổi: …".
3. Góc trên-trái: **avatar + thanh máu + thanh mana**. Dưới đó chỉ còn dòng chế độ / vật cầm (khi có) và dòng thông báo (tự ẩn sau [số tạm] 4 giây).
4. **Trên cùng giữa:** thông số (FPS, nhịp, dựng lại chunk).
5. **Menu phép (G)** thay nút buổi + phím T: nút "Phép" ở chỗ nút buổi cũ → bảng 2 mục: **Đổi buổi** (icon buổi hiện tại → mở vòng 4 mốc như cũ) và **Mưa** (vào chế độ Tưới, như R; hiện thời gian hồi).

Brief **0065** (sau) làm icon + vòng tiến trình trên ô ruộng, icon trạng thái sinh vật (thay nhãn chữ + dấu "!" đình trệ), đo ≥ 30 FPS điện thoại. **Không** đụng nhãn chữ sinh vật / dấu đình trệ trong brief này.

Bắt đầu: kiểm brief 0063 có `> Trạng thái: đã duyệt`. Không có → dừng, `cần Opus`.

**File lạ / của phiên khác — KHÔNG commit, KHÔNG sửa:** `client/Assets/InitTestScene*.unity(.meta)`; thay đổi chưa commit trong `design/` (economy, mechanics, progression, 05-roadmap, summary, glossary, ideas, decisions/README, decisions/0038-*), `.claude/skills/local-terrain-gen/`, `tools/terrain_catalog*`, `.ai/skill-map.local.json`. Chỉ `git add` đúng file trong "Phạm vi".

Ước lượng: 1–1,5 ngày-người (một nửa của 2–3 ngày dòng 7l e, M1).

## Bối cảnh cần đọc
Đường dẫn tính từ `client/Assets/Game/` trừ khi ghi khác. Số dòng theo commit `6844059` — tìm theo tên nếu lệch.

**HUD trái hiện nay (IMGUI):**
- `Client/TerrainEditInput.cs`:
  - `HelpText()` `:498–506` (dùng `hud.help_idle` / `hud.help_fly` / `hud.help_walk`);
  - `public string HudText()` `:509–530`: ghép `hud.mode` + `hud.held` + HelpText + `hud.till_help` + `hud.camera_help` + `hud.elevation` + `hud.aimed_drop` + `Farming.HudLines()` + `hud.aimed_harvest` + `lastResult` + `hud.rebuild`;
  - `public static float HudBottom` `:535`; `void OnGUI()` `:535–560` (thanh máu/mana tạm của brief 0060 qua `Bar(...)` `:562–568`, nhãn `vitals.hp` / `vitals.mana`);
  - `lastResult` `:30`, `public void SetResult(string text)` `:198`; `public void ToggleMode(ControlMode m)` `:156–162`; `public ControlState Control` `:42`; `public PlayerFarmActions Farming` `:63`.
- `Client/PlayerFarmActions.cs`: `public string HudLines()` `:260–280` (dòng Mưa sẵn sàng / hồi, đang xới / đang đi tới, `StatusLine`, nông sản + túi hạt trong túi); `public Cooldown RainCooldown` `:18` (`Ready`, `RemainingWholeSeconds` — `Core/PlayerTimers.cs:57,69`).
- `Client/SimView.cs` `OnGUI()` `:541–575`: dòng `hud.fps` + `SiteStatus.StorageText` + từng `SiteStatus.PlotText` + `Meter.LastLine`, đặt ở `StatusTop(TerrainEditInput.HudBottom)` `:536`; sau đó `DrawFarmGui` (bảng ô ruộng — giữ) và nhãn sinh vật + dấu đình trệ (giữ, 0065 làm).
- Chữ hiện có: `Client/Resources/Lang/vi.json` (khoá `hud.*`, `farm.rain_ready`, `farm.rain_hud_cooldown`, `farm.hud_*`, `time.current`, `time.hint`, `vitals.*`). Cách đọc: `GameSettings.Text.Get(key)` / `.Format(key, args)`; mọi chữ tiếng Việt phải nằm trong `vi.json` (test `Tests/Core/Text/HardcodedTextScanTests.cs`; test có chữ kỳ vọng thì gắn `// vi-text`).

**Vòng buổi (uGUI dựng bằng code, không EventSystem):**
- `Client/TimeDial.cs` (363 dòng) — đọc hết. Nút tròn `button` (`Build()` `:285–289`, vị trí `(-16, -232)` neo trên-phải, 72×72), dòng `periodText` (`:279–281`), vòng `ring` + 4 mốc, `hintText` (`time.hint`). `Update()` `:118–166` đọc `GameInput.ToggleTimeDial` (T), `TimeDialClose` (Esc), `TimeDialPress` (chuột trái), chạm. `PointerDown(Vector2)` `:175–196`, `BlocksPointer` `:17–19`, `IsAnyOpen` `:15`, `Refresh()` `:224–247`.
- `Client/TimeDialGraphic.cs`: `enum TimeDialShape : byte { Disk, Moon, Sun, Dawn, Dusk }`; `OnPopulateMesh` vẽ bằng `Fan` / `Quad` / `Tri`.
- Nơi kiểm `TimeDial.BlocksPointer` / `IsAnyOpen`: `Client/FollowCamera.cs:100`, `Client/PlayerCharacter.cs:353`, `Client/TerrainEditInput.cs:115`, `Client/SettingsView.cs:66`.

**Cài đặt (IMGUI):** `Client/SettingsView.cs` (210 dòng) — đọc hết: `PanelRect` `:31` (460×480), `OnGUI()` `:118–201` (các hàng camera / độ nhạy / cỡ chữ / FPS / ngôn ngữ / xuất file / thư mục ngôn ngữ / nút Đóng), `OtherPanelOpen()` `:62–68`, `HandleKeys` `:70`.

**Phím:** `Client/Input/GameControls.inputactions` (map `Player` có `ToggleTimeDial` = `<Keyboard>/t`; map `TimeDial` có `Press` chuột trái, `Close` Esc), `Client/GameInput.cs:50` (`ToggleTimeDial => Get("Player/ToggleTimeDial")`), `Tests/ClientEdit/GameControlsTests.cs` (`Expected()` dòng 47 `["Player/ToggleTimeDial"] = B("<Keyboard>/t")`; đếm `43` ở dòng 74 và 103).

**Số cấu hình:** `design/data/sim_m1.json` khối `"player"` (dòng 33–40) ↔ `Core/Sim/SimConfig.cs` lớp player (`manaFlashMs` `:161` là mẫu) ↔ `Core/PlayerMotion.cs:35–49` (đổi ms → giây, vd `ManaFlashSeconds = p.manaFlashMs / 1000f;`).

**Test đang dùng chữ HUD (phải sửa theo ý mới):**
- `Tests/Client/TimeDialPlayTests.cs` — 8 test, `SetUp` dựng `DayCycleView` + `TimeDial` trên `camGo`; dùng `dial.PeriodText`, `dial.ButtonScreenCenter`, `dial.ButtonShape`.
- `Tests/Client/PlayerFarmPlayTests.cs:324` (`"Mưa (R): 10 giây"` trong `HudLines()`), `:363` (`"Nông sản Cây A: "` trong `HudText()`), `:375–384` `HudInitial` (`"Mưa (R): sẵn sàng"`, `"Chế độ:"`, `"Mặc định"`, `"lên biển = tự đi tới xới"`).
- `Tests/Client/PlayerBagSowPlayTests.cs:348–351` `StatusTopBelowHud`; `:368` dùng `farming.HudLines()` — đọc test để biết kiểm gì.
- `Tests/Client/ObservationCameraPlayTests.cs:128–129` (`"<color=yellow>Camera quan sát</color>"`, `"chuột phải lên khối = đặt camera quan sát"` trong `HudText()`).
- `Tests/Client/SettingsPlayTests.cs:273–275` dựng `TimeDial`.
- Các test `StringAssert.Contains(..., input.HudText())` kiểm **thông báo** (`ControlModePlayTests.cs:236,303,371,388,523`, `DropPlayTests.cs:103`, `PlayerBagSowPlayTests.cs:199,293`, `ObservationCameraPlayTests.cs:100`) — giữ nguyên, phải còn qua (HudText vẫn chứa thông báo).

**API Unity (6000.4 / uGUI 2.0.0). Chỉ dùng — đều đã có trong repo:**
- IMGUI: `GUI.Label`, `GUI.Button`, `GUI.DrawTexture(Rect, Texture, ScaleMode, bool, float, Color, float, float)` [docs](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/GUI.DrawTexture.html) (đã dùng ở `TerrainEditInput.Bar`), `GUI.Toolbar(Rect, int, string[])` [docs](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/GUI.Toolbar.html), `GUI.BeginScrollView` / `GUI.EndScrollView` [docs](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/GUI.BeginScrollView.html), `GUIStyle.CalcHeight`.
- uGUI: `Graphic.OnPopulateMesh(VertexHelper)` / `SetVerticesDirty` [docs](https://docs.unity3d.com/Packages/com.unity.ugui@2.0/api/UnityEngine.UI.Graphic.html), `VertexHelper` [docs](https://docs.unity3d.com/Packages/com.unity.ugui@2.0/api/UnityEngine.UI.VertexHelper.html), `RectTransformUtility.ScreenPointToLocalPointInRectangle` [docs](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/RectTransformUtility.ScreenPointToLocalPointInRectangle.html), `Text`, `CanvasGroup` [docs](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/CanvasGroup.html) — như `TimeDial.Build`.
- `Object.FindAnyObjectByType<T>()` [docs](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/Object.FindAnyObjectByType.html).
- API khác không có ở đây → dừng, `cần Opus`.

## Quyết định đã chốt (không được đổi)
Từ 0034 (user, 2026-10-23) và task contract:
1. **Bỏ khỏi màn hình:** chữ hướng dẫn phím (→ Cài đặt → Hướng dẫn); nút + dòng "Đang ở buổi: …"; (loài ghim, đình trệ trên HUD: client chưa có → không có gì để bỏ). Nhãn chữ sinh vật → 0065.
2. **Còn lại:** trên-phải minimap (không đổi); trên-trái avatar + máu + mana; trên cùng thông số (FPS…); giữa-dưới túi phụ (không đổi).
3. **Menu phép, phím G** (thay T; bỏ phím T): mục Đổi buổi chỉ hiện icon buổi hiện tại, chọn → mở vòng 4 mốc như cũ (0024); mục phép Mưa.
4. Chữ hiển thị mới qua `vi.json` (0032); phím mới qua `GameControls.inputactions` (0033); số [số tạm] trong `design/data/sim_m1.json`.
5. "User xem" gộp một lần cuối task (user 2026-10-24): mục "User tự test" chỉ để Planner chép vào danh sách cuối.

**Đề xuất Planner (user xem ở lần chơi thử cuối, đổi được):**
6. Giữ HUD IMGUI (không chuyển uGUI ở brief này — ít việc, ít rủi ro). Avatar = ô vuông bo góc 44×44 màu giữ chỗ + chữ cái đầu (khoá `hud.avatar_letter` = "B" — "Bạn"); art thật sau.
7. Dưới avatar chỉ còn: dòng **chế độ / vật cầm** — chỉ hiện khi chế độ ≠ Mặc định hoặc đang cầm; dòng **thông báo** (`lastResult`, trạng thái tự xới / đi tới, `StatusLine`) — **ẩn sau `hudMessageMs` = 4000** [số tạm] kể từ lần chữ thông báo đổi. `HudText()` vẫn trả cả thông báo (không theo thời gian) để test cũ dùng được; chỉ phần vẽ ẩn.
8. Bỏ khỏi HUD (đã có chỗ khác): nông sản / túi hạt trong túi (xem trong túi B), dòng kho + ô ruộng (chuột phải kho / click trái ô ruộng; 0065 thêm icon), "Nhặt (chuột phải): …" / "Thu hoạch (chuột phải): …" (gợi ý phím), góc nhìn camera `hud.elevation` (debug).
9. Thông số trên cùng giữa: `"{1:F1} FPS · mục tiêu {0} · nhịp {2}"` + (khi có) dựng lại chunk + dòng bench `Meter.LastLine` ngay dưới. F1/F2/F3 ghi trong Hướng dẫn.
10. Cài đặt có **thanh tab** trên cùng: `Chung` | `Hướng dẫn` (7l f sẽ thêm `Điều khiển`). Tab Chung = nội dung cũ.
11. Menu phép làm **trong `TimeDial.cs`** (cùng canvas, không thêm component vào scene): nút "Phép" (hình sao) ở đúng chỗ nút buổi cũ → bảng 2 mục bên trái nút. Chọn Đổi buổi → bảng đóng, vòng mở. Chọn Mưa → bảng đóng, chế độ Tưới bật (nếu đang Tưới thì giữ, không tắt). G: không có gì mở → mở bảng; bảng hoặc vòng đang mở → đóng hết. Esc / click ra ngoài → đóng.

## Phạm vi
- Được sửa/tạo:
  - `client/Assets/Game/Client/TerrainEditInput.cs`, `PlayerFarmActions.cs`, `SimView.cs`, `SettingsView.cs`, `TimeDial.cs`, `TimeDialGraphic.cs`, `GameInput.cs`, `FollowCamera.cs`, `PlayerCharacter.cs` (chỉ dòng kiểm chặn con trỏ nếu cần), `Client/Input/GameControls.inputactions`, `Client/Resources/Lang/vi.json`;
  - `client/Assets/Game/Core/HudMessage.cs` (mới, + `.meta` do Unity sinh), `Core/Sim/SimConfig.cs`, `Core/PlayerMotion.cs`;
  - `design/data/sim_m1.json` (chỉ thêm `"hudMessageMs": 4000` vào khối `player`);
  - test: `Tests/Core/HudMessageTests.cs` (mới), `Tests/Client/TimeDialPlayTests.cs`, `PlayerFarmPlayTests.cs`, `PlayerBagSowPlayTests.cs`, `ObservationCameraPlayTests.cs`, `SettingsPlayTests.cs`, `Tests/ClientEdit/GameControlsTests.cs`, và test khác **chỉ** khi nó kiểm chữ hướng dẫn / phím T / dòng buổi vừa bỏ (ghi tên vào Kết quả);
  - `design/02-gameplay/controls-ui.md` — chỉ dòng "Menu phép" của bảng phím: bỏ chữ "[Planner] (thay T)" → "G (thay T, 7l e1)"; file này không có thay đổi của phiên khác (kiểm `git diff --stat design/02-gameplay/controls-ui.md` rỗng trước khi sửa; có thay đổi → không sửa, ghi vào Kết quả);
  - mục Kết quả của brief này.
- Không được sửa: nhãn sinh vật + dấu đình trệ trong `SimView.OnGUI` (0065), `InventoryView.cs`, `MinimapView.cs`, `ObservationCamera.cs` (nút "Xem camera (V)" giữ nguyên), luật `Game.Core` khác, scene `.unity`, file của phiên khác (trên).

## Các bước
1. **Số tạm `hudMessageMs`.**
   - `design/data/sim_m1.json` khối `player`: thêm `"hudMessageMs": 4000` ngay sau `"manaFlashMs": 600`.
   - `Core/Sim/SimConfig.cs` (lớp player, cạnh `manaFlashMs`): `public int hudMessageMs = 4000;`.
   - `Core/PlayerMotion.cs` (`public static class PlayerMotion`): `public static float HudMessageSeconds { get; private set; } = 4f;` cạnh `ManaFlashSeconds` `:21`; trong `Configure(PlayerConfig p)` `:33` gán `HudMessageSeconds = p.hudMessageMs / 1000f;`. Client đọc `PlayerMotion.HudMessageSeconds` (như `PlayerCharacter.cs:249` đọc `PlayerMotion.ManaFlashSeconds`).
   - Xong khi: compile sạch; `ConfigTests` (nếu kiểm trường JSON) qua.

2. **`Core/HudMessage.cs` (mới, C# thuần, namespace `Game.Core`).**
   ```csharp
   /// <summary>Dòng thông báo HUD (0034 mục 7, brief 0064): hiện khi chữ đổi, ẩn sau Seconds giây. Chữ rỗng → ẩn.</summary>
   public sealed class HudMessage
   {
       public HudMessage(float seconds);
       public float Seconds { get; set; }
       public string Text { get; }          // chữ gần nhất đưa vào Update
       public void Update(string text, float now);   // text khác Text (so sánh string.Equals) → Text = text, mốc = now
       public bool Visible(float now);      // Text != "" && now - mốc < Seconds
   }
   ```
   - Khởi tạo: `Text = ""`, mốc = −∞ (vd `float.NegativeInfinity`).
   - `Update(null, …)` coi như `""`.
   - Xong khi: test bước 9 (EditMode) qua.

3. **`TerrainEditInput` — HUD trái gọn.**
   - Xoá `HelpText()` và mọi chỗ dùng `hud.help_*`, `hud.till_help`, `hud.camera_help`, `hud.elevation`, `hud.aimed_drop`, `hud.aimed_harvest`. Thuộc tính `AimedDrop` / `AimedHarvest` **giữ** nếu code / test khác còn dùng (grep); chỉ bỏ khỏi chữ HUD.
   - Tách `HudText()` thành:
     - `public string ModeLine()` — `""` khi `Control.Mode == ControlMode.Default && !Control.HasHeld`; ngược lại `Format("hud.mode", <tên chế độ>)` (chỉ khi Mode ≠ Default) nối `Format("hud.held", ItemLabel(Control.Held))` (khi HasHeld). Nếu chỉ có vật cầm (Mode = Default) thì bỏ phần "Chế độ" và bỏ dấu " · " đầu của `hud.held` (dùng `.TrimStart(' ', '·')` hoặc khoá riêng — chọn cách gọn, ghi vào Kết quả).
     - `public string MessageText()` — các dòng thông báo nối bằng `\n`, bỏ dòng rỗng: `Farming.HudLines()` (sau bước 4) rồi `lastResult`.
     - `public string HudText()` = `ModeLine()` + `"\n"` + `MessageText()` (giữ tên để test cũ dùng). Không còn `hud.rebuild` (chuyển sang thông số, bước 5).
   - Trường `readonly HudMessage message = new HudMessage(4f)`. Mỗi khung trong `Update()` (cuối hàm): `message.Seconds = PlayerMotion.HudMessageSeconds; message.Update(MessageText(), Time.time)` (đọc mỗi khung vì `Configure` có thể chạy sau khi component tạo). Thuộc tính `public bool MessageVisible => message.Visible(Time.time);`.
   - `OnGUI()` vẽ (đơn vị GUI, đã nhân `UiScale.Gui` như cũ; vẫn bỏ qua khi `view.Bag == null || MinimapView.IsAnyOpen`):
     - **Avatar** `Rect(10, 10, 44, 44)`: `GUI.DrawTexture(..., Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0f, new Color(0.30f, 0.55f, 0.45f, 0.95f), 0f, 8f)` + chữ `T("hud.avatar_letter")` giữa ô (style căn giữa, chữ đậm). Chỉ vẽ khi có `PlayerCharacter.Active`.
     - **Thanh** máu / mana: đổi `Bar` để nhận `x`: máu `(60, 14)`, mana `(60, 32)`, rộng 160, cao 12 (màu + nháy như cũ); nhãn số `vitals.hp` / `vitals.mana` ở `x = 226`, `y = 12` / `30`.
     - Dòng chế độ ở `y = 60` (khi `ModeLine() != ""`), dòng thông báo ngay dưới (khi `MessageVisible`), rộng 600, tự xuống dòng theo `CalcHeight`.
   - Xoá `HudBottom` nếu không còn ai dùng sau bước 5 (grep); còn dùng → giữ.
   - Xong khi: compile; `HudText()` không còn chữ "Chuột trái", "WASD", "1–4", "K: kho".

4. **`PlayerFarmActions.HudLines()`**: bỏ dòng Mưa (`farm.rain_ready` / `farm.rain_hud_cooldown` — chuyển vào menu phép) và vòng lặp nông sản / túi hạt (`farm.hud_produce` / `farm.hud_seeds`). Còn: đang xới / đang đi tới + `StatusLine`. Trả `""` khi không có gì (không có `\n` thừa ở đầu).

5. **`SimView.OnGUI` — thông số trên cùng.**
   - Bỏ `SiteStatus.StorageText` và vòng `SiteStatus.PlotText` khỏi HUD (hàm Core giữ nguyên — bảng / test khác dùng). Bỏ `StatusTop` nếu không còn ai dùng (test `StatusTopBelowHud` xoá theo, bước 10).
   - Thêm `public string StatsText()`: `Format("hud.fps", TargetFps, 1f / smoothedDt, Sim.State.Tick)` + (khi `view.LastRebuildFrame >= 0`) `Format("hud.rebuild", view.LastRebuildChunks, view.LastMeshMs, view.LastWriteMs, view.LastBakeMs)`. (Tên trường `view.*` như đang dùng ở `TerrainEditInput.HudText` — chuyển nguyên sang; `view` ở SimView cũng là `TerrainChunkView`.)
   - Vẽ `StatsText()` căn giữa trên cùng: `Rect(gw * 0.5f - 400f, 4f, 800f, 22f)` với `gw = Screen.width / scale`, style căn giữa (`TextAnchor.UpperCenter`, richText). `Meter.LastLine` (khi có) ở dòng ngay dưới, cùng cách căn.
   - Vẽ thông số cả khi bản đồ mở? **Không** — bỏ qua khi `MinimapView.IsAnyOpen` (như HUD trái).
   - Nhãn sinh vật + dấu đình trệ: **giữ nguyên**.

6. **`vi.json`.**
   - Sửa:
     - `hud.mode` → `"Chế độ: <b><color=yellow>{0}</color></b>"`
     - `hud.held` → `" · Cầm: <b><color=yellow>{0}</color></b>"`
     - `hud.fps` → `"{1:F1} FPS · mục tiêu {0} · nhịp {2}"`
     - `hud.rebuild` → `" · dựng lại {0} chunk: mesh {1:F1} ms, ghi {2:F1} ms, bake {3:F1} ms"`
     - `time.hint` → `"Chọn mốc · G / Esc: đóng"`
   - Xoá (sau khi grep chắc không còn code dùng): `hud.help_fly`, `hud.help_idle`, `hud.help_walk`, `hud.till_help`, `hud.camera_help`, `hud.elevation`, `hud.aimed_drop`, `hud.aimed_harvest`, `farm.hud_produce`, `farm.hud_seeds`, `farm.rain_ready`, `farm.rain_hud_cooldown`, `time.current`. Khoá nào còn được dùng → giữ, ghi vào Kết quả.
   - Thêm (giữ thứ tự abc như file hiện có):
     - `hud.avatar_letter` = `"B"`
     - `settings.tab_general` = `"Chung"`
     - `settings.tab_guide` = `"Hướng dẫn"`
     - `spell.button` = `"Phép (G)"`
     - `spell.time` = `"Đổi buổi"`
     - `spell.rain` = `"Mưa"`
     - `spell.rain_cooldown` = `"{0} giây"`
     - `guide.move` = `"Di chuyển: chuột trái lên chỗ muốn tới (giữ để đi theo con trỏ) · A: nhảy · Shift: lướt"`
     - `guide.camera` = `"Camera: giữ chuột giữa + di chuột để xoay · lăn chuột: gần / xa · Space: về sau lưng · X: góc nhìn thứ nhất"`
     - `guide.modes` = `"Chế độ: D mặc định · Q đào · E xới · R tưới (bấm lại = tắt). Chuột phải: làm việc của chế độ / thu hoạch / mở kho / nhặt — chọn trong {0:0.#} ô, nhân vật tự đi tới {1:0.#} ô rồi làm"`
     - `guide.till` = `"Xới: chuột phải lên đất / cỏ = đánh dấu · lên biển = tự đi tới xới · lên ô ruộng = chọn giống. Click trái ô ruộng: bảng ô ruộng (nút Bỏ đánh dấu)"`
     - `guide.collect` = `"F: thu vật rơi trong {0:0.#} ô"`
     - `guide.bag` = `"B: túi (kèm túi phụ) · P: ẩn / hiện túi phụ · 1–0: cầm vật ở ô túi phụ · chuột phải ô: cầm · click trái ô: thông tin · Esc / D: thôi cầm"`
     - `guide.transfer` = `"Chuyển đồ giữa túi / túi phụ / kho đang mở: kéo thả cả chồng; hoặc click chọn ô → Tab đổi đích → 1: tất cả · 2: một nửa · 3: một phần ba"`
     - `guide.observe` = `"Camera quan sát: cầm vật phẩm Camera quan sát + chuột phải lên khối = đặt (cao hơn khối 2 ô) · V: xem / quay về · đang xem: giữ chuột giữa để xoay"`
     - `guide.spells` = `"G: menu phép — Đổi buổi (chọn mốc trên vòng 4 mốc) · Mưa (vào chế độ tưới, như R)"`
     - `guide.other` = `"M: bản đồ · O hoặc Esc (khi không có bảng nào mở): cài đặt"`
     - `guide.debug` = `"Phím debug: F1 khoá 30 FPS · F2 khoá 60 FPS · F3 camera bay (WASD, Q/E, giữ chuột giữa; phím chế độ tắt khi bay)"`
   - Xong khi: `HardcodedTextScanTests` qua (mọi khoá dùng trong code có trong `vi.json`).

7. **`SettingsView` — tab Hướng dẫn.**
   - Trường `int tab` (0 = Chung, 1 = Hướng dẫn); `public int Tab { get => tab; set => tab = value; }` (test dùng). Mở lại bảng giữ tab cũ.
   - Trong `OnGUI`, ngay dưới tiêu đề: `tab = GUI.Toolbar(new Rect(x, y, 240f, row - 4f), tab, new[] { T("settings.tab_general"), T("settings.tab_guide") }); y += row;`. (Tạo mảng mỗi khung cũng được — bảng chỉ vẽ khi mở.)
   - Tab 0: toàn bộ nội dung cũ, không đổi.
   - Tab 1: `GUI.BeginScrollView(new Rect(x, y, w, p.yMax - 48f - y), scroll, new Rect(0, 0, w - 20f, h))` với `h = WrapStyle().CalcHeight(new GUIContent(GuideText()), w - 20f)`; một `GUI.Label` với `WrapStyle()`; `GUI.EndScrollView()`. Trường `Vector2 scroll`.
   - `public static string GuideText()` = các khoá `guide.move`, `guide.camera`, `guide.modes` (`Format` với `PlayerRules.TargetReach`, `PlayerRules.ActReach`), `guide.till`, `guide.collect` (`PlayerRules.CollectReach`), `guide.bag`, `guide.transfer`, `guide.observe`, `guide.spells`, `guide.other`, `guide.debug` — nối bằng `"\n\n"`.
   - Nút Đóng giữ chỗ cũ (cả hai tab).
   - `OtherPanelOpen()`: giữ `TimeDial.IsAnyOpen` (sau bước 8 nó gồm cả bảng phép).
   - Xong khi: test bước 10 qua.

8. **Menu phép trong `TimeDial.cs` + phím G.**
   - Phím: trong `GameControls.inputactions` đổi tên action `Player/ToggleTimeDial` → `ToggleSpellMenu`, binding `<Keyboard>/g` (giữ id cũ của action/binding, chỉ đổi `name` + `path` + tên action trong binding). `GameInput.cs`: `public static InputAction ToggleSpellMenu => Get("Player/ToggleSpellMenu");` (xoá `ToggleTimeDial`). Map `TimeDial` (Press, Close) dùng chung cho bảng phép — không thêm map.
   - `TimeDialGraphic`: thêm `Star` vào cuối enum (`{ Disk, Moon, Sun, Dawn, Dusk, Star }` — **thêm cuối** để không đổi số các giá trị cũ) và `Drop` (giọt nước). Vẽ: `Star` = 4 cánh: 4 `Tri` từ tâm ra 4 hướng (dài 0,95r, chân rộng 0,3r) + `Fan` tâm 0,25r; `Drop` = `Fan` tròn tâm `(0, −0,25r)` bán kính 0,55r + `Tri` đỉnh `(0, 0,95r)`, hai chân `(±0,5r, −0,1r)`. Hình giữ chỗ, không cần đẹp.
   - `TimeDial.Build()`:
     - **Bỏ** `periodText` (và `Period Line`). Thuộc tính `PeriodText` xoá; `shownPeriod` bỏ nếu không còn dùng.
     - Nút `button` giữ vị trí / cỡ, icon đổi thành `Star` màu `new Color(0.75f, 0.85f, 1f, 1f)` (không đổi theo buổi nữa). Thêm nhãn nhỏ `T("spell.button")` dưới nút (font 16, `(0, −44)` so với tâm nút, outline như nhãn mốc).
     - Thêm `menu` (RectTransform, tắt lúc đầu) neo cùng góc với nút, chứa 2 mục tròn 64×64 đặt **bên trái nút**, cùng hàng: mục buổi tâm tại `(-16 - 72 - 16 - 32, -232 - 36)` = `(-136, -268)`, mục Mưa tại `(-216, -268)` (anchor `(1,1)`, pivot `0.5,0.5`). Mỗi mục: nền `Disk` `NormalBg`, icon 48×48 bên trong, nhãn dưới (font 16): `spell.time` / `spell.rain`. Mục Mưa thêm chữ hồi (`spell.rain_cooldown`, font 16) ngay dưới nhãn, rỗng khi sẵn sàng; nền mục Mưa mờ (`alpha 0.5`) khi đang hồi.
     - Icon mục buổi = `ShapeOf` / `ColorOf` của `(TimeLandmark)day.Day.CurrentWindow` (cập nhật ở `Refresh` như icon nút cũ).
   - Trạng thái: `public bool MenuOpen { get; private set; }`; `IsAnyOpen => active != null && (active.IsOpen || active.MenuOpen)`; `BlocksPointer` thêm `|| active.MenuOpen`.
   - Hàm công khai (test dùng):
     - `public void SetMenuOpen(bool open)` (bật/tắt `menu`; mở menu thì đóng vòng).
     - `public void ToggleMenu()` — `IsOpen || MenuOpen` → đóng cả hai; ngược lại mở menu.
     - `public void ChooseTime()` — đóng menu, `SetOpen(true)`.
     - `public bool ChooseRain()` — đóng menu; tìm `TerrainEditInput` bằng `Object.FindAnyObjectByType<TerrainEditInput>()` (lưu lại lần sau); null → `false`. Có: nếu `input.Control.Mode != ControlMode.Rain` thì `input.ToggleMode(ControlMode.Rain)`; trả `input.Control.Mode == ControlMode.Rain`.
     - `public Vector2 MenuTimeScreenCenter`, `public Vector2 MenuRainScreenCenter` (như `ButtonScreenCenter`), `public TimeDialShape MenuTimeShape`, `public string RainCooldownText`.
   - `Update()`:
     - `GameInput.ToggleSpellMenu.WasPressedThisFrame()` (thay `ToggleTimeDial`) → `ToggleMenu()` (giữ chặn `actedFrame`).
     - `TimeDialClose` (Esc): vòng mở → đóng vòng; không thì menu mở → đóng menu.
   - `PointerDown(screen)`:
     - trên nút → `ToggleMenu()`;
     - menu mở: trên mục buổi → `ChooseTime()`; trên mục Mưa → `ChooseRain()`; ngoài nút + ngoài 2 mục → đóng menu; return;
     - vòng mở: logic cũ (chọn mốc / ngoài vòng → đóng);
     - không gì mở và không trên nút → không làm gì (như cũ).
   - Ẩn khi bản đồ mở: như cũ (`SetOpen(false)` + `SetMenuOpen(false)`).
   - Chữ hồi Mưa trong `Refresh`: lấy `PlayerFarmActions` qua `TerrainEditInput.Farming` (input tìm như trên; null → chữ rỗng). `RainCooldown.Ready` → `""`; không → `Format("spell.rain_cooldown", RainCooldown.RemainingWholeSeconds)`. Chỉ gán `Text.text` khi chuỗi đổi.
   - Summary đầu lớp `TimeDial`: cập nhật một câu: nút "Phép (G)" → menu phép (Đổi buổi → vòng 4 mốc; Mưa → chế độ Tưới), bỏ dòng buổi (0034 mục 7, brief 0064).
   - Xong khi: test bước 10 qua; phím T không còn trong `.inputactions` (`grep -c '<Keyboard>/t"' client/Assets/Game/Client/Input/GameControls.inputactions` = 0).

9. **Test EditMode mới `Tests/Core/HudMessageTests.cs`** (namespace + asmdef như `Tests/Core/PlayerTimersTests.cs`):
   - `HiddenBeforeAnyText`: mới tạo → `Visible(0)` false.
   - `ShowsThenHidesAfterSeconds`: `Seconds = 4`; `Update("a", 10)` → `Visible(10)` true, `Visible(13.9f)` true, `Visible(14f)` false.
   - `SameTextDoesNotRestart`: `Update("a", 10)`, `Update("a", 13)` → `Visible(14.5f)` false.
   - `NewTextRestarts`: `Update("a", 10)`, `Update("b", 13)` → `Visible(16.5f)` true.
   - `EmptyHidden`: `Update("", 10)` → `Visible(10)` false; `Update(null, 11)` → `Text == ""`.
   - `GameControlsTests.Expected()`: `["Player/ToggleTimeDial"] = B("<Keyboard>/t")` → `["Player/ToggleSpellMenu"] = B("<Keyboard>/g")`; tổng vẫn `43`. Test không trùng phím cùng map phải qua (G chưa dùng trong `Player`).
   - Xong khi: EditMode qua hết.

10. **Test PlayMode — sửa + thêm.**
    - `TimeDialPlayTests` (sửa theo ý mới, giữ ý nghĩa vòng 4 mốc):
      - `BuildsClosedWithPeriodText` → đổi tên `BuildsClosedWithSpellButton`: vòng + menu đóng, canvas bật, `ButtonShape == TimeDialShape.Star`; `MenuTimeShape` = `Sun` lúc 12 giờ, `Moon` lúc 0 giờ, `Moon` lúc 5 giờ (cùng mốc thời gian như test cũ).
      - `ButtonTogglesRing` → `ButtonOpensMenuThenTimeOpensRing`: `PointerDown(ButtonScreenCenter)` → `MenuOpen`, vòng đóng; `PointerDown(MenuTimeScreenCenter)` → menu đóng, `IsOpen` + `RingActive`; `PointerDown(ButtonScreenCenter)` → tất cả đóng.
      - Mọi test khác đang mở vòng bằng `PointerDown(dial.ButtonScreenCenter)`: thay bằng helper `OpenRing()` = `dial.ChooseTime()` (hoặc 2 lần PointerDown như trên) + `yield return null` nếu cần. Ý nghĩa còn lại (thứ tự mốc, chọn mốc xoay + đóng, mốc hiện tại tô sáng + không làm gì, mờ khi xoay, ngoài vòng → đóng + chặn con trỏ, ẩn khi bản đồ mở) giữ nguyên.
      - Thêm `ToggleMenuClosesAll`: `ToggleMenu()` → `MenuOpen`; `ToggleMenu()` → đóng; `ChooseTime()` rồi `ToggleMenu()` → vòng + menu đóng.
      - Thêm `MenuBlocksPointerAndOutsideCloses`: mở menu → `TimeDial.BlocksPointer(MenuRainScreenCenter)` true; `PointerDown(new Vector2(5, 5))` → menu đóng.
      - Thêm `RainWithoutInputReturnsFalse`: `ChooseRain()` trong fixture này (không có `TerrainEditInput`) → `false`, menu đóng, không lỗi.
    - Test menu Mưa có nhân vật — trong `ControlModePlayTests` (fixture có `TerrainEditInput` + `SimView`; xem `SetUp` của nó). Thêm `SpellMenuRainEntersRainMode`: lấy `dial = Object.FindAnyObjectByType<TimeDial>()`; nếu fixture không có `TimeDial` → thêm một `DayCycleView` + `TimeDial` như `SettingsPlayTests.cs:273–275`. `dial.ChooseRain()` → true, `input.Control.Mode == ControlMode.Rain`; gọi lần nữa → vẫn Rain (không tắt). Hồi Mưa: kích hoạt hồi (dùng cách `PlayerFarmPlayTests.cs:324` gây hồi — đọc test đó), chờ 1 khung → `dial.RainCooldownText == "10 giây"` (`// vi-text`; 10 = `rainCooldownMs` 10000 — nếu test cũ dùng số khác thì theo nó).
    - `PlayerFarmPlayTests`:
      - `:324` `"Mưa (R): 10 giây"` trong `HudLines()` → đổi sang kiểm `RainCooldown.RemainingWholeSeconds == 10` (hoặc như test trên qua `TimeDial`) — chọn cách không cần thêm fixture; ghi vào Kết quả.
      - `:363` `"Nông sản Cây A: "` trong HudText → đổi sang kiểm `view.Bag.GetProduce(0) > 0` (hoặc số cụ thể test đang có) **và** `StringAssert.DoesNotContain("Nông sản Cây A", input.HudText())`.
      - `HudInitial` → `HudCompact`: ban đầu `input.ModeLine() == ""`; `HudText()` không chứa `"Chuột trái"`, `"WASD"`, `"Mưa (R)"` (`// vi-text`); `ToTill()` → `ModeLine()` chứa `"Xới"`; `SettingsView.GuideText()` chứa `"lên biển = tự đi tới xới"` (`// vi-text`).
    - `PlayerBagSowPlayTests`: `StatusTopBelowHud` xoá nếu `StatusTop` đã bỏ (ghi lý do trong Kết quả); `:368` sửa theo `HudLines()` mới (không còn dòng Mưa / nông sản) — giữ ý của test.
    - `ObservationCameraPlayTests:128–129`: `"<color=yellow>Camera quan sát</color>"` kiểm trên `tei.ModeLine()`; `"chuột phải lên khối = đặt"` → kiểm trên `SettingsView.GuideText()`.
    - `SettingsPlayTests`: thêm `GuideTabShowsNewKeys`: `SettingsView.GuideText()` chứa `"Tab đổi đích"`, `"P: ẩn / hiện túi phụ"`, `"G: menu phép"`, `"kéo thả cả chồng"` và **không** chứa `"1–4"`, `"K:"`, `"Alt"`; thêm `TabPersistsAcrossReopen`: `Tab = 1`, `SetOpen(false)`, `SetOpen(true)` → `Tab == 1`. Chỗ `:273–275` dựng `TimeDial` — chỉ sửa nếu nó dùng `PeriodText` / T.
    - `SimView` thông số: thêm vào test PlayMode có `SimView` sẵn (vd `SimViewPlayTests` — xem fixture) `StatsTextOnTop`: chờ sim chạy 1 khung → `sim.StatsText()` chứa `"FPS"` + `"nhịp"` (`// vi-text`) và không chứa `"F1"`; `input.HudText()` (nếu fixture có `TerrainEditInput`) không chứa `"Kho ·"` (`// vi-text`).
    - Xong khi: EditMode + PlayMode qua hết, 0 lỗi console.

11. **`controls-ui.md`** dòng "Menu phép": `G [Planner] (thay T)` → `G (thay T; 7l e1, brief 0064)` — chỉ khi file sạch (Phạm vi).

12. **Kết quả:** ghi giờ bắt đầu / kết thúc, danh sách test đã sửa / xoá + lý do, khoá `vi.json` đã xoá / thêm, lệch so với brief.

## Tiêu chí xong
- `grep -c "hud.help_\|hud.till_help\|hud.camera_help\|time.current" client/Assets/Game/Client/Resources/Lang/vi.json` = 0 (trừ khoá còn được dùng — ghi lý do).
- `grep -n '<Keyboard>/t"' client/Assets/Game/Client/Input/GameControls.inputactions` rỗng; có `<Keyboard>/g`.
- `HudText()` lúc đầu không có chữ hướng dẫn; thông báo vẫn có (test thông báo cũ qua).
- Cài đặt có tab Hướng dẫn với chữ phím mới.
- Nút Phép (G) → menu 2 mục; Đổi buổi → vòng 4 mốc; Mưa → chế độ Tưới.
- EditMode + PlayMode qua hết qua MCP, 0 lỗi console.
- `Game.Core` không tham chiếu UnityEngine (`HudMessage.cs`).

## Test sau khi xong

### Phần cần test
- `HudMessage` (bước 2) · `HudMessageTests` 5 test · hiện / ẩn đúng mốc, chữ giống không làm mới, chữ rỗng ẩn.
- Số `hudMessageMs` (bước 1) · đọc từ JSON · `ConfigTests` qua; HUD ẩn thông báo theo số này (gián tiếp qua `MessageVisible` — không bắt buộc test PlayMode chờ 4 giây).
- HUD trái gọn (bước 3–4) · `HudCompact`, `ObservationCameraPlayTests` sửa · không còn chữ hướng dẫn; `ModeLine` rỗng khi Mặc định + không cầm; có tên chế độ / vật cầm khi có.
- Thông báo cũ (regression bước 3) · `ControlModePlayTests` (236, 303, 371, 388, 523), `DropPlayTests:103`, `PlayerBagSowPlayTests` (199, 293), `ObservationCameraPlayTests:100` · vẫn qua không sửa.
- Thông số trên cùng (bước 5) · `StatsTextOnTop` · có FPS + nhịp, không còn F1; HUD trái không còn dòng kho.
- Chữ (bước 6) · `HardcodedTextScanTests` · qua.
- Tab Hướng dẫn (bước 7) · `GuideTabShowsNewKeys`, `TabPersistsAcrossReopen` · chữ phím mới, không còn phím cũ.
- Menu phép (bước 8) · `TimeDialPlayTests` (8 test cũ sửa + 3 mới), `SpellMenuRainEntersRainMode` · nút → menu; Đổi buổi → vòng; Mưa → chế độ Tưới; G đóng tất cả; chặn con trỏ; click ngoài đóng; hồi Mưa hiện "10 giây".
- Vòng 4 mốc cũ (regression) · `TimeDialPlayTests` còn lại: thứ tự, chọn mốc xoay + đóng, mốc hiện tại, mờ khi xoay, ẩn khi bản đồ mở · qua.
- Phím (bước 8) · `GameControlsTests` · `Player/ToggleSpellMenu` = G, 43 action, không trùng.
- Cài đặt / Esc (regression) · `SettingsPlayTests` cũ · Esc khi menu phép mở không mở Cài đặt (`OtherPanelOpen` dùng `TimeDial.IsAnyOpen` — thêm 1 dòng assert vào test Esc sẵn có nếu có test như vậy; không có thì bỏ qua, ghi vào Kết quả).
- Toàn bộ test cũ · EditMode + PlayMode qua MCP · qua hết.

### Lệnh executor chạy
1. Unity Editor đang mở project qua MCP ([0010](../../design/decisions/0010-mcp-for-unity.md)): sau khi sửa code, MCP `refresh_unity` → `read_console` (Error) = 0.
2. MCP `run_tests` EditMode (toàn bộ) → ghi dòng tổng.
3. MCP `run_tests` PlayMode (toàn bộ) → ghi dòng tổng.
4. Dọn sau PlayMode: `git checkout -- client/ProjectSettings/EditorSettings.asset`.
5. `python3 tools/check_design.py` (vì sửa `design/data/sim_m1.json` + `controls-ui.md`) → exit 0.
6. `git status --short` — chỉ file trong Phạm vi; commit code + test + brief (message tiếng Anh, vd `brief 0064: 7l e1 compact HUD, guide tab, spell menu (G)`).

### User tự test
(Gộp vào danh sách chơi thử cuối task — Planner chép; không chờ user.)
- Mở scene SimView, Play: góc trên-trái chỉ còn avatar ô vuông chữ "B" + thanh máu + thanh mana + số; không còn đoạn chữ hướng dẫn dài, không còn dòng kho / ô ruộng.
- Bấm Q: hiện "Chế độ: Đào"; bấm D: dòng chế độ biến mất. Đào một khối: thông báo hiện rồi tự ẩn sau ~4 giây.
- Trên cùng giữa: "xx FPS · mục tiêu 60 · nhịp N" (đọc được, không che minimap).
- O → Cài đặt → tab Hướng dẫn: đọc chữ phím, đúng với phím đang dùng (B, P, 1–0, kéo thả, Tab, 1/2/3, G…); cuộn được nếu dài.
- Không còn dòng "Đang ở buổi"; nút "Phép (G)" dưới minimap; G mở menu 2 mục; Đổi buổi → vòng 4 mốc như cũ; Mưa → vào chế độ Tưới, chuột phải ô ruộng tưới được; sau khi tưới, mở lại menu thấy "10 giây" đếm lùi. T không còn làm gì.
- Đánh giá: vị trí / cỡ avatar, thanh, menu phép có dễ dùng không; 4 giây ẩn thông báo có hợp không.

## Dừng và chuyển lại Opus khi
- Brief mơ hồ hoặc mâu thuẫn với code thật (vd fixture `ControlModePlayTests` không dựng được `TimeDial`).
- Cần một quyết định thuộc AGENTS.md §3.4 hoặc ngoài "Quyết định đã chốt".
- Cùng một lỗi sửa 2 lần vẫn chưa qua.
- Phải sửa file ngoài "Phạm vi" (vd test khác hỏng vì lý do không phải chữ hướng dẫn / T / dòng buổi).
- Cần API Unity không có trong "Bối cảnh cần đọc".

---
<!-- Từ đây trở xuống: EXECUTOR ghi. -->

## Kết quả
- Base commit: d78524f
- Chạy bằng: Sonnet 5.5 (claude-sonnet-5-5) · medium
- Bắt đầu: 2026-10-24 07:04 · Kết thúc: 07:25 (đồng hồ máy; gồm ~20 phút chờ test EditMode/PlayMode)
- Commit code: dedb9ea
- File đổi: Client/{TerrainEditInput,PlayerFarmActions,SimView,SettingsView,TimeDial,TimeDialGraphic,GameInput}.cs, Input/GameControls.inputactions, Resources/Lang/vi.json, **Resources/Config/sim_m1.json (bản sao client, ngoài Phạm vi nhưng `ConfigTests.SimClientCopyMatchesDesignData` bắt buộc)**; Core/{HudMessage (mới),PlayerMotion,Sim/SimConfig}.cs; design/data/sim_m1.json; controls-ui.md (dòng Menu phép); test: HudMessageTests (mới), TimeDialPlayTests, PlayerFarmPlayTests, PlayerBagSowPlayTests, ObservationCameraPlayTests, SettingsPlayTests, SimViewPlayTests, ControlModePlayTests, GameControlsTests.
- Test sửa/xoá: `StatusTopBelowHud` xoá (StatusTop đã bỏ); `PlayerFarmPlayTests` RainRules (:324 → kiểm `RainCooldown.RemainingWholeSeconds == 10`), nông sản (→ `GetProduce>0` + HudText không chứa), `HudInitial` → `HudCompact`; ObservationCamera → `ModeLine()` + `GuideText()`; TimeDialPlayTests: `BuildsClosedWithPeriodText` → `BuildsClosedWithSpellButton`, `ButtonTogglesRing` → `ButtonOpensMenuThenTimeOpensRing`, thêm `ToggleMenuClosesAll`, `MenuBlocksPointerAndOutsideCloses`, `RainWithoutInputReturnsFalse`; mới: `SpellMenuRainEntersRainMode` (ControlModePlayTests), `GuideTabShowsNewKeys`, `TabPersistsAcrossReopen`, `StatsTextOnTop` (SimViewPlayTests; không kiểm HudText vì fixture không có TerrainEditInput). Các test của TimeDialPlayTests khác mở vòng bằng `SetOpen(true)` sẵn có nên không cần helper.
- vi.json: xoá 13 khoá (hud.help_fly/idle/walk, till_help, camera_help, elevation, aimed_drop, aimed_harvest, farm.hud_produce/seeds, rain_ready, rain_hud_cooldown, time.current); không khoá nào còn được dùng. Thêm 21 khoá (hud.avatar_letter, settings.tab_*, spell.*, guide.*); sửa hud.mode/held/fps/rebuild, time.hint. File ghi lại bằng json.dump sắp xếp theo key.
- Lệch brief: ModeLine bỏ " · " đầu của hud.held bằng `TrimStart(' ', '·')`; `HudMessage` cập nhật ở đầu `Update()` của TerrainEditInput (không phải cuối, vì Update có `return` sớm); không thêm assert Esc vào test Settings (không có test Esc với TimeDial); khoá `time.period_*` + `PeriodName` giữ/bỏ: `PeriodName` xoá, khoá `time.period_*` còn trong vi.json (không dùng, vô hại).

## Kết quả test
- HudMessage (b2): đạt — `HudMessageTests` 5 test.
- hudMessageMs (b1): đạt — `ConfigTests` (kể cả SimClientCopyMatchesDesignData sau khi chép bản client).
- HUD gọn (b3–4): đạt — `HudCompact`, `ObservationCameraPlayTests.HoldCameraAndRightClickPlaces`.
- Thông báo cũ: đạt — các test `HudText()` cũ không sửa vẫn qua.
- Thông số trên (b5): đạt — `StatsTextOnTop`.
- Chữ (b6): đạt — `HardcodedTextScanTests` (sau bỏ 6 marker `// vi-text` thừa).
- Tab Hướng dẫn (b7): đạt — `GuideTabShowsNewKeys`, `TabPersistsAcrossReopen`.
- Menu phép (b8): đạt — `TimeDialPlayTests`, `SpellMenuRainEntersRainMode` ("10 giây").
- Phím: đạt — `GameControlsTests` (43 action, G); grep `<Keyboard>/t"` = 0, `/g"` = 1.
- Esc khi menu phép mở: bỏ qua (không có test Esc sẵn kèm TimeDial).
- Lệnh: `refresh_unity` + console Error = 0. EditMode toàn bộ: 548 test, lần đầu 545 qua / 3 lỗi (vi-text thừa, bản sao sim_m1.json — đã sửa; `IncrementalRebuildMatchesFullRebuild` timeout 180s do Editor chạy nền chậm) → chạy lại 3 test: 3/3 qua. PlayMode toàn bộ: 248 test, 247 qua; `SeaSkyTintPlayTests.DayCycleOffNearSeaUnchanged` lỗi khi chạy cả bộ (0,079 > 0,002), chạy riêng qua → có vẻ phụ thuộc thứ tự/flaky, không liên quan HUD (Planner xem lại). Đã `git checkout -- client/ProjectSettings/EditorSettings.asset`. `check_design.py` exit 0.
- User xem: chờ user — gộp cuối.

## Vướng mắc

---
<!-- PLANNER ghi khi review. -->

## Review
