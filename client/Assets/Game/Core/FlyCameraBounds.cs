using System;

namespace Game.Core
{
    /// <summary>
    /// Biên bay của camera (world-tech bước 7b3, brief 0012): trong SlowZone ô cuối trước mép x/z và trần, thành phần vận tốc
    /// hướng ra ngoài giảm tuyến tính về 0, không bao giờ vượt biên; sương dày dần theo khoảng cách tới mép ngang.
    /// </summary>
    public static class FlyCameraBounds
    {
        /// <summary>Bề rộng vùng chậm dần trước mép / trần (ô).</summary>
        public const float SlowZone = 2f;
        /// <summary>Trần bay cao hơn đỉnh world chừng này ô.</summary>
        public const int CeilingAbove = 16;
        /// <summary>Cuối sương (đơn vị thế giới) khi camera ở đúng mép world.</summary>
        public const float EdgeFogEnd = 1f;

        public static float CeilingY(WorldSize s) => s.Y + CeilingAbove;

        /// <summary>Dời d theo một trục từ pos, biên [min, max]: hướng ra ngoài nhân (khoảng còn lại / SlowZone, tối đa 1) và không vượt biên; đã ở ngoài biên thì không đi xa thêm.</summary>
        public static float LimitAxis(float pos, float d, float min, float max)
        {
            if (d > 0f)
            {
                float room = max - pos;
                if (room <= 0f) return 0f;
                return Math.Min(d * Math.Min(room / SlowZone, 1f), room);
            }
            if (d < 0f)
            {
                float room = pos - min;
                if (room <= 0f) return 0f;
                return Math.Max(d * Math.Min(room / SlowZone, 1f), -room);
            }
            return d;
        }

        /// <summary>Giới hạn một lần dời camera (toạ độ terrain): x ∈ [0, Size.X], z ∈ [0, Size.Z], y ≤ CeilingY; không giới hạn dưới.</summary>
        public static void LimitDelta(WorldSize s, float x, float y, float z, ref float dx, ref float dy, ref float dz)
        {
            dx = LimitAxis(x, dx, 0f, s.X);
            dz = LimitAxis(z, dz, 0f, s.Z);
            dy = LimitAxis(y, dy, float.NegativeInfinity, CeilingY(s));
        }

        /// <summary>Độ dày sương biên: 0 khi cách mép ngang gần nhất ≥ SlowZone, 1 ở mép hoặc ngoài world.</summary>
        public static float EdgeFactor(WorldSize s, float x, float z)
        {
            float dist = Math.Min(Math.Min(x, s.X - x), Math.Min(z, s.Z - z));
            return 1f - Math.Min(Math.Max(dist / SlowZone, 0f), 1f);
        }

        /// <summary>Sương tuyến tính khi độ dày biên = t: cuối sương giảm theo cấp số nhân từ baseEnd về EdgeFogEnd, đầu sương về 0 và luôn nhỏ hơn cuối.</summary>
        public static void EdgeFog(float baseStart, float baseEnd, float t, out float start, out float end)
        {
            end = baseEnd * (float)Math.Pow(EdgeFogEnd / baseEnd, t);
            start = end * (baseStart / baseEnd) * (1f - t);
        }

        /// <summary>Góc ngẩng của hướng nhìn (độ): 0 = ngang, dương = nhìn lên. forwardY = thành phần y của vector hướng nhìn đơn vị.</summary>
        public static float ElevationDegrees(float forwardY) =>
            (float)(Math.Asin(Math.Min(Math.Max(forwardY, -1f), 1f)) * 180.0 / Math.PI);

        /// <summary>Chuỗi góc cho HUD: số nguyên làm tròn, "+12°", "-5°", "0°".</summary>
        public static string FormatElevation(float degrees)
        {
            int r = (int)Math.Round(degrees, MidpointRounding.AwayFromZero);
            return r > 0 ? $"+{r}°" : r < 0 ? $"-{-r}°" : "0°";
        }
    }
}
