using System;
using System.Collections.Generic;

namespace Game.Core
{
    /// <summary>Chọn chồng vật rơi bằng tia (task contract 7e, brief 0037): hộp cạnh BoxSize, đáy ở mặt ô, giữa ô theo x/z; chỉ chồng trong tầm việc tay.</summary>
    public static class DropPick
    {
        public const float BoxSize = 0.5f;

        /// <summary>
        /// Chồng có hộp bị tia (o + t·d, t ≥ 0) trúng với t ≤ maxDistance và tâm ô trong tầm việc tay của chân (feet); t nhỏ nhất thắng,
        /// bằng nhau → chồng đứng trước trong stacks. Không có → null, distance = 0. Toạ độ ô (terrain).
        /// </summary>
        public static DropStack Pick(IReadOnlyList<DropStack> stacks, float ox, float oy, float oz, float dx, float dy, float dz,
            float maxDistance, float feetX, float feetY, float feetZ, out float distance)
        {
            if (stacks == null) throw new ArgumentNullException(nameof(stacks));
            DropStack best = null;
            float bestT = float.MaxValue;
            const float h = BoxSize * 0.5f;
            for (int i = 0; i < stacks.Count; i++)
            {
                var s = stacks[i];
                if (!PlayerRules.InHandReach(feetX, feetY, feetZ, s.X, s.Y, s.Z)) continue;
                if (!RayBox(ox, oy, oz, dx, dy, dz,
                        s.X + 0.5f - h, s.Y, s.Z + 0.5f - h, s.X + 0.5f + h, s.Y + BoxSize, s.Z + 0.5f + h, out float t)) continue;
                if (t > maxDistance) continue;
                if (t < bestT) { bestT = t; best = s; }
            }
            distance = best != null ? bestT : 0f;
            return best;
        }

        /// <summary>Tia cắt hộp AABB (slab); gốc trong hộp → t = 0.</summary>
        public static bool RayBox(float ox, float oy, float oz, float dx, float dy, float dz,
            float minX, float minY, float minZ, float maxX, float maxY, float maxZ, out float t)
        {
            t = 0f;
            float tmin = 0f, tmax = float.PositiveInfinity;
            if (!Slab(ox, dx, minX, maxX, ref tmin, ref tmax)) return false;
            if (!Slab(oy, dy, minY, maxY, ref tmin, ref tmax)) return false;
            if (!Slab(oz, dz, minZ, maxZ, ref tmin, ref tmax)) return false;
            t = tmin;
            return true;
        }

        static bool Slab(float o, float d, float min, float max, ref float tmin, ref float tmax)
        {
            if (Math.Abs(d) < 1e-8f) return o >= min && o <= max;
            float t1 = (min - o) / d, t2 = (max - o) / d;
            if (t1 > t2) { float x = t1; t1 = t2; t2 = x; }
            tmin = Math.Max(tmin, t1);
            tmax = Math.Min(tmax, t2);
            return tmin <= tmax;
        }
    }
}
