using System;
using System.Collections.Generic;

namespace Game.Core
{
    public enum ItemContainer : byte { Bag = 0, Hotbar = 1, Stock = 2 }

    /// <summary>Chuyển đồ túi / túi phụ / kho (0034 mục 6).</summary>
    public static class ItemTransfer
    {
        // Thứ tự ưu tiên Bag > Hotbar > Stock, bỏ source; Stock chỉ có khi stockOpen.
        static List<ItemContainer> Targets(ItemContainer source, bool stockOpen)
        {
            var list = new List<ItemContainer>(3);
            if (source != ItemContainer.Bag) list.Add(ItemContainer.Bag);
            if (source != ItemContainer.Hotbar) list.Add(ItemContainer.Hotbar);
            if (stockOpen && source != ItemContainer.Stock) list.Add(ItemContainer.Stock);
            return list;
        }

        public static ItemContainer DefaultTarget(ItemContainer source, bool stockOpen) =>
            Targets(source, stockOpen)[0];

        /// <summary>Đích kế tiếp sau current theo vòng; current không có trong danh sách → phần tử đầu.</summary>
        public static ItemContainer NextTarget(ItemContainer source, ItemContainer current, bool stockOpen)
        {
            List<ItemContainer> t = Targets(source, stockOpen);
            int i = t.IndexOf(current);
            return i < 0 ? t[0] : t[(i + 1) % t.Count];
        }

        /// <summary>part 1 → count; 2 → ceil(count/2); 3 → ceil(count/3).</summary>
        public static int PartAmount(int count, int part)
        {
            if (count < 0) throw new ArgumentOutOfRangeException(nameof(count), count, "Count must be >= 0");
            if (part < 1 || part > 3) throw new ArgumentOutOfRangeException(nameof(part), part, "Part must be 1, 2 or 3");
            return (count + part - 1) / part;
        }

        /// <summary>Chuyển min(amount, số ở ô, to.CanAdd) từ ô slot sang to; trả số đã chuyển.</summary>
        public static int FromSlot(SlotInventory from, int slot, IItemStore to, int amount)
        {
            if (from == null) throw new ArgumentNullException(nameof(from));
            if (to == null) throw new ArgumentNullException(nameof(to));
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount), amount, "Amount must be >= 0");
            if (ReferenceEquals(from, to)) throw new ArgumentException("Source and target must differ.");
            ItemStack st = from.Get(slot);
            if (st.IsEmpty) return 0;
            int n = Math.Min(Math.Min(amount, st.Count), to.CanAdd(st.Item, amount));
            if (n <= 0) return 0;
            n = to.Add(st.Item, n);
            from.RemoveFromSlot(slot, n);
            return n;
        }

        /// <summary>Chuyển min(amount, from.Count(item), to.CanAdd) theo loại; trả số đã chuyển.</summary>
        public static int FromStore(IItemStore from, ItemId item, IItemStore to, int amount)
        {
            if (from == null) throw new ArgumentNullException(nameof(from));
            if (to == null) throw new ArgumentNullException(nameof(to));
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount), amount, "Amount must be >= 0");
            if (ReferenceEquals(from, to)) throw new ArgumentException("Source and target must differ.");
            int n = Math.Min(Math.Min(amount, from.Count(item)), to.CanAdd(item, amount));
            if (n <= 0) return 0;
            n = to.Add(item, n);
            from.Remove(item, n);
            return n;
        }

        /// <summary>Kéo thả từ kho vào một ô (0034 mục 6): ô trống hoặc cùng loại → chuyển min(amount, from.Count(item), chỗ trống của ô); ô chứa loại khác → 0. Trả số đã chuyển.</summary>
        public static int ToSlot(IItemStore from, ItemId item, SlotInventory to, int slot, int amount)
        {
            if (from == null) throw new ArgumentNullException(nameof(from));
            if (to == null) throw new ArgumentNullException(nameof(to));
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount), amount, "Amount must be >= 0");
            if (ReferenceEquals(from, to)) throw new ArgumentException("Source and target must differ.");
            int n = Math.Min(amount, from.Count(item));
            if (n <= 0) return 0;
            n = to.AddToSlot(slot, item, n);
            if (n > 0) from.Remove(item, n);
            return n;
        }
    }
}
