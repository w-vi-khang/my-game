using System;
using System.Collections.Generic;

namespace Game.Core.Sim
{
    /// <summary>Mô phỏng nông trại M1 — nhịp cố định, số nguyên, tất định (simulation-tech bước 1, 0014). A* đi bộ theo khung chiếm chỗ (bước 2, 0015). Ô ruộng + huỷ vụ (bước 6a, 0020).</summary>
    public sealed class Simulation
    {
        public readonly SimConfig Config;
        public readonly SimState State;
        public readonly WorldData World;
        readonly GridPathfinder pathfinder;
        bool writingWorld;

        public WorldEditor Editor { get; private set; }
        readonly List<CellPos> tmpPath = new List<CellPos>();
        readonly List<CellPos> goals = new List<CellPos>();
        readonly List<CellPos> playerGoals = new List<CellPos>();
        readonly List<CellPos> storeGoals = new List<CellPos>();
        readonly List<CellPos> storePath = new List<CellPos>();
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
            State = new SimState(new SimRng(config.simSeed, 1UL), config.crops.Length);
        }

        public SimBounds Bounds => pathfinder.Bounds;

        /// <summary>Số ô mảng tìm đường — đo bộ nhớ (brief 0019).</summary>
        public int PathfinderCellCount => pathfinder.CellCount;

        public long AStarCount => pathfinder.AStarCount;
        public long BfsCount => pathfinder.BfsCount;

        /// <summary>Đo thời gian từng pha của Tick (brief 0028); không vào mã băm.</summary>
        public bool ProfilePhases;
        public static readonly string[] PhaseNames = { "Terrain", "Home", "Work", "Crop", "Move", "Assign", "Stuck", "Idle", "Report" };
        public readonly long[] PhaseTicks = new long[9];

        public int AddCreature(string speciesId, CellPos cell)
        {
            var sp = Config.FindSpecies(speciesId);
            if (!pathfinder.CanStand(cell, sp.sizeX, sp.sizeZ, sp.sizeY))
                throw new ArgumentException("Creature cannot stand at " + cell);
            for (int i = 0; i < State.Creatures.Count; i++)
            {
                var o = State.Creatures[i];
                if (FramesOverlap(o.Cell, o.SizeX, o.SizeZ, o.SizeY, cell, sp.sizeX, sp.sizeZ, sp.sizeY))
                    throw new ArgumentException("Cell already has a creature: " + cell);
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
                throw new ArgumentException("Use AddHaulStage for hauling.");
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
                throw new ArgumentException("Storage cannot be placed at " + cell);
            if (IsOccupied(cell.X, cell.Y, cell.Z))
                throw new ArgumentException("Cell is already occupied: " + cell);
            pathfinder.SetBlocked(cell, true);
            homeDirty = true;
            var st = new Storage(State.Storages.Count, cell, Config.storage.capacity);
            State.Storages.Add(st);
            State.Stock.Capacity += Config.storage.capacity;
            for (int k = 0; k < Config.crops.Length; k++) State.Stock.Seeds[k] += Config.crops[k].startSeeds;
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
                throw new ArgumentException("House cannot be placed at " + cell);
            for (int dx = 0; dx < hc.sizeX; dx++)
                for (int dz = 0; dz < hc.sizeZ; dz++)
                    for (int dy = 0; dy < hc.sizeY; dy++)
                        if (IsOccupied(cell.X + dx, cell.Y + dy, cell.Z + dz))
                            throw new ArgumentException("Cell is already occupied: " + cell.Offset(dx, dy, dz));
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
                throw new ArgumentException("No such creature: " + creatureId);
            if (houseId < 0 || houseId >= State.Houses.Count)
                throw new ArgumentException("No such house: " + houseId);
            var c = State.Creatures[creatureId];
            var h = State.Houses[houseId];
            if (c.HomeId >= 0) throw new ArgumentException("Creature already has a house: " + creatureId);
            if (h.Residents.Count >= h.Capacity) throw new ArgumentException("House is full: " + houseId);
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
                if (g.DroppedBy != c.Id || g.Status == StageStatus.Assigned || g.Status == StageStatus.Done || g.Status == StageStatus.Cancelled) continue;
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

        /// <summary>Tin ô đổi (nối vào WorldEditor.CellChanged) — xử lý ở nhịp kế; ô do mô phỏng tự xới không tính.</summary>
        public void OnCellChanged(int x, int y, int z) { if (!writingWorld) terrainDirty = true; }

        /// <summary>Nối world editor (0020): đào/đặt báo cho mô phỏng, ô có sinh vật/nhà/kho không đặt được; xới xong ghi ô ruộng qua editor.</summary>
        public void AttachEditor(WorldEditor editor)
        {
            if (editor == null) throw new ArgumentNullException(nameof(editor));
            if (editor.World != World) throw new ArgumentException("Editor must edit the same WorldData as the simulation.");
            if (Editor != null) throw new InvalidOperationException("Simulation is already attached to an editor.");
            Editor = editor;
            editor.IsOccupied = IsOccupied;
            editor.CellChanged += OnCellChanged;
        }

        void WriteFarmland(CellPos c)
        {
            if (Editor == null)
            {
                WorldEditor.Till(World, c.X, c.Y, c.Z);
                return;
            }
            writingWorld = true;
            try { Editor.TryTill(c.X, c.Y, c.Z); }
            finally { writingWorld = false; }
        }

        /// <summary>Ô ruộng còn nguyên: ô trên rỗng; chưa xới → đất/cỏ, đã xới → ô ruộng (0020).</summary>
        bool PlotIntact(FarmPlot p)
        {
            if (World.GetMaterial(p.Site.X, p.Site.Y, p.Site.Z) != BlockMaterial.Empty) return false;
            var m = World.GetMaterial(p.Soil.X, p.Soil.Y, p.Soil.Z);
            return p.Tilled ? m == BlockMaterial.Farmland : m == BlockMaterial.Dirt || m == BlockMaterial.Grass;
        }

        /// <summary>Loại cây của vụ hiện tại (ô tiến trình, 0027).</summary>
        int CropOf(FarmPlot p) => p.CurrentCrop;

        /// <summary>Mở công đoạn gieo: vụ rời ô đợi đầu sang ô tiến trình (0027). Gọi khi Slots.Count > 0.</summary>
        void BeginSow(FarmPlot p)
        {
            var head = p.Slots[0];
            p.CurrentCrop = head.CropId;
            if (--head.Count == 0) p.Slots.RemoveAt(0);
            p.Phase = PlotPhase.Sow;
            AddPlotStage(p, WorkAbility.Sow);
        }

        /// <summary>Bỏ công đoạn đang mở của ô (0020/0027): sinh vật đang làm về rảnh, công đoạn Cancelled. Không trả hạt (0027).</summary>
        void AbortActive(FarmPlot p, out int stageId, out int creatureId)
        {
            stageId = p.StageId;
            creatureId = -1;
            if (stageId < 0) return;
            var s = State.Stages[stageId];
            if (s.Status == StageStatus.Assigned)
            {
                creatureId = s.AssigneeId;
                ReleaseCreature(State.Creatures[creatureId]);
            }
            s.Status = StageStatus.Cancelled;
            s.Stall = StallReason.None;
            s.AssigneeKind = AssigneeKind.None;
            s.AssigneeId = -1;
        }

        /// <summary>Huỷ vụ (0020, 0027): không trả hạt, sinh vật đang làm về rảnh, ô rời lệnh, xoá hàng đợi. Vận chuyển hàng đã thu giữ nguyên.</summary>
        void CancelPlot(FarmPlot p)
        {
            AbortActive(p, out int stageId, out int creatureId);
            p.Removed = true;
            p.Slots.Clear();
            p.CurrentCrop = -1;
            p.TillPaused = false;
            p.Phase = PlotPhase.Idle;
            p.StageId = -1;
            State.Events.Add(new SimEvent(State.Tick, SimEventKind.CropCancelled, stageId, creatureId, p.Soil, StallReason.None, 0));
        }

        public void Tick()
        {
            State.Tick++;
            long t = ProfilePhases ? System.Diagnostics.Stopwatch.GetTimestamp() : 0;
            if (terrainDirty)
            {
                terrainDirty = false;
                TerrainPhase();
                homeDirty = true;
            }
            t = Lap(0, t);
            if (homeDirty)
            {
                homeDirty = false;
                HomePhase();
            }
            t = Lap(1, t);
            WorkPhase();
            t = Lap(2, t);
            CropPhase();
            t = Lap(3, t);
            MovePhase();
            t = Lap(4, t);
            AssignPhase();
            t = Lap(5, t);
            StuckPhase();
            t = Lap(6, t);
            IdlePhase();
            t = Lap(7, t);
            ReportPhase();
            Lap(8, t);
        }

        long Lap(int phase, long start)
        {
            if (!ProfilePhases) return 0;
            long now = System.Diagnostics.Stopwatch.GetTimestamp();
            PhaseTicks[phase] += now - start;
            return now;
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
                if (!ps[i].Removed && FramesOverlap(p, sx, sz, sy, ps[i].Site, 1, 1, 1)) return true;
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
            var ps = State.Plots;
            for (int i = 0; i < ps.Count; i++)
                if (!ps[i].Removed && !PlotIntact(ps[i])) CancelPlot(ps[i]);
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

        void OnStageDone(WorkStage s, PlayerBag bag = null)
        {
            if (s.PlotId >= 0)
            {
                var p = State.Plots[s.PlotId];
                switch (s.Ability)
                {
                    case WorkAbility.Till:
                        if (!p.Tilled) { WriteFarmland(p.Soil); p.Tilled = true; }
                        p.SoilReady = true;
                        if (p.Slots.Count > 0)
                        {
                            BeginSow(p);
                        }
                        else
                        {
                            p.Phase = PlotPhase.Idle;
                            p.StageId = -1;
                        }
                        break;
                    case WorkAbility.Sow:
                        p.Phase = PlotPhase.Water;
                        AddPlotStage(p, WorkAbility.Water);
                        break;
                    case WorkAbility.Water:
                        p.Phase = PlotPhase.Growing;
                        p.StageId = -1;
                        p.RipeTick = State.Tick + Config.GrowTicks(CropOf(p));
                        break;
                    case WorkAbility.Harvest:
                        int crop = CropOf(p);
                        var cd = Config.crops[crop];
                        if (bag != null)
                        {
                            bag.AddProduce(crop, cd.producePerHarvest);
                            bag.AddSeeds(crop, cd.seedsPerHarvest);
                        }
                        else
                        {
                            int k = NearestStorage(p.Site);
                            var haul = new WorkStage(State.Stages.Count, p.OrderId, WorkAbility.Haul, p.Site, true,
                                State.Storages[k].Cell, Config.BaseTicks(WorkAbility.Haul), 0,
                                Approach.AdjacentOrOn, Approach.Adjacent, -1, k,
                                cd.producePerHarvest, cd.seedsPerHarvest, crop);
                            State.Stages.Add(haul);
                            State.Orders[p.OrderId].StageIds.Add(haul.Id);
                        }
                        p.SoilReady = false;
                        p.CurrentCrop = -1;
                        if (p.Slots.Count > 0)
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
                var stock = State.Stock;
                stock.Reserved -= s.LoadProduce + s.LoadSeeds;
                stock.Produce[s.CropId] += s.LoadProduce;
                stock.Seeds[s.CropId] += s.LoadSeeds;
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

        /// <summary>Ô ruộng chưa Removed có Soil = soil; -1 = không có.</summary>
        public int PlotAt(CellPos soil)
        {
            var ps = State.Plots;
            for (int i = 0; i < ps.Count; i++)
                if (!ps[i].Removed && ps[i].Soil == soil) return i;
            return -1;
        }

        /// <summary>Có đánh dấu xới được ô soil không (0025): trong map và vùng mô phỏng, đất/cỏ, ô trên rỗng, chưa là ô ruộng / chưa có biển.</summary>
        public bool CanMarkTill(CellPos soil)
        {
            var size = World.Size;
            if (!size.InBounds(soil.X, soil.Y, soil.Z) || !Bounds.Contains(soil.X, soil.Z)) return false;
            var m = World.GetMaterial(soil.X, soil.Y, soil.Z);
            if (m != BlockMaterial.Dirt && m != BlockMaterial.Grass) return false;
            if (!size.InBounds(soil.X, soil.Y + 1, soil.Z) || World.GetMaterial(soil.X, soil.Y + 1, soil.Z) != BlockMaterial.Empty) return false;
            return PlotAt(soil) < 0;
        }

        /// <summary>Đánh dấu xới một ô (0025): ô có biển, mở công đoạn xới. Trả id ô ruộng; -1 khi không hợp lệ.</summary>
        public int MarkTill(CellPos soil)
        {
            if (!CanMarkTill(soil)) return -1;
            int orderId = AddOrder(new[] { soil });
            var p = new FarmPlot(State.Plots.Count, orderId, soil);
            State.Plots.Add(p);
            p.SlotCount = Config.farm.queueSlots;
            p.SlotMax = Config.farm.slotMaxCount;
            p.Phase = PlotPhase.Till;
            AddPlotStage(p, WorkAbility.Till);
            return p.Id;
        }

        /// <summary>Bỏ đánh dấu (0025): chỉ ô có biển (chưa thành ô ruộng). Sinh vật đang xới về rảnh. Không phát sự kiện.</summary>
        public bool Unmark(int plotId)
        {
            var p = State.Plots[plotId];
            if (p.Removed || p.Tilled) return false;
            AbortActive(p, out _, out _);
            p.Removed = true;
            p.Slots.Clear();
            p.Phase = PlotPhase.Idle;
            p.StageId = -1;
            return true;
        }

        /// <summary>Chọn hạt cropId được không (0026 mục 2): ô ruộng còn, có kho, ô đợi cuối cùng loại chưa đầy hoặc còn ô đợi trống.</summary>
        public bool CanEnqueueCrop(int plotId, int cropId)
        {
            if (cropId < 0 || cropId >= Config.crops.Length)
                throw new ArgumentException("No such crop: " + cropId);
            var p = State.Plots[plotId];
            if (p.Removed || State.Storages.Count == 0) return false;
            int n = p.Slots.Count;
            if (n > 0 && p.Slots[n - 1].CropId == cropId && p.Slots[n - 1].Count < p.SlotMax) return true;
            return n < p.SlotCount;
        }

        /// <summary>Chọn hạt cropId (0026 mục 2): ô đợi cuối cùng loại chưa đầy → +1; ngược lại thêm ô đợi mới ×1. Không gộp ô cùng loại kề nhau. Ô đứng yên → bắt đầu vụ.</summary>
        public bool EnqueueCrop(int plotId, int cropId)
        {
            if (!CanEnqueueCrop(plotId, cropId)) return false;
            var p = State.Plots[plotId];
            int n = p.Slots.Count;
            if (n > 0 && p.Slots[n - 1].CropId == cropId && p.Slots[n - 1].Count < p.SlotMax) p.Slots[n - 1].Count++;
            else p.Slots.Add(new CropSlot(cropId, 1));
            if (p.Phase == PlotPhase.Idle) StartCrop(p);
            return true;
        }

        /// <summary>Nút + của ô đợi index (0026 mục 3).</summary>
        public bool IncreaseSlot(int plotId, int index)
        {
            var p = State.Plots[plotId];
            if (p.Removed || index < 0 || index >= p.Slots.Count || p.Slots[index].Count >= p.SlotMax) return false;
            p.Slots[index].Count++;
            return true;
        }

        /// <summary>Nút − của ô đợi index: về 0 → xoá, các ô sau đôn lên. Không đụng vụ hiện tại (0027).</summary>
        public bool DecreaseSlot(int plotId, int index)
        {
            var p = State.Plots[plotId];
            if (p.Removed || index < 0 || index >= p.Slots.Count) return false;
            if (--p.Slots[index].Count == 0) p.Slots.RemoveAt(index);
            return true;
        }

        /// <summary>Nút x của ô đợi index: xoá cả ô đợi, các ô sau đôn lên (0027).</summary>
        public bool ClearSlot(int plotId, int index)
        {
            var p = State.Plots[plotId];
            if (p.Removed || index < 0 || index >= p.Slots.Count) return false;
            p.Slots.RemoveAt(index);
            return true;
        }

        /// <summary>Huỷ vụ trong ô tiến trình (0027): không trả hạt, ô vẫn là ô ruộng; còn ô đợi → vụ kế. Không phát sự kiện.</summary>
        public bool CancelCurrent(int plotId)
        {
            var p = State.Plots[plotId];
            if (p.Removed || p.CurrentCrop < 0) return false;
            var phase = p.Phase;
            AbortActive(p, out _, out _);
            p.CurrentCrop = -1;
            p.StageId = -1;
            if (phase == PlotPhase.Water || phase == PlotPhase.Growing || phase == PlotPhase.Harvest) p.SoilReady = false;
            if (p.Slots.Count > 0) StartCrop(p);
            else p.Phase = PlotPhase.Idle;
            return true;
        }

        /// <summary>Tạm dừng xới lại (0027): chỉ ô ruộng đã xới đang ở pha xới. Sinh vật đang làm về rảnh qua Unassign (giữ tiến độ).</summary>
        public bool PauseTill(int plotId)
        {
            var p = State.Plots[plotId];
            if (p.Removed || !p.Tilled || p.Phase != PlotPhase.Till || p.TillPaused) return false;
            var s = State.Stages[p.StageId];
            if (s.Status == StageStatus.Assigned) Unassign(s.AssigneeId);
            p.TillPaused = true;
            return true;
        }

        /// <summary>Tiếp tục xới (0027).</summary>
        public bool ResumeTill(int plotId)
        {
            var p = State.Plots[plotId];
            if (p.Removed || !p.TillPaused) return false;
            p.TillPaused = false;
            return true;
        }

        /// <summary>Người chơi làm xong công đoạn s (0028, simulation-tech "Người chơi làm công đoạn"). bag != null chỉ dùng cho thu hoạch: hàng vào túi, không chở.</summary>
        void CompleteByPlayer(WorkStage s, PlayerBag bag = null)
        {
            if (s.Status == StageStatus.Assigned) Unassign(s.AssigneeId);
            s.WorkDone = s.WorkRequired;
            s.Status = StageStatus.Done;
            s.Stall = StallReason.None;
            s.Carried = false;
            s.AssigneeKind = AssigneeKind.Player;
            s.AssigneeId = -1;
            OnStageDone(s, bag);
        }

        bool ValidPlot(int plotId) => plotId >= 0 && plotId < State.Plots.Count && !State.Plots[plotId].Removed;

        void CheckCropArg(int cropId)
        {
            if (cropId < 0 || cropId >= Config.crops.Length)
                throw new ArgumentException("Crop not in config: " + cropId);
        }

        /// <summary>Người chơi tự xới được ô plotId không: ô còn, đang ở pha xới (lần đầu hoặc xới lại), có công đoạn.</summary>
        public bool CanPlayerTill(int plotId)
        {
            if (!ValidPlot(plotId)) return false;
            var p = State.Plots[plotId];
            return p.Phase == PlotPhase.Till && p.StageId >= 0;
        }

        /// <summary>Người chơi xới xong (0028 mục 7): bỏ tạm dừng, hoàn thành công đoạn xới. false nếu !CanPlayerTill.</summary>
        public bool PlayerTill(int plotId)
        {
            if (!CanPlayerTill(plotId)) return false;
            var p = State.Plots[plotId];
            p.TillPaused = false;
            CompleteByPlayer(State.Stages[p.StageId]);
            return true;
        }

        /// <summary>Kiểm người chơi gieo cây cropId vào ô plotId được không, không đổi gì. Ô còn hạt trong hàng đợi (Slots) → QueueNotEmpty (0034 mục 4).</summary>
        public PlayerSowResult CheckPlayerSow(int plotId, int cropId, PlayerBag bag)
        {
            CheckCropArg(cropId);
            if (bag == null) throw new ArgumentNullException(nameof(bag));
            if (!ValidPlot(plotId)) return PlayerSowResult.NoPlot;
            var p = State.Plots[plotId];
            if (p.Phase == PlotPhase.Sow && p.CurrentCrop != cropId) return PlayerSowResult.OtherCrop;
            if (p.Phase == PlotPhase.Water || p.Phase == PlotPhase.Growing || p.Phase == PlotPhase.Harvest)
                return PlayerSowResult.HasCrop;
            if (p.Slots.Count > 0) return PlayerSowResult.QueueNotEmpty; // gieo tay chỉ ô hàng đợi trống (0034 mục 4)
            if (p.Phase == PlotPhase.Till || (p.Phase == PlotPhase.Idle && !p.SoilReady))
                return PlayerSowResult.NotTilled;
            if (bag.GetSeeds(cropId) <= 0) return PlayerSowResult.NoSeeds;
            return PlayerSowResult.Ok;
        }

        /// <summary>Gieo bằng hạt túi (0028 mục 8). Ok → trừ 1 hạt túi, vụ sang pha tưới.</summary>
        public PlayerSowResult PlayerSow(int plotId, int cropId, PlayerBag bag)
        {
            var r = CheckPlayerSow(plotId, cropId, bag);
            if (r != PlayerSowResult.Ok) return r;
            var p = State.Plots[plotId];
            bag.TryTakeSeeds(cropId, 1);
            if (p.Phase == PlotPhase.Idle)
            {
                p.CurrentCrop = cropId;
                p.Phase = PlotPhase.Sow;
                AddPlotStage(p, WorkAbility.Sow);
            }
            CompleteByPlayer(State.Stages[p.StageId]);
            return PlayerSowResult.Ok;
        }

        /// <summary>Cây chín (pha thu hoạch) và ô còn.</summary>
        public bool CanPlayerHarvest(int plotId)
        {
            if (!ValidPlot(plotId)) return false;
            var p = State.Plots[plotId];
            return p.Phase == PlotPhase.Harvest && p.StageId >= 0;
        }

        /// <summary>Thu hoạch vào túi (0028 mục 10), không chở. false nếu !CanPlayerHarvest.</summary>
        public bool PlayerHarvest(int plotId, PlayerBag bag)
        {
            if (bag == null) throw new ArgumentNullException(nameof(bag));
            if (!CanPlayerHarvest(plotId)) return false;
            CompleteByPlayer(State.Stages[State.Plots[plotId].StageId], bag);
            return true;
        }

        /// <summary>Phép Mưa lên ô cell (0028 mục 9): ô ruộng chờ tưới → tưới xong; ô đất → cỏ; khác → NoEffect, không đổi gì.</summary>
        public RainResult Rain(CellPos cell)
        {
            int id = PlotAt(cell);
            if (id >= 0 && State.Plots[id].Phase == PlotPhase.Water && State.Plots[id].StageId >= 0)
            {
                CompleteByPlayer(State.Stages[State.Plots[id].StageId]);
                return RainResult.Watered;
            }
            bool changed;
            if (Editor != null)
            {
                writingWorld = true;
                try { changed = Editor.TryGrow(cell.X, cell.Y, cell.Z); }
                finally { writingWorld = false; }
            }
            else
            {
                changed = WorldEditor.Grow(World, cell.X, cell.Y, cell.Z);
            }
            return changed ? RainResult.Grassed : RainResult.NoEffect;
        }

        /// <summary>Có kho nào trong tầm thao tác của nhân vật (0034: 10 ô).</summary>
        public bool StorageInReach(float feetX, float feetY, float feetZ)
        {
            var st = State.Storages;
            for (int i = 0; i < st.Count; i++)
                if (PlayerRules.InTargetReach(feetX, feetY, feetZ, st[i].Cell.X, st[i].Cell.Y, st[i].Cell.Z)) return true;
            return false;
        }

        /// <summary>Túi → kho chung: chuyển min(amount, số trong túi, Stock.Free) hạt (produce = false) hoặc nông sản; trả số đã chuyển.</summary>
        public int StoreToStock(PlayerBag bag, int cropId, bool produce, int amount)
        {
            CheckCropArg(cropId);
            if (bag == null) throw new ArgumentNullException(nameof(bag));
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount), amount, "Amount must be ≥ 0");
            int n = Math.Min(amount, produce ? bag.GetProduce(cropId) : bag.GetSeeds(cropId));
            n = Math.Min(n, Math.Max(0, State.Stock.Free));
            if (n <= 0) return 0;
            if (produce)
            {
                bag.TryTakeProduce(cropId, n);
                State.Stock.Produce[cropId] += n;
            }
            else
            {
                bag.TryTakeSeeds(cropId, n);
                State.Stock.Seeds[cropId] += n;
            }
            return n;
        }

        /// <summary>Kho chung → túi: chuyển min(amount, số trong kho); trả số đã chuyển.</summary>
        public int TakeFromStock(PlayerBag bag, int cropId, bool produce, int amount)
        {
            CheckCropArg(cropId);
            if (bag == null) throw new ArgumentNullException(nameof(bag));
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount), amount, "Amount must be ≥ 0");
            int n = Math.Min(amount, produce ? State.Stock.Produce[cropId] : State.Stock.Seeds[cropId]);
            if (n <= 0) return 0;
            if (produce)
            {
                State.Stock.Produce[cropId] -= n;
                bag.AddProduce(cropId, n);
            }
            else
            {
                State.Stock.Seeds[cropId] -= n;
                bag.AddSeeds(cropId, n);
            }
            return n;
        }

        /// <summary>Đường cho nhân vật (khung 1 × 1 × 2, không tránh sinh vật) từ ô chân feet tới ô kề ô ruộng plotId — như sinh vật (AdjacentOrOn). result không gồm feet; đã đứng ở đích → true, result rỗng. Không tới được / ô không còn → false.</summary>
        public bool FindPlayerPathToPlot(CellPos feet, int plotId, List<CellPos> result)
        {
            if (!ValidPlot(plotId)) return false;
            var site = State.Plots[plotId].Site;
            int skipped = PlayerAdjacentGoals(site, true);
            if (pathfinder.FindPathToAny(feet, playerGoals, 1, 1, 2, result)) return true;
            if (skipped > 0)
            {
                PlayerAdjacentGoals(site, false);
                if (pathfinder.FindPathToAny(feet, playerGoals, 1, 1, 2, result)) return true;
            }
            if (feet == site)
            {
                result.Clear();
                return true;
            }
            return pathfinder.FindPath(feet, site, 1, 1, 2, result);
        }

        int PlayerAdjacentGoals(CellPos site, bool skipPlots)
        {
            playerGoals.Clear();
            int skipped = 0;
            for (int d = 0; d < 4; d++)
                for (int k = 0; k < 3; k++)
                {
                    var n = site.Offset(DisplaceX[d], DisplaceY[k], DisplaceZ[d]);
                    if (pathfinder.CanStand(n, 1, 1, 2))
                    {
                        if (skipPlots && OnPlot(n, 1, 1, 2)) skipped++;
                        else playerGoals.Add(n);
                        break;
                    }
                }
            return skipped;
        }

        void StartCrop(FarmPlot p)
        {
            if (p.SoilReady)
            {
                BeginSow(p);
            }
            else
            {
                p.Phase = PlotPhase.Till;
                AddPlotStage(p, WorkAbility.Till);
            }
        }

        /// <summary>Kho gần nhất theo đường đi khung 1×1×1 từ from tới ô kề kho (0025); bằng nhau → id nhỏ hơn. Không kho nào tới được → gần nhất theo Manhattan.</summary>
        int NearestStorage(CellPos from)
        {
            var ss = State.Storages;
            int best = -1, bestLen = int.MaxValue;
            for (int i = 0; i < ss.Count; i++)
            {
                storeGoals.Clear();
                for (int d = 0; d < 4; d++)
                    for (int k = 0; k < 3; k++)
                    {
                        var n = ss[i].Cell.Offset(DisplaceX[d], DisplaceY[k], DisplaceZ[d]);
                        if (pathfinder.CanStand(n, 1, 1, 1)) { storeGoals.Add(n); break; }
                    }
                if (!pathfinder.FindPathToAny(from, storeGoals, 1, 1, 1, storePath)) continue;
                if (storePath.Count < bestLen) { bestLen = storePath.Count; best = i; }
            }
            if (best >= 0) return best;
            int bestD = int.MaxValue;
            for (int i = 0; i < ss.Count; i++)
            {
                int d = CellPos.Manhattan(from, ss[i].Cell);
                if (d < bestD) { bestD = d; best = i; }
            }
            return best;
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
                if (g.Stall == StallReason.NoSeeds && State.Stock.Seeds[CropOf(State.Plots[g.PlotId])] > 0
                    || g.Stall == StallReason.StorageFull && State.Stock.Free >= g.LoadProduce + g.LoadSeeds
                    || g.Stall == StallReason.NoAbility && HasHomedAbility(g.Ability))
                {
                    g.Status = StageStatus.Open;
                    g.Stall = StallReason.None;
                }
            }
            for (int i = 0; i < stages.Count; i++)
            {
                var g2 = stages[i];
                if (g2.Status == StageStatus.Open && g2.AssigneeKind == AssigneeKind.None
                    && !(g2.PlotId >= 0 && State.Plots[g2.PlotId].TillPaused)) open.Add(g2);
            }
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
                if (s.PlotId >= 0 && s.Ability == WorkAbility.Sow && State.Stock.Seeds[CropOf(State.Plots[s.PlotId])] <= 0)
                {
                    s.Status = StageStatus.Blocked;
                    s.Stall = StallReason.NoSeeds;
                    continue;
                }
                if (s.StorageId >= 0 && State.Stock.Free < s.LoadProduce + s.LoadSeeds)
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
            if (s.PlotId >= 0 && s.Ability == WorkAbility.Sow) State.Stock.Seeds[CropOf(State.Plots[s.PlotId])]--;
            if (s.StorageId >= 0) State.Stock.Reserved += s.LoadProduce + s.LoadSeeds;
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
            if (s.PlotId >= 0 && s.Ability == WorkAbility.Sow) State.Stock.Seeds[CropOf(State.Plots[s.PlotId])]++;
            if (s.StorageId >= 0) State.Stock.Reserved -= s.LoadProduce + s.LoadSeeds;
            if (s.Carried)
            {
                s.Target = c.Cell;
                s.Carried = false;
                s.DroppedBy = c.Id;
            }
            s.Status = StageStatus.Open;
            s.AssigneeKind = AssigneeKind.None;
            s.AssigneeId = -1;
            ReleaseCreature(c);
        }

        void ReleaseCreature(Creature c)
        {
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
