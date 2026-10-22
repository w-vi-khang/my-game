using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Game.Client.Tests
{
    /// <summary>World 7h b (brief 0039): WalkBench — điểm dẫn, kiểm vòng đất khô, chọn bán kính, tự đi + ghi dòng WALKBENCH.</summary>
    public class WalkBenchPlayTests
    {
        GameObject camGo, go, playerGo;
        TerrainChunkView view;
        SimView sv;
        WalkBench wb;
        PlayerCharacter player;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            camGo = new GameObject("Cam");
            camGo.tag = "MainCamera";
            camGo.AddComponent<Camera>();
            camGo.AddComponent<DebugFlyCamera>();
            camGo.AddComponent<FollowCamera>();
            go = new GameObject("Terrain");
            go.SetActive(false);
            view = go.AddComponent<TerrainChunkView>();
            view.source = WorldSource.MapsV3;
            view.streaming = true;
            sv = go.AddComponent<SimView>();
            wb = go.AddComponent<WalkBench>();
            wb.warmupSeconds = 0.5f;
            wb.measureSeconds = 4f;
            playerGo = new GameObject("Player");
            playerGo.SetActive(false);
            player = playerGo.AddComponent<PlayerCharacter>();
            player.terrain = view;
            player.readKeyboard = false;
            playerGo.SetActive(true);
            go.SetActive(true);
            yield return null;
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Object.Destroy(playerGo);
            Object.Destroy(go);
            Object.Destroy(camGo);
            yield return null;
        }

        [UnityTest]
        public IEnumerator CarrotLeadsAlongCircle()
        {
            var a = WalkBench.Carrot(Vector2.zero, 10f, new Vector2(0f, 5f), 25f);
            Assert.AreEqual(4.2262f, a.x, 1e-3f);
            Assert.AreEqual(9.0631f, a.y, 1e-3f);
            var b = WalkBench.Carrot(Vector2.zero, 10f, new Vector2(-3f, 0f), 25f);
            Assert.AreEqual(-9.0631f, b.x, 1e-3f);
            Assert.AreEqual(4.2262f, b.y, 1e-3f);
            yield return null;
        }

        [UnityTest]
        public IEnumerator RouteIsDryRejectsSeaAndOutside()
        {
            for (int i = 0; i < 300 && view.World == null; i++)
                yield return null;
            Assert.IsNotNull(view.World);
            Assert.IsFalse(WalkBench.RouteIsDry(view.World, new Vector2(12f, 12f), 5));
            Assert.IsFalse(WalkBench.RouteIsDry(view.World, new Vector2(2f, 2f), 5));
        }

        [UnityTest]
        public IEnumerator DefaultSeedRouteIsDry()
        {
            for (int i = 0; i < 300 && wb.RouteRadius <= 0; i++)
                yield return null;
            Assert.Greater(wb.RouteRadius, 0);
            CollectionAssert.Contains(WalkBench.RadiusCandidates, wb.RouteRadius);
            Assert.IsTrue(wb.RouteDry);
            Assert.IsTrue(WalkBench.RouteIsDry(view.World, wb.RouteCenter, wb.RouteRadius));
        }

        [UnityTest]
        public IEnumerator WalksRouteAndLogsSummary()
        {
            float t0 = Time.realtimeSinceStartup;
            while (!wb.Done && Time.realtimeSinceStartup - t0 < 60f)
                yield return null;
            Assert.IsTrue(wb.Done);
            Assert.Greater(wb.Distance, 5f, $"stuck={wb.StuckCount}");
            foreach (var key in new[] { "WALKBENCH ", "avgFps=", "p50Ms=", "p99Ms=", "maxMs=", "distance=", "stuck=", "routeR=", "holeFrames=", "maxMissing=", "loadedChunksMax=", "sysMemMaxMB=", "pass30fps=", "passNoHole=" })
                StringAssert.Contains(key, wb.Summary);
            Assert.AreEqual(Vector2.zero, player.MoveInput);
        }
    }
}
