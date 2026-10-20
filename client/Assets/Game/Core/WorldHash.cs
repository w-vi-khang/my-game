namespace Game.Core
{
    /// <summary>
    /// Mã băm FNV-1a 64 bit của world (kích thước + mọi ô). Chỉ dùng phép toán số nguyên → giống nhau trên mọi máy.
    /// Dùng để kiểm tất định: cùng seed + cùng phiên bản bộ sinh → cùng mã băm (world-tech bước 1).
    /// </summary>
    public static class WorldHash
    {
        const ulong OffsetBasis = 14695981039346656037UL;
        const ulong Prime = 1099511628211UL;

        public static ulong Compute(WorldData world)
        {
            ulong h = OffsetBasis;
            h = MixInt(h, world.Size.X);
            h = MixInt(h, world.Size.Y);
            h = MixInt(h, world.Size.Z);
            var cells = world.Cells;
            for (int i = 0; i < cells.Length; i++)
            {
                ushort c = cells[i];
                h = (h ^ (byte)c) * Prime;
                h = (h ^ (byte)(c >> 8)) * Prime;
            }
            return h;
        }

        /// <summary>Mã băm bảng map (kích thước + id từng cột); không băm tên map.</summary>
        public static ulong Compute(MapTable maps)
        {
            ulong h = OffsetBasis;
            h = MixInt(h, maps.SizeX);
            h = MixInt(h, maps.SizeZ);
            var ids = maps.Ids;
            for (int i = 0; i < ids.Length; i++)
                h = (h ^ ids[i]) * Prime;
            return h;
        }

        public static string ToHex(ulong hash) => hash.ToString("x16");

        static ulong MixInt(ulong h, int v)
        {
            for (int i = 0; i < 4; i++)
            {
                h = (h ^ (byte)(v >> (8 * i))) * Prime;
            }
            return h;
        }
    }
}
