using Game.Core;
using UnityEngine;

namespace Game.Client
{
    /// <summary>
    /// Đọc cấu hình JSON. Nguồn chuẩn ở design/data/ (dùng chung với backend, skill game-balance);
    /// client giữ bản sao trong Resources/Config — test Game.World.Tests.ConfigTests bảo đảm hai bản giống nhau.
    /// </summary>
    public static class ConfigLoader
    {
        public const string WorldM1Path = "Config/world_m1";

        public static TerrainParams LoadTerrainParams()
        {
            var asset = Resources.Load<TextAsset>(WorldM1Path);
            if (asset == null)
                throw new System.IO.FileNotFoundException($"Không thấy Resources/{WorldM1Path}.json");
            var p = JsonUtility.FromJson<TerrainParams>(asset.text);
            p.Validate();
            return p;
        }
    }
}
