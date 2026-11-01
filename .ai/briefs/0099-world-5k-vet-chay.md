# Brief 0099 — World 5k phần 4b: vệt chảy (hướng chảy vào đỉnh nước + shader vệt trôi theo hướng)

> Trạng thái: chờ làm (làm sau khi brief 0098 đã duyệt)
> Review: đầy đủ + user xem (gộp cuối 5k)
> Model thực thi: Sonnet · medium
> Cập nhật: 2026-11-02

## Mục tiêu
Phần 4 (hiển thị) của task [5k](../tasks/m1-world-5k-nuoc-kieu-anh-tham-khao.md). Dùng `WorldData.FlowDirs` của brief [0098](0098-world-5k-huong-chay-main-flow.md):
- Mặt nước chảy ngang có **vệt sáng dài trôi theo hướng chảy** (hướng cha→ô), liền qua chỗ rẽ và qua ranh chunk.
- **Ao, nguồn, biển nền:** không có vệt, chỉ gợn như cũ.
- **Mặt bên cột thác:** vệt trôi xuống.
- Tham số trong `water_m1.json` mục `look` [số tạm].

## Bối cảnh cần đọc
**Planner đã chốt thiết kế (2026-11-02), lý do:**
- Màu đỉnh nước đã dùng R (khoảng cách tới bờ), G (độ thoáng), A (độ dày). Chỉ còn B, không đủ cho vectơ 2 chiều có dấu. Vì vậy thêm **luồng đỉnh thứ 4**: TEXCOORD0, `SNorm8 × 4` = 4 byte/đỉnh.
  - Bố trí: x, y = hướng chảy (x, z) trong −1..1; z = cờ mặt thác 0..1; w = 0.
  - Đỉnh địa hình ghi 0. Docs: kích thước thuộc tính phải là bội của 4 byte; tối đa 4 luồng.
- **Hướng tại đỉnh mặt trên:** trung bình song tuyến hướng của tối đa 4 ô nước gần nhất cùng lớp.
  - Ô chảy không rơi lấy hướng theo FlowDirs; ô nguồn/ao có hướng 0; ô không phải nước bị bỏ, không tính vào trọng số.
  - Kết quả: vệt mờ dần khi vào ao, giữ đủ mạnh sát bờ. Cùng vị trí thế giới luôn cho cùng giá trị, nên liền qua ranh chunk.
  - Hàm chỉ đọc ô trong ±1, nên lề dựng lại chunk 1 ô (`CellMargin`) là đủ. Brief 0098 đã phát `WaterSurfaceChanged` khi hướng đổi.
- **Đỉnh không hướng lên** (mặt bên cột thác, bo mép tràn, bo chân thác, mặt bên/đáy nước khác): hướng 0, cờ thác 1. Mặt bên ô nước thường hiếm và thấp, nên có vệt dọc cũng chấp nhận được.
- **Shader:**
  - Hướng thay đổi theo chỗ (chỗ rẽ). Vì vậy không xoay toạ độ theo hướng, mà trộn 2 lớp sọc theo |f.x| và |f.z|: một lớp sọc dài dọc trục x, một lớp dọc trục z.
  - Sọc trôi theo hướng bằng 2 pha lệch nửa chu kỳ (kiểu flow map). Độ lệch không tăng mãi, nên chỗ rẽ không bị kéo méo theo thời gian.
  - Mặt thác: sọc dọc trục y trôi xuống.
  - Nhân `_VertexDepth`: `Ocean.mat` (= 0) và mesh có UV thường (primitive, vành biển) không bị đọc nhầm UV thành hướng chảy.
  - `_FlowStreakStrength` mặc định 0.
- Chi phí điện thoại:
  - Pixel có chảy: 4 lần `ValueNoise` cho 2 pha × 2 lớp. Pixel thác: 1 lần.
  - Pixel ao/biển bỏ qua nhờ `if`. Phần lớn màn hình là biển/ao nên vùng `if` đồng nhất.

**Code** (tại commit `468cc20` + brief 0098):
- `client/Assets/Game/World/ChunkMesh.cs` `:6–21` (trường `Colors` `:11`).
- `client/Assets/Game/World/ChunkMeshWriter.cs`:
  - chú thích lớp `:10–13`;
  - `WriteAll`: `SetVertexBufferParams` + 3 `GetVertexData` `:33–39`.
- `client/Assets/Game/World/BlockMesher.cs`:
  - `WaterVertexColor` `:53–56`;
  - `BuildChunks` `:289–359`:
    - ghim `cells` + safety handle `:301–306`;
    - khởi tạo `MeshJob` `:324–337`;
    - `finally` `:345–358` (`AtomicSafetyHandle.Release(cellsSafety)` + `UnsafeUtility.ReleaseGCObject(cellsGcHandle)`);
  - `struct Output` `:361–403`: `Create` `:369–379`, `ToChunkMesh` `:381–390`, `Dispose`;
  - `struct MeshJob` `:414`: các trường `:419–431`;
  - `WaterShoreAt` `:1728` (mẫu đọc ô theo toạ độ thế giới, `ly = floor(wp.y − 1e-3)`);
  - `CellAt` `:1661`, `IsFallingWater` `:1067`;
  - `AddWaterVertex` `:1776–1792`, `AddVertex` `:1794–1801`;
  - `MaterialAt` (đọc `Cells[x + SizeX * (z + SizeZ * p.y)]`).
- `client/Assets/Game/Client/Shaders/Ocean.shader` (sau 0097):
  - chú thích đầu file `:1–10`;
  - Properties `_SparkleFadeEnd` `:45`; CBUFFER `:66–68`;
  - `struct Attributes` `:110`, `Varyings` `:111–118`, `vert` `:121–133`;
  - `Sparkle` `:183–196`;
  - `frag`: `half3 c = diffuse + light.color * glow;` `:246`, `half foam = …` `:247`, `alpha = lerp(alpha, 1.0h, saturate(glow));` `:263`.
- `client/Assets/Game/Core/WaterConfig.cs`:
  - `configVersion = 7` `:12`, `sparkleFadeEnd` `:77`;
  - `Validate` kiểm `sparkleFadeEnd` `:110–112`; có `CheckColor`, `CheckRange`.
- `client/Assets/Game/Client/WaterLook.cs`: dòng cuối `_SparkleFadeEnd` `:40`.
- JSON: `design/data/water_m1.json` + `client/Assets/Game/Client/Resources/Config/water_m1.json` (giống hệt nhau).
- Test:
  - `Tests/World/BlockMesherTests.cs`: `StoneFloor16()` `:1050` (đá ở y = 4), `WaterVertexColor(mesh, normal, pos)` `:1070–1080`;
  - `Tests/Core/WaterConfigTests.cs` `:18`, `:66`, `:79`;
  - `Tests/ClientEdit/WaterLookTests.cs` (`sparkleFadeEnd = 40f` `:49`, kiểm `:82`, `GlintExtrasOffByDefault` `:101`);
  - mẫu render PlayMode: `Tests/Client/WaterGlintPlayTests.cs` (SetUp camera 10 m nhìn thẳng xuống, mặt trời đỉnh đầu, `RenderLinear`, `CountBrighter`).
- Docs (6000.4):
  - `VertexAttributeDescriptor` (bội 4 byte, tối đa 4 luồng) [docs](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/Rendering.VertexAttributeDescriptor.html);
  - `VertexAttributeFormat` [docs](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/Rendering.VertexAttributeFormat.html);
  - `Mesh.MeshData.SetVertexBufferParams` [docs](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/Mesh.MeshData.SetVertexBufferParams.html), `Mesh.MeshData.GetVertexData` [docs](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/Mesh.MeshData.GetVertexData.html);
  - `UnsafeUtility.PinGCArrayAndGetDataAddress` / `ReleaseGCObject` và `NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray`: dùng y như ghim `cells` ở `:301–306`;
  - test: `Mesh.SetVertices` [docs](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/Mesh.SetVertices.html), `Mesh.SetColors` [docs](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/Mesh.SetColors.html), `Mesh.SetUVs` [docs](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/Mesh.SetUVs.html), `Mesh.SetTriangles` [docs](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/Mesh.SetTriangles.html), `Mesh.SetNormals` [docs](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/Mesh.SetNormals.html);
  - `_Time` [docs](https://docs.unity3d.com/6000.4/Documentation/Manual/SL-UnityShaderVariables.html).
  - Không dùng API Unity nào khác.

## Quyết định đã chốt (không được đổi)
1. **`ChunkMesh`** thêm sau `Colors`:
   ```csharp
   /// <summary>Luồng TEXCOORD0 (SNorm8 × 4, byte thấp = x) của mọi đỉnh: hướng chảy (x, z) + cờ mặt thác cho đỉnh nước, 0 cho đỉnh địa hình (brief 0099). Null → ghi 0.</summary>
   public uint[] Flow;
   ```
2. **`ChunkMeshWriter.WriteAll`**:
   - thêm descriptor thứ 4: `new VertexAttributeDescriptor(VertexAttribute.TexCoord0, VertexAttributeFormat.SNorm8, 4, 3)`;
   - sau 3 dòng `GetVertexData`:
     ```csharp
     var flow = data.GetVertexData<uint>(3);
     if (chunk.Flow != null) flow.CopyFrom(chunk.Flow);
     else for (int i = 0; i < flow.Length; i++) flow[i] = 0u;
     ```
   - Chú thích lớp: thêm "luồng 3 = TEXCOORD0 SNorm8×4 hướng chảy (brief 0099)".
3. **`BlockMesher`**:
   - Hằng/hàm tĩnh, đặt sau `WaterVertexColor`:
     ```csharp
     /// <summary>Luồng TEXCOORD0 của đỉnh nước (brief 0099): x, y = hướng chảy (x, z) −1..1, z = cờ mặt thác 0..1, w = 0; SNorm8, byte thấp = x.</summary>
     static uint PackFlow(float2 f, float fall) =>
         (uint)(byte)(sbyte)(int)math.round(127f * math.clamp(f.x, -1f, 1f))
         | (uint)(byte)(sbyte)(int)math.round(127f * math.clamp(f.y, -1f, 1f)) << 8
         | (uint)(byte)(sbyte)(int)math.round(127f * math.saturate(fall)) << 16;
     ```
   - `Output`: thêm `public NativeList<uint> Flow;`, tạo trong `Create` (`Allocator.TempJob`), `Flow = Flow.AsArray().ToArray()` trong `ToChunkMesh`, `if (Flow.IsCreated) Flow.Dispose();` trong `Dispose`.
   - `MeshJob`: thêm `[ReadOnly] public NativeArray<byte> FlowDirs;` và `public bool HasFlowDirs;`.
   - `BuildChunks`: ngay sau khối ghim `cells` (`:301–306`) ghim mảng hướng theo cùng cách:
     ```csharp
     bool hasFlow = world.FlowDirs != null;
     byte[] flowSrc = world.FlowDirs ?? new byte[1];
     void* flowPtr = UnsafeUtility.PinGCArrayAndGetDataAddress(flowSrc, out ulong flowGcHandle);
     var flowDirs = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<byte>(flowPtr, flowSrc.Length, Allocator.None);
     #if ENABLE_UNITY_COLLECTIONS_CHECKS
     var flowSafety = AtomicSafetyHandle.Create();
     NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref flowDirs, flowSafety);
     #endif
     ```
     - job: `FlowDirs = flowDirs, HasFlowDirs = hasFlow,`;
     - `finally`: `AtomicSafetyHandle.Release(flowSafety)` trong `#if`, và `UnsafeUtility.ReleaseGCObject(flowGcHandle);`.
   - `AddVertex` (`:1794`): thêm `Out.Flow.Add(0u);` sau `Out.Colors.Add(color);`.
   - Hàm mới trong `MeshJob`, đặt sau `WaterShoreAt`:
     ```csharp
     /// <summary>Hướng chảy tại đỉnh mặt trên p (toạ độ chunk), brief 0099: trung bình song tuyến hướng (FlowDirs) của tối đa 4 ô nước gần nhất cùng lớp;
     /// chỉ ô chảy không rơi có hướng (nguồn/ao = 0), ô không phải nước hoặc ngoài map không tính.</summary>
     float2 FlowAt(float3 p)
     {
         if (!HasFlowDirs)
             return float2.zero;
         float3 wp = p + (float3)Origin;
         int ly = (int)math.floor(wp.y - 1e-3f);
         if (ly < 0 || ly >= SizeY)
             return float2.zero;
         int x0 = (int)math.floor(wp.x - 0.5f), z0 = (int)math.floor(wp.z - 0.5f);
         float2 acc = float2.zero;
         float wsum = 0f;
         for (int j = 0; j <= 1; j++)
         for (int i = 0; i <= 1; i++)
         {
             int x = x0 + i, z = z0 + j;
             if (x < 0 || x >= SizeX || z < 0 || z >= SizeZ)
                 continue;
             int idx = x + SizeX * (z + SizeZ * ly);
             ushort c = Cells[idx];
             if ((c & 0xFF) != (int)BlockMaterial.Water)
                 continue;
             float w = math.saturate(1f - math.abs(wp.x - (x + 0.5f))) * math.saturate(1f - math.abs(wp.z - (z + 0.5f)));
             if (w <= 0f)
                 continue;
             wsum += w;
             if (Cell.IsFlowing(c) && !Cell.IsFalling(c))
                 acc += w * DirVec(FlowDirs[idx]);
         }
         return wsum > 0f ? acc / wsum : float2.zero;
     }

     /// <summary>Mã FlowDirs (brief 0098) → vectơ (x, z): 1 = +z, 2 = +x, 3 = −z, 4 = −x, khác = 0.</summary>
     static float2 DirVec(byte code) =>
         code == 1 ? new float2(0, 1) : code == 2 ? new float2(1, 0) : code == 3 ? new float2(0, -1) : code == 4 ? new float2(-1, 0) : float2.zero;
     ```
   - `AddWaterVertex` (`:1776`): tính thêm `uint flow`:
     - nhánh `n.y > 0.999f`: `flow = PackFlow(FlowAt(p), 0f);`;
     - nhánh còn lại: `flow = PackFlow(float2.zero, 1f);`;
     - sau `int i = AddVertex(p, n, color);` thêm `Out.Flow[i] = flow;`.
   - Khoá hàn đỉnh giữ nguyên. Cùng vị trí và pháp tuyến cho cùng `FlowAt`, nên đỉnh hàn chung vẫn đúng.
4. **Shader `Ocean.shader`**:
   - Properties, sau `_SparkleFadeEnd` (`:45`):
     ```
     _FlowStreakColor("Flow streak color (brief 0099)", Color) = (1, 1, 1, 1)
     _FlowStreakStrength("Flow streak strength", Float) = 0
     _FlowStreakScale("Flow streak scale (per m)", Float) = 1.5
     _FlowStreakLength("Flow streak length (ratio)", Float) = 5
     _FlowStreakSpeed("Flow streak speed (cells/s)", Float) = 1.2
     _FlowStreakCycle("Flow streak cycle (s)", Float) = 2
     _FlowStreakCut("Flow streak cut", Float) = 0.55
     _FlowStreakSoft("Flow streak soft", Float) = 0.15
     _FlowFallSpeed("Fall streak speed (cells/s)", Float) = 3
     ```
   - CBUFFER, sau `:67`:
     ```hlsl
     half4 _FlowStreakColor;
     float _FlowStreakScale, _FlowStreakLength, _FlowStreakSpeed, _FlowStreakCycle, _FlowFallSpeed;
     half _FlowStreakStrength, _FlowStreakCut, _FlowStreakSoft;
     ```
   - `struct Attributes { float4 positionOS : POSITION; half4 color : COLOR; half4 flow : TEXCOORD0; };`
   - `Varyings` thêm `half3 flow : TEXCOORD4;`.
   - `vert` thêm: `o.flow = input.flow.xyz * _VertexDepth; // hướng chảy + cờ mặt thác (brief 0099); Ocean.mat (_VertexDepth 0) và mesh có UV thường → 0`
   - Hàm mới, đặt sau `Sparkle`:
     ```hlsl
     // Vệt chảy (brief 0099): mặt trên — sọc dài theo hướng chảy f (TEXCOORD0), trôi theo f bằng 2 pha lệch nửa chu kỳ (kiểu flow map) để chỗ hướng đổi không bị kéo méo;
     // sọc dọc x và sọc dọc z trộn theo |f.x|, |f.y| nên không phải xoay toạ độ; ao/nguồn f = 0 → không vệt. Mặt thác (fall) — sọc dọc trôi xuống.
     half StreakNoise(float2 u, half2 w)
     {
         float l = _FlowStreakLength;
         return w.x * ValueNoise(float2(u.x / l, u.y)) + w.y * ValueNoise(float2(u.x, u.y / l) + float2(17.3, 41.7));
     }

     half FlowStreak(float3 pws, half2 f, half fall)
     {
         half top = 0.0h, side = 0.0h;
         half mag = saturate(length(f));
         if (mag > 0.01h)
         {
             half2 w = abs(f) / (abs(f.x) + abs(f.y));
             float ph = _Time.y / _FlowStreakCycle;
             float p0 = frac(ph), p1 = frac(ph + 0.5);
             float travel = _FlowStreakSpeed * _FlowStreakCycle;
             half n0 = StreakNoise((pws.xz - f * (p0 * travel)) * _FlowStreakScale, w);
             half n1 = StreakNoise((pws.xz - f * (p1 * travel)) * _FlowStreakScale, w);
             half nb = lerp(n0, n1, abs(1.0 - 2.0 * p0));
             top = smoothstep(_FlowStreakCut, _FlowStreakCut + _FlowStreakSoft, nb) * mag;
         }
         if (fall > 0.01h)
         {
             float2 uf = float2((pws.x + pws.z) * _FlowStreakScale, (pws.y + _Time.y * _FlowFallSpeed) * _FlowStreakScale / _FlowStreakLength);
             side = smoothstep(_FlowStreakCut, _FlowStreakCut + _FlowStreakSoft, ValueNoise(uf)) * fall;
         }
         return saturate((top + side) * _FlowStreakStrength);
     }
     ```
   - `frag`:
     - sau `half3 c = diffuse + light.color * glow;` (`:246`), trước dòng `foam`:
       ```hlsl
       half streak = FlowStreak(input.positionWS, input.flow.xy, input.flow.z);
       c = lerp(c, _FlowStreakColor.rgb * lightTerm, streak);
       ```
     - sau `alpha = lerp(alpha, 1.0h, saturate(glow));` (`:263`): `alpha = lerp(alpha, 1.0h, streak);`.
   - Đầu file, sau dòng 0097 (`:10`):
     `// Brief 0099: vệt chảy — hướng chảy (TEXCOORD0.xy từ FlowDirs) + cờ mặt thác (TEXCOORD0.z); sọc trôi theo hướng (2 pha), thác trôi xuống; _FlowStreakStrength mặc định 0.`
   - Không sửa pass `WaterDepthPrepass` / `DepthOnly`.
5. **JSON** `configVersion` 7 → 8. Thêm vào `look`, sau `sparkleFadeEnd`, mọi số [số tạm], chưa chốt:
   ```json
   "flowStreakColor": [1.0, 1.0, 1.0],
   "flowStreakStrength": 0.35,
   "flowStreakScale": 1.5,
   "flowStreakLength": 5.0,
   "flowStreakSpeed": 1.2,
   "flowStreakCycle": 2.0,
   "flowStreakCut": 0.55,
   "flowStreakSoft": 0.15,
   "flowFallSpeed": 3.0
   ```
   - `WaterConfig.cs`: `configVersion = 8`; thêm 9 trường cùng tên, cùng giá trị, sau `sparkleFadeEnd`.
   - `Validate`, trước kiểm `foamLine + foamSoft`:
     - `CheckColor(flowStreakColor, …)`;
     - `CheckRange`: `flowStreakStrength` 0..1; `flowStreakScale` 0,1..8; `flowStreakLength` 1..20; `flowStreakSpeed` 0..6; `flowStreakCycle` 0,25..8; `flowStreakCut` 0..0,95; `flowStreakSoft` 0,01..0,5; `flowFallSpeed` 0..12;
     - `if (flowStreakCut + flowStreakSoft > 1f) throw new ArgumentException("flowStreakCut + flowStreakSoft must be <= 1.");`
   - `WaterLook.Apply`: sau `:40` thêm `_FlowStreakColor` (alpha 1, như `_FoamColor`) + 8 `SetFloat`, tên như QĐ 4.

## Phạm vi
- Được sửa:
  - `client/Assets/Game/World/ChunkMesh.cs`, `ChunkMeshWriter.cs`, `BlockMesher.cs`;
  - `client/Assets/Game/Client/Shaders/Ocean.shader` (Properties, CBUFFER, pass `ForwardLit`, chú thích đầu);
  - `client/Assets/Game/Core/WaterConfig.cs`, `client/Assets/Game/Client/WaterLook.cs`, 2 file `water_m1.json`;
  - test: `Tests/World/BlockMesherTests.cs` (chỉ thêm), `Tests/Core/WaterConfigTests.cs`, `Tests/ClientEdit/WaterLookTests.cs`;
  - test mới `client/Assets/Game/Tests/Client/WaterStreakPlayTests.cs` (+ `.meta` do Unity tạo).
- Không được sửa: `WorldEditor.cs`, `WorldData.cs`, `Water.mat`, `Ocean.mat`, `Terrain.shader`, `TerrainChunkView.cs`, `OceanBackdrop.cs`, `design/` (Planner đã cập nhật world-tech/glossary/ideas), mọi test khác.

## Các bước
1. **Test mesh trước** (`BlockMesherTests`). Helper `static uint WaterVertexFlow(ChunkMesh mesh, float3 normal, float3 pos)` giống `WaterVertexColor` nhưng trả `mesh.Flow[i]`, và `static int SB(uint f, int k) => (sbyte)((f >> (8 * k)) & 0xFF);`. 3 test:
   - `WaterSurfaceFlowEncoded`:
     - Dựng: `StoneFloor16()`; lớp y = 5 toàn `Cell.Make(BlockMaterial.Stone)`, trừ hàng z = 8: `(0, 5, 8) = Cell.Make(BlockMaterial.Water, Cell.MaxWaterLevel)`; x = 1..15: `Cell.MakeFlowing(0, 5, false)`.
     - `var dirs = w.EnsureFlowDirs();` x = 1..15: `dirs[w.Size.Index(x, 5, 8)] = 2;`. `BlockMesher.Build(w, 0, 0, 0, true, 5)`.
     - Đỉnh up `(5, 5.9, 8)` và `(5, 5.9, 9)`: `SB(f, 0)` = 127, `SB(f, 1)` = 0, `SB(f, 2)` = 0.
     - Đỉnh `(1, 5.9, 8)`: `SB(f, 0)` = 64 ±1, `SB(f, 1)` = 0.
     - Đỉnh `(0, 5.9, 8)`: f = 0.
   - `WaterFlowZeroWithoutFlowDirs`: world như trên nhưng **không** gọi `EnsureFlowDirs` → đỉnh `(5, 5.9, 8)`: f = 0.
   - `WaterColumnSideFlowIsFall`:
     - Dựng: `StoneFloor16()`; `(8, 5, 8)` và `(8, 6, 8)` = `Cell.MakeFlowing(0, 5, true)`; `Build`.
     - Duyệt `mesh.WaterIndices`:
       - đỉnh có `Normals[i].y < 0.999f`: đếm, kiểm `SB(f, 2)` = 127, `SB(f, 0)` = 0, `SB(f, 1)` = 0;
       - đỉnh còn lại: `SB(f, 2)` = 0.
     - Số đỉnh không hướng lên > 0. Mọi đỉnh trong `mesh.Indices` (địa hình): `mesh.Flow[i] == 0`. Kiểm `mesh.Flow.Length == mesh.Vertices.Length`.
   - Xong khi: lỗi biên dịch mong đợi (thiếu `ChunkMesh.Flow`), ghi output console.
2. **`ChunkMesh` + `BlockMesher`** theo QĐ 1, 3. Xong khi: 3 test bước 1 + toàn bộ `BlockMesherTests` cũ đạt.
3. **`ChunkMeshWriter`** theo QĐ 2.
4. **Config** theo QĐ 5. `WaterConfigTests`:
   - `:18` → 8;
   - `LookParsesAndValidates` thêm `flowStreakStrength` = 0,35 và `flowStreakLength` = 5;
   - `LookRejectsOutOfRange` thêm `flowStreakCycle = 0.1f` và `flowStreakCut = 0.9f, flowStreakSoft = 0.2f` → `ArgumentException`.
5. **Shader + `WaterLook`** theo QĐ 4–5. `WaterLookTests`:
   - `ApplySetsAllProperties` gán và kiểm 9 thuộc tính mới:
     - `flowStreakColor` (0,2; 0,3; 0,4);
     - `flowStreakStrength` 0,5; `flowStreakScale` 2; `flowStreakLength` 7; `flowStreakSpeed` 2;
     - `flowStreakCycle` 3; `flowStreakCut` 0,6; `flowStreakSoft` 0,2; `flowFallSpeed` 4.
   - Sửa `<summary>`: "+ 9 (vệt chảy, 0099)".
   - Test mới `FlowStreakOffByDefault`: `_FlowStreakStrength` của material mới = 0.
   - Xong khi: `refresh_unity` → `read_console` Error = 0, không cảnh báo shader `Game/Ocean`.
6. **Test render PlayMode mới** `WaterStreakPlayTests.cs`, namespace `Game.Client.Tests`.
   - SetUp/TearDown, `Size = 256`, `RenderLinear`, `CountBrighter`: chép từ `WaterGlintPlayTests`, gồm cả camera và đèn.
   - Thay plane primitive bằng mesh tự dựng:
     - 4 đỉnh `(−6, 0, −6)`, `(6, 0, −6)`, `(6, 0, 6)`, `(−6, 0, 6)`;
     - tam giác `{0, 2, 1, 0, 3, 2}`, nhìn từ trên thấy mặt trước; kiểm bằng test 1;
     - pháp tuyến `Vector3.up`; màu `new Color32(255, 255, 255, 255)` cho cả 4 đỉnh;
     - UV0 = `List<Vector4>` 4 phần tử giống nhau, đặt bằng hàm `SetFlow(Vector4 f)` gọi `mesh.SetUVs(0, list)`.
     - `MeshFilter` + `MeshRenderer` trên một GameObject; huỷ mesh ở TearDown.
   - Material `Game/Ocean` mặc định (đục): `_VertexDepth` = 1, `_RippleStrength` 0, `_Glint` 0, `_FlowStreakStrength` 0.
   - Test 1 `StreakAppearsOnFlowingSurface`:
     - `SetFlow((1, 0, 0, 0))`; render `a`;
     - `_FlowStreakStrength` = 1; render `b`;
     - `n = CountBrighter(a, b, 0.05f)`: `n > Size * Size / 20` và `n < Size * Size * 9 / 10`.
   - Test 2 `NoStreakWithoutFlow`: `SetFlow((0, 0, 0, 0))`; tương tự → `CountBrighter(a, b, 0.01f) == 0`.
   - Test 3 `NoStreakOnOceanMaterial`: `SetFlow((1, 0, 0, 0))`, `_VertexDepth` = 0 → `CountBrighter(a, b, 0.01f) == 0`.
   - Test 4 `FallStreakOnMarkedSurface`: `SetFlow((0, 0, 1, 0))` → `n > Size * Size / 20`.
   - Mỗi test `Debug.Log` số đo và có assert message.
   - Xong khi: `run_tests` PlayMode lọc `WaterStreakPlayTests` đạt 4/4.
7. **Kiểm tổng** theo "Lệnh executor chạy".

## Tiêu chí xong
- 3 test mesh mới đạt và đã ghi lỗi biên dịch trên code cũ. `BlockMesherTests` cũ đạt, số đỉnh/tam giác không đổi.
- `WaterConfigTests`, `ConfigTests`, `WaterLookTests` đạt. `WaterStreakPlayTests` 4/4, có số đo.
- EditMode toàn bộ đạt. **PlayMode toàn bộ** đạt, vì bố cục đỉnh của mọi chunk đổi.
- Console 0 lỗi. `git status` chỉ có file trong Phạm vi.

## Test sau khi xong

### Phần cần test
- Hướng chảy vào đỉnh (mới) · `BlockMesherTests.WaterSurfaceFlowEncoded`, `WaterFlowZeroWithoutFlowDirs` · 127 / 64 / 0.
- Cờ mặt thác + địa hình = 0 (mới) · `WaterColumnSideFlowIsFall` · đỉnh không hướng lên: z = 127; địa hình: 0.
- Mesh cũ (regression) · toàn bộ `BlockMesherTests`, `IncrementalRebuildTests`, `GoldenHashTests` · đạt.
- Luồng đỉnh thứ 4 khi ghi mesh (đổi) · PlayMode toàn bộ (mọi chunk qua `ChunkMeshWriter`) · đạt, console không lỗi `SetVertexBufferParams`.
- Config vệt chảy (mới) · `WaterConfigTests`, `ConfigTests.WaterClientCopyMatchesDesignData` · đạt; `configVersion` 8.
- Áp tham số + tắt mặc định (mới) · `WaterLookTests` · đạt.
- Shader vệt chảy (mới) · `WaterStreakPlayTests` · có vệt khi có hướng / có cờ thác; không vệt khi hướng 0 hoặc `_VertexDepth` 0.
- Nước/biển cũ (regression) · `OceanBackdropTests`, `WaterFlowPlayTests`, `WaterLayerPlayTests`, `DayCycleWaterPlayTests`, `FlyCameraWaterPlayTests`, `SeaSkyTintPlayTests`, `WaterGlintPlayTests` (nằm trong PlayMode toàn bộ) · đạt, không đổi baseline.

### Lệnh executor chạy
1. Sau bước 1: `refresh_unity` → `read_console` (Error): ghi lỗi biên dịch mong đợi.
2. Sau bước 2–3: `refresh_unity` → `read_console` (Error) = 0 → `run_tests` EditMode lọc `BlockMesherTests` → đạt.
3. Sau bước 4–5: `refresh_unity` → `read_console` = 0 → `run_tests` EditMode lọc `WaterConfigTests`, `ConfigTests`, `WaterLookTests` → đạt.
4. Sau bước 6: `run_tests` PlayMode lọc `WaterStreakPlayTests` → 4/4.
5. `run_tests` EditMode toàn bộ → đạt.
6. `run_tests` PlayMode toàn bộ → đạt. Sau đó dọn:
   - `git checkout -- client/ProjectSettings/EditorSettings.asset` (và `ProjectSettings.asset` nếu `runInBackground` đổi);
   - xoá `InitTestScene*` nếu có.
7. `git status` / `git diff --stat` → chỉ file trong Phạm vi.

### User tự test
Gộp cuối 5k. Play scene SimView, ban ngày:
- (a) Đào một rãnh từ ao/biển: trên mặt rãnh có vệt sáng dài trôi theo hướng nước chảy, liền qua chỗ rẽ, không giật hay méo ở góc.
- (b) Mặt ao, biển nền: không vệt, chỉ gợn như cũ.
- (c) Thác demo / đổ nước từ trên cao: mặt cột thác có vệt trôi xuống.
- (d) Rãnh cắt ngang ranh chunk: vệt liền, không đứt. Đặt khối làm nước đổi hướng: vệt đổi theo.
- (e) Vệt không quá dày, không quá chói cạnh bọt bờ và lấp lánh.
- Số chỉnh được: các trường `flowStreak*` và `flowFallSpeed` trong `design/data/water_m1.json`, mục `look`. Ví dụ: vệt quá dày → tăng `flowStreakCut`; quá ngắn → tăng `flowStreakLength`; trôi chậm → tăng `flowStreakSpeed`.

## Dừng và chuyển lại Opus khi
- Brief 0098 chưa duyệt (chưa có `WorldData.FlowDirs`).
- Brief mơ hồ hoặc mâu thuẫn với code thật (số dòng lệch nhiều, chữ ký khác).
- Giá trị ở bước 1 khác kỳ vọng mà code đúng QĐ 3: ghi giá trị thật, không tự đổi kỳ vọng.
- `SetVertexBufferParams` báo lỗi định dạng/luồng, hoặc shader lỗi / vượt giới hạn interpolator.
- Test PlayMode cũ fail: ghi lỗi, không tự đổi baseline.
- Cần API Unity ngoài brief, quyết định ngoài "Quyết định đã chốt", hoặc sửa file ngoài "Phạm vi".
- Cùng một lỗi sửa 2 lần vẫn chưa qua.

---
<!-- Từ đây trở xuống: EXECUTOR ghi. -->

## Kết quả

## Kết quả test

## Vướng mắc

---
<!-- PLANNER ghi khi review. -->

## Review
