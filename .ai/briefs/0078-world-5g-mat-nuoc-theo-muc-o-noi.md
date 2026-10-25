# Brief 0078 — World 5g: mặt nước thấp dần theo mức, lan 5 ô, ô nối; tái hiện lỗi đứng trên mặt nước

> Trạng thái: chờ làm
> Review: đầy đủ + user xem
> Model thực thi: Sonnet · medium
> Cập nhật: 2026-10-25

Task contract: [m1-world-nuoc-chay](../tasks/m1-world-nuoc-chay.md) (mục "Cập nhật 2026-10-25 — 0042"). Quyết định: [0042](../../design/decisions/0042-mat-nuoc-theo-muc-lan-5-o-o-noi.md). Làm tiếp trên code của brief [0076](0076-world-5f-nuoc-ranh-hep-thac-nuoc.md) và [0077](0077-world-5f-mat-ben-o-nuoc-thac.md) (commit `6fe5bfa`).

## Mục tiêu
- Mặt nước của ô đang chảy thấp dần theo mức. Mặt nước liền giữa các ô, mép lan mỏng dần, không còn cảnh hộp xanh chồng nhau.
- Lan trên mặt bằng tối đa 5 ô (JSON). Hai vùng nước cách nhau 1 ô trống thì ô đó thành nước (ô nối).
- Đổi mức nước thì chunk được dựng lại.
- Tái hiện lỗi "nhân vật đứng trên mặt nước ở chân thác" và ghi số liệu. Brief này **không sửa** lỗi đó; Planner quyết cách sửa sau.

## Bối cảnh cần đọc
- `client/Assets/Game/Core/Cell.cs`: `GetWaterLevel` (:57), `IsFlowing` (:75), `IsFalling` (:77), `IsDraining` (:79), `IsWaterSource` (:85-86), `MakeFlowing(ushort cell, int level, bool falling, bool draining = false)` (:89). Đây là các hàm tĩnh thuần (chỉ thao tác bit), gọi được trong job Burst.
- `client/Assets/Game/World/BlockMesher.cs`:
  - `WaterSurfaceDrop = 0.1f` (:43).
  - `Build(WorldData world, int chunkX, int chunkY, int chunkZ, bool grassLip = true)` (:254); `BuildAll(WorldData world, bool grassLip = true)` (:258); `BuildChunks(WorldData world, IReadOnlyList<int> chunkIndices, bool grassLip = true)` (:267). Khởi tạo `MeshJob` ở (:296-306), có `GrassLip = grassLip` (:302).
  - Các trường của `MeshJob`: `public bool GrassLip;` (:382), `public int SizeX, SizeY, SizeZ;` (:383).
  - Nhánh ô nước trong `Execute` (:414-421):
    ```csharp
    int above = MaterialAt(g + new int3(0, 1, 0));
    if (above == (int)BlockMaterial.Empty)
        AddWaterSurface(new int3(x, y, z));
    AddWaterSides(new int3(x, y, z), g, above == (int)BlockMaterial.Empty);
    ```
  - `AddWaterSurface(int3 c)` (:904-915), `AddWaterSides(int3 c, int3 g, bool airAbove)` (:918-940), `AddWaterQuad` (:942), `AddWaterTri` (:952), `MaterialAt(int3 p)` (:980-989): y < 0 → Bedrock; y ≥ SizeY → Empty; x, z ngoài map thì kẹp về cột biên.
- `client/Assets/Game/Core/WorldEditor.cs`:
  - `int flowMaxDistance = 7` (:56), `FlowMaxDistance` (:59-69); `public event Action<int, int, int> CellChanged;` (:112).
  - `IsSolidAt` (:248-256); `Sides` (:~458); `IsWaterCell`, `IsStandingWater` (:~460-464); `Emit` (:466-…).
  - `StepFlow`: nhánh lan ngang (`int m = 0; bool allDraining = true; …` rồi `if (m >= 1) … else if (Cell.GetMaterial(old) == BlockMaterial.Water) target = Cell.ClearWater(old); else continue;`); vòng hậu xử lý cuối (`foreach (var (x, y, z, oldMat, cell) in changed)`, nhánh `if (oldMat != newMat)` phát `CellChanged`).
- `client/Assets/Game/Client/TerrainChunkView.cs`: gán cấu hình nước + `Editor.CellChanged += OnCellChanged;` (:228-233); `void OnCellChanged(int x, int y, int z) => Editor.AffectedChunks(x, y, z, dirty);` (:324); `Rebuild` gọi `BlockMesher.BuildChunks(World, chunkIndices)` (:389).
- `client/Assets/Game/Core/WaterConfig.cs` (`flowMaxDistance = 7`); `design/data/water_m1.json` và bản sao `client/Assets/Game/Client/Resources/Config/water_m1.json` (hai file phải giống nhau từng byte, có `ConfigTests.WaterClientCopyMatchesDesignData`).
- Test:
  - `client/Assets/Game/Tests/Core/WaterConfigTests.cs:18` (`Assert.AreEqual(7, cfg.flowMaxDistance)`), `ConfigFileMatchesDefaults` (:25-29: mặc định của `WaterConfig` phải khớp file).
  - `client/Assets/Game/Tests/Core/WaterFlowTests.cs`: `Flat`, `Editor`, `Settle`, `Dig`, `Place`, `OpenFloor` (:67-73), `SpreadStopsAfterN` (:109-…).
  - `client/Assets/Game/Tests/Client/WaterFlowPlayTests.cs:57` (`Assert.AreEqual(7, view.Editor.FlowMaxDistance)`), `WaitSettled` (:61-70).
  - `client/Assets/Game/Tests/World/BlockMesherTests.cs`: các test mặt nước ở cuối file (`WaterSurfaceOnlyWhereAirIsAbove`, các test của 0077). Mọi ô nước trong các test này là **nguồn** (`Cell.Make(Water, Max)`), nên độ cao không đổi.
- Nhân vật: `client/Assets/Game/Client/PlayerCharacter.cs` — `FeetLocal` (:64), `Controller` (:51, `CharacterController`), `Place(Vector3 feetWorld, float yawDeg)` (:251), `InWater` (:37). Thác demo: `SimView.Plain`, `SimView.WaterfallSource`; toạ độ cột thác `(cx + 13, G + 1..G + 4, cz − 11)` với `cx = plain.X0 + plain.Size / 2`, `cz = plain.Z0 + plain.Size / 2`, `G = plain.Ground`. Ao ở `y = G`, x ∈ [cx + 10, cx + 13], z ∈ [cz − 12, cz − 9], viền cát x ∈ [cx + 9, cx + 14], z ∈ [cz − 13, cz − 8].

## Quyết định đã chốt (không được đổi)
1. **N = 5 trong JSON:** `water_m1.json` (cả 2 file) có `"flowMaxDistance": 5`, các trường khác giữ nguyên. `WaterConfig.flowMaxDistance = 5`. **Không** đổi mặc định `flowMaxDistance = 7` trong `WorldEditor`, vì các test EditMode tự dựng editor và đang dùng 7.
2. **Độ cao mặt nước** (từ đáy ô, đơn vị ô). Hằng `F = WaterFullLevel` (xem QĐ 4):
   - ô có nước ngay phía trên → `1`;
   - ô nguồn (`!Cell.IsFlowing`) hoặc ô đang rơi (`Cell.IsFalling`) → `1 − WaterSurfaceDrop`;
   - ô đang chảy còn lại, mức `L` → `(1 − WaterSurfaceDrop) * math.min(L, F) / F`.
3. **Góc mặt nước:** góc lưới `(gx, gz)` ở tầng `y` xét 4 ô `(gx − 1 + i, y, gz − 1 + j)`, i, j ∈ {0, 1}. Đọc ô qua cùng cách kẹp biên với `MaterialAt`. Bỏ qua ô không phải nước. Có ô nào độ cao `1` → góc `1`; ngược lại góc = trung bình độ cao các ô nước (luôn ≥ 1 ô, vì ô đang vẽ là nước).
4. **Tham số mesher:** `Build`, `BuildAll`, `BuildChunks` thêm tham số cuối `int waterFullLevel = Cell.MaxWaterLevel`, truyền vào trường mới `public int WaterFullLevel;` của `MeshJob`. `waterFullLevel < 1` → `ArgumentOutOfRangeException` (kiểm trong `BuildChunks`). `TerrainChunkView.Rebuild` truyền `Editor.FlowMaxDistance`.
5. **Hàm mới trong `MeshJob`:**
   ```csharp
   ushort CellAt(int3 p);                 // như MaterialAt nhưng trả nguyên ô: y < 0 → (ushort)BlockMaterial.Bedrock, y ≥ SizeY → 0
   float WaterHeight(int3 p);             // QĐ 2; ô không phải nước → −1
   float CornerHeight(int3 g, int dx, int dz); // QĐ 3; góc (g.x + dx, g.z + dz), dx, dz ∈ {0, 1}, tầng g.y
   ```
6. **`AddWaterSurface(int3 c, int3 g)`:** giữ thứ tự 4 đỉnh và thứ tự chỉ số hiện có. Đỉnh `i`: `(c.x + dx, c.y + CornerHeight(g, dx, dz), c.z + dz)` với `(dx, dz)` lần lượt (0, 0), (1, 0), (1, 1), (0, 1). Pháp tuyến giữ `(0, 1, 0)`. Điều kiện vẽ giữ nguyên (ô trên là `Empty`).
7. **`AddWaterSides(int3 c, int3 g)`** (bỏ tham số `airAbove`): điều kiện vẽ giữ như 0077 (ô ngang `Empty`; mặt đáy khi ô dưới `Empty`). Hai đỉnh dưới ở `c.y`. Hai đỉnh trên ở `c.y + CornerHeight` của đúng hai góc nằm trên cạnh đó. Ví dụ mặt −x dùng góc (0, 0) và (0, 1); mặt +z dùng góc (0, 1) và (1, 1). Vẫn dùng `AddWaterQuad`.
8. **Sự kiện dựng lại:** `WorldEditor` thêm
   ```csharp
   /// <summary>Ô nước đổi mức/cờ mà vật liệu không đổi (0042) — client dựng lại mesh; mô phỏng không nghe.</summary>
   public event Action<int, int, int> WaterSurfaceChanged;
   ```
   - Phát trong vòng hậu xử lý cuối của `StepFlow`, khi `oldMat == newMat && newMat == BlockMaterial.Water`.
   - `CellChanged` giữ nguyên như hiện có.
   - `TerrainChunkView` thêm `Editor.WaterSurfaceChanged += OnCellChanged;` ngay sau dòng :233.
9. **Ô nối** — `WorldEditor`:
   ```csharp
   /// <summary>Nước đứng, không đang rút, tại (x, y, z) — neo của ô nối (0042).</summary>
   bool IsBridgeAnchor(int x, int y, int z); // IsWaterCell && IsStandingWater(c) && !Cell.IsDraining(c)
   /// <summary>Ô nối (0042): ô đứng được, có neo ở hai phía đối diện (x − 1 và x + 1, hoặc z − 1 và z + 1).</summary>
   bool IsBridge(int x, int y, int z);
   ```
   - "Đứng được": `IsSolidAt(World, x, y − 1, z) || (y − 1 >= 0 && IsStandingWater(World.Get(x, y − 1, z)))`.
   - Trong nhánh lan ngang của `StepFlow`, giữa `if (m >= 1)` và `else if (… Water) ClearWater`, thêm `else if (IsBridge(x, y, z)) target = Cell.SetModified(Cell.MakeFlowing(old, 1, false), true);`.
   - Không thêm hàng chờ mới: ô khe đã vào hàng chờ qua `AddFlowDependents` khi ô kề đổi.
10. **Mã băm** (`GoldenHashTests`, `SimDemoTests`, `SimDemoMapsTests`) qua không sửa số. Đổi số → dừng, cần Opus.

## Phạm vi
- Được sửa:
  - `client/Assets/Game/World/BlockMesher.cs`, `client/Assets/Game/Core/WorldEditor.cs`, `client/Assets/Game/Core/WaterConfig.cs`;
  - `client/Assets/Game/Client/TerrainChunkView.cs` (chỉ dòng đăng ký sự kiện + tham số `BuildChunks` trong `Rebuild`);
  - `design/data/water_m1.json` + bản sao client;
  - test: `Tests/World/BlockMesherTests.cs`, `Tests/Core/WaterFlowTests.cs`, `Tests/Core/WaterConfigTests.cs`, `Tests/Client/WaterFlowPlayTests.cs`;
  - brief này (Kết quả trở xuống).
- Không được sửa: mọi file khác (`PlayerCharacter`, `SimDemo`, `SimView`, `Cell.cs`, scene, `design/*.md`).

## Các bước
1. **Mesher:** QĐ 2–7. Xong khi: biên dịch được, console 0 Error. Burst báo lỗi khi gọi `Cell.*` trong job → dừng, cần Opus.
2. **Editor + client:** QĐ 1, 8, 9; `TerrainChunkView` (QĐ 4, 8); hai JSON; `WaterConfig`. Sửa test theo N mới: `WaterConfigTests.cs:18` 7 → 5; `WaterFlowPlayTests.cs:57` 7 → 5.
3. **Test mesher mới** (`BlockMesherTests.cs`, world `new WorldData(new WorldSize(16, 16, 16))`, đá nền y = 4 với mọi x, z ∈ 0..15):
   - `FlowingWaterSurfaceFollowsLevel`:
     - ô A (4, 5, 4) = `Cell.MakeFlowing(0, 5, false)`, ô B (5, 5, 4) = `Cell.MakeFlowing(0, 3, false)`; `Build(w, 0, 0, 0, true, 5)`.
     - Có đỉnh nước pháp tuyến (0, 1, 0) tại (4, 5,9, 4), (5, 5,72, 4), (5, 5,72, 5), (6, 5,54, 4), sai số 1e-4.
     - Không có tam giác nước nào có pháp tuyến đỉnh (±1, 0, 0) mà cả 3 đỉnh nằm trên mặt phẳng x = 5 (không có vách giữa A và B).
     - Mặt bên −z của A (pháp tuyến (0, 0, −1), z = 4, x ∈ [4, 5]) có đỉnh trên ở y = 5,9 (x = 4) và y = 5,72 (x = 5).
   - `FallingAndSourceWaterStayFull`: ô (4, 5, 4) = `MakeFlowing(0, 2, true)` (đang rơi), ô (6, 5, 4) = nguồn → mọi đỉnh mặt trên ở y = 5,9; `waterFullLevel` 5.
   - `BuildRejectsBadWaterFullLevel`: `Build(w, 0, 0, 0, true, 0)` ném `ArgumentOutOfRangeException`.
   - Các test nước của 0077 và `WaterSurfaceOnlyWhereAirIsAbove` qua **không sửa**.
4. **Test editor mới** (`WaterFlowTests.cs`):
   - `BridgeFillsOneCellGap`:
     - `Flat(5)`; nguồn A (0, 6, 8), đá (1, 6, 8); nguồn B (6, 6, 8), đá (5, 6, 8); editor `FlowMaxDistance = 2`.
     - `Dig(1, 6, 8)`, `Dig(5, 6, 8)`, `Settle` → (2, 6, 8) và (4, 6, 8) là nước đang chảy mức 1; (3, 6, 8) là nước đang chảy mức 1 (ô nối); (3, 6, 7) và (3, 6, 9) rỗng.
   - `BridgeDrainsWhenOneSideGone`: tiếp theo trên, `Place(6, 6, 8)` (lên nguồn B), `Settle` → (3, 6, 8), (4, 6, 8), (5, 6, 8) rỗng; (1, 6, 8), (2, 6, 8) vẫn là nước.
   - `LevelChangeRaisesWaterSurfaceChanged`:
     - `OpenFloor`, `Dig(1, 6, 8)`, `Settle`; đăng ký đếm `WaterSurfaceChanged` (trong handler assert ô là `Water`) và `CellChanged`.
     - `Place(0, 6, 8)`, gọi `StepWater()` 1 lần → số sự kiện `WaterSurfaceChanged` ≥ 1.
     - Tiếp `Settle`. Trong handler `CellChanged` (đăng ký trước `Place`), assert ô lúc phát **không** phải nước (chỉ có nước → rỗng và ô đặt khối).
   - Regression: mọi test cũ trong `WaterFlowTests` qua không sửa (vẫn N = 7 mặc định của editor).
5. **PlayMode:** chỉ sửa assert N (bước 2). Các test hiện có (`WaterfallPoursIntoPond`, `WaterfallColumnIsMeshed`, `WaterSettlesAfterStart`, `DiggingSandNextToPondFillsHole`) qua không sửa gì khác.
6. **Tái hiện lỗi đứng trên mặt nước** (MCP, Play scene SimView trên Mac, không sửa code):
   - Lấy `PlayerCharacter` (`Object.FindAnyObjectByType<PlayerCharacter>()`), `SimView`, `TerrainChunkView`; tính `cx, cz, G`.
   - Ba vị trí chân (toạ độ local của terrain, đổi sang world bằng `terrain.transform.TransformPoint` trước khi gọi `Place`):
     - P1 = (cx + 13,5, G, cz − 10,5): đáy ao ngay dưới cột thác;
     - P2 = (cx + 12,5, G, cz − 10,5): ao cạnh cột thác;
     - P3 = (cx + 11,5, G, cz − 10,5): ao xa thác.
   - Với mỗi vị trí: `Place(p, 0)`, chờ 90 khung (Editor mất focus thì dùng `EditorApplication.Step` — xem ghi chú MCP). Ghi `FeetLocal.y` ở khung 1, 30, 90; `Controller.isGrounded`; `InWater`. Ghi kết quả `Physics.Raycast(world feet + up × 0,5, down, 3)`: tên collider, `hit.point.y` (local).
   - Đạt nghĩa là `FeetLocal.y` ≈ G (lệch ≤ 0,1). Lệch > 0,1 ở vị trí nào → ghi đủ số liệu vào "Vướng mắc".
7. Chạy cả bộ, dọn, chụp ảnh.

## Tiêu chí xong
- Test mới qua; EditMode + PlayMode toàn bộ qua (trước brief: 644 / 276; ghi số mới).
- Mã băm không đổi số; console 0 Error; `git diff --stat` chỉ có file trong Phạm vi.
- `grep -n "UnityEngine" client/Assets/Game/Core/WorldEditor.cs client/Assets/Game/Core/WaterConfig.cs` → rỗng.
- Bước 6 có số liệu đủ 3 vị trí.

## Test sau khi xong

### Phần cần test
- Độ cao mặt nước theo mức, góc trung bình, không vách giữa hai ô nước (QĐ 2, 3, 5, 6): `FlowingWaterSurfaceFollowsLevel`, `FallingAndSourceWaterStayFull`.
- Mặt bên theo góc (QĐ 7): `FlowingWaterSurfaceFollowsLevel` (mặt −z), các test 0077 (`FallingWaterColumnHasSideFaces`, `EnclosedWaterHasOnlyTopFaces`, `FloatingWaterCellHasBottomFace`, `WaterAtMapEdgeHasNoOuterWall`).
- Tham số mesher (QĐ 4): `BuildRejectsBadWaterFullLevel`; toàn bộ `BlockMesherTests` (tất định, mesh kín, 16 bit).
- N = 5 (QĐ 1): `WaterConfigTests`, `ConfigTests.WaterClientCopyMatchesDesignData`, PlayMode `DiggingSandNextToPondFillsHole`.
- Sự kiện dựng lại (QĐ 8): `LevelChangeRaisesWaterSurfaceChanged`; ảnh bước 6 của Lệnh (mép nước mỏng dần sau khi lan = chunk đã dựng lại theo mức).
- Ô nối (QĐ 9): `BridgeFillsOneCellGap`, `BridgeDrainsWhenOneSideGone`; regression `WaterFlowTests` toàn bộ (lan, rãnh, rút, thác).
- Phần cũ có thể ảnh hưởng: `SeaFloodTests`, `WorldEditorTests`, `HabitatTrackerTests`, `HabitatWorldTests`, `HabitatDemoTests`; PlayMode `WaterFlowPlayTests`, `HabitatPlayTests`, `SimViewMapsPlayTests`, `TerrainEditPlayTests`, `DayCycleWaterPlayTests`, `FlyCameraWaterPlayTests`, `OceanBackdropTests`; mã băm.
- EditMode + PlayMode toàn bộ.

### Lệnh executor chạy
1. MCP `refresh_unity` → `read_console` (Error) = 0.
2. MCP `run_tests` EditMode lọc: `BlockMesherTests`, `WaterFlowTests`, `WaterConfigTests`, `ConfigTests`, `SeaFloodTests`, `WorldEditorTests`, `HabitatTrackerTests`, `HabitatWorldTests`, `HabitatDemoTests`, `GoldenHashTests`, `SimDemoTests`, `SimDemoMapsTests`.
   - Test mesher mới fail ngay sau lần biên dịch đầu → `refresh_unity` lại rồi chạy lại một lần, vì Burst có thể chưa nạp bản mới (đã gặp ở brief 0077).
3. MCP `run_tests` PlayMode lọc: `WaterFlowPlayTests`, `HabitatPlayTests`, `SimViewMapsPlayTests`, `TerrainEditPlayTests`, `DayCycleWaterPlayTests`, `FlyCameraWaterPlayTests`, `OceanBackdropTests`.
4. MCP `run_tests` EditMode toàn bộ; PlayMode toàn bộ. Ghi dòng tổng. MCP rớt khi PlayMode chạy: `sleep 60` rồi `get_test_job`, lặp lại; không kết thúc lượt khi test còn chạy.
5. Bước 6 (tái hiện) qua MCP `execute_code` trong Play.
6. Play SimView, chụp **1** ảnh: đào một ô cát cạnh ao (qua `view.Editor.TryDig`), chờ yên, đặt camera nhìn chéo từ trên xuống vùng nước vừa lan (thấy mép mỏng dần) + thác. Ghi đường dẫn ảnh (scratchpad).
7. Dọn: thoát Play; `git checkout -- client/ProjectSettings/EditorSettings.asset`; xoá `client/Assets/InitTestScene*.unity(.meta)` nếu có. `git status --short` chỉ còn file của brief.
8. `grep` ở Tiêu chí xong → rỗng.

Ghi giờ bắt đầu/kết thúc (lệnh `date`) vào Kết quả.

### User tự test (gộp với 0076 + 0077)
Scene **SimView** trên Mac, Play:
1. Thác cạnh ao (phía +x): thấy cột nước rơi vào ao, không tràn ra cỏ.
2. Đào cát cạnh ao ra một vùng rộng: nước lan tối đa 5 ô, mặt nước thấp dần về mép; không thấy hộp xanh chồng nhau.
3. Đào rãnh 1 ô dài 10–15 ô: nước chảy hết rãnh, mặt nước đầy rãnh. Rãnh mở ra vùng rộng: lan 5 ô quanh cửa rãnh, mép mỏng dần.
4. Hai vùng nước cách nhau 1 ô trống thì nối liền. Cách nhau 2 ô thì không nối.
5. Chặn giữa rãnh → đoạn sau rút. Lấp nguồn thác → rãnh và thác rút. Khi nước rút, mặt nước hạ dần chứ không biến mất cả khối.
6. Nước chảy xuống sườn bậc thang: mặt nước liền, không thành bậc hộp.
7. Ao, biển nhìn như cũ. Đào bờ biển → biển loang như trước.
8. (Lỗi đứng trên mặt nước: chờ kết quả bước 6 và brief sửa sau.)

## Dừng và chuyển lại Opus khi
- Brief mơ hồ hoặc mâu thuẫn với code thật (số dòng lệch nhiều, chữ ký khác).
- Test được giữ nguyên kỳ vọng fail mà phải đổi kỳ vọng; mã băm đổi.
- Burst không biên dịch được job khi gọi `Cell.*`; cần API Unity không có trong brief.
- Cần quyết định ngoài "Quyết định đã chốt"; phải sửa file ngoài "Phạm vi".
- Cùng một lỗi sửa 2 lần vẫn chưa qua.
- Bước 6 tái hiện được lỗi → ghi số liệu vào Vướng mắc, nhưng **vẫn hoàn tất** bước 7 và báo `chờ review`. Không sửa lỗi đó.

---

## Kết quả

## Kết quả kiểm chứng

## Vướng mắc

---

## Review
