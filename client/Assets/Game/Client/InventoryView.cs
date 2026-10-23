using System.Collections.Generic;
using Game.Core;
using Game.Core.Sim;
using Game.Core.View;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Client
{
    /// <summary>
    /// Túi / túi phụ / kho ô vuông (0034 mục 4, 6; world 7l d2, brief 0062). uGUI dựng bằng code, không EventSystem (tự kiểm con trỏ, phím qua GameInput).
    /// B túi, P ẩn/hiện túi phụ (mở túi luôn hiện), kho mở từ SimView.OpenStockAt (mở luôn túi, tự đóng khi xa > 10 ô).
    /// Click trái ô → bảng thông tin lề trái; chuột phải ô → cầm, túi đóng; 1–0 = ô túi phụ khi túi + kho đóng.
    /// Brief 0063: kéo thả cả chồng (tự làm, ngưỡng 8 px); chọn ô → Tab đổi đích (túi > túi phụ > kho) → 1 / 2 / 3.
    /// </summary>
    [RequireComponent(typeof(TerrainChunkView))]
    public class InventoryView : MonoBehaviour
    {
        const float CellSize = 56f, CellGap = 6f, PanelPad = 8f, ButtonSize = 28f;
        const int Columns = 5;
        /// <summary>Con trỏ đi xa điểm nhấn chừng này (px màn hình) thì bắt đầu kéo [số tạm] — cảm giác UI.</summary>
        public const float DragThresholdPx = 8f;

        public static InventoryView Active { get; private set; }
        public static bool AnyOpen => Active != null && (Active.BagOpen || Active.StockOpen || Active.HasSelection);
        public static bool BlocksPointer(Vector2 screen) => Active != null && Active.Covers(screen);

        public bool BagOpen { get; private set; }
        public bool StockOpen { get; private set; }
        /// <summary>Khối kho đang mở; -1 = không.</summary>
        public int StockIndex { get; private set; } = -1;
        public bool HotbarHidden { get; private set; }
        public bool HotbarShown => BagOpen || !HotbarHidden;
        public int BagPage { get; private set; }
        public int HotbarPage { get; private set; }
        public int StockPage { get; private set; }
        public bool HasSelection { get; private set; }
        public ItemContainer SelectedContainer { get; private set; }
        /// <summary>Bag / Hotbar: chỉ số ô trong SlotInventory.</summary>
        public int SelectedSlot { get; private set; } = -1;
        /// <summary>Stock: vật phẩm đang chọn (lưu ItemId, không lưu chỉ số).</summary>
        public ItemId SelectedStockItem { get; private set; }
        public bool InfoVisible => HasSelection;
        public bool Dragging { get; private set; }
        /// <summary>Đích của phím 1 / 2 / 3 (Tab đổi).</summary>
        public ItemContainer CurrentTarget { get; private set; }
        public bool TransferActive => BagOpen && HasSelection;
        /// <summary>4 dòng bảng thông tin nối "\n" (cho test); "" khi không chọn.</summary>
        public string InfoText => TryGetSelected(out var item) ? string.Join("\n", InfoLines(item)) : "";

        sealed class SlotUi
        {
            public RectTransform Rt;
            public Image Bg, Icon;
            public Text Count, Crop, Key;
            public string CountShown = "", CropShown = "", KeyShown = "";
        }

        sealed class Grid
        {
            public ItemContainer Container;
            public RectTransform Panel, Frame;
            public Image FrameImage;
            public SlotUi[] Cells;
            public RectTransform Prev, Next;
            public Text PageLabel, Title, Hint;
            public string PageShown = "", TitleShown = "";
        }

        TerrainChunkView view;
        SimView simView;
        Font font;
        Canvas canvas;
        Grid bagGrid, hotbarGrid, stockGrid;
        RectTransform infoPanel;
        Text[] infoLines;
        RectTransform dragRt;
        Image dragImage;
        StockStore stockStore;
        bool pressActive, hasSource;
        Vector2 pressPos;
        ItemContainer dragFrom;
        int dragSlot = -1;
        ItemId dragItem;
        string[] infoShown;
        int consumedFrame = -1;
        bool built;
        readonly List<ItemStack> stockList = new List<ItemStack>();

        SimView Sim => simView != null && simView.Sim != null ? simView : null;

        void Awake()
        {
            Active = this;
            view = GetComponent<TerrainChunkView>();
            simView = GetComponent<SimView>();
            GameSettings.Changed += RefreshText;
        }

        void OnDestroy()
        {
            GameSettings.Changed -= RefreshText;
            if (Active == this) Active = null;
        }

        // ---------- trạng thái ----------

        public void ToggleBag() => SetBagOpen(!BagOpen);

        /// <summary>Đóng túi → đóng cả kho + bỏ chọn.</summary>
        public void SetBagOpen(bool open)
        {
            BagOpen = open;
            if (!open)
            {
                CancelDrag();
                StockOpen = false;
                StockIndex = -1;
                ClearSelection();
            }
        }

        public void ToggleHotbar() => HotbarHidden = !HotbarHidden;

        /// <summary>Mở kho + túi, trang 0. Không có Sim → false.</summary>
        public bool OpenStock(int storageIndex)
        {
            if (Sim == null || storageIndex < 0 || storageIndex >= simView.Sim.State.Storages.Count) return false;
            StockIndex = storageIndex;
            StockOpen = true;
            StockPage = 0;
            BagOpen = true;
            ResetTarget();
            return true;
        }

        public void CloseAll() => SetBagOpen(false);

        public void ClearSelection()
        {
            HasSelection = false;
            SelectedSlot = -1;
        }

        SlotInventory Inv(ItemContainer c) => c == ItemContainer.Hotbar ? view.Bag.Hotbar : view.Bag.Main;

        int PageOf(ItemContainer c) => c == ItemContainer.Bag ? BagPage : c == ItemContainer.Hotbar ? HotbarPage : StockPage;

        int PageCountOf(ItemContainer c)
        {
            if (c != ItemContainer.Stock) return Inv(c).PageCount;
            BuildStock();
            int per = view.Bag.Main.PageSize;
            return Mathf.Max(1, (stockList.Count + per - 1) / per);
        }

        int ShownCount(ItemContainer c)
        {
            if (c == ItemContainer.Stock) return view.Bag.Main.PageSize;
            var inv = Inv(c);
            int page = PageOf(c);
            return inv.PageEnd(page) - inv.PageStart(page);
        }

        public void NextPage(ItemContainer c)
        {
            if (c == ItemContainer.Bag) BagPage = view.Bag.Main.NextPage(BagPage);
            else if (c == ItemContainer.Hotbar) HotbarPage = view.Bag.Hotbar.NextPage(HotbarPage);
            else StockPage = (StockPage + 1) % PageCountOf(c);
        }

        public void PrevPage(ItemContainer c)
        {
            if (c == ItemContainer.Bag) BagPage = view.Bag.Main.PrevPage(BagPage);
            else if (c == ItemContainer.Hotbar) HotbarPage = view.Bag.Hotbar.PrevPage(HotbarPage);
            else { int n = PageCountOf(c); StockPage = (StockPage + n - 1) % n; }
        }

        void BuildStock()
        {
            stockList.Clear();
            if (Sim == null) return;
            var st = simView.Sim.State.Stock;
            int n = simView.Sim.Config.crops.Length;
            for (int i = 0; i < n; i++)
                if (st.Seeds[i] > 0) stockList.Add(new ItemStack(ItemId.SeedBag(i), st.Seeds[i]));
            for (int i = 0; i < n; i++)
                if (st.Produce[i] > 0) stockList.Add(new ItemStack(ItemId.Produce(i), st.Produce[i]));
        }

        /// <summary>Ô thứ slotOnPage của trang đang xem; ngoài trang / trống → default.</summary>
        public ItemStack SlotAt(ItemContainer c, int slotOnPage)
        {
            if (view == null || view.Bag == null || slotOnPage < 0 || slotOnPage >= ShownCount(c)) return default;
            if (c == ItemContainer.Stock)
            {
                BuildStock();
                int idx = StockPage * view.Bag.Main.PageSize + slotOnPage;
                return idx < stockList.Count ? stockList[idx] : default;
            }
            var inv = Inv(c);
            return inv.Get(inv.PageStart(PageOf(c)) + slotOnPage);
        }

        public string SlotCountText(ItemContainer c, int slotOnPage)
        {
            var s = SlotAt(c, slotOnPage);
            return s.IsEmpty ? "" : s.Count.ToString();
        }

        /// <summary>Chọn ô (chỉ số trong trang đang xem); click lại đúng ô → bỏ chọn; ô trống → bỏ chọn, false.</summary>
        public bool Select(ItemContainer c, int slotOnPage)
        {
            var s = SlotAt(c, slotOnPage);
            if (s.IsEmpty) { ClearSelection(); return false; }
            if (c == ItemContainer.Stock)
            {
                if (HasSelection && SelectedContainer == c && SelectedStockItem == s.Item) { ClearSelection(); return true; }
                SelectedStockItem = s.Item;
                SelectedSlot = -1;
            }
            else
            {
                int slot = Inv(c).PageStart(PageOf(c)) + slotOnPage;
                if (HasSelection && SelectedContainer == c && SelectedSlot == slot) { ClearSelection(); return true; }
                SelectedSlot = slot;
            }
            SelectedContainer = c;
            HasSelection = true;
            ResetTarget();
            return true;
        }

        /// <summary>Vật phẩm đang chọn; ô đã trống → bỏ chọn và trả false.</summary>
        bool TryGetSelected(out ItemId item)
        {
            item = default;
            if (!HasSelection) return false;
            if (SelectedContainer == ItemContainer.Stock)
            {
                BuildStock();
                foreach (var s in stockList)
                    if (s.Item == SelectedStockItem) { item = s.Item; return true; }
                return false;
            }
            var st = Inv(SelectedContainer).Get(SelectedSlot);
            item = st.Item;
            return !st.IsEmpty;
        }

        /// <summary>Chuột phải ô túi / túi phụ (0034 mục 4): cầm vật đó, đóng túi. Nông sản → dòng "Không cầm được". Kho → không làm gì.</summary>
        public bool HoldFromSlot(ItemContainer c, int slotOnPage)
        {
            if (c == ItemContainer.Stock) return false;
            var s = SlotAt(c, slotOnPage);
            if (s.IsEmpty) return false;
            var edit = GetComponent<TerrainEditInput>();
            if (edit == null) return false;
            if (!edit.HoldItem(s.Item))
            {
                if (s.Item.Kind == ItemKind.Produce) edit.SetResult(Fmt("inv.not_holdable", ItemName(s.Item)));
                return false;
            }
            SetBagOpen(false);
            ClearSelection();
            return true;
        }

        /// <summary>Phím k = 0..9 (1–9, 0): cầm ô túi phụ thứ k của trang đang xem. Chỉ khi túi + kho đóng.</summary>
        public bool UseHotbarKey(int k)
        {
            if (view == null || view.Bag == null || BagOpen || StockOpen) return false;
            return HoldFromSlot(ItemContainer.Hotbar, k);
        }

        // ---------- chuyển đồ (brief 0063) ----------

        StockStore Stock => Sim == null ? null : (stockStore ??= new StockStore(simView.Sim));

        IItemStore StoreOf(ItemContainer c) => c == ItemContainer.Bag ? (IItemStore)view.Bag.Main : c == ItemContainer.Hotbar ? view.Bag.Hotbar : Stock;

        static string TargetName(ItemContainer c) => T(c == ItemContainer.Bag ? "inv.target_bag" : c == ItemContainer.Hotbar ? "inv.target_hotbar" : "inv.target_stock");

        void ResetTarget()
        {
            if (HasSelection) CurrentTarget = ItemTransfer.DefaultTarget(SelectedContainer, StockOpen);
        }

        /// <summary>Tab: đích kế tiếp (Quyết định 3). Không TransferActive → không làm gì.</summary>
        public void NextTarget()
        {
            if (!TransferActive) return;
            CurrentTarget = ItemTransfer.NextTarget(SelectedContainer, CurrentTarget, StockOpen);
        }

        /// <summary>1 / 2 / 3: chuyển PartAmount(số ở ô chọn, part) sang CurrentTarget; trả số đã chuyển (không TransferActive → 0).</summary>
        public int TransferPart(int part)
        {
            if (!TransferActive || !TryGetSelected(out var item)) return 0;
            var src = SelectedContainer;
            var dest = CurrentTarget;
            if (dest == src || StoreOf(dest) == null) return 0;
            int count = src == ItemContainer.Stock ? Stock.Count(item) : Inv(src).Get(SelectedSlot).Count;
            int amount = ItemTransfer.PartAmount(count, part);
            int n = src == ItemContainer.Stock
                ? ItemTransfer.FromStore(Stock, item, StoreOf(dest), amount)
                : ItemTransfer.FromSlot(Inv(src), SelectedSlot, StoreOf(dest), amount);
            Report(n, item, dest);
            if (HasSelection && !TryGetSelected(out _)) ClearSelection();
            return n;
        }

        void Report(int n, ItemId item, ItemContainer dest)
        {
            var edit = GetComponent<TerrainEditInput>();
            if (edit == null) return;
            if (n > 0) edit.SetResult(Fmt("inv.moved", n, ItemName(item), TargetName(dest)));
            else if (dest == ItemContainer.Stock && (item.Kind == ItemKind.Block || item.Kind == ItemKind.Tool)) edit.SetResult(Fmt("inv.stock_rejects", ItemName(item)));
            else edit.SetResult(Fmt("inv.move_none", TargetName(dest)));
        }

        void CancelDrag()
        {
            Dragging = false;
            pressActive = false;
            hasSource = false;
            if (dragRt != null) dragRt.gameObject.SetActive(false);
        }

        /// <summary>Nhấn chuột trái: trúng phần nào của InventoryView → ghi điểm nhấn (+ ô nguồn nếu ô có đồ), true.</summary>
        public bool PointerDown(Vector2 screen)
        {
            if (!built || !Covers(screen)) return false;
            pressActive = true;
            pressPos = screen;
            hasSource = false;
            foreach (var c in Containers)
            {
                if (!Visible(c)) continue;
                var g = GridOf(c);
                if (!Hit(g.Panel, screen)) continue;
                int n = ShownCount(c);
                for (int i = 0; i < n && i < g.Cells.Length; i++)
                {
                    if (!Hit(g.Cells[i].Rt, screen)) continue;
                    var st = SlotAt(c, i);
                    if (st.IsEmpty) return true;
                    hasSource = true;
                    dragFrom = c;
                    dragItem = st.Item;
                    dragSlot = c == ItemContainer.Stock ? -1 : Inv(c).PageStart(PageOf(c)) + i;
                    return true;
                }
            }
            return true;
        }

        /// <summary>Giữ chuột: quá DragThresholdPx từ điểm nhấn và có ô nguồn → Dragging; hình kéo bám con trỏ.</summary>
        public void PointerMove(Vector2 screen)
        {
            if (!pressActive || !hasSource) return;
            if (!Dragging && (screen - pressPos).magnitude > DragThresholdPx)
            {
                Dragging = true;
                dragImage.color = new Color(IconColor(dragItem).r, IconColor(dragItem).g, IconColor(dragItem).b, 0.8f);
                dragRt.gameObject.SetActive(true);
                dragRt.SetAsLastSibling();
            }
            if (Dragging) dragRt.position = new Vector3(screen.x, screen.y, 0f);
        }

        /// <summary>Nhả: Dragging → DropAt(screen); không → LeftClickAt(điểm nhấn) như 0062. Trả true nếu đã xử lý.</summary>
        public bool PointerUp(Vector2 screen)
        {
            if (!pressActive) return false;
            bool was = Dragging, source = hasSource;
            var press = pressPos;
            CancelDrag();
            hasSource = source;
            if (was) { DropAt(screen); hasSource = false; return true; }
            hasSource = false;
            return LeftClickAt(press);
        }

        /// <summary>true nếu điểm trong khung nền của nơi c (nơi đó đang hiện).</summary>
        public bool CoversPanelFrame(ItemContainer c, Vector2 screen) => built && Visible(c) && Hit(GridOf(c).Frame, screen);

        /// <summary>Thả ô nguồn (hasSource) tại screen: chuyển cả chồng (Quyết định 2); trả số đã chuyển.</summary>
        int DropAt(Vector2 screen)
        {
            if (!hasSource) return 0;
            ItemContainer tc = default;
            int ti = -1;
            bool found = false;
            foreach (var c in Containers)
            {
                if (!Visible(c)) continue;
                var g = GridOf(c);
                if (!Hit(g.Panel, screen)) continue;
                found = true;
                tc = c;
                int n = ShownCount(c);
                for (int i = 0; i < n && i < g.Cells.Length; i++)
                    if (Hit(g.Cells[i].Rt, screen)) { ti = i; break; }
                break;
            }
            hasSource = false;
            if (!found) return 0;
            var src = dragFrom;
            var item = dragItem;
            if (src == ItemContainer.Stock)
            {
                if (tc == ItemContainer.Stock || Stock == null) return 0;
                int count = Stock.Count(item);
                if (count <= 0) return 0;
                var destInv = Inv(tc);
                int moved = 0;
                if (ti >= 0) moved = ItemTransfer.ToSlot(Stock, item, destInv, destInv.PageStart(PageOf(tc)) + ti, count);
                if (moved == 0) moved = ItemTransfer.FromStore(Stock, item, destInv, count);
                Report(moved, item, tc);
                return moved;
            }
            var srcInv = Inv(src);
            var st = srcInv.Get(dragSlot);
            if (st.IsEmpty || st.Item != item) return 0;
            if (tc == ItemContainer.Stock)
            {
                if (Stock == null) return 0;
                int moved = ItemTransfer.FromSlot(srcInv, dragSlot, Stock, st.Count);
                Report(moved, item, tc);
                return moved;
            }
            var di = Inv(tc);
            if (ti >= 0)
            {
                int destSlot = di.PageStart(PageOf(tc)) + ti;
                if (di == srcInv && destSlot == dragSlot) return 0;
                return srcInv.Move(dragSlot, di, destSlot) ? st.Count : 0; // gộp / đổi chỗ: không ghi dòng (thấy ngay trên ô)
            }
            if (di == srcInv) return 0;
            int m = ItemTransfer.FromSlot(srcInv, dragSlot, di, st.Count);
            if (m == 0) Report(0, item, tc);
            return m;
        }

        // ---------- chữ ----------

        static string T(string k) => GameSettings.Text.Get(k);
        static string Fmt(string k, params object[] a) => GameSettings.Text.Format(k, a);

        string CropLabel(int crop) => Sim != null ? SiteStatus.CropName(GameSettings.Text, simView.Sim.Config, crop) : crop.ToString();

        public string ItemName(ItemId item)
        {
            switch (item.Kind)
            {
                case ItemKind.SeedBag: return Fmt("inv.seeds", CropLabel(item.Crop));
                case ItemKind.Produce: return Fmt("inv.produce", CropLabel(item.Crop));
                default: return TerrainEditInput.ItemLabel(item);
            }
        }

        string InfoSource(ItemId item)
        {
            switch (item.Kind)
            {
                case ItemKind.Block: return T("inv.source_block");
                case ItemKind.SeedBag: return T("inv.source_seeds");
                case ItemKind.Produce: return T("inv.source_produce");
                default: return T("inv.source_camera");
            }
        }

        string InfoStock(ItemId item)
        {
            if (Sim == null) return T("inv.info_no_stock");
            var st = simView.Sim.State.Stock;
            if (item.Kind == ItemKind.SeedBag && item.Crop < st.Seeds.Length) return Fmt("inv.info_stock", st.Seeds[item.Crop]);
            if (item.Kind == ItemKind.Produce && item.Crop < st.Produce.Length) return Fmt("inv.info_stock", st.Produce[item.Crop]);
            return T("inv.info_no_stock");
        }

        public static Color IconColor(ItemId item)
        {
            switch (item.Kind)
            {
                case ItemKind.Block: return DropView.ColorOf(item);
                case ItemKind.SeedBag: return DropView.SeedBagColor;
                case ItemKind.Produce: return new Color(0.95f, 0.55f, 0.15f, 1f);
                default: return new Color(0.35f, 0.35f, 0.45f, 1f);
            }
        }

        // ---------- vòng lặp ----------

        void Update()
        {
            if (view == null || view.Bag == null) return;
            if (!built) Build();
            if (GameInput.ToggleBag.WasPressedThisFrame()) ToggleBag();
            if (GameInput.ToggleHotbar.WasPressedThisFrame()) ToggleHotbar();
            if (GameInput.PanelsClose.WasPressedThisFrame() && AnyOpen) CloseAll();
            if (!BagOpen && !StockOpen)
                for (int k = 0; k < 10; k++)
                    if (GameInput.Hotbar(k).WasPressedThisFrame()) UseHotbarKey(k);
            AutoCloseStock();
            if (TransferActive)
            {
                if (GameInput.NextTarget.WasPressedThisFrame()) NextTarget();
                if (GameInput.TransferAll.WasPressedThisFrame()) TransferPart(1);
                else if (GameInput.TransferHalf.WasPressedThisFrame()) TransferPart(2);
                else if (GameInput.TransferThird.WasPressedThisFrame()) TransferPart(3);
            }
            if (GameInput.HasPointer)
            {
                var pos = GameInput.PointerPosition;
                if (GameInput.MoveClick.WasPressedThisFrame() && PointerDown(pos)) consumedFrame = Time.frameCount;
                if (pressActive && GameInput.MoveClick.IsPressed()) PointerMove(pos);
                if (pressActive && GameInput.MoveClick.WasReleasedThisFrame() && PointerUp(pos)) consumedFrame = Time.frameCount;
                if (!Dragging && GameInput.UseTool.WasPressedThisFrame() && RightClickAt(pos)) consumedFrame = Time.frameCount;
            }
            Refresh();
        }

        /// <summary>Kho đang mở mà nhân vật xa khối kho > TargetReach → kho đóng (túi giữ nguyên).</summary>
        void AutoCloseStock()
        {
            if (!StockOpen) return;
            var player = PlayerCharacter.Active;
            if (Sim != null && player != null && StockIndex >= 0 && StockIndex < simView.Sim.State.Storages.Count)
            {
                var f = player.FeetLocal;
                var cell = simView.Sim.State.Storages[StockIndex].Cell;
                if (PlayerRules.InTargetReach(f.x, f.y, f.z, cell.X, cell.Y, cell.Z)) return;
            }
            else if (Sim != null && player == null) return;
            StockOpen = false;
            StockIndex = -1;
            if (HasSelection && SelectedContainer == ItemContainer.Stock) ClearSelection();
            ResetTarget();
        }

        // ---------- con trỏ ----------

        static bool Hit(RectTransform rt, Vector2 screen) =>
            rt != null && rt.gameObject.activeInHierarchy && RectTransformUtility.RectangleContainsScreenPoint(rt, screen, null);

        Grid GridOf(ItemContainer c) => c == ItemContainer.Bag ? bagGrid : c == ItemContainer.Hotbar ? hotbarGrid : stockGrid;

        bool Visible(ItemContainer c) => c == ItemContainer.Bag ? BagOpen : c == ItemContainer.Hotbar ? HotbarShown : StockOpen;

        public bool Covers(Vector2 screen)
        {
            if (Dragging || Time.frameCount == consumedFrame) return true;
            if (!built) return false;
            foreach (var c in Containers)
            {
                if (!Visible(c)) continue;
                var g = GridOf(c);
                if (Hit(g.Panel, screen) || Hit(g.Prev, screen) || Hit(g.Next, screen)) return true;
            }
            return HasSelection && Hit(infoPanel, screen);
        }

        static readonly ItemContainer[] Containers = { ItemContainer.Hotbar, ItemContainer.Bag, ItemContainer.Stock };

        /// <summary>Tâm ô (chỉ số trong trang) ra toạ độ màn hình (pixel, gốc dưới-trái). Nơi đó đang ẩn → (-1, -1).</summary>
        public Vector2 SlotScreenCenter(ItemContainer c, int slotOnPage)
        {
            if (!built || !Visible(c)) return new Vector2(-1f, -1f);
            var g = GridOf(c);
            if (slotOnPage < 0 || slotOnPage >= g.Cells.Length || slotOnPage >= ShownCount(c)) return new Vector2(-1f, -1f);
            var rt = g.Cells[slotOnPage].Rt;
            Vector3 w = rt.TransformPoint(rt.rect.center);
            return new Vector2(w.x, w.y);
        }

        /// <summary>Trúng ô → chọn (bật/tắt); trúng nút trang → đổi trang. true nếu trúng phần nào của InventoryView.</summary>
        public bool LeftClickAt(Vector2 screen)
        {
            if (!Covers(screen)) return false;
            if (!built) return true;
            foreach (var c in Containers)
            {
                if (!Visible(c)) continue;
                var g = GridOf(c);
                if (PageControlsShown(c))
                {
                    if (Hit(g.Prev, screen)) { PrevPage(c); return true; }
                    if (Hit(g.Next, screen)) { NextPage(c); return true; }
                }
                if (!Hit(g.Panel, screen)) continue;
                int n = ShownCount(c);
                for (int i = 0; i < n && i < g.Cells.Length; i++)
                    if (Hit(g.Cells[i].Rt, screen)) { Select(c, i); return true; }
            }
            return true;
        }

        /// <summary>Chuột phải ô túi / túi phụ → cầm. true nếu trúng phần nào của InventoryView.</summary>
        public bool RightClickAt(Vector2 screen)
        {
            if (!Covers(screen)) return false;
            if (!built) return true;
            foreach (var c in Containers)
            {
                if (!Visible(c) || c == ItemContainer.Stock) continue;
                var g = GridOf(c);
                if (!Hit(g.Panel, screen)) continue;
                int n = ShownCount(c);
                for (int i = 0; i < n && i < g.Cells.Length; i++)
                    if (Hit(g.Cells[i].Rt, screen)) { HoldFromSlot(c, i); return true; }
            }
            return true;
        }

        bool PageControlsShown(ItemContainer c) => PageCountOf(c) > 1;

        // ---------- dựng uGUI ----------

        static RectTransform NewRt(string name, Transform parent)
        {
            var rt = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
            rt.SetParent(parent, false);
            return rt;
        }

        static void Place(RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;
        }

        static Image NewImage(RectTransform rt, Color c)
        {
            var img = rt.gameObject.AddComponent<Image>();
            img.color = c;
            img.raycastTarget = false;
            return img;
        }

        Text NewText(string name, RectTransform parent, int size, TextAnchor align, Vector2 inset)
        {
            var rt = NewRt(name, parent);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = inset;
            rt.offsetMax = -inset;
            var t = rt.gameObject.AddComponent<Text>();
            t.font = font;
            t.fontSize = size;
            t.alignment = align;
            t.color = Color.white;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            return t;
        }

        RectTransform NewButton(string name, RectTransform parent, string label)
        {
            var rt = NewRt(name, parent);
            NewImage(rt, new Color(0f, 0f, 0f, 0.55f));
            NewText("Label", rt, 20, TextAnchor.MiddleCenter, Vector2.zero).text = label;
            return rt;
        }

        SlotUi NewCell(RectTransform parent, int index, int columns, float top, bool keyLabel)
        {
            int col = index % columns, row = index / columns;
            var rt = NewRt("Cell " + index, parent);
            Place(rt, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(PanelPad + col * (CellSize + CellGap), -(top + row * (CellSize + CellGap))), new Vector2(CellSize, CellSize));
            var cell = new SlotUi { Rt = rt, Bg = NewImage(rt, new Color(0f, 0f, 0f, 0.55f)) };
            var iconRt = NewRt("Icon", rt);
            Place(iconRt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(CellSize - 16f, CellSize - 16f));
            cell.Icon = NewImage(iconRt, Color.white);
            cell.Count = NewText("Count", rt, 16, TextAnchor.LowerRight, new Vector2(3f, 3f));
            cell.Crop = NewText("Crop", rt, 13, TextAnchor.UpperRight, new Vector2(3f, 3f));
            if (keyLabel) cell.Key = NewText("Key", rt, 13, TextAnchor.UpperLeft, new Vector2(3f, 3f));
            return cell;
        }

        /// <summary>Lưới có tiêu đề + trang (túi, kho) hoặc hàng đơn (túi phụ). Panel neo theo anchor/pivot/pos.</summary>
        Grid NewGrid(ItemContainer c, RectTransform root, Vector2 anchor, Vector2 pos, int cellCount, int columns, bool titled)
        {
            int rows = (cellCount + columns - 1) / columns;
            float top = titled ? 30f : PanelPad;
            float w = columns * CellSize + (columns - 1) * CellGap + PanelPad * 2f;
            float h = top + rows * CellSize + (rows - 1) * CellGap + PanelPad + (titled ? 22f : 0f);
            var g = new Grid { Container = c, Cells = new SlotUi[cellCount] };
            g.Panel = NewRt(c + " Panel", root);
            Place(g.Panel, anchor, anchor, pos, new Vector2(w, h));
            NewImage(g.Panel, new Color(0f, 0f, 0f, 0.35f));
            // Khung nền (padding 6, sau các ô): đổi màu khi là đích chuyển đồ (brief 0063).
            int usedCols = Mathf.Min(columns, cellCount);
            g.Frame = NewRt("Frame", g.Panel);
            Place(g.Frame, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(PanelPad - 6f, -(top - 6f)),
                new Vector2(usedCols * CellSize + (usedCols - 1) * CellGap + 12f, rows * CellSize + (rows - 1) * CellGap + 12f));
            g.FrameImage = NewImage(g.Frame, Color.clear);
            for (int i = 0; i < cellCount; i++) g.Cells[i] = NewCell(g.Panel, i, columns, top, c == ItemContainer.Hotbar);
            var tr = new Vector2(1f, 1f);
            if (titled)
            {
                g.Title = NewText("Title", g.Panel, 16, TextAnchor.UpperLeft, new Vector2(PanelPad, 6f));
                g.Hint = NewText("Hint", g.Panel, 14, TextAnchor.LowerLeft, new Vector2(PanelPad, 4f));
                g.Next = NewButton("Next", g.Panel, ">");
                Place(g.Next, tr, tr, new Vector2(-PanelPad, -2f), new Vector2(ButtonSize, ButtonSize));
                var lab = NewRt("Page", g.Panel);
                Place(lab, tr, tr, new Vector2(-PanelPad - ButtonSize, -2f), new Vector2(56f, ButtonSize));
                g.PageLabel = NewText("Text", lab, 16, TextAnchor.MiddleCenter, Vector2.zero);
                g.Prev = NewButton("Prev", g.Panel, "<");
                Place(g.Prev, tr, tr, new Vector2(-PanelPad - ButtonSize - 56f, -2f), new Vector2(ButtonSize, ButtonSize));
            }
            else
            {
                // Túi phụ: nút trang hai bên.
                g.Prev = NewButton("Prev", g.Panel, "<");
                Place(g.Prev, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-4f, 0f), new Vector2(ButtonSize, ButtonSize));
                g.Next = NewButton("Next", g.Panel, ">");
                Place(g.Next, new Vector2(1f, 0.5f), new Vector2(0f, 0.5f), new Vector2(4f, 0f), new Vector2(ButtonSize, ButtonSize));
            }
            return g;
        }

        void Build()
        {
            built = true;
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var canvasGo = new GameObject("Inventory", typeof(RectTransform));
            canvasGo.transform.SetParent(transform, false);
            canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 12;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 1f;
            var root = (RectTransform)canvasGo.transform;

            var main = view.Bag.Main;
            var hot = view.Bag.Hotbar;
            int mainCells = main.PageEnd(0) - main.PageStart(0);
            int hotCells = hot.PageEnd(0) - hot.PageStart(0);
            hotbarGrid = NewGrid(ItemContainer.Hotbar, root, new Vector2(0.5f, 0f), new Vector2(0f, 12f), hotCells, hotCells, false);
            bagGrid = NewGrid(ItemContainer.Bag, root, new Vector2(0.5f, 0f), new Vector2(0f, 84f), mainCells, Columns, true);
            stockGrid = NewGrid(ItemContainer.Stock, root, new Vector2(1f, 0.5f), new Vector2(-12f, 0f), main.PageSize, Columns, true);

            infoPanel = NewRt("Info Panel", root);
            Place(infoPanel, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(12f, 0f), new Vector2(280f, 166f));
            NewImage(infoPanel, new Color(0f, 0f, 0f, 0.7f));
            infoLines = new Text[5];
            infoShown = new string[5] { "", "", "", "", "" };
            for (int i = 0; i < 5; i++)
            {
                var line = NewRt("Line " + i, infoPanel);
                Place(line, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(10f, -8f - 30f * i), new Vector2(260f, 28f));
                infoLines[i] = NewText("Text", line, 18, TextAnchor.UpperLeft, Vector2.zero);
                infoLines[i].horizontalOverflow = HorizontalWrapMode.Wrap;
            }
            infoLines[0].fontStyle = FontStyle.Bold;
            var dragGo = NewRt("Drag", root);
            Place(dragGo, Vector2.zero, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(CellSize - 16f, CellSize - 16f));
            dragRt = dragGo;
            dragImage = NewImage(dragGo, Color.white);
            dragGo.gameObject.SetActive(false);
            RefreshText();
        }

        /// <summary>Gán lại chữ cố định (đổi ngôn ngữ).</summary>
        void RefreshText()
        {
            if (!built) return;
            bagGrid.Title.text = T("inv.bag_title");
            bagGrid.Hint.text = T("inv.bag_hint");
            stockGrid.Hint.text = "";
            foreach (var g in new[] { bagGrid, stockGrid }) { g.PageShown = ""; g.TitleShown = ""; }
        }

        // ---------- vẽ lại mỗi frame ----------

        void Refresh()
        {
            if (HasSelection && !TryGetSelected(out _)) ClearSelection();
            if (HasSelection && (CurrentTarget == SelectedContainer || (CurrentTarget == ItemContainer.Stock && !StockOpen))) ResetTarget();
            foreach (var c0 in Containers)
                GridOf(c0).FrameImage.color = TransferActive && CurrentTarget == c0 ? new Color(0.25f, 0.5f, 0.9f, 0.45f) : Color.clear;
            hotbarGrid.Panel.gameObject.SetActive(HotbarShown);
            bagGrid.Panel.gameObject.SetActive(BagOpen);
            stockGrid.Panel.gameObject.SetActive(StockOpen);
            infoPanel.gameObject.SetActive(HasSelection);
            foreach (var c in Containers)
            {
                if (!Visible(c)) continue;
                var g = GridOf(c);
                int shown = ShownCount(c);
                bool paged = PageControlsShown(c);
                g.Prev.gameObject.SetActive(paged);
                g.Next.gameObject.SetActive(paged);
                if (g.PageLabel != null)
                {
                    string page = Fmt("inv.page", PageOf(c) + 1, PageCountOf(c));
                    if (page != g.PageShown) { g.PageShown = page; g.PageLabel.text = page; }
                    g.PageLabel.gameObject.SetActive(paged);
                }
                if (c == ItemContainer.Stock && Sim != null)
                {
                    string title = SiteStatus.StockPanelText(GameSettings.Text, simView.Sim.State.Stock, simView.Sim.Config).Split('\n')[0];
                    if (title != g.TitleShown) { g.TitleShown = title; g.Title.text = title; }
                }
                for (int i = 0; i < g.Cells.Length; i++)
                {
                    var cell = g.Cells[i];
                    bool on = i < shown;
                    if (cell.Rt.gameObject.activeSelf != on) cell.Rt.gameObject.SetActive(on);
                    if (!on) continue;
                    var s = SlotAt(c, i);
                    bool selected = HasSelection && SelectedContainer == c && IsSelectedSlot(c, i, s);
                    cell.Bg.color = selected ? new Color(0.95f, 0.8f, 0.2f, 0.9f) : new Color(0f, 0f, 0f, 0.55f);
                    cell.Icon.enabled = !s.IsEmpty;
                    if (!s.IsEmpty) cell.Icon.color = IconColor(s.Item);
                    SetText(cell.Count, ref cell.CountShown, s.IsEmpty ? "" : s.Count.ToString());
                    bool crop = !s.IsEmpty && (s.Item.Kind == ItemKind.SeedBag || s.Item.Kind == ItemKind.Produce);
                    SetText(cell.Crop, ref cell.CropShown, crop ? (Sim != null ? simView.Sim.Config.crops[s.Item.Crop].id : s.Item.Crop.ToString()) : "");
                    if (cell.Key != null)
                    {
                        SetText(cell.Key, ref cell.KeyShown, i < 10 ? ((i + 1) % 10).ToString() : "");
                    }
                }
            }
            RefreshInfo();
        }

        bool IsSelectedSlot(ItemContainer c, int slotOnPage, ItemStack s)
        {
            if (c == ItemContainer.Stock) return SelectedStockItem == s.Item;
            return SelectedSlot == Inv(c).PageStart(PageOf(c)) + slotOnPage;
        }

        static void SetText(Text t, ref string shown, string value)
        {
            if (shown == value) return;
            shown = value;
            t.text = value;
        }

        string[] InfoLines(ItemId item)
        {
            var lines = new List<string>
            {
                ItemName(item),
                InfoSource(item),
                Fmt("inv.info_bag", view.Bag.Count(item)),
                InfoStock(item),
            };
            if (TransferActive) lines.Add(Fmt("inv.target", TargetName(CurrentTarget)));
            return lines.ToArray();
        }

        void RefreshInfo()
        {
            if (!HasSelection || !TryGetSelected(out var item)) return;
            var lines = InfoLines(item);
            for (int i = 0; i < 5; i++) SetText(infoLines[i], ref infoShown[i], i < lines.Length ? lines[i] : "");
        }
    }
}
