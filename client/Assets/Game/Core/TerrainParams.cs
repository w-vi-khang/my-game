using System;

namespace Game.Core
{
    /// <summary>
    /// Thông số sinh địa hình M1. Nguồn chuẩn: design/data/world_m1.json (client đọc bản sao trong Resources bằng JsonUtility —
    /// tên trường phải khớp JSON). Số là số tạm, chỉnh khi chơi thử.
    /// </summary>
    [Serializable]
    public class TerrainParams
    {
        /// <summary>Tăng khi đổi thuật toán sinh làm world khác đi (world-tech "Lưu trữ"). 1 = M1 bước 1; 2 = đảo + vành biển (0009).</summary>
        public int generatorVersion = 2;
        public uint seed = 20261013;

        public int sizeX = 64;
        public int sizeY = 32;
        public int sizeZ = 64;

        public float baseHeight = 14f;
        public float amplitude1 = 5f;
        public float frequency1 = 0.035f;
        public float amplitude2 = 1.5f;
        public float frequency2 = 0.11f;
        public int minSurface = 8;
        public int maxSurface = 20;

        public int bedrockLayers = 2;
        public int dirtDepth = 3;
        public int waterLevel = 10;
        /// <summary>Mặt đất cao tới waterLevel + sandBand thì lớp trên cùng là cát.</summary>
        public int sandBand = 1;

        public float lakeRadius = 9f;
        public float lakeDepth = 7f;

        // ---- Đảo + vành biển (generatorVersion ≥ 2, 0009) ----
        /// <summary>Số ô từ biên map toàn là đáy biển.</summary>
        public float islandMargin = 3f;
        /// <summary>Bề rộng sườn (ô) từ đáy biển lên địa hình noise.</summary>
        public float islandFalloff = 10f;
        /// <summary>Đáy biển ở rìa = waterLevel − seaFloorDepth.</summary>
        public int seaFloorDepth = 3;

        public WorldSize Size => new WorldSize(sizeX, sizeY, sizeZ);

        public void Validate()
        {
            var _ = Size;
            if (bedrockLayers < 1 || bedrockLayers >= minSurface)
                throw new ArgumentException("bedrockLayers phải ≥ 1 và < minSurface");
            if (minSurface > maxSurface || maxSurface >= sizeY)
                throw new ArgumentException("Cần minSurface ≤ maxSurface < sizeY");
            if (waterLevel < minSurface || waterLevel >= maxSurface)
                throw new ArgumentException("waterLevel phải nằm trong [minSurface, maxSurface)");
            if (dirtDepth < 0 || sandBand < 0 || lakeRadius <= 0f || lakeDepth < 0f)
                throw new ArgumentException("dirtDepth, sandBand, lakeDepth ≥ 0; lakeRadius > 0");
            if (generatorVersion < 1 || generatorVersion > 2)
                throw new ArgumentException("generatorVersion chỉ hỗ trợ 1 hoặc 2");
            if (generatorVersion >= 2 && (islandMargin < 0f || islandFalloff <= 0f || seaFloorDepth < 1 || waterLevel - seaFloorDepth < bedrockLayers))
                throw new ArgumentException("Đảo: islandMargin ≥ 0, islandFalloff > 0, seaFloorDepth ≥ 1, đáy biển không thấp hơn lớp đá đáy");
        }
    }
}
