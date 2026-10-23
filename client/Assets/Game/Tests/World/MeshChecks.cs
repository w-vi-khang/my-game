using System;
using System.Collections.Generic;
using Unity.Mathematics;

namespace Game.World.Tests
{
    /// <summary>Kiểm mesh kín sau khi hàn đỉnh theo vị trí: mỗi cạnh có hướng a→b phải có đúng chừng ấy cạnh b→a
    /// (mặt kín, các tam giác cùng chiều). Cạnh mà <c>ignore</c> trả true (vd mép map) được bỏ qua.</summary>
    static class MeshChecks
    {
        public static long Key(float3 p)
        {
            var q = (int3)math.round(p * 1024f);
            return ((long)(q.x + 100000) * 400000L + (q.y + 100000)) * 400000L + (q.z + 100000);
        }

        public static int UnbalancedEdges(IEnumerable<(float3[] vertices, int[] indices, float3 offset)> parts,
            Func<float3, float3, bool> ignore, Action<string> log = null)
        {
            var directed = new Dictionary<(long, long), int>();
            var pos = new Dictionary<long, float3>();
            foreach (var (vertices, indices, offset) in parts)
            {
                for (int t = 0; t < indices.Length; t += 3)
                for (int e = 0; e < 3; e++)
                {
                    float3 a = vertices[indices[t + e]] + offset, b = vertices[indices[t + (e + 1) % 3]] + offset;
                    long ka = Key(a), kb = Key(b);
                    if (ka == kb)
                        continue; // cạnh suy biến sau khi hàn — không ảnh hưởng độ kín
                    pos[ka] = a;
                    pos[kb] = b;
                    directed.TryGetValue((ka, kb), out int n);
                    directed[(ka, kb)] = n + 1;
                }
            }
            int bad = 0;
            foreach (var kv in directed)
            {
                directed.TryGetValue((kv.Key.Item2, kv.Key.Item1), out int back);
                if (back == kv.Value)
                    continue;
                float3 a = pos[kv.Key.Item1], b = pos[kv.Key.Item2];
                if (ignore != null && ignore(a, b))
                    continue;
                bad++;
                if (bad <= 5)
                    log?.Invoke($"UNBALANCED {a} → {b}: {kv.Value} times, reverse {back} times");
            }
            return bad;
        }
    }
}
