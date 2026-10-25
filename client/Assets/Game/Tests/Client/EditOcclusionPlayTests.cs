using System.Collections;
using Game.Core;
using Game.Core.Sim;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Game.Client.Tests
{
    /// <summary>Brief 0021: nhà, kho, dấu mốc chặn tia đào/đặt; ruộng, sinh vật thì không.</summary>
    public class EditOcclusionPlayTests
    {
        GameObject camGo, go;
        TerrainChunkView view;
        SimView sv;
        TerrainEditInput input;

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
            sv.habitat = false;
            input = go.AddComponent<TerrainEditInput>();
            yield return null;
            yield return null;
            Physics.SyncTransforms();
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Object.Destroy(go);
            Object.Destroy(camGo);
            yield return null;
        }

        static Ray Down(float x, float z, float top) => new Ray(new Vector3(x, top, z), Vector3.down);

        [UnityTest]
        public IEnumerator HouseAndStorageBlockPick()
        {
            foreach (var h in sv.Sim.State.Houses)
                Assert.IsFalse(input.Pick(Down(h.Cell.X + h.SizeX * 0.5f, h.Cell.Z + h.SizeZ * 0.5f, 80f), out _, out _), "house " + h.Id);
            var s = sv.Sim.State.Storages[0];
            Assert.IsFalse(input.Pick(Down(s.Cell.X + 0.5f, s.Cell.Z + 0.5f, 80f), out _, out _), "storage");
            Assert.AreEqual(3, go.transform.Find("Sim").GetComponentsInChildren<Collider>().Length);
            Debug.Log("EDITOCCLUSIONTEST ok");
            yield return null;
        }

        [UnityTest]
        public IEnumerator PlotsAndCreaturesDoNotBlock()
        {
            foreach (var p in sv.Sim.State.Plots)
            {
                Assert.IsTrue(input.Pick(Down(p.Soil.X + 0.5f, p.Soil.Z + 0.5f, 80f), out var dig, out var place), "plot " + p.Id);
                Assert.AreEqual(new Vector3Int(p.Soil.X, p.Soil.Y, p.Soil.Z), dig);
                Assert.AreEqual(dig + Vector3Int.up, place);
            }
            foreach (var c in sv.Sim.State.Creatures)
            {
                Assert.IsTrue(input.Pick(Down(c.Cell.X + 0.5f, c.Cell.Z + 0.5f, 80f), out var d2, out _), "creature " + c.Id);
                Assert.AreEqual(c.Cell.X, d2.x);
                Assert.AreEqual(c.Cell.Z, d2.z);
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator LandmarksBlockPick()
        {
            foreach (var l in view.Landmarks)
                Assert.IsFalse(input.Pick(Down(l.X + 0.5f, l.Z + 0.5f, 80f), out _, out _), l.Name);
            yield return null;
        }

        [UnityTest]
        public IEnumerator OpenGroundStillPicks()
        {
            var plain = sv.Plain.Value;
            int x = plain.X0 + plain.Size / 2, z = plain.Z0 + plain.Size / 2;
            Assert.IsTrue(input.Pick(Down(x + 0.5f, z + 0.5f, 80f), out var dig, out _));
            Assert.AreEqual(new Vector3Int(x, plain.Ground, z), dig);
            Assert.IsFalse(view.IsTerrainCollider(null));
            yield return null;
        }
    }
}
