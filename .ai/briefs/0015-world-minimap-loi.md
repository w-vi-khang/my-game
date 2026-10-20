# Brief 0015 — World 7c (phần 1/2): lõi minimap — ảnh nền tầng độ cao, ranh giới, vị trí tên, phóng to/kéo

> Trạng thái: chờ làm
> Review: đầy đủ + user xem
> Model thực thi: Sonnet · medium
> Cập nhật: 2026-10-20 22:41

<!-- Planner (Opus) viết, Executor (Sonnet) làm theo ở PHIÊN KHÁC — executor không có lịch sử chat, không được tự quyết.
Trạng thái: chờ làm → đang làm → chờ review → đã duyệt; hoặc → cần Opus (executor dừng, hỏi ở mục Vướng mắc).
File được commit. -->

## Mục tiêu
Task [world bước 7](../tasks/m1-world-buoc-7-nhieu-map-minimap.md), dòng **7c** của [world-tech](../../design/04-tech/world-tech.md#kế-hoạch-prototype-m1), theo [0022](../../design/decisions/0022-world-nhieu-map-va-minimap-m1.md) mục 5. Chia làm 2 brief: **0015 (brief này) = phần lõi trong `Game.Core`, có test EditMode, không có UI**; 0016 (viết sau khi 0015 được duyệt) = giao diện uGUI, phím M, chuột, chụm 2 ngón, chạy trên điện thoại.
Làm 3 lớp C# thuần: `MinimapImage` (ảnh nền RGBA, mỗi cột 1 điểm ảnh, màu theo tầng độ cao hoặc độ sâu nước), `MinimapOverlay` (đường ranh giới giữa các map dạng vector đã làm mượt + vị trí đặt tên map), `MinimapViewport` (phép phóng to/kéo/chụm 2 ngón). Menu xem trước `Game/Ảnh xem trước world nhiều map` đổi sang dùng màu của `MinimapImage` để user xem bảng màu trước khi làm UI.

## Bối cảnh cần đọc
- `client/Assets/Game/Core/WorldData.cs`:
  - `WorldSize` `:6-52`: `public int Index(int x, int y, int z) => x + X * (z + Z * y);` (`:31`), các trường `X, Y, Z` (`:10`).
  - `WorldData` `:55-94`: `public ushort[] Cells => cells;` (`:75`), `public ushort Get(int x, int y, int z)` (`:77`), `public void Set(int x, int y, int z, ushort cell)` (`:79`), `public BlockMaterial GetMaterial(int x, int y, int z)` (`:81`), `public int SurfaceHeight(int x, int z)` (`:84-93`, ô rắn cao nhất, không tính nước; -1 nếu cột rỗng). Constructor `public WorldData(WorldSize size)` (`:60`) tạo world toàn ô rỗng (0 = `Empty`).
- `client/Assets/Game/Core/Cell.cs`: `enum BlockMaterial : byte { Empty = 0, Grass = 1, Dirt = 2, Stone = 3, Sand = 4, Water = 5, Bedrock = 6 }` (`:4-14`); `public const int MaxWaterLevel = 7;` (`:22`); `public static ushort Make(BlockMaterial material, int waterLevel = 0)` (`:32`); `public static BlockMaterial GetMaterial(ushort cell)` (`:37`).
- `client/Assets/Game/Core/MapTable.cs`: `public const byte Ocean = 255;` (`:9`); `public readonly int SizeX, SizeZ;` (`:11`); constructor `public MapTable(int sizeX, int sizeZ, byte[] ids, string[] names)` (`:15`, ids dài đúng sizeX·sizeZ, chỉ số `x + SizeX * z`); `public int MapCount => names.Length;` (`:28`); `public byte Get(int x, int z)` (`:30`).
- `client/Assets/Game/Core/Game.Core.asmdef`: `"noEngineReferences": true`, không tham chiếu gì → 3 lớp mới chỉ dùng `System`, `System.Collections.Generic`.
- `client/Assets/Game/World/WorldMapGenerator.cs:16`: `public static WorldData Generate(WorldMapParams p, out MapTable maps)`. `WorldMapParams` mặc định `seed = 20261013` (`client/Assets/Game/Core/WorldMapParams.cs:13`), world 384 × 48 × 384, mặt nước `waterLevel = 12` (`design/data/world_maps_m1.json`).
- Mẫu test dùng world thật có cache: `client/Assets/Game/Tests/World/WorldMapGeneratorTests.cs:12-30` (mảng `Seeds`, `Cache`, `Get(seed)`, `[OneTimeTearDown] ClearCache`). Test lõi thuần ở `client/Assets/Game/Tests/Core/` (namespace `Game.Core.Tests`, asmdef `Game.Core.Tests` chỉ tham chiếu `Game.Core`); test cần bộ sinh ở `client/Assets/Game/Tests/World/` (namespace `Game.World.Tests`).
- `client/Assets/Game/Client/Editor/WorldMapPreview.cs` (công cụ Editor, brief 0009): mảng `LandTiers` `:18-29`; khối chọn màu `:50-64` — nhánh ranh giới (`:51-52`, giữ nguyên), nhánh nước `:53-58` và nhánh đất `:59-63` (thay ở bước 5).
- Không dùng API Unity mới nào (lõi thuần C#; công cụ Editor chỉ dùng lại `Color32` đã có).

## Quyết định đã chốt (không được đổi)
1. **Ảnh nền** đọc thẳng từ `WorldData` đã sinh (không dựng voxel, không lưu): mỗi cột quét từ trên xuống — `surface` = ô rắn cao nhất (mọi vật liệu khác `Empty`, `Water`); `waterDepth` = (ô `Water` cao nhất phía trên `surface`) − `surface`, 0 nếu không có nước. Có nước → màu nước theo độ sâu; không có → màu đất theo tầng.
2. **Tầng đất:** tầng = `max(surface, 0) / 4` (mỗi 4 ô độ cao một tầng, 12 tầng 0–11 với world cao 48), vượt bảng thì dùng màu cuối. **Dải nước:** `(max(depth − 1, 0)) / 2` chặn ở 3 → 4 dải: sâu 1–2, 3–4, 5–6, ≥ 7. Bảng màu (0xRRGGBB) chép đúng ở bước 1; user xem ở review và có thể đổi sau.
3. **Định dạng ảnh:** `byte[]` RGBA32, 4 byte (R, G, B, A = 255) mỗi cột, cột (x, z) ở byte `(x + Size.X · z) · 4` — x chạy nhanh nhất, hàng z = 0 trước (khớp `Texture2D.SetPixels32`: "starting at the bottom left" [docs](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/Texture2D.SetPixels32.html) — brief 0016 dùng).
4. **Ranh giới** theo toạ độ góc ô (cột (x, z) chiếm [x, x + 1] × [z, z + 1]): đoạn đơn vị giữa hai cột kề 4 hướng thuộc 2 map khác nhau, **cả hai không phải biển** (đất giáp biển không vẽ ranh giới — màu nước đã cho thấy bờ). Nối đoạn thành đường gấp khúc giữa các nút bậc ≠ 2 (chỗ 3 map gặp, chỗ ranh giới chạm biển/biên world); vòng kín đóng bằng điểm đầu. Làm mượt **Chaikin 2 lần** (¼–¾), đường hở giữ nguyên điểm đầu/cuối → các đường vẫn gặp nhau đúng ở nút. Thứ tự duyệt cố định (bước 2) → tất định.
5. **Vị trí tên map:** trọng tâm các cột của map (tâm cột = x + 0,5); nếu cột chứa trọng tâm không thuộc map đó → tâm cột gần trọng tâm nhất (bằng nhau → cột gặp trước khi duyệt z rồi x). Map không có cột → NaN.
6. **Vùng nhìn:** zoom 1 = cả world vừa khung (lấy tỉ lệ nhỏ hơn của 2 trục), zoom trong [1, 8]; phóng quanh điểm con trỏ (điểm world dưới con trỏ giữ chỗ); kéo theo pixel; chụm 2 ngón = kéo theo trung điểm rồi phóng theo tỉ lệ khoảng cách quanh trung điểm mới; luôn chặn: trục nào vùng nhìn ≥ world thì căn giữa, ngược lại vùng nhìn nằm trong [0, kích thước world]. Toạ độ khung: pixel, gốc dưới-trái, y lên; z của world lên trên màn hình.
7. Không sửa `WorldData`, `MapTable`, bộ sinh, `design/`; không thêm package/asmdef. Cập nhật minimap khi đào/đặt là bước 7d (không làm ở đây).
8. Giờ bắt đầu/kết thúc lấy bằng lệnh `date "+%Y-%m-%d %H:%M"` lúc đó, không ước đoán.

## Phạm vi
- Được tạo: `client/Assets/Game/Core/MinimapImage.cs`, `client/Assets/Game/Core/MinimapOverlay.cs`, `client/Assets/Game/Core/MinimapViewport.cs`, `client/Assets/Game/Tests/Core/MinimapImageTests.cs`, `client/Assets/Game/Tests/Core/MinimapOverlayTests.cs`, `client/Assets/Game/Tests/Core/MinimapViewportTests.cs`, `client/Assets/Game/Tests/World/MinimapWorldTests.cs` (+ file `.meta` Unity tự tạo cho 7 file này).
- Được sửa: `client/Assets/Game/Client/Editor/WorldMapPreview.cs` (chỉ bước 5); mục Kết quả / Kết quả test / Vướng mắc của brief này.
- Không được sửa: mọi file khác (kể cả `WorldData.cs`, `MapTable.cs`, `WorldMapGenerator.cs`, scene, asmdef, `design/`).

## Các bước
0. Chạy `date "+%Y-%m-%d %H:%M"` → ghi "Bắt đầu" vào Kết quả; ghi base commit (`git rev-parse --short HEAD`).

1. **`client/Assets/Game/Core/MinimapImage.cs`** — tạo file đúng nội dung:
   ```csharp
   using System;

   namespace Game.Core
   {
       /// <summary>
       /// Ảnh nền minimap (world-tech bước 7c, brief 0015): 1 điểm ảnh = 1 cột (x, z). Cột có nước phía trên mặt đất → màu theo độ sâu nước;
       /// không có → màu theo tầng độ cao (mỗi TierHeight ô một tầng). Đọc thẳng WorldData, không dựng voxel → tạo lại được từ seed, không lưu.
       /// </summary>
       public static class MinimapImage
       {
           /// <summary>Số ô độ cao mỗi tầng màu.</summary>
           public const int TierHeight = 4;

           /// <summary>Màu tầng đất 0xRRGGBB; tầng = max(mặt đất, 0) / TierHeight (0–11 với world cao 48); tầng vượt bảng dùng màu cuối.</summary>
           public static readonly uint[] LandTiers =
           {
               0x6F8F55, // 0: 0–3
               0x7A9C5B, // 1: 4–7
               0x86A862, // 2: 8–11
               0xA8D47A, // 3: 12–15 (ngay trên mặt biển)
               0x93C66A, // 4: 16–19
               0x7DB65B, // 5: 20–23
               0x9DB15E, // 6: 24–27
               0xBFB46A, // 7: 28–31
               0xC9A26A, // 8: 32–35
               0xAE8A63, // 9: 36–39
               0x9C928A, // 10: 40–43
               0xEDEDED, // 11: 44–47
           };

           /// <summary>Màu nước 0xRRGGBB theo độ sâu (ô): 1–2, 3–4, 5–6, ≥ 7.</summary>
           public static readonly uint[] WaterBands = { 0x8FD3EC, 0x5DB0DE, 0x3486C4, 0x1F5799 };

           public static uint LandColor(int surface) =>
               LandTiers[Math.Min(Math.Max(surface, 0) / TierHeight, LandTiers.Length - 1)];

           public static uint WaterColor(int depth) =>
               WaterBands[Math.Min(Math.Max(depth - 1, 0) / 2, WaterBands.Length - 1)];

           /// <summary>
           /// Quét cột từ trên xuống: surface = ô rắn cao nhất (không tính nước; -1 nếu cột rỗng);
           /// waterDepth = ô nước cao nhất phía trên surface − surface (0 nếu không có nước phía trên).
           /// </summary>
           public static void ColumnInfo(WorldData world, int x, int z, out int surface, out int waterDepth)
           {
               var s = world.Size;
               var cells = world.Cells;
               int waterTop = -1;
               surface = -1;
               for (int y = s.Y - 1; y >= 0; y--)
               {
                   var m = Cell.GetMaterial(cells[s.Index(x, y, z)]);
                   if (m == BlockMaterial.Empty)
                       continue;
                   if (m == BlockMaterial.Water)
                   {
                       if (waterTop < 0)
                           waterTop = y;
                       continue;
                   }
                   surface = y;
                   break;
               }
               waterDepth = waterTop >= 0 ? waterTop - surface : 0;
           }

           /// <summary>Màu 0xRRGGBB của cột (x, z).</summary>
           public static uint ColumnColor(WorldData world, int x, int z)
           {
               ColumnInfo(world, x, z, out int surface, out int waterDepth);
               return waterDepth > 0 ? WaterColor(waterDepth) : LandColor(surface);
           }

           /// <summary>
           /// Ảnh RGBA32 cả world: 4 byte (R, G, B, A = 255) mỗi cột, cột (x, z) ở byte (x + Size.X · z) · 4 —
           /// x chạy nhanh nhất, hàng z = 0 trước (khớp Texture2D.SetPixels32: hàng dưới cùng trước).
           /// </summary>
           public static byte[] Build(WorldData world)
           {
               var s = world.Size;
               var rgba = new byte[s.X * s.Z * 4];
               for (int z = 0; z < s.Z; z++)
                   for (int x = 0; x < s.X; x++)
                   {
                       uint c = ColumnColor(world, x, z);
                       int i = (x + s.X * z) * 4;
                       rgba[i] = (byte)(c >> 16);
                       rgba[i + 1] = (byte)(c >> 8);
                       rgba[i + 2] = (byte)c;
                       rgba[i + 3] = 255;
                   }
               return rgba;
           }
       }
   }
   ```
   Xong khi: biên dịch không lỗi (kiểm ở bước 6).

2. **`client/Assets/Game/Core/MinimapOverlay.cs`** — tạo file đúng nội dung:
   ```csharp
   using System;
   using System.Collections.Generic;

   namespace Game.Core
   {
       /// <summary>
       /// Lớp vẽ đè lên ảnh nền minimap (world-tech bước 7c, brief 0015): đường ranh giới giữa các map và vị trí tên map.
       /// Toạ độ theo góc ô: cột (x, z) chiếm [x, x + 1] × [z, z + 1].
       /// </summary>
       public static class MinimapOverlay
       {
           /// <summary>Số lần làm mượt Chaikin mặc định của ranh giới.</summary>
           public const int SmoothIterations = 2;

           /// <summary>
           /// Ranh giới: mỗi đoạn đơn vị nằm giữa hai cột kề (4 hướng) thuộc hai map khác nhau, cả hai không phải biển.
           /// Nối đoạn thành đường gấp khúc giữa các nút bậc ≠ 2 (chỗ 3 map gặp nhau, chỗ ranh giới chạm biển hoặc biên world);
           /// vòng kín (mọi nút bậc 2) lặp lại điểm đầu ở cuối. Mỗi đường là mảng [x0, z0, x1, z1, …], làm mượt Chaikin
           /// smoothIterations lần (đường hở giữ nguyên điểm đầu/cuối).
           /// </summary>
           public static List<float[]> Borders(MapTable maps, int smoothIterations = SmoothIterations)
           {
               int sx = maps.SizeX, sz = maps.SizeZ, nx = sx + 1;
               var edgeA = new List<int>();
               var edgeB = new List<int>();
               // Cạnh dọc giữa cột (x, z) và (x + 1, z): góc (x + 1, z) – (x + 1, z + 1).
               for (int z = 0; z < sz; z++)
                   for (int x = 0; x + 1 < sx; x++)
                       if (Separates(maps.Get(x, z), maps.Get(x + 1, z)))
                       {
                           edgeA.Add(x + 1 + nx * z);
                           edgeB.Add(x + 1 + nx * (z + 1));
                       }
               // Cạnh ngang giữa cột (x, z) và (x, z + 1): góc (x, z + 1) – (x + 1, z + 1).
               for (int z = 0; z + 1 < sz; z++)
                   for (int x = 0; x < sx; x++)
                       if (Separates(maps.Get(x, z), maps.Get(x, z + 1)))
                       {
                           edgeA.Add(x + nx * (z + 1));
                           edgeB.Add(x + 1 + nx * (z + 1));
                       }

               // Nút = góc ô (cx + nx · cz); mỗi nút có tối đa 4 cạnh, theo thứ tự tạo cạnh.
               int nodeCount = nx * (sz + 1);
               var degree = new int[nodeCount];
               var incident = new int[nodeCount * 4];
               for (int e = 0; e < edgeA.Count; e++)
               {
                   incident[edgeA[e] * 4 + degree[edgeA[e]]++] = e;
                   incident[edgeB[e] * 4 + degree[edgeB[e]]++] = e;
               }

               var used = new bool[edgeA.Count];
               var lines = new List<float[]>();
               var pts = new List<int>();
               // Đường hở: bắt đầu ở nút bậc ≠ 2 theo chỉ số nút tăng dần, mỗi cạnh chưa dùng của nút đó theo thứ tự.
               for (int node = 0; node < nodeCount; node++)
               {
                   if (degree[node] == 0 || degree[node] == 2)
                       continue;
                   for (int k = 0; k < degree[node]; k++)
                   {
                       int e = incident[node * 4 + k];
                       if (used[e])
                           continue;
                       pts.Clear();
                       pts.Add(node);
                       int cur = node;
                       while (true)
                       {
                           used[e] = true;
                           cur = edgeA[e] == cur ? edgeB[e] : edgeA[e];
                           pts.Add(cur);
                           if (degree[cur] != 2)
                               break;
                           int next = incident[cur * 4] == e ? incident[cur * 4 + 1] : incident[cur * 4];
                           if (used[next])
                               break;
                           e = next;
                       }
                       lines.Add(Smooth(ToCoords(pts, nx), false, smoothIterations));
                   }
               }
               // Vòng kín còn lại: bắt đầu ở đầu A của cạnh chưa dùng đầu tiên.
               for (int e0 = 0; e0 < edgeA.Count; e0++)
               {
                   if (used[e0])
                       continue;
                   int start = edgeA[e0], cur = start, e = e0;
                   pts.Clear();
                   pts.Add(start);
                   while (true)
                   {
                       used[e] = true;
                       cur = edgeA[e] == cur ? edgeB[e] : edgeA[e];
                       pts.Add(cur);
                       if (cur == start)
                           break;
                       int next = incident[cur * 4] == e ? incident[cur * 4 + 1] : incident[cur * 4];
                       if (used[next])
                           break;
                       e = next;
                   }
                   lines.Add(Smooth(ToCoords(pts, nx), true, smoothIterations));
               }
               return lines;
           }

           /// <summary>
           /// Vị trí đặt tên mỗi map [x0, z0, x1, z1, …] (toạ độ góc ô): trọng tâm các cột của map (tâm cột = x + 0,5);
           /// cột chứa trọng tâm không thuộc map đó → tâm cột của map gần trọng tâm nhất (bằng nhau → cột gặp trước theo z rồi x).
           /// Map không có cột → NaN.
           /// </summary>
           public static float[] LabelPositions(MapTable maps)
           {
               int count = maps.MapCount;
               var sumX = new double[count];
               var sumZ = new double[count];
               var n = new long[count];
               for (int z = 0; z < maps.SizeZ; z++)
                   for (int x = 0; x < maps.SizeX; x++)
                   {
                       int id = maps.Get(x, z);
                       if (id == MapTable.Ocean || id >= count)
                           continue;
                       sumX[id] += x + 0.5;
                       sumZ[id] += z + 0.5;
                       n[id]++;
                   }

               var pos = new float[count * 2];
               for (int m = 0; m < count; m++)
               {
                   if (n[m] == 0)
                   {
                       pos[2 * m] = pos[2 * m + 1] = float.NaN;
                       continue;
                   }
                   double cx = sumX[m] / n[m], cz = sumZ[m] / n[m];
                   int ix = (int)Math.Floor(cx), iz = (int)Math.Floor(cz);
                   if (ix >= 0 && iz >= 0 && ix < maps.SizeX && iz < maps.SizeZ && maps.Get(ix, iz) == m)
                   {
                       pos[2 * m] = (float)cx;
                       pos[2 * m + 1] = (float)cz;
                       continue;
                   }
                   double best = double.MaxValue;
                   int bx = 0, bz = 0;
                   for (int z = 0; z < maps.SizeZ; z++)
                       for (int x = 0; x < maps.SizeX; x++)
                       {
                           if (maps.Get(x, z) != m)
                               continue;
                           double dx = x + 0.5 - cx, dz = z + 0.5 - cz, d = dx * dx + dz * dz;
                           if (d < best)
                           {
                               best = d;
                               bx = x;
                               bz = z;
                           }
                       }
                   pos[2 * m] = bx + 0.5f;
                   pos[2 * m + 1] = bz + 0.5f;
               }
               return pos;
           }

           static bool Separates(byte a, byte b) => a != b && a != MapTable.Ocean && b != MapTable.Ocean;

           static float[] ToCoords(List<int> nodes, int nx)
           {
               var p = new float[nodes.Count * 2];
               for (int i = 0; i < nodes.Count; i++)
               {
                   p[2 * i] = nodes[i] % nx;
                   p[2 * i + 1] = nodes[i] / nx;
               }
               return p;
           }

           /// <summary>Chaikin: mỗi đoạn A→B thay bằng 2 điểm ¾A + ¼B và ¼A + ¾B. Đường hở giữ điểm đầu/cuối; vòng kín lặp điểm đầu ở cuối.</summary>
           static float[] Smooth(float[] p, bool closed, int iterations)
           {
               for (int it = 0; it < iterations; it++)
               {
                   int n = p.Length / 2;
                   if (n < 3)
                       break;
                   var q = new List<float>(4 * n);
                   if (!closed)
                   {
                       q.Add(p[0]);
                       q.Add(p[1]);
                   }
                   for (int i = 0; i + 1 < n; i++)
                   {
                       float ax = p[2 * i], az = p[2 * i + 1], bx = p[2 * i + 2], bz = p[2 * i + 3];
                       q.Add(0.75f * ax + 0.25f * bx);
                       q.Add(0.75f * az + 0.25f * bz);
                       q.Add(0.25f * ax + 0.75f * bx);
                       q.Add(0.25f * az + 0.75f * bz);
                   }
                   if (closed)
                   {
                       q.Add(q[0]);
                       q.Add(q[1]);
                   }
                   else
                   {
                       q.Add(p[2 * n - 2]);
                       q.Add(p[2 * n - 1]);
                   }
                   p = q.ToArray();
               }
               return p;
           }
       }
   }
   ```
   Xong khi: biên dịch không lỗi.

3. **`client/Assets/Game/Core/MinimapViewport.cs`** — tạo file đúng nội dung:
   ```csharp
   using System;

   namespace Game.Core
   {
       /// <summary>
       /// Vùng nhìn của minimap (world-tech bước 7c, brief 0015): phóng to/kéo/chụm 2 ngón trên ảnh nền.
       /// Toạ độ khung: pixel, gốc góc dưới-trái, x sang phải, y lên trên. Toạ độ world: cột (x, z), z lên trên màn hình.
       /// Zoom = 1 → cả world vừa khung (tỉ lệ nhỏ hơn của 2 trục); zoom trong [1, MaxZoom].
       /// </summary>
       public sealed class MinimapViewport
       {
           public const float MaxZoom = 8f;

           public readonly float WorldWidth, WorldHeight;
           public float PanelWidth { get; private set; }
           public float PanelHeight { get; private set; }
           public float Zoom { get; private set; } = 1f;
           /// <summary>Điểm world ở giữa khung.</summary>
           public float CenterX { get; private set; }
           public float CenterZ { get; private set; }

           public MinimapViewport(float worldWidth, float worldHeight, float panelWidth, float panelHeight)
           {
               if (!(worldWidth > 0f) || !(worldHeight > 0f))
                   throw new ArgumentException("Kích thước world phải dương");
               WorldWidth = worldWidth;
               WorldHeight = worldHeight;
               CenterX = worldWidth * 0.5f;
               CenterZ = worldHeight * 0.5f;
               SetPanelSize(panelWidth, panelHeight);
           }

           /// <summary>Số pixel khung cho mỗi cột world.</summary>
           public float PixelsPerColumn => Math.Min(PanelWidth / WorldWidth, PanelHeight / WorldHeight) * Zoom;

           /// <summary>Đổi kích thước khung (vd xoay màn hình), giữ zoom và tâm rồi chặn lại.</summary>
           public void SetPanelSize(float width, float height)
           {
               if (!(width > 0f) || !(height > 0f))
                   throw new ArgumentException("Kích thước khung phải dương");
               PanelWidth = width;
               PanelHeight = height;
               Clamp();
           }

           /// <summary>Về zoom 1, tâm world.</summary>
           public void Reset()
           {
               Zoom = 1f;
               CenterX = WorldWidth * 0.5f;
               CenterZ = WorldHeight * 0.5f;
               Clamp();
           }

           public void WorldToPanel(float wx, float wz, out float px, out float py)
           {
               float k = PixelsPerColumn;
               px = PanelWidth * 0.5f + (wx - CenterX) * k;
               py = PanelHeight * 0.5f + (wz - CenterZ) * k;
           }

           public void PanelToWorld(float px, float py, out float wx, out float wz)
           {
               float k = PixelsPerColumn;
               wx = CenterX + (px - PanelWidth * 0.5f) / k;
               wz = CenterZ + (py - PanelHeight * 0.5f) / k;
           }

           /// <summary>Phóng zoom × factor quanh điểm khung (px, py): điểm world dưới (px, py) giữ nguyên chỗ, trừ khi bị chặn ở mép world.</summary>
           public void ZoomAt(float px, float py, float factor)
           {
               if (!(factor > 0f))
                   return;
               PanelToWorld(px, py, out float wx, out float wz);
               Zoom = Math.Min(Math.Max(Zoom * factor, 1f), MaxZoom);
               float k = PixelsPerColumn;
               CenterX = wx - (px - PanelWidth * 0.5f) / k;
               CenterZ = wz - (py - PanelHeight * 0.5f) / k;
               Clamp();
           }

           /// <summary>Kéo theo (dx, dy) pixel khung: kéo sang phải → nội dung đi sang phải.</summary>
           public void Pan(float dx, float dy)
           {
               float k = PixelsPerColumn;
               CenterX -= dx / k;
               CenterZ -= dy / k;
               Clamp();
           }

           /// <summary>
           /// Chụm 2 ngón: ngón A từ (a0x, a0y) tới (a1x, a1y), ngón B từ (b0x, b0y) tới (b1x, b1y) (pixel khung) →
           /// kéo theo trung điểm, rồi phóng quanh trung điểm mới theo tỉ lệ khoảng cách mới / cũ (bỏ phóng khi khoảng cách cũ &lt; 1 pixel).
           /// </summary>
           public void Pinch(float a0x, float a0y, float b0x, float b0y, float a1x, float a1y, float b1x, float b1y)
           {
               float m0x = (a0x + b0x) * 0.5f, m0y = (a0y + b0y) * 0.5f;
               float m1x = (a1x + b1x) * 0.5f, m1y = (a1y + b1y) * 0.5f;
               Pan(m1x - m0x, m1y - m0y);
               float d0 = Distance(a0x, a0y, b0x, b0y), d1 = Distance(a1x, a1y, b1x, b1y);
               if (d0 >= 1f)
                   ZoomAt(m1x, m1y, d1 / d0);
           }

           /// <summary>
           /// Vùng ảnh nền hiện trong khung, theo phần của world (u, v = góc dưới-trái; w, h = bề rộng, bề cao) — dùng làm RawImage.uvRect.
           /// Có thể ra ngoài 0..1 khi khung rộng hơn world (zoom 1, khung không vuông).
           /// </summary>
           public void VisibleUv(out float u, out float v, out float w, out float h)
           {
               float k = PixelsPerColumn;
               float hw = PanelWidth * 0.5f / k, hh = PanelHeight * 0.5f / k;
               u = (CenterX - hw) / WorldWidth;
               v = (CenterZ - hh) / WorldHeight;
               w = 2f * hw / WorldWidth;
               h = 2f * hh / WorldHeight;
           }

           void Clamp()
           {
               float k = PixelsPerColumn;
               CenterX = ClampAxis(CenterX, PanelWidth * 0.5f / k, WorldWidth);
               CenterZ = ClampAxis(CenterZ, PanelHeight * 0.5f / k, WorldHeight);
           }

           /// <summary>Vùng nhìn ≥ world theo trục này → căn giữa; ngược lại giữ vùng nhìn trong [0, size].</summary>
           static float ClampAxis(float center, float half, float size) =>
               2f * half >= size ? size * 0.5f : Math.Min(Math.Max(center, half), size - half);

           static float Distance(float ax, float ay, float bx, float by)
           {
               float dx = bx - ax, dy = by - ay;
               return (float)Math.Sqrt(dx * dx + dy * dy);
           }
       }
   }
   ```
   Xong khi: biên dịch không lỗi.

4. **Test** — tạo 4 file test đúng nội dung dưới đây.

   a. `client/Assets/Game/Tests/Core/MinimapImageTests.cs`:
   ```csharp
   using System.Collections.Generic;
   using NUnit.Framework;

   namespace Game.Core.Tests
   {
       /// <summary>Ảnh nền minimap trên world dựng tay (brief 0015).</summary>
       public class MinimapImageTests
       {
           static void Fill(WorldData w, int x, int z, int fromY, int toY, BlockMaterial m)
           {
               for (int y = fromY; y <= toY; y++)
                   w.Set(x, y, z, Cell.Make(m, m == BlockMaterial.Water ? Cell.MaxWaterLevel : 0));
           }

           static uint Pixel(byte[] img, WorldData w, int x, int z)
           {
               int i = (x + w.Size.X * z) * 4;
               Assert.AreEqual(255, img[i + 3], $"alpha cột ({x}, {z})");
               return (uint)(img[i] << 16 | img[i + 1] << 8 | img[i + 2]);
           }

           [Test]
           public void PalettesHaveDistinctColors()
           {
               Assert.AreEqual(12, MinimapImage.LandTiers.Length);
               Assert.AreEqual(4, MinimapImage.WaterBands.Length);
               var all = new HashSet<uint>();
               foreach (var c in MinimapImage.LandTiers)
                   Assert.IsTrue(c <= 0xFFFFFF && all.Add(c), $"màu đất {c:x6} trùng hoặc sai");
               foreach (var c in MinimapImage.WaterBands)
                   Assert.IsTrue(c <= 0xFFFFFF && all.Add(c), $"màu nước {c:x6} trùng hoặc sai");
           }

           [Test]
           public void TierAndBandBoundaries()
           {
               var t = MinimapImage.LandTiers;
               var b = MinimapImage.WaterBands;
               Assert.AreEqual(t[0], MinimapImage.LandColor(-1));
               Assert.AreEqual(t[3], MinimapImage.LandColor(15));
               Assert.AreEqual(t[4], MinimapImage.LandColor(16));
               Assert.AreEqual(t[11], MinimapImage.LandColor(47));
               Assert.AreEqual(t[11], MinimapImage.LandColor(60));
               Assert.AreEqual(b[0], MinimapImage.WaterColor(0));
               Assert.AreEqual(b[0], MinimapImage.WaterColor(2));
               Assert.AreEqual(b[1], MinimapImage.WaterColor(3));
               Assert.AreEqual(b[2], MinimapImage.WaterColor(6));
               Assert.AreEqual(b[3], MinimapImage.WaterColor(7));
               Assert.AreEqual(b[3], MinimapImage.WaterColor(30));
           }

           [Test]
           public void ColumnColorFollowsTiersAndWaterDepth()
           {
               var w = new WorldData(new WorldSize(16, 48, 16));
               Fill(w, 0, 0, 0, 20, BlockMaterial.Stone);                                                  // mặt 20 → tầng 5
               Fill(w, 1, 0, 0, 10, BlockMaterial.Stone); Fill(w, 1, 0, 11, 14, BlockMaterial.Water);     // sâu 4 → dải 1
               Fill(w, 2, 0, 0, 10, BlockMaterial.Stone); Fill(w, 2, 0, 11, 11, BlockMaterial.Water);     // sâu 1 → dải 0
               Fill(w, 3, 0, 0, 2, BlockMaterial.Stone); Fill(w, 3, 0, 3, 20, BlockMaterial.Water);       // sâu 18 → dải 3
               Fill(w, 4, 0, 0, 47, BlockMaterial.Stone);                                                  // mặt 47 → tầng 11
               // cột (5, 0) rỗng → tầng 0
               Fill(w, 6, 0, 0, 12, BlockMaterial.Stone); Fill(w, 6, 0, 21, 21, BlockMaterial.Grass);     // vòm: ô rắn cao nhất 21 → tầng 5
               Fill(w, 0, 1, 0, 30, BlockMaterial.Bedrock); Fill(w, 0, 1, 31, 31, BlockMaterial.Sand);    // hàng z = 1: mặt 31 → tầng 7

               MinimapImage.ColumnInfo(w, 1, 0, out int s1, out int d1);
               Assert.AreEqual(10, s1);
               Assert.AreEqual(4, d1);
               MinimapImage.ColumnInfo(w, 5, 0, out int s5, out int d5);
               Assert.AreEqual(-1, s5);
               Assert.AreEqual(0, d5);

               var img = MinimapImage.Build(w);
               Assert.AreEqual(16 * 16 * 4, img.Length);
               var t = MinimapImage.LandTiers;
               var b = MinimapImage.WaterBands;
               Assert.AreEqual(t[5], Pixel(img, w, 0, 0), "mặt 20");
               Assert.AreEqual(b[1], Pixel(img, w, 1, 0), "sâu 4");
               Assert.AreEqual(b[0], Pixel(img, w, 2, 0), "sâu 1");
               Assert.AreEqual(b[3], Pixel(img, w, 3, 0), "sâu 18");
               Assert.AreEqual(t[11], Pixel(img, w, 4, 0), "mặt 47");
               Assert.AreEqual(t[0], Pixel(img, w, 5, 0), "cột rỗng");
               Assert.AreEqual(t[5], Pixel(img, w, 6, 0), "vòm");
               Assert.AreEqual(t[7], Pixel(img, w, 0, 1), "hàng z = 1");
               Assert.AreEqual(t[0], Pixel(img, w, 15, 15), "góc trên-phải rỗng");
           }
       }
   }
   ```

   b. `client/Assets/Game/Tests/Core/MinimapOverlayTests.cs`:
   ```csharp
   using System;
   using NUnit.Framework;

   namespace Game.Core.Tests
   {
       /// <summary>Ranh giới + vị trí tên map trên bảng map dựng tay (brief 0015).</summary>
       public class MinimapOverlayTests
       {
           static MapTable Table(int sx, int sz, int mapCount, Func<int, int, byte> id)
           {
               var ids = new byte[sx * sz];
               for (int z = 0; z < sz; z++)
                   for (int x = 0; x < sx; x++)
                       ids[x + sx * z] = id(x, z);
               var names = new string[mapCount];
               for (int i = 0; i < mapCount; i++)
                   names[i] = "Map " + (i + 1);
               return new MapTable(sx, sz, ids, names);
           }

           [Test]
           public void TwoMapsGiveOneStraightBorder()
           {
               var t = Table(8, 8, 2, (x, z) => (byte)(x < 4 ? 0 : 1));
               var raw = MinimapOverlay.Borders(t, 0);
               Assert.AreEqual(1, raw.Count);
               CollectionAssert.AreEqual(new float[] { 4, 0, 4, 1, 4, 2, 4, 3, 4, 4, 4, 5, 4, 6, 4, 7, 4, 8 }, raw[0]);

               var smooth = MinimapOverlay.Borders(t);
               Assert.AreEqual(1, smooth.Count);
               var s = smooth[0];
               Assert.AreEqual(72, s.Length, "9 điểm → 18 → 36 điểm sau 2 lần Chaikin");
               for (int i = 0; i < s.Length; i += 2)
                   Assert.AreEqual(4f, s[i], 1e-6f, "đường thẳng giữ nguyên x = 4");
               Assert.AreEqual(0f, s[1], 1e-6f, "giữ điểm đầu");
               Assert.AreEqual(8f, s[s.Length - 1], 1e-6f, "giữ điểm cuối");
           }

           [Test]
           public void OceanIsNotABorder()
           {
               var t = Table(8, 8, 1, (x, z) => x < 4 ? (byte)0 : MapTable.Ocean);
               Assert.AreEqual(0, MinimapOverlay.Borders(t).Count);
           }

           [Test]
           public void ThreeMapsMeetAtJunction()
           {
               var t = Table(8, 8, 3, (x, z) => (byte)(z >= 4 ? 2 : x < 4 ? 0 : 1));
               var raw = MinimapOverlay.Borders(t, 0);
               Assert.AreEqual(3, raw.Count);
               CollectionAssert.AreEqual(new float[] { 4, 0, 4, 1, 4, 2, 4, 3, 4, 4 }, raw[0]);
               CollectionAssert.AreEqual(new float[] { 0, 4, 1, 4, 2, 4, 3, 4, 4, 4 }, raw[1]);
               CollectionAssert.AreEqual(new float[] { 4, 4, 5, 4, 6, 4, 7, 4, 8, 4 }, raw[2]);

               foreach (var line in MinimapOverlay.Borders(t))
               {
                   bool startsAtJunction = line[0] == 4f && line[1] == 4f;
                   bool endsAtJunction = line[line.Length - 2] == 4f && line[line.Length - 1] == 4f;
                   Assert.IsTrue(startsAtJunction || endsAtJunction, "sau làm mượt 3 đường vẫn gặp nhau ở (4, 4)");
               }
           }

           [Test]
           public void IslandMapGivesClosedLoop()
           {
               var t = Table(8, 8, 2, (x, z) => (byte)(x >= 2 && x <= 5 && z >= 2 && z <= 5 ? 1 : 0));
               var raw = MinimapOverlay.Borders(t, 0);
               Assert.AreEqual(1, raw.Count);
               var r = raw[0];
               Assert.AreEqual(17 * 2, r.Length, "16 cạnh → 17 điểm (lặp điểm đầu)");
               Assert.AreEqual(2f, r[0]); Assert.AreEqual(2f, r[1]);
               Assert.AreEqual(2f, r[r.Length - 2]); Assert.AreEqual(2f, r[r.Length - 1]);

               var s = MinimapOverlay.Borders(t)[0];
               Assert.AreEqual(65 * 2, s.Length, "17 → 33 → 65 điểm");
               Assert.AreEqual(s[0], s[s.Length - 2]);
               Assert.AreEqual(s[1], s[s.Length - 1]);
           }

           [Test]
           public void BordersAreDeterministic()
           {
               var t = Table(16, 16, 3, (x, z) => (byte)((x * 7 + z * 3) / 20 % 3));
               var a = MinimapOverlay.Borders(t);
               var b = MinimapOverlay.Borders(t);
               Assert.AreEqual(a.Count, b.Count);
               for (int i = 0; i < a.Count; i++)
                   CollectionAssert.AreEqual(a[i], b[i]);
           }

           [Test]
           public void LabelAtCentroidOrNearestColumn()
           {
               // Map 1 là khối 4 × 4 ở giữa, map 0 bao quanh: trọng tâm map 0 = (4, 4) rơi vào map 1 → dùng tâm cột map 0 gần nhất,
               // bằng nhau (khoảng cách² 6,5) → cột gặp trước theo z rồi x = (3, 1).
               var t = Table(8, 8, 3, (x, z) => (byte)(x >= 2 && x <= 5 && z >= 2 && z <= 5 ? 1 : 0));
               var p = MinimapOverlay.LabelPositions(t);
               Assert.AreEqual(6, p.Length);
               Assert.AreEqual(3.5f, p[0], 1e-6f);
               Assert.AreEqual(1.5f, p[1], 1e-6f);
               Assert.AreEqual(4f, p[2], 1e-6f);
               Assert.AreEqual(4f, p[3], 1e-6f);
               Assert.IsTrue(float.IsNaN(p[4]) && float.IsNaN(p[5]), "map 2 không có cột → NaN");
           }
       }
   }
   ```

   c. `client/Assets/Game/Tests/Core/MinimapViewportTests.cs` (khung 800 × 600, world 384 × 384 → zoom 1: 600 / 384 = 1,5625 pixel/cột):
   ```csharp
   using NUnit.Framework;

   namespace Game.Core.Tests
   {
       /// <summary>Phóng to/kéo/chụm 2 ngón của minimap (brief 0015).</summary>
       public class MinimapViewportTests
       {
           const float Eps = 1e-3f;

           static MinimapViewport New() => new MinimapViewport(384f, 384f, 800f, 600f);

           static void AssertPanel(MinimapViewport v, float wx, float wz, float px, float py, string what)
           {
               v.WorldToPanel(wx, wz, out float gx, out float gy);
               Assert.AreEqual(px, gx, Eps, what + ".x");
               Assert.AreEqual(py, gy, Eps, what + ".y");
           }

           [Test]
           public void ZoomOneFitsWholeWorld()
           {
               var v = New();
               Assert.AreEqual(1f, v.Zoom);
               Assert.AreEqual(1.5625f, v.PixelsPerColumn, Eps);
               AssertPanel(v, 192f, 192f, 400f, 300f, "tâm world");
               AssertPanel(v, 0f, 0f, 100f, 0f, "góc (0, 0)");
               AssertPanel(v, 384f, 384f, 700f, 600f, "góc (384, 384)");
               v.PanelToWorld(400f, 300f, out float wx, out float wz);
               Assert.AreEqual(192f, wx, Eps);
               Assert.AreEqual(192f, wz, Eps);
           }

           [Test]
           public void ZoomAtKeepsPointUnderCursor()
           {
               var v = New();
               v.PanelToWorld(500f, 350f, out float wx, out float wz);
               Assert.AreEqual(256f, wx, Eps);
               Assert.AreEqual(224f, wz, Eps);
               v.ZoomAt(500f, 350f, 2f);
               Assert.AreEqual(2f, v.Zoom, Eps);
               Assert.AreEqual(224f, v.CenterX, Eps);
               Assert.AreEqual(208f, v.CenterZ, Eps);
               AssertPanel(v, 256f, 224f, 500f, 350f, "điểm dưới con trỏ");
           }

           [Test]
           public void ZoomIsClampedAndZoomOutRecenters()
           {
               var v = New();
               v.ZoomAt(500f, 350f, 100f);
               Assert.AreEqual(MinimapViewport.MaxZoom, v.Zoom, Eps);
               v.ZoomAt(400f, 300f, 0.001f);
               Assert.AreEqual(1f, v.Zoom, Eps);
               Assert.AreEqual(192f, v.CenterX, Eps);
               Assert.AreEqual(192f, v.CenterZ, Eps);
           }

           [Test]
           public void PanIsClampedToWorld()
           {
               var v = New();
               v.Pan(100f, 100f);
               Assert.AreEqual(192f, v.CenterX, Eps, "zoom 1: vùng nhìn ≥ world → căn giữa");
               Assert.AreEqual(192f, v.CenterZ, Eps);

               v.ZoomAt(400f, 300f, 2f); // 3,125 pixel/cột: nửa vùng nhìn 128 × 96 cột
               v.Pan(10000f, 0f);
               Assert.AreEqual(128f, v.CenterX, Eps, "kéo sang phải hết cỡ → thấy mép trái world");
               v.Pan(-10000f, 0f);
               Assert.AreEqual(256f, v.CenterX, Eps);
               v.Pan(0f, 10000f);
               Assert.AreEqual(96f, v.CenterZ, Eps);
               v.Pan(0f, -10000f);
               Assert.AreEqual(288f, v.CenterZ, Eps);
           }

           [Test]
           public void PinchZoomsAroundMidpoint()
           {
               var v = New();
               v.Pinch(300f, 300f, 500f, 300f, 250f, 300f, 550f, 300f); // khoảng cách 200 → 300
               Assert.AreEqual(1.5f, v.Zoom, Eps);
               AssertPanel(v, 192f, 192f, 400f, 300f, "điểm dưới trung điểm giữ chỗ");
           }

           [Test]
           public void PinchWithoutSpreadPans()
           {
               var a = New();
               var b = New();
               a.ZoomAt(400f, 300f, 2f);
               b.ZoomAt(400f, 300f, 2f);
               a.Pinch(300f, 300f, 500f, 300f, 320f, 310f, 520f, 310f);
               b.Pan(20f, 10f);
               Assert.AreEqual(2f, a.Zoom, Eps);
               Assert.AreEqual(b.CenterX, a.CenterX, Eps);
               Assert.AreEqual(b.CenterZ, a.CenterZ, Eps);
               Assert.AreEqual(185.6f, a.CenterX, Eps);
               Assert.AreEqual(188.8f, a.CenterZ, Eps);
           }

           [Test]
           public void VisibleUvMatchesView()
           {
               var v = New();
               v.VisibleUv(out float u, out float vv, out float w, out float h);
               Assert.AreEqual(-1f / 6f, u, Eps, "zoom 1: khung rộng hơn world → u < 0");
               Assert.AreEqual(0f, vv, Eps);
               Assert.AreEqual(4f / 3f, w, Eps);
               Assert.AreEqual(1f, h, Eps);

               v.ZoomAt(400f, 300f, 2f);
               v.VisibleUv(out u, out vv, out w, out h);
               Assert.AreEqual(1f / 6f, u, Eps);
               Assert.AreEqual(0.25f, vv, Eps);
               Assert.AreEqual(2f / 3f, w, Eps);
               Assert.AreEqual(0.5f, h, Eps);
           }

           [Test]
           public void InvalidSizesThrow()
           {
               Assert.Throws<System.ArgumentException>(() => new MinimapViewport(0f, 384f, 800f, 600f));
               Assert.Throws<System.ArgumentException>(() => new MinimapViewport(384f, 384f, 800f, 0f));
               Assert.Throws<System.ArgumentException>(() => New().SetPanelSize(-1f, 600f));
           }
       }
   }
   ```

   d. `client/Assets/Game/Tests/World/MinimapWorldTests.cs`:
   ```csharp
   using System;
   using System.Collections.Generic;
   using System.Diagnostics;
   using System.Linq;
   using Game.Core;
   using NUnit.Framework;

   namespace Game.World.Tests
   {
       /// <summary>Minimap trên world nhiều map v3 (brief 0015, world-tech 7c): ảnh nền tất định + đúng màu tầng; ranh giới khớp bảng map; tên nằm trong map.</summary>
       public class MinimapWorldTests
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

           [Test]
           public void ImageIsDeterministic()
           {
               var p = new WorldMapParams();
               var a = WorldMapGenerator.Generate(p, out _);
               var b = WorldMapGenerator.Generate(p, out _);
               var sw = Stopwatch.StartNew();
               var ia = MinimapImage.Build(a);
               sw.Stop();
               var ib = MinimapImage.Build(b);
               TestContext.WriteLine($"MINIMAP build {a.Size.X}×{a.Size.Z} ms={sw.Elapsed.TotalMilliseconds:F1}");
               Assert.AreEqual(a.Size.X * a.Size.Z * 4, ia.Length);
               Assert.AreEqual(ia.Length, ib.Length);
               for (int i = 0; i < ia.Length; i++)
                   if (ia[i] != ib[i])
                       Assert.Fail($"byte {i} khác: {ia[i]} ≠ {ib[i]}");
           }

           [Test]
           public void EveryPixelMatchesTierRule()
           {
               var (w, _) = Get(new WorldMapParams().seed);
               var img = MinimapImage.Build(w);
               var landTiers = new SortedSet<int>();
               var waterBands = new SortedSet<int>();
               for (int z = 0; z < w.Size.Z; z++)
                   for (int x = 0; x < w.Size.X; x++)
                   {
                       // Tính độc lập với MinimapImage.ColumnInfo: mặt đất bằng WorldData.SurfaceHeight, nước = ô Water cao nhất phía trên.
                       int s = w.SurfaceHeight(x, z);
                       int top = -1;
                       for (int y = w.Size.Y - 1; y > s; y--)
                           if (w.GetMaterial(x, y, z) == BlockMaterial.Water)
                           {
                               top = y;
                               break;
                           }
                       uint want;
                       if (top >= 0)
                       {
                           int d = top - s;
                           want = MinimapImage.WaterColor(d);
                           waterBands.Add(Math.Min((d - 1) / 2, 3));
                       }
                       else
                       {
                           want = MinimapImage.LandColor(s);
                           landTiers.Add(Math.Min(Math.Max(s, 0) / MinimapImage.TierHeight, 11));
                       }
                       int i = (x + w.Size.X * z) * 4;
                       uint got = (uint)(img[i] << 16 | img[i + 1] << 8 | img[i + 2]);
                       if (got != want || img[i + 3] != 255)
                           Assert.Fail($"cột ({x}, {z}): màu {got:x6} (alpha {img[i + 3]}) ≠ {want:x6}");
                   }
               TestContext.WriteLine($"MINIMAP tầng đất có mặt: {string.Join(",", landTiers)} · dải nước: {string.Join(",", waterBands)}");
               Assert.GreaterOrEqual(landTiers.Count, 6, "ảnh phải có ít nhất 6 tầng đất");
               Assert.GreaterOrEqual(waterBands.Count, 2, "ảnh phải có ít nhất 2 dải nước");
           }

           [Test]
           public void BordersMatchMapTable([ValueSource(nameof(Seeds))] uint seed)
           {
               var (_, maps) = Get(seed);
               bool Sep(byte a, byte b) => a != b && a != MapTable.Ocean && b != MapTable.Ocean;
               // Cạnh ngăn hai map: (x, z, true) = đoạn dọc (x, z)–(x, z + 1); (x, z, false) = đoạn ngang (x, z)–(x + 1, z).
               var edges = new HashSet<(int x, int z, bool vertical)>();
               for (int z = 0; z < maps.SizeZ; z++)
                   for (int x = 0; x < maps.SizeX; x++)
                   {
                       if (x + 1 < maps.SizeX && Sep(maps.Get(x, z), maps.Get(x + 1, z)))
                           edges.Add((x + 1, z, true));
                       if (z + 1 < maps.SizeZ && Sep(maps.Get(x, z), maps.Get(x, z + 1)))
                           edges.Add((x, z + 1, false));
                   }
               Assert.Greater(edges.Count, 0);

               // Chưa làm mượt: mỗi đoạn là đúng một cạnh ngăn map, mỗi cạnh có đúng một lần.
               var raw = MinimapOverlay.Borders(maps, 0);
               var seen = new HashSet<(int x, int z, bool vertical)>();
               foreach (var line in raw)
                   for (int i = 0; i + 3 < line.Length; i += 2)
                   {
                       float ax = line[i], az = line[i + 1], bx = line[i + 2], bz = line[i + 3];
                       Assert.AreEqual(1f, Math.Abs(bx - ax) + Math.Abs(bz - az), 1e-6f, "đoạn đơn vị theo trục");
                       var key = ax == bx ? ((int)ax, (int)Math.Min(az, bz), true) : ((int)Math.Min(ax, bx), (int)az, false);
                       Assert.IsTrue(edges.Contains(key), $"seed {seed}: đoạn {key} không ngăn hai map");
                       Assert.IsTrue(seen.Add(key), $"seed {seed}: đoạn {key} lặp");
                   }
               Assert.AreEqual(edges.Count, seen.Count, "mọi cạnh ngăn map đều có trong ranh giới");

               // Đã làm mượt: mỗi điểm cách một cạnh ngăn map ≤ 0,5 ô; trung điểm mỗi cạnh cách đường làm mượt ≤ 0,5 ô.
               var smooth = MinimapOverlay.Borders(maps);
               var edgeList = edges.ToList();
               double maxDev = 0, maxGap = 0;
               int points = 0;
               foreach (var line in smooth)
                   for (int i = 0; i + 1 < line.Length; i += 2)
                   {
                       points++;
                       double best = double.MaxValue;
                       foreach (var e in edgeList)
                           best = Math.Min(best, DistToEdge(line[i], line[i + 1], e));
                       maxDev = Math.Max(maxDev, best);
                   }
               foreach (var e in edgeList)
               {
                   double mx = e.x + (e.vertical ? 0 : 0.5), mz = e.z + (e.vertical ? 0.5 : 0);
                   double best = double.MaxValue;
                   foreach (var line in smooth)
                       for (int i = 0; i + 3 < line.Length; i += 2)
                           best = Math.Min(best, DistToSegment(mx, mz, line[i], line[i + 1], line[i + 2], line[i + 3]));
                   maxGap = Math.Max(maxGap, best);
               }
               TestContext.WriteLine($"seed {seed}: {edges.Count} cạnh, {raw.Count} đường, {points} điểm sau làm mượt, lệch xa nhất {maxDev:F3}, hở xa nhất {maxGap:F3}");
               Assert.LessOrEqual(maxDev, 0.5, $"seed {seed}: điểm làm mượt xa ranh giới");
               Assert.LessOrEqual(maxGap, 0.5, $"seed {seed}: đường làm mượt bỏ sót cạnh");
           }

           [Test]
           public void LabelsSitInsideTheirMap([ValueSource(nameof(Seeds))] uint seed)
           {
               var (_, maps) = Get(seed);
               var pos = MinimapOverlay.LabelPositions(maps);
               Assert.AreEqual(maps.MapCount * 2, pos.Length);
               for (int m = 0; m < maps.MapCount; m++)
               {
                   float x = pos[2 * m], z = pos[2 * m + 1];
                   Assert.IsFalse(float.IsNaN(x) || float.IsNaN(z), $"seed {seed}: map {m} không có vị trí tên");
                   TestContext.WriteLine($"seed {seed}: {maps.Name(m)} ở ({x:F1}, {z:F1})");
                   Assert.AreEqual(m, (int)maps.Get((int)Math.Floor(x), (int)Math.Floor(z)), $"seed {seed}: tên {maps.Name(m)} nằm ngoài map");
               }
           }

           static double DistToEdge(double px, double pz, (int x, int z, bool vertical) e) =>
               e.vertical ? DistToSegment(px, pz, e.x, e.z, e.x, e.z + 1) : DistToSegment(px, pz, e.x, e.z, e.x + 1, e.z);

           static double DistToSegment(double px, double pz, double ax, double az, double bx, double bz)
           {
               double dx = bx - ax, dz = bz - az, len2 = dx * dx + dz * dz;
               double t = len2 > 0 ? Math.Max(0, Math.Min(1, ((px - ax) * dx + (pz - az) * dz) / len2)) : 0;
               double cx = ax + t * dx - px, cz = az + t * dz - pz;
               return Math.Sqrt(cx * cx + cz * cz);
           }
       }
   }
   ```
   Xong khi: 4 file viết xong.

5. **`client/Assets/Game/Client/Editor/WorldMapPreview.cs`** — dùng màu của `MinimapImage` thay bảng màu riêng (để user xem bảng màu minimap trên PNG). Số dòng dưới đây theo file **trước khi sửa** (commit `ca90b92`) — sửa từ dưới lên để số dòng không lệch:
   - Xoá mảng `LandTiers` (`:18-29`, từ dòng `static readonly Color32[] LandTiers =` tới hết `};`) và dòng trống ngay sau nó.
   - Trong khối chọn màu, thay từ dòng `else if (s < p.waterLevel)` (`:53`) tới hết nhánh `else { … }` (`:63`) bằng:
   ```csharp
                   else
                   {
                       uint c = MinimapImage.ColumnColor(world, x, z);
                       color = new Color32((byte)(c >> 16), (byte)(c >> 8), (byte)c, 255);
                   }
   ```
   - Sửa dòng tóm tắt `:11-12` thành: `/// Xuất ảnh PNG xem trước world nhiều map (brief 0009, bước 7a): 1 điểm ảnh = 1 cột, màu theo MinimapImage (brief 0015); ranh giới map tô đen theo cột.` và `/// Chỉ là công cụ xem; minimap trong game làm ở brief 0016.`
   - Giữ nguyên: nhánh ranh giới đen (`:51-52`), đếm `land`, `s`, log `WORLDPREVIEW`.
   Xong khi: biên dịch không lỗi; chạy menu `Game/Ảnh xem trước world nhiều map` (MCP `execute_menu_item`) → console có dòng `WORLDPREVIEW` và file `client/Builds/world_maps_preview.png` được ghi lại (đường dẫn tương đối thư mục project Unity; `Builds/` không commit — kiểm bằng `git status`).

6. **Biên dịch + test** — theo "Lệnh executor chạy".

7. Commit code (7 file mới + `.meta` + `WorldMapPreview.cs`) với message `brief 0015: lõi minimap — ảnh nền, ranh giới, vùng nhìn`; ghi Kết quả, Kết quả test; `date` → "Kết thúc"; đổi trạng thái brief thành `chờ review`; commit brief `brief 0015: kết quả`.

## Tiêu chí xong
- 7 file mới biên dịch không lỗi; `Game.Core` vẫn không tham chiếu UnityEngine (asmdef không đổi).
- 4 lớp test mới đạt hết: `MinimapImageTests` (3), `MinimapOverlayTests` (6), `MinimapViewportTests` (8), `MinimapWorldTests` (2 + 5 + 5 = 12) — tổng 29 test mới.
- Toàn bộ EditMode đạt (trước brief 272 theo review 0014 → sau 301).
- Kết quả ghi: dòng `MINIMAP build … ms=…`, dòng `MINIMAP tầng đất có mặt…`, 5 dòng `seed …: … cạnh, … đường, …, lệch xa nhất …, hở xa nhất …`, 15 dòng vị trí tên, dòng `WORLDPREVIEW …` của menu xem trước.

## Test sau khi xong

### Phần cần test
- `MinimapImage` (bước 1) · EditMode `MinimapImageTests` · bảng màu 16 màu khác nhau; ranh giới tầng 15/16, dải nước 2/3, 6/7; cột dựng tay đúng màu (đất theo tầng, nước theo độ sâu, vòm lấy ô cao nhất, cột rỗng = tầng 0, hàng z = 1 đúng vị trí byte).
- `MinimapImage` trên world thật (bước 1) · EditMode `MinimapWorldTests.ImageIsDeterministic`, `EveryPixelMatchesTierRule` · 2 lần sinh cùng seed → ảnh giống từng byte; mọi điểm ảnh khớp quy tắc tính độc lập; ≥ 6 tầng đất, ≥ 2 dải nước; ghi ms dựng ảnh.
- `MinimapOverlay.Borders` (bước 2) · EditMode `MinimapOverlayTests` (thẳng 2 map, biển không phải ranh giới, 3 map gặp nhau, vòng kín, tất định) + `MinimapWorldTests.BordersMatchMapTable` × 5 seed · đoạn chưa làm mượt = đúng tập cạnh ngăn map, không thiếu không lặp; sau làm mượt lệch ≤ 0,5 ô và không bỏ sót cạnh.
- `MinimapOverlay.LabelPositions` (bước 2) · EditMode `LabelAtCentroidOrNearestColumn` + `MinimapWorldTests.LabelsSitInsideTheirMap` × 5 seed · trọng tâm hoặc cột gần nhất; tên nằm trong đúng map.
- `MinimapViewport` (bước 3) · EditMode `MinimapViewportTests` (8 test) · số đúng như trong test.
- `WorldMapPreview` (bước 5) · chạy menu `Game/Ảnh xem trước world nhiều map` · có dòng `WORLDPREVIEW`, mã băm `world=a7719c8999939263 maps=ab2074e6196e5f51` không đổi, file PNG được ghi.
- Regression: toàn bộ EditMode (`WorldMapGeneratorTests`, `GoldenHashTests`, … không đổi) · đạt. PlayMode không cần chạy (không đổi code runtime — `WorldMapPreview` là Editor).

### Lệnh executor chạy
1. MCP `refresh_unity` → `read_console` (types error) — 0 lỗi biên dịch.
2. MCP `run_tests` EditMode lọc 4 lớp mới (`Game.Core.Tests.MinimapImageTests`, `Game.Core.Tests.MinimapOverlayTests`, `Game.Core.Tests.MinimapViewportTests`, `Game.World.Tests.MinimapWorldTests`) → ghi số đạt / tổng (29) và chép các dòng `MINIMAP …`, `seed …` từ output.
3. MCP `run_tests` EditMode (cả bộ) → ghi dòng tổng kết (mong đợi 301/301).
4. MCP `execute_menu_item` `Game/Ảnh xem trước world nhiều map` → `read_console` chép dòng `WORLDPREVIEW`; `ls -la client/Builds/world_maps_preview.png` (giờ sửa đổi mới).
5. `git status --short` — chỉ có file trong Phạm vi (+ `.meta`, + brief); `client/Builds/` không có trong danh sách (đã bị ignore — nếu có thì không commit, ghi vào Kết quả).
6. Commit code; `date "+%Y-%m-%d %H:%M"` → "Kết thúc"; commit brief.

### User tự test
Planner gửi ảnh `client/Builds/world_maps_preview.png` khi review. User xem bảng màu:
1. Các tầng độ cao phân biệt được bằng mắt (đồng bằng xanh nhạt → đồi vàng/nâu → đỉnh xám/trắng), không có hai tầng kề nhau trông như một.
2. Nước: nông (bờ, hồ cạn) nhạt, sâu (biển xa, giữa hồ) đậm; bờ biển rõ.
3. Muốn đổi màu nào → nói màu/tầng; Planner sửa ở brief 0016.
(Ranh giới vector, tên map, phóng to/kéo, chụm 2 ngón: user xem ở brief 0016.)

## Dừng và chuyển lại Opus khi
- Brief mơ hồ hoặc mâu thuẫn với code thật (số dòng lệch nhiều, chữ ký khác).
- Một test ra khác mong đợi mà không phải lỗi gõ — ghi số thật/dòng lỗi, **không tự sửa số trong test hay code lõi** (vd `BordersMatchMapTable` lệch > 0,5, `LabelsSitInsideTheirMap` sai map, `EveryPixelMatchesTierRule` có < 6 tầng).
- Cần một quyết định ngoài "Quyết định đã chốt" (vd đổi màu, đổi số lần làm mượt, đổi luật tầng).
- Cần API Unity không có trong brief.
- Cùng một lỗi sửa 2 lần vẫn chưa qua.
- Phải sửa file ngoài "Phạm vi".

---
<!-- Từ đây trở xuống: EXECUTOR ghi. -->

## Kết quả

## Kết quả test

## Vướng mắc

---
<!-- PLANNER ghi khi review. -->

## Review
