using Spirit604.DotsCity.Simulation.Train;
using Unity.Burst;
using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Traffic
{
    [UpdateAfter(typeof(TrafficEnableCustomMovementStateSystem))]
    [UpdateInGroup(typeof(MainThreadEventGroup))]
    [BurstCompile]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial struct TrafficDisableCustomMovementStateSystem : ISystem
    {
        private EntityQuery updateGroup;

        void ISystem.OnCreate(ref SystemState state)
        {
            updateGroup = SystemAPI.QueryBuilder()
                .WithNone<TrafficRailMovementTag, TrafficAccurateAligmentCustomMovementTag, TrainTag>()
                .WithAllRW<TrafficCustomMovementTag>()
                .Build();

            state.RequireForUpdate(updateGroup);
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            foreach ((var tag, Entity entity) in SystemAPI.Query<EnabledRefRW<TrafficCustomMovementTag>>()
                .WithNone<TrafficRailMovementTag, TrafficAccurateAligmentCustomMovementTag, TrainTag>()
                .WithEntityAccess())
            {
                tag.ValueRW = false;
            }
        }
    }
}