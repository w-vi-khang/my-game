# Brief 0041 — Sim 7f (client A): nhân vật tự đi tới xới, phép Mưa, click thu hoạch

> Trạng thái: đã duyệt
> Review: đầy đủ + user xem
> Model thực thi: Sonnet · medium
> Cập nhật: 2026-10-22 18:40

<!-- Planner (Opus) viết, Executor (Sonnet) làm theo ở PHIÊN KHÁC — executor không có lịch sử chat, không được tự quyết.
Trạng thái: chờ làm → đang làm → chờ review → đã duyệt; hoặc → cần Opus (executor dừng, hỏi ở mục Vướng mắc).
Review: nhẹ (đọc mục Kết quả) | đầy đủ (đọc cả diff) | user xem (có phần user tự kiểm bằng mắt/chơi thử) — có thể ghép "đầy đủ + user xem".
Model thực thi: BẮT BUỘC, đúng dạng `<Model> · <effort>` — Model: Opus | Sonnet | Haiku | Fable; effort: low | medium | high | xhigh | max (chọn theo AGENTS.md §10.2).
Executor kiểm tra model + effort của phiên TRƯỚC khi làm (scripts/brief_check.py); lệch hoặc không xác định được → cảnh báo user và DỪNG.
File được commit. Tự kiểm trước khi giao (AGENTS.md §10.3): executor có phải hỏi gì không? còn chỗ nào bỏ ngỏ 2 phương án? -->

## Mục tiêu
Phần client A của [task contract sim 7f](../tasks/m1-sim-buoc-7f-tu-trong-trot.md) (đề xuất 2, 6, 7; user xác nhận 2026-10-22). Dùng API lõi của brief 0040 (đã duyệt, `32f0656`):

- **Tự xới:** cầm Xới, chuột trái vào ô có biển trong 8 ô → nhân vật tự đi tới ô kề rồi đứng xới 6 giây. Bấm WASD/Space, Tab, F3 hoặc click công cụ khác → huỷ. Bảng ô ruộng có nút `Tự xới` cho ô xới lại.
- **Bỏ đánh dấu** chuyển sang **chuột phải** vào ô có biển.
- **Phép Mưa:** phím **R** lên ô đang nhắm, trong 8 ô, hồi 10 giây, hình hộp xanh nhạt 0,5 giây.
- **Click thu hoạch:** cây chín, với mọi công cụ → nông sản + hạt vào túi.
- HUD: dòng Mưa, dòng trạng thái tự xới / Mưa / thu hoạch, dòng nông sản.

**Không có trong brief này** (brief 0042): bảng túi (B), chế độ gieo, nút chuyển kho ↔ túi. Tài liệu `design/` Planner cập nhật sau brief 0042.

Ước lượng ~2 ngày-người (trong 6–10 của 7f). Ghi giờ bắt đầu/kết thúc (theo `date`) vào mục Kết quả.

## Bối cảnh cần đọc
Đường dẫn tính từ `client/Assets/Game/`. Số dòng theo commit `32f0656`.

**API lõi (brief 0040):**
- `Core/Sim/Simulation.cs`:
  - `public bool CanPlayerTill(int plotId)` (`:983`), `public bool PlayerTill(int plotId)` (`:991`);
  - `public bool CanPlayerHarvest(int plotId)` (`:1034`), `public bool PlayerHarvest(int plotId, PlayerBag bag)` (`:1042`);
  - `public RainResult Rain(CellPos cell)` (`:1051`) — `RainResult { NoEffect, Watered, Grassed }` (`Core/Sim/SimData.cs`);
  - `public bool FindPlayerPathToPlot(CellPos feet, int plotId, List<CellPos> result)` (`:1126`) — đường không gồm ô xuất phát, gồm ô đích; đã ở đích → true, rỗng;
  - `public int PlotAt(CellPos soil)`, `public bool Unmark(int plotId)`, `State.Plots[id]` (`Soil`, `Tilled`, `Phase`, `CurrentCrop`, `Removed`).
- `Core/PlayerTimers.cs`: `WorkTimer(float)` — `Start()`, `Cancel()`, `bool Advance(float dt)` (true đúng một lần khi xong), `Elapsed`, `Duration`, `Running`; `Cooldown(float)` — `Start()`, `Advance(float dt)`, `Ready`, `RemainingWholeSeconds`.
- `Core/Sim/SimConfig.cs:191-192`: `PlayerTillSeconds` (6), `RainCooldownSeconds` (10). `crops[i].id` = `"A"`, `"B"`.
- `Core/PlayerBag.cs`: `GetProduce(int crop)`, `GetSeeds(int crop)`.
- `Core/WorldEditor.cs`: `public bool TryGrow(int x, int y, int z)` (đất có ô trên rỗng → cỏ, phát `CellChanged`).
- `Core/PlayerRules.cs`: `InToolReach(...)` 8 ô.

**Nhân vật:** `Client/PlayerCharacter.cs` (220 dòng)
- `public Vector2 MoveInput; public bool JumpInput;` (`:19-20`); `readKeyboard` (`:18`).
- `public static PlayerCharacter Active` (`:29`), `FlyMode` (`:31`), `FeetLocal` (`:36`, toạ độ terrain), `public bool InToolReach(Vector3Int cell)` (`:137-141`).
- `void Update()` (`:158-218`):
  - đọc phím vào `MoveInput` / `JumpInput` (`:183-192`);
  - `var f = FeetLocal; InWater = …` (`:194-195`);
  - `var dir = Quaternion.Euler(0f, yaw, 0f) * new Vector3(MoveInput.x, 0f, MoveInput.y);` (`:198`), chuẩn hoá (`:199`), `var h = dir * (walkSpeed * (InWater ? … : 1f) * Time.deltaTime);` (`:200`);
  - nhảy `if (Controller.isGrounded && JumpInput) vy = jumpSpeed;` (`:203`);
  - `ClampToWorld`, `Controller.Move(...)`, xoay `Visual` theo `dir` (`:206-217`).

**Công cụ:** `Client/TerrainEditInput.cs` (395 dòng)
- `Awake()` (`:58-65`): tạo `DropView` nếu chưa có — **mẫu** thêm component.
- `Update()` (`:67-133`): phím 1–4, Tab (`:77`), F (`:78`); chặn khi con trỏ trên UI (`:82-87`); `AimedDrop = PickDrop(ray)` (`:106`) và nhánh nhặt (`:107-113`); `Pick(...)` (`:114-118`); `ShowAim(true, …)` (`:120`); chặn khi camera bay + phím đi (`:129`); `if (mouse.leftButton.wasPressedThisFrame) UseTool(digCell, placeCell);` (`:132`).
- `public void ToggleTool()` (`:136`).
- `public EditResult UseTool(Vector3Int digCell, Vector3Int placeCell)` (`:178-205`); nhánh Xới (`:183-194`) đặt `LastTill`, `lastResult`.
- `static string HelpText()` (`:287-295`), `public string HudText()` (`:298-319`): dòng Xới `:303`, dòng túi hạt `:312-313`, `text += $"\n{lastResult}";` (`:315`).
- `CreateGhost()` (cuối file) dùng shader `Game/GhostBlock`, màu qua `ghostMat.SetColor("_BaseColor", …)`.

**Bảng ô ruộng:** `Client/SimView.cs` (523 dòng)
- `public enum TillResult { Invalid, Marked, Unmarked, OpenedPlot }` (`:11`).
- `public Simulation Sim` (`:65`), `void UpdatePlotSigns()` (`:233`), `public int OpenPlotId` (`:263`).
- `public TillResult UseTill(Vector3Int cell)` (`:272-292`): ô có biển chưa xới → `Unmark` + `Unmarked` (`:279-284`); ô ruộng → mở bảng (`:285-286`); không có → `MarkTill`.
- `public void ClosePlotPanel()` (`:314-318`).
- `void DrawFarmGui(float scale)` (`:340-413`): nút `"Đóng (Esc)"` ở `new Rect(pr.xMax - 120f, pr.yMax - 36f, 110f, 28f)` (`:412`).

**Cấu hình:** `Client/ConfigLoader.cs`: `public static SimConfig LoadSimConfig()`.

**Test PlayMode mẫu:**
- `Tests/Client/WalkBenchPlayTests.cs:17-41` — dựng camera (`Camera`, `DebugFlyCamera`, `FollowCamera`), `TerrainChunkView` (`source = WorldSource.MapsV3`, `streaming = true`) + `SimView`, nhân vật `readKeyboard = false`, `go.SetActive(false)` rồi bật lại.
- `Tests/Client/DropPlayTests.cs:66-88` — `Run(move, jump, seconds)`, `Teleport(feetLocal)`.
- `Tests/Client/SimFarmUiPlayTests.cs:36-48` — `FindMarkable()`; `:120-140` — `UnmarkAndDigHideSign` (dùng `UseTill` để bỏ đánh dấu, **phải đổi**).

**API Unity / Input System 1.19** (đã tra):
- `Mouse.rightButton` — `public ButtonControl rightButton { get; protected set; }` [docs](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.19/api/UnityEngine.InputSystem.Mouse.html).
- `ButtonControl.wasPressedThisFrame` [docs](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.19/api/UnityEngine.InputSystem.Controls.ButtonControl.html).
- `Keyboard.rKey` — `public KeyControl rKey { get; }` [docs](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.19/api/UnityEngine.InputSystem.Keyboard.html).
- Ngoài ra chỉ dùng API đã có trong file đang sửa (`GameObject.CreatePrimitive`, `Shader.Find`, `Time.deltaTime`, `GUI.Button`, `Mathf`).

## Quyết định đã chốt (không được đổi)
**Task contract 7f (user xác nhận 2026-10-22):**
1. Click ô có biển (chưa xới) bằng Xới trong 8 ô → tự đi tới ô kề, đứng xới **6 giây** (`PlayerTillSeconds`), xong → `Sim.PlayerTill`. Huỷ khi bấm WASD/Space, đổi công cụ, dùng công cụ khác; không giữ tiến độ dở. Không có đường → `Không tới được ô này`. Sinh vật xới xong trước → người chơi dừng, không lỗi.
2. Ô xới lại: nút `Tự xới` trong bảng ô ruộng. Bỏ đánh dấu = **chuột phải** vào ô có biển.
3. Mưa: phím R, ô nhắm (ô sẽ đào) trong 8 ô; `NoEffect` → không tính hồi, HUD nêu lý do. Hồi theo `RainCooldownSeconds`. Hình: hộp xanh nhạt trong suốt trên ô, 0,5 giây.
4. Thu hoạch: cây chín trong 8 ô, chuột trái với **mọi công cụ** → `Sim.PlayerHarvest`, thay cho công cụ.

**Planner chốt khi viết brief:**

5. **Tự đi ở `PlayerCharacter`** (không xoay camera):
   ```csharp
   public enum AutoWalkState { None, Walking, Arrived, Cancelled, Failed }
   /// <summary>Trạng thái tự đi theo đường ô (sim 7f, brief 0041).</summary>
   public AutoWalkState AutoWalk { get; private set; }
   /// <summary>Bắt đầu tự đi qua các ô chân (toạ độ terrain, Game.Core.Sim.CellPos). Đường rỗng → Arrived ngay.</summary>
   public void StartAutoWalk(IReadOnlyList<Game.Core.Sim.CellPos> path)
   /// <summary>Đang Walking → None (người gọi tự huỷ).</summary>
   public void StopAutoWalk()
   ```
   Luật trong `Update`, ngay sau `InWater = …` (`:195`):
   - `AutoWalk == Walking` và (`FlyMode` hoặc `MoveInput.sqrMagnitude > 0.01f` hoặc `JumpInput`) → `AutoWalk = Cancelled`.
   - Còn `Walking`: đích = `path[index]`, tâm `(X + 0.5, Z + 0.5)`. Khoảng cách ngang từ `f` tới tâm `< 0.15f` → `index++`, `stall = 0`; `index == Count` → `AutoWalk = Arrived`.
   - Còn đích: hướng = vector ngang (toạ độ terrain) chuẩn hoá, đổi sang world bằng `terrain.transform.TransformDirection`; **thay** `dir` ở `:198`. Độ dài bước ngang = `Mathf.Min(walkSpeed * hệ số nước * Time.deltaTime, khoảng cách ngang còn lại)` (không vượt đích).
   - Nhảy tự động: `đích.Y > Mathf.FloorToInt(f.y + 0.05f)` → coi như `JumpInput` ở dòng `:203` (biến riêng `autoJump`, **không** ghi vào `JumpInput`).
   - `stall += Time.deltaTime` mỗi khung đang Walking; `stall > 3f` → `AutoWalk = Failed`.
6. **Component mới `Client/PlayerFarmActions.cs`**, `TerrainEditInput.Awake` thêm nếu chưa có (như `DropView`), lộ ra `public PlayerFarmActions Farming { get; private set; }`. Giữ mọi trạng thái tự xới / Mưa / thu hoạch; `TerrainEditInput` chỉ gọi.
7. **Ô chân** khi tìm đường: `new CellPos(Mathf.FloorToInt(f.x), Mathf.FloorToInt(f.y + 0.05f), Mathf.FloorToInt(f.z))`, `f = player.FeetLocal`.
8. **`TillResult` thêm `SelfTill`** (cuối enum). `SimView.UseTill`: ô có biển chưa xới → trả `SelfTill`, **không** bỏ đánh dấu. Bỏ đánh dấu qua hàm mới `SimView.UnmarkAt`.
9. **Mưa không có mô phỏng** (scene TerrainView): dùng `view.Editor.TryGrow` → `Grassed` / `NoEffect`. Không có nhân vật → không kiểm tầm (như công cụ).
10. Mọi dòng chữ HUD ghi nguyên văn ở bước 2–5; số ô in dạng `(x, y, z)` như `Vector3Int.ToString()` đang dùng.

## Phạm vi
- Được sửa/tạo:
  - `Client/PlayerCharacter.cs` — chỉ tự đi (bước 1);
  - `Client/PlayerFarmActions.cs` (mới, + `.meta`);
  - `Client/TerrainEditInput.cs`, `Client/SimView.cs` — chỉ các chỗ ở bước 3–5;
  - `Tests/Client/PlayerFarmPlayTests.cs` (mới, + `.meta`); `Tests/Client/SimFarmUiPlayTests.cs` — chỉ `UnmarkAndDigHideSign`;
  - mục Kết quả của brief này.
- Không được sửa: `Core/**`, scene `.unity`, `design/`, task contract, test khác, file khác.

## Các bước
1. **`Client/PlayerCharacter.cs`** — tự đi theo quyết định 5. Thêm `using Game.Core.Sim;` nếu cần. Trường riêng: `readonly List<CellPos> autoPath = new List<CellPos>(); int autoIndex; float autoStall;`. `StartAutoWalk` chép đường, `autoIndex = 0`, `autoStall = 0`, `AutoWalk = path.Count == 0 ? Arrived : Walking`. Sửa summary lớp thêm "tự đi theo đường ô (sim 7f, brief 0041)". Xong khi: biên dịch được; đi bằng phím như cũ (test `PlayerCharacterPlayTests` qua).

2. **`Client/PlayerFarmActions.cs`** (mới, namespace `Game.Client`, `[RequireComponent(typeof(TerrainChunkView))]`):
   ```csharp
   public enum SelfTillState { None, Walking, Tilling }

   /// <summary>Người chơi tự xới (đi tới + 6 giây), phép Mưa (R, hồi), thu hoạch vào túi — sim 7f, brief 0041. Luật ở Game.Core (Simulation, PlayerTimers).</summary>
   public class PlayerFarmActions : MonoBehaviour
   {
       public SelfTillState TillState { get; private set; }
       public int TillPlotId { get; private set; } = -1;
       public WorkTimer TillTimer { get; private set; }
       public Cooldown RainCooldown { get; private set; }
       /// <summary>Dòng trạng thái mới nhất (tự xới / Mưa / thu hoạch) cho HUD; "" lúc đầu.</summary>
       public string StatusLine { get; private set; } = "";

       public bool BeginSelfTill(int plotId);
       public void CancelSelfTill();
       public RainResult CastRain(Vector3Int cell);
       /// <summary>Id ô ruộng chín có Soil = cell, trong tầm 8 ô (có nhân vật); -1 nếu không.</summary>
       public int RipePlotAt(Vector3Int cell);
       public bool Harvest(int plotId);
       /// <summary>Các dòng HUD của trồng trọt (bước 5).</summary>
       public string HudLines();
   }
   ```
   - `Awake`: `view = GetComponent<TerrainChunkView>()`; `var cfg = ConfigLoader.LoadSimConfig(); TillTimer = new WorkTimer(cfg.PlayerTillSeconds); RainCooldown = new Cooldown(cfg.RainCooldownSeconds);`; tạo hộp Mưa: `GameObject.CreatePrimitive(PrimitiveType.Cube)`, tên `"Mưa"`, `Destroy` collider, cha = `transform` (`SetParent(transform, false)`), vật liệu `new Material(Shader.Find("Game/GhostBlock"))` với `_BaseColor = new Color(0.55f, 0.75f, 1f, 0.45f)`, `SetActive(false)`.
   - `Farm`: `GetComponent<SimView>()` có `Sim != null` → `Sim`; không thì null (tìm lại mỗi lần như `TerrainEditInput.Farm`).
   - **`BeginSelfTill(plotId)`** theo thứ tự, mỗi lần false thì đặt `StatusLine` như sau:
     1. `Sim == null` hoặc `!Sim.CanPlayerTill(plotId)` → `"Tự xới: ô này không cần xới"`;
     2. `PlayerCharacter.Active == null` → `"Tự xới: cần nhân vật"`; `FlyMode` → `"Tự xới: tắt camera bay (F3) trước"`;
     3. `CancelSelfTill()` nếu đang có; ô chân theo quyết định 7; `Sim.FindPlayerPathToPlot(feet, plotId, path)` false → `"Không tới được ô này"`;
     4. được: `TillPlotId = plotId`; đường rỗng → vào Tilling ngay (`TillTimer.Start()`), không thì `player.StartAutoWalk(path)`, `TillState = Walking`. `StatusLine = $"Tự xới ô {soil}: đang đi tới"` (`soil` = `Vector3Int` của `Plots[plotId].Soil`). Trả true.
   - **`CancelSelfTill()`**: đang None → không làm gì. Ngược lại `player?.StopAutoWalk()` (nếu đang Walking), `TillTimer.Cancel()`, `TillState = None`, `TillPlotId = -1`, `StatusLine = "Huỷ tự xới"`.
   - **`Update`**:
     - `RainCooldown.Advance(Time.deltaTime)`; hộp Mưa đang hiện quá 0,5 giây → ẩn.
     - `TillState != None` mà `Sim == null`, ô `Removed`, hoặc `!Sim.CanPlayerTill(TillPlotId)` (sinh vật đã xới xong) → về None, `StatusLine = "Tự xới: ô đã được xới"`, dừng tự đi.
     - `Walking`: `AutoWalk == Arrived` → `TillTimer.Start()`, `TillState = Tilling`; `Cancelled` → như `CancelSelfTill()`; `Failed` → về None, `StatusLine = "Không tới được ô này"`.
     - (`soil` ở các dòng dưới = `Vector3Int` của `Sim.State.Plots[TillPlotId].Soil`.)
     - `Tilling`: nhân vật `FlyMode`, `MoveInput.sqrMagnitude > 0.01f` hoặc `JumpInput` → `CancelSelfTill()`. Không thì `TillTimer.Advance(Time.deltaTime)` true → `Sim.PlayerTill(TillPlotId)`, `TillState = None`, `StatusLine = $"Xới xong ô {soil}"`, `TillPlotId = -1`.
   - **`CastRain(cell)`** theo thứ tự:
     1. có nhân vật và `!player.InToolReach(cell)` → `StatusLine = "Mưa: ngoài tầm 8 ô"`, `NoEffect`;
     2. `!RainCooldown.Ready` → `$"Mưa: còn {RainCooldown.RemainingWholeSeconds} giây"`, `NoEffect`;
     3. `r = Sim != null ? Sim.Rain(new CellPos(cell.x, cell.y, cell.z)) : (view.Editor != null && view.Editor.TryGrow(cell.x, cell.y, cell.z) ? RainResult.Grassed : RainResult.NoEffect)`;
     4. `Watered` → `$"Mưa: tưới ô ruộng {cell}"`; `Grassed` → `$"Mưa: ô đất {cell} thành cỏ"`; hai trường hợp này `RainCooldown.Start()`, hiện hộp Mưa tại `localPosition = cell + new Vector3(0.5f, 1.5f, 0.5f)`;
     5. `NoEffect` → `"Mưa: không có tác dụng — chỉ ô ruộng chờ tưới hoặc ô đất phía trên trống"`.
   - **`RipePlotAt(cell)`**: `Sim == null` → -1; `id = Sim.PlotAt(...)`; `id < 0 || !Sim.CanPlayerHarvest(id)` → -1; có nhân vật mà `!InToolReach(cell)` → -1; trả `id`.
   - **`Harvest(plotId)`**: `crop = Plots[plotId].CurrentCrop`; `Sim.PlayerHarvest(plotId, view.Bag)` false → false. True → `var cd = Sim.Config.crops[crop]; StatusLine = $"Thu hoạch Cây {cd.id}: +{cd.producePerHarvest} nông sản, +{cd.seedsPerHarvest} hạt";`.
   - **`HudLines()`**: dòng 1 `RainCooldown.Ready ? "Mưa (R): sẵn sàng" : $"Mưa (R): {RainCooldown.RemainingWholeSeconds} giây"`; Tilling → thêm `$"\nĐang xới ô {soil}: {(int)TillTimer.Elapsed}/{(int)TillTimer.Duration} giây"`; Walking → `$"\nĐang đi tới ô {soil}…"`; `StatusLine` khác "" → thêm `"\n" + StatusLine`. Có `Sim`: với mỗi loại cây `i`, `view.Bag.GetProduce(i) > 0` → `$"\nNông sản Cây {id}: {n}"`; `i != StarterBag.Crop` và `GetSeeds(i) > 0` → `$"\nTúi hạt Cây {id}: {n}"` (Cây A đã có dòng riêng ở `HudText`).

3. **`Client/SimView.cs`**:
   - `TillResult` thêm `SelfTill` cuối enum; summary enum ghi thêm "SelfTill = ô có biển chưa xới → tự đi tới xới (brief 0041)".
   - `UseTill`: nhánh ô chưa xới (`:279-284`) → `return TillResult.SelfTill;` (bỏ `Unmark`, `UpdatePlotSigns`).
   - Thêm:
     ```csharp
     /// <summary>Chuột phải khi cầm Xới (brief 0041): ô có biển chưa xới → bỏ đánh dấu. true khi đã bỏ.</summary>
     public bool UnmarkAt(Vector3Int cell)
     /// <summary>Nút "Tự xới" của bảng ô ruộng (ô xới lại): đóng bảng rồi tự đi tới xới. false nếu không tự xới được.</summary>
     public bool SelfTillOpenPlot()
     ```
     `UnmarkAt`: `id = Sim.PlotAt(...)`, `id >= 0 && !Plots[id].Tilled && Sim.Unmark(id)` → `UpdatePlotSigns()`, true. `SelfTillOpenPlot`: `OpenPlotId < 0` hoặc `!Sim.CanPlayerTill(OpenPlotId)` hoặc không có `GetComponent<PlayerFarmActions>()` → false; `int id = OpenPlotId; ClosePlotPanel(); return farming.BeginSelfTill(id);`.
   - `DrawFarmGui`: trước nút `"Đóng (Esc)"` (`:412`) thêm nút `"Tự xới"` ở `new Rect(pr.x + 10f, pr.yMax - 36f, 110f, 28f)`, `GUI.enabled = Sim.CanPlayerTill(OpenPlotId) && PlayerCharacter.Active != null;`, click → `SelfTillOpenPlot()`; trả `GUI.enabled = true` ngay sau.

4. **`Client/TerrainEditInput.cs`**:
   - `Awake`: thêm `Farming` như `DropView` (quyết định 6).
   - Thêm `public int AimedHarvest { get; private set; } = -1;`, đặt `-1` đầu `Update` cạnh `AimedDrop = null`.
   - `ToggleTool()`: đổi thành thân khối, gọi `Farming?.CancelSelfTill()` trước khi đổi công cụ (giữ nguyên vòng Đào → Đặt → Xới).
   - `Update`, sau `Pick(...)` thành công (`:114-118`), **trước** `ShowAim(true, …)` (`:120`):
     - `if (kb != null && kb.rKey.wasPressedThisFrame) Farming.CastRain(digCell);`
     - `AimedHarvest = Farming.RipePlotAt(digCell);` `AimedHarvest >= 0` → `ShowAim(false, default, default);`, chặn camera bay như `:129`, `if (mouse.leftButton.wasPressedThisFrame) Farming.Harvest(AimedHarvest);`, `return;`.
   - Thay `:132` bằng:
     ```csharp
     if (mouse.leftButton.wasPressedThisFrame) UseTool(digCell, placeCell);
     if (Tool == EditTool.Till && mouse.rightButton.wasPressedThisFrame && Farm != null
         && (player == null || player.InToolReach(digCell)) && Farm.UnmarkAt(digCell))
         lastResult = $"Bỏ đánh dấu {digCell}";
     ```
   - R khi `Pick` thất bại (không có ô nhắm) không làm gì.
   - `UseTool`: sau kiểm tầm (`:181-182`), trước nhánh Xới: `if (Farming != null && Farming.TillState != SelfTillState.None) Farming.CancelSelfTill();` (click công cụ nào cũng huỷ tự xới đang chạy; click lên biển thì bắt đầu lại ở dưới). Nhánh Xới: thêm case `TillResult.SelfTill => …`: gọi `Farming.BeginSelfTill(Farm.Sim.PlotAt(new CellPos(digCell.x, digCell.y, digCell.z)))`; `lastResult = ""` (thông báo nằm ở `Farming.StatusLine`); trả `EditResult.Ok`. Bỏ case `Unmarked` khỏi chuỗi (UseTill không còn trả).
   - `HelpText()`: ở dòng có nhân vật không bay, sau `"F: thu vật rơi (trong 8 ô) · "` thêm `"R: phép Mưa (trong 8 ô) · chuột trái lên cây chín: thu hoạch · "`.
   - `HudText()`: dòng Xới (`:303`) đổi nguyên văn thành `"Xới: chuột trái lên đất/cỏ = đánh dấu · lên biển = tự đi tới xới · chuột phải lên biển = bỏ đánh dấu · lên ô ruộng = chọn giống · K: kho\n"`; trước `text += $"\n{lastResult}";` thêm `if (Farming != null) text += "\n" + Farming.HudLines();` và `if (AimedHarvest >= 0) text += $"\nThu hoạch (chuột trái): Cây {Farm.Sim.Config.crops[Farm.Sim.State.Plots[AimedHarvest].CurrentCrop].id}";`.
   - Sửa summary lớp: thêm "Sim 7f (brief 0041): chuột trái lên biển = tự đi tới xới, chuột phải = bỏ đánh dấu; R = phép Mưa; chuột trái lên cây chín (mọi công cụ) = thu hoạch."

5. **`Tests/Client/SimFarmUiPlayTests.cs`** — `UnmarkAndDigHideSign` (`:120-140`): `Assert.AreEqual(TillResult.Unmarked, sv.UseTill(a));` → `Assert.AreEqual(TillResult.SelfTill, sv.UseTill(a)); Assert.IsTrue(sv.UnmarkAt(a));`. Phần còn lại giữ.

6. **`Tests/Client/PlayerFarmPlayTests.cs`** (mới) — dựng như `WalkBenchPlayTests.SetUp` nhưng **không** có `WalkBench`, thêm `TerrainEditInput`; chờ tới khi `sv.Sim != null` và `player.Ready` (tối đa 600 khung). Ngay sau đó **`sv.enabled = false`** để mô phỏng không tự chạy (sinh vật không giành việc); test tự gọi `sv.Sim.Tick()` khi cần. Các test ở "Phần cần test". Ô thử: `FindMarkable` như `SimFarmUiPlayTests` nhưng quanh chân nhân vật trong bán kính 6 và bỏ ô chân đang đứng.

## Tiêu chí xong
- EditMode toàn bộ qua (không đổi Core, kỳ vọng như trước: 472/472).
- PlayMode toàn bộ qua (cũ + mới), 0 lỗi console khi chạy.
- `git diff --stat` chỉ có file trong Phạm vi.

## Test sau khi xong
### Phần cần test
**Mới — `PlayerFarmPlayTests`:**
- **Tự đi:** `StartAutoWalk` với đường từ `FindPlayerPathToPlot` tới một ô có biển cách 4–6 ô → trong 10 giây `AutoWalk == Arrived`, chân cách tâm ô cuối < 0,3 ngang. Đường rỗng → `Arrived` ngay.
- **Huỷ tự đi:** đang Walking đặt `player.MoveInput = (0, 1)` 1 khung → `Cancelled`.
- **Tự xới trọn:** Xới, `input.UseTool(cell, cell)` lên ô có biển → `Farming.TillState == Walking` → (≤ 10 giây) `Tilling` → sau ≥ 6 giây (≤ 8) ô `Tilled`, `TillState == None`, `StatusLine` chứa `"Xới xong ô"`.
- **Huỷ khi đang xới:** đang `Tilling` đặt `MoveInput = (0, 1)` 1 khung → `None`, ô chưa `Tilled`, `StatusLine == "Huỷ tự xới"`. Tab (`input.ToggleTool()`) khi Walking → cũng huỷ.
- **Không tới được:** ô có biển `soil`; ghi thẳng `view.World.Set(x, y, z, Cell.Make(BlockMaterial.Stone))` cho 4 cột kề `(soil.X ± 1, soil.Z)`, `(soil.X, soil.Z ± 1)` với `y = soil.Y + 1 … soil.Y + 3` (bộ tìm đường đọc world trực tiếp, không cần dựng lại mesh) → `BeginSelfTill` false, `StatusLine == "Không tới được ô này"`.
- **Sinh vật xới trước:** đang Walking, gọi `sv.Sim.PlayerTill(id)` (giả sinh vật xong) → khung kế `TillState == None`, `StatusLine == "Tự xới: ô đã được xới"`.
- **Nút Tự xới (xới lại):** đưa ô về pha xới lại bằng API lõi: `MarkTill` → `PlayerTill` → `bag.AddSeeds(0, 1)` → `EnqueueCrop(id, 0)` → `PlayerSow` → `Rain` → `Plots[id].RipeTick = State.Tick` → `Sim.Tick()` (pha Harvest) → `EnqueueCrop(id, 0)` → `PlayerHarvest` → pha `Till`, `Tilled`. `sv.UseTill(cell) == OpenedPlot` → `sv.SelfTillOpenPlot()` true, `OpenPlotId == -1`, `TillState != None`. Ô `Idle` → `SelfTillOpenPlot()` false.
- **UseTill / UnmarkAt:** ô có biển → `UseTill == SelfTill`, ô vẫn có biển; `UnmarkAt` → true, ô `Removed`; `UnmarkAt` lên ô ruộng đã xới → false.
- **Mưa:**
  - ô đất mặt (đào ô cỏ mặt bằng `view.Dig`, ô đất ngay dưới thành ô nhắm) trong tầm → `Grassed`, ô thành cỏ, `RainCooldown.Ready == false`; **cùng khung** (chưa `yield`) `Farming.HudLines()` chứa `"Mưa (R): 10 giây"`, hộp `"Mưa"` đang hiện;
  - gọi lại ngay → `NoEffect`, `StatusLine == "Mưa: còn 10 giây"`;
  - `RainCooldown.Advance(10f)` → ô ruộng pha `Water` → `Watered`, pha `Growing`;
  - ô đá / ô cỏ khi sẵn sàng → `NoEffect`, vẫn `Ready`;
  - ô cách > 8 → `"Mưa: ngoài tầm 8 ô"`, vẫn `Ready`.
- **Thu hoạch:** ô pha `Harvest` trong tầm → `RipePlotAt == id`; `Harvest(id)` true → túi nông sản Cây A +1, hạt +1, HUD chứa `"Nông sản Cây A: 1"`, `StatusLine` chứa `"Thu hoạch Cây A"`; ô chín cách > 8 → `RipePlotAt == -1`.
- **HUD lúc đầu** chứa `"Mưa (R): sẵn sàng"` và dòng Xới mới (`"lên biển = tự đi tới xới"`).

**Cũ có thể bị ảnh hưởng:**
- `SimFarmUiPlayTests` (UseTill đổi; `UnmarkAndDigHideSign` đã sửa), `TerrainEditPlayTests`, `DropPlayTests` (HUD, `UseTool`), `PlayerCharacterPlayTests`, `WalkBenchPlayTests` (`PlayerCharacter.Update` đổi), `SimViewPlayTests`, `SimViewMapsPlayTests` — đều phải qua không đổi kỳ vọng (trừ test đã nêu).
- EditMode — không đổi Core, chạy lại để chắc.

### Lệnh executor chạy
1. `date` (giờ bắt đầu) trước bước 1.
2. Unity Editor mở (MCP): `refresh_unity` → `read_console` không lỗi biên dịch.
3. `run_tests` PlayMode lọc `PlayerFarmPlayTests` → sửa tới khi qua.
4. `run_tests` PlayMode toàn bộ → ghi dòng tổng; `run_tests` EditMode toàn bộ → ghi dòng tổng.
5. `git checkout -- client/ProjectSettings/EditorSettings.asset`.
   - Editor tắt: `python3 tools/unity_test.py PlayMode` rồi `python3 tools/unity_test.py EditMode`.
6. `date` (giờ kết thúc).

### User tự test
Trên Mac, scene SimView (Play), nhân vật (không bay):
1. HUD có `Mưa (R): sẵn sàng`.
2. Tab tới Xới, chuột trái lên đất/cỏ gần nhân vật → có biển. Chuột trái lên biển đó → nhân vật **tự đi tới cạnh ô**, HUD `Đang xới ô …: 0/6 giây` đếm lên, 6 giây sau ô thành ô ruộng, HUD `Xới xong ô …`.
3. Lặp lại, giữa lúc đang đi hoặc đang xới bấm W → HUD `Huỷ tự xới`, ô vẫn chỉ có biển.
4. Chuột phải lên một biển → biển biến mất.
5. Đào một ô cỏ cho lộ ô đất, ngắm ô đất đó, bấm R → hộp xanh nhạt hiện chớp ~0,5 giây, ô thành cỏ, HUD đếm `Mưa (R): 10 giây` lùi. Bấm R lần nữa ngay → `Mưa: còn … giây`.
6. Chọn giống cho ô ruộng vừa xới (bảng ô ruộng); khi sinh vật gieo xong và ô chờ tưới, ngắm ô đó bấm R → ô tưới xong, cây bắt đầu lớn.
7. Khi cây chín (hoặc ô ruộng sinh vật trồng đã chín), chuột trái lên ô đó với bất kỳ công cụ → HUD `Thu hoạch Cây A: +1 nông sản, +1 hạt`, dòng `Nông sản Cây A: 1`, không đào mất ô.
8. Cảm giác nhân vật tự đi (có vấp, kẹt, đi lệch không) — ghi nhận xét.

## Dừng và chuyển lại Opus khi
- Brief mơ hồ hoặc mâu thuẫn với code thật (vd số dòng lệch không tìm được đoạn cần sửa).
- Một test cũ ngoài `UnmarkAndDigHideSign` đổi kết quả — không tự sửa kỳ vọng test cũ.
- Tự đi không tới đích được trong test (kẹt cạnh khối, nhảy không lên) sau 2 lần sửa.
- Cần API Unity không có trong brief.
- Cần một quyết định thuộc AGENTS.md §3.4 hoặc ngoài "Quyết định đã chốt"; phải sửa file ngoài "Phạm vi".

---
<!-- Từ đây trở xuống: EXECUTOR ghi. -->

## Kết quả
Base commit: 745a00e
Chạy bằng: claude-sonnet-5-5 · medium
Bắt đầu: 2026-10-22 18:03 · Kết thúc: 2026-10-22 18:13 (theo `date`)
Commit: `f4138f5`. File: `Client/PlayerCharacter.cs` (tự đi), `Client/PlayerFarmActions.cs` (+meta, mới), `Client/SimView.cs` (SelfTill, UnmarkAt, SelfTillOpenPlot, nút Tự xới), `Client/TerrainEditInput.cs` (R, chuột phải, thu hoạch, HUD), `Tests/Client/PlayerFarmPlayTests.cs` (+meta, mới), `Tests/Client/SimFarmUiPlayTests.cs` (chỉ `UnmarkAndDigHideSign`).
Ghi chú: test chọn ô thử theo khoảng cách Euclid 3,5–6,5 ô (không phải 4–6 Chebyshev) vì ô chéo cách 6 Chebyshev ≈ 8,5 > tầm 8; HUD/bước theo brief nguyên văn.

## Kết quả test
- PlayerFarmPlayTests: 12/12 đạt (AutoWalkArrives, AutoWalkCancelledByKey, SelfTillFull, CancelWhileTilling, TabCancelsWalking, Unreachable, CreatureTillsFirst, SelfTillButtonRetill, UseTillAndUnmarkAt, RainRules, HarvestIntoBag, HudInitial).
- `run_tests` PlayMode toàn bộ: 133/133 đạt. EditMode toàn bộ: 472/472 đạt. Console: 0 lỗi.
- `git checkout -- client/ProjectSettings/EditorSettings.asset` đã chạy.
- User tự test (8 mục, kể cả cảm giác tự đi): chờ user.

## Vướng mắc

---
<!-- PLANNER ghi khi review. -->

## Review
Review đầy đủ (Planner, phiên Opus, 2026-10-22):
- `Chạy bằng` (claude-sonnet-5-5 · medium) khớp `Model thực thi` [repo].
- Phạm vi: `git show --stat f4138f5` — 8 file, đúng danh sách Phạm vi; không đụng Core/scene/design [run].
- Phần cần test: 12 test mới đủ các dòng (tự đi, huỷ, tự xới trọn, huỷ khi xới, Tab, không tới được, sinh vật xới trước, nút Tự xới, UseTill/UnmarkAt, Mưa, thu hoạch, HUD); PlayMode 133/133, EditMode 472/472 theo executor — không chạy lại (kết quả đủ, không đáng ngờ).
- Đối chiếu Quyết định đã chốt 1–10 với diff: `PlayerCharacter` tự đi đúng luật quyết định 5 (huỷ khi phím/nhảy/bay, ngưỡng 0,15, bước không vượt đích, `autoJump` riêng, stall 3 giây); `PlayerFarmActions` đúng thứ tự kiểm và chữ HUD nguyên văn bước 2; `SimView.UseTill` trả `SelfTill`, `UnmarkAt`, `SelfTillOpenPlot`, nút `Tự xới`; `TerrainEditInput` R / thu hoạch trước `ShowAim`, chuột phải bỏ biển, `ToggleTool` + `UseTool` huỷ tự xới, HUD/HelpText nguyên văn.
- Nhỏ, không chặn (gộp vào brief 0042): chuột phải bỏ biển đúng ô đang tự xới → HUD báo `Tự xới: ô đã được xới` (sai ý, nên là `Huỷ tự xới`); R không có tác dụng khi con trỏ đang ngắm vật rơi hoặc trên UI (chấp nhận ở M1).
- Giờ thực tế: 10 phút (18:03–18:13) so với ước lượng ~2 ngày-người.
- User xem: chờ user chạy 8 mục "User tự test" — chưa kết luận.
- User xem đạt [user 2026-10-22] → đã duyệt.
