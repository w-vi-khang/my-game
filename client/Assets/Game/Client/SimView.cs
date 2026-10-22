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
    /// Xới (brief 0030): biển canh tác, bảng ô ruộng chọn giống/hàng đợi, nút Kho (K); trại demo có 1 vụ Cây A ban đầu, sau đó người chơi tự chọn.
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
                    Debug.Log($"PLAYER xuất hiện ở trại ({feet.x:F1}, {feet.y:F1}, {feet.z:F1})");
                }
            }
            Debug.Log($"{(stressBench ? "SIMBENCH" : "SIMVIEW")} {plain}, bỏ {lms.Count - kept.Count} dấu mốc trong đồng bằng");
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
                if (kb.bKey.wasPressedThisFrame) ToggleBag();
                if (BagOpen && kb.escapeKey.wasPressedThisFrame) BagOpen = false;
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
                if (!Plain.HasValue)
                {
                    Debug.LogError("SIMVIEW thiếu đồng bằng (afterGenerate bị ghi đè?)");
                    enabled = false;
                    return;
                }
                var plain = Plain.Value;
                Sim = new Simulation(cfg, view.World, plain.Bounds);
                Sim.AttachEditor(view.Editor);
                SimDemo.Build(Sim, plain);
                Debug.Log($"SIMVIEW trại giữa {plain}, mảng tìm đường {Sim.PathfinderCellCount} ô");
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
            storageColliders.Clear();
            foreach (var s in Sim.State.Storages)
            {
                var box = Box("Kho " + s.Id, new Vector3(s.Cell.X + 0.5f, s.Cell.Y + 0.45f, s.Cell.Z + 0.5f), new Vector3(0.9f, 0.9f, 0.9f), StorageColor, root, solid: true);
                storageColliders.Add(box.GetComponent<Collider>());
            }
            EnsurePlotVisuals();
            UpdateBodies();
            UpdatePlotSigns();
            SetFrameRate(60);
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
        public void ToggleStock() => StockOpen = !StockOpen;

        /// <summary>Một dòng bảng túi (brief 0042). SeedCrop ≥ 0 = dòng túi hạt (click → chế độ gieo), -1 = chỉ xem.</summary>
        public struct BagRow { public string Label; public int SeedCrop; }
        public bool BagOpen { get; private set; }
        public void ToggleBag() => BagOpen = !BagOpen;
        readonly List<BagRow> bagRows = new List<BagRow>();
        readonly List<Collider> storageColliders = new List<Collider>();

        /// <summary>Các dòng bảng túi: khối (theo giá trị BlockMaterial tăng dần), túi hạt, nông sản (theo loại cây); bỏ dòng 0.</summary>
        public static void BagRows(PlayerBag bag, SimConfig cfg, List<BagRow> rows)
        {
            rows.Clear();
            for (int m = 1; m < 256; m++)
            {
                int n = bag.Get((BlockMaterial)m);
                if (n > 0) rows.Add(new BagRow { Label = $"{TerrainEditInput.ItemLabel(ItemId.Block((BlockMaterial)m))} × {n}", SeedCrop = -1 });
            }
            for (int i = 0; i < cfg.crops.Length; i++)
            {
                int n = bag.GetSeeds(i);
                if (n > 0) rows.Add(new BagRow { Label = $"Túi hạt Cây {cfg.crops[i].id} × {n}", SeedCrop = i });
            }
            for (int i = 0; i < cfg.crops.Length; i++)
            {
                int n = bag.GetProduce(i);
                if (n > 0) rows.Add(new BagRow { Label = $"Nông sản Cây {cfg.crops[i].id} × {n}", SeedCrop = -1 });
            }
        }

        /// <summary>Click túi hạt crop: vào chế độ gieo, đóng bảng túi. false nếu không vào được.</summary>
        public bool ChooseSeedFromBag(int crop)
        {
            var farming = GetComponent<PlayerFarmActions>();
            if (farming == null || !farming.EnterSowMode(crop)) return false;
            BagOpen = false;
            return true;
        }

        public static Rect BagButtonRect(float gw, float gh) => new Rect(gw - 120f, gh - 46f, 110f, 36f);
        public static Rect BagPanelRect(float gw, float gh, int rows) { float h = 32f + 26f * Mathf.Max(rows, 1) + 24f; return new Rect(gw - 330f, gh - 56f - h, 320f, h); }

        /// <summary>Chỉ số kho có collider c (hộp kho); -1 nếu không phải.</summary>
        public int StorageIdOf(Collider c) => c == null ? -1 : storageColliders.IndexOf(c);

        /// <summary>Click khối kho index (brief 0042): trong 8 ô (hoặc không có nhân vật) → mở bảng kho, true.</summary>
        public bool OpenStockAt(int index)
        {
            var player = PlayerCharacter.Active;
            if (player != null)
            {
                var f = player.FeetLocal;
                var cell = Sim.State.Storages[index].Cell;
                if (!PlayerRules.InHandReach(f.x, f.y, f.z, cell.X, cell.Y, cell.Z)) return false;
            }
            StockOpen = true;
            return true;
        }

        /// <summary>Có kho trong 8 ô (không có nhân vật → true).</summary>
        public bool CanTransferStock
        {
            get
            {
                var player = PlayerCharacter.Active;
                if (player == null) return true;
                var f = player.FeetLocal;
                return Sim.StorageInReach(f.x, f.y, f.z);
            }
        }

        /// <summary>Dòng kết quả chuyển gần nhất; "" lúc đầu.</summary>
        public string StockMessage { get; private set; } = "";

        /// <summary>Nút chuyển: toBag = kho → túi, ngược lại túi → kho; all = Shift (tất cả), không thì 1. Trả số đã chuyển.</summary>
        public int TransferStock(int crop, bool produce, bool toBag, bool all)
        {
            string kind = produce ? "nông sản" : "hạt";
            string id = Sim.Config.crops[crop].id;
            int amount = all ? int.MaxValue : 1;
            if (!CanTransferStock) { StockMessage = "Lại gần kho (8 ô) để chuyển đồ"; return 0; }
            int n;
            if (toBag)
            {
                n = Sim.TakeFromStock(view.Bag, crop, produce, amount);
                if (n == 0) { StockMessage = $"Kho không có {kind} Cây {id}"; return 0; }
            }
            else
            {
                n = Sim.StoreToStock(view.Bag, crop, produce, amount);
                if (n == 0)
                {
                    int inBag = produce ? view.Bag.GetProduce(crop) : view.Bag.GetSeeds(crop);
                    StockMessage = inBag == 0 ? $"Túi không có {kind} Cây {id}" : "Kho đầy";
                    return 0;
                }
            }
            StockMessage = $"Đã chuyển {n} {kind} Cây {id} → {(toBag ? "túi" : "kho")}";
            return n;
        }

        static float GuiScale => Mathf.Max(1f, Screen.height / 720f);
        public static Rect StockButtonRect(float gw, float gh) => new Rect(10f, gh - 46f, 110f, 36f);
        public static Rect StockPanelRect(float gw, float gh, int crops) { float h = 64f + 50f * crops; return new Rect(10f, gh - 56f - h, 480f, h); }
        public static Rect PlotPanelRect(float gw, float gh) => new Rect(gw * 0.5f - 200f, gh - 310f, 400f, 300f);

        static SimView active;

        /// <summary>Con trỏ (toạ độ màn hình, gốc dưới-trái) trên nút Kho / bảng kho / bảng ô ruộng → công cụ không dùng (brief 0030 mục 12).</summary>
        public static bool BlocksPointer(Vector2 screen) => active != null && active.Sim != null && active.Covers(screen);

        public bool Covers(Vector2 screen)
        {
            float s = GuiScale, gw = Screen.width / s, gh = Screen.height / s;
            var g = new Vector2(screen.x / s, (Screen.height - screen.y) / s);
            if (StockButtonRect(gw, gh).Contains(g)) return true;
            if (StockOpen && StockPanelRect(gw, gh, Sim.Config.crops.Length).Contains(g)) return true;
            if (BagButtonRect(gw, gh).Contains(g)) return true;
            if (BagOpen)
            {
                BagRows(view.Bag, Sim.Config, bagRows);
                if (BagPanelRect(gw, gh, bagRows.Count).Contains(g)) return true;
            }
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
                GUI.Label(new Rect(r.x + 8f, r.y + 4f, r.width - 16f, 24f), SiteStatus.StockPanelText(Sim.State.Stock, Sim.Config).Split('\n')[0], hudStyle);
                var stock = Sim.State.Stock;
                bool shift = Keyboard.current != null && Keyboard.current.shiftKey.isPressed;
                for (int i = 0; i < Sim.Config.crops.Length; i++)
                {
                    float y0 = r.y + 32f + 50f * i;
                    GUI.Label(new Rect(r.x + 8f, y0, r.width - 16f, 24f),
                        $"{SiteStatus.CropName(Sim.Config, i)} · kho: nông sản {stock.Produce[i]} · hạt {stock.Seeds[i]} · túi: nông sản {view.Bag.GetProduce(i)} · hạt {view.Bag.GetSeeds(i)}", hudStyle);
                    GUI.enabled = CanTransferStock;
                    string[] names = { "Hạt → túi", "Hạt → kho", "Nông sản → túi", "Nông sản → kho" };
                    for (int b = 0; b < 4; b++)
                    {
                        if (GUI.Button(new Rect(r.x + 8f + 114f * b, y0 + 24f, 110f, 22f), names[b]))
                            TransferStock(i, b >= 2, b % 2 == 0, shift);
                    }
                    GUI.enabled = true;
                }
                GUI.Label(new Rect(r.x + 8f, r.yMax - 28f, r.width - 16f, 24f),
                    !CanTransferStock ? "Lại gần kho (8 ô) để chuyển đồ" : (StockMessage != "" ? StockMessage : "Mỗi click 1 · Shift + click: tất cả"), hudStyle);
            }
            if (GUI.Button(BagButtonRect(gw, gh), "Túi (B)")) ToggleBag();
            if (BagOpen)
            {
                BagRows(view.Bag, Sim.Config, bagRows);
                var br = BagPanelRect(gw, gh, bagRows.Count);
                GUI.Box(br, "");
                GUI.Label(new Rect(br.x + 8f, br.y + 4f, br.width - 16f, 24f), "Túi", hudStyle);
                if (bagRows.Count == 0) GUI.Label(new Rect(br.x + 8f, br.y + 32f, br.width - 16f, 24f), "Túi trống", hudStyle);
                for (int k = 0; k < bagRows.Count; k++)
                {
                    var rr = new Rect(br.x + 8f, br.y + 32f + 26f * k, br.width - 16f, 24f);
                    if (bagRows[k].SeedCrop >= 0)
                    {
                        if (GUI.Button(rr, bagRows[k].Label)) { ChooseSeedFromBag(bagRows[k].SeedCrop); break; }
                    }
                    else GUI.Label(rr, bagRows[k].Label, hudStyle);
                }
                GUI.Label(new Rect(br.x + 8f, br.yMax - 26f, br.width - 16f, 24f), "Click túi hạt để gieo · B / Esc: đóng", hudStyle);
            }
            if (OpenPlotId < 0) return;
            var pr = PlotPanelRect(gw, gh);
            GUI.Box(pr, "");
            var p = Sim.State.Plots[OpenPlotId];
            var cfg = Sim.Config;
            float x = pr.x + 10f, y = pr.y + 8f;
            GUI.Label(new Rect(x, y, pr.width - 20f, 24f), SiteStatus.PlotText(p, Sim.State.Tick, cfg.GrowTicks(p.CurrentCrop >= 0 ? p.CurrentCrop : 0)), hudStyle);
            y += 26f;

            // Hạt giống: ô vuông bo góc màu cây + chữ, icon hạt góc trên trái.
            GUI.Label(new Rect(x, y, 200f, 22f), "Hạt giống", hudStyle);
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
            GUI.Label(new Rect(x, y, 90f, 22f), "Đang làm", hudStyle);
            GUI.Label(new Rect(x + 100f, y, 200f, 22f), "Hàng đợi", hudStyle);
            y += 22f;
            var cr = new Rect(x, y, 72f, 72f);
            bool busy = p.Phase != PlotPhase.Idle;
            string cropText = p.CurrentCrop >= 0 ? cfg.crops[p.CurrentCrop].id : (busy ? "Xới" : "");
            Tile(cr, p.CurrentCrop >= 0 ? CropColor(p.CurrentCrop) : EmptyColor, cropText, 1f);
            if (busy)
            {
                if (p.TillPaused)
                {
                    if (GUI.Button(new Rect(cr.x + 4f, cr.y + 25f, 64f, 22f), "Tiếp tục")) ProgressAction();
                }
                else if (GUI.Button(new Rect(cr.xMax - 18f, cr.y, 18f, 18f), "x")) ProgressAction();
            }
            var bar = new Rect(cr.x, cr.yMax + 4f, 72f, 8f);
            Tile(bar, EmptyColor, "", 1f, 3f);
            int pct = SiteStatus.ProgressPercent(p, Sim.State, cfg);
            if (pct > 0) Tile(new Rect(bar.x, bar.y, bar.width * pct / 100f, bar.height), BarColor, "", 1f, 3f);
            GUI.Label(new Rect(cr.x - 4f, bar.yMax + 2f, 96f, 22f), SiteStatus.ProgressText(p, Sim.State, cfg), hudStyle);

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
            if (GUI.Button(new Rect(pr.x + 10f, pr.yMax - 36f, 110f, 28f), "Tự xới")) SelfTillOpenPlot();
            GUI.enabled = true;
            if (GUI.Button(new Rect(pr.xMax - 120f, pr.yMax - 36f, 110f, 28f), "Đóng (Esc)")) ClosePlotPanel();
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

        public static float StatusTop(float hudBottom) => Mathf.Max(100f, hudBottom + 4f);

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
            float y0 = StatusTop(TerrainEditInput.HudBottom);
            GUI.Label(new Rect(10, y0, 1000, 24),
                $"F1: 30 FPS · F2: 60 FPS — mục tiêu {TargetFps} · đo {1f / smoothedDt:F1} FPS · nhịp {Sim.State.Tick}", hudStyle);
            int line = 1;
            GUI.Label(new Rect(10, y0 + 24 * line++, 1000, 24), SiteStatus.StorageText(Sim.State.Stock), hudStyle);
            if (Meter == null)
                foreach (var p in Sim.State.Plots)
                    GUI.Label(new Rect(10, y0 + 24 * line++, 1000, 24), SiteStatus.PlotText(p, Sim.State.Tick, Sim.Config.GrowTicks(p.CurrentCrop >= 0 ? p.CurrentCrop : 0)), hudStyle);
            if (Meter != null && Meter.LastLine != null)
                GUI.Label(new Rect(10, y0 + 24 * line++, 1400, 24), Meter.LastLine, hudStyle);

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
