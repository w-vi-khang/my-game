using Game.Core;
using Game.World;
using UnityEngine;

namespace Game.Client
{
    /// <summary>
    /// Camera dưới nước (world-tech bước 7b2, brief 0011): sương ngắn màu nước + nền màu nước (không lộ skybox ở chân trời);
    /// lên khỏi nước thì trả lại sương và nền cũ của scene.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class UnderwaterView : MonoBehaviour
    {
        public TerrainChunkView terrain;
        public Color fogColor = new Color(0.08f, 0.32f, 0.45f);
        public float fogStart = 0f;
        public float fogEnd = 40f;

        public bool IsUnderwater { get; private set; }

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
            RenderSettings.fogColor = fogColor;
            RenderSettings.fogStartDistance = fogStart;
            RenderSettings.fogEndDistance = fogEnd;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = fogColor;
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
