using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Game.Client.Tests
{
    /// <summary>Task m1-bien-nhuom-mau-troi: mặt biển xa nhuộm màu chân trời theo buổi, sương không gãy ở mốc fogEnd; nhìn gần / dưới nước / không có DayCycleView giữ như cũ.</summary>
    public class SeaSkyTintPlayTests
    {
        const int H = 3_600_000;
        const int Size = 256;
        const float CamH = 26f, Pitch = 13f;
        static readonly (string name, long ms)[] Phases = { ("binh-minh", 6L * H), ("ngay", 12L * H), ("hoang-hon", 18L * H), ("dem", 0L) };

        // Mốc đo trước khi sửa shader (base e15b86a, bước 2 brief 0034). BaselineFilled = true → test chỉ ghi log.
        static readonly bool BaselineFilled = true;
        static readonly float[] BeforeDev90 = { 0.47318f, 0.27425f, 0.47318f, 0.02406f };      // theo thứ tự Phases
        static readonly float[] BeforeKinkSea = { 0.03123f, 0.01790f, 0.03123f, 0.00167f };    // theo thứ tự Phases
        const float BeforeKinkTerrain = 0.02230f;                           // trưa
        static readonly Color BeforeOffNear40 = new Color(0.03095f, 0.27051f, 0.46851f, 1f);

        GameObject lightGo, camGo, sea, ground;
        Material skyMat, seaMat, groundMat;
        DayCycleView view;
        long now;

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
            RenderSettings.fogStartDistance = 64f;
            RenderSettings.fogEndDistance = 120f;
            Shader.SetGlobalFloat("_EdgeFogBlend", 0f);

            lightGo = new GameObject("Light");
            lightGo.AddComponent<Light>().type = LightType.Directional;

            camGo = new GameObject("Cam");
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.Skybox;
            cam.fieldOfView = 60;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 128f;
            camGo.transform.position = new Vector3(0, CamH, 0);
            camGo.transform.rotation = Quaternion.Euler(Pitch, 0, 0);
            view = camGo.AddComponent<DayCycleView>();
            view.sun = lightGo.GetComponent<Light>();
            view.clockMs = () => now;
            view.secondsNow = () => 100.0;

            sea = GameObject.CreatePrimitive(PrimitiveType.Plane);
            sea.transform.position = Vector3.zero;
            sea.transform.localScale = new Vector3(60, 1, 60);
            seaMat = new Material(Shader.Find("Game/Ocean"));
            seaMat.SetFloat("_RippleStrength", 0f);
            seaMat.SetFloat("_Glint", 0f);
            seaMat.SetFloat("_Cull", 0f);
            sea.GetComponent<MeshRenderer>().sharedMaterial = seaMat;

            ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.transform.position = Vector3.zero;
            ground.transform.localScale = new Vector3(60, 1, 60);
            groundMat = new Material(Shader.Find("Game/Terrain"));
            groundMat.SetFloat("_PatternStrength", 0f);
            ground.GetComponent<MeshRenderer>().sharedMaterial = groundMat;
            ground.SetActive(false);
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Object.Destroy(camGo);
            Object.Destroy(lightGo);
            Object.Destroy(sea);
            Object.Destroy(ground);
            Object.Destroy(skyMat);
            Object.Destroy(seaMat);
            Object.Destroy(groundMat);
            RenderSettings.skybox = oldSkybox;
            RenderSettings.fog = oldFog;
            RenderSettings.fogMode = oldMode;
            RenderSettings.fogStartDistance = oldStart;
            RenderSettings.fogEndDistance = oldEnd;
            RenderSettings.fogColor = oldColor;
            Shader.SetGlobalFloat("_DayCycleOn", 0f);
            yield return null;
        }

        // Ảnh float linear (không lượng tử 8 bit); điểm (x, y) = px[y * Size + x], y = 0 là hàng dưới cùng.
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

        // Hàng ảnh nơi tia ở cột giữa chạm mặt phẳng y = 0 ở khoảng cách d (camera cao CamH, cúi Pitch độ, FOV dọc 60).
        static int RowAt(float d)
        {
            float a = Mathf.Asin(CamH / d) - Pitch * Mathf.Deg2Rad; // góc dưới trục camera
            return Mathf.RoundToInt(Size * 0.5f - Size * 0.5f * Mathf.Tan(a) / Mathf.Tan(30f * Mathf.Deg2Rad) - 0.5f);
        }

        // Trung bình một kênh trên cột 120..135 của hàng y.
        static float Avg(Color[] px, int y, int ch)
        {
            float s = 0f;
            for (int x = 120; x < 136; x++) s += px[y * Size + x][ch];
            return s / 16f;
        }

        static Color AvgColor(Color[] px, int y) => new Color(Avg(px, y, 0), Avg(px, y, 1), Avg(px, y, 2), 1f);

        // Lệch lớn nhất theo kênh R, G, B.
        static float MaxDiff(Color a, Color b) =>
            Mathf.Max(Mathf.Abs(a.r - b.r), Mathf.Abs(a.g - b.g), Mathf.Abs(a.b - b.b));

        // Độ gãy độ dốc tại hàng ứng với khoảng cách d: |dốc 3 hàng phía gần − dốc 3 hàng phía xa|, lớn nhất theo kênh.
        static float Kink(Color[] px, float d)
        {
            int r = RowAt(d);
            float best = 0f;
            for (int ch = 0; ch < 3; ch++)
            {
                float near = (Avg(px, r, ch) - Avg(px, r - 3, ch)) / 3f;
                float far = (Avg(px, r + 3, ch) - Avg(px, r, ch)) / 3f;
                best = Mathf.Max(best, Mathf.Abs(near - far));
            }
            return best;
        }

        Camera Cam => camGo.GetComponent<Camera>();

        [UnityTest]
        public IEnumerator SeaAt90mBlendsTowardFogColor()
        {
            for (int i = 0; i < Phases.Length; i++)
            {
                now = Phases[i].ms;
                view.Apply();
                var px = RenderLinear(Cam);
                float dev = MaxDiff(AvgColor(px, RowAt(90f)), RenderSettings.fogColor.linear);
                Debug.Log($"[0034] dev90 {Phases[i].name}: {dev:F5}");
                if (BaselineFilled) Assert.LessOrEqual(dev, 0.5f * BeforeDev90[i], Phases[i].name);
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator SeaFogHasNoKinkAtFogEnd()
        {
            for (int i = 0; i < Phases.Length; i++)
            {
                now = Phases[i].ms;
                view.Apply();
                var px = RenderLinear(Cam);
                float k = Kink(px, 120f);
                Debug.Log($"[0034] kinkSea {Phases[i].name}: {k:F5}");
                if (BaselineFilled) Assert.LessOrEqual(k, 0.5f * BeforeKinkSea[i], Phases[i].name);
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator TerrainFogHasNoKinkAtFogEnd()
        {
            sea.SetActive(false);
            ground.SetActive(true);
            now = 12 * H;
            view.Apply();
            var px = RenderLinear(Cam);
            float k = Kink(px, 120f);
            Debug.Log($"[0034] kinkTerrain: {k:F5}");
            if (BaselineFilled) Assert.LessOrEqual(k, 0.5f * BeforeKinkTerrain);
            yield return null;
        }

        [UnityTest]
        public IEnumerator DayCycleOffNearSeaUnchanged()
        {
            now = 12 * H;
            view.Apply();
            Shader.SetGlobalFloat("_DayCycleOn", 0f);
            var px = RenderLinear(Cam);
            var c = AvgColor(px, RowAt(40f));
            Debug.Log($"[0034] offNear40: {c.r:F5} {c.g:F5} {c.b:F5}");
            if (BaselineFilled) Assert.LessOrEqual(MaxDiff(c, BeforeOffNear40), 0.002f);
            yield return null;
        }

        [UnityTest]
        public IEnumerator CloseTopViewUnchangedByDayCycle()
        {
            camGo.transform.SetPositionAndRotation(new Vector3(0, 3, 0), Quaternion.Euler(60, 0, 0));
            now = 12 * H;
            view.Apply();
            var on = AvgColor(RenderLinear(Cam), 128);
            Shader.SetGlobalFloat("_DayCycleOn", 0f);
            var off = AvgColor(RenderLinear(Cam), 128);
            Debug.Log($"[0034] closeTop on {on.r:F5} {on.g:F5} {on.b:F5} off {off.r:F5} {off.g:F5} {off.b:F5}");
            Assert.LessOrEqual(MaxDiff(on, off), 0.01f);
            yield return null;
        }

        [UnityTest]
        public IEnumerator UnderwaterViewUnchangedByDayCycle()
        {
            camGo.transform.SetPositionAndRotation(new Vector3(0, -3, 0), Quaternion.Euler(-10, 0, 0));
            now = 12 * H;
            view.Apply();
            var on = AvgColor(RenderLinear(Cam), 128);
            Shader.SetGlobalFloat("_DayCycleOn", 0f);
            var off = AvgColor(RenderLinear(Cam), 128);
            Debug.Log($"[0034] underwater on {on.r:F5} {on.g:F5} {on.b:F5} off {off.r:F5} {off.g:F5} {off.b:F5}");
            Assert.LessOrEqual(MaxDiff(on, off), 0.01f);
            yield return null;
        }
    }
}
