# Brief 0037 — Sim 7e (client): hiển thị vật rơi, phím F (Thu), click nhặt, túi hạt đầu game, HUD túi hạt

> Trạng thái: chờ review
> Review: đầy đủ + user xem
> Model thực thi: Sonnet · medium
> Cập nhật: 2026-10-22 15:54

<!-- Planner (Opus) viết, Executor (Sonnet) làm theo ở PHIÊN KHÁC — executor không có lịch sử chat, không được tự quyết.
Trạng thái: chờ làm → đang làm → chờ review → đã duyệt; hoặc → cần Opus (executor dừng, hỏi ở mục Vướng mắc).
Review: nhẹ (đọc mục Kết quả) | đầy đủ (đọc cả diff) | user xem (có phần user tự kiểm bằng mắt/chơi thử) — có thể ghép "đầy đủ + user xem".
Model thực thi: BẮT BUỘC, đúng dạng `<Model> · <effort>` — Model: Opus | Sonnet | Haiku | Fable; effort: low | medium | high | xhigh | max (chọn theo AGENTS.md §10.2).
Executor kiểm tra model + effort của phiên TRƯỚC khi làm (scripts/brief_check.py); lệch hoặc không xác định được → cảnh báo user và DỪNG.
File được commit. Tự kiểm trước khi giao (AGENTS.md §10.3): executor có phải hỏi gì không? còn chỗ nào bỏ ngỏ 2 phương án? -->

## Mục tiêu
Phần client của [task contract sim 7e](../tasks/m1-sim-buoc-7e-vat-roi-tui.md) ([0028](../../design/decisions/0028-nhan-vat-tui-vat-roi-tu-trong-trot.md), Chốt). Lõi (`PlayerBag`, `DropField`, tầm 10/4) đã xong ở brief 0036 (đã duyệt).

- **Hiển thị vật rơi** (`Client/DropView.cs` mới): khối mini xoay chậm, vẽ gộp bằng instancing; chồng vừa nhặt bay về nhân vật 0,2 giây.
- **Phím F (Thu):** nhặt mọi chồng trong tầm việc tay 4 ô.
- **Click nhặt:** tia chuột trúng chồng trong 4 ô → nhặt cả chồng, không đào/đặt/xới; chồng ngoài 4 ô không chặn tia.
- **Túi hạt Cây A × 4 đầu game:** đặt cách chỗ xuất phát 3 ô theo hướng nhân vật nhìn (luật chọn ô trong `Core/StarterBag.cs` mới).
- **HUD:** dòng `Túi hạt Cây A: N`, dòng gợi ý khi ngắm `Nhặt (chuột trái): Đất × 3`, chữ trợ giúp có F.

Ước lượng ~1,5–2 ngày-người (phần còn lại của 3–5 ngày-người của 7e). Ghi giờ bắt đầu/kết thúc (theo `date`) vào mục Kết quả.

## Bối cảnh cần đọc
Đường dẫn tính từ `client/Assets/Game/`. Số dòng theo commit `fa72764`.

**Task contract:** `.ai/tasks/m1-sim-buoc-7e-vat-roi-tui.md` — mục "Đề xuất của Planner" 2, 3, 6, 7, 8 (user xác nhận 2026-10-22).

**Lõi đã có (không sửa):**
- `Core/DropField.cs`:
  - `public readonly struct ItemId` (`:9-29`): `Kind` (`ItemKind.Block` / `ItemKind.SeedBag`), `Value`, `ItemId.Block(BlockMaterial)`, `ItemId.SeedBag(int crop)`, `Material`, `Crop`, `==`.
  - `public sealed class DropStack` (`:32-44`): `Id`, `Item`, `Count`, `X`, `Y`, `Z` (ô chồng nằm; ô dưới luôn đặc).
  - `DropField` (`:50`): `IReadOnlyList<DropStack> Stacks`, `int Version`, `event Action<DropStack> Collected` (phát mỗi chồng nhặt, sau khi cộng túi, trước khi gỡ), `IReadOnlyList<DropStack> At(int x, int y, int z)`, `DropStack Find(int id)`, `DropStack Add(int x, int y, int z, ItemId item, int count)` (ô đặc → `ArgumentException`; tự hạ xuống ô ngay trên khối đặc; gộp cùng loại), `int CollectInReach(float feetX, float feetY, float feetZ, PlayerBag bag)`, `bool TryCollect(int stackId, float feetX, float feetY, float feetZ, PlayerBag bag)`.
- `Core/PlayerBag.cs`: `int Get(BlockMaterial)`, `void Add(BlockMaterial, int amount = 1)`, `int GetSeeds(int crop)`.
- `Core/PlayerRules.cs`: `HandReach = 4f` (`:14`), `BodyCenterHeight = 1f` (`:16`), `static bool InHandReach(float feetX, float feetY, float feetZ, int x, int y, int z)` (`:24`).
- `Core/WorldEditor.cs`: `public readonly DropField Drops;`, `public readonly PlayerBag Bag;`, `public static bool IsSolidAt(WorldData world, int x, int y, int z)` (`:171-179`: `y < 0` → đặc; ngoài map → không; ô khác Empty/Water → đặc).
- `Core/WorldData.cs`: `Size` (`X`, `Y`, `Z`, `InBounds(x, y, z)`), `BlockMaterial GetMaterial(int x, int y, int z)` (`:81`).
- Cây A = `crops[0]` trong `design/data/sim_m1.json` (`"id": "A"`, dòng 26). Hạt theo loại cây dùng chỉ số trong `crops`.

**Client:**
- `Client/TerrainEditInput.cs` (toàn file 318 dòng):
  - summary lớp `:11-18`; `MissingColor` `:40-41`;
  - `Awake` `:52-57`;
  - `Update` `:59-114`: phím `:61-69`; chặn con trỏ trên UI `:72-77`; lăn chuột `:78-87`; camera/ray `:89-98`; `var player = PlayerCharacter.Active;` `:109`; chặn khi bay `:111`; click `:113`;
  - `public EditResult UseTool(Vector3Int digCell, Vector3Int placeCell)` `:159-188`, dòng kết quả đào `:182`;
  - `public bool Pick(Ray ray, out Vector3Int digCell, out Vector3Int placeCell)` `:193-201` (dùng `Physics.Raycast(ray, out var hit, maxDistance)`);
  - `static string Name(BlockMaterial m)` `:203-207` (Dirt → "đất", Grass → "cỏ", Stone → "đá", Sand → "cát", khác → `m.ToString()`);
  - `Explain` `:209-219`; `HelpText` `:221-229`; `OnGUI` `:231-253`;
  - `public static Color GhostColorOf(BlockMaterial m)` `:120-124` (TopColor, alpha 0,5).
- `Client/PlayerCharacter.cs`:
  - `public static PlayerCharacter Active` `:22`; `public Vector3 FeetLocal` `:29` (toạ độ terrain);
  - `AddBlock` `:80-93` (mẫu tạo material từ `Resources.Load<UnityEngine.Material>("Materials/Landmark")`, dự phòng `GraphicsSettings.currentRenderPipeline.defaultMaterial`, `SetColor("_BaseColor", …)`);
  - `public void Place(Vector3 feetWorld, float yawDeg)` `:112-122`;
  - `Update` `:149-201`, móc `IsPlayerAt` `:152-153`.
- Chỗ gọi `player.Place(…, 0f)` lúc xuất phát: `Client/DemoPlain.cs:32` (scene TerrainView) và `Client/SimView.cs:94` (scene SimView). Cả hai gọi trong `afterGenerate`, **trước** khi `TerrainChunkView.Build` tạo `Editor` (`Client/TerrainChunkView.cs:159-165`) — vì vậy túi hạt phải đặt sau, trong `PlayerCharacter.Update`.
- Yaw 0 = nhân vật nhìn +z (mũi ở +z, `PlayerCharacter.cs:77`).
- Dấu mốc, nhà, kho có collider; nhân vật và hình của nó ở layer "Ignore Raycast" (`PlayerCharacter.cs:48`, `:85`) nên `Physics.Raycast` mặc định không trúng nhân vật.
- `TerrainEditInput` có trong scene TerrainView, SimView, Bench, SimBench và được `AddComponent` trong nhiều test PlayMode → `DropView` sẽ do `TerrainEditInput.Awake` tự thêm (không sửa scene).

**Mẫu test PlayMode có nhân vật:** `Tests/Client/PlayerCharacterPlayTests.cs:13-62` (SetUp/TearDown: camera + `TerrainChunkView` MapsV3 streaming + `DemoPlain` + `TerrainEditInput` + `PlayerCharacter`, `readKeyboard = false`; `A = (plain.X0 + 6, plain.Ground, plain.Z0 + 6)`, `G = plain.Ground`), `Teleport` `:78-86`, `C(...)` `:88`. Đồng bằng phẳng: ô y = G đặc, y = G + 1 rỗng.

**API Unity mới** (Planner đã tra 6000.4; không dùng API nào khác ngoài danh sách này và API đã có trong project):
- `Graphics.RenderMeshInstanced<T>(in RenderParams rparams, Mesh mesh, int submeshIndex, List<T> instanceData, int instanceCount = -1, int startInstance = 0)` — [docs](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/Graphics.RenderMeshInstanced.html). Tham số đầu là `in` (đã kiểm bằng reflection trong Editor 6000.4.8f1) → gọi `Graphics.RenderMeshInstanced(rp, cube, 0, list, n, start)` không cần `ref`/`in`. Material phải `enableInstancing = true`, không thì ném `InvalidOperationException`. Mỗi lần gọi tối đa 1023 thể hiện, với `Matrix4x4` (2 ma trận/thể hiện) là 511 → chia lô 500.
- `RenderParams` — [docs](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/RenderParams.html): constructor `new RenderParams(Material)`, field `worldBounds`, `shadowCastingMode`, `receiveShadows`.
- `Material.enableInstancing` — [docs](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/Material-enableInstancing.html).
- `Matrix4x4.TRS(Vector3 pos, Quaternion q, Vector3 s)` — [docs](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/Matrix4x4.TRS.html).

## Quyết định đã chốt (không được đổi)
**Task contract 7e (user xác nhận 2026-10-22):**
1. **F (nhấn một lần) = Thu:** mọi chồng có tâm ô trong 4 ô vào túi ngay (`DropField.CollectInReach`). Hình ảnh: vật bay về nhân vật 0,2 giây rồi biến mất (chỉ hiển thị, túi đã cộng ngay).
2. **Click nhặt:** với mọi công cụ, tia chuột trúng một chồng trong 4 ô → nhặt cả chồng (`TryCollect`), không đào/đặt/xới. Chồng ngoài 4 ô không chặn tia → đào/đặt khối phía sau như thường.
3. **Túi hạt đầu game:** vật rơi `ItemId.SeedBag(0)` × 4, cách chỗ xuất phát 3 ô theo hướng nhìn lúc đầu, trên mặt đất; ô đó là nước hoặc không có mặt đất → ô gần nhất đứng được trong vùng 5 × 5 quanh chỗ xuất phát. Mọi scene có nhân vật (TerrainView, SimView).
4. **Hiển thị:** khối mini cạnh 0,3 ô, màu như khối, xoay chậm; túi hạt là hộp nhỏ màu riêng; một chồng một hình; số lượng hiện trong dòng gợi ý khi ngắm (`Đất × 3`); vẽ gộp (instancing), không GameObject cho mỗi chồng.
5. **HUD:** thanh túi giữ như cũ; thêm dòng `Túi hạt Cây A: N` khi N > 0.

**Planner chốt khi viết brief:**

6. **Không có nhân vật** (`PlayerCharacter.Active == null`) → F và click nhặt không làm gì (không có thân để đo tầm). **Camera bay F3** (có nhân vật, `FlyMode`) → vẫn nhặt được, đo từ thân nhân vật (giống công cụ ở brief 0035).
7. **Hộp chọn khi click:** khối vuông cạnh 0,5, đáy ở mặt ô (`Y`), giữa ô theo x/z — lớn hơn hình 0,3 cho dễ click. Bị che bởi collider đầu tiên mà tia trúng (địa hình, nhà, kho, dấu mốc). Nhiều chồng trúng → chồng có khoảng cách vào hộp nhỏ nhất; bằng nhau → chồng đứng trước trong `Stacks`. Luật này ở Core (`DropPick`) để test EditMode.
8. **Ô "đứng được" cho túi hạt** (cột (cx, cz)):
   - trong map;
   - `top` = ô cao nhất của cột có vật liệu khác `Empty`; không có → không được;
   - ô `top` đặc (`WorldEditor.IsSolidAt`; nước → không được); `top + 1 < Size.Y`;
   - ô đặt = (cx, top + 1, cz); `|top + 1 − floor(feetY)| ≤ 1`;
   - ô đặt trong tầm việc tay của chỗ xuất phát (`PlayerRules.InHandReach`).
   
   Thứ tự thử: ô ưu tiên (sx + 3·dirX, sz + 3·dirZ) trước; rồi 24 cột trong 5 × 5 quanh (sx, sz) (bỏ cột xuất phát và ô ưu tiên nếu trùng) xếp theo dx² + dz² tăng, rồi dz tăng, rồi dx tăng. Không ô nào được → không đặt túi hạt, log cảnh báo. `sx = floor(feetX)`, `sz = floor(feetZ)`.
9. **Hướng nhìn → (dirX, dirZ)** = `(RoundToInt(sin yaw), RoundToInt(cos yaw))`; yaw 0 → (0, 1).
10. **Túi hạt chỉ đặt một lần cho mỗi `WorldEditor`** (đầu game), tại chỗ `Place` gần nhất trước khi có `Editor`. `Place` gọi lại sau đó (vd teleport trong test) không đặt thêm. `PlayerCharacter.giveStarterBag` (mặc định `true`) để tắt khi cần.
11. **Hình:** khối dùng mesh lập phương có sẵn (`GameObject.CreatePrimitive(PrimitiveType.Cube)` lấy `sharedMesh` rồi huỷ GameObject); material sao từ `Resources/Materials/Landmark` (URP Lit) như `PlayerCharacter.AddBlock`, mỗi loại vật phẩm một material, `enableInstancing = true`. Màu khối = `TerrainEditInput.GhostColorOf(m)` với alpha 1; túi hạt màu `(0.55, 0.40, 0.20)` (như ô hạt giống ở `SimView.cs:416`), cỡ 0,3 × 0,4 × 0,3. Xoay quanh trục y 45°/giây, lệch pha `Id × 37°`. Không đổ bóng. Bay: nội suy thẳng từ tâm chồng tới tâm thân (chân + 1), thu nhỏ còn 50 % ở cuối.
12. **Dòng chữ** (chép nguyên văn ở bước 4): kết quả Thu, kết quả nhặt, gợi ý khi ngắm, trợ giúp F, lý do `NoMaterial`/`Occupied` mới, kết quả đào "rơi 1 …".
13. **Tài liệu** (`simulation-tech` bước 7e, roadmap giờ thực tế) Planner ghi khi review, executor không sửa `design/`.

## Phạm vi
- Được sửa/tạo:
  - `Core/DropPick.cs`, `Core/StarterBag.cs` (mới, + `.meta` do Unity tạo);
  - `Client/DropView.cs` (mới, + `.meta`);
  - `Client/TerrainEditInput.cs`, `Client/PlayerCharacter.cs` — chỉ các chỗ ở bước 4, 5;
  - `Tests/Core/DropPickTests.cs`, `Tests/Core/StarterBagTests.cs`, `Tests/Client/DropPlayTests.cs` (mới, + `.meta`);
  - mục Kết quả của brief này.
- Không được sửa:
  - `Core/DropField.cs`, `Core/PlayerBag.cs`, `Core/WorldEditor.cs`, `Core/PlayerRules.cs`, `Core/Sim/**`;
  - `Client/TerrainChunkView.cs`, `Client/SimView.cs`, `Client/DemoPlain.cs`, `Client/TerrainBench.cs`;
  - scene `.unity`, `design/`, task contract, test cũ;
  - file khác.

## Các bước
1. **`Core/DropPick.cs`** (mới, namespace `Game.Core`, chỉ `using System; using System.Collections.Generic;`):
   ```csharp
   /// <summary>Chọn chồng vật rơi bằng tia (task contract 7e, brief 0037): hộp cạnh BoxSize, đáy ở mặt ô, giữa ô theo x/z; chỉ chồng trong tầm việc tay.</summary>
   public static class DropPick
   {
       public const float BoxSize = 0.5f;

       /// <summary>
       /// Chồng có hộp bị tia (o + t·d, t ≥ 0) trúng với t ≤ maxDistance và tâm ô trong tầm việc tay của chân (feet); t nhỏ nhất thắng,
       /// bằng nhau → chồng đứng trước trong stacks. Không có → null, distance = 0. Toạ độ ô (terrain).
       /// </summary>
       public static DropStack Pick(IReadOnlyList<DropStack> stacks, float ox, float oy, float oz, float dx, float dy, float dz,
           float maxDistance, float feetX, float feetY, float feetZ, out float distance)

       /// <summary>Tia cắt hộp AABB (slab); gốc trong hộp → t = 0.</summary>
       public static bool RayBox(float ox, float oy, float oz, float dx, float dy, float dz,
           float minX, float minY, float minZ, float maxX, float maxY, float maxZ, out float t)
   }
   ```
   - `stacks == null` → `ArgumentNullException`.
   - Hộp của chồng `s`: min = `(s.X + 0.5 − 0.25, s.Y, s.Z + 0.5 − 0.25)`, max = `(s.X + 0.5 + 0.25, s.Y + 0.5, s.Z + 0.5 + 0.25)`.
   - `RayBox` theo slab: `tmin = 0`, `tmax = +∞`; mỗi trục: `|d| < 1e-8f` → trúng chỉ khi `min ≤ o ≤ max`; không thì `t1 = (min − o)/d`, `t2 = (max − o)/d`, đổi chỗ nếu `t1 > t2`, `tmin = max(tmin, t1)`, `tmax = min(tmax, t2)`; `tmin > tmax` → false. Trúng → `t = tmin`.
   - Chọn: chỉ thay ứng viên khi `t < bestT` (dấu `<`, giữ chồng trước khi bằng).
   - Xong khi: biên dịch được, không `using UnityEngine`.

2. **`Core/StarterBag.cs`** (mới, namespace `Game.Core`, chỉ `using System; using System.Collections.Generic;`):
   ```csharp
   /// <summary>Túi hạt đầu game (0028, task contract 7e): vật rơi túi hạt Cây A × 4 cách chỗ xuất phát 3 ô theo hướng nhìn, trên mặt đất trong tầm việc tay.</summary>
   public static class StarterBag
   {
       /// <summary>Cây A = SimConfig.crops[0] (design/data/sim_m1.json).</summary>
       public const int Crop = 0;
       public const int Seeds = 4;
       public const int Distance = 3;
       /// <summary>Nửa cạnh vùng dự phòng 5 × 5.</summary>
       public const int SearchRadius = 2;

       /// <summary>Ô đặt túi hạt theo QĐ 8 brief 0037; false nếu không có ô nào.</summary>
       public static bool TryFindSpot(WorldData world, float feetX, float feetY, float feetZ, int dirX, int dirZ, out int x, out int y, out int z)

       /// <summary>Đặt vật rơi SeedBag(Crop) × Seeds vào ô của TryFindSpot; trả chồng, hoặc null nếu không có ô.</summary>
       public static DropStack Place(WorldEditor editor, float feetX, float feetY, float feetZ, int dirX, int dirZ)
   }
   ```
   - `world`/`editor` null → `ArgumentNullException`.
   - `TryFindSpot` làm đúng QĐ 8 (ô đứng được + thứ tự thử). Gợi ý: hàm riêng `static bool Standable(WorldData w, int cx, int cz, float feetX, float feetY, float feetZ, out int y)`; `floor` dùng `(int)Math.Floor(...)`.
   - `Place`: `TryFindSpot(editor.World, …)` → `editor.Drops.Add(x, y, z, ItemId.SeedBag(Crop), Seeds)`.
   - Xong khi: biên dịch được, không `using UnityEngine`.

3. **`Client/DropView.cs`** (mới). Viết theo khung sau (được thêm `using`, sửa lỗi biên dịch nhỏ; không đổi hành vi):
   ```csharp
   using System.Collections.Generic;
   using Game.Core;
   using UnityEngine;
   using UnityEngine.Rendering;

   namespace Game.Client
   {
       /// <summary>
       /// Vẽ vật rơi (task contract sim 7e, brief 0037): mỗi chồng một khối mini cạnh 0,3 ô màu như khối (túi hạt: hộp 0,3 × 0,4 × 0,3 màu nâu), xoay chậm;
       /// vẽ gộp theo loại bằng Graphics.RenderMeshInstanced, không tạo GameObject cho từng chồng. Chồng vừa nhặt bay về nhân vật 0,2 giây (chỉ hiển thị).
       /// TerrainEditInput tự thêm component này.
       /// </summary>
       [RequireComponent(typeof(TerrainChunkView))]
       public class DropView : MonoBehaviour
       {
           public const float BlockSize = 0.3f;
           public static readonly Vector3 SeedBagSize = new Vector3(0.3f, 0.4f, 0.3f);
           public static readonly Color SeedBagColor = new Color(0.55f, 0.40f, 0.20f);
           public const float FlySeconds = 0.2f;
           public const float SpinDegreesPerSecond = 45f;
           /// <summary>Số thể hiện mỗi lần vẽ: ≤ 511 khi mỗi thể hiện là Matrix4x4 (docs RenderMeshInstanced).</summary>
           const int Batch = 500;

           struct Flying { public ItemId Item; public Vector3 From; public float Start; }

           TerrainChunkView view;
           WorldEditor subscribed;
           Mesh cube;
           UnityEngine.Material baseMat;
           readonly Dictionary<ItemId, UnityEngine.Material> mats = new Dictionary<ItemId, UnityEngine.Material>();
           readonly Dictionary<ItemId, List<Matrix4x4>> groups = new Dictionary<ItemId, List<Matrix4x4>>();
           readonly List<Flying> flying = new List<Flying>();

           /// <summary>Số thể hiện đã vẽ ở khung gần nhất (chồng + vật đang bay).</summary>
           public int LastInstanceCount { get; private set; }
           public int FlyingCount => flying.Count;

           public static Vector3 SizeOf(ItemId item) => item.Kind == ItemKind.SeedBag ? SeedBagSize : Vector3.one * BlockSize;
           /// <summary>Tâm hình của chồng (toạ độ terrain): giữa ô theo x/z, đáy ở mặt ô.</summary>
           public static Vector3 LocalCenter(DropStack s) => new Vector3(s.X + 0.5f, s.Y + SizeOf(s.Item).y * 0.5f, s.Z + 0.5f);
           public static Color ColorOf(ItemId item)
           {
               if (item.Kind == ItemKind.SeedBag) return SeedBagColor;
               var c = TerrainEditInput.GhostColorOf(item.Material);
               c.a = 1f;
               return c;
           }

           void Awake()
           {
               view = GetComponent<TerrainChunkView>();
               var tmp = GameObject.CreatePrimitive(PrimitiveType.Cube);
               tmp.SetActive(false);
               cube = tmp.GetComponent<MeshFilter>().sharedMesh;
               Destroy(tmp);
               baseMat = Resources.Load<UnityEngine.Material>("Materials/Landmark");
               if (baseMat == null) baseMat = GraphicsSettings.currentRenderPipeline.defaultMaterial;
           }

           void OnDestroy()
           {
               if (subscribed != null) subscribed.Drops.Collected -= OnCollected;
               foreach (var m in mats.Values) Destroy(m);
           }

           public UnityEngine.Material MaterialFor(ItemId item)
           {
               if (!mats.TryGetValue(item, out var m))
               {
                   m = new UnityEngine.Material(baseMat) { enableInstancing = true };
                   m.SetColor("_BaseColor", ColorOf(item));
                   mats[item] = m;
               }
               return m;
           }

           void OnCollected(DropStack s) => flying.Add(new Flying { Item = s.Item, From = LocalCenter(s), Start = Time.time });

           void LateUpdate()
           {
               LastInstanceCount = 0;
               var editor = view.Editor;
               if (editor != subscribed)
               {
                   if (subscribed != null) subscribed.Drops.Collected -= OnCollected;
                   subscribed = editor;
                   flying.Clear();
                   if (editor != null) editor.Drops.Collected += OnCollected;
               }
               if (editor == null) return;

               foreach (var list in groups.Values) list.Clear();
               var toWorld = view.transform.localToWorldMatrix;
               float now = Time.time;
               foreach (var s in editor.Drops.Stacks)
                   AddInstance(s.Item, toWorld * Matrix4x4.TRS(LocalCenter(s), Quaternion.Euler(0f, now * SpinDegreesPerSecond + s.Id * 37f, 0f), SizeOf(s.Item)));
               var player = PlayerCharacter.Active;
               for (int i = flying.Count - 1; i >= 0; i--)
               {
                   var f = flying[i];
                   float k = (now - f.Start) / FlySeconds;
                   if (k >= 1f) { flying.RemoveAt(i); continue; }
                   var to = player != null ? player.FeetLocal + Vector3.up * PlayerRules.BodyCenterHeight : f.From;
                   AddInstance(f.Item, toWorld * Matrix4x4.TRS(Vector3.Lerp(f.From, to, k), Quaternion.identity, SizeOf(f.Item) * (1f - 0.5f * k)));
               }

               var size = new Vector3(view.World.Size.X, view.World.Size.Y, view.World.Size.Z);
               var bounds = new Bounds(view.transform.TransformPoint(size * 0.5f), size + Vector3.one * 2f);
               foreach (var kv in groups)
               {
                   var list = kv.Value;
                   if (list.Count == 0) continue;
                   var rp = new RenderParams(MaterialFor(kv.Key)) { worldBounds = bounds, shadowCastingMode = ShadowCastingMode.Off, receiveShadows = true };
                   for (int start = 0; start < list.Count; start += Batch)
                       Graphics.RenderMeshInstanced(rp, cube, 0, list, Mathf.Min(Batch, list.Count - start), start);
                   LastInstanceCount += list.Count;
               }
           }

           void AddInstance(ItemId item, Matrix4x4 m)
           {
               if (!groups.TryGetValue(item, out var list)) groups[item] = list = new List<Matrix4x4>();
               list.Add(m);
           }
       }
   }
   ```
   - Xong khi: biên dịch không lỗi, không cảnh báo mới trong Console.

4. **`Client/TerrainEditInput.cs`:**
   - **Summary lớp** (`:14`): "đỏ = kho hết" → "đỏ = túi hết"; "kho vật liệu" → "túi đồ". Thêm một dòng ngay sau `:16`:
     `/// Sim 7e (brief 0037): F = Thu (nhặt mọi vật rơi trong 4 ô); tia chuột trúng vật rơi trong 4 ô → chuột trái nhặt cả chồng thay cho công cụ; vật rơi ngoài 4 ô không chặn tia.`
   - **`MissingColor`** summary (`:40`): "kho hết" → "túi hết".
   - **Thêm thành viên** (sau `GhostColor`, `:48`):
     ```csharp
     /// <summary>Vẽ vật rơi (brief 0037); thêm trong Awake nếu chưa có.</summary>
     public DropView DropView { get; private set; }
     /// <summary>Chồng vật rơi đang ngắm (trong tầm việc tay); null nếu không có.</summary>
     public DropStack AimedDrop { get; private set; }
     ```
   - **`Awake`** (`:52-57`) thêm cuối: `DropView = GetComponent<DropView>(); if (DropView == null) DropView = gameObject.AddComponent<DropView>();`
   - **`Update`:**
     - dòng đầu thân hàm: `AimedDrop = null;`
     - trong khối `if (kb != null)` (`:62-69`) thêm: `if (kb.fKey.wasPressedThisFrame) CollectNearby();`
     - thay đoạn `:93-98` (từ `var ray = …` tới hết khối `if (!Pick(...)) { … }`) bằng:
       ```csharp
       var ray = cam.ScreenPointToRay(mouse.position.ReadValue());
       var player = PlayerCharacter.Active;
       // Vật rơi trong tầm việc tay 4 ô dưới con trỏ → chuột trái nhặt cả chồng, không đào/đặt/xới (task contract 7e); ngoài 4 ô không chặn tia.
       AimedDrop = PickDrop(ray);
       if (AimedDrop != null)
       {
           ShowAim(false, default, default);
           if (player.FlyMode && DebugFlyCamera.IsMoveKeyHeld(kb)) return;
           if (mouse.leftButton.wasPressedThisFrame) CollectDrop(AimedDrop);
           return;
       }
       if (!Pick(ray, out var digCell, out var placeCell))
       {
           ShowAim(false, default, default);
           return;
       }
       ```
     - xoá dòng `var player = PlayerCharacter.Active;` ở `:109` (đã khai báo ở trên). Phần còn lại giữ nguyên.
   - **Thêm phương thức** (sau `Pick`, `:201`):
     ```csharp
     /// <summary>Chồng vật rơi gần nhất mà tia trúng, trong tầm việc tay 4 ô, không bị collider nào (địa hình, nhà, dấu mốc) che; không có nhân vật → null.</summary>
     public DropStack PickDrop(Ray ray)
     {
         var player = PlayerCharacter.Active;
         if (player == null || view.Editor == null) return null;
         float limit = Physics.Raycast(ray, out var hit, maxDistance) ? hit.distance : maxDistance;
         var o = view.transform.InverseTransformPoint(ray.origin);
         var d = view.transform.InverseTransformDirection(ray.direction);
         var f = player.FeetLocal;
         return DropPick.Pick(view.Editor.Drops.Stacks, o.x, o.y, o.z, d.x, d.y, d.z, limit, f.x, f.y, f.z, out _);
     }

     /// <summary>Phím F (Thu): nhặt mọi chồng trong 4 ô; ghi dòng kết quả. Trả số chồng đã nhặt; không có nhân vật → 0, không đổi dòng kết quả.</summary>
     public int CollectNearby()
     {
         var player = PlayerCharacter.Active;
         if (player == null || view.Editor == null) return 0;
         var f = player.FeetLocal;
         int n = view.Editor.Drops.CollectInReach(f.x, f.y, f.z, view.Bag);
         lastResult = n > 0 ? $"Thu: nhặt {n} chồng vật rơi" : "Thu: không có vật rơi trong 4 ô";
         return n;
     }

     /// <summary>Click nhặt một chồng (phải trong tầm việc tay); ghi dòng kết quả khi được.</summary>
     public bool CollectDrop(DropStack s)
     {
         var player = PlayerCharacter.Active;
         if (player == null || view.Editor == null || s == null) return false;
         var label = DropLabel(s);
         var f = player.FeetLocal;
         if (!view.Editor.Drops.TryCollect(s.Id, f.x, f.y, f.z, view.Bag)) return false;
         lastResult = $"Nhặt {label}";
         return true;
     }

     /// <summary>Tên vật phẩm: khối → Đất / Cỏ / Đá / Cát (khác: tên enum); túi hạt loại 0 → "Túi hạt Cây A", loại khác → "Túi hạt loại N".</summary>
     public static string ItemLabel(ItemId item)
     {
         if (item.Kind == ItemKind.SeedBag)
             return item.Crop == StarterBag.Crop ? "Túi hạt Cây A" : $"Túi hạt loại {item.Crop}";
         var n = Name(item.Material);
         return char.ToUpper(n[0]) + n.Substring(1);
     }

     /// <summary>vd "Đất × 3".</summary>
     public static string DropLabel(DropStack s) => $"{ItemLabel(s.Item)} × {s.Count}";
     ```
   - **`UseTool`** `:182`: `$"Đào {digCell}: +1 {Name(m)}"` → `$"Đào {digCell}: rơi 1 {Name(m == BlockMaterial.Farmland ? BlockMaterial.Dirt : m)}"`.
   - **`Explain`** (`:211`, `:216`):
     - `NoMaterial` → `"túi hết loại này (nhặt vật rơi bằng F hoặc chuột trái, hoặc chọn loại khác bằng 1–4 / Alt + lăn chuột)"`;
     - `Occupied` → `"ô đang bị chiếm, hoặc vật rơi trong ô không còn chỗ đẩy lên"`.
   - **`HelpText`** (`:227`, `:228`): chèn `" · F: thu vật rơi (trong 4 ô)"` ngay sau `"(trong 10 ô quanh nhân vật)"` ở cả hai dòng. Dòng `:225` (không có nhân vật) giữ nguyên.
   - **`OnGUI` → tách `HudText()`:**
     ```csharp
     /// <summary>Chữ HUD góc trái (tách khỏi OnGUI để test); "" nếu chưa có túi.</summary>
     public string HudText()
     ```
     - thân = phần dựng `text` ở `:235-248` hiện tại (đầu hàm: `if (view.Bag == null) return "";`), thêm hai chỗ:
       - ngay sau vòng `for` của dòng `"Túi: "` (`:242-245`):
         ```csharp
         int seeds = view.Bag.GetSeeds(StarterBag.Crop);
         if (seeds > 0) text += $"\nTúi hạt Cây A: {seeds}";
         if (AimedDrop != null) text += $"\nNhặt (chuột trái): {DropLabel(AimedDrop)}";
         ```
       - rồi mới tới `text += $"\n{lastResult}";` như cũ.
     - `OnGUI`: `if (view.Bag == null || MinimapView.IsAnyOpen) return; var text = HudText();` rồi phần `scale`/`GUI.Label` như cũ; chiều cao `Rect` `140` → `200`.
   - Xong khi: biên dịch được; grep `kho hết` trong file → không còn.

5. **`Client/PlayerCharacter.cs`:**
   - Field (sau `JumpInput`, `:20`):
     ```csharp
     /// <summary>Đặt túi hạt Cây A × 4 gần chỗ xuất phát khi có Editor (sim 7e, brief 0037).</summary>
     public bool giveStarterBag = true;
     /// <summary>Chồng túi hạt đầu game đã đặt; null nếu chưa đặt hoặc không có chỗ.</summary>
     public DropStack StarterStack { get; private set; }
     Vector3 spawnFeetLocal;
     float spawnYaw;
     bool hasSpawn;
     WorldEditor starterGivenFor;
     ```
   - `Place` (`:112-122`) thêm cuối: `if (terrain != null) { spawnFeetLocal = terrain.transform.InverseTransformPoint(feetWorld); spawnYaw = yawDeg; hasSpawn = true; }`
   - `Update`: ngay sau móc `IsPlayerAt` (`:152-153`) thêm:
     ```csharp
     if (giveStarterBag && hasSpawn && terrain.Editor != null && starterGivenFor != terrain.Editor)
     {
         starterGivenFor = terrain.Editor;
         float r = spawnYaw * Mathf.Deg2Rad;
         StarterStack = StarterBag.Place(terrain.Editor, spawnFeetLocal.x, spawnFeetLocal.y, spawnFeetLocal.z, Mathf.RoundToInt(Mathf.Sin(r)), Mathf.RoundToInt(Mathf.Cos(r)));
         if (StarterStack != null) Debug.Log($"STARTERBAG túi hạt Cây A × {StarterBag.Seeds} ở ({StarterStack.X}, {StarterStack.Y}, {StarterStack.Z})");
         else Debug.LogWarning("STARTERBAG không tìm được chỗ đặt túi hạt quanh chỗ xuất phát");
     }
     ```
   - Summary lớp (`:10-11`) thêm: "Đầu game đặt túi hạt Cây A × 4 gần chỗ xuất phát (brief 0037)."
   - Xong khi: biên dịch được.

6. **`Tests/Core/DropPickTests.cs`** (mới, namespace `Game.Core.Tests`). SetUp: world 32 × 16 × 32, y = 0 `Bedrock` mọi cột; `field = new DropField(world)`; `dirt = field.Add(5, 1, 5, ItemId.Block(BlockMaterial.Dirt), 1)`; `stone = field.Add(8, 1, 5, ItemId.Block(BlockMaterial.Stone), 1)`. Test:
   1. `RayFromAboveHitsStack`: o (5.5, 5, 5.5), d (0, −1, 0), max 100, chân (5.5, 1, 5.5) → `dirt`, distance 3,5 (±1e-4).
   2. `MaxDistanceOccludes`: như 1, max 3,4 → null.
   3. `OutOfHandReachIgnored`: như 1, chân (5.5, 1, 10.5) → null.
   4. `NearestAlongRayWins`: o (0, 1.25, 5.5), d (1, 0, 0), max 100, chân (6.5, 1, 5.5) → `dirt` (t = 5,25); o (20, 1.25, 5.5), d (−1, 0, 0) cùng chân → `stone` (t = 11,25).
   5. `MissBesideBox`: o (5.5, 5, 5.8), d (0, −1, 0), chân (5.5, 1, 5.5) → null.
   6. `OriginInsideBox`: o (5.5, 1.2, 5.5), d (1, 0, 0), chân (5.5, 1, 5.5) → `dirt`, distance 0.
   7. `TieKeepsEarlierStack`: thêm `field.Add(5, 1, 5, ItemId.Block(BlockMaterial.Sand), 1)` (cùng ô với `dirt`, hộp trùng) → test 1 vẫn trả `dirt`.
   8. `NullStacksThrows`: `Pick(null, …)` → `ArgumentNullException`.
   - Xong khi: 8 test qua.

7. **`Tests/Core/StarterBagTests.cs`** (mới, namespace `Game.Core.Tests`). SetUp: world 32 × 16 × 32, y = 0 `Bedrock`, y = 1 `Dirt` mọi cột; `bag = new PlayerBag()`; `editor = new WorldEditor(world, bag, 2)`. Chân mặc định (10.5, 2, 10.5). Test:
   1. `SpotThreeAheadOnFlatGround`: dir (0, 1) → true, (10, 2, 13). `Place` → chồng `ItemId.SeedBag(0)` × 4 ở (10, 2, 13); `editor.Drops.CollectInReach(10.5f, 2f, 10.5f, bag)` → 1; `bag.GetSeeds(0) == 4`.
   2. `DirectionPlusX`: dir (1, 0) → (13, 2, 10).
   3. `WaterAheadFallsBackToNearest`: `world.Set(10, 1, 13, Cell.Make(BlockMaterial.Water, 7))`; dir (0, 1) → (10, 2, 9).
   4. `TallColumnAheadSkipped`: Stone ở (10, 2..4, 13); dir (0, 1) → (10, 2, 9).
   5. `HoleOneDownAccepted`: `world.Set(10, 1, 13, Cell.Make(BlockMaterial.Empty))`; dir (0, 1) → (10, 1, 13).
   6. `NoSpotReturnsFalse`: Water (`Cell.Make(BlockMaterial.Water, 7)`) ở y = 1 cho x 6..14, z 6..16; dir (0, 1) → false; `Place` → null; `editor.Drops.Stacks` rỗng.
   7. `OutOfBoundsSkipped`: chân (0.5, 2, 0.5), dir (0, −1) → (1, 2, 0).
   - `Cell.Make(BlockMaterial material, int waterLevel = 0)` có ở `Core/Cell.cs:34` → `Cell.Make(BlockMaterial.Empty)` là ô rỗng.
   - Xong khi: 7 test qua.

8. **`Tests/Client/DropPlayTests.cs`** (mới, namespace `Game.Client.Tests`). SetUp/TearDown/`Run`/`Teleport`/`C` chép từ `PlayerCharacterPlayTests.cs:13-88`, giữ thêm `TerrainEditInput input` (= kết quả `AddComponent<TerrainEditInput>()`) và `DropView dv` (= `input.DropView`). Mọi ô dùng toạ độ terrain. Test:
   1. `StarterBagNearSpawnAndHud`:
      - `player.StarterStack` khác null, `Item == ItemId.SeedBag(0)`, `Count == 4`, có trong `view.Editor.Drops.Stacks`;
      - `input.HudText()` không chứa `"Túi hạt Cây A"`;
      - `input.CollectNearby()` ≥ 1; `view.Bag.GetSeeds(0) == 4`; `input.HudText()` chứa `"Túi hạt Cây A: 4"`.
   2. `CollectNearbyTakesOnlyWithin4`:
      - `Teleport(new Vector3(A.x + 0.5f, G + 1, A.z + 0.5f))`; ghi `dirt0 = view.Bag.Get(Dirt)`, `stone0 = view.Bag.Get(Stone)`;
      - `near = Drops.Add(A.x + 2, G + 1, A.z, Block(Dirt), 2)`, `far = Drops.Add(A.x + 6, G + 1, A.z, Block(Stone), 1)`;
      - `input.CollectNearby() == 1`; túi Dirt = `dirt0 + 2`, Stone = `stone0`; `Drops.Find(far.Id)` khác null, `Drops.Find(near.Id)` null;
      - `dv.FlyingCount == 1`; `Run(Vector2.zero, false, 0.3f)` → `dv.FlyingCount == 0`.
   3. `ClickPicksOnlyInReachAndFarDoesNotBlockDig`:
      - Teleport như test 2; thêm `near`/`far` như test 2;
      - `rayNear = new Ray(view.transform.TransformPoint(new Vector3(A.x + 2.5f, G + 6f, A.z + 0.5f)), Vector3.down)` → `input.PickDrop(rayNear)?.Id == near.Id`;
      - `rayFar` tương tự ở `A.x + 6.5f` → `input.PickDrop(rayFar)` null; `input.Pick(rayFar, out var dig, out _)` true và `dig == C(A.x + 6, G, A.z)`;
      - `input.CollectDrop(near)` true, túi Dirt = `dirt0 + 2`; `input.CollectDrop(far)` false, `far` còn;
      - ô `(A.x + 2, G, A.z)` vẫn đặc (`WorldEditor.IsSolidAt(view.World, …)`).
   4. `DigDropsVisibleThenCollectAndPlace`:
      - Teleport như test 2; `c = C(A.x + 2, G, A.z)`; `m = view.World.GetMaterial(c.x, c.y, c.z)`; `b0 = view.Bag.Get(m)`;
      - `input.UseTool(c, c + Vector3Int.up) == EditResult.Ok` (công cụ mặc định Đào);
      - `view.Editor.Drops.At(c.x, c.y, c.z)` có 1 chồng `ItemId.Block(m)` × 1; `view.Bag.Get(m) == b0`;
      - `yield return null;` → `dv.LastInstanceCount == view.Editor.Drops.Stacks.Count`;
      - `input.CollectNearby() == 1`; `view.Bag.Get(m) == b0 + 1`;
      - `view.Place(c, m) == EditResult.Ok`; `view.Bag.Get(m) == b0`.
   5. `DropViewDrawsGroupsWithInstancedMaterials`:
      - Teleport như test 2; `d = Drops.Add(A.x + 2, G + 1, A.z, Block(Dirt), 3)`; `s = Drops.Add(A.x + 2, G + 1, A.z + 2, ItemId.SeedBag(0), 4)`;
      - `yield return null;` → `dv.LastInstanceCount == view.Editor.Drops.Stacks.Count + dv.FlyingCount`;
      - `dv.MaterialFor(ItemId.Block(Dirt)).enableInstancing` true; `MaterialFor(Block(Dirt)).GetColor("_BaseColor")` bằng `DropView.ColorOf(Block(Dirt))` (so từng kênh ±1e-3);
      - `DropView.LocalCenter(d)` = `(A.x + 2.5, G + 1.15, A.z + 0.5)`; `DropView.LocalCenter(s).y` = `G + 1.2` (±1e-4);
      - `TerrainEditInput.DropLabel(d) == "Đất × 3"`; `TerrainEditInput.ItemLabel(ItemId.SeedBag(0)) == "Túi hạt Cây A"`.
   - Xong khi: 5 test qua, không có log lỗi.

## Tiêu chí xong
- `grep -n "UnityEngine" client/Assets/Game/Core/DropPick.cs client/Assets/Game/Core/StarterBag.cs` → không có.
- `grep -n "kho hết" client/Assets/Game/Client/TerrainEditInput.cs` → không có.
- `git diff --stat fa72764 -- client/Assets/Game/Core/DropField.cs client/Assets/Game/Core/PlayerBag.cs client/Assets/Game/Core/WorldEditor.cs client/Assets/Game/Core/Sim client/Assets/Game/Client/Scenes` → rỗng.
- EditMode: toàn bộ qua, gồm 8 `DropPickTests` + 7 `StarterBagTests`.
- PlayMode: toàn bộ qua, gồm 5 `DropPlayTests`.
- Sau PlayMode, `git status` không có `client/ProjectSettings/EditorSettings.asset`.

## Test sau khi xong

### Phần cần test
- **Chọn chồng bằng tia (Core)** · `DropPickTests` 1–8 · trúng từ trên, bị che theo `maxDistance`, ngoài 4 ô bỏ, gần nhất thắng, trượt cạnh, gốc trong hộp, bằng nhau giữ chồng trước, null → exception.
- **Ô túi hạt đầu game (Core)** · `StarterBagTests` 1–7 · 3 ô phía trước, hướng +x, nước/cột cao → ô gần nhất (10, 2, 9), hố sâu 1 nhận, không có ô → false/null, ngoài map bỏ qua; nhặt được 4 hạt.
- **Túi hạt đầu game trong game + HUD** · `DropPlayTests.StarterBagNearSpawnAndHud` · có chồng × 4, F nhặt, HUD `Túi hạt Cây A: 4`.
- **F (Thu) chỉ trong 4 ô + bay về** · `DropPlayTests.CollectNearbyTakesOnlyWithin4` · nhặt chồng +2 ô, bỏ chồng +6 ô; `FlyingCount` 1 rồi 0 sau 0,3 s.
- **Click nhặt / chồng xa không chặn đào** · `DropPlayTests.ClickPicksOnlyInReachAndFarDoesNotBlockDig` · tia trúng chồng gần → chồng đó; tia vào chồng xa → không chọn, `Pick` ra ô đất phía dưới; nhặt không đào.
- **Đào → vật rơi hiện, nhặt, đặt lại** · `DropPlayTests.DigDropsVisibleThenCollectAndPlace` · chồng × 1 tại ô đào, `LastInstanceCount` = số chồng, túi +1 rồi −1.
- **Vẽ instancing + nhãn** · `DropPlayTests.DropViewDrawsGroupsWithInstancedMaterials` · đếm thể hiện đúng, material `enableInstancing`, màu, tâm hình, `Đất × 3`, `Túi hạt Cây A`.
- **Nhân vật, tầm 10 ô, ô chiếm, F3 không đổi** (phần cũ, `PlayerCharacter` sửa) · toàn bộ `PlayerCharacterPlayTests` (gồm `ReachLimitsTools`: đào ở 10 ô được, > 10 ô không) · qua.
- **Đào/đặt/khung/bản mờ qua `TerrainEditInput`** (phần cũ, `Update` sửa) · toàn bộ `TerrainEditPlayTests`, `EditOcclusionPlayTests` · qua.
- **Xới, bảng ô ruộng, kho** (phần cũ, dùng `TerrainEditInput`) · `SimFarmUiPlayTests`, `SimViewPlayTests`, `SimViewMapsPlayTests` · qua.
- **Còn lại** (minimap, streaming, bench, trời, nước) · `run_tests` PlayMode + EditMode toàn bộ · qua.

### Lệnh executor chạy
1. Unity Editor đang mở project qua MCP: `run_tests` mode `EditMode` (toàn bộ) → ghi dòng tổng kết pass/fail.
2. `run_tests` mode `PlayMode` (toàn bộ) → ghi dòng tổng kết.
3. `git checkout -- client/ProjectSettings/EditorSettings.asset`.
4. `read_console` (Error) sau PlayMode → ghi số lỗi (mong đợi 0 lỗi do code mới).
5. Grep + `git diff --stat` ở "Tiêu chí xong" → ghi kết quả.
6. Editor tắt (không có MCP): thay 1–2 bằng `python3 tools/unity_test.py EditMode` và `python3 tools/unity_test.py PlayMode`.

### User tự test
Trên Mac, Play scene `TerrainView` rồi scene `SimView` (cả hai có nhân vật). Cần thấy:
1. **Túi hạt đầu game:** hộp nhỏ màu nâu nằm trên đất khoảng 3 ô trước mặt nhân vật lúc bắt đầu, xoay chậm. Nhấn **F** → hộp bay về nhân vật rồi biến mất; HUD có dòng `Túi hạt Cây A: 4`; dòng kết quả `Thu: nhặt 1 chồng vật rơi`.
2. **Đào ra vật rơi:** đào vài ô cỏ, đất, đá, cát → mỗi ô có khối mini đúng màu nằm trong lỗ, xoay chậm. Thanh `Túi:` **không** tăng khi đào. Dòng kết quả `Đào (…): rơi 1 cỏ`.
3. **Vật rơi nằm đúng chỗ:** không lơ lửng rõ, không chìm hẳn vào đất (mesh bo cạnh có thể lệch nhẹ — ghi lại nếu thấy xấu).
4. **Ngắm + click nhặt:** đưa chuột lên một vật rơi trong khoảng 4 ô → không có khung trắng, HUD có `Nhặt (chuột trái): Cỏ × 1`; chuột trái → nhặt, `Túi:` tăng, vật bay về.
5. **Vật rơi ngoài 4 ô:** đứng xa hơn 4 ô, click vào vật rơi → **đào khối phía sau** như thường (vật không chặn).
6. **F gom nhiều:** đào 5–6 ô quanh mình rồi nhấn F → mọi vật trong 4 ô bay về cùng lúc; vật xa hơn ở lại.
7. **Rơi / đẩy lên:** đào khối ngay dưới một vật rơi → vật rơi xuống ô dưới; đặt khối vào ô có vật rơi → vật nhảy lên trên khối mới.
8. **Đặt trừ túi:** đặt khối giảm số trong `Túi:`; hết loại đó → bản mờ đỏ, lý do `túi hết loại này (nhặt vật rơi bằng F hoặc chuột trái, …)`.
9. **Mượt:** đào liên tục ~30 ô không thấy giật thêm so với trước.
10. **Camera bay F3:** bấm F3, F vẫn nhặt vật quanh **nhân vật** (không quanh camera).

## Dừng và chuyển lại Opus khi
- Brief mơ hồ hoặc mâu thuẫn với code thật (vd số dòng lệch làm không chắc sửa chỗ nào).
- Cần một quyết định thuộc AGENTS.md §3.4 hoặc ngoài "Quyết định đã chốt".
- Cùng một lỗi sửa 2 lần vẫn chưa qua.
- Phải sửa file ngoài "Phạm vi" (vd `DropField`, `TerrainChunkView`, scene).
- `RenderMeshInstanced` ném lỗi hoặc không vẽ được với material Landmark (URP Lit) — không tự đổi shader/material.
- Test cũ fail (không phải test mới) — không tự sửa test cũ.
- Cần API Unity ngoài danh sách ở "Bối cảnh cần đọc" và API đã có trong project.

---
<!-- Từ đây trở xuống: EXECUTOR ghi. -->

## Kết quả
- Base commit: 04bc014
- Chạy bằng: claude-sonnet-5-5 · medium (OK: brief 0037 cần Sonnet · medium)
- Bắt đầu: 2026-10-22 15:44 · kết thúc: 2026-10-22 15:54 (~10 phút thực tế)
- Commit: 35ddfd4 — file: Core/DropPick.cs, Core/StarterBag.cs, Client/DropView.cs, Client/TerrainEditInput.cs, Client/PlayerCharacter.cs, Tests/Core/DropPickTests.cs, Tests/Core/StarterBagTests.cs, Tests/Client/DropPlayTests.cs (+ .meta)
- Lệch brief: không. DropView chép nguyên khung brief.
<!-- Base commit (trước khi làm) · Chạy bằng: <model id> · <effort> (dòng OK của brief_check.py) · commit của brief · file đã đổi · giờ bắt đầu/kết thúc · ghi chú lệch so với brief (nếu có, kèm lý do). -->

## Kết quả test
- Chọn chồng bằng tia (DropPickTests 1–8) → đạt (EditMode 437/437).
- Ô túi hạt (StarterBagTests 1–7) → đạt.
- Túi hạt đầu game + HUD, F 4 ô + bay, click nhặt/chồng xa không chặn, đào→nhặt→đặt, instancing + nhãn (DropPlayTests 1–5) → đạt (chạy riêng 5/5; log `STARTERBAG … ở (123, 21, 206)`).
- Phần cũ (PlayerCharacter, TerrainEdit, Sim*, còn lại) → đạt (PlayMode toàn bộ 117/117).
- Phần user tự test (10 mục) → chờ user.
- `run_tests EditMode` → 437 passed / 0 failed. `run_tests PlayMode` → 117 passed / 0 failed.
- `git checkout -- EditorSettings.asset` → đã chạy; `read_console` Error → 0.
- grep UnityEngine trong DropPick/StarterBag → không có; grep `kho hết` → không có; `git diff --stat fa72764` file cấm sửa → rỗng.
<!-- Mỗi dòng ở "Phần cần test" → đạt / không đạt / chờ user + bằng chứng (tên test, dòng output).
Mỗi lệnh ở "Lệnh executor chạy": `<lệnh>` → <kết quả thật, dòng tổng kết>. -->

## Vướng mắc
<!-- Khi trạng thái = cần Opus: câu hỏi cụ thể + các phương án (kèm đề xuất). -->

---
<!-- PLANNER ghi khi review. -->

## Review
<!-- Kết luận: đã duyệt | cần sửa (thêm bước sửa vào "Các bước", trạng thái về chờ làm). Điểm đã đối chiếu. -->
