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
        public int configVersion = 8;
        public int maxLevel = 5;
        public int stepsPerSecond = 8;
        public int wallBoostMinSolid = 2;
        /// <summary>Ô chảy thấp hơn nguồn gốc của cây, cách ≤ basinFillRadius ô ngang (Chebyshev), lên mức tối đa (design/decisions/0044).</summary>
        public int basinFillRadius = 16;
        /// <summary>Bán kính bo cạnh đứng / mép tràn của mặt nước (ô) và số đoạn cung (brief 0080).</summary>
        public float filletRadius = 0.3f;
        public int filletSegments = 3;
        /// <summary>Hình ảnh mặt nước: màu nông→sâu, viền bờ mờ, gợn, lấp lánh (brief 0095, 0097), bọt bờ (0096) — số tạm, chưa chốt.</summary>
        public WaterLookConfig look = new WaterLookConfig();

        public void Validate()
        {
            if (maxLevel < 1 || maxLevel > 7)
                throw new ArgumentException("maxLevel must be in 1..7.");
            if (stepsPerSecond < 1 || stepsPerSecond > 60)
                throw new ArgumentException("stepsPerSecond must be in 1..60.");
            if (wallBoostMinSolid < 1 || wallBoostMinSolid > 4)
                throw new ArgumentException("wallBoostMinSolid must be in 1..4.");
            if (basinFillRadius < 0 || basinFillRadius > 64)
                throw new ArgumentException("basinFillRadius must be in 0..64.");
            if (!(filletRadius >= 0.05f && filletRadius <= 0.45f))
                throw new ArgumentException("filletRadius must be in 0.05..0.45.");
            if (filletSegments < 1 || filletSegments > 8)
                throw new ArgumentException("filletSegments must be in 1..8.");
            if (look == null)
                throw new ArgumentException("look is missing.");
            look.Validate();
        }
    }

    /// <summary>Tham số hình ảnh mặt nước (brief 0095), áp vào material qua WaterLook.Apply. Màu viết như trong Inspector.</summary>
    [Serializable]
    public class WaterLookConfig
    {
        public float[] shallowColor = { 0.45f, 0.88f, 0.85f };
        public float[] deepColor = { 0.12f, 0.50f, 0.70f };
        public float depthFull = 2.0f;
        public float deepAlpha = 0.6f;
        public float shallowAlphaFactor = 0.25f;
        public float shoreSoftWidth = 0.35f;
        public float rippleScale = 0.45f;
        public float rippleStrength = 0.25f;
        public float glint = 1.0f;
        public float[] foamColor = { 1f, 1f, 1f };
        public float foamWidth = 2.0f;
        public float foamBands = 3.0f;
        public float foamLine = 0.3f;
        public float foamSoft = 0.15f;
        public float foamSpeed = 0.25f;
        public float foamSwing = 0.3f;
        public float foamSwingSpeed = 0.8f;
        public float foamNoiseScale = 1.2f;
        public float foamOpenMin = 0.2f;
        public float foamStrength = 0.9f;
        public float glintPower = 160f;
        public float glintHalo = 0.8f;
        public float glintHaloPower = 300f;
        public float sparkleStrength = 1.5f;
        public float sparkleScale = 1.5f;
        public float sparkleDensity = 0.35f;
        public float sparkleSize = 0.15f;
        public float sparkleSpeed = 3f;
        public float sparkleFadeStart = 20f;
        public float sparkleFadeEnd = 60f;
        public float[] flowStreakColor = { 1f, 1f, 1f };
        public float flowStreakStrength = 0.35f;
        public float flowStreakScale = 1.5f;
        public float flowStreakLength = 5f;
        public float flowStreakSpeed = 1.2f;
        public float flowStreakCycle = 2f;
        public float flowStreakCut = 0.55f;
        public float flowStreakSoft = 0.15f;
        public float flowFallSpeed = 3f;

        public void Validate()
        {
            CheckColor(shallowColor, "shallowColor");
            CheckColor(deepColor, "deepColor");
            CheckRange(depthFull, 0.25f, 4f, "depthFull");
            CheckRange(deepAlpha, 0f, 1f, "deepAlpha");
            CheckRange(shallowAlphaFactor, 0f, 1f, "shallowAlphaFactor");
            CheckRange(shoreSoftWidth, 0.05f, 2f, "shoreSoftWidth");
            CheckRange(rippleScale, 0.05f, 4f, "rippleScale");
            CheckRange(rippleStrength, 0f, 1f, "rippleStrength");
            CheckRange(glint, 0f, 2f, "glint");
            CheckColor(foamColor, "foamColor");
            CheckRange(foamWidth, 0.5f, 4f, "foamWidth");
            CheckRange(foamBands, 1f, 8f, "foamBands");
            CheckRange(foamLine, 0.05f, 0.9f, "foamLine");
            CheckRange(foamSoft, 0.01f, 0.5f, "foamSoft");
            CheckRange(foamSpeed, 0f, 2f, "foamSpeed");
            CheckRange(foamSwing, 0f, 1f, "foamSwing");
            CheckRange(foamSwingSpeed, 0f, 4f, "foamSwingSpeed");
            CheckRange(foamNoiseScale, 0.1f, 4f, "foamNoiseScale");
            CheckRange(foamOpenMin, 0f, 0.9f, "foamOpenMin");
            CheckRange(foamStrength, 0f, 1f, "foamStrength");
            CheckRange(glintPower, 8f, 1024f, "glintPower");
            CheckRange(glintHalo, 0f, 2f, "glintHalo");
            CheckRange(glintHaloPower, 8f, 2048f, "glintHaloPower");
            CheckRange(sparkleStrength, 0f, 4f, "sparkleStrength");
            CheckRange(sparkleScale, 0.1f, 8f, "sparkleScale");
            CheckRange(sparkleDensity, 0f, 1f, "sparkleDensity");
            CheckRange(sparkleSize, 0.02f, 0.45f, "sparkleSize");
            CheckRange(sparkleSpeed, 0f, 20f, "sparkleSpeed");
            CheckRange(sparkleFadeStart, 0f, 200f, "sparkleFadeStart");
            CheckRange(sparkleFadeEnd, 0f, 300f, "sparkleFadeEnd");
            if (!(sparkleFadeEnd > sparkleFadeStart))
                throw new ArgumentException("sparkleFadeEnd must be > sparkleFadeStart.");
            CheckColor(flowStreakColor, "flowStreakColor");
            CheckRange(flowStreakStrength, 0f, 1f, "flowStreakStrength");
            CheckRange(flowStreakScale, 0.1f, 8f, "flowStreakScale");
            CheckRange(flowStreakLength, 1f, 20f, "flowStreakLength");
            CheckRange(flowStreakSpeed, 0f, 6f, "flowStreakSpeed");
            CheckRange(flowStreakCycle, 0.25f, 8f, "flowStreakCycle");
            CheckRange(flowStreakCut, 0f, 0.95f, "flowStreakCut");
            CheckRange(flowStreakSoft, 0.01f, 0.5f, "flowStreakSoft");
            CheckRange(flowFallSpeed, 0f, 12f, "flowFallSpeed");
            if (flowStreakCut + flowStreakSoft > 1f)
                throw new ArgumentException("flowStreakCut + flowStreakSoft must be <= 1.");
            if (foamLine + foamSoft > 1f)
                throw new ArgumentException("foamLine + foamSoft must be <= 1.");
        }

        static void CheckColor(float[] c, string name)
        {
            if (c == null || c.Length != 3)
                throw new ArgumentException($"{name} must have 3 elements.");
            for (int i = 0; i < 3; i++)
                if (!(c[i] >= 0f && c[i] <= 1f))
                    throw new ArgumentException($"{name}[{i}] must be in 0..1.");
        }

        static void CheckRange(float v, float min, float max, string name)
        {
            if (!(v >= min && v <= max))
                throw new ArgumentException($"{name} must be in {min}..{max}.");
        }
    }
}
