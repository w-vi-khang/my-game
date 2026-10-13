using UnityEngine;

namespace Game.Client
{
    /// <summary>
    /// Nền đại dương của scene TerrainView (0009; không phải khối nước thật): mặt phẳng lớn đặt ngay dưới mặt nước của world
    /// (ô y ≤ waterLevel là nước → mặt nước ở y = waterLevel + 1) và căn giữa map. Hồ trong map cũng hiện nước nhờ mặt này.
    /// </summary>
    public class OceanBackdrop : MonoBehaviour
    {
        /// <summary>Hạ mặt biển dưới mặt nước một chút để bãi cát ở mép nước không bị phủ.</summary>
        public float belowWaterSurface = 0.1f;

        void Awake()
        {
            var p = ConfigLoader.LoadTerrainParams();
            transform.position = new Vector3(p.sizeX * 0.5f, p.waterLevel + 1 - belowWaterSurface, p.sizeZ * 0.5f);
        }
    }
}
