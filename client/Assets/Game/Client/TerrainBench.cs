using System.Collections.Generic;
using System.Text;
using Unity.Profiling;
using Game.Core;
using Game.World;
using UnityEngine;

namespace Game.Client
{
    /// <summary>
    /// Đo hiệu năng trên điện thoại (world-tech bước 6: "≥ 30 FPS khi đào/đặt liên tục; ghi ms/chunk, số tam giác").
    /// Chạy tự động trong scene Bench: camera cố định nhìn toàn đảo, khởi động <see cref="warmupSeconds"/> giây, rồi đo
    /// <see cref="measureSeconds"/> giây — mỗi khung hình đào hoặc đặt lại một ô ngẫu nhiên (seed cố định), thỉnh thoảng đào bờ
    /// biển cho nước lan. Kết quả hiện trên màn hình và ghi log "BENCH ..." (đọc bằng adb logcat), kèm mã băm world để so tất định.
    /// Bước 7b (brief 0010): mặc định bay vòng tròn qua 3 map (30 ô/s), không đào; đo ms dựng mỗi khung, khung có lỗ trong bán kính sương, bộ nhớ.
    /// </summary>
    [RequireComponent(typeof(TerrainChunkView))]
    public class TerrainBench : MonoBehaviour
    {
        public float warmupSeconds = 3f;
        public float measureSeconds = 40f;
        /// <summary>Mỗi bao nhiêu lần sửa thì có một lần đào ô bờ biển (cho nước lan).</summary>
        public int coastEvery = 15;
        public int seed = 6;
        /// <summary>Đào/đặt mỗi khung như bước 6 (mặc định tắt: bay tự động).</summary>
        public bool editEachFrame = false;
        public float flySpeed = 30f;
        public float flyRadius = 96f;
        public float flyHeight = 44f;
        public float flyPitch = 20f;
        public Vector2 flyCenter = new Vector2(192f, 192f);

        TerrainChunkView view;
        Camera cam;
        ProfilerRecorder systemMem, gcMem;
        readonly List<float> buildMs = new List<float>();
        int holeFrames, maxMissing, maxLoadedChunks;
        long maxSystemMem, maxGcMem;
        System.Random rng;
        readonly List<float> frameMs = new List<float>();
        double meshMs, writeMs, bakeMs;
        int rebuilds, rebuiltChunks, edits, lastRebuildFrame = -1;
        float startTime;
        string worldHash = "", result = "Preparing…";
        bool done;
        Vector3Int pendingRestore;
        BlockMaterial pendingMaterial;
        bool hasPending;

        void Awake()
        {
            cam = Camera.main;
            PlaceCamera(0f);
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

        /// <summary>Đặt camera trên vòng bay tại góc theta (rad), nhìn theo tiếp tuyến, chúi xuống flyPitch độ.</summary>
        void PlaceCamera(float theta)
        {
            if (cam == null)
                return;
            cam.transform.position = new Vector3(flyCenter.x + flyRadius * Mathf.Cos(theta), flyHeight, flyCenter.y + flyRadius * Mathf.Sin(theta));
            var tangent = new Vector3(-Mathf.Sin(theta), 0f, Mathf.Cos(theta));
            cam.transform.rotation = Quaternion.LookRotation(tangent) * Quaternion.Euler(flyPitch, 0f, 0f);
        }

        void Start()
        {
            view = GetComponent<TerrainChunkView>();
            rng = new System.Random(seed);
            Application.targetFrameRate = 60; // Android mặc định khoá 30 → mở 60 để thấy dư địa
            startTime = Time.realtimeSinceStartup;
        }

        void Update()
        {
            if (done || view.World == null)
                return;
            if (worldHash.Length == 0)
            {
                worldHash = WorldHash.ToHex(WorldHash.Compute(view.World));
                foreach (BlockMaterial m in new[] { BlockMaterial.Grass, BlockMaterial.Dirt, BlockMaterial.Stone, BlockMaterial.Sand })
                    view.Bag.Add(m, view.Bag.Main.StackMax);
            }
            float t = Time.realtimeSinceStartup - startTime;
            if (t < warmupSeconds)
            {
                result = $"Warming up… {warmupSeconds - t:F0} s";
                return;
            }
            if (t > warmupSeconds + measureSeconds)
            {
                Finish();
                return;
            }
            frameMs.Add(Time.unscaledDeltaTime * 1000f);
            if (view.LastRebuildFrame != lastRebuildFrame && view.LastRebuildFrame >= 0)
            {
                lastRebuildFrame = view.LastRebuildFrame;
                rebuilds++;
                rebuiltChunks += view.LastRebuildChunks;
                meshMs += view.LastMeshMs;
                writeMs += view.LastWriteMs;
                bakeMs += view.LastBakeMs;
                buildMs.Add((float)(view.LastMeshMs + view.LastWriteMs + view.LastBakeMs));
            }
            PlaceCamera(flySpeed * (t - warmupSeconds) / flyRadius);
            int missing = view.MissingColumnsWithin(120);
            if (missing > 0) holeFrames++;
            maxMissing = Mathf.Max(maxMissing, missing);
            maxLoadedChunks = Mathf.Max(maxLoadedChunks, view.LoadedChunkCount);
            if (systemMem.Valid) maxSystemMem = System.Math.Max(maxSystemMem, systemMem.LastValue);
            if (gcMem.Valid) maxGcMem = System.Math.Max(maxGcMem, gcMem.LastValue);
            if (editEachFrame) EditOnce();
            result = $"Measuring… {warmupSeconds + measureSeconds - t:F0} s · {frameMs.Count} frames · {edits} edits";
        }

        /// <summary>Luân phiên: đào một ô bề mặt ngẫu nhiên, khung sau đặt lại đúng ô đó (địa hình giữ ổn định, chunk dựng lại mỗi khung).</summary>
        void EditOnce()
        {
            var w = view.World;
            if (hasPending)
            {
                if (view.Place(pendingRestore, pendingMaterial) == EditResult.Ok)
                    edits++;
                hasPending = false;
                return;
            }
            bool coast = coastEvery > 0 && edits % coastEvery == 0;
            for (int attempt = 0; attempt < (coast ? 256 : 32); attempt++)
            {
                int x = rng.Next(4, w.Size.X - 4), z = rng.Next(4, w.Size.Z - 4);
                int y = coast ? view.Editor.SeaLevel : w.SurfaceHeight(x, z);
                if (y < 0 || !Cell.IsDiggable(w.Get(x, y, z)))
                    continue;
                if (coast && !TouchesWaterSideways(w, x, y, z))
                    continue;
                var m = w.GetMaterial(x, y, z);
                if (view.Dig(new Vector3Int(x, y, z)) != EditResult.Ok)
                    continue;
                edits++;
                // Ô bờ biển để nước lan, không đặt lại; ô thường đặt lại ở khung sau.
                if (!coast)
                {
                    pendingRestore = new Vector3Int(x, y, z);
                    pendingMaterial = m;
                    hasPending = true;
                }
                return;
            }
        }

        static bool TouchesWaterSideways(WorldData w, int x, int y, int z) =>
            w.GetMaterial(x - 1, y, z) == BlockMaterial.Water || w.GetMaterial(x + 1, y, z) == BlockMaterial.Water ||
            w.GetMaterial(x, y, z - 1) == BlockMaterial.Water || w.GetMaterial(x, y, z + 1) == BlockMaterial.Water;

        void Finish()
        {
            done = true;
            var sorted = new List<float>(frameMs);
            sorted.Sort();
            float sum = 0;
            foreach (var f in frameMs) sum += f;
            float avgMs = frameMs.Count > 0 ? sum / frameMs.Count : 0;
            float p50 = Pct(sorted, 0.5f), p99 = Pct(sorted, 0.99f), max = sorted.Count > 0 ? sorted[sorted.Count - 1] : 0;
            int tris = view.TotalTriangles(out int waterTris);
            double perChunkMesh = rebuiltChunks > 0 ? meshMs / rebuiltChunks : 0;
            var sb = new StringBuilder();
            sb.Append($"BENCH device={SystemInfo.deviceModel} | cpu={SystemInfo.processorType} x{SystemInfo.processorCount} | gpu={SystemInfo.graphicsDeviceName} ({SystemInfo.graphicsDeviceType}) | ");
            sb.Append($"screen={Screen.width}x{Screen.height} | quality={QualitySettings.names[QualitySettings.GetQualityLevel()]} | ");
            sb.Append($"frames={frameMs.Count} avgFps={(avgMs > 0 ? 1000f / avgMs : 0):F1} avgMs={avgMs:F2} p50Ms={p50:F2} p99Ms={p99:F2} maxMs={max:F2} | ");
            sb.Append($"edits={edits} rebuilds={rebuilds} chunksPerRebuild={(rebuilds > 0 ? (double)rebuiltChunks / rebuilds : 0):F2} ");
            sb.Append($"meshMsPerRebuild={(rebuilds > 0 ? meshMs / rebuilds : 0):F2} meshMsPerChunk={perChunkMesh:F2} writeMs={(rebuilds > 0 ? writeMs / rebuilds : 0):F2} bakeMs={(rebuilds > 0 ? bakeMs / rebuilds : 0):F2} | ");
            var sortedBuild = new List<float>(buildMs);
            sortedBuild.Sort();
            float sumB = 0;
            foreach (var f in buildMs) sumB += f;
            float avg = buildMs.Count > 0 ? sumB / buildMs.Count : 0;
            float p99b = Pct(sortedBuild, 0.99f), maxb = sortedBuild.Count > 0 ? sortedBuild[sortedBuild.Count - 1] : 0;
            sb.Append($"fly={flySpeed:F0}u/s r={flyRadius:F0} | buildFrames={buildMs.Count} buildMsAvg={avg:F2} buildMsP99={p99b:F2} buildMsMax={maxb:F2} | holeFrames={holeFrames} maxMissing={maxMissing} loadedChunksMax={maxLoadedChunks} | genMs={view.GenerateMs:F0} initialLoadMs={view.InitialLoadMs:F0} | sysMemMaxMB={maxSystemMem / 1048576.0:F1} gcReservedMaxMB={maxGcMem / 1048576.0:F1} gcTotalMB={System.GC.GetTotalMemory(false) / 1048576.0:F1} worldCellsMB={view.World.Cells.Length * 2 / 1048576.0:F1} | pass30fps={(avgMs > 0 && 1000f / avgMs >= 30f)} passNoHole={(holeFrames == 0)} | ");
            sb.Append($"tris={tris} waterTris={waterTris} | hash={worldHash} v{view.GeneratorVersion}");
            result = sb.ToString().Replace(" | ", "\n");
            Debug.Log(sb.ToString());
        }

        static float Pct(List<float> sorted, float q) =>
            sorted.Count == 0 ? 0 : sorted[Mathf.Clamp(Mathf.CeilToInt(q * sorted.Count) - 1, 0, sorted.Count - 1)];

        void OnGUI()
        {
            if (MinimapView.IsAnyOpen) return; // Bản đồ mở → ẩn hộp kết quả (brief 0016)
            var style = new GUIStyle(GUI.skin.label) { fontSize = Mathf.Max(18, Screen.height / 40), wordWrap = true };
            style.normal.textColor = Color.white;
            var r = new Rect(30, Screen.height * 0.45f, Screen.width - 60, Screen.height * 0.55f);
            GUI.Box(r, GUIContent.none);
            GUI.Label(new Rect(r.x + 10, r.y + 10, r.width - 20, r.height - 20), result, style);
        }
    }
}
