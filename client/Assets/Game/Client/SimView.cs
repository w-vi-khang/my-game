using System.Collections.Generic;
using Game.Core;
using Game.Core.Sim;
using Game.Core.View;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

namespace Game.Client
{
    /// <summary>Kết quả chuột trái khi cầm Xới. SelfTill = ô có biển chưa xới → tự đi tới xới (brief 0041).</summary>
    public enum TillResult { Invalid, Marked, Unmarked, OpenedPlot, SelfTill }

    /// <summary>
    /// Scene SimView (sim bước 6, user chọn 2026-10-16): mô phỏng M1 chạy trên map thật với bố trí SimDemo (2 nhà, 1 kho, 4 ô ruộng, L1–L3).
    /// Sinh vật = khối hộp theo khung chiếm chỗ, màu theo loài, mũi chỉ hướng đi; vị trí nội suy giữa 2 nhịp (trễ 1 nhịp).
    /// Nhãn trên đầu = trạng thái; "!" đỏ = đình trệ tại ô. F1 / F2: khoá 30 / 60 FPS (chỉ Game view trong Editor).
    /// HUD góc trái: kho + từng ô ruộng (giai đoạn, % lớn); mầm xanh cao theo %; hộp vàng trên lưng = đang mang hàng (sửa sau user xem 2026-10-16).
    /// Đào/đặt (TerrainEditInput) báo cho mô phỏng qua WorldEditor.CellChanged; ô có sinh vật/nhà/kho không đặt được.
    /// Xới (brief 0030): biển canh tác, bảng ô ruộng chọn giống/hàng đợi, kho mở bằng chuột phải khối kho (InventoryView, brief 0062); trại demo có 1 vụ Cây A ban đầu, sau đó người chơi tự chọn.
    /// Xới xong → ô ruộng (màu riêng, tấm 'Ruộng' ẩn); đào/đặt lên ô ruộng → huỷ vụ, bảng ghi 'đã huỷ' (sim 6a, 0020).
    /// World 3 map (task m1-demo-dong-bang): san đồng bằng 48 × 48 giữa Map 1 (DemoPlain.Flatten), trại SimDemo bố trí giãn ở giữa, mô phỏng trong vùng 64 × 64, camera nhìn vào trại.
    /// </summary>
    [RequireComponent(typeof(TerrainChunkView))]
    public class SimView : MonoBehaviour
    {
        public int maxTicksPerFrame = 5;

        /// <summary>Scene đo sim bước 7 (brief 0028): đồng bằng giữa Map 1 + 60 sinh vật, log SIMBENCH.</summary>
        public bool stressBench;
        public float benchSeconds = 240f;
        /// <summary>Đồng bằng đã san (world nhiều map, cả demo lẫn bench); null khi chạy đảo v2.</summary>
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
            view.afterGenerate = OnAfterGenerate;
            if (GetComponent<SettingsView>() == null) gameObject.AddComponent<SettingsView>();
        }

        public static Vector3 BenchCameraPos(StressPlain p) => new Vector3(p.X0 + p.Size * 0.5f, p.Ground + 34f, p.Z0 - 6f);

        IReadOnlyList<Landmark> OnAfterGenerate(WorldData world, MapTable maps, WorldMapParams p, IReadOnlyList<Landmark> lms)
        {
            var kept = DemoPlain.Flatten(world, maps, p, lms, out var plain);
            Plain = plain;
            if (stressBench) DemoPlain.PlaceCamera(BenchCameraPos(plain), 50f);
            else
            {
                DemoPlain.PlaceCamera(DemoPlain.CameraPos(plain), DemoPlain.Pitch);
                var player = PlayerCharacter.Active;
                if (player != null)
                {
                    var feet = PlayerCharacter.PlainSpawn(plain);
                    player.Place(transform.TransformPoint(feet), 0f);
                    Debug.Log($"PLAYER spawned at camp ({feet.x:F1}, {feet.y:F1}, {feet.z:F1})");
                }
            }
            Debug.Log($"{(stressBench ? "SIMBENCH" : "SIMVIEW")} {plain}, dropped {lms.Count - kept.Count} landmarks inside the plain");
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

            if (DebugKeys.WasPressed(Key.F1)) SetFrameRate(30);
            if (DebugKeys.WasPressed(Key.F2)) SetFrameRate(60);
            if (OpenPlotId >= 0 && GameInput.PanelsClose.WasPressedThisFrame()) ClosePlotPanel();
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
                    Debug.LogError("SIMBENCH requires TerrainChunkView source = MapsV3");
                    enabled = false;
                    return;
                }
                var plain = Plain.Value;
                Sim = new Simulation(cfg, view.World, plain.Bounds);
                Sim.AttachEditor(view.Editor);
                SimStress.Build(Sim, plain);
                Sim.ProfilePhases = true;
                Meter = new SimBenchMeter(benchSeconds);
                Debug.Log($"SIMBENCH layout: {Sim.State.Creatures.Count} creatures, {Sim.State.Houses.Count} houses, {Sim.State.Storages.Count} storages, {Sim.State.Plots.Count} plots, pathfinder array {Sim.PathfinderCellCount} cells");
            }
            else if (view.Maps == null)
            {
                Sim = new Simulation(cfg, view.World);
                Sim.AttachEditor(view.Editor);
                SimDemo.Build(Sim);
            }
            else
            {
                if (!Plain.HasValue)
                {
                    Debug.LogError("SIMVIEW missing plain (afterGenerate overwritten?)");
                    enabled = false;
                    return;
                }
                var plain = Plain.Value;
                Sim = new Simulation(cfg, view.World, plain.Bounds);
                Sim.AttachEditor(view.Editor);
                SimDemo.Build(Sim, plain);
                Debug.Log($"SIMVIEW camp in the middle of {plain}, pathfinder array {Sim.PathfinderCellCount} cells");
            }
            boxBase = Resources.Load<UnityEngine.Material>("Materials/Landmark");
            if (boxBase == null)
                Debug.LogError("SIMVIEW missing Resources/Materials/Landmark.mat");

            clock = new SimClock(cfg.ticksPerSecond, maxTicksPerFrame);
            interp = new SimInterpolator();
            interp.Capture(Sim.State);
            markers = new StallMarkers();

            root = new GameObject("Sim").transform;
            root.SetParent(transform, false);
            foreach (var h in Sim.State.Houses)
                Box("House " + h.Id, new Vector3(h.Cell.X + h.SizeX * 0.5f, h.Cell.Y + h.SizeY * 0.5f, h.Cell.Z + h.SizeZ * 0.5f),
                    new Vector3(h.SizeX * 0.98f, h.SizeY * 0.98f, h.SizeZ * 0.98f), HouseColor, root, solid: true);
            storageColliders.Clear();
            foreach (var s in Sim.State.Storages)
            {
                var box = Box("Storage " + s.Id, new Vector3(s.Cell.X + 0.5f, s.Cell.Y + 0.45f, s.Cell.Z + 0.5f), new Vector3(0.9f, 0.9f, 0.9f), StorageColor, root, solid: true);
                storageColliders.Add(box.GetComponent<Collider>());
            }
            EnsurePlotVisuals();
            UpdateBodies();
            UpdatePlotSigns();
            SetFrameRate(GameSettings.Current.targetFps);
        }

        public void SetFrameRate(int fps)
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
                var sign = new GameObject("Sign " + p.Id);
                sign.transform.SetParent(root, false);
                sign.transform.localPosition = new Vector3(p.Soil.X + 0.5f, p.Soil.Y + 1f, p.Soil.Z + 0.5f);
                Box("Post", new Vector3(0f, 0.35f, 0f), new Vector3(0.08f, 0.7f, 0.08f), PostColor, sign.transform);
                Box("Board", new Vector3(0f, 0.65f, 0f), new Vector3(0.5f, 0.3f, 0.05f), SignColor, sign.transform);
                signs.Add(sign);
                var sprout = Box("Sprout " + p.Id, Vector3.zero, Vector3.one, SproutColor, root);
                sprout.SetActive(false);
                sprouts.Add(sprout.transform);
            }
        }

        public int SignCount => signs.Count;
        public bool SignVisible(int plotId) => plotId < signs.Count && signs[plotId].activeSelf;

        /// <summary>Ô ruộng đang mở bảng; -1 = đóng.</summary>
        public int OpenPlotId { get; private set; } = -1;

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
                    return TillResult.SelfTill;
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

        /// <summary>Chuột phải khi cầm Xới (brief 0041): ô có biển chưa xới → bỏ đánh dấu. true khi đã bỏ.</summary>
        public bool UnmarkAt(Vector3Int cell)
        {
            if (Sim == null) return false;
            int id = Sim.PlotAt(ToCell(cell));
            if (id < 0 || Sim.State.Plots[id].Tilled || !Sim.Unmark(id)) return false;
            UpdatePlotSigns();
            return true;
        }

        /// <summary>Click trái ô ruộng (0034 mục 3): mở bảng ô ruộng plotId (có biển hoặc đã xới). false nếu không có / đã bỏ.</summary>
        public bool OpenPlotPanel(int plotId)
        {
            if (Sim == null || plotId < 0 || plotId >= Sim.State.Plots.Count || Sim.State.Plots[plotId].Removed) return false;
            OpenPlotId = plotId;
            return true;
        }

        /// <summary>Nút "Bỏ đánh dấu" (0034 mục 3): ô đang mở chưa xới → bỏ biển, huỷ tự xới ô đó, đóng bảng. true khi đã bỏ.</summary>
        public bool UnmarkOpenPlot()
        {
            if (OpenPlotId < 0 || Sim.State.Plots[OpenPlotId].Tilled) return false;
            int id = OpenPlotId;
            var s = Sim.State.Plots[id].Soil;
            ClosePlotPanel();
            if (!UnmarkAt(new Vector3Int(s.X, s.Y, s.Z))) return false;
            var farming = GetComponent<PlayerFarmActions>();
            if (farming != null && farming.TillState != SelfTillState.None && farming.TillPlotId == id) farming.CancelSelfTill();
            return true;
        }

        /// <summary>Nút "Tự xới" của bảng ô ruộng (ô xới lại): đóng bảng rồi tự đi tới xới. false nếu không tự xới được.</summary>
        public bool SelfTillOpenPlot()
        {
            var farming = GetComponent<PlayerFarmActions>();
            if (OpenPlotId < 0 || !Sim.CanPlayerTill(OpenPlotId) || farming == null) return false;
            int id = OpenPlotId;
            ClosePlotPanel();
            return farming.BeginSelfTill(id);
        }

        /// <summary>Ô hạt giống cropId (0026 mục 2).</summary>
        public bool ChooseCrop(int cropId) => OpenPlotId >= 0 && Sim.EnqueueCrop(OpenPlotId, cropId);
        public bool CanChooseCrop(int cropId) => OpenPlotId >= 0 && Sim.CanEnqueueCrop(OpenPlotId, cropId);
        /// <summary>Nút + / − / x của ô đợi index.</summary>
        public bool IncreaseQueued(int index) => OpenPlotId >= 0 && Sim.IncreaseSlot(OpenPlotId, index);
        public bool DecreaseQueued(int index) => OpenPlotId >= 0 && Sim.DecreaseSlot(OpenPlotId, index);
        public bool ClearQueued(int index) => OpenPlotId >= 0 && Sim.ClearSlot(OpenPlotId, index);

        /// <summary>Nút trên ô tiến trình (0027): xới lại → tạm dừng / tiếp tục; từ gieo trở đi → huỷ vụ (không trả hạt).</summary>
        public bool ProgressAction()
        {
            if (OpenPlotId < 0) return false;
            var p = Sim.State.Plots[OpenPlotId];
            if (p.Tilled && p.Phase == PlotPhase.Till)
                return p.TillPaused ? Sim.ResumeTill(OpenPlotId) : Sim.PauseTill(OpenPlotId);
            return Sim.CancelCurrent(OpenPlotId);
        }

        /// <summary>Đóng bảng; ô đang tạm dừng xới → tiếp tục (0027 mục 2).</summary>
        public void ClosePlotPanel()
        {
            if (OpenPlotId >= 0) Sim.ResumeTill(OpenPlotId);
            OpenPlotId = -1;
        }
        readonly List<Collider> storageColliders = new List<Collider>();

        static string T(string k) => GameSettings.Text.Get(k);
        static string Format(string k, params object[] a) => GameSettings.Text.Format(k, a);

        /// <summary>Chỉ số kho có collider c (hộp kho); -1 nếu không phải.</summary>
        public int StorageIdOf(Collider c) => c == null ? -1 : storageColliders.IndexOf(c);

        /// <summary>Click khối kho index (brief 0042): trong tầm thao tác (hoặc không có nhân vật) → mở kho (InventoryView, mở kèm túi), true.</summary>
        public bool OpenStockAt(int index)
        {
            var player = PlayerCharacter.Active;
            if (player != null)
            {
                var f = player.FeetLocal;
                var cell = Sim.State.Storages[index].Cell;
                if (!PlayerRules.InTargetReach(f.x, f.y, f.z, cell.X, cell.Y, cell.Z)) return false;
            }
            var inv = InventoryView.Active;
            return inv != null && inv.OpenStock(index);
        }

        static float GuiScale => UiScale.Gui;
        public static Rect PlotPanelRect(float gw, float gh) => new Rect(gw * 0.5f - 200f, gh - 310f, 400f, 300f);

        static SimView active;

        /// <summary>Con trỏ (toạ độ màn hình, gốc dưới-trái) trên bảng ô ruộng → công cụ không dùng (brief 0030 mục 12).</summary>
        public static bool BlocksPointer(Vector2 screen) => SettingsView.BlocksPointer(screen) || (active != null && active.Sim != null && active.Covers(screen));

        /// <summary>Bảng ô ruộng, hoặc túi / kho / bảng thông tin (InventoryView) đang mở (Shift lướt bị bỏ qua, brief 0055).</summary>
        public static bool AnyPanelOpen => (active != null && active.OpenPlotId >= 0) || InventoryView.AnyOpen;

        public bool Covers(Vector2 screen)
        {
            float s = GuiScale, gw = Screen.width / s, gh = Screen.height / s;
            var g = new Vector2(screen.x / s, (Screen.height - screen.y) / s);
            return OpenPlotId >= 0 && PlotPanelRect(gw, gh).Contains(g);
        }

        void DrawFarmGui(float scale)
        {
            float gw = Screen.width / scale, gh = Screen.height / scale;
            if (OpenPlotId < 0) return;
            var pr = PlotPanelRect(gw, gh);
            GUI.Box(pr, "");
            var p = Sim.State.Plots[OpenPlotId];
            var cfg = Sim.Config;
            float x = pr.x + 10f, y = pr.y + 8f;
            GUI.Label(new Rect(x, y, pr.width - 20f, 24f), SiteStatus.PlotText(GameSettings.Text, p, Sim.State.Tick, cfg.GrowTicks(p.CurrentCrop >= 0 ? p.CurrentCrop : 0)), hudStyle);
            y += 26f;

            // Hạt giống: ô vuông bo góc màu cây + chữ, icon hạt góc trên trái.
            GUI.Label(new Rect(x, y, 200f, 22f), T("plot.seed_title"), hudStyle);
            y += 22f;
            for (int i = 0; i < cfg.crops.Length; i++)
            {
                var r = new Rect(x + i * 64f, y, 56f, 56f);
                bool can = CanChooseCrop(i);
                Tile(r, CropColor(i), cfg.crops[i].id, can ? 1f : 0.35f);
                Tile(new Rect(r.x + 4f, r.y + 4f, 14f, 14f), SeedColor, "", can ? 1f : 0.35f, 7f);
                GUI.enabled = can;
                if (GUI.Button(r, GUIContent.none, GUIStyle.none)) ChooseCrop(i);
                GUI.enabled = true;
            }
            y += 64f;

            // Ô tiến trình (trái) + các ô đợi (phải).
            GUI.Label(new Rect(x, y, 90f, 22f), T("plot.working"), hudStyle);
            GUI.Label(new Rect(x + 100f, y, 200f, 22f), T("plot.queue"), hudStyle);
            y += 22f;
            var cr = new Rect(x, y, 72f, 72f);
            bool busy = p.Phase != PlotPhase.Idle;
            string cropText = p.CurrentCrop >= 0 ? cfg.crops[p.CurrentCrop].id : (busy ? T("plot.till_tile") : "");
            Tile(cr, p.CurrentCrop >= 0 ? CropColor(p.CurrentCrop) : EmptyColor, cropText, 1f);
            if (busy)
            {
                if (p.TillPaused)
                {
                    if (GUI.Button(new Rect(cr.x + 4f, cr.y + 25f, 64f, 22f), T("plot.resume"))) ProgressAction();
                }
                else if (GUI.Button(new Rect(cr.xMax - 18f, cr.y, 18f, 18f), "x")) ProgressAction();
            }
            var bar = new Rect(cr.x, cr.yMax + 4f, 72f, 8f);
            Tile(bar, EmptyColor, "", 1f, 3f);
            int pct = SiteStatus.ProgressPercent(p, Sim.State, cfg);
            if (pct > 0) Tile(new Rect(bar.x, bar.y, bar.width * pct / 100f, bar.height), BarColor, "", 1f, 3f);
            GUI.Label(new Rect(cr.x - 4f, bar.yMax + 2f, 96f, 22f), SiteStatus.ProgressText(GameSettings.Text, p, Sim.State, cfg), hudStyle);

            for (int k = 0; k < p.SlotCount; k++)
            {
                var r = new Rect(x + 100f + k * 84f, y, 72f, 72f);
                bool has = k < p.Slots.Count;
                Tile(r, has ? CropColor(p.Slots[k].CropId) : EmptyColor, has ? cfg.crops[p.Slots[k].CropId].id : "", 1f);
                float by = r.yMax + 4f;
                GUI.enabled = has;
                if (GUI.Button(new Rect(r.xMax - 18f, r.y, 18f, 18f), "x")) { ClearQueued(k); GUI.enabled = true; break; }
                if (GUI.Button(new Rect(r.x, by, 20f, 20f), "−")) { DecreaseQueued(k); GUI.enabled = true; break; }
                GUI.enabled = has && p.Slots[k].Count < p.SlotMax;
                if (GUI.Button(new Rect(r.xMax - 20f, by, 20f, 20f), "+")) { IncreaseQueued(k); GUI.enabled = true; break; }
                GUI.enabled = true;
                GUI.Label(new Rect(r.x + 22f, by, 28f, 20f), SiteStatus.SlotCountText(p, k), labelStyle);
            }
            GUI.enabled = true;

            GUI.enabled = Sim.CanPlayerTill(OpenPlotId) && PlayerCharacter.Active != null;
            if (GUI.Button(new Rect(pr.x + 10f, pr.yMax - 36f, 110f, 28f), T("plot.self_till"))) SelfTillOpenPlot();
            GUI.enabled = !p.Tilled;
            if (GUI.Button(new Rect(pr.x + 130f, pr.yMax - 36f, 120f, 28f), T("plot.unmark"))) UnmarkOpenPlot();
            GUI.enabled = true;
            if (OpenPlotId < 0) return;
            if (GUI.Button(new Rect(pr.xMax - 120f, pr.yMax - 36f, 110f, 28f), T("plot.close"))) ClosePlotPanel();
        }

        static readonly Color EmptyColor = new Color(0.25f, 0.25f, 0.25f, 0.9f);
        static readonly Color SeedColor = new Color(0.55f, 0.40f, 0.20f);
        static readonly Color BarColor = new Color(0.35f, 0.80f, 0.35f);

        /// <summary>Màu tạm của loại cây (0027 mục 5 — art sau).</summary>
        static Color CropColor(int cropId) =>
            cropId == 0 ? new Color(0.35f, 0.70f, 0.30f) : cropId == 1 ? new Color(0.85f, 0.55f, 0.20f) : new Color(0.6f, 0.6f, 0.6f);

        /// <summary>Ô màu bo góc + chữ giữa (hình tạm, OnGUI).</summary>
        void Tile(Rect r, Color c, string text, float alpha, float radius = 8f)
        {
            c.a *= alpha;
            GUI.DrawTexture(r, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0f, c, 0f, radius);
            if (text.Length > 0) GUI.Label(r, text, labelStyle);
        }

        void UpdateSprouts()
        {
            var ps = Sim.State.Plots;
            for (int i = 0; i < ps.Count && i < sprouts.Count; i++)
            {
                var p = ps[i];
                int grow = Sim.Config.GrowTicks(p.CurrentCrop >= 0 ? p.CurrentCrop : 0);
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
            var body = new GameObject("Creature " + c.Id + " " + c.SpeciesId).transform;
            body.SetParent(root, false);
            Box("Body", Vector3.zero, new Vector3(c.SizeX * 0.8f, c.SizeY * 0.8f, c.SizeZ * 0.8f), SpeciesColor(c.SpeciesId), body);
            var pivot = new GameObject("Facing").transform;
            pivot.SetParent(body, false);
            Box("Nose", new Vector3(0f, c.SizeY * 0.15f, Mathf.Max(c.SizeX, c.SizeZ) * 0.4f + 0.1f), new Vector3(0.25f, 0.25f, 0.3f), NoseColor, pivot);
            var load = Box("Load", new Vector3(0f, c.SizeY * 0.4f + 0.15f, 0f), new Vector3(0.35f, 0.3f, 0.35f), LoadColor, body);
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

        /// <summary>World-space labels (creature status, red "!" markers) are hidden while the Settings panel is open so they do not draw over it.</summary>
        public static bool WorldLabelsVisible => !SettingsView.IsAnyOpen;

        GUIStyle statsStyle;

        /// <summary>Thông số trên cùng giữa: FPS đo / mục tiêu, nhịp sim, dựng lại chunk (khi có).</summary>
        public string StatsText()
        {
            var text = Format("hud.fps", TargetFps, 1f / smoothedDt, Sim.State.Tick);
            if (view.LastRebuildFrame >= 0)
                text += Format("hud.rebuild", view.LastRebuildChunks, view.LastMeshMs, view.LastWriteMs, view.LastBakeMs);
            return text;
        }

        void OnGUI()
        {
            if (Sim == null)
                return;
            float scale = UiScale.Gui;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            if (labelStyle == null)
            {
                labelStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, richText = true };
                hudStyle = new GUIStyle(GUI.skin.label) { richText = true };
            }
            if (!MinimapView.IsAnyOpen)
            {
                float gw = Screen.width / scale;
                statsStyle ??= new GUIStyle(hudStyle) { alignment = TextAnchor.UpperCenter };
                GUI.Label(new Rect(gw * 0.5f - 400f, 4f, 800f, 22f), StatsText(), statsStyle);
                if (Meter != null && Meter.LastLine != null)
                    GUI.Label(new Rect(gw * 0.5f - 700f, 26f, 1400f, 22f), Meter.LastLine, statsStyle);
            }

            if (!MinimapView.IsAnyOpen)
                DrawFarmGui(scale);

            var cam = Camera.main;
            if (cam == null || !WorldLabelsVisible)
                return;
            var cs = Sim.State.Creatures;
            for (int i = 0; i < cs.Count && i < bodies.Count; i++)
                DrawLabel(cam, bodies[i].position + Vector3.up * (cs[i].SizeY * 0.4f + 0.35f), CreatureStatus.Text(GameSettings.Text, Sim.State, cs[i]), scale);
            foreach (var e in markers.Active)
                DrawLabel(cam, transform.TransformPoint(new Vector3(e.Cell.X + 0.5f, e.Cell.Y + 1.3f, e.Cell.Z + 0.5f)),
                    "<color=red><b>! " + CreatureStatus.ReasonText(GameSettings.Text, e.Reason) + "</b></color>", scale);
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
