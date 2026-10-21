using UnityEngine;

namespace Game.Client
{
    /// <summary>Trạng thái bầu trời/ánh sáng tại một pha trong ngày (brief 0023, 0024).</summary>
    public struct DaySkyState
    {
        public Color zenith, horizon, cloud, cloudShade;              // horizon cũng là màu sương trên cạn
        public Color ambientSky, ambientEquator, ambientGround;
        public Color sunColor;                                         // màu đĩa mặt trời và màu đèn ban ngày
        public Color lightColor;                                       // màu đèn thật (mặt trời hoặc mặt trăng)
        public Color waterTint;                                        // nhân vào màu dưới nước
        public float lightIntensity, stars;                            // stars 0..1
        public Vector3 sunDir;                                         // hướng TỚI mặt trời (đơn vị)
        public Vector3 lightForward;                                   // hướng chiếu của đèn
        public Quaternion starRotation;                                // starRotation * NoonSunDir = sunDir
    }

    /// <summary>
    /// Bầu trời theo pha (brief 0023): quỹ đạo mặt trời qua NoonSunDir (giữa trưa = góc đèn cũ 50°, −30°) và SetDir (lặn ở pha 0,75);
    /// một đèn dùng cho mặt trời (S.y ≥ 0) và mặt trăng (đối diện); màu nội suy theo 8 khoá [số tạm], giữa trưa = màu scene cũ.
    /// </summary>
    public static class DaySky
    {
        public const float NoonPitch = 50f, Yaw = -30f;
        public const float SunIntensity = 0.9f, MoonIntensity = 0.22f, LightRamp = 0.25f;
        public static readonly Color MoonLightColor = new Color(0.62f, 0.70f, 0.95f);
        public static readonly Color MoonDiscColor = new Color(0.85f, 0.88f, 0.95f);
        public static readonly Vector3 NoonSunDir = -(Quaternion.Euler(NoonPitch, Yaw, 0f) * Vector3.forward);
        public static readonly Vector3 SetDir = Quaternion.Euler(0f, Yaw, 0f) * Vector3.right;
        public static readonly Vector3 Axis = Vector3.Cross(NoonSunDir, SetDir).normalized;

        struct Key
        {
            public float phase, stars;
            public Color zenith, horizon, cloud, cloudShade, ambientSky, ambientEquator, ambientGround, sun, waterTint;
        }

        static Color C(float r, float g, float b) => new Color(r, g, b, 1f);

        static Key Night() => new Key
        {
            stars = 1f,
            zenith = C(.02f, .03f, .09f), horizon = C(.08f, .11f, .20f), cloud = C(.16f, .18f, .26f), cloudShade = C(.10f, .12f, .18f),
            ambientSky = C(.12f, .15f, .26f), ambientEquator = C(.09f, .10f, .14f), ambientGround = C(.05f, .05f, .07f),
            sun = C(1f, .62f, .38f), waterTint = C(.22f, .26f, .38f),
        };

        static Key Twilight() => new Key
        {
            stars = 0.5f,
            zenith = C(.06f, .08f, .20f), horizon = C(.30f, .26f, .38f), cloud = C(.34f, .30f, .40f), cloudShade = C(.20f, .20f, .30f),
            ambientSky = C(.20f, .22f, .36f), ambientEquator = C(.20f, .18f, .24f), ambientGround = C(.10f, .09f, .10f),
            sun = C(1f, .62f, .38f), waterTint = C(.35f, .36f, .48f),
        };

        static Key Sunrise() => new Key
        {
            stars = 0f,
            zenith = C(.30f, .40f, .66f), horizon = C(.96f, .66f, .46f), cloud = C(1f, .80f, .66f), cloudShade = C(.62f, .50f, .56f),
            ambientSky = C(.52f, .52f, .66f), ambientEquator = C(.74f, .56f, .46f), ambientGround = C(.30f, .26f, .24f),
            sun = C(1f, .62f, .38f), waterTint = C(.80f, .70f, .68f),
        };

        static Key DayKey() => new Key
        {
            stars = 0f,
            zenith = C(.36f, .62f, .92f), horizon = C(.78f, .88f, .97f), cloud = C(1f, 1f, 1f), cloudShade = C(.80f, .85f, .93f),
            ambientSky = C(.72f, .82f, .96f), ambientEquator = C(.70f, .69f, .64f), ambientGround = C(.42f, .37f, .30f),
            sun = C(1f, .95f, .86f), waterTint = C(1f, 1f, 1f),
        };

        static Key At(Key k, float phase) { k.phase = phase; return k; }

        static readonly Key[] Keys =
        {
            At(Night(), 0.00f), At(Twilight(), 0.20f), At(Sunrise(), 0.25f), At(DayKey(), 0.30f),
            At(DayKey(), 0.50f), At(DayKey(), 0.70f), At(Sunrise(), 0.75f), At(Twilight(), 0.80f),
        };

        public static DaySkyState Evaluate(float phase)
        {
            phase -= Mathf.Floor(phase);
            int i = 0;
            for (int k = 0; k < Keys.Length; k++)
                if (Keys[k].phase <= phase) i = k;
            Key a = Keys[i], b = Keys[(i + 1) % Keys.Length];
            float span = (b.phase - a.phase + 1f) % 1f;
            float t = ((phase - a.phase + 1f) % 1f) / span;

            var s = new DaySkyState
            {
                zenith = Color.Lerp(a.zenith, b.zenith, t),
                horizon = Color.Lerp(a.horizon, b.horizon, t),
                cloud = Color.Lerp(a.cloud, b.cloud, t),
                cloudShade = Color.Lerp(a.cloudShade, b.cloudShade, t),
                ambientSky = Color.Lerp(a.ambientSky, b.ambientSky, t),
                ambientEquator = Color.Lerp(a.ambientEquator, b.ambientEquator, t),
                ambientGround = Color.Lerp(a.ambientGround, b.ambientGround, t),
                sunColor = Color.Lerp(a.sun, b.sun, t),
                waterTint = Color.Lerp(a.waterTint, b.waterTint, t),
                stars = Mathf.Lerp(a.stars, b.stars, t),
            };

            float ang = (phase - 0.5f) * 2f * Mathf.PI;
            Vector3 S = Mathf.Cos(ang) * NoonSunDir + Mathf.Sin(ang) * SetDir;
            s.sunDir = S;
            if (S.y >= 0f)
            {
                s.lightForward = -S;
                s.lightColor = s.sunColor;
                s.lightIntensity = SunIntensity * Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(S.y / LightRamp));
            }
            else
            {
                s.lightForward = S;
                s.lightColor = MoonLightColor;
                s.lightIntensity = MoonIntensity * Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(-S.y / LightRamp));
            }
            s.starRotation = Quaternion.LookRotation(S, Axis) * Quaternion.Inverse(Quaternion.LookRotation(NoonSunDir, Axis));
            return s;
        }
    }
}
