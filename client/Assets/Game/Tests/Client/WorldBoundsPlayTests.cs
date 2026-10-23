using System.Collections;
using Game.Core;
using Game.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Game.Client.Tests
{
    /// <summary>World-tech bước 7b3 (brief 0012): camera không ra khỏi world, sương biên theo khoảng cách tới mép.</summary>
    public class WorldBoundsPlayTests
    {
        GameObject go, camGo;
        TerrainChunkView view;
        Camera cam;
        DebugFlyCamera fly;
        UnderwaterView under;
        WorldEdgeFog edge;
        int S;
        bool fog0;
        FogMode mode0;
        float start0, end0;
        CameraClearFlags clear0;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            go = new GameObject("Terrain");
            view = go.AddComponent<TerrainChunkView>();
            yield return null; // Start → Build
            Assert.IsNotNull(view.World, "Build not run");
            camGo = new GameObject("Cam");
            cam = camGo.AddComponent<Camera>();
            fly = camGo.AddComponent<DebugFlyCamera>();
            fly.terrain = view;
            under = camGo.AddComponent<UnderwaterView>();
            under.terrain = view;
            S = view.Editor.SeaLevel;
            fog0 = RenderSettings.fog;
            mode0 = RenderSettings.fogMode;
            start0 = RenderSettings.fogStartDistance;
            end0 = RenderSettings.fogEndDistance;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogStartDistance = 64f;
            RenderSettings.fogEndDistance = 120f;
            edge = camGo.AddComponent<WorldEdgeFog>();
            edge.terrain = view;
            edge.underwater = under;
            yield return null; // Start lưu base
            clear0 = cam.clearFlags;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Object.Destroy(camGo);
            Object.Destroy(go);
            yield return null;
            RenderSettings.fog = fog0;
            RenderSettings.fogMode = mode0;
            RenderSettings.fogStartDistance = start0;
            RenderSettings.fogEndDistance = end0;
            Shader.SetGlobalFloat(WorldEdgeFog.BlendProperty, 0f);
        }

        [UnityTest]
        public IEnumerator FlyStopsAtWorldEdge()
        {
            camGo.transform.position = new Vector3(3f, 36f, 32f);
            for (int i = 0; i < 300; i++)
                fly.Fly(new Vector3(-0.5f, 0, 0));
            float x = camGo.transform.position.x;
            Assert.GreaterOrEqual(x, 0f);
            Assert.Less(x, 0.01f);
            fly.Fly(new Vector3(1, 0, 0));
            Assert.AreEqual(x + 1f, camGo.transform.position.x, 1e-3f);
            yield return null;
        }

        [UnityTest]
        public IEnumerator FlyStopsAtCeiling()
        {
            camGo.transform.position = new Vector3(32f, 40f, 32f);
            for (int i = 0; i < 300; i++)
                fly.Fly(new Vector3(0, 1, 0));
            float y = camGo.transform.position.y;
            Assert.LessOrEqual(y, 48f + 1e-4f);
            Assert.Greater(y, 47.99f);
            yield return null;
        }

        [UnityTest]
        public IEnumerator EdgeFogFollowsDistance()
        {
            camGo.transform.position = new Vector3(32f, 36f, 32f);
            yield return null;
            Assert.AreEqual(120f, RenderSettings.fogEndDistance, 1e-3f);
            Assert.AreEqual(64f, RenderSettings.fogStartDistance, 1e-3f);
            Assert.AreEqual(0f, Shader.GetGlobalFloat("_EdgeFogBlend"), 1e-4f);
            camGo.transform.position = new Vector3(0f, 36f, 32f);
            yield return null;
            Assert.AreEqual(1f, RenderSettings.fogEndDistance, 1e-3f);
            Assert.AreEqual(0f, RenderSettings.fogStartDistance, 1e-3f);
            Assert.AreEqual(1f, Shader.GetGlobalFloat("_EdgeFogBlend"), 1e-4f);
            Assert.AreEqual(1f, edge.Factor, 1e-4f);
            camGo.transform.position = new Vector3(1f, 36f, 32f);
            yield return null;
            Assert.AreEqual(10.954f, RenderSettings.fogEndDistance, 1e-2f);
            Assert.AreEqual(0.5f, Shader.GetGlobalFloat("_EdgeFogBlend"), 1e-4f);
            camGo.transform.position = new Vector3(32f, 36f, 32f);
            yield return null;
            Assert.AreEqual(120f, RenderSettings.fogEndDistance, 1e-3f);
            Assert.AreEqual(64f, RenderSettings.fogStartDistance, 1e-3f);
            Assert.AreEqual(0f, Shader.GetGlobalFloat("_EdgeFogBlend"), 1e-4f);
            camGo.transform.position = new Vector3(1f, 36f, 32f);
            yield return null;
            edge.enabled = false;
            Assert.AreEqual(120f, RenderSettings.fogEndDistance, 1e-3f);
            Assert.AreEqual(64f, RenderSettings.fogStartDistance, 1e-3f);
            Assert.AreEqual(0f, Shader.GetGlobalFloat("_EdgeFogBlend"), 1e-4f);
        }

        [UnityTest]
        public IEnumerator EdgeFogUnderwater()
        {
            var w = view.World;
            int fz = -1;
            for (int z = 16; z < w.Size.Z - 16; z++)
                if (w.GetMaterial(1, S, z) == BlockMaterial.Water && w.GetMaterial(1, S + 1, z) == BlockMaterial.Empty)
                {
                    fz = z;
                    break;
                }
            if (fz < 0)
                Assert.Fail("No water cell at the edge");
            camGo.transform.position = new Vector3(1.0f, S + 0.3f, fz + 0.5f);
            yield return null;
            Assert.IsTrue(under.IsUnderwater);
            Assert.AreEqual(6.325f, RenderSettings.fogEndDistance, 1e-2f);
            Assert.AreEqual(0f, RenderSettings.fogStartDistance, 1e-2f);
            var exp = Color.Lerp(under.backgroundColor, under.fogColor, 0.5f);
            Assert.AreEqual(exp.r, cam.backgroundColor.r, 1e-3f);
            Assert.AreEqual(exp.g, cam.backgroundColor.g, 1e-3f);
            Assert.AreEqual(exp.b, cam.backgroundColor.b, 1e-3f);
            camGo.transform.position = new Vector3(32f, 36f, 32f);
            yield return null;
            Assert.IsFalse(under.IsUnderwater);
            Assert.AreEqual(120f, RenderSettings.fogEndDistance, 1e-3f);
            Assert.AreEqual(64f, RenderSettings.fogStartDistance, 1e-3f);
            Assert.AreEqual(clear0, cam.clearFlags);
        }
    }
}
