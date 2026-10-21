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
        /// <summary>Số lần chạy A* / BFS từ lúc tạo (đo sim bước 7, brief 0028) — không vào trạng thái mô phỏng.</summary>
        public long AStarCount { get; private set; }
        public long BfsCount { get; private set; }

        struct Node
        {
            public int F, H, Seq, Index;
        }

        static readonly int[] DirX = { 1, -1, 0, 0 };
        static readonly int[] DirZ = { 0, 0, 1, -1 };
        static readonly int[] DirY = { 0, 1, -1 };

        readonly WorldData world;
        readonly SimBounds bounds;
        readonly int sizeY;
        readonly int[] g;
        readonly int[] parent;
        readonly int[] seenStamp;
        readonly int[] closedStamp;
        readonly bool[] blocked;
        readonly bool[] tempBlocked;
        readonly List<int> tempList = new List<int>();
        readonly List<int> queue = new List<int>();
        readonly List<CellPos> goalBuf = new List<CellPos>();
        int stamp;
        readonly List<Node> heap = new List<Node>();

        public GridPathfinder(WorldData world)
            : this(world, world != null ? SimBounds.Whole(world.Size) : new SimBounds(0, 0, 1, 1))
        {
        }

        /// <summary>Tìm đường trong vùng mô phỏng (brief 0019): mảng cấp theo vùng; ô ngoài vùng coi như ngoài map.</summary>
        public GridPathfinder(WorldData world, SimBounds bounds)
        {
            this.world = world ?? throw new ArgumentNullException(nameof(world));
            if (!bounds.FitsIn(world.Size)) throw new ArgumentException("Vùng mô phỏng ngoài world: " + bounds);
            this.bounds = bounds;
            sizeY = world.Size.Y;
            int n = bounds.SizeX * bounds.SizeZ * sizeY;
            g = new int[n];
            parent = new int[n];
            seenStamp = new int[n];
            closedStamp = new int[n];
            blocked = new bool[n];
            tempBlocked = new bool[n];
        }

        public SimBounds Bounds => bounds;

        /// <summary>Số ô của mảng tìm đường (= SizeX · SizeZ · chiều cao world của vùng).</summary>
        public int CellCount => g.Length;

        bool InRegion(int x, int y, int z) => y >= 0 && y < sizeY && bounds.Contains(x, z);

        /// <summary>Chỉ số trong vùng: x nhanh nhất, rồi z, rồi y — như WorldSize.Index nhưng tính từ góc vùng.</summary>
        int Index(int x, int y, int z) => (x - bounds.X0) + bounds.SizeX * ((z - bounds.Z0) + bounds.SizeZ * y);

        void Coords(int i, out int x, out int y, out int z)
        {
            x = i % bounds.SizeX + bounds.X0;
            int rest = i / bounds.SizeX;
            z = rest % bounds.SizeZ + bounds.Z0;
            y = rest / bounds.SizeZ;
        }

        /// <summary>Đánh dấu ô bị công trình chiếm (kho): ô đó không cho thân.</summary>
        public void SetBlocked(CellPos c, bool value)
        {
            if (!InRegion(c.X, c.Y, c.Z)) throw new ArgumentException("Ô ngoài vùng mô phỏng: " + c);
            blocked[Index(c.X, c.Y, c.Z)] = value;
        }

        /// <summary>Chặn tạm ô (x, y, z) cho thân (sinh vật đang chắn, 0019) tới khi ClearTempBlocks; ngoài map thì bỏ qua.</summary>
        public void AddTempBlock(int x, int y, int z)
        {
            if (!InRegion(x, y, z)) return;
            int i = Index(x, y, z);
            if (tempBlocked[i]) return;
            tempBlocked[i] = true;
            tempList.Add(i);
        }

        public void ClearTempBlocks()
        {
            for (int i = 0; i < tempList.Count; i++) tempBlocked[tempList[i]] = false;
            tempList.Clear();
        }

        /// <summary>Ô cho thân: trong map, rỗng, không bị công trình hay chặn tạm.</summary>
        public bool IsBodyFree(int x, int y, int z)
        {
            if (!InRegion(x, y, z)) return false;
            int i = Index(x, y, z);
            return !blocked[i] && !tempBlocked[i] && world.GetMaterial(x, y, z) == BlockMaterial.Empty;
        }

        /// <summary>Ô đỡ: trong map, không rỗng và không phải nước.</summary>
        public bool IsSupport(int x, int y, int z)
        {
            if (!InRegion(x, y, z)) return false;
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
            goalBuf.Clear();
            goalBuf.Add(goal);
            return Search(start, goalBuf, sx, sz, sy, result);
        }

        /// <summary>A* nhiều đích: đường ngắn nhất tới một trong các ô đích (đích không đứng được bị bỏ).</summary>
        public bool FindPathToAny(CellPos start, List<CellPos> goals, int sx, int sz, int sy, List<CellPos> result)
        {
            result.Clear();
            goalBuf.Clear();
            for (int i = 0; i < goals.Count; i++)
                if (CanStand(goals[i], sx, sz, sy)) goalBuf.Add(goals[i]);
            if (goalBuf.Count == 0) return false;
            if (!InRegion(start.X, start.Y, start.Z)) return false;
            for (int i = 0; i < goalBuf.Count; i++)
                if (goalBuf[i] == start) return true;
            return Search(start, goalBuf, sx, sz, sy, result);
        }

        /// <summary>
        /// BFS từ start (thứ tự hướng như A*), tối đa maxSteps bước: đường tới ô đầu tiên lấy ra khỏi hàng đợi thoả accept
        /// (không xét chính start). Đường trong result, không gồm start.
        /// </summary>
        public bool FindPathToNearest(CellPos start, int sx, int sz, int sy, int maxSteps, Predicate<CellPos> accept, List<CellPos> result)
        {
            BfsCount++;
            result.Clear();
            if (!InRegion(start.X, start.Y, start.Z)) return false;
            stamp++;
            queue.Clear();
            int startIdx = Index(start.X, start.Y, start.Z);
            g[startIdx] = 0;
            parent[startIdx] = -1;
            seenStamp[startIdx] = stamp;
            queue.Add(startIdx);
            for (int head = 0; head < queue.Count; head++)
            {
                int ci = queue[head];
                Coords(ci, out int cx, out int cy, out int cz);
                var cur = new CellPos(cx, cy, cz);
                if (ci != startIdx && accept(cur))
                {
                    for (int i = ci; i != startIdx; i = parent[i])
                    {
                        Coords(i, out int px, out int py, out int pz);
                        result.Add(new CellPos(px, py, pz));
                    }
                    result.Reverse();
                    return true;
                }
                if (g[ci] >= maxSteps) continue;
                for (int d = 0; d < 4; d++)
                    for (int k = 0; k < DirY.Length; k++)
                    {
                        var to = new CellPos(cx + DirX[d], cy + DirY[k], cz + DirZ[d]);
                        if (!InRegion(to.X, to.Y, to.Z) || !CanStep(cur, to, sx, sz, sy)) continue;
                        int ni = Index(to.X, to.Y, to.Z);
                        if (seenStamp[ni] != stamp)
                        {
                            seenStamp[ni] = stamp;
                            g[ni] = g[ci] + 1;
                            parent[ni] = ci;
                            queue.Add(ni);
                        }
                        break;
                    }
            }
            return false;
        }

        /// <summary>
        /// BFS nhiều nguồn: các ô starts trong vùng và đứng được (theo thứ tự danh sách, bỏ trùng), rồi thứ tự hướng như A*;
        /// chỉ đi vào ô thoả region. result = mọi ô tới được (kể cả nguồn) theo thứ tự lấy ra khỏi hàng đợi.
        /// </summary>
        public void FloodRegion(List<CellPos> starts, int sx, int sz, int sy, Predicate<CellPos> region, List<CellPos> result)
        {
            BfsCount++;
            result.Clear();
            stamp++;
            queue.Clear();
            for (int i = 0; i < starts.Count; i++)
            {
                var s = starts[i];
                if (!InRegion(s.X, s.Y, s.Z) || !region(s) || !CanStand(s, sx, sz, sy)) continue;
                int si = Index(s.X, s.Y, s.Z);
                if (seenStamp[si] == stamp) continue;
                seenStamp[si] = stamp;
                queue.Add(si);
            }
            for (int head = 0; head < queue.Count; head++)
            {
                int ci = queue[head];
                Coords(ci, out int cx, out int cy, out int cz);
                var cur = new CellPos(cx, cy, cz);
                result.Add(cur);
                for (int d = 0; d < 4; d++)
                    for (int k = 0; k < DirY.Length; k++)
                    {
                        var to = new CellPos(cx + DirX[d], cy + DirY[k], cz + DirZ[d]);
                        if (!InRegion(to.X, to.Y, to.Z) || !CanStep(cur, to, sx, sz, sy)) continue;
                        int ni = Index(to.X, to.Y, to.Z);
                        if (seenStamp[ni] != stamp && region(to))
                        {
                            seenStamp[ni] = stamp;
                            queue.Add(ni);
                        }
                        break;
                    }
            }
        }

        int Heuristic(int x, int z, List<CellPos> goals)
        {
            int best = int.MaxValue;
            for (int i = 0; i < goals.Count; i++)
            {
                int h = Math.Abs(goals[i].X - x) + Math.Abs(goals[i].Z - z);
                if (h < best) best = h;
            }
            return best;
        }

        bool Search(CellPos start, List<CellPos> goals, int sx, int sz, int sy, List<CellPos> result)
        {
            AStarCount++;
            if (!InRegion(start.X, start.Y, start.Z) || !CanStand(goals[0], sx, sz, sy)) return false;

            stamp++;
            heap.Clear();
            int seq = 0;
            int startIdx = Index(start.X, start.Y, start.Z);
            g[startIdx] = 0;
            parent[startIdx] = -1;
            seenStamp[startIdx] = stamp;
            int h0 = Heuristic(start.X, start.Z, goals);
            Push(new Node { F = h0, H = h0, Seq = seq++, Index = startIdx });

            while (heap.Count > 0)
            {
                var cur = Pop();
                if (closedStamp[cur.Index] == stamp) continue;
                closedStamp[cur.Index] = stamp;
                Coords(cur.Index, out int cx, out int cy, out int cz);
                if (IsGoal(cx, cy, cz, goals))
                {
                    for (int i = cur.Index; i != startIdx; i = parent[i])
                    {
                        Coords(i, out int px, out int py, out int pz);
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
                        if (!InRegion(to.X, to.Y, to.Z)) continue;
                        if (!CanStep(from, to, sx, sz, sy)) continue;
                        int ni = Index(to.X, to.Y, to.Z);
                        if (closedStamp[ni] == stamp) break;
                        int ng = g[cur.Index] + 1;
                        if (seenStamp[ni] != stamp || ng < g[ni])
                        {
                            seenStamp[ni] = stamp;
                            g[ni] = ng;
                            parent[ni] = cur.Index;
                            int h = Heuristic(to.X, to.Z, goals);
                            Push(new Node { F = ng + h, H = h, Seq = seq++, Index = ni });
                        }
                        break;
                    }
                }
            }
            return false;
        }

        static bool IsGoal(int x, int y, int z, List<CellPos> goals)
        {
            for (int i = 0; i < goals.Count; i++)
                if (goals[i].X == x && goals[i].Y == y && goals[i].Z == z) return true;
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
