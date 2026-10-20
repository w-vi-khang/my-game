# Brief 0013 — World 7b3 (phần 2): đóng mặt dưới mép cỏ

> Trạng thái: chờ làm
> Review: đầy đủ + user xem
> Model thực thi: Sonnet · medium
> Cập nhật: 2026-10-20 20:05

<!-- Planner (Opus) viết, Executor (Sonnet) làm theo ở PHIÊN KHÁC — executor không có lịch sử chat, không được tự quyết.
Trạng thái: chờ làm → đang làm → chờ review → đã duyệt; hoặc → cần Opus (executor dừng, hỏi ở mục Vướng mắc).
File được commit. -->

## Mục tiêu
Task [world bước 7](../tasks/m1-world-buoc-7-nhieu-map-minimap.md), dòng **7b3** của [world-tech](../../design/04-tech/world-tech.md#kế-hoạch-prototype-m1), phần 2 (phần 1 = brief 0012; **làm sau khi brief 0012 đã commit** — dùng chung Unity Editor).
Từ dưới nước nhìn lên, mép cỏ (`BlockMesher`) trông như tờ giấy mỏng / nhìn xuyên được vì mép chỉ có mặt trên + váy, phía dưới hở (shader `Game/Terrain` cull mặt sau). Đóng mặt dưới để nhìn từ dưới thấy mép có độ dày; nhìn từ trên **không đổi**.

## Bối cảnh cần đọc
- `client/Assets/Game/World/BlockMesher.cs`:
  - Hằng mép cỏ `:29-42`: `LipOverhang = 0.08f`, `LipRound = 0.04f`, `LipDepth = 0.22f, LipWave1 = 0.06f, LipWave2 = 0.03f`, `LipSegments = 4, LipCornerSegments = 3`; `BevelRadius = 0.125f` (`:27`, trong job là trường `R`).
  - `AddGrassLip` `:688-700`, `LipPoint` `:703-712` (`static float3 LipPoint(int3 c, int sideAxis, int sideSign, float t, float dy, float sigma)` — t = khoảng ra ngoài mặt sườn, dy = độ cao so với mặt trên, sigma = 0..1 dọc cạnh), `HasLip` `:714-721`.
  - `AddLipEdge` `:723-772`: mặt cắt hiện tại (t, dy): P0 (−R, 0) → P1 (tTop, 0) → P3 (LipOverhang, −depth); `tTop = LipOverhang − LipRound = 0.04` (`:730`). Mặt trên = quạt P0a/P0b + hàng P1; váy = tứ giác hàng P1 ↔ hàng P3 (`:746-751`); nắp hai đầu `:752-771` (tứ giác q0, q1, q3, qi với qi = (−R, −depth)).
  - `AddLipCorner` `:775-803`: mảnh góc lồi 1/4 vòng quanh trục đứng `axisLocal`, P1_k / P3_k theo `dir` = (sx·cos a, 0, sz·sin a), độ sâu `math.lerp(d0, d1, k / LipCornerSegments)`; k = 0 khớp đầu dải sườn x, k = LipCornerSegments khớp đầu dải sườn z.
  - `AddVertex(float3 p, float3 n, uint color)` `:818`; `AddOriented(int a, int b, int c, float3 want)` `:828` — tự đảo thứ tự để mặt trước hướng theo `want`.
- `client/Assets/Game/Tests/World/BlockMesherTests.cs`: `GeometricNormal(ChunkMesh m, int t)` `:55` (= cross(b − a, c − a), mặt trước); `MaxLip` `:288`; `LipOnlyOnGrassWithOpenTopAndSide` `:301`; `LipIsDeterministicAndContinuousAcrossCellsAndChunks` `:342-375`; `TrianglesFaceTheirNormals` (`:230-242`, chạy trên chunk có mép cỏ: mọi tam giác phải có pháp tuyến hình học cùng hướng tổng pháp tuyến đỉnh, `> 0`); MESHSTATS ở `ColliderIsOneQuadPerExposedFaceOnM1World` `:97-125` và `EveryM1ChunkFits16BitIndices` `:245-259`.
- `ChunkMesh` (`client/Assets/Game/World/ChunkMesh.cs`): `float3[] Vertices`, `float3[] Normals`, `uint[] Colors`, `int[] Indices`.
- Không dùng API Unity mới (chỉ Unity.Mathematics đã có trong file).

## Quyết định đã chốt (không được đổi)
1. Mặt cắt mới của mép cỏ (đơn vị ô, t ra ngoài sườn, dy so với mặt trên), đi theo vòng: **P0 (−R, 0) → P1 (tTop, 0) → P3 (LipOverhang, −depth) → B (tTop, −LipFloor) → Q (−R, −LipFloor)**. Hai mặt mới: **mặt trong của váy** P3 → B (nghiêng, quay xuống-vào trong) và **đáy phẳng** B → Q ở độ sâu cố định `LipFloor`. Lý do: đáy phẳng cùng độ sâu ở mọi dải và mọi góc → dải kề nhau và mảnh góc khớp nhau không khe; B nằm trong váy (váy ở độ sâu 0,11 có t ≥ 0,054 > 0,04) nên mặt mới không lòi ra ngoài; Q ở −R nằm trong khối (sườn khối ở độ sâu ≥ 0,11 lõm vào < 0,001 ô) nên không hở chỗ nối với sườn.
2. Hằng mới `LipFloor = 0.11f` = váy ngắn nhất (0,22 − 0,06 − 0,03 = 0,13) trừ 0,02 → mặt trong của váy không suy biến (cao ≥ 0,02).
3. Mọi đỉnh mới có pháp tuyến (0, −1, 0), màu = `color` của mép (màu mặt trên ô cỏ, như váy); hàng P3 được **thêm đỉnh mới** (không dùng chung đỉnh P3 của váy vì khác pháp tuyến). Mọi tam giác mới thêm bằng `AddOriented(..., down)`.
4. Đáy phẳng của dải = quạt từ đỉnh Q đầu dải qua cả hàng B tới Q cuối (không chia lưới Q) — không có đỉnh chữ T trong dải. Đáy phẳng của góc = quạt từ tâm trục góc ở độ sâu LipFloor.
5. Nắp hai đầu đổi thành ngũ giác đúng mặt cắt mới (q0, q1, q3, qb, qi), tam giác hoá bằng quạt từ qb (đỉnh lõm duy nhất).
6. Không đổi: mặt trên, váy, độ sâu váy, hình góc nhìn từ trên, collider, mép chỉ trên ô cỏ lộ mặt trên + sườn, `LipSegments`/`LipCornerSegments`, shader. Số tam giác mép cỏ tăng (mỗi dải +13, mỗi góc +9) — chấp nhận, Planner đo FPS điện thoại khi review.

## Phạm vi
- Được sửa: `client/Assets/Game/World/BlockMesher.cs`, `client/Assets/Game/Tests/World/BlockMesherTests.cs`; mục Kết quả / Kết quả test / Vướng mắc của brief này.
- Không được sửa: file khác (kể cả `ChunkMeshWriter`, shader, scene, design/).

## Các bước
0. Trước khi sửa: chạy EditMode `BlockMesherTests` (MCP `run_tests` lọc theo class) và chép nguyên văn 2 dòng `MESHSTATS ...` trong output của `ColliderIsOneQuadPerExposedFaceOnM1World` và `EveryM1ChunkFits16BitIndices` vào Kết quả (mốc "trước").

1. **Hằng** — `BlockMesher.cs`, ngay sau dòng `LipDepth ...` (`:37`) thêm:
   ```csharp
   /// <summary>Đáy phẳng của mép cỏ nằm dưới mặt trên chừng này (brief 0013): nông hơn váy ngắn nhất (0,13) 0,02 để mặt trong của váy không suy biến.</summary>
   public const float LipFloor = 0.11f;
   ```

2. **Test mới viết trước, phải hỏng trước khi sửa mesher** — trong `BlockMesherTests.cs`, mục "Mép cỏ (bước 5.3)", thêm helper và 2 test:
   ```csharp
   /// <summary>Tia (o, dir) cắt tam giác gần nhất (Möller–Trumbore). normal = GeometricNormal (mặt trước theo quy ước mesher).</summary>
   static bool FirstHit(ChunkMesh m, float3 o, float3 dir, out float dist, out float3 normal)
   {
       dist = float.MaxValue;
       normal = 0;
       for (int t = 0; t < m.Indices.Length; t += 3)
       {
           float3 a = m.Vertices[m.Indices[t]], b = m.Vertices[m.Indices[t + 1]], c = m.Vertices[m.Indices[t + 2]];
           float3 e1 = b - a, e2 = c - a, p = math.cross(dir, e2);
           float det = math.dot(e1, p);
           if (math.abs(det) < 1e-9f)
               continue;
           float inv = 1f / det;
           float3 s = o - a;
           float u = math.dot(s, p) * inv;
           if (u < 0f || u > 1f)
               continue;
           float3 q = math.cross(s, e1);
           float v = math.dot(dir, q) * inv;
           if (v < 0f || u + v > 1f)
               continue;
           float d = math.dot(e2, q) * inv;
           if (d > 1e-5f && d < dist)
           {
               dist = d;
               normal = GeometricNormal(m, t);
           }
       }
       return dist < float.MaxValue;
   }

   /// <summary>Điểm (x, z) dưới phần mép nhô ra của ô cỏ (4, 8, 4): 4 sườn × sigma {0,3; 0,7} × t {0,01; 0,03; 0,05; 0,07} và 4 góc × r {0,02; 0,05; 0,07} theo đường chéo 45°.</summary>
   static List<float2> LipFootprint()
   {
       var pts = new List<float2>();
       foreach (float s in new[] { 0.3f, 0.7f })
       foreach (float t in new[] { 0.01f, 0.03f, 0.05f, 0.07f })
       {
           pts.Add(new float2(5 + t, 4 + s));
           pts.Add(new float2(4 - t, 4 + s));
           pts.Add(new float2(4 + s, 5 + t));
           pts.Add(new float2(4 + s, 4 - t));
       }
       foreach (int sx in new[] { -1, 1 })
       foreach (int sz in new[] { -1, 1 })
       foreach (float r in new[] { 0.02f, 0.05f, 0.07f })
           pts.Add(new float2(4.5f + 0.5f * sx + r * 0.70710678f * sx, 4.5f + 0.5f * sz + r * 0.70710678f * sz));
       return pts;
   }

   [Test]
   public void LipIsClosedFromBelowAndUnchangedFromAbove()
   {
       // Ô cỏ đơn (4, 8, 4), mặt trên y = 9. Tia từ dưới lên vào vùng mép nhô ra phải chạm mặt quay xuống (không nhìn xuyên),
       // ở giữa đáy váy sâu nhất và đáy phẳng; tia từ trên xuống chạm mặt quay lên (mặt trên hoặc váy) như trước.
       var w = new WorldData(new WorldSize(16, 16, 16));
       w.Set(4, 8, 4, Cell.Make(BlockMaterial.Grass));
       var mesh = BlockMesher.Build(w, 0, 0, 0);
       foreach (var p in LipFootprint())
       {
           Assert.IsTrue(FirstHit(mesh, new float3(p.x, 8f, p.y), new float3(0, 1, 0), out float dUp, out float3 nUp), $"dưới {p}: không chạm");
           Assert.Less(nUp.y, 0f, $"dưới {p}: chạm mặt sau (nhìn xuyên)");
           Assert.That(8f + dUp, Is.InRange(9f - MaxLip - 1e-4f, 9f - BlockMesher.LipFloor + 1e-4f), $"dưới {p}: độ cao chạm");
           Assert.IsTrue(FirstHit(mesh, new float3(p.x, 10f, p.y), new float3(0, -1, 0), out float dDown, out float3 nDown), $"trên {p}: không chạm");
           Assert.Greater(nDown.y, 0f, $"trên {p}: chạm mặt sau");
           Assert.That(10f - dDown, Is.InRange(9f - MaxLip - 1e-4f, 9f + 1e-4f), $"trên {p}: độ cao chạm");
       }
   }

   [Test]
   public void LipEndCapMatchesClosedCrossSection()
   {
       // Ô cỏ (4, 8, 4) kề ô đá (4, 8, 5) → dải mép sườn +x và −x có nắp ở đầu z = 5 (pháp tuyến (0, 0, 1)).
       // Nắp là ngũ giác của mặt cắt mới: 5 đỉnh mỗi nắp; dưới đáy phẳng chỉ còn điểm đáy váy (t = LipOverhang).
       var w = new WorldData(new WorldSize(16, 16, 16));
       w.Set(4, 8, 4, Cell.Make(BlockMaterial.Grass));
       w.Set(4, 8, 5, Cell.Make(BlockMaterial.Stone));
       var mesh = BlockMesher.Build(w, 0, 0, 0);
       int cap = 0;
       for (int i = 0; i < mesh.Vertices.Length; i++)
       {
           float3 p = mesh.Vertices[i];
           if (!mesh.Normals[i].Equals(new float3(0, 0, 1)) || math.abs(p.z - 5f) > 1e-4f)
               continue;
           cap++;
           if (p.y < 9f - BlockMesher.LipFloor - 1e-4f)
               Assert.IsTrue(p.x >= 5f + BlockMesher.LipOverhang - 1e-4f || p.x <= 4f - BlockMesher.LipOverhang + 1e-4f, $"nắp lòi dưới đáy phẳng ở {p}");
       }
       Assert.AreEqual(2 * 5, cap, "2 nắp × 5 đỉnh");
   }
   ```
   (File đã `using System.Collections.Generic;`, `Unity.Mathematics`, `Game.Core`.) Chạy 2 test này **trước bước 3** → cả hai phải **không đạt** (ghi dòng lỗi đầu tiên vào Kết quả). Đạt ngay → dừng, `cần Opus`.

3. **`AddLipEdge`** — chèn ngay sau vòng lặp váy (sau `:751`, trước dòng `// Nắp hai đầu ...`), chép đúng:
   ```csharp
                // Mặt dưới (brief 0013): mặt trong của váy (hàng P3 → hàng B) và đáy phẳng (hàng B → F0, F1) ở độ sâu LipFloor,
                // pháp tuyến −y → nhìn từ dưới lên mép cỏ có độ dày, không nhìn xuyên. Hàng P3 thêm đỉnh mới (khác pháp tuyến váy).
                float3 down = new float3(0, -1, 0);
                int firstB = Out.Vertices.Length;
                for (int k = 0; k <= LipSegments; k++)
                {
                    float sigma = (float)k / LipSegments;
                    float depth = LipDepthAt(g[edgeAxis] + sigma, sideAxis, sideSign, plane, top);
                    AddVertex(LipPoint(c, sideAxis, sideSign, LipOverhang, -depth, sigma), down, color); // P3'_k = firstB + 2k
                    AddVertex(LipPoint(c, sideAxis, sideSign, tTop, -LipFloor, sigma), down, color);     // B_k = firstB + 2k + 1
                }
                int f0 = AddVertex(LipPoint(c, sideAxis, sideSign, -R, -LipFloor, 0), down, color);
                int f1 = AddVertex(LipPoint(c, sideAxis, sideSign, -R, -LipFloor, 1), down, color);
                for (int k = 0; k < LipSegments; k++)
                {
                    int a = firstB + 2 * k, b = firstB + 2 * (k + 1);
                    AddOriented(a, b, b + 1, down);
                    AddOriented(a, b + 1, a + 1, down);
                    AddOriented(f0, a + 1, b + 1, down);
                }
                AddOriented(f0, firstB + 2 * LipSegments + 1, f1, down);
   ```
   Xong khi: biên dịch không lỗi (tên `f0`/`f1`/`down`/`firstB` không trùng biến nào ở cùng phạm vi; nếu trùng → `cần Opus`).

4. **Nắp** — trong vòng lặp nắp (`:762-770`), thay 4 dòng `int q0 ... int qi ...` + 2 dòng `AddOriented(q0, ...)` bằng:
   ```csharp
                    int q0 = AddVertex(LipPoint(c, sideAxis, sideSign, -R, 0, sigma), capN, color);
                    int q1 = AddVertex(LipPoint(c, sideAxis, sideSign, tTop, 0, sigma), capN, color);
                    int q3 = AddVertex(LipPoint(c, sideAxis, sideSign, LipOverhang, -depth, sigma), capN, color);
                    int qb = AddVertex(LipPoint(c, sideAxis, sideSign, tTop, -LipFloor, sigma), capN, color);
                    int qi = AddVertex(LipPoint(c, sideAxis, sideSign, -R, -LipFloor, sigma), capN, color);
                    // Ngũ giác mặt cắt (brief 0013), lõm ở qb → quạt từ qb.
                    AddOriented(qb, qi, q0, capN);
                    AddOriented(qb, q0, q1, capN);
                    AddOriented(qb, q1, q3, capN);
   ```
   Sửa luôn chú thích `/// <summary>` hoặc comment của `AddLipEdge` nếu có nhắc mặt cắt cũ — không có thì thôi.

5. **`AddLipCorner`** — chèn cuối hàm, sau vòng lặp tam giác (sau `:802`, trước `}` đóng hàm), chép đúng:
   ```csharp
                // Mặt dưới (brief 0013): mặt trong của váy + đáy phẳng 1/4 tròn quanh trục góc; k = 0 và k = LipCornerSegments
                // trùng đỉnh P3'/B ở đầu hai dải sườn kề → không khe.
                float3 down = new float3(0, -1, 0);
                int floorCenter = AddVertex(axisLocal + new float3(0, -LipFloor, 0), down, color);
                int firstD = Out.Vertices.Length;
                for (int k = 0; k <= LipCornerSegments; k++)
                {
                    float a = (float)k / LipCornerSegments * 0.5f * math.PI;
                    float3 dir = new float3(sx * math.cos(a), 0, sz * math.sin(a));
                    float depth = math.lerp(d0, d1, (float)k / LipCornerSegments);
                    AddVertex(axisLocal + dir * LipOverhang + new float3(0, -depth, 0), down, color); // P3'_k = firstD + 2k
                    AddVertex(axisLocal + dir * tTop + new float3(0, -LipFloor, 0), down, color);     // B_k = firstD + 2k + 1
                }
                for (int k = 0; k < LipCornerSegments; k++)
                {
                    int a = firstD + 2 * k, b = firstD + 2 * (k + 1);
                    AddOriented(a, b, b + 1, down);
                    AddOriented(a, b + 1, a + 1, down);
                    AddOriented(floorCenter, a + 1, b + 1, down);
                }
   ```
   Cập nhật summary của class (`:11-18`) không cần. Cập nhật comment hằng `LipOverhang` không cần.

6. **Sửa test cũ `LipIsDeterministicAndContinuousAcrossCellsAndChunks`** (`:342-375`) — mặt mới thêm đỉnh trùng vị trí điểm đáy váy (P3') và đỉnh B. Đổi:
   - dòng `:363` thành chỉ lấy điểm đáy váy (t = LipOverhang):
     `bool skirt = (p.z < 4f - BlockMesher.LipOverhang + 1e-3f || p.z > 5f + BlockMesher.LipOverhang - 1e-3f) && p.y < 9f - 1e-3f;`
   - dòng `:374` thành `Assert.AreEqual(4, kv.Value, "điểm đáy váy ở ranh giới phải trùng giữa hai dải kề nhau (kể cả qua biên chunk x = 16) — mỗi dải 2 đỉnh: váy + mặt dưới");`
   - Giữ nguyên `Assert.AreEqual(5 * 2, at.Count, ...)` ở `:372`.

7. Chạy test theo mục "Lệnh executor chạy". Chép 2 dòng MESHSTATS mới (mốc "sau") vào Kết quả, kèm phần trăm tăng của "mép cỏ thêm" và "tổng". Commit `world 7b3: đóng mặt dưới mép cỏ (brief 0013)`. Điền Kết quả (base commit, giờ bắt đầu/kết thúc, model + effort, file đổi, lệch so với brief).

## Tiêu chí xong
- 2 test mới không đạt trước bước 3 và đạt sau bước 5.
- Toàn bộ EditMode đạt (sau brief 0012 là 269 = 260 + 9; con số thật ghi theo output), toàn bộ PlayMode đạt.
- MESHSTATS trước/sau đã ghi; mọi chunk world M1 vẫn ≤ 65.535 đỉnh (`EveryM1ChunkFits16BitIndices` đạt).

## Test sau khi xong

### Phần cần test
- Mặt dưới dải mép + góc (bước 3, 5) · `LipIsClosedFromBelowAndUnchangedFromAbove` · 56 điểm: từ dưới chạm mặt quay xuống trong [9 − 0,31; 8,89]; từ trên chạm mặt quay lên trong [8,69; 9].
- Nắp đầu dải (bước 4) · `LipEndCapMatchesClosedCrossSection` · 10 đỉnh nắp, không đỉnh nào lòi dưới đáy phẳng trừ điểm đáy váy.
- Liền giữa ô kề và qua biên chunk (bước 3 + 6) · `LipIsDeterministicAndContinuousAcrossCellsAndChunks` (đã sửa) · 10 điểm, mỗi điểm 4 đỉnh.
- Chiều tam giác mới (bước 3–5) · `TrianglesFaceTheirNormals` · đạt (mọi tam giác mới quay theo pháp tuyến −y).
- Regression mép cỏ cũ · `LipOnlyOnGrassWithOpenTopAndSide`, `LipDepthStaysInRange` · đạt không sửa.
- Regression mesh khối, seam, dựng lại tăng dần, ghi mesh 16 bit · `BlockMesherTests` còn lại, `ChunkSeamTests`, `IncrementalRebuildTests`, `EveryM1ChunkFits16BitIndices`, `WritesUnityMeshWithColorsAndCollider` · đạt.
- Regression client · PlayMode cả bộ (nạp dần chunk world v3 ghi mesh qua `ChunkMeshWriter` — lỗi > 65.535 đỉnh sẽ ném exception) · đạt.
- Số tam giác · MESHSTATS trước/sau · ghi số; "mép cỏ thêm" dự kiến khoảng gấp đôi (mỗi dải 13 → 26 tam giác, mỗi góc 9 → 18) — lệch xa (vd > ×2,5) thì ghi chú, không dừng.

### Lệnh executor chạy
1. MCP `run_tests` EditMode lọc `BlockMesherTests` (bước 0, mốc trước).
2. Sau bước 2: MCP `run_tests` EditMode lọc 2 test mới → phải không đạt.
3. Sau bước 6: MCP `refresh_unity` → `read_console` (error) 0 lỗi; `run_tests` EditMode cả bộ → dòng tổng kết.
4. MCP `run_tests` PlayMode cả bộ → dòng tổng kết. Dọn: `git checkout -- client/ProjectSettings/EditorSettings.asset`.
5. `git status --short` — chỉ còn 2 file trong Phạm vi (+ brief); commit.

### User tự test
Mở scene TerrainView, Play (chuột): lặn xuống nước cạnh một bờ cỏ (giống ảnh `client/Logs/water_below_up.png`), nhìn lên mép cỏ:
1. Mép cỏ nhìn từ dưới có **mặt đáy và độ dày** (không còn như tờ giấy, không nhìn xuyên lên trời).
2. Lên khỏi nước, nhìn mép cỏ từ trên và từ ngang: **giống hệt trước** (váy lượn sóng, góc tròn).
3. Đào/đặt vài ô cỏ ở bờ → mép cỏ mới vẫn kín từ dưới.
(Planner đo FPS điện thoại bằng Bench khi review — user không cần làm.)

## Dừng và chuyển lại Opus khi
- Brief mơ hồ hoặc mâu thuẫn với code thật (số dòng lệch nhiều, biến trùng tên, mặt cắt khác mô tả).
- 2 test mới đạt ngay trước khi sửa mesher, hoặc vẫn không đạt sau bước 5.
- Bất kỳ test cũ nào ngoài `LipIsDeterministicAndContinuousAcrossCellsAndChunks` hỏng.
- Cần một quyết định thuộc AGENTS.md §3.4 hoặc ngoài "Quyết định đã chốt".
- Cùng một lỗi sửa 2 lần vẫn chưa qua.
- Phải sửa file ngoài "Phạm vi".

---
<!-- Từ đây trở xuống: EXECUTOR ghi. -->

## Kết quả

## Kết quả test

## Vướng mắc

---
<!-- PLANNER ghi khi review. -->

## Review
