using Spirit604.DotsCity.Simulation.Car;
using Spirit604.Extensions;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Spirit604.DotsCity.Simulation.Traffic
{
    [UpdateInGroup(typeof(PreEarlyJobGroup))]
    [BurstCompile]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial struct TrafficStartInitSystem : ISystem
    {
        private EntityQuery updateQuery;

        [BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            updateQuery = SystemAPI.QueryBuilder()
                .WithDisabled<TrafficSwitchTargetNodeRequestTag, TrafficNextTrafficNodeRequestTag>()
                .WithAll<TrafficTag, TrafficInitTag>()
                .Build();

            state.RequireForUpdate(updateQuery);
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var initJob = new InitJob()
            {
            };

            initJob.Schedule();
        }

        [WithDisabled(typeof(TrafficSwitchTargetNodeRequestTag), typeof(TrafficNextTrafficNodeRequestTag))]
        [WithAll(typeof(TrafficTag), typeof(TrafficInitTag))]
        [BurstCompile]
        private partial struct InitJob : IJobEntity
        {
            void Execute(
                EnabledRefRW<TrafficInitTag> trafficInitTagRW,
                in TrafficPathComponent trafficPathComponent,
                in TrafficDestinationComponent trafficDestinationComponent,
                in LocalTransform transform)
            {
                if ((trafficDestinationComponent.CurrentNode != trafficDestinationComponent.DestinationNode ||
                    trafficDestinationComponent.DestinationNode != trafficDestinationComponent.NextDestinationNode && trafficDestinationComponent.NextDestinationNode != Entity.Null) &&
                    !trafficPathComponent.PreviousDestination.Equals(trafficPathComponent.DestinationWayPoint) &&
                    !transform.Position.Flat().Equals(trafficPathComponent.DestinationWayPoint.Flat()) &&
                    !trafficPathComponent.DestinationWayPoint.Equals(float3.zero))
                {
                    trafficInitTagRW.ValueRW = false;
                }
            }
        }
    }
}