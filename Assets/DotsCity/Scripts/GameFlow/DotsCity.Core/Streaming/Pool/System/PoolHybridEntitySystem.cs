using Spirit604.Extensions;
using Unity.Burst;
using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.Core
{
    [UpdateInGroup(typeof(DestroyGroup), OrderFirst = true)]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial struct PoolHybridEntitySystem : ISystem
    {
        private EntityQuery updateQuery;

        [BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            updateQuery = SystemAPI.QueryBuilder()
                .WithAny<PooledEventTag, CulledEventTag>()
                .WithAll<WorldEntitySharedType, PoolableTag>()
                .Build();

            updateQuery.SetSharedComponentFilter(new WorldEntitySharedType(EntityWorldType.HybridEntity));

            state.RequireForUpdate(updateQuery);
        }

        void ISystem.OnUpdate(ref SystemState state)
        {
            var commandBuffer = SystemAPI.GetSingleton<BeginInitializationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);

            foreach (var (transform, entity) in SystemAPI.Query<SystemAPI.ManagedAPI.UnityEngineComponent<Transform>>()
                .WithAny<PooledEventTag, CulledEventTag>()
                .WithAll<WorldEntitySharedType, PoolableTag>()
                .WithSharedComponentFilter(new WorldEntitySharedType { EntityWorldType = EntityWorldType.HybridEntity })
                .WithEntityAccess())
            {
                transform.Value.gameObject.ReturnToPool();
                commandBuffer.DestroyEntity(entity);
            }
        }
    }
}