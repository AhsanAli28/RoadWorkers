using Spirit604.DotsCity.Simulation.Car;
using Spirit604.DotsCity.Simulation.Level.Streaming;
using Spirit604.DotsCity.Simulation.Road;
using Spirit604.Extensions;
using Spirit604.Gameplay.Road;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Traffic
{
    [UpdateInGroup(typeof(TrafficLateSimulationGroup))]
    [BurstCompile]
    public partial struct TrafficTargetSystem : ISystem
    {
        private EntityQuery updateQuery;

        [BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            updateQuery = SystemAPI.QueryBuilder()
                .WithNone<TrafficCustomDestinationComponent, TrafficNoTargetTag, TrafficCustomTargetingTag, TrafficChangingLaneEventTag>()
                .WithDisabledRW<TrafficAchievedTag, TrafficSwitchTargetNodeRequestTag>()
                .WithAllRW<SpeedComponent, TrafficDestinationComponent>()
                .WithAllRW<TrafficPathComponent>()
                .WithPresentRW<TrafficNextTrafficNodeRequestTag>()
                .WithAll<TrafficTag, HasDriverTag, TrafficTypeComponent, LocalTransform>()
                .Build();

            state.RequireForUpdate(updateQuery);
            state.RequireForUpdate<TrafficNodeResolverSystem.RuntimePathDataRef>();
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var targetJob = new TargetJob()
            {
                CapacityLookup = SystemAPI.GetComponentLookup<TrafficNodeCapacityComponent>(true),
                NodeSettingsLookup = SystemAPI.GetComponentLookup<TrafficNodeSettingsComponent>(true),
                WorldTransformLookup = SystemAPI.GetComponentLookup<LocalToWorld>(true),
                TrafficRailMovementLookup = SystemAPI.GetComponentLookup<TrafficRailMovementTag>(true),
                Graph = SystemAPI.GetSingleton<PathGraphSystem.Singleton>(),
                ParkingConfig = SystemAPI.GetSingleton<TrafficParkingConfigReference>(),
                TrafficGeneralSettingsReference = SystemAPI.GetSingleton<TrafficGeneralSettingsReference>(),
                TrafficDestinationConfigReference = SystemAPI.GetSingleton<TrafficDestinationConfigReference>(),
                TrafficCommonSettingsConfigBlobReference = SystemAPI.GetSingleton<TrafficCommonSettingsConfigBlobReference>(),
            };

            targetJob.ScheduleParallel(updateQuery);
        }

        [WithNone(typeof(TrafficCustomDestinationComponent), typeof(TrafficChangingLaneEventTag))]
        [WithAll(typeof(TrafficTag), typeof(HasDriverTag))]
        [BurstCompile]
        private partial struct TargetJob : IJobEntity
        {
            [ReadOnly]
            public ComponentLookup<TrafficNodeCapacityComponent> CapacityLookup;

            [ReadOnly]
            public ComponentLookup<TrafficNodeSettingsComponent> NodeSettingsLookup;

            [ReadOnly]
            public ComponentLookup<LocalToWorld> WorldTransformLookup;

            [ReadOnly]
            public ComponentLookup<TrafficRailMovementTag> TrafficRailMovementLookup;

            [ReadOnly]
            public PathGraphSystem.Singleton Graph;

            [ReadOnly]
            public TrafficParkingConfigReference ParkingConfig;

            [ReadOnly]
            public TrafficGeneralSettingsReference TrafficGeneralSettingsReference;

            [ReadOnly]
            public TrafficDestinationConfigReference TrafficDestinationConfigReference;

            [ReadOnly]
            public TrafficCommonSettingsConfigBlobReference TrafficCommonSettingsConfigBlobReference;

            void Execute(
                Entity entity,
                ref SpeedComponent speedComponent,
                ref TrafficDestinationComponent destinationComponent,
                ref TrafficPathComponent trafficPathComponent,
                EnabledRefRW<TrafficNextTrafficNodeRequestTag> trafficNextTrafficNodeRequestTagRW,
                EnabledRefRW<TrafficSwitchTargetNodeRequestTag> trafficSwitchTargetNodeRequestTagRW,
                EnabledRefRW<TrafficAchievedTag> trafficAchievedTag,
                in TrafficTypeComponent trafficTypeComponent,
                in LocalTransform transform)
            {
                float3 carPosition = transform.Position;
                float3 currentTargetPosition = destinationComponent.Destination;

                ref readonly var pathData = ref Graph.GetPathData(trafficPathComponent.CurrentGlobalPathIndex);

                var dstNode = destinationComponent.DestinationNode;
                var hasDstNode = NodeSettingsLookup.HasComponent(dstNode);

                float distanceToTarget = math.distance(currentTargetPosition, carPosition);
                CheckDistanceHowFarPreviousTrafficLight(in WorldTransformLookup, ref destinationComponent, in transform, TrafficDestinationConfigReference.Config.Value.MaxDistanceFromPreviousLightSQ);

                if (destinationComponent.PathConnectionType != PathConnectionType.PathPoint)
                {
                    var endPosition = Graph.GetEndPosition(in pathData);
                    float distanceToTargetNode = math.distance(endPosition, carPosition);

                    destinationComponent.DistanceToEndOfPath = distanceToTargetNode;

                    var nextNodeRequest = CheckIfNewTrafficNodeIsCloseEnough(ref destinationComponent, distanceToTargetNode, TrafficDestinationConfigReference.Config.Value.MinDistanceToNewLight);

                    if (nextNodeRequest)
                    {
                        if (!hasDstNode)
                        {
                            // Destination node unloaded due to road streaming
                            trafficAchievedTag.ValueRW = true;
                            destinationComponent.AchieveState = AchieveState.NoTarget;
                            return;
                        }

                        if (destinationComponent.NextDestinationNode == Entity.Null)
                        {
                            trafficNextTrafficNodeRequestTagRW.ValueRW = true;
                        }
                    }
                }
                else
                {
                    destinationComponent.DistanceToEndOfPath = math.distance(destinationComponent.Destination, carPosition);
                    destinationComponent.DistanceToWaypoint = destinationComponent.DistanceToEndOfPath;
                }

                float checkDistanceToTarget = 0;

                switch (destinationComponent.PathConnectionType)
                {
                    case PathConnectionType.TrafficNode:
                        checkDistanceToTarget = !hasDstNode || NodeSettingsLookup[dstNode].CustomAchieveDistance == 0 ? TrafficDestinationConfigReference.Config.Value.MinDistanceToTarget : NodeSettingsLookup[dstNode].CustomAchieveDistance;
                        break;
                    case PathConnectionType.PathPoint:
                        checkDistanceToTarget = TrafficDestinationConfigReference.Config.Value.MinDistanceToPathPointTarget;
                        break;
                }

                bool targetIsAchieved = distanceToTarget < checkDistanceToTarget;

                if (targetIsAchieved && destinationComponent.PathConnectionType == PathConnectionType.PathPoint)
                {
                    if (trafficPathComponent.LocalPathNodeIndex < pathData.NodeCount - 1)
                    {
                        targetIsAchieved = false;
                    }
                }

                bool switchToNextTarget = targetIsAchieved;

                if (switchToNextTarget)
                {
                    if (!hasDstNode)
                    {
                        // Destination node unloaded due to road streaming
                        trafficAchievedTag.ValueRW = true;
                        destinationComponent.AchieveState = AchieveState.NoTarget;
                        return;
                    }

                    var defaultNode = NodeSettingsLookup[dstNode].TrafficNodeType == TrafficNodeType.Default;

                    if (defaultNode)
                    {
                        trafficSwitchTargetNodeRequestTagRW.ValueRW = true;
                    }
                    else
                    {
                        trafficAchievedTag.ValueRW = true;
                        destinationComponent.AchieveState = AchieveState.Success;
                    }

                    return;
                }
                else
                {
                    float distanceToLocalTarget = math.distance(trafficPathComponent.DestinationWayPoint, carPosition);

                    destinationComponent.DistanceToWaypoint = distanceToLocalTarget;

                    var isRailMovement = TrafficRailMovementLookup.HasComponent(entity);

                    float checkDistanceToTargetRouteNode = !isRailMovement ? TrafficDestinationConfigReference.Config.Value.MinDistanceToTargetRouteNode : TrafficDestinationConfigReference.Config.Value.MinDistanceToTargetRailRouteNode;

                    bool forceSwitchNode = false;

                    var forward = math.mul(transform.Rotation, math.forward());

                    if (TrafficDestinationConfigReference.Config.Value.OutOfPathMethod != OutOfPathResolveMethod.Disabled)
                    {
                        float3 directionToNode = math.normalize(trafficPathComponent.DestinationWayPoint - carPosition).Flat();

                        var carDirection = trafficPathComponent.PathDirection == PathForwardType.Forward ? forward : -forward;
                        var inRange = distanceToLocalTarget > TrafficDestinationConfigReference.Config.Value.MinDistanceToOutOfPath && distanceToLocalTarget < TrafficDestinationConfigReference.Config.Value.MaxDistanceToOutOfPath;

                        if (inRange)
                        {
                            switch (TrafficDestinationConfigReference.Config.Value.OutOfPathMethod)
                            {
                                case OutOfPathResolveMethod.SwitchNode:
                                    {
                                        float dot = math.dot(directionToNode, carDirection);
                                        forceSwitchNode = dot < 0f;
                                        break;
                                    }
                                case OutOfPathResolveMethod.Backward:
                                    {
                                        if (trafficPathComponent.PathDirection == PathForwardType.Forward)
                                        {
                                            trafficAchievedTag.ValueRW = true;
                                            destinationComponent.AchieveState = AchieveState.Backward;
                                        }

                                        break;
                                    }
                                case OutOfPathResolveMethod.Cull:
                                    {
                                        trafficAchievedTag.ValueRW = true;
                                        destinationComponent.AchieveState = AchieveState.Cull;

                                        break;
                                    }
                            }
                        }
                    }

                    if (TrafficDestinationConfigReference.Config.Value.HighSpeedRouteNodeCalc)
                    {
                        if (speedComponent.Value > TrafficCommonSettingsConfigBlobReference.Reference.Value.DefaultLaneSpeed)
                        {
                            var multiplier = speedComponent.Value / TrafficCommonSettingsConfigBlobReference.Reference.Value.DefaultLaneSpeed * TrafficDestinationConfigReference.Config.Value.HighSpeedRouteNodeMult;
                            checkDistanceToTargetRouteNode *= multiplier;
                        }
                    }

                    bool routeNodeIsAchieved = distanceToLocalTarget < checkDistanceToTargetRouteNode || forceSwitchNode;

                    if (routeNodeIsAchieved)
                    {
                        var pathNodes = Graph.GetRouteNodes(in pathData);

                        var currentLocalPathNodeIndex = trafficPathComponent.LocalPathNodeIndex;
                        var newLocalPathNodeIndex = math.clamp(currentLocalPathNodeIndex + 1, 0, pathNodes.Length - 1);

                        currentLocalPathNodeIndex = newLocalPathNodeIndex - 1;
                        var pathNode = pathNodes[currentLocalPathNodeIndex];

                        speedComponent.LaneLimit = pathNode.SpeedLimit;
                        trafficPathComponent.PathDirection = pathNode.ForwardNodeDirectionType;

                        if (TrafficGeneralSettingsReference.Config.Value.ChangeLaneSupport && pathData.HasOption(PathOptions.HasCustomNode))
                        {
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
                                        trafficAchievedTag.ValueRW = true;
                                        destinationComponent.AchieveState = AchieveState.ChangeLane;
                                        return;
                                    }
                                }
                            }
                        }

                        trafficPathComponent.LocalPathNodeIndex = newLocalPathNodeIndex;

                        Vector3 newTargetWaypoint = pathNodes[newLocalPathNodeIndex].Position;

                        if (!trafficPathComponent.DestinationWayPoint.Equals(float3.zero))
                        {
                            trafficPathComponent.PreviousDestination = trafficPathComponent.DestinationWayPoint;
                        }

                        trafficPathComponent.DestinationWayPoint = newTargetWaypoint;
                    }
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool CheckIfNewTrafficNodeIsCloseEnough(ref TrafficDestinationComponent destinationComponent, float distanceToNode, float minDistanceToNewLight)
        {
            if (distanceToNode < minDistanceToNewLight && destinationComponent.CurrentNode != destinationComponent.DestinationNode)
            {
                destinationComponent.CurrentNode = destinationComponent.DestinationNode;
                return true;
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void CheckDistanceHowFarPreviousTrafficLight(
            in ComponentLookup<LocalToWorld> nodePositions,
            ref TrafficDestinationComponent destinationComponent,
            in LocalTransform transform,
            float maxDistanceFromPreviousLightSQ)
        {
            var currentNodeEntity = destinationComponent.CurrentNode;

            if (currentNodeEntity != Entity.Null && currentNodeEntity == destinationComponent.PreviousNode && nodePositions.HasComponent(currentNodeEntity))
            {
                float distanceToCurrentNode = math.distancesq(nodePositions[currentNodeEntity].Position, transform.Position);

                if (distanceToCurrentNode > maxDistanceFromPreviousLightSQ)
                {
                    var dirToTarget = math.normalize(nodePositions[currentNodeEntity].Position - transform.Position);

                    var dot = math.dot(transform.Forward(), dirToTarget);

                    // The light is behind
                    if (dot < 0)
                    {
                        destinationComponent.CurrentNode = Entity.Null;
                    }
                }
            }
        }
    }
}