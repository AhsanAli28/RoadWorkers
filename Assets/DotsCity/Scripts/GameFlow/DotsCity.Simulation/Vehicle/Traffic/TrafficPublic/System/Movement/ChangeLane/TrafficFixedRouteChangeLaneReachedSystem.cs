using Spirit604.DotsCity.Simulation.Car;
using Spirit604.DotsCity.Simulation.Level.Streaming;
using Spirit604.DotsCity.Simulation.Road;
using Spirit604.DotsCity.Simulation.Traffic;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.TrafficPublic
{
    [UpdateInGroup(typeof(EarlyJobGroup))]
    [BurstCompile]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial struct TrafficFixedRouteChangeLaneReachedSystem : ISystem
    {
        private EntityQuery updateQuery;

        [BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            updateQuery = SystemAPI.QueryBuilder()
                .WithAll<TrafficFixedRouteTag, TrafficChangingLaneEventTag>()
                .Build();

            state.RequireForUpdate(updateQuery);
            state.RequireForUpdate<TrafficNodeResolverSystem.RuntimePathDataRef>();
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var achieveLaneJob = new AchieveLaneJob()
            {
                Graph = SystemAPI.GetSingleton<PathGraphSystem.Singleton>(),
                RuntimePathDataRef = SystemAPI.GetSingleton<TrafficNodeResolverSystem.RuntimePathDataRef>(),
            };

            achieveLaneJob.Schedule();
        }

        [WithAll(typeof(TrafficFixedRouteTag), typeof(TrafficChangingLaneEventTag))]
        [BurstCompile]
        public partial struct AchieveLaneJob : IJobEntity
        {
            [ReadOnly]
            public PathGraphSystem.Singleton Graph;

            [ReadOnly]
            public TrafficNodeResolverSystem.RuntimePathDataRef RuntimePathDataRef;

            void Execute(
                ref TrafficDestinationComponent destinationComponent,
                ref TrafficPathComponent pathComponent,
                ref TrafficChangeLaneComponent trafficChangeLaneComponent,
                ref TrafficFixedRouteComponent trafficFixedRouteComponent,
                ref TrafficTargetDirectionComponent trafficTargetDirectionComponent,
                ref SpeedComponent speedComponent,
                EnabledRefRW<TrafficChangingLaneEventTag> trafficChangingLaneEventTagRW)
            {
                if (!trafficChangeLaneComponent.ReachedTarget)
                    return;

                trafficChangeLaneComponent.ReachedTarget = false;

                int achievedPathIndex = trafficChangeLaneComponent.TargetPathGlobalIndex;

                pathComponent.CurrentGlobalPathIndex = achievedPathIndex;
                var targetLocalNodeIndex = trafficChangeLaneComponent.TargetLocalNodeIndex;
                pathComponent.LocalPathNodeIndex = targetLocalNodeIndex;

                ref readonly var achievedPathData = ref Graph.GetPathData(achievedPathIndex);
                ref readonly var previousNode = ref Graph.GetPathNodeData(in achievedPathData, targetLocalNodeIndex - 1);
                ref readonly var targetNode = ref Graph.GetPathNodeData(in achievedPathData, targetLocalNodeIndex);

                var destinationWayPoint = targetNode.Position;
                pathComponent.DestinationWayPoint = destinationWayPoint;
                speedComponent.LaneLimit = previousNode.SpeedLimit;

                destinationComponent.Destination = Graph.GetEndPosition(in achievedPathData);

                RuntimePathDataRef.TryGetValue(achievedPathIndex, out destinationComponent.PreviousNode, out destinationComponent.DestinationNode);

                trafficTargetDirectionComponent.Direction = 0;

                trafficFixedRouteComponent.RouteNodeIndex = ++trafficFixedRouteComponent.RouteNodeIndex % trafficFixedRouteComponent.RouteLength;

                trafficChangingLaneEventTagRW.ValueRW = false;
            }
        }
    }
}