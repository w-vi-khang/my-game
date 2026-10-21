using Game.Core;
using Game.World;
using UnityEngine;

namespace Game.Client
{
    /// <summary>
    /// Camera dưới nước (world-tech bước 7b2, brief 0011): sương ngắn màu nước + nền sáng màu nước (không lộ skybox ở chân trời);
    /// lên khỏi nước thì trả lại sương và nền cũ của scene.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class UnderwaterView : MonoBehaviour
    {
        public TerrainChunkView terrain;
        public Color fogColor = new Color(0.14f, 0.44f, 0.56f);
        /// <summary>Nền khi dưới nước — sáng hơn sương: nhìn lên qua mặt nước thấy sáng như ánh trời (brief 0011 sửa 1).</summary>
        public Color backgroundColor = new Color(0.42f, 0.70f, 0.82f);
        public float fogStart = 0f;
        public float fogEnd = 40f;
        /// <summary>Nhân vào màu sương/nền dưới nước theo buổi (DayCycleView, brief 0023); trắng = như cũ.</summary>
        public Color tint = Color.white;

        public bool IsUnderwater { get; private set; }
        public Color CurrentFogColor => fogColor * tint;
        public Color CurrentBackground => backgroundColor * tint;

        Camera cam;
        Color savedFogColor, savedBackground;
        float savedFogStart, savedFogEnd;
        CameraClearFlags savedClearFlags;

        void Awake()
        {
            cam = GetComponent<Camera>();
        }

        void LateUpdate()
        {
            Refresh();
        }

        /// <summary>Kiểm camera có dưới nước không và bật/tắt hiệu ứng khi đổi trạng thái; gọi nhiều lần trong một khung không đổi gì thêm (WorldEdgeFog gọi trước khi ghi sương, brief 0012).</summary>
        public void Refresh()
        {
            bool under = false;
            if (terrain != null && terrain.World != null)
            {
                var p = terrain.transform.InverseTransformPoint(transform.position);
                under = FlyCameraCollision.IsUnderwater(terrain.World, p.x, p.y, p.z, BlockMesher.WaterSurfaceDrop);
            }
            if (under == IsUnderwater)
                return;
            if (under)
                Enter();
            else
                Restore();
        }

        /// <summary>Đặt tint; đang dưới nước thì ghi lại màu sương và nền ngay (brief 0023).</summary>
        public void ApplyTint(Color t)
        {
            tint = t;
            if (!IsUnderwater)
                return;
            RenderSettings.fogColor = CurrentFogColor;
            cam.backgroundColor = CurrentBackground;
        }

        void OnDisable()
        {
            if (IsUnderwater)
                Restore();
        }

        void Enter()
        {
            savedFogColor = RenderSettings.fogColor;
            savedFogStart = RenderSettings.fogStartDistance;
            savedFogEnd = RenderSettings.fogEndDistance;
            savedClearFlags = cam.clearFlags;
            savedBackground = cam.backgroundColor;
            RenderSettings.fogColor = CurrentFogColor;
            RenderSettings.fogStartDistance = fogStart;
            RenderSettings.fogEndDistance = fogEnd;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = CurrentBackground;
            IsUnderwater = true;
        }

        void Restore()
        {
            RenderSettings.fogColor = savedFogColor;
            RenderSettings.fogStartDistance = savedFogStart;
            RenderSettings.fogEndDistance = savedFogEnd;
            cam.clearFlags = savedClearFlags;
            cam.backgroundColor = savedBackground;
            IsUnderwater = false;
        }
    }
}
