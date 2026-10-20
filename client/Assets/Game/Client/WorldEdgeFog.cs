using Game.Core;
using UnityEngine;

namespace Game.Client
{
    /// <summary>
    /// Sương biên world (world-tech bước 7b3, brief 0012): trong 2 ô cuối trước mép x/z, sương dày dần tới che hết — cả bầu trời
    /// (skybox Game/SkyClouds trộn màu sương qua biến global _EdgeFogBlend) và nền dưới nước. Trần bay không có sương.
    /// Gọi UnderwaterView.Refresh() trước khi ghi nên không phụ thuộc thứ tự LateUpdate giữa hai component.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class WorldEdgeFog : MonoBehaviour
    {
        public const string BlendProperty = "_EdgeFogBlend";

        public TerrainChunkView terrain;
        /// <summary>Hiệu ứng dưới nước của cùng camera; null → chỉ xử lý trên cạn.</summary>
        public UnderwaterView underwater;

        /// <summary>Độ dày sương biên của khung gần nhất (0..1).</summary>
        public float Factor { get; private set; }

        Camera cam;
        float baseStart, baseEnd;

        void Start()
        {
            cam = GetComponent<Camera>();
            baseStart = RenderSettings.fogStartDistance;
            baseEnd = RenderSettings.fogEndDistance;
        }

        void LateUpdate()
        {
            if (terrain == null || terrain.World == null)
                return;
            if (underwater != null)
                underwater.Refresh();
            var p = terrain.transform.InverseTransformPoint(transform.position);
            Factor = FlyCameraBounds.EdgeFactor(terrain.World.Size, p.x, p.z);
            bool under = underwater != null && underwater.IsUnderwater;
            float s0 = under ? underwater.fogStart : baseStart;
            float e0 = under ? underwater.fogEnd : baseEnd;
            FlyCameraBounds.EdgeFog(s0, e0, Factor, out float start, out float end);
            RenderSettings.fogStartDistance = start;
            RenderSettings.fogEndDistance = end;
            Shader.SetGlobalFloat(BlendProperty, Factor);
            if (under)
                cam.backgroundColor = Color.Lerp(underwater.backgroundColor, underwater.fogColor, Factor);
        }

        void OnDisable()
        {
            Factor = 0f;
            Shader.SetGlobalFloat(BlendProperty, 0f);
            if (underwater != null && underwater.IsUnderwater)
            {
                RenderSettings.fogStartDistance = underwater.fogStart;
                RenderSettings.fogEndDistance = underwater.fogEnd;
                if (cam != null)
                    cam.backgroundColor = underwater.backgroundColor;
            }
            else if (cam != null)
            {
                RenderSettings.fogStartDistance = baseStart;
                RenderSettings.fogEndDistance = baseEnd;
            }
        }
    }
}
