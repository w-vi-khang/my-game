# Brief 0059 — 7l c2: cầm vật phẩm từ túi (khối / hạt / camera quan sát), bỏ Tab tạm, vòng tầm khi chuột phải ngoài tầm, F hiện vùng 2 ô + gió hút

> Trạng thái: chờ review
> Review: đầy đủ + user xem
> Model thực thi: Sonnet · medium
> Cập nhật: 2026-10-24 (Planner trả lời vướng mắc vòng 2: giữ thứ tự kiểm, sửa test; cho sửa DropPlayTests.cs)

<!-- Planner (Opus) viết, Executor (Sonnet) làm theo ở PHIÊN KHÁC — executor không có lịch sử chat, không được tự quyết. -->

## Mục tiêu
Bước **7l c2** của task [m1-world-7l-dieu-khien-pc](../tasks/m1-world-7l-dieu-khien-pc.md). Luật: [0034 mục 3–4](../../design/decisions/0034-dieu-khien-pc-click-di-chuyen.md#4-cầm-vật-phẩm), [0036 mục 5–7](../../design/decisions/0036-sinh-vat-chi-lam-cong-doan-duoc-giao.md).
1. **Cầm vật phẩm từ túi:** B → bấm dòng vật phẩm trong bảng túi → túi đóng, nhân vật cầm vật đó. Ba loại:
   - **khối** — bản mờ ở ô đặt được, **bản mờ đỏ** khi chỗ đó không đặt được, hết khối → tự thôi cầm;
   - **túi hạt** — thay hẳn "chế độ gieo" cũ; chỉ gieo tay được ô ruộng có **hàng đợi trống**;
   - **camera quan sát** — dòng cố định "Camera quan sát × 1" trong bảng túi, không mất khi dùng.
2. **Xoá Tab vòng tạm** của c1 (brief 0058): `CycleTempHold`, action `Player/CycleTool`.
3. **Vòng tầm thao tác** chỉ hiện khi **bấm chuột phải** mà ô đích ngoài tầm 10 ô, rồi mờ dần trong 1 giây. Không còn hiện khi chỉ di chuột.
4. **F Thu:** hiện **vòng vùng 2 ô** quanh nhân vật + **vệt gió** xoáy vào nhân vật (0,5 giây), kể cả khi không có vật rơi. Vật rơi bay về nhân vật 0,3 giây (hiện 0,2 giây, số cứng).
5. Ba số thời gian `[số tạm]` (1 s / 0,5 s / 0,3 s) đặt trong JSON `player` (2 bản), không viết cứng.

Bắt đầu: kiểm brief 0058 có `> Trạng thái: đã duyệt` và commit `680697c` có trong `git log`. Không có → dừng, `cần Opus`.

**File lạ chưa track** `client/Assets/InitTestScene*.unity(.meta)` (scene tạm của Unity Test Runner): không commit, không xoá khi Editor đang chạy test.

## Bối cảnh cần đọc
Đường dẫn dưới đây tính từ `client/Assets/Game/` trừ khi ghi khác.

**Game.Core:**
- `Core/ControlState.cs` (72 dòng, **không sửa**):
  - `ControlState.Hold(ItemId item)` `:20` (Mode = Default, cầm item);
  - `ToggleMode` `:18` (luôn bỏ vật cầm);
  - `ClearToDefault()` `:22`.
  - `RightClick.Resolve` `:51–70`: ngoài tầm → `OutOfReach`; cầm vật **khác khối** + `HeldUsable` → `UseHeld` (ưu tiên 1); chế độ; `RipeCrop` → Harvest; Storage; Drop; cầm khối + `HeldUsable` → `PlaceBlock`; còn lại `None`.
- `Core/DropField.cs:6–47`:
  - `enum ItemKind : byte { Block = 0, SeedBag = 1, Produce = 2, Tool = 3 }`, `enum ToolItem : byte { ObservationCamera = 0 }`;
  - `ItemId.Block(BlockMaterial m)`, `ItemId.SeedBag(int crop)`, `ItemId.Tool(ToolItem t)`, thuộc tính `Kind`, `Material`, `Crop`.
  - `CollectInReach` `:175–189` gọi `Take` → phát `Collected` → `DropView.OnCollected` cho chồng bay.
- `Core/PlayerBag.cs`:
  - `Get(BlockMaterial)` `:23`, `GetSeeds(int crop)` `:43`;
  - `Add(ItemId, int)` `:99–108` — Tool ném `NotSupportedException` → camera quan sát **không** nằm trong `PlayerBag`.
- `Core/Sim/SimData.cs`:
  - `:68` `public enum PlayerSowResult : byte { Ok = 0, NoPlot = 1, NotTilled = 2, HasCrop = 3, OtherCrop = 4, NoSeeds = 5 }`;
  - `FarmPlot.Slots` `:238` = các ô đợi, **không gồm vụ hiện tại** (chú thích `:237`, quyết định 0027).
- `Core/Sim/Simulation.cs:1000–1014` `CheckPlayerSow(int plotId, int cropId, PlayerBag bag)`, `:1016–1031` `PlayerSow`. `EnqueueCrop` `:888–897` (ô đứng yên → `StartCrop` lấy 1 vụ khỏi `Slots`).
- `Core/PlayerMotion.cs:9–37`: thuộc tính tĩnh đọc từ `PlayerConfig` (mẫu `ClickMarkSeconds` `:13`, `:32`).
- `Core/Sim/SimConfig.cs`:
  - `PlayerConfig` `:128–162` (field `clickMarkMs` `:157`);
  - kiểm biên `:400–401` (`clickMarkMs` trong [50, 5000]).
- JSON `design/data/sim_m1.json` khối `"player"`, dòng có `"clickMarkMs": 500, ` (dòng cuối của khối). Bản sao `client/Assets/Game/Client/Resources/Config/sim_m1.json` phải giống hệt (`ConfigTests` kiểm).

**Client (đọc hết các đoạn này):**
- `Client/TerrainEditInput.cs` (565 dòng) — file sửa nhiều nhất:
  - `:37–38` `MissingColor`;
  - `:81–146` `Update` (Esc `:100–104`, Tab `:99`);
  - `:161–169` `ToggleMode` (gọi `Farming.ExitSowMode()` `:166`);
  - `:171–181` `CycleTempHold`;
  - `:183–188` `SelectMaterial`;
  - `:204–210` `HeldUsableAt(Vector3Int placeCell)`;
  - `:220–237` `Preview` (dòng gieo cũ `:235`);
  - `:240–260` `RightClickAt`;
  - `:262–286` `Execute(..., bool camera, BlockMaterial material)`;
  - `:327–333` `HideAim`;
  - `:336–369` `ShowAim` (vòng tầm `:341–342`, bản mờ hết khối `:360–368`);
  - `:397–405` `CollectNearby`;
  - `:420–426` `ItemLabel`;
  - `:449–457` `HelpText`;
  - `:460–486` `HudText` (dòng `hud.held` `:464`).
- `Client/PlayerFarmActions.cs` (310 dòng):
  - `:21–22` `SowCrop`;
  - `:114–130` `EnterSowMode`;
  - `:132–138` `ExitSowMode`;
  - `:140–176` `SowAt(Vector3Int cell)`;
  - `:286–291` `HudLines` (dòng gieo `:289–290`).
- `Client/SimView.cs`:
  - `:364–365` `BagRow`;
  - `:374–393` `BagRows`;
  - `:395–402` `ChooseSeedFromBag`;
  - `:522–540` vẽ bảng túi (nút dòng `:531–537`, `bag.empty` `:529`, `bag.hint` `:539`).
- `Client/MoveMarkers.cs` (118 dòng, đọc hết): vòng tầm `reach` `:16–17`, `:35–37`; `Child` `:45–51`, `Circle` `:53–62`, `Make` `:64–82`, `SetColor` `:84–88`, `LateUpdate` `:100–116` (mẫu mờ dần của dấu click `:102–112`).
- `Client/DropView.cs`: `FlySeconds` `:19`, dùng ở `:101`; summary `:10`.
- `Client/SettingsView.cs:62–68` `OtherPanelOpen` (dòng `SowCrop` `:67`). `SettingsView` có `[DefaultExecutionOrder(-50)]` `:8` → chạy trước `TerrainEditInput.Update`, nên vẫn thấy vật cầm trước khi Esc bỏ cầm.
- `Client/GameInput.cs:18` `CycleTool`. `Client/Input/GameControls.inputactions`: action `CycleTool` (khoảng dòng 89–97) + binding `<Keyboard>/tab` có `"action": "CycleTool"` (khoảng dòng 352–362).
- `Client/Resources/Lang/vi.json` + `loc/en.json` + `loc/context.json` (gốc repo; bản dịch tiếng Anh chuẩn + ngữ cảnh, xem bước 10).
- Test: `Tests/ClientEdit/GameControlsTests.cs` (`Expected()` dòng `["Player/CycleTool"]` `:33`; số `36` ở 2 chỗ); test liệt kê ở bước 11–12.

API Unity dùng (6000.4). Không dùng API nào khác:
- `LineRenderer` ([docs](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/LineRenderer.html)) — đã dùng ở `MoveMarkers.Make`.
- `LineRenderer.SetPosition(int index, Vector3 position)` ([docs](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/LineRenderer.SetPosition.html)).
- `LineRenderer.startColor` / `endColor` ([docs](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/LineRenderer-startColor.html), [docs](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/LineRenderer-endColor.html)) — qua `SetColor` có sẵn.
- `Renderer.enabled` ([docs](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/Renderer-enabled.html)).
- `Time.deltaTime` ([docs](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/Time-deltaTime.html)).
- `GUI.Button` ([docs](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/GUI.Button.html)) — đã dùng ở bảng túi.

Hiệu ứng gió dùng `LineRenderer`, không dùng `ParticleSystem` (0036 mục 6 cho phép một trong hai; LineRenderer đã có mẫu trong `MoveMarkers`, test đọc trạng thái được).

## Quyết định đã chốt (không được đổi)
1. **Nguồn cầm** (task contract, user 2026-10-23): trước 7l d, cầm qua **bảng túi cũ** (B → bấm dòng).
   - Dòng khối, túi hạt, camera quan sát bấm được. Dòng nông sản chỉ xem.
   - Bấm cầm được → đóng bảng túi.
   - Phím 1–4 / Alt + lăn giữ như c1:
     - chọn loại khối;
     - đang cầm khối → cầm loại mới, nếu túi còn loại đó;
     - túi hết loại đó → không đổi gì, dòng kết quả `hold.none`;
     - không cầm gì → chỉ đổi loại đang chọn, **không** tự cầm.
   - Scene không có SimView (không có bảng túi) → người chơi không cầm được bằng tay. Chấp nhận tới 7l d (túi ô vuông). API `HoldItem` vẫn dùng được từ code / test.
2. **Camera quan sát là vật phẩm cố định** (0034 mục 4): không lưu trong `PlayerBag` (`Add(Tool)` ném lỗi, giữ nguyên). Bảng túi luôn có dòng cuối "Camera quan sát × 1"; dùng không mất.
3. **Hàng đợi trống** (0034 mục 4, 0036 mục 2–3):
   - "hàng đợi" = `FarmPlot.Slots` (không gồm vụ hiện tại, 0027);
   - `Slots.Count > 0` → không gieo tay được, kết quả mới `PlayerSowResult.QueueNotEmpty = 6`;
   - ô đang ở pha Sow (vụ hiện tại lấy từ hàng đợi) mà `Slots` trống → vẫn gieo được, cùng loại: luật "chen vào ô đã giao", 0036 mục 3. `OtherCrop` giữ nguyên.
   - Thứ tự kiểm trong `CheckPlayerSow`: `NoPlot` → `OtherCrop` → `HasCrop` → **`QueueNotEmpty`** → `NotTilled` → `NoSeeds`.
4. **Hết vật cầm → tự thôi cầm** (0034 mục 4):
   - khối: túi còn 0 loại đó; túi hạt: còn 0 hạt loại đó → `Control.ClearToDefault()`, dòng kết quả nối thêm `hold.empty`;
   - camera không bao giờ hết.
   - Kiểm ngay sau khi làm PlaceBlock / UseHeld, và mỗi frame đầu `Update` (bắt cả trường hợp chuyển đồ sang kho).
   - Không còn bản mờ đỏ "hết khối" (`MissingColor` → đổi tên `InvalidColor`, cùng giá trị `(1, 0.2, 0.2, 0.5)`).
5. **Bản mờ đỏ khi sai chỗ** (0034 mục 4): cầm khối, `Preview` = `None`, có ô, (không có nhân vật hoặc `InTargetReach(digCell)`), `CheckPlace(placeCell, Held.Material) != EditResult.Ok` → bản mờ màu `InvalidColor` ở `placeCell`. `PlaceBlock` → bản mờ màu vật liệu như cũ. `OutOfReach` → không bản mờ (0034 mục 3 "không tô khối").
6. **Cầm hạt:** `HeldUsable` = có Sim, `digCell` là ô ruộng, `CheckPlayerSow(id, crop, bag) == Ok`. Chuột phải mà `Preview` = `None` khi đang cầm hạt và có ô → gọi `Farming.SowAt(digCell, crop)` ngay để dòng trạng thái báo lý do (không đi, không gieo vì đã biết không Ok).
7. **Bỏ chế độ gieo cũ hẳn:** xoá `SowCrop`, `EnterSowMode`, `ExitSowMode`, dòng HUD `farm.hud_sow`, `ChooseSeedFromBag`. Esc: đang cầm → `ClearToDefault()` (như c1, bỏ nhánh gieo). `SettingsView.OtherPanelOpen` dùng `Control.HasHeld` thay `SowCrop`.
8. **Cầm vật mới** (`HoldItem`): huỷ tự xới + huỷ đi tới (dừng nhân vật) như Tab tạm c1; no-op (false) khi đang xem camera quan sát.
9. **Vòng tầm** (0036 mục 5):
   - chỉ bật khi `RightClickAt` trả `OutOfReach` (mọi chế độ, có hay không cầm — 0036 không giới hạn);
   - alpha bắt đầu 0,25 (như c1), giảm tuyến tính về 0 trong `PlayerMotion.ReachRingFadeSeconds` rồi tắt;
   - chuột phải ngoài tầm lần nữa → bắt đầu lại;
   - `ShowAim` / `HideAim` **không** đụng vòng tầm nữa.
10. **F Thu** (0036 mục 6):
    - `CollectNearby()` có nhân vật → luôn gọi `Markers.ShowCollect()`, kể cả 0 chồng (đề xuất Planner, user đồng ý 2026-10-24).
    - Hiệu ứng tạm, art sau:
      - vòng bán kính `PlayerRules.CollectReach`, 48 điểm, cao 0,03, nét 0,06, màu `(0.75, 0.95, 1, 0.5)`;
      - 8 vệt gió, mỗi vệt `LineRenderer` 2 điểm, nét 0,05, cao 0,5, cùng màu;
      - alpha của vòng + vệt = `0.5 × (1 − t)`, t = thời gian đã qua / `CollectShowSeconds`; t ≥ 1 → tắt hết.
    - Vệt i (0..7), k = t:
      - góc `a = i × 45° + 22,5° + 90° × k` (xoáy);
      - đầu ngoài cách tâm `r1 = CollectReach × (1 − k) + 0,3 × k`;
      - đầu trong `r0 = max(0,2, r1 − 0,6)`;
      - điểm = `(cos a × r, 0, sin a × r)`.
    - Vật rơi bay `PlayerMotion.CollectFlySeconds` (thay hằng `DropView.FlySeconds`). Áp dụng cho mọi chồng bị nhặt, cả nhặt bằng chuột phải.
11. **Tham số `[số tạm]`, chưa chốt** (0036 mục 7, AGENTS.local "Tham số chưa chốt"):
    - thêm vào `player` của JSON: `reachRingFadeMs` 1000, `collectShowMs` 500, `collectFlyMs` 300;
    - mỗi số kiểm biên [50, 5000] như `clickMarkMs`;
    - không bump `configVersion` (c1 cũng không bump khi đổi key).
12. **Input:** xoá action `Player/CycleTool` + binding Tab. Tổng hành động 36 → **35**. Tab được dùng lại ở 7l d (đổi đích chuyển đồ), không làm ở đây.
13. Không làm: mana (c3), túi ô vuông / túi phụ / phím 1–0 (7l d), luật sinh vật chỉ làm công đoạn được giao (sim 7g), nút Tiếp tục.
14. Chuỗi người chơi đọc + bản tiếng Anh + ngữ cảnh: chép nguyên văn ở bước 10.
15. **`ObservationCamera.cs`** (Planner trả lời vướng mắc 2026-10-24, phương án A): chỉ xoá dòng `farming.ExitSowMode();` trong `Open` (khối `if (farming != null)` còn `farming.CancelSelfTill();`). Không sửa gì khác trong file này.

16. **Thứ tự kiểm giữ như quyết định 3** (Planner trả lời vướng mắc vòng 2, 2026-10-24, phương án A): ô có hàng đợi (`Slots`) = đã giao cho sinh vật → báo `QueueNotEmpty` cả khi chưa xới; báo `NotTilled` trước sẽ dẫn người chơi xới xong vẫn không gieo được. Sửa test `SowRejections` theo bước 13, không đổi `CheckPlayerSow`.
17. **Chờ vật rơi bay xong trong test** = `PlayerMotion.CollectFlySeconds + 0.2f` (không chờ đúng bằng thời gian bay).

## Phạm vi
- Được sửa:
  - Core:
    - `client/Assets/Game/Core/Sim/SimData.cs` (chỉ enum `PlayerSowResult`);
    - `Core/Sim/Simulation.cs` (chỉ `CheckPlayerSow`);
    - `Core/Sim/SimConfig.cs` (chỉ 3 field + 3 kiểm biên);
    - `Core/PlayerMotion.cs`.
  - JSON: `design/data/sim_m1.json`, `client/Assets/Game/Client/Resources/Config/sim_m1.json` (chỉ thêm 3 key).
  - Client: `Client/TerrainEditInput.cs`, `Client/PlayerFarmActions.cs`, `Client/SimView.cs` (chỉ phần túi), `Client/MoveMarkers.cs`, `Client/DropView.cs`, `Client/SettingsView.cs` (chỉ `OtherPanelOpen`), `Client/GameInput.cs`, `Client/Input/GameControls.inputactions`, `Client/ObservationCamera.cs` (chỉ xoá dòng `farming.ExitSowMode();`, quyết định 15).
  - Chuỗi: `Client/Resources/Lang/vi.json`, `loc/en.json`, `loc/context.json`.
  - Test: các file ở bước 11–12; `Tests/Client/DropPlayTests.cs` (chỉ thời gian chờ, bước 13).
  - File brief này (mục Kết quả / Kết quả test / Vướng mắc).
- Không được sửa: `Core/ControlState.cs`, `Core/PlayerBag.cs`, `Client/WalkToAct.cs`, `ObservationCamera.cs` (ngoài dòng ở quyết định 15), `PlayerCharacter.cs`, `design/` ngoài `design/data/sim_m1.json`, task contract, mọi file khác. Không sửa `GameControlsTests.cs` ngoài `Expected()` và hai số đếm.

## Các bước
1. **Core — gieo tay chỉ khi hàng đợi trống:**
   - `SimData.cs:68` → `public enum PlayerSowResult : byte { Ok = 0, NoPlot = 1, NotTilled = 2, HasCrop = 3, OtherCrop = 4, NoSeeds = 5, QueueNotEmpty = 6 }`.
   - `Simulation.CheckPlayerSow`: ngay sau dòng `return PlayerSowResult.HasCrop;` (và trước kiểm `NotTilled`) thêm:
     ```csharp
     if (p.Slots.Count > 0) return PlayerSowResult.QueueNotEmpty; // gieo tay chỉ ô hàng đợi trống (0034 mục 4)
     ```
   - Summary của `CheckPlayerSow`: thêm câu "Ô còn hạt trong hàng đợi (Slots) → QueueNotEmpty (0034 mục 4)."
   - Xong khi: biên dịch không lỗi.
2. **Core — tham số thời gian:**
   - `SimConfig.cs` `PlayerConfig`: sau `public int clickMarkMs = 500;` thêm
     ```csharp
     public int reachRingFadeMs = 1000;
     public int collectShowMs = 500;
     public int collectFlyMs = 300;
     ```
   - Kiểm biên: sau 2 dòng `clickMarkMs` (`:400–401`) thêm 3 cặp cùng dạng, thông báo `"player.reachRingFadeMs must be in [50, 5000]."`, `"player.collectShowMs must be in [50, 5000]."`, `"player.collectFlyMs must be in [50, 5000]."`.
   - `PlayerMotion.cs`:
     - thêm 3 thuộc tính sau `ClickMarkSeconds`: `ReachRingFadeSeconds` (= 1f), `CollectShowSeconds` (= 0.5f), `CollectFlySeconds` (= 0.3f), dạng `public static float X { get; private set; } = …;`;
     - mỗi cái một dòng `/// <summary>` ngắn: "Vòng tầm mờ hết sau chừng này giây (0036 mục 5, số tạm).", "F Thu: vòng vùng + gió hiện chừng này giây (0036 mục 6, số tạm).", "Vật rơi bay về nhân vật chừng này giây (0036 mục 6, số tạm).";
     - trong `Configure`: `ReachRingFadeSeconds = p.reachRingFadeMs / 1000f;` (và 2 dòng tương tự).
   - JSON 2 bản: thay đúng chuỗi `"clickMarkMs": 500, ` bằng `"clickMarkMs": 500, "reachRingFadeMs": 1000, "collectShowMs": 500, "collectFlyMs": 300, `. Hai file vẫn giống hệt.
   - Xong khi: `diff design/data/sim_m1.json client/Assets/Game/Client/Resources/Config/sim_m1.json` không ra gì.
3. **MoveMarkers.cs — vòng tầm mờ dần + hiệu ứng F:**
   - Xoá `ShowReachRing(bool)`. Thêm:
     ```csharp
     /// <summary>Alpha hiện tại của vòng tầm (0 khi tắt).</summary>
     public float ReachRingAlpha { get; private set; }
     /// <summary>Chuột phải ngoài tầm thao tác (0036 mục 5): bật vòng tầm, alpha 0,25 rồi mờ hết trong PlayerMotion.ReachRingFadeSeconds.</summary>
     public void FlashReachRing()
     public bool CollectVisible => collect.enabled;
     public float CollectAlpha { get; private set; }
     /// <summary>Số vệt gió đang bật (0 hoặc WindStreaks).</summary>
     public int WindVisibleCount { get; private set; }
     public const int WindStreaks = 8;
     /// <summary>F Thu (0036 mục 6): vòng vùng bán kính PlayerRules.CollectReach + 8 vệt gió xoáy vào, mờ hết trong PlayerMotion.CollectShowSeconds. Gọi lại → bắt đầu lại.</summary>
     public void ShowCollect()
     ```
   - Field: `LineRenderer collect; LineRenderer[] wind; float reachAge, collectAge;`. Hằng màu `static readonly Color CollectColor = new Color(0.75f, 0.95f, 1f, 0.5f);`.
   - `Awake`, sau dựng `reach`:
     ```csharp
     collect = Make(Child("CollectRing"), Circle(48, PlayerRules.CollectReach), true, 0.06f);
     collect.enabled = false;
     wind = new LineRenderer[WindStreaks];
     for (int i = 0; i < WindStreaks; i++)
     {
         var w = Child("Wind" + i);
         w.transform.localPosition = new Vector3(0f, 0.5f, 0f);
         wind[i] = Make(w, new[] { Vector3.zero, Vector3.zero }, false, 0.05f);
         wind[i].enabled = false;
     }
     ```
   - `FlashReachRing`: `reachAge = 0f; ReachRingAlpha = 0.25f; SetColor(reach, new Color(1f, 1f, 1f, 0.25f)); reach.enabled = true;`.
   - `ShowCollect`: `collectAge = 0f;` bật `collect` + mọi `wind[i]`, gọi `UpdateCollect(0f)`.
   - `void UpdateCollect(float k)` (private, k = 0..1):
     - `CollectAlpha = 0.5f * (1f - k)`; màu = `CollectColor` với alpha đó cho `collect` và mọi vệt;
     - mỗi vệt i: tính `a`, `r1`, `r0` theo quyết định 10 (độ → radian bằng `Mathf.Deg2Rad`);
     - `wind[i].SetPosition(0, new Vector3(Mathf.Cos(a) * r1, 0f, Mathf.Sin(a) * r1))`, `SetPosition(1, …r0)`;
     - `WindVisibleCount = WindStreaks`.
   - `LateUpdate`, đầu hàm, thêm:
     ```csharp
     if (reach.enabled)
     {
         reachAge += Time.deltaTime;
         float k = reachAge / PlayerMotion.ReachRingFadeSeconds;
         if (k >= 1f) { reach.enabled = false; ReachRingAlpha = 0f; }
         else { ReachRingAlpha = 0.25f * (1f - k); SetColor(reach, new Color(1f, 1f, 1f, ReachRingAlpha)); }
     }
     if (collect.enabled)
     {
         collectAge += Time.deltaTime;
         float k = collectAge / PlayerMotion.CollectShowSeconds;
         if (k >= 1f)
         {
             collect.enabled = false;
             foreach (var w in wind) w.enabled = false;
             CollectAlpha = 0f;
             WindVisibleCount = 0;
         }
         else UpdateCollect(k);
     }
     ```
   - Summary lớp: "vòng tầm thao tác (0034 mục 3)" → "vòng tầm thao tác khi chuột phải ngoài tầm, mờ dần (0036 mục 5) + vòng vùng Thu + vệt gió khi bấm F (0036 mục 6)".
   - Xong khi: biên dịch không lỗi; `grep -rn "ShowReachRing" client/Assets/Game` chỉ còn ở test (sửa ở bước 12).
4. **DropView.cs:** xoá `public const float FlySeconds = 0.2f;`; `:101` dùng `PlayerMotion.CollectFlySeconds`; summary `:10` "0,2 giây" → "PlayerMotion.CollectFlySeconds (0036 mục 6)".
5. **PlayerFarmActions.cs — bỏ chế độ gieo:**
   - Xoá `SowCrop` (+ summary), `EnterSowMode`, `ExitSowMode`, 2 dòng HUD gieo `:289–290`.
   - Cùng lúc: `ObservationCamera.cs:151` xoá dòng `farming.ExitSowMode();` (quyết định 15).
   - `SowAt` đổi chữ ký + summary:
     ```csharp
     /// <summary>Gieo 1 hạt túi loại crop vào ô ruộng có Soil = cell (cầm túi hạt, 0034 mục 4). Ghi StatusLine (lý do khi không gieo được). true khi đã gieo.</summary>
     public bool SowAt(Vector3Int cell, int crop)
     ```
   - Thân hàm: như cũ nhưng thay `SowCrop` bằng `crop`; đầu hàm `if (crop < 0 || sim == null || view.Bag == null) return false;`.
     - Nhánh `Ok`: chỉ `StatusLine = Format("farm.sow_ok", cropName, cell); return true;` (bỏ khối "hết hạt").
     - Nhánh `NoSeeds`: bỏ `SowCrop = -1;`.
     - Thêm `case PlayerSowResult.QueueNotEmpty: StatusLine = T("farm.sow_queue"); break;`.
   - Summary lớp `:11` "chế độ gieo từ túi hạt (brief 0042)" → "gieo khi cầm túi hạt (0034 mục 4, brief 0059)".
   - Xong khi: `grep -n "SowCrop\|SowMode" client/Assets/Game/Client` không còn kết quả.
6. **TerrainEditInput.cs:**
   - **Xoá:** `CycleTempHold`; dòng Tab `:99`; `Farming.ExitSowMode();` trong `ToggleMode`; dòng gieo cũ trong `Preview` `:235`.
   - **Đổi tên** `MissingColor` → `InvalidColor`, summary "Màu bản mờ khi chỗ con trỏ không đặt được khối đang cầm (0034 mục 4)."
   - **Thêm API:**
     ```csharp
     /// <summary>Cầm vật phẩm (0034 mục 4): khối / túi hạt (túi còn ≥ 1; hạt cần có Sim) hoặc camera quan sát. Huỷ tự xới + đi tới (dừng nhân vật). Khối thuộc bảng 1–4 → đổi loại đang chọn theo. false (không đổi gì) khi đang xem camera quan sát, không có túi, hết, hoặc nông sản.</summary>
     public bool HoldItem(ItemId item)
     ```
     Thân:
     ```csharp
     if (Observation != null && Observation.Engaged) return false;
     if (!CanHold(item)) return false;
     Farming.CancelSelfTill();
     Walk.Cancel(PlayerCharacter.Active, true);
     if (item.Kind == ItemKind.Block) { int i = System.Array.IndexOf(Palette, item.Material); if (i >= 0) selected = i; }
     Control.Hold(item);
     return true;
     ```
     `bool CanHold(ItemId item)` (private):
     - `Tool` → `item.ToolKind == ToolItem.ObservationCamera`;
     - `Block` → `view.Bag != null && view.Bag.Get(item.Material) > 0`;
     - `SeedBag` → `Farm != null && view.Bag != null && view.Bag.GetSeeds(item.Crop) > 0`;
     - còn lại false.
   - **`DropHeldIfEmpty()`** (private): `Control.HasHeld` và `Control.Held.Kind` là Block / SeedBag mà `!CanHold(Control.Held)` → `var label = ItemLabel(Control.Held); Control.ClearToDefault(); lastResult += Format("hold.empty", label);`. Gọi ở:
     - (a) `Update` ngay sau dòng `if (Walk.Tick(player)) …`;
     - (b) cuối `Execute` cho `PlaceBlock` và `UseHeld`.
     - Với hạt, dòng `farm.sow_ok` nằm ở `Farming.StatusLine`, còn `hold.empty` nằm ở `lastResult` của `TerrainEditInput` → chấp nhận hai dòng riêng.
   - **`SelectMaterial(int i)`**:
     ```csharp
     if (Control.HasHeld && Control.Held.Kind == ItemKind.Block)
     {
         if (!HoldItem(ItemId.Block(Palette[i]))) lastResult = Format("hold.none", Name(Palette[i]));
         return;
     }
     selected = i;
     ```
   - **Esc** (`:100–104`): thành `if (GameInput.Back.WasPressedThisFrame() && Control.HasHeld) Control.ClearToDefault();`.
   - **`HeldUsableAt`** → `bool HeldUsableAt(Vector3Int digCell, Vector3Int placeCell)`; thêm nhánh:
     ```csharp
     if (Control.Held.Kind == ItemKind.SeedBag)
     {
         if (Farm == null || view.Bag == null) return false;
         int id = Farm.Sim.PlotAt(new CellPos(digCell.x, digCell.y, digCell.z));
         return id >= 0 && Farm.Sim.CheckPlayerSow(id, Control.Held.Crop, view.Bag) == PlayerSowResult.Ok;
     }
     ```
     `Preview` gọi `HeldUsableAt(digCell, placeCell)`.
   - **`RightClickAt`**:
     - ngay sau `var a = Preview(...)`:
       ```csharp
       var player = PlayerCharacter.Active;
       if (a == RightClickAction.OutOfReach && player != null) player.Markers.FlashReachRing();
       if (a == RightClickAction.None && hasCell && Control.HasHeld && Control.Held.Kind == ItemKind.SeedBag)
           Farming.SowAt(digCell, Control.Held.Crop); // chỉ để báo lý do (quyết định 6)
       ```
       (bỏ dòng `var player = …` cũ ở dưới);
     - thay `bool camera = …; var material = …;` bằng `var held = Control.HasHeld ? Control.Held : default;`;
     - truyền `held` vào `Execute`.
   - **`Execute(RightClickAction a, Vector3Int digCell, Vector3Int placeCell, DropStack drop, int storageId, ItemId held)`**:
     - PlaceBlock dùng `held.Material`;
     - UseHeld:
       ```csharp
       if (held.Kind == ItemKind.Tool) PlaceObservationCamera(digCell);
       else if (held.Kind == ItemKind.SeedBag) { lastResult = ""; Farming.SowAt(digCell, held.Crop); }
       ```
     - sau switch: `if (a == RightClickAction.PlaceBlock || a == RightClickAction.UseHeld) DropHeldIfEmpty();`.
   - **`HideAim`**: bỏ dòng vòng tầm (chỉ tắt lớp đỏ + bản mờ).
   - **`ShowAim`**:
     - bỏ 2 dòng `bool active …` / `ShowReachRing` (`:341–342`);
     - khối cuối (`:360–368`) đổi điều kiện `== EditResult.NoMaterial` thành `!= EditResult.Ok`, màu `InvalidColor`;
     - summary: bỏ "OutOfReach … → vòng tầm", "(đỏ khi túi hết)" → "bản mờ đỏ khi chỗ đó không đặt được".
   - **`CollectNearby`**: sau kiểm `player == null …` thêm `player.Markers.ShowCollect();` (trước khi Thu). Summary thêm "hiện vùng 2 ô + gió (0036 mục 6)".
   - **`ItemLabel`**: thêm đầu hàm `if (item.Kind == ItemKind.Tool) return T("hud.held_camera");`. Summary thêm "camera quan sát → Camera quan sát".
   - **`HudText`** dòng `hud.held`: `Format("hud.held", ItemLabel(Control.Held))`.
   - Summary lớp `:10`: "Tab cầm tạm khối / camera quan sát tới c2." → "World 7l c2 (brief 0059): cầm vật phẩm từ bảng túi (khối / túi hạt / camera quan sát, HoldItem), bản mờ đỏ khi sai chỗ, hết → thôi cầm; vòng tầm chỉ khi chuột phải ngoài tầm; F hiện vùng + gió."
   - Xong khi: biên dịch không lỗi; `grep -n "CycleTempHold\|CycleTool\|MissingColor\|SowCrop\|ShowReachRing" client/Assets/Game/Client` không còn kết quả.
7. **SimView.cs — bảng túi cầm vật:**
   - `BagRow` → `public struct BagRow { public string Label; public ItemId Item; public bool Holdable; }`. Summary: "Một dòng bảng túi (brief 0042, 0059). Holdable = bấm để cầm (khối, túi hạt, camera quan sát); nông sản chỉ xem."
   - `BagRows`: khối `Item = ItemId.Block(m), Holdable = true`; túi hạt `Item = ItemId.SeedBag(i), Holdable = true`; nông sản `Item = ItemId.Produce(i), Holdable = false`. Cuối hàm thêm dòng camera:
     ```csharp
     var cam = ItemId.Tool(ToolItem.ObservationCamera);
     rows.Add(new BagRow { Label = Format("bag.row_block", TerrainEditInput.ItemLabel(cam), 1), Item = cam, Holdable = true });
     ```
     Summary hàm thêm "…, cuối cùng camera quan sát (luôn có, 0034 mục 4)".
   - Thay `ChooseSeedFromBag` bằng:
     ```csharp
     /// <summary>Bấm dòng túi (0034 mục 4): cầm item, đóng bảng túi. false nếu không cầm được.</summary>
     public bool HoldFromBag(ItemId item)
     {
         var edit = GetComponent<TerrainEditInput>();
         if (edit == null || !edit.HoldItem(item)) return false;
         BagOpen = false;
         return true;
     }
     ```
   - Vẽ bảng: `if (bagRows[k].Holdable) { if (GUI.Button(rr, bagRows[k].Label)) { HoldFromBag(bagRows[k].Item); break; } }`. Dòng `bag.empty` (`:529`) xoá — luôn có ít nhất dòng camera.
   - Xong khi: `grep -n "SeedCrop\|ChooseSeedFromBag\|bag.empty" client/Assets/Game/Client/SimView.cs` không còn kết quả. Khoá `bag.empty` giữ trong vi.json (không xoá, có thể dùng lại ở 7l d).
8. **SettingsView.cs:67** → `return editInput != null && editInput.Control.HasHeld;`.
9. **Input:**
   - `GameControls.inputactions`: xoá object action `CycleTool` trong map `Player` và object binding có `"action": "CycleTool"`. Giữ đúng JSON (dấu phẩy).
   - `GameInput.cs:18`: xoá dòng `CycleTool`.
   - `GameControlsTests.cs`: `Expected()` xoá dòng `["Player/CycleTool"] = B("<Keyboard>/tab"),`; hai số `36` → `35`.
   - Xong khi: `grep -rn "CycleTool" client/Assets/Game` không còn kết quả; `python3 -c "import json;json.load(open('client/Assets/Game/Client/Input/GameControls.inputactions'))"` chạy được.
10. **Chuỗi** — `vi.json` (thứ tự khoá theo bảng chữ cái như file đang có), `loc/en.json` (cùng thứ tự), `loc/context.json` (object `key: ngữ cảnh`):
    - **Xoá** khỏi cả 3 file: `farm.hud_sow`, `farm.sow_exit`, `farm.sow_last`.
    - **Thêm** (cả 3 file; `{0}` là chỗ điền):

      | key | vi | en | context |
      |---|---|---|---|
      | `farm.sow_queue` | `Gieo: ô còn hạt trong hàng đợi — chỉ gieo tay ô có hàng đợi trống` | `Sow: this plot still has seeds queued — you can only hand-sow a plot with an empty queue` | `Message when the player, holding a seed bag, right-clicks a tilled plot whose seed queue (hàng đợi) is not empty: hand-sowing is only allowed on plots with an empty queue.` |
      | `hold.empty` | ` · hết {0}, thôi cầm` | ` · out of {0}, no longer holding it` | `Appended to the result line when the held item runs out (last block placed / last seed sown); the character stops holding it. {0} = item name (e.g. 'Đất' = 'Dirt', 'Túi hạt Cây A' = 'Crop A seed bag'). Starts with ' · ' (keep it).` |
      | `hold.none` | `Túi không còn {0}` | `No {0} left in the bag` | `Result line when the player presses 1–4 to switch the held block to a type the bag has none of. {0} = block name in lowercase (e.g. 'đá' = 'stone').` |

    - **Đổi text** (vi / en; context đổi theo):
      - `bag.hint`:
        - vi `Bấm vật phẩm để cầm · B / Esc: đóng`;
        - en `Click an item to hold it · B / Esc: close`;
        - context `Footer hint in the bag panel. Clicking an item row (block, seed bag, observation camera) makes the character hold it. 'B / Esc' are keyboard keys (keep them); 'đóng' = close the panel.`
      - `hud.held`:
        - vi ` · Cầm: <b><color=yellow>{0}</color></b> (Esc / D: thôi cầm)`;
        - en ` · Holding: <b><color=yellow>{0}</color></b> (Esc / D: put away)`;
        - context `Appended to the HUD mode line: the item currently held in hand. Starts with ' · '. Keep the rich-text tags. {0} = held item name. 'Esc' or 'D' puts the item away (stop holding).`
      - `hud.help_walk`: trong text hiện có, thay đúng đoạn ` · Tab: cầm tạm khối / camera quan sát · B: túi · ` bằng ` · B: túi (bấm vật phẩm để cầm) · `.
        - en: thay ` · Tab: temporarily hold a block / observation camera · B: bag · ` bằng ` · B: bag (click an item to hold it) · `.
        - context: thay `(A, Shift, F, Tab, B, 1–4, Alt, Space, X, F3)` bằng `(A, Shift, F, B, 1–4, Alt, Space, X, F3)`.
      - `hud.help_idle`: xoá đúng đoạn ` · Tab: cầm tạm khối / camera quan sát` (vi) và ` · Tab: temporarily hold a block / observation camera` (en).
        - context: `Mentions keys Tab, 1–4,` → `Mentions keys 1–4,`.
    - Xong khi:
      - `python3 -c "import json;[json.load(open(p)) for p in ['client/Assets/Game/Client/Resources/Lang/vi.json','loc/en.json','loc/context.json']]"` chạy được;
      - `python3 "<Plugin root>/skills/translation/scripts/loc_check.py" check loc/en.json` exit 0 (Plugin root ở "Trạng thái phiên");
      - `grep -rn "farm.hud_sow\|farm.sow_exit\|farm.sow_last" client/Assets/Game loc` không còn kết quả.
11. **Test mới / đổi trong `Tests/Client/ControlModePlayTests.cs`** (dùng `SetUp`, `Ahead(n)`, `WaitWalk()` có sẵn; tên đúng như dưới; chuỗi tiếng Việt trong assert ghi chú `// vi-text` như các test khác):
    - `ModeKeysToggleAndTabCycle` → **`ModeKeysAndHoldItem`**:
      - phần chế độ giữ nguyên;
      - `view.Bag.Add(BlockMaterial.Dirt, 2)` + `Add(BlockMaterial.Stone, 1)`;
      - `HoldItem(ItemId.Block(Dirt))` true → `Held.Material == Dirt`, `SelectedMaterial == Dirt`;
      - `SelectMaterial(2)` → `Held.Material == Stone`;
      - `SelectMaterial(3)` (Cát, túi 0) → vẫn Stone, `HudText()` chứa `Túi không còn cát`;
      - `HoldItem(ItemId.Tool(ToolItem.ObservationCamera))` true → `Held.Kind == Tool`;
      - `HoldItem(ItemId.Produce(0))` false;
      - `HoldItem(ItemId.Block(Sand))` false (túi 0), vật cầm không đổi;
      - `ToggleMode(Dig)` → không cầm, Mode Dig.
    - `OutOfTargetReachDoesNothingAndShowsRing` → **`OutOfReachRightClickFlashesRing`**:
      - ô cách 12, Dig mode: `ShowAim(Preview(...))` → `ReachRingVisible` **sai** (di chuột không hiện);
      - `RightClickAt` trả `OutOfReach`, `ClickMove == None`, khối còn, `ReachRingVisible` đúng, `player.Markers.ReachRingAlpha` trong (0,2; 0,25];
      - chờ 0,5 s → alpha < 0,2 và > 0;
      - chờ thêm tới tổng > `PlayerMotion.ReachRingFadeSeconds` + 0,2 s → `ReachRingVisible` sai;
      - `ToggleMode(Default)` (không cầm) → `RightClickAt` ô đó → vòng lại hiện.
    - `TempHoldPlacesBlockAfterWalking` → **`HeldBlockPlacesAfterWalkingAndExitsWhenEmpty`**:
      - túi chỉ có 1 Đất (`view.Bag.Get(Dirt)` trừ hết rồi `Add(Dirt, 1)`);
      - `HoldItem(Block(Dirt))`;
      - `RightClickAt` trả `PlaceBlock` → chờ Done → ô là Đất, `Control.HasHeld` sai, `HudText()` chứa `hết Đất, thôi cầm`.
    - `TempHoldCameraPlacesAfterWalking` → **`HeldCameraPlacesAfterWalking`**: `HoldItem(Tool(ObservationCamera))` thay 2 lần `CycleTempHold`; sau Done vẫn cầm camera (`Held.Kind == Tool`).
    - `SowModeWalksThenSows` → **`HeldSeedWalksThenSows`**:
      - ô ruộng đã xới trống cách 6 ô; `view.Bag.AddSeeds(0, 2)`;
      - Dig mode → `HoldItem(SeedBag(0))` → Mode Default;
      - `RightClickAt` trả `UseHeld` → chờ Done → túi giảm 1, vẫn cầm hạt.
    - **Mới `HeldSeedLastSeedExits`**: như trên với đúng 1 hạt (trừ hết rồi thêm 1), đứng trong 2 ô → `UseHeld`, gieo xong ngay → không cầm, `HudText()` chứa `thôi cầm`.
    - **Mới `HeldSeedRejectsQueuedPlot`**:
      - ô ruộng đã xới, `sv.Sim.EnqueueCrop(id, 0)` hai lần (lần 1 mở vụ hiện tại, lần 2 nằm trong `Slots`) → `Slots.Count > 0`;
      - `HoldItem(SeedBag(0))`, đứng trong 2 ô;
      - `Preview` ≠ `UseHeld`; `RightClickAt` → không gieo (số hạt túi không đổi);
      - `farming.StatusLine` = `Gieo: ô còn hạt trong hàng đợi — chỉ gieo tay ô có hàng đợi trống`.
    - **Mới `HeldSeedDefersToHarvest`**: ô ruộng chín (cách làm chín như `DigModeOnRipePlotDigsIt`), đứng trong 2 ô, cầm hạt → `Preview` = `Harvest`.
    - **Mới `GhostRedWhenCannotPlace`**:
      - cầm khối Đất (túi có);
      - `placeCell` = ô đang là khối đặc (vd `cell` mặt đất, truyền `RightClickAt`/`Preview(true, cell, cell)`) → `Preview` = `None`;
      - `ShowAim` → `GhostVisible`, `GhostColor == InvalidColor`;
      - ô đặt được → `GhostColor == GhostColorOf(Dirt)`.
    - **Mới `CollectShowsAreaAndWindEvenWhenEmpty`**:
      - không có vật rơi trong 2 ô → `CollectNearby()` trả 0;
      - `player.Markers.CollectVisible` đúng, `WindVisibleCount == 8`, `CollectAlpha` ≈ 0,5 (±0,05);
      - chờ `PlayerMotion.CollectShowSeconds` + 0,2 s → `CollectVisible` sai, `WindVisibleCount == 0`.
    - **Mới `CollectedDropFliesForConfiguredTime`**:
      - 1 chồng cách 1,5 ô → `CollectNearby()` trả 1 → `DropView.FlyingCount == 1`;
      - chờ `PlayerMotion.CollectFlySeconds` + 0,2 s → `FlyingCount == 0`.
    - **Mới `BagPanelHoldsAndCloses`** (có Sim):
      - `sv.ToggleBag()`;
      - `SimView.BagRows` có dòng cuối `Item == ItemId.Tool(ToolItem.ObservationCamera)`, `Holdable`, nhãn `Camera quan sát × 1`;
      - `sv.HoldFromBag(ItemId.Tool(ToolItem.ObservationCamera))` true → `BagOpen` sai, cầm camera;
      - `sv.HoldFromBag(ItemId.Produce(0))` false.
12. **Sửa test cũ** (giữ ý nghĩa, chỉ đổi cách gọi). Quy tắc:
    - `CycleTempHold()` → `HoldItem(ItemId.Block(...))` (thêm khối vào túi trước nếu test chưa có) / `HoldItem(ItemId.Tool(ToolItem.ObservationCamera))`;
    - `EnterSowMode(c)` / `ChooseSeedFromBag(c)` → `HoldItem(ItemId.SeedBag(c))` / `sv.HoldFromBag(ItemId.SeedBag(c))`;
    - `farming.SowCrop == c` → `input.Control.HasHeld && input.Control.Held == ItemId.SeedBag(c)`;
    - `farming.SowAt(cell)` → `farming.SowAt(cell, c)`;
    - `MissingColor` → `InvalidColor`;
    - vòng tầm do `ShowAim` → do `RightClickAt`.

    | File | Test |
    |---|---|
    | `Tests/Client/PlayerBagSowPlayTests.cs` | helper `EnterSow`; `BagRowsList` (thêm dòng camera cuối: 4 dòng, `Holdable` thay `SeedCrop`); `BagPanelBlocksPointer` (`rows` = số dòng thật từ `BagRows`, không cứng 1); `EnterSowModeRules` → `HoldSeedRules` (cầm hạt có hạt → đóng túi; hết hạt loại 1 → false, vẫn cầm loại 0; cầm hạt huỷ tự xới); `SowIdlePlot`; `SowWaitingSameCrop` (giữ: 1 lần `EnqueueCrop` → `Slots` trống → vẫn gieo được); `SowRejections` (bỏ assert `SowCrop`; giữ các lý do); `SowLastSeedExits` → kiểm qua `RightClickAt` trong 2 ô: không còn cầm; `SowModeIgnoresDigMode` → `HeldSeedIgnoresDigMode` (Dig mode → `HoldItem(SeedBag(0))` → Mode Default; `RightClickAt` lên ô cỏ không phải ô ruộng trả `None`, khối không đổi, `farming.StatusLine` = `Gieo: chỉ gieo vào ô ruộng đã xới`); `ExitSow` → `ExitHold` (Esc qua `Control.ClearToDefault` không test được bằng phím → gọi `ToggleMode(Default)` và `ToggleMode(Dig)`: đều thôi cầm); `SowHud` → xoá (HUD gieo cũ đã bỏ; thay bằng `HudText()` chứa `Cầm: <b><color=yellow>Túi hạt Cây A</color></b>` khi cầm hạt — đặt trong `HoldSeedRules`) |
    | `Tests/Core/Sim/PlayerFarmTests.cs` | `SowRejections` giữ; thêm test **`SowRejectsWhenQueueNotEmpty`**: ô đã xới, `EnqueueCrop` 2 lần cùng loại → `PlayerSow` trả `QueueNotEmpty`, hạt túi không đổi, `Slots.Count` không đổi; `ClearSlot` hết → `PlayerSow` Ok. Test khác hỏng vì `QueueNotEmpty` → dừng, `cần Opus` (không tự đổi luật) |
    | `Tests/Client/TerrainEditPlayTests.cs` | `HeldBlockShowsGhostAtPlaceCell`; `GhostRedWhenOutOfMaterial` → `HoldingLastBlockExitsAfterPlace` (hết khối → không cầm, không còn bản mờ đỏ "hết"); `GhostHiddenWhenRuleFails` → bản mờ **đỏ** (`InvalidColor`) khi luật đặt sai, trong tầm; `RightClickDigsOrPlacesByModeAndHeld` |
    | `Tests/Client/ObservationCameraPlayTests.cs` | `TabHoldCameraAndRightClickPlaces` → `HoldCameraAndRightClickPlaces`; `ToolsAndMovementLockedWhileViewing` (đang xem: `HoldItem` trả false, không đổi gì) |
    | `Tests/Client/PlayerFarmPlayTests.cs` | `TabCancelsWalking` → `HoldItemCancelsWalking` (`HoldItem` khối vẫn huỷ tự xới) |
    | `Tests/Client/PlayerCharacterPlayTests.cs` | `ReachLimitsRightClick` (vòng tầm kiểm sau `RightClickAt` ngoài tầm, không sau `ShowAim`) |
    | `Tests/Core/Sim/SimConfigTests.cs` | assert mặc định 3 field mới (cạnh `clickMarkMs` `:59`); 3 dòng `Bad(...)` mới (vd `reachRingFadeMs = 6000`) cạnh `:99` |
    | `Tests/Core/PlayerMotionTests.cs` | test `Configure` `:26–30`: thêm 3 field vào `new PlayerConfig { … }` và 3 assert giây |
    | `Tests/ClientEdit/GameControlsTests.cs` | `Expected()` + 2 số đếm (bước 9) |

    Test nào khác hỏng vì đổi ở brief này → sửa theo cùng quy tắc, ghi tên vào Kết quả. Hỏng mà không thuộc quy tắc → dừng, `cần Opus`.
13. **Sửa 2 test hỏng** (Planner trả lời vướng mắc vòng 2, quyết định 16–17). Code bước 1–12 đã có trong working tree — không làm lại.
    - `Tests/Core/Sim/PlayerFarmTests.cs` `SowRejections`:
      - `:168–169`: đổi comment thành `// pha Till, còn 1 vụ trong hàng đợi → hàng đợi trước` và assert `PlayerSowResult.QueueNotEmpty` (thay `NotTilled`); thêm `Assert.AreEqual(1, Plot.Slots.Count);` ngay sau.
      - Cuối test (sau `Assert.Throws…`) thêm khối kiểm `NotTilled` với hàng đợi trống:
        ```csharp
        // pha Till, hàng đợi trống → chưa xới
        Layout(false);
        bag.AddSeeds(0, 1);
        Assert.AreEqual(PlotPhase.Till, Plot.Phase);
        Assert.AreEqual(PlayerSowResult.NotTilled, sim.PlayerSow(id, 0, bag));
        Assert.AreEqual(1, bag.GetSeeds(0));
        ```
        (`Layout` tạo lại `sim`, `id`, `bag` — `PlayerFarmTests.cs:23–40`.)
    - `Tests/Client/DropPlayTests.cs`: mọi `yield return Run(Vector2.zero, false, 0.3f);` (`:88`, `:116`, `:126`, `:135`, `:157` — cùng dạng chờ vật bay) → `yield return Run(Vector2.zero, false, PlayerMotion.CollectFlySeconds + 0.2f);`. Thiếu `using` cho namespace của `PlayerMotion` → thêm. Không sửa gì khác trong file.
    - Chạy lại: EditMode + PlayMode toàn bộ (phải 529/529 và 219/219), rồi dọn `git checkout -- client/ProjectSettings/EditorSettings.asset`; commit code; ghi Kết quả / Kết quả test, trạng thái `chờ review`.

## Tiêu chí xong
- EditMode + PlayMode qua hết, 0 lỗi console (qua MCP, xem "Lệnh executor chạy").
- `grep -rn "CycleTempHold\|CycleTool\|MissingColor\|SowCrop\|EnterSowMode\|ExitSowMode\|ChooseSeedFromBag\|ShowReachRing\|SeedCrop\|FlySeconds\|farm.hud_sow\|farm.sow_exit\|farm.sow_last" client/Assets/Game loc` → không kết quả.
- `diff design/data/sim_m1.json client/Assets/Game/Client/Resources/Config/sim_m1.json` → không khác.
- `loc_check.py check loc/en.json` exit 0.
- `Game.Core` không thêm `using UnityEngine` (`grep -rn "UnityEngine" client/Assets/Game/Core` như trước brief).
- `python3 tools/check_design.py` exit 0.
- Mỗi test ở bước 11 có trong `ControlModePlayTests.cs` và qua; `PlayerFarmTests.SowRejectsWhenQueueNotEmpty` qua.

## Test sau khi xong

### Phần cần test
- Cầm vật từ túi (bước 6, 7) · `ModeKeysAndHoldItem`, `BagPanelHoldsAndCloses`, `PlayerBagSowPlayTests.HoldSeedRules`, `BagRowsList` · cầm đúng loại, túi đóng, nông sản không cầm, 1–4 đổi khối đang cầm / báo hết.
- Khối: bản mờ đỏ khi sai chỗ, hết khối thôi cầm (bước 6) · `GhostRedWhenCannotPlace`, `HeldBlockPlacesAfterWalkingAndExitsWhenEmpty`, `TerrainEditPlayTests.HoldingLastBlockExitsAfterPlace`, `GhostHiddenWhenRuleFails` · màu `InvalidColor`; đặt khối cuối → không cầm, HUD `hết Đất, thôi cầm`.
- Hạt: chỉ hàng đợi trống (bước 1, 5, 6) · `PlayerFarmTests.SowRejectsWhenQueueNotEmpty`, `HeldSeedRejectsQueuedPlot`, `HeldSeedWalksThenSows`, `HeldSeedLastSeedExits`, `HeldSeedDefersToHarvest`, `PlayerBagSowPlayTests` (`SowIdlePlot`, `SowWaitingSameCrop`, `SowRejections`) · `QueueNotEmpty`, dòng lý do đúng; vụ hiện tại không có hàng đợi vẫn gieo; cây chín → thu hoạch trước.
- Camera quan sát là vật phẩm (bước 6, 7) · `HeldCameraPlacesAfterWalking`, `ObservationCameraPlayTests.HoldCameraAndRightClickPlaces`, `ToolsAndMovementLockedWhileViewing` · đặt được, vẫn cầm sau khi đặt; đang xem không cầm được.
- Bỏ Tab (bước 9) · `GameControlsTests` (EditMode) · 35 hành động, không còn Tab ở map Player, không trùng phím.
- Vòng tầm khi chuột phải ngoài tầm (bước 3, 6) · `OutOfReachRightClickFlashesRing`, `PlayerCharacterPlayTests.ReachLimitsRightClick` · di chuột không hiện; chuột phải hiện rồi mờ hết sau ~1 giây; cả chế độ Mặc định.
- F vùng + gió (bước 3, 6) · `CollectShowsAreaAndWindEvenWhenEmpty` · hiện cả khi 0 chồng, 8 vệt, tắt sau ~0,5 giây.
- Vật rơi bay 0,3 giây (bước 2, 4) · `CollectedDropFliesForConfiguredTime` + `DropPlayTests` (toàn bộ) · bay xong đúng thời gian JSON.
- Tham số JSON (bước 2) · `SimConfigTests` (gồm `ConfigMatchesCodeDefaults`), `ConfigTests` (2 bản giống hệt), `PlayerMotionTests` · đọc đúng số, kiểm biên.
- Esc / Cài đặt (bước 6, 8) · regression `SettingsPlayTests` + `ObservationCameraPlayTests`; hành vi "đang cầm thì Esc không mở Cài đặt" kiểm ở User tự test mục 6 (phím thật).
- Chuỗi + bản dịch (bước 10) · `HardcodedTextScanTests`, `SettingsPlayTests` (ngôn ngữ), `loc_check.py check loc/en.json` · không thiếu khoá, placeholder khớp.
- Regression c1 (đã duyệt 0058) · toàn bộ `ControlModePlayTests` còn lại (`DigWalksToTwoCellsThenDigs`, `BlockedWalkIsUnreachable`, `ModeKeyCancelsWalkAndStops`, `TillMarksAfterWalking`, `RainAfterWalking`, `CollectTakesOnlyWithin2`, `RightClickDropWalksThenPicks`, `LeftClickPlotOpensPanelWithUnmark`, `UnmarkOpenPlotCancelsSelfTill`, `DigModeOnRipePlotDigsIt`, `HighlightInRangeDefaultNone`) · qua.
- Regression khác · `PlayerFarmPlayTests`, `PlayerBagSowPlayTests`, `DropPlayTests`, `ClickMovePlayTests`, `CameraPcPlayTests`, `SimFarmUiPlayTests`, `TerrainEditPlayTests`, `EditOcclusionPlayTests`, `ObservationCameraPlayTests`, toàn bộ EditMode · qua hết.
- Không đọc thẳng phím · `GameControlsTests.NoDirectInputReadsInScope` · qua.

### Lệnh executor chạy
1. Grep ở "Tiêu chí xong" + `diff` 2 bản JSON + parse JSON (vi.json, en.json, context.json, `.inputactions`) + `python3 "<Plugin root>/skills/translation/scripts/loc_check.py" check loc/en.json`.
2. Unity Editor đang mở qua MCP ([0010](../../design/decisions/0010-mcp-for-unity.md)):
   - `refresh_unity` → `read_console` (0 lỗi biên dịch);
   - `run_tests` EditMode (toàn bộ) → `run_tests` PlayMode (toàn bộ);
   - `read_console` lỗi sau khi chạy.
   - Ghi dòng tổng kết số test thật.
   - Editor tắt → `sh "<Plugin root>/scripts/run.sh" verify_run client_test client_test_play`.
3. **Dọn:** sau `run_tests` PlayMode → `git checkout -- client/ProjectSettings/EditorSettings.asset`. Không commit `client/Assets/InitTestScene*`.
4. `sh "<Plugin root>/scripts/run.sh" verify_run --all` trước commit (client_test / client_test_play exit 1 vì Editor đang mở → ghi `Chưa kiểm được` + kết quả MCP như brief 0058).
5. Commit code + test + chuỗi, message `brief 0059: 7l c2 hold items from bag, drop Tab temp hold, reach ring on right-click, F collect area + wind`; commit kết quả brief riêng `brief 0059: result`. Không push.

### User tự test
Mở scene SimView trên Mac, Play:
1. **Túi:**
   - B → bảng túi có dòng khối, túi hạt, nông sản, dòng cuối `Camera quan sát × 1`;
   - bấm dòng khối → túi đóng, HUD `Cầm: Đất (Esc / D: thôi cầm)`;
   - bấm dòng nông sản → không có gì (không phải nút).
2. **Khối:**
   - di chuột lên chỗ đặt được → bản mờ màu khối;
   - lên chỗ không đặt được (vd sát nhân vật, khối đặc) → bản mờ đỏ;
   - chuột phải chỗ đặt được → đi tới, đặt;
   - đặt hết khối loại đó → tự thôi cầm, HUD `hết …, thôi cầm`;
   - 1–4 khi đang cầm → đổi loại; loại túi không có → `Túi không còn …`.
3. **Hạt:**
   - cầm túi hạt → chuột phải ô ruộng đã xới, hàng đợi trống → đi tới, gieo;
   - ô có hạt trong hàng đợi (bảng ô ruộng thêm 2 vụ) → không gieo, dòng `Gieo: ô còn hạt trong hàng đợi …`;
   - gieo hạt cuối → thôi cầm.
4. **Camera quan sát:** B → `Camera quan sát × 1` → chuột phải khối → đi tới, đặt; vẫn cầm; V xem được.
5. **Tab:** không còn làm gì.
6. **Esc / D / Q:** đang cầm → thôi cầm; Esc khi đang cầm không mở Cài đặt.
7. **Vòng tầm:**
   - Q, di chuột ra xa > 10 ô → **không** có vòng;
   - chuột phải ở đó → vòng tròn quanh nhân vật hiện rồi mờ hết trong khoảng 1 giây;
   - Mặc định chuột phải xa cũng hiện vòng.
8. **F:**
   - bấm F chỗ không có vật rơi → vòng mờ 2 ô + vệt gió xoáy vào nhân vật, tắt sau ~0,5 giây;
   - có vật rơi trong 2 ô → vật rơi bay vào nhân vật (~0,3 giây) rồi vào túi.
   - Cảm giác thời gian 1 s / 0,5 s / 0,3 s có ổn không — số tạm, chỉnh trong `design/data/sim_m1.json` → `player` (`reachRingFadeMs`, `collectShowMs`, `collectFlyMs`).

## Dừng và chuyển lại Opus khi
- Brief mơ hồ hoặc mâu thuẫn với code thật (vd `FarmPlot.Slots` không phải `List<CropSlot>`, `EnqueueCrop` lần 2 không vào `Slots`).
- Cần một quyết định thuộc AGENTS.md §3.4 hoặc ngoài "Quyết định đã chốt".
- Cùng một lỗi sửa 2 lần vẫn chưa qua.
- Phải sửa file ngoài "Phạm vi".
- Test cũ hỏng mà không sửa được theo quy tắc ở bước 12 (đặc biệt test Core gieo / băm mô phỏng hỏng vì `QueueNotEmpty`).

---
<!-- Từ đây trở xuống: EXECUTOR ghi. -->

## Kết quả
Base commit: 612bfb7 (tiếp tục sau trả lời Planner; HEAD 30fd1ce)
Chạy bằng: Sonnet · medium (claude-sonnet-5-5)
Bắt đầu: 2026-10-24 01:28 · Kết thúc: 2026-10-24 02:41 (vòng 3 sau trả lời Planner)
Commit code: 88424f8 (29 file: Core Sim/PlayerMotion/SimConfig, Client TerrainEditInput/PlayerFarmActions/SimView/MoveMarkers/DropView/SettingsView/GameInput/ObservationCamera/inputactions, vi.json, loc/en.json, loc/context.json, 2 JSON sim_m1, test). Vòng 3 sửa thêm: PlayerFarmTests.SowRejections, DropPlayTests (5 chỗ chờ).
Lệch: không (giờ thực tế tổng chưa tính, Planner cộng).
<!-- Base commit · Chạy bằng: <model id> · <effort> · commit · file đã đổi · thời gian bắt đầu/kết thúc (giờ thực tế M1) · lệch so với brief. -->

## Kết quả test
Sau bước 13 (vòng 3, 2026-10-24 02:41). Unity MCP.
- Biên dịch: 0 lỗi console.
- EditMode toàn bộ: **529/529** đạt (gồm `PlayerFarmTests.SowRejections`, `SowRejectsWhenQueueNotEmpty`, `GameControlsTests`, `SimConfigTests`, `PlayerMotionTests`).
- PlayMode toàn bộ: **219/219** đạt (gồm `DropPlayTests.*`, `ControlModePlayTests` mọi test bước 11, `PlayerBagSowPlayTests`, `TerrainEditPlayTests`, `ObservationCameraPlayTests`). Lần chạy đầu bị ngắt ở 193/219 ("player was stopped", không có test hỏng); chạy lại sạch.
- Grep tên đã xoá: rỗng; 2 JSON giống hệt; `loc_check.py check loc/en.json` OK (exit 0); `check_design.py` OK; `Game.Core` không có UnityEngine.
- Dọn: `git checkout -- client/ProjectSettings/EditorSettings.asset` đã làm. `InitTestScene*` không commit.
- `verify_run --all`: **Chưa chạy** (Editor đang mở → client_test/client_test_play không chạy dòng lệnh được; thay bằng kết quả MCP ở trên).
- Phần "User tự test" (mục 1–8): **chờ user** (hình ảnh, cảm giác, thời gian 1 s / 0,5 s / 0,3 s).

## Vướng mắc
**(Vòng 2, 2026-10-24)** Hai test hỏng, cả hai do luật brief nhưng ngoài quy tắc sửa:
1. `PlayerFarmTests.SowRejections` (:162–182): `Layout(false, crops: new[]{1})` enqueue sẵn 1 vụ vào `Slots` ở pha Till → theo thứ tự quyết định 3 (`QueueNotEmpty` trước `NotTilled`) dòng 169 trả `QueueNotEmpty`. Brief ghi "giữ" nên không cho sửa.
   - Phương án A: đổi test — bỏ `crops:` cho phần NotTilled (Layout rỗng), và phần `OtherCrop` (cần vụ hiện tại khác loại) dựng bằng `EnqueueCrop` 1 lần (Slots trống vì vụ lấy ra ngay). Cần xem lại các assert sau.
   - Phương án B: đổi thứ tự kiểm (`NotTilled` trước `QueueNotEmpty`) — hợp lý: ô chưa xới thì nói "chưa xới" trước; vẫn đúng quyết định 3 về ý nghĩa nhưng đổi thứ tự đã chốt.
   - Đề xuất: B (ít sửa test nhất, thông báo cho người chơi tự nhiên hơn) nhưng là quyết định của Planner.
2. `DropPlayTests.CollectNearbyTakesOnlyWithin2` (:126): chờ `Run(..., 0.3f)` đúng bằng `CollectFlySeconds` = 0,3 → còn 1 chồng đang bay (đua thời gian). `DropPlayTests.cs` ngoài Phạm vi.
   - Đề xuất: cho sửa `DropPlayTests.cs` — đổi thời gian chờ thành `PlayerMotion.CollectFlySeconds + 0.2f` (như bước 11 đã làm cho test mới). Có thể các test khác trong file cùng dạng nhưng hiện chỉ test này hỏng.
**Planner trả lời:**
1. Chọn **A** (giữ thứ tự quyết định 3, sửa test) — quyết định 16, bước 13. Lý do: ô còn hàng đợi là ô đã giao cho sinh vật; báo "chưa xới" sẽ khiến người chơi xới xong vẫn bị chặn. Phần `OtherCrop` của test giữ nguyên (sau xới, vụ lấy khỏi `Slots` → `Slots` trống, như vướng mắc mô tả).
2. Đồng ý: được sửa `DropPlayTests.cs`, chỉ thời gian chờ, cả 5 chỗ cùng dạng — quyết định 17, bước 13.


---
<!-- PLANNER ghi khi review. -->

## Review
