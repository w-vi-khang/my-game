using UnityEngine;
using UnityEngine.UI;

namespace Game.Client
{
    /// <summary>Biểu tượng dấu mốc trên bản đồ (brief 0018): hình thoi (checkpoint) hoặc hình vuông (công trình) phủ rect — viền tối, lõi màu `color` thu 0,65.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public class MinimapLandmarkGraphic : MaskableGraphic
    {
        public bool diamond;
        public Color32 outlineColor = new Color32(20, 20, 20, 255);

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var r = rectTransform.rect;
            AddShape(vh, r, 1f, outlineColor);
            AddShape(vh, r, 0.65f, color);
        }

        void AddShape(VertexHelper vh, Rect r, float scale, Color32 c)
        {
            var o = r.center;
            float hw = r.width * 0.5f * scale, hh = r.height * 0.5f * scale;
            int v = vh.currentVertCount;
            if (diamond)
            {
                vh.AddVert(new Vector3(o.x, o.y + hh), c, Vector4.zero);
                vh.AddVert(new Vector3(o.x + hw, o.y), c, Vector4.zero);
                vh.AddVert(new Vector3(o.x, o.y - hh), c, Vector4.zero);
                vh.AddVert(new Vector3(o.x - hw, o.y), c, Vector4.zero);
            }
            else
            {
                vh.AddVert(new Vector3(o.x - hw, o.y + hh), c, Vector4.zero);
                vh.AddVert(new Vector3(o.x + hw, o.y + hh), c, Vector4.zero);
                vh.AddVert(new Vector3(o.x + hw, o.y - hh), c, Vector4.zero);
                vh.AddVert(new Vector3(o.x - hw, o.y - hh), c, Vector4.zero);
            }
            vh.AddTriangle(v, v + 1, v + 2);
            vh.AddTriangle(v, v + 2, v + 3);
        }
    }
}
