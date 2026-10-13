using Game.Core;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace Game.World
{
    /// <summary>
    /// Sinh địa hình M1 theo seed (design/04-tech/world-tech.md bước 1). Mọi phép số thực nằm trong job Burst
    /// FloatMode.Deterministic để cùng seed cho cùng world trên mọi máy 64-bit (architecture.md "Nền tảng code M1").
    /// </summary>
    public static class TerrainGenerator
    {
        public static WorldData Generate(TerrainParams p) => Generate(p, out _);

        /// <param name="ranWithBurst">false nếu job chạy bằng C# thường (Burst tắt) — khi đó không bảo đảm tất định giữa máy.</param>
        public static WorldData Generate(TerrainParams p, out bool ranWithBurst)
        {
            p.Validate();
            var size = p.Size;
            var cells = new NativeArray<ushort>(size.CellCount, Allocator.TempJob);
            var managedFlag = new NativeArray<int>(1, Allocator.TempJob);
            try
            {
                var job = new TerrainJob
                {
                    Cells = cells,
                    ManagedFlag = managedFlag,
                    SizeX = size.X,
                    SizeY = size.Y,
                    SizeZ = size.Z,
                    Seed = p.seed == 0 ? 1u : p.seed,
                    BaseHeight = p.baseHeight,
                    Amplitude1 = p.amplitude1,
                    Frequency1 = p.frequency1,
                    Amplitude2 = p.amplitude2,
                    Frequency2 = p.frequency2,
                    MinSurface = p.minSurface,
                    MaxSurface = p.maxSurface,
                    BedrockLayers = p.bedrockLayers,
                    DirtDepth = p.dirtDepth,
                    WaterLevel = p.waterLevel,
                    SandBand = p.sandBand,
                    LakeRadius = p.lakeRadius,
                    LakeDepth = p.lakeDepth,
                };
                job.Schedule(size.X * size.Z, 64).Complete();
                ranWithBurst = managedFlag[0] == 0;
                return new WorldData(size, cells.ToArray());
            }
            finally
            {
                cells.Dispose();
                managedFlag.Dispose();
            }
        }

        [BurstCompile(FloatMode = FloatMode.Deterministic, CompileSynchronously = true)]
        struct TerrainJob : IJobParallelFor
        {
            // Mỗi job ghi cả cột (nhiều chỉ số khác chỉ số job) — các cột không chồng nhau.
            [NativeDisableParallelForRestriction] public NativeArray<ushort> Cells;
            [NativeDisableParallelForRestriction] public NativeArray<int> ManagedFlag;

            public int SizeX, SizeY, SizeZ;
            public uint Seed;
            public float BaseHeight, Amplitude1, Frequency1, Amplitude2, Frequency2;
            public int MinSurface, MaxSurface, BedrockLayers, DirtDepth, WaterLevel, SandBand;
            public float LakeRadius, LakeDepth;

            public void Execute(int column)
            {
                MarkIfManaged(ManagedFlag);

                int x = column % SizeX;
                int z = column / SizeX;

                // Offset noise + tâm hồ suy từ seed (Random của Unity.Mathematics là xorshift số nguyên).
                var rng = new Random(Seed);
                float2 offset1 = rng.NextFloat2(-1000f, 1000f);
                float2 offset2 = rng.NextFloat2(-1000f, 1000f);
                float margin = LakeRadius + 4f;
                float2 lakeCenter = rng.NextFloat2(new float2(margin), new float2(SizeX - margin, SizeZ - margin));

                float2 pos = new float2(x, z);
                float h = BaseHeight
                          + Amplitude1 * noise.snoise(pos * Frequency1 + offset1)
                          + Amplitude2 * noise.snoise(pos * Frequency2 + offset2);
                float d = math.distance(pos, lakeCenter) / LakeRadius;
                if (d < 1f)
                    h -= LakeDepth * (1f - d * d);

                int surface = math.clamp((int)math.floor(h), MinSurface, MaxSurface);
                // Hồ có thể kéo mặt đất xuống dưới MinSurface — vẫn giữ trên lớp đá đáy.
                if (d < 1f)
                    surface = math.max((int)math.floor(h), BedrockLayers);

                bool sandTop = surface <= WaterLevel + SandBand;

                for (int y = 0; y < SizeY; y++)
                {
                    ushort cell;
                    if (y < BedrockLayers)
                        cell = (ushort)BlockMaterial.Bedrock;
                    else if (y <= surface)
                    {
                        int depth = surface - y;
                        if (depth == 0)
                            cell = (ushort)(sandTop ? BlockMaterial.Sand : BlockMaterial.Grass);
                        else if (depth <= DirtDepth)
                            cell = (ushort)BlockMaterial.Dirt;
                        else
                            cell = (ushort)BlockMaterial.Stone;
                    }
                    else if (y <= WaterLevel)
                        cell = (ushort)((ushort)BlockMaterial.Water | (Cell.MaxWaterLevel << Cell.WaterShift));
                    else
                        cell = (ushort)BlockMaterial.Empty;

                    Cells[x + SizeX * (z + SizeZ * y)] = cell;
                }
            }

            [BurstDiscard]
            static void MarkIfManaged(NativeArray<int> flag)
            {
                flag[0] = 1;
            }
        }
    }
}
