using System.Collections;
using System.Collections.Generic;
using Game.Core;
using Game.Core.Sim;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Game.Client.Tests
{
    /// <summary>Sim bước 7 (brief 0028): SimView ở chế độ đo — đồng bằng, 60 sinh vật, bộ đo SIMBENCH.</summary>
    public class SimBenchPlayTests
    {
        GameObject camGo, go;
        TerrainChunkView view;
        SimView sv;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            camGo = new GameObject("Cam");
            camGo.tag = "MainCamera";
            camGo.AddComponent<Camera>();
            go = new GameObject("Terrain");
            go.SetActive(false);
            view = go.AddComponent<TerrainChunkView>();
            view.source = WorldSource.MapsV3;
            view.streaming = true;
            view.streamTarget = camGo.transform;
            sv = go.AddComponent<SimView>();
            sv.stressBench = true;
            go.SetActive(true);
            yield return null;
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Object.Destroy(go);
            Object.Destroy(camGo);
            yield return null;
        }

        [UnityTest]
        public IEnumerator SetsUpStressFarmOnPlain()
        {
            Assert.IsTrue(sv.Plain.HasValue);
            var plain = sv.Plain.Value;
            Assert.AreEqual(60, sv.Sim.State.Creatures.Count);
            Assert.IsNotNull(sv.Meter);
            var expect = SimView.BenchCameraPos(plain);
            Assert.AreEqual(0f, Vector3.Distance(expect, camGo.transform.position), 0.01f);
            foreach (var l in view.Landmarks)
                Assert.IsFalse(plain.Contains(l.X, l.Z));
            Assert.IsTrue(sv.Sim.ProfilePhases);
            yield return null;
        }

        [UnityTest]
        public IEnumerator MeterWindowAndSummary()
        {
            var m = new SimBenchMeter(20f);
            for (int i = 0; i < 100; i++) m.AddTick(1000);
            m.AddFrame(0f, 0.016f, 5, sv.Sim);
            m.AddFrame(10f, 0.016f, 3, sv.Sim);
            StringAssert.StartsWith("SIMBENCH t=10s ticks=100", m.LastLine);
            StringAssert.Contains("maxTicksFrame=5", m.LastLine);
            m.AddFrame(20f, 0.016f, 1, sv.Sim);
            Assert.IsTrue(m.Done);
            StringAssert.StartsWith("SIMBENCH TONG", m.Summary);
            StringAssert.Contains("pass5ms=", m.Summary);
            StringAssert.Contains("phaseUs Terrain=", m.Summary);
            yield return null;
        }

        [UnityTest]
        public IEnumerator PercentileMatchesRule()
        {
            var l = new List<double>();
            for (int i = 1; i <= 100; i++) l.Add(i);
            Assert.AreEqual(95, SimBenchMeter.Percentile(l, 0.95));
            Assert.AreEqual(99, SimBenchMeter.Percentile(l, 0.99));
            Assert.AreEqual(50, SimBenchMeter.Percentile(l, 0.5));
            var one = new List<double> { 5 };
            foreach (var q in new[] { 0.0, 0.5, 0.95, 0.99, 1.0 })
                Assert.AreEqual(5, SimBenchMeter.Percentile(one, q));
            yield return null;
        }
    }
}
