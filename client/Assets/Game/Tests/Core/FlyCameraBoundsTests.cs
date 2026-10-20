using NUnit.Framework;

namespace Game.Core.Tests
{
    /// <summary>World-tech bước 7b3 (brief 0012): biên bay, sương biên, góc nhìn HUD.</summary>
    public class FlyCameraBoundsTests
    {
        const float Eps = 1e-4f;
        static readonly WorldSize S = new WorldSize(384, 48, 384);

        [Test]
        public void LimitAxisFarFromEdgeIsUnchanged()
        {
            Assert.AreEqual(-0.5f, FlyCameraBounds.LimitAxis(10, -0.5f, 0, 384), Eps);
            Assert.AreEqual(0.5f, FlyCameraBounds.LimitAxis(10, 0.5f, 0, 384), Eps);
            Assert.AreEqual(0f, FlyCameraBounds.LimitAxis(5, 0, 0, 384), Eps);
        }

        [Test]
        public void LimitAxisSlowsOnlyOutward()
        {
            Assert.AreEqual(-0.25f, FlyCameraBounds.LimitAxis(1, -0.5f, 0, 384), Eps);
            Assert.AreEqual(0.5f, FlyCameraBounds.LimitAxis(1, 0.5f, 0, 384), Eps);
            Assert.AreEqual(0.25f, FlyCameraBounds.LimitAxis(383, 0.5f, 0, 384), Eps);
            Assert.AreEqual(-0.5f, FlyCameraBounds.LimitAxis(383, -0.5f, 0, 384), Eps);
        }

        [Test]
        public void LimitAxisNeverCrossesEdge()
        {
            Assert.AreEqual(-0.1f, FlyCameraBounds.LimitAxis(0.1f, -3f, 0, 384), Eps);
            Assert.AreEqual(0f, FlyCameraBounds.LimitAxis(0, -1f, 0, 384), Eps);
            Assert.AreEqual(0f, FlyCameraBounds.LimitAxis(-2, -1f, 0, 384), Eps);
            Assert.AreEqual(1f, FlyCameraBounds.LimitAxis(-2, 1f, 0, 384), Eps);
        }

        [Test]
        public void CeilingIsWorldTopPlus16()
        {
            Assert.AreEqual(64f, FlyCameraBounds.CeilingY(S), Eps);
            float dx = 0, dy = 1, dz = 0;
            FlyCameraBounds.LimitDelta(S, 100, 63, 100, ref dx, ref dy, ref dz);
            Assert.AreEqual(0.5f, dy, Eps);
            dx = 0; dy = 5; dz = 0;
            FlyCameraBounds.LimitDelta(S, 100, 63.9f, 100, ref dx, ref dy, ref dz);
            Assert.AreEqual(0.1f, dy, Eps);
            dx = 0; dy = -5; dz = 0;
            FlyCameraBounds.LimitDelta(S, 100, 63.9f, 100, ref dx, ref dy, ref dz);
            Assert.AreEqual(-5f, dy, Eps);
            dx = 0; dy = -5; dz = 0;
            FlyCameraBounds.LimitDelta(S, 100, 2, 100, ref dx, ref dy, ref dz);
            Assert.AreEqual(-5f, dy, Eps);
        }

        [Test]
        public void ApproachEdgeIsSmoothAndStops()
        {
            float x = 5f;
            for (int i = 0; i < 600; i++)
            {
                float prev = x;
                x += FlyCameraBounds.LimitAxis(x, -0.16f, 0, 384);
                Assert.GreaterOrEqual(x, 0f);
                Assert.LessOrEqual(x, prev);
            }
            Assert.Less(x, 0.01f);
        }

        [Test]
        public void EdgeFactorByDistance()
        {
            Assert.AreEqual(0f, FlyCameraBounds.EdgeFactor(S, 192, 192), Eps);
            Assert.AreEqual(0.5f, FlyCameraBounds.EdgeFactor(S, 1, 192), Eps);
            Assert.AreEqual(1f, FlyCameraBounds.EdgeFactor(S, 0, 192), Eps);
            Assert.AreEqual(1f, FlyCameraBounds.EdgeFactor(S, -1, 192), Eps);
            Assert.AreEqual(0.5f, FlyCameraBounds.EdgeFactor(S, 383, 192), Eps);
            Assert.AreEqual(0.75f, FlyCameraBounds.EdgeFactor(S, 192, 0.5f), Eps);
            Assert.AreEqual(0.75f, FlyCameraBounds.EdgeFactor(S, 1, 0.5f), Eps);
            Assert.AreEqual(0f, FlyCameraBounds.EdgeFactor(S, 2, 192), Eps);
        }

        [Test]
        public void EdgeFogInterpolates()
        {
            FlyCameraBounds.EdgeFog(64, 120, 0, out float s, out float e);
            Assert.AreEqual(64f, s, 1e-3f);
            Assert.AreEqual(120f, e, 1e-3f);
            FlyCameraBounds.EdgeFog(64, 120, 1, out s, out e);
            Assert.AreEqual(0f, s, 1e-3f);
            Assert.AreEqual(1f, e, 1e-3f);
            FlyCameraBounds.EdgeFog(64, 120, 0.5f, out s, out e);
            Assert.AreEqual(10.954f, e, 1e-3f);
            Assert.AreEqual(2.921f, s, 1e-3f);
            FlyCameraBounds.EdgeFog(0, 40, 0.5f, out s, out e);
            Assert.AreEqual(0f, s, 1e-3f);
            Assert.AreEqual(6.325f, e, 1e-3f);
            float prevEnd = float.MaxValue;
            for (int i = 0; i <= 20; i++)
            {
                FlyCameraBounds.EdgeFog(64, 120, i * 0.05f, out s, out e);
                Assert.Less(s, e);
                Assert.LessOrEqual(e, prevEnd);
                prevEnd = e;
            }
        }

        [Test]
        public void ElevationDegrees()
        {
            Assert.AreEqual(0f, FlyCameraBounds.ElevationDegrees(0), 1e-3f);
            Assert.AreEqual(90f, FlyCameraBounds.ElevationDegrees(1), 1e-3f);
            Assert.AreEqual(-90f, FlyCameraBounds.ElevationDegrees(-1), 1e-3f);
            Assert.AreEqual(30f, FlyCameraBounds.ElevationDegrees(0.5f), 1e-3f);
            Assert.AreEqual(90f, FlyCameraBounds.ElevationDegrees(1.0001f), 1e-3f);
        }

        [Test]
        public void FormatElevation()
        {
            Assert.AreEqual("+12°", FlyCameraBounds.FormatElevation(12.4f));
            Assert.AreEqual("0°", FlyCameraBounds.FormatElevation(-0.4f));
            Assert.AreEqual("-6°", FlyCameraBounds.FormatElevation(-5.6f));
            Assert.AreEqual("0°", FlyCameraBounds.FormatElevation(0));
            Assert.AreEqual("+90°", FlyCameraBounds.FormatElevation(89.5f));
        }
    }
}
