# Brief 0097 — World 5k phần 3: lấp lánh (đốm mặt trời có quầng + chấm lấp lánh nhỏ)

> Trạng thái: chờ làm
> Review: đầy đủ + user xem (gộp cuối 5k)
> Model thực thi: Sonnet · medium
> Cập nhật: 2026-11-01

## Mục tiêu
Phần 3 của task [5k](../tasks/m1-world-5k-nuoc-kieu-anh-tham-khao.md), theo ảnh tham khảo (ngoài repo: `~/Documents/User/khang/Game/art-test/shots/ref_water_style_5k.webp`):
- **Đốm mặt trời rõ hơn:** chỗ mặt nước phản chiếu mặt trời có một quầng sáng tròn, mềm, to hơn hẳn. Giữ thêm các đốm gắt nhỏ do gợn sóng như hiện nay.
- **Chấm lấp lánh nhỏ** rải trên mặt nước, mỗi chấm nhấp nháy theo nhịp riêng. Mờ dần khi ở xa camera, để chỗ xa không bị nhiễu răng cưa.
- Áp cho cả nước trong map lẫn biển nền (cùng shader `Game/Ocean`, cùng `WaterLook.Apply`).
- Không cần dữ liệu đỉnh mới. Tham số nằm trong `water_m1.json` mục `look` [số tạm].

## Bối cảnh cần đọc
**Planner đã chốt thiết kế (2026-11-01), lý do:**
- **Quầng** tính bằng **pháp tuyến phẳng** (0, 1, 0), không dùng pháp tuyến gợn. Nhờ vậy quầng là một đốm tròn liền, như ảnh tham khảo. Phần `spec` cũ (pháp tuyến gợn) vẫn giữ để có các đốm gắt nhỏ. Độ gắt của nó thành tham số `_GlintPower`, mặc định 160 = giá trị cũ.
- Không dựa vào bloom: camera game không bật post-processing. Quầng mềm thay cho hiệu ứng toả sáng.
- **Chấm lấp lánh:**
  - Chia mặt nước thành lưới ô theo toạ độ thế giới. Mỗi ô có xác suất có một chấm.
  - Tâm chấm đặt ngẫu nhiên nhưng cách mép ô ≥ bán kính, nên chỉ cần xét ô hiện tại, không xét ô kề. Mỗi pixel tốn 4 lần `HashInt`. Hiện frag đã dùng khoảng 28 lần, nên chi phí trên điện thoại tăng ít.
  - Chấm nhấp nháy theo pha băm riêng của từng ô. Chấm mờ theo khoảng cách, có tắt trong bóng. Chỉ hiện ở mặt trên, không hiện khi nhìn từ dưới nước lên.
- Nước trong suốt mỏng (alpha thấp) làm đốm sáng gần như mất. Vì vậy alpha được kéo lên theo độ sáng lấp lánh, giống cách bọt làm ở brief 0096.
- Mặc định trong shader: `_GlintHalo` = 0 và `_SparkleStrength` = 0. Material không qua `WaterLook.Apply` (material tạo trong test, `Ocean.mat` khi chạy ngoài scene) giữ hình như cũ. Riêng alpha có thay đổi khi `_Glint` > 0: bước kéo alpha lên tính cả `spec`. `Ocean.mat` là material đục nên không bị ảnh hưởng; các test PlayMode cũ đều đặt `_Glint` = 0.

**Code** (tại commit `fab9553`):
- `client/Assets/Game/Client/Shaders/Ocean.shader`:
  - chú thích đầu file `:1–9` (dòng 0096 ở `:9`);
  - Properties: `_Glint` `:18`, `_FoamStrength` `:34`;
  - `HLSLINCLUDE` + `CBUFFER_START(UnityPerMaterial)` `:44–56` (dòng bọt cuối `:54`);
  - `HashInt(int2 c)` `:124–132`;
  - `ShoreFoam` `:155–166`;
  - `frag` `:188–230`:
    - `Light light = …` `:203`, `half shadow` `:204`;
    - `half spec = pow(saturate(dot(n, normalize(light.direction + v))), 160.0) * _Glint * shadow;` `:208`;
    - `half3 c = diffuse + light.color * spec;` `:209`;
    - foam `:210–211`;
    - `float dist = distance(input.positionWS, GetCameraPositionWS());` `:212`;
    - alpha `:225–228`.
- `client/Assets/Game/Core/WaterConfig.cs`:
  - `public int configVersion = 6;` `:12`;
  - `WaterLookConfig` `:46–109`: `glint = 0.6f` `:56`, `foamStrength = 0.9f` `:67`;
  - `Validate()` `:69–93` (kiểm `foamLine + foamSoft` ở `:91–92`);
  - `CheckRange(float v, float min, float max, string name)` `:104`.
- `client/Assets/Game/Client/WaterLook.cs` `:9–31` (`Apply`, dòng cuối `m.SetFloat("_FoamStrength", c.foamStrength);` `:30`).
- JSON: `design/data/water_m1.json` và bản sao `client/Assets/Game/Client/Resources/Config/water_m1.json`. Hai file phải giống hệt nhau (`ConfigTests.WaterClientCopyMatchesDesignData`).
- Test:
  - `client/Assets/Game/Tests/Core/WaterConfigTests.cs`:
    - `Assert.AreEqual(6, cfg.configVersion);` `:18`;
    - `ConfigFileMatchesDefaults` `:28–32`: so `ToJson` mặc định C# với JSON, nên mặc định trong `WaterConfig.cs` phải khớp JSON;
    - `LookParsesAndValidates` `:53–63`, `LookRejectsOutOfRange` `:65–73`.
  - `client/Assets/Game/Tests/ClientEdit/WaterLookTests.cs`: `ApplySetsAllProperties` `:11–70` (`glint = 1.2f` `:28`), `FoamOffByDefault` `:72–78`, `AssertColor` `:80`.
  - Mẫu test render PlayMode: `client/Assets/Game/Tests/Client/WaterLayerPlayTests.cs`:
    - `SetUp` `:19–48`: tắt sương, `_DayCycleOn` = 0, tắt đèn khác, tạo đèn + camera;
    - `TearDown` `:50–58`;
    - `RenderLinear(Camera cam)` `:60–75`: RenderTexture `ARGBFloat` → `GetPixels`.
    - Namespace `Game.Client.Tests`, asmdef `Game.Client.Tests.asmdef` cùng thư mục.
- Docs (6000.4):
  - `_Time` (y = giây) [docs](https://docs.unity3d.com/6000.4/Documentation/Manual/SL-UnityShaderVariables.html);
  - `Material.SetFloat` [docs](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/Material.SetFloat.html), `Material.GetFloat` [docs](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/Material.GetFloat.html);
  - `Camera.Render` [docs](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/Camera.Render.html), `Texture2D.ReadPixels` [docs](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/Texture2D.ReadPixels.html): test PlayMode đã dùng ở `WaterLayerPlayTests`;
  - `GameObject.CreatePrimitive(PrimitiveType.Plane)`: plane 10×10 m, pháp tuyến hướng lên [docs](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/GameObject.CreatePrimitive.html).
  - `pow`, `sin`, `step`, `smoothstep`, `length`, `saturate` là hàm HLSL. `IS_FRONT_VFACE` là macro URP, shader đang dùng ở `:196`.
  - Không dùng API Unity nào khác.

## Quyết định đã chốt (không được đổi)
1. **Properties** (thêm sau `_FoamStrength` `:34`):
   ```
   _GlintPower("Glint power (brief 0097)", Float) = 160
   _GlintHalo("Sun halo", Float) = 0
   _GlintHaloPower("Sun halo power", Float) = 300
   _SparkleStrength("Sparkle strength", Float) = 0
   _SparkleScale("Sparkle cells per m", Float) = 1.5
   _SparkleDensity("Sparkle density", Float) = 0.35
   _SparkleSize("Sparkle radius (cell)", Float) = 0.15
   _SparkleSpeed("Sparkle speed", Float) = 3
   _SparkleFadeStart("Sparkle fade start (m)", Float) = 20
   _SparkleFadeEnd("Sparkle fade end (m)", Float) = 60
   ```
   CBUFFER, sau dòng bọt `:54`:
   ```hlsl
   float _GlintPower, _GlintHaloPower, _SparkleScale, _SparkleFadeStart, _SparkleFadeEnd;
   half _GlintHalo, _SparkleStrength, _SparkleDensity, _SparkleSize, _SparkleSpeed;
   ```
2. **Hàm mới** trong pass `ForwardLit`, đặt ngay sau `ShoreFoam`:
   ```hlsl
   // Chấm lấp lánh (brief 0097): lưới ô theo toạ độ thế giới (_SparkleScale ô/m); mỗi ô có chấm với xác suất _SparkleDensity,
   // tâm ngẫu nhiên cách mép ô ≥ bán kính (không cần xét ô kề), nhấp nháy theo pha riêng; mờ dần theo khoảng cách để chỗ xa không nhiễu.
   half Sparkle(float2 xz, float dist)
   {
       float2 p = xz * _SparkleScale;
       int2 cell = (int2)floor(p);
       half on = step(HashInt(cell), _SparkleDensity);
       float2 ctr = float2(HashInt(cell + int2(37, 11)), HashInt(cell + int2(-19, 53))) * (1.0 - 2.0 * _SparkleSize) + _SparkleSize;
       half r = length(p - floor(p) - ctr) / _SparkleSize;
       half dotV = 1.0h - smoothstep(0.25h, 1.0h, r);
       half tw = saturate(sin(_Time.y * _SparkleSpeed + HashInt(cell + int2(71, -29)) * 6.2831853));
       tw *= tw;
       tw *= tw;
       half fade = 1.0h - smoothstep(_SparkleFadeStart, _SparkleFadeEnd, dist);
       return on * dotV * tw * fade * _SparkleStrength;
   }
   ```
3. **`frag`**:
   - **Chuyển** dòng `float dist = distance(input.positionWS, GetCameraPositionWS());` (`:212`) lên ngay sau `half shadow = …` (`:204`). Nội dung giữ nguyên, các chỗ dùng `dist` phía dưới giữ nguyên.
   - Thay hai dòng `:208–209` bằng:
     ```hlsl
     float3 hv = normalize(light.direction + v);
     half spec = pow(saturate(dot(n, hv)), _GlintPower) * _Glint * shadow;
     // Quầng mặt trời (brief 0097): pháp tuyến phẳng → đốm tròn mềm, liền; spec gợn ở trên cho đốm gắt nhỏ.
     float3 nFlat = IS_FRONT_VFACE(face, float3(0, 1, 0), float3(0, -1, 0));
     half halo = pow(saturate(dot(nFlat, hv)), _GlintHaloPower) * _GlintHalo * shadow;
     half sparkle = Sparkle(input.positionWS.xz, dist) * shadow * IS_FRONT_VFACE(face, 1.0h, 0.0h);
     half glow = spec + halo + sparkle;
     half3 c = diffuse + light.color * glow;
     ```
   - Alpha: giữa `alpha *= lerp(_ThinAlpha, 1.0h, dT);` (`:226`) và `alpha = lerp(alpha, 1.0h, foam);` (`:227`), thêm:
     `alpha = lerp(alpha, 1.0h, saturate(glow)); // đốm sáng không bị mất ở chỗ nước mỏng (brief 0097)`
   - Đầu file, sau dòng `:9`, thêm:
     `// Brief 0097: lấp lánh — quầng mặt trời (pháp tuyến phẳng) + chấm lấp lánh theo lưới ô, mờ theo khoảng cách; _GlintHalo, _SparkleStrength mặc định 0.`
   - Không sửa pass `WaterDepthPrepass` / `DepthOnly`, `Varyings`, `vert`.
4. **JSON**: `configVersion` 6 → 7. Trong `look`:
   - đổi `"glint": 0.6` → `"glint": 1.0`;
   - thêm sau `foamStrength`, mọi số [số tạm], chưa chốt:
   ```json
   "glintPower": 160.0,
   "glintHalo": 0.8,
   "glintHaloPower": 300.0,
   "sparkleStrength": 1.5,
   "sparkleScale": 1.5,
   "sparkleDensity": 0.35,
   "sparkleSize": 0.15,
   "sparkleSpeed": 3.0,
   "sparkleFadeStart": 20.0,
   "sparkleFadeEnd": 60.0
   ```
5. **`WaterConfig.cs`**:
   - `configVersion = 7`, `glint = 1.0f`;
   - thêm 10 trường cùng tên, cùng giá trị như QĐ 4, đặt sau `foamStrength`;
   - `Validate()` thêm, trước kiểm `foamLine + foamSoft`, dùng `CheckRange`:
     - `glintPower` 8..1024; `glintHalo` 0..2; `glintHaloPower` 8..2048; `sparkleStrength` 0..4;
     - `sparkleScale` 0,1..8; `sparkleDensity` 0..1; `sparkleSize` 0,02..0,45; `sparkleSpeed` 0..20;
     - `sparkleFadeStart` 0..200; `sparkleFadeEnd` 0..300;
     - và: `if (!(sparkleFadeEnd > sparkleFadeStart)) throw new ArgumentException("sparkleFadeEnd must be > sparkleFadeStart.");`
   - Cập nhật `<summary>` của trường `look` (`:21`): "màu nông→sâu, viền bờ mờ, gợn, lấp lánh (brief 0095, 0097), bọt bờ (0096)".
6. **`WaterLook.Apply`**: thêm sau `:30`, đúng 10 dòng `m.SetFloat("_GlintPower", c.glintPower);` … `m.SetFloat("_SparkleFadeEnd", c.sparkleFadeEnd);`, tên thuộc tính theo QĐ 1.

## Phạm vi
- Được sửa:
  - `client/Assets/Game/Client/Shaders/Ocean.shader`: Properties, CBUFFER, pass `ForwardLit`, chú thích đầu file;
  - `client/Assets/Game/Core/WaterConfig.cs`, `client/Assets/Game/Client/WaterLook.cs`;
  - 2 file `water_m1.json`;
  - test: `Tests/Core/WaterConfigTests.cs`, `Tests/ClientEdit/WaterLookTests.cs`;
  - test mới `client/Assets/Game/Tests/Client/WaterGlintPlayTests.cs` (+ `.meta` do Unity tạo);
  - `design/04-tech/world-tech.md`: 1 câu ở dòng `:141`.
- Không được sửa: `Water.mat`, `Ocean.mat`, `BlockMesher.cs`, `OceanBackdrop.cs`, `TerrainChunkView.cs`, pass `WaterDepthPrepass`/`DepthOnly`, mọi test khác.

## Các bước
1. **Config (C# + JSON)** theo QĐ 4–5. Sửa `WaterConfigTests`:
   - `:18` → `7`;
   - `LookParsesAndValidates` thêm: `glint` = 1, `glintHalo` = 0,8, `sparkleScale` = 1,5, `sparkleFadeEnd` = 60 (sai số 1e-6);
   - `LookRejectsOutOfRange` thêm 3 trường hợp `ArgumentException`:
     - `glintHaloPower = 4`;
     - `sparkleSize = 0.5f`;
     - `sparkleFadeStart = 60f, sparkleFadeEnd = 60f`.
   - Xong khi: `run_tests` EditMode lọc `WaterConfigTests`, `ConfigTests` đạt.
2. **Shader** theo QĐ 1–3. Xong khi: `refresh_unity` → `read_console` (Error) = 0, không có cảnh báo shader `Game/Ocean`.
3. **`WaterLook`** theo QĐ 6. Sửa `WaterLookTests`:
   - `ApplySetsAllProperties` gán 10 trường mới bằng số khác mặc định và kiểm `GetFloat` từng thuộc tính:
     - `glintPower` 200; `glintHalo` 1,1; `glintHaloPower` 500; `sparkleStrength` 2;
     - `sparkleScale` 2,5; `sparkleDensity` 0,6; `sparkleSize` 0,2; `sparkleSpeed` 5;
     - `sparkleFadeStart` 10; `sparkleFadeEnd` 40.
   - Sửa `<summary>` dòng `:7` thành "9 + 11 (bọt, 0096) + 10 (lấp lánh, 0097)".
   - Test mới `GlintExtrasOffByDefault`: `new Material(Shader.Find("Game/Ocean"))` có `_GlintHalo` = 0, `_SparkleStrength` = 0, `_GlintPower` = 160. Làm theo mẫu `FoamOffByDefault`, có `try/finally DestroyImmediate`.
   - Xong khi: `run_tests` EditMode lọc `WaterLookTests` đạt.
4. **Test render PlayMode mới** `WaterGlintPlayTests.cs` (namespace `Game.Client.Tests`):
   - `const int Size = 256;`
   - **SetUp/TearDown** theo mẫu `WaterLayerPlayTests` `:19–58`:
     - tắt sương; `Shader.SetGlobalFloat("_DayCycleOn", 0f)`; tắt các `Light` đang bật, khôi phục ở TearDown;
     - đèn `Directional` tạo bằng `AddComponent<Light>()`, `rotation = Quaternion.Euler(90, 0, 0)` (mặt trời đỉnh đầu);
     - camera `clearFlags = SolidColor`, nền đen, `fieldOfView = 60`, near 0,1, far 50, `position = (0, 10, 0)`, `rotation = Euler(90, 0, 0)` (nhìn thẳng xuống);
     - `GameObject.CreatePrimitive(PrimitiveType.Plane)` tại gốc, `localScale = (3, 1, 3)`;
     - material `new Material(Shader.Find("Game/Ocean"))` (đục, mặc định), đặt `_RippleStrength` 0, `_Glint` 0, `_GlintHalo` 0, `_SparkleStrength` 0, `_SparkleSpeed` 0;
     - TearDown huỷ mọi object/material đã tạo.
   - Chép `RenderLinear` từ `WaterLayerPlayTests` `:60–75` (dùng `Size` của file mới). Hàm đếm: `static int CountBrighter(Color[] a, Color[] b, float th)` đếm pixel có `max(b.r − a.r, b.g − a.g, b.b − a.b) > th`.
   - Test 1 `SunHaloBrightensReflectionOnly`:
     - render `a` (halo 0); đặt `_GlintHalo` = 1, `_GlintHaloPower` = 300; render `b`;
     - pixel giữa `idx = (Size/2) * Size + Size/2`: `b − a` (kênh r) > 0,5;
     - pixel góc `idx = 5 * Size + 5`: |`b − a`| < 0,02.
   - Test 2 `SparkleAddsSmallDots`:
     - render `a` (sparkle 0);
     - đặt `_SparkleStrength` 2, `_SparkleScale` 1,5, `_SparkleDensity` 1, `_SparkleSize` 0,3, `_SparkleFadeStart` 20, `_SparkleFadeEnd` 60; render `b`;
     - `n = CountBrighter(a, b, 0.2f)`: `n > 200` và `n < Size * Size / 4`.
   - Test 3 `SparkleFadesWithDistance`: như test 2 nhưng `_SparkleFadeStart` 1, `_SparkleFadeEnd` 2 (camera cách 10 m) → `CountBrighter(a, b, 0.02f) == 0`.
   - Mỗi test `Debug.Log` số đo, assert message có số đo, cuối test `yield return null`.
   - Xong khi: `run_tests` PlayMode lọc `WaterGlintPlayTests` đạt 3/3.
5. **Kiểm tổng** theo "Lệnh executor chạy".
6. **`design/04-tech/world-tech.md:141`**: sau câu "Phần 2 (brief 0096): …" thêm nguyên văn:
   ` Phần 3 (brief 0097): đốm mặt trời thêm quầng tròn mềm (tính theo pháp tuyến phẳng), giữ đốm gắt nhỏ theo gợn; chấm lấp lánh nhỏ theo lưới ô, nhấp nháy theo pha riêng, mờ dần theo khoảng cách; tham số glint*/sparkle* trong water_m1.json "look" [số tạm], chưa chốt.`

## Tiêu chí xong
- `WaterConfigTests`, `ConfigTests`, `WaterLookTests` (gồm `GlintExtrasOffByDefault`) đạt.
- `WaterGlintPlayTests` 3/3 đạt, có ghi số đo (giữa/góc, `n`).
- EditMode toàn bộ đạt (gồm `GoldenHashTests`, `IncrementalRebuildTests`).
- PlayMode 6 lớp nước/biển cũ đạt, không đổi baseline.
- Console 0 lỗi. `git status` chỉ có file trong Phạm vi.

## Test sau khi xong

### Phần cần test
- Config lấp lánh (mới) · `WaterConfigTests.Look*`, `ConfigFileMatchesDefaults`, `ConfigParsesAndValidates`, `ConfigTests.WaterClientCopyMatchesDesignData` · đạt; `configVersion` 7.
- Áp tham số + mặc định tắt (mới) · `WaterLookTests` · đạt.
- Shader biên dịch (đổi) · `refresh_unity` + `read_console` Error · 0.
- Quầng mặt trời (mới) · `WaterGlintPlayTests.SunHaloBrightensReflectionOnly` · giữa > 0,5, góc < 0,02.
- Chấm lấp lánh + mờ xa (mới) · `WaterGlintPlayTests.SparkleAddsSmallDots`, `SparkleFadesWithDistance` · 200 < n < 16384; n = 0.
- Nước/biển trong Play (regression: alpha, chuyển `dist`) · PlayMode `OceanBackdropTests`, `WaterFlowPlayTests`, `WaterLayerPlayTests`, `DayCycleWaterPlayTests`, `FlyCameraWaterPlayTests`, `SeaSkyTintPlayTests` · đạt, không đổi baseline.
- Toàn bộ EditMode (regression) · đạt.

### Lệnh executor chạy
1. Sau bước 1: `run_tests` EditMode lọc `WaterConfigTests`, `ConfigTests` → đạt.
2. Sau bước 2–3: `refresh_unity` → `read_console` (Error) = 0 → `run_tests` EditMode lọc `WaterLookTests` → đạt.
3. Sau bước 4: `run_tests` PlayMode lọc `WaterGlintPlayTests` → 3/3. Ghi số đo từ log.
4. `run_tests` EditMode toàn bộ → đạt.
5. `run_tests` PlayMode lọc 6 lớp cũ ở trên → đạt.
6. Dọn: `git checkout -- client/ProjectSettings/EditorSettings.asset`, và `ProjectSettings.asset` nếu `runInBackground` đổi. Xoá `InitTestScene*` nếu có.
7. `git status` / `git diff --stat` → chỉ file trong Phạm vi.
8. Ảnh (không chặn brief): Play scene **SimView**, chụp 1 ảnh Game view → `~/Documents/User/khang/Game/art-test/shots/b0097_after_1.png`. Không đưa camera tới chỗ có nước được bằng công cụ có sẵn → chụp góc mặc định và ghi lý do. Không dựng world thử thay SimView.

### User tự test
Gộp cuối 5k. Play, scene SimView, ban ngày, xem cạnh ảnh tham khảo:
- (a) Chỗ mặt nước phản chiếu mặt trời: có quầng sáng tròn, mềm, rõ. Quanh quầng có đốm gắt nhỏ theo gợn. Xoay/di camera thì quầng di theo.
- (b) Chấm lấp lánh nhỏ rải trên biển và ao, nhấp nháy lúc sáng lúc tắt, không quá dày. Ở xa không nhiễu, không có dải sọc.
- (c) Trong bóng địa hình và ban đêm: lấp lánh yếu đi hoặc tắt.
- (d) Bờ, rãnh, ao nhỏ: đốm sáng không làm hiện viền cứng ở mép nước. Bọt bờ vẫn như 0096.
- (e) Lặn xuống nước nhìn lên: không có chấm lấp lánh.
- Số chỉnh được: `glint`, `glint*` và `sparkle*` trong `design/data/water_m1.json`, mục `look`. Ví dụ: chấm quá nhỏ → tăng `sparkleSize`; quá dày → giảm `sparkleDensity`; quầng quá to → tăng `glintHaloPower`.

## Dừng và chuyển lại Opus khi
- Brief mơ hồ hoặc mâu thuẫn với code thật (số dòng lệch nhiều, chữ ký khác).
- Số đo `WaterGlintPlayTests` khác kỳ vọng mà code đúng QĐ 1–3: ghi số thật, không tự đổi ngưỡng.
- Test PlayMode cũ fail: ghi lỗi, không tự đổi baseline.
- Shader lỗi, cảnh báo ở pass khác, hoặc vượt giới hạn trên target hiện tại.
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
