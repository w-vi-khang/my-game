# Brief 0094 — World 5j (lỗi 14–15): đầu dải mép cỏ tròn ngắn + mép cỏ nhận AO

> Trạng thái: đã duyệt
> Review: đầy đủ + user xem (gộp cuối 5j)
> Model thực thi: Opus · medium
> Cập nhật: 2026-11-01 22:10

## Mục tiêu
Lỗi 14 và 15 của task [5j](../tasks/m1-world-5j-sua-loi-choi-thu.md), mục 14–15. User chơi thử ngày 2026-11-01 và thấy hai lỗi ở bậc thang cỏ:
- **Lỗi 15:** đầu dải mép cỏ chạm vách nhọn như lưỡi dao. Ô hẹp 1 ô bị thon cả hai đầu thành mũi tên chữ V.
- **Lỗi 14:** đầu dải cạnh vách thành mảng sáng, sườn lộ ra dưới đầu thon trông tối hẳn.

Sửa theo lựa chọn của user (2026-11-01):
- (A) Đầu thon đổi sang dạng **1/4 elip**, dài **0,3 ô** [số tạm]. Đầu dải tròn như đầu lưỡi.
- (B) Đỉnh mép cỏ **nhận AO** giống mặt trên khối.

## Bối cảnh cần đọc
**Planner dựng lại 2026-11-01** [run, MCP `execute_code`, world 16³]:
- Lỗi 15: thon 0091 dùng `smoothstep` trên 0,5 ô và thu cả t lẫn dy về (0, 0), nên đầu dải thành lưỡi dao. Ô hẹp thon hai đầu nối nhau ở giữa thành chữ V.
- Lỗi 14: đỉnh mặt khối có AO, còn đỉnh mép cỏ dùng màu gốc. Sát vách cao hơn, mặt cỏ tối dần nhưng dải cỏ vẫn sáng nên nổi thành mảng.
- Ảnh user (ngoài repo): `~/Documents/User/khang/Game/art-test/shots/user5j_grass_lip_dark_patch.png`, `user5j_terraces_sharp.webp`.

**Code** — `client/Assets/Game/World/BlockMesher.cs` (số dòng theo commit `945b006`; sau bước 1, các dòng phía dưới lùi 2):
- Hằng: `public const int LipSegments = 4, LipCornerSegments = 3;` (`:55`), `public const float LipTaperLength = 0.5f;` (`:57`, summary ở `:56`).
- `public static readonly float[] AoBrightness = { 0.62f, 0.74f, 0.87f, 1f };` (`:119`). `public static uint Shade(uint color, float k)` (`:122`): nhân RGB, giữ byte alpha.
- Đỉnh mặt khối nhận AO trong `Vert` (`:639–654`): `Out.Colors.Add(Shade(color, AoBrightness[ao]))`. `ao` lấy từ `static int AoLevel(...)` (`:660–675`):
  - ô trước mặt của chính ô rỗng; xét hai ô cạnh (side1, side2) và ô chéo (corner) quanh đỉnh;
  - `return side1 == 1 && side2 == 1 ? 0 : 3 - side1 - side2 - corner;`;
  - tổ hợp ô (`config`, `:427–440`) lấy bằng `IsSolid`.
- Mép cỏ (struct `MeshJob`, `[BurstCompile]` `:404–405`):
  - `void AddGrassLip(int3 c, int3 g, uint color)` (`:755–770`). `c` = ô theo toạ độ chunk (đỉnh mesh dùng hệ này), `g` = ô theo toạ độ world. Gọi ở `:471` với `Palette[m]`.
  - `void AddLipConcave(int3 c, int3 g, int sx, int sz, uint color)` (`:784–805`): 6 đỉnh `far, p0..p4` qua `AddVertex(..., n, color)`.
  - `static float3 LipPoint(int3 c, int sideAxis, int sideSign, float t, float dy, float sigma)` (`:808`).
  - `void AddLipEdge(int3 c, int3 g, int sideAxis, int sideSign, uint color)` (`:828–887`):
    - `taper0`/`taper1` (`:839–840`); hàm cục bộ `TaperAt` (`:841–847`, `smoothstep`);
    - lưới `i < LipProfilePoints` × `k <= LipSegments`, `sigma = (float)k / LipSegments` (`:851–861`);
    - quạt mặt trên `p0a/p0b` (`:863–867`);
    - mặt cong và bỏ tam giác suy biến `taper1 && k == LipSegments - 1` / `taper0 && k == 0` (`:869–879`);
    - đáy trong `f0/f1` (`:881–886`).
  - `void AddLipCorner(int3 c, int3 g, int sx, int sz, uint color)` (`:890–929`): đỉnh `center`, `wCenter`, lưới `rows × col`, đều qua `AddVertex(..., color)`.
- `int AddVertex(float3 p, float3 n, uint color)` (`:1700`). `void AddOriented(int a, int b, int c, float3 want)` (`:1710`). `bool IsSolid(int3 p)` (`:1732`).
- `FixedList128Bytes<float>` (Unity.Collections 6.4.0, đã có trong `Library/PackageCache` [run]). Repo đã dùng `FixedList32Bytes<byte>`/`FixedList64Bytes<byte>` trong cùng job (`:535`, `:547`). [docs](https://docs.unity3d.com/Packages/com.unity.collections@6.4/api/Unity.Collections.FixedList128Bytes-1.html)

**Test** — `client/Assets/Game/Tests/World/BlockMesherTests.cs`:
- `LipEndTapersIntoSolidNeighbour` (`:466–495`): ô cỏ (4,8,4) kề đá (4,8,5). Kỳ vọng f(z 4,75) = 0,5 sẽ sai theo thon mới. **Viết lại** ở bước 4.
- Test mép cỏ giữ nguyên kỳ vọng:
  - `LipDepthStaysInRange` `:313`, `LipProfileIsRoundedPillow` `:323`, `LipOnlyOnGrassWithOpenTopAndSide` `:356`;
  - `LipIsClosedFromBelowAndUnchangedFromAbove` `:447`, `LipIsDeterministicAndContinuousAcrossCellsAndChunks` `:497`;
  - `TrianglesFaceTheirNormals` `:253`, `EveryM1ChunkFits16BitIndices`, `ColliderIsOneQuadPerExposedFaceOnM1World` `:97`.
- Test màu `ConcaveCornerIsDarkerByVertexAo`, `GrassSidesUseDirtColor`, `FlatGroundIsOneQuadPerColumnOnTop` dùng `grassLip: false` hoặc không có mép → không bị ảnh hưởng.
- `LipTapersAtChannelSpill` `:1095`, `LipTapersIntoUpstreamCell` `:1219` là mép tràn **nước**, không liên quan.

## Quyết định đã chốt (không được đổi)
1. **Đầu thon** vẫn đúng điều kiện cũ: `taper0`/`taper1` (`:839–840`) không đổi. Đầu khác giữ nguyên.
2. **Đường thon 1/4 elip:**
   - `s` = khoảng cách tới đầu thon gần nhất, như cũ.
   - `s >= LipTaperLength` → f = 1. Ngược lại `u = 1 − s / LipTaperLength`, `f = sqrt(max(0, 1 − u²))`.
   - Áp `p' = p · f` như 0091 (cả t và dy). Pháp tuyến giữ như `LipProfile` trả về.
3. `LipTaperLength` = **0,3** [số tạm, chưa chốt]. Là hằng hình học mesh, không đưa vào JSON (như 0091 QĐ 4). Điều kiện: ≤ 0,5 để hai đầu thon của ô hẹp không chồng nhau.
4. **Lưới dọc cạnh không đều** (thay 0091 QĐ 6). Thêm hằng `LipTaperSegments` = 4.
   - Mỗi đầu thon có các cột tại `s_j = LipTaperLength · (1 − cos(j · π/2 / LipTaperSegments))`, j = 0..3. Khi đó f = sin(j · 22,5°) = 0; 0,383; 0,707; 0,924.
   - Phần giữa gồm: `lo` (= `LipTaperLength` nếu `taper0`, ngược lại 0), các điểm `k / LipSegments` nằm hẳn trong (lo, hi), rồi `hi` (= `1 − LipTaperLength` nếu `taper1`, ngược lại 1).
   - **Dải không thon** cho đúng 5 cột 0; 0,25; 0,5; 0,75; 1 như hiện tại. Đỉnh và tam giác không đổi.
5. **Bỏ tam giác suy biến** ở cột thon như 0091 QĐ 7: cột đầu (k = 0) khi `taper0`, cột cuối (k = cols − 2) khi `taper1`.
6. **AO mép cỏ:**
   - Độ sáng tại một đỉnh mép cỏ của ô cỏ g = nội suy song tuyến độ sáng AO của 4 góc mặt trên ô g.
   - Toạ độ trong ô: `a = saturate(p.x − c.x)`, `b = saturate(p.z − c.z)`. p là toạ độ chunk của đỉnh; điểm nhô ra ngoài ô bị kẹp về mép.
   - AO góc: đúng công thức `AoLevel` cho mặt trên. Ô trước mặt là `g + (0,1,0)`; side1, side2, corner là ba ô còn lại ở y + 1 quanh góc. Tính bằng `IsSolid`.
   - Màu đỉnh = `Shade(color, k)`.
   - Áp cho **mọi** đỉnh của `AddLipEdge`, `AddLipCorner`, `AddLipConcave`. Ô cỏ giữa vùng trống (4 góc sáng = 1) cho màu y như trước.
7. Không đổi: mặt cắt `LipProfile`, độ sâu `LipDepthAt`, `LipSegments`, `LipCornerSegments`, hình mảnh góc lồi và góc lõm, collider, shader, mép tràn nước.

## Phạm vi
- Được sửa:
  - `client/Assets/Game/World/BlockMesher.cs`
  - `client/Assets/Game/Tests/World/BlockMesherTests.cs`
  - file brief này (mục Kết quả trở xuống)
- Ảnh chụp kiểm (bước 0 và bước 5): ghi ra `~/Documents/User/khang/Game/art-test/shots/b0094_*.png`, ngoài repo.
- Không được sửa: mọi file khác.

## Các bước
0. **Chụp trước khi sửa.** Chạy đoạn code ở mục "Code chụp kiểm" bằng MCP `execute_code` (compiler mặc định codedom, C# 6) với `tag = "before"`.
   - Nếu Editor đang ở Play Mode: `manage_editor` action `stop` một lần, ghi vào Kết quả.
   - Xong khi: có 4 file `b0094_before_0..3.png`.
1. **Hằng** trong `BlockMesher.cs`.
   - Thay `:56–57` bằng nguyên văn:
     ```csharp
             /// <summary>Độ dài đoạn thon (1/4 elip) ở đầu dải mép cỏ chạm khối đặc dọc cạnh (brief 0091 lỗi 10, brief 0094 lỗi 15) [số tạm]. Phải ≤ 0,5.</summary>
             public const float LipTaperLength = 0.3f;
             /// <summary>Số đoạn của mỗi đoạn thon: cột tại s_j = LipTaperLength·(1 − cos(j·90°/LipTaperSegments)) (brief 0094).</summary>
             public const int LipTaperSegments = 4;
     ```
   - Xong khi compile được.
2. **Đường thon + lưới** trong `AddLipEdge` (`:835–887`).
   - Thay `TaperAt` bằng:
     ```csharp
                     float TaperAt(float sigma)
                     {
                         if (!taper0 && !taper1)
                             return 1f;
                         float s = math.min(taper0 ? sigma : 1f, taper1 ? 1f - sigma : 1f);
                         if (s >= LipTaperLength)
                             return 1f;
                         float u = 1f - s / LipTaperLength;
                         return math.sqrt(math.max(0f, 1f - u * u));
                     }
     ```
   - Ngay sau `TaperAt`, dựng danh sách sigma:
     ```csharp
                     // Lưới dọc cạnh (brief 0094): đoạn thon dày theo cos → mặt cắt nở theo 1/4 elip; phần giữa giữ lưới k / LipSegments.
                     var sig = new FixedList128Bytes<float>();
                     if (taper0)
                         for (int j = 0; j < LipTaperSegments; j++)
                             sig.Add(TaperSigma(j));
                     float lo = taper0 ? LipTaperLength : 0f, hi = taper1 ? 1f - LipTaperLength : 1f;
                     sig.Add(lo);
                     for (int k = 1; k < LipSegments; k++)
                     {
                         float s = (float)k / LipSegments;
                         if (s > lo + 1e-4f && s < hi - 1e-4f)
                             sig.Add(s);
                     }
                     sig.Add(hi);
                     if (taper1)
                         for (int j = LipTaperSegments - 1; j >= 0; j--)
                             sig.Add(1f - TaperSigma(j));
                     int cols = sig.Length;
     ```
   - Thêm hàm tĩnh trong `MeshJob`, đặt ngay trước `AddLipEdge`:
     ```csharp
                 /// <summary>Vị trí cột thứ j (từ đầu thon) của đoạn thon: LipTaperLength·(1 − cos(j·90°/LipTaperSegments)) (brief 0094).</summary>
                 static float TaperSigma(int j) => LipTaperLength * (1f - math.cos(j * (0.5f * math.PI / LipTaperSegments)));
     ```
   - Sửa phần dùng lưới:
     - `int row = LipSegments + 1;` → `int row = cols;`.
     - Vòng tạo đỉnh `for (int k = 0; k <= LipSegments; k++)` → `for (int k = 0; k < cols; k++)`, với `float sigma = sig[k];`.
     - Quạt mặt trên: `AddOriented(p0a, p0b, first + cols - 1, up);` và `for (int k = cols - 1; k > 0; k--)`.
     - Mặt cong: `for (int k = 0; k < cols - 1; k++)`, điều kiện bỏ `taper1 && k == cols - 2`. Giữ `taper0 && k == 0`.
     - Đáy trong: `for (int k = 0; k < cols - 1; k++)`, rồi `AddOriented(f0, w + cols - 1, f1, down);`.
   - Sửa comment `:849–850`: "lưới LipProfilePoints hàng × cols cột (brief 0094); đỉnh (i, k) = first + i·row + k".
   - Xong khi: compile được, không còn `LipSegments` trong thân `AddLipEdge` ngoài vòng dựng `sig`.
3. **AO mép cỏ.** Thêm vào `MeshJob`, ngay trước `AddGrassLip`:
   ```csharp
               /// <summary>Độ sáng AO 4 góc mặt trên ô g (x0z0, x1z0, x0z1, x1z1): cùng công thức AoLevel cho mặt trên — ô trước mặt g + (0,1,0) (brief 0094, lỗi 14).</summary>
               float4 TopAo(int3 g)
               {
                   float4 r = 0;
                   for (int i = 0; i < 4; i++)
                   {
                       int dx = (i & 1) == 1 ? 1 : -1, dz = (i & 2) == 2 ? 1 : -1;
                       int3 up = g + new int3(0, 1, 0);
                       int side1 = IsSolid(up + new int3(dx, 0, 0)) ? 1 : 0;
                       int side2 = IsSolid(up + new int3(0, 0, dz)) ? 1 : 0;
                       int corner = IsSolid(up + new int3(dx, 0, dz)) ? 1 : 0;
                       r[i] = AoBrightness[side1 == 1 && side2 == 1 ? 0 : 3 - side1 - side2 - corner];
                   }
                   return r;
               }

               /// <summary>Đỉnh mép cỏ của ô c (toạ độ chunk): màu nhân độ sáng AO mặt trên nội suy song tuyến tại (x, z) của p, kẹp vào ô (brief 0094).</summary>
               int AddLipVertex(int3 c, float4 ao, float3 p, float3 n, uint color)
               {
                   float a = math.saturate(p.x - c.x), b = math.saturate(p.z - c.z);
                   float k = math.lerp(math.lerp(ao.x, ao.y, a), math.lerp(ao.z, ao.w, a), b);
                   return AddVertex(p, n, Shade(color, k));
               }
   ```
   - Trong `AddLipEdge`, `AddLipCorner`, `AddLipConcave`: đầu hàm thêm `float4 ao = TopAo(g);`. Đổi **mọi** `AddVertex(X, N, color)` thành `AddLipVertex(c, ao, X, N, color)`. Không đổi biểu thức X, N.
     - `AddLipEdge`: đỉnh lưới, `p0a`, `p0b`, `f0`, `f1`.
     - `AddLipCorner`: `center`, `wCenter`, đỉnh lưới.
     - `AddLipConcave`: `far`, `p0..p4`.
   - Không gọi `TopAo` trong vòng lặp đỉnh.
   - Xong khi: `grep -n "AddVertex(" BlockMesher.cs` không còn dòng nào trong ba hàm trên.
4. **Test** trong `BlockMesherTests.cs`.
   - **Viết lại `LipEndTapersIntoSolidNeighbour`.** Giữ dựng world và vòng kiểm z = 5: không có đỉnh pháp tuyến (0,0,1), x trong [4, 5]. Giữ kiểm mid z = 4,5: hai bên đủ `LipOverhang`. Bỏ `maxQuarter` z 4,75, thay bằng:
     ```csharp
                 // brief 0094: thon 1/4 elip — tại z = 5 − s_j, mép +x nhô đúng sin(j·22,5°)·LipOverhang; tại z = 5 − LipTaperLength nhô đủ.
                 for (int j = 1; j <= BlockMesher.LipTaperSegments; j++)
                 {
                     float th = j * 0.5f * math.PI / BlockMesher.LipTaperSegments;
                     float z = 5f - BlockMesher.LipTaperLength * (1f - math.cos(th));
                     float maxX = float.MinValue;
                     for (int i = 0; i < mesh.Vertices.Length; i++)
                         if (math.abs(mesh.Vertices[i].z - z) < 1e-4f)
                             maxX = math.max(maxX, mesh.Vertices[i].x);
                     Assert.AreEqual(5f + math.sin(th) * BlockMesher.LipOverhang, maxX, 1e-3f, $"+x lip at z {z}");
                 }
     ```
     Sửa comment đầu test: "brief 0091/0094: đầu dải chạm khối đặc thon 1/4 elip về 0 (LipTaperLength), không nắp."
   - **Test mới `NarrowCellLipIsRoundedNotV`**: ô cỏ (4,8,4), đá (4,8,3) và (4,8,5).
     - (a) Với z ∈ {4 + LipTaperLength, 4,5, 5 − LipTaperLength}: max x của đỉnh có |z − z0| < 1e-4 bằng `5 + LipOverhang` (±1e-3).
     - (b) Mọi đỉnh có |z − 4| < 1e-4 hoặc |z − 5| < 1e-4 có x ≤ 5 + 1e-4.
     - (c) Mọi tam giác có ít nhất một đỉnh x > 5 + 1e-3 có `math.length(GeometricNormal(mesh, t)) > 1e-7f`.
   - **Test mới `LipTakesTopFaceAo`:**
     - (a) Ô cỏ đơn (4,8,4). Mọi đỉnh nằm ngoài ô [4,5]×[4,5] theo x/z quá 1e-3 có màu đúng bằng `BlockMesher.TopColor(BlockMaterial.Grass)`.
     - (b) Ô cỏ (4,8,4), đá (4,8,5) và (4,9,5) (vách bậc cao hơn ở +z).
       - Đỉnh có x > 5 + 1e-3 và |z − 4| < 1e-4: màu = `TopColor(Grass)`.
       - Đỉnh có x > 5 + 1e-3 và |z − z1| < 1e-4, với `z1 = 5 − LipTaperLength·(1 − cos(22,5°))`: mỗi kênh R, G, B lệch ≤ 1 so với `BlockMesher.Shade(TopColor(Grass), math.lerp(1f, BlockMesher.AoBrightness[2], z1 - 4f))`. Byte alpha bằng của `TopColor`.
       - Mỗi đỉnh mặt trên khối (|y − 9| < 1e-4, `Normals[i].y > 0.99f`, x ∈ [4,8; 5], z ∈ [4,8; 5]) có màu = `Shade(TopColor(Grass), AoBrightness[2])`. Có ít nhất 1 đỉnh như vậy.
     - (c) Góc lõm: ô cỏ (4,8,4), (5,8,4), (4,8,5); đá (5,9,4).
       - Đỉnh mảnh góc lõm của ô (4,8,4) là đỉnh có |y − 9,002| < 1e-4, x ∈ [4,85; 5,02], z ∈ [4,85; 5,02].
       - Có ≥ 1 đỉnh như vậy, và mỗi đỉnh có kênh R < kênh R của `TopColor(Grass)`.
   - Xong khi: 3 test trên đạt.
5. **Chụp sau khi sửa:** chạy lại "Code chụp kiểm" với `tag = "after"`.
   - Xong khi: có 4 file `b0094_after_0..3.png`.
   - Mở xem bằng Read. Ghi 1 dòng nhận xét mỗi ảnh vào Kết quả: đầu dải tròn hay nhọn, có chữ V không, có mảng sáng sát vách không.

### Code chụp kiểm
Đổi `tag` cho từng lần chạy. World thử giống lúc Planner dựng lại: bậc thang 3 tầng, khe 1 ô ở x = 4 và x = 7, không nước.
```csharp
string tag = "before";
var w = new Game.Core.WorldData(new Game.Core.WorldSize(16, 16, 16));
for (int x = 0; x < 16; x++) for (int z = 0; z < 16; z++) {
  int hh = z < 3 ? 4 : (z >= 8 ? 7 : (x <= 9 ? 6 : 5));
  if (x == 4 && z >= 3 && z < 8) hh = 5;
  if (x == 7 && z >= 5 && z < 8) hh = 5;
  for (int y = 0; y <= hh; y++) w.Set(x, y, z, Game.Core.Cell.Make(y == hh ? Game.Core.BlockMaterial.Grass : Game.Core.BlockMaterial.Dirt));
}
var cm = Game.World.BlockMesher.Build(w, 0, 0, 0);
var root = new GameObject("TmpLipRoot"); root.transform.position = new Vector3(1000, 0, 1000);
var mesh = new Mesh(); Game.World.ChunkMeshWriter.Write(cm, mesh);
root.AddComponent<MeshFilter>().sharedMesh = mesh;
var mat = new Material(Shader.Find("Game/Terrain"));
root.AddComponent<MeshRenderer>().sharedMaterials = new Material[] { mat, mat };
var go = new GameObject("TmpShotCam"); var cam = go.AddComponent<Camera>();
if (Camera.main != null) cam.CopyFrom(Camera.main);
cam.fieldOfView = 35; cam.nearClipPlane = 0.02f; cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0.6f, 0.75f, 0.9f);
var tr = root.transform;
Vector3[] pos = { new Vector3(5.5f, 10f, 1.0f), new Vector3(4.5f, 7.6f, 0.6f), new Vector3(5.3f, 5.75f, 1.9f), new Vector3(7.45f, 7.15f, 7.0f) };
Vector3[] look = { new Vector3(5.5f, 6f, 7f), new Vector3(4.5f, 5.8f, 3.2f), new Vector3(4.6f, 5.9f, 3.0f), new Vector3(8.0f, 6.85f, 8.0f) };
string dir = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal) + "/Documents/User/khang/Game/art-test/shots/";
for (int k = 0; k < pos.Length; k++) {
  go.transform.position = tr.TransformPoint(pos[k]); go.transform.LookAt(tr.TransformPoint(look[k]));
  var rt = new RenderTexture(800, 600, 24); cam.targetTexture = rt; cam.Render();
  RenderTexture.active = rt; var tex = new Texture2D(800, 600, TextureFormat.RGB24, false); tex.ReadPixels(new Rect(0, 0, 800, 600), 0, 0); tex.Apply();
  System.IO.File.WriteAllBytes(dir + "b0094_" + tag + "_" + k + ".png", tex.EncodeToPNG());
  RenderTexture.active = null; cam.targetTexture = null; UnityEngine.Object.DestroyImmediate(rt); UnityEngine.Object.DestroyImmediate(tex);
}
UnityEngine.Object.DestroyImmediate(go); UnityEngine.Object.DestroyImmediate(root);
return dir + "b0094_" + tag + "_0..3.png";
```
Lưu ý: compiler codedom chỉ hỗ trợ C# 6, không dùng tuple. Không lưu scene. Đoạn code tự xoá object tạm.

## Tiêu chí xong
- `LipEndTapersIntoSolidNeighbour` (viết lại), `NarrowCellLipIsRoundedNotV`, `LipTakesTopFaceAo` đạt.
- Toàn bộ `BlockMesherTests` và toàn bộ EditMode đạt.
- `read_console` Error = 0.
- `git diff` chỉ chạm 2 file trong Phạm vi (cộng file brief).
- Có 8 ảnh `b0094_{before,after}_0..3.png`, kèm nhận xét.

## Test sau khi xong

### Phần cần test
- Thon 1/4 elip, độ dài 0,3 (bước 1–2, QĐ 2–4) · `LipEndTapersIntoSolidNeighbour` viết lại · trên code cũ: fail; sau sửa: đạt.
- Ô hẹp tròn, không chữ V, không tam giác suy biến (bước 2, QĐ 3–5) · `NarrowCellLipIsRoundedNotV` · trên code cũ: fail; sau sửa: đạt.
- Mép cỏ nhận AO ở dải, góc lõm; ô trống giữ màu cũ (bước 3, QĐ 6) · `LipTakesTopFaceAo` · trên code cũ: fail ở (b)/(c); sau sửa: đạt.
- Dải không thon không đổi hình, liền qua ô và chunk (QĐ 4, 7) · `LipIsDeterministicAndContinuousAcrossCellsAndChunks`, `LipOnlyOnGrassWithOpenTopAndSide`, `LipIsClosedFromBelowAndUnchangedFromAbove` · đạt, không đổi kỳ vọng.
- Mặt đúng hướng, chunk vừa chỉ số 16 bit (bước 2 thêm đỉnh) · `TrianglesFaceTheirNormals`, `EveryM1ChunkFits16BitIndices` · đạt. Ghi số đỉnh tối đa/chunk từ dòng `MESHSTATS`.
- Phần khác không bị ảnh hưởng (regression) · EditMode toàn bộ · đạt.
- Hình ảnh đầu dải (bước 0, 5) · 8 ảnh chụp kiểm · after: đầu dải tròn, khe hẹp không chữ V, không mảng sáng sát vách.

### Lệnh executor chạy
1. Bước 0: chụp `before`.
2. Sau khi viết test ở bước 4 nhưng **trước** bước 1–3 (code cũ): MCP `run_tests` EditMode, lọc 3 test ở bước 4 → ghi fail và thông báo.
   - Thứ tự làm thực tế: 0 → 4 → chạy lệnh này → 1 → 2 → 3 → chạy lại.
   - Hằng `LipTaperSegments` chưa có thì test không compile được. Khi đó thêm tạm hằng ở bước 1 với `LipTaperLength` vẫn 0,5 để chạy fail-first, ghi rõ vào Kết quả.
3. Sau bước 3: `run_tests` EditMode lớp `BlockMesherTests` → số đạt/tổng.
4. `run_tests` EditMode toàn bộ → số đạt/tổng.
5. `read_console` Error → 0.
6. Bước 5: chụp `after`, xem và nhận xét.
7. `git status` → chỉ file trong Phạm vi.

Không chạy PlayMode: chỉ đổi hình mesh và màu đỉnh, không đổi logic. Không cần dọn `EditorSettings.asset`.

### User tự test
Gộp cuối 5j. SimView, Play, bậc thang cỏ phía nam trại (chỗ chụp `user5j_terraces_sharp.webp`):
- Đầu dải cỏ chạm vách bậc cao hơn: đầu tròn như đầu lưỡi, không còn lưỡi dao/sừng nhọn.
- Ô hẹp 1 ô kẹp giữa hai bậc cao: dải cỏ có đáy cong, phẳng ở giữa, không còn mũi tên chữ V.
- Sát chân vách: dải cỏ tối dần cùng mặt cỏ, không còn mảng sáng nổi lên. Góc sườn dưới đầu dải không còn vệt tối tương phản mạnh.
- Bậc thang sát nước (chỗ `user5j_grass_lip_dark_patch.png`): không còn phần đen dưới đầu dải.
- Dải cỏ ở chỗ khác (sườn đồi, góc lồi, góc lõm) trông như cũ.

## Dừng và chuyển lại Opus khi
- Brief mơ hồ hoặc mâu thuẫn với code thật (vd số dòng, chữ ký khác Bối cảnh).
- Burst không compile được `FixedList128Bytes<float>`, `float4` indexer hoặc `AoBrightness` trong hàm mới.
- Test cũ ở "giữ nguyên kỳ vọng" fail.
- Cần một quyết định thuộc AGENTS.md §3.4 hoặc ngoài "Quyết định đã chốt".
- Cùng một lỗi sửa 2 lần vẫn chưa qua.
- Phải sửa file ngoài "Phạm vi".

---

## Kết quả
- Base commit: 977bf3d
- Chạy bằng: claude-opus-5-5 · medium
- Bắt đầu: 2026-11-01 21:40
- Kết thúc: 2026-11-01 21:52 (≈ 12 phút làm + ~7 phút chờ EditMode toàn bộ).
- Commit: `fb0d8b0` — `client/Assets/Game/World/BlockMesher.cs`, `client/Assets/Game/Tests/World/BlockMesherTests.cs`.
- Bước 0: Editor không ở Play Mode. 4 ảnh `b0094_before_0..3.png` [run].
- Bước 1–3 làm đúng nguyên văn brief. `float4 ao = TopAo(g);` trong `AddLipEdge` đặt sau khi dựng `sig` (trước vòng đỉnh), ngoài mọi vòng lặp. `grep AddVertex(` trong 3 hàm mép cỏ: không còn (chỉ còn trong `AddLipVertex`).
- Burst compile `FixedList128Bytes<float>`, `float4` indexer, `AoBrightness`: không lỗi, `read_console` Error = 0.
- Nhận xét ảnh after:
  - `after_0`: đầu dải cạnh vách bo tròn ngắn, không còn lưỡi dao; sát vách không có mảng sáng nổi.
  - `after_1`: khe hẹp x = 4 nhìn từ dưới: đáy dải cong tròn, phẳng ở giữa, không chữ V.
  - `after_2`: (before là mũi tên chữ V rõ) giờ dải hẹp có hai đầu tròn, đáy phẳng. Còn một mấu nhọn nhỏ tối ở góc trên phải (~690, 200 px) — có cả ở ảnh before, không phải dải đang sửa (có thể là mảnh của ô kế bên/góc) [chưa xác minh], đề nghị Planner xem.
  - `after_3`: đầu dải sát vách tròn, tối dần theo vách, không còn mảng sáng.


## Kết quả test
- Fail-first (code cũ + hằng tạm `LipTaperSegments = 4`, `LipTaperLength` vẫn 0,5) [run, MCP run_tests]: 1/3 đạt.
  - `LipEndTapersIntoSolidNeighbour`: fail — "+x lip at z 4.96194 Expected 5.04592 But was -3.40E+38" (không có cột tại z mới).
  - `LipTakesTopFaceAo`: fail ở (b) — "top face color at float3(5f, 9f, 5f) Expected 22326906 But was 23120012".
  - `NarrowCellLipIsRoundedNotV`: **đạt** trên code cũ — với hằng tạm 0,5 ba điểm z (4 + 0,5; 4,5; 5 − 0,5) trùng nhau ở 4,5 nên (a) không phân biệt được; (b)(c) code cũ cũng thoả (đã bỏ tam giác suy biến từ 0091). Test không bắt được chữ V khi `LipTaperLength` = 0,5 — Planner xem có cần thêm kiểm (vd tại z 4,25 nhô đủ) không.
- Thon 1/4 elip 0,3 · `LipEndTapersIntoSolidNeighbour` · **đạt**.
- Ô hẹp tròn · `NarrowCellLipIsRoundedNotV` · **đạt** (xem ghi chú fail-first).
- Mép cỏ nhận AO · `LipTakesTopFaceAo` · **đạt**.
- Dải không thon không đổi · `LipIsDeterministicAndContinuousAcrossCellsAndChunks`, `LipOnlyOnGrassWithOpenTopAndSide`, `LipIsClosedFromBelowAndUnchangedFromAbove` · **đạt** (trong lớp 55/55).
- Mặt đúng hướng, 16 bit · `TrianglesFaceTheirNormals`, `EveryM1ChunkFits16BitIndices` · **đạt**. `MESHSTATS rounded edges: 22/32 chunks have surface; max 15032 triangles, 11845 vertices/chunk`.
- Lệnh:
  - `run_tests` EditMode lớp `BlockMesherTests`: 55/55 đạt.
  - `run_tests` EditMode toàn bộ: 696/696 đạt (418 s).
  - `read_console` Error: 0.
  - `git status`: chỉ 2 file Phạm vi + brief (file `.ai/tasks/m1-world-5k-nuoc-kieu-anh-tham-khao.md` untracked là của phiên khác, không đụng).
- Hình ảnh · 8 ảnh `b0094_{before,after}_0..3.png` · nhận xét ở Kết quả · **chờ user** (gộp cuối 5j).

## Vướng mắc

---

## Review
- Phiên con Planner (Opus · medium), 2026-11-01.
- "Chạy bằng" claude-opus-5-5 · medium khớp "Model thực thi". Mọi dòng "Phần cần test" có kết quả; dòng hình ảnh `chờ user` → gộp cuối 5j như mức Review ghi.
- Diff `fb0d8b0` (2 file, +207/−34) khớp bước 1–3 nguyên văn: hằng, `TaperAt` 1/4 elip, lưới `sig` + `TaperSigma`, `cols` thay `LipSegments` ở mọi chỗ dùng lưới, bỏ tam giác suy biến `k == cols − 2`/`k == 0`. `TopAo`/`AddLipVertex` đúng QĐ 6; `TopAo(g)` gọi một lần mỗi hàm, ngoài vòng lặp. Không đổi `LipProfile`, `LipDepthAt`, collider, mép nước (QĐ 7). Không file ngoài Phạm vi.
- AO liền qua ô kề: góc chung của hai ô cỏ kề có cùng tổng `side2 + corner` (corner ô này = side2 ô kia) → cùng độ sáng; khớp `LipIsDeterministicAndContinuousAcrossCellsAndChunks` đạt.
- Test: 3 test mới/viết lại đúng bước 4; lớp 55/55, EditMode 696/696, Error 0. Đỉnh tối đa 11845/chunk < 65535.
- Ghi chú (không chặn): `NarrowCellLipIsRoundedNotV` đạt cả trên code cũ trong lần fail-first vì brief bảo giữ hằng tạm 0,5 (3 điểm z trùng 4,5) — lỗi thiết kế bước fail-first của brief, không phải executor. Với hằng 0,3, code cũ (lưới 0,25) không có cột ở z 4,3/4,7 → test sẽ fail; đủ bắt hồi quy về lưới cũ.
- Ghi chú cho lượt user xem: ảnh `b0094_after_2.png` có mấu nhọn nhỏ tối ở góc trên phải (chỗ dải ô hẹp gặp vách), có cả ở ảnh before → không do brief này. Nếu user còn thấy "nhọn" ở đó khi chơi thử, ghi thành lỗi mới trong task 5j.
- Kết luận: đã duyệt
