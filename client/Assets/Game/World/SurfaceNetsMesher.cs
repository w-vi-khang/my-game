using Game.Core;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace Game.World
{
    /// <summary>Kết quả dựng mesh một chunk; toạ độ tính từ gốc chunk, đơn vị = 1 ô gameplay.</summary>
    public sealed class ChunkMesh
    {
        public float3[] Vertices;
        public float3[] Normals;
        /// <summary>Vị trí Surface Nets trước khi làm mượt (để kiểm giới hạn lệch).</summary>
        public float3[] RawVertices;
        public int[] Indices;
        public int TriangleCount => Indices.Length / 3;
    }

    /// <summary>
    /// Surface Nets ×2 (design/04-tech/world-tech.md "Dựng mesh bề mặt mịn"): mỗi ô gameplay = 2 × 2 × 2 mẫu; mỗi ô dựng mesh
    /// (giữa 8 mẫu) có mặt cắt → một đỉnh = trung bình các điểm cắt cạnh; làm mượt một lượt, mỗi đỉnh lệch ≤ MaxShift ô.
    /// Đọc thêm 1 ô gameplay đệm quanh chunk → hai chunk cạnh nhau tính ra cùng đỉnh ở biên (mặt liền, pháp tuyến liền).
    /// </summary>
    public static class SurfaceNetsMesher
    {
        public const int Resolution = 2;
        public const float MaxShift = 0.2f;

        const int N = WorldSize.ChunkSize * Resolution; // 32 mẫu mỗi trục trong chunk
        const int SampleMin = -3, SampleCount = N + 6;  // mẫu -3..34
        const int DualMin = -3, DualCount = N + 5;      // ô dựng mesh có đỉnh thô -3..33
        const int SmoothMin = -2, SmoothCount = N + 3;  // đỉnh đã làm mượt -2..32 (dùng cho pháp tuyến)
        const int OutMin = -1, OutCount = N + 1;        // đỉnh đầu ra -1..31

        public static ChunkMesh Build(WorldData world, int chunkX, int chunkY, int chunkZ)
        {
            var cells = new NativeArray<ushort>(world.Cells, Allocator.TempJob);
            try
            {
                return Build(cells, world.Size, chunkX, chunkY, chunkZ);
            }
            finally
            {
                cells.Dispose();
            }
        }

        public static ChunkMesh Build(NativeArray<ushort> cells, WorldSize size, int chunkX, int chunkY, int chunkZ)
        {
            var vertices = new NativeList<float3>(Allocator.TempJob);
            var normals = new NativeList<float3>(Allocator.TempJob);
            var raw = new NativeList<float3>(Allocator.TempJob);
            var indices = new NativeList<int>(Allocator.TempJob);
            try
            {
                new MeshJob
                {
                    Cells = cells,
                    SizeX = size.X, SizeY = size.Y, SizeZ = size.Z,
                    Origin = new int3(chunkX, chunkY, chunkZ) * WorldSize.ChunkSize,
                    MaxShiftSamples = MaxShift * Resolution,
                    Vertices = vertices, Normals = normals, RawVertices = raw, Indices = indices,
                }.Run();
                return new ChunkMesh
                {
                    Vertices = vertices.AsArray().ToArray(),
                    Normals = normals.AsArray().ToArray(),
                    RawVertices = raw.AsArray().ToArray(),
                    Indices = indices.AsArray().ToArray(),
                };
            }
            finally
            {
                vertices.Dispose();
                normals.Dispose();
                raw.Dispose();
                indices.Dispose();
            }
        }

        /// <summary>Dựng mọi chunk song song (mỗi chunk một job, dùng chung mảng ô chỉ đọc). Thứ tự: x nhanh nhất, rồi z, rồi y.</summary>
        public static ChunkMesh[] BuildAll(WorldData world)
        {
            var size = world.Size;
            int count = size.ChunkCount;
            var cells = new NativeArray<ushort>(world.Cells, Allocator.TempJob);
            var lists = new (NativeList<float3> v, NativeList<float3> n, NativeList<float3> r, NativeList<int> i)[count];
            var handles = new NativeArray<JobHandle>(count, Allocator.Temp);
            try
            {
                for (int c = 0; c < count; c++)
                {
                    int cx = c % size.ChunksX, cz = c / size.ChunksX % size.ChunksZ, cy = c / (size.ChunksX * size.ChunksZ);
                    lists[c] = (new NativeList<float3>(Allocator.TempJob), new NativeList<float3>(Allocator.TempJob),
                        new NativeList<float3>(Allocator.TempJob), new NativeList<int>(Allocator.TempJob));
                    handles[c] = new MeshJob
                    {
                        Cells = cells,
                        SizeX = size.X, SizeY = size.Y, SizeZ = size.Z,
                        Origin = new int3(cx, cy, cz) * WorldSize.ChunkSize,
                        MaxShiftSamples = MaxShift * Resolution,
                        Vertices = lists[c].v, Normals = lists[c].n, RawVertices = lists[c].r, Indices = lists[c].i,
                    }.Schedule();
                }
                JobHandle.CompleteAll(handles);
                var result = new ChunkMesh[count];
                for (int c = 0; c < count; c++)
                {
                    result[c] = new ChunkMesh
                    {
                        Vertices = lists[c].v.AsArray().ToArray(),
                        Normals = lists[c].n.AsArray().ToArray(),
                        RawVertices = lists[c].r.AsArray().ToArray(),
                        Indices = lists[c].i.AsArray().ToArray(),
                    };
                }
                return result;
            }
            finally
            {
                handles.Dispose();
                foreach (var l in lists)
                {
                    if (l.v.IsCreated) l.v.Dispose();
                    if (l.n.IsCreated) l.n.Dispose();
                    if (l.r.IsCreated) l.r.Dispose();
                    if (l.i.IsCreated) l.i.Dispose();
                }
                cells.Dispose();
            }
        }

        [BurstCompile(CompileSynchronously = true)]
        struct MeshJob : IJob
        {
            [ReadOnly] public NativeArray<ushort> Cells;
            public int SizeX, SizeY, SizeZ;
            public int3 Origin;
            public float MaxShiftSamples;

            public NativeList<float3> Vertices, Normals, RawVertices;
            public NativeList<int> Indices;

            public void Execute()
            {
                var solid = new NativeArray<byte>(SampleCount * SampleCount * SampleCount, Allocator.Temp);
                var rawPos = new NativeArray<float3>(DualCount * DualCount * DualCount, Allocator.Temp);
                var hasVert = new NativeArray<byte>(DualCount * DualCount * DualCount, Allocator.Temp);
                var smooth = new NativeArray<float3>(SmoothCount * SmoothCount * SmoothCount, Allocator.Temp);
                var normalSum = new NativeArray<float3>(SmoothCount * SmoothCount * SmoothCount, Allocator.Temp);

                // 1. Lấy mẫu: mẫu s (toạ độ trong chunk) nằm ở tâm ô con s của lưới ×2 → ô gameplay floor((gốc×2 + s) / 2).
                int3 o2 = Origin * Resolution;
                for (int y = 0; y < SampleCount; y++)
                for (int z = 0; z < SampleCount; z++)
                for (int x = 0; x < SampleCount; x++)
                {
                    int3 g = o2 + new int3(x, y, z) + SampleMin;
                    solid[SIndex(x, y, z)] = IsSolid(FloorDiv(g.x), FloorDiv(g.y), FloorDiv(g.z)) ? (byte)1 : (byte)0;
                }

                // 2. Đỉnh thô của mỗi ô dựng mesh có mặt cắt: trung bình trung điểm các cạnh đổi dấu (toạ độ mẫu).
                //    Ô dựng mesh d có 8 góc là mẫu d..d+1; DualMin = SampleMin nên chỉ số mảng trùng nhau.
                for (int y = 0; y < DualCount; y++)
                for (int z = 0; z < DualCount; z++)
                for (int x = 0; x < DualCount; x++)
                {
                    float3 sum = 0;
                    int count = 0;
                    for (int e = 0; e < 12; e++)
                    {
                        EdgeCorners(e, out int3 a, out int3 b);
                        if (solid[SIndex(x + a.x, y + a.y, z + a.z)] != solid[SIndex(x + b.x, y + b.y, z + b.z)])
                        {
                            sum += (float3)(a + b) * 0.5f;
                            count++;
                        }
                    }
                    if (count > 0)
                    {
                        int di = DIndex(x, y, z);
                        hasVert[di] = 1;
                        rawPos[di] = new float3(x, y, z) + DualMin + sum / count;
                    }
                }

                // 3. Làm mượt một lượt (ô -2..32): kéo về trung bình 6 láng giềng có đỉnh, độ lệch ≤ MaxShift.
                for (int y = 0; y < SmoothCount; y++)
                for (int z = 0; z < SmoothCount; z++)
                for (int x = 0; x < SmoothCount; x++)
                {
                    int dx = x + (SmoothMin - DualMin), dy = y + (SmoothMin - DualMin), dz = z + (SmoothMin - DualMin);
                    int di = DIndex(dx, dy, dz);
                    if (hasVert[di] == 0)
                        continue;
                    float3 p = rawPos[di];
                    float3 avg = 0;
                    int n = 0;
                    AddNeighbor(dx - 1, dy, dz, hasVert, rawPos, ref avg, ref n);
                    AddNeighbor(dx + 1, dy, dz, hasVert, rawPos, ref avg, ref n);
                    AddNeighbor(dx, dy - 1, dz, hasVert, rawPos, ref avg, ref n);
                    AddNeighbor(dx, dy + 1, dz, hasVert, rawPos, ref avg, ref n);
                    AddNeighbor(dx, dy, dz - 1, hasVert, rawPos, ref avg, ref n);
                    AddNeighbor(dx, dy, dz + 1, hasVert, rawPos, ref avg, ref n);
                    float3 q = p;
                    if (n > 0)
                    {
                        float3 delta = avg / n - p;
                        float len = math.length(delta);
                        if (len > MaxShiftSamples)
                            delta *= MaxShiftSamples / len;
                        q = p + delta;
                    }
                    smooth[MIndex(x, y, z)] = q;
                }

                // 4. Đỉnh đầu ra: ô -1..31 có đỉnh.
                var outIndex = new NativeArray<int>(OutCount * OutCount * OutCount, Allocator.Temp);
                for (int y = 0; y < OutCount; y++)
                for (int z = 0; z < OutCount; z++)
                for (int x = 0; x < OutCount; x++)
                {
                    int3 c = new int3(x, y, z) + OutMin;
                    int3 d = c - DualMin;
                    int di = DIndex(d.x, d.y, d.z);
                    if (hasVert[di] == 0)
                    {
                        outIndex[OIndex(x, y, z)] = -1;
                        continue;
                    }
                    outIndex[OIndex(x, y, z)] = Vertices.Length;
                    int3 m = c - SmoothMin;
                    Vertices.Add(ToLocal(smooth[MIndex(m.x, m.y, m.z)]));
                    RawVertices.Add(ToLocal(rawPos[di]));
                    Normals.Add(float3.zero);
                }

                // 5. Mặt tứ giác cho mỗi cạnh mẫu đổi dấu. Cạnh thuộc chunk khi mẫu đầu ∈ [0, 32) mọi trục → mỗi mặt chỉ một chunk vẽ.
                //    Mọi cạnh có đủ 4 đỉnh đã làm mượt (ô -2..32) đều cộng pháp tuyến → pháp tuyến ở biên khớp chunk bên cạnh.
                for (int axis = 0; axis < 3; axis++)
                {
                    int u = (axis + 1) % 3, v = (axis + 2) % 3;
                    for (int sy = -2; sy <= N; sy++)
                    for (int sz = -2; sz <= N; sz++)
                    for (int sx = -2; sx <= N; sx++)
                    {
                        int3 s = new int3(sx, sy, sz);
                        int3 t = s;
                        t[axis] += 1;
                        byte a = solid[SIndex(s.x - SampleMin, s.y - SampleMin, s.z - SampleMin)];
                        byte b = solid[SIndex(t.x - SampleMin, t.y - SampleMin, t.z - SampleMin)];
                        if (a == b)
                            continue;

                        // 4 ô dựng mesh quanh cạnh: trục cạnh = s[axis], hai trục còn lại = s-1 và s.
                        int3 c00 = s, c10 = s, c11 = s, c01 = s;
                        c00[u] -= 1; c00[v] -= 1;
                        c10[v] -= 1;
                        c01[u] -= 1;
                        if (!InSmooth(c00) || !InSmooth(c10) || !InSmooth(c11) || !InSmooth(c01))
                            continue;
                        float3 p00 = SmoothAt(smooth, c00), p10 = SmoothAt(smooth, c10);
                        float3 p11 = SmoothAt(smooth, c11), p01 = SmoothAt(smooth, c01);

                        // Pháp tuyến hướng về phía rỗng.
                        float3 want = 0;
                        want[axis] = a == 1 ? 1f : -1f;
                        float3 n0 = math.cross(p10 - p00, p11 - p00) + math.cross(p11 - p00, p01 - p00);
                        bool flip = math.dot(n0, want) < 0;
                        float3 faceNormal = flip ? -n0 : n0;
                        AddNormal(normalSum, c00, faceNormal);
                        AddNormal(normalSum, c10, faceNormal);
                        AddNormal(normalSum, c11, faceNormal);
                        AddNormal(normalSum, c01, faceNormal);

                        bool owned = sx >= 0 && sy >= 0 && sz >= 0 && sx < N && sy < N && sz < N;
                        if (!owned)
                            continue;
                        int i00 = outIndex[OIndex(c00)], i10 = outIndex[OIndex(c10)];
                        int i11 = outIndex[OIndex(c11)], i01 = outIndex[OIndex(c01)];
                        if (!flip)
                        {
                            Indices.Add(i00); Indices.Add(i10); Indices.Add(i11);
                            Indices.Add(i00); Indices.Add(i11); Indices.Add(i01);
                        }
                        else
                        {
                            Indices.Add(i00); Indices.Add(i11); Indices.Add(i10);
                            Indices.Add(i00); Indices.Add(i01); Indices.Add(i11);
                        }
                    }
                }

                for (int y = 0; y < OutCount; y++)
                for (int z = 0; z < OutCount; z++)
                for (int x = 0; x < OutCount; x++)
                {
                    int vi = outIndex[OIndex(x, y, z)];
                    if (vi < 0)
                        continue;
                    int3 m = new int3(x, y, z) + OutMin - SmoothMin;
                    float3 nrm = normalSum[MIndex(m.x, m.y, m.z)];
                    Normals[vi] = math.lengthsq(nrm) > 0 ? math.normalize(nrm) : new float3(0, 1, 0);
                }

                solid.Dispose();
                rawPos.Dispose();
                hasVert.Dispose();
                smooth.Dispose();
                normalSum.Dispose();
                outIndex.Dispose();
            }

            static void AddNormal(NativeArray<float3> sum, int3 c, float3 n)
            {
                int3 m = c - SmoothMin;
                int i = MIndex(m.x, m.y, m.z);
                sum[i] = sum[i] + n;
            }

            static float3 SmoothAt(NativeArray<float3> smooth, int3 c)
            {
                int3 m = c - SmoothMin;
                return smooth[MIndex(m.x, m.y, m.z)];
            }

            static void AddNeighbor(int x, int y, int z, NativeArray<byte> hasVert, NativeArray<float3> rawPos, ref float3 sum, ref int n)
            {
                if (x < 0 || y < 0 || z < 0 || x >= DualCount || y >= DualCount || z >= DualCount)
                    return;
                int i = DIndex(x, y, z);
                if (hasVert[i] == 0)
                    return;
                sum += rawPos[i];
                n++;
            }

            bool IsSolid(int x, int y, int z)
            {
                if (y < 0)
                    return true;
                if (y >= SizeY)
                    return false;
                // Ngoài map theo x/z: lấy cột biên gần nhất → không sinh vách đứng ở mép map (0009).
                x = math.clamp(x, 0, SizeX - 1);
                z = math.clamp(z, 0, SizeZ - 1);
                var m = (BlockMaterial)(Cells[x + SizeX * (z + SizeZ * y)] & 0xFF);
                return m != BlockMaterial.Empty && m != BlockMaterial.Water;
            }

            /// <summary>Toạ độ mẫu (tâm ô con) → toạ độ trong chunk theo ô gameplay.</summary>
            static float3 ToLocal(float3 sample) => (sample + 0.5f) / Resolution;

            static int FloorDiv(int g) => g >= 0 ? g / Resolution : -((-g + Resolution - 1) / Resolution);

            static int SIndex(int x, int y, int z) => x + SampleCount * (z + SampleCount * y);
            static int DIndex(int x, int y, int z) => x + DualCount * (z + DualCount * y);
            static int MIndex(int x, int y, int z) => x + SmoothCount * (z + SmoothCount * y);
            static int OIndex(int x, int y, int z) => x + OutCount * (z + OutCount * y);
            static int OIndex(int3 c) => OIndex(c.x - OutMin, c.y - OutMin, c.z - OutMin);
            static bool InSmooth(int3 c) => math.all(c >= SmoothMin) && math.all(c < SmoothMin + SmoothCount);

            static void EdgeCorners(int e, out int3 a, out int3 b)
            {
                // 12 cạnh của khối lập phương đơn vị: 4 theo x, 4 theo y, 4 theo z.
                int k = e >> 2, i = e & 3;
                int3 p = 0;
                int u = (k + 1) % 3, v = (k + 2) % 3;
                p[u] = i & 1;
                p[v] = (i >> 1) & 1;
                a = p;
                b = p;
                b[k] = 1;
            }
        }
    }
}
