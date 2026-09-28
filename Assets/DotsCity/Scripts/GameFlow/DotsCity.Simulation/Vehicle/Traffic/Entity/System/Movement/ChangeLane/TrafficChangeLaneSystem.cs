using Spirit604.DotsCity.Simulation.Car;
using Spirit604.DotsCity.Simulation.Level.Streaming;
using Spirit604.DotsCity.Simulation.Road;
using Spirit604.DotsCity.Simulation.Traffic.Obstacle;
using Spirit604.Extensions;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Spirit604.DotsCity.Simulation.Traffic
{
    [UpdateAfter(typeof(TrafficObstacleSystem))]
    [UpdateInGroup(typeof(TrafficSimulationGroup))]
    [BurstCompile]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial struct TrafficChangeLaneSystem : ISystem
    {
        private EntityQuery updateQuery;

        [BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            updateQuery = SystemAPI.QueryBuilder()
                .WithNone<TrafficChangingLaneEventTag>()
                .WithAll<TrafficChangeLaneComponent>()
                .Build();

            state.RequireForUpdate(updateQuery);
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            #region Change lane logic depending on car count at current lane

            var changeLaneJob = new ChangeLaneJob()
            {
                ObstacleSystemSingleton = SystemAPI.GetSingleton<TrafficObstacleSystem.Singleton>(),
                RuntimePathDataRef = SystemAPI.GetSingleton<TrafficNodeResolverSystem.RuntimePathDataRef>(),
                SpeedLookup = SystemAPI.GetComponentLookup<SpeedComponent>(true),
                TrafficSettingsComponentLookup = SystemAPI.GetComponentLookup<TrafficSettingsComponent>(true),
                Graph = SystemAPI.GetSingleton<PathGraphSystem.Singleton>(),
                TrafficChangeLaneConfigReference = SystemAPI.GetSingleton<TrafficChangeLaneConfigReference>(),
                TrafficCommonSettingsConfigBlobReference = SystemAPI.GetSingleton<TrafficCommonSettingsConfigBlobReference>(),
                CurrentTime = (float)SystemAPI.Time.ElapsedTime
            };

            changeLaneJob.ScheduleParallel();

            #endregion
        }

        [WithDisabled(typeof(TrafficChangingLaneEventTag))]
        [WithNone(typeof(TrafficChangeLaneRequestedPositionComponent), typeof(TrafficIdleTag), typeof(TrafficCustomDestinationComponent))]
        [WithAll(typeof(TrafficDefaultTag), typeof(HasDriverTag))]
        [BurstCompile]
        public partial struct ChangeLaneJob : IJobEntity
        {
            [ReadOnly]
            public TrafficObstacleSystem.Singleton ObstacleSystemSingleton;

            [ReadOnly]
            public TrafficNodeResolverSystem.RuntimePathDataRef RuntimePathDataRef;

            [ReadOnly]
            public ComponentLookup<SpeedComponent> SpeedLookup;

            [ReadOnly]
            public ComponentLookup<TrafficSettingsComponent> TrafficSettingsComponentLookup;

            [ReadOnly]
            public PathGraphSystem.Singleton Graph;

            [ReadOnly]
            public TrafficObstacleConfigReference TrafficObstacleConfigReference;

            [ReadOnly]
            public TrafficChangeLaneConfigReference TrafficChangeLaneConfigReference;

            [ReadOnly]
            public TrafficCommonSettingsConfigBlobReference TrafficCommonSettingsConfigBlobReference;

            [ReadOnly]
            public float CurrentTime;

            void Execute(
                Entity entity,
                [ChunkIndexInQuery] int entityInQueryIndex,
                ref TrafficChangeLaneComponent trafficChangeLaneComponent,
                ref TrafficTargetDirectionComponent trafficTargetDirectionComponent,
                ref TrafficPathComponent trafficPathComponent,
                ref TrafficDestinationComponent destinationComponent,

#if UNITY_EDITOR
                ref TrafficChangeLaneDebugInfoComponent trafficChangeLaneDebugInfoComponent,
#endif

                EnabledRefRW<TrafficChangingLaneEventTag> trafficChangingLaneEventTagRW,
                in TrafficTypeComponent trafficTypeComponent,
                in LocalTransform transform)
            {
                ref var configRef = ref TrafficObstacleConfigReference.Config;
                ref var changeLaneRef = ref TrafficChangeLaneConfigReference.Config;

                if (trafficChangeLaneComponent.CheckTimeStamp > CurrentTime)
                    return;

                var rnd = UnityMathematicsExtension.GetRandomGen(CurrentTime, entityInQueryIndex, transform.Position);
                var randomDelay = rnd.NextFloat(changeLaneRef.Value.MinCheckFrequency, changeLaneRef.Value.MaxCheckFrequency);

                if (trafficChangeLaneComponent.CheckTimeStamp != 0)
                {
                    trafficChangeLaneComponent.CheckTimeStamp += randomDelay;
                }
                else
                {
                    trafficChangeLaneComponent.CheckTimeStamp = CurrentTime + randomDelay;
                    return;
                }

                int currentPathIndex = trafficPathComponent.CurrentGlobalPathIndex;

                if (!Graph.Exist(currentPathIndex))
                    return;

                var parallelPaths = Graph.GetParallelPaths(currentPathIndex);

                if (parallelPaths.Length == 0)
                    return;

                float3 carPosition = transform.Position;
                float3 currentTargetPosition = destinationComponent.Destination;
                float remainDistanceToEndOfPath = destinationComponent.DistanceToEndOfPath;

#if UNITY_EDITOR
                trafficChangeLaneDebugInfoComponent.RemainDistanceToEndOfPath = remainDistanceToEndOfPath;
                trafficChangeLaneDebugInfoComponent.ShouldChangeLane = 0;
                trafficChangeLaneComponent.DistanceToOtherCarsInNeighborLane = 9999f;
                trafficChangeLaneComponent.TargetCarDirection = 0;
#endif

                bool farEnoughToChangeLane = remainDistanceToEndOfPath > changeLaneRef.Value.MaxDistanceToEndOfPath;

#if !UNITY_EDITOR
                if (!farEnoughToChangeLane)
                {
                    return;
                }
#endif

                ref var carHashMap = ref ObstacleSystemSingleton.CarHashMap;

                bool shouldChangeLane = false;

                int targetPathKey = -1;

                float minDistance = float.MaxValue;
                var forward = transform.Forward();

                Entity lastCarEntity = default;

                int currentLaneCarsCount = GetLaneCarCount(ref carHashMap, forward, transform.Position, entity, currentPathIndex, ref minDistance, ref lastCarEntity);

#if UNITY_EDITOR
                trafficChangeLaneDebugInfoComponent.CurrentLaneCarCount = currentLaneCarsCount;
                trafficChangeLaneDebugInfoComponent.NeighborLaneCarCount = 999;
#endif

                if (changeLaneRef.Value.RandomChangeLaneChance > 0 && CurrentTime >= trafficChangeLaneComponent.RandomEventTimeStamp)
                {
                    if (trafficChangeLaneComponent.RandomEventTimeStamp == 0)
                    {
                        trafficChangeLaneComponent.RandomEventTimeStamp = CurrentTime + changeLaneRef.Value.RandomChangeLaneFrequency;
                    }
                    else
                    {
                        trafficChangeLaneComponent.RandomEventTimeStamp = CurrentTime + changeLaneRef.Value.RandomChangeLaneFrequency;
                        var randomGen = UnityMathematicsExtension.GetRandomGen(CurrentTime, entity);
                        shouldChangeLane = UnityMathematicsExtension.ChanceDropped(changeLaneRef.Value.RandomChangeLaneChance, randomGen);
                    }
                }

                bool closeEnoughToLastCar = minDistance < changeLaneRef.Value.MinDistanceToLastCarInLane;
                bool laneHasOtherCars = currentLaneCarsCount >= changeLaneRef.Value.MinCarsToChangeLane;

                if (laneHasOtherCars && closeEnoughToLastCar && !shouldChangeLane)
                {
                    int maxCarCount = int.MaxValue;

                    for (int i = 0; i < parallelPaths.Length; i++)
                    {
                        int tempTargetPathKey = parallelPaths[i];

                        if (!Graph.IsAvailable(tempTargetPathKey, in trafficTypeComponent))
                            continue;

                        int neighbourLaneCarsCount = GetLaneCarCount(ref carHashMap, forward, transform.Position, entity, tempTargetPathKey);

#if UNITY_EDITOR
                        if (trafficChangeLaneDebugInfoComponent.NeighborLaneCarCount > neighbourLaneCarsCount)
                        {
                            trafficChangeLaneDebugInfoComponent.NeighborLaneCarCount = neighbourLaneCarsCount;
                        }
#endif

                        int difference = currentLaneCarsCount - neighbourLaneCarsCount;

                        if (difference >= changeLaneRef.Value.MinCarDiffToChangeLane && maxCarCount > neighbourLaneCarsCount)
                        {
                            maxCarCount = neighbourLaneCarsCount;
                            targetPathKey = parallelPaths[i];
                            shouldChangeLane = true;
                        }
                    }

                    if (!shouldChangeLane && changeLaneRef.Value.MaxSpeedDiff > 0 && lastCarEntity != Entity.Null)
                    {
                        var currentSpeedCar = SpeedLookup[entity];
                        var frontSpeedCar = SpeedLookup[lastCarEntity];

                        var speedDiff = currentSpeedCar.Value - frontSpeedCar.Value;

                        if (speedDiff >= changeLaneRef.Value.MaxSpeedDiff)
                        {
                            shouldChangeLane = true;
                        }
                        else
                        {
                            var currentCarSettings = TrafficSettingsComponentLookup[entity];
                            var frontCarSettings = TrafficSettingsComponentLookup[lastCarEntity];

                            speedDiff = currentCarSettings.MaxSpeed - frontCarSettings.MaxSpeed;

                            // If the vehicle in front is slower than the current
                            if (speedDiff >= changeLaneRef.Value.MaxSpeedDiff && currentSpeedCar.LaneLimit > frontCarSettings.MaxSpeed)
                            {
                                shouldChangeLane = true;
                            }
                        }
                    }
                }

                if (shouldChangeLane && targetPathKey == -1)
                {
                    int maxCarCount = int.MaxValue;

                    for (int i = 0; i < parallelPaths.Length; i++)
                    {
                        int tempTargetPathKey = parallelPaths[i];

                        if (!Graph.IsAvailable(tempTargetPathKey, in trafficTypeComponent))
                            continue;

                        int neighbourLaneCarsCount = GetLaneCarCount(ref carHashMap, forward, transform.Position, entity, tempTargetPathKey);

#if UNITY_EDITOR
                        if (trafficChangeLaneDebugInfoComponent.NeighborLaneCarCount > neighbourLaneCarsCount)
                        {
                            trafficChangeLaneDebugInfoComponent.NeighborLaneCarCount = neighbourLaneCarsCount;
                        }
#endif

                        if (maxCarCount > neighbourLaneCarsCount)
                        {
                            maxCarCount = neighbourLaneCarsCount;
                            targetPathKey = parallelPaths[i];
                            shouldChangeLane = true;
                        }
                    }
                }

                if (!shouldChangeLane)
                {
                    return;
                }

                if (!farEnoughToChangeLane)
                {
                    return;
                }

                var localNodeIndex = trafficPathComponent.LocalPathNodeIndex;

                if (localNodeIndex == 0)
                {
                    return;
                }

                int targetPathNodeIndex;
                float3 destination;

                var found = TrafficChangeLaneUtils.GetTargetLanePositionAndIndex(
                    SpeedLookup[entity].Value,
                    transform.Position,
                    ref Graph,
                    ref TrafficChangeLaneConfigReference,
                    ref TrafficCommonSettingsConfigBlobReference,
                    currentPathIndex,
                    targetPathKey,
                    localNodeIndex - 1,
                    out targetPathNodeIndex,
                    out destination);

                if (!found)
                    return;

                if (targetPathNodeIndex == 0)
                    return;

                ref readonly var targetPathData = ref Graph.GetPathData(targetPathKey);

                bool intersectedPathFarEnough = true;

                if (changeLaneRef.Value.CheckTheIntersectedPaths)
                {
                    var intersectedPaths = Graph.GetIntersectedPaths(targetPathKey);

                    bool hasCars = true;

                    for (int i = 0; i < intersectedPaths.Length; i++)
                    {
                        var intersectedPath = intersectedPaths[i];

                        if (changeLaneRef.Value.IgnoreEmptyIntersects)
                        {
                            hasCars = HasAnyCar(ref carHashMap, intersectedPath.IntersectedPathIndex);
                        }

                        if (hasCars)
                        {
                            var intersectPosition = intersectedPath.IntersectPosition;

                            float distanceToIntersectedPointSQ = math.distancesq(destination, intersectPosition);

                            if (distanceToIntersectedPointSQ <= changeLaneRef.Value.MaxDistanceToIntersectedPathSQ)
                            {
                                intersectedPathFarEnough = false;
                                break;
                            }
                        }
                    }
                }

                bool targetNodeIsAvailable = true;

                if (intersectedPathFarEnough && targetPathData.HasOption(PathOptions.HasCustomNode))
                {
                    targetNodeIsAvailable = Graph.IsAvailable(in targetPathData, targetPathNodeIndex - 1, in trafficTypeComponent);
                }

                if (intersectedPathFarEnough && targetNodeIsAvailable)
                {
                    var targetLaneEntity = RuntimePathDataRef.TryToGetSourceNode(targetPathKey);

                    var canChangeLane = TrafficChangeLaneUtils.CalcTargetLaneObstacles(
                        entity,
                        destination,
                        targetPathKey,
                        targetPathNodeIndex,
                        targetLaneEntity,
                        TrafficCommonSettingsConfigBlobReference.Reference.Value.DefaultLaneSpeed,
                        ref destinationComponent,
                        ref trafficPathComponent,
                        ref trafficChangeLaneComponent,
                        ref trafficTargetDirectionComponent,
                        in transform,
                        in carHashMap,
                        in changeLaneRef);

                    if (canChangeLane)
                    {
                        trafficChangingLaneEventTagRW.ValueRW = true;
                    }
                }
            }
        }

        #region Helper methods

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetLaneCarCount(ref NativeParallelMultiHashMap<int, CarHashData> carHashMap, float3 forward, float3 carPosition, Entity sourceEntity, int pathKey, ref float minDistance, ref Entity lastCarEntity)
        {
            int carCount = 0;

            forward = forward.Flat();
            lastCarEntity = Entity.Null;

            if (carHashMap.TryGetFirstValue(pathKey, out var carHashEntity, out var nativeMultiHashMapIterator))
            {
                do
                {
                    if (carHashEntity.Entity == sourceEntity)
                    {
                        continue;
                    }

                    float targetCarDirection = math.dot(forward, carHashEntity.Position.Flat() - carPosition);

                    if (targetCarDirection > 0)
                    {
                        carCount++;

                        float distance = math.distancesq(carHashEntity.Position, carPosition);

                        if (distance < minDistance)
                        {
                            lastCarEntity = carHashEntity.Entity;
                            minDistance = distance;
                        }
                    }

                } while (carHashMap.TryGetNextValue(out carHashEntity, ref nativeMultiHashMapIterator));
            }

            minDistance = math.sqrt(minDistance);

            return carCount;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetLaneCarCount(ref NativeParallelMultiHashMap<int, CarHashData> carHashMap, float3 forward, float3 carPosition, Entity sourceEntity, int pathKey)
        {
            int carCount = 0;

            carPosition = carPosition.Flat();
            forward = forward.Flat();

            if (carHashMap.TryGetFirstValue(pathKey, out var carHashEntity, out var nativeMultiHashMapIterator))
            {
                do
                {
                    if (carHashEntity.Entity == sourceEntity)
                    {
                        continue;
                    }

                    float targetCarDirection = math.dot(forward, carHashEntity.Position.Flat() - carPosition);

                    if (targetCarDirection > 0)
                    {
                        carCount++;
                    }

                } while (carHashMap.TryGetNextValue(out carHashEntity, ref nativeMultiHashMapIterator));
            }

            return carCount;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool HasAnyCar(ref NativeParallelMultiHashMap<int, CarHashData> carHashMap, int pathKey) => carHashMap.TryGetFirstValue(pathKey, out var carHashEntity, out var nativeMultiHashMapIterator);

        #endregion
    }
}