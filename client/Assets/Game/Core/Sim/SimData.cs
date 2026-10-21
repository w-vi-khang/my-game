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
        public CellPos Offset(int dx, int dy, int dz) => new CellPos(X + dx, Y + dy, Z + dz);
        public override string ToString() => "(" + X + ", " + Y + ", " + Z + ")";

        public static int Manhattan(CellPos a, CellPos b) => Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y) + Math.Abs(a.Z - b.Z);
    }

    public enum CreatureState : byte { Idle = 0, Moving = 1, Working = 2 }

    public enum StageStatus : byte { Open = 0, Assigned = 1, Done = 2, Blocked = 3, Cancelled = 4 }

    /// <summary>Lý do đình trệ (0016, 0017).</summary>
    public enum StallReason : byte { None = 0, NoPath = 1, NoSeeds = 2, StorageFull = 3, NoAbility = 4 }

    public enum SimEventKind : byte { StallStarted = 1, StallCleared = 2, CreatureRespawned = 3, CropCancelled = 4 }

    /// <summary>Sự kiện thông báo (0017). Đình trệ: CreatureId = -1, Cell = Target của stage. Hiện lại: StageId = -1, Reason = None, Cell = ô hiện lại. Huỷ vụ: Cell = Soil, Seeds = số hạt trả, Reason = None.</summary>
    public readonly struct SimEvent
    {
        public readonly long Tick;
        public readonly SimEventKind Kind;
        public readonly int StageId, CreatureId;
        public readonly CellPos Cell;
        public readonly StallReason Reason;
        public readonly int Seeds;

        public SimEvent(long tick, SimEventKind kind, int stageId, int creatureId, CellPos cell, StallReason reason, int seeds = 0)
        {
            Tick = tick;
            Kind = kind;
            StageId = stageId;
            CreatureId = creatureId;
            Cell = cell;
            Reason = reason;
            Seeds = seeds;
        }
    }

    /// <summary>Cách tới đích: Exact = đứng đúng ô; Adjacent = đứng ở ô kề; AdjacentOrOn = thử ô kề, không được thì đứng trên ô (0016).</summary>
    public enum Approach : byte { Exact = 0, Adjacent = 1, AdjacentOrOn = 2 }

    public enum PlotPhase : byte { Idle = 0, Till = 1, Sow = 2, Water = 3, Growing = 4, Harvest = 5 }

    /// <summary>Player: người chơi nhận việc qua tab nhiệm vụ — M2 (0014); M1 chỉ có trường.</summary>
    public enum AssigneeKind : byte { None = 0, Creature = 1, Player = 2 }

    public sealed class Creature
    {
        public readonly int Id;
        public readonly string SpeciesId;
        public readonly WorkAbility Ability1, Ability2;
        public readonly int Mastery1, Mastery2;
        public readonly int SpeedPerTick;
        public readonly int SizeX, SizeZ, SizeY;
        public readonly List<CellPos> Path = new List<CellPos>();
        public int PathIndex;

        public CellPos Cell;
        public CreatureState State;
        public int StageId = -1;
        public int HomeId = -1;
        public bool HomeReachable;
        public long StuckSince = -1;
        public CellPos MoveTarget;
        public int MoveProgress;
        /// <summary>Số nhịp liên tiếp bị sinh vật khác chắn ô kế tiếp (0019).</summary>
        public int BlockedTicks;
        /// <summary>Đang đi xuyên (phao, 0019): bỏ qua luật chặn tới khi khung không còn chồng lên sinh vật khác.</summary>
        public bool Phasing;
        /// <summary>Đang nhường đường cho sinh vật có id này (đi tới ô tránh hoặc chờ ở đó); -1 = không.</summary>
        public int YieldTo = -1;
        /// <summary>Đã tới ô tránh, chờ con kia đi qua rồi mới đi tiếp.</summary>
        public bool YieldHold;
        /// <summary>Nhịp hẹn đi lang thang (con rảnh trong bán kính quanh nhà, 0019); -1 = chưa hẹn.</summary>
        public long WanderAt = -1;

        public Creature(int id, string speciesId, WorkAbility ability1, WorkAbility ability2, int mastery1, int mastery2, int speedPerTick, int sizeX, int sizeZ, int sizeY, CellPos cell)
        {
            Id = id;
            SpeciesId = speciesId;
            Ability1 = ability1;
            Ability2 = ability2;
            Mastery1 = mastery1;
            Mastery2 = mastery2;
            SpeedPerTick = speedPerTick;
            SizeX = sizeX;
            SizeZ = sizeZ;
            SizeY = sizeY;
            Cell = cell;
        }

        public bool Contains(int x, int y, int z) =>
            x >= Cell.X && x < Cell.X + SizeX && z >= Cell.Z && z < Cell.Z + SizeZ && y >= Cell.Y && y < Cell.Y + SizeY;

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
        public StallReason Stall;
        public StallReason ReportedStall;
        public int DroppedBy = -1;
        public Approach TargetApproach;
        public readonly Approach DestinationApproach;
        /// <summary>>= 0 chỉ cho công đoạn xới/gieo/tưới/thu hoạch của ô ruộng; vận chuyển của ruộng có PlotId = -1, StorageId >= 0.</summary>
        public readonly int PlotId;
        public readonly int StorageId;
        public readonly int LoadProduce, LoadSeeds;
        /// <summary>Loại cây của hàng chở; -1 = không phải vận chuyển nông sản.</summary>
        public readonly int CropId;

        public long WorkRequired => (long)BaseTicks * 1000;
        public int ProgressPermille => (int)(WorkDone * 1000 / WorkRequired);

        public WorkStage(int id, int orderId, WorkAbility ability, CellPos target, bool hasDestination, CellPos destination, int baseTicks, int priority,
            Approach targetApproach = Approach.Exact, Approach destinationApproach = Approach.Exact,
            int plotId = -1, int storageId = -1, int loadProduce = 0, int loadSeeds = 0, int cropId = -1)
        {
            Id = id;
            OrderId = orderId;
            Ability = ability;
            Target = target;
            HasDestination = hasDestination;
            Destination = destination;
            BaseTicks = baseTicks;
            Priority = priority;
            TargetApproach = targetApproach;
            DestinationApproach = destinationApproach;
            PlotId = plotId;
            StorageId = storageId;
            LoadProduce = loadProduce;
            LoadSeeds = loadSeeds;
            CropId = cropId;
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

    /// <summary>Chỗ đặt kho; hàng nằm ở kho chung <see cref="SimState.Stock"/> (0025).</summary>
    public sealed class Storage
    {
        public readonly int Id;
        public readonly CellPos Cell;
        public readonly int Capacity;

        public Storage(int id, CellPos cell, int capacity)
        {
            Id = id;
            Cell = cell;
            Capacity = capacity;
        }
    }

    /// <summary>Kho chung (0025): hàng của mọi kho tính chung, sức chứa = tổng sức chứa các kho. Mảng theo loại cây.</summary>
    public sealed class Stockpile
    {
        public int Capacity;
        public readonly int[] Seeds, Produce;
        public int Reserved;

        public Stockpile(int cropCount)
        {
            Seeds = new int[cropCount];
            Produce = new int[cropCount];
        }

        public int SeedsTotal { get { int n = 0; for (int i = 0; i < Seeds.Length; i++) n += Seeds[i]; return n; } }
        public int ProduceTotal { get { int n = 0; for (int i = 0; i < Produce.Length; i++) n += Produce[i]; return n; } }
        public int Free => Capacity - SeedsTotal - ProduceTotal - Reserved;
    }

    public sealed class FarmPlot
    {
        public readonly int Id, OrderId;
        public readonly CellPos Soil, Site;
        public readonly List<int> Queue = new List<int>();
        public PlotPhase Phase;
        public int StageId = -1;
        public long RipeTick;
        /// <summary>Đã thành ô ruộng (0020) — xới xong lần đầu.</summary>
        public bool Tilled;
        /// <summary>Trạng thái "đã xới" (0025): xới xong bật, thu hoạch xong tắt. Vụ bắt đầu khi bật → gieo, tắt → xới.</summary>
        public bool SoilReady;
        /// <summary>Đã rời lệnh sau khi huỷ vụ (0020) — không còn công đoạn, không nhận vụ mới.</summary>
        public bool Removed;

        public FarmPlot(int id, int orderId, CellPos soil)
        {
            Id = id;
            OrderId = orderId;
            Soil = soil;
            Site = soil.Offset(0, 1, 0);
        }
    }

    /// <summary>Nhà mẫu M1 — khối đặc (0017). Cell = góc nhỏ nhất.</summary>
    public sealed class House
    {
        public readonly int Id;
        public readonly CellPos Cell;
        public readonly int SizeX, SizeZ, SizeY, Capacity;
        public readonly List<int> Residents = new List<int>();

        public House(int id, CellPos cell, int sizeX, int sizeZ, int sizeY, int capacity)
        {
            Id = id;
            Cell = cell;
            SizeX = sizeX;
            SizeZ = sizeZ;
            SizeY = sizeY;
            Capacity = capacity;
        }

        public bool Contains(int x, int y, int z) =>
            x >= Cell.X && x < Cell.X + SizeX && z >= Cell.Z && z < Cell.Z + SizeZ && y >= Cell.Y && y < Cell.Y + SizeY;
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
        public readonly Stockpile Stock;
        public readonly List<FarmPlot> Plots = new List<FarmPlot>();
        public readonly List<House> Houses = new List<House>();
        /// <summary>Sự kiện thông báo — mô phỏng chỉ thêm vào, nơi đọc tự Clear. Không tính vào mã băm.</summary>
        public readonly List<SimEvent> Events = new List<SimEvent>();

        public SimState(SimRng rng, int cropCount = 1)
        {
            Rng = rng;
            Stock = new Stockpile(cropCount);
        }
    }
}
