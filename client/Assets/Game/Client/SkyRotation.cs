using System;
using Game.Core.DayCycle;

namespace Game.Client
{
    /// <summary>Xoay bầu trời khi chọn mốc (0024 mục 5, brief 0023): pha hiển thị đi từ pha cũ tới pha thật theo chiều tiến trong DurationSeconds, ease in-out.</summary>
    public sealed class SkyRotation
    {
        int arcMs;
        double startSeconds;

        public SkyRotation(double durationSeconds)
        {
            if (double.IsNaN(durationSeconds) || durationSeconds <= 0)
                throw new ArgumentOutOfRangeException(nameof(durationSeconds));
            DurationSeconds = durationSeconds;
        }

        public double DurationSeconds { get; }

        public void Start(int arcMs, double nowSeconds)
        {
            if (arcMs < 0 || arcMs >= TimeConfig.DayMs) throw new ArgumentOutOfRangeException(nameof(arcMs));
            this.arcMs = arcMs;
            startSeconds = nowSeconds;
        }

        public bool IsRotating(double nowSeconds) => arcMs > 0 && nowSeconds < startSeconds + DurationSeconds;

        public int DisplayPhaseMs(int truePhaseMs, double nowSeconds)
        {
            if (!IsRotating(nowSeconds)) return truePhaseMs;
            double u = (nowSeconds - startSeconds) / DurationSeconds;
            long remaining = (long)Math.Round(arcMs * (1.0 - Ease(u)));
            long r = (truePhaseMs - remaining) % TimeConfig.DayMs;
            return (int)(r < 0 ? r + TimeConfig.DayMs : r);
        }

        public static double Ease(double u)
        {
            if (u < 0) u = 0; else if (u > 1) u = 1;
            return u * u * (3 - 2 * u);
        }
    }
}
