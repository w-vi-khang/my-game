using System.Collections;
using Game.Core;
using Game.Core.Sim;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Game.Client.Tests
{
    /// <summary>Nước chảy 5e (brief 0075) trong scene SimView trên world nhiều map.</summary>
    public class WaterFlowPlayTests
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
            camGo.transform.SetPositionAndRotation(new Vector3(192, 40, 150), Quaternion.Euler(25f, 0f, 0f));
            go = new GameObject("Terrain");
            view = go.AddComponent<TerrainChunkView>();
            view.source = WorldSource.MapsV3;
            view.streaming = true;
            view.streamTarget = camGo.transform;
            sv = go.AddComponent<SimView>();
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
        public IEnumerator DiggingSandNextToPondFillsHole()
        {
            var plain = sv.Plain.Value;
            int wx0 = plain.X0 + plain.Size / 2 + SimDemo.PlainPondMin.X;
            int wz0 = plain.Z0 + plain.Size / 2 + SimDemo.PlainPondMin.Z;
            int x = wx0 - 1, y = plain.Ground, z = wz0 + 1;
            Assert.AreEqual(EditResult.Ok, view.Editor.TryDig(x, y, z));
            float t = 0f;
            while (t < 2f && view.World.GetMaterial(x, y, z) != BlockMaterial.Water)
            {
                t += Time.deltaTime;
                yield return null;
            }
            Assert.AreEqual(BlockMaterial.Water, view.World.GetMaterial(x, y, z));
            Assert.AreEqual(7, view.Editor.FlowMaxDistance);
            Assert.AreEqual(8f, view.waterStepsPerSecond);
        }

        [UnityTest]
        public IEnumerator NoFloodOnPlainWithoutEdits()
        {
            for (int i = 0; i < 30; i++)
                yield return null;
            Assert.IsFalse(view.Editor.HasPendingWater);
        }
    }
}
