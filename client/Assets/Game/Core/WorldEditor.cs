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
    /// (design/04-tech/world-tech.md "Chỉnh sửa").
    /// Biển tràn + luật đặt khối theo design/decisions/0012: đặt vào ô rỗng hoặc ô
    /// nước kề mặt một khối đặc; đào ô ≤ mực nước biển kề nước (hoặc biển ngoài map) → nước lan qua các ô rỗng ≤ mực nước
    /// biển thông với nó (6 hướng) theo nhịp (design/decisions/0013): StepWater() một nhịp — rơi thẳng xuống ngay, lan ngang/dâng
    /// 1 ô mỗi nhịp. Nước biển (nguồn) không tự rút. Nước chảy (design/decisions/0043): cây cha–con — nguồn (ô nước có sẵn, ô biển
    /// loang) mức MaxLevel; ô chảy mức 0..MaxLevel, mỗi ô lan sang ô ngang mức − 1 (1 ô/nhịp), rơi xuống ô rỗng bên dưới mức MaxLevel;
    /// ô có ≥ WallBoostMinSolid ô ngang đặc được tăng mức lên MaxLevel rồi phát sóng mức cho ô chảy kề. Mất ô cha thì cả nhánh
    /// con bị xoá dây chuyền mỗi nhịp một lớp, rồi tái tạo từ nước kề còn nối với nguồn.
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

        int maxLevel = 5;

        /// <summary>Mức nước tối đa của nguồn / ô rơi (0043), hợp lệ 1–7.</summary>
        public int MaxLevel
        {
            get => maxLevel;
            set
            {
                if (value < 1 || value > Cell.MaxWaterLevel)
                    throw new ArgumentOutOfRangeException(nameof(value), value, "MaxLevel must be in 1..7");
                maxLevel = value;
            }
        }

        int wallBoostMinSolid = 2;

        /// <summary>Số ô ngang đặc tối thiểu để ô nước chảy được tăng mức lên MaxLevel (0043), hợp lệ 1–4.</summary>
        public int WallBoostMinSolid
        {
            get => wallBoostMinSolid;
            set
            {
                if (value < 1 || value > 4)
                    throw new ArgumentOutOfRangeException(nameof(value), value, "WallBoostMinSolid must be in 1..4");
                wallBoostMinSolid = value;
            }
        }

        struct FlowNode
        {
            public int X, Y, Z, Parent;
            public bool Source;
        }

        const int NoParent = -1;

        /// <summary>Cây cha–con của nước chảy (0043): vị trí → id, id → nút, id cha → id con (thứ tự thêm).</summary>
        readonly Dictionary<(int, int, int), int> nodeAt = new Dictionary<(int, int, int), int>();
        readonly Dictionary<int, FlowNode> nodes = new Dictionary<int, FlowNode>();
        readonly Dictionary<int, List<int>> children = new Dictionary<int, List<int>>();
        int nextId;

        /// <summary>Nút cần xét lan ở nhịp sau; xử lý theo id tăng dần → tất định.</summary>
        readonly List<int> active = new List<int>();
        readonly HashSet<int> activeSet = new HashSet<int>();

        /// <summary>Hàng xoá dây chuyền: nút sẽ bị xoá ở nhịp sau.</summary>
        readonly List<int> cascade = new List<int>();
        readonly HashSet<int> cascadeSet = new HashSet<int>();

        int stepChanged;

        /// <summary>Còn nước đang lan hoặc đang rút (gọi StepWater theo nhịp).</summary>
        public bool HasPendingWater => waterFront.Count > 0 || active.Count > 0 || cascade.Count > 0;

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
            ActivateAround(x, y, z);
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
            if (wasWater && nodeAt.TryGetValue((x, y, z), out int removed))
                RemoveNode(removed, true);
            foreach (var (dx, dz) in D4)
                if (TryFlowingNode(x + dx, y, z + dz, out int n))
                    WallBoost(n);
            Activate(x, y + 1, z);
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
            Activate(x, y + 1, z);
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

        /// <summary>Một nhịp nước: biển loang (0013) rồi nước chảy (0043). Trả tổng số ô đổi trong nhịp.</summary>
        public int StepWater() => StepSea() + StepFlow();

        static readonly (int dx, int dz)[] D4 = { (0, 1), (1, 0), (0, -1), (-1, 0) };

        /// <summary>Đưa ô nước (x, y, z) vào hàng chờ xét lan ở nhịp sau (thác demo có sẵn khi sinh world).</summary>
        public void WakeWater(int x, int y, int z) => Activate(x, y, z);

        /// <summary>Có nút nước ở ô (x, y, z) không; nguồn → parentId = −1 (test).</summary>
        public bool TryGetFlowNode(int x, int y, int z, out int id, out int parentId)
        {
            parentId = NoParent;
            if (!nodeAt.TryGetValue((x, y, z), out id))
                return false;
            parentId = nodes[id].Parent;
            return true;
        }

        int LevelOf(int id)
        {
            var n = nodes[id];
            return n.Source ? maxLevel : Cell.GetWaterLevel(World.Get(n.X, n.Y, n.Z));
        }

        /// <summary>Nút ở ô (x, y, z); ô nguồn chưa có nút thì tạo (lười). Ô không phải nước, hoặc ô chảy không có nút → −1.</summary>
        int GetOrCreateNode(int x, int y, int z)
        {
            if (!World.Size.InBounds(x, y, z))
                return -1;
            if (nodeAt.TryGetValue((x, y, z), out int id))
                return id;
            if (!Cell.IsWaterSource(World.Get(x, y, z)))
                return -1;
            id = nextId++;
            nodes[id] = new FlowNode { X = x, Y = y, Z = z, Parent = NoParent, Source = true };
            nodeAt[(x, y, z)] = id;
            return id;
        }

        /// <summary>Ô (x, y, z) là nước chảy có nút (không phải nguồn).</summary>
        bool TryFlowingNode(int x, int y, int z, out int id)
        {
            id = -1;
            if (!World.Size.InBounds(x, y, z))
                return false;
            ushort c = World.Get(x, y, z);
            return Cell.GetMaterial(c) == BlockMaterial.Water && Cell.IsFlowing(c) && nodeAt.TryGetValue((x, y, z), out id);
        }

        void ActivateId(int id)
        {
            if (activeSet.Add(id))
                active.Add(id);
        }

        void Activate(int x, int y, int z)
        {
            if (!World.Size.InBounds(x, y, z) || World.GetMaterial(x, y, z) != BlockMaterial.Water)
                return;
            int id = GetOrCreateNode(x, y, z);
            if (id >= 0)
                ActivateId(id);
        }

        /// <summary>Kích hoạt 4 ô ngang (D4) rồi ô phía trên.</summary>
        void ActivateAround(int x, int y, int z)
        {
            foreach (var (dx, dz) in D4)
                Activate(x + dx, y, z + dz);
            Activate(x, y + 1, z);
        }

        /// <summary>Gỡ nút khỏi bảng và khỏi danh sách con của cha; toCascade → con của nó (id tăng dần) vào hàng xoá.</summary>
        void RemoveNode(int id, bool toCascade)
        {
            var node = nodes[id];
            nodeAt.Remove((node.X, node.Y, node.Z));
            nodes.Remove(id);
            if (node.Parent != NoParent && children.TryGetValue(node.Parent, out var siblings))
                siblings.Remove(id);
            if (children.TryGetValue(id, out var list))
            {
                if (toCascade)
                {
                    var sorted = new List<int>(list);
                    sorted.Sort();
                    foreach (int c in sorted)
                        if (cascadeSet.Add(c))
                            cascade.Add(c);
                }
                children.Remove(id);
            }
        }

        void SetLevel(int id, int level)
        {
            var n = nodes[id];
            World.Set(n.X, n.Y, n.Z, Cell.SetWaterLevel(World.Get(n.X, n.Y, n.Z), level));
            stepChanged++;
            WaterSurfaceChanged?.Invoke(n.X, n.Y, n.Z);
        }

        void Reparent(int id, int parent)
        {
            var n = nodes[id];
            if (n.Parent != NoParent && children.TryGetValue(n.Parent, out var old))
                old.Remove(id);
            n.Parent = parent;
            nodes[id] = n;
            if (!children.TryGetValue(parent, out var list))
                children[parent] = list = new List<int>();
            list.Add(id);
        }

        /// <summary>ancestor là tổ tiên (kể cả cha) của id.</summary>
        bool IsAncestor(int ancestor, int id)
        {
            int cur = id;
            for (int step = 0; step <= nodes.Count; step++)
            {
                if (!nodes.TryGetValue(cur, out var node))
                    return false;
                int p = node.Parent;
                if (p == NoParent)
                    return false;
                if (p == ancestor)
                    return true;
                cur = p;
            }
            throw new InvalidOperationException("water parent cycle");
        }

        /// <summary>Nút thuộc nhánh đang xoá (0043 QĐ 10).</summary>
        bool IsDoomed(int id)
        {
            int cur = id;
            for (int step = 0; step <= nodes.Count + 1; step++)
            {
                if (cascadeSet.Contains(cur))
                    return true;
                if (!nodes.TryGetValue(cur, out var node))
                    return true;
                if (node.Source || node.Parent == NoParent)
                    return false;
                cur = node.Parent;
            }
            throw new InvalidOperationException("water parent cycle");
        }

        /// <summary>Ô chảy kề ≥ WallBoostMinSolid ô ngang đặc → mức MaxLevel rồi phát sóng mức (0043 QĐ 8).</summary>
        void WallBoost(int id)
        {
            var n = nodes[id];
            if (n.Source)
                return;
            int level = LevelOf(id);
            if (level <= 0 || level >= maxLevel)
                return;
            int solid = 0;
            foreach (var (dx, dz) in D4)
                if (IsSolidAt(World, n.X + dx, n.Y, n.Z + dz))
                    solid++;
            if (solid < wallBoostMinSolid)
                return;
            SetLevel(id, maxLevel);
            ActivateId(id);
            Propagate(id);
        }

        /// <summary>Phát sóng mức: ô chảy kề thấp hơn mức − 1 được nâng lên mức − 1, đổi cha nếu không phải cha/tổ tiên (0043 QĐ 9).</summary>
        void Propagate(int start)
        {
            var stack = new Stack<int>();
            stack.Push(start);
            while (stack.Count > 0)
            {
                int e = stack.Pop();
                if (!nodes.TryGetValue(e, out var en))
                    continue;
                int level = LevelOf(e);
                foreach (var (dx, dz) in D4)
                {
                    if (!TryFlowingNode(en.X + dx, en.Y, en.Z + dz, out int n))
                        continue;
                    if (nodes[n].Source || LevelOf(n) >= level - 1)
                        continue;
                    SetLevel(n, level - 1);
                    ActivateId(n);
                    if (nodes[e].Parent != n && !IsAncestor(n, e))
                        Reparent(n, e);
                    stack.Push(n);
                }
            }
        }

        /// <summary>Tạo nút nước chảy mới ở ô rỗng (x, y, z) dưới nút cha parent.</summary>
        int CreateChild(int parent, int x, int y, int z, int level, bool falling)
        {
            ushort old = World.Get(x, y, z);
            World.Set(x, y, z, Cell.SetModified(Cell.MakeFlowing(old, level, falling), true));
            int id = nextId++;
            nodes[id] = new FlowNode { X = x, Y = y, Z = z, Parent = parent, Source = false };
            nodeAt[(x, y, z)] = id;
            if (!children.TryGetValue(parent, out var list))
                children[parent] = list = new List<int>();
            list.Add(id);
            stepChanged++;
            CellChanged?.Invoke(x, y, z);
            RevertFarmlandBelow(x, y, z);
            ActivateId(id);
            Activate(x, y + 1, z);
            WallBoost(id);
            return id;
        }

        /// <summary>Ô nước đứng được: nguồn, hoặc ô chảy không rơi.</summary>
        bool IsStandable(int x, int y, int z)
        {
            if (!World.Size.InBounds(x, y, z))
                return false;
            ushort c = World.Get(x, y, z);
            return Cell.GetMaterial(c) == BlockMaterial.Water && (Cell.IsWaterSource(c) || !Cell.IsFalling(c));
        }

        /// <summary>Một nhịp nước chảy (0043): A xoá dây chuyền + tái tạo, B lan. Trả số ô đổi (vật liệu hoặc mức).</summary>
        int StepFlow()
        {
            stepChanged = 0;
            StepCascade();
            StepSpread();
            return stepChanged;
        }

        void StepCascade()
        {
            if (cascade.Count == 0)
                return;
            var batch = new List<int>(cascade);
            batch.Sort();
            cascade.Clear();
            cascadeSet.Clear();
            var next = new List<int>();
            var deleted = new List<(int x, int y, int z)>();
            foreach (int id in batch)
            {
                if (!nodes.TryGetValue(id, out var node))
                    continue;
                if (children.TryGetValue(id, out var kids))
                    next.AddRange(kids);
                RemoveNode(id, false);
                World.Set(node.X, node.Y, node.Z, Cell.ClearWater(World.Get(node.X, node.Y, node.Z)));
                stepChanged++;
                CellChanged?.Invoke(node.X, node.Y, node.Z);
                deleted.Add((node.X, node.Y, node.Z));
            }
            next.Sort();
            int prev = -1;
            foreach (int c in next)
            {
                if (c == prev || !nodes.ContainsKey(c))
                    continue;
                prev = c;
                if (cascadeSet.Add(c))
                    cascade.Add(c);
            }

            foreach (var (x, y, z) in deleted)
            {
                if (World.GetMaterial(x, y, z) != BlockMaterial.Empty)
                    continue;
                if (SeaLevel >= 0 && y <= SeaLevel && TouchesWater(x, y, z))
                {
                    AddToFront(x, y, z);
                    continue;
                }
                int bestLevel = -1, bestParent = NoParent;
                foreach (var (dx, dz) in D4)
                {
                    int nx = x + dx, nz = z + dz;
                    if (!World.Size.InBounds(nx, y, nz) || World.GetMaterial(nx, y, nz) != BlockMaterial.Water)
                        continue;
                    int level, parent;
                    if (Cell.IsWaterSource(World.Get(nx, y, nz)))
                    {
                        level = maxLevel;
                        parent = GetOrCreateNode(nx, y, nz);
                    }
                    else if (nodeAt.TryGetValue((nx, y, nz), out int nid) && !IsDoomed(nid))
                    {
                        level = Cell.GetWaterLevel(World.Get(nx, y, nz));
                        parent = nodes[nid].Parent;
                    }
                    else
                        continue;
                    if (level > bestLevel)
                    {
                        bestLevel = level;
                        bestParent = parent;
                    }
                }
                if (bestLevel >= 0 && bestParent != NoParent && nodes.ContainsKey(bestParent))
                    CreateChild(bestParent, x, y, z, bestLevel, false);
            }

            foreach (var (x, y, z) in deleted)
                ActivateAround(x, y, z);
        }

        void StepSpread()
        {
            if (active.Count == 0)
                return;
            var batch = new List<int>(active);
            batch.Sort();
            active.Clear();
            activeSet.Clear();
            foreach (int id in batch)
            {
                if (!nodes.TryGetValue(id, out var node))
                    continue;
                ushort cell = World.Get(node.X, node.Y, node.Z);
                if (Cell.GetMaterial(cell) != BlockMaterial.Water || IsDoomed(id))
                    continue;
                int level = LevelOf(id);
                bool falling = !node.Source && Cell.IsFalling(cell);
                int x = node.X, y = node.Y, z = node.Z;
                bool spread;
                if (y - 1 < 0 || IsSolidAt(World, x, y - 1, z))
                    spread = true;
                else if (World.GetMaterial(x, y - 1, z) == BlockMaterial.Empty)
                {
                    CreateChild(id, x, y - 1, z, maxLevel, true);
                    continue;
                }
                else if (falling)
                    continue;
                else
                {
                    ushort b = World.Get(x, y - 1, z);
                    if (Cell.IsWaterSource(b) || !Cell.IsFalling(b))
                        spread = true;
                    else
                        spread = y - 2 < 0 || IsSolidAt(World, x, y - 2, z) || IsStandable(x, y - 2, z);
                }
                if (!spread || level <= 0)
                    continue;
                foreach (var (dx, dz) in D4)
                {
                    int nx = x + dx, nz = z + dz;
                    if (!World.Size.InBounds(nx, y, nz) || World.GetMaterial(nx, y, nz) != BlockMaterial.Empty)
                        continue;
                    if (SeaLevel >= 0 && y <= SeaLevel && TouchesWater(nx, y, nz))
                        AddToFront(nx, y, nz);
                    else
                        CreateChild(id, nx, y, nz, level - 1, false);
                }
            }
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
