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
                Assert.AreEqual(8f, mesh.Vertices[i].y, 1e-6f, $"vertex {i}");
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
            TestContext.WriteLine($"MESHSTATS rounded edges world_m1: {faces} exposed faces; rendered {renderTris} triangles " +
                                  $"({(float)renderTris / faces:F2}/face), max {maxTris}/chunk; collider {colliderTris}; " +
                                  $"grass lip adds {lipTris} → total {renderTris + lipTris}");
        }

        [Test]
        public void SingleCubeIsRoundedAndClosed()
        {
            var w = new WorldData(new WorldSize(16, 16, 16));
            w.Set(4, 8, 4, Cell.Make(BlockMaterial.Stone));
            var mesh = BlockMesher.Build(w, 0, 0, 0);
            Assert.AreEqual(0, Unbalanced(mesh), "single block must be closed");
            // Mọi đỉnh nằm trên mặt "lõi co r rồi nở r": khoảng cách tới lõi [r, 1 − r]³ đúng bằng r; pháp tuyến hướng ra từ lõi.
            float3 lo = new float3(4, 8, 4) + R, hi = new float3(5, 9, 5) - R;
            int rounded = 0;
            for (int i = 0; i < mesh.Vertices.Length; i++)
            {
                float3 p = mesh.Vertices[i];
                float3 q = math.clamp(p, lo, hi);
                Assert.AreEqual(R, math.distance(p, q), 1e-4f, $"vertex {i} {p}");
                Assert.Less(math.distance(mesh.Normals[i], math.normalize(p - q)), 1e-3f, $"normal {i}");
                if (math.cmax(math.abs(math.normalize(p - q))) < 0.99f)
                    rounded++;
            }
            Assert.Greater(rounded, 0, "must have vertices on the rounded part");
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
                Assert.AreEqual(0, Unbalanced(mesh), $"combination {config}");
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
                Assert.AreEqual(0, Unbalanced(mesh), $"random shape {trial}");
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
                Assert.IsFalse(onSharedPlane, $"redundant faces at x = 5: {a} {b} {c}");
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
                Assert.AreEqual(top ? BlockMesher.TopColor(BlockMaterial.Grass) : BlockMesher.TopColor(BlockMaterial.Dirt), c0, $"triangle {t / 3}");
                if (top)
                    grassTris++;
            }
            Assert.Greater(grassTris, 0);
        }

        [Test]
        public void FarmlandTopOwnColorSidesDirtNoLip()
        {
            var w = new WorldData(new WorldSize(16, 16, 16));
            w.Set(4, 8, 4, Cell.Make(BlockMaterial.Farmland));
            var mesh = BlockMesher.Build(w, 0, 0, 0, grassLip: false);
            int tops = 0;
            for (int t = 0; t < mesh.Indices.Length; t += 3)
            {
                uint c0 = mesh.Colors[mesh.Indices[t]];
                Assert.AreEqual(c0, mesh.Colors[mesh.Indices[t + 1]]);
                Assert.AreEqual(c0, mesh.Colors[mesh.Indices[t + 2]]);
                bool top = math.normalize(GeometricNormal(mesh, t)).y > 0.7f;
                Assert.AreEqual(top ? BlockMesher.TopColor(BlockMaterial.Farmland) : BlockMesher.TopColor(BlockMaterial.Dirt), c0, $"triangle {t / 3}");
                if (top) tops++;
            }
            Assert.Greater(tops, 0);
            Assert.AreNotEqual(BlockMesher.TopColor(BlockMaterial.Dirt), BlockMesher.TopColor(BlockMaterial.Farmland));
            var withLip = BlockMesher.Build(w, 0, 0, 0);
            Assert.AreEqual(mesh.TriangleCount, withLip.TriangleCount);
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
                Assert.Greater(math.dot(face, vn), 0f, $"triangle {t / 3}");
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
            TestContext.WriteLine($"MESHSTATS rounded edges: {surfaceChunks}/32 chunks have surface; max {maxTris} triangles, {maxVerts} vertices/chunk");
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
                    Assert.AreEqual(1f, math.length(n[i]), 1e-5f, $"depth {depth}, point {i}: unit normal");
                    Assert.That(p[i].x, Is.InRange(-1e-5f, lo + 1e-5f), $"depth {depth}, point {i}: t in [0, LipOverhang]");
                    if (i > 0 && i < 7)
                        Assert.Less(p[i].y, p[i - 1].y + 1e-6f, $"depth {depth}, point {i}: A0..C2 go downward");
                }
                AssertNear(new float2(lo - rt, 0f), p[0], "A0"); AssertNear(new float2(0f, 1f), n[0], "n A0");
                AssertNear(new float2(lo, -rt), p[3], "A3"); AssertNear(new float2(1f, 0f), n[3], "n A3");
                AssertNear(new float2(lo, -(depth - rb)), p[4], "C0"); AssertNear(new float2(1f, 0f), n[4], "n C0");
                AssertNear(new float2(lo - rb, -depth), p[6], "C2"); AssertNear(new float2(0f, -1f), n[6], "n C2");
                AssertNear(new float2(0f, -BlockMesher.LipFloor), p[7], "W"); AssertNear(new float2(0f, -1f), n[7], "n W");
                Assert.Greater(p[7].y, p[6].y + 0.04f, $"depth {depth}: underside slopes up into the wall");
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
            AssertInside(BlockMesher.Build(stone, 0, 0, 0), lo, hi, "rock");

            var covered = new WorldData(new WorldSize(16, 16, 16));
            covered.Set(4, 8, 4, Cell.Make(BlockMaterial.Grass));
            covered.Set(4, 9, 4, Cell.Make(BlockMaterial.Stone));
            AssertInside(BlockMesher.Build(covered, 0, 0, 0), lo, new float3(5, 10, 5), "grass pressed down");

            var open = new WorldData(new WorldSize(16, 16, 16));
            open.Set(4, 8, 4, Cell.Make(BlockMaterial.Grass));
            var mesh = BlockMesher.Build(open, 0, 0, 0);
            float maxOut = 0, minY = float.MaxValue;
            foreach (var p in mesh.Vertices)
            {
                maxOut = math.max(maxOut, math.cmax(math.max(lo - p, p - hi)));
                minY = math.min(minY, p.y);
            }
            Assert.AreEqual(BlockMesher.LipOverhang, maxOut, 1e-4f, "overhanging lip");
            Assert.GreaterOrEqual(minY, 8f - 1e-4f, "block bottom");
            int lipLow = 0;
            foreach (var p in mesh.Vertices)
                if (math.cmax(math.max(lo - p, p - hi)) > 1e-3f)
                {
                    Assert.GreaterOrEqual(p.y, 9f - MaxLip - 1e-4f, $"skirt too deep at {p}");
                    if (p.y < 9f - 0.1f) lipLow++;
                }
            Assert.Greater(lipLow, 0, "must have a hanging grass skirt");
        }

        static void AssertInside(ChunkMesh m, float3 lo, float3 hi, string what)
        {
            foreach (var p in m.Vertices)
                Assert.IsTrue(math.all(p >= lo - 1e-4f) && math.all(p <= hi + 1e-4f), $"{what}: vertex {p} outside cell");
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
                Assert.IsTrue(FirstHit(mesh, new float3(p.x, 8f, p.y), new float3(0, 1, 0), out float dUp, out float3 nUp), $"below {p}: no hit");
                Assert.Less(nUp.y, 0f, $"below {p}: hit back face (see-through)");
                Assert.That(8f + dUp, Is.InRange(9f - MaxLip - 1e-4f, 9f - BlockMesher.LipFloor + 1e-4f), $"below {p}: hit height");
                Assert.IsTrue(FirstHit(mesh, new float3(p.x, 10f, p.y), new float3(0, -1, 0), out float dDown, out float3 nDown), $"above {p}: no hit");
                Assert.Greater(nDown.y, 0f, $"above {p}: hit back face");
                Assert.That(10f - dDown, Is.InRange(9f - MaxLip - 1e-4f, 9f + 1e-4f), $"above {p}: hit height");
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
                    Assert.IsTrue(p.x >= 5f + BlockMesher.LipOverhang - BlockMesher.LipBottomRound - 1e-4f || p.x <= 4f - BlockMesher.LipOverhang + BlockMesher.LipBottomRound + 1e-4f, $"cap pokes out under the bottom at {p}");
            }
            Assert.AreEqual(2 * (BlockMesher.LipProfilePoints + 2), cap, "2 caps x (LipProfilePoints + 2) vertices");
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
                CollectionAssert.AreEqual(a[c].Vertices, b[c].Vertices, "deterministic: vertices");
                CollectionAssert.AreEqual(a[c].Indices, b[c].Indices, "deterministic: indices");
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
            Assert.AreEqual(5 * 2 * 2, at.Count, "each boundary x = 14..18, each slope has 2 front-face points (A3, C0)");
            foreach (var kv in at)
                Assert.AreEqual(2, kv.Value, "front-face points at the boundary must coincide between two adjacent strips (including across chunk edge x = 16) — 1 vertex per point per strip");
        }

        // ---------------- Không nhìn xuyên ở góc giao nhau (brief 0073) ----------------

        /// <summary>
        /// Địa hình cột trong world 16³: ngoài vùng 2..12 cao 6; trong vùng cao h(x, z); ô trên cùng mỗi cột là <paramref name="top"/>(x, z), dưới là đất.
        /// Cấu hình (brief 0073): H1 góc lõm chữ L cùng độ cao, H2 hai ô chéo nhau, H3 dải gặp bậc cao hơn, H3b dải gặp ô đá cùng độ cao,
        /// H3c bậc thang dọc sườn, H4 địa hình ngẫu nhiên (seed cố định).
        /// </summary>
        static WorldData CornerWorld(string name)
        {
            var rng = new System.Random(73);
            var rnd = new int[16, 16];
            for (int z = 0; z < 16; z++)
            for (int x = 0; x < 16; x++)
                rnd[x, z] = 6 + rng.Next(4);
            int H(int x, int z)
            {
                if (x < 2 || x > 12 || z < 2 || z > 12)
                    return 6;
                return name switch
                {
                    "H1" => x <= 6 || z >= 7 ? 8 : 6,
                    "H2" => (x <= 6 && z <= 6) || (x >= 7 && z >= 7) ? 8 : 6,
                    "H3" => z <= 6 ? (x >= 7 ? 9 : 8) : 6,
                    "H3b" => z <= 6 ? 8 : 6,
                    "H3c" => z <= 6 ? 8 + (x >= 6 ? 1 : 0) + (x >= 8 ? 1 : 0) : 6,
                    _ => rnd[x, z],
                };
            }
            var w = new WorldData(new WorldSize(16, 16, 16));
            for (int z = 0; z < 16; z++)
            for (int x = 0; x < 16; x++)
            {
                int h = H(x, z);
                var topMat = name == "H3b" && x >= 7 && z >= 2 && z <= 6 && x <= 12 ? BlockMaterial.Stone : BlockMaterial.Grass;
                for (int y = 0; y < h; y++)
                    w.Set(x, y, z, Cell.Make(y == h - 1 ? topMat : BlockMaterial.Dirt));
            }
            return w;
        }

        /// <summary>Mesh để bắn nhiều tia: lọc tam giác theo hộp bao của đoạn tia trước khi tính giao (Möller–Trumbore).</summary>
        sealed class RayMesh
        {
            readonly ChunkMesh m;
            readonly float3[] lo, hi;

            public RayMesh(ChunkMesh mesh)
            {
                m = mesh;
                int n = m.Indices.Length / 3;
                lo = new float3[n];
                hi = new float3[n];
                for (int t = 0; t < n; t++)
                {
                    float3 a = m.Vertices[m.Indices[3 * t]], b = m.Vertices[m.Indices[3 * t + 1]], c = m.Vertices[m.Indices[3 * t + 2]];
                    lo[t] = math.min(a, math.min(b, c));
                    hi[t] = math.max(a, math.max(b, c));
                }
            }

            /// <summary>Tam giác gần nhất trên đoạn (o, o + dir·maxDist); normal = mặt trước theo quy ước mesher.</summary>
            public bool Hit(float3 o, float3 dir, float maxDist, out float dist, out float3 normal)
            {
                float3 end = o + dir * maxDist, slo = math.min(o, end) - 1e-3f, shi = math.max(o, end) + 1e-3f;
                dist = float.MaxValue;
                normal = 0;
                for (int t = 0; t < lo.Length; t++)
                {
                    if (math.any(hi[t] < slo) || math.any(lo[t] > shi))
                        continue;
                    float3 a = m.Vertices[m.Indices[3 * t]], b = m.Vertices[m.Indices[3 * t + 1]], c = m.Vertices[m.Indices[3 * t + 2]];
                    float3 e1 = b - a, e2 = c - a, pv = math.cross(dir, e2);
                    float det = math.dot(e1, pv);
                    if (math.abs(det) < 1e-9f)
                        continue;
                    float inv = 1f / det;
                    float3 s = o - a;
                    float u = math.dot(s, pv) * inv;
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
                        normal = math.cross(e1, e2);
                    }
                }
                return dist <= maxDist;
            }
        }

        static bool CornerSolid(WorldData w, int3 c)
        {
            if (c.y < 0) return true;
            if (c.y >= 16) return false;
            var mat = w.GetMaterial(math.clamp(c.x, 0, 15), c.y, math.clamp(c.z, 0, 15));
            return mat != BlockMaterial.Empty && mat != BlockMaterial.Water;
        }

        /// <summary>Khoảng cách từ p tới ô đặc gần nhất (27 ô quanh p).</summary>
        static float ClearanceToSolid(WorldData w, float3 p)
        {
            int3 c = (int3)math.floor(p);
            float best = float.MaxValue;
            for (int dz = -1; dz <= 1; dz++)
            for (int dy = -1; dy <= 1; dy++)
            for (int dx = -1; dx <= 1; dx++)
            {
                int3 q = c + new int3(dx, dy, dz);
                if (!CornerSolid(w, q))
                    continue;
                float3 d = math.max(0f, math.max(q - p, p - (q + 1)));
                best = math.min(best, math.length(d));
            }
            return best;
        }

        /// <summary>
        /// Bắn tia nhìn từ trên xiên xuống (12 phương vị × độ cao 20°, 45°, 70°) vào điểm trong ô đặc cách mỗi mặt ô ≥ R + 0,05,
        /// gần góc/cạnh mặt trên của mọi ô lộ mặt trên. Gốc tia cách đích 2,5 trong không khí, cách mọi ô đặc ≥ 0,4 (ngoài mép cỏ).
        /// Tia phải chạm một mặt quay về phía nó trước khi tới đích; không chạm hoặc chạm mặt sau = lỗ. Trả số tia hở.
        /// </summary>
        static int SeeThroughRays(WorldData w, RayMesh rm, out int rays, out string sample)
        {
            const float L = 2.5f;
            float[] offs = { 0.18f, 0.5f, 0.82f };
            int holes = 0;
            rays = 0;
            sample = "";
            for (int z = 2; z <= 12; z++)
            for (int x = 2; x <= 12; x++)
            for (int y = 1; y < 15; y++)
            {
                if (!CornerSolid(w, new int3(x, y, z)) || CornerSolid(w, new int3(x, y + 1, z)))
                    continue;
                foreach (float ox in offs)
                foreach (float oz in offs)
                {
                    if (ox == 0.5f && oz == 0.5f)
                        continue;
                    float3 target = new float3(x + ox, y + 1 - 0.18f, z + oz);
                    for (int az = 0; az < 12; az++)
                    foreach (float elDeg in new[] { 20f, 45f, 70f })
                    {
                        float a = az * math.PI / 6f + 0.13f, el = math.radians(elDeg);
                        float3 dir = new float3(math.cos(el) * math.cos(a), -math.sin(el), math.cos(el) * math.sin(a));
                        float3 o = target - dir * L;
                        if (ClearanceToSolid(w, o) < 0.4f)
                            continue;
                        rays++;
                        bool hit = rm.Hit(o, dir, L - 1e-4f, out float dist, out float3 n);
                        if (!hit || math.dot(n, dir) >= 0f)
                        {
                            holes++;
                            if (holes <= 8)
                                sample += $" [target {target} az {az} el {elDeg} {(!hit ? "no hit" : $"back face at {o + dir * dist}")}]";
                        }
                    }
                }
            }
            return holes;
        }

        [TestCase("H1")]
        [TestCase("H2")]
        [TestCase("H3")]
        [TestCase("H3b")]
        [TestCase("H3c")]
        [TestCase("H4")]
        public void NoSeeThroughAtCorners(string config)
        {
            var w = CornerWorld(config);
            var mesh = BlockMesher.Build(w, 0, 0, 0);
            int holes = SeeThroughRays(w, new RayMesh(mesh), out int rays, out string sample);
            TestContext.WriteLine($"{config}: {holes}/{rays} see-through rays, {mesh.Vertices.Length} vertices{sample}");
            Assert.Greater(rays, 1000, "enough rays");
            Assert.AreEqual(0, holes, $"{config}: see-through rays{sample}");
        }

        [TestCase("H1")]
        [TestCase("H2")]
        [TestCase("H3")]
        [TestCase("H3b")]
        [TestCase("H3c")]
        [TestCase("H4")]
        public void NoBevelDentShowsAtGrassCornersFromAbove(string config)
        {
            // Brief 0073 QĐ 5: vết lõm bo cạnh ở góc lõm (cạnh lồi rẽ góc lõm) không được mép cỏ che → hiện thành tam giác tối.
            // Tia từ trên xuống (thẳng đứng + xiên 45° theo 4 hướng chéo) vào điểm trên mặt trên ô cỏ trong vùng ≤ R quanh mỗi góc ô
            // phải chạm mặt quay về phía tia ở y ≥ mặt trên − 0,02 (mặt trên ô, mặt trên mép cỏ, mảnh góc lõm, hoặc khối cao hơn).
            var w = CornerWorld(config);
            var mesh = BlockMesher.Build(w, 0, 0, 0);
            var rm = new RayMesh(mesh);
            var dirs = new[]
            {
                new float3(0, -1, 0), math.normalize(new float3(1, -1, 1)), math.normalize(new float3(-1, -1, 1)),
                math.normalize(new float3(1, -1, -1)), math.normalize(new float3(-1, -1, -1)),
            };
            var offs = new[] { new float2(0.02f, 0.02f), new float2(0.06f, 0.02f), new float2(0.02f, 0.06f), new float2(0.06f, 0.06f), new float2(0.1f, 0.02f), new float2(0.02f, 0.1f) };
            int dents = 0, probes = 0;
            string sample = "";
            for (int z = 2; z <= 12; z++)
            for (int x = 2; x <= 12; x++)
            for (int y = 1; y < 15; y++)
            {
                if (w.GetMaterial(x, y, z) != BlockMaterial.Grass || CornerSolid(w, new int3(x, y + 1, z)))
                    continue;
                foreach (int sx in new[] { 0, 1 })
                foreach (int sz in new[] { 0, 1 })
                foreach (var o in offs)
                foreach (var d in dirs)
                {
                    float3 t = new float3(x + sx + (sx == 0 ? o.x : -o.x), y + 1, z + sz + (sz == 0 ? o.y : -o.y));
                    float3 origin = t - d * 0.6f;
                    if (ClearanceToSolid(w, origin) < R)
                        continue; // gốc sát vách khối cao hơn → có thể nằm trong phần bo của vách, không phải chỗ mép cỏ
                    probes++;
                    bool hit = rm.Hit(origin, d, 0.6f + 0.5f, out float dist, out float3 n);
                    float3 at = origin + d * dist;
                    if (!hit || math.dot(n, d) >= 0f || at.y < y + 1 - 0.02f)
                    {
                        dents++;
                        if (dents <= 8)
                            sample += $" [{t} dir {d} {(hit ? $"hit {at}" : "no hit")}]";
                    }
                }
            }
            TestContext.WriteLine($"{config}: {dents}/{probes} top probes show a dent, {mesh.Vertices.Length} vertices{sample}");
            Assert.Greater(probes, 500, "enough probes");
            Assert.AreEqual(0, dents, $"{config}: bevel dent visible from above{sample}");
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
                Assert.AreEqual(baseColor >> 24, c >> 24, "keeps pattern code");
                bool touchesWall = p.x >= 5f - 1e-4f && p.x <= 6f + 1e-4f && p.z >= 4f - 1e-4f && p.z <= 5f + 1e-4f;
                bool far = math.abs(p.x - 5.5f) > 2f || math.abs(p.z - 4.5f) > 2f;
                if (touchesWall && (c & 0xFF) < (baseColor & 0xFF))
                    darkNearWall++;
                if (far)
                {
                    Assert.AreEqual(baseColor, c, $"vertex far from wall is not darkened at {p}");
                    farChecked++;
                }
            }
            Assert.Greater(darkNearWall, 0, "base vertex next to wall foot must be darker (AO)");
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
            var stone = Cell.Make(BlockMaterial.Stone);
            for (int y = 7; y <= 8; y++)
            {
                w.Set(3, y, 4, stone); w.Set(5, y, 4, stone); w.Set(4, y, 3, stone); w.Set(4, y, 5, stone);
            }
            w.Set(7, 7, 8, stone); w.Set(9, 7, 8, stone); w.Set(8, 7, 7, stone); w.Set(8, 7, 9, stone); w.Set(8, 6, 8, stone);
            var mesh = BlockMesher.Build(w, 0, 0, 0);
            Assert.AreEqual(2, mesh.WaterTriangleCount, "exactly one water quad");
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
                Assert.Greater(math.cross(b - a, c - a).y, 0f, "water face points up (same convention as front face)");
            }
        }

        static ushort Water() => Cell.Make(BlockMaterial.Water, Cell.MaxWaterLevel);

        [Test]
        public void FallingWaterColumnHasSideFaces()
        {
            var w = new WorldData(new WorldSize(16, 16, 16));
            w.Set(4, 3, 4, Cell.Make(BlockMaterial.Stone));
            for (int y = 4; y <= 7; y++)
                w.Set(4, y, 4, Water());
            var mesh = BlockMesher.Build(w, 0, 0, 0);
            Assert.AreEqual(4 * 4 * 2 + 2, mesh.WaterTriangleCount);
            float topY = 8f - BlockMesher.WaterSurfaceDrop;
            bool hasLow = false, hasTop = false;
            for (int k = 0; k < mesh.WaterIndices.Length; k++)
            {
                float3 p = mesh.Vertices[mesh.WaterIndices[k]];
                Assert.That(p.x, Is.InRange(4f - 1e-5f, 5f + 1e-5f));
                Assert.That(p.z, Is.InRange(4f - 1e-5f, 5f + 1e-5f));
                Assert.That(p.y, Is.InRange(4f - 1e-5f, topY + 1e-5f));
                if (math.abs(p.y - 4f) < 1e-5f) hasLow = true;
                if (math.abs(p.y - topY) < 1e-5f) hasTop = true;
            }
            Assert.IsTrue(hasLow);
            Assert.IsTrue(hasTop);
            for (int t = 0; t < mesh.WaterIndices.Length; t += 3)
            {
                int ia = mesh.WaterIndices[t];
                float3 a = mesh.Vertices[ia], b = mesh.Vertices[mesh.WaterIndices[t + 1]], c = mesh.Vertices[mesh.WaterIndices[t + 2]];
                float3 n = mesh.Normals[ia];
                Assert.Greater(math.dot(math.cross(b - a, c - a), n), 0f);
                bool ok = n.Equals(new float3(0, 1, 0)) || n.Equals(new float3(1, 0, 0)) || n.Equals(new float3(-1, 0, 0))
                    || n.Equals(new float3(0, 0, 1)) || n.Equals(new float3(0, 0, -1));
                Assert.IsTrue(ok, $"normal {n}");
            }
        }

        [Test]
        public void EnclosedWaterHasOnlyTopFaces()
        {
            var w = new WorldData(new WorldSize(16, 16, 16));
            var stone = Cell.Make(BlockMaterial.Stone);
            for (int x = 2; x <= 6; x++)
            for (int z = 2; z <= 5; z++)
            {
                w.Set(x, 4, z, stone);
                if (x == 2 || x == 6 || z == 2 || z == 5)
                    w.Set(x, 5, z, stone);
            }
            for (int x = 3; x <= 5; x++)
            for (int z = 3; z <= 4; z++)
                w.Set(x, 5, z, Water());
            var mesh = BlockMesher.Build(w, 0, 0, 0);
            Assert.AreEqual(6 * 2, mesh.WaterTriangleCount);
        }

        [Test]
        public void FloatingWaterCellHasBottomFace()
        {
            var w = new WorldData(new WorldSize(16, 16, 16));
            w.Set(4, 8, 4, Water());
            var mesh = BlockMesher.Build(w, 0, 0, 0);
            Assert.AreEqual(6 * 2, mesh.WaterTriangleCount);
            int down = 0;
            for (int t = 0; t < mesh.WaterIndices.Length; t += 3)
            {
                int ia = mesh.WaterIndices[t];
                if (!mesh.Normals[ia].Equals(new float3(0, -1, 0)))
                    continue;
                down++;
                for (int k = 0; k < 3; k++)
                    Assert.AreEqual(8f, mesh.Vertices[mesh.WaterIndices[t + k]].y, 1e-5f);
            }
            Assert.AreEqual(2, down);
        }

        [Test]
        public void WaterAtMapEdgeHasNoOuterWall()
        {
            var w = new WorldData(new WorldSize(16, 16, 16));
            var stone = Cell.Make(BlockMaterial.Stone);
            w.Set(0, 4, 0, stone); w.Set(1, 5, 0, stone); w.Set(0, 5, 1, stone);
            w.Set(0, 5, 0, Water());
            var mesh = BlockMesher.Build(w, 0, 0, 0);
            Assert.AreEqual(2, mesh.WaterTriangleCount);
        }

        static WorldData StoneFloor16()
        {
            var w = new WorldData(new WorldSize(16, 16, 16));
            for (int z = 0; z < 16; z++)
            for (int x = 0; x < 16; x++)
                w.Set(x, 4, z, Cell.Make(BlockMaterial.Stone));
            return w;
        }

        static bool HasWaterVertex(ChunkMesh mesh, float3 normal, float3 pos)
        {
            for (int t = 0; t < mesh.WaterIndices.Length; t++)
            {
                int i = mesh.WaterIndices[t];
                if (mesh.Normals[i].Equals(normal) && math.distance(mesh.Vertices[i], pos) < 1e-4f)
                    return true;
            }
            return false;
        }

        [Test]
        public void FlowingWaterSurfaceFollowsLevel()
        {
            var w = StoneFloor16();
            w.Set(4, 5, 4, Cell.MakeFlowing(0, 5, false));
            w.Set(5, 5, 4, Cell.MakeFlowing(0, 3, false));
            var mesh = BlockMesher.Build(w, 0, 0, 0, true, 5);
            var up = new float3(0, 1, 0);
            Assert.IsTrue(HasWaterVertex(mesh, up, new float3(4, 5.9f, 4)));
            Assert.IsTrue(HasWaterVertex(mesh, up, new float3(5, 5.75f, 4)));
            Assert.IsTrue(HasWaterVertex(mesh, up, new float3(5, 5.75f, 5)));
            Assert.IsTrue(HasWaterVertex(mesh, up, new float3(6, 5.6f, 4)));
            bool minusZ = false;
            for (int t = 0; t < mesh.WaterIndices.Length; t += 3)
            {
                bool allX5 = true;
                for (int k = 0; k < 3; k++)
                {
                    int i = mesh.WaterIndices[t + k];
                    float3 n = mesh.Normals[i];
                    float3 v = mesh.Vertices[i];
                    if (math.abs(n.x) != 1f || math.abs(v.x - 5f) > 1e-4f)
                        allX5 = false;
                }
                Assert.IsFalse(allX5, "wall between A and B");
            }
            var south = new float3(0, 0, -1);
            minusZ = HasWaterVertex(mesh, south, new float3(4, 5.9f, 4)) && HasWaterVertex(mesh, south, new float3(5, 5.75f, 4));
            Assert.IsTrue(minusZ);
        }

        [Test]
        public void FallingAndSourceWaterStayFull()
        {
            var w = StoneFloor16();
            w.Set(4, 5, 4, Cell.MakeFlowing(0, 2, true));
            w.Set(6, 5, 4, Water());
            var mesh = BlockMesher.Build(w, 0, 0, 0, true, 5);
            int tops = 0;
            for (int t = 0; t < mesh.WaterIndices.Length; t++)
            {
                int i = mesh.WaterIndices[t];
                if (!mesh.Normals[i].Equals(new float3(0, 1, 0)))
                    continue;
                tops++;
                Assert.AreEqual(5.9f, mesh.Vertices[i].y, 1e-4f);
            }
            Assert.Greater(tops, 0);
        }

        [Test]
        public void BuildRejectsBadWaterFullLevel()
        {
            var w = StoneFloor16();
            Assert.Throws<System.ArgumentOutOfRangeException>(() => BlockMesher.Build(w, 0, 0, 0, true, 0));
        }
    }
}
