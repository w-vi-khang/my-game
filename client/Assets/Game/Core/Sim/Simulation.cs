using System;
using System.Collections.Generic;

namespace Game.Core.Sim
{
    /// <summary>Mô phỏng nông trại M1 — nhịp cố định, số nguyên, tất định (simulation-tech bước 1, 0014). A* đi bộ theo khung chiếm chỗ (bước 2, 0015).</summary>
    public sealed class Simulation
    {
        public readonly SimConfig Config;
        public readonly SimState State;
        public readonly WorldData World;
        readonly GridPathfinder pathfinder;
        readonly List<CellPos> tmpPath = new List<CellPos>();
        readonly List<CellPos> goals = new List<CellPos>();
        readonly List<CellPos> homePath = new List<CellPos>();
        readonly List<CellPos> wanderCells = new List<CellPos>();
        bool terrainDirty;
        bool homeDirty;

        public Simulation(SimConfig config, WorldData world)
            : this(config, world, world != null ? SimBounds.Whole(world.Size) : new SimBounds(0, 0, 1, 1))
        {
        }

        /// <summary>Mô phỏng trong vùng (world 7e, brief 0019): sinh vật chỉ tìm đường trong bounds.</summary>
        public Simulation(SimConfig config, WorldData world, SimBounds bounds)
        {
            if (world == null) throw new ArgumentNullException(nameof(world));
            config.Validate();
            Config = config;
            World = world;
            pathfinder = new GridPathfinder(world, bounds);
            State = new SimState(new SimRng(config.simSeed, 1UL));
        }

        public SimBounds Bounds => pathfinder.Bounds;

        /// <summary>Số ô mảng tìm đường — đo bộ nhớ (brief 0019).</summary>
        public int PathfinderCellCount => pathfinder.CellCount;

        public int AddCreature(string speciesId, CellPos cell)
        {
            var sp = Config.FindSpecies(speciesId);
            if (!pathfinder.CanStand(cell, sp.sizeX, sp.sizeZ, sp.sizeY))
                throw new ArgumentException("Sinh vật không đứng được tại " + cell);
            for (int i = 0; i < State.Creatures.Count; i++)
            {
                var o = State.Creatures[i];
                if (FramesOverlap(o.Cell, o.SizeX, o.SizeZ, o.SizeY, cell, sp.sizeX, sp.sizeZ, sp.sizeY))
                    throw new ArgumentException("Ô đã có sinh vật: " + cell);
            }
            var c = new Creature(State.Creatures.Count, speciesId,
                SimConfig.ParseAbility(sp.ability1), SimConfig.ParseAbility(sp.ability2),
                sp.mastery1, sp.mastery2, Config.SpeedPerTick(sp), sp.sizeX, sp.sizeZ, sp.sizeY, cell)
            {
                State = CreatureState.Idle,
                MoveTarget = cell,
            };
            State.Creatures.Add(c);
            return c.Id;
        }

        public int AddOrder(IReadOnlyList<CellPos> area)
        {
            var list = new List<CellPos>(area.Count);
            for (int i = 0; i < area.Count; i++) list.Add(area[i]);
            var o = new WorkOrder(State.Orders.Count, list);
            State.Orders.Add(o);
            return o.Id;
        }

        public int AddStage(int orderId, WorkAbility ability, CellPos target, int priority = 0)
        {
            if (ability == WorkAbility.Haul)
                throw new ArgumentException("Vận chuyển dùng AddHaulStage.");
            var s = new WorkStage(State.Stages.Count, orderId, ability, target, false, default, Config.BaseTicks(ability), priority);
            State.Stages.Add(s);
            State.Orders[orderId].StageIds.Add(s.Id);
            return s.Id;
        }

        public int AddHaulStage(int orderId, CellPos pickup, CellPos destination, int priority = 0)
        {
            var s = new WorkStage(State.Stages.Count, orderId, WorkAbility.Haul, pickup, true, destination, Config.BaseTicks(WorkAbility.Haul), priority);
            State.Stages.Add(s);
            State.Orders[orderId].StageIds.Add(s.Id);
            return s.Id;
        }

        public int AddStorage(CellPos cell)
        {
            if (!pathfinder.CanStand(cell, 1, 1, 1))
                throw new ArgumentException("Kho không đặt được tại " + cell);
            if (IsOccupied(cell.X, cell.Y, cell.Z))
                throw new ArgumentException("Ô đã bị chiếm: " + cell);
            pathfinder.SetBlocked(cell, true);
            homeDirty = true;
            var st = new Storage(State.Storages.Count, cell, Config.storage.capacity, Config.storage.startSeeds);
            State.Storages.Add(st);
            return st.Id;
        }

        /// <summary>Có sinh vật nào đang chiếm ô (x, y, z) — nối vào WorldEditor.IsOccupied.</summary>
        public bool IsOccupied(int x, int y, int z)
        {
            var cs = State.Creatures;
            for (int i = 0; i < cs.Count; i++)
                if (cs[i].Contains(x, y, z)) return true;
            var ss = State.Storages;
            for (int i = 0; i < ss.Count; i++)
                if (ss[i].Cell.X == x && ss[i].Cell.Y == y && ss[i].Cell.Z == z) return true;
            var hs = State.Houses;
            for (int i = 0; i < hs.Count; i++)
                if (hs[i].Contains(x, y, z)) return true;
            return false;
        }

        /// <summary>Đặt nhà mẫu (khối đặc, 0017); cell = góc nhỏ nhất.</summary>
        public int AddHouse(CellPos cell)
        {
            var hc = Config.house;
            if (!pathfinder.CanStand(cell, hc.sizeX, hc.sizeZ, hc.sizeY))
                throw new ArgumentException("Nhà không đặt được tại " + cell);
            for (int dx = 0; dx < hc.sizeX; dx++)
                for (int dz = 0; dz < hc.sizeZ; dz++)
                    for (int dy = 0; dy < hc.sizeY; dy++)
                        if (IsOccupied(cell.X + dx, cell.Y + dy, cell.Z + dz))
                            throw new ArgumentException("Ô đã bị chiếm: " + cell.Offset(dx, dy, dz));
            for (int dx = 0; dx < hc.sizeX; dx++)
                for (int dz = 0; dz < hc.sizeZ; dz++)
                    for (int dy = 0; dy < hc.sizeY; dy++)
                        pathfinder.SetBlocked(cell.Offset(dx, dy, dz), true);
            var h = new House(State.Houses.Count, cell, hc.sizeX, hc.sizeZ, hc.sizeY, hc.capacity);
            State.Houses.Add(h);
            homeDirty = true;
            return h.Id;
        }

        public void AssignHome(int creatureId, int houseId)
        {
            if (creatureId < 0 || creatureId >= State.Creatures.Count)
                throw new ArgumentException("Không có sinh vật: " + creatureId);
            if (houseId < 0 || houseId >= State.Houses.Count)
                throw new ArgumentException("Không có nhà: " + houseId);
            var c = State.Creatures[creatureId];
            var h = State.Houses[houseId];
            if (c.HomeId >= 0) throw new ArgumentException("Sinh vật đã có nhà: " + creatureId);
            if (h.Residents.Count >= h.Capacity) throw new ArgumentException("Nhà đã đủ: " + houseId);
            c.HomeId = houseId;
            h.Residents.Add(creatureId);
            homeDirty = true;
        }

        /// <summary>Ô kề nhà mà sinh vật c đứng được, theo thứ tự +X, −X, +Z, −Z (0017) — điền vào goals.</summary>
        void HomeGoals(Creature c, House h)
        {
            goals.Clear();
            var a = h.Cell;
            for (int dz = 0; dz < h.SizeZ; dz++) AddHomeGoal(c, a.Offset(h.SizeX, 0, dz));
            for (int dz = 0; dz < h.SizeZ; dz++) AddHomeGoal(c, a.Offset(-1, 0, dz));
            for (int dx = 0; dx < h.SizeX; dx++) AddHomeGoal(c, a.Offset(dx, 0, h.SizeZ));
            for (int dx = 0; dx < h.SizeX; dx++) AddHomeGoal(c, a.Offset(dx, 0, -1));
        }

        void AddHomeGoal(Creature c, CellPos p)
        {
            for (int k = 0; k < 3; k++)
            {
                var n = p.Offset(0, DisplaceY[k], 0);
                if (pathfinder.CanStand(n, c.SizeX, c.SizeZ, c.SizeY))
                {
                    goals.Add(n);
                    return;
                }
            }
        }

        void HomePhase()
        {
            var cs = State.Creatures;
            for (int i = 0; i < cs.Count; i++)
            {
                var c = cs[i];
                if (c.HomeId < 0) continue;
                HomeGoals(c, State.Houses[c.HomeId]);
                c.HomeReachable = pathfinder.FindPathToAny(c.Cell, goals, c.SizeX, c.SizeZ, c.SizeY, homePath);
            }
        }

        bool HasHomedAbility(WorkAbility a)
        {
            var cs = State.Creatures;
            for (int i = 0; i < cs.Count; i++)
                if (cs[i].HomeId >= 0 && cs[i].HasAbility(a)) return true;
            return false;
        }

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

        void StuckPhase()
        {
            var cs = State.Creatures;
            for (int i = 0; i < cs.Count; i++)
            {
                var c = cs[i];
                if (c.HomeId >= 0 && c.State == CreatureState.Idle && !c.HomeReachable)
                {
                    if (c.StuckSince < 0) c.StuckSince = State.Tick;
                    else if (State.Tick - c.StuckSince >= Config.StuckTicks) Respawn(c);
                }
                else
                {
                    c.StuckSince = -1;
                }
            }
        }

        void Respawn(Creature c)
        {
            HomeGoals(c, State.Houses[c.HomeId]);
            int pick = -1;
            for (int i = 0; i < goals.Count; i++)
                if (FirstBlocker(c, goals[i]) < 0)
                {
                    pick = i;
                    break;
                }
            if (pick < 0) return; // mọi ô kề nhà đang có sinh vật → thử lại nhịp sau
            var cell = goals[pick];
            c.Cell = cell;
            c.Path.Clear();
            c.PathIndex = 0;
            c.MoveProgress = 0;
            c.MoveTarget = cell;
            c.StuckSince = -1;
            c.HomeReachable = true;
            c.BlockedTicks = 0;
            c.Phasing = false;
            c.YieldTo = -1;
            c.YieldHold = false;
            c.WanderAt = -1;
            var stages = State.Stages;
            for (int i = 0; i < stages.Count; i++)
            {
                var g = stages[i];
                if (g.DroppedBy != c.Id || g.Status == StageStatus.Assigned || g.Status == StageStatus.Done) continue;
                g.Target = cell;
                g.DroppedBy = -1;
                if (g.Status == StageStatus.Blocked && g.Stall == StallReason.NoPath)
                {
                    g.Status = StageStatus.Open;
                    g.Stall = StallReason.None;
                }
            }
            State.Events.Add(new SimEvent(State.Tick, SimEventKind.CreatureRespawned, -1, c.Id, cell, StallReason.None));
        }

        void ReportPhase()
        {
            var stages = State.Stages;
            for (int i = 0; i < stages.Count; i++)
            {
                var g = stages[i];
                var cur = g.Status == StageStatus.Blocked ? g.Stall : StallReason.None;
                if (cur == g.ReportedStall) continue;
                if (cur != StallReason.None)
                    State.Events.Add(new SimEvent(State.Tick, SimEventKind.StallStarted, g.Id, -1, g.Target, cur));
                else
                    State.Events.Add(new SimEvent(State.Tick, SimEventKind.StallCleared, g.Id, -1, g.Target, g.ReportedStall));
                g.ReportedStall = cur;
            }
        }

        /// <summary>Tin ô đổi (nối vào WorldEditor.CellChanged) — xử lý ở nhịp kế.</summary>
        public void OnCellChanged(int x, int y, int z) => terrainDirty = true;

        public void Tick()
        {
            State.Tick++;
            if (terrainDirty)
            {
                terrainDirty = false;
                TerrainPhase();
                homeDirty = true;
            }
            if (homeDirty)
            {
                homeDirty = false;
                HomePhase();
            }
            WorkPhase();
            CropPhase();
            MovePhase();
            AssignPhase();
            StuckPhase();
            IdlePhase();
            ReportPhase();
        }

        bool CanStand(Creature c) => pathfinder.CanStand(c.Cell, c.SizeX, c.SizeZ, c.SizeY);

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

        /// <summary>Khung c tại Cell chồng lên khung tại Cell của sinh vật khác.</summary>
        bool OverlapsAnyCell(Creature c)
        {
            var cs = State.Creatures;
            for (int i = 0; i < cs.Count; i++)
                if (i != c.Id && FramesOverlap(cs[i].Cell, cs[i].SizeX, cs[i].SizeZ, cs[i].SizeY, c.Cell, c.SizeX, c.SizeZ, c.SizeY)) return true;
            return false;
        }

        /// <summary>Chỗ chiếm của c (Cell + ô kế tiếp đang bước sang) chồng lên khung tại Cell của sinh vật khác — dùng để tắt Phasing.</summary>
        bool OccupancyOverlapsAnyCell(Creature c)
        {
            var cs = State.Creatures;
            for (int i = 0; i < cs.Count; i++)
                if (i != c.Id && OccupancyOverlaps(c, cs[i].Cell, cs[i].SizeX, cs[i].SizeZ, cs[i].SizeY)) return true;
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

        bool FindPath(Creature c, CellPos from, CellPos to) => pathfinder.FindPath(from, to, c.SizeX, c.SizeZ, c.SizeY, tmpPath);

        void TerrainPhase()
        {
            var stages = State.Stages;
            for (int i = 0; i < stages.Count; i++)
            {
                if (stages[i].Status == StageStatus.Blocked && stages[i].Stall == StallReason.NoPath)
                {
                    stages[i].Status = StageStatus.Open;
                    stages[i].Stall = StallReason.None;
                }
            }

            var cs = State.Creatures;
            var handled = new bool[cs.Count];
            for (int i = 0; i < cs.Count; i++)
            {
                var c = cs[i];
                if (CanStand(c)) continue;
                handled[i] = true;
                Displace(c);
                if (c.State == CreatureState.Working) Unassign(c.Id);
                else if (c.State == CreatureState.Moving) Repath(c);
            }

            for (int i = 0; i < cs.Count; i++)
            {
                var c = cs[i];
                if (handled[i] || c.State != CreatureState.Moving) continue;
                var prev = c.Cell;
                for (int k = c.PathIndex; k < c.Path.Count; k++)
                {
                    if (!pathfinder.CanStep(prev, c.Path[k], c.SizeX, c.SizeZ, c.SizeY))
                    {
                        Repath(c);
                        break;
                    }
                    prev = c.Path[k];
                }
            }
        }

        static readonly int[] DisplaceX = { 1, -1, 0, 0 };
        static readonly int[] DisplaceZ = { 0, 0, 1, -1 };
        static readonly int[] DisplaceY = { 0, 1, -1 };

        /// <summary>Ô dưới chân bị đào: chạy ra ô bên cạnh, không có thì rơi thẳng xuống (0015).</summary>
        void Displace(Creature c)
        {
            for (int d = 0; d < 4; d++)
                for (int k = 0; k < 3; k++)
                {
                    var n = c.Cell.Offset(DisplaceX[d], DisplaceY[k], DisplaceZ[d]);
                    if (pathfinder.CanStep(c.Cell, n, c.SizeX, c.SizeZ, c.SizeY) && FirstBlocker(c, n) < 0)
                    {
                        c.Cell = n;
                        c.MoveProgress = 0;
                        return;
                    }
                }

            while (c.Cell.Y - 1 >= 0 && ColumnsBodyFree(c, c.Cell.Y - 1))
                c.Cell = c.Cell.Offset(0, -1, 0);
            if (OverlapsAnyCell(c)) c.Phasing = true;
            c.MoveProgress = 0;
        }

        bool ColumnsBodyFree(Creature c, int y)
        {
            for (int dx = 0; dx < c.SizeX; dx++)
                for (int dz = 0; dz < c.SizeZ; dz++)
                    if (!pathfinder.IsBodyFree(c.Cell.X + dx, y, c.Cell.Z + dz)) return false;
            return true;
        }

        void Repath(Creature c)
        {
            c.YieldTo = -1;
            c.YieldHold = false;
            c.BlockedTicks = 0;
            if (c.StageId < 0)
            {
                StopWalk(c);
                return;
            }
            var s = State.Stages[c.StageId];
            bool ok = s.HasDestination && s.Carried
                ? PlanTo(c, s.Destination, s.DestinationApproach)
                : PlanTo(c, s.Target, s.TargetApproach);
            if (ok)
            {
                SetPath(c);
                c.MoveProgress = 0;
            }
            else
            {
                BlockNoPath(c);
            }
        }

        /// <summary>Sinh vật rảnh đang né mà đường hỏng (địa hình đổi) → đứng lại.</summary>
        void StopWalk(Creature c)
        {
            c.State = CreatureState.Idle;
            c.Path.Clear();
            c.PathIndex = 0;
            c.MoveProgress = 0;
            c.BlockedTicks = 0;
            c.WanderAt = -1;
            c.MoveTarget = c.Cell;
        }

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

        /// <summary>Tìm đường tới chỗ site theo cách tiếp cận a; đường trong tmpPath (rỗng = đã đứng đúng chỗ).</summary>
        bool PlanTo(Creature c, CellPos site, Approach a)
        {
            if (a == Approach.Exact)
            {
                if (c.Cell == site)
                {
                    tmpPath.Clear();
                    return true;
                }
                return FindPath(c, c.Cell, site);
            }
            int skipped = AdjacentGoals(c, site, true);
            if (pathfinder.FindPathToAny(c.Cell, goals, c.SizeX, c.SizeZ, c.SizeY, tmpPath)) return true;
            if (skipped > 0)
            {
                AdjacentGoals(c, site, false);
                if (pathfinder.FindPathToAny(c.Cell, goals, c.SizeX, c.SizeZ, c.SizeY, tmpPath)) return true;
            }
            if (a == Approach.AdjacentOrOn) return PlanTo(c, site, Approach.Exact);
            return false;
        }

        void SetPath(Creature c)
        {
            c.Path.Clear();
            for (int i = 0; i < tmpPath.Count; i++) c.Path.Add(tmpPath[i]);
            c.PathIndex = 0;
            c.MoveTarget = tmpPath.Count > 0 ? tmpPath[tmpPath.Count - 1] : c.Cell;
        }

        void BlockNoPath(Creature c)
        {
            var s = State.Stages[c.StageId];
            Unassign(c.Id);
            s.Status = StageStatus.Blocked;
            s.Stall = StallReason.NoPath;
        }

        void WorkPhase()
        {
            var cs = State.Creatures;
            for (int i = 0; i < cs.Count; i++)
            {
                var c = cs[i];
                if (c.State != CreatureState.Working) continue;
                var s = State.Stages[c.StageId];
                s.WorkDone += Config.WorkSpeedPermille(c.MasteryFor(s.Ability));
                if (s.WorkDone >= s.WorkRequired)
                {
                    s.WorkDone = s.WorkRequired;
                    s.Status = StageStatus.Done;
                    s.Carried = false;
                    c.State = CreatureState.Idle;
                    c.StageId = -1;
                    OnStageDone(s);
                }
            }
        }

        void OnStageDone(WorkStage s)
        {
            if (s.PlotId >= 0)
            {
                var p = State.Plots[s.PlotId];
                switch (s.Ability)
                {
                    case WorkAbility.Till:
                        p.Phase = PlotPhase.Sow;
                        AddPlotStage(p, WorkAbility.Sow);
                        break;
                    case WorkAbility.Sow:
                        p.Phase = PlotPhase.Water;
                        AddPlotStage(p, WorkAbility.Water);
                        break;
                    case WorkAbility.Water:
                        p.Phase = PlotPhase.Growing;
                        p.StageId = -1;
                        p.RipeTick = State.Tick + Config.GrowTicks;
                        break;
                    case WorkAbility.Harvest:
                        var haul = new WorkStage(State.Stages.Count, p.OrderId, WorkAbility.Haul, p.Site, true,
                            State.Storages[p.StorageId].Cell, Config.BaseTicks(WorkAbility.Haul), 0,
                            Approach.AdjacentOrOn, Approach.Adjacent, -1, p.StorageId,
                            Config.crop.producePerHarvest, Config.crop.seedsPerHarvest);
                        State.Stages.Add(haul);
                        State.Orders[p.OrderId].StageIds.Add(haul.Id);
                        p.Queue.RemoveAt(0);
                        if (p.Queue.Count > 0)
                        {
                            StartCrop(p);
                        }
                        else
                        {
                            p.Phase = PlotPhase.Idle;
                            p.StageId = -1;
                        }
                        break;
                }
            }
            else if (s.Ability == WorkAbility.Haul && s.StorageId >= 0)
            {
                var st = State.Storages[s.StorageId];
                st.Reserved -= s.LoadProduce + s.LoadSeeds;
                st.Produce += s.LoadProduce;
                st.Seeds += s.LoadSeeds;
            }
        }

        void CropPhase()
        {
            var ps = State.Plots;
            for (int i = 0; i < ps.Count; i++)
            {
                var p = ps[i];
                if (p.Phase == PlotPhase.Growing && State.Tick >= p.RipeTick)
                {
                    p.Phase = PlotPhase.Harvest;
                    AddPlotStage(p, WorkAbility.Harvest);
                }
            }
        }

        public int AddFarmOrder(IReadOnlyList<CellPos> soilCells, int storageId)
        {
            if (storageId < 0 || storageId >= State.Storages.Count)
                throw new ArgumentException("Không có kho: " + storageId);
            var size = World.Size;
            for (int i = 0; i < soilCells.Count; i++)
            {
                var f = soilCells[i];
                if (!size.InBounds(f.X, f.Y, f.Z))
                    throw new ArgumentException("Ô ruộng ngoài map: " + f);
                if (!Bounds.Contains(f.X, f.Z))
                    throw new ArgumentException("Ô ruộng ngoài vùng mô phỏng: " + f);
                var m = World.GetMaterial(f.X, f.Y, f.Z);
                if (m != BlockMaterial.Dirt && m != BlockMaterial.Grass)
                    throw new ArgumentException("Ô ruộng phải là đất hoặc cỏ: " + f);
                if (!size.InBounds(f.X, f.Y + 1, f.Z) || World.GetMaterial(f.X, f.Y + 1, f.Z) != BlockMaterial.Empty)
                    throw new ArgumentException("Ô phía trên ô ruộng phải rỗng: " + f);
                for (int j = 0; j < i; j++)
                    if (soilCells[j] == f) throw new ArgumentException("Trùng ô ruộng trong danh sách: " + f);
                for (int j = 0; j < State.Plots.Count; j++)
                    if (State.Plots[j].Soil == f) throw new ArgumentException("Ô đã là ruộng: " + f);
            }
            int orderId = AddOrder(soilCells);
            for (int i = 0; i < soilCells.Count; i++)
            {
                var p = new FarmPlot(State.Plots.Count, orderId, storageId, soilCells[i]);
                State.Plots.Add(p);
                p.Queue.Add(0);
                StartCrop(p);
            }
            return orderId;
        }

        /// <summary>Thêm một vụ vào hàng đợi của ô ruộng; false khi hàng đợi đầy.</summary>
        public bool EnqueueCrop(int plotId)
        {
            var p = State.Plots[plotId];
            if (p.Queue.Count >= Config.farm.plotQueueCapacity) return false;
            p.Queue.Add(0);
            if (p.Phase == PlotPhase.Idle) StartCrop(p);
            return true;
        }

        void StartCrop(FarmPlot p)
        {
            p.Phase = PlotPhase.Till;
            AddPlotStage(p, WorkAbility.Till);
        }

        void AddPlotStage(FarmPlot p, WorkAbility a)
        {
            var s = new WorkStage(State.Stages.Count, p.OrderId, a, p.Site, false, default, Config.BaseTicks(a), 0,
                Approach.AdjacentOrOn, Approach.Exact, p.Id);
            State.Stages.Add(s);
            State.Orders[p.OrderId].StageIds.Add(s.Id);
            p.StageId = s.Id;
        }

        void MovePhase()
        {
            var cs = State.Creatures;
            for (int i = 0; i < cs.Count; i++)
            {
                var c = cs[i];
                if (c.Phasing && !OccupancyOverlapsAnyCell(c)) c.Phasing = false;
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
            if (c.StageId < 0)
            {
                StopWalk(c); // con rảnh không có đường khác → đứng lại (Quyết định 8)
                return;
            }
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

        /// <summary>Đường cho y tới ô gần nhất ngoài Cell + phần đường còn lại của w, tránh mọi sinh vật khác — vào tmpPath; avoidPlots: không nhận ô ruộng.</summary>
        bool FindAside(Creature y, Creature w, bool avoidPlots)
        {
            var cs = State.Creatures;
            for (int i = 0; i < cs.Count; i++)
                if (i != y.Id) TempBlockOccupancy(cs[i]);
            bool ok = pathfinder.FindPathToNearest(y.Cell, y.SizeX, y.SizeZ, y.SizeY, AsideSearchSteps, p => !PathTouches(w, p, y) && !(avoidPlots && OnPlot(p, y.SizeX, y.SizeZ, y.SizeY)), tmpPath);
            pathfinder.ClearTempBlocks();
            return ok;
        }

        /// <summary>Sinh vật rảnh o né khỏi đường của w; tới nơi thì Idle (OnArrive).</summary>
        bool Dodge(Creature o, Creature w)
        {
            if (!FindAside(o, w, true)) return false;
            o.State = CreatureState.Moving;
            SetPath(o);
            o.MoveProgress = 0;
            o.BlockedTicks = 0;
            o.WanderAt = -1;
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
            if (!FindAside(y, w, false)) return;
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

        void AssignPhase()
        {
            var open = new List<WorkStage>();
            var stages = State.Stages;
            for (int i = 0; i < stages.Count; i++)
            {
                var g = stages[i];
                if (g.Status != StageStatus.Blocked) continue;
                if (g.Stall == StallReason.NoSeeds && State.Storages[State.Plots[g.PlotId].StorageId].Seeds > 0
                    || g.Stall == StallReason.StorageFull && State.Storages[g.StorageId].Free >= g.LoadProduce + g.LoadSeeds
                    || g.Stall == StallReason.NoAbility && HasHomedAbility(g.Ability))
                {
                    g.Status = StageStatus.Open;
                    g.Stall = StallReason.None;
                }
            }
            for (int i = 0; i < stages.Count; i++)
                if (stages[i].Status == StageStatus.Open && stages[i].AssigneeKind == AssigneeKind.None) open.Add(stages[i]);
            if (open.Count == 0) return;
            open.Sort((a, b) =>
            {
                int p = b.Priority.CompareTo(a.Priority);
                return p != 0 ? p : a.Id.CompareTo(b.Id);
            });

            var cs = State.Creatures;
            var eligible = new List<Creature>();
            var scores = new List<long>();
            var group = new List<int>();
            for (int k = 0; k < open.Count; k++)
            {
                var s = open[k];
                if (s.PlotId >= 0 && s.Ability == WorkAbility.Sow && State.Storages[State.Plots[s.PlotId].StorageId].Seeds <= 0)
                {
                    s.Status = StageStatus.Blocked;
                    s.Stall = StallReason.NoSeeds;
                    continue;
                }
                if (s.StorageId >= 0 && State.Storages[s.StorageId].Free < s.LoadProduce + s.LoadSeeds)
                {
                    s.Status = StageStatus.Blocked;
                    s.Stall = StallReason.StorageFull;
                    continue;
                }
                if (!HasHomedAbility(s.Ability))
                {
                    s.Status = StageStatus.Blocked;
                    s.Stall = StallReason.NoAbility;
                    continue;
                }
                eligible.Clear();
                scores.Clear();
                for (int i = 0; i < cs.Count; i++)
                {
                    var c = cs[i];
                    if (c.HomeId < 0 || c.StageId >= 0 || !c.HasAbility(s.Ability)) continue;
                    eligible.Add(c);
                    scores.Add((long)CellPos.Manhattan(c.Cell, s.Target) * 100000 / c.MasteryFor(s.Ability));
                }
                if (eligible.Count == 0) continue;

                bool assigned = false;
                while (eligible.Count > 0)
                {
                    long best = long.MaxValue;
                    for (int i = 0; i < scores.Count; i++)
                        if (scores[i] < best) best = scores[i];
                    group.Clear();
                    for (int i = 0; i < scores.Count; i++)
                        if (scores[i] == best) group.Add(i);
                    int pickIdx = group.Count == 1 ? group[0] : group[State.Rng.NextInt(group.Count)];
                    var pick = eligible[pickIdx];
                    if (PlanTo(pick, s.Target, s.TargetApproach))
                    {
                        Assign(pick, s, tmpPath);
                        assigned = true;
                        break;
                    }
                    eligible.RemoveAt(pickIdx);
                    scores.RemoveAt(pickIdx);
                }
                if (!assigned)
                {
                    s.Status = StageStatus.Blocked;
                    s.Stall = StallReason.NoPath;
                }
            }
        }

        void Assign(Creature c, WorkStage s, List<CellPos> path)
        {
            if (s.PlotId >= 0 && s.Ability == WorkAbility.Sow) State.Storages[State.Plots[s.PlotId].StorageId].Seeds--;
            if (s.StorageId >= 0) State.Storages[s.StorageId].Reserved += s.LoadProduce + s.LoadSeeds;
            s.DroppedBy = -1;
            s.Status = StageStatus.Assigned;
            s.AssigneeKind = AssigneeKind.Creature;
            s.AssigneeId = c.Id;
            c.StageId = s.Id;
            c.BlockedTicks = 0;
            c.YieldTo = -1;
            c.YieldHold = false;
            c.MoveProgress = 0;
            c.State = CreatureState.Moving;
            if (path.Count == 0)
            {
                c.Path.Clear();
                c.PathIndex = 0;
                OnArrive(c);
            }
            else
            {
                SetPath(c);
            }
        }

        public void Unassign(int creatureId)
        {
            var c = State.Creatures[creatureId];
            if (c.StageId < 0) return;
            var s = State.Stages[c.StageId];
            if (s.PlotId >= 0 && s.Ability == WorkAbility.Sow) State.Storages[State.Plots[s.PlotId].StorageId].Seeds++;
            if (s.StorageId >= 0) State.Storages[s.StorageId].Reserved -= s.LoadProduce + s.LoadSeeds;
            if (s.Carried)
            {
                s.Target = c.Cell;
                s.Carried = false;
                s.DroppedBy = c.Id;
            }
            s.Status = StageStatus.Open;
            s.AssigneeKind = AssigneeKind.None;
            s.AssigneeId = -1;
            c.State = CreatureState.Idle;
            c.StageId = -1;
            c.BlockedTicks = 0;
            c.YieldTo = -1;
            c.YieldHold = false;
            c.MoveProgress = 0;
            c.Path.Clear();
            c.PathIndex = 0;
            c.MoveTarget = c.Cell;
        }
    }
}
