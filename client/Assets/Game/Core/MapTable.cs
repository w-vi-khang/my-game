using System;
using System.Collections.Generic;

namespace Game.Core
{
    /// <summary>Bảng map (glossary): mỗi cột (x, z) thuộc map nào; Ocean = cột biển, không thuộc map nào (decision 0022).</summary>
    public sealed class MapTable
    {
        public const byte Ocean = 255;

        public readonly int SizeX, SizeZ;
        readonly byte[] ids;
        readonly string[] names;

        public MapTable(int sizeX, int sizeZ, byte[] ids, string[] names)
        {
            if (ids == null || ids.Length != sizeX * sizeZ)
                throw new ArgumentException("ids phải có đúng sizeX * sizeZ phần tử");
            if (names == null)
                throw new ArgumentException("names không được null");
            SizeX = sizeX;
            SizeZ = sizeZ;
            this.ids = ids;
            this.names = names;
        }

        public byte[] Ids => ids;
        public int MapCount => names.Length;
        public string Name(int map) => names[map];
        public byte Get(int x, int z) => ids[x + SizeX * z];

        /// <summary>Gộp mảnh rời (decision 0022, brief 0009 QĐ 10): mỗi map chỉ giữ mảnh lớn nhất (nối 4 hướng); mảnh khác chuyển sang map kề có nhiều cạnh chung nhất (hoà → chỉ số map nhỏ hơn); mảnh không kề map nào giữ nguyên. Lặp tới khi không đổi. Chỉ số nguyên → tất định. Trả về tổng số cột đã chuyển.</summary>
        public static int MergeFragments(byte[] ids, int sizeX, int sizeZ, int mapCount)
        {
            if (ids == null || ids.Length != sizeX * sizeZ || mapCount < 1 || mapCount > 255)
                throw new ArgumentException("MergeFragments: tham số không hợp lệ");
            int total = 0;
            for (int pass = 0; pass <= 16; pass++)
            {
                var label = new int[ids.Length];
                for (int i = 0; i < label.Length; i++) label[i] = -1;
                var fragMap = new List<byte>();
                var fragCols = new List<List<int>>();
                var queue = new Queue<int>();
                for (int c = 0; c < ids.Length; c++)
                {
                    if (ids[c] == Ocean || label[c] >= 0) continue;
                    int f = fragMap.Count;
                    var cols = new List<int>();
                    fragMap.Add(ids[c]);
                    fragCols.Add(cols);
                    label[c] = f;
                    queue.Enqueue(c);
                    while (queue.Count > 0)
                    {
                        int cur = queue.Dequeue();
                        cols.Add(cur);
                        int cx = cur % sizeX, cz = cur / sizeX;
                        for (int d = 0; d < 4; d++)
                        {
                            int nx = cx + (d == 0 ? 1 : d == 1 ? -1 : 0);
                            int nz = cz + (d == 2 ? 1 : d == 3 ? -1 : 0);
                            if (nx < 0 || nz < 0 || nx >= sizeX || nz >= sizeZ) continue;
                            int n = nx + sizeX * nz;
                            if (label[n] >= 0 || ids[n] != ids[c]) continue;
                            label[n] = f;
                            queue.Enqueue(n);
                        }
                    }
                }

                var keep = new int[256];
                for (int i = 0; i < 256; i++) keep[i] = -1;
                for (int f = 0; f < fragMap.Count; f++)
                {
                    int m = fragMap[f];
                    if (keep[m] < 0 || fragCols[f].Count > fragCols[keep[m]].Count) keep[m] = f;
                }

                int changed = 0;
                var edges = new int[256];
                for (int f = 0; f < fragMap.Count; f++)
                {
                    if (keep[fragMap[f]] == f) continue;
                    Array.Clear(edges, 0, edges.Length);
                    byte own = fragMap[f];
                    foreach (int cur in fragCols[f])
                    {
                        int cx = cur % sizeX, cz = cur / sizeX;
                        for (int d = 0; d < 4; d++)
                        {
                            int nx = cx + (d == 0 ? 1 : d == 1 ? -1 : 0);
                            int nz = cz + (d == 2 ? 1 : d == 3 ? -1 : 0);
                            if (nx < 0 || nz < 0 || nx >= sizeX || nz >= sizeZ) continue;
                            byte v = ids[nx + sizeX * nz];
                            if (v != Ocean && v != own) edges[v]++;
                        }
                    }
                    int target = -1, best = 0;
                    for (int m = 0; m < 256; m++)
                        if (edges[m] > best) { best = edges[m]; target = m; }
                    if (target < 0) continue;
                    foreach (int cur in fragCols[f]) ids[cur] = (byte)target;
                    changed += fragCols[f].Count;
                }
                if (changed == 0) return total;
                total += changed;
            }
            throw new InvalidOperationException("MergeFragments không hội tụ");
        }
    }
}
