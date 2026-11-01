using Game.Core;
using NUnit.Framework;
using UnityEngine;

namespace Game.Client.EditTests
{
    /// <summary>WaterLook.Apply đặt đủ 9 thuộc tính hình ảnh nước lên material Game/Ocean (brief 0095).</summary>
    public class WaterLookTests
    {
        [Test]
        public void ApplySetsAllProperties()
        {
            var shader = Shader.Find("Game/Ocean");
            Assert.IsNotNull(shader);
            var m = new Material(shader);
            try
            {
                var c = new WaterLookConfig
                {
                    shallowColor = new[] { 0.1f, 0.2f, 0.3f },
                    deepColor = new[] { 0.4f, 0.5f, 0.6f },
                    depthFull = 1.5f,
                    deepAlpha = 0.7f,
                    shallowAlphaFactor = 0.2f,
                    shoreSoftWidth = 0.5f,
                    rippleScale = 0.9f,
                    rippleStrength = 0.1f,
                    glint = 1.2f,
                };
                float vertexDepthBefore = m.GetFloat("_VertexDepth");
                WaterLook.Apply(m, c);
                AssertColor(m, "_ShallowColor", 0.1f, 0.2f, 0.3f);
                AssertColor(m, "_DeepColor", 0.4f, 0.5f, 0.6f);
                Assert.AreEqual(1.5f, m.GetFloat("_DepthFull"), 1e-4f);
                Assert.AreEqual(0.7f, m.GetFloat("_Alpha"), 1e-4f);
                Assert.AreEqual(0.2f, m.GetFloat("_ThinAlpha"), 1e-4f);
                Assert.AreEqual(0.5f, m.GetFloat("_ShoreSoftWidth"), 1e-4f);
                Assert.AreEqual(0.9f, m.GetFloat("_RippleScale"), 1e-4f);
                Assert.AreEqual(0.1f, m.GetFloat("_RippleStrength"), 1e-4f);
                Assert.AreEqual(1.2f, m.GetFloat("_Glint"), 1e-4f);
                Assert.AreEqual(0f, m.GetFloat("_VertexDepth"), 1e-6f);
                Assert.AreEqual(vertexDepthBefore, m.GetFloat("_VertexDepth"), 1e-6f);
            }
            finally
            {
                Object.DestroyImmediate(m);
            }
        }

        static void AssertColor(Material m, string name, float r, float g, float b)
        {
            var c = m.GetColor(name);
            Assert.AreEqual(r, c.r, 1e-4f, name);
            Assert.AreEqual(g, c.g, 1e-4f, name);
            Assert.AreEqual(b, c.b, 1e-4f, name);
            Assert.AreEqual(1f, c.a, 1e-4f, name);
        }
    }
}
