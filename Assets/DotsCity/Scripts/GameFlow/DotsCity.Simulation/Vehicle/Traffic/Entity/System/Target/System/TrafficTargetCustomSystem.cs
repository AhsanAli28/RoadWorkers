using Spirit604.DotsCity.Simulation.Car;
using Spirit604.DotsCity.Simulation.Level.Streaming;
using Spirit604.DotsCity.Simulation.Road;
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
    [UpdateInGroup(typeof(TrafficLateSimulationGroup))]
    [BurstCompile]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    /// <summary>
    /// An analogue of TrafficTargetSystem except that this vehicle has a custom destination distance with TrafficDestinationSharedConfig is a shared configuration.
    /// </summary>
    public partial struct TrafficTargetCustomSystem : ISystem
    {
        private EntityQuery updateQuery;

        [BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            updateQuery = SystemAPI.QueryBuilder()
                .WithNone<TrafficCustomDestinationComponent, TrafficNoTargetTag, TrafficChangingLaneEventTag>()
                .WithDisabledRW<TrafficAchievedTag, TrafficSwitchTargetNodeRequestTag>()
                .WithAllRW<SpeedComponent, TrafficDestinationComponent>()
                .WithAllRW<TrafficPathComponent>()
                .WithPresentRW<TrafficNextTrafficNodeRequestTag>()
                .WithAll<TrafficTag, HasDriverTag, TrafficTypeComponent, TrafficDestinationSharedConfig, LocalTransform>()
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
                TrafficNavConfig = SystemAPI.GetSingleton<TrafficDestinationConfigReference>(),
                TrafficGeneralSettingsReference = SystemAPI.GetSingleton<TrafficGeneralSettingsReference>(),
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
            public TrafficDestinationConfigReference TrafficNavConfig;

            [ReadOnly]
            public TrafficGeneralSettingsReference TrafficGeneralSettingsReference;

            void Execute(
                Entity entity,
                ref SpeedComponent speedComponent,
                ref TrafficDestinationComponent destinationComponent,
                ref TrafficPathComponent trafficPathComponent,
                EnabledRefRW<TrafficNextTrafficNodeRequestTag> trafficNextTrafficNodeRequestTagRW,
                EnabledRefRW<TrafficSwitchTargetNodeRequestTag> trafficSwitchTargetNodeRequestTagRW,
                EnabledRefRW<TrafficAchievedTag> trafficAchievedTag,
                in TrafficDestinationSharedConfig trafficDestinationSharedConfig,
                in TrafficTypeComponent trafficTypeComponent,
                in LocalTransform transform)
            {
                float3 carPosition = transform.Position;
                float3 currentTargetPosition = destinationComponent.Destination;

                ref readonly var pathData = ref Graph.GetPathData(trafficPathComponent.CurrentGlobalPathIndex);

                var dstNode = destinationComponent.DestinationNode;
                var hasDstNode = NodeSettingsLookup.HasComponent(dstNode);

                float distanceToTarget = math.distance(currentTargetPosition, carPosition);
                TrafficTargetSystem.CheckDistanceHowFarPreviousTrafficLight(in WorldTransformLookup, ref destinationComponent, in transform, trafficDestinationSharedConfig.MaxDistanceFromPreviousLightSQ);

                if (destinationComponent.PathConnectionType != PathConnectionType.PathPoint)
                {
                    var endPosition = Graph.GetEndPosition(in pathData);
                    float distanceToTargetNode = math.distance(endPosition, carPosition);

                    destinationComponent.DistanceToEndOfPath = distanceToTargetNode;

                    var nextNodeRequest = TrafficTargetSystem.CheckIfNewTrafficNodeIsCloseEnough(ref destinationComponent, distanceToTargetNode, trafficDestinationSharedConfig.MinDistanceToNewLight);

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
                        checkDistanceToTarget = !hasDstNode || NodeSettingsLookup[dstNode].CustomAchieveDistance == 0 ? trafficDestinationSharedConfig.MinDistanceToTarget : NodeSettingsLookup[dstNode].CustomAchieveDistance;
                        break;
                    case PathConnectionType.PathPoint:
                        checkDistanceToTarget = trafficDestinationSharedConfig.MinDistanceToPathPointTarget;
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

                    float checkDistanceToTargetRouteNode = !isRailMovement ? trafficDestinationSharedConfig.MinDistanceToTargetRouteNode : trafficDestinationSharedConfig.MinDistanceToTargetRailRouteNode;

                    bool forceSwitchNode = false;

                    var forward = math.mul(transform.Rotation, math.forward());

                    if (TrafficNavConfig.Config.Value.OutOfPathMethod != OutOfPathResolveMethod.Disabled)
                    {
                        float3 directionToNode = math.normalize(trafficPathComponent.DestinationWayPoint - carPosition).Flat();

                        var carDirection = trafficPathComponent.PathDirection == PathForwardType.Forward ? forward : -forward;
                        var inRange = distanceToLocalTarget > TrafficNavConfig.Config.Value.MinDistanceToOutOfPath && distanceToLocalTarget < TrafficNavConfig.Config.Value.MaxDistanceToOutOfPath;

                        if (inRange)
                        {
                            switch (TrafficNavConfig.Config.Value.OutOfPathMethod)
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
                                            return;
                                        }

                                        break;
                                    }
                                case OutOfPathResolveMethod.Cull:
                                    {
                                        trafficAchievedTag.ValueRW = true;
                                        destinationComponent.AchieveState = AchieveState.Cull;
                                        return;
                                    }
                            }
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
                                trafficAchievedTag.ValueRW = true;
                                destinationComponent.AchieveState = AchieveState.ChangeLane;
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
    }
}