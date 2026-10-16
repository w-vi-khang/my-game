namespace Game.Core.Sim
{
    /// <summary>
    /// Bố trí demo cho scene SimView (sim bước 6) trên map M1 (world_m1.json, seed 20261013): mặt đất y = 17 quanh x 30–36, z 16–20
    /// (bản đồ độ cao chạy thật 2026-10-16). 2 nhà, 1 kho, 4 ô ruộng (một lệnh trồng cây); L1 + L2 ở nhà A, L3 ở nhà B.
    /// </summary>
    public static class SimDemo
    {
        public static readonly CellPos HouseA = new CellPos(30, 18, 19);
        public static readonly CellPos HouseB = new CellPos(32, 18, 19);
        public static readonly CellPos StorageCell = new CellPos(34, 18, 17);
        public static readonly CellPos[] Soil =
            { new CellPos(30, 17, 16), new CellPos(31, 17, 16), new CellPos(30, 17, 17), new CellPos(31, 17, 17) };
        public static readonly string[] Species = { "L1", "L2", "L3" };
        public static readonly CellPos[] CreatureCells =
            { new CellPos(32, 18, 16), new CellPos(33, 18, 16), new CellPos(36, 18, 16) };

        /// <summary>Gọi một lần trên mô phỏng mới (chưa Tick). Trả id kho.</summary>
        public static int Build(Simulation sim)
        {
            int a = sim.AddHouse(HouseA);
            int b = sim.AddHouse(HouseB);
            int storage = sim.AddStorage(StorageCell);
            for (int i = 0; i < Species.Length; i++)
                sim.AddCreature(Species[i], CreatureCells[i]);
            sim.AssignHome(0, a);
            sim.AssignHome(1, a);
            sim.AssignHome(2, b);
            sim.AddFarmOrder(Soil, storage);
            return storage;
        }

        /// <summary>Ô ruộng nào hết vụ trong hàng đợi → thêm một vụ (trang trại demo chạy mãi). Gọi trước mỗi Tick().</summary>
        public static void KeepFarming(Simulation sim)
        {
            var ps = sim.State.Plots;
            for (int i = 0; i < ps.Count; i++)
                if (ps[i].Queue.Count == 0) sim.EnqueueCrop(ps[i].Id);
        }
    }
}
