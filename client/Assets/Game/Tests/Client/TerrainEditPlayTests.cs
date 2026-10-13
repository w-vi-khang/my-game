using System.Collections;
using System.Collections.Generic;
using Game.Core;
using Game.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Game.Client.Tests
{
    /// <summary>World-tech bước 4 — "Sửa ô → mesh cập nhật ≤ 2 khung hình trên PC" + collider theo bề mặt mới.</summary>
    public class TerrainEditPlayTests
    {
        GameObject go;
        TerrainChunkView view;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            go = new GameObject("Terrain");
            view = go.AddComponent<TerrainChunkView>();
            yield return null; // Start → Build
            Assert.IsNotNull(view.World, "Build chưa chạy");
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Object.Destroy(go);
            yield return null;
        }

        IEnumerator WaitRebuild(int editFrame, List<int> framesTaken)
        {
            int waited = 0;
            while (view.LastRebuildFrame < editFrame && waited < 5)
            {
                yield return null;
                waited++;
            }
            Assert.GreaterOrEqual(view.LastRebuildFrame, editFrame, "mesh chưa dựng lại sau 5 khung hình");
            framesTaken.Add(view.LastRebuildFrame - editFrame);
        }

        float SurfaceRay(int x, int z)
        {
            Assert.IsTrue(Physics.Raycast(new Vector3(x + 0.5f, 60f, z + 0.5f), Vector3.down, out var hit, 100f),
                $"raycast không trúng collider ở cột {x},{z}");
            return hit.point.y;
        }

        void AssertMeshesMatchMesher(List<int> chunks)
        {
            var s = view.World.Size;
            foreach (var c in chunks)
            {
                int cx = c % s.ChunksX, cz = c / s.ChunksX % s.ChunksZ, cy = c / (s.ChunksX * s.ChunksZ);
                var expected = BlockMesher.Build(view.World, cx, cy, cz);
                var mesh = view.ChunkMesh(c);
                Assert.AreEqual(expected.Vertices.Length, mesh.vertexCount, $"chunk {c}: số đỉnh");
                Assert.AreEqual(expected.Indices.Length, (int)mesh.GetIndexCount(0), $"chunk {c}: số chỉ số");
                var v = mesh.vertices;
                for (int i = 0; i < v.Length; i++)
                    Assert.AreEqual((Vector3)expected.Vertices[i], v[i], $"chunk {c}: đỉnh {i}");
            }
        }

        [UnityTest]
        public IEnumerator DigAndPlaceUpdateMeshAndColliderWithinTwoFrames()
        {
            // Cột 16,16 là góc của 4 chunk theo x/z → dựng lại ≥ 4 chunk.
            int x = 16, z = 16;
            int y = view.World.SurfaceHeight(x, z);
            var cell = new Vector3Int(x, y, z);
            var material = view.World.GetMaterial(x, y, z);
            float y0 = SurfaceRay(x, z);
            var affected = new List<int>();
            view.Editor.AffectedChunks(x, y, z, affected);
            Assert.GreaterOrEqual(affected.Count, 4);
            var frames = new List<int>();

            int frame = Time.frameCount;
            Assert.AreEqual(EditResult.Ok, view.Dig(cell));
            Assert.AreEqual(1, view.Inventory.Get(material));
            yield return WaitRebuild(frame, frames);
            Assert.AreEqual(affected.Count, view.LastRebuildChunks);
            AssertMeshesMatchMesher(affected);
            float y1 = SurfaceRay(x, z);
            Assert.Less(y1, y0 - 0.5f, "collider chưa theo hố vừa đào");

            frame = Time.frameCount;
            Assert.AreEqual(EditResult.Ok, view.Place(cell, material));
            Assert.AreEqual(0, view.Inventory.Get(material));
            yield return WaitRebuild(frame, frames);
            AssertMeshesMatchMesher(affected);
            Assert.AreEqual(y0, SurfaceRay(x, z), 1e-3f, "đặt lại → bề mặt như cũ");

            Debug.Log($"PLAYTEST khung hình chờ: [{string.Join(", ", frames)}]");
            foreach (var f in frames)
                Assert.LessOrEqual(f, 2, "mesh cập nhật chậm hơn 2 khung hình");
        }

        [UnityTest]
        public IEnumerator BuildKeepsOtherChildren()
        {
            // Hồi quy (user báo 2026-10-13): Build() xoá mọi con của Terrain → khung chọn ô của TerrainEditInput bị huỷ
            // → MissingReferenceException trong Update, không đào/đặt được.
            var marker = new GameObject("con khác");
            marker.transform.SetParent(go.transform, false);
            view.Build();
            yield return null;
            Assert.IsTrue(marker != null, "Build() đã xoá con không phải chunk");
            Assert.Greater(go.transform.Find("Chunks").childCount, 0, "không có chunk");
            view.Build();
            int roots = 0;
            foreach (Transform t in go.transform)
                if (t.name == "Chunks")
                    roots++;
            yield return null;
            Assert.AreEqual(1, roots, "Build() lần hai để lại chunk cũ");
        }

        [UnityTest]
        public IEnumerator EditInputSurvivesBuild()
        {
            var input = go.AddComponent<TerrainEditInput>();
            view.Build();
            yield return null;
            Assert.IsNotNull(go.transform.Find("Ô đang chọn"), "khung chọn ô bị Build() huỷ");
            Assert.IsTrue(input.isActiveAndEnabled);
        }

        [UnityTest]
        public IEnumerator CornerEditRebuildsEightChunks()
        {
            // Ô 16,16,16 là góc của 8 chunk (4 × 2 × 4 chunk của map M1).
            var cell = new Vector3Int(16, 16, 16);
            var frames = new List<int>();
            int surface = view.World.SurfaceHeight(16, 16);
            if (surface < 16)
            {
                // Luật đặt (0012): khối phải kề khối đặc → dựng cột đỡ từ mặt đất (kể cả trong nước) lên y = 15 trước.
                view.Inventory.Add(BlockMaterial.Dirt, 16 - surface);
                for (int y = surface + 1; y < 16; y++)
                    Assert.AreEqual(EditResult.Ok, view.Place(new Vector3Int(16, y, 16), BlockMaterial.Dirt), $"cột đỡ y = {y}");
                yield return null;
            }
            int frame = Time.frameCount;
            if (view.World.GetMaterial(16, 16, 16) == BlockMaterial.Empty)
            {
                Assert.AreEqual(EditResult.Ok, view.Place(cell, BlockMaterial.Dirt));
            }
            else
            {
                Assert.AreEqual(EditResult.Ok, view.Dig(cell));
            }
            yield return WaitRebuild(frame, frames);
            Assert.AreEqual(8, view.LastRebuildChunks);
            Assert.LessOrEqual(frames[0], 2);
            Debug.Log($"PLAYTEST góc 8 chunk: mesh {view.LastMeshMs:F2} ms, ghi {view.LastWriteMs:F2} ms, bake {view.LastBakeMs:F2} ms, chờ {frames[0]} khung hình");
        }

        [UnityTest]
        public IEnumerator TimingSingleChunkEdits()
        {
            // Đo 20 lần sửa giữa chunk (1 chunk) — ghi trung bình vào log.
            var rng = new System.Random(7);
            double mesh = 0, write = 0, bake = 0;
            int n = 0;
            while (n < 20)
            {
                int x = 16 * rng.Next(4) + 4 + rng.Next(8), z = 16 * rng.Next(4) + 4 + rng.Next(8);
                int y = view.World.SurfaceHeight(x, z);
                if (y % 16 < 3 || y % 16 > 12)
                    continue;
                int frame = Time.frameCount;
                if (view.Dig(new Vector3Int(x, y, z)) != EditResult.Ok)
                    continue;
                var frames = new List<int>();
                yield return WaitRebuild(frame, frames);
                Assert.AreEqual(1, view.LastRebuildChunks);
                mesh += view.LastMeshMs;
                write += view.LastWriteMs;
                bake += view.LastBakeMs;
                n++;
            }
            Debug.Log($"PLAYTEST 1 chunk (TB {n} lần): mesh {mesh / n:F2} ms, ghi {write / n:F2} ms, bake {bake / n:F2} ms");
        }
    }
}
