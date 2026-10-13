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

        // ---- Mép cỏ (bước 5.3, glossary "Mép cỏ"): chỉ hiển thị, không vào collider ----
        /// <summary>Mặt trên ô cỏ nhô ra khỏi sườn (ô).</summary>
        public const float LipOverhang = 0.08f;
        /// <summary>Mép trên của váy cỏ lùi vào so với LipOverhang → cạnh ngoài hơi tròn khi chiếu sáng.</summary>
        public const float LipRound = 0.04f;
        /// <summary>Độ dài váy cỏ dưới mặt trên: LipDepth ± LipWave1 ± LipWave2 (lượn sóng theo toạ độ dọc cạnh).</summary>
        public const float LipDepth = 0.22f, LipWave1 = 0.06f, LipWave2 = 0.03f;
        /// <summary>Số đoạn dọc mỗi cạnh ô của váy cỏ / số đoạn của mảnh góc 1/4 vòng.</summary>
        public const int LipSegments = 4, LipCornerSegments = 3;

        /// <summary>
        /// Độ dài váy cỏ tại toạ độ thế giới <paramref name="w"/> dọc một đường cạnh. Đường cạnh xác định bởi trục sườn
        /// (0 = x, 2 = z), hướng sườn, toạ độ mặt sườn và độ cao mặt trên → pha riêng mỗi đường; cùng w → cùng giá trị
        /// (liền giữa hai ô kề và qua biên chunk).
        /// </summary>
        public static float LipDepthAt(float w, int sideAxis, int sideSign, int plane, int top)
        {
            uint h = math.hash(new int4(plane, top, sideAxis, sideSign));
            float phase = (h & 0xFFFF) / 65536f * 2f * math.PI;
            return LipDepth + LipWave1 * math.sin(2f * math.PI * 0.9f * w + phase)
                            + LipWave2 * math.sin(2f * math.PI * 2.1f * w + 1.7f * phase);
        }

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

        /// <param name="grassLip">false → bỏ mép cỏ (để kiểm mesh khối kín; mép cỏ là bề mặt hở chồng lên khối).</param>
        public static ChunkMesh Build(WorldData world, int chunkX, int chunkY, int chunkZ, bool grassLip = true) =>
            BuildChunks(world, new[] { world.Size.ChunkIndex(chunkX * WorldSize.ChunkSize, chunkY * WorldSize.ChunkSize, chunkZ * WorldSize.ChunkSize) }, grassLip)[0];

        /// <summary>Dựng mọi chunk song song (mỗi chunk một job, dùng chung mảng ô chỉ đọc). Thứ tự: x nhanh nhất, rồi z, rồi y.</summary>
        public static ChunkMesh[] BuildAll(WorldData world, bool grassLip = true)
        {
            var all = new int[world.Size.ChunkCount];
            for (int c = 0; c < all.Length; c++)
                all[c] = c;
            return BuildChunks(world, all, grassLip);
        }

        /// <summary>Dựng song song các chunk theo chỉ số (x nhanh nhất, rồi z, rồi y); kết quả cùng thứ tự với <paramref name="chunkIndices"/>.</summary>
        public static ChunkMesh[] BuildChunks(WorldData world, IReadOnlyList<int> chunkIndices, bool grassLip = true)
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
                        GrassLip = grassLip,
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
            public bool GrassLip;
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
                    if (GrassLip && m == (int)BlockMaterial.Grass && !IsSolid(g + new int3(0, 1, 0)))
                        AddGrassLip(new int3(x, y, z), g, Palette[m]);
                }
                gridVert.Dispose();
                config.Dispose();
            }

            /// <summary>
            /// Mặt khối bo: lưới 4 × 4 điểm, điểm (i, j) ở toạ độ mặt (F(i), F(j)), F = 0, r, 1 − r, 1.
            /// Cạnh mặt có điểm dời → giữ cả 4 điểm của cạnh (mặt kề cũng giữ → không đỉnh chữ T). Đường lưới bên trong chỉ giữ khi
            /// cạnh tương ứng cong (điểm giữa cạnh dời) hoặc có góc dời mà không cạnh kề nào cong; mỗi ô lưới rút gọn là đa giác lồi
            /// (có thể có điểm thẳng hàng trên cạnh) → cắt tai thành tam giác.
            /// </summary>
            void AddFace(int3 c, int axis, int sign, uint color, NativeArray<byte> config, NativeArray<int> gridVert)
            {
                int u = (axis + 1) % 3, v = (axis + 2) % 3;
                int3 planeBase = Origin + c; // toạ độ thế giới (Lookup trừ Origin để tra tổ hợp của chunk)
                if (sign > 0)
                    planeBase[axis] += 1;
                float3 faceNormal = 0;
                faceNormal[axis] = sign;

                int moved = 0; // bit i + 4j = điểm (i, j) dời
                for (int k = 0; k < 16; k++)
                {
                    int i = k & 3, j = k >> 2;
                    bool onEdge = i == 0 || i == 3 || j == 0 || j == 3; // điểm trong (1..2, 1..2) luôn nằm trên mặt phẳng
                    if (onEdge && Moved(planeBase, axis, u, v, i, j, config))
                        moved |= 1 << k;
                }
                if (moved == 0)
                {
                    AddFlatFace(c, axis, sign, color);
                    return;
                }
                bool curvedB = Bit(moved, 1, 0) || Bit(moved, 2, 0), curvedT = Bit(moved, 1, 3) || Bit(moved, 2, 3);
                bool curvedL = Bit(moved, 0, 1) || Bit(moved, 0, 2), curvedR = Bit(moved, 3, 1) || Bit(moved, 3, 2);
                bool splitB = curvedB || Bit(moved, 0, 0) || Bit(moved, 3, 0), splitT = curvedT || Bit(moved, 0, 3) || Bit(moved, 3, 3);
                bool splitL = curvedL || Bit(moved, 0, 0) || Bit(moved, 0, 3), splitR = curvedR || Bit(moved, 3, 0) || Bit(moved, 3, 3);
                bool row1 = curvedB, row2 = curvedT, col1 = curvedL, col2 = curvedR;
                // Góc dời mà không cạnh kề nào cong (vd cạnh lồi rẽ góc lõm) → giữ hai đường lưới quanh góc cho phần cong nằm gọn ở góc.
                if (Bit(moved, 0, 0) && !curvedB && !curvedL) { row1 = true; col1 = true; }
                if (Bit(moved, 3, 0) && !curvedB && !curvedR) { row1 = true; col2 = true; }
                if (Bit(moved, 0, 3) && !curvedT && !curvedL) { row2 = true; col1 = true; }
                if (Bit(moved, 3, 3) && !curvedT && !curvedR) { row2 = true; col2 = true; }
                // Đường lưới trong có đầu mút trên cạnh mặt không tách sẽ bị gộp vào góc (Snap) → dải cong suy biến. Khi đó giữ đường
                // vuông góc để dải có điểm trong (vd cạnh lồi nằm trong khe, hai đầu là góc lõm). Lặp tới khi ổn định (tối đa 2 lượt).
                for (int pass = 0; pass < 2; pass++)
                {
                    if ((row1 || row2) && !splitL) col1 = true;
                    if ((row1 || row2) && !splitR) col2 = true;
                    if ((col1 || col2) && !splitB) row1 = true;
                    if ((col1 || col2) && !splitT) row2 = true;
                }

                var cols = new FixedList32Bytes<byte>();
                var rows = new FixedList32Bytes<byte>();
                cols.Add(0); if (col1) cols.Add(1); if (col2) cols.Add(2); cols.Add(3);
                rows.Add(0); if (row1) rows.Add(1); if (row2) rows.Add(2); rows.Add(3);

                for (int k = 0; k < 16; k++)
                    gridVert[k] = -1;
                for (int rj = 0; rj + 1 < rows.Length; rj++)
                for (int ci = 0; ci + 1 < cols.Length; ci++)
                {
                    int c0 = cols[ci], c1 = cols[ci + 1], r0 = rows[rj], r1 = rows[rj + 1];
                    // Đa giác ô theo chiều ngược kim đồng hồ trong (u, v): dưới → phải → trên → trái; điểm giữa chỉ có trên cạnh mặt đã tách.
                    var poly = new FixedList64Bytes<byte>();
                    poly.Add((byte)(c0 + 4 * r0));
                    if (r0 == 0 && splitB) for (int i = c0 + 1; i < c1; i++) poly.Add((byte)i);
                    poly.Add((byte)(c1 + 4 * r0));
                    if (c1 == 3 && splitR) for (int j = r0 + 1; j < r1; j++) poly.Add((byte)(3 + 4 * j));
                    poly.Add((byte)(c1 + 4 * r1));
                    if (r1 == 3 && splitT) for (int i = c1 - 1; i > c0; i--) poly.Add((byte)(i + 12));
                    poly.Add((byte)(c0 + 4 * r1));
                    if (c0 == 0 && splitL) for (int j = r1 - 1; j > r0; j--) poly.Add((byte)(4 * j));
                    // Đầu mút đường lưới trong nằm trên cạnh mặt không tách → gộp vào góc (mặt kề chỉ có 2 điểm ở cạnh đó).
                    var snapped = new FixedList64Bytes<byte>();
                    for (int k = 0; k < poly.Length; k++)
                    {
                        byte key = Snap(poly[k], splitB, splitT, splitL, splitR);
                        if (snapped.Length == 0 || snapped[snapped.Length - 1] != key)
                            snapped.Add(key);
                    }
                    if (snapped.Length > 1 && snapped[0] == snapped[snapped.Length - 1])
                        snapped.RemoveAt(snapped.Length - 1);
                    ClipEars(snapped, sign, planeBase, axis, u, v, faceNormal, color, config, gridVert);
                }
            }

            /// <summary>Cắt tai đa giác lồi (toạ độ danh nghĩa trên mặt) — bỏ qua đỉnh thẳng hàng; tam giác ngược kim đồng hồ trong (u, v)
            /// là mặt trước khi sign > 0 (cùng quy ước AddFlatFace), đảo khi sign &lt; 0.</summary>
            void ClipEars(FixedList64Bytes<byte> poly, int sign, int3 planeBase, int axis, int u, int v, float3 faceNormal, uint color,
                NativeArray<byte> config, NativeArray<int> gridVert)
            {
                int guard = 0;
                while (poly.Length >= 3 && guard++ < 64)
                {
                    bool clipped = false;
                    for (int k = 0; k < poly.Length; k++)
                    {
                        int kp = (k + poly.Length - 1) % poly.Length, kn = (k + 1) % poly.Length;
                        float2 a = Nominal(poly[kp]), b = Nominal(poly[k]), cc = Nominal(poly[kn]);
                        float area = (b.x - a.x) * (cc.y - a.y) - (b.y - a.y) * (cc.x - a.x);
                        if (area <= 1e-6f)
                            continue; // thẳng hàng (hoặc lõm — không có ở đa giác lồi)
                        bool inside = false;
                        for (int t = 0; t < poly.Length && !inside; t++)
                        {
                            if (t == kp || t == k || t == kn)
                                continue;
                            inside = InTriangle(Nominal(poly[t]), a, b, cc);
                        }
                        if (inside)
                            continue;
                        int ia = Vert(poly[kp], planeBase, axis, u, v, faceNormal, color, config, gridVert);
                        int ib = Vert(poly[k], planeBase, axis, u, v, faceNormal, color, config, gridVert);
                        int ic = Vert(poly[kn], planeBase, axis, u, v, faceNormal, color, config, gridVert);
                        if (sign > 0) AddTriangle(ia, ib, ic);
                        else AddTriangle(ia, ic, ib);
                        poly.RemoveAt(k);
                        clipped = true;
                        break;
                    }
                    if (!clipped)
                        break; // còn lại toàn điểm thẳng hàng
                }
            }

            /// <summary>Điểm giữa trên cạnh mặt không tách → góc gần nhất của cạnh đó.</summary>
            static byte Snap(byte key, bool splitB, bool splitT, bool splitL, bool splitR)
            {
                int i = key & 3, j = key >> 2;
                if (j == 0 && !splitB && (i == 1 || i == 2)) i = i == 1 ? 0 : 3;
                else if (j == 3 && !splitT && (i == 1 || i == 2)) i = i == 1 ? 0 : 3;
                if (i == 0 && !splitL && (j == 1 || j == 2)) j = j == 1 ? 0 : 3;
                else if (i == 3 && !splitR && (j == 1 || j == 2)) j = j == 1 ? 0 : 3;
                return (byte)(i + 4 * j);
            }

            static bool Bit(int moved, int i, int j) => (moved >> (i + 4 * j) & 1) != 0;

            float2 Nominal(int key)
            {
                int i = key & 3, j = key >> 2;
                return new float2(F(i), F(j));
            }

            float F(int i) => i == 0 ? 0f : i == 1 ? R : i == 2 ? 1f - R : 1f;

            /// <summary>p nằm trong hoặc trên tam giác (a, b, c) ngược kim đồng hồ.</summary>
            static bool InTriangle(float2 p, float2 a, float2 b, float2 c)
            {
                float d0 = (b.x - a.x) * (p.y - a.y) - (b.y - a.y) * (p.x - a.x);
                float d1 = (c.x - b.x) * (p.y - b.y) - (c.y - b.y) * (p.x - b.x);
                float d2 = (a.x - c.x) * (p.y - c.y) - (a.y - c.y) * (p.x - c.x);
                return d0 >= -1e-6f && d1 >= -1e-6f && d2 >= -1e-6f;
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

            // ---------------- Mép cỏ (bước 5.3) ----------------

            /// <summary>Ô cỏ lộ mặt trên: mỗi sườn x/z lộ → một dải mép cỏ; góc lồi (hai sườn kề cùng lộ) → mảnh 1/4 vòng nối.</summary>
            void AddGrassLip(int3 c, int3 g, uint color)
            {
                bool xp = !IsSolid(g + new int3(1, 0, 0)), xn = !IsSolid(g + new int3(-1, 0, 0));
                bool zp = !IsSolid(g + new int3(0, 0, 1)), zn = !IsSolid(g + new int3(0, 0, -1));
                if (xp) AddLipEdge(c, g, 0, 1, color);
                if (xn) AddLipEdge(c, g, 0, -1, color);
                if (zp) AddLipEdge(c, g, 2, 1, color);
                if (zn) AddLipEdge(c, g, 2, -1, color);
                if (xp && zp) AddLipCorner(c, g, 1, 1, color);
                if (xp && zn) AddLipCorner(c, g, 1, -1, color);
                if (xn && zp) AddLipCorner(c, g, -1, 1, color);
                if (xn && zn) AddLipCorner(c, g, -1, -1, color);
            }

            /// <summary>Điểm trên mặt cắt mép cỏ: t = khoảng cách ra ngoài mặt sườn, dy = độ cao so với mặt trên, sigma = vị trí dọc cạnh 0..1.</summary>
            static float3 LipPoint(int3 c, int sideAxis, int sideSign, float t, float dy, float sigma)
            {
                int edgeAxis = sideAxis == 0 ? 2 : 0;
                float3 p = c;
                p[sideAxis] += (sideSign > 0 ? 1 : 0) + sideSign * t;
                p[edgeAxis] += sigma;
                p.y += 1 + dy;
                return p;
            }

            /// <summary>Ô bên cạnh có mép cỏ cùng sườn → dải mép nối tiếp, không cần nắp.</summary>
            bool HasLip(int3 cell, int sideAxis, int sideSign)
            {
                if (MaterialAt(cell) != (int)BlockMaterial.Grass || IsSolid(cell + new int3(0, 1, 0)))
                    return false;
                int3 side = cell;
                side[sideAxis] += sideSign;
                return !IsSolid(side);
            }

            void AddLipEdge(int3 c, int3 g, int sideAxis, int sideSign, uint color)
            {
                int edgeAxis = sideAxis == 0 ? 2 : 0;
                int plane = g[sideAxis] + (sideSign > 0 ? 1 : 0), top = g.y + 1;
                float3 up = new float3(0, 1, 0), outward = 0;
                outward[sideAxis] = sideSign;
                float3 roundN = math.normalize(up + outward);
                float tTop = LipOverhang - LipRound;

                int p0a = AddVertex(LipPoint(c, sideAxis, sideSign, -R, 0, 0), up, color);
                int p0b = AddVertex(LipPoint(c, sideAxis, sideSign, -R, 0, 1), up, color);
                int firstP1 = Out.Vertices.Length;
                for (int k = 0; k <= LipSegments; k++)
                {
                    float sigma = (float)k / LipSegments;
                    float depth = LipDepthAt(g[edgeAxis] + sigma, sideAxis, sideSign, plane, top);
                    AddVertex(LipPoint(c, sideAxis, sideSign, tTop, 0, sigma), roundN, color);           // P1_k = firstP1 + 2k
                    AddVertex(LipPoint(c, sideAxis, sideSign, LipOverhang, -depth, sigma), outward, color); // P3_k = firstP1 + 2k + 1
                }
                // Mặt trên: quạt từ P0a qua P0b và hàng P1.
                AddOriented(p0a, p0b, firstP1 + 2 * LipSegments, up);
                for (int k = LipSegments; k > 0; k--)
                    AddOriented(p0a, firstP1 + 2 * k, firstP1 + 2 * (k - 1), up);
                // Váy: tứ giác giữa hàng P1 và hàng P3.
                for (int k = 0; k < LipSegments; k++)
                {
                    int a = firstP1 + 2 * k, b = firstP1 + 2 * (k + 1);
                    AddOriented(a, b, b + 1, outward);
                    AddOriented(a, b + 1, a + 1, outward);
                }
                // Nắp hai đầu: không nối tiếp ô bên cạnh và không phải góc lồi (góc lồi có mảnh 1/4 vòng).
                for (int end = 0; end < 2; end++)
                {
                    int e = end == 0 ? -1 : 1;
                    int3 next = g;
                    next[edgeAxis] += e;
                    if (HasLip(next, sideAxis, sideSign) || !IsSolid(next))
                        continue;
                    float sigma = end;
                    float depth = LipDepthAt(g[edgeAxis] + sigma, sideAxis, sideSign, plane, top);
                    float3 capN = 0;
                    capN[edgeAxis] = e;
                    int q0 = AddVertex(LipPoint(c, sideAxis, sideSign, -R, 0, sigma), capN, color);
                    int q1 = AddVertex(LipPoint(c, sideAxis, sideSign, tTop, 0, sigma), capN, color);
                    int q3 = AddVertex(LipPoint(c, sideAxis, sideSign, LipOverhang, -depth, sigma), capN, color);
                    int qi = AddVertex(LipPoint(c, sideAxis, sideSign, -R, -depth, sigma), capN, color);
                    AddOriented(q0, q1, q3, capN);
                    AddOriented(q0, q3, qi, capN);
                }
            }

            /// <summary>Mảnh góc lồi: quét mặt cắt mép cỏ 1/4 vòng quanh trục đứng qua góc ô; độ dài váy nội suy giữa hai đầu dải.</summary>
            void AddLipCorner(int3 c, int3 g, int sx, int sz, uint color)
            {
                float3 axisLocal = new float3(c.x + (sx > 0 ? 1 : 0), c.y + 1, c.z + (sz > 0 ? 1 : 0));
                int cornerX = g.x + (sx > 0 ? 1 : 0), cornerZ = g.z + (sz > 0 ? 1 : 0), top = g.y + 1;
                // Dải sườn x chạy dọc z (w = z góc); dải sườn z chạy dọc x (w = x góc).
                float d0 = LipDepthAt(cornerZ, 0, sx, cornerX, top);
                float d1 = LipDepthAt(cornerX, 2, sz, cornerZ, top);
                float3 up = new float3(0, 1, 0);
                float tTop = LipOverhang - LipRound;
                int center = AddVertex(axisLocal, up, color);
                int first = Out.Vertices.Length;
                for (int k = 0; k <= LipCornerSegments; k++)
                {
                    float a = (float)k / LipCornerSegments * 0.5f * math.PI;
                    float3 dir = new float3(sx * math.cos(a), 0, sz * math.sin(a));
                    float depth = math.lerp(d0, d1, (float)k / LipCornerSegments);
                    AddVertex(axisLocal + dir * tTop, math.normalize(up + dir), color);                       // P1_k = first + 2k
                    AddVertex(axisLocal + dir * LipOverhang + new float3(0, -depth, 0), dir, color);          // P3_k = first + 2k + 1
                }
                for (int k = 0; k < LipCornerSegments; k++)
                {
                    int a = first + 2 * k, b = first + 2 * (k + 1);
                    float am = ((float)k + 0.5f) / LipCornerSegments * 0.5f * math.PI;
                    float3 mid = new float3(sx * math.cos(am), 0, sz * math.sin(am));
                    AddOriented(center, a, b, up);
                    AddOriented(a, b, b + 1, mid);
                    AddOriented(a, b + 1, a + 1, mid);
                }
            }

            int AddVertex(float3 p, float3 n, uint color)
            {
                int i = Out.Vertices.Length;
                Out.Vertices.Add(p);
                Out.Normals.Add(n);
                Out.Colors.Add(color);
                return i;
            }

            /// <summary>Thêm tam giác, đảo thứ tự nếu cần để cross(b − a, c − a) cùng hướng <paramref name="want"/> (quy ước mặt trước của mesher).</summary>
            void AddOriented(int a, int b, int c, float3 want)
            {
                float3 pa = Out.Vertices[a], pb = Out.Vertices[b], pc = Out.Vertices[c];
                if (math.dot(math.cross(pb - pa, pc - pa), want) >= 0)
                    AddTriangle(a, b, c);
                else
                    AddTriangle(a, c, b);
            }

            int MaterialAt(int3 p)
            {
                if (p.y < 0)
                    return (int)BlockMaterial.Bedrock;
                if (p.y >= SizeY)
                    return (int)BlockMaterial.Empty;
                int x = math.clamp(p.x, 0, SizeX - 1);
                int z = math.clamp(p.z, 0, SizeZ - 1);
                return Cells[x + SizeX * (z + SizeZ * p.y)] & 0xFF;
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
