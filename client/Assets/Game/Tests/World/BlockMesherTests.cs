using System.Collections.Generic;
using Game.Core;
using NUnit.Framework;
using Unity.Mathematics;
using UnityEngine;

namespace Game.World.Tests
{
    /// <summary>Bước 5.1–5.2 (decision 0011): mesh khối bo cạnh — mặt lộ, màu theo vật liệu, mesh kín, chiều tam giác, 16 bit.</summary>
    public class BlockMesherTests
    {
        const float R = BlockMesher.BevelRadius;

        /// <summary>World 32×16×32, cỏ ở lớp trên cùng, đất bên dưới, mặt đất ở y = groundTop.</summary>
        static WorldData Flat(int groundTop)
        {
            var w = new WorldData(new WorldSize(32, 16, 32));
            for (int y = 0; y < groundTop; y++)
            for (int z = 0; z < 32; z++)
            for (int x = 0; x < 32; x++)
                w.Set(x, y, z, Cell.Make(y == groundTop - 1 ? BlockMaterial.Grass : BlockMaterial.Dirt));
            return w;
        }

        /// <summary>Đếm mặt lộ bằng cách duyệt thẳng ô (cùng luật biên với mesher: dưới đáy đặc, trên trời rỗng, ngoài x/z lấy cột biên).</summary>
        static int CountExposedFaces(WorldData w, int cx, int cy, int cz)
        {
            var s = w.Size;
            bool Solid(int x, int y, int z)
            {
                if (y < 0) return true;
                if (y >= s.Y) return false;
                x = math.clamp(x, 0, s.X - 1);
                z = math.clamp(z, 0, s.Z - 1);
                var m = w.GetMaterial(x, y, z);
                return m != BlockMaterial.Empty && m != BlockMaterial.Water;
            }
            int faces = 0;
            for (int y = cy * 16; y < cy * 16 + 16; y++)
            for (int z = cz * 16; z < cz * 16 + 16; z++)
            for (int x = cx * 16; x < cx * 16 + 16; x++)
            {
                if (!Solid(x, y, z))
                    continue;
                if (!Solid(x + 1, y, z)) faces++;
                if (!Solid(x - 1, y, z)) faces++;
                if (!Solid(x, y + 1, z)) faces++;
                if (!Solid(x, y - 1, z)) faces++;
                if (!Solid(x, y, z + 1)) faces++;
                if (!Solid(x, y, z - 1)) faces++;
            }
            return faces;
        }

        static float3 GeometricNormal(ChunkMesh m, int t)
        {
            float3 a = m.Vertices[m.Indices[t]], b = m.Vertices[m.Indices[t + 1]], c = m.Vertices[m.Indices[t + 2]];
            return math.cross(b - a, c - a);
        }

        static int Unbalanced(ChunkMesh m) =>
            MeshChecks.UnbalancedEdges(new[] { (m.Vertices, m.Indices, float3.zero) }, null, TestContext.WriteLine);

        [Test]
        public void BuildChunksTwiceGivesIdenticalMeshesAndLeavesCellsUnchanged()
        {
            var world = TerrainGenerator.Generate(new TerrainParams());
            var before = (ushort[])world.Cells.Clone();
            var idx = new[] { 0, 5, 17 };
            var a = BlockMesher.BuildChunks(world, idx);
            var b = BlockMesher.BuildChunks(world, idx);
            for (int k = 0; k < idx.Length; k++)
            {
                CollectionAssert.AreEqual(a[k].Vertices, b[k].Vertices, $"Vertices {k}");
                CollectionAssert.AreEqual(a[k].Indices, b[k].Indices, $"Indices {k}");
                CollectionAssert.AreEqual(a[k].WaterIndices, b[k].WaterIndices, $"WaterIndices {k}");
                CollectionAssert.AreEqual(a[k].ColliderIndices, b[k].ColliderIndices, $"ColliderIndices {k}");
            }
            CollectionAssert.AreEqual(before, world.Cells);
        }

        [Test]
        public void FlatGroundIsOneQuadPerColumnOnTop()
        {
            var mesh = BlockMesher.Build(Flat(8), 0, 0, 0);
            // Mặt phẳng, không vách (mép map lấy cột biên), không cạnh lồi → đúng 16 × 16 tứ giác phẳng ở y = 8.
            Assert.AreEqual(16 * 16 * 2, mesh.TriangleCount);
            for (int i = 0; i < mesh.Vertices.Length; i++)
            {
                Assert.AreEqual(8f, mesh.Vertices[i].y, 1e-6f, $"đỉnh {i}");
                Assert.AreEqual(new float3(0, 1, 0), mesh.Normals[i]);
                Assert.AreEqual(BlockMesher.TopColor(BlockMaterial.Grass), mesh.Colors[i]);
            }
        }

        [Test]
        public void ColliderIsOneQuadPerExposedFaceOnM1World()
        {
            var world = TerrainGenerator.Generate(new TerrainParams());
            var s = world.Size;
            int renderTris = 0, colliderTris = 0, faces = 0, maxTris = 0, lipTris = 0;
            for (int cy = 0; cy < s.ChunksY; cy++)
            for (int cz = 0; cz < s.ChunksZ; cz++)
            for (int cx = 0; cx < s.ChunksX; cx++)
            {
                var mesh = BlockMesher.Build(world, cx, cy, cz, grassLip: false);
                int f = CountExposedFaces(world, cx, cy, cz);
                lipTris += BlockMesher.Build(world, cx, cy, cz).TriangleCount - mesh.TriangleCount;
                Assert.AreEqual(f * 2, mesh.ColliderIndices.Length / 3, $"collider chunk {cx},{cy},{cz}");
                Assert.AreEqual(f * 4, mesh.ColliderVertices.Length, $"collider chunk {cx},{cy},{cz}");
                // Mặt bo: 2 (phẳng) .. 18 (lưới 3 × 3) tam giác.
                Assert.GreaterOrEqual(mesh.TriangleCount, f * 2, $"chunk {cx},{cy},{cz}");
                Assert.LessOrEqual(mesh.TriangleCount, f * 18, $"chunk {cx},{cy},{cz}");
                faces += f;
                renderTris += mesh.TriangleCount;
                colliderTris += mesh.ColliderIndices.Length / 3;
                maxTris = math.max(maxTris, mesh.TriangleCount);
            }
            TestContext.WriteLine($"MESHSTATS bo cạnh world_m1: {faces} mặt lộ; hiển thị {renderTris} tam giác " +
                                  $"({(float)renderTris / faces:F2}/mặt), lớn nhất {maxTris}/chunk; collider {colliderTris}; " +
                                  $"mép cỏ thêm {lipTris} → tổng {renderTris + lipTris}");
        }

        [Test]
        public void SingleCubeIsRoundedAndClosed()
        {
            var w = new WorldData(new WorldSize(16, 16, 16));
            w.Set(4, 8, 4, Cell.Make(BlockMaterial.Stone));
            var mesh = BlockMesher.Build(w, 0, 0, 0);
            Assert.AreEqual(0, Unbalanced(mesh), "khối đơn phải kín");
            // Mọi đỉnh nằm trên mặt "lõi co r rồi nở r": khoảng cách tới lõi [r, 1 − r]³ đúng bằng r; pháp tuyến hướng ra từ lõi.
            float3 lo = new float3(4, 8, 4) + R, hi = new float3(5, 9, 5) - R;
            int rounded = 0;
            for (int i = 0; i < mesh.Vertices.Length; i++)
            {
                float3 p = mesh.Vertices[i];
                float3 q = math.clamp(p, lo, hi);
                Assert.AreEqual(R, math.distance(p, q), 1e-4f, $"đỉnh {i} {p}");
                Assert.Less(math.distance(mesh.Normals[i], math.normalize(p - q)), 1e-3f, $"pháp tuyến {i}");
                if (math.cmax(math.abs(math.normalize(p - q))) < 0.99f)
                    rounded++;
            }
            Assert.Greater(rounded, 0, "phải có đỉnh trên phần bo");
        }

        [Test]
        public void AllVertexConfigurationsAreClosed()
        {
            // Hình bo quanh một đỉnh lưới chỉ phụ thuộc 8 ô quanh nó → thử đủ 256 tổ hợp 2 × 2 × 2 đặt giữa không gian rỗng.
            for (int config = 1; config < 256; config++)
            {
                var w = new WorldData(new WorldSize(16, 16, 16));
                for (int k = 0; k < 8; k++)
                    if ((config >> k & 1) != 0)
                        w.Set(7 + (k & 1), 7 + (k >> 1 & 1), 7 + (k >> 2 & 1), Cell.Make(BlockMaterial.Stone));
                var mesh = BlockMesher.Build(w, 0, 0, 0);
                Assert.AreEqual(0, Unbalanced(mesh), $"tổ hợp {config}");
            }
        }

        [Test]
        public void RandomShapesAreClosed()
        {
            // Hình ngẫu nhiên 5 × 5 × 5 (mọi kiểu 26 ô lân cận: chạm chéo, lỗ, cột mảnh), seed cố định.
            var rng = new System.Random(2026);
            for (int trial = 0; trial < 2000; trial++)
            {
                var w = new WorldData(new WorldSize(16, 16, 16));
                double density = 0.3 + 0.4 * rng.NextDouble();
                for (int y = 5; y < 10; y++)
                for (int z = 5; z < 10; z++)
                for (int x = 5; x < 10; x++)
                    if (rng.NextDouble() < density)
                        w.Set(x, y, z, Cell.Make(BlockMaterial.Dirt));
                var mesh = BlockMesher.Build(w, 0, 0, 0);
                Assert.AreEqual(0, Unbalanced(mesh), $"hình ngẫu nhiên {trial}");
            }
        }

        [Test]
        public void NoFaceBetweenTwoSolidCells()
        {
            // Hai ô đặc kề nhau giữa không trung: collider 10 mặt; mesh hiển thị không có tam giác nằm trên mặt chung x = 5.
            var w = new WorldData(new WorldSize(16, 16, 16));
            w.Set(4, 8, 4, Cell.Make(BlockMaterial.Stone));
            w.Set(5, 8, 4, Cell.Make(BlockMaterial.Stone));
            var mesh = BlockMesher.Build(w, 0, 0, 0);
            Assert.AreEqual(10 * 2, mesh.ColliderIndices.Length / 3);
            Assert.AreEqual(0, Unbalanced(mesh));
            for (int t = 0; t < mesh.Indices.Length; t += 3)
            {
                float3 a = mesh.Vertices[mesh.Indices[t]], b = mesh.Vertices[mesh.Indices[t + 1]], c = mesh.Vertices[mesh.Indices[t + 2]];
                bool onSharedPlane = a.x == 5f && b.x == 5f && c.x == 5f;
                Assert.IsFalse(onSharedPlane, $"mặt thừa ở x = 5: {a} {b} {c}");
            }
        }

        [Test]
        public void GrassSidesUseDirtColor()
        {
            var w = new WorldData(new WorldSize(16, 16, 16));
            w.Set(4, 8, 4, Cell.Make(BlockMaterial.Grass));
            var mesh = BlockMesher.Build(w, 0, 0, 0, grassLip: false);
            int grassTris = 0;
            for (int t = 0; t < mesh.Indices.Length; t += 3)
            {
                uint c0 = mesh.Colors[mesh.Indices[t]];
                Assert.AreEqual(c0, mesh.Colors[mesh.Indices[t + 1]]);
                Assert.AreEqual(c0, mesh.Colors[mesh.Indices[t + 2]]);
                // Tam giác của mặt trên (kể cả dải bo tới 45°) nghiêng < 45° → pháp tuyến hình học y > 0,7.
                bool top = math.normalize(GeometricNormal(mesh, t)).y > 0.7f;
                Assert.AreEqual(top ? BlockMesher.TopColor(BlockMaterial.Grass) : BlockMesher.TopColor(BlockMaterial.Dirt), c0, $"tam giác {t / 3}");
                if (top)
                    grassTris++;
            }
            Assert.Greater(grassTris, 0);
        }

        [Test]
        public void WaterIsNotSolid()
        {
            // Ô đất dưới ô nước có mặt trên (nước là rỗng với mesher — mặt nước là bước 5b).
            var w = new WorldData(new WorldSize(16, 16, 16));
            w.Set(4, 8, 4, Cell.Make(BlockMaterial.Dirt));
            w.Set(4, 9, 4, Cell.Make(BlockMaterial.Water));
            var mesh = BlockMesher.Build(w, 0, 0, 0);
            Assert.AreEqual(6 * 2, mesh.ColliderIndices.Length / 3);
        }

        [Test]
        public void TrianglesFaceTheirNormals()
        {
            var mesh = BlockMesher.Build(TerrainGenerator.Generate(new TerrainParams()), 1, 0, 1);
            Assert.Greater(mesh.TriangleCount, 0);
            for (int t = 0; t < mesh.Indices.Length; t += 3)
            {
                // Quy ước như mesher cũ: pháp tuyến hình học = cross(b − a, c − a) cùng hướng pháp tuyến đỉnh.
                float3 face = GeometricNormal(mesh, t);
                float3 vn = mesh.Normals[mesh.Indices[t]] + mesh.Normals[mesh.Indices[t + 1]] + mesh.Normals[mesh.Indices[t + 2]];
                Assert.Greater(math.dot(face, vn), 0f, $"tam giác {t / 3}");
            }
        }

        [Test]
        public void EveryM1ChunkFits16BitIndices()
        {
            var world = TerrainGenerator.Generate(new TerrainParams());
            int maxVerts = 0, maxTris = 0, surfaceChunks = 0;
            foreach (var mesh in BlockMesher.BuildAll(world))
            {
                Assert.LessOrEqual(mesh.Vertices.Length, ChunkMeshWriter.MaxVertices16);
                Assert.LessOrEqual(mesh.ColliderVertices.Length, ChunkMeshWriter.MaxVertices16);
                maxVerts = math.max(maxVerts, mesh.Vertices.Length);
                maxTris = math.max(maxTris, mesh.TriangleCount);
                if (mesh.TriangleCount > 0)
                    surfaceChunks++;
            }
            TestContext.WriteLine($"MESHSTATS bo cạnh: {surfaceChunks}/32 chunk có mặt; lớn nhất {maxTris} tam giác, {maxVerts} đỉnh/chunk");
        }

        [Test]
        public void WritesUnityMeshWithColorsAndCollider()
        {
            var chunk = BlockMesher.Build(Flat(8), 0, 0, 0);
            var mesh = new Mesh();
            var collider = new Mesh();
            try
            {
                ChunkMeshWriter.Write(chunk, mesh);
                ChunkMeshWriter.WriteColliders(new List<ChunkMesh> { chunk }, new List<Mesh> { collider });
                Assert.AreEqual(UnityEngine.Rendering.IndexFormat.UInt16, mesh.indexFormat);
                Assert.AreEqual(chunk.Vertices.Length, mesh.vertexCount);
                Assert.AreEqual(chunk.Indices.Length, (int)mesh.GetIndexCount(0));
                Assert.AreEqual(8f, mesh.bounds.max.y, 1e-3f);
                Assert.AreEqual(new Color32(140, 200, 96, BlockMesher.PatternGrass), mesh.colors32[0]);
                Assert.AreEqual(chunk.ColliderVertices.Length, collider.vertexCount);
                Assert.AreEqual(chunk.ColliderIndices.Length, (int)collider.GetIndexCount(0));
            }
            finally
            {
                Object.DestroyImmediate(mesh);
                Object.DestroyImmediate(collider);
            }
        }

        // ---------------- Mép cỏ (bước 5.3) ----------------

        static readonly float MaxLip = BlockMesher.LipDepth + BlockMesher.LipWave1 + BlockMesher.LipWave2;

        [Test]
        public void LipDepthStaysInRange()
        {
            for (int k = 0; k < 2000; k++)
            {
                float d = BlockMesher.LipDepthAt(k * 0.137f, k % 2 == 0 ? 0 : 2, k % 3 == 0 ? -1 : 1, k % 17, k % 11);
                Assert.That(d, Is.InRange(BlockMesher.LipDepth - BlockMesher.LipWave1 - BlockMesher.LipWave2 - 1e-5f, MaxLip + 1e-5f));
            }
        }

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

        [Test]
        public void LipOnlyOnGrassWithOpenTopAndSide()
        {
            // Đá: không mép. Cỏ bị khối đè: không mép. Cỏ lộ mặt trên: có đỉnh nhô ra ngoài ô đúng LipOverhang, váy không sâu quá MaxLip.
            float3 lo = new float3(4, 8, 4), hi = new float3(5, 9, 5);
            var stone = new WorldData(new WorldSize(16, 16, 16));
            stone.Set(4, 8, 4, Cell.Make(BlockMaterial.Stone));
            AssertInside(BlockMesher.Build(stone, 0, 0, 0), lo, hi, "đá");

            var covered = new WorldData(new WorldSize(16, 16, 16));
            covered.Set(4, 8, 4, Cell.Make(BlockMaterial.Grass));
            covered.Set(4, 9, 4, Cell.Make(BlockMaterial.Stone));
            AssertInside(BlockMesher.Build(covered, 0, 0, 0), lo, new float3(5, 10, 5), "cỏ bị đè");

            var open = new WorldData(new WorldSize(16, 16, 16));
            open.Set(4, 8, 4, Cell.Make(BlockMaterial.Grass));
            var mesh = BlockMesher.Build(open, 0, 0, 0);
            float maxOut = 0, minY = float.MaxValue;
            foreach (var p in mesh.Vertices)
            {
                maxOut = math.max(maxOut, math.cmax(math.max(lo - p, p - hi)));
                minY = math.min(minY, p.y);
            }
            Assert.AreEqual(BlockMesher.LipOverhang, maxOut, 1e-4f, "mép nhô ra");
            Assert.GreaterOrEqual(minY, 8f - 1e-4f, "đáy khối");
            int lipLow = 0;
            foreach (var p in mesh.Vertices)
                if (math.cmax(math.max(lo - p, p - hi)) > 1e-3f)
                {
                    Assert.GreaterOrEqual(p.y, 9f - MaxLip - 1e-4f, $"váy quá sâu ở {p}");
                    if (p.y < 9f - 0.1f) lipLow++;
                }
            Assert.Greater(lipLow, 0, "phải có váy cỏ buông xuống");
        }

        static void AssertInside(ChunkMesh m, float3 lo, float3 hi, string what)
        {
            foreach (var p in m.Vertices)
                Assert.IsTrue(math.all(p >= lo - 1e-4f) && math.all(p <= hi + 1e-4f), $"{what}: đỉnh {p} ngoài ô");
        }

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

        /// <summary>Điểm (x, z) dưới phần mép nhô ra của ô cỏ (4, 8, 4): 4 sườn × sigma {0,3; 0,7} × t {0,01 … 0,11} và 4 góc × r {0,02; 0,05; 0,08; 0,11} theo đường chéo 45° (mép nhô 0,12 — brief 0014).</summary>
        static List<float2> LipFootprint()
        {
            var pts = new List<float2>();
            foreach (float s in new[] { 0.3f, 0.7f })
            foreach (float t in new[] { 0.01f, 0.03f, 0.05f, 0.07f, 0.09f, 0.11f })
            {
                pts.Add(new float2(5 + t, 4 + s));
                pts.Add(new float2(4 - t, 4 + s));
                pts.Add(new float2(4 + s, 5 + t));
                pts.Add(new float2(4 + s, 4 - t));
            }
            foreach (int sx in new[] { -1, 1 })
            foreach (int sz in new[] { -1, 1 })
            foreach (float r in new[] { 0.02f, 0.05f, 0.08f, 0.11f })
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
            // Nắp là đa giác mặt cắt kín (brief 0014): LipProfilePoints + 2 đỉnh mỗi nắp; dưới độ sâu LipFloor chỉ có cuộn dưới (t ≥ LipOverhang − LipBottomRound).
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
                    Assert.IsTrue(p.x >= 5f + BlockMesher.LipOverhang - BlockMesher.LipBottomRound - 1e-4f || p.x <= 4f - BlockMesher.LipOverhang + BlockMesher.LipBottomRound + 1e-4f, $"nắp lòi dưới đáy ở {p}");
            }
            Assert.AreEqual(2 * (BlockMesher.LipProfilePoints + 2), cap, "2 nắp × (LipProfilePoints + 2) đỉnh");
        }

        [Test]
        public void LipIsDeterministicAndContinuousAcrossCellsAndChunks()
        {
            // Hàng ô cỏ x = 13..18 (z = 4, y = 8) vắt qua biên chunk x = 16 → mép sườn −z và +z chạy qua hai chunk.
            var w = new WorldData(new WorldSize(32, 16, 16));
            for (int x = 13; x <= 18; x++)
                w.Set(x, 8, 4, Cell.Make(BlockMaterial.Grass));
            var a = BlockMesher.BuildAll(w);
            var b = BlockMesher.BuildAll(w);
            for (int c = 0; c < a.Length; c++)
            {
                CollectionAssert.AreEqual(a[c].Vertices, b[c].Vertices, "tất định: đỉnh");
                CollectionAssert.AreEqual(a[c].Indices, b[c].Indices, "tất định: chỉ số");
            }
            // Đỉnh mặt trước của mép cỏ (t = LipOverhang: A3 và C0 của mặt cắt, thấp hơn mặt trên) tại ranh giới hai ô x = 14..18: phải có ở cả dải bên trái lẫn bên phải.
            var at = new Dictionary<long, int>();
            for (int c = 0; c < a.Length; c++)
            {
                float3 o = new float3(c % 2, 0, 0) * WorldSize.ChunkSize;
                foreach (var v in a[c].Vertices)
                {
                    float3 p = v + o;
                    bool skirt = (p.z < 4f - BlockMesher.LipOverhang + 1e-3f || p.z > 5f + BlockMesher.LipOverhang - 1e-3f) && p.y < 9f - 1e-3f;
                    if (skirt && p.x >= 14f - 1e-4f && p.x <= 18f + 1e-4f && math.abs(p.x - math.round(p.x)) < 1e-4f)
                    {
                        long k = MeshChecks.Key(p);
                        at.TryGetValue(k, out int n);
                        at[k] = n + 1;
                    }
                }
            }
            Assert.AreEqual(5 * 2 * 2, at.Count, "mỗi ranh giới x = 14..18, mỗi sườn có 2 điểm mặt trước (A3, C0)");
            foreach (var kv in at)
                Assert.AreEqual(2, kv.Value, "điểm mặt trước ở ranh giới phải trùng giữa hai dải kề nhau (kể cả qua biên chunk x = 16) — mỗi dải 1 đỉnh mỗi điểm");
        }

        // ---------------- AO theo đỉnh + mã hoa văn (bước 5.4) ----------------

        [Test]
        public void ConcaveCornerIsDarkerByVertexAo()
        {
            // Nền phẳng (mặt trên y = 8) + một cột tường ở (5, 8, 4): đỉnh mặt nền sát chân tường tối hơn, đỉnh xa tường giữ màu gốc.
            var w = Flat(8);
            w.Set(5, 8, 4, Cell.Make(BlockMaterial.Stone));
            var mesh = BlockMesher.Build(w, 0, 0, 0, grassLip: false);
            uint baseColor = BlockMesher.TopColor(BlockMaterial.Grass);
            int darkNearWall = 0, farChecked = 0;
            for (int i = 0; i < mesh.Vertices.Length; i++)
            {
                float3 p = mesh.Vertices[i];
                if (math.abs(p.y - 8f) > 1e-4f || mesh.Normals[i].y < 0.99f)
                    continue;
                uint c = mesh.Colors[i];
                Assert.AreEqual(baseColor >> 24, c >> 24, "giữ mã hoa văn");
                bool touchesWall = p.x >= 5f - 1e-4f && p.x <= 6f + 1e-4f && p.z >= 4f - 1e-4f && p.z <= 5f + 1e-4f;
                bool far = math.abs(p.x - 5.5f) > 2f || math.abs(p.z - 4.5f) > 2f;
                if (touchesWall && (c & 0xFF) < (baseColor & 0xFF))
                    darkNearWall++;
                if (far)
                {
                    Assert.AreEqual(baseColor, c, $"đỉnh xa tường không bị tối ở {p}");
                    farChecked++;
                }
            }
            Assert.Greater(darkNearWall, 0, "đỉnh nền sát chân tường phải tối hơn (AO)");
            Assert.Greater(farChecked, 0);
        }

        [Test]
        public void ShadeKeepsPatternByte()
        {
            uint c = BlockMesher.TopColor(BlockMaterial.Dirt);
            uint d = BlockMesher.Shade(c, BlockMesher.AoBrightness[0]);
            Assert.AreEqual(c >> 24, d >> 24);
            Assert.Less(d & 0xFF, c & 0xFF);
            Assert.AreEqual(c, BlockMesher.Shade(c, 1f));
        }

        // ---------------- Mặt nước theo ô (bước 5c, 0012) ----------------

        [Test]
        public void WaterSurfaceOnlyWhereAirIsAbove()
        {
            // Cột nước 2 ô (y 7, 8) trên nền đá: chỉ ô trên cùng có mặt nước, ở y = 9 − WaterSurfaceDrop. Ô nước bị đá đè: không có.
            var w = new WorldData(new WorldSize(16, 16, 16));
            w.Set(4, 6, 4, Cell.Make(BlockMaterial.Stone));
            w.Set(4, 7, 4, Cell.Make(BlockMaterial.Water, Cell.MaxWaterLevel));
            w.Set(4, 8, 4, Cell.Make(BlockMaterial.Water, Cell.MaxWaterLevel));
            w.Set(8, 7, 8, Cell.Make(BlockMaterial.Water, Cell.MaxWaterLevel));
            w.Set(8, 8, 8, Cell.Make(BlockMaterial.Stone));
            var mesh = BlockMesher.Build(w, 0, 0, 0);
            Assert.AreEqual(2, mesh.WaterTriangleCount, "đúng một tứ giác mặt nước");
            for (int k = 0; k < mesh.WaterIndices.Length; k++)
            {
                float3 p = mesh.Vertices[mesh.WaterIndices[k]];
                Assert.AreEqual(9f - BlockMesher.WaterSurfaceDrop, p.y, 1e-5f);
                Assert.That(p.x, Is.InRange(4f, 5f));
                Assert.That(p.z, Is.InRange(4f, 5f));
            }
            for (int t = 0; t < mesh.WaterIndices.Length; t += 3)
            {
                float3 a = mesh.Vertices[mesh.WaterIndices[t]], b = mesh.Vertices[mesh.WaterIndices[t + 1]], c = mesh.Vertices[mesh.WaterIndices[t + 2]];
                Assert.Greater(math.cross(b - a, c - a).y, 0f, "mặt nước hướng lên (cùng quy ước mặt trước)");
            }
        }
    }
}
