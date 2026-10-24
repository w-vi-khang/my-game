# Brief 0067 — sim 7g: sinh vật chỉ làm công đoạn được giao (vụ tự làm, nút Tiếp tục, xới lại khi được giao)

> Trạng thái: chờ review
> Review: đầy đủ + user xem
> Model thực thi: Sonnet · medium
> Cập nhật: 2026-10-24

<!-- Planner (Opus) viết, Executor (Sonnet) làm theo ở PHIÊN KHÁC — executor không có lịch sử chat, không được tự quyết. -->

## Mục tiêu
Bước **sim 7g** (brief duy nhất) của task [m1-sim-7g-sinh-vat-chi-lam-viec-duoc-giao](../tasks/m1-sim-7g-sinh-vat-chi-lam-viec-duoc-giao.md). Luật: [0036](../../design/decisions/0036-sinh-vat-chi-lam-cong-doan-duoc-giao.md) mục 1–4.
1. Lõi (`Game.Core`): cờ **vụ tự làm** (`SelfMade`) và **được giao 1 công đoạn** (`Granted`) trên `FarmPlot`. Công đoạn của ô vụ tự làm không giao cho sinh vật trừ khi `Granted`. `Simulation.CanContinue` / `Continue`. Người chơi tự xới được ô đứng yên chưa xới lại.
2. Client: nút **Tiếp tục** trên ô tiến trình của bảng ô ruộng (`SimView`).

## Bối cảnh cần đọc
- `client/Assets/Game/Core/Sim/SimData.cs:233-262` — `FarmPlot` (`Slots`, `CurrentCrop`, `TillPaused`, `Phase`, `StageId`, `Tilled`, `SoilReady`, `Removed`). `enum PlotPhase { Idle, Till, Sow, Water, Growing, Harvest }` (dòng 62).
- `client/Assets/Game/Core/Sim/Simulation.cs`:
  - `BeginSow` (382-389), `AbortActive` (392-407), `CancelPlot` (410-420).
  - `OnStageDone(WorkStage s, PlayerBag bag = null)` (744-812): Till → `SoilReady = true`, còn hàng đợi thì `BeginSow`, không thì Idle. Sow → Water. Water → Growing. Harvest → chở về kho (hoặc vào túi), `SoilReady = false`, `CurrentCrop = -1`, còn hàng đợi thì `StartCrop`, không thì Idle.
  - `CropPhase` (814-827), `MarkTill` (849-861), `EnqueueCrop` (888-898), `CancelCurrent` (927-940), `PauseTill`/`ResumeTill` (942-960).
  - `CompleteByPlayer` (971-981): người chơi làm xong công đoạn → `OnStageDone`.
  - `CanPlayerTill` (992-998), `PlayerTill` (1000-1008), `CheckPlayerSow` / `PlayerSow` (1010-1042), `CanPlayerHarvest` / `PlayerHarvest` (1044-1059), `Rain` (1061-).
  - `StartCrop` (1175-1186), `AddPlotStage(FarmPlot p, WorkAbility a)` (1215-1222).
  - `AssignPhase` (1410-): danh sách `open` bỏ qua công đoạn của ô `TillPaused` (dòng 1428-1431):
    ```csharp
    if (g2.Status == StageStatus.Open && g2.AssigneeKind == AssigneeKind.None
        && !(g2.PlotId >= 0 && State.Plots[g2.PlotId].TillPaused)) open.Add(g2);
    ```
- `client/Assets/Game/Core/Sim/SimHash.cs:103-122` — băm từng ô ruộng; dòng 121 `h = MixInt(h, p.SoilReady ? 1 : 0);`.
- `client/Assets/Game/Client/SimView.cs`:
  - `SelfTillOpenPlot` (327-335) dùng `Sim.CanPlayerTill`.
  - `ProgressAction` (345-353), `ClosePlotPanel` (355-360).
  - `DrawFarmGui` — ô tiến trình (dòng 431-443):
    ```csharp
    bool busy = p.Phase != PlotPhase.Idle;
    ...
    if (busy)
    {
        if (p.TillPaused)
        {
            if (GUI.Button(new Rect(cr.x + 4f, cr.y + 25f, 64f, 22f), T("plot.resume"))) ProgressAction();
        }
        else if (GUI.Button(new Rect(cr.xMax - 18f, cr.y, 18f, 18f), "x")) ProgressAction();
    }
    ```
  - Nút "Tự xới" (dòng 466-467) bật theo `Sim.CanPlayerTill(OpenPlotId)`.
- Mẫu test lõi: `client/Assets/Game/Tests/Core/Sim/PlayerFarmTests.cs:11-70`:
  - `Layout(bool creatures, Action<WorldData> tweak = null, int[] crops = null)`: thế giới phẳng, 3 sinh vật L1–L3, 1 kho, `MarkTill(Soil)`, `EnqueueCrop` theo `crops`, `TestWorlds.HomeAll`.
  - Helper `RunTo`, `LastStage`, `CountStages`; `ToHarvest(crop)`.
- Mẫu test PlayMode: `client/Assets/Game/Tests/Client/SimFarmUiPlayTests.cs:18-70` (`SetUp`, `TickUntilTilled`, `OpenPlot0`), `ProgressPauseResumeAndCloseResumes` (190-210).
- Bảng chuỗi `client/Assets/Game/Client/Resources/Lang/vi.json` (key xếp theo chữ cái); `HardcodedTextScanTests.AllUsedKeysExist` kiểm key dùng trong code có trong file.

## Quyết định đã chốt (không được đổi)
- 0036 mục 1–4 (user 2026-10-24), xem task contract.
- Đề xuất Planner, đã ghi trong task contract:
  - **Vụ tự làm** chỉ bật khi `PlayerSow` gieo ô **Idle**. Gieo vào ô đang pha Sow (vụ từ hàng đợi) = chen vào → không bật.
  - Chọn hạt vào hàng đợi trong lúc vụ tự làm không đổi vụ hiện tại.
  - Hết vụ (thu hoạch xong, huỷ vụ, huỷ ô) → tắt vụ tự làm.
  - **Tiếp tục** dùng được khi:
    - (a) vụ tự làm đang chờ tưới hoặc chờ thu hoạch, chưa giao;
    - (b) ô ruộng đứng yên chưa xới lại (`Tilled && Idle && !SoilReady && CurrentCrop < 0`) → tạo công đoạn xới, giao luôn. Áp dụng cho cả ô có hàng đợi vừa hết.
  - Người chơi tự xới được ô (b) qua `CanPlayerTill` / `PlayerTill` (nút "Tự xới" hiện có tự bật).
- `Granted` tắt khi **bất kỳ** công đoạn của ô xong (sinh vật, người chơi, Mưa) → đúng 1 công đoạn.
- Công đoạn bị giữ vẫn `Open` (không `Blocked`, không báo đình trệ); chỉ bị bỏ qua ở `AssignPhase`, như `TillPaused`.
- Băm: trường mới chỉ trộn khi khác mặc định → kịch bản cũ giữ nguyên mã băm.
- Không thêm số mới; không đổi icon / chữ ô tiến trình.
- Review kết luận "code ok, chờ user xem gộp cuối" (user 2026-10-24).

## Phạm vi
- Được sửa/tạo:
  - `client/Assets/Game/Core/Sim/SimData.cs`, `client/Assets/Game/Core/Sim/Simulation.cs`, `client/Assets/Game/Core/Sim/SimHash.cs`
  - `client/Assets/Game/Client/SimView.cs`, `client/Assets/Game/Client/Resources/Lang/vi.json`
  - `client/Assets/Game/Tests/Core/Sim/AssignedWorkTests.cs` (mới) + `.meta` do Unity tạo
  - `client/Assets/Game/Tests/Client/SimFarmUiPlayTests.cs` (thêm test)
  - `.ai/briefs/0067-sim-7g-sinh-vat-chi-lam-viec-duoc-giao.md` (mục Kết quả trở xuống)
- Không được sửa: test cũ (trừ thêm test vào `SimFarmUiPlayTests`), `design/*`, mọi file đang có thay đổi chưa commit của phiên khác (`design/decisions/0038-*`, `design/02-gameplay/*`, `design/05-roadmap.md`, `design/summary.md`, `design/glossary.md`, `design/ideas.md`, `design/decisions/README.md`, `tools/terrain_catalog*`, `.claude/skills/local-terrain-gen/`, `.ai/skill-map.local.json`).

## Các bước
1. **`FarmPlot`** (`SimData.cs`, ngay sau trường `SoilReady`):
   ```csharp
   /// <summary>Vụ tự làm (0036 mục 2): người chơi tự gieo ô đứng yên; công đoạn của vụ chỉ giao sinh vật khi Granted.</summary>
   public bool SelfMade;
   /// <summary>Tiếp tục (0036 mục 1): giao đúng 1 công đoạn kế; tắt khi một công đoạn của ô xong.</summary>
   public bool Granted;
   ```

2. **`Simulation`** (`Simulation.cs`):
   - Thêm hai hàm riêng (đặt cạnh `PlotIntact`):
     ```csharp
     /// <summary>Công đoạn của ô không giao cho sinh vật: tạm dừng xới (0027) hoặc vụ tự làm chưa bấm Tiếp tục (0036).</summary>
     static bool Held(FarmPlot p) => p.TillPaused || (p.SelfMade && !p.Granted);

     /// <summary>Ô ruộng đứng yên chưa xới lại (sau thu hoạch / huỷ vụ, hàng đợi trống) — 0036 mục 2.</summary>
     static bool NeedsRetill(FarmPlot p) => p.Tilled && !p.Removed && p.Phase == PlotPhase.Idle && !p.SoilReady && p.CurrentCrop < 0;
     ```
   - `AssignPhase` dòng 1430: thay `State.Plots[g2.PlotId].TillPaused` bằng `Held(State.Plots[g2.PlotId])`.
   - `OnStageDone`: ngay sau `var p = State.Plots[s.PlotId];` thêm `p.Granted = false;`. Trong `case WorkAbility.Harvest`: ngay sau `p.CurrentCrop = -1;` thêm `p.SelfMade = false;`.
   - `PlayerSow`: trong khối `if (p.Phase == PlotPhase.Idle) { … }`, thêm `p.SelfMade = true;` (trước `AddPlotStage`).
   - `CancelCurrent`: ngay sau `p.CurrentCrop = -1;` thêm `p.SelfMade = false; p.Granted = false;`. `CancelPlot`: ngay sau `p.CurrentCrop = -1;` thêm cùng hai dòng.
   - `CanPlayerTill`: đổi `return` thành `return (p.Phase == PlotPhase.Till && p.StageId >= 0) || NeedsRetill(p);`. Cập nhật chú thích: `/// <summary>Người chơi tự xới được ô plotId không: ô còn, đang ở pha xới (lần đầu hoặc xới lại) có công đoạn, hoặc ô đứng yên chưa xới lại (0036).</summary>`.
   - `PlayerTill`: sau `var p = State.Plots[plotId];` thêm `if (p.Phase == PlotPhase.Idle) { p.Phase = PlotPhase.Till; AddPlotStage(p, WorkAbility.Till); }` (trước `p.TillPaused = false;`).
   - Thêm API công khai (đặt sau `ResumeTill`):
     ```csharp
     /// <summary>Nút Tiếp tục dùng được không (0036 mục 1–2): vụ tự làm đang chờ tưới / thu hoạch chưa giao, hoặc ô đứng yên chưa xới lại.</summary>
     public bool CanContinue(int plotId)
     {
         if (!ValidPlot(plotId)) return false;
         var p = State.Plots[plotId];
         if (p.Granted) return false;
         if (p.SelfMade && p.StageId >= 0 && (p.Phase == PlotPhase.Water || p.Phase == PlotPhase.Harvest)) return true;
         return NeedsRetill(p);
     }

     /// <summary>Tiếp tục (0036 mục 1): giao sinh vật đúng 1 công đoạn kế; ô đứng yên chưa xới lại → mở công đoạn xới. false nếu !CanContinue.</summary>
     public bool Continue(int plotId)
     {
         if (!CanContinue(plotId)) return false;
         var p = State.Plots[plotId];
         if (NeedsRetill(p))
         {
             p.Phase = PlotPhase.Till;
             AddPlotStage(p, WorkAbility.Till);
         }
         p.Granted = true;
         return true;
     }
     ```
   - Xong khi: biên dịch; test bước 6 qua.

3. **`SimHash`** (`SimHash.cs`, ngay sau dòng 121):
   `if (p.SelfMade || p.Granted) h = MixInt(h, (p.SelfMade ? 1 : 0) | (p.Granted ? 2 : 0));`
   Chú thích cuối dòng: `// 0036: chỉ trộn khi khác mặc định — giữ mã băm kịch bản cũ`.

4. **Chuỗi** (`vi.json`): thêm `{"key": "plot.continue", "text": "Tiếp tục"}` đúng thứ tự chữ cái (sau `plot.close`).

5. **`SimView`** (`SimView.cs`):
   - Thêm sau `ProgressAction`:
     ```csharp
     /// <summary>Nút Tiếp tục trên ô tiến trình (0036): giao sinh vật đúng 1 công đoạn kế.</summary>
     public bool CanContinueOpenPlot => OpenPlotId >= 0 && Sim.CanContinue(OpenPlotId);
     public bool ContinueOpenPlot() => OpenPlotId >= 0 && Sim.Continue(OpenPlotId);
     ```
   - `DrawFarmGui`, thay khối `if (busy) { … }` ở trên bằng:
     ```csharp
     if (p.TillPaused)
     {
         if (GUI.Button(new Rect(cr.x + 4f, cr.y + 25f, 64f, 22f), T("plot.resume"))) ProgressAction();
     }
     else
     {
         if (busy && GUI.Button(new Rect(cr.xMax - 18f, cr.y, 18f, 18f), "x")) ProgressAction();
         else if (CanContinueOpenPlot && GUI.Button(new Rect(cr.x + 4f, cr.y + 25f, 64f, 22f), T("plot.continue"))) ContinueOpenPlot();
     }
     ```
     (`TillPaused` chỉ xảy ra khi `busy`, nên hành vi cũ giữ nguyên.)
   - Xong khi: biên dịch, 0 lỗi console.

6. **Test** — viết đúng các test ở "Phần cần test". `AssignedWorkTests` chép `Layout` / `RunTo` / `LastStage` từ `PlayerFarmTests` (không sửa `PlayerFarmTests`). "N nhịp" = 600 nhịp (`TestWorlds.NoWander`, 3 sinh vật cách ô ≤ 5 ô; ở `PlayerFarmTests` công đoạn được nhận trong < 600 nhịp).

## Tiêu chí xong
- Đúng cột "Xong khi" dòng 7g (simulation-tech): các test EditMode + PlayMode dưới đây qua.
- `Game.Core` không tham chiếu UnityEngine; EditMode + PlayMode toàn bộ qua MCP (trừ test đã biết chỉ hỏng khi chạy cả bộ — ghi rõ, chạy riêng lại), 0 lỗi console.

## Test sau khi xong

### Phần cần test
- Lõi (bước 1–3) · `AssignedWorkTests` (EditMode, `namespace Game.Core.Tests`):
  - `SelfSow_NoCreatureWaters` — `Layout(true)`; `PlayerTill(id)`; túi 1 hạt cây 0; `PlayerSow` → Ok, `SelfMade` true; chạy 600 nhịp → `Phase == Water`, công đoạn tưới `Status == Open`, `AssigneeKind == None`; không sinh vật nào có `StageId` trỏ tới công đoạn đó.
  - `Continue_WatersThenStops_RipeNotHarvested` — tiếp theo trên: `CanContinue` true; `Continue` true; `Continue` lần hai false (đã giao); chạy tới `Phase == Growing` (≤ 2000 nhịp) → công đoạn tưới `Done`, `AssigneeKind == Creature`; `Granted` false. Chạy tới chín + 600 nhịp → `Phase == Harvest`, công đoạn thu `Open`, chưa giao; `CanContinue` true.
  - `Continue_Harvests_ThenIdleUntilContinue` — tiếp theo: `Continue` → chạy tới `Phase == Idle` → có công đoạn `Haul` mới; `SelfMade` false; `SoilReady` false; chạy 600 nhịp → vẫn Idle, số công đoạn `Till` không đổi; `CanContinue` true → `Continue` → `Phase == Till`; chạy tới `SoilReady` → `Phase == Idle`, công đoạn xới mới `AssigneeKind == Creature`; `CanContinue` false; `Granted` false.
  - `SelfMade_PlayerHarvest_StaysIdle` — `Layout(true)`, tự xới + tự gieo + `Rain` + `RunTo` chín; `PlayerHarvest` → Idle, `SelfMade` false; chạy 600 nhịp → vẫn Idle, không thêm công đoạn `Till`.
  - `AssignedPlot_PlayerHarvests_CreaturesRetillAndNextCrop` — `Layout(true, crops: new[] { 0, 0 })` (2 vụ cây 0 trong hàng đợi); chạy tới `Phase == Harvest`; `PlayerHarvest` → `Phase == Till`; chạy tới `Phase == Water` hoặc sau → công đoạn xới lại + gieo `AssigneeKind == Creature`; `SelfMade` false suốt.
  - `PlayerTill_SignWithQueue_CreatureSows` — `Layout(true, crops: new[] { 0 })`; `PlayerTill` ngay → `Phase == Sow`; chạy tới `Phase == Growing` → công đoạn gieo `AssigneeKind == Creature`.
  - `PlayerTill_SignNoQueue_StaysIdle` — `Layout(true)`; `PlayerTill` → Idle, `SoilReady`; chạy 600 nhịp → Idle, không công đoạn mới; `CanContinue` false.
  - `PlayerTill_IdleUnready_Retills` — sau `SelfMade_PlayerHarvest` (Idle, `!SoilReady`): `CanPlayerTill` true; `PlayerTill` true → `SoilReady` true, `Phase == Idle`.
  - `EnqueueDuringSelfMade_KeepsHeld_NextCropAssigned` — vụ tự làm pha Water; `EnqueueCrop(id, 0)` true; chạy 600 nhịp → tưới vẫn chưa giao; `Rain`; chín → `PlayerHarvest` → `Phase == Till` (vụ kế), `SelfMade` false; chạy tới `Phase == Water` hoặc sau → công đoạn xới lại `AssigneeKind == Creature`.
  - `CancelCurrent_ClearsSelfMade` — vụ tự làm pha Water, `Continue`, `CancelCurrent` → `SelfMade` false, `Granted` false, Idle.
  - `CanContinue_FalseCases` — vụ giao (hàng đợi) ở pha Water / Growing: false; ô `TillPaused`: false; ô `Removed`: false; id ngoài khoảng: false.
  - `Deterministic` — chạy cùng kịch bản tự gieo + Tiếp tục hai lần (hai `Simulation` mới) → `SimHash.Compute(state)` bằng nhau ở mỗi nhịp (mẫu `CropQueueTests.cs:548`).
- Client (bước 4–5) · `SimFarmUiPlayTests`, thêm:
  - `ContinueRetillsIdlePlot` — `OpenPlot0`; tick tới `Water`/`Growing`; `ProgressAction` (huỷ vụ; hàng đợi demo lúc này trống như `DemoDoesNotRefillQueue`) → `Phase == Idle`, `!SoilReady`, `Slots.Count == 0`; `CanContinueOpenPlot` true; `ContinueOpenPlot()` true → `Phase == Till`; tick tới `SoilReady` → `Idle`; `CanContinueOpenPlot` false.
  - `ContinueGrantsSelfMadeWater` — ô 0 Idle `SoilReady` (từ test trên hoặc tự dựng); `sv.Sim.PlayerSow(0, 0, bag)` với `new PlayerBag()` có 1 hạt → `SelfMade`; tick 600 → tưới chưa giao; `ContinueOpenPlot()` → `Granted`; tick tới `Growing`.
- Regression · `PlayerFarmTests`, `FarmTests` (và mọi test Sim), `SimViewPlayTests`, `PlayerFarmPlayTests`, `PlayerBagSowPlayTests`, `ControlModePlayTests`, `WorldIconsPlayTests`, `HardcodedTextScanTests` · qua.
- Toàn bộ · EditMode + PlayMode · qua hết.

### Lệnh executor chạy
1. MCP `refresh_unity` → `read_console` (Error) = 0.
2. MCP `run_tests` EditMode (toàn bộ) → dòng tổng.
3. MCP `run_tests` PlayMode (toàn bộ) → dòng tổng.
   - Biết trước có thể hỏng khi chạy cả bộ (không do brief này): `IncrementalRebuildMatchesFullRebuild`, `SeaSkyTintPlayTests.DayCycleOffNearSeaUnchanged`, `WorldIconsPlayTests.CreatureIconMatchesState`.
   - Nếu hỏng: chạy riêng lại, ghi cả hai kết quả. Test khác hỏng → sửa.
4. Dọn: `git checkout -- client/ProjectSettings/EditorSettings.asset`.
5. `git status --short` — chỉ file trong Phạm vi (không stage file chưa commit của phiên khác); commit code + test + brief, vd `brief 0067: sim 7g creatures only do assigned work (self-made crops, Continue button)`. Không push.

### User tự test
(Gộp vào danh sách chơi thử cuối task — Planner chép; không chờ user.)
- Đánh dấu một ô (biển), hàng đợi trống → sinh vật xới xong rồi ô đứng yên.
- Tự gieo hạt (cầm hạt + chuột phải) lên ô đó → không sinh vật nào tới tưới. Mở bảng ô ruộng → ô tiến trình có nút **Tiếp tục** → bấm → một sinh vật tới tưới rồi thôi.
- Cây chín → không ai thu. Bấm Tiếp tục → sinh vật thu, chở về kho. Ô đứng yên → Tiếp tục (hoặc "Tự xới") → xới lại.
- Ô có hạt trong hàng đợi: sinh vật làm đủ chuỗi như cũ. Tự thu hoạch / Mưa giữa chừng → sinh vật làm tiếp các bước sau.
- Đánh giá: vị trí / chữ nút Tiếp tục; có cần chữ "chờ Tiếp tục" trên ô tiến trình không.

## Dừng và chuyển lại Opus khi
- Brief mơ hồ hoặc mâu thuẫn với code thật (vd dòng trích dẫn đã khác; `PlayerFarmTests.Layout` không như mô tả).
- Test cũ hỏng vì hành vi mới (không phải lỗi code) — không tự sửa test cũ.
- Mã băm của test tất định cũ đổi.
- Cần một quyết định thuộc AGENTS.md §3.4 hoặc ngoài "Quyết định đã chốt".
- Cùng một lỗi sửa 2 lần vẫn chưa qua.
- Phải sửa file ngoài "Phạm vi".

---
<!-- Từ đây trở xuống: EXECUTOR ghi. -->

## Kết quả
Base commit: 71ad5b9
Chạy bằng: claude-sonnet-5-5 · medium
Bắt đầu: 2026-10-24 09:34 · Kết thúc: ~10:25 (giờ máy; phần lớn chờ test chạy nền)
Commit: 467b3b1
File đổi: Core/Sim/SimData.cs, Simulation.cs, SimHash.cs; Client/SimView.cs; Resources/Lang/vi.json (plot.continue); Tests/Core/Sim/AssignedWorkTests.cs (mới); Tests/Client/SimFarmUiPlayTests.cs (+2 test).
Lệch brief: không (một lần sửa test `CanContinue_FalseCases`: `Unmark` chỉ dùng được cho ô chưa xới nên case Removed dùng Layout mới).


## Kết quả test
- Biên dịch: `read_console` Error = 0.
- Lõi (`AssignedWorkTests`, 12 test) + regression Sim/`PlayerFarmTests`/`FarmTests`/`HardcodedTextScanTests`: đạt — EditMode toàn bộ 582/582 qua (mã băm test tất định cũ không đổi).
- Client (`ContinueRetillsIdlePlot`, `ContinueGrantsSelfMadeWater`) + regression PlayMode: đạt — PlayMode toàn bộ 267 test, 266 qua; hỏng duy nhất `SeaSkyTintPlayTests.DayCycleOffNearSeaUnchanged` (0.021 > 0.002, đã biết chỉ hỏng khi chạy cả bộ); chạy riêng 1/1 qua.
- Dọn: `git checkout -- client/ProjectSettings/EditorSettings.asset` đã chạy.
- Xem nút Tiếp tục (vị trí/chữ), chơi thử: chờ user — gộp cuối.


## Vướng mắc

---
<!-- PLANNER ghi khi review. -->

## Review
