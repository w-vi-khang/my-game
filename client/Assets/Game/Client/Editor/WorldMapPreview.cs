using System.Diagnostics;
using System.IO;
using Game.Core;
using Game.World;
using UnityEditor;
using UnityEngine;

namespace Game.Client.Editor
{
    /// <summary>
    /// Xuất ảnh PNG xem trước world nhiều map (brief 0009, bước 7a): 1 điểm ảnh = 1 cột. Chỉ là công cụ xem hình dạng;
    /// minimap thật làm ở bước 7c.
    /// </summary>
    public static class WorldMapPreview
    {
        const string OutputPath = "Builds/world_maps_preview.png";

        static readonly Color32[] LandTiers =
        {
            new Color32(200, 190, 120, 255), new Color32(200, 190, 120, 255), new Color32(200, 190, 120, 255),
            new Color32(120, 180, 90, 255),
            new Color32(100, 165, 80, 255),
            new Color32(85, 150, 70, 255),
            new Color32(110, 140, 70, 255),
            new Color32(140, 130, 80, 255),
            new Color32(150, 120, 90, 255),
            new Color32(160, 140, 120, 255),
            new Color32(200, 200, 200, 255),
        };

        [MenuItem("Game/Ảnh xem trước world nhiều map")]
        public static void Export()
        {
            var p = ConfigLoader.LoadWorldMapParams();
            var sw = Stopwatch.StartNew();
            var world = WorldMapGenerator.Generate(p, out var maps, out bool burst);
            sw.Stop();

            var tex = new Texture2D(p.sizeX, p.sizeZ, TextureFormat.RGBA32, false);
            var px = new Color32[p.sizeX * p.sizeZ];
            var land = new int[maps.MapCount];
            for (int z = 0; z < p.sizeZ; z++)
            for (int x = 0; x < p.sizeX; x++)
            {
                byte id = maps.Get(x, z);
                int s = world.SurfaceHeight(x, z);
                if (id != MapTable.Ocean && s >= p.waterLevel)
                    land[id]++;

                Color32 color;
                if (id != MapTable.Ocean && ((x + 1 < p.sizeX && IsOtherMap(maps, x + 1, z, id)) || (z + 1 < p.sizeZ && IsOtherMap(maps, x, z + 1, id))))
                    color = new Color32(30, 30, 30, 255);
                else if (s < p.waterLevel)
                {
                    int depth = p.waterLevel - s;
                    float t = Mathf.Min(depth - 1, 7) / 7f;
                    color = Color32.Lerp(new Color32(90, 170, 220, 255), new Color32(20, 60, 140, 255), t);
                }
                else
                {
                    int tier = s / 4;
                    color = tier >= LandTiers.Length ? new Color32(240, 240, 240, 255) : LandTiers[tier];
                }
                px[x + p.sizeX * z] = color;
            }
            tex.SetPixels32(px);
            tex.Apply();

            Directory.CreateDirectory("Builds");
            File.WriteAllBytes(OutputPath, ImageConversion.EncodeToPNG(tex));
            Object.DestroyImmediate(tex);

            string counts = "";
            for (int i = 0; i < land.Length; i++)
                counts += $" {maps.Name(i)}={land[i]}";
            UnityEngine.Debug.Log($"WORLDPREVIEW seed={p.seed} ms={sw.ElapsedMilliseconds} burst={burst} world={WorldHash.ToHex(WorldHash.Compute(world))} maps={WorldHash.ToHex(WorldHash.Compute(maps))} land:{counts} file={OutputPath}");
        }

        static bool IsOtherMap(MapTable maps, int x, int z, byte id)
        {
            byte n = maps.Get(x, z);
            return n != MapTable.Ocean && n != id;
        }
    }
}
