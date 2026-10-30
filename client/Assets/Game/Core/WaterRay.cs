using System;

namespace Game.Core
{
    /// <summary>Ô nước đầu tiên mà tia đi vào (brief 0086, task 5j lỗi 4): đi từng ô theo Amanatides–Woo, toạ độ ô (terrain). Chỉ tìm nước — chặn bởi khối đặc là việc của người gọi (giới hạn maxT).</summary>
    public static class WaterRay
    {
        /// <summary>
        /// Tia o + t·d (0 ≤ t ≤ maxT). Trúng ô Water (mọi loại: nguồn, chảy, rơi) → true, t = lúc đi vào ô, (nx, ny, nz) = pháp tuyến mặt đi vào;
        /// gốc trong ô nước → t = 0, pháp tuyến (0, 1, 0). Ô ngoài world bỏ qua. d gần 0 (|d|² &lt; 1e-12) hoặc không trúng → false, t = 0, pháp tuyến 0.
        /// </summary>
        public static bool FirstWater(WorldData world, float ox, float oy, float oz, float dx, float dy, float dz, float maxT,
            out float t, out int nx, out int ny, out int nz) =>
            FirstWater(world, ox, oy, oz, dx, dy, dz, maxT, out t, out nx, out ny, out nz, out _);

        /// <summary>
        /// Như overload trên. reachesEnd: từ ô nước đầu tiên tới maxT tia đi liền trong nước (hoặc tới một ô đặc) — không qua ô rỗng / ngoài world (brief 0089).
        /// </summary>
        public static bool FirstWater(WorldData world, float ox, float oy, float oz, float dx, float dy, float dz, float maxT,
            out float t, out int nx, out int ny, out int nz, out bool reachesEnd)
        {
            reachesEnd = false;
            if (world == null) throw new ArgumentNullException(nameof(world));
            t = 0f; nx = 0; ny = 0; nz = 0;
            if (dx * dx + dy * dy + dz * dz < 1e-12f) return false;

            int x = (int)Math.Floor(ox), y = (int)Math.Floor(oy), z = (int)Math.Floor(oz);
            int sx = Math.Sign(dx), sy = Math.Sign(dy), sz = Math.Sign(dz);
            float tdx = sx != 0 ? 1f / Math.Abs(dx) : float.PositiveInfinity;
            float tdy = sy != 0 ? 1f / Math.Abs(dy) : float.PositiveInfinity;
            float tdz = sz != 0 ? 1f / Math.Abs(dz) : float.PositiveInfinity;
            float tmx = sx > 0 ? (x + 1 - ox) / dx : sx < 0 ? (ox - x) / -dx : float.PositiveInfinity;
            float tmy = sy > 0 ? (y + 1 - oy) / dy : sy < 0 ? (oy - y) / -dy : float.PositiveInfinity;
            float tmz = sz > 0 ? (z + 1 - oz) / dz : sz < 0 ? (oz - z) / -dz : float.PositiveInfinity;

            float cur = 0f;
            int ax = 0, ay = 1, az = 0; // pháp tuyến mặt đi vào
            int maxSteps = 3 * ((int)Math.Ceiling(maxT) + 2);
            for (int i = 0; i <= maxSteps; i++)
            {
                if (world.Size.InBounds(x, y, z) && world.GetMaterial(x, y, z) == BlockMaterial.Water)
                {
                    t = cur; nx = ax; ny = ay; nz = az;
                    reachesEnd = WaterRunReachesEnd(world, x, y, z, sx, sy, sz, tdx, tdy, tdz, tmx, tmy, tmz, maxT, maxSteps - i);
                    return true;
                }
                if (tmx <= tmy && tmx <= tmz)
                {
                    cur = tmx; if (cur > maxT) return false;
                    x += sx; tmx += tdx; ax = -sx; ay = 0; az = 0;
                }
                else if (tmy <= tmz)
                {
                    cur = tmy; if (cur > maxT) return false;
                    y += sy; tmy += tdy; ax = 0; ay = -sy; az = 0;
                }
                else
                {
                    cur = tmz; if (cur > maxT) return false;
                    z += sz; tmz += tdz; ax = 0; ay = 0; az = -sz;
                }
            }
            return false;
        }

        /// <summary>Đi tiếp từ ô nước (x, y, z): mọi ô đi vào trước maxT − 1e-3 là nước, hoặc gặp ô đặc trước ô rỗng / ngoài world → true.</summary>
        static bool WaterRunReachesEnd(WorldData world, int x, int y, int z, int sx, int sy, int sz,
            float tdx, float tdy, float tdz, float tmx, float tmy, float tmz, float maxT, int steps)
        {
            for (int i = 0; i < steps; i++)
            {
                float cur;
                if (tmx <= tmy && tmx <= tmz) { cur = tmx; x += sx; tmx += tdx; }
                else if (tmy <= tmz) { cur = tmy; y += sy; tmy += tdy; }
                else { cur = tmz; z += sz; tmz += tdz; }
                if (cur >= maxT - 1e-3f) return true;
                if (!world.Size.InBounds(x, y, z)) return false;
                var m = world.GetMaterial(x, y, z);
                if (m == BlockMaterial.Empty) return false;
                if (m != BlockMaterial.Water) return true;
            }
            return true;
        }
    }
}
