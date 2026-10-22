using System;

namespace Game.Core
{
    /// <summary>
    /// Túi đồ của người chơi (0028): khối theo vật liệu + hạt + nông sản theo loại cây (chỉ số trong SimConfig.crops).
    /// Khác kho của sinh vật, không có mức tối đa (0004).
    /// </summary>
    public sealed class PlayerBag
    {
        readonly int[] counts = new int[256];
        readonly int[] seeds = new int[256];
        readonly int[] produce = new int[256];

        /// <summary>(loại cây, số hạt mới).</summary>
        public event Action<int, int> SeedsChanged;

        /// <summary>(loại cây, số nông sản mới).</summary>
        public event Action<int, int> ProduceChanged;

        public event Action<BlockMaterial, int> Changed;

        public int Get(BlockMaterial material) => counts[(byte)material];

        public void Add(BlockMaterial material, int amount = 1)
        {
            if (amount < 0)
                throw new ArgumentOutOfRangeException(nameof(amount), amount, "Amount to add must be ≥ 0");
            counts[(byte)material] += amount;
            Changed?.Invoke(material, counts[(byte)material]);
        }

        /// <summary>Lấy 1 đơn vị; false nếu hết.</summary>
        public bool TryTake(BlockMaterial material)
        {
            if (counts[(byte)material] <= 0)
                return false;
            counts[(byte)material]--;
            Changed?.Invoke(material, counts[(byte)material]);
            return true;
        }

        public int GetSeeds(int crop)
        {
            CheckCrop(crop);
            return seeds[crop];
        }

        public void AddSeeds(int crop, int amount)
        {
            CheckCrop(crop);
            if (amount < 0)
                throw new ArgumentOutOfRangeException(nameof(amount), amount, "Amount to add must be ≥ 0");
            seeds[crop] += amount;
            SeedsChanged?.Invoke(crop, seeds[crop]);
        }

        public int GetProduce(int crop)
        {
            CheckCrop(crop);
            return produce[crop];
        }

        public void AddProduce(int crop, int amount)
        {
            CheckCrop(crop);
            if (amount < 0)
                throw new ArgumentOutOfRangeException(nameof(amount), amount, "Amount to add must be ≥ 0");
            produce[crop] += amount;
            ProduceChanged?.Invoke(crop, produce[crop]);
        }

        /// <summary>Đủ thì trừ amount hạt, phát SeedsChanged, true; thiếu → false, không đổi. amount = 0 → true, không phát.</summary>
        public bool TryTakeSeeds(int crop, int amount)
        {
            CheckCrop(crop);
            if (amount < 0)
                throw new ArgumentOutOfRangeException(nameof(amount), amount, "Amount to take must be ≥ 0");
            if (seeds[crop] < amount) return false;
            if (amount == 0) return true;
            seeds[crop] -= amount;
            SeedsChanged?.Invoke(crop, seeds[crop]);
            return true;
        }

        /// <summary>Như TryTakeSeeds cho nông sản (phát ProduceChanged).</summary>
        public bool TryTakeProduce(int crop, int amount)
        {
            CheckCrop(crop);
            if (amount < 0)
                throw new ArgumentOutOfRangeException(nameof(amount), amount, "Amount to take must be ≥ 0");
            if (produce[crop] < amount) return false;
            if (amount == 0) return true;
            produce[crop] -= amount;
            ProduceChanged?.Invoke(crop, produce[crop]);
            return true;
        }

        /// <summary>Thêm vật phẩm: Block → Add(Material, amount); SeedBag → AddSeeds(Crop, amount).</summary>
        public void Add(ItemId item, int amount)
        {
            if (item.Kind == ItemKind.Block)
                Add(item.Material, amount);
            else
                AddSeeds(item.Crop, amount);
        }

        static void CheckCrop(int crop)
        {
            if (crop < 0 || crop > 255)
                throw new ArgumentOutOfRangeException(nameof(crop), crop, "Crop must be in 0..255");
        }
    }
}
