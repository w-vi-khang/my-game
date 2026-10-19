# Brief 0008 — Simulation bước 6b (phần 2): sinh vật rảnh về quanh nhà + lang thang, không dừng trên ô ruộng

> Trạng thái: chờ review
> Review: đầy đủ + user xem
> Model thực thi: Sonnet · medium
> Cập nhật: 2026-10-19 15:17 — Executor xong

<!-- Planner (Opus) viết, Executor (Sonnet) làm theo ở PHIÊN KHÁC — executor không có lịch sử chat, không được tự quyết.
Trạng thái: chờ làm → đang làm → chờ review → đã duyệt; hoặc → cần Opus (executor dừng, hỏi ở mục Vướng mắc).
Review: nhẹ (đọc mục Kết quả) | đầy đủ (đọc cả diff) | user xem (có phần user tự kiểm bằng mắt/chơi thử) — có thể ghép "đầy đủ + user xem".
Model thực thi: BẮT BUỘC, đúng dạng `<Model> · <effort>` — Model: Opus | Sonnet | Haiku | Fable; effort: low | medium | high | xhigh | max (chọn theo AGENTS.md §10.2).
Executor kiểm tra model + effort của phiên TRƯỚC khi làm (scripts/brief_check.py); lệch hoặc không xác định được → cảnh báo user và DỪNG.
File được commit. Tự kiểm trước khi giao (AGENTS.md §10.3): executor có phải hỏi gì không? còn chỗ nào bỏ ngỏ 2 phương án? -->

## Mục tiêu
Phần 2 (cuối) của [task 6b](../tasks/m1-sim-buoc-6b-luat-di-lai.md) — quyết định [0019](../../design/decisions/0019-chan-nhau-ranh-quanh-nha-khong-dung-tren-ruong.md) mục 2–3:
- Sinh vật rảnh ở ngoài **bán kính 4 ô** quanh nhà thì đi về trong bán kính. Ở trong bán kính thì cứ **50–100 nhịp** đi tới một ô ngẫu nhiên trong bán kính. Có việc thì bỏ lang thang ngay.
- Sinh vật **đi qua** ô ruộng thoải mái nhưng **không dừng** trên ô ruộng khi không có việc ở chính ô đó: ô ruộng không được làm đích lang thang, không làm chỗ né, không làm chỗ đứng làm việc cho ô khác (trừ khi chỉ còn ô ruộng để đứng).

Brief 0007 (phần 1: chặn nhau khi đi) đã duyệt ở commit `0991aa2`, code ở `20d0529`.

## Bối cảnh cần đọc
- `client/Assets/Game/Core/Sim/Simulation.cs` (976 dòng). Chỗ sẽ sửa:
  - `HomeGoals :143-151` (ô kề nhà, đứng được, thứ tự +X, −X, +Z, −Z → điền vào `goals`). `Respawn :204-241` (reset trường ở `:223-226`). `Tick :262-282`: thứ tự pha hiện là Terrain → Home → Work → Crop → Move → Assign → Stuck → Report.
  - `CanStand :284`, `FramesOverlap :289-290` (public static), `FirstBlocker :301-307`, `FindPath :353`.
  - `Repath :429-452`, `StopWalk :455-462`, `AdjacentGoals :465-478`, `PlanTo :481-496`, `SetPath :498-504`.
  - `HandleBlocked :704-719` (`:711-712` là chỗ chờ đủ 10 nhịp → `RepathAvoiding`), `FindAside :748-756`, `Dodge :759-767`, `StartYield :770-784` (gọi `FindAside` ở `:778`), `OnArrive :811-832` (con không có việc → `Idle`).
  - `AssignPhase :834-921`: điều kiện chọn sinh vật `:889` là `c.HomeId < 0 || c.StageId >= 0 || ...` → con đang lang thang (`Moving`, `StageId = -1`) vẫn nhận việc được. **Không sửa** hàm này.
  - Bộ đệm dùng chung: `tmpPath :13`, `goals :14`.
- `client/Assets/Game/Core/Sim/SimData.cs` — `Creature :65-114` (trường `YieldHold :91`), `FarmPlot :198-215` (`Site = Soil.Offset(0, 1, 0)` = ô sinh vật đứng trên ruộng), `House :218-237`, `SimState :240-257` (`Rng` dùng chung, `Plots`, `Houses`).
- `client/Assets/Game/Core/Sim/GridPathfinder.cs` — `DirX/DirZ/DirY :17-19`, `seenStamp :24`, `queue :29`, `stamp :31`, `CanStand :85-95`, `CanStep :97-106`, `FindPathToNearest :143-188` (mẫu BFS: mỗi hướng lấy biến thể Y hợp lệ đầu tiên rồi `break`).
- `client/Assets/Game/Core/Sim/SimHash.cs :38` (dòng `YieldHold` — thêm trường mới ngay sau).
- `client/Assets/Game/Core/Sim/SimConfig.cs` — `CreatureConfig :100-106`, `PhaseTicks :162`, `Validate :192-267` (2 dòng kiểm cuối `:265-266`). `SimRng.NextInt(bound)` trả số đều trong `[0, bound)`.
- `design/data/sim_m1.json` (nguồn chuẩn) + bản chép `client/Assets/Game/Client/Resources/Config/sim_m1.json` (`ConfigTests` bắt hai bản giống hệt; `SimConfigTests.ConfigMatchesCodeDefaults` bắt JSON = giá trị mặc định trong code).
- Test: `Tests/Core/Sim/TestWorlds.cs` (`Flat`, `Pillar`, `HomeAll` — `HomeAll` đặt nhà ở góc (14, y, 14), xa các sinh vật trong hầu hết test cũ), `SimConfigTests.cs` (`ConfigParsesAndValidates :15`, `ValidateRejectsBadValues :36`, ca cuối `:94-96`), `GridPathfinderTests.cs`, `MovementTests.cs` (`NewOrder :11`, `Overlaps :14`, `IdleInPathDodges :173-222` — mẫu cho test F), `Tests/World/SimDemoTests.cs` (`NewSim :15`, `NoTwoCreaturesShareCell :41-73`).
- Nhịp tính tay dưới đây dựa trên: L1, L2 đi 150 phần nghìn ô/nhịp; đường đặt ở nhịp T thì bắt đầu đi từ nhịp T+1, tới ô thứ k ở nhịp T + ⌈1000·k / 150⌉ (1 ô: +7, 2 ô: +14, 3 ô: +20, 4 ô: +27). Xới 60 nhịp, bắt đầu cộng từ nhịp sau nhịp tới nơi (khớp test cũ: tới nhịp 55 → xong nhịp 115).

## Quyết định đã chốt (không được đổi)
1. **Bán kính** (user xác nhận 2026-10-19): khoảng cách Chebyshev theo X/Z giữa khung sinh vật và khung nhà (0 = chạm), không tính Y. "Trong bán kính" = khoảng cách ≤ `homeRadius` (4).
2. **Ô ruộng** = ô `Site` của mọi `FarmPlot` (mọi ô của lệnh trồng cây, kể cả chưa xới — 0021). Sinh vật "đứng trên ô ruộng" = khung tại `Cell` chồng lên ô `Site`.
3. **Chọn ô lang thang:** BFS từ các ô kề nhà (`HomeGoals`) chỉ đi qua ô trong bán kính → bỏ ô đang đứng, ô ruộng, ô có sinh vật khác chiếm (`FirstBlocker`) → chọn ngẫu nhiên đều một ô (`State.Rng`, **chỉ rút số khi có hơn 1 ô**) → A\* từ chỗ đang đứng.
4. **Về nhà** (user 2026-10-19): con rảnh ở ngoài bán kính đi tới một ô chọn đúng như mục 3, **ngay** nhịp đó. Con rảnh đứng trên ô ruộng cũng vậy.
5. **Lang thang:** con rảnh trong bán kính, không ở trên ô ruộng → hẹn `WanderAt` = nhịp hiện tại + `wanderMinMs..wanderMaxMs` đổi ra nhịp (50..100, `State.Rng`, chỉ rút số khi min ≠ max); tới hẹn thì chọn ô như mục 3. Không có ô nào → hẹn lại.
6. **Chỉ con có nhà và về nhà được** (`HomeId >= 0 && HomeReachable`) mới về/lang thang. Con không về nhà được đứng yên như cũ → luật kẹt 0017 giữ nguyên.
7. **Có việc → bỏ lang thang ngay:** đã có sẵn (`AssignPhase :889`); `IdlePhase` xoá hẹn của con đang có việc.
8. **Con rảnh đang đi bị chắn** (user 2026-10-19): ở nhịp chờ thứ 10 (bội của `BlockWaitTicks`), nếu `RepathAvoiding` không được thì **đứng lại** (`StopWalk` → `Idle`) — không chờ tới đi xuyên. Nhịp đó `IdlePhase` xử lý luôn: trong bán kính → hẹn lang thang; ngoài bán kính hoặc trên ô ruộng → chọn đích mới ngay.
9. **Chỗ đứng làm việc** (`PlanTo` với `Adjacent` / `AdjacentOrOn`): thử các ô kề **không phải ô ruộng** trước; không tới được ô nào trong đó thì mới thử cả ô ruộng kề (user xác nhận 2026-10-19: "chỉ còn ô ruộng khác để đứng làm việc thì vẫn cho đứng"); rồi mới tới đứng trên chính ô (`AdjacentOrOn`) như cũ.
10. **Chỗ né:** con rảnh né (`Dodge`) không chọn ô ruộng. Con đang có việc nhường (`StartYield`) **được** đứng chờ trên ô ruộng (đứng chờ = đang đi, user xác nhận 2026-10-19).
11. **Pha mới `IdlePhase`** chạy sau `StuckPhase`, trước `ReportPhase` (để con vừa hiện lại ở nhà cũng được xử lý trong nhịp). Các pha khác giữ thứ tự.
12. **Test cũ hỏng** — làm đúng thứ tự, ghi bảng vào Kết quả (`test · dòng · cách sửa · nguyên nhân`):
    - (a) Test trong `Tests/Core/Sim/` (trừ `SimDeterminismTests` và test mới của brief này): đổi cấu hình của test đó từ `new SimConfig()` sang `TestWorlds.NoWander()` (con rảnh đứng yên như trước 0019 mục 2). Cấu hình tạo trong hàm phụ dùng chung (vd `MovementTests.HeadOn`, `FarmTests.Layout(new SimConfig(), …)` ở chỗ gọi) → đổi ở chỗ tạo cấu hình. Không sửa số, không xoá assert, không đổi bố trí. Chạy lại: đạt → xong.
    - (b) Vẫn hỏng sau (a), và nguyên nhân (xác nhận bằng trace `Cell`, `State`, `StageId`, `MoveTarget` quanh nhịp lệch) là luật ô ruộng (mục 9, 10, hoặc con rảnh rời ô ruộng ở mục 4) → **chỉ** sửa con số mong đợi (nhịp, ô).
    - (c) Còn lại, hoặc test hỏng thuộc `SimDeterminismTests`, `SimDemoTests`, `Tests/Core/View/*`, PlayMode → **dừng, `cần Opus`**, ghi nhịp đầu tiên lệch + trace.

## Phạm vi
- Được sửa/tạo:
  - `client/Assets/Game/Core/Sim/Simulation.cs`, `SimData.cs`, `GridPathfinder.cs`, `SimHash.cs`, `SimConfig.cs`
  - `design/data/sim_m1.json`, `client/Assets/Game/Client/Resources/Config/sim_m1.json`
  - `client/Assets/Game/Tests/Core/Sim/TestWorlds.cs`, `SimConfigTests.cs`, `GridPathfinderTests.cs`
  - Tạo `client/Assets/Game/Tests/Core/Sim/IdleTests.cs` (+ `.meta` do Unity tạo)
  - `client/Assets/Game/Tests/World/SimDemoTests.cs`
  - Test cũ khác trong `client/Assets/Game/Tests/Core/Sim/` **chỉ** theo Quyết định 12.
  - Mục Kết quả / Kết quả test / Vướng mắc của brief này.
- Không được sửa: `SimDemo.cs`, `Client/*` (kể cả `SimView.cs`), `Core/View/*`, `design/` (trừ `sim_m1.json`), task contract, file khác.

## Các bước
Trước khi bắt đầu: đổi Trạng thái → `đang làm`, ghi giờ bắt đầu.

1. **Cấu hình** — `SimConfig.cs`, hai file `sim_m1.json`, `SimConfigTests.cs`.
   - `CreatureConfig`, sau `phaseAfterMs` (`:105`):
     ```csharp
     public int homeRadius = 4;
     public int wanderMinMs = 5000;
     public int wanderMaxMs = 10000;
     ```
   - Sau `PhaseTicks :162`:
     ```csharp
     public int WanderMinTicks => MsToTicks(creature.wanderMinMs);

     public int WanderMaxTicks => MsToTicks(creature.wanderMaxMs);
     ```
   - `Validate`, sau kiểm `phaseAfterMs :265-266`:
     ```csharp
     if (creature.homeRadius < 1 || creature.homeRadius > 64)
         throw new ArgumentException("creature.homeRadius phải trong [1, 64].");
     if (creature.wanderMinMs <= 0 || creature.wanderMinMs % msPerTick != 0)
         throw new ArgumentException("creature.wanderMinMs phải > 0 và chia hết cho " + msPerTick + ".");
     if (creature.wanderMaxMs < creature.wanderMinMs || creature.wanderMaxMs % msPerTick != 0)
         throw new ArgumentException("creature.wanderMaxMs phải >= wanderMinMs và chia hết cho " + msPerTick + ".");
     ```
   - `design/data/sim_m1.json`, dòng `creature` thành:
     `"creature": { "stuckTimeMs": 30000, "blockWaitMs": 1000, "phaseAfterMs": 5000, "homeRadius": 4, "wanderMinMs": 5000, "wanderMaxMs": 10000 }`
     rồi `cp design/data/sim_m1.json client/Assets/Game/Client/Resources/Config/sim_m1.json`.
   - `SimConfigTests.ConfigParsesAndValidates`: thêm `Assert.AreEqual(4, cfg.creature.homeRadius);`, `Assert.AreEqual(50, cfg.WanderMinTicks);`, `Assert.AreEqual(100, cfg.WanderMaxTicks);`. `ValidateRejectsBadValues`, sau ca `phaseAfterMs <= blockWaitMs` (`:94-96`):
     ```csharp
     c = new SimConfig();
     c.creature.homeRadius = 0;
     Assert.Throws<ArgumentException>(c.Validate, "homeRadius 0");

     c = new SimConfig();
     c.creature.homeRadius = 65;
     Assert.Throws<ArgumentException>(c.Validate, "homeRadius 65");

     c = new SimConfig();
     c.creature.wanderMinMs = 5050;
     Assert.Throws<ArgumentException>(c.Validate, "wanderMinMs lẻ nhịp");

     c = new SimConfig();
     c.creature.wanderMaxMs = 4000;
     Assert.Throws<ArgumentException>(c.Validate, "wanderMaxMs < wanderMinMs");
     ```
   Xong khi: `cmp` hai file JSON exit 0.

2. **Trường mới + mã băm** — `SimData.cs`, `SimHash.cs`.
   - `Creature`, sau `public bool YieldHold;` (`:91`):
     ```csharp
     /// <summary>Nhịp hẹn đi lang thang (con rảnh trong bán kính quanh nhà, 0019); -1 = chưa hẹn.</summary>
     public long WanderAt = -1;
     ```
   - `SimHash.cs`, ngay sau dòng `YieldHold` (`:38`): `h = MixLong(h, c.WanderAt);`

3. **BFS trong vùng** — `GridPathfinder.cs`, `GridPathfinderTests.cs`.
   - Thêm ngay sau `FindPathToNearest` (`:188`):
     ```csharp
     /// <summary>
     /// BFS nhiều nguồn: các ô starts trong vùng và đứng được (theo thứ tự danh sách, bỏ trùng), rồi thứ tự hướng như A*;
     /// chỉ đi vào ô thoả region. result = mọi ô tới được (kể cả nguồn) theo thứ tự lấy ra khỏi hàng đợi.
     /// </summary>
     public void FloodRegion(List<CellPos> starts, int sx, int sz, int sy, Predicate<CellPos> region, List<CellPos> result)
     {
         result.Clear();
         var size = world.Size;
         stamp++;
         queue.Clear();
         for (int i = 0; i < starts.Count; i++)
         {
             var s = starts[i];
             if (!size.InBounds(s.X, s.Y, s.Z) || !region(s) || !CanStand(s, sx, sz, sy)) continue;
             int si = size.Index(s.X, s.Y, s.Z);
             if (seenStamp[si] == stamp) continue;
             seenStamp[si] = stamp;
             queue.Add(si);
         }
         for (int head = 0; head < queue.Count; head++)
         {
             int ci = queue[head];
             size.Coords(ci, out int cx, out int cy, out int cz);
             var cur = new CellPos(cx, cy, cz);
             result.Add(cur);
             for (int d = 0; d < 4; d++)
                 for (int k = 0; k < DirY.Length; k++)
                 {
                     var to = new CellPos(cx + DirX[d], cy + DirY[k], cz + DirZ[d]);
                     if (!size.InBounds(to.X, to.Y, to.Z) || !CanStep(cur, to, sx, sz, sy)) continue;
                     int ni = size.Index(to.X, to.Y, to.Z);
                     if (seenStamp[ni] != stamp && region(to))
                     {
                         seenStamp[ni] = stamp;
                         queue.Add(ni);
                     }
                     break;
                 }
         }
     }
     ```
   - `GridPathfinderTests` thêm test `FloodRegionStaysInRegion`: `Flat(0)` (đứng ở y = 1), `region = p => Math.Abs(p.X - 5) <= 1 && Math.Abs(p.Z - 5) <= 1`:
     - `starts = [(5,1,5), (5,1,5)]` → `result.Count == 9`, `result[0] == (5,1,5)`, `result[1] == (6,1,5)`, mọi ô thoả region.
     - `starts = [(9,1,9)]` → `result.Count == 0`.
     (Thêm `using System;` nếu file chưa có.)

4. **Hàm phụ trong `Simulation`** — thêm gần `FirstBlocker` (`:301`), và trường `readonly List<CellPos> wanderCells = new List<CellPos>();` cạnh `homePath :15`:
   ```csharp
   /// <summary>Khoảng cách Chebyshev X/Z giữa khung (sx, sz) tại p và khung nhà h; 0 = chạm hoặc chồng; không tính Y (0019).</summary>
   public static int HomeDistance(CellPos p, int sx, int sz, House h)
   {
       int dx = Math.Max(0, Math.Max(h.Cell.X - (p.X + sx - 1), p.X - (h.Cell.X + h.SizeX - 1)));
       int dz = Math.Max(0, Math.Max(h.Cell.Z - (p.Z + sz - 1), p.Z - (h.Cell.Z + h.SizeZ - 1)));
       return Math.Max(dx, dz);
   }

   /// <summary>Khung (sx, sz, sy) tại p chồng lên ô Site của một ô ruộng (0019 mục 3).</summary>
   public bool OnPlot(CellPos p, int sx, int sz, int sy)
   {
       var ps = State.Plots;
       for (int i = 0; i < ps.Count; i++)
           if (FramesOverlap(p, sx, sz, sy, ps[i].Site, 1, 1, 1)) return true;
       return false;
   }
   ```

5. **Pha rảnh** — `Simulation.cs`.
   - `Tick :262-282`: thêm `IdlePhase();` giữa `StuckPhase();` và `ReportPhase();`.
   - Thêm (ngay sau `StuckPhase`):
     ```csharp
     /// <summary>Con rảnh có nhà: ngoài bán kính hoặc trên ô ruộng → đi ngay; trong bán kính → hẹn rồi lang thang (0019 mục 2–3).</summary>
     void IdlePhase()
     {
         var cs = State.Creatures;
         for (int i = 0; i < cs.Count; i++)
         {
             var c = cs[i];
             if (c.StageId >= 0 || c.HomeId < 0 || !c.HomeReachable)
             {
                 c.WanderAt = -1;
                 continue;
             }
             if (c.State != CreatureState.Idle) continue;
             var h = State.Houses[c.HomeId];
             bool mustLeave = OnPlot(c.Cell, c.SizeX, c.SizeZ, c.SizeY)
                 || HomeDistance(c.Cell, c.SizeX, c.SizeZ, h) > Config.creature.homeRadius;
             if (!mustLeave)
             {
                 if (c.WanderAt < 0) c.WanderAt = State.Tick + NextWanderDelay();
                 if (State.Tick < c.WanderAt) continue;
             }
             c.WanderAt = -1;
             if (!StartWander(c, h) && !mustLeave) c.WanderAt = State.Tick + NextWanderDelay();
         }
     }

     int NextWanderDelay()
     {
         int min = Config.WanderMinTicks;
         int span = Config.WanderMaxTicks - min + 1;
         return span == 1 ? min : min + State.Rng.NextInt(span);
     }

     /// <summary>Đi tới một ô ngẫu nhiên trong bán kính quanh nhà h: tới được từ ô kề nhà, không phải ô đang đứng, không phải ô ruộng, không có sinh vật khác.</summary>
     bool StartWander(Creature c, House h)
     {
         HomeGoals(c, h);
         int r = Config.creature.homeRadius;
         pathfinder.FloodRegion(goals, c.SizeX, c.SizeZ, c.SizeY, p => HomeDistance(p, c.SizeX, c.SizeZ, h) <= r, wanderCells);
         int n = 0;
         for (int i = 0; i < wanderCells.Count; i++)
         {
             var p = wanderCells[i];
             if (p == c.Cell || OnPlot(p, c.SizeX, c.SizeZ, c.SizeY) || FirstBlocker(c, p) >= 0) continue;
             wanderCells[n++] = p;
         }
         if (n == 0) return false;
         var target = wanderCells[n == 1 ? 0 : State.Rng.NextInt(n)];
         if (!FindPath(c, c.Cell, target)) return false;
         c.State = CreatureState.Moving;
         SetPath(c);
         c.MoveProgress = 0;
         c.BlockedTicks = 0;
         return true;
     }
     ```

6. **Các chỗ khác trong `Simulation.cs`**:
   - `StopWalk :455-462`: thêm `c.BlockedTicks = 0;` và `c.WanderAt = -1;` (trước `c.MoveTarget = c.Cell;`).
   - `HandleBlocked`: ngay sau `if (RepathAvoiding(c, next)) return;` (`:712`) thêm
     ```csharp
     if (c.StageId < 0)
     {
         StopWalk(c); // con rảnh không có đường khác → đứng lại (Quyết định 8)
         return;
     }
     ```
   - `FindAside :748-756`: thêm tham số `bool avoidPlots` (chữ ký `bool FindAside(Creature y, Creature w, bool avoidPlots)`), điều kiện nhận thành `p => !PathTouches(w, p, y) && !(avoidPlots && OnPlot(p, y.SizeX, y.SizeZ, y.SizeY))`. Sửa `<summary>` thêm "; avoidPlots: không nhận ô ruộng".
   - `Dodge :759-767`: gọi `FindAside(o, w, true)`; thêm `o.WanderAt = -1;` sau `o.BlockedTicks = 0;`.
   - `StartYield :778`: gọi `FindAside(y, w, false)`.
   - `Respawn`: sau `c.YieldHold = false;` (`:226`) thêm `c.WanderAt = -1;`.
   - `AdjacentGoals :465-478` thay bằng:
     ```csharp
     /// <summary>Ô kề của chỗ p (tối đa 4, theo thứ tự hướng) mà sinh vật c đứng được — điền vào goals (0016). skipPlots: bỏ ô ruộng (0019); trả số ô đã bỏ.</summary>
     int AdjacentGoals(Creature c, CellPos p, bool skipPlots)
     {
         goals.Clear();
         int skipped = 0;
         for (int d = 0; d < 4; d++)
             for (int k = 0; k < 3; k++)
             {
                 var n = p.Offset(DisplaceX[d], DisplaceY[k], DisplaceZ[d]);
                 if (pathfinder.CanStand(n, c.SizeX, c.SizeZ, c.SizeY))
                 {
                     if (skipPlots && OnPlot(n, c.SizeX, c.SizeZ, c.SizeY)) skipped++;
                     else goals.Add(n);
                     break;
                 }
             }
         return skipped;
     }
     ```
   - `PlanTo :481-496`: hai dòng `AdjacentGoals(c, site); if (pathfinder.FindPathToAny(...)) return true;` thay bằng
     ```csharp
     int skipped = AdjacentGoals(c, site, true);
     if (pathfinder.FindPathToAny(c.Cell, goals, c.SizeX, c.SizeZ, c.SizeY, tmpPath)) return true;
     if (skipped > 0)
     {
         AdjacentGoals(c, site, false);
         if (pathfinder.FindPathToAny(c.Cell, goals, c.SizeX, c.SizeZ, c.SizeY, tmpPath)) return true;
     }
     ```
     (`FindPathToAny` với `goals` rỗng trả false.) Phần `AdjacentOrOn → Exact` giữ nguyên.
   Xong khi: MCP `refresh_unity` → `read_console` (Error) không có lỗi biên dịch.

7. **`TestWorlds.cs`** — thêm:
   ```csharp
   /// <summary>Cấu hình mặc định nhưng con rảnh đứng yên: bán kính phủ cả map 16×16, hẹn lang thang 1 giờ — cho test cũ không kiểm luật rảnh (0019 mục 2).</summary>
   public static SimConfig NoWander()
   {
       var c = new SimConfig();
       c.creature.homeRadius = 64;
       c.creature.wanderMinMs = 3600000;
       c.creature.wanderMaxMs = 3600000;
       return c;
   }
   ```

8. **Test mới** — tạo `Tests/Core/Sim/IdleTests.cs` (namespace `Game.Core.Tests`, class `IdleTests`, `using System; using System.Collections.Generic; using Game.Core.Sim; using NUnit.Framework;`). Hàm phụ:
   ```csharp
   static void RunTo(Simulation sim, long tick)
   {
       while (sim.State.Tick < tick) sim.Tick();
   }

   static SimConfig Cfg(int radius, int wanderMs)
   {
       var c = new SimConfig();
       c.creature.homeRadius = radius;
       c.creature.wanderMinMs = wanderMs;
       c.creature.wanderMaxMs = wanderMs;
       return c;
   }

   /// <summary>Flat(1) + nhà id 0 tại (10,2,10) (x 10–11, z 10–11). Cột Stone y 1..3 trên mọi ô (x 9–12, z 9–12) ngoài nhà, trừ các ô (x, z) trong open.</summary>
   static Simulation Ring(SimConfig cfg, params (int x, int z)[] open)
   {
       var w = TestWorlds.Flat(1);
       for (int x = 9; x <= 12; x++)
           for (int z = 9; z <= 12; z++)
           {
               bool inHouse = x >= 10 && x <= 11 && z >= 10 && z <= 11;
               if (inHouse || Array.IndexOf(open, (x, z)) >= 0) continue;
               TestWorlds.Pillar(w, x, z, 3);
           }
       var sim = new Simulation(cfg, w);
       sim.AddHouse(new CellPos(10, 2, 10));
       return sim;
   }
   ```
   Chép thêm `Overlaps(SimState)` và `Done(Simulation, int)` từ `MovementTests` (y nguyên). Số nhịp dưới đây đã tính tay theo code ở bước 5–6 — **lệch thì không sửa số, dừng `cần Opus`** kèm `Cell`, `State`, `MoveTarget`, `WanderAt`, `BlockedTicks` quanh nhịp lệch. "c" = `sim.State.Creatures[0]` nếu không nói khác.
   - **A `FarIdleWalksBackIntoRadius`** — `Ring(Cfg(1, 5000), (12, 10))`; `AddCreature("L1", (15,2,10))`; `AssignHome(0, 0)`.
     - `RunTo 1`: `Moving`, `MoveTarget == (12,2,10)`, `Path.Count == 3`, `WanderAt == -1`.
     - `RunTo 20`: `Cell == (13,2,10)`, `Moving`.
     - `RunTo 21`: `Cell == (12,2,10)`, `Idle`, `WanderAt == 71`.
     - `RunTo 70`: `Idle`, `WanderAt == 71`.
     - `RunTo 71`: `Idle`, `Cell == (12,2,10)`, `WanderAt == 121` (không còn ô nào khác → hẹn lại).
   - **B `WandersOnTimerWithinRadius`** — `Ring(Cfg(1, 5000), (12, 10), (12, 11))`; L1 tại (12,2,10); `AssignHome(0, 0)`.
     - `RunTo 50`: `Idle`, `Cell == (12,2,10)`, `WanderAt == 51`.
     - `RunTo 51`: `Moving`, `StageId == -1`, `MoveTarget == (12,2,11)`.
     - `RunTo 57`: `Cell == (12,2,10)`, `Moving`.
     - `RunTo 58`: `Cell == (12,2,11)`, `Idle`, `WanderAt == 108`.
     - `RunTo 108`: `Moving`, `MoveTarget == (12,2,10)`.
     - `RunTo 115`: `Cell == (12,2,10)`, `Idle`, `WanderAt == 165`.
   - **C `IdleOnPlotLeavesAtOnce`** — `Ring(Cfg(1, 5000), (12, 10), (12, 11))`; `int k = sim.AddStorage((1,2,1))`; `sim.AddFarmOrder([(12,1,11)], k)`; `AddCreature("L2", (12,2,11))` (đứng trên ô ruộng; không ai xới được → công đoạn xới đình trệ, đúng ý test); `AssignHome(0, 0)`. Tick từng nhịp tới 58, mỗi nhịp assert **không** (`State == Idle` và `Cell == (12,2,11)`), và:
     - nhịp 1: `Moving`, `MoveTarget == (12,2,10)`.
     - nhịp 8: `Cell == (12,2,10)`, `Idle`, `WanderAt == 58`.
     - nhịp 58: `Idle`, `Cell == (12,2,10)`, `WanderAt == 108` (ô ruộng không làm đích lang thang).
   - **D `WorkStandsOffOtherPlot`** — `w = Flat(1)`; `Pillar(w, 4, 5, 3)`; `Pillar(w, 5, 4, 3)`; `sim = new Simulation(new SimConfig(), w)`; `AddCreature("L1", (8,2,5))`; `k = AddStorage((1,2,1))`; `AddFarmOrder([(5,1,5), (6,1,5)], k)`; `HomeAll`. `till = sim.State.Stages[sim.State.Plots[0].StageId]`.
     - `RunTo 1`: `MoveTarget == (5,2,6)` (ô kề (6,2,5) là ô ruộng khác → bỏ), `Path.Count == 4`.
     - `RunTo 27`: `Moving`. `RunTo 28`: `Cell == (5,2,6)`, `Working`.
     - `RunTo 87`: `till.Status != Done`. `RunTo 88`: `till.Status == Done`.
   - **E `WorkStandsOnOtherPlotWhenOnlyChoice`** — như D, thêm `Pillar(w, 5, 6, 3)` (chỉ còn ô kề (6,2,5) là ô ruộng của plot 1).
     - `RunTo 1`: `MoveTarget == (6,2,5)`, `Path.Count == 2`.
     - `RunTo 15`: `Cell == (6,2,5)`, `Working`.
     - `RunTo 74`: `till.Status != Done`. `RunTo 75`: `till.Status == Done`.
   - **F `IdleDodgeSkipsPlot`** — như `MovementTests.IdleInPathDodges :173-222` nhưng: cấu hình `TestWorlds.NoWander()`; sau `AddStage` thêm `int k = sim.AddStorage(new CellPos(1, 2, 1)); sim.AddFarmOrder(new List<CellPos> { new CellPos(5, 1, 6) }, k);` (ô ruộng ở (5,2,6) — chỗ né cũ). Giữ vòng lặp, `maxWait`, các mốc; **chỉ** đổi ô né: nhịp 16 `idle.MoveTarget == (5,2,4)`, nhịp 22 `idle.Cell == (5,2,4)`. Các mốc khác giữ nguyên (15, 16 `BlockedTicks == 1`, 22 `BlockedTicks == 7`, 23, 49, 108, 109; `maxWait == 7`).
   - **G `IdleRoamsInsideRadiusOffPlots`** — cấu hình mặc định `new SimConfig()`. `w = Flat(1)`; `sim = new Simulation(new SimConfig(), w)`; `int h = sim.AddHouse((7,2,7))`; `int k = sim.AddStorage((1,2,14))`; `sim.AddFarmOrder([(4,1,4), (5,1,4), (4,1,5)], k)` (không có L1 → xới đình trệ); `AddCreature("L2", (1,2,1))`, `AddCreature("L2", (2,2,1))`; `AssignHome(0, h)`, `AssignHome(1, h)`. `house = sim.State.Houses[h]`. Chạy 3000 nhịp, sau mỗi nhịp với từng con c:
     - `Overlaps(sim.State) == 0`.
     - `c.State == Idle` → `Simulation.HomeDistance(c.Cell, 1, 1, house) <= 4` và `!sim.OnPlot(c.Cell, 1, 1, 1)`.
     - `c.State == Moving` → `HomeDistance(c.MoveTarget, 1, 1, house) <= 4` và `!sim.OnPlot(c.MoveTarget, 1, 1, 1)`.
     - Theo dõi trạng thái nhịp trước: chuyển sang `Idle` từ trạng thái khác → `idleSince[c] = tick`; chuyển `Idle` → `Moving` khi `idleSince[c] >= 0` → `gap = tick − idleSince[c]`, assert `50 <= gap <= 100`, đếm `starts[c]++`, gom `gap` vào một tập. (`idleSince` khởi tạo −1 nên lần đi về nhà ở nhịp 1 không tính.)
     - Sau vòng: `starts[0] >= 10`, `starts[1] >= 10`, tập `gap` có ≥ 2 giá trị khác nhau. Ghi `starts` và số giá trị gap vào `TestContext.WriteLine`.

9. **Map M1** — `Tests/World/SimDemoTests.cs`, thêm test `IdleStaysNearHomeOffPlots`: `NewSim()`; lặp 4000 nhịp: `SimDemo.KeepFarming(sim); sim.Tick(); sim.State.Events.Clear();` rồi với mỗi sinh vật c có `c.State == CreatureState.Idle`:
   - `sim.OnPlot(c.Cell, c.SizeX, c.SizeZ, c.SizeY)` → `idleOnPlot++` (ghi nhịp + id lần đầu bằng `TestContext.WriteLine`);
   - `Simulation.HomeDistance(c.Cell, c.SizeX, c.SizeZ, sim.State.Houses[c.HomeId]) > sim.Config.creature.homeRadius` → `idleOutside++`.
   Đếm thêm `wanderStarts` = số lần một con chuyển từ `Idle` (nhịp trước) sang `Moving` với `StageId == -1`. Assert `idleOnPlot == 0`, `idleOutside == 0`, `wanderStarts > 0`; ghi ba số vào `TestContext.WriteLine` và vào Kết quả. Khác → dừng `cần Opus`.

10. **Chạy toàn bộ test, xử lý test cũ** — theo "Lệnh executor chạy". Test cũ hỏng → làm đúng Quyết định 12 (a → b → c), ghi bảng. Xong rồi commit (chỉ file trong Phạm vi), message `brief 0008: sim 6b rảnh quanh nhà, không dừng trên ruộng`. Đổi Trạng thái → `chờ review`, ghi giờ kết thúc.

## Tiêu chí xong
- EditMode toàn bộ đạt: 204 cũ + 9 mới = **213 / 213** (mới: 1 GridPathfinder, 7 IdleTests, 1 SimDemo).
- PlayMode toàn bộ đạt (8/8).
- `cmp design/data/sim_m1.json client/Assets/Game/Client/Resources/Config/sim_m1.json` exit 0.
- `grep -rnE "float|double|System\.Random|UnityEngine|DateTime|Dictionary|HashSet|PriorityQueue" client/Assets/Game/Core/Sim/` → rỗng.
- `git diff --stat` chỉ có file trong Phạm vi.

## Test sau khi xong

### Phần cần test
- Cấu hình mới · `SimConfigTests` (3 test sẵn có, thêm assert + 4 ca lỗi) + `ConfigTests.SimClientCopyMatchesDesignData` · đạt; `homeRadius` 4, `WanderMinTicks` 50, `WanderMaxTicks` 100.
- BFS trong vùng · `GridPathfinderTests.FloodRegionStaysInRegion` · đạt (9 ô, thứ tự BFS, nguồn ngoài vùng bị bỏ).
- Ngoài bán kính → về ngay; hẹn lại khi không còn ô · `IdleTests.FarIdleWalksBackIntoRadius` · đạt đúng mốc 1/20/21/70/71.
- Lang thang theo hẹn trong bán kính · `IdleTests.WandersOnTimerWithinRadius` · đạt đúng mốc 50/51/57/58/108/115.
- Rảnh trên ô ruộng → rời ngay; ô ruộng không làm đích lang thang · `IdleTests.IdleOnPlotLeavesAtOnce` · đạt.
- Chỗ đứng làm việc bỏ ô ruộng khác · `IdleTests.WorkStandsOffOtherPlot` · đạt (đứng (5,2,6), xong nhịp 88).
- Chỉ còn ô ruộng → vẫn đứng · `IdleTests.WorkStandsOnOtherPlotWhenOnlyChoice` · đạt (đứng (6,2,5), xong nhịp 75).
- Né không vào ô ruộng · `IdleTests.IdleDodgeSkipsPlot` · đạt (né sang (5,2,4), `maxWait == 7`).
- Lang thang ngẫu nhiên 50–100 nhịp, luôn trong bán kính, không dừng trên ruộng, không chồng ô · `IdleTests.IdleRoamsInsideRadiusOffPlots` · đạt; ghi `starts`.
- Map M1 thật, 4000 nhịp `KeepFarming` · `SimDemoTests.IdleStaysNearHomeOffPlots` · `idleOnPlot == 0`, `idleOutside == 0`, `wanderStarts > 0`; ghi ba số.
- Regression — chặn nhau (0007) · `MovementTests` (5), `SimDemoTests.NoTwoCreaturesShareCell` · đạt (0 chồng ô; ghi số nhịp có Phasing).
- Regression — tất định (lang thang bật) · `SimDeterminismTests` (3) · đạt; mã băm có `WanderAt`.
- Regression — mô phỏng cũ · `SimulationTests`, `SimulationTerrainTests`, `FarmTests`, `HomeTests`, `MasteryTests`, `SimDemoTests.OneRoundFillsStorage` / `KeepFarmingContinues` / `BuildPlacesLayout` · đạt; mọi test đổi cấu hình / đổi số theo Quyết định 12 có trong bảng.
- Regression — hiển thị · `Tests/Core/View/*` (EditMode), `SimViewPlayTests` (PlayMode, `SproutCount == 4`) · đạt.

### Lệnh executor chạy
1. `python3 "<Plugin root>/scripts/brief_check.py" 0008 --model <model id>` → exit 0 (trước bước 1 của "Các bước").
2. `cmp design/data/sim_m1.json client/Assets/Game/Client/Resources/Config/sim_m1.json` → exit 0.
3. MCP `refresh_unity` → `read_console` (Error): không có lỗi biên dịch.
4. MCP `run_tests` EditMode — chạy `IdleTests` + `GridPathfinderTests` trước; rồi toàn bộ → ghi dòng tổng kết (mong đợi 213 / 213).
5. MCP `run_tests` PlayMode toàn bộ → ghi dòng tổng kết. **Ngay sau đó:** `git checkout -- client/ProjectSettings/EditorSettings.asset`.
6. `git status --short client/ProjectSettings/`. Còn file đổi → `git checkout --` file đó, ghi tên vào Kết quả.
7. Lệnh `grep` ở Tiêu chí xong → rỗng.
8. `git status --short` + `git diff --stat` trước commit: chỉ file trong Phạm vi.
9. Ghi giờ bắt đầu/kết thúc theo đồng hồ máy (`date +%H:%M`) vào Kết quả, đúng lúc bắt đầu và lúc commit.

Editor tắt thì bước 3–5 dùng `python3 tools/unity_test.py EditMode` / `PlayMode` (không cần bước dọn `EditorSettings.asset` nếu file không đổi).

### User tự test
Sau khi executor xong (đã hẹn từ brief 0007). Mở `Assets/Game/Client/Scenes/SimView.unity`, bấm Play, bấm vào Game view, xem khoảng 2–3 phút:
1. **Rảnh không đứng im mãi:** con nào rảnh (nhãn "… · rảnh") cứ khoảng 5–10 giây lại đi tới chỗ khác quanh nhà của nó (nhãn lúc đi là "… · đi").
2. **Không đi xa nhà:** con rảnh chỉ loanh quanh trong khoảng 4 ô quanh nhà.
3. **Không dừng trên ruộng:** con rảnh có thể đi ngang qua 4 ô ruộng nâu sẫm nhưng không đứng lại trên đó. Con đang làm (xới/gieo/tưới/thu hoạch) đứng cạnh ruộng, không đứng trên ô ruộng khác.
4. **Có việc là đi làm ngay:** con đang lang thang mà có việc mới (vd tới lúc thu hoạch) thì đổi hướng đi làm luôn.
5. **Không chồng nhau** (từ 0007): hai hộp không đứng chung một ô; gặp nhau thì chờ / tránh / né.
Ghi lại điều gì trông lạ (đi giật, đứng chồng lâu, đứng trên ruộng, đi quá xa).

## Dừng và chuyển lại Opus khi
- Brief mơ hồ hoặc mâu thuẫn với code thật.
- Cần một quyết định thuộc AGENTS.md §3.4 hoặc ngoài "Quyết định đã chốt".
- Cùng một lỗi sửa 2 lần vẫn chưa qua.
- Phải sửa file ngoài "Phạm vi".
- Mốc nhịp trong test mới lệch so với brief (không tự sửa số).
- Test cũ hỏng thuộc trường hợp (c) của Quyết định 12.

---
<!-- Từ đây trở xuống: EXECUTOR ghi. -->

## Kết quả
Base commit: 13cab1e
Chạy bằng: claude-sonnet-5-5 · medium
Giờ bắt đầu: 15:10 · kết thúc: 15:17 (đồng hồ máy)
Commit code: bd83724 (brief 0008: sim 6b rảnh quanh nhà, không dừng trên ruộng)
File đổi: Core/Sim/{Simulation,SimData,SimHash,SimConfig,GridPathfinder}.cs; design/data/sim_m1.json + bản chép Resources/Config; Tests/Core/Sim/{TestWorlds,SimConfigTests,GridPathfinderTests,FarmTests,HomeTests,MasteryTests,MovementTests,SimulationTerrainTests,SimulationTests}.cs; Tests/World/SimDemoTests.cs; mới IdleTests.cs(+.meta).
Làm đúng brief, không lệch số nhịp ở test mới (7 IdleTests + FloodRegion đạt ngay lần đầu).

Bảng test cũ hỏng (Quyết định 12):
| test | dòng | cách sửa | nguyên nhân |
|---|---|---|---|
| 23 test trong FarmTests, HomeTests, MasteryTests (WorkTicksFollowTable ×11, SwapMidStage…), MovementTests.IdleInPathDodges, SimulationTerrainTests.DisplacedWorkerKeepsProgress, SimulationTests (4) | mọi `new SimConfig()` trong 6 file | (a) → `TestWorlds.NoWander()` (đổi cả ca không hỏng trong cùng file) | con rảnh giờ lang thang/về nhà (0019 mục 2) |
| FarmTests.OneHarvestReachesStorage | `RunTo 1451/1452` | (b) → `1458/1459` (hauler xong +7 nhịp) | trace: L1 rảnh đứng ở (6,2,5) chắn hauler; Dodge giờ không chọn ô ruộng (5,2,5) nên né sang ô xa hơn (6,2,6)→(7,2,6), hauler tới (6,2,6) lúc 1429 thay vì 1422 (Quyết định 10) |
| FarmTests.DroppedLoadReleasesReservation | `RunTo 1422` | (b) → `1429` | cùng nguyên nhân |

Số đếm: SimDemoTests.IdleStaysNearHomeOffPlots idleOnPlot=0, idleOutside=0, wanderStarts=76; NoTwoCreaturesShareCell: nhịp có Phasing = 0; test G starts = 23, 26 (32 giá trị gap khác nhau).
Dọn: `git checkout -- client/ProjectSettings/EditorSettings.asset` sau PlayMode; `git status client/ProjectSettings/` sạch.
<!-- Base commit (trước khi làm) · Chạy bằng: <model id> · <effort> (dòng OK của brief_check.py) · commit của brief · file đã đổi · giờ bắt đầu/kết thúc · bảng test cũ (Quyết định 12: test · dòng · cách sửa · nguyên nhân) · số đếm của SimDemo (idleOnPlot, idleOutside, wanderStarts, nhịp có Phasing) · starts của test G · ghi chú lệch so với brief (nếu có, kèm lý do). -->

## Kết quả test
Phần cần test: tất cả **đạt** — Cấu hình (SimConfigTests + ConfigTests); FloodRegionStaysInRegion; IdleTests ×7 (FarIdleWalksBackIntoRadius, WandersOnTimerWithinRadius, IdleOnPlotLeavesAtOnce, WorkStandsOffOtherPlot, WorkStandsOnOtherPlotWhenOnlyChoice, IdleDodgeSkipsPlot, IdleRoamsInsideRadiusOffPlots); SimDemoTests.IdleStaysNearHomeOffPlots; regression MovementTests/NoTwoCreaturesShareCell/SimDeterminismTests/mô phỏng cũ/Core/View + SimViewPlayTests đạt. Phần user tự xem: **chờ user** (mục "User tự test").
Lệnh: brief_check → OK (sonnet-5-5 · medium); `cmp` 2 JSON → exit 0; refresh_unity + read_console Error → 0 lỗi; EditMode Idle+GridPathfinder → 18/18; EditMode toàn bộ → 213/213 (sau sửa test cũ; lần đầu 188/213); PlayMode → 8/8; `git status client/ProjectSettings/` → sạch; grep cấm trong Core/Sim → rỗng; `git diff --stat` chỉ file trong Phạm vi.
<!-- Mỗi dòng ở "Phần cần test" → đạt / không đạt / chờ user + bằng chứng (tên test, dòng output).
Mỗi lệnh ở "Lệnh executor chạy": `<lệnh>` → <kết quả thật, dòng tổng kết>. -->

## Vướng mắc
<!-- Khi trạng thái = cần Opus: câu hỏi cụ thể + các phương án (kèm đề xuất). -->

---
<!-- PLANNER ghi khi review. -->

## Review
<!-- Kết luận: đã duyệt | cần sửa (thêm bước sửa vào "Các bước", trạng thái về chờ làm). Điểm đã đối chiếu. -->
