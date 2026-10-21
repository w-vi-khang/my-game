using System;
using System.Collections.Generic;

namespace Game.Core.Sim
{
    /// <summary>Chỗ đặt trang trại demo trên world nhiều map (world 7e, brief 0019): cửa sổ gần phẳng, độ cao theo từng cột.</summary>
    public readonly struct DemoSite
    {
        /// <summary>Độ dời (x, z) cộng vào mọi ô của bố trí gốc (đảo v2).</summary>
        public readonly int Dx, Dz;
        public readonly SimBounds Bounds;
        readonly int[] heights;

        /// <param name="heights">9 × 7 độ cao mặt đất của cửa sổ, chỉ số rx + SiteSizeX · rz.</param>
        public DemoSite(int dx, int dz, int[] heights, SimBounds bounds)
        {
            if (heights == null || heights.Length != SimDemo.SiteSizeX * SimDemo.SiteSizeZ)
                throw new ArgumentException("heights phải có " + SimDemo.SiteSizeX * SimDemo.SiteSizeZ + " phần tử");
            Dx = dx;
            Dz = dz;
            this.heights = heights;
            Bounds = bounds;
        }

        /// <summary>Độ cao mặt đất của cột world (x, z) trong cửa sổ.</summary>
        public int Height(int x, int z)
        {
            int rx = x - (Dx + SimDemo.BaseX - 1), rz = z - (Dz + SimDemo.BaseZ - 1);
            if (rx < 0 || rx >= SimDemo.SiteSizeX || rz < 0 || rz >= SimDemo.SiteSizeZ)
                throw new ArgumentOutOfRangeException(nameof(x), "Cột ngoài cửa sổ: (" + x + ", " + z + ")");
            return heights[rx + SimDemo.SiteSizeX * rz];
        }

        public CellPos Shift(CellPos c) =>
            new CellPos(c.X + Dx, Height(c.X + Dx, c.Z + Dz) + (c.Y - SimDemo.BaseGround), c.Z + Dz);

        public override string ToString()
        {
            int min = int.MaxValue, max = int.MinValue;
            foreach (var h in heights)
            {
                if (h < min) min = h;
                if (h > max) max = h;
            }
            return $"dời ({Dx}, {Dz}), mặt y {min}–{max}, vùng {Bounds}";
        }
    }

    /// <summary>
    /// Bố trí demo cho scene SimView (sim bước 6) trên map M1 (world_m1.json, seed 20261013): mặt đất y = 17 quanh x 30–36, z 16–20
    /// (bản đồ độ cao chạy thật 2026-10-16). 2 nhà, 1 kho, 4 ô ruộng (một lệnh trồng cây); L1 + L2 ở nhà A, L3 ở nhà B.
    /// World nhiều map: FindSite + Build(sim, site) dời bố trí tới chỗ phẳng gần checkpoint Map 1 (world 7e, brief 0019).
    /// </summary>
    public static class SimDemo
    {
        /// <summary>Góc (x, z) nhỏ nhất của bố trí gốc và độ cao ô ruộng gốc (đảo v2).</summary>
        public const int BaseX = 30, BaseZ = 16, BaseGround = 17;
        /// <summary>Cửa sổ phẳng: bố trí chiếm x 30–36, z 16–20, thêm 1 cột lề mỗi phía.</summary>
        public const int SiteSizeX = 9, SiteSizeZ = 7;
        /// <summary>Dấu mốc phải cách cửa sổ ít nhất bấy nhiêu cột.</summary>
        public const int LandmarkClearance = 2;

        public static readonly CellPos HouseA = new CellPos(30, 18, 19);
        public static readonly CellPos HouseB = new CellPos(32, 18, 19);
        public static readonly CellPos StorageCell = new CellPos(34, 18, 17);
        public static readonly CellPos[] Soil =
            { new CellPos(30, 17, 16), new CellPos(31, 17, 16), new CellPos(30, 17, 17), new CellPos(31, 17, 17) };
        public static readonly string[] Species = { "L1", "L2", "L3" };
        public static readonly CellPos[] CreatureCells =
            { new CellPos(32, 18, 16), new CellPos(33, 18, 16), new CellPos(36, 18, 16) };

        /// <summary>Gọi một lần trên mô phỏng mới (chưa Tick). Trả id kho.</summary>
        public static int Build(Simulation sim) => Build(sim, c => c);

        /// <summary>Bố trí demo dời tới chỗ site (world nhiều map). Trả id kho.</summary>
        public static int Build(Simulation sim, DemoSite site) => Build(sim, site.Shift);

        static int Build(Simulation sim, Func<CellPos, CellPos> at)
        {
            int a = sim.AddHouse(at(HouseA));
            int b = sim.AddHouse(at(HouseB));
            int storage = sim.AddStorage(at(StorageCell));
            for (int i = 0; i < Species.Length; i++)
                sim.AddCreature(Species[i], at(CreatureCells[i]));
            sim.AssignHome(0, a);
            sim.AssignHome(1, a);
            sim.AssignHome(2, b);
            var soil = new CellPos[Soil.Length];
            for (int i = 0; i < Soil.Length; i++)
                soil[i] = at(Soil[i]);
            sim.AddFarmOrder(soil, storage);
            return storage;
        }

        /// <summary>
        /// Tìm chỗ đặt trang trại demo (brief 0019): cửa sổ 9 × 7 cột trong map, gần phẳng (cột kề chênh ≤ 1, nền 2 nhà phẳng), khô, mặt là cỏ/đất, không dấu mốc trong lề 2 cột;
        /// tâm gần tâm checkpoint của map nhất (bằng nhau → gặp trước, duyệt z rồi x). Vùng = hộp regionSize quanh tâm cửa sổ, kẹp trong world.
        /// </summary>
        public static DemoSite FindSite(WorldData world, MapTable maps, IReadOnlyList<Landmark> landmarks, int map, int waterLevel, int regionSize)
        {
            var s = world.Size;
            int sx = s.X, sz = s.Z;
            if (regionSize < SiteSizeX || regionSize > sx || regionSize > sz)
                throw new ArgumentException("regionSize phải trong [" + SiteSizeX + ", cạnh world]");
            var surface = new int[sx * sz];
            var ok = new bool[sx * sz];
            for (int z = 0; z < sz; z++)
                for (int x = 0; x < sx; x++)
                {
                    MinimapImage.ColumnInfo(world, x, z, out int surf, out int depth);
                    int i = x + sx * z;
                    surface[i] = surf;
                    var m = surf >= 0 ? world.GetMaterial(x, surf, z) : BlockMaterial.Empty;
                    ok[i] = maps.Get(x, z) == map && depth == 0 && surf >= waterLevel
                            && (m == BlockMaterial.Grass || m == BlockMaterial.Dirt);
                }

            int cpx = -1, cpz = -1;
            for (int k = 0; k < landmarks.Count; k++)
                if (landmarks[k].Kind == LandmarkKind.Checkpoint && landmarks[k].Map == map)
                {
                    cpx = landmarks[k].X;
                    cpz = landmarks[k].Z;
                    break;
                }
            if (cpx < 0)
                throw new InvalidOperationException("Không có checkpoint của map " + map);

            long bestD = long.MaxValue;
            int bx = -1, bz = -1;
            for (int wz = 0; wz + SiteSizeZ <= sz; wz++)
                for (int wx = 0; wx + SiteSizeX <= sx; wx++)
                {
                    long ddx = 2L * wx + SiteSizeX - (2L * cpx + 1);
                    long ddz = 2L * wz + SiteSizeZ - (2L * cpz + 1);
                    long d = ddx * ddx + ddz * ddz;
                    if (d >= bestD || !IsSiteWalkable(surface, ok, sx, wx, wz) || NearLandmark(landmarks, wx, wz))
                        continue;
                    bestD = d;
                    bx = wx;
                    bz = wz;
                }
            if (bx < 0)
                throw new InvalidOperationException("Không có chỗ phẳng " + SiteSizeX + " × " + SiteSizeZ + " cho trang trại demo trong map " + map);

            var heights = new int[SiteSizeX * SiteSizeZ];
            for (int dz = 0; dz < SiteSizeZ; dz++)
                for (int dx = 0; dx < SiteSizeX; dx++)
                    heights[dx + SiteSizeX * dz] = surface[bx + dx + sx * (bz + dz)];
            int cx = bx + SiteSizeX / 2, cz = bz + SiteSizeZ / 2;
            int x0 = Math.Min(Math.Max(cx - regionSize / 2, 0), sx - regionSize);
            int z0 = Math.Min(Math.Max(cz - regionSize / 2, 0), sz - regionSize);
            return new DemoSite(bx + 1 - BaseX, bz + 1 - BaseZ, heights, new SimBounds(x0, z0, regionSize, regionSize));
        }

        static bool IsSiteWalkable(int[] surface, bool[] ok, int sx, int wx, int wz)
        {
            for (int dz = 0; dz < SiteSizeZ; dz++)
                for (int dx = 0; dx < SiteSizeX; dx++)
                {
                    int i = wx + dx + sx * (wz + dz);
                    if (!ok[i])
                        return false;
                    if (dx + 1 < SiteSizeX && Math.Abs(surface[i] - surface[i + 1]) > 1)
                        return false;
                    if (dz + 1 < SiteSizeZ && Math.Abs(surface[i] - surface[i + sx]) > 1)
                        return false;
                }
            return FlatSquare(surface, sx, wx + 1, wz + 4) && FlatSquare(surface, sx, wx + 3, wz + 4);
        }

        static bool FlatSquare(int[] surface, int sx, int x, int z)
        {
            int h = surface[x + sx * z];
            return surface[x + 1 + sx * z] == h && surface[x + sx * (z + 1)] == h && surface[x + 1 + sx * (z + 1)] == h;
        }

        static bool NearLandmark(IReadOnlyList<Landmark> landmarks, int wx, int wz)
        {
            for (int k = 0; k < landmarks.Count; k++)
            {
                var l = landmarks[k];
                if (l.X >= wx - LandmarkClearance && l.X < wx + SiteSizeX + LandmarkClearance
                    && l.Z >= wz - LandmarkClearance && l.Z < wz + SiteSizeZ + LandmarkClearance)
                    return true;
            }
            return false;
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
