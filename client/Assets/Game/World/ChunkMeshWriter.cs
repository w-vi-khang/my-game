using System;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game.World
{
    /// <summary>
    /// Ghi ChunkMesh vào UnityEngine.Mesh bằng Mesh.AllocateWritableMeshData → ApplyAndDisposeWritableMeshData,
    /// chỉ số 16 bit (world-tech "Dựng mesh bề mặt mịn"; 32 bit không chắc chạy trên mọi máy).
    /// </summary>
    public static class ChunkMeshWriter
    {
        public const int MaxVertices16 = ushort.MaxValue;

        public static void Write(ChunkMesh chunk, Mesh mesh)
        {
            if (chunk.Vertices.Length > MaxVertices16)
                throw new InvalidOperationException($"Chunk có {chunk.Vertices.Length} đỉnh > {MaxVertices16} (giới hạn chỉ số 16 bit)");

            var array = Mesh.AllocateWritableMeshData(1);
            var data = array[0];
            data.SetVertexBufferParams(chunk.Vertices.Length,
                new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3, 0),
                new VertexAttributeDescriptor(VertexAttribute.Normal, VertexAttributeFormat.Float32, 3, 1));
            data.GetVertexData<float3>(0).CopyFrom(chunk.Vertices);
            data.GetVertexData<float3>(1).CopyFrom(chunk.Normals);

            data.SetIndexBufferParams(chunk.Indices.Length, IndexFormat.UInt16);
            var indices = data.GetIndexData<ushort>();
            for (int i = 0; i < chunk.Indices.Length; i++)
                indices[i] = (ushort)chunk.Indices[i];

            data.subMeshCount = 1;
            data.SetSubMesh(0, new SubMeshDescriptor(0, chunk.Indices.Length));
            Mesh.ApplyAndDisposeWritableMeshData(array, mesh);
            mesh.RecalculateBounds();
        }
    }
}
