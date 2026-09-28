using Unity.Burst;
using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Traffic
{
    [UpdateInGroup(typeof(MainThreadEventGroup))]
    [BurstCompile]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial struct TrafficEnableCustomMovementStateSystem : ISystem
    {
        private EntityQuery updateGroup;

        void ISystem.OnCreate(ref SystemState state)
        {
            updateGroup = SystemAPI.QueryBuilder()
                .WithAny<TrafficRailMovementTag, TrafficAccurateAligmentCustomMovementTag>()
                .WithDisabledRW<TrafficCustomMovementTag>()
                .Build();

            state.RequireForUpdate(updateGroup);
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            foreach ((var tag, Entity entity) in SystemAPI.Query<EnabledRefRW<TrafficCustomMovementTag>>()
                .WithAny<TrafficRailMovementTag, TrafficAccurateAligmentCustomMovementTag>()
                .WithDisabled<TrafficCustomMovementTag>()
                .WithEntityAccess())
            {
                tag.ValueRW = true;
            }
        }
    }
}