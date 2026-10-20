# Brief 0014 — World 7b4: mép cỏ bo tròn ôm khối

> Trạng thái: chờ review
> Review: đầy đủ + user xem
> Model thực thi: Sonnet · medium
> Cập nhật: 2026-10-20 21:57

<!-- Planner (Opus) viết, Executor (Sonnet) làm theo ở PHIÊN KHÁC — executor không có lịch sử chat, không được tự quyết.
Trạng thái: chờ làm → đang làm → chờ review → đã duyệt; hoặc → cần Opus (executor dừng, hỏi ở mục Vướng mắc).
File được commit. -->

## Mục tiêu
Task [world bước 7](../tasks/m1-world-buoc-7-nhieu-map-minimap.md), dòng **7b4** của [world-tech](../../design/04-tech/world-tech.md#kế-hoạch-prototype-m1) (user chốt 2026-10-20 khi xem brief 0013).
Mép cỏ hiện là một váy phẳng nhô 0,08 ô với pháp tuyến phẳng, nhìn ngang thì mỏng như tờ bìa. Đổi mặt cắt thành **khối bo tròn như cái gối**: mặt trên cuộn xuống, mặt trước phồng ra (nhô 0,12), đáy lượn sóng cuộn ngược vào vách (dày trung bình 0,28), pháp tuyến mượt theo đường cong. Vẫn liền giữa các ô kề và qua biên chunk, vẫn kín khi nhìn từ dưới (như brief 0013).

## Bối cảnh cần đọc
- `client/Assets/Game/World/BlockMesher.cs` (dòng theo commit `3e391e9`):
  - Hằng mép cỏ `:31-44`: `LipOverhang = 0.08f` (`:33`), `LipRound = 0.04f` (`:35`), `LipDepth = 0.22f, LipWave1 = 0.06f, LipWave2 = 0.03f` (`:37`), `LipFloor = 0.11f` (`:39`), `LipSegments = 4, LipCornerSegments = 3` (`:44`). `BevelRadius = 0.125f` (`:29`; trong job là trường `R`).
  - `public static float LipDepthAt(float w, int sideAxis, int sideSign, int plane, int top)` `:51-57` — static của lớp, job Burst gọi được. Hàm mới `LipProfile` đặt ngay sau hàm này, cùng kiểu.
  - Trong job: `AddGrassLip` `:690-702` (không đổi), `static float3 LipPoint(int3 c, int sideAxis, int sideSign, float t, float dy, float sigma)` `:705-713` (không đổi; t = khoảng ra ngoài mặt sườn, dy = độ cao so với mặt trên, sigma = 0..1 dọc cạnh), `HasLip` `:716-723` (không đổi).
  - `void AddLipEdge(int3 c, int3 g, int sideAxis, int sideSign, uint color)` `:725-798` và `void AddLipCorner(int3 c, int3 g, int sx, int sz, uint color)` `:800-849` (gồm dòng `/// <summary>` ở `:800`) — **thay toàn bộ** ở bước 3–4.
  - `int AddVertex(float3 p, float3 n, uint color)` `:865`; `void AddOriented(int a, int b, int c, float3 want)` `:875` (tự đảo thứ tự để cross(b − a, c − a) cùng hướng `want`). `Out.Vertices`, `Out.Normals` là `NativeList<float3>` (`:303`) — đọc bằng chỉ số `Out.Normals[i]`.
- `client/Assets/Game/Tests/World/BlockMesherTests.cs`:
  - `MaxLip` `:288`; `LipOnlyOnGrassWithOpenTopAndSide` `:301` (giữ nguyên, phải vẫn đạt); `FirstHit` `:342`; `LipFootprint` `:372-389`; `LipIsClosedFromBelowAndUnchangedFromAbove` `:391-407`; `LipEndCapMatchesClosedCrossSection` `:410-430`; `LipIsDeterministicAndContinuousAcrossCellsAndChunks` `:432-467`; `TrianglesFaceTheirNormals` `:230-242`; MESHSTATS ở `:119` (`ColliderIsOneQuadPerExposedFaceOnM1World`) và `:258` (`EveryM1ChunkFits16BitIndices`).
  - `MeshChecks.Key(float3 p)` (`client/Assets/Game/Tests/World/MeshChecks.cs:11`) làm tròn toạ độ ×1024.
- Không dùng API Unity mới (chỉ `Unity.Mathematics` đã có: `float2`, `float3`, `math.cos/sin/lerp/normalize`).

## Quyết định đã chốt (không được đổi)
1. **Mặt cắt** (đơn vị ô; t ra ngoài mặt sườn, dy so với mặt trên; depth = độ sâu đáy ở mặt trước = `LipDepthAt`), 8 điểm theo thứ tự, mỗi điểm có pháp tuyến (nt, ny) riêng → shading mượt:
   - A0..A3: **cuộn trên** = 1/4 cung tâm (LipOverhang − LipTopRound, −LipTopRound), bán kính LipTopRound, góc 90° → 60° → 30° → 0°; pháp tuyến = hướng bán kính. A0 = (0,04; 0) hướng lên, A3 = (0,12; −0,08) hướng ra.
   - C0..C2: **cuộn dưới** = 1/4 cung tâm (LipOverhang − LipBottomRound, −(depth − LipBottomRound)), bán kính LipBottomRound, góc 0° → −45° → −90°. C0 = (0,12; −(depth − 0,06)) hướng ra, C2 = (0,06; −depth) hướng xuống. A3 → C0 là mặt trước thẳng đứng.
   - W = (0; −LipFloor), pháp tuyến (0, −1): **chạm vách**. C2 → W là mặt dưới nghiêng lên vào vách (vì depth ≥ 0,205 > LipFloor = 0,16).
   - Ngoài 8 điểm: mặt trên vẫn quạt từ P0 (−R, 0) như cũ; **đáy trong** = quạt từ F (−R, −LipFloor) qua hàng W (giữ lý do của 0013: phủ phần khối lõm do bo cạnh đứng ở góc, nhìn từ dưới không xuyên).
2. **Hằng:** `LipOverhang = 0.12f`; bỏ `LipRound`, thêm `LipTopRound = 0.08f, LipBottomRound = 0.06f`; `LipDepth = 0.28f, LipWave1 = 0.05f, LipWave2 = 0.025f` (depth 0,205–0,355); `LipFloor = 0.16f`; thêm `LipProfilePoints = 8`. `LipSegments`, `LipCornerSegments`, công thức `LipDepthAt` không đổi.
3. Mặt cắt nằm ở **một hàm** `public static void LipProfile(int i, float depth, out float2 p, out float2 n)` của lớp — dải, góc, nắp đều gọi hàm này → đỉnh ở chỗ nối dải ↔ dải, dải ↔ góc trùng nhau từng bit.
4. Dải: lưới đỉnh 8 hàng mặt cắt × (LipSegments + 1) cột, **mỗi điểm một đỉnh** (dùng chung cho hai tứ giác kề, pháp tuyến mượt). Pháp tuyến 3D = outward·nt + up·ny.
5. Góc lồi: quét 7 hàng A0..C2 1/4 vòng như cũ (dir = (sx·cos a, 0, sz·sin a), depth = lerp(d0, d1)); pháp tuyến 3D = dir·nt + up·ny. Hàng W của góc thu về **một đỉnh trên trục** (axisLocal, −LipFloor) → mặt dưới nghiêng của góc là quạt từ đỉnh này. Mặt trên quạt từ tâm trục như cũ.
6. Nắp đầu dải: đa giác đúng mặt cắt kín (−R, 0) → A0..A3 → C0..C2 → W → (−R, −LipFloor), 10 đỉnh, pháp tuyến phẳng `capN`, tam giác hoá bằng quạt từ W (đỉnh lõm duy nhất, nằm trong bao lồi của các đỉnh còn lại). Điều kiện có nắp không đổi.
7. Không đổi: collider, mép chỉ có trên ô cỏ lộ mặt trên + sườn, màu (`color` của mép), shader, `AddGrassLip`, `LipPoint`, `HasLip`. Số tam giác mép cỏ tăng (dải 26 → 66, góc 18 → 42, nắp 3 → 8; ước ~×2,5 mép cỏ, ~+3% tổng) — chấp nhận, Planner đo FPS điện thoại khi review.
8. Giờ bắt đầu/kết thúc lấy bằng lệnh `date "+%Y-%m-%d %H:%M"` lúc đó, **không ước đoán** (hai brief trước ghi sai giờ).

## Phạm vi
- Được sửa: `client/Assets/Game/World/BlockMesher.cs`, `client/Assets/Game/Tests/World/BlockMesherTests.cs`; mục Kết quả / Kết quả test / Vướng mắc của brief này.
- Không được sửa: file khác (kể cả `ChunkMeshWriter`, shader, scene, `design/`); các test khác trong `BlockMesherTests.cs` ngoài 3 test nêu ở bước 2.

## Các bước
0. Chạy `date "+%Y-%m-%d %H:%M"` → ghi "Bắt đầu" vào Kết quả. Trước khi sửa: chạy EditMode `BlockMesherTests` (MCP `run_tests`, lọc theo class) và chép nguyên văn 2 dòng `MESHSTATS ...` vào Kết quả (mốc "trước").

1. **Hằng + hàm mặt cắt** — `BlockMesher.cs`:
   - Thay `:33-39` (từ dòng `/// <summary>Mặt trên ô cỏ nhô ra khỏi sườn (ô).</summary>` tới hết dòng `public const float LipFloor = 0.11f;`) bằng:
   ```csharp
        /// <summary>Mép cỏ nhô ra khỏi sườn ở chỗ phồng nhất — mặt trước (ô, brief 0014).</summary>
        public const float LipOverhang = 0.12f;
        /// <summary>Bán kính cuộn trên (mặt trên cuộn xuống mặt trước) và cuộn dưới (mặt trước cuộn vào đáy) của mặt cắt mép cỏ (brief 0014).</summary>
        public const float LipTopRound = 0.08f, LipBottomRound = 0.06f;
        /// <summary>Độ sâu đáy mép cỏ ở mặt trước: LipDepth ± LipWave1 ± LipWave2 (lượn sóng theo toạ độ dọc cạnh) = 0,205–0,355.</summary>
        public const float LipDepth = 0.28f, LipWave1 = 0.05f, LipWave2 = 0.025f;
        /// <summary>Mặt dưới mép cỏ chạm vách ở độ sâu này rồi chạy phẳng vào trong khối tới −R (brief 0014); nông hơn đáy nông nhất (0,205) → mặt dưới nghiêng lên vào vách.</summary>
        public const float LipFloor = 0.16f;
        /// <summary>Số điểm mặt cắt mép cỏ (LipProfile): A0..A3 cuộn trên, C0..C2 cuộn dưới, W chạm vách.</summary>
        public const int LipProfilePoints = 8;
   ```
   - Ngay sau hàm `LipDepthAt` (sau dấu `}` ở `:57`) thêm:
   ```csharp

        /// <summary>
        /// Điểm thứ <paramref name="i"/> (0..LipProfilePoints − 1) của mặt cắt mép cỏ khi đáy mặt trước sâu <paramref name="depth"/> (brief 0014):
        /// p = (t ra ngoài mặt sườn, dy so với mặt trên), n = pháp tuyến đơn vị trong mặt cắt (theo t, theo y).
        /// A0..A3 cuộn trên (90° → 0°), C0..C2 cuộn dưới (0° → −90°), W chạm vách ở (0, −LipFloor).
        /// </summary>
        public static void LipProfile(int i, float depth, out float2 p, out float2 n)
        {
            if (i <= 3)
            {
                float a = (3 - i) * (math.PI / 6f);
                n = new float2(math.cos(a), math.sin(a));
                p = new float2(LipOverhang - LipTopRound, -LipTopRound) + LipTopRound * n;
            }
            else if (i <= 6)
            {
                float a = -(i - 4) * (math.PI / 4f);
                n = new float2(math.cos(a), math.sin(a));
                p = new float2(LipOverhang - LipBottomRound, -(depth - LipBottomRound)) + LipBottomRound * n;
            }
            else
            {
                n = new float2(0f, -1f);
                p = new float2(0f, -LipFloor);
            }
        }
   ```
   Xong khi: biên dịch được sau bước 3–4 (bước 1 đứng riêng sẽ lỗi vì `LipRound` còn dùng ở `AddLipEdge`/`AddLipCorner` — bình thường, làm tiếp).

2. **Test — sửa 3 test, thêm 1 test** trong `BlockMesherTests.cs`, mục "Mép cỏ (bước 5.3)":
   a. `LipFootprint` (`:372-389`): đổi mảng t thành `new[] { 0.01f, 0.03f, 0.05f, 0.07f, 0.09f, 0.11f }`, mảng r thành `new[] { 0.02f, 0.05f, 0.08f, 0.11f }`; sửa summary thành `4 sườn × sigma {0,3; 0,7} × t {0,01 … 0,11} và 4 góc × r {0,02; 0,05; 0,08; 0,11} theo đường chéo 45° (mép nhô 0,12 — brief 0014).` Thân `LipIsClosedFromBelowAndUnchangedFromAbove` giữ nguyên.
   b. `LipEndCapMatchesClosedCrossSection`: sửa 2 dòng chú thích đầu thành
      `// Ô cỏ (4, 8, 4) kề ô đá (4, 8, 5) → dải mép sườn +x và −x có nắp ở đầu z = 5 (pháp tuyến (0, 0, 1)).`
      `// Nắp là đa giác mặt cắt kín (brief 0014): LipProfilePoints + 2 đỉnh mỗi nắp; dưới độ sâu LipFloor chỉ có cuộn dưới (t ≥ LipOverhang − LipBottomRound).`
      Trong vòng lặp, thay điều kiện `Assert.IsTrue(...)` thành:
      ```csharp
                    Assert.IsTrue(p.x >= 5f + BlockMesher.LipOverhang - BlockMesher.LipBottomRound - 1e-4f || p.x <= 4f - BlockMesher.LipOverhang + BlockMesher.LipBottomRound + 1e-4f, $"nắp lòi dưới đáy ở {p}");
      ```
      và dòng cuối thành `Assert.AreEqual(2 * (BlockMesher.LipProfilePoints + 2), cap, "2 nắp × (LipProfilePoints + 2) đỉnh");`
   c. `LipIsDeterministicAndContinuousAcrossCellsAndChunks`: sửa chú thích dòng `// Đỉnh của váy cỏ ...` thành `// Đỉnh mặt trước của mép cỏ (t = LipOverhang: A3 và C0 của mặt cắt, thấp hơn mặt trên) tại ranh giới hai ô x = 14..18: phải có ở cả dải bên trái lẫn bên phải.`; thay 3 dòng assert cuối thành:
      ```csharp
            Assert.AreEqual(5 * 2 * 2, at.Count, "mỗi ranh giới x = 14..18, mỗi sườn có 2 điểm mặt trước (A3, C0)");
            foreach (var kv in at)
                Assert.AreEqual(2, kv.Value, "điểm mặt trước ở ranh giới phải trùng giữa hai dải kề nhau (kể cả qua biên chunk x = 16) — mỗi dải 1 đỉnh mỗi điểm");
      ```
   d. Thêm test mới ngay sau `LipDepthStaysInRange`:
      ```csharp
        [Test]
        public void LipProfileIsRoundedPillow()
        {
            // Mặt cắt (brief 0014): cuộn trên từ mặt trên xuống mặt trước, mặt trước thẳng ở t = LipOverhang, cuộn dưới vào đáy, mặt dưới nghiêng lên chạm vách.
            float lo = BlockMesher.LipOverhang, rt = BlockMesher.LipTopRound, rb = BlockMesher.LipBottomRound;
            float minDepth = BlockMesher.LipDepth - BlockMesher.LipWave1 - BlockMesher.LipWave2;
            foreach (float depth in new[] { minDepth, BlockMesher.LipDepth, MaxLip })
            {
                var p = new float2[BlockMesher.LipProfilePoints];
                var n = new float2[BlockMesher.LipProfilePoints];
                for (int i = 0; i < p.Length; i++)
                {
                    BlockMesher.LipProfile(i, depth, out p[i], out n[i]);
                    Assert.AreEqual(1f, math.length(n[i]), 1e-5f, $"depth {depth}, điểm {i}: pháp tuyến đơn vị");
                    Assert.That(p[i].x, Is.InRange(-1e-5f, lo + 1e-5f), $"depth {depth}, điểm {i}: t trong [0, LipOverhang]");
                    if (i > 0 && i < 7)
                        Assert.Less(p[i].y, p[i - 1].y + 1e-6f, $"depth {depth}, điểm {i}: A0..C2 đi xuống");
                }
                AssertNear(new float2(lo - rt, 0f), p[0], "A0"); AssertNear(new float2(0f, 1f), n[0], "n A0");
                AssertNear(new float2(lo, -rt), p[3], "A3"); AssertNear(new float2(1f, 0f), n[3], "n A3");
                AssertNear(new float2(lo, -(depth - rb)), p[4], "C0"); AssertNear(new float2(1f, 0f), n[4], "n C0");
                AssertNear(new float2(lo - rb, -depth), p[6], "C2"); AssertNear(new float2(0f, -1f), n[6], "n C2");
                AssertNear(new float2(0f, -BlockMesher.LipFloor), p[7], "W"); AssertNear(new float2(0f, -1f), n[7], "n W");
                Assert.Greater(p[7].y, p[6].y + 0.04f, $"depth {depth}: mặt dưới nghiêng lên vào vách");
            }
        }

        static void AssertNear(float2 want, float2 got, string what)
        {
            Assert.AreEqual(want.x, got.x, 1e-5f, $"{what}.x");
            Assert.AreEqual(want.y, got.y, 1e-5f, $"{what}.y");
        }
      ```
   Xong khi: file test viết xong (chưa chạy được tới khi mesher biên dịch).

3. **Dải** — thay toàn bộ `AddLipEdge` (`:725-798`, từ dòng `void AddLipEdge(` tới dấu `}` đóng hàm) bằng:
   ```csharp
            void AddLipEdge(int3 c, int3 g, int sideAxis, int sideSign, uint color)
            {
                int edgeAxis = sideAxis == 0 ? 2 : 0;
                int plane = g[sideAxis] + (sideSign > 0 ? 1 : 0), top = g.y + 1;
                float3 up = new float3(0, 1, 0), down = new float3(0, -1, 0), outward = 0;
                outward[sideAxis] = sideSign;

                // Mặt cắt bo tròn (brief 0014): lưới LipProfilePoints hàng × (LipSegments + 1) cột; đỉnh (i, k) = first + i·row + k,
                // mỗi điểm một đỉnh dùng chung cho hai tứ giác kề → pháp tuyến mượt theo đường cong.
                int row = LipSegments + 1;
                int first = Out.Vertices.Length;
                for (int i = 0; i < LipProfilePoints; i++)
                    for (int k = 0; k <= LipSegments; k++)
                    {
                        float sigma = (float)k / LipSegments;
                        float depth = LipDepthAt(g[edgeAxis] + sigma, sideAxis, sideSign, plane, top);
                        LipProfile(i, depth, out float2 p, out float2 n);
                        AddVertex(LipPoint(c, sideAxis, sideSign, p.x, p.y, sigma), outward * n.x + up * n.y, color);
                    }
                // Mặt trên: quạt từ P0a qua P0b và hàng A0.
                int p0a = AddVertex(LipPoint(c, sideAxis, sideSign, -R, 0, 0), up, color);
                int p0b = AddVertex(LipPoint(c, sideAxis, sideSign, -R, 0, 1), up, color);
                AddOriented(p0a, p0b, first + LipSegments, up);
                for (int k = LipSegments; k > 0; k--)
                    AddOriented(p0a, first + k, first + k - 1, up);
                // Mặt cong: tứ giác giữa hàng i và i + 1 (A0 → … → C2 → W).
                for (int i = 0; i + 1 < LipProfilePoints; i++)
                    for (int k = 0; k < LipSegments; k++)
                    {
                        int a = first + i * row + k, b = a + 1, a2 = a + row, b2 = b + row;
                        float3 want = Out.Normals[a] + Out.Normals[b2];
                        AddOriented(a, b, b2, want);
                        AddOriented(a, b2, a2, want);
                    }
                // Đáy trong: quạt từ F0 (−R, −LipFloor) qua hàng W tới F1 — phủ phần khối lõm do bo cạnh, nhìn từ dưới không xuyên.
                int w = first + (LipProfilePoints - 1) * row;
                int f0 = AddVertex(LipPoint(c, sideAxis, sideSign, -R, -LipFloor, 0), down, color);
                int f1 = AddVertex(LipPoint(c, sideAxis, sideSign, -R, -LipFloor, 1), down, color);
                for (int k = 0; k < LipSegments; k++)
                    AddOriented(f0, w + k, w + k + 1, down);
                AddOriented(f0, w + LipSegments, f1, down);
                // Nắp hai đầu: không nối tiếp ô bên cạnh và không phải góc lồi (góc lồi có mảnh 1/4 vòng).
                for (int end = 0; end < 2; end++)
                {
                    int e = end == 0 ? -1 : 1;
                    int3 next = g;
                    next[edgeAxis] += e;
                    if (HasLip(next, sideAxis, sideSign) || !IsSolid(next))
                        continue;
                    float sigma = end;
                    float depth = LipDepthAt(g[edgeAxis] + sigma, sideAxis, sideSign, plane, top);
                    float3 capN = 0;
                    capN[edgeAxis] = e;
                    // Đa giác mặt cắt kín: (−R, 0) → A0..A3 → C0..C2 → W → (−R, −LipFloor); quạt từ W (đỉnh lõm duy nhất).
                    int q0 = AddVertex(LipPoint(c, sideAxis, sideSign, -R, 0, sigma), capN, color);
                    for (int i = 0; i < LipProfilePoints; i++)
                    {
                        LipProfile(i, depth, out float2 p, out float2 n);
                        AddVertex(LipPoint(c, sideAxis, sideSign, p.x, p.y, sigma), capN, color); // q0 + 1 + i
                    }
                    int qi = AddVertex(LipPoint(c, sideAxis, sideSign, -R, -LipFloor, sigma), capN, color);
                    int qw = q0 + LipProfilePoints;
                    AddOriented(qw, qi, q0, capN);
                    for (int i = 0; i + 1 < LipProfilePoints; i++)
                        AddOriented(qw, q0 + i, q0 + i + 1, capN);
                }
            }
   ```

4. **Góc lồi** — thay toàn bộ `AddLipCorner` cùng dòng summary của nó (`:800-849`) bằng:
   ```csharp
            /// <summary>Mảnh góc lồi: quét mặt cắt mép cỏ (A0..C2) 1/4 vòng quanh trục đứng qua góc ô; độ sâu đáy nội suy giữa hai đầu dải; W thu về một đỉnh trên trục.</summary>
            void AddLipCorner(int3 c, int3 g, int sx, int sz, uint color)
            {
                float3 axisLocal = new float3(c.x + (sx > 0 ? 1 : 0), c.y + 1, c.z + (sz > 0 ? 1 : 0));
                int cornerX = g.x + (sx > 0 ? 1 : 0), cornerZ = g.z + (sz > 0 ? 1 : 0), top = g.y + 1;
                // Dải sườn x chạy dọc z (w = z góc); dải sườn z chạy dọc x (w = x góc).
                float d0 = LipDepthAt(cornerZ, 0, sx, cornerX, top);
                float d1 = LipDepthAt(cornerX, 2, sz, cornerZ, top);
                float3 up = new float3(0, 1, 0), down = new float3(0, -1, 0);
                int center = AddVertex(axisLocal, up, color);
                int wCenter = AddVertex(axisLocal + new float3(0, -LipFloor, 0), down, color);
                // Lưới: hàng i = 0..LipProfilePoints − 2 (A0..C2) × cột k = 0..LipCornerSegments; đỉnh (i, k) = first + i·col + k.
                // k = 0 trùng đầu dải sườn x, k = LipCornerSegments trùng đầu dải sườn z.
                int rows = LipProfilePoints - 1, col = LipCornerSegments + 1;
                int first = Out.Vertices.Length;
                for (int i = 0; i < rows; i++)
                    for (int k = 0; k <= LipCornerSegments; k++)
                    {
                        float a = (float)k / LipCornerSegments * 0.5f * math.PI;
                        float3 dir = new float3(sx * math.cos(a), 0, sz * math.sin(a));
                        float depth = math.lerp(d0, d1, (float)k / LipCornerSegments);
                        LipProfile(i, depth, out float2 p, out float2 n);
                        AddVertex(axisLocal + dir * p.x + new float3(0, p.y, 0), dir * n.x + up * n.y, color);
                    }
                // Mặt trên: quạt từ tâm trục qua hàng A0.
                for (int k = 0; k < LipCornerSegments; k++)
                    AddOriented(center, first + k, first + k + 1, up);
                // Mặt cong giữa các hàng A0 → … → C2.
                for (int i = 0; i + 1 < rows; i++)
                    for (int k = 0; k < LipCornerSegments; k++)
                    {
                        int a = first + i * col + k, b = a + 1, a2 = a + col, b2 = b + col;
                        float3 want = Out.Normals[a] + Out.Normals[b2];
                        AddOriented(a, b, b2, want);
                        AddOriented(a, b2, a2, want);
                    }
                // Mặt dưới nghiêng C2 → W: quạt từ đỉnh W trên trục.
                int c2 = first + (rows - 1) * col;
                for (int k = 0; k < LipCornerSegments; k++)
                    AddOriented(wCenter, c2 + k, c2 + k + 1, down);
            }
   ```
   Xong khi: `grep -n "LipRound\|tTop" client/Assets/Game/World/BlockMesher.cs` không còn kết quả; MCP `refresh_unity` → `read_console` (error) 0 lỗi biên dịch.

5. Chạy EditMode `BlockMesherTests` → tất cả đạt (gồm `LipProfileIsRoundedPillow`, 3 test sửa ở bước 2, `LipOnlyOnGrassWithOpenTopAndSide`, `TrianglesFaceTheirNormals`, `EveryM1ChunkFits16BitIndices`). Chép 2 dòng `MESHSTATS ...` mới vào Kết quả (mốc "sau").

## Tiêu chí xong
- `LipProfileIsRoundedPillow` đạt; 3 test sửa đạt; toàn bộ EditMode đạt (trước brief: 271, sau: 272) và PlayMode đạt (19).
- `BlockMesher.cs` không còn `LipRound`, `tTop`; `LipProfile` là hàm duy nhất tính mặt cắt (dải, góc, nắp đều gọi).
- Kết quả có 2 cặp dòng MESHSTATS trước/sau; số đỉnh lớn nhất/chunk vẫn ≤ 65.535 (test `EveryM1ChunkFits16BitIndices` đạt).

## Test sau khi xong

### Phần cần test
- Hằng + `LipProfile` (bước 1) · EditMode `LipProfileIsRoundedPillow`, `LipDepthStaysInRange` · 8 điểm đúng toạ độ/pháp tuyến ở 3 độ sâu; depth trong 0,205–0,355.
- Dải mép mới (bước 3) · EditMode `LipOnlyOnGrassWithOpenTopAndSide` (nhô đúng 0,12, không sâu quá MaxLip), `LipIsDeterministicAndContinuousAcrossCellsAndChunks` (tất định; A3, C0 trùng ở ranh giới ô và biên chunk) · đạt.
- Kín từ dưới, không đổi hướng từ trên, cả dải và góc (bước 3–4) · EditMode `LipIsClosedFromBelowAndUnchangedFromAbove` với footprint mới tới t = 0,11 / r = 0,11 · tia từ dưới chạm mặt quay xuống trong [9 − MaxLip, 9 − LipFloor]; tia từ trên chạm mặt quay lên.
- Nắp đầu dải (bước 3) · EditMode `LipEndCapMatchesClosedCrossSection` · 2 × 10 đỉnh nắp; dưới LipFloor chỉ có điểm cuộn dưới.
- Hướng tam giác + pháp tuyến mượt (bước 3–4) · EditMode `TrianglesFaceTheirNormals` · đạt.
- Giới hạn chỉ số 16-bit + số tam giác (bước 3–4) · EditMode `EveryM1ChunkFits16BitIndices`, `ColliderIsOneQuadPerExposedFaceOnM1World` · đạt; MESHSTATS trước/sau ghi vào Kết quả.
- Test hỏng trước khi sửa mesher: sau bước 2, **trước bước 3**, không chạy được (biên dịch lỗi do bước 1) — bỏ qua mốc "hỏng trước"; thay vào đó ghi ở Kết quả: footprint t = 0,09 / 0,11 vượt mép cũ 0,08 nên với mesher cũ tia sẽ không chạm (không cần chạy).
- Regression mesh + world: toàn bộ EditMode (`BlockMesherTests` khác: kín, bo cạnh, AO, nước, collider) và PlayMode (`TerrainStreamingPlayTests`, `TerrainEditPlayTests`, `FlyCameraWaterPlayTests`, `WorldBoundsPlayTests`, `SimViewPlayTests`…) · đạt.

### Lệnh executor chạy
1. MCP `refresh_unity` → `read_console` (types error) — 0 lỗi biên dịch.
2. MCP `run_tests` EditMode (cả bộ) → ghi dòng tổng kết (số đạt / tổng).
3. MCP `run_tests` PlayMode (cả bộ) → ghi dòng tổng kết. Sau đó dọn: `git checkout -- client/ProjectSettings/EditorSettings.asset`.
4. `grep -n "LipRound\|tTop" client/Assets/Game/World/BlockMesher.cs` → không có dòng nào.
5. `git status --short` — chỉ còn 2 file trong Phạm vi (+ brief); commit code `brief 0014: mép cỏ bo tròn ôm khối`.
6. `date "+%Y-%m-%d %H:%M"` → ghi "Kết thúc" vào Kết quả rồi mới commit brief.

### User tự test
Mở scene TerrainView, Play (chuột), so với ảnh mép cỏ Pokopia user gửi 2026-10-20:
1. Đứng ngang hoặc hơi thấp hơn một bờ cỏ: mép cỏ là **gờ dày bo tròn như cái gối**, nhô ra khỏi vách đất, mép dưới lượn sóng cuộn vào vách; ánh sáng chuyển mượt trên chỗ cuộn (không còn như tấm bìa phẳng).
2. Nhìn từ trên xuống và chéo: mặt trên phẳng cuộn tròn xuống mép; không lỗ/khe ở **góc lồi**, ở chỗ nối hai ô, và ở **đầu dải** sát khối khác (nắp).
3. Lặn xuống nước cạnh bờ cỏ, nhìn lên: mép vẫn kín, thấy mặt dưới nghiêng vào vách.
4. Đào/đặt vài ô cỏ ở bờ: mép mới đúng hình như trên.
(Planner đo FPS điện thoại bằng Bench khi review — user không cần làm.)

## Dừng và chuyển lại Opus khi
- Brief mơ hồ hoặc mâu thuẫn với code thật (vd số dòng lệch nhiều, chữ ký khác).
- Cần một quyết định thuộc AGENTS.md §3.4 hoặc ngoài "Quyết định đã chốt" (vd muốn đổi số đo mặt cắt, số điểm, cách tam giác hoá).
- Cần API Unity không có trong brief.
- Một test ở bước 2 ra khác mong đợi mà không phải lỗi gõ — ghi số thật/dòng lỗi, không tự sửa số trong test.
- Cùng một lỗi sửa 2 lần vẫn chưa qua.
- Phải sửa file ngoài "Phạm vi".

---
<!-- Từ đây trở xuống: EXECUTOR ghi. -->

## Kết quả
- Base commit: b750455
- Chạy bằng: claude-sonnet-5-5 · medium
- Bắt đầu: 2026-10-20 21:49 · Kết thúc: 2026-10-20 21:57
- Commit code: 721a659 — client/Assets/Game/World/BlockMesher.cs, client/Assets/Game/Tests/World/BlockMesherTests.cs
- Lệch brief: không (chép đúng; viết script chèn nguyên văn khối code trong brief). Test mới đạt ngay sau bước 4; mốc "hỏng trước" bỏ theo brief: footprint t = 0,09 / 0,11 vượt mép cũ 0,08 nên mesher cũ không có mặt để tia chạm.
- MESHSTATS trước: `MESHSTATS bo cạnh world_m1: 6862 mặt lộ; hiển thị 54322 tam giác (7.92/mặt), lớn nhất 4396/chunk; collider 13724; mép cỏ thêm 44376 → tổng 98698` · `MESHSTATS bo cạnh: 22/32 chunk có mặt; lớn nhất 7716 tam giác, 8276 đỉnh/chunk`
- MESHSTATS sau: `MESHSTATS bo cạnh world_m1: 6862 mặt lộ; hiển thị 54322 tam giác (7.92/mặt), lớn nhất 4396/chunk; collider 13724; mép cỏ thêm 111256 → tổng 165578` · `MESHSTATS bo cạnh: 22/32 chunk có mặt; lớn nhất 12746 tam giác, 10952 đỉnh/chunk`
- Số liệu: mép cỏ thêm 44376 → 111256 (×2,51, đúng ước ~×2,5); tổng 98698 → 165578 (+67,8% — lớn hơn nhiều so với "~+3% tổng" ghi trong QĐ 7 của brief; Planner lưu ý khi đo FPS). Đỉnh lớn nhất/chunk 8276 → 10952 (≪ 65.535).

## Kết quả test
- Hằng + LipProfile, dải, góc, nắp, kín từ dưới/không đổi từ trên, liền giữa ô/chunk, hướng tam giác, 16-bit, collider: đạt — EditMode `BlockMesherTests` 21/21 (gồm `LipProfileIsRoundedPillow`, 3 test sửa, `LipOnlyOnGrassWithOpenTopAndSide`, `LipDepthStaysInRange`, `TrianglesFaceTheirNormals`, `EveryM1ChunkFits16BitIndices`).
- Regression: đạt — EditMode cả bộ 272/272 (271 + 1), PlayMode 19/19.
- Biên dịch 0 lỗi; `grep "LipRound\|tTop"` BlockMesher.cs → không có dòng. Đã `git checkout -- EditorSettings.asset`; git status chỉ còn 2 file Phạm vi + brief.
- Chờ user: "User tự test" 1–4 (hình gối bo tròn, góc/nối/nắp không khe, nhìn từ dưới nước, đào/đặt).

## Vướng mắc

---
<!-- PLANNER ghi khi review. -->

## Review
