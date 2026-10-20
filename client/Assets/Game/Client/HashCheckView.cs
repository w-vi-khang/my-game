using System.Diagnostics;
using Game.Core;
using Game.World;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Game.Client
{
    /// <summary>
    /// Scene kiểm tất định (world-tech bước 1): sinh world M1 từ cấu hình, hiện mã băm lên màn hình và ghi log
    /// "HASHCHECK ..." (đọc bằng adb logcat). Phải trùng Game.World.Tests.GoldenHashTests.ExpectedV2 (generatorVersion trong cấu hình).
    /// Kèm world nhiều map v3 (world_maps_m1) — phải trùng GoldenHashTests.ExpectedV3World / ExpectedV3Maps.
    /// </summary>
    public class HashCheckView : MonoBehaviour
    {
        string text = "Generating world...";

        void Start()
        {
            var p = ConfigLoader.LoadTerrainParams();
            var sw = Stopwatch.StartNew();
            var world = TerrainGenerator.Generate(p, out bool ranWithBurst);
            sw.Stop();
            string hash = WorldHash.ToHex(WorldHash.Compute(world));
            text = $"world_m1 v{p.generatorVersion} seed {p.seed}\nhash {hash}\nburst {ranWithBurst} · {sw.ElapsedMilliseconds} ms\n{SystemInfo.processorType} · {SystemInfo.operatingSystem}";
            Debug.Log($"HASHCHECK hash={hash} burst={ranWithBurst} ms={sw.ElapsedMilliseconds} v={p.generatorVersion} seed={p.seed}");

            var p3 = ConfigLoader.LoadWorldMapParams();
            var sw3 = Stopwatch.StartNew();
            var world3 = WorldMapGenerator.Generate(p3, out var maps, out bool burst3);
            sw3.Stop();
            long ms3 = sw3.ElapsedMilliseconds;
            string worldHash3 = WorldHash.ToHex(WorldHash.Compute(world3));
            string mapsHash3 = WorldHash.ToHex(WorldHash.Compute(maps));
            text += $"\n\nworld_maps_m1 v{p3.generatorVersion} seed {p3.seed}\nworld {worldHash3}\nmaps {mapsHash3}\nburst {burst3} · {ms3} ms";
            Debug.Log($"HASHCHECK3 world={worldHash3} maps={mapsHash3} burst={burst3} ms={ms3} v={p3.generatorVersion} seed={p3.seed}");
        }

        void OnGUI()
        {
            var style = new GUIStyle(GUI.skin.label) { fontSize = Mathf.Max(24, Screen.height / 30), wordWrap = true };
            style.normal.textColor = Color.white;
            GUI.Label(new Rect(40, 80, Screen.width - 80, Screen.height - 160), text, style);
        }
    }
}
