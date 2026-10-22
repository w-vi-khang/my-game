using System.Collections.Generic;
using System.Text;
using Unity.Profiling;
using Game.Core;
using UnityEngine;

namespace Game.Client
{
    /// <summary>
    /// Đo FPS khi nhân vật đi liên tục (world 7h b, brief 0039). Chỉ gắn trong scene WalkBench (bản sao SimView, menu "Game/Build APK WalkBench").
    /// Nhân vật tự đi vòng tròn quanh trại (bán kính chọn để cả vòng trên đất khô), tự nhảy khi trước mặt có khối, kẹt thì rẽ phải 1,5 giây.
    /// Khởi động warmupSeconds rồi đo measureSeconds; ghi một dòng WALKBENCH (đọc bằng adb logcat).
    /// </summary>
    [RequireComponent(typeof(TerrainChunkView), typeof(SimView))]
    public class WalkBench : MonoBehaviour
    {
        public float warmupSeconds = 3f;
        public float measureSeconds = 40f;
        public float leadDegrees = 25f;
        public float stuckCheckSeconds = 2f, stuckMinDistance = 1f, detourSeconds = 1.5f;
        public static readonly int[] RadiusCandidates = { 36, 32, 40, 28, 44 };
        public const int RouteSamples = 72, HoleRadius = 120;

        public bool Done { get; private set; }
        public string Summary { get; private set; }
        public float Distance { get; private set; }
        public int StuckCount { get; private set; }
        public int RouteRadius { get; private set; }
        public bool RouteDry { get; private set; }
        public Vector2 RouteCenter { get; private set; }

        TerrainChunkView view;
        SimView sv;
        PlayerCharacter player;
        FollowCamera follow;
        ProfilerRecorder systemMem, gcMem;
        readonly List<double> frameMs = new List<double>();
        bool started;
        float startTime, nextStuckCheck, detourUntil;
        Vector2 lastCheckPos, lastPos;
        int holeFrames, maxMissing, maxLoadedChunks, waterFrames;
        long maxSystemMem, maxGcMem;
        string result = "Đang chuẩn bị…";

        void Awake()
        {
            view = GetComponent<TerrainChunkView>();
            sv = GetComponent<SimView>();
        }

        void OnEnable()
        {
            systemMem = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "System Used Memory");
            gcMem = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Reserved Memory");
        }

        void OnDisable()
        {
            systemMem.Dispose();
            gcMem.Dispose();
        }

        /// <summary>Điểm dẫn: trên vòng tròn (tâm, bán kính), ở góc cực của chân + leadDeg. Quy ước x = tâm.x + R·sin, z = tâm.y + R·cos.</summary>
        public static Vector2 Carrot(Vector2 center, float radius, Vector2 feet, float leadDeg)
        {
            float theta = Mathf.Atan2(feet.x - center.x, feet.y - center.y) + leadDeg * Mathf.Deg2Rad;
            return center + radius * new Vector2(Mathf.Sin(theta), Mathf.Cos(theta));
        }

        /// <summary>Bán kính đầu tiên trong RadiusCandidates mà cả vòng trên đất khô; không có thì RadiusCandidates[0], dry = false.</summary>
        public static int PickRadius(WorldData world, Vector2 center, out bool dry)
        {
            foreach (int r in RadiusCandidates)
                if (RouteIsDry(world, center, r)) { dry = true; return r; }
            dry = false;
            return RadiusCandidates[0];
        }

        /// <summary>RouteSamples điểm cách đều trên vòng: trong world, cột không rỗng, không có nước.</summary>
        public static bool RouteIsDry(WorldData world, Vector2 center, int radius)
        {
            var s = world.Size;
            for (int k = 0; k < RouteSamples; k++)
            {
                float a = k * (2f * Mathf.PI / RouteSamples);
                int x = Mathf.FloorToInt(center.x + radius * Mathf.Sin(a));
                int z = Mathf.FloorToInt(center.y + radius * Mathf.Cos(a));
                if (x < 0 || z < 0 || x >= s.X || z >= s.Z) return false;
                MinimapImage.ColumnInfo(world, x, z, out int surface, out int waterDepth);
                if (surface < 0 || waterDepth > 0) return false;
            }
            return true;
        }

        void Update()
        {
            if (Done) return;
            if (!started)
            {
                player = PlayerCharacter.Active;
                if (player == null || !player.Ready || !sv.Plain.HasValue || view.World == null) return;
                var cam = Camera.main;
                follow = cam != null ? cam.GetComponent<FollowCamera>() : null;
                player.readKeyboard = false;
                var c = DemoPlain.CampCenter(sv.Plain.Value);
                RouteCenter = new Vector2(c.x, c.z);
                RouteRadius = PickRadius(view.World, RouteCenter, out bool dry);
                RouteDry = dry;
                var f0 = player.FeetLocal;
                lastPos = lastCheckPos = new Vector2(f0.x, f0.z);
                startTime = Time.realtimeSinceStartup;
                nextStuckCheck = stuckCheckSeconds;
                started = true;
                Debug.Log($"WALKBENCH bắt đầu: tâm ({RouteCenter.x:F1}, {RouteCenter.y:F1}), R={RouteRadius}, đất khô={RouteDry}");
            }

            float t = Time.realtimeSinceStartup - startTime;
            if (t > warmupSeconds + measureSeconds)
            {
                Finish();
                return;
            }

            var f = player.FeetLocal;
            var feet = new Vector2(f.x, f.z);
            if (t >= nextStuckCheck)
            {
                if (Vector2.Distance(feet, lastCheckPos) < stuckMinDistance)
                {
                    StuckCount++;
                    detourUntil = t + detourSeconds;
                }
                lastCheckPos = feet;
                nextStuckCheck = t + stuckCheckSeconds;
            }

            var dir = Carrot(RouteCenter, RouteRadius, feet, leadDegrees) - feet;
            if (t < detourUntil) dir = new Vector2(dir.y, -dir.x); // rẽ phải 90°
            if (dir.sqrMagnitude > 1e-6f) dir.Normalize();
            var worldDir = view.transform.TransformDirection(new Vector3(dir.x, 0f, dir.y));
            if (follow != null) follow.yaw = Mathf.Atan2(worldDir.x, worldDir.z) * Mathf.Rad2Deg;
            player.MoveInput = new Vector2(0f, 1f);
            int px = Mathf.FloorToInt(f.x + dir.x * 0.7f), py = Mathf.FloorToInt(f.y + 0.05f), pz = Mathf.FloorToInt(f.z + dir.y * 0.7f);
            player.JumpInput = WorldEditor.IsSolidAt(view.World, px, py, pz);

            if (t >= warmupSeconds)
            {
                frameMs.Add(Time.unscaledDeltaTime * 1000.0);
                Distance += Vector2.Distance(feet, lastPos);
                int missing = view.MissingColumnsWithin(HoleRadius);
                if (missing > 0) holeFrames++;
                maxMissing = Mathf.Max(maxMissing, missing);
                maxLoadedChunks = Mathf.Max(maxLoadedChunks, view.LoadedChunkCount);
                if (player.InWater) waterFrames++;
                if (systemMem.Valid) maxSystemMem = System.Math.Max(maxSystemMem, systemMem.LastValue);
                if (gcMem.Valid) maxGcMem = System.Math.Max(maxGcMem, gcMem.LastValue);
                result = $"WALKBENCH đang đo… {warmupSeconds + measureSeconds - t:F0} s · {Distance:F0} ô · kẹt {StuckCount}";
            }
            else result = $"WALKBENCH khởi động… {warmupSeconds - t:F0} s";
            lastPos = feet;
        }

        void Finish()
        {
            Done = true;
            player.MoveInput = Vector2.zero;
            player.JumpInput = false;
            var sorted = new List<double>(frameMs);
            sorted.Sort();
            double sum = 0;
            foreach (var x in frameMs) sum += x;
            double avgMs = frameMs.Count > 0 ? sum / frameMs.Count : 0;
            double avgFps = avgMs > 0 ? 1000.0 / avgMs : 0;
            double max = sorted.Count > 0 ? sorted[sorted.Count - 1] : 0;
            var sb = new StringBuilder();
            sb.Append($"WALKBENCH device={SystemInfo.deviceModel} | gpu={SystemInfo.graphicsDeviceName} ({SystemInfo.graphicsDeviceType}) | screen={Screen.width}x{Screen.height} | ");
            sb.Append($"frames={frameMs.Count} avgFps={avgFps:F1} avgMs={avgMs:F2} p50Ms={SimBenchMeter.Percentile(sorted, 0.5):F2} p99Ms={SimBenchMeter.Percentile(sorted, 0.99):F2} maxMs={max:F2} | ");
            sb.Append($"distance={Distance:F1} stuck={StuckCount} waterFrames={waterFrames} routeR={RouteRadius} routeDry={RouteDry} | ");
            sb.Append($"holeFrames={holeFrames} maxMissing={maxMissing} loadedChunksMax={maxLoadedChunks} | ");
            sb.Append($"sysMemMaxMB={maxSystemMem / 1048576.0:F1} gcReservedMaxMB={maxGcMem / 1048576.0:F1} | creatures={(sv.Sim != null ? sv.Sim.State.Creatures.Count : 0)} | ");
            sb.Append($"pass30fps={avgFps >= 30.0} passNoHole={holeFrames == 0}");
            Summary = sb.ToString();
            result = Summary.Replace(" | ", "\n");
            Debug.Log(Summary);
        }

        void OnGUI()
        {
            if (MinimapView.IsAnyOpen) return;
            var style = new GUIStyle(GUI.skin.label) { fontSize = Mathf.Max(18, Screen.height / 40), wordWrap = true };
            style.normal.textColor = Color.white;
            var r = new Rect(30, Screen.height * 0.45f, Screen.width - 60, Screen.height * 0.55f);
            GUI.Box(r, GUIContent.none);
            GUI.Label(new Rect(r.x + 10, r.y + 10, r.width - 20, r.height - 20), result, style);
        }
    }
}
