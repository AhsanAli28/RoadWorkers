using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Simulation.Car;
using Spirit604.DotsCity.Simulation.Level.Streaming;
using Spirit604.DotsCity.Simulation.Road;
using Spirit604.DotsCity.Simulation.TrafficArea;
using Spirit604.Extensions;
using Spirit604.Gameplay.Road;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Traffic
{
    [UpdateInGroup(typeof(TrafficProcessNodeGroup))]
    [BurstCompile]
    public partial struct TrafficTargetAchievedSystem : ISystem
    {
        private EntityQuery updateQuery;

        [BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            updateQuery = SystemAPI.QueryBuilder()
                .WithNone<TrafficCustomDestinationComponent, TrafficNoTargetTag, TrafficChangingLaneEventTag>()
                .WithDisabledRW<TrafficSwitchTargetNodeRequestTag>()
                .WithAllRW<TrafficDestinationComponent, TrafficAchievedTag>()
                .WithAllRW<TrafficPathComponent, TrafficStateComponent>()
                .WithPresentRW<TrafficEnteredTriggerNodeTag, TrafficEnteringTriggerNodeTag>()
                .WithPresentRW<TrafficIdleTag>()
                .WithAll<TrafficTag, HasDriverTag, TrafficTypeComponent, LocalTransform, SpeedComponent>()
                .Build();

            state.RequireForUpdate(updateQuery);
            state.RequireForUpdate<TrafficNodeResolverSystem.RuntimePathDataRef>();
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var targetJob = new TargetJob()
            {
                CommandBuffer = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged),
                TrafficAreaNodeLookup = SystemAPI.GetComponentLookup<TrafficAreaNode>(true),
                CapacityLookup = SystemAPI.GetComponentLookup<TrafficNodeCapacityComponent>(true),
                NodeSettingsLookup = SystemAPI.GetComponentLookup<TrafficNodeSettingsComponent>(true),
                InViewOfCameraLookup = SystemAPI.GetComponentLookup<InViewOfCameraTag>(true),
                Graph = SystemAPI.GetSingleton<PathGraphSystem.Singleton>(),
                TrafficNavConfig = SystemAPI.GetSingleton<TrafficDestinationConfigReference>(),
                ParkingConfig = SystemAPI.GetSingleton<TrafficParkingConfigReference>(),
                TrafficChangeLaneConfigReference = SystemAPI.GetSingleton<TrafficChangeLaneConfigReference>(),
                TrafficCommonSettingsConfigBlobReference = SystemAPI.GetSingleton<TrafficCommonSettingsConfigBlobReference>(),
                TrafficGeneralSettingsReference = SystemAPI.GetSingleton<TrafficGeneralSettingsReference>(),
                TrafficDestinationConfigReference = SystemAPI.GetSingleton<TrafficDestinationConfigReference>(),
                RuntimePathDataRef = SystemAPI.GetSingleton<TrafficNodeResolverSystem.RuntimePathDataRef>(),
            };

            targetJob.Run(updateQuery);
        }

        [WithNone(typeof(TrafficCustomDestinationComponent), typeof(TrafficChangingLaneEventTag))]
        [WithAll(typeof(TrafficTag), typeof(HasDriverTag))]
        [BurstCompile]
        private partial struct TargetJob : IJobEntity
        {
            public EntityCommandBuffer CommandBuffer;

            [ReadOnly]
            public ComponentLookup<TrafficAreaNode> TrafficAreaNodeLookup;

            [ReadOnly]
            public ComponentLookup<TrafficNodeCapacityComponent> CapacityLookup;

            [ReadOnly]
            public ComponentLookup<TrafficNodeSettingsComponent> NodeSettingsLookup;

            [ReadOnly]
            public ComponentLookup<InViewOfCameraTag> InViewOfCameraLookup;

            [ReadOnly]
            public PathGraphSystem.Singleton Graph;

            [ReadOnly]
            public TrafficDestinationConfigReference TrafficNavConfig;

            [ReadOnly]
            public TrafficParkingConfigReference ParkingConfig;

            [ReadOnly]
            public TrafficChangeLaneConfigReference TrafficChangeLaneConfigReference;

            [ReadOnly]
            public TrafficCommonSettingsConfigBlobReference TrafficCommonSettingsConfigBlobReference;

            [ReadOnly]
            public TrafficGeneralSettingsReference TrafficGeneralSettingsReference;

            [ReadOnly]
            public TrafficDestinationConfigReference TrafficDestinationConfigReference;

            [ReadOnly]
            public TrafficNodeResolverSystem.RuntimePathDataRef RuntimePathDataRef;

            void Execute(
                Entity entity,
                [ChunkIndexInQuery] int entityInQueryIndex,
                ref TrafficDestinationComponent destinationComponent,
                ref TrafficStateComponent trafficStateComponent,
                ref TrafficPathComponent trafficPathComponent,
                in SpeedComponent speedComponent,
                EnabledRefRW<TrafficEnteredTriggerNodeTag> trafficEnteredTriggerNodeTagRW,
                EnabledRefRW<TrafficEnteringTriggerNodeTag> trafficEnteringTriggerNodeTagRW,
                EnabledRefRW<TrafficSwitchTargetNodeRequestTag> trafficSwitchTargetNodeRequestTagRW,
                EnabledRefRW<TrafficAchievedTag> trafficAchievedTagRW,
                EnabledRefRW<TrafficIdleTag> trafficIdleTagRW,
                in TrafficTypeComponent trafficTypeComponent,
                in LocalTransform transform)
            {
                trafficAchievedTagRW.ValueRW = false;
                var achieveState = destinationComponent.AchieveState;
                destinationComponent.AchieveState = AchieveState.Default;

                ref readonly var pathData = ref Graph.GetPathData(trafficPathComponent.CurrentGlobalPathIndex);

                var forward = math.mul(transform.Rotation, math.forward());
                var carPosition = transform.Position;

                var distanceToLocalTarget = destinationComponent.DistanceToWaypoint;

                switch (achieveState)
                {
                    case AchieveState.Success:
                        {
                            var lockAligment = trafficTypeComponent.TrafficGroup == TrafficGroupType.Tram;

                            TrafficTargetUtils.ProcessAchievedTarget(
                              ref CommandBuffer,
                              entity,
                              ref destinationComponent,
                              ref trafficStateComponent,
                              ref trafficEnteredTriggerNodeTagRW,
                              ref trafficEnteringTriggerNodeTagRW,
                              ref trafficSwitchTargetNodeRequestTagRW,
                              ref trafficIdleTagRW,
                              in NodeSettingsLookup,
                              in CapacityLookup,
                              in TrafficAreaNodeLookup,
                              in ParkingConfig,
                              lockAligment);

                            break;
                        }
                    case AchieveState.NoTarget:
                        {
                            TrafficNoTargetUtils.AddNoTarget(ref CommandBuffer, entity, ref trafficStateComponent, ref trafficIdleTagRW, in TrafficDestinationConfigReference);
                            break;
                        }
                    case AchieveState.ChangeLane:
                        {
                            var pathNodes = Graph.GetRouteNodes(in pathData);

                            var currentLocalPathNodeIndex = trafficPathComponent.LocalPathNodeIndex;
                            var newLocalPathNodeIndex = math.clamp(currentLocalPathNodeIndex + 1, 0, pathNodes.Length - 1);

                            currentLocalPathNodeIndex = newLocalPathNodeIndex - 1;
                            var pathNode = pathNodes[currentLocalPathNodeIndex];

                            var isAvailable = pathNode.IsAvailable(in trafficTypeComponent);

                            if (!isAvailable && pathData.ParallelCount > 0)
                            {
                                var parallelPathsIndexes = Graph.GetParallelPaths(in pathData);

                                for (int j = 0; j < parallelPathsIndexes.Length; j++)
                                {
                                    var parallelIndex = parallelPathsIndexes[j];
                                    ref readonly var parallelPathData = ref Graph.GetPathData(parallelIndex);

                                    isAvailable = parallelPathData.IsAvailable(in trafficTypeComponent);

                                    if (isAvailable)
                                    {
                                        var sourcePathEntity = RuntimePathDataRef.TryToGetSourceNode(parallelIndex);

                                        TrafficChangeLaneUtils.SetWaitForChangeLane(
                                            ref CommandBuffer,
                                            trafficPathComponent.CurrentGlobalPathIndex,
                                            parallelIndex,
                                            currentLocalPathNodeIndex,
                                            entity,
                                            ref trafficPathComponent,
                                            ref destinationComponent,
                                            ref trafficStateComponent,
                                            ref trafficIdleTagRW,
                                            in transform,
                                            sourcePathEntity,
                                            ref Graph,
                                            ref TrafficChangeLaneConfigReference,
                                            ref TrafficCommonSettingsConfigBlobReference,
                                            speedComponent.Value);

                                        return;
                                    }
                                }
                            }
                            break;
                        }
                    case AchieveState.Backward:
                        {
                            if (trafficPathComponent.PathDirection == PathForwardType.Forward)
                            {
                                var directionToNode = math.normalize(trafficPathComponent.DestinationWayPoint - carPosition).Flat();
                                float dot = math.dot(directionToNode, forward);
                                var isSidePoint = math.abs(dot) < 0.1f;

                                if (isSidePoint)
                                {
                                    var side = Vector3.SignedAngle(directionToNode, forward, Vector3.up) < 0 ? -1 : 1;
                                    var point = trafficPathComponent.DestinationWayPoint - math.normalize(trafficPathComponent.DestinationWayPoint - trafficPathComponent.PreviousDestination) * distanceToLocalTarget;

                                    TrafficCustomDestinationComponent customDestinationComponent = new TrafficCustomDestinationComponent()
                                    {
                                        Destination = point
                                    };

                                    CommandBuffer.AddComponent(entity, customDestinationComponent);
                                    return;
                                }
                            }

                            break;
                        }
                    case AchieveState.Cull:
                        {
                            if (!InViewOfCameraLookup.HasComponent(entity))
                            {
                                var carDirection = trafficPathComponent.PathDirection == PathForwardType.Forward ? forward : -forward;
                                var directionToNode = math.normalize(trafficPathComponent.DestinationWayPoint - carPosition).Flat();

                                float dot = math.dot(directionToNode, carDirection);
                                var cull = dot < 0f;

                                if (cull)
                                {
                                    PoolEntityUtils.DestroyEntity(ref CommandBuffer, entity);
                                    return;
                                }
                            }

                            break;
                        }
                }
            }
        }
    }
}