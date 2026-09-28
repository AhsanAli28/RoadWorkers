using Spirit604.DotsCity.Simulation.Car;
using Spirit604.DotsCity.Simulation.Road;
using Spirit604.DotsCity.Simulation.Traffic.Obstacle;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Spirit604.DotsCity.Simulation.Traffic
{
    [UpdateInGroup(typeof(SimulationGroup))]
    [BurstCompile]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial struct TrafficAvoidanceSystem : ISystem
    {
        private EntityQuery updateQuery;

        [BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            updateQuery = SystemAPI.QueryBuilder()
                .WithNone<TrafficChangingLaneEventTag>()
                .WithAll<HasDriverTag, TrafficAvoidanceComponent>()
                .Build();

            state.RequireForUpdate(updateQuery);
            state.RequireForUpdate<TrafficObstacleSystem.Singleton>();
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var avoidanceJob = new AvoidanceJob()
            {
                EventQueue = SystemAPI.GetSingleton<TrafficAvoidanceEventPlaybackSystem.Singleton>(),
                TrafficNodeSettingsLookup = SystemAPI.GetComponentLookup<TrafficNodeSettingsComponent>(true),
                TrafficCustomTargetDataLookup = SystemAPI.GetComponentLookup<TrafficCustomDestinationComponent>(true),
                PathConnectionLookup = SystemAPI.GetBufferLookup<PathConnectionElement>(true),
                TrafficAvoidanceConfigReference = SystemAPI.GetSingleton<TrafficAvoidanceConfigReference>(),
                ObstacleSystemSingleton = SystemAPI.GetSingleton<TrafficObstacleSystem.Singleton>(),
            };

            avoidanceJob.ScheduleParallel();
        }

        [WithNone(typeof(TrafficChangingLaneEventTag))]
        [WithAll(typeof(HasDriverTag))]
        [BurstCompile]
        private partial struct AvoidanceJob : IJobEntity
        {
            [NativeDisableParallelForRestriction]
            public TrafficAvoidanceEventPlaybackSystem.Singleton EventQueue;

            [ReadOnly]
            public ComponentLookup<TrafficNodeSettingsComponent> TrafficNodeSettingsLookup;

            [ReadOnly]
            public ComponentLookup<TrafficCustomDestinationComponent> TrafficCustomTargetDataLookup;

            [ReadOnly]
            public BufferLookup<PathConnectionElement> PathConnectionLookup;

            [ReadOnly]
            public TrafficAvoidanceConfigReference TrafficAvoidanceConfigReference;

            [ReadOnly]
            public TrafficObstacleSystem.Singleton ObstacleSystemSingleton;

            private NativeParallelMultiHashMap<int, CarHashData> CarHashMap => ObstacleSystemSingleton.CarHashMap;

            private NativeParallelHashMap<Entity, CarHashData> CarObstacleHashMap => ObstacleSystemSingleton.CarObstacleHashMap;

            private void Execute(
                Entity entity,
                ref TrafficObstacleComponent trafficObstacleComponent,
                ref TrafficAvoidanceComponent trafficAvoidanceComponent)
            {
                switch (trafficAvoidanceComponent.State)
                {
                    case AvoidanceState.Default:
                        {
                            if (!CarObstacleHashMap.ContainsKey(trafficObstacleComponent.ObstacleEntity) || !CarObstacleHashMap.ContainsKey(entity))
                                return;

                            var sourceObstacleData = CarObstacleHashMap[entity];
                            var targetObstacleData = CarObstacleHashMap[trafficObstacleComponent.ObstacleEntity];

                            var isCyclicalObstacle = sourceObstacleData.PathIndex != targetObstacleData.PathIndex && targetObstacleData.ObstacleEntity == entity && sourceObstacleData.ObstacleEntity == targetObstacleData.Entity;

                            if (isCyclicalObstacle && TrafficAvoidanceConfigReference.Config.Value.ResolveCyclicObstacle)
                            {
                                var targetIsCloser = targetObstacleData.DistanceToEnd < sourceObstacleData.DistanceToEnd;
                                bool backwardIsAvailable = false;

                                float3 destination;
                                var sourceAvailable = CheckBackwardAvailability(sourceObstacleData.PathIndex, sourceObstacleData, out destination);

                                if (targetIsCloser)
                                {
                                    backwardIsAvailable = sourceAvailable;
                                }
                                else
                                {
                                    var targetAvailable = CheckBackwardAvailability(targetObstacleData.PathIndex, targetObstacleData, out var destination1);

                                    if (sourceAvailable && !targetAvailable)
                                    {
                                        backwardIsAvailable = true;
                                    }
                                }

                                if (backwardIsAvailable)
                                {
                                    trafficObstacleComponent.IgnoreType = IgnoreType.Avoidance;

                                    EventQueue.AddEvent(
                                        new TrafficAvoidanceEventPlaybackSystem.AvoidanceEventData()
                                        {
                                            Entity = entity,
                                            Destination = destination,
                                            CustomProcess = true,
                                            VehicleBoundsPoint = VehicleBoundsPoint.BackwardPoint,
                                            AchieveDistance = TrafficAvoidanceConfigReference.Config.Value.CustomAchieveDistance
                                        });

                                    trafficAvoidanceComponent.State = AvoidanceState.WaitingForBackwardDestination;
                                    return;
                                }
                            }

                            break;
                        }
                    case AvoidanceState.WaitingForBackwardDestination:
                        {
                            bool removeState = false;
                            bool hasComponent = false;

                            if (trafficObstacleComponent.ObstacleEntity == Entity.Null)
                            {
                                removeState = true;
                            }

                            if (TrafficCustomTargetDataLookup.HasComponent(entity))
                            {
                                hasComponent = true;
                                var data = TrafficCustomTargetDataLookup[entity];

                                if (!removeState)
                                {
                                    removeState = data.Passed;
                                }
                            }

                            if (removeState)
                            {
                                if (hasComponent)
                                {
                                    EventQueue.RemoveEventTag(entity);
                                }

                                trafficObstacleComponent.IgnoreType = IgnoreType.None;
                                trafficAvoidanceComponent.State = AvoidanceState.Default;
                            }

                            break;
                        }
                }
            }

            private bool CheckBackwardAvailability(int pathIndex, CarHashData sourceCarHashData, out float3 destination)
            {
                destination = sourceCarHashData.BackwardPoint - sourceCarHashData.Forward * sourceCarHashData.Bounds.Size.x;

                if (CarHashMap.CountValuesForKey(pathIndex) > 1)
                {
                    if (CarHashMap.TryGetFirstValue(pathIndex, out var targetCarHash, out var nativeMultiHashMapIterator))
                    {
                        do
                        {
                            if (sourceCarHashData.Entity == targetCarHash.Entity)
                                continue;

                            //target car in front of current
                            if (targetCarHash.DistanceToEnd < sourceCarHashData.DistanceToEnd)
                                continue;

                            var dir1 = math.normalize(destination - targetCarHash.ForwardPoint);
                            var dir2 = math.normalize(sourceCarHashData.Position - targetCarHash.ForwardPoint);

                            var dot = math.dot(dir1, dir2);

                            if (dot < 0)
                                return false;

                        } while (CarHashMap.TryGetNextValue(out targetCarHash, ref nativeMultiHashMapIterator));
                    }
                }

                return true;
            }
        }
    }
}