using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Game.Client.Tests
{
    /// <summary>Task m1-sua-mau-chan-troi: vật bị sương phủ kín và trời ngay dưới chân trời cùng màu (sương và skybox Game/SkyClouds nhận màu chân trời như nhau).</summary>
    public class HorizonFogMatchPlayTests
    {
        const int H = 3_600_000;
        const int Size = 256;
        GameObject lightGo, camGo, cube;
        Material skyMat, cubeMat;
        DayCycleView view;
        long now = 12 * H;

        Material oldSkybox;
        bool oldFog;
        FogMode oldMode;
        float oldStart, oldEnd;
        Color oldColor;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            now = 12 * H;
            oldSkybox = RenderSettings.skybox;
            oldFog = RenderSettings.fog;
            oldMode = RenderSettings.fogMode;
            oldStart = RenderSettings.fogStartDistance;
            oldEnd = RenderSettings.fogEndDistance;
            oldColor = RenderSettings.fogColor;

            skyMat = new Material(Shader.Find("Game/SkyClouds"));
            RenderSettings.skybox = skyMat;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogStartDistance = 1f;
            RenderSettings.fogEndDistance = 2f;
            Shader.SetGlobalFloat("_EdgeFogBlend", 0f);

            lightGo = new GameObject("Light");
            lightGo.AddComponent<Light>().type = LightType.Directional;

            camGo = new GameObject("Cam");
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.Skybox;
            cam.fieldOfView = 60;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 128f;
            camGo.transform.position = Vector3.zero;
            camGo.transform.rotation = Quaternion.identity;
            view = camGo.AddComponent<DayCycleView>();
            view.sun = lightGo.GetComponent<Light>();
            view.clockMs = () => now;
            view.secondsNow = () => 100.0;

            cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.transform.position = new Vector3(0, -30, 100);
            cube.transform.localScale = new Vector3(400, 40, 2);
            cubeMat = new Material(Shader.Find("Game/Terrain"));
            cube.GetComponent<MeshRenderer>().sharedMaterial = cubeMat;
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Object.Destroy(camGo);
            Object.Destroy(lightGo);
            Object.Destroy(cube);
            Object.Destroy(skyMat);
            Object.Destroy(cubeMat);
            RenderSettings.skybox = oldSkybox;
            RenderSettings.fog = oldFog;
            RenderSettings.fogMode = oldMode;
            RenderSettings.fogStartDistance = oldStart;
            RenderSettings.fogEndDistance = oldEnd;
            RenderSettings.fogColor = oldColor;
            yield return null;
        }

        static Color32[] RenderPixels(Camera cam)
        {
            var rt = new RenderTexture(Size, Size, 24);
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
            tex.ReadPixels(new Rect(0, 0, Size, Size), 0, 0);
            tex.Apply();
            var px = tex.GetPixels32();
            cam.targetTexture = null;
            RenderTexture.active = null;
            Object.Destroy(rt);
            Object.Destroy(tex);
            return px;
        }

        static void Close(Color32 e, Color32 a, int tol, string what)
        {
            Assert.LessOrEqual(Mathf.Abs(e.r - a.r), tol, $"{what} R: {e} vs {a}");
            Assert.LessOrEqual(Mathf.Abs(e.g - a.g), tol, $"{what} G: {e} vs {a}");
            Assert.LessOrEqual(Mathf.Abs(e.b - a.b), tol, $"{what} B: {e} vs {a}");
        }

        void CheckMatch()
        {
            view.Apply();
            var px = RenderPixels(camGo.GetComponent<Camera>());
            Color32 sky = px[120 * Size + 128];
            Color32 block = px[64 * Size + 128];
            Close(sky, block, 2, "trời vs khối");
            Close(block, (Color32)RenderSettings.fogColor, 2, "khối vs fogColor");
        }

        [UnityTest]
        public IEnumerator SkyBelowHorizonMatchesFoggedGeometry_Noon()
        {
            now = 12 * H;
            CheckMatch();
            yield return null;
        }

        [UnityTest]
        public IEnumerator SkyBelowHorizonMatchesFoggedGeometry_Midnight()
        {
            now = 0;
            CheckMatch();
            yield return null;
        }

        [UnityTest]
        public IEnumerator CubeIsRenderedWhenFogOff()
        {
            now = 12 * H;
            view.Apply();
            RenderSettings.fog = false;
            var px = RenderPixels(camGo.GetComponent<Camera>());
            Color32 block = px[64 * Size + 128];
            Color32 fog = (Color32)RenderSettings.fogColor;
            int d = Mathf.Max(Mathf.Abs(block.r - fog.r), Mathf.Abs(block.g - fog.g), Mathf.Abs(block.b - fog.b));
            Assert.GreaterOrEqual(d, 13, $"khối {block} vs fog {fog}");
            yield return null;
        }
    }
}
