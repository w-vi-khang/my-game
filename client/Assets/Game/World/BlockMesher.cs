using System.Collections.Generic;
using Game.Core;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
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
        /// <summary>Mép cỏ nhô ra khỏi sườn ở chỗ phồng nhất — mặt trước (ô, brief 0014).</summary>
        public const float LipOverhang = 0.12f;
        /// <summary>Bán kính cuộn trên (mặt trên cuộn xuống mặt trước) và cuộn dưới (mặt trước cuộn vào đáy) của mặt cắt mép cỏ (brief 0014).</summary>
        public const float LipTopRound = 0.08f, LipBottomRound = 0.06f;
        /// <summary>Độ sâu đáy mép cỏ ở mặt trước: LipDepth ± LipWave1 ± LipWave2 (lượn sóng theo toạ độ dọc cạnh) = 0,205–0,355.</summary>
        public const float LipDepth = 0.28f, LipWave1 = 0.05f, LipWave2 = 0.025f;
        /// <summary>Mặt dưới mép cỏ chạm vách ở độ sâu này rồi chạy phẳng vào trong khối tới −R (brief 0014); nông hơn đáy nông nhất (0,205) → mặt dưới nghiêng lên vào vách.</summary>
        public const float LipFloor = 0.16f;
        /// <summary>Số điểm mặt cắt mép cỏ (LipProfile): A0..A3 cuộn trên, C0..C2 cuộn dưới, W chạm vách.</summary>
        public const int LipProfilePoints = 8;
        /// <summary>Mặt nước của ô nước có không khí phía trên nằm thấp hơn mặt trên ô chừng này (trùng mặt biển nền — OceanBackdrop).</summary>
        public const float WaterSurfaceDrop = 0.1f;
        /// <summary>Bán kính bo (ô) và số đoạn cung mặc định của mặt nước (0043 mục 6, brief 0080).</summary>
        public const float DefaultWaterFilletRadius = 0.3f;
        public const int DefaultWaterFilletSegments = 3;

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

        /// <summary>
        /// Điểm thứ <paramref name="i"/> (0..LipProfilePoints − 1) của mặt cắt mép cỏ khi đáy mặt trước sâu <paramref name="depth"/> (brief 0014):
        /// p = (t ra ngoài mặt sườn, dy so với mặt trên), n = pháp tuyến đơn vị trong mặt cắt (theo t, theo y).
        /// A0..A3 cuộn trên (90° → 0°), C0..C2 cuộn dưới (0° → −90°), W chạm vách ở (0, −LipFloor).
        /// </summary>
        public static void LipProfile(int i, float depth, out float2 p, out float2 n)
        {
            if (i <= 3)
            {
                float a = (3 - i) * (math.PI / 6f);
                n = new float2(math.cos(a), math.sin(a));
                p = new float2(LipOverhang - LipTopRound, -LipTopRound) + LipTopRound * n;
            }
            else if (i <= 6)
            {
                float a = -(i - 4) * (math.PI / 4f);
                n = new float2(math.cos(a), math.sin(a));
                p = new float2(LipOverhang - LipBottomRound, -(depth - LipBottomRound)) + LipBottomRound * n;
            }
            else
            {
                n = new float2(0f, -1f);
                p = new float2(0f, -LipFloor);
            }
        }

        /// <summary>Mã hoa văn ghi ở byte alpha của màu đỉnh — shader Game/Terrain vẽ hoa văn theo mã (bước 5.4).</summary>
        public const byte PatternNone = 0, PatternGrass = 1, PatternDirt = 2, PatternStone = 3, PatternSand = 4;

        /// <summary>Màu sRGB (R, G, B) + mã hoa văn theo vật liệu (bước 5.4: tươi hơn, theo hướng art "tươi sáng").</summary>
        public static uint TopColor(BlockMaterial m) => m switch
        {
            BlockMaterial.Grass => Pack(140, 200, 96, PatternGrass),
            BlockMaterial.Dirt => Pack(222, 180, 126, PatternDirt),
            BlockMaterial.Farmland => Pack(150, 104, 66, PatternDirt),
            BlockMaterial.Stone => Pack(160, 160, 156, PatternStone),
            BlockMaterial.Sand => Pack(238, 220, 160, PatternSand),
            BlockMaterial.Bedrock => Pack(96, 92, 90, PatternStone),
            _ => Pack(255, 0, 255, PatternNone), // không nên xuất hiện — màu chói để dễ thấy lỗi
        };

        /// <summary>Màu sườn/đáy: ô cỏ và ô ruộng lộ vách đất (như khối cỏ tham khảo trong 0011); vật liệu khác giữ màu của nó.</summary>
        public static uint SideColor(BlockMaterial m) => m == BlockMaterial.Grass || m == BlockMaterial.Farmland ? TopColor(BlockMaterial.Dirt) : TopColor(m);

        static uint Pack(byte r, byte g, byte b, byte pattern) => r | (uint)g << 8 | (uint)b << 16 | (uint)pattern << 24;

        /// <summary>Độ sáng theo mức che (AO theo đỉnh, 0fps): 0 = bị che nhiều nhất … 3 = không che.</summary>
        public static readonly float[] AoBrightness = { 0.62f, 0.74f, 0.87f, 1f };

        /// <summary>Nhân RGB của màu đỉnh với độ sáng AO, giữ byte alpha (mã hoa văn).</summary>
        public static uint Shade(uint color, float k)
        {
            uint r = (uint)math.round((color & 0xFF) * k), g = (uint)math.round((color >> 8 & 0xFF) * k), b = (uint)math.round((color >> 16 & 0xFF) * k);
            return r | g << 8 | b << 16 | (color & 0xFF000000u);
        }

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
        public static ChunkMesh Build(WorldData world, int chunkX, int chunkY, int chunkZ, bool grassLip = true, int waterFullLevel = Cell.MaxWaterLevel,
            float waterFilletRadius = DefaultWaterFilletRadius, int waterFilletSegments = DefaultWaterFilletSegments) =>
            BuildChunks(world, new[] { world.Size.ChunkIndex(chunkX * WorldSize.ChunkSize, chunkY * WorldSize.ChunkSize, chunkZ * WorldSize.ChunkSize) }, grassLip, waterFullLevel, waterFilletRadius, waterFilletSegments)[0];

        /// <summary>Dựng mọi chunk song song (mỗi chunk một job, dùng chung mảng ô chỉ đọc). Thứ tự: x nhanh nhất, rồi z, rồi y.</summary>
        public static ChunkMesh[] BuildAll(WorldData world, bool grassLip = true, int waterFullLevel = Cell.MaxWaterLevel,
            float waterFilletRadius = DefaultWaterFilletRadius, int waterFilletSegments = DefaultWaterFilletSegments)
        {
            var all = new int[world.Size.ChunkCount];
            for (int c = 0; c < all.Length; c++)
                all[c] = c;
            return BuildChunks(world, all, grassLip, waterFullLevel, waterFilletRadius, waterFilletSegments);
        }

        /// <summary>Dựng song song các chunk theo chỉ số (x nhanh nhất, rồi z, rồi y); kết quả cùng thứ tự với <paramref name="chunkIndices"/>. Mảng ô của world được ghim và đọc trực tiếp (không chép, brief 0010).</summary>
        public static unsafe ChunkMesh[] BuildChunks(WorldData world, IReadOnlyList<int> chunkIndices, bool grassLip = true, int waterFullLevel = Cell.MaxWaterLevel,
            float waterFilletRadius = DefaultWaterFilletRadius, int waterFilletSegments = DefaultWaterFilletSegments)
        {
            if (!(waterFilletRadius >= 0.05f && waterFilletRadius <= 0.45f))
                throw new System.ArgumentOutOfRangeException(nameof(waterFilletRadius), waterFilletRadius, "waterFilletRadius must be in 0.05..0.45");
            if (waterFilletSegments < 1 || waterFilletSegments > 8)
                throw new System.ArgumentOutOfRangeException(nameof(waterFilletSegments), waterFilletSegments, "waterFilletSegments must be in 1..8");
            if (waterFullLevel < 1)
                throw new System.ArgumentOutOfRangeException(nameof(waterFullLevel), waterFullLevel, "waterFullLevel must be >= 1");
            EnsureTable();
            var size = world.Size;
            int count = chunkIndices.Count;
            void* cellsPtr = UnsafeUtility.PinGCArrayAndGetDataAddress(world.Cells, out ulong cellsGcHandle);
            var cells = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<ushort>(cellsPtr, world.Cells.Length, Allocator.None);
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            var cellsSafety = AtomicSafetyHandle.Create();
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref cells, cellsSafety);
#endif
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
                        WaterFullLevel = waterFullLevel,
                        WaterFilletRadius = waterFilletRadius, WaterFilletSegments = waterFilletSegments,
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
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.Release(cellsSafety);
#endif
                UnsafeUtility.ReleaseGCObject(cellsGcHandle);
            }
        }

        struct Output
        {
            public NativeList<float3> Vertices, Normals, ColliderVertices;
            public NativeList<uint> Colors;
            public NativeList<int> Indices, ColliderIndices, WaterIndices;
            /// <summary>Bảng hàn đỉnh nước của chunk (brief 0080).</summary>
            public NativeHashMap<WaterVertexKey, int> WaterVerts;

            public static Output Create() => new Output
            {
                Vertices = new NativeList<float3>(Allocator.TempJob),
                Normals = new NativeList<float3>(Allocator.TempJob),
                Colors = new NativeList<uint>(Allocator.TempJob),
                Indices = new NativeList<int>(Allocator.TempJob),
                ColliderVertices = new NativeList<float3>(Allocator.TempJob),
                ColliderIndices = new NativeList<int>(Allocator.TempJob),
                WaterIndices = new NativeList<int>(Allocator.TempJob),
                WaterVerts = new NativeHashMap<WaterVertexKey, int>(1024, Allocator.TempJob),
            };

            public ChunkMesh ToChunkMesh() => new ChunkMesh
            {
                Vertices = Vertices.AsArray().ToArray(),
                Normals = Normals.AsArray().ToArray(),
                Colors = Colors.AsArray().ToArray(),
                Indices = Indices.AsArray().ToArray(),
                ColliderVertices = ColliderVertices.AsArray().ToArray(),
                ColliderIndices = ColliderIndices.AsArray().ToArray(),
                WaterIndices = WaterIndices.AsArray().ToArray(),
            };

            public void Dispose()
            {
                if (Vertices.IsCreated) Vertices.Dispose();
                if (Normals.IsCreated) Normals.Dispose();
                if (Colors.IsCreated) Colors.Dispose();
                if (Indices.IsCreated) Indices.Dispose();
                if (ColliderVertices.IsCreated) ColliderVertices.Dispose();
                if (ColliderIndices.IsCreated) ColliderIndices.Dispose();
                if (WaterIndices.IsCreated) WaterIndices.Dispose();
                if (WaterVerts.IsCreated) WaterVerts.Dispose();
            }
        }

        public struct WaterVertexKey : System.IEquatable<WaterVertexKey>
        {
            public int3 P, N;
            public bool Equals(WaterVertexKey o) => P.Equals(o.P) && N.Equals(o.N);
            public override bool Equals(object obj) => obj is WaterVertexKey o && Equals(o);
            public override int GetHashCode() => (int)math.hash(new int4(P.x, P.y, P.z, N.x * 31 + N.y * 17 + N.z));
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
            public int WaterFullLevel;
            public float WaterFilletRadius;
            public int WaterFilletSegments;
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
                    if (m == (int)BlockMaterial.Water)
                    {
                        int above = MaterialAt(g + new int3(0, 1, 0));
                        AddWaterCell(new int3(x, y, z), g, above == (int)BlockMaterial.Empty);
                        continue;
                    }
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
                    for (int k = 0; k < 16; k++)
                        gridVert[k] = -1;
                    int v00 = Vert(0, planeBase, axis, u, v, faceNormal, color, config, gridVert);
                    int v10 = Vert(3, planeBase, axis, u, v, faceNormal, color, config, gridVert);
                    int v11 = Vert(15, planeBase, axis, u, v, faceNormal, color, config, gridVert);
                    int v01 = Vert(12, planeBase, axis, u, v, faceNormal, color, config, gridVert);
                    // Chia tứ giác theo đường chéo hợp AO (0fps "quad flipping") để không lệch sáng.
                    bool flip = Ao(v00) + Ao(v11) < Ao(v10) + Ao(v01);
                    int a = flip ? v10 : v00, b = flip ? v11 : v10, cc = flip ? v01 : v11, d = flip ? v00 : v01;
                    if (sign > 0) { AddTriangle(a, b, cc); AddTriangle(a, cc, d); }
                    else { AddTriangle(a, cc, b); AddTriangle(a, d, cc); }
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
            /// là mặt trước khi sign > 0 (cùng quy ước AddColliderFace), đảo khi sign &lt; 0.</summary>
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
                int ao = AoLevel(slot / TableOffsets, axis, faceNormal[axis] > 0 ? 1 : -1, u, v, i, j);
                int index = Out.Vertices.Length;
                Out.Vertices.Add(pos);
                Out.Normals.Add(nrm);
                Out.Colors.Add(Shade(color, AoBrightness[ao]));
                gridVert[key] = index;
                return index;
            }

            /// <summary>
            /// AO theo đỉnh (0fps): trong 2 × 2 ô phía trước mặt quanh đỉnh lưới, ô trước mặt của chính ô này rỗng; hai ô cạnh (side)
            /// và ô chéo (corner) đặc thì che. Cả hai side đặc → 0; còn lại 3 − số ô đặc.
            /// </summary>
            static int AoLevel(int config, int axis, int sign, int u, int v, int i, int j)
            {
                int3 d = 0; // ô trước mặt của chính ô, tính theo chỉ số trong 8 ô quanh đỉnh (0/1 mỗi trục)
                d[axis] = sign > 0 ? 1 : 0;
                d[u] = i < 2 ? 1 : 0;
                d[v] = j < 2 ? 1 : 0;
                int3 s1 = d, s2 = d, cr = d;
                s1[u] = 1 - s1[u];
                s2[v] = 1 - s2[v];
                cr[u] = 1 - cr[u];
                cr[v] = 1 - cr[v];
                int side1 = config >> (s1.x + 2 * s1.y + 4 * s1.z) & 1;
                int side2 = config >> (s2.x + 2 * s2.y + 4 * s2.z) & 1;
                int corner = config >> (cr.x + 2 * cr.y + 4 * cr.z) & 1;
                return side1 == 1 && side2 == 1 ? 0 : 3 - side1 - side2 - corner;
            }

            /// <summary>Độ sáng đã ghi vào đỉnh (tổng RGB) — các đỉnh cùng mặt cùng màu gốc nên so được mức AO.</summary>
            int Ao(int vertex)
            {
                uint c = Out.Colors[vertex];
                return (int)((c & 0xFF) + (c >> 8 & 0xFF) + (c >> 16 & 0xFF));
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

            /// <summary>Ô cỏ lộ mặt trên: mỗi sườn x/z lộ → một dải mép cỏ; góc lồi (hai sườn kề cùng lộ) → mảnh 1/4 vòng nối; góc lõm → mảnh phẳng phủ vết lõm bo cạnh.</summary>
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
                for (int sx = -1; sx <= 1; sx += 2)
                    for (int sz = -1; sz <= 1; sz += 2)
                        AddLipConcave(c, g, sx, sz, color);
            }

            /// <summary>Mảnh góc lõm được nâng lên khỏi mặt trên chừng này để không z-fighting với phần phẳng của mặt trên (brief 0073 QĐ 5).</summary>
            const float LipConcaveLift = 0.002f;

            /// <summary>Mảnh góc lõm chồm qua đường trục chừng này (cùng màu, cùng pháp tuyến với mặt trên dải bên cạnh → chồng lên không lộ).</summary>
            const float LipConcaveOverlap = 0.01f;

            /// <summary>
            /// Mảnh góc lõm (brief 0073): ở góc (sx, sz) của mặt trên ô cỏ g, hai ô kề theo x và theo z đặc, ô chéo rỗng → mặt trên của g
            /// bị bo lõm quanh trục góc (cạnh lồi rẽ góc lõm) mà mép cỏ không phủ: dải của hai ô kề dừng/bắt đầu ở trục, hoặc ô kề không có dải.
            /// Phủ góc phần tư R × R của g quanh trục bằng một mảnh phẳng ở mặt trên (nâng LipConcaveLift). Ở một trục góc chỉ một
            /// trong 4 ô quanh nó thoả điều kiện (hai ô kề của nó đặc, ô chéo rỗng) → mỗi góc thêm đúng một lần.
            /// </summary>
            void AddLipConcave(int3 c, int3 g, int sx, int sz, uint color)
            {
                if (!IsSolid(g + new int3(sx, 0, 0)) || !IsSolid(g + new int3(0, 0, sz)) || IsSolid(g + new int3(sx, 0, sz)))
                    return;
                // Toạ độ (u, v) = khoảng cách vào trong g theo −sx, −sz tính từ trục. Mảnh phủ [0, R]² và chồm qua hai đường trục
                // LipConcaveOverlap (u < 0 hoặc v < 0, không cả hai — ô chéo rỗng) để không lọt khe pixel với mặt trên dải mép cỏ bên cạnh.
                // Lục giác lõm ở trục → quạt từ đỉnh (R, R).
                float3 n = new float3(0, 1, 0);
                float3 axis = new float3(c.x + (sx > 0 ? 1 : 0), c.y + 1 + LipConcaveLift, c.z + (sz > 0 ? 1 : 0));
                float3 du = new float3(-sx, 0, 0), dv = new float3(0, 0, -sz);
                const float o = LipConcaveOverlap;
                int far = AddVertex(axis + R * (du + dv), n, color);
                int p0 = AddVertex(axis + R * du - o * dv, n, color);
                int p1 = AddVertex(axis - o * dv, n, color);
                int p2 = AddVertex(axis, n, color);
                int p3 = AddVertex(axis - o * du, n, color);
                int p4 = AddVertex(axis - o * du + R * dv, n, color);
                AddOriented(far, p0, p1, n);
                AddOriented(far, p1, p2, n);
                AddOriented(far, p2, p3, n);
                AddOriented(far, p3, p4, n);
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
                float3 up = new float3(0, 1, 0), down = new float3(0, -1, 0), outward = 0;
                outward[sideAxis] = sideSign;

                // Mặt cắt bo tròn (brief 0014): lưới LipProfilePoints hàng × (LipSegments + 1) cột; đỉnh (i, k) = first + i·row + k,
                // mỗi điểm một đỉnh dùng chung cho hai tứ giác kề → pháp tuyến mượt theo đường cong.
                int row = LipSegments + 1;
                int first = Out.Vertices.Length;
                for (int i = 0; i < LipProfilePoints; i++)
                    for (int k = 0; k <= LipSegments; k++)
                    {
                        float sigma = (float)k / LipSegments;
                        float depth = LipDepthAt(g[edgeAxis] + sigma, sideAxis, sideSign, plane, top);
                        LipProfile(i, depth, out float2 p, out float2 n);
                        AddVertex(LipPoint(c, sideAxis, sideSign, p.x, p.y, sigma), outward * n.x + up * n.y, color);
                    }
                // Mặt trên: quạt từ P0a qua P0b và hàng A0.
                int p0a = AddVertex(LipPoint(c, sideAxis, sideSign, -R, 0, 0), up, color);
                int p0b = AddVertex(LipPoint(c, sideAxis, sideSign, -R, 0, 1), up, color);
                AddOriented(p0a, p0b, first + LipSegments, up);
                for (int k = LipSegments; k > 0; k--)
                    AddOriented(p0a, first + k, first + k - 1, up);
                // Mặt cong: tứ giác giữa hàng i và i + 1 (A0 → … → C2 → W).
                for (int i = 0; i + 1 < LipProfilePoints; i++)
                    for (int k = 0; k < LipSegments; k++)
                    {
                        int a = first + i * row + k, b = a + 1, a2 = a + row, b2 = b + row;
                        float3 want = Out.Normals[a] + Out.Normals[b2];
                        AddOriented(a, b, b2, want);
                        AddOriented(a, b2, a2, want);
                    }
                // Đáy trong: quạt từ F0 (−R, −LipFloor) qua hàng W tới F1 — phủ phần khối lõm do bo cạnh, nhìn từ dưới không xuyên.
                int w = first + (LipProfilePoints - 1) * row;
                int f0 = AddVertex(LipPoint(c, sideAxis, sideSign, -R, -LipFloor, 0), down, color);
                int f1 = AddVertex(LipPoint(c, sideAxis, sideSign, -R, -LipFloor, 1), down, color);
                for (int k = 0; k < LipSegments; k++)
                    AddOriented(f0, w + k, w + k + 1, down);
                AddOriented(f0, w + LipSegments, f1, down);
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
                    // Đa giác mặt cắt kín: (−R, 0) → A0..A3 → C0..C2 → W → (−R, −LipFloor); quạt từ W (đỉnh lõm duy nhất).
                    int q0 = AddVertex(LipPoint(c, sideAxis, sideSign, -R, 0, sigma), capN, color);
                    for (int i = 0; i < LipProfilePoints; i++)
                    {
                        LipProfile(i, depth, out float2 p, out float2 n);
                        AddVertex(LipPoint(c, sideAxis, sideSign, p.x, p.y, sigma), capN, color); // q0 + 1 + i
                    }
                    int qi = AddVertex(LipPoint(c, sideAxis, sideSign, -R, -LipFloor, sigma), capN, color);
                    int qw = q0 + LipProfilePoints;
                    AddOriented(qw, qi, q0, capN);
                    for (int i = 0; i + 1 < LipProfilePoints; i++)
                        AddOriented(qw, q0 + i, q0 + i + 1, capN);
                }
            }

            /// <summary>Mảnh góc lồi: quét mặt cắt mép cỏ (A0..C2) 1/4 vòng quanh trục đứng qua góc ô; độ sâu đáy nội suy giữa hai đầu dải; W thu về một đỉnh trên trục.</summary>
            void AddLipCorner(int3 c, int3 g, int sx, int sz, uint color)
            {
                float3 axisLocal = new float3(c.x + (sx > 0 ? 1 : 0), c.y + 1, c.z + (sz > 0 ? 1 : 0));
                int cornerX = g.x + (sx > 0 ? 1 : 0), cornerZ = g.z + (sz > 0 ? 1 : 0), top = g.y + 1;
                // Dải sườn x chạy dọc z (w = z góc); dải sườn z chạy dọc x (w = x góc).
                float d0 = LipDepthAt(cornerZ, 0, sx, cornerX, top);
                float d1 = LipDepthAt(cornerX, 2, sz, cornerZ, top);
                float3 up = new float3(0, 1, 0), down = new float3(0, -1, 0);
                int center = AddVertex(axisLocal, up, color);
                int wCenter = AddVertex(axisLocal + new float3(0, -LipFloor, 0), down, color);
                // Lưới: hàng i = 0..LipProfilePoints − 2 (A0..C2) × cột k = 0..LipCornerSegments; đỉnh (i, k) = first + i·col + k.
                // k = 0 trùng đầu dải sườn x, k = LipCornerSegments trùng đầu dải sườn z.
                int rows = LipProfilePoints - 1, col = LipCornerSegments + 1;
                int first = Out.Vertices.Length;
                for (int i = 0; i < rows; i++)
                    for (int k = 0; k <= LipCornerSegments; k++)
                    {
                        float a = (float)k / LipCornerSegments * 0.5f * math.PI;
                        float3 dir = new float3(sx * math.cos(a), 0, sz * math.sin(a));
                        float depth = math.lerp(d0, d1, (float)k / LipCornerSegments);
                        LipProfile(i, depth, out float2 p, out float2 n);
                        AddVertex(axisLocal + dir * p.x + new float3(0, p.y, 0), dir * n.x + up * n.y, color);
                    }
                // Mặt trên: quạt từ tâm trục qua hàng A0.
                for (int k = 0; k < LipCornerSegments; k++)
                    AddOriented(center, first + k, first + k + 1, up);
                // Mặt cong giữa các hàng A0 → … → C2.
                for (int i = 0; i + 1 < rows; i++)
                    for (int k = 0; k < LipCornerSegments; k++)
                    {
                        int a = first + i * col + k, b = a + 1, a2 = a + col, b2 = b + col;
                        float3 want = Out.Normals[a] + Out.Normals[b2];
                        AddOriented(a, b, b2, want);
                        AddOriented(a, b2, a2, want);
                    }
                // Mặt dưới nghiêng C2 → W: quạt từ đỉnh W trên trục.
                int c2 = first + (rows - 1) * col;
                for (int k = 0; k < LipCornerSegments; k++)
                    AddOriented(wCenter, c2 + k, c2 + k + 1, down);
            }

            /// <summary>Ô nước (0012, 0041–0043): ô cột (đang rơi hoặc ngay trên ô đang rơi) vẽ liền + bo cạnh đứng / mép tràn; ô khác vẽ mặt trên (nếu thoáng), mặt bên, mặt đáy.</summary>
            void AddWaterCell(int3 c, int3 g, bool airAbove)
            {
                bool column = IsFallingWater(g) || IsFallingWater(g - new int3(0, 1, 0));
                if (!column)
                {
                    if (airAbove)
                        AddWaterSurface(c, g);
                    AddWaterSides(c, g);
                    return;
                }
                int e = (int)BlockMaterial.Empty;
                var col = new WaterCol
                {
                    c = c,
                    h = new float4(CornerHeight(g, 0, 0), CornerHeight(g, 1, 0), CornerHeight(g, 0, 1), CornerHeight(g, 1, 1)),
                    open = new bool4(MaterialAt(g + new int3(1, 0, 0)) == e, MaterialAt(g - new int3(1, 0, 0)) == e,
                        MaterialAt(g + new int3(0, 0, 1)) == e, MaterialAt(g - new int3(0, 0, 1)) == e),
                    lip = airAbove,
                };
                col.rv = math.min(WaterFilletRadius, math.cmin(col.h));
                AddWaterColumn(g, col);
            }

            bool IsFallingWater(int3 p)
            {
                ushort cell = CellAt(p);
                return Cell.GetMaterial(cell) == BlockMaterial.Water && Cell.IsFlowing(cell) && Cell.IsFalling(cell);
            }

            /// <summary>Ô cột có ô trên thoáng (mép tràn)?</summary>
            bool IsLipCell(int3 p) =>
                MaterialAt(p) == (int)BlockMaterial.Water && MaterialAt(p + new int3(0, 1, 0)) == (int)BlockMaterial.Empty
                && (IsFallingWater(p) || IsFallingWater(p - new int3(0, 1, 0)));

            struct WaterCol
            {
                public int3 c;
                /// <summary>CornerHeight theo (sx + 2·sz).</summary>
                public float4 h;
                /// <summary>Mặt hở: +x, −x, +z, −z.</summary>
                public bool4 open;
                public bool lip;
                public float rv;

                public bool Rounded(int sx, int sz) => open[sx == 1 ? 0 : 1] && open[sz == 1 ? 2 : 3];
                public float Hc(int sx, int sz) => h[sx + 2 * sz];
                public float H(float u, float v) => (1 - u) * (1 - v) * h.x + u * (1 - v) * h.y + (1 - u) * v * h.z + u * v * h.w;
                public float3 Pos(float u, float y, float v) => new float3(c.x + u, y, c.z + v);
            }

            float3 CircleDir(int sx, int sz, float phi) =>
                math.cos(phi) * new float3(sx == 1 ? 1 : -1, 0, 0) + math.sin(phi) * new float3(0, 0, sz == 1 ? 1 : -1);

            /// <summary>Hướng d: 0 = +x, 1 = −x, 2 = +z, 3 = −z → (trục x?, dấu).</summary>
            static float3 DirVec(int d) => d < 2 ? new float3(d == 0 ? 1 : -1, 0, 0) : new float3(0, 0, d == 2 ? 1 : -1);

            /// <summary>Điểm cung mép tràn tại điểm trong P (u, v) của mặt trên, cung k.</summary>
            int WaterArcVertex(in WaterCol col, float2 puv, float3 outDir, int k, bool withOutNormal, float3 forcedNormal)
            {
                float R = WaterFilletRadius;
                float th = k * math.PI * 0.5f / WaterFilletSegments;
                float sn = math.sin(th), cs = math.cos(th);
                float3 up = new float3(0, 1, 0);
                float3 p = col.Pos(puv.x, col.c.y + col.H(puv.x, puv.y), puv.y) + outDir * (R * sn) - up * (col.rv * (1 - cs));
                float3 n = withOutNormal ? math.normalize(outDir * (col.rv * sn) + up * (R * cs)) : forcedNormal;
                return AddWaterVertex(p, n);
            }

            /// <summary>Ô cột (brief 0080, 0043 mục 6): mặt bên rút ngắn + trụ bo đứng; mép tràn thêm mặt trên, cung, cầu góc, nắp.</summary>
            void AddWaterColumn(int3 g, in WaterCol col)
            {
                float R = WaterFilletRadius;
                int S = WaterFilletSegments;
                float3 up = new float3(0, 1, 0);
                int3 c = col.c;

                if (col.lip)
                {
                    float u0 = col.open.y ? R : 0, u1 = col.open.x ? 1 - R : 1;
                    float v0 = col.open.w ? R : 0, v1 = col.open.z ? 1 - R : 1;
                    int a = AddWaterVertex(col.Pos(u0, c.y + col.H(u0, v0), v0), up);
                    int b = AddWaterVertex(col.Pos(u1, c.y + col.H(u1, v0), v0), up);
                    int cc = AddWaterVertex(col.Pos(u1, c.y + col.H(u1, v1), v1), up);
                    int dd = AddWaterVertex(col.Pos(u0, c.y + col.H(u0, v1), v1), up);
                    AddWaterTri(a, b, cc); AddWaterTri(a, cc, dd);
                }

                // Mặt bên, cung và nắp trên từng cạnh hở.
                for (int d = 0; d < 4; d++)
                {
                    if (!col.open[d])
                        continue;
                    bool axisX = d < 2;
                    int cs = d == 0 || d == 2 ? 1 : 0;
                    float3 outDir = DirVec(d);
                    bool r0 = axisX ? col.Rounded(cs, 0) : col.Rounded(0, cs);
                    bool r1 = axisX ? col.Rounded(cs, 1) : col.Rounded(1, cs);
                    float t0 = r0 ? R : 0, t1 = r1 ? 1 - R : 1;
                    float pe = cs == 1 ? 1 - R : R; // toạ độ mép mặt trên phía d
                    float2 P0 = axisX ? new float2(pe, t0) : new float2(t0, pe);
                    float2 P1 = axisX ? new float2(pe, t1) : new float2(t1, pe);
                    float2 E0 = axisX ? new float2(cs, t0) : new float2(t0, cs); // điểm trên mặt phẳng cạnh
                    float2 E1 = axisX ? new float2(cs, t1) : new float2(t1, cs);

                    int b0 = AddWaterVertex(col.Pos(E0.x, c.y, E0.y), outDir);
                    int b1 = AddWaterVertex(col.Pos(E1.x, c.y, E1.y), outDir);
                    int s0, s1;
                    if (col.lip)
                    {
                        s0 = WaterArcVertex(col, P0, outDir, S, true, default);
                        s1 = WaterArcVertex(col, P1, outDir, S, true, default);
                        for (int k = 0; k < S; k++)
                        {
                            int p0 = WaterArcVertex(col, P0, outDir, k, true, default), p1 = WaterArcVertex(col, P1, outDir, k, true, default);
                            int q0 = WaterArcVertex(col, P0, outDir, k + 1, true, default), q1 = WaterArcVertex(col, P1, outDir, k + 1, true, default);
                            AddWaterTri(p0, p1, q1); AddWaterTri(p0, q1, q0);
                        }
                    }
                    else
                    {
                        float hA = axisX ? col.Hc(cs, 0) : col.Hc(0, cs), hB = axisX ? col.Hc(cs, 1) : col.Hc(1, cs);
                        s0 = AddWaterVertex(col.Pos(E0.x, c.y + math.lerp(hA, hB, t0), E0.y), outDir);
                        s1 = AddWaterVertex(col.Pos(E1.x, c.y + math.lerp(hA, hB, t1), E1.y), outDir);
                    }
                    AddWaterTri(b0, b1, s1); AddWaterTri(b0, s1, s0);

                    if (!col.lip)
                        continue;
                    for (int end = 0; end < 2; end++)
                    {
                        if (end == 0 ? r0 : r1)
                            continue;
                        float3 eDir = axisX ? new float3(0, 0, end == 0 ? -1 : 1) : new float3(end == 0 ? -1 : 1, 0, 0);
                        int3 nb = g + new int3((int)eDir.x, 0, (int)eDir.z);
                        if (MaterialAt(nb) != (int)BlockMaterial.Water)
                            continue;
                        if (IsLipCell(nb) && MaterialAt(nb + new int3((int)outDir.x, 0, (int)outDir.z)) == (int)BlockMaterial.Empty)
                            continue;
                        float2 corner = end == 0 ? E0 : E1;
                        float2 pp = end == 0 ? P0 : P1;
                        float3 capN = -eDir;
                        int csx = (int)corner.x, csz = (int)corner.y;
                        int q = AddWaterVertex(col.Pos(corner.x, c.y + col.Hc(csx, csz), corner.y), capN);
                        for (int k = 0; k < S; k++)
                            AddWaterTri(q, WaterArcVertex(col, pp, outDir, k, false, capN), WaterArcVertex(col, pp, outDir, k + 1, false, capN));
                    }
                }

                // Trụ bo đứng và cầu góc ở mỗi góc bo.
                for (int sz = 0; sz < 2; sz++)
                for (int sx = 0; sx < 2; sx++)
                {
                    if (!col.Rounded(sx, sz))
                        continue;
                    float2 ax = new float2(sx == 1 ? 1 - R : R, sz == 1 ? 1 - R : R);
                    float hc = c.y + col.H(ax.x, ax.y);
                    float yt = col.lip ? hc - col.rv : c.y + col.Hc(sx, sz);
                    for (int j = 0; j < S; j++)
                    {
                        float3 d0 = CircleDir(sx, sz, j * math.PI * 0.5f / S), d1 = CircleDir(sx, sz, (j + 1) * math.PI * 0.5f / S);
                        int a0 = AddWaterVertex(col.Pos(ax.x + R * d0.x, c.y, ax.y + R * d0.z), d0);
                        int a1 = AddWaterVertex(col.Pos(ax.x + R * d1.x, c.y, ax.y + R * d1.z), d1);
                        int t1v = AddWaterVertex(col.Pos(ax.x + R * d1.x, yt, ax.y + R * d1.z), d1);
                        int t0v = AddWaterVertex(col.Pos(ax.x + R * d0.x, yt, ax.y + R * d0.z), d0);
                        AddWaterTri(a0, a1, t1v); AddWaterTri(a0, t1v, t0v);
                    }
                    if (!col.lip)
                        continue;
                    for (int i = 0; i < S; i++)
                    for (int j = 0; j < S; j++)
                    {
                        int v00 = WaterSphereVertex(col, sx, sz, ax, hc, i, j), v10 = WaterSphereVertex(col, sx, sz, ax, hc, i + 1, j);
                        int v11 = WaterSphereVertex(col, sx, sz, ax, hc, i + 1, j + 1), v01 = WaterSphereVertex(col, sx, sz, ax, hc, i, j + 1);
                        AddWaterTri(v00, v10, v11); AddWaterTri(v00, v11, v01);
                    }
                }
            }

            int WaterSphereVertex(in WaterCol col, int sx, int sz, float2 ax, float hc, int i, int j)
            {
                float R = WaterFilletRadius;
                float th = i * math.PI * 0.5f / WaterFilletSegments;
                float3 dir = CircleDir(sx, sz, j * math.PI * 0.5f / WaterFilletSegments);
                float3 up = new float3(0, 1, 0);
                float sn = math.sin(th), cs = math.cos(th);
                float3 p = new float3(col.c.x + ax.x, hc, col.c.z + ax.y) + dir * (R * sn) - up * (col.rv * (1 - cs));
                return AddWaterVertex(p, math.normalize(dir * (col.rv * sn) + up * (R * cs)));
            }

            /// <summary>Mặt nước của ô nước có không khí phía trên (0012, 0042): tứ giác có độ cao từng góc theo CornerHeight, submesh 1.</summary>
            void AddWaterSurface(int3 c, int3 g)
            {
                float3 up = new float3(0, 1, 0);
                AddWaterQuad(new float3(c.x, c.y + CornerHeight(g, 0, 0), c.z), new float3(c.x + 1, c.y + CornerHeight(g, 1, 0), c.z),
                    new float3(c.x + 1, c.y + CornerHeight(g, 1, 1), c.z + 1), new float3(c.x, c.y + CornerHeight(g, 0, 1), c.z + 1), up);
            }

            /// <summary>Mặt bên (ô ngang Empty) và mặt đáy (ô dưới Empty) của ô nước (0041, 0042): đỉnh trên theo độ cao hai góc của cạnh.</summary>
            void AddWaterSides(int3 c, int3 g)
            {
                for (int axis = 0; axis < 3; axis += 2)
                for (int sign = -1; sign <= 1; sign += 2)
                {
                    int3 d = axis == 0 ? new int3(sign, 0, 0) : new int3(0, 0, sign);
                    if (MaterialAt(g + d) != (int)BlockMaterial.Empty)
                        continue;
                    float3 n = new float3(d.x, 0, d.z);
                    float plane = (axis == 0 ? c.x : c.z) + (sign > 0 ? 1 : 0);
                    int s1 = sign > 0 ? 1 : 0;
                    if (axis == 0)
                        AddWaterQuad(new float3(plane, c.y, c.z), new float3(plane, c.y, c.z + 1),
                            new float3(plane, c.y + CornerHeight(g, s1, 1), c.z + 1), new float3(plane, c.y + CornerHeight(g, s1, 0), c.z), n);
                    else
                        AddWaterQuad(new float3(c.x, c.y, plane), new float3(c.x + 1, c.y, plane),
                            new float3(c.x + 1, c.y + CornerHeight(g, 1, s1), plane), new float3(c.x, c.y + CornerHeight(g, 0, s1), plane), n);
                }
                if (MaterialAt(g + new int3(0, -1, 0)) == (int)BlockMaterial.Empty)
                    AddWaterQuad(new float3(c.x, c.y, c.z), new float3(c.x + 1, c.y, c.z),
                        new float3(c.x + 1, c.y, c.z + 1), new float3(c.x, c.y, c.z + 1), new float3(0, -1, 0));
            }

            /// <summary>Ô nguyên như MaterialAt: y &lt; 0 → Bedrock, y ≥ SizeY → 0, x/z ngoài map kẹp về cột biên.</summary>
            ushort CellAt(int3 p)
            {
                if (p.y < 0)
                    return (ushort)BlockMaterial.Bedrock;
                if (p.y >= SizeY)
                    return 0;
                int x = math.clamp(p.x, 0, SizeX - 1);
                int z = math.clamp(p.z, 0, SizeZ - 1);
                return Cells[x + SizeX * (z + SizeZ * p.y)];
            }

            /// <summary>Độ cao mặt nước của ô (0042 QĐ 2, 0043 QĐ 5), tính từ đáy ô; ô chảy không rơi cao (mức + 1)/(WaterFullLevel + 1) mặt trên (mức 0 vẫn có mặt); ô không phải nước → −1.</summary>
            float WaterHeight(int3 p)
            {
                ushort cell = CellAt(p);
                if (Cell.GetMaterial(cell) != BlockMaterial.Water)
                    return -1f;
                if (Cell.GetMaterial(CellAt(p + new int3(0, 1, 0))) == BlockMaterial.Water)
                    return 1f;
                if (!Cell.IsFlowing(cell) || Cell.IsFalling(cell))
                    return 1f - WaterSurfaceDrop;
                return (1f - WaterSurfaceDrop) * (math.min(Cell.GetWaterLevel(cell), WaterFullLevel) + 1f) / (WaterFullLevel + 1f);
            }

            /// <summary>Độ cao góc lưới (g.x + dx, g.z + dz) của ô g (0042 QĐ 3): có ô cao 1 → 1, ngược lại trung bình các ô nước quanh góc.</summary>
            float CornerHeight(int3 g, int dx, int dz)
            {
                float sum = 0;
                int n = 0;
                for (int j = 0; j < 2; j++)
                for (int i = 0; i < 2; i++)
                {
                    float h = WaterHeight(new int3(g.x + dx - 1 + i, g.y, g.z + dz - 1 + j));
                    if (h < 0)
                        continue;
                    if (h >= 1f)
                        return 1f;
                    sum += h;
                    n++;
                }
                return sum / n;
            }

            /// <summary>Tứ giác nước (0041): 4 đỉnh theo vòng p0→p1→p2→p3 (hàn đỉnh trùng), 2 tam giác vào submesh nước, mặt trước quay theo n.</summary>
            void AddWaterQuad(float3 p0, float3 p1, float3 p2, float3 p3, float3 n)
            {
                int i0 = AddWaterVertex(p0, n), i1 = AddWaterVertex(p1, n), i2 = AddWaterVertex(p2, n), i3 = AddWaterVertex(p3, n);
                AddWaterTri(i0, i1, i2);
                AddWaterTri(i0, i2, i3);
            }

            /// <summary>Tam giác nước: mặt trước quay theo tổng pháp tuyến 3 đỉnh; bỏ tam giác suy biến (trùng chỉ số).</summary>
            void AddWaterTri(int a, int b, int c)
            {
                if (a == b || b == c || a == c)
                    return;
                float3 pa = Out.Vertices[a], pb = Out.Vertices[b], pc = Out.Vertices[c];
                float3 n = Out.Normals[a] + Out.Normals[b] + Out.Normals[c];
                if (math.dot(math.cross(pb - pa, pc - pa), n) < 0f)
                {
                    int t = b; b = c; c = t;
                }
                Out.WaterIndices.Add(a); Out.WaterIndices.Add(b); Out.WaterIndices.Add(c);
            }

            /// <summary>Đỉnh nước dùng chung: cùng vị trí (1/1024) và pháp tuyến (1/1024) → cùng chỉ số (brief 0080).</summary>
            int AddWaterVertex(float3 p, float3 n)
            {
                var key = new WaterVertexKey { P = (int3)math.round(p * 1024f), N = (int3)math.round(n * 1024f) };
                if (Out.WaterVerts.TryGetValue(key, out int found))
                    return found;
                int i = AddVertex(p, n, 0);
                Out.WaterVerts.Add(key, i);
                return i;
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
