using System;
using System.Collections.Generic;

namespace Game.Core
{
    /// <summary>Chọn cột chunk cần nạp/gỡ quanh camera (world-tech bước 7b, brief 0010). Số nguyên, tất định.</summary>
    public sealed class ChunkStreamPlan
    {
        public readonly int ColumnsX, ColumnsZ, LoadRadius, UnloadRadius;
        readonly bool[] loaded;
        readonly List<KeyValuePair<long, int>> candidates = new List<KeyValuePair<long, int>>();

        public ChunkStreamPlan(int columnsX, int columnsZ, int loadRadius, int unloadRadius)
        {
            if (columnsX <= 0) throw new ArgumentOutOfRangeException(nameof(columnsX));
            if (columnsZ <= 0) throw new ArgumentOutOfRangeException(nameof(columnsZ));
            if (loadRadius < 0) throw new ArgumentOutOfRangeException(nameof(loadRadius));
            if (unloadRadius < loadRadius) throw new ArgumentOutOfRangeException(nameof(unloadRadius));
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
            toLoad.Clear();
            toUnload.Clear();
            long unloadSq = (long)UnloadRadius * UnloadRadius;
            long loadSq = (long)LoadRadius * LoadRadius;
            candidates.Clear();
            for (int c = 0; c < loaded.Length; c++)
            {
                long d = DistanceSq(px, pz, c % ColumnsX, c / ColumnsX);
                if (loaded[c])
                {
                    if (d > unloadSq)
                    {
                        loaded[c] = false;
                        LoadedCount--;
                        toUnload.Add(c);
                    }
                }
                else if (d <= loadSq)
                {
                    candidates.Add(new KeyValuePair<long, int>(d, c));
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
