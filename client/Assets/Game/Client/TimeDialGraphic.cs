using UnityEngine;
using UnityEngine.UI;

namespace Game.Client
{
    public enum TimeDialShape : byte { Disk, Moon, Sun, Dawn, Dusk, Star, Drop, Ring, Hoe, Seed, Sprout, Crate, Bang, Dots }

    /// <summary>Hình vẽ bằng code cho vòng chọn thời điểm (brief 0024): đĩa tròn, trăng khuyết, mặt trời có tia, mọc/lặn (nửa đĩa + chân trời + mũi tên). Một màu = `color`.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public class TimeDialGraphic : MaskableGraphic
    {
        public TimeDialShape shape;
        /// <summary>Phần vòng được vẽ (0..1) — chỉ dùng cho Ring.</summary>
        public float fill = 1f;

        public void SetFill(float f)
        {
            f = Mathf.Clamp01(f);
            if (fill == f)
                return;
            fill = f;
            SetVerticesDirty();
        }

        public void SetShape(TimeDialShape s)
        {
            if (shape == s)
                return;
            shape = s;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var rect = rectTransform.rect;
            Vector2 o = rect.center;
            float r = 0.5f * Mathf.Min(rect.width, rect.height);
            Color32 c = color;
            switch (shape)
            {
                case TimeDialShape.Disk:
                    Fan(vh, o, r, 0f, 360f, 32, c);
                    break;
                case TimeDialShape.Sun:
                    Fan(vh, o, 0.5f * r, 0f, 360f, 32, c);
                    for (int k = 0; k < 8; k++)
                    {
                        float a = k * 45f * Mathf.Deg2Rad;
                        var d = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                        var p = new Vector2(-Mathf.Sin(a), Mathf.Cos(a));
                        Quad(vh, o + d * 0.65f * r - p * 0.08f * r, o + d * 0.95f * r - p * 0.08f * r,
                            o + d * 0.95f * r + p * 0.08f * r, o + d * 0.65f * r + p * 0.08f * r, c);
                    }
                    break;
                case TimeDialShape.Moon:
                {
                    const int N = 16;
                    for (int i = 0; i < N; i++)
                    {
                        Vector2 o0 = MoonOuter(i, N, r), i0 = MoonInner(i, N, r);
                        Vector2 o1 = MoonOuter(i + 1, N, r), i1 = MoonInner(i + 1, N, r);
                        Quad(vh, o + o0, o + i0, o + i1, o + o1, c);
                    }
                    break;
                }
                case TimeDialShape.Star:
                    Fan(vh, o, 0.25f * r, 0f, 360f, 16, c);
                    for (int k = 0; k < 4; k++)
                    {
                        float a = k * 90f * Mathf.Deg2Rad;
                        var d = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                        var p = new Vector2(-Mathf.Sin(a), Mathf.Cos(a));
                        Tri(vh, o + d * 0.95f * r, o + p * 0.15f * r, o - p * 0.15f * r, c);
                    }
                    break;
                case TimeDialShape.Drop:
                    Fan(vh, o + new Vector2(0f, -0.25f * r), 0.55f * r, 0f, 360f, 24, c);
                    Tri(vh, o + new Vector2(0f, 0.95f * r), o + new Vector2(-0.5f * r, -0.1f * r), o + new Vector2(0.5f * r, -0.1f * r), c);
                    break;
                case TimeDialShape.Ring:
                {
                    if (fill <= 0f)
                        break;
                    int n = Mathf.Max(1, Mathf.CeilToInt(48f * fill));
                    for (int i = 0; i < n; i++)
                    {
                        float a0 = (90f - 360f * fill * i / n) * Mathf.Deg2Rad;
                        float a1 = (90f - 360f * fill * (i + 1) / n) * Mathf.Deg2Rad;
                        var d0 = new Vector2(Mathf.Cos(a0), Mathf.Sin(a0));
                        var d1 = new Vector2(Mathf.Cos(a1), Mathf.Sin(a1));
                        Quad(vh, o + d0 * 0.84f * r, o + d0 * r, o + d1 * r, o + d1 * 0.84f * r, c);
                    }
                    break;
                }
                case TimeDialShape.Hoe:
                {
                    var dir = new Vector2(1f, 1.3f).normalized;
                    var p = new Vector2(-dir.y, dir.x) * 0.07f * r;
                    var a = new Vector2(-0.6f * r, -0.7f * r);
                    var b = new Vector2(0.4f * r, 0.6f * r);
                    Quad(vh, o + a - p, o + b - p, o + b + p, o + a + p, c);
                    Quad(vh, o + new Vector2(0.1f * r, 0.45f * r), o + new Vector2(0.8f * r, 0.45f * r),
                        o + new Vector2(0.8f * r, 0.7f * r), o + new Vector2(0.1f * r, 0.7f * r), c);
                    break;
                }
                case TimeDialShape.Seed:
                    Fan(vh, o + new Vector2(0f, -0.1f * r), 0.45f * r, 0f, 360f, 20, c);
                    Tri(vh, o + new Vector2(0f, 0.7f * r), o + new Vector2(-0.35f * r, 0.05f * r), o + new Vector2(0.35f * r, 0.05f * r), c);
                    break;
                case TimeDialShape.Sprout:
                    Quad(vh, o + new Vector2(-0.06f * r, -0.8f * r), o + new Vector2(0.06f * r, -0.8f * r),
                        o + new Vector2(0.06f * r, 0.3f * r), o + new Vector2(-0.06f * r, 0.3f * r), c);
                    Fan(vh, o + new Vector2(-0.3f * r, 0.35f * r), 0.32f * r, 0f, 360f, 14, c);
                    Fan(vh, o + new Vector2(0.3f * r, 0.35f * r), 0.32f * r, 0f, 360f, 14, c);
                    break;
                case TimeDialShape.Crate:
                    Quad(vh, o + new Vector2(-0.6f * r, -0.6f * r), o + new Vector2(0.6f * r, -0.6f * r),
                        o + new Vector2(0.6f * r, 0.6f * r), o + new Vector2(-0.6f * r, 0.6f * r), c);
                    break;
                case TimeDialShape.Bang:
                    Quad(vh, o + new Vector2(-0.12f * r, -0.15f * r), o + new Vector2(0.12f * r, -0.15f * r),
                        o + new Vector2(0.12f * r, 0.85f * r), o + new Vector2(-0.12f * r, 0.85f * r), c);
                    Fan(vh, o + new Vector2(0f, -0.6f * r), 0.13f * r, 0f, 360f, 12, c);
                    break;
                case TimeDialShape.Dots:
                    for (int k = -1; k <= 1; k++)
                        Fan(vh, o + new Vector2(0.5f * k * r, 0f), 0.14f * r, 0f, 360f, 10, c);
                    break;
                case TimeDialShape.Dawn:
                case TimeDialShape.Dusk:
                    Quad(vh, o + new Vector2(-0.9f * r, -0.35f * r), o + new Vector2(0.9f * r, -0.35f * r),
                        o + new Vector2(0.9f * r, -0.25f * r), o + new Vector2(-0.9f * r, -0.25f * r), c);
                    Fan(vh, o + new Vector2(0f, -0.25f * r), 0.5f * r, 0f, 180f, 16, c);
                    if (shape == TimeDialShape.Dawn)
                        Tri(vh, o + new Vector2(0f, 0.95f * r), o + new Vector2(-0.25f * r, 0.55f * r), o + new Vector2(0.25f * r, 0.55f * r), c);
                    else
                        Tri(vh, o + new Vector2(0f, 0.55f * r), o + new Vector2(-0.25f * r, 0.95f * r), o + new Vector2(0.25f * r, 0.95f * r), c);
                    break;
            }
        }

        static float MoonS(int i, int n, float r)
        {
            float t = -1f + 2f * i / n;
            return Mathf.Sqrt(Mathf.Max(0f, 1f - t * t)) * 0.85f * r;
        }

        static Vector2 MoonOuter(int i, int n, float r) => new Vector2(0.25f * r - MoonS(i, n, r), (-1f + 2f * i / n) * 0.85f * r);
        static Vector2 MoonInner(int i, int n, float r) => new Vector2(0.25f * r - 0.3f * MoonS(i, n, r), (-1f + 2f * i / n) * 0.85f * r);

        static void Tri(VertexHelper vh, Vector2 a, Vector2 b, Vector2 c, Color32 col)
        {
            int v = vh.currentVertCount;
            vh.AddVert(a, col, Vector4.zero);
            vh.AddVert(b, col, Vector4.zero);
            vh.AddVert(c, col, Vector4.zero);
            vh.AddTriangle(v, v + 1, v + 2);
        }

        static void Quad(VertexHelper vh, Vector2 a, Vector2 b, Vector2 c, Vector2 d, Color32 col)
        {
            int v = vh.currentVertCount;
            vh.AddVert(a, col, Vector4.zero);
            vh.AddVert(b, col, Vector4.zero);
            vh.AddVert(c, col, Vector4.zero);
            vh.AddVert(d, col, Vector4.zero);
            vh.AddTriangle(v, v + 1, v + 2);
            vh.AddTriangle(v, v + 2, v + 3);
        }

        static void Fan(VertexHelper vh, Vector2 center, float radius, float a0, float a1, int seg, Color32 col)
        {
            int v = vh.currentVertCount;
            vh.AddVert(center, col, Vector4.zero);
            for (int i = 0; i <= seg; i++)
            {
                float a = Mathf.Lerp(a0, a1, i / (float)seg) * Mathf.Deg2Rad;
                vh.AddVert(center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius, col, Vector4.zero);
            }
            for (int i = 0; i < seg; i++)
                vh.AddTriangle(v, v + 1 + i, v + 2 + i);
        }
    }
}
