using Game.Core;
using NUnit.Framework;
using UnityEngine;

namespace Game.Client.EditTests
{
    /// <summary>WaterLook.Apply đặt đủ 9 + 11 (bọt, brief 0096) + 10 (lấp lánh, brief 0097) + 9 (vệt chảy, 0099) thuộc tính hình ảnh nước lên material Game/Ocean (brief 0095).</summary>
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
                    foamColor = new[] { 0.7f, 0.8f, 0.9f },
                    foamWidth = 1.5f,
                    foamBands = 4f,
                    foamLine = 0.4f,
                    foamSoft = 0.2f,
                    foamSpeed = 0.5f,
                    foamSwing = 0.6f,
                    foamSwingSpeed = 1.5f,
                    foamNoiseScale = 2f,
                    foamOpenMin = 0.3f,
                    foamStrength = 0.7f,
                    glintPower = 200f,
                    glintHalo = 1.1f,
                    glintHaloPower = 500f,
                    sparkleStrength = 2f,
                    sparkleScale = 2.5f,
                    sparkleDensity = 0.6f,
                    sparkleSize = 0.2f,
                    sparkleSpeed = 5f,
                    sparkleFadeStart = 10f,
                    sparkleFadeEnd = 40f,
                    flowStreakColor = new[] { 0.2f, 0.3f, 0.4f },
                    flowStreakStrength = 0.5f,
                    flowStreakScale = 2f,
                    flowStreakLength = 7f,
                    flowStreakSpeed = 2f,
                    flowStreakCycle = 3f,
                    flowStreakCut = 0.6f,
                    flowStreakSoft = 0.2f,
                    flowFallSpeed = 4f,
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
                AssertColor(m, "_FoamColor", 0.7f, 0.8f, 0.9f);
                Assert.AreEqual(1.5f, m.GetFloat("_FoamWidth"), 1e-4f);
                Assert.AreEqual(4f, m.GetFloat("_FoamBands"), 1e-4f);
                Assert.AreEqual(0.4f, m.GetFloat("_FoamLine"), 1e-4f);
                Assert.AreEqual(0.2f, m.GetFloat("_FoamSoft"), 1e-4f);
                Assert.AreEqual(0.5f, m.GetFloat("_FoamSpeed"), 1e-4f);
                Assert.AreEqual(0.6f, m.GetFloat("_FoamSwing"), 1e-4f);
                Assert.AreEqual(1.5f, m.GetFloat("_FoamSwingSpeed"), 1e-4f);
                Assert.AreEqual(2f, m.GetFloat("_FoamNoiseScale"), 1e-4f);
                Assert.AreEqual(0.3f, m.GetFloat("_FoamOpenMin"), 1e-4f);
                Assert.AreEqual(0.7f, m.GetFloat("_FoamStrength"), 1e-4f);
                Assert.AreEqual(200f, m.GetFloat("_GlintPower"), 1e-3f);
                Assert.AreEqual(1.1f, m.GetFloat("_GlintHalo"), 1e-4f);
                Assert.AreEqual(500f, m.GetFloat("_GlintHaloPower"), 1e-3f);
                Assert.AreEqual(2f, m.GetFloat("_SparkleStrength"), 1e-4f);
                Assert.AreEqual(2.5f, m.GetFloat("_SparkleScale"), 1e-4f);
                Assert.AreEqual(0.6f, m.GetFloat("_SparkleDensity"), 1e-4f);
                Assert.AreEqual(0.2f, m.GetFloat("_SparkleSize"), 1e-4f);
                Assert.AreEqual(5f, m.GetFloat("_SparkleSpeed"), 1e-4f);
                Assert.AreEqual(10f, m.GetFloat("_SparkleFadeStart"), 1e-4f);
                Assert.AreEqual(40f, m.GetFloat("_SparkleFadeEnd"), 1e-4f);
                AssertColor(m, "_FlowStreakColor", 0.2f, 0.3f, 0.4f);
                Assert.AreEqual(0.5f, m.GetFloat("_FlowStreakStrength"), 1e-4f);
                Assert.AreEqual(2f, m.GetFloat("_FlowStreakScale"), 1e-4f);
                Assert.AreEqual(7f, m.GetFloat("_FlowStreakLength"), 1e-4f);
                Assert.AreEqual(2f, m.GetFloat("_FlowStreakSpeed"), 1e-4f);
                Assert.AreEqual(3f, m.GetFloat("_FlowStreakCycle"), 1e-4f);
                Assert.AreEqual(0.6f, m.GetFloat("_FlowStreakCut"), 1e-4f);
                Assert.AreEqual(0.2f, m.GetFloat("_FlowStreakSoft"), 1e-4f);
                Assert.AreEqual(4f, m.GetFloat("_FlowFallSpeed"), 1e-4f);
                Assert.AreEqual(0f, m.GetFloat("_VertexDepth"), 1e-6f);
                Assert.AreEqual(vertexDepthBefore, m.GetFloat("_VertexDepth"), 1e-6f);
            }
            finally
            {
                Object.DestroyImmediate(m);
            }
        }

        [Test]
        public void FoamOffByDefault()
        {
            var m = new Material(Shader.Find("Game/Ocean"));
            try { Assert.AreEqual(0f, m.GetFloat("_FoamStrength")); }
            finally { Object.DestroyImmediate(m); }
        }

        [Test]
        public void FlowStreakOffByDefault()
        {
            var m = new Material(Shader.Find("Game/Ocean"));
            try { Assert.AreEqual(0f, m.GetFloat("_FlowStreakStrength")); }
            finally { Object.DestroyImmediate(m); }
        }

        [Test]
        public void GlintExtrasOffByDefault()
        {
            var m = new Material(Shader.Find("Game/Ocean"));
            try
            {
                Assert.AreEqual(0f, m.GetFloat("_GlintHalo"));
                Assert.AreEqual(0f, m.GetFloat("_SparkleStrength"));
                Assert.AreEqual(160f, m.GetFloat("_GlintPower"));
            }
            finally { Object.DestroyImmediate(m); }
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
