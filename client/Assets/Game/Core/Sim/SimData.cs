using System;
using System.Collections.Generic;

namespace Game.Core.Sim
{
    public readonly struct CellPos : IEquatable<CellPos>
    {
        public readonly int X, Y, Z;

        public CellPos(int x, int y, int z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public bool Equals(CellPos o) => X == o.X && Y == o.Y && Z == o.Z;
        public override bool Equals(object obj) => obj is CellPos o && Equals(o);
        public override int GetHashCode() => unchecked((X * 397 ^ Y) * 397 ^ Z);
        public static bool operator ==(CellPos a, CellPos b) => a.Equals(b);
        public static bool operator !=(CellPos a, CellPos b) => !a.Equals(b);
        public override string ToString() => "(" + X + ", " + Y + ", " + Z + ")";

        public static int Manhattan(CellPos a, CellPos b) => Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y) + Math.Abs(a.Z - b.Z);
    }

    public enum CreatureState : byte { Idle = 0, Moving = 1, Working = 2 }

    public enum StageStatus : byte { Open = 0, Assigned = 1, Done = 2 }

    /// <summary>Player: người chơi nhận việc qua tab nhiệm vụ — M2 (0014); M1 chỉ có trường.</summary>
    public enum AssigneeKind : byte { None = 0, Creature = 1, Player = 2 }

    public sealed class Creature
    {
        public readonly int Id;
        public readonly string SpeciesId;
        public readonly WorkAbility Ability1, Ability2;
        public readonly int Mastery1, Mastery2;
        public readonly int SpeedPerTick;

        public CellPos Cell;
        public CreatureState State;
        public int StageId = -1;
        public CellPos MoveTarget;
        public int MoveProgress;

        public Creature(int id, string speciesId, WorkAbility ability1, WorkAbility ability2, int mastery1, int mastery2, int speedPerTick, CellPos cell)
        {
            Id = id;
            SpeciesId = speciesId;
            Ability1 = ability1;
            Ability2 = ability2;
            Mastery1 = mastery1;
            Mastery2 = mastery2;
            SpeedPerTick = speedPerTick;
            Cell = cell;
        }

        public bool HasAbility(WorkAbility a) => a == Ability1 || a == Ability2;

        public int MasteryFor(WorkAbility a) => a == Ability1 ? Mastery1 : a == Ability2 ? Mastery2 : 0;
    }

    /// <summary>Target = nơi bắt đầu (vận chuyển: nơi lấy hàng, hoặc nơi hàng đang nằm). Carried = hàng đang được mang.</summary>
    public sealed class WorkStage
    {
        public readonly int Id, OrderId;
        public readonly WorkAbility Ability;
        public readonly bool HasDestination;
        public readonly CellPos Destination;
        public readonly int BaseTicks;
        public readonly int Priority;

        public CellPos Target;
        public long WorkDone;
        public StageStatus Status;
        public AssigneeKind AssigneeKind;
        public int AssigneeId = -1;
        public bool Carried;

        public long WorkRequired => (long)BaseTicks * 1000;
        public int ProgressPermille => (int)(WorkDone * 1000 / WorkRequired);

        public WorkStage(int id, int orderId, WorkAbility ability, CellPos target, bool hasDestination, CellPos destination, int baseTicks, int priority)
        {
            Id = id;
            OrderId = orderId;
            Ability = ability;
            Target = target;
            HasDestination = hasDestination;
            Destination = destination;
            BaseTicks = baseTicks;
            Priority = priority;
        }
    }

    public sealed class WorkOrder
    {
        public readonly int Id;
        public readonly List<CellPos> Area;
        public readonly List<int> StageIds = new List<int>();

        public WorkOrder(int id, List<CellPos> area)
        {
            Id = id;
            Area = area;
        }
    }

    public sealed class Storage
    {
        public readonly int Id;
        public readonly CellPos Cell;
        public readonly int Capacity;
        public int Seeds;
        public int Produce;

        public Storage(int id, CellPos cell, int capacity, int seeds)
        {
            Id = id;
            Cell = cell;
            Capacity = capacity;
            Seeds = seeds;
        }
    }

    /// <summary>Quy ước: id = vị trí trong list.</summary>
    public sealed class SimState
    {
        public long Tick;
        public readonly SimRng Rng;
        public readonly List<Creature> Creatures = new List<Creature>();
        public readonly List<WorkStage> Stages = new List<WorkStage>();
        public readonly List<WorkOrder> Orders = new List<WorkOrder>();
        public readonly List<Storage> Storages = new List<Storage>();

        public SimState(SimRng rng)
        {
            Rng = rng;
        }
    }
}
