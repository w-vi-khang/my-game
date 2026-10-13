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
    }

    /// <summary>
    /// Đào/đặt nguyên ô theo luật M1 (design/02-gameplay/mechanics.md "Voxel world — luật M1") và tính chunk cần dựng lại
    /// (design/04-tech/world-tech.md "Chỉnh sửa"). Biển tràn + luật đặt khối theo design/decisions/0012: đặt vào ô rỗng hoặc ô
    /// nước kề mặt một khối đặc; đào ô ≤ mực nước biển kề nước (hoặc biển ngoài map) → nước loang ngay qua các ô rỗng ≤ mực
    /// nước biển thông với nó (6 hướng). Nước không tự rút.
    /// </summary>
    public sealed class WorldEditor
    {
        public readonly WorldData World;
        public readonly MaterialInventory Inventory;

        /// <summary>Số ô quanh chunk mà bộ dựng mesh đọc (đệm). Sửa ô trong vùng này của chunk bên cạnh → chunk đó cũng bẩn.</summary>
        public readonly int MeshMargin;

        /// <summary>Mực nước biển S (ô y ≤ S có thể ngập); &lt; 0 → không có biển loang.</summary>
        public readonly int SeaLevel;

        /// <summary>Số ô vừa ngập ở lần đào gần nhất (0 nếu không loang).</summary>
        public int LastFloodCount { get; private set; }

        /// <summary>Móc kiểm tra ô bị chiếm (người chơi, sinh vật, cây, kho). M1 bước 4 chưa có vật thể → mặc định null.</summary>
        public Func<int, int, int, bool> IsOccupied;

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

        /// <summary>Đào: ô thành rỗng, vật liệu +1 vào kho.</summary>
        public EditResult TryDig(int x, int y, int z)
        {
            if (!World.Size.InBounds(x, y, z))
                return EditResult.OutOfBounds;
            ushort cell = World.Get(x, y, z);
            if (!Cell.IsDiggable(cell))
                return EditResult.NotDiggable;

            var material = Cell.GetMaterial(cell);
            World.Set(x, y, z, Cell.SetModified(Cell.SetMaterial(cell, BlockMaterial.Empty), true));
            Inventory.Add(material);
            CellChanged?.Invoke(x, y, z);
            LastFloodCount = Flood(x, y, z);
            return EditResult.Ok;
        }

        /// <summary>Đặt: vào ô rỗng hoặc ô nước (nước ô đó mất) kề mặt một khối đặc, tiêu 1 vật liệu từ kho.</summary>
        public EditResult TryPlace(int x, int y, int z, BlockMaterial material)
        {
            if (!World.Size.InBounds(x, y, z))
                return EditResult.OutOfBounds;
            if (!IsPlaceable(material))
                return EditResult.NotPlaceable;
            ushort cell = World.Get(x, y, z);
            var current = Cell.GetMaterial(cell);
            if (current != BlockMaterial.Empty && current != BlockMaterial.Water)
                return EditResult.NotEmpty;
            if (!HasSolidNeighbor(x, y, z))
                return EditResult.NotAttached;
            if (IsOccupied != null && IsOccupied(x, y, z))
                return EditResult.Occupied;
            if (!Inventory.TryTake(material))
                return EditResult.NoMaterial;

            World.Set(x, y, z, Cell.SetModified(Cell.SetWaterLevel(Cell.SetMaterial(cell, material), 0), true));
            CellChanged?.Invoke(x, y, z);
            return EditResult.Ok;
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

        /// <summary>Biển loang từ ô vừa đào: nếu ô ≤ SeaLevel kề nguồn nước → mọi ô rỗng ≤ SeaLevel thông với nó (6 hướng) thành nước.</summary>
        int Flood(int x, int y, int z)
        {
            if (SeaLevel < 0 || y > SeaLevel)
                return 0;
            bool touches = false;
            foreach (var (dx, dy, dz) in Faces)
                touches |= IsWaterSource(x + dx, y + dy, z + dz);
            if (!touches)
                return 0;

            var queue = new Queue<(int, int, int)>();
            int count = 0;
            Fill(x, y, z);
            while (queue.Count > 0)
            {
                var (cx, cy, cz) = queue.Dequeue();
                foreach (var (dx, dy, dz) in Faces)
                {
                    int nx = cx + dx, ny = cy + dy, nz = cz + dz;
                    if (ny <= SeaLevel && World.Size.InBounds(nx, ny, nz) && World.GetMaterial(nx, ny, nz) == BlockMaterial.Empty)
                        Fill(nx, ny, nz);
                }
            }
            return count;

            void Fill(int fx, int fy, int fz)
            {
                ushort old = World.Get(fx, fy, fz);
                World.Set(fx, fy, fz, Cell.SetModified(Cell.SetWaterLevel(Cell.SetMaterial(old, BlockMaterial.Water), Cell.MaxWaterLevel), true));
                queue.Enqueue((fx, fy, fz));
                count++;
                CellChanged?.Invoke(fx, fy, fz);
            }
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
