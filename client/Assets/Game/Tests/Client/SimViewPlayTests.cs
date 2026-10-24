using System.Collections;
using Game.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Game.Client.Tests
{
    /// <summary>Sim bước 6 — SimView dựng được demo, khoá 60 FPS và trả lại cài đặt FPS khi huỷ.</summary>
    public class SimViewPlayTests
    {
        [UnityTest]
        public IEnumerator SetsUpDemoAndRestoresFrameRate()
        {
            int before = Application.targetFrameRate;
            int vs = QualitySettings.vSyncCount;
            var go = new GameObject("Terrain");
            var view = go.AddComponent<TerrainChunkView>();
            var sv = go.AddComponent<SimView>();
            yield return null;
            yield return null;

            Assert.IsNotNull(sv.Sim);
            Assert.AreEqual(3, sv.Sim.State.Creatures.Count);
            Assert.AreEqual(3, sv.BodyCount);
            Assert.AreEqual(4, sv.SproutCount);
            Assert.AreEqual(60, sv.TargetFps);
            Assert.AreEqual(60, Application.targetFrameRate);
            Assert.AreEqual(0, QualitySettings.vSyncCount);
            Assert.IsTrue(view.Editor.IsOccupied(34, 18, 17));

            Object.Destroy(go);
            yield return null;
            Assert.AreEqual(before, Application.targetFrameRate);
            Assert.AreEqual(vs, QualitySettings.vSyncCount);
        }

        [UnityTest]
        public IEnumerator StatsTextOnTop()
        {
            var go = new GameObject("Terrain");
            go.AddComponent<TerrainChunkView>();
            var sv = go.AddComponent<SimView>();
            yield return null;
            yield return null;
            var t = sv.StatsText();
            StringAssert.Contains("FPS", t);
            StringAssert.Contains("nhịp", t); // vi-text
            StringAssert.DoesNotContain("F1", t);
            Object.Destroy(go);
            yield return null;
        }

        [UnityTest]
        public IEnumerator TilledPlotBecomesFarmlandAndRebuildsChunk()
        {
            var go = new GameObject("Terrain");
            var view = go.AddComponent<TerrainChunkView>();
            var sv = go.AddComponent<SimView>();
            yield return null;
            yield return null;

            var p = sv.Sim.State.Plots[0];
            int f0 = view.LastRebuildFrame;
            for (int i = 0; i < 2000 && !p.Tilled; i++) sv.Sim.Tick();
            Assert.IsTrue(p.Tilled);
            yield return null;

            Assert.AreEqual(BlockMaterial.Farmland, view.World.GetMaterial(p.Soil.X, p.Soil.Y, p.Soil.Z));
            Assert.Greater(view.LastRebuildFrame, f0);
            Assert.AreSame(view.Editor, sv.Sim.Editor);
            Object.Destroy(go);
            yield return null;
        }
    }
}
