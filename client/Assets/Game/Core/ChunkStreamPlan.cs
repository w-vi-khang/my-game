using System;
using System.Collections.Generic;

namespace Game.Core
{
    /// <summary>One streaming viewpoint: a cell position plus its own load/unload radii (in cells).</summary>
    public readonly struct StreamViewpoint
    {
        public readonly int X, Z, LoadRadius, UnloadRadius;
        public StreamViewpoint(int x, int z, int loadRadius, int unloadRadius) { X = x; Z = z; LoadRadius = loadRadius; UnloadRadius = unloadRadius; }
    }

    /// <summary>Chọn cột chunk cần nạp/gỡ quanh camera (world-tech bước 7b, brief 0010). Số nguyên, tất định.</summary>
    public sealed class ChunkStreamPlan
    {
        public readonly int ColumnsX, ColumnsZ, LoadRadius, UnloadRadius, MaxViewpoints;
        readonly bool[] loaded;
        readonly List<KeyValuePair<long, int>> candidates = new List<KeyValuePair<long, int>>();
        readonly List<StreamViewpoint> single = new List<StreamViewpoint>();

        public ChunkStreamPlan(int columnsX, int columnsZ, int loadRadius, int unloadRadius, int maxViewpoints = 1)
        {
            if (columnsX <= 0) throw new ArgumentOutOfRangeException(nameof(columnsX));
            if (columnsZ <= 0) throw new ArgumentOutOfRangeException(nameof(columnsZ));
            if (loadRadius < 0) throw new ArgumentOutOfRangeException(nameof(loadRadius));
            if (unloadRadius < loadRadius) throw new ArgumentOutOfRangeException(nameof(unloadRadius));
            if (maxViewpoints < 1) throw new ArgumentOutOfRangeException(nameof(maxViewpoints));
            MaxViewpoints = maxViewpoints;
            ColumnsX = columnsX;
            ColumnsZ = columnsZ;
            LoadRadius = loadRadius;
            UnloadRadius = unloadRadius;
            loaded = new bool[columnsX * columnsZ];
        }

        public int ColumnCount => ColumnsX * ColumnsZ;

        /// <summary>Số cột đang nạp.</summary>
        public int LoadedCount { get; private set; }

        /// <summary>column = cx + ColumnsX * cz.</summary>
        public bool IsLoaded(int column) => loaded[column];

        /// <summary>Bình phương khoảng cách (ô) từ ô (px, pz) tới ô gần nhất của cột (cx, cz); 0 nếu ô nằm trong cột.</summary>
        public static long DistanceSq(int px, int pz, int cx, int cz)
        {
            int x0 = cx * WorldSize.ChunkSize, x1 = x0 + WorldSize.ChunkSize - 1;
            int z0 = cz * WorldSize.ChunkSize, z1 = z0 + WorldSize.ChunkSize - 1;
            int dx = px < x0 ? x0 - px : (px > x1 ? px - x1 : 0);
            int dz = pz < z0 ? z0 - pz : (pz > z1 ? pz - z1 : 0);
            return (long)dx * dx + (long)dz * dz;
        }

        /// <summary>
        /// toLoad/toUnload được Clear trước khi ghi. Gỡ: mọi cột đang nạp có DistanceSq &gt; UnloadRadius² (thứ tự chỉ số tăng).
        /// Nạp: các cột chưa nạp có DistanceSq ≤ LoadRadius², sắp theo (DistanceSq, chỉ số) tăng, lấy tối đa maxLoad cột (maxLoad ≤ 0 → không giới hạn).
        /// Cột trong toLoad/toUnload được đánh dấu đã nạp/đã gỡ ngay trong lần gọi này.
        /// </summary>
        public void Update(int px, int pz, int maxLoad, List<int> toLoad, List<int> toUnload)
        {
            single.Clear();
            single.Add(new StreamViewpoint(px, pz, LoadRadius, UnloadRadius));
            Update(single, maxLoad, toLoad, toUnload);
        }

        /// <summary>
        /// Multi-viewpoint update. Load: unloaded column within the load radius of at least one viewpoint.
        /// Unload: loaded column beyond the unload radius of every viewpoint. Load order: smallest squared distance
        /// to any viewpoint, then column index; one shared maxLoad budget (maxLoad &lt;= 0 means unlimited).
        /// </summary>
        public void Update(IReadOnlyList<StreamViewpoint> viewpoints, int maxLoad, List<int> toLoad, List<int> toUnload)
        {
            if (viewpoints == null) throw new ArgumentNullException(nameof(viewpoints));
            if (viewpoints.Count < 1 || viewpoints.Count > MaxViewpoints)
                throw new ArgumentException($"viewpoint count {viewpoints.Count} outside 1..{MaxViewpoints}", nameof(viewpoints));
            for (int i = 0; i < viewpoints.Count; i++)
            {
                var v = viewpoints[i];
                if (v.LoadRadius < 0) throw new ArgumentOutOfRangeException(nameof(viewpoints), "LoadRadius < 0");
                if (v.UnloadRadius < v.LoadRadius) throw new ArgumentOutOfRangeException(nameof(viewpoints), "UnloadRadius < LoadRadius");
            }
            toLoad.Clear();
            toUnload.Clear();
            int count = viewpoints.Count;
            candidates.Clear();
            for (int c = 0; c < loaded.Length; c++)
            {
                int cx = c % ColumnsX, cz = c / ColumnsX;
                long dMin = long.MaxValue;
                bool inLoad = false, inKeep = false;
                for (int i = 0; i < count; i++)
                {
                    var v = viewpoints[i];
                    long d = DistanceSq(v.X, v.Z, cx, cz);
                    if (d < dMin) dMin = d;
                    if (d <= (long)v.LoadRadius * v.LoadRadius) inLoad = true;
                    if (d <= (long)v.UnloadRadius * v.UnloadRadius) inKeep = true;
                }
                if (loaded[c])
                {
                    if (!inKeep)
                    {
                        loaded[c] = false;
                        LoadedCount--;
                        toUnload.Add(c);
                    }
                }
                else if (inLoad)
                {
                    candidates.Add(new KeyValuePair<long, int>(dMin, c));
                }
            }
            candidates.Sort((a, b) => a.Key != b.Key ? a.Key.CompareTo(b.Key) : a.Value.CompareTo(b.Value));
            int n = maxLoad <= 0 ? candidates.Count : Math.Min(maxLoad, candidates.Count);
            for (int k = 0; k < n; k++)
            {
                int c = candidates[k].Value;
                loaded[c] = true;
                LoadedCount++;
                toLoad.Add(c);
            }
        }

        /// <summary>Số cột có DistanceSq ≤ radius² mà chưa nạp (kiểm "không lỗ").</summary>
        public int MissingWithin(int px, int pz, int radius)
        {
            long r2 = (long)radius * radius;
            int missing = 0;
            for (int c = 0; c < loaded.Length; c++)
                if (!loaded[c] && DistanceSq(px, pz, c % ColumnsX, c / ColumnsX) <= r2)
                    missing++;
            return missing;
        }

        /// <summary>Gỡ mọi cột (không trả danh sách) — dùng khi Build lại.</summary>
        public void Clear()
        {
            Array.Clear(loaded, 0, loaded.Length);
            LoadedCount = 0;
        }
    }
}
