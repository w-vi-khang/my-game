# Brief 0006 — Simulation bước 6: hiển thị (nội suy vị trí, trạng thái)

> Trạng thái: chờ review
> Review: đầy đủ + user xem
> Model thực thi: Sonnet · medium
> Cập nhật: 2026-10-16 22:20

<!-- Planner (Opus) viết, Executor (Sonnet) làm theo ở PHIÊN KHÁC — executor không có lịch sử chat, không được tự quyết.
Trạng thái: chờ làm → đang làm → chờ review → đã duyệt; hoặc → cần Opus (executor dừng, hỏi ở mục Vướng mắc).
Review: nhẹ (đọc mục Kết quả) | đầy đủ (đọc cả diff) | user xem (có phần user tự kiểm bằng mắt/chơi thử) — có thể ghép "đầy đủ + user xem".
Model thực thi: BẮT BUỘC, đúng dạng `<Model> · <effort>` — Model: Opus | Sonnet | Haiku | Fable; effort: low | medium | high | xhigh | max (chọn theo AGENTS.md §10.2).
Executor kiểm tra model + effort của phiên TRƯỚC khi làm (scripts/brief_check.py); lệch hoặc không xác định được → cảnh báo user và DỪNG.
File được commit. Tự kiểm trước khi giao (AGENTS.md §10.3): executor có phải hỏi gì không? còn chỗ nào bỏ ngỏ 2 phương án? -->

## Mục tiêu
Lần đầu chạy mô phỏng M1 (`Game.Core.Sim`) trong client Unity, trên map M1 thật (scene mới `SimView`):
- sinh vật là khối hộp, đi **mượt ở 30 và 60 FPS** nhờ nội suy giữa hai nhịp;
- nhãn trạng thái trên đầu; dấu đình trệ tại ô.

Task contract: `.ai/tasks/m1-sim-buoc-6-hien-thi.md`.

## Bối cảnh cần đọc
Code ở commit `a263c89`.
- `client/Assets/Game/Core/Sim/SimData.cs`:
  - `:6` `CellPos` (`X, Y, Z`, `new CellPos(x, y, z)`);
  - `:28` `enum CreatureState { Idle, Moving, Working }`; `:33` `enum StallReason { None, NoPath, NoSeeds, StorageFull, NoAbility }`; `:35` `enum SimEventKind { StallStarted = 1, StallCleared = 2, CreatureRespawned = 3 }`;
  - `:38-55` `SimEvent`, constructor `SimEvent(long tick, SimEventKind kind, int stageId, int creatureId, CellPos cell, StallReason reason)`. Đình trệ: `CreatureId = -1`, `Cell` = `Target` của công đoạn. Hiện lại: `StageId = -1`.
  - `:65-106` `Creature`: constructor `Creature(int id, string speciesId, WorkAbility ability1, WorkAbility ability2, int mastery1, int mastery2, int speedPerTick, int sizeX, int sizeZ, int sizeY, CellPos cell)`. Trường dùng ở brief này: `Id`, `SpeciesId`, `SizeX/SizeZ/SizeY`, `Path` (`:73`, **không** chứa ô đang đứng; `Path[PathIndex]` = ô kế tiếp), `PathIndex`, `Cell` (`:76`), `State`, `StageId`, `MoveProgress` (`:83`, phần nghìn ô đã đi về phía `Path[PathIndex]`).
  - `:109` `WorkStage`, constructor `WorkStage(int id, int orderId, WorkAbility ability, CellPos target, bool hasDestination, CellPos destination, int baseTicks, int priority, …mặc định)`; `:135` `public int ProgressPermille => (int)(WorkDone * 1000 / WorkRequired);` (`WorkRequired = BaseTicks × 1000`).
  - `:190` `FarmPlot` (`Id`, `Soil`, `Queue`, `Phase`); `:210` `House` (`Cell`, `SizeX/Z/Y`); `:171` `Storage` (`Cell`, `Seeds`, `Produce`).
  - `:232` `SimState`, constructor `SimState(SimRng rng)`; `:243` `Events` ("nơi đọc tự Clear").
- `client/Assets/Game/Core/Sim/SimRng.cs:11` `public SimRng(ulong initState, ulong initSeq)`.
- `client/Assets/Game/Core/Sim/SimConfig.cs:6` `enum WorkAbility { Till, Sow, Water, Harvest, Haul }`; `:115` `public int ticksPerSecond = 10;`.
- `client/Assets/Game/Core/Sim/Simulation.cs`:
  - `:19` `public Simulation(SimConfig config, WorldData world)`;
  - `:29` `AddCreature(string speciesId, CellPos cell)`; `:72` `AddStorage(CellPos cell)`; `:86` `public bool IsOccupied(int x, int y, int z)`; `:101` `AddHouse(CellPos cell)`; `:121` `AssignHome(int creatureId, int houseId)`; `:495` `AddFarmOrder(IReadOnlyList<CellPos> soilCells, int storageId)`; `:527` `public bool EnqueueCrop(int plotId)` (ruộng `Idle` → bắt đầu vụ ngay);
  - `:243` `public void OnCellChanged(int x, int y, int z)`; `:245` `public void Tick()`;
  - `:551-570` `MovePhase`: mỗi nhịp `MoveProgress += SpeedPerTick`; đủ 1000 → `Cell = Path[PathIndex++]`, trừ 1000; hết đường → `MoveProgress = 0`, tới nơi;
  - `:198` `Respawn` (đặt thẳng `Cell`, sự kiện `CreatureRespawned`); `:226-240` `ReportPhase` (sự kiện đình trệ theo từng công đoạn: đổi lý do → thêm một `StallStarted` mới, không có `StallCleared` ở giữa);
  - `:452-469` thu hoạch xong → `Queue.RemoveAt(0)`; hàng đợi rỗng → `Phase = Idle`.
- `client/Assets/Game/Core/WorldEditor.cs:50` `public Func<int, int, int, bool> IsOccupied;`; `:53` `public event Action<int, int, int> CellChanged;`.
- `client/Assets/Game/Client/TerrainChunkView.cs`:
  - `:44-46` `World`, `Inventory`, `Editor` (có sau `Build()`); `:55-58` `Start()` gọi `Build()`; `:73` `Editor.CellChanged += OnCellChanged;`;
  - toạ độ: ô (x, y, z) chiếm khối `[x, x+1) × [y, y+1) × [z, z+1)` trong không gian local của transform này (`:104-108` `CellAt`); mặt trên của ô đặc y − 1 nằm ở độ cao y.
- `client/Assets/Game/Client/TerrainEditInput.cs`:
  - `:106-122` `OnGUI`: mẫu phóng chữ `scale = Mathf.Max(1f, Screen.height / 720f)`, `GUI.matrix = Matrix4x4.Scale(...)`, nhãn ở `Rect(10, 10, 1000, 90)`;
  - `:136-139` mẫu tạo material URP: `new Material(GraphicsSettings.currentRenderPipeline.defaultMaterial)` + `SetColor("_BaseColor", …)`;
  - `:36-43` mẫu đọc phím `Keyboard.current.xxxKey.wasPressedThisFrame`.
- `client/Assets/Game/Client/ConfigLoader.cs` (cả file, 24 dòng): mẫu `LoadTerrainParams()`.
- `client/Assets/Game/Tests/World/ConfigTests.cs:10-21`: mẫu `ClientCopyMatchesDesignData` (đường dẫn qua `Application.dataPath`).
- `client/Assets/Game/Tests/Core/Sim/TestWorlds.cs`:
  - `Flat(0)`: 16 × 16 × 16, sinh vật đứng ở y = 1;
  - `HomeAll(sim)`: nhà ở (14, ·, 14), gọi sau `AddCreature` cuối, trước `Tick()` đầu.
- `client/Assets/Game/Tests/Client/TerrainEditPlayTests.cs:12-31`: mẫu PlayMode (tạo `GameObject` + `AddComponent<TerrainChunkView>()`, `yield return null`, `Object.Destroy`).
- Scene `client/Assets/Game/Client/Scenes/TerrainView.unity`: GameObject `Main Camera` (có `DebugFlyCamera`), `Terrain` (có `TerrainChunkView` + `TerrainEditInput`, transform gốc 0), `Directional Light`, `Ocean`.
- **Quy ước thời gian** (đã kiểm bằng test bước 1–5): "nhịp n" = trạng thái sau lần `Tick()` thứ n. Lệnh thêm trước `Tick()` đầu → giao việc ở nhịp 1. Đường n ô, tốc độ v (L1: 150 phần nghìn ô/nhịp) → tới nơi ở nhịp 1 + ⌈1000·n / v⌉.
- **Bản đồ độ cao map M1** (Planner chạy thật 2026-10-16 [run]): mặt đất y = 17 (đứng ở y = 18) tại x 30–37 × z 16–17, x 30–34 × z 18, x 30–33 × z 19–20. Các ô kề kho (34, 18, 17) đều có mặt đất y = 17.

API Unity (đã tra docs 6000.4 / Input System 1.19):
- `QualitySettings.vSyncCount = 0` rồi mới đặt `Application.targetFrameRate`: "If vSyncCount != 0, then targetFrameRate is ignored". Trong Editor targetFrameRate "affects only the Game view"; mặc định −1. [docs](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/Application-targetFrameRate.html), [docs](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/QualitySettings-vSyncCount.html)
- `Time.deltaTime` (theo `timeScale`, chặn bởi `Time.maximumDeltaTime`), `Time.unscaledDeltaTime`. [docs](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/Time-deltaTime.html), [docs](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/Time-unscaledDeltaTime.html)
- `public Vector3 WorldToScreenPoint(Vector3 position)`: z = khoảng cách tới camera; gốc màn hình ở **dưới trái**. GUI gốc ở trên trái → `y_gui = Screen.height − y`. [docs](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/Camera.WorldToScreenPoint.html)
- `public static GameObject CreatePrimitive(PrimitiveType type)`: có sẵn collider → phải xoá để không chặn raycast đào/đặt. [docs](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/GameObject.CreatePrimitive.html)
- `public static Quaternion LookRotation(Vector3 forward, Vector3 upwards = Vector3.up)`: forward = 0 → log lỗi. Hướng trong brief này không bao giờ = 0. [docs](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/Quaternion.LookRotation.html)
- `Keyboard.f1Key`, `Keyboard.f2Key` (`KeyControl`). [docs](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.19/api/UnityEngine.InputSystem.Keyboard.html)

## Quyết định đã chốt (không được đổi)
- (user) Scene mới `SimView` = bản chép của `TerrainView` + component `SimView` trên `Terrain`. Đào/đặt nối vào mô phỏng: `Editor.IsOccupied = Sim.IsOccupied`, `Editor.CellChanged += Sim.OnCellChanged`.
- (user) Sinh vật = hộp theo khung, màu theo loài, mũi chỉ hướng. Không làm HUD kho/ruộng.
- (user) Trạng thái = nhãn Idle/Moving/Working + tiến độ, dấu "!" đỏ + lý do tại ô đình trệ. Chữ **nguyên văn** ở bước 3.
- (user) Nội suy giữa 2 nhịp (trễ 1 nhịp). Test EditMode giả lập khung. F1 / F2 = 30 / 60 FPS.
- (Planner) Lớp hiển thị C# thuần nằm trong `Game.Core`, thư mục `client/Assets/Game/Core/View/`, namespace `Game.Core.View`.
  - Được dùng `double` / `Dictionary` vì không thuộc trạng thái mô phỏng.
  - `Core/Sim` vẫn giữ luật grep cấm. Trong `Core/Sim` chỉ thêm `SimDemo.cs` (chỉ số nguyên).
- (Planner) `SimClock` cộng thời gian theo **micro giây số nguyên**: `(long)Math.Round(dt × 1e6)`. Tối đa 5 nhịp mỗi khung; vượt → chạy 5 nhịp, bộ cộng về 0.
- (Planner) Dời > 1000 phần nghìn ô theo một trục trong một nhịp, hoặc có sự kiện `CreatureRespawned` → đặt thẳng, không nội suy.
- (Planner) Bố trí demo cố định (toạ độ ở bước 4). **Không đổi toạ độ và không sửa số trong test cho khớp code.** Test sai → dừng, ghi Vướng mắc.
- Không sửa mô phỏng (`Simulation.cs`, `SimData.cs`, `SimConfig.cs`, …) và không sửa test cũ (trừ thêm 1 test vào `ConfigTests.cs`).

## Phạm vi
- Được tạo (+ `.meta` do Unity sinh):
  - `client/Assets/Game/Core/View/SimClock.cs`, `SimInterpolator.cs`, `StallMarkers.cs`, `CreatureStatus.cs`;
  - `client/Assets/Game/Core/Sim/SimDemo.cs`;
  - `client/Assets/Game/Client/SimView.cs`;
  - `client/Assets/Game/Client/Resources/Config/sim_m1.json` (bản chép);
  - `client/Assets/Game/Client/Scenes/SimView.unity`;
  - `client/Assets/Game/Tests/Core/View/SimClockTests.cs`, `SimInterpolatorTests.cs`, `SmoothMotionTests.cs`, `StallMarkersTests.cs`, `CreatureStatusTests.cs`;
  - `client/Assets/Game/Tests/World/SimDemoTests.cs`;
  - `client/Assets/Game/Tests/Client/SimViewPlayTests.cs`.
- Được sửa:
  - `client/Assets/Game/Client/ConfigLoader.cs` (thêm hàm);
  - `client/Assets/Game/Tests/World/ConfigTests.cs` (thêm 1 test);
  - brief này (Trạng thái, Kết quả, Kết quả test, Vướng mắc).
- Không được sửa: mọi file khác, nhất là `Core/Sim/*` (trừ tạo `SimDemo.cs`), `TerrainChunkView.cs`, `TerrainEditInput.cs`, `TerrainView.unity`, asmdef, `ProjectSettings/*`, `design/`, `.ai/tasks/`.

## Các bước
1. **`SimClock`** — `client/Assets/Game/Core/View/SimClock.cs`:
   ```csharp
   using System;

   namespace Game.Core.View
   {
       /// <summary>
       /// Đếm nhịp cho phần hiển thị (sim bước 6): cộng thời gian khung hình theo micro giây số nguyên → số nhịp phải chạy trong khung;
       /// Alpha = phần nhịp đã trôi (0..1), dùng để nội suy. Không thuộc trạng thái mô phỏng (chỉ quyết định khi nào gọi Tick).
       /// </summary>
       public sealed class SimClock
       {
           public readonly long TickMicros;
           public readonly int MaxTicksPerFrame;
           long accumulator;

           public SimClock(int ticksPerSecond, int maxTicksPerFrame = 5)
           {
               if (ticksPerSecond <= 0 || 1000000 % ticksPerSecond != 0)
                   throw new ArgumentException("ticksPerSecond phải dương và chia hết 1000000: " + ticksPerSecond);
               if (maxTicksPerFrame < 1)
                   throw new ArgumentException("maxTicksPerFrame phải >= 1: " + maxTicksPerFrame);
               TickMicros = 1000000 / ticksPerSecond;
               MaxTicksPerFrame = maxTicksPerFrame;
           }

           public long AccumulatorMicros => accumulator;
           public double Alpha => (double)accumulator / TickMicros;

           /// <summary>Cộng thời gian một khung; trả số nhịp phải chạy. Quá MaxTicksPerFrame → chạy MaxTicksPerFrame, bỏ phần dư (không đuổi theo).</summary>
           public int Advance(double deltaSeconds)
           {
               if (!(deltaSeconds > 0)) return 0;
               if (deltaSeconds > 10) deltaSeconds = 10;
               accumulator += (long)Math.Round(deltaSeconds * 1000000.0);
               long n = accumulator / TickMicros;
               if (n > MaxTicksPerFrame)
               {
                   accumulator = 0;
                   return MaxTicksPerFrame;
               }
               accumulator -= n * TickMicros;
               return (int)n;
           }
       }
   }
   ```
   Xong khi: biên dịch được, test bước 7.1 đạt.

2. **`SimInterpolator`** — `client/Assets/Game/Core/View/SimInterpolator.cs`:
   ```csharp
   using System;
   using System.Collections.Generic;
   using Game.Core.Sim;

   namespace Game.Core.View
   {
       /// <summary>
       /// Nội suy vị trí sinh vật giữa 2 nhịp (sim bước 6): mỗi nhịp lưu vị trí liên tục (ô + MoveProgress, phần nghìn ô);
       /// hiển thị = nhịp trước → nhịp sau theo alpha, nên trễ 1 nhịp. Dời quá 1 ô theo một trục, hoặc sự kiện hiện lại → đặt thẳng.
       /// Chỉ số = id sinh vật (= vị trí trong State.Creatures).
       /// </summary>
       public sealed class SimInterpolator
       {
           public const int SnapMilli = 1000;

           struct Track
           {
               public int PX, PY, PZ, CX, CY, CZ, HX, HZ;
           }

           readonly List<Track> tracks = new List<Track>();

           public int Count => tracks.Count;

           /// <summary>Vị trí liên tục (phần nghìn ô) của góc nhỏ nhất của khung.</summary>
           public static void PoseMilli(Creature c, out int x, out int y, out int z)
           {
               x = c.Cell.X * 1000;
               y = c.Cell.Y * 1000;
               z = c.Cell.Z * 1000;
               if (c.State == CreatureState.Moving && c.PathIndex < c.Path.Count)
               {
                   var n = c.Path[c.PathIndex];
                   x += (n.X - c.Cell.X) * c.MoveProgress;
                   y += (n.Y - c.Cell.Y) * c.MoveProgress;
                   z += (n.Z - c.Cell.Z) * c.MoveProgress;
               }
           }

           /// <summary>Gọi một lần sau khi dựng xong (trước Tick đầu), và sau mỗi Tick().</summary>
           public void Capture(SimState s)
           {
               var cs = s.Creatures;
               for (int i = 0; i < cs.Count; i++)
               {
                   PoseMilli(cs[i], out int x, out int y, out int z);
                   if (i >= tracks.Count)
                   {
                       tracks.Add(new Track { PX = x, PY = y, PZ = z, CX = x, CY = y, CZ = z, HX = 0, HZ = 1 });
                       continue;
                   }
                   var t = tracks[i];
                   t.PX = t.CX; t.PY = t.CY; t.PZ = t.CZ;
                   t.CX = x; t.CY = y; t.CZ = z;
                   if (Math.Abs(t.CX - t.PX) > SnapMilli || Math.Abs(t.CY - t.PY) > SnapMilli || Math.Abs(t.CZ - t.PZ) > SnapMilli)
                   {
                       t.PX = t.CX; t.PY = t.CY; t.PZ = t.CZ;
                   }
                   else if (t.CX != t.PX || t.CZ != t.PZ)
                   {
                       t.HX = t.CX - t.PX;
                       t.HZ = t.CZ - t.PZ;
                   }
                   tracks[i] = t;
               }
           }

           /// <summary>Gọi sau Capture của cùng nhịp, trước khi Clear sự kiện: sinh vật vừa hiện lại → đặt thẳng.</summary>
           public void ApplyEvents(IReadOnlyList<SimEvent> events)
           {
               for (int i = 0; i < events.Count; i++)
               {
                   var e = events[i];
                   if (e.Kind != SimEventKind.CreatureRespawned || e.CreatureId < 0 || e.CreatureId >= tracks.Count) continue;
                   var t = tracks[e.CreatureId];
                   t.PX = t.CX; t.PY = t.CY; t.PZ = t.CZ;
                   tracks[e.CreatureId] = t;
               }
           }

           /// <summary>Vị trí hiển thị (đơn vị ô) của góc nhỏ nhất của khung; alpha bị kẹp vào [0, 1].</summary>
           public void Sample(int creatureId, double alpha, out double x, out double y, out double z)
           {
               if (alpha < 0) alpha = 0;
               else if (alpha > 1) alpha = 1;
               var t = tracks[creatureId];
               x = (t.PX + (t.CX - t.PX) * alpha) / 1000.0;
               y = (t.PY + (t.CY - t.PY) * alpha) / 1000.0;
               z = (t.PZ + (t.CZ - t.PZ) * alpha) / 1000.0;
           }

           /// <summary>Hướng đi ngang gần nhất (không bao giờ là 0, 0); ban đầu (0, 1) = +Z.</summary>
           public void Heading(int creatureId, out int hx, out int hz)
           {
               var t = tracks[creatureId];
               hx = t.HX;
               hz = t.HZ;
           }
       }
   }
   ```
   Xong khi: test bước 7.2 và 7.3 đạt.

3. **`StallMarkers` + `CreatureStatus`** — `client/Assets/Game/Core/View/`:
   - `StallMarkers.cs`:
     ```csharp
     using System.Collections.Generic;
     using Game.Core.Sim;

     namespace Game.Core.View
     {
         /// <summary>Dấu đình trệ đang hiện (sim bước 6): công đoạn → sự kiện StallStarted gần nhất; StallCleared → bỏ. Sự kiện khác bỏ qua.</summary>
         public sealed class StallMarkers
         {
             readonly Dictionary<int, SimEvent> active = new Dictionary<int, SimEvent>();

             public int Count => active.Count;
             public IEnumerable<SimEvent> Active => active.Values;
             public bool TryGet(int stageId, out SimEvent e) => active.TryGetValue(stageId, out e);

             public void ApplyEvents(IReadOnlyList<SimEvent> events)
             {
                 for (int i = 0; i < events.Count; i++)
                 {
                     var e = events[i];
                     if (e.Kind == SimEventKind.StallStarted) active[e.StageId] = e;
                     else if (e.Kind == SimEventKind.StallCleared) active.Remove(e.StageId);
                 }
             }
         }
     }
     ```
   - `CreatureStatus.cs` — chữ người chơi đọc, **chép nguyên văn**:
     ```csharp
     using Game.Core.Sim;

     namespace Game.Core.View
     {
         /// <summary>Chữ trạng thái trên đầu sinh vật và lý do đình trệ (sim bước 6, tạm cho prototype).</summary>
         public static class CreatureStatus
         {
             public static string AbilityName(WorkAbility a) => a switch
             {
                 WorkAbility.Till => "xới",
                 WorkAbility.Sow => "gieo",
                 WorkAbility.Water => "tưới",
                 WorkAbility.Harvest => "thu hoạch",
                 WorkAbility.Haul => "vận chuyển",
                 _ => a.ToString(),
             };

             public static string ReasonText(StallReason r) => r switch
             {
                 StallReason.NoPath => "không có đường",
                 StallReason.NoSeeds => "hết hạt",
                 StallReason.StorageFull => "kho đầy",
                 StallReason.NoAbility => "thiếu năng lực",
                 _ => r.ToString(),
             };

             /// <summary>"L1 · rảnh" | "L1 · đi" | "L1 · đi → xới" | "L1 · xới 45%" (phần trăm làm tròn xuống).</summary>
             public static string Text(SimState s, Creature c)
             {
                 string head = c.SpeciesId + " · ";
                 if (c.State == CreatureState.Working && c.StageId >= 0)
                 {
                     var st = s.Stages[c.StageId];
                     return head + AbilityName(st.Ability) + " " + (st.ProgressPermille / 10) + "%";
                 }
                 if (c.State == CreatureState.Moving)
                     return c.StageId >= 0 ? head + "đi → " + AbilityName(s.Stages[c.StageId].Ability) : head + "đi";
                 return head + "rảnh";
             }
         }
     }
     ```
   Xong khi: test bước 7.4 và 7.5 đạt.

4. **`SimDemo`** — `client/Assets/Game/Core/Sim/SimDemo.cs` (chỉ số nguyên, không dùng API cấm):
   ```csharp
   namespace Game.Core.Sim
   {
       /// <summary>
       /// Bố trí demo cho scene SimView (sim bước 6) trên map M1 (world_m1.json, seed 20261013): mặt đất y = 17 quanh x 30–36, z 16–20
       /// (bản đồ độ cao chạy thật 2026-10-16). 2 nhà, 1 kho, 4 ô ruộng (một lệnh trồng cây); L1 + L2 ở nhà A, L3 ở nhà B.
       /// </summary>
       public static class SimDemo
       {
           public static readonly CellPos HouseA = new CellPos(30, 18, 19);
           public static readonly CellPos HouseB = new CellPos(32, 18, 19);
           public static readonly CellPos StorageCell = new CellPos(34, 18, 17);
           public static readonly CellPos[] Soil =
               { new CellPos(30, 17, 16), new CellPos(31, 17, 16), new CellPos(30, 17, 17), new CellPos(31, 17, 17) };
           public static readonly string[] Species = { "L1", "L2", "L3" };
           public static readonly CellPos[] CreatureCells =
               { new CellPos(32, 18, 16), new CellPos(33, 18, 16), new CellPos(36, 18, 16) };

           /// <summary>Gọi một lần trên mô phỏng mới (chưa Tick). Trả id kho.</summary>
           public static int Build(Simulation sim)
           {
               int a = sim.AddHouse(HouseA);
               int b = sim.AddHouse(HouseB);
               int storage = sim.AddStorage(StorageCell);
               for (int i = 0; i < Species.Length; i++)
                   sim.AddCreature(Species[i], CreatureCells[i]);
               sim.AssignHome(0, a);
               sim.AssignHome(1, a);
               sim.AssignHome(2, b);
               sim.AddFarmOrder(Soil, storage);
               return storage;
           }

           /// <summary>Ô ruộng nào hết vụ trong hàng đợi → thêm một vụ (trang trại demo chạy mãi). Gọi trước mỗi Tick().</summary>
           public static void KeepFarming(Simulation sim)
           {
               var ps = sim.State.Plots;
               for (int i = 0; i < ps.Count; i++)
                   if (ps[i].Queue.Count == 0) sim.EnqueueCrop(ps[i].Id);
           }
       }
   }
   ```
   Xong khi: test bước 7.6 đạt.

5. **Config cho client**:
   - `cp design/data/sim_m1.json client/Assets/Game/Client/Resources/Config/sim_m1.json`.
   - `ConfigLoader.cs`: thêm `using Game.Core.Sim;`, hằng `public const string SimM1Path = "Config/sim_m1";` và hàm sau (cùng mẫu `LoadTerrainParams`):
     ```csharp
     public static SimConfig LoadSimConfig()
     {
         var asset = Resources.Load<TextAsset>(SimM1Path);
         if (asset == null)
             throw new System.IO.FileNotFoundException($"Không thấy Resources/{SimM1Path}.json");
         var c = JsonUtility.FromJson<SimConfig>(asset.text);
         c.Validate();
         return c;
     }
     ```
     Sửa comment `:6-9` của class: "Đọc cấu hình JSON (world_m1, sim_m1)…" và "test Game.World.Tests.ConfigTests bảo đảm các bản giống nhau".
   - `ConfigTests.cs`: thêm test `SimClientCopyMatchesDesignData`, giống `ClientCopyMatchesDesignData` nhưng với `sim_m1.json`. Thông báo khi lỗi: `"Bản trong client khác design/data/sim_m1.json — chép lại từ design/data"`.
   Xong khi: `cmp` hai file exit 0; test mới đạt.

6. **`SimView`** — `client/Assets/Game/Client/SimView.cs`. Chép đúng code dưới, chỉ sửa khi không biên dịch được, và ghi lại chỗ sửa trong Kết quả:
   ```csharp
   using System.Collections.Generic;
   using Game.Core.Sim;
   using Game.Core.View;
   using UnityEngine;
   using UnityEngine.InputSystem;
   using UnityEngine.Rendering;

   namespace Game.Client
   {
       /// <summary>
       /// Scene SimView (sim bước 6, user chọn 2026-10-16): mô phỏng M1 chạy trên map thật với bố trí SimDemo (2 nhà, 1 kho, 4 ô ruộng, L1–L3).
       /// Sinh vật = khối hộp theo khung chiếm chỗ, màu theo loài, mũi chỉ hướng đi; vị trí nội suy giữa 2 nhịp (trễ 1 nhịp).
       /// Nhãn trên đầu = trạng thái; "!" đỏ = đình trệ tại ô. F1 / F2: khoá 30 / 60 FPS (chỉ Game view trong Editor).
       /// Đào/đặt (TerrainEditInput) báo cho mô phỏng qua WorldEditor.CellChanged; ô có sinh vật/nhà/kho không đặt được.
       /// </summary>
       [RequireComponent(typeof(TerrainChunkView))]
       public class SimView : MonoBehaviour
       {
           public int maxTicksPerFrame = 5;

           static readonly Color HouseColor = new Color(0.55f, 0.35f, 0.20f);
           static readonly Color StorageColor = new Color(0.60f, 0.60f, 0.60f);
           static readonly Color PlotColor = new Color(0.30f, 0.18f, 0.10f);
           static readonly Color NoseColor = new Color(0.15f, 0.15f, 0.15f);

           static Color SpeciesColor(string id) => id switch
           {
               "L1" => new Color(0.95f, 0.55f, 0.20f),
               "L2" => new Color(0.25f, 0.60f, 0.95f),
               "L3" => new Color(0.65f, 0.40f, 0.90f),
               _ => Color.white,
           };

           TerrainChunkView view;
           Transform root;
           SimClock clock;
           SimInterpolator interp;
           StallMarkers markers;
           readonly List<Transform> bodies = new List<Transform>();
           readonly List<Transform> pivots = new List<Transform>();
           int savedVSync, savedTargetFps;
           float smoothedDt = 1f / 60f;
           GUIStyle labelStyle, hudStyle;

           public Simulation Sim { get; private set; }
           public int TargetFps { get; private set; }
           public int BodyCount => bodies.Count;

           void Awake()
           {
               view = GetComponent<TerrainChunkView>();
               savedVSync = QualitySettings.vSyncCount;
               savedTargetFps = Application.targetFrameRate;
           }

           void OnDestroy()
           {
               QualitySettings.vSyncCount = savedVSync;
               Application.targetFrameRate = savedTargetFps;
           }

           void Update()
           {
               if (Sim == null)
               {
                   // TerrainChunkView dựng world trong Start — Update đầu tiên chạy sau mọi Start.
                   if (view.World == null || view.Editor == null) return;
                   Setup();
               }

               var kb = Keyboard.current;
               if (kb != null)
               {
                   if (kb.f1Key.wasPressedThisFrame) SetFrameRate(30);
                   if (kb.f2Key.wasPressedThisFrame) SetFrameRate(60);
               }
               smoothedDt = smoothedDt * 0.9f + Time.unscaledDeltaTime * 0.1f;

               int n = clock.Advance(Time.deltaTime);
               var events = Sim.State.Events;
               for (int i = 0; i < n; i++)
               {
                   SimDemo.KeepFarming(Sim);
                   Sim.Tick();
                   interp.Capture(Sim.State);
                   interp.ApplyEvents(events);
                   markers.ApplyEvents(events);
                   events.Clear();
               }
               UpdateBodies();
           }

           void Setup()
           {
               var cfg = ConfigLoader.LoadSimConfig();
               Sim = new Simulation(cfg, view.World);
               view.Editor.IsOccupied = Sim.IsOccupied;
               view.Editor.CellChanged += Sim.OnCellChanged;
               SimDemo.Build(Sim);

               clock = new SimClock(cfg.ticksPerSecond, maxTicksPerFrame);
               interp = new SimInterpolator();
               interp.Capture(Sim.State);
               markers = new StallMarkers();

               root = new GameObject("Sim").transform;
               root.SetParent(transform, false);
               foreach (var h in Sim.State.Houses)
                   Box("Nhà " + h.Id, new Vector3(h.Cell.X + h.SizeX * 0.5f, h.Cell.Y + h.SizeY * 0.5f, h.Cell.Z + h.SizeZ * 0.5f),
                       new Vector3(h.SizeX * 0.98f, h.SizeY * 0.98f, h.SizeZ * 0.98f), HouseColor, root);
               foreach (var s in Sim.State.Storages)
                   Box("Kho " + s.Id, new Vector3(s.Cell.X + 0.5f, s.Cell.Y + 0.45f, s.Cell.Z + 0.5f), new Vector3(0.9f, 0.9f, 0.9f), StorageColor, root);
               foreach (var p in Sim.State.Plots)
                   Box("Ruộng " + p.Id, new Vector3(p.Soil.X + 0.5f, p.Soil.Y + 1.02f, p.Soil.Z + 0.5f), new Vector3(0.9f, 0.04f, 0.9f), PlotColor, root);
               UpdateBodies();
               SetFrameRate(60);
           }

           void SetFrameRate(int fps)
           {
               TargetFps = fps;
               QualitySettings.vSyncCount = 0; // vSyncCount != 0 thì targetFrameRate bị bỏ qua
               Application.targetFrameRate = fps;
           }

           void UpdateBodies()
           {
               var cs = Sim.State.Creatures;
               while (bodies.Count < cs.Count)
                   CreateBody(cs[bodies.Count]);
               for (int i = 0; i < cs.Count; i++)
               {
                   var c = cs[i];
                   interp.Sample(i, clock.Alpha, out double x, out double y, out double z);
                   bodies[i].localPosition = new Vector3((float)x + c.SizeX * 0.5f, (float)y + c.SizeY * 0.4f, (float)z + c.SizeZ * 0.5f);
                   interp.Heading(i, out int hx, out int hz);
                   pivots[i].localRotation = Quaternion.LookRotation(new Vector3(hx, 0f, hz));
               }
           }

           void CreateBody(Creature c)
           {
               var body = new GameObject("Sinh vật " + c.Id + " " + c.SpeciesId).transform;
               body.SetParent(root, false);
               Box("Thân", Vector3.zero, new Vector3(c.SizeX * 0.8f, c.SizeY * 0.8f, c.SizeZ * 0.8f), SpeciesColor(c.SpeciesId), body);
               var pivot = new GameObject("Hướng").transform;
               pivot.SetParent(body, false);
               Box("Mũi", new Vector3(0f, c.SizeY * 0.15f, Mathf.Max(c.SizeX, c.SizeZ) * 0.4f + 0.1f), new Vector3(0.25f, 0.25f, 0.3f), NoseColor, pivot);
               bodies.Add(body);
               pivots.Add(pivot);
           }

           void Box(string name, Vector3 localPos, Vector3 scale, Color color, Transform parent)
           {
               var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
               go.name = name;
               Destroy(go.GetComponent<Collider>()); // không chặn raycast đào/đặt
               var t = go.transform;
               t.SetParent(parent, false);
               t.localPosition = localPos;
               t.localScale = scale;
               var mat = new UnityEngine.Material(GraphicsSettings.currentRenderPipeline.defaultMaterial);
               mat.SetColor("_BaseColor", color);
               go.GetComponent<MeshRenderer>().sharedMaterial = mat;
           }

           void OnGUI()
           {
               if (Sim == null)
                   return;
               float scale = Mathf.Max(1f, Screen.height / 720f);
               GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
               if (labelStyle == null)
               {
                   labelStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, richText = true };
                   hudStyle = new GUIStyle(GUI.skin.label) { richText = true };
               }
               GUI.Label(new Rect(10, 100, 1000, 24),
                   $"F1: 30 FPS · F2: 60 FPS — mục tiêu {TargetFps} · đo {1f / smoothedDt:F1} FPS · nhịp {Sim.State.Tick}", hudStyle);

               var cam = Camera.main;
               if (cam == null)
                   return;
               var cs = Sim.State.Creatures;
               for (int i = 0; i < cs.Count && i < bodies.Count; i++)
                   DrawLabel(cam, bodies[i].position + Vector3.up * (cs[i].SizeY * 0.4f + 0.35f), CreatureStatus.Text(Sim.State, cs[i]), scale);
               foreach (var e in markers.Active)
                   DrawLabel(cam, transform.TransformPoint(new Vector3(e.Cell.X + 0.5f, e.Cell.Y + 1.3f, e.Cell.Z + 0.5f)),
                       "<color=red><b>! " + CreatureStatus.ReasonText(e.Reason) + "</b></color>", scale);
           }

           void DrawLabel(Camera cam, Vector3 world, string text, float scale)
           {
               var sp = cam.WorldToScreenPoint(world);
               if (sp.z <= 0f)
                   return;
               // WorldToScreenPoint gốc dưới trái; GUI gốc trên trái.
               GUI.Label(new Rect(sp.x / scale - 100f, (Screen.height - sp.y) / scale - 11f, 200f, 22f), text, labelStyle);
           }
       }
   }
   ```
   Xong khi: MCP `refresh_unity` → `read_console` (Error) không có lỗi biên dịch.

7. **Test** (chép số đúng như dưới; test sai → dừng, không sửa số). Lớp test EditMode trong `client/Assets/Game/Tests/Core/View/`: `namespace Game.Core.Tests`, `using Game.Core.Sim; using Game.Core.View; using NUnit.Framework;`. Tạo `SimState` tay: `var st = new SimState(new SimRng(1UL, 1UL));`. Tạo sinh vật tay: `new Creature(0, "L1", WorkAbility.Till, WorkAbility.Sow, 100, 100, 150, 1, 1, 1, new CellPos(3, 1, 4))` (mặc định `Idle`), rồi `st.Creatures.Add(c)`. So `double` bằng `Assert.AreEqual(expected, actual, 1e-9)`.
   1. `SimClockTests.cs` (5 test):
      - `Fps60TicksEverySixthFrame`: `new SimClock(10)`, 60 lần `Advance(1.0 / 60)`. Khung (đếm từ 1) chia hết cho 6 trả 1, khung khác trả 0. Tổng 10 nhịp; sau khung 60, `AccumulatorMicros == 20`.
      - `Fps30TicksOnFrames4_7_10`: 31 lần `Advance(1.0 / 30)`. Khung 4, 7, 10, 13, 16, 19, 22, 25, 28, 31 trả 1, khung khác trả 0. Tổng sau khung 30 = 9, sau khung 31 = 10; `AccumulatorMicros == 33323`.
      - `LongFrameIsCapped`: `Advance(1.0)` → 5, `AccumulatorMicros == 0`, `Alpha == 0`. Tiếp `Advance(0.25)` → 2, `AccumulatorMicros == 50000`, `Alpha` = 0.5.
      - `NonPositiveOrNaNIgnored`: `Advance(0)`, `Advance(-1)`, `Advance(double.NaN)` đều → 0; `AccumulatorMicros == 0`.
      - `ConstructorRejectsBadValues`: `new SimClock(0)`, `new SimClock(3)`, `new SimClock(10, 0)` đều `Assert.Throws<System.ArgumentException>`.
   2. `SimInterpolatorTests.cs` (9 test), một sinh vật như trên:
      - `FirstCaptureHasNoMotion`: `Capture`; `Sample(0, 0.7)` = (3, 1, 4); `Count == 1`; `Heading` = (0, 1).
      - `MovingInterpolatesProgress`: `State = Moving`, `Path.Add(new CellPos(4, 1, 4))`, `PathIndex = 0`, `MoveProgress = 300`; `Capture`; `MoveProgress = 450`; `Capture`. `Sample(0, 0.5)` = (3.375, 1, 4); `Heading` = (150, 0).
      - `AlphaIsClamped`: dựng lại đúng như test trên (2 lần `Capture`, `MoveProgress` 300 rồi 450); `Sample(0, 1.5)`.x = 3.45; `Sample(0, -1)`.x = 3.3.
      - `PoseMilliClimb`: `Cell` (3, 1, 4), `State = Moving`, `Path` = [(4, 2, 4)], `MoveProgress = 500` → `PoseMilli` = (3500, 1500, 4000).
      - `PoseMilliIgnoresProgressWhenNotMoving`: như trên nhưng `State = Working` → (3000, 1000, 4000).
      - `JumpOverOneCellSnaps`: `Capture` ở (3, 1, 4); `Cell = (5, 1, 4)`; `Capture`. `Sample(0, 0.5)`.x = 5.0; `Heading` vẫn (0, 1).
      - `OneCellDropInterpolates`: `Cell` (3, 2, 4) → `Capture`; `Cell = (3, 1, 4)` → `Capture`. `Sample(0, 0.5)`.y = 1.5.
      - `RespawnEventSnaps`: `Capture` ở (3, 1, 4); `Cell = (4, 1, 4)`; `Capture`. `ApplyEvents(new[] { new SimEvent(5, SimEventKind.CreatureRespawned, -1, 0, new CellPos(4, 1, 4), StallReason.None) })` → `Sample(0, 0.5)`.x = 4.0.
      - `LateCreatureStartsWithoutMotion`: `Capture` với 1 sinh vật; thêm sinh vật id 1 ở (7, 1, 7); `Capture`. `Count == 2`; `Sample(1, 0.5)` = (7, 1, 7).
   3. `SmoothMotionTests.cs` (3 test; thêm `using System; using System.Collections.Generic;`). Đây là test "Xong khi" của bước 6. Hàm chung `RunStraightWalk(double[] frameTimes)`:
      - `var sim = new Simulation(new SimConfig(), TestWorlds.Flat(0));`
      - `sim.AddCreature("L1", new CellPos(1, 1, 1));`
      - `sim.AddStage(sim.AddOrder(new List<CellPos>()), WorkAbility.Till, new CellPos(13, 1, 1));`
      - `TestWorlds.HomeAll(sim);`
      - `clock = new SimClock(10)`, `interp = new SimInterpolator()`, `interp.Capture(sim.State)`; `px` = `Sample(0, clock.Alpha)`.x.
      - Lặp khung f = 0, 1, 2…, dừng khi tổng số nhịp đã chạy ≥ 100:
        - `dt = frameTimes[f % frameTimes.Length]`; `before` = số nhịp trước khung;
        - `n = clock.Advance(dt)`; mỗi nhịp: `sim.Tick(); interp.Capture(sim.State); interp.ApplyEvents(sim.State.Events); sim.State.Events.Clear();`;
        - `Sample(0, clock.Alpha, out x, out y, out z)`. Assert `y == 1.0` và `z == 1.0`.
        - Nếu `before >= 2` **và** số nhịp sau khung `<= 80`: assert `x − px` = `1.5e-6 × Math.Round(dt × 1e6)` (sai ≤ 1e-9, thông báo kèm số khung), assert `x − px > 0`, đếm `window++`.
        - `px = x`.
      - Sau vòng lặp: `window >= 200`; `px == 13.0`; sinh vật `Cell == (13, 1, 1)`, `State == Working`.
      - Lý do: giao việc ở nhịp 1, tới nơi ở nhịp 1 + ⌈12000 / 150⌉ = 81. Vị trí ở nhịp k (1 ≤ k ≤ 81) = 1 + 0,15 (k − 1). Hiển thị trễ 1 nhịp → x = 1 + 0,15 (k − 2 + alpha), tuyến tính theo thời gian. Mỗi khung dời đúng 0,15 ô × (micro giây của khung ÷ 100000).
      - Test:
        - `Smooth30Fps`: `{ 1.0 / 30 }`;
        - `Smooth60Fps`: `{ 1.0 / 60 }`;
        - `SmoothUnevenFrames`: `{ 1.0 / 30, 1.0 / 60, 1.0 / 45, 1.0 / 90, 1.0 / 24 }`.
   4. `StallMarkersTests.cs` (3 test), sự kiện tạo tay:
      - `StartThenClear`: `StallStarted` công đoạn 3 `NoPath` ô (5, 1, 5) → `Count == 1`, `TryGet(3)` đúng `Reason == NoPath`, `Cell == (5, 1, 5)`. Tiếp `StallCleared` công đoạn 3 → `Count == 0`.
      - `ReasonChangeReplaces`: `StallStarted` 3 `NoSeeds`, rồi `StallStarted` 3 `StorageFull` → `Count == 1`, lý do `StorageFull`.
      - `RespawnIgnored`: chỉ có `CreatureRespawned` → `Count == 0`.
   5. `CreatureStatusTests.cs` (3 test). `st.Stages.Add(new WorkStage(0, 0, WorkAbility.Till, new CellPos(5, 1, 5), false, default, 60, 0));` sinh vật có `StageId = 0`.
      - `TextPerState`: `Idle` → `"L1 · rảnh"`; `Moving` + `StageId = -1` → `"L1 · đi"`; `Moving` + `StageId = 0` → `"L1 · đi → xới"`; `Working` + `WorkDone = 27500` → `"L1 · xới 45%"`.
      - `PercentRoundsDown`: `Working`; `WorkDone = 0` → `"L1 · xới 0%"`; `WorkDone = 59999` → `"L1 · xới 99%"`; `WorkDone = 60000` → `"L1 · xới 100%"`.
      - `ReasonTexts`: `NoPath` → `"không có đường"`, `NoSeeds` → `"hết hạt"`, `StorageFull` → `"kho đầy"`, `NoAbility` → `"thiếu năng lực"`; `AbilityName(Haul)` → `"vận chuyển"`, `AbilityName(Harvest)` → `"thu hoạch"`.
   6. `client/Assets/Game/Tests/World/SimDemoTests.cs` (3 test):
      - `namespace Game.World.Tests`. Dựng: đọc `design/data/world_m1.json` (đường dẫn như `ConfigTests.cs:11`) → `JsonUtility.FromJson<TerrainParams>` → `TerrainGenerator.Generate(p)`. Đọc `design/data/sim_m1.json` → `JsonUtility.FromJson<SimConfig>`. Rồi `new Simulation(cfg, world)` → `SimDemo.Build(sim)`. Mỗi nhịp đếm `StallStarted` trong `sim.State.Events` rồi `Clear()`.
      - `BuildPlacesLayout`: 2 nhà (`Cell` = `HouseA`, `HouseB`); 1 kho ở (34, 18, 17), `Seeds == 10`. 3 sinh vật L1, L2, L3 ở (32, 18, 16), (33, 18, 16), (36, 18, 16), `HomeId` 0, 0, 1. 4 ô ruộng, `Soil` đúng thứ tự `SimDemo.Soil`.
      - `OneRoundFillsStorage`: 4000 lần `Tick()` (không gọi `KeepFarming`) → 0 `StallStarted`; kho `Produce == 4`, `Seeds == 10`.
      - `KeepFarmingContinues`: 8000 lần (`SimDemo.KeepFarming(sim)` trước mỗi `Tick()`) → 0 `StallStarted`; kho `Produce >= 8`.
   7. `client/Assets/Game/Tests/Client/SimViewPlayTests.cs` (PlayMode, 1 test, mẫu `TerrainEditPlayTests`). `[UnityTest] SetsUpDemoAndRestoresFrameRate`:
      - ghi `before = Application.targetFrameRate`, `vs = QualitySettings.vSyncCount`;
      - `go = new GameObject("Terrain")`; `view = go.AddComponent<TerrainChunkView>()`; `sv = go.AddComponent<SimView>()`; `yield return null` hai lần.
      - Assert:
        - `sv.Sim != null`; `sv.Sim.State.Creatures.Count == 3`; `sv.BodyCount == 3`;
        - `sv.TargetFps == 60`; `Application.targetFrameRate == 60`; `QualitySettings.vSyncCount == 0`;
        - `view.Editor.IsOccupied(34, 18, 17)` là true (kho).
      - `Object.Destroy(go)`; `yield return null` → `Application.targetFrameRate == before`, `QualitySettings.vSyncCount == vs`.

8. **Scene `SimView`** (cần Unity Editor mở, MCP):
   1. `cp client/Assets/Game/Client/Scenes/TerrainView.unity client/Assets/Game/Client/Scenes/SimView.unity` (**không** chép `.meta`). MCP `refresh_unity` → Unity tạo `SimView.unity.meta` mới.
   2. MCP `manage_scene` mở `Assets/Game/Client/Scenes/SimView.unity`.
   3. MCP `manage_components`: thêm component `Game.Client.SimView` vào GameObject `Terrain`.
   4. MCP `manage_gameobject`: `Main Camera` position (26, 26, 10), rotation euler (38.66, 36.87, 0). Hướng nhìn về tâm khu demo ≈ (32, 18, 18).
   5. MCP `manage_scene` lưu scene.

   Xong khi:
   - `grep -c "$(grep guid client/Assets/Game/Client/SimView.cs.meta | awk '{print $2}')" client/Assets/Game/Client/Scenes/SimView.unity` = 1;
   - `git diff --quiet -- client/Assets/Game/Client/Scenes/TerrainView.unity` exit 0;
   - Play thử scene trong Editor vài giây: `read_console` (Error) rỗng, có 3 hộp sinh vật (`find_gameobjects` tên bắt đầu "Sinh vật"). Thoát Play.

## Tiêu chí xong
- Mọi test ở bước 7 đạt, đúng tên và số. EditMode toàn bộ = **163 + 27 = 190**, PlayMode toàn bộ đạt (cũ + 1).
- `grep -rnE "float|double|System\.Random|UnityEngine|DateTime|Dictionary|HashSet|PriorityQueue" client/Assets/Game/Core/Sim/` → rỗng.
- `grep -rn "UnityEngine\|UnityEditor" client/Assets/Game/Core/View/` → rỗng.
- `cmp design/data/sim_m1.json client/Assets/Game/Client/Resources/Config/sim_m1.json` exit 0.
- Scene `SimView.unity` có component `SimView`; `TerrainView.unity` không đổi; `ProjectSettings/` không đổi sau bước dọn.
- `python3 tools/check_design.py` exit 0.

## Test sau khi xong

### Phần cần test
- **Bộ đếm nhịp** (bước 1) · `SimClockTests` (5) · số nhịp từng khung đúng như bước 7.1.
- **Nội suy** (bước 2) · `SimInterpolatorTests` (9) · giá trị như bước 7.2.
- **Đi mượt ở 30 / 60 FPS / khung lệch** (bước 1 + 2, "Xong khi" của bước 6) · `SmoothMotionTests` (3) · mỗi khung trong đoạn đi dời đúng 1,5 ô/giây × dt, không khung đứng yên, tới đúng (13, 1, 1).
- **Dấu đình trệ** (bước 3) · `StallMarkersTests` (3) · bật/tắt/thay lý do đúng.
- **Chữ trạng thái** (bước 3) · `CreatureStatusTests` (3) · đúng nguyên văn.
- **Bố trí demo trên map M1** (bước 4) · `SimDemoTests` (3) · bố trí đúng; 4 vụ vào kho; chạy mãi không đình trệ.
- **Config client** (bước 5) · `ConfigTests.SimClientCopyMatchesDesignData` + `cmp` · giống nhau.
- **`SimView` dựng trong Unity** (bước 6) · `SimViewPlayTests` (1) · dựng được 3 sinh vật, khoá 60 FPS, trả lại cài đặt FPS.
- **Scene** (bước 8) · grep guid, `git diff` TerrainView, Play thử · như "Xong khi" bước 8.
- **Regression mô phỏng bước 1–5** · EditMode toàn bộ · 163 test cũ vẫn đạt (không sửa dòng nào trong `Core/Sim/` cũ).
- **Regression world/terrain** (scene `TerrainView`, `TerrainEditPlayTests`, `OceanBackdropTests`) · PlayMode toàn bộ · test cũ vẫn đạt.

### Lệnh executor chạy
1. `cmp design/data/sim_m1.json client/Assets/Game/Client/Resources/Config/sim_m1.json` → exit 0.
2. MCP `refresh_unity` → `read_console` (Error): không có lỗi biên dịch.
3. MCP `run_tests` EditMode toàn bộ → ghi dòng tổng kết (mong đợi 190 / 190).
4. MCP `run_tests` PlayMode toàn bộ → ghi dòng tổng kết. **Ngay sau đó:** `git checkout -- client/ProjectSettings/EditorSettings.asset`.
5. `git status --short client/ProjectSettings/`. Còn file đổi (vd `QualitySettings.asset` do đổi `vSyncCount` khi Play) → `git checkout --` file đó, ghi tên file vào Kết quả.
6. Hai lệnh grep ở Tiêu chí xong → rỗng.
7. `python3 tools/check_design.py` → exit 0.
8. `git status --short` + `git diff --stat` trước commit: chỉ file trong Phạm vi.
9. Ghi giờ bắt đầu/kết thúc (đồng hồ máy) vào Kết quả.

Editor tắt thì bước 3–4 dùng `python3 tools/unity_test.py EditMode` / `PlayMode`. Bước 8 của "Các bước" bắt buộc cần Editor: Editor tắt → trạng thái `cần Opus`.

### User tự test
Mở `Assets/Game/Client/Scenes/SimView.unity` trong Editor, bấm Play. Bấm vào Game view để nhận phím.
1. **Dựng cảnh:** gần camera thấy 2 hộp nâu (nhà), 1 hộp xám (kho), 4 ô ruộng nâu sẫm, 3 hộp cam / xanh dương / tím (L1 / L2 / L3) có mũi đen chỉ hướng đi.
2. **Nhãn trạng thái:** đổi theo việc, vd "L1 · đi → xới", "L1 · xới 37%" (số tăng dần), "L2 · tưới …", "L3 · đi → vận chuyển", "… · rảnh".
3. **Đi mượt:**
   - Bấm F1: HUD ghi "mục tiêu 30", số "đo" ≈ 30. Sinh vật đi đều, không khựng mỗi 0,1 giây, không nhảy ô, kể cả lúc leo / xuống 1 ô.
   - Bấm F2: "mục tiêu 60", "đo" ≈ 60, đi đều như vậy.
   - Ghi lại nếu số "đo" thấp hơn nhiều so với mục tiêu (máy chậm).
4. **Dấu đình trệ:**
   - Đào 8 khối ở chỗ khác để có vật liệu (chuột trái).
   - Đặt (chuột phải) tường cao 2 ô kín 4 ô kề kho: (33, 18–19, 17), (35, 18–19, 17), (34, 18–19, 16), (34, 18–19, 18). Đó là các ô bên trái / phải / trước / sau hộp xám, ở tầng sinh vật đứng và tầng trên.
   - Chờ tới lúc thu hoạch, khoảng 2,5 phút sau khi Play. Mong đợi: hiện "! không có đường" màu đỏ ở chỗ hàng nằm (ô ruộng vừa thu hoạch).
   - Đào bỏ một khối tường tầng dưới → dấu mất, L3 mang hàng vào kho.
5. **Đào/đặt vẫn chạy như TerrainView.** Đặt khối lên ô có sinh vật / nhà / kho → báo "ô đang bị chiếm".

Phản hồi cho Planner: đạt / không đạt từng mục, kèm ảnh chụp nếu có chỗ lạ.

## Dừng và chuyển lại Opus khi
- Brief mơ hồ hoặc mâu thuẫn với code thật.
- Cần một quyết định thuộc AGENTS.md §3.4 hoặc ngoài "Quyết định đã chốt".
- Cùng một lỗi sửa 2 lần vẫn chưa qua.
- Phải sửa file ngoài "Phạm vi".
- Một số trong test (nhịp, toạ độ, sản lượng, độ dời) không khớp. **Không sửa số trong test cho khớp code, không đổi toạ độ demo.** Ghi số thực tế vào Vướng mắc.
- `SimDemo.Build` ném lỗi trên map M1 (ô không đứng được / không phải đất).
- Test cũ nào hỏng.
- Unity Editor không mở được cho bước 8.

---
<!-- Từ đây trở xuống: EXECUTOR ghi. -->

## Kết quả
Base commit: 0477fb0
Chạy bằng: Sonnet · medium (claude-sonnet-5-5)
Bắt đầu: 2026-10-16 22:09 · dừng: 22:13 (bước 1–7); tiếp bước 8 từ 22:14, xong 22:19. Tổng ≈ 9 phút làm việc.
Commit code (bước 1–7): 487e523. SimView.cs chép nguyên, không sửa. Commit bước 8 (scene SimView.unity + .meta): 4f2932f. Không sửa SimView.cs.
File đổi toàn brief: Core/View/{SimClock,SimInterpolator,StallMarkers,CreatureStatus}.cs, Core/Sim/SimDemo.cs, Client/{SimView,ConfigLoader}.cs, Resources/Config/sim_m1.json, Scenes/SimView.unity, 5 test Core/View, SimDemoTests, SimViewPlayTests, ConfigTests (+.meta).

## Kết quả test
- Bộ đếm nhịp, nội suy, đi mượt 30/60/lệch, dấu đình trệ, chữ trạng thái, bố trí demo, config client: đạt (EditMode qua MCP `run_tests`: 190/190, 0 lỗi).
- `SimViewPlayTests`: đạt. PlayMode toàn bộ qua MCP: 8/8 đạt. Đã `git checkout` EditorSettings.asset; `git status client/ProjectSettings/` sạch.
- Scene: grep guid SimView.cs trong SimView.unity = 1; `git diff` TerrainView.unity exit 0; Play thử: `read_console` (Error) rỗng, 3 GameObject "Sinh vật …" dưới "Sim", mô phỏng chạy (nhịp 24 sau 120 bước, L1 đang Working). Camera (26, 26, 10) / euler (38.66, 36.87, 0).
- cmp sim_m1.json exit 0; hai grep rỗng; check_design exit 0.
- Hình ảnh, độ mượt F1/F2, nhãn, dấu "!" đình trệ, đào/đặt: **chờ user** (mục "User tự test"); chưa tự đánh đạt.
- Ghi chú: lần Play đầu tiên ngay sau khi tạo component, Start chưa chạy (domain reload); vào Play lại thì bình thường.

## Vướng mắc
Không còn (Editor đã mở, bước 8 xong).

---
<!-- PLANNER ghi khi review. -->

## Review
Planner, 2026-10-16 — phần code: **đạt**; chờ user xem (mục "User tự test" 1–5) rồi mới kết luận.
- Chạy bằng Sonnet · medium = Model thực thi. Mọi dòng "Phần cần test" có kết quả; dòng hình ảnh/cảm giác = chờ user.
- Diff `0477fb0..5a6c993`: 35 file, đều trong Phạm vi; `Core/Sim` chỉ thêm `SimDemo.cs`; không đổi test cũ, `TerrainView.unity`, `ProjectSettings/`.
- Số trong test khớp brief: `SmoothMotionTests` (1,5e-6 × µs, cửa sổ nhịp 2–80, ≥ 200 khung, tới (13, 1, 1)); `SimClockTests` (khung 6k / 3k + 1, 20 / 33323 µs, chặn 5); `SimDemoTests` (4 nông sản, 10 hạt, 0 đình trệ; ≥ 8 sau 8000 nhịp).
- `SimView`: thứ tự KeepFarming → Tick → Capture → ApplyEvents → Clear, nối `IsOccupied` / `CellChanged`, lưu và trả `vSyncCount` / `targetFrameRate` đúng brief.
- Ghi nhận: lần Play đầu ngay sau khi thêm component không chạy Start (domain reload) — không phải lỗi code.
- Giờ thực tế ≈ 9 phút làm việc của executor (ước lượng 1–2 ngày-người).
