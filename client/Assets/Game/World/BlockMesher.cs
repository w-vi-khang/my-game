using System.Collections.Generic;
using Game.Core;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace Game.World
{
    /// <summary>
    /// Mesh khối bo cạnh (decision 0011, world-tech "Dựng mesh khối bo cạnh", bước 5.1–5.2).
    /// Mỗi mặt ô đặc giáp ô rỗng/nước được chia lưới 4 × 4 điểm (toạ độ 0, r, 1 − r, 1 trên mặt); mỗi điểm được đặt lại lên
    /// mặt "lăn quả cầu bán kính r bên trong khối" (phép mở hình thái học): cạnh lồi thành cung tròn, góc lồi thành mặt cầu,
    /// cạnh lõm giữ vuông. Vị trí mới của một điểm chỉ phụ thuộc 8 ô quanh đỉnh lưới gần nhất → hai mặt dùng chung điểm luôn
    /// tính ra cùng vị trí (mesh kín), và chỉ có 256 tổ hợp → tính sẵn bảng.
    /// Cạnh mặt không có điểm nào dời → bỏ hai điểm giữa (không sinh đỉnh chữ T); mặt không cạnh nào dời → 1 tứ giác.
    /// Màu đỉnh theo vật liệu; sườn và đáy ô cỏ mang màu đất (mép cỏ là bước 5.3). Collider: mặt khối vuông.
    /// </summary>
    public static class BlockMesher
    {
        /// <summary>
        /// Số ô gameplay quanh chunk mà một chunk đọc: mặt lộ phụ thuộc 6 ô kề, hình bo phụ thuộc 8 ô quanh mỗi đỉnh của ô → đệm 1 ô.
        /// Sửa ô cách biên ≤ CellMargin → chunk bên cạnh cũng phải dựng lại (WorldEditor.AffectedChunks).
        /// </summary>
        public const int CellMargin = 1;

        /// <summary>Bán kính bo cạnh (ô). 1/8 biểu diễn đúng trong float → điểm thẳng hàng trên cạnh không lệch.</summary>
        public const float BevelRadius = 0.125f;

        /// <summary>Màu sRGB (R, G, B) theo vật liệu — tạm cho bước 5.1; texture + ánh sáng tinh chỉnh ở bước 5.4.</summary>
        public static uint TopColor(BlockMaterial m) => m switch
        {
            BlockMaterial.Grass => Pack(118, 186, 72),
            BlockMaterial.Dirt => Pack(196, 154, 104),
            BlockMaterial.Stone => Pack(150, 150, 146),
            BlockMaterial.Sand => Pack(228, 212, 152),
            BlockMaterial.Bedrock => Pack(90, 88, 86),
            _ => Pack(255, 0, 255), // không nên xuất hiện — màu chói để dễ thấy lỗi
        };

        /// <summary>Màu sườn/đáy: ô cỏ lộ vách đất (như khối cỏ tham khảo trong 0011); vật liệu khác giữ màu của nó.</summary>
        public static uint SideColor(BlockMaterial m) => m == BlockMaterial.Grass ? TopColor(BlockMaterial.Dirt) : TopColor(m);

        static uint Pack(byte r, byte g, byte b) => r | (uint)g << 8 | (uint)b << 16 | 0xFFu << 24;

        // ---- Bảng bo cạnh: [tổ hợp 8 ô (256)] × [điểm lệch khỏi đỉnh lưới (27, mỗi trục −r/0/+r)] ----
        const int TableOffsets = 27;
        static float3[] tableOffset, tableNormal;
        static byte[] tableMoved;

        /// <summary>Chỉ số điểm lệch: mỗi trục 0 → 0, +r → 1, −r → 2.</summary>
        static int OffsetIndex(int3 k) => k.x + 3 * k.y + 9 * k.z;

        static void EnsureTable()
        {
            if (tableOffset != null)
                return;
            var off = new NativeArray<float3>(256 * TableOffsets, Allocator.TempJob);
            var nrm = new NativeArray<float3>(256 * TableOffsets, Allocator.TempJob);
            var moved = new NativeArray<byte>(256 * TableOffsets, Allocator.TempJob);
            try
            {
                new BevelTableJob { R = BevelRadius, Offset = off, Normal = nrm, Moved = moved }.Run();
                tableNormal = nrm.ToArray();
                tableMoved = moved.ToArray();
                tableOffset = off.ToArray(); // gán cuối: các mảng khác đã sẵn khi tableOffset != null
            }
            finally
            {
                off.Dispose();
                nrm.Dispose();
                moved.Dispose();
            }
        }

        /// <summary>
        /// Với mỗi tổ hợp 8 ô quanh đỉnh lưới L (bit dx + 2dy + 4dz = ô L + (dx−1, dy−1, dz−1) đặc) và mỗi điểm p = L + lệch:
        /// tìm q gần p nhất cách mọi ô rỗng ≥ r (lõi khối sau khi co r), rồi đặt p' = q + r·chuẩn hoá(p − q).
        /// q chọn trong tập ứng viên p + r·(tổng các nhóm trục, mỗi nhóm chuẩn hoá) — đủ cho mặt phẳng, cạnh lồi/lõm, góc lồi
        /// và chỗ cạnh lồi rẽ góc lõm; hoà thì ưu tiên ứng viên dùng nhiều trục hơn (đối xứng).
        /// </summary>
        [BurstCompile(CompileSynchronously = true)]
        struct BevelTableJob : IJob
        {
            public float R;
            public NativeArray<float3> Offset, Normal;
            public NativeArray<byte> Moved;

            public void Execute()
            {
                for (int config = 0; config < 256; config++)
                for (int kz = 0; kz < 3; kz++)
                for (int ky = 0; ky < 3; ky++)
                for (int kx = 0; kx < 3; kx++)
                {
                    float3 p = new float3(Step(kx), Step(ky), Step(kz)) * R;
                    int slot = config * TableOffsets + OffsetIndex(new int3(kx, ky, kz));
                    float bestScore = float.MaxValue;
                    float3 bestQ = p;
                    for (int oz = -1; oz <= 1; oz++)
                    for (int oy = -1; oy <= 1; oy++)
                    for (int ox = -1; ox <= 1; ox++)
                    {
                        int3 o = new int3(ox, oy, oz);
                        int m = math.abs(ox) + math.abs(oy) + math.abs(oz);
                        if (m == 0)
                            continue;
                        // Các cách chia trục khác 0 thành nhóm: m = 1 → 1 cách; m = 2 → riêng / gộp; m = 3 → riêng / gộp cả / gộp cặp + 1 riêng (3 cách).
                        int partitions = m == 1 ? 1 : m == 2 ? 2 : 5;
                        for (int part = 0; part < partitions; part++)
                        {
                            float3 dir = o;
                            float3 step;
                            int groups;
                            if (part == 0)
                            {
                                step = dir;
                                groups = m;
                            }
                            else if (part == 1)
                            {
                                step = math.normalize(dir);
                                groups = 1;
                            }
                            else
                            {
                                int single = part - 2;
                                float3 pair = dir;
                                pair[single] = 0;
                                float3 one = 0;
                                one[single] = dir[single];
                                step = math.normalize(pair) + one;
                                groups = 2;
                            }
                            float3 q = p + R * step;
                            float len = math.length(q - p);
                            float score = len - 1e-4f * m + 1e-5f * groups;
                            if (score < bestScore && Feasible(q, config))
                            {
                                bestScore = score;
                                bestQ = q;
                            }
                        }
                    }
                    float3 d = p - bestQ;
                    float dl = math.length(d);
                    if (bestScore == float.MaxValue || dl < 1e-6f)
                    {
                        Offset[slot] = p;
                        Normal[slot] = 0;
                        Moved[slot] = 0;
                        continue;
                    }
                    float3 n = d / dl;
                    float3 s = bestQ + R * n;
                    bool moved = math.distance(s, p) > 1e-5f;
                    // Điểm không dời giữ đúng toạ độ gốc (bội của 1/8) → điểm trên cạnh thẳng hàng tuyệt đối, hàn đỉnh không lệch.
                    Offset[slot] = moved ? s : p;
                    Normal[slot] = moved ? n : 0;
                    Moved[slot] = moved ? (byte)1 : (byte)0;
                }
            }

            static float Step(int k) => k == 0 ? 0f : k == 1 ? 1f : -1f;

            /// <summary>q cách mọi ô rỗng trong 8 ô quanh đỉnh ≥ R (sai số nhỏ cho điểm nằm đúng trên biên).</summary>
            bool Feasible(float3 q, int config)
            {
                for (int k = 0; k < 8; k++)
                {
                    if ((config >> k & 1) != 0)
                        continue;
                    float3 lo = new float3(k & 1, k >> 1 & 1, k >> 2 & 1) - 1f;
                    float3 outside = math.max(0, math.max(lo - q, q - (lo + 1f)));
                    if (math.length(outside) < R - 1e-5f)
                        return false;
                }
                return true;
            }
        }

        public static ChunkMesh Build(WorldData world, int chunkX, int chunkY, int chunkZ) =>
            BuildChunks(world, new[] { world.Size.ChunkIndex(chunkX * WorldSize.ChunkSize, chunkY * WorldSize.ChunkSize, chunkZ * WorldSize.ChunkSize) })[0];

        /// <summary>Dựng mọi chunk song song (mỗi chunk một job, dùng chung mảng ô chỉ đọc). Thứ tự: x nhanh nhất, rồi z, rồi y.</summary>
        public static ChunkMesh[] BuildAll(WorldData world)
        {
            var all = new int[world.Size.ChunkCount];
            for (int c = 0; c < all.Length; c++)
                all[c] = c;
            return BuildChunks(world, all);
        }

        /// <summary>Dựng song song các chunk theo chỉ số (x nhanh nhất, rồi z, rồi y); kết quả cùng thứ tự với <paramref name="chunkIndices"/>.</summary>
        public static ChunkMesh[] BuildChunks(WorldData world, IReadOnlyList<int> chunkIndices)
        {
            EnsureTable();
            var size = world.Size;
            int count = chunkIndices.Count;
            var cells = new NativeArray<ushort>(world.Cells, Allocator.TempJob);
            var palette = new NativeArray<uint>(2 * 256, Allocator.TempJob);
            for (int m = 0; m < 256; m++)
            {
                palette[m] = TopColor((BlockMaterial)m);
                palette[256 + m] = SideColor((BlockMaterial)m);
            }
            var offset = new NativeArray<float3>(tableOffset, Allocator.TempJob);
            var normal = new NativeArray<float3>(tableNormal, Allocator.TempJob);
            var moved = new NativeArray<byte>(tableMoved, Allocator.TempJob);
            var outputs = new Output[count];
            var handles = new NativeArray<JobHandle>(count, Allocator.Temp);
            try
            {
                for (int k = 0; k < count; k++)
                {
                    int c = chunkIndices[k];
                    int cx = c % size.ChunksX, cz = c / size.ChunksX % size.ChunksZ, cy = c / (size.ChunksX * size.ChunksZ);
                    outputs[k] = Output.Create();
                    handles[k] = new MeshJob
                    {
                        Cells = cells,
                        Palette = palette,
                        TableOffset = offset, TableNormal = normal, TableMoved = moved,
                        R = BevelRadius,
                        SizeX = size.X, SizeY = size.Y, SizeZ = size.Z,
                        Origin = new int3(cx, cy, cz) * WorldSize.ChunkSize,
                        Out = outputs[k],
                    }.Schedule();
                }
                JobHandle.CompleteAll(handles);
                var result = new ChunkMesh[count];
                for (int k = 0; k < count; k++)
                    result[k] = outputs[k].ToChunkMesh();
                return result;
            }
            finally
            {
                handles.Dispose();
                foreach (var o in outputs)
                    o.Dispose();
                moved.Dispose();
                normal.Dispose();
                offset.Dispose();
                palette.Dispose();
                cells.Dispose();
            }
        }

        struct Output
        {
            public NativeList<float3> Vertices, Normals, ColliderVertices;
            public NativeList<uint> Colors;
            public NativeList<int> Indices, ColliderIndices;

            public static Output Create() => new Output
            {
                Vertices = new NativeList<float3>(Allocator.TempJob),
                Normals = new NativeList<float3>(Allocator.TempJob),
                Colors = new NativeList<uint>(Allocator.TempJob),
                Indices = new NativeList<int>(Allocator.TempJob),
                ColliderVertices = new NativeList<float3>(Allocator.TempJob),
                ColliderIndices = new NativeList<int>(Allocator.TempJob),
            };

            public ChunkMesh ToChunkMesh() => new ChunkMesh
            {
                Vertices = Vertices.AsArray().ToArray(),
                Normals = Normals.AsArray().ToArray(),
                Colors = Colors.AsArray().ToArray(),
                Indices = Indices.AsArray().ToArray(),
                ColliderVertices = ColliderVertices.AsArray().ToArray(),
                ColliderIndices = ColliderIndices.AsArray().ToArray(),
            };

            public void Dispose()
            {
                if (Vertices.IsCreated) Vertices.Dispose();
                if (Normals.IsCreated) Normals.Dispose();
                if (Colors.IsCreated) Colors.Dispose();
                if (Indices.IsCreated) Indices.Dispose();
                if (ColliderVertices.IsCreated) ColliderVertices.Dispose();
                if (ColliderIndices.IsCreated) ColliderIndices.Dispose();
            }
        }

        [BurstCompile(CompileSynchronously = true)]
        struct MeshJob : IJob
        {
            const int N = WorldSize.ChunkSize;
            const int V = N + 1; // đỉnh lưới 0..16 mỗi trục

            [ReadOnly] public NativeArray<ushort> Cells;
            /// <summary>[0..255] màu mặt trên, [256..511] màu sườn/đáy theo vật liệu.</summary>
            [ReadOnly] public NativeArray<uint> Palette;
            [ReadOnly] public NativeArray<float3> TableOffset, TableNormal;
            [ReadOnly] public NativeArray<byte> TableMoved;
            public float R;
            public int SizeX, SizeY, SizeZ;
            public int3 Origin;
            public Output Out;

            public void Execute()
            {
                // Tổ hợp 8 ô quanh mỗi đỉnh lưới của chunk (đọc ô −1..16 → đệm 1 ô).
                var config = new NativeArray<byte>(V * V * V, Allocator.Temp);
                for (int y = 0; y < V; y++)
                for (int z = 0; z < V; z++)
                for (int x = 0; x < V; x++)
                {
                    int bits = 0;
                    for (int k = 0; k < 8; k++)
                    {
                        int3 cell = Origin + new int3(x, y, z) + new int3(k & 1, k >> 1 & 1, k >> 2 & 1) - 1;
                        if (IsSolid(cell))
                            bits |= 1 << k;
                    }
                    config[x + V * (z + V * y)] = (byte)bits;
                }

                var gridVert = new NativeArray<int>(16, Allocator.Temp);
                for (int y = 0; y < N; y++)
                for (int z = 0; z < N; z++)
                for (int x = 0; x < N; x++)
                {
                    int3 g = Origin + new int3(x, y, z);
                    if (g.x >= SizeX || g.y >= SizeY || g.z >= SizeZ)
                        continue;
                    int m = Cells[g.x + SizeX * (g.z + SizeZ * g.y)] & 0xFF;
                    if (!IsSolidMaterial(m))
                        continue;
                    for (int axis = 0; axis < 3; axis++)
                    for (int sign = -1; sign <= 1; sign += 2)
                    {
                        int3 nb = g;
                        nb[axis] += sign;
                        if (IsSolid(nb))
                            continue;
                        uint color = axis == 1 && sign > 0 ? Palette[m] : Palette[256 + m];
                        AddColliderFace(new int3(x, y, z), axis, sign);
                        AddFace(new int3(x, y, z), axis, sign, color, config, gridVert);
                    }
                }
                gridVert.Dispose();
                config.Dispose();
            }

            /// <summary>Mặt khối bo: lưới 4 × 4 điểm, điểm (i, j) ở toạ độ mặt (F(i), F(j)), F = 0, r, 1 − r, 1.</summary>
            void AddFace(int3 c, int axis, int sign, uint color, NativeArray<byte> config, NativeArray<int> gridVert)
            {
                int u = (axis + 1) % 3, v = (axis + 2) % 3;
                int3 planeBase = Origin + c; // toạ độ thế giới (Lookup trừ Origin để tra tổ hợp của chunk)
                if (sign > 0)
                    planeBase[axis] += 1;
                float3 faceNormal = 0;
                faceNormal[axis] = sign;

                // Cạnh nào có điểm dời: dưới (j = 0), trên (j = 3), trái (i = 0), phải (i = 3).
                bool bottom = false, top = false, left = false, right = false;
                for (int k = 0; k < 4; k++)
                {
                    bottom |= Moved(planeBase, axis, u, v, k, 0, config);
                    top |= Moved(planeBase, axis, u, v, k, 3, config);
                    left |= Moved(planeBase, axis, u, v, 0, k, config);
                    right |= Moved(planeBase, axis, u, v, 3, k, config);
                }
                if (!bottom && !top && !left && !right)
                {
                    AddFlatFace(c, axis, sign, color);
                    return;
                }

                for (int k = 0; k < 16; k++)
                    gridVert[k] = -1;
                for (int j = 0; j < 3; j++)
                for (int i = 0; i < 3; i++)
                {
                    int a = Vert(Remap(i, j, bottom, top, left, right), planeBase, axis, u, v, faceNormal, color, config, gridVert);
                    int b = Vert(Remap(i + 1, j, bottom, top, left, right), planeBase, axis, u, v, faceNormal, color, config, gridVert);
                    int cc = Vert(Remap(i + 1, j + 1, bottom, top, left, right), planeBase, axis, u, v, faceNormal, color, config, gridVert);
                    int d = Vert(Remap(i, j + 1, bottom, top, left, right), planeBase, axis, u, v, faceNormal, color, config, gridVert);
                    // Quy ước như AddFlatFace: cross(du, dv) = +trục → mặt dương (a, b, c)(a, c, d), mặt âm đảo.
                    if (sign > 0)
                    {
                        AddTriangle(a, b, cc);
                        AddTriangle(a, cc, d);
                    }
                    else
                    {
                        AddTriangle(a, cc, b);
                        AddTriangle(a, d, cc);
                    }
                }
            }

            /// <summary>Cạnh không có điểm dời → hai điểm giữa của cạnh nhập vào hai góc (mặt kề cũng chỉ có 2 điểm ở cạnh đó).</summary>
            static int Remap(int i, int j, bool bottom, bool top, bool left, bool right)
            {
                if (j == 0 && !bottom && (i == 1 || i == 2)) i = i == 1 ? 0 : 3;
                else if (j == 3 && !top && (i == 1 || i == 2)) i = i == 1 ? 0 : 3;
                if (i == 0 && !left && (j == 1 || j == 2)) j = j == 1 ? 0 : 3;
                else if (i == 3 && !right && (j == 1 || j == 2)) j = j == 1 ? 0 : 3;
                return i + 4 * j;
            }

            int Vert(int key, int3 planeBase, int axis, int u, int v, float3 faceNormal, uint color, NativeArray<byte> config, NativeArray<int> gridVert)
            {
                if (gridVert[key] >= 0)
                    return gridVert[key];
                int i = key & 3, j = key >> 2;
                Lookup(planeBase, axis, u, v, i, j, config, out int3 lattice, out int slot);
                float3 pos = (float3)(lattice - Origin) + TableOffset[slot];
                float3 nrm = TableMoved[slot] != 0 ? TableNormal[slot] : faceNormal;
                int index = Out.Vertices.Length;
                Out.Vertices.Add(pos);
                Out.Normals.Add(nrm);
                Out.Colors.Add(color);
                gridVert[key] = index;
                return index;
            }

            bool Moved(int3 planeBase, int axis, int u, int v, int i, int j, NativeArray<byte> config)
            {
                Lookup(planeBase, axis, u, v, i, j, config, out _, out int slot);
                return TableMoved[slot] != 0;
            }

            /// <summary>Điểm (i, j) của mặt → đỉnh lưới gần nhất (toạ độ thế giới) + ô trong bảng bo.</summary>
            void Lookup(int3 planeBase, int axis, int u, int v, int i, int j, NativeArray<byte> config, out int3 lattice, out int slot)
            {
                lattice = planeBase;
                if (i >= 2) lattice[u] += 1;
                if (j >= 2) lattice[v] += 1;
                int3 k = 0; // lệch theo trục: 0 → 0, +r → 1, −r → 2
                k[u] = i == 1 ? 1 : i == 2 ? 2 : 0;
                k[v] = j == 1 ? 1 : j == 2 ? 2 : 0;
                int3 l = lattice - Origin;
                slot = config[l.x + V * (l.z + V * l.y)] * TableOffsets + OffsetIndex(k);
            }

            void AddTriangle(int a, int b, int c)
            {
                if (a == b || b == c || a == c)
                    return;
                Out.Indices.Add(a);
                Out.Indices.Add(b);
                Out.Indices.Add(c);
            }

            /// <summary>Tứ giác phẳng trên mặt <paramref name="axis"/> phía <paramref name="sign"/> của ô cục bộ <paramref name="c"/>.
            /// Thứ tự đỉnh: cross(b − a, c − a) cùng hướng pháp tuyến (test TrianglesFaceTheirNormals).</summary>
            void AddFlatFace(int3 c, int axis, int sign, uint color)
            {
                QuadCorners(c, axis, sign, out float3 p00, out float3 du, out float3 dv, out float3 normal);
                int i0 = Out.Vertices.Length;
                Out.Vertices.Add(p00);
                Out.Vertices.Add(p00 + du);
                Out.Vertices.Add(p00 + du + dv);
                Out.Vertices.Add(p00 + dv);
                for (int k = 0; k < 4; k++)
                {
                    Out.Normals.Add(normal);
                    Out.Colors.Add(color);
                }
                AddQuadIndices(Out.Indices, i0, sign);
            }

            void AddColliderFace(int3 c, int axis, int sign)
            {
                QuadCorners(c, axis, sign, out float3 p00, out float3 du, out float3 dv, out _);
                int i0 = Out.ColliderVertices.Length;
                Out.ColliderVertices.Add(p00);
                Out.ColliderVertices.Add(p00 + du);
                Out.ColliderVertices.Add(p00 + du + dv);
                Out.ColliderVertices.Add(p00 + dv);
                AddQuadIndices(Out.ColliderIndices, i0, sign);
            }

            static void QuadCorners(int3 c, int axis, int sign, out float3 p00, out float3 du, out float3 dv, out float3 normal)
            {
                int u = (axis + 1) % 3, v = (axis + 2) % 3;
                p00 = c;
                if (sign > 0)
                    p00[axis] += 1;
                du = 0;
                dv = 0;
                du[u] = 1;
                dv[v] = 1;
                normal = 0;
                normal[axis] = sign;
            }

            static void AddQuadIndices(NativeList<int> indices, int i0, int sign)
            {
                // cross(du, dv) = +trục → mặt dương giữ thứ tự (0,1,2)(0,2,3), mặt âm đảo.
                if (sign > 0)
                {
                    indices.Add(i0); indices.Add(i0 + 1); indices.Add(i0 + 2);
                    indices.Add(i0); indices.Add(i0 + 2); indices.Add(i0 + 3);
                }
                else
                {
                    indices.Add(i0); indices.Add(i0 + 2); indices.Add(i0 + 1);
                    indices.Add(i0); indices.Add(i0 + 3); indices.Add(i0 + 2);
                }
            }

            static bool IsSolidMaterial(int m) => m != (int)BlockMaterial.Empty && m != (int)BlockMaterial.Water;

            bool IsSolid(int3 p)
            {
                if (p.y < 0)
                    return true;
                if (p.y >= SizeY)
                    return false;
                // Ngoài map theo x/z: lấy cột biên gần nhất → không sinh vách đứng ở mép map (0009).
                int x = math.clamp(p.x, 0, SizeX - 1);
                int z = math.clamp(p.z, 0, SizeZ - 1);
                return IsSolidMaterial(Cells[x + SizeX * (z + SizeZ * p.y)] & 0xFF);
            }
        }
    }
}
