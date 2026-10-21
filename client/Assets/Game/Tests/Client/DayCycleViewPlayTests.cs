using System.Collections;
using Game.Core.DayCycle;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Game.Client.Tests
{
    public class DayCycleViewPlayTests
    {
        const int H = 3_600_000;
        GameObject lightGo, camGo;
        Light light;
        DayCycleView view;
        long now = 12 * H;
        double t = 100.0;

        static void Near(Color e, Color a, float tol = 1e-4f)
        {
            Assert.AreEqual(e.r, a.r, tol, "r");
            Assert.AreEqual(e.g, a.g, tol, "g");
            Assert.AreEqual(e.b, a.b, tol, "b");
        }

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            now = 12 * H;
            t = 100.0;
            lightGo = new GameObject("Light");
            light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            camGo = new GameObject("Cam");
            camGo.AddComponent<Camera>();
            view = camGo.AddComponent<DayCycleView>();
            view.sun = light;
            view.clockMs = () => now;
            view.secondsNow = () => t;
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Object.Destroy(camGo);
            Object.Destroy(lightGo);
            yield return null;
        }

        [UnityTest]
        public IEnumerator FollowsClockAtNoonAndMidnight()
        {
            Assert.Less(Vector3.Angle(light.transform.forward, Quaternion.Euler(50, -30, 0) * Vector3.forward), 0.05f);
            Assert.AreEqual(0.9f, light.intensity, 1e-4f);
            Near(DaySky.Evaluate(0.5f).horizon, RenderSettings.fogColor);
            Near(DaySky.Evaluate(0.5f).ambientSky, RenderSettings.ambientSkyColor);
            Assert.AreEqual(1f, Shader.GetGlobalFloat("_DayCycleOn"));
            now = 0;
            view.Apply();
            Assert.AreEqual(0.22f, light.intensity, 1e-4f);
            Assert.Less(light.transform.forward.y, 0f);
            Near(DaySky.Evaluate(0f).horizon, RenderSettings.fogColor);
            yield return null;
        }

        [UnityTest]
        public IEnumerator SelectRotatesForwardThenIgnoresDuringRotation()
        {
            Assert.AreEqual(18 * H, view.Select(TimeLandmark.Dawn));
            view.Apply();
            Assert.AreEqual(12 * H, view.DisplayPhaseMs);
            Assert.IsTrue(view.IsRotating);
            t += 1.5;
            view.Apply();
            Assert.AreEqual(21 * H, view.DisplayPhaseMs);
            int off = view.Day.OffsetMs;
            Assert.AreEqual(-1, view.Select(TimeLandmark.Noon));
            Assert.AreEqual(off, view.Day.OffsetMs);
            t += 1.5;
            view.Apply();
            Assert.IsFalse(view.IsRotating);
            Assert.AreEqual(view.Day.PhaseMs, view.DisplayPhaseMs);
            Assert.AreEqual(6 * H, view.DisplayPhaseMs);
            yield return null;
        }

        [UnityTest]
        public IEnumerator SameWindowDoesNothing()
        {
            Assert.AreEqual(0, view.Select(TimeLandmark.Noon));
            Assert.IsFalse(view.IsRotating);
            yield return null;
        }

        [UnityTest]
        public IEnumerator DisableClearsSkyFlag()
        {
            view.enabled = false;
            Assert.AreEqual(0f, Shader.GetGlobalFloat("_DayCycleOn"));
            yield return null;
        }
    }
}
