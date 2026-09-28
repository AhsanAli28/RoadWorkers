using Unity.Burst;
using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.TrafficArea
{
    [UpdateInGroup(typeof(TrafficAreaSimulationGroup))]
    [BurstCompile]
    public partial struct TrafficAreaObserveEnableSystem : ISystem
    {
        private EntityQuery updateQuery;

        [BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            updateQuery = SystemAPI.QueryBuilder()
                .WithDisabled<TrafficAreaCarObserverEnabledTag>()
                .WithAny<TrafficAreaProcessingEnterQueueTag, TrafficAreaProcessingExitQueueTag>()
                .WithAll<TrafficAreaComponent>()
                .Build();

            state.RequireForUpdate(updateQuery);
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            foreach (var (trafficAreaCarObserverEnabledTagRW, entity) in SystemAPI.Query<EnabledRefRW<TrafficAreaCarObserverEnabledTag>>()
                .WithDisabled<TrafficAreaCarObserverEnabledTag>()
                .WithAny<TrafficAreaProcessingEnterQueueTag, TrafficAreaProcessingExitQueueTag>()
                .WithAll<TrafficAreaComponent>()
                .WithEntityAccess())
            {
                trafficAreaCarObserverEnabledTagRW.ValueRW = true;
            }
        }
    }
}