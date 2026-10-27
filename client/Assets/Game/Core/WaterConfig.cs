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
        public int configVersion = 3;
        public int maxLevel = 5;
        public int stepsPerSecond = 8;
        public int wallBoostMinSolid = 2;
        /// <summary>Bán kính bo cạnh đứng / mép tràn của mặt nước (ô) và số đoạn cung (brief 0080).</summary>
        public float filletRadius = 0.3f;
        public int filletSegments = 3;

        public void Validate()
        {
            if (maxLevel < 1 || maxLevel > 7)
                throw new ArgumentException("maxLevel must be in 1..7.");
            if (stepsPerSecond < 1 || stepsPerSecond > 60)
                throw new ArgumentException("stepsPerSecond must be in 1..60.");
            if (wallBoostMinSolid < 1 || wallBoostMinSolid > 4)
                throw new ArgumentException("wallBoostMinSolid must be in 1..4.");
            if (!(filletRadius >= 0.05f && filletRadius <= 0.45f))
                throw new ArgumentException("filletRadius must be in 0.05..0.45.");
            if (filletSegments < 1 || filletSegments > 8)
                throw new ArgumentException("filletSegments must be in 1..8.");
        }
    }
}
