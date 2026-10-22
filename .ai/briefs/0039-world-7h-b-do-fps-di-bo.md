# Brief 0039 — World 7h (b): đo FPS điện thoại khi nhân vật tự đi

> Trạng thái: cần Opus
> Review: đầy đủ
> Model thực thi: Sonnet · medium
> Cập nhật: 2026-10-22 16:54

<!-- Planner (Opus) viết, Executor (Sonnet) làm theo ở PHIÊN KHÁC — executor không có lịch sử chat, không được tự quyết.
Trạng thái: chờ làm → đang làm → chờ review → đã duyệt; hoặc → cần Opus (executor dừng, hỏi ở mục Vướng mắc).
Review: nhẹ (đọc mục Kết quả) | đầy đủ (đọc cả diff) | user xem (có phần user tự kiểm bằng mắt/chơi thử) — có thể ghép "đầy đủ + user xem".
Model thực thi: BẮT BUỘC, đúng dạng `<Model> · <effort>` — Model: Opus | Sonnet | Haiku | Fable; effort: low | medium | high | xhigh | max (chọn theo AGENTS.md §10.2).
Executor kiểm tra model + effort của phiên TRƯỚC khi làm (scripts/brief_check.py); lệch hoặc không xác định được → cảnh báo user và DỪNG.
File được commit. Tự kiểm trước khi giao (AGENTS.md §10.3): executor có phải hỏi gì không? còn chỗ nào bỏ ngỏ 2 phương án? -->

## Mục tiêu
Task [m1-world-7h-b-do-fps-dien-thoai](../tasks/m1-world-7h-b-do-fps-dien-thoai.md) (user xác nhận 2026-10-22). Mục tiêu là đo được trên điện thoại SimView có giữ **≥ 30 FPS** khi nhân vật đi liên tục, và có lộ lỗ chunk không. Việc cần làm:

1. Viết component `WalkBench`: cho nhân vật tự đi vòng quanh trại, đo khung hình, ghi một dòng `WALKBENCH` ra log.
2. Tạo scene `WalkBench` (bản sao SimView có gắn `WalkBench`) và APK riêng.
3. Viết test PlayMode.
4. Cài APK lên Xiaomi 24095PCADG, đọc logcat.

Ước lượng 0,5–1 ngày-người. Ghi giờ bắt đầu và kết thúc (theo `date`) vào mục Kết quả.

## Bối cảnh cần đọc
Đường dẫn tính từ `client/Assets/Game/`. Số dòng theo commit `6db16aa`.

**`Client/PlayerCharacter.cs`**
- `:18-20`: `public bool readKeyboard = true; public Vector2 MoveInput; public bool JumpInput;`.
- `:30` `public static PlayerCharacter Active`; `:31` `Ready` (true khi đã có collider đất dưới chân); `:33` `InWater`; `:37` `public Vector3 FeetLocal`.
- `:188-192`: khi `readKeyboard == false`, code không ghi đè `MoveInput`/`JumpInput`.
- `:197-198`: hướng đi bằng `Quaternion.Euler(0f, follow.yaw, 0f) * new Vector3(MoveInput.x, 0f, MoveInput.y)`. Vì vậy `MoveInput = (0, 1)` + `follow.yaw = Atan2(dx, dz)·Rad2Deg` ⇒ nhân vật đi theo hướng (dx, dz).
- `:203`: nhảy khi đang chạm đất và `JumpInput`. Nhảy lên được bậc 1 ô, không lên được bậc 2 ô.

**`Client/FollowCamera.cs`**
- `:16` `public float yaw;`.
- `:29-53` `LateUpdate`: chỉ đổi yaw khi giữ chuột giữa, sau đó gọi `SnapNow()`.

**`Client/SimView.cs`**
- `:32` `public StressPlain? Plain`; `:65` `public Simulation Sim`.
- `:82-100` `OnAfterGenerate`: san đồng bằng, đặt nhân vật ở `PlayerCharacter.PlainSpawn(plain)`.
- `:207` `SetFrameRate(60)` (vSync 0, targetFrameRate 60).

**`Client/DemoPlain.cs:51`**: `public static Vector3 CampCenter(StressPlain p)` (toạ độ terrain).

**`Client/TerrainChunkView.cs`**
- `:80` `LoadedChunkCount`; `:95` `public int MissingColumnsWithin(int radius)` (tâm là `streamTarget`, tức nhân vật).
- `:34` `loadRadius = 128`.

**`Client/TerrainBench.cs`** (mẫu đo, KHÔNG sửa)
- `:58-66` ProfilerRecorder `"System Used Memory"` / `"GC Reserved Memory"` (`using Unity.Profiling;`).
- `:119-124` đếm `holeFrames` bằng `MissingColumnsWithin(120)`.
- `:199-208` `OnGUI` hộp kết quả.

**Hàm khác**
- `Client/SimBenchMeter.cs:97`: `public static double Percentile(List<double> sorted, double q)`.
- `Core/MinimapImage.cs:44`: `public static void ColumnInfo(WorldData world, int x, int z, out int surface, out int waterDepth)`. `surface < 0` = cột rỗng; `waterDepth > 0` = có nước trên mặt.
- `Core/WorldEditor.cs:171`: `public static bool IsSolidAt(WorldData world, int x, int y, int z)` (không tính nước).

**Mẫu build: `Client/Editor/SimBenchBuild.cs:15-78`** (menu, `EnsureScene` chép SimView, `BuildAndroid`).

**Mẫu test PlayMode**: `Tests/Client/PlayerCharacterPlayTests.cs:22-53` (dựng camera + terrain + nhân vật) và `Tests/Client/SimBenchPlayTests.cs:18-35`.

**API Unity:** chỉ dùng API đã có trong repo (ProfilerRecorder, SystemInfo, BuildPipeline, EditorSceneManager, AssetDatabase, GUI). Không thêm API mới.

## Quyết định đã chốt (không được đổi)
1. **Không sửa SimView, PlayerCharacter, FollowCamera, scene SimView.**
   - "Cờ" của contract chính là sự có mặt của component `WalkBench`: nó chỉ được gắn trong scene `WalkBench`. SimView không có nó nên chạy như cũ.
2. **Lộ trình**
   - Tâm = `DemoPlain.CampCenter(plain)` (x, z).
   - Bán kính chọn theo thứ tự `{36, 32, 40, 28, 44}`: lấy số đầu tiên mà 72 điểm cách đều trên vòng tròn đều nằm trong world, cột không rỗng, không có nước. Không số nào đạt thì dùng 36 và `routeDry=False`.
   - Mỗi khung, nhân vật đi về "điểm dẫn" trên vòng tròn, ở góc cực hiện tại + 25° (đi ngược chiều kim đồng hồ khi nhìn từ trên, với quy ước x = sin, z = cos). Từ chỗ xuất hiện, nhân vật tự đi xoắn ra vòng tròn.
3. **Tự nhảy:** `JumpInput = IsSolidAt(ô ở chân, lệch 0,7 ô theo hướng đi)`.
4. **Kẹt:** cứ 2 giây so vị trí (x, z) với 2 giây trước. Đi được < 1 ô thì `stuck++` và rẽ phải 90° trong 1,5 giây.
5. **Thời gian:** tính từ khung đầu tiên `player.Ready` và có `Plain`. Khởi động 3 giây (vẫn đi), đo 40 giây (theo `Time.realtimeSinceStartup`). Hết giờ thì dừng đi, ghi log một lần.
6. **Lỗ chunk:** `MissingColumnsWithin(120)`, giống `TerrainBench`.
7. Scene đo **tắt `TerrainEditInput`** (giống SimBench) và **giữ** `stressBench = false` (trại demo thường).
8. Tên:
   - scene `Assets/Game/Client/Scenes/WalkBench.unity`;
   - APK `Builds/walkbench.apk`;
   - app id tạm `com.viventa.walkbench`;
   - menu `Game/Tạo scene WalkBench`, `Game/Build APK WalkBench`.
9. **Không đạt ≥ 30 FPS hoặc có lỗ chunk không phải lỗi của brief.** Ghi số liệu, không tối ưu gì.

## Phạm vi
- **Được tạo:**
  - `Client/WalkBench.cs`;
  - `Client/Editor/WalkBenchBuild.cs`;
  - `Client/Scenes/WalkBench.unity` (sinh bằng menu);
  - `Tests/Client/WalkBenchPlayTests.cs`;
  - file `.meta` của các file trên;
  - mục Kết quả của brief này.
- **Không được sửa:** mọi file khác. Gồm `SimView.cs`, `PlayerCharacter.cs`, `FollowCamera.cs`, `TerrainBench.cs`, `SimBenchBuild.cs`, `Scenes/SimView.unity`, `Scenes/SimBench.unity`, `Core/`, `design/`, `ProjectSettings/` (dọn theo bước 6).
- **Không commit** ảnh chụp hay file trong `client/Builds/`.

## Các bước
1. **`Client/WalkBench.cs`** — tạo đúng nội dung sau.
   - Xong khi: biên dịch không lỗi (`read_console` Error rỗng).

```csharp
using System.Collections.Generic;
using System.Text;
using Unity.Profiling;
using Game.Core;
using UnityEngine;

namespace Game.Client
{
    /// <summary>
    /// Đo FPS khi nhân vật đi liên tục (world 7h b, brief 0039). Chỉ gắn trong scene WalkBench (bản sao SimView, menu "Game/Build APK WalkBench").
    /// Nhân vật tự đi vòng tròn quanh trại (bán kính chọn để cả vòng trên đất khô), tự nhảy khi trước mặt có khối, kẹt thì rẽ phải 1,5 giây.
    /// Khởi động warmupSeconds rồi đo measureSeconds; ghi một dòng WALKBENCH (đọc bằng adb logcat).
    /// </summary>
    [RequireComponent(typeof(TerrainChunkView), typeof(SimView))]
    public class WalkBench : MonoBehaviour
    {
        public float warmupSeconds = 3f;
        public float measureSeconds = 40f;
        public float leadDegrees = 25f;
        public float stuckCheckSeconds = 2f, stuckMinDistance = 1f, detourSeconds = 1.5f;
        public static readonly int[] RadiusCandidates = { 36, 32, 40, 28, 44 };
        public const int RouteSamples = 72, HoleRadius = 120;

        public bool Done { get; private set; }
        public string Summary { get; private set; }
        public float Distance { get; private set; }
        public int StuckCount { get; private set; }
        public int RouteRadius { get; private set; }
        public bool RouteDry { get; private set; }
        public Vector2 RouteCenter { get; private set; }

        TerrainChunkView view;
        SimView sv;
        PlayerCharacter player;
        FollowCamera follow;
        ProfilerRecorder systemMem, gcMem;
        readonly List<double> frameMs = new List<double>();
        bool started;
        float startTime, nextStuckCheck, detourUntil;
        Vector2 lastCheckPos, lastPos;
        int holeFrames, maxMissing, maxLoadedChunks, waterFrames;
        long maxSystemMem, maxGcMem;
        string result = "Đang chuẩn bị…";

        void Awake()
        {
            view = GetComponent<TerrainChunkView>();
            sv = GetComponent<SimView>();
        }

        void OnEnable()
        {
            systemMem = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "System Used Memory");
            gcMem = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Reserved Memory");
        }

        void OnDisable()
        {
            systemMem.Dispose();
            gcMem.Dispose();
        }

        /// <summary>Điểm dẫn: trên vòng tròn (tâm, bán kính), ở góc cực của chân + leadDeg. Quy ước x = tâm.x + R·sin, z = tâm.y + R·cos.</summary>
        public static Vector2 Carrot(Vector2 center, float radius, Vector2 feet, float leadDeg)
        {
            float theta = Mathf.Atan2(feet.x - center.x, feet.y - center.y) + leadDeg * Mathf.Deg2Rad;
            return center + radius * new Vector2(Mathf.Sin(theta), Mathf.Cos(theta));
        }

        /// <summary>Bán kính đầu tiên trong RadiusCandidates mà cả vòng trên đất khô; không có thì RadiusCandidates[0], dry = false.</summary>
        public static int PickRadius(WorldData world, Vector2 center, out bool dry)
        {
            foreach (int r in RadiusCandidates)
                if (RouteIsDry(world, center, r)) { dry = true; return r; }
            dry = false;
            return RadiusCandidates[0];
        }

        /// <summary>RouteSamples điểm cách đều trên vòng: trong world, cột không rỗng, không có nước.</summary>
        public static bool RouteIsDry(WorldData world, Vector2 center, int radius)
        {
            var s = world.Size;
            for (int k = 0; k < RouteSamples; k++)
            {
                float a = k * (2f * Mathf.PI / RouteSamples);
                int x = Mathf.FloorToInt(center.x + radius * Mathf.Sin(a));
                int z = Mathf.FloorToInt(center.y + radius * Mathf.Cos(a));
                if (x < 0 || z < 0 || x >= s.X || z >= s.Z) return false;
                MinimapImage.ColumnInfo(world, x, z, out int surface, out int waterDepth);
                if (surface < 0 || waterDepth > 0) return false;
            }
            return true;
        }

        void Update()
        {
            if (Done) return;
            if (!started)
            {
                player = PlayerCharacter.Active;
                if (player == null || !player.Ready || !sv.Plain.HasValue || view.World == null) return;
                var cam = Camera.main;
                follow = cam != null ? cam.GetComponent<FollowCamera>() : null;
                player.readKeyboard = false;
                var c = DemoPlain.CampCenter(sv.Plain.Value);
                RouteCenter = new Vector2(c.x, c.z);
                RouteRadius = PickRadius(view.World, RouteCenter, out bool dry);
                RouteDry = dry;
                var f0 = player.FeetLocal;
                lastPos = lastCheckPos = new Vector2(f0.x, f0.z);
                startTime = Time.realtimeSinceStartup;
                nextStuckCheck = stuckCheckSeconds;
                started = true;
                Debug.Log($"WALKBENCH bắt đầu: tâm ({RouteCenter.x:F1}, {RouteCenter.y:F1}), R={RouteRadius}, đất khô={RouteDry}");
            }

            float t = Time.realtimeSinceStartup - startTime;
            if (t > warmupSeconds + measureSeconds)
            {
                Finish();
                return;
            }

            var f = player.FeetLocal;
            var feet = new Vector2(f.x, f.z);
            if (t >= nextStuckCheck)
            {
                if (Vector2.Distance(feet, lastCheckPos) < stuckMinDistance)
                {
                    StuckCount++;
                    detourUntil = t + detourSeconds;
                }
                lastCheckPos = feet;
                nextStuckCheck = t + stuckCheckSeconds;
            }

            var dir = Carrot(RouteCenter, RouteRadius, feet, leadDegrees) - feet;
            if (t < detourUntil) dir = new Vector2(dir.y, -dir.x); // rẽ phải 90°
            if (dir.sqrMagnitude > 1e-6f) dir.Normalize();
            var worldDir = view.transform.TransformDirection(new Vector3(dir.x, 0f, dir.y));
            if (follow != null) follow.yaw = Mathf.Atan2(worldDir.x, worldDir.z) * Mathf.Rad2Deg;
            player.MoveInput = new Vector2(0f, 1f);
            int px = Mathf.FloorToInt(f.x + dir.x * 0.7f), py = Mathf.FloorToInt(f.y + 0.05f), pz = Mathf.FloorToInt(f.z + dir.y * 0.7f);
            player.JumpInput = WorldEditor.IsSolidAt(view.World, px, py, pz);

            if (t >= warmupSeconds)
            {
                frameMs.Add(Time.unscaledDeltaTime * 1000.0);
                Distance += Vector2.Distance(feet, lastPos);
                int missing = view.MissingColumnsWithin(HoleRadius);
                if (missing > 0) holeFrames++;
                maxMissing = Mathf.Max(maxMissing, missing);
                maxLoadedChunks = Mathf.Max(maxLoadedChunks, view.LoadedChunkCount);
                if (player.InWater) waterFrames++;
                if (systemMem.Valid) maxSystemMem = System.Math.Max(maxSystemMem, systemMem.LastValue);
                if (gcMem.Valid) maxGcMem = System.Math.Max(maxGcMem, gcMem.LastValue);
                result = $"WALKBENCH đang đo… {warmupSeconds + measureSeconds - t:F0} s · {Distance:F0} ô · kẹt {StuckCount}";
            }
            else result = $"WALKBENCH khởi động… {warmupSeconds - t:F0} s";
            lastPos = feet;
        }

        void Finish()
        {
            Done = true;
            player.MoveInput = Vector2.zero;
            player.JumpInput = false;
            var sorted = new List<double>(frameMs);
            sorted.Sort();
            double sum = 0;
            foreach (var x in frameMs) sum += x;
            double avgMs = frameMs.Count > 0 ? sum / frameMs.Count : 0;
            double avgFps = avgMs > 0 ? 1000.0 / avgMs : 0;
            double max = sorted.Count > 0 ? sorted[sorted.Count - 1] : 0;
            var sb = new StringBuilder();
            sb.Append($"WALKBENCH device={SystemInfo.deviceModel} | gpu={SystemInfo.graphicsDeviceName} ({SystemInfo.graphicsDeviceType}) | screen={Screen.width}x{Screen.height} | ");
            sb.Append($"frames={frameMs.Count} avgFps={avgFps:F1} avgMs={avgMs:F2} p50Ms={SimBenchMeter.Percentile(sorted, 0.5):F2} p99Ms={SimBenchMeter.Percentile(sorted, 0.99):F2} maxMs={max:F2} | ");
            sb.Append($"distance={Distance:F1} stuck={StuckCount} waterFrames={waterFrames} routeR={RouteRadius} routeDry={RouteDry} | ");
            sb.Append($"holeFrames={holeFrames} maxMissing={maxMissing} loadedChunksMax={maxLoadedChunks} | ");
            sb.Append($"sysMemMaxMB={maxSystemMem / 1048576.0:F1} gcReservedMaxMB={maxGcMem / 1048576.0:F1} | creatures={(sv.Sim != null ? sv.Sim.State.Creatures.Count : 0)} | ");
            sb.Append($"pass30fps={avgFps >= 30.0} passNoHole={holeFrames == 0}");
            Summary = sb.ToString();
            result = Summary.Replace(" | ", "\n");
            Debug.Log(Summary);
        }

        void OnGUI()
        {
            if (MinimapView.IsAnyOpen) return;
            var style = new GUIStyle(GUI.skin.label) { fontSize = Mathf.Max(18, Screen.height / 40), wordWrap = true };
            style.normal.textColor = Color.white;
            var r = new Rect(30, Screen.height * 0.45f, Screen.width - 60, Screen.height * 0.55f);
            GUI.Box(r, GUIContent.none);
            GUI.Label(new Rect(r.x + 10, r.y + 10, r.width - 20, r.height - 20), result, style);
        }
    }
}
```

   - `WorldData`, `WorldEditor`, `MinimapImage` đều thuộc namespace `Game.Core`. Nếu biên dịch lỗi thì dừng, `cần Opus`.

2. **`Client/Editor/WalkBenchBuild.cs`** — chép `SimBenchBuild.cs` rồi đổi những chỗ sau:
   - Tên lớp: `WalkBenchBuild`.
   - Summary: `/// Build APK đo FPS đi bộ (world 7h b, brief 0039): scene WalkBench = bản sao SimView có thêm WalkBench (nhân vật tự đi vòng quanh trại, log WALKBENCH).` + dòng menu/`-executeMethod Game.Client.Editor.WalkBenchBuild.BuildAndroid`.
   - Hằng:
     - `ScenePath = "Assets/Game/Client/Scenes/WalkBench.unity"`;
     - `OutputPath = "Builds/walkbench.apk"`;
     - `TempAppId = "com.viventa.walkbench"`.
     - `SourceScene` giữ nguyên.
   - Menu: `[MenuItem("Game/Tạo scene WalkBench")]`, `[MenuItem("Game/Build APK WalkBench")]`.
   - Trong vòng `foreach (var sv in views)`, thay 2 dòng bằng:
     ```csharp
     if (sv.GetComponent<WalkBench>() == null)
         sv.gameObject.AddComponent<WalkBench>();
     EditorUtility.SetDirty(sv.gameObject);
     ```
     Không đặt `stressBench`.
   - Thông báo lỗi: `"Scene WalkBench không có SimView"`.
   - Comment trên vòng `TerrainEditInput`: `// Tắt đào/đặt (nhân vật tự đi).`
   - Log build: `$"WalkBench build: {report.summary.result}, …"`.
   - Xong khi: biên dịch không lỗi.

3. **Sinh scene:** MCP `execute_menu_item` `Game/Tạo scene WalkBench`.
   - Xong khi: có `client/Assets/Game/Client/Scenes/WalkBench.unity` (+ `.meta`), và `git diff --stat -- client/Assets/Game/Client/Scenes/SimView.unity` rỗng.
   - Nếu `SimView.unity` bị đổi: `git checkout --` file đó và ghi lại vào Kết quả.

4. **`Tests/Client/WalkBenchPlayTests.cs`** — tạo lớp `WalkBenchPlayTests` (namespace `Game.Client.Tests`).
   - Field: `GameObject camGo, go, playerGo; TerrainChunkView view; SimView sv; WalkBench wb; PlayerCharacter player;`.
   - `[UnitySetUp] SetUp()`, theo thứ tự:
     1. `camGo` có tag `MainCamera`, thêm `Camera`, `DebugFlyCamera`, `FollowCamera`.
     2. `go = new GameObject("Terrain"); go.SetActive(false);` rồi thêm `TerrainChunkView` (`source = WorldSource.MapsV3`, `streaming = true`), `SimView`, `WalkBench`. Đặt `wb.warmupSeconds = 0.5f; wb.measureSeconds = 4f;`.
     3. `playerGo` inactive, thêm `PlayerCharacter`, `player.terrain = view; player.readKeyboard = false;`, rồi `playerGo.SetActive(true);`.
     4. `go.SetActive(true);`, rồi `yield return null;` hai lần.
   - `[UnityTearDown]`: Destroy `playerGo`, `go`, `camGo`; `yield return null;`.
   - Các test (`[UnityTest]`):
     - `CarrotLeadsAlongCircle`:
       - `WalkBench.Carrot(Vector2.zero, 10f, new Vector2(0f, 5f), 25f)` ≈ (4.2262, 9.0631);
       - `Carrot(Vector2.zero, 10f, new Vector2(-3f, 0f), 25f)` ≈ (−9.0631, 4.2262);
       - sai số 1e-3; `yield return null`.
     - `RouteIsDryRejectsSeaAndOutside`:
       - `RouteIsDry(view.World, new Vector2(12f, 12f), 5)` == false (cột cách biên < 24 ô là biển);
       - `RouteIsDry(view.World, new Vector2(2f, 2f), 5)` == false (ra ngoài world).
       - Trước đó chờ `view.World != null` (tối đa 300 khung).
     - `DefaultSeedRouteIsDry`:
       - chờ tối đa 300 khung tới khi `wb.RouteRadius > 0`;
       - `CollectionAssert.Contains(WalkBench.RadiusCandidates, wb.RouteRadius)`;
       - `Assert.IsTrue(wb.RouteDry)`;
       - `Assert.IsTrue(WalkBench.RouteIsDry(view.World, wb.RouteCenter, wb.RouteRadius))`.
     - `WalksRouteAndLogsSummary`:
       - chờ tới khi `wb.Done`, tối đa 60 giây theo `Time.realtimeSinceStartup`;
       - `Assert.IsTrue(wb.Done)`;
       - `Assert.Greater(wb.Distance, 5f)`;
       - `wb.Summary` chứa từng chuỗi: `"WALKBENCH "`, `"avgFps="`, `"p50Ms="`, `"p99Ms="`, `"maxMs="`, `"distance="`, `"stuck="`, `"routeR="`, `"holeFrames="`, `"maxMissing="`, `"loadedChunksMax="`, `"sysMemMaxMB="`, `"pass30fps="`, `"passNoHole="`;
       - `Assert.AreEqual(Vector2.zero, player.MoveInput)`.
   - Xong khi: 4 test qua (`run_tests` PlayMode lọc `Game.Client.Tests.WalkBenchPlayTests`).
   - **`DefaultSeedRouteIsDry` fail** (`RouteDry=false`): KHÔNG đổi bán kính hay thuật toán. Ghi R, tâm, dòng log `WALKBENCH bắt đầu` vào Vướng mắc, rồi dừng với trạng thái `cần Opus`.
   - **`WalksRouteAndLogsSummary` fail vì `Distance ≤ 5`**: ghi `StuckCount`, vị trí chân đầu/cuối, rồi dừng với trạng thái `cần Opus`.

5. **Chạy toàn bộ test:** EditMode, rồi PlayMode.
   - Xong khi: tất cả qua.
   - Test cũ fail thì dừng, `cần Opus`.

6. **Dọn ProjectSettings:**
   - `git checkout -- client/ProjectSettings/EditorSettings.asset` (sau PlayMode).
   - Sau bước 7 (build): `git checkout -- client/ProjectSettings/UnityConnectSettings.asset`.
   - Sau đó `git status --short client/ProjectSettings` phải rỗng. Còn file khác bị đổi thì ghi tên vào Kết quả và `git checkout --` file đó.

7. **Build APK:** MCP `execute_menu_item` `Game/Build APK WalkBench`.
   - Xong khi: console có `WalkBench build: Succeeded`.

8. **Đo trên điện thoại** (AI tự làm, không nhờ user cài):
   - `ADB` = `/Applications/Unity/Hub/Editor/6000.4.8f1/PlaybackEngines/AndroidPlayer/SDK/platform-tools/adb` (không có trong PATH).
   1. **Chọn máy:**
      - `ADB devices`: có serial USB `PFQCRWVWR46HU4GQ` thì dùng nó.
      - Không có: chạy `ADB mdns services` để tìm cổng của điện thoại, `ADB connect <ip:port>`. **Không dùng `192.168.1.7:6060`** (đó là Android TV).
      - Kiểm `ADB -s <máy> shell getprop ro.product.model` → phải là `24095PCADG`.
      - Không tìm thấy máy thì ghi "chờ user cắm lại điện thoại" vào Vướng mắc rồi dừng.
   2. **Cài và mở app:**
      - `ADB -s <máy> install -r client/Builds/walkbench.apk` → `Success`;
      - `ADB -s <máy> logcat -c`;
      - `ADB -s <máy> shell monkey -p com.viventa.walkbench -c android.intent.category.LAUNCHER 1`.
   3. **Khoảng 25 giây sau:**
      - `ADB -s <máy> shell dumpsys window | grep -E "mDreamingLockscreen|KeyguardShowing"`.
      - Nếu không khoá: `ADB -s <máy> exec-out screencap -p > client/Builds/walkbench-0039.png`, mở bằng Read, ghi ra những gì thấy (nhân vật xanh giữa màn hình, địa hình, hộp chữ `WALKBENCH đang đo…`).
      - Nếu màn hình khoá: ghi "chờ user mở khoá" vào Vướng mắc rồi dừng.
   4. **Chờ tổng 90 giây** kể từ lúc mở app, rồi chạy:
      - `ADB -s <máy> logcat -d -s Unity | grep -E "WALKBENCH|Exception"`;
      - `ADB -s <máy> shell dumpsys thermalservice | grep -m1 "Thermal Status"`.
      - Chép nguyên văn dòng `WALKBENCH bắt đầu`, dòng `WALKBENCH device=…` và dòng Thermal vào Kết quả.
      - Chưa có dòng `device=` thì chờ thêm 60 giây rồi đọc lại. Vẫn không có thì ghi lại và dừng.

## Tiêu chí xong
- 4 test `WalkBenchPlayTests` qua. Toàn bộ EditMode + PlayMode qua.
- `git diff --stat 6db16aa -- client/Assets/Game/Client/Scenes/SimView.unity client/Assets/Game/Client/SimView.cs client/Assets/Game/Client/PlayerCharacter.cs client/Assets/Game/Client/FollowCamera.cs` rỗng.
- `git status --short client/ProjectSettings` rỗng.
- Kết quả có dòng `WALKBENCH device=…` từ điện thoại 24095PCADG, có `pass30fps=` và `passNoHole=`, không có `Exception`. Giá trị True/False đều được (QĐ 9).

## Test sau khi xong

### Phần cần test
| Phần | Cách test | Mong đợi |
|---|---|---|
| Điểm dẫn trên vòng (`Carrot`) | `WalkBenchPlayTests.CarrotLeadsAlongCircle` | Đúng 2 điểm, sai số 1e-3 |
| Kiểm vòng trên đất khô (`RouteIsDry`) | `RouteIsDryRejectsSeaAndOutside` | Biển và ngoài world → false |
| Chọn bán kính cho seed mặc định (`PickRadius`) | `DefaultSeedRouteIsDry` | R thuộc ứng viên, `RouteDry` true |
| Tự đi + đo + dòng log (`Update`/`Finish`) | `WalksRouteAndLogsSummary` | Xong trong 60 s, đi > 5 ô, log đủ 14 trường, dừng đi |
| Build script + scene | Bước 3 + bước 7 | Có scene; `WalkBench build: Succeeded` |
| SimView không đổi (regression) | `git diff` ở Tiêu chí xong + `SimViewPlayTests`, `SimViewMapsPlayTests`, `SimFarmUiPlayTests`, `PlayerCharacterPlayTests`, `DropPlayTests` trong PlayMode toàn bộ | Rỗng; qua |
| SimBench / Bench (regression, không sửa) | `SimBenchPlayTests` trong PlayMode toàn bộ | Qua |
| Lõi | EditMode toàn bộ | Qua (không đổi số test) |
| Điện thoại | Bước 8 | Dòng `WALKBENCH device=…`, 0 `Exception` |

### Lệnh executor chạy
1. MCP `read_console` (Error) sau bước 1 và bước 2 → rỗng.
2. MCP `run_tests` PlayMode, lọc `Game.Client.Tests.WalkBenchPlayTests` → 4/4.
3. MCP `run_tests` EditMode (toàn bộ) → ghi `x/x`.
4. MCP `run_tests` PlayMode (toàn bộ) → ghi `x/x`.
5. `git checkout -- client/ProjectSettings/EditorSettings.asset`.
6. MCP `execute_menu_item` `Game/Build APK WalkBench` → `Succeeded`. Sau đó `git checkout -- client/ProjectSettings/UnityConnectSettings.asset`, `git status --short client/ProjectSettings` → rỗng.
7. Bước 8 (adb).
8. Commit code + scene + test + Kết quả. Không commit `client/Builds/`.

### User tự test
Không có thao tác. Planner đọc số liệu điện thoại và báo user (contract: user chỉ đọc số).

## Dừng và chuyển lại Opus khi
- Brief mơ hồ hoặc mâu thuẫn với code thật (vd số dòng lệch làm không chắc sửa chỗ nào).
- Cần một quyết định thuộc AGENTS.md §3.4 hoặc ngoài "Quyết định đã chốt".
- Cùng một lỗi sửa 2 lần vẫn chưa qua.
- Phải sửa file ngoài "Phạm vi".
- Các trường hợp đã ghi ở bước 1, 4, 5, 8.

---
<!-- Từ đây trở xuống: EXECUTOR ghi. -->

## Kết quả
- Base commit: a1983ea
- Chạy bằng: claude-sonnet-5-5 · medium (OK: brief 0039 cần Sonnet · medium)
- Giờ bắt đầu: 2026-10-22 16:54 (+07)
- Commit code: 0e4600b — file: Client/WalkBench.cs, Client/Editor/WalkBenchBuild.cs, Client/Scenes/WalkBench.unity, Tests/Client/WalkBenchPlayTests.cs (+ .meta). Không lệch brief (WalkBench.cs chép nguyên văn; WalkBenchBuild chép từ SimBenchBuild theo bước 2).
- Giờ dừng: 17:05 (+07), dừng ở bước 8.1 vì không có điện thoại.
<!-- Base commit (trước khi làm) · Chạy bằng: <model id> · <effort> (dòng OK của brief_check.py) · commit của brief · file đã đổi · ghi chú lệch so với brief (nếu có, kèm lý do). Giờ bắt đầu/kết thúc theo `date`. -->

## Kết quả test
- Điểm dẫn / RouteIsDry / PickRadius / tự đi + log → đạt: `WalkBenchPlayTests` 4/4.
- Build script + scene → đạt: scene sinh ra, SimView.unity không đổi; console `WalkBench build: Succeeded, 851276220 byte → Builds/walkbench.apk` (35 MB).
- SimView không đổi → `git diff --stat 6db16aa` rỗng.
- `read_console` Error sau bước 1/2 → rỗng. EditMode toàn bộ → 437/437. PlayMode toàn bộ → 121/121 (gồm SimView/PlayerCharacter/SimBench).
- `git status --short client/ProjectSettings` → rỗng (đã checkout EditorSettings + UnityConnectSettings).
- Điện thoại (bước 8) → CHƯA LÀM: không đạt điều kiện máy (xem Vướng mắc).
<!-- Mỗi dòng ở "Phần cần test" → đạt / không đạt / chờ user + bằng chứng (tên test, dòng output).
Mỗi lệnh ở "Lệnh executor chạy": `<lệnh>` → <kết quả thật, dòng tổng kết>. -->

## Vướng mắc
Chờ user cắm lại điện thoại. `adb devices` rỗng (kể cả sau kill-server/start-server); `adb mdns services` chỉ thấy 192.168.1.7:6060 (Android TV — bị cấm dùng); serial PFQCRWVWR46HU4GQ không tìm thấy. APK đã build sẵn ở client/Builds/walkbench.apk. Khi máy cắm lại: chạy tiếp bước 8 (cài, mở, đọc logcat) — không cần build lại.
<!-- Khi trạng thái = cần Opus: câu hỏi cụ thể + các phương án (kèm đề xuất). -->

---
<!-- PLANNER ghi khi review. -->

## Review
<!-- Kết luận: đã duyệt | cần sửa (thêm bước sửa vào "Các bước", trạng thái về chờ làm). Điểm đã đối chiếu. -->
