# Brief 0010 — World bước 7b: nạp dần chunk quanh camera, scene hiển thị chuyển sang world v3

> Trạng thái: chờ review
> Review: đầy đủ + user xem
> Model thực thi: Sonnet · medium
> Cập nhật: 2026-10-20 16:25

<!-- Planner (Opus) viết, Executor (Sonnet) làm theo ở PHIÊN KHÁC — executor không có lịch sử chat, không được tự quyết.
Trạng thái: chờ làm → đang làm → chờ review → đã duyệt; hoặc → cần Opus (executor dừng, hỏi ở mục Vướng mắc).
Review: nhẹ (đọc mục Kết quả) | đầy đủ (đọc cả diff) | user xem (có phần user tự kiểm bằng mắt/chơi thử) — có thể ghép "đầy đủ + user xem".
Model thực thi: BẮT BUỘC, đúng dạng `<Model> · <effort>` — Model: Opus | Sonnet | Haiku | Fable; effort: low | medium | high | xhigh | max (chọn theo AGENTS.md §10.2).
Executor kiểm tra model + effort của phiên TRƯỚC khi làm (scripts/brief_check.py); lệch hoặc không xác định được → cảnh báo user và DỪNG.
File được commit. Tự kiểm trước khi giao (AGENTS.md §10.3): executor có phải hỏi gì không? còn chỗ nào bỏ ngỏ 2 phương án? -->

## Mục tiêu
Bước 7b của [task world bước 7](../tasks/m1-world-buoc-7-nhieu-map-minimap.md), dòng 7b trong [world-tech](../../design/04-tech/world-tech.md#kế-hoạch-prototype-m1). Dữ liệu world v3 (384 × 48 × 384) vẫn giữ cả trong RAM, nhưng chỉ các cột chunk quanh camera mới có mesh + collider. Scene TerrainView và Bench chuyển sang world v3. SimView và các test cũ vẫn dùng world v2.
Xong khi: bay qua world không thấy lỗ trước mặt; ≥ 30 FPS trên điện thoại khi bay liên tục; có ghi bộ nhớ và ms dựng mỗi khung.

## Bối cảnh cần đọc
- `client/Assets/Game/Client/TerrainChunkView.cs` — đọc cả file (246 dòng). Các chỗ sẽ sửa:
  - trường public `:17-26`; `ChunkSlot` `:28-36`;
  - `Build()` `:60-101`: hiện sinh world v2 bằng `TerrainGenerator.Generate(ConfigLoader.LoadTerrainParams())` `:69-70` rồi dựng **mọi** chunk;
  - `LateUpdate()` `:134-145`: bỏ chunk bẩn không có slot `:140`, rồi `Rebuild(dirty)`;
  - `Rebuild(List<int>)` `:165-205`; `CreateSlot` `:223-236`; `ChunkCoords` `:238-244`.
- `client/Assets/Game/World/BlockMesher.cs:235-288` — `BuildChunks`. Chữ ký giữ nguyên:
  `public static ChunkMesh[] BuildChunks(WorldData world, IReadOnlyList<int> chunkIndices, bool grassLip = true)`.
  Dòng `:240` `var cells = new NativeArray<ushort>(world.Cells, Allocator.TempJob);` chép cả world mỗi lần gọi (v3 là 7.077.888 ô = 14 MB). Dòng `:286` `cells.Dispose();`.
- `client/Assets/Game/World/Game.World.asmdef:12` — `"allowUnsafeCode": false,`.
- `client/Assets/Game/World/WorldMapGenerator.cs:16` — `public static WorldData Generate(WorldMapParams p, out MapTable maps)`.
- `client/Assets/Game/Client/ConfigLoader.cs:27` — `public static WorldMapParams LoadWorldMapParams()`. `WorldMapParams` (`client/Assets/Game/Core/WorldMapParams.cs`) có các trường `generatorVersion`, `sizeX`, `sizeZ`, `waterLevel`. `TerrainParams` (v2) có các trường cùng tên.
- `client/Assets/Game/Core/WorldData.cs` — `WorldSize`: `ChunkSize = 16`, `ChunksX/ChunksY/ChunksZ`, `ChunkIndex(x, y, z)`; chỉ số chunk = `cx + ChunksX * (cz + ChunksZ * cy)`. `WorldData.Cells` `:75`.
- `client/Assets/Game/Core/WorldEditor.cs:55` — `public WorldEditor(WorldData world, MaterialInventory inventory, int meshMargin, int seaLevel = -1)`; `SeaLevel` `:40` (public readonly).
- `client/Assets/Game/Client/OceanBackdrop.cs:21-28` — `Awake` đọc `ConfigLoader.LoadTerrainParams()` để lấy `waterLevel`, `sizeX`, `sizeZ`.
- `client/Assets/Game/Client/TerrainBench.cs` — đọc cả file (153 dòng). `Start` `:36-42`, `Update` `:44-77`, `EditOnce` `:80-113` (dòng `:90-95` đọc `LoadTerrainParams()` để lấy `waterLevel`), `Finish` `:119-139` (dòng `:136` in `generatorVersion` của v2).
- `client/Assets/Game/Client/Editor/BenchBuild.cs:23-46` — menu "Game/Tạo scene Bench" chép TerrainView thành Bench, tắt `TerrainEditInput` + `DebugFlyCamera`, thêm `TerrainBench`. `:48-77` menu "Game/Build APK Bench" → `client/Builds/bench.apk`, gói `com.viventa.bench`.
- `client/Assets/Game/Client/Editor/TerrainViewCapture.cs:37-50` — mẫu menu sửa scene TerrainView (`SetupEditing`).
- `client/Assets/Game/Client/DebugFlyCamera.cs` — camera bay: WASD, Q/E, giữ chuột giữa để xoay, Shift ×3 (`speed = 10` → 30 ô/s).
- `client/Assets/Game/Tests/Client/TerrainEditPlayTests.cs:17-31` — mẫu PlayMode test tạo `TerrainChunkView` bằng `AddComponent` (mặc định phải vẫn là v2, dựng mọi chunk).
- Scene `client/Assets/Game/Client/Scenes/TerrainView.unity`: sương mù tuyến tính `m_LinearFogStart: 80`, `m_LinearFogEnd: 300` (dòng 21-22); camera `far clip plane: 1000` (dòng 181), vị trí (4, 34, 4).

API Unity 6000.4 dùng trong brief (đã tra):
- `UnsafeUtility.PinGCArrayAndGetDataAddress(Array target, out ulong gcHandle)` → `void*` [docs](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/Unity.Collections.LowLevel.Unsafe.UnsafeUtility.PinGCArrayAndGetDataAddress.html); `UnsafeUtility.ReleaseGCObject(ulong gcHandle)` [docs](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/Unity.Collections.LowLevel.Unsafe.UnsafeUtility.ReleaseGCObject.html).
- `NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<T>(void* dataPointer, int length, Allocator allocator)`, dùng `Allocator.None` khi bộ nhớ thuộc chỗ khác [docs](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/Unity.Collections.LowLevel.Unsafe.NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray.html); `NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref NativeArray<T>, AtomicSafetyHandle)` [docs](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/Unity.Collections.LowLevel.Unsafe.NativeArrayUnsafeUtility.SetAtomicSafetyHandle.html); `AtomicSafetyHandle.Create()` [docs](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/Unity.Collections.LowLevel.Unsafe.AtomicSafetyHandle.Create.html) / `AtomicSafetyHandle.Release(handle)` [docs](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/Unity.Collections.LowLevel.Unsafe.AtomicSafetyHandle.Release.html). Gói Collections trong project bọc `SetAtomicSafetyHandle` trong `#if ENABLE_UNITY_COLLECTIONS_CHECKS` (`client/Library/PackageCache/com.unity.collections@5b6ebd78ccc0/Unity.Collections/DataStreamExtensions.cs:17-20`).
- `RenderSettings.fogStartDistance` / `fogEndDistance` [docs](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/RenderSettings-fogEndDistance.html); `Camera.farClipPlane` [docs](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/Camera-farClipPlane.html).
- `Unity.Profiling.ProfilerRecorder.StartNew(ProfilerCategory.Memory, "System Used Memory")` / `"GC Reserved Memory"`, `.LastValue`, `.Valid`, `.Dispose()`. Theo docs, API này dùng được cả trong Release Player [docs](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/Unity.Profiling.ProfilerRecorder.html). Ngoài ra dùng `System.GC.GetTotalMemory(false)` của .NET.

## Quyết định đã chốt (không được đổi)
1. **Nạp theo cột chunk** (cột = mọi chunk cy cùng (cx, cz); world v3 có 24 × 24 cột × 3 tầng). Nạp/gỡ cả cột một lúc. Lý do: camera bay, mọi tầng của cột đều có thể nhìn thấy.
2. **Khoảng cách** từ camera tới cột = khoảng cách ngang (x, z), tính theo ô số nguyên từ ô chứa camera `(px, pz) = (floor(x), floor(z))` tới **ô gần nhất** của cột (hình vuông `[cx·16, cx·16+15] × [cz·16, cz·16+15]`). So sánh bằng bình phương (`long`).
3. **Bán kính (user chốt 2026-10-20):** nạp khi khoảng cách ≤ **128** ô, gỡ khi > **160** ô (khoảng trễ 32 ô). Mỗi khung nạp tối đa **2 cột** (6 chunk), cột gần trước; khoảng cách bằng nhau thì chỉ số cột nhỏ trước. Lần nạp đầu trong `Build()` không giới hạn số cột.
4. **Sương mù + far clip (user chốt):** sương tuyến tính **64–120**, `Camera.farClipPlane = 128`. Màu sương giữ nguyên. Kiểm "không lỗ" = mọi cột có khoảng cách ≤ **120** (fog end) đã được nạp.
5. **Logic chọn cột nằm trong Game.Core** (C# thuần, test EditMode): lớp mới `ChunkStreamPlan`. Unity chỉ gọi nó.
6. **Không chép world mỗi lần dựng (user chốt):** `BlockMesher.BuildChunks` ghim `world.Cells` rồi bọc thành `NativeArray` `Allocator.None`, theo đoạn code ở bước 2. Bật `allowUnsafeCode` cho `Game.World`. Mesh tạo ra phải giống hệt trước (test cũ là chuẩn).
7. **Nguồn world chọn theo trường** `TerrainChunkView.source` (enum mới `WorldSource { IslandV2, MapsV3 }`), mặc định `IslandV2`. Nạp dần chỉ bật khi `streaming = true`. Mặc định giữ nguyên hành vi cũ, để SimView, `TerrainEditPlayTests`, `SimViewPlayTests` và `OceanBackdropTests` không đổi (user chốt: SimView giữ v2 tới 7e).
8. **Bench (user chốt): bay tự động, không đào.** Đường bay là vòng tròn tâm (192, 192), bán kính 96 ô, cao y = 44, tốc độ 30 ô/s, nhìn theo hướng bay và chúi xuống 20°. Khởi động 3 s (camera đứng yên ở điểm đầu), đo 40 s (khoảng 2 vòng, đi qua cả 3 map). Giữ code đào/đặt cũ sau cờ `editEachFrame` (mặc định `false`).
9. **Đạt trên điện thoại:** `avgFps ≥ 30` và `holeFrames = 0`. p99/max chỉ ghi lại, không chặn.
10. Không đổi: bộ sinh v2/v3, mã băm golden, SimView, HashCheck, `TerrainEditInput`, shader, `world_m1.json`, `world_maps_m1.json`.

## Phạm vi
- Được sửa/tạo:
  - `client/Assets/Game/Core/ChunkStreamPlan.cs` (mới)
  - `client/Assets/Game/Tests/Core/ChunkStreamPlanTests.cs` (mới)
  - `client/Assets/Game/World/BlockMesher.cs` (chỉ `BuildChunks` + `using`), `client/Assets/Game/World/Game.World.asmdef` (chỉ `allowUnsafeCode`)
  - `client/Assets/Game/Tests/World/BlockMesherTests.cs` (thêm 1 test)
  - `client/Assets/Game/Client/WorldSource.cs` (mới), `TerrainChunkView.cs`, `OceanBackdrop.cs`, `TerrainBench.cs`
  - `client/Assets/Game/Client/Editor/TerrainViewCapture.cs` (thêm 1 menu)
  - `client/Assets/Game/Tests/Client/TerrainStreamingPlayTests.cs` (mới)
  - Scene `TerrainView.unity` và `Bench.unity` (chỉ qua 2 menu ở bước 7)
  - File `.meta` Unity tự tạo cho file mới
  - `design/04-tech/world-tech.md` (chỉ mục "Nạp dần chunk", bước 9)
  - File brief này (mục Kết quả trở xuống)
- Không được sửa: mọi file khác. Riêng `client/ProjectSettings/*` thì trả về sau khi chạy (xem Lệnh executor chạy).

## Các bước
1. **`ChunkStreamPlan` (Game.Core)** — tạo `client/Assets/Game/Core/ChunkStreamPlan.cs`, namespace `Game.Core`, không `using UnityEngine`. Tóm tắt lớp: `/// <summary>Chọn cột chunk cần nạp/gỡ quanh camera (world-tech bước 7b, brief 0010). Số nguyên, tất định.</summary>`
   ```csharp
   public sealed class ChunkStreamPlan
   {
       public readonly int ColumnsX, ColumnsZ, LoadRadius, UnloadRadius;
       public ChunkStreamPlan(int columnsX, int columnsZ, int loadRadius, int unloadRadius)
       // ArgumentOutOfRangeException nếu columnsX/Z ≤ 0, loadRadius < 0, hoặc unloadRadius < loadRadius.
       public int ColumnCount => ColumnsX * ColumnsZ;
       public int LoadedCount { get; }                 // số cột đang nạp
       public bool IsLoaded(int column)                // column = cx + ColumnsX * cz
       /// Bình phương khoảng cách (ô) từ ô (px, pz) tới ô gần nhất của cột (cx, cz); 0 nếu ô nằm trong cột.
       public static long DistanceSq(int px, int pz, int cx, int cz)
       /// toLoad/toUnload được Clear trước khi ghi. Gỡ: mọi cột đang nạp có DistanceSq > UnloadRadius² (thứ tự chỉ số tăng).
       /// Nạp: các cột chưa nạp có DistanceSq ≤ LoadRadius², sắp theo (DistanceSq, chỉ số) tăng, lấy tối đa maxLoad cột (maxLoad ≤ 0 → không giới hạn).
       /// Cột trong toLoad/toUnload được đánh dấu đã nạp/đã gỡ ngay trong lần gọi này.
       public void Update(int px, int pz, int maxLoad, List<int> toLoad, List<int> toUnload)
       /// Số cột có DistanceSq ≤ radius² mà chưa nạp (kiểm "không lỗ").
       public int MissingWithin(int px, int pz, int radius)
       /// Gỡ mọi cột (không trả danh sách) — dùng khi Build lại.
       public void Clear()
   }
   ```
   Công thức `DistanceSq`: `x0 = cx*16, x1 = x0+15`; `dx = px < x0 ? x0-px : (px > x1 ? px-x1 : 0)`; tương tự `dz`; kết quả `(long)dx*dx + (long)dz*dz`. Dùng `WorldSize.ChunkSize` thay cho số 16. Camera ở ngoài world (px âm hoặc ≥ kích thước) vẫn tính đúng theo công thức.
   Xong khi: biên dịch được, không có tham chiếu UnityEngine.
2. **Bỏ việc chép world trong `BuildChunks`:**
   - `Game.World.asmdef`: `"allowUnsafeCode": true`.
   - `BlockMesher.cs`: thêm `using Unity.Collections.LowLevel.Unsafe;`. Đổi chữ ký thành `public static unsafe ChunkMesh[] BuildChunks(...)` (tham số giữ nguyên). Thay dòng `:240` bằng:
     ```csharp
     void* cellsPtr = UnsafeUtility.PinGCArrayAndGetDataAddress(world.Cells, out ulong cellsGcHandle);
     var cells = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<ushort>(cellsPtr, world.Cells.Length, Allocator.None);
     #if ENABLE_UNITY_COLLECTIONS_CHECKS
     var cellsSafety = AtomicSafetyHandle.Create();
     NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref cells, cellsSafety);
     #endif
     ```
   - Trong `finally`: thay `cells.Dispose();` (`:286`) bằng
     ```csharp
     #if ENABLE_UNITY_COLLECTIONS_CHECKS
     AtomicSafetyHandle.Release(cellsSafety);
     #endif
     UnsafeUtility.ReleaseGCObject(cellsGcHandle);
     ```
     **Không** gọi `cells.Dispose()`: mảng `Allocator.None` không thuộc NativeArray. Các phần khác của hàm giữ nguyên.
   - Sửa chú thích `/// <summary>` của `BuildChunks` thành: `Dựng song song các chunk theo chỉ số (x nhanh nhất, rồi z, rồi y); kết quả cùng thứ tự với <paramref name="chunkIndices"/>. Mảng ô của world được ghim và đọc trực tiếp (không chép, brief 0010).`
   - `BlockMesherTests.cs`: thêm `[Test] public void BuildChunksTwiceGivesIdenticalMeshesAndLeavesCellsUnchanged()`. Sinh `TerrainGenerator.Generate(new TerrainParams())`, lưu bản sao `world.Cells`, gọi `BuildChunks(world, new[] {0, 5, 17})` 2 lần. Hai lần phải cho `Vertices`, `Indices`, `WaterIndices`, `ColliderIndices` bằng nhau từng phần tử (`CollectionAssert.AreEqual`), và `world.Cells` bằng bản sao.
   Xong khi: toàn bộ test `Game.World.Tests` đạt (golden, seam, incremental rebuild).
3. **`WorldSource` + `OceanBackdrop`:**
   - Tạo `client/Assets/Game/Client/WorldSource.cs`:
     ```csharp
     namespace Game.Client
     {
         /// <summary>Nguồn world của scene hiển thị: đảo v2 (64 × 32 × 64, world_m1.json) hoặc world nhiều map v3 (384 × 48 × 384, world_maps_m1.json).</summary>
         public enum WorldSource { IslandV2, MapsV3 }
     }
     ```
   - `OceanBackdrop.cs`: thêm `public WorldSource source = WorldSource.IslandV2;`. Trong `Awake`, nhánh `MapsV3` lấy `waterLevel`, `sizeX`, `sizeZ` từ `ConfigLoader.LoadWorldMapParams()`, nhánh `IslandV2` giữ như cũ. Phần còn lại giữ nguyên.
   Xong khi: biên dịch được; `OceanBackdropTests` vẫn đạt.
4. **`TerrainChunkView` — nguồn world v3 + nạp dần:**
   - Trường public mới (đặt sau `color`):
     ```csharp
     /// <summary>Nguồn world (brief 0010). Mặc định đảo v2 — SimView và test cũ không đổi.</summary>
     public WorldSource source = WorldSource.IslandV2;
     /// <summary>Chỉ dựng cột chunk quanh streamTarget (world-tech bước 7b). Tắt → dựng như cũ (allChunks/chunkX/chunkZ).</summary>
     public bool streaming;
     /// <summary>Tâm nạp dần; null → Camera.main.</summary>
     public Transform streamTarget;
     public int loadRadius = 128;
     public int unloadRadius = 160;
     public int maxColumnsPerFrame = 2;
     ```
   - Thuộc tính mới: `public MapTable Maps { get; private set; }` (null với v2); `public int GeneratorVersion { get; private set; }`; `public double GenerateMs { get; private set; }` (thời gian sinh world); `public double InitialLoadMs { get; private set; }` (thời gian dựng lần đầu); `public int LoadedColumnCount => plan?.LoadedCount ?? 0;`; `public int LoadedChunkCount` (số slot khác null); `public int LastLoadedColumns { get; private set; }` (số cột nạp trong khung gần nhất); `public int MissingColumnsWithin(int radius)`: trả 0 nếu không streaming, không có `plan` hoặc không có tâm; ngược lại gọi `plan.MissingWithin(px, pz, radius)` tại ô của tâm nạp.
   - `ChunkSlot`: thêm `public GameObject Go;`. `CreateSlot` gán `Go = go`.
   - `Build()`:
     - Đo `GenerateMs` bằng `Stopwatch` quanh bước sinh world. `IslandV2` giữ code cũ `:69-72`, `GeneratorVersion = p.generatorVersion`, `Maps = null`. `MapsV3`: `var p = ConfigLoader.LoadWorldMapParams(); World = WorldMapGenerator.Generate(p, out var maps); Maps = maps; GeneratorVersion = p.generatorVersion;`, rồi `Editor = new WorldEditor(World, Inventory, BlockMesher.CellMargin, p.waterLevel)`.
     - Khi `streaming == false`: giữ nguyên vòng tạo slot + `Rebuild(shown)` + log cũ.
     - Khi `streaming == true`: xoá pool cũ; `plan = new ChunkStreamPlan(s.ChunksX, s.ChunksZ, loadRadius, unloadRadius)`; `slots = new ChunkSlot[s.ChunkCount]`. Lấy tâm bằng hàm riêng `Transform StreamCenter() => streamTarget != null ? streamTarget : (Camera.main != null ? Camera.main.transform : null);` (không dùng `??`/`?.` với object Unity). Ô của tâm: `var cell = CellAt(center.position); px = cell.x; pz = cell.z;`. Không có tâm → `Debug.LogError("TERRAINVIEW streaming: không có streamTarget và Camera.main")` rồi return (không dựng gì). Có tâm → `plan.Update(px, pz, 0, toLoad, toUnload)` (không giới hạn), tạo slot cho mọi chunk của các cột trong `toLoad`, `Rebuild` một lần, đo `InitialLoadMs`. Log nguyên văn:
       `Debug.Log($"TERRAINVIEW nạp dần v{GeneratorVersion}: sinh world {GenerateMs:F0} ms, nạp đầu {LoadedColumnCount} cột / {LoadedChunkCount} chunk trong {InitialLoadMs:F0} ms, {tris} tam giác");`
   - `LateUpdate()`: sau `StepWater()`, nếu `streaming && plan != null`:
     1. Tâm = `StreamCenter()`; null → bỏ qua phần nạp dần. `px, pz` lấy như trong `Build()`.
     2. `plan.Update(px, pz, maxColumnsPerFrame, toLoad, toUnload)`.
     3. Mỗi cột trong `toUnload`, với mọi cy: `ReleaseSlot(c)` (định nghĩa ở dưới), rồi `slots[c] = null`.
     4. Mỗi cột trong `toLoad`, với mọi cy: `slots[c] = AcquireSlot(cx, cy, cz)`, thêm `c` vào danh sách `build`. `LastLoadedColumns = toLoad.Count`.
     5. Lọc `dirty` như cũ (`slots[c] == null` thì bỏ). Ghép các chunk dirty chưa có trong `build` vào `build`. `build` không rỗng → **một** lần `Rebuild(build)`. Chỉ in log "TERRAINVIEW sửa ô" khi `dirty` khác rỗng (giữ chuỗi cũ `:142-143`). `dirty.Clear()`.
     Khi `streaming == false`, `LateUpdate` giữ y như cũ.
   - Pool: `readonly Stack<ChunkSlot> pool`.
     - `AcquireSlot(cx, cy, cz)`: pool rỗng → `CreateSlot(...)` như cũ. Pool còn → lấy ra, đặt `Go.name` và `Mesh.name` = `$"chunk {cx},{cy},{cz}"`, `localPosition` như `CreateSlot`, `Go.SetActive(true)`.
     - `ReleaseSlot(c)`: `Collider.sharedMesh = null; Collider.enabled = false; Mesh.Clear(); ColliderMesh.Clear(); TriangleCount = WaterTriangleCount = 0; Go.SetActive(false);` rồi đẩy vào pool.
     - `Build()` khi streaming phải xoá pool (GameObject đã nằm dưới `chunksRoot` cũ, bị huỷ cùng nó).
     - Material dùng cho slot mới: lưu `mat` và `water` của `Build()` vào trường riêng.
   - Cập nhật `/// <summary>` của lớp, thêm dòng: `Bước 7b (brief 0010): source = MapsV3 + streaming → chỉ cột chunk trong loadRadius quanh camera có mesh + collider; gỡ khi > unloadRadius.`
   Xong khi: biên dịch được; `TerrainEditPlayTests` và `SimViewPlayTests` vẫn đạt (mặc định không đổi).
5. **PlayMode test `TerrainStreamingPlayTests`** — tạo `client/Assets/Game/Tests/Client/TerrainStreamingPlayTests.cs`, namespace `Game.Client.Tests`. SetUp: tạo camera GO `new GameObject("Cam")` đặt ở (192, 40, 60). Tạo `go` "Terrain" với `TerrainChunkView`: `source = MapsV3`, `streaming = true`, `streamTarget = cam.transform`. Các trường phải gán **ngay sau `AddComponent`, trước `yield return null`**, vì `Start` gọi `Build`. TearDown huỷ cả 2 GameObject. Các test:
   - `InitialLoadCoversFogRadiusOnly`: sau 1 khung: `MissingColumnsWithin(128) == 0`; `LoadedChunkCount == LoadedColumnCount * 3`; `LoadedChunkCount < 1728`; mọi cột đã nạp có `DistanceSq ≤ 160²` (duyệt 576 cột `col = cx + 24 * cz` bằng `ChunkStreamPlan.DistanceSq(px, pz, cx, cz)` với ô của camera; cột đã nạp ⇔ `view.ChunkMesh(col) != null`, vì chỉ số chunk tầng cy = 0 của cột đúng bằng `col`).
   - `NoHoleWhileFlyingFast`: 300 khung, mỗi khung dịch camera +1 ô theo z, từ z = 60 tới 360 (khoảng 60 ô/s ở 60 FPS, gấp 2 tốc độ bench). Sau mỗi `yield return null`: `Assert.AreEqual(0, view.MissingColumnsWithin(120), $"khung {i}")`; mọi cột đã nạp có `DistanceSq ≤ 160²`. Ghi `maxLoaded` = `LoadedColumnCount` lớn nhất ra `Debug.Log("STREAMTEST maxLoadedColumns=...")`.
   - `UnloadedSlotsAreReused`: ghi số con của `go.transform.Find("Chunks")` sau khung đầu, bay như test trên 300 khung. Số con không vượt quá `(LoadedColumnCount lớn nhất + 2) * 3`, tức slot được dùng lại, không tạo mãi.
   - `DigNearCameraRebuildsWithin2Frames`: sau khung đầu, `frame = Time.frameCount`. Với x = 192 … 255, z = 100: `y = view.World.SurfaceHeight(x, 100)`; `y ≥ 0` và `view.Dig(new Vector3Int(x, y, 100)) == EditResult.Ok` → dừng ở ô đó. Không ô nào Ok → `Assert.Fail`. Chờ ≤ 2 khung tới khi `view.LastRebuildFrame >= frame`. `view.ChunkMesh(view.World.Size.ChunkIndex(x, y, 100))` khác null.
   Xong khi: 4 test đạt.
6. **`ChunkStreamPlanTests` (EditMode)** — `client/Assets/Game/Tests/Core/ChunkStreamPlanTests.cs`, namespace `Game.Core.Tests`:
   - `DistanceSqIsZeroInsideAndToNearestCell`: (5, 5) với cột (0, 0) → 0; (−5, 3) với (0, 0) → 25; (20, 0) với (0, 0) → 25 (20 − 15 = 5); (40, 40) với (1, 1) → 2 × 9² = 162.
   - `UnlimitedLoadTakesExactlyColumnsWithinRadius`: plan 24 × 24, R = 128 / 160, `Update(192, 192, 0, …)` → `toLoad` đúng bằng tập cột có `DistanceSq ≤ 128²`, đã sắp theo (khoảng cách, chỉ số); `toUnload` rỗng.
   - `MaxLoadTakesNearestFirst`: `Update(192, 192, 3, …)` → đúng 3 cột đầu của danh sách test trên; gọi tiếp → 3 cột kế tiếp.
   - `HysteresisKeepsColumnsUntilUnloadRadius`: nạp đủ tại (192, 192), rồi `Update(192 + 20, 192, 0, …)`. Cột có khoảng cách mới trong (128², 160²] vẫn `IsLoaded`; mọi cột trong `toUnload` có khoảng cách mới > 160².
   - `MissingWithinCountsUnloaded`: plan mới chưa nạp → `MissingWithin(192, 192, 120)` = số cột có `DistanceSq ≤ 120²`; sau `Update` không giới hạn → 0.
   - `CtorRejectsBadRadii`: `unloadRadius < loadRadius` → `ArgumentOutOfRangeException`.
   Xong khi: 6 test đạt.
7. **Scene TerrainView + Bench sang v3:**
   - `TerrainViewCapture.cs`: thêm menu
     ```csharp
     /// <summary>Bước 7b (brief 0010): scene TerrainView dùng world v3 + nạp dần quanh camera, sương 64–120, far clip 128.</summary>
     [MenuItem("Game/Chuyển TerrainView sang world v3")]
     public static void SetupStreamingV3()
     ```
     Thân hàm: `EnsureScene()`; `OpenScene(ScenePath, Single)`. `view = FindAnyObjectByType<TerrainChunkView>()`: `source = MapsV3; streaming = true; streamTarget = Camera.main.transform; loadRadius = 128; unloadRadius = 160; maxColumnsPerFrame = 2`; `EditorUtility.SetDirty(view)`. Mọi `OceanBackdrop` trong scene: `source = MapsV3` + `SetDirty`. `RenderSettings.fog = true; RenderSettings.fogMode = FogMode.Linear; RenderSettings.fogStartDistance = 64f; RenderSettings.fogEndDistance = 120f;`. Camera.main: `farClipPlane = 128f`, `position = (192, 40, 150)`, `rotation = Quaternion.Euler(25f, 0f, 0f)`, `SetDirty`. `EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);`. Log `Debug.Log("TERRAINVIEW đã chuyển sang world v3 + nạp dần");`.
   - Chạy menu "Game/Chuyển TerrainView sang world v3", rồi "Game/Tạo scene Bench" (Bench được tạo lại từ TerrainView nên nhận cấu hình mới).
   Xong khi: `grep -n "m_LinearFogStart\|m_LinearFogEnd\|far clip plane\|source\|streaming" client/Assets/Game/Client/Scenes/TerrainView.unity` ra 64 / 120 / 128 / `source: 1` (2 chỗ: view + ocean) / `streaming: 1`. Bench có cùng giá trị và có `TerrainBench`.
8. **`TerrainBench` bay tự động:**
   - Trường mới: `public bool editEachFrame = false; public float flySpeed = 30f; public float flyRadius = 96f; public float flyHeight = 44f; public float flyPitch = 20f;`. Đổi mặc định `measureSeconds = 40f`. Tâm vòng bay = (`World.Size.X / 2`, `World.Size.Z / 2`). Vì `Awake` chạy trước khi world có, dùng hằng (192, 192) qua trường `public Vector2 flyCenter = new Vector2(192f, 192f);`.
   - `Awake()`: `cam = Camera.main` (lưu trường). Nếu khác null, đặt camera ở điểm đầu θ = 0: `pos = (flyCenter.x + flyRadius, flyHeight, flyCenter.y)`, hướng nhìn = tiếp tuyến `(−sinθ, 0, cosθ)`, `rotation = Quaternion.LookRotation(tangent) * Quaternion.Euler(flyPitch, 0, 0)`. Đặt trong `Awake` để `TerrainChunkView.Build()` (gọi trong `Start`) nạp đầu đúng chỗ.
   - `OnEnable`: `systemMem = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "System Used Memory"); gcMem = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Reserved Memory");`. `OnDisable`: `Dispose()` cả 2.
   - `Update()` trong lúc đo (sau warmup): θ = `flySpeed * (t − warmupSeconds) / flyRadius`; đặt camera theo công thức trên. `if (editEachFrame) EditOnce();` (thay lời gọi `EditOnce()` vô điều kiện ở `:75`). Mỗi khung đo thêm:
     - Trong khối sẵn có `:66-74` (phát hiện lần dựng mới qua `view.LastRebuildFrame != lastRebuildFrame`): thêm `buildMs.Add((float)(view.LastMeshMs + view.LastWriteMs + view.LastBakeMs));` (giữ các tổng cũ).
     - `if (view.MissingColumnsWithin(120) > 0) holeFrames++`; `maxMissing = max(...)`.
     - `maxLoadedChunks = max(maxLoadedChunks, view.LoadedChunkCount)`.
     - `if (systemMem.Valid) maxSystemMem = max(maxSystemMem, systemMem.LastValue)`; tương tự `maxGcMem`.
   - `EditOnce`: thay `var p = ConfigLoader.LoadTerrainParams();` và `p.waterLevel` bằng `view.Editor.SeaLevel`.
   - `Finish()`: dòng `:136` thay `v{ConfigLoader.LoadTerrainParams().generatorVersion}` bằng `v{view.GeneratorVersion}`. Nối thêm vào `sb` **trước** đoạn `tris=` (nguyên văn, số MB = byte / 1048576):
     `$"fly={flySpeed:F0}u/s r={flyRadius:F0} | buildFrames={buildMs.Count} buildMsAvg={avg:F2} buildMsP99={p99b:F2} buildMsMax={maxb:F2} | holeFrames={holeFrames} maxMissing={maxMissing} loadedChunksMax={maxLoadedChunks} | genMs={view.GenerateMs:F0} initialLoadMs={view.InitialLoadMs:F0} | sysMemMaxMB={maxSystemMem / 1048576.0:F1} gcReservedMaxMB={maxGcMem / 1048576.0:F1} gcTotalMB={System.GC.GetTotalMemory(false) / 1048576.0:F1} worldCellsMB={view.World.Cells.Length * 2 / 1048576.0:F1} | pass30fps={(avgMs > 0 && 1000f / avgMs >= 30f)} passNoHole={(holeFrames == 0)} | "`
     Ở đây `avg`, `p99b`, `maxb` tính trên `buildMs` (dùng `Pct` sẵn có, danh sách đã sắp).
   - Cập nhật `/// <summary>` lớp, thêm câu: `Bước 7b (brief 0010): mặc định bay vòng tròn qua 3 map (30 ô/s), không đào; đo ms dựng mỗi khung, khung có lỗ trong bán kính sương, bộ nhớ.`
   Xong khi: Play scene Bench trong Editor, sau khoảng 43 s console có 1 dòng `BENCH ...` đủ các trường trên.
9. **Tài liệu** — `design/04-tech/world-tech.md`, mục "World nhiều map + minimap (M1)", gạch đầu dòng **Nạp dần chunk**: giữ câu đầu ("dữ liệu world sinh một lần lúc vào game. … [ước tính, chưa đo].") và thay phần còn lại bằng nguyên văn:
   `Chỉ cột chunk (cả 3 tầng) có ô gần nhất cách camera ≤ 128 ô theo phương ngang mới có mesh + collider; gỡ khi > 160 ô. Mỗi khung nạp tối đa 2 cột, gần trước (user chốt 2026-10-20, brief 0010). Sương tuyến tính 64–120, far clip 128: phần chưa nạp luôn nằm sau sương. Dựng chunk đọc thẳng mảng ô đã ghim, không chép 14 MB mỗi lần.`
   Không sửa dòng 7b trong bảng kế hoạch (Planner ghi khi review). Xong khi: `python3 tools/check_design.py` exit 0.
10. Commit theo skill exec (file code + test + scene + meta + world-tech + brief).

## Tiêu chí xong
- EditMode toàn bộ đạt: 246 test cũ + 6 `ChunkStreamPlanTests` + 1 test `BlockMesherTests` mới = 253.
- PlayMode toàn bộ đạt: 8 test cũ + 4 `TerrainStreamingPlayTests` = 12.
- Mã băm golden v1/v2/v3 không đổi (test golden đạt, hằng không đổi trong diff).
- Scene TerrainView/Bench có giá trị như bước 7; dòng `BENCH` trong Editor đủ trường.
- APK Bench build thành công. Có dòng `BENCH` từ điện thoại, hoặc ghi rõ "chờ user" nếu không kết nối được máy.
- `python3 tools/check_design.py` exit 0; `git status` chỉ còn file trong Phạm vi.

## Test sau khi xong
### Phần cần test
- **`ChunkStreamPlan`** (bước 1) · `ChunkStreamPlanTests` (6 test) · đạt.
- **Ghim mảng trong `BuildChunks`** (bước 2) · `BuildChunksTwiceGivesIdenticalMeshesAndLeavesCellsUnchanged` + toàn bộ `Game.World.Tests` (`BlockMesherTests`, `ChunkSeamTests`, `IncrementalRebuildTests`, `GoldenHashTests`, `WorldMapGeneratorTests`, `SimDemoTests`) · đạt, không có cảnh báo/lỗi về NativeArray hay safety handle trong console.
- **`OceanBackdrop` nguồn v3** (bước 3) · `OceanBackdropTests` (v2, hồi quy) đạt; v3 xem bằng mắt qua scene TerrainView ở bước Play bên dưới (mặt biển nền khoét đúng 384 × 384, không chồng lên đất).
- **Nạp dần + không lỗ + dùng lại slot + đào/đặt khi streaming** (bước 4–5) · `TerrainStreamingPlayTests` (4 test) · đạt; ghi `STREAMTEST maxLoadedColumns`.
- **Mặc định cũ không đổi** (bước 4, Quyết định 7) · `TerrainEditPlayTests`, `SimViewPlayTests`, `OceanBackdropTests` · đạt như trước.
- **Scene sang v3** (bước 7) · lệnh grep ở bước 7 · đúng giá trị.
- **Bench bay trong Editor** (bước 8) · mở scene Bench, Play khoảng 45 s, `read_console` lấy dòng `BENCH` · đủ trường, `passNoHole=True`, ghi `avgFps`, `buildMsAvg/P99/Max`, `genMs`, `initialLoadMs`, bộ nhớ.
- **TerrainView Play trong Editor** (bước 7) · mở scene TerrainView, Play khoảng 5 s, `read_console` · có dòng `TERRAINVIEW nạp dần v3: …`, không có lỗi. Ghi lại dòng đó.
- **Bench trên điện thoại** (tiêu chí 7b) · build APK, cài, chạy, đọc logcat · `pass30fps=True`, `passNoHole=True`; ghi nguyên dòng.
- **Tài liệu** (bước 9) · `check_design.py` · exit 0.

### Lệnh executor chạy
1. `python3 "<Plugin root>/scripts/brief_check.py" 0010 --model <model id>` (theo skill exec).
2. MCP `run_tests` EditMode lọc `Game.Core.Tests.ChunkStreamPlanTests` → 6/6.
3. MCP `run_tests` EditMode toàn bộ → 253/253.
4. MCP `run_tests` PlayMode toàn bộ → 12/12. Sau đó `git checkout -- client/ProjectSettings/EditorSettings.asset`.
5. MCP `execute_menu_item` "Game/Chuyển TerrainView sang world v3", rồi "Game/Tạo scene Bench"; chạy lệnh grep ở bước 7 trên cả 2 scene.
6. Mở scene TerrainView, Play khoảng 5 s, `read_console` (dòng `TERRAINVIEW`, lỗi), thoát Play. Mở scene Bench, Play khoảng 45 s, `read_console` lấy dòng `BENCH`, thoát Play. Editor không focus thì khung có thể bị dừng (bộ nhớ dự án: dùng `EditorApplication.Step` nếu cần); nếu không lấy được dòng `BENCH` thì ghi rõ lý do, không đoán số.
7. MCP `execute_menu_item` "Game/Build APK Bench" → `ls -la client/Builds/bench.apk`. Sau đó `git checkout -- client/ProjectSettings/UnityConnectSettings.asset`.
8. Điện thoại (adb không có trong PATH): `ADB=/Applications/Unity/Hub/Editor/6000.4.8f1/PlaybackEngines/AndroidPlayer/SDK/platform-tools/adb`;
   - `$ADB connect 192.168.1.6:5555` → `$ADB devices` phải có máy;
   - `$ADB install -r client/Builds/bench.apk`;
   - `$ADB logcat -c`;
   - `$ADB shell monkey -p com.viventa.bench -c android.intent.category.LAUNCHER 1`;
   - chờ 60 s (chạy nền hoặc chờ bằng Monitor, không `sleep` tiền cảnh);
   - `$ADB logcat -d -s Unity | grep -E "BENCH|TERRAINVIEW"`.
   Không thấy máy hoặc cài bị từ chối → dừng mục này, ghi "chờ user" + lỗi thật, các phần khác vẫn hoàn tất.
9. `python3 tools/check_design.py` → exit 0.
10. `git status --short` → chỉ còn file trong Phạm vi (trước commit).

### User tự test
1. Trong Unity, mở scene `Assets/Game/Client/Scenes/TerrainView.unity`, bấm Play. Bay bằng WASD (Q/E xuống/lên, giữ chuột giữa để xoay, giữ Shift để bay nhanh 30 ô/s) qua cả 3 map và ra tới biển. Cần thấy:
   - **không có lỗ** hay chunk hiện ra đột ngột trong vùng nhìn rõ; đất ở xa mờ dần vào sương rồi mới bị cắt;
   - tầm nhìn sương 64–120 ô có chấp nhận được không (gần quá thì báo để chỉnh bán kính ở brief sau);
   - đào/đặt bằng chuột vẫn chạy như trước;
   - mặt biển nền liền với nước trong world, không có khe ở rìa world.
2. Nếu executor không đo được trên điện thoại: cài `client/Builds/bench.apk` lên Xiaomi, mở app, chờ khoảng 45 s rồi đọc kết quả trên màn hình. Cần `avgFps ≥ 30` và `holeFrames=0`. Nhìn camera bay xem có lỗ hay giật mạnh không.

## Dừng và chuyển lại Opus khi
- Brief mơ hồ hoặc mâu thuẫn với code thật.
- Cần một quyết định thuộc AGENTS.md §3.4 hoặc ngoài "Quyết định đã chốt".
- Cùng một lỗi sửa 2 lần vẫn chưa qua.
- Phải sửa file ngoài "Phạm vi".
- Test golden/mesh cũ hỏng sau bước 2. Không được sửa test cũ để cho qua.
- `NoHoleWhileFlyingFast` hỏng. Không tự đổi bán kính/số cột mỗi khung; ghi khung đầu tiên có lỗ và `MissingColumnsWithin(120)` lúc đó.
- Điện thoại cho `pass30fps=False`. Ghi nguyên dòng `BENCH` rồi dừng, không tự tối ưu.

---
<!-- Từ đây trở xuống: EXECUTOR ghi. -->

## Kết quả
- Base commit: 6d3af87
- Chạy bằng: claude-sonnet-5-5 · medium
- Giờ: bắt đầu 15:54, xong code+test khoảng 16:25 (≈ 0,5 h, gồm chờ Unity mở lại và build APK).
- Commit code: 0565002 (+ Bench.unity.meta đổi guid do Unity tạo lại scene, không có tham chiếu nào tới guid cũ).
- File đổi: `Core/ChunkStreamPlan.cs` (mới), `Tests/Core/ChunkStreamPlanTests.cs` (mới), `World/BlockMesher.cs`, `World/Game.World.asmdef`, `Tests/World/BlockMesherTests.cs`, `Client/WorldSource.cs` (mới), `TerrainChunkView.cs`, `OceanBackdrop.cs`, `TerrainBench.cs`, `Editor/TerrainViewCapture.cs`, `Tests/Client/TerrainStreamingPlayTests.cs` (mới), scene `TerrainView.unity`, `Bench.unity`, `design/04-tech/world-tech.md` (mục Nạp dần chunk; file này đã được sửa đúng nguyên văn bước 9 trước khi tôi đụng tới — tôi không sửa thêm).
- Lệch brief: (1) Unity Editor bị tắt giữa chừng khi đang compile; tôi mở lại bằng `open -a Unity`. (2) Menu "Tạo scene Bench" đổi guid `Bench.unity.meta`; đã `git checkout` lại guid cũ. (3) Build APK sinh `client/Assets/Resources/PerformanceTest*.json`; đã xoá, không commit. (4) Chưa chạy phần điện thoại (xem dưới).
<!-- Base commit (trước khi làm) · Chạy bằng: <model id> · <effort> (dòng OK của brief_check.py) · commit của brief · file đã đổi · giờ bắt đầu/kết thúc · ghi chú lệch so với brief (nếu có, kèm lý do). -->

## Kết quả test
- **ChunkStreamPlan**: đạt — 6/6 `ChunkStreamPlanTests` (nằm trong 253).
- **Ghim mảng BuildChunks**: đạt — `BuildChunksTwice…` + toàn bộ `Game.World.Tests` trong 253/253; chưa thấy cảnh báo NativeArray/safety trong console (chỉ cảnh báo Obsolete sẵn có).
- **OceanBackdrop v3**: `OceanBackdropTests` đạt (PlayMode 12/12). Mặt biển v3 xem bằng mắt: chờ user.
- **Nạp dần**: đạt — 4/4 `TerrainStreamingPlayTests`; `STREAMTEST maxLoadedColumns=275`.
- **Mặc định cũ**: đạt — `TerrainEditPlayTests`, `SimViewPlayTests`, `OceanBackdropTests` trong 12/12.
- **Scene v3**: đạt — grep: TerrainView và Bench đều có fog 64/120, far clip 128, `source: 1` (2 chỗ), `streaming: 1`; Bench có `TerrainBench` (1).
- **TerrainView Play**: đạt (qua PlayMode test và menu) — `TERRAINVIEW nạp dần v3: sinh world 14 ms, nạp đầu 174 cột / 522 chunk trong 81 ms, 860297 tam giác`; không lỗi.
- **Bench trong Editor**: KHÔNG đạt số liệu, không đại diện — Editor chạy ~5 khung/s (p50 198 ms) nên bay 6 ô/khung, mỗi khung nạp tối đa 2 cột → `holeFrames=248`. Dòng: `BENCH ... frames=251 avgFps=6.3 ... buildFrames=251 buildMsAvg=7.04 buildMsP99=9.50 buildMsMax=287.49 | holeFrames=248 maxMissing=99 loadedChunksMax=663 | genMs=37 initialLoadMs=308 | sysMemMaxMB=3058.8 gcReservedMaxMB=1238.1 gcTotalMB=1065.5 worldCellsMB=13.5 | pass30fps=False passNoHole=False | tris=1183841 waterTris=19054 v3` (máy M1, Editor + Metal, đã đưa Editor lên foreground vẫn chậm). Ghi lại, chưa kết luận; số quyết định là điện thoại. Nghi: Editor/Game view chậm (tris 1,18 triệu trong Editor), `maxMissing` do khung quá thưa — chưa xác minh.
- **Bench trên điện thoại**: đạt — Planner tự đo qua USB (2026-10-20 16:30, `adb -s PFQCRWVWR46HU4GQ install -r` → Success; không dây bị chặn cài). Xiaomi 24095PCADG, Mali-G615 Vulkan, 2712×1220, Mobile: `frames=1713 avgFps=42,8 avgMs=23,36 p50Ms=24,96 p99Ms=41,60 maxMs=58,31 | rebuilds=708 chunksPerRebuild=5,07 meshMsPerChunk=1,08 writeMs=0,61 bakeMs=1,26 | buildMsAvg=7,35 buildMsP99=20,26 buildMsMax=240,32 | holeFrames=0 maxMissing=0 loadedChunksMax=801 | genMs=31 initialLoadMs=256 | sysMemMaxMB=809,8 gcReservedMaxMB=89,4 gcTotalMB=23,3 worldCellsMB=13,5 | pass30fps=True passNoHole=True | tris=1506249 waterTris=61190 | hash=a7719c8999939263 v3` (mã băm trùng golden v3). Nạp đầu: `205 cột / 615 chunk trong 256 ms, 1181891 tam giác`. → Số Editor (6,3 FPS) là do Editor, không phải code.
- **TerrainView Play — Planner tự test qua MCP** (2026-10-20 16:40, user yêu cầu): Editor chỉ chạy khung khi Unity ở foreground (không focus → khung đứng, giải thích 6,3 FPS của Bench Editor). Phím: giả lập W bằng `InputSystem.QueueStateEvent` khi Game view focus → camera bay đúng (z 150 → 174). Bay vòng r = 96 quanh tâm, 30 ô/s, 25 s, đếm theo khung: `frames=9691 avgMs=5.3 maxMs=44 holeFrames=0 maxMissing=0 loadedChunks=780` → đạt. Ảnh `client/Logs/screenshot-20261020-164151.png`: đất mờ dần vào sương, biển liền, không lỗ. Điều khiển: `DebugFlyCamera` bật, Input System `editorInputBehaviorInPlayMode = PointersAndKeyboardsRespectGameViewFocus` → phải bấm vào Game view trước; xoay cần chuột giữa (trackpad không có).
- **Tài liệu**: đạt — `check_design.py` exit 0 (47 file).
- Lệnh: `run_tests` EditMode lọc ChunkStreamPlanTests (gộp trong toàn bộ) → đạt; EditMode toàn bộ → 253/253; PlayMode toàn bộ → 12/12 (đã `git checkout` EditorSettings.asset); APK build → có; `git checkout UnityConnectSettings.asset` xong; `git status` chỉ còn file trong Phạm vi.
<!-- Mỗi dòng ở "Phần cần test" → đạt / không đạt / chờ user + bằng chứng (tên test, dòng output).
Mỗi lệnh ở "Lệnh executor chạy": `<lệnh>` → <kết quả thật, dòng tổng kết>. -->

## Vướng mắc
<!-- Khi trạng thái = cần Opus: câu hỏi cụ thể + các phương án (kèm đề xuất). -->

---
<!-- PLANNER ghi khi review. -->

## Review
<!-- Kết luận: đã duyệt | cần sửa (thêm bước sửa vào "Các bước", trạng thái về chờ làm). Điểm đã đối chiếu. -->
- Đã đối chiếu (Planner, 2026-10-20 16:46):
  - `Chạy bằng: claude-sonnet-5-5 · medium` khớp `Model thực thi: Sonnet · medium`. Mọi dòng "Phần cần test" có kết quả; Bench Editor không đạt nhưng có lý do (Editor không ở foreground → khung đứng), số quyết định là điện thoại: đạt.
  - Diff `0565002` (19 file, đều trong Phạm vi): `ChunkStreamPlan` theo QĐ 1–3, 5 (cột, khoảng cách tới ô gần nhất bằng `long`, ≤ 128 nạp / > 160 gỡ, tối đa 2 cột, sắp (khoảng cách, chỉ số)); `BlockMesher.BuildChunks` ghim mảng + `Allocator.None`, nhả trong `finally` (QĐ 6); `WorldSource` mặc định `IslandV2`, `streaming` mặc định tắt (QĐ 7); chỉ số chunk `col + cols·cy` khớp thứ tự x → z → y; slot gỡ tắt collider, dựng lại bật theo số tam giác. Không đổi hằng golden.
  - Tiêu chí xong: EditMode 253/253, PlayMode 12/12, scene grep đúng, APK có, điện thoại `pass30fps=True passNoHole=True` (42,8 FPS), mã băm v3 trùng golden, `check_design.py` exit 0 (chạy lại khi review).
  - Ghi lại, không chặn: `buildMsMax 240 ms` trên điện thoại (giật lẻ), 1,5 triệu tam giác khi nạp 801 chunk — xem lại khi có điều khiển thật / LOD.
  - Mức `user xem`: chờ user bay thử TerrainView (mục User tự test 1). User báo chưa điều khiển được camera → đang hỏi.
