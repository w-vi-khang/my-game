using System.Collections.Generic;
using Game.Core.Habitat;
using Game.Core.Sim;
using Game.Core.View;
using UnityEngine;

namespace Game.Client
{
    /// <summary>
    /// Bảng môi trường sống (sim 8b, brief 0074, 0039): phím H mở/đóng; mỗi loài một dòng + từng yêu cầu với số đếm hiện tại (đọc lại mỗi lần vẽ,
    /// đào/đặt đổi ngay). Thông báo "loài đã trở lại" hiện 4 giây dưới dòng FPS kể cả khi bảng đóng.
    /// </summary>
    public class HabitatPanelView : MonoBehaviour
    {
        const float NoticeSeconds = 4f; // [số tạm]
        static HabitatPanelView active;

        SimView sim;
        GUIStyle labelStyle, noticeStyle;
        readonly List<(string text, float until)> notices = new List<(string, float)>();

        public static bool IsAnyOpen => active != null && active.IsOpen;
        public bool IsOpen { get; private set; }

        public string Notice
        {
            get
            {
                var parts = new List<string>();
                for (int i = 0; i < notices.Count; i++)
                    if (notices[i].until > Time.unscaledTime) parts.Add(notices[i].text);
                return string.Join(" · ", parts);
            }
        }

        public static Rect PanelRect(float gw, float gh, int rows) => new Rect(gw - 376f, 230f, 360f, 44f + 24f * rows + 40f);

        public static bool BlocksPointer(Vector2 screen) => active != null && active.Covers(screen);

        public static void ShowReturned(string speciesId)
        {
            if (active == null) return;
            active.notices.Add((Format("habitat.returned", speciesId), Time.unscaledTime + NoticeSeconds));
        }

        public void SetOpen(bool open) => IsOpen = open && sim != null && sim.Habitat != null;

        int RowCount()
        {
            var t = sim.Habitat;
            int rows = t.SpeciesCount;
            for (int s = 0; s < t.SpeciesCount; s++) rows += t.RequirementCount(s);
            return rows;
        }

        bool Hidden => MinimapView.IsAnyOpen || SettingsView.IsAnyOpen;

        public bool Covers(Vector2 screen)
        {
            if (!IsOpen || sim == null || sim.Habitat == null || Hidden) return false;
            float s = UiScale.Gui, gw = Screen.width / s, gh = Screen.height / s;
            var g = new Vector2(screen.x / s, (Screen.height - screen.y) / s);
            return PanelRect(gw, gh, RowCount()).Contains(g);
        }

        void Awake()
        {
            active = this;
            sim = GetComponent<SimView>();
        }

        void OnDestroy()
        {
            if (active == this) active = null;
        }

        void Update()
        {
            if (sim == null || sim.Habitat == null)
            {
                IsOpen = false;
                return;
            }
            if (GameInput.ToggleHabitat.WasPressedThisFrame() && !SettingsView.IsAnyOpen && !MinimapView.IsAnyOpen)
                IsOpen = !IsOpen;
            else if (IsOpen && GameInput.PanelsClose.WasPressedThisFrame())
                IsOpen = false;
        }

        static string T(string k) => GameSettings.Text.Get(k);
        static string Format(string k, params object[] a) => GameSettings.Text.Format(k, a);

        public static string MaterialName(string material) => material switch
        {
            "Grass" => T("item.grass"),
            "Dirt" => T("item.dirt"),
            "Stone" => T("item.stone"),
            "Sand" => T("item.sand"),
            "Water" => T("habitat.mat_water"),
            "Solid" => T("habitat.mat_solid"),
            _ => material,
        };

        public static string RequirementLabel(HabitatRequirement r)
        {
            switch (r.kind)
            {
                case HabitatRequirement.KindCells:
                    var parts = new List<string> { Format("habitat.req_cells", MaterialName(r.material)) };
                    if (r.nearWater) parts.Add(T("habitat.near_water"));
                    if (r.exposed) parts.Add(T("habitat.exposed"));
                    if (r.minY > 0) parts.Add(Format("habitat.min_y", r.minY));
                    return string.Join(" ", parts);
                case HabitatRequirement.KindCrops:
                    return r.cropId < 0 ? T("habitat.req_crops") : Format("habitat.req_crops_n", r.cropId);
                case HabitatRequirement.KindStructures:
                    return r.structure == "house" ? T("habitat.req_house") : T("habitat.req_storage");
                case HabitatRequirement.KindSpecies:
                    return Format("habitat.req_species", r.species);
                case HabitatRequirement.KindPeriod:
                    return Format("habitat.req_period", string.Join(", ", r.periods));
                default:
                    return r.kind;
            }
        }

        /// <summary>Dòng yêu cầu r của loài s (không thẻ màu); đọc tracker ngay lúc gọi.</summary>
        public string RowText(int s, int r)
        {
            var t = sim.Habitat;
            var period = sim.CurrentPeriod;
            var req = sim.HabitatConfig.species[s].requirements[r];
            string text = Format("habitat.row", RequirementLabel(req), t.Current(s, r, sim.Sim.State, period), t.Target(s, r));
            if (t.IsMet(s, r, sim.Sim.State, period)) text += " " + T("habitat.met");
            return text;
        }

        string AbilityText(string speciesId)
        {
            var sp = sim.Sim.Config.FindSpecies(speciesId);
            return CreatureStatus.AbilityName(GameSettings.Text, SimConfig.ParseAbility(sp.ability1)) + ", " +
                   CreatureStatus.AbilityName(GameSettings.Text, SimConfig.ParseAbility(sp.ability2));
        }

        const string Green = "<color=#58d858>", EndColor = "</color>";

        void OnGUI()
        {
            if (sim == null || sim.Habitat == null || Hidden) return;
            float scale = UiScale.Gui;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            float gw = Screen.width / scale, gh = Screen.height / scale;
            labelStyle ??= new GUIStyle(GUI.skin.label) { richText = true };
            noticeStyle ??= new GUIStyle(GUI.skin.label) { richText = true, alignment = TextAnchor.UpperCenter, fontSize = 20, fontStyle = FontStyle.Bold };

            string notice = Notice;
            if (notice.Length > 0) GUI.Label(new Rect(gw * 0.5f - 300f, 40f, 600f, 32f), notice, noticeStyle);
            if (!IsOpen) return;

            var t = sim.Habitat;
            var p = PanelRect(gw, gh, RowCount());
            GUI.Box(p, "");
            float x = p.x + 12f, w = p.width - 24f, y = p.y + 8f;
            GUI.Label(new Rect(x, y, w, 28f), "<b>" + T("habitat.title") + "</b>", labelStyle);
            y += 28f;
            for (int s = 0; s < t.SpeciesCount; s++)
            {
                bool open = t.IsUnlocked(s);
                string status = open ? Green + T("habitat.status_unlocked") + EndColor : T("habitat.status_locked");
                GUI.Label(new Rect(x, y, w, 24f), Format("habitat.species", t.SpeciesId(s), AbilityText(t.SpeciesId(s))) + " · " + status, labelStyle);
                y += 24f;
                for (int r = 0; r < t.RequirementCount(s); r++)
                {
                    string row = RowText(s, r);
                    if (t.IsMet(s, r, sim.Sim.State, sim.CurrentPeriod)) row = Green + row + EndColor;
                    GUI.Label(new Rect(x + 16f, y, w - 16f, 24f), row, labelStyle);
                    y += 24f;
                }
            }
            if (GUI.Button(new Rect(p.xMax - 112f, p.yMax - 36f, 100f, 28f), T("settings.close"))) IsOpen = false;
        }
    }
}
