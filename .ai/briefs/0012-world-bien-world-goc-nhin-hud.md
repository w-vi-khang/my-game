# Brief 0012 — World 7b3 (phần 1): biên world chậm dần + sương, góc nhìn trên HUD

> Trạng thái: chờ review
> Review: đầy đủ + user xem
> Model thực thi: Sonnet · medium
> Cập nhật: 2026-10-20 20:55

<!-- Planner (Opus) viết, Executor (Sonnet) làm theo ở PHIÊN KHÁC — executor không có lịch sử chat, không được tự quyết.
Trạng thái: chờ làm → đang làm → chờ review → đã duyệt; hoặc → cần Opus (executor dừng, hỏi ở mục Vướng mắc).
File được commit. -->

## Mục tiêu
Task [world bước 7](../tasks/m1-world-buoc-7-nhieu-map-minimap.md), dòng **7b3** trong [world-tech](../../design/04-tech/world-tech.md#kế-hoạch-prototype-m1) (phần 2 — mép cỏ kín — là brief 0013, làm sau brief này).
1. Camera bay không ra khỏi world: trong **2 ô cuối** trước mép x/z, tốc độ hướng ra ngoài giảm dần về 0 và **sương dày dần tới che hết** (cả bầu trời); **trần bay = đỉnh world + 16 ô**, cũng chậm dần ở 2 ô cuối (trần không có sương).
2. HUD hiện **góc ngẩng/cúi** của camera: 0° = ngang mặt biển, nhìn lên dương, nhìn xuống âm. Không đổi điều khiển.

## Bối cảnh cần đọc
- `client/Assets/Game/Core/FlyCameraCollision.cs` (cả file, 104 dòng): va chạm camera trên lưới ô. Chữ ký dùng lại:
  `public static void Move(WorldData world, ref float x, ref float y, ref float z, float dx, float dy, float dz, float radius)` (`:45`).
- `client/Assets/Game/Core/WorldData.cs:6-24`: `public readonly struct WorldSize` có `X`, `Y`, `Z` (int), ctor `WorldSize(int x, int y, int z)` bắt buộc chia hết cho 16. `WorldData.Size` kiểu `WorldSize`. Namespace `Game.Core`.
- `client/Assets/Game/Client/DebugFlyCamera.cs:35-48` — `public void Fly(Vector3 delta)`: đổi sang toạ độ terrain (`p`, `d`), gọi `FlyCameraCollision.Move`, đổi lại. Toạ độ terrain = đơn vị ô, world nằm trong [0, Size.X] × [0, Size.Y] × [0, Size.Z].
- `client/Assets/Game/Client/UnderwaterView.cs` (cả file, 80 dòng): `LateUpdate` (`:33-47`) kiểm dưới nước → `Enter()` lưu rồi đặt sương 0–40 màu nước + nền `SolidColor`; `Restore()` trả lại. Trường public: `terrain`, `fogColor`, `backgroundColor`, `fogStart`, `fogEnd`, `IsUnderwater`.
- `client/Assets/Game/Client/Shaders/SkyClouds.shader` (cả file, 102 dòng): skybox của scene TerrainView; `frag` ở `:85-98`, `CBUFFER` ở `:29-33`. `unity_FogColor` đã được khai báo trong URP (`real4 unity_FogColor;` ở `client/Library/PackageCache/com.unity.render-pipelines.universal@13e5115b98bf/ShaderLibrary/UnityInput.hlsl:226`, được `Core.hlsl` include — shader đã include `Core.hlsl` ở `:27`).
- `client/Assets/Game/Client/TerrainEditInput.cs:108-124` — `OnGUI`: dòng hướng dẫn `:112`, nhãn `GUI.Label(new Rect(10, 10, 1000, 90), ...)` ở `:123`.
- `client/Assets/Game/Client/Editor/TerrainViewCapture.cs:88-131` — menu `Game/Nước trong + camera va chạm cho TerrainView` (mẫu cho menu mới: mở scene, tìm component, SetDirty, lưu scene). `ScenePath` ở `:16`.
- Scene TerrainView: sương tuyến tính 64–120 (`TerrainView.unity:17-22`), camera `m_ClearFlags: 1` (Skybox), skybox = `SkyClouds`. World v3 = 384 × 48 × 384 (`client/Assets/Game/Client/Resources/Config/world_maps_m1.json`) → trần 64.
- Test mẫu: `client/Assets/Game/Tests/Core/FlyCameraCollisionTests.cs` (EditMode, NUnit), `client/Assets/Game/Tests/Client/FlyCameraWaterPlayTests.cs` (PlayMode: dựng `TerrainChunkView` mặc định = đảo v2 64 × 32 × 64, `S = view.Editor.SeaLevel`).

API Unity dùng (đã tra 6000.4):
- `Shader.SetGlobalFloat(string name, float value)`, `Shader.GetGlobalFloat(string name)` — [docs](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/Shader.SetGlobalFloat.html), [docs](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/Shader.GetGlobalFloat.html). Biến global chỉ có tác dụng khi shader **không** khai báo nó trong `Properties` / `CBUFFER UnityPerMaterial`.
- `RenderSettings.fogStartDistance` / `fogEndDistance` — [docs](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/RenderSettings-fogStartDistance.html), [docs](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/RenderSettings-fogEndDistance.html).
- `Camera.backgroundColor` — [docs](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/Camera-backgroundColor.html); `Color.Lerp` — [docs](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/Color.Lerp.html); `Transform.forward` — [docs](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/Transform-forward.html); `MenuItem` — [docs](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/MenuItem.html).

## Quyết định đã chốt (không được đổi)
1. **Vùng chậm** = 2 ô cuối trước mép (user). Mỗi trục tính riêng, chỉ giảm thành phần **hướng ra ngoài**; hướng vào trong giữ nguyên. Hệ số = khoảng còn lại / 2 (tuyến tính) → càng gần càng chậm, tiệm cận mép. Kèm kẹp cứng: không bao giờ vượt mép trong một lần dời (khung dài).
2. Biên ngang = [0, Size.X] và [0, Size.Z] (toạ độ terrain). Trần = `Size.Y + 16` (user; world v3 → 64). Không có giới hạn dưới mới (đáy đã là ô đặc trong `FlyCameraCollision`).
3. Camera đang ở ngoài biên (vd đặt tay) → không kéo về; chỉ chặn đi xa thêm, đi vào thì bình thường.
4. Giới hạn áp **trước** va chạm: `LimitDelta` → `FlyCameraCollision.Move`. Logic ở Game.Core (C# thuần), không UnityEngine.
5. **Sương biên** chỉ theo mép ngang (x/z), không theo trần. `t = 1 − clamp01(khoảng cách ngang nhỏ nhất tới 4 mép / 2)`. Cuối sương: `end = baseEnd × (EdgeFogEnd / baseEnd)^t` (giảm theo cấp số nhân — dày dần đều cảm giác), `EdgeFogEnd = 1`; đầu sương: `start = end × (baseStart / baseEnd) × (1 − t)` (luôn < end). Base = sương scene (64–120) trên cạn, `UnderwaterView.fogStart/fogEnd` (0–40) dưới nước.
6. Bầu trời: skybox `SkyClouds` trộn về màu sương theo biến global `_EdgeFogBlend` = t (không sửa material asset). Dưới nước (nền SolidColor): màu nền = `Color.Lerp(underwater.backgroundColor, underwater.fogColor, t)`.
7. Component mới `WorldEdgeFog` trên camera; không phụ thuộc thứ tự chạy script: mỗi `LateUpdate` nó gọi `underwater.Refresh()` trước (hàm mới, tách từ `UnderwaterView.LateUpdate`, gọi hai lần trong cùng khung không đổi gì) rồi mới ghi sương. Không dùng `DefaultExecutionOrder`.
8. Góc nhìn = `asin(forward.y)` đổi ra độ (độc lập Euler); hiển thị số nguyên làm tròn, có dấu `+` khi dương, `-` khi âm, `0°` khi tròn về 0. Dòng HUD nguyên văn ở bước 7.
9. Không đổi: tốc độ bay, điều khiển chuột giữa, va chạm, sương/hiệu ứng dưới nước khi ở xa mép, Bench (không build lại scene Bench trong brief này — Planner làm khi đo điện thoại).

## Phạm vi
- Được sửa/tạo:
  - `client/Assets/Game/Core/FlyCameraBounds.cs` (mới)
  - `client/Assets/Game/Client/DebugFlyCamera.cs`, `client/Assets/Game/Client/UnderwaterView.cs`, `client/Assets/Game/Client/WorldEdgeFog.cs` (mới), `client/Assets/Game/Client/TerrainEditInput.cs`
  - `client/Assets/Game/Client/Shaders/SkyClouds.shader`
  - `client/Assets/Game/Client/Editor/TerrainViewCapture.cs`, `client/Assets/Game/Client/Scenes/TerrainView.unity` (chỉ qua menu ở bước 6)
  - `client/Assets/Game/Tests/Core/FlyCameraBoundsTests.cs` (mới), `client/Assets/Game/Tests/Client/WorldBoundsPlayTests.cs` (mới); file `.meta` Unity tự sinh cho các file mới
  - Mục Kết quả / Kết quả test / Vướng mắc của brief này
- Không được sửa: `FlyCameraCollision.cs`, `BlockMesher.cs` (brief 0013), scene Bench, `design/`, test cũ, mọi file khác.

## Các bước
1. **`client/Assets/Game/Core/FlyCameraBounds.cs`** (mới, namespace `Game.Core`, `using System;`) — chép đúng:
   ```csharp
   /// <summary>
   /// Biên bay của camera (world-tech bước 7b3, brief 0012): trong SlowZone ô cuối trước mép x/z và trần, thành phần vận tốc
   /// hướng ra ngoài giảm tuyến tính về 0, không bao giờ vượt biên; sương dày dần theo khoảng cách tới mép ngang.
   /// </summary>
   public static class FlyCameraBounds
   {
       /// <summary>Bề rộng vùng chậm dần trước mép / trần (ô).</summary>
       public const float SlowZone = 2f;
       /// <summary>Trần bay cao hơn đỉnh world chừng này ô.</summary>
       public const int CeilingAbove = 16;
       /// <summary>Cuối sương (đơn vị thế giới) khi camera ở đúng mép world.</summary>
       public const float EdgeFogEnd = 1f;

       public static float CeilingY(WorldSize s) => s.Y + CeilingAbove;

       /// <summary>Dời d theo một trục từ pos, biên [min, max]: hướng ra ngoài nhân (khoảng còn lại / SlowZone, tối đa 1) và không vượt biên; đã ở ngoài biên thì không đi xa thêm.</summary>
       public static float LimitAxis(float pos, float d, float min, float max)
       {
           if (d > 0f)
           {
               float room = max - pos;
               if (room <= 0f) return 0f;
               return Math.Min(d * Math.Min(room / SlowZone, 1f), room);
           }
           if (d < 0f)
           {
               float room = pos - min;
               if (room <= 0f) return 0f;
               return Math.Max(d * Math.Min(room / SlowZone, 1f), -room);
           }
           return d;
       }

       /// <summary>Giới hạn một lần dời camera (toạ độ terrain): x ∈ [0, Size.X], z ∈ [0, Size.Z], y ≤ CeilingY; không giới hạn dưới.</summary>
       public static void LimitDelta(WorldSize s, float x, float y, float z, ref float dx, ref float dy, ref float dz)
       {
           dx = LimitAxis(x, dx, 0f, s.X);
           dz = LimitAxis(z, dz, 0f, s.Z);
           dy = LimitAxis(y, dy, float.NegativeInfinity, CeilingY(s));
       }

       /// <summary>Độ dày sương biên: 0 khi cách mép ngang gần nhất ≥ SlowZone, 1 ở mép hoặc ngoài world.</summary>
       public static float EdgeFactor(WorldSize s, float x, float z)
       {
           float dist = Math.Min(Math.Min(x, s.X - x), Math.Min(z, s.Z - z));
           return 1f - Math.Min(Math.Max(dist / SlowZone, 0f), 1f);
       }

       /// <summary>Sương tuyến tính khi độ dày biên = t: cuối sương giảm theo cấp số nhân từ baseEnd về EdgeFogEnd, đầu sương về 0 và luôn nhỏ hơn cuối.</summary>
       public static void EdgeFog(float baseStart, float baseEnd, float t, out float start, out float end)
       {
           end = baseEnd * (float)Math.Pow(EdgeFogEnd / baseEnd, t);
           start = end * (baseStart / baseEnd) * (1f - t);
       }

       /// <summary>Góc ngẩng của hướng nhìn (độ): 0 = ngang, dương = nhìn lên. forwardY = thành phần y của vector hướng nhìn đơn vị.</summary>
       public static float ElevationDegrees(float forwardY) =>
           (float)(Math.Asin(Math.Min(Math.Max(forwardY, -1f), 1f)) * 180.0 / Math.PI);

       /// <summary>Chuỗi góc cho HUD: số nguyên làm tròn, "+12°", "-5°", "0°".</summary>
       public static string FormatElevation(float degrees)
       {
           int r = (int)Math.Round(degrees, MidpointRounding.AwayFromZero);
           return r > 0 ? $"+{r}°" : r < 0 ? $"-{-r}°" : "0°";
       }
   }
   ```
   Xong khi: biên dịch không lỗi (`read_console` không có error).

2. **`DebugFlyCamera.cs`** — trong `Fly` (`:45-46`), giữa `float x = p.x, ...;` và `FlyCameraCollision.Move(...)` thêm:
   ```csharp
   float dx = d.x, dy = d.y, dz = d.z;
   FlyCameraBounds.LimitDelta(terrain.World.Size, x, y, z, ref dx, ref dy, ref dz);
   ```
   và đổi lời gọi thành `FlyCameraCollision.Move(terrain.World, ref x, ref y, ref z, dx, dy, dz, collisionRadius);`. Thêm vào summary của class (sau dòng `Bước 7b2 ...` ở `:10`) dòng: `/// Bước 7b3 (brief 0012): không ra khỏi world — chậm dần trong 2 ô cuối trước mép x/z và trần (đỉnh world + 16 ô).` Nhánh `terrain == null` giữ nguyên (bay tự do). Xong khi: biên dịch không lỗi.

3. **`UnderwaterView.cs`** — đổi `void LateUpdate()` thành:
   ```csharp
   void LateUpdate()
   {
       Refresh();
   }

   /// <summary>Kiểm camera có dưới nước không và bật/tắt hiệu ứng khi đổi trạng thái; gọi nhiều lần trong một khung không đổi gì thêm (WorldEdgeFog gọi trước khi ghi sương, brief 0012).</summary>
   public void Refresh()
   {
       // thân cũ của LateUpdate (dòng 35-46), giữ nguyên từng dòng
   }
   ```
   Xong khi: biên dịch không lỗi; `FlyCameraWaterPlayTests` vẫn đạt (bước 9).

4. **`SkyClouds.shader`** — ngay sau `CBUFFER_END` (`:33`) thêm:
   ```hlsl
   // Sương biên world (brief 0012): 0 = trời bình thường, 1 = cả trời màu sương. Biến global (Shader.SetGlobalFloat), không khai báo trong Properties.
   half _EdgeFogBlend;
   ```
   và trong `frag`, thay `return half4(c, 1);` (`:97`) bằng:
   ```hlsl
   c = lerp(c, unity_FogColor.rgb, _EdgeFogBlend);
   return half4(c, 1);
   ```
   Xong khi: shader biên dịch không lỗi (`read_console`), scene TerrainView vẫn thấy trời có mây khi đứng giữa world.

5. **`client/Assets/Game/Client/WorldEdgeFog.cs`** (mới, namespace `Game.Client`) — chép đúng:
   ```csharp
   using Game.Core;
   using UnityEngine;

   namespace Game.Client
   {
       /// <summary>
       /// Sương biên world (world-tech bước 7b3, brief 0012): trong 2 ô cuối trước mép x/z, sương dày dần tới che hết — cả bầu trời
       /// (skybox Game/SkyClouds trộn màu sương qua biến global _EdgeFogBlend) và nền dưới nước. Trần bay không có sương.
       /// Gọi UnderwaterView.Refresh() trước khi ghi nên không phụ thuộc thứ tự LateUpdate giữa hai component.
       /// </summary>
       [RequireComponent(typeof(Camera))]
       public class WorldEdgeFog : MonoBehaviour
       {
           public const string BlendProperty = "_EdgeFogBlend";

           public TerrainChunkView terrain;
           /// <summary>Hiệu ứng dưới nước của cùng camera; null → chỉ xử lý trên cạn.</summary>
           public UnderwaterView underwater;

           /// <summary>Độ dày sương biên của khung gần nhất (0..1).</summary>
           public float Factor { get; private set; }

           Camera cam;
           float baseStart, baseEnd;

           void Start()
           {
               cam = GetComponent<Camera>();
               baseStart = RenderSettings.fogStartDistance;
               baseEnd = RenderSettings.fogEndDistance;
           }

           void LateUpdate()
           {
               if (terrain == null || terrain.World == null)
                   return;
               if (underwater != null)
                   underwater.Refresh();
               var p = terrain.transform.InverseTransformPoint(transform.position);
               Factor = FlyCameraBounds.EdgeFactor(terrain.World.Size, p.x, p.z);
               bool under = underwater != null && underwater.IsUnderwater;
               float s0 = under ? underwater.fogStart : baseStart;
               float e0 = under ? underwater.fogEnd : baseEnd;
               FlyCameraBounds.EdgeFog(s0, e0, Factor, out float start, out float end);
               RenderSettings.fogStartDistance = start;
               RenderSettings.fogEndDistance = end;
               Shader.SetGlobalFloat(BlendProperty, Factor);
               if (under)
                   cam.backgroundColor = Color.Lerp(underwater.backgroundColor, underwater.fogColor, Factor);
           }

           void OnDisable()
           {
               Factor = 0f;
               Shader.SetGlobalFloat(BlendProperty, 0f);
               if (underwater != null && underwater.IsUnderwater)
               {
                   RenderSettings.fogStartDistance = underwater.fogStart;
                   RenderSettings.fogEndDistance = underwater.fogEnd;
                   if (cam != null)
                       cam.backgroundColor = underwater.backgroundColor;
               }
               else if (cam != null)
               {
                   RenderSettings.fogStartDistance = baseStart;
                   RenderSettings.fogEndDistance = baseEnd;
               }
           }
       }
   }
   ```
   (`cam != null` ở nhánh trên cạn = đã chạy `Start`, tức base đã lưu.) Xong khi: biên dịch không lỗi.

6. **`TerrainViewCapture.cs`** — thêm menu sau `SetupWaterAndCamera` (trước `Capture`):
   ```csharp
   /// <summary>Bước 7b3 (brief 0012): sương biên world trên camera của TerrainView.</summary>
   [MenuItem("Game/Biên world cho TerrainView")]
   public static void SetupWorldEdge()
   {
       EnsureScene();
       var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
       var view = Object.FindAnyObjectByType<TerrainChunkView>();
       var cam = Camera.main;
       var edge = cam.GetComponent<WorldEdgeFog>();
       if (edge == null)
           edge = cam.gameObject.AddComponent<WorldEdgeFog>();
       edge.terrain = view;
       edge.underwater = cam.GetComponent<UnderwaterView>();
       EditorUtility.SetDirty(edge);
       EditorSceneManager.MarkSceneDirty(scene);
       EditorSceneManager.SaveScene(scene);
       Debug.Log("TERRAINVIEW đã thêm sương biên world");
   }
   ```
   Chạy menu này một lần qua MCP (`execute_menu_item` "Game/Biên world cho TerrainView"). Xong khi: console có dòng `TERRAINVIEW đã thêm sương biên world`; `git diff --stat` có `TerrainView.unity`; trong scene, camera có `WorldEdgeFog` với `terrain` và `underwater` khác null (kiểm bằng MCP `find_gameobjects`/`manage_components` hoặc đọc file scene có `m_Script` của WorldEdgeFog).

7. **`TerrainEditInput.cs` `OnGUI`** — sau dòng `var text = "Chuột trái: đào ...\nKho: ";` (`:112`), giữ nguyên dòng đó và chèn góc nhìn vào **trước** chữ `Kho: ` bằng cách đổi dòng `:112` thành:
   ```csharp
   var text = "Chuột trái: đào · Chuột phải: đặt loại đang chọn · 1–4 / lăn chuột: chọn loại · WASD, Q/E, giữ chuột giữa: camera · dừng bay mới đào/đặt được\n";
   var cam = Camera.main;
   if (cam != null)
       text += $"Góc nhìn: {FlyCameraBounds.FormatElevation(FlyCameraBounds.ElevationDegrees(cam.transform.forward.y))} (0° = ngang mặt biển, + nhìn lên, - nhìn xuống)\n";
   text += "Kho: ";
   ```
   và đổi `new Rect(10, 10, 1000, 90)` (`:123`) thành `new Rect(10, 10, 1000, 115)`. (File đã `using Game.Core;`.) Xong khi: biên dịch không lỗi; trong Play scene TerrainView, dòng thứ 2 của nhãn góc trái là `Góc nhìn: …`.

8. **Test EditMode** `client/Assets/Game/Tests/Core/FlyCameraBoundsTests.cs` (mới, namespace `Game.Core.Tests`, `using NUnit.Framework;`), class `FlyCameraBoundsTests`, `const float Eps = 1e-4f;`, mỗi mục một `[Test]`:
   - `LimitAxisFarFromEdgeIsUnchanged`: `LimitAxis(10, -0.5f, 0, 384)` = −0,5; `LimitAxis(10, 0.5f, 0, 384)` = 0,5; `LimitAxis(5, 0, 0, 384)` = 0.
   - `LimitAxisSlowsOnlyOutward`: `LimitAxis(1, -0.5f, 0, 384)` = −0,25; `LimitAxis(1, 0.5f, 0, 384)` = 0,5; `LimitAxis(383, 0.5f, 0, 384)` = 0,25; `LimitAxis(383, -0.5f, 0, 384)` = −0,5.
   - `LimitAxisNeverCrossesEdge`: `LimitAxis(0.1f, -3f, 0, 384)` = −0,1 (±Eps); `LimitAxis(0, -1f, 0, 384)` = 0; `LimitAxis(-2, -1f, 0, 384)` = 0; `LimitAxis(-2, 1f, 0, 384)` = 1.
   - `CeilingIsWorldTopPlus16`: `var s = new WorldSize(384, 48, 384);` → `CeilingY(s)` = 64. Với `LimitDelta(s, 100, 63, 100, ref dx, ref dy, ref dz)` lần lượt: (dx, dy, dz) = (0, 1, 0) → dy = 0,5; (0, 5, 0) tại y = 63,9 → dy = 0,1 (±Eps); (0, −5, 0) tại y = 63,9 → dy = −5; (0, −5, 0) tại y = 2 → dy = −5 (không giới hạn dưới).
   - `ApproachEdgeIsSmoothAndStops`: x = 5; lặp 600 lần `x += LimitAxis(x, -0.16f, 0, 384)`; mỗi lần kiểm x ≥ 0 và x không tăng; cuối cùng x < 0,01. (Tay: x < 2 thì mỗi bước x × 0,92 → sau ~580 bước ≈ 2 × 0,92^580 ≈ 0.)
   - `EdgeFactorByDistance`: `s = new WorldSize(384, 48, 384)`: `EdgeFactor(s, 192, 192)` = 0; `(1, 192)` = 0,5; `(0, 192)` = 1; `(-1, 192)` = 1; `(383, 192)` = 0,5; `(192, 0.5f)` = 0,75; `(1, 0.5f)` = 0,75 (lấy mép gần nhất); `(2, 192)` = 0.
   - `EdgeFogInterpolates`: `EdgeFog(64, 120, 0, ...)` → (64, 120); `t = 1` → start 0, end 1; `t = 0.5f` → end = 10,954 (±1e-3, = √120), start = 2,921 (±1e-3); `EdgeFog(0, 40, 0.5f, ...)` → start 0, end 6,325 (±1e-3). Với t = 0, 0,05, …, 1: start < end và end không tăng khi t tăng.
   - `ElevationDegrees`: `ElevationDegrees(0)` = 0; `(1)` = 90; `(-1)` = −90; `(0.5f)` = 30 (±1e-3); `(1.0001f)` = 90.
   - `FormatElevation`: `12.4f` → `"+12°"`; `-0.4f` → `"0°"`; `-5.6f` → `"-6°"`; `0` → `"0°"`; `89.5f` → `"+90°"`.

9. **Test PlayMode** `client/Assets/Game/Tests/Client/WorldBoundsPlayTests.cs` (mới, namespace `Game.Client.Tests`), dựng giống `FlyCameraWaterPlayTests` (`:19-41`: `TerrainChunkView` mặc định = đảo v2 64 × 32 × 64, `yield return null`, camera + `DebugFlyCamera` (terrain = view) + `UnderwaterView` (terrain = view)). Thêm: trong `SetUp`, **trước** khi thêm `WorldEdgeFog`, lưu `RenderSettings.fog/fogMode/fogStartDistance/fogEndDistance` rồi đặt `fog = true`, `fogMode = FogMode.Linear`, start 64, end 120; thêm `WorldEdgeFog edge` (terrain = view, underwater = under); `yield return null` (để `Start` lưu base). `TearDown`: huỷ object, `yield return null`, trả lại 4 giá trị RenderSettings đã lưu, `Shader.SetGlobalFloat(WorldEdgeFog.BlendProperty, 0)`. Các `[UnityTest]`:
   - `FlyStopsAtWorldEdge`: đặt camera (3, 36, 32) (trên mọi ô — Size.Y = 32); 300 lần `fly.Fly(new Vector3(-0.5f, 0, 0))` → x ≥ 0 và x < 0,01; sau đó `fly.Fly(new Vector3(1, 0, 0))` → x tăng đúng 1 (±1e-3) (hướng vào trong không bị chậm dù đang ở vùng chậm).
   - `FlyStopsAtCeiling`: đặt (32, 40, 32); 300 lần `fly.Fly(new Vector3(0, 1, 0))` → y ≤ 48 + 1e-4 và y > 47,99.
   - `EdgeFogFollowsDistance`: đặt (32, 36, 32), `yield return null` → `RenderSettings.fogEndDistance` = 120 (±1e-3), start = 64, `Shader.GetGlobalFloat("_EdgeFogBlend")` = 0. Đặt (0, 36, 32), `yield return null` → end = 1, start = 0, blend = 1, `edge.Factor` = 1. Đặt (1, 36, 32), `yield return null` → end = 10,954 (±1e-2), blend = 0,5. Đặt lại (32, 36, 32), `yield return null` → end = 120, start = 64, blend = 0. Cuối: `edge.enabled = false` → end = 120, start = 64, blend = 0.
   - `EdgeFogUnderwater`: tìm z trong [16, Size.Z − 16) có ô (1, S, z) là `BlockMaterial.Water` và (1, S + 1, z) là `BlockMaterial.Empty` (không có → `Assert.Fail("Không có ô nước ở mép")`); đặt camera (1.0f, S + 0.3f, z + 0.5f); `yield return null` → `under.IsUnderwater` = true; end = 6,325 (±1e-2); start = 0; `cam.backgroundColor` ≈ `Color.Lerp(under.backgroundColor, under.fogColor, 0.5f)` (từng kênh ±1e-3). Đặt (32, 36, 32), `yield return null` → `under.IsUnderwater` = false, end = 120 (±1e-3), start = 64, `cam.clearFlags` trở lại giá trị ban đầu (lưu ngay sau SetUp).

10. Chạy test + kiểm tay theo mục "Lệnh executor chạy"; chụp 3 ảnh Game view trong Play scene TerrainView (MCP), lưu vào `client/Logs/` (đã ignore, không commit): `edge_far.png` (camera (192, 30, 192)), `edge_1cell.png` (camera (1, 30, 192)), `edge_at.png` (camera (0.05, 30, 192)); cả ba rotation Euler (10, −90, 0) (nhìn ra phía −x, hơi cúi); đặt vị trí qua MCP rồi chờ ≥ 30 khung cho chunk nạp xong mới chụp. Commit code + test + scene: `world 7b3: biên world chậm dần + sương, góc nhìn trên HUD (brief 0012)`. Điền mục Kết quả (base commit, giờ bắt đầu/kết thúc, model + effort, file đổi, lệch so với brief).

## Tiêu chí xong
- `FlyCameraBoundsTests` 9/9 đạt; `WorldBoundsPlayTests` 4/4 đạt; toàn bộ EditMode và PlayMode đạt (EditMode trước brief: 260, PlayMode: 15).
- `Game.Core` không có `using UnityEngine` trong `FlyCameraBounds.cs`.
- Scene TerrainView có `WorldEdgeFog` trên camera (terrain + underwater gán sẵn).
- 3 ảnh ở `client/Logs/`: `edge_far.png` trời có mây như cũ; `edge_1cell.png` sương dày rõ; `edge_at.png` gần như một màu sương.

## Test sau khi xong

### Phần cần test
- `FlyCameraBounds.LimitAxis` / `LimitDelta` (bước 1) · EditMode 5 test đầu ở bước 8 · giá trị đúng như bước 8.
- `EdgeFactor`, `EdgeFog` (bước 1) · EditMode `EdgeFactorByDistance`, `EdgeFogInterpolates` · đúng số ở bước 8.
- `ElevationDegrees`, `FormatElevation` (bước 1, dùng ở bước 7) · EditMode 2 test cuối bước 8 · đúng chuỗi/số.
- `DebugFlyCamera.Fly` có biên (bước 2) · PlayMode `FlyStopsAtWorldEdge`, `FlyStopsAtCeiling` · dừng sát mép/trần, không vượt; đi vào không bị chậm.
- `WorldEdgeFog` + `UnderwaterView.Refresh` (bước 3, 5) · PlayMode `EdgeFogFollowsDistance`, `EdgeFogUnderwater` · sương/nền/blend đúng theo khoảng cách, trên cạn và dưới nước, trả lại khi rời mép và khi tắt component.
- Skybox trộn màu sương (bước 4) · ảnh `edge_far.png`, `edge_1cell.png`, `edge_at.png` · trời bình thường ở giữa; trời chuyển dần sang màu sương ở mép.
- HUD góc nhìn (bước 7) · Play TerrainView: đặt camera rotation Euler (−20, 0, 0) qua MCP (x âm = ngẩng lên), chụp ảnh Game view và đọc nhãn góc trái · dòng `Góc nhìn: +20° (0° = ngang mặt biển, + nhìn lên, - nhìn xuống)`.
- Regression: va chạm camera + dưới nước · `FlyCameraCollisionTests`, `FlyCameraWaterPlayTests`, `FlyCameraMoveKeyPlayTests` · vẫn đạt.
- Regression: nạp dần chunk, đào/đặt, SimView · `TerrainStreamingPlayTests`, `TerrainEditPlayTests`, `SimViewPlayTests` · vẫn đạt.

### Lệnh executor chạy
1. MCP `refresh_unity` → `read_console` (types error) — 0 lỗi biên dịch.
2. MCP `execute_menu_item` "Game/Biên world cho TerrainView" → `read_console` thấy dòng log.
3. MCP `run_tests` EditMode (cả bộ) → ghi dòng tổng kết (số đạt / tổng).
4. MCP `run_tests` PlayMode (cả bộ) → ghi dòng tổng kết. Sau đó dọn: `git checkout -- client/ProjectSettings/EditorSettings.asset`.
5. Play scene TerrainView qua MCP, đặt camera theo bước 10 và kiểm HUD, chụp ảnh; dừng Play.
6. `python3 tools/check_design.py` (không đổi design/, chỉ để chắc chắn vẫn exit 0).
7. `git status --short` — chỉ còn file trong Phạm vi; commit.

### User tự test
Mở scene TerrainView, Play (chuột):
1. Bay (WASD, Shift) thẳng ra phía mép world (vd từ giữa bay theo −x tới x ≈ 0): trong ~2 ô cuối thấy **chậm dần và dừng hẳn**, không có cảm giác đụng vách; sương **dày dần tới che hết**, cả trời chuyển sang màu sương. Quay đầu bay vào → sương tan dần, tốc độ bình thường ngay.
2. Bay lên cao (E) tới trần (y = 64): chậm dần và dừng, không có sương.
3. Lặn xuống nước gần mép world rồi bay ra mép: sương nước dày dần, nền chuyển sang màu sương nước.
4. Góc trái màn hình: dòng `Góc nhìn: …` — nhìn ngang ≈ `0°`, giữ chuột giữa ngẩng lên → số dương, cúi xuống → số âm.

## Dừng và chuyển lại Opus khi
- Brief mơ hồ hoặc mâu thuẫn với code thật (vd số dòng lệch nhiều, chữ ký khác).
- Cần một quyết định thuộc AGENTS.md §3.4 hoặc ngoài "Quyết định đã chốt" (vd muốn đổi công thức sương, vùng chậm, thêm sương ở trần).
- Cần API Unity không có trong brief.
- Một số test ở bước 8/9 ra khác số mong đợi mà không phải lỗi gõ — ghi số thật, không tự sửa số trong test.
- Cùng một lỗi sửa 2 lần vẫn chưa qua.
- Phải sửa file ngoài "Phạm vi".

---
<!-- Từ đây trở xuống: EXECUTOR ghi. -->

## Kết quả
- Base commit: 6d3d055
- Chạy bằng: claude-sonnet-5-5 · medium
- Bắt đầu: 2026-10-20 19:50 · Kết thúc: ~2026-10-20 20:25 (~35 phút, phần lớn là chờ EditMode ~3,5 phút)
- Commit code: ced4c10 — DebugFlyCamera.cs, TerrainViewCapture.cs, TerrainView.unity, SkyClouds.shader, TerrainEditInput.cs, UnderwaterView.cs, WorldEdgeFog.cs(+meta), FlyCameraBounds.cs(+meta), FlyCameraBoundsTests.cs(+meta), WorldBoundsPlayTests.cs(+meta)
- Lệch brief: không có (chép đúng). Ghi chú: Editor đang ở Play lúc đầu → phải `manage_editor stop` trước khi chạy menu/test; ảnh MCP lưu nhầm vào client/client/Logs, đã chuyển về client/Logs. PlayMode tổng 19 (brief ghi 15 + 4 mới).

## Kết quả test
- LimitAxis/LimitDelta, EdgeFactor, EdgeFog, ElevationDegrees, FormatElevation: đạt — EditMode FlyCameraBoundsTests 9/9.
- DebugFlyCamera.Fly có biên: đạt — PlayMode FlyStopsAtWorldEdge, FlyStopsAtCeiling.
- WorldEdgeFog + UnderwaterView.Refresh: đạt — PlayMode EdgeFogFollowsDistance, EdgeFogUnderwater.
- Skybox trộn sương: đạt (xem ảnh) — edge_far.png trời có mây; edge_1cell.png trời nhạt, mây mờ, đất ngoài ~11 ô ẩn; edge_at.png gần như một màu sương.
- HUD góc nhìn: đạt — Euler (−20,0,0) cho nhãn "Góc nhìn: +20° (0° = ngang mặt biển, …)" (hud_pitch.png; chữ khó đọc trên nền trời sáng, chờ user xem).
- Regression: đạt — EditMode 269/269, PlayMode 19/19 (gồm FlyCameraWater, MoveKey, Streaming, Edit, SimView).
- check_design.py: exit 0. Đã `git checkout -- EditorSettings.asset`.
- Chờ user: mục "User tự test" 1–4 (cảm giác chậm dần, sương, nước gần mép).

## Vướng mắc

---
<!-- PLANNER ghi khi review. -->

## Review
- Chạy bằng `claude-sonnet-5-5 · medium` khớp Model thực thi. 9/9 dòng "Phần cần test" có kết quả; dòng skybox + HUD + "User tự test" chờ user.
- Diff ced4c10 (14 file, đều trong Phạm vi): `FlyCameraBounds.cs`, `WorldEdgeFog.cs` khớp nguyên văn bước 1, 5 (`diff -w` [run]); `DebugFlyCamera`, `UnderwaterView.Refresh`, `SkyClouds` (`_EdgeFogBlend` ngoài CBUFFER), HUD, menu đúng bước 2–4, 6–7. Scene: camera có `WorldEdgeFog` (terrain 1663237067, underwater 41987369). `FlyCameraBounds.cs` chỉ `using System;`.
- Test: FlyCameraBoundsTests 9 `[Test]`, WorldBoundsPlayTests 4 `[UnityTest]`, giá trị assert đúng bước 8–9; tổng 269/269 + 19/19 (không chạy lại — không đáng ngờ).
- Ảnh: `edge_1cell.png` đất ẩn, trời nhạt dần (đúng end ≈ 11); `hud_pitch.png` đọc được `+20°` nhưng chữ trắng mờ trên nền trời sáng.
- Giờ: kết quả ghi "kết thúc ~20:25 (~35 phút)" nhưng commit kết quả 19:57, ảnh 19:56 → thực tế ~7 phút theo git; roadmap ghi theo git.
- Đề xuất (ngoài phạm vi): nhãn HUD khó đọc trên trời sáng → nền mờ/viền chữ, nếu user muốn thì brief riêng.
- Chờ user xem (mục "User tự test" 1–4) rồi mới kết luận.
