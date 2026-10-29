using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Game.Client.Tests
{
    /// <summary>Brief 0082: nước trong suốt chỉ hiện mặt gần camera nhất (depth prepass), không chồng mặt sau/mặt trong.</summary>
    public class WaterLayerPlayTests
    {
        const int Size = 128;

        GameObject lightGo, camGo, cube;
        readonly List<Material> mats = new List<Material>();
        readonly List<Light> disabledLights = new List<Light>();
        bool oldFog;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            oldFog = RenderSettings.fog;
            RenderSettings.fog = false;
            Shader.SetGlobalFloat("_DayCycleOn", 0f);

            disabledLights.Clear();
            foreach (var l in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
                if (l.enabled) { l.enabled = false; disabledLights.Add(l); }

            lightGo = new GameObject("Light");
            lightGo.AddComponent<Light>().type = LightType.Directional;
            lightGo.transform.rotation = Quaternion.Euler(50, 30, 0);

            camGo = new GameObject("Cam");
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
            cam.fieldOfView = 60;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 50f;
            camGo.transform.position = new Vector3(0, 3.5f, -4);
            camGo.transform.LookAt(new Vector3(0, 1.5f, 0));

            cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.transform.position = new Vector3(0, 1.5f, 0);
            cube.transform.localScale = new Vector3(1, 3, 1);
            cube.transform.rotation = Quaternion.Euler(0, 30, 0);
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            foreach (var l in disabledLights) if (l != null) l.enabled = true;
            Object.Destroy(camGo);
            Object.Destroy(lightGo);
            Object.Destroy(cube);
            foreach (var m in mats) Object.Destroy(m);
            mats.Clear();
            RenderSettings.fog = oldFog;
            yield return null;
        }

        Material MakeWater(int cull, bool prepass)
        {
            var m = new Material(Shader.Find("Game/Ocean"));
            m.SetFloat("_Alpha", 0.45f);
            m.SetFloat("_SrcBlend", 5f);
            m.SetFloat("_DstBlend", 10f);
            m.SetFloat("_ZWrite", 0f);
            m.SetFloat("_Cull", cull);
            m.SetFloat("_RippleStrength", 0f);
            m.SetFloat("_Glint", 0f);
            m.renderQueue = 3000;
            m.SetShaderPassEnabled("SRPDefaultUnlit", prepass);
            mats.Add(m);
            return m;
        }

        static Color[] RenderLinear(Camera cam)
        {
            var rt = new RenderTexture(Size, Size, 24, RenderTextureFormat.ARGBFloat);
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(Size, Size, TextureFormat.RGBAFloat, false);
            tex.ReadPixels(new Rect(0, 0, Size, Size), 0, 0);
            tex.Apply();
            var px = tex.GetPixels();
            cam.targetTexture = null;
            RenderTexture.active = null;
            Object.Destroy(rt);
            Object.Destroy(tex);
            return px;
        }

        static float Diff(Color[] a, Color[] b)
        {
            float best = 0f;
            for (int i = 0; i < a.Length; i++)
                best = Mathf.Max(best, Mathf.Abs(a[i].r - b[i].r), Mathf.Abs(a[i].g - b[i].g), Mathf.Abs(a[i].b - b[i].b));
            return best;
        }

        [UnityTest]
        public IEnumerator TransparentWaterShowsOnlyNearestLayer()
        {
            var cam = camGo.GetComponent<Camera>();
            var rend = cube.GetComponent<MeshRenderer>();

            rend.sharedMaterial = MakeWater(0, true);
            var a = RenderLinear(cam);
            rend.sharedMaterial = MakeWater(2, false);
            var b = RenderLinear(cam);
            rend.sharedMaterial = MakeWater(0, false);
            var c = RenderLinear(cam);

            int covered = 0;
            foreach (var p in b) if (p.r + p.g + p.b > 0.01f) covered++;
            float diffCB = Diff(c, b), diffAB = Diff(a, b);
            Debug.Log($"WaterLayer covered={covered} diffCB={diffCB:F5} diffAB={diffAB:F5}");
            string msg = $"covered={covered} diffCB={diffCB:F5} diffAB={diffAB:F5}";
            Assert.Greater(covered, 500, msg);
            Assert.Greater(diffCB, 0.02f, msg);
            Assert.LessOrEqual(diffAB, 0.005f, msg);
            yield return null;
        }
    }
}
