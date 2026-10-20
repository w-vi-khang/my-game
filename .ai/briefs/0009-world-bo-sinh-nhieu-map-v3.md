# Brief 0009 — World bước 7a: bộ sinh world nhiều map (generatorVersion 3)

> Trạng thái: chờ review
> Review: đầy đủ + user xem
> Model thực thi: Sonnet · medium
> Cập nhật: 2026-10-20 13:10 (Planner trả lời Vướng mắc: thêm gộp mảnh rời — QĐ 10, bước 4b)

<!-- Planner (Opus) viết, Executor (Sonnet) làm theo ở PHIÊN KHÁC — executor không có lịch sử chat, không được tự quyết.
Trạng thái: chờ làm → đang làm → chờ review → đã duyệt; hoặc → cần Opus (executor dừng, hỏi ở mục Vướng mắc).
Review: nhẹ (đọc mục Kết quả) | đầy đủ (đọc cả diff) | user xem (có phần user tự kiểm bằng mắt/chơi thử) — có thể ghép "đầy đủ + user xem".
Model thực thi: BẮT BUỘC, đúng dạng `<Model> · <effort>` — Model: Opus | Sonnet | Haiku | Fable; effort: low | medium | high | xhigh | max (chọn theo AGENTS.md §10.2).
Executor kiểm tra model + effort của phiên TRƯỚC khi làm (scripts/brief_check.py); lệch hoặc không xác định được → cảnh báo user và DỪNG.
File được commit. Tự kiểm trước khi giao (AGENTS.md §10.3): executor có phải hỏi gì không? còn chỗ nào bỏ ngỏ 2 phương án? -->

## Mục tiêu
Bước 7a của [task world bước 7](../tasks/m1-world-buoc-7-nhieu-map-minimap.md) (quyết định [0022](../../design/decisions/0022-world-nhieu-map-va-minimap-m1.md)). Viết **bộ sinh world nhiều map** mới (generatorVersion 3): world 384 × 48 × 384 gồm một khối đất liền ở giữa, chia thành 3 map bằng ranh giới cong, biển bao quanh. Bộ sinh trả về dữ liệu world và **bảng map** (mỗi cột thuộc map nào). Bộ sinh phải tất định, có mã băm chuẩn, và chạy được trong scene HashCheck trên điện thoại. Kèm một menu Editor xuất ảnh PNG xem trước để user duyệt hình dạng.

Bước này **chỉ** thêm bộ sinh. Các scene đang có (TerrainView, SimView, Bench) vẫn dùng world v2 64 × 32 × 64. Bước 7b/7e mới chuyển sang v3.

## Bối cảnh cần đọc
- Bộ sinh v1/v2 (mẫu để chép cách làm; **không sửa** file này): `client/Assets/Game/World/TerrainGenerator.cs`
  - `Generate :18-61`: cấp `NativeArray<ushort>` `Allocator.TempJob`, schedule `job.Schedule(size.X * size.Z, 64).Complete()`, cờ `ManagedFlag` để biết job có chạy bằng Burst không, `new WorldData(size, cells.ToArray())`, dispose trong `finally`.
  - `[BurstCompile(FloatMode = FloatMode.Deterministic, CompileSynchronously = true)] struct TerrainJob : IJobParallelFor :63-150`. Rng: `var rng = new Random(Seed);` rồi `rng.NextFloat2(-1000f, 1000f)` `:86-91`. Noise: `noise.snoise(pos * Frequency1 + offset1)` `:95`. Hồ `:97-99`. Đảo v2 `:101-108` (`math.smoothstep`, `math.lerp(SeaFloor, h, island)`). Chọn mặt đất `:110-117`. Ghi lớp ô `:119-142`, công thức nước `(ushort)((ushort)BlockMaterial.Water | (Cell.MaxWaterLevel << Cell.WaterShift))` `:137`, chỉ số `Cells[x + SizeX * (z + SizeZ * y)]` `:141`. `[BurstDiscard] static void MarkIfManaged :145-149`.
- `client/Assets/Game/Core/TerrainParams.cs` — mẫu lớp cấu hình `[Serializable]` + `Validate()` `:47-62`. **Không sửa.**
- `client/Assets/Game/Core/WorldData.cs` — `WorldSize(int x, int y, int z)` (chia hết cho 16) `:12-19`, `CellCount :21`, `ChunkCount :25`, `Index(x, y, z) => x + X * (z + Z * y)` `:31`, `WorldData(WorldSize size, ushort[] cells)` `:66-72`, `Get`/`GetMaterial` `:77-81`, `public int SurfaceHeight(int x, int z)` `:84-94` (ô cao nhất không phải Empty/Water; −1 nếu không có).
- `client/Assets/Game/Core/Cell.cs` — `enum BlockMaterial : byte { Empty = 0, Grass = 1, Dirt = 2, Stone = 3, Sand = 4, Water = 5, Bedrock = 6 }` `:4-14`, `MaxWaterLevel = 7 :22`, `WaterShift = 10 :27`.
- `client/Assets/Game/Core/WorldHash.cs` — FNV-1a 64 bit: `Compute(WorldData) :12-26`, `ToHex :28`, `MixInt :30-37`.
- `client/Assets/Game/Client/ConfigLoader.cs :13-24` — `WorldM1Path = "Config/world_m1"`, `LoadTerrainParams()` (Resources.Load<TextAsset> → JsonUtility.FromJson → Validate).
- `client/Assets/Game/Client/HashCheckView.cs :17-26` — scene HashCheck in mã băm lên màn hình + log `HASHCHECK ...`.
- `client/Assets/Game/Client/Editor/HashCheckBuild.cs` — menu `Game/Build APK HashCheck` `:32`, ra `Builds/hashcheck.apk` `:19` (đường dẫn tương đối với thư mục `client/`). Thư mục `client/Builds/` đã nằm trong `.gitignore`.
- Test mẫu: `client/Assets/Game/Tests/World/TerrainGeneratorTests.cs` (`LayerRules :42-84`, `Version2HasOceanRingAroundIsland :97-117`), `GoldenHashTests.cs :11-38`, `ConfigTests.cs :8-50`.
- Asmdef: `Game.Core` (No Engine References), `Game.World` (Burst, Mathematics, Collections), `Game.World.Tests` (EditMode, tham chiếu Game.Core, Game.World, Unity.Mathematics, Unity.Collections), `Game.Client.Editor` (`client/Assets/Game/Client/Editor/Game.Client.Editor.asmdef`, hiện chỉ tham chiếu `Game.Client`).
- Thiết kế: [world-tech "World nhiều map + minimap (M1)"](../../design/04-tech/world-tech.md#world-nhiều-map--minimap-m1) và dòng 7a trong bảng kế hoạch prototype.
- API Unity/package dùng trong brief (đã tra):
  - `Unity.Mathematics` 1.3.2 (bản trong `client/Library/PackageCache/com.unity.mathematics@19a9377c4ffa`): `noise.snoise(float2)`, `Random.NextFloat(float min, float max)` (`random.cs:472`), `Random.NextFloat2(float min, float max)`, `math.TAU` (float, = 2π, `math.cs:164`), `math.sincos(float x, out float s, out float c)` (`math.cs:4876`), `math.smoothstep`, `math.lerp`, `math.distance`, `math.distancesq`, `math.floor`, `math.clamp`, `math.min`, `math.max` [repo].
  - Burst `FloatMode.Deterministic`: kết quả số thực giống nhau trên mọi nền tảng được hỗ trợ, dùng bản tất định của các hàm toán, chỉ hỗ trợ 64-bit — [docs](https://docs.unity3d.com/Packages/com.unity.burst@1.8/manual/compilation-burstcompile.html).
  - `JsonUtility`: trường mảng (`string[]`) của lớp `[Serializable]` được tuần tự hoá — [docs 6000.4](https://docs.unity3d.com/6000.4/Documentation/Manual/script-serialization-rules.html); trường thiếu trong JSON giữ giá trị khởi tạo — [docs 6000.4](https://docs.unity3d.com/6000.4/Documentation/Manual/json-serialization.html).
  - `new Texture2D(int width, int height, TextureFormat textureFormat, bool mipChain)` — [docs 6000.4](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/Texture2D-ctor.html); texture tạo từ script đọc được (`isReadable` = true) — [docs 6000.4](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/Texture-isReadable.html).
  - `Texture2D.SetPixels32(Color32[] colors, int miplevel = 0)`: mảng chạy theo từng hàng, bắt đầu từ góc **dưới trái**; phải gọi `Apply()` sau đó — [docs 6000.4](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/Texture2D.SetPixels32.html).
  - `Color32.Lerp(Color32 a, Color32 b, float t)` (t bị kẹp vào [0, 1]) — [docs 6000.4](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/Color32.Lerp.html).
  - `ImageConversion.EncodeToPNG(Texture2D tex) : byte[]` (texture không nén, đọc được) — [docs 6000.4](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/ImageConversion.EncodeToPNG.html).

## Quyết định đã chốt (không được đổi)
1. **File + lớp mới** (user 2026-10-20). v3 nằm ở `WorldMapParams` + `MapTable` + `WorldMapGenerator` + `design/data/world_maps_m1.json`. `TerrainGenerator`, `TerrainParams`, `world_m1.json` và mọi scene/test đang dùng chúng giữ nguyên. Mã băm v1/v2 không đổi.
2. **Bố cục** (user 2026-10-20): world 384 × 48 × 384. Đất liền là một khối méo noise ở giữa world. 3 tâm map cách đều nhau quanh tâm world, góc xoay theo seed. Ranh giới là Voronoi trên toạ độ đã méo bằng noise 2 tầng. Cột cách biên world < `seaRing` (24) luôn là đáy biển.
3. **Địa hình** (user 2026-10-20): cả world dùng chung một bộ noise độ cao. Mỗi map có 1 hồ ở tâm map. Không có preset riêng từng map.
4. **Ảnh xem trước** (user 2026-10-20): menu Editor xuất PNG 384 × 384, 1 điểm ảnh = 1 cột, màu theo bảng ở bước 9. Đây chỉ là công cụ xem, minimap thật làm ở 7c.
5. **Bảng map**: 1 byte/cột, chỉ số `x + SizeX * z`. Giá trị `0..MapCount−1` là map, `MapTable.Ocean = 255` là biển (cột có `island == 0`, xem công thức).
6. **"Cột đất"** (dùng cho tiêu chí diện tích): cột có `SurfaceHeight(x, z) >= waterLevel`, tức trên mặt đất không có nước.
7. **Thứ tự rút số ngẫu nhiên** trong job phải đúng như bước 4. Đổi thứ tự là ra world khác.
8. **Tên map** chỉ nằm trong JSON (`mapNames`) và không được đưa vào mã băm.
9. **Chỉnh tham số khi test hình dạng hỏng** (Planner sửa 2026-10-20, sau Vướng mắc lần 1) — tính từ `borderWarpAmplitude` 18 **có** bước gộp mảnh rời (QĐ 10); chỉ được làm đúng như sau, tối đa 4 lần chỉnh, mỗi lần ghi vào bảng ở mục Kết quả (đánh số tiếp từ lần 2):
   - `EachBorderBendsAtLeast8` hỏng → tăng `borderWarpAmplitude` 18 → 22 (chỉ một lần).
   - `EachMapIsOneRegion` hỏng sau khi đã gộp mảnh → đây là lỗi code của `MergeFragments`, không chỉnh tham số: kiểm lại bước 4b; sửa 2 lần vẫn hỏng → `cần Opus`.
   - `EachMapLandAreaWithin25Percent` hỏng vì map quá nhỏ → tăng `landRadius` thêm 4, tối đa 140. Hỏng vì quá lớn → giảm 4, tối thiểu 124.
   - Hai test mâu thuẫn nhau (vd 22 làm đứt map, 14 làm ranh giới thẳng), hoặc đã hết 4 lần → dừng, `cần Opus`.
   - **Không** đổi danh sách seed test, không đổi công thức, không nới ngưỡng.
10. **Gộp mảnh rời** (Planner 2026-10-20, trả lời Vướng mắc lần 1). Ranh giới Voronoi méo có thể cắt bờ biển hai lần, để lại một dải cột của map này nằm kẹp giữa map khác và biển (seed 7: dải 25 cột nước nông ven bờ của map 1, 0 cột đất, bao bởi map 2 + biển — Planner đo qua MCP; giữ nguyên ở biên độ 14, 18, 22). Vì vậy sau job luôn chạy `MapTable.MergeFragments` (bước 4b): mỗi map chỉ giữ mảnh lớn nhất, mảnh khác chuyển sang map kề. Chỉ dùng số nguyên, chạy ngoài job → vẫn tất định. Bước này nằm trong mã băm bảng map. `borderWarpAmplitude` trở lại 18.

## Phạm vi
- Được tạo:
  - `client/Assets/Game/Core/WorldMapParams.cs`, `client/Assets/Game/Core/MapTable.cs`
  - `client/Assets/Game/World/WorldMapGenerator.cs`
  - `design/data/world_maps_m1.json`, `client/Assets/Game/Client/Resources/Config/world_maps_m1.json`
  - `client/Assets/Game/Tests/World/WorldMapGeneratorTests.cs`
  - `client/Assets/Game/Client/Editor/WorldMapPreview.cs`
  - các file `.meta` do Unity tạo cho những file trên
- Được sửa:
  - `client/Assets/Game/Core/WorldHash.cs` (thêm hàm, không đổi hàm cũ)
  - `client/Assets/Game/Client/ConfigLoader.cs` (thêm hàm)
  - `client/Assets/Game/Client/HashCheckView.cs`
  - `client/Assets/Game/Client/Editor/Game.Client.Editor.asmdef` (thêm tham chiếu)
  - `client/Assets/Game/Tests/World/GoldenHashTests.cs`, `client/Assets/Game/Tests/World/ConfigTests.cs` (thêm test, không sửa test cũ)
  - brief này (mục Kết quả trở xuống)
- Không được sửa: `TerrainGenerator.cs`, `TerrainParams.cs`, `design/data/world_m1.json` và bản chép trong Resources, mọi scene `.unity`, code sim (`Core/Sim/*`), các file trong `design/` (Planner cập nhật world-tech + roadmap khi review).

## Các bước
1. **`client/Assets/Game/Core/WorldMapParams.cs`** (namespace `Game.Core`, `using System;`). Thêm lớp `[Serializable] public class WorldMapParams` có summary: `Thông số bộ sinh world nhiều map (generatorVersion 3, world-tech bước 7a, decision 0022). Nguồn chuẩn: design/data/world_maps_m1.json (client đọc bản sao trong Resources bằng JsonUtility — tên trường phải khớp JSON). Số là số tạm, chỉnh khi chơi thử.` Các trường và giá trị mặc định, đúng thứ tự sau:
   ```csharp
   public int generatorVersion = 3;
   public uint seed = 20261013;
   public int sizeX = 384;
   public int sizeY = 48;
   public int sizeZ = 384;
   public float baseHeight = 22f;
   public float amplitude1 = 10f;
   public float frequency1 = 0.02f;
   public float amplitude2 = 2.5f;
   public float frequency2 = 0.08f;
   public int minSurface = 10;
   public int maxSurface = 40;
   public int bedrockLayers = 2;
   public int dirtDepth = 3;
   public int waterLevel = 12;
   public int sandBand = 1;
   public float lakeRadius = 12f;
   public float lakeDepth = 7f;
   /// <summary>Số ô từ biên world chắc chắn là đáy biển.</summary>
   public int seaRing = 24;
   /// <summary>Đáy biển = waterLevel − seaFloorDepth.</summary>
   public int seaFloorDepth = 4;
   /// <summary>Bán kính khối đất liền quanh tâm world (ô), trước khi méo.</summary>
   public float landRadius = 132f;
   /// <summary>Bề rộng sườn (ô) từ đáy biển lên địa hình noise.</summary>
   public float coastFalloff = 12f;
   public float coastWarpAmplitude = 14f;
   public float coastWarpFrequency = 0.015f;
   public int mapCount = 3;
   /// <summary>Khoảng cách từ tâm world tới tâm mỗi map (ô).</summary>
   public float mapCenterRadius = 64f;
   public float borderWarpAmplitude = 18f;
   public float borderWarpFrequency = 0.01f;
   /// <summary>Tên tạm (0022); tên thật user đặt sau qua design/01-story/naming.md.</summary>
   public string[] mapNames = { "Map 1", "Map 2", "Map 3" };

   public WorldSize Size => new WorldSize(sizeX, sizeY, sizeZ);
   ```
   `public void Validate()` ném `ArgumentException` (thông báo tiếng Việt, mỗi nhóm một câu) khi:
   - `generatorVersion != 3`;
   - `Size` không hợp lệ (gọi `var _ = Size;` như TerrainParams);
   - `bedrockLayers < 1 || bedrockLayers >= minSurface`; `minSurface > maxSurface || maxSurface >= sizeY`; `waterLevel < minSurface || waterLevel >= maxSurface`;
   - `dirtDepth < 0 || sandBand < 0 || lakeRadius <= 0f || lakeDepth < 0f`;
   - `seaRing < 1 || seaFloorDepth < 1 || waterLevel - seaFloorDepth < bedrockLayers`;
   - `landRadius <= 0f || coastFalloff <= 0f || coastWarpAmplitude < 0f || coastWarpFrequency <= 0f || borderWarpAmplitude < 0f || borderWarpFrequency <= 0f`;
   - `mapCount < 2 || mapCount > 8 || mapNames == null || mapNames.Length != mapCount` hoặc có tên rỗng/null;
   - `mapCenterRadius <= 0f || mapCenterRadius + lakeRadius >= landRadius - coastWarpAmplitude - coastFalloff` (hồ phải nằm trọn trong đất liền).

   Xong khi: biên dịch không lỗi, `Game.Core` vẫn không tham chiếu UnityEngine.

2. **`client/Assets/Game/Core/MapTable.cs`** (namespace `Game.Core`):
   ```csharp
   /// <summary>Bảng map (glossary): mỗi cột (x, z) thuộc map nào; Ocean = cột biển, không thuộc map nào (decision 0022).</summary>
   public sealed class MapTable
   {
       public const byte Ocean = 255;
       public readonly int SizeX, SizeZ;
       readonly byte[] ids;
       readonly string[] names;

       public MapTable(int sizeX, int sizeZ, byte[] ids, string[] names)  // ids.Length phải = sizeX * sizeZ, names != null → không thì ArgumentException
       public byte[] Ids => ids;
       public int MapCount => names.Length;
       public string Name(int map) => names[map];
       public byte Get(int x, int z) => ids[x + SizeX * z];
   }
   ```
   Xong khi: biên dịch không lỗi.

3. **`client/Assets/Game/Core/WorldHash.cs`**: thêm `public static ulong Compute(MapTable maps)`. Bắt đầu từ `OffsetBasis`, `MixInt(h, maps.SizeX)`, `MixInt(h, maps.SizeZ)`, rồi mỗi byte trong `maps.Ids` lấy `h = (h ^ b) * Prime`. Không băm tên. Không sửa `Compute(WorldData)`.

4. **`client/Assets/Game/World/WorldMapGenerator.cs`** (namespace `Game.World`, cùng các `using` như TerrainGenerator). Summary: `Sinh world nhiều map (generatorVersion 3, world-tech bước 7a, decision 0022): khối đất liền giữa world chia 3 map bằng ranh giới Voronoi méo noise; biển bao quanh. Mọi phép số thực nằm trong job Burst FloatMode.Deterministic.`
   - API công khai:
     ```csharp
     public static WorldData Generate(WorldMapParams p, out MapTable maps) => Generate(p, out maps, out _);
     public static WorldData Generate(WorldMapParams p, out MapTable maps, out bool ranWithBurst)
     ```
     Làm như `TerrainGenerator.Generate :18-61`: `p.Validate()`, cấp `cells` (`NativeArray<ushort>`, `size.CellCount`), `mapIds` (`NativeArray<byte>`, `sizeX * sizeZ`), `managedFlag`, tất cả `Allocator.TempJob`; `Schedule(size.X * size.Z, 64).Complete()`; `maps = new MapTable(p.sizeX, p.sizeZ, mapIds.ToArray(), (string[])p.mapNames.Clone())`; dispose trong `finally`. `Seed = p.seed == 0 ? 1u : p.seed`. `SeaFloor = p.waterLevel - p.seaFloorDepth`.
   - Job `[BurstCompile(FloatMode = FloatMode.Deterministic, CompileSynchronously = true)] struct WorldMapJob : IJobParallelFor` có `[NativeDisableParallelForRestriction]` cho `Cells` và `ManagedFlag` như TerrainJob (`MapIds` thì mỗi job ghi đúng chỉ số `column` của nó). Trong `Execute(int column)`, viết **đúng thứ tự** sau:
     ```text
     MarkIfManaged(ManagedFlag)
     x = column % SizeX ; z = column / SizeX ; pos = float2(x, z)
     rng = new Random(Seed)
     offset1 = rng.NextFloat2(-1000, 1000)      // noise độ cao tầng 1
     offset2 = rng.NextFloat2(-1000, 1000)      // noise độ cao tầng 2
     offsetCoast = rng.NextFloat2(-1000, 1000)  // méo bờ biển
     offsetWarpX1 = rng.NextFloat2(-1000, 1000) // méo ranh giới, trục x, tầng 1
     offsetWarpZ1 = rng.NextFloat2(-1000, 1000) // trục z, tầng 1
     offsetWarpX2 = rng.NextFloat2(-1000, 1000) // trục x, tầng 2
     offsetWarpZ2 = rng.NextFloat2(-1000, 1000) // trục z, tầng 2
     angle0 = rng.NextFloat(0, math.TAU)

     worldCenter = float2(SizeX, SizeZ) * 0.5

     // Độ cao noise (như v2)
     h = BaseHeight + Amplitude1 * snoise(pos * Frequency1 + offset1) + Amplitude2 * snoise(pos * Frequency2 + offset2)

     // Tâm map i (i = 0..MapCount-1): a = angle0 + i * (TAU / MapCount); sincos(a, out s, out c);
     //   center_i = worldCenter + MapCenterRadius * float2(c, s)
     // Hồ: dLake = min_i distance(pos, center_i) / LakeRadius ; inLake = dLake < 1
     if (inLake) h -= LakeDepth * (1 - dLake * dLake)

     // Đất liền
     shore = LandRadius + CoastWarpAmplitude * snoise(pos * CoastWarpFrequency + offsetCoast) - distance(pos, worldCenter)
     island = smoothstep(0, CoastFalloff, shore)
     edge = min(min(x, z), min(SizeX - 1 - x, SizeZ - 1 - z))
     if (edge < SeaRing) island = 0
     h = lerp(SeaFloor, h, island)

     // Bảng map
     if (island <= 0) MapIds[column] = 255 (MapTable.Ocean)
     else
       f = BorderWarpFrequency ; A = BorderWarpAmplitude
       q = pos + A * float2(snoise(pos * f + offsetWarpX1), snoise(pos * f + offsetWarpZ1))
               + (0.25 * A) * float2(snoise(pos * (2 * f) + offsetWarpX2), snoise(pos * (2 * f) + offsetWarpZ2))
       MapIds[column] = chỉ số i có distancesq(q, center_i) nhỏ nhất (so sánh "<" nghiêm ngặt → hoà thì giữ i nhỏ hơn)

     // Mặt đất — như TerrainJob :110-117, bản v2
     surface = clamp((int)floor(h), MinSurface, MaxSurface)
     if (inLake || island < 1) surface = min(max((int)floor(h), BedrockLayers), MaxSurface)
     sandTop = surface <= WaterLevel + SandBand
     // Ghi lớp ô: chép nguyên khối vòng for :119-142 của TerrainJob (đá đáy, cỏ/cát, đất, đá, nước đầy, rỗng)
     ```
   - Copy `MarkIfManaged` như `:145-149`.

   Xong khi: biên dịch không lỗi; test `RunsWithBurst` (bước 7) đạt.

4b. **Gộp mảnh rời** (QĐ 10).
   - Trong `client/Assets/Game/Core/MapTable.cs`, thêm vào lớp `MapTable`:
     ```csharp
     /// <summary>Gộp mảnh rời (decision 0022, brief 0009 QĐ 10): mỗi map chỉ giữ mảnh lớn nhất (nối 4 hướng); mảnh khác chuyển sang map kề có nhiều cạnh chung nhất (hoà → chỉ số map nhỏ hơn); mảnh không kề map nào giữ nguyên. Lặp tới khi không đổi. Chỉ số nguyên → tất định. Trả về tổng số cột đã chuyển.</summary>
     public static int MergeFragments(byte[] ids, int sizeX, int sizeZ, int mapCount)
     ```
     Cách làm (cần `using System.Collections.Generic;`):
     1. Ném `ArgumentException` nếu `ids == null || ids.Length != sizeX * sizeZ || mapCount < 1 || mapCount > 255`.
     2. Vòng lặp tối đa 16 lượt (lượt thứ 17 vẫn còn đổi → ném `InvalidOperationException("MergeFragments không hội tụ")`). Mỗi lượt:
        - Gán nhãn mảnh: duyệt chỉ số `c = 0 .. ids.Length − 1` (tức quét z rồi x); bỏ qua `Ocean` và cột đã có nhãn; BFS 4 hướng (trong world) qua các cột cùng giá trị. Mảnh đánh số 0, 1, 2… theo thứ tự tìm thấy; lưu cho mỗi mảnh: map, danh sách cột.
        - Mảnh giữ lại của mỗi map = mảnh có nhiều cột nhất; hoà → mảnh có số thứ tự nhỏ hơn.
        - Duyệt các mảnh còn lại theo số thứ tự. Với mỗi mảnh: đếm `edges[m]` = số cặp (cột trong mảnh, cột kề 4 hướng) mà cột kề có `ids` hiện tại `!= Ocean` và khác map của mảnh. Chọn `target` = `m` có `edges[m]` lớn nhất (so sánh `>` nghiêm ngặt khi duyệt `m` từ 0 → hoà giữ `m` nhỏ). Mọi `edges` = 0 → bỏ qua mảnh. Ngược lại gán `ids[c] = (byte)target` cho mọi cột của mảnh, cộng số cột vào bộ đếm lượt.
        - Bộ đếm lượt = 0 → thoát vòng, trả tổng.
   - Trong `WorldMapGenerator.Generate`, thay dòng tạo `maps` bằng:
     ```csharp
     var ids = mapIds.ToArray();
     MapTable.MergeFragments(ids, p.sizeX, p.sizeZ, p.mapCount);
     maps = new MapTable(p.sizeX, p.sizeZ, ids, (string[])p.mapNames.Clone());
     ```
     Cập nhật summary của lớp: thêm câu `Sau job gộp mảnh rời của bảng map (MapTable.MergeFragments).`
   - Xong khi: biên dịch không lỗi; 2 test `MergeFragments*` (bước 7) và `EachMapIsOneRegion` × 5 seed đạt.

5. **JSON**: tạo `design/data/world_maps_m1.json`, viết đúng nội dung dưới đây (4 dấu cách thụt lề, giống `world_m1.json`). Sau đó chép **nguyên byte** sang `client/Assets/Game/Client/Resources/Config/world_maps_m1.json` (`cp`).
   ```json
   {
       "generatorVersion": 3,
       "seed": 20261013,
       "sizeX": 384,
       "sizeY": 48,
       "sizeZ": 384,
       "baseHeight": 22.0,
       "amplitude1": 10.0,
       "frequency1": 0.02,
       "amplitude2": 2.5,
       "frequency2": 0.08,
       "minSurface": 10,
       "maxSurface": 40,
       "bedrockLayers": 2,
       "dirtDepth": 3,
       "waterLevel": 12,
       "sandBand": 1,
       "lakeRadius": 12.0,
       "lakeDepth": 7.0,
       "seaRing": 24,
       "seaFloorDepth": 4,
       "landRadius": 132.0,
       "coastFalloff": 12.0,
       "coastWarpAmplitude": 14.0,
       "coastWarpFrequency": 0.015,
       "mapCount": 3,
       "mapCenterRadius": 64.0,
       "borderWarpAmplitude": 18.0,
       "borderWarpFrequency": 0.01,
       "mapNames": ["Map 1", "Map 2", "Map 3"]
   }
   ```
   Nếu phải chỉnh tham số theo Quyết định 9, sửa **cả 3 chỗ**: 2 file JSON và giá trị mặc định trong `WorldMapParams`.

6. **`client/Assets/Game/Client/ConfigLoader.cs`**: thêm `public const string WorldMapsM1Path = "Config/world_maps_m1";` và `public static WorldMapParams LoadWorldMapParams()`, cùng mẫu với `LoadTerrainParams :16-24` (không thấy file thì ném `FileNotFoundException`, sau đó gọi `Validate`). Cập nhật summary của lớp thành `(world_m1, world_maps_m1, sim_m1)`.

7. **Test `client/Assets/Game/Tests/World/WorldMapGeneratorTests.cs`** (namespace `Game.World.Tests`).
   - Dùng bộ đệm `static readonly Dictionary<uint, (WorldData world, MapTable maps)>` để mỗi seed chỉ sinh một lần. Xoá bộ đệm trong `[OneTimeTearDown]`.
   - Hàm `Params(uint seed) => new WorldMapParams { seed = seed }`.
   - `static readonly uint[] Seeds = { 20261013u, 7u, 123456789u, 42u, 99u };`
   - Mỗi test ghi số đo bằng `TestContext.WriteLine`.

   Các test:
   - `RunsWithBurst`: `Generate(Params(20261013), out _, out bool burst)` → `Assert.IsTrue(burst)`.
   - `SameSeedSameWorldAndMaps`: sinh 2 lần **không qua bộ đệm**. Cả `WorldHash.Compute(world)` và `WorldHash.Compute(maps)` đều bằng nhau. Ghi thời gian sinh (Stopwatch, ms) của lần 2.
   - `DifferentSeedDifferentWorldAndMaps`: seed 1 và 2 → cả 2 mã băm đều khác.
   - `SizeIs384x48x384`: `world.Size == new WorldSize(384, 48, 384)`, `ChunkCount == 1728`, `maps.SizeX == 384`, `maps.SizeZ == 384`, `maps.MapCount == 3`, `maps.Name(0) == "Map 1"`.
   - `LayerRules` (chỉ seed 20261013): chép logic `TerrainGeneratorTests.LayerRules :42-84`, dùng `WorldMapParams`. Thay assert "dưới minSurface chỉ ở lòng hồ" bằng: nếu `s < minSurface` thì ô `s + 1` là nước.
   - `SeaRingIsOcean` (chỉ seed 20261013): mọi cột có `edge < seaRing` (cùng công thức `edge` như job) → `SurfaceHeight == waterLevel − seaFloorDepth`, ô `waterLevel` là nước, ô `waterLevel + 1` rỗng, `maps.Get == MapTable.Ocean`. Đếm số cột như vậy, phải > 0.
   - `OceanColumnsAreSeaFloorAndLandHasAMap([ValueSource(nameof(Seeds))] uint seed)`: mọi cột `Ocean` có `SurfaceHeight == seaFloor`; mọi cột đất (Quyết định 6) có `maps.Get != Ocean`; mọi giá trị khác `Ocean` đều `< MapCount`.
   - `EachMapLandAreaWithin25Percent([ValueSource(nameof(Seeds))] uint seed)`: đếm cột đất theo map. Mỗi map phải nằm trong `[12288, 20480]` (128² ± 25%). Ghi số đếm của cả 3 map.
   - `EachMapIsOneRegion([ValueSource(nameof(Seeds))] uint seed)`: với mỗi map i, BFS 4 hướng từ cột đầu tiên thuộc i (quét z rồi x), chỉ đi qua cột có `Get == i`. Số cột tới được phải bằng tổng số cột của map i.
   - `EachBorderBendsAtLeast8([ValueSource(nameof(Seeds))] uint seed)`: với mỗi cặp `a < b`:
     - `B` = danh sách cột `(x, z)` có `Get == a` và có ít nhất một cột kề 4 hướng (trong world) mang `Get == b`, quét z rồi x.
     - `B` rỗng → fail "map a và b không giáp nhau".
     - `P`, `Q` = cặp trong `B` có khoảng cách bình phương (số nguyên) lớn nhất; hoà thì giữ cặp tìm thấy trước (vòng `i < j`, `j` từ `i + 1`).
     - Assert `|Q − P| >= 32`.
     - `dev` = max trên `B` của `|cross(Q − P, R − P)| / |Q − P|` (tính bằng `double`). Assert `dev >= 8`. Ghi `dev` của từng cặp.
   - `NoFlagsSetByGenerator` (seed 20261013): như `TerrainGeneratorTests :86-95`.
   - `MergeFragmentsMovesStripToNeighbor` (bước 4b; `O` = `MapTable.Ocean`): lưới `sizeX = 6, sizeZ = 3`, mảng `ids` theo chỉ số `x + 6 * z`:
     ```text
     z=0:  0 0 0 1 1 1
     z=1:  0 0 0 1 1 1
     z=2:  O 1 O O O O
     ```
     Sau `MapTable.MergeFragments(ids, 6, 3, 2)`: trả về `1`; ô `(1, 2)` thành `0`; 17 ô còn lại không đổi.
   - `MergeFragmentsKeepsConnectedMapsUnchanged`: cùng lưới nhưng ô `(1, 2)` = `0` → trả về `0`, mảng không đổi.

   Xong khi: mọi test trong lớp đều đạt. Nếu test hình dạng hỏng thì làm theo Quyết định 9.

8. **`GoldenHashTests.cs` + `ConfigTests.cs`** (chỉ thêm, giữ nguyên test cũ):
   - `GoldenHashTests`:
     - thêm `public const string ExpectedV3World = "...";` và `public const string ExpectedV3Maps = "...";`, summary `v3 (world nhiều map, 0022) — macOS ARM64 <ngày>; điện thoại: chờ user (brief 0009).`;
     - thêm test `WorldMapsMatchGoldenHash`: `new WorldMapParams()`, assert `generatorVersion == 3`, so `WorldHash.ToHex` của world và của maps với 2 hằng.
     - Cách lấy giá trị: chạy test lần đầu với chuỗi `"0000000000000000"`, lấy giá trị thật từ dòng `TestContext.WriteLine`/thông báo lỗi, chép vào hằng, chạy lại cho đạt. **Chỉ lấy sau khi** mọi test ở bước 7 đã đạt (tham số đã cố định).
   - `ConfigTests`: thêm 3 test theo mẫu `:14-49`, dùng `world_maps_m1.json` + `WorldMapParams`:
     - `WorldMapsClientCopyMatchesDesignData`;
     - `WorldMapsConfigParsesAndValidates` (Size = `384×48×384`, `generatorVersion == 3`, `mapNames.Length == 3`);
     - `WorldMapsConfigMatchesCodeDefaults` (so `JsonUtility.ToJson`).

9. **Ảnh xem trước `client/Assets/Game/Client/Editor/WorldMapPreview.cs`** (namespace `Game.Client.Editor`):
   - `using`: `System.Diagnostics` (Stopwatch), `System.IO`, `Game.Core`, `Game.World`, `UnityEditor`, `UnityEngine`; `ConfigLoader` nằm ở namespace cha `Game.Client`. Gọi Debug bằng `UnityEngine.Debug.Log` (tránh trùng tên với `System.Diagnostics.Debug`).
   - Trước tiên thêm `"Game.Core"` và `"Game.World"` vào `references` của `Game.Client.Editor.asmdef`, sau `"Game.Client"`.
   - `[MenuItem("Game/Ảnh xem trước world nhiều map")] public static void Export()`:
     - gọi `ConfigLoader.LoadWorldMapParams()` rồi `WorldMapGenerator.Generate(p, out var maps, out bool burst)`, đo ms;
     - `var tex = new Texture2D(p.sizeX, p.sizeZ, TextureFormat.RGBA32, false)`;
     - tạo mảng `Color32[sizeX * sizeZ]`, phần tử `[x + sizeX * z]` (góc dưới trái = (0, 0), +z hướng lên);
     - `tex.SetPixels32(px); tex.Apply();`
     - `Directory.CreateDirectory("Builds")`, rồi `File.WriteAllBytes("Builds/world_maps_preview.png", ImageConversion.EncodeToPNG(tex))`;
     - `UnityEngine.Object.DestroyImmediate(tex)`;
     - `Debug.Log("WORLDPREVIEW ...")` gồm: seed, ms, burst, 2 mã băm (hex), số cột đất của từng map kèm tên.
   - Màu mỗi cột (theo thứ tự ưu tiên):
     1. Viền ranh giới: `Get(x, z) != Ocean` và (`x + 1 < sizeX` và `Get(x + 1, z)` khác Ocean và khác `Get(x, z)`) hoặc (`z + 1 < sizeZ` và `Get(x, z + 1)` khác Ocean và khác `Get(x, z)`) → `(30, 30, 30, 255)`.
     2. Nước (`s = SurfaceHeight < waterLevel`): `depth = waterLevel − s`, `t = min(depth − 1, 7) / 7f`, màu = `Color32.Lerp(new Color32(90, 170, 220, 255), new Color32(20, 60, 140, 255), t)`.
     3. Đất: `tier = s / 4`, màu theo bảng `tier → RGB`: 0–2 `(200, 190, 120)` · 3 `(120, 180, 90)` · 4 `(100, 165, 80)` · 5 `(85, 150, 70)` · 6 `(110, 140, 70)` · 7 `(140, 130, 80)` · 8 `(150, 120, 90)` · 9 `(160, 140, 120)` · 10 `(200, 200, 200)` · ≥ 11 `(240, 240, 240)`; alpha 255.
   - Xong khi: chạy menu (MCP `execute_menu_item` "Game/Ảnh xem trước world nhiều map") → có file `client/Builds/world_maps_preview.png` 384 × 384, console có dòng `WORLDPREVIEW`, không lỗi.

10. **`HashCheckView.cs`**: giữ nguyên phần v2 (dòng `:19-25`). Sau đó sinh thêm world v3 bằng `ConfigLoader.LoadWorldMapParams()` + `WorldMapGenerator.Generate(p3, out var maps, out bool burst3)`, đo ms riêng.
    - Nối vào `text` (giữ nguyên chữ):
      ```
      \n\nworld_maps_m1 v{p3.generatorVersion} seed {p3.seed}\nworld {worldHash3}\nmaps {mapsHash3}\nburst {burst3} · {ms3} ms
      ```
    - Thêm log `Debug.Log($"HASHCHECK3 world={worldHash3} maps={mapsHash3} burst={burst3} ms={ms3} v={p3.generatorVersion} seed={p3.seed}");`.
    - Đổi summary của lớp: thêm câu `Kèm world nhiều map v3 (world_maps_m1) — phải trùng GoldenHashTests.ExpectedV3World / ExpectedV3Maps.`
    - Xong khi: Play scene `Assets/Game/Client/Scenes/HashCheck.unity` trong Editor → console có dòng `HASHCHECK3` với 2 mã băm trùng 2 hằng ở bước 8 và `burst=True`. Thoát Play.

11. **Build APK cho user kiểm trên điện thoại**: chạy menu `Game/Build APK HashCheck` → `client/Builds/hashcheck.apk`. Máy này không có `adb` trong PATH, nên **không** tự cài: ghi đường dẫn APK vào Kết quả, phần điện thoại để `chờ user`. Sau khi build → `git checkout -- client/ProjectSettings/UnityConnectSettings.asset`. Kiểm `git status`: không còn thay đổi ngoài Phạm vi; nếu có file ProjectSettings khác bị đổi thì ghi lại tên file, **không** tự revert.

12. Commit code (message `brief 0009: bộ sinh world nhiều map v3`), sau đó điền mục Kết quả + Kết quả test, đổi trạng thái → `chờ review`, rồi commit brief.

## Tiêu chí xong
- Mọi test EditMode đạt: test cũ + `WorldMapGeneratorTests` + 3 test mới trong `ConfigTests` + `WorldMapsMatchGoldenHash`. PlayMode cũ vẫn đạt.
- `Version1StillMatchesItsGoldenHash` và `DefaultWorldMatchesGoldenHash` (v2) vẫn đạt với hằng cũ, không sửa.
- Có `client/Builds/world_maps_preview.png` và `client/Builds/hashcheck.apk` (không commit, đã gitignore).
- `HASHCHECK3` trong Editor trùng golden hash.
- Mục Kết quả có đủ: thời gian sinh v3 trong Editor (ms); số cột đất từng map × 5 seed; `dev` từng cặp × 5 seed; bảng chỉnh tham số (nếu có); giờ bắt đầu/kết thúc.

## Test sau khi xong
### Phần cần test
- **WorldMapParams.Validate + JSON** (bước 1, 5) · `ConfigTests.WorldMapsConfigParsesAndValidates`, `WorldMapsConfigMatchesCodeDefaults`, `WorldMapsClientCopyMatchesDesignData` · đạt.
- **MapTable + WorldHash.Compute(MapTable)** (bước 2, 3) · `SameSeedSameWorldAndMaps`, `DifferentSeedDifferentWorldAndMaps`, `SizeIs384x48x384` · đạt.
- **Bộ sinh chạy Burst, tất định** (bước 4) · `RunsWithBurst`, `SameSeedSameWorldAndMaps` · `burst = true`, mã băm bằng nhau.
- **Lớp ô đúng luật** (bước 4) · `LayerRules`, `NoFlagsSetByGenerator` · đạt.
- **Vành biển + bảng map khớp đất/biển** (bước 4) · `SeaRingIsOcean`, `OceanColumnsAreSeaFloorAndLandHasAMap` × 5 seed · đạt.
- **Diện tích mỗi map 128² ± 25%** (tiêu chí 7a) · `EachMapLandAreaWithin25Percent` × 5 seed · đạt, ghi số.
- **Gộp mảnh rời** (bước 4b) · `MergeFragmentsMovesStripToNeighbor`, `MergeFragmentsKeepsConnectedMapsUnchanged` · đạt.
- **Ranh giới cong ≥ 8 ô, map liền một khối** (tiêu chí 7a) · `EachBorderBendsAtLeast8`, `EachMapIsOneRegion` × 5 seed · đạt, ghi `dev`.
- **Golden hash v3** (bước 8) · `WorldMapsMatchGoldenHash` · đạt.
- **Golden hash v1/v2 không đổi** (hồi quy, Quyết định 1) · `GoldenHashTests.Version1StillMatchesItsGoldenHash`, `DefaultWorldMatchesGoldenHash` · đạt, hằng không đổi trong diff.
- **ConfigLoader** (bước 6) · gián tiếp qua bước 9 + 10 (menu và HashCheck đọc từ Resources) · không lỗi.
- **Ảnh xem trước** (bước 9) · chạy menu · có file PNG 384 × 384 + dòng `WORLDPREVIEW`.
- **HashCheck v3** (bước 10) · Play scene HashCheck trong Editor · dòng `HASHCHECK3` trùng 2 hằng, `burst=True`; dòng `HASHCHECK` v2 vẫn `807d00bd048026a0`.
- **Phần cũ có thể bị ảnh hưởng** (asmdef Editor đổi, Resources thêm file) · toàn bộ EditMode + PlayMode · số test cũ đạt như trước (EditMode 213 + test mới, PlayMode 8/8).

### Lệnh executor chạy
1. `python3 "<Plugin root>/scripts/brief_check.py" 0009 --model <model id>` (theo skill exec).
2. MCP `run_tests` EditMode, lọc `Game.World.Tests.WorldMapGeneratorTests` → đạt (lặp theo Quyết định 9 nếu cần).
3. MCP `run_tests` EditMode toàn bộ → đạt hết; ghi tổng số.
4. MCP `run_tests` PlayMode toàn bộ → 8/8. Sau đó `git checkout -- client/ProjectSettings/EditorSettings.asset`.
5. MCP `execute_menu_item` "Game/Ảnh xem trước world nhiều map" → `read_console` lấy dòng `WORLDPREVIEW`; `ls -la client/Builds/world_maps_preview.png`.
6. Mở scene HashCheck, Play, `read_console` lấy `HASHCHECK` + `HASHCHECK3`, thoát Play.
7. MCP `execute_menu_item` "Game/Build APK HashCheck" → `ls -la client/Builds/hashcheck.apk`. Sau đó `git checkout -- client/ProjectSettings/UnityConnectSettings.asset`.
8. `python3 tools/check_design.py` → exit 0.
9. `git status --short` → chỉ còn file trong Phạm vi (trước commit).

### User tự test
1. Mở `client/Builds/world_maps_preview.png` (phóng to lên khoảng 2–3 lần). Cần thấy:
   - một khối đất ở giữa, biển bao quanh, mép ảnh toàn màu biển;
   - đúng 3 vùng ngăn bởi đường viền đen; mỗi đường viền đi từ gần tâm ra bờ biển và **cong** (không thẳng, không phải hình chữ nhật);
   - mỗi vùng có một hồ (vệt xanh) ở gần giữa;
   - bờ biển lượn, không tròn đều.
   Nhận xét hình dạng có vừa ý không (vd muốn ranh giới cong hơn hay đất to/nhỏ hơn).
2. Cài `client/Builds/hashcheck.apk` lên điện thoại Android (Xiaomi 24095PCADG đã dùng ở bước 1). Mở app, chờ dòng `world_maps_m1 v3`. Đọc 2 mã `world ...` và `maps ...`, so với `ExpectedV3World` / `ExpectedV3Maps` trong mục Kết quả: phải **trùng**. Ghi thêm số ms hiện trên màn hình. Dòng v2 phía trên vẫn là `807d00bd048026a0`.

## Dừng và chuyển lại Opus khi
- Brief mơ hồ hoặc mâu thuẫn với code thật.
- Cần một quyết định thuộc AGENTS.md §3.4 hoặc ngoài "Quyết định đã chốt" (vd đổi công thức, đổi seed test, nới ngưỡng).
- Test hình dạng vẫn hỏng sau khi chỉnh đúng như Quyết định 9.
- Mã băm v1/v2 đổi.
- Cùng một lỗi sửa 2 lần vẫn chưa qua.
- Phải sửa file ngoài "Phạm vi".

---
<!-- Từ đây trở xuống: EXECUTOR ghi. -->

## Kết quả
<!-- Base commit (trước khi làm) · Chạy bằng: <model id> · <effort> (dòng OK của brief_check.py) · commit của brief · file đã đổi · ghi chú lệch so với brief (nếu có, kèm lý do). -->
- Base commit: ba77040
- Chạy bằng: claude-sonnet-5-5 · medium (OK: brief 0009 cần Sonnet · medium — phiên: claude-sonnet-5-5 · medium)
- Bắt đầu: 2026-10-20 12:05; dừng 12:30 (cần Opus). Làm tiếp lần 2 từ 14:00 (base commit 90b3845, claude-sonnet-5-5 · medium), xong 14:15.
- Commit code: d5f6b17 (lần 2: MapTable.MergeFragments + gọi trong WorldMapGenerator + 2 test MergeFragments + hằng golden v3); code còn lại ở WIP 711755b. Không chỉnh tham số (borderWarpAmplitude giữ 18, lượt 2+ không cần).
- Golden v3 (macOS ARM64): ExpectedV3World = a7719c8999939263 · ExpectedV3Maps = ab2074e6196e5f51.
- APK: client/Builds/hashcheck.apk (34,6 MB); ảnh: client/Builds/world_maps_preview.png (384×384).
- Thời gian sinh v3 trong Editor: 12–14 ms (test/menu), 23 ms (HashCheck).
- Cột đất theo map [Map1, Map2, Map3]: 20261013 [17854, 14668, 18242] · 7 [16407, 16302, 16643] · 123456789 [17151, 18045, 16294] · 42 [17104, 16762, 16552] · 99 [16807, 16698, 17433].
- dev ranh giới [0-1, 0-2, 1-2]: 20261013 [15.46, 15.35, 18.67] · 7 [14.56, 11.38, 17.12] · 123456789 [28.35, 9.46, 24.15] · 42 [23.51, 11.92, 13.04] · 99 [17.03, 11.18, 24.19].
- Đã làm: bước 1–4 (WorldMapParams, MapTable, WorldHash.Compute(MapTable), WorldMapGenerator), 5 (JSON ×2), 6 (ConfigLoader), 7 (WorldMapGeneratorTests), 8 (test golden/config; hằng ExpectedV3* còn "0000000000000000" — chưa lấy), 9 (WorldMapPreview + asmdef), 10 (HashCheckView). Chưa chạy: bước 8 lấy hash, 9–11 chạy menu/build, test toàn bộ. Code để ở commit WIP `brief 0009: WIP`.
- Bảng chỉnh tham số (Quyết định 9), tham số trả về 18 (giá trị brief) khi dừng:

| Lần | Chỉnh | Kết quả |
|---|---|---|
| 0 | borderWarpAmplitude 18 | `EachMapIsOneRegion(7)` hỏng (map 1: 17612 cột, tới được 17587); các test khác đạt (kể cả `EachBorderBendsAtLeast8` 5 seed) |
| 1 | 18 → 14 (vì EachMapIsOneRegion hỏng) | `EachMapIsOneRegion(7)` VẪN hỏng (17510 / 17487); thêm `EachBorderBendsAtLeast8(123456789)` hỏng (cặp 0-2 dev 7.53) và `(99)` hỏng (cặp 0-2 dev 7.57) |
| 2 | trả về 18 + MergeFragments (QĐ 10, không chỉnh tham số) | mọi test hình dạng đạt, 5 seed |

## Kết quả test
- WorldMapParams/JSON (ConfigTests ×3) → đạt (trong EditMode toàn bộ 246/246).
- MapTable + hash, Burst, tất định, lớp ô, vành biển, bảng map khớp đất/biển, diện tích ±25%, gộp mảnh (2 test), ranh giới cong ≥ 8 + một khối × 5 seed, golden v3 → đạt (WorldMapGeneratorTests 29/29; WorldMapsMatchGoldenHash đạt trong toàn bộ).
- Golden v1/v2 không đổi → đạt (hằng cũ không sửa).
- Ảnh xem trước → đạt: dòng `WORLDPREVIEW seed=20261013 ms=14 burst=True world=a7719c8999939263 maps=ab2074e6196e5f51 ...`, PNG 384×384.
- HashCheck → đạt: `HASHCHECK hash=807d00bd048026a0 ... v=2`; `HASHCHECK3 world=a7719c8999939263 maps=ab2074e6196e5f51 burst=True ms=23 v=3`.
- Điện thoại + nhận xét ảnh → chờ user.
- Lệnh: brief_check → OK; EditMode toàn bộ → 246/246; PlayMode → 8/8 (đã `git checkout` EditorSettings.asset); menu ảnh → ok; Play HashCheck → ok; build APK → Succeeded (đã checkout UnityConnectSettings.asset); check_design → exit 0; git status chỉ còn file trong Phạm vi.

## Vướng mắc
<!-- Khi trạng thái = cần Opus: câu hỏi cụ thể + các phương án (kèm đề xuất). -->
**Trả lời của Planner (2026-10-20 13:10):** chọn hướng A nhưng sửa ở bộ sinh, không nới test. Planner chạy bộ sinh seed 7 qua MCP `execute_code`: mảnh rời của map 1 là dải 25 cột (A=18; 23 cột ở A=14; 29 ở A=22), bbox x285–296 z98–100, cách tâm world ~135 ô, **0 cột đất** (toàn nước nông ở sườn bờ), kề 16 cạnh biển + 14 cạnh map 2 [run]. Nguyên nhân: ranh giới méo cắt bờ biển hai lần — không do biên độ méo, nên chỉnh biên độ không chữa được. Đã thêm QĐ 10 + bước 4b (gộp mảnh rời bằng số nguyên sau job), sửa QĐ 9 (về lại 18; region hỏng sau khi gộp = lỗi code). Làm tiếp từ commit WIP 711755b: bước 4b, test mới ở bước 7, rồi bước 8–12.

**Câu hỏi (executor, lần 1):** Hai test hình dạng mâu thuẫn theo Quyết định 9. Seed 7 luôn có một mảnh nhỏ (~23–25 cột) của map 1 tách khỏi thân chính ở cả 18 lẫn 14 (giảm biên độ méo không chữa được), còn ở 14 ranh giới cặp 0-2 của seed 123456789 và 99 chỉ cong 7,5 (< 8). Quyết định 9 không còn lượt nào hợp lệ (tăng 22 → bend đạt nhưng tới nay chưa đo được region; giảm thêm không được phép). Cần Opus quyết.

Dữ liệu ở 14 (dev các cặp): seed 20261013: 0-1 10.97, 0-2 12.24, 1-2 14.55 · seed 7: 11.66 / 9.69 / 15.28 · seed 123456789: 20.90 / 7.53 / (chưa in vì fail) · seed 42: 20.51 / 11.91 / 9.11 · seed 99: 13.46 / 7.57 / (chưa in).
Diện tích map ở 14 (cột đất): 20261013 [17631, 15018, 18115] · 7 [16478, 16179, 16695] · 123456789 [17108, 17880, 16502] · 42 [16899, 16752, 16767] · 99 [16979, 16589, 17370] — đều trong [12288, 20480].

**Phương án:**
- A (đề xuất): giữ borderWarpAmplitude 18 (bend đạt) và sửa nguyên nhân mảnh rời: Voronoi méo bằng noise tần số cao (tầng 2) có thể tạo "đảo" nhỏ; đổi cách méo (vd bỏ tầng 2 hoặc giảm 0,25A → 0,15A) hoặc cho phép test bỏ qua mảnh < 50 cột. Cả hai đều ngoài Quyết định 9.
- B: thử 22 (đúng Quyết định 9 dòng 1) rồi xem region — executor chưa thử vì region đã hỏng ở cả 18 và 14.
- C: đổi seed 7 trong danh sách seed test (bị cấm nếu không có lệnh Opus).

---
<!-- PLANNER ghi khi review. -->

## Review
<!-- Kết luận: đã duyệt | cần sửa (thêm bước sửa vào "Các bước", trạng thái về chờ làm). Điểm đã đối chiếu. -->
