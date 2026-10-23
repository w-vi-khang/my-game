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
            Assert.IsNotNull(view.World, "Build not run");
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
            Assert.GreaterOrEqual(view.LastRebuildFrame, editFrame, "mesh not rebuilt after 5 frames");
            framesTaken.Add(view.LastRebuildFrame - editFrame);
        }

        float SurfaceRay(int x, int z)
        {
            Assert.IsTrue(Physics.Raycast(new Vector3(x + 0.5f, 60f, z + 0.5f), Vector3.down, out var hit, 100f),
                $"raycast did not hit collider at column {x},{z}");
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
                Assert.AreEqual(expected.Vertices.Length, mesh.vertexCount, $"chunk {c}: vertex count");
                Assert.AreEqual(expected.Indices.Length, (int)mesh.GetIndexCount(0), $"chunk {c}: index count");
                var v = mesh.vertices;
                for (int i = 0; i < v.Length; i++)
                    Assert.AreEqual((Vector3)expected.Vertices[i], v[i], $"chunk {c}: vertex {i}");
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
            Assert.AreEqual(0, view.Bag.Get(material));
            Assert.AreEqual(1, view.Editor.Drops.At(x, y, z).Count);
            yield return WaitRebuild(frame, frames);
            Assert.AreEqual(affected.Count, view.LastRebuildChunks);
            AssertMeshesMatchMesher(affected);
            float y1 = SurfaceRay(x, z);
            Assert.Less(y1, y0 - 0.5f, "collider did not follow the new pit");

            frame = Time.frameCount;
            view.Bag.Add(material);
            Assert.AreEqual(EditResult.Ok, view.Place(cell, material));
            Assert.AreEqual(0, view.Bag.Get(material));
            yield return WaitRebuild(frame, frames);
            AssertMeshesMatchMesher(affected);
            Assert.AreEqual(y0, SurfaceRay(x, z), 1e-3f, "placed back → surface as before");

            Debug.Log($"PLAYTEST frames waited: [{string.Join(", ", frames)}]");
            foreach (var f in frames)
                Assert.LessOrEqual(f, 2, "mesh updated slower than 2 frames");
        }

        [UnityTest]
        public IEnumerator BuildKeepsOtherChildren()
        {
            // Hồi quy (user báo 2026-10-13): Build() xoá mọi con của Terrain → khung chọn ô của TerrainEditInput bị huỷ
            // → MissingReferenceException trong Update, không đào/đặt được.
            var marker = new GameObject("Other Child");
            marker.transform.SetParent(go.transform, false);
            view.Build();
            yield return null;
            Assert.IsTrue(marker != null, "Build() removed a non-chunk child");
            Assert.Greater(go.transform.Find("Chunks").childCount, 0, "no chunks");
            view.Build();
            int roots = 0;
            foreach (Transform t in go.transform)
                if (t.name == "Chunks")
                    roots++;
            yield return null;
            Assert.AreEqual(1, roots, "second Build() left old chunks");
        }

        void FindColumn(out int x, out int z, out int h)
        {
            for (int i = 16; i <= 60; i++)
            {
                int hh = view.World.SurfaceHeight(i, i);
                if (hh < 2) continue;
                var top = view.World.GetMaterial(i, hh, i);
                if (top != BlockMaterial.Dirt && top != BlockMaterial.Grass && top != BlockMaterial.Stone && top != BlockMaterial.Sand) continue;
                var below = view.World.GetMaterial(i, hh - 1, i);
                if (below == BlockMaterial.Empty || below == BlockMaterial.Water) continue;
                var r = view.Editor.CheckPlace(i, hh + 1, i, BlockMaterial.Dirt);
                if (r != EditResult.Ok && r != EditResult.NoMaterial) continue;
                if (view.Editor.CheckPlace(i, hh + 3, i, BlockMaterial.Dirt) != EditResult.NotAttached) continue;
                x = z = i; h = hh;
                return;
            }
            Assert.Fail("test column not found");
            x = z = h = 0;
        }

        [UnityTest]
        public IEnumerator ToolStartsDigAndTabToggles()
        {
            var input = go.AddComponent<TerrainEditInput>();
            yield return null;
            Assert.AreEqual(EditTool.Dig, input.Tool);
            input.ToggleTool();
            Assert.AreEqual(EditTool.Place, input.Tool);
            input.ToggleTool();
            Assert.AreEqual(EditTool.Dig, input.Tool);
        }

        [UnityTest]
        public IEnumerator DigToolShowsOutlineOnly()
        {
            var input = go.AddComponent<TerrainEditInput>();
            yield return null;
            FindColumn(out int x, out int z, out int h);
            input.ShowAim(true, new Vector3Int(x, h, z), new Vector3Int(x, h + 1, z));
            Assert.IsTrue(input.OutlineVisible);
            Assert.IsFalse(input.GhostVisible);
            input.ShowAim(false, default, default);
            Assert.IsFalse(input.OutlineVisible);
            Assert.IsFalse(input.GhostVisible);
        }

        [UnityTest]
        public IEnumerator PlaceToolShowsGhostAtPlaceCell()
        {
            var input = go.AddComponent<TerrainEditInput>();
            yield return null;
            FindColumn(out int x, out int z, out int h);
            input.ToggleTool();
            view.Bag.Add(BlockMaterial.Dirt);
            var place = new Vector3Int(x, h + 1, z);
            Assert.AreEqual(EditResult.Ok, view.Editor.CheckPlace(place.x, place.y, place.z, BlockMaterial.Dirt));
            input.ShowAim(true, new Vector3Int(x, h, z), place);
            Assert.IsTrue(input.GhostVisible);
            Assert.IsFalse(input.OutlineVisible);
            Assert.AreEqual(place, input.GhostCell);
            Assert.AreEqual(TerrainEditInput.GhostColorOf(BlockMaterial.Dirt), input.GhostColor);
        }

        [UnityTest]
        public IEnumerator GhostRedWhenOutOfMaterial()
        {
            var input = go.AddComponent<TerrainEditInput>();
            yield return null;
            FindColumn(out int x, out int z, out int h);
            input.ToggleTool();
            var place = new Vector3Int(x, h + 1, z);
            Assert.AreEqual(EditResult.NoMaterial, view.Editor.CheckPlace(place.x, place.y, place.z, BlockMaterial.Dirt));
            input.ShowAim(true, new Vector3Int(x, h, z), place);
            Assert.IsTrue(input.GhostVisible);
            Assert.AreEqual(TerrainEditInput.MissingColor, input.GhostColor);
        }

        [UnityTest]
        public IEnumerator GhostHiddenWhenRuleFails()
        {
            var input = go.AddComponent<TerrainEditInput>();
            yield return null;
            FindColumn(out int x, out int z, out int h);
            input.ToggleTool();
            view.Bag.Add(BlockMaterial.Dirt);
            var dig = new Vector3Int(x, h, z);
            input.ShowAim(true, dig, new Vector3Int(x, h + 3, z));
            Assert.IsFalse(input.GhostVisible);
            var place = new Vector3Int(x, h + 1, z);
            view.Editor.IsOccupied = (cx, cy, cz) => true;
            input.ShowAim(true, dig, place);
            view.Editor.IsOccupied = null;
            Assert.IsFalse(input.GhostVisible);
        }

        [UnityTest]
        public IEnumerator GhostHasNoColliderAndShader()
        {
            var input = go.AddComponent<TerrainEditInput>();
            yield return null;
            Assert.IsNull(input.Ghost.GetComponent<Collider>());
            Assert.AreEqual("Ghost", input.Ghost.name);
            Assert.IsNotNull(Shader.Find("Game/GhostBlock"));
            view.Build();
            yield return null;
            Assert.IsNotNull(go.transform.Find("Ghost"));
        }

        [UnityTest]
        public IEnumerator UseToolDigsOrPlacesByTool()
        {
            var input = go.AddComponent<TerrainEditInput>();
            yield return null;
            FindColumn(out int x, out int z, out int h);
            var dig = new Vector3Int(x, h, z);
            var place = new Vector3Int(x, h + 1, z);
            var mat = view.World.GetMaterial(x, h, z);
            int before = view.Bag.Get(mat);
            Assert.AreEqual(EditResult.Ok, input.UseTool(dig, place));
            Assert.AreEqual(BlockMaterial.Empty, view.World.GetMaterial(x, h, z));
            Assert.AreEqual(before, view.Bag.Get(mat));
            Assert.AreEqual(1, view.Editor.Drops.At(x, h, z).Count);
            input.ToggleTool();
            view.Bag.Add(BlockMaterial.Dirt);
            Assert.AreEqual(EditResult.Ok, input.UseTool(dig, dig));
            Assert.AreEqual(BlockMaterial.Dirt, view.World.GetMaterial(x, h, z));
        }

        [UnityTest]
        public IEnumerator EditInputSurvivesBuild()
        {
            var input = go.AddComponent<TerrainEditInput>();
            view.Build();
            yield return null;
            Assert.IsNotNull(go.transform.Find("Selection"), "selection frame destroyed by Build()");
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
                view.Bag.Add(BlockMaterial.Dirt, 16 - surface);
                for (int y = surface + 1; y < 16; y++)
                    Assert.AreEqual(EditResult.Ok, view.Place(new Vector3Int(16, y, 16), BlockMaterial.Dirt), $"supporting column y = {y}");
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
            Debug.Log($"PLAYTEST 8-chunk corner: mesh {view.LastMeshMs:F2} ms, write {view.LastWriteMs:F2} ms, bake {view.LastBakeMs:F2} ms, waited {frames[0]} frames");
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
            Debug.Log($"PLAYTEST 1 chunk (avg {n} runs): mesh {mesh / n:F2} ms, write {write / n:F2} ms, bake {bake / n:F2} ms");
        }

        [UnityTest]
        public IEnumerator SeaFlowsInGraduallyAfterDig()
        {
            // 0013: đào ô bờ biển ở mực nước biển → không ngập ngay; TerrainChunkView chạy StepWater theo nhịp → ngập sau vài nhịp.
            var p = ConfigLoader.LoadTerrainParams();
            int S = p.waterLevel;
            var w = view.World;
            Vector3Int cell = default;
            bool found = false;
            for (int z = 1; z < p.sizeZ - 1 && !found; z++)
            for (int x = 1; x < p.sizeX - 1 && !found; x++)
            {
                if (!Cell.IsDiggable(w.Get(x, S, z)))
                    continue;
                if (w.GetMaterial(x - 1, S, z) == BlockMaterial.Water || w.GetMaterial(x + 1, S, z) == BlockMaterial.Water ||
                    w.GetMaterial(x, S, z - 1) == BlockMaterial.Water || w.GetMaterial(x, S, z + 1) == BlockMaterial.Water)
                {
                    cell = new Vector3Int(x, S, z);
                    found = true;
                }
            }
            Assert.IsTrue(found, "no shore cell at water level");
            Assert.AreEqual(EditResult.Ok, view.Dig(cell));
            Assert.AreEqual(BlockMaterial.Empty, w.GetMaterial(cell.x, cell.y, cell.z), "not flooded right after digging");
            Assert.IsTrue(view.Editor.HasPendingWater);
            float start = Time.time;
            while (view.Editor.HasPendingWater && Time.time - start < 5f)
                yield return null;
            Assert.IsFalse(view.Editor.HasPendingWater, "water must settle within 5 seconds");
            Assert.AreEqual(BlockMaterial.Water, w.GetMaterial(cell.x, cell.y, cell.z), "shore cell flooded");
            Assert.Greater(Time.time - start, 0.5f / view.waterStepsPerSecond, "floods after at least one tick, not instantly");
        }
    }
}
