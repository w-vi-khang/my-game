using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Game.Client.Tests
{
    /// <summary>Brief 0099: vệt chảy của shader Game/Ocean đọc hướng chảy / cờ thác từ TEXCOORD0 của mesh (camera nhìn thẳng xuống, mặt trời đỉnh đầu).</summary>
    public class WaterStreakPlayTests
    {
        const int Size = 256;

        GameObject lightGo, camGo, quad;
        Mesh mesh;
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

            mesh = new Mesh();
            mesh.SetVertices(new List<Vector3> { new Vector3(-6, 0, -6), new Vector3(6, 0, -6), new Vector3(6, 0, 6), new Vector3(-6, 0, 6) });
            mesh.SetNormals(new List<Vector3> { Vector3.up, Vector3.up, Vector3.up, Vector3.up });
            var white = new Color32(255, 255, 255, 255);
            mesh.SetColors(new List<Color32> { white, white, white, white });
            mesh.SetTriangles(new List<int> { 0, 2, 1, 0, 3, 2 }, 0);
            quad = new GameObject("Quad");
            quad.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = quad.AddComponent<MeshRenderer>();

            mat = new Material(Shader.Find("Game/Ocean"));
            mat.SetFloat("_VertexDepth", 1f);
            mat.SetFloat("_RippleStrength", 0f);
            mat.SetFloat("_Glint", 0f);
            mat.SetFloat("_GlintHalo", 0f);
            mat.SetFloat("_SparkleStrength", 0f);
            mat.SetFloat("_FlowStreakStrength", 0f);
            mr.sharedMaterial = mat;
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            foreach (var l in disabledLights) if (l != null) l.enabled = true;
            Object.Destroy(camGo);
            Object.Destroy(lightGo);
            Object.Destroy(quad);
            Object.Destroy(mesh);
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

        void SetFlow(Vector4 f) => mesh.SetUVs(0, new List<Vector4> { f, f, f, f });

        int Measure(Vector4 flow, float th)
        {
            var cam = camGo.GetComponent<Camera>();
            SetFlow(flow);
            mat.SetFloat("_FlowStreakStrength", 0f);
            var a = RenderLinear(cam);
            mat.SetFloat("_FlowStreakStrength", 1f);
            var b = RenderLinear(cam);
            return CountBrighter(a, b, th);
        }

        [UnityTest]
        public IEnumerator StreakAppearsOnFlowingSurface()
        {
            int n = Measure(new Vector4(1, 0, 0, 0), 0.05f);
            Debug.Log($"[0099] flowing: n={n}");
            Assert.Greater(n, Size * Size / 20, $"n={n}");
            Assert.Less(n, Size * Size * 9 / 10, $"n={n}");
            yield return null;
        }

        [UnityTest]
        public IEnumerator NoStreakWithoutFlow()
        {
            int n = Measure(Vector4.zero, 0.01f);
            Debug.Log($"[0099] no flow: n={n}");
            Assert.AreEqual(0, n, $"n={n}");
            yield return null;
        }

        [UnityTest]
        public IEnumerator NoStreakOnOceanMaterial()
        {
            mat.SetFloat("_VertexDepth", 0f);
            int n = Measure(new Vector4(1, 0, 0, 0), 0.01f);
            Debug.Log($"[0099] ocean material: n={n}");
            Assert.AreEqual(0, n, $"n={n}");
            yield return null;
        }

        [UnityTest]
        public IEnumerator FallStreakOnMarkedSurface()
        {
            int n = Measure(new Vector4(0, 0, 1, 0), 0.05f);
            Debug.Log($"[0099] fall: n={n}");
            Assert.Greater(n, Size * Size / 20, $"n={n}");
            yield return null;
        }
    }
}
