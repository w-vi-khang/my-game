using Game.Core;
using Game.Core.Sim;
using UnityEngine;
using Game.World;

namespace Game.Client
{
    /// <summary>
    /// Điều khiển PC trong scene (0033, 0034): phím 1–4 / Alt + lăn chọn loại khối.
    /// World 7l c1 (brief 0058): chế độ D/Q/E/R (ControlState), chuột phải theo RightClick.Resolve trong 10 ô rồi tự đi tới 2 ô (WalkToAct), lớp đỏ + vòng tầm, F Thu 2 ô, click trái ô ruộng → bảng; Tab cầm tạm khối / camera quan sát tới c2.
    /// Tia trúng nhà, kho hay dấu mốc trước địa hình → không có ô chọn (brief 0021). Vật rơi / kho dưới con trỏ là đích của chuột phải (nhặt / mở kho).
    /// Ô chọn = raycast vào collider rồi lùi/tiến nửa ô theo pháp tuyến — mesh mịn nên ở góc có thể lệch 1 ô (chấp nhận ở M1).
    /// </summary>
    [RequireComponent(typeof(TerrainChunkView))]
    public class TerrainEditInput : MonoBehaviour
    {
        public float maxDistance = 200f;

        static readonly BlockMaterial[] Palette =
            { BlockMaterial.Dirt, BlockMaterial.Grass, BlockMaterial.Stone, BlockMaterial.Sand };
        static readonly string[] PaletteKeys = { "item.dirt", "item.grass", "item.stone", "item.sand" };
        static string PaletteName(int i) => T(PaletteKeys[i]);
        static string T(string k) => GameSettings.Text.Get(k);
        static string Format(string k, params object[] a) => GameSettings.Text.Format(k, a);

        TerrainChunkView view;
        MeshRenderer highlightRenderer;
        Transform highlight;
        int selected;
        string lastResult = "";
        SimView simView;
        /// <summary>SimView cùng GameObject đã dựng Sim; null → không có công cụ Xới (scene TerrainView). Tìm lại mỗi lần vì SimView có thể thêm sau Awake.</summary>
        SimView Farm { get { if (simView == null) simView = GetComponent<SimView>(); return simView != null && simView.Sim != null ? simView : null; } }
        public bool TillAvailable => Farm != null;
        public TillResult LastTill { get; private set; }

        /// <summary>Màu bản mờ khi túi hết loại đang chọn.</summary>
        public static readonly Color MissingColor = new Color(1f, 0.2f, 0.2f, 0.5f);
        /// <summary>Chế độ D/Q/E/R + vật đang cầm (0034 mục 3).</summary>
        public ControlState Control { get; } = new ControlState();
        /// <summary>Việc chuột phải / click trái đang đi tới rồi làm.</summary>
        public WalkToAct Walk { get; } = new WalkToAct();
        /// <summary>Màu lớp phủ đỏ lên khối sẽ bị tác động.</summary>
        public static readonly Color HighlightColor = new Color(1f, 0.15f, 0.15f, 0.35f);
        public bool HighlightVisible => highlightRenderer.enabled;
        public Vector3Int HighlightCell { get; private set; }
        public bool ReachRingVisible { get { var p = PlayerCharacter.Active; return p != null && p.Markers.ReachRingVisible; } }
        public BlockMaterial SelectedMaterial => Palette[selected];
        public GameObject Ghost { get; private set; }
        public bool GhostVisible => ghostRenderer.enabled;
        public Vector3Int GhostCell => Vector3Int.RoundToInt(Ghost.transform.localPosition);
        public Color GhostColor { get; private set; }
        MeshRenderer ghostRenderer;
        UnityEngine.Material ghostMat;

        /// <summary>Vẽ vật rơi (brief 0037); thêm trong Awake nếu chưa có.</summary>
        public DropView DropView { get; private set; }
        /// <summary>Chồng vật rơi đang ngắm (trong tầm thao tác); null nếu không có.</summary>
        public DropStack AimedDrop { get; private set; }

        /// <summary>Tự xới / Mưa / thu hoạch (brief 0041); thêm trong Awake nếu chưa có.</summary>
        public PlayerFarmActions Farming { get; private set; }
        /// <summary>Observation camera (brief 0053); added in Awake if missing.</summary>
        public ObservationCamera Observation { get; private set; }
        /// <summary>Id ô ruộng chín đang ngắm (trong tầm); -1 nếu không.</summary>
        public int AimedHarvest { get; private set; } = -1;

        void Awake()
        {
            view = GetComponent<TerrainChunkView>();
            CreateHighlight();
            CreateGhost();
            DropView = GetComponent<DropView>();
            if (DropView == null) DropView = gameObject.AddComponent<DropView>();
            Farming = GetComponent<PlayerFarmActions>();
            if (Farming == null) Farming = gameObject.AddComponent<PlayerFarmActions>();
            Observation = GetComponent<ObservationCamera>();
            if (Observation == null) Observation = gameObject.AddComponent<ObservationCamera>();
        }

        void Update()
        {
            AimedDrop = null;
            AimedHarvest = -1;
            var player = PlayerCharacter.Active;
            if (Observation.Engaged) { Walk.Cancel(player, false); HideAim(); return; } // xem camera quan sát: không công cụ (brief 0053)
            if (Walk.Tick(player)) lastResult = T("farm.unreachable");
            if (GameInput.SelectBlock1.WasPressedThisFrame()) SelectMaterial(0);
            if (GameInput.SelectBlock2.WasPressedThisFrame()) SelectMaterial(1);
            if (GameInput.SelectBlock3.WasPressedThisFrame()) SelectMaterial(2);
            if (GameInput.SelectBlock4.WasPressedThisFrame()) SelectMaterial(3);
            if (player != null && !player.FlyMode)
            {
                if (GameInput.ModeDefault.WasPressedThisFrame()) ToggleMode(ControlMode.Default);
                if (GameInput.ModeDig.WasPressedThisFrame()) ToggleMode(ControlMode.Dig);
                if (GameInput.ModeTill.WasPressedThisFrame()) ToggleMode(ControlMode.Till);
                if (GameInput.ModeRain.WasPressedThisFrame()) ToggleMode(ControlMode.Rain);
            }
            if (GameInput.CycleTool.WasPressedThisFrame()) CycleTempHold();
            if (GameInput.Back.WasPressedThisFrame())
            {
                if (Farming.SowCrop >= 0) Farming.ExitSowMode();
                else if (Control.HasHeld) Control.ClearToDefault();
            }
            if (GameInput.Collect.WasPressedThisFrame()) CollectNearby();

            bool hasPointer = GameInput.HasPointer;
            var pointer = GameInput.PointerPosition;
            // Bản đồ hoặc vòng thời điểm đang mở, hoặc con trỏ trên nút của chúng / bảng trại (brief 0030) → không lăn chọn loại, không đào/đặt (brief 0016, 0024).
            if (hasPointer && (MinimapView.BlocksPointer(pointer) || TimeDial.BlocksPointer(pointer) || SimView.BlocksPointer(pointer) || ObservationCamera.BlocksPointer(pointer)))
            {
                HideAim();
                return;
            }
            if (hasPointer)
            {
                // Alt + lăn chuột đổi loại (lăn không Alt = thu phóng camera nhân vật, user 2026-10-22).
                if (GameInput.CycleBlockModifier.IsPressed())
                {
                    float wheel = GameInput.Zoom.ReadValue<float>();
                    if (wheel > 0.01f) SelectMaterial((selected + Palette.Length - 1) % Palette.Length);
                    else if (wheel < -0.01f) SelectMaterial((selected + 1) % Palette.Length);
                }
            }

            var cam = Camera.main;
            if (!hasPointer || cam == null || view.Editor == null)
                return;

            var ray = cam.ScreenPointToRay(pointer);
            AimedDrop = PickDrop(ray);
            int sid = -1;
            if (Farm != null && Physics.Raycast(ray, out var sh, maxDistance)) sid = Farm.StorageIdOf(sh.collider);
            bool hasCell = Pick(ray, out var digCell, out var placeCell);
            AimedHarvest = hasCell ? Farming.RipePlotAt(digCell) : -1;
            if (GameInput.MoveClick.WasPressedThisFrame())
            {
                Walk.Cancel(player, false);
                if (hasCell && AimedDrop == null && sid < 0) ViewPlotAt(digCell);
            }
            var a = Preview(hasCell, digCell, placeCell, AimedDrop, sid);
            ShowAim(a, hasCell, digCell, placeCell);
            // Camera bay (không có nhân vật, hoặc chế độ debug F3): chỉ làm khi dừng (user 2026-10-20).
            if ((player == null || player.FlyMode) && DebugFlyCamera.IsMoveKeyHeld()) return;
            if (GameInput.UseTool.WasPressedThisFrame()) RightClickAt(hasCell, digCell, placeCell, AimedDrop, sid);
        }

        /// <summary>Places the observation camera ObservationHeight cells above the top face of cell, facing like Camera.main (decision 0035); writes the HUD result line. False if no camera, engaged, or cell out of target reach.</summary>
        public bool PlaceObservationCamera(Vector3Int cell)
        {
            var cam = Camera.main;
            if (cam == null) return false;
            var player = PlayerCharacter.Active;
            if (player != null && !player.InTargetReach(cell)) return false;
            var pos = view.transform.TransformPoint(new Vector3(cell.x + 0.5f, cell.y + 1f + PlayerMotion.ObservationHeight, cell.z + 0.5f));
            if (!Observation.Place(pos, cam.transform.rotation)) return false;
            lastResult = Format("edit.camera_placed", cell);
            return true;
        }

        /// <summary>D/Q/E/R (0034 mục 3): thoát gieo, huỷ đi tới (dừng nhân vật), Control.ToggleMode(m). No-op khi đang xem camera quan sát; Till khi không có Sim → no-op.</summary>
        public void ToggleMode(ControlMode m)
        {
            if (Observation != null && Observation.Engaged) return;
            if (m == ControlMode.Till && !TillAvailable) return;
            Farming.ExitSowMode();
            Walk.Cancel(PlayerCharacter.Active, true);
            Control.ToggleMode(m);
        }

        /// <summary>Tab vòng tạm tới c2: không cầm → khối SelectedMaterial → camera quan sát → không cầm. Đang gieo → chỉ thoát gieo. Huỷ tự xới + đi tới (dừng nhân vật). No-op khi đang xem.</summary>
        public void CycleTempHold()
        {
            if (Observation != null && Observation.Engaged) return;
            if (Farming.SowCrop >= 0) { Farming.ExitSowMode(); return; }
            Farming.CancelSelfTill();
            Walk.Cancel(PlayerCharacter.Active, true);
            if (!Control.HasHeld) Control.Hold(ItemId.Block(SelectedMaterial));
            else if (Control.Held.Kind == ItemKind.Block) Control.Hold(ItemId.Tool(ToolItem.ObservationCamera));
            else Control.ClearToDefault();
        }

        /// <summary>Phím 1–4 / Alt + lăn: chọn loại i; đang cầm khối → cầm loại mới.</summary>
        public void SelectMaterial(int i)
        {
            selected = i;
            if (Control.HasHeld && Control.Held.Kind == ItemKind.Block) Control.Hold(ItemId.Block(Palette[i]));
        }

        Vector3Int StorageCell(int sid)
        {
            var c = Farm.Sim.State.Storages[sid].Cell;
            return new Vector3Int(c.X, c.Y, c.Z);
        }

        bool ModeUsableAt(Vector3Int digCell) => Control.Mode switch
        {
            ControlMode.Dig => true,
            ControlMode.Till => Farm != null && Farm.CanTillAt(digCell),
            ControlMode.Rain => true,
            _ => false,
        };

        bool HeldUsableAt(Vector3Int placeCell)
        {
            if (!Control.HasHeld) return false;
            if (Control.Held.Kind == ItemKind.Block)
                return view.Editor != null && view.Editor.CheckPlace(placeCell.x, placeCell.y, placeCell.z, Control.Held.Material) == EditResult.Ok;
            return Control.Held.Kind == ItemKind.Tool;
        }

        bool IsUntilledPlot(Vector3Int cell)
        {
            if (Farm == null) return false;
            int id = Farm.Sim.PlotAt(new CellPos(cell.x, cell.y, cell.z));
            return id >= 0 && !Farm.Sim.State.Plots[id].Tilled;
        }

        /// <summary>Việc chuột phải sẽ làm ở chỗ này (không làm gì). hasCell = Pick trúng địa hình; drop / storageId từ tia (null / -1 nếu không).</summary>
        public RightClickAction Preview(bool hasCell, Vector3Int digCell, Vector3Int placeCell, DropStack drop = null, int storageId = -1)
        {
            if (!hasCell && drop == null && storageId < 0) return RightClickAction.None;
            var player = PlayerCharacter.Active;
            var reachCell = drop != null ? new Vector3Int(drop.X, drop.Y, drop.Z) : storageId >= 0 ? StorageCell(storageId) : digCell;
            var t = new RightClickTarget
            {
                InTargetReach = player == null || player.InTargetReach(reachCell),
                HeldUsable = hasCell && HeldUsableAt(placeCell),
                ModeUsable = hasCell && ModeUsableAt(digCell),
                RipeCrop = hasCell && Farming.RipePlotAt(digCell) >= 0,
                Storage = storageId >= 0,
                Drop = drop != null,
            };
            var a = RightClick.Resolve(Control, t);
            if (a == RightClickAction.None && hasCell && Farming.SowCrop >= 0 && !Control.HasHeld) a = RightClickAction.UseHeld; // chế độ gieo cũ = cầm hạt (c2 thay)
            return a;
        }

        /// <summary>Chuột phải: Preview rồi làm ngay hoặc đi tới 2 ô rồi làm (WalkToAct). Trả việc đã chọn; None / OutOfReach → không làm gì, không đổi dòng kết quả.</summary>
        public RightClickAction RightClickAt(bool hasCell, Vector3Int digCell, Vector3Int placeCell, DropStack drop = null, int storageId = -1)
        {
            if (Observation != null && Observation.Engaged) return RightClickAction.None;
            var a = Preview(hasCell, digCell, placeCell, drop, storageId);
            if (a == RightClickAction.None || a == RightClickAction.OutOfReach) return a;
            var player = PlayerCharacter.Active;
            if (Farming.TillState != SelfTillState.None) Farming.CancelSelfTill();
            bool camera = Control.HasHeld && Control.Held.Kind == ItemKind.Tool;
            var material = Control.HasHeld && Control.Held.Kind == ItemKind.Block ? Control.Held.Material : SelectedMaterial;
            if (a == RightClickAction.Till && IsUntilledPlot(digCell))
            {
                Walk.Cancel(player, false);
                Execute(a, digCell, placeCell, drop, storageId, camera, material); // tự xới tự tìm đường (brief 0041)
                return a;
            }
            var cell = a == RightClickAction.PlaceBlock ? placeCell
                : drop != null && a == RightClickAction.PickDrop ? new Vector3Int(drop.X, drop.Y, drop.Z)
                : storageId >= 0 && a == RightClickAction.OpenStorage ? StorageCell(storageId) : digCell;
            Walk.Begin(player, cell, a == RightClickAction.Dig, () => Execute(a, digCell, placeCell, drop, storageId, camera, material));
            return a;
        }

        void Execute(RightClickAction a, Vector3Int digCell, Vector3Int placeCell, DropStack drop, int storageId, bool camera, BlockMaterial material)
        {
            switch (a)
            {
                case RightClickAction.Dig: DoDig(digCell); break;
                case RightClickAction.Till: DoTill(digCell); break;
                case RightClickAction.Rain: Farming.CastRain(digCell); break;
                case RightClickAction.Harvest:
                    int id = Farming.RipePlotAt(digCell);
                    if (id >= 0) Farming.Harvest(id);
                    break;
                case RightClickAction.OpenStorage:
                    lastResult = Farm.OpenStockAt(storageId) ? T("edit.open_stock") : Format("edit.stock_far", PlayerRules.TargetReach);
                    break;
                case RightClickAction.PickDrop: CollectDrop(drop); break;
                case RightClickAction.PlaceBlock:
                    var p = view.Place(placeCell, material);
                    lastResult = p == EditResult.Ok ? Format("edit.place_ok", Name(material), placeCell) : Format("edit.place_fail", Name(material), Explain(p));
                    break;
                case RightClickAction.UseHeld:
                    if (camera) PlaceObservationCamera(digCell);
                    else { lastResult = ""; Farming.SowAt(digCell); }
                    break;
            }
        }

        void DoDig(Vector3Int digCell)
        {
            var m = view.World.Size.InBounds(digCell.x, digCell.y, digCell.z)
                ? view.World.GetMaterial(digCell.x, digCell.y, digCell.z) : BlockMaterial.Empty;
            var r = view.Dig(digCell);
            lastResult = r == EditResult.Ok ? Format("edit.dig_ok", digCell, Name(m == BlockMaterial.Farmland ? BlockMaterial.Dirt : m)) : Format("edit.dig_fail", digCell, Explain(r));
        }

        void DoTill(Vector3Int digCell)
        {
            LastTill = Farm != null ? Farm.UseTill(digCell) : TillResult.Invalid;
            lastResult = LastTill switch
            {
                TillResult.Marked => Format("edit.mark", digCell),
                TillResult.SelfTill => "",
                TillResult.OpenedPlot => Format("edit.open_plot", Farm.OpenPlotId),
                _ => Format("edit.till_invalid", digCell),
            };
            if (LastTill == TillResult.SelfTill) Farming.BeginSelfTill(Farm.Sim.PlotAt(new CellPos(digCell.x, digCell.y, digCell.z)));
        }

        /// <summary>Click trái lên ô ruộng ở cell: đi tới 2 ô rồi mở bảng ô ruộng. false nếu không có Sim / không phải ô ruộng.</summary>
        public bool ViewPlotAt(Vector3Int cell)
        {
            if (Farm == null) return false;
            int id = Farm.Sim.PlotAt(new CellPos(cell.x, cell.y, cell.z));
            if (id < 0) return false;
            Walk.Begin(PlayerCharacter.Active, cell, false, () => Farm.OpenPlotPanel(id));
            return true;
        }

        /// <summary>Màu bản mờ của vật liệu: TopColor, alpha 0,5.</summary>
        public static Color GhostColorOf(BlockMaterial m)
        {
            uint c = BlockMesher.TopColor(m);
            return new Color((c & 0xFF) / 255f, (c >> 8 & 0xFF) / 255f, (c >> 16 & 0xFF) / 255f, 0.5f);
        }

        /// <summary>Tắt lớp đỏ, bản mờ, vòng tầm.</summary>
        public void HideAim()
        {
            highlightRenderer.enabled = false;
            ghostRenderer.enabled = false;
            var p = PlayerCharacter.Active;
            if (p != null) p.Markers.ShowReachRing(false);
        }

        /// <summary>Tô / bản mờ / vòng tầm theo việc a (0034 mục 3): Dig / Till / Rain / UseHeld → lớp đỏ ở digCell; PlaceBlock → bản mờ màu vật liệu ở placeCell (đỏ khi túi hết); OutOfReach khi có chế độ / vật cầm → vòng tầm.</summary>
        public void ShowAim(RightClickAction a, bool hasCell, Vector3Int digCell, Vector3Int placeCell)
        {
            HideAim();
            if (view.Editor == null) return;
            var player = PlayerCharacter.Active;
            bool active = Control.Mode != ControlMode.Default || Control.HasHeld || Farming.SowCrop >= 0;
            if (player != null) player.Markers.ShowReachRing(active && a == RightClickAction.OutOfReach);
            switch (a)
            {
                case RightClickAction.Dig:
                case RightClickAction.Till:
                case RightClickAction.Rain:
                case RightClickAction.UseHeld:
                    highlight.localPosition = (Vector3)digCell - Vector3.one * 0.02f;
                    HighlightCell = digCell;
                    highlightRenderer.enabled = true;
                    return;
                case RightClickAction.PlaceBlock:
                    GhostColor = GhostColorOf(Control.Held.Material);
                    ghostMat.SetColor("_BaseColor", GhostColor);
                    Ghost.transform.localPosition = placeCell;
                    ghostRenderer.enabled = true;
                    return;
            }
            if (a == RightClickAction.None && hasCell && Control.HasHeld && Control.Held.Kind == ItemKind.Block
                && (player == null || player.InTargetReach(digCell))
                && view.Editor.CheckPlace(placeCell.x, placeCell.y, placeCell.z, Control.Held.Material) == EditResult.NoMaterial)
            {
                GhostColor = MissingColor;
                ghostMat.SetColor("_BaseColor", GhostColor);
                Ghost.transform.localPosition = placeCell;
                ghostRenderer.enabled = true;
            }
        }

        /// <summary>
        /// Ô sẽ đào / ô sẽ đặt theo tia. Chỉ địa hình cho ô chọn: tia trúng trước vào nhà, kho, dấu mốc (có collider) → false (brief 0021).
        /// </summary>
        public bool Pick(Ray ray, out Vector3Int digCell, out Vector3Int placeCell)
        {
            digCell = placeCell = default;
            if (!Physics.Raycast(ray, out var hit, maxDistance) || !view.IsTerrainCollider(hit.collider))
                return false;
            digCell = view.CellAt(hit.point - hit.normal * 0.5f);
            placeCell = view.CellAt(hit.point + hit.normal * 0.5f);
            return true;
        }

        /// <summary>Chồng vật rơi gần nhất mà tia trúng, trong tầm thao tác 10 ô, không bị collider nào (địa hình, nhà, dấu mốc) che; không có nhân vật → null.</summary>
        public DropStack PickDrop(Ray ray)
        {
            var player = PlayerCharacter.Active;
            if (player == null || view.Editor == null) return null;
            float limit = Physics.Raycast(ray, out var hit, maxDistance) ? hit.distance : maxDistance;
            var o = view.transform.InverseTransformPoint(ray.origin);
            var d = view.transform.InverseTransformDirection(ray.direction);
            var f = player.FeetLocal;
            return DropPick.Pick(view.Editor.Drops.Stacks, o.x, o.y, o.z, d.x, d.y, d.z, limit, f.x, f.y, f.z, out _);
        }

        /// <summary>Phím F (Thu): nhặt mọi chồng trong tầm Thu 2 ô; ghi dòng kết quả. Trả số chồng đã nhặt; không có nhân vật → 0, không đổi dòng kết quả.</summary>
        public int CollectNearby()
        {
            var player = PlayerCharacter.Active;
            if (player == null || view.Editor == null) return 0;
            var f = player.FeetLocal;
            int n = view.Editor.Drops.CollectInReach(f.x, f.y, f.z, view.Bag);
            lastResult = n > 0 ? Format("edit.collect_ok", n) : Format("edit.collect_none", PlayerRules.CollectReach);
            return n;
        }

        /// <summary>Nhặt một chồng (phải trong tầm thao tác); ghi dòng kết quả khi được.</summary>
        public bool CollectDrop(DropStack s)
        {
            var player = PlayerCharacter.Active;
            if (player == null || view.Editor == null || s == null) return false;
            var label = DropLabel(s);
            var f = player.FeetLocal;
            if (!view.Editor.Drops.TryCollect(s.Id, f.x, f.y, f.z, view.Bag)) return false;
            lastResult = Format("edit.pick", label);
            return true;
        }

        /// <summary>Tên vật phẩm: khối → Đất / Cỏ / Đá / Cát (khác: tên enum); túi hạt loại 0 → "Túi hạt Cây A", loại khác → "Túi hạt loại N".</summary>
        public static string ItemLabel(ItemId item)
        {
            if (item.Kind == ItemKind.SeedBag)
                return item.Crop == StarterBag.Crop ? T("item.seed_bag_a") : Format("item.seed_bag_n", item.Crop);
            var n = Name(item.Material);
            return char.ToUpper(n[0]) + n.Substring(1);
        }

        /// <summary>vd "Đất × 3".</summary>
        public static string DropLabel(DropStack s) => Format("item.drop_label", ItemLabel(s.Item), s.Count);

        static string Name(BlockMaterial m)
        {
            int i = System.Array.IndexOf(Palette, m);
            return i >= 0 ? PaletteName(i) : m.ToString();
        }

        static string Explain(EditResult r) => r switch
        {
            EditResult.NoMaterial => T("edit.explain_no_material"),
            EditResult.NotEmpty => T("edit.explain_not_empty"),
            EditResult.NotAttached => T("edit.explain_not_attached"),
            EditResult.NotDiggable => T("edit.explain_not_diggable"),
            EditResult.OutOfBounds => T("edit.explain_out_of_bounds"),
            EditResult.Occupied => T("edit.explain_occupied"),
            EditResult.NotPlaceable => T("edit.explain_not_placeable"),
            _ => r.ToString(),
        };

        static string HelpText()
        {
            var player = PlayerCharacter.Active;
            if (player == null)
                return T("hud.help_idle");
            if (player.FlyMode)
                return Format("hud.help_fly", PlayerRules.TargetReach, PlayerRules.CollectReach);
            return Format("hud.help_walk", PlayerRules.TargetReach, PlayerRules.ActReach, PlayerRules.CollectReach);
        }

        /// <summary>Chữ HUD góc trái (tách khỏi OnGUI để test); "" nếu chưa có túi.</summary>
        public string HudText()
        {
            if (view.Bag == null) return "";
            var text = Format("hud.mode", T(Control.Mode switch { ControlMode.Dig => "hud.mode_dig", ControlMode.Till => "hud.mode_till", ControlMode.Rain => "hud.mode_rain", _ => "hud.mode_default" }))
                + (Control.HasHeld ? Format("hud.held", Control.Held.Kind == ItemKind.Tool ? T("hud.held_camera") : ItemLabel(Control.Held)) : "")
                + HelpText();
            if (Control.Mode == ControlMode.Till)
                text += T("hud.till_help");
            if (Control.HasHeld && Control.Held.Kind == ItemKind.Tool) text += T("hud.camera_help");
            var cam = Camera.main;
            if (cam != null)
                text += Format("hud.elevation", FlyCameraBounds.FormatElevation(FlyCameraBounds.ElevationDegrees(cam.transform.forward.y)));
            text += T("hud.bag");
            for (int i = 0; i < Palette.Length; i++)
                text += i == selected
                    ? Format("hud.bag_sel", i + 1, PaletteName(i), view.Bag.Get(Palette[i]))
                    : Format("hud.bag_item", i + 1, PaletteName(i), view.Bag.Get(Palette[i]));
            int seeds = view.Bag.GetSeeds(StarterBag.Crop);
            if (seeds > 0) text += Format("hud.seed_bag_a", seeds);
            if (AimedDrop != null) text += Format("hud.aimed_drop", DropLabel(AimedDrop));
            if (Farming != null) text += "\n" + Farming.HudLines();
            if (AimedHarvest >= 0) text += Format("hud.aimed_harvest", Game.Core.View.SiteStatus.CropName(GameSettings.Text, Farm.Sim.Config, Farm.Sim.State.Plots[AimedHarvest].CurrentCrop));
            text += $"\n{lastResult}";
            if (view.LastRebuildFrame >= 0)
                text += Format("hud.rebuild", view.LastRebuildChunks, view.LastMeshMs, view.LastWriteMs, view.LastBakeMs);
            return text;
        }

        GUIStyle hudStyle;

        /// <summary>Đáy HUD trái (đơn vị GUI) ở khung vẽ gần nhất; 0 khi không vẽ — SimView đặt chữ trạng thái dưới mốc này (brief 0043).</summary>
        public static float HudBottom { get; private set; }

        void OnGUI()
        {
            if (view.Bag == null || MinimapView.IsAnyOpen)
            {
                HudBottom = 0f;
                return;
            }
            var text = HudText();
            // Phóng chữ theo chiều cao màn hình (mốc 720p) — màn Retina chữ mặc định quá nhỏ.
            float scale = UiScale.Gui;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            hudStyle ??= new GUIStyle(GUI.skin.label) { richText = true };
            float h = hudStyle.CalcHeight(new GUIContent(text), 1000f);
            GUI.Label(new Rect(10, 10, 1000, h), text, hudStyle);
            HudBottom = 10f + h;
        }

        static Mesh CubeMesh()
        {
            var verts = new Vector3[8];
            for (int i = 0; i < 8; i++)
                verts[i] = new Vector3(i & 1, i >> 1 & 1, i >> 2 & 1);
            var tris = new[]
            {
                0, 2, 1, 1, 2, 3, // z = 0
                4, 5, 6, 5, 7, 6, // z = 1
                0, 1, 4, 1, 5, 4, // y = 0
                2, 6, 3, 3, 6, 7, // y = 1
                0, 4, 2, 2, 4, 6, // x = 0
                1, 3, 5, 3, 7, 5, // x = 1
            };
            var mesh = new Mesh { name = "Ghost" };
            mesh.vertices = verts;
            mesh.triangles = tris;
            mesh.RecalculateBounds();
            return mesh;
        }

        void CreateHighlight()
        {
            var go = new GameObject("Highlight");
            go.transform.SetParent(transform, false);
            go.AddComponent<MeshFilter>().sharedMesh = CubeMesh();
            highlightRenderer = go.AddComponent<MeshRenderer>();
            highlightRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var shader = Shader.Find("Game/GhostBlock");
            if (shader == null)
                shader = Shader.Find("Game/SelectionOverlay");
            var mat = new UnityEngine.Material(shader);
            mat.SetColor("_BaseColor", HighlightColor);
            highlightRenderer.sharedMaterial = mat;
            go.transform.localScale = Vector3.one * 1.04f;
            highlightRenderer.enabled = false;
            highlight = go.transform;
        }

        void CreateGhost()
        {
            var go = new GameObject("Ghost");
            go.transform.SetParent(transform, false);
            go.AddComponent<MeshFilter>().sharedMesh = CubeMesh();
            ghostRenderer = go.AddComponent<MeshRenderer>();
            ghostRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var shader = Shader.Find("Game/GhostBlock");
            if (shader == null)
                shader = Shader.Find("Game/SelectionOverlay");
            ghostMat = new UnityEngine.Material(shader);
            ghostRenderer.sharedMaterial = ghostMat;
            ghostRenderer.enabled = false;
            Ghost = go;
        }
    }
}
