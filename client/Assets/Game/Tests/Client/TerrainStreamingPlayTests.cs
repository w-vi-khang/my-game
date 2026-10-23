using System.Collections;
using Game.Core;
using Game.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Game.Client.Tests
{
    /// <summary>World-tech bước 7b (brief 0010): nạp dần cột chunk quanh camera — không lỗ trong bán kính sương, dùng lại slot, đào vẫn dựng lại.</summary>
    public class TerrainStreamingPlayTests
    {
        GameObject camGo, go;
        TerrainChunkView view;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            camGo = new GameObject("Cam");
            camGo.transform.position = new Vector3(192, 40, 60);
            go = new GameObject("Terrain");
            view = go.AddComponent<TerrainChunkView>();
            view.source = WorldSource.MapsV3;
            view.streaming = true;
            view.streamTarget = camGo.transform;
            yield return null; // Start → Build
            Assert.IsNotNull(view.World, "Build not run");
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Object.Destroy(go);
            Object.Destroy(camGo);
            yield return null;
        }

        void AssertLoadedWithinUnloadRadius()
        {
            var cell = view.CellAt(camGo.transform.position);
            for (int col = 0; col < 576; col++)
                if (view.ChunkMesh(col) != null)
                    Assert.LessOrEqual(ChunkStreamPlan.DistanceSq(cell.x, cell.z, col % 24, col / 24), 160L * 160, $"column {col}");
        }

        [UnityTest]
        public IEnumerator InitialLoadCoversFogRadiusOnly()
        {
            yield return null;
            Assert.AreEqual(0, view.MissingColumnsWithin(128));
            Assert.AreEqual(view.LoadedColumnCount * 3, view.LoadedChunkCount);
            Assert.Less(view.LoadedChunkCount, 1728);
            AssertLoadedWithinUnloadRadius();
        }

        IEnumerator Fly(System.Action<int> perFrame)
        {
            for (int i = 0; i < 300; i++)
            {
                camGo.transform.position = new Vector3(192, 40, 60 + i + 1);
                yield return null;
                perFrame(i);
            }
        }

        [UnityTest]
        public IEnumerator NoHoleWhileFlyingFast()
        {
            yield return null;
            int maxLoaded = 0;
            yield return Fly(i =>
            {
                Assert.AreEqual(0, view.MissingColumnsWithin(120), $"khung {i}");
                AssertLoadedWithinUnloadRadius();
                maxLoaded = Mathf.Max(maxLoaded, view.LoadedColumnCount);
            });
            Debug.Log($"STREAMTEST maxLoadedColumns={maxLoaded}");
        }

        [UnityTest]
        public IEnumerator UnloadedSlotsAreReused()
        {
            yield return null;
            var root = go.transform.Find("Chunks");
            int maxLoaded = view.LoadedColumnCount;
            yield return Fly(i => maxLoaded = Mathf.Max(maxLoaded, view.LoadedColumnCount));
            Assert.LessOrEqual(root.childCount, (maxLoaded + 2) * 3);
        }

        [UnityTest]
        public IEnumerator DigNearCameraRebuildsWithin2Frames()
        {
            yield return null;
            int frame = Time.frameCount;
            int fx = -1, fy = -1;
            for (int x = 192; x <= 255 && fx < 0; x++)
            {
                int y = view.World.SurfaceHeight(x, 100);
                if (y >= 0 && view.Dig(new Vector3Int(x, y, 100)) == EditResult.Ok)
                {
                    fx = x;
                    fy = y;
                }
            }
            if (fx < 0)
                Assert.Fail("No cell could be dug");
            for (int w = 0; w < 2 && view.LastRebuildFrame < frame; w++)
                yield return null;
            Assert.GreaterOrEqual(view.LastRebuildFrame, frame);
            Assert.IsNotNull(view.ChunkMesh(view.World.Size.ChunkIndex(fx, fy, 100)));
        }
    }
}
