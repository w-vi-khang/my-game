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
        public int TriangleCount => Indices.Length / 3;
    }
}
