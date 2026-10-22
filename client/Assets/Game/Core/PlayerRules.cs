using System;

namespace Game.Core
{
    /// <summary>
    /// Luật nhân vật người chơi M1 (world 7h, 0028): tầm với 4 ô từ tâm thân, ô bị thân chiếm, biên world, trong nước.
    /// Gốc nhân vật = chân (giữa đáy); thân cao BodyHeight, nửa rộng HalfWidth.
    /// </summary>
    public static class PlayerRules
    {
        public const float Reach = 4f;
        /// <summary>Tâm thân cao hơn chân chừng này (nhân vật 2 ô).</summary>
        public const float BodyCenterHeight = 1f;
        public const float HalfWidth = 0.4f;
        public const float BodyHeight = 1.8f;
        public const float WaterSpeedFactor = 0.5f;
        const float Shrink = 0.001f;

        /// <summary>Tâm ô (x, y, z) cách tâm thân (chân + BodyCenterHeight) ≤ Reach (Euclid).</summary>
        public static bool InReach(float feetX, float feetY, float feetZ, int x, int y, int z)
        {
            float dx = x + 0.5f - feetX, dy = y + 0.5f - (feetY + BodyCenterHeight), dz = z + 0.5f - feetZ;
            return dx * dx + dy * dy + dz * dz <= Reach * Reach + 1e-4f;
        }

        /// <summary>Hộp thân (co Shrink mỗi phía) giao ô (x, y, z).</summary>
        public static bool Occupies(float feetX, float feetY, float feetZ, int x, int y, int z) =>
            feetX + HalfWidth - Shrink > x && feetX - HalfWidth + Shrink < x + 1 &&
            feetZ + HalfWidth - Shrink > z && feetZ - HalfWidth + Shrink < z + 1 &&
            feetY + BodyHeight - Shrink > y && feetY + Shrink < y + 1;

        /// <summary>Cắt bước đi ngang để chân luôn trong [HalfWidth, Size − HalfWidth] theo x và z.</summary>
        public static void ClampToWorld(WorldSize s, float feetX, float feetZ, ref float dx, ref float dz)
        {
            dx = Math.Min(Math.Max(feetX + dx, HalfWidth), s.X - HalfWidth) - feetX;
            dz = Math.Min(Math.Max(feetZ + dz, HalfWidth), s.Z - HalfWidth) - feetZ;
        }

        /// <summary>Ô chứa điểm chân + 0,5 là nước (ngoài world → false).</summary>
        public static bool InWater(WorldData world, float feetX, float feetY, float feetZ)
        {
            int x = (int)Math.Floor(feetX), y = (int)Math.Floor(feetY + 0.5f), z = (int)Math.Floor(feetZ);
            return world.Size.InBounds(x, y, z) && world.GetMaterial(x, y, z) == BlockMaterial.Water;
        }
    }
}
