# Brief 0019 — World 7e (phần 1/2): vùng mô phỏng 64 × 64 (tìm đường cấp mảng theo vùng) + SimDemo tự tìm chỗ trong Map 1

> Trạng thái: cần Opus
> Review: đầy đủ
> Model thực thi: Sonnet · medium
> Cập nhật: 2026-10-21 12:20

<!-- Planner (Opus) viết, Executor (Sonnet) làm theo ở PHIÊN KHÁC — executor không có lịch sử chat, không được tự quyết.
Trạng thái: chờ làm → đang làm → chờ review → đã duyệt; hoặc → cần Opus (executor dừng, hỏi ở mục Vướng mắc).
File được commit. -->

## Mục tiêu
Task [world bước 7](../tasks/m1-world-buoc-7-nhieu-map-minimap.md), dòng **7e** của [world-tech](../../design/04-tech/world-tech.md#kế-hoạch-prototype-m1). User chốt ngày 2026-10-21 (ghi trong task, mục "Planner đề xuất"). Bước 7e chia làm 2 brief:
- **0019 (brief này), chỉ lõi Game.Core + test EditMode:**
  1. **Vùng mô phỏng** `SimBounds`: hộp cột, đủ chiều cao world. `GridPathfinder` cấp mảng theo vùng thay vì theo cả world: hộp 64 × 64 × 48 cần ~3,5 MB, cả world 384 × 384 × 48 cần ~127 MB.
  2. `Simulation` nhận vùng.
  3. `SimConfig.regionSize` (= 64, đặt trong `sim_m1.json`).
  4. `SimDemo.FindSite`: tự tìm chỗ phẳng 9 × 7 cột trong Map 1, gần checkpoint Map 1, tránh dấu mốc. Bố trí demo cũ được dời tới đó.
- **0020 (viết sau khi 0019 được duyệt):** scene SimView chuyển sang world 3 map (nạp dần), camera đặt ở trại, PlayMode + user xem.

Mô phỏng trên đảo v2 cũ **phải cho kết quả y hệt**: vùng mặc định là cả world, mọi test mô phỏng cũ và mã băm mô phỏng không đổi.

## Bối cảnh cần đọc
- `client/Assets/Game/Core/WorldData.cs`:
  - `WorldSize` có `X, Y, Z` và `CellCount` (`:21`).
  - `InBounds` ở `:27`.
  - `Index(x, y, z) => x + X * (z + Z * y)` ở `:31`.
  - `Coords` ở `:33-39`.
  - `WorldData.GetMaterial(x, y, z)` ở `:81`.
- `client/Assets/Game/Core/Sim/GridPathfinder.cs`, 346 dòng:
  - Trường mảng `g, parent, seenStamp, closedStamp` (int) và `blocked, tempBlocked` (bool) ở `:22-27`.
  - Constructor `public GridPathfinder(WorldData world)` ở `:34-44`, cấp mảng theo `world.Size.CellCount` (`:37`).
  - Các chỗ dùng kích thước world:
    - `world.Size.InBounds` / `world.Size.Index` ở `:49-50`, `:56-57`, `:72-73`, `:80`, `:133`;
    - `var size = world.Size;` rồi `size.InBounds` / `size.Index` / `size.Coords` ở `:146-176`, `:197-220`, `:244-282`.
  - Tham số `Predicate<CellPos> region` của `FloodRegion` (`:194`) là tên có sẵn, **không** đổi.
- `client/Assets/Game/Core/Sim/Simulation.cs`:
  - Constructor `public Simulation(SimConfig config, WorldData world)` ở `:20-27`, tạo `new GridPathfinder(world)` ở `:26`.
  - `AddFarmOrder` ở `:684`. Trong đó `var size = World.Size;` ở `:688`, kiểm `if (!size.InBounds(f.X, f.Y, f.Z)) throw new ArgumentException("Ô ruộng ngoài map: " + f);` ở `:692-693`.
- `client/Assets/Game/Core/Sim/SimConfig.cs`:
  - Lớp `SimConfig` ở `:116`, trường `ticksPerSecond` ở `:120`.
  - `Validate()` ở `:199`, dòng `int msPerTick = 1000 / ticksPerSecond;` ở `:203`.
- `design/data/sim_m1.json` và `client/Assets/Game/Client/Resources/Config/sim_m1.json`: phải giống từng byte (`ConfigTests.SimClientCopyMatchesDesignData`). Dòng 4 là `    "ticksPerSecond": 10,`.
- `client/Assets/Game/Core/Sim/SimDemo.cs`, 41 dòng, không có `using`:
  - Bố trí gốc trên đảo v2: mặt đất y = 17. Ô ruộng ở y = 17; nhà, kho, sinh vật ở y = 18.
  - Các ô chiếm x 30–36, z 16–20 (nhà 2 × 2 × 2 tại (30, 18, 19) và (32, 18, 19)).
  - `Build(Simulation sim)` ở `:19-31`.
- `client/Assets/Game/Core/MinimapImage.cs:44`: `public static void ColumnInfo(WorldData world, int x, int z, out int surface, out int waterDepth)`. `surface` là ô rắn cao nhất không tính nước (−1 nếu cột rỗng); `waterDepth` là độ sâu nước phía trên surface.
- `client/Assets/Game/Core/LandmarkPlacer.cs`:
  - `Landmark` có các trường `Kind, Map, Type, X, Z, Surface, Name`.
  - `LandmarkPlacer.Place(WorldData, MapTable, WorldMapParams)` trả checkpoint theo map trước.
- `client/Assets/Game/Core/MapTable.cs`: `byte Get(int x, int z)`, `MapCount`.
- `client/Assets/Game/Core/WorldMapParams.cs`: `waterLevel = 12` (`:29`), `seed`.
- `CellPos` (`client/Assets/Game/Core/Sim/SimData.cs:6`) có `Offset(dx, dy, dz)` (`:22`). `BlockMaterial` (`Core/Cell.cs:4`) gồm `Empty, Grass, Dirt, Stone, Sand, Water, Bedrock`.
- Test:
  - `client/Assets/Game/Tests/Core/Sim/` (asmdef `Game.Core.Tests`, namespace `Game.Core.Tests`): `TestWorlds.Flat(groundTop)` là world 16 × 16 × 16, ô y = 1..groundTop là Dirt, sinh vật đứng ở y = groundTop + 1. Mẫu tạo cấu hình: `new SimConfig { simSeed = … }` (`SimulationTests.cs:129`).
  - `client/Assets/Game/Tests/World/` (asmdef `Game.World.Tests`, namespace `Game.World.Tests`): `SimDemoTests.cs` là mẫu (đọc `sim_m1.json` qua `Data(...)` ở `:13`, hàm `Run` ở `:25-38` đếm `StallStarted`). `LandmarkPlacerTests.cs` có mẫu sinh world v3: `WorldMapGenerator.Generate(p, out var maps)`.

Không dùng API Unity nào: toàn bộ là C# thuần trong Game.Core (No Engine References). Test World dùng `JsonUtility`, `Application.dataPath`, `File`, giống `SimDemoTests`.

## Quyết định đã chốt (không được đổi)
1. **Vùng mô phỏng** là `SimBounds(x0, z0, sizeX, sizeZ)`: hộp cột `[x0, x0 + sizeX) × [z0, z0 + sizeZ)`, luôn đủ chiều cao world.
   - Tìm đường chỉ đi trong vùng: ô ngoài vùng coi như ngoài map. Cụ thể là không đỡ, không cho thân, `CanStand` sai; `SetBlocked` ngoài vùng ném lỗi; `AddTempBlock` ngoài vùng bỏ qua.
   - Mảng của `GridPathfinder` có `sizeX · sizeZ · SizeY` phần tử, xếp theo cùng thứ tự với `WorldSize.Index` (x nhanh nhất, rồi z, rồi y) nhưng tính từ góc vùng.
2. **Vùng mặc định = cả world** (`SimBounds.Whole`). Constructor cũ `GridPathfinder(world)` và `Simulation(config, world)` vẫn có và cho kết quả y hệt trước: mọi test mô phỏng cũ, `SimDeterminismTests` và `SimDemoTests` không đổi một dòng.
3. `Simulation.AddFarmOrder`: ô ruộng ngoài vùng thì ném `ArgumentException("Ô ruộng ngoài vùng mô phỏng: …")`. Nhà, kho, sinh vật ngoài vùng tự bị chặn vì `CanStand` sai.
4. `SimConfig.regionSize = 64`, trong `[16, 256]`, có trong cả 2 file `sim_m1.json`. Mô phỏng **không** tự dùng giá trị này: `FindSite` nhận nó làm tham số, còn SimView (0020) truyền vào.
5. **Chỗ đặt trại** (`SimDemo.FindSite`):
   - Cửa sổ **9 × 7 cột**, tức bố trí gốc x 30–36 × z 16–20 thêm 1 cột lề mỗi phía.
   - Mọi cột trong cửa sổ phải: thuộc đúng `map`, cùng một độ cao `surface`, `waterDepth == 0`, `surface ≥ waterLevel`, vật liệu ở `surface` là Grass hoặc Dirt.
   - Không dấu mốc nào nằm trong cửa sổ nới rộng 2 cột mỗi phía.
   - Chọn cửa sổ có tâm gần tâm checkpoint của `map` nhất. So bằng số nguyên `long` với toạ độ nhân đôi. Bằng nhau thì lấy cửa sổ gặp trước (duyệt z rồi x).
   - Độ dời = (góc cửa sổ + 1) − (30, ·, 16); `Dy = surface − 17`.
   - **Vùng** = hộp `regionSize × regionSize` có tâm ở ô giữa cửa sổ, kẹp trong world.
   - Không tìm được → `InvalidOperationException`.
6. `SimDemo.Build(sim)` cũ giữ nguyên kết quả (dời 0). `Build(sim, site)` dời mọi ô theo `site`.
7. Không làm scene, SimView, client (để brief 0020). Không đổi bộ sinh, `LandmarkPlacer`, `design/` (ngoài 2 file JSON).
8. Giờ bắt đầu/kết thúc lấy bằng `date "+%Y-%m-%d %H:%M"` ngay lúc đó.

## Phạm vi
- **Được tạo** (cùng các file `.meta` Unity tự tạo):
  - `client/Assets/Game/Core/Sim/SimBounds.cs`
  - `client/Assets/Game/Tests/Core/Sim/SimBoundsTests.cs`
  - `client/Assets/Game/Tests/World/SimDemoMapsTests.cs`
- **Được sửa:**
  - `client/Assets/Game/Core/Sim/GridPathfinder.cs` (bước 2)
  - `Simulation.cs` (bước 3)
  - `SimConfig.cs` + 2 file `sim_m1.json` (bước 4)
  - `SimDemo.cs` (bước 5)
  - mục Kết quả / Kết quả test / Vướng mắc của brief này.
- **Không được sửa:** mọi file khác, kể cả các test cũ. `ProjectSettings/` phải trả lại sau test.

## Các bước
0. `date "+%Y-%m-%d %H:%M"` → ghi "Bắt đầu", "Base commit" (`git rev-parse --short HEAD`) vào Kết quả; trạng thái `đang làm`.

1. **Tạo `client/Assets/Game/Core/Sim/SimBounds.cs`:**
   ```csharp
   using System;

   namespace Game.Core.Sim
   {
       /// <summary>
       /// Vùng mô phỏng (world 7e, brief 0019): hộp cột [X0, X0 + SizeX) × [Z0, Z0 + SizeZ), đủ chiều cao world.
       /// Tìm đường chỉ đi trong vùng → bộ nhớ theo vùng, không theo cả world.
       /// </summary>
       public readonly struct SimBounds : IEquatable<SimBounds>
       {
           public readonly int X0, Z0, SizeX, SizeZ;

           public SimBounds(int x0, int z0, int sizeX, int sizeZ)
           {
               if (sizeX < 1 || sizeZ < 1) throw new ArgumentException("Vùng mô phỏng phải có kích thước ≥ 1");
               X0 = x0;
               Z0 = z0;
               SizeX = sizeX;
               SizeZ = sizeZ;
           }

           public static SimBounds Whole(WorldSize size) => new SimBounds(0, 0, size.X, size.Z);

           public bool Contains(int x, int z) => x >= X0 && x < X0 + SizeX && z >= Z0 && z < Z0 + SizeZ;

           public bool FitsIn(WorldSize size) => X0 >= 0 && Z0 >= 0 && X0 + SizeX <= size.X && Z0 + SizeZ <= size.Z;

           public bool Equals(SimBounds o) => X0 == o.X0 && Z0 == o.Z0 && SizeX == o.SizeX && SizeZ == o.SizeZ;
           public override bool Equals(object obj) => obj is SimBounds o && Equals(o);
           public override int GetHashCode() => ((X0 * 397 ^ Z0) * 397 ^ SizeX) * 397 ^ SizeZ;
           public static bool operator ==(SimBounds a, SimBounds b) => a.Equals(b);
           public static bool operator !=(SimBounds a, SimBounds b) => !a.Equals(b);
           public override string ToString() => $"x {X0}..{X0 + SizeX - 1}, z {Z0}..{Z0 + SizeZ - 1}";
       }
   }
   ```
   Xong khi: biên dịch không lỗi.

2. **`GridPathfinder.cs`:**
   1. Thêm trường `readonly SimBounds bounds;` và `readonly int sizeY;` cạnh `readonly WorldData world;` (`:21`).
   2. Thay constructor `:34-44` bằng:
      ```csharp
      public GridPathfinder(WorldData world)
          : this(world, world != null ? SimBounds.Whole(world.Size) : new SimBounds(0, 0, 1, 1))
      {
      }

      /// <summary>Tìm đường trong vùng mô phỏng (brief 0019): mảng cấp theo vùng; ô ngoài vùng coi như ngoài map.</summary>
      public GridPathfinder(WorldData world, SimBounds bounds)
      {
          this.world = world ?? throw new ArgumentNullException(nameof(world));
          if (!bounds.FitsIn(world.Size)) throw new ArgumentException("Vùng mô phỏng ngoài world: " + bounds);
          this.bounds = bounds;
          sizeY = world.Size.Y;
          int n = bounds.SizeX * bounds.SizeZ * sizeY;
          g = new int[n];
          parent = new int[n];
          seenStamp = new int[n];
          closedStamp = new int[n];
          blocked = new bool[n];
          tempBlocked = new bool[n];
      }

      public SimBounds Bounds => bounds;

      /// <summary>Số ô của mảng tìm đường (= SizeX · SizeZ · chiều cao world của vùng).</summary>
      public int CellCount => g.Length;

      bool InRegion(int x, int y, int z) => y >= 0 && y < sizeY && bounds.Contains(x, z);

      /// <summary>Chỉ số trong vùng: x nhanh nhất, rồi z, rồi y — như WorldSize.Index nhưng tính từ góc vùng.</summary>
      int Index(int x, int y, int z) => (x - bounds.X0) + bounds.SizeX * ((z - bounds.Z0) + bounds.SizeZ * y);

      void Coords(int i, out int x, out int y, out int z)
      {
          x = i % bounds.SizeX + bounds.X0;
          int rest = i / bounds.SizeX;
          z = rest % bounds.SizeZ + bounds.Z0;
          y = rest / bounds.SizeZ;
      }
      ```
   3. Trong phần còn lại của file:
      - thay mọi `world.Size.InBounds(` và `size.InBounds(` bằng `InRegion(`;
      - thay mọi `world.Size.Index(` và `size.Index(` bằng `Index(`;
      - thay mọi `size.Coords(` bằng `Coords(`;
      - xoá 3 dòng `var size = world.Size;` (trong `FindPathToNearest`, `FloodRegion`, `Search`);
      - thông báo lỗi ở `SetBlocked` đổi từ `"Ô ngoài map: "` thành `"Ô ngoài vùng mô phỏng: "`.
      Ngoài các thay đổi trên, không đổi logic hay thứ tự nào.
   Xong khi:
   - `grep -n "Size\.\|size\." client/Assets/Game/Core/Sim/GridPathfinder.cs` chỉ còn các dòng trong 2 constructor (`world.Size`) và `bounds.Size…`;
   - biên dịch không lỗi;
   - MCP `run_tests` EditMode lọc `GridPathfinderTests` đạt hết như trước.

3. **`Simulation.cs`:**
   1. Thay constructor `:20-27` bằng:
      ```csharp
      public Simulation(SimConfig config, WorldData world)
          : this(config, world, world != null ? SimBounds.Whole(world.Size) : new SimBounds(0, 0, 1, 1))
      {
      }

      /// <summary>Mô phỏng trong vùng (world 7e, brief 0019): sinh vật chỉ tìm đường trong bounds.</summary>
      public Simulation(SimConfig config, WorldData world, SimBounds bounds)
      {
          if (world == null) throw new ArgumentNullException(nameof(world));
          config.Validate();
          Config = config;
          World = world;
          pathfinder = new GridPathfinder(world, bounds);
          State = new SimState(new SimRng(config.simSeed, 1UL));
      }

      public SimBounds Bounds => pathfinder.Bounds;

      /// <summary>Số ô mảng tìm đường — đo bộ nhớ (brief 0019).</summary>
      public int PathfinderCellCount => pathfinder.CellCount;
      ```
   2. Trong `AddFarmOrder`, ngay sau dòng `throw new ArgumentException("Ô ruộng ngoài map: " + f);` (`:693`), thêm:
      ```csharp
      if (!Bounds.Contains(f.X, f.Z))
          throw new ArgumentException("Ô ruộng ngoài vùng mô phỏng: " + f);
      ```
   Xong khi: biên dịch không lỗi.

4. **`SimConfig.regionSize`:**
   1. `SimConfig.cs`: thêm sau `public int ticksPerSecond = 10;` (`:120`):
      ```csharp
      /// <summary>Cạnh vùng mô phỏng (cột) quanh trang trại trên world nhiều map (world 7e, brief 0019).</summary>
      public int regionSize = 64;
      ```
      Trong `Validate()`, ngay sau dòng `int msPerTick = …` (`:203`), thêm:
      ```csharp
      if (regionSize < 16 || regionSize > 256)
          throw new ArgumentException("regionSize phải trong [16, 256].");
      ```
   2. Hai file `sim_m1.json` (`design/data/` và `client/Assets/Game/Client/Resources/Config/`): thêm dòng `    "regionSize": 64,` ngay sau dòng `    "ticksPerSecond": 10,`. Sau đó chạy `cmp` hai file → không in gì.
   Xong khi: biên dịch không lỗi; `ConfigTests` đạt.

5. **`SimDemo.cs`:** thêm `using System;` và `using System.Collections.Generic;` ở đầu file.
   1. Thêm struct `DemoSite` trong cùng file, trước `public static class SimDemo`:
      ```csharp
      /// <summary>Chỗ đặt trang trại demo trên world nhiều map (world 7e, brief 0019).</summary>
      public readonly struct DemoSite
      {
          /// <summary>Độ dời cộng vào mọi ô của bố trí gốc (đảo v2).</summary>
          public readonly int Dx, Dy, Dz;
          /// <summary>Độ cao mặt đất chung của cửa sổ 9 × 7 cột.</summary>
          public readonly int Surface;
          public readonly SimBounds Bounds;

          public DemoSite(int dx, int dy, int dz, int surface, SimBounds bounds)
          {
              Dx = dx;
              Dy = dy;
              Dz = dz;
              Surface = surface;
              Bounds = bounds;
          }

          public CellPos Shift(CellPos c) => c.Offset(Dx, Dy, Dz);

          public override string ToString() => $"dời ({Dx}, {Dy}, {Dz}), mặt y = {Surface}, vùng {Bounds}";
      }
      ```
   2. Trong `SimDemo`, thêm hằng số:
      ```csharp
      /// <summary>Góc (x, z) nhỏ nhất của bố trí gốc và độ cao ô ruộng gốc (đảo v2).</summary>
      public const int BaseX = 30, BaseZ = 16, BaseGround = 17;
      /// <summary>Cửa sổ phẳng: bố trí chiếm x 30–36, z 16–20, thêm 1 cột lề mỗi phía.</summary>
      public const int SiteSizeX = 9, SiteSizeZ = 7;
      /// <summary>Dấu mốc phải cách cửa sổ ít nhất bấy nhiêu cột.</summary>
      public const int LandmarkClearance = 2;
      ```
   3. Đổi `Build(Simulation sim)` thành:
      ```csharp
      /// <summary>Gọi một lần trên mô phỏng mới (chưa Tick). Trả id kho.</summary>
      public static int Build(Simulation sim) => Build(sim, 0, 0, 0);

      /// <summary>Bố trí demo dời tới chỗ site (world nhiều map). Trả id kho.</summary>
      public static int Build(Simulation sim, DemoSite site) => Build(sim, site.Dx, site.Dy, site.Dz);

      static int Build(Simulation sim, int dx, int dy, int dz)
      {
          int a = sim.AddHouse(HouseA.Offset(dx, dy, dz));
          int b = sim.AddHouse(HouseB.Offset(dx, dy, dz));
          int storage = sim.AddStorage(StorageCell.Offset(dx, dy, dz));
          for (int i = 0; i < Species.Length; i++)
              sim.AddCreature(Species[i], CreatureCells[i].Offset(dx, dy, dz));
          sim.AssignHome(0, a);
          sim.AssignHome(1, a);
          sim.AssignHome(2, b);
          var soil = new CellPos[Soil.Length];
          for (int i = 0; i < Soil.Length; i++)
              soil[i] = Soil[i].Offset(dx, dy, dz);
          sim.AddFarmOrder(soil, storage);
          return storage;
      }
      ```
      Thứ tự gọi giữ nguyên như bản cũ: nhà A, nhà B, kho, sinh vật, AssignHome, AddFarmOrder.
   4. Thêm `FindSite`:
      ```csharp
      /// <summary>
      /// Tìm chỗ đặt trang trại demo (brief 0019): cửa sổ 9 × 7 cột trong map, cùng độ cao, khô, mặt là cỏ/đất, không dấu mốc trong lề 2 cột;
      /// tâm gần tâm checkpoint của map nhất (bằng nhau → gặp trước, duyệt z rồi x). Vùng = hộp regionSize quanh tâm cửa sổ, kẹp trong world.
      /// </summary>
      public static DemoSite FindSite(WorldData world, MapTable maps, IReadOnlyList<Landmark> landmarks, int map, int waterLevel, int regionSize)
      {
          var s = world.Size;
          int sx = s.X, sz = s.Z;
          if (regionSize < SiteSizeX || regionSize > sx || regionSize > sz)
              throw new ArgumentException("regionSize phải trong [" + SiteSizeX + ", cạnh world]");
          var surface = new int[sx * sz];
          var ok = new bool[sx * sz];
          for (int z = 0; z < sz; z++)
              for (int x = 0; x < sx; x++)
              {
                  MinimapImage.ColumnInfo(world, x, z, out int surf, out int depth);
                  int i = x + sx * z;
                  surface[i] = surf;
                  var m = surf >= 0 ? world.GetMaterial(x, surf, z) : BlockMaterial.Empty;
                  ok[i] = maps.Get(x, z) == map && depth == 0 && surf >= waterLevel
                          && (m == BlockMaterial.Grass || m == BlockMaterial.Dirt);
              }

          int cpx = -1, cpz = -1;
          for (int k = 0; k < landmarks.Count; k++)
              if (landmarks[k].Kind == LandmarkKind.Checkpoint && landmarks[k].Map == map)
              {
                  cpx = landmarks[k].X;
                  cpz = landmarks[k].Z;
                  break;
              }
          if (cpx < 0)
              throw new InvalidOperationException("Không có checkpoint của map " + map);

          long bestD = long.MaxValue;
          int bx = -1, bz = -1;
          for (int wz = 0; wz + SiteSizeZ <= sz; wz++)
              for (int wx = 0; wx + SiteSizeX <= sx; wx++)
              {
                  long ddx = 2L * wx + SiteSizeX - (2L * cpx + 1);
                  long ddz = 2L * wz + SiteSizeZ - (2L * cpz + 1);
                  long d = ddx * ddx + ddz * ddz;
                  if (d >= bestD || !IsSiteFlat(surface, ok, sx, wx, wz) || NearLandmark(landmarks, wx, wz))
                      continue;
                  bestD = d;
                  bx = wx;
                  bz = wz;
              }
          if (bx < 0)
              throw new InvalidOperationException("Không có chỗ phẳng " + SiteSizeX + " × " + SiteSizeZ + " cho trang trại demo trong map " + map);

          int ground = surface[bx + sx * bz];
          int cx = bx + SiteSizeX / 2, cz = bz + SiteSizeZ / 2;
          int x0 = Math.Min(Math.Max(cx - regionSize / 2, 0), sx - regionSize);
          int z0 = Math.Min(Math.Max(cz - regionSize / 2, 0), sz - regionSize);
          return new DemoSite(bx + 1 - BaseX, ground - BaseGround, bz + 1 - BaseZ, ground, new SimBounds(x0, z0, regionSize, regionSize));
      }

      static bool IsSiteFlat(int[] surface, bool[] ok, int sx, int wx, int wz)
      {
          int h = surface[wx + sx * wz];
          for (int dz = 0; dz < SiteSizeZ; dz++)
              for (int dx = 0; dx < SiteSizeX; dx++)
              {
                  int i = wx + dx + sx * (wz + dz);
                  if (!ok[i] || surface[i] != h)
                      return false;
              }
          return true;
      }

      static bool NearLandmark(IReadOnlyList<Landmark> landmarks, int wx, int wz)
      {
          for (int k = 0; k < landmarks.Count; k++)
          {
              var l = landmarks[k];
              if (l.X >= wx - LandmarkClearance && l.X < wx + SiteSizeX + LandmarkClearance
                  && l.Z >= wz - LandmarkClearance && l.Z < wz + SiteSizeZ + LandmarkClearance)
                  return true;
          }
          return false;
      }
      ```
      Cập nhật `<summary>` của lớp `SimDemo`: thêm câu "World nhiều map: FindSite + Build(sim, site) dời bố trí tới chỗ phẳng gần checkpoint Map 1 (world 7e, brief 0019)."
   Xong khi: biên dịch không lỗi; MCP `run_tests` EditMode lọc `SimDemoTests` đạt hết như trước.

6. **Tạo `client/Assets/Game/Tests/Core/Sim/SimBoundsTests.cs`** (namespace `Game.Core.Tests`, `using System; using System.Collections.Generic; using Game.Core.Sim; using NUnit.Framework;`). Có 6 test:
   1. `WholeBoundsMatchesWorld`: `pf = new GridPathfinder(TestWorlds.Flat(0))` → `pf.Bounds == new SimBounds(0, 0, 16, 16)`, `pf.CellCount == 16 * 16 * 16`.
   2. `RegionLimitsStandAndPath`:
      - Tạo `w = TestWorlds.Flat(0)`, `pf = new GridPathfinder(w, new SimBounds(4, 4, 8, 8))`. Kiểm `pf.CellCount == 8 * 8 * 16`.
      - `CanStand(1 × 1 × 1)`: tại (4, 1, 4) và (11, 1, 11) → true; tại (3, 1, 5) và (12, 1, 11) → false.
      - `FindPath((4, 1, 4) → (11, 1, 11))` → true, mọi ô trong đường có `Bounds.Contains`.
      - `FindPath((5, 1, 5) → (13, 1, 5))` → false.
      - `FloodRegion` từ (5, 1, 5) với `region = p => true` → 64 ô, tất cả trong vùng.
      - `AddTempBlock(0, 1, 0)` không ném.
      - `SetBlocked((0, 1, 0))` ném `ArgumentException`.
   3. `RegionPathEqualsWholeWorldPathInside`:
      - Tạo `w = TestWorlds.Flat(0)`, thêm `TestWorlds.Pillar(w, 6, 5, 2)`, `Pillar(w, 6, 6, 2)`, `Pillar(w, 6, 7, 2)`.
      - Tạo `whole = new GridPathfinder(w)` và `part = new GridPathfinder(w, new SimBounds(2, 2, 12, 12))`.
      - `FindPath((3, 1, 6) → (10, 1, 6))` trên cả hai → cùng true, **cùng số bước**; mọi ô của đường `part` nằm trong vùng.
      - Không so từng phần tử: thứ tự đẩy vào heap có thể khác vì ô ngoài vùng bị bỏ, nên A* có thể chọn một đường khác nhưng dài bằng nhau.
   4. `InvalidBoundsThrow`: `new GridPathfinder(TestWorlds.Flat(0), b)` ném `ArgumentException` với b = `(−1, 0, 8, 8)`, `(0, 0, 17, 8)`, `(10, 10, 8, 8)`. `new SimBounds(0, 0, 0, 4)` ném `ArgumentException`.
   5. `SimulationRejectsOutsideRegion`:
      - Tạo `w = TestWorlds.Flat(1)`, `sim = new Simulation(new SimConfig(), w, new SimBounds(0, 0, 8, 8))`. Kiểm `sim.Bounds == new SimBounds(0, 0, 8, 8)` và `sim.PathfinderCellCount == 8 * 8 * 16`.
      - `AddStorage((2, 2, 2))` ok (lấy id).
      - Các lệnh sau ném `ArgumentException`:
        - `AddCreature("L1", (10, 2, 10))`;
        - `AddStorage((10, 2, 10))`;
        - `AddHouse((10, 2, 10))`;
        - `AddFarmOrder(new[] { new CellPos(10, 1, 10) }, storage)`.
      - `AddFarmOrder(new[] { new CellPos(3, 1, 3) }, storage)` không ném.
   6. `RegionSizeValidated`: `new SimConfig { regionSize = 15 }.Validate()` và `regionSize = 257` ném `ArgumentException`; `regionSize = 64` không ném.
   Xong khi: `run_tests` EditMode lọc `SimBoundsTests` → 6/6.

7. **Tạo `client/Assets/Game/Tests/World/SimDemoMapsTests.cs`** (namespace `Game.World.Tests`, các `using` như `SimDemoTests.cs` + `System.Collections.Generic`, `System.Diagnostics`).
   - Hàm dựng chung `Setup(out WorldData world, out MapTable maps, out List<Landmark> lms, out SimConfig cfg, out DemoSite site, out double ms)`:
     - `var p = new WorldMapParams();`
     - `world = WorldMapGenerator.Generate(p, out maps);`
     - `lms = LandmarkPlacer.Place(world, maps, p);`
     - `cfg` đọc `design/data/sim_m1.json` như `SimDemoTests.NewSim`;
     - đo bằng `Stopwatch`: `site = SimDemo.FindSite(world, maps, lms, 0, p.waterLevel, cfg.regionSize);`.
   - Có 5 test:
   1. `SiteIsFlatDryInMap1AwayFromLandmarks`:
      - Cửa sổ từ `wx = site.Dx + SimDemo.BaseX − 1`, `wz = site.Dz + SimDemo.BaseZ − 1`, kích thước 9 × 7. Mỗi cột kiểm:
        - `maps.Get == 0`;
        - `ColumnInfo` → `surface == site.Surface`, `waterDepth == 0`, `surface ≥ p.waterLevel`;
        - vật liệu ở surface là Grass hoặc Dirt.
      - `site.Dy == site.Surface − SimDemo.BaseGround`.
      - Không dấu mốc nào trong cửa sổ nới 2 cột.
      - Về vùng: `site.Bounds.SizeX == SizeZ == 64`, `site.Bounds.FitsIn(world.Size)`, chứa cả 4 góc cửa sổ.
      - Ghi log: `TestContext.WriteLine` và `UnityEngine.Debug.Log($"SIMSITE {site}, cách checkpoint Map 1 {dist:F1} ô, tìm {ms:F1} ms")`. Trong đó `dist` là khoảng cách từ tâm cửa sổ (wx + 4,5; wz + 3,5) tới (cp.X + 0,5; cp.Z + 0,5), `cp = lms[0]`.
   2. `SiteIsDeterministic`: gọi `Setup` hai lần → hai `site` bằng nhau ở mọi trường (`Dx, Dy, Dz, Surface, Bounds`).
   3. `BuildPlacesShiftedLayout`:
      - Tạo `sim = new Simulation(cfg, world, site.Bounds)`, gọi `SimDemo.Build(sim, site)`.
      - Kiểm như `SimDemoTests.BuildPlacesLayout` (`:117-140`) nhưng mọi ô kỳ vọng qua `site.Shift(...)`:
        - 2 nhà, 1 kho, 10 hạt;
        - 3 sinh vật đúng loài, ô, nhà;
        - 4 ô ruộng.
      - `sim.Bounds == site.Bounds`.
      - `sim.PathfinderCellCount == 64 * 64 * world.Size.Y`. Ghi log `SIMMEM {sim.PathfinderCellCount} ô ≈ {sim.PathfinderCellCount * 18 / 1048576.0:F1} MB (cả world {world.Size.CellCount * 18 / 1048576.0:F1} MB)`.
   4. `OneRoundFillsStorageOnMaps`: như `SimDemoTests.OneRoundFillsStorage` trên `sim` ở test 3 (có hàm `Run` riêng chép từ `SimDemoTests:25-38`). Chạy 4000 nhịp, không `KeepFarming` → 0 lần `StallStarted`, `Produce == 4`, `Seeds == 10`.
   5. `KeepFarmingNoOverlapIdleNearHomeOnMaps`: chạy 8000 nhịp, mỗi nhịp gọi `KeepFarming`. Kiểm:
      - 0 lần `StallStarted`;
      - `Produce ≥ 8`;
      - 0 lần hai sinh vật không `Phasing` chồng khung (dùng `Simulation.FramesOverlap` như `SimDemoTests:56-67`);
      - 0 lần sinh vật `Idle` đứng trên ruộng (`sim.OnPlot`);
      - 0 lần sinh vật `Idle` ngoài `homeRadius` (`Simulation.HomeDistance`).
      Ghi log `SIMMAPS produce=… stalls=… overlaps=… idleOnPlot=… idleOutside=…`.
   Xong khi: `run_tests` EditMode lọc `SimDemoMapsTests` → 5/5.

8. **Commit:**
   1. `git status --short`: chỉ còn file trong Phạm vi. Commit code với message `brief 0019: vùng mô phỏng 64×64 + SimDemo tự tìm chỗ trong Map 1`.
   2. `date "+%Y-%m-%d %H:%M"` → ghi "Kết thúc", trạng thái `chờ review`, commit brief với message `brief 0019: kết quả`.

## Tiêu chí xong
- Biên dịch không lỗi; Game.Core vẫn No Engine References (không `using UnityEngine` trong `Core/`).
- EditMode cả bộ đạt: 311 + 6 + 5 = 322. Mọi test cũ (`GridPathfinderTests`, `Sim*Tests`, `SimDemoTests`, `SimDeterminismTests`, `GoldenHashTests`, `ConfigTests`…) đạt **mà không sửa**.
- PlayMode cả bộ 32/32, chạy để chắc `SimView` (đảo v2) không đổi.
- Hai file `sim_m1.json` giống từng byte.
- `git status` sạch sau commit.

## Test sau khi xong

### Phần cần test
| Phần | Bước | Test | Kết quả mong đợi |
|---|---|---|---|
| Vùng mặc định = cả world, không đổi hành vi | 1–3 | `WholeBoundsMatchesWorld` + mọi test mô phỏng cũ + `SimDeterminismTests` | Đạt, không sửa test cũ |
| Tìm đường trong vùng | 1–2 | `RegionLimitsStandAndPath`, `RegionPathEqualsWholeWorldPathInside`, `InvalidBoundsThrow` | Ngoài vùng không đứng/đi được; đường trong vùng dài bằng đường trên cả world; vùng sai ném lỗi; mảng = 8·8·16 |
| Simulation chặn ngoài vùng | 3 | `SimulationRejectsOutsideRegion` | Nhà/kho/sinh vật/ruộng ngoài vùng ném lỗi; trong vùng được |
| `regionSize` | 4 | `RegionSizeValidated`, `ConfigTests` | 15/257 ném; 64 đạt; 2 JSON giống nhau |
| Tìm chỗ đặt trại | 5 | `SiteIsFlatDryInMap1AwayFromLandmarks`, `SiteIsDeterministic` | 9 × 7 cột phẳng, khô, cỏ/đất, Map 1, xa dấu mốc; vùng 64 × 64 chứa cửa sổ; tất định |
| Bố trí dời + bộ nhớ | 5 | `BuildPlacesShiftedLayout` | Đúng bố trí gốc dời theo site; mảng tìm đường = 64·64·48 |
| Demo chạy trong Map 1 | 5 | `OneRoundFillsStorageOnMaps`, `KeepFarmingNoOverlapIdleNearHomeOnMaps` | 0 kẹt; đủ sản lượng; 0 chồng ô; rảnh không trên ruộng, không ngoài bán kính |
| Bố trí cũ không đổi | 5 | `SimDemoTests` (5 test cũ) | Đạt |
| Regression chung | — | EditMode cả bộ + PlayMode cả bộ | 322/322, 32/32 |

### Lệnh executor chạy
1. MCP `refresh_unity` → `read_console` (types error) → 0 lỗi.
2. `cmp design/data/sim_m1.json client/Assets/Game/Client/Resources/Config/sim_m1.json` → không in gì.
3. MCP `run_tests` EditMode lọc `SimBoundsTests`, `SimDemoMapsTests`, `GridPathfinderTests`, `SimDemoTests`, `ConfigTests` → đạt. Đọc console, lấy các dòng `SIMSITE`, `SIMMEM`, `SIMMAPS`.
4. MCP `run_tests` EditMode cả bộ → ghi dòng tổng kết.
5. MCP `run_tests` PlayMode cả bộ → ghi dòng tổng kết. Sau đó dọn: `git checkout -- client/ProjectSettings/EditorSettings.asset`.
6. `git status --short` → commit (bước 8).
7. `date "+%Y-%m-%d %H:%M"` → "Kết thúc"; commit brief.

### User tự test
Không có (chỉ lõi). Phần xem trại chạy trong Map 1 làm ở brief 0020.

## Dừng và chuyển lại Opus khi
- Brief mơ hồ hoặc mâu thuẫn với code thật (số dòng không khớp nội dung trích).
- `FindSite` ném `InvalidOperationException` (Map 1 không có chỗ phẳng 9 × 7).
- `SimDemoMapsTests` có kẹt (`StallStarted` > 0), chồng ô hoặc thiếu sản lượng. Không tự chỉnh bố trí hay luật.
- Một test cũ hỏng sau bước 2 hoặc 3 (vùng mặc định phải cho kết quả y hệt).
- Cần quyết định ngoài "Quyết định đã chốt", hoặc phải sửa file ngoài "Phạm vi".
- Cùng một lỗi sửa 2 lần vẫn chưa qua.

---
<!-- Từ đây trở xuống: EXECUTOR ghi. -->

## Kết quả
- Bắt đầu: 2026-10-21 11:59
- Base commit: a6acf32
- Chạy bằng: claude-sonnet-5-5 · medium

## Kết quả test
- Bước 1–7 đã viết (SimBounds, GridPathfinder, Simulation, regionSize + 2 JSON giống nhau [`cmp`], SimDemo.FindSite/DemoSite/Build, 2 file test), biên dịch không lỗi. **Chưa commit code** (để nguyên trong working tree cho Planner xem).
- EditMode lọc `SimBoundsTests`, `GridPathfinderTests`, `SimDemoTests`, `ConfigTests`: 22/22 đạt (gồm 6/6 `SimBoundsTests`, test cũ không sửa).
- `SimDemoMapsTests`: 0/5 — cả 5 ném `InvalidOperationException: Không có chỗ phẳng 9 × 7 cho trang trại demo trong map 0` từ `SimDemo.FindSite`. Chưa chạy EditMode/PlayMode cả bộ.

## Vướng mắc
Thời điểm dừng: 2026-10-21 12:02. Điều kiện dừng "FindSite ném InvalidOperationException".

**Dữ liệu** [run, test chẩn đoán tạm, đã xoá] — seed mặc định `new WorldMapParams()`, map 0: 18999 cột; khô (waterDepth 0, surface ≥ 12) 17854; mặt Grass/Dirt thuộc 17015 cột; checkpoint Map 1 ở (123, 211), surface 18. Cửa sổ 9 × 7 tốt nhất theo điều kiện "cùng độ cao": **49/63** cột (bỏ điều kiện độ cao thì có cửa sổ 63/63). Tức địa hình có đủ cỏ/đất khô nhưng không có mảng 63 cột bằng phẳng tuyệt đối (sinh theo noise, có bậc 1 ô).

**Câu hỏi:** nới điều kiện "cùng một độ cao" thế nào?
- (A) Cho phép chênh lệch: chọn cửa sổ mà các cột chênh tối đa 1 ô, rồi **san phẳng** (đào/đắp WorldData) cho khớp bố trí gốc. Đụng bộ sinh/editor, ngoài phạm vi hiện tại.
- (B) Cho phép chênh ≤ 1 ô, bố trí dời theo độ cao **mode/thấp nhất**, ô ruộng/nhà ở y khác nhau từng cột — phải đổi bố trí gốc (SimDemo.Build dùng y cố định) → ngoài "Quyết định đã chốt".
- (C) Cho `FindSite` tự san phẳng cửa sổ trước khi build (ghi `Cell.Make(Dirt/Air)` qua WorldData.Set) — đơn giản, tất định, nhưng sửa world sau Place; cần quyết về `Landmark.Surface` và minimap.
**Đề xuất:** (C), san cửa sổ 9 × 7 về độ cao `surface` phổ biến nhất (hạ cột cao, đắp Dirt cột thấp) bên trong `FindSite` hoặc hàm riêng `SimDemo.Flatten(world, site)`, điều kiện tìm: chênh ≤ 1 ô so với mode. Planner chốt rồi cập nhật brief.

---
<!-- PLANNER ghi khi review. -->

## Review
