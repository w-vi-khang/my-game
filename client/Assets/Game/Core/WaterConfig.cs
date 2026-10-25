using System;

namespace Game.Core
{
    /// <summary>
    /// Cấu hình nước chảy M1 (design/decisions/0040). Nguồn chuẩn: design/data/water_m1.json (đọc bằng JsonUtility — tên trường phải khớp JSON).
    /// Số là số tạm, chưa chốt.
    /// </summary>
    [Serializable]
    public class WaterConfig
    {
        public int configVersion = 1;
        public int flowMaxDistance = 7;
        public int stepsPerSecond = 8;

        public void Validate()
        {
            if (flowMaxDistance < 1 || flowMaxDistance > 7)
                throw new ArgumentException("flowMaxDistance must be in 1..7.");
            if (stepsPerSecond < 1 || stepsPerSecond > 60)
                throw new ArgumentException("stepsPerSecond must be in 1..60.");
        }
    }
}
