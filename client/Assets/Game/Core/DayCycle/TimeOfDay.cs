using System;

namespace Game.Core.DayCycle
{
    /// <summary>
    /// Buổi trong ngày (0024): pha = (giờ đồng hồ + OffsetMs) mod 1 ngày, ms nguyên. Tách khỏi đồng hồ mô phỏng — không đọc/ghi SimState.
    /// Chọn mốc: còn trong khung của mốc đó → không đổi gì; ngược lại đặt OffsetMs để pha = mốc, buổi trôi tiếp theo giờ thực.
    /// </summary>
    public sealed class TimeOfDay
    {
        readonly Func<long> clockMs;

        public TimeOfDay(TimeConfig config, Func<long> clockMs)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            if (clockMs == null) throw new ArgumentNullException(nameof(clockMs));
            config.Validate();
            Config = config;
            this.clockMs = clockMs;
        }

        public TimeConfig Config { get; }
        public int OffsetMs { get; private set; }

        public int PhaseMs => (int)Mod(clockMs() + OffsetMs, TimeConfig.DayMs);
        public DayPeriod Period => Config.PeriodAt(PhaseMs);
        public TimeLandmark CurrentWindow => Config.WindowAt(PhaseMs);

        public bool IsMet(PeriodCondition condition) => condition.IsMet(Period);

        /// <summary>Trả cung xoay chiều tiến (ms); 0 = đang trong khung mốc, không đổi gì.</summary>
        public int SelectLandmark(TimeLandmark m)
        {
            long now = clockMs();
            int phase = (int)Mod(now + OffsetMs, TimeConfig.DayMs);
            if (Config.WindowAt(phase) == m) return 0;
            int target = Config.LandmarkMs(m);
            OffsetMs = (int)Mod(target - now, TimeConfig.DayMs);
            return ForwardArc(phase, target);
        }

        public static int ForwardArc(int fromMs, int toMs)
        {
            if (fromMs < 0 || fromMs >= TimeConfig.DayMs) throw new ArgumentOutOfRangeException(nameof(fromMs));
            if (toMs < 0 || toMs >= TimeConfig.DayMs) throw new ArgumentOutOfRangeException(nameof(toMs));
            return (int)Mod((long)toMs - fromMs, TimeConfig.DayMs);
        }

        static long Mod(long a, long m) { long r = a % m; return r < 0 ? r + m : r; }
    }
}
