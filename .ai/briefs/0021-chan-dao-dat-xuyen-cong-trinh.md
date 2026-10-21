# Brief 0021 — Đào/đặt không xuyên qua nhà, kho, dấu mốc

> Trạng thái: đã duyệt
> Review: đầy đủ + user xem
> Model thực thi: Sonnet · medium
> Cập nhật: 2026-10-21 12:45

<!-- Planner (Opus) viết, Executor (Sonnet) làm theo ở PHIÊN KHÁC — executor không có lịch sử chat, không được tự quyết.
Trạng thái: chờ làm → đang làm → chờ review → đã duyệt; hoặc → cần Opus (executor dừng, hỏi ở mục Vướng mắc).
File được commit. -->

## Mục tiêu
User xem brief [0020](0020-world-simview-tren-world-3-map.md) ngày 2026-10-21 và thấy: một khối bị nhà/kho che vẫn đào/đặt xuyên qua được.

Nguyên nhân:
- Hộp nhà/kho của `SimView` và khối dấu mốc của `TerrainChunkView` bị gỡ collider (cố ý từ sim bước 6 / brief 0018, để "không chặn raycast đào/đặt").
- Vì vậy tia chuột đi xuyên qua chúng và trúng địa hình phía sau.

**User chốt 2026-10-21:**
- Nhà, kho, dấu mốc chặn tia đào/đặt. Tia trúng chúng → không có ô chọn, không đào/đặt.
- Sinh vật, ô ruộng, mầm **không** chặn: sinh vật di chuyển liên tục; ô ruộng phải đào được để huỷ vụ.

## Bối cảnh cần đọc
- `client/Assets/Game/Client/TerrainEditInput.cs`:
  - trường `TerrainChunkView view;` (`:23`), `public float maxDistance = 200f;` (`:15`);
  - trong `Update()`:
    - `:60` `var cam = Camera.main;`
    - `:64` `var ray = cam.ScreenPointToRay(mouse.position.ReadValue());`
    - `:65-69` `if (!Physics.Raycast(ray, out var hit, maxDistance)) { outline.enabled = false; return; }`
    - `:71` `var digCell = view.CellAt(hit.point - hit.normal * 0.5f);`
    - `:72` `var placeCell = view.CellAt(hit.point + hit.normal * 0.5f);`
    - phần còn lại dùng `digCell`/`placeCell`.
  - Đầu file có `<summary>` của lớp (`:7-11`).
- `client/Assets/Game/Client/TerrainChunkView.cs`:
  - `Transform chunksRoot;` (`:54`), tạo trong `Build()` (`:123-124`). Mọi GameObject chunk (có `MeshCollider`) là con của `chunksRoot` (`:456`).
  - `public Vector3Int CellAt(Vector3 worldPoint)` ở `:221`.
  - `BuildLandmarkMarkers()` có `<summary>` ở `:370`, nội dung: "…; không collider."
  - `MarkerBox(...)` ở `:399`; dòng `:403` là `DestroyImmediate(go.GetComponent<Collider>()); // không chặn raycast đào/đặt; …`.
- `client/Assets/Game/Client/SimView.cs`:
  - `Box(string name, Vector3 localPos, Vector3 scale, Color color, Transform parent)` ở `:234`; dòng `:238` là `Destroy(go.GetComponent<Collider>()); // không chặn raycast đào/đặt`.
  - Các chỗ gọi `Box`:
    - nhà `:139-140`, kho `:142`;
    - ruộng `:145`, mầm `:146`;
    - thân, mũi, hàng của sinh vật `:223`, `:226`, `:227`.
  - Hộp nằm dưới con `"Sim"` của GameObject Terrain.
- `client/Assets/Game/Tests/Client/LandmarkViewPlayTests.cs:69`: `Assert.AreEqual(0, root.GetComponentsInChildren<Collider>(true).Length);`. Yêu cầu cũ ("không collider") nay đổi theo quyết định của user, nên dòng này được sửa (bước 3).
- Mẫu SetUp PlayMode trên world v3 có SimView: `client/Assets/Game/Tests/Client/SimViewMapsPlayTests.cs:17-39`.
  - Trường `camGo, go, view, sv`;
  - camera `MainCamera` ở (192, 40, 150), `Euler(25, 0, 0)`;
  - `TerrainChunkView` có `source = MapsV3`, `streaming = true`, `streamTarget = camGo.transform`;
  - `AddComponent<SimView>()`;
  - 2 lần `yield return null`.
- `DebugFlyCamera` va chạm bằng dữ liệu world (`FlyCameraCollision.Move(terrain.World, …)`, `DebugFlyCamera.cs:49`), không dùng Physics → thêm collider không ảnh hưởng camera.

API Unity dùng:
- `Physics.Raycast` [docs](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/Physics.Raycast.html);
- `Transform.IsChildOf` [docs](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/Transform.IsChildOf.html);
- `Physics.SyncTransforms` [docs](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/Physics.SyncTransforms.html) (chỉ trong test, để collider vừa tạo có mặt ngay cho raycast);
- `RaycastHit.collider` [docs](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/RaycastHit-collider.html).

## Quyết định đã chốt (không được đổi)
1. **Chặn:**
   - hộp nhà và hộp kho của `SimView` giữ `BoxCollider` (từ `CreatePrimitive`);
   - mọi khối dấu mốc (`MarkerBox`) giữ `BoxCollider`.
   - Hộp ruộng, mầm, thân/mũi/hàng của sinh vật vẫn gỡ collider như cũ.
2. **Chỉ địa hình mới cho ô chọn:** `TerrainEditInput` lấy hit đầu tiên của `Physics.Raycast`. Nếu collider đó không thuộc chunk địa hình (`!view.IsTerrainCollider(hit.collider)`) → ẩn khung chọn, không đào/đặt, không đổi `lastResult`.
3. `TerrainChunkView.IsTerrainCollider(Collider c)` = `c != null && chunksRoot != null && c.transform.IsChildOf(chunksRoot)`.
4. Tách phần chọn ô thành hàm public `TerrainEditInput.Pick(Ray ray, out Vector3Int digCell, out Vector3Int placeCell)` để test gọi được; `Update` dùng hàm này. Cách tính ô giữ y hệt (`CellAt(hit.point ∓ hit.normal * 0.5f)`).
5. Không đổi luật đào/đặt trong `WorldEditor`, không đổi Game.Core, không đổi scene, không build APK.
6. Giờ bắt đầu/kết thúc lấy bằng `date "+%Y-%m-%d %H:%M"` ngay lúc đó.

## Phạm vi
- **Được sửa:**
  - `client/Assets/Game/Client/TerrainEditInput.cs`
  - `client/Assets/Game/Client/TerrainChunkView.cs` (chỉ thêm `IsTerrainCollider` + sửa `MarkerBox` và `<summary>` của `BuildLandmarkMarkers`)
  - `client/Assets/Game/Client/SimView.cs` (chỉ `Box` + 2 chỗ gọi cho nhà, kho)
  - `client/Assets/Game/Tests/Client/LandmarkViewPlayTests.cs` (chỉ dòng `:69`)
  - mục Kết quả / Kết quả test / Vướng mắc / Kiểm chứng của brief này.
- **Được tạo:** `client/Assets/Game/Tests/Client/EditOcclusionPlayTests.cs` (+ `.meta`).
- **Không được sửa:** mọi file khác. `ProjectSettings/` phải trả lại sau test.

## Các bước
0. `date "+%Y-%m-%d %H:%M"` → ghi "Bắt đầu" và "Base commit" vào Kết quả; trạng thái `đang làm`.

1. **`TerrainChunkView.cs`:**
   1. Thêm ngay trước `public Vector3Int CellAt(Vector3 worldPoint)` (`:221`), giữ dòng `/// <summary>Ô gameplay chứa điểm …` của `CellAt` đi liền với `CellAt`:
      ```csharp
      /// <summary>Collider thuộc chunk địa hình (không phải khối dấu mốc, công trình…) — brief 0021.</summary>
      public bool IsTerrainCollider(Collider c) => c != null && chunksRoot != null && c.transform.IsChildOf(chunksRoot);

      ```
   2. Trong `MarkerBox`, xoá dòng `DestroyImmediate(go.GetComponent<Collider>()); …` (`:403`). Thay bằng dòng chú thích:
      `// Giữ BoxCollider: dấu mốc che địa hình phía sau → chặn đào/đặt xuyên qua (brief 0021).`
   3. Trong `<summary>` của `BuildLandmarkMarkers` (`:370`), đổi "; không collider." thành "; có collider để chặn đào/đặt xuyên qua (brief 0021)."
   Xong khi: biên dịch không lỗi.

2. **`SimView.cs`:**
   1. Đổi chữ ký thành `GameObject Box(string name, Vector3 localPos, Vector3 scale, Color color, Transform parent, bool solid = false)`.
      Thay dòng `Destroy(go.GetComponent<Collider>()); // không chặn raycast đào/đặt` bằng:
      ```csharp
      if (!solid)
          Destroy(go.GetComponent<Collider>()); // sinh vật, ruộng, mầm: không chặn raycast đào/đặt
      ```
   2. Ở 2 chỗ gọi cho nhà (`:139-140`) và kho (`:142`), thêm đối số cuối `solid: true`. Các chỗ gọi khác giữ nguyên.
   Xong khi: biên dịch không lỗi.

3. **`TerrainEditInput.cs`:**
   1. Thêm hàm public (đặt ngay sau `Update()`):
      ```csharp
      /// <summary>
      /// Ô sẽ đào / ô sẽ đặt theo tia. Chỉ địa hình cho ô chọn: tia trúng trước vào nhà, kho, dấu mốc (có collider) → false (brief 0021).
      /// </summary>
      public bool Pick(Ray ray, out Vector3Int digCell, out Vector3Int placeCell)
      {
          digCell = placeCell = default;
          if (!Physics.Raycast(ray, out var hit, maxDistance) || !view.IsTerrainCollider(hit.collider))
              return false;
          digCell = view.CellAt(hit.point - hit.normal * 0.5f);
          placeCell = view.CellAt(hit.point + hit.normal * 0.5f);
          return true;
      }
      ```
   2. Trong `Update()`, thay khối từ `if (!Physics.Raycast(ray, out var hit, maxDistance))` (`:65`) tới hết dòng `var placeCell = …` (`:72`) bằng:
      ```csharp
      if (!Pick(ray, out var digCell, out var placeCell))
      {
          outline.enabled = false;
          return;
      }
      ```
      Phần sau (`outline.enabled = true;` …) giữ nguyên.
   3. Thêm vào cuối `<summary>` của lớp 1 dòng: `/// Tia trúng nhà, kho hay dấu mốc trước địa hình → không có ô chọn (brief 0021).`
   Xong khi: biên dịch không lỗi; `grep -n "Physics.Raycast" client/Assets/Game/Client/TerrainEditInput.cs` chỉ còn 1 dòng, trong `Pick`.

4. **`LandmarkViewPlayTests.cs:69`:** thay bằng
   ```csharp
   Assert.AreEqual(root.GetComponentsInChildren<MeshRenderer>(true).Length, root.GetComponentsInChildren<Collider>(true).Length); // mỗi khối có collider (brief 0021)
   ```
   Không sửa dòng nào khác của file.

5. **Tạo `client/Assets/Game/Tests/Client/EditOcclusionPlayTests.cs`** (namespace `Game.Client.Tests`; `using System.Collections; using Game.Core; using Game.Core.Sim; using NUnit.Framework; using UnityEngine; using UnityEngine.TestTools;`).
   - SetUp/TearDown như `SimViewMapsPlayTests.cs:17-39` (trường `camGo, go, view, sv`). Thêm trường `TerrainEditInput input;`, gán `input = go.AddComponent<TerrainEditInput>();` ngay sau `AddComponent<SimView>()`. Sau 2 lần `yield return null`, gọi `Physics.SyncTransforms();`.
   - Hàm phụ `static Ray Down(float x, float z, float top) => new Ray(new Vector3(x, top, z), Vector3.down);`. Mọi tia dùng `top = 80f`: trên đỉnh world (48) và trên mọi khối dấu mốc, vì tia bắt đầu bên trong collider thì không trúng collider đó. Quãng tới đất < `maxDistance` 200.
   - Có 4 test:
   1. `HouseAndStorageBlockPick`:
      - Với mỗi nhà `h` trong `sv.Sim.State.Houses`: `input.Pick(Down(h.Cell.X + h.SizeX * 0.5f, h.Cell.Z + h.SizeZ * 0.5f, 80f), out _, out _)` → false.
      - Kho `s = sv.Sim.State.Storages[0]`: `Pick(Down(s.Cell.X + 0.5f, s.Cell.Z + 0.5f, 80f), …)` → false.
      - `go.transform.Find("Sim").GetComponentsInChildren<Collider>().Length == 3` (2 nhà + 1 kho).
   2. `PlotsAndCreaturesDoNotBlock`:
      - Mỗi ô ruộng `p` trong `sv.Sim.State.Plots`: `Pick(Down(p.Soil.X + 0.5f, p.Soil.Z + 0.5f, 80f), out var dig, out var place)` → true; `dig == new Vector3Int(p.Soil.X, p.Soil.Y, p.Soil.Z)`; `place == dig + Vector3Int.up`.
      - Mỗi sinh vật `c` (ô `c.Cell` lúc đó): `Pick(Down(c.Cell.X + 0.5f, c.Cell.Z + 0.5f, 80f), out var d2, out _)` → true; `d2.x == c.Cell.X`, `d2.z == c.Cell.Z`.
   3. `LandmarksBlockPick`: mỗi `l` trong `view.Landmarks` → `Pick(Down(l.X + 0.5f, l.Z + 0.5f, 80f), out _, out _)` → false.
   4. `OpenGroundStillPicks`:
      - Cột góc cửa sổ trại: `x = site.Dx + SimDemo.BaseX − 1`, `z = site.Dz + SimDemo.BaseZ − 1`, `site = sv.Site.Value`.
      - `Pick(Down(x + 0.5f, z + 0.5f, 80f), out var dig, out _)` → true; `dig == new Vector3Int(x, site.Height(x, z), z)`.
      - `Assert.IsFalse(view.IsTerrainCollider(null))`.
   - Ghi `Debug.Log("EDITOCCLUSIONTEST ok")` ở cuối test 1.
   Xong khi: MCP `run_tests` PlayMode lọc `EditOcclusionPlayTests` → 4/4.

6. **Chạy toàn bộ + commit:**
   1. PlayMode lọc `EditOcclusionPlayTests`, `LandmarkViewPlayTests`, `TerrainEditPlayTests`, `SimViewPlayTests`, `SimViewMapsPlayTests`, `MinimapViewPlayTests` → đạt hết.
   2. EditMode cả bộ (322), PlayMode cả bộ (35 + 4 = 39). Dọn: `git checkout -- client/ProjectSettings/EditorSettings.asset`.
   3. `git status --short`: chỉ còn file trong Phạm vi. Commit code với message `brief 0021: đào/đặt không xuyên qua nhà, kho, dấu mốc`.
   4. `date "+%Y-%m-%d %H:%M"` → ghi "Kết thúc", trạng thái `chờ review`, commit brief với message `brief 0021: kết quả`.

## Tiêu chí xong
- Biên dịch không lỗi; không đổi file ngoài Phạm vi.
- `EditOcclusionPlayTests` 4/4; `LandmarkViewPlayTests` đạt với dòng `:69` mới; các test PlayMode cũ khác đạt không sửa.
- EditMode 322/322; PlayMode 39/39.
- `git status` sạch sau commit.

## Test sau khi xong

### Phần cần test
| Phần | Bước | Test | Kết quả mong đợi |
|---|---|---|---|
| Nhà, kho chặn đào/đặt | 2, 3 | `HouseAndStorageBlockPick` | Tia xuống nhà/kho → không có ô chọn; dưới "Sim" có đúng 3 collider |
| Ruộng, sinh vật không chặn | 2, 3 | `PlotsAndCreaturesDoNotBlock` | Tia xuống ruộng → đào đúng ô ruộng, đặt ô trên; tia qua sinh vật → trúng cột dưới sinh vật |
| Dấu mốc chặn | 1, 3 | `LandmarksBlockPick`; `LandmarkViewPlayTests` (dòng `:69` mới) | Tia xuống dấu mốc → không có ô chọn; mỗi khối dấu mốc có collider |
| Đất trống vẫn chọn được | 1, 3 | `OpenGroundStillPicks` | Ô đào = mặt đất cột đó; `IsTerrainCollider(null)` = false |
| Đào/đặt cũ không đổi | 3 | `TerrainEditPlayTests`, `TerrainStreamingPlayTests`, `MinimapViewPlayTests` | Đạt, không sửa |
| SimView không đổi | 2 | `SimViewPlayTests`, `SimViewMapsPlayTests` | Đạt, không sửa |
| Regression chung | — | EditMode + PlayMode cả bộ | 322/322, 39/39 |
| Cảm giác khi chơi | — | User xem trên Mac (mục dưới) | User xác nhận |

### Lệnh executor chạy
1. MCP `refresh_unity` → `read_console` (types error) → 0 lỗi.
2. `grep -n "Physics.Raycast" client/Assets/Game/Client/TerrainEditInput.cs` → 1 dòng.
3. MCP `run_tests` PlayMode lọc theo bước 6.1 → ghi số đạt.
4. MCP `run_tests` EditMode cả bộ → ghi dòng tổng kết.
5. MCP `run_tests` PlayMode cả bộ → ghi dòng tổng kết. Sau đó dọn: `git checkout -- client/ProjectSettings/EditorSettings.asset`.
6. `git status --short` → commit (bước 6).
7. `date "+%Y-%m-%d %H:%M"` → "Kết thúc"; commit brief.

### User tự test
Mở `Assets/Game/Client/Scenes/SimView.unity`, bấm Play:
1. Rê chuột lên nhà, kho: không hiện khung trắng. Click trái/phải: không đào/đặt gì, kể cả khối đất ngay sau nhà.
2. Rê chuột lên ô ruộng: khung trắng ở ô ruộng, đào được (vụ bị huỷ như cũ).
3. Rê chuột qua sinh vật: khung trắng ở khối đất dưới/sau sinh vật như trước.
4. Bay tới một dấu mốc (mở bản đồ để tìm): rê chuột lên khối dấu mốc → không có khung trắng; đất quanh nó vẫn đào/đặt được.
5. Mở `TerrainView.unity`: dấu mốc cũng chặn như trên; đào/đặt chỗ khác bình thường.

## Dừng và chuyển lại Opus khi
- Brief mơ hồ hoặc mâu thuẫn với code thật (số dòng không khớp nội dung trích).
- Một test cũ (ngoài `LandmarkViewPlayTests:69`) hỏng.
- `PlotsAndCreaturesDoNotBlock` hỏng vì tia trúng hộp ruộng, mầm hay sinh vật. Không tự đổi QĐ 1.
- Cần quyết định ngoài "Quyết định đã chốt", hoặc phải sửa file ngoài "Phạm vi".
- Cùng một lỗi sửa 2 lần vẫn chưa qua.

---
<!-- Từ đây trở xuống: EXECUTOR ghi. -->

## Kết quả
- Bắt đầu: 2026-10-21 14:11
- Base commit: 670ff8e
- Chạy bằng: claude-sonnet-5-5 · medium
- Kết thúc: 2026-10-21 14:17; commit code 4209dc9
- File: TerrainEditInput.cs, TerrainChunkView.cs, SimView.cs, LandmarkViewPlayTests.cs (dòng :69) (sửa); EditOcclusionPlayTests.cs (+.meta, mới)

## Kết quả test
- Biên dịch: 0 lỗi. `grep -c Physics.Raycast TerrainEditInput.cs` = 1 (trong `Pick`).
- PlayMode lọc (EditOcclusion, Landmark, TerrainEdit, SimView, SimViewMaps, Minimap): 27/27 đạt, gồm `EditOcclusionPlayTests` 4/4 (nhà/kho chặn, ruộng+sinh vật không chặn, dấu mốc chặn, đất trống vẫn chọn).
- EditMode cả bộ: 322/322. PlayMode cả bộ: 39/39. Đã `git checkout -- EditorSettings.asset`.
- Cảm giác khi chơi: user xem trên Mac, phản hồi "ok ổn" [user 2026-10-21].

## Vướng mắc
Không có.

---
<!-- PLANNER ghi khi review. -->

## Review
- `Chạy bằng` (claude-sonnet-5-5 · medium) khớp `Model thực thi` (Sonnet · medium). Mọi dòng "Phần cần test" có kết quả.
- Diff `4209dc9` [repo]: đúng QĐ 1–5. Nhà/kho `solid: true`, ruộng/mầm/sinh vật vẫn gỡ collider. `MarkerBox` giữ collider. `IsTerrainCollider` + `Pick` đúng mẫu, cách tính ô không đổi, `Update` dùng `Pick`. Chỉ sửa file trong Phạm vi; `git status` sạch.
- Test: EditOcclusion 4/4, nhóm PlayMode lọc 27/27, EditMode 322/322, PlayMode 39/39 [theo Kết quả test]; không chạy lại (không đáng ngờ).
- User xem trên Mac: "ok ổn" [user 2026-10-21].
- Kết luận: đã duyệt
