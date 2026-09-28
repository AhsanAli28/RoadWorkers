using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Simulation.Car;
using Spirit604.Extensions;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using static Spirit604.DotsCity.Simulation.Traffic.Obstacle.TrafficObstacleUtils;

namespace Spirit604.DotsCity.Simulation.Traffic.Obstacle
{
    [WithAll(typeof(TrafficTag), typeof(HasDriverTag), typeof(TrafficChangingLaneEventTag))]
    [BurstCompile]
    public partial struct ChangeLaneObstacleCalcJob : IJobEntity
    {
        [ReadOnly]
        public NativeParallelMultiHashMap<int, CarHashData> CarHashMap;

        [ReadOnly]
        public NativeParallelMultiHashMap<int, CarChangeLaneEntityComponent> CarHashMapChangeLane;

        [ReadOnly]
        public TrafficObstacleConfigReference TrafficObstacleConfigReference;

        void Execute(
            Entity currentEntity,
            ref TrafficObstacleComponent carObstacleComponent,
            ref TrafficDestinationComponent destinationComponent,
            in TrafficPathComponent trafficPathComponent,
            in LocalTransform transform,
            in TrafficChangeLaneComponent trafficChangeLaneComponent,
            in BoundsComponent boundsComponent)
        {
            carObstacleComponent = TrafficObstacleComponent.GetDefault();

            ref var configRef = ref TrafficObstacleConfigReference.Config;

            var targetPathKey = trafficChangeLaneComponent.TargetPathGlobalIndex;

            var targetPathIndex = trafficChangeLaneComponent.TargetPathGlobalIndex;
            var currentPosition = transform.Position.Flat();

            float distanceToEnd = destinationComponent.DistanceToEndOfPath;

            float distanceToDstLane = math.distance(currentPosition, trafficChangeLaneComponent.Destination);

            bool notTooClose = distanceToDstLane > boundsComponent.Extents.z * 1.3f;

            if (notTooClose)
            {
                float customDistanceToObstacle = -1;

                if (distanceToDstLane < configRef.Value.CloseDistanceToChangeLanePoint)
                {
                    customDistanceToObstacle = configRef.Value.MaxDistanceToObstacleChangeLane;
                }

                var approachData = ApproachData.GetDefault();

                // Forward vehicle point
                var obstacleData = CheckPathForObstacle(
                    in CarHashMap,
                    in configRef,
                    currentEntity,
                    targetPathIndex,
                    currentPosition,
                    raycastMode: false,
                    ref approachData,
                    distanceToEnd,
                    customDistanceToObstacle,
                    backwardPointCheckObstacle: false,
                    currentCarChangingLane: true,
                    ignoreCycleObstacle: true);

                if (!obstacleData.HasObstacle)
                {
                    // Backward vehicle point
                    obstacleData = CheckPathForObstacle(
                        in CarHashMap,
                        in configRef,
                        currentEntity,
                        targetPathIndex,
                        currentPosition,
                        raycastMode: false,
                        ref approachData,
                        distanceToEnd,
                        customDistanceToObstacle,
                        backwardPointCheckObstacle: true,
                        currentCarChangingLane: true);
                }

                carObstacleComponent.ApproachType = approachData.ApproachType;
                carObstacleComponent.ApproachSpeed = approachData.Speed;

                if (obstacleData.HasObstacle)
                {
                    bool obstacleConflict = false;

                    if (CarHashMap.TryGetFirstValue(targetPathKey, out var targetCarHash2, out var nativeMultiHashMapIterator2))
                    {
                        do
                        {
                            if (targetCarHash2.Entity == currentEntity)
                                continue;

                            if (targetCarHash2.Entity == obstacleData.ObstacleEntity)
                            {
                                if (targetCarHash2.CurrentObstacleEntity == currentEntity)
                                {
                                    obstacleConflict = true;
                                }

                                break;
                            }

                        } while (CarHashMap.TryGetNextValue(out targetCarHash2, ref nativeMultiHashMapIterator2));
                    }

                    if (!obstacleConflict)
                    {
                        carObstacleComponent.ObstacleEntity = obstacleData.ObstacleEntity;
                        carObstacleComponent.ObstacleType = obstacleData.ObstacleType;
                    }

                    return;
                }
            }

            #region Calculate obstacle for few changing cars at the same time

            if (CarHashMapChangeLane.TryGetFirstValue(targetPathKey, out var targetCarHash, out var nativeMultiHashMapIterator))
            {
                do
                {
                    if (targetCarHash.Entity == currentEntity)
                        continue;

                    float otherCarDistanceToPoint = math.distance(trafficChangeLaneComponent.Destination, targetCarHash.Destination);

                    var closeChangePoints = otherCarDistanceToPoint < configRef.Value.MaxDistanceToObstacleChangeLane;

                    if (!closeChangePoints)
                        continue;

                    var otherCarCloserToDst = destinationComponent.DistanceToEndOfPath > targetCarHash.DistanceToEnd;

                    var samePath = trafficPathComponent.CurrentGlobalPathIndex == targetCarHash.SourcePathIndex;
                    bool hasPriority = trafficPathComponent.CurrentGlobalPathIndex > targetCarHash.SourcePathIndex;
                    bool otherHasObstacle = targetCarHash.ObstacleEntity != Entity.Null;

                    if ((!hasPriority && !samePath && !otherHasObstacle) || (otherCarCloserToDst && samePath))
                    {
                        carObstacleComponent.ObstacleEntity = targetCarHash.Entity;
                        carObstacleComponent.ObstacleType = ObstacleType.FewChangeLaneCars;
                        break;
                    }

                } while (CarHashMapChangeLane.TryGetNextValue(out targetCarHash, ref nativeMultiHashMapIterator));

                #endregion
            }
        }
    }
}
