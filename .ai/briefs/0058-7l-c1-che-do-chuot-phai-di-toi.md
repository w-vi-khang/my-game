# Brief 0058 — 7l c1: chế độ D/Q/E/R, chuột phải tự đi tới 2 ô rồi làm, lớp đỏ + vòng tầm, F Thu 2 ô, click trái ô ruộng → bảng (Bỏ đánh dấu)

> Trạng thái: đã duyệt
> Review: đầy đủ + user xem
> Model thực thi: Sonnet · medium
> Cập nhật: 2026-10-23 20:56

<!-- Planner (Opus) viết, Executor (Sonnet) làm theo ở PHIÊN KHÁC — executor không có lịch sử chat, không được tự quyết. -->

## Mục tiêu
Bước **7l c1** của task [m1-world-7l-dieu-khien-pc](../tasks/m1-world-7l-dieu-khien-pc.md) (luật: [0034 mục 3](../../design/decisions/0034-dieu-khien-pc-click-di-chuyen.md#3-chế-độ-hành-động-và-chuột-phải)):
1. Bỏ "công cụ Tab" (Đào/Đặt/Xới/Camera). Thay bằng **chế độ** D Mặc định / Q Đào / E Xới / R Tưới (bấm lại = tắt) dùng `ControlState` có sẵn ở `Game.Core`.
2. Chuột phải: chọn việc theo `RightClick.Resolve` (đã có), trong **tầm thao tác 10 ô**; nhân vật **tự đi thẳng tới khi trong 2 ô** rồi mới làm; kẹt → "Không tới được ô này".
3. Khối dưới con trỏ phủ **lớp đỏ nhạt** (bỏ hộp viền trắng); con trỏ ngoài tầm thao tác khi đang có chế độ / vật cầm → **vòng tầm thao tác** bán kính 10 dưới chân.
4. **F Thu** hút vật rơi trong **2 ô**.
5. **Click trái ô ruộng** (có biển hoặc đã xới) → đi tới 2 ô → mở bảng ô ruộng; bảng có nút **Bỏ đánh dấu**.
6. Đặt khối + đặt camera quan sát chưa thành vật phẩm (brief c2 sau): giữ tạm bằng **Tab vòng tạm** Không → cầm khối → cầm camera quan sát → Không.
7. Chuyển hết tầm cũ 8 ô (`ToolReach`/`HandReach`) sang tầm mới (`TargetReach` 10 / `ActReach` 2 / `DigReach` 2 / `CollectReach` 2) rồi gỡ hẳn tầm cũ + key JSON.

Bắt đầu: kiểm brief 0057 có `> Trạng thái: đã duyệt` và commit `9d6ce97` có trong `git log`; không có → dừng, `cần Opus`.

**File đang dở của phiên trước:** `client/Assets/Game/Tests/Client/TerrainEditPlayTests.cs` có sẵn 2 dòng chưa commit trong `TimingSingleChunkEdits` (`view.waterStepsPerSecond = 0f;` + chú thích, sửa test chập chờn). Giữ nguyên 2 dòng đó, commit chung với brief này, ghi một dòng ở mục Kết quả. `git diff` của file đó lúc bắt đầu khác 2 dòng này → dừng, `cần Opus`.

## Bối cảnh cần đọc
Đường dẫn dưới đây tính từ `client/Assets/Game/` trừ khi ghi khác.

**Game.Core (đã có, đọc hết):**
- `Core/ControlState.cs` (72 dòng):
  - `enum ControlMode : byte { Default = 0, Dig = 1, Till = 2, Rain = 3 }`.
  - `ControlState`:
    - thuộc tính `Mode`, `HasHeld`, `Held` (`ItemId`), `event Action Changed`;
    - `ToggleMode(ControlMode m)`: `m == Default` hoặc `m == Mode` → `Default`; luôn bỏ vật cầm;
    - `Hold(ItemId item)`: Mode = Default, cầm item;
    - `ClearToDefault()`.
  - `RightClickAction { None, OutOfReach, UseHeld, Dig, Till, Rain, Harvest, OpenStorage, PickDrop, PlaceBlock }`.
  - `RightClickTarget { InTargetReach, HeldUsable, ModeUsable, RipeCrop, Storage, Drop }` (bool).
  - `RightClick.Resolve(ControlState state, in RightClickTarget t)` `:51–70`. Thứ tự: ngoài tầm → OutOfReach; cầm vật khác khối + dùng được → UseHeld; chế độ ≠ Default + dùng được → Dig/Till/Rain; RipeCrop → Harvest; Storage → OpenStorage; Drop → PickDrop; cầm khối + dùng được → PlaceBlock; còn lại None.
- `Core/PlayerRules.cs` (83 dòng):
  - `:12–17` `ToolReach`, `HandReach` (8) — **gỡ ở brief này**;
  - `:18–21` `TargetReach 10`, `ActReach 2`, `DigReach 2`, `CollectReach 2`;
  - `:24–35` `Configure(PlayerConfig p)`;
  - `:43–54` các hàm `InToolReach/InHandReach/InTargetReach/InActReach/InDigReach/InCollectReach(float feetX, float feetY, float feetZ, int x, int y, int z)`.
- `Core/DropField.cs:6` `enum ItemKind : byte { Block = 0, SeedBag = 1, Produce = 2, Tool = 3 }`, `:8` `enum ToolItem : byte { ObservationCamera = 0 }`, `:16` `ItemId.Block(BlockMaterial m)`, `:29` `ItemId.Tool(ToolItem t)`. `ItemId.Material`, `ItemId.Kind` (đã dùng ở `TerrainEditInput.ItemLabel`).
- Chỗ Core còn dùng tầm cũ: `Core/DropPick.cs:25` (`InHandReach`), `Core/DropField.cs:177` (`CollectInReach`), `:192` (`TryCollect`), `Core/StarterBag.cs:66` (`Standable`), `Core/Sim/Simulation.cs:1075–1080` (`StorageInReach`), `Core/Sim/SimConfig.cs:131–132` (field `toolReach`, `handReach`) và `:350–353` (kiểm biên).
- JSON: `design/data/sim_m1.json:34` và bản sao `client/Assets/Game/Client/Resources/Config/sim_m1.json:34` (phải giống hệt — `ConfigTests` kiểm) có `"toolReach": 8, "handReach": 8, `.

**Client (đọc hết các đoạn này):**
- `Client/TerrainEditInput.cs` (506 dòng) — file sửa nhiều nhất:
  - `:8–9` `enum EditTool { Dig, Place, Till, Camera }`;
  - `:26–28` `maxDistance`, `outlinePixels`;
  - `:37–69` field + thuộc tính (`outline`, `selected`, `lastResult`, `Farm`, `TillAvailable`, `LastTill`, `MissingColor`, `Tool`, `SelectedMaterial`, `Ghost`, `OutlineVisible`, `GhostVisible`, `GhostCell`, `GhostColor`, `DropView`, `AimedDrop`, `Farming`, `Observation`, `AimedHarvest`);
  - `:71–82` `Awake`;
  - `:84–181` `Update` (thứ tự hiện tại: khoá khi xem camera → phím 1–4, Tab, Esc thoát gieo, F → chặn UI → Alt+lăn → nhánh công cụ Camera → vật rơi → kho → `Pick` → R Mưa → thu hoạch → gieo → `ShowAim` + độ rộng nét khung → `UseTool` khi chuột phải);
  - `:184–194` `UnmarkAimed` (không còn ai gọi);
  - `:197–207` `PlaceObservationCamera(Vector3Int cell)`;
  - `:210–222` `ToggleTool`;
  - `:235–262` `ShowAim(bool picked, Vector3Int digCell, Vector3Int placeCell)`;
  - `:265–304` `public EditResult UseTool(Vector3Int digCell, Vector3Int placeCell)` (nhánh gieo, Camera, kiểm tầm, Xới `:279–292`, Đào `:293–300`, Đặt `:301–303`);
  - `:309–317` `Pick(Ray ray, out Vector3Int digCell, out Vector3Int placeCell)`;
  - `:320–329` `PickDrop(Ray ray)`;
  - `:332–340` `CollectNearby()`;
  - `:343–352` `CollectDrop(DropStack s)`;
  - `:355–370` `ItemLabel`, `DropLabel`, `Name`;
  - `:384–392` `HelpText()`;
  - `:395–419` `HudText()`;
  - `:443–472` `CreateOutline()`;
  - `:474–504` `CreateGhost()`.
- `Client/PlayerFarmActions.cs` (308 dòng):
  - `:72–104` `BeginSelfTill(int plotId)` (tự tìm đường, giữ nguyên);
  - `:115–128` `EnterSowMode(int crop)`;
  - `:131–136` `ExitSowMode()`;
  - `:139–174` `SowAt(Vector3Int cell)` (kiểm tầm `:145–149`);
  - `:230–258` `CastRain(Vector3Int cell)` (kiểm tầm `:233–237`);
  - `:261–270` `RipePlotAt(Vector3Int cell)` (kiểm tầm `:268`);
  - `:272–281` `Harvest(int plotId)`.
- `Client/PlayerCharacter.cs` (426 dòng):
  - `:15` `enum ClickMoveState { None, Walking, Arrived, Blocked }`;
  - `:52` `public Vector3 FeetLocal`;
  - `:78` `ClickMove`;
  - `:94–99` `public void MoveTo(Vector3 targetLocal)` (đích quy về độ cao chân, `ClickMove = Walking`, reset kẹt);
  - `:101–104` `public void StopClickMove()`;
  - `:85` `public MoveMarkers Markers`;
  - `:267–271` `public bool InToolReach(Vector3Int cell)` (wrapper tầm duy nhất);
  - `:361–376` đi tới đích: tới `PlayerMotion.ArriveDistance` → `Arrived`;
  - `:404–407` kẹt 0,3 giây → `Blocked`;
  - `:315–321` khi `FlyMode` hoặc `ObservationCamera.InputLocked` → `ClickMove` Walking thành `None`.
- `Client/MoveMarkers.cs` (đọc hết): `Awake` `:21–33`, `Child` `:39–45`, `Circle` `:48–57`, `Make` `:59–77`, `SetColor` `:79–83`.
- `Client/SimView.cs`:
  - `:263–264` biển;
  - `:267` `OpenPlotId`;
  - `:273` `CanTillAt`;
  - `:276–295` `UseTill`;
  - `:298–305` `UnmarkAt(Vector3Int cell)`;
  - `:308–315` `SelfTillOpenPlot`;
  - `:336–340` `ClosePlotPanel`;
  - `:375` `ChooseSeedFromBag`;
  - `:390–401` `OpenStockAt(int index)` (kiểm `InHandReach` `:397`);
  - `:404–414` `CanTransferStock`;
  - `:424`, `:499` chuỗi `stock.too_far` với `PlayerRules.HandReach`;
  - `:520–586` vẽ bảng ô ruộng (`var p = Sim.State.Plots[OpenPlotId];` `:523`; nút "Tự xới" `:582–584`, "Đóng" `:585`).
- `Client/GameInput.cs` (49 dòng, đọc hết) + `Client/Input/GameControls.inputactions` (map `Player` 24 action; `CastRain` = `<Keyboard>/r`, `CycleTool` = `<Keyboard>/tab`; D/Q/E chưa gán trong mọi map).
- `Client/DebugFlyCamera.cs:37, 75–80` đọc thẳng W/A/S/D/Q/E khi bay (F3) — không sửa, chỉ cần biết để tắt phím chế độ khi bay.
- `Client/Resources/Lang/vi.json` — các khoá ở bước 9.
- Test: `Tests/ClientEdit/GameControlsTests.cs:22–47` (`Expected()`), `:64`, `:93` (đếm 33); test liệt kê ở mục "Phần cần test".

API Unity dùng (đều đã có trong code, 6000.4 / Input System 1.19). Không dùng API nào khác:
- `LineRenderer` ([docs](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/LineRenderer.html)).
- `MeshRenderer`, `Transform.localScale` ([docs](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/Transform-localScale.html)).
- `Physics.Raycast` ([docs](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/Physics.Raycast.html)).
- `GUI.Button` ([docs](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/GUI.Button.html)).
- `InputAction.WasPressedThisFrame` ([docs](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.19/api/UnityEngine.InputSystem.InputAction.html)).

## Quyết định đã chốt (không được đổi)
1. **Tab vòng tạm** (user, 2026-10-23): Tab đổi vòng Không cầm → cầm khối (`ItemId.Block(SelectedMaterial)`) → cầm camera quan sát (`ItemId.Tool(ToolItem.ObservationCamera)`) → Không cầm, qua `ControlState.Hold` / `ClearToDefault`. Phím 1–4 / Alt+lăn đổi loại khối; đang cầm khối → cầm luôn loại mới. Đào/Xới không còn trong Tab. Brief c2 thay Tab tạm bằng cầm từ túi.
2. **Tự đi tới** (Planner): đi thẳng bằng `PlayerCharacter.MoveTo(tâm ô)`, mỗi frame kiểm tầm. Đào dùng `InDigReach`, việc khác `InActReach`.
   - Vào tầm → `StopClickMove()` rồi làm.
   - `ClickMove` không còn `Walking` (Arrived / Blocked / None) mà chưa vào tầm → huỷ, dòng kết quả `farm.unreachable` ("Không tới được ô này").
   - Không tìm đường. Ngoại lệ: Xới lên biển chưa xới vẫn gọi `BeginSelfTill` (tự tìm đường như cũ, không đi tới 2 ô trước).
   - Không có nhân vật hoặc đang bay (F3) → làm ngay, không đi, không kiểm tầm 2 ô.
   - Đã trong tầm lúc click → làm ngay.
3. **Huỷ việc đang đi tới:** chuột phải mới (thay việc cũ), click trái lên world (không trên UI), phím chế độ D/Q/E/R, Tab, đang xem camera quan sát. Click trái thì không dừng nhân vật (nhân vật đi theo click mới). Phím chế độ / Tab thì dừng nhân vật.
4. **Tầm** (0034 mục 3; ghi trước ở 0054):
   - Chuột phải chỉ có tác dụng khi ô đích trong `TargetReach` 10. Ô đích = ô vật rơi nếu nhắm vật rơi, ô kho nếu nhắm kho, còn lại `digCell`.
   - Hàm thực hiện (`SowAt`, `CastRain`, `RipePlotAt`, `OpenStockAt`, `PlaceObservationCamera`, `DropField.TryCollect`, `DropPick.Pick`, `Simulation.StorageInReach`, `StarterBag`) kiểm `TargetReach` làm lưới an toàn. Tầm 2 ô do bộ đi tới bảo đảm.
   - F Thu (`DropField.CollectInReach`) dùng `CollectReach` 2.
   - Gỡ hẳn `ToolReach`, `HandReach`, `InToolReach`, `InHandReach`, field + kiểm biên `toolReach`/`handReach`, key JSON ở cả 2 bản, `PlayerCharacter.InToolReach`.
5. **Chế độ "dùng được ở chỗ này"** (`ModeUsable`): Đào = có ô (kết quả đào quyết lúc làm); Xới = `Farm.CanTillAt(digCell)`; Tưới = có ô. **Vật cầm dùng được** (`HeldUsable`): khối = `CheckPlace(placeCell, Held.Material) == EditResult.Ok`; camera = có ô.
6. **Chế độ gieo cũ** (B → bấm túi hạt) giữ tới c2, coi như cầm hạt:
   - vào chế độ gieo → `Control.ClearToDefault()`;
   - `Resolve` trả `None` mà đang gieo, có ô, trong tầm → `UseHeld` (gieo, đi tới 2 ô);
   - D/Q/E/R/Tab/Esc thoát chế độ gieo.
7. **Phím chế độ tắt** khi không có nhân vật hoặc đang bay F3 (camera bay đọc thẳng D/Q/E). Gọi API `ToggleMode` từ code vẫn chạy. Không có Sim (`TillAvailable == false`) → E không làm gì.
8. **Esc** (Player/Back): đang gieo → thoát gieo; không thì đang cầm → `ClearToDefault()`. Không đụng chế độ khi không cầm gì.
9. **Lớp đỏ:** hộp lập phương (dùng chung mesh của bản mờ), shader `Game/GhostBlock` (ZTest thường → chỉ mặt lộ ra ngoài địa hình có màu), màu `(1, 0.15, 0.15, 0.35)`, phóng 1,04 quanh ô. Hiện khi việc chuột phải sẽ là Dig / Till / Rain / UseHeld. PlaceBlock → bản mờ màu vật liệu như cũ. Cầm khối mà túi hết loại đó (`CheckPlace == NoMaterial`, trong tầm) → bản mờ đỏ `MissingColor` như cũ. Chế độ Mặc định không cầm gì → không tô. Bỏ hẳn hộp viền trắng (`outline`, `outlinePixels`).
10. **Vòng tầm:** `LineRenderer` vòng tròn 64 điểm, bán kính `PlayerRules.TargetReach`, con của nhân vật (cao 0,03 như vòng chân), nét 0,08, màu `(1, 1, 1, 0.25)`, shader như `MoveMarkers.Make`. Hiện khi `Resolve` trả `OutOfReach` **và** (chế độ ≠ Mặc định **hoặc** đang cầm **hoặc** đang gieo). Con trỏ trên UI / trời → ẩn.
11. **Click trái ô ruộng:** ô dưới con trỏ có ô ruộng (`Sim.PlotAt(digCell) >= 0`, có biển hoặc đã xới), không nhắm vật rơi / kho → đi tới 2 ô → `SimView.OpenPlotPanel(id)`. Không giới hạn 10 ô (click trái = đi, tầm vô hạn).
12. **Nút Bỏ đánh dấu** ở bảng ô ruộng: bật khi ô chưa xới (`!p.Tilled`). Bấm → bỏ biển như `UnmarkAt`, ô đang tự xới thì huỷ tự xới, đóng bảng.
13. Phím mới: `Player/ModeDefault` D, `Player/ModeDig` Q, `Player/ModeTill` E; đổi tên `Player/CastRain` → `Player/ModeRain` (vẫn R). Tổng hành động 33 → **36**. Giữ `CycleTool` (Tab).
14. Mana không làm ở brief này (c3). Không làm túi ô vuông / kho tự đóng (7l d), HUD gọn (7l e).
15. Chuỗi người chơi đọc: chép nguyên văn ở bước 9.

## Phạm vi
- Được sửa:
  - `client/Assets/Game/Core/PlayerRules.cs`, `Core/DropPick.cs`, `Core/DropField.cs`, `Core/StarterBag.cs`, `Core/Sim/Simulation.cs` (chỉ `StorageInReach`), `Core/Sim/SimConfig.cs` (chỉ 2 field + kiểm biên);
  - `design/data/sim_m1.json`, `client/Assets/Game/Client/Resources/Config/sim_m1.json` (chỉ xoá 2 key);
  - `client/Assets/Game/Client/TerrainEditInput.cs`, `Client/PlayerFarmActions.cs`, `Client/PlayerCharacter.cs` (chỉ wrapper tầm), `Client/MoveMarkers.cs`, `Client/SimView.cs`, `Client/GameInput.cs`, `Client/Input/GameControls.inputactions`, `Client/Resources/Lang/vi.json`;
  - tạo `client/Assets/Game/Client/WalkToAct.cs` (+ `.meta` Unity tự tạo);
  - test: các file ở mục "Phần cần test"; tạo `client/Assets/Game/Tests/Client/ControlModePlayTests.cs`;
  - file brief này (mục Kết quả / Kết quả test / Vướng mắc).
- Không được sửa: `Core/ControlState.cs`, `ObservationCamera.cs`, `FollowCamera.cs`, `DebugFlyCamera.cs`, `design/` ngoài `design/data/sim_m1.json`, task contract, mọi file khác. Không sửa `Tests/ClientEdit/GameControlsTests.cs` ngoài `Expected()` và hai số đếm.

## Các bước
1. **Core — tầm** (`Core/`):
   - `PlayerRules.cs`:
     - xoá `ToolReach`, `HandReach` (+ 2 dòng summary + 2 dòng `///` chú thích), 2 dòng gán trong `Configure`, `InToolReach`, `InHandReach`;
     - summary lớp `:7` đổi "tầm với mọi thao tác 8 ô (0031)" → "tầm thao tác 10 ô, tầm thực hiện / đào / Thu 2 ô (0034)".
   - `DropPick.cs:25` → `PlayerRules.InTargetReach`; summary `:6`, `:11` "tầm việc tay" → "tầm thao tác".
   - `DropField.cs:177` → `PlayerRules.InCollectReach`; `:192` → `PlayerRules.InTargetReach`.
   - `StarterBag.cs:66` → `PlayerRules.InTargetReach`.
   - `Sim/Simulation.cs` `StorageInReach` → `PlayerRules.InTargetReach`; summary "(0031: 8 ô)" → "(tầm thao tác 0034: 10 ô)".
   - `Sim/SimConfig.cs`: xoá field `toolReach`, `handReach` (`:131–132`) và 4 dòng kiểm biên `:350–353`.
   - JSON 2 bản: xoá đúng chuỗi `"toolReach": 8, "handReach": 8, ` ở dòng 34. Hai file vẫn giống hệt (`diff` không ra gì).
   - Xong khi: `grep -rn "ToolReach\|HandReach\|toolReach\|handReach\|InToolReach\|InHandReach" client/Assets/Game design/data` chỉ còn trong file test (sửa ở bước 11).
2. **PlayerCharacter.cs** `:267–271`: thay `InToolReach` bằng 3 hàm cùng dạng: `InTargetReach(Vector3Int cell)`, `InActReach(Vector3Int cell)`, `InDigReach(Vector3Int cell)` (gọi `PlayerRules.InTargetReach/InActReach/InDigReach(f.x, f.y, f.z, cell.x, cell.y, cell.z)`).
3. **Tạo `Client/WalkToAct.cs`** — chép đúng:
   ```csharp
   using System;
   using UnityEngine;

   namespace Game.Client
   {
       public enum WalkToActState { Idle, Walking, Done, Unreachable, Cancelled }

       /// <summary>Đi thẳng tới khi ô trong tầm thực hiện (2 ô; đào: tầm đào) rồi làm (0034 mục 3, 7l c1). Không tìm đường; kẹt / tới đích mà chưa trong tầm → Unreachable.</summary>
       public sealed class WalkToAct
       {
           public WalkToActState State { get; private set; }
           public Vector3Int Cell { get; private set; }
           public bool DigReach { get; private set; }
           Action act;

           public static bool InReach(PlayerCharacter p, Vector3Int cell, bool digReach) => digReach ? p.InDigReach(cell) : p.InActReach(cell);

           /// <summary>player null / đang bay / đã trong tầm → act() ngay, Done. Không thì MoveTo(tâm ô), Walking (thay việc đang đi nếu có).</summary>
           public void Begin(PlayerCharacter player, Vector3Int cell, bool digReach, Action act)
           {
               Cell = cell;
               DigReach = digReach;
               this.act = null;
               if (player == null || player.FlyMode || InReach(player, cell, digReach))
               {
                   State = WalkToActState.Done;
                   act?.Invoke();
                   return;
               }
               this.act = act;
               player.MoveTo(new Vector3(cell.x + 0.5f, 0f, cell.z + 0.5f));
               State = WalkToActState.Walking;
           }

           /// <summary>Walking → Cancelled; stop = dừng nhân vật đang đi. Trạng thái khác: không đổi.</summary>
           public void Cancel(PlayerCharacter player, bool stop)
           {
               if (State != WalkToActState.Walking) return;
               State = WalkToActState.Cancelled;
               act = null;
               if (stop && player != null) player.StopClickMove();
           }

           /// <summary>Gọi mỗi frame. Trả true đúng frame chuyển sang Unreachable.</summary>
           public bool Tick(PlayerCharacter player)
           {
               if (State != WalkToActState.Walking) return false;
               if (player == null) { State = WalkToActState.Cancelled; act = null; return false; }
               if (InReach(player, Cell, DigReach))
               {
                   player.StopClickMove();
                   var a = act;
                   act = null;
                   State = WalkToActState.Done;
                   a?.Invoke();
                   return false;
               }
               if (player.ClickMove != ClickMoveState.Walking)
               {
                   State = WalkToActState.Unreachable;
                   act = null;
                   return true;
               }
               return false;
           }
       }
   }
   ```
   Xong khi: biên dịch không lỗi.
4. **MoveMarkers.cs** — vòng tầm thao tác:
   - field `LineRenderer reach;`;
   - trong `Awake` sau `arrow`:
     ```csharp
     reach = Make(Child("ReachRing"), Circle(64, PlayerRules.TargetReach), true, 0.08f);
     SetColor(reach, new Color(1f, 1f, 1f, 0.25f));
     reach.enabled = false;
     ```
     cần `using Game.Core;`;
   - thêm:
     ```csharp
     public bool ReachRingVisible => reach.enabled;
     public void ShowReachRing(bool on) { if (reach.enabled != on) reach.enabled = on; }
     ```
   - summary lớp thêm "vòng tầm thao tác (0034 mục 3)".
   - `PlayerCharacter.Awake` đã gọi `ConfigLoader.LoadSimConfig()` trước `AddComponent<MoveMarkers>()` (`:171–174`) → bán kính đúng số JSON.
5. **Input** (0033):
   - `GameControls.inputactions` map `Player`:
     - thêm 3 action Button `ModeDefault` (`<Keyboard>/d`), `ModeDig` (`<Keyboard>/q`), `ModeTill` (`<Keyboard>/e`) — cùng dạng các action Button khác, id GUID mới;
     - đổi tên action `CastRain` → `ModeRain` (sửa cả trường `action` của binding `<Keyboard>/r`).
   - `GameInput.cs`: xoá `CastRain`; thêm `ModeDefault`, `ModeDig`, `ModeTill`, `ModeRain` (`Get("Player/…")`).
   - `Tests/ClientEdit/GameControlsTests.cs`: trong `Expected()` xoá dòng `CastRain`, thêm 4 dòng `["Player/ModeDefault"] = B("<Keyboard>/d")`, `ModeDig` q, `ModeTill` e, `ModeRain` r; số `33` ở `:64`, `:93` → `36`.
   - Xong khi: `grep -rn "CastRain" client/Assets/Game` không còn kết quả.
6. **PlayerFarmActions.cs:**
   - `SowAt` `:145`, `CastRain` `:233`, `RipePlotAt` `:268` → `player.InTargetReach(cell)`; chuỗi `farm.sow_far` / `farm.rain_far` truyền `PlayerRules.TargetReach`.
   - `EnterSowMode` nhánh thành công, ngay trước `SowCrop = crop;`:
     ```csharp
     var edit = GetComponent<TerrainEditInput>();
     if (edit != null) edit.Control.ClearToDefault();
     ```
   - Summary `:260` "trong tầm 8 ô" → "trong tầm thao tác".
7. **SimView.cs:**
   - `OpenStockAt` `:397` → `PlayerRules.InTargetReach`; `:424`, `:499` `PlayerRules.HandReach` → `PlayerRules.TargetReach`; summary `:389`, `:403` "8 ô" → "tầm thao tác".
   - Thêm sau `UnmarkAt`:
     ```csharp
     /// <summary>Click trái ô ruộng (0034 mục 3): mở bảng ô ruộng plotId (có biển hoặc đã xới). false nếu không có / đã bỏ.</summary>
     public bool OpenPlotPanel(int plotId)
     {
         if (Sim == null || plotId < 0 || plotId >= Sim.State.Plots.Count || Sim.State.Plots[plotId].Removed) return false;
         OpenPlotId = plotId;
         return true;
     }

     /// <summary>Nút "Bỏ đánh dấu" (0034 mục 3): ô đang mở chưa xới → bỏ biển, huỷ tự xới ô đó, đóng bảng. true khi đã bỏ.</summary>
     public bool UnmarkOpenPlot()
     {
         if (OpenPlotId < 0 || Sim.State.Plots[OpenPlotId].Tilled) return false;
         int id = OpenPlotId;
         var s = Sim.State.Plots[id].Soil;
         ClosePlotPanel();
         if (!UnmarkAt(new Vector3Int(s.X, s.Y, s.Z))) return false;
         var farming = GetComponent<PlayerFarmActions>();
         if (farming != null && farming.TillState != SelfTillState.None && farming.TillPlotId == id) farming.CancelSelfTill();
         return true;
     }
     ```
     - Đã kiểm [repo]: `Sim.State.Plots` là `List<FarmPlot>` (`Core/Sim/SimData.cs:296`); `FarmPlot.Soil` `:236`, `Tilled` `:249`, `Removed` `:253`; `Storages` là `List<Storage>` `:294`, `Storage.Cell` `:84`.
   - Bảng ô ruộng, giữa nút "Tự xới" (`:582–584`) và "Đóng" (`:585`):
     ```csharp
     GUI.enabled = !p.Tilled;
     if (GUI.Button(new Rect(pr.x + 130f, pr.yMax - 36f, 120f, 28f), T("plot.unmark"))) UnmarkOpenPlot();
     GUI.enabled = true;
     ```
     Sau nút này mà `OpenPlotId < 0` (bảng vừa đóng) thì `return` trước khi vẽ "Đóng", tránh đọc ô đã bỏ.
8. **TerrainEditInput.cs** — viết lại phần điều khiển:
   - **Xoá:**
     - `EditTool` (`:8–9`), `Tool`, `ToggleTool`, `UnmarkAimed`, `UseTool`;
     - `outline`, `outlinePixels`, `OutlineVisible`, `CreateOutline`, đoạn tính độ rộng nét (`:169–175`);
     - nhánh công cụ Camera (`:120–126`).
   - **Thêm thuộc tính:**
     ```csharp
     public ControlState Control { get; } = new ControlState();
     public WalkToAct Walk { get; } = new WalkToAct();
     public static readonly Color HighlightColor = new Color(1f, 0.15f, 0.15f, 0.35f);
     public bool HighlightVisible => highlightRenderer.enabled;
     public Vector3Int HighlightCell { get; private set; }
     ```
     - field `MeshRenderer highlightRenderer; Transform highlight;`;
     - `public bool ReachRingVisible => PlayerCharacter.Active != null && PlayerCharacter.Active.Markers.ReachRingVisible;`.
   - **Mesh dùng chung:** tách mesh lập phương trong `CreateGhost` ra `static Mesh CubeMesh()` (mesh giữ nguyên như hiện có). `CreateGhost` dùng nó.
   - **Thêm `CreateHighlight()`** (gọi trong `Awake` thay `CreateOutline`):
     - GameObject "Highlight", con của `transform`, `MeshFilter` = `CubeMesh()`, `MeshRenderer` không đổ bóng;
     - material `Shader.Find("Game/GhostBlock")` (null → `"Game/SelectionOverlay"`), `_BaseColor` = `HighlightColor`;
     - `localScale = Vector3.one * 1.04f`, tắt lúc đầu.
     - Đặt ô: `highlight.localPosition = cell - Vector3.one * 0.02f; HighlightCell = cell;`.
   - **Thêm API** (chép đúng tên + chữ ký):
     ```csharp
     /// <summary>D/Q/E/R (0034 mục 3): thoát gieo, huỷ đi tới (dừng nhân vật), Control.ToggleMode(m). No-op khi đang xem camera quan sát; Till khi không có Sim → no-op.</summary>
     public void ToggleMode(ControlMode m)
     /// <summary>Tab vòng tạm tới c2: không cầm → khối SelectedMaterial → camera quan sát → không cầm. Đang gieo → chỉ thoát gieo. Huỷ tự xới + đi tới (dừng nhân vật). No-op khi đang xem.</summary>
     public void CycleTempHold()
     /// <summary>Phím 1–4 / Alt + lăn: chọn loại i; đang cầm khối → cầm loại mới.</summary>
     public void SelectMaterial(int i)
     /// <summary>Việc chuột phải sẽ làm ở chỗ này (không làm gì). hasCell = Pick trúng địa hình; drop / storageId từ tia (null / -1 nếu không).</summary>
     public RightClickAction Preview(bool hasCell, Vector3Int digCell, Vector3Int placeCell, DropStack drop = null, int storageId = -1)
     /// <summary>Chuột phải: Preview rồi làm ngay hoặc đi tới 2 ô rồi làm (WalkToAct). Trả việc đã chọn; None / OutOfReach → không làm gì, không đổi dòng kết quả.</summary>
     public RightClickAction RightClickAt(bool hasCell, Vector3Int digCell, Vector3Int placeCell, DropStack drop = null, int storageId = -1)
     /// <summary>Click trái lên ô ruộng ở cell: đi tới 2 ô rồi mở bảng ô ruộng. false nếu không có Sim / không phải ô ruộng.</summary>
     public bool ViewPlotAt(Vector3Int cell)
     /// <summary>Tô / bản mờ / vòng tầm theo việc a (quyết định 9–10).</summary>
     public void ShowAim(RightClickAction a, bool hasCell, Vector3Int digCell, Vector3Int placeCell)
     public void HideAim()   // tắt lớp đỏ, bản mờ, vòng tầm
     ```
   - **`ToggleMode`:**
     ```csharp
     if (Observation != null && Observation.Engaged) return;
     if (m == ControlMode.Till && !TillAvailable) return;
     Farming.ExitSowMode();
     Walk.Cancel(PlayerCharacter.Active, true);
     Control.ToggleMode(m);
     ```
   - **`CycleTempHold`:**
     ```csharp
     if (Observation != null && Observation.Engaged) return;
     if (Farming.SowCrop >= 0) { Farming.ExitSowMode(); return; }
     Farming.CancelSelfTill();
     Walk.Cancel(PlayerCharacter.Active, true);
     if (!Control.HasHeld) Control.Hold(ItemId.Block(SelectedMaterial));
     else if (Control.Held.Kind == ItemKind.Block) Control.Hold(ItemId.Tool(ToolItem.ObservationCamera));
     else Control.ClearToDefault();
     ```
   - **`SelectMaterial(int i)`:** `selected = i;` rồi nếu `Control.HasHeld && Control.Held.Kind == ItemKind.Block` → `Control.Hold(ItemId.Block(Palette[i]))`. Alt+lăn (`:108–113`) gọi `SelectMaterial` với chỉ số mới.
   - **`Preview`:**
     ```csharp
     if (!hasCell && drop == null && storageId < 0) return RightClickAction.None;
     var player = PlayerCharacter.Active;
     var reachCell = drop != null ? new Vector3Int(drop.X, drop.Y, drop.Z) : storageId >= 0 ? StorageCell(storageId) : digCell;
     var t = new RightClickTarget
     {
         InTargetReach = player == null || player.InTargetReach(reachCell),
         HeldUsable = hasCell && HeldUsableAt(placeCell),
         ModeUsable = hasCell && ModeUsableAt(digCell),
         RipeCrop = hasCell && Farming.RipePlotAt(digCell) >= 0,
         Storage = storageId >= 0,
         Drop = drop != null,
     };
     var a = RightClick.Resolve(Control, t);
     if (a == RightClickAction.None && hasCell && Farming.SowCrop >= 0 && !Control.HasHeld) a = RightClickAction.UseHeld; // chế độ gieo cũ = cầm hạt (c2 thay)
     return a;
     ```
     - `StorageCell(sid)` = `Farm.Sim.State.Storages[sid].Cell` đổi sang `Vector3Int`.
     - `ModeUsableAt(digCell)`: `Control.Mode` = Dig → true; Till → `Farm != null && Farm.CanTillAt(digCell)`; Rain → true; Default → false.
     - `HeldUsableAt(placeCell)`:
       - không cầm → false;
       - khối → `view.Editor != null && view.Editor.CheckPlace(placeCell.x, placeCell.y, placeCell.z, Control.Held.Material) == EditResult.Ok`;
       - `ItemKind.Tool` → true;
       - loại khác → false.
     - Ngoài tầm mà đang gieo: `Resolve` đã trả `OutOfReach` trước, nên dòng gieo không đè.
   - **`RightClickAt`:**
     ```csharp
     if (Observation != null && Observation.Engaged) return RightClickAction.None;
     var a = Preview(hasCell, digCell, placeCell, drop, storageId);
     if (a == RightClickAction.None || a == RightClickAction.OutOfReach) return a;
     var player = PlayerCharacter.Active;
     if (Farming.TillState != SelfTillState.None) Farming.CancelSelfTill();
     bool camera = Control.HasHeld && Control.Held.Kind == ItemKind.Tool;
     var material = Control.HasHeld && Control.Held.Kind == ItemKind.Block ? Control.Held.Material : SelectedMaterial;
     if (a == RightClickAction.Till && IsUntilledPlot(digCell))
     {
         Walk.Cancel(player, false);
         Execute(a, digCell, placeCell, drop, storageId, camera, material); // tự xới tự tìm đường (brief 0041)
         return a;
     }
     var cell = a == RightClickAction.PlaceBlock ? placeCell
         : drop != null && a == RightClickAction.PickDrop ? new Vector3Int(drop.X, drop.Y, drop.Z)
         : storageId >= 0 && a == RightClickAction.OpenStorage ? StorageCell(storageId) : digCell;
     Walk.Begin(player, cell, a == RightClickAction.Dig, () => Execute(a, digCell, placeCell, drop, storageId, camera, material));
     return a;
     ```
     `IsUntilledPlot(cell)` = `Farm != null`, `Farm.Sim.PlotAt(new CellPos(...)) >= 0` và ô đó `!Tilled`.
   - **`Execute(RightClickAction a, Vector3Int digCell, Vector3Int placeCell, DropStack drop, int storageId, bool camera, BlockMaterial material)`** (private):
     - Dig → code Đào cũ (`:295–299`) đưa vào `DoDig(digCell)`;
     - Till → code Xới cũ (`:281–291`) đưa vào `DoTill(digCell)` (vẫn ghi `LastTill`, `lastResult`, gọi `BeginSelfTill` khi `SelfTill`);
     - Rain → `Farming.CastRain(digCell)`;
     - Harvest → `int id = Farming.RipePlotAt(digCell); if (id >= 0) Farming.Harvest(id);`;
     - OpenStorage → `lastResult = Farm.OpenStockAt(storageId) ? T("edit.open_stock") : Format("edit.stock_far", PlayerRules.TargetReach);`;
     - PickDrop → `CollectDrop(drop)`;
     - PlaceBlock → `var p = view.Place(placeCell, material); lastResult = p == EditResult.Ok ? Format("edit.place_ok", Name(material), placeCell) : Format("edit.place_fail", Name(material), Explain(p));`;
     - UseHeld → `camera ? PlaceObservationCamera(digCell) : Farming.SowAt(digCell)` (bỏ giá trị trả về; gieo xong thì `lastResult = ""` như nhánh gieo cũ `:268`).
   - **`PlaceObservationCamera`** `:202` → `player.InTargetReach(cell)`.
   - **`ViewPlotAt`:**
     ```csharp
     if (Farm == null) return false;
     int id = Farm.Sim.PlotAt(new CellPos(cell.x, cell.y, cell.z));
     if (id < 0) return false;
     Walk.Begin(PlayerCharacter.Active, cell, false, () => Farm.OpenPlotPanel(id));
     return true;
     ```
   - **`ShowAim` / `HideAim`:**
     - `HideAim`: `highlightRenderer.enabled = false; ghostRenderer.enabled = false; PlayerCharacter.Active?.Markers.ShowReachRing(false)`. Dùng `if (p != null)`, không dùng `?.` với UnityEngine.Object.
     - `ShowAim`:
       1. gọi `HideAim()`; `view.Editor == null` → return;
       2. `bool active = Control.Mode != ControlMode.Default || Control.HasHeld || Farming.SowCrop >= 0;` → có nhân vật thì `Markers.ShowReachRing(active && a == RightClickAction.OutOfReach)`;
       3. Dig / Till / Rain / UseHeld → bật lớp đỏ ở `digCell`;
       4. PlaceBlock → bản mờ ở `placeCell`, `GhostColor = GhostColorOf(Control.Held.Material)` (đặt `_BaseColor` như code cũ `:258–261`);
       5. `a == None`, `hasCell`, đang cầm khối, (không có nhân vật hoặc `InTargetReach(digCell)`), `CheckPlace(placeCell, Held.Material) == EditResult.NoMaterial` → bản mờ màu `MissingColor`.
   - **`Update` mới** (thay `:84–181`), theo đúng thứ tự:
     ```csharp
     AimedDrop = null;
     AimedHarvest = -1;
     var player = PlayerCharacter.Active;
     if (Observation.Engaged) { Walk.Cancel(player, false); HideAim(); return; }
     if (Walk.Tick(player)) lastResult = T("farm.unreachable");
     if (GameInput.SelectBlock1.WasPressedThisFrame()) SelectMaterial(0);
     // … SelectBlock2–4 → SelectMaterial(1..3)
     if (player != null && !player.FlyMode)
     {
         if (GameInput.ModeDefault.WasPressedThisFrame()) ToggleMode(ControlMode.Default);
         if (GameInput.ModeDig.WasPressedThisFrame()) ToggleMode(ControlMode.Dig);
         if (GameInput.ModeTill.WasPressedThisFrame()) ToggleMode(ControlMode.Till);
         if (GameInput.ModeRain.WasPressedThisFrame()) ToggleMode(ControlMode.Rain);
     }
     if (GameInput.CycleTool.WasPressedThisFrame()) CycleTempHold();
     if (GameInput.Back.WasPressedThisFrame())
     {
         if (Farming.SowCrop >= 0) Farming.ExitSowMode();
         else if (Control.HasHeld) Control.ClearToDefault();
     }
     if (GameInput.Collect.WasPressedThisFrame()) CollectNearby();
     ```
     - Tiếp theo: chặn UI (giữ điều kiện `:100`; `ShowAim(false, …)` → `HideAim()`), rồi Alt+lăn (dùng `SelectMaterial`). Giữ return khi `!hasPointer || cam == null || view.Editor == null`.
     - Sau đó:
     ```csharp
     var ray = cam.ScreenPointToRay(pointer);
     AimedDrop = PickDrop(ray);
     int sid = -1;
     if (Farm != null && Physics.Raycast(ray, out var sh, maxDistance)) sid = Farm.StorageIdOf(sh.collider);
     bool hasCell = Pick(ray, out var digCell, out var placeCell);
     AimedHarvest = hasCell ? Farming.RipePlotAt(digCell) : -1;
     if (GameInput.MoveClick.WasPressedThisFrame())
     {
         Walk.Cancel(player, false);
         if (hasCell && AimedDrop == null && sid < 0) ViewPlotAt(digCell);
     }
     var a = Preview(hasCell, digCell, placeCell, AimedDrop, sid);
     ShowAim(a, hasCell, digCell, placeCell);
     if ((player == null || player.FlyMode) && DebugFlyCamera.IsMoveKeyHeld()) return;
     if (GameInput.UseTool.WasPressedThisFrame()) RightClickAt(hasCell, digCell, placeCell, AimedDrop, sid);
     ```
   - **`CollectNearby`:** chuỗi `edit.collect_none` truyền `PlayerRules.CollectReach`. Summary `:16–20`, `:331`, `:319`, `:342` sửa "8 ô" / "tầm việc tay" theo tầm mới.
   - Summary lớp `:11–22`: thêm dòng `/// World 7l c1 (brief 0058): chế độ D/Q/E/R (ControlState), chuột phải theo RightClick.Resolve trong 10 ô rồi tự đi tới 2 ô (WalkToAct), lớp đỏ + vòng tầm, F Thu 2 ô, click trái ô ruộng → bảng; Tab cầm tạm khối / camera quan sát tới c2.` Xoá các câu nói Tab đổi công cụ / khung trắng / 8 ô đã sai.
   - Xong khi: biên dịch không lỗi; `grep -n "EditTool\|ToggleTool\|UseTool(\|outline\|InToolReach\|HandReach\|ToolReach" client/Assets/Game/Client` không còn kết quả. `GameInput.UseTool` (action chuột phải) vẫn còn — đúng.
9. **vi.json** (`Client/Resources/Lang/vi.json`, giữ định dạng `{ "key": …, "text": … }`, thứ tự khoá theo bảng chữ cái như file đang có):
   - **Xoá:** `hud.tool`, `hud.tool_camera`, `hud.tool_dig`, `hud.tool_place`, `hud.tool_till`.
   - **Thêm** (text nguyên văn):

     | key | text |
     |---|---|
     | `hud.mode` | `Chế độ: <b><color=yellow>{0}</color></b> (D mặc định · Q đào · E xới · R tưới)` |
     | `hud.mode_default` | `Mặc định` |
     | `hud.mode_dig` | `Đào` |
     | `hud.mode_till` | `Xới` |
     | `hud.mode_rain` | `Tưới (Mưa)` |
     | `hud.held` | ` · Cầm: <b><color=yellow>{0}</color></b> (Tab đổi · Esc bỏ)` |
     | `hud.held_camera` | `Camera quan sát` |
     | `plot.unmark` | `Bỏ đánh dấu` |

   - **Đổi text** (nguyên văn; `\n` là ký tự xuống dòng trong JSON như file đang có):
     - `hud.help_walk`: ` · Chuột trái: đi tới chỗ click (giữ để đi theo con trỏ) · click trái ô ruộng: xem bảng · A: nhảy · Shift: lướt · Chuột phải: làm việc của chế độ / thu hoạch / mở kho / nhặt (chọn trong {0:0.#} ô, tự đi tới {1:0.#} ô rồi làm) · F: thu vật rơi (trong {2:0.#} ô) · Tab: cầm tạm khối / camera quan sát · B: túi · 1–4 / Alt + lăn chuột: chọn loại khối · giữ chuột giữa: xoay · lăn chuột: gần/xa · Space: camera về sau lưng · X: góc nhìn thứ nhất · F3: camera bay\n`
     - `hud.help_fly`: ` · Chuột phải: làm việc của chế độ (trong {0:0.#} ô quanh nhân vật, làm ngay không đi tới) · F: thu vật rơi (trong {1:0.#} ô) · 1–4 / Alt + lăn chuột: chọn loại khối · Camera bay: WASD, Q/E, giữ chuột giữa (phím chế độ tắt khi bay) · dừng bay mới đào/đặt được · F3: về nhân vật\n`
     - `hud.help_idle`: ` · Không có nhân vật: phím chế độ tắt · Tab: cầm tạm khối / camera quan sát · 1–4 / Alt + lăn chuột: chọn loại khối · WASD, Q/E, giữ chuột giữa: camera · dừng bay mới đào/đặt được\n`
     - `hud.till_help`: `Xới: chuột phải lên đất/cỏ = đánh dấu · lên biển = tự đi tới xới · lên ô ruộng = chọn giống · click trái ô ruộng: bảng (nút Bỏ đánh dấu) · K: kho\n`
   - `HelpText()`: `help_walk` truyền `PlayerRules.TargetReach, PlayerRules.ActReach, PlayerRules.CollectReach`; `help_fly` truyền `PlayerRules.TargetReach, PlayerRules.CollectReach`.
   - `HudText()` dòng đầu:
     ```csharp
     var text = Format("hud.mode", T(Control.Mode switch { ControlMode.Dig => "hud.mode_dig", ControlMode.Till => "hud.mode_till", ControlMode.Rain => "hud.mode_rain", _ => "hud.mode_default" }))
         + (Control.HasHeld ? Format("hud.held", Control.Held.Kind == ItemKind.Tool ? T("hud.held_camera") : ItemLabel(Control.Held)) : "")
         + HelpText();
     ```
     `hud.till_help` khi `Control.Mode == ControlMode.Till`; `hud.camera_help` khi đang cầm camera (`Held.Kind == ItemKind.Tool`). Phần còn lại giữ nguyên.
   - Xong khi: `python3 -c "import json;json.load(open('client/Assets/Game/Client/Resources/Lang/vi.json'))"` chạy được; `grep -rn '"hud.tool' client/Assets/Game` không còn.
10. **Test mới** `Tests/Client/ControlModePlayTests.cs`:
    - Dựng scene / nhân vật / Sim theo mẫu `SetUp` của `Tests/Client/PlayerFarmPlayTests.cs:22–80` (đồng bằng phẳng, `player.Place(...)`).
    - Chờ có điều kiện: dùng mẫu `WaitSeconds(float s, Func<bool> until)` (`PlayerFarmPlayTests.cs:131`), timeout 5 giây.
    - Test case (tên đúng như dưới):
      - `ModeKeysToggleAndTabCycle`:
        - `ToggleMode(Dig)` → Mode Dig; gọi lại → Default;
        - `ToggleMode(Till)` rồi `ToggleMode(Rain)` → Rain;
        - `CycleTempHold` ×3 → cầm khối (Material = `SelectedMaterial`) → cầm `ItemKind.Tool` → không cầm;
        - đang cầm khối gọi `SelectMaterial(2)` → `Held.Material == BlockMaterial.Stone`;
        - đang cầm khối gọi `ToggleMode(Dig)` → không cầm, Mode Dig.
      - `DigWalksToTwoCellsThenDigs`:
        - nhân vật trên đất phẳng; ô đích = khối mặt đất cách 6 ô theo x;
        - Dig mode; `RightClickAt(true, cell, cell + Vector3Int.up)` → trả `Dig`, `Walk.State == Walking`, khối còn;
        - chờ `Walk.State == Done` → khối đã đào, `player.InDigReach(cell)` đúng.
      - `OutOfTargetReachDoesNothingAndShowsRing`:
        - ô cách 12 ô, Dig mode → `RightClickAt` trả `OutOfReach`, `player.ClickMove == None`, khối còn;
        - `ShowAim(OutOfReach, true, cell, …)` → `ReachRingVisible` đúng, `HighlightVisible` sai;
        - `ToggleMode(Default)` rồi `ShowAim(Preview(...), …)` → `ReachRingVisible` sai.
      - `HighlightInRangeDefaultNone`:
        - Dig mode, ô cách 5 ô → `Preview` = Dig, `ShowAim` → `HighlightVisible`, `HighlightCell == cell`;
        - Default mode → `Preview` = None, `HighlightVisible` sai.
      - `BlockedWalkIsUnreachable`:
        - dựng tường cao 3 ô (đặt khối bằng `view.Editor`) giữa nhân vật và ô đích; ô đích cách 6 ô;
        - Dig → chờ `Walk.State == Unreachable` → khối đích còn, `HudText()` chứa `Không tới được ô này`.
      - `ModeKeyCancelsWalkAndStops`: đang Walking (Dig, ô 6 ô) → `ToggleMode(Dig)` → `Walk.State == Cancelled`, `player.ClickMove != Walking`, khối còn sau 1 giây.
      - `TillMarksAfterWalking`: Till mode, ô đất trống cách 6 ô → `RightClickAt` → chờ Done → `Farm.Sim.PlotAt(cell) >= 0` (đã đánh dấu).
      - `RainAfterWalking`: Rain mode, khối mặt trên là Đất (`BlockMaterial.Dirt`, đặt bằng `view.Editor` nếu cần) cách 6 ô → chờ Done → `Farming.RainCooldown.Ready == false`.
      - `TempHoldPlacesBlockAfterWalking`:
        - `CycleTempHold` (cầm khối Đất, túi có Đất), `placeCell` trống cách 6 ô;
        - `RightClickAt(true, digCell, placeCell)` trả `PlaceBlock` → chờ Done → ô `placeCell` là Đất.
      - `TempHoldCameraPlacesAfterWalking`: cầm camera (Tab ×2), ô cách 6 ô → trả `UseHeld` → chờ Done → `Observation` đã có điểm đặt. Dùng cách kiểm "đã đặt" mà `ObservationCameraPlayTests` đang dùng.
      - `CollectTakesOnlyWithin2`: 2 chồng vật rơi cách 1,5 ô và 3 ô (`view.Editor.Drops.Add`) → `CollectNearby()` trả 1, chồng xa còn.
      - `RightClickDropWalksThenPicks`: chồng cách 6 ô, Default mode → `RightClickAt(false, default, default, stack)` trả `PickDrop` → chờ Done → chồng đã vào túi.
      - `LeftClickPlotOpensPanelWithUnmark`:
        - đánh dấu ô (Till mode, đứng gần) → Default; `ViewPlotAt(cell)` khi cách 6 ô → `Farm.OpenPlotId < 0` ngay sau gọi;
        - chờ `Farm.OpenPlotId >= 0` → `Farm.UnmarkOpenPlot()` trả true → biển ẩn (`Farm.SignVisible(id)` sai), `OpenPlotId == -1`.
      - `UnmarkOpenPlotCancelsSelfTill`:
        - ô có biển, `Farming.BeginSelfTill(id)` (đang Walking) → `Farm.OpenPlotPanel(id)` → `UnmarkOpenPlot()`;
        - → `Farming.TillState == None`; thay test cũ `UnmarkAimedCancelsSelfTill`.
      - `DigModeOnRipePlotDigsIt`: ô ruộng chín (cách mẫu `HarvestIntoBag` trong `PlayerFarmPlayTests.cs:353` làm chín), đứng trong 2 ô → Dig mode, `Preview` = Dig; Default mode `Preview` = Harvest.
      - `SowModeWalksThenSows`:
        - ô ruộng đã xới trống cách 6 ô, `Farming.EnterSowMode(0)` → `Control` về Default;
        - `RightClickAt` trả `UseHeld` → chờ Done → túi giảm 1 hạt.
11. **Sửa test cũ** cho đúng ý nghĩa mới (không xoá ý nghĩa, chỉ đổi cách gọi / khoảng cách). Quy tắc chung:
    - `ToggleTool` → `ToggleMode(...)` / `CycleTempHold()`;
    - `Tool == EditTool.X` → `Control.Mode` / `Control.Held`;
    - `UseTool(dig, place)` → `RightClickAt(true, dig, place)`, nhân vật đặt **trong 2 ô** nếu test cần làm ngay, hoặc chờ `Walk.State == Done`;
    - `OutlineVisible` → `HighlightVisible`;
    - `ShowAim(true, d, p)` → `ShowAim(Preview(true, d, p), true, d, p)`;
    - kiểm tầm 8 → tầm mới (10 thao tác / 2 thực hiện / 2 Thu).

    Danh sách:

    | File | Test |
    |---|---|
    | `Tests/Client/TerrainEditPlayTests.cs` | `ToolStartsDigAndTabToggles` → `ModeStartsDefault` (Mode Default, không cầm); `DigToolShowsOutlineOnly`; `PlaceToolShowsGhostAtPlaceCell`; `GhostRedWhenOutOfMaterial`; `GhostHiddenWhenRuleFails`; `UseToolDigsOrPlacesByTool`. **Giữ 2 dòng chưa commit** ở `TimingSingleChunkEdits` |
    | `Tests/Client/SimFarmUiPlayTests.cs` | `TabCyclesFourToolsWithSim` → `TillModeOnlyWithSim` (có Sim: `ToggleMode(Till)` → Till; dựng không Sim theo cách test đang làm: → vẫn Default); `MarkShowsSignSameFrame`; `SignHidesWhenTilled`; `ShowAimTillOutline` |
    | `Tests/Client/PlayerFarmPlayTests.cs` | `SelfTillFull`; `CancelWhileTilling`; `TabCancelsWalking` (Tab = `CycleTempHold` vẫn huỷ tự xới); `CreatureTillsFirst`; `RainRules` (đổi `CastRain` key → gọi `Farming.CastRain` như cũ, tầm ngoài = > 10 ô); `HarvestIntoBag` (vị trí `cell2.x + 12.5f` > 10 ô → giữ ý "ngoài tầm không ngắm"); `HudInitial` (chữ HUD mới: chứa `Chế độ:` và `Mặc định`) |
    | `Tests/Client/PlayerBagSowPlayTests.cs` | `EnterSowModeRules`; `SowIdlePlot`; `SowModeDoesNotUseTool` → `SowModeIgnoresDigMode` (đang gieo: chuột phải gieo, không đào); `ExitSow` (Tab / Esc / phím chế độ thoát gieo); `ClickStorageBlock` (`OpenStockAt` trong 10 ô); `UnmarkAimedCancelsSelfTill` → xoá (thay bằng test mới ở bước 10); `ArrivedClearsWalkingLine` |
    | `Tests/Client/PlayerCharacterPlayTests.cs` | `ReachLimitsTools` → `ReachLimitsRightClick` (ngoài 10 ô → `OutOfReach`, không lớp đỏ; trong 10 ô → đi tới); `FlyModeToggle` (`InToolReach` → `InTargetReach`) |
    | `Tests/Client/DropPlayTests.cs` | `StarterBagNearSpawnAndHud`; `CollectNearbyTakesOnlyWithin8` → `…Within2`; `ClickPicksOnlyInReachAndFarDoesNotBlockDig` (tầm ngắm vật rơi 10); `DigDropsVisibleThenCollectAndPlace` |
    | `Tests/Client/ObservationCameraPlayTests.cs` | `CameraToolCycleAndClickPlaces` → `TabHoldCameraAndRightClickPlaces`; `ToolsAndMovementLockedWhileViewing` (đang xem: `RightClickAt` trả None, `ToggleMode` / `CycleTempHold` không đổi gì). Các test khác gọi `PlaceObservationCamera` giữ nguyên nếu vẫn qua |
    | `Tests/Core/PlayerRulesTests.cs` | `Configure_ChangesReach` (bỏ `toolReach`/`handReach`, kiểm bằng `targetReach`/`collectReach`); xoá `InToolReach_BoundaryExactly8`, `InToolReach_8Point1Outside`, `InHandReach_BoundaryExactly8`, `InHandReach_8Point1Outside` (đã có `NewReaches_Boundary`) |
    | `Tests/Core/DropFieldTests.cs` | `CollectInReachTakesOnlyWithin8` → `…Within2`; `TryCollectSingleStack` (tầm 10); `SeedBagsMergeIntoBagSeeds`, `VersionIncrementsOnChanges` (đặt chân trong 2 ô nếu đang xa hơn) |
    | `Tests/Core/DropPickTests.cs` | `OutOfHandReachIgnored` → `OutOfTargetReachIgnored` (chồng ngoài 10 ô bị bỏ, trong 10 ô được chọn); test khác giữ |
    | `Tests/Core/Sim/SimConfigTests.cs` | `:33–34` xoá 2 assert; `:86–89` xoá 4 dòng `Bad(...)` |
    | `Tests/Core/Sim/PlayerFarmTests.cs` | `StorageInReach` (biên 10) |
    | `Tests/Core/StarterBagTests.cs` | `SpotThreeAheadOnFlatGround`: `CollectInReach` từ chân cách túi 3 ô → đổi chân sang ô cạnh túi rồi Thu (vị trí đặt túi không đổi) |
    | `Tests/Core/WorldEditorTests.cs` | `DigThenPlaceRoundTrip` (chân trong 2 ô của vật rơi nếu đang xa hơn) |

    Test nào khác hỏng vì đổi ở brief này → sửa theo cùng quy tắc, ghi tên vào Kết quả. Hỏng mà không thuộc quy tắc → dừng, `cần Opus`.

## Tiêu chí xong
- EditMode + PlayMode qua hết, 0 lỗi console (qua MCP, xem "Lệnh executor chạy").
- `grep -rn "ToolReach\|HandReach\|toolReach\|handReach\|InToolReach\|InHandReach\|EditTool\|ToggleTool\|CastRain\|\"hud.tool" client/Assets/Game design/data` → không kết quả.
- `diff design/data/sim_m1.json client/Assets/Game/Client/Resources/Config/sim_m1.json` → không khác.
- `Game.Core` không thêm `using UnityEngine` (`grep -rn "UnityEngine" client/Assets/Game/Core` như trước brief).
- `python3 tools/check_design.py` exit 0.
- Mỗi test ở bước 10 có trong file mới và qua.

## Test sau khi xong

### Phần cần test
- Phím chế độ + Tab tạm (bước 5, 8) · `ControlModePlayTests.ModeKeysToggleAndTabCycle` · bấm lại tắt, Tab vòng 3 trạng thái, 1–4 đổi khối đang cầm.
- Bảng phím (bước 5) · `GameControlsTests` (EditMode) · 36 hành động, D/Q/E/R đúng, không trùng phím trong map.
- Đi tới rồi đào (bước 3, 8) · `DigWalksToTwoCellsThenDigs` · đào sau khi vào 2 ô.
- Ngoài 10 ô + vòng tầm (bước 4, 8) · `OutOfTargetReachDoesNothingAndShowsRing` · không đi, không đào, vòng chỉ hiện khi có chế độ.
- Lớp đỏ (bước 8) · `HighlightInRangeDefaultNone` + test TerrainEdit đã sửa · tô đúng ô; Mặc định không tô.
- Kẹt → Không tới được (bước 3) · `BlockedWalkIsUnreachable` · dòng HUD đúng, khối còn.
- Huỷ bằng phím chế độ (bước 3, 8) · `ModeKeyCancelsWalkAndStops`.
- Xới / Tưới / Đặt / Camera / Gieo sau khi đi tới (bước 6, 8) · `TillMarksAfterWalking`, `RainAfterWalking`, `TempHoldPlacesBlockAfterWalking`, `TempHoldCameraPlacesAfterWalking`, `SowModeWalksThenSows`.
- F Thu 2 ô (bước 1, 8) · `CollectTakesOnlyWithin2` + `DropFieldTests.CollectInReachTakesOnlyWithin2`.
- Chuột phải vật rơi (bước 1, 8) · `RightClickDropWalksThenPicks` + `DropPickTests.OutOfTargetReachIgnored`.
- Click trái ô ruộng + Bỏ đánh dấu (bước 7, 8) · `LeftClickPlotOpensPanelWithUnmark`, `UnmarkOpenPlotCancelsSelfTill`.
- Thứ tự ưu tiên: Đào lên ô ruộng chín (bước 8) · `DigModeOnRipePlotDigsIt`.
- Gỡ tầm cũ + JSON (bước 1) · `PlayerRulesTests`, `SimConfigTests` (gồm `ConfigMatchesCodeDefaults`), `ConfigTests` (2 bản JSON giống hệt), `PlayerFarmTests.StorageInReach`, `StarterBagTests`, `WorldEditorTests`.
- Chuỗi (bước 9) · `PlayerFarmPlayTests.HudInitial` + test chuỗi / ngôn ngữ đang có (`PlayerSettingsTests.Language`, test kiểm khoá vi.json nếu có) · không thiếu khoá, HUD có `Chế độ:`.
- Regression camera quan sát (đã duyệt 0057) · toàn bộ `ObservationCameraPlayTests` · vẫn qua.
- Regression tự xới, gieo, kho, vật rơi, click di chuyển · `PlayerFarmPlayTests`, `PlayerBagSowPlayTests`, `DropPlayTests`, `ClickMovePlayTests`, `CameraPcPlayTests`, `SimFarmUiPlayTests`, `TerrainEditPlayTests`, `EditOcclusionPlayTests` · qua hết.
- Không đọc thẳng phím · `GameControlsTests.NoDirectInputReadsInScope` · qua.

### Lệnh executor chạy
1. Grep ở "Tiêu chí xong" + `diff` 2 bản JSON + `python3 -c "import json;json.load(open('client/Assets/Game/Client/Resources/Lang/vi.json'))"`.
2. Unity Editor đang mở qua MCP ([0010](../../design/decisions/0010-mcp-for-unity.md)):
   - `refresh_unity` → `read_console` (0 lỗi biên dịch);
   - `run_tests` EditMode (toàn bộ) → `run_tests` PlayMode (toàn bộ);
   - `read_console` lỗi sau khi chạy.
   - Ghi dòng tổng kết số test thật.
   - Editor tắt → `sh "<Plugin root>/scripts/run.sh" verify_run client_test client_test_play`.
3. **Dọn:** sau `run_tests` PlayMode → `git checkout -- client/ProjectSettings/EditorSettings.asset`.
4. `sh "<Plugin root>/scripts/run.sh" verify_run --all` trước commit.
5. Commit (code + test + 2 dòng chờ trong `TerrainEditPlayTests.cs`), message `brief 0058: 7l c1 action modes, right-click walk-to-act, red highlight, reach ring, F collect 2, plot unmark`; commit kết quả brief riêng `brief 0058: result`. Không push.

### User tự test
Mở scene SimView trên Mac, Play:
1. **Chế độ:**
   - bấm Q → HUD `Chế độ: Đào`; bấm Q lần nữa → `Mặc định`;
   - E → Xới, R → Tưới (Mưa), D → Mặc định.
2. **Lớp đỏ:**
   - Q, di chuột lên mặt đất gần → mặt khối dưới con trỏ phủ đỏ nhạt;
   - không còn hộp viền trắng;
   - D → không tô.
3. **Vòng tầm:** Q, di chuột ra xa > 10 ô → vòng tròn mờ quanh nhân vật, không tô khối; đưa con trỏ lại gần → vòng tắt.
4. **Đi tới rồi làm:**
   - Q, chuột phải khối cách ~6 ô → nhân vật đi thẳng tới, dừng cách ~2 ô rồi đào;
   - chuột phải sau bức tường cao → nhân vật kẹt rồi HUD `Không tới được ô này`.
5. **Huỷ:** đang đi tới đào → click trái chỗ khác → nhân vật đổi hướng, không đào. Đang đi tới → bấm Q → dừng tại chỗ.
6. **Xới / Tưới:**
   - E chuột phải đất → đi tới, cắm biển;
   - E chuột phải biển → tự đi tới xới như cũ;
   - R chuột phải ô ruộng / đất → đi tới, Mưa.
7. **Mặc định:**
   - chuột phải cây chín → đi tới, thu hoạch;
   - chuột phải kho → đi tới, mở kho;
   - chuột phải vật rơi → đi tới, nhặt.
8. **Đào lên ô ruộng:** Q chuột phải ô ruộng có cây chín → đào (không thu hoạch).
9. **F:** F khi vật rơi cách 1–2 ô → nhặt; cách 3–5 ô → không nhặt.
10. **Ô ruộng:**
    - click trái ô có biển từ xa → nhân vật đi tới, bảng ô ruộng hiện;
    - nút `Bỏ đánh dấu` bấm được → biển mất, bảng đóng;
    - ô đã xới: nút mờ.
11. **Tab tạm:**
    - Tab → `Cầm: Đất`, bản mờ khối; chuột phải → đi tới, đặt;
    - 1–4 đổi loại;
    - Tab → `Cầm: Camera quan sát`, chuột phải khối → đi tới, đặt camera; V xem được;
    - Tab → hết cầm. Esc cũng bỏ cầm.
12. **Gieo:** B → bấm túi hạt → chuột phải ô ruộng đã xới cách xa → đi tới, gieo.
13. **Bay F3:** Q/E/D không đổi chế độ (camera bay vẫn dùng); F3 về nhân vật → phím chế độ chạy lại.

## Dừng và chuyển lại Opus khi
- Brief mơ hồ hoặc mâu thuẫn với code thật (vd tên field `Plots[i]` khác, `Storages[i].Cell` không có).
- Cần một quyết định thuộc AGENTS.md §3.4 hoặc ngoài "Quyết định đã chốt".
- Cùng một lỗi sửa 2 lần vẫn chưa qua.
- Phải sửa file ngoài "Phạm vi".
- Test cũ hỏng mà không sửa được theo quy tắc ở bước 11.

---
<!-- Từ đây trở xuống: EXECUTOR ghi. -->

## Kết quả
Base commit: fdb5c0f
Chạy bằng: claude-sonnet-5-5 · medium
Bắt đầu: 2026-10-23 20:32 · kết thúc: 2026-10-23 20:55 (~23 phút thực tế, gồm ~10 phút chờ test Unity)
Commit: a5515ac (34 file: Core tầm, WalkToAct.cs mới, TerrainEditInput viết lại điều khiển, MoveMarkers vòng tầm, SimView OpenPlotPanel/UnmarkOpenPlot + nút, PlayerFarmActions, PlayerCharacter, GameInput + .inputactions, vi.json, 2 JSON, test).
2 dòng chờ trong `TerrainEditPlayTests.cs` (`waterStepsPerSecond = 0f`) đã commit chung.
Lệch so với brief:
- `PlayerFarmPlayTests.SelfTillFull/CancelWhileTilling/CreatureTillsFirst/…` chỉ đổi cách gọi (`RightClickAt`), giữ ý nghĩa.
- Test sửa thêm ngoài danh sách: `TerrainEditPlayTests.EditInputSurvivesBuild` (tìm `Highlight` thay `Selection`), `SimFarmUiPlayTests.ShowAimTillOutline` → `ShowAimTillHighlight`, `TerrainEditPlayTests.UseToolDigsOrPlacesByTool` → `RightClickDigsOrPlacesByModeAndHeld`, `SimFarmUiPlayTests.UnmarkAndDigHideSign` giữ nguyên (qua).
- Chuỗi tiếng Việt trong test mới dùng tiếng Anh (HardcodedTextScanTests).
- Scene `.unity` còn field tuần tự hoá thừa `outlinePixels` (Unity bỏ qua; không sửa vì ngoài phạm vi).
<!-- Base commit · Chạy bằng: <model id> · <effort> · commit · file đã đổi · thời gian bắt đầu/kết thúc (giờ thực tế M1) · lệch so với brief. -->

## Kết quả test
- EditMode (MCP, toàn bộ): 528/528 đạt. PlayMode (MCP, toàn bộ): 213/213 đạt (gồm 16 test mới `ControlModePlayTests`). `read_console` lỗi: 0.
- Phần cần test: phím chế độ + Tab → đạt (`ModeKeysToggleAndTabCycle`); bảng phím → đạt (`GameControlsTests`, 36); đi tới rồi đào → đạt (`DigWalksToTwoCellsThenDigs`); ngoài 10 ô + vòng tầm → đạt (`OutOfTargetReachDoesNothingAndShowsRing`); lớp đỏ → đạt (`HighlightInRangeDefaultNone`, `DigModeShowsRedHighlightOnly`); kẹt → đạt (`BlockedWalkIsUnreachable`); huỷ bằng phím → đạt (`ModeKeyCancelsWalkAndStops`); Xới/Tưới/Đặt/Camera/Gieo sau khi đi → đạt (5 test tương ứng); F Thu 2 ô → đạt (`CollectTakesOnlyWithin2`, `DropFieldTests.CollectInReachTakesOnlyWithin2`); chuột phải vật rơi → đạt; click trái ô ruộng + Bỏ đánh dấu → đạt (2 test); Đào lên ô chín → đạt (`DigModeOnRipePlotDigsIt`); gỡ tầm cũ + JSON → đạt (EditMode); chuỗi → đạt (`HudInitial`, `HardcodedTextScanTests`); regression camera quan sát / tự xới / gieo / kho / vật rơi → đạt (PlayMode toàn bộ).
- Lệnh: grep tiêu chí → không kết quả (trừ `Farming.CastRain` là hàm, không phải key input); `diff` 2 JSON → giống; vi.json parse được; `Game.Core` không có `UnityEngine`; `check_design.py` exit 0; `verify_run design asset_check` exit 0 (client_test/client_test_play chạy qua MCP vì Editor đang mở).
- Dọn: `git checkout -- client/ProjectSettings/EditorSettings.asset` đã chạy, sạch.
- Chờ user xem: toàn bộ mục "User tự test" (hình ảnh lớp đỏ / vòng tầm, cảm giác đi tới, HUD).

## Vướng mắc

---
<!-- PLANNER ghi khi review. -->

## Review
- Planner Opus · 2026-10-23. `Chạy bằng` claude-sonnet-5-5 · medium = `Model thực thi` → khớp. Mọi dòng "Phần cần test" có kết quả (đạt).
- `git show --stat a5515ac`: 34 file, đều trong Phạm vi (+ test sửa thêm đã ghi ở Kết quả, chỉ đổi cách gọi / tên). Không đụng `ControlState.cs`, camera, `design/` ngoài `sim_m1.json`.
- Grep tiêu chí: còn `CastRain` nhưng là hàm `PlayerFarmActions.CastRain`, không phải key input — mẫu grep của brief rộng quá, chấp nhận. `diff` 2 JSON → giống [run].
- Đối chiếu Quyết định đã chốt (đọc `TerrainEditInput.cs`, `WalkToAct.cs`, `SimView.cs`, `MoveMarkers.cs`, `PlayerRules.cs`, `DropField.cs`, `DropPick.cs`, `PlayerFarmActions.cs`):
  - QĐ 1 Tab vòng tạm + 1–4 đổi khối đang cầm → đúng (`CycleTempHold`, `SelectMaterial`).
  - QĐ 2 WalkToAct: bay / không nhân vật / đã trong tầm → làm ngay; `ClickMove != Walking` → `farm.unreachable`; Xới lên biển chưa xới → `BeginSelfTill` không đi trước → đúng.
  - QĐ 3 huỷ: chuột phải mới, click trái (không dừng), D/Q/E/R + Tab (dừng), đang xem camera → đúng.
  - QĐ 4 tầm: ô đích theo vật rơi / kho / digCell; lưới an toàn `TargetReach` ở các hàm thực hiện; F dùng `CollectReach`; tầm cũ gỡ hết → đúng.
  - QĐ 5–8 (`ModeUsableAt`, `HeldUsableAt`, gieo = UseHeld chỉ khi `Resolve` None — ngoài tầm đã thành `OutOfReach` trong `ControlState.Resolve`; phím chế độ tắt khi bay; Esc) → đúng.
  - QĐ 9–10 lớp đỏ (GhostBlock, màu, ×1,04, bỏ outline), vòng tầm 64 điểm r=TargetReach nét 0,08 α0,25 → đúng.
  - QĐ 11–12 click trái ô ruộng → đi tới → `OpenPlotPanel`; nút Bỏ đánh dấu bật khi `!Tilled`, huỷ tự xới ô đó, đóng bảng → đúng.
  - QĐ 13 / bước 9 chuỗi vi.json nguyên văn → đúng (đọc bằng python) [run].
- Ghi nhận nhỏ (không chặn): `WalkToAct.Begin` khi ô mới đã trong tầm thì làm ngay nhưng không dừng nhân vật nếu đang đi tới việc cũ → nhân vật đi tiếp tới ô cũ. Brief không quy định; chờ user xem có khó chịu không. Scene còn field thừa `outlinePixels` (Unity bỏ qua).
- User xem 13 mục (2026-10-24): "ok làm tiếp" → đạt. Góp ý đổi hành vi (không phải lỗi so với brief) → đưa vào c2: vòng tầm chỉ hiện khi **bấm chuột phải** ngoài tầm (không hiện khi chỉ di chuột); F hiện vùng 2 ô + hiệu ứng gió hút vật rơi về nhân vật.
- Giờ thực tế: Executor 23 phút → ghi vào world-tech dòng 7l c.
- Kết luận: đã duyệt

## Kiểm chứng
- Chưa kiểm được qua `verify_run client_test client_test_play`: Unity Editor đang mở project (exit 1, "another Unity instance"); theo AGENTS.local §10 đã chạy toàn bộ qua MCP thay thế: EditMode 528/528, PlayMode 213/213, console 0 lỗi (sau commit `a5515ac`, file không đổi thêm).
- `design`, `asset_check` qua `verify_run`: exit 0.
- Checklist: pillar/thành phần/glossary/roadmap/nguồn/phạm vi — không đổi `design/` (ngoài `design/data/sim_m1.json`) → không áp dụng; Game.Core không tham chiếu UnityEngine → đạt (grep rỗng); bước M1 có test → đạt (mục Kết quả test); chỉ làm hạng mục M1 (0007) → đạt; giờ thực tế ~23 phút → ghi ở mục Kết quả.
- Review (Planner, 2026-10-23): `.ai/project.json` đổi ngoài phiên này (thêm `"child_sessions": {"auto_chain": true}`, không phải do review) → `json.load` đọc được [run]; không commit thay user. Checklist cho review: không đổi `design/`, code → pillar/thành phần/glossary/nguồn/ước lượng không áp dụng; Game.Core không có UnityEngine → đạt (grep rỗng ở Kết quả test); test đúng "Xong khi" → đạt về code, phần hình ảnh chờ user xem; chỉ M1 → đạt; giờ thực tế ~23 phút → ghi vào roadmap khi duyệt (chưa duyệt).
