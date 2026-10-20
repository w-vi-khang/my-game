using System;
using System.Collections.Generic;

namespace Game.Core
{
    /// <summary>
    /// Lớp vẽ đè lên ảnh nền minimap (world-tech bước 7c, brief 0015): đường ranh giới giữa các map và vị trí tên map.
    /// Toạ độ theo góc ô: cột (x, z) chiếm [x, x + 1] × [z, z + 1].
    /// </summary>
    public static class MinimapOverlay
    {
        /// <summary>Số lần làm mượt Chaikin mặc định của ranh giới.</summary>
        public const int SmoothIterations = 2;

        /// <summary>
        /// Ranh giới: mỗi đoạn đơn vị nằm giữa hai cột kề (4 hướng) thuộc hai map khác nhau, cả hai không phải biển.
        /// Nối đoạn thành đường gấp khúc giữa các nút bậc ≠ 2 (chỗ 3 map gặp nhau, chỗ ranh giới chạm biển hoặc biên world);
        /// vòng kín (mọi nút bậc 2) lặp lại điểm đầu ở cuối. Mỗi đường là mảng [x0, z0, x1, z1, …], làm mượt Chaikin
        /// smoothIterations lần (đường hở giữ nguyên điểm đầu/cuối).
        /// </summary>
        public static List<float[]> Borders(MapTable maps, int smoothIterations = SmoothIterations)
        {
            int sx = maps.SizeX, sz = maps.SizeZ, nx = sx + 1;
            var edgeA = new List<int>();
            var edgeB = new List<int>();
            // Cạnh dọc giữa cột (x, z) và (x + 1, z): góc (x + 1, z) – (x + 1, z + 1).
            for (int z = 0; z < sz; z++)
                for (int x = 0; x + 1 < sx; x++)
                    if (Separates(maps.Get(x, z), maps.Get(x + 1, z)))
                    {
                        edgeA.Add(x + 1 + nx * z);
                        edgeB.Add(x + 1 + nx * (z + 1));
                    }
            // Cạnh ngang giữa cột (x, z) và (x, z + 1): góc (x, z + 1) – (x + 1, z + 1).
            for (int z = 0; z + 1 < sz; z++)
                for (int x = 0; x < sx; x++)
                    if (Separates(maps.Get(x, z), maps.Get(x, z + 1)))
                    {
                        edgeA.Add(x + nx * (z + 1));
                        edgeB.Add(x + 1 + nx * (z + 1));
                    }

            // Nút = góc ô (cx + nx · cz); mỗi nút có tối đa 4 cạnh, theo thứ tự tạo cạnh.
            int nodeCount = nx * (sz + 1);
            var degree = new int[nodeCount];
            var incident = new int[nodeCount * 4];
            for (int e = 0; e < edgeA.Count; e++)
            {
                incident[edgeA[e] * 4 + degree[edgeA[e]]++] = e;
                incident[edgeB[e] * 4 + degree[edgeB[e]]++] = e;
            }

            var used = new bool[edgeA.Count];
            var lines = new List<float[]>();
            var pts = new List<int>();
            // Đường hở: bắt đầu ở nút bậc ≠ 2 theo chỉ số nút tăng dần, mỗi cạnh chưa dùng của nút đó theo thứ tự.
            for (int node = 0; node < nodeCount; node++)
            {
                if (degree[node] == 0 || degree[node] == 2)
                    continue;
                for (int k = 0; k < degree[node]; k++)
                {
                    int e = incident[node * 4 + k];
                    if (used[e])
                        continue;
                    pts.Clear();
                    pts.Add(node);
                    int cur = node;
                    while (true)
                    {
                        used[e] = true;
                        cur = edgeA[e] == cur ? edgeB[e] : edgeA[e];
                        pts.Add(cur);
                        if (degree[cur] != 2)
                            break;
                        int next = incident[cur * 4] == e ? incident[cur * 4 + 1] : incident[cur * 4];
                        if (used[next])
                            break;
                        e = next;
                    }
                    lines.Add(Smooth(ToCoords(pts, nx), false, smoothIterations));
                }
            }
            // Vòng kín còn lại: bắt đầu ở đầu A của cạnh chưa dùng đầu tiên.
            for (int e0 = 0; e0 < edgeA.Count; e0++)
            {
                if (used[e0])
                    continue;
                int start = edgeA[e0], cur = start, e = e0;
                pts.Clear();
                pts.Add(start);
                while (true)
                {
                    used[e] = true;
                    cur = edgeA[e] == cur ? edgeB[e] : edgeA[e];
                    pts.Add(cur);
                    if (cur == start)
                        break;
                    int next = incident[cur * 4] == e ? incident[cur * 4 + 1] : incident[cur * 4];
                    if (used[next])
                        break;
                    e = next;
                }
                lines.Add(Smooth(ToCoords(pts, nx), true, smoothIterations));
            }
            return lines;
        }

        /// <summary>
        /// Vị trí đặt tên mỗi map [x0, z0, x1, z1, …] (toạ độ góc ô): trọng tâm các cột của map (tâm cột = x + 0,5);
        /// cột chứa trọng tâm không thuộc map đó → tâm cột của map gần trọng tâm nhất (bằng nhau → cột gặp trước theo z rồi x).
        /// Map không có cột → NaN.
        /// </summary>
        public static float[] LabelPositions(MapTable maps)
        {
            int count = maps.MapCount;
            var sumX = new double[count];
            var sumZ = new double[count];
            var n = new long[count];
            for (int z = 0; z < maps.SizeZ; z++)
                for (int x = 0; x < maps.SizeX; x++)
                {
                    int id = maps.Get(x, z);
                    if (id == MapTable.Ocean || id >= count)
                        continue;
                    sumX[id] += x + 0.5;
                    sumZ[id] += z + 0.5;
                    n[id]++;
                }

            var pos = new float[count * 2];
            for (int m = 0; m < count; m++)
            {
                if (n[m] == 0)
                {
                    pos[2 * m] = pos[2 * m + 1] = float.NaN;
                    continue;
                }
                double cx = sumX[m] / n[m], cz = sumZ[m] / n[m];
                int ix = (int)Math.Floor(cx), iz = (int)Math.Floor(cz);
                if (ix >= 0 && iz >= 0 && ix < maps.SizeX && iz < maps.SizeZ && maps.Get(ix, iz) == m)
                {
                    pos[2 * m] = (float)cx;
                    pos[2 * m + 1] = (float)cz;
                    continue;
                }
                double best = double.MaxValue;
                int bx = 0, bz = 0;
                for (int z = 0; z < maps.SizeZ; z++)
                    for (int x = 0; x < maps.SizeX; x++)
                    {
                        if (maps.Get(x, z) != m)
                            continue;
                        double dx = x + 0.5 - cx, dz = z + 0.5 - cz, d = dx * dx + dz * dz;
                        if (d < best)
                        {
                            best = d;
                            bx = x;
                            bz = z;
                        }
                    }
                pos[2 * m] = bx + 0.5f;
                pos[2 * m + 1] = bz + 0.5f;
            }
            return pos;
        }

        static bool Separates(byte a, byte b) => a != b && a != MapTable.Ocean && b != MapTable.Ocean;

        static float[] ToCoords(List<int> nodes, int nx)
        {
            var p = new float[nodes.Count * 2];
            for (int i = 0; i < nodes.Count; i++)
            {
                p[2 * i] = nodes[i] % nx;
                p[2 * i + 1] = nodes[i] / nx;
            }
            return p;
        }

        /// <summary>Chaikin: mỗi đoạn A→B thay bằng 2 điểm ¾A + ¼B và ¼A + ¾B. Đường hở giữ điểm đầu/cuối; vòng kín lặp điểm đầu ở cuối.</summary>
        static float[] Smooth(float[] p, bool closed, int iterations)
        {
            for (int it = 0; it < iterations; it++)
            {
                int n = p.Length / 2;
                if (n < 3)
                    break;
                var q = new List<float>(4 * n);
                if (!closed)
                {
                    q.Add(p[0]);
                    q.Add(p[1]);
                }
                for (int i = 0; i + 1 < n; i++)
                {
                    float ax = p[2 * i], az = p[2 * i + 1], bx = p[2 * i + 2], bz = p[2 * i + 3];
                    q.Add(0.75f * ax + 0.25f * bx);
                    q.Add(0.75f * az + 0.25f * bz);
                    q.Add(0.25f * ax + 0.75f * bx);
                    q.Add(0.25f * az + 0.75f * bz);
                }
                if (closed)
                {
                    q.Add(q[0]);
                    q.Add(q[1]);
                }
                else
                {
                    q.Add(p[2 * n - 2]);
                    q.Add(p[2 * n - 1]);
                }
                p = q.ToArray();
            }
            return p;
        }
    }
}
