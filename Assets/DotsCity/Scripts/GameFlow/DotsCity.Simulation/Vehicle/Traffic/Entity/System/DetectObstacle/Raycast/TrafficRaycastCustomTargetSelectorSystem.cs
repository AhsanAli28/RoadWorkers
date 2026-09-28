using Spirit604.DotsCity.Simulation.Car;
using Unity.Burst;
using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Traffic
{
    [UpdateInGroup(typeof(MainThreadEventGroup))]
    [RequireMatchingQueriesForUpdate]
    [BurstCompile]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial struct TrafficRaycastEnableCustomTargetSelectorSystem : ISystem
    {
        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var commandBuffer = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);

            foreach (var (comp1, comp2, entity) in
                SystemAPI.Query<RefRO<HasDriverTag>, RefRO<TrafficTag>>()
                .WithNone<TrafficCustomRaycastTargetTag>()
                .WithAny<TrafficBackwardMovementTag>()
                .WithEntityAccess())
            {
                commandBuffer.AddComponent<TrafficCustomRaycastTargetTag>(entity);
            }
        }
    }

    [UpdateInGroup(typeof(MainThreadEventGroup))]
    [RequireMatchingQueriesForUpdate]
    [BurstCompile]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial struct TrafficRaycastDisableCustomTargetSelectorSystem1 : ISystem
    {
        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var commandBuffer = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);

            foreach (var (comp1, comp2, entity) in
                SystemAPI.Query<RefRO<TrafficCustomRaycastTargetTag>, RefRO<TrafficBackwardMovementTag>>()
                .WithNone<HasDriverTag>()
                .WithAll<TrafficTag>()
                .WithEntityAccess())
            {
                commandBuffer.RemoveComponent<TrafficCustomRaycastTargetTag>(entity);
            }
        }
    }

    [UpdateInGroup(typeof(MainThreadEventGroup))]
    [RequireMatchingQueriesForUpdate]
    [BurstCompile]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial struct TrafficRaycastDisableCustomTargetSelectorSystem2 : ISystem
    {
        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var commandBuffer = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);

            foreach (var (comp1, comp2, entity) in
                SystemAPI.Query<RefRO<TrafficCustomRaycastTargetTag>, RefRO<TrafficTag>>()
                .WithDisabled<TrafficBackwardMovementTag>()
                .WithEntityAccess())
            {
                commandBuffer.RemoveComponent<TrafficCustomRaycastTargetTag>(entity);
            }
        }
    }
}