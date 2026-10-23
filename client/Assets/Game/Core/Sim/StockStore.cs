using System;

namespace Game.Core.Sim
{
    /// <summary>Kho chung (Stockpile) nhìn như IItemStore: chỉ hạt (SeedBag) + nông sản (Produce), giới hạn Stock.Free (0028, 0034).</summary>
    public sealed class StockStore : IItemStore
    {
        readonly Simulation sim;

        public StockStore(Simulation sim)
        {
            this.sim = sim ?? throw new ArgumentNullException(nameof(sim));
        }

        static void CheckAmount(int amount)
        {
            if (amount < 0)
                throw new ArgumentOutOfRangeException(nameof(amount), amount, "Amount must be >= 0");
        }

        // Mảng đếm của loại vật phẩm; null nếu kho không nhận loại này.
        int[] ArrayOf(ItemId item)
        {
            Stockpile s = sim.State.Stock;
            int[] a;
            if (item.Kind == ItemKind.SeedBag) a = s.Seeds;
            else if (item.Kind == ItemKind.Produce) a = s.Produce;
            else return null;
            return item.Crop < a.Length ? a : null;
        }

        public int Count(ItemId item)
        {
            int[] a = ArrayOf(item);
            return a == null ? 0 : a[item.Crop];
        }

        public int CanAdd(ItemId item, int amount)
        {
            CheckAmount(amount);
            if (ArrayOf(item) == null) return 0;
            return Math.Min(amount, Math.Max(0, sim.State.Stock.Free));
        }

        public int Add(ItemId item, int amount)
        {
            int n = CanAdd(item, amount);
            if (n > 0) ArrayOf(item)[item.Crop] += n;
            return n;
        }

        public int Remove(ItemId item, int amount)
        {
            CheckAmount(amount);
            int n = Math.Min(amount, Count(item));
            if (n > 0) ArrayOf(item)[item.Crop] -= n;
            return n;
        }
    }
}
