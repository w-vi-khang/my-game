using Game.Core;
using Game.Core.DayCycle;
using Game.Core.Habitat;
using Game.Core.Sim;
using UnityEngine;

namespace Game.Client
{
    /// <summary>
    /// Đọc cấu hình JSON (world_m1, world_maps_m1, sim_m1, time_m1, water_m1). Nguồn chuẩn ở design/data/ (dùng chung với backend, skill game-balance);
    /// client giữ bản sao trong Resources/Config — test Game.World.Tests.ConfigTests bảo đảm các bản giống nhau.
    /// </summary>
    public static class ConfigLoader
    {
        public const string WorldM1Path = "Config/world_m1";
        public const string WorldMapsM1Path = "Config/world_maps_m1";
        public const string SimM1Path = "Config/sim_m1";
        public const string TimeM1Path = "Config/time_m1";
        public const string WaterM1Path = "Config/water_m1";
        public const string HabitatM1Path = "Config/habitat_m1";

        public static TerrainParams LoadTerrainParams()
        {
            var asset = Resources.Load<TextAsset>(WorldM1Path);
            if (asset == null)
                throw new System.IO.FileNotFoundException($"Resources/ not found: Resources/{WorldM1Path}.json");
            var p = JsonUtility.FromJson<TerrainParams>(asset.text);
            p.Validate();
            return p;
        }

        public static WorldMapParams LoadWorldMapParams()
        {
            var asset = Resources.Load<TextAsset>(WorldMapsM1Path);
            if (asset == null)
                throw new System.IO.FileNotFoundException($"Resources/ not found: Resources/{WorldMapsM1Path}.json");
            var p = JsonUtility.FromJson<WorldMapParams>(asset.text);
            p.Validate();
            return p;
        }

        public static SimConfig LoadSimConfig()
        {
            var asset = Resources.Load<TextAsset>(SimM1Path);
            if (asset == null)
                throw new System.IO.FileNotFoundException($"Resources/ not found: Resources/{SimM1Path}.json");
            var c = JsonUtility.FromJson<SimConfig>(asset.text);
            c.Validate();
            PlayerRules.Configure(c.player);
            PlayerMotion.Configure(c.player);
            return c;
        }

        public static TimeConfig LoadTimeConfig()
        {
            var asset = Resources.Load<TextAsset>(TimeM1Path);
            if (asset == null)
                throw new System.IO.FileNotFoundException($"Resources/ not found: Resources/{TimeM1Path}.json");
            var c = JsonUtility.FromJson<TimeConfig>(asset.text);
            c.Validate();
            return c;
        }

        public static WaterConfig LoadWaterConfig()
        {
            var asset = Resources.Load<TextAsset>(WaterM1Path);
            if (asset == null)
                throw new System.IO.FileNotFoundException($"Resources/ not found: Resources/{WaterM1Path}.json");
            var c = JsonUtility.FromJson<WaterConfig>(asset.text);
            c.Validate();
            return c;
        }

        public static HabitatConfig LoadHabitatConfig()
        {
            var asset = Resources.Load<TextAsset>(HabitatM1Path);
            if (asset == null)
                throw new System.IO.FileNotFoundException($"Resources/ not found: Resources/{HabitatM1Path}.json");
            var c = JsonUtility.FromJson<HabitatConfig>(asset.text);
            c.Validate();
            return c;
        }
    }
}
