using System.Collections.Generic;
using UnityEngine;

namespace Game.Client
{
    /// <summary>
    /// Nền đại dương của scene TerrainView (0009, 0012; không phải khối nước thật): mặt biển hình vành khăn quanh map, ở
    /// y = waterLevel + 1 − 0,1 (ô y ≤ waterLevel là nước). Phần trong map được khoét — trong map, mặt nước vẽ theo từng ô
    /// nước (BlockMesher submesh 1) để vùng khô thấp hơn mực nước không hiện nước.
    /// </summary>
    [RequireComponent(typeof(MeshFilter))]
    public class OceanBackdrop : MonoBehaviour
    {
        /// <summary>Hạ mặt biển dưới mặt trên ô nước chừng này — bằng BlockMesher.WaterSurfaceDrop để mặt biển liền mặt nước trong map.</summary>
        public float belowWaterSurface = 0.1f;
        /// <summary>Mặt biển trải ra mỗi phía ngoài map (đơn vị); xa hơn đã chìm trong sương mù.</summary>
        public float extent = 400f;
        /// <summary>Kích thước ô lưới của mặt biển — đủ nhỏ để sương mù tính theo đỉnh không lệch.</summary>
        public float cellSize = 16f;

        void Awake()
        {
            var p = ConfigLoader.LoadTerrainParams();
            transform.position = new Vector3(0, p.waterLevel + 1 - belowWaterSurface, 0);
            transform.rotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            GetComponent<MeshFilter>().sharedMesh = BuildRing(p.sizeX, p.sizeZ, extent, cellSize);
        }

        /// <summary>Lưới ô vuông phủ [−extent, size + extent], bỏ các ô nằm trong map [0, sizeX] × [0, sizeZ].</summary>
        public static Mesh BuildRing(int sizeX, int sizeZ, float extent, float cellSize)
        {
            var verts = new List<Vector3>();
            var tris = new List<int>();
            int nx = Mathf.CeilToInt((sizeX + 2 * extent) / cellSize), nz = Mathf.CeilToInt((sizeZ + 2 * extent) / cellSize);
            // Lưới gióng theo gốc map để mép khoét trùng biên map.
            float x0 = -Mathf.Ceil(extent / cellSize) * cellSize, z0 = -Mathf.Ceil(extent / cellSize) * cellSize;
            for (int j = 0; j < nz; j++)
            for (int i = 0; i < nx; i++)
            {
                float ax = x0 + i * cellSize, az = z0 + j * cellSize, bx = ax + cellSize, bz = az + cellSize;
                // Cắt ô lưới theo biên map: phần nằm trong map bỏ, phần ngoài giữ (map có thể không chia hết cellSize).
                AddOutside(verts, tris, ax, az, bx, bz, sizeX, sizeZ);
            }
            var mesh = new Mesh { name = "Ocean ring" };
            mesh.indexFormat = verts.Count > 65535 ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16;
            mesh.SetVertices(verts);
            mesh.SetTriangles(tris, 0);
            var normals = new Vector3[verts.Count];
            for (int k = 0; k < normals.Length; k++) normals[k] = Vector3.up;
            mesh.normals = normals;
            mesh.RecalculateBounds();
            return mesh;
        }

        static void AddOutside(List<Vector3> verts, List<int> tris, float ax, float az, float bx, float bz, int sizeX, int sizeZ)
        {
            // Hình chữ nhật ô trừ hình chữ nhật map → tối đa 4 dải (trái, phải, dưới, trên).
            float ix0 = Mathf.Max(ax, 0), ix1 = Mathf.Min(bx, sizeX), iz0 = Mathf.Max(az, 0), iz1 = Mathf.Min(bz, sizeZ);
            if (ix0 >= ix1 || iz0 >= iz1)
            {
                AddQuad(verts, tris, ax, az, bx, bz);
                return;
            }
            if (ax < ix0) AddQuad(verts, tris, ax, az, ix0, bz);
            if (bx > ix1) AddQuad(verts, tris, ix1, az, bx, bz);
            if (az < iz0) AddQuad(verts, tris, ix0, az, ix1, iz0);
            if (bz > iz1) AddQuad(verts, tris, ix0, iz1, ix1, bz);
        }

        static void AddQuad(List<Vector3> verts, List<int> tris, float ax, float az, float bx, float bz)
        {
            int i = verts.Count;
            verts.Add(new Vector3(ax, 0, az));
            verts.Add(new Vector3(ax, 0, bz));
            verts.Add(new Vector3(bx, 0, bz));
            verts.Add(new Vector3(bx, 0, az));
            // Mặt hướng lên (+y): thứ tự theo chiều kim đồng hồ khi nhìn từ trên.
            tris.Add(i); tris.Add(i + 1); tris.Add(i + 2);
            tris.Add(i); tris.Add(i + 2); tris.Add(i + 3);
        }
    }
}
