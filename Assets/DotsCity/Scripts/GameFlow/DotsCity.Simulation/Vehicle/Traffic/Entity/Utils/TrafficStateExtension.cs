using Spirit604.Extensions;
using System.Runtime.CompilerServices;
using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Traffic
{
    public static class TrafficStateExtension
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool AddIdleState<T>(ref EntityCommandBuffer commandBuffer, Entity entity, ref TrafficStateComponent trafficStateComponent, ref EnabledRefRW<TrafficIdleTag> trafficIdleTagRW, TrafficIdleState trafficIdleState) where T : unmanaged, IComponentData
        {
            if (AddIdleState(ref trafficStateComponent, ref trafficIdleTagRW, trafficIdleState))
            {
                commandBuffer.AddComponent<T>(entity);
                return true;
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool AddIdleState<T>(ref EntityCommandBuffer.ParallelWriter commandBuffer, int sortKey, Entity entity, ref TrafficStateComponent trafficStateComponent, ref EnabledRefRW<TrafficIdleTag> trafficIdleTagRW, TrafficIdleState trafficIdleState) where T : unmanaged, IComponentData
        {
            if (AddIdleState(ref trafficStateComponent, ref trafficIdleTagRW, trafficIdleState))
            {
                commandBuffer.AddComponent<T>(sortKey, entity);
                return true;
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool AddIdleState<T>(ref EntityCommandBuffer commandBuffer, Entity entity, ref TrafficStateComponent trafficStateComponent, TrafficIdleState trafficIdleState) where T : unmanaged, IComponentData
        {
            if (AddIdleState(ref commandBuffer, entity, ref trafficStateComponent, trafficIdleState))
            {
                commandBuffer.AddComponent<T>(entity);
                return true;
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool AddIdleState(ref TrafficStateComponent trafficStateComponent, ref EnabledRefRW<TrafficIdleTag> trafficIdleTagRW, TrafficIdleState trafficIdleState)
        {
            if (DotsEnumExtension.HasFlagUnsafe(trafficStateComponent.TrafficIdleState, trafficIdleState))
            {
                return false;
            }

            var addComponent = trafficStateComponent.TrafficIdleState == TrafficIdleState.Default;

            trafficStateComponent.TrafficIdleState = DotsEnumExtension.AddFlag(trafficStateComponent.TrafficIdleState, trafficIdleState);

            if (addComponent)
            {
                trafficIdleTagRW.ValueRW = true;
            }

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool AddIdleState(ref EntityCommandBuffer commandBuffer, Entity entity, ref TrafficStateComponent trafficStateComponent, TrafficIdleState trafficIdleState)
        {
            if (DotsEnumExtension.HasFlagUnsafe(trafficStateComponent.TrafficIdleState, trafficIdleState))
            {
                return false;
            }

            var addComponent = trafficStateComponent.TrafficIdleState == TrafficIdleState.Default;

            trafficStateComponent.TrafficIdleState = DotsEnumExtension.AddFlag(trafficStateComponent.TrafficIdleState, trafficIdleState);

            if (addComponent)
            {
                commandBuffer.SetComponentEnabled<TrafficIdleTag>(entity, true);
            }

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool RemoveIdleState<T>(ref EntityCommandBuffer commandBuffer, Entity entity, ref TrafficStateComponent trafficStateComponent, ref EnabledRefRW<TrafficIdleTag> trafficIdleTagRW, TrafficIdleState trafficIdleState) where T : unmanaged, IComponentData
        {
            if (RemoveIdleState(ref trafficStateComponent, ref trafficIdleTagRW, trafficIdleState))
            {
                commandBuffer.RemoveComponent<T>(entity);
                return true;
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool RemoveIdleState<T>(ref EntityCommandBuffer commandBuffer, Entity entity, ref TrafficStateComponent trafficStateComponent, TrafficIdleState trafficIdleState) where T : unmanaged, IComponentData
        {
            if (RemoveIdleState(ref commandBuffer, entity, ref trafficStateComponent, trafficIdleState))
            {
                commandBuffer.RemoveComponent<T>(entity);
                return true;
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool RemoveIdleState<T>(ref EntityCommandBuffer.ParallelWriter commandBuffer, int sortKey, Entity entity, ref TrafficStateComponent trafficStateComponent, ref EnabledRefRW<TrafficIdleTag> trafficIdleTagRW, TrafficIdleState trafficIdleState) where T : unmanaged, IComponentData
        {
            if (RemoveIdleState(ref trafficStateComponent, ref trafficIdleTagRW, trafficIdleState))
            {
                commandBuffer.RemoveComponent<T>(sortKey, entity);
                return true;
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool RemoveIdleState(ref TrafficStateComponent trafficStateComponent, ref EnabledRefRW<TrafficIdleTag> trafficIdleTagRW, TrafficIdleState trafficIdleState)
        {
            if (!DotsEnumExtension.HasFlagUnsafe(trafficStateComponent.TrafficIdleState, trafficIdleState))
            {
                return false;
            }

            trafficStateComponent.TrafficIdleState = DotsEnumExtension.RemoveFlag(trafficStateComponent.TrafficIdleState, trafficIdleState);

            if (trafficStateComponent.TrafficIdleState == TrafficIdleState.Default)
            {
                trafficIdleTagRW.ValueRW = false;
            }

            return true;
        }

        public static bool RemoveIdleState(ref EntityCommandBuffer commandBuffer, Entity entity, ref TrafficStateComponent trafficStateComponent, TrafficIdleState trafficIdleState)
        {
            if (!DotsEnumExtension.HasFlagUnsafe(trafficStateComponent.TrafficIdleState, trafficIdleState))
            {
                return false;
            }

            trafficStateComponent.TrafficIdleState = DotsEnumExtension.RemoveFlag(trafficStateComponent.TrafficIdleState, trafficIdleState);

            if (trafficStateComponent.TrafficIdleState == TrafficIdleState.Default)
            {
                commandBuffer.SetComponentEnabled<TrafficIdleTag>(entity, false);
            }

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool AddIdleState(EntityManager entityManager, Entity entity, TrafficIdleState trafficIdleState)
        {
            var trafficStateComponent = entityManager.GetComponentData<TrafficStateComponent>(entity);

            if (DotsEnumExtension.HasFlagUnsafe(trafficStateComponent.TrafficIdleState, trafficIdleState))
            {
                return false;
            }

            var addComponent = trafficStateComponent.TrafficIdleState == TrafficIdleState.Default;

            trafficStateComponent.TrafficIdleState = DotsEnumExtension.AddFlag(trafficStateComponent.TrafficIdleState, trafficIdleState);

            if (addComponent)
            {
                entityManager.SetComponentEnabled<TrafficIdleTag>(entity, true);
            }

            entityManager.SetComponentData(entity, trafficStateComponent);

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool RemoveIdleState(EntityManager entityManager, Entity entity, TrafficIdleState trafficIdleState)
        {
            var trafficStateComponent = entityManager.GetComponentData<TrafficStateComponent>(entity);

            if (!DotsEnumExtension.HasFlagUnsafe(trafficStateComponent.TrafficIdleState, trafficIdleState))
            {
                return false;
            }

            trafficStateComponent.TrafficIdleState = DotsEnumExtension.RemoveFlag(trafficStateComponent.TrafficIdleState, trafficIdleState);

            if (trafficStateComponent.TrafficIdleState == TrafficIdleState.Default)
            {
                entityManager.SetComponentEnabled<TrafficIdleTag>(entity, false);
            }

            entityManager.SetComponentData(entity, trafficStateComponent);
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool HasIdleState(in this TrafficStateComponent trafficStateComponent, TrafficIdleState trafficIdleState)
        {
            return DotsEnumExtension.HasFlagUnsafe(trafficStateComponent.TrafficIdleState, trafficIdleState);
        }
    }
}