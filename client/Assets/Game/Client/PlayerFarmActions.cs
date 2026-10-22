using System.Collections.Generic;
using Game.Core;
using Game.Core.Sim;
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
            RainBox.name = "Mưa";
            Destroy(RainBox.GetComponent<Collider>());
            RainBox.transform.SetParent(transform, false);
            var mat = new UnityEngine.Material(Shader.Find("Game/GhostBlock"));
            mat.SetColor("_BaseColor", new Color(0.55f, 0.75f, 1f, 0.45f));
            RainBox.GetComponent<Renderer>().sharedMaterial = mat;
            RainBox.SetActive(false);
        }

        static Vector3Int ToV(CellPos c) => new Vector3Int(c.X, c.Y, c.Z);

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
                StatusLine = "Tự xới: ô này không cần xới";
                return false;
            }
            var player = PlayerCharacter.Active;
            if (player == null) { StatusLine = "Tự xới: cần nhân vật"; return false; }
            if (player.FlyMode) { StatusLine = "Tự xới: tắt camera bay (F3) trước"; return false; }
            CancelSelfTill();
            var f = player.FeetLocal;
            var feet = new CellPos(Mathf.FloorToInt(f.x), Mathf.FloorToInt(f.y + 0.05f), Mathf.FloorToInt(f.z));
            if (!sim.FindPlayerPathToPlot(feet, plotId, path))
            {
                StatusLine = "Không tới được ô này";
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
            StatusLine = $"Tự xới ô {PlotSoil()}: đang đi tới";
            return true;
        }

        public void CancelSelfTill()
        {
            if (TillState == SelfTillState.None) return;
            if (TillState == SelfTillState.Walking) PlayerCharacter.Active?.StopAutoWalk();
            ResetTill();
            StatusLine = "Huỷ tự xới";
        }

        /// <summary>Vào chế độ gieo loại crop (túi còn hạt). Huỷ tự xới đang chạy. false nếu không có mô phỏng / túi hết hạt loại đó.</summary>
        public bool EnterSowMode(int crop)
        {
            var sim = Sim;
            if (sim == null || view.Bag == null) return false;
            if (view.Bag.GetSeeds(crop) <= 0)
            {
                StatusLine = $"Gieo: túi hết hạt Cây {sim.Config.crops[crop].id}";
                return false;
            }
            CancelSelfTill();
            SowCrop = crop;
            StatusLine = "";
            return true;
        }

        /// <summary>Đang ở chế độ gieo → thoát, StatusLine "Thoát chế độ gieo".</summary>
        public void ExitSowMode()
        {
            if (SowCrop < 0) return;
            SowCrop = -1;
            StatusLine = "Thoát chế độ gieo";
        }

        /// <summary>Chuột trái ở chế độ gieo: gieo 1 hạt túi vào ô ruộng có Soil = cell. true khi đã gieo.</summary>
        public bool SowAt(Vector3Int cell)
        {
            var sim = Sim;
            if (SowCrop < 0 || sim == null || view.Bag == null) return false;
            var id = sim.Config.crops[SowCrop].id;
            var player = PlayerCharacter.Active;
            if (player != null && !player.InToolReach(cell))
            {
                StatusLine = "Gieo: ngoài tầm 8 ô";
                return false;
            }
            int plotId = sim.PlotAt(new CellPos(cell.x, cell.y, cell.z));
            var r = plotId < 0 ? PlayerSowResult.NoPlot : sim.PlayerSow(plotId, SowCrop, view.Bag);
            switch (r)
            {
                case PlayerSowResult.Ok:
                    StatusLine = $"Gieo Cây {id} vào ô {cell}";
                    if (view.Bag.GetSeeds(SowCrop) == 0)
                    {
                        SowCrop = -1;
                        StatusLine += " · hết hạt, thoát chế độ gieo";
                    }
                    return true;
                case PlayerSowResult.NoPlot: StatusLine = "Gieo: chỉ gieo vào ô ruộng đã xới"; break;
                case PlayerSowResult.NotTilled: StatusLine = "Gieo: ô chưa xới xong"; break;
                case PlayerSowResult.HasCrop: StatusLine = "Gieo: ô đã có cây"; break;
                case PlayerSowResult.OtherCrop:
                    StatusLine = $"Ô đang chờ gieo Cây {sim.Config.crops[sim.State.Plots[plotId].CurrentCrop].id}";
                    break;
                case PlayerSowResult.NoSeeds:
                    StatusLine = $"Gieo: túi hết hạt Cây {id}";
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
                StatusLine = "Tự xới: ô đã được xới";
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
                        break;
                    case AutoWalkState.Cancelled:
                        CancelSelfTill();
                        break;
                    case AutoWalkState.Failed:
                        ResetTill();
                        StatusLine = "Không tới được ô này";
                        break;
                }
                return;
            }
            // Tilling
            if (player != null && (player.FlyMode || player.MoveInput.sqrMagnitude > 0.01f || player.JumpInput))
            {
                CancelSelfTill();
                return;
            }
            if (TillTimer.Advance(Time.deltaTime))
            {
                var soil = PlotSoil();
                sim.PlayerTill(TillPlotId);
                ResetTill();
                StatusLine = $"Xới xong ô {soil}";
            }
        }

        public RainResult CastRain(Vector3Int cell)
        {
            var player = PlayerCharacter.Active;
            if (player != null && !player.InToolReach(cell))
            {
                StatusLine = "Mưa: ngoài tầm 8 ô";
                return RainResult.NoEffect;
            }
            if (!RainCooldown.Ready)
            {
                StatusLine = $"Mưa: còn {RainCooldown.RemainingWholeSeconds} giây";
                return RainResult.NoEffect;
            }
            var sim = Sim;
            var r = sim != null
                ? sim.Rain(new CellPos(cell.x, cell.y, cell.z))
                : (view.Editor != null && view.Editor.TryGrow(cell.x, cell.y, cell.z) ? RainResult.Grassed : RainResult.NoEffect);
            if (r == RainResult.NoEffect)
            {
                StatusLine = "Mưa: không có tác dụng — chỉ ô ruộng chờ tưới hoặc ô đất phía trên trống";
                return r;
            }
            StatusLine = r == RainResult.Watered ? $"Mưa: tưới ô ruộng {cell}" : $"Mưa: ô đất {cell} thành cỏ";
            RainCooldown.Start();
            RainBox.transform.localPosition = cell + new Vector3(0.5f, 1.5f, 0.5f);
            RainBox.SetActive(true);
            rainBoxLeft = RainBoxSeconds;
            return r;
        }

        /// <summary>Id ô ruộng chín có Soil = cell, trong tầm 8 ô (có nhân vật); -1 nếu không.</summary>
        public int RipePlotAt(Vector3Int cell)
        {
            var sim = Sim;
            if (sim == null) return -1;
            int id = sim.PlotAt(new CellPos(cell.x, cell.y, cell.z));
            if (id < 0 || !sim.CanPlayerHarvest(id)) return -1;
            var player = PlayerCharacter.Active;
            if (player != null && !player.InToolReach(cell)) return -1;
            return id;
        }

        public bool Harvest(int plotId)
        {
            var sim = Sim;
            if (sim == null) return false;
            int crop = sim.State.Plots[plotId].CurrentCrop;
            if (!sim.PlayerHarvest(plotId, view.Bag)) return false;
            var cd = sim.Config.crops[crop];
            StatusLine = $"Thu hoạch Cây {cd.id}: +{cd.producePerHarvest} nông sản, +{cd.seedsPerHarvest} hạt";
            return true;
        }

        /// <summary>Các dòng HUD của trồng trọt (bước 5).</summary>
        public string HudLines()
        {
            var text = RainCooldown.Ready ? "Mưa (R): sẵn sàng" : $"Mưa (R): {RainCooldown.RemainingWholeSeconds} giây";
            if (SowCrop >= 0 && Sim != null && view.Bag != null)
                text += $"\nGieo Cây {Sim.Config.crops[SowCrop].id} (còn {view.Bag.GetSeeds(SowCrop)}) — chuột phải / Esc để thoát";
            if (TillState == SelfTillState.Tilling)
                text += $"\nĐang xới ô {PlotSoil()}: {(int)TillTimer.Elapsed}/{(int)TillTimer.Duration} giây";
            else if (TillState == SelfTillState.Walking)
                text += $"\nĐang đi tới ô {PlotSoil()}…";
            if (StatusLine != "") text += "\n" + StatusLine;
            var sim = Sim;
            if (sim != null && view.Bag != null)
            {
                for (int i = 0; i < sim.Config.crops.Length; i++)
                {
                    var id = sim.Config.crops[i].id;
                    int n = view.Bag.GetProduce(i);
                    if (n > 0) text += $"\nNông sản Cây {id}: {n}";
                    int s = view.Bag.GetSeeds(i);
                    if (i != StarterBag.Crop && s > 0) text += $"\nTúi hạt Cây {id}: {s}";
                }
            }
            return text;
        }
    }
}
