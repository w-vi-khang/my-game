# Brief 0083 — World 5j (lỗi 6–8): mép tràn thon vào ô thượng nguồn thay cho nắp; ẩn thân khi camera quá gần

> Trạng thái: đã duyệt
> Review: đầy đủ + user xem (gộp cuối 5j)
> Model thực thi: Opus · medium
> Cập nhật: 2026-10-29 18:54

## Mục tiêu
Sửa lỗi 6–8 của task [5j](../tasks/m1-world-5j-sua-loi-choi-thu.md):
- **Lỗi 6 (gai nước ở mép tràn):** bỏ "nắp" ở đầu dải cung mép tràn; thay bằng đoạn **thon**: trong khoảng `R` từ đầu đó, cung thu nhỏ dần về 0 để mép cột nối phẳng vào ô nước thượng nguồn.
- **Lỗi 7 (gờ mỏng đầu cột):** chưa tái hiện riêng được. Giả thuyết: cùng là mảnh nắp ở lỗi 6, nhìn từ bên cạnh nên thành gờ mỏng. Brief này không sửa gì riêng cho lỗi 7, chỉ chụp ảnh để user kiểm khi chơi thử cuối 5j.
- **Lỗi 8 (mảng xanh đậm khi camera dưới nước nhìn lên):** thân nhân vật che camera. Khi camera bị đẩy sát nhân vật (khoảng cách thực < ngưỡng), ẩn thân nhân vật.

## Bối cảnh cần đọc
**Planner đã chẩn đoán (2026-10-29, MCP `execute_code` ở EditMode, không commit):**
- **Lỗi 6.** Tái hiện bằng thế giới: vách đá y 2..11, z ≤ 6. Ao nước nguồn y = 11, x 5..9, z 1..5. Đào ô bờ (7,11,6) rồi `SettleWater` → ô kênh (7,11,6) mức 5, ô mép tràn (7,11,7) mức 4, cột rơi (7,10..3,7). Mesh có 3 tam giác nắp ở mặt phẳng z = 7, pháp tuyến (0,0,1), cạnh x = 8: Q = (8, 11.825, 7), các điểm cung (7.85, 11.785, 7), (7.96, 11.675, 7), (8, 11.525, 7); bên x = 7 đối xứng. Nắp là hình tam giác cong lõm, mũi ở góc ô Q, nằm ngoài cung bo của mép tràn. Render thấy đúng hình "gai" như ảnh user `~/Documents/User/khang/Game/art-test/shots/user0082_lip_spike.webp` [run].
- **Lỗi 8.** Màu mảng ở ảnh `user0082_underwater.png` đồng đều (47,77,138) tại hai điểm cách xa nhau, khớp màu mặt khuất của thân áo xanh trong `user0082_lip_spike.webp` (44,85,153) [run, đọc pixel]. Khi camera ở pitch âm (nhìn lên, `minPitch = -20`), `SphereCast` trúng đáy/vách nên `CurrentDistance` có thể xuống tới 0,3 (`FollowCamera.cs:129`), camera nằm sát đầu nhân vật. Kết luận này **chưa xác minh trong Play**: bước 1 xác minh trước khi sửa.
- **Lỗi 7.** Thử thêm cấu hình "khối cát đè trên đầu cột" nhưng không thấy gờ. Giả thuyết: nắp lỗi 6 nhìn nghiêng [chưa xác minh].

**Code:**
- `client/Assets/Game/World/BlockMesher.cs`:
  - `void AddWaterColumn(int3 g, in WaterCol col)` (`:1057`). Mặt trên mép tràn là hình chữ nhật thu theo cạnh hở (`:1064–1073`). Vòng `for (int d = 0; d < 4; d++)` (`:1075`) vẽ cho mỗi cạnh hở:
    - `t0/t1`, `P0/P1` (điểm trong mặt trên, `pe` = `1 − R` hoặc `R`), `E0/E1` (điểm trên mặt phẳng cạnh) ở `:1082–1090`;
    - mặt bên `b0, b1, s1, s0` (`:1092–1113`);
    - dải cung `WaterArcVertex(col, P0/P1, outDir, k, true, default)` (`:1096–1105`);
    - **nắp** (`:1115–1134`). Điều kiện vẽ nắp:
      ```
      if (end == 0 ? r0 : r1) continue;                       // đầu đã bo
      nb = g + eDir; if (MaterialAt(nb) != Water) continue;   // không phải nước
      if (IsLipCell(nb) && MaterialAt(nb + outDir) == Empty) continue; // mép tràn nối tiếp cùng hướng
      ```
  - `int WaterArcVertex(in WaterCol col, float2 puv, float3 outDir, int k, bool withOutNormal, float3 forcedNormal)` (`:1045`): `p = Pos(puv, c.y + H(puv)) + outDir·R·sinθ − up·rv·(1 − cosθ)`, θ = k·90°/S.
  - `struct WaterCol` (`:1016–1037`): `h`, `open`, `lip`, `rv`, `Rounded(sx, sz)`, `Hc(sx, sz)`, `H(u, v)`, `Pos(u, y, v)`.
  - `int AddWaterVertex(float3 p, float3 n)` hàn đỉnh theo vị trí + pháp tuyến. `void AddWaterTri(int a, int b, int c)` tự quay mặt trước theo tổng pháp tuyến 3 đỉnh và bỏ tam giác trùng chỉ số (`:1509`).
- `client/Assets/Game/Tests/World/BlockMesherTests.cs`:
  - `LipCapsMeetUpstreamCell` (`:1019–1052`) đang khẳng định có 6 tam giác nắp, nên phải viết lại (bước 3);
  - helper `HasWaterVertexNear(mesh, pos, normal)` (`:941`, sai số 1e-3), `AssertWatertight(m, lo, hi)` (`:1080`), `AssertNoDuplicateWaterVertices` (`:952`), `StoneFloor16()` (`:921`).
- `client/Assets/Game/Client/FollowCamera.cs`: `public void SnapNow()` (`:112`); nhánh góc nhìn thứ ba `:124–130`. `SetFirstPerson` (`:61–70`) gọi `target.SetBodyVisible(!on)`. `OnEnable`/`OnDisable` (`:88–89`).
- `client/Assets/Game/Client/PlayerCharacter.cs:58`: `public void SetBodyVisible(bool on)`; `public bool BodyVisible { get; private set; }`.
- Cấu hình người chơi:
  - `client/Assets/Game/Core/Sim/SimConfig.cs`: class `PlayerConfig` (trường `:160–169`), kiểm hợp lệ `:418–425`;
  - `client/Assets/Game/Core/PlayerMotion.cs`: thuộc tính `:27–30`, `Configure` `:54–58`;
  - `design/data/sim_m1.json:39` và bản sao `client/Assets/Game/Client/Resources/Config/sim_m1.json:39`. `ConfigTests` bắt hai file phải giống hệt nhau.
- Test cũ: `client/Assets/Game/Tests/Core/Sim/SimConfigTests.cs:65–67, :109–110`; `client/Assets/Game/Tests/Client/CameraPcPlayTests.cs` (SetUp `:22–55`, `Teleport`, `Spot`, `BodyVisibilityOnDisableAndFly` `:187–201`).

## Quyết định đã chốt (không được đổi)
Ký hiệu: `R` = `WaterFilletRadius`, `S` = `WaterFilletSegments`. Với một cạnh hở `d` của ô mép tràn: `t` = toạ độ dọc cạnh (0..1), `cs`, `pe`, `outDir`, `E(t)` = điểm trên mặt phẳng cạnh, `P(t)` = điểm trong cách cạnh `R`, giống code hiện tại.

1. **Đầu thon** = đúng những đầu dải trước đây vẽ nắp (giữ nguyên ba điều kiện ở `:1117–1124`). Tính trước vòng vẽ: `bool taper[d][end]` cho cả 4 cạnh, vì mặt trên cần biết. Thêm hàm `bool LipTaper(int3 g, in WaterCol col, int d, int end)`, trả `false` khi `!col.lip` hoặc `!col.open[d]`. **Xoá hẳn code vẽ nắp.**
2. **Hình đoạn thon** ở đầu `end` (đầu `t = 0` khi end = 0, `t = 1` khi end = 1; gọi `tE` = t ở đầu đó, `tR` = `tE + R` hoặc `tE − R`):
   - **Đỉnh mũi `V`:** vị trí `Pos(E(tE).x, c.y + Hc(góc ở đầu đó), E(tE).y)`, pháp tuyến `normalize(outDir + up)`.
   - **Cột cung đầy đủ** tại `tR`: `a_k = WaterArcVertex(col, P(tR), outDir, k, true, default)`, k = 0..S.
   - Quạt `AddWaterTri(V, a_k, a_{k+1})` với k = 0..S−1.
   - Dải cung chính chạy từ `tR` (thay cho `t0`/`t1` ở đầu đó) tới đầu kia, như cũ.
3. **Mặt bên** cạnh `d`, khi có đầu thon. Gọi:
   - `L` = đỉnh trên phía đầu 0: `a_S` tại `tR` nếu đầu 0 thon, ngược lại `s0` như cũ;
   - `Rr` = đỉnh trên phía đầu 1, tương tự;
   - `sV0`/`sV1` = đỉnh ở vị trí của `V` nhưng pháp tuyến `outDir`.
   
   Tam giác: `(b0, b1, Rr)`, `(b0, Rr, L)`; thêm `(b0, L, sV0)` nếu đầu 0 thon, `(b1, sV1, Rr)` nếu đầu 1 thon. `b0, b1` giữ nguyên (`E0/E1` ở đáy `yb`). Không thêm đỉnh ở cạnh đáy, để mối nối với ô cột bên dưới không sinh T-junction.
4. **Mặt trên mép tràn** (thay `:1064–1073`):
   - Hình chữ nhật `[u0,u1]×[v0,v1]` giữ như cũ. Mỗi đầu thon chèn điểm `P(tR)` vào cạnh phía `d` của hình chữ nhật (điểm này nằm trên cạnh đó).
   - Vẽ đa giác lồi đó bằng quạt từ tâm `((u0+u1)/2, (v0+v1)/2)`: đỉnh tâm cao `c.y + H(tâm)`, pháp tuyến up. Các đỉnh biên đi theo thứ tự vòng quanh hình chữ nhật.
   - Mỗi đầu thon thêm 1 tam giác `(E(tE), P(tR), K)`, pháp tuyến up, độ cao theo `H`. `K` = góc hình chữ nhật gần `E(tE)`: toạ độ theo trục cạnh `d` = `pe`, theo trục dọc cạnh = `tE`.
   - Không có đầu thon → kết quả giống hệt hiện tại (2 tam giác).
5. Không đổi: ô không phải mép tràn, trụ bo, cầu góc, bo lõm chân thác (`AddFootFillets`), mặt ao, shader.
6. **Ẩn thân khi camera gần (lỗi 8):** trong `SnapNow`, nhánh góc nhìn thứ ba, sau khi có `CurrentDistance`:
   ```csharp
   bool hide = CurrentDistance < PlayerMotion.CameraHideBodyDistance;
   if (target.BodyVisible == hide) target.SetBodyVisible(!hide);
   ```
   Nhánh góc nhìn thứ nhất giữ nguyên. Ngưỡng là tham số mới `player.cameraHideBodyDistance` = **1.0** [số tạm, chưa chốt], kiểm hợp lệ trong `[0.3, 3]` với thông báo `"player.cameraHideBodyDistance must be in [0.3, 3]."`. `PlayerMotion.CameraHideBodyDistance` mặc định `1f`.
7. Lỗi 7 không có thay đổi riêng (xem Mục tiêu).

## Phạm vi
- Được sửa/tạo:
  - `client/Assets/Game/World/BlockMesher.cs`
  - `client/Assets/Game/Tests/World/BlockMesherTests.cs`
  - `client/Assets/Game/Client/FollowCamera.cs`
  - `client/Assets/Game/Core/Sim/SimConfig.cs`
  - `client/Assets/Game/Core/PlayerMotion.cs`
  - `design/data/sim_m1.json`, `client/Assets/Game/Client/Resources/Config/sim_m1.json`
  - `client/Assets/Game/Tests/Core/Sim/SimConfigTests.cs`
  - `client/Assets/Game/Tests/Client/CameraPcPlayTests.cs`
  - `design/02-gameplay/controls-ui.md` (1 câu, bước 7)
  - brief này (mục Kết quả)
- Không được sửa: shader, `TerrainChunkView`, `PlayerCharacter`, `UnderwaterView`, mô phỏng nước (`WorldEditor`/Game.Core nước), task file, roadmap.

## Các bước
Ghi giờ bắt đầu/kết thúc bằng `date`.

1. **Xác minh lỗi 8 trước khi sửa:**
   - Scene SimView, Play, đi tới ao dưới thác demo, đứng trong nước sát vách.
   - Xoay camera nhìn lên hết cỡ (pitch = `minPitch` −20, qua `FollowCamera.SetAngles` bằng MCP `execute_code` nếu cần) sao cho `CurrentDistance` < 1.
   - Chụp 1 ảnh → `~/Documents/User/khang/Game/art-test/shots/brief0083_cam_before.png`.
   - Ẩn thân tạm bằng `SetBodyVisible(false)` qua `execute_code`, chụp 1 ảnh → `brief0083_cam_hidden.png`.
   - Xong khi: ảnh trước có mảng xanh đậm phẳng cạnh thẳng, ảnh sau không còn. **Nếu ảnh sau vẫn còn mảng đó → dừng, `cần Opus`**, ghi `CurrentDistance` và vị trí camera vào Vướng mắc; không làm bước 6–7. Thoát Play.
2. **Test tái hiện lỗi 6 (viết trước, phải fail với code cũ)** — `BlockMesherTests.cs`, test mới `LipTapersAtChannelSpill`:
   - Dựng thế giới `WorldData(new WorldSize(16,16,16))`:
     - y = 0 Bedrock, y = 1 Stone toàn map;
     - Stone ở y 2..11 cho mọi x khi z ≤ 6;
     - nước `Cell.Make(BlockMaterial.Water, Cell.MaxWaterLevel)` ở y = 11, x 5..9, z 1..5 (ghi đè đá).
   - `var bag = new PlayerBag(); bag.Add(BlockMaterial.Stone, 1000); var e = new WorldEditor(w, bag, 1, -1);`. Assert `e.TryDig(7, 11, 6) == EditResult.Ok`; `e.SettleWater(10000)`; Assert `!e.HasPendingWater`.
   - Assert tiền đề:
     - (7,11,7) là nước mức 4, không rơi;
     - (7,10,7) đang rơi (`Cell.IsFalling`).
   - `var mesh = BlockMesher.Build(w, 0, 0, 0, true, 5);`
   - Assert:
     - (a) **không có nắp:** 0 tam giác nước có cả 3 đỉnh `|z − 7| < 1e-4`, `x ∈ [7, 8]`, `y > 11 + 1e-4`;
     - (b) có đỉnh mũi `(8, 11.825, 7)` pháp tuyến `(0.7071, 0.7071, 0)`, và `(7, 11.825, 7)` pháp tuyến `(−0.7071, 0.7071, 0)`;
     - (c) có đỉnh cung đầy đủ `(8, 11.5025, 7.3)` pháp tuyến `(1,0,0)`, và đỉnh mặt trên `(7.7, 11.8025, 7.3)` pháp tuyến up. Độ cao: `Hc` ở cạnh z = 7 là 0.825 và ở cạnh z = 8 là 0.75, nên `H(·, v=0.3)` = 0.8025, và `rv` = 0.3;
     - (d) dáng thon: mọi đỉnh nước có `|x − 8| < 1e-4`, `z ∈ [7, 7.3]` thoả `y ≤ 11.825 − (11.825 − 11.5025)·(z − 7)/0.3 + 1e-3`;
     - (e) `AssertWatertight(mesh, new float3(7, 11, 7.001f), new float3(8, 12, 8))`;
     - (f) `AssertNoDuplicateWaterVertices(mesh)`.
   - Chạy với code cũ: (a) phải fail. Ghi dòng lỗi vào Kết quả test.
3. **Viết lại `LipCapsMeetUpstreamCell` → `LipTapersIntoUpstreamCell`** (cùng thế giới, giữ đoạn dựng `:1021–1031`):
   - Bỏ đếm 6 nắp. Thay bằng: 0 tam giác nước có cả 3 đỉnh `|x − 5| < 1e-4` và `y > 9 + 1e-4`.
   - Có đỉnh `(5, 9.825, 4)` pháp tuyến `(0, 0.7071, −0.7071)` và `(5, 9.825, 5)` pháp tuyến `(0, 0.7071, 0.7071)`.
   - Có `(5.3, 9.5025, 4)` pháp tuyến `(0,0,−1)`, `(5.3, 9.5025, 5)` pháp tuyến `(0,0,1)`.
   - Có `(5.3, 9.8025, 4.3)` và `(5.3, 9.8025, 4.7)` pháp tuyến up.
   - `AssertWatertight(mesh, new float3(5.001f, 9, 4), new float3(6, 10, 5))`.
   - Giữ vòng "sharp column corner" ở x = 6 và `AssertNoDuplicateWaterVertices`.
4. **Sửa `BlockMesher.cs`** theo QĐ 1–5:
   - thêm `LipTaper`, tính `taper` trước mặt trên;
   - viết lại mặt trên (QĐ 4), đoạn thon + dải chính (QĐ 2), mặt bên (QĐ 3);
   - xoá khối nắp `:1115–1134`;
   - sửa `<summary>` của `AddWaterColumn` (`:1056`): "nắp" → "đoạn thon".
   
   Xong khi: test bước 2 + 3 đạt, và mọi test `BlockMesherTests` khác vẫn đạt (đặc biệt `FallingColumnIsRoundedAndContinuous`, `Foot*`, `WaterVerticesAreWelded`).
5. **Lệch số khi chạy test:** nếu một số kỳ vọng ở bước 2c/3 lệch ≤ 1e-3 do làm tròn thì sửa theo số thật và ghi lại. Lệch lớn hơn → dừng, `cần Opus` (không chỉnh kỳ vọng cho khớp code).
6. **Ẩn thân khi camera gần (QĐ 6):**
   - `SimConfig.cs`: thêm `public float cameraHideBodyDistance = 1f;` sau `observationHeight`, và kiểm hợp lệ sau dòng `observationHeight`.
   - `PlayerMotion.cs`: thêm thuộc tính sau `ObservationHeight`, doc-comment `/// <summary>Camera góc 3 gần tâm xoay hơn chừng này (m) thì ẩn thân nhân vật (brief 0083, số tạm).</summary>`; thêm `CameraHideBodyDistance = p.cameraHideBodyDistance;` trong `Configure`.
   - Hai file `sim_m1.json`: thêm `, "cameraHideBodyDistance": 1` ngay sau `"observationHeight": 2` (hai file giống hệt nhau).
   - `FollowCamera.cs`: thêm 2 dòng QĐ 6 sau `:129`, trước `SetPositionAndRotation`.
   - `SimConfigTests.cs`: thêm `Assert.AreEqual(1f, cfg.player.cameraHideBodyDistance);` cạnh `:67`, và `Bad(c => c.player.cameraHideBodyDistance = 0.2f, "cameraHideBodyDistance 0.2");` cạnh `:110`.
7. **Test PlayMode `CameraHidesBodyWhenClose`** trong `CameraPcPlayTests.cs`:
   - `yield return Teleport(Spot);` → `follow.SetAngles(0f, 10f);` → `yield return null;` → Assert `player.BodyVisible`, `follow.CurrentDistance > 1f`.
   - Tạo `GameObject.CreatePrimitive(PrimitiveType.Cube)` đặt ở `pivot + rot·back·0.8` (pivot = `player.transform.position + Vector3.up * follow.pivotHeight`, rot = `Quaternion.Euler(10f, 0f, 0f)`), scale 0.5 → `yield return null;` → Assert `follow.CurrentDistance < 1f` và `!player.BodyVisible`.
   - `Object.Destroy(cube)`; `yield return null; yield return null;` → Assert `player.BodyVisible`.
   - `design/02-gameplay/controls-ui.md:9`: thêm vào cuối câu đầu mục camera: ` Camera bị khối đẩy sát nhân vật (< 1 m [số tạm], sim_m1.json \`cameraHideBodyDistance\`) thì ẩn thân nhân vật (brief 0083).`

## Tiêu chí xong
- `LipTapersAtChannelSpill` fail trước khi sửa (ghi dòng lỗi), đạt sau khi sửa. `LipTapersIntoUpstreamCell` đạt.
- EditMode toàn bộ, PlayMode toàn bộ đạt; console 0 Error.
- Ảnh bước 1 và bước 8 (mục Lệnh) có đường dẫn trong Kết quả.
- `git status --short` chỉ còn file trong Phạm vi.

## Test sau khi xong
### Phần cần test
- Mép tràn thon, không còn gai (lỗi 6) · `LipTapersAtChannelSpill` (thác thật do mô phỏng) + `LipTapersIntoUpstreamCell` · đạt; không còn tam giác nắp; dáng thon tuyến tính; vùng mép tràn kín.
- Cột rơi, mép tràn bốn phía bo (không có đầu thon) không đổi · `FallingColumnIsRoundedAndContinuous`, `WaterVerticesAreWelded` · đạt, không sửa kỳ vọng.
- Bo lõm chân thác không đổi · `FootFilletFullPond`, `FootMixedCornerAndCaps`, `FootInnerCornerMiter` · đạt.
- Mặt nước/mặt bên ô thường · `WaterSurfaceOnlyWhereAirIsAbove`, `FallingWaterColumnHasSideFaces`, `FlowingWaterSurfaceFollowsLevel`, `EnclosedWaterHasOnlyTopFaces` · đạt.
- Nước trong suốt không chồng lớp (0082) · `WaterLayerPlayTests` · đạt.
- Ẩn thân khi camera gần (lỗi 8) · `CameraHidesBodyWhenClose` · đạt.
- Góc nhìn thứ nhất / bật tắt thân cũ · `FirstPersonAtEyes`, `ToggleAndResetFromFirstPerson`, `BodyVisibilityOnDisableAndFly` · đạt.
- Cấu hình · `SimConfigTests` (mặc định 1, 0.2 bị từ chối), `ConfigTests` (hai bản `sim_m1.json` giống hệt nhau) · đạt.

### Lệnh executor chạy
1. Sau mỗi lần sửa code: MCP `refresh_unity` → `read_console` (Error) = 0.
2. Bước 2: MCP `run_tests` EditMode lọc `LipTapersAtChannelSpill` → ghi dòng fail (code cũ).
3. Sau bước 4: `run_tests` EditMode lọc `BlockMesherTests` → đạt, ghi dòng tổng.
4. Sau bước 7: `run_tests` EditMode lọc `SimConfigTests|ConfigTests`, PlayMode lọc `CameraPcPlayTests` → đạt.
5. `run_tests` EditMode toàn bộ; PlayMode toàn bộ. Ghi dòng tổng. Nếu MCP rớt khi đang chạy: `sleep 60` rồi `get_test_job`, lặp lại. Không kết thúc lượt khi test còn chạy.
6. `python3 tools/check_design.py` → exit 0.
7. Dọn: thoát Play; `git checkout -- client/ProjectSettings/EditorSettings.asset`; xoá `client/Assets/InitTestScene*.unity(.meta)` nếu có. `git status --short` chỉ còn file trong Phạm vi.
8. Chụp **2** ảnh Game view (SimView, Play, thác demo; lưu vào `~/Documents/User/khang/Game/art-test/shots/`):
   - `brief0083_lip_front.png`: nhìn chếch từ dưới phía trước mép tràn, thấy chỗ mép tràn nối vách;
   - `brief0083_lip_side.png`: nhìn ngang sát vách, ngang tầm đầu cột (góc như `user0082_column_side.png`).
   
   Thoát Play, chạy lại bước dọn 7.
9. Commit code + test (`brief 0083: taper waterfall lip into upstream cell; hide body when camera is close`), rồi commit brief (`brief 0083: result`).

### User tự test
Gộp vào lần chơi thử cuối 5j. SimView, Play (Mac):
1. Thác demo, nhìn chỗ mép tràn nối vách từ phía trước-dưới và từ bên cạnh: không còn gai nhọn (lỗi 6), không còn gờ mỏng ở đầu cột (lỗi 7). Mép cột thon dần vào dòng nước phía sau.
2. Còn gờ ở lỗi 7 → chụp lại ảnh kèm hướng nhìn để Planner mở brief riêng.
3. Đứng trong ao dưới chân thác sát vách, giữ chuột giữa kéo nhìn lên hết cỡ: không còn mảng xanh đậm phẳng (lỗi 8). Lùi ra chỗ trống: thân nhân vật hiện lại. Ngưỡng 1 m có hợp không (sửa `cameraHideBodyDistance` trong `sim_m1.json`).

## Dừng và chuyển lại Opus khi
- Brief mơ hồ hoặc mâu thuẫn với code thật (vd điều kiện nắp ở `:1117–1124` khác mô tả).
- Bước 1: ẩn thân vẫn còn mảng xanh đậm.
- Kỳ vọng số ở bước 2–3 lệch > 1e-3.
- Cần một quyết định thuộc AGENTS.md §3.4 hoặc ngoài "Quyết định đã chốt".
- Cùng một lỗi sửa 2 lần vẫn chưa qua.
- Phải sửa file ngoài "Phạm vi".

---

## Kết quả
- Base commit: 9c90e8e
- Chạy bằng: claude-opus-5-5 · medium
- Bắt đầu: 2026-10-29 18:37 · Kết thúc: 2026-10-29 18:54 (~17 phút)
- Commit code: `ec4dd3b` — file: `BlockMesher.cs`, `BlockMesherTests.cs`, `FollowCamera.cs`, `SimConfig.cs`, `PlayerMotion.cs`, 2 × `sim_m1.json`, `SimConfigTests.cs`, `CameraPcPlayTests.cs`, `design/02-gameplay/controls-ui.md`.
- Bước 1 (lỗi 8): SimView Play, nhân vật ở (137.6, 20.05, 200.5) trong ao chân thác demo sát vách đất x = 138, `SetAngles(-90, -20)` → `CurrentDistance` = 0.3, camera (137.88, 21.55, 200.50). Ảnh trước có khối tối phẳng cạnh thẳng ở giữa dưới khung; ẩn thân → khối đó mất [run]. Lưu ý: ao demo chỉ sâu 1 ô nên camera không nằm dưới mặt nước như ảnh user; xác minh được "thân che camera" chứ không tái hiện đúng góc dưới nước.
  - `~/Documents/User/khang/Game/art-test/shots/brief0083_cam_before.png`, `brief0083_cam_hidden.png`.
- BlockMesher: thêm `LipTaper`; tính `tp0/tp1` (bool4) trước mặt trên; mặt trên có đầu thon → quạt từ tâm qua `FixedList128Bytes<float2>` (không đầu thon → 2 tam giác như cũ); đoạn thon = quạt V→cung tại tR + `(b0, s0, sV)` / `(b1, sV, s1)` + tam giác mặt trên `(E, P(tR), K)`; xoá khối nắp; sửa summary.
- Lệch brief (nhỏ, trong phạm vi):
  - `CameraHidesBodyWhenClose`: thêm `Physics.SyncTransforms()` sau khi đặt khối (không có thì SphereCast chưa thấy khối mới, `CurrentDistance` = 6.30 → fail); khối thành field `cube`, `TearDown` huỷ nếu còn (lần fail đầu khối sót lại làm `ToggleAndResetFromFirstPerson` fail theo).
  - `LipCapsMeetUpstreamCell` đổi tên + thay assert như bước 3; thêm helper `CountWaterTris`.
  - controls-ui.md: câu mới thêm vào cuối dòng mục camera (sau "X = góc nhìn thứ nhất."), không chèn giữa dòng.
- Ảnh lệnh 8: `~/Documents/User/khang/Game/art-test/shots/brief0083_lip_front.png`, `brief0083_lip_side.png` — nhìn qua không thấy gai/gờ ở chỗ mép tràn nối kênh và vách (chờ user xác nhận).

## Kết quả test
- Mép tràn thon (lỗi 6) · `LipTapersAtChannelSpill` code cũ fail: `lip cap Expected: 0 But was: 6` (BlockMesherTests.cs:1057); `LipTapersIntoUpstreamCell` code cũ cũng fail `Expected: 0 But was: 6`. Sau sửa cả hai đạt, không chỉnh số kỳ vọng nào · **đạt**.
- Cột rơi / mép tràn bốn phía bo, chân thác, mặt nước thường · `BlockMesherTests` 49/49 Passed, không sửa kỳ vọng test cũ · **đạt**.
- Nước không chồng lớp (0082) · `WaterLayerPlayTests` trong PlayMode toàn bộ · **đạt**.
- Ẩn thân khi camera gần (lỗi 8) · `CameraHidesBodyWhenClose` · **đạt**; `CameraPcPlayTests` 8/8 (gồm `FirstPersonAtEyes`, `ToggleAndResetFromFirstPerson`, `BodyVisibilityOnDisableAndFly`) · **đạt**.
- Cấu hình · `SimConfigTests`, `ConfigTests` trong EditMode toàn bộ · **đạt**; `cmp` hai `sim_m1.json` giống hệt.
- Lệnh: `refresh_unity` + `read_console` Error = 0 sau mỗi lần sửa; EditMode toàn bộ **660/660 Passed**; PlayMode toàn bộ **278/278 Passed**; `python3 tools/check_design.py` exit 0; dọn: EditorSettings.asset checkout, không còn InitTestScene, `git status --short` sạch ngoài brief.
- Lỗi 7, hình mép tràn/cảm giác ẩn thân, ngưỡng 1 m · **chờ user** (gộp cuối 5j).

## Vướng mắc

---

## Review
- Chạy bằng Opus · medium khớp Model thực thi; ~17 phút.
- Mọi dòng Phần cần test có kết quả: 2 test mép tràn fail với code cũ (6 tam giác nắp), đạt sau sửa; `CameraHidesBodyWhenClose` đạt; EditMode 660/660, PlayMode 278/278 [run trong brief].
- `git show --stat ec4dd3b`: 10 file, đều trong Phạm vi. Lệch nhỏ (thêm `Physics.SyncTransforms()` trong test, đổi tên `LipCapsMeetUpstreamCell` theo bước 3) hợp lý, không đổi QĐ.
- Lỗi 7, hình mép tràn, cảm giác ẩn thân: user xem gộp cuối 5j (user 2026-10-29).
- Kết luận: đã duyệt
