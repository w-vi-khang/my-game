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
        }
    }
}
