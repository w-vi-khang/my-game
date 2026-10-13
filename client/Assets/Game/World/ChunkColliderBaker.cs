using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace Game.World
{
    /// <summary>
    /// Bake collider cho nhiều mesh chunk song song bằng Physics.BakeMesh trong job (world-tech "Va chạm";
    /// docs 6000.4: an toàn đa luồng, không bake cùng một mesh từ nhiều luồng, collider chỉ dùng lại kết quả bake khi cookingOptions khớp).
    /// </summary>
    public static class ChunkColliderBaker
    {
        /// <summary>Gán cho MeshCollider.cookingOptions để dùng lại kết quả bake (phải khớp với lúc bake).</summary>
        public const MeshColliderCookingOptions CookingOptions =
            MeshColliderCookingOptions.UseFastMidphase | MeshColliderCookingOptions.EnableMeshCleaning |
            MeshColliderCookingOptions.WeldColocatedVertices;

        struct BakeJob : IJobParallelFor
        {
            [ReadOnly] public NativeArray<EntityId> MeshIds;

            public void Execute(int index) => Physics.BakeMesh(MeshIds[index], false, CookingOptions);
        }

        /// <summary>Bake song song và chờ xong; mỗi mesh chỉ xuất hiện một lần trong danh sách.</summary>
        public static void BakeAll(IReadOnlyList<Mesh> meshes)
        {
            if (meshes.Count == 0)
                return;
            var ids = new NativeArray<EntityId>(meshes.Count, Allocator.TempJob);
            try
            {
                for (int i = 0; i < meshes.Count; i++)
                    ids[i] = meshes[i].GetEntityId();
                new BakeJob { MeshIds = ids }.Schedule(ids.Length, 1).Complete();
            }
            finally
            {
                ids.Dispose();
            }
        }
    }
}
