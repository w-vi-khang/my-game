using UnityEngine;
using UnityEngine.UI;

namespace Game.Client
{
    /// <summary>Dấu người chơi trên bản đồ (brief 0016): tam giác chĩa lên (+y) phủ rect — viền tối, lõi màu `color` thu 0,6. Xoay/dời bằng RectTransform, không dựng lại đỉnh.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public class MinimapMarkerGraphic : MaskableGraphic
    {
        public Color32 outlineColor = new Color32(20, 20, 20, 255);

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var r = rectTransform.rect;
            AddTriangle(vh, r, 1f, outlineColor);
            AddTriangle(vh, r, 0.6f, color);
        }

        static void AddTriangle(VertexHelper vh, Rect r, float scale, Color32 c)
        {
            var center = r.center;
            float hw = r.width * 0.5f * scale, hh = r.height * 0.5f * scale;
            int v = vh.currentVertCount;
            vh.AddVert(new Vector3(center.x, center.y + hh), c, Vector4.zero);
            vh.AddVert(new Vector3(center.x + hw, center.y - hh), c, Vector4.zero);
            vh.AddVert(new Vector3(center.x - hw, center.y - hh), c, Vector4.zero);
            vh.AddTriangle(v, v + 1, v + 2);
        }
    }
}
