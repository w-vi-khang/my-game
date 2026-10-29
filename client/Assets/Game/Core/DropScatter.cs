using System;

namespace Game.Core
{
    /// <summary>Độ lệch hiển thị của chồng vật rơi trong ô (brief 0087, task 5j lỗi 5): tất định theo Id chồng; chỉ dùng cho vẽ và chọn bằng tia, không đổi ô của chồng.</summary>
    public static class DropScatter
    {
        /// <summary>Lệch ngang (dx, dz) đều trong hình tròn bán kính radius quanh tâm ô, góc quay yawDeg trong [0, 360). radius &lt; 0 → ArgumentOutOfRangeException.</summary>
        public static void Offset(int stackId, float radius, out float dx, out float dz, out float yawDeg)
        {
            if (radius < 0f) throw new ArgumentOutOfRangeException(nameof(radius));
            uint b = unchecked((uint)stackId * 3u);
            double u1 = Hash(b) / 4294967296.0;
            double u2 = Hash(unchecked(b + 1u)) / 4294967296.0;
            double u3 = Hash(unchecked(b + 2u)) / 4294967296.0;
            double r = radius * Math.Sqrt(u1);
            double a = 2.0 * Math.PI * u2;
            dx = (float)(r * Math.Cos(a));
            dz = (float)(r * Math.Sin(a));
            yawDeg = (float)(360.0 * u3);
            if (yawDeg >= 360f) yawDeg = 0f;
        }

        /// <summary>Băm 32 bit (lowbias32).</summary>
        public static uint Hash(uint x)
        {
            unchecked
            {
                x ^= x >> 16; x *= 0x7feb352dU; x ^= x >> 15; x *= 0x846ca68bU; x ^= x >> 16;
                return x;
            }
        }
    }
}
