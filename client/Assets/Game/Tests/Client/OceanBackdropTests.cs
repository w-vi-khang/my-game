using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Game.Client.Tests
{
    /// <summary>Nền đại dương (0009, scene TerrainView): mặt biển ngay dưới mặt nước của world, căn giữa map.</summary>
    public class OceanBackdropTests
    {
        [UnityTest]
        public IEnumerator OceanSitsJustBelowWaterSurfaceAtMapCenter()
        {
            var p = ConfigLoader.LoadTerrainParams();
            var go = new GameObject("Ocean test");
            try
            {
                var ocean = go.AddComponent<OceanBackdrop>();
                yield return null;
                float surface = p.waterLevel + 1; // ô y ≤ waterLevel là nước
                Assert.AreEqual(surface - ocean.belowWaterSurface, go.transform.position.y, 1e-5f);
                Assert.Less(go.transform.position.y, surface, "mặt biển không được phủ bãi cát ở mép nước");
                Assert.AreEqual(p.sizeX * 0.5f, go.transform.position.x, 1e-5f);
                Assert.AreEqual(p.sizeZ * 0.5f, go.transform.position.z, 1e-5f);
            }
            finally
            {
                Object.Destroy(go);
            }
        }
    }
}
