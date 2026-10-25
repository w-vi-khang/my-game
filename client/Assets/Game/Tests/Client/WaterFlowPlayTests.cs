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

        IEnumerator WaitSettled()
        {
            float t = 0f;
            while (t < 5f && view.Editor.HasPendingWater)
            {
                t += Time.deltaTime;
                yield return null;
            }
            Assert.IsFalse(view.Editor.HasPendingWater);
        }

        [UnityTest]
        public IEnumerator WaterSettlesAfterStart()
        {
            yield return WaitSettled();
        }

        [UnityTest]
        public IEnumerator WaterfallPoursIntoPond()
        {
            yield return WaitSettled();
            Assert.IsTrue(sv.WaterfallSource.HasValue);
            var plain = sv.Plain.Value;
            int cx = plain.X0 + plain.Size / 2, cz = plain.Z0 + plain.Size / 2, g = plain.Ground;
            var w = view.World;
            for (int y = g + 1; y <= g + 4; y++)
                Assert.AreEqual(BlockMaterial.Water, w.GetMaterial(cx + 13, y, cz - 11), $"fall y={y}");
            Assert.AreEqual(BlockMaterial.Empty, w.GetMaterial(cx + 12, g + 1, cz - 11));
            Assert.AreEqual(BlockMaterial.Empty, w.GetMaterial(cx + 14, g + 1, cz - 11));
            Assert.AreEqual(BlockMaterial.Empty, w.GetMaterial(cx + 13, g + 1, cz - 12));
            Assert.IsTrue(Cell.IsWaterSource(w.Get(cx + 10, g, cz - 12)));
        }

        [UnityTest]
        public IEnumerator WaterfallColumnIsMeshed()
        {
            yield return WaitSettled();
            yield return null;
            yield return null;
            view.TotalTriangles(out int water);
            Assert.Greater(water, 0);
            var plain = sv.Plain.Value;
            int wx = plain.X0 + plain.Size / 2 + 13, wy = plain.Ground + 2, wz = plain.Z0 + plain.Size / 2 - 11;
            var m = Game.World.BlockMesher.Build(view.World, wx >> 4, wy >> 4, wz >> 4);
            int lx = wx & 15, lz = wz & 15;
            bool found = false;
            for (int t = 0; t < m.WaterIndices.Length && !found; t += 3)
            {
                var n = m.Normals[m.WaterIndices[t]];
                if (Mathf.Abs(n.x) != 1f || n.y != 0f || n.z != 0f)
                    continue;
                bool inside = true;
                for (int k = 0; k < 3; k++)
                {
                    var p = m.Vertices[m.WaterIndices[t + k]];
                    if (p.x < lx - 1e-4f || p.x > lx + 1 + 1e-4f || p.z < lz - 1e-4f || p.z > lz + 1 + 1e-4f)
                        inside = false;
                }
                found = inside;
            }
            Assert.IsTrue(found, "side face of waterfall column");
        }
    }
}
