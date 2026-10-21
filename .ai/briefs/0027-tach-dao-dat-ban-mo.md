# Brief 0027 — Tách công cụ đào / đặt, bản mờ khi đặt

> Trạng thái: chờ review
> Review: đầy đủ + user xem
> Model thực thi: Sonnet · medium
> Cập nhật: 2026-10-21 18:33

<!-- Planner (Opus) viết, Executor (Sonnet) làm theo ở PHIÊN KHÁC — executor không có lịch sử chat, không được tự quyết.
Trạng thái: chờ làm → đang làm → chờ review → đã duyệt; hoặc → cần Opus (executor dừng, hỏi ở mục Vướng mắc).
Review: nhẹ (đọc mục Kết quả) | đầy đủ (đọc cả diff) | user xem (có phần user tự kiểm bằng mắt/chơi thử) — có thể ghép "đầy đủ + user xem".
Model thực thi: BẮT BUỘC, đúng dạng `<Model> · <effort>` — Model: Opus | Sonnet | Haiku | Fable; effort: low | medium | high | xhigh | max (chọn theo AGENTS.md §10.2).
Executor kiểm tra model + effort của phiên TRƯỚC khi làm (scripts/brief_check.py); lệch hoặc không xác định được → cảnh báo user và DỪNG.
File được commit. Tự kiểm trước khi giao (AGENTS.md §10.3): executor có phải hỏi gì không? còn chỗ nào bỏ ngỏ 2 phương án? -->

## Mục tiêu
Làm theo [task contract](../tasks/m1-dieu-khien-tach-dao-dat.md): đào và đặt thành **hai công cụ riêng**.
- **Tab** chuyển giữa Đào và Đặt. **Chuột trái** dùng công cụ đang cầm.
- **Đào:** khung trắng trên khối đang có, như hiện nay.
- **Đặt:** **bản mờ** của khối sắp đặt, hiện ở ô trống sát mặt khối đang trỏ.

Ước lượng 0,5–1 ngày-người. Ghi giờ bắt đầu/kết thúc vào mục Kết quả.

## Bối cảnh cần đọc
Đường dẫn tính từ `client/Assets/Game/`.
- `Core/WorldEditor.cs:86-107` — `public EditResult TryPlace(int x, int y, int z, BlockMaterial material)`.
  - Kiểm theo thứ tự: `OutOfBounds` → `NotPlaceable` → `NotEmpty` (ô không rỗng, không nước) → `NotAttached` (`HasSolidNeighbor`) → `Occupied` (`IsOccupied`) → `NoMaterial` (`Inventory.TryTake`).
  - Đủ điều kiện thì ghi ô, phát `CellChanged`, rồi gọi `RevertFarmlandBelow`.
  - `MaterialInventory` có `Get(m)`, `Add(m)`, `TryTake(m)`.
- `Client/TerrainEditInput.cs`:
  - Doc comment class `:7-12`. Field `:16-27`: `Palette`, `PaletteNames`, `view`, `outline`, `selected`, `lastResult`. `Awake` `:29-33`.
  - `Update` `:35-93`:
    - phím 1–4 `:37-44`;
    - chặn khi bản đồ / vòng thời điểm mở `:46-52`;
    - lăn chuột `:53-59`;
    - `Pick` rồi khung `:65-77`;
    - chặn khi đang bay `:79`;
    - chuột trái đào `:81-87`, chuột phải đặt `:88-92`.
  - `Pick` `:98-106`: `digCell` = ô sau mặt trúng, `placeCell` = ô trước mặt trúng (ô trống sát mặt). **Không sửa.**
  - `Explain` `:114-124`. `OnGUI` `:126-146`, dòng hướng dẫn ở `:130`.
  - `CreateOutline` `:148-178`: `LineRenderer` con tên "Ô đang chọn", dùng shader `Game/SelectionOverlay`, khối đơn vị 0..1 đặt bằng `localPosition = ô`.
- `Client/Resources/Shaders/SelectionOverlay.shader`: unlit, `_BaseColor * vertex color`, `ZTest Always`. Shader mới ở bước 3 chép theo khung này.
- `World/BlockMesher.cs:91-101` — `public static uint TopColor(BlockMaterial m)`, giá trị đóng gói `r | g << 8 | b << 16 | pattern << 24`. `Game.Client.asmdef` đã tham chiếu `Game.World`.
- `Client/TerrainChunkView.cs`:
  - `Inventory` / `Editor` (`:63-64`, tạo ở `:159-160`);
  - `Dig(Vector3Int)` / `Place(Vector3Int, BlockMaterial)` (`:230-232`);
  - `IsTerrainCollider` (`:221`), `CellAt` (`:224`).
- `Core/WorldData.cs:83-84` — `SurfaceHeight(x, z)`: ô rắn cao nhất trong cột, không tính nước.
- Test mẫu:
  - `Tests/Core/WorldEditorTests.cs:14-31` (`SetUp`: world 32×16×32, đá đáy y = 0, `changed` đếm `CellChanged`), `:125-129` (`PlaceOnOccupiedRejected`).
  - `Tests/Client/TerrainEditPlayTests.cs:17-31` (SetUp / TearDown), `:124-131` (`EditInputSurvivesBuild`).
  - `Tests/Client/EditOcclusionPlayTests.cs` gọi `input.Pick(...)` trực tiếp, không giả lập chuột.
- API Unity dùng mới (đã tra 6000.4):
  - `Mesh.vertices` [docs](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/Mesh-vertices.html), `Mesh.triangles` [docs](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/Mesh-triangles.html);
  - `Renderer.enabled` [docs](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/Renderer-enabled.html), `Renderer.shadowCastingMode` [docs](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/Renderer-shadowCastingMode.html);
  - `Keyboard.tabKey` (Input System 1.19, [repo] `client/Library/PackageCache/com.unity.inputsystem@21a28c3a6c83/InputSystem/Devices/Keyboard.cs:1317`: `public KeyControl tabKey => this[Key.Tab];`).
  - `MeshFilter.sharedMesh`, `Shader.Find`, `Material.SetColor` đã có trong code.
  - **Không** dùng `GameObject.CreatePrimitive`: hàm này tự thêm collider ([docs](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/GameObject.CreatePrimitive.html)).

## Quyết định đã chốt (không được đổi)
**User chốt 2026-10-21:**
1. Tab chuyển Đào ⇄ Đặt (Telex dùng chữ s/f/r/x/j để bỏ dấu nên không dùng phím chữ). Chuột trái dùng công cụ đang cầm. **Chuột phải không làm gì.**
2. Bản mờ:
   - màu vật liệu đang chọn, alpha 0,5;
   - chỉ hiện khi `CheckPlace` = `Ok`;
   - `NoMaterial` (kho hết) → bản mờ **đỏ** (1; 0,2; 0,2; 0,5);
   - mọi kết quả khác → ẩn.
3. Cầm Đặt: khung trắng tắt. Cầm Đào: bản mờ tắt.

**Planner chốt:**

4. Luật đặt chỉ nằm trong `WorldEditor`: thêm `CheckPlace`, không sửa dữ liệu. `TryPlace` gọi `CheckPlace`, hành vi và thứ tự kiểm giữ nguyên. Client không tự kiểm luật.
5. Bản mờ:
   - con của Terrain, tên **"Bản mờ"**;
   - `MeshFilter` + `MeshRenderer`, khối lập phương 0..1 tự dựng (8 đỉnh, 36 chỉ số), **không có collider**, không đổ bóng;
   - shader mới `Game/GhostBlock`: chỉ `_BaseColor`, `ZTest LEqual`, `ZWrite Off`, `Cull Off`, trộn alpha, queue Transparent.
   - Lý do `ZTest LEqual`: bị sinh vật/nhà che thì không vẽ đè. `Cull Off`: khỏi lo chiều tam giác.
6. Công cụ mặc định khi mở scene: **Đào**.
7. Logic tách thành hàm public để test PlayMode gọi thẳng, không giả lập chuột/phím (giống `Pick`): `ToggleTool`, `ShowAim`, `UseTool`. `Update` chỉ đọc input rồi gọi các hàm này.
8. Phím 1–4 và lăn chuột vẫn chọn vật liệu, dùng được ở cả hai công cụ.

## Phạm vi
- **Được sửa:**
  - `client/Assets/Game/Core/WorldEditor.cs`, `client/Assets/Game/Client/TerrainEditInput.cs`;
  - test: `client/Assets/Game/Tests/Core/WorldEditorTests.cs`, `client/Assets/Game/Tests/Client/TerrainEditPlayTests.cs`.
- **Được tạo:** `client/Assets/Game/Client/Resources/Shaders/GhostBlock.shader` (+ `.meta` do Unity sinh).
- **Không được sửa:**
  - `Pick`, `TerrainChunkView.cs`, `BlockMesher.cs`, `SelectionOverlay.shader`, scene `.unity`;
  - `design/`, task contract, test cũ khác (chỉ thêm test mới vào 2 file test trên);
  - mọi file khác.

## Các bước
Đường dẫn tính từ `client/Assets/Game/`.

1. **`Core/WorldEditor.cs`** — thêm ngay trước `TryPlace`:
   ```csharp
   /// <summary>Kiểm đặt (không sửa world, không trừ kho): cùng kết quả và thứ tự kiểm như TryPlace (brief 0027 — bản mờ).</summary>
   public EditResult CheckPlace(int x, int y, int z, BlockMaterial material)
   {
       if (!World.Size.InBounds(x, y, z))
           return EditResult.OutOfBounds;
       if (!IsPlaceable(material))
           return EditResult.NotPlaceable;
       var current = World.GetMaterial(x, y, z);
       if (current != BlockMaterial.Empty && current != BlockMaterial.Water)
           return EditResult.NotEmpty;
       if (!HasSolidNeighbor(x, y, z))
           return EditResult.NotAttached;
       if (IsOccupied != null && IsOccupied(x, y, z))
           return EditResult.Occupied;
       if (Inventory.Get(material) <= 0)
           return EditResult.NoMaterial;
       return EditResult.Ok;
   }
   ```
   - Thân `TryPlace`, thay các dòng kiểm từ `InBounds` tới `IsOccupied`, thành:
     ```csharp
     var check = CheckPlace(x, y, z, material);
     if (check != EditResult.Ok)
         return check;
     if (!Inventory.TryTake(material))
         return EditResult.NoMaterial;
     ushort cell = World.Get(x, y, z);
     ```
   - Phần ghi ô / `CellChanged` / `RevertFarmlandBelow` giữ nguyên.
   - Nếu `World.GetMaterial` hoặc `MaterialInventory.Get` không tồn tại đúng chữ ký → dừng, `cần Opus`.

   Xong khi: test `CheckPlace` mới và mọi test `WorldEditorTests` / `SeaFloodTests` cũ đạt.

2. **`Client/TerrainEditInput.cs`** — thêm kiểu công cụ, bản mờ, hàm public:
   - a. Trên class (trong namespace `Game.Client`):
     ```csharp
     /// <summary>Công cụ đang cầm (brief 0027): Tab chuyển.</summary>
     public enum EditTool { Dig, Place }
     ```
   - b. Field / property mới:
     ```csharp
     /// <summary>Màu bản mờ khi kho hết loại đang chọn.</summary>
     public static readonly Color MissingColor = new Color(1f, 0.2f, 0.2f, 0.5f);
     public EditTool Tool { get; private set; } = EditTool.Dig;
     public BlockMaterial SelectedMaterial => Palette[selected];
     public GameObject Ghost { get; private set; }
     public bool OutlineVisible => outline.enabled;
     public bool GhostVisible => ghostRenderer.enabled;
     public Vector3Int GhostCell => Vector3Int.RoundToInt(Ghost.transform.localPosition);
     public Color GhostColor { get; private set; }
     MeshRenderer ghostRenderer;
     Material ghostMat;
     ```
   - c. `Awake`: sau `outline = CreateOutline();` thêm `CreateGhost();`.
   - d. Hàm mới:
     ```csharp
     /// <summary>Tab: Đào ⇄ Đặt.</summary>
     public void ToggleTool() => Tool = Tool == EditTool.Dig ? EditTool.Place : EditTool.Dig;

     /// <summary>Màu bản mờ của vật liệu: TopColor, alpha 0,5.</summary>
     public static Color GhostColorOf(BlockMaterial m)
     {
         uint c = BlockMesher.TopColor(m);
         return new Color((c & 0xFF) / 255f, (c >> 8 & 0xFF) / 255f, (c >> 16 & 0xFF) / 255f, 0.5f);
     }

     /// <summary>
     /// Cập nhật khung / bản mờ theo công cụ (brief 0027). picked = false → tắt cả hai.
     /// Đào: khung ở digCell. Đặt: bản mờ ở placeCell khi CheckPlace = Ok (màu vật liệu) hoặc NoMaterial (đỏ); kết quả khác → ẩn.
     /// </summary>
     public void ShowAim(bool picked, Vector3Int digCell, Vector3Int placeCell)
     {
         outline.enabled = false;
         ghostRenderer.enabled = false;
         if (!picked || view.Editor == null) return;
         if (Tool == EditTool.Dig)
         {
             outline.enabled = true;
             outline.transform.localPosition = digCell;
             return;
         }
         var r = view.Editor.CheckPlace(placeCell.x, placeCell.y, placeCell.z, SelectedMaterial);
         if (r != EditResult.Ok && r != EditResult.NoMaterial) return;
         GhostColor = r == EditResult.Ok ? GhostColorOf(SelectedMaterial) : MissingColor;
         ghostMat.SetColor("_BaseColor", GhostColor);
         Ghost.transform.localPosition = placeCell;
         ghostRenderer.enabled = true;
     }

     /// <summary>Chuột trái: dùng công cụ đang cầm; ghi dòng kết quả ở HUD.</summary>
     public EditResult UseTool(Vector3Int digCell, Vector3Int placeCell)
     {
         if (Tool == EditTool.Dig)
         {
             var m = view.World.Size.InBounds(digCell.x, digCell.y, digCell.z)
                 ? view.World.GetMaterial(digCell.x, digCell.y, digCell.z) : BlockMaterial.Empty;
             var r = view.Dig(digCell);
             lastResult = r == EditResult.Ok ? $"Đào {digCell}: +1 {Name(m)}" : $"Đào {digCell}: không được — {Explain(r)}";
             return r;
         }
         var p = view.Place(placeCell, SelectedMaterial);
         lastResult = p == EditResult.Ok ? $"Đặt {PaletteNames[selected]} {placeCell}" : $"Đặt {PaletteNames[selected]}: không được — {Explain(p)}";
         return p;
     }
     ```
   - e. `CreateGhost()` (đặt sau `CreateOutline`):
     - `new GameObject("Bản mờ")`, `SetParent(transform, false)`.
     - Mesh lập phương: 8 đỉnh `(0,0,0)…(1,1,1)` theo `new Vector3(i & 1, i >> 1 & 1, i >> 2 & 1)` với i = 0..7; 12 tam giác (36 chỉ số), mỗi mặt 2 tam giác. Gán `mesh.vertices`, `mesh.triangles`, rồi `mesh.RecalculateBounds()`. Chiều tam giác không quan trọng vì shader `Cull Off`.
     - `AddComponent<MeshFilter>().sharedMesh = mesh`. `ghostRenderer = AddComponent<MeshRenderer>()`, `shadowCastingMode = ShadowCastingMode.Off`.
     - Material: `Shader.Find("Game/GhostBlock")`, không thấy thì dùng `Game/SelectionOverlay`. `ghostMat = new Material(shader)`, `ghostRenderer.sharedMaterial = ghostMat`.
     - `ghostRenderer.enabled = false`; `Ghost = go`.
     - **Không** `AddComponent` collider nào.
   - f. `Update`:
     - Ngay sau khối phím 1–4 (`:44`, vẫn trong `if (kb != null)`), thêm `if (kb.tabKey.wasPressedThisFrame) ToggleTool();`.
     - Ở chỗ chặn bản đồ/vòng thời điểm (`:50`), thay `outline.enabled = false;` bằng `ShowAim(false, default, default);`. Làm tương tự ở nhánh `!Pick` (`:68`).
     - Thay `:72-73` (`outline.enabled = true; outline.transform.localPosition = digCell;`) bằng `ShowAim(true, digCell, placeCell);`. Bọc phần tính `widthMultiplier` (`:74-77`) trong `if (outline.enabled) { … }`.
     - Thay `:81-92` (hai nhánh chuột trái/phải) bằng `if (mouse.leftButton.wasPressedThisFrame) UseTool(digCell, placeCell);`.
     - Bỏ nhánh chuột phải.
   - g. `OnGUI` — dòng `:130` thay bằng:
     ```csharp
     var text = $"Công cụ: <b><color=yellow>{(Tool == EditTool.Dig ? "Đào" : "Đặt")}</color></b> (Tab đổi) · Chuột trái: dùng công cụ · 1–4 / lăn chuột: chọn loại · WASD, Q/E, giữ chuột giữa: camera · dừng bay mới đào/đặt được\n";
     ```
   - h. Doc comment class (`:7-12`) — thay dòng 2 (`chuột trái đào, chuột phải đặt…`) bằng:

     `Tab đổi công cụ Đào ⇄ Đặt (brief 0027), chuột trái dùng công cụ; phím 1–4 hoặc lăn chuột chọn đất/cỏ/đá/cát. Đào: khung trắng = ô sẽ đào. Đặt: bản mờ màu vật liệu ở ô sẽ đặt (đỏ = kho hết; sai luật → không có); nhãn góc trái = công cụ, kho vật liệu, lý do khi không đào/đặt được.`
   - Cần `using UnityEngine.Rendering;` cho `ShadowCastingMode`, hoặc ghi đầy đủ tên như `CreateOutline` đang làm.

   Xong khi: biên dịch không lỗi; test PlayMode mới đạt.

3. **`Client/Resources/Shaders/GhostBlock.shader`** — chép khung `SelectionOverlay.shader` và đổi:
   - Comment đầu: `// Bản mờ khối sắp đặt (TerrainEditInput, brief 0027): unlit, trong suốt, ZTest thường (bị vật phía trước che). Nằm trong Resources để bản build có shader cho Shader.Find.`
   - Tên `Shader "Game/GhostBlock"`.
   - `Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" }`.
   - Pass: `ZTest LEqual`, `ZWrite Off`, `Cull Off`, `Blend SrcAlpha OneMinusSrcAlpha`.
   - Bỏ `color` khỏi `Attributes` / `Varyings`. `frag` trả `_BaseColor`.

   Xong khi: `read_console` không có lỗi shader; `Shader.Find("Game/GhostBlock")` khác null (assert trong test PlayMode).

4. **Test** theo "Phần cần test", chạy theo "Lệnh executor chạy".

## Tiêu chí xong
- EditMode hết đỏ, số test = 364 + số test mới. PlayMode hết đỏ, số test = 69 + số test mới.
- Test cũ đạt mà không sửa assert.
- `Game.Core` không thêm `using UnityEngine`.
- `git status` sạch ngoài các file trong Phạm vi. Đã dọn `EditorSettings.asset`.

## Test sau khi xong

### Phần cần test
**Mới — EditMode** (`WorldEditorTests.cs`, dùng `SetUp` sẵn có):
- `CheckPlaceMatchesRulesWithoutSideEffects` — mỗi ca kiểm kết quả, rồi kiểm world và kho không đổi (`changed` rỗng, `inv.Get` không đổi):
  - (-1,1,5) Dirt → `OutOfBounds`;
  - (5,1,5) `Water` → `NotPlaceable`;
  - (1,1,1) Dirt → `NotEmpty`;
  - (5,5,5) Dirt → `NotAttached`;
  - `IsOccupied = (x,y,z) => x == 4 && y == 1 && z == 4`, (4,1,4) Dirt → `Occupied`;
  - (5,1,5) Dirt khi kho rỗng → `NoMaterial`;
  - `inv.Add(Dirt)`, (5,1,5) Dirt → `Ok`, `inv.Get(Dirt)` vẫn 1, ô (5,1,5) vẫn `Empty`;
  - (2,1,1) là ô nước → `Ok` (đặt được vào nước), ô vẫn là `Water`.
- `TryPlaceAgreesWithCheckPlace` — cùng `SetUp`, theo đúng thứ tự sau. Mỗi ca: `var c = editor.CheckPlace(...)`, rồi `Assert.AreEqual(c, editor.TryPlace(...))`:
  - (-1,1,5), (5,1,5) với `Water`, (1,1,1), (5,5,5);
  - (4,1,4) sau khi đặt `IsOccupied` như trên;
  - (5,1,5) Dirt khi kho rỗng → cả hai `NoMaterial`;
  - `inv.Add(Dirt)`, (5,1,5) Dirt → cả hai `Ok`, sau đó ô là Dirt và `inv.Get(Dirt)` = 0.

**Mới — PlayMode** (`TerrainEditPlayTests.cs`, dùng `SetUp` sẵn có; mỗi test `input = go.AddComponent<TerrainEditInput>()` rồi `yield return null`).

Cột thử lấy bằng helper riêng trong file test, `FindColumn(out int x, out int z, out int h)`:
- duyệt `x = z = 16, 17, …, 60` (đường chéo);
- lấy cột đầu tiên có `h = SurfaceHeight(x, z) ≥ 2`, ô `(x,h,z)` là Dirt/Grass/Stone/Sand, và ô `(x,h-1,z)` không phải Empty/Water;
- cột đó còn phải có `CheckPlace(x,h+1,z, Dirt)` ∈ {Ok, NoMaterial} và `CheckPlace(x,h+3,z, Dirt)` = `NotAttached`;
- không có cột nào → `Assert.Fail`.

Đặt `dig = (x,h,z)`, `place = (x,h+1,z)`.
- `ToolStartsDigAndTabToggles`: `Tool` = Dig; `ToggleTool()` → Place; lần nữa → Dig.
- `DigToolShowsOutlineOnly`: `ShowAim(true, dig, place)` → `OutlineVisible` true, `GhostVisible` false. `ShowAim(false, …)` → cả hai false.
- `PlaceToolShowsGhostAtPlaceCell`:
  - `ToggleTool()`, `view.Inventory.Add(Dirt)`. Tiền điều kiện `view.Editor.CheckPlace(place…, Dirt)` = `Ok`.
  - `ShowAim(true, dig, place)` → `GhostVisible` true, `OutlineVisible` false, `GhostCell` = place, `GhostColor` = `GhostColorOf(Dirt)`.
- `GhostRedWhenOutOfMaterial`: Place, kho rỗng (tiền điều kiện `CheckPlace` = `NoMaterial`) → `GhostVisible` true, `GhostColor` = `MissingColor`.
- `GhostHiddenWhenRuleFails`: Place, kho có Dirt.
  - `ShowAim(true, dig, (x,h+3,z))` → `GhostVisible` false.
  - `view.Editor.IsOccupied = (x,y,z) => true` với `place` → false.
  - Trả `IsOccupied = null` cuối test.
- `GhostHasNoColliderAndShader`: `input.Ghost.GetComponent<Collider>()` null; `Ghost.name` = "Bản mờ"; `Shader.Find("Game/GhostBlock")` khác null; `view.Build()` rồi `yield return null` → `go.transform.Find("Bản mờ")` khác null.
- `UseToolDigsOrPlacesByTool`:
  - Dig: `UseTool(dig, place)` = `Ok`, ô dig rỗng, kho +1 vật liệu cũ.
  - `ToggleTool()`, `view.Inventory.Add(Dirt)`, rồi `UseTool(dig, dig)` (đặt lại vào ô vừa đào; ô dưới đặc nên kề khối) = `Ok`. Ô dig = Dirt.

**Regression (không sửa assert):**
- `WorldEditorTests`, `SeaFloodTests`, `PlotRulesTests` (đặt lên ô ruộng vẫn huỷ vụ, `TryPlace` vẫn gọi `RevertFarmlandBelow`), `MinimapEditTests`.
- PlayMode: `TerrainEditPlayTests` cũ (`EditInputSurvivesBuild`…), `EditOcclusionPlayTests` (`Pick` không đổi), `SimViewPlayTests`, `MinimapViewPlayTests`, `TimeDialPlayTests`.

### Lệnh executor chạy
1. Unity Editor đang mở project qua MCP:
   - `refresh_unity`, rồi `read_console` (Error): không có lỗi biên dịch / shader.
   - Kiểm `GhostBlock.shader.meta` đã sinh.
2. `run_tests` EditMode (toàn bộ) → ghi dòng tổng kết.
3. `run_tests` PlayMode (toàn bộ) → ghi dòng tổng kết. Sau đó chạy `git checkout -- client/ProjectSettings/EditorSettings.asset`.
4. `grep -n "UnityEngine" client/Assets/Game/Core/WorldEditor.cs` → không có kết quả.
5. `git status --short` → chỉ có file trong Phạm vi.

Editor tắt thì dùng `python3 tools/unity_test.py EditMode` / `PlayMode` (cùng bước dọn).

### User tự test
Mở `client/Assets/Game/Client/Scenes/SimView.unity` (rồi `TerrainView.unity`), bấm Play:
1. Mở scene: HUD ghi "Công cụ: Đào". Trỏ chuột vào địa hình: khung trắng trên khối, không có bản mờ. Chuột trái → đào đúng khối đó.
2. Bấm **Tab** → HUD "Công cụ: Đặt".
   - Khung trắng mất. Có **bản mờ** màu vật liệu đang chọn ở ô trống sát mặt khối đang trỏ: trỏ mặt trên → bản mờ nằm trên; trỏ sườn → bản mờ nằm cạnh.
   - Chuột trái → khối đặt đúng chỗ bản mờ.
3. Chọn loại kho đang 0 (phím 1–4 hoặc lăn chuột) → bản mờ **đỏ**. Bấm chuột trái → HUD ghi lý do "kho hết loại này".
4. Trỏ vào chỗ không đặt được (ô có sinh vật, nhà, kho) → không có bản mờ.
5. Chuột phải không làm gì. Tab lần nữa → về Đào.
6. Bản mờ có dễ nhìn không, có bị địa hình che lấp nhiều không (shader dùng ZTest thường). Muốn chỉnh độ mờ/màu thì ghi ở Review.

## Dừng và chuyển lại Opus khi
- Brief mơ hồ hoặc mâu thuẫn với code thật (số dòng lệch đến mức không tìm thấy đoạn code; chữ ký khác với trích dẫn).
- Cần một quyết định thuộc AGENTS.md §3.4 hoặc ngoài "Quyết định đã chốt".
- Cùng một lỗi sửa 2 lần vẫn chưa qua.
- Phải sửa file ngoài "Phạm vi", hoặc phải sửa assert của test cũ.
- Cần API Unity không có trong brief.

---
<!-- Từ đây trở xuống: EXECUTOR ghi. -->

## Kết quả
Base commit: c9d5ea8
Chạy bằng: claude-sonnet-5-5 · medium
Bắt đầu: 2026-10-21 18:26 · Kết thúc: 2026-10-21 18:33 (~0,3 giờ thực tế, chủ yếu chờ EditMode ~5 phút)
Commit: 9cecff2
File đổi: Core/WorldEditor.cs, Client/TerrainEditInput.cs, Client/Resources/Shaders/GhostBlock.shader(+.meta), Tests/Core/WorldEditorTests.cs, Tests/Client/TerrainEditPlayTests.cs. Không lệch brief.
<!-- Base commit (trước khi làm) · Chạy bằng: <model id> · <effort> (dòng OK của brief_check.py) · commit của brief · file đã đổi · ghi chú lệch so với brief (nếu có, kèm lý do). Giờ bắt đầu / kết thúc. -->

## Kết quả test
- CheckPlaceMatchesRulesWithoutSideEffects, TryPlaceAgreesWithCheckPlace (EditMode) → đạt.
- PlayMode mới (ToolStartsDigAndTabToggles, DigToolShowsOutlineOnly, PlaceToolShowsGhostAtPlaceCell, GhostRedWhenOutOfMaterial, GhostHiddenWhenRuleFails, GhostHasNoColliderAndShader, UseToolDigsOrPlacesByTool) → đạt.
- Regression (WorldEditor/SeaFlood/PlotRules/Minimap EditMode, PlayMode cũ) → đạt, không sửa assert.
- User tự test (6 mục) → chờ user.
- refresh_unity + read_console Error → 0 lỗi; GhostBlock.shader.meta đã sinh.
- run_tests EditMode → 366/366 passed (364+2).
- run_tests PlayMode → 76/76 passed (69+7); đã `git checkout -- client/ProjectSettings/EditorSettings.asset`.
- grep UnityEngine WorldEditor.cs → không có.
- git status --short → chỉ file trong Phạm vi (+ brief).
<!-- Mỗi dòng ở "Phần cần test" → đạt / không đạt / chờ user + bằng chứng (tên test, dòng output).
Mỗi lệnh ở "Lệnh executor chạy": `<lệnh>` → <kết quả thật, dòng tổng kết>. -->

## Vướng mắc
<!-- Khi trạng thái = cần Opus: câu hỏi cụ thể + các phương án (kèm đề xuất). -->

---
<!-- PLANNER ghi khi review. -->

## Review
<!-- Kết luận: đã duyệt | cần sửa (thêm bước sửa vào "Các bước", trạng thái về chờ làm). Điểm đã đối chiếu. -->
