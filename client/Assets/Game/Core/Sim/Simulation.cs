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
        bool terrainDirty;
        bool homeDirty;

        public Simulation(SimConfig config, WorldData world)
        {
            if (world == null) throw new ArgumentNullException(nameof(world));
            config.Validate();
            Config = config;
            World = world;
            pathfinder = new GridPathfinder(world);
            State = new SimState(new SimRng(config.simSeed, 1UL));
        }

        public int AddCreature(string speciesId, CellPos cell)
        {
            var sp = Config.FindSpecies(speciesId);
            if (!pathfinder.CanStand(cell, sp.sizeX, sp.sizeZ, sp.sizeY))
                throw new ArgumentException("Sinh vật không đứng được tại " + cell);
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
            if (goals.Count == 0) return;
            var cell = goals[0];
            c.Cell = cell;
            c.Path.Clear();
            c.PathIndex = 0;
            c.MoveProgress = 0;
            c.MoveTarget = cell;
            c.StuckSince = -1;
            c.HomeReachable = true;
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
            ReportPhase();
        }

        bool CanStand(Creature c) => pathfinder.CanStand(c.Cell, c.SizeX, c.SizeZ, c.SizeY);

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
                    if (pathfinder.CanStep(c.Cell, n, c.SizeX, c.SizeZ, c.SizeY))
                    {
                        c.Cell = n;
                        c.MoveProgress = 0;
                        return;
                    }
                }

            while (c.Cell.Y - 1 >= 0 && ColumnsBodyFree(c, c.Cell.Y - 1))
                c.Cell = c.Cell.Offset(0, -1, 0);
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

        /// <summary>Ô kề của chỗ p (tối đa 4, theo thứ tự hướng) mà sinh vật c đứng được — điền vào goals (0016).</summary>
        void AdjacentGoals(Creature c, CellPos p)
        {
            goals.Clear();
            for (int d = 0; d < 4; d++)
                for (int k = 0; k < 3; k++)
                {
                    var n = p.Offset(DisplaceX[d], DisplaceY[k], DisplaceZ[d]);
                    if (pathfinder.CanStand(n, c.SizeX, c.SizeZ, c.SizeY))
                    {
                        goals.Add(n);
                        break;
                    }
                }
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
            AdjacentGoals(c, site);
            if (pathfinder.FindPathToAny(c.Cell, goals, c.SizeX, c.SizeZ, c.SizeY, tmpPath)) return true;
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
                if (c.State != CreatureState.Moving) continue;
                c.MoveProgress += c.SpeedPerTick;
                while (c.MoveProgress >= 1000 && c.PathIndex < c.Path.Count)
                {
                    c.Cell = c.Path[c.PathIndex++];
                    c.MoveProgress -= 1000;
                }
                if (c.PathIndex >= c.Path.Count)
                {
                    c.MoveProgress = 0;
                    c.Path.Clear();
                    c.PathIndex = 0;
                    OnArrive(c);
                }
            }
        }

        void OnArrive(Creature c)
        {
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
                    if (c.HomeId < 0 || c.State != CreatureState.Idle || !c.HasAbility(s.Ability)) continue;
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
            c.MoveProgress = 0;
            c.Path.Clear();
            c.PathIndex = 0;
            c.MoveTarget = c.Cell;
        }
    }
}
