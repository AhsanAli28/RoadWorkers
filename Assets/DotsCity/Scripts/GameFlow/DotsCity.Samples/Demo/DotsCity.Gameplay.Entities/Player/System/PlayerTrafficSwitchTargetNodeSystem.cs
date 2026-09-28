using Spirit604.DotsCity.Simulation.Car;
using Spirit604.DotsCity.Simulation.Road;
using Spirit604.DotsCity.Simulation.Traffic;
using Spirit604.Extensions;
using Spirit604.Gameplay.Road;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Spirit604.DotsCity.Gameplay.Player
{
    [UpdateInGroup(typeof(StructuralSystemGroup))]
    [BurstCompile]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial struct PlayerTrafficSwitchTargetNodeSystem : ISystem
    {
        private EntityQuery updateQuery;

        [BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            updateQuery = SystemAPI.QueryBuilder()
                .WithAllRW<TrafficPathComponent, TrafficTargetDirectionComponent>()
                .WithAllRW<TrafficDestinationComponent, TrafficStateComponent>()
                .WithAllRW<SpeedComponent, TrafficSwitchTargetNodeRequestTag>()
                .WithPresentRW<TrafficEnteringTriggerNodeTag, TrafficNextTrafficNodeRequestTag>()
                .WithAll<TrafficPlayerControlTag, HasDriverTag, LocalTransform>()
                .Build();

            state.RequireForUpdate(updateQuery);
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var switchTrafficNodeJob = new SwitchTrafficNodeJob()
            {
                CommandBuffer = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged),
                TrafficNodeSettingsLookup = SystemAPI.GetComponentLookup<TrafficNodeSettingsComponent>(true),
                TrafficRailMovementLookup = SystemAPI.GetComponentLookup<TrafficRailMovementTag>(true),
                TrafficNoTargetLookup = SystemAPI.GetComponentLookup<TrafficNoTargetTag>(true),
                Graph = SystemAPI.GetSingleton<PathGraphSystem.Singleton>(),
                TrafficGeneralSettingsReference = SystemAPI.GetSingleton<TrafficGeneralSettingsReference>(),
                TrafficRoadConfigReference = SystemAPI.GetSingleton<TrafficRoadConfigReference>(),
            };

            switchTrafficNodeJob.Schedule(updateQuery);
        }

        [WithAll(typeof(TrafficPlayerControlTag), typeof(HasDriverTag))]
        [BurstCompile]
        private partial struct SwitchTrafficNodeJob : IJobEntity
        {
            public EntityCommandBuffer CommandBuffer;

            [ReadOnly]
            public ComponentLookup<TrafficNodeSettingsComponent> TrafficNodeSettingsLookup;

            [ReadOnly]
            public ComponentLookup<TrafficRailMovementTag> TrafficRailMovementLookup;

            [ReadOnly]
            public ComponentLookup<TrafficNoTargetTag> TrafficNoTargetLookup;

            [ReadOnly]
            public PathGraphSystem.Singleton Graph;

            [ReadOnly]
            public TrafficGeneralSettingsReference TrafficGeneralSettingsReference;

            [ReadOnly]
            public TrafficRoadConfigReference TrafficRoadConfigReference;

            private void Execute(
                Entity entity,
                ref TrafficPathComponent trafficPathComponent,
                ref TrafficTargetDirectionComponent trafficTargetDirectionComponent,
                ref TrafficDestinationComponent destinationComponent,
                ref TrafficStateComponent trafficStateComponent,
                ref SpeedComponent speedComponent,
                EnabledRefRW<TrafficSwitchTargetNodeRequestTag> trafficSwitchTargetNodeRequestTagRW,
                EnabledRefRW<TrafficNextTrafficNodeRequestTag> trafficNextTrafficNodeRequestTagRW,
                EnabledRefRW<TrafficEnteringTriggerNodeTag> trafficEnteringTriggerNodeTagRW,
                in LocalTransform transform)
            {
                var noTarget = TrafficNoTargetLookup.HasComponent(entity);

                if (destinationComponent.NextDestinationNode == Entity.Null)
                {
                    if (!noTarget)
                    {
                        TrafficStateExtension.AddIdleState<TrafficNoTargetTag>(ref CommandBuffer, entity, ref trafficStateComponent, TrafficIdleState.NoTarget);
                    }

                    return;
                }

                if (noTarget)
                {
                    TrafficStateExtension.RemoveIdleState<TrafficNoTargetTag>(ref CommandBuffer, entity, ref trafficStateComponent, TrafficIdleState.NoTarget);
                }

                trafficNextTrafficNodeRequestTagRW.ValueRW = true;

                var currentTargetEntity = destinationComponent.DestinationNode;
                var newTargetNodeEntity = destinationComponent.NextDestinationNode;

                var achievedGlobalPathIndex = destinationComponent.NextGlobalPathIndex;

                if (!TrafficNodeSettingsLookup.HasComponent(currentTargetEntity))
                {
                    return;
                }

                trafficSwitchTargetNodeRequestTagRW.ValueRW = false;

                destinationComponent.PreviousNode = destinationComponent.DestinationNode;
                destinationComponent.DestinationNode = newTargetNodeEntity;
                destinationComponent.NextDestinationNode = default;
                destinationComponent.NextGlobalPathIndex = -1;

                if (TrafficNodeSettingsLookup.HasComponent(newTargetNodeEntity))
                {
                    var dstSettings = TrafficNodeSettingsLookup[newTargetNodeEntity];

                    if ((dstSettings.TrafficNodeTypeFlag & TrafficRoadConfigReference.Config.Value.LinkedNodeFlags) != 0)
                    {
                        trafficEnteringTriggerNodeTagRW.ValueRW = true;
                    }
                }

                destinationComponent.ChangeLanePathIndex = destinationComponent.NextChangeLanePathIndex;

                ref readonly var selectedPathData = ref Graph.GetPathData(achievedGlobalPathIndex);
                bool isCurved = selectedPathData.PathCurveType != PathCurveType.StraightLine;
                bool isStraightRoad = selectedPathData.PathRoadType == PathRoadType.StraightRoad;

                float3 targetPosition = Graph.GetEndPosition(in selectedPathData);

                int direction = 0;

                if (isCurved)
                {
                    Vector3 directionToTargetWaypoint = math.normalize(targetPosition - transform.Position);

                    float signedAngle = Vector3.SignedAngle(transform.Forward(), directionToTargetWaypoint, Vector3.up);
                    direction = signedAngle > 0 ? 1 : -1;
                }

                trafficTargetDirectionComponent = new TrafficTargetDirectionComponent { Direction = direction };

                Vector3 targetWaypoint = default;
                Vector3 previousTargetWaypoint = default;

                var localPathNodeIndex = -1;
                float currentSpeedLimit = 0;

                var pathNodes = Graph.GetRouteNodes(in selectedPathData);

                if (destinationComponent.PathConnectionType == PathConnectionType.PathPoint)
                {
                    destinationComponent.NextDestinationNode = Entity.Null;
                    var connectionPosition = destinationComponent.Destination;
                    var minIndex = 0;
                    var maxIndex = pathNodes.Length;

                    localPathNodeIndex = -1;

                    for (int i = minIndex; i < maxIndex - 1; i++)
                    {
                        int currentIndex = i;
                        int nextIndex = i + 1;

                        bool containsPoint = VectorExtensions.IsBetween3DSpace(pathNodes[currentIndex].Position, pathNodes[nextIndex].Position, connectionPosition);

                        if (containsPoint)
                        {
                            localPathNodeIndex = currentIndex;
                            break;
                        }
                    }

                    if (localPathNodeIndex == -1)
                    {
#if UNITY_EDITOR
                        bool intersected = false;
#endif

                        ref readonly var sourcePathData = ref Graph.GetPathData(trafficPathComponent.CurrentGlobalPathIndex);
                        var intersectedPaths = Graph.GetIntersectedPaths(in sourcePathData);

                        for (int i = 0; i < intersectedPaths.Length; i++)
                        {
                            if (intersectedPaths[i].IntersectedPathIndex == trafficPathComponent.CurrentGlobalPathIndex)
                            {
                                localPathNodeIndex = intersectedPaths[i].LocalNodeIndex;

#if UNITY_EDITOR
                                intersected = true;
#endif

                                break;
                            }
                        }

                        if (localPathNodeIndex == -1)
                        {
                            localPathNodeIndex = selectedPathData.NodeCount - 2;
                        }

#if UNITY_EDITOR
                        UnityEngine.Debug.Log($"TrafficSwitchTargetNodeSystem. Source path {trafficPathComponent.CurrentGlobalPathIndex} Achieved Path {achievedGlobalPathIndex}. PathPoint not assigned properly. Target path intersected {intersected}");
#endif
                    }

                    currentSpeedLimit = pathNodes[localPathNodeIndex].SpeedLimit;
                    targetWaypoint = pathNodes[localPathNodeIndex + 1].Position;
                    previousTargetWaypoint = pathNodes[localPathNodeIndex].Position;
                    localPathNodeIndex = localPathNodeIndex + 1;
                }
                else
                {
                    // Start path from 0 index for switching in the traffic destination system,
                    // as the lane may have an unavailable traffic group type

                    localPathNodeIndex = 0;
                    targetWaypoint = pathNodes[0].Position;
                    previousTargetWaypoint = trafficPathComponent.DestinationWayPoint;
                    currentSpeedLimit = pathNodes[0].SpeedLimit;
                }

                destinationComponent.Destination = targetPosition;
                speedComponent.LaneLimit = currentSpeedLimit;

                trafficPathComponent = new TrafficPathComponent
                {
                    CurrentGlobalPathIndex = achievedGlobalPathIndex,
                    LocalPathNodeIndex = localPathNodeIndex,
                    DestinationWayPoint = targetWaypoint,
                    PreviousDestination = previousTargetWaypoint,
                    Priority = selectedPathData.Priority,
                };

                destinationComponent.PathConnectionType = destinationComponent.NextPathConnectionType;

                if (TrafficGeneralSettingsReference.Config.Value.RailSupport)
                {
                    var addRail = selectedPathData.HasOption(PathOptions.Rail);
                    var hasRail = TrafficRailMovementLookup.HasComponent(entity);

                    if (addRail)
                    {
                        if (!hasRail)
                        {
                            CommandBuffer.AddComponent<TrafficRailMovementTag>(entity);
                        }
                    }
                    else if (hasRail)
                    {
                        CommandBuffer.RemoveComponent<TrafficRailMovementTag>(entity);
                    }
                }
            }
        }
    }
}