using System;
using System.Collections.Generic;

namespace Game.Core.Sim
{
    /// <summary>Đồng bằng cho bố trí đo (sim bước 7, brief 0028): góc (X0, Z0), cạnh Size, mặt cỏ ở y = Ground; Bounds = vùng mô phỏng.</summary>
    public readonly struct StressPlain
    {
        public readonly int X0, Z0, Size, Ground;
        public readonly SimBounds Bounds;
        public StressPlain(int x0, int z0, int size, int ground, SimBounds bounds) { X0 = x0; Z0 = z0; Size = size; Ground = ground; Bounds = bounds; }
        public bool Contains(int x, int z) => x >= X0 && x < X0 + Size && z >= Z0 && z < Z0 + Size;
        public override string ToString() => $"plain ({X0}, {Z0}) {Size} x {Size}, surface y {Ground}, region {Bounds}";
    }

    /// <summary>Bố trí đo sim bước 7 (brief 0028): đồng bằng 48 × 48 giữa một map, 30 nhà, 60 sinh vật (20 mỗi loài), 5 kho, 15 lệnh × 4 ô.</summary>
    public static class SimStress
    {
        public const int PlainSize = 48, HouseCount = 30, CreatureCount = 60, StorageCount = 5, OrderCount = 15;

        /// <summary>Làm phẳng đồng bằng giữa map (ghi thẳng vào world, không bật cờ đã sửa) và trả chỗ + vùng mô phỏng.</summary>
        public static StressPlain FlattenPlain(WorldData world, MapTable maps, int map, int bedrockLayers, int dirtDepth, int waterLevel, int regionSize)
        {
            var s = world.Size;
            int sx = s.X, sz = s.Z;
            if (regionSize < PlainSize || regionSize > Math.Min(sx, sz))
                throw new ArgumentException("regionSize must be in [" + PlainSize + ", smallest world side]");
            long sumX = 0, sumZ = 0, n = 0;
            for (int z = 0; z < sz; z++)
                for (int x = 0; x < sx; x++)
                    if (maps.Get(x, z) == map)
                    {
                        sumX += x;
                        sumZ += z;
                        n++;
                    }
            if (n == 0)
                throw new InvalidOperationException("Map " + map + " has no columns");
            int cx = (int)(sumX / n), cz = (int)(sumZ / n);
            int half = PlainSize / 2;
            int x0 = Math.Min(Math.Max(cx - half, 0), sx - PlainSize);
            int z0 = Math.Min(Math.Max(cz - half, 0), sz - PlainSize);

            long sum = 0;
            for (int z = z0; z < z0 + PlainSize; z++)
                for (int x = x0; x < x0 + PlainSize; x++)
                {
                    MinimapImage.ColumnInfo(world, x, z, out int surf, out _);
                    sum += surf < 0 ? waterLevel : surf;
                }
            int ground = (int)(sum / (PlainSize * PlainSize));
            ground = Math.Min(Math.Max(ground, waterLevel + 2), s.Y - 12);

            ushort grass = Cell.Make(BlockMaterial.Grass), dirt = Cell.Make(BlockMaterial.Dirt),
                   stone = Cell.Make(BlockMaterial.Stone), empty = Cell.Make(BlockMaterial.Empty);
            for (int z = z0; z < z0 + PlainSize; z++)
                for (int x = x0; x < x0 + PlainSize; x++)
                    for (int y = bedrockLayers; y < s.Y; y++)
                    {
                        ushort c;
                        if (y > ground) c = empty;
                        else if (y == ground) c = grass;
                        else if (y >= ground - dirtDepth) c = dirt;
                        else c = stone;
                        world.Set(x, y, z, c);
                    }

            int cxp = x0 + half, czp = z0 + half;
            int xr = Math.Min(Math.Max(cxp - regionSize / 2, 0), sx - regionSize);
            int zr = Math.Min(Math.Max(czp - regionSize / 2, 0), sz - regionSize);
            return new StressPlain(x0, z0, PlainSize, ground, new SimBounds(xr, zr, regionSize, regionSize));
        }

        /// <summary>Bỏ các dấu mốc nằm trong đồng bằng, giữ thứ tự cũ.</summary>
        public static List<Landmark> WithoutPlain(IReadOnlyList<Landmark> landmarks, StressPlain plain)
        {
            var result = new List<Landmark>(landmarks.Count);
            for (int i = 0; i < landmarks.Count; i++)
                if (!plain.Contains(landmarks[i].X, landmarks[i].Z))
                    result.Add(landmarks[i]);
            return result;
        }

        /// <summary>Đặt 30 nhà, 60 sinh vật, 5 kho, 15 nhóm 4 ô ruộng lên mô phỏng mới (chưa có sinh vật).</summary>
        public static void Build(Simulation sim, StressPlain plain)
        {
            if (sim.State.Creatures.Count != 0)
                throw new InvalidOperationException("Build may only be called on a new simulation");
            int y = plain.Ground + 1;
            var houseCells = new CellPos[HouseCount];
            for (int h = 0; h < HouseCount; h++)
            {
                houseCells[h] = new CellPos(plain.X0 + 4 + 4 * (h % 10), y, plain.Z0 + 4 + 6 * (h / 10));
                sim.AddHouse(houseCells[h]);
            }
            for (int i = 0; i < CreatureCount; i++)
            {
                var hc = houseCells[i / 2];
                sim.AddCreature(SimDemo.Species[i % 3], new CellPos(hc.X + i % 2, y, hc.Z - 1));
                sim.AssignHome(i, i / 2);
            }
            for (int k = 0; k < StorageCount; k++)
                sim.AddStorage(new CellPos(plain.X0 + 6 + 8 * k, y, plain.Z0 + 23));
            for (int o = 0; o < OrderCount; o++)
            {
                int j = o % 5, r = o / 5;
                int ox = plain.X0 + 4 + 8 * j, oz = plain.Z0 + 28 + 6 * r;
                var soil = new[]
                {
                    new CellPos(ox, plain.Ground, oz), new CellPos(ox + 1, plain.Ground, oz),
                    new CellPos(ox, plain.Ground, oz + 1), new CellPos(ox + 1, plain.Ground, oz + 1),
                };
                for (int q = 0; q < soil.Length; q++)
                {
                    int id = sim.MarkTill(soil[q]);
                    if (id >= 0) sim.EnqueueCrop(id, 0);
                }
            }
        }
    }
}
