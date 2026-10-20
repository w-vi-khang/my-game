using System;

namespace Game.Core
{
    /// <summary>
    /// Camera bay trên lưới ô world (world-tech bước 7b2, brief 0011): không vào ô đặc, vào được ô nước và ô trống.
    /// Camera là hộp nửa cạnh radius; dời lần lượt theo x, z, y → bị chặn trục nào thì trượt theo trục còn lại.
    /// Ngoài world theo x/z lấy cột biên gần nhất, y &lt; 0 là đặc, y ≥ Size.Y là trống (giống BlockMesher.IsSolid).
    /// </summary>
    public static class FlyCameraCollision
    {
        /// <summary>Khoảng hở còn lại giữa hộp camera và mặt ô đặc khi bị chặn.</summary>
        public const float Skin = 0.001f;
        /// <summary>Bước dời tối đa mỗi lần kiểm — nhỏ hơn 1 ô để bay nhanh không xuyên vách dày 1 ô.</summary>
        public const float MaxStep = 0.25f;

        public static bool IsSolid(WorldData world, int x, int y, int z)
        {
            var s = world.Size;
            if (y < 0)
                return true;
            if (y >= s.Y)
                return false;
            x = Math.Min(Math.Max(x, 0), s.X - 1);
            z = Math.Min(Math.Max(z, 0), s.Z - 1);
            var m = world.GetMaterial(x, y, z);
            return m != BlockMaterial.Empty && m != BlockMaterial.Water;
        }

        /// <summary>Hộp nửa cạnh radius quanh (x, y, z) chạm ô đặc nào không.</summary>
        public static bool Overlaps(WorldData world, float x, float y, float z, float radius)
        {
            int x0 = Floor(x - radius), x1 = Floor(x + radius);
            int y0 = Floor(y - radius), y1 = Floor(y + radius);
            int z0 = Floor(z - radius), z1 = Floor(z + radius);
            for (int iy = y0; iy <= y1; iy++)
            for (int iz = z0; iz <= z1; iz++)
            for (int ix = x0; ix <= x1; ix++)
                if (IsSolid(world, ix, iy, iz))
                    return true;
            return false;
        }

        /// <summary>Dời camera một đoạn (dx, dy, dz); đang kẹt trong ô đặc thì dời tự do để thoát ra.</summary>
        public static void Move(WorldData world, ref float x, ref float y, ref float z, float dx, float dy, float dz, float radius)
        {
            if (Overlaps(world, x, y, z, radius))
            {
                x += dx;
                y += dy;
                z += dz;
                return;
            }
            MoveAxis(world, ref x, ref y, ref z, 0, dx, radius);
            MoveAxis(world, ref x, ref y, ref z, 2, dz, radius);
            MoveAxis(world, ref x, ref y, ref z, 1, dy, radius);
        }

        static void MoveAxis(WorldData world, ref float x, ref float y, ref float z, int axis, float d, float radius)
        {
            if (d == 0f)
                return;
            int n = Math.Max(1, (int)Math.Ceiling(Math.Abs(d) / MaxStep));
            float step = d / n;
            for (int i = 0; i < n; i++)
            {
                float cx = x, cy = y, cz = z;
                if (axis == 0) cx += step; else if (axis == 1) cy += step; else cz += step;
                if (!Overlaps(world, cx, cy, cz, radius))
                {
                    x = cx; y = cy; z = cz;
                    continue;
                }
                // Bị chặn: áp sát mặt ô đặc (cách Skin) rồi dừng trục này.
                float c = axis == 0 ? cx : axis == 1 ? cy : cz;
                float clamped = step > 0f
                    ? Floor(c + radius) - radius - Skin
                    : Floor(c - radius) + 1 + radius + Skin;
                if (axis == 0) x = clamped; else if (axis == 1) y = clamped; else z = clamped;
                return;
            }
        }

        /// <summary>
        /// Camera ở dưới nước: ô chứa camera là nước, và ô trên không phải trống hoặc camera thấp hơn mặt nước
        /// (mặt nước của ô có không khí phía trên nằm ở y + 1 − surfaceDrop, xem BlockMesher.WaterSurfaceDrop).
        /// </summary>
        public static bool IsUnderwater(WorldData world, float x, float y, float z, float surfaceDrop)
        {
            var s = world.Size;
            int ix = Floor(x), iy = Floor(y), iz = Floor(z);
            if (iy < 0 || iy >= s.Y)
                return false;
            ix = Math.Min(Math.Max(ix, 0), s.X - 1);
            iz = Math.Min(Math.Max(iz, 0), s.Z - 1);
            if (world.GetMaterial(ix, iy, iz) != BlockMaterial.Water)
                return false;
            bool airAbove = iy + 1 >= s.Y || world.GetMaterial(ix, iy + 1, iz) == BlockMaterial.Empty;
            return !airAbove || y < iy + 1 - surfaceDrop;
        }

        static int Floor(float v) => (int)Math.Floor(v);
    }
}
