using NUnit.Framework;

namespace Game.Core.Tests
{
    /// <summary>Phóng to/kéo/chụm 2 ngón của minimap (brief 0015).</summary>
    public class MinimapViewportTests
    {
        const float Eps = 1e-3f;

        static MinimapViewport New() => new MinimapViewport(384f, 384f, 800f, 600f);

        static void AssertPanel(MinimapViewport v, float wx, float wz, float px, float py, string what)
        {
            v.WorldToPanel(wx, wz, out float gx, out float gy);
            Assert.AreEqual(px, gx, Eps, what + ".x");
            Assert.AreEqual(py, gy, Eps, what + ".y");
        }

        [Test]
        public void ZoomOneFitsWholeWorld()
        {
            var v = New();
            Assert.AreEqual(1f, v.Zoom);
            Assert.AreEqual(1.5625f, v.PixelsPerColumn, Eps);
            AssertPanel(v, 192f, 192f, 400f, 300f, "world center");
            AssertPanel(v, 0f, 0f, 100f, 0f, "corner (0, 0)");
            AssertPanel(v, 384f, 384f, 700f, 600f, "corner (384, 384)");
            v.PanelToWorld(400f, 300f, out float wx, out float wz);
            Assert.AreEqual(192f, wx, Eps);
            Assert.AreEqual(192f, wz, Eps);
        }

        [Test]
        public void ZoomAtKeepsPointUnderCursor()
        {
            var v = New();
            v.PanelToWorld(500f, 350f, out float wx, out float wz);
            Assert.AreEqual(256f, wx, Eps);
            Assert.AreEqual(224f, wz, Eps);
            v.ZoomAt(500f, 350f, 2f);
            Assert.AreEqual(2f, v.Zoom, Eps);
            Assert.AreEqual(224f, v.CenterX, Eps);
            Assert.AreEqual(208f, v.CenterZ, Eps);
            AssertPanel(v, 256f, 224f, 500f, 350f, "point under cursor");
        }

        [Test]
        public void ZoomIsClampedAndZoomOutRecenters()
        {
            var v = New();
            v.ZoomAt(500f, 350f, 100f);
            Assert.AreEqual(MinimapViewport.MaxZoom, v.Zoom, Eps);
            v.ZoomAt(400f, 300f, 0.001f);
            Assert.AreEqual(1f, v.Zoom, Eps);
            Assert.AreEqual(192f, v.CenterX, Eps);
            Assert.AreEqual(192f, v.CenterZ, Eps);
        }

        [Test]
        public void PanIsClampedToWorld()
        {
            var v = New();
            v.Pan(100f, 100f);
            Assert.AreEqual(192f, v.CenterX, Eps, "zoom 1: view ≥ world → centered");
            Assert.AreEqual(192f, v.CenterZ, Eps);

            v.ZoomAt(400f, 300f, 2f); // 3,125 pixel/cột: nửa vùng nhìn 128 × 96 cột
            v.Pan(10000f, 0f);
            Assert.AreEqual(128f, v.CenterX, Eps, "drag fully right → left edge of world visible");
            v.Pan(-10000f, 0f);
            Assert.AreEqual(256f, v.CenterX, Eps);
            v.Pan(0f, 10000f);
            Assert.AreEqual(96f, v.CenterZ, Eps);
            v.Pan(0f, -10000f);
            Assert.AreEqual(288f, v.CenterZ, Eps);
        }

        [Test]
        public void PinchZoomsAroundMidpoint()
        {
            var v = New();
            v.Pinch(300f, 300f, 500f, 300f, 250f, 300f, 550f, 300f); // khoảng cách 200 → 300
            Assert.AreEqual(1.5f, v.Zoom, Eps);
            AssertPanel(v, 192f, 192f, 400f, 300f, "point under placeholder midpoint");
        }

        [Test]
        public void PinchWithoutSpreadPans()
        {
            var a = New();
            var b = New();
            a.ZoomAt(400f, 300f, 2f);
            b.ZoomAt(400f, 300f, 2f);
            a.Pinch(300f, 300f, 500f, 300f, 320f, 310f, 520f, 310f);
            b.Pan(20f, 10f);
            Assert.AreEqual(2f, a.Zoom, Eps);
            Assert.AreEqual(b.CenterX, a.CenterX, Eps);
            Assert.AreEqual(b.CenterZ, a.CenterZ, Eps);
            Assert.AreEqual(185.6f, a.CenterX, Eps);
            Assert.AreEqual(188.8f, a.CenterZ, Eps);
        }

        [Test]
        public void VisibleUvMatchesView()
        {
            var v = New();
            v.VisibleUv(out float u, out float vv, out float w, out float h);
            Assert.AreEqual(-1f / 6f, u, Eps, "zoom 1: viewport wider than world → u < 0");
            Assert.AreEqual(0f, vv, Eps);
            Assert.AreEqual(4f / 3f, w, Eps);
            Assert.AreEqual(1f, h, Eps);

            v.ZoomAt(400f, 300f, 2f);
            v.VisibleUv(out u, out vv, out w, out h);
            Assert.AreEqual(1f / 6f, u, Eps);
            Assert.AreEqual(0.25f, vv, Eps);
            Assert.AreEqual(2f / 3f, w, Eps);
            Assert.AreEqual(0.5f, h, Eps);
        }

        [Test]
        public void InvalidSizesThrow()
        {
            Assert.Throws<System.ArgumentException>(() => new MinimapViewport(0f, 384f, 800f, 600f));
            Assert.Throws<System.ArgumentException>(() => new MinimapViewport(384f, 384f, 800f, 0f));
            Assert.Throws<System.ArgumentException>(() => New().SetPanelSize(-1f, 600f));
        }
    }
}
