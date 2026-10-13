using Unity.Mathematics;

namespace Game.World
{
    /// <summary>Kết quả dựng mesh một chunk; toạ độ tính từ gốc chunk, đơn vị = 1 ô gameplay.</summary>
    public sealed class ChunkMesh
    {
        public float3[] Vertices;
        public float3[] Normals;
        /// <summary>Màu đỉnh sRGB, 4 byte RGBA gói trong uint (byte thấp = R) — shader Game/Terrain đổi sang linear.</summary>
        public uint[] Colors;
        public int[] Indices;
        /// <summary>Submesh 1: mặt nước theo ô (0012) — dùng chung mảng đỉnh, vẽ bằng material nước.</summary>
        public int[] WaterIndices;
        /// <summary>Mesh va chạm: mặt khối vuông chưa bo (decision 0011 "Collider") — khớp đúng lưới gameplay.</summary>
        public float3[] ColliderVertices;
        public int[] ColliderIndices;
        /// <summary>Tam giác địa hình (submesh 0, không tính mặt nước).</summary>
        public int TriangleCount => Indices.Length / 3;
        public int WaterTriangleCount => WaterIndices.Length / 3;
    }
}
