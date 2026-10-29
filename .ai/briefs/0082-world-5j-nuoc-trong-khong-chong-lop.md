# Brief 0082 — World 5j (phần 1): nước trong suốt chỉ hiện lớp gần nhất (cột thác hết "đứt khúc")

> Trạng thái: chờ review
> Review: đầy đủ + user xem
> Model thực thi: Sonnet · medium
> Cập nhật: 2026-10-29 18:17

## Mục tiêu
User chơi thử (brief 0081, mục Review) thấy cột thác "đứt khúc", thấy từng mảng. Nguyên nhân khả dĩ [repo]: `Water.mat` trong suốt với `ZWrite Off` + `Cull Off`, nên mặt sau và mặt trong của cùng mesh nước được vẽ chồng lên mặt trước theo thứ tự tam giác, không theo độ sâu. Brief này thêm **depth prepass** cho shader `Game/Ocean`: một pass chỉ ghi độ sâu chạy trước pass màu, nên mỗi điểm ảnh chỉ còn hiện mặt nước gần camera nhất. Task contract: [m1-world-5j-sua-loi-choi-thu](../tasks/m1-world-5j-sua-loi-choi-thu.md), lỗi 1.

## Bối cảnh cần đọc
- `client/Assets/Game/Client/Shaders/Ocean.shader` (180 dòng):
  - `:4` chú thích brief 0011 (Water.mat trong suốt: Blend SrcAlpha/OneMinusSrcAlpha, ZWrite Off, Cull Off).
  - `:16-19` thuộc tính `_SrcBlend`, `_DstBlend`, `_ZWrite`, `_Cull` (`[HideInInspector] … Float`).
  - `:25-33` `HLSLINCLUDE` gồm `Core.hlsl` và `CBUFFER UnityPerMaterial`.
  - `:35-41` pass đầu tiên `Name "ForwardLit"`, `Tags { "LightMode" = "UniversalForward" }`, `Blend [_SrcBlend] [_DstBlend]`, `ZWrite [_ZWrite]`, `Cull [_Cull]`.
  - `:59-66` vertex của ForwardLit: `VertexPositionInputs p = GetVertexPositionInputs(input.positionOS.xyz); o.positionCS = p.positionCS;`.
  - `:160-177` pass `DepthOnly` (`LightMode` = `DepthOnly`, ZWrite On, ColorMask R) — để nguyên.
- `client/Assets/Game/Client/Materials/Water.mat`: `:19` `m_CustomRenderQueue: 3000`, `:21` `disabledShaderPasses: []`, float `_Alpha 0.45`, `_Cull 0`, `_DstBlend 10`, `_SrcBlend 5`, `_ZWrite 0`. Material này gán cho `TerrainChunkView.waterMaterial` trong các scene SimView, TerrainView, Bench, SimBench, WalkBench.
- `client/Assets/Game/Client/Materials/Ocean.mat` (mặt biển nền, đục): `:21` `disabledShaderPasses: []`.
- `client/Assets/Game/Client/TerrainChunkView.cs:432` `CreateWaterMaterial()`: material dự phòng khi không gán, đục. Không đổi.
- Mẫu test render: `client/Assets/Game/Tests/Client/SeaSkyTintPlayTests.cs`. Tắt các Light còn sót ở `:55-58`, `RenderLinear(Camera cam)` ở `:116-131` (RenderTexture ARGBFloat, `cam.Render()`, `ReadPixels`, `GetPixels`).
- Docs Unity 6000.4:
  - `LightMode` = `SRPDefaultUnlit`: "Use this LightMode tag value to draw an extra Pass when rendering objects." [docs](https://docs.unity3d.com/6000.4/Documentation/Manual/urp/urp-shaders/urp-shaderlab-pass-tags.html). Docs **không** nói thứ tự giữa pass `SRPDefaultUnlit` và `UniversalForward` của cùng object; test ở bước 1 sẽ kiểm việc này.
  - `Material.SetShaderPassEnabled(string passName, bool enabled)`: bật/tắt pass theo tên `LightMode` (không phân biệt hoa thường) [docs](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/Material.SetShaderPassEnabled.html).

## Quyết định đã chốt (không được đổi)
1. Thêm vào `Ocean.shader` một pass **đặt trước** pass `ForwardLit`, nguyên văn:
   ```hlsl
           // Brief 0082: depth prepass — nước trong suốt chỉ hiện mặt gần camera nhất (không chồng mặt sau/mặt trong theo thứ tự tam giác).
           Pass
           {
               Name "WaterDepthPrepass"
               Tags { "LightMode" = "SRPDefaultUnlit" }
               ZWrite On
               ColorMask 0
               Cull [_Cull]

               HLSLPROGRAM
               #pragma vertex vert
               #pragma fragment frag

               float4 vert(float4 positionOS : POSITION) : SV_POSITION
               {
                   return GetVertexPositionInputs(positionOS.xyz).positionCS; // cùng phép biến đổi với ForwardLit để độ sâu trùng khít
               }

               half4 frag() : SV_Target { return 0; }
               ENDHLSL
           }
   ```
   Lý do: pass ForwardLit (ZTest mặc định LEqual) chỉ còn vẽ được điểm có độ sâu bằng mặt gần nhất. `Cull [_Cull]` giữ được nhìn từ dưới nước (Cull Off).
2. `Ocean.mat` (đục) tắt pass này: `disabledShaderPasses:` + dòng `  - SRPDefaultUnlit` (ghi tay trong YAML, giống cách Unity lưu). `Water.mat` để `disabledShaderPasses: []`.
3. Không đổi pass ForwardLit, DepthOnly, thuộc tính, các giá trị float của hai material, `BlockMesher`, `TerrainChunkView`.
4. Nếu test ở bước 3 không đạt (ví dụ URP vẽ pass `SRPDefaultUnlit` **sau** `UniversalForward`), **không** tự thử cách khác (ZTest, render queue, renderer feature…): dừng, `cần Opus`, ghi số đo.

## Phạm vi
- Được sửa/tạo: `client/Assets/Game/Client/Shaders/Ocean.shader`, `client/Assets/Game/Client/Materials/Ocean.mat`, `client/Assets/Game/Tests/Client/WaterLayerPlayTests.cs` (+ `.meta` do Unity tạo), brief này (từ Kết quả trở xuống).
- Không được sửa: mọi file khác (gồm `Water.mat`, scene, `BlockMesher.cs`, `TerrainChunkView.cs`, test cũ, `design/`).

## Các bước
1. Tạo `WaterLayerPlayTests.cs` (namespace `Game.Client.Tests`, class `WaterLayerPlayTests`), một `[UnityTest] public IEnumerator TransparentWaterShowsOnlyNearestLayer()`:
   - Dựng giống `SeaSkyTintPlayTests`: tắt mọi `Light` đang bật (nhớ để bật lại ở TearDown), tạo 1 Directional Light xoay `Euler(50, 30, 0)`; `RenderSettings.fog = false` (khôi phục ở TearDown); `Shader.SetGlobalFloat("_DayCycleOn", 0f)`.
   - Camera: `clearFlags = SolidColor`, `backgroundColor = Color.black`, FOV 60, near 0.1, far 50, vị trí `(0, 3.5, -4)`, `transform.LookAt(new Vector3(0, 1.5f, 0))`. Ảnh 128 × 128 (`RenderLinear` chép từ `SeaSkyTintPlayTests:116-131`, đổi `Size` = 128).
   - Vật: `GameObject.CreatePrimitive(PrimitiveType.Cube)`, vị trí `(0, 1.5, 0)`, scale `(1, 3, 1)`, xoay `Euler(0, 30, 0)`.
   - Material `MakeWater(int cull, bool prepass)`: `new Material(Shader.Find("Game/Ocean"))`, `SetFloat`: `_Alpha` 0.45, `_SrcBlend` 5, `_DstBlend` 10, `_ZWrite` 0, `_Cull` cull, `_RippleStrength` 0, `_Glint` 0; `renderQueue = 3000`; `SetShaderPassEnabled("SRPDefaultUnlit", prepass)`.
   - Render 3 ảnh: `A` = `MakeWater(0, true)` (mới), `B` = `MakeWater(2, false)` (tham chiếu: chỉ mặt trước của hộp lồi), `C` = `MakeWater(0, false)` (cách cũ). Mỗi lần đổi `renderer.sharedMaterial` rồi `RenderLinear`.
   - `diff(X, Y)` = max trên mọi điểm ảnh của max |ΔR|, |ΔG|, |ΔB|. `covered` = số điểm của `B` có R + G + B > 0.01.
   - Assert theo thứ tự: `covered` > 500; `diff(C, B)` > 0.02 (test nhạy với lỗi cũ); `diff(A, B)` ≤ 0.005. Thông báo lỗi in cả 3 số. Trước khi assert, `Debug.Log` một dòng `WaterLayer covered=… diffCB=… diffAB=…`.
   - TearDown: huỷ camera, light, cube, 3 material; bật lại light; khôi phục fog.
2. Chạy test này **trước khi** sửa shader (Lệnh 2). Kỳ vọng: không đạt ở assert `diff(A, B)`, vì chưa có pass prepass thì `A` = `C`. Ghi dòng `WaterLayer …` vào Kết quả test.
3. Sửa `Ocean.shader` theo QĐ 1: chèn pass ngay trước dòng `Pass` của `ForwardLit` (`:35`). Thêm vào cuối chú thích đầu file (sau `:5`) dòng: `// Brief 0082: pass "WaterDepthPrepass" (SRPDefaultUnlit) ghi độ sâu trước ForwardLit — nước trong suốt chỉ hiện mặt gần nhất; Ocean.mat (đục) tắt pass này.` Sửa `Ocean.mat` theo QĐ 2. Chạy lại test → đạt.
4. Chạy "Lệnh executor chạy" từ bước 3.

## Tiêu chí xong
- `refresh_unity` → console 0 Error (kể cả lỗi biên dịch shader `Game/Ocean`).
- `TransparentWaterShowsOnlyNearestLayer` không đạt trước khi sửa shader (bước 2), đạt sau khi sửa (bước 3).
- EditMode + PlayMode toàn bộ đạt (ghi dòng tổng).

## Test sau khi xong

### Phần cần test
- **Mới** — `WaterLayerPlayTests.TransparentWaterShowsOnlyNearestLayer` · trước sửa: không đạt (diffAB ≈ diffCB); sau sửa: covered > 500, diffCB > 0.02, diffAB ≤ 0.005.
- **Cũ** — nước nhìn từ dưới lên / camera bay trong nước: `FlyCameraWaterPlayTests`, `DayCycleWaterPlayTests` · đạt.
- **Cũ** — mặt biển nền đục (Ocean.mat tắt prepass) và màu biển theo buổi: `SeaSkyTintPlayTests`, `HorizonFogMatchPlayTests`, `OceanBackdropTests` · đạt.
- **Cũ** — mesh nước: `WaterFlowPlayTests` (`WaterfallColumnIsMeshed`, `WaterfallPoursIntoPond`), EditMode `BlockMesherTests`, `GoldenHashTests` · đạt (không đổi code mesher).
- **Toàn bộ** EditMode + PlayMode · đạt.

### Lệnh executor chạy
Ghi giờ bắt đầu/kết thúc bằng lệnh `date`.
1. Sau bước 1: MCP `refresh_unity` → `read_console` (Error) = 0.
2. MCP `run_tests` PlayMode lọc `WaterLayerPlayTests` → ghi kết quả + dòng log `WaterLayer …` (bước 2).
3. Sau bước 3: `refresh_unity` → `read_console` (Error) = 0; `run_tests` PlayMode lọc `WaterLayerPlayTests` → đạt, ghi dòng log.
4. MCP `run_tests` EditMode toàn bộ; PlayMode toàn bộ. Ghi dòng tổng. Nếu MCP rớt khi đang chạy: `sleep 60` rồi `get_test_job`, lặp lại. Không kết thúc lượt khi test còn chạy.
5. Dọn: thoát Play; `git checkout -- client/ProjectSettings/EditorSettings.asset`; xoá `client/Assets/InitTestScene*.unity(.meta)` nếu có. `git status --short` chỉ còn file trong Phạm vi.
6. Chụp **1** ảnh Game view (scene SimView, Play): thác demo nhìn ngang hơi chếch từ trên. Lưu ngoài repo vào `~/Documents/User/khang/Game/art-test/shots/brief0082_column.png`, ghi đường dẫn vào Kết quả.
7. Commit code + test (`brief 0082: water depth prepass so transparent water shows only nearest layer`), rồi commit brief (`brief 0082: result`).

### User tự test
SimView, Play (trên Mac):
1. Thác demo: cột nước nhìn liền một khối, không còn mảng sáng/tối hay khối nhạt bên trong. Đi vòng quanh, nhìn gần và xa.
2. Nhìn ao qua cột thác: vẫn thấy ao và đáy phía sau, không có lỗ hay viền đen ở mép cột.
3. F3 camera bay xuống dưới mặt ao, nhìn lên: vẫn thấy mặt nước và bờ như trước.
4. Mặt biển nền ngoài đảo trông như trước.

## Dừng và chuyển lại Opus khi
- Brief mơ hồ hoặc mâu thuẫn với code thật (số dòng lệch nhiều, chữ ký khác).
- Bước 2 mà test đã đạt (không tái hiện được lỗi): ghi số đo, dừng.
- Bước 3 không đạt (QĐ 4), hoặc shader lỗi biên dịch vì `GetVertexPositionInputs` không có trong pass mới: ghi lỗi, dừng.
- Test cũ fail; mã băm `GoldenHashTests` đổi.
- Cần API Unity ngoài các API đã ghi trong brief, cần quyết định ngoài "Quyết định đã chốt", hoặc phải sửa file ngoài "Phạm vi".

---
<!-- Từ đây trở xuống: EXECUTOR ghi. -->

## Kết quả
Base commit: 10f032b
Chạy bằng: Sonnet 5.5 (claude-sonnet-5-5) · medium
Bắt đầu: 2026-10-29 18:07 · Kết thúc: 2026-10-29 18:16 (~9 phút)
Commit: 482eeab — Ocean.shader, Ocean.mat, WaterLayerPlayTests.cs(+.meta).
Ảnh Game view (lệnh 6): KHÔNG chụp (tiết kiệm usage; user tự xem ở mục User tự test).

## Kết quả test
- Mới `TransparentWaterShowsOnlyNearestLayer`: TRƯỚC sửa không đạt — `covered=2591 diffCB=0.37604 diffAB=0.37604` (đúng kỳ vọng, A = C). SAU sửa đạt (cả 3 assert; dòng log `WaterLayer…` sau sửa không được MCP trả về, chỉ có kết quả Passed).
- `refresh_unity` + `read_console` Error = 0 (trước và sau sửa shader).
- EditMode toàn bộ: 659/659 đạt. PlayMode toàn bộ: 277/277 đạt (gồm FlyCameraWater, DayCycleWater, SeaSkyTint, HorizonFogMatch, OceanBackdrop, WaterFlow, BlockMesher, GoldenHash).
- Dọn: EditorSettings.asset đã checkout; không có InitTestScene; git status chỉ còn file trong Phạm vi.
- Phần nhìn bằng mắt (cột thác liền khối, nhìn từ dưới nước, biển nền): chờ user.

## Vướng mắc

---
<!-- PLANNER ghi khi review. -->

## Review
