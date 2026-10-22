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
        /// <summary>Đặt: kho hết vật liệu loại này.</summary>
        NoMaterial,
        /// <summary>Đặt: ô đang có người chơi, sinh vật, cây hoặc kho.</summary>
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
    /// 1 ô mỗi nhịp. Nước không tự rút.
    /// </summary>
    public sealed class WorldEditor
    {
        public readonly WorldData World;
        public readonly MaterialInventory Inventory;

        /// <summary>Số ô quanh chunk mà bộ dựng mesh đọc (đệm). Sửa ô trong vùng này của chunk bên cạnh → chunk đó cũng bẩn.</summary>
        public readonly int MeshMargin;

        /// <summary>Mực nước biển S (ô y ≤ S có thể ngập); &lt; 0 → không có biển loang.</summary>
        public readonly int SeaLevel;

        /// <summary>Mặt trước dòng nước: ô sẽ ngập ở nhịp sau (nếu khi đó còn rỗng). Thứ tự chèn cố định → tất định.</summary>
        readonly List<(int x, int y, int z)> waterFront = new List<(int, int, int)>();
        readonly HashSet<(int, int, int)> waterFrontSet = new HashSet<(int, int, int)>();

        /// <summary>Còn nước đang lan (gọi StepWater theo nhịp).</summary>
        public bool HasPendingWater => waterFront.Count > 0;

        /// <summary>Móc kiểm tra ô bị chiếm (người chơi, sinh vật, cây, kho). M1 bước 4 chưa có vật thể → mặc định null.</summary>
        public Func<int, int, int, bool> IsOccupied;

        /// <summary>Móc ô bị nhân vật người chơi chiếm (world 7h) — tách khỏi IsOccupied vì Simulation.AttachEditor ghi đè móc đó.</summary>
        public Func<int, int, int, bool> IsPlayerAt;

        /// <summary>Ô (x, y, z) vừa đổi — cho tìm đường, môi trường sống, hiển thị.</summary>
        public event Action<int, int, int> CellChanged;

        public WorldEditor(WorldData world, MaterialInventory inventory, int meshMargin, int seaLevel = -1)
        {
            SeaLevel = seaLevel;
            if (meshMargin < 0 || meshMargin >= WorldSize.ChunkSize)
                throw new ArgumentOutOfRangeException(nameof(meshMargin), meshMargin, "Đệm phải trong 0..ChunkSize-1");
            World = world ?? throw new ArgumentNullException(nameof(world));
            Inventory = inventory ?? throw new ArgumentNullException(nameof(inventory));
            MeshMargin = meshMargin;
        }

        public static bool IsPlaceable(BlockMaterial m) =>
            m == BlockMaterial.Grass || m == BlockMaterial.Dirt || m == BlockMaterial.Stone || m == BlockMaterial.Sand;

        /// <summary>Đào: ô thành rỗng, vật liệu +1 vào kho (ô ruộng → đất, 0020).</summary>
        public EditResult TryDig(int x, int y, int z)
        {
            if (!World.Size.InBounds(x, y, z))
                return EditResult.OutOfBounds;
            ushort cell = World.Get(x, y, z);
            if (!Cell.IsDiggable(cell))
                return EditResult.NotDiggable;

            var material = Cell.GetMaterial(cell);
            World.Set(x, y, z, Cell.SetModified(Cell.SetMaterial(cell, BlockMaterial.Empty), true));
            Inventory.Add(material == BlockMaterial.Farmland ? BlockMaterial.Dirt : material);
            CellChanged?.Invoke(x, y, z);
            if (SeaLevel >= 0 && y <= SeaLevel && TouchesWater(x, y, z))
                AddToFront(x, y, z);
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
            if (Inventory.Get(material) <= 0)
                return EditResult.NoMaterial;
            return EditResult.Ok;
        }

        /// <summary>Đặt: vào ô rỗng hoặc ô nước (nước ô đó mất) kề mặt một khối đặc, tiêu 1 vật liệu từ kho.</summary>
        public EditResult TryPlace(int x, int y, int z, BlockMaterial material)
        {
            var check = CheckPlace(x, y, z, material);
            if (check != EditResult.Ok)
                return check;
            if (!Inventory.TryTake(material))
                return EditResult.NoMaterial;
            ushort cell = World.Get(x, y, z);

            World.Set(x, y, z, Cell.SetModified(Cell.SetWaterLevel(Cell.SetMaterial(cell, material), 0), true));
            CellChanged?.Invoke(x, y, z);
            RevertFarmlandBelow(x, y, z);
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
        bool IsSolid(int x, int y, int z)
        {
            if (y < 0)
                return true;
            if (!World.Size.InBounds(x, y, z))
                return false;
            var m = World.GetMaterial(x, y, z);
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
            return World.GetMaterial(x, y, z) == BlockMaterial.Water;
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
        }

        /// <summary>
        /// Một nhịp nước lan (0013): mọi ô ở mặt trước còn rỗng thành nước; từ mỗi ô vừa ngập, nước rơi thẳng xuống qua các ô rỗng
        /// ≤ SeaLevel ngay trong nhịp; ô rỗng ≤ SeaLevel kề ngang hoặc phía trên các ô vừa ngập vào mặt trước nhịp sau.
        /// Ô ở mặt trước đã bị đặt khối thì bỏ qua (khối chặn dòng nước). Trả về số ô ngập trong nhịp.
        /// </summary>
        public int StepWater()
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
