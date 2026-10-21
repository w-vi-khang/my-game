# Brief 0023 — World 7f-2: bầu trời, ánh sáng, sương, dưới nước theo pha + xoay 3 giây

> Trạng thái: đã duyệt
> Review: đầy đủ + user xem
> Model thực thi: Sonnet · medium
> Cập nhật: 2026-10-21 15:48

<!-- Planner (Opus) viết, Executor (Sonnet) làm theo ở PHIÊN KHÁC — executor không có lịch sử chat, không được tự quyết.
Trạng thái: chờ làm → đang làm → chờ review → đã duyệt; hoặc → cần Opus (executor dừng, hỏi ở mục Vướng mắc).
Review: nhẹ (đọc mục Kết quả) | đầy đủ (đọc cả diff) | user xem (có phần user tự kiểm bằng mắt/chơi thử) — có thể ghép "đầy đủ + user xem".
Model thực thi: BẮT BUỘC, đúng dạng `<Model> · <effort>` — Model: Opus | Sonnet | Haiku | Fable; effort: low | medium | high | xhigh | max (chọn theo AGENTS.md §10.2).
Executor kiểm tra model + effort của phiên TRƯỚC khi làm (scripts/brief_check.py); lệch hoặc không xác định được → cảnh báo user và DỪNG.
File được commit. Tự kiểm trước khi giao (AGENTS.md §10.3): executor có phải hỏi gì không? còn chỗ nào bỏ ngỏ 2 phương án? -->

## Mục tiêu
Nối lõi `TimeOfDay` (brief 0022) vào client. Bầu trời (màu, mặt trời, mặt trăng, sao), đèn chính, ánh sáng môi trường (ambient), màu sương và màu dưới nước đổi theo pha trong ngày. Chọn mốc thì bầu trời xoay tới mốc theo chiều tiến trong 3 giây, ease in-out. Trong lúc đang xoay, lệnh chọn bị bỏ qua. Brief này chưa có vòng chọn: tạm dùng phím F5–F8 để user xem, còn Bench tự chọn mốc định kỳ để đo FPS lúc xoay. Vòng chọn 4 biểu tượng và nhãn buổi làm ở 7f-3.

Task contract: [.ai/tasks/m1-world-buoc-7f-buoi-trong-ngay.md](../tasks/m1-world-buoc-7f-buoi-trong-ngay.md) (brief 2/3).

## Bối cảnh cần đọc
- [0024](../../design/decisions/0024-buoi-trong-ngay-va-chon-thoi-diem.md), mục Quyết định 2–5: pha lấy từ giờ máy cộng offset, 4 mốc, xoay luôn theo chiều tiến, 3 giây ease in-out, đang xoay thì bỏ qua lệnh chọn.
- Lõi đã có trong `client/Assets/Game/Core/DayCycle/` (brief 0022, **không sửa**):
  - `TimeConfig`: `public const int DayMs = 86_400_000;`, trường `rotateDurationMs = 3000`, `public void Validate()`.
  - `TimeOfDay(TimeConfig config, Func<long> clockMs)`, `int PhaseMs`, `TimeLandmark CurrentWindow`, `int SelectLandmark(TimeLandmark m)`. Hàm này trả cung xoay (ms). Trả 0 nghĩa là pha đang trong khung của mốc, không đổi gì.
  - `public static int ForwardArc(int fromMs, int toMs)`.
  - `enum TimeLandmark : byte { Midnight = 0, Dawn = 1, Noon = 2, Dusk = 3 }`.
- `client/Assets/Game/Client/ConfigLoader.cs:37-45`: mẫu `LoadSimConfig`, chép theo cho `LoadTimeConfig`.
- `client/Assets/Game/Client/UnderwaterView.cs:61-84`: `Enter()` lưu rồi ghi `RenderSettings.fogColor` và `cam.backgroundColor`, `Restore()` trả lại. `Refresh()` (dòng 39-53) gọi nhiều lần trong một khung cũng không sao.
- `client/Assets/Game/Client/WorldEdgeFog.cs:48-49` và `:60-61` ghi `cam.backgroundColor` từ `underwater.backgroundColor` và `underwater.fogColor`.
- `client/Assets/Game/Client/Shaders/SkyClouds.shader`: skybox của các scene. Biến global `_EdgeFogBlend` (dòng 34-35, 99) là mẫu cho biến global mới.
- `client/Assets/Game/Client/Resources/Shaders/Terrain.shader:146-149` và `Shaders/Ocean.shader:114-118`: địa hình và biển tính sáng bằng `GetMainLight` + `SampleSH`. Vì vậy chỉ cần đổi đèn và ambient là địa hình, biển tối theo, **không sửa 2 shader này**.
- Scene `TerrainView.unity`, `SimView.unity`: `Main Camera` có `DebugFlyCamera`, `UnderwaterView`, `WorldEdgeFog`. Có 1 `Directional Light` với `m_Color (1, 0.95, 0.86)`, `m_Intensity 0.9`, góc (50, -30, 0). RenderSettings có `m_AmbientMode: 1` (Trilight), fog tuyến tính màu `(0.78, 0.88, 0.97)`, ambient sky `(0.72, 0.82, 0.96)`, equator `(0.7, 0.69, 0.64)`, ground `(0.42, 0.37, 0.3)` (TerrainView.unity:17-29).
- `client/Assets/Game/Client/Editor/BenchBuild.cs:23-45`: `EnsureScene()` chép TerrainView thành Bench mỗi lần build (`BuildAndroid` gọi `EnsureScene`).
- `client/Assets/Game/Tests/Client/FlyCameraWaterPlayTests.cs:20-42, 68-88`: mẫu dựng terrain, camera, `UnderwaterView` và tìm cột nước trong PlayMode.
- `client/Assets/Game/Tests/World/ConfigTests.cs:22-32`: mẫu `SimClientCopyMatchesDesignData`.

## API Unity dùng (đã tra 6000.4)
- `RenderSettings.ambientSkyColor` / `ambientEquatorColor` / `ambientGroundColor` / `ambientMode` / `fogColor`: [docs](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/RenderSettings-ambientSkyColor.html), [docs](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/RenderSettings-fogColor.html), [docs](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/RenderSettings-ambientMode.html). Planner đã chạy thử [run, MCP execute_code 2026-10-21]: ở chế độ Trilight, gán `ambientSkyColor` thì `RenderSettings.ambientProbe` đổi ngay (đây là SH mà `SampleSH` đọc). Gán trong Editor không làm scene bị dirty.
- `Light.color`, `Light.intensity`: [docs](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/Light-color.html), [docs](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/Light-intensity.html).
- `Quaternion.LookRotation(Vector3 forward, Vector3 upwards = Vector3.up)`: [docs](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/Quaternion.LookRotation.html).
- `Shader.SetGlobalFloat`, `SetGlobalColor`, `SetGlobalVector`, `SetGlobalMatrix` (bản `int nameID`): [docs](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/Shader.SetGlobalMatrix.html).
- `Time.unscaledTimeAsDouble`: [docs](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/Time-unscaledTimeAsDouble.html).
- `Object.FindObjectsByType<T>(FindObjectsSortMode)`: [docs](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/Object.FindObjectsByType.html).
- `[DefaultExecutionOrder(n)]`: [docs](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/DefaultExecutionOrder.html).
- `Keyboard.current.f5Key` … `f8Key` (`.wasPressedThisFrame`): Input System 1.19, cùng kiểu với `f1Key` đang dùng ở `SimView.cs:84` [run, unity_reflect `Keyboard.f8Key`].
- `Matrix4x4.Rotate(Quaternion)`, `Color * Color` (nhân từng kênh), `Mathf.SmoothStep`, `Vector3.Angle`: API cơ bản có từ lâu, dùng như mô tả trong các bước.

## Quyết định đã chốt (không được đổi)
1. **Nguồn giờ** là giờ máy địa phương: `DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond`. Mốc 0 rơi vào nửa đêm, nên `mod DayMs` ra đúng giờ trong ngày. Đồng hồ hoạt ảnh xoay là `Time.unscaledTimeAsDouble` (giây). Cả hai nguồn đều tiêm được (`Func<long>`, `Func<double>`) để test.
2. **Pha hiển thị**: khi đang xoay, `hiển thị = (pha thật − round(cung × (1 − Ease(u)))) mod DayMs`, với `u = (giờ − lúc bắt đầu) / 3 giây` và `Ease(u) = u²(3 − 2u)` (kẹp u trong [0; 1]). Lúc u = 0 thì đúng bằng pha cũ, lúc u = 1 thì bằng pha thật. Pha thật vẫn trôi trong lúc xoay. Khi không xoay thì hiển thị = pha thật.
3. **Đang xoay** (`now < start + 3 s` và cung > 0) thì `Select` trả −1 và không gọi `SelectLandmark` (0024 mục 5).
4. **Quỹ đạo mặt trời**: hướng tới mặt trời `S(pha) = cos(a)·NoonSunDir + sin(a)·SetDir`, với `a = (pha − 0,5)·2π`. `NoonSunDir = −(Euler(50, −30, 0)·forward)`, tức giữa trưa đúng góc đèn hiện tại. `SetDir = Euler(0, −30, 0)·right`, nằm ngang, là hướng mặt trời lặn (pha 0,75), còn pha 0,25 mọc ở hướng ngược lại. Độ cao cực đại 50°, giữa đêm −50°.
5. **Một đèn cho cả mặt trời và mặt trăng**: `S.y ≥ 0` thì đèn chiếu theo `−S`, màu theo bảng, cường độ `0,9 × SmoothStep(0, 1, clamp01(S.y / 0,25))`. `S.y < 0` thì đèn là mặt trăng, chiếu theo `S`, màu `(0,62; 0,70; 0,95)`, cường độ `0,22 × SmoothStep(0, 1, clamp01(−S.y / 0,25))`. Ở chân trời cả hai cường độ bằng 0, nên đổi hướng không thấy giật.
6. **Bảng màu** theo 8 khoá pha, nội suy tuyến tính, quay vòng 0,8 → 1,0 (= 0,0). Giữa trưa (và 0,30, 0,70) **đúng bằng** màu scene hiện tại, nên ban ngày nhìn y như trước. Mọi giá trị khác là [số tạm], user chỉnh sau khi xem.
7. **Bầu trời** đổi qua biến global, **không** ghi vào `SkyClouds.mat`, để asset không bị đổi khi Play. `_DayCycleOn = 0` thì shader cho kết quả như cũ (scene không có `DayCycleView`, ví dụ HashCheck). Sao xoay cùng bầu trời theo `_DayStarRot`.
8. **Dưới nước**: `UnderwaterView` nhận `tint` (mặc định trắng). Màu sương và nền dưới nước là `màu gốc × tint`. `DayCycleView` chạy trước (`[DefaultExecutionOrder(-100)]`): gọi `underwater.Refresh()` trước, rồi mới ghi. Đang dưới nước thì chỉ đặt tint, còn trên cạn thì ghi `RenderSettings.fogColor`. Nhờ vậy khi vừa ngoi lên, màu sương cũ do `Restore()` trả lại bị ghi đè ngay trong khung đó.
9. Phím tạm **F5 giữa đêm, F6 bình minh, F7 giữa trưa, F8 hoàng hôn** (`debugKeys`, mặc định bật). F1/F2 đã dùng ở `SimView`, số 1–4 đã dùng ở `TerrainEditInput`. 7f-3 sẽ thay bằng vòng chọn.
10. **Bench**: `benchCycleSeconds = 5`. Cứ 5 giây chọn mốc `(CurrentWindow + 2) % 4` (cung từ 0,25 tới 0,5 ngày), mỗi lần ghi log `DAYCYCLE …`. Ở scene thường giá trị này = 0 (tắt).
11. Không sửa: lõi `Game.Core/*`, `Terrain.shader`, `Ocean.shader`, các file `.mat`, `TerrainBench.cs`, `SimView.cs`. Không lưu offset (0024 mục 4).

## Phạm vi
- Được tạo:
  - `client/Assets/Game/Client/DaySky.cs`, `SkyRotation.cs`, `DayCycleView.cs`
  - `client/Assets/Game/Client/Resources/Config/time_m1.json`: bản sao byte-đúng-byte của `design/data/time_m1.json`
  - `client/Assets/Game/Tests/Client/DaySkyTests.cs`, `SkyRotationTests.cs`, `DayCycleViewPlayTests.cs`, `DayCycleWaterPlayTests.cs`
  - Kèm `.meta` do Unity sinh.
- Được sửa:
  - `client/Assets/Game/Client/ConfigLoader.cs`, `UnderwaterView.cs`, `WorldEdgeFog.cs`
  - `client/Assets/Game/Client/Shaders/SkyClouds.shader`
  - `client/Assets/Game/Client/Editor/BenchBuild.cs`
  - `client/Assets/Game/Tests/World/ConfigTests.cs` (chỉ thêm 1 test)
  - `client/Assets/Game/Client/Scenes/TerrainView.unity`, `SimView.unity` (chỉ thêm component `DayCycleView` vào `Main Camera`)
  - `client/Assets/Game/Client/Scenes/Bench.unity`: do `EnsureScene` tự tạo lại khi build
- Không được sửa: mọi file khác, kể cả `design/`, `Bench.unity.meta` (guid phải giữ nguyên), `ProjectSettings/`.

## Các bước
1. **Cấu hình**
   - Chép `design/data/time_m1.json` sang `client/Assets/Game/Client/Resources/Config/time_m1.json`, dùng `cp` để giữ nguyên byte.
   - `ConfigLoader.cs`:
     - thêm `using Game.Core.DayCycle;` và `public const string TimeM1Path = "Config/time_m1";`;
     - thêm `public static TimeConfig LoadTimeConfig()`, chép đúng mẫu `LoadSimConfig` (dòng 37-45): đọc file, `FromJson<TimeConfig>`, gọi `Validate()`, trả về;
     - doc comment lớp (dòng 8): đổi `(world_m1, world_maps_m1, sim_m1)` thành `(world_m1, world_maps_m1, sim_m1, time_m1)`.
   - `ConfigTests.cs`: thêm `TimeClientCopyMatchesDesignData`, chép đúng mẫu dòng 22-32, đổi tên file thành `time_m1.json`.

   Xong khi: `cmp design/data/time_m1.json client/Assets/Game/Client/Resources/Config/time_m1.json` exit 0, và console 0 lỗi.

2. **`DaySky.cs`** (namespace `Game.Client`, `using UnityEngine;`). Đây là hàm thuần: từ pha (0..1) ra trạng thái bầu trời và ánh sáng.
   ```csharp
   /// <summary>Trạng thái bầu trời/ánh sáng tại một pha trong ngày (brief 0023, 0024).</summary>
   public struct DaySkyState
   {
       public Color zenith, horizon, cloud, cloudShade;              // horizon cũng là màu sương trên cạn
       public Color ambientSky, ambientEquator, ambientGround;
       public Color sunColor;                                         // màu đĩa mặt trời và màu đèn ban ngày
       public Color lightColor;                                       // màu đèn thật (mặt trời hoặc mặt trăng)
       public Color waterTint;                                        // nhân vào màu dưới nước
       public float lightIntensity, stars;                            // stars 0..1
       public Vector3 sunDir;                                         // hướng TỚI mặt trời (đơn vị)
       public Vector3 lightForward;                                   // hướng chiếu của đèn
       public Quaternion starRotation;                                // starRotation * NoonSunDir = sunDir
   }

   /// <summary>
   /// Bầu trời theo pha (brief 0023): quỹ đạo mặt trời qua NoonSunDir (giữa trưa = góc đèn cũ 50°, −30°) và SetDir (lặn ở pha 0,75);
   /// một đèn dùng cho mặt trời (S.y ≥ 0) và mặt trăng (đối diện); màu nội suy theo 8 khoá [số tạm], giữa trưa = màu scene cũ.
   /// </summary>
   public static class DaySky
   {
       public const float NoonPitch = 50f, Yaw = -30f;
       public const float SunIntensity = 0.9f, MoonIntensity = 0.22f, LightRamp = 0.25f;
       public static readonly Color MoonLightColor = new Color(0.62f, 0.70f, 0.95f);
       public static readonly Color MoonDiscColor = new Color(0.85f, 0.88f, 0.95f);
       public static readonly Vector3 NoonSunDir = -(Quaternion.Euler(NoonPitch, Yaw, 0f) * Vector3.forward);
       public static readonly Vector3 SetDir = Quaternion.Euler(0f, Yaw, 0f) * Vector3.right;
       public static readonly Vector3 Axis = Vector3.Cross(NoonSunDir, SetDir).normalized;
       public static DaySkyState Evaluate(float phase) { … }
   }
   ```
   - Khoá: `struct Key { public float phase, stars; public Color zenith, horizon, cloud, cloudShade, ambientSky, ambientEquator, ambientGround, sun, waterTint; }`, mảng `static readonly Key[] Keys` gồm 8 phần tử **theo đúng thứ tự pha** trong bảng dưới. Giá trị trong bảng là RGB, alpha = 1.

     | pha | zenith | horizon | cloud | cloudShade | ambientSky | ambientEquator | ambientGround | sun | waterTint | stars |
     |---|---|---|---|---|---|---|---|---|---|---|
     | 0,00 | .02 .03 .09 | .08 .11 .20 | .16 .18 .26 | .10 .12 .18 | .12 .15 .26 | .09 .10 .14 | .05 .05 .07 | 1 .62 .38 | .22 .26 .38 | 1 |
     | 0,20 | .06 .08 .20 | .30 .26 .38 | .34 .30 .40 | .20 .20 .30 | .20 .22 .36 | .20 .18 .24 | .10 .09 .10 | 1 .62 .38 | .35 .36 .48 | 0,5 |
     | 0,25 | .30 .40 .66 | .96 .66 .46 | 1 .80 .66 | .62 .50 .56 | .52 .52 .66 | .74 .56 .46 | .30 .26 .24 | 1 .62 .38 | .80 .70 .68 | 0 |
     | 0,30 | .36 .62 .92 | .78 .88 .97 | 1 1 1 | .80 .85 .93 | .72 .82 .96 | .70 .69 .64 | .42 .37 .30 | 1 .95 .86 | 1 1 1 | 0 |
     | 0,50 | (như 0,30) | | | | | | | | | 0 |
     | 0,70 | (như 0,30) | | | | | | | | | 0 |
     | 0,75 | (như 0,25) | | | | | | | | | 0 |
     | 0,80 | (như 0,20) | | | | | | | | | 0,5 |

     Màu 0,30/0,50/0,70 lấy đúng từ scene và `SkyClouds.shader:7-10`. Nên khai báo 4 bộ màu (night, twilight, sunrise, day) rồi dùng lại cho các khoá giống nhau.
   - `Evaluate(float phase)`:
     1. `phase -= Mathf.Floor(phase);`
     2. Chọn `i` là chỉ số lớn nhất có `Keys[i].phase <= phase`. `a = Keys[i]`, `b = Keys[(i + 1) % Keys.Length]`, `span = (b.phase - a.phase + 1f) % 1f`, `t = ((phase - a.phase + 1f) % 1f) / span`. Mọi trường màu và `stars` nội suy `Color.Lerp`/`Mathf.Lerp(a, b, t)`. `sunColor` lấy cột `sun`.
     3. `float ang = (phase - 0.5f) * 2f * Mathf.PI; Vector3 S = Mathf.Cos(ang) * NoonSunDir + Mathf.Sin(ang) * SetDir;` rồi gán `sunDir = S`.
     4. `S.y >= 0`: `lightForward = -S`, `lightColor = sunColor`, `lightIntensity = SunIntensity * Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(S.y / LightRamp))`. Ngược lại: `lightForward = S`, `lightColor = MoonLightColor`, `lightIntensity = MoonIntensity * Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(-S.y / LightRamp))`.
     5. `starRotation = Quaternion.LookRotation(S, Axis) * Quaternion.Inverse(Quaternion.LookRotation(NoonSunDir, Axis));`

   Xong khi: biên dịch xong, console 0 lỗi.

3. **`SkyRotation.cs`** (namespace `Game.Client`, `using System; using Game.Core.DayCycle;`). Lớp C# thường, không phải MonoBehaviour.
   ```csharp
   /// <summary>Xoay bầu trời khi chọn mốc (0024 mục 5, brief 0023): pha hiển thị đi từ pha cũ tới pha thật theo chiều tiến trong DurationSeconds, ease in-out.</summary>
   public sealed class SkyRotation
   {
       public SkyRotation(double durationSeconds)        // durationSeconds <= 0 hoặc NaN → ArgumentOutOfRangeException
       public double DurationSeconds { get; }
       public void Start(int arcMs, double nowSeconds)   // arcMs ngoài [0; DayMs) → ArgumentOutOfRangeException; lưu arc, start
       public bool IsRotating(double nowSeconds)         // arc > 0 && nowSeconds < start + DurationSeconds
       public int DisplayPhaseMs(int truePhaseMs, double nowSeconds)
       public static double Ease(double u)               // kẹp u vào [0;1], trả u*u*(3-2u)
   }
   ```
   `DisplayPhaseMs`: nếu `!IsRotating(now)` thì trả `truePhaseMs`. Ngược lại tính `u = (now - start) / DurationSeconds`, `long remaining = (long)Math.Round(arc * (1.0 - Ease(u)))`, `long r = (truePhaseMs - remaining) % TimeConfig.DayMs`, rồi trả `(int)(r < 0 ? r + TimeConfig.DayMs : r)`.

4. **`UnderwaterView.cs`** (chỉ thêm, không đổi logic phát hiện):
   - Trường `/// <summary>Nhân vào màu sương/nền dưới nước theo buổi (DayCycleView, brief 0023); trắng = như cũ.</summary> public Color tint = Color.white;`
   - `public Color CurrentFogColor => fogColor * tint;` và `public Color CurrentBackground => backgroundColor * tint;`
   - `Enter()`: dòng 68 đổi thành `RenderSettings.fogColor = CurrentFogColor;`, dòng 72 đổi thành `cam.backgroundColor = CurrentBackground;`.
   - Thêm:
     ```csharp
     /// <summary>Đặt tint; đang dưới nước thì ghi lại màu sương và nền ngay (brief 0023).</summary>
     public void ApplyTint(Color t)
     {
         tint = t;
         if (!IsUnderwater)
             return;
         RenderSettings.fogColor = CurrentFogColor;
         cam.backgroundColor = CurrentBackground;
     }
     ```
   - **`WorldEdgeFog.cs`**: dòng 49 đổi thành `Color.Lerp(underwater.CurrentBackground, underwater.CurrentFogColor, Factor)`, dòng 61 đổi thành `cam.backgroundColor = underwater.CurrentBackground;`. Không đổi gì khác.

   Xong khi: `FlyCameraWaterPlayTests` và `WorldBoundsPlayTests` cũ vẫn đạt (tint trắng nên màu không đổi).

5. **`SkyClouds.shader`**:
   - Ngay sau khai báo `half _EdgeFogBlend;` (dòng 35), thêm:
     ```hlsl
     // Buổi trong ngày (brief 0023): DayCycleView ghi các biến global sau; _DayCycleOn = 0 (scene không có DayCycleView) → trời như cũ.
     half _DayCycleOn;
     half4 _DayZenith, _DayHorizon, _DayCloud, _DayCloudShade, _DaySunColor, _DayMoonColor;
     float4 _DaySunDir;      // xyz: hướng tới mặt trời (đơn vị)
     half _DayStars;         // độ hiện của sao 0..1
     float4x4 _DayStarRot;   // đổi hướng nhìn sang hệ của bầu sao (xoay cùng mặt trời)
     ```
   - Ngay sau hàm `Fbm` (sau dòng 85), thêm:
     ```hlsl
     // Sao: lưới 3D trên hướng nhìn đã xoay; mỗi ô 1,5 % là sao, sáng nhất ở tâm ô. Băm số nguyên như mây (đúng trên Mali).
     half StarField(float3 d)
     {
         float3 p = mul((float3x3)_DayStarRot, d) * 180.0;
         float3 fl = floor(p);
         int3 c = (int3)fl;
         float h = HashInt(int2(c.x ^ (c.z * 374761393), c.y ^ (c.z * 668265263)));
         float3 f = p - fl - 0.5;
         return (half)(step(0.985, h) * saturate(1.0 - length(f) * 2.5));
     }
     ```
   - Thay toàn bộ thân `frag` (dòng 88-100) bằng:
     ```hlsl
     float3 d = normalize(input.dir);
     float up = saturate(d.y);
     half3 zenith = lerp(_ZenithColor.rgb, _DayZenith.rgb, _DayCycleOn);
     half3 horizon = lerp(_HorizonColor.rgb, _DayHorizon.rgb, _DayCycleOn);
     half3 cloudLit = lerp(_CloudColor.rgb, _DayCloud.rgb, _DayCycleOn);
     half3 cloudDark = lerp(_CloudShadeColor.rgb, _DayCloudShade.rgb, _DayCycleOn);
     half3 sky = lerp(horizon, zenith, pow(up, 0.55));
     // Mặt trời, mặt trăng (đối diện mặt trời), sao — chỉ trên chân trời; mây che lên trên.
     float above = smoothstep(0.0, 0.04, d.y);
     float cs = dot(d, _DaySunDir.xyz);
     half sunDisc = smoothstep(0.9996, 0.9998, cs);
     half sunGlow = pow(saturate(cs), 48.0) * 0.35;
     half moonDisc = smoothstep(0.9997, 0.9999, -cs);
     sky += _DayCycleOn * above * (_DaySunColor.rgb * (sunDisc + sunGlow) + _DayMoonColor.rgb * moonDisc + StarField(d) * _DayStars);
     // Mây: chiếu hướng nhìn lên mặt phẳng trời (gần chân trời mây dày, nhỏ lại theo phối cảnh).
     float2 uv = d.xz / (d.y + 0.12) * _CloudScale + _Time.y * _CloudSpeed * float2(1, 0.4);
     float n = Fbm(uv);
     float cloud = smoothstep(_CloudCover, _CloudCover + _CloudSoftness, n) * smoothstep(0.0, 0.18, d.y);
     // Mặt dưới mây tối hơn ở chỗ mây dày.
     half3 cloudCol = lerp(cloudLit, cloudDark, saturate((n - _CloudCover) * 2.5));
     half3 c = lerp(sky, cloudCol, cloud * 0.92);
     c = lerp(c, unity_FogColor.rgb, _EdgeFogBlend);
     return half4(c, 1);
     ```
   - Comment đầu file (dòng 1-2): thêm câu `Brief 0023: màu, mặt trời/mặt trăng/sao theo buổi qua biến global _Day* (DayCycleView).`

   Xong khi: `read_console` không có lỗi shader. Mở scene TerrainView ở Edit mode (chưa Play), bầu trời phải trông như trước, vì `_DayCycleOn = 0`.

6. **`DayCycleView.cs`** (namespace `Game.Client`, `using System; using Game.Core.DayCycle; using UnityEngine; using UnityEngine.InputSystem; using UnityEngine.Rendering;`):
   ```csharp
   /// <summary>
   /// Buổi trong ngày trên client (0024, brief 0023): pha từ TimeOfDay (giờ máy + offset) → bầu trời (biến global của Game/SkyClouds),
   /// đèn chính (mặt trời/mặt trăng), ambient Trilight, màu sương, tint dưới nước. Chọn mốc → xoay theo chiều tiến trong
   /// rotateDurationMs, ease in-out; đang xoay thì bỏ qua lệnh chọn. Chạy trước UnderwaterView/WorldEdgeFog để ghi màu sương đúng khung.
   /// </summary>
   [DefaultExecutionOrder(-100)]
   public class DayCycleView : MonoBehaviour
   ```
   - Trường public:
     - `public Light sun;`, null thì tìm trong `Start`;
     - `public UnderwaterView underwater;`, null thì lấy `GetComponent<UnderwaterView>()` trong `Start`;
     - `/// <summary>Phím tạm F5 giữa đêm · F6 bình minh · F7 giữa trưa · F8 hoàng hôn (7f-3 thay bằng vòng chọn).</summary> public bool debugKeys = true;`
     - `/// <summary>> 0: cứ chừng này giây tự chọn mốc (CurrentWindow + 2) % 4 — chỉ dùng ở scene Bench.</summary> public float benchCycleSeconds = 0f;`
     - `[NonSerialized] public Func<long> clockMs;`, null thì dùng `LocalClockMs`;
     - `[NonSerialized] public Func<double> secondsNow;`, null thì dùng `() => Time.unscaledTimeAsDouble`.
   - Thuộc tính:
     - `public TimeOfDay Day { get; private set; }`. Không đặt tên `Time` để khỏi trùng `UnityEngine.Time`.
     - `public int DisplayPhaseMs { get; private set; }`
     - `public DaySkyState State { get; private set; }`
     - `public bool IsRotating => rotation != null && rotation.IsRotating(Now());`
   - `public static long LocalClockMs() => DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;`
   - `double Now() => secondsNow != null ? secondsNow() : Time.unscaledTimeAsDouble;`
   - Id shader dùng `static readonly int … = Shader.PropertyToID("…")` cho: `_DayCycleOn`, `_DayZenith`, `_DayHorizon`, `_DayCloud`, `_DayCloudShade`, `_DaySunColor`, `_DayMoonColor`, `_DaySunDir`, `_DayStars`, `_DayStarRot`.
   - `Start()`:
     1. Nếu `sun == null`: duyệt `FindObjectsByType<Light>(FindObjectsSortMode.None)`, lấy đèn đầu tiên có `type == LightType.Directional && enabled`.
     2. Nếu `underwater == null` thì `underwater = GetComponent<UnderwaterView>();`.
     3. `var cfg = ConfigLoader.LoadTimeConfig(); Day = new TimeOfDay(cfg, clockMs ?? LocalClockMs); rotation = new SkyRotation(cfg.rotateDurationMs / 1000.0);`
     4. `RenderSettings.ambientMode = AmbientMode.Trilight;` rồi `nextBench = Now() + benchCycleSeconds;` rồi `Apply();`
   - `void LateUpdate()`:
     1. Nếu `Day == null` thì return.
     2. Nếu `debugKeys` và `Keyboard.current != null`: `f5Key.wasPressedThisFrame` → `Select(TimeLandmark.Midnight)`, `f6` → Dawn, `f7` → Noon, `f8` → Dusk.
     3. Nếu `benchCycleSeconds > 0 && Now() >= nextBench`: `Select((TimeLandmark)(((int)Day.CurrentWindow + 2) % 4)); nextBench += benchCycleSeconds;`
     4. `Apply();`
   - Hàm `Select`:
     ```csharp
     /// <summary>Chọn mốc (0024): trả cung xoay (ms); 0 = đang trong khung mốc, không đổi; −1 = đang xoay, bỏ qua.</summary>
     public int Select(TimeLandmark m)
     {
         double now = Now();
         if (rotation.IsRotating(now))
             return -1;
         int from = Day.PhaseMs;
         int arc = Day.SelectLandmark(m);
         if (arc > 0)
         {
             rotation.Start(arc, now);
             Debug.Log($"DAYCYCLE select={m} arcMs={arc} fromMs={from} toMs={Day.PhaseMs}");
         }
         return arc;
     }
     ```
   - `public void Apply()` (public để test gọi ngay sau khi đổi đồng hồ giả):
     1. `DisplayPhaseMs = rotation.DisplayPhaseMs(Day.PhaseMs, Now());`
     2. `var s = DaySky.Evaluate(DisplayPhaseMs / (float)TimeConfig.DayMs); State = s;`
     3. Nếu `sun != null`: `sun.transform.rotation = Quaternion.LookRotation(s.lightForward); sun.color = s.lightColor; sun.intensity = s.lightIntensity;`
     4. `RenderSettings.ambientSkyColor = s.ambientSky; RenderSettings.ambientEquatorColor = s.ambientEquator; RenderSettings.ambientGroundColor = s.ambientGround;`
     5. Nếu `underwater != null`: `underwater.Refresh(); underwater.ApplyTint(s.waterTint);`
     6. Nếu `underwater == null || !underwater.IsUnderwater`: `RenderSettings.fogColor = s.horizon;`
     7. Ghi global: `_DayCycleOn` = 1. `_DayZenith`, `_DayHorizon`, `_DayCloud`, `_DayCloudShade` lấy từ `s`. `_DaySunColor = s.sunColor`. `_DayMoonColor = DaySky.MoonDiscColor`. `_DaySunDir = (Vector4)s.sunDir`. `_DayStars = s.stars`. `_DayStarRot = Matrix4x4.Rotate(Quaternion.Inverse(s.starRotation))`.
   - `void OnDisable() { Shader.SetGlobalFloat(DayCycleOnId, 0f); }`. Không trả lại đèn và ambient: thoát Play thì scene tự trả.
   - **Không** dùng `[RequireComponent]`.

   Xong khi: biên dịch xong, console 0 lỗi.

7. **Scene**: dùng MCP. Mở `Assets/Game/Client/Scenes/TerrainView.unity`, thêm component `DayCycleView` vào `Main Camera`, để mặc định (sun và underwater để trống, tự tìm), rồi lưu. Làm tương tự với `SimView.unity`.

   Xong khi: `grep -c "Game.Client.DayCycleView" client/Assets/Game/Client/Scenes/TerrainView.unity client/Assets/Game/Client/Scenes/SimView.unity` ra 1 cho mỗi file, và `git diff --stat` 2 scene chỉ thêm vài chục dòng.

8. **`BenchBuild.cs`**: trong `EnsureScene()`, ngay sau dòng `view.gameObject.AddComponent<TerrainBench>();` (sau khối `if`, dòng 41-42), thêm:
   ```csharp
   // Brief 0023: đo FPS khi bầu trời xoay — cứ 5 giây tự chọn mốc; không có bàn phím.
   foreach (var day in UnityEngine.Object.FindObjectsByType<DayCycleView>(FindObjectsSortMode.None))
   {
       day.benchCycleSeconds = 5f;
       day.debugKeys = false;
   }
   ```

9. **Test** (thư mục `client/Assets/Game/Tests/Client/`, namespace `Game.Client.Tests`, assembly PlayMode `Game.Client.Tests`). Đặt `const int H = 3_600_000; const int Day = TimeConfig.DayMs;`. Hàm so màu: `static void Near(Color e, Color a, float tol = 1e-4f)` assert r, g, b lệch ≤ tol.
   - **`DaySkyTests.cs`** (`[Test]`):
     - `NoonMatchesOldSceneLook`: `Evaluate(0.5f)` cho `zenith (0.36, 0.62, 0.92)`, `horizon (0.78, 0.88, 0.97)`, `ambientSky (0.72, 0.82, 0.96)`, `ambientEquator (0.70, 0.69, 0.64)`, `ambientGround (0.42, 0.37, 0.30)`, `lightColor (1, 0.95, 0.86)`, `waterTint` trắng, `stars == 0`, `lightIntensity` = 0,9 (±1e-5). `Vector3.Angle(lightForward, Quaternion.Euler(50, -30, 0) * Vector3.forward) < 0.01f`.
     - `SunPath`: `sunDir.y` tại 0,25 và 0,75 thì |·| < 1e-4; tại 0,5 bằng `sin 50°` (±1e-4); tại 0 bằng `−sin 50°`. `Angle(sunDir(0.75), SetDir) < 0.01`, `Angle(sunDir(0.25), -SetDir) < 0.01`. Cường độ tại 0 bằng 0,22, tại 0,25 dưới 1e-4. Với mọi pha k/200 (k = 0..199) có `lightIntensity > 1e-3` thì `lightForward.y < 0` (đèn luôn chiếu xuống).
     - `ContinuousIncludingWrap`: lấy mẫu pha k/2000 (k = 0..2000; mẫu cuối là 1,0, tức quay vòng về 0). Giữa 2 mẫu liền nhau, mọi kênh r, g, b của 7 màu (zenith, horizon, cloud, cloudShade, ambientSky, ambientEquator, ambientGround) cùng `waterTint`, `stars` và `lightIntensity` lệch < 0,02. Nếu `Vector3.Angle(lightForward[k], lightForward[k+1]) > 10f` thì cả 2 cường độ đều < 1e-3 (chỉ đổi hướng đèn lúc tắt).
     - `NightIsDarkWithStars`: tại 0, `ambientSky.grayscale < 0.25f * ambientSky.grayscale` của giữa trưa, `stars == 1`, `lightColor` bằng `DaySky.MoonLightColor`. Với mọi pha trong [0,30; 0,70] (bước 0,01) thì `stars == 0`.
     - `StarsRotateWithSun`: tại các pha 0; 0,13; 0,25; 0,5; 0,77 thì `Angle(Quaternion.Inverse(s.starRotation) * s.sunDir, DaySky.NoonSunDir) < 0.01`. Tại 0,5 thì `Quaternion.Angle(s.starRotation, Quaternion.identity) < 0.01`.
   - **`SkyRotationTests.cs`** (`[Test]`):
     - `EaseEnds`: `Ease(0) == 0`, `Ease(1) == 1`, `Ease(0.5) == 0.5`, `Ease(-1) == 0`, `Ease(2) == 1`.
     - `RotatesForwardOverDuration`: `r = new SkyRotation(3.0)`, ban đầu `IsRotating(0)` là false. `r.Start(18 * H, 10.0)`, rồi:
       - `DisplayPhaseMs(6 * H, 10.0) == 12 * H`;
       - `DisplayPhaseMs(6 * H, 11.5) == 21 * H`;
       - `IsRotating(12.999)` là true, `IsRotating(13.0)` là false;
       - `DisplayPhaseMs(6 * H, 13.0) == 6 * H`.

       Duyệt t từ 10,00 tới 13,00 bước 0,01 (dùng `10.0 + i * 0.01`, i = 0..300) với pha thật cố định `6 * H`: mỗi bước `TimeOfDay.ForwardArc(prev, cur) < H`, tổng các bước đúng bằng `18 * H`.
     - `ZeroArcAndBadArgs`: `Start(0, 5.0)` thì `IsRotating(5.0)` là false và `DisplayPhaseMs(777, 5.0) == 777`. `new SkyRotation(0)`, `Start(-1, 0)`, `Start(Day, 0)` đều ném `ArgumentOutOfRangeException`.
   - **`DayCycleViewPlayTests.cs`** (`[UnityTest]`). SetUp tạo:
     - `lightGo` có `Light` (`type = LightType.Directional`);
     - `camGo` có `Camera` và `DayCycleView` với `view.sun = light`, `view.clockMs = () => now`, `view.secondsNow = () => t` (trường `long now = 12 * H; double t = 100.0;`).
     Sau đó `yield return null` để `Start` chạy. TearDown `Object.Destroy` cả 2 rồi `yield return null`.
     - `FollowsClockAtNoonAndMidnight`:
       - Giữa trưa: `Angle(light.transform.forward, Euler(50,-30,0)*forward) < 0.05`, `light.intensity` 0,9 (±1e-4), `Near(DaySky.Evaluate(0.5f).horizon, RenderSettings.fogColor)`, `Near(DaySky.Evaluate(0.5f).ambientSky, RenderSettings.ambientSkyColor)`, `Shader.GetGlobalFloat("_DayCycleOn") == 1`.
       - `now = 0; view.Apply();`: `light.intensity` 0,22 (±1e-4), `light.transform.forward.y < 0`, `Near(DaySky.Evaluate(0f).horizon, RenderSettings.fogColor)`.
     - `SelectRotatesForwardThenIgnoresDuringRotation`:
       - `Select(Dawn)` trả `18 * H`, rồi `view.Apply()`. `DisplayPhaseMs == 12 * H`, `IsRotating` là true.
       - `t += 1.5; view.Apply();`: `DisplayPhaseMs == 21 * H`.
       - `int off = view.Day.OffsetMs;` rồi `Select(Noon)` trả −1 và `view.Day.OffsetMs == off`.
       - `t += 1.5; view.Apply();`: `IsRotating` là false, `DisplayPhaseMs == view.Day.PhaseMs == 6 * H`.
     - `SameWindowDoesNothing`: lúc giữa trưa, `Select(Noon)` trả 0 và `IsRotating` là false.
     - `DisableClearsSkyFlag`: `view.enabled = false` thì `Shader.GetGlobalFloat("_DayCycleOn") == 0`.
   - **`DayCycleWaterPlayTests.cs`** (`[UnityTest]`): SetUp chép `FlyCameraWaterPlayTests.cs:20-34` (terrain, camera, `DebugFlyCamera`, `UnderwaterView`), thêm `lightGo` có Light Directional. Trên `camGo` thêm `DayCycleView` với `sun = light`, `underwater = under`, `clockMs = () => now`, `secondsNow = () => 0.0`, `long now = 0` (giữa đêm). Tìm cột nước theo đúng vòng lặp ở `FlyCameraWaterPlayTests.cs:70-88`. TearDown hủy cả 3 object.
     - `NightTintsUnderwaterAndSurfacingRestoresCurrentFog`:
       1. Đặt camera tại `(fx + 0.5, S + 3.5, fz + 0.5)` rồi `yield return null`. Kiểm `IsUnderwater` false, `Near(DaySky.Evaluate(0f).horizon, RenderSettings.fogColor)`.
       2. `fly.Fly(new Vector3(0, -3.2f, 0))` rồi `yield return null`. Kiểm `IsUnderwater` true, `Near(under.fogColor * DaySky.Evaluate(0f).waterTint, RenderSettings.fogColor)`, `Near(under.backgroundColor * DaySky.Evaluate(0f).waterTint, cam.backgroundColor)`.
       3. `now = 12 * H;` rồi `yield return null`. Kiểm `Near(under.fogColor, RenderSettings.fogColor)` (tint trắng).
       4. `fly.Fly(new Vector3(0, 3.2f, 0))` rồi `yield return null`. Kiểm `IsUnderwater` false, `Near(DaySky.Evaluate(0.5f).horizon, RenderSettings.fogColor)`: phải là màu giữa trưa hiện tại, không phải màu giữa đêm do `Restore()` trả lại.

   Xong khi: `run_tests` PlayMode lọc `DaySkyTests`, `SkyRotationTests`, `DayCycleViewPlayTests`, `DayCycleWaterPlayTests`, `FlyCameraWaterPlayTests`, `WorldBoundsPlayTests` thì đạt hết. Sau đó chạy `git checkout -- client/ProjectSettings/EditorSettings.asset`.

10. **Bench + điện thoại.** `ADB` là `/Applications/Unity/Hub/Editor/6000.4.8f1/PlaybackEngines/AndroidPlayer/SDK/platform-tools/adb`. Máy Xiaomi 24095PCADG, serial `PFQCRWVWR46HU4GQ` (USB). Nếu `ADB devices` không có serial này thì chạy `ADB connect 192.168.1.6:5555` và dùng `-s 192.168.1.6:5555`.
    1. MCP `execute_menu_item` "Game/Build APK Bench" → console có `Bench build: Succeeded`.
    2. Cài và chạy: `ADB -s <máy> install -r client/Builds/bench.apk` → `Success`. Sau đó `ADB -s <máy> logcat -c`, rồi `ADB -s <máy> shell monkey -p com.viventa.bench -c android.intent.category.LAUNCHER 1`.
    3. Chờ 75 giây (chạy nền hoặc Monitor), rồi chạy `ADB -s <máy> logcat -d -s Unity | grep -E "BENCH|DAYCYCLE|Exception"`. Ghi vào Kết quả:
       - số dòng `DAYCYCLE` (phải ≥ 6) và nguyên văn 2 dòng đầu;
       - `avgFps` (phải ≥ 30), `p99Ms`, `holeFrames` (= 0), `tris` (> 0), `hash` (= `a7719c8999939263`) của dòng `BENCH`;
       - mọi dòng `Exception` (phải không có).

       Sau 150 giây vẫn không có `BENCH` (máy khoá màn hình) → ghi "chờ user mở khoá" và làm tiếp bước 11.
    4. Dọn:
       - `git checkout -- client/ProjectSettings/UnityConnectSettings.asset` (nếu đổi);
       - nếu `git diff client/Assets/Game/Client/Scenes/Bench.unity.meta` có thay đổi thì `git checkout -- client/Assets/Game/Client/Scenes/Bench.unity.meta`.

11. **Chạy toàn bộ + commit:**
    1. EditMode cả bộ: 339 cũ + 1 (`TimeClientCopyMatchesDesignData`) = 340, đạt hết.
    2. PlayMode cả bộ: 39 cũ + 13 mới = 52, đạt hết. Sau đó `git checkout -- client/ProjectSettings/EditorSettings.asset`.
    3. `python3 tools/check_design.py` → hợp lệ.
    4. `git status --short`: chỉ còn file trong Phạm vi. Commit code với message `brief 0023: bầu trời, ánh sáng, sương, dưới nước theo buổi + xoay 3 giây`.
    5. `date "+%Y-%m-%d %H:%M"` → ghi "Kết thúc", đặt trạng thái `chờ review`, commit brief với message `brief 0023: kết quả`.

## Tiêu chí xong
- `DaySky`, `SkyRotation`, `DayCycleView` đúng chữ ký và hành vi ở bước 2, 3, 6. `UnderwaterView` và `WorldEdgeFog` chỉ đổi đúng như bước 4. `SkyClouds.shader` đúng bước 5.
- `DayCycleView` có trên `Main Camera` của TerrainView và SimView. Bench tự chọn mốc mỗi 5 giây.
- EditMode 340/340, PlayMode 52/52. Test cũ không bị sửa (ngoài việc thêm 1 test vào `ConfigTests.cs`).
- Điện thoại: `avgFps ≥ 30`, `holeFrames=0`, hash v3 trùng, ≥ 6 dòng `DAYCYCLE`, 0 `Exception` (hoặc ghi "chờ user mở khoá").
- Chỉ các file trong Phạm vi bị đổi.

## Test sau khi xong

### Phần cần test
| Phần | Bước | Test | Kết quả mong đợi |
|---|---|---|---|
| Bản sao cấu hình + loader | 1 | `cmp`; `ConfigTests.TimeClientCopyMatchesDesignData`; `LoadTimeConfig` (qua các test của `DayCycleView`) | Giống byte; đọc và validate được |
| Ban ngày giữ nguyên như cũ | 2 | `NoonMatchesOldSceneLook` | Màu, đèn, góc lúc giữa trưa đúng như scene cũ |
| Quỹ đạo mặt trời + đèn mặt trăng | 2 | `SunPath` | Mọc/lặn ở chân trời đúng hướng; đèn luôn chiếu xuống |
| Không giật màu/đèn, kể cả qua nửa đêm | 2 | `ContinuousIncludingWrap` | Mỗi bước < 0,02; chỉ đổi hướng đèn lúc cường độ ≈ 0 |
| Đêm tối + sao | 2 | `NightIsDarkWithStars` | Ambient đêm < 25 % trưa; ban ngày không có sao |
| Sao xoay cùng bầu trời | 2 | `StarsRotateWithSun` | Ma trận sao đưa hướng mặt trời về `NoonSunDir` |
| Xoay theo chiều tiến, ease, đúng 3 giây | 3 | `SkyRotationTests` (3) | 12 h → 21 h → 6 h; mỗi bước tiến; tổng = cung |
| Tint dưới nước, không ảnh hưởng cũ | 4 | `FlyCameraWaterPlayTests`, `WorldBoundsPlayTests` (cũ) | Đạt, không sửa test |
| Dưới nước theo buổi + ngoi lên đúng màu | 4, 6 | `DayCycleWaterPlayTests` | Đêm thì tối theo tint; đổi giờ khi đang lặn thì đổi theo; ngoi lên thì màu sương là màu hiện tại |
| Bầu trời shader | 5 | `read_console` 0 lỗi shader; Edit mode trời như cũ; user xem | Không lỗi; có mặt trời, mặt trăng, sao khi Play |
| `DayCycleView` theo giờ, chọn mốc, bỏ qua khi xoay | 6 | `DayCycleViewPlayTests` (4) | Như bước 9 |
| Gắn vào scene | 7 | `grep` 2 scene | Mỗi scene 1 `DayCycleView` |
| Bench + điện thoại | 8, 10 | APK + logcat | ≥ 6 `DAYCYCLE`, avgFps ≥ 30, holeFrames 0, hash trùng, 0 Exception |
| Regression chung | — | EditMode + PlayMode cả bộ | 340/340, 52/52 |

### Lệnh executor chạy
Unity Editor dùng chung qua MCP (quyết định 0010). Nếu Editor đang tắt thì thay `run_tests` bằng `python3 tools/unity_test.py EditMode|PlayMode` (không build APK được, ghi lại).
1. `cmp design/data/time_m1.json client/Assets/Game/Client/Resources/Config/time_m1.json` → exit 0.
2. MCP `refresh_unity` → `read_console` (types error) → 0 lỗi, kể cả lỗi shader.
3. `grep -c "Game.Client.DayCycleView" client/Assets/Game/Client/Scenes/TerrainView.unity client/Assets/Game/Client/Scenes/SimView.unity` → 1 và 1.
4. MCP `run_tests` PlayMode lọc 6 lớp ở bước 9 → đạt. Dọn: `git checkout -- client/ProjectSettings/EditorSettings.asset`.
5. MCP `run_tests` EditMode lọc `ConfigTests` → đạt.
6. Bước 10 (Bench, APK, adb, logcat). Dọn `UnityConnectSettings.asset` và `Bench.unity.meta`.
7. EditMode cả bộ, rồi PlayMode cả bộ → ghi dòng tổng kết. Dọn `EditorSettings.asset`.
8. `python3 tools/check_design.py` → hợp lệ.
9. `git status --short` → commit (bước 11).

### User tự test (Planner gửi khi review)
Trên Mac, scene `SimView` (và `TerrainView`), nhấn Play:
1. Bầu trời lúc mở ra đúng với giờ máy (vd buổi chiều thì nắng xiên; sau 18 giờ thì tối, có sao và trăng).
2. Đang trưa nhấn **F6** (bình minh): mặt trời đi về phía tây, lặn, trời tối có sao, rồi mọc ở phía đông. Không quay ngược, mượt, khoảng 3 giây.
3. Trong lúc đang xoay, nhấn phím khác thì không có tác dụng. Đang ở bình minh nhấn lại **F6** thì không có gì xảy ra.
4. Ban đêm: địa hình, nhà, biển tối dần nhưng vẫn nhìn được. Bay xuống nước (Q) thì nước tối theo. Ngoi lên thì sương đúng màu đêm. Bay ra mép world thì sương biên vẫn tan vào trời.
5. Màu bình minh, hoàng hôn, đêm có ưng ý không: đây là [số tạm], ghi lại chỗ muốn chỉnh.

## Dừng và chuyển lại Opus khi
- Brief mơ hồ hoặc mâu thuẫn với code thật (vd số dòng lệch tới mức không xác định được chỗ sửa).
- Cần một quyết định thuộc AGENTS.md §3.4 hoặc ngoài "Quyết định đã chốt" (vd đổi bảng màu, đổi góc, thêm hiệu ứng).
- Cùng một lỗi sửa 2 lần vẫn chưa qua (gồm lỗi shader trên điện thoại).
- Phải sửa file ngoài "Phạm vi".
- Cần API Unity chưa có trong mục "API Unity dùng".
- `avgFps < 30` trên điện thoại: ghi số đo, dừng, không tự tối ưu.

---
<!-- Từ đây trở xuống: EXECUTOR ghi. -->

## Kết quả
- Bắt đầu: 2026-10-21 15:33
- Base commit: e55cce8
- Chạy bằng: claude-sonnet-5-5 · medium
- Kết thúc: 2026-10-21 15:48 · commit code de5fe38
- File đổi: tạo DaySky.cs, SkyRotation.cs, DayCycleView.cs, Resources/Config/time_m1.json, 4 file test trong Tests/Client (+.meta); sửa ConfigLoader.cs, UnderwaterView.cs, WorldEdgeFog.cs, SkyClouds.shader, BenchBuild.cs, ConfigTests.cs (+1 test), TerrainView.unity, SimView.unity, Bench.unity (EnsureScene tạo lại).
- Lệch brief:
  1. `StarsRotateWithSun`: `Vector3.Angle(...) < 0.01` fail ở pha 0,25 (0,028°) do sai số acos float khi 2 vector gần trùng. Đổi sang so khoảng cách vector `< 1.7e-4` (≈0,01°) — cùng ý nghĩa, các test khác giữ nguyên. Logic DaySky không đổi.
  2. SimView.unity: Unity lưu lại khi save nên diff có thêm giá trị mặc định cho component có sẵn (`landmarkIconSize`… của một MonoBehaviour khác, `tint` của UnderwaterView) và xếp lại thứ tự khối SimView — không đổi hành vi.
  3. Lệnh `grep -c "Game.Client.DayCycleView"` ở bước 7 đếm 2 mỗi scene (1 khối component + `m_EditorClassIdentifier`), không phải 1; mỗi scene có đúng 1 component (guid script khớp).
  4. Bước 10 điện thoại: **chưa chạy được** (xem Kết quả test) → chờ user.

## Kết quả test
Tất cả phần cần test **đạt**, trừ dòng Bench + điện thoại: **chờ user** (không đo được).
- Bản sao cấu hình + loader: `cmp` exit 0; `ConfigTests.TimeClientCopyMatchesDesignData` đạt (EditMode 340/340); `LoadTimeConfig` chạy qua DayCycleViewPlayTests/DayCycleWaterPlayTests.
- Ban ngày như cũ (`NoonMatchesOldSceneLook`), `SunPath`, `ContinuousIncludingWrap`, `NightIsDarkWithStars`, `StarsRotateWithSun` (xem lệch 1): đạt. `SkyRotationTests` (3): đạt.
- `FlyCameraWaterPlayTests`, `WorldBoundsPlayTests` (cũ): đạt, không sửa. `DayCycleWaterPlayTests` (1): đạt. `DayCycleViewPlayTests` (4): đạt. Chạy lọc 6 lớp: 19/19 đạt.
- Shader: `read_console` error 0 (chỉ có warning CS0618 FindObjectsSortMode giống các file cũ). Chưa xem trời bằng mắt (user xem).
- Scene: `DayCycleView` có trên `Main Camera` của TerrainView và SimView; Bench.unity có `debugKeys: 0`, `benchCycleSeconds: 5`.
- Bench APK: build `Bench build: Succeeded` (00:02:38 → Builds/bench.apk). **Cài/chạy không được**: không thấy thiết bị USB `PFQCRWVWR46HU4GQ`; `192.168.1.6:5555` lúc đầu hiện `device` nhưng `install` báo `adb: device offline`, `disconnect/connect` thì `failed to connect ... Operation timed out` (ping tới 192.168.1.6 vẫn thông). Không có số `DAYCYCLE`, `avgFps`, `hash` → **chờ user** cắm USB/bật lại ADB không dây rồi chạy lại bước 10.2–10.3 (APK đã build sẵn ở client/Builds/bench.apk, build từ commit de5fe38).
- Dọn: EditorSettings.asset, UnityConnectSettings.asset checkout lại; Bench.unity.meta không đổi.
- Cả bộ: EditMode 340/340 đạt; PlayMode 52/52 đạt. `check_design.py` hợp lệ. `git status` chỉ còn file trong Phạm vi (đã commit).

## Vướng mắc

---
<!-- PLANNER ghi khi review. -->

## Review
- Chạy bằng `claude-sonnet-5-5 · medium` khớp `Model thực thi: Sonnet · medium`.
- Phần cần test: đủ 13 dòng có kết quả; dòng Bench + điện thoại executor để `chờ user` → Planner chạy lại bước 10.2–10.3 (2026-10-21 15:50, `adb connect 192.168.1.6:5555` thành công, APK từ de5fe38): [run]
  - 10 dòng `DAYCYCLE` (≥ 6), 2 dòng đầu: `DAYCYCLE select=Midnight arcMs=29388260 fromMs=57011740 toMs=0`, `DAYCYCLE select=Noon arcMs=43194984 fromMs=5016 toMs=43200000`.
  - `BENCH`: avgFps 50,6 (≥ 30), p99Ms 33,29, holeFrames 0, tris 3889524, hash `a7719c8999939263 v3` trùng. 0 dòng `Exception`. `git status` sạch.
- Diff de5fe38 (`git show --stat`: 25 file, đều trong Phạm vi): `DayCycleView`, `SkyRotation`, `UnderwaterView`/`WorldEdgeFog`, `SkyClouds.shader`, `BenchBuild`, `ConfigLoader` đúng chữ ký và nội dung bước 3–6, 8; không đụng Core, Terrain/Ocean shader, `.mat`, `SimView.cs`, `TerrainBench.cs`.
- Lệch brief chấp nhận: (1) `StarsRotateWithSun` so khoảng cách vector < 1,7e-4 thay cho `Angle < 0.01` (acos float kém chính xác khi 2 vector gần trùng; cùng ngưỡng ~0,01°); (2) SimView.unity có thêm giá trị mặc định do Unity lưu, không đổi hành vi; (3) `grep -c` ra 2 do `m_EditorClassIdentifier` — lỗi đếm trong brief, mỗi scene đúng 1 component.
- Tiêu chí xong: đạt hết phần đo bằng lệnh (EditMode 340/340, PlayMode 52/52, điện thoại). Còn phần `user xem` (mục "User tự test" 1–5) → chưa kết luận.
- User xem (2026-10-21, Mac): mục "User tự test" 1–5 đạt hết, chưa chỉnh bảng màu [user].
- Giờ: Executor 15 phút (15:33–15:48); đo điện thoại + review đến 15:51, user duyệt sau đó. Cộng vào roadmap khi xong cả bước 7f (cùng 0022 và brief 3/3).
- Kết luận: đã duyệt
