# Brief 0033 — Sửa lệch màu sương ↔ bầu trời ở chân trời (gửi màu trời dạng linear)

> Trạng thái: chờ review
> Review: đầy đủ + user xem
> Model thực thi: Sonnet · medium
> Cập nhật: 2026-10-22 11:19

<!-- Planner (Opus) viết, Executor (Sonnet) làm theo ở PHIÊN KHÁC — executor không có lịch sử chat, không được tự quyết.
Trạng thái: chờ làm → đang làm → chờ review → đã duyệt; hoặc → cần Opus (executor dừng, hỏi ở mục Vướng mắc).
Review: nhẹ (đọc mục Kết quả) | đầy đủ (đọc cả diff) | user xem (có phần user tự kiểm bằng mắt/chơi thử) — có thể ghép "đầy đủ + user xem".
Model thực thi: BẮT BUỘC, đúng dạng `<Model> · <effort>` — Model: Opus | Sonnet | Haiku | Fable; effort: low | medium | high | xhigh | max (chọn theo AGENTS.md §10.2).
Executor kiểm tra model + effort của phiên TRƯỚC khi làm (scripts/brief_check.py); lệch hoặc không xác định được → cảnh báo user và DỪNG.
File được commit. Tự kiểm trước khi giao (AGENTS.md §10.3): executor có phải hỏi gì không? còn chỗ nào bỏ ngỏ 2 phương án? -->

## Mục tiêu
Làm theo [task contract](../tasks/m1-sua-mau-chan-troi.md): màu bầu trời (skybox `Game/SkyClouds`) và màu sương trùng nhau ở mọi pha trong ngày, để vật ở xa bị sương phủ kín tan vào trời. Hết mảng xanh phẳng (ban ngày) / khối tối (ban đêm) ở chân trời bên trái scene SimView; ban đêm sao phủ tới chân trời.

Ước lượng 0,25 ngày-người. Ghi giờ bắt đầu/kết thúc (theo `date`) vào mục Kết quả.

## Bối cảnh cần đọc
Đường dẫn tính từ `client/Assets/Game/`. Số dòng theo commit `ab50b06`.
- Nguyên nhân (đã chẩn đoán, xem task contract mục "Nguyên nhân"): project ở Linear color space. `RenderSettings.fogColor` được Unity đổi gamma → linear khi đưa lên shader (`unity_FogColor` = `fogColor.linear`, đo được), còn `Shader.SetGlobalColor` **không** đổi ([docs](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/Shader.SetGlobalColor.html): "unlike Material.SetColor, this function doesn't do color space conversion"). `DayCycleView` gửi cùng màu `s.horizon` theo cả hai đường → lệch.
- `Client/DayCycleView.cs:95-126` — `public void Apply()`. Đoạn cần sửa (`:114-122`), nguyên văn hiện tại:
  ```csharp
            if (underwater == null || !underwater.IsUnderwater)
                RenderSettings.fogColor = s.horizon;
            Shader.SetGlobalFloat(DayCycleOnId, 1f);
            Shader.SetGlobalColor(ZenithId, s.zenith);
            Shader.SetGlobalColor(HorizonId, s.horizon);
            Shader.SetGlobalColor(CloudId, s.cloud);
            Shader.SetGlobalColor(CloudShadeId, s.cloudShade);
            Shader.SetGlobalColor(SunColorId, s.sunColor);
            Shader.SetGlobalColor(MoonColorId, DaySky.MoonDiscColor);
  ```
  Tên biến global: `_DayZenith`, `_DayHorizon`, `_DayCloud`, `_DayCloudShade`, `_DaySunColor`, `_DayMoonColor` (`:31-40`).
- `Client/Shaders/SkyClouds.shader:105-129` — `frag`: dưới chân trời (`d.y <= 0`) trời = màu chân trời (`up = 0`), không mặt trời/sao (`above = smoothstep(0.0, 0.04, d.y)` = 0), không mây (`smoothstep(0.0, 0.18, d.y)` = 0); cuối hàm trộn `unity_FogColor` theo `_EdgeFogBlend`. Không sửa shader.
- `Client/Resources/Shaders/Terrain.shader:152` — địa hình trộn sương `MixFog(c, ComputeFogFactorZ0ToFar(distance(...)))`; ở xa hơn `fogEndDistance` thì màu = `unity_FogColor`. Không sửa.
- `Client/DaySky.cs:5-17` — `struct DaySkyState` (các trường `zenith, horizon, cloud, cloudShade, sunColor`); `:28` `public static readonly Color MoonDiscColor`; `:81` `public static DaySkyState Evaluate(float phase)`. Không sửa.
- Test hiện có: `Tests/Client/DayCycleViewPlayTests.cs` (setup `:25-40`: camera + `DayCycleView` + đèn directional, `clockMs = () => now`, `secondsNow = () => t`; `now = 12 * H` → pha 0,5, `now = 0` → pha 0; helper `Near(Color e, Color a, float tol = 1e-4f)` `:18-23`; test `FollowsClockAtNoonAndMidnight` `:50-64`). `Tests/Client/DayCycleWaterPlayTests.cs`, `Tests/Client/TimeDialPlayTests.cs` cũng dùng `DayCycleView`.
- Mẫu render ra ảnh có sẵn: `Client/Editor/TerrainViewCapture.cs:74-77` (bật sương tuyến tính) và `:178-187` (`new RenderTexture(...)`, `cam.targetTexture = rt; cam.Render(); RenderTexture.active = rt; tex.ReadPixels(...)`).
- API Unity dùng (6000.4):
  - `Color.linear` — [docs](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/Color-linear.html): "A color value in linear space converted from an sRGB value."
  - `Shader.SetGlobalColor` — [docs](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/Shader.SetGlobalColor.html).
  - `Shader.GetGlobalVector(string name)` — [docs](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/Shader.GetGlobalVector.html): `public static Vector4 GetGlobalVector(string name);`
  - `Shader.SetGlobalFloat`, `Shader.Find`, `GameObject.CreatePrimitive(PrimitiveType.Cube)`, `RenderSettings.skybox/fog/fogMode/fogStartDistance/fogEndDistance`, `Camera.Render`, `RenderTexture`, `Texture2D.ReadPixels` — đã dùng trong code repo (`TerrainViewCapture.cs`, test hiện có), không mới.

## Quyết định đã chốt (không được đổi)
1. **Cách A1 (user 2026-10-22):** gửi 6 màu bầu trời `_Day*` dạng `.linear`. `RenderSettings.fogColor` giữ nguyên `s.horizon` (Unity tự đổi). Không làm cách ngược lại (đổi màu sương sang `.gamma`).
2. Không đổi bảng màu khoá trong `DaySky` (giá trị gamma, như màu material trước brief 0023). Hệ quả chấp nhận: trời đậm hơn hiện nay một chút (về đúng màu trước 0023).
3. Không đổi far clip, khoảng sương, ngưỡng sao/mặt trăng trong shader, `UnderwaterView`, `WorldEdgeFog`, `OceanBackdrop`, shader `Ocean`/`Terrain`/`SkyClouds`.

## Phạm vi
- Được sửa/tạo:
  - `client/Assets/Game/Client/DayCycleView.cs`
  - `client/Assets/Game/Tests/Client/DayCycleViewPlayTests.cs`
  - `client/Assets/Game/Tests/Client/HorizonFogMatchPlayTests.cs` (mới, kèm `.meta` Unity tự tạo)
  - Mục Kết quả / Kết quả test / Vướng mắc của brief này.
- Không được sửa: mọi file khác (shader, `DaySky.cs`, scene, material, `design/`, roadmap).

## Các bước
1. **`Client/DayCycleView.cs`** — thay đoạn `:116-122` bằng nguyên văn:
   ```csharp
            Shader.SetGlobalFloat(DayCycleOnId, 1f);
            // Shader.SetGlobalColor không đổi gamma → linear như RenderSettings.fogColor, nên gửi .linear để chân trời trùng màu sương (task m1-sua-mau-chan-troi).
            Shader.SetGlobalColor(ZenithId, s.zenith.linear);
            Shader.SetGlobalColor(HorizonId, s.horizon.linear);
            Shader.SetGlobalColor(CloudId, s.cloud.linear);
            Shader.SetGlobalColor(CloudShadeId, s.cloudShade.linear);
            Shader.SetGlobalColor(SunColorId, s.sunColor.linear);
            Shader.SetGlobalColor(MoonColorId, DaySky.MoonDiscColor.linear);
   ```
   Các dòng khác của `Apply` giữ nguyên. Xong khi: biên dịch không lỗi (`read_console` không có error).

2. **`Tests/Client/DayCycleViewPlayTests.cs`** — trong `FollowsClockAtNoonAndMidnight`:
   - Sau dòng `Assert.AreEqual(1f, Shader.GetGlobalFloat("_DayCycleOn"));` (`:57`) thêm:
     ```csharp
            var noon = DaySky.Evaluate(0.5f);
            Near(noon.horizon.linear, (Color)Shader.GetGlobalVector("_DayHorizon"));
            Near(noon.zenith.linear, (Color)Shader.GetGlobalVector("_DayZenith"));
            Near(noon.cloud.linear, (Color)Shader.GetGlobalVector("_DayCloud"));
            Near(noon.cloudShade.linear, (Color)Shader.GetGlobalVector("_DayCloudShade"));
            Near(noon.sunColor.linear, (Color)Shader.GetGlobalVector("_DaySunColor"));
            Near(DaySky.MoonDiscColor.linear, (Color)Shader.GetGlobalVector("_DayMoonColor"));
            Near(RenderSettings.fogColor.linear, (Color)Shader.GetGlobalVector("_DayHorizon"));
     ```
   - Sau dòng `Near(DaySky.Evaluate(0f).horizon, RenderSettings.fogColor);` (`:62`) thêm:
     ```csharp
            Near(DaySky.Evaluate(0f).horizon.linear, (Color)Shader.GetGlobalVector("_DayHorizon"));
            Near(RenderSettings.fogColor.linear, (Color)Shader.GetGlobalVector("_DayHorizon"));
     ```
   Xong khi: test này đạt.

3. **`Tests/Client/HorizonFogMatchPlayTests.cs`** (mới) — namespace `Game.Client.Tests`, lớp `HorizonFogMatchPlayTests`, summary: `/// <summary>Task m1-sua-mau-chan-troi: vật bị sương phủ kín và trời ngay dưới chân trời cùng màu (sương và skybox Game/SkyClouds nhận màu chân trời như nhau).</summary>`. Cấu trúc:
   - Trường lưu để khôi phục ở TearDown: `RenderSettings.skybox`, `RenderSettings.fog`, `RenderSettings.fogMode`, `RenderSettings.fogStartDistance`, `RenderSettings.fogEndDistance`, `RenderSettings.fogColor`.
   - `[UnitySetUp] SetUp()`:
     - `RenderSettings.skybox = new Material(Shader.Find("Game/SkyClouds"));` `RenderSettings.fog = true; RenderSettings.fogMode = FogMode.Linear; RenderSettings.fogStartDistance = 1f; RenderSettings.fogEndDistance = 2f;` `Shader.SetGlobalFloat("_EdgeFogBlend", 0f);`
     - Đèn: `new GameObject("Light")` + `Light` `type = LightType.Directional`.
     - Camera: `new GameObject("Cam")` + `Camera` (`clearFlags = CameraClearFlags.Skybox`, `fieldOfView = 60`, `nearClipPlane = 0.1f`, `farClipPlane = 128f`), vị trí `Vector3.zero`, xoay `Quaternion.identity` (nhìn ngang về +z). Thêm `DayCycleView` vào camera, `sun = light`, `clockMs = () => now`, `secondsNow = () => 100.0` (trường `long now`, `const int H = 3_600_000`).
     - Khối: `GameObject.CreatePrimitive(PrimitiveType.Cube)`, `position = (0, -30, 100)`, `localScale = (400, 40, 2)` (mặt trên ở y = −10), `GetComponent<MeshRenderer>().sharedMaterial = new Material(Shader.Find("Game/Terrain"))`.
     - `yield return null;`
   - `[UnityTearDown]`: `Object.Destroy` camera, đèn, khối, hai material đã tạo; khôi phục các giá trị `RenderSettings` đã lưu; `yield return null`.
   - Hàm phụ `Color32[] RenderPixels(Camera cam)`: `var rt = new RenderTexture(256, 256, 24); cam.targetTexture = rt; cam.Render(); RenderTexture.active = rt; var tex = new Texture2D(256, 256, TextureFormat.RGBA32, false); tex.ReadPixels(new Rect(0, 0, 256, 256), 0, 0); tex.Apply();` lấy `tex.GetPixels32()`; dọn `cam.targetTexture = null; RenderTexture.active = null;` `Object.Destroy(rt); Object.Destroy(tex);`. Điểm ảnh (x, y) = `px[y * 256 + x]` (y = 0 là hàng dưới cùng).
   - Hai điểm lấy mẫu (x = 128): **trời** y = 120 (hơi dưới chân trời, tia đi qua phía trên mặt khối → skybox); **khối** y = 64 (tia chạm mặt trước khối ở khoảng cách ~100 > fogEnd → phủ sương kín).
   - Hàm phụ so màu: lệch ≤ 2 mỗi kênh R, G, B (byte 0–255), message ghi cả hai màu.
   - `[UnityTest] SkyBelowHorizonMatchesFoggedGeometry_Noon()`: `now = 12 * H; view.Apply();` → `RenderPixels` → (a) màu trời ≈ màu khối (≤ 2/255); (b) màu khối ≈ `(Color32)RenderSettings.fogColor` (≤ 2/255).
   - `[UnityTest] SkyBelowHorizonMatchesFoggedGeometry_Midnight()`: như trên với `now = 0`.
   - `[UnityTest] CubeIsRenderedWhenFogOff()` (kiểm tra mẫu khối thật sự chạm khối): `now = 12 * H; view.Apply(); RenderSettings.fog = false;` → `RenderPixels` → màu khối khác `(Color32)RenderSettings.fogColor` ít nhất 13 (≈ 0,05) ở ít nhất một kênh.
   Xong khi: 3 test đạt sau bước 1. Kiểm thêm: tạm hoàn tác bước 1 (`git stash` KHÔNG dùng — chỉ sửa tay `s.horizon.linear` → `s.horizon` ở dòng `_DayHorizon`, chạy riêng lớp này, ghi kết quả, rồi sửa lại) → hai test `SkyBelowHorizon...` phải **không đạt** (chứng minh test bắt được lỗi). Ghi kết quả cả hai lần vào Kết quả test.

4. Chạy toàn bộ test (mục "Lệnh executor chạy"), ghi kết quả, dọn `EditorSettings.asset`.

## Tiêu chí xong
- `DayCycleView.Apply` gửi 6 màu `_Day*` dạng `.linear`; `RenderSettings.fogColor` vẫn = `s.horizon`.
- `DayCycleViewPlayTests` (đã thêm assert) và 3 test `HorizonFogMatchPlayTests` đạt; hai test `SkyBelowHorizon...` không đạt khi tạm bỏ `.linear` ở `_DayHorizon`.
- Toàn bộ EditMode + PlayMode hết đỏ. `git status` chỉ còn 3 file trong Phạm vi (+ `.meta` mới) và brief này.

## Test sau khi xong

### Phần cần test
- `DayCycleView` gửi màu trời dạng linear · `DayCycleViewPlayTests.FollowsClockAtNoonAndMidnight` · đạt; `_DayHorizon` = `fogColor.linear` ở pha 0,5 và 0.
- Trời dưới chân trời trùng màu vật bị sương phủ kín (ngày, đêm) · `HorizonFogMatchPlayTests.SkyBelowHorizonMatchesFoggedGeometry_Noon` / `_Midnight` · đạt với code mới; không đạt khi tạm bỏ `.linear` ở `_DayHorizon`.
- Mẫu điểm ảnh khối thật sự là khối · `HorizonFogMatchPlayTests.CubeIsRenderedWhenFogOff` · đạt.
- Phần cũ có thể ảnh hưởng — sương/dưới nước theo pha · `DayCycleWaterPlayTests` · đạt, không sửa.
- Phần cũ — vòng chọn mốc, xoay trời · `TimeDialPlayTests`, các test còn lại của `DayCycleViewPlayTests` · đạt, không sửa.
- Phần cũ — các scene SimView/TerrainView/Bench, minimap, sim · toàn bộ PlayMode + EditMode · hết đỏ (số test = số trước + 3 PlayMode mới).

### Lệnh executor chạy
1. Nếu Editor đang ở Play mode: `manage_editor` action `stop` trước khi chạy test.
2. `run_tests` (MCP) PlayMode, lọc lớp `Game.Client.Tests.HorizonFogMatchPlayTests` và `Game.Client.Tests.DayCycleViewPlayTests` → đạt hết.
3. Kiểm "test bắt được lỗi" như bước 3 (sửa tạm, chạy riêng `HorizonFogMatchPlayTests`, sửa lại).
4. `run_tests` EditMode toàn bộ → dòng tổng kết.
5. `run_tests` PlayMode toàn bộ → dòng tổng kết.
6. `git checkout -- client/ProjectSettings/EditorSettings.asset`
7. `git status --short` → chỉ file trong Phạm vi.

### User tự test
Mở scene `client/Assets/Game/Client/Scenes/SimView.unity`, bấm Play trên Mac. Camera đặt ở trại demo nhìn về +z, nghiêng 35°; kéo chuột giữa để ngẩng lên thấy chân trời (phía trái là biển).
1. Ban ngày (vòng chọn mốc, phím T → Trưa): phía trái trên mặt biển xa **không còn** mảng xanh phẳng cạnh cứng; biển/đồi xa tan dần vào trời.
2. Ban đêm (T → Nửa đêm): **không còn** khối tối che trời bên trái; sao phủ xuống tới chân trời.
3. Bình minh và hoàng hôn: màu trời, mây, đĩa mặt trời vẫn chấp nhận được (trời sẽ đậm hơn trước một chút — về đúng màu trước brief 0023).

## Dừng và chuyển lại Opus khi
- Brief mơ hồ hoặc mâu thuẫn với code thật.
- Cần một quyết định thuộc AGENTS.md §3.4 hoặc ngoài "Quyết định đã chốt".
- Cùng một lỗi sửa 2 lần vẫn chưa qua (vd test render đạt cả khi bỏ `.linear`, hoặc không đạt với code mới).
- Phải sửa file ngoài "Phạm vi".

---
<!-- Từ đây trở xuống: EXECUTOR ghi. -->

## Kết quả
Base commit: fe941b6
Chạy bằng: claude-sonnet-5-5 · medium
Bắt đầu: 2026-10-22 11:13 · Kết thúc: 2026-10-22 11:19 (~0,1 giờ; ước lượng 0,25 ngày-người)
Commit: 78ff704 — `Client/DayCycleView.cs`, `Tests/Client/DayCycleViewPlayTests.cs`, `Tests/Client/HorizonFogMatchPlayTests.cs` (+ `.meta`).

## Kết quả test
- `DayCycleView` gửi màu linear · `DayCycleViewPlayTests.FollowsClockAtNoonAndMidnight` · đạt (PlayMode lớp: 7/7 cùng HorizonFogMatch).
- Trời = khối bị sương phủ kín (trưa, đêm) · `HorizonFogMatchPlayTests.SkyBelowHorizon..._Noon/_Midnight` · đạt với code mới. Khi tạm bỏ `.linear` ở `_DayHorizon`: không đạt cả hai (trưa R 229 vs 199; đêm R 80 vs 20) → test bắt được lỗi; đã sửa lại `.linear`.
- Mẫu điểm ảnh là khối · `CubeIsRenderedWhenFogOff` · đạt.
- Phần cũ (DayCycleWater, TimeDial, ...) · đạt, nằm trong chạy toàn bộ.
- EditMode toàn bộ: 400/400 đạt. PlayMode toàn bộ: 94/94 đạt (chưa kể số "trước + 3": không có số trước để đối chiếu).
- `git checkout -- client/ProjectSettings/EditorSettings.asset` đã chạy; `git status` chỉ còn file trong Phạm vi + brief.
- User tự xem (chờ user): SimView ngày/đêm/bình minh–hoàng hôn như mục "User tự test".

## Vướng mắc

---
<!-- PLANNER ghi khi review. -->

## Review
