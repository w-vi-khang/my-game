using System;

namespace Game.Core
{
    /// <summary>Một ô: vật phẩm + số lượng; Count 0 = ô trống.</summary>
    public readonly struct ItemStack
    {
        public readonly ItemId Item;
        public readonly int Count;
        public ItemStack(ItemId item, int count) { Item = item; Count = count; }
        public bool IsEmpty => Count <= 0;
    }

    /// <summary>Kho chứa đếm theo loại vật phẩm (túi, túi phụ: SlotInventory; kho chung: StockStore).</summary>
    public interface IItemStore
    {
        int Count(ItemId item);
        /// <summary>Số thêm được, 0..amount.</summary>
        int CanAdd(ItemId item, int amount);
        /// <summary>Thêm min(amount, CanAdd); trả số đã thêm.</summary>
        int Add(ItemId item, int amount);
        /// <summary>Lấy min(amount, Count); trả số đã lấy.</summary>
        int Remove(ItemId item, int amount);
    }

    /// <summary>Túi ô vuông (0034 mục 6): Capacity ô, mỗi ô một loại, tối đa StackMax; trang PageSize ô, trang vòng.</summary>
    public sealed class SlotInventory : IItemStore
    {
        readonly ItemId[] items;
        readonly int[] counts;

        public SlotInventory(int capacity, int pageSize, int stackMax)
        {
            if (capacity < 1) throw new ArgumentOutOfRangeException(nameof(capacity), capacity, "Capacity must be >= 1");
            if (pageSize < 1) throw new ArgumentOutOfRangeException(nameof(pageSize), pageSize, "Page size must be >= 1");
            if (stackMax < 1) throw new ArgumentOutOfRangeException(nameof(stackMax), stackMax, "Stack max must be >= 1");
            Capacity = capacity;
            PageSize = pageSize;
            StackMax = stackMax;
            items = new ItemId[capacity];
            counts = new int[capacity];
        }

        public int Capacity { get; }
        public int PageSize { get; }
        public int StackMax { get; }
        public int PageCount => (Capacity + PageSize - 1) / PageSize;
        /// <summary>Phát với chỉ số ô vừa đổi (mỗi ô đổi một lần mỗi lệnh).</summary>
        public event Action<int> SlotChanged;

        void CheckPage(int page)
        {
            if (page < 0 || page >= PageCount)
                throw new ArgumentOutOfRangeException(nameof(page), page, "Page out of range");
        }

        void CheckSlot(int slot)
        {
            if (slot < 0 || slot >= Capacity)
                throw new ArgumentOutOfRangeException(nameof(slot), slot, "Slot out of range");
        }

        static void CheckAmount(int amount)
        {
            if (amount < 0)
                throw new ArgumentOutOfRangeException(nameof(amount), amount, "Amount must be >= 0");
        }

        public int PageStart(int page) { CheckPage(page); return page * PageSize; }
        public int PageEnd(int page) { CheckPage(page); return Math.Min(Capacity, (page + 1) * PageSize); }
        public int NextPage(int page) { CheckPage(page); return (page + 1) % PageCount; }
        public int PrevPage(int page) { CheckPage(page); return (page + PageCount - 1) % PageCount; }

        public ItemStack Get(int slot)
        {
            CheckSlot(slot);
            return new ItemStack(items[slot], counts[slot]);
        }

        public int Count(ItemId item)
        {
            int n = 0;
            for (int i = 0; i < Capacity; i++)
                if (counts[i] > 0 && items[i] == item) n += counts[i];
            return n;
        }

        public int CanAdd(ItemId item, int amount)
        {
            CheckAmount(amount);
            long room = 0;
            for (int i = 0; i < Capacity; i++)
            {
                if (counts[i] == 0) room += StackMax;
                else if (items[i] == item) room += StackMax - counts[i];
            }
            return (int)Math.Min(amount, room);
        }

        public int Add(ItemId item, int amount)
        {
            CheckAmount(amount);
            int left = amount;
            for (int i = 0; i < Capacity && left > 0; i++)
                if (counts[i] > 0 && items[i] == item)
                    left -= Put(i, item, left);
            for (int i = 0; i < Capacity && left > 0; i++)
                if (counts[i] == 0)
                    left -= Put(i, item, left);
            return amount - left;
        }

        /// <summary>Chỉ lấp các ô đang có cùng loại (không mở ô trống), theo chỉ số tăng; trả số đã thêm.</summary>
        public int TopUp(ItemId item, int amount)
        {
            CheckAmount(amount);
            int left = amount;
            for (int i = 0; i < Capacity && left > 0; i++)
                if (counts[i] > 0 && items[i] == item)
                    left -= Put(i, item, left);
            return amount - left;
        }

        public int Remove(ItemId item, int amount)
        {
            CheckAmount(amount);
            int left = amount;
            for (int i = Capacity - 1; i >= 0 && left > 0; i--)
                if (counts[i] > 0 && items[i] == item)
                    left -= Take(i, left);
            return amount - left;
        }

        /// <summary>Ô trống hoặc cùng loại: thêm min(amount, StackMax − Count); khác loại → 0.</summary>
        public int AddToSlot(int slot, ItemId item, int amount)
        {
            CheckSlot(slot);
            CheckAmount(amount);
            if (counts[slot] > 0 && items[slot] != item) return 0;
            return Put(slot, item, amount);
        }

        /// <summary>Lấy min(amount, Count); hết → ô trống.</summary>
        public int RemoveFromSlot(int slot, int amount)
        {
            CheckSlot(slot);
            CheckAmount(amount);
            return Take(slot, amount);
        }

        /// <summary>Kéo thả: ô đích trống → cả chồng; cùng loại → gộp, dư ở lại; khác loại → đổi chỗ.</summary>
        public bool Move(int fromSlot, SlotInventory dest, int destSlot)
        {
            if (dest == null) throw new ArgumentNullException(nameof(dest));
            CheckSlot(fromSlot);
            dest.CheckSlot(destSlot);
            if (counts[fromSlot] == 0) return false;
            if (ReferenceEquals(dest, this) && destSlot == fromSlot) return false;
            ItemId item = items[fromSlot];
            int n = counts[fromSlot];
            if (dest.counts[destSlot] == 0)
            {
                if (n > dest.StackMax) return false;
                Take(fromSlot, n);
                dest.Put(destSlot, item, n);
                return true;
            }
            if (dest.items[destSlot] == item)
            {
                int m = Math.Min(n, dest.StackMax - dest.counts[destSlot]);
                if (m == 0) return false;
                Take(fromSlot, m);
                dest.Put(destSlot, item, m);
                return true;
            }
            ItemId otherItem = dest.items[destSlot];
            int otherCount = dest.counts[destSlot];
            if (n > dest.StackMax || otherCount > StackMax) return false;
            Take(fromSlot, n);
            dest.Take(destSlot, otherCount);
            Put(fromSlot, otherItem, otherCount);
            dest.Put(destSlot, item, n);
            return true;
        }

        // Đặt vào ô (đã đảm bảo trống hoặc cùng loại): trả số đặt được.
        int Put(int slot, ItemId item, int amount)
        {
            int n = Math.Min(amount, StackMax - counts[slot]);
            if (n <= 0) return 0;
            items[slot] = item;
            counts[slot] += n;
            SlotChanged?.Invoke(slot);
            return n;
        }

        int Take(int slot, int amount)
        {
            int n = Math.Min(amount, counts[slot]);
            if (n <= 0) return 0;
            counts[slot] -= n;
            SlotChanged?.Invoke(slot);
            return n;
        }
    }
}
