using System.Collections;
using Game.Core;
using Game.Core.Sim;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Game.Client.Tests
{
    /// <summary>Task m1-demo-dong-bang: component DemoPlain (scene TerrainView) — san đồng bằng, bỏ dấu mốc, đặt camera, đồng bộ góc DebugFlyCamera.</summary>
    public class DemoPlainPlayTests
    {
        GameObject camGo, go;
        TerrainChunkView view;
        DemoPlain dp;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            camGo = new GameObject("Cam");
            camGo.tag = "MainCamera";
            camGo.AddComponent<Camera>();
            camGo.AddComponent<DebugFlyCamera>();
            camGo.transform.SetPositionAndRotation(new Vector3(192, 40, 150), Quaternion.Euler(25f, 0f, 0f));
            go = new GameObject("Terrain");
            view = go.AddComponent<TerrainChunkView>();
            view.source = WorldSource.MapsV3;
            view.streaming = true;
            view.streamTarget = camGo.transform;
            dp = go.AddComponent<DemoPlain>();
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
        public IEnumerator FlattensPlainAndRemovesLandmarks()
        {
            Assert.IsTrue(dp.Plain.HasValue);
            var plain = dp.Plain.Value;
            for (int z = plain.Z0; z < plain.Z0 + plain.Size; z++)
                for (int x = plain.X0; x < plain.X0 + plain.Size; x++)
                {
                    MinimapImage.ColumnInfo(view.World, x, z, out int surf, out int depth);
                    Assert.AreEqual(plain.Ground, surf, $"mặt ({x},{z})");
                    Assert.AreEqual(0, depth, $"nước ({x},{z})");
                    Assert.AreEqual(BlockMaterial.Grass, view.World.GetMaterial(x, surf, z), $"vật liệu ({x},{z})");
                }
            foreach (var l in view.Landmarks)
                Assert.IsFalse(plain.Contains(l.X, l.Z), "dấu mốc " + l.Name + " trong đồng bằng");
            yield return null;
        }

        [UnityTest]
        public IEnumerator PlacesCameraAndSyncsFly()
        {
            var plain = dp.Plain.Value;
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
            yield return null;
        }
    }
}
