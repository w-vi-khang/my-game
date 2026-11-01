using Game.Core;
using UnityEngine;

namespace Game.Client
{
    /// <summary>Áp tham số hình ảnh nước (water_m1.json "look", brief 0095) vào material Game/Ocean. Không đụng _VertexDepth, blend, ZWrite, Cull.</summary>
    public static class WaterLook
    {
        public static void Apply(Material m, WaterLookConfig c)
        {
            m.SetColor("_ShallowColor", new Color(c.shallowColor[0], c.shallowColor[1], c.shallowColor[2], 1f));
            m.SetColor("_DeepColor", new Color(c.deepColor[0], c.deepColor[1], c.deepColor[2], 1f));
            m.SetFloat("_DepthFull", c.depthFull);
            m.SetFloat("_Alpha", c.deepAlpha);
            m.SetFloat("_ThinAlpha", c.shallowAlphaFactor);
            m.SetFloat("_ShoreSoftWidth", c.shoreSoftWidth);
            m.SetFloat("_RippleScale", c.rippleScale);
            m.SetFloat("_RippleStrength", c.rippleStrength);
            m.SetFloat("_Glint", c.glint);
            m.SetColor("_FoamColor", new Color(c.foamColor[0], c.foamColor[1], c.foamColor[2], 1f));
            m.SetFloat("_FoamWidth", c.foamWidth);
            m.SetFloat("_FoamBands", c.foamBands);
            m.SetFloat("_FoamLine", c.foamLine);
            m.SetFloat("_FoamSoft", c.foamSoft);
            m.SetFloat("_FoamSpeed", c.foamSpeed);
            m.SetFloat("_FoamSwing", c.foamSwing);
            m.SetFloat("_FoamSwingSpeed", c.foamSwingSpeed);
            m.SetFloat("_FoamNoiseScale", c.foamNoiseScale);
            m.SetFloat("_FoamOpenMin", c.foamOpenMin);
            m.SetFloat("_FoamStrength", c.foamStrength);
            m.SetFloat("_GlintPower", c.glintPower);
            m.SetFloat("_GlintHalo", c.glintHalo);
            m.SetFloat("_GlintHaloPower", c.glintHaloPower);
            m.SetFloat("_SparkleStrength", c.sparkleStrength);
            m.SetFloat("_SparkleScale", c.sparkleScale);
            m.SetFloat("_SparkleDensity", c.sparkleDensity);
            m.SetFloat("_SparkleSize", c.sparkleSize);
            m.SetFloat("_SparkleSpeed", c.sparkleSpeed);
            m.SetFloat("_SparkleFadeStart", c.sparkleFadeStart);
            m.SetFloat("_SparkleFadeEnd", c.sparkleFadeEnd);
            m.SetColor("_FlowStreakColor", new Color(c.flowStreakColor[0], c.flowStreakColor[1], c.flowStreakColor[2], 1f));
            m.SetFloat("_FlowStreakStrength", c.flowStreakStrength);
            m.SetFloat("_FlowStreakScale", c.flowStreakScale);
            m.SetFloat("_FlowStreakLength", c.flowStreakLength);
            m.SetFloat("_FlowStreakSpeed", c.flowStreakSpeed);
            m.SetFloat("_FlowStreakCycle", c.flowStreakCycle);
            m.SetFloat("_FlowStreakCut", c.flowStreakCut);
            m.SetFloat("_FlowStreakSoft", c.flowStreakSoft);
            m.SetFloat("_FlowFallSpeed", c.flowFallSpeed);
        }
    }
}
