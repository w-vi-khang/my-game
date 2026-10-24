using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Game.Client.Tests
{
    /// <summary>World 7h b (brief 0039) + 7i (brief 0052): WalkBench — carrot, dry-route check, radius pick, auto walk + WALKBENCH line, second viewpoint at Map 2.</summary>
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
            wb.secondViewpoint = TestContext.CurrentContext.Test.Name == nameof(SecondViewpointWalkLogsSummary);
            wb.targetPlots = TestContext.CurrentContext.Test.Name == nameof(TargetPlotsMarksTwenty) || TestContext.CurrentContext.Test.Name == nameof(TargetPlotsAheadOnRoute) ? 20 : 0;
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
            foreach (var key in new[] { "WALKBENCH ", "viewpoints=1", "secondHoleFrames=0", "loadedColumnsMax=", "avgFps=", "p50Ms=", "p99Ms=", "maxMs=", "distance=", "stuck=", "routeR=", "holeFrames=", "maxMissing=", "loadedChunksMax=", "sysMemMaxMB=", "pass30fps=", "passNoHole=" })
                StringAssert.Contains(key, wb.Summary);
            Assert.AreEqual(Vector2.zero, player.MoveInput);
            Assert.IsNull(wb.SecondViewpoint);
        }

        [UnityTest]
        public IEnumerator TargetPlotsMarksTwenty()
        {
            float t0 = Time.realtimeSinceStartup;
            while (!wb.Done && Time.realtimeSinceStartup - t0 < 60f)
                yield return null;
            Assert.IsTrue(wb.Done);
            Assert.GreaterOrEqual(sv.Sim.State.Plots.Count, 20);
            StringAssert.Contains("plots=", wb.Summary);
            StringAssert.Contains("plotIconsVisibleAvg=", wb.Summary);
            StringAssert.Contains("plotIconsVisibleMax=", wb.Summary);
            StringAssert.Contains("iconFrames=", wb.Summary);
            StringAssert.Contains("iconFramesAvgMs=", wb.Summary);
        }

        [UnityTest]
        public IEnumerator TargetPlotsAheadOnRoute()
        {
            for (int i = 0; i < 300 && sv.Sim.State.Plots.Count < 20; i++)
                yield return null;
            Assert.GreaterOrEqual(sv.Sim.State.Plots.Count, 20);
            // The sim starts with a few demo plots near the camp; the bench only adds plots up to the target.
            int ahead = 0;
            foreach (var plot in sv.Sim.State.Plots)
            {
                float d = Vector2.Distance(new Vector2(plot.Soil.X + 0.5f, plot.Soil.Z + 0.5f), wb.RouteCenter);
                if (d >= wb.RouteRadius - 6f && d <= wb.RouteRadius - 2f) ahead++;
            }
            Assert.GreaterOrEqual(ahead, 16, $"plots={sv.Sim.State.Plots.Count}");
        }

        [UnityTest]
        public IEnumerator MapCentroidLiesInMap2()
        {
            for (int i = 0; i < 300 && view.Maps == null; i++)
                yield return null;
            Assert.IsNotNull(view.Maps);
            var c = WalkBench.MapCentroid(view.Maps, WalkBench.SecondMap);
            Assert.AreEqual(c, WalkBench.MapCentroid(view.Maps, WalkBench.SecondMap));
            Assert.AreEqual(WalkBench.SecondMap, (int)view.Maps.Get(Mathf.FloorToInt(c.x), Mathf.FloorToInt(c.y)));
            Assert.Throws<System.ArgumentException>(() => WalkBench.MapCentroid(view.Maps, 7));
        }

        [UnityTest]
        public IEnumerator SecondViewpointWalkLogsSummary()
        {
            float t0 = Time.realtimeSinceStartup;
            while (!wb.Done && Time.realtimeSinceStartup - t0 < 60f)
                yield return null;
            Assert.IsTrue(wb.Done);
            Assert.IsNotNull(wb.SecondViewpoint);
            Assert.AreEqual(2, view.ViewpointCount);
            foreach (var key in new[] { "viewpoints=2", "secondCenter=(", "secondHoleFrames=", "loadedColumnsMax=", "passNoHole=" })
                StringAssert.Contains(key, wb.Summary);
            var cell = view.CellAt(wb.SecondViewpoint.position);
            Assert.AreEqual(Mathf.FloorToInt(wb.SecondCenter.x), cell.x);
            Assert.AreEqual(Mathf.FloorToInt(wb.SecondCenter.y), cell.z);
            for (int i = 0; i < 300 && view.MissingColumnsWithin(wb.SecondViewpoint, 128) != 0; i++)
                yield return null;
            Assert.AreEqual(0, view.MissingColumnsWithin(wb.SecondViewpoint, 128));
        }
    }
}
