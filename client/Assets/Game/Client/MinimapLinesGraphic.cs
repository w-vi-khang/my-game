using System.Collections.Generic;
using Game.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Client
{
    /// <summary>
    /// Đường ranh giới map trên bản đồ (world-tech bước 7c, brief 0016): mỗi đường [x0, z0, x1, z1, …] toạ độ góc ô (MinimapOverlay.Borders)
    /// → dải tứ giác dày Thickness (đơn vị canvas), nối dài nửa bề dày ở hai đầu để khớp chỗ gấp. Pivot (0, 0) nên toạ độ cục bộ = toạ độ khung của Viewport.
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public class MinimapLinesGraphic : MaskableGraphic
    {
        public List<float[]> Lines;
        public MinimapViewport Viewport;
        public float Thickness = 3f;
        /// <summary>Số đỉnh lần dựng gần nhất (UI mesh tối đa 65.000 đỉnh).</summary>
        public int LastVertexCount { get; private set; }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            LastVertexCount = 0;
            if (Lines == null || Viewport == null)
                return;
            Color32 c = color;
            float half = Thickness * 0.5f;
            foreach (var line in Lines)
            {
                for (int i = 0; i + 3 < line.Length; i += 2)
                {
                    Viewport.WorldToPanel(line[i], line[i + 1], out float ax, out float ay);
                    Viewport.WorldToPanel(line[i + 2], line[i + 3], out float bx, out float by);
                    float dx = bx - ax, dy = by - ay;
                    float len = Mathf.Sqrt(dx * dx + dy * dy);
                    if (len < 1e-4f)
                        continue;
                    float ex = dx / len * half, ey = dy / len * half; // dọc đoạn
                    float nx = -ey, ny = ex;                          // vuông góc
                    ax -= ex; ay -= ey; bx += ex; by += ey;
                    int v = vh.currentVertCount;
                    vh.AddVert(new Vector3(ax + nx, ay + ny), c, Vector4.zero);
                    vh.AddVert(new Vector3(bx + nx, by + ny), c, Vector4.zero);
                    vh.AddVert(new Vector3(bx - nx, by - ny), c, Vector4.zero);
                    vh.AddVert(new Vector3(ax - nx, ay - ny), c, Vector4.zero);
                    vh.AddTriangle(v, v + 1, v + 2);
                    vh.AddTriangle(v, v + 2, v + 3);
                }
            }
            LastVertexCount = vh.currentVertCount;
        }
    }
}
