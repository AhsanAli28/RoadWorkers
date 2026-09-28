using Spirit604.DotsCity.Simulation.Car;
using Spirit604.DotsCity.Simulation.Road;
using Spirit604.DotsCity.Simulation.Traffic;
using Spirit604.Gameplay.Road;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.TrafficPublic
{
    [UpdateInGroup(typeof(TrafficProcessNodeGroup), OrderLast = true)]
    [BurstCompile]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial struct TrafficFixedRouteSwitchTargetNodeSystem : ISystem
    {
        private EntityQuery updateQuery;

        [BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            updateQuery = SystemAPI.QueryBuilder()
                .WithNone<TrafficNoTargetTag>()
                .WithAllRW<TrafficDestinationComponent, TrafficFixedRouteComponent>()
                .WithAllRW<TrafficPathComponent, TrafficTargetDirectionComponent>()
                .WithAllRW<TrafficStateComponent, SpeedComponent>()
                .WithAllRW<TrafficSwitchTargetNodeRequestTag>()
                .WithPresentRW<TrafficEnteringTriggerNodeTag, TrafficIdleTag>()
                .WithAll<TrafficFixedRouteTag, LocalTransform>()
                .Build();

            state.RequireForUpdate(updateQuery);
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var switchTargetNodeJob = new SwitchTargetNodeJob()
            {
                CommandBuffer = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged),
                WorldTransformLookup = SystemAPI.GetComponentLookup<LocalToWorld>(true),
                TrafficNodeSettingsLookup = SystemAPI.GetComponentLookup<TrafficNodeSettingsComponent>(true),
                FixedRouteNodeLookup = SystemAPI.GetBufferLookup<FixedRouteNodeElement>(true),
                Graph = SystemAPI.GetSingleton<PathGraphSystem.Singleton>(),
                TrafficRoadConfigReference = SystemAPI.GetSingleton<TrafficRoadConfigReference>(),
            };

            switchTargetNodeJob.Run(updateQuery);
        }

        [WithNone(typeof(TrafficNoTargetTag))]
        [WithAll(typeof(TrafficFixedRouteTag))]
        [BurstCompile]
        public partial struct SwitchTargetNodeJob : IJobEntity
        {
            public EntityCommandBuffer CommandBuffer;

            [ReadOnly]
            public ComponentLookup<LocalToWorld> WorldTransformLookup;

            [ReadOnly]
            public ComponentLookup<TrafficNodeSettingsComponent> TrafficNodeSettingsLookup;

            [ReadOnly]
            public BufferLookup<FixedRouteNodeElement> FixedRouteNodeLookup;

            [ReadOnly]
            public PathGraphSystem.Singleton Graph;

            [ReadOnly]
            public TrafficRoadConfigReference TrafficRoadConfigReference;

            void Execute(
                Entity entity,
                ref TrafficDestinationComponent destinationComponent,
                ref TrafficFixedRouteComponent trafficRouteComponent,
                ref TrafficPathComponent trafficPathComponent,
                ref TrafficTargetDirectionComponent trafficTargetDirectionComponent,
                ref TrafficStateComponent trafficStateComponent,
                ref SpeedComponent speedComponent,
                EnabledRefRW<TrafficSwitchTargetNodeRequestTag> trafficSwitchTargetNodeRequestTagRW,
                EnabledRefRW<TrafficEnteringTriggerNodeTag> trafficEnteringTriggerNodeTagRW,
                EnabledRefRW<TrafficIdleTag> trafficIdleTagRW,
                in LocalTransform transform)
            {
                trafficSwitchTargetNodeRequestTagRW.ValueRW = false;

                var route = FixedRouteNodeLookup[trafficRouteComponent.RouteEntity];

                int routeNodeIndex = -1;
                int newRouteNodeIndex = -1;
                int nextRouteNodeIndex = -1;

                if (trafficRouteComponent.LoopPath)
                {
                    routeNodeIndex = trafficRouteComponent.RouteNodeIndex;
                    newRouteNodeIndex = (trafficRouteComponent.RouteNodeIndex + 1) % route.Length;
                    nextRouteNodeIndex = (trafficRouteComponent.RouteNodeIndex + 2) % route.Length;
                }
                else
                {
                    routeNodeIndex = trafficRouteComponent.RouteNodeIndex;

                    if (route.Length > trafficRouteComponent.RouteNodeIndex + 1)
                    {
                        newRouteNodeIndex = trafficRouteComponent.RouteNodeIndex + 1;
                    }

                    if (route.Length > trafficRouteComponent.RouteNodeIndex + 2)
                    {
                        nextRouteNodeIndex = trafficRouteComponent.RouteNodeIndex + 2;
                    }
                }

                Entity newTargetEntity = Entity.Null;

                if (newRouteNodeIndex != -1)
                {
                    newTargetEntity = route[newRouteNodeIndex].TrafficNodeEntity;
                }

                Entity nextTargetEntity = Entity.Null;

                if (nextRouteNodeIndex != -1)
                {
                    nextTargetEntity = route[nextRouteNodeIndex].TrafficNodeEntity;
                }

                if (!TrafficNodeSettingsLookup.HasComponent(newTargetEntity))
                {
                    TrafficStateExtension.AddIdleState<TrafficNoTargetTag>(ref CommandBuffer, entity, ref trafficStateComponent, ref trafficIdleTagRW, TrafficIdleState.NoTarget);
                    return;
                }

                int nextGlobalPathIndex = route[routeNodeIndex].PathKey;

                ref readonly var selectedPathData = ref Graph.GetPathData(nextGlobalPathIndex);
                var currentPathNodes = Graph.GetRouteNodes(in selectedPathData);

                bool isCurved = selectedPathData.PathCurveType != PathCurveType.StraightLine;
                bool isStraightRoad = selectedPathData.PathRoadType == PathRoadType.StraightRoad;

                int direction = 0;

                float currentSpeedLimit = currentPathNodes[0].SpeedLimit;

                if (isCurved)
                {
                    Vector3 endPathPoint = Graph.GetEndPosition(selectedPathData);
                    Vector3 directionToTargetWaypoint = (endPathPoint - (Vector3)transform.Position).normalized;

                    float signedAngle = Vector3.SignedAngle(transform.Forward(), directionToTargetWaypoint, Vector3.up);
                    direction = signedAngle > 0 ? 1 : -1;
                }

                trafficTargetDirectionComponent = new TrafficTargetDirectionComponent { Direction = direction };

                var priority = selectedPathData.Priority;

                bool nextIsChangeLaneNode = route[newRouteNodeIndex].IsChangeLaneNode == 1;

                Vector3 targetPosition = default;

                if (!nextIsChangeLaneNode)
                {
                    targetPosition = WorldTransformLookup[newTargetEntity].Position;
                }
                else
                {
                    targetPosition = route[newRouteNodeIndex].Position;
                }

                float3 targetWaypoint = default;
                float3 previoutTargetWaypoint = default;

                int targetWaypointIndex = 1;
                bool isChangeLaneNode = route[routeNodeIndex].IsChangeLaneNode == 1;

                if (isChangeLaneNode)
                {
                    var laneTargetPosition = route[newRouteNodeIndex].Position;
                    var pathkey = route[newRouteNodeIndex].PathKey;
                    var localNodeIndex = route[newRouteNodeIndex].CustomLocalTargetWaypointIndex;
                    var targetLaneEntity = route[newRouteNodeIndex].TrafficNodeEntity;

                    targetWaypoint = laneTargetPosition;
                    priority = -5;
                    currentSpeedLimit = route[routeNodeIndex].CustomSpeedLimit;

                    var changeLaneSettings = new TrafficChangeLaneRequestedPositionComponent()
                    {
                        Destination = laneTargetPosition,
                        TargetPathKey = pathkey,
                        TargetPathNodeIndex = localNodeIndex,
                        TargetSourceLaneEntity = targetLaneEntity,
                    };

                    CommandBuffer.AddComponent(entity, changeLaneSettings);

                    TrafficStateExtension.AddIdleState<TrafficWaitForChangeLaneTag>(ref CommandBuffer, entity, ref trafficStateComponent, ref trafficIdleTagRW, TrafficIdleState.WaitForChangeLane);
                }
                else
                {
                    targetWaypoint = currentPathNodes[targetWaypointIndex].Position;
                    previoutTargetWaypoint = currentPathNodes[0].Position;
                }

                trafficPathComponent = new TrafficPathComponent
                {
                    CurrentGlobalPathIndex = nextGlobalPathIndex,
                    LocalPathNodeIndex = targetWaypointIndex,
                    DestinationWayPoint = targetWaypoint,
                    PreviousDestination = previoutTargetWaypoint,
                    Priority = priority
                };

                destinationComponent.CurrentNode = destinationComponent.DestinationNode;
                destinationComponent.PreviousNode = destinationComponent.DestinationNode;
                destinationComponent.DestinationNode = newTargetEntity;
                destinationComponent.NextDestinationNode = nextTargetEntity;
                destinationComponent.NextGlobalPathIndex = route[newRouteNodeIndex].PathKey;

                var dstSettings = TrafficNodeSettingsLookup[newTargetEntity];

                if ((dstSettings.TrafficNodeTypeFlag & TrafficRoadConfigReference.Config.Value.LinkedNodeFlags) != 0)
                {
                    trafficEnteringTriggerNodeTagRW.ValueRW = true;
                }

                destinationComponent.Destination = targetPosition;

                speedComponent.LaneLimit = currentSpeedLimit;

                trafficRouteComponent.RouteNodeIndex = newRouteNodeIndex;
            }
        }
    }
}