using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Game.Client.Tests
{
    /// <summary>Nền đại dương (0009, 0012): mặt biển vành khăn ngay dưới mặt nước, khoét đúng phần map.</summary>
    public class OceanBackdropTests
    {
        [UnityTest]
        public IEnumerator OceanRingSitsAtWaterSurfaceAndSkipsMap()
        {
            var p = ConfigLoader.LoadTerrainParams();
            var go = new GameObject("Ocean test", typeof(MeshFilter));
            try
            {
                var ocean = go.AddComponent<OceanBackdrop>();
                yield return null;
                float surface = p.waterLevel + 1; // ô y ≤ waterLevel là nước
                Assert.AreEqual(surface - ocean.belowWaterSurface, go.transform.position.y, 1e-5f);
                Assert.AreEqual(Game.World.BlockMesher.WaterSurfaceDrop, ocean.belowWaterSurface, 1e-6f, "liền với mặt nước trong map");

                var mesh = go.GetComponent<MeshFilter>().sharedMesh;
                Assert.IsNotNull(mesh);
                var v = mesh.vertices;
                var t = mesh.triangles;
                float area = 0;
                for (int i = 0; i < t.Length; i += 3)
                {
                    Vector3 a = v[t[i]], b = v[t[i + 1]], c = v[t[i + 2]];
                    Vector3 center = (a + b + c) / 3f;
                    bool insideMap = center.x > 0 && center.x < p.sizeX && center.z > 0 && center.z < p.sizeZ;
                    Assert.IsFalse(insideMap, $"tam giác mặt biển nằm trong map ở {center}");
                    Vector3 n = Vector3.Cross(b - a, c - a);
                    Assert.Greater(n.y, 0f, "mặt hướng lên");
                    area += n.magnitude * 0.5f;
                }
                float full = (p.sizeX + 2 * ocean.extent) * (p.sizeZ + 2 * ocean.extent);
                Assert.AreEqual(full - p.sizeX * p.sizeZ, area, full * 1e-4f, "phủ kín vành ngoài map");
            }
            finally
            {
                Object.Destroy(go);
            }
        }
    }
}
