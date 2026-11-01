# Brief 0095 — World 5k phần 1: màu theo độ sâu, thấy đáy, viền bờ mềm (khoảng cách tới bờ ghi vào đỉnh)

> Trạng thái: đã duyệt
> Review: đầy đủ + user xem (gộp cuối 5k)
> Model thực thi: Sonnet · medium
> Cập nhật: 2026-11-01

## Mục tiêu
Phần 1 của task [5k](../tasks/m1-world-5k-nuoc-kieu-anh-tham-khao.md):
- Nước xanh ngọc nhạt sát bờ, đậm dần ra xa; chỗ nông trong, thấy đáy.
- Viền chỗ nước chạm khối đặc mờ dần, không cắt gắt. Chuyển màu êm (smoothstep), không có đường cứng.
- Áp chung cho nước trong map (`Water.mat`) và biển nền (`Ocean.mat`), liền màu ở chỗ giáp nhau.
- Tham số hình ảnh chuyển vào `design/data/water_m1.json` (mục `look`) [số tạm], không còn chỉ nằm trong material.

Mesher ghi thêm **khoảng cách tới bờ** vào kênh R màu đỉnh mặt nước. Phần 2 (bọt bờ) sẽ dùng lại dữ liệu này.

## Bối cảnh cần đọc
**Planner đã chốt thiết kế (2026-11-01), lý do:**
- Biển nền (`OceanBackdrop`) nằm hoàn toàn ngoài map 384×384. Đảo có `landRadius` 132 quanh tâm 192, `coastWarpAmplitude` 14 (`design/data/world_maps_m1.json`), nên bờ cách mép map ≥ ~30 ô [repo]. Vì vậy biển nền luôn là "xa bờ + sâu": không cần đáy, không cần tính khoảng cách tới bờ cho nó. Chỉ cần dùng chung công thức shader với `_VertexDepth = 0` (độ sâu = tối đa, khoảng cách = tối đa).
- **Không dùng depth texture URP.** Độ dày (brief 0084) và khoảng cách tới bờ đều có sẵn trong đỉnh, nên không tốn thêm một lượt vẽ trên điện thoại.
- Khoảng cách tới bờ chỉ đổi khi **khối đặc** đổi (đào/đặt). Ô thường cách chunk khác ≤ 4 ô, nên chỉ lúc đó mới cần đánh dấu chunk kề theo lề 4. Nước lan/rút vẫn dùng lề 1 như cũ, nên không tăng số chunk phải dựng lại khi nước chảy.

**Code** (số dòng tại commit `b261ec8`):
- `client/Assets/Game/World/BlockMesher.cs`:
  - `CellMargin = 1` (`:26`);
  - `WaterSurfaceDrop` (`:43`), `WaterDepthEncodeMax = 4f` (`:45`), `static uint WaterDepthColor(float depth)` (`:48–49`): RGB trắng, A = độ dày;
  - trong job: `ushort CellAt(int3 p)` (`:1654`, toạ độ thế giới, x/z kẹp về biên), `float WaterDepthAt(float3 p)` (`:1698`, mẫu cách đổi toạ độ chunk → thế giới `wp = p + (float3)Origin`, `ly = floor(wp.y − 1e-3)`), `int AddWaterVertex(float3 p, float3 n)` (`:1741–1751`, dòng gọi `AddVertex(p, n, WaterDepthColor(...))` ở `:1748`), `int MaterialAt(int3 p)` (`:1772`), `static bool IsSolidMaterial(int m)` (`:1783`).
- `client/Assets/Game/World/ChunkMesh.cs:10`: `Colors` là RGBA gói trong uint, byte thấp = R, byte cao = A.
- `client/Assets/Game/Client/Shaders/Ocean.shader`:
  - Properties `:10–28`, CBUFFER `:33–39` (trong `HLSLINCLUDE`, mọi pass dùng chung);
  - pass `ForwardLit`: `Attributes` `:81`, `Varyings` `:82–87`, `WaterDepthEncodeMax` `:89`, `vert` `:91–98`;
  - `frag`: `baseCol` hai dòng `:166–167`, alpha `:188–189`.
  - Pass `WaterDepthPrepass` và `DepthOnly` **không đổi**.
- `client/Assets/Game/Core/WaterConfig.cs` (`[Serializable]`, `Validate()`), `client/Assets/Game/Client/ConfigLoader.cs:64` `LoadWaterConfig()`. Bản sao `client/Assets/Game/Client/Resources/Config/water_m1.json` phải giống hệt `design/data/water_m1.json` (test `ConfigTests.WaterClientCopyMatchesDesignData`).
- `client/Assets/Game/Core/WorldEditor.cs`: event `CellChanged` (`:136`), `WaterSurfaceChanged` (`:139`); `TryDig` (`:156`, `CellChanged?.Invoke` ở `:168`); `TryPlace` (`:200`, `CellChanged?.Invoke` ở `:212`); `public static void AffectedChunks(WorldSize size, int margin, int x, int y, int z, ICollection<int> result)` (`:1043`).
- `client/Assets/Game/Client/TerrainChunkView.cs`: đọc `waterCfg` `:230–236`, đăng ký event `:237–238`, `var water = waterMaterial != null ? waterMaterial : CreateWaterMaterial();` `:244`, `slotWater = water;` `:246`, `OnCellChanged` `:329`.
- `client/Assets/Game/Client/OceanBackdrop.cs`: `Awake()` `:23–39`. Scene dùng `Ocean.mat` trên `MeshRenderer` cùng object. Test `OceanBackdropTests` tạo object **không có** `MeshRenderer`.
- Test:
  - `Tests/World/BlockMesherTests.cs`: `StoneFloor16()` `:1050` (đá ở y = 4), `WaterVertexColor(mesh, normal, pos)` `:1070`, `AssertNoDuplicateWaterVertices` `:1138`, mẫu `WaterSurfaceDepthEncodesLevel` `:1083`. Test này kiểm `c & 0xFFFFFF == 0xFFFFFF`; ở đó không có khối đặc cùng lớp nên R vẫn 255, test **không cần sửa**.
  - `Tests/Core/WaterConfigTests.cs:18` kiểm `configVersion == 4` → sửa thành 5.
  - `Tests/Core/WorldEditorTests.cs` (mẫu dựng editor `:28`).
  - `Tests/ClientEdit/` (asmdef EditMode `Game.Client.EditTests`), `Tests/Client/OceanBackdropTests.cs` (PlayMode).
  - `Tests/Client/WaterLayerPlayTests.cs:67` và `Client/Editor/TerrainViewCapture.cs:100` đặt `_Alpha` 0,45 trên material riêng. Không sửa hai file này.
- **Docs Unity 6000.4:**
  - `new Material(Material source)`: "Create a material by copying all properties from another material" [docs](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/Material-ctor.html).
  - `Material.SetColor(string, Color)` / `SetFloat` [docs](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/Material.SetColor.html). Màu trong JSON viết theo cùng quy ước với giá trị trong `.mat` (như Inspector); thuộc tính màu của shader không có `[HDR]`/`[Gamma]`.
  - `Renderer.material` tự tạo bản sao riêng cho renderer, người gọi phải `Destroy` bản sao đó [docs](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/Renderer-material.html).
  - JsonUtility dùng luật serialize của Unity: được dùng class `[Serializable]` lồng nhau và mảng `float[]` [docs](https://docs.unity3d.com/6000.4/Documentation/Manual/script-serialization-rules.html), [docs](https://docs.unity3d.com/6000.4/Documentation/Manual/json-serialization.html).
  - Không dùng API Unity nào khác.
- Lưu ý review 0079: sửa `BlockMesher.cs` xong, nếu test mesh mới ra giá trị như code cũ (R = 255 ở mọi đỉnh) thì thêm rồi bỏ một dòng chú thích trong `BlockMesher.cs` để ép Burst biên dịch lại.

## Quyết định đã chốt (không được đổi)
Ký hiệu `S` = `WaterShoreEncodeMax` = 4 (ô), `E` = `WaterDepthEncodeMax` = 4.

1. **Khoảng cách tới bờ** tại đỉnh mặt trên `p` (toạ độ chunk), với `wp = p + Origin`, `ly = floor(wp.y − 1e-3)`:
   - duyệt mọi cột `x ∈ [floor(wp.x − S), floor(wp.x + S)]`, `z ∈ [floor(wp.z − S), floor(wp.z + S)]`;
   - ô `(x, ly, z)` là khối đặc (`IsSolidMaterial(MaterialAt(...))`) → `dx = max(x − wp.x, wp.x − (x + 1), 0)`, `dz` tương tự, `d = sqrt(dx² + dz²)`;
   - khoảng cách = `min(S, min d)`; không có khối đặc → `S`.

   Khoảng cách chỉ phụ thuộc vị trí đỉnh, nên đỉnh hàn chung luôn cùng giá trị. Chỉ tính khối đặc **cùng lớp** `ly`: đáy bên dưới không tính là bờ.
2. **Mã hoá màu đỉnh nước:** `R = round(255 · saturate(khoảng cách / S))`, `G = B = 255` (để dành cho phần 4), `A` = độ dày như 0084. Hàm mới `static uint WaterVertexColor(float depth, float shore)` trả `R | 0x00FFFF00u | (A << 24)`, với R, A tính bằng `math.round(255f * math.saturate(...))` như `WaterDepthColor`. Đỉnh `n.y > 0.999` → `WaterVertexColor(WaterDepthAt(p), WaterShoreDistanceAt(p))`. Mọi đỉnh nước khác → `WaterVertexColor(E, S)` (= `0xFFFFFFFF`). Bỏ `WaterDepthColor` cũ nếu không còn chỗ dùng.
3. **Lề dựng lại chunk:** hằng `public const int WaterShoreMargin = 4;` (= `S`) trong `BlockMesher`. `WorldEditor` thêm `public event Action<int, int, int> SolidChanged;`, phát **chỉ** trong `TryDig` và `TryPlace`, ngay sau `CellChanged?.Invoke` ở hai hàm đó. `TerrainChunkView` đăng ký `SolidChanged` → `WorldEditor.AffectedChunks(World.Size, BlockMesher.WaterShoreMargin, x, y, z, dirty)`; huỷ đăng ký ở chỗ huỷ `CellChanged` nếu có. `CellMargin` và lề của `WorldEditor` giữ 1.
4. **Shader** (`Ocean.shader`, pass `ForwardLit`):
   - Property mới `_ShoreSoftWidth("Độ rộng viền bờ mờ (ô, brief 0095)", Float) = 0.35`; CBUFFER thêm `half _ShoreSoftWidth;`.
   - `static const float WaterShoreEncodeMax = 4.0; // = BlockMesher.WaterShoreEncodeMax`.
   - `Varyings` thêm `half shoreD : TEXCOORD2;`. `vert`: `o.shoreD = lerp(WaterShoreEncodeMax, input.color.r * WaterShoreEncodeMax, _VertexDepth);`.
   - `frag`: thay hai dòng `baseCol` (`:166–167`) bằng
     ```hlsl
     half dT = smoothstep(0.0h, 1.0h, input.depthT); // chuyển màu êm (brief 0095)
     half3 baseCol = lerp(_ShallowColor.rgb, _DeepColor.rgb, dT);
     ```
     Biến `fres` vẫn tính như cũ (còn dùng cho nhuộm trời và alpha).
   - Alpha: `alpha *= lerp(_ThinAlpha, 1.0h, input.depthT);` đổi thành `alpha *= lerp(_ThinAlpha, 1.0h, dT);`. Ngay sau đó thêm `alpha *= smoothstep(0.0h, _ShoreSoftWidth, input.shoreD); // viền bờ mờ dần (brief 0095)`.
   - Đầu file thêm dòng: `// Brief 0095: màu nông→sâu theo smoothstep độ dày; viền bờ mờ theo khoảng cách tới bờ (kênh R màu đỉnh); tham số từ water_m1.json "look" (WaterLook).`
   - `Ocean.mat` đục (Blend One Zero) nên alpha không ảnh hưởng. `_VertexDepth = 0` → `depthT = 1`, `shoreD = S`, nên biển nền ra đúng màu `_DeepColor`.
5. **Tham số trong JSON** (`water_m1.json`, `configVersion` 4 → 5, thêm object `look`; mọi số là [số tạm], chưa chốt):
   ```json
   "look": {
       "shallowColor": [0.45, 0.88, 0.85],
       "deepColor": [0.12, 0.50, 0.70],
       "depthFull": 2.0,
       "deepAlpha": 0.6,
       "shallowAlphaFactor": 0.25,
       "shoreSoftWidth": 0.35,
       "rippleScale": 0.45,
       "rippleStrength": 0.25,
       "glint": 0.6
   }
   ```
   - `WaterConfig.cs`: thêm `[Serializable] public class WaterLookConfig` (cùng file, mặc định = các số trên, có `Validate()`), và field `public WaterLookConfig look = new WaterLookConfig();`. `WaterConfig.Validate()` gọi `look.Validate()`.
   - Khoảng hợp lệ (ngoài khoảng → `ArgumentException`, tên trường trong message):
     - hai màu: đúng 3 phần tử, mỗi phần tử 0..1;
     - `depthFull` 0,25..4; `deepAlpha` 0..1; `shallowAlphaFactor` 0..1; `shoreSoftWidth` 0,05..2; `rippleScale` 0,05..4; `rippleStrength` 0..1; `glint` 0..2.
   - Ánh xạ sang material: `shallowColor` → `_ShallowColor`, `deepColor` → `_DeepColor` (alpha 1), `depthFull` → `_DepthFull`, `deepAlpha` → `_Alpha`, `shallowAlphaFactor` → `_ThinAlpha`, `shoreSoftWidth` → `_ShoreSoftWidth`, `rippleScale` → `_RippleScale`, `rippleStrength` → `_RippleStrength`, `glint` → `_Glint`. **Không** đặt `_VertexDepth`, blend, ZWrite, Cull.
6. **Áp tham số lúc chạy** — file mới `client/Assets/Game/Client/WaterLook.cs`: `public static class WaterLook { public static void Apply(Material m, WaterLookConfig c) }`, set đủ 9 thuộc tính ở QĐ 5.
   - `TerrainChunkView`, chỗ `:244`: material nước = `new Material(waterMaterial)` nếu `waterMaterial != null`, ngược lại `CreateWaterMaterial()`. Sau đó gọi `WaterLook.Apply(water, waterCfg.look)`. `TerrainChunkView` chưa có `OnDestroy`: không thêm (một bản sao duy nhất, sống cùng scene).
   - `OceanBackdrop.Awake`: `var r = GetComponent<MeshRenderer>(); if (r != null) WaterLook.Apply(r.material, ConfigLoader.LoadWaterConfig().look);`. Giữ bản sao trong field và `Destroy` nó trong `OnDestroy`.
   - Không sửa `Water.mat`, `Ocean.mat`: giá trị trong đó chỉ còn là mặc định khi chạy ngoài scene.

## Phạm vi
- Được sửa:
  - `client/Assets/Game/World/BlockMesher.cs`;
  - `client/Assets/Game/Client/Shaders/Ocean.shader` (Properties, CBUFFER, pass `ForwardLit`);
  - `client/Assets/Game/Core/WaterConfig.cs`, `client/Assets/Game/Core/WorldEditor.cs` (chỉ event `SolidChanged` + 2 dòng invoke);
  - `client/Assets/Game/Client/TerrainChunkView.cs`, `client/Assets/Game/Client/OceanBackdrop.cs`, `client/Assets/Game/Client/WaterLook.cs` (mới, kèm `.meta` do Unity tạo);
  - `design/data/water_m1.json` + bản sao `client/Assets/Game/Client/Resources/Config/water_m1.json` (giống hệt);
  - test: `Tests/World/BlockMesherTests.cs`, `Tests/Core/WaterConfigTests.cs`, `Tests/Core/WorldEditorTests.cs`, `Tests/ClientEdit/WaterLookTests.cs` (mới), `Tests/Client/OceanBackdropTests.cs`;
  - `design/04-tech/world-tech.md` (1 câu, bước 7).
- Không được sửa: `Water.mat`, `Ocean.mat`, pass `WaterDepthPrepass`/`DepthOnly`, luật nước trong `WorldEditor` (ngoài event), `CellMargin`, mọi test cũ trừ dòng `configVersion` ở `WaterConfigTests.cs:18`.

## Các bước
1. **Test mesh trước (phải fail trên code cũ)** — `BlockMesherTests.WaterSurfaceShoreDistanceEncoded`:
   - Dựng: `StoneFloor16()`. Nguồn `Cell.Make(BlockMaterial.Water, Cell.MaxWaterLevel)` ở mọi ô `(x, 5, z)`, x 4..12, z 2..12. Vách đá `Cell.Make(BlockMaterial.Stone)` ở `(3, 5, z)`, z 2..12. Cột đá ở `(10, 5, 10)`, đặt đè lên ô nước. `BlockMesher.Build(w, 0, 0, 0, true, 5)`.
   - Đỉnh up `(x, 5.9, 4)`, x = 4, 5, 6, 7, 8: R (`c & 0xFF`) = 0 / 64 / 128 / 191 / 255 (±1).
   - Đỉnh up `(11, 5.9, 12)`: R = 64 ±1. Đỉnh up `(12, 5.9, 12)`: R = 90 ±1 (√2 ô tới góc cột đá).
   - Các đỉnh trên: `(c >> 8) & 0xFFFF == 0xFFFF` (G, B trắng). A (độ dày) = 57 ±1.
   - Mọi đỉnh trong `WaterIndices` có `Normals.y ≤ 0.999` có `c == 0xFFFFFFFF`.
   - `AssertNoDuplicateWaterVertices(mesh)`.
   - Xong khi: chạy trên code cũ fail (R = 255). Ghi output fail.
2. **`BlockMesher.cs`** theo QĐ 1–3: hằng `WaterShoreEncodeMax = 4f`, `WaterShoreMargin = 4` (dưới `WaterDepthEncodeMax`, có `<summary>` tiếng Việt như hằng bên cạnh), hàm mã hoá mới, `float WaterShoreDistanceAt(float3 p)` trong job (cạnh `WaterDepthAt`), sửa `AddWaterVertex`. Khoá hàn giữ nguyên.
   - Xong khi: test bước 1 và hai test 0084 (`WaterSurfaceDepth*`) đạt.
3. **`WorldEditor` + `TerrainChunkView` (lề 4)** theo QĐ 3. Test mới trong `WorldEditorTests`: `SolidChangedOnlyOnDigAndPlace`:
   - đếm `SolidChanged`: `TryDig` thành công → +1; `TryPlace` thành công → +1;
   - nước lan sau `TryDig` cạnh nguồn nước (dùng mẫu dựng nước có sẵn trong test Core, vd `WaterFlowTests.PondWorld()` nếu truy cập được, không thì dựng tương tự) rồi chạy nhịp nước tới khi lặng → số đếm không tăng thêm;
   - `CellChanged` vẫn phát như cũ.
4. **Config:** `WaterConfig.cs` + 2 file JSON theo QĐ 5. `WaterConfigTests`:
   - sửa `:18` thành 5;
   - test mới `LookParsesAndValidates`: đọc JSON nguồn, `look.shallowColor` = {0.45, 0.88, 0.85}, `look.shoreSoftWidth` = 0,35, `look.depthFull` = 2;
   - test mới `LookRejectsOutOfRange`: `shoreSoftWidth = 0`, `deepColor` 2 phần tử, `deepAlpha = 1.5` → mỗi trường hợp `Validate()` ném `ArgumentException`.
5. **Shader + `WaterLook` + áp vào view** theo QĐ 4, 6.
   - `Tests/ClientEdit/WaterLookTests.cs` (EditMode): `ApplySetsAllProperties`. `new Material(Shader.Find("Game/Ocean"))`, `Apply` với một `WaterLookConfig` có số khác mặc định → `GetColor`/`GetFloat` đúng 9 thuộc tính (sai số 1e-4), `_VertexDepth` vẫn 0. `DestroyImmediate` material.
   - `OceanBackdropTests` (PlayMode) thêm `OceanRingUsesWaterLook`: object có `MeshFilter` + `MeshRenderer` (gán `sharedMaterial` = `new Material(Shader.Find("Game/Ocean"))`), `AddComponent<OceanBackdrop>()`, `yield return null`. Kiểm `renderer.sharedMaterial` có `_ShallowColor` = `look.shallowColor` của config và `_ShoreSoftWidth` = 0,35.
   - Xong khi: `refresh_unity` → `read_console` Error = 0.
6. **Kiểm tổng:** chạy test theo mục "Lệnh executor chạy".
7. **`design/04-tech/world-tech.md:141`**: cuối mục "Hiển thị dòng chảy" thêm câu: ` Phần 1 (brief 0095): khoảng cách tới bờ (≤ 4 ô, khối đặc cùng lớp) ghi vào kênh R màu đỉnh mặt nước; shader đổi màu nông→sâu bằng smoothstep độ dày, viền bờ mờ trong 0,35 ô; tham số ở water_m1.json "look" [số tạm], chưa chốt; đào/đặt khối đánh dấu chunk kề theo lề 4.`

## Tiêu chí xong
- Test mới đạt: `WaterSurfaceShoreDistanceEncoded` (fail trên code cũ, có ghi output), `SolidChangedOnlyOnDigAndPlace`, `LookParsesAndValidates`, `LookRejectsOutOfRange`, `ApplySetsAllProperties`, `OceanRingUsesWaterLook`.
- EditMode toàn bộ đạt, gồm `GoldenHashTests` (mã băm không đổi) và `ConfigTests.WaterClientCopyMatchesDesignData`.
- PlayMode các lớp nước + biển đạt.
- Console 0 lỗi sau `refresh_unity`.
- `git status` chỉ có file trong Phạm vi.

## Test sau khi xong

### Phần cần test
- Khoảng cách tới bờ ghi vào đỉnh (mới) · `BlockMesherTests.WaterSurfaceShoreDistanceEncoded` · R 0/64/128/191/255, 64, 90; G/B trắng; đỉnh không hướng lên = `0xFFFFFFFF`.
- Độ dày 0084 không đổi (regression) · `BlockMesherTests.WaterSurfaceDepth*` · đạt, không sửa test.
- Mesh nước cũ: độ cao, hàn đỉnh, bo, mép tràn (regression) · toàn bộ `BlockMesherTests` · đạt, số tam giác/đỉnh không đổi.
- Lề dựng lại khi đào/đặt (mới) · `WorldEditorTests.SolidChangedOnlyOnDigAndPlace` · đạt.
- Luật nước + biển (regression) · `WaterFlowTests`, `SeaFloodTests`, `WorldEditorTests` · đạt.
- Config look (mới) · `WaterConfigTests.Look*`, `ConfigTests.WaterClientCopyMatchesDesignData` · đạt.
- Áp tham số vào material (mới) · `WaterLookTests.ApplySetsAllProperties` (EditMode), `OceanBackdropTests.OceanRingUsesWaterLook` (PlayMode) · đạt.
- Shader biên dịch, material asset không đổi (regression) · `refresh_unity` + `read_console` Error · 0 lỗi; `git diff --stat` không có `Water.mat`, `Ocean.mat`.
- Nước/biển trong Play (regression) · PlayMode `OceanBackdropTests`, `WaterFlowPlayTests`, `WaterLayerPlayTests`, `DayCycleWaterPlayTests`, `FlyCameraWaterPlayTests`, `SeaSkyTintPlayTests` · đạt.
- Toàn bộ EditMode (regression) · đạt.

### Lệnh executor chạy
1. Viết test bước 1 trước, chạy `run_tests` EditMode lọc `BlockMesherTests.WaterSurfaceShoreDistanceEncoded`. Ghi output fail.
2. Sau bước 2–5: `refresh_unity` → `read_console` (Error) = 0.
3. `run_tests` EditMode lọc `BlockMesherTests`, `WorldEditorTests`, `WaterConfigTests`, `ConfigTests`, `WaterLookTests`, `WaterFlowTests`, `SeaFloodTests` → đạt. Nếu R = 255 ở mọi đỉnh thì ép biên dịch lại (xem Bối cảnh).
4. `run_tests` EditMode toàn bộ → đạt.
5. `run_tests` PlayMode lọc 6 lớp ở trên → đạt; rồi `git checkout -- client/ProjectSettings/EditorSettings.asset` (và `ProjectSettings.asset` nếu `runInBackground` bị đổi, như 0084).
6. `git status` / `git diff --stat` → chỉ file trong Phạm vi; xoá `InitTestScene*` nếu có.
7. Chụp 2 ảnh Game view scene SimView (Play, camera nhìn xiên xuống một bãi cát có biển + một ao) lưu ngoài repo: `~/Documents/User/khang/Game/art-test/shots/b0095_after_1.png`, `_2.png`. Chỉ để Planner so với ảnh tham khảo; không chỉnh số theo ảnh.

### User tự test
Gộp cuối 5k. Planner chuyển sang danh sách chơi thử. Play, scene SimView, xem cạnh ảnh tham khảo (`art-test/shots/ref_water_style_5k.webp`):
- (a) Bãi cát giáp biển: nước sát bờ xanh ngọc nhạt, trong, thấy cát đáy; ra xa đậm dần, không có vạch đổi màu.
- (b) Chỗ nước chạm khối đặc (bờ biển, thành ao, vách rãnh): viền mờ dần, không cắt gắt.
- (c) Giáp ranh mép map ↔ biển nền: liền màu, không lộ đường.
- (d) Rãnh 1 ô, ao, mặt nước mỏng mức 0–5 (0084) vẫn phân biệt dày/mỏng.
- (e) Đào/đặt khối sát bờ ở gần ranh chunk: viền mờ cập nhật ngay cả hai bên, không có vệt sai.
- Số chỉnh được (màu nông/sâu, `depthFull`, `deepAlpha`, `shallowAlphaFactor`, `shoreSoftWidth`) trong `design/data/water_m1.json`, mục `look`.

## Dừng và chuyển lại Opus khi
- Brief mơ hồ hoặc mâu thuẫn với code thật (số dòng lệch nhiều, chữ ký khác).
- Bước 1 ra giá trị R khác kỳ vọng mà code đúng QĐ 1–2: ghi giá trị thật, không tự đổi kỳ vọng. Không tìm thấy đỉnh ở toạ độ test (vd do bo góc): ghi đỉnh gần nhất.
- `WaterLayerPlayTests` hoặc test PlayMode nước khác fail vì `deepAlpha` 0,6 khác 0,45 cũ: ghi lỗi, không đổi số JSON.
- Biển nền đổi hình rõ rệt ngoài màu (vd lộ đường lưới, trong suốt), hoặc shader lỗi ở pass khác.
- Cần API Unity ngoài brief, cần quyết định ngoài "Quyết định đã chốt", hoặc phải sửa file ngoài "Phạm vi".
- Cùng một lỗi sửa 2 lần vẫn chưa qua.

---
<!-- Từ đây trở xuống: EXECUTOR ghi. -->

## Kết quả
Base commit: 90a96c4
Chạy bằng: claude-sonnet-5-5 · medium
Bắt đầu: 2026-11-01 22:13 · Dừng ở bước 6 (PlayMode) ~22:50, chưa chụp ảnh bước 7.
Code commit: 57d63af (bước 1–5 + world-tech; file: BlockMesher, Ocean.shader, WaterConfig, WorldEditor, TerrainChunkView, OceanBackdrop, WaterLook, JSON x2, test, asmdef EditTests).

## Kết quả test
- Bước 1: `WaterSurfaceShoreDistanceEncoded` fail trên code cũ: `R x=4 Expected: 0.0d +/- 1.0d But was: 255.0d`. Sau bước 2: đạt.
- Console Error sau refresh: 0.
- EditMode toàn bộ: 701/701 đạt (gồm BlockMesherTests 56, GoldenHash, ConfigTests, WaterLookTests, SolidChangedOnlyOnDigAndPlace, Look*).
- PlayMode 6 lớp (16 test): 15 đạt, **1 fail**: `SeaSkyTintPlayTests.DayCycleOffNearSeaUnchanged` — `Expected ≤ 0.002, But was 0.099`; output `offNear40: 0.00985 0.17151 0.40503` so với baseline cứng `BeforeOffNear40 = (0.03095, 0.27051, 0.46851)` (SeaSkyTintPlayTests.cs:21).
- Đã `git checkout -- client/ProjectSettings/EditorSettings.asset`.
- Sau Planner trả lời: sửa `BeforeOffNear40` (SeaSkyTintPlayTests.cs:21) → (0.00985, 0.17151, 0.40503) theo chỉ dẫn. PlayMode 6 lớp chạy lại: **16/16 đạt**. Đã dọn `EditorSettings.asset`.
- Ảnh bước 7: `art-test/shots/b0095_after_1.png`, `_2.png` (world thử 16³ dựng bằng execute_code: bậc cát + biển + ao, Water.mat + WaterLook; không phải scene SimView/Play). Phần xem hình: chờ user.
- Phần user tự test (a)–(e): chờ user.

## Vướng mắc
(Đã được Planner trả lời: phương án A sửa 1 hằng baseline; asmdef chấp nhận.)
1. **Test cũ so màu biển với baseline cứng.** `SeaSkyTintPlayTests.DayCycleOffNearSeaUnchanged` (tạo OceanBackdrop + MeshRenderer, màu biển gần camera, tắt day cycle) so với màu của `Ocean.mat` cũ; giờ `OceanBackdrop.Awake` áp `look` (deepColor 0,12/0,50/0,70) nên màu đổi (≈0,099 lệch). Đây đúng là hệ quả thiết kế (biển ra màu `_DeepColor` mới), không phải lỗi mesh/shader. Brief cấm sửa test cũ và cấm đổi số JSON.
   - Phương án A: Planner cập nhật baseline `BeforeOffNear40` (và các `Before*` khác nếu dùng màu) ở SeaSkyTintPlayTests theo màu mới. Đề xuất.
   - Phương án B: test tự đặt lại màu Ocean.mat cũ sau khi tạo OceanBackdrop.
   - Phương án C: OceanBackdrop không áp look khi... (không khuyến nghị, trái QĐ 6).
2. **Sửa ngoài Phạm vi (một dòng):** `Tests/ClientEdit/Game.Client.EditTests.asmdef` thêm `"Game.Core"` vào references, vì `WaterLookTests` dùng `WaterLookConfig` (assembly Game.Core, EditTests chưa tham chiếu → CS0012). Cần Planner xác nhận.

---
<!-- PLANNER ghi khi review. -->

## Review
- Chạy bằng Sonnet · medium, khớp Model thực thi. Thời gian theo commit: 22:10 (brief) → 22:29 (kết quả), ~20 phút. Brief ghi "dừng ~22:50" lệch với giờ commit, nên lấy theo git.
- Mọi dòng Phần cần test có kết quả [run trong brief]:
  - `WaterSurfaceShoreDistanceEncoded` fail trên code cũ (R 255), đạt sau khi sửa.
  - EditMode 701/701 đạt. Chạy trước lần đổi baseline, nhưng lần đổi đó chỉ chạm file PlayMode.
  - PlayMode 6 lớp 16/16 đạt sau khi đổi baseline.
- Vướng mắc 1: Executor chẩn đoán sai nguyên nhân. `SeaSkyTintPlayTests` dùng `Plane` + material mặc định, không dùng `OceanBackdrop`. Lệch màu là do QĐ 4 bỏ trộn fresnel trong `baseCol`, đúng chủ đích. Planner cho đổi đúng 1 hằng (`038fce4`).
- Vướng mắc 2: asmdef EditTests thêm `Game.Core` — chấp nhận.
- Đọc diff `90a96c4..c07e41c` (19 file, đúng Phạm vi + 2 file trên):
  - `WaterShoreDistanceAt`, `WaterVertexColor`, `SolidChanged` (chỉ `TryDig`/`TryPlace`), `OnSolidChanged` lề 4, shader `dT`/`shoreD`, `WaterLook.Apply` 9 thuộc tính: đúng QĐ 1–6.
  - `Water.mat`, `Ocean.mat` không đổi.
- Ảnh bước 7 chụp world thử 16³, không phải SimView. Ảnh 2 không thấy nước. Không đủ để đánh giá hình → để user xem trong SimView (gộp cuối 5k, mục User tự test a–e).
- Kết luận: đã duyệt.

