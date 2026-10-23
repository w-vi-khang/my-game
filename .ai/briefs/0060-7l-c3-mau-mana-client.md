# Brief 0060 — 7l c3: nối máu / mana vào client (tốn mana khi đào / đặt / Mưa, hồi khi đi, rơi cao, ngập đầu, hồi sinh, thanh trên-trái)

> Trạng thái: chờ review
> Review: đầy đủ + user xem
> Model thực thi: Sonnet · medium
> Cập nhật: 2026-10-24

<!-- Planner (Opus) viết, Executor (Sonnet) làm theo ở PHIÊN KHÁC — executor không có lịch sử chat, không được tự quyết. -->

## Mục tiêu
Bước **7l c3** (bước cuối của 7l c) của task [m1-world-7l-dieu-khien-pc](../tasks/m1-world-7l-dieu-khien-pc.md). Luật: [0034 mục 5](../../design/decisions/0034-dieu-khien-pc-click-di-chuyen.md#5-máu-và-mana-năng-lượng). Luật lõi `Game.Core.PlayerVitals` đã có và đã test (brief 0054), client **chưa dùng**. Brief này nối nó vào nhân vật:
1. Đào, đặt khối, phép Mưa **tốn mana**. Không đủ → không làm, **thanh mana nháy**, dòng kết quả báo thiếu.
2. Mana hồi theo quãng đi ngang (`AddMoved`).
3. Máu mất khi **rơi cao** (`Land`) và khi **nước ngập đầu** (`Tick`); tự hồi máu (`Tick`).
4. Máu về 0 → **hiện lại ở chỗ xuất phát**, đầy máu + mana, không mất đồ.
5. **Thanh máu + thanh mana** tạm ở góc trên-trái HUD (7l e làm lại thành avatar + thanh).

Bắt đầu: kiểm brief 0059 có `> Trạng thái: đã duyệt` và commit `431bca3` có trong `git log`. Không có → dừng, `cần Opus`.

**File lạ chưa track** `client/Assets/InitTestScene*.unity(.meta)` (scene tạm của Unity Test Runner): không commit, không xoá khi Editor đang chạy test.

Ước lượng: 0,5–1 ngày-người (phần còn lại của dòng 7l c trong world-tech, M1).

## Bối cảnh cần đọc
Đường dẫn dưới đây tính từ `client/Assets/Game/` trừ khi ghi khác.

**Game.Core:**
- `Core/PlayerVitals.cs` (154 dòng, **không sửa**, đọc hết):
  - `public enum ManaAction : byte { Dig = 0, Place = 1, Rain = 2 }` `:6`;
  - `public PlayerVitals(PlayerConfig c)` `:16`;
  - `Hp`, `HpMax`, `Mana`, `ManaMax`, `IsDead` `:34–38`; event `Changed`, `Died` `:40–42`;
  - `public int Cost(ManaAction a)` `:44`; `public bool CanSpend(ManaAction a)` `:54`; `public bool TrySpend(ManaAction a)` `:56`;
  - `public void AddMoved(float horizontalCells)` `:69` (số ô đi ngang, ≥ 0, mỗi 1 ô cộng `manaRegenPerCell`);
  - `public int Land(float fallCells)` `:86` (rơi > `fallSafeCells` → mất `fallDamagePerCell` mỗi ô vượt, làm tròn xuống; trả máu đã mất);
  - `public void Tick(int ms, bool headUnderwater)` `:95` (ngập đầu: mất `drownDamagePerSecond` mỗi giây; không ngập: hồi `hpRegenPerSecond` mỗi giây sau `hpRegenDelayMs` không mất máu);
  - `public void Respawn()` `:131` (đầy máu + mana, xoá bộ đếm).
- `Core/PlayerRules.cs` (79 dòng): hằng `BodyCenterHeight` `:29`, `BodyHeight` `:31`; `InWater(WorldData world, float feetX, float feetY, float feetZ)` `:65–69`.
- `Core/FlyCameraCollision.cs:88` `public static bool IsUnderwater(WorldData world, float x, float y, float z, float surfaceDrop)` — điểm (x, y, z) ở dưới mặt nước (đã tính mặt nước hạ `surfaceDrop` ở ô nước có không khí phía trên).
- `World/BlockMesher.cs:43` `public const float WaterSurfaceDrop = 0.1f;` (assembly `Game.World`; `Game.Core` **không** tham chiếu được → truyền vào làm tham số, như `UnderwaterView.cs:49` đang làm).
- `Core/PlayerMotion.cs`: thuộc tính tĩnh đọc từ `PlayerConfig` (mẫu `ClickMarkSeconds`; 3 số c2 `ReachRingFadeSeconds` / `CollectShowSeconds` / `CollectFlySeconds` ngay sau).
- `Core/Sim/SimConfig.cs`: `PlayerConfig` `:128–165` (field `collectFlyMs` ngay sau `clickMarkMs`); kiểm biên `collectFlyMs` (khoảng `:409–410`, cùng dạng `clickMarkMs`).
- JSON `design/data/sim_m1.json` khối `"player"` (dòng 33–40): các key mana / máu đã có ở dòng 36–37; dòng 39 có `"collectFlyMs": 300, `. Bản sao `client/Assets/Game/Client/Resources/Config/sim_m1.json` phải giống hệt (`ConfigTests` kiểm).

**Client (đọc hết các đoạn này):**
- `Client/PlayerCharacter.cs` (438 dòng):
  - field `spawnFeetLocal`, `spawnYaw`, `hasSpawn` `:29–31` (dùng cho túi hạt đầu game — **giữ nguyên**, không dùng làm chỗ hồi sinh, vì `Place` ghi đè mỗi lần gọi);
  - `InWater` `:37`; `Awake` `:158–175` (`ConfigLoader.LoadSimConfig();` `:171` trả `SimConfig`, hiện bỏ giá trị trả về);
  - `Place(Vector3 feetWorld, float yawDeg)` `:236–250`;
  - `Update` `:303–437`: `if (!Ready) {...}` `:319–325`; `InWater = ...` `:342`; `Controller.Move` + `moved` / `movedH` `:410–414`.
- `Client/TerrainEditInput.cs` (≈ 565 dòng):
  - field `lastResult` `:31`;
  - `Update` `:82–…` (`Walk.Tick` `:88`, `DropHeldIfEmpty();` `:89`);
  - `RightClickAt` `:266–288` (dòng `if (a == RightClickAction.None || a == RightClickAction.OutOfReach) return a;` `:274`);
  - `Execute` `:290–316` (`case RightClickAction.PlaceBlock:` `:305–309`);
  - `DoDig` `:318–324`;
  - `OnGUI` `:521–536` (chữ HUD vẽ ở `new Rect(10, 10, 1000, h)` `:534`, `HudBottom = 10f + h;` `:535`).
- `Client/PlayerFarmActions.cs`: `CastRain(Vector3Int cell)` `:199–227` (kiểm hồi chiêu `:207–211`, gọi `sim.Rain` `:213`, `RainCooldown.Start();` `:222`).
- `Client/WalkToAct.cs:36` `public void Cancel(PlayerCharacter player, bool stop)`.
- `Client/SimView.cs:622–627` `Tile` — mẫu vẽ ô màu bằng `GUI.DrawTexture(r, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0f, c, 0f, radius)`.
- `Client/Resources/Lang/vi.json` + `loc/en.json` + `loc/context.json` (gốc repo; khoá xếp theo bảng chữ cái).
- Test mẫu: `Tests/Client/ControlModePlayTests.cs` (`SetUp`, `Ahead(n)`, `WaitWalk()`, `Wait(s)`, `SetBag`, test `DigWalksToTwoCellsThenDigs` `:174`, `RainAfterWalking` `:273`); `Tests/Client/PlayerCharacterPlayTests.cs` (`SetUp` có `Time.captureFramerate = 60`, `Teleport(feetLocal)` `:81`, `G`, `A`, test `WaterHalvesSpeed` `:203` tìm cột nước sâu ≥ 4 bằng `MinimapImage.ColumnInfo`); `Tests/Core/PlayerRulesTests.cs` (test `InWater` `:60`).

API Unity dùng (6000.4). Không dùng API nào khác:
- `CharacterController.isGrounded` ([docs](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/CharacterController-isGrounded.html)) — đã dùng.
- `Transform.InverseTransformVector` ([docs](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/Transform.InverseTransformVector.html)), `Transform.TransformPoint` ([docs](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/Transform.TransformPoint.html)) — đã dùng.
- `Time.deltaTime` ([docs](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/Time-deltaTime.html)), `Time.time` ([docs](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/Time-time.html)).
- `GUI.DrawTexture` (overload 8 tham số có `Color` + `borderRadius`, như `SimView.Tile`) ([docs](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/GUI.DrawTexture.html)), `Texture2D.whiteTexture` ([docs](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/Texture2D-whiteTexture.html)), `GUI.Label` ([docs](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/GUI.Label.html)).

## Quyết định đã chốt (không được đổi)
1. **Luật số** lấy nguyên từ `PlayerVitals` + JSON có sẵn (0034 mục 5, [số tạm]): mana 100, đào 5, đặt 3, Mưa 15, hồi 2 mỗi ô; máu 100, rơi > 4 ô mất 10/ô vượt, ngập đầu 5/giây, hồi 1/giây sau 5 giây. **Không** sửa `PlayerVitals.cs`, không đổi các số này.
2. **Chủ sở hữu:** `PlayerCharacter` giữ `public PlayerVitals Vitals { get; private set; }`, tạo trong `Awake` từ `ConfigLoader.LoadSimConfig().player`. Không có nhân vật (scene không có `PlayerCharacter`) → không tốn mana, không thanh.
3. **Tốn mana chỉ khi làm thành công** (Planner): kiểm `CanSpend` trước; làm; kết quả Ok / có hiệu ứng → `TrySpend`.
   - Đào: `view.Dig` trả `EditResult.Ok`. Đặt: `view.Place` trả `Ok`. Mưa: `CastRain` ra `Watered` / `Grassed`.
   - Kiểm hai lần: lúc **chuột phải** (`RightClickAt`, trước khi đi tới — thiếu thì không đi) và lúc **làm** (`DoDig` / `PlaceBlock` / `CastRain` — vì `CastRain` còn được gọi thẳng từ code).
   - Thiếu: không làm, `player.FlashMana()`, dòng kết quả `vitals.no_mana` (cần N, còn M). `RightClickAt` khi thiếu trả `RightClickAction.None`.
   - Xới, gieo, thu hoạch, nhặt, đặt camera quan sát, lướt: không tốn (0034 mục 5 "hành động khác không tốn").
4. **Hồi mana theo đi:** mỗi frame sau `Controller.Move`, quãng đi ngang (toạ độ terrain) → `Vitals.AddMoved`. Tính cả lướt và tự đi. Không tính khi `FlyMode`. `Place` (dịch chuyển) không tính.
5. **Rơi:** đo từ điểm cao nhất của chân khi không chạm đất tới lúc chạm đất → `Vitals.Land(cao nhất − hiện tại)`.
   - Đang ở trong nước (`InWater`) → điểm cao nhất đặt lại bằng chân hiện tại: **rơi xuống nước không mất máu** (đề xuất Planner — 0034 chỉ nói "rơi từ chỗ quá cao"; nước đỡ là cách hiểu tự nhiên, user sửa được khi xem).
   - `FlyMode` → đặt lại, không tính. `Place` → đặt lại về chân mới.
6. **Ngập đầu:** điểm đầu = chân + `PlayerRules.HeadHeight` (= **1,5**, ngang mắt; đầu hình khối ở 1,13–1,83). Ngập = `FlyCameraCollision.IsUnderwater(world, x, chân + 1,5, z, BlockMesher.WaterSurfaceDrop)`. Hàm mới ở Core: `PlayerRules.HeadUnderwater(world, feetX, feetY, feetZ, surfaceDrop)`.
7. **Thời gian:** mỗi frame (sau khi `Ready`, không `FlyMode`) cộng `Time.deltaTime × 1000` vào bộ đếm float, lấy phần nguyên ms → `Vitals.Tick(ms, HeadUnderwater)`, giữ phần lẻ.
8. **Hồi sinh** (0034 mục 5, đề xuất Planner đã ghi trong 0034):
   - **Chỗ xuất phát** = chỗ của lần gọi `Place` **đầu tiên** (field mới `respawnFeetLocal`, `respawnYaw`, `hasRespawn`; không dùng `spawnFeetLocal` vì `Place` ghi đè mỗi lần — test dịch chuyển nhân vật nhiều lần).
   - `IsDead` → `Place(chỗ xuất phát)`, `StopAutoWalk()`, `Vitals.Respawn()`, `Respawns++`. Chưa có chỗ xuất phát → chỉ `Vitals.Respawn()` + `Respawns++` tại chỗ.
   - Không mất đồ: không đụng túi, vật đang cầm giữ nguyên.
   - `TerrainEditInput` thấy `Respawns` tăng → huỷ đi tới (`Walk.Cancel(player, false)`), huỷ tự xới, dòng kết quả `vitals.respawned`.
9. **Thanh nháy** khi thiếu mana: `PlayerCharacter.FlashMana()` → nháy trong `PlayerMotion.ManaFlashSeconds`; tham số mới `manaFlashMs` **600** `[số tạm]`, chưa chốt, kiểm biên [50, 5000] như `clickMarkMs`. Không bump `configVersion`.
10. **Thanh HUD tạm** (7l e làm lại thành avatar + thanh, 0034 mục 7): trong `TerrainEditInput.OnGUI`, khi có `PlayerCharacter.Active`:
    - hai thanh ở `x = 10`: máu `y = 10`, mana `y = 26`; mỗi thanh nền `160 × 12`, màu nền `(0, 0, 0, 0.5)`, phần đầy rộng `160 × giá trị / tối đa`, bo góc 3;
    - màu máu `(0.85, 0.2, 0.2, 1)`, mana `(0.25, 0.5, 1, 1)`;
    - đang nháy: nền mana đổi giữa `(0, 0, 0, 0.5)` và `(1, 1, 1, 0.8)` mỗi 0,1 giây (`Mathf.FloorToInt(Time.time * 10f) % 2 == 0` → trắng);
    - nhãn chữ bên phải mỗi thanh ở `x = 176`, rộng 200, cao 16: `vitals.hp` / `vitals.mana`;
    - chữ HUD cũ dời xuống `y = 44`; `HudBottom = 44f + h`. Không có nhân vật → như cũ (`y = 10`).
11. Không làm: avatar, menu phép G, bỏ chữ hướng dẫn (7l e); túi ô vuông (7l d); nâng tầm / trang bị chỉ số; hiệu ứng âm thanh / màn hình đỏ khi mất máu.

## Phạm vi
- Được sửa:
  - Core: `Core/PlayerRules.cs` (thêm `HeadHeight` + `HeadUnderwater`), `Core/PlayerMotion.cs` (thêm `ManaFlashSeconds`), `Core/Sim/SimConfig.cs` (chỉ field `manaFlashMs` + 1 kiểm biên).
  - JSON: `design/data/sim_m1.json`, `client/Assets/Game/Client/Resources/Config/sim_m1.json` (chỉ thêm key `manaFlashMs`).
  - Client: `Client/PlayerCharacter.cs`, `Client/TerrainEditInput.cs`, `Client/PlayerFarmActions.cs` (chỉ `CastRain`).
  - Chuỗi: `Client/Resources/Lang/vi.json`, `loc/en.json`, `loc/context.json`.
  - Test: `Tests/Core/PlayerRulesTests.cs`, `Tests/Core/Sim/SimConfigTests.cs`, `Tests/Core/PlayerMotionTests.cs`, `Tests/Client/ControlModePlayTests.cs`, `Tests/Client/PlayerCharacterPlayTests.cs`; test cũ khác chỉ theo quy tắc bước 9.
  - File brief này (mục Kết quả / Kết quả test / Vướng mắc).
- Không được sửa: `Core/PlayerVitals.cs`, `Tests/Core/PlayerVitalsTests.cs`, `Core/ControlState.cs`, `Core/PlayerBag.cs`, `Client/WalkToAct.cs`, `Client/SimView.cs`, `Client/MoveMarkers.cs`, `design/` ngoài `design/data/sim_m1.json`, task contract, mọi file khác.

## Các bước
1. **Core — đầu ngập nước:** `Core/PlayerRules.cs`
   - sau `public const float BodyHeight = 1.8f;` thêm
     ```csharp
     /// <summary>Điểm đầu (ngang mắt) cao hơn chân chừng này — dùng cho ngập đầu (0034 mục 5).</summary>
     public const float HeadHeight = 1.5f;
     ```
   - sau hàm `InWater` thêm
     ```csharp
     /// <summary>Đầu ngập nước (0034 mục 5): điểm chân + HeadHeight dưới mặt nước. surfaceDrop = BlockMesher.WaterSurfaceDrop (client truyền vào).</summary>
     public static bool HeadUnderwater(WorldData world, float feetX, float feetY, float feetZ, float surfaceDrop) =>
         FlyCameraCollision.IsUnderwater(world, feetX, feetY + HeadHeight, feetZ, surfaceDrop);
     ```
   - Xong khi: biên dịch không lỗi; `grep -rn "UnityEngine" client/Assets/Game/Core` như trước brief.
2. **Core — tham số nháy:**
   - `SimConfig.cs` `PlayerConfig`: sau `public int collectFlyMs = 300;` thêm `public int manaFlashMs = 600;`.
   - Kiểm biên: sau cặp dòng `collectFlyMs` thêm
     ```csharp
     if (player.manaFlashMs < 50 || player.manaFlashMs > 5000)
         throw new ArgumentException("player.manaFlashMs must be in [50, 5000].");
     ```
   - `PlayerMotion.cs`: sau `CollectFlySeconds` thêm
     ```csharp
     /// <summary>Thanh mana nháy chừng này giây khi không đủ mana (0034 mục 5, số tạm).</summary>
     public static float ManaFlashSeconds { get; private set; } = 0.6f;
     ```
     và trong `Configure` sau dòng `CollectFlySeconds = ...` thêm `ManaFlashSeconds = p.manaFlashMs / 1000f;`.
   - JSON (2 bản, giống hệt): trong dòng 39 của khối `player`, sau `"collectFlyMs": 300, ` chèn `"manaFlashMs": 600, `.
   - Xong khi: `diff design/data/sim_m1.json client/Assets/Game/Client/Resources/Config/sim_m1.json` rỗng.
3. **PlayerCharacter — máu / mana** (`Client/PlayerCharacter.cs`):
   - Thuộc tính / field mới (cạnh các thuộc tính đầu lớp):
     ```csharp
     /// <summary>Máu + mana (0034 mục 5, brief 0060).</summary>
     public PlayerVitals Vitals { get; private set; }
     /// <summary>Đầu ngập nước ở frame vừa rồi.</summary>
     public bool HeadUnderwater { get; private set; }
     /// <summary>Số lần hồi sinh (máu về 0); TerrainEditInput theo dõi để huỷ việc đang làm.</summary>
     public int Respawns { get; private set; }
     /// <summary>Chỗ xuất phát = chỗ Place lần đầu (toạ độ terrain); hồi sinh về đây.</summary>
     public Vector3 RespawnFeetLocal => respawnFeetLocal;
     public bool ManaFlashing => manaFlashLeft > 0f;
     Vector3 respawnFeetLocal;
     float respawnYaw, manaFlashLeft, airPeakY, vitalsMsAcc;
     bool hasRespawn, airborne;
     ```
   - `Awake`: thay `ConfigLoader.LoadSimConfig();` bằng `Vitals = new PlayerVitals(ConfigLoader.LoadSimConfig().player);`.
   - `public void FlashMana() => manaFlashLeft = PlayerMotion.ManaFlashSeconds;` (summary: "Thiếu mana: thanh mana nháy (0034 mục 5).").
   - `Place`: sau dòng `if (terrain != null) { spawnFeetLocal = ... }` thêm
     ```csharp
     if (terrain != null && !hasRespawn) { respawnFeetLocal = terrain.transform.InverseTransformPoint(feetWorld); respawnYaw = yawDeg; hasRespawn = true; }
     airborne = false;
     if (terrain != null) airPeakY = terrain.transform.InverseTransformPoint(feetWorld).y;
     ```
   - `Update`: ngay sau dòng `if (terrain == null || terrain.World == null) return;` thêm `if (manaFlashLeft > 0f) manaFlashLeft -= Time.deltaTime;`.
   - `Update`: sau dòng `if (Dashing && movedH < wantH * 0.5f) Dash.Stop();` thêm gọi `UpdateVitals(moved);` và viết hàm:
     ```csharp
     /// <summary>Mana hồi theo quãng đi, rơi cao, ngập đầu, hồi sinh (0034 mục 5, brief 0060).</summary>
     void UpdateVitals(Vector3 movedWorld)
     {
         var fa = FeetLocal;
         if (!FlyMode)
         {
             var ml = terrain.transform.InverseTransformVector(movedWorld);
             float cells = Mathf.Sqrt(ml.x * ml.x + ml.z * ml.z);
             if (cells > 0f) Vitals.AddMoved(cells);
         }
         if (FlyMode || PlayerRules.InWater(terrain.World, fa.x, fa.y, fa.z)) { airborne = false; airPeakY = fa.y; }
         else if (!Controller.isGrounded) { if (!airborne) { airborne = true; airPeakY = Mathf.Max(airPeakY, fa.y); } else airPeakY = Mathf.Max(airPeakY, fa.y); }
         else
         {
             if (airborne) Vitals.Land(Mathf.Max(0f, airPeakY - fa.y));
             airborne = false;
             airPeakY = fa.y;
         }
         HeadUnderwater = PlayerRules.HeadUnderwater(terrain.World, fa.x, fa.y, fa.z, Game.World.BlockMesher.WaterSurfaceDrop);
         if (!FlyMode)
         {
             vitalsMsAcc += Time.deltaTime * 1000f;
             int ms = (int)vitalsMsAcc;
             vitalsMsAcc -= ms;
             if (ms > 0) Vitals.Tick(ms, HeadUnderwater);
         }
         if (Vitals.IsDead) RespawnAtStart();
     }

     /// <summary>Máu về 0 (0034 mục 5): về chỗ xuất phát, đầy máu + mana, không mất đồ.</summary>
     void RespawnAtStart()
     {
         StopAutoWalk();
         if (hasRespawn) Place(terrain.transform.TransformPoint(respawnFeetLocal), respawnYaw);
         Vitals.Respawn();
         Respawns++;
     }
     ```
     Ghi chú: `airPeakY` khi đang đứng = chân hiện tại, nên bước khỏi mép thì điểm cao nhất = mặt đất vừa rời. Nếu `Game.World` chưa có `using`, dùng tên đầy đủ như trên (Client đã tham chiếu `Game.World`, xem `UnderwaterView.cs`).
   - Summary lớp: nối thêm câu "Máu + mana (0034 mục 5, brief 0060): Vitals; tốn mana ở TerrainEditInput / PlayerFarmActions; rơi cao, ngập đầu, hồi sinh ở chỗ Place lần đầu."
   - Xong khi: biên dịch không lỗi.
4. **TerrainEditInput — tốn mana** (`Client/TerrainEditInput.cs`):
   - Hàm mới:
     ```csharp
     /// <summary>Đủ mana cho việc m (không có nhân vật → luôn đủ). Thiếu → thanh nháy + dòng "Không đủ mana" (0034 mục 5).</summary>
     bool HasMana(ManaAction m)
     {
         var p = PlayerCharacter.Active;
         if (p == null || p.Vitals.CanSpend(m)) return true;
         p.FlashMana();
         lastResult = Format("vitals.no_mana", p.Vitals.Cost(m), p.Vitals.Mana);
         return false;
     }

     static void SpendMana(ManaAction m)
     {
         var p = PlayerCharacter.Active;
         if (p != null) p.Vitals.TrySpend(m);
     }

     static bool ManaOf(RightClickAction a, out ManaAction m)
     {
         m = a == RightClickAction.Dig ? ManaAction.Dig : a == RightClickAction.PlaceBlock ? ManaAction.Place : ManaAction.Rain;
         return a == RightClickAction.Dig || a == RightClickAction.PlaceBlock || a == RightClickAction.Rain;
     }
     ```
   - `RightClickAt`: ngay sau dòng `if (a == RightClickAction.None || a == RightClickAction.OutOfReach) return a;` thêm
     ```csharp
     if (ManaOf(a, out var cost) && !HasMana(cost)) return RightClickAction.None; // thiếu mana: không đi, không làm
     ```
   - `DoDig`: đầu hàm thêm `if (!HasMana(ManaAction.Dig)) return;`; sau `var r = view.Dig(digCell);` thêm `if (r == EditResult.Ok) SpendMana(ManaAction.Dig);`.
   - `Execute` `case RightClickAction.PlaceBlock:` → trước `var p = view.Place(...)` thêm `if (!HasMana(ManaAction.Place)) break;`; sau dòng `var p = ...` thêm `if (p == EditResult.Ok) SpendMana(ManaAction.Place);`. (`material` khai báo trước dòng `break` mới — giữ thứ tự hợp lệ C#.)
   - Rain không thêm ở `Execute` (đã kiểm trong `CastRain`, bước 5).
   - Hồi sinh: field `PlayerCharacter seenPlayer; int seenRespawns;`. Trong `Update`, ngay sau `var player = PlayerCharacter.Active;` thêm
     ```csharp
     if (player != seenPlayer) { seenPlayer = player; seenRespawns = player != null ? player.Respawns : 0; }
     else if (player != null && player.Respawns != seenRespawns)
     {
         seenRespawns = player.Respawns;
         Walk.Cancel(player, false);
         Farming.CancelSelfTill();
         lastResult = T("vitals.respawned");
     }
     ```
   - Xong khi: biên dịch không lỗi.
5. **PlayerFarmActions.CastRain:** sau khối kiểm hồi chiêu (`if (!RainCooldown.Ready) {...}`) thêm
   ```csharp
   if (player != null && !player.Vitals.CanSpend(ManaAction.Rain))
   {
       player.FlashMana();
       StatusLine = Format("vitals.no_mana", player.Vitals.Cost(ManaAction.Rain), player.Vitals.Mana);
       return RainResult.NoEffect;
   }
   ```
   và ngay trước `RainCooldown.Start();` thêm `if (player != null) player.Vitals.TrySpend(ManaAction.Rain);`. Summary `CastRain` (nếu có) nối "Tốn mana (0034 mục 5)."
6. **Thanh HUD** (`TerrainEditInput.OnGUI`, quyết định 10): sau `hudStyle ??= ...`, trước `float h = ...`:
   ```csharp
   var pc = PlayerCharacter.Active;
   float top = 10f;
   if (pc != null)
   {
       var v = pc.Vitals;
       Bar(10f, v.Hp, v.HpMax, new Color(0.85f, 0.2f, 0.2f, 1f), false);
       Bar(26f, v.Mana, v.ManaMax, new Color(0.25f, 0.5f, 1f, 1f), pc.ManaFlashing);
       GUI.Label(new Rect(176f, 8f, 200f, 16f), Format("vitals.hp", v.Hp, v.HpMax), hudStyle);
       GUI.Label(new Rect(176f, 24f, 200f, 16f), Format("vitals.mana", v.Mana, v.ManaMax), hudStyle);
       top = 44f;
   }
   ```
   đổi `new Rect(10, 10, 1000, h)` → `new Rect(10f, top, 1000f, h)` và `HudBottom = 10f + h;` → `HudBottom = top + h;`. Hàm:
   ```csharp
   static void Bar(float y, int value, int max, Color fill, bool flash)
   {
       var bg = flash && Mathf.FloorToInt(Time.time * 10f) % 2 == 0 ? new Color(1f, 1f, 1f, 0.8f) : new Color(0f, 0f, 0f, 0.5f);
       GUI.DrawTexture(new Rect(10f, y, 160f, 12f), Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0f, bg, 0f, 3f);
       float w = max > 0 ? 160f * Mathf.Clamp01((float)value / max) : 0f;
       if (w > 0f) GUI.DrawTexture(new Rect(10f, y, w, 12f), Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0f, fill, 0f, 3f);
   }
   ```
   - Xong khi: biên dịch không lỗi; `HardcodedTextScanTests` qua (chữ đi qua bảng chuỗi).
7. **Chuỗi** — `vi.json` (đúng thứ tự bảng chữ cái như file đang có), `loc/en.json` (cùng thứ tự), `loc/context.json` (object `key: ngữ cảnh`). **Thêm** cả 3 file (`{0}`, `{1}` là chỗ điền):

   | key | vi | en | context |
   |---|---|---|---|
   | `vitals.hp` | `Máu {0}/{1}` | `HP {0}/{1}` | `Label next to the health bar in the top-left HUD. {0} = current health, {1} = max health. 'Máu' = the player character's health.` |
   | `vitals.mana` | `Mana {0}/{1}` | `Mana {0}/{1}` | `Label next to the mana (energy) bar in the top-left HUD. {0} = current mana, {1} = max mana. Mana is spent on digging, placing blocks and the Rain spell; it refills while walking.` |
   | `vitals.no_mana` | `Không đủ mana: cần {0}, còn {1}` | `Not enough mana: need {0}, have {1}` | `Result line when the player tries to dig, place a block or cast Rain without enough mana; nothing happens and the mana bar flashes. {0} = mana the action costs, {1} = mana left.` |
   | `vitals.respawned` | `Bạn ngất đi và tỉnh lại ở chỗ xuất phát (đầy máu + mana, không mất đồ)` | `You passed out and woke up at the starting point (full HP + mana, nothing lost)` | `Result line after the character's health reaches 0 (long fall or drowning): the character reappears at the starting point with full health and mana and keeps all items.` |

   - Xong khi: parse JSON 3 file chạy được; `python3 "<Plugin root>/skills/translation/scripts/loc_check.py" check loc/en.json` exit 0 (Plugin root ở "Trạng thái phiên").
8. **Test mới** (chuỗi tiếng Việt trong assert ghi chú `// vi-text` như các test khác; tên đúng như dưới):
   - `Tests/Core/PlayerRulesTests.cs` — **`HeadUnderwater`**: dựng `WorldData` nhỏ như test `InWater` `:60`: cột nước 3 ô (y = 1..3) trên đáy đá y = 0, không khí trên → chân y = 1 (đầu 2,5) → true; chỉ 1 ô nước (y = 1), y = 2 trống → chân y = 1 → false; chân y = 2,5 (đầu 4,0 ngoài nước) → false.
   - `Tests/Core/Sim/SimConfigTests.cs`: assert mặc định `manaFlashMs` = 600 (cạnh assert `collectFlyMs`); `Bad(c => c.player.manaFlashMs = 6000, "manaFlashMs 6000")` cạnh dòng `collectFlyMs`.
   - `Tests/Core/PlayerMotionTests.cs` `ConfigureConvertsMs`: thêm `manaFlashMs = 800` vào `new PlayerConfig { … }` và `Assert.AreEqual(0.8f, PlayerMotion.ManaFlashSeconds, 1e-5f);`.
   - `Tests/Client/ControlModePlayTests.cs` (dùng `SetUp`, `Ahead(n)`, `WaitWalk()`, `Wait(s)` có sẵn; helper mới `void Drain() { while (player.Vitals.TrySpend(ManaAction.Dig)) { } }` → mana 0):
     - **`DigSpendsMana`**: `ToggleMode(Dig)`, `RightClickAt` lên `Ahead(1)` (trong 2 ô) → đợi `WaitWalk()` → khối thành `Empty`, `player.Vitals.Mana == 95`.
     - **`NoManaBlocksDigAndFlashes`**: `Drain()`; `ToggleMode(Dig)`; `RightClickAt` lên `Ahead(6)` → trả `None`; `input.Walk.State != WalkToActState.Walking`; `player.ClickMove == ClickMoveState.None`; khối không đổi; `player.ManaFlashing`; `input.HudText()` chứa `Không đủ mana: cần 5, còn 0` // vi-text; sau `Wait(PlayerMotion.ManaFlashSeconds + 0.2f)` → `!player.ManaFlashing`.
     - **`PlaceSpendsMana`**: `SetBag(Dirt, 2)`, `HoldItem(ItemId.Block(BlockMaterial.Dirt))`, `RightClickAt` đặt lên trên `Ahead(2)` (placeCell = `Ahead(2) + up`) → đợi → ô đặt là `Dirt`, `Mana == 97`.
     - **`RainSpendsMana`**: như `RainAfterWalking` `:273` nhưng ô `Ahead(1)` (đổi thành `Dirt`), `ToggleMode(Rain)`, `RightClickAt` → `WaitWalk()` → `!farming.RainCooldown.Ready`, `player.Vitals.Mana == 85`.
     - **`RainBlockedWithoutMana`**: ô `Ahead(1)` đổi thành `Dirt`; `Drain()`; `farming.CastRain(Ahead(1))` trả `RainResult.NoEffect`; `farming.RainCooldown.Ready` vẫn true; `farming.StatusLine` chứa `Không đủ mana: cần 15, còn 0` // vi-text; vật liệu ô `Ahead(1)` vẫn `Dirt`; `player.ManaFlashing`.
     - **`WalkingRegensMana`**: `Drain()`; `player.MoveTo(new Vector3(spot.x + 6.5f, 0f, spot.z + 0.5f))` (toạ độ terrain; y bỏ qua); đợi tới khi `player.ClickMove != ClickMoveState.Walking` (tối đa 5 giây, mẫu như `WaitWalk`); quãng đi ngang ≈ 6 ô → `player.Vitals.Mana >= 10` và `<= 12`.
     - **`RespawnShowsLineAndCancelsWalk`**: `ToggleMode(Dig)`, `RightClickAt` lên `Ahead(6)` (đang Walking); đẩy chết bằng cách dịch chuyển lên cao: `player.Place(view.transform.TransformPoint(new Vector3(spot.x + 0.5f, spot.y + 1f + 16.5f, spot.z + 0.5f)), 0f)`; đợi tối đa 5 giây tới khi `player.Respawns == 1`; rồi 1 frame → `input.Walk.State != WalkToActState.Walking`, `input.HudText()` chứa `tỉnh lại ở chỗ xuất phát` // vi-text, `Hp == HpMax`, `Mana == ManaMax`.
   - `Tests/Client/PlayerCharacterPlayTests.cs` (dùng `Teleport`, `G`, `A` có sẵn; helper `IEnumerator Land(int frames = 240)` đợi tới khi `player.Controller.isGrounded` rồi thêm 2 frame; đầu mỗi test gọi `player.Vitals.Respawn()` để đầy máu):
     - **`FallOverFourCellsHurts`**: `Teleport(new Vector3(A.x + 0.5f, G + 1 + 4.5f, A.z + 0.5f))` → `Land()` → `Hp == 100`; `Teleport(... G + 1 + 6.5f ...)` → `Land()` → `Hp == 80`.
     - **`FallIntoWaterNoDamage`**: tìm cột nước sâu ≥ 4 như `WaterHalvesSpeed` (không có → `Assert.Inconclusive`); `Teleport(new Vector3(wx + 0.5f, ws + d + 1 + 6.5f, A.z + 0.5f))` (`d` = waterDepth) → đợi chạm đáy → `Hp == 100`.
     - **`HeadUnderwaterDrowns`**: cùng cột nước; `Teleport` tới đáy (`ws + 1`) → đợi chạm đáy → `player.HeadUnderwater`; ghi `hp0 = Hp`; chạy `2,2` giây (`Run(Vector2.zero, false, 2.2f)`) → `hp0 − Hp` trong [10, 15]; rồi `Teleport` lên đất `(A.x + 0.5f, G + 1, A.z + 0.5f)` → `!player.HeadUnderwater`; ghi `hp1`; chạy 1 giây → `Hp == hp1` (chưa đủ 5 giây để hồi, không mất thêm).
     - **`DeathRespawnsAtStartFull`**: `view.Bag.Add(BlockMaterial.Stone, 3)`; ghi `start = player.RespawnFeetLocal`; `Teleport(new Vector3(A.x + 0.5f, G + 1 + 16.5f, A.z + 0.5f))` → đợi tối đa 5 giây tới khi `player.Respawns == 1` → `Hp == HpMax`, `Mana == ManaMax`, khoảng cách ngang `FeetLocal` ↔ `start` < 0,6; `view.Bag.Get(BlockMaterial.Stone) >= 3` (không mất đồ).
   - Xong khi: các test trên có trong file, biên dịch không lỗi.
9. **Test cũ hỏng** — quy tắc (giữ ý nghĩa test):
   - Hỏng vì **hết mana** (test đào / đặt / Mưa nhiều lần trong một test): gọi `player.Vitals.Respawn()` (nạp đầy) ngay trước lệnh bị chặn. Ghi tên test vào Kết quả.
   - Hỏng vì `HudBottom` / vị trí chữ HUD dời xuống 34 đơn vị: sửa số mong đợi theo `top = 44` (quyết định 10). Ghi tên.
   - Hỏng vì **mất máu / hồi sinh** (nhân vật bị dịch chuyển về chỗ xuất phát giữa test) hoặc lý do khác → dừng, `cần Opus`.

## Tiêu chí xong
- EditMode + PlayMode qua hết, 0 lỗi console (qua MCP, xem "Lệnh executor chạy").
- `diff design/data/sim_m1.json client/Assets/Game/Client/Resources/Config/sim_m1.json` → không khác.
- `loc_check.py check loc/en.json` exit 0.
- `Game.Core` không thêm `using UnityEngine` (`grep -rn "UnityEngine" client/Assets/Game/Core` như trước brief).
- `git diff --stat <base>..HEAD -- client/Assets/Game/Core/PlayerVitals.cs client/Assets/Game/Tests/Core/PlayerVitalsTests.cs` rỗng.
- `python3 tools/check_design.py` exit 0.
- Mỗi test ở bước 8 có trong file và qua.

## Test sau khi xong

### Phần cần test
- Đầu ngập nước (bước 1) · `PlayerRulesTests.HeadUnderwater` · 3 trường hợp đúng.
- Tham số nháy (bước 2) · `SimConfigTests` (gồm `ConfigMatchesCodeDefaults`, `Bad manaFlashMs 6000`), `PlayerMotionTests.ConfigureConvertsMs`, `ConfigTests` (2 bản JSON giống hệt) · đọc đúng 600 / 800 ms, kiểm biên.
- Tốn mana đào / đặt (bước 4) · `DigSpendsMana`, `PlaceSpendsMana` · 95 / 97 sau một lần làm thành công.
- Thiếu mana chặn + nháy (bước 4, 6) · `NoManaBlocksDigAndFlashes` · trả `None`, không đi, khối không đổi, nháy rồi hết nháy, dòng `Không đủ mana: cần 5, còn 0`.
- Mưa tốn mana (bước 5) · `RainSpendsMana`, `RainBlockedWithoutMana` · 85 sau Mưa; thiếu → `NoEffect`, hồi chiêu không chạy, ô không đổi, dòng báo.
- Hồi mana khi đi (bước 3) · `WalkingRegensMana` · đi ~6 ô từ 0 → 10–12.
- Rơi cao (bước 3) · `FallOverFourCellsHurts`, `FallIntoWaterNoDamage` · 4,5 ô → 0; 6,5 ô → −20; rơi xuống nước → 0.
- Ngập đầu (bước 1, 3) · `HeadUnderwaterDrowns` · 2,2 giây mất 10–15; lên bờ không mất thêm.
- Hồi sinh (bước 3, 4) · `DeathRespawnsAtStartFull`, `RespawnShowsLineAndCancelsWalk` · về chỗ Place đầu, đầy máu + mana, không mất đồ, huỷ đi tới, dòng báo.
- Thanh HUD + chuỗi (bước 6, 7) · `HardcodedTextScanTests`, `SettingsPlayTests` (ngôn ngữ), `loc_check.py check loc/en.json`, User tự test mục 1 · không thiếu khoá, placeholder khớp; thanh hiện đúng chỗ.
- Luật lõi không đổi · `PlayerVitalsTests` (toàn bộ, không sửa) · qua.
- Regression điều khiển (đào / đặt / Mưa / cầm / F, có thể hết mana hoặc HUD dời) · toàn bộ `ControlModePlayTests`, `PlayerBagSowPlayTests`, `TerrainEditPlayTests`, `PlayerFarmPlayTests`, `SimFarmUiPlayTests`, `ObservationCameraPlayTests`, `DropPlayTests` · qua (sửa theo quy tắc bước 9, ghi tên).
- Regression di chuyển / nước / camera (rơi, ngập có thể làm mất máu) · `PlayerCharacterPlayTests` (gồm `WaterHalvesSpeed`, `FallsWhenGroundDugBelow`, `JumpsOneBlockStepButNotTwo`), `ClickMovePlayTests`, `CameraPcPlayTests`, `EditOcclusionPlayTests`, `MinimapCornerPlayTests`, `MinimapViewPlayTests`, `TerrainStreamingPlayTests` · qua.
- Toàn bộ EditMode (gồm `GameControlsTests`, `NoDirectInputReadsInScope`) · qua.

### Lệnh executor chạy
1. `diff` 2 bản JSON + parse JSON (vi.json, en.json, context.json, 2 bản sim_m1.json) + `python3 "<Plugin root>/skills/translation/scripts/loc_check.py" check loc/en.json` + `grep -rn "UnityEngine" client/Assets/Game/Core` + `python3 tools/check_design.py`.
2. Unity Editor đang mở qua MCP ([0010](../../design/decisions/0010-mcp-for-unity.md)):
   - `refresh_unity` → `read_console` (0 lỗi biên dịch);
   - `run_tests` EditMode (toàn bộ) → `run_tests` PlayMode (toàn bộ);
   - `read_console` lỗi sau khi chạy.
   - Ghi dòng tổng kết số test thật.
   - Editor tắt → `sh "<Plugin root>/scripts/run.sh" verify_run client_test client_test_play`.
3. **Dọn:** sau `run_tests` PlayMode → `git checkout -- client/ProjectSettings/EditorSettings.asset`. Không commit `client/Assets/InitTestScene*`.
4. `sh "<Plugin root>/scripts/run.sh" verify_run --all` trước commit (client_test / client_test_play exit 1 vì Editor đang mở → ghi `Chưa kiểm được` + kết quả MCP như brief 0059).
5. Commit code + test + chuỗi, message `brief 0060: 7l c3 wire HP/mana into client (costs, regen, fall, drowning, respawn, HUD bars)`; commit kết quả brief riêng `brief 0060: result`. Không push.
6. Ghi giờ bắt đầu / kết thúc **mọi vòng** vào Kết quả (giờ thực tế M1).

### User tự test
Mở scene SimView trên Mac, Play:
1. **Thanh:** góc trên-trái có 2 thanh — đỏ `Máu 100/100`, xanh `Mana 100/100`; chữ HUD cũ nằm ngay dưới, không đè.
2. **Tốn mana:** Q đào 1 khối → `Mana 95/100`; cầm khối, đặt → giảm 3; R Mưa lên đất → giảm 15.
3. **Hết mana:** đào liên tục tới khi thiếu → không đi, không đào, **thanh mana nháy** khoảng 0,6 giây, dòng `Không đủ mana: cần 5, còn …`.
4. **Hồi mana:** click trái đi một quãng → thanh mana tăng dần (2 mỗi ô).
5. **Rơi:** xây cột cao ≥ 6 khối (hoặc tìm vách), nhảy xuống → máu giảm (6 ô ≈ −20); rơi 4 ô → không mất.
6. **Ngập đầu:** đi xuống nước sâu → máu giảm ~5 mỗi giây; lên bờ → ngừng giảm, sau ~5 giây bắt đầu hồi 1 mỗi giây.
7. **Hồi sinh:** ở dưới nước tới khi máu về 0 (khoảng 20 giây) → về chỗ xuất phát, đầy máu + mana, túi còn nguyên, dòng `Bạn ngất đi và tỉnh lại ở chỗ xuất phát …`.
8. Cảm giác số: chi phí 5 / 3 / 15, hồi 2 mỗi ô, nháy 0,6 giây có ổn không — số tạm, chỉnh trong `design/data/sim_m1.json` → `player` (`manaDig`, `manaPlace`, `manaRain`, `manaRegenPerCell`, `manaFlashMs`, `fallSafeCells`, `fallDamagePerCell`, `drownDamagePerSecond`).

## Dừng và chuyển lại Opus khi
- Brief mơ hồ hoặc mâu thuẫn với code thật (vd `ConfigLoader.LoadSimConfig()` không trả `SimConfig`, `PlayerVitals` khác chữ ký ở "Bối cảnh").
- Cần một quyết định thuộc AGENTS.md §3.4 hoặc ngoài "Quyết định đã chốt".
- Cùng một lỗi sửa 2 lần vẫn chưa qua.
- Phải sửa file ngoài "Phạm vi" (gồm `PlayerVitals.cs`).
- Test cũ hỏng mà không thuộc quy tắc bước 9.

---
<!-- Từ đây trở xuống: EXECUTOR ghi. -->

## Kết quả
<!-- Base commit · Chạy bằng: <model id> · <effort> · commit · file đã đổi · thời gian bắt đầu/kết thúc mọi vòng (giờ thực tế M1) · lệch so với brief. -->
- Base commit: a298d56
- Chạy bằng: claude-sonnet-5-5 · medium
- Bắt đầu: 2026-10-24 02:58 · kết thúc: 03:11 (một vòng, ≈ 0,2 giờ; phần lớn là đợi biên dịch + test)
- Commit: 08c9493 (`brief 0060: 7l c3 wire HP/mana into client ...`)
- File đã đổi: Core/PlayerRules.cs, Core/PlayerMotion.cs, Core/Sim/SimConfig.cs; design/data/sim_m1.json + Resources/Config/sim_m1.json; Client/PlayerCharacter.cs, TerrainEditInput.cs, PlayerFarmActions.cs; Lang/vi.json, loc/en.json, loc/context.json; Tests: PlayerRulesTests, SimConfigTests, PlayerMotionTests, ControlModePlayTests, PlayerCharacterPlayTests.
- Lệch so với brief: không. Không có test cũ hỏng → không áp dụng quy tắc bước 9. `PlayerVitals.cs` / `PlayerVitalsTests.cs` không đổi. Đã chạy `git checkout -- client/ProjectSettings/EditorSettings.asset` sau PlayMode.


## Kết quả test
- Lệnh 1: 2 bản sim_m1.json `diff` rỗng; parse JSON (5 file) OK; `loc_check.py check loc/en.json` exit 0 (177 chuỗi nguồn, 177 dịch); `grep UnityEngine Core` rỗng; `check_design.py` exit 0.
- Lệnh 2 (MCP): biên dịch 0 lỗi console; EditMode **530/530 qua**; PlayMode **230/230 qua** (0 skipped, nên 4 test nước/rơi không Inconclusive); console 0 lỗi sau chạy.
- Lệnh 4 `verify_run --all`: client_test / client_test_play exit 1 vì Editor đang mở → **Chưa kiểm được** bằng dòng lệnh; kết quả MCP ở trên thay thế. Các lệnh khác trong `--all` không báo lỗi trong phần đuôi output đã xem.
- Phần cần test: tất cả mục dùng test (đầu ngập, tham số nháy, đào/đặt/Mưa tốn mana, thiếu mana chặn + nháy, hồi mana khi đi, rơi cao, rơi xuống nước, ngập đầu, hồi sinh, PlayerVitalsTests, regression điều khiển + di chuyển, toàn bộ EditMode) → **đạt** (nằm trong 530 + 230 test qua; test mới theo đúng tên ở bước 8).
- Thanh HUD hiển thị đúng chỗ, cảm giác số → **chờ user** (mục "User tự test" 1–8).


## Vướng mắc

---
<!-- PLANNER ghi khi review. -->

## Review
Planner (Opus 5.5), 2026-10-24, mức đầy đủ + user xem.
- `Chạy bằng` claude-sonnet-5-5 · medium khớp `Model thực thi`.
- Phần cần test: mọi dòng có kết quả (EditMode 530/530, PlayMode 230/230 qua MCP, 0 skipped → 3 test nước không Inconclusive; diff JSON / loc_check / check_design / grep Core đạt); thanh HUD + cảm giác số → User tự test.
- Diff `08c9493` (16 file, đúng Phạm vi; `PlayerVitals.cs` / `PlayerVitalsTests.cs` không đổi): `Vitals` tạo từ `LoadSimConfig().player` (QĐ 2); kiểm mana ở `RightClickAt` + lúc làm, chỉ trừ khi Ok / Mưa có hiệu ứng (QĐ 3); `AddMoved` sau `Controller.Move`, bỏ qua FlyMode (QĐ 4); rơi đo từ đỉnh, đặt lại trong nước / FlyMode / Place (QĐ 5); `HeadUnderwater` chân + 1,5 qua `FlyCameraCollision.IsUnderwater` (QĐ 6); bộ đếm ms giữ phần lẻ (QĐ 7); hồi sinh ở chỗ Place đầu, `TerrainEditInput` huỷ đi tới + dòng báo (QĐ 8); `manaFlashMs` 600 + kiểm biên (QĐ 9); thanh HUD đúng toạ độ / màu, chữ dời xuống 44 (QĐ 10). 12 test bước 8 có đủ tên.
- Giờ thực tế c3: 02:58–03:11 (≈ 0,2 giờ Executor, một vòng).
- Chờ user test (User tự test mục 1–8) trước khi kết luận.
