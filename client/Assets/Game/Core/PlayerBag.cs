using System;
using Game.Core.Sim;

namespace Game.Core
{
    /// <summary>
    /// Túi đồ của người chơi (0028, 0034 mục 6): túi Main + túi phụ Hotbar, ô vuông, StackMax mỗi ô.
    /// Thêm: lấp chồng cùng loại ở túi phụ → ở túi → ô trống túi → ô trống túi phụ. Lấy theo loại: túi trước, túi phụ sau (brief 0061).
    /// Event phát tổng mới (túi + túi phụ) khi số đổi; sửa thẳng Main / Hotbar thì không phát.
    /// </summary>
    public sealed class PlayerBag : IItemStore
    {
        public static int DefaultBagSlots { get; private set; } = 10;
        public static int DefaultHotbarSlots { get; private set; } = 2;
        public static int DefaultBagPageSize { get; private set; } = 10;
        public static int DefaultHotbarPageSize { get; private set; } = 10;
        public static int DefaultStackMax { get; private set; } = 999;

        /// <summary>Lưu kích thước túi từ cấu hình (PlayerRules.Configure gọi). null → ArgumentNullException.</summary>
        public static void Configure(PlayerConfig p)
        {
            if (p == null) throw new ArgumentNullException(nameof(p));
            DefaultBagSlots = p.bagSlots;
            DefaultHotbarSlots = p.hotbarSlots;
            DefaultBagPageSize = p.bagPageSize;
            DefaultHotbarPageSize = p.hotbarPageSize;
            DefaultStackMax = p.stackMax;
        }

        public PlayerBag() : this(DefaultBagSlots, DefaultHotbarSlots, DefaultBagPageSize, DefaultHotbarPageSize, DefaultStackMax) { }

        public PlayerBag(int bagSlots, int hotbarSlots, int bagPageSize, int hotbarPageSize, int stackMax)
        {
            Main = new SlotInventory(bagSlots, bagPageSize, stackMax);
            Hotbar = new SlotInventory(hotbarSlots, hotbarPageSize, stackMax);
        }

        /// <summary>Túi (0034 mục 6).</summary>
        public SlotInventory Main { get; }

        /// <summary>Túi phụ (giữa-dưới màn hình, phím 1–0).</summary>
        public SlotInventory Hotbar { get; }

        /// <summary>(loại cây, tổng hạt mới).</summary>
        public event Action<int, int> SeedsChanged;

        /// <summary>(loại cây, tổng nông sản mới).</summary>
        public event Action<int, int> ProduceChanged;

        public event Action<BlockMaterial, int> Changed;

        public int Count(ItemId item) => Main.Count(item) + Hotbar.Count(item);

        public int CanAdd(ItemId item, int amount) => (int)Math.Min((long)Main.CanAdd(item, amount) + Hotbar.CanAdd(item, amount), amount);

        /// <summary>Thêm min(amount, chỗ trống); trả số đã thêm. Tool nhận được (camera quan sát).</summary>
        public int Add(ItemId item, int amount)
        {
            int left = amount; // amount < 0 → SlotInventory ném ArgumentOutOfRangeException
            left -= Hotbar.TopUp(item, left);
            left -= Main.TopUp(item, left);
            left -= Main.Add(item, left); // lúc này chỉ còn ô trống
            left -= Hotbar.Add(item, left);
            int n = amount - left;
            if (n > 0) Raise(item);
            return n;
        }

        public int Remove(ItemId item, int amount)
        {
            int n = Main.Remove(item, amount);
            n += Hotbar.Remove(item, amount - n);
            if (n > 0) Raise(item);
            return n;
        }

        void Raise(ItemId item)
        {
            switch (item.Kind)
            {
                case ItemKind.Block: Changed?.Invoke(item.Material, Count(item)); break;
                case ItemKind.SeedBag: SeedsChanged?.Invoke(item.Crop, Count(item)); break;
                case ItemKind.Produce: ProduceChanged?.Invoke(item.Crop, Count(item)); break;
            }
        }

        public int Get(BlockMaterial material) => Count(ItemId.Block(material));

        public int Add(BlockMaterial material, int amount = 1)
        {
            if (amount < 0)
                throw new ArgumentOutOfRangeException(nameof(amount), amount, "Amount to add must be ≥ 0");
            return Add(ItemId.Block(material), amount);
        }

        /// <summary>Lấy 1 đơn vị; false nếu hết.</summary>
        public bool TryTake(BlockMaterial material) => Remove(ItemId.Block(material), 1) == 1;

        public int GetSeeds(int crop)
        {
            CheckCrop(crop);
            return Count(ItemId.SeedBag(crop));
        }

        /// <summary>Trả số hạt đã thêm (túi đầy → ít hơn amount).</summary>
        public int AddSeeds(int crop, int amount)
        {
            CheckCrop(crop);
            if (amount < 0)
                throw new ArgumentOutOfRangeException(nameof(amount), amount, "Amount to add must be ≥ 0");
            return Add(ItemId.SeedBag(crop), amount);
        }

        public int GetProduce(int crop)
        {
            CheckCrop(crop);
            return Count(ItemId.Produce(crop));
        }

        /// <summary>Trả số nông sản đã thêm (túi đầy → ít hơn amount).</summary>
        public int AddProduce(int crop, int amount)
        {
            CheckCrop(crop);
            if (amount < 0)
                throw new ArgumentOutOfRangeException(nameof(amount), amount, "Amount to add must be ≥ 0");
            return Add(ItemId.Produce(crop), amount);
        }

        /// <summary>Đủ thì trừ amount hạt, phát SeedsChanged, true; thiếu → false, không đổi. amount = 0 → true, không phát.</summary>
        public bool TryTakeSeeds(int crop, int amount)
        {
            CheckCrop(crop);
            if (amount < 0)
                throw new ArgumentOutOfRangeException(nameof(amount), amount, "Amount to take must be ≥ 0");
            var item = ItemId.SeedBag(crop);
            if (Count(item) < amount) return false;
            if (amount == 0) return true;
            Remove(item, amount);
            return true;
        }

        /// <summary>Như TryTakeSeeds cho nông sản (phát ProduceChanged).</summary>
        public bool TryTakeProduce(int crop, int amount)
        {
            CheckCrop(crop);
            if (amount < 0)
                throw new ArgumentOutOfRangeException(nameof(amount), amount, "Amount to take must be ≥ 0");
            var item = ItemId.Produce(crop);
            if (Count(item) < amount) return false;
            if (amount == 0) return true;
            Remove(item, amount);
            return true;
        }

        static void CheckCrop(int crop)
        {
            if (crop < 0 || crop > 255)
                throw new ArgumentOutOfRangeException(nameof(crop), crop, "Crop must be in 0..255");
        }
    }
}
