using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Spirit604.DotsCity.Simulation.Traffic
{
    [UpdateInGroup(typeof(TrafficLateSimulationGroup))]
    [BurstCompile]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial struct TrafficChangeLaneTargetSystem : ISystem
    {
        private EntityQuery updateGroup;

        [BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            updateGroup = SystemAPI.QueryBuilder()
                  .WithAll<TrafficChangingLaneEventTag, TrafficChangeLaneComponent>()
                  .Build();

            state.RequireForUpdate(updateGroup);
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var changeLaneJob = new ChangeLaneJob()
            {
                LocalTransformLookup = SystemAPI.GetComponentLookup<LocalTransform>(),
                TrafficChangeLaneConfigReference = SystemAPI.GetSingleton<TrafficChangeLaneConfigReference>(),
            };

            changeLaneJob.ScheduleParallel();
        }

        [WithNone(typeof(TrafficCustomDestinationComponent))]
        [WithAll(typeof(TrafficChangingLaneEventTag))]
        [BurstCompile]
        public partial struct ChangeLaneJob : IJobEntity
        {
            [ReadOnly]
            public ComponentLookup<LocalTransform> LocalTransformLookup;

            [ReadOnly]
            public TrafficChangeLaneConfigReference TrafficChangeLaneConfigReference;

            void Execute(
                ref TrafficChangeLaneComponent trafficChangeLaneComponent,
                ref TrafficObstacleComponent carObstacleComponent,
                ref TrafficDestinationComponent destinationComponent,
                in LocalTransform transform)
            {
                if (trafficChangeLaneComponent.ReachedTarget)
                    return;

                float distanceToEnd = 0;

                float distanceSQ = math.distancesq(destinationComponent.Destination, transform.Position);

                if (LocalTransformLookup.HasComponent(destinationComponent.DestinationNode))
                {
                    distanceToEnd = math.distance(LocalTransformLookup[destinationComponent.DestinationNode].Position, transform.Position);
                }
                else
                {
                    distanceToEnd = math.sqrt(distanceSQ);
                }

                destinationComponent.DistanceToEndOfPath = distanceToEnd;

                if (distanceSQ < TrafficChangeLaneConfigReference.Config.Value.AchieveDistanceSQ)
                {
                    trafficChangeLaneComponent.ReachedTarget = true;
                }
            }
        }
    }
}