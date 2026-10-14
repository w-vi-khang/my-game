using System;
using System.Collections.Generic;

namespace Game.Core.Sim
{
    /// <summary>Mô phỏng nông trại M1 — nhịp cố định, số nguyên, tất định (simulation-tech bước 1, 0014). Đi theo khoảng cách ô, chưa có A* (bước 2).</summary>
    public sealed class Simulation
    {
        public readonly SimConfig Config;
        public readonly SimState State;

        public Simulation(SimConfig config)
        {
            config.Validate();
            Config = config;
            State = new SimState(new SimRng(config.simSeed, 1UL));
        }

        public int AddCreature(string speciesId, CellPos cell)
        {
            var sp = Config.FindSpecies(speciesId);
            var c = new Creature(State.Creatures.Count, speciesId,
                SimConfig.ParseAbility(sp.ability1), SimConfig.ParseAbility(sp.ability2),
                sp.mastery1, sp.mastery2, Config.SpeedPerTick(sp), cell)
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

        public void Tick()
        {
            State.Tick++;
            WorkPhase();
            MovePhase();
            AssignPhase();
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
                while (c.MoveProgress >= 1000 && c.Cell != c.MoveTarget)
                {
                    c.Cell = StepToward(c.Cell, c.MoveTarget);
                    c.MoveProgress -= 1000;
                }
                if (c.Cell == c.MoveTarget)
                {
                    c.MoveProgress = 0;
                    OnArrive(c);
                }
            }
        }

        static CellPos StepToward(CellPos from, CellPos to)
        {
            if (from.X != to.X) return new CellPos(from.X + Math.Sign(to.X - from.X), from.Y, from.Z);
            if (from.Z != to.Z) return new CellPos(from.X, from.Y, from.Z + Math.Sign(to.Z - from.Z));
            return new CellPos(from.X, from.Y + Math.Sign(to.Y - from.Y), from.Z);
        }

        void OnArrive(Creature c)
        {
            var s = State.Stages[c.StageId];
            if (s.HasDestination && !s.Carried)
            {
                s.Carried = true;
                if (c.Cell == s.Destination) c.State = CreatureState.Working;
                else c.MoveTarget = s.Destination;
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
            var candidates = new List<Creature>();
            for (int k = 0; k < open.Count; k++)
            {
                var s = open[k];
                candidates.Clear();
                long best = long.MaxValue;
                for (int i = 0; i < cs.Count; i++)
                {
                    var c = cs[i];
                    if (c.State != CreatureState.Idle || !c.HasAbility(s.Ability)) continue;
                    long score = (long)CellPos.Manhattan(c.Cell, s.Target) * 100000 / c.MasteryFor(s.Ability);
                    if (score < best)
                    {
                        best = score;
                        candidates.Clear();
                        candidates.Add(c);
                    }
                    else if (score == best)
                    {
                        candidates.Add(c);
                    }
                }
                if (candidates.Count == 0) continue;
                var chosen = candidates.Count == 1 ? candidates[0] : candidates[State.Rng.NextInt(candidates.Count)];
                Assign(chosen, s);
            }
        }

        void Assign(Creature c, WorkStage s)
        {
            s.Status = StageStatus.Assigned;
            s.AssigneeKind = AssigneeKind.Creature;
            s.AssigneeId = c.Id;
            c.StageId = s.Id;
            c.MoveProgress = 0;
            if (c.Cell == s.Target)
            {
                c.State = CreatureState.Moving;
                OnArrive(c);
            }
            else
            {
                c.State = CreatureState.Moving;
                c.MoveTarget = s.Target;
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
        }
    }
}
