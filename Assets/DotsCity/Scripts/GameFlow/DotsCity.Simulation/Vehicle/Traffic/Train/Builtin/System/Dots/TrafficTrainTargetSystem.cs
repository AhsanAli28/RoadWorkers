using Spirit604.DotsCity.Simulation.Car;
using Spirit604.DotsCity.Simulation.Level.Streaming;
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

namespace Spirit604.DotsCity.Simulation.Train
{
    [UpdateInGroup(typeof(LateInitGroup))]
    [BurstCompile]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial struct TrafficTrainTargetSystem : ISystem
    {
        private EntityQuery updateQuery;

        [BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            updateQuery = SystemAPI.QueryBuilder()
                .WithNone<TrafficCustomDestinationComponent, TrafficNoTargetTag>()
                .WithDisabledRW<TrafficSwitchTargetNodeRequestTag, TrafficAchievedTag>()
                .WithAllRW<SpeedComponent, TrafficDestinationComponent>()
                .WithAllRW<TrafficPathComponent>()
                .WithPresentRW<TrafficNextTrafficNodeRequestTag>()
                .WithAll<TrafficTag, HasDriverTag, TrafficTypeComponent, LocalTransform, TrainTag, TrainComponent>()
                .Build();

            state.RequireForUpdate(updateQuery);
            state.RequireForUpdate<TrafficNodeResolverSystem.RuntimePathDataRef>();
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var targetJob = new TargetJob()
            {
                NodeSettingsLookup = SystemAPI.GetComponentLookup<TrafficNodeSettingsComponent>(true),
                WorldTransformLookup = SystemAPI.GetComponentLookup<LocalToWorld>(true),
                TrafficRailMovementLookup = SystemAPI.GetComponentLookup<TrafficRailMovementTag>(true),
                Graph = SystemAPI.GetSingleton<PathGraphSystem.Singleton>(),
                TrafficNavConfig = SystemAPI.GetSingleton<TrafficDestinationConfigReference>(),
                DeltaTime = SystemAPI.Time.DeltaTime
            };

            targetJob.Run(updateQuery);
        }

        [WithNone(typeof(TrafficCustomDestinationComponent))]
        [WithAll(typeof(TrafficTag), typeof(HasDriverTag))]
        [BurstCompile]
        private partial struct TargetJob : IJobEntity
        {
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
            public float DeltaTime;

            void Execute(
                Entity entity,
                ref SpeedComponent speedComponent,
                ref TrafficDestinationComponent destinationComponent,
                ref TrafficPathComponent trafficPathComponent,
                EnabledRefRW<TrafficSwitchTargetNodeRequestTag> trafficSwitchTargetNodeRequestTagRW,
                EnabledRefRW<TrafficNextTrafficNodeRequestTag> trafficNextTrafficNodeRequestTagRW,
                EnabledRefRW<TrafficAchievedTag> trafficAchievedTag,
                in TrainComponent trainComponent,
                in TrafficTypeComponent trafficTypeComponent,
                in LocalTransform transform)
            {
                float3 carPosition = transform.Position;
                float3 currentTargetPosition = destinationComponent.Destination;
                ref readonly var pathData = ref Graph.GetPathData(trafficPathComponent.CurrentGlobalPathIndex);

                var dstNode = destinationComponent.DestinationNode;
                var hasDstNode = NodeSettingsLookup.HasComponent(dstNode);

                float distanceToTarget = math.distance(currentTargetPosition, carPosition);
                TrafficTargetSystem.CheckDistanceHowFarPreviousTrafficLight(in WorldTransformLookup, ref destinationComponent, in transform, TrafficNavConfig.Config.Value.MaxDistanceFromPreviousLightSQ);

                if (destinationComponent.PathConnectionType != PathConnectionType.PathPoint)
                {
                    var endPosition = Graph.GetEndPosition(in pathData);
                    float distanceToTargetNode = math.distance(endPosition, carPosition);

                    destinationComponent.DistanceToEndOfPath = distanceToTargetNode;

                    var nextNodeRequest = TrafficTargetSystem.CheckIfNewTrafficNodeIsCloseEnough(ref destinationComponent, distanceToTargetNode, TrafficNavConfig.Config.Value.MinDistanceToNewLight);

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
                        checkDistanceToTarget = !hasDstNode || NodeSettingsLookup[dstNode].CustomAchieveDistance == 0 ? TrafficNavConfig.Config.Value.MinDistanceToTarget : NodeSettingsLookup[dstNode].CustomAchieveDistance;
                        break;
                    case PathConnectionType.PathPoint:
                        checkDistanceToTarget = TrafficNavConfig.Config.Value.MinDistanceToPathPointTarget;
                        break;
                }

                bool switchToNextTarget = false;

                float distanceToLocalTarget = math.distance(trafficPathComponent.DestinationWayPoint, carPosition);

                destinationComponent.DistanceToWaypoint = distanceToLocalTarget;

                var isRailMovement = TrafficRailMovementLookup.HasComponent(entity);

                float checkDistanceToTargetRouteNode = !isRailMovement ? TrafficNavConfig.Config.Value.MinDistanceToTargetRouteNode : TrafficNavConfig.Config.Value.MinDistanceToTargetRailRouteNode;

                bool forceSwitchNode = false;

                var forward = math.mul(transform.Rotation, math.forward());

                if (destinationComponent.DistanceToWaypoint < 1)
                {
                    float3 directionToNode = math.normalize(trafficPathComponent.DestinationWayPoint - carPosition).Flat();

                    var carDirection = trafficPathComponent.PathDirection == PathForwardType.Forward ? forward : -forward;
                    var inRange = distanceToLocalTarget > TrafficNavConfig.Config.Value.MinDistanceToOutOfPath && distanceToLocalTarget < TrafficNavConfig.Config.Value.MaxDistanceToOutOfPath;

                    float dot = math.dot(directionToNode, carDirection);

                    forceSwitchNode = dot < 0f;
                }

                var deltaDistance = speedComponent.Value * DeltaTime;
                bool routeNodeIsAchieved = destinationComponent.DistanceToWaypoint < deltaDistance || forceSwitchNode;

                if (routeNodeIsAchieved)
                {
                    var pathNodes = Graph.GetRouteNodes(in pathData);

                    var currentLocalPathNodeIndex = trafficPathComponent.LocalPathNodeIndex;
                    var newLocalPathNodeIndex = currentLocalPathNodeIndex + 1;

                    if (newLocalPathNodeIndex >= pathNodes.Length)
                    {
                        switchToNextTarget = true;
                    }
                    else
                    {
                        currentLocalPathNodeIndex = newLocalPathNodeIndex - 1;
                        var pathNode = pathNodes[currentLocalPathNodeIndex];

                        speedComponent.LaneLimit = pathNode.SpeedLimit;
                        trafficPathComponent.PathDirection = pathNode.ForwardNodeDirectionType;

                        trafficPathComponent.LocalPathNodeIndex = newLocalPathNodeIndex;

                        Vector3 newTargetWaypoint = pathNodes[newLocalPathNodeIndex].Position;

                        if (!trafficPathComponent.DestinationWayPoint.Equals(float3.zero))
                        {
                            trafficPathComponent.PreviousDestination = trafficPathComponent.DestinationWayPoint;
                        }

                        trafficPathComponent.DestinationWayPoint = newTargetWaypoint;
                        destinationComponent.DistanceToWaypoint = math.distance(trafficPathComponent.DestinationWayPoint, carPosition);
                    }
                }

                if (switchToNextTarget)
                {
                    if (!hasDstNode)
                    {
                        // Destination node unloaded due to road streaming
                        trafficAchievedTag.ValueRW = true;
                        destinationComponent.AchieveState = AchieveState.NoTarget;
                        return;
                    }

                    if (!trainComponent.IsParent)
                    {
                        trafficSwitchTargetNodeRequestTagRW.ValueRW = true;
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
                }
            }
        }
    }
}