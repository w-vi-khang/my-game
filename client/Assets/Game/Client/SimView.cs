using System.Collections.Generic;
using Game.Core.Sim;
using Game.Core.View;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

namespace Game.Client
{
    /// <summary>
    /// Scene SimView (sim bước 6, user chọn 2026-10-16): mô phỏng M1 chạy trên map thật với bố trí SimDemo (2 nhà, 1 kho, 4 ô ruộng, L1–L3).
    /// Sinh vật = khối hộp theo khung chiếm chỗ, màu theo loài, mũi chỉ hướng đi; vị trí nội suy giữa 2 nhịp (trễ 1 nhịp).
    /// Nhãn trên đầu = trạng thái; "!" đỏ = đình trệ tại ô. F1 / F2: khoá 30 / 60 FPS (chỉ Game view trong Editor).
    /// HUD góc trái: kho + từng ô ruộng (giai đoạn, % lớn); mầm xanh cao theo %; hộp vàng trên lưng = đang mang hàng (sửa sau user xem 2026-10-16).
    /// Đào/đặt (TerrainEditInput) báo cho mô phỏng qua WorldEditor.CellChanged; ô có sinh vật/nhà/kho không đặt được.
    /// </summary>
    [RequireComponent(typeof(TerrainChunkView))]
    public class SimView : MonoBehaviour
    {
        public int maxTicksPerFrame = 5;

        static readonly Color HouseColor = new Color(0.55f, 0.35f, 0.20f);
        static readonly Color StorageColor = new Color(0.60f, 0.60f, 0.60f);
        static readonly Color PlotColor = new Color(0.30f, 0.18f, 0.10f);
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
        int savedVSync, savedTargetFps;
        float smoothedDt = 1f / 60f;
        GUIStyle labelStyle, hudStyle;

        public Simulation Sim { get; private set; }
        public int TargetFps { get; private set; }
        public int BodyCount => bodies.Count;
        public int SproutCount => sprouts.Count;

        void Awake()
        {
            view = GetComponent<TerrainChunkView>();
            savedVSync = QualitySettings.vSyncCount;
            savedTargetFps = Application.targetFrameRate;
        }

        void OnDestroy()
        {
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
            }
            smoothedDt = smoothedDt * 0.9f + Time.unscaledDeltaTime * 0.1f;

            int n = clock.Advance(Time.deltaTime);
            var events = Sim.State.Events;
            for (int i = 0; i < n; i++)
            {
                SimDemo.KeepFarming(Sim);
                Sim.Tick();
                interp.Capture(Sim.State);
                interp.ApplyEvents(events);
                markers.ApplyEvents(events);
                events.Clear();
            }
            UpdateBodies();
            UpdateSprouts();
        }

        void Setup()
        {
            var cfg = ConfigLoader.LoadSimConfig();
            Sim = new Simulation(cfg, view.World);
            view.Editor.IsOccupied = Sim.IsOccupied;
            view.Editor.CellChanged += Sim.OnCellChanged;
            SimDemo.Build(Sim);

            clock = new SimClock(cfg.ticksPerSecond, maxTicksPerFrame);
            interp = new SimInterpolator();
            interp.Capture(Sim.State);
            markers = new StallMarkers();

            root = new GameObject("Sim").transform;
            root.SetParent(transform, false);
            foreach (var h in Sim.State.Houses)
                Box("Nhà " + h.Id, new Vector3(h.Cell.X + h.SizeX * 0.5f, h.Cell.Y + h.SizeY * 0.5f, h.Cell.Z + h.SizeZ * 0.5f),
                    new Vector3(h.SizeX * 0.98f, h.SizeY * 0.98f, h.SizeZ * 0.98f), HouseColor, root);
            foreach (var s in Sim.State.Storages)
                Box("Kho " + s.Id, new Vector3(s.Cell.X + 0.5f, s.Cell.Y + 0.45f, s.Cell.Z + 0.5f), new Vector3(0.9f, 0.9f, 0.9f), StorageColor, root);
            foreach (var p in Sim.State.Plots)
            {
                Box("Ruộng " + p.Id, new Vector3(p.Soil.X + 0.5f, p.Soil.Y + 1.02f, p.Soil.Z + 0.5f), new Vector3(0.9f, 0.04f, 0.9f), PlotColor, root);
                var sprout = Box("Mầm " + p.Id, Vector3.zero, Vector3.one, SproutColor, root);
                sprout.SetActive(false);
                sprouts.Add(sprout.transform);
            }
            UpdateBodies();
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

        void UpdateSprouts()
        {
            var ps = Sim.State.Plots;
            int grow = Sim.Config.GrowTicks;
            for (int i = 0; i < ps.Count && i < sprouts.Count; i++)
            {
                var p = ps[i];
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

        GameObject Box(string name, Vector3 localPos, Vector3 scale, Color color, Transform parent)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            Destroy(go.GetComponent<Collider>()); // không chặn raycast đào/đặt
            var t = go.transform;
            t.SetParent(parent, false);
            t.localPosition = localPos;
            t.localScale = scale;
            var mat = new UnityEngine.Material(GraphicsSettings.currentRenderPipeline.defaultMaterial);
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
            foreach (var s in Sim.State.Storages)
                GUI.Label(new Rect(10, 100 + 24 * line++, 1000, 24), SiteStatus.StorageText(s), hudStyle);
            foreach (var p in Sim.State.Plots)
                GUI.Label(new Rect(10, 100 + 24 * line++, 1000, 24), SiteStatus.PlotText(p, Sim.State.Tick, Sim.Config.GrowTicks), hudStyle);

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
