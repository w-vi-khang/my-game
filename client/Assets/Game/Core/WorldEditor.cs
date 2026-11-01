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
    /// ô có ≥ WallBoostMinSolid ô ngang đặc được tăng mức lên MaxLevel rồi phát sóng mức cho ô chảy kề. Ô chảy thấp hơn nguồn gốc của cây, trong BasinFillRadius ô ngang, cũng lên MaxLevel (0044). Mất ô cha thì cả nhánh
    /// con bị xoá dây chuyền mỗi nhịp một lớp, rồi tái tạo từ nước kề còn nối với nguồn.
    /// Mức tính lại theo hình dạng mỗi nhịp (0045): điểm bất động trên thành phần ngang; lớp ngang mặt nguồn đầy khi vùng kín trong BasinFillRadius.
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

        int basinFillRadius = 16;

        /// <summary>Bán kính ngang (Chebyshev) quanh nguồn gốc mà ô chảy thấp hơn nguồn lên mức MaxLevel (0044), hợp lệ 0–64.</summary>
        public int BasinFillRadius
        {
            get => basinFillRadius;
            set
            {
                if (value < 0 || value > 64)
                    throw new ArgumentOutOfRangeException(nameof(value), value, "BasinFillRadius must be in 0..64");
                basinFillRadius = value;
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

        /// <summary>Vị trí cần tính lại mức (0045) ở cuối nhịp; List giữ thứ tự chèn → tất định.</summary>
        readonly List<(int x, int y, int z)> relevelDirty = new List<(int, int, int)>();
        readonly HashSet<(int, int, int)> relevelDirtySet = new HashSet<(int, int, int)>();

        int stepChanged;

        /// <summary>Còn nước đang lan, đang rút hoặc chờ tính lại mức (0045) — gọi StepWater theo nhịp.</summary>
        public bool HasPendingWater => waterFront.Count > 0 || active.Count > 0 || cascade.Count > 0 || relevelDirty.Count > 0;

        /// <summary>Móc kiểm tra ô bị chiếm (người chơi, sinh vật, cây, kho). M1 bước 4 chưa có vật thể → mặc định null.</summary>
        public Func<int, int, int, bool> IsOccupied;

        /// <summary>Móc ô bị nhân vật người chơi chiếm (world 7h) — tách khỏi IsOccupied vì Simulation.AttachEditor ghi đè móc đó.</summary>
        public Func<int, int, int, bool> IsPlayerAt;

        /// <summary>Ô (x, y, z) vừa đổi — cho tìm đường, môi trường sống, hiển thị.</summary>
        public event Action<int, int, int> CellChanged;

        /// <summary>Khối đặc đổi do đào/đặt (brief 0095) — client dựng lại chunk trong lề WaterShoreMargin (khoảng cách tới bờ của mặt nước). Nước lan/rút không phát.</summary>
        public event Action<int, int, int> SolidChanged;

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
            SolidChanged?.Invoke(x, y, z);
            if (SeaLevel >= 0 && y <= SeaLevel && TouchesWater(x, y, z))
                AddToFront(x, y, z);
            ActivateAround(x, y, z);
            MarkDirtyAround(x, y, z);
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
            SolidChanged?.Invoke(x, y, z);
            RevertFarmlandBelow(x, y, z);
            if (wasWater && nodeAt.TryGetValue((x, y, z), out int removed))
                RemoveNode(removed, true);
            foreach (var (dx, dz) in D4)
                if (TryFlowingNode(x + dx, y, z + dz, out int n))
                    WallBoost(n);
            Activate(x, y + 1, z);
            MarkDirtyAround(x, y, z);
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
            MarkDirtyAround(x, y, z);
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

        /// <summary>Hướng chảy chính (main_flow, user 2026-11-02): ô con C của ô nước chảy B nằm tiếp theo hướng cha → B (C − B = B − cha; cha kề B theo 1 trong 6 hướng).
        /// Không có (không phải nút, nguồn, cha không kề, không có con đúng hướng) → false. Vật nổi (M2) ưu tiên trôi theo hướng này.</summary>
        public bool TryGetMainFlow(int x, int y, int z, out int cx, out int cy, out int cz)
        {
            cx = cy = cz = 0;
            if (!nodeAt.TryGetValue((x, y, z), out int id))
                return false;
            var n = nodes[id];
            if (n.Parent == NoParent || !nodes.TryGetValue(n.Parent, out var p))
                return false;
            int dx = n.X - p.X, dy = n.Y - p.Y, dz = n.Z - p.Z;
            if (Math.Abs(dx) + Math.Abs(dy) + Math.Abs(dz) != 1)
                return false;
            if (!nodeAt.TryGetValue((x + dx, y + dy, z + dz), out int c) || nodes[c].Parent != id)
                return false;
            cx = x + dx; cy = y + dy; cz = z + dz;
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

        void MarkDirty(int x, int y, int z)
        {
            if (relevelDirtySet.Add((x, y, z)))
                relevelDirty.Add((x, y, z));
        }

        /// <summary>Đánh dấu 4 ô ngang (D4) cần tính lại mức.</summary>
        void MarkDirtyAround(int x, int y, int z)
        {
            foreach (var (dx, dz) in D4)
                MarkDirty(x + dx, y, z + dz);
        }

        /// <summary>Gỡ nút khỏi bảng và khỏi danh sách con của cha; toCascade → con của nó (id tăng dần) vào hàng xoá.</summary>
        void RemoveNode(int id, bool toCascade)
        {
            var node = nodes[id];
            nodeAt.Remove((node.X, node.Y, node.Z));
            nodes.Remove(id);
            SetFlowDir(node.X, node.Y, node.Z, 0);
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
            byte code = FlowDirCode(n.X, n.Y, n.Z, parent);
            if (World.FlowDirAt(n.X, n.Y, n.Z) != code)
            {
                SetFlowDir(n.X, n.Y, n.Z, code);
                WaterSurfaceChanged?.Invoke(n.X, n.Y, n.Z);
            }
            if (!children.TryGetValue(parent, out var list))
                children[parent] = list = new List<int>();
            list.Add(id);
        }

        /// <summary>Mã hướng cha → nút (FlowDirs, brief 0098): cha kề ngang cùng lớp → chỉ số D4 + 1; còn lại 0.</summary>
        byte FlowDirCode(int x, int y, int z, int parent)
        {
            if (parent == NoParent || !nodes.TryGetValue(parent, out var p) || p.Y != y)
                return 0;
            for (int i = 0; i < D4.Length; i++)
                if (x - p.X == D4[i].dx && z - p.Z == D4[i].dz)
                    return (byte)(i + 1);
            return 0;
        }

        void SetFlowDir(int x, int y, int z, byte code)
        {
            if (code == 0 && World.FlowDirs == null)
                return;
            World.EnsureFlowDirs()[World.Size.Index(x, y, z)] = code;
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

        /// <summary>Nguồn gốc của cây chứa id (đi theo Parent tới nút Source); không có thì −1.</summary>
        int RootSourceOf(int id)
        {
            int cur = id;
            for (int step = 0; step <= nodes.Count; step++)
            {
                if (!nodes.TryGetValue(cur, out var node))
                    return -1;
                if (node.Source)
                    return cur;
                if (node.Parent == NoParent)
                    return -1;
                cur = node.Parent;
            }
            throw new InvalidOperationException("water parent cycle");
        }

        /// <summary>Ô chảy thấp hơn nguồn gốc và trong BasinFillRadius ô ngang (Chebyshev) từ nguồn (0044); enclosed = thành phần ngang của ô là vùng kín ngang mặt nguồn (0045, Relevel tính).</summary>
        bool InBasin(int id, bool enclosed = false)
        {
            var n = nodes[id];
            if (n.Source)
                return false;
            if (enclosed)
                return true;
            int s = RootSourceOf(id);
            if (s < 0)
                return false;
            var src = nodes[s];
            return n.Y < src.Y && Math.Max(Math.Abs(n.X - src.X), Math.Abs(n.Z - src.Z)) <= basinFillRadius;
        }

        /// <summary>Ô chảy trong vùng lấp (0044) hoặc kề ≥ WallBoostMinSolid ô ngang đặc (0043 QĐ 8) → mức MaxLevel rồi phát sóng mức.</summary>
        void WallBoost(int id)
        {
            var n = nodes[id];
            if (n.Source)
                return;
            int level = LevelOf(id);
            if (level >= maxLevel)
                return;
            if (!InBasin(id))
            {
                if (level <= 0)
                    return;
                int solid = 0;
                foreach (var (dx, dz) in D4)
                    if (IsSolidAt(World, n.X + dx, n.Y, n.Z + dz))
                        solid++;
                if (solid < wallBoostMinSolid)
                    return;
            }
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
                    if (InBasin(n))
                        SetLevel(n, maxLevel);
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
            SetFlowDir(x, y, z, FlowDirCode(x, y, z, parent));
            if (!children.TryGetValue(parent, out var list))
                children[parent] = list = new List<int>();
            list.Add(id);
            stepChanged++;
            CellChanged?.Invoke(x, y, z);
            RevertFarmlandBelow(x, y, z);
            ActivateId(id);
            Activate(x, y + 1, z);
            MarkDirty(x, y, z);
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
            Relevel();
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
                MarkDirty(node.X, node.Y, node.Z);
                MarkDirtyAround(node.X, node.Y, node.Z);
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
                // 0045 mục 2: ô tái tạo nhận mức max(ô kề) − 1 (như lan) — ô kề mức 0 không tái tạo, khớp Relevel.
                if (bestLevel >= 1 && bestParent != NoParent && nodes.ContainsKey(bestParent))
                    CreateChild(bestParent, x, y, z, bestLevel - 1, false);
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

        /// <summary>
        /// Tính lại mức theo hình dạng (0045) cho mọi thành phần ngang (nút chảy cùng tầng, 4 hướng, không thuộc nhánh đang xoá)
        /// chứa một vị trí bẩn; xử lý theo (y, z, x) tăng → tất định.
        /// </summary>
        void Relevel()
        {
            if (relevelDirty.Count == 0)
                return;
            var positions = new List<(int x, int y, int z)>(relevelDirty);
            relevelDirty.Clear();
            relevelDirtySet.Clear();
            positions.Sort((a, b) => a.y != b.y ? a.y.CompareTo(b.y) : a.z != b.z ? a.z.CompareTo(b.z) : a.x.CompareTo(b.x));
            var done = new HashSet<int>();
            foreach (var (x, y, z) in positions)
            {
                if (!TryFlowingNode(x, y, z, out int id) || done.Contains(id) || IsDoomed(id))
                    continue;
                RelevelComponent(HorizontalComponent(id, done));
            }
        }

        /// <summary>BFS 4 hướng cùng tầng qua nút chảy không thuộc nhánh đang xoá; thêm vào done.</summary>
        List<int> HorizontalComponent(int start, HashSet<int> done)
        {
            var comp = new List<int> { start };
            done.Add(start);
            for (int i = 0; i < comp.Count; i++)
            {
                var n = nodes[comp[i]];
                foreach (var (dx, dz) in D4)
                    if (TryFlowingNode(n.X + dx, n.Y, n.Z + dz, out int nb) && !done.Contains(nb) && !IsDoomed(nb))
                    {
                        done.Add(nb);
                        comp.Add(nb);
                    }
            }
            return comp;
        }

        /// <summary>
        /// Vùng kín (0045 mục 1): BFS 4 hướng ở tầng của thành phần qua ô rỗng / nước chảy (không qua nguồn, ô đặc; ngoài world = vách)
        /// không gặp ô nào cách nguồn src quá BasinFillRadius (Chebyshev).
        /// </summary>
        bool IsEnclosed(List<int> comp, FlowNode src)
        {
            int y = src.Y;
            var seen = new HashSet<(int, int)>();
            var queue = new Queue<(int x, int z)>();
            foreach (int id in comp)
            {
                var n = nodes[id];
                if (seen.Add((n.X, n.Z)))
                    queue.Enqueue((n.X, n.Z));
            }
            while (queue.Count > 0)
            {
                var (x, z) = queue.Dequeue();
                if (Math.Max(Math.Abs(x - src.X), Math.Abs(z - src.Z)) > basinFillRadius)
                    return false;
                foreach (var (dx, dz) in D4)
                {
                    int nx = x + dx, nz = z + dz;
                    if (!World.Size.InBounds(nx, y, nz) || seen.Contains((nx, nz)))
                        continue;
                    ushort c = World.Get(nx, y, nz);
                    var m = Cell.GetMaterial(c);
                    if (m != BlockMaterial.Empty && !(m == BlockMaterial.Water && !Cell.IsWaterSource(c)))
                        continue;
                    seen.Add((nx, nz));
                    queue.Enqueue((nx, nz));
                }
            }
            return true;
        }

        /// <summary>
        /// Mức = điểm bất động nhỏ nhất trên thành phần C (0045): L(c) = max(seed, L(kề) − 1, MaxLevel − 1 nếu kề nguồn), rồi tăng
        /// lên MaxLevel khi lấp hố (0044) / vùng kín, hoặc L &gt; 0 và ≥ WallBoostMinSolid ô ngang đặc (như WallBoost). Cha dựng kiểu Prim
        /// từ nguồn + ô rơi; ô không đạt được rút theo dây chuyền 0043.
        /// </summary>
        void RelevelComponent(List<int> comp)
        {
            int count = comp.Count;
            var index = new Dictionary<(int, int), int>(count);
            int y = nodes[comp[0]].Y, minId = comp[0];
            for (int i = 0; i < count; i++)
            {
                var n = nodes[comp[i]];
                index[(n.X, n.Z)] = i;
                minId = Math.Min(minId, comp[i]);
            }
            int srcId = RootSourceOf(minId);
            bool enclosed = srcId >= 0 && nodes[srcId].Y == y && IsEnclosed(comp, nodes[srcId]);

            var basin = new bool[count];
            var walled = new bool[count];
            var falling = new bool[count];
            var level = new int[count];
            var queue = new Queue<int>();
            for (int i = 0; i < count; i++)
            {
                var n = nodes[comp[i]];
                basin[i] = InBasin(comp[i], enclosed);
                int solid = 0;
                foreach (var (dx, dz) in D4)
                    if (IsSolidAt(World, n.X + dx, y, n.Z + dz))
                        solid++;
                walled[i] = solid >= wallBoostMinSolid;
                falling[i] = Cell.IsFalling(World.Get(n.X, y, n.Z));
                level[i] = -1;
                int seed = falling[i] ? maxLevel : (TouchesSource(n.X, y, n.Z) ? maxLevel - 1 : -1);
                if (seed >= 0)
                {
                    level[i] = Boosted(i, seed);
                    queue.Enqueue(i);
                }
            }

            int Boosted(int i, int l)
            {
                if (l >= maxLevel)
                    return l;
                if (basin[i] || (l > 0 && walled[i]))
                    return maxLevel;
                return l;
            }

            while (queue.Count > 0)
            {
                int i = queue.Dequeue();
                int cand = level[i] - 1;
                if (cand < 0)
                    continue;
                var n = nodes[comp[i]];
                foreach (var (dx, dz) in D4)
                {
                    if (!index.TryGetValue((n.X + dx, n.Z + dz), out int j))
                        continue;
                    int l = Boosted(j, cand);
                    if (l > level[j])
                    {
                        level[j] = l;
                        queue.Enqueue(j);
                    }
                }
            }

            // Cha kiểu Prim: ô rơi giữ cha tầng trên; ô khác gắn (mức cao trước, bằng nhau → id nhỏ trước) vào ô kề đã gắn mức cao nhất.
            var attached = new bool[count];
            var frontier = new SortedSet<(int negLevel, int id)>();
            void AddNeighbours(int i)
            {
                var n = nodes[comp[i]];
                foreach (var (dx, dz) in D4)
                    if (index.TryGetValue((n.X + dx, n.Z + dz), out int j) && !attached[j] && level[j] >= 0)
                        frontier.Add((-level[j], comp[j]));
            }
            for (int i = 0; i < count; i++)
            {
                if (level[i] < 0)
                    continue;
                if (falling[i])
                    attached[i] = true;
                else if (TouchesSource(nodes[comp[i]].X, y, nodes[comp[i]].Z))
                    frontier.Add((-level[i], comp[i]));
            }
            for (int i = 0; i < count; i++)
                if (attached[i])
                    AddNeighbours(i);
            while (frontier.Count > 0)
            {
                var top = frontier.Min;
                frontier.Remove(top);
                int id = top.id;
                var n = nodes[id];
                int i = index[(n.X, n.Z)];
                if (attached[i])
                    continue;
                int bestLevel = -1, bestParent = NoParent;
                bool keep = false;
                foreach (var (dx, dz) in D4)
                {
                    int nx = n.X + dx, nz = n.Z + dz, l, p;
                    if (index.TryGetValue((nx, nz), out int j))
                    {
                        if (!attached[j])
                            continue;
                        l = level[j];
                        p = comp[j];
                    }
                    else if (World.Size.InBounds(nx, y, nz) && World.GetMaterial(nx, y, nz) == BlockMaterial.Water &&
                             Cell.IsWaterSource(World.Get(nx, y, nz)))
                    {
                        l = maxLevel;
                        p = GetOrCreateNode(nx, y, nz);
                    }
                    else
                        continue;
                    if (l > bestLevel)
                    {
                        bestLevel = l;
                        bestParent = p;
                        keep = p == n.Parent;
                    }
                    else if (l == bestLevel && p == n.Parent)
                        keep = true;
                }
                if (bestParent == NoParent)
                    continue;
                attached[i] = true;
                if (!keep && bestParent != n.Parent)
                    Reparent(id, bestParent);
                AddNeighbours(i);
            }

            for (int i = 0; i < count; i++)
            {
                int id = comp[i];
                if (level[i] >= 0)
                {
                    if (level[i] != LevelOf(id))
                    {
                        SetLevel(id, level[i]);
                        ActivateId(id);
                    }
                    continue;
                }
                // Không đạt được: vào hàng xoá khi cha không phải ô không-đạt-được khác trong C; còn lại rút theo dây chuyền.
                var pn = nodes[id];
                bool parentUnreached = pn.Parent != NoParent && nodes.TryGetValue(pn.Parent, out var par) && par.Y == y &&
                                       index.TryGetValue((par.X, par.Z), out int pi) && comp[pi] == pn.Parent && level[pi] < 0;
                if (!parentUnreached && cascadeSet.Add(id))
                    cascade.Add(id);
            }
        }

        /// <summary>Có ô nguồn trong map kề ngang (x, y, z).</summary>
        bool TouchesSource(int x, int y, int z)
        {
            foreach (var (dx, dz) in D4)
            {
                int nx = x + dx, nz = z + dz;
                if (World.Size.InBounds(nx, y, nz) && World.GetMaterial(nx, y, nz) == BlockMaterial.Water &&
                    Cell.IsWaterSource(World.Get(nx, y, nz)))
                    return true;
            }
            return false;
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
