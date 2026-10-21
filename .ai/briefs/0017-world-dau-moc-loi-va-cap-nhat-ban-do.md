# Brief 0017 — World 7d (phần 1/2): bản đồ cập nhật khi đào/đặt + lõi đặt dấu mốc (checkpoint, công trình đặc trưng)

> Trạng thái: đã duyệt
> Review: đầy đủ + user xem
> Model thực thi: Sonnet · medium
> Cập nhật: 2026-10-21

<!-- Planner (Opus) viết, Executor (Sonnet) làm theo ở PHIÊN KHÁC — executor không có lịch sử chat, không được tự quyết.
Trạng thái: chờ làm → đang làm → chờ review → đã duyệt; hoặc → cần Opus (executor dừng, hỏi ở mục Vướng mắc).
File được commit. -->

## Mục tiêu
Task [world bước 7](../tasks/m1-world-buoc-7-nhieu-map-minimap.md), dòng **7d** của [world-tech](../../design/04-tech/world-tech.md#kế-hoạch-prototype-m1), theo quyết định [0023](../../design/decisions/0023-dau-moc-cong-trinh-checkpoint-va-dan-duong.md). Bước 7d chia làm 2 brief:
- **0017 (brief này):**
  - (a) Bản đồ cập nhật đúng điểm ảnh của cột vừa đào/đặt. Lõi: `MinimapImage.UpdateColumn`. Client: `MinimapView` nghe sự kiện `WorldEditor.CellChanged`.
  - (b) Lõi `LandmarkPlacer` trong Game.Core: chọn vị trí **1 checkpoint mỗi map** và các **công trình đặc trưng** (loại, tên tạm, số lượng trong JSON), tất định theo seed, chạy sau khi sinh world, không đổi ô world.
  - Có test EditMode + PlayMode.
- **0018 (viết sau khi 0017 được duyệt):** hiển thị dấu mốc trên bản đồ và trong world.

## Bối cảnh cần đọc
- `client/Assets/Game/Core/MinimapImage.cs`:
  - `public static void ColumnInfo(WorldData world, int x, int z, out int surface, out int waterDepth)` (`:44`): surface = ô rắn cao nhất, không tính nước (−1 nếu cột rỗng); waterDepth = ô nước cao nhất phía trên surface − surface (0 nếu không có nước).
  - `public static uint ColumnColor(WorldData world, int x, int z)` (`:68`, trả 0xRRGGBB).
  - `public static byte[] Build(WorldData world)` (`:78-93`): RGBA32, cột (x, z) ở byte `(x + Size.X · z) · 4`.
  - Lớp đóng ở `:94`.
  - `public const int TierHeight = 4;` (`:12`).
- `client/Assets/Game/Core/WorldMapParams.cs`:
  - `[Serializable] public class WorldMapParams` (`:9-82`), đọc bằng `JsonUtility`; tên trường phải khớp JSON.
  - `public uint seed` (`:13`), `public int waterLevel = 12;` (`:29`), `public int mapCount = 3;` (`:46`), `public string[] mapNames = …;` (`:52`).
  - `public void Validate()` (`:56-81`); câu `throw` cuối cùng ở `:80`.
- `client/Assets/Game/Core/MapTable.cs`: `public const byte Ocean = 255;` (`:9`), `SizeX, SizeZ` (`:11`), `MapCount` (`:28`), `Name(int)` (`:29`), `Get(int x, int z)` (`:30`).
- `client/Assets/Game/Core/Sim/SimRng.cs`: `namespace Game.Core.Sim`, PCG32 chỉ dùng số nguyên. `public SimRng(ulong initState, ulong initSeq)` (`:11`), `public int NextInt(int bound)` (`:30`, đều trong [0, bound)).
- `client/Assets/Game/Core/WorldEditor.cs`:
  - Constructor `public WorldEditor(WorldData world, MaterialInventory inventory, int meshMargin, int seaLevel = -1)` (`:55`).
  - `public event Action<int, int, int> CellChanged;` (`:53`): bắn sau mỗi lần đổi ô, kể cả nước lan theo nhịp.
  - `public readonly int SeaLevel;` (`:40`, = waterLevel của world v3).
  - `TryDig` (`:69`), `TryPlace(int x, int y, int z, BlockMaterial material)` (`:87`), `public readonly MaterialInventory Inventory;` (`:34`), kết quả `EditResult.Ok`.
- `client/Assets/Game/Core/Cell.cs`: `Cell.IsDiggable(ushort)`, `WorldData.Get(x, y, z)`, `WorldData.GetMaterial(x, y, z)`.
- `client/Assets/Game/World/WorldMapGenerator.cs:16`: `public static WorldData Generate(WorldMapParams p, out MapTable maps)`.
- JSON cấu hình: `design/data/world_maps_m1.json` (nguồn chuẩn) và bản sao `client/Assets/Game/Client/Resources/Config/world_maps_m1.json`, hai file phải **giống từng byte**. Các test liên quan trong `client/Assets/Game/Tests/World/ConfigTests.cs`:
  - `WorldMapsClientCopyMatchesDesignData` (`:55`): hai file giống nhau.
  - `WorldMapsConfigMatchesCodeDefaults` (`:74`): `JsonUtility.ToJson` của JSON = của `new WorldMapParams()`.
- Mã băm mẫu: `GoldenHashTests.ExpectedV3World = "a7719c8999939263"`, `ExpectedV3Maps = "ab2074e6196e5f51"` (`client/Assets/Game/Tests/World/GoldenHashTests.cs:19-21`); `WorldHash.Compute(WorldData)` / `WorldHash.Compute(MapTable)` / `WorldHash.ToHex(ulong)`.
- Mẫu test có cache world: `client/Assets/Game/Tests/World/MinimapWorldTests.cs:12-30` (namespace `Game.World.Tests`; asmdef `Game.World.Tests` tham chiếu `Game.Core`, `Game.World`).
- `client/Assets/Game/Client/MinimapView.cs` (brief 0016):
  - Trường `float lastZoom …` (`:50`), `public double BuildMs` (`:57`), `void OnDestroy()` (`:74`).
  - `Update()`: khối dựng `:78-83`.
  - `Build()`: `var rgba = MinimapImage.Build(view.World);` (`:263`), `Texture.Apply(false);` (`:275`).
- `client/Assets/Game/Tests/Client/MinimapViewPlayTests.cs`: lớp `MinimapViewPlayTests`; `SetUp` dựng world v3 nạp dần, camera tại (192, 40, 60); có trường `view`, `map`.
- API Unity mới duy nhất: `Texture2D.SetPixel(int x, int y, Color color)` ([docs](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/Texture2D.SetPixel.html)), đi kèm `Apply(false)` đã dùng ở 0016. Ngoài ra không dùng API Unity nào khác; cần thêm → `cần Opus`.

## Quyết định đã chốt (không được đổi)
1. **Cập nhật bản đồ theo cột:** mỗi lần `CellChanged(x, y, z)`, tính lại màu cột (x, z) bằng `ColumnColor`, ghi vào mảng RGBA đang giữ. Màu đổi thì `SetPixel` vào texture. Cuối `Update` có thay đổi thì gọi `Apply(false)` **một lần mỗi khung**. Không dựng lại cả ảnh.
2. **Dấu mốc tính sau khi sinh world**, chỉ đọc `WorldData` + `MapTable` + `WorldMapParams`. Không sửa ô nào, nên mã băm world/bảng map giữ nguyên. Chỉ dùng số nguyên, rút số ngẫu nhiên bằng `SimRng(seed, 0x4C414E44)` ("LAND").
3. **Ô ứng viên** của map m là cột (x, z) thoả cả 4 điều kiện:
   - không nằm sát biên world (1 ≤ x ≤ SizeX − 2, cùng điều kiện với z);
   - `maps.Get(x, z) == m`;
   - khoảng cách BFS 4 hướng (đi trong cùng map) tới **cột biên** ≥ `landmarkBorderMargin`. Cột biên là cột biển, cột sát biên world, hoặc cột có láng giềng 4 hướng thuộc map khác hay biển;
   - **khô và phẳng**: cột và 8 cột quanh nó đều có `waterDepth == 0` và `surface ≥ waterLevel`, chênh độ cao với cột giữa ≤ 1.
   
   Danh sách ứng viên theo thứ tự duyệt z rồi x.
4. **Checkpoint của map m:** ứng viên gần trọng tâm các cột của map m nhất (tâm cột = x + 0,5). So khoảng cách bằng số nguyên `((2x + 1)·n − (2·ΣX + n))² + (… z …)²`, kiểu `long`. Bằng nhau thì lấy ứng viên gặp trước. Tên hiển thị: `"Checkpoint " + maps.Name(m)`.
5. **Công trình đặc trưng:** duyệt `landmarkTypes` theo thứ tự, mỗi loại `count` cái. Mỗi cái được rút ngẫu nhiên (`NextInt`) trong số ứng viên của map đó còn cách **mọi dấu mốc đã đặt** (gồm checkpoint) ít nhất `landmarkSpacing` ô (dx² + dz² ≥ spacing²). Tên là `name`; nếu count > 1 thì thêm `" k"` (k từ 1). Hết chỗ → ném `InvalidOperationException` (không bỏ qua im lặng).
6. **Cấu hình tạm** (số và tên tạm, chỉnh khi chơi thử; tên thật user đặt sau):
   - Mỗi map 2 loại, mỗi loại 1 cái: "Công trình 1A", "Công trình 1B", "Công trình 2A", "Công trình 2B", "Công trình 3A", "Công trình 3B".
   - `landmarkBorderMargin = 6`, `landmarkSpacing = 20`.
7. **Thứ tự kết quả:** các checkpoint theo map 0..n−1, sau đó các công trình theo thứ tự cấu hình.
8. Không làm phần hiển thị dấu mốc (brief 0018). Không sửa bộ sinh, `design/`, scene. Không làm dịch chuyển hay dẫn đường (M2 — 0023).
9. Giờ bắt đầu/kết thúc lấy bằng `date "+%Y-%m-%d %H:%M"` ngay lúc đó, không ước đoán.

## Phạm vi
- **Được tạo:**
  - `client/Assets/Game/Core/LandmarkPlacer.cs`
  - `client/Assets/Game/Tests/World/LandmarkPlacerTests.cs`
  - `client/Assets/Game/Tests/World/MinimapEditTests.cs`
  - các file `.meta` Unity tự tạo cho 3 file trên.
- **Được sửa:**
  - `client/Assets/Game/Core/WorldMapParams.cs` (bước 1)
  - `design/data/world_maps_m1.json` + `client/Assets/Game/Client/Resources/Config/world_maps_m1.json` (bước 2)
  - `client/Assets/Game/Core/MinimapImage.cs` (bước 3)
  - `client/Assets/Game/Client/MinimapView.cs` (bước 6)
  - `client/Assets/Game/Tests/Client/MinimapViewPlayTests.cs` (bước 7)
  - mục Kết quả / Kết quả test / Vướng mắc của brief này.
- **Không được sửa:** mọi file khác (bộ sinh, `WorldEditor`, `TerrainChunkView`, scene, asmdef, `design/` ngoài JSON). Riêng `ProjectSettings/` phải trả lại sau test.

## Các bước
0. Chạy `date "+%Y-%m-%d %H:%M"` → ghi "Bắt đầu" vào Kết quả; ghi base commit (`git rev-parse --short HEAD`). Unity Editor phải đang mở `client/` và có MCP (thử MCP `read_console`); không có → `cần Opus`.

1. **`client/Assets/Game/Core/WorldMapParams.cs`**:
   - Ngay sau dòng `public string[] mapNames = { "Map 1", "Map 2", "Map 3" };` (`:52`), thêm:
     ```csharp

             /// <summary>Công trình đặc trưng của từng map (0023): loại, tên tạm, số lượng — tên thật user đặt sau.</summary>
             public LandmarkType[] landmarkTypes =
             {
                 new LandmarkType(0, "Công trình 1A", 1),
                 new LandmarkType(0, "Công trình 1B", 1),
                 new LandmarkType(1, "Công trình 2A", 1),
                 new LandmarkType(1, "Công trình 2B", 1),
                 new LandmarkType(2, "Công trình 3A", 1),
                 new LandmarkType(2, "Công trình 3B", 1),
             };
             /// <summary>Dấu mốc cách cột biên (biển, map khác, biên world) ít nhất bấy nhiêu bước 4 hướng trong cùng map.</summary>
             public int landmarkBorderMargin = 6;
             /// <summary>Hai dấu mốc cách nhau ít nhất bấy nhiêu ô (đường chim bay).</summary>
             public int landmarkSpacing = 20;
     ```
   - Ngay sau câu `throw` ở `:80` (vẫn trong `Validate`), thêm:
     ```csharp
                 if (landmarkTypes == null || landmarkBorderMargin < 2 || landmarkSpacing < 1)
                     throw new ArgumentException("landmarkTypes khác null, landmarkBorderMargin ≥ 2, landmarkSpacing ≥ 1");
                 foreach (var t in landmarkTypes)
                     if (t == null || t.map < 0 || t.map >= mapCount || string.IsNullOrEmpty(t.name) || t.count < 1 || t.count > 8)
                         throw new ArgumentException("Mỗi landmarkTypes: map trong [0, mapCount), name khác rỗng, count trong [1, 8]");
     ```
   - Thêm lớp mới, cùng file, sau dấu `}` đóng lớp `WorldMapParams` (`:82`) và trước `}` đóng namespace:
     ```csharp

         /// <summary>Một loại công trình đặc trưng của map (0023), đọc từ JSON.</summary>
         [Serializable]
         public class LandmarkType
         {
             public int map;
             public string name;
             public int count = 1;

             public LandmarkType() { }

             public LandmarkType(int map, string name, int count)
             {
                 this.map = map;
                 this.name = name;
                 this.count = count;
             }
         }
     ```

2. **JSON** — trong `design/data/world_maps_m1.json`, thay dòng cuối `    "mapNames": ["Map 1", "Map 2", "Map 3"]` (dòng ngay trước `}`) bằng:
   ```json
       "mapNames": ["Map 1", "Map 2", "Map 3"],
       "landmarkTypes": [
           { "map": 0, "name": "Công trình 1A", "count": 1 },
           { "map": 0, "name": "Công trình 1B", "count": 1 },
           { "map": 1, "name": "Công trình 2A", "count": 1 },
           { "map": 1, "name": "Công trình 2B", "count": 1 },
           { "map": 2, "name": "Công trình 3A", "count": 1 },
           { "map": 2, "name": "Công trình 3B", "count": 1 }
       ],
       "landmarkBorderMargin": 6,
       "landmarkSpacing": 20
   ```
   (4 dấu cách thụt đầu dòng như phần còn lại; file vẫn kết thúc bằng `}` + xuống dòng). Rồi chép sang client: `cp design/data/world_maps_m1.json client/Assets/Game/Client/Resources/Config/world_maps_m1.json`. Xong khi: `cmp` hai file không in gì; `python3 -c "import json;json.load(open('design/data/world_maps_m1.json'))"` không lỗi.

3. **`client/Assets/Game/Core/MinimapImage.cs`** — thêm ngay sau dấu `}` đóng hàm `Build` (`:93`), trong lớp:
   ```csharp

           /// <summary>
           /// Tính lại màu cột (x, z) vào ảnh RGBA đang giữ (world-tech 7d, brief 0017) — gọi khi ô của cột đổi (đào/đặt, nước lan).
           /// Trả true khi màu đổi.
           /// </summary>
           public static bool UpdateColumn(WorldData world, byte[] rgba, int x, int z)
           {
               uint c = ColumnColor(world, x, z);
               int i = (x + world.Size.X * z) * 4;
               byte r = (byte)(c >> 16), g = (byte)(c >> 8), b = (byte)c;
               if (rgba[i] == r && rgba[i + 1] == g && rgba[i + 2] == b && rgba[i + 3] == 255)
                   return false;
               rgba[i] = r;
               rgba[i + 1] = g;
               rgba[i + 2] = b;
               rgba[i + 3] = 255;
               return true;
           }
   ```

4. **`client/Assets/Game/Core/LandmarkPlacer.cs`** — tạo đúng nội dung:
   ```csharp
   using System;
   using System.Collections.Generic;
   using Game.Core.Sim;

   namespace Game.Core
   {
       public enum LandmarkKind : byte { Checkpoint = 0, Structure = 1 }

       /// <summary>Một dấu mốc trên bản đồ (0023): checkpoint hoặc công trình đặc trưng ở cột (X, Z); Surface = ô rắn cao nhất của cột lúc đặt.</summary>
       public readonly struct Landmark
       {
           public readonly LandmarkKind Kind;
           public readonly int Map;
           /// <summary>Chỉ số trong WorldMapParams.landmarkTypes; −1 với checkpoint.</summary>
           public readonly int Type;
           public readonly int X, Z, Surface;
           public readonly string Name;

           public Landmark(LandmarkKind kind, int map, int type, int x, int z, int surface, string name)
           {
               Kind = kind;
               Map = map;
               Type = type;
               X = x;
               Z = z;
               Surface = surface;
               Name = name;
           }

           public override string ToString() => $"{Kind} {Name} map={Map} type={Type} ({X},{Z}) y={Surface}";
       }

       /// <summary>
       /// Đặt dấu mốc sau khi sinh world (world-tech 7d, decision 0023, brief 0017): 1 checkpoint mỗi map (ứng viên gần trọng tâm map nhất)
       /// + công trình đặc trưng theo landmarkTypes (rút PCG32 theo seed). Chỉ đọc world/bảng map, số nguyên → tất định, không đổi mã băm world.
       /// </summary>
       public static class LandmarkPlacer
       {
           /// <summary>Luồng PCG32 riêng của dấu mốc ("LAND").</summary>
           public const ulong RngStream = 0x4C414E44UL;

           static readonly int[] Dx = { 1, -1, 0, 0 };
           static readonly int[] Dz = { 0, 0, 1, -1 };

           public static List<Landmark> Place(WorldData world, MapTable maps, WorldMapParams p)
           {
               var s = world.Size;
               if (maps.SizeX != s.X || maps.SizeZ != s.Z)
                   throw new ArgumentException("Bảng map khác kích thước world");
               int sx = s.X, sz = s.Z;
               var surface = new int[sx * sz];
               var dry = new bool[sx * sz];
               for (int z = 0; z < sz; z++)
                   for (int x = 0; x < sx; x++)
                   {
                       MinimapImage.ColumnInfo(world, x, z, out int surf, out int depth);
                       int i = x + sx * z;
                       surface[i] = surf;
                       dry[i] = depth == 0 && surf >= p.waterLevel;
                   }

               var dist = BorderDistance(maps);
               var candidates = new List<int>[maps.MapCount];
               for (int m = 0; m < candidates.Length; m++)
                   candidates[m] = new List<int>();
               for (int z = 1; z < sz - 1; z++)
                   for (int x = 1; x < sx - 1; x++)
                   {
                       int i = x + sx * z;
                       byte id = maps.Get(x, z);
                       if (id == MapTable.Ocean || id >= maps.MapCount || dist[i] < p.landmarkBorderMargin || !IsFlatDry(surface, dry, sx, x, z))
                           continue;
                       candidates[id].Add(i);
                   }

               var result = new List<Landmark>();
               for (int m = 0; m < maps.MapCount; m++)
               {
                   long sumX = 0, sumZ = 0, n = 0;
                   for (int z = 0; z < sz; z++)
                       for (int x = 0; x < sx; x++)
                           if (maps.Get(x, z) == m)
                           {
                               sumX += x;
                               sumZ += z;
                               n++;
                           }
                   if (n == 0 || candidates[m].Count == 0)
                       throw new InvalidOperationException($"{maps.Name(m)}: không có chỗ đặt checkpoint");
                   long cx2 = 2 * sumX + n, cz2 = 2 * sumZ + n;
                   int best = -1;
                   long bestD = long.MaxValue;
                   foreach (int i in candidates[m])
                   {
                       long dx = (2L * (i % sx) + 1) * n - cx2;
                       long dz = (2L * (i / sx) + 1) * n - cz2;
                       long d = dx * dx + dz * dz;
                       if (d < bestD)
                       {
                           bestD = d;
                           best = i;
                       }
                   }
                   result.Add(new Landmark(LandmarkKind.Checkpoint, m, -1, best % sx, best / sx, surface[best], "Checkpoint " + maps.Name(m)));
               }

               var rng = new SimRng(p.seed, RngStream);
               long minD2 = (long)p.landmarkSpacing * p.landmarkSpacing;
               var pool = new List<int>();
               for (int t = 0; t < p.landmarkTypes.Length; t++)
               {
                   var type = p.landmarkTypes[t];
                   if (type.map >= maps.MapCount)
                       throw new ArgumentException($"landmarkTypes[{t}].map ngoài bảng map");
                   for (int k = 0; k < type.count; k++)
                   {
                       pool.Clear();
                       foreach (int i in candidates[type.map])
                       {
                           int x = i % sx, z = i / sx;
                           bool far = true;
                           foreach (var l in result)
                           {
                               long dx = x - l.X, dz = z - l.Z;
                               if (dx * dx + dz * dz < minD2)
                               {
                                   far = false;
                                   break;
                               }
                           }
                           if (far)
                               pool.Add(i);
                       }
                       if (pool.Count == 0)
                           throw new InvalidOperationException($"{maps.Name(type.map)}: không còn chỗ đặt {type.name}");
                       int pick = pool[rng.NextInt(pool.Count)];
                       string name = type.count > 1 ? $"{type.name} {k + 1}" : type.name;
                       result.Add(new Landmark(LandmarkKind.Structure, type.map, t, pick % sx, pick / sx, surface[pick], name));
                   }
               }
               return result;
           }

           /// <summary>
           /// Khoảng cách BFS 4 hướng, chỉ đi trong cùng map, tới cột biên. Cột biên (giá trị 0) là cột biển, cột sát biên world,
           /// hoặc cột có láng giềng 4 hướng thuộc map khác/biển. Chỉ số x + SizeX · z.
           /// </summary>
           public static int[] BorderDistance(MapTable maps)
           {
               int sx = maps.SizeX, sz = maps.SizeZ;
               var dist = new int[sx * sz];
               var queue = new int[sx * sz];
               int head = 0, tail = 0;
               for (int z = 0; z < sz; z++)
                   for (int x = 0; x < sx; x++)
                   {
                       int i = x + sx * z;
                       byte id = maps.Get(x, z);
                       bool border = id == MapTable.Ocean || x == 0 || z == 0 || x == sx - 1 || z == sz - 1
                                     || maps.Get(x - 1, z) != id || maps.Get(x + 1, z) != id
                                     || maps.Get(x, z - 1) != id || maps.Get(x, z + 1) != id;
                       if (border)
                       {
                           dist[i] = 0;
                           queue[tail++] = i;
                       }
                       else
                           dist[i] = -1;
                   }
               while (head < tail)
               {
                   int i = queue[head++];
                   int x = i % sx, z = i / sx;
                   byte id = maps.Get(x, z);
                   for (int k = 0; k < 4; k++)
                   {
                       int nx = x + Dx[k], nz = z + Dz[k];
                       if (nx < 0 || nz < 0 || nx >= sx || nz >= sz)
                           continue;
                       int j = nx + sx * nz;
                       if (dist[j] >= 0 || maps.Get(nx, nz) != id)
                           continue;
                       dist[j] = dist[i] + 1;
                       queue[tail++] = j;
                   }
               }
               return dist;
           }

           /// <summary>Cột (x, z) và 8 cột quanh khô (không nước, mặt ≥ mực nước) và chênh độ cao với cột giữa ≤ 1. Cần 1 ≤ x ≤ sx − 2, tương tự z.</summary>
           static bool IsFlatDry(int[] surface, bool[] dry, int sx, int x, int z)
           {
               int c = x + sx * z;
               if (!dry[c])
                   return false;
               for (int dz = -1; dz <= 1; dz++)
                   for (int dx = -1; dx <= 1; dx++)
                   {
                       int j = x + dx + sx * (z + dz);
                       if (!dry[j] || Math.Abs(surface[j] - surface[c]) > 1)
                           return false;
                   }
               return true;
           }
       }
   }
   ```
   Xong bước 1–4 khi: MCP `refresh_unity` → `read_console` (error) không có lỗi.

5. **Test EditMode** (namespace `Game.World.Tests`):
   - `client/Assets/Game/Tests/World/LandmarkPlacerTests.cs` — tạo đúng nội dung:
     ```csharp
     using System;
     using System.Collections.Generic;
     using System.Diagnostics;
     using Game.Core;
     using NUnit.Framework;

     namespace Game.World.Tests
     {
         /// <summary>Dấu mốc bản đồ (decision 0023, world-tech 7d, brief 0017): checkpoint + công trình đặc trưng đặt theo seed sau khi sinh world.</summary>
         public class LandmarkPlacerTests
         {
             static readonly uint[] Seeds = { 20261013u, 7u, 123456789u, 42u, 99u };

             static readonly Dictionary<uint, (WorldData world, MapTable maps)> Cache = new Dictionary<uint, (WorldData, MapTable)>();

             static (WorldData world, MapTable maps) Get(uint seed)
             {
                 if (!Cache.TryGetValue(seed, out var r))
                 {
                     var w = WorldMapGenerator.Generate(new WorldMapParams { seed = seed }, out var m);
                     r = (w, m);
                     Cache[seed] = r;
                 }
                 return r;
             }

             [OneTimeTearDown]
             public void ClearCache() => Cache.Clear();

             static List<Landmark> Place(uint seed)
             {
                 var (w, m) = Get(seed);
                 return LandmarkPlacer.Place(w, m, new WorldMapParams { seed = seed });
             }

             [Test]
             public void PlacesCheckpointPerMapThenStructuresInConfigOrder()
             {
                 var p = new WorldMapParams();
                 foreach (var seed in Seeds)
                 {
                     var (w, m) = Get(seed);
                     var sw = Stopwatch.StartNew();
                     var list = LandmarkPlacer.Place(w, m, new WorldMapParams { seed = seed });
                     sw.Stop();
                     int expected = m.MapCount;
                     foreach (var t in p.landmarkTypes)
                         expected += t.count;
                     Assert.AreEqual(expected, list.Count, $"seed {seed}");
                     for (int i = 0; i < m.MapCount; i++)
                     {
                         Assert.AreEqual(LandmarkKind.Checkpoint, list[i].Kind);
                         Assert.AreEqual(i, list[i].Map);
                         Assert.AreEqual(-1, list[i].Type);
                         Assert.AreEqual("Checkpoint " + m.Name(i), list[i].Name);
                     }
                     int k = m.MapCount;
                     for (int t = 0; t < p.landmarkTypes.Length; t++)
                         for (int c = 0; c < p.landmarkTypes[t].count; c++, k++)
                         {
                             Assert.AreEqual(LandmarkKind.Structure, list[k].Kind);
                             Assert.AreEqual(t, list[k].Type);
                             Assert.AreEqual(p.landmarkTypes[t].map, list[k].Map);
                             Assert.AreEqual(p.landmarkTypes[t].name, list[k].Name);
                         }
                     UnityEngine.Debug.Log($"LANDMARKS seed={seed} ms={sw.Elapsed.TotalMilliseconds:F1} | " + string.Join(" | ", list.ConvertAll(l => l.ToString())));
                 }
             }

             [Test]
             public void IsDeterministic()
             {
                 var a = Place(20261013u);
                 var b = Place(20261013u);
                 var fresh = WorldMapGenerator.Generate(new WorldMapParams(), out var freshMaps);
                 var c = LandmarkPlacer.Place(fresh, freshMaps, new WorldMapParams());
                 CollectionAssert.AreEqual(a.ConvertAll(l => l.ToString()), b.ConvertAll(l => l.ToString()));
                 CollectionAssert.AreEqual(a.ConvertAll(l => l.ToString()), c.ConvertAll(l => l.ToString()));
             }

             [Test]
             public void OnDryFlatLandDeepInsideOwnMap()
             {
                 var p = new WorldMapParams();
                 int margin = p.landmarkBorderMargin;
                 foreach (var seed in Seeds)
                 {
                     var (w, m) = Get(seed);
                     foreach (var l in Place(seed))
                     {
                         Assert.AreEqual(l.Map, m.Get(l.X, l.Z), $"seed {seed} {l}");
                         MinimapImage.ColumnInfo(w, l.X, l.Z, out int surf, out int depth);
                         Assert.AreEqual(surf, l.Surface, $"seed {seed} {l}");
                         Assert.AreEqual(0, depth, $"seed {seed} {l} có nước");
                         Assert.GreaterOrEqual(surf, p.waterLevel, $"seed {seed} {l} thấp hơn mực nước");
                         for (int dz = -1; dz <= 1; dz++)
                             for (int dx = -1; dx <= 1; dx++)
                             {
                                 MinimapImage.ColumnInfo(w, l.X + dx, l.Z + dz, out int s2, out int d2);
                                 Assert.AreEqual(0, d2, $"seed {seed} {l} láng giềng có nước");
                                 Assert.LessOrEqual(Math.Abs(s2 - surf), 1, $"seed {seed} {l} không phẳng");
                             }
                         for (int dz = -margin; dz <= margin; dz++)
                             for (int dx = -margin; dx <= margin; dx++)
                                 if (Math.Abs(dx) + Math.Abs(dz) <= margin)
                                     Assert.AreEqual(l.Map, m.Get(l.X + dx, l.Z + dz), $"seed {seed} {l} gần biên ({dx},{dz})");
                     }
                 }
             }

             [Test]
             public void LandmarksAreSpacedApart()
             {
                 long min2 = (long)new WorldMapParams().landmarkSpacing * new WorldMapParams().landmarkSpacing;
                 foreach (var seed in Seeds)
                 {
                     var list = Place(seed);
                     for (int i = 0; i < list.Count; i++)
                         for (int j = i + 1; j < list.Count; j++)
                         {
                             long dx = list[i].X - list[j].X, dz = list[i].Z - list[j].Z;
                             Assert.GreaterOrEqual(dx * dx + dz * dz, min2, $"seed {seed}: {list[i]} và {list[j]}");
                         }
                 }
             }

             [Test]
             public void CheckpointIsNearMapCentroid()
             {
                 foreach (var seed in Seeds)
                 {
                     var (_, m) = Get(seed);
                     var list = Place(seed);
                     for (int map = 0; map < m.MapCount; map++)
                     {
                         double sx = 0, sz = 0;
                         int n = 0;
                         for (int z = 0; z < m.SizeZ; z++)
                             for (int x = 0; x < m.SizeX; x++)
                                 if (m.Get(x, z) == map)
                                 {
                                     sx += x + 0.5;
                                     sz += z + 0.5;
                                     n++;
                                 }
                         double cx = sx / n, cz = sz / n;
                         var cp = list[map];
                         double d = Math.Sqrt((cp.X + 0.5 - cx) * (cp.X + 0.5 - cx) + (cp.Z + 0.5 - cz) * (cp.Z + 0.5 - cz));
                         UnityEngine.Debug.Log($"CHECKPOINT seed={seed} {cp} cách trọng tâm {d:F1} ô");
                         Assert.LessOrEqual(d, 48.0, $"seed {seed} {cp}");
                     }
                 }
             }

             [Test]
             public void DoesNotChangeWorldOrMapHashes()
             {
                 var w = WorldMapGenerator.Generate(new WorldMapParams(), out var m);
                 LandmarkPlacer.Place(w, m, new WorldMapParams());
                 Assert.AreEqual(GoldenHashTests.ExpectedV3World, WorldHash.ToHex(WorldHash.Compute(w)));
                 Assert.AreEqual(GoldenHashTests.ExpectedV3Maps, WorldHash.ToHex(WorldHash.Compute(m)));
             }

             [Test]
             public void InvalidLandmarkConfigThrows()
             {
                 Assert.DoesNotThrow(() => new WorldMapParams().Validate());
                 Assert.Throws<ArgumentException>(() => new WorldMapParams { landmarkTypes = null }.Validate());
                 Assert.Throws<ArgumentException>(() => new WorldMapParams { landmarkTypes = new[] { new LandmarkType(3, "x", 1) } }.Validate());
                 Assert.Throws<ArgumentException>(() => new WorldMapParams { landmarkTypes = new[] { new LandmarkType(0, "", 1) } }.Validate());
                 Assert.Throws<ArgumentException>(() => new WorldMapParams { landmarkTypes = new[] { new LandmarkType(0, "x", 0) } }.Validate());
                 Assert.Throws<ArgumentException>(() => new WorldMapParams { landmarkBorderMargin = 1 }.Validate());
                 Assert.Throws<ArgumentException>(() => new WorldMapParams { landmarkSpacing = 0 }.Validate());
             }

             [Test]
             public void BorderDistanceIsZeroAtBordersAndGrowsInside()
             {
                 var (_, m) = Get(20261013u);
                 var dist = LandmarkPlacer.BorderDistance(m);
                 for (int z = 1; z < m.SizeZ - 1; z++)
                     for (int x = 1; x < m.SizeX - 1; x++)
                     {
                         int i = x + m.SizeX * z;
                         byte id = m.Get(x, z);
                         bool border = id == MapTable.Ocean || m.Get(x - 1, z) != id || m.Get(x + 1, z) != id || m.Get(x, z - 1) != id || m.Get(x, z + 1) != id;
                         if (border)
                         {
                             Assert.AreEqual(0, dist[i], $"({x},{z})");
                             continue;
                         }
                         Assert.Greater(dist[i], 0, $"({x},{z})");
                         int minN = Math.Min(Math.Min(dist[i - 1], dist[i + 1]), Math.Min(dist[i - m.SizeX], dist[i + m.SizeX]));
                         Assert.AreEqual(minN + 1, dist[i], $"({x},{z})");
                     }
             }
         }
     }
     ```
   - `client/Assets/Game/Tests/World/MinimapEditTests.cs` — tạo đúng nội dung:
     ```csharp
     using Game.Core;
     using NUnit.Framework;

     namespace Game.World.Tests
     {
         /// <summary>World-tech 7d (brief 0017): đào/đặt một cột → đúng một điểm ảnh minimap đổi theo độ cao mới.</summary>
         public class MinimapEditTests
         {
             /// <summary>Cột đất khô đầu tiên (duyệt z rồi x) có mặt ở mép tầng (surface % TierHeight == 0), cao hơn mực nước ≥ 2, ô trên cùng đào được.</summary>
             static bool FindTierEdgeColumn(WorldData w, int waterLevel, out int x, out int z, out int top)
             {
                 for (z = 0; z < w.Size.Z; z++)
                     for (x = 0; x < w.Size.X; x++)
                     {
                         MinimapImage.ColumnInfo(w, x, z, out top, out int depth);
                         if (depth == 0 && top % MinimapImage.TierHeight == 0 && top >= waterLevel + 2 && Cell.IsDiggable(w.Get(x, top, z)))
                             return true;
                     }
                 x = z = top = -1;
                 return false;
             }

             static int DiffPixels(byte[] a, byte[] b)
             {
                 int n = 0;
                 for (int i = 0; i < a.Length; i += 4)
                     if (a[i] != b[i] || a[i + 1] != b[i + 1] || a[i + 2] != b[i + 2] || a[i + 3] != b[i + 3])
                         n++;
                 return n;
             }

             [Test]
             public void DigAndPlaceChangeExactlyOnePixel()
             {
                 var p = new WorldMapParams();
                 var world = WorldMapGenerator.Generate(p, out _);
                 var editor = new WorldEditor(world, new MaterialInventory(), 1, p.waterLevel);
                 Assert.IsTrue(FindTierEdgeColumn(world, p.waterLevel, out int x, out int z, out int top), "không có cột ở mép tầng");
                 var before = MinimapImage.Build(world);
                 var img = (byte[])before.Clone();
                 var material = world.GetMaterial(x, top, z);

                 Assert.AreEqual(EditResult.Ok, editor.TryDig(x, top, z));
                 Assert.IsTrue(MinimapImage.UpdateColumn(world, img, x, z), "đào ô trên cùng ở mép tầng phải đổi màu");
                 Assert.AreEqual(1, DiffPixels(before, img), "đúng một điểm ảnh đổi");
                 CollectionAssert.AreEqual(MinimapImage.Build(world), img, "ảnh cập nhật theo cột = ảnh dựng lại");
                 int i = (x + world.Size.X * z) * 4;
                 uint c = MinimapImage.ColumnColor(world, x, z);
                 Assert.AreEqual(c, (uint)(img[i] << 16 | img[i + 1] << 8 | img[i + 2]));
                 Assert.IsFalse(MinimapImage.UpdateColumn(world, img, x, z), "gọi lại không đổi gì");

                 Assert.AreEqual(EditResult.Ok, editor.TryPlace(x, top, z, material));
                 Assert.IsTrue(MinimapImage.UpdateColumn(world, img, x, z));
                 CollectionAssert.AreEqual(before, img, "đặt lại → ảnh như cũ");
             }

             [Test]
             public void DigInsideTierKeepsPixel()
             {
                 var p = new WorldMapParams();
                 var world = WorldMapGenerator.Generate(p, out _);
                 var editor = new WorldEditor(world, new MaterialInventory(), 1, p.waterLevel);
                 Assert.IsTrue(FindTierEdgeColumn(world, p.waterLevel, out int x, out int z, out int top));
                 var img = MinimapImage.Build(world);
                 // Đặt thêm 1 ô lên cột ở mép tầng (top % 4 == 0 → top + 1 cùng tầng) → màu không đổi.
                 editor.Inventory.Add(BlockMaterial.Dirt);
                 Assert.AreEqual(EditResult.Ok, editor.TryPlace(x, top + 1, z, BlockMaterial.Dirt));
                 Assert.IsFalse(MinimapImage.UpdateColumn(world, img, x, z), "cùng tầng → không đổi điểm ảnh");
                 CollectionAssert.AreEqual(MinimapImage.Build(world), img);
             }
         }
     }
     ```
     (`editor.Inventory` là trường `public readonly MaterialInventory Inventory` — `client/Assets/Game/Core/WorldEditor.cs:34`.)
   Xong khi: MCP `run_tests` EditMode lọc `LandmarkPlacerTests` + `MinimapEditTests` + `ConfigTests` → đạt hết. Ghi các dòng console `LANDMARKS …` và `CHECKPOINT …` của seed 20261013 vào Kết quả.

6. **`client/Assets/Game/Client/MinimapView.cs`** — bản đồ cập nhật khi đổi ô:
   - Sau dòng `float lastZoom = -1f, lastCx, lastCz, lastW, lastH;` (`:50`) thêm:
     ```csharp
             byte[] rgba;
             bool pixelsDirty;
     ```
   - Sau dòng `public double BuildMs { get; private set; }` (`:57`) thêm:
     ```csharp
             /// <summary>Số lần một điểm ảnh đổi màu do đào/đặt/nước (brief 0017).</summary>
             public int UpdatedPixelCount { get; private set; }
     ```
   - Thay `void OnDestroy() { if (Texture != null) Destroy(Texture); }` (`:74`) bằng:
     ```csharp
             void OnDestroy()
             {
                 if (IsReady && view != null && view.Editor != null)
                     view.Editor.CellChanged -= OnCellChanged;
                 if (Texture != null)
                     Destroy(Texture);
             }

             /// <summary>Ô (x, y, z) đổi → tính lại màu cột (x, z); đổi thì ghi vào texture, Apply một lần cuối Update (brief 0017).</summary>
             void OnCellChanged(int x, int y, int z)
             {
                 if (!MinimapImage.UpdateColumn(view.World, rgba, x, z))
                     return;
                 int i = (x + view.World.Size.X * z) * 4;
                 Texture.SetPixel(x, z, new Color32(rgba[i], rgba[i + 1], rgba[i + 2], 255));
                 pixelsDirty = true;
                 UpdatedPixelCount++;
             }
     ```
   - Trong `Update()`, ngay sau khối `if (!IsReady) { … Build(); }` (kết thúc ở `:83`), thêm:
     ```csharp
                 if (pixelsDirty)
                 {
                     Texture.Apply(false);
                     pixelsDirty = false;
                 }
     ```
   - Trong `Build()`: đổi `var rgba = MinimapImage.Build(view.World);` (`:263`) thành `rgba = MinimapImage.Build(view.World);`; ngay sau `Texture.Apply(false);` (`:275`) thêm `view.Editor.CellChanged += OnCellChanged;`.
   Xong khi: `refresh_unity` → `read_console` không lỗi.

7. **PlayMode test** — `client/Assets/Game/Tests/Client/MinimapViewPlayTests.cs`: thêm test sau vào cuối lớp, trước `}` đóng lớp:
   ```csharp

           [UnityTest]
           public IEnumerator DigUpdatesTexturePixel()
           {
               var w = view.World;
               int wl = view.Editor.SeaLevel;
               int fx = -1, fz = -1, top = -1;
               for (int z = 80; z < 160 && fx < 0; z++)
                   for (int x = 150; x < 234 && fx < 0; x++)
                   {
                       MinimapImage.ColumnInfo(w, x, z, out int s, out int d);
                       if (d == 0 && s % MinimapImage.TierHeight == 0 && s >= wl + 2 && Cell.IsDiggable(w.Get(x, s, z)))
                       {
                           fx = x;
                           fz = z;
                           top = s;
                       }
                   }
               Assert.GreaterOrEqual(fx, 0, "không có cột ở mép tầng gần camera");
               int idx = fx + w.Size.X * fz;
               var before = map.Texture.GetPixels32()[idx];
               Assert.AreEqual(EditResult.Ok, view.Dig(new Vector3Int(fx, top, fz)));
               yield return null;
               var after = map.Texture.GetPixels32()[idx];
               uint c = MinimapImage.ColumnColor(w, fx, fz);
               Assert.AreEqual(new Color32((byte)(c >> 16), (byte)(c >> 8), (byte)c, 255), after);
               Assert.AreNotEqual(before, after, "điểm ảnh phải đổi");
               Assert.GreaterOrEqual(map.UpdatedPixelCount, 1);
               CollectionAssert.AreEqual(MinimapImage.Build(w), ToBytes(map.Texture.GetPixels32()), "texture = ảnh dựng lại từ world đã sửa");
           }

           static byte[] ToBytes(Color32[] px)
           {
               var b = new byte[px.Length * 4];
               for (int i = 0; i < px.Length; i++)
               {
                   b[i * 4] = px[i].r;
                   b[i * 4 + 1] = px[i].g;
                   b[i * 4 + 2] = px[i].b;
                   b[i * 4 + 3] = px[i].a;
               }
               return b;
           }
   ```
   Xong khi: `run_tests` PlayMode lọc `MinimapViewPlayTests` → 9/9.

8. Chạy đủ "Lệnh executor chạy", dọn file Unity tự ghi lại, rồi commit code: `brief 0017: bản đồ cập nhật khi đào/đặt + lõi đặt dấu mốc`. Chạy `date "+%Y-%m-%d %H:%M"` → ghi "Kết thúc" vào Kết quả, đặt trạng thái `chờ review`, commit brief: `brief 0017: kết quả`.

## Tiêu chí xong
- Biên dịch không lỗi.
- EditMode cả bộ đạt: 301 test cũ + 10 test mới (8 `LandmarkPlacerTests`, 2 `MinimapEditTests`) = 311.
- PlayMode cả bộ đạt: 27 + 1 = 28.
- Hai file JSON giống từng byte; `ConfigTests` đạt.
- Mã băm world/bảng map v3 không đổi (`GoldenHashTests` + `DoesNotChangeWorldOrMapHashes`).
- `git status` sạch sau commit: không còn `EditorSettings.asset` hay file nào ngoài Phạm vi.

## Test sau khi xong

### Phần cần test
| Phần | Bước | Test | Kết quả mong đợi |
|---|---|---|---|
| Cấu hình mới đọc/kiểm được | 1–2 | `ConfigTests` (cả 3 test world_maps), `InvalidLandmarkConfigThrows` | JSON = mặc định code; 2 bản giống nhau; cấu hình sai bị ném lỗi |
| `UpdateColumn` | 3 | `DigAndPlaceChangeExactlyOnePixel`, `DigInsideTierKeepsPixel` | Đào ở mép tầng → đúng 1 điểm ảnh đổi, ảnh = dựng lại; đặt lại → như cũ; trong cùng tầng → không đổi |
| Đặt dấu mốc: số lượng, thứ tự, tên | 4 | `PlacesCheckpointPerMapThenStructuresInConfigOrder` (5 seed) | 3 checkpoint + 6 công trình, đúng thứ tự |
| Tất định | 4 | `IsDeterministic` | Hai lần đặt và world sinh lại cho cùng kết quả |
| Đúng map, đất khô phẳng, xa biên | 4 | `OnDryFlatLandDeepInsideOwnMap` (5 seed) | Đúng map; không nước; mặt ≥ mực nước; 8 láng giềng chênh ≤ 1; trong bán kính Manhattan 6 chỉ có cùng map |
| Khoảng cách giữa dấu mốc | 4 | `LandmarksAreSpacedApart` | Mọi cặp cách ≥ 20 ô |
| Checkpoint gần tâm map | 4 | `CheckpointIsNearMapCentroid` | ≤ 48 ô; ghi khoảng cách thật |
| Khoảng cách tới biên | 4 | `BorderDistanceIsZeroAtBordersAndGrowsInside` | Biên = 0; cột trong = min láng giềng + 1 |
| Không đổi world | 4 | `DoesNotChangeWorldOrMapHashes` | Mã băm world/bảng map = mã băm mẫu |
| Bản đồ đổi khi đào trong Play | 6–7 | PlayMode `DigUpdatesTexturePixel` | Điểm ảnh đổi đúng màu; texture = ảnh dựng lại |
| Regression bản đồ 0016 | 6 | 8 test cũ của `MinimapViewPlayTests` | Đạt |
| Regression chung | — | EditMode cả bộ (bộ sinh, `GoldenHashTests`, `MinimapWorldTests`, sim…) + PlayMode cả bộ (đào/đặt, nạp dần, camera, SimView) | Đạt |
| Thao tác thật | — | Mục "User tự test" | Chờ user |

### Lệnh executor chạy
1. MCP `refresh_unity` → `read_console` (types error) — 0 lỗi biên dịch.
2. `cmp design/data/world_maps_m1.json client/Assets/Game/Client/Resources/Config/world_maps_m1.json` → không in gì.
3. MCP `run_tests` EditMode lọc `LandmarkPlacerTests`, `MinimapEditTests`, `ConfigTests` → đạt; đọc console, lấy các dòng `LANDMARKS` / `CHECKPOINT`.
4. MCP `run_tests` EditMode (cả bộ) → ghi dòng tổng kết.
5. MCP `run_tests` PlayMode (cả bộ) → ghi dòng tổng kết. Sau đó dọn: `git checkout -- client/ProjectSettings/EditorSettings.asset`.
6. `git status --short` → chỉ còn file trong Phạm vi; commit (bước 8).
7. `date "+%Y-%m-%d %H:%M"` → ghi "Kết thúc"; commit brief.

### User tự test
Mở scene TerrainView trên Mac, bấm Play:
1. Bay tới một sườn đồi, đào liên tục 4–5 ô xuống ở cùng một chỗ.
2. Bấm M mở bản đồ, phóng to tới chỗ vừa đào: điểm ảnh chỗ đó đã đổi sang màu tầng thấp hơn. Nếu hố thông ra biển và nước tràn vào thì chỗ đó chuyển sang màu nước.
3. Đóng bản đồ, đặt khối lấp lại hố rồi mở bản đồ: điểm ảnh về màu cũ.

(Dấu checkpoint và công trình chưa hiện ở brief này; hiển thị làm ở brief 0018.)

## Dừng và chuyển lại Opus khi
- Brief mơ hồ hoặc mâu thuẫn với code thật (vd số dòng không khớp nội dung trích).
- Một seed ném `InvalidOperationException` (hết chỗ đặt), hoặc `CheckpointIsNearMapCentroid` hỏng. Không được tự chỉnh số trong JSON.
- Cần một quyết định thuộc AGENTS.md §3.4 hoặc ngoài "Quyết định đã chốt", hoặc cần API Unity ngoài danh sách.
- Cùng một lỗi sửa 2 lần vẫn chưa qua.
- Phải sửa file ngoài "Phạm vi".

---
<!-- Từ đây trở xuống: EXECUTOR ghi. -->

## Kết quả
- Bắt đầu: 2026-10-20 23:45
- Base commit: 5d4366d
- Chạy bằng: claude-sonnet-5-5 · medium
- Kết thúc: 2026-10-20 23:51
- Commit code: fa0d8b9 — sửa WorldMapParams.cs, MinimapImage.cs, MinimapView.cs, MinimapViewPlayTests.cs, 2 file JSON; tạo LandmarkPlacer.cs, LandmarkPlacerTests.cs, MinimapEditTests.cs (+ .meta)
- Lệch brief nhỏ: Unity đang ở Play Mode lúc chạy test → dừng Play bằng manage_editor.

## Kết quả test
- Biên dịch: read_console error = 0.
- JSON: `cmp` hai file không in gì; json.load không lỗi → đạt.
- EditMode lọc (LandmarkPlacerTests + MinimapEditTests + ConfigTests): 17/17 đạt → đạt các dòng: cấu hình, UpdateColumn, số lượng/thứ tự, tất định, đúng map/khô phẳng/xa biên, khoảng cách, checkpoint gần tâm, BorderDistance, không đổi mã băm, cấu hình sai bị ném.
- EditMode cả bộ: 311/311 đạt. PlayMode cả bộ: 28/28 đạt (gồm `DigUpdatesTexturePixel` + 8 test cũ MinimapViewPlayTests).
- Dọn: `git checkout -- client/ProjectSettings/EditorSettings.asset`; git status sạch trước commit brief.
- Seed 20261013 (console):
  - CHECKPOINT Map 1 (123,211) y=18, cách trọng tâm 0,5 ô; Map 2 (210,119) y=27, 0,1 ô; Map 3 (243,238) y=14, 1,3 ô.
  - LANDMARKS ms=26,0: Công trình 1A (139,269) y=27; 1B (86,171) y=14; 2A (201,143) y=19; 2B (176,113) y=27; 3A (223,201) y=25; 3B (192,202) y=28.
- Thao tác thật (mục "User tự test"): chờ user.

## Vướng mắc

---
<!-- PLANNER ghi khi review. -->

## Review
- Chạy bằng `claude-sonnet-5-5 · medium` khớp Model thực thi.
- Mọi dòng "Phần cần test" có kết quả; EditMode 311/311, PlayMode 28/28, `cmp` JSON đạt (theo executor, không đáng ngờ → không chạy lại).
- Diff `5d4366d..fa0d8b9`: chỉ file trong Phạm vi. `LandmarkPlacer` đúng quyết định 2–7 (ứng viên 4 điều kiện, BFS biên, checkpoint so `long` lấy cái gặp trước, spacing tính cả checkpoint, tên `" k"` khi count > 1, hết chỗ ném lỗi, thứ tự kết quả). `UpdateColumn` + `MinimapView` (`SetPixel` khi màu đổi, `Apply(false)` một lần/khung, huỷ đăng ký trong `OnDestroy`) đúng quyết định 1.
- `Validate` thêm ràng buộc `landmarkBorderMargin ≥ 2`, `count ≤ 8` — hợp lý, không trái brief.
- User tự test trên Mac (đào hố → bản đồ đổi màu, lấp lại → về cũ): "ổn" [user 2026-10-21].
- Giờ 7d phần 1 đã ghi vào roadmap.
- Kết luận: đã duyệt
