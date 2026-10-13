using System.Collections.Generic;
using Game.Core;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace Game.World
{
    /// <summary>
    /// Mesh khối (decision 0011, world-tech "Dựng mesh khối bo cạnh", bước 5.1): mỗi mặt ô đặc giáp ô rỗng/nước → một tứ giác
    /// 4 đỉnh riêng (pháp tuyến phẳng), màu đỉnh theo vật liệu; sườn và đáy ô cỏ mang màu đất (mép cỏ là bước 5.3).
    /// Chưa bo cạnh (bước 5.2). Đọc 1 ô đệm quanh chunk để biết mặt nào lộ.
    /// </summary>
    public static class BlockMesher
    {
        /// <summary>
        /// Số ô gameplay quanh chunk mà một chunk đọc: mặt lộ chỉ phụ thuộc 6 ô kề → đệm 1 ô mỗi phía.
        /// Sửa ô cách biên ≤ CellMargin → chunk bên cạnh cũng phải dựng lại (WorldEditor.AffectedChunks).
        /// </summary>
        public const int CellMargin = 1;

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
            var size = world.Size;
            int count = chunkIndices.Count;
            var cells = new NativeArray<ushort>(world.Cells, Allocator.TempJob);
            var palette = new NativeArray<uint>(2 * 256, Allocator.TempJob);
            for (int m = 0; m < 256; m++)
            {
                palette[m] = TopColor((BlockMaterial)m);
                palette[256 + m] = SideColor((BlockMaterial)m);
            }
            var lists = new (NativeList<float3> v, NativeList<float3> n, NativeList<uint> col, NativeList<int> i)[count];
            var handles = new NativeArray<JobHandle>(count, Allocator.Temp);
            try
            {
                for (int k = 0; k < count; k++)
                {
                    int c = chunkIndices[k];
                    int cx = c % size.ChunksX, cz = c / size.ChunksX % size.ChunksZ, cy = c / (size.ChunksX * size.ChunksZ);
                    lists[k] = (new NativeList<float3>(Allocator.TempJob), new NativeList<float3>(Allocator.TempJob),
                        new NativeList<uint>(Allocator.TempJob), new NativeList<int>(Allocator.TempJob));
                    handles[k] = new MeshJob
                    {
                        Cells = cells,
                        Palette = palette,
                        SizeX = size.X, SizeY = size.Y, SizeZ = size.Z,
                        Origin = new int3(cx, cy, cz) * WorldSize.ChunkSize,
                        Vertices = lists[k].v, Normals = lists[k].n, Colors = lists[k].col, Indices = lists[k].i,
                    }.Schedule();
                }
                JobHandle.CompleteAll(handles);
                var result = new ChunkMesh[count];
                for (int k = 0; k < count; k++)
                {
                    result[k] = new ChunkMesh
                    {
                        Vertices = lists[k].v.AsArray().ToArray(),
                        Normals = lists[k].n.AsArray().ToArray(),
                        Colors = lists[k].col.AsArray().ToArray(),
                        Indices = lists[k].i.AsArray().ToArray(),
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
                    if (l.col.IsCreated) l.col.Dispose();
                    if (l.i.IsCreated) l.i.Dispose();
                }
                palette.Dispose();
                cells.Dispose();
            }
        }

        [BurstCompile(CompileSynchronously = true)]
        struct MeshJob : IJob
        {
            [ReadOnly] public NativeArray<ushort> Cells;
            /// <summary>[0..255] màu mặt trên, [256..511] màu sườn/đáy theo vật liệu.</summary>
            [ReadOnly] public NativeArray<uint> Palette;
            public int SizeX, SizeY, SizeZ;
            public int3 Origin;

            public NativeList<float3> Vertices, Normals;
            public NativeList<uint> Colors;
            public NativeList<int> Indices;

            public void Execute()
            {
                const int n = WorldSize.ChunkSize;
                for (int y = 0; y < n; y++)
                for (int z = 0; z < n; z++)
                for (int x = 0; x < n; x++)
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
                        AddFace(new int3(x, y, z), axis, sign, color);
                    }
                }
            }

            /// <summary>Tứ giác trên mặt <paramref name="axis"/> phía <paramref name="sign"/> của ô cục bộ <paramref name="c"/>.
            /// Thứ tự đỉnh: cross(b − a, c − a) cùng hướng pháp tuyến (quy ước mặt trước của mesher cũ, test TrianglesFaceTheirNormals).</summary>
            void AddFace(int3 c, int axis, int sign, uint color)
            {
                int u = (axis + 1) % 3, v = (axis + 2) % 3;
                float3 p00 = c;
                if (sign > 0)
                    p00[axis] += 1;
                float3 du = 0, dv = 0;
                du[u] = 1;
                dv[v] = 1;
                float3 normal = 0;
                normal[axis] = sign;

                int i0 = Vertices.Length;
                Vertices.Add(p00);
                Vertices.Add(p00 + du);
                Vertices.Add(p00 + du + dv);
                Vertices.Add(p00 + dv);
                for (int k = 0; k < 4; k++)
                {
                    Normals.Add(normal);
                    Colors.Add(color);
                }
                // cross(du, dv) = +trục → mặt dương giữ thứ tự (0,1,2)(0,2,3), mặt âm đảo.
                if (sign > 0)
                {
                    Indices.Add(i0); Indices.Add(i0 + 1); Indices.Add(i0 + 2);
                    Indices.Add(i0); Indices.Add(i0 + 2); Indices.Add(i0 + 3);
                }
                else
                {
                    Indices.Add(i0); Indices.Add(i0 + 2); Indices.Add(i0 + 1);
                    Indices.Add(i0); Indices.Add(i0 + 3); Indices.Add(i0 + 2);
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
