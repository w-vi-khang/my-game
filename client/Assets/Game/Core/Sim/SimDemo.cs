using System;

namespace Game.Core.Sim
{
    /// <summary>
    /// Bố trí demo cho scene SimView (sim bước 6) trên map M1 (world_m1.json, seed 20261013): mặt đất y = 17 quanh x 30–36, z 16–20
    /// (bản đồ độ cao chạy thật 2026-10-16). 2 nhà, 1 kho, 4 ô ruộng (một lệnh trồng cây); L1 + L2 ở nhà A, L3 ở nhà B.
    /// World nhiều map: Build(sim, plain) đặt bố trí giãn giữa đồng bằng san phẳng (task m1-demo-dong-bang).
    /// </summary>
    public static class SimDemo
    {
        /// <summary>Góc (x, z) nhỏ nhất của bố trí gốc và độ cao ô ruộng gốc (đảo v2).</summary>
        public const int BaseX = 30, BaseZ = 16, BaseGround = 17;

        public static readonly CellPos HouseA = new CellPos(30, 18, 19);
        public static readonly CellPos HouseB = new CellPos(32, 18, 19);
        public static readonly CellPos StorageCell = new CellPos(34, 18, 17);
        public static readonly CellPos[] Soil =
            { new CellPos(30, 17, 16), new CellPos(31, 17, 16), new CellPos(30, 17, 17), new CellPos(31, 17, 17) };
        public static readonly string[] Species = { "L1", "L2", "L3" };
        public static readonly CellPos[] CreatureCells =
            { new CellPos(32, 18, 16), new CellPos(33, 18, 16), new CellPos(36, 18, 16) };

        /// <summary>Bố trí giãn trên đồng bằng (task m1-demo-dong-bang): (dx, y, dz) tính từ tâm đồng bằng; y 0 = ô mặt đất, 1 = ô trên mặt đất.</summary>
        public static readonly CellPos PlainHouseA = new CellPos(-4, 1, 4);
        public static readonly CellPos PlainHouseB = new CellPos(1, 1, 4);
        public static readonly CellPos PlainStorage = new CellPos(5, 1, -1);
        public static readonly CellPos[] PlainSoil =
            { new CellPos(-6, 0, -2), new CellPos(-5, 0, -2), new CellPos(-6, 0, -1), new CellPos(-5, 0, -1) };
        public static readonly CellPos[] PlainCreatureCells =
            { new CellPos(-3, 1, 2), new CellPos(-2, 1, 2), new CellPos(2, 1, 2) };

        /// <summary>Ô thế giới của ô tương đối rel trong bố trí giãn.</summary>
        public static CellPos OnPlain(StressPlain plain, CellPos rel) =>
            new CellPos(plain.X0 + plain.Size / 2 + rel.X, plain.Ground + rel.Y, plain.Z0 + plain.Size / 2 + rel.Z);

        /// <summary>Bố trí đảo v2. Gọi một lần trên mô phỏng mới (chưa Tick). Trả id kho.</summary>
        public static int Build(Simulation sim) => Build(sim, c => c, HouseA, HouseB, StorageCell, CreatureCells, Soil);

        /// <summary>Bố trí giãn giữa đồng bằng (task m1-demo-dong-bang). Trả id kho.</summary>
        public static int Build(Simulation sim, StressPlain plain) =>
            Build(sim, c => OnPlain(plain, c), PlainHouseA, PlainHouseB, PlainStorage, PlainCreatureCells, PlainSoil);

        static int Build(Simulation sim, Func<CellPos, CellPos> at, CellPos houseA, CellPos houseB, CellPos storageCell, CellPos[] creatureCells, CellPos[] soilCells)
        {
            int a = sim.AddHouse(at(houseA));
            int b = sim.AddHouse(at(houseB));
            int storage = sim.AddStorage(at(storageCell));
            for (int i = 0; i < Species.Length; i++)
                sim.AddCreature(Species[i], at(creatureCells[i]));
            sim.AssignHome(0, a);
            sim.AssignHome(1, a);
            sim.AssignHome(2, b);
            var soil = new CellPos[soilCells.Length];
            for (int i = 0; i < soilCells.Length; i++)
                soil[i] = at(soilCells[i]);
            for (int i = 0; i < soil.Length; i++)
            {
                int id = sim.MarkTill(soil[i]);
                if (id < 0) throw new InvalidOperationException("Could not mark demo field cell " + soil[i]);
                sim.EnqueueCrop(id, 0);
            }
            return storage;
        }

        /// <summary>Ô ruộng không còn ô đợi và không có vụ hiện tại → thêm một vụ Cây A (trang trại demo chạy mãi); ô đã huỷ vụ thì bỏ qua. Gọi trước mỗi Tick().</summary>
        public static void KeepFarming(Simulation sim)
        {
            var ps = sim.State.Plots;
            for (int i = 0; i < ps.Count; i++)
                if (!ps[i].Removed && ps[i].Slots.Count == 0 && ps[i].CurrentCrop < 0) sim.EnqueueCrop(ps[i].Id, 0);
        }
    }
}
