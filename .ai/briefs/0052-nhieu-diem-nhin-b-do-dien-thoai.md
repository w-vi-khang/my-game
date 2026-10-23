# Brief 0052 — nhiều điểm nhìn B: WalkBench có điểm nhìn thứ hai ở Map 2, đo điện thoại

> Trạng thái: cần Opus
> Review: đầy đủ
> Model thực thi: Sonnet · medium
> Cập nhật: 2026-10-23 11:25

<!-- Planner (Opus) viết, Executor (Sonnet) làm theo ở PHIÊN KHÁC — executor không có lịch sử chat, không được tự quyết. -->

## Mục tiêu
Thêm vào `WalkBench` một cờ `secondViewpoint`. Bật cờ thì có thêm một điểm nhìn cố định ở tâm Map 2. Build thêm một APK có bật cờ, rồi đo trên điện thoại cả hai APK (1 và 2 điểm nhìn). Tiêu chí: **2 điểm nhìn ≥ 30 FPS**.

Đây là tiêu chí thứ ba của task [m1-world-7i-nhieu-diem-nhin](../tasks/m1-world-7i-nhieu-diem-nhin.md) (đọc trước). Lõi và API client đã xong ở brief 0051 (đã duyệt). Quyết định gốc: [0029](../../design/decisions/0029-nhieu-diem-nhin-camera-quan-sat.md). Pillar P3. Thành phần: client Unity (`Game.Client`, công cụ đo). Ước lượng ~0,4 ngày-người.

## Bối cảnh cần đọc
- `client/Assets/Game/Client/WalkBench.cs` (cả file, ~200 dòng) [repo]:
  - Trường `:17-30`. Trường riêng `:32-43`, gồm `int holeFrames, maxMissing, maxLoadedChunks, waterFrames;` ở `:41`. `public const int RouteSamples = 72, HoleRadius = 120;` ở `:22`.
  - `Update` `:95`. Khối `if (!started)` ở `:98-115`: chờ `player.Ready`, `sv.Plain.HasValue`, `view.World != null`; đặt `RouteCenter`/`RouteRadius`/`RouteDry` (`:105-108`); log `WALKBENCH started: …` ở `:114`.
  - Khối đo `if (t >= warmupSeconds)` ở `:146-158`: `view.MissingColumnsWithin(HoleRadius)` → `holeFrames`/`maxMissing`, `view.LoadedChunkCount` → `maxLoadedChunks`.
  - `Finish` `:163-185`: dòng `holeFrames=… maxMissing=… loadedChunksMax=…` ở `:179`, dòng `pass30fps=… passNoHole=…` ở `:181`.
- `client/Assets/Game/Client/Editor/WalkBenchBuild.cs` (cả file, 80 dòng):
  - `EnsureScene()` `:22-47` (menu `Game/Create Scene WalkBench`) sao SimView → WalkBench.unity, thêm `WalkBench` vào mọi `SimView` ở `:35-40`.
  - `BuildAndroid()` `:49-78` (menu `Game/Build APK WalkBench`) build `OutputPath = "Builds/walkbench.apk"` (`:19`), app id tạm `com.viventa.walkbench`.
- API điểm nhìn của `TerrainChunkView` (brief 0051) [repo] `client/Assets/Game/Client/TerrainChunkView.cs`:
  ```csharp
  public int maxViewpoints = 2;                                         // :38
  public MapTable Maps { get; private set; }                            // :72 (null khi source không phải MapsV3)
  public int LoadedColumnCount => plan != null ? plan.LoadedCount : 0;  // :85
  public int MissingColumnsWithin(Transform viewpoint, int radius)      // :104
  public void AddViewpoint(Transform target, int loadRadius, int unloadRadius) // :113
  public int ViewpointCount                                             // :137 (điểm chính + điểm phụ còn sống)
  public Vector3Int CellAt(Vector3 worldPoint)                          // :290 (InverseTransformPoint rồi FloorToInt)
  ```
  Trường `loadRadius = 128`, `unloadRadius = 160` ở `:34-35`.
- `client/Assets/Game/Core/MapTable.cs`: `public readonly int SizeX, SizeZ;` (`:11`), `public byte Get(int x, int z)` (`:30`), `Ocean = 255`. Map 1 = id 0 (trại ở đây: `DemoPlain.Flatten` gọi `FlattenPlain(world, maps, 0, …)`, `client/Assets/Game/Client/DemoPlain.cs:42`), **Map 2 = id 1**, Map 3 = id 2.
- `client/Assets/Game/Tests/Client/WalkBenchPlayTests.cs` (4 test, 100 dòng): `SetUp` `:17-43` (`go.SetActive(true)` ở `:40`), `WalksRouteAndLogsSummary` `:87-98` (danh sách khoá ở `:95`).
- Scene `client/Assets/Game/Client/Scenes/WalkBench.unity` được commit. Nó được tạo lại từ SimView mỗi lần build.
- Số đo trước (brief 0039, 2026-10-22, 1 điểm nhìn) [repo] `.ai/briefs/0039-world-7h-b-do-fps-di-bo.md:453`: `avgFps=49,4 p50Ms=16,65 p99Ms=41,59 maxMs=187,65 holeFrames=0 loadedChunksMax=861`, Thermal Status 2.

## Quyết định đã chốt (không được đổi)
1. Điểm nhìn thứ hai **cố định ở tâm Map 2**, bật bằng cờ trong WalkBench, đo ngay trong 7i (user, 2026-10-23).
2. "Tâm Map 2" = trung bình (x + 0,5, z + 0,5) của mọi cột có `Maps.Get(x, z) == 1`. Planner chọn cách này vì tính được từ `MapTable` mà không chép lại thuật toán sinh map.
3. Bán kính của điểm nhìn thứ hai = `view.loadRadius` / `view.unloadRadius` (128/160, giống điểm chính).
4. Cờ được đặt lúc build, bằng menu riêng; APK riêng là `Builds/walkbench-2vp.apk`. Cùng app id `com.viventa.walkbench`. Scene commit giữ **1 điểm nhìn** (`secondViewpoint: 0`).
5. Dòng log thêm `viewpoints=`, `secondCenter=`, `secondHoleFrames=`, `loadedColumnsMax=`. `passNoHole` = cả hai vùng không thủng. Mọi khoá cũ giữ nguyên.
6. Đo cả 2 APK trong cùng lần: 1 điểm nhìn trước, nghỉ, rồi 2 điểm nhìn. Như vậy so được trong cùng điều kiện máy.
7. Dưới 30 FPS: ghi số liệu, đặt `chờ review` và ghi rõ "dưới 30 FPS" ở đầu Kết quả. **Không** tối ưu, không đổi bán kính hay `maxColumnsPerFrame`.
8. Code và comment mới viết bằng tiếng Anh (`HardcodedTextScanTests`). Comment tiếng Việt cũ không đụng tới.

## Phạm vi
- Được sửa/tạo:
  - `client/Assets/Game/Client/WalkBench.cs`
  - `client/Assets/Game/Client/Editor/WalkBenchBuild.cs`
  - `client/Assets/Game/Tests/Client/WalkBenchPlayTests.cs`
  - `client/Assets/Game/Client/Scenes/WalkBench.unity` (chỉ do `EnsureScene` tạo lại)
  - mục Kết quả / Kết quả test / Vướng mắc của brief này
- Không được sửa: `TerrainChunkView.cs`, `ChunkStreamPlan.cs`, `SimView.unity`, mọi file khác. Roadmap và world-tech do Planner cập nhật khi review.

## Các bước
1. **`WalkBench.cs` — trường mới.** Sau `public Vector2 RouteCenter { get; private set; }` (`:30`) thêm:
   ```csharp
   /// <summary>Adds a fixed second streaming viewpoint at the Map 2 centroid (task 7i, brief 0052). Read once when the walk starts.</summary>
   public bool secondViewpoint;
   /// <summary>Map id of the second viewpoint (Map 2).</summary>
   public const int SecondMap = 1;
   public Transform SecondViewpoint { get; private set; }
   public Vector2 SecondCenter { get; private set; }
   ```
   Sửa dòng `:41` thành `int holeFrames, maxMissing, maxLoadedChunks, waterFrames, secondHoleFrames, maxLoadedColumns;`.
   - Xong khi: biên dịch không lỗi.

2. **`WalkBench.cs` — hàm `MapCentroid`.** Thêm ngay sau `RouteIsDry` (sau `:93`):
   ```csharp
   /// <summary>Mean of (x + 0.5, z + 0.5) over every column whose map id is map; throws ArgumentException when the map has no column.</summary>
   public static Vector2 MapCentroid(MapTable maps, int map)
   {
       long sx = 0, sz = 0, n = 0;
       for (int z = 0; z < maps.SizeZ; z++)
           for (int x = 0; x < maps.SizeX; x++)
               if (maps.Get(x, z) == map) { sx += x; sz += z; n++; }
       if (n == 0) throw new System.ArgumentException($"map {map} has no column", nameof(map));
       return new Vector2((float)((double)sx / n + 0.5), (float)((double)sz / n + 0.5));
   }
   ```

3. **`WalkBench.cs` — đặt điểm nhìn khi bắt đầu.** Trong khối `if (!started)`, chèn ngay sau `RouteDry = dry;` (`:108`):
   ```csharp
   if (secondViewpoint)
   {
       if (view.Maps == null)
       {
           Debug.LogError("WALKBENCH secondViewpoint needs a multi-map world (source MapsV3); running with one viewpoint");
           secondViewpoint = false;
       }
       else
       {
           SecondCenter = MapCentroid(view.Maps, SecondMap);
           var vp = new GameObject("SecondViewpoint").transform;
           vp.SetParent(view.transform, false);
           vp.localPosition = new Vector3(SecondCenter.x, view.World.Size.Y, SecondCenter.y);
           view.AddViewpoint(vp, view.loadRadius, view.unloadRadius);
           SecondViewpoint = vp;
           Debug.Log($"WALKBENCH second viewpoint: map {SecondMap + 1} centroid ({SecondCenter.x:F1}, {SecondCenter.y:F1})");
       }
   }
   ```
   - `WorldSize` có `public readonly int X, Y, Z;` [repo] `client/Assets/Game/Core/WorldData.cs:10`; độ cao không ảnh hưởng nạp (chỉ dùng x, z).
   - Xong khi: biên dịch không lỗi.

4. **`WalkBench.cs` — đếm khi đo.** Trong khối `if (t >= warmupSeconds)`, ngay sau dòng `maxLoadedChunks = …` (`:153`) thêm:
   ```csharp
   maxLoadedColumns = Mathf.Max(maxLoadedColumns, view.LoadedColumnCount);
   if (SecondViewpoint != null && view.MissingColumnsWithin(SecondViewpoint, HoleRadius) > 0) secondHoleFrames++;
   ```

5. **`WalkBench.cs` — dòng log.** Trong `Finish`:
   - Thay dòng `:179` bằng hai dòng:
     ```csharp
     sb.Append($"viewpoints={view.ViewpointCount} secondCenter={(SecondViewpoint != null ? $"({SecondCenter.x:F1};{SecondCenter.y:F1})" : "none")} | ");
     sb.Append($"holeFrames={holeFrames} secondHoleFrames={secondHoleFrames} maxMissing={maxMissing} loadedColumnsMax={maxLoadedColumns} loadedChunksMax={maxLoadedChunks} | ");
     ```
   - Dòng `:181` đổi thành `sb.Append($"pass30fps={avgFps >= 30.0} passNoHole={holeFrames == 0 && secondHoleFrames == 0}");`.
   - Xong khi: biên dịch không lỗi; `read_console` Error rỗng.

6. **`WalkBenchBuild.cs`.**
   - Thêm hằng sau `:19`: `const string OutputPath2 = "Builds/walkbench-2vp.apk";`.
   - Đổi `EnsureScene`:
     - Giữ `[MenuItem("Game/Create Scene WalkBench")]` trên `public static void EnsureScene() => EnsureScene(false);`.
     - Thân cũ chuyển sang overload mới, có comment `/// <summary>Recreates the WalkBench scene from SimView and sets secondViewpoint on every WalkBench.</summary>`:
       ```csharp
       public static void EnsureScene(bool secondViewpoint)
       ```
     - Vòng `:35-40` thành:
       ```csharp
       var wb = sv.GetComponent<WalkBench>();
       if (wb == null) wb = sv.gameObject.AddComponent<WalkBench>();
       wb.secondViewpoint = secondViewpoint;
       EditorUtility.SetDirty(sv.gameObject);
       ```
   - Đổi `BuildAndroid`:
     - Giữ `[MenuItem("Game/Build APK WalkBench")]` trên `public static void BuildAndroid() => Build(false, OutputPath);`.
     - Thân cũ chuyển sang `static void Build(bool secondViewpoint, string outputPath)`. Gọi `EnsureScene(secondViewpoint);` thay cho `EnsureScene();`. Mọi chỗ `OutputPath` trong thân đổi thành `outputPath` (gồm `Directory.CreateDirectory`, `locationPathName`, dòng `Debug.Log`).
   - Thêm menu mới:
     ```csharp
     /// <summary>Same APK with a fixed second viewpoint at the Map 2 centroid (task 7i, brief 0052); the saved scene goes back to one viewpoint afterwards.</summary>
     [MenuItem("Game/Build APK WalkBench 2 Viewpoints")]
     public static void BuildAndroidTwoViewpoints()
     {
         try { Build(true, OutputPath2); }
         finally { EnsureScene(false); }
     }
     ```
   - Xong khi: biên dịch không lỗi; menu `Game/Build APK WalkBench 2 Viewpoints` có trong `mcpforunity://menu-items`.

7. **`WalkBenchPlayTests.cs`.**
   - Trong `SetUp`, ngay trước `go.SetActive(true);` (`:40`) thêm:
     ```csharp
     wb.secondViewpoint = TestContext.CurrentContext.Test.Name == nameof(SecondViewpointWalkLogsSummary);
     ```
   - `WalksRouteAndLogsSummary`:
     - thêm `"viewpoints=1"`, `"secondHoleFrames=0"`, `"loadedColumnsMax="` vào mảng khoá `:95`;
     - thêm `Assert.IsNull(wb.SecondViewpoint);` cuối test.
   - Test mới `MapCentroidLiesInMap2`:
     ```csharp
     [UnityTest]
     public IEnumerator MapCentroidLiesInMap2()
     {
         for (int i = 0; i < 300 && view.Maps == null; i++)
             yield return null;
         Assert.IsNotNull(view.Maps);
         var c = WalkBench.MapCentroid(view.Maps, WalkBench.SecondMap);
         Assert.AreEqual(c, WalkBench.MapCentroid(view.Maps, WalkBench.SecondMap));
         Assert.AreEqual(WalkBench.SecondMap, (int)view.Maps.Get(Mathf.FloorToInt(c.x), Mathf.FloorToInt(c.y)));
         Assert.Throws<System.ArgumentException>(() => WalkBench.MapCentroid(view.Maps, 7));
     }
     ```
   - Test mới `SecondViewpointWalkLogsSummary`:
     ```csharp
     [UnityTest]
     public IEnumerator SecondViewpointWalkLogsSummary()
     {
         float t0 = Time.realtimeSinceStartup;
         while (!wb.Done && Time.realtimeSinceStartup - t0 < 60f)
             yield return null;
         Assert.IsTrue(wb.Done);
         Assert.IsNotNull(wb.SecondViewpoint);
         Assert.AreEqual(2, view.ViewpointCount);
         foreach (var key in new[] { "viewpoints=2", "secondCenter=(", "secondHoleFrames=", "loadedColumnsMax=", "passNoHole=" })
             StringAssert.Contains(key, wb.Summary);
         var cell = view.CellAt(wb.SecondViewpoint.position);
         Assert.AreEqual(Mathf.FloorToInt(wb.SecondCenter.x), cell.x);
         Assert.AreEqual(Mathf.FloorToInt(wb.SecondCenter.y), cell.z);
         for (int i = 0; i < 300 && view.MissingColumnsWithin(wb.SecondViewpoint, 128) != 0; i++)
             yield return null;
         Assert.AreEqual(0, view.MissingColumnsWithin(wb.SecondViewpoint, 128));
     }
     ```
   - Sửa summary đầu lớp (`:8`) thành tiếng Anh: `/// <summary>World 7h b (brief 0039) + 7i (brief 0052): WalkBench — carrot, dry-route check, radius pick, auto walk + WALKBENCH line, second viewpoint at Map 2.</summary>`.
   - `MapCentroidLiesInMap2` fail ở dòng `Maps.Get(...) == SecondMap` (tâm rơi ngoài Map 2) → **không** đổi công thức. Ghi `c` và giá trị `Get` vào Vướng mắc, đặt `cần Opus`.
   - Xong khi: `WalkBenchPlayTests` 6/6.

8. **Build 2 APK** (sau khi mọi test qua, xem Lệnh executor chạy):
   - MCP `execute_menu_item` `Game/Build APK WalkBench` → console có `WalkBench build: Succeeded … → Builds/walkbench.apk`.
   - MCP `execute_menu_item` `Game/Build APK WalkBench 2 Viewpoints` → `WalkBench build: Succeeded … → Builds/walkbench-2vp.apk`.
   - Sau đó:
     - `git checkout -- client/ProjectSettings/UnityConnectSettings.asset`;
     - `grep -c "secondViewpoint: 1" client/Assets/Game/Client/Scenes/WalkBench.unity` → `0`;
     - `grep -c "secondViewpoint: 0" …` → ≥ 1.
   - Ghi `git diff --stat -- client/Assets/Game/Client/Scenes/WalkBench.unity` vào Kết quả rồi commit scene cùng code.

9. **Đo điện thoại** (AI tự làm, không nhờ user cài). `ADB` = `/Applications/Unity/Hub/Editor/6000.4.8f1/PlaybackEngines/AndroidPlayer/SDK/platform-tools/adb` (không có trong PATH).
   1. **Chọn máy:**
      - `ADB devices`: có serial USB `PFQCRWVWR46HU4GQ` thì dùng nó.
      - Không có: `ADB connect 192.168.1.6:5555`, hoặc `ADB mdns services` để tìm cổng rồi `ADB connect <ip:port>`. **Không dùng `192.168.1.7:6060`** (đó là Android TV).
      - Kiểm `ADB -s <máy> shell getprop ro.product.model` → phải là `24095PCADG`.
      - Không tìm thấy máy → ghi "chờ user cắm lại điện thoại" vào Vướng mắc, đặt `cần Opus`, dừng.
   2. **Lần A — 1 điểm nhìn:**
      - `ADB -s <máy> shell dumpsys thermalservice | grep -m1 "Thermal Status"` (ghi lại);
      - `ADB -s <máy> install -r client/Builds/walkbench.apk` → `Success`;
      - `ADB -s <máy> logcat -c`;
      - `ADB -s <máy> shell monkey -p com.viventa.walkbench -c android.intent.category.LAUNCHER 1`;
      - khoảng 25 giây sau: `ADB -s <máy> shell dumpsys window | grep -E "mDreamingLockscreen|KeyguardShowing"`. Màn hình khoá → ghi "chờ user mở khoá" vào Vướng mắc, `cần Opus`, dừng. Không chụp ảnh màn hình.
      - Chờ tổng 90 giây kể từ lúc mở app, rồi `ADB -s <máy> logcat -d -s Unity | grep -E "WALKBENCH|Exception"`. Chưa có dòng `device=` → chờ thêm 60 giây, đọc lại. Vẫn không có → ghi lại và dừng (`cần Opus`).
      - `ADB -s <máy> shell am force-stop com.viventa.walkbench`.
   3. **Nghỉ 60 giây**, rồi **Lần B — 2 điểm nhìn:** giống lần A nhưng cài `client/Builds/walkbench-2vp.apk`. Dòng log phải có thêm `WALKBENCH second viewpoint: map 2 centroid …` và `viewpoints=2`.
   4. Chép nguyên văn vào Kết quả: cho mỗi lần, dòng Thermal trước khi chạy, dòng `WALKBENCH started`, dòng `WALKBENCH second viewpoint` (lần B), dòng `WALKBENCH device=…`.
   5. Kẻ bảng so sánh ở Kết quả. Cột: 0039 · lần A · lần B. Dòng: avgFps, p50Ms, p99Ms, maxMs, holeFrames, secondHoleFrames, loadedColumnsMax, loadedChunksMax, sysMemMaxMB, Thermal. Cột 0039 lấy từ "Bối cảnh"; ô không có ghi `—`.
   6. Lần B `avgFps < 30` → áp dụng QĐ 7.

## Tiêu chí xong
- `WalkBenchPlayTests` 6/6. Toàn bộ EditMode + PlayMode qua.
- Hai APK build `Succeeded`.
- Kết quả có dòng `WALKBENCH device=…` của **cả hai lần** từ 24095PCADG, 0 `Exception`, kèm bảng so sánh.
- Lần B có `viewpoints=2`. FPS đạt hay không đều ghi; dưới 30 thì theo QĐ 7.
- `git diff --stat <base> -- client/Assets/Game/Client/TerrainChunkView.cs client/Assets/Game/Core/ChunkStreamPlan.cs client/Assets/Game/Client/Scenes/SimView.unity` rỗng.
- `git status --short` sạch sau commit và bước dọn.

## Test sau khi xong

### Phần cần test
| Phần | Cách test | Mong đợi |
|---|---|---|
| `MapCentroid` (bước 2) | `WalkBenchPlayTests.MapCentroidLiesInMap2` | Tâm nằm trong Map 2, gọi 2 lần ra cùng giá trị, map không có cột → `ArgumentException` |
| Đặt điểm nhìn thứ hai + đếm + log (bước 1, 3, 4, 5) | `SecondViewpointWalkLogsSummary` | Xong trong 60 s; `ViewpointCount == 2`; log có 5 khoá; ô của điểm nhìn đúng `SecondCenter`; vùng 128 quanh đó nạp đủ |
| Cờ tắt giữ hành vi cũ (bước 5, regression) | `WalksRouteAndLogsSummary` (đã sửa) | Qua; log có `viewpoints=1`, `secondHoleFrames=0`, mọi khoá cũ; `SecondViewpoint` null |
| Hàm cũ không đổi | `CarrotLeadsAlongCircle`, `RouteIsDryRejectsSeaAndOutside`, `DefaultSeedRouteIsDry` | Qua |
| Build script (bước 6) + scene (bước 8) | 2 lệnh build + 2 lệnh `grep` ở bước 8 | Cả hai `Succeeded`; scene `secondViewpoint: 1` = 0 dòng |
| Nạp chunk nhiều điểm nhìn (regression 0051) | `TerrainStreamingPlayTests` + `ChunkStreamPlanTests` trong bộ đầy đủ | Qua |
| SimView và phần còn lại (regression) | PlayMode toàn bộ, EditMode toàn bộ | Qua; EditMode vẫn 504 test (không thêm/bớt), PlayMode 170 + 2 = 172 |
| Điện thoại (bước 9) | adb lần A + lần B | 2 dòng `WALKBENCH device=…`, lần B `viewpoints=2`, 0 `Exception` |

### Lệnh executor chạy
1. MCP `read_console` (Error) sau bước 5 và sau bước 7 → rỗng.
2. MCP `run_tests` PlayMode, lọc `Game.Client.Tests.WalkBenchPlayTests` → 6/6.
3. MCP `run_tests` EditMode (toàn bộ) → ghi `x/x`.
4. MCP `run_tests` PlayMode (toàn bộ) → ghi `x/x`.
5. `git checkout -- client/ProjectSettings/EditorSettings.asset` (sau PlayMode).
6. Bước 8 (2 build). Sau đó `git checkout -- client/ProjectSettings/UnityConnectSettings.asset`, rồi `git status --short client/ProjectSettings` → rỗng. Còn file khác trong ProjectSettings bị đổi → ghi tên vào Kết quả và `git checkout --` file đó.
7. Bước 9 (adb).
8. Ghi giờ bắt đầu/kết thúc theo `date` vào Kết quả.

### User tự test
Không có thao tác. Planner đọc số liệu điện thoại và báo user (task contract: AI tự cài APK và đọc logcat).

## Dừng và chuyển lại Opus khi
- Brief mơ hồ hoặc mâu thuẫn với code thật (vd số dòng lệch tới mức không tìm được chỗ chèn, tên API khác).
- Cần một quyết định thuộc AGENTS.md §3.4 hoặc ngoài "Quyết định đã chốt".
- Cùng một lỗi sửa 2 lần vẫn chưa qua.
- Phải sửa file ngoài "Phạm vi".
- `MapCentroidLiesInMap2` báo tâm ngoài Map 2 (bước 7).
- Không tìm thấy điện thoại, màn hình khoá, hoặc không có dòng `device=` (bước 9).
- FPS dưới 30 **không** phải lý do dừng: ghi số rồi `chờ review` (QĐ 7).

---
<!-- Từ đây trở xuống: EXECUTOR ghi. -->

## Kết quả
Base commit: a14e7f4
Chạy bằng: claude-sonnet-5-5 · medium
Giờ: bắt đầu 11:22, dừng 11:40 (2026-10-23) — bước 1–8 xong, bước 9 (điện thoại) chưa đo được.
Đã làm: bước 1–8. Scene: `git diff --stat` = 4 insertions (`secondViewpoint: 0`); `grep "secondViewpoint: 1"` = 0, `: 0` = 1. `WalkBench.unity.meta` bị đổi guid do tạo lại scene → đã `git checkout` lại. Hai APK khác nhau (cmp differ): `client/Builds/walkbench.apk`, `walkbench-2vp.apk` (35.382.399 byte). Log build 2vp: `WalkBench build: Succeeded … → Builds/walkbench-2vp.apk`; lần build 1 điểm nhìn: APK mới 11:31 (dòng log không còn trong console khi đọc).
Bước 9: CHƯA ĐO (xem Vướng mắc).

## Kết quả test
- `MapCentroidLiesInMap2`, `SecondViewpointWalkLogsSummary`, `WalksRouteAndLogsSummary`: đạt — `WalkBenchPlayTests` 6/6 [run].
- read_console Error: rỗng sau biên dịch.
- EditMode toàn bộ 504/504; PlayMode toàn bộ 172/172. Đã `git checkout` EditorSettings.asset và UnityConnectSettings.asset.
- Build 2 APK: đạt (xem Kết quả). Điện thoại: không đạt/chưa chạy — không có máy.

## Vướng mắc
Không tìm thấy điện thoại 24095PCADG: `adb devices` rỗng (không USB); `adb connect 192.168.1.6:5555` → Connection refused; `adb mdns services` báo `adb-PFQCRWVWR46HU4GQ-fN8sCo 192.168.1.6:35903` nhưng `adb connect 192.168.1.6:35903` → Connection refused (mdns cũ). Cần user cắm USB (hoặc bật lại wireless debugging) rồi chạy lại bước 9 với hai APK đã build sẵn (không cần build lại).

---
<!-- PLANNER ghi khi review. -->

## Review
