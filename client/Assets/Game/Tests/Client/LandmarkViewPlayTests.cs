using System.Collections;
using System.Collections.Generic;
using Game.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Game.Client.Tests
{
    /// <summary>World-tech bước 7d (brief 0018): khối dấu mốc trong world v3.</summary>
    public class LandmarkViewPlayTests
    {
        GameObject camGo, go;
        TerrainChunkView view;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            camGo = new GameObject("Cam");
            camGo.tag = "MainCamera";
            camGo.AddComponent<Camera>();
            camGo.transform.position = new Vector3(192, 40, 60);
            go = new GameObject("Terrain");
            view = go.AddComponent<TerrainChunkView>();
            view.source = WorldSource.MapsV3;
            view.streaming = true;
            view.streamTarget = camGo.transform;
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
        public IEnumerator MarkersMatchLandmarks()
        {
            Assert.IsNotNull(view.Landmarks);
            Assert.AreEqual(9, view.Landmarks.Count);
            var expect = LandmarkPlacer.Place(view.World, view.Maps, ConfigLoader.LoadWorldMapParams());
            var root = go.transform.Find("Dấu mốc");
            Assert.IsNotNull(root);
            Assert.AreEqual(9, root.childCount);
            for (int i = 0; i < 9; i++)
            {
                var l = view.Landmarks[i];
                var e = expect[i];
                Assert.AreEqual(e.Kind, l.Kind);
                Assert.AreEqual(e.Map, l.Map);
                Assert.AreEqual(e.Type, l.Type);
                Assert.AreEqual(e.X, l.X);
                Assert.AreEqual(e.Z, l.Z);
                Assert.AreEqual(e.Surface, l.Surface);
                Assert.AreEqual(e.Name, l.Name);
                var t = root.GetChild(i);
                Assert.AreEqual(l.Name, t.name);
                Assert.AreEqual(new Vector3(l.X + 0.5f, l.Surface + 1, l.Z + 0.5f), t.localPosition);
                Assert.AreEqual(l.Kind == LandmarkKind.Checkpoint ? 2 : 1, t.childCount);
                var c = t.GetChild(0).GetComponent<MeshRenderer>().sharedMaterial.GetColor("_BaseColor");
                var want = (Color)LandmarkStyle.ColorOf(l);
                Assert.AreEqual(want.r, c.r, 1e-3f);
                Assert.AreEqual(want.g, c.g, 1e-3f);
                Assert.AreEqual(want.b, c.b, 1e-3f);
            }
            Assert.AreEqual(root.GetComponentsInChildren<MeshRenderer>(true).Length, root.GetComponentsInChildren<Collider>(true).Length); // mỗi khối có collider (brief 0021)
            var baseMat = Resources.Load<Material>("Materials/Landmark");
            Assert.IsNotNull(baseMat, "thiếu Resources/Materials/Landmark.mat");
            Assert.AreEqual("Universal Render Pipeline/Lit", baseMat.shader.name);
            foreach (var r in root.GetComponentsInChildren<MeshRenderer>())
                Assert.AreSame(baseMat.shader, r.sharedMaterial.shader);
            var mats = new HashSet<Material>();
            foreach (var r in root.GetComponentsInChildren<MeshRenderer>())
                mats.Add(r.sharedMaterial);
            Assert.LessOrEqual(mats.Count, 7);
            Debug.Log($"LANDMARKVIEWTEST {view.Landmarks.Count} dấu mốc, {mats.Count} material, đặt {view.LandmarkMs:F1} ms");
            yield return null;
        }

        [UnityTest]
        public IEnumerator RebuildReplacesMarkers()
        {
            view.Build();
            yield return null;
            int n = 0;
            Transform found = null;
            foreach (Transform c in go.transform)
                if (c.name == "Dấu mốc")
                {
                    n++;
                    found = c;
                }
            Assert.AreEqual(1, n);
            Assert.AreEqual(9, found.childCount);
        }

        [UnityTest]
        public IEnumerator IslandWorldHasNoLandmarks()
        {
            var g = new GameObject("Island");
            var v = g.AddComponent<TerrainChunkView>();
            v.source = WorldSource.IslandV2;
            v.streaming = false;
            yield return null;
            Assert.IsNull(v.Landmarks);
            Assert.IsNull(g.transform.Find("Dấu mốc"));
            Object.Destroy(g);
        }
    }
}
