using Game.Core;
using Game.Core.Sim;
using UnityEngine;
using Game.World;

namespace Game.Client
{
    /// <summary>
    /// Điều khiển PC trong scene (0033, 0034): phím 1–4 / Alt + lăn chọn loại khối.
    /// World 7l c1 (brief 0058): chế độ D/Q/E/R (ControlState), chuột phải theo RightClick.Resolve trong 10 ô rồi tự đi tới 2 ô (WalkToAct), lớp đỏ, F Thu 2 ô, click trái ô ruộng → bảng.
    /// World 7l c2 (brief 0059): cầm vật phẩm từ bảng túi (khối / túi hạt / camera quan sát, HoldItem), bản mờ đỏ khi sai chỗ, hết → thôi cầm; vòng tầm chỉ khi chuột phải ngoài tầm; F hiện vùng + gió.
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
        PlayerCharacter seenPlayer;
        int seenRespawns;
        SimView simView;
        /// <summary>SimView cùng GameObject đã dựng Sim; null → không có công cụ Xới (scene TerrainView). Tìm lại mỗi lần vì SimView có thể thêm sau Awake.</summary>
        SimView Farm { get { if (simView == null) simView = GetComponent<SimView>(); return simView != null && simView.Sim != null ? simView : null; } }
        public bool TillAvailable => Farm != null;
        public TillResult LastTill { get; private set; }

        /// <summary>Màu bản mờ khi chỗ con trỏ không đặt được khối đang cầm (0034 mục 4).</summary>
        public static readonly Color InvalidColor = new Color(1f, 0.2f, 0.2f, 0.5f);
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
            if (player != seenPlayer) { seenPlayer = player; seenRespawns = player != null ? player.Respawns : 0; }
            else if (player != null && player.Respawns != seenRespawns)
            {
                seenRespawns = player.Respawns;
                Walk.Cancel(player, false);
                Farming.CancelSelfTill();
                lastResult = T("vitals.respawned");
            }
            if (Observation.Engaged) { Walk.Cancel(player, false); HideAim(); return; } // xem camera quan sát: không công cụ (brief 0053)
            if (Walk.Tick(player)) lastResult = T("farm.unreachable");
            DropHeldIfEmpty();
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
            if (GameInput.Back.WasPressedThisFrame() && Control.HasHeld) Control.ClearToDefault();
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

        /// <summary>D/Q/E/R (0034 mục 3): huỷ đi tới (dừng nhân vật), Control.ToggleMode(m). No-op khi đang xem camera quan sát; Till khi không có Sim → no-op.</summary>
        public void ToggleMode(ControlMode m)
        {
            if (Observation != null && Observation.Engaged) return;
            if (m == ControlMode.Till && !TillAvailable) return;
            Walk.Cancel(PlayerCharacter.Active, true);
            Control.ToggleMode(m);
        }

        /// <summary>Cầm vật phẩm (0034 mục 4): khối / túi hạt (túi còn ≥ 1; hạt cần có Sim) hoặc camera quan sát. Huỷ tự xới + đi tới (dừng nhân vật). Khối thuộc bảng 1–4 → đổi loại đang chọn theo. false (không đổi gì) khi đang xem camera quan sát, không có túi, hết, hoặc nông sản.</summary>
        public bool HoldItem(ItemId item)
        {
            if (Observation != null && Observation.Engaged) return false;
            if (!CanHold(item)) return false;
            Farming.CancelSelfTill();
            Walk.Cancel(PlayerCharacter.Active, true);
            if (item.Kind == ItemKind.Block) { int i = System.Array.IndexOf(Palette, item.Material); if (i >= 0) selected = i; }
            Control.Hold(item);
            return true;
        }

        bool CanHold(ItemId item)
        {
            switch (item.Kind)
            {
                case ItemKind.Tool: return item.ToolKind == ToolItem.ObservationCamera;
                case ItemKind.Block: return view.Bag != null && view.Bag.Get(item.Material) > 0;
                case ItemKind.SeedBag: return Farm != null && view.Bag != null && view.Bag.GetSeeds(item.Crop) > 0;
                default: return false;
            }
        }

        /// <summary>Hết khối / hạt đang cầm → thôi cầm, nối "hết …, thôi cầm" vào dòng kết quả (0034 mục 4).</summary>
        void DropHeldIfEmpty()
        {
            if (!Control.HasHeld) return;
            var k = Control.Held.Kind;
            if ((k != ItemKind.Block && k != ItemKind.SeedBag) || CanHold(Control.Held)) return;
            var label = ItemLabel(Control.Held);
            Control.ClearToDefault();
            lastResult += Format("hold.empty", label);
        }

        /// <summary>Phím 1–4 / Alt + lăn: chọn loại i; đang cầm khối → cầm loại mới (túi hết loại đó → không đổi, dòng "Túi không còn…"). Không cầm → chỉ đổi loại đang chọn.</summary>
        public void SelectMaterial(int i)
        {
            if (Control.HasHeld && Control.Held.Kind == ItemKind.Block)
            {
                if (!HoldItem(ItemId.Block(Palette[i]))) lastResult = Format("hold.none", Name(Palette[i]));
                return;
            }
            selected = i;
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

        bool HeldUsableAt(Vector3Int digCell, Vector3Int placeCell)
        {
            if (!Control.HasHeld) return false;
            if (Control.Held.Kind == ItemKind.SeedBag)
            {
                if (Farm == null || view.Bag == null) return false;
                int id = Farm.Sim.PlotAt(new CellPos(digCell.x, digCell.y, digCell.z));
                return id >= 0 && Farm.Sim.CheckPlayerSow(id, Control.Held.Crop, view.Bag) == PlayerSowResult.Ok;
            }
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
                HeldUsable = hasCell && HeldUsableAt(digCell, placeCell),
                ModeUsable = hasCell && ModeUsableAt(digCell),
                RipeCrop = hasCell && Farming.RipePlotAt(digCell) >= 0,
                Storage = storageId >= 0,
                Drop = drop != null,
            };
            return RightClick.Resolve(Control, t);
        }

        /// <summary>Chuột phải: Preview rồi làm ngay hoặc đi tới 2 ô rồi làm (WalkToAct). Trả việc đã chọn; None / OutOfReach → không làm gì, không đổi dòng kết quả.</summary>
        public RightClickAction RightClickAt(bool hasCell, Vector3Int digCell, Vector3Int placeCell, DropStack drop = null, int storageId = -1)
        {
            if (Observation != null && Observation.Engaged) return RightClickAction.None;
            var a = Preview(hasCell, digCell, placeCell, drop, storageId);
            var player = PlayerCharacter.Active;
            if (a == RightClickAction.OutOfReach && player != null) player.Markers.FlashReachRing();
            if (a == RightClickAction.None && hasCell && Control.HasHeld && Control.Held.Kind == ItemKind.SeedBag)
                Farming.SowAt(digCell, Control.Held.Crop); // chỉ để báo lý do (quyết định 6)
            if (a == RightClickAction.None || a == RightClickAction.OutOfReach) return a;
            if (ManaOf(a, out var cost) && !HasMana(cost)) return RightClickAction.None; // thiếu mana: không đi, không làm
            if (Farming.TillState != SelfTillState.None) Farming.CancelSelfTill();
            var held = Control.HasHeld ? Control.Held : default;
            if (a == RightClickAction.Till && IsUntilledPlot(digCell))
            {
                Walk.Cancel(player, false);
                Execute(a, digCell, placeCell, drop, storageId, held); // tự xới tự tìm đường (brief 0041)
                return a;
            }
            var cell = a == RightClickAction.PlaceBlock ? placeCell
                : drop != null && a == RightClickAction.PickDrop ? new Vector3Int(drop.X, drop.Y, drop.Z)
                : storageId >= 0 && a == RightClickAction.OpenStorage ? StorageCell(storageId) : digCell;
            Walk.Begin(player, cell, a == RightClickAction.Dig, () => Execute(a, digCell, placeCell, drop, storageId, held));
            return a;
        }

        /// <summary>Đủ mana cho việc m (không có nhân vật → luôn đủ). Thiếu → thanh nháy + dòng "Không đủ mana" (0034 mục 5).</summary>
        bool HasMana(ManaAction m)
        {
            var p = PlayerCharacter.Active;
            if (p == null || p.Vitals.CanSpend(m)) return true;
            p.FlashMana();
            lastResult = Format("vitals.no_mana", p.Vitals.Cost(m), p.Vitals.Mana);
            return false;
        }

        static void SpendMana(ManaAction m)
        {
            var p = PlayerCharacter.Active;
            if (p != null) p.Vitals.TrySpend(m);
        }

        static bool ManaOf(RightClickAction a, out ManaAction m)
        {
            m = a == RightClickAction.Dig ? ManaAction.Dig : a == RightClickAction.PlaceBlock ? ManaAction.Place : ManaAction.Rain;
            return a == RightClickAction.Dig || a == RightClickAction.PlaceBlock || a == RightClickAction.Rain;
        }

        void Execute(RightClickAction a, Vector3Int digCell, Vector3Int placeCell, DropStack drop, int storageId, ItemId held)
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
                    var material = held.Material;
                    if (!HasMana(ManaAction.Place)) break;
                    var p = view.Place(placeCell, material);
                    if (p == EditResult.Ok) SpendMana(ManaAction.Place);
                    lastResult = p == EditResult.Ok ? Format("edit.place_ok", Name(material), placeCell) : Format("edit.place_fail", Name(material), Explain(p));
                    break;
                case RightClickAction.UseHeld:
                    if (held.Kind == ItemKind.Tool) PlaceObservationCamera(digCell);
                    else if (held.Kind == ItemKind.SeedBag) { lastResult = ""; Farming.SowAt(digCell, held.Crop); }
                    break;
            }
            if (a == RightClickAction.PlaceBlock || a == RightClickAction.UseHeld) DropHeldIfEmpty();
        }

        void DoDig(Vector3Int digCell)
        {
            if (!HasMana(ManaAction.Dig)) return;
            var m = view.World.Size.InBounds(digCell.x, digCell.y, digCell.z)
                ? view.World.GetMaterial(digCell.x, digCell.y, digCell.z) : BlockMaterial.Empty;
            var r = view.Dig(digCell);
            if (r == EditResult.Ok) SpendMana(ManaAction.Dig);
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

        /// <summary>Tắt lớp đỏ, bản mờ.</summary>
        public void HideAim()
        {
            highlightRenderer.enabled = false;
            ghostRenderer.enabled = false;
        }

        /// <summary>Tô / bản mờ theo việc a (0034 mục 3): Dig / Till / Rain / UseHeld → lớp đỏ ở digCell; PlaceBlock → bản mờ màu vật liệu ở placeCell ; cầm khối mà chỗ đó không đặt được → bản mờ đỏ (InvalidColor).</summary>
        public void ShowAim(RightClickAction a, bool hasCell, Vector3Int digCell, Vector3Int placeCell)
        {
            HideAim();
            if (view.Editor == null) return;
            var player = PlayerCharacter.Active;
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
                && view.Editor.CheckPlace(placeCell.x, placeCell.y, placeCell.z, Control.Held.Material) != EditResult.Ok)
            {
                GhostColor = InvalidColor;
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

        /// <summary>Phím F (Thu): nhặt mọi chồng trong tầm Thu 2 ô; hiện vùng 2 ô + gió (0036 mục 6); ghi dòng kết quả. Trả số chồng đã nhặt; không có nhân vật → 0, không đổi dòng kết quả.</summary>
        public int CollectNearby()
        {
            var player = PlayerCharacter.Active;
            if (player == null || view.Editor == null) return 0;
            player.Markers.ShowCollect();
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

        /// <summary>Tên vật phẩm: khối → Đất / Cỏ / Đá / Cát (khác: tên enum); túi hạt loại 0 → "Túi hạt Cây A", loại khác → "Túi hạt loại N"; camera quan sát → Camera quan sát.</summary>
        public static string ItemLabel(ItemId item)
        {
            if (item.Kind == ItemKind.Tool) return T("hud.held_camera");
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
                + (Control.HasHeld ? Format("hud.held", ItemLabel(Control.Held)) : "")
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
            var pc = PlayerCharacter.Active;
            float top = 10f;
            if (pc != null)
            {
                var v = pc.Vitals;
                Bar(10f, v.Hp, v.HpMax, new Color(0.85f, 0.2f, 0.2f, 1f), false);
                Bar(26f, v.Mana, v.ManaMax, new Color(0.25f, 0.5f, 1f, 1f), pc.ManaFlashing);
                GUI.Label(new Rect(176f, 8f, 200f, 16f), Format("vitals.hp", v.Hp, v.HpMax), hudStyle);
                GUI.Label(new Rect(176f, 24f, 200f, 16f), Format("vitals.mana", v.Mana, v.ManaMax), hudStyle);
                top = 44f;
            }
            float h = hudStyle.CalcHeight(new GUIContent(text), 1000f);
            GUI.Label(new Rect(10f, top, 1000f, h), text, hudStyle);
            HudBottom = top + h;
        }

        static void Bar(float y, int value, int max, Color fill, bool flash)
        {
            var bg = flash && Mathf.FloorToInt(Time.time * 10f) % 2 == 0 ? new Color(1f, 1f, 1f, 0.8f) : new Color(0f, 0f, 0f, 0.5f);
            GUI.DrawTexture(new Rect(10f, y, 160f, 12f), Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0f, bg, 0f, 3f);
            float w = max > 0 ? 160f * Mathf.Clamp01((float)value / max) : 0f;
            if (w > 0f) GUI.DrawTexture(new Rect(10f, y, w, 12f), Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0f, fill, 0f, 3f);
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
