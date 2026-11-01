using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Game.Client.Tests
{
    /// <summary>Brief 0097: quầng mặt trời và chấm lấp lánh của shader Game/Ocean (camera nhìn thẳng xuống mặt phẳng, mặt trời đỉnh đầu).</summary>
    public class WaterGlintPlayTests
    {
        const int Size = 256;

        GameObject lightGo, camGo, plane;
        Material mat;
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
            lightGo.transform.rotation = Quaternion.Euler(90, 0, 0);

            camGo = new GameObject("Cam");
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
            cam.fieldOfView = 60;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 50f;
            camGo.transform.position = new Vector3(0, 10, 0);
            camGo.transform.rotation = Quaternion.Euler(90, 0, 0);

            plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
            plane.transform.position = Vector3.zero;
            plane.transform.localScale = new Vector3(3, 1, 3);

            mat = new Material(Shader.Find("Game/Ocean"));
            mat.SetFloat("_RippleStrength", 0f);
            mat.SetFloat("_Glint", 0f);
            mat.SetFloat("_GlintHalo", 0f);
            mat.SetFloat("_SparkleStrength", 0f);
            mat.SetFloat("_SparkleSpeed", 0f);
            plane.GetComponent<MeshRenderer>().sharedMaterial = mat;
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            foreach (var l in disabledLights) if (l != null) l.enabled = true;
            Object.Destroy(camGo);
            Object.Destroy(lightGo);
            Object.Destroy(plane);
            Object.Destroy(mat);
            RenderSettings.fog = oldFog;
            yield return null;
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

        static int CountBrighter(Color[] a, Color[] b, float th)
        {
            int n = 0;
            for (int i = 0; i < a.Length; i++)
                if (Mathf.Max(b[i].r - a[i].r, b[i].g - a[i].g, b[i].b - a[i].b) > th) n++;
            return n;
        }

        void SetSparkle(float fadeStart, float fadeEnd)
        {
            mat.SetFloat("_SparkleStrength", 2f);
            mat.SetFloat("_SparkleScale", 1.5f);
            mat.SetFloat("_SparkleDensity", 1f);
            mat.SetFloat("_SparkleSize", 0.3f);
            mat.SetFloat("_SparkleFadeStart", fadeStart);
            mat.SetFloat("_SparkleFadeEnd", fadeEnd);
        }

        [UnityTest]
        public IEnumerator SunHaloBrightensReflectionOnly()
        {
            var cam = camGo.GetComponent<Camera>();
            var a = RenderLinear(cam);
            mat.SetFloat("_GlintHalo", 1f);
            mat.SetFloat("_GlintHaloPower", 300f);
            var b = RenderLinear(cam);
            int mid = (Size / 2) * Size + Size / 2, corner = 5 * Size + 5;
            float dMid = b[mid].r - a[mid].r, dCorner = Mathf.Abs(b[corner].r - a[corner].r);
            Debug.Log($"[0097] halo: mid d={dMid:F4}, corner d={dCorner:F4}");
            Assert.Greater(dMid, 0.5f, $"mid d={dMid}");
            Assert.Less(dCorner, 0.02f, $"corner d={dCorner}");
            yield return null;
        }

        [UnityTest]
        public IEnumerator SparkleAddsSmallDots()
        {
            var cam = camGo.GetComponent<Camera>();
            var a = RenderLinear(cam);
            SetSparkle(20f, 60f);
            var b = RenderLinear(cam);
            int n = CountBrighter(a, b, 0.2f);
            Debug.Log($"[0097] sparkle: n={n}");
            Assert.Greater(n, 200, $"n={n}");
            Assert.Less(n, Size * Size / 4, $"n={n}");
            yield return null;
        }

        [UnityTest]
        public IEnumerator SparkleFadesWithDistance()
        {
            var cam = camGo.GetComponent<Camera>();
            var a = RenderLinear(cam);
            SetSparkle(1f, 2f);
            var b = RenderLinear(cam);
            int n = CountBrighter(a, b, 0.02f);
            Debug.Log($"[0097] sparkle far: n={n}");
            Assert.AreEqual(0, n, $"n={n}");
            yield return null;
        }
    }
}
