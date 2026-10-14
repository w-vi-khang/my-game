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
        bool terrainDirty;

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
            return false;
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
            }
            WorkPhase();
            MovePhase();
            AssignPhase();
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
            if (FindPath(c, c.Cell, c.MoveTarget))
            {
                CopyPath(c);
                c.MoveProgress = 0;
            }
            else
            {
                BlockNoPath(c);
            }
        }

        void CopyPath(Creature c)
        {
            c.Path.Clear();
            for (int i = 0; i < tmpPath.Count; i++) c.Path.Add(tmpPath[i]);
            c.PathIndex = 0;
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
                s.WorkDone += 1000;
                if (s.WorkDone >= s.WorkRequired)
                {
                    s.WorkDone = s.WorkRequired;
                    s.Status = StageStatus.Done;
                    s.Carried = false;
                    c.State = CreatureState.Idle;
                    c.StageId = -1;
                }
            }
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
                if (c.Cell == s.Destination)
                {
                    c.State = CreatureState.Working;
                }
                else if (FindPath(c, c.Cell, s.Destination))
                {
                    c.MoveTarget = s.Destination;
                    CopyPath(c);
                }
                else
                {
                    BlockNoPath(c);
                }
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
                eligible.Clear();
                scores.Clear();
                for (int i = 0; i < cs.Count; i++)
                {
                    var c = cs[i];
                    if (c.State != CreatureState.Idle || !c.HasAbility(s.Ability)) continue;
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
                    if (pick.Cell == s.Target)
                    {
                        Assign(pick, s, null);
                        assigned = true;
                        break;
                    }
                    if (FindPath(pick, pick.Cell, s.Target))
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
            s.Status = StageStatus.Assigned;
            s.AssigneeKind = AssigneeKind.Creature;
            s.AssigneeId = c.Id;
            c.StageId = s.Id;
            c.MoveProgress = 0;
            c.State = CreatureState.Moving;
            if (c.Cell == s.Target)
            {
                c.Path.Clear();
                c.PathIndex = 0;
                OnArrive(c);
            }
            else
            {
                c.MoveTarget = s.Target;
                c.Path.Clear();
                for (int i = 0; i < path.Count; i++) c.Path.Add(path[i]);
                c.PathIndex = 0;
            }
        }

        public void Unassign(int creatureId)
        {
            var c = State.Creatures[creatureId];
            if (c.StageId < 0) return;
            var s = State.Stages[c.StageId];
            if (s.Carried)
            {
                s.Target = c.Cell;
                s.Carried = false;
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
