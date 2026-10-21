using System;
using Game.Core.DayCycle;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game.Client
{
    /// <summary>
    /// Buổi trong ngày trên client (0024, brief 0023): pha từ TimeOfDay (giờ máy + offset) → bầu trời (biến global của Game/SkyClouds),
    /// đèn chính (mặt trời/mặt trăng), ambient Trilight, màu sương, tint dưới nước. Chọn mốc → xoay theo chiều tiến trong
    /// rotateDurationMs, ease in-out; đang xoay thì bỏ qua lệnh chọn. Chạy trước UnderwaterView/WorldEdgeFog để ghi màu sương đúng khung.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class DayCycleView : MonoBehaviour
    {
        public Light sun;
        public UnderwaterView underwater;
        /// <summary>&gt; 0: cứ chừng này giây tự chọn mốc (CurrentWindow + 2) % 4 — chỉ dùng ở scene Bench.</summary>
        public float benchCycleSeconds = 0f;
        [NonSerialized] public Func<long> clockMs;
        [NonSerialized] public Func<double> secondsNow;

        public TimeOfDay Day { get; private set; }
        public int DisplayPhaseMs { get; private set; }
        public DaySkyState State { get; private set; }
        public bool IsRotating => rotation != null && rotation.IsRotating(Now());

        SkyRotation rotation;
        double nextBench;

        static readonly int DayCycleOnId = Shader.PropertyToID("_DayCycleOn");
        static readonly int ZenithId = Shader.PropertyToID("_DayZenith");
        static readonly int HorizonId = Shader.PropertyToID("_DayHorizon");
        static readonly int CloudId = Shader.PropertyToID("_DayCloud");
        static readonly int CloudShadeId = Shader.PropertyToID("_DayCloudShade");
        static readonly int SunColorId = Shader.PropertyToID("_DaySunColor");
        static readonly int MoonColorId = Shader.PropertyToID("_DayMoonColor");
        static readonly int SunDirId = Shader.PropertyToID("_DaySunDir");
        static readonly int StarsId = Shader.PropertyToID("_DayStars");
        static readonly int StarRotId = Shader.PropertyToID("_DayStarRot");

        public static long LocalClockMs() => DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;

        double Now() => secondsNow != null ? secondsNow() : Time.unscaledTimeAsDouble;

        void Start()
        {
            if (sun == null)
            {
                foreach (var l in FindObjectsByType<Light>(FindObjectsSortMode.None))
                    if (l.type == LightType.Directional && l.enabled)
                    {
                        sun = l;
                        break;
                    }
            }
            if (underwater == null)
                underwater = GetComponent<UnderwaterView>();
            var cfg = ConfigLoader.LoadTimeConfig();
            Day = new TimeOfDay(cfg, clockMs ?? LocalClockMs);
            rotation = new SkyRotation(cfg.rotateDurationMs / 1000.0);
            RenderSettings.ambientMode = AmbientMode.Trilight;
            nextBench = Now() + benchCycleSeconds;
            Apply();
        }

        void LateUpdate()
        {
            if (Day == null)
                return;
            if (benchCycleSeconds > 0 && Now() >= nextBench)
            {
                Select((TimeLandmark)(((int)Day.CurrentWindow + 2) % 4));
                nextBench += benchCycleSeconds;
            }
            Apply();
        }

        /// <summary>Chọn mốc (0024): trả cung xoay (ms); 0 = đang trong khung mốc, không đổi; −1 = đang xoay, bỏ qua.</summary>
        public int Select(TimeLandmark m)
        {
            double now = Now();
            if (rotation.IsRotating(now))
                return -1;
            int from = Day.PhaseMs;
            int arc = Day.SelectLandmark(m);
            if (arc > 0)
            {
                rotation.Start(arc, now);
                Debug.Log($"DAYCYCLE select={m} arcMs={arc} fromMs={from} toMs={Day.PhaseMs}");
            }
            return arc;
        }

        public void Apply()
        {
            DisplayPhaseMs = rotation.DisplayPhaseMs(Day.PhaseMs, Now());
            var s = DaySky.Evaluate(DisplayPhaseMs / (float)TimeConfig.DayMs);
            State = s;
            if (sun != null)
            {
                sun.transform.rotation = Quaternion.LookRotation(s.lightForward);
                sun.color = s.lightColor;
                sun.intensity = s.lightIntensity;
            }
            RenderSettings.ambientSkyColor = s.ambientSky;
            RenderSettings.ambientEquatorColor = s.ambientEquator;
            RenderSettings.ambientGroundColor = s.ambientGround;
            if (underwater != null)
            {
                underwater.Refresh();
                underwater.ApplyTint(s.waterTint);
            }
            if (underwater == null || !underwater.IsUnderwater)
                RenderSettings.fogColor = s.horizon;
            Shader.SetGlobalFloat(DayCycleOnId, 1f);
            Shader.SetGlobalColor(ZenithId, s.zenith);
            Shader.SetGlobalColor(HorizonId, s.horizon);
            Shader.SetGlobalColor(CloudId, s.cloud);
            Shader.SetGlobalColor(CloudShadeId, s.cloudShade);
            Shader.SetGlobalColor(SunColorId, s.sunColor);
            Shader.SetGlobalColor(MoonColorId, DaySky.MoonDiscColor);
            Shader.SetGlobalVector(SunDirId, (Vector4)s.sunDir);
            Shader.SetGlobalFloat(StarsId, s.stars);
            Shader.SetGlobalMatrix(StarRotId, Matrix4x4.Rotate(Quaternion.Inverse(s.starRotation)));
        }

        void OnDisable()
        {
            Shader.SetGlobalFloat(DayCycleOnId, 0f);
        }
    }
}
