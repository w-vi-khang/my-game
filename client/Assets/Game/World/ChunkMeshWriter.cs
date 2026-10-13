using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game.World
{
    /// <summary>
    /// Ghi ChunkMesh vào UnityEngine.Mesh bằng Mesh.AllocateWritableMeshData → ApplyAndDisposeWritableMeshData,
    /// chỉ số 16 bit (world-tech "Dựng mesh khối bo cạnh"; 32 bit không chắc chạy trên mọi máy). Luồng 2 = màu đỉnh RGBA8.
    /// </summary>
    public static class ChunkMeshWriter
    {
        public const int MaxVertices16 = ushort.MaxValue;

        public static void Write(ChunkMesh chunk, Mesh mesh) => WriteAll(new[] { chunk }, new[] { mesh });

        /// <summary>Ghi nhiều chunk trong một lần xin MeshDataArray (khuyến nghị của docs Mesh.MeshData — world-tech "Chỉnh sửa").</summary>
        public static void WriteAll(IReadOnlyList<ChunkMesh> chunks, IReadOnlyList<Mesh> meshes)
        {
            if (chunks.Count != meshes.Count)
                throw new ArgumentException($"{chunks.Count} chunk nhưng {meshes.Count} mesh");
            for (int k = 0; k < chunks.Count; k++)
                if (chunks[k].Vertices.Length > MaxVertices16)
                    throw new InvalidOperationException($"Chunk có {chunks[k].Vertices.Length} đỉnh > {MaxVertices16} (giới hạn chỉ số 16 bit)");

            var array = Mesh.AllocateWritableMeshData(chunks.Count);
            for (int k = 0; k < chunks.Count; k++)
            {
                var chunk = chunks[k];
                var data = array[k];
                data.SetVertexBufferParams(chunk.Vertices.Length,
                    new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3, 0),
                    new VertexAttributeDescriptor(VertexAttribute.Normal, VertexAttributeFormat.Float32, 3, 1),
                    new VertexAttributeDescriptor(VertexAttribute.Color, VertexAttributeFormat.UNorm8, 4, 2));
                data.GetVertexData<float3>(0).CopyFrom(chunk.Vertices);
                data.GetVertexData<float3>(1).CopyFrom(chunk.Normals);
                data.GetVertexData<uint>(2).CopyFrom(chunk.Colors);

                // Submesh 0 = địa hình, submesh 1 = mặt nước (có thể rỗng) — MeshRenderer có 2 material.
                int water = chunk.WaterIndices?.Length ?? 0;
                data.SetIndexBufferParams(chunk.Indices.Length + water, IndexFormat.UInt16);
                var indices = data.GetIndexData<ushort>();
                for (int i = 0; i < chunk.Indices.Length; i++)
                    indices[i] = (ushort)chunk.Indices[i];
                for (int i = 0; i < water; i++)
                    indices[chunk.Indices.Length + i] = (ushort)chunk.WaterIndices[i];

                data.subMeshCount = 2;
                data.SetSubMesh(0, new SubMeshDescriptor(0, chunk.Indices.Length), MeshUpdateFlags.DontRecalculateBounds);
                data.SetSubMesh(1, new SubMeshDescriptor(chunk.Indices.Length, water), MeshUpdateFlags.DontRecalculateBounds);
            }
            var list = new List<Mesh>(meshes);
            Mesh.ApplyAndDisposeWritableMeshData(array, list);
            foreach (var mesh in list)
                mesh.RecalculateBounds();
        }

        /// <summary>Ghi mesh va chạm (mặt khối vuông, chỉ vị trí) của nhiều chunk trong một lần xin MeshDataArray.</summary>
        public static void WriteColliders(IReadOnlyList<ChunkMesh> chunks, IReadOnlyList<Mesh> meshes)
        {
            if (chunks.Count != meshes.Count)
                throw new ArgumentException($"{chunks.Count} chunk nhưng {meshes.Count} mesh");
            for (int k = 0; k < chunks.Count; k++)
                if (chunks[k].ColliderVertices.Length > MaxVertices16)
                    throw new InvalidOperationException($"Collider chunk có {chunks[k].ColliderVertices.Length} đỉnh > {MaxVertices16}");

            var array = Mesh.AllocateWritableMeshData(chunks.Count);
            for (int k = 0; k < chunks.Count; k++)
            {
                var chunk = chunks[k];
                var data = array[k];
                data.SetVertexBufferParams(chunk.ColliderVertices.Length,
                    new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3, 0));
                data.GetVertexData<float3>(0).CopyFrom(chunk.ColliderVertices);
                data.SetIndexBufferParams(chunk.ColliderIndices.Length, IndexFormat.UInt16);
                var indices = data.GetIndexData<ushort>();
                for (int i = 0; i < chunk.ColliderIndices.Length; i++)
                    indices[i] = (ushort)chunk.ColliderIndices[i];
                data.subMeshCount = 1;
                data.SetSubMesh(0, new SubMeshDescriptor(0, chunk.ColliderIndices.Length));
            }
            var list = new List<Mesh>(meshes);
            Mesh.ApplyAndDisposeWritableMeshData(array, list);
            foreach (var mesh in list)
                mesh.RecalculateBounds();
        }
    }
}
