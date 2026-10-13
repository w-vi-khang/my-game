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
        /// <summary>Đặt: ô không rỗng (kể cả nước).</summary>
        NotEmpty,
        /// <summary>Đặt: loại vật liệu không đặt được (rỗng, nước, đá đáy).</summary>
        NotPlaceable,
        /// <summary>Đặt: kho hết vật liệu loại này.</summary>
        NoMaterial,
        /// <summary>Đặt: ô đang có người chơi, sinh vật, cây hoặc kho.</summary>
        Occupied,
    }

    /// <summary>
    /// Đào/đặt nguyên ô theo luật M1 (design/02-gameplay/mechanics.md "Voxel world — luật M1") và tính chunk cần dựng lại
    /// (design/04-tech/world-tech.md "Chỉnh sửa").
    /// </summary>
    public sealed class WorldEditor
    {
        public readonly WorldData World;
        public readonly MaterialInventory Inventory;

        /// <summary>Số ô quanh chunk mà bộ dựng mesh đọc (đệm). Sửa ô trong vùng này của chunk bên cạnh → chunk đó cũng bẩn.</summary>
        public readonly int MeshMargin;

        /// <summary>Móc kiểm tra ô bị chiếm (người chơi, sinh vật, cây, kho). M1 bước 4 chưa có vật thể → mặc định null.</summary>
        public Func<int, int, int, bool> IsOccupied;

        /// <summary>Ô (x, y, z) vừa đổi — cho tìm đường, môi trường sống, hiển thị.</summary>
        public event Action<int, int, int> CellChanged;

        public WorldEditor(WorldData world, MaterialInventory inventory, int meshMargin)
        {
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
            return EditResult.Ok;
        }

        /// <summary>Đặt: chỉ vào ô rỗng, tiêu 1 vật liệu từ kho.</summary>
        public EditResult TryPlace(int x, int y, int z, BlockMaterial material)
        {
            if (!World.Size.InBounds(x, y, z))
                return EditResult.OutOfBounds;
            if (!IsPlaceable(material))
                return EditResult.NotPlaceable;
            ushort cell = World.Get(x, y, z);
            if (Cell.GetMaterial(cell) != BlockMaterial.Empty)
                return EditResult.NotEmpty;
            if (IsOccupied != null && IsOccupied(x, y, z))
                return EditResult.Occupied;
            if (!Inventory.TryTake(material))
                return EditResult.NoMaterial;

            World.Set(x, y, z, Cell.SetModified(Cell.SetMaterial(cell, material), true));
            CellChanged?.Invoke(x, y, z);
            return EditResult.Ok;
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
