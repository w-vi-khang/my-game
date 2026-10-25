using System;
using System.Collections.Generic;

namespace Game.Core
{
    public enum EditResult
    {
        Ok,
        OutOfBounds,
        /// <summary>Đào: ô rỗng, nước, đá đáy hoặc khối cốt truyện.</summary>
        NotDiggable,
        /// <summary>Đặt: ô đã có khối đặc (rỗng hoặc nước thì đặt được — 0012).</summary>
        NotEmpty,
        /// <summary>Đặt: loại vật liệu không đặt được (rỗng, nước, đá đáy).</summary>
        NotPlaceable,
        /// <summary>Đặt: túi hết vật liệu loại này.</summary>
        NoMaterial,
        /// <summary>Đặt: ô đang có người chơi, sinh vật, cây hoặc kho, hoặc có vật rơi không còn chỗ đẩy lên.</summary>
        Occupied,
        /// <summary>Đặt: ô không kề mặt khối đặc nào (0012).</summary>
        NotAttached,
        /// <summary>Ô ngoài tầm với của nhân vật (world 7h) — client trả, editor không dùng.</summary>
        OutOfReach,
    }

    /// <summary>
    /// Đào/đặt nguyên ô theo luật M1 (design/02-gameplay/mechanics.md "Voxel world — luật M1") và tính chunk cần dựng lại
    /// (design/04-tech/world-tech.md "Chỉnh sửa"). Biển tràn + luật đặt khối theo design/decisions/0012: đặt vào ô rỗng hoặc ô
    /// nước kề mặt một khối đặc; đào ô ≤ mực nước biển kề nước (hoặc biển ngoài map) → nước lan qua các ô rỗng ≤ mực nước
    /// biển thông với nó (6 hướng) theo nhịp (design/decisions/0013): StepWater() một nhịp — rơi thẳng xuống ngay, lan ngang/dâng
    /// 1 ô mỗi nhịp. Nước biển (nguồn) không tự rút. Nước chảy (design/decisions/0040): nguồn (ô nước có sẵn, ô biển loang) không cạn;
    /// nước rơi thẳng xuống cả cột, lan ngang tối đa FlowMaxDistance ô (1 ô/nhịp) khi đứng trên khối đặc/nước; mất nguồn thì rút dần.
    /// </summary>
    public sealed class WorldEditor
    {
        public readonly WorldData World;
        public readonly PlayerBag Bag;

        /// <summary>Vật rơi trên world (0028, task contract 7e).</summary>
        public readonly DropField Drops;

        /// <summary>Số ô quanh chunk mà bộ dựng mesh đọc (đệm). Sửa ô trong vùng này của chunk bên cạnh → chunk đó cũng bẩn.</summary>
        public readonly int MeshMargin;

        /// <summary>Mực nước biển S (ô y ≤ S có thể ngập); &lt; 0 → không có biển loang.</summary>
        public readonly int SeaLevel;

        /// <summary>Mặt trước dòng nước: ô sẽ ngập ở nhịp sau (nếu khi đó còn rỗng). Thứ tự chèn cố định → tất định.</summary>
        readonly List<(int x, int y, int z)> waterFront = new List<(int, int, int)>();
        readonly HashSet<(int, int, int)> waterFrontSet = new HashSet<(int, int, int)>();

        /// <summary>Ô cần tính lại nước chảy ở nhịp sau (0040). Thứ tự chèn cố định → tất định.</summary>
        readonly List<(int x, int y, int z)> flowQueue = new List<(int, int, int)>();
        readonly HashSet<(int, int, int)> flowQueueSet = new HashSet<(int, int, int)>();

        int flowMaxDistance = 7;

        /// <summary>Số ô nước chảy lan ngang tối đa N (0040), hợp lệ 1–7.</summary>
        public int FlowMaxDistance
        {
            get => flowMaxDistance;
            set
            {
                if (value < 1 || value > Cell.MaxWaterLevel)
                    throw new ArgumentOutOfRangeException(nameof(value), value, "FlowMaxDistance must be in 1..7");
                flowMaxDistance = value;
            }
        }

        int narrowMaxOpenSides = 2;

        /// <summary>Ô nước hẹp khi số ô ngang không đặc ≤ giá trị này (0041), hợp lệ 0–3.</summary>
        public int NarrowMaxOpenSides
        {
            get => narrowMaxOpenSides;
            set
            {
                if (value < 0 || value > 3)
                    throw new ArgumentOutOfRangeException(nameof(value), value, "NarrowMaxOpenSides must be in 0..3");
                narrowMaxOpenSides = value;
            }
        }

        int supportCheckMaxCells = 4096;

        /// <summary>Khối nước lớn hơn số này thì bỏ kiểm nguồn (0041), hợp lệ 64–65536.</summary>
        public int SupportCheckMaxCells
        {
            get => supportCheckMaxCells;
            set
            {
                if (value < 64 || value > 65536)
                    throw new ArgumentOutOfRangeException(nameof(value), value, "SupportCheckMaxCells must be in 64..65536");
                supportCheckMaxCells = value;
            }
        }

        /// <summary>Ô cần kiểm khối nước còn nguồn không (0041): kề ô vừa từ nước thành không phải nước.</summary>
        readonly List<(int x, int y, int z)> supportStarts = new List<(int, int, int)>();
        readonly HashSet<(int, int, int)> supportStartSet = new HashSet<(int, int, int)>();

        /// <summary>Còn nước đang lan hoặc đang rút (gọi StepWater theo nhịp).</summary>
        public bool HasPendingWater => waterFront.Count > 0 || flowQueue.Count > 0 || supportStarts.Count > 0;

        /// <summary>Móc kiểm tra ô bị chiếm (người chơi, sinh vật, cây, kho). M1 bước 4 chưa có vật thể → mặc định null.</summary>
        public Func<int, int, int, bool> IsOccupied;

        /// <summary>Móc ô bị nhân vật người chơi chiếm (world 7h) — tách khỏi IsOccupied vì Simulation.AttachEditor ghi đè móc đó.</summary>
        public Func<int, int, int, bool> IsPlayerAt;

        /// <summary>Ô (x, y, z) vừa đổi — cho tìm đường, môi trường sống, hiển thị.</summary>
        public event Action<int, int, int> CellChanged;

        /// <summary>Ô nước đổi mức/cờ mà vật liệu không đổi (0042) — client dựng lại mesh; mô phỏng không nghe.</summary>
        public event Action<int, int, int> WaterSurfaceChanged;

        public WorldEditor(WorldData world, PlayerBag bag, int meshMargin, int seaLevel = -1)
        {
            SeaLevel = seaLevel;
            if (meshMargin < 0 || meshMargin >= WorldSize.ChunkSize)
                throw new ArgumentOutOfRangeException(nameof(meshMargin), meshMargin, "Margin must be in 0..ChunkSize-1");
            World = world ?? throw new ArgumentNullException(nameof(world));
            Bag = bag ?? throw new ArgumentNullException(nameof(bag));
            Drops = new DropField(World);
            MeshMargin = meshMargin;
        }

        public static bool IsPlaceable(BlockMaterial m) =>
            m == BlockMaterial.Grass || m == BlockMaterial.Dirt || m == BlockMaterial.Stone || m == BlockMaterial.Sand;

        /// <summary>Đào: ô thành rỗng, rơi ra 1 vật rơi cùng vật liệu tại ô (ô ruộng → đất, 0020; vật rơi 0028); túi không đổi.</summary>
        public EditResult TryDig(int x, int y, int z)
        {
            if (!World.Size.InBounds(x, y, z))
                return EditResult.OutOfBounds;
            ushort cell = World.Get(x, y, z);
            if (!Cell.IsDiggable(cell))
                return EditResult.NotDiggable;

            var material = Cell.GetMaterial(cell);
            World.Set(x, y, z, Cell.SetModified(Cell.SetMaterial(cell, BlockMaterial.Empty), true));
            Drops.Add(x, y, z, ItemId.Block(material == BlockMaterial.Farmland ? BlockMaterial.Dirt : material), 1);
            Drops.OnCellCleared(x, y, z);
            CellChanged?.Invoke(x, y, z);
            if (SeaLevel >= 0 && y <= SeaLevel && TouchesWater(x, y, z))
                AddToFront(x, y, z);
            AddFlowDependents(x, y, z);
            AddFlowRing2(x, y, z);
            return EditResult.Ok;
        }

        /// <summary>Kiểm đặt (không sửa world, không trừ kho): cùng kết quả và thứ tự kiểm như TryPlace (brief 0027 — bản mờ).</summary>
        public EditResult CheckPlace(int x, int y, int z, BlockMaterial material)
        {
            if (!World.Size.InBounds(x, y, z))
                return EditResult.OutOfBounds;
            if (!IsPlaceable(material))
                return EditResult.NotPlaceable;
            var current = World.GetMaterial(x, y, z);
            if (current != BlockMaterial.Empty && current != BlockMaterial.Water)
                return EditResult.NotEmpty;
            if (!HasSolidNeighbor(x, y, z))
                return EditResult.NotAttached;
            if (IsOccupied != null && IsOccupied(x, y, z))
                return EditResult.Occupied;
            if (IsPlayerAt != null && IsPlayerAt(x, y, z))
                return EditResult.Occupied;
            if (!Drops.CanPushUp(x, y, z))
                return EditResult.Occupied;
            if (Bag.Get(material) <= 0)
                return EditResult.NoMaterial;
            return EditResult.Ok;
        }

        /// <summary>Đặt: vào ô rỗng hoặc ô nước (nước ô đó mất) kề mặt một khối đặc, tiêu 1 vật liệu từ túi; vật rơi trong ô bị đẩy lên (task contract 7e).</summary>
        public EditResult TryPlace(int x, int y, int z, BlockMaterial material)
        {
            var check = CheckPlace(x, y, z, material);
            if (check != EditResult.Ok)
                return check;
            if (!Bag.TryTake(material))
                return EditResult.NoMaterial;
            ushort cell = World.Get(x, y, z);
            bool wasWater = Cell.GetMaterial(cell) == BlockMaterial.Water;

            World.Set(x, y, z, Cell.SetModified(Cell.ClearFlow(Cell.SetWaterLevel(Cell.SetMaterial(cell, material), 0)), true));
            Drops.PushUp(x, y, z);
            CellChanged?.Invoke(x, y, z);
            RevertFarmlandBelow(x, y, z);
            AddFlowDependents(x, y, z);
            AddFlowRing2(x, y, z);
            if (wasWater)
                AddSupportNeighbors(x, y, z);
            return EditResult.Ok;
        }

        /// <summary>Xới (0020): ô đất/cỏ → ô ruộng, cờ đã sửa bật. Chỉ ghi dữ liệu, không phát sự kiện. true khi ô đổi.</summary>
        public static bool Till(WorldData world, int x, int y, int z)
        {
            if (!world.Size.InBounds(x, y, z)) return false;
            ushort cell = world.Get(x, y, z);
            var m = Cell.GetMaterial(cell);
            if (m != BlockMaterial.Dirt && m != BlockMaterial.Grass) return false;
            world.Set(x, y, z, Cell.SetModified(Cell.SetMaterial(cell, BlockMaterial.Farmland), true));
            return true;
        }

        /// <summary>Xới qua editor: như Till, ô đổi thì phát CellChanged (mesh, minimap cập nhật).</summary>
        public bool TryTill(int x, int y, int z)
        {
            if (!Till(World, x, y, z)) return false;
            CellChanged?.Invoke(x, y, z);
            return true;
        }

        /// <summary>Phép Mưa (0028): ô đất có ô trên rỗng (trong map) → cỏ, cờ đã sửa bật. Chỉ ghi dữ liệu, không phát sự kiện. true khi ô đổi.</summary>
        public static bool Grow(WorldData world, int x, int y, int z)
        {
            if (!world.Size.InBounds(x, y, z) || !world.Size.InBounds(x, y + 1, z)) return false;
            ushort cell = world.Get(x, y, z);
            if (Cell.GetMaterial(cell) != BlockMaterial.Dirt) return false;
            if (Cell.GetMaterial(world.Get(x, y + 1, z)) != BlockMaterial.Empty) return false;
            world.Set(x, y, z, Cell.SetModified(Cell.SetMaterial(cell, BlockMaterial.Grass), true));
            return true;
        }

        /// <summary>Như Grow qua editor: ô đổi thì phát CellChanged.</summary>
        public bool TryGrow(int x, int y, int z)
        {
            if (!Grow(World, x, y, z)) return false;
            CellChanged?.Invoke(x, y, z);
            return true;
        }

        /// <summary>Ô (x, y, z) vừa có khối hoặc nước: ô ruộng ngay bên dưới trở lại ô đất (0020).</summary>
        void RevertFarmlandBelow(int x, int y, int z)
        {
            if (y - 1 < 0) return;
            ushort below = World.Get(x, y - 1, z);
            if (Cell.GetMaterial(below) != BlockMaterial.Farmland) return;
            World.Set(x, y - 1, z, Cell.SetModified(Cell.SetMaterial(below, BlockMaterial.Dirt), true));
            CellChanged?.Invoke(x, y - 1, z);
        }

        static readonly (int dx, int dy, int dz)[] Faces = { (1, 0, 0), (-1, 0, 0), (0, 1, 0), (0, -1, 0), (0, 0, 1), (0, 0, -1) };

        /// <summary>Ô đặc = trong map và không rỗng/không nước. Dưới đáy map coi là đặc; ngoài map theo x/z là biển (không đặc).</summary>
        bool IsSolid(int x, int y, int z) => IsSolidAt(World, x, y, z);

        /// <summary>Như IsSolid nhưng tĩnh, dùng cho DropField.</summary>
        public static bool IsSolidAt(WorldData world, int x, int y, int z)
        {
            if (y < 0)
                return true;
            if (!world.Size.InBounds(x, y, z))
                return false;
            var m = world.GetMaterial(x, y, z);
            return m != BlockMaterial.Empty && m != BlockMaterial.Water;
        }

        bool HasSolidNeighbor(int x, int y, int z)
        {
            foreach (var (dx, dy, dz) in Faces)
                if (IsSolid(x + dx, y + dy, z + dz))
                    return true;
            return false;
        }

        /// <summary>Ô (x, y, z) là nguồn nước: ô nước trong map, hoặc biển ngoài map theo x/z ở y ≤ SeaLevel.</summary>
        bool IsWaterSource(int x, int y, int z)
        {
            if (y < 0 || y > SeaLevel || y >= World.Size.Y)
                return false;
            if (x < 0 || z < 0 || x >= World.Size.X || z >= World.Size.Z)
                return true;
            return World.GetMaterial(x, y, z) == BlockMaterial.Water && Cell.IsWaterSource(World.Get(x, y, z));
        }

        bool TouchesWater(int x, int y, int z)
        {
            foreach (var (dx, dy, dz) in Faces)
                if (IsWaterSource(x + dx, y + dy, z + dz))
                    return true;
            return false;
        }

        bool IsFloodable(int x, int y, int z) =>
            y <= SeaLevel && World.Size.InBounds(x, y, z) && World.GetMaterial(x, y, z) == BlockMaterial.Empty;

        void AddToFront(int x, int y, int z)
        {
            if (waterFrontSet.Add((x, y, z)))
                waterFront.Add((x, y, z));
        }

        void FillWater(int x, int y, int z)
        {
            ushort old = World.Get(x, y, z);
            World.Set(x, y, z, Cell.SetModified(Cell.SetWaterLevel(Cell.SetMaterial(old, BlockMaterial.Water), Cell.MaxWaterLevel), true));
            CellChanged?.Invoke(x, y, z);
            RevertFarmlandBelow(x, y, z);
            AddFlowDependents(x, y, z);
        }

        /// <summary>
        /// Một nhịp nước lan (0013): mọi ô ở mặt trước còn rỗng thành nước; từ mỗi ô vừa ngập, nước rơi thẳng xuống qua các ô rỗng
        /// ≤ SeaLevel ngay trong nhịp; ô rỗng ≤ SeaLevel kề ngang hoặc phía trên các ô vừa ngập vào mặt trước nhịp sau.
        /// Ô ở mặt trước đã bị đặt khối thì bỏ qua (khối chặn dòng nước). Trả về số ô ngập trong nhịp.
        /// </summary>
        int StepSea()
        {
            if (waterFront.Count == 0)
                return 0;
            var front = waterFront.ToArray();
            waterFront.Clear();
            waterFrontSet.Clear();
            var filled = new List<(int x, int y, int z)>();
            foreach (var (x, y, z) in front)
            {
                if (!IsFloodable(x, y, z))
                    continue;
                FillWater(x, y, z);
                filled.Add((x, y, z));
                for (int fy = y - 1; IsFloodable(x, fy, z); fy--)
                {
                    FillWater(x, fy, z);
                    filled.Add((x, fy, z));
                }
            }
            foreach (var (x, y, z) in filled)
                foreach (var (dx, dy, dz) in Faces)
                    if (dy >= 0 && IsFloodable(x + dx, y + dy, z + dz))
                        AddToFront(x + dx, y + dy, z + dz);
            return filled.Count;
        }

        /// <summary>Một nhịp nước: biển loang (0013) rồi nước chảy (0040). Trả tổng số ô đổi trong nhịp.</summary>
        public int StepWater() => StepSea() + StepFlow();

        void AddFlow(int x, int y, int z)
        {
            if (World.Size.InBounds(x, y, z) && flowQueueSet.Add((x, y, z)))
                flowQueue.Add((x, y, z));
        }

        void AddFlowRing(int x, int y, int z)
        {
            AddFlow(x + 1, y, z);
            AddFlow(x - 1, y, z);
            AddFlow(x, y, z + 1);
            AddFlow(x, y, z - 1);
        }

        /// <summary>Ô phụ thuộc của ô P vừa đổi (0040 QĐ 6): P, ô dưới, 4 ô ngang của P, của ô trên P và của ô cao hơn P hai ô.</summary>
        void AddFlowDependents(int x, int y, int z)
        {
            AddFlow(x, y, z);
            AddFlow(x, y - 1, z);
            AddFlowRing(x, y, z);
            AddFlowRing(x, y + 1, z);
            AddFlowRing(x, y + 2, z);
        }

        static readonly (int dx, int dz)[] Ring2 = { (2, 0), (-2, 0), (0, 2), (0, -2), (1, 1), (1, -1), (-1, 1), (-1, -1) };

        /// <summary>Độ hẹp của ô kề P phụ thuộc 8 ô cách P hai bước ngang (0041 QĐ 6).</summary>
        void AddFlowRing2(int x, int y, int z)
        {
            foreach (var (dx, dz) in Ring2)
                AddFlow(x + dx, y, z + dz);
        }

        /// <summary>Đưa ô (x, y, z) vào hàng chờ tính lại (thác demo có sẵn khi sinh world, 0041).</summary>
        public void WakeWater(int x, int y, int z) => AddFlowDependents(x, y, z);

        void AddSupportNeighbors(int x, int y, int z)
        {
            foreach (var (dx, dy, dz) in Faces)
            {
                var p = (x + dx, y + dy, z + dz);
                if (supportStartSet.Add(p))
                    supportStarts.Add(p);
            }
        }

        bool IsNarrow(int x, int y, int z)
        {
            int open = 0;
            foreach (var (dx, dz) in Sides)
                if (!IsSolidAt(World, x + dx, y, z + dz))
                    open++;
            return open <= narrowMaxOpenSides;
        }

        /// <summary>Ô (x, y, z) là nguồn nuôi khối nước: ô nước nguồn trong map, hoặc biển ngoài map theo x/z ở y ≤ SeaLevel (chỉ xét ô ngang).</summary>
        bool IsSupportSource(int x, int y, int z, bool horizontal)
        {
            if (World.Size.InBounds(x, y, z))
                return Cell.IsWaterSource(World.Get(x, y, z));
            return horizontal && SeaLevel >= 0 && y >= 0 && y <= SeaLevel
                   && (x < 0 || z < 0 || x >= World.Size.X || z >= World.Size.Z) && y < World.Size.Y;
        }

        /// <summary>Kiểm các khối nước đang chảy kề ô vừa mất nước: khối không còn nguồn thì bật cờ rút (0041 QĐ 7).</summary>
        void ProcessSupport()
        {
            if (supportStarts.Count == 0)
                return;
            var starts = supportStarts.ToArray();
            supportStarts.Clear();
            supportStartSet.Clear();
            var visited = new HashSet<(int, int, int)>();
            foreach (var start in starts)
            {
                if (!World.Size.InBounds(start.Item1, start.Item2, start.Item3)) continue;
                if (!Cell.IsFlowing(World.Get(start.Item1, start.Item2, start.Item3))) continue;
                if (visited.Contains(start)) continue;

                var block = new List<(int x, int y, int z)>();
                var queue = new Queue<(int x, int y, int z)>();
                visited.Add(start);
                queue.Enqueue(start);
                bool supported = false;
                while (queue.Count > 0 && !supported)
                {
                    var (x, y, z) = queue.Dequeue();
                    block.Add((x, y, z));
                    if (block.Count > supportCheckMaxCells || IsSupportSource(x, y + 1, z, false))
                    {
                        supported = true;
                        break;
                    }
                    foreach (var (dx, dz) in Sides)
                        if (IsSupportSource(x + dx, y, z + dz, true))
                        {
                            supported = true;
                            break;
                        }
                    if (supported) break;
                    foreach (var (dx, dy, dz) in Faces)
                    {
                        var n = (x + dx, y + dy, z + dz);
                        if (!World.Size.InBounds(n.Item1, n.Item2, n.Item3) || visited.Contains(n)) continue;
                        if (!Cell.IsFlowing(World.Get(n.Item1, n.Item2, n.Item3))) continue;
                        visited.Add(n);
                        queue.Enqueue(n);
                    }
                }
                if (supported)
                    continue;
                foreach (var (x, y, z) in block)
                {
                    World.Set(x, y, z, Cell.SetDraining(World.Get(x, y, z), true));
                    AddFlowDependents(x, y, z);
                }
            }
        }

        static readonly (int dx, int dz)[] Sides = { (1, 0), (-1, 0), (0, 1), (0, -1) };

        bool IsWaterCell(int x, int y, int z) =>
            World.Size.InBounds(x, y, z) && World.GetMaterial(x, y, z) == BlockMaterial.Water;

        /// <summary>Ô nước là nguồn hoặc đang chảy ngang (cờ rơi tắt).</summary>
        static bool IsStandingWater(ushort c) =>
            Cell.GetMaterial(c) == BlockMaterial.Water && (Cell.IsWaterSource(c) || !Cell.IsFalling(c));

        /// <summary>Nước đứng, không đang rút, tại (x, y, z) — neo của ô nối (0042).</summary>
        bool IsBridgeAnchor(int x, int y, int z)
        {
            if (!IsWaterCell(x, y, z))
                return false;
            ushort c = World.Get(x, y, z);
            return IsStandingWater(c) && !Cell.IsDraining(c);
        }

        /// <summary>Ô nối (0042): ô đứng được, có neo ở hai phía đối diện (x − 1 và x + 1, hoặc z − 1 và z + 1).</summary>
        bool IsBridge(int x, int y, int z)
        {
            bool stand = IsSolidAt(World, x, y - 1, z) || (y - 1 >= 0 && IsStandingWater(World.Get(x, y - 1, z)));
            if (!stand)
                return false;
            return (IsBridgeAnchor(x - 1, y, z) && IsBridgeAnchor(x + 1, y, z))
                || (IsBridgeAnchor(x, y, z - 1) && IsBridgeAnchor(x, y, z + 1));
        }

        /// <summary>Mức nước ô nước (x, y, z) truyền sang ô ngang (0040 QĐ 3); 0 nếu không truyền.</summary>
        int Emit(int x, int y, int z, out bool fromDraining)
        {
            fromDraining = false;
            ushort c = World.Get(x, y, z);
            bool source = Cell.IsWaterSource(c);
            bool falling = Cell.IsFalling(c) && !source;
            // 0041 QĐ 4: nước rơi xuống nguồn thì nhập vào, không lan.
            if (falling && y - 1 >= 0 && Cell.IsWaterSource(World.Get(x, y - 1, z)))
                return 0;
            int level;
            if (source || falling)
                level = flowMaxDistance;
            else
            {
                fromDraining = Cell.IsDraining(c);
                int s = Cell.GetWaterLevel(c);
                level = IsNarrow(x, y, z) && !fromDraining ? s : s - 1;
            }
            if (level < 1)
                return 0;
            if (IsSolidAt(World, x, y - 1, z))
                return level;
            ushort b = World.Get(x, y - 1, z);
            if (Cell.GetMaterial(b) != BlockMaterial.Water)
                return 0;
            if (IsStandingWater(b))
                return level;
            // B đang rơi: chỉ đứng được khi ô này không rơi và B đã chạm đáy.
            if (falling)
                return 0;
            if (y - 2 < 0 || IsSolidAt(World, x, y - 2, z))
                return level;
            return IsStandingWater(World.Get(x, y - 2, z)) ? level : 0;
        }

        /// <summary>Một nhịp nước chảy (0040): tính trạng thái mong muốn của các ô trong hàng chờ rồi ghi một lượt. Trả số ô đổi.</summary>
        int StepFlow()
        {
            ProcessSupport();
            if (flowQueue.Count == 0)
                return 0;
            var snapshot = flowQueue.ToArray();
            flowQueue.Clear();
            flowQueueSet.Clear();

            var writes = new List<(int x, int y, int z, ushort cell)>();
            foreach (var (x, y, z) in snapshot)
            {
                ushort old = World.Get(x, y, z);
                if (IsSolidAt(World, x, y, z) || Cell.IsWaterSource(old))
                    continue;
                ushort target;
                if (SeaLevel >= 0 && y <= SeaLevel && TouchesWater(x, y, z))
                {
                    // 0040 QĐ 7a: biển ưu tiên vùng ≤ SeaLevel chạm nguồn.
                    if (Cell.GetMaterial(old) == BlockMaterial.Empty)
                    {
                        AddToFront(x, y, z);
                        continue;
                    }
                    target = Cell.SetModified(Cell.ClearFlow(Cell.SetWaterLevel(old, Cell.MaxWaterLevel)), true);
                }
                else if (IsWaterCell(x, y + 1, z))
                {
                    target = Cell.SetModified(Cell.MakeFlowing(old, flowMaxDistance, true), true);
                }
                else
                {
                    int m = 0;
                    bool allDraining = true;
                    foreach (var (dx, dz) in Sides)
                    {
                        if (!IsWaterCell(x + dx, y, z + dz)) continue;
                        int e = Emit(x + dx, y, z + dz, out bool fd);
                        if (e < 1) continue;
                        if (e > m) m = e;
                        if (!fd) allDraining = false;
                    }
                    if (m >= 1)
                        target = Cell.SetModified(Cell.MakeFlowing(old, m, false, allDraining), true);
                    else if (IsBridge(x, y, z))
                        target = Cell.SetModified(Cell.MakeFlowing(old, 1, false), true);
                    else if (Cell.GetMaterial(old) == BlockMaterial.Water)
                        target = Cell.ClearWater(old);
                    else
                        continue;
                }
                if (target != old)
                    writes.Add((x, y, z, target));
            }

            var changed = new List<(int x, int y, int z, BlockMaterial oldMat, ushort cell)>();
            var becameWater = new List<(int x, int y, int z)>();
            foreach (var (x, y, z, cell) in writes)
            {
                var oldMat = World.GetMaterial(x, y, z);
                World.Set(x, y, z, cell);
                changed.Add((x, y, z, oldMat, cell));
                if (oldMat != BlockMaterial.Water && Cell.GetMaterial(cell) == BlockMaterial.Water)
                    becameWater.Add((x, y, z));
            }
            foreach (var (x, y0, z) in becameWater)
            {
                for (int fy = y0 - 1; fy >= 0 && World.GetMaterial(x, fy, z) == BlockMaterial.Empty; fy--)
                {
                    ushort cell = Cell.SetModified(Cell.MakeFlowing(World.Get(x, fy, z), flowMaxDistance, true), true);
                    World.Set(x, fy, z, cell);
                    changed.Add((x, fy, z, BlockMaterial.Empty, cell));
                }
            }

            foreach (var (x, y, z, oldMat, cell) in changed)
            {
                var newMat = Cell.GetMaterial(cell);
                if (oldMat != newMat)
                {
                    CellChanged?.Invoke(x, y, z);
                    if (newMat == BlockMaterial.Water)
                        RevertFarmlandBelow(x, y, z);
                    else
                    {
                        if (oldMat == BlockMaterial.Water)
                            AddSupportNeighbors(x, y, z);
                        if (SeaLevel >= 0 && y <= SeaLevel && TouchesWater(x, y, z))
                            AddToFront(x, y, z);
                    }
                }
                else if (newMat == BlockMaterial.Water)
                    WaterSurfaceChanged?.Invoke(x, y, z);
                AddFlowDependents(x, y, z);
            }
            return changed.Count;
        }

        /// <summary>Chạy StepWater tới khi hết nước đang lan (test, tua nhanh). Trả về tổng số ô ngập.</summary>
        public int SettleWater(int maxSteps = 100000)
        {
            int total = 0;
            for (int i = 0; i < maxSteps && HasPendingWater; i++)
                total += StepWater();
            return total;
        }

        /// <summary>Thêm vào <paramref name="result"/> chỉ số mọi chunk có vùng đọc (chunk ± MeshMargin ô) chứa ô (x, y, z), kể cả chunk chéo.</summary>
        public void AffectedChunks(int x, int y, int z, ICollection<int> result) =>
            AffectedChunks(World.Size, MeshMargin, x, y, z, result);

        public static void AffectedChunks(WorldSize size, int margin, int x, int y, int z, ICollection<int> result)
        {
            const int C = WorldSize.ChunkSize;
            int x0 = Math.Max(0, FloorDiv(x - margin, C)), x1 = Math.Min(size.ChunksX - 1, FloorDiv(x + margin, C));
            int y0 = Math.Max(0, FloorDiv(y - margin, C)), y1 = Math.Min(size.ChunksY - 1, FloorDiv(y + margin, C));
            int z0 = Math.Max(0, FloorDiv(z - margin, C)), z1 = Math.Min(size.ChunksZ - 1, FloorDiv(z + margin, C));
            for (int cy = y0; cy <= y1; cy++)
            for (int cz = z0; cz <= z1; cz++)
            for (int cx = x0; cx <= x1; cx++)
            {
                int c = cx + size.ChunksX * (cz + size.ChunksZ * cy);
                if (!result.Contains(c))
                    result.Add(c);
            }
        }

        static int FloorDiv(int a, int b) => a >= 0 ? a / b : -((-a + b - 1) / b);
    }
}
