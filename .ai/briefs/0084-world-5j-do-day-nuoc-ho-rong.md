# Brief 0084 — World 5j (lỗi 2–3): mặt nước đậm/nhạt theo độ dày; test ghi lại hành vi hố rộng của 0043

> Trạng thái: đã duyệt
> Review: đầy đủ + user xem (gộp cuối 5j)
> Model thực thi: Sonnet · medium
> Cập nhật: 2026-10-29 20:10

## Mục tiêu
Lỗi 2 và 3 của task [5j](../tasks/m1-world-5j-sua-loi-choi-thu.md):
- **Lỗi 3 (mức ≥ 2 trông như nhau):** mesher ghi **độ dày nước** vào alpha màu đỉnh của mặt nước hướng lên. Shader nước dùng độ dày đó: nước mỏng nhạt màu và trong hơn, nước dày giữ như cũ. Nhờ vậy các mức 0–5 nhìn ra khác nhau.
- **Lỗi 2 (hố rộng không đầy):** brief này **không đổi luật**. Chỉ thêm test ghi lại hành vi hiện tại của 0043: hố rộng chỉ có nước trong bán kính mức. Muốn đổi thì phải có quyết định mới của user; Planner đã hỏi user ngoài brief.

## Bối cảnh cần đọc
**Planner đã chẩn đoán (2026-10-29, MCP `execute_code` ở EditMode, không commit):**
- **Lỗi 2 — code làm đúng luật 0043, nhưng luật không lấp đầy hố rộng.** Dựng thế giới `Flat(3)` 16×16×16, ao nguồn x 4..7, z 4..7, y = 3. Đào hố 2 lớp (y 3 và 2), x 8..15, z 2..9, rồi `SettleWater`. Kết quả hàng z = 5 [run]:
  - lớp trên y = 3: x 8..12 mức 4, 3, 2, 1, 0; x 13..15 trống;
  - lớp dưới y = 2: (8,2,5) rơi mức 5, x 9..13 mức 4, 3, 2, 1, 0; x 14, 15 trống.

  Nguyên nhân: mỗi ô lan giảm 1 mức (0043 QĐ 2.3), tăng mức bởi tường chỉ áp cho ô kề ≥ 2 khối đặc (QĐ 3). Vì vậy trong hố rộng nước chỉ tới tối đa 5 ô tính từ chỗ vào. Đáy xa chỉ có nước mức 0–1, cao 0,15–0,3, đúng như ảnh user `~/Documents/User/khang/Game/art-test/shots/user0081_hole.webp`. Test cũ `WideDeepPitFills` chỉ dùng hố 3×3, nên không bắt được trường hợp này.
- **Lỗi 3 — độ cao mesh đúng công thức, nhưng shader không thể hiện độ dày.** Hành lang mức 5..0: đỉnh mặt trên ra 3,90 / 3,75 / 3,60 / 3,45 / 3,30 / 3,20, tức 0,15 mỗi mức, đúng `0,9 × (L + 1) / 6` [run]. Tuy vậy, shader nước dùng alpha cố định (`_Alpha` 0,45) và màu chỉ theo góc nhìn (`Ocean.shader:158`). Nước 0,45 ô và 0,9 ô vì thế trông như nhau. Chỉ ở mép mức 0 mới thấy mặt nước sát đáy.

**Code:**
- `client/Assets/Game/World/BlockMesher.cs`:
  - `public const float WaterSurfaceDrop = 0.1f;` (`:43`) — thêm hằng mới ngay dưới dòng này.
  - Job có `public int3 Origin;` (`:413`). Toạ độ đỉnh là toạ độ chunk, toạ độ thế giới = `p + (float3)Origin` (xem `WaterVertexWorld` `:1003`).
  - `ushort CellAt(int3 p)` (`:1506`) nhận toạ độ thế giới: y < 0 → Bedrock, y ≥ SizeY → 0, x/z kẹp về biên.
  - `float WaterHeight(int3 p)` (`:1518`), `float CornerHeight(int3 g, int dx, int dz)` (`:1531`).
  - `void AddWaterSurface(int3 c, int3 g)` (`:1474`): mặt trên, pháp tuyến `(0,1,0)`.
  - `int AddWaterVertex(float3 p, float3 n)` (`:1572–1580`): hàn đỉnh theo khoá vị trí + pháp tuyến, rồi gọi `AddVertex(p, n, 0)`. **Màu đỉnh nước hiện là 0.**
  - `int AddVertex(float3 p, float3 n, uint color)` (`:1582`).
- `client/Assets/Game/World/ChunkMesh.cs:10`: `Colors` là RGBA 4 byte gói trong uint, byte thấp = R. Byte cao (bit 24–31) là A.
- `client/Assets/Game/World/ChunkMeshWriter.cs:38`: stream 2 = `VertexAttribute.Color, UNorm8 × 4`, dùng chung cho cả submesh nước. Shader đọc bằng semantic `COLOR` (float4, 0..1), ví dụ `Terrain.shader:60`.
- `client/Assets/Game/Client/Shaders/Ocean.shader` (dùng cho cả `Water.mat` trong suốt và `Ocean.mat` đục):
  - Properties `:11–20`; `CBUFFER_START(UnityPerMaterial)` `:29–33` (trong `HLSLINCLUDE`, cả 3 pass dùng chung);
  - pass `ForwardLit`: `struct Attributes` `:75`, `struct Varyings` `:76–80`, `vert` `:82–89`;
  - `frag`: `half3 baseCol = …` `:158`, `half alpha = …; return half4(c, alpha);` `:182–183`.
  - Pass `WaterDepthPrepass` và `DepthOnly` **không đổi**.
- `client/Assets/Game/Client/Materials/Water.mat:26–34`: `m_Floats` xếp theo ABC (`_Alpha: 0.45` …). `Ocean.mat`: **không sửa**. Thuộc tính mới không có trong file thì lấy giá trị mặc định của shader.
- Test:
  - `client/Assets/Game/Tests/World/BlockMesherTests.cs`: `StoneFloor16()` (`:921`, đá ở y = 4), `HasWaterVertex(mesh, normal, pos)` (`:930`), `AssertNoDuplicateWaterVertices(mesh)` (`:952`), mẫu dựng mesh `FlowingWaterSurfaceFollowsLevel` (`:1297`, `BlockMesher.Build(w, 0, 0, 0, true, 5)`);
  - `client/Assets/Game/Tests/Core/WaterFlowTests.cs`: `Flat`, `Editor`, `Settle`, `Dig`, `IsWater`, `PondWorld()` (`:10–51`), `WideDeepPitFills` (`:96`).
- `Cell.MakeFlowing(ushort cell, int level, bool falling)` (`Cell.cs:83`). `Cell.Make(BlockMaterial.Water, Cell.MaxWaterLevel)` = ô nguồn.
- Docs: semantic `COLOR` = "The per-vertex color", float4 [docs](https://docs.unity3d.com/6000.4/Documentation/Manual/SL-HLSLSemantics.html). Không dùng API Unity nào khác.
- Lưu ý từ review 0079: sau khi sửa `BlockMesher.cs`, Burst/assembly `Game.World` có thể chưa nạp bản mới dù đã `refresh_unity`. Nếu test mesh mới fail với giá trị như code cũ (A = 0), thêm rồi bỏ một dòng chú thích trong `BlockMesher.cs` để ép biên dịch lại.

## Quyết định đã chốt (không được đổi)
Ký hiệu `E` = `WaterDepthEncodeMax` = 4.

1. **Độ dày** tại đỉnh mặt trên `p` (toạ độ chunk), `wp = p + Origin`:
   - `ly = floor(wp.y − 1e-3)`;
   - các cột: x trong `{floor(wp.x − 1e-3) .. floor(wp.x + 1e-3)}` × z tương tự, tức 1, 2 hoặc 4 cột chạm điểm đó;
   - với mỗi cột có ô `(x, ly, z)` là nước: `k` = số ô nước liền ngay bên dưới (`ly − 1`, `ly − 2`, …), dừng ở ô không phải nước hoặc khi `k = E`; `floor_i = ly − k`;
   - độ dày = `wp.y − trung bình(floor_i)`; không có cột nào là nước → `E`.

   Độ dày chỉ phụ thuộc vị trí đỉnh, nên đỉnh hàn chung luôn cùng giá trị.
2. **Mã hoá:** `color = 0x00FFFFFF | (round(255 · saturate(độ dày / E)) << 24)`. Chỉ đỉnh nước có `n.y > 0.999` mới tính độ dày; mọi đỉnh nước khác (mặt bên, đáy, cột rơi, cung bo) có A = 255.
3. **Shader:** `t = lerp(1, saturate(color.a · E / _DepthFull), _VertexDepth)`; `baseCol = lerp(_ShallowColor.rgb, baseCol, t)`; `alpha *= lerp(_ThinAlpha, 1, t)`. `Ocean.mat` không đặt `_VertexDepth` (mặc định 0, t = 1) nên biển nền không đổi.
4. **Số [số tạm], chưa chốt, chỉnh trong `Water.mat`:** `_VertexDepth` 1, `_DepthFull` 1 (ô), `_ThinAlpha` 0,25. Đây là thông số hình ảnh của material, giống `_Alpha` hiện có. Với các số này, hệ số alpha theo mức 0..5 (1 lớp) = 0,36 / 0,48 / 0,59 / 0,70 / 0,81 / 0,93; nước dày ≥ 1 ô giữ như cũ.
5. **Lỗi 2:** không sửa `WorldEditor`, không đổi luật lan. Chỉ thêm test ghi lại hành vi 0043 (bước 4).

## Phạm vi
- Được sửa:
  - `client/Assets/Game/World/BlockMesher.cs` (hằng, 2 hàm mới, `AddWaterVertex`);
  - `client/Assets/Game/Client/Shaders/Ocean.shader` (Properties, CBUFFER, pass `ForwardLit`);
  - `client/Assets/Game/Client/Materials/Water.mat` (3 float);
  - `client/Assets/Game/Tests/World/BlockMesherTests.cs`, `client/Assets/Game/Tests/Core/WaterFlowTests.cs` (thêm test);
  - `design/04-tech/world-tech.md` (1 dòng, bước 5).
- Không được sửa: `WorldEditor.cs`, `Ocean.mat`, `water_m1.json`, các pass `WaterDepthPrepass`/`DepthOnly`, mọi test cũ.

## Các bước
1. **`BlockMesher.cs` — mã hoá độ dày.**
   - Dưới `:43` thêm:
     ```csharp
     /// <summary>Độ dày nước (ô) ứng với alpha 255 trong màu đỉnh mặt nước (brief 0084) — bằng hằng cùng tên trong Ocean.shader.</summary>
     public const float WaterDepthEncodeMax = 4f;
     ```
   - Thêm `static uint WaterDepthColor(float depth)` theo QĐ 2.
   - Trong job, cạnh `CornerHeight`, thêm `float WaterDepthAt(float3 p)` theo QĐ 1, dùng `CellAt` và `Cell.GetMaterial(...) == BlockMaterial.Water`.
   - `AddWaterVertex`: thay `AddVertex(p, n, 0)` bằng `AddVertex(p, n, n.y > 0.999f ? WaterDepthColor(WaterDepthAt(p)) : WaterDepthColor(WaterDepthEncodeMax))`. Khoá hàn giữ nguyên.
   - Xong khi: biên dịch không lỗi; test bước 3 đạt.
2. **`Ocean.shader` + `Water.mat`.**
   - Properties, sau `_Alpha`:
     ```
     _VertexDepth("Độ dày từ màu đỉnh (1 = nước trong map, brief 0084)", Range(0, 1)) = 0
     _DepthFull("Độ dày coi là đầy (ô)", Float) = 1
     _ThinAlpha("Hệ số alpha khi nước rất mỏng", Range(0, 1)) = 0.25
     ```
   - CBUFFER thêm `half _VertexDepth, _ThinAlpha; float _DepthFull;`.
   - Pass `ForwardLit`:
     - `Attributes` thêm `half4 color : COLOR;`;
     - `Varyings` thêm `half depthT : TEXCOORD1;`;
     - thêm `static const float WaterDepthEncodeMax = 4.0; // = BlockMesher.WaterDepthEncodeMax`;
     - trong `vert`: `o.depthT = lerp(1.0h, saturate(input.color.a * WaterDepthEncodeMax / _DepthFull), _VertexDepth);`;
     - trong `frag`, ngay sau dòng `baseCol` (`:158`): `baseCol = lerp(_ShallowColor.rgb, baseCol, input.depthT);`;
     - trước `return`: `alpha *= lerp(_ThinAlpha, 1.0h, input.depthT);`.
   - Thêm 1 dòng chú thích đầu file, sau dòng `// Brief 0082: …`:
     `// Brief 0084: mặt nước nhạt + trong hơn khi mỏng (độ dày từ alpha màu đỉnh do BlockMesher ghi; _VertexDepth = 0 cho Ocean.mat).`
   - `Water.mat` `m_Floats` thêm, giữ thứ tự ABC: `- _DepthFull: 1`, `- _ThinAlpha: 0.25`, `- _VertexDepth: 1`.
   - Xong khi: `refresh_unity` → console không có lỗi shader.
3. **`BlockMesherTests.cs` — thêm 2 test.** Helper `static uint WaterVertexColor(ChunkMesh mesh, float3 normal, float3 pos)` tìm như `HasWaterVertex` (sai số 1e-4), không thấy thì `Assert.Fail`.
   - `WaterSurfaceDepthEncodesLevel`:
     - dựng: `StoneFloor16()`; ô chảy `Cell.MakeFlowing(0, L, false)` tại (x, 5, 4), x = 3..8, L = 5, 4, 3, 2, 1, 0; `Build(w, 0, 0, 0, true, 5)`;
     - đỉnh up tại (x, 5 + h, 4), x = 3..9, h = 0,9 / 0,825 / 0,675 / 0,525 / 0,375 / 0,225 / 0,15: alpha (`c >> 24`) = 57 / 53 / 43 / 33 / 24 / 14 / 10 (sai số ±1), giảm dần thật sự;
     - `c & 0xFFFFFF == 0xFFFFFF`;
     - mọi đỉnh trong `WaterIndices` có `Normals.y ≤ 0.999` có alpha 255;
     - `AssertNoDuplicateWaterVertices(mesh)`.
   - `WaterSurfaceDepthCountsWaterBelow`:
     - dựng: `StoneFloor16()`; nguồn `Cell.Make(BlockMaterial.Water, Cell.MaxWaterLevel)` tại (4,5,4); `Cell.MakeFlowing(0, 3, false)` tại (4,6,4); nguồn riêng tại (10,5,10);
     - 4 đỉnh up (4|5, 6.6, 4|5): alpha 102 ±1 (độ dày 1,6);
     - đỉnh up (10, 5.9, 10): alpha 57 ±1.
   - Xong khi: cả hai test fail trên code cũ (A = 0) và đạt sau bước 1.
4. **`WaterFlowTests.cs` — test ghi lại hành vi 0043 (lỗi 2).**
   - Thêm `WideHolePastLevelRadiusStaysEmpty`, chú thích: `/// <summary>0043 hiện tại: hố rộng chỉ có nước trong bán kính mức (lan giảm 1 mức/ô). Đổi kỳ vọng khi user chốt luật lấp hố (task 5j lỗi 2).</summary>`.
   - Dựng: `PondWorld()`, `Editor(w)`; đào y = 3 rồi y = 2, z 2..9, x 8..15; `Settle(e)`.
   - Khẳng định, hàng z = 5:
     - (8..12, 3, 5) là nước, mức 4, 3, 2, 1, 0; (13..15, 3, 5) Empty;
     - (8,2,5) nước, `Cell.IsFalling`; (9..13, 2, 5) mức 4, 3, 2, 1, 0; (14, 2, 5), (15, 2, 5) Empty.
   - Xong khi: test đạt ngay (ghi lại hành vi, không phải sửa lỗi).
5. **`design/04-tech/world-tech.md:138`** — cuối dòng `- **Hiển thị:** …`, thêm câu: ` Độ dày nước (từ mặt tới đáy đặc) ghi vào alpha màu đỉnh mặt trên; shader cho nước mỏng nhạt + trong hơn — `_DepthFull` 1, `_ThinAlpha` 0,25 trong `Water.mat` [số tạm], chưa chốt (brief 0084, lỗi 3 bước 5j).`

## Tiêu chí xong
- 3 test mới đạt. 2 test mesh fail trên code cũ (ghi output fail ở Kết quả test).
- EditMode toàn bộ đạt, gồm `GoldenHashTests`, mã băm không đổi.
- PlayMode các lớp nước đạt.
- Console 0 lỗi sau `refresh_unity`.
- `git status` chỉ có file trong Phạm vi.

## Test sau khi xong

### Phần cần test
- Mã hoá độ dày theo mức (mới) · `BlockMesherTests.WaterSurfaceDepthEncodesLevel` · alpha 57→10 giảm dần, RGB trắng, đỉnh không hướng lên = 255, không trùng đỉnh.
- Độ dày cộng ô nước bên dưới (mới) · `BlockMesherTests.WaterSurfaceDepthCountsWaterBelow` · 102 và 57.
- Hành vi hố rộng 0043 (mới, ghi lại) · `WaterFlowTests.WideHolePastLevelRadiusStaysEmpty` · đạt.
- Mesh nước cũ: độ cao, hàn đỉnh, kín, mép tràn thon, bo lõm (regression) · toàn bộ `BlockMesherTests` · đạt, số tam giác/đỉnh không đổi vì chỉ đổi màu.
- Luật nước 0043 + biển 0013 (regression) · toàn bộ `WaterFlowTests`, `SeaFloodTests` · đạt.
- Shader biên dịch, `Ocean.mat` không đổi (regression) · `refresh_unity` + `read_console` Error · 0 lỗi; `git diff --stat` không có `Ocean.mat`.
- Nước/biển trong Play (regression) · PlayMode `WaterFlowPlayTests`, `WaterLayerPlayTests`, `DayCycleWaterPlayTests`, `FlyCameraWaterPlayTests` · đạt.
- Toàn bộ EditMode (regression, gồm `GoldenHashTests`) · đạt.

### Lệnh executor chạy
1. Trước bước 1: thêm 2 test bước 3 rồi chạy `run_tests` EditMode lọc `BlockMesherTests.WaterSurfaceDepth`. Ghi lại output fail.
2. Sau bước 1–4: `refresh_unity` → `read_console` (Error) = 0.
3. `run_tests` EditMode lọc `BlockMesherTests`, `WaterFlowTests`, `SeaFloodTests` → đạt (gặp A = 0 → ép biên dịch lại, xem Bối cảnh).
4. `run_tests` EditMode toàn bộ → đạt.
5. `run_tests` PlayMode lọc 4 lớp ở trên → đạt; rồi `git checkout -- client/ProjectSettings/EditorSettings.asset`.
6. `git status` / `git diff --stat` → chỉ file trong Phạm vi; xoá `InitTestScene*` nếu có.

### User tự test
Gộp cuối 5j. Planner chuyển sang danh sách chơi thử. Play, scene SimView:
- (a) Đào một dải 1 lớp dài 6 ô từ bờ ao ra chỗ rộng: nước nhạt + trong dần theo từng ô, không chỉ ở ô cuối.
- (b) Mặt ao, rãnh 1 ô (mức 5) và biển trông gần như trước.
- (c) Hố 2 lớp cạnh ao: chỗ có 2 lớp nước đậm hơn chỗ 1 lớp mỏng.
- (d) Không lộ đường lưới hay vệt màu đứt ở mép ô.

## Dừng và chuyển lại Opus khi
- Brief mơ hồ hoặc mâu thuẫn với code thật (số dòng lệch nhiều, chữ ký khác).
- `WideHolePastLevelRadiusStaysEmpty` ra giá trị khác bước 4: ghi giá trị thật, không tự đổi kỳ vọng.
- Alpha ở bước 3 lệch quá ±1 mà code đúng QĐ 1–2: ghi giá trị thật.
- Thêm `COLOR` làm `Ocean.mat` (biển nền) đổi hình, hoặc shader lỗi trên pass khác.
- Cần API Unity ngoài brief, cần quyết định ngoài "Quyết định đã chốt", hoặc phải sửa file ngoài "Phạm vi".
- Cùng một lỗi sửa 2 lần vẫn chưa qua.

---
<!-- Từ đây trở xuống: EXECUTOR ghi. -->

## Kết quả
Base commit: 89eef6b
Chạy bằng: claude-sonnet-5-5 · medium
Bắt đầu: 2026-10-29 19:24 · Kết thúc: ~20:10 (phần lớn là chờ chạy test EditMode ~6 phút/lượt)
Commit: 60c980d
File đổi: BlockMesher.cs (hằng WaterDepthEncodeMax, WaterDepthColor, WaterDepthAt, AddWaterVertex), Ocean.shader, Water.mat, BlockMesherTests.cs (+2 test), WaterFlowTests.cs (+1 test), design/04-tech/world-tech.md:139.
Ghi chú: không gặp A = 0 sau khi sửa (không cần ép biên dịch lại). PlayMode/MCP làm ProjectSettings.asset đổi `runInBackground` 0→1 và EditorSettings.asset → đã `git checkout` hoàn lại cả hai.

## Kết quả test
- Mã hoá độ dày theo mức: đạt (`WaterSurfaceDepthEncodesLevel`). Trước khi sửa code fail: `alpha x=3 Expected: 57.0d +/- 1.0d But was: 0.0d`.
- Độ dày cộng ô nước dưới: đạt (`WaterSurfaceDepthCountsWaterBelow`). Trước khi sửa fail: `(4,4) Expected: 102.0d +/- 1.0d But was: 0.0d`.
- Hố rộng 0043: đạt ngay (`WaterFlowTests.WideHolePastLevelRadiusStaysEmpty`), đúng giá trị brief.
- EditMode toàn bộ: 663/663 đạt (gồm GoldenHashTests, BlockMesherTests, WaterFlowTests, SeaFloodTests).
- Shader/console: `refresh_unity` + `read_console` Error = 0; `Ocean.mat` không đổi (git status).
- PlayMode `Game.Client.Tests.{WaterFlow,WaterLayer,DayCycleWater,FlyCameraWater}PlayTests`: 8/8 đạt.
- User tự test (a)–(d): chờ user (gộp cuối 5j).
- git status: chỉ file trong Phạm vi.

## Vướng mắc

---
<!-- PLANNER ghi khi review. -->

## Review
- Chạy bằng Sonnet · medium khớp Model thực thi (đã hạ từ high, commit `89eef6b`). ~46 phút, phần lớn chờ test.
- Mọi dòng Phần cần test có kết quả: 2 test độ dày fail trước sửa (alpha 0), đạt sau; `WideHolePastLevelRadiusStaysEmpty` đạt (ghi hành vi 0043, sẽ đổi ở brief cài 0044); EditMode 663/663; PlayMode 4 lớp nước 8/8 đúng lệnh 5 [run trong brief].
- `git show --stat 60c980d`: 6 file, đúng Phạm vi; `Ocean.mat` không đổi.
- Hình đậm/nhạt theo mức: user xem gộp cuối 5j.
- Kết luận: đã duyệt
