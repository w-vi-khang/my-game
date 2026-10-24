using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Unity.Profiling;
using Game.Core;
using Game.Core.Sim;
using UnityEngine;
using UnityEngine.Rendering;

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
        /// <summary>Adds a fixed second streaming viewpoint at the Map 2 centroid (task 7i, brief 0052). Read once when the walk starts.</summary>
        public bool secondViewpoint;
        /// <summary>0 = giữ nguyên; &gt; 0 = đánh dấu xới quanh trại tới khi có chừng này ô ruộng (đo icon ô ruộng, brief 0065).</summary>
        public int targetPlots;
        /// <summary>Map id of the second viewpoint (Map 2).</summary>
        public const int SecondMap = 1;
        public Transform SecondViewpoint { get; private set; }
        public Vector2 SecondCenter { get; private set; }

        TerrainChunkView view;
        SimView sv;
        PlayerCharacter player;
        FollowCamera follow;
        ProfilerRecorder systemMem, gcMem;
        readonly List<double> frameMs = new List<double>();
        readonly List<double> gpuMs = new List<double>();
        readonly FrameTiming[] ftBuf = new FrameTiming[1];
        long visiblePlotIconSum;
        WorldIconsView icons;
        bool started;
        float startTime, nextStuckCheck, detourUntil;
        Vector2 lastCheckPos, lastPos;
        int holeFrames, maxMissing, maxLoadedChunks, waterFrames, secondHoleFrames, maxLoadedColumns;
        long maxSystemMem, maxGcMem;
        string result = "Preparing…";

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

        /// <summary>Mean of (x + 0.5, z + 0.5) over every column whose map id is map; throws ArgumentException when the map has no column.</summary>
        public static Vector2 MapCentroid(MapTable maps, int map)
        {
            long sx = 0, sz = 0, n = 0;
            for (int z = 0; z < maps.SizeZ; z++)
                for (int x = 0; x < maps.SizeX; x++)
                    if (maps.Get(x, z) == map) { sx += x; sz += z; n++; }
            if (n == 0) throw new System.ArgumentException($"map {map} has no column", nameof(map));
            return new Vector2((float)((double)sx / n + 0.5), (float)((double)sz / n + 0.5));
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
                if (secondViewpoint)
                {
                    if (view.Maps == null)
                    {
                        Debug.LogError("WALKBENCH secondViewpoint needs a multi-map world (source MapsV3); running with one viewpoint");
                        secondViewpoint = false;
                    }
                    else
                    {
                        SecondCenter = MapCentroid(view.Maps, SecondMap);
                        var vp = new GameObject("SecondViewpoint").transform;
                        vp.SetParent(view.transform, false);
                        vp.localPosition = new Vector3(SecondCenter.x, view.World.Size.Y, SecondCenter.y);
                        view.AddViewpoint(vp, view.loadRadius, view.unloadRadius);
                        SecondViewpoint = vp;
                        Debug.Log($"WALKBENCH second viewpoint: map {SecondMap + 1} centroid ({SecondCenter.x:F1}, {SecondCenter.y:F1})");
                    }
                }
                if (targetPlots > 0) AddPlots(targetPlots);
                icons = GetComponent<WorldIconsView>();
                var f0 = player.FeetLocal;
                lastPos = lastCheckPos = new Vector2(f0.x, f0.z);
                startTime = Time.realtimeSinceStartup;
                nextStuckCheck = stuckCheckSeconds;
                started = true;
                Debug.Log($"WALKBENCH started: center ({RouteCenter.x:F1}, {RouteCenter.y:F1}), R={RouteRadius}, dry land={RouteDry}");
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
                if (FrameTimingManager.IsFeatureEnabled())
                {
                    FrameTimingManager.CaptureFrameTimings();
                    if (FrameTimingManager.GetLatestTimings(1, ftBuf) == 1 && ftBuf[0].gpuFrameTime > 0)
                        gpuMs.Add(ftBuf[0].gpuFrameTime);
                }
                if (icons != null) visiblePlotIconSum += icons.VisiblePlotIcons;
                Distance += Vector2.Distance(feet, lastPos);
                int missing = view.MissingColumnsWithin(HoleRadius);
                if (missing > 0) holeFrames++;
                maxMissing = Mathf.Max(maxMissing, missing);
                maxLoadedChunks = Mathf.Max(maxLoadedChunks, view.LoadedChunkCount);
                maxLoadedColumns = Mathf.Max(maxLoadedColumns, view.LoadedColumnCount);
                if (SecondViewpoint != null && view.MissingColumnsWithin(SecondViewpoint, HoleRadius) > 0) secondHoleFrames++;
                if (player.InWater) waterFrames++;
                if (systemMem.Valid) maxSystemMem = System.Math.Max(maxSystemMem, systemMem.LastValue);
                if (gcMem.Valid) maxGcMem = System.Math.Max(maxGcMem, gcMem.LastValue);
                result = $"WALKBENCH measuring… {warmupSeconds + measureSeconds - t:F0} s · {Distance:F0} cells · stuck {StuckCount}";
            }
            else result = $"WALKBENCH warming up… {warmupSeconds - t:F0} s";
            lastPos = feet;
        }

        /// <summary>Marks tilling on ring-shaped columns (step 2) around the camp until the sim has count plots.</summary>
        void AddPlots(int count)
        {
            var c = DemoPlain.CampCenter(sv.Plain.Value);
            int cx = Mathf.FloorToInt(c.x), cz = Mathf.FloorToInt(c.z);
            var world = view.World;
            for (int k = 3; k <= 16 && sv.Sim.State.Plots.Count < count; k++)
                for (int dz = -k; dz <= k && sv.Sim.State.Plots.Count < count; dz += 2)
                    for (int dx = -k; dx <= k && sv.Sim.State.Plots.Count < count; dx += 2)
                    {
                        if (Mathf.Abs(dx) != k && Mathf.Abs(dz) != k) continue;
                        int x = cx + dx, z = cz + dz;
                        if (!world.Size.InBounds(x, 0, z)) continue;
                        int y = world.Size.Y - 2;
                        while (y > 0 && world.GetMaterial(x, y, z) == BlockMaterial.Empty) y--;
                        sv.Sim.MarkTill(new CellPos(x, y, z));
                    }
            Debug.Log($"WALKBENCH plots: {sv.Sim.State.Plots.Count} (target {count})");
        }

        /// <summary>GPU time from FrameTimingManager + GPU Resident Drawer state of the running URP asset (world 7k, brief 0070). Read by reflection: Game.Client does not reference the URP assembly.</summary>
        string GpuSummary()
        {
            string grd = "?", occ = "?";
            var rp = GraphicsSettings.currentRenderPipeline;
            if (rp != null)
            {
                var t = rp.GetType();
                var m = t.GetProperty("gpuResidentDrawerMode", BindingFlags.Public | BindingFlags.Instance);
                var o = t.GetProperty("gpuResidentDrawerEnableOcclusionCullingInCameras", BindingFlags.Public | BindingFlags.Instance);
                if (m != null) grd = m.GetValue(rp)?.ToString() ?? "?";
                if (o != null) occ = o.GetValue(rp)?.ToString() ?? "?";
            }
            double avg = -1, p99 = -1;
            if (FrameTimingManager.IsFeatureEnabled() && gpuMs.Count > 0)
            {
                var sorted = new List<double>(gpuMs);
                sorted.Sort();
                double sum = 0;
                foreach (var x in gpuMs) sum += x;
                avg = sum / gpuMs.Count;
                p99 = SimBenchMeter.Percentile(sorted, 0.99);
            }
            return $"gpuMsAvg={avg:F2} gpuMsP99={p99:F2} gpuSamples={gpuMs.Count} grd={grd} occ={occ}";
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
            sb.Append($"viewpoints={view.ViewpointCount} secondCenter={(SecondViewpoint != null ? $"({SecondCenter.x:F1};{SecondCenter.y:F1})" : "none")} | ");
            sb.Append($"holeFrames={holeFrames} secondHoleFrames={secondHoleFrames} maxMissing={maxMissing} loadedColumnsMax={maxLoadedColumns} loadedChunksMax={maxLoadedChunks} | ");
            sb.Append($"sysMemMaxMB={maxSystemMem / 1048576.0:F1} gcReservedMaxMB={maxGcMem / 1048576.0:F1} | creatures={(sv.Sim != null ? sv.Sim.State.Creatures.Count : 0)} | ");
            sb.Append($"plots={(sv.Sim != null ? sv.Sim.State.Plots.Count : 0)} plotIconsVisibleAvg={(icons != null && frameMs.Count > 0 ? (double)visiblePlotIconSum / frameMs.Count : -1.0):F1} | ");
            sb.Append(GpuSummary()).Append(" | ");
            sb.Append($"pass30fps={avgFps >= 30.0} passNoHole={holeFrames == 0 && secondHoleFrames == 0}");
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
