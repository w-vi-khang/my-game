using System;

namespace Game.Core
{
    /// <summary>
    /// Thông số bộ sinh world nhiều map (generatorVersion 3, world-tech bước 7a, decision 0022). Nguồn chuẩn: design/data/world_maps_m1.json
    /// (client đọc bản sao trong Resources bằng JsonUtility — tên trường phải khớp JSON). Số là số tạm, chỉnh khi chơi thử.
    /// </summary>
    [Serializable]
    public class WorldMapParams
    {
        public int generatorVersion = 3;
        public uint seed = 20261013;

        public int sizeX = 384;
        public int sizeY = 48;
        public int sizeZ = 384;

        public float baseHeight = 22f;
        public float amplitude1 = 10f;
        public float frequency1 = 0.02f;
        public float amplitude2 = 2.5f;
        public float frequency2 = 0.08f;
        public int minSurface = 10;
        public int maxSurface = 40;

        public int bedrockLayers = 2;
        public int dirtDepth = 3;
        public int waterLevel = 12;
        public int sandBand = 1;

        public float lakeRadius = 12f;
        public float lakeDepth = 7f;

        /// <summary>Số ô từ biên world chắc chắn là đáy biển.</summary>
        public int seaRing = 24;
        /// <summary>Đáy biển = waterLevel − seaFloorDepth.</summary>
        public int seaFloorDepth = 4;
        /// <summary>Bán kính khối đất liền quanh tâm world (ô), trước khi méo.</summary>
        public float landRadius = 132f;
        /// <summary>Bề rộng sườn (ô) từ đáy biển lên địa hình noise.</summary>
        public float coastFalloff = 12f;
        public float coastWarpAmplitude = 14f;
        public float coastWarpFrequency = 0.015f;

        public int mapCount = 3;
        /// <summary>Khoảng cách từ tâm world tới tâm mỗi map (ô).</summary>
        public float mapCenterRadius = 64f;
        public float borderWarpAmplitude = 18f;
        public float borderWarpFrequency = 0.01f;
        /// <summary>Tên tạm (0022); tên thật user đặt sau qua design/01-story/naming.md.</summary>
        public string[] mapNames = { "Map 1", "Map 2", "Map 3" };

        /// <summary>Công trình đặc trưng của từng map (0023): loại, tên tạm, số lượng — tên thật user đặt sau.</summary>
        public LandmarkType[] landmarkTypes =
        {
            new LandmarkType(0, "Công trình 1A", 1),
            new LandmarkType(0, "Công trình 1B", 1),
            new LandmarkType(1, "Công trình 2A", 1),
            new LandmarkType(1, "Công trình 2B", 1),
            new LandmarkType(2, "Công trình 3A", 1),
            new LandmarkType(2, "Công trình 3B", 1),
        };
        /// <summary>Dấu mốc cách cột biên (biển, map khác, biên world) ít nhất bấy nhiêu bước 4 hướng trong cùng map.</summary>
        public int landmarkBorderMargin = 6;
        /// <summary>Hai dấu mốc cách nhau ít nhất bấy nhiêu ô (đường chim bay).</summary>
        public int landmarkSpacing = 20;

        public WorldSize Size => new WorldSize(sizeX, sizeY, sizeZ);

        public void Validate()
        {
            if (generatorVersion != 3)
                throw new ArgumentException("generatorVersion of a multi-map world must be 3");
            var _ = Size;
            if (bedrockLayers < 1 || bedrockLayers >= minSurface)
                throw new ArgumentException("bedrockLayers must be ≥ 1 and < minSurface");
            if (minSurface > maxSurface || maxSurface >= sizeY)
                throw new ArgumentException("Requires minSurface ≤ maxSurface < sizeY");
            if (waterLevel < minSurface || waterLevel >= maxSurface)
                throw new ArgumentException("waterLevel must be within [minSurface, maxSurface)");
            if (dirtDepth < 0 || sandBand < 0 || lakeRadius <= 0f || lakeDepth < 0f)
                throw new ArgumentException("dirtDepth, sandBand, lakeDepth ≥ 0; lakeRadius > 0");
            if (seaRing < 1 || seaFloorDepth < 1 || waterLevel - seaFloorDepth < bedrockLayers)
                throw new ArgumentException("seaRing ≥ 1, seaFloorDepth ≥ 1, sea floor must not be lower than the bedrock layer");
            if (landRadius <= 0f || coastFalloff <= 0f || coastWarpAmplitude < 0f || coastWarpFrequency <= 0f
                || borderWarpAmplitude < 0f || borderWarpFrequency <= 0f)
                throw new ArgumentException("landRadius, coastFalloff, coastWarpFrequency, borderWarpFrequency > 0; coastWarpAmplitude, borderWarpAmplitude ≥ 0");
            if (mapCount < 2 || mapCount > 8 || mapNames == null || mapNames.Length != mapCount)
                throw new ArgumentException("mapCount in [2, 8] and mapNames must have exactly mapCount names");
            foreach (var n in mapNames)
                if (string.IsNullOrEmpty(n))
                    throw new ArgumentException("Map name must not be empty");
            if (mapCenterRadius <= 0f || mapCenterRadius + lakeRadius >= landRadius - coastWarpAmplitude - coastFalloff)
                throw new ArgumentException("Lake must lie entirely within the land: mapCenterRadius + lakeRadius < landRadius − coastWarpAmplitude − coastFalloff");
            if (landmarkTypes == null || landmarkBorderMargin < 2 || landmarkSpacing < 1)
                throw new ArgumentException("landmarkTypes non-null, landmarkBorderMargin ≥ 2, landmarkSpacing ≥ 1");
            foreach (var t in landmarkTypes)
                if (t == null || t.map < 0 || t.map >= mapCount || string.IsNullOrEmpty(t.name) || t.count < 1 || t.count > 8)
                    throw new ArgumentException("Each landmarkTypes: map in [0, mapCount), name non-empty, count in [1, 8]");
        }
    }

    /// <summary>Một loại công trình đặc trưng của map (0023), đọc từ JSON.</summary>
    [Serializable]
    public class LandmarkType
    {
        public int map;
        public string name;
        public int count = 1;

        public LandmarkType() { }

        public LandmarkType(int map, string name, int count)
        {
            this.map = map;
            this.name = name;
            this.count = count;
        }
    }
}
