using Spirit604.DotsCity.Core;
using System.Runtime.CompilerServices;
using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Traffic
{
    public static class TrafficNoTargetUtils
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AddNoTarget(
            ref EntityCommandBuffer CommandBuffer,
            Entity entity,
            ref TrafficStateComponent trafficStateComponent,
            ref EnabledRefRW<TrafficIdleTag> trafficIdleTagRW,
            in TrafficDestinationConfigReference TrafficTargetConfigReference)
        {
            switch (TrafficTargetConfigReference.Config.Value.NoDestinationReact)
            {
                case NoDestinationReactType.Idle:
                    TrafficStateExtension.AddIdleState<TrafficNoTargetTag>(ref CommandBuffer, entity, ref trafficStateComponent, ref trafficIdleTagRW, TrafficIdleState.NoTarget);
                    break;
                case NoDestinationReactType.DestroyVehicle:
                    PoolEntityUtils.DestroyEntity(ref CommandBuffer, entity);
                    break;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AddNoTarget(
            ref EntityCommandBuffer.ParallelWriter CommandBuffer,
            Entity entity,
            int entityInQueryIndex,
            ref TrafficStateComponent trafficStateComponent,
            ref EnabledRefRW<TrafficIdleTag> trafficIdleTagRW,
            in TrafficDestinationConfigReference TrafficTargetConfigReference)
        {
            switch (TrafficTargetConfigReference.Config.Value.NoDestinationReact)
            {
                case NoDestinationReactType.Idle:
                    TrafficStateExtension.AddIdleState<TrafficNoTargetTag>(ref CommandBuffer, entityInQueryIndex, entity, ref trafficStateComponent, ref trafficIdleTagRW, TrafficIdleState.NoTarget);
                    break;
                case NoDestinationReactType.DestroyVehicle:
                    PoolEntityUtils.DestroyEntity(ref CommandBuffer, entityInQueryIndex, entity);
                    break;
            }
        }
    }
}