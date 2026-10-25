# Brief 0077 — World 5f: vẽ mặt bên / mặt đáy ô nước (thác nước nhìn thấy được)

> Trạng thái: chờ user xem
> Review: đầy đủ + user xem
> Model thực thi: Sonnet · medium
> Cập nhật: 2026-10-25 22:15

Task contract: [m1-world-nuoc-chay](../tasks/m1-world-nuoc-chay.md) (phần 5f). Quyết định: [0041](../../design/decisions/0041-nuoc-chay-trong-ranh-hep-va-thac-nuoc.md) (có thác nước). Làm tiếp sau brief [0076](0076-world-5f-nuoc-ranh-hep-thac-nuoc.md) (code đã duyệt; thác đúng trong dữ liệu nhưng không hiển thị).

## Mục tiêu
- Cột nước rơi (thác) và mép nước đang chảy nhìn thấy được: mesher vẽ thêm mặt bên của ô nước khi ô ngang là `Empty`, và mặt đáy khi ô dưới là `Empty`.
- Không đổi hình của nước bị bao kín (ao, hồ, biển nguyên vẹn): ở đó không có ô `Empty` cạnh bên → không thêm mặt.

## Bối cảnh cần đọc
- `client/Assets/Game/World/BlockMesher.cs`:
  - `WaterSurfaceDrop = 0.1f` (:43) — mặt nước có không khí phía trên thấp hơn đỉnh ô chừng này.
  - `MeshJob.Execute` (:384-431); nhánh ô nước (:414-420):
    ```csharp
    if (m == (int)BlockMaterial.Water)
    {
        int above = MaterialAt(g + new int3(0, 1, 0));
        if (above == (int)BlockMaterial.Empty)
            AddWaterSurface(new int3(x, y, z));
        continue;
    }
    ```
    `g` = toạ độ world của ô; `new int3(x, y, z)` = toạ độ trong chunk (đỉnh mesh dùng toạ độ trong chunk).
  - `AddWaterSurface(int3 c)` (:902-914): 4 đỉnh qua `AddVertex(p, n, 0)`, 2 tam giác ghi vào `Out.WaterIndices` (submesh nước, material `Water.mat`).
  - `int AddVertex(float3 p, float3 n, uint color)` (:916-923).
  - `AddOriented` / `AddTriangle` (:667, :925-933) ghi vào `Out.Indices` (địa hình) — **không dùng** cho nước.
  - `int MaterialAt(int3 p)` (:935-944): y < 0 → Bedrock; y ≥ SizeY → Empty; x/z ngoài map **lấy cột biên gần nhất** → ô nước ở mép map có "ô ngoài map" chính là nó (nước) → không sinh mặt ở mép map.
- `client/Assets/Game/World/ChunkMesh.cs:14,20` (`WaterIndices`, `WaterTriangleCount`).
- `client/Assets/Game/Tests/World/BlockMesherTests.cs`: test nước `WaterSurfaceOnlyWhereAirIsAbove` (:803-829) — hai cột nước **không có tường bên** → với luật mới sẽ có mặt bên, phải sửa (bước 2).
- Thác demo: `client/Assets/Game/Core/Sim/SimDemo.cs` `PlaceWaterfall`; `SimView.WaterfallSource`; PlayMode `client/Assets/Game/Tests/Client/WaterFlowPlayTests.cs` `WaterfallPoursIntoPond` (cột nước (cx + 13, y, cz − 11), y = G + 1..G + 4).

## Quyết định đã chốt (không được đổi)
1. **Điều kiện vẽ** (trong nhánh ô nước, sau phần mặt trên hiện có):
   - Mặt bên hướng (axis ∈ {0, 2}, sign ∈ {−1, +1}): vẽ khi `MaterialAt(g ± trục) == (int)BlockMaterial.Empty`. Kề nước hoặc khối đặc → không vẽ.
   - Mặt đáy: vẽ khi `MaterialAt(g + new int3(0, -1, 0)) == (int)BlockMaterial.Empty`.
   - Mặt trên: giữ nguyên như hiện nay.
2. **Hình:**
   - Mặt bên: hình chữ nhật đứng trên mặt phẳng `c[axis] + (sign > 0 ? 1 : 0)`, phủ trục ngang còn lại từ `c` tới `c + 1`, theo y từ `c.y` tới `top`, với `top = c.y + 1 − WaterSurfaceDrop` nếu ô trên là `Empty` (trùng mép mặt nước), ngược lại `top = c.y + 1` (M1 vẽ đầy ô — task contract "Ngoài phạm vi").
   - Mặt đáy: hình vuông ngang ở `y = c.y`, x, z từ `c` tới `c + 1`.
   - Pháp tuyến đỉnh = hướng ra ngoài ô (mặt bên: ±x hoặc ±z; đáy: −y). Màu đỉnh 0 (như `AddWaterSurface`).
   - Mỗi mặt 4 đỉnh riêng (không dùng chung đỉnh với mặt khác — giống mặt trên hiện có).
3. **Hàm mới** trong `MeshJob`, cạnh `AddWaterSurface`:
   ```csharp
   /// <summary>Tứ giác nước (0041): 4 đỉnh theo vòng p0→p1→p2→p3, 2 tam giác vào submesh nước, mặt trước quay theo n.</summary>
   void AddWaterQuad(float3 p0, float3 p1, float3 p2, float3 p3, float3 n)
   ```
   - Thêm 4 đỉnh bằng `AddVertex(p, n, 0)`; hai tam giác (0, 1, 2), (0, 2, 3); với mỗi tam giác, nếu `math.dot(math.cross(pb − pa, pc − pa), n) < 0` thì ghi theo thứ tự (a, c, b). Ghi vào `Out.WaterIndices`.
   - Mặt bên và mặt đáy dùng `AddWaterQuad`. **Không** sửa `AddWaterSurface` (giữ nguyên thứ tự đỉnh → mesh của nước kín không đổi byte).
4. Không đổi collider, mặt địa hình, `ChunkMesh`, `ChunkMeshWriter`, material, shader.
5. Mã băm world không đổi (mesher không đụng dữ liệu world).

## Phạm vi
- Được sửa: `client/Assets/Game/World/BlockMesher.cs` (nhánh ô nước + hàm `AddWaterQuad` + comment summary lớp nếu cần nhắc mặt nước), `client/Assets/Game/Tests/World/BlockMesherTests.cs`, `client/Assets/Game/Tests/Client/WaterFlowPlayTests.cs` (chỉ thêm 1 test bước 4), brief này (Kết quả trở xuống).
- Không được sửa: mọi file khác (Core, SimDemo, SimView, scene, material, `design/`).

## Các bước
1. **Mesher:** QĐ 1–3 trong `BlockMesher.cs`. Xong khi: biên dịch được, console 0 Error.
2. **Sửa test cũ** `WaterSurfaceOnlyWhereAirIsAbove`: thêm tường đá để không ô nước nào có ô `Empty` cạnh bên hay bên dưới; kỳ vọng giữ nguyên (đúng 1 tứ giác ở y = 9 − drop trong x, z ∈ [4, 5], hướng lên).
   - Cột (4, 7..8, 4): đá ở 4 ô ngang (3, y, 4), (5, y, 4), (4, y, 3), (4, y, 5) với y = 7, 8.
   - Ô (8, 7, 8): đá ở (7, 7, 8), (9, 7, 8), (8, 7, 7), (8, 7, 9) và (8, 6, 8) (bên dưới đang rỗng).
3. **Test mới** trong `BlockMesherTests.cs` (mục "Mặt nước theo ô"; mọi world `new WorldData(new WorldSize(16, 16, 16))`, `BlockMesher.Build(w, 0, 0, 0)`):
   - `FallingWaterColumnHasSideFaces`: đá (4, 3, 4); nước `Cell.Make(BlockMaterial.Water, Cell.MaxWaterLevel)` ở (4, y, 4), y = 4..7; còn lại rỗng. Kỳ vọng:
     - `WaterTriangleCount == 4 * 4 * 2 + 2` (= 34; 4 ô × 4 mặt bên + 1 mặt trên; không mặt đáy vì dưới là đá);
     - mọi đỉnh nước: x, z ∈ [4, 5], y ∈ [4, 8 − WaterSurfaceDrop] (sai số 1e-5);
     - có đỉnh nước ở y = 4 và có đỉnh ở y = 8 − WaterSurfaceDrop;
     - mỗi tam giác nước: `dot(cross(b − a, c − a), normal của đỉnh a) > 0` (mặt trước quay ra ngoài) và normal đỉnh a là một trong (0, 1, 0), (±1, 0, 0), (0, 0, ±1).
   - `EnclosedWaterHasOnlyTopFaces`: đá nền y = 4 với x ∈ 2..6, z ∈ 2..5; đá y = 5 ở viền (x ∈ {2, 6} hoặc z ∈ {2, 5}, x ∈ 2..6, z ∈ 2..5); nước ở (x, 5, z), x ∈ 3..5, z ∈ 3..4 (6 ô). Kỳ vọng `WaterTriangleCount == 6 * 2` (không mặt giữa hai ô nước, không mặt cạnh đá).
   - `FloatingWaterCellHasBottomFace`: chỉ một ô nước ở (4, 8, 4). Kỳ vọng `WaterTriangleCount == 6 * 2`; có đúng 2 tam giác có normal đỉnh (0, −1, 0), mọi đỉnh của chúng ở y = 8.
   - `WaterAtMapEdgeHasNoOuterWall`: đá (0, 4, 0), (1, 5, 0), (0, 5, 1); nước (0, 5, 0). Kỳ vọng `WaterTriangleCount == 2` (phía x < 0 và z < 0 lấy cột biên = chính ô nước → không mặt).
   - Test tất định hiện có (:64-80, so `WaterIndices`) phải qua không sửa.
4. **PlayMode** `WaterFlowPlayTests.cs`, test mới `WaterfallColumnIsMeshed`: chờ yên như `WaterfallPoursIntoPond` (tối đa 5 giây tới `!view.Editor.HasPendingWater`), rồi `yield return null` 2 lần (cho chunk dựng lại); `view.TotalTriangles(out int water)` (`TerrainChunkView.cs:309`) → `water > 0`; và dựng trực tiếp chunk chứa ô (cx + 13, G + 2, cz − 11): `var m = BlockMesher.Build(view.World, wx >> 4, wy >> 4, wz >> 4)` (chunk 16 ô — `WorldSize.ChunkSize`), kiểm có ít nhất 1 tam giác nước có normal đỉnh (1, 0, 0) hoặc (−1, 0, 0) mà mọi đỉnh nằm trong x ∈ [lx, lx + 1], z ∈ [lz, lz + 1] với `lx = wx & 15`, `lz = wz & 15` (toạ độ trong chunk). `cx, cz, G` lấy như `WaterfallPoursIntoPond`.
5. Chạy test, dọn, chụp ảnh thác.

## Tiêu chí xong
- Test mới + `WaterSurfaceOnlyWhereAirIsAbove` đã sửa qua; EditMode + PlayMode toàn bộ qua (trước brief: 640 / 275; ghi số mới — mong 644 / 276).
- `GoldenHashTests`, `SimDemoTests`, `SimDemoMapsTests` qua không sửa.
- Console 0 Error. `git diff --stat` chỉ có file trong Phạm vi.
- Ảnh Play thấy rõ cột thác.

## Test sau khi xong

### Phần cần test
- Mặt bên ô nước khi kề `Empty` (QĐ 1–3, bước 1): `FallingWaterColumnHasSideFaces`, PlayMode `WaterfallColumnIsMeshed`.
- Không thêm mặt khi kề nước/đặc, mép map (QĐ 1, biển/ao không đổi): `EnclosedWaterHasOnlyTopFaces`, `WaterAtMapEdgeHasNoOuterWall`, `WaterSurfaceOnlyWhereAirIsAbove`.
- Mặt đáy (QĐ 1–2): `FloatingWaterCellHasBottomFace`.
- Chiều mặt trước / pháp tuyến (QĐ 3): `FallingWaterColumnHasSideFaces`.
- Phần cũ có thể ảnh hưởng: toàn bộ `BlockMesherTests` (tất định, mesh kín địa hình, 16 bit), PlayMode `WaterFlowPlayTests`, `DayCycleWaterPlayTests`, `FlyCameraWaterPlayTests`, `TerrainEditPlayTests`, `SimViewMapsPlayTests`; mã băm.
- EditMode + PlayMode toàn bộ.

### Lệnh executor chạy
1. MCP `refresh_unity` → `read_console` (Error) = 0.
2. MCP `run_tests` EditMode lọc `BlockMesherTests`, `GoldenHashTests`, `SimDemoTests`, `SimDemoMapsTests`.
3. MCP `run_tests` PlayMode lọc `WaterFlowPlayTests`, `DayCycleWaterPlayTests`, `FlyCameraWaterPlayTests`, `TerrainEditPlayTests`, `SimViewMapsPlayTests`.
4. MCP `run_tests` EditMode toàn bộ; PlayMode toàn bộ. Ghi dòng tổng. MCP rớt khi PlayMode chạy: `sleep 60` rồi `get_test_job`, lặp lại; không kết thúc lượt khi test còn chạy.
5. Dọn: `git checkout -- client/ProjectSettings/EditorSettings.asset`; xoá `client/Assets/InitTestScene*.unity(.meta)` nếu có. `git status --short` chỉ còn file của brief.
6. Play scene SimView (Mac), chờ 3 giây, chụp **1** ảnh nhìn ngang vào thác (camera đặt cách `sv.WaterfallSource` khoảng 8 ô về phía −z, cao ngang G + 3, nhìn vào cột (cx + 13, G + 3, cz − 11) qua MCP). Ghi đường dẫn ảnh (scratchpad) vào Kết quả. Thoát Play.

Ghi giờ bắt đầu/kết thúc (lệnh `date`) vào Kết quả.

### User tự test (gộp với mục "User tự test" của brief 0076)
Scene **SimView** trên Mac, Play:
1. Cạnh ao (phía +x) có khối đất cao 5 ô; **thấy cột nước rơi** từ mép xuống ao; không tràn ra cỏ quanh ao.
2. Ao, biển nhìn như trước (không có vách nước lạ ở bờ).
3. Các mục 2–6 của brief 0076: rãnh 1 ô dài chảy hết; ra chỗ rộng lan tối đa 7 ô; chặn giữa rãnh → đoạn sau rút; phá mép / lấp nguồn thác → rút; đào cát cạnh ao → hố đầy; đào bờ biển → biển loang như trước.
4. Mép nước đang lan ở chỗ rộng giờ có vách đứng cao gần 1 ô (M1 vẽ đầy ô) — chỉ báo nếu thấy khó chịu.

## Dừng và chuyển lại Opus khi
- Brief mơ hồ hoặc mâu thuẫn với code thật (số dòng lệch nhiều, chữ ký khác).
- Test cũ ngoài `WaterSurfaceOnlyWhereAirIsAbove` fail mà phải đổi kỳ vọng; mã băm đổi.
- Cần API Unity không có trong brief.
- Cần quyết định ngoài "Quyết định đã chốt"; phải sửa file ngoài "Phạm vi".
- Cùng một lỗi sửa 2 lần vẫn chưa qua.

---

## Kết quả

- Base commit: a8831a5 · Chạy bằng: Sonnet 5.5 (claude-sonnet-5-5) · medium
- Giờ: bắt đầu 21:53, kết thúc 22:14 (2026-10-25), ~21 phút (nhiều phần là chờ test 6 + 3 phút).
- Commit: 6fe5bfa — `BlockMesher.cs` (AddWaterSides + AddWaterQuad + AddWaterTri; AddWaterSurface giữ nguyên), `BlockMesherTests.cs` (sửa `WaterSurfaceOnlyWhereAirIsAbove`, thêm 4 test), `WaterFlowPlayTests.cs` (+`WaterfallColumnIsMeshed`).
- Ảnh thác: scratchpad `waterfall0077b.png` (cột thác rơi vào ao, camera tắt FollowCamera/DebugFlyCamera thủ công; ảnh `waterfall0077.png` đầu là camera chưa dời, bỏ).
- Lưu ý: lần chạy EditMode đầu 2 test mới fail (đếm 2 tam giác) do chạy khi assembly/Burst chưa nạp bản mới (sau lỗi biên dịch lần đầu ở test); sau khi biên dịch lại sạch thì qua, không đổi code mesher.

## Kết quả kiểm chứng

- `FallingWaterColumnHasSideFaces`, PlayMode `WaterfallColumnIsMeshed`: đạt.
- `EnclosedWaterHasOnlyTopFaces`, `WaterAtMapEdgeHasNoOuterWall`, `WaterSurfaceOnlyWhereAirIsAbove` (đã sửa): đạt.
- `FloatingWaterCellHasBottomFace`: đạt.
- EditMode toàn bộ: 644/644 đạt (gồm GoldenHashTests, SimDemoTests, SimDemoMapsTests, BlockMesherTests tất định). PlayMode toàn bộ: 276/276 đạt.
- Console 0 Error; dọn EditorSettings.asset, không có InitTestScene; `git status` sạch sau commit.
- Ảnh Play: thấy rõ cột thác — đạt (phần user tự xem: chờ user).

## Vướng mắc

Không.

---

## Review

**2026-10-25 — Planner (Opus), review đầy đủ: code đạt; chờ user xem.**
- Diff `a8831a5..6fe5bfa`: `AddWaterSides` vẽ mặt bên khi ô ngang `Empty` (qua `MaterialAt` → mép map lấy cột biên, không vách), top hạ `WaterSurfaceDrop` khi trên là `Empty`, mặt đáy khi dưới `Empty`; `AddWaterQuad`/`AddWaterTri` ghi `WaterIndices`, đảo thứ tự theo `n`; `AddWaterSurface` không đổi — đúng QĐ 1–5. Chỉ 3 file trong Phạm vi.
- Test: EditMode 644/644, PlayMode 276/276, mã băm không đổi [executor chạy qua MCP; Planner đọc kết quả].
- Ảnh `waterfall0077b.png` (scratchpad phiên executor): thấy cột thác từ mép rơi vào ao. Thấy vạch ngang nhạt ở chỗ nối giữa các ô của cột (mỗi ô một tứ giác riêng, nước trong suốt) — chỉ ghi lại, user xem có khó chịu không.
- Giờ: executor 21:53–22:14 (~21 phút); đã ghi roadmap.

