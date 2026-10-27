using System;

namespace Game.Core
{
    /// <summary>
    /// Cấu hình nước chảy M1 (design/decisions/0043). Nguồn chuẩn: design/data/water_m1.json (đọc bằng JsonUtility — tên trường phải khớp JSON).
    /// Số là số tạm, chưa chốt.
    /// </summary>
    [Serializable]
    public class WaterConfig
    {
        public int configVersion = 2;
        public int maxLevel = 5;
        public int stepsPerSecond = 8;
        public int wallBoostMinSolid = 2;

        public void Validate()
        {
            if (maxLevel < 1 || maxLevel > 7)
                throw new ArgumentException("maxLevel must be in 1..7.");
            if (stepsPerSecond < 1 || stepsPerSecond > 60)
                throw new ArgumentException("stepsPerSecond must be in 1..60.");
            if (wallBoostMinSolid < 1 || wallBoostMinSolid > 4)
                throw new ArgumentException("wallBoostMinSolid must be in 1..4.");
        }
    }
}
