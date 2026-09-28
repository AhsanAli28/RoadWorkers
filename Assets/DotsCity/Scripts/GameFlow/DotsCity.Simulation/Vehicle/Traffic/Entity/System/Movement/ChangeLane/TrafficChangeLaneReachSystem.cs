using Spirit604.DotsCity.Simulation.Car;
using Spirit604.DotsCity.Simulation.Level.Streaming;
using Spirit604.DotsCity.Simulation.Road;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Traffic
{
    [UpdateInGroup(typeof(LateEventGroup))]
    [BurstCompile]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial struct TrafficChangeLaneReachSystem : ISystem
    {
        private EntityQuery trafficGroup;

        [BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            trafficGroup = SystemAPI.QueryBuilder()
                .WithAny<TrafficDefaultTag, TrafficPlayerControlTag>()
                .WithAll<TrafficChangingLaneEventTag, TrafficChangeLaneComponent>()
                .Build();

            state.RequireForUpdate(trafficGroup);
            state.RequireForUpdate<TrafficNodeResolverSystem.RuntimePathDataRef>();
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var achieveTargetLaneJob = new AchieveTargetLaneJob()
            {
                RuntimePathDataRef = SystemAPI.GetSingleton<TrafficNodeResolverSystem.RuntimePathDataRef>(),
                Graph = SystemAPI.GetSingleton<PathGraphSystem.Singleton>(),
                TrafficChangeLaneConfigReference = SystemAPI.GetSingleton<TrafficChangeLaneConfigReference>(),
                CurrentTime = (float)SystemAPI.Time.ElapsedTime,
            };

            achieveTargetLaneJob.Schedule();
        }

        [WithAny(typeof(TrafficDefaultTag), typeof(TrafficPlayerControlTag))]
        [WithAll(typeof(TrafficChangingLaneEventTag))]
        [BurstCompile]
        public partial struct AchieveTargetLaneJob : IJobEntity
        {
            [ReadOnly]
            public TrafficNodeResolverSystem.RuntimePathDataRef RuntimePathDataRef;

            [ReadOnly]
            public PathGraphSystem.Singleton Graph;

            [ReadOnly]
            public TrafficChangeLaneConfigReference TrafficChangeLaneConfigReference;

            [ReadOnly]
            public float CurrentTime;

            void Execute(
                ref TrafficDestinationComponent destinationComponent,
                ref TrafficPathComponent trafficPathComponent,
                ref TrafficChangeLaneComponent trafficChangeLaneComponent,
                ref TrafficTargetDirectionComponent trafficTargetDirectionComponent,
                ref SpeedComponent speedComponent,
                EnabledRefRW<TrafficChangingLaneEventTag> trafficChangingLaneEventTagRW)
            {
                if (!trafficChangeLaneComponent.ReachedTarget)
                    return;

                trafficChangeLaneComponent.ReachedTarget = false;
                float blockTime = TrafficChangeLaneConfigReference.Config.Value.BlockDurationAfterChangeLane;

                trafficChangeLaneComponent.CheckTimeStamp = CurrentTime + blockTime;

                int achievedPathIndex = trafficChangeLaneComponent.TargetPathGlobalIndex;

                trafficPathComponent.CurrentGlobalPathIndex = achievedPathIndex;
                var targetLocalNodeIndex = trafficChangeLaneComponent.TargetLocalNodeIndex;
                trafficPathComponent.LocalPathNodeIndex = targetLocalNodeIndex;

                ref readonly var achievedPathData = ref Graph.GetPathData(achievedPathIndex);
                ref readonly var previousTargetNode = ref Graph.GetPathNodeData(in achievedPathData, targetLocalNodeIndex - 1);
                ref readonly var targetNode = ref Graph.GetPathNodeData(in achievedPathData, targetLocalNodeIndex);

                var targetWayPoint = targetNode.Position;
                trafficPathComponent.DestinationWayPoint = targetWayPoint;
                speedComponent.LaneLimit = previousTargetNode.SpeedLimit;

                RuntimePathDataRef.TryGetValue(achievedPathIndex, out destinationComponent.PreviousNode, out destinationComponent.DestinationNode);

                destinationComponent.CurrentNode = Entity.Null;
                destinationComponent.NextDestinationNode = Entity.Null;

                destinationComponent.Destination = Graph.GetEndPosition(in achievedPathData);
                destinationComponent.NextGlobalPathIndex = -1;
                trafficTargetDirectionComponent = new TrafficTargetDirectionComponent()
                {
                    Direction = 0
                };

                trafficChangingLaneEventTagRW.ValueRW = false;
            }
        }
    }
}