using System;
using System.Collections.Generic;

namespace Game.Core.Sim
{
    /// <summary>
    /// A* đi bộ trên lưới ô theo khung chiếm chỗ (simulation-tech bước 2, 0015). 4 hướng, leo/xuống 1 ô, không vào nước.
    /// Tất định: thứ tự hướng và cách so trong hàng đợi cố định.
    /// </summary>
    public sealed class GridPathfinder
    {
        struct Node
        {
            public int F, H, Seq, Index;
        }

        static readonly int[] DirX = { 1, -1, 0, 0 };
        static readonly int[] DirZ = { 0, 0, 1, -1 };
        static readonly int[] DirY = { 0, 1, -1 };

        readonly WorldData world;
        readonly int[] g;
        readonly int[] parent;
        readonly int[] seenStamp;
        readonly int[] closedStamp;
        int stamp;
        readonly List<Node> heap = new List<Node>();

        public GridPathfinder(WorldData world)
        {
            this.world = world ?? throw new ArgumentNullException(nameof(world));
            int n = world.Size.CellCount;
            g = new int[n];
            parent = new int[n];
            seenStamp = new int[n];
            closedStamp = new int[n];
        }

        /// <summary>Ô cho thân: trong map và rỗng.</summary>
        public bool IsBodyFree(int x, int y, int z) =>
            world.Size.InBounds(x, y, z) && world.GetMaterial(x, y, z) == BlockMaterial.Empty;

        /// <summary>Ô đỡ: trong map, không rỗng và không phải nước.</summary>
        public bool IsSupport(int x, int y, int z)
        {
            if (!world.Size.InBounds(x, y, z)) return false;
            var m = world.GetMaterial(x, y, z);
            return m != BlockMaterial.Empty && m != BlockMaterial.Water;
        }

        public bool CanStand(CellPos a, int sx, int sz, int sy)
        {
            for (int dx = 0; dx < sx; dx++)
                for (int dz = 0; dz < sz; dz++)
                {
                    if (!IsSupport(a.X + dx, a.Y - 1, a.Z + dz)) return false;
                    for (int dy = 0; dy < sy; dy++)
                        if (!IsBodyFree(a.X + dx, a.Y + dy, a.Z + dz)) return false;
                }
            return true;
        }

        public bool CanStep(CellPos from, CellPos to, int sx, int sz, int sy)
        {
            if (Math.Abs(to.X - from.X) + Math.Abs(to.Z - from.Z) != 1) return false;
            int ddy = to.Y - from.Y;
            if (ddy < -1 || ddy > 1) return false;
            if (!CanStand(to, sx, sz, sy)) return false;
            if (ddy == 1 && !ColumnsFree(from, from.Y + sy, sx, sz)) return false;
            if (ddy == -1 && !ColumnsFree(to, to.Y + sy, sx, sz)) return false;
            return true;
        }

        bool ColumnsFree(CellPos a, int y, int sx, int sz)
        {
            for (int dx = 0; dx < sx; dx++)
                for (int dz = 0; dz < sz; dz++)
                    if (!IsBodyFree(a.X + dx, y, a.Z + dz)) return false;
            return true;
        }

        public bool FindPath(CellPos start, CellPos goal, int sx, int sz, int sy, List<CellPos> result)
        {
            result.Clear();
            if (start == goal) return true;
            var size = world.Size;
            if (!size.InBounds(start.X, start.Y, start.Z) || !CanStand(goal, sx, sz, sy)) return false;

            stamp++;
            heap.Clear();
            int seq = 0;
            int startIdx = size.Index(start.X, start.Y, start.Z);
            g[startIdx] = 0;
            parent[startIdx] = -1;
            seenStamp[startIdx] = stamp;
            int h0 = Math.Abs(goal.X - start.X) + Math.Abs(goal.Z - start.Z);
            Push(new Node { F = h0, H = h0, Seq = seq++, Index = startIdx });

            while (heap.Count > 0)
            {
                var cur = Pop();
                if (closedStamp[cur.Index] == stamp) continue;
                closedStamp[cur.Index] = stamp;
                size.Coords(cur.Index, out int cx, out int cy, out int cz);
                if (cx == goal.X && cy == goal.Y && cz == goal.Z)
                {
                    for (int i = cur.Index; i != startIdx; i = parent[i])
                    {
                        size.Coords(i, out int px, out int py, out int pz);
                        result.Add(new CellPos(px, py, pz));
                    }
                    result.Reverse();
                    return true;
                }

                var from = new CellPos(cx, cy, cz);
                for (int d = 0; d < 4; d++)
                {
                    for (int k = 0; k < DirY.Length; k++)
                    {
                        var to = new CellPos(cx + DirX[d], cy + DirY[k], cz + DirZ[d]);
                        if (!size.InBounds(to.X, to.Y, to.Z)) continue;
                        if (!CanStep(from, to, sx, sz, sy)) continue;
                        int ni = size.Index(to.X, to.Y, to.Z);
                        if (closedStamp[ni] == stamp) break;
                        int ng = g[cur.Index] + 1;
                        if (seenStamp[ni] != stamp || ng < g[ni])
                        {
                            seenStamp[ni] = stamp;
                            g[ni] = ng;
                            parent[ni] = cur.Index;
                            int h = Math.Abs(goal.X - to.X) + Math.Abs(goal.Z - to.Z);
                            Push(new Node { F = ng + h, H = h, Seq = seq++, Index = ni });
                        }
                        break;
                    }
                }
            }
            return false;
        }

        static bool Less(Node a, Node b)
        {
            if (a.F != b.F) return a.F < b.F;
            if (a.H != b.H) return a.H < b.H;
            return a.Seq < b.Seq;
        }

        void Push(Node n)
        {
            heap.Add(n);
            int i = heap.Count - 1;
            while (i > 0)
            {
                int p = (i - 1) / 2;
                if (!Less(heap[i], heap[p])) break;
                (heap[i], heap[p]) = (heap[p], heap[i]);
                i = p;
            }
        }

        Node Pop()
        {
            var top = heap[0];
            int last = heap.Count - 1;
            heap[0] = heap[last];
            heap.RemoveAt(last);
            int i = 0;
            while (true)
            {
                int l = 2 * i + 1, r = l + 1, m = i;
                if (l < heap.Count && Less(heap[l], heap[m])) m = l;
                if (r < heap.Count && Less(heap[r], heap[m])) m = r;
                if (m == i) break;
                (heap[i], heap[m]) = (heap[m], heap[i]);
                i = m;
            }
            return top;
        }
    }
}
