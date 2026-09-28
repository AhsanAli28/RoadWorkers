using Spirit604.DotsCity.Simulation.Traffic;
using Spirit604.DotsCity.Simulation.Traffic.Obstacle;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

namespace Spirit604.DotsCity.Simulation.TrafficPublic
{
    [UpdateInGroup(typeof(MainThreadEventGroup))]
    [BurstCompile]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial struct TrafficWaitForChangeLaneEventSystem : ISystem
    {
        private EntityQuery updateQuery;

        [BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            updateQuery = SystemAPI.QueryBuilder()
                .WithDisabled<TrafficChangingLaneEventTag>()
                .WithAllRW<TrafficDestinationComponent>()
                .WithAllRW<TrafficPathComponent, TrafficChangeLaneComponent>()
                .WithAllRW<TrafficTargetDirectionComponent, TrafficStateComponent>()
                .WithPresentRW<TrafficIdleTag>()
                .WithAll<TrafficWaitForChangeLaneTag, TrafficChangeLaneRequestedPositionComponent, LocalTransform>()
                .Build();

            state.RequireForUpdate(updateQuery);
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var waitForChangeLaneJob = new WaitForChangeLaneJob()
            {
                CommandBuffer = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged),
                ObstacleSystemSingleton = SystemAPI.GetSingleton<TrafficObstacleSystem.Singleton>(),
                TrafficCommonSettingsConfigBlobReference = SystemAPI.GetSingleton<TrafficCommonSettingsConfigBlobReference>(),
                TrafficChangeLaneConfigReference = SystemAPI.GetSingleton<TrafficChangeLaneConfigReference>(),
            };

            waitForChangeLaneJob.Run(updateQuery);
        }

        [WithDisabled(typeof(TrafficChangingLaneEventTag))]
        [WithAll(typeof(TrafficWaitForChangeLaneTag))]
        [BurstCompile]
        public partial struct WaitForChangeLaneJob : IJobEntity
        {
            public EntityCommandBuffer CommandBuffer;

            [ReadOnly]
            public TrafficObstacleSystem.Singleton ObstacleSystemSingleton;

            [ReadOnly]
            public TrafficCommonSettingsConfigBlobReference TrafficCommonSettingsConfigBlobReference;

            [ReadOnly]
            public TrafficChangeLaneConfigReference TrafficChangeLaneConfigReference;

            void Execute(
                Entity entity,
                ref TrafficDestinationComponent destinationComponent,
                ref TrafficPathComponent trafficPathComponent,
                ref TrafficChangeLaneComponent trafficChangeLaneComponent,
                ref TrafficTargetDirectionComponent trafficTargetDirectionComponent,
                ref TrafficStateComponent trafficStateComponent,
                EnabledRefRW<TrafficChangingLaneEventTag> trafficChangingLaneEventTagRW,
                EnabledRefRW<TrafficIdleTag> trafficIdleTagRW,
                in TrafficChangeLaneRequestedPositionComponent trafficChangeLaneRequestedPositionComponent,
                in LocalTransform transform)
            {
                ref var config = ref TrafficChangeLaneConfigReference.Config;
                ref var carHashMap = ref ObstacleSystemSingleton.CarHashMap;

                var canChange = TrafficChangeLaneUtils.CalcTargetLaneObstacles(
                     entity,
                     trafficChangeLaneRequestedPositionComponent.Destination,
                     trafficChangeLaneRequestedPositionComponent.TargetPathKey,
                     trafficChangeLaneRequestedPositionComponent.TargetPathNodeIndex,
                     trafficChangeLaneRequestedPositionComponent.TargetSourceLaneEntity,
                     TrafficCommonSettingsConfigBlobReference.Reference.Value.DefaultLaneSpeed,
                     ref destinationComponent,
                     ref trafficPathComponent,
                     ref trafficChangeLaneComponent,
                     ref trafficTargetDirectionComponent,
                     in transform,
                     in carHashMap,
                     in config);

                if (canChange)
                {
                    CommandBuffer.RemoveComponent<TrafficChangeLaneRequestedPositionComponent>(entity);
                    trafficChangingLaneEventTagRW.ValueRW = true;
                    TrafficStateExtension.RemoveIdleState<TrafficWaitForChangeLaneTag>(ref CommandBuffer, entity, ref trafficStateComponent, ref trafficIdleTagRW, TrafficIdleState.WaitForChangeLane);
                }
            }
        }
    }
}