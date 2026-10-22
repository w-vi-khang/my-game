# Brief 0034 — Biển nhuộm màu trời theo buổi, sương chuyển mềm ở chân trời

> Trạng thái: chờ review
> Review: đầy đủ + user xem
> Model thực thi: Sonnet · medium
> Cập nhật: 2026-10-22 12:10

<!-- Planner (Opus) viết, Executor (Sonnet) làm theo ở PHIÊN KHÁC — executor không có lịch sử chat, không được tự quyết.
Trạng thái: chờ làm → đang làm → chờ review → đã duyệt; hoặc → cần Opus (executor dừng, hỏi ở mục Vướng mắc).
Review: nhẹ (đọc mục Kết quả) | đầy đủ (đọc cả diff) | user xem (có phần user tự kiểm bằng mắt/chơi thử) — có thể ghép "đầy đủ + user xem".
Model thực thi: BẮT BUỘC, đúng dạng `<Model> · <effort>` — Model: Opus | Sonnet | Haiku | Fable; effort: low | medium | high | xhigh | max (chọn theo AGENTS.md §10.2).
Executor kiểm tra model + effort của phiên TRƯỚC khi làm (scripts/brief_check.py); lệch hoặc không xác định được → cảnh báo user và DỪNG.
File được commit. Tự kiểm trước khi giao (AGENTS.md §10.3): executor có phải hỏi gì không? còn chỗ nào bỏ ngỏ 2 phương án? -->

## Mục tiêu
Làm theo [task contract](../tasks/m1-bien-nhuom-mau-troi.md). Khi nhìn ra biển ở mọi buổi, mặt biển ở xa chuyển dần sang màu chân trời, rồi tan vào dải sương và bầu trời. Không còn chỗ gãy (mép "màn sương") ở mốc 120 m. Giữ nguyên far clip 128 và khoảng sương 64–120.

Ước lượng 0,5 ngày-người. Ghi giờ bắt đầu/kết thúc (theo `date`) vào mục Kết quả.

## Bối cảnh cần đọc
Đường dẫn tính từ `client/Assets/Game/`. Số dòng theo commit `e15b86a`.

**Nguyên nhân** (Planner đã đo, xem task contract mục "Nguyên nhân"):
- Ở tầm 64–120 m, mặt biển dùng màu cố định của material (xám ~110,98,96), không đổi theo buổi.
- Sương tuyến tính phủ kín đúng ở 120 m nhưng độ dốc tại đó khác 0, nên trên màn hình có chỗ gãy.

**`Client/Shaders/Ocean.shader`** (shader `Game/Ocean`, dùng cho `OceanBackdrop` và `Water.mat` của chunk):
- `:24-32` — `HLSLINCLUDE` có `CBUFFER_START(UnityPerMaterial)` (`_DeepColor, _ShallowColor, _RippleScale, _RippleStrength, _Glint, _Alpha`).
- `:48` — đã include `Fog.hlsl`.
- `:94-98` — `float Waves(float2 p)`.
- `:100` — chữ ký `half4 frag(Varyings input, FRONT_FACE_TYPE face : FRONT_FACE_SEMANTIC) : SV_Target`.
- `:108` — `n = IS_FRONT_VFACE(face, n, -n);`. Macro `IS_FRONT_VFACE(face, A, B)` trả A cho mặt trước (mặt trên, camera trên nước) và B cho mặt sau (camera dưới nước, `Water.mat` có `Cull Off`).
- `:110-112`, nguyên văn:
  ```hlsl
                float3 v = normalize(GetWorldSpaceViewDir(input.positionWS));
                half fres = pow(1.0 - saturate(dot(v, n)), 3.0);
                half3 baseCol = lerp(_DeepColor.rgb, _ShallowColor.rgb, saturate(0.25 + fres));
  ```
- `:119-129`, nguyên văn:
  ```hlsl
                half3 c = diffuse + light.color * spec;
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
  ```

**`Client/Resources/Shaders/Terrain.shader`** (shader `Game/Terrain`):
- `:41-42` — include `Fog.hlsl`, `Lighting.hlsl`.
- `:13` — property `_PatternStrength("Pattern strength", Range(0, 1)) = 1`.
- `:152`, nguyên văn:
  ```hlsl
                c = MixFog(c, ComputeFogFactorZ0ToFar(distance(input.positionWS, GetCameraPositionWS())));
  ```

**URP 17.4.0** (`client/Library/PackageCache/com.unity.render-pipelines.universal@13e5115b98bf/ShaderLibrary/ShaderVariablesFunctions.hlsl` [repo]):
- `:315` — `real ComputeFogFactorZ0ToFar(float z)`. Với sương tuyến tính, hàm trả `saturate(z * unity_FogParams.z + unity_FogParams.w)`, tức độ rõ: 1 ở gần (≤ fogStart), 0 ở xa (≥ fogEnd).
- `:452` `MixFogColor`, `:513` `half3 MixFog(half3 fragColor, half fogFactor)`. Với sương tuyến tính: `frag * f + fogColor * (1 - f)`.
- Biến `FOG_LINEAR` là dynamic branch (`Fog.hlsl:6`), đã được dùng trong `Ocean.shader:125-128`.

**Biến global của chu kỳ ngày** (do `Client/DayCycleView.cs` ghi, brief 0023/0033):
- `_DayCycleOn` (float: 1 khi có `DayCycleView`, mặc định 0).
- `_DayHorizon` (màu chân trời dạng **linear**). Trùng `RenderSettings.fogColor.linear`, tức `unity_FogColor` mà sương dùng.
- Cách khai báo mẫu ngoài CBUFFER: `Client/Shaders/SkyClouds.shader:37-39`.

**Test mẫu:**
- `Tests/Client/HorizonFogMatchPlayTests.cs`:
  - Setup skybox / sương / đèn / camera / `DayCycleView`: `:24-65`.
  - TearDown khôi phục `RenderSettings`: `:67-82`.
  - Helper `RenderPixels`: `:84-99`.
  - Giờ được tính như sau: `now = 12 * H` ứng với pha 0,5 (trưa); `0` ứng với nửa đêm; `6 * H` ứng với pha 0,25 (khoá Sunrise, bình minh); `18 * H` ứng với pha 0,75 (khoá Sunrise, hoàng hôn) (`Client/DaySky.cs:75-79`).

**API Unity (6000.4)** — ngoài các API đã có trong `HorizonFogMatchPlayTests`:
- `new RenderTexture(int width, int height, int depth, RenderTextureFormat format)` — [docs](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/RenderTexture-ctor.html). Dùng định dạng `RenderTextureFormat.ARGBFloat` ([docs](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/RenderTextureFormat.ARGBFloat.html)) để đọc màu linear dạng float, không bị lượng tử 8 bit.
- `new Texture2D(w, h, TextureFormat.RGBAFloat, false)` — [docs](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/TextureFormat.RGBAFloat.html).
- `Texture2D.ReadPixels` — [docs](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/Texture2D.ReadPixels.html).
- `public Color[] GetPixels(int miplevel = 0)` — [docs](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/Texture2D.GetPixels.html).
- `Material.SetFloat` — [docs](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/Material.SetFloat.html).
- `GameObject.CreatePrimitive(PrimitiveType.Plane)` cho mặt phẳng 10×10 m, khi scale (60, 1, 60) thành 600×600 m. `Color` có indexer `[0..3]`.

## Quyết định đã chốt (không được đổi)
1. **Biển nhuộm màu trời** (user 2026-10-22). Trong `Ocean.shader`, chỉ ở **mặt trên**, màu đã chiếu sáng `c` được lerp về `_DayHorizon.rgb` **trước** `MixFog`, với hệ số `skyT = saturate(smoothstep(32, 120, dist) + 0.5 * fres)` nhân `_DayCycleOn`. Dùng hằng trong shader, không thêm property hay sửa material.
   - Lý do chọn hằng: ở 90 m `skyT` ≈ 0,9; ở 50 m ≈ 0,17; nhìn gần từ trên xuống ≈ 0. Nhờ vậy biển chuyển dần, còn nước gần vẫn giữ màu.
2. **Sương chuyển mềm** (user 2026-10-22). Cả `Ocean.shader` và `Terrain.shader` dùng hàm `SoftFogFactor` giống hệt nhau. Hàm này nắn hệ số sương tuyến tính `f` thành `f*f*(3-2f)` (smoothstep), áp ở mọi lúc sương tuyến tính bật.
   - Các phần **không** đổi: `RenderSettings.fogStartDistance/EndDistance`, far clip, `fogColor`.
   - Hệ quả đã chấp nhận: sương dưới nước (`UnderwaterView`, 0–40 m) cũng mềm theo, vì cùng shader.
   - Độ trong của `Water.mat` (`fogVis`) dùng hệ số đã nắn.
3. `_DayCycleOn = 0` thì không nhuộm (`skyT * 0`). Mặt sau (camera dưới nước) không nhuộm.
4. Không sửa: `DayCycleView.cs`, `DaySky.cs`, `SkyClouds.shader`, `UnderwaterView.cs`, `WorldEdgeFog.cs`, material, scene, `GhostBlock.shader`, `SelectionOverlay.shader`.
5. **Không đo FPS trên điện thoại.**
   - Lý do: mỗi fragment chỉ thêm 2 smoothstep, 1 lerp và vài phép nhân, không có texture fetch hay vòng lặp. Đo thì tốn một lần build APK.
   - Nếu user thấy giật khi xem → tách việc đo.
6. Ngưỡng kiểm (đo bằng float linear, so với số đo trước khi sửa trên chính máy này):
   - Lệch màu sương ở 90 m ≤ 0,5 × trước.
   - Độ gãy ở 120 m ≤ 0,5 × trước (biển: 4 buổi; địa hình: trưa).
   - Nhìn gần hoặc dưới nước: bật và tắt `_DayCycleOn` lệch ≤ 0,01 mỗi kênh.
   - `_DayCycleOn = 0` ở 40 m: lệch so với trước ≤ 0,002 mỗi kênh.

## Phạm vi
- Được sửa/tạo:
  - `client/Assets/Game/Client/Shaders/Ocean.shader`
  - `client/Assets/Game/Client/Resources/Shaders/Terrain.shader`
  - `client/Assets/Game/Tests/Client/SeaSkyTintPlayTests.cs` (mới, kèm `.meta` Unity tự tạo)
  - Các mục Kết quả / Kết quả test / Vướng mắc của brief này.
- Không được sửa: mọi file khác (gồm `HorizonFogMatchPlayTests.cs`, `design/`, roadmap).

## Các bước
1. **Tạo `Tests/Client/SeaSkyTintPlayTests.cs`** (chưa sửa shader).
   - Namespace `Game.Client.Tests`, lớp `SeaSkyTintPlayTests`.
   - Summary: `/// <summary>Task m1-bien-nhuom-mau-troi: mặt biển xa nhuộm màu chân trời theo buổi, sương không gãy ở mốc fogEnd; nhìn gần / dưới nước / không có DayCycleView giữ như cũ.</summary>`.

   **Hằng và trường:**
   - `const int H = 3_600_000; const int Size = 256; const float CamH = 26f, Pitch = 13f;`
   - `static readonly (string name, long ms)[] Phases = { ("binh-minh", 6L * H), ("ngay", 12L * H), ("hoang-hon", 18L * H), ("dem", 0L) };`
   - Mốc đo trước khi sửa, điền ở bước 2:
     ```csharp
     // Mốc đo trước khi sửa shader (base e15b86a, bước 2 brief 0034). BaselineFilled = false → test chỉ ghi log.
     static readonly bool BaselineFilled = false;
     static readonly float[] BeforeDev90 = { 0f, 0f, 0f, 0f };      // theo thứ tự Phases
     static readonly float[] BeforeKinkSea = { 0f, 0f, 0f, 0f };    // theo thứ tự Phases
     const float BeforeKinkTerrain = 0f;                           // trưa
     static readonly Color BeforeOffNear40 = new Color(0f, 0f, 0f, 1f);
     ```
   - Trường đối tượng: `GameObject lightGo, camGo, sea, ground; Material skyMat, seaMat, groundMat; DayCycleView view; long now;`. Thêm các trường lưu `RenderSettings` giống `HorizonFogMatchPlayTests:18-22`.

   **`[UnitySetUp] SetUp()`:**
   - Lưu `RenderSettings` như `HorizonFogMatchPlayTests:28-33`.
   - Skybox `Game/SkyClouds`; `RenderSettings.fog = true; fogMode = FogMode.Linear; fogStartDistance = 64f; fogEndDistance = 120f;`; `Shader.SetGlobalFloat("_EdgeFogBlend", 0f);`.
   - Đèn directional giống `:43-44`.
   - Camera giống `:46-57`, nhưng `position = new Vector3(0, CamH, 0)` và `rotation = Quaternion.Euler(Pitch, 0, 0)`. `now = 12 * H`.
   - `sea = GameObject.CreatePrimitive(PrimitiveType.Plane)`, `position = Vector3.zero`, `localScale = new Vector3(60, 1, 60)`. `seaMat = new Material(Shader.Find("Game/Ocean"))` với `seaMat.SetFloat("_RippleStrength", 0f); seaMat.SetFloat("_Glint", 0f); seaMat.SetFloat("_Cull", 0f);` (mặt phẳng, không lấp lánh, vẽ cả mặt dưới như `Water.mat`). Gán `sharedMaterial`.
   - `ground` cũng là Plane, cùng vị trí và scale. `groundMat = new Material(Shader.Find("Game/Terrain"))` với `groundMat.SetFloat("_PatternStrength", 0f)`. Gán `sharedMaterial`, rồi `ground.SetActive(false)`.
   - `yield return null;`

   **`[UnityTearDown]`:** `Object.Destroy` camera, đèn, `sea`, `ground`, ba material. Khôi phục `RenderSettings`. `Shader.SetGlobalFloat("_DayCycleOn", 0f);` rồi `yield return null`.

   **Hàm phụ** (chép nguyên văn):
   ```csharp
        // Ảnh float linear (không lượng tử 8 bit); điểm (x, y) = px[y * Size + x], y = 0 là hàng dưới cùng.
        static Color[] RenderLinear(Camera cam)
        {
            var rt = new RenderTexture(Size, Size, 24, RenderTextureFormat.ARGBFloat);
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(Size, Size, TextureFormat.RGBAFloat, false);
            tex.ReadPixels(new Rect(0, 0, Size, Size), 0, 0);
            tex.Apply();
            var px = tex.GetPixels();
            cam.targetTexture = null;
            RenderTexture.active = null;
            Object.Destroy(rt);
            Object.Destroy(tex);
            return px;
        }

        // Hàng ảnh nơi tia ở cột giữa chạm mặt phẳng y = 0 ở khoảng cách d (camera cao CamH, cúi Pitch độ, FOV dọc 60).
        static int RowAt(float d)
        {
            float a = Mathf.Asin(CamH / d) - Pitch * Mathf.Deg2Rad; // góc dưới trục camera
            return Mathf.RoundToInt(Size * 0.5f - Size * 0.5f * Mathf.Tan(a) / Mathf.Tan(30f * Mathf.Deg2Rad) - 0.5f);
        }

        // Trung bình một kênh trên cột 120..135 của hàng y.
        static float Avg(Color[] px, int y, int ch)
        {
            float s = 0f;
            for (int x = 120; x < 136; x++) s += px[y * Size + x][ch];
            return s / 16f;
        }

        static Color AvgColor(Color[] px, int y) => new Color(Avg(px, y, 0), Avg(px, y, 1), Avg(px, y, 2), 1f);

        // Lệch lớn nhất theo kênh R, G, B.
        static float MaxDiff(Color a, Color b) =>
            Mathf.Max(Mathf.Abs(a.r - b.r), Mathf.Abs(a.g - b.g), Mathf.Abs(a.b - b.b));

        // Độ gãy độ dốc tại hàng ứng với khoảng cách d: |dốc 3 hàng phía gần − dốc 3 hàng phía xa|, lớn nhất theo kênh.
        static float Kink(Color[] px, float d)
        {
            int r = RowAt(d);
            float best = 0f;
            for (int ch = 0; ch < 3; ch++)
            {
                float near = (Avg(px, r, ch) - Avg(px, r - 3, ch)) / 3f;
                float far = (Avg(px, r + 3, ch) - Avg(px, r, ch)) / 3f;
                best = Mathf.Max(best, Mathf.Abs(near - far));
            }
            return best;
        }

        Camera Cam => camGo.GetComponent<Camera>();
   ```

   **Các test** (mỗi số đo đều `Debug.Log` với tiền tố `[0034]`, rồi chỉ assert khi `BaselineFilled`):
   - `[UnityTest] SeaAt90mBlendsTowardFogColor()`
     - Lặp `i` qua `Phases`: `now = Phases[i].ms; view.Apply();`. Lấy `px = RenderLinear(Cam)`.
     - `dev = MaxDiff(AvgColor(px, RowAt(90f)), RenderSettings.fogColor.linear)`.
     - Log: `$"[0034] dev90 {Phases[i].name}: {dev:F5}"`.
     - Khi `BaselineFilled`: `Assert.LessOrEqual(dev, 0.5f * BeforeDev90[i], Phases[i].name)`.
     - Cuối test: `yield return null`.
   - `[UnityTest] SeaFogHasNoKinkAtFogEnd()`
     - Lặp như trên. `k = Kink(px, 120f)`, log `$"[0034] kinkSea {name}: {k:F5}"`.
     - Khi `BaselineFilled`: `Assert.LessOrEqual(k, 0.5f * BeforeKinkSea[i], name)`.
   - `[UnityTest] TerrainFogHasNoKinkAtFogEnd()`
     - `sea.SetActive(false); ground.SetActive(true); now = 12 * H; view.Apply();` rồi `px = RenderLinear(Cam)`.
     - `k = Kink(px, 120f)`, log `$"[0034] kinkTerrain: {k:F5}"`.
     - Khi `BaselineFilled`: `Assert.LessOrEqual(k, 0.5f * BeforeKinkTerrain)`.
   - `[UnityTest] DayCycleOffNearSeaUnchanged()`
     - `now = 12 * H; view.Apply(); Shader.SetGlobalFloat("_DayCycleOn", 0f);` rồi `px = RenderLinear(Cam)`.
     - `c = AvgColor(px, RowAt(40f))`, log `$"[0034] offNear40: {c.r:F5} {c.g:F5} {c.b:F5}"`.
     - Khi `BaselineFilled`: `Assert.LessOrEqual(MaxDiff(c, BeforeOffNear40), 0.002f)`.
   - `[UnityTest] CloseTopViewUnchangedByDayCycle()`
     - `camGo.transform.SetPositionAndRotation(new Vector3(0, 3, 0), Quaternion.Euler(60, 0, 0)); now = 12 * H; view.Apply();`
     - `on = AvgColor(RenderLinear(Cam), 128)`. Sau đó `Shader.SetGlobalFloat("_DayCycleOn", 0f);` và `off = AvgColor(RenderLinear(Cam), 128)`.
     - Không `yield` giữa hai lần render.
     - Log cả hai. Luôn assert `MaxDiff(on, off) <= 0.01f`, không phụ thuộc `BaselineFilled`.
   - `[UnityTest] UnderwaterViewUnchangedByDayCycle()`
     - Như test trên, với `camGo.transform.SetPositionAndRotation(new Vector3(0, -3, 0), Quaternion.Euler(-10, 0, 0))`. Camera dưới mặt phẳng, nhìn lên xiên nên thấy mặt dưới ở ~17 m.
     - Luôn assert `MaxDiff(on, off) <= 0.01f`.

   Xong khi: biên dịch không lỗi (`read_console` không có error). Lớp chạy 6/6 đạt (lúc này chưa assert mốc).

2. **Đo mốc trước khi sửa shader.**
   - Chạy riêng lớp `Game.Client.Tests.SeaSkyTintPlayTests` (PlayMode). Lấy các dòng `[0034]` từ `read_console` và chép nguyên vào mục Kết quả test, phần "Trước khi sửa".
   - Kiểm độ nhạy. Nếu có giá trị nào dưới đây thì **dừng, `cần Opus`**, ghi số đo:
     - `dev90` < 0,005 ở mốc nào đó;
     - `kinkSea` hoặc `kinkTerrain` < 0,0005 (ảnh float, không nhiễu lượng tử).
   - Nếu đạt: điền các hằng `BeforeDev90`, `BeforeKinkSea`, `BeforeKinkTerrain`, `BeforeOffNear40` bằng số đo (giữ 5 chữ số thập phân), rồi đặt `BaselineFilled = true`.
   - Chạy lại lớp này: `SeaAt90m...`, `SeaFog...` và `TerrainFog...` phải **không đạt**, chứng tỏ test bắt được lỗi; `DayCycleOffNearSeaUnchanged` và hai test nhìn gần / dưới nước đạt. Ghi kết quả.

3. **`Client/Shaders/Ocean.shader`.**
   - Thay dòng 4 bằng hai dòng:
     ```hlsl
     // Brief 0011: material Water.mat bật trong suốt (Blend SrcAlpha/OneMinusSrcAlpha, ZWrite Off, Cull Off) — mặc định vẫn đục như cũ.
     // Task m1-bien-nhuom-mau-troi (brief 0034): mặt trên nhuộm dần sang màu chân trời theo buổi; sương chuyển mềm (SoftFogFactor).
     ```
   - Chèn ngay trước dòng `half4 frag(...)` (`:100`), sau dòng trống `:99`:
     ```hlsl
            // Buổi trong ngày (brief 0023/0033): DayCycleView ghi biến global; _DayHorizon là màu chân trời dạng linear (= màu sương).
            // _DayCycleOn = 0 (scene không có DayCycleView) → không nhuộm.
            half _DayCycleOn;
            half4 _DayHorizon;
            // Biển nhuộm màu trời (task m1-bien-nhuom-mau-troi): theo khoảng cách (m) và theo góc nhìn xiên (fresnel).
            static const float SkyTintStart = 32.0, SkyTintEnd = 120.0;
            static const half SkyTintFresnel = 0.5;

            // Sương chuyển mềm (task m1-bien-nhuom-mau-troi): hệ số sương tuyến tính nắn bằng smoothstep → độ dốc về 0 ở fogStart và fogEnd,
            // không còn gãy ở mép sương. Giữ giống hệt hàm cùng tên trong Terrain.shader.
            half SoftFogFactor(float dist)
            {
                half f = ComputeFogFactorZ0ToFar(dist);
                #if defined(FOG_LINEAR_KEYWORD_DECLARED)
                if (FOG_LINEAR)
                    f = f * f * (3.0 - 2.0 * f);
                #endif
                return f;
            }

     ```
   - Thay 4 dòng `:119-122` (từ `half3 c = diffuse + light.color * spec;` tới `c = MixFog(c, fogF);`) bằng:
     ```hlsl
                half3 c = diffuse + light.color * spec;
                float dist = distance(input.positionWS, GetCameraPositionWS());
                // Biển nhuộm màu trời: chỉ mặt trên; xa và nhìn xiên → về màu chân trời theo buổi, trước sương (task m1-bien-nhuom-mau-troi).
                half skyT = saturate(smoothstep(SkyTintStart, SkyTintEnd, dist) + fres * SkyTintFresnel);
                c = lerp(c, _DayHorizon.rgb, skyT * _DayCycleOn * IS_FRONT_VFACE(face, 1.0, 0.0));
                // Sương theo khoảng cách thật (brief 0010 QĐ 11): far clip phẳng + màn hình rộng → mép nhìn xa hơn bán kính nạp.
                half fogF = SoftFogFactor(dist);
                c = MixFog(c, fogF);
     ```
   - Các dòng `:123-130` giữ nguyên (`fogVis = fogF` lúc này là hệ số đã nắn).

   Xong khi: shader biên dịch không lỗi (`read_console` không có lỗi shader `Game/Ocean`).

4. **`Client/Resources/Shaders/Terrain.shader`.**
   - Sau dòng `:42` (`#include ".../Lighting.hlsl"`), chèn một dòng trống rồi đến:
     ```hlsl
            // Sương chuyển mềm (task m1-bien-nhuom-mau-troi): hệ số sương tuyến tính nắn bằng smoothstep → độ dốc về 0 ở fogStart và fogEnd,
            // không còn gãy ở mép sương. Giữ giống hệt hàm cùng tên trong Ocean.shader.
            half SoftFogFactor(float dist)
            {
                half f = ComputeFogFactorZ0ToFar(dist);
                #if defined(FOG_LINEAR_KEYWORD_DECLARED)
                if (FOG_LINEAR)
                    f = f * f * (3.0 - 2.0 * f);
                #endif
                return f;
            }
     ```
   - Dòng `:152` (sau khi chèn, số dòng đã dời) đổi thành:
     ```hlsl
                c = MixFog(c, SoftFogFactor(distance(input.positionWS, GetCameraPositionWS())));
     ```
   - Comment ngay trên dòng đó giữ nguyên.

   Xong khi: không lỗi shader `Game/Terrain`.

5. Chạy lại `SeaSkyTintPlayTests`: 6/6 đạt. Chép các dòng `[0034]` vào phần "Sau khi sửa". Sau đó chạy toàn bộ test (mục "Lệnh executor chạy"), ghi kết quả, và dọn `EditorSettings.asset`.

## Tiêu chí xong
- `Ocean.shader` nhuộm mặt trên theo Quyết định 1. Cả hai shader dùng `SoftFogFactor`.
- `SeaSkyTintPlayTests` 6/6 đạt với `BaselineFilled = true` và các mốc đo ở bước 2. Ba test mốc không đạt khi chạy trước lúc sửa shader.
- `HorizonFogMatchPlayTests`, `DayCycleViewPlayTests`, `DayCycleWaterPlayTests` và toàn bộ EditMode + PlayMode đều đạt.
- `git status` chỉ còn 3 file trong Phạm vi (cộng `.meta` mới) và brief này.

## Test sau khi xong

### Phần cần test
| Phần | Test | Kết quả mong đợi |
|---|---|---|
| Biển xa nhuộm màu trời, 4 buổi (bước 3) | `SeaSkyTintPlayTests.SeaAt90mBlendsTowardFogColor` | Đạt: lệch ở 90 m ≤ 0,5 × trước ở cả 4 buổi. Không đạt khi chưa sửa shader (bước 2). |
| Biển không gãy ở 120 m (bước 3, `SoftFogFactor` + nhuộm) | `SeaFogHasNoKinkAtFogEnd` | Đạt ≤ 0,5 × trước ở 4 buổi. Không đạt trước khi sửa. |
| Địa hình không gãy ở 120 m (bước 4) | `TerrainFogHasNoKinkAtFogEnd` | Đạt ≤ 0,5 × trước. Không đạt trước khi sửa. |
| Không có `DayCycleView` thì biển gần như cũ (QĐ 3) | `DayCycleOffNearSeaUnchanged` | Đạt, lệch ≤ 0,002. |
| Nước nhìn gần từ trên xuống gần như không đổi | `CloseTopViewUnchangedByDayCycle` | Đạt, bật/tắt lệch ≤ 0,01. |
| Từ dưới nước nhìn lên không nhuộm (QĐ 3) | `UnderwaterViewUnchangedByDayCycle` | Đạt, lệch ≤ 0,01. |
| Phần cũ: chân trời trùng màu sương (0033). Sương 1–2 m nên mềm hay không vẫn kín. | `HorizonFogMatchPlayTests` (3 test) | Đạt, không sửa test. |
| Phần cũ: chu kỳ ngày, dưới nước theo buổi | `DayCycleViewPlayTests`, `DayCycleWaterPlayTests`, `TimeDialPlayTests` | Đạt. |
| Phần cũ: các scene và test khác dùng `Game/Terrain` / `Game/Ocean` (TerrainView, SimView, Bench, minimap…) | Toàn bộ PlayMode + EditMode | Hết đỏ. Số test PlayMode = số trước (94 theo brief 0033) + 6. |

### Lệnh executor chạy
1. Nếu Editor đang ở Play mode: `manage_editor` action `stop`.
2. `run_tests` (MCP) PlayMode, lọc `Game.Client.Tests.SeaSkyTintPlayTests`. Chạy ở bước 1, hai lần ở bước 2, một lần ở bước 5.
3. `read_console` lọc `[0034]` sau mỗi lần chạy có đo.
4. `run_tests` PlayMode, lọc `Game.Client.Tests.HorizonFogMatchPlayTests`, `Game.Client.Tests.DayCycleViewPlayTests` và `Game.Client.Tests.DayCycleWaterPlayTests` → đạt hết.
5. `run_tests` EditMode toàn bộ → ghi dòng tổng kết.
6. `run_tests` PlayMode toàn bộ → ghi dòng tổng kết.
7. `git checkout -- client/ProjectSettings/EditorSettings.asset`
8. `git status --short` → chỉ còn file trong Phạm vi.

### User tự test
Mở scene `client/Assets/Game/Client/Scenes/SimView.unity` và bấm Play trên Mac. Camera ở trại demo; kéo chuột giữa để ngẩng lên, thấy chân trời và biển ở phía trái. Đổi buổi bằng phím T (vòng chọn mốc).
1. **Trưa, bình minh, hoàng hôn, nửa đêm:**
   - Biển ở xa chuyển dần sang màu chân trời của buổi đó, rồi liền vào dải sương và bầu trời.
   - Không còn dải xám đứng yên hay mép ngang của "màn sương".
   - Dải màu phẳng ngay dưới chân trời **vẫn còn**, vì camera cao 26 m (ngoài phạm vi). Nó chỉ cần liền màu với biển và trời.
2. **Nhìn gần nước trong map** (hạ camera sát mặt nước, nhìn xuống): màu nước như trước.
3. **Lặn xuống nước rồi nhìn lên:** mặt nước không đổi màu theo buổi. Sương dưới nước chuyển mềm hơn một chút, đây là hệ quả đã chấp nhận (QĐ 2). Báo nếu thấy khó chịu.
4. **Đồi và địa hình ở xa:** tan vào sương mượt, không có vệt ngang ở mép sương.

## Dừng và chuyển lại Opus khi
- Brief mơ hồ hoặc mâu thuẫn với code thật (vd số dòng lệch, đoạn trích không khớp).
- Bước 2 có số đo dưới ngưỡng độ nhạy, hoặc ba test mốc vẫn đạt khi chưa sửa shader.
- `RenderTextureFormat.ARGBFloat` / `ReadPixels` vào `RGBAFloat` báo lỗi hoặc không được hỗ trợ trên máy này.
- Cần một quyết định thuộc AGENTS.md §3.4 hoặc ngoài "Quyết định đã chốt". Ví dụ: muốn đổi hằng 32/120/0,5 vì test không đạt.
- Cùng một lỗi sửa 2 lần vẫn chưa qua.
- Phải sửa file ngoài "Phạm vi".

---
<!-- Từ đây trở xuống: EXECUTOR ghi. -->

## Kết quả
Base commit: a289840
Chạy bằng: claude-sonnet-5-5 · medium
Bắt đầu: 2026-10-22 12:07 · Kết thúc: 2026-10-22 12:16 (chủ yếu chờ EditMode ~5 phút)
Commit: 83aef27 — Ocean.shader, Terrain.shader, Tests/Client/SeaSkyTintPlayTests.cs (+.meta)
Ghi chú: BaselineFilled = true với mốc đo ở dưới. TerrainFog sau sửa 0,01062 so ngưỡng 0,01115 (đạt sát ngưỡng).

## Kết quả test
**Trước khi sửa** (base a289840; độ nhạy đạt: dev90 min 0,02406 ≥ 0,005; kink min 0,00167 ≥ 0,0005):
- dev90: binh-minh 0.47318 · ngay 0.27425 · hoang-hon 0.47318 · dem 0.02406
- kinkSea: binh-minh 0.03123 · ngay 0.01790 · hoang-hon 0.03123 · dem 0.00167
- kinkTerrain 0.02230 · offNear40 0.03095 0.27051 0.46851
- closeTop on = off (0.02802 0.25684 0.45972); underwater on = off (0.06821 0.44531 0.58105)
- Chạy lại với mốc đã điền (chưa sửa shader): SeaAt90m, SeaFog, TerrainFog **không đạt** (bắt được lỗi); 3 test còn lại đạt.

**Sau khi sửa:**
- dev90: binh-minh 0.04099 · ngay 0.02397 · hoang-hon 0.04099 · dem 0.00210
- kinkSea: 0.00011 · 0.00010 · 0.00011 · 0.00000
- kinkTerrain 0.01062; offNear40 0.03095 0.27051 0.46851 (lệch 0 so trước)
- closeTop on 0.02869 0.25739 0.46021 vs off 0.02802 0.25684 0.45972 (lệch 0,0007); underwater on = off

| Phần | Kết quả |
|---|---|
| SeaAt90mBlendsTowardFogColor | đạt (≤ 0,5 × trước cả 4 buổi) |
| SeaFogHasNoKinkAtFogEnd | đạt |
| TerrainFogHasNoKinkAtFogEnd | đạt (0,01062 ≤ 0,01115) |
| DayCycleOffNearSeaUnchanged | đạt |
| CloseTopViewUnchangedByDayCycle | đạt |
| UnderwaterViewUnchangedByDayCycle | đạt |
| HorizonFogMatch + DayCycleView + DayCycleWater (PlayMode) | đạt 8/8 |
| Toàn bộ EditMode | 400/400 đạt |
| Toàn bộ PlayMode | 100/100 đạt (94 + 6) |
| Console error | không có |
| Kiểm bằng mắt (mục "User tự test") | chờ user |

Dọn: `git checkout -- client/ProjectSettings/EditorSettings.asset` đã chạy; `git status` chỉ còn brief.

## Vướng mắc

---
<!-- PLANNER ghi khi review. -->

## Review
- Chạy bằng `claude-sonnet-5-5 · medium` khớp Model thực thi. 9/9 dòng "Phần cần test" có kết quả; dòng kiểm bằng mắt `chờ user`.
- Diff 83aef27 (`git show`): Ocean.shader nhuộm mặt trên đúng QĐ 1 (`skyT = saturate(smoothstep(32,120,dist) + 0.5*fres)` × `_DayCycleOn` × `IS_FRONT_VFACE(face,1,0)`, trước `MixFog`; hằng trong shader, không thêm property); `SoftFogFactor` giống hệt ở hai shader, `fogVis` dùng hệ số đã nắn (QĐ 2, 3). Không đụng file ngoài Phạm vi (QĐ 4); `git status` sạch.
- Test bắt được lỗi: ba test mốc không đạt trên base, đạt sau sửa; số đo trước/sau hợp lý (dev90 giảm ~11×, kinkSea ≈ 0; offNear40 lệch 0; nhìn gần lệch 0,0007; dưới nước 0). 400 EditMode + 100 PlayMode đạt — không chạy lại.
- kinkTerrain 0,01062 vs ngưỡng 0,01115 — sát ngưỡng nhưng hợp lý: smoothstep chỉ làm độ dốc về 0 *tại* 120 m, còn độ cong trong cửa sổ 3 hàng (mỗi hàng ở xa ứng nhiều mét) vẫn cho chênh dốc; biển về ~0 vì đã nhuộm sát màu sương trước khi chạm 120 m. Không phải lỗi, nhưng test này có thể chập chờn nếu đổi máy/driver.
- Nhỏ (không bắt sửa): comment trong test ghi "base e15b86a" và "BaselineFilled = true → test chỉ ghi log" — sai chữ (base thật a289840; true → assert). Ghi chú, để lần sau đụng file thì sửa.
- Giờ: 9 phút / ước lượng 0,5 ngày — đã ghi roadmap.
- Chờ user xem trong SimView (mục "User tự test" 1–4) rồi mới kết luận.

