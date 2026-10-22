using System;

namespace Game.Core.DayCycle
{
    /// <summary>Điều kiện môi trường sống loại "buổi": thoả khi đang ở một trong các buổi liệt kê (0024). Chưa nối vào bộ môi trường sống (chưa có code).</summary>
    [Serializable]
    public class PeriodCondition
    {
        public string[] periods;

        public PeriodCondition() { }
        public PeriodCondition(params string[] periods) { this.periods = periods; }

        public static DayPeriod ParsePeriod(string s)
        {
            if (!string.IsNullOrEmpty(s) && char.IsLetter(s[0])
                && Enum.TryParse(s, false, out DayPeriod p) && Enum.IsDefined(typeof(DayPeriod), p))
                return p;
            throw new ArgumentException("Invalid period: " + s);
        }

        public void Validate()
        {
            if (periods == null || periods.Length == 0) throw new ArgumentException("periods must not be empty.");
            var seen = new bool[4];
            foreach (var s in periods)
            {
                var p = ParsePeriod(s);
                if (seen[(int)p]) throw new ArgumentException("periods has duplicate period: " + p);
                seen[(int)p] = true;
            }
        }

        public bool IsMet(DayPeriod current)
        {
            if (periods == null) return false;
            foreach (var s in periods)
                if (ParsePeriod(s) == current) return true;
            return false;
        }
    }
}
