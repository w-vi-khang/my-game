using System.Collections;
using Game.Core;
using Game.Core.Sim;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Game.Client.Tests
{
    /// <summary>World 7e (brief 0020): SimView trên world 3 map — trại trong Map 1, vùng 64 × 64, camera ở trại.</summary>
    public class SimViewMapsPlayTests
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
        public IEnumerator SetsUpCampInMap1()
        {
            Assert.IsNotNull(sv.Sim);
            Assert.IsTrue(sv.Site.HasValue);
            var site = sv.Site.Value;
            var expect = SimDemo.FindSite(view.World, view.Maps, view.Landmarks, 0, ConfigLoader.LoadWorldMapParams().waterLevel, ConfigLoader.LoadSimConfig().regionSize);
            Assert.AreEqual(expect.Dx, site.Dx);
            Assert.AreEqual(expect.Dz, site.Dz);
            Assert.AreEqual(expect.Bounds, site.Bounds);
            Assert.AreEqual(site.Bounds, sv.Sim.Bounds);
            Assert.AreEqual(64 * 64 * view.World.Size.Y, sv.Sim.PathfinderCellCount);
            Assert.AreEqual(3, sv.Sim.State.Creatures.Count);
            Assert.AreEqual(3, sv.BodyCount);
            Assert.AreEqual(4, sv.SproutCount);
            Assert.AreEqual(site.Shift(SimDemo.HouseA), sv.Sim.State.Houses[0].Cell);
            var k = site.Shift(SimDemo.StorageCell);
            Assert.IsTrue(view.Editor.IsOccupied(k.X, k.Y, k.Z));
            yield return null;
        }

        [UnityTest]
        public IEnumerator CameraLooksAtCamp()
        {
            var site = sv.Site.Value;
            var want = SimView.CameraPosFor(site);
            var p = camGo.transform.position;
            Assert.AreEqual(want.x, p.x, 1e-4f);
            Assert.AreEqual(want.y, p.y, 1e-4f);
            Assert.AreEqual(want.z, p.z, 1e-4f);
            Assert.AreEqual(25f, camGo.transform.rotation.eulerAngles.x, 1e-3f);
            var vp = camGo.GetComponent<Camera>().WorldToViewportPoint(SimView.CampCenter(site));
            Assert.Greater(vp.z, 0f);
            Assert.Greater(vp.x, 0f);
            Assert.Less(vp.x, 1f);
            Assert.Greater(vp.y, 0f);
            Assert.Less(vp.y, 1f);
            Assert.AreEqual(BlockMaterial.Empty, view.World.GetMaterial(Mathf.FloorToInt(p.x), Mathf.FloorToInt(p.y), Mathf.FloorToInt(p.z)));
            int frames = 0;
            while (frames < 600 && view.MissingColumnsWithin(48) != 0)
            {
                yield return null;
                frames++;
            }
            Assert.AreEqual(0, view.MissingColumnsWithin(48));
            Debug.Log($"SIMVIEWTEST nạp quanh trại sau {frames} khung");
        }

        [UnityTest]
        public IEnumerator SimRunsAndBoxesUseLandmarkMaterial()
        {
            for (int i = 0; i < 120; i++)
                yield return null;
            Assert.Greater(sv.Sim.State.Tick, 0);
            var baseMat = Resources.Load<Material>("Materials/Landmark");
            Assert.IsNotNull(baseMat);
            var root = go.transform.Find("Sim");
            Assert.IsNotNull(root);
            foreach (var r in root.GetComponentsInChildren<MeshRenderer>())
                Assert.AreSame(baseMat.shader, r.sharedMaterial.shader, r.name);
        }
    }
}
