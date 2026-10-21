using NUnit.Framework;
using UnityEngine;

namespace Game.Client.Tests
{
    /// <summary>Brief 0023: bầu trời/ánh sáng theo pha (hàm thuần DaySky).</summary>
    public class DaySkyTests
    {
        static void Near(Color e, Color a, float tol = 1e-4f)
        {
            Assert.AreEqual(e.r, a.r, tol, "r");
            Assert.AreEqual(e.g, a.g, tol, "g");
            Assert.AreEqual(e.b, a.b, tol, "b");
        }

        [Test]
        public void NoonMatchesOldSceneLook()
        {
            var s = DaySky.Evaluate(0.5f);
            Near(new Color(0.36f, 0.62f, 0.92f), s.zenith);
            Near(new Color(0.78f, 0.88f, 0.97f), s.horizon);
            Near(new Color(0.72f, 0.82f, 0.96f), s.ambientSky);
            Near(new Color(0.70f, 0.69f, 0.64f), s.ambientEquator);
            Near(new Color(0.42f, 0.37f, 0.30f), s.ambientGround);
            Near(new Color(1f, 0.95f, 0.86f), s.lightColor);
            Near(Color.white, s.waterTint);
            Assert.AreEqual(0f, s.stars);
            Assert.AreEqual(0.9f, s.lightIntensity, 1e-5f);
            Assert.Less(Vector3.Angle(s.lightForward, Quaternion.Euler(50, -30, 0) * Vector3.forward), 0.01f);
        }

        [Test]
        public void SunPath()
        {
            Assert.Less(Mathf.Abs(DaySky.Evaluate(0.25f).sunDir.y), 1e-4f);
            Assert.Less(Mathf.Abs(DaySky.Evaluate(0.75f).sunDir.y), 1e-4f);
            float sin50 = Mathf.Sin(50f * Mathf.Deg2Rad);
            Assert.AreEqual(sin50, DaySky.Evaluate(0.5f).sunDir.y, 1e-4f);
            Assert.AreEqual(-sin50, DaySky.Evaluate(0f).sunDir.y, 1e-4f);
            Assert.Less(Vector3.Angle(DaySky.Evaluate(0.75f).sunDir, DaySky.SetDir), 0.01f);
            Assert.Less(Vector3.Angle(DaySky.Evaluate(0.25f).sunDir, -DaySky.SetDir), 0.01f);
            Assert.AreEqual(0.22f, DaySky.Evaluate(0f).lightIntensity, 1e-5f);
            Assert.Less(DaySky.Evaluate(0.25f).lightIntensity, 1e-4f);
            for (int k = 0; k < 200; k++)
            {
                var s = DaySky.Evaluate(k / 200f);
                if (s.lightIntensity > 1e-3f)
                    Assert.Less(s.lightForward.y, 0f, "pha " + k / 200f);
            }
        }

        [Test]
        public void ContinuousIncludingWrap()
        {
            const int N = 2000;
            var prev = DaySky.Evaluate(0f);
            for (int k = 1; k <= N; k++)
            {
                var cur = DaySky.Evaluate(k / (float)N);
                string at = " ở mẫu " + k;
                Step(prev.zenith, cur.zenith, "zenith" + at);
                Step(prev.horizon, cur.horizon, "horizon" + at);
                Step(prev.cloud, cur.cloud, "cloud" + at);
                Step(prev.cloudShade, cur.cloudShade, "cloudShade" + at);
                Step(prev.ambientSky, cur.ambientSky, "ambientSky" + at);
                Step(prev.ambientEquator, cur.ambientEquator, "ambientEquator" + at);
                Step(prev.ambientGround, cur.ambientGround, "ambientGround" + at);
                Step(prev.waterTint, cur.waterTint, "waterTint" + at);
                Assert.Less(Mathf.Abs(prev.stars - cur.stars), 0.02f, "stars" + at);
                Assert.Less(Mathf.Abs(prev.lightIntensity - cur.lightIntensity), 0.02f, "lightIntensity" + at);
                if (Vector3.Angle(prev.lightForward, cur.lightForward) > 10f)
                {
                    Assert.Less(prev.lightIntensity, 1e-3f, "đổi hướng đèn khi còn sáng" + at);
                    Assert.Less(cur.lightIntensity, 1e-3f, "đổi hướng đèn khi còn sáng" + at);
                }
                prev = cur;
            }
        }

        static void Step(Color a, Color b, string what)
        {
            Assert.Less(Mathf.Abs(a.r - b.r), 0.02f, what + " r");
            Assert.Less(Mathf.Abs(a.g - b.g), 0.02f, what + " g");
            Assert.Less(Mathf.Abs(a.b - b.b), 0.02f, what + " b");
        }

        [Test]
        public void NightIsDarkWithStars()
        {
            var night = DaySky.Evaluate(0f);
            var noon = DaySky.Evaluate(0.5f);
            Assert.Less(night.ambientSky.grayscale, 0.25f * noon.ambientSky.grayscale);
            Assert.AreEqual(1f, night.stars);
            Near(DaySky.MoonLightColor, night.lightColor);
            for (int i = 0; i <= 40; i++)
                Assert.AreEqual(0f, DaySky.Evaluate(0.30f + i * 0.01f).stars, "pha " + (0.30f + i * 0.01f));
        }

        [Test]
        public void StarsRotateWithSun()
        {
            foreach (float p in new[] { 0f, 0.13f, 0.25f, 0.5f, 0.77f })
            {
                var s = DaySky.Evaluate(p);
                // Vector3.Angle dùng acos nên sai số float cỡ 0,02° khi hai vector gần trùng; so khoảng cách (1,7e-4 ≈ 0,01°).
                Assert.Less((Quaternion.Inverse(s.starRotation) * s.sunDir - DaySky.NoonSunDir).magnitude, 1.7e-4f, "pha " + p);
            }
            Assert.Less(Quaternion.Angle(DaySky.Evaluate(0.5f).starRotation, Quaternion.identity), 0.01f);
        }
    }
}
