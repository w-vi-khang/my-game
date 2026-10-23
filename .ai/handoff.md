# Handoff — 2026-10-23

## Đang làm
Task `m1-world-7l-dieu-khien-pc` ([task contract](tasks/m1-world-7l-dieu-khien-pc.md)). 7l a, 7l b (+ bản sửa 0057, quyết định [0035](../design/decisions/0035-camera-quan-sat-dat-tren-o-xoay-khi-xem.md)) **đã duyệt**. Bước tiếp: **7l c**, user chốt chia 3 brief (task contract dòng 70–71):
- **c1 (brief 0058, viết tiếp):** chế độ D/Q/E/R (bấm lại tắt) + chuột phải tự đi tới 2 ô rồi làm + tô đỏ khối dưới con trỏ (bỏ hộp viền trắng) + vòng tầm thao tác 10 khi ngoài tầm + F Thu 2 ô + click trái ô ruộng → đi tới 2 ô → bảng ô ruộng có nút **Bỏ đánh dấu**. Luật: 0034 mục 3.
- **c2:** cầm vật phẩm (khối bản mờ/đỏ, hạt chỉ ô ruộng hàng đợi trống, camera quan sát là vật phẩm). Trước 7l d: cầm tạm qua bảng túi cũ (B → bấm), khối tạm chọn bằng 1–4.
- **c3:** nối máu/mana vào client (TrySpend đào/đặt/Mưa, AddMoved, Land, Tick ngập).
- `client/Assets/Game/Tests/Client/TerrainEditPlayTests.cs` có thay đổi dở của phiên khác — không commit.
- Task contract (dòng 70–71) đã sửa, **chưa commit** → commit cùng brief 0058.

## Khảo sát code cho 7l c (subagent, 2026-10-23 — paths dưới client/Assets/Game/)
- **Core có sẵn từ 0054, client CHƯA dùng:** `Core/ControlState.cs` — `enum ControlMode { Default, Dig, Till, Rain }`; `ControlState` (`Mode`, `HasHeld`, `Held`, `event Changed`, `ToggleMode(m)` (Default hoặc mode đang bật → Default, luôn bỏ vật cầm), `Hold(item)`, `ClearToDefault()`); `RightClick.Resolve(state, in RightClickTarget t)` :51 → `RightClickAction { None, OutOfReach, UseHeld, Dig, Till, Rain, Harvest, OpenStorage, PickDrop, PlaceBlock }`; `RightClickTarget { InTargetReach, HeldUsable, ModeUsable, RipeCrop, Storage, Drop }`. Resolve không kiểm tầm thực hiện 2 ô (caller làm). `PlayerRules`: `TargetReach 10, ActReach 2, DigReach 2, CollectReach 2` + `InTargetReach/InActReach/InDigReach/InCollectReach(feetX,feetY,feetZ,x,y,z)`; `ToolReach/HandReach 8` + `InToolReach/InHandReach` còn dùng ở client (0054 ghi: 7l c chuyển sang Target/Act rồi gỡ cả key JSON `toolReach/handReach`). `PlayerVitals` (TrySpend, AddMoved, Land, Tick) chưa nối.
- **TerrainEditInput.cs:** `enum EditTool { Dig, Place, Till, Camera }`, Tab `ToggleTool` :210; `Update` :84–181 thứ tự: khoá khi xem camera → phím (1–4, Tab, Esc thoát gieo, F `CollectNearby` dùng HandReach 8) → chặn UI → Alt+lăn → nhánh Camera → `PickDrop` → kho (`OpenStockAt`, HandReach) → `Pick` → R `CastRain` (không cần chế độ) → thu hoạch → gieo → `ShowAim` (viền trắng LineRenderer `Game/SelectionOverlay` :443, ghost `Game/GhostBlock` :474) → `UseTool` (chuột phải). `UnmarkAimed` :184 không còn ai gọi. HUD `HudText` :395, khoá `hud.tool` ("(Tab đổi)"), `hud.help_walk` (vi.json:339).
- **PlayerFarmActions.cs:** tự xới `BeginSelfTill` :72 dùng `sim.FindPlayerPathToPlot` + `player.StartAutoWalk(path)`, Update poll `player.AutoWalk` (Arrived/Cancelled/Failed → `farm.unreachable`); `SowAt`, `CastRain`, `RipePlotAt` dùng InToolReach; không dùng mana.
- **PlayerCharacter.cs:** `MoveTo(targetLocal)` :94 đi thẳng, `ClickMove` (None/Walking/Arrived/Blocked; Blocked sau stuckMs 300), `StopClickMove`, `StartAutoWalk/StopAutoWalk/AutoWalk`, `HasMoveIntent` :87, `InToolReach` :267 là wrapper tầm duy nhất. **Không có helper "đi tới rồi làm"**, không có event — phải poll. Chuột trái luôn đi (không xét ô ruộng). Lưu ý review 0055: giữ chuột trái vào tường không thành Blocked (MoveTo reset stuck mỗi frame).
- **Đường đi chung:** chỉ có `Simulation.FindPlayerPathToPlot`; `GridPathfinder.FindPathToNearest(...)` (Core/Sim/GridPathfinder.cs:176) nhưng `pathfinder` trong Simulation là private.
- **SimView.cs (IMGUI):** bảng ô ruộng chỉ mở qua `UseTill` (công cụ Xới + chuột phải lên ô đã xới); nút: chọn giống, hàng đợi +/−/x, tạm dừng/tiếp/huỷ, "Tự xới" :583, "Đóng" :585; **chưa có Bỏ đánh dấu** (`UnmarkAt` :298 có sẵn). Túi B: click hàng hạt → `ChooseSeedFromBag` :375 → `EnterSowMode`. Kho `OpenStockAt` :390 và `Sim.StorageInReach` dùng HandReach 8. Không có túi phụ.
- **Input (GameControls.inputactions, Player 24 action, tổng 33):** D/Q/E/G **chưa gán**; R = CastRain; Tab = CycleTool; 1–4 SelectBlock. `DebugFlyCamera.cs:37,75–80` đọc thẳng W/A/S/D/Q/E (chỉ chế độ bay F3). `GameControlsTests`: đếm 33, bảng Expected(), không trùng phím trong map, `NoDirectInputReadsInScope`; thêm action → sửa đếm + Expected + `GameInput.cs`.
- **Test liên quan cần sửa khi bỏ Tab/công cụ:** TerrainEditPlayTests (dở!), SimFarmUiPlayTests (`TabCyclesFourToolsWithSim`, `ShowAimTillOutline`…), PlayerFarmPlayTests (`TabCancelsWalking`…), PlayerBagSowPlayTests, DropPlayTests (`CollectNearbyTakesOnlyWithin8`…), ObservationCameraPlayTests (`CameraToolCycleAndClickPlaces`), PlayerCharacterPlayTests (`ReachLimitsTools`); Core: ControlStateTests, PlayerVitalsTests.

## Điểm Planner phải chốt khi viết c1 (hỏi user nếu là quyết định thiết kế)
1. Trong c1, Đặt khối và Camera quan sát chưa thành vật cầm (c2) → giữ tạm cách nào? Đề xuất: giữ Tab chỉ còn vòng tạm "Đặt / Camera" hoặc tạm giữ Tab cũ tới c2 — **hỏi user**.
2. Tự đi tới 2 ô: đề xuất `MoveTo` đi thẳng + poll `InActReach` mỗi frame, kẹt (Blocked) → huỷ + "Không tới được" (0034); không dùng tìm đường (ngoài tự xới đã có).
3. Đổi tầm: client chuyển InToolReach/InHandReach → InTargetReach (click) + InActReach (thực hiện), F Thu dùng CollectReach 2; gỡ ToolReach/HandReach + key JSON (cả 2 bản sim_m1.json) — c1 hay để c3? Đề xuất c1 (đã ghi ở 0054).
4. Chế độ mặc định chuột phải: thu hoạch / mở kho / nhặt (không đào) — user phản hồi 0056.
5. Vòng tầm: vòng bán kính 10 mờ dưới đất khi có chế độ và con trỏ ngoài tầm; tô đỏ khối thay viền trắng (shader mới hay tái dùng `Game/SelectionOverlay` đổi màu — đọc shader rồi chọn).

## Bước tiếp
1. Phiên mới (Opus · high): đọc handoff này + 0034 mục 3 + task contract; hỏi user điểm 1 (và điểm khác nếu cần); đọc code các chỗ trên lấy `path:line` thật; viết brief **0058** (c1) theo `/ai-workflow-kit:brief`; commit task contract + brief; giao Executor Sonnet · medium (thẻ đề xuất).
2. Sau đó c2, c3, rồi 7l d, 7l e; user chơi thử sau 7l e.

## Model đề xuất
Opus · high (chốt điểm còn mở + viết brief c1).
