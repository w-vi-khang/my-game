# Brief 0080 — World 5i (phần 1): mesh nước liền (hàn đỉnh), cột nước rơi bo cạnh đứng, bo lồi mép tràn

> Trạng thái: chờ làm
> Review: đầy đủ (user xem gộp sau brief 0081 — bo lõm chân thác)
> Model thực thi: Sonnet · high
> Cập nhật: 2026-10-28 05:10

## Mục tiêu
Làm phần hiển thị của [0043](../../design/decisions/0043-nuoc-cay-cha-con-tang-muc-boi-tuong.md) mục 6 trong `BlockMesher`, trừ bo lõm chân thác (brief 0081):
- mesh nước một chunk dùng chung đỉnh (đỉnh trùng vị trí + pháp tuyến được gộp);
- cột nước rơi là một khối liền, bo tròn 4 cạnh đứng ở chỗ hai mặt bên cùng hở;
- ô trên cùng của cột (mép tràn) bo lồi giữa mặt nước và mặt đổ xuống.
Bán kính và số đoạn cung là tham số trong `water_m1.json`.

## Bối cảnh cần đọc
- Quyết định: `design/decisions/0043-nuoc-cay-cha-con-tang-muc-boi-tuong.md` mục 6 "Hiển thị". Task: `.ai/tasks/m1-world-nuoc-cay-cha-con.md`.
- `client/Assets/Game/World/BlockMesher.cs` (1053 dòng):
  - `:43` `public const float WaterSurfaceDrop = 0.1f;` (hằng số khác ở `:26-46`).
  - `:254` `Build(...)`, `:258` `BuildAll(...)`, `:267` chữ ký hiện tại:
    `public static unsafe ChunkMesh[] BuildChunks(WorldData world, IReadOnlyList<int> chunkIndices, bool grassLip = true, int waterFullLevel = Cell.MaxWaterLevel)`; kiểm `waterFullLevel` ở `:269-270`; gán vào job ở `:305`.
  - `:376-377` `N = 16`, `V = 17`; `:386` trường job `public int WaterFullLevel;`.
  - `:391` `Execute()`; nhánh nước `:418-424`: ô nước có ô trên `Empty` → `AddWaterSurface(c, g)`; luôn `AddWaterSides(c, g)`. `c` = toạ độ trong chunk (đỉnh mesh dùng `c`), `g` = toạ độ world.
  - `:907-918` `AddWaterSurface` (gọi thẳng `AddVertex`), `:920-944` `AddWaterSides` (mặt bên khi ô ngang `Empty`, mặt đáy khi ô dưới `Empty`; đỉnh trên theo `CornerHeight`).
  - `:946-955` `CellAt`, `:956-967` `WaterHeight`, `:969-987` `CornerHeight(int3 g, int dx, int dz)`.
  - `:988-997` `AddWaterQuad(float3 p0, float3 p1, float3 p2, float3 p3, float3 n)` (tạo 4 đỉnh mới); `:999-1006` `AddWaterTri(int a, int b, int c, float3 pa, float3 pb, float3 pc, float3 n)` (đảo thứ tự nếu `dot(cross(pb−pa, pc−pa), n) < 0`).
  - `:1008-1015` `AddVertex(float3 p, float3 n, uint color)`; `:1027` `MaterialAt(int3 p)`.
- `client/Assets/Game/World/ChunkMesh.cs:20` `WaterTriangleCount`.
- `client/Assets/Game/Core/WaterConfig.cs` (30 dòng, `JsonUtility`, `Validate()` ném `ArgumentException`).
- `client/Assets/Game/Client/TerrainChunkView.cs`: `:25` `public float waterStepsPerSecond = 8f;`; `:228-231` nạp config; `:389` `BlockMesher.BuildChunks(World, chunkIndices, true, Editor.MaxLevel)`.
- JSON: `design/data/water_m1.json` và bản sao `client/Assets/Game/Client/Resources/Config/water_m1.json` (giống hệt nhau).
- Test: `Tests/World/BlockMesherTests.cs:800-1000` (mặt nước; helper `StoneFloor16` `:921`, `HasWaterVertex` `:930`, `Water()` `:835`), `Tests/Core/WaterConfigTests.cs`, `Tests/Client/WaterFlowPlayTests.cs:95` `WaterfallColumnIsMeshed`.
- `Cell`: `Cell.IsFlowing(ushort)`, `Cell.IsFalling(ushort)`, `Cell.GetMaterial(ushort)`, `Cell.MakeFlowing(ushort cell, int level, bool falling)`.
- API mới duy nhất: `Unity.Collections.NativeHashMap<TKey, TValue>` (com.unity.collections 6.4.0 trong `Packages/manifest.json`) [docs](https://docs.unity3d.com/Packages/com.unity.collections@6.4/api/Unity.Collections.NativeHashMap-2.html):
  - `public NativeHashMap(int initialCapacity, AllocatorManager.AllocatorHandle allocator)`
  - `public bool TryGetValue(TKey key, out TValue item)`, `public void Add(TKey key, TValue item)`, `public void Dispose()`
  - ràng buộc `TKey : unmanaged, IEquatable<TKey>`, `TValue : unmanaged`.

## Quyết định đã chốt (không được đổi)
Ký hiệu trong ô nước `g` (toạ độ world), `c` (trong chunk): `u = x − c.x`, `v = z − c.z` ∈ [0, 1]. "Mặt hở" hướng `d` ∈ {+x, −x, +z, −z} = `MaterialAt(g + d) == Empty` (đúng điều kiện vẽ mặt bên hiện nay). `R` = `WaterFilletRadius`, `S` = `WaterFilletSegments`. `Hc(sx, sz)` = `CornerHeight(g, sx, sz)` (sx, sz ∈ {0, 1}).

1. **Tham số (Planner, [số tạm]):** `WaterConfig` thêm `public float filletRadius = 0.3f;` (hợp lệ 0,05..0,45) và `public int filletSegments = 3;` (1..8), `configVersion = 3`. Thứ tự trường: `configVersion, maxLevel, stepsPerSecond, wallBoostMinSolid, filletRadius, filletSegments`. JSON (hai file giống hệt):
   ```json
   {
       "configVersion": 3,
       "maxLevel": 5,
       "stepsPerSecond": 8,
       "wallBoostMinSolid": 2,
       "filletRadius": 0.3,
       "filletSegments": 3
   }
   ```
   `Validate` ném `ArgumentException("filletRadius must be in 0.05..0.45.")` / `("filletSegments must be in 1..8.")`.
2. **`BuildChunks`** thêm 2 tham số cuối: `float waterFilletRadius = DefaultWaterFilletRadius, int waterFilletSegments = DefaultWaterFilletSegments` với `public const float DefaultWaterFilletRadius = 0.3f; public const int DefaultWaterFilletSegments = 3;` cạnh `:43`. Ngoài khoảng (0,05..0,45; 1..8) → `ArgumentOutOfRangeException`. `Build` và `BuildAll` thêm cùng 2 tham số tuỳ chọn, chuyển tiếp. Job thêm trường `WaterFilletRadius`, `WaterFilletSegments`. `TerrainChunkView` lưu `waterCfg.filletRadius`/`filletSegments` vào 2 trường private, truyền vào `BuildChunks` ở `:389`.
3. **Hàn đỉnh:** mọi đỉnh nước (mặt trên, mặt bên, mặt đáy, cung, cầu, trụ, nắp) đi qua `int AddWaterVertex(float3 p, float3 n)`: khoá = `(int3)math.round(p * 1024f)` + `(int3)math.round(n * 1024f)` (struct `WaterVertexKey : IEquatable<WaterVertexKey>` có `GetHashCode`); đã có → trả chỉ số cũ; chưa → `AddVertex(p, n, 0)` rồi `Add`. Bảng `NativeHashMap<WaterVertexKey, int>(1024, Allocator.Temp)` tạo đầu `Execute`, `Dispose` cuối `Execute`. Không hàn đỉnh địa hình. `AddWaterQuad` lấy 4 chỉ số qua `AddWaterVertex` rồi gọi `AddWaterTri` 2 lần như cũ. Hướng tam giác cong: `n` truyền cho `AddWaterTri` = `normalize(tổng pháp tuyến 3 đỉnh)`.
4. **Ô cột:** ô nước là "ô cột" khi (ô chảy và đang rơi: `Cell.IsFlowing && Cell.IsFalling`) **hoặc** ô ngay dưới (`g − y`) là nước chảy đang rơi. Ô không phải ô cột: vẽ y như hiện nay (chỉ khác hàn đỉnh).
5. **Góc bo đứng** của ô cột: góc `(sx, sz)` bo khi mặt hở cả hướng x phía `sx` (sx = 1 → +x, 0 → −x) **và** hướng z phía `sz`. Mặt bên trên cạnh hở `d` chạy theo trục cạnh `t` (trục còn lại) từ `t0 = R` nếu góc đầu t = 0 bo, ngược lại 0, tới `t1 = 1 − R` nếu góc đầu t = 1 bo, ngược lại 1. Mặt đáy giữ nguyên hình vuông.
6. **Trụ bo đứng** ở mỗi góc bo: trục tại `(sx ? 1 − R : R, sz ? 1 − R : R)`; `dA` = hướng x của góc (±x), `dB` = hướng z (±z); vòng φ_j = j·90°/S (j = 0..S), điểm = trục + R·(cos φ_j·dA + sin φ_j·dB), pháp tuyến `cos φ_j·dA + sin φ_j·dB`; S dải tứ giác từ `c.y` tới đỉnh trụ (QĐ 7/8).
7. **Ô cột không phải mép** (ô trên không `Empty`): mặt bên đỉnh tại t = `c.y + lerp(Hc ở hai đầu cạnh, t)`; đỉnh trụ = `c.y + Hc(sx, sz)`. Không vẽ mặt trên (như cũ).
8. **Mép tràn** = ô cột có ô trên `Empty`. `rv = min(R, min 4 giá trị Hc)`. Hàm `H(u, v)` = nội suy song tuyến 4 giá trị `Hc`.
   - a. **Mặt trên:** 1 tứ giác, pháp tuyến +y, `u ∈ [u0, u1]`, `v ∈ [v0, v1]` với `u0 = R` nếu hở −x, ngược lại 0; `u1 = 1 − R` nếu hở +x, ngược lại 1; `v0`, `v1` tương tự theo −z, +z. Đỉnh cao `c.y + H(u, v)`.
   - b. **Dải cung** trên mỗi cạnh hở `d` (`out` = d): ở hai đầu `t0`, `t1` (QĐ 5), điểm trong `P` = điểm trên cạnh mặt trên phía `d` tại t, cao `c.y + H(P)`; cung k = 0..S, θ_k = k·90°/S: điểm `P + out·R·sin θ_k − up·rv·(1 − cos θ_k)`, pháp tuyến `normalize(out·rv·sin θ_k + up·R·cos θ_k)`. S tứ giác nối cung đầu t0 với cung đầu t1.
   - c. **Mặt bên** trên cạnh hở `d`: đáy `c.y`, đỉnh tại t = điểm cuối cung (k = S) ở t đó (cao `c.y + H(P) − rv`).
   - d. **Cầu góc** ở mỗi góc bo: tâm `Pc` = góc mặt trên phía đó, cao `hc = c.y + H(Pc)`; điểm (θ_i, φ_j), i, j = 0..S: `Pc + R·sin θ_i·(cos φ_j·dA + sin φ_j·dB) − up·rv·(1 − cos θ_i)`, pháp tuyến `normalize(rv·sin θ_i·(cos φ_j·dA + sin φ_j·dB) + up·R·cos θ_i)`. Lưới i, j = 0..S−1: tứ giác (i, j), (i+1, j), (i+1, j+1), (i, j+1); hàng i = 0 chỉ tam giác (0, j), (1, j), (1, j+1) (các điểm i = 0 trùng nhau, sau hàn là một đỉnh). Đỉnh trụ ở góc này = `hc − rv`.
   - e. **Nắp:** đầu dải (t = 0 hoặc 1) **không** bo, gọi `e` = hướng vuông góc ở đầu đó (hướng của cạnh có t đó). Vẽ nắp khi ô `g + e` là nước **và không** (là mép tràn và hở hướng `d`). Nắp nằm trên mặt phẳng biên `e` của ô: quạt từ `Q` = góc ô ở đầu đó, cao `c.y + Hc(góc đó)`, tới các điểm cung k = 0..S ở đầu đó: tam giác (Q, k, k+1); pháp tuyến nắp = `−e` (quay vào ô mép tràn).
9. Không đổi: địa hình, mép cỏ, collider, `WaterHeight`, `CornerHeight`, cách vẽ ô không phải ô cột, bộ sinh world, `WorldEditor`.
10. Ngoài phạm vi brief này: bo lõm chân thác (brief 0081).

## Phạm vi
- Được sửa: `client/Assets/Game/World/BlockMesher.cs` (nhánh nước, hàm nước, `Build`/`BuildAll`/`BuildChunks`, job, hằng số mới); `client/Assets/Game/Core/WaterConfig.cs`; `client/Assets/Game/Client/TerrainChunkView.cs` (chỉ trường lưu tham số bo, `:228-231`, `:389`); hai `water_m1.json`; test `Tests/World/BlockMesherTests.cs` (thêm test, helper mới), `Tests/Core/WaterConfigTests.cs`; brief này (Kết quả trở xuống).
- Không được sửa: mọi file khác (`WorldEditor`, `Cell`, `SimView`, `SimDemo`, scene, `design/*.md`, test PlayMode).

## Các bước
1. **Config:** QĐ 1 (`WaterConfig`, hai JSON), QĐ 2 phần `TerrainChunkView`. `WaterConfigTests`: kiểm `filletRadius` 0,3 (sai số 1e-6), `filletSegments` 3, `configVersion` 3; `ValidateRejectsBadValues` thêm `filletRadius` 0,04 và 0,46, `filletSegments` 0 và 9 ném.
2. **`BuildChunks` + job:** QĐ 2. Xong khi biên dịch, test cũ không đổi.
3. **Hàn đỉnh:** QĐ 3. Xong khi test cũ của mặt nước (`:800-1000`) vẫn đạt.
4. **Ô cột + trụ + mặt bên rút ngắn:** QĐ 4–7.
5. **Mép tràn:** QĐ 8 (a–e).
6. **Test mới** (bảng "Phần cần test"). Helper mới `HasWaterVertexNear(ChunkMesh m, float3 pos, float3 normal)` (vị trí sai số 1e-3, pháp tuyến sai số 1e-3).
7. Chạy "Lệnh executor chạy".

## Tiêu chí xong
- `refresh_unity` → console 0 Error.
- Mọi dòng "Phần cần test" đạt; EditMode + PlayMode toàn bộ đạt (ghi dòng tổng).
- `diff design/data/water_m1.json client/Assets/Game/Client/Resources/Config/water_m1.json` → không khác.

## Test sau khi xong

### Phần cần test
Mọi test mesh mới dùng `BlockMesher.Build(w, 0, 0, 0, true, 5)` (R 0,3, S 3 mặc định), `StoneFloor16()` (đá ở y = 4). `cos 30° = 0.8660`.

**mới (`BlockMesherTests`):**
- `WaterVerticesAreWelded` · `StoneFloor16`, nước nguồn `Water()` ở (6,5,6), (7,5,6) · `WaterTriangleCount` = 16; số chỉ số đỉnh khác nhau trong `WaterIndices` = 26; không hai đỉnh nước khác chỉ số cùng vị trí (1e-4) và cùng pháp tuyến (1e-4).
- `FallingColumnIsRoundedAndContinuous` · `StoneFloor16`; (8,y,8) y = 5..8 `Cell.MakeFlowing(0, 5, true)`; (8,9,8) `Water()` · 
  - không đỉnh nước nào có (x, z) = góc ô (8,8), (9,8), (8,9), (9,9) (sai số 1e-4) với y ∈ [5, 9,9];
  - có đỉnh trụ (góc +x/+z, φ = 30°) `(8.9598, 7, 8.85)` pháp tuyến `(0.8660, 0, 0.5)`;
  - có mặt trên `(8.3, 9.9, 8.3)` pháp tuyến +y; điểm cung `(8.85, 9.8598, 8.3)` pháp tuyến `(0.5, 0.8660, 0)`; cuối cung `(9, 9.6, 8.3)` pháp tuyến +x; tâm cầu `(8.7, 9.9, 8.7)` pháp tuyến +y;
  - mặt bên +x: đỉnh `(9, 5, 8.3)` và `(9, 9.6, 8.7)` pháp tuyến +x;
  - không tam giác nước nào có pháp tuyến đỉnh đầu `(0, ±1, 0)` và cả 3 đỉnh có y ∈ {6, 7, 8, 9} (không mặt ngang giữa hai ô cột);
  - mọi đỉnh nước x, z ∈ [8, 9], y ∈ [5, 9,9] (sai số 1e-4);
  - mọi tam giác: `dot(cross(b − a, c − a), tổng pháp tuyến 3 đỉnh) > 0`;
  - không hai đỉnh nước trùng (như test trên).
- `LipCapsMeetUpstreamCell` · `StoneFloor16`; đá (4,y,4) y = 5..8; A = (4,9,4) `MakeFlowing(0, 5, false)`; B = (5,9,4) `MakeFlowing(0, 4, false)`; (5,y,4) y = 5..8 `MakeFlowing(0, 5, true)` · 
  - đúng 6 tam giác nước có cả 3 đỉnh x = 5 (1e-4) và pháp tuyến đỉnh đầu `(1, 0, 0)` (2 nắp × S);
  - có đỉnh mặt trên `(5, 9.825, 4.3)` pháp tuyến +y; có đỉnh nắp `(5, 9.825, 4)` pháp tuyến +x;
  - không đỉnh nước nào có (x, z) = (6, 4) hoặc (6, 5) với y ∈ [5, 9,9];
  - không hai đỉnh nước trùng.
- `BuildRejectsBadFillet` · `StoneFloor16` · `Build(w,0,0,0,true,5, 0.04f, 3)`, `(…, 0.46f, 3)`, `(…, 0.3f, 0)`, `(…, 0.3f, 9)` đều ném `ArgumentOutOfRangeException`.

**config:** `WaterConfigTests` (bước 1) đạt, gồm test "file khớp mặc định" sẵn có.

**cũ — giữ nguyên, phải đạt:** `WaterSurfaceOnlyWhereAirIsAbove`, `FallingWaterColumnHasSideFaces` (cột nguồn, không phải ô cột → vẫn 34 tam giác), `EnclosedWaterHasOnlyTopFaces`, `FloatingWaterCellHasBottomFace`, `WaterAtMapEdgeHasNoOuterWall`, `FlowingWaterSurfaceFollowsLevel`, `FallingAndSourceWaterStayFull`, `BuildRejectsBadWaterFullLevel`; toàn bộ `BlockMesherTests`, `ChunkSeamTests`, `GoldenHashTests`, `WaterFlowTests`, `SeaFloodTests`; PlayMode `WaterFlowPlayTests` (gồm `WaterfallColumnIsMeshed`, `WaterfallPoursIntoPond`), `TerrainEditPlayTests`, `DayCycleWaterPlayTests`, `FlyCameraWaterPlayTests`, `OceanBackdropTests`.

### Lệnh executor chạy
1. MCP `refresh_unity` → `read_console` (Error) = 0.
2. MCP `run_tests` EditMode lọc `BlockMesherTests`, `WaterConfigTests`, `ConfigTests`, `ChunkSeamTests`, `GoldenHashTests`, `WaterFlowTests`, `SeaFloodTests`.
   - Test mesher ra giá trị cũ ngay sau biên dịch (Burst chưa nạp bản mới — brief 0079): thêm rồi gỡ một dòng chú thích trong `BlockMesher.cs`, `refresh_unity`, chạy lại; vẫn cũ → xoá `client/Library/BurstCache/JIT` rồi `refresh_unity`.
3. MCP `run_tests` PlayMode lọc `WaterFlowPlayTests`, `TerrainEditPlayTests`, `DayCycleWaterPlayTests`, `FlyCameraWaterPlayTests`, `OceanBackdropTests`.
4. MCP `run_tests` EditMode toàn bộ; PlayMode toàn bộ. Ghi dòng tổng. MCP rớt khi chạy: `sleep 60` rồi `get_test_job`, lặp; không kết thúc lượt khi test còn chạy.
5. `diff` ở "Tiêu chí xong".
6. Dọn: thoát Play; `git checkout -- client/ProjectSettings/EditorSettings.asset`; xoá `client/Assets/InitTestScene*.unity(.meta)` nếu có. `git status --short` chỉ còn file trong Phạm vi.

Ghi giờ bắt đầu/kết thúc (lệnh `date`). Chụp **1** ảnh Game view (SimView, Play) nhìn thác demo từ bên cạnh, lưu ngoài repo, ghi đường dẫn vào Kết quả — chỉ để Planner xem, không thay user xem.

### User tự test
Không ở brief này — gộp sau brief 0081. Ghi sẵn để Planner chuyển sang: SimView, Play — (1) thác demo: cột nước liền, cạnh đứng bo tròn, mép tràn bo lồi, không khe hở/nhấp nháy; (2) đào rãnh ra vách: mép tràn bo, nhìn từ trên không thấy lỗ chỗ nối với ô phía trước; cùng 5 mục của brief 0079.

## Dừng và chuyển lại Opus khi
- Brief mơ hồ hoặc mâu thuẫn với code thật (số dòng lệch nhiều, chữ ký khác).
- Một giá trị trong "Phần cần test" ra khác mô tả mà code đúng QĐ 1–8 (Planner có thể tính sai) — ghi giá trị thật, không tự đổi kỳ vọng.
- Test cũ ở "cũ — giữ nguyên" fail; mã băm `GoldenHashTests` đổi.
- Cần API Unity ngoài `NativeHashMap` đã ghi; cần quyết định ngoài "Quyết định đã chốt"; phải sửa file ngoài "Phạm vi".
- Cùng một lỗi sửa 2 lần vẫn chưa qua.

---
<!-- Từ đây trở xuống: EXECUTOR ghi. -->

## Kết quả

## Kết quả test

## Vướng mắc

---
<!-- PLANNER ghi khi review. -->

## Review
