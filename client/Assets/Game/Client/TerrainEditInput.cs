using Game.Core;
using Game.Core.Sim;
using UnityEngine;
using Game.World;
using UnityEngine.InputSystem;

namespace Game.Client
{
    /// <summary>Công cụ đang cầm (brief 0027, 0030): Tab xoay Đào → Đặt → Xới (Xới khi có SimView).</summary>
    public enum EditTool { Dig, Place, Till }

    /// <summary>
    /// Đào/đặt bằng chuột trong scene TerrainView (world-tech bước 4, user duyệt 2026-10-13):
    /// Tab đổi công cụ Đào ⇄ Đặt (brief 0027), chuột trái dùng công cụ; phím 1–4 hoặc lăn chuột chọn đất/cỏ/đá/cát.
    /// Đào: khung trắng = ô sẽ đào. Đặt: bản mờ màu vật liệu ở ô sẽ đặt (đỏ = túi hết; sai luật → không có); nhãn góc trái = công cụ, túi đồ, lý do khi không đào/đặt được.
    /// Tia trúng nhà, kho hay dấu mốc trước địa hình → không có ô chọn (brief 0021).
    /// Sim 7e (brief 0037): F = Thu (nhặt mọi vật rơi trong 8 ô); tia chuột trúng vật rơi trong 8 ô → chuột trái nhặt cả chồng thay cho công cụ; vật rơi ngoài 8 ô không chặn tia.
    /// World 7h (brief 0035): có nhân vật (PlayerCharacter.Active) → đào/đặt/xới chỉ trong tầm 8 ô (0031), được dùng khi đang đi; Alt + lăn chọn loại.
    /// Sim 7f (brief 0041): chuột trái lên biển = tự đi tới xới, chuột phải = bỏ đánh dấu; R = phép Mưa; chuột trái lên cây chín (mọi công cụ) = thu hoạch.
    /// Sim 7f (brief 0042): chế độ gieo — chuột trái gieo, chuột phải / Esc / Tab thoát; click khối kho trong 8 ô mở bảng kho.
    /// Ô chọn = raycast vào collider rồi lùi/tiến nửa ô theo pháp tuyến — mesh mịn nên ở góc có thể lệch 1 ô (chấp nhận ở M1).
    /// </summary>
    [RequireComponent(typeof(TerrainChunkView))]
    public class TerrainEditInput : MonoBehaviour
    {
        public float maxDistance = 200f;
        /// <summary>Độ dày nét khung chọn tính bằng pixel màn hình — giữ nguyên dù camera xa hay gần.</summary>
        public float outlinePixels = 3f;

        static readonly BlockMaterial[] Palette =
            { BlockMaterial.Dirt, BlockMaterial.Grass, BlockMaterial.Stone, BlockMaterial.Sand };
        static readonly string[] PaletteKeys = { "item.dirt", "item.grass", "item.stone", "item.sand" };
        static string PaletteName(int i) => T(PaletteKeys[i]);
        static string T(string k) => GameSettings.Text.Get(k);
        static string Format(string k, params object[] a) => GameSettings.Text.Format(k, a);

        TerrainChunkView view;
        LineRenderer outline;
        int selected;
        string lastResult = "";
        SimView simView;
        /// <summary>SimView cùng GameObject đã dựng Sim; null → không có công cụ Xới (scene TerrainView). Tìm lại mỗi lần vì SimView có thể thêm sau Awake.</summary>
        SimView Farm { get { if (simView == null) simView = GetComponent<SimView>(); return simView != null && simView.Sim != null ? simView : null; } }
        public bool TillAvailable => Farm != null;
        public TillResult LastTill { get; private set; }

        /// <summary>Màu bản mờ khi túi hết loại đang chọn.</summary>
        public static readonly Color MissingColor = new Color(1f, 0.2f, 0.2f, 0.5f);
        public EditTool Tool { get; private set; } = EditTool.Dig;
        public BlockMaterial SelectedMaterial => Palette[selected];
        public GameObject Ghost { get; private set; }
        public bool OutlineVisible => outline.enabled;
        public bool GhostVisible => ghostRenderer.enabled;
        public Vector3Int GhostCell => Vector3Int.RoundToInt(Ghost.transform.localPosition);
        public Color GhostColor { get; private set; }
        MeshRenderer ghostRenderer;
        UnityEngine.Material ghostMat;

        /// <summary>Vẽ vật rơi (brief 0037); thêm trong Awake nếu chưa có.</summary>
        public DropView DropView { get; private set; }
        /// <summary>Chồng vật rơi đang ngắm (trong tầm việc tay); null nếu không có.</summary>
        public DropStack AimedDrop { get; private set; }

        /// <summary>Tự xới / Mưa / thu hoạch (brief 0041); thêm trong Awake nếu chưa có.</summary>
        public PlayerFarmActions Farming { get; private set; }
        /// <summary>Id ô ruộng chín đang ngắm (trong tầm); -1 nếu không.</summary>
        public int AimedHarvest { get; private set; } = -1;

        void Awake()
        {
            view = GetComponent<TerrainChunkView>();
            outline = CreateOutline();
            CreateGhost();
            DropView = GetComponent<DropView>();
            if (DropView == null) DropView = gameObject.AddComponent<DropView>();
            Farming = GetComponent<PlayerFarmActions>();
            if (Farming == null) Farming = gameObject.AddComponent<PlayerFarmActions>();
        }

        void Update()
        {
            AimedDrop = null;
            AimedHarvest = -1;
            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.digit1Key.wasPressedThisFrame) selected = 0;
                if (kb.digit2Key.wasPressedThisFrame) selected = 1;
                if (kb.digit3Key.wasPressedThisFrame) selected = 2;
                if (kb.digit4Key.wasPressedThisFrame) selected = 3;
                if (kb.tabKey.wasPressedThisFrame) ToggleTool();
                if (kb.escapeKey.wasPressedThisFrame && Farming.SowCrop >= 0) Farming.ExitSowMode();
                if (kb.fKey.wasPressedThisFrame) CollectNearby();
            }

            var mouse = Mouse.current;
            if (mouse != null && Farming.SowCrop >= 0 && mouse.rightButton.wasPressedThisFrame)
            {
                Farming.ExitSowMode();
                ShowAim(false, default, default);
                return;
            }
            // Bản đồ hoặc vòng thời điểm đang mở, hoặc con trỏ trên nút của chúng / bảng trại (brief 0030) → không lăn chọn loại, không đào/đặt (brief 0016, 0024).
            if (mouse != null && (MinimapView.BlocksPointer(mouse.position.ReadValue()) || TimeDial.BlocksPointer(mouse.position.ReadValue()) || SimView.BlocksPointer(mouse.position.ReadValue())))
            {
                ShowAim(false, default, default);
                return;
            }
            if (mouse != null)
            {
                // Alt + lăn chuột đổi loại (lăn không Alt = thu phóng camera nhân vật, user 2026-10-22).
                if (kb != null && kb.altKey.isPressed)
                {
                    float wheel = mouse.scroll.ReadValue().y;
                    if (wheel > 0.01f) selected = (selected + Palette.Length - 1) % Palette.Length;
                    else if (wheel < -0.01f) selected = (selected + 1) % Palette.Length;
                }
            }

            var cam = Camera.main;
            if (mouse == null || cam == null || view.Editor == null)
                return;

            var ray = cam.ScreenPointToRay(mouse.position.ReadValue());
            var player = PlayerCharacter.Active;
            // Vật rơi trong tầm việc tay 8 ô dưới con trỏ → chuột trái nhặt cả chồng, không đào/đặt/xới (task contract 7e); ngoài 8 ô không chặn tia.
            AimedDrop = PickDrop(ray);
            if (AimedDrop != null)
            {
                ShowAim(false, default, default);
                if (player.FlyMode && DebugFlyCamera.IsMoveKeyHeld(kb)) return;
                if (mouse.leftButton.wasPressedThisFrame) CollectDrop(AimedDrop);
                return;
            }
            if (Farm != null && mouse.leftButton.wasPressedThisFrame && Physics.Raycast(ray, out var sh, maxDistance))
            {
                int sid = Farm.StorageIdOf(sh.collider);
                if (sid >= 0) { lastResult = Farm.OpenStockAt(sid) ? T("edit.open_stock") : Format("edit.stock_far", PlayerRules.HandReach); ShowAim(false, default, default); return; }
            }
            if (!Pick(ray, out var digCell, out var placeCell))
            {
                ShowAim(false, default, default);
                return;
            }

            if (kb != null && kb.rKey.wasPressedThisFrame) Farming.CastRain(digCell);
            AimedHarvest = Farming.RipePlotAt(digCell);
            if (AimedHarvest >= 0)
            {
                ShowAim(false, default, default);
                if ((player == null || player.FlyMode) && DebugFlyCamera.IsMoveKeyHeld(kb)) return;
                if (mouse.leftButton.wasPressedThisFrame) Farming.Harvest(AimedHarvest);
                return;
            }

            if (Farming.SowCrop >= 0)
            {
                ShowAim(false, default, default);
                if ((player == null || player.FlyMode) && DebugFlyCamera.IsMoveKeyHeld(kb)) return;
                if (mouse.leftButton.wasPressedThisFrame) UseTool(digCell, placeCell);
                return;
            }

            ShowAim(true, digCell, placeCell);
            if (outline.enabled)
            {
            // Độ rộng nét theo đơn vị thế giới để ra outlinePixels trên màn hình ở khoảng cách tới tâm ô.
            float dist = Vector3.Distance(cam.transform.position, outline.transform.TransformPoint(0.5f, 0.5f, 0.5f));
            float worldPerPixel = 2f * dist * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad) / Screen.height;
            outline.widthMultiplier = outlinePixels * worldPerPixel;
            }

            // Camera bay (không có nhân vật, hoặc chế độ debug F3): chỉ đào/đặt khi dừng (user 2026-10-20). Có nhân vật: được dùng công cụ khi đang đi (0028).
            if ((player == null || player.FlyMode) && DebugFlyCamera.IsMoveKeyHeld(kb)) return;

            if (mouse.leftButton.wasPressedThisFrame) UseTool(digCell, placeCell);
            if (Tool == EditTool.Till && mouse.rightButton.wasPressedThisFrame) UnmarkAimed(digCell);
        }

        /// <summary>Chuột phải khi cầm Xới: bỏ biển ô cell (trong 8 ô); ô đang tự xới → huỷ tự xới. true khi đã bỏ.</summary>
        public bool UnmarkAimed(Vector3Int cell)
        {
            if (Farm == null) return false;
            var player = PlayerCharacter.Active;
            if (player != null && !player.InToolReach(cell)) return false;
            int id = Farm.Sim.PlotAt(new CellPos(cell.x, cell.y, cell.z));
            if (!Farm.UnmarkAt(cell)) return false;
            if (Farming.TillState != SelfTillState.None && Farming.TillPlotId == id) Farming.CancelSelfTill();
            lastResult = Format("edit.unmark", cell);
            return true;
        }

        /// <summary>Tab: Đào → Đặt → Xới (khi có SimView) → Đào.</summary>
        public void ToggleTool()
        {
            if (Farming != null && Farming.SowCrop >= 0) { Farming.ExitSowMode(); return; }
            Farming?.CancelSelfTill();
            Tool = Tool == EditTool.Dig ? EditTool.Place : Tool == EditTool.Place && TillAvailable ? EditTool.Till : EditTool.Dig;
        }

        /// <summary>Màu bản mờ của vật liệu: TopColor, alpha 0,5.</summary>
        public static Color GhostColorOf(BlockMaterial m)
        {
            uint c = BlockMesher.TopColor(m);
            return new Color((c & 0xFF) / 255f, (c >> 8 & 0xFF) / 255f, (c >> 16 & 0xFF) / 255f, 0.5f);
        }

        /// <summary>
        /// Cập nhật khung / bản mờ theo công cụ (brief 0027). picked = false → tắt cả hai.
        /// Đào: khung ở digCell. Đặt: bản mờ ở placeCell khi CheckPlace = Ok (màu vật liệu) hoặc NoMaterial (đỏ); kết quả khác → ẩn.
        /// </summary>
        public void ShowAim(bool picked, Vector3Int digCell, Vector3Int placeCell)
        {
            outline.enabled = false;
            ghostRenderer.enabled = false;
            if (!picked || view.Editor == null) return;
            var player = PlayerCharacter.Active;
            if (player != null && !player.InToolReach(Tool == EditTool.Place ? placeCell : digCell)) return; // ngoài tầm 8 ô: không khung, không bản mờ (0031)
            if (Tool == EditTool.Till)
            {
                if (Farm == null || !Farm.CanTillAt(digCell)) return;
                outline.enabled = true;
                outline.transform.localPosition = digCell;
                return;
            }
            if (Tool == EditTool.Dig)
            {
                outline.enabled = true;
                outline.transform.localPosition = digCell;
                return;
            }
            var r = view.Editor.CheckPlace(placeCell.x, placeCell.y, placeCell.z, SelectedMaterial);
            if (r != EditResult.Ok && r != EditResult.NoMaterial) return;
            GhostColor = r == EditResult.Ok ? GhostColorOf(SelectedMaterial) : MissingColor;
            ghostMat.SetColor("_BaseColor", GhostColor);
            Ghost.transform.localPosition = placeCell;
            ghostRenderer.enabled = true;
        }

        /// <summary>Chuột trái: dùng công cụ đang cầm; ghi dòng kết quả ở HUD.</summary>
        public EditResult UseTool(Vector3Int digCell, Vector3Int placeCell)
        {
            if (Farming != null && Farming.SowCrop >= 0) { lastResult = ""; return Farming.SowAt(digCell) ? EditResult.Ok : EditResult.NotPlaceable; }
            var player = PlayerCharacter.Active;
            if (player != null && !player.InToolReach(Tool == EditTool.Place ? placeCell : digCell))
                return EditResult.OutOfReach; // click ngoài tầm: không làm gì, không đổi dòng kết quả (0031)
            if (Farming != null && Farming.TillState != SelfTillState.None) Farming.CancelSelfTill();
            if (Tool == EditTool.Till)
            {
                LastTill = Farm != null ? Farm.UseTill(digCell) : TillResult.Invalid;
                lastResult = LastTill switch
                {
                    TillResult.Marked => Format("edit.mark", digCell),
                    TillResult.SelfTill => "",
                    TillResult.OpenedPlot => Format("edit.open_plot", Farm.OpenPlotId),
                    _ => Format("edit.till_invalid", digCell),
                };
                if (LastTill == TillResult.SelfTill) Farming.BeginSelfTill(Farm.Sim.PlotAt(new Game.Core.Sim.CellPos(digCell.x, digCell.y, digCell.z)));
                // Giữ chữ ký EditResult cho Đào/Đặt; kết quả Xới đọc ở LastTill.
                return LastTill == TillResult.Invalid ? EditResult.NotPlaceable : EditResult.Ok;
            }
            if (Tool == EditTool.Dig)
            {
                var m = view.World.Size.InBounds(digCell.x, digCell.y, digCell.z)
                    ? view.World.GetMaterial(digCell.x, digCell.y, digCell.z) : BlockMaterial.Empty;
                var r = view.Dig(digCell);
                lastResult = r == EditResult.Ok ? Format("edit.dig_ok", digCell, Name(m == BlockMaterial.Farmland ? BlockMaterial.Dirt : m)) : Format("edit.dig_fail", digCell, Explain(r));
                return r;
            }
            var p = view.Place(placeCell, SelectedMaterial);
            lastResult = p == EditResult.Ok ? Format("edit.place_ok", PaletteName(selected), placeCell) : Format("edit.place_fail", PaletteName(selected), Explain(p));
            return p;
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

        /// <summary>Chồng vật rơi gần nhất mà tia trúng, trong tầm việc tay 8 ô, không bị collider nào (địa hình, nhà, dấu mốc) che; không có nhân vật → null.</summary>
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

        /// <summary>Phím F (Thu): nhặt mọi chồng trong 8 ô; ghi dòng kết quả. Trả số chồng đã nhặt; không có nhân vật → 0, không đổi dòng kết quả.</summary>
        public int CollectNearby()
        {
            var player = PlayerCharacter.Active;
            if (player == null || view.Editor == null) return 0;
            var f = player.FeetLocal;
            int n = view.Editor.Drops.CollectInReach(f.x, f.y, f.z, view.Bag);
            lastResult = n > 0 ? Format("edit.collect_ok", n) : Format("edit.collect_none", PlayerRules.HandReach);
            return n;
        }

        /// <summary>Click nhặt một chồng (phải trong tầm việc tay); ghi dòng kết quả khi được.</summary>
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
                return Format("hud.help_fly", PlayerRules.ToolReach, PlayerRules.HandReach);
            return Format("hud.help_walk", PlayerRules.ToolReach, PlayerRules.HandReach);
        }

        /// <summary>Chữ HUD góc trái (tách khỏi OnGUI để test); "" nếu chưa có túi.</summary>
        public string HudText()
        {
            if (view.Bag == null) return "";
            var text = Format("hud.tool", T(Tool switch { EditTool.Dig => "hud.tool_dig", EditTool.Place => "hud.tool_place", _ => "hud.tool_till" })) + HelpText();
            if (Tool == EditTool.Till)
                text += T("hud.till_help");
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

        LineRenderer CreateOutline()
        {
            var go = new GameObject("Ô đang chọn");
            go.transform.SetParent(transform, false);
            var lr = go.AddComponent<LineRenderer>();
            lr.useWorldSpace = false;
            lr.widthMultiplier = 0.04f;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            // Vẽ đè lên địa hình: khung bao ô đặc nên nửa dưới nằm trong mặt mesh mịn, ZTest thường sẽ che mất.
            var shader = Shader.Find("Game/SelectionOverlay");
            if (shader == null)
                shader = Shader.Find("Universal Render Pipeline/Unlit");
            var mat = shader != null
                ? new UnityEngine.Material(shader)
                : new UnityEngine.Material(UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline.defaultMaterial);
            mat.SetColor("_BaseColor", Color.white);
            lr.sharedMaterial = mat;
            // 12 cạnh khối lập phương đơn vị trong một đường 16 điểm (3 cạnh vẽ lặp).
            var p = new[]
            {
                new Vector3(0, 0, 0), new Vector3(1, 0, 0), new Vector3(1, 1, 0), new Vector3(0, 1, 0), new Vector3(0, 0, 0),
                new Vector3(0, 0, 1), new Vector3(1, 0, 1), new Vector3(1, 1, 1), new Vector3(0, 1, 1), new Vector3(0, 0, 1),
                new Vector3(0, 1, 1), new Vector3(0, 1, 0), new Vector3(1, 1, 0), new Vector3(1, 1, 1), new Vector3(1, 0, 1),
                new Vector3(1, 0, 0),
            };
            lr.positionCount = p.Length;
            lr.SetPositions(p);
            lr.enabled = false;
            return lr;
        }

        void CreateGhost()
        {
            var go = new GameObject("Bản mờ");
            go.transform.SetParent(transform, false);
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
            var mesh = new Mesh { name = "Bản mờ" };
            mesh.vertices = verts;
            mesh.triangles = tris;
            mesh.RecalculateBounds();
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
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
