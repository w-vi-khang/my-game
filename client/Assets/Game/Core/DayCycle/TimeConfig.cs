using System;

namespace Game.Core.DayCycle
{
    /// <summary>4 mốc chọn thời điểm (0024).</summary>
    public enum TimeLandmark : byte { Midnight = 0, Dawn = 1, Noon = 2, Dusk = 3 }

    /// <summary>4 buổi dùng cho điều kiện môi trường sống (0024).</summary>
    public enum DayPeriod : byte { Dawn = 0, Day = 1, Dusk = 2, Night = 3 }

    /// <summary>
    /// Cấu hình buổi trong ngày M1. Nguồn chuẩn: design/data/time_m1.json (đọc bằng JsonUtility — tên trường phải khớp JSON).
    /// Pha ghi theo phần nghìn ngày (‰); số là số tạm (0024).
    /// </summary>
    [Serializable]
    public class TimeConfig
    {
        public int configVersion = 1;
        public int midnightPermille = 0;
        public int dawnPermille = 250;
        public int noonPermille = 500;
        public int duskPermille = 750;
        public int dawnStartPermille = 200;
        public int dayStartPermille = 300;
        public int duskStartPermille = 700;
        public int nightStartPermille = 800;
        public int rotateDurationMs = 3000;

        public const int DayMs = 86_400_000;
        public const int MsPerPermille = 86_400;

        public int LandmarkMs(TimeLandmark m)
        {
            switch (m)
            {
                case TimeLandmark.Midnight: return midnightPermille * MsPerPermille;
                case TimeLandmark.Dawn: return dawnPermille * MsPerPermille;
                case TimeLandmark.Noon: return noonPermille * MsPerPermille;
                case TimeLandmark.Dusk: return duskPermille * MsPerPermille;
                default: throw new ArgumentException("Invalid landmark time: " + m);
            }
        }

        public TimeLandmark WindowAt(int phaseMs)
        {
            if (phaseMs < 0 || phaseMs >= DayMs) throw new ArgumentOutOfRangeException(nameof(phaseMs));
            if (phaseMs < midnightPermille * MsPerPermille) return TimeLandmark.Dusk;
            if (phaseMs < dawnPermille * MsPerPermille) return TimeLandmark.Midnight;
            if (phaseMs < noonPermille * MsPerPermille) return TimeLandmark.Dawn;
            if (phaseMs < duskPermille * MsPerPermille) return TimeLandmark.Noon;
            return TimeLandmark.Dusk;
        }

        public DayPeriod PeriodAt(int phaseMs)
        {
            if (phaseMs < 0 || phaseMs >= DayMs) throw new ArgumentOutOfRangeException(nameof(phaseMs));
            if (phaseMs < dawnStartPermille * MsPerPermille) return DayPeriod.Night;
            if (phaseMs < dayStartPermille * MsPerPermille) return DayPeriod.Dawn;
            if (phaseMs < duskStartPermille * MsPerPermille) return DayPeriod.Day;
            if (phaseMs < nightStartPermille * MsPerPermille) return DayPeriod.Dusk;
            return DayPeriod.Night;
        }

        public void Validate()
        {
            if (!(0 <= midnightPermille && midnightPermille < dawnPermille && dawnPermille < noonPermille
                  && noonPermille < duskPermille && duskPermille < 1000))
                throw new ArgumentException("Landmark times must be increasing within [0, 1000).");
            if (!(0 <= dawnStartPermille && dawnStartPermille < dayStartPermille && dayStartPermille < duskStartPermille
                  && duskStartPermille < nightStartPermille && nightStartPermille < 1000))
                throw new ArgumentException("Period boundaries must be increasing within [0, 1000).");
            if (rotateDurationMs <= 0) throw new ArgumentException("rotateDurationMs must be > 0.");
        }
    }
}
