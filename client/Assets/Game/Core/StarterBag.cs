using System;
using System.Collections.Generic;

namespace Game.Core
{
    /// <summary>Túi hạt đầu game (0028, task contract 7e): vật rơi túi hạt Cây A × 4 cách chỗ xuất phát 3 ô theo hướng nhìn, trên mặt đất trong tầm việc tay.</summary>
    public static class StarterBag
    {
        /// <summary>Cây A = SimConfig.crops[0] (design/data/sim_m1.json).</summary>
        public static int Crop { get; internal set; } = 0;
        public static int Seeds { get; internal set; } = 4;
        public const int Distance = 3;
        /// <summary>Nửa cạnh vùng dự phòng 5 × 5.</summary>
        public const int SearchRadius = 2;

        /// <summary>Ô đặt túi hạt theo QĐ 8 brief 0037; false nếu không có ô nào.</summary>
        public static bool TryFindSpot(WorldData world, float feetX, float feetY, float feetZ, int dirX, int dirZ, out int x, out int y, out int z)
        {
            if (world == null) throw new ArgumentNullException(nameof(world));
            int sx = (int)Math.Floor(feetX), sz = (int)Math.Floor(feetZ);
            int px = sx + Distance * dirX, pz = sz + Distance * dirZ;
            if (Standable(world, px, pz, feetX, feetY, feetZ, out y)) { x = px; z = pz; return true; }

            var cols = new List<(int dx, int dz)>();
            for (int dz = -SearchRadius; dz <= SearchRadius; dz++)
                for (int dx = -SearchRadius; dx <= SearchRadius; dx++)
                {
                    if (dx == 0 && dz == 0) continue;
                    if (sx + dx == px && sz + dz == pz) continue;
                    cols.Add((dx, dz));
                }
            cols.Sort((a, b) =>
            {
                int c = (a.dx * a.dx + a.dz * a.dz).CompareTo(b.dx * b.dx + b.dz * b.dz);
                if (c != 0) return c;
                c = a.dz.CompareTo(b.dz);
                return c != 0 ? c : a.dx.CompareTo(b.dx);
            });
            foreach (var (dx, dz) in cols)
                if (Standable(world, sx + dx, sz + dz, feetX, feetY, feetZ, out y)) { x = sx + dx; z = sz + dz; return true; }
            x = y = z = 0;
            return false;
        }

        /// <summary>Đặt vật rơi SeedBag(Crop) × Seeds vào ô của TryFindSpot; trả chồng, hoặc null nếu không có ô.</summary>
        public static DropStack Place(WorldEditor editor, float feetX, float feetY, float feetZ, int dirX, int dirZ)
        {
            if (editor == null) throw new ArgumentNullException(nameof(editor));
            if (!TryFindSpot(editor.World, feetX, feetY, feetZ, dirX, dirZ, out int x, out int y, out int z)) return null;
            return editor.Drops.Add(x, y, z, ItemId.SeedBag(Crop), Seeds);
        }

        static bool Standable(WorldData w, int cx, int cz, float feetX, float feetY, float feetZ, out int y)
        {
            y = 0;
            var size = w.Size;
            if (cx < 0 || cz < 0 || cx >= size.X || cz >= size.Z) return false;
            int top = -1;
            for (int j = size.Y - 1; j >= 0; j--)
                if (w.GetMaterial(cx, j, cz) != BlockMaterial.Empty) { top = j; break; }
            if (top < 0) return false;
            if (!WorldEditor.IsSolidAt(w, cx, top, cz)) return false;
            if (top + 1 >= size.Y) return false;
            y = top + 1;
            if (Math.Abs(y - (int)Math.Floor(feetY)) > 1) return false;
            return PlayerRules.InTargetReach(feetX, feetY, feetZ, cx, y, cz);
        }
    }
}
