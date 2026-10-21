using System.Collections.Generic;
using Game.Core;
using Game.Core.Sim;
using Game.Core.View;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

namespace Game.Client
{
    public enum TillResult { Invalid, Marked, Unmarked, OpenedPlot }

    /// <summary>
    /// Scene SimView (sim bước 6, user chọn 2026-10-16): mô phỏng M1 chạy trên map thật với bố trí SimDemo (2 nhà, 1 kho, 4 ô ruộng, L1–L3).
    /// Sinh vật = khối hộp theo khung chiếm chỗ, màu theo loài, mũi chỉ hướng đi; vị trí nội suy giữa 2 nhịp (trễ 1 nhịp).
    /// Nhãn trên đầu = trạng thái; "!" đỏ = đình trệ tại ô. F1 / F2: khoá 30 / 60 FPS (chỉ Game view trong Editor).
    /// HUD góc trái: kho + từng ô ruộng (giai đoạn, % lớn); mầm xanh cao theo %; hộp vàng trên lưng = đang mang hàng (sửa sau user xem 2026-10-16).
    /// Đào/đặt (TerrainEditInput) báo cho mô phỏng qua WorldEditor.CellChanged; ô có sinh vật/nhà/kho không đặt được.
    /// Xới (brief 0030): biển canh tác, bảng ô ruộng chọn giống/hàng đợi, nút Kho (K); trại demo có 1 vụ Cây A ban đầu, sau đó người chơi tự chọn.
    /// Xới xong → ô ruộng (màu riêng, tấm 'Ruộng' ẩn); đào/đặt lên ô ruộng → huỷ vụ, bảng ghi 'đã huỷ' (sim 6a, 0020).
    /// World 3 map (world 7e, brief 0020): Terrain dùng MapsV3 → SimDemo.FindSite chọn chỗ trong Map 1, mô phỏng trong vùng 64 × 64, camera đặt ở trại.
    /// </summary>
    [RequireComponent(typeof(TerrainChunkView))]
    public class SimView : MonoBehaviour
    {
        public int maxTicksPerFrame = 5;

        /// <summary>Scene đo sim bước 7 (brief 0028): đồng bằng giữa Map 1 + 60 sinh vật, log SIMBENCH.</summary>
        public bool stressBench;
        public float benchSeconds = 240f;
        public StressPlain? Plain { get; private set; }
        public SimBenchMeter Meter { get; private set; }

        static readonly Color HouseColor = new Color(0.55f, 0.35f, 0.20f);
        static readonly Color StorageColor = new Color(0.60f, 0.60f, 0.60f);
        static readonly Color PostColor = new Color(0.45f, 0.30f, 0.15f);
        static readonly Color SignColor = new Color(0.90f, 0.80f, 0.55f);
        static readonly Color NoseColor = new Color(0.15f, 0.15f, 0.15f);
        static readonly Color LoadColor = new Color(0.95f, 0.85f, 0.25f);
        static readonly Color SproutColor = new Color(0.30f, 0.75f, 0.25f);

        static Color SpeciesColor(string id) => id switch
        {
            "L1" => new Color(0.95f, 0.55f, 0.20f),
            "L2" => new Color(0.25f, 0.60f, 0.95f),
            "L3" => new Color(0.65f, 0.40f, 0.90f),
            _ => Color.white,
        };

        TerrainChunkView view;
        Transform root;
        SimClock clock;
        SimInterpolator interp;
        StallMarkers markers;
        readonly List<Transform> bodies = new List<Transform>();
        readonly List<Transform> pivots = new List<Transform>();
        readonly List<GameObject> loads = new List<GameObject>();
        readonly List<Transform> sprouts = new List<Transform>();
        readonly List<GameObject> signs = new List<GameObject>();
        int savedVSync, savedTargetFps;
        float smoothedDt = 1f / 60f;
        GUIStyle labelStyle, hudStyle;

        public Simulation Sim { get; private set; }
        /// <summary>Chỗ đặt trại trên world nhiều map (world 7e, brief 0020); null khi chạy đảo v2.</summary>
        public DemoSite? Site { get; private set; }
        UnityEngine.Material boxBase;
        public int TargetFps { get; private set; }
        public int BodyCount => bodies.Count;
        public int SproutCount => sprouts.Count;

        void Awake()
        {
            active = this;
            view = GetComponent<TerrainChunkView>();
            savedVSync = QualitySettings.vSyncCount;
            savedTargetFps = Application.targetFrameRate;
            if (stressBench) view.afterGenerate = OnAfterGenerate;
        }

        public static Vector3 BenchCameraPos(StressPlain p) => new Vector3(p.X0 + p.Size * 0.5f, p.Ground + 34f, p.Z0 - 6f);

        IReadOnlyList<Landmark> OnAfterGenerate(WorldData world, MapTable maps, WorldMapParams p, IReadOnlyList<Landmark> lms)
        {
            var plain = SimStress.FlattenPlain(world, maps, 0, p.bedrockLayers, p.dirtDepth, p.waterLevel, ConfigLoader.LoadSimConfig().regionSize);
            Plain = plain;
            var cam = Camera.main;
            if (cam != null)
                cam.transform.SetPositionAndRotation(BenchCameraPos(plain), Quaternion.Euler(50f, 0f, 0f));
            var kept = SimStress.WithoutPlain(lms, plain);
            Debug.Log($"SIMBENCH {plain}, bỏ {lms.Count - kept.Count} dấu mốc trong đồng bằng");
            return kept;
        }

        void OnDestroy()
        {
            if (active == this) active = null;
            QualitySettings.vSyncCount = savedVSync;
            Application.targetFrameRate = savedTargetFps;
        }

        void Update()
        {
            if (Sim == null)
            {
                // TerrainChunkView dựng world trong Start — Update đầu tiên chạy sau mọi Start.
                if (view.World == null || view.Editor == null) return;
                Setup();
            }

            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.f1Key.wasPressedThisFrame) SetFrameRate(30);
                if (kb.f2Key.wasPressedThisFrame) SetFrameRate(60);
                if (kb.kKey.wasPressedThisFrame) ToggleStock();
                if (OpenPlotId >= 0 && kb.escapeKey.wasPressedThisFrame) ClosePlotPanel();
            }
            smoothedDt = smoothedDt * 0.9f + Time.unscaledDeltaTime * 0.1f;

            int n = clock.Advance(Time.deltaTime);
            var events = Sim.State.Events;
            for (int i = 0; i < n; i++)
            {
                if (Meter != null) SimDemo.KeepFarming(Sim); // chỉ cảnh đo (brief 0028); demo: người chơi tự chọn giống (0025, brief 0030)
                long t0 = System.Diagnostics.Stopwatch.GetTimestamp();
                Sim.Tick();
                Meter?.AddTick(System.Diagnostics.Stopwatch.GetTimestamp() - t0);
                interp.Capture(Sim.State);
                interp.ApplyEvents(events);
                markers.ApplyEvents(events);
                events.Clear();
            }
            if (OpenPlotId >= 0 && Sim.State.Plots[OpenPlotId].Removed) ClosePlotPanel();
            Meter?.AddFrame(Time.unscaledTime, Time.unscaledDeltaTime, n, Sim);
            EnsurePlotVisuals();
            UpdateBodies();
            UpdateSprouts();
            UpdatePlotSigns();
        }

        void Setup()
        {
            var cfg = ConfigLoader.LoadSimConfig();
            if (stressBench)
            {
                if (!Plain.HasValue)
                {
                    Debug.LogError("SIMBENCH cần TerrainChunkView source = MapsV3");
                    enabled = false;
                    return;
                }
                var plain = Plain.Value;
                Sim = new Simulation(cfg, view.World, plain.Bounds);
                Sim.AttachEditor(view.Editor);
                SimStress.Build(Sim, plain);
                Sim.ProfilePhases = true;
                Meter = new SimBenchMeter(benchSeconds);
                Debug.Log($"SIMBENCH bố trí: {Sim.State.Creatures.Count} sinh vật, {Sim.State.Houses.Count} nhà, {Sim.State.Storages.Count} kho, {Sim.State.Plots.Count} ô ruộng, mảng tìm đường {Sim.PathfinderCellCount} ô");
            }
            else if (view.Maps == null)
            {
                Sim = new Simulation(cfg, view.World);
                Sim.AttachEditor(view.Editor);
                SimDemo.Build(Sim);
            }
            else
            {
                var sw = System.Diagnostics.Stopwatch.StartNew();
                var site = SimDemo.FindSite(view.World, view.Maps, view.Landmarks, 0, ConfigLoader.LoadWorldMapParams().waterLevel, cfg.regionSize);
                double ms = sw.Elapsed.TotalMilliseconds;
                Site = site;
                Sim = new Simulation(cfg, view.World, site.Bounds);
                Sim.AttachEditor(view.Editor);
                SimDemo.Build(Sim, site);
                var camPos = PlaceCamera(site);
                Debug.Log($"SIMVIEW trại {site}, camera ({camPos.x:F0}, {camPos.y:F0}, {camPos.z:F0}), tìm chỗ {ms:F1} ms, mảng tìm đường {Sim.PathfinderCellCount} ô");
            }
            boxBase = Resources.Load<UnityEngine.Material>("Materials/Landmark");
            if (boxBase == null)
                Debug.LogError("SIMVIEW thiếu Resources/Materials/Landmark.mat");

            clock = new SimClock(cfg.ticksPerSecond, maxTicksPerFrame);
            interp = new SimInterpolator();
            interp.Capture(Sim.State);
            markers = new StallMarkers();

            root = new GameObject("Sim").transform;
            root.SetParent(transform, false);
            foreach (var h in Sim.State.Houses)
                Box("Nhà " + h.Id, new Vector3(h.Cell.X + h.SizeX * 0.5f, h.Cell.Y + h.SizeY * 0.5f, h.Cell.Z + h.SizeZ * 0.5f),
                    new Vector3(h.SizeX * 0.98f, h.SizeY * 0.98f, h.SizeZ * 0.98f), HouseColor, root, solid: true);
            foreach (var s in Sim.State.Storages)
                Box("Kho " + s.Id, new Vector3(s.Cell.X + 0.5f, s.Cell.Y + 0.45f, s.Cell.Z + 0.5f), new Vector3(0.9f, 0.9f, 0.9f), StorageColor, root, solid: true);
            EnsurePlotVisuals();
            UpdateBodies();
            UpdatePlotSigns();
            SetFrameRate(60);
        }

        /// <summary>Camera nhìn vào trại (brief 0020): cột giữa cửa sổ, cao hơn mặt đất 12, lùi 26 theo −z, xoay Euler(25, 0, 0) như scene.</summary>
        public static Vector3 CameraPosFor(DemoSite site)
        {
            int ccx = site.Dx + SimDemo.BaseX - 1 + 4, ccz = site.Dz + SimDemo.BaseZ - 1 + 3;
            int h = site.Height(ccx, ccz);
            return new Vector3(ccx + 0.5f, h + 12f, ccz + 0.5f - 26f);
        }

        /// <summary>Tâm cửa sổ 9 × 7 (toạ độ thế giới, trên mặt đất cột giữa).</summary>
        public static Vector3 CampCenter(DemoSite site)
        {
            int ccx = site.Dx + SimDemo.BaseX - 1 + 4, ccz = site.Dz + SimDemo.BaseZ - 1 + 3;
            return new Vector3(ccx + 0.5f, site.Height(ccx, ccz) + 1f, ccz + 0.5f);
        }

        Vector3 PlaceCamera(DemoSite site)
        {
            var pos = CameraPosFor(site);
            var cam = Camera.main;
            if (cam == null)
                return pos;
            cam.transform.SetPositionAndRotation(pos, Quaternion.Euler(25f, 0f, 0f));
            return pos;
        }

        void SetFrameRate(int fps)
        {
            TargetFps = fps;
            QualitySettings.vSyncCount = 0; // vSyncCount != 0 thì targetFrameRate bị bỏ qua
            Application.targetFrameRate = fps;
        }

        void UpdateBodies()
        {
            var cs = Sim.State.Creatures;
            while (bodies.Count < cs.Count)
                CreateBody(cs[bodies.Count]);
            for (int i = 0; i < cs.Count; i++)
            {
                var c = cs[i];
                interp.Sample(i, clock.Alpha, out double x, out double y, out double z);
                bodies[i].localPosition = new Vector3((float)x + c.SizeX * 0.5f, (float)y + c.SizeY * 0.4f, (float)z + c.SizeZ * 0.5f);
                interp.Heading(i, out int hx, out int hz);
                pivots[i].localRotation = Quaternion.LookRotation(new Vector3(hx, 0f, hz));
                loads[i].SetActive(SiteStatus.IsCarrying(Sim.State, c));
            }
        }

        void UpdatePlotSigns()
        {
            var ps = Sim.State.Plots;
            for (int i = 0; i < ps.Count && i < signs.Count; i++)
                signs[i].SetActive(!ps[i].Tilled && !ps[i].Removed);
        }

        /// <summary>Biển canh tác (cọc + tấm, hình tạm — 0025) và mầm cho mọi ô ruộng chưa có; gọi mỗi khung và ngay sau MarkTill.</summary>
        void EnsurePlotVisuals()
        {
            var ps = Sim.State.Plots;
            while (signs.Count < ps.Count)
            {
                var p = ps[signs.Count];
                var sign = new GameObject("Biển " + p.Id);
                sign.transform.SetParent(root, false);
                sign.transform.localPosition = new Vector3(p.Soil.X + 0.5f, p.Soil.Y + 1f, p.Soil.Z + 0.5f);
                Box("Cọc", new Vector3(0f, 0.35f, 0f), new Vector3(0.08f, 0.7f, 0.08f), PostColor, sign.transform);
                Box("Tấm", new Vector3(0f, 0.65f, 0f), new Vector3(0.5f, 0.3f, 0.05f), SignColor, sign.transform);
                signs.Add(sign);
                var sprout = Box("Mầm " + p.Id, Vector3.zero, Vector3.one, SproutColor, root);
                sprout.SetActive(false);
                sprouts.Add(sprout.transform);
            }
        }

        public int SignCount => signs.Count;
        public bool SignVisible(int plotId) => plotId < signs.Count && signs[plotId].activeSelf;

        /// <summary>Ô ruộng đang mở bảng; -1 = đóng.</summary>
        public int OpenPlotId { get; private set; } = -1;
        public bool StockOpen { get; private set; }

        static CellPos ToCell(Vector3Int c) => new CellPos(c.x, c.y, c.z);

        /// <summary>Ô Xới nhắm được (brief 0030 mục 8): đánh dấu được, hoặc đang là ô ruộng / ô có biển.</summary>
        public bool CanTillAt(Vector3Int cell) => Sim != null && (Sim.CanMarkTill(ToCell(cell)) || Sim.PlotAt(ToCell(cell)) >= 0);

        /// <summary>Chuột trái khi cầm Xới (0025, brief 0030 mục 2).</summary>
        public TillResult UseTill(Vector3Int cell)
        {
            if (Sim == null) return TillResult.Invalid;
            var c = ToCell(cell);
            int id = Sim.PlotAt(c);
            if (id >= 0)
            {
                if (!Sim.State.Plots[id].Tilled)
                {
                    Sim.Unmark(id);
                    UpdatePlotSigns();
                    return TillResult.Unmarked;
                }
                OpenPlotId = id;
                return TillResult.OpenedPlot;
            }
            id = Sim.MarkTill(c);
            if (id < 0) return TillResult.Invalid;
            EnsurePlotVisuals();
            UpdatePlotSigns();
            return TillResult.Marked;
        }

        /// <summary>Nút "+ Cây X" của bảng ô ruộng.</summary>
        public bool ChooseCrop(int cropId) => OpenPlotId >= 0 && Sim.EnqueueCrop(OpenPlotId, cropId);
        /// <summary>Nút "Xoá" ở dòng index của hàng đợi.</summary>
        public bool RemoveQueued(int index) => OpenPlotId >= 0 && Sim.RemoveCrop(OpenPlotId, index);
        public void ClosePlotPanel() => OpenPlotId = -1;
        public void ToggleStock() => StockOpen = !StockOpen;

        static float GuiScale => Mathf.Max(1f, Screen.height / 720f);
        public static Rect StockButtonRect(float gw, float gh) => new Rect(10f, gh - 46f, 110f, 36f);
        public static Rect StockPanelRect(float gw, float gh, int crops) { float h = 32f + 24f * crops; return new Rect(10f, gh - 56f - h, 320f, h); }
        public static Rect PlotPanelRect(float gw, float gh) => new Rect(gw * 0.5f - 170f, gh - 250f, 340f, 240f);

        static SimView active;

        /// <summary>Con trỏ (toạ độ màn hình, gốc dưới-trái) trên nút Kho / bảng kho / bảng ô ruộng → công cụ không dùng (brief 0030 mục 12).</summary>
        public static bool BlocksPointer(Vector2 screen) => active != null && active.Sim != null && active.Covers(screen);

        public bool Covers(Vector2 screen)
        {
            float s = GuiScale, gw = Screen.width / s, gh = Screen.height / s;
            var g = new Vector2(screen.x / s, (Screen.height - screen.y) / s);
            if (StockButtonRect(gw, gh).Contains(g)) return true;
            if (StockOpen && StockPanelRect(gw, gh, Sim.Config.crops.Length).Contains(g)) return true;
            return OpenPlotId >= 0 && PlotPanelRect(gw, gh).Contains(g);
        }

        void DrawFarmGui(float scale)
        {
            float gw = Screen.width / scale, gh = Screen.height / scale;
            if (GUI.Button(StockButtonRect(gw, gh), "Kho (K)")) ToggleStock();
            if (StockOpen)
            {
                var r = StockPanelRect(gw, gh, Sim.Config.crops.Length);
                GUI.Box(r, "");
                GUI.Label(new Rect(r.x + 8f, r.y + 4f, r.width - 16f, r.height - 8f), SiteStatus.StockPanelText(Sim.State.Stock, Sim.Config), hudStyle);
            }
            if (OpenPlotId < 0) return;
            var pr = PlotPanelRect(gw, gh);
            GUI.Box(pr, "");
            var p = Sim.State.Plots[OpenPlotId];
            var cfg = Sim.Config;
            float x = pr.x + 8f, w = pr.width - 16f, y = pr.y + 8f;
            GUI.Label(new Rect(x, y, w, 26f), SiteStatus.PlotText(p, Sim.State.Tick, cfg.GrowTicks(p.Queue.Count > 0 ? p.Queue[0] : 0)), hudStyle);
            y += 26f;
            GUI.Label(new Rect(x, y, 90f, 26f), "Chọn giống:", hudStyle);
            GUI.enabled = p.Queue.Count < cfg.farm.plotQueueCapacity;
            for (int i = 0; i < cfg.crops.Length; i++)
                if (GUI.Button(new Rect(x + 95f + i * 104f, y, 100f, 24f), "+ " + SiteStatus.CropName(cfg, i))) ChooseCrop(i);
            GUI.enabled = true;
            y += 26f;
            GUI.Label(new Rect(x, y, w, 26f), "Hàng đợi (" + p.Queue.Count + "/" + cfg.farm.plotQueueCapacity + "):", hudStyle);
            y += 26f;
            for (int k = 0; k < cfg.farm.plotQueueCapacity; k++)
            {
                GUI.Label(new Rect(x, y, w - 70f, 26f), SiteStatus.QueueLine(p, cfg, k), hudStyle);
                if (k < p.Queue.Count && GUI.Button(new Rect(x + w - 60f, y, 60f, 24f), "Xoá")) { RemoveQueued(k); break; }
                y += 26f;
            }
            if (GUI.Button(new Rect(x, pr.yMax - 36f, 110f, 28f), "Đóng (Esc)")) ClosePlotPanel();
        }

        void UpdateSprouts()
        {
            var ps = Sim.State.Plots;
            for (int i = 0; i < ps.Count && i < sprouts.Count; i++)
            {
                var p = ps[i];
                int grow = Sim.Config.GrowTicks(p.Queue.Count > 0 ? p.Queue[0] : 0);
                bool show = p.Phase == PlotPhase.Growing || p.Phase == PlotPhase.Harvest;
                sprouts[i].gameObject.SetActive(show);
                if (!show) continue;
                int pct = p.Phase == PlotPhase.Harvest ? 100 : SiteStatus.GrowPercent(p, Sim.State.Tick, grow);
                float h = 0.05f + 0.6f * pct / 100f;
                sprouts[i].localPosition = new Vector3(p.Soil.X + 0.5f, p.Soil.Y + 1.04f + h * 0.5f, p.Soil.Z + 0.5f);
                sprouts[i].localScale = new Vector3(0.3f, h, 0.3f);
            }
        }

        void CreateBody(Creature c)
        {
            var body = new GameObject("Sinh vật " + c.Id + " " + c.SpeciesId).transform;
            body.SetParent(root, false);
            Box("Thân", Vector3.zero, new Vector3(c.SizeX * 0.8f, c.SizeY * 0.8f, c.SizeZ * 0.8f), SpeciesColor(c.SpeciesId), body);
            var pivot = new GameObject("Hướng").transform;
            pivot.SetParent(body, false);
            Box("Mũi", new Vector3(0f, c.SizeY * 0.15f, Mathf.Max(c.SizeX, c.SizeZ) * 0.4f + 0.1f), new Vector3(0.25f, 0.25f, 0.3f), NoseColor, pivot);
            var load = Box("Hàng", new Vector3(0f, c.SizeY * 0.4f + 0.15f, 0f), new Vector3(0.35f, 0.3f, 0.35f), LoadColor, body);
            load.SetActive(false);
            loads.Add(load);
            bodies.Add(body);
            pivots.Add(pivot);
        }

        GameObject Box(string name, Vector3 localPos, Vector3 scale, Color color, Transform parent, bool solid = false)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            if (!solid)
                Destroy(go.GetComponent<Collider>()); // sinh vật, ruộng, mầm: không chặn raycast đào/đặt
            var t = go.transform;
            t.SetParent(parent, false);
            t.localPosition = localPos;
            t.localScale = scale;
            var mat = new UnityEngine.Material(boxBase != null ? boxBase : GraphicsSettings.currentRenderPipeline.defaultMaterial);
            mat.SetColor("_BaseColor", color);
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;
            return go;
        }

        void OnGUI()
        {
            if (Sim == null)
                return;
            float scale = Mathf.Max(1f, Screen.height / 720f);
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            if (labelStyle == null)
            {
                labelStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, richText = true };
                hudStyle = new GUIStyle(GUI.skin.label) { richText = true };
            }
            GUI.Label(new Rect(10, 100, 1000, 24),
                $"F1: 30 FPS · F2: 60 FPS — mục tiêu {TargetFps} · đo {1f / smoothedDt:F1} FPS · nhịp {Sim.State.Tick}", hudStyle);
            int line = 1;
            GUI.Label(new Rect(10, 100 + 24 * line++, 1000, 24), SiteStatus.StorageText(Sim.State.Stock), hudStyle);
            if (Meter == null)
                foreach (var p in Sim.State.Plots)
                    GUI.Label(new Rect(10, 100 + 24 * line++, 1000, 24), SiteStatus.PlotText(p, Sim.State.Tick, Sim.Config.GrowTicks(p.Queue.Count > 0 ? p.Queue[0] : 0)), hudStyle);
            if (Meter != null && Meter.LastLine != null)
                GUI.Label(new Rect(10, 100 + 24 * line++, 1400, 24), Meter.LastLine, hudStyle);

            if (!MinimapView.IsAnyOpen)
                DrawFarmGui(scale);

            var cam = Camera.main;
            if (cam == null)
                return;
            var cs = Sim.State.Creatures;
            for (int i = 0; i < cs.Count && i < bodies.Count; i++)
                DrawLabel(cam, bodies[i].position + Vector3.up * (cs[i].SizeY * 0.4f + 0.35f), CreatureStatus.Text(Sim.State, cs[i]), scale);
            foreach (var e in markers.Active)
                DrawLabel(cam, transform.TransformPoint(new Vector3(e.Cell.X + 0.5f, e.Cell.Y + 1.3f, e.Cell.Z + 0.5f)),
                    "<color=red><b>! " + CreatureStatus.ReasonText(e.Reason) + "</b></color>", scale);
        }

        void DrawLabel(Camera cam, Vector3 world, string text, float scale)
        {
            var sp = cam.WorldToScreenPoint(world);
            if (sp.z <= 0f)
                return;
            // WorldToScreenPoint gốc dưới trái; GUI gốc trên trái.
            GUI.Label(new Rect(sp.x / scale - 100f, (Screen.height - sp.y) / scale - 11f, 200f, 22f), text, labelStyle);
        }
    }
}
