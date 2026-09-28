using Unity.Burst;
using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Road
{
    [UpdateInGroup(typeof(MainThreadEventGroup), OrderLast = true)]
    [BurstCompile]
    public partial struct TrafficNodeAvailableSystem : ISystem
    {
        private EntityQuery updateGroup;

        [BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            updateGroup = SystemAPI.QueryBuilder()
                .WithPresentRW<TrafficNodeAvailableTag>()
                .WithAll<TrafficNodeAvailableComponent, TrafficNodeCapacityComponent>()
                .Build();

            state.RequireForUpdate(updateGroup);
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var availableNodeJob = new AvailableNodeJob()
            {
            };

            availableNodeJob.Run(updateGroup);
        }

        [WithChangeFilter(typeof(TrafficNodeAvailableComponent))]
        [WithChangeFilter(typeof(TrafficNodeCapacityComponent))]
        [BurstCompile]
        private partial struct AvailableNodeJob : IJobEntity
        {
            void Execute(
                EnabledRefRW<TrafficNodeAvailableTag> trafficNodeAvailableRW,
                in TrafficNodeAvailableComponent trafficNodeAvailableComponent,
                in TrafficNodeCapacityComponent trafficNodeCapacityComponent)
            {
                var currentAvailableState = trafficNodeAvailableComponent.IsAvailable && trafficNodeCapacityComponent.HasSlots();

                if (trafficNodeAvailableRW.ValueRW != currentAvailableState)
                {
                    trafficNodeAvailableRW.ValueRW = currentAvailableState;
                }
            }
        }
    }
}