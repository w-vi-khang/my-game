using System.Diagnostics;
using Game.Core;
using Game.World;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Game.Client
{
    /// <summary>
    /// Scene kiểm tất định (world-tech bước 1): sinh world M1 từ cấu hình, hiện mã băm lên màn hình và ghi log
    /// "HASHCHECK ..." (đọc bằng adb logcat). Phải trùng Game.World.Tests.GoldenHashTests.ExpectedV1.
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
        }

        void OnGUI()
        {
            var style = new GUIStyle(GUI.skin.label) { fontSize = Mathf.Max(24, Screen.height / 30), wordWrap = true };
            style.normal.textColor = Color.white;
            GUI.Label(new Rect(40, 80, Screen.width - 80, Screen.height - 160), text, style);
        }
    }
}
