using System.Collections;
using Game.Core;
using Game.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Game.Client.Tests
{
    /// <summary>World-tech bước 7b2 (brief 0011): camera bay không vào ô đặc, vào nước thì bật hiệu ứng dưới nước và trả lại khi lên.</summary>
    public class FlyCameraWaterPlayTests
    {
        GameObject go, camGo;
        TerrainChunkView view;
        Camera cam;
        DebugFlyCamera fly;
        UnderwaterView under;
        int S;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            go = new GameObject("Terrain");
            view = go.AddComponent<TerrainChunkView>();
            yield return null; // Start → Build
            Assert.IsNotNull(view.World, "Build chưa chạy");
            camGo = new GameObject("Cam");
            cam = camGo.AddComponent<Camera>();
            fly = camGo.AddComponent<DebugFlyCamera>();
            fly.terrain = view;
            under = camGo.AddComponent<UnderwaterView>();
            under.terrain = view;
            S = view.Editor.SeaLevel;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Object.Destroy(camGo);
            Object.Destroy(go);
            yield return null;
        }

        [UnityTest]
        public IEnumerator FlyStopsAboveGround()
        {
            var w = view.World;
            int fx = -1, fz = -1, h = 0;
            for (int z = 0; z < w.Size.Z && fx < 0; z++)
            for (int x = 0; x < w.Size.X; x++)
            {
                int sh = w.SurfaceHeight(x, z);
                if (sh > S && sh + 4 < w.Size.Y)
                {
                    fx = x; fz = z; h = sh;
                    break;
                }
            }
            if (fx < 0)
                Assert.Fail("Không có cột đất phù hợp");
            camGo.transform.position = new Vector3(fx + 0.5f, h + 3.5f, fz + 0.5f);
            fly.Fly(new Vector3(0, -10, 0));
            Assert.AreEqual(h + 1 + 0.25f + FlyCameraCollision.Skin, camGo.transform.position.y, 1e-3f);
            yield return null;
        }

        [UnityTest]
        public IEnumerator FlyIntoWaterSwitchesUnderwaterView()
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
            var fogColor0 = RenderSettings.fogColor;
            float start0 = RenderSettings.fogStartDistance, end0 = RenderSettings.fogEndDistance;
            var clear0 = cam.clearFlags;
            var bg0 = cam.backgroundColor;
            camGo.transform.position = new Vector3(fx + 0.5f, S + 3.5f, fz + 0.5f);
            yield return null;
            Assert.IsFalse(under.IsUnderwater);
            fly.Fly(new Vector3(0, -3.2f, 0));
            Assert.AreEqual(S + 0.3f, camGo.transform.position.y, 1e-3f);
            yield return null;
            Assert.IsTrue(under.IsUnderwater);
            Assert.AreEqual(40f, RenderSettings.fogEndDistance, 1e-4f);
            Assert.AreEqual(under.fogColor, RenderSettings.fogColor);
            Assert.AreEqual(CameraClearFlags.SolidColor, cam.clearFlags);
            Assert.AreEqual(under.backgroundColor, cam.backgroundColor);
            fly.Fly(new Vector3(0, 3.2f, 0));
            yield return null;
            Assert.IsFalse(under.IsUnderwater);
            Assert.AreEqual(fogColor0, RenderSettings.fogColor);
            Assert.AreEqual(start0, RenderSettings.fogStartDistance, 1e-4f);
            Assert.AreEqual(end0, RenderSettings.fogEndDistance, 1e-4f);
            Assert.AreEqual(clear0, cam.clearFlags);
            Assert.AreEqual(bg0, cam.backgroundColor);
        }
    }
}
