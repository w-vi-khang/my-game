using System.Collections;
using Game.Core;
using Game.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Game.Client.Tests
{
    /// <summary>Brief 0023: dưới nước theo buổi — tint theo pha, ngoi lên thì sương là màu hiện tại.</summary>
    public class DayCycleWaterPlayTests
    {
        const int H = 3_600_000;
        GameObject go, camGo, lightGo;
        TerrainChunkView view;
        Camera cam;
        DebugFlyCamera fly;
        UnderwaterView under;
        DayCycleView day;
        long now;
        int S;

        static void Near(Color e, Color a, float tol = 1e-4f)
        {
            Assert.AreEqual(e.r, a.r, tol, "r");
            Assert.AreEqual(e.g, a.g, tol, "g");
            Assert.AreEqual(e.b, a.b, tol, "b");
        }

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            now = 0;
            go = new GameObject("Terrain");
            view = go.AddComponent<TerrainChunkView>();
            yield return null;
            Assert.IsNotNull(view.World, "Build chưa chạy");
            lightGo = new GameObject("Light");
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            camGo = new GameObject("Cam");
            cam = camGo.AddComponent<Camera>();
            fly = camGo.AddComponent<DebugFlyCamera>();
            fly.terrain = view;
            under = camGo.AddComponent<UnderwaterView>();
            under.terrain = view;
            day = camGo.AddComponent<DayCycleView>();
            day.sun = light;
            day.underwater = under;
            day.debugKeys = false;
            day.clockMs = () => now;
            day.secondsNow = () => 0.0;
            S = view.Editor.SeaLevel;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Object.Destroy(camGo);
            Object.Destroy(lightGo);
            Object.Destroy(go);
            yield return null;
        }

        [UnityTest]
        public IEnumerator NightTintsUnderwaterAndSurfacingRestoresCurrentFog()
        {
            var w = view.World;
            int fx = -1, fz = -1;
            for (int z = 0; z < w.Size.Z && fx < 0; z++)
            for (int x = 0; x < w.Size.X; x++)
            {
                if (w.GetMaterial(x, S, z) != BlockMaterial.Water || w.SurfaceHeight(x, z) > S - 2)
                    continue;
                bool clear = true;
                for (int y = S + 1; y <= S + 4; y++)
                    if (w.GetMaterial(x, y, z) != BlockMaterial.Empty)
                        clear = false;
                if (clear)
                {
                    fx = x; fz = z;
                    break;
                }
            }
            if (fx < 0)
                Assert.Fail("Không có cột nước phù hợp");
            camGo.transform.position = new Vector3(fx + 0.5f, S + 3.5f, fz + 0.5f);
            yield return null;
            Assert.IsFalse(under.IsUnderwater);
            Near(DaySky.Evaluate(0f).horizon, RenderSettings.fogColor);

            fly.Fly(new Vector3(0, -3.2f, 0));
            yield return null;
            Assert.IsTrue(under.IsUnderwater);
            Near(under.fogColor * DaySky.Evaluate(0f).waterTint, RenderSettings.fogColor);
            Near(under.backgroundColor * DaySky.Evaluate(0f).waterTint, cam.backgroundColor);

            now = 12 * H;
            yield return null;
            Near(under.fogColor, RenderSettings.fogColor);

            fly.Fly(new Vector3(0, 3.2f, 0));
            yield return null;
            Assert.IsFalse(under.IsUnderwater);
            Near(DaySky.Evaluate(0.5f).horizon, RenderSettings.fogColor);
        }
    }
}
