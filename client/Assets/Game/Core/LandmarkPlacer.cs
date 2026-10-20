using System;
using System.Collections.Generic;
using Game.Core.Sim;

namespace Game.Core
{
    public enum LandmarkKind : byte { Checkpoint = 0, Structure = 1 }

    /// <summary>Một dấu mốc trên bản đồ (0023): checkpoint hoặc công trình đặc trưng ở cột (X, Z); Surface = ô rắn cao nhất của cột lúc đặt.</summary>
    public readonly struct Landmark
    {
        public readonly LandmarkKind Kind;
        public readonly int Map;
        /// <summary>Chỉ số trong WorldMapParams.landmarkTypes; −1 với checkpoint.</summary>
        public readonly int Type;
        public readonly int X, Z, Surface;
        public readonly string Name;

        public Landmark(LandmarkKind kind, int map, int type, int x, int z, int surface, string name)
        {
            Kind = kind;
            Map = map;
            Type = type;
            X = x;
            Z = z;
            Surface = surface;
            Name = name;
        }

        public override string ToString() => $"{Kind} {Name} map={Map} type={Type} ({X},{Z}) y={Surface}";
    }

    /// <summary>
    /// Đặt dấu mốc sau khi sinh world (world-tech 7d, decision 0023, brief 0017): 1 checkpoint mỗi map (ứng viên gần trọng tâm map nhất)
    /// + công trình đặc trưng theo landmarkTypes (rút PCG32 theo seed). Chỉ đọc world/bảng map, số nguyên → tất định, không đổi mã băm world.
    /// </summary>
    public static class LandmarkPlacer
    {
        /// <summary>Luồng PCG32 riêng của dấu mốc ("LAND").</summary>
        public const ulong RngStream = 0x4C414E44UL;

        static readonly int[] Dx = { 1, -1, 0, 0 };
        static readonly int[] Dz = { 0, 0, 1, -1 };

        public static List<Landmark> Place(WorldData world, MapTable maps, WorldMapParams p)
        {
            var s = world.Size;
            if (maps.SizeX != s.X || maps.SizeZ != s.Z)
                throw new ArgumentException("Bảng map khác kích thước world");
            int sx = s.X, sz = s.Z;
            var surface = new int[sx * sz];
            var dry = new bool[sx * sz];
            for (int z = 0; z < sz; z++)
                for (int x = 0; x < sx; x++)
                {
                    MinimapImage.ColumnInfo(world, x, z, out int surf, out int depth);
                    int i = x + sx * z;
                    surface[i] = surf;
                    dry[i] = depth == 0 && surf >= p.waterLevel;
                }

            var dist = BorderDistance(maps);
            var candidates = new List<int>[maps.MapCount];
            for (int m = 0; m < candidates.Length; m++)
                candidates[m] = new List<int>();
            for (int z = 1; z < sz - 1; z++)
                for (int x = 1; x < sx - 1; x++)
                {
                    int i = x + sx * z;
                    byte id = maps.Get(x, z);
                    if (id == MapTable.Ocean || id >= maps.MapCount || dist[i] < p.landmarkBorderMargin || !IsFlatDry(surface, dry, sx, x, z))
                        continue;
                    candidates[id].Add(i);
                }

            var result = new List<Landmark>();
            for (int m = 0; m < maps.MapCount; m++)
            {
                long sumX = 0, sumZ = 0, n = 0;
                for (int z = 0; z < sz; z++)
                    for (int x = 0; x < sx; x++)
                        if (maps.Get(x, z) == m)
                        {
                            sumX += x;
                            sumZ += z;
                            n++;
                        }
                if (n == 0 || candidates[m].Count == 0)
                    throw new InvalidOperationException($"{maps.Name(m)}: không có chỗ đặt checkpoint");
                long cx2 = 2 * sumX + n, cz2 = 2 * sumZ + n;
                int best = -1;
                long bestD = long.MaxValue;
                foreach (int i in candidates[m])
                {
                    long dx = (2L * (i % sx) + 1) * n - cx2;
                    long dz = (2L * (i / sx) + 1) * n - cz2;
                    long d = dx * dx + dz * dz;
                    if (d < bestD)
                    {
                        bestD = d;
                        best = i;
                    }
                }
                result.Add(new Landmark(LandmarkKind.Checkpoint, m, -1, best % sx, best / sx, surface[best], "Checkpoint " + maps.Name(m)));
            }

            var rng = new SimRng(p.seed, RngStream);
            long minD2 = (long)p.landmarkSpacing * p.landmarkSpacing;
            var pool = new List<int>();
            for (int t = 0; t < p.landmarkTypes.Length; t++)
            {
                var type = p.landmarkTypes[t];
                if (type.map >= maps.MapCount)
                    throw new ArgumentException($"landmarkTypes[{t}].map ngoài bảng map");
                for (int k = 0; k < type.count; k++)
                {
                    pool.Clear();
                    foreach (int i in candidates[type.map])
                    {
                        int x = i % sx, z = i / sx;
                        bool far = true;
                        foreach (var l in result)
                        {
                            long dx = x - l.X, dz = z - l.Z;
                            if (dx * dx + dz * dz < minD2)
                            {
                                far = false;
                                break;
                            }
                        }
                        if (far)
                            pool.Add(i);
                    }
                    if (pool.Count == 0)
                        throw new InvalidOperationException($"{maps.Name(type.map)}: không còn chỗ đặt {type.name}");
                    int pick = pool[rng.NextInt(pool.Count)];
                    string name = type.count > 1 ? $"{type.name} {k + 1}" : type.name;
                    result.Add(new Landmark(LandmarkKind.Structure, type.map, t, pick % sx, pick / sx, surface[pick], name));
                }
            }
            return result;
        }

        /// <summary>
        /// Khoảng cách BFS 4 hướng, chỉ đi trong cùng map, tới cột biên. Cột biên (giá trị 0) là cột biển, cột sát biên world,
        /// hoặc cột có láng giềng 4 hướng thuộc map khác/biển. Chỉ số x + SizeX · z.
        /// </summary>
        public static int[] BorderDistance(MapTable maps)
        {
            int sx = maps.SizeX, sz = maps.SizeZ;
            var dist = new int[sx * sz];
            var queue = new int[sx * sz];
            int head = 0, tail = 0;
            for (int z = 0; z < sz; z++)
                for (int x = 0; x < sx; x++)
                {
                    int i = x + sx * z;
                    byte id = maps.Get(x, z);
                    bool border = id == MapTable.Ocean || x == 0 || z == 0 || x == sx - 1 || z == sz - 1
                                  || maps.Get(x - 1, z) != id || maps.Get(x + 1, z) != id
                                  || maps.Get(x, z - 1) != id || maps.Get(x, z + 1) != id;
                    if (border)
                    {
                        dist[i] = 0;
                        queue[tail++] = i;
                    }
                    else
                        dist[i] = -1;
                }
            while (head < tail)
            {
                int i = queue[head++];
                int x = i % sx, z = i / sx;
                byte id = maps.Get(x, z);
                for (int k = 0; k < 4; k++)
                {
                    int nx = x + Dx[k], nz = z + Dz[k];
                    if (nx < 0 || nz < 0 || nx >= sx || nz >= sz)
                        continue;
                    int j = nx + sx * nz;
                    if (dist[j] >= 0 || maps.Get(nx, nz) != id)
                        continue;
                    dist[j] = dist[i] + 1;
                    queue[tail++] = j;
                }
            }
            return dist;
        }

        /// <summary>Cột (x, z) và 8 cột quanh khô (không nước, mặt ≥ mực nước) và chênh độ cao với cột giữa ≤ 1. Cần 1 ≤ x ≤ sx − 2, tương tự z.</summary>
        static bool IsFlatDry(int[] surface, bool[] dry, int sx, int x, int z)
        {
            int c = x + sx * z;
            if (!dry[c])
                return false;
            for (int dz = -1; dz <= 1; dz++)
                for (int dx = -1; dx <= 1; dx++)
                {
                    int j = x + dx + sx * (z + dz);
                    if (!dry[j] || Math.Abs(surface[j] - surface[c]) > 1)
                        return false;
                }
            return true;
        }
    }
}
