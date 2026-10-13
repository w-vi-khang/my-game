using System;

namespace Game.Core
{
    /// <summary>
    /// Kho vật liệu của người chơi (luật M1, design/02-gameplay/mechanics.md "Voxel world — luật M1"):
    /// một bộ đếm theo loại vật liệu, bắt đầu 0, chưa giới hạn (túi đồ đầy đủ là M2).
    /// </summary>
    public sealed class MaterialInventory
    {
        readonly int[] counts = new int[256];

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
    }
}
