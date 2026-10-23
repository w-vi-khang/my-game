using System.Collections.Generic;
using Game.Core;
using Game.Core.Sim;
using Game.Core.View;
using UnityEngine;

namespace Game.Client
{
    public enum SelfTillState { None, Walking, Tilling }

    /// <summary>Người chơi tự xới (đi tới + 6 giây), phép Mưa (R, hồi), thu hoạch vào túi, chế độ gieo từ túi hạt (brief 0042) — sim 7f, brief 0041. Luật ở Game.Core (Simulation, PlayerTimers).</summary>
    [RequireComponent(typeof(TerrainChunkView))]
    public class PlayerFarmActions : MonoBehaviour
    {
        public SelfTillState TillState { get; private set; }
        public int TillPlotId { get; private set; } = -1;
        public WorkTimer TillTimer { get; private set; }
        public Cooldown RainCooldown { get; private set; }
        /// <summary>Dòng trạng thái mới nhất (tự xới / Mưa / thu hoạch) cho HUD; "" lúc đầu.</summary>
        public string StatusLine { get; private set; } = "";
        /// <summary>Loại cây đang gieo (chế độ gieo, brief 0042); -1 = không ở chế độ gieo.</summary>
        public int SowCrop { get; private set; } = -1;
        /// <summary>Hộp Mưa (xanh nhạt, 0,5 giây).</summary>
        public GameObject RainBox { get; private set; }

        const float RainBoxSeconds = 0.5f;
        TerrainChunkView view;
        SimView simView;
        float rainBoxLeft;
        readonly List<CellPos> path = new List<CellPos>();

        Simulation Sim
        {
            get
            {
                if (simView == null) simView = GetComponent<SimView>();
                return simView != null ? simView.Sim : null;
            }
        }

        void Awake()
        {
            view = GetComponent<TerrainChunkView>();
            var cfg = ConfigLoader.LoadSimConfig();
            TillTimer = new WorkTimer(cfg.PlayerTillSeconds);
            RainCooldown = new Cooldown(cfg.RainCooldownSeconds);
            RainBox = GameObject.CreatePrimitive(PrimitiveType.Cube);
            RainBox.name = "Rain";
            Destroy(RainBox.GetComponent<Collider>());
            RainBox.transform.SetParent(transform, false);
            var mat = new UnityEngine.Material(Shader.Find("Game/GhostBlock"));
            mat.SetColor("_BaseColor", new Color(0.55f, 0.75f, 1f, 0.45f));
            RainBox.GetComponent<Renderer>().sharedMaterial = mat;
            RainBox.SetActive(false);
        }

        static Vector3Int ToV(CellPos c) => new Vector3Int(c.X, c.Y, c.Z);

        static string T(string k) => GameSettings.Text.Get(k);
        static string Format(string k, params object[] a) => GameSettings.Text.Format(k, a);
        string CropN(int crop) => SiteStatus.CropName(GameSettings.Text, Sim.Config, crop);

        Vector3Int PlotSoil() => ToV(Sim.State.Plots[TillPlotId].Soil);

        void ResetTill()
        {
            TillTimer.Cancel();
            TillState = SelfTillState.None;
            TillPlotId = -1;
        }

        public bool BeginSelfTill(int plotId)
        {
            var sim = Sim;
            if (sim == null || !sim.CanPlayerTill(plotId))
            {
                StatusLine = T("farm.till_not_needed");
                return false;
            }
            var player = PlayerCharacter.Active;
            if (player == null) { StatusLine = T("farm.till_need_player"); return false; }
            if (player.FlyMode) { StatusLine = T("farm.till_fly_off"); return false; }
            CancelSelfTill();
            var f = player.FeetLocal;
            var feet = new CellPos(Mathf.FloorToInt(f.x), Mathf.FloorToInt(f.y + 0.05f), Mathf.FloorToInt(f.z));
            if (!sim.FindPlayerPathToPlot(feet, plotId, path))
            {
                StatusLine = T("farm.unreachable");
                return false;
            }
            TillPlotId = plotId;
            if (path.Count == 0)
            {
                TillTimer.Start();
                TillState = SelfTillState.Tilling;
            }
            else
            {
                player.StartAutoWalk(path);
                TillState = SelfTillState.Walking;
            }
            StatusLine = Format("farm.till_walking", PlotSoil());
            return true;
        }

        public void CancelSelfTill()
        {
            if (TillState == SelfTillState.None) return;
            if (TillState == SelfTillState.Walking) PlayerCharacter.Active?.StopAutoWalk();
            ResetTill();
            StatusLine = T("farm.till_cancel");
        }

        /// <summary>Vào chế độ gieo loại crop (túi còn hạt). Huỷ tự xới đang chạy. false nếu không có mô phỏng / túi hết hạt loại đó.</summary>
        public bool EnterSowMode(int crop)
        {
            var sim = Sim;
            if (sim == null || view.Bag == null) return false;
            if (view.Bag.GetSeeds(crop) <= 0)
            {
                StatusLine = Format("farm.sow_no_seeds", CropN(crop));
                return false;
            }
            CancelSelfTill();
            var edit = GetComponent<TerrainEditInput>();
            if (edit != null) edit.Control.ClearToDefault();
            SowCrop = crop;
            StatusLine = "";
            return true;
        }

        /// <summary>Đang ở chế độ gieo → thoát, StatusLine "Thoát chế độ gieo".</summary>
        public void ExitSowMode()
        {
            if (SowCrop < 0) return;
            SowCrop = -1;
            StatusLine = T("farm.sow_exit");
        }

        /// <summary>Chuột trái ở chế độ gieo: gieo 1 hạt túi vào ô ruộng có Soil = cell. true khi đã gieo.</summary>
        public bool SowAt(Vector3Int cell)
        {
            var sim = Sim;
            if (SowCrop < 0 || sim == null || view.Bag == null) return false;
            var cropName = CropN(SowCrop);
            var player = PlayerCharacter.Active;
            if (player != null && !player.InTargetReach(cell))
            {
                StatusLine = Format("farm.sow_far", PlayerRules.TargetReach);
                return false;
            }
            int plotId = sim.PlotAt(new CellPos(cell.x, cell.y, cell.z));
            var r = plotId < 0 ? PlayerSowResult.NoPlot : sim.PlayerSow(plotId, SowCrop, view.Bag);
            switch (r)
            {
                case PlayerSowResult.Ok:
                    StatusLine = Format("farm.sow_ok", cropName, cell);
                    if (view.Bag.GetSeeds(SowCrop) == 0)
                    {
                        SowCrop = -1;
                        StatusLine += T("farm.sow_last");
                    }
                    return true;
                case PlayerSowResult.NoPlot: StatusLine = T("farm.sow_no_plot"); break;
                case PlayerSowResult.NotTilled: StatusLine = T("farm.sow_not_tilled"); break;
                case PlayerSowResult.HasCrop: StatusLine = T("farm.sow_has_crop"); break;
                case PlayerSowResult.OtherCrop:
                    StatusLine = Format("farm.sow_other", CropN(sim.State.Plots[plotId].CurrentCrop));
                    break;
                case PlayerSowResult.NoSeeds:
                    StatusLine = Format("farm.sow_no_seeds", cropName);
                    SowCrop = -1;
                    break;
            }
            return false;
        }

        void Update()
        {
            RainCooldown.Advance(Time.deltaTime);
            if (RainBox.activeSelf)
            {
                rainBoxLeft -= Time.deltaTime;
                if (rainBoxLeft <= 0f) RainBox.SetActive(false);
            }
            if (TillState == SelfTillState.None) return;

            var sim = Sim;
            var player = PlayerCharacter.Active;
            if (sim == null || sim.State.Plots[TillPlotId].Removed || !sim.CanPlayerTill(TillPlotId))
            {
                if (TillState == SelfTillState.Walking) player?.StopAutoWalk();
                ResetTill();
                StatusLine = T("farm.till_done_by_other");
                return;
            }
            if (TillState == SelfTillState.Walking)
            {
                if (player == null) { ResetTill(); return; }
                switch (player.AutoWalk)
                {
                    case AutoWalkState.Arrived:
                        TillTimer.Start();
                        TillState = SelfTillState.Tilling;
                        StatusLine = "";
                        break;
                    case AutoWalkState.Cancelled:
                        CancelSelfTill();
                        break;
                    case AutoWalkState.Failed:
                        ResetTill();
                        StatusLine = T("farm.unreachable");
                        break;
                }
                return;
            }
            // Tilling
            if (player != null && player.HasMoveIntent)
            {
                CancelSelfTill();
                return;
            }
            if (TillTimer.Advance(Time.deltaTime))
            {
                var soil = PlotSoil();
                sim.PlayerTill(TillPlotId);
                ResetTill();
                StatusLine = Format("farm.till_done", soil);
            }
        }

        public RainResult CastRain(Vector3Int cell)
        {
            var player = PlayerCharacter.Active;
            if (player != null && !player.InTargetReach(cell))
            {
                StatusLine = Format("farm.rain_far", PlayerRules.TargetReach);
                return RainResult.NoEffect;
            }
            if (!RainCooldown.Ready)
            {
                StatusLine = Format("farm.rain_cooldown", RainCooldown.RemainingWholeSeconds);
                return RainResult.NoEffect;
            }
            var sim = Sim;
            var r = sim != null
                ? sim.Rain(new CellPos(cell.x, cell.y, cell.z))
                : (view.Editor != null && view.Editor.TryGrow(cell.x, cell.y, cell.z) ? RainResult.Grassed : RainResult.NoEffect);
            if (r == RainResult.NoEffect)
            {
                StatusLine = T("farm.rain_no_effect");
                return r;
            }
            StatusLine = r == RainResult.Watered ? Format("farm.rain_watered", cell) : Format("farm.rain_grassed", cell);
            RainCooldown.Start();
            RainBox.transform.localPosition = cell + new Vector3(0.5f, 1.5f, 0.5f);
            RainBox.SetActive(true);
            rainBoxLeft = RainBoxSeconds;
            return r;
        }

        /// <summary>Id ô ruộng chín có Soil = cell, trong tầm thao tác (có nhân vật); -1 nếu không.</summary>
        public int RipePlotAt(Vector3Int cell)
        {
            var sim = Sim;
            if (sim == null) return -1;
            int id = sim.PlotAt(new CellPos(cell.x, cell.y, cell.z));
            if (id < 0 || !sim.CanPlayerHarvest(id)) return -1;
            var player = PlayerCharacter.Active;
            if (player != null && !player.InTargetReach(cell)) return -1;
            return id;
        }

        public bool Harvest(int plotId)
        {
            var sim = Sim;
            if (sim == null) return false;
            int crop = sim.State.Plots[plotId].CurrentCrop;
            if (!sim.PlayerHarvest(plotId, view.Bag)) return false;
            var cd = sim.Config.crops[crop];
            StatusLine = Format("farm.harvest", CropN(crop), cd.producePerHarvest, cd.seedsPerHarvest);
            return true;
        }

        /// <summary>Các dòng HUD của trồng trọt (bước 5).</summary>
        public string HudLines()
        {
            var text = RainCooldown.Ready ? T("farm.rain_ready") : Format("farm.rain_hud_cooldown", RainCooldown.RemainingWholeSeconds);
            if (SowCrop >= 0 && Sim != null && view.Bag != null)
                text += Format("farm.hud_sow", CropN(SowCrop), view.Bag.GetSeeds(SowCrop));
            if (TillState == SelfTillState.Tilling)
                text += Format("farm.hud_tilling", PlotSoil(), (int)TillTimer.Elapsed, (int)TillTimer.Duration);
            else if (TillState == SelfTillState.Walking)
                text += Format("farm.hud_walking", PlotSoil());
            if (StatusLine != "") text += "\n" + StatusLine;
            var sim = Sim;
            if (sim != null && view.Bag != null)
            {
                for (int i = 0; i < sim.Config.crops.Length; i++)
                {
                    int n = view.Bag.GetProduce(i);
                    if (n > 0) text += Format("farm.hud_produce", CropN(i), n);
                    int s = view.Bag.GetSeeds(i);
                    if (i != StarterBag.Crop && s > 0) text += Format("farm.hud_seeds", CropN(i), s);
                }
            }
            return text;
        }
    }
}
