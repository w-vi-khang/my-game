# Brief 0100 — World 5k sửa lỗi chơi thử: bọt bờ trơn 1 dải mềm, giảm độ bóng

> Trạng thái: cần Opus
> Review: đầy đủ + user xem
> Model thực thi: Sonnet · medium
> Cập nhật: 2026-11-02

## Mục tiêu
Sửa 2 lỗi user thấy khi chơi thử 5k ngày 2026-11-02. Lỗi ghi ở mục "Lỗi chơi thử" của task [5k](../tasks/m1-world-5k-nuoc-kieu-anh-tham-khao.md). User chọn hướng "Giảm mạnh, giữ nhẹ".
- **Lỗi 5k-1:** bọt bờ hiện thành nhiều vạch trắng gấp khúc theo bậc ô. Sửa thành **1 dải bọt mềm** chạy theo một đường **trơn**. Cách làm: thêm một khoảng cách tới bờ đã làm mềm, ghi vào kênh B màu đỉnh, chỉ bọt dùng.
- **Lỗi 5k-2:** mặt nước quá bóng. Giảm phản chiếu mặt trời, quầng, chấm lấp lánh. Vệt chảy giảm còn một nửa. Chỉ đổi số trong JSON.

## Bối cảnh cần đọc
**Planner đã chốt thiết kế (2026-11-02), kèm lý do:**
- Planner tắt từng hiệu ứng lúc Play rồi chụp lại [run]. Tắt `_FoamStrength` thì hết vạch gấp khúc. Tắt `_Glint`, `_GlintHalo`, `_SparkleStrength` thì hết mảng trắng loang. Tắt vệt chảy thì biển gần như không đổi.
- Vì sao vạch gãy: khoảng cách tới bờ (kênh R, brief 0095) chỉ tính ở đỉnh góc ô, rồi nội suy tuyến tính trên từng tam giác. Ở góc lõm của bờ bậc thang, đường đồng mức gãy thành chữ V. Bọt lại là 3 vạch mảnh (`foamLine` 0,3, `foamSoft` 0,15) nên chỗ gãy lộ rõ.
- Cách sửa: tính thêm **soft-min** (log-sum-exp) của khoảng cách tới mọi khối đặc trong cửa sổ quét: `foamD = −k · ln(Σ exp(−dᵢ / k))`.
  - Ở góc lõm, nhiều khối cùng góp vào tổng nên giá trị nhỏ hơn. Kết quả là đường đồng mức được bo tròn.
  - Dùng chung vòng lặp có sẵn, không tốn thêm lần quét.
  - Planner đã mô phỏng bằng Python trên một bờ bậc thang không đều: k = 0,5 cộng 1 dải mềm cho đường bọt trơn, hết gãy V. Ảnh ở scratchpad, không đưa vào repo.
- **Không đổi kênh R.** Viền bờ mờ và màu (0095) giữ nguyên. Giá trị soft-min nhỏ hơn khoảng cách thật, khoảng −0,35 ô ở bờ thẳng khi cách 1 ô. Nếu dùng chung R, viền bờ 0095 sẽ đổi. Vì vậy ghi vào kênh **B**, hiện đang để dành = 255.
- `k` là hằng mesher `WaterFoamSoftK` = 0,5 [số tạm], chưa chốt. Theo tiền lệ `LipTaperLength`: số trong mesher là hằng `public const`, vì đổi nó phải dựng lại mesh. Các số shader vẫn nằm trong JSON.
- Shader thêm 1 giá trị nhưng không thêm interpolator: `open` (`TEXCOORD3`) đổi từ `half` thành `half2`.

**Code** (tại commit `97818c3`):
- `client/Assets/Game/World/BlockMesher.cs`:
  - hằng `WaterShoreEncodeMax` `:48`, `WaterShoreMargin` `:50`;
  - `static uint WaterVertexColor(float depth, float shore, float open)` `:52–56`, trong đó B cố định `| 0x00FF0000u` `:55`;
  - `void WaterShoreAt(float3 p, out float dist, out float open)` `:1749–1773`;
  - lời gọi trong `AddWaterVertex`: `:1843–1844` (đỉnh mặt trên) và `:1849` (đỉnh không hướng lên: `WaterVertexColor(WaterDepthEncodeMax, WaterShoreEncodeMax, 1f)`).
  - `WaterVertexColor` không có chỗ gọi nào khác.
- `client/Assets/Game/Client/Shaders/Ocean.shader`:
  - chú thích đầu file `:1–11`;
  - `Varyings` `:124–132` (`half open : TEXCOORD3;` `:130`);
  - `vert` `:137–148` (`o.open = …` `:145`);
  - `ShoreFoam(float2 xz, half d, half open)` `:184–194`;
  - `frag` gọi bọt `:295`: `half foam = ShoreFoam(input.positionWS.xz, input.shoreD, input.open);`. `input.open` chỉ dùng ở dòng này.
- JSON: `design/data/water_m1.json`, mục `look`, và bản sao `client/Assets/Game/Client/Resources/Config/water_m1.json`. Hai file phải giống hệt nhau. `configVersion` = 8, **không đổi** (không thêm trường, chỉ đổi số).
- Khoảng hợp lệ (`WaterConfig.cs:92–130`) đã chứa mọi số mới ở QĐ 4, không cần sửa `WaterConfig.cs`.
- Test:
  - `Tests/World/BlockMesherTests.cs`: `StoneFloor16()` `:1050`, `WaterVertexColor(mesh, normal, pos)` `:1070`.
  - `WaterSurfaceOpennessEncoded` `:1208–1225`: kiểm `B == 255` ở `:1222`, **phải sửa**.
  - `WaterSurfaceShoreDistanceEncoded` `:1228–1267`: kiểm `B == 255` ở `:1247`, `:1253`, `:1258`, **phải sửa**. Kiểm đỉnh không hướng lên `0xFFFFFFFFu` ở `:1264`, giữ nguyên.
  - `WaterSurfaceDepthEncodesLevel` `:1181` kiểm `rgb == 0xFFFFFF`. Lớp 5 ở test đó không có khối đặc nên B vẫn 255, **không sửa**.
  - `Tests/Core/WaterConfigTests.cs`: `LookParsesAndValidates` `:53–69` đọc số thật trong JSON. Phải sửa các dòng `:60` (foamWidth), `:61` (foamBands), `:63` (glint), `:64` (glintHalo), `:67` (flowStreakStrength).
  - `Tests/Client/WaterGlintPlayTests.cs` tự đặt số trên material riêng (`:49–53`, `:97–111`), không phụ thuộc JSON → không sửa.
- Docs: chỉ dùng hàm HLSL có sẵn và `Unity.Mathematics`: `math.exp`, `math.log`, `math.clamp`; mesher đã dùng `math.sqrt`, `math.saturate`. Không dùng API Unity mới.
- Lưu ý review 0079 và 0096: nếu test mesh mới ra B = 255 ở mọi đỉnh sau khi sửa → ép Burst biên dịch lại (thêm hoặc bỏ một dòng chú thích trong `BlockMesher.cs`).

## Quyết định đã chốt (không được đổi)
1. **Mesher, khoảng cách cho bọt:**
   - Thêm hằng, đặt sau `WaterShoreMargin` (`:50`):
     ```csharp
     /// <summary>Độ mềm (ô) của khoảng cách tới bờ cho bọt — soft-min log-sum-exp, bo tròn góc lõm của bờ bậc thang (brief 0100) [số tạm].</summary>
     public const float WaterFoamSoftK = 0.5f;
     ```
   - `WaterShoreAt` đổi chữ ký thành `void WaterShoreAt(float3 p, out float dist, out float open, out float foamDist)`.
     - Trong vòng lặp, với ô đặc: `float d = math.sqrt(dx * dx + dz * dz); best = math.min(best, d); acc += math.exp(-d / WaterFoamSoftK);`. `float acc = 0f;` khai báo cạnh `best`.
     - Sau vòng lặp: `foamDist = acc > 0f ? math.clamp(-WaterFoamSoftK * math.log(acc), 0f, WaterShoreEncodeMax) : WaterShoreEncodeMax;`.
     - `dist` và `open` không đổi so với hiện tại.
   - Cập nhật `<summary>` của `WaterShoreAt`: thêm "foamDist = soft-min khoảng cách (WaterFoamSoftK), chỉ dùng cho bọt (brief 0100)".
2. **Mã hoá:**
   - Đổi thành `static uint WaterVertexColor(float depth, float shore, float open, float foam)`. B = `(uint)math.round(255f * math.saturate(foam / WaterShoreEncodeMax)) << 16`, thay cho `0x00FF0000u`.
   - `<summary>` sửa "B = 255 (để dành)" thành "B = khoảng cách tới bờ đã làm mềm cho bọt / WaterShoreEncodeMax (brief 0100)".
   - `:1843–1844`: `WaterShoreAt(p, out float shore, out float open, out float foamDist); color = WaterVertexColor(WaterDepthAt(p), shore, open, foamDist);`.
   - `:1849`: `WaterVertexColor(WaterDepthEncodeMax, WaterShoreEncodeMax, 1f, WaterShoreEncodeMax)`. Đỉnh không hướng lên giữ `0xFFFFFFFF`.
3. **Shader** (pass `ForwardLit`, không đụng pass khác):
   - `Varyings`: `half open : TEXCOORD3;` đổi thành `half2 open : TEXCOORD3; // x = độ thoáng (G, brief 0096), y = khoảng cách tới bờ cho bọt (B, brief 0100)`.
   - `vert`: `o.open = half2(lerp(1.0h, input.color.g, _VertexDepth), lerp(WaterShoreEncodeMax, input.color.b * WaterShoreEncodeMax, _VertexDepth));`.
   - `frag` `:295`: `half foam = ShoreFoam(input.positionWS.xz, input.open.y, input.open.x);`.
   - Viền bờ mờ (`alpha *= smoothstep(0.0h, _ShoreSoftWidth, input.shoreD)`) vẫn dùng `shoreD` (R), không đổi.
   - Hàm `ShoreFoam` không đổi.
   - Chú thích đầu file, thêm sau dòng 0099: `// Brief 0100: bọt dùng khoảng cách tới bờ đã làm mềm (soft-min, kênh B màu đỉnh) → đường bọt trơn ở bờ bậc thang; viền bờ vẫn theo R.`
4. **JSON** (cả 2 file, mục `look`; mọi số [số tạm], chưa chốt). Đổi đúng các trường sau, giữ nguyên trường khác:

   | Trường | Cũ | Mới |
   |---|---|---|
   | `foamWidth` | 2.0 | 1.6 |
   | `foamBands` | 3.0 | 1.0 |
   | `foamLine` | 0.3 | 0.5 |
   | `foamSoft` | 0.15 | 0.35 |
   | `foamStrength` | 0.9 | 0.5 |
   | `glint` | 1.0 | 0.3 |
   | `glintHalo` | 0.8 | 0.25 |
   | `sparkleStrength` | 1.5 | 0.4 |
   | `sparkleDensity` | 0.35 | 0.2 |
   | `flowStreakStrength` | 0.35 | 0.18 |

   `foamLine + foamSoft` = 0,85 ≤ 1, hợp lệ.

## Phạm vi
- Được sửa:
  - `client/Assets/Game/World/BlockMesher.cs` (QĐ 1–2);
  - `client/Assets/Game/Client/Shaders/Ocean.shader` (chỉ chú thích đầu file, `Varyings`, `vert`, 1 dòng `frag` của pass `ForwardLit`);
  - 2 file `water_m1.json`;
  - `Tests/World/BlockMesherTests.cs`: test mới, cộng đúng 4 dòng kiểm B ở `:1222`, `:1247`, `:1253`, `:1258`;
  - `Tests/Core/WaterConfigTests.cs`: 5 dòng `:60`, `:61`, `:63`, `:64`, `:67`;
  - `design/04-tech/world-tech.md`: 1 câu ở `:141`.
- Không được sửa: `WaterConfig.cs`, `WaterLook.cs`, `Water.mat`, `Ocean.mat`, pass `WaterDepthPrepass`/`DepthOnly`, hàm `ShoreFoam`, `WorldEditor.cs`, `TerrainChunkView.cs`, `OceanBackdrop.cs`, mọi test khác.

## Các bước
1. **Viết test mesh trước.** Các test này phải fail trên code cũ.
   - Sửa `WaterSurfaceOpennessEncoded` `:1222`: B (`(c >> 16) & 0xFF`) = **0** ±1. Rãnh 1 ô giữa đá nên khoảng cách mềm = 0.
   - Sửa `WaterSurfaceShoreDistanceEncoded`:
     - `:1247`: thay bằng mảng `int[] wantB = { 0, 29, 88, 148, 255 };`, kiểm `wantB[k]` ±1 cho hàng z = 4, x = 4..8.
     - `:1253` (11,12): B = 64 ±1. `:1258` (12,12): B = 90 ±1. Hai điểm này chỉ có 1 khối đặc trong cửa sổ nên B = R.
   - Test mới `WaterSurfaceFoamDistanceRoundsConcaveCorner`:
     - Dựng: `StoneFloor16()`. Lớp y = 5: ô có `x <= 3 || z <= 3` là `Cell.Make(BlockMaterial.Stone)`, còn lại là `Cell.Make(BlockMaterial.Water, Cell.MaxWaterLevel)`. Gọi `BlockMesher.Build(w, 0, 0, 0, true, 5)`.
     - Đỉnh up `(6, 5.9, 6)` (góc lõm, chéo): R = 128 ±1, B = **60** ±1.
     - Đỉnh up `(6, 5.9, 12)` và `(12, 5.9, 6)` (bờ thẳng): R = 128 ±1, B = **81** ±1.
     - Đỉnh up `(5, 5.9, 5)`: R = 64 ±1, B = 3 ±1.
     - Kiểm thêm: B ở `(6, 5.9, 6)` nhỏ hơn B ở `(6, 5.9, 12)`. Đây là chứng cứ góc lõm được bo tròn.
   - Các số kỳ vọng do Planner tính bằng mô phỏng Python của đúng công thức QĐ 1–2, gồm cửa sổ `floor(wp ± 4)` và khoảng cách tới hộp ô.
   - Xong khi: cả 3 test fail trên code cũ (B = 255). Ghi output fail.
2. **Sửa `BlockMesher.cs`** theo QĐ 1–2.
   - Xong khi: 3 test ở bước 1 đạt, và `WaterSurfaceDepth*`, `WaterSurfaceFlow*`, `WaterColumnSideFlowIsFall` vẫn đạt.
3. **Sửa shader** theo QĐ 3.
   - Xong khi: `refresh_unity` → `read_console` Error = 0.
4. **Đổi số JSON** theo QĐ 4 ở cả 2 file. Sửa `WaterConfigTests.LookParsesAndValidates` cho khớp:
   - `:60` → 1.6f; `:61` → 1f; `:63` → 0.3f; `:64` → 0.25f; `:67` → 0.18f.
   - Xong khi: `WaterConfigTests` và `ConfigTests.WaterClientCopyMatchesDesignData` đạt.
5. **Kiểm tổng** theo mục "Lệnh executor chạy".
6. **`design/04-tech/world-tech.md:141`**: thêm câu sau vào cuối dòng, sau câu "Hướng này lưu ở … chưa chốt.":
   ` Sửa lỗi chơi thử (brief 0100, user 2026-11-02 chọn "giảm mạnh, giữ nhẹ"): bọt dùng khoảng cách tới bờ đã làm mềm (soft-min, k = 0,5 [số tạm], ghi kênh B màu đỉnh) để đường bọt trơn ở bờ bậc thang, còn 1 dải mềm; phản chiếu/quầng/lấp lánh và vệt chảy giảm mạnh trong water_m1.json "look" [số tạm], chưa chốt.`

## Tiêu chí xong
- 3 test mesh ở bước 1: fail trên code cũ (có ghi output) và đạt sau khi sửa.
- `WaterConfigTests` và `ConfigTests.WaterClientCopyMatchesDesignData` đạt.
- Toàn bộ EditMode đạt.
- PlayMode toàn bộ đạt, không đổi baseline.
- Console 0 lỗi.
- `git status` chỉ có file trong Phạm vi.

## Test sau khi xong

### Phần cần test
- Khoảng cách mềm ghi vào kênh B (mới):
  - Cách test: `BlockMesherTests.WaterSurfaceFoamDistanceRoundsConcaveCorner`, `WaterSurfaceShoreDistanceEncoded`, `WaterSurfaceOpennessEncoded`.
  - Mong đợi: B đúng số ở bước 1; R và G không đổi; đỉnh không hướng lên vẫn `0xFFFFFFFF`.
- Độ dày, hướng chảy, mesh nước cũ (regression):
  - Cách test: toàn bộ `BlockMesherTests`.
  - Mong đợi: đạt; số tam giác và số đỉnh không đổi.
- Số JSON mới hợp lệ, hai bản sao giống nhau:
  - Cách test: `WaterConfigTests`, `ConfigTests.WaterClientCopyMatchesDesignData`.
  - Mong đợi: đạt.
- Áp tham số lên material (regression):
  - Cách test: `WaterLookTests`.
  - Mong đợi: đạt, không sửa test.
- Shader biên dịch (regression):
  - Cách test: `refresh_unity` rồi `read_console` (Error).
  - Mong đợi: 0 lỗi.
- Nước/biển trong Play (regression):
  - Cách test: PlayMode toàn bộ, gồm `OceanBackdropTests`, `WaterFlowPlayTests`, `WaterLayerPlayTests`, `DayCycleWaterPlayTests`, `FlyCameraWaterPlayTests`, `SeaSkyTintPlayTests`, `WaterGlintPlayTests`.
  - Mong đợi: đạt, không đổi baseline.
- Toàn bộ EditMode (regression):
  - Mong đợi: đạt.

### Lệnh executor chạy
1. Viết/sửa test bước 1 trước. Chạy `run_tests` EditMode lọc `BlockMesherTests.WaterSurfaceFoamDistance`, `BlockMesherTests.WaterSurfaceShoreDistance`, `BlockMesherTests.WaterSurfaceOpenness`. Ghi output fail.
2. Sau bước 2–4: `refresh_unity`, rồi `read_console` (Error) = 0.
3. `run_tests` EditMode lọc `BlockMesherTests`, `WaterConfigTests`, `ConfigTests`, `WaterLookTests` → đạt.
4. `run_tests` EditMode toàn bộ → đạt.
5. `run_tests` PlayMode toàn bộ → đạt. Sau đó chạy `git checkout -- client/ProjectSettings/EditorSettings.asset`, và cả `ProjectSettings.asset` nếu `runInBackground` bị đổi.
6. `git status` và `git diff --stat` → chỉ có file trong Phạm vi. Xoá `InitTestScene*` nếu có.
7. Chụp ảnh (không chặn brief):
   - Play scene **SimView**, chụp 1 ảnh Game view góc mặc định ban ngày.
   - Lưu `~/Documents/User/khang/Game/art-test/shots/b0100_after_1.png`.
   - Không dựng world thử thay SimView.

### User tự test
Gộp với danh sách chơi thử 5k. Play scene SimView, ban ngày, rồi ban đêm. Xem đúng chỗ ở 2 ảnh lỗi: biển phía đông trại và ao đào cạnh rãnh.
- (a) Bờ biển và ao: chỉ còn **1 dải bọt mềm** sát bờ, chạy theo một đường trơn. Ở góc bậc thang của bờ không còn vạch gấp khúc chữ V.
- (b) Mặt nước không còn mảng trắng loang. Vẫn thấy một quầng nắng nhẹ ở hướng mặt trời và vài chấm lấp lánh thưa.
- (c) Ban đêm: không còn vạch hay mảng sáng nổi trên biển.
- (d) Rãnh có nước chảy: vẫn thấy vệt chảy, nhưng nhạt hơn trước.
- (e) Viền bờ mờ và màu nông/sâu (phần 1) trông như trước.
- Số chỉnh được (`design/data/water_m1.json`, mục `look`):
  - bọt vẫn rõ quá → giảm `foamStrength`;
  - vẫn bóng quá → giảm `glint`, `glintHalo`, `sparkleStrength`;
  - mất hẳn lấp lánh → tăng lại các trường đó.
  - Độ mềm của đường bọt là hằng `BlockMesher.WaterFoamSoftK` (0,5). Không chỉnh trong JSON.

## Dừng và chuyển lại Opus khi
- Brief mơ hồ, hoặc mâu thuẫn với code thật (số dòng lệch nhiều, chữ ký khác).
- B ở bước 1 khác kỳ vọng quá ±1 dù code đúng QĐ 1–2: ghi giá trị thật, không tự đổi kỳ vọng.
- Test PlayMode cũ fail: ghi lỗi, không tự đổi baseline.
- Shader lỗi ở pass khác.
- Cần API Unity ngoài brief, quyết định ngoài "Quyết định đã chốt", hoặc phải sửa file ngoài "Phạm vi".
- Cùng một lỗi sửa 2 lần vẫn chưa qua.

---
<!-- Từ đây trở xuống: EXECUTOR ghi. -->

## Kết quả
- Base commit: 70a6887
- Chạy bằng: claude-sonnet-5-5 · medium
- Bắt đầu: 05:23 (2026-11-02)

- Đã làm (CHƯA commit code, đang ở working tree): bước 1–4 + 6 (BlockMesher, Ocean.shader, 2 JSON, test, world-tech.md:141). Console Error = 0.

## Kết quả test
- Bước 1 trên code cũ: 3 test fail đúng (B = 255): FoamDistanceRoundsConcaveCorner "B corner Expected 60 But was 255"; OpennessEncoded "B x=8 Expected 0 But was 255"; ShoreDistanceEncoded "B x=4 Expected 0 But was 255".
- EditMode toàn bộ sau sửa: 713/714 đạt. 1 fail: `WaterConfigTests.ConfigFileMatchesDefaults` ("glint":1.0 (default) vs 0.3 (file)).
- PlayMode: chưa chạy (dừng trước).

## Vướng mắc
- `ConfigFileMatchesDefaults` (`WaterConfigTests.cs:28-32`) so JSON với `new WaterConfig()`, tức giá trị mặc định trong `WaterConfig.cs`. Brief nói không cần sửa `WaterConfig.cs`, nhưng 10 số mới lệch default → test fail.
- Phương án A (đề xuất): cho phép sửa default trong `WaterConfig.cs` (`:56 glint`, `:58 foamWidth`, `:59 foamBands`, foamLine, foamSoft, foamStrength, `:69 glintHalo`, sparkleStrength, sparkleDensity, `:79 flowStreakStrength`) khớp bảng QĐ 4; thêm `WaterConfig.cs` vào Phạm vi.
- Phương án B: sửa test để bỏ so sánh — không nên.
- Sau khi Planner duyệt: Executor đổi default, chạy lại EditMode + PlayMode, commit.

---
<!-- PLANNER ghi khi review. -->

## Review

## Kiểm chứng
- design (verify_run): đạt, exit 0 — `design/ hợp lệ: 70 file`.
- client_test / client_test_play qua `tools/unity_test.py`: Chưa kiểm được: Editor đang mở project (dùng chung qua MCP, 0010), script đòi Editor tắt. Thay bằng `run_tests` MCP: EditMode 713/714 (1 fail `ConfigFileMatchesDefaults`, xem Vướng mắc), PlayMode chưa chạy.
- Checklist: phạm vi M1 (0007) đạt — chỉ sửa nước (5k); Game.Core không đổi tham chiếu Unity (đạt, chỉ đổi test + JSON); các mục thiết kế khác không áp dụng (không thêm tính năng/thuật ngữ mới). Giờ thực tế: chưa ghi (brief chưa xong).
