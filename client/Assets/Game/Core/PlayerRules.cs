using System;
using Game.Core.Sim;

namespace Game.Core
{
    /// <summary>
    /// Luật nhân vật người chơi M1 (world 7h, 0028): tầm với mọi thao tác 8 ô (0031) từ tâm thân, ô bị thân chiếm, biên world, trong nước.
    /// Gốc nhân vật = chân (giữa đáy); thân cao BodyHeight, nửa rộng HalfWidth.
    /// </summary>
    public static class PlayerRules
    {
        /// <summary>Tầm công cụ: đào, đặt, xới, click ô ruộng (0031: 8 ô). Client hiện tại còn dùng; 7l c (0034) chuyển sang TargetReach/ActReach rồi gỡ.</summary>
        /// (đọc từ sim_m1.json player.toolReach — Configure)
        public static float ToolReach { get; private set; } = 8f;
        /// <summary>Tầm việc tay: nhặt (sau này gieo, thu hoạch, phép Mưa, mở kho — 0031: 8 ô). Client hiện tại còn dùng; 7l c (0034) chuyển sang TargetReach/ActReach rồi gỡ.</summary>
        /// (đọc từ sim_m1.json player.handReach — Configure)
        public static float HandReach { get; private set; } = 8f;
        public static float TargetReach { get; private set; } = 10f; // tầm thao tác (0034)
        public static float ActReach { get; private set; } = 2f;     // tầm thực hiện
        public static float DigReach { get; private set; } = 2f;     // tầm đào (nâng cấp sau)
        public static float CollectReach { get; private set; } = 2f; // Thu (F)

        /// <summary>Đặt luật người chơi từ cấu hình (ConfigLoader gọi sau Validate). null → ArgumentNullException.</summary>
        public static void Configure(PlayerConfig p)
        {
            if (p == null) throw new ArgumentNullException(nameof(p));
            ToolReach = p.toolReach;
            HandReach = p.handReach;
            TargetReach = p.targetReach;
            ActReach = p.actReach;
            DigReach = p.digReach;
            CollectReach = p.collectReach;
            StarterBag.Crop = p.starterCrop;
            StarterBag.Seeds = p.starterSeeds;
        }
        /// <summary>Tâm thân cao hơn chân chừng này (nhân vật 2 ô).</summary>
        public const float BodyCenterHeight = 1f;
        public const float HalfWidth = 0.4f;
        public const float BodyHeight = 1.8f;
        public const float WaterSpeedFactor = 0.5f;
        const float Shrink = 0.001f;

        public static bool InToolReach(float feetX, float feetY, float feetZ, int x, int y, int z) =>
            Within(ToolReach, feetX, feetY, feetZ, x, y, z);
        public static bool InHandReach(float feetX, float feetY, float feetZ, int x, int y, int z) =>
            Within(HandReach, feetX, feetY, feetZ, x, y, z);
        public static bool InTargetReach(float feetX, float feetY, float feetZ, int x, int y, int z) =>
            Within(TargetReach, feetX, feetY, feetZ, x, y, z);
        public static bool InActReach(float feetX, float feetY, float feetZ, int x, int y, int z) =>
            Within(ActReach, feetX, feetY, feetZ, x, y, z);
        public static bool InDigReach(float feetX, float feetY, float feetZ, int x, int y, int z) =>
            Within(DigReach, feetX, feetY, feetZ, x, y, z);
        public static bool InCollectReach(float feetX, float feetY, float feetZ, int x, int y, int z) =>
            Within(CollectReach, feetX, feetY, feetZ, x, y, z);

        /// <summary>Tâm ô (x, y, z) cách tâm thân (chân + BodyCenterHeight) ≤ reach (Euclid).</summary>
        static bool Within(float reach, float feetX, float feetY, float feetZ, int x, int y, int z)
        {
            float dx = x + 0.5f - feetX, dy = y + 0.5f - (feetY + BodyCenterHeight), dz = z + 0.5f - feetZ;
            return dx * dx + dy * dy + dz * dz <= reach * reach + 1e-4f;
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
