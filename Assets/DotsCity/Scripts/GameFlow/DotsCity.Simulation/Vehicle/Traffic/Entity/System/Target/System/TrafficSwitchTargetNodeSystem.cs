using Spirit604.DotsCity.Simulation.Car;
using Spirit604.DotsCity.Simulation.Road;
using Spirit604.Extensions;
using Spirit604.Gameplay.Road;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

#if RUNTIME_ROAD
using Spirit604.DotsCity.Simulation.Level.Streaming;
using Spirit604.DotsCity.Simulation.Traffic.Obstacle;
#endif

namespace Spirit604.DotsCity.Simulation.Traffic
{
    [UpdateAfter(typeof(TrafficTargetAchievedSystem))]
    [UpdateInGroup(typeof(TrafficProcessNodeGroup))]
    [BurstCompile]
    public partial struct TrafficSwitchTargetNodeSystem : ISystem
    {
        private EntityQuery updateQuery;

#if RUNTIME_ROAD
        private EntityQuery parkingTrafficGroup;
#endif

        [BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
#if RUNTIME_ROAD
            parkingTrafficGroup = SystemAPI.QueryBuilder()
                .WithAll<TrafficTag, TrafficNodeLinkedComponent>()
                .Build();
#endif

            updateQuery = SystemAPI.QueryBuilder()
                .WithDisabledRW<TrafficNextTrafficNodeRequestTag>()
                .WithAllRW<TrafficPathComponent, TrafficTargetDirectionComponent>()
                .WithAllRW<TrafficDestinationComponent, TrafficStateComponent>()
                .WithAllRW<SpeedComponent, TrafficSwitchTargetNodeRequestTag>()
                .WithPresentRW<TrafficIdleTag, TrafficEnteringTriggerNodeTag>()
                .WithAll<TrafficDefaultTag, HasDriverTag, LocalTransform, TrafficTypeComponent>()
                .Build();

            state.RequireForUpdate(updateQuery);

#if RUNTIME_ROAD
            state.RequireForUpdate<TrafficNodeResolverSystem.RuntimePathDataRef>();
            state.RequireForUpdate<TrafficObstacleSystem.Singleton>();
#endif
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var switchTrafficNodeJob = new SwitchTrafficNodeJob()
            {
                CommandBuffer = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged),
                TrafficNodeSettingsLookup = SystemAPI.GetComponentLookup<TrafficNodeSettingsComponent>(true),
                TrafficRailMovementLookup = SystemAPI.GetComponentLookup<TrafficRailMovementTag>(true),
                AligmentLookup = SystemAPI.GetComponentLookup<TrafficAccurateAligmentCustomMovementTag>(true),
                Graph = SystemAPI.GetSingleton<PathGraphSystem.Singleton>(),
                TrafficChangeLaneConfigReference = SystemAPI.GetSingleton<TrafficChangeLaneConfigReference>(),
                TrafficCommonSettingsConfigBlobReference = SystemAPI.GetSingleton<TrafficCommonSettingsConfigBlobReference>(),
                TrafficDestinationConfigReference = SystemAPI.GetSingleton<TrafficDestinationConfigReference>(),
                TrafficGeneralSettingsReference = SystemAPI.GetSingleton<TrafficGeneralSettingsReference>(),
                TrafficRoadConfigReference = SystemAPI.GetSingleton<TrafficRoadConfigReference>(),
#if RUNTIME_ROAD
                TrafficNodeAvailableLookup = SystemAPI.GetComponentLookup<TrafficNodeAvailableComponent>(true),
                TrafficNodeCapacityLookup = SystemAPI.GetComponentLookup<TrafficNodeCapacityComponent>(true),
                RuntimePathDataRef = SystemAPI.GetSingleton<TrafficNodeResolverSystem.RuntimePathDataRef>(),
                TrafficSpawnerConfigBlobReference = SystemAPI.GetSingleton<TrafficSpawnerConfigBlobReference>(),
                TrafficObstacleSystemSingleton = SystemAPI.GetSingleton<TrafficObstacleSystem.Singleton>(),
                CurrentParkingCount = parkingTrafficGroup.CalculateEntityCount(),
                Timestamp = (float)SystemAPI.Time.ElapsedTime
#endif
            };

            switchTrafficNodeJob.Run(updateQuery);
        }

        [WithAll(typeof(TrafficDefaultTag), typeof(HasDriverTag))]
        [BurstCompile]
        private partial struct SwitchTrafficNodeJob : IJobEntity
        {
            public EntityCommandBuffer CommandBuffer;

            [ReadOnly]
            public ComponentLookup<TrafficNodeSettingsComponent> TrafficNodeSettingsLookup;

            [ReadOnly]
            public ComponentLookup<TrafficRailMovementTag> TrafficRailMovementLookup;

            [ReadOnly]
            public ComponentLookup<TrafficAccurateAligmentCustomMovementTag> AligmentLookup;

            [ReadOnly]
            public PathGraphSystem.Singleton Graph;

            [ReadOnly]
            public TrafficChangeLaneConfigReference TrafficChangeLaneConfigReference;

            [ReadOnly]
            public TrafficCommonSettingsConfigBlobReference TrafficCommonSettingsConfigBlobReference;

            [ReadOnly]
            public TrafficDestinationConfigReference TrafficDestinationConfigReference;

            [ReadOnly]
            public TrafficGeneralSettingsReference TrafficGeneralSettingsReference;

            [ReadOnly]
            public TrafficRoadConfigReference TrafficRoadConfigReference;

#if RUNTIME_ROAD
            [ReadOnly]
            public ComponentLookup<TrafficNodeAvailableComponent> TrafficNodeAvailableLookup;

            [ReadOnly]
            public ComponentLookup<TrafficNodeCapacityComponent> TrafficNodeCapacityLookup;

            [ReadOnly]
            public TrafficNodeResolverSystem.RuntimePathDataRef RuntimePathDataRef;

            [ReadOnly]
            public TrafficSpawnerConfigBlobReference TrafficSpawnerConfigBlobReference;

            [ReadOnly]
            public TrafficObstacleSystem.Singleton TrafficObstacleSystemSingleton;

            [ReadOnly]
            public int CurrentParkingCount;

            [ReadOnly]
            public float Timestamp;
#endif

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
                EnabledRefRW<TrafficIdleTag> trafficIdleTagRW,
                in TrafficTypeComponent trafficTypeComponent,
                in LocalTransform transform)
            {
                bool targetIsDestroyNode = TrafficNodeSettingsLookup.HasComponent(destinationComponent.DestinationNode) && TrafficNodeSettingsLookup[destinationComponent.DestinationNode].TrafficNodeType == TrafficNodeType.DestroyVehicle;

                if (!targetIsDestroyNode && (destinationComponent.NextDestinationNode == Entity.Null || destinationComponent.NextGlobalPathIndex == -1 || destinationComponent.NextDestinationNode == destinationComponent.DestinationNode))
                {
                    trafficNextTrafficNodeRequestTagRW.ValueRW = true;
                    return;
                }

                var currentTargetEntity = destinationComponent.DestinationNode;
                var newTargetNodeEntity = destinationComponent.NextDestinationNode;

                var achievedGlobalPathIndex = destinationComponent.NextGlobalPathIndex;

                if (!TrafficNodeSettingsLookup.HasComponent(currentTargetEntity))
                    return;

                trafficSwitchTargetNodeRequestTagRW.ValueRW = false;

                destinationComponent.PreviousNode = destinationComponent.DestinationNode;
                destinationComponent.ChangeLanePathIndex = destinationComponent.NextChangeLanePathIndex;

                var changeLanePathIndex = destinationComponent.ChangeLanePathIndex;

                if (changeLanePathIndex > 0)
                {
                    int sourceLocalNodeIndex = 0;

                    var found = TrafficChangeLaneUtils.SetWaitForChangeLane(
                        ref CommandBuffer,
                        achievedGlobalPathIndex,
                        changeLanePathIndex,
                        sourceLocalNodeIndex,
                        entity,
                        ref trafficPathComponent,
                        ref destinationComponent,
                        ref trafficStateComponent,
                        ref trafficIdleTagRW,
                        in transform,
                        currentTargetEntity,
                        ref Graph,
                        ref TrafficChangeLaneConfigReference,
                        ref TrafficCommonSettingsConfigBlobReference,
                        speedComponent.Value);

                    if (!found)
                    {
                        TrafficNoTargetUtils.AddNoTarget(ref CommandBuffer, entity, ref trafficStateComponent, ref trafficIdleTagRW, in TrafficDestinationConfigReference);
                    }

                    return;
                }
                else
                {
#if RUNTIME_ROAD
                    // Choose the route with the fewest cars for the run-time road scene
                    var currentPathIndex = trafficPathComponent.CurrentGlobalPathIndex;

                    if (currentPathIndex != achievedGlobalPathIndex)
                    {
                        var connectedPaths = Graph.GetConnectedPaths(currentPathIndex);

                        if (connectedPaths.Length > 1)
                        {
                            int maxCount = int.MaxValue;

                            var paths = new NativeList<int>(Allocator.Temp);

                            for (int i = 0; i < connectedPaths.Length; i++)
                            {
                                var connectedPathIndex = connectedPaths[i];

                                var nextEntity = RuntimePathDataRef.TryToGetConnectedNode(connectedPathIndex);

                                if (nextEntity == Entity.Null)
                                    continue;

                                if (!Graph.IsAvailable(connectedPathIndex, in trafficTypeComponent))
                                    continue;

                                var trafficNodeSettings = TrafficNodeSettingsLookup[nextEntity];
                                var trafficNodeCapacity = TrafficNodeCapacityLookup[nextEntity];

                                switch (trafficNodeSettings.TrafficNodeType)
                                {
                                    case TrafficNodeType.Parking:
                                        {
                                            var placeIsAvailable = TrafficNodeAvailableLookup[nextEntity].IsAvailable;
                                            var isAvailable = trafficNodeCapacity.HasSlots() && TrafficSpawnerConfigBlobReference.Reference.Value.MaxParkingCarsCount > CurrentParkingCount && placeIsAvailable;

                                            if (!isAvailable)
                                                continue;

                                            break;
                                        }
                                }

                                var carCount = TrafficObstacleSystemSingleton.CarHashMap.CountValuesForKey(connectedPathIndex);
                                var nextPaths = Graph.GetConnectedPaths(connectedPathIndex);

                                for (int j = 0; j < nextPaths.Length; j++)
                                {
                                    var nextCarCount = TrafficObstacleSystemSingleton.CarHashMap.CountValuesForKey(nextPaths[j]);
                                    carCount += nextCarCount;
                                }

                                if (carCount < maxCount)
                                {
                                    maxCount = carCount;

                                    paths.Clear();
                                    paths.Add(connectedPathIndex);
                                }
                                else if (carCount == maxCount)
                                {
                                    paths.Add(connectedPathIndex);
                                }
                            }

                            if (paths.Length > 0)
                            {
                                if (paths.Length == 1)
                                {
                                    achievedGlobalPathIndex = paths[0];
                                }
                                else
                                {
                                    var rnd = UnityMathematicsExtension.GetRandomGen(Timestamp, entity.Index);
                                    var randomLocalValue = rnd.NextInt(0, paths.Length);
                                    achievedGlobalPathIndex = paths[randomLocalValue];
                                }

                                newTargetNodeEntity = RuntimePathDataRef.TryToGetConnectedNode(achievedGlobalPathIndex);
                            }

                            paths.Dispose();
                        }
                    }
#endif
                }

                destinationComponent.DestinationNode = newTargetNodeEntity;

                if (TrafficNodeSettingsLookup.HasComponent(newTargetNodeEntity))
                {
                    var dstSettings = TrafficNodeSettingsLookup[newTargetNodeEntity];

                    if ((dstSettings.TrafficNodeTypeFlag & TrafficRoadConfigReference.Config.Value.LinkedNodeFlags) != 0)
                    {
                        trafficEnteringTriggerNodeTagRW.ValueRW = true;
                    }
                }

                ref readonly var selectedPathData = ref Graph.GetPathData(achievedGlobalPathIndex);

                float3 targetPosition = Graph.GetEndPosition(in selectedPathData);

                int direction = GetDirection(in transform, selectedPathData, targetPosition);

                trafficTargetDirectionComponent = new TrafficTargetDirectionComponent
                {
                    Direction = direction,
                };

                Vector3 targetWaypoint = default;
                Vector3 previousTargetWaypoint = default;

                var localPathNodeIndex = -1;
                float currentSpeedLimit = 0;

                if (destinationComponent.PathConnectionType == PathConnectionType.PathPoint)
                {
                    var pathNodes = Graph.GetRouteNodes(achievedGlobalPathIndex);

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

                        ref readonly var selectedPath = ref Graph.GetPathData(trafficPathComponent.CurrentGlobalPathIndex);
                        var intersectedPaths = Graph.GetIntersectedPaths(in selectedPath);

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
                            localPathNodeIndex = selectedPath.NodeCount - 2;
                        }

#if UNITY_EDITOR
                        Debug.Log($"TrafficSwitchTargetNodeSystem. Source path {trafficPathComponent.CurrentGlobalPathIndex} Achieved Path {achievedGlobalPathIndex}. PathPoint not assigned properly. Target path intersected {intersected}");
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

                    ref readonly var startNode = ref Graph.GetPathNodeData(in selectedPathData, 0);
                    targetWaypoint = startNode.Position;
                    previousTargetWaypoint = trafficPathComponent.DestinationWayPoint;
                    currentSpeedLimit = startNode.SpeedLimit;

                    trafficNextTrafficNodeRequestTagRW.ValueRW = true;
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

        private static int GetDirection(in LocalTransform transform, in PathGraphSystem.PathData selectedPathData, float3 targetPosition)
        {
            int direction = 0;

            bool isCurved = selectedPathData.PathCurveType != PathCurveType.StraightLine;

            if (isCurved)
            {
                Vector3 directionToTargetWaypoint = math.normalize(targetPosition - transform.Position);

                float signedAngle = Vector3.SignedAngle(transform.Forward(), directionToTargetWaypoint, Vector3.up);
                direction = signedAngle > 0 ? 1 : -1;
            }

            return direction;
        }
    }
}