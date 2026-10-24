using System.Collections.Generic;
using Game.Core;
using Game.Core.Sim;
using Game.Core.View;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Client
{
    /// <summary>Icon trên world (0034 mục 7, brief 0065): icon + vòng tiến trình trên ô ruộng, icon trạng thái sinh vật, "!" đình trệ tại ô. Một canvas Overlay dựng bằng code, pool phần tử, chỉ dựng lại hình khi icon đổi.</summary>
    [RequireComponent(typeof(SimView))]
    public class WorldIconsView : MonoBehaviour
    {
        static readonly Color DiskBg = new Color(0f, 0f, 0f, 0.55f);
        static readonly Color StallBg = new Color(0.35f, 0f, 0f, 0.75f);
        static readonly Color RingBack = new Color(1f, 1f, 1f, 0.2f);
        static readonly Color RingFront = new Color(0.35f, 0.8f, 0.35f, 1f);

        sealed class Item
        {
            public RectTransform Root;
            public TimeDialGraphic Bg, RingBg, RingFg, Icon;
            public WorldIcon Last;
            public bool HasLast, Active = true;
        }

        SimView sim;
        GameObject canvasGo;
        RectTransform canvasRect;
        readonly List<Item> plots = new List<Item>();
        readonly List<Item> creatures = new List<Item>();
        readonly List<Item> stalls = new List<Item>();

        public bool IsReady => canvasGo != null;
        public bool CanvasActive => canvasGo != null && canvasGo.activeSelf;
        public int PlotItemCount => plots.Count;
        public int CreatureItemCount => creatures.Count;
        public int VisiblePlotIcons { get; private set; }
        public int VisibleStallIcons { get; private set; }
        public WorldIcon PlotIcon(int plotId) => plots[plotId].Last;
        public float PlotRingFill(int plotId) => plots[plotId].RingFg.fill;
        public TimeDialShape PlotShape(int plotId) => plots[plotId].Icon.shape;
        public WorldIcon CreatureIcon(int i) => creatures[i].Last;

        void Awake() => sim = GetComponent<SimView>();

        void OnDestroy()
        {
            if (canvasGo != null)
                Destroy(canvasGo);
        }

        void Build()
        {
            canvasGo = new GameObject("World Icons", typeof(RectTransform));
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 4;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 1f;
            canvasRect = (RectTransform)canvasGo.transform;
        }

        Item NewItem(float size, string name)
        {
            var it = new Item();
            var half = new Vector2(0.5f, 0.5f);
            it.Root = NewRect(name, canvasRect, size);
            it.Bg = Add(NewRect("Bg", it.Root, size), TimeDialShape.Disk, DiskBg);
            it.RingBg = Add(NewRect("RingBg", it.Root, size), TimeDialShape.Ring, RingBack);
            it.RingFg = Add(NewRect("RingFg", it.Root, size), TimeDialShape.Ring, RingFront);
            it.Icon = Add(NewRect("Icon", it.Root, size * 0.68f), TimeDialShape.Disk, Color.white);
            return it;
        }

        static RectTransform NewRect(string name, Transform parent, float size)
        {
            var rt = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(size, size);
            return rt;
        }

        static TimeDialGraphic Add(RectTransform rt, TimeDialShape shape, Color c)
        {
            var g = rt.gameObject.AddComponent<TimeDialGraphic>();
            g.shape = shape;
            g.color = c;
            g.raycastTarget = false;
            return g;
        }

        static TimeDialShape ShapeOf(WorldIconKind k) => k switch
        {
            WorldIconKind.Till => TimeDialShape.Hoe,
            WorldIconKind.Sow => TimeDialShape.Seed,
            WorldIconKind.Water => TimeDialShape.Drop,
            WorldIconKind.Grow => TimeDialShape.Sprout,
            WorldIconKind.Ripe => TimeDialShape.Disk,
            WorldIconKind.Harvest => TimeDialShape.Disk,
            WorldIconKind.Haul => TimeDialShape.Crate,
            WorldIconKind.Stall => TimeDialShape.Bang,
            _ => TimeDialShape.Dots,
        };

        static Color ColorOf(WorldIcon i) => i.Kind switch
        {
            WorldIconKind.Till => new Color(0.80f, 0.65f, 0.45f),
            WorldIconKind.Sow => new Color(0.55f, 0.40f, 0.20f),
            WorldIconKind.Water => new Color(0.35f, 0.65f, 1f),
            WorldIconKind.Grow => SimView.SproutColor,
            WorldIconKind.Ripe => SimView.CropColor(i.Crop),
            WorldIconKind.Empty => new Color(0.7f, 0.7f, 0.7f),
            WorldIconKind.Harvest => new Color(0.95f, 0.85f, 0.25f),
            WorldIconKind.Haul => SimView.LoadColor,
            WorldIconKind.Stall => new Color(1f, 0.25f, 0.2f),
            _ => new Color(0.8f, 0.8f, 0.8f),
        };

        static void Apply(Item it, WorldIcon icon)
        {
            if (it.HasLast && it.Last == icon)
                return;
            it.Last = icon;
            it.HasLast = true;
            it.Icon.SetShape(ShapeOf(icon.Kind));
            var c = ColorOf(icon);
            if (icon.Dim) c.a = 0.5f;
            it.Icon.color = c;
            it.Bg.color = icon.Kind == WorldIconKind.Stall ? StallBg : DiskBg;
            bool ring = icon.Percent >= 0;
            it.RingBg.gameObject.SetActive(ring);
            it.RingFg.gameObject.SetActive(ring);
            if (ring)
                it.RingFg.SetFill(icon.Percent / 100f);
        }

        void SetActive(Item it, bool on)
        {
            if (it.Active == on)
                return;
            it.Active = on;
            it.Root.gameObject.SetActive(on);
        }

        bool Place(Item it, Camera cam, Vector3 world)
        {
            var sp = cam.WorldToScreenPoint(world);
            bool visible = sp.z > 0f && Vector3.Distance(cam.transform.position, world) <= PlayerMotion.IconMaxCells
                && sp.x > -64f && sp.x < Screen.width + 64f && sp.y > -64f && sp.y < Screen.height + 64f;
            SetActive(it, visible);
            if (!visible)
                return false;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, sp, null, out var local);
            it.Root.anchoredPosition = local;
            return true;
        }

        void LateUpdate()
        {
            if (sim.Sim == null)
                return;
            if (canvasGo == null)
                Build();
            var cam = Camera.main;
            bool show = SimView.WorldLabelsVisible && !MinimapView.IsAnyOpen && cam != null;
            if (canvasGo.activeSelf != show)
                canvasGo.SetActive(show);
            if (!show)
            {
                VisiblePlotIcons = 0;
                VisibleStallIcons = 0;
                return;
            }
            var state = sim.Sim.State;
            var cfg = sim.Sim.Config;
            var tr = sim.transform;

            while (plots.Count < state.Plots.Count)
                plots.Add(NewItem(PlayerMotion.PlotIconPx, "Plot " + plots.Count));
            int vis = 0;
            for (int i = 0; i < state.Plots.Count; i++)
            {
                var p = state.Plots[i];
                var icon = WorldIcons.ForPlot(p, state, cfg);
                var it = plots[i];
                if (icon.Kind == WorldIconKind.None) { SetActive(it, false); continue; }
                Apply(it, icon);
                if (Place(it, cam, tr.TransformPoint(new Vector3(p.Soil.X + 0.5f, p.Soil.Y + 2.3f, p.Soil.Z + 0.5f)))) vis++;
            }
            VisiblePlotIcons = vis;

            while (creatures.Count < state.Creatures.Count)
                creatures.Add(NewItem(PlayerMotion.CreatureIconPx, "Creature " + creatures.Count));
            for (int i = 0; i < state.Creatures.Count; i++)
            {
                var c = state.Creatures[i];
                var icon = WorldIcons.ForCreature(state, c);
                var it = creatures[i];
                if (icon.Kind == WorldIconKind.None || i >= sim.BodyCount) { SetActive(it, false); continue; }
                Apply(it, icon);
                Place(it, cam, sim.Body(i).position + Vector3.up * (c.SizeY * 0.4f + 0.6f));
            }

            int n = 0;
            foreach (var e in sim.Markers.Active)
            {
                if (n >= stalls.Count)
                    stalls.Add(NewItem(PlayerMotion.CreatureIconPx, "Stall " + n));
                var it = stalls[n++];
                Apply(it, WorldIcons.ForStall());
                Place(it, cam, tr.TransformPoint(new Vector3(e.Cell.X + 0.5f, e.Cell.Y + 1.6f, e.Cell.Z + 0.5f)));
            }
            VisibleStallIcons = n;
            for (int i = n; i < stalls.Count; i++)
                SetActive(stalls[i], false);
        }
    }
}
