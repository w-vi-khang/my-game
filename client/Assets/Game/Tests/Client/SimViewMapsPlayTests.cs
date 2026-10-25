using System.Collections;
using Game.Core;
using Game.Core.Sim;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Game.Client.Tests
{
    /// <summary>Task m1-demo-dong-bang: SimView trên world 3 map — trại giữa đồng bằng san phẳng, vùng 64 × 64, camera nhìn vào trại.</summary>
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
        public IEnumerator SetsUpCampOnPlain()
        {
            Assert.IsNotNull(sv.Sim);
            Assert.IsTrue(sv.Plain.HasValue);
            var plain = sv.Plain.Value;
            Assert.AreEqual(plain.Bounds, sv.Sim.Bounds);
            Assert.AreEqual(64 * 64 * view.World.Size.Y, sv.Sim.PathfinderCellCount);
            Assert.AreEqual(1, sv.Sim.State.Creatures.Count);
            Assert.AreEqual(1, sv.BodyCount);
            Assert.AreEqual(3, sv.Sim.State.Houses.Count);
            Assert.AreEqual(4, sv.SproutCount);
            Assert.AreEqual(SimDemo.OnPlain(plain, SimDemo.PlainHouseA), sv.Sim.State.Houses[0].Cell);
            var k = SimDemo.OnPlain(plain, SimDemo.PlainStorage);
            Assert.IsTrue(view.Editor.IsOccupied(k.X, k.Y, k.Z));
            int pondX0 = plain.X0 + plain.Size / 2 + SimDemo.PlainPondMin.X, pondZ0 = plain.Z0 + plain.Size / 2 + SimDemo.PlainPondMin.Z;
            for (int z = plain.Z0; z < plain.Z0 + plain.Size; z++)
                for (int x = plain.X0; x < plain.X0 + plain.Size; x++)
                {
                    if (sv.habitat && x >= pondX0 - 1 && x <= pondX0 + SimDemo.PondSize && z >= pondZ0 - 1 && z <= pondZ0 + SimDemo.PondSize) continue; // ao + viền cát
                    if (sv.habitat && x >= plain.X0 + plain.Size / 2 + 14 && x <= plain.X0 + plain.Size / 2 + 18
                        && z >= plain.Z0 + plain.Size / 2 - 13 && z <= plain.Z0 + plain.Size / 2 - 9) continue; // thác nước demo (0041)
                    MinimapImage.ColumnInfo(view.World, x, z, out int surf, out int depth);
                    Assert.AreEqual(plain.Ground, surf, $"surface ({x},{z})");
                    Assert.AreEqual(0, depth, $"water ({x},{z})");
                    Assert.AreEqual(BlockMaterial.Grass, view.World.GetMaterial(x, surf, z), $"material ({x},{z})");
                }
            foreach (var l in view.Landmarks)
                Assert.IsFalse(plain.Contains(l.X, l.Z), "landmark " + l.Name + " inside the plain");
            yield return null;
        }

        [UnityTest]
        public IEnumerator CameraLooksAtCamp()
        {
            var plain = sv.Plain.Value;
            var want = DemoPlain.CameraPos(plain);
            var p = camGo.transform.position;
            Assert.AreEqual(want.x, p.x, 1e-4f);
            Assert.AreEqual(want.y, p.y, 1e-4f);
            Assert.AreEqual(want.z, p.z, 1e-4f);
            Assert.AreEqual(DemoPlain.Pitch, camGo.transform.rotation.eulerAngles.x, 1e-3f);
            var vp = camGo.GetComponent<Camera>().WorldToViewportPoint(DemoPlain.CampCenter(plain));
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
            Debug.Log($"SIMVIEWTEST streamed around camp after {frames} frames");
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
