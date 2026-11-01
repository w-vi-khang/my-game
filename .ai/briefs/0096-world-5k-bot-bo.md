# Brief 0096 — World 5k phần 2: bọt bờ (vệt bọt song song bờ, trôi vào/ra; độ thoáng ghi vào đỉnh)

> Trạng thái: chờ làm
> Review: đầy đủ + user xem (gộp cuối 5k)
> Model thực thi: Sonnet · medium
> Cập nhật: 2026-11-01

## Mục tiêu
Phần 2 của task [5k](../tasks/m1-world-5k-nuoc-kieu-anh-tham-khao.md):
- Gần bờ có nhiều vệt bọt trắng song song với bờ, chạy dần vào bờ, cả dải trôi vào/ra theo thời gian.
- Vệt bọt có mép mềm và đứt quãng (theo nhiễu), không phải đường kẻ đều.
- **Không có bọt** ở chỗ nước hẹp (rãnh 1–2 ô, ao 3×3). Nếu không chặn, rãnh 1 ô sẽ trắng cả rãnh, vì mọi điểm đều cách bờ ≤ 0,5 ô.
- Biển nền không có bọt: nó luôn xa bờ.
- Tham số trong `water_m1.json` mục `look` [số tạm].

## Bối cảnh cần đọc
**Planner đã chốt thiết kế (2026-11-01), lý do:**
- Dùng lại khoảng cách tới bờ của brief 0095 (kênh R, `shoreD` 0..4 ô trong shader). Vệt bọt là các dải theo `shoreD`, nên tự song song với bờ.
- **Độ thoáng** = tỉ lệ ô **không đặc** trong cửa sổ quét cùng lớp, dùng chung vòng lặp với khoảng cách tới bờ. Ví dụ: rãnh 1 ô ≈ 0,11; ao 5×5 ≈ 0,31; bờ biển ≈ 0,5–0,9; biển khơi 1.
  - Đếm ô **không đặc** chứ không đếm ô nước. Nhờ vậy độ thoáng chỉ đổi khi khối đặc đổi, và lề dựng lại chunk 4 khi đào/đặt (`SolidChanged`, brief 0095) vẫn đủ. Nếu đếm ô nước thì phải đổi lề khi nước lan.
  - Ghi vào kênh G. Kênh B vẫn để dành cho phần 4 (vệt chảy). Phần 4 sẽ tự chốt cách ghi hướng chảy (B + kênh khác hoặc UV).
- `_FoamStrength` mặc định 0 trong shader. Material không qua `WaterLook.Apply` (`Ocean.mat` khi chạy ngoài scene, material tạo trong test) sẽ không có bọt. Biển nền có `shoreD` = 4 ≥ `foamWidth`, nên dù có Apply cũng không có bọt.

**Code** (tại commit `6385511`):
- `client/Assets/Game/World/BlockMesher.cs`:
  - `static uint WaterVertexColor(float depth, float shore)` `:53`;
  - `float WaterShoreDistanceAt(float3 p)` `:1727–1745` (cửa sổ `x0..x1`, `z0..z1`, `IsSolidMaterial(MaterialAt(...))`);
  - dòng gọi trong `AddWaterVertex` `:1775`.
- `client/Assets/Game/Client/Shaders/Ocean.shader`:
  - dòng chú thích 0095 `:8`; Properties (`_ShoreSoftWidth` `:22`); CBUFFER `:35–41`;
  - `Varyings` `:84–90`; `vert` `:95–104` (`o.shoreD` `:102`);
  - `ValueNoise` `:124`;
  - `frag`: `half3 diffuse = …` `:177`, `half3 c = diffuse + light.color * spec;` `:179`, alpha `:193–195`.
- `client/Assets/Game/Core/WaterConfig.cs`: `WaterLookConfig` `:46–85` (`CheckColor`, `CheckRange`). `client/Assets/Game/Client/WaterLook.cs` (`Apply`, 9 thuộc tính).
- JSON: `design/data/water_m1.json` + bản sao `client/Assets/Game/Client/Resources/Config/water_m1.json` (phải giống hệt).
- Test:
  - `Tests/World/BlockMesherTests.cs`: `StoneFloor16()` `:1050`, `WaterVertexColor(mesh, normal, pos)` `:1070`, `WaterSurfaceDepthEncodesLevel` `:1083` (kiểm `c & 0xFFFFFF == 0xFFFFFF` `:1101`; lớp 5 ở đó không có khối đặc nên G vẫn 255 → không sửa), `WaterSurfaceShoreDistanceEncoded` `:1128–1163` (kiểm `GB == 0xFFFF` ở `:1145`, `:1150`, `:1154` → **phải sửa**, xem bước 1).
  - `Tests/Core/WaterConfigTests.cs` (`configVersion` `:18`, `LookParsesAndValidates` `:53`, `LookRejectsOutOfRange` `:63`); `Tests/ClientEdit/WaterLookTests.cs` (`ApplySetsAllProperties` `:11`).
- Docs: `_Time` (y = thời gian, giây) là biến shader dựng sẵn của Unity, shader đang dùng ở `Waves()` [docs](https://docs.unity3d.com/6000.4/Documentation/Manual/SL-UnityShaderVariables.html). `Material.SetColor`/`SetFloat` như brief 0095 [docs](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/Material.SetColor.html). `frac`, `sin`, `smoothstep` là hàm HLSL. Không dùng API Unity nào khác.
- Lưu ý review 0079: nếu test mesh mới ra G = 255 ở mọi đỉnh sau khi sửa → ép Burst biên dịch lại (thêm/bỏ một dòng chú thích trong `BlockMesher.cs`).

## Quyết định đã chốt (không được đổi)
1. **Độ thoáng** tại đỉnh mặt trên `p`: dùng **cùng cửa sổ** với QĐ 1 brief 0095 (`x ∈ [floor(wp.x − S), floor(wp.x + S)]`, z tương tự, lớp `ly`).
   - `open = (số ô không đặc) / ((x1 − x0 + 1) · (z1 − z0 + 1))`. "Không đặc" = `!IsSolidMaterial(MaterialAt(...))`; ô ngoài map kẹp về biên như `MaterialAt`.
   - Gộp vào một hàm `void WaterShoreAt(float3 p, out float dist, out float open)` thay `WaterShoreDistanceAt`. Một vòng lặp; giá trị `dist` không đổi so với 0095.
2. **Mã hoá:** `WaterVertexColor(float depth, float shore, float open)`:
   - `R` = khoảng cách (như 0095), `G = round(255 · saturate(open))`, `B = 255`, `A` = độ dày.
   - Đỉnh không hướng lên giữ `0xFFFFFFFF` (G = 255).
3. **Shader** (pass `ForwardLit`):
   - `Varyings` thêm `half open : TEXCOORD3;`. `vert`: `o.open = lerp(1.0h, input.color.g, _VertexDepth);`.
   - Properties + CBUFFER, mặc định = số JSON ở QĐ 4, riêng `_FoamStrength` = 0:
     - `_FoamColor` (Color);
     - `_FoamWidth`, `_FoamBands`, `_FoamLine`, `_FoamSoft`, `_FoamSpeed`, `_FoamSwing`, `_FoamSwingSpeed`, `_FoamNoiseScale`, `_FoamOpenMin`, `_FoamStrength` (Float).
   - Hàm mới, đặt sau `Waves`:
     ```hlsl
     // Bọt bờ (brief 0096): dải song song bờ theo khoảng cách tới bờ, chạy vào bờ + cả dải trôi vào/ra; đứt quãng theo nhiễu; tắt ở chỗ hẹp (độ thoáng thấp).
     half ShoreFoam(float2 xz, half d, half open)
     {
         half env = 1.0h - smoothstep(_FoamWidth * 0.5h, _FoamWidth, d);
         env *= smoothstep(_FoamOpenMin, _FoamOpenMin + 0.15h, open);
         float t = _Time.y;
         float swing = sin(t * _FoamSwingSpeed) * _FoamSwing;
         half b = frac((d + swing) * _FoamBands / _FoamWidth + t * _FoamSpeed);
         half stripe = smoothstep(0.0h, _FoamSoft, b) * (1.0h - smoothstep(_FoamLine, _FoamLine + _FoamSoft, b));
         stripe *= smoothstep(0.35h, 0.65h, ValueNoise(xz * _FoamNoiseScale + t * 0.05));
         return saturate(stripe * env * _FoamStrength);
     }
     ```
   - `frag`:
     - tách `half3 lightTerm = light.color * ndl * shadow * 0.7 + SampleSH(nLit);` rồi `half3 diffuse = baseCol * lightTerm;`;
     - sau dòng `half3 c = …` (`:179`): `half foam = ShoreFoam(input.positionWS.xz, input.shoreD, input.open);` và `c = lerp(c, _FoamColor.rgb * lightTerm, foam);`;
     - alpha: giữa dòng `alpha *= lerp(_ThinAlpha, …)` và dòng `alpha *= smoothstep(0.0h, _ShoreSoftWidth, …)` thêm `alpha = lerp(alpha, 1.0h, foam);`. Viền bờ mờ vẫn nhân sau cùng, nên bọt sát mép cũng mờ dần.
   - Đầu file thêm: `// Brief 0096: bọt bờ — dải theo khoảng cách tới bờ (R), tắt ở chỗ hẹp theo độ thoáng (G màu đỉnh); _FoamStrength mặc định 0.`
4. **JSON** `configVersion` 5 → 6. Thêm vào `look` (mọi số [số tạm], chưa chốt):
   ```json
   "foamColor": [1.0, 1.0, 1.0],
   "foamWidth": 2.0,
   "foamBands": 3.0,
   "foamLine": 0.3,
   "foamSoft": 0.15,
   "foamSpeed": 0.25,
   "foamSwing": 0.3,
   "foamSwingSpeed": 0.8,
   "foamNoiseScale": 1.2,
   "foamOpenMin": 0.2,
   "foamStrength": 0.9
   ```
   - Khoảng hợp lệ (`CheckColor`/`CheckRange`):
     - `foamColor` như màu;
     - `foamWidth` 0,5..4; `foamBands` 1..8; `foamLine` 0,05..0,9; `foamSoft` 0,01..0,5; `foamSpeed` 0..2; `foamSwing` 0..1; `foamSwingSpeed` 0..4; `foamNoiseScale` 0,1..4; `foamOpenMin` 0..0,9; `foamStrength` 0..1;
     - thêm: `foamLine + foamSoft ≤ 1` (message có `foamLine`).
   - `WaterLook.Apply` set thêm 11 thuộc tính tương ứng (`_FoamColor` alpha 1).

## Phạm vi
- Được sửa:
  - `client/Assets/Game/World/BlockMesher.cs`;
  - `client/Assets/Game/Client/Shaders/Ocean.shader` (Properties, CBUFFER, pass `ForwardLit`);
  - `client/Assets/Game/Core/WaterConfig.cs`, `client/Assets/Game/Client/WaterLook.cs`;
  - 2 file `water_m1.json`;
  - test: `Tests/World/BlockMesherTests.cs` (test mới + đúng 3 dòng `GB` của `WaterSurfaceShoreDistanceEncoded`), `Tests/Core/WaterConfigTests.cs`, `Tests/ClientEdit/WaterLookTests.cs`;
  - `design/04-tech/world-tech.md` (1 câu, bước 6).
- Không được sửa: `Water.mat`, `Ocean.mat`, pass `WaterDepthPrepass`/`DepthOnly`, `WorldEditor.cs`, `TerrainChunkView.cs`, `OceanBackdrop.cs`, mọi test khác.

## Các bước
1. **Test mesh trước** (phải fail trên code cũ):
   - Test mới `BlockMesherTests.WaterSurfaceOpennessEncoded`:
     - Dựng: `StoneFloor16()`; lớp y = 5 toàn đá `Cell.Make(BlockMaterial.Stone)` ở mọi (x, z), trừ rãnh x = 8, z 0..15 là nguồn nước `Cell.Make(BlockMaterial.Water, Cell.MaxWaterLevel)`. `Build(w, 0, 0, 0, true, 5)`.
     - Đỉnh up `(8, 5.9, 7)` và `(9, 5.9, 7)`: G (`(c >> 8) & 0xFF`) = 28 ±1 (9/81). B = 255. R = 0 ±1 (sát vách).
   - Sửa `WaterSurfaceShoreDistanceEncoded`: thay 3 chỗ kiểm `(c >> 8) & 0xFFFF == 0xFFFF` bằng B (`(c >> 16) & 0xFF`) = 255 và G:
     - hàng z = 4, x = 4..8: G = 233, 233, 233, 233, 255 (±1) (vách x = 3, z 2..8 chiếm 7/81 ô cửa sổ, trừ x = 8);
     - `(11, 5.9, 12)` và `(12, 5.9, 12)`: G = 252 ±1 (cột đá chiếm 1/81).
   - Xong khi: cả hai test fail trên code cũ (G = 255 / 255 ≠ 233). Ghi output fail.
2. **`BlockMesher.cs`** theo QĐ 1–2. Khoá hàn giữ nguyên. Cập nhật `<summary>` của `WaterVertexColor` (G = độ thoáng).
   - Xong khi: test bước 1 + `WaterSurfaceDepth*` đạt.
3. **Config** theo QĐ 4. `WaterConfigTests`:
   - `:18` → 6;
   - `LookParsesAndValidates` thêm `foamWidth` = 2, `foamBands` = 3, `foamOpenMin` = 0,2;
   - `LookRejectsOutOfRange` thêm 2 trường hợp `foamWidth = 5` và `foamLine = 0.9, foamSoft = 0.2` → `ArgumentException`.
4. **Shader + `WaterLook`** theo QĐ 3–4. `WaterLookTests`:
   - `ApplySetsAllProperties` kiểm thêm 11 thuộc tính bọt;
   - test mới `FoamOffByDefault`: `new Material(Shader.Find("Game/Ocean")).GetFloat("_FoamStrength") == 0`.
   - Xong khi: `refresh_unity` → `read_console` Error = 0.
5. **Kiểm tổng** theo "Lệnh executor chạy".
6. **`design/04-tech/world-tech.md:141`**: sau câu "Phần 1 (brief 0095) …" thêm: ` Phần 2 (brief 0096): bọt bờ là các dải theo khoảng cách tới bờ, chạy vào bờ + trôi vào/ra, đứt quãng theo nhiễu; độ thoáng (tỉ lệ ô không đặc trong 9×9 cùng lớp) ghi vào kênh G, chỗ hẹp (rãnh, ao nhỏ) không có bọt; tham số foam* trong water_m1.json "look" [số tạm], chưa chốt.`

## Tiêu chí xong
- `WaterSurfaceOpennessEncoded` + `WaterSurfaceShoreDistanceEncoded` (đã sửa) fail trên code cũ, có ghi output, và đạt sau khi sửa.
- Test config/look mới đạt.
- EditMode toàn bộ đạt (gồm `GoldenHashTests`, `ConfigTests.WaterClientCopyMatchesDesignData`).
- PlayMode 6 lớp nước/biển đạt; `SeaSkyTintPlayTests` không phải đo lại (biển không có bọt).
- Console 0 lỗi. `git status` chỉ có file trong Phạm vi.

## Test sau khi xong

### Phần cần test
- Độ thoáng ghi vào đỉnh (mới) · `BlockMesherTests.WaterSurfaceOpennessEncoded`, `WaterSurfaceShoreDistanceEncoded` · G 28 / 233 / 255 / 252; R như 0095; B 255; đỉnh không hướng lên `0xFFFFFFFF`.
- Độ dày + mesh nước cũ (regression) · toàn bộ `BlockMesherTests` · đạt, số tam giác/đỉnh không đổi.
- Config foam (mới) · `WaterConfigTests.Look*`, `ConfigTests.WaterClientCopyMatchesDesignData` · đạt.
- Áp tham số + bọt tắt mặc định (mới) · `WaterLookTests` · đạt.
- Shader biên dịch (regression) · `refresh_unity` + `read_console` Error · 0.
- Nước/biển trong Play (regression) · PlayMode `OceanBackdropTests`, `WaterFlowPlayTests`, `WaterLayerPlayTests`, `DayCycleWaterPlayTests`, `FlyCameraWaterPlayTests`, `SeaSkyTintPlayTests` · đạt, không đổi baseline.
- Toàn bộ EditMode (regression) · đạt.

### Lệnh executor chạy
1. Viết/sửa test bước 1 trước → `run_tests` EditMode lọc `BlockMesherTests.WaterSurfaceOpenness`, `BlockMesherTests.WaterSurfaceShoreDistance` → ghi output fail.
2. Sau bước 2–4: `refresh_unity` → `read_console` (Error) = 0.
3. `run_tests` EditMode lọc `BlockMesherTests`, `WaterConfigTests`, `ConfigTests`, `WaterLookTests` → đạt.
4. `run_tests` EditMode toàn bộ → đạt.
5. `run_tests` PlayMode lọc 6 lớp ở trên → đạt; rồi `git checkout -- client/ProjectSettings/EditorSettings.asset` (và `ProjectSettings.asset` nếu `runInBackground` đổi).
6. `git status` / `git diff --stat` → chỉ file trong Phạm vi; xoá `InitTestScene*` nếu có.
7. Ảnh (không chặn brief): Play scene **SimView**, chụp 2 ảnh Game view ở chỗ bãi cát giáp biển và cạnh một ao. Lưu `~/Documents/User/khang/Game/art-test/shots/b0096_after_1.png`, `_2.png`. Không đưa camera tới bờ được bằng công cụ có sẵn → chụp góc mặc định và ghi lý do. Không dựng world thử thay SimView.

### User tự test
Gộp cuối 5k. Play, scene SimView, xem cạnh ảnh tham khảo:
- (a) Bờ biển: 2–3 vệt bọt trắng song song bờ, chạy vào bờ, cả dải trôi vào/ra; mép vệt mềm, đứt quãng.
- (b) Rãnh 1 ô / rãnh 2 ô / ao nhỏ: không bọt hoặc rất ít; ao rộng ≥ 5×5: có bọt ven thành.
- (c) Ranh chunk: vệt bọt liền, không đứt ở đường thẳng. Đào/đặt khối sát bờ: bọt cập nhật hai bên.
- (d) Mép map ↔ biển nền: không có bọt, không lộ đường.
- Số chỉnh được: các trường `foam*` trong `design/data/water_m1.json`, mục `look`.

## Dừng và chuyển lại Opus khi
- Brief mơ hồ hoặc mâu thuẫn với code thật (số dòng lệch nhiều, chữ ký khác).
- G ở bước 1 khác kỳ vọng mà code đúng QĐ 1–2: ghi giá trị thật, không tự đổi kỳ vọng.
- Test PlayMode cũ (gồm `SeaSkyTintPlayTests`) fail: ghi lỗi, không tự đổi baseline.
- Shader lỗi ở pass khác, hoặc vượt giới hạn interpolator trên target hiện tại.
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
