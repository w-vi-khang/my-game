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
            Assert.AreEqual(60, sv.TargetFps);
            Assert.AreEqual(60, Application.targetFrameRate);
            Assert.AreEqual(0, QualitySettings.vSyncCount);
            Assert.IsTrue(view.Editor.IsOccupied(34, 18, 17));

            Object.Destroy(go);
            yield return null;
            Assert.AreEqual(before, Application.targetFrameRate);
            Assert.AreEqual(vs, QualitySettings.vSyncCount);
        }
    }
}
