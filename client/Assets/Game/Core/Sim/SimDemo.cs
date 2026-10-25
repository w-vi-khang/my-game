using System;
using System.Collections.Generic;

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

        /// <summary>Ao 4 × 4 viền cát (0039 mục 4): (dx, 0, dz) tính từ tâm đồng bằng. [số tạm]</summary>
        public static readonly CellPos PlainPondMin = new CellPos(10, 0, -12);
        public const int PondSize = 4;

        /// <summary>Nhà thứ ba cho trại có môi trường sống (0039). [số tạm]</summary>
        public static readonly CellPos PlainHouseC = new CellPos(6, 1, 4);

        /// <summary>Ao 4 × 4 viền cát (0039 mục 4): nước ở y = plain.Ground, vòng cát 1 ô quanh ao cùng tầng. Gọi sau FlattenPlain, trước khi dựng mesh.</summary>
        public static void PlacePond(WorldData world, StressPlain plain)
        {
            int cx = plain.X0 + plain.Size / 2, cz = plain.Z0 + plain.Size / 2;
            int wx0 = cx + PlainPondMin.X, wz0 = cz + PlainPondMin.Z;
            ushort water = Cell.Make(BlockMaterial.Water, Cell.MaxWaterLevel), sand = Cell.Make(BlockMaterial.Sand);
            for (int z = wz0 - 1; z <= wz0 + PondSize; z++)
                for (int x = wx0 - 1; x <= wx0 + PondSize; x++)
                {
                    bool inside = x >= wx0 && x < wx0 + PondSize && z >= wz0 && z < wz0 + PondSize;
                    world.Set(x, plain.Ground, z, inside ? water : sand);
                }
        }

        /// <summary>Thác nước demo cạnh ao (0041): khối đất, nguồn trên đỉnh, rãnh 1 ô ra mép nhô trên ao. Gọi sau PlacePond. Trả ô nguồn (toạ độ world) để gọi WorldEditor.WakeWater.</summary>
        public static CellPos PlaceWaterfall(WorldData world, StressPlain plain)
        {
            int cx = plain.X0 + plain.Size / 2, cz = plain.Z0 + plain.Size / 2, g = plain.Ground;
            ushort dirt = Cell.Make(BlockMaterial.Dirt);
            for (int y = g + 1; y <= g + 5; y++)
                for (int z = cz - 12; z <= cz - 10; z++)
                    for (int x = cx + 15; x <= cx + 18; x++)
                        world.Set(x, y, z, dirt);
            var source = new CellPos(cx + 17, g + 5, cz - 11);
            world.Set(source.X, source.Y, source.Z, Cell.Make(BlockMaterial.Water, Cell.MaxWaterLevel));
            world.Set(cx + 16, g + 5, cz - 11, Cell.Make(BlockMaterial.Empty));
            world.Set(cx + 15, g + 5, cz - 11, Cell.Make(BlockMaterial.Empty));
            world.Set(cx + 14, g + 4, cz - 11, dirt);
            world.Set(cx + 14, g + 5, cz - 12, dirt);
            world.Set(cx + 14, g + 5, cz - 10, dirt);
            world.Set(cx + 14, g + 5, cz - 11, Cell.Make(BlockMaterial.Empty));
            return source;
        }

        /// <summary>Bố trí giãn + nhà C; chỉ tạo sinh vật cho loài trong startSpecies (theo thứ tự Species, ô PlainCreatureCells tương ứng); nhà = nhà đầu tiên (A, B, C) còn chỗ. 4 ô ruộng + vụ Cây A như Build. Trả id kho.</summary>
        public static int BuildHabitat(Simulation sim, StressPlain plain, IReadOnlyList<string> startSpecies)
        {
            CellPos At(CellPos c) => OnPlain(plain, c);
            sim.AddHouse(At(PlainHouseA));
            sim.AddHouse(At(PlainHouseB));
            sim.AddHouse(At(PlainHouseC));
            int storage = sim.AddStorage(At(PlainStorage));
            for (int i = 0; i < Species.Length; i++)
            {
                bool start = false;
                for (int k = 0; k < startSpecies.Count; k++) if (startSpecies[k] == Species[i]) start = true;
                if (!start) continue;
                int id = sim.AddCreature(Species[i], At(PlainCreatureCells[i]));
                for (int h = 0; h < sim.State.Houses.Count; h++)
                    if (sim.State.Houses[h].Residents.Count < sim.State.Houses[h].Capacity)
                    {
                        sim.AssignHome(id, h);
                        break;
                    }
            }
            for (int i = 0; i < PlainSoil.Length; i++)
            {
                int id = sim.MarkTill(At(PlainSoil[i]));
                if (id < 0) throw new InvalidOperationException("Could not mark demo field cell " + At(PlainSoil[i]));
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
