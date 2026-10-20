# Brief 0011 — World bước 7b2: nước trong + camera bay không xuyên khối

> Trạng thái: chờ làm
> Review: đầy đủ + user xem
> Model thực thi: Sonnet · medium
> Cập nhật: 2026-10-20 19:15 (sửa lần 1: từ dưới nước nhìn lên tối)

<!-- Planner (Opus) viết, Executor (Sonnet) làm theo ở PHIÊN KHÁC — executor không có lịch sử chat, không được tự quyết.
Trạng thái: chờ làm → đang làm → chờ review → đã duyệt; hoặc → cần Opus (executor dừng, hỏi ở mục Vướng mắc).
Review: nhẹ (đọc mục Kết quả) | đầy đủ (đọc cả diff) | user xem (có phần user tự kiểm bằng mắt/chơi thử) — có thể ghép "đầy đủ + user xem".
Model thực thi: BẮT BUỘC, đúng dạng `<Model> · <effort>` — Model: Opus | Sonnet | Haiku | Fable; effort: low | medium | high | xhigh | max (chọn theo AGENTS.md §10.2).
Executor kiểm tra model + effort của phiên TRƯỚC khi làm (scripts/brief_check.py); lệch hoặc không xác định được → cảnh báo user và DỪNG.
File được commit. Tự kiểm trước khi giao (AGENTS.md §10.3): executor có phải hỏi gì không? còn chỗ nào bỏ ngỏ 2 phương án? -->

## Mục tiêu
Bước 7b2 của [task world bước 7](../tasks/m1-world-buoc-7-nhieu-map-minimap.md), dòng 7b2 trong [world-tech](../../design/04-tech/world-tech.md#kế-hoạch-prototype-m1) (user chốt 2026-10-20 khi xem brief 0010). Trong scene TerrainView: mặt nước theo ô trong suốt, thấy đáy; camera bay vào được ô nước nhưng không vào ô đặc, trượt dọc vách; camera dưới nước có sương ngắn màu nước, nhìn lên vẫn thấy bờ.
Xong khi: test camera không vào ô đặc, vào được ô nước đạt; ≥ 30 FPS trên điện thoại; user xem thấy đáy nước nông và từ dưới nước thấy bờ.

## Bối cảnh cần đọc
- `client/Assets/Game/Client/DebugFlyCamera.cs` — đọc cả file (53 dòng). Dời camera ở `:42-50`, dòng cuối `transform.position += move * (v * Time.deltaTime);` `:50`.
- `client/Assets/Game/Client/Shaders/Ocean.shader` — đọc cả file (136 dòng). `Properties` `:6-13`; `CBUFFER` `:21-25`; pass `ForwardLit` `:28-114` (Tags `:31`); `frag` `:91-112`: pháp tuyến `n` `:97`, `fres` `:100`, sương + `return` `:109-111`.
- `client/Assets/Game/Client/Materials/Ocean.mat` — material dùng chung cho mặt biển nền (`OceanBackdrop`) **và** mặt nước theo ô (`TerrainChunkView.waterMaterial`) trong cả 3 scene TerrainView/SimView/Bench (guid `3c4f12bab1e8847abb25f093937a9af8`).
- `client/Assets/Game/World/BlockMesher.cs`:
  - `:39` `public const float WaterSurfaceDrop = 0.1f;` — mặt nước của ô nước có không khí phía trên nằm ở `y + 1 − 0,1`.
  - `:383-389` ô nước chỉ sinh mặt trên (`AddWaterSurface` `:806-817`, pháp tuyến +y, một mặt); `:392-397` mặt ô đặc kề nước **có** được sinh (nước không tính là đặc) → đáy dưới nước đã có mesh.
  - `:849-862` `IsSolidMaterial` / `IsSolid`: y < 0 đặc, y ≥ SizeY trống, ngoài world theo x/z lấy cột biên gần nhất. Lớp mới ở bước 1 phải dùng **đúng** luật này.
- `client/Assets/Game/Core/WorldData.cs` — `WorldSize(int x, int y, int z)` (bội của 16) `:12`; `WorldData(WorldSize size)` `:60`; `Set(int x, int y, int z, ushort cell)` `:79`; `GetMaterial(int x, int y, int z)` `:81`; `SurfaceHeight(int x, int z)` `:84` (ô rắn cao nhất, không tính nước; −1 nếu rỗng).
- `client/Assets/Game/Core/Cell.cs` — `enum BlockMaterial : byte { Empty = 0, Grass, Dirt, Stone, Sand, Water = 5, Bedrock = 6 }` `:4-14`; `public static ushort Make(BlockMaterial material, int waterLevel = 0)` `:32`.
- `client/Assets/Game/Core/WorldEditor.cs:40` — `public readonly int SeaLevel;` (ô y ≤ SeaLevel là nước biển). `TerrainChunkView.Editor` trả `WorldEditor`.
- `client/Assets/Game/Client/TerrainChunkView.cs` — `World` (`WorldData`), `Editor`, `waterMaterial` `:23`; `CellAt(Vector3)` `:201-205` dùng `transform.InverseTransformPoint`. Mặc định (`source = IslandV2`) sinh đảo v2 64 × 32 × 64, có vành biển.
- `client/Assets/Game/Client/Editor/TerrainViewCapture.cs` — mẫu menu sửa scene: `SetupStreamingV3` `:53-83` (`EnsureScene()`, `OpenScene`, `FindAnyObjectByType`, `SetDirty`, `SaveScene`). Hằng `ScenePath` `:15`.
- `client/Assets/Game/Client/Editor/BenchBuild.cs:23-46` — menu "Game/Tạo scene Bench" chép lại TerrainView thành Bench, tắt `DebugFlyCamera` + `TerrainEditInput`, thêm `TerrainBench`. Bench không bao giờ xuống dưới nước (bay ở y = 44).
- `client/Assets/Game/Tests/Client/TerrainEditPlayTests.cs:13-30` — mẫu PlayMode: `go.AddComponent<TerrainChunkView>()`, `yield return null` (Start → Build).
- Scene `TerrainView.unity`: sương tuyến tính 64–120, màu sương (0,78; 0,88; 0,97) `:17-22`; camera `m_ClearFlags: 1` (Skybox) `:158`, `near clip plane: 0.3` `:180`, `far clip plane: 128` `:181`, `field of view: 60` `:182`.
- `design/data/world_maps_m1.json`: `waterLevel` 12, `seaFloorDepth` 4, `lakeDepth` 7 → biển v3 nông (đáy ở y ≈ 8).

API dùng trong brief (đã tra, Unity 6000.4 / URP 17.4):
- ShaderLab đặt trạng thái theo thuộc tính material: `Blend[_SrcBlend][_DstBlend]`, `ZWrite[_ZWrite]`, `Cull[_Cull]` — URP 17.4 `Shaders/Lit.shader:106-108` trong `client/Library/PackageCache/com.unity.render-pipelines.universal@13e5115b98bf` [repo]; lệnh [Blend](https://docs.unity3d.com/6000.4/Documentation/Manual/SL-Blend.html), [ZWrite](https://docs.unity3d.com/6000.4/Documentation/Manual/SL-ZWrite.html), [Cull](https://docs.unity3d.com/6000.4/Documentation/Manual/SL-Cull.html), [Properties](https://docs.unity3d.com/6000.4/Documentation/Manual/SL-Properties.html).
- Mặt trước/sau trong fragment: `FRONT_FACE_TYPE`, `FRONT_FACE_SEMANTIC`, `IS_FRONT_VFACE(VAL, FRONT, BACK)` — `com.unity.render-pipelines.core@6f62546dd936/ShaderLibrary/API/{Metal,Vulkan,GLES3}.hlsl:11-18` [repo].
- `ComputeFogFactorZ0ToFar(float z)` (`ShaderVariablesFunctions.hlsl:315`): với sương tuyến tính trả `saturate((end − z)/(end − start))` = độ rõ (1 gần, 0 xa); khối `#if defined(FOG_LINEAR_KEYWORD_DECLARED) if (FOG_LINEAR) …` là cách URP tự kiểm (`:317-324`) [repo].
- `UnityEngine.Rendering.BlendMode` [docs](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/Rendering.BlendMode.html), `CullMode` [docs](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/Rendering.CullMode.html), `RenderQueue` [docs](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/Rendering.RenderQueue.html); `Material(Material source)` [docs](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/Material-ctor.html), `Material.SetFloat` [docs](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/Material.SetFloat.html), `Material.renderQueue` [docs](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/Material-renderQueue.html); `AssetDatabase.CreateAsset` [docs](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/AssetDatabase.CreateAsset.html), `AssetDatabase.LoadAssetAtPath` [docs](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/AssetDatabase.LoadAssetAtPath.html).
- `RenderSettings.fogColor` [docs](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/RenderSettings-fogColor.html), `fogStartDistance` / `fogEndDistance` [docs](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/RenderSettings-fogEndDistance.html); `Camera.clearFlags` [docs](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/Camera-clearFlags.html), `Camera.backgroundColor` [docs](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/Camera-backgroundColor.html), `Camera.nearClipPlane` [docs](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/Camera-nearClipPlane.html); `Transform.InverseTransformPoint` [docs](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/Transform.InverseTransformPoint.html), `Transform.InverseTransformVector` [docs](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/Transform.InverseTransformVector.html), `Transform.TransformPoint`.

## Quyết định đã chốt (không được đổi)
1. **Nước trong chỉ ở TerrainView (+ Bench, vì Bench chép từ TerrainView).** Material mới `Water.mat` (bản sao `Ocean.mat`, chế độ trong suốt) gán cho `TerrainChunkView.waterMaterial` của TerrainView. `Ocean.mat`, mặt biển nền (`OceanBackdrop`) và SimView **giữ đục như cũ** (SimView giữ v2 tới 7e). Lý do: mặt biển nền ngoài world không có đáy bên dưới.
2. **Một shader `Game/Ocean` cho cả hai chế độ**, bật trong suốt bằng thuộc tính material (`_SrcBlend`, `_DstBlend`, `_ZWrite`, `_Cull`, `_Alpha`). Giá trị mặc định của các thuộc tính mới cho kết quả **giống hệt** shader cũ (Blend One Zero, ZWrite On, Cull Back).
3. **Độ đục của nước:** gốc `_Alpha = 0,45`; nhìn xiên đục dần tới 1 theo `fres` sẵn có; xa thì đục dần tới 1 theo đúng độ rõ của sương (`ComputeFogFactorZ0ToFar`) → chỗ xa/sâu tan vào màu sương như trên cạn. **Không** đọc depth texture, không tính độ sâu riêng (user chốt).
4. **Mặt nước nhìn từ dưới lên:** `Cull Off` cho `Water.mat`; mặt sau lật pháp tuyến. Không thêm mặt bên cho nước.
5. **Va chạm camera trong Game.Core** (C# thuần, test EditMode): lớp tĩnh mới `FlyCameraCollision`. Camera là hộp nửa cạnh `radius = 0,25`; ô đặc = mọi vật liệu trừ `Empty` và `Water`, luật biên giống `BlockMesher.IsSolid`. Dời lần lượt theo trục **x → z → y**, mỗi lần tối đa `0,25` ô (không xuyên vách 1 ô khi bay nhanh); bị chặn thì áp sát mặt ô đặc, cách `Skin = 0,001`, và dừng trục đó (trục khác vẫn đi → trượt dọc vách). Đang kẹt trong ô đặc lúc bắt đầu (vd vừa đặt khối lên camera) → dời tự do khung đó để thoát.
6. **Near clip = 0,1** cho camera TerrainView. Lý do: góc mặt near clip cách camera ≈ 0,17 ô (FOV 60, màn 2,2:1) < 0,25 → không nhìn xuyên vách khi áp sát.
7. **Dưới nước** = ô chứa camera là `Water` và (ô trên không phải `Empty`, hoặc `y < iy + 1 − WaterSurfaceDrop`). Khi dưới nước: sương tuyến tính **0–40**, màu **(0,08; 0,32; 0,45)**; camera `clearFlags = SolidColor`, `backgroundColor` = màu sương nước (không lộ skybox sáng ở chân trời dưới nước). Lên khỏi nước → trả lại đúng giá trị cũ của scene. Chỉ đổi khi trạng thái đổi.
8. Va chạm chỉ bật khi `DebugFlyCamera.terrain` được gán (TerrainView). `terrain == null` → bay xuyên như cũ (SimView không đổi). Bench tắt `DebugFlyCamera` như cũ.
9. Không đổi: bộ sinh world, mã băm golden, `BlockMesher`, `TerrainChunkView`, `TerrainEditInput`, `TerrainBench`, `OceanBackdrop`, `SkyClouds.shader`, `Terrain.shader`, scene SimView/HashCheck, file JSON trong `design/data/`.
10. **Sửa sau review 1 (user 2026-10-20: "từ nước nhìn lên tối quá"):** mặt dưới của mặt nước chiếu sáng theo pháp tuyến hướng lên (như mặt trên); sương dưới nước sáng hơn (0,14; 0,44; 0,56); nền khi dưới nước tách khỏi màu sương, sáng hơn (0,42; 0,70; 0,82) → nhìn lên qua mặt nước thấy sáng. Tầm 0–40 và độ trong 0,45 giữ nguyên (user thấy ổn).

## Phạm vi
- Được sửa/tạo:
  - `client/Assets/Game/Core/FlyCameraCollision.cs` (mới), `client/Assets/Game/Tests/Core/FlyCameraCollisionTests.cs` (mới)
  - `client/Assets/Game/Client/Shaders/Ocean.shader`
  - `client/Assets/Game/Client/DebugFlyCamera.cs`, `client/Assets/Game/Client/UnderwaterView.cs` (mới)
  - `client/Assets/Game/Client/Editor/TerrainViewCapture.cs` (thêm 1 menu)
  - `client/Assets/Game/Client/Materials/Water.mat` (menu tạo)
  - `client/Assets/Game/Tests/Client/FlyCameraWaterPlayTests.cs` (mới)
  - Scene `TerrainView.unity` và `Bench.unity` (chỉ qua menu ở bước 6)
  - File `.meta` Unity tự tạo cho file mới
  - `design/04-tech/world-tech.md` (chỉ dòng **Hiển thị** của mục nước, bước 8)
  - File brief này (mục Kết quả trở xuống)
- `Ocean.mat`: chỉ chấp nhận nếu Unity tự ghi thêm các thuộc tính mới với giá trị mặc định (`_Alpha: 1`, `_SrcBlend: 1`, `_DstBlend: 0`, `_ZWrite: 1`, `_Cull: 2`); khác giá trị đó → dừng, `cần Opus`.
- Không được sửa: mọi file khác. `client/ProjectSettings/*` trả về sau khi chạy (xem Lệnh executor chạy).

## Các bước
1. **`FlyCameraCollision` (Game.Core)** — tạo `client/Assets/Game/Core/FlyCameraCollision.cs`, chép nguyên văn:
   ```csharp
   using System;

   namespace Game.Core
   {
       /// <summary>
       /// Camera bay trên lưới ô world (world-tech bước 7b2, brief 0011): không vào ô đặc, vào được ô nước và ô trống.
       /// Camera là hộp nửa cạnh radius; dời lần lượt theo x, z, y → bị chặn trục nào thì trượt theo trục còn lại.
       /// Ngoài world theo x/z lấy cột biên gần nhất, y &lt; 0 là đặc, y ≥ Size.Y là trống (giống BlockMesher.IsSolid).
       /// </summary>
       public static class FlyCameraCollision
       {
           /// <summary>Khoảng hở còn lại giữa hộp camera và mặt ô đặc khi bị chặn.</summary>
           public const float Skin = 0.001f;
           /// <summary>Bước dời tối đa mỗi lần kiểm — nhỏ hơn 1 ô để bay nhanh không xuyên vách dày 1 ô.</summary>
           public const float MaxStep = 0.25f;

           public static bool IsSolid(WorldData world, int x, int y, int z)
           {
               var s = world.Size;
               if (y < 0)
                   return true;
               if (y >= s.Y)
                   return false;
               x = Math.Min(Math.Max(x, 0), s.X - 1);
               z = Math.Min(Math.Max(z, 0), s.Z - 1);
               var m = world.GetMaterial(x, y, z);
               return m != BlockMaterial.Empty && m != BlockMaterial.Water;
           }

           /// <summary>Hộp nửa cạnh radius quanh (x, y, z) chạm ô đặc nào không.</summary>
           public static bool Overlaps(WorldData world, float x, float y, float z, float radius)
           {
               int x0 = Floor(x - radius), x1 = Floor(x + radius);
               int y0 = Floor(y - radius), y1 = Floor(y + radius);
               int z0 = Floor(z - radius), z1 = Floor(z + radius);
               for (int iy = y0; iy <= y1; iy++)
               for (int iz = z0; iz <= z1; iz++)
               for (int ix = x0; ix <= x1; ix++)
                   if (IsSolid(world, ix, iy, iz))
                       return true;
               return false;
           }

           /// <summary>Dời camera một đoạn (dx, dy, dz); đang kẹt trong ô đặc thì dời tự do để thoát ra.</summary>
           public static void Move(WorldData world, ref float x, ref float y, ref float z, float dx, float dy, float dz, float radius)
           {
               if (Overlaps(world, x, y, z, radius))
               {
                   x += dx;
                   y += dy;
                   z += dz;
                   return;
               }
               MoveAxis(world, ref x, ref y, ref z, 0, dx, radius);
               MoveAxis(world, ref x, ref y, ref z, 2, dz, radius);
               MoveAxis(world, ref x, ref y, ref z, 1, dy, radius);
           }

           static void MoveAxis(WorldData world, ref float x, ref float y, ref float z, int axis, float d, float radius)
           {
               if (d == 0f)
                   return;
               int n = Math.Max(1, (int)Math.Ceiling(Math.Abs(d) / MaxStep));
               float step = d / n;
               for (int i = 0; i < n; i++)
               {
                   float cx = x, cy = y, cz = z;
                   if (axis == 0) cx += step; else if (axis == 1) cy += step; else cz += step;
                   if (!Overlaps(world, cx, cy, cz, radius))
                   {
                       x = cx; y = cy; z = cz;
                       continue;
                   }
                   // Bị chặn: áp sát mặt ô đặc (cách Skin) rồi dừng trục này.
                   float c = axis == 0 ? cx : axis == 1 ? cy : cz;
                   float p = axis == 0 ? x : axis == 1 ? y : z;
                   float clamped = step > 0f
                       ? Math.Max(p, Floor(c + radius) - radius - Skin)
                       : Math.Min(p, Floor(c - radius) + 1 + radius + Skin);
                   if (axis == 0) x = clamped; else if (axis == 1) y = clamped; else z = clamped;
                   return;
               }
           }

           /// <summary>
           /// Camera ở dưới nước: ô chứa camera là nước, và ô trên không phải trống hoặc camera thấp hơn mặt nước
           /// (mặt nước của ô có không khí phía trên nằm ở y + 1 − surfaceDrop, xem BlockMesher.WaterSurfaceDrop).
           /// </summary>
           public static bool IsUnderwater(WorldData world, float x, float y, float z, float surfaceDrop)
           {
               var s = world.Size;
               int ix = Floor(x), iy = Floor(y), iz = Floor(z);
               if (iy < 0 || iy >= s.Y)
                   return false;
               ix = Math.Min(Math.Max(ix, 0), s.X - 1);
               iz = Math.Min(Math.Max(iz, 0), s.Z - 1);
               if (world.GetMaterial(ix, iy, iz) != BlockMaterial.Water)
                   return false;
               bool airAbove = iy + 1 >= s.Y || world.GetMaterial(ix, iy + 1, iz) == BlockMaterial.Empty;
               return !airAbove || y < iy + 1 - surfaceDrop;
           }

           static int Floor(float v) => (int)Math.Floor(v);
       }
   }
   ```
   Xong khi: biên dịch được, không có `using UnityEngine`.
2. **`FlyCameraCollisionTests` (EditMode)** — `client/Assets/Game/Tests/Core/FlyCameraCollisionTests.cs`, namespace `Game.Core.Tests`, lớp `FlyCameraCollisionTests`. Hàm dựng world dùng chung: `WorldData MakeWorld(int floorTop, int waterTop)` → world `new WorldSize(16, 16, 16)`; mọi (x, z): y = 0…floorTop đặt `Cell.Make(BlockMaterial.Stone)`, y = floorTop + 1…waterTop đặt `Cell.Make(BlockMaterial.Water)` (waterTop ≤ floorTop → không có nước). Hằng `const float R = 0.25f, Eps = 1e-4f;`. Các test (so số thực bằng `Assert.AreEqual(expected, actual, Eps)`):
   - `IsSolidFollowsMesherRules`: `MakeWorld(3, 7)`. `IsSolid(8, 2, 8)` true; `IsSolid(8, 5, 8)` false (nước); `IsSolid(8, 10, 8)` false; `IsSolid(0, -1, 0)` true; `IsSolid(0, 16, 0)` false; `IsSolid(-5, 2, 3)` true và `IsSolid(20, 2, 3)` true (ngoài world lấy cột biên); `IsSolid(-5, 5, 3)` false.
   - `MoveStopsOnFloor`: `MakeWorld(3, 3)`; camera (8,5; 6,5; 8,5); `Move(…, 0, -10, 0, R)` → y = 4 + R + Skin; x, z không đổi.
   - `MoveEntersWater`: `MakeWorld(3, 7)`; camera (8,5; 10,5; 8,5); `Move(…, 0, -5, 0, R)` → y = 5,5 (không bị chặn).
   - `MoveSlidesAlongWall`: world 16³ rỗng, đặt Stone mọi ô có x = 10; camera (9; 8,5; 5,5); `Move(…, 2, 0, 3, R)` → x = 10 − R − Skin, z = 8,5, y = 8,5.
   - `FastMoveDoesNotTunnelThinWall`: world như test trên; camera (8,5; 8,5; 8,5); `Move(…, 30, 0, 0, R)` → x = 10 − R − Skin.
   - `StuckInsideSolidMovesFreely`: `MakeWorld(3, 3)`; camera (8,5; 2,5; 8,5); `Move(…, 0, 3, 0, R)` → y = 5,5.
   - `IsUnderwaterUsesSurfaceDrop`: `MakeWorld(3, 7)`, drop = 0,1. (8,5; 7,5; 8,5) → true; (8,5; 7,95; 8,5) → false; (8,5; 5,5; 8,5) → true; (8,5; 8,5; 8,5) → false; (8,5; 2,5; 8,5) → false (đá). Rồi đặt Stone tại (8, 8, 8) → (8,5; 7,95; 8,5) → true.
   Xong khi: 7 test đạt.
3. **`Ocean.shader` — chế độ trong suốt bằng thuộc tính** (không đổi gì khác):
   - `Properties`, thêm sau `_Glint` (`:12`):
     ```
     _Alpha("Alpha (chỉ material trong suốt)", Range(0, 1)) = 1
     [HideInInspector] _SrcBlend("Src blend", Float) = 1
     [HideInInspector] _DstBlend("Dst blend", Float) = 0
     [HideInInspector] _ZWrite("ZWrite", Float) = 1
     [HideInInspector] _Cull("Cull", Float) = 2
     ```
   - `CBUFFER` `:24`: đổi `half _RippleStrength, _Glint;` thành `half _RippleStrength, _Glint, _Alpha;`.
   - Pass `ForwardLit`, ngay sau dòng `Tags { "LightMode" = "UniversalForward" }` (`:31`), thêm 3 dòng:
     ```
     Blend [_SrcBlend] [_DstBlend]
     ZWrite [_ZWrite]
     Cull [_Cull]
     ```
   - Sửa chú thích đầu file (dòng 1–3), thêm dòng 4: `// Brief 0011: material Water.mat bật trong suốt (Blend SrcAlpha/OneMinusSrcAlpha, ZWrite Off, Cull Off) — mặc định vẫn đục như cũ.`
   - `frag` `:91`: đổi chữ ký thành `half4 frag(Varyings input, FRONT_FACE_TYPE face : FRONT_FACE_SEMANTIC) : SV_Target`.
   - Ngay sau dòng tính `n` (`:97`) thêm:
     ```
     n = IS_FRONT_VFACE(face, n, -n); // mặt dưới (camera dưới nước, Cull Off): pháp tuyến quay xuống
     ```
   - Thay 3 dòng `:109-111` (chú thích sương, `c = MixFog(...)`, `return half4(c, 1);`) bằng:
     ```
     // Sương theo khoảng cách thật (brief 0010 QĐ 11): far clip phẳng + màn hình rộng → mép nhìn xa hơn bán kính nạp.
     half fogF = ComputeFogFactorZ0ToFar(distance(input.positionWS, GetCameraPositionWS()));
     c = MixFog(c, fogF);
     // Nước trong (brief 0011): nhìn xiên đục hơn (fresnel); xa đục dần theo sương → chỗ xa/sâu tan vào màu sương như trên cạn.
     half fogVis = 1.0; // độ rõ: 1 gần, 0 xa — chỉ tính với sương tuyến tính (scene dùng sương tuyến tính)
     #if defined(FOG_LINEAR_KEYWORD_DECLARED)
     if (FOG_LINEAR)
         fogVis = fogF;
     #endif
     half alpha = lerp(1.0, lerp(_Alpha, 1.0, fres), fogVis);
     return half4(c, alpha);
     ```
   Xong khi: shader biên dịch không lỗi (`read_console` không có lỗi shader), `OceanBackdropTests` vẫn đạt.
4. **`DebugFlyCamera` — va chạm:**
   - Thêm `using Game.Core;`.
   - Trường mới (sau `lookSensitivity`):
     ```csharp
     /// <summary>World để chặn camera vào ô đặc (brief 0011); null → bay xuyên như cũ (SimView).</summary>
     public TerrainChunkView terrain;
     /// <summary>Nửa cạnh hộp va chạm của camera (ô). Phải lớn hơn khoảng cách từ camera tới góc mặt near clip (near 0,1 → ~0,17).</summary>
     public float collisionRadius = 0.25f;
     ```
   - Hàm mới (public, đặt sau `IsMoveKeyHeld`):
     ```csharp
     /// <summary>Dời camera theo delta (toạ độ thế giới); có terrain → không vào ô đặc, trượt dọc vách (brief 0011).</summary>
     public void Fly(Vector3 delta)
     {
         if (terrain == null || terrain.World == null)
         {
             transform.position += delta;
             return;
         }
         var t = terrain.transform;
         var p = t.InverseTransformPoint(transform.position);
         var d = t.InverseTransformVector(delta);
         float x = p.x, y = p.y, z = p.z;
         FlyCameraCollision.Move(terrain.World, ref x, ref y, ref z, d.x, d.y, d.z, collisionRadius);
         transform.position = t.TransformPoint(new Vector3(x, y, z));
     }
     ```
   - `:50`: thay `transform.position += move * (v * Time.deltaTime);` bằng `Fly(move * (v * Time.deltaTime));`.
   - Chú thích lớp (`:6-9`): thêm dòng `/// Bước 7b2 (brief 0011): gán terrain → không vào ô đặc, vào được ô nước, trượt dọc vách.` trước `/// </summary>`.
   Xong khi: biên dịch được; `FlyCameraMoveKeyPlayTests` vẫn đạt.
5. **`UnderwaterView`** — tạo `client/Assets/Game/Client/UnderwaterView.cs`, chép nguyên văn:
   ```csharp
   using Game.Core;
   using Game.World;
   using UnityEngine;

   namespace Game.Client
   {
       /// <summary>
       /// Camera dưới nước (world-tech bước 7b2, brief 0011): sương ngắn màu nước + nền màu nước (không lộ skybox ở chân trời);
       /// lên khỏi nước thì trả lại sương và nền cũ của scene.
       /// </summary>
       [RequireComponent(typeof(Camera))]
       public class UnderwaterView : MonoBehaviour
       {
           public TerrainChunkView terrain;
           public Color fogColor = new Color(0.08f, 0.32f, 0.45f);
           public float fogStart = 0f;
           public float fogEnd = 40f;

           public bool IsUnderwater { get; private set; }

           Camera cam;
           Color savedFogColor, savedBackground;
           float savedFogStart, savedFogEnd;
           CameraClearFlags savedClearFlags;

           void Awake()
           {
               cam = GetComponent<Camera>();
           }

           void LateUpdate()
           {
               bool under = false;
               if (terrain != null && terrain.World != null)
               {
                   var p = terrain.transform.InverseTransformPoint(transform.position);
                   under = FlyCameraCollision.IsUnderwater(terrain.World, p.x, p.y, p.z, BlockMesher.WaterSurfaceDrop);
               }
               if (under == IsUnderwater)
                   return;
               if (under)
                   Enter();
               else
                   Restore();
           }

           void OnDisable()
           {
               if (IsUnderwater)
                   Restore();
           }

           void Enter()
           {
               savedFogColor = RenderSettings.fogColor;
               savedFogStart = RenderSettings.fogStartDistance;
               savedFogEnd = RenderSettings.fogEndDistance;
               savedClearFlags = cam.clearFlags;
               savedBackground = cam.backgroundColor;
               RenderSettings.fogColor = fogColor;
               RenderSettings.fogStartDistance = fogStart;
               RenderSettings.fogEndDistance = fogEnd;
               cam.clearFlags = CameraClearFlags.SolidColor;
               cam.backgroundColor = fogColor;
               IsUnderwater = true;
           }

           void Restore()
           {
               RenderSettings.fogColor = savedFogColor;
               RenderSettings.fogStartDistance = savedFogStart;
               RenderSettings.fogEndDistance = savedFogEnd;
               cam.clearFlags = savedClearFlags;
               cam.backgroundColor = savedBackground;
               IsUnderwater = false;
           }
       }
   }
   ```
   Xong khi: biên dịch được.
6. **Menu sửa scene TerrainView** — `TerrainViewCapture.cs`: thêm `using UnityEngine.Rendering;`, 2 hằng `const string OceanMatPath = "Assets/Game/Client/Materials/Ocean.mat";` và `const string WaterMatPath = "Assets/Game/Client/Materials/Water.mat";` (cạnh `ScenePath`), và menu đặt sau `SetupStreamingV3`:
   ```csharp
   /// <summary>Bước 7b2 (brief 0011): mặt nước theo ô dùng Water.mat (trong suốt), camera bay va chạm + hiệu ứng dưới nước, near clip 0,1.</summary>
   [MenuItem("Game/Nước trong + camera va chạm cho TerrainView")]
   public static void SetupWaterAndCamera()
   {
       EnsureScene();
       var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
       var water = AssetDatabase.LoadAssetAtPath<Material>(WaterMatPath);
       if (water == null)
       {
           water = new Material(AssetDatabase.LoadAssetAtPath<Material>(OceanMatPath)) { name = "Water" };
           AssetDatabase.CreateAsset(water, WaterMatPath);
       }
       water.SetFloat("_Alpha", 0.45f);
       water.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
       water.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
       water.SetFloat("_ZWrite", 0f);
       water.SetFloat("_Cull", (float)CullMode.Off);
       water.renderQueue = (int)RenderQueue.Transparent;
       EditorUtility.SetDirty(water);
       AssetDatabase.SaveAssets();

       var view = Object.FindAnyObjectByType<TerrainChunkView>();
       view.waterMaterial = water;
       EditorUtility.SetDirty(view);
       var cam = Camera.main;
       cam.nearClipPlane = 0.1f;
       EditorUtility.SetDirty(cam);
       var fly = cam.GetComponent<DebugFlyCamera>();
       fly.terrain = view;
       fly.collisionRadius = 0.25f;
       EditorUtility.SetDirty(fly);
       var under = cam.GetComponent<UnderwaterView>();
       if (under == null)
           under = cam.gameObject.AddComponent<UnderwaterView>();
       under.terrain = view;
       EditorUtility.SetDirty(under);
       EditorSceneManager.MarkSceneDirty(scene);
       EditorSceneManager.SaveScene(scene);
       Debug.Log("TERRAINVIEW đã bật nước trong + camera va chạm + hiệu ứng dưới nước");
   }
   ```
   (Không dùng `??` với object Unity.) Chạy menu "Game/Nước trong + camera va chạm cho TerrainView", rồi "Game/Tạo scene Bench" (Bench chép lại từ TerrainView).
   Xong khi: `grep -n "near clip plane\|waterMaterial\|terrain: {fileID\|collisionRadius" client/Assets/Game/Client/Scenes/TerrainView.unity` ra `0.1`, `waterMaterial` trỏ guid của `Water.mat` (lấy từ `Water.mat.meta`, khác `3c4f12bab1e8847abb25f093937a9af8`), 2 dòng `terrain:` khác `fileID: 0` (DebugFlyCamera + UnderwaterView), `collisionRadius: 0.25`. Bench có cùng giá trị. `Water.mat` có `m_CustomRenderQueue: 3000`, `_Alpha: 0.45`, `_SrcBlend: 5`, `_DstBlend: 10`, `_ZWrite: 0`, `_Cull: 0`. SimView vẫn trỏ `Ocean.mat`.
7. **PlayMode test `FlyCameraWaterPlayTests`** — `client/Assets/Game/Tests/Client/FlyCameraWaterPlayTests.cs`, namespace `Game.Client.Tests`. `[UnitySetUp]`: tạo `go` "Terrain" + `TerrainChunkView` mặc định (đảo v2), `yield return null`, `Assert.IsNotNull(view.World)`. Tạo `camGo = new GameObject("Cam")`, `cam = camGo.AddComponent<Camera>()`, `fly = camGo.AddComponent<DebugFlyCamera>()` (`fly.terrain = view`), `under = camGo.AddComponent<UnderwaterView>()` (`under.terrain = view`). `[UnityTearDown]`: `Object.Destroy(camGo); Object.Destroy(go); yield return null;`. `S = view.Editor.SeaLevel`. Test:
   - `FlyStopsAboveGround`: tìm (x, z) đầu tiên (duyệt z rồi x từ 0) có `h = view.World.SurfaceHeight(x, z) > S` và `h + 4 < view.World.Size.Y`; không có → `Assert.Fail`. Đặt `camGo.transform.position = (x + 0,5; h + 3,5; z + 0,5)`; `fly.Fly(new Vector3(0, -10, 0))` → `Assert.AreEqual(h + 1 + 0.25f + FlyCameraCollision.Skin, camGo.transform.position.y, 1e-3f)`.
   - `FlyIntoWaterSwitchesUnderwaterView`: tìm (x, z) đầu tiên có `GetMaterial(x, S, z) == Water`, các ô y = S + 1…S + 4 của cột đều `Empty` và `SurfaceHeight(x, z) <= S - 2`; không có → `Assert.Fail`. Ghi `fogColor0 = RenderSettings.fogColor`, `start0`, `end0`, `clear0 = cam.clearFlags`, `bg0 = cam.backgroundColor`. Đặt camera ở (x + 0,5; S + 3,5; z + 0,5); `yield return null` → `Assert.IsFalse(under.IsUnderwater)`. `fly.Fly(new Vector3(0, -3.2f, 0))` → y = S + 0,3 (sai số 1e-3, không bị chặn); `yield return null` → `IsUnderwater` true, `RenderSettings.fogEndDistance` = 40 (sai số 1e-4), `RenderSettings.fogColor == under.fogColor`, `cam.clearFlags == CameraClearFlags.SolidColor`, `cam.backgroundColor == under.fogColor`. `fly.Fly(new Vector3(0, 3.2f, 0))`; `yield return null` → `IsUnderwater` false và 5 giá trị bằng lúc đầu.
   Xong khi: 2 test đạt.
8. **Tài liệu** — `design/04-tech/world-tech.md:117`, dòng `- **Hiển thị:** mesh mặt nước riêng, cao theo mực nước, tách khỏi mesh địa hình; shader trong suốt.` → nối thêm vào cuối dòng (cùng dòng) nguyên văn:
   ` M1 (brief 0011, bước 7b2): mặt nước theo ô dùng \`Water.mat\` — shader \`Game/Ocean\` chế độ trong suốt, độ đục gốc 0,45, nhìn xiên và ở xa đục dần theo sương (không tính độ sâu); nhìn được từ dưới lên. Camera dưới nước: sương 0–40 màu nước. Camera bay không vào ô đặc, vào được ô nước (hộp 0,25 ô, trượt dọc vách). Mặt biển nền ngoài world vẫn đục.`
   Không sửa dòng 7b2 trong bảng kế hoạch (Planner ghi khi review). Xong khi: `python3 tools/check_design.py` exit 0.
9. **Kiểm hình ảnh qua MCP** — Game view Free Aspect. Mở TerrainView, Play; đưa Unity lên foreground: `osascript -e 'tell application "System Events" to set frontmost of (first process whose unix id is <pid>) to true'` với `<pid>` = `pgrep -x Unity`. Dùng `execute_code`:
   ```csharp
   var view = Object.FindAnyObjectByType<Game.Client.TerrainChunkView>();
   int S = view.Editor.SeaLevel, x = 192, zs = -1;
   for (int z = 0; z < view.World.Size.Z; z++) if (view.World.SurfaceHeight(x, z) > S) { zs = z; break; }
   int zu = -1;
   for (int z = zs - 1; z >= 0; z--) if (view.World.SurfaceHeight(x, z) <= S - 3) { zu = z; break; }
   return $"S={S} zs={zs} zu={zu}";
   ```
   - Ảnh 1 `client/Logs/water_above.png`: camera (x + 0,5; S + 5; zs − 10), `Quaternion.Euler(35, 0, 0)`. Chờ ≥ 60 khung, chụp (`manage_camera` hoặc `ScreenCapture.CaptureScreenshot`). Cần: thấy đáy cát/đá dưới mặt nước gần bờ, xa dần tan vào màu sương.
   - Ảnh 2 `client/Logs/water_below.png`: camera (x + 0,5; S + 0,3; zu + 0,5), `Quaternion.Euler(-15, 0, 0)`. Chờ ≥ 60 khung, `UnderwaterView.IsUnderwater` phải true, chụp. Cần: nền màu nước, thấy bờ (dốc lên mặt nước) phía trước.
   - Đặt camera (x + 0,5; S + 3; zs + 0,5) (trên bờ), gọi `Camera.main.GetComponent<Game.Client.DebugFlyCamera>().Fly(new Vector3(0, -10, 0))`, ghi `y` và `SurfaceHeight(x, zs)`: phải `y ≈ SurfaceHeight + 1,251`.
   `zs` hoặc `zu` = −1 → thử x = 100, rồi 300; vẫn không có → ghi rõ, không đoán. Thoát Play. Ghi đường dẫn ảnh và số liệu vào Kết quả.
10. **Bench trên điện thoại** (nước trong suốt thêm overdraw): menu "Game/Build APK Bench" → `client/Builds/bench.apk`. Cài qua USB: `ADB=/Applications/Unity/Hub/Editor/6000.4.8f1/PlaybackEngines/AndroidPlayer/SDK/platform-tools/adb`; `$ADB -s PFQCRWVWR46HU4GQ install -r client/Builds/bench.apk`; `$ADB -s PFQCRWVWR46HU4GQ logcat -c`; `$ADB -s PFQCRWVWR46HU4GQ shell monkey -p com.viventa.bench -c android.intent.category.LAUNCHER 1`; chờ 60 s (chạy nền hoặc Monitor, không `sleep` tiền cảnh); `$ADB -s PFQCRWVWR46HU4GQ logcat -d -s Unity | grep -E "BENCH|TERRAINVIEW"`. Cần `pass30fps=True passNoHole=True`; ghi nguyên dòng `BENCH` (so với 0010: 45,6 FPS). Không thấy máy trong `$ADB devices` → ghi "chờ Planner", không đoán số.
11. Commit theo skill exec (code + test + shader + material + scene + meta + world-tech + brief), trạng thái `chờ review`.

### Sửa sau review 1 (Planner 2026-10-20, QĐ 10)
12. **`Ocean.shader` — mặt dưới chiếu sáng như mặt trên.** Trong `frag`, thay dòng `n = IS_FRONT_VFACE(face, n, -n); // …` bằng 2 dòng:
    ```
    float3 nLit = n; // chiếu sáng luôn theo pháp tuyến hướng lên: mặt dưới sáng như mặt trên (brief 0011 sửa 1 — từ dưới nhìn lên không tối)
    n = IS_FRONT_VFACE(face, n, -n); // mặt dưới (camera dưới nước, Cull Off): pháp tuyến quay xuống — chỉ dùng cho fresnel và lấp lánh
    ```
    Rồi ở dòng `half ndl = …` đổi `dot(n, light.direction)` thành `dot(nLit, light.direction)`; ở dòng `half3 diffuse = …` đổi `SampleSH(n)` thành `SampleSH(nLit)`. Dòng `spec` và `fres` giữ `n`. Mặt trên không đổi (`nLit == n`).
13. **`UnderwaterView.cs`:** đổi mặc định `fogColor` thành `new Color(0.14f, 0.44f, 0.56f)`. Thêm trường ngay sau `fogColor`:
    ```csharp
    /// <summary>Nền khi dưới nước — sáng hơn sương: nhìn lên qua mặt nước thấy sáng như ánh trời (brief 0011 sửa 1).</summary>
    public Color backgroundColor = new Color(0.42f, 0.70f, 0.82f);
    ```
    Trong `Enter()` đổi `cam.backgroundColor = fogColor;` thành `cam.backgroundColor = backgroundColor;`. Chú thích lớp: thay `nền màu nước` bằng `nền sáng màu nước`.
14. **Menu `SetupWaterAndCamera`** (`TerrainViewCapture.cs`): sau `under.terrain = view;` thêm `under.fogColor = new Color(0.14f, 0.44f, 0.56f); under.backgroundColor = new Color(0.42f, 0.70f, 0.82f); under.fogStart = 0f; under.fogEnd = 40f;` (scene đã lưu giá trị cũ, đổi mặc định trong code không đủ). Chạy lại menu "Game/Nước trong + camera va chạm cho TerrainView", rồi "Game/Tạo scene Bench"; sau đó nếu `Bench.unity.meta` đổi guid thì `git checkout -- client/Assets/Game/Client/Scenes/Bench.unity.meta`. Xong khi: `grep -n "fogColor\|backgroundColor" client/Assets/Game/Client/Scenes/TerrainView.unity` có (0.14, 0.44, 0.56) và (0.42, 0.7, 0.82) ở component UnderwaterView; Bench giống.
15. **Test `FlyIntoWaterSwitchesUnderwaterView`:** đổi kiểm `cam.backgroundColor == under.fogColor` thành `cam.backgroundColor == under.backgroundColor`. Không đổi gì khác.
16. **Ảnh lại qua MCP** (cách như bước 9, Editor ra khỏi Play trước khi chạy menu): chụp lại `client/Logs/water_below.png` (cùng vị trí, `Euler(-15, 0, 0)`) và thêm `client/Logs/water_below_up.png` (cùng vị trí, `Euler(-60, 0, 0)` — nhìn gần thẳng lên mặt nước). Cần: vùng mặt nước phía trên sáng hơn ảnh cũ (so với ảnh `water_below.png` của lần làm đầu — giữ bản cũ thành `water_below_old.png` trước khi chụp).
17. Chạy lại: EditMode toàn bộ → 260/260; PlayMode toàn bộ → 15/15, rồi `git checkout -- client/ProjectSettings/EditorSettings.asset`. Không cần build APK (bench bay trên cao, không bao giờ dưới nước; mặt trên không đổi). Ghi vào mục **Kết quả — sửa lần 1** (mới, dưới "Kết quả test"; không xoá kết quả cũ): `Chạy bằng`, giờ bắt đầu/kết thúc, commit, kết quả bước 12–17. Commit theo skill exec, trạng thái `chờ review`.

## Tiêu chí xong
- EditMode toàn bộ đạt: 253 cũ + 7 `FlyCameraCollisionTests` = **260**.
- PlayMode toàn bộ đạt: 13 cũ + 2 `FlyCameraWaterPlayTests` = **15**.
- Mã băm golden không đổi (test golden đạt).
- Scene TerrainView/Bench và `Water.mat` có giá trị như bước 6; SimView vẫn dùng `Ocean.mat`.
- 2 ảnh ở bước 9 có, số liệu `Fly` trên bờ đúng.
- Dòng `BENCH` từ điện thoại có `pass30fps=True passNoHole=True`, hoặc ghi "chờ Planner".
- `python3 tools/check_design.py` exit 0; `git status` chỉ còn file trong Phạm vi.

## Test sau khi xong
### Phần cần test
- **`FlyCameraCollision`** (bước 1) · `FlyCameraCollisionTests` (7 test: luật ô đặc + biên, dừng trên sàn, vào nước, trượt dọc vách, bay nhanh không xuyên vách 1 ô, kẹt thì thoát, dưới nước theo mặt nước) · đạt.
- **Shader `Game/Ocean` mặc định vẫn đục** (bước 3, QĐ 2) · `OceanBackdropTests` (PlayMode, hồi quy) + `read_console` không lỗi shader · đạt; SimView mở trong Editor vẫn trỏ `Ocean.mat` (grep guid `3c4f12bab1e8847abb25f093937a9af8` trong `SimView.unity` còn 2 chỗ).
- **Nước trong + nhìn từ dưới** (bước 3, 6) · ảnh `water_above.png`, `water_below.png` ở bước 9 · thấy đáy gần bờ, xa tan vào sương; dưới nước nền màu nước, thấy bờ.
- **Va chạm camera trong Unity** (bước 4) · `FlyStopsAboveGround` + số liệu `Fly` trên bờ ở bước 9 · đạt, `y ≈ SurfaceHeight + 1,251`.
- **Hiệu ứng dưới nước bật/tắt + trả lại sương cũ** (bước 5) · `FlyIntoWaterSwitchesUnderwaterView` · đạt.
- **Camera bay không gán terrain vẫn như cũ** (QĐ 8) · `FlyCameraMoveKeyPlayTests`, `SimViewPlayTests` · đạt.
- **Hồi quy world/nạp dần** · `TerrainStreamingPlayTests`, `TerrainEditPlayTests`, toàn bộ `Game.World.Tests` (golden, seam) · đạt như trước.
- **Scene** (bước 6) · lệnh grep ở bước 6 trên TerrainView + Bench, đọc `Water.mat` · đúng giá trị.
- **Hiệu năng điện thoại** (tiêu chí 7b2) · bước 10 · `pass30fps=True passNoHole=True`.
- **Tài liệu** (bước 8) · `check_design.py` · exit 0.

### Lệnh executor chạy
1. `python3 "<Plugin root>/scripts/brief_check.py" 0011 --model <model id>` (theo skill exec).
2. MCP `run_tests` EditMode lọc `Game.Core.Tests.FlyCameraCollisionTests` → 7/7.
3. MCP `execute_menu_item` "Game/Nước trong + camera va chạm cho TerrainView", rồi "Game/Tạo scene Bench"; `read_console` không lỗi; chạy lệnh grep ở bước 6 trên cả 2 scene; `grep -n "m_CustomRenderQueue\|_Alpha\|_SrcBlend\|_DstBlend\|_ZWrite\|_Cull" client/Assets/Game/Client/Materials/Water.mat`; `grep -c "3c4f12bab1e8847abb25f093937a9af8" client/Assets/Game/Client/Scenes/SimView.unity` → 2.
4. MCP `run_tests` EditMode toàn bộ → 260/260.
5. MCP `run_tests` PlayMode toàn bộ → 15/15. Sau đó `git checkout -- client/ProjectSettings/EditorSettings.asset`.
6. Bước 9 (kiểm hình ảnh qua MCP).
7. Bước 10 (build APK, điện thoại). Sau build: `git checkout -- client/ProjectSettings/UnityConnectSettings.asset`; xoá `client/Assets/Resources/PerformanceTest*.json` nếu có.
8. `python3 tools/check_design.py` → exit 0.
9. `git status --short` → chỉ file trong Phạm vi; `git diff client/Assets/Game/Client/Materials/Ocean.mat` → rỗng hoặc chỉ thêm thuộc tính mặc định (xem Phạm vi).

### User tự test
Mở scene TerrainView, bấm Play, bấm vào Game view (để nhận phím), bay bằng W/A/S/D/Q/E (giữ Shift bay nhanh, giữ chuột giữa để xoay):
1. Bay thấp trên biển gần bờ, nhìn xuống: thấy đáy cát/đá qua mặt nước; ra xa bờ, đáy mờ dần vào màu sương. Độ trong (0,45) có vừa không?
2. Bấm Q lặn xuống nước: màn hình chuyển sang sương xanh ngắn; nhìn lên/về phía bờ vẫn thấy bờ và mặt nước từ bên dưới. Màu và tầm nhìn (0–40 ô) có ổn không? Lên khỏi nước → trời và sương trở lại như cũ.
3. Bay thẳng vào sườn đồi / xuống mặt đất: camera dừng sát mặt, không nhìn xuyên vào trong khối; giữ W chéo vào vách thì trượt dọc vách. Dưới nước, bay xuống đáy cũng bị chặn.
4. Bay xuyên qua một hồ trong map: vào được nước, không kẹt ở mặt nước.

## Dừng và chuyển lại Opus khi
- Brief mơ hồ hoặc mâu thuẫn với code thật.
- Cần một quyết định thuộc AGENTS.md §3.4 hoặc ngoài "Quyết định đã chốt" (vd đổi số 0,45 / 0–40 / màu vì thấy chưa đẹp → ghi vào Kết quả cho user xem, không tự đổi).
- Cùng một lỗi sửa 2 lần vẫn chưa qua.
- Phải sửa file ngoài "Phạm vi".
- Shader lỗi trên điện thoại (nước đen/hồng, mất mặt nước) hoặc `pass30fps=False`.

---
<!-- Từ đây trở xuống: EXECUTOR ghi. -->

## Kết quả
- Base commit: 011d8a1
- Chạy bằng: claude-sonnet-5-5 · medium
- Bắt đầu: 2026-10-20 17:59 · kết thúc: 2026-10-20 18:32
- Commit code: xem `git log` (message `brief 0011: world 7b2 — nước trong + camera va chạm`); brief: `brief 0011: kết quả`
- File đổi: FlyCameraCollision.cs (+test), Ocean.shader, DebugFlyCamera.cs, UnderwaterView.cs, TerrainViewCapture.cs, Water.mat, FlyCameraWaterPlayTests.cs, TerrainView.unity, Bench.unity, world-tech.md (+ .meta)
- **Lệch so với brief (Planner xem):** (1) `FlyCameraCollision.MoveAxis` — công thức kẹp ở chiều âm trong brief (`Math.Min(p, …)`) làm `MoveStopsOnFloor` fail (y = 4,25 thay vì 4,251: camera dừng đúng chạm mặt ô, mất Skin). Đã đổi thành gán thẳng `limit` cho cả hai chiều (bỏ `Math.Max/Min(p, …)`); 7/7 đạt. (2) Menu "Tạo scene Bench" tạo lại Bench.unity → guid trong `Bench.unity.meta` đổi; đã `git checkout` trả meta về guid cũ (không có chỗ nào khác tham chiếu guid). (3) Bước 10 (APK/điện thoại) không làm được: `adb devices` rỗng → **chờ Planner**.
<!-- Base commit (trước khi làm) · Chạy bằng: <model id> · <effort> (dòng OK của brief_check.py) · giờ bắt đầu/kết thúc · commit của brief · file đã đổi · ghi chú lệch so với brief (nếu có, kèm lý do). -->

## Kết quả test
- Phần cần test:
  - FlyCameraCollision: đạt — FlyCameraCollisionTests 7/7.
  - Shader mặc định vẫn đục: đạt — OceanBackdropTests nằm trong PlayMode 15/15; console không lỗi shader; SimView grep guid Ocean.mat = 2.
  - Nước trong + nhìn từ dưới: đạt (cần user xem) — `client/Logs/water_above.png` thấy đáy cát/bậc đá qua nước gần bờ, xa dần tan vào trắng sương; `client/Logs/water_below.png` (IsUnderwater=True) nền xanh nước, thấy vách bờ phía trước và mặt nước từ dưới.
  - Va chạm trong Unity: đạt — FlyStopsAboveGround; Fly trên bờ cột (192,58): y=14,251, SurfaceHeight=13 → 13+1,251.
  - Hiệu ứng dưới nước bật/tắt: đạt — FlyIntoWaterSwitchesUnderwaterView.
  - Không gán terrain vẫn như cũ: đạt — FlyCameraMoveKeyPlayTests, SimViewPlayTests trong PlayMode 15/15.
  - Hồi quy world: đạt — EditMode 260/260, PlayMode 15/15.
  - Scene: đạt — TerrainView + Bench: near clip 0.1, 2 dòng terrain: {fileID: 1663237067}, collisionRadius 0.25, waterMaterial guid 55831bd2…; Water.mat: m_CustomRenderQueue 3000, _Alpha 0.45, _SrcBlend 5, _DstBlend 10, _ZWrite 0, _Cull 0; Ocean.mat không đổi.
  - Hiệu năng điện thoại: **chờ Planner** — không có thiết bị USB; chưa build APK.
  - Tài liệu: đạt — check_design.py exit 0.
- Lệnh: brief_check → OK; run_tests EditMode lọc → 7/7; menu + grep → như trên; EditMode toàn bộ → 260/260; PlayMode toàn bộ → 15/15 (đã `git checkout` EditorSettings.asset); bước 9 → xong; bước 10 → không có máy; check_design → exit 0; git diff Ocean.mat → rỗng.
<!-- Mỗi dòng ở "Phần cần test" → đạt / không đạt / chờ user + bằng chứng (tên test, dòng output).
Mỗi lệnh ở "Lệnh executor chạy": `<lệnh>` → <kết quả thật, dòng tổng kết>. -->

## Vướng mắc
<!-- Khi trạng thái = cần Opus: câu hỏi cụ thể + các phương án (kèm đề xuất). -->

---
<!-- PLANNER ghi khi review. -->

## Review
<!-- Kết luận: đã duyệt | cần sửa (thêm bước sửa vào "Các bước", trạng thái về chờ làm). Điểm đã đối chiếu. -->
- Review lần 1 (Opus · medium, 2026-10-20) — **chưa kết luận**, chờ 2 việc bên dưới.
- `Chạy bằng` claude-sonnet-5-5 · medium = `Model thực thi` Sonnet · medium: khớp.
- Kết quả test: đủ 10/10 dòng; 9 đạt, 1 chờ (hiệu năng điện thoại).
- Diff 39c906b (16 file) — đúng Phạm vi; `git status` sạch; Ocean.mat không đổi; SimView vẫn trỏ Ocean.mat.
  - `Ocean.shader`: mặc định Blend 1/0, ZWrite 1, Cull 2, `_Alpha` 1 → alpha = 1, giống shader cũ (QĐ 2). Lật pháp tuyến mặt sau bằng `IS_FRONT_VFACE` (QĐ 4). Alpha = lerp(1, lerp(_Alpha, 1, fres), độ rõ sương) đúng QĐ 3, không đọc depth.
  - `DebugFlyCamera.Fly`: terrain null → dời như cũ (QĐ 8); có terrain → đổi sang toạ độ local, gọi `FlyCameraCollision.Move`.
  - `FlyCameraCollision`: luật ô đặc/biên giống `BlockMesher.IsSolid`; thứ tự x → z → y, bước ≤ 0,25, kẹt thì dời tự do (QĐ 5); `IsUnderwater` đúng QĐ 7.
- **Lệch (1) — công thức kẹp `MoveAxis`:** brief sai, executor sửa đúng. `MoveStopsOnFloor` đi từ 6,5 xuống theo bước 0,25 → tới đúng 4,25 (hộp chạm mặt ô y = 4 nhưng không tính là chồng lấn); bước sau bị chặn, giới hạn 4,251 > p → `Math.Min(p, …)` giữ 4,25, mất Skin. Gán thẳng giới hạn an toàn: khi p không chồng lấn thì giới hạn lệch p tối đa Skin ngược chiều đi → không xuyên vách. Chấp nhận. Ghi nhận: theo "Dừng và chuyển lại Opus khi" (brief mâu thuẫn với test) lẽ ra phải dừng `cần Opus`, không tự sửa — lỗi do Planner viết công thức sai.
- **Lệch (2) — guid `Bench.unity.meta`:** trả về guid cũ, không ai tham chiếu guid mới. Chấp nhận.
- **Chờ (A) — bench điện thoại (bước 10):** 2026-10-20 review lại, `adb devices` vẫn rỗng. Khi máy cắm USB: Planner/Executor chạy bước 10 (build APK → cài → logcat), cần `pass30fps=True passNoHole=True`, ghi dòng `BENCH` vào đây.
- **Chờ (B) — user xem:** 4 mục ở "User tự test" (độ trong 0,45; dưới nước màu + tầm 0–40; va chạm/trượt vách; xuyên hồ không kẹt). Ảnh tham khảo: `client/Logs/water_above.png`, `client/Logs/water_below.png`.
- **Bench điện thoại (A) — đạt** (Planner chạy 2026-10-20 19:09, USB): `avgFps=43,0 p99Ms=41,59 maxMs=58,21 | buildMsAvg=7,38 buildMsMax=263,84 | holeFrames=0 | sysMemMaxMB=806,8 | pass30fps=True passNoHole=True | tris=1506249 waterTris=61190 | hash=a7719c8999939263 v3` (0010: 45,6 FPS). Lần build đầu lỗi vì Editor đang Play; đã thoát Play, build lại, trả `UnityConnectSettings.asset` + `Bench.unity.meta`.
- **User xem (B) — 2026-10-20:** dùng chuột; nước trong, va chạm "khá ổn"; **từ nước nhìn lên tối quá** (ảnh user: mặt nước nhìn từ dưới xanh sẫm). Nguyên nhân: mặt dưới lật pháp tuyến → không nhận sáng mặt trời + SH hướng xuống tối; nền dưới nước = màu sương sẫm (0,08; 0,32; 0,45). → Sửa lần 1: QĐ 10, bước 12–17.
- Kết luận: cần sửa
