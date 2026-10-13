using System;

namespace Game.Core
{
    /// <summary>Kích thước world theo ô gameplay; chunk 16³ (design/04-tech/world-tech.md).</summary>
    public readonly struct WorldSize : IEquatable<WorldSize>
    {
        public const int ChunkSize = 16;

        public readonly int X, Y, Z;

        public WorldSize(int x, int y, int z)
        {
            if (x <= 0 || y <= 0 || z <= 0 || x % ChunkSize != 0 || y % ChunkSize != 0 || z % ChunkSize != 0)
                throw new ArgumentException($"Kích thước phải dương và chia hết cho {ChunkSize}: {x}×{y}×{z}");
            X = x;
            Y = y;
            Z = z;
        }

        public int CellCount => X * Y * Z;
        public int ChunksX => X / ChunkSize;
        public int ChunksY => Y / ChunkSize;
        public int ChunksZ => Z / ChunkSize;
        public int ChunkCount => ChunksX * ChunksY * ChunksZ;

        public bool InBounds(int x, int y, int z) =>
            x >= 0 && y >= 0 && z >= 0 && x < X && y < Y && z < Z;

        /// <summary>Chỉ số phẳng: x chạy nhanh nhất, rồi z, rồi y (từng lớp ngang).</summary>
        public int Index(int x, int y, int z) => x + X * (z + Z * y);

        public void Coords(int index, out int x, out int y, out int z)
        {
            x = index % X;
            int rest = index / X;
            z = rest % Z;
            y = rest / Z;
        }

        /// <summary>Chỉ số chunk chứa ô (x, y, z).</summary>
        public int ChunkIndex(int x, int y, int z)
        {
            int cx = x / ChunkSize, cy = y / ChunkSize, cz = z / ChunkSize;
            return cx + ChunksX * (cz + ChunksZ * cy);
        }

        public bool Equals(WorldSize other) => X == other.X && Y == other.Y && Z == other.Z;
        public override bool Equals(object obj) => obj is WorldSize other && Equals(other);
        public override int GetHashCode() => (X * 397 ^ Y) * 397 ^ Z;
        public override string ToString() => $"{X}×{Y}×{Z}";
    }

    /// <summary>Lưới gameplay — nguồn chuẩn của world (lưới dựng mesh suy ra từ đây, không lưu).</summary>
    public sealed class WorldData
    {
        public readonly WorldSize Size;
        readonly ushort[] cells;

        public WorldData(WorldSize size)
        {
            Size = size;
            cells = new ushort[size.CellCount];
        }

        public WorldData(WorldSize size, ushort[] cells)
        {
            if (cells == null || cells.Length != size.CellCount)
                throw new ArgumentException($"Cần {size.CellCount} ô, nhận {cells?.Length}");
            Size = size;
            this.cells = cells;
        }

        /// <summary>Mảng ô gốc (đọc/ghi trực tiếp — dùng khi sinh world, băm, dựng mesh).</summary>
        public ushort[] Cells => cells;

        public ushort Get(int x, int y, int z) => cells[Size.Index(x, y, z)];

        public void Set(int x, int y, int z, ushort cell) => cells[Size.Index(x, y, z)] = cell;

        public BlockMaterial GetMaterial(int x, int y, int z) => Cell.GetMaterial(Get(x, y, z));

        /// <summary>Ô rắn cao nhất trong cột (không tính nước); -1 nếu cột rỗng.</summary>
        public int SurfaceHeight(int x, int z)
        {
            for (int y = Size.Y - 1; y >= 0; y--)
            {
                var m = GetMaterial(x, y, z);
                if (m != BlockMaterial.Empty && m != BlockMaterial.Water)
                    return y;
            }
            return -1;
        }
    }
}
