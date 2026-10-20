using Game.Core;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace Game.World
{
    /// <summary>
    /// Sinh world nhiều map (generatorVersion 3, world-tech bước 7a, decision 0022): khối đất liền giữa world chia 3 map bằng
    /// ranh giới Voronoi méo noise; biển bao quanh. Mọi phép số thực nằm trong job Burst FloatMode.Deterministic.
    /// Sau job gộp mảnh rời của bảng map (MapTable.MergeFragments).
    /// </summary>
    public static class WorldMapGenerator
    {
        public static WorldData Generate(WorldMapParams p, out MapTable maps) => Generate(p, out maps, out _);

        /// <param name="ranWithBurst">false nếu job chạy bằng C# thường (Burst tắt) — khi đó không bảo đảm tất định giữa máy.</param>
        public static WorldData Generate(WorldMapParams p, out MapTable maps, out bool ranWithBurst)
        {
            p.Validate();
            var size = p.Size;
            var cells = new NativeArray<ushort>(size.CellCount, Allocator.TempJob);
            var mapIds = new NativeArray<byte>(p.sizeX * p.sizeZ, Allocator.TempJob);
            var managedFlag = new NativeArray<int>(1, Allocator.TempJob);
            try
            {
                var job = new WorldMapJob
                {
                    Cells = cells,
                    MapIds = mapIds,
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
                    SeaRing = p.seaRing,
                    SeaFloor = p.waterLevel - p.seaFloorDepth,
                    LandRadius = p.landRadius,
                    CoastFalloff = p.coastFalloff,
                    CoastWarpAmplitude = p.coastWarpAmplitude,
                    CoastWarpFrequency = p.coastWarpFrequency,
                    MapCount = p.mapCount,
                    MapCenterRadius = p.mapCenterRadius,
                    BorderWarpAmplitude = p.borderWarpAmplitude,
                    BorderWarpFrequency = p.borderWarpFrequency,
                };
                job.Schedule(size.X * size.Z, 64).Complete();
                ranWithBurst = managedFlag[0] == 0;
                var ids = mapIds.ToArray();
                MapTable.MergeFragments(ids, p.sizeX, p.sizeZ, p.mapCount);
                maps = new MapTable(p.sizeX, p.sizeZ, ids, (string[])p.mapNames.Clone());
                return new WorldData(size, cells.ToArray());
            }
            finally
            {
                cells.Dispose();
                mapIds.Dispose();
                managedFlag.Dispose();
            }
        }

        [BurstCompile(FloatMode = FloatMode.Deterministic, CompileSynchronously = true)]
        struct WorldMapJob : IJobParallelFor
        {
            // Mỗi job ghi cả cột (nhiều chỉ số khác chỉ số job) — các cột không chồng nhau.
            [NativeDisableParallelForRestriction] public NativeArray<ushort> Cells;
            [NativeDisableParallelForRestriction] public NativeArray<byte> MapIds;
            [NativeDisableParallelForRestriction] public NativeArray<int> ManagedFlag;

            public int SizeX, SizeY, SizeZ;
            public uint Seed;
            public float BaseHeight, Amplitude1, Frequency1, Amplitude2, Frequency2;
            public int MinSurface, MaxSurface, BedrockLayers, DirtDepth, WaterLevel, SandBand;
            public float LakeRadius, LakeDepth;
            public int SeaRing, SeaFloor;
            public float LandRadius, CoastFalloff, CoastWarpAmplitude, CoastWarpFrequency;
            public int MapCount;
            public float MapCenterRadius, BorderWarpAmplitude, BorderWarpFrequency;

            public void Execute(int column)
            {
                MarkIfManaged(ManagedFlag);

                int x = column % SizeX;
                int z = column / SizeX;
                float2 pos = new float2(x, z);

                // Thứ tự rút số ngẫu nhiên cố định (đổi thứ tự = world khác).
                var rng = new Random(Seed);
                float2 offset1 = rng.NextFloat2(-1000f, 1000f);
                float2 offset2 = rng.NextFloat2(-1000f, 1000f);
                float2 offsetCoast = rng.NextFloat2(-1000f, 1000f);
                float2 offsetWarpX1 = rng.NextFloat2(-1000f, 1000f);
                float2 offsetWarpZ1 = rng.NextFloat2(-1000f, 1000f);
                float2 offsetWarpX2 = rng.NextFloat2(-1000f, 1000f);
                float2 offsetWarpZ2 = rng.NextFloat2(-1000f, 1000f);
                float angle0 = rng.NextFloat(0f, math.TAU);

                float2 worldCenter = new float2(SizeX, SizeZ) * 0.5f;

                float h = BaseHeight
                          + Amplitude1 * noise.snoise(pos * Frequency1 + offset1)
                          + Amplitude2 * noise.snoise(pos * Frequency2 + offset2);

                // Tâm map i và hồ ở tâm mỗi map.
                float minDistLake = float.MaxValue;
                float step = math.TAU / MapCount;
                for (int i = 0; i < MapCount; i++)
                {
                    math.sincos(angle0 + i * step, out float s, out float c);
                    float2 center = worldCenter + MapCenterRadius * new float2(c, s);
                    minDistLake = math.min(minDistLake, math.distance(pos, center));
                }
                float dLake = minDistLake / LakeRadius;
                bool inLake = dLake < 1f;
                if (inLake)
                    h -= LakeDepth * (1f - dLake * dLake);

                // Đất liền.
                float shore = LandRadius + CoastWarpAmplitude * noise.snoise(pos * CoastWarpFrequency + offsetCoast)
                              - math.distance(pos, worldCenter);
                float island = math.smoothstep(0f, CoastFalloff, shore);
                int edge = math.min(math.min(x, z), math.min(SizeX - 1 - x, SizeZ - 1 - z));
                if (edge < SeaRing)
                    island = 0f;
                h = math.lerp(SeaFloor, h, island);

                // Bảng map.
                if (island <= 0f)
                    MapIds[column] = 255;
                else
                {
                    float f = BorderWarpFrequency;
                    float a = BorderWarpAmplitude;
                    float2 q = pos
                               + a * new float2(noise.snoise(pos * f + offsetWarpX1), noise.snoise(pos * f + offsetWarpZ1))
                               + (0.25f * a) * new float2(noise.snoise(pos * (2f * f) + offsetWarpX2), noise.snoise(pos * (2f * f) + offsetWarpZ2));
                    int best = 0;
                    float bestD = float.MaxValue;
                    for (int i = 0; i < MapCount; i++)
                    {
                        math.sincos(angle0 + i * step, out float s, out float c);
                        float2 center = worldCenter + MapCenterRadius * new float2(c, s);
                        float d = math.distancesq(q, center);
                        if (d < bestD)
                        {
                            bestD = d;
                            best = i;
                        }
                    }
                    MapIds[column] = (byte)best;
                }

                int surface = math.clamp((int)math.floor(h), MinSurface, MaxSurface);
                if (inLake || island < 1f)
                    surface = math.min(math.max((int)math.floor(h), BedrockLayers), MaxSurface);

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
