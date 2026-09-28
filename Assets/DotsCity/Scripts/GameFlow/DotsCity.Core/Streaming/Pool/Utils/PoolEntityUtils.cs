using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Entities;

namespace Spirit604.DotsCity.Core
{
    public static class PoolEntityUtils
    {
        public static void AddPoolComponents(IBaker baker, Entity entity, EntityWorldType entityWorldType)
        {
            baker.AddSharedComponent(entity, new WorldEntitySharedType(entityWorldType));

            AddPoolComponents(baker, entity);
        }

        public static void AddPoolComponents(IBaker baker, Entity entity)
        {
            baker.AddComponent(entity, typeof(PoolableTag));
            baker.AddComponent(entity, typeof(PooledEventTag));

            baker.SetComponentEnabled<PooledEventTag>(entity, false);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AddPoolComponents(ref EntityCommandBuffer commandBuffer, Entity entity)
        {
            commandBuffer.AddComponent<PoolableTag>(entity);
            commandBuffer.AddComponent<PooledEventTag>(entity);

            commandBuffer.SetComponentEnabled<PooledEventTag>(entity, false);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AddPoolComponents(ref EntityCommandBuffer commandBuffer, Entity entity, EntityWorldType entityWorldType)
        {
            commandBuffer.AddSharedComponent(entity, new WorldEntitySharedType(entityWorldType));

            AddPoolComponents(ref commandBuffer, entity);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void DestroyEntity(
            ref EntityCommandBuffer.ParallelWriter commandBuffer,
            int entityInQueryIndex,
            Entity entity)
        {
            commandBuffer.SetComponentEnabled<PooledEventTag>(entityInQueryIndex, entity, true);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void DestroyEntity(
            in ArchetypeChunk chunk,
            ref ComponentTypeHandle<PooledEventTag> pooledEventHandleType,
            int entityIndexInChunk)
        {
            chunk.SetComponentEnabled(ref pooledEventHandleType, entityIndexInChunk, true);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void DestroyEntity(
            ref EntityCommandBuffer commandBuffer,
            Entity entity)
        {
            commandBuffer.SetComponentEnabled<PooledEventTag>(entity, true);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void DestroyEntity(
            ref EnabledRefRW<PooledEventTag> pooledEventTagRW)
        {
            pooledEventTagRW.ValueRW = true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void DestroyEntity(
            ref EntityCommandBuffer commandBuffer,
            NativeArray<Entity> entities)
        {
            for (int i = 0; i < entities.Length; i++)
            {
                commandBuffer.SetComponentEnabled<PooledEventTag>(entities[i], true);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void DestroyEntity(
            ref EntityManager entityManager,
            Entity entity)
        {
            entityManager.SetComponentEnabled<PooledEventTag>(entity, true);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void DestroyEntity(
            ref EntityManager entityManager,
            NativeArray<Entity> entities)
        {
            for (int i = 0; i < entities.Length; i++)
            {
                entityManager.SetComponentEnabled<PooledEventTag>(entities[i], true);
            }
        }
    }
}
