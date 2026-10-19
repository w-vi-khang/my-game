# Brief 0007 — Simulation bước 6b (phần 1): sinh vật chặn nhau khi đi

> Trạng thái: cần Opus
> Review: đầy đủ
> Model thực thi: Sonnet · medium
> Cập nhật: 2026-10-19 12:50

<!-- Planner (Opus) viết, Executor (Sonnet) làm theo ở PHIÊN KHÁC — executor không có lịch sử chat, không được tự quyết.
Trạng thái: chờ làm → đang làm → chờ review → đã duyệt; hoặc → cần Opus (executor dừng, hỏi ở mục Vướng mắc).
Review: nhẹ (đọc mục Kết quả) | đầy đủ (đọc cả diff) | user xem (có phần user tự kiểm bằng mắt/chơi thử) — có thể ghép "đầy đủ + user xem".
Model thực thi: BẮT BUỘC, đúng dạng `<Model> · <effort>` — Model: Opus | Sonnet | Haiku | Fable; effort: low | medium | high | xhigh | max (chọn theo AGENTS.md §10.2).
Executor kiểm tra model + effort của phiên TRƯỚC khi làm (scripts/brief_check.py); lệch hoặc không xác định được → cảnh báo user và DỪNG.
File được commit. Tự kiểm trước khi giao (AGENTS.md §10.3): executor có phải hỏi gì không? còn chỗ nào bỏ ngỏ 2 phương án? -->

## Mục tiêu
Phần 1 của [task 6b](../tasks/m1-sim-buoc-6b-luat-di-lai.md) (quyết định [0019](../../design/decisions/0019-chan-nhau-ranh-quanh-nha-khong-dung-tren-ruong.md) mục 1): hai sinh vật không bao giờ đứng chung ô (trừ lúc đi xuyên — "phao"). Gặp con khác chắn → chờ 10 nhịp rồi tìm đường khác; đối đầu ở lối hẹp → con ưu tiên thấp hơn nhường; kẹt 50 nhịp → đi xuyên; con rảnh chắn đường con đang làm việc → tự né.
**Không làm** ở brief này: rảnh về quanh nhà + lang thang, luật ô ruộng (0019 mục 2–3) → brief 0008 sau khi 0007 được duyệt.

## Bối cảnh cần đọc
- `client/Assets/Game/Core/Sim/Simulation.cs` — toàn file (726 dòng). Chỗ sẽ sửa:
  - `AddCreature :29-43`; `Respawn :198-224`; `Tick :245-265` (thứ tự pha: Terrain → Home → Work → Crop → **Move** → Assign → Stuck → Report — **không đổi**).
  - `TerrainPhase :271-310` (gọi `Repath` cho sinh vật `Moving`); `Displace :317-334`; `Repath :344-359`; `PlanTo :378-393`; `SetPath :395-401`.
  - `MovePhase :551-572`, `OnArrive :574-588`, `AssignPhase :590-677` (điều kiện chọn sinh vật ở `:645`), `Assign :679-700`, `Unassign :702-724`.
  - Bộ đệm dùng chung: `tmpPath :13`, `goals :14`. `FindPath(c, from, to)` `:269`.
- `client/Assets/Game/Core/Sim/SimData.cs` — `Creature :65-106` (thêm trường sau `MoveProgress :83`).
- `client/Assets/Game/Core/Sim/GridPathfinder.cs` — `DirX/DirZ/DirY :17-19`, mảng `g, parent, seenStamp :22-24`, `blocked :26`, `stamp :28`, `IsBodyFree :50-51`, `CanStep :71-80`, `Search :126-181` (mẫu dựng đường từ `parent`, quy tắc "mỗi hướng lấy biến thể Y hợp lệ đầu tiên rồi `break`").
- `client/Assets/Game/Core/Sim/SimHash.cs :32-34` (phần sinh vật, sau `StuckSince`).
- `client/Assets/Game/Core/Sim/SimConfig.cs` — `CreatureConfig :97-101`, `StuckTicks :152`, `Validate :193-256` (dòng cuối kiểm `creature.stuckTimeMs :254-255`).
- `design/data/sim_m1.json` (nguồn chuẩn) + bản chép `client/Assets/Game/Client/Resources/Config/sim_m1.json` (test `Game.World.Tests.ConfigTests` bắt hai bản giống hệt).
- Test: `Tests/Core/Sim/TestWorlds.cs` (`Flat`, `Wall`, `HomeAll`), `SimConfigTests.cs`, `GridPathfinderTests.cs`, `SimulationTerrainTests.cs` (`Build :22`, `DigUnderCreatureStepsAside :108-116`), `HomeTests.cs` (`RunTo :10`, `Hole :20`, `AssertEvent :29`, `StuckIdleRespawnsAtHome :146-179`), `Tests/World/SimDemoTests.cs` (`NewSim :14`, `Run :25`).
- Nhịp tính tay dưới đây dựa trên: L1, L2 đi 150 phần nghìn ô/nhịp (1 ô ≈ 7 nhịp); Till 60 nhịp, Water 30 nhịp; nhịp 1 chỉ giao việc, đi từ nhịp 2 (khớp test cũ `StageDoneAfterTravelPlusWork`: 8 ô → tới nhịp 55, xong nhịp 115).

## Quyết định đã chốt (không được đổi)
1. A\* (`GridPathfinder`) **không** tính sinh vật. Luật chặn chỉ xét lúc sắp bước (0019).
2. **Chỗ chiếm** của sinh vật o = khung tại `o.Cell`, **cộng** khung tại ô kế tiếp `o.Path[o.PathIndex]` nếu `o.State == Moving && o.MoveProgress > 0` (đã bắt đầu bước sang = giữ chỗ). Sinh vật chỉ được cộng tiến độ về ô kế tiếp khi ô đó (theo khung của nó) không chồng lên chỗ chiếm của con nào khác.
3. **Tất định:** xét sinh vật theo thứ tự id; con id nhỏ cập nhật trước, con sau thấy ngay trạng thái mới (giữ chỗ trong nhịp). Đã được user xác nhận 2026-10-19 (đề xuất 4 của 0019).
4. Số nhịp: chờ `blockWaitMs` = 1000 ms (10 nhịp), đi xuyên `phaseAfterMs` = 5000 ms (50 nhịp), trong `sim_m1.json` → mục `creature`.
5. Bị chắn: `BlockedTicks` +1 mỗi nhịp; về 0 ở nhịp nào được cộng tiến độ. Ở nhịp `BlockedTicks` là bội của 10 (10, 20, 30, 40): thử tìm đường khác, coi các con đang chắn ô kế tiếp là vật cản; không được → nếu **đối đầu** (con chắn cũng đang đi và ô kế tiếp của nó chồng lên chỗ chiếm của con này) thì con ưu tiên thấp hơn nhường. Nhịp `BlockedTicks` đạt 50 → `Phasing = true` và bước luôn trong nhịp đó.
6. Ưu tiên = `Priority` của công đoạn đang nhận; sinh vật không có công đoạn = thấp nhất (`int.MinValue`). Bằng nhau → id lớn hơn nhường.
7. Nhường = đi tới ô gần nhất (BFS, tối đa 8 bước) mà khung không chồng lên `Cell` lẫn phần đường còn lại của con kia; đường đi tránh mọi sinh vật khác. Tới ô tránh → lập lại đường tới việc, **đứng chờ** cho tới khi ô kế tiếp của mình không còn nằm trên phần đường còn lại / ô hiện tại của con kia (hoặc con kia hết `Moving`). Không có ô tránh → không nhường (chờ tiếp, tới 50 thì đi xuyên).
8. Con rảnh (`StageId < 0`) chắn con đang có việc → con rảnh **né** ngay nhịp chắn đầu tiên (và thử lại ở mỗi bội của 10): đi tới ô tránh như mục 7, tới nơi thì thành `Idle`. Con rảnh khi né có `State = Moving`, `StageId = -1` (CreatureStatus đã hiện "L1 · đi" cho trường hợp này).
9. Đi xuyên: `Phasing` bỏ qua luật chặn; tắt ở đầu lượt xét của chính nó trong `MovePhase` khi khung tại `Cell` không còn chồng lên `Cell` của con nào. Test "không chung ô" bỏ qua cặp có một con đang `Phasing`.
10. Sinh vật rảnh nhận việc được cả khi đang né (`StageId < 0`, kể cả `Moving`).
11. Giữ bất biến ở các chỗ dời sinh vật khác: `AddCreature` từ chối ô đã có sinh vật; `Displace` không chọn ô bên cạnh đang bị chiếm (rơi xuống mà chồng → bật `Phasing`); `Respawn` chọn ô kề nhà đầu tiên còn trống, hết ô trống → chờ nhịp sau.
12. Test cũ đổi số do luật mới: **chỉ** được sửa con số mong đợi (nhịp, ô) — không xoá assert, không đổi bố trí, không nới điều kiện — và chỉ khi nguyên nhân là chờ / né / nhường / đi xuyên. Ghi bảng ở mục Kết quả (xem bước 10). Lỗi khác (exception, đình trệ mới, sản lượng khác, `Produce`/`Seeds` cuối khác) → dừng, `cần Opus`.

## Phạm vi
- Được sửa/tạo:
  - `client/Assets/Game/Core/Sim/Simulation.cs`, `SimData.cs`, `GridPathfinder.cs`, `SimHash.cs`, `SimConfig.cs`
  - `design/data/sim_m1.json`, `client/Assets/Game/Client/Resources/Config/sim_m1.json`
  - `client/Assets/Game/Tests/Core/Sim/TestWorlds.cs`, `SimConfigTests.cs`, `GridPathfinderTests.cs`, `SimulationTerrainTests.cs`, `HomeTests.cs`
  - Tạo `client/Assets/Game/Tests/Core/Sim/MovementTests.cs` (+ `.meta` do Unity tạo)
  - `client/Assets/Game/Tests/World/SimDemoTests.cs`
  - Test cũ khác trong `client/Assets/Game/Tests/` **chỉ** theo Quyết định 12.
  - Mục Kết quả / Kết quả test / Vướng mắc của brief này.
- Không được sửa: `Client/SimView.cs`, `Core/View/*`, `SimDemo.cs`, `design/` (trừ `sim_m1.json`), task contract, file khác.

## Các bước
Trước khi bắt đầu: đổi Trạng thái → `đang làm`, ghi giờ bắt đầu.

1. **Cấu hình** — `SimConfig.cs`, hai file `sim_m1.json`, `SimConfigTests.cs`.
   - `CreatureConfig :97-101` thêm (sau `stuckTimeMs`):
     ```csharp
     public int blockWaitMs = 1000;
     public int phaseAfterMs = 5000;
     ```
   - Sau `StuckTicks :152` thêm:
     ```csharp
     public int BlockWaitTicks => MsToTicks(creature.blockWaitMs);

     public int PhaseTicks => MsToTicks(creature.phaseAfterMs);
     ```
   - `Validate`, sau kiểm `stuckTimeMs :254-255`:
     ```csharp
     if (creature.blockWaitMs <= 0 || creature.blockWaitMs % msPerTick != 0)
         throw new ArgumentException("creature.blockWaitMs phải > 0 và chia hết cho " + msPerTick + ".");
     if (creature.phaseAfterMs <= creature.blockWaitMs || creature.phaseAfterMs % msPerTick != 0)
         throw new ArgumentException("creature.phaseAfterMs phải > blockWaitMs và chia hết cho " + msPerTick + ".");
     ```
   - `design/data/sim_m1.json` dòng cuối mục `creature` → `"creature": { "stuckTimeMs": 30000, "blockWaitMs": 1000, "phaseAfterMs": 5000 }`. Rồi `cp design/data/sim_m1.json client/Assets/Game/Client/Resources/Config/sim_m1.json`.
   - `SimConfigTests.ConfigParsesAndValidates`: thêm `Assert.AreEqual(10, cfg.BlockWaitTicks);` và `Assert.AreEqual(50, cfg.PhaseTicks);`. `ValidateRejectsBadValues`, sau ca `stuckTimeMs lẻ nhịp`:
     ```csharp
     c = new SimConfig();
     c.creature.blockWaitMs = 1050;
     Assert.Throws<ArgumentException>(c.Validate, "blockWaitMs lẻ nhịp");

     c = new SimConfig();
     c.creature.phaseAfterMs = 1000;
     Assert.Throws<ArgumentException>(c.Validate, "phaseAfterMs <= blockWaitMs");
     ```
   Xong khi: `cmp` hai file JSON exit 0.

2. **Trường mới của sinh vật + mã băm** — `SimData.cs`, `SimHash.cs`.
   - `Creature`, sau `public int MoveProgress;` (`:83`):
     ```csharp
     /// <summary>Số nhịp liên tiếp bị sinh vật khác chắn ô kế tiếp (0019).</summary>
     public int BlockedTicks;
     /// <summary>Đang đi xuyên (phao, 0019): bỏ qua luật chặn tới khi khung không còn chồng lên sinh vật khác.</summary>
     public bool Phasing;
     /// <summary>Đang nhường đường cho sinh vật có id này (đi tới ô tránh hoặc chờ ở đó); -1 = không.</summary>
     public int YieldTo = -1;
     /// <summary>Đã tới ô tránh, chờ con kia đi qua rồi mới đi tiếp.</summary>
     public bool YieldHold;
     ```
   - `SimHash.cs`, ngay sau `h = MixLong(h, c.StuckSince);` (`:34`):
     ```csharp
     h = MixInt(h, c.BlockedTicks);
     h = MixInt(h, c.Phasing ? 1 : 0);
     h = MixInt(h, c.YieldTo);
     h = MixInt(h, c.YieldHold ? 1 : 0);
     ```

3. **Chặn tạm + BFS ô gần nhất** — `GridPathfinder.cs`, `GridPathfinderTests.cs`.
   - Trường mới (cạnh `blocked`): `readonly bool[] tempBlocked;` (khởi tạo `new bool[n]` trong constructor), `readonly List<int> tempList = new List<int>();`, `readonly List<int> queue = new List<int>();`.
   - Thay `IsBodyFree :50-51` bằng:
     ```csharp
     /// <summary>Ô cho thân: trong map, rỗng, không bị công trình hay chặn tạm.</summary>
     public bool IsBodyFree(int x, int y, int z)
     {
         if (!world.Size.InBounds(x, y, z)) return false;
         int i = world.Size.Index(x, y, z);
         return !blocked[i] && !tempBlocked[i] && world.GetMaterial(x, y, z) == BlockMaterial.Empty;
     }
     ```
   - Thêm (sau `SetBlocked`):
     ```csharp
     /// <summary>Chặn tạm ô (x, y, z) cho thân (sinh vật đang chắn, 0019) tới khi ClearTempBlocks; ngoài map thì bỏ qua.</summary>
     public void AddTempBlock(int x, int y, int z)
     {
         if (!world.Size.InBounds(x, y, z)) return;
         int i = world.Size.Index(x, y, z);
         if (tempBlocked[i]) return;
         tempBlocked[i] = true;
         tempList.Add(i);
     }

     public void ClearTempBlocks()
     {
         for (int i = 0; i < tempList.Count; i++) tempBlocked[tempList[i]] = false;
         tempList.Clear();
     }
     ```
   - Thêm (sau `FindPathToAny`):
     ```csharp
     /// <summary>
     /// BFS từ start (thứ tự hướng như A*), tối đa maxSteps bước: đường tới ô đầu tiên lấy ra khỏi hàng đợi thoả accept
     /// (không xét chính start). Đường trong result, không gồm start.
     /// </summary>
     public bool FindPathToNearest(CellPos start, int sx, int sz, int sy, int maxSteps, Predicate<CellPos> accept, List<CellPos> result)
     {
         result.Clear();
         var size = world.Size;
         if (!size.InBounds(start.X, start.Y, start.Z)) return false;
         stamp++;
         queue.Clear();
         int startIdx = size.Index(start.X, start.Y, start.Z);
         g[startIdx] = 0;
         parent[startIdx] = -1;
         seenStamp[startIdx] = stamp;
         queue.Add(startIdx);
         for (int head = 0; head < queue.Count; head++)
         {
             int ci = queue[head];
             size.Coords(ci, out int cx, out int cy, out int cz);
             var cur = new CellPos(cx, cy, cz);
             if (ci != startIdx && accept(cur))
             {
                 for (int i = ci; i != startIdx; i = parent[i])
                 {
                     size.Coords(i, out int px, out int py, out int pz);
                     result.Add(new CellPos(px, py, pz));
                 }
                 result.Reverse();
                 return true;
             }
             if (g[ci] >= maxSteps) continue;
             for (int d = 0; d < 4; d++)
                 for (int k = 0; k < DirY.Length; k++)
                 {
                     var to = new CellPos(cx + DirX[d], cy + DirY[k], cz + DirZ[d]);
                     if (!size.InBounds(to.X, to.Y, to.Z) || !CanStep(cur, to, sx, sz, sy)) continue;
                     int ni = size.Index(to.X, to.Y, to.Z);
                     if (seenStamp[ni] != stamp)
                     {
                         seenStamp[ni] = stamp;
                         g[ni] = g[ci] + 1;
                         parent[ni] = ci;
                         queue.Add(ni);
                     }
                     break;
                 }
         }
         return false;
     }
     ```
     (`Predicate<T>` thuộc `System`, file đã `using System;`.)
   - `GridPathfinderTests` thêm 2 test:
     - `TempBlockBlocksUntilCleared`: `Flat(0)`; `CanStand((3,1,3),1,1,1)` true → `AddTempBlock(3,1,3)` → false → `ClearTempBlocks()` → true.
     - `NearestFollowsBfsOrder`: `Flat(0)`, start (5,1,5), `accept = p => p.X == 7`: `maxSteps` 8 → true, đường đúng `[(6,1,5), (7,1,5)]`; `maxSteps` 1 → false, `result.Count == 0`.

4. **Hàm phụ trong `Simulation`** — thêm vào class (gần `CanStand :267`):
   ```csharp
   /// <summary>Tìm ô tránh / ô né trong tối đa ngần này bước (0019).</summary>
   const int AsideSearchSteps = 8;

   public static bool FramesOverlap(CellPos a, int asx, int asz, int asy, CellPos b, int bsx, int bsz, int bsy) =>
       a.X < b.X + bsx && b.X < a.X + asx && a.Z < b.Z + bsz && b.Z < a.Z + asz && a.Y < b.Y + bsy && b.Y < a.Y + asy;

   /// <summary>Chỗ chiếm của o (khung tại Cell, cộng ô kế tiếp khi đã bắt đầu bước sang) chồng lên khung kích thước (sx, sz, sy) tại p.</summary>
   static bool OccupancyOverlaps(Creature o, CellPos p, int sx, int sz, int sy)
   {
       if (FramesOverlap(o.Cell, o.SizeX, o.SizeZ, o.SizeY, p, sx, sz, sy)) return true;
       return o.State == CreatureState.Moving && o.MoveProgress > 0 && o.PathIndex < o.Path.Count
           && FramesOverlap(o.Path[o.PathIndex], o.SizeX, o.SizeZ, o.SizeY, p, sx, sz, sy);
   }

   /// <summary>Id nhỏ nhất của sinh vật khác c có chỗ chiếm chồng lên khung c tại p; -1 = trống.</summary>
   int FirstBlocker(Creature c, CellPos p)
   {
       var cs = State.Creatures;
       for (int i = 0; i < cs.Count; i++)
           if (i != c.Id && OccupancyOverlaps(cs[i], p, c.SizeX, c.SizeZ, c.SizeY)) return i;
       return -1;
   }

   /// <summary>Khung c tại Cell chồng lên khung tại Cell của sinh vật khác.</summary>
   bool OverlapsAnyCell(Creature c)
   {
       var cs = State.Creatures;
       for (int i = 0; i < cs.Count; i++)
           if (i != c.Id && FramesOverlap(cs[i].Cell, cs[i].SizeX, cs[i].SizeZ, cs[i].SizeY, c.Cell, c.SizeX, c.SizeZ, c.SizeY)) return true;
       return false;
   }

   /// <summary>Khung của y tại p chồng lên w.Cell hoặc phần đường còn lại của w (từ PathIndex).</summary>
   static bool PathTouches(Creature w, CellPos p, Creature y)
   {
       if (FramesOverlap(w.Cell, w.SizeX, w.SizeZ, w.SizeY, p, y.SizeX, y.SizeZ, y.SizeY)) return true;
       for (int i = w.PathIndex; i < w.Path.Count; i++)
           if (FramesOverlap(w.Path[i], w.SizeX, w.SizeZ, w.SizeY, p, y.SizeX, y.SizeZ, y.SizeY)) return true;
       return false;
   }

   void TempBlockFrame(CellPos a, Creature o)
   {
       for (int dx = 0; dx < o.SizeX; dx++)
           for (int dz = 0; dz < o.SizeZ; dz++)
               for (int dy = 0; dy < o.SizeY; dy++)
                   pathfinder.AddTempBlock(a.X + dx, a.Y + dy, a.Z + dz);
   }

   void TempBlockOccupancy(Creature o)
   {
       TempBlockFrame(o.Cell, o);
       if (o.State == CreatureState.Moving && o.MoveProgress > 0 && o.PathIndex < o.Path.Count)
           TempBlockFrame(o.Path[o.PathIndex], o);
   }

   int Rank(Creature c) => c.StageId >= 0 ? State.Stages[c.StageId].Priority : int.MinValue;
   ```
   `AddCreature :29-43`: sau kiểm `CanStand` thêm
   ```csharp
   for (int i = 0; i < State.Creatures.Count; i++)
   {
       var o = State.Creatures[i];
       if (FramesOverlap(o.Cell, o.SizeX, o.SizeZ, o.SizeY, cell, sp.sizeX, sp.sizeZ, sp.sizeY))
           throw new ArgumentException("Ô đã có sinh vật: " + cell);
   }
   ```

5. **MovePhase mới + xử lý bị chắn** — `Simulation.cs`. Thay `MovePhase :551-572` và `OnArrive :574-588` bằng đoạn dưới (thêm các hàm mới ngay sau):
   ```csharp
   void MovePhase()
   {
       var cs = State.Creatures;
       for (int i = 0; i < cs.Count; i++)
       {
           var c = cs[i];
           if (c.Phasing && !OverlapsAnyCell(c)) c.Phasing = false;
           if (c.State != CreatureState.Moving) continue;
           if (c.YieldHold && HoldForYield(c)) continue;
           if (c.PathIndex < c.Path.Count)
           {
               var next = c.Path[c.PathIndex];
               if (!c.Phasing)
               {
                   int b = FirstBlocker(c, next);
                   if (b >= 0)
                   {
                       c.BlockedTicks++;
                       if (c.BlockedTicks < Config.PhaseTicks)
                       {
                           HandleBlocked(c, cs[b], next);
                           continue;
                       }
                       c.Phasing = true;
                   }
               }
               c.BlockedTicks = 0;
               c.MoveProgress += c.SpeedPerTick;
               if (c.MoveProgress >= 1000)
               {
                   c.Cell = next;
                   c.PathIndex++;
                   c.MoveProgress -= 1000;
                   // Phần dư chỉ được giữ (= giữ chỗ ô kế tiếp) khi ô đó trống.
                   if (c.MoveProgress > 0 && c.PathIndex < c.Path.Count && !c.Phasing && FirstBlocker(c, c.Path[c.PathIndex]) >= 0)
                       c.MoveProgress = 0;
               }
           }
           if (c.PathIndex >= c.Path.Count)
           {
               c.MoveProgress = 0;
               c.Path.Clear();
               c.PathIndex = 0;
               if (c.YieldTo >= 0 && !c.YieldHold) ResumeAfterYield(c);
               else OnArrive(c);
           }
       }
   }

   /// <summary>c bị o chắn ô next; BlockedTicks đã tăng và còn dưới PhaseTicks (0019).</summary>
   void HandleBlocked(Creature c, Creature o, CellPos next)
   {
       int wait = Config.BlockWaitTicks;
       if (c.StageId >= 0 && o.StageId < 0 && (o.State == CreatureState.Idle || PathTouches(c, o.MoveTarget, o)))
       {
           if ((c.BlockedTicks == 1 || c.BlockedTicks % wait == 0) && Dodge(o, c)) return;
       }
       if (c.BlockedTicks % wait != 0) return;
       if (RepathAvoiding(c, next)) return;
       bool headOn = o.State == CreatureState.Moving && o.PathIndex < o.Path.Count
           && OccupancyOverlaps(c, o.Path[o.PathIndex], o.SizeX, o.SizeZ, o.SizeY);
       if (!headOn) return;
       int rc = Rank(c), ro = Rank(o);
       var y = rc < ro || rc == ro && c.Id > o.Id ? c : o;
       StartYield(y, y == c ? o : c);
   }

   /// <summary>Tìm đường khác, coi các sinh vật đang chiếm ô next là vật cản. Thất bại → giữ đường cũ.</summary>
   bool RepathAvoiding(Creature c, CellPos next)
   {
       var cs = State.Creatures;
       for (int i = 0; i < cs.Count; i++)
           if (i != c.Id && OccupancyOverlaps(cs[i], next, c.SizeX, c.SizeZ, c.SizeY)) TempBlockOccupancy(cs[i]);
       bool ok;
       if (c.StageId >= 0 && c.YieldTo < 0)
       {
           var s = State.Stages[c.StageId];
           ok = s.HasDestination && s.Carried
               ? PlanTo(c, s.Destination, s.DestinationApproach)
               : PlanTo(c, s.Target, s.TargetApproach);
       }
       else
       {
           ok = FindPath(c, c.Cell, c.MoveTarget);
       }
       pathfinder.ClearTempBlocks();
       if (!ok) return false;
       SetPath(c);
       c.MoveProgress = 0;
       c.BlockedTicks = 0;
       return true;
   }

   /// <summary>Đường cho y tới ô gần nhất ngoài Cell + phần đường còn lại của w, tránh mọi sinh vật khác — vào tmpPath.</summary>
   bool FindAside(Creature y, Creature w)
   {
       var cs = State.Creatures;
       for (int i = 0; i < cs.Count; i++)
           if (i != y.Id) TempBlockOccupancy(cs[i]);
       bool ok = pathfinder.FindPathToNearest(y.Cell, y.SizeX, y.SizeZ, y.SizeY, AsideSearchSteps, p => !PathTouches(w, p, y), tmpPath);
       pathfinder.ClearTempBlocks();
       return ok;
   }

   /// <summary>Sinh vật rảnh o né khỏi đường của w; tới nơi thì Idle (OnArrive).</summary>
   bool Dodge(Creature o, Creature w)
   {
       if (!FindAside(o, w)) return false;
       o.State = CreatureState.Moving;
       SetPath(o);
       o.MoveProgress = 0;
       o.BlockedTicks = 0;
       return true;
   }

   /// <summary>y nhường đường cho w (đối đầu, 0019).</summary>
   void StartYield(Creature y, Creature w)
   {
       if (y.StageId < 0)
       {
           Dodge(y, w);
           return;
       }
       if (y.YieldTo == w.Id && !y.YieldHold) return;
       if (!FindAside(y, w)) return;
       SetPath(y);
       y.MoveProgress = 0;
       y.BlockedTicks = 0;
       y.YieldTo = w.Id;
       y.YieldHold = false;
   }

   /// <summary>Tới ô tránh: lập lại đường tới việc rồi chờ con kia đi qua.</summary>
   void ResumeAfterYield(Creature c)
   {
       int w = c.YieldTo;
       Repath(c);
       if (c.State != CreatureState.Moving) return;
       if (c.Path.Count == 0)
       {
           OnArrive(c);
           return;
       }
       c.YieldTo = w;
       c.YieldHold = true;
   }

   /// <summary>true = còn phải chờ: ô kế tiếp nằm trên Cell / phần đường còn lại của con được nhường mà nó vẫn đang đi.</summary>
   bool HoldForYield(Creature c)
   {
       var w = State.Creatures[c.YieldTo];
       if (c.PathIndex < c.Path.Count && w.State == CreatureState.Moving && PathTouches(w, c.Path[c.PathIndex], c)) return true;
       c.YieldTo = -1;
       c.YieldHold = false;
       return false;
   }

   void OnArrive(Creature c)
   {
       c.YieldTo = -1;
       c.YieldHold = false;
       if (c.StageId < 0)
       {
           c.State = CreatureState.Idle;
           return;
       }
       var s = State.Stages[c.StageId];
       if (s.HasDestination && !s.Carried)
       {
           s.Carried = true;
           if (!PlanTo(c, s.Destination, s.DestinationApproach)) BlockNoPath(c);
           else if (tmpPath.Count == 0) c.State = CreatureState.Working;
           else SetPath(c);
       }
       else
       {
           c.State = CreatureState.Working;
       }
   }
   ```

6. **Các chỗ khác trong `Simulation.cs`**:
   - `Repath :344-359`: thêm ở đầu hàm
     ```csharp
     c.YieldTo = -1;
     c.YieldHold = false;
     c.BlockedTicks = 0;
     if (c.StageId < 0)
     {
         StopWalk(c);
         return;
     }
     ```
     và hàm mới:
     ```csharp
     /// <summary>Sinh vật rảnh đang né mà đường hỏng (địa hình đổi) → đứng lại.</summary>
     void StopWalk(Creature c)
     {
         c.State = CreatureState.Idle;
         c.Path.Clear();
         c.PathIndex = 0;
         c.MoveProgress = 0;
         c.MoveTarget = c.Cell;
     }
     ```
   - `AssignPhase :645`: đổi `c.State != CreatureState.Idle` thành `c.StageId >= 0` (dòng thành `if (c.HomeId < 0 || c.StageId >= 0 || !c.HasAbility(s.Ability)) continue;`).
   - `Assign :679-700` và `Unassign :702-724`: thêm `c.BlockedTicks = 0; c.YieldTo = -1; c.YieldHold = false;` (trong `Assign` đặt ngay trước `c.MoveProgress = 0;`; trong `Unassign` đặt ngay trước `c.MoveProgress = 0;`).
   - `Displace :317-334`: điều kiện trong vòng lặp thành `if (pathfinder.CanStep(c.Cell, n, c.SizeX, c.SizeZ, c.SizeY) && FirstBlocker(c, n) < 0)`. Sau vòng `while` rơi xuống, trước `c.MoveProgress = 0;` cuối hàm: `if (OverlapsAnyCell(c)) c.Phasing = true;`.
   - `Respawn :198-224`: thay `if (goals.Count == 0) return; var cell = goals[0];` bằng
     ```csharp
     int pick = -1;
     for (int i = 0; i < goals.Count; i++)
         if (FirstBlocker(c, goals[i]) < 0)
         {
             pick = i;
             break;
         }
     if (pick < 0) return; // mọi ô kề nhà đang có sinh vật → thử lại nhịp sau
     var cell = goals[pick];
     ```
     và sau `c.HomeReachable = true;` thêm `c.BlockedTicks = 0; c.Phasing = false; c.YieldTo = -1; c.YieldHold = false;`.
   Xong khi: MCP `refresh_unity` → `read_console` (Error) không có lỗi biên dịch.

7. **Bản đồ test** — `TestWorlds.cs`, thêm:
   ```csharp
   /// <summary>Cột Stone tại (x, z), y từ 1..m.</summary>
   public static void Pillar(WorldData w, int x, int z, int m)
   {
       for (int y = 1; y <= m; y++) w.Set(x, y, z, Cell.Make(BlockMaterial.Stone));
   }

   /// <summary>
   /// Dùng với Flat(1): hành lang 1 ô dọc trục X tại z = zc, ô đứng y = 2, x từ x0..x1. Hai bên (z = zc ± 1) và hai đầu
   /// (x0 − 1, x1 + 1) là cột Stone y 1..3 (không leo được). pocketX ≥ 0: thêm ô tránh (pocketX, 2, zc + 1), chặn ở (pocketX, zc + 2).
   /// </summary>
   public static void Corridor(WorldData w, int x0, int x1, int zc, int pocketX = -1)
   {
       for (int x = x0 - 1; x <= x1 + 1; x++)
       {
           if (x != pocketX) Pillar(w, x, zc + 1, 3);
           Pillar(w, x, zc - 1, 3);
       }
       Pillar(w, x0 - 1, zc, 3);
       Pillar(w, x1 + 1, zc, 3);
       if (pocketX >= 0) Pillar(w, pocketX, zc + 2, 3);
   }
   ```

8. **Test mới** — tạo `Tests/Core/Sim/MovementTests.cs` (namespace `Game.Core.Tests`, class `MovementTests`, `using System; using System.Collections.Generic; using Game.Core.Sim; using NUnit.Framework;`). Hàm phụ:
   ```csharp
   static int NewOrder(Simulation sim) => sim.AddOrder(new List<CellPos>());

   /// <summary>Số cặp sinh vật có khung (tại Cell) chồng nhau, bỏ qua cặp có con đang Phasing.</summary>
   static int Overlaps(SimState s)
   {
       int n = 0;
       var cs = s.Creatures;
       for (int i = 0; i < cs.Count; i++)
           for (int j = i + 1; j < cs.Count; j++)
           {
               var a = cs[i];
               var b = cs[j];
               if (a.Phasing || b.Phasing) continue;
               if (Simulation.FramesOverlap(a.Cell, a.SizeX, a.SizeZ, a.SizeY, b.Cell, b.SizeX, b.SizeZ, b.SizeY)) n++;
           }
       return n;
   }

   /// <summary>Tick tới nhịp tick, sau mỗi nhịp assert không chồng ô.</summary>
   static void RunTo(Simulation sim, long tick)
   {
       while (sim.State.Tick < tick)
       {
           sim.Tick();
           Assert.AreEqual(0, Overlaps(sim.State), "chồng ô ở nhịp " + sim.State.Tick);
       }
   }

   /// <summary>Hành lang x 1..11, z = 5 (Flat(1)); A = L1 (id 0) ở (1,2,5) xới tại (11,2,5); B = L2 (id 1) ở (11,2,5) tưới tại (1,2,5).</summary>
   static Simulation HeadOn(int pocketX)
   {
       var w = TestWorlds.Flat(1);
       TestWorlds.Corridor(w, 1, 11, 5, pocketX);
       var sim = new Simulation(new SimConfig(), w);
       sim.AddCreature("L1", new CellPos(1, 2, 5));
       sim.AddCreature("L2", new CellPos(11, 2, 5));
       int o = NewOrder(sim);
       sim.AddStage(o, WorkAbility.Till, new CellPos(11, 2, 5));
       sim.AddStage(o, WorkAbility.Water, new CellPos(1, 2, 5));
       TestWorlds.HomeAll(sim);
       return sim;
   }
   ```
   Các test (số nhịp đã tính tay theo đúng code ở bước 5 — **sai khác thì không sửa số, dừng `cần Opus`** kèm vị trí A/B, `BlockedTicks`, `YieldTo` quanh nhịp lệch):
   Trong hai test `HeadOn…`: A = `sim.State.Creatures[0]`, B = `Creatures[1]`; "stage 0 / stage 1 Done" = `sim.State.Stages[0 / 1].Status == StageStatus.Done`.
   - `HeadOnWithPocketYields` — `HeadOn(8)`:
     - `RunTo(sim, 37)`: `B.YieldTo == -1`, `B.BlockedTicks == 9`, `A.BlockedTicks == 2`.
     - `RunTo(sim, 38)`: A.Cell (6,2,5), B.Cell (7,2,5), `B.YieldTo == 0`, `B.MoveTarget == (8,2,6)`, `B.BlockedTicks == 0`.
     - `RunTo(sim, 52)`: B.Cell (8,2,6), `B.YieldHold == true`, A.Cell (7,2,5).
     - `RunTo(sim, 65)`: `B.YieldHold == true`. `RunTo(sim, 66)`: `B.YieldTo == -1`, `B.YieldHold == false`, A.Cell (9,2,5).
     - `RunTo(sim, 138)`: stage 0 chưa Done. `RunTo(sim, 139)`: stage 0 Done, A.Cell (11,2,5).
     - `RunTo(sim, 148)`: stage 1 chưa Done. `RunTo(sim, 149)`: stage 1 Done, B.Cell (1,2,5).
     - Không nhịp nào có `Phasing` (assert `!A.Phasing && !B.Phasing` ở các mốc trên).
   - `HeadOnNoPocketPhasesAtFifty` — `HeadOn(-1)`:
     - `RunTo(sim, 77)`: `B.BlockedTicks == 49`, `!B.Phasing`, `A.BlockedTicks == 42`.
     - `RunTo(sim, 78)`: `B.Phasing == true`, `!A.Phasing`.
     - `RunTo(sim, 84)`: B.Cell (6,2,5) = A.Cell (chồng nhau, hợp lệ vì B đang Phasing), `A.BlockedTicks == 49`, `!A.Phasing`.
     - `RunTo(sim, 91)`: `!B.Phasing`, A.Cell (7,2,5), B.Cell (5,2,5).
     - `RunTo(sim, 146)`: stage 1 chưa Done. `RunTo(sim, 147)`: stage 1 Done, B.Cell (1,2,5). `RunTo(sim, 177)`: stage 0 chưa Done. `RunTo(sim, 178)`: stage 0 Done, A.Cell (11,2,5).
   - `WorkerBlockingWaitsTenThenDetours` — `Flat(1)`, không hành lang. M = L1 (id 0) ở (2,2,5), W = L2 (id 1) ở (5,2,5). `s0 = AddStage(o, Till, (8,2,5))`, `s1 = AddStage(o, Water, (5,2,5))`; `HomeAll`.
     - `RunTo(sim, 1)`: W `Working`. `RunTo(sim, 15)`: M.Cell (4,2,5).
     - `RunTo(sim, 24)`: M.Cell (4,2,5), `M.BlockedTicks == 9`, `M.Path[M.PathIndex] == (5,2,5)`.
     - `RunTo(sim, 25)`: `M.BlockedTicks == 0`, `M.PathIndex == 0`, đường mới đúng `[(4,2,6), (5,2,6), (6,2,6), (7,2,6), (8,2,6), (8,2,5)]`, W vẫn `Working`.
     - `RunTo(sim, 64)`: M.Cell (8,2,6), M `Moving`. `RunTo(sim, 65)`: M.Cell (8,2,5), M `Working`. `RunTo(sim, 124)`: `Stages[s0]` chưa Done. `RunTo(sim, 125)`: `Stages[s0]` Done.
   - `IdleInPathDodges` — `Flat(1)`. M = L1 (id 0) ở (2,2,5), I = L2 (id 1) ở (5,2,5), chỉ có `s0 = AddStage(o, Till, (8,2,5))`; `HomeAll`. Test này tick bằng vòng lặp riêng (mỗi nhịp: `sim.Tick()`, assert `Overlaps == 0`, `maxWait = Math.Max(maxWait, M.BlockedTicks)`), kiểm các mốc khi `State.Tick` bằng:
     - 15: M.Cell (4,2,5), I `Idle`.
     - 16: I `Moving`, `I.StageId == -1`, `I.MoveTarget == (5,2,6)`, `M.BlockedTicks == 1`.
     - 22: I.Cell (5,2,6), I `Idle`, `M.BlockedTicks == 7`.
     - 23: `M.BlockedTicks == 0`.
     - 49: M.Cell (8,2,5), M `Working`. 108: `Stages[s0]` chưa Done. 109: `Stages[s0]` Done. Dừng vòng ở 109; `maxWait == 7` (≤ 10).
   - `AddCreatureRejectsOverlap` — `Flat(0)`: `AddCreature("L1", (3,1,3))` → `AddCreature("L2", (3,1,3))` ném `ArgumentException`; `AddCreature("L2", (4,1,3))` không ném.

   Thêm vào file cũ:
   - `SimulationTerrainTests` — `DigUnderCreatureAvoidsOccupiedNeighbor`: như `DigUnderCreatureStepsAside :108-116` nhưng trước khi đào thêm `int o = sim.AddCreature("L2", new CellPos(6, 2, 5));`. Sau 1 nhịp: sinh vật đào dưới chân ở (4,2,5), o vẫn ở (6,2,5).
   - `HomeTests` — `RespawnSkipsOccupiedHomeCell`: như `StuckIdleRespawnsAtHome :146-179` (giữ `Hole`, nhà (10,2,10), L1 (3,2,3), L2 (6,2,6), `AssignHome` cả hai) và thêm `sim.AddCreature("L3", new CellPos(12, 2, 10));` (không gắn nhà, đứng yên). `RunTo(sim, 301)`: L1.Cell (12,2,11) (ô kề nhà thứ hai, vì (12,2,10) có L3), có đúng 1 sự kiện `CreatureRespawned` nhịp 301, Cell (12,2,11).

9. **Map M1** — `Tests/World/SimDemoTests.cs`, test `NoTwoCreaturesShareCell`: `NewSim()`; lặp 4000 nhịp: `SimDemo.KeepFarming(sim); sim.Tick(); sim.State.Events.Clear();` rồi đếm cặp chồng khung như hàm `Overlaps` ở bước 8 (chép hàm vào class này, dùng `Simulation.FramesOverlap`) và đếm số nhịp có ít nhất một con `Phasing`. Assert tổng số lần chồng = 0; ghi số nhịp có `Phasing` vào thông điệp (`TestContext.WriteLine`) và vào Kết quả. Khác 0 → dừng `cần Opus`, ghi nhịp đầu tiên, id, ô, `State`, `StageId` của hai con.

10. **Chạy toàn bộ test, xử lý test cũ** — theo "Lệnh executor chạy". Test cũ fail:
    - Đúng Quyết định 12 (chỉ lệch nhịp/ô vì chờ/né/nhường/đi xuyên) → sửa con số, ghi bảng vào Kết quả: `test · dòng · cũ → mới · nguyên nhân` (con nào chắn con nào, nhịp mấy, né/nhường tới ô nào — đọc bằng cách in `Cell`, `BlockedTicks`, `YieldTo` quanh nhịp đó).
    - Khác → dừng, `cần Opus`.
    Xong rồi commit (chỉ file trong Phạm vi), message `brief 0007: sim 6b chặn nhau khi đi`. Đổi Trạng thái → `chờ review`, ghi giờ kết thúc.

## Tiêu chí xong
- EditMode toàn bộ đạt: 194 cũ + 10 mới = **204 / 204** (mới: 2 GridPathfinder, 5 MovementTests, 1 SimulationTerrain, 1 Home, 1 SimDemo).
- PlayMode toàn bộ đạt (8/8).
- `cmp design/data/sim_m1.json client/Assets/Game/Client/Resources/Config/sim_m1.json` exit 0.
- `grep -rnE "float|double|System\.Random|UnityEngine|DateTime|Dictionary|HashSet|PriorityQueue" client/Assets/Game/Core/Sim/` → rỗng.
- `git diff --stat` chỉ có file trong Phạm vi.

## Test sau khi xong

### Phần cần test
- Cấu hình mới · `SimConfigTests` (3 test sẵn có, đã thêm assert/ca lỗi) + `ConfigTests.SimClientCopyMatchesDesignData` · đạt; `BlockWaitTicks` 10, `PhaseTicks` 50; hai ca lỗi ném `ArgumentException`.
- Chặn tạm + BFS · `GridPathfinderTests.TempBlockBlocksUntilCleared`, `NearestFollowsBfsOrder` · đạt.
- Đối đầu có ô tránh → nhường, chờ, đi tiếp · `MovementTests.HeadOnWithPocketYields` · đạt đúng các mốc nhịp, không chồng ô, không Phasing.
- Đối đầu không ô tránh → đi xuyên đúng nhịp 50 · `MovementTests.HeadOnNoPocketPhasesAtFifty` · đạt; chỉ chồng ô khi đang Phasing; cả hai tới đích.
- Con đang làm chắn → chờ đúng 10 nhịp rồi đi vòng · `MovementTests.WorkerBlockingWaitsTenThenDetours` · đạt.
- Con rảnh chắn → tự né, con đang đi chờ ≤ 10 nhịp · `MovementTests.IdleInPathDodges` · đạt, `maxWait == 7`.
- Không thêm sinh vật chồng ô · `MovementTests.AddCreatureRejectsOverlap` · đạt.
- Đào dưới chân không dời vào ô có sinh vật · `SimulationTerrainTests.DigUnderCreatureAvoidsOccupiedNeighbor` · đạt.
- Hiện lại ở nhà bỏ qua ô có sinh vật · `HomeTests.RespawnSkipsOccupiedHomeCell` · đạt.
- Map M1 thật, 4000 nhịp `KeepFarming` · `SimDemoTests.NoTwoCreaturesShareCell` · 0 lần chồng; ghi số nhịp có Phasing.
- Regression — tất định: `SimDeterminismTests` (3 test: cùng mã băm 2000 / 4000 nhịp, mã đổi khi trạng thái đổi) · đạt (mã băm có thêm 4 trường mới).
- Regression — mô phỏng cũ: `SimulationTests`, `SimulationTerrainTests`, `FarmTests`, `HomeTests`, `MasteryTests`, `SimDemoTests.OneRoundFillsStorage` / `KeepFarmingContinues` (0 đình trệ, đủ sản lượng) · đạt; số đã sửa theo Quyết định 12 liệt kê trong bảng.
- Regression — hiển thị: `Tests/Core/View/*` (EditMode), `SimViewPlayTests` (PlayMode, có `SproutCount == 4`) · đạt.

### Lệnh executor chạy
1. `python3 "<Plugin root>/scripts/brief_check.py" 0007 --model <model id>` → exit 0 (trước bước 1 của "Các bước").
2. `cmp design/data/sim_m1.json client/Assets/Game/Client/Resources/Config/sim_m1.json` → exit 0.
3. MCP `refresh_unity` → `read_console` (Error): không có lỗi biên dịch.
4. MCP `run_tests` EditMode toàn bộ → ghi dòng tổng kết (mong đợi 204 / 204). Khi đang sửa có thể chạy riêng `MovementTests` trước cho nhanh.
5. MCP `run_tests` PlayMode toàn bộ → ghi dòng tổng kết. **Ngay sau đó:** `git checkout -- client/ProjectSettings/EditorSettings.asset`.
6. `git status --short client/ProjectSettings/`. Còn file đổi → `git checkout --` file đó, ghi tên vào Kết quả.
7. Lệnh `grep` ở Tiêu chí xong → rỗng.
8. `git status --short` + `git diff --stat` trước commit: chỉ file trong Phạm vi.
9. Ghi giờ bắt đầu/kết thúc (đồng hồ máy) vào Kết quả.

Editor tắt thì bước 3–5 dùng `python3 tools/unity_test.py EditMode` / `PlayMode` (không cần bước dọn `EditorSettings.asset` nếu file không đổi).

### User tự test
Không (xem hình trong SimView để ở brief 0008, khi có đủ luật rảnh + ô ruộng).

## Dừng và chuyển lại Opus khi
- Brief mơ hồ hoặc mâu thuẫn với code thật.
- Cần một quyết định thuộc AGENTS.md §3.4 hoặc ngoài "Quyết định đã chốt".
- Cùng một lỗi sửa 2 lần vẫn chưa qua.
- Phải sửa file ngoài "Phạm vi".
- Mốc nhịp trong test mới lệch so với brief (không tự sửa số).
- Test cũ fail ngoài trường hợp của Quyết định 12.

---
<!-- Từ đây trở xuống: EXECUTOR ghi. -->

## Kết quả
Base commit: af6ea06
Chạy bằng: Sonnet · medium (OK: brief 0007 cần Sonnet · medium — phiên: claude-sonnet-5-5 · medium)
Giờ bắt đầu: 12:31

<!-- Base commit (trước khi làm) · Chạy bằng: <model id> · <effort> (dòng OK của brief_check.py) · commit của brief · file đã đổi · giờ bắt đầu/kết thúc · bảng test cũ đổi số (Quyết định 12) · số nhịp có Phasing trên map M1 · ghi chú lệch so với brief (nếu có, kèm lý do). -->

## Kết quả test
<!-- Mỗi dòng ở "Phần cần test" → đạt / không đạt + bằng chứng (tên test, dòng output).
Mỗi lệnh ở "Lệnh executor chạy": `<lệnh>` → <kết quả thật, dòng tổng kết>. -->

## Vướng mắc
Code bước 1–9 đã viết đủ (chưa commit, còn trong working tree); biên dịch sạch. EditMode: 200/204 đạt. 4 fail:
- `MovementTests.HeadOnNoPocketPhasesAtFifty` (mốc nhịp 84 lệch — dừng theo brief): kỳ vọng B.Cell = (6,2,5) = A.Cell, thực tế A (6,2,5), B (7,2,5) (B chỉ có MoveProgress 150, chưa vào ô A).
- `FarmTests.DroppedLoadReleasesReservation` (:265), `OneHarvestReachesStorage` (:109), `QueueRepeatsAndCapsAtTwo` (:197, 9 → 8): chưa phân tích (có thể cùng nguyên nhân dưới đây; chưa đụng vào vì chưa rõ có thuộc Quyết định 12 không).

Nguyên nhân đã thấy trong trace (HeadOn(-1)): nhịp 78 B bật `Phasing` và cộng 150 (B.Cell vẫn (7,2,5), A.Cell (6,2,5), không chồng khung). Nhịp 79 đầu lượt B: `Phasing && !OverlapsAnyCell(B)` → tắt `Phasing` ngay (Quyết định 9 viết đúng như vậy), B lại bị A chắn (A ở (6,2,5) chiếm ô B muốn vào), `BlockedTicks` đếm lại từ 1, MoveProgress 150 giữ nguyên. Tới nhịp 85 A tới 50 → A cũng `Phasing` + cộng 150, rồi cả hai lại tắt Phasing và đếm lại. Kết quả: **bế tắc vĩnh viễn** (tới nhịp 95 vẫn A(6,2,5)/B(7,2,5), bt tăng dần rồi lặp), không ai đi xuyên được. Tức Quyết định 9 ("tắt khi khung tại Cell không chồng") mâu thuẫn với mục đích đi xuyên: Phasing phải sống tới khi B đã **đi vào rồi ra khỏi** ô chồng.
Phương án: (1) Phasing chỉ tắt khi đã từng chồng ô (thêm cờ `PhaseOverlapped`: bật khi `OverlapsAnyCell`, tắt Phasing khi cờ bật mà không còn chồng) — đề xuất; (2) Phasing tắt khi con đi hết `Path` hiện tại / tới đích; (3) Phasing tắt sau N nhịp. Phương án 1 giữ đúng ý 0019. Nếu chọn 1, mốc nhịp ở HeadOnNoPocket (84, 91, 146–178) sẽ khác — cần Opus tính lại; có thể ảnh hưởng 3 test FarmTests.
Thêm ghi chú: `GridPathfinder.AddTempBlock/IsBodyFree` & các bước khác chạy đúng theo brief; các test MovementTests khác (HeadOnWithPocketYields, WorkerBlocking…, IdleInPathDodges, AddCreatureRejectsOverlap), 2 GridPathfinder, SimConfig, Terrain, Home đều đạt.
<!-- Khi trạng thái = cần Opus: câu hỏi cụ thể + các phương án (kèm đề xuất). -->

---
<!-- PLANNER ghi khi review. -->

## Review

## Kiểm chứng
Dừng giữa chừng (`cần Opus`), đang chờ Planner quyết định về `Phasing`; chưa kiểm chứng đầy đủ.
- EditMode (MCP, Editor đang mở): 200/204 đạt, 4 fail (xem Vướng mắc). Biên dịch: 0 lỗi.
- `cmp` hai file `sim_m1.json`: exit 0.
- PlayMode, `check_design.py`, grep Game.Core, checklist: chưa chạy / chưa đối chiếu — chờ Planner.
