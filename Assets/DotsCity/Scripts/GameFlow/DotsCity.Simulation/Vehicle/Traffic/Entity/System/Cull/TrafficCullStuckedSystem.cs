using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Simulation.Car;
using Spirit604.Gameplay.Road;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using static Spirit604.DotsCity.Simulation.Traffic.TrafficPathfindingSystem;

namespace Spirit604.DotsCity.Simulation.Traffic
{
    [UpdateInGroup(typeof(LateEventGroup))]
    [BurstCompile]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial struct TrafficCullStuckedSystem : ISystem
    {
        private EntityQuery trafficGroup;

        [BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            trafficGroup = SystemAPI.QueryBuilder()
                .WithNone<CulledEventTag>()
                .WithAbsent<PathRequest>()
                .WithDisabled<PooledEventTag>()
                .WithAll<HasDriverTag, TrafficStuckInfoComponent>()
                .Build();

            state.RequireForUpdate(trafficGroup);
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var cullStuckedJob = new CullStuckedJob()
            {
                InViewOfCameraLookup = SystemAPI.GetComponentLookup<InViewOfCameraTag>(true),
                TrafficNoTargetLookup = SystemAPI.GetComponentLookup<TrafficNoTargetTag>(true),
                TrafficAntistuckConfigReference = SystemAPI.GetSingleton<TrafficAntistuckConfigReference>(),
                CurrentTimestamp = (float)SystemAPI.Time.ElapsedTime,
            };

            cullStuckedJob.ScheduleParallel();
        }

        [WithNone(typeof(CulledEventTag), typeof(TrafficPlayerControlTag))]
        [WithDisabled(typeof(PooledEventTag))]
        [WithAll(typeof(TrafficTag), typeof(HasDriverTag))]
        [BurstCompile]
        public partial struct CullStuckedJob : IJobEntity
        {
            [ReadOnly]
            public ComponentLookup<InViewOfCameraTag> InViewOfCameraLookup;

            [ReadOnly]
            public ComponentLookup<TrafficNoTargetTag> TrafficNoTargetLookup;

            [ReadOnly]
            public TrafficAntistuckConfigReference TrafficAntistuckConfigReference;

            [ReadOnly]
            public float CurrentTimestamp;

            void Execute(
                Entity entity,
                ref TrafficStuckInfoComponent trafficStuckInfoComponent,
                EnabledRefRW<PooledEventTag> pooledEventTagRW,
                in TrafficObstacleComponent carObstacleComponent,
                in TrafficDestinationComponent destinationComponent,
                in TrafficLightDataComponent trafficLightDataComponent)
            {
                float remainDistance = destinationComponent.DistanceToEndOfPath;

                bool stucked = carObstacleComponent.HasObstacle || TrafficNoTargetLookup.HasComponent(entity);

                if ((!stucked) ||
                    (trafficLightDataComponent.LightStateOfTargetNode != LightState.Green && trafficLightDataComponent.LightStateOfTargetNode != LightState.Uninitialized) ||
                    trafficStuckInfoComponent.SavedTimestamp == 0)
                {
                    SaveTimestamp(ref trafficStuckInfoComponent, CurrentTimestamp, remainDistance, in TrafficAntistuckConfigReference);
                }
                else
                {
                    float diffDistance = trafficStuckInfoComponent.SavedRemainDistance - remainDistance;

                    if (diffDistance > TrafficAntistuckConfigReference.Config.Value.StuckDistanceDiff)
                    {
                        SaveTimestamp(ref trafficStuckInfoComponent, CurrentTimestamp, remainDistance, in TrafficAntistuckConfigReference);
                    }
                    else
                    {
                        bool canPool = !TrafficAntistuckConfigReference.Config.Value.CullOutOfTheCameraOnly || !InViewOfCameraLookup.IsComponentEnabled(entity);
                        bool shouldPool = (CurrentTimestamp - trafficStuckInfoComponent.SavedTimestamp) > TrafficAntistuckConfigReference.Config.Value.ObstacleStuckTime;

                        if (canPool && shouldPool)
                        {
                            PoolEntityUtils.DestroyEntity(ref pooledEventTagRW);
                        }
                    }
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void SaveTimestamp(ref TrafficStuckInfoComponent trafficStuckInfoComponent, float currentTimestamp, float remainDistance, in TrafficAntistuckConfigReference trafficAntistuckConfigReference)
        {
            if (remainDistance != 0)
            {
                trafficStuckInfoComponent.SavedRemainDistance = remainDistance;
                float additionalTime = math.clamp(remainDistance / 5, 0, trafficAntistuckConfigReference.Config.Value.ObstacleStuckTime);
                trafficStuckInfoComponent.SavedTimestamp = currentTimestamp + additionalTime;
            }
        }
    }
}