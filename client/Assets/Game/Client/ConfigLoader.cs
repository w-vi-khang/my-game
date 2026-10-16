using Game.Core;
using Game.Core.Sim;
using UnityEngine;

namespace Game.Client
{
    /// <summary>
    /// Đọc cấu hình JSON (world_m1, sim_m1). Nguồn chuẩn ở design/data/ (dùng chung với backend, skill game-balance);
    /// client giữ bản sao trong Resources/Config — test Game.World.Tests.ConfigTests bảo đảm các bản giống nhau.
    /// </summary>
    public static class ConfigLoader
    {
        public const string WorldM1Path = "Config/world_m1";
        public const string SimM1Path = "Config/sim_m1";

        public static TerrainParams LoadTerrainParams()
        {
            var asset = Resources.Load<TextAsset>(WorldM1Path);
            if (asset == null)
                throw new System.IO.FileNotFoundException($"Không thấy Resources/{WorldM1Path}.json");
            var p = JsonUtility.FromJson<TerrainParams>(asset.text);
            p.Validate();
            return p;
        }

        public static SimConfig LoadSimConfig()
        {
            var asset = Resources.Load<TextAsset>(SimM1Path);
            if (asset == null)
                throw new System.IO.FileNotFoundException($"Không thấy Resources/{SimM1Path}.json");
            var c = JsonUtility.FromJson<SimConfig>(asset.text);
            c.Validate();
            return c;
        }
    }
}
