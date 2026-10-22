using System;

namespace Game.Core
{
    /// <summary>
    /// Túi đồ của người chơi (0028): khối theo vật liệu + hạt theo loại cây (chỉ số trong SimConfig.crops).
    /// Khác kho của sinh vật, không có mức tối đa (0004).
    /// </summary>
    public sealed class PlayerBag
    {
        readonly int[] counts = new int[256];
        readonly int[] seeds = new int[256];

        /// <summary>(loại cây, số hạt mới).</summary>
        public event Action<int, int> SeedsChanged;

        public event Action<BlockMaterial, int> Changed;

        public int Get(BlockMaterial material) => counts[(byte)material];

        public void Add(BlockMaterial material, int amount = 1)
        {
            if (amount < 0)
                throw new ArgumentOutOfRangeException(nameof(amount), amount, "Số lượng thêm phải ≥ 0");
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
                throw new ArgumentOutOfRangeException(nameof(amount), amount, "Số lượng thêm phải ≥ 0");
            seeds[crop] += amount;
            SeedsChanged?.Invoke(crop, seeds[crop]);
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
                throw new ArgumentOutOfRangeException(nameof(crop), crop, "Loại cây phải trong 0..255");
        }
    }
}
