using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Simulation.Car;
using Spirit604.DotsCity.Simulation.Road;
using Spirit604.DotsCity.Simulation.TrafficArea;
using Spirit604.Extensions;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using static Spirit604.DotsCity.Simulation.Traffic.Obstacle.TrafficObstacleChangeLaneUtils;
using static Spirit604.DotsCity.Simulation.Traffic.Obstacle.TrafficObstacleUtils;

namespace Spirit604.DotsCity.Simulation.Traffic.Obstacle
{
    [WithNone(typeof(TrafficChangingLaneEventTag), typeof(TrafficIdleTag), typeof(TrafficWagonComponent))]
    [WithAll(typeof(TrafficTag), typeof(HasDriverTag))]
    [BurstCompile]
    public partial struct ObstacleCalcJob : IJobEntity
    {
        [ReadOnly]
        public NativeParallelMultiHashMap<int, CarHashData> CarHashMapParallel;

        [ReadOnly]
        public ComponentLookup<TrafficChangeLaneComponent> TrafficChangeLaneLookup;

        [ReadOnly]
        public ComponentLookup<TrafficChangingLaneEventTag> TrafficCarChangingLaneEventLookup;

        [ReadOnly]
        public ComponentLookup<TrafficAreaAlignedTag> TrafficAreaAlignedLookup;

        [ReadOnly]
        public ComponentLookup<TrafficNodeLinkedComponent> TrafficNodeLinkedComponentLookup;

        [ReadOnly]
        public PathGraphSystem.Singleton Graph;

        [ReadOnly]
        public TrafficObstacleConfigReference TrafficObstacleConfigReference;

        [ReadOnly]
        public TrafficCommonSettingsConfigBlobReference TrafficCommonSettingsConfigBlobReference;

        [ReadOnly]
        public TrafficAvoidanceConfigReference TrafficAvoidanceConfigReference;

        [ReadOnly]
        public bool ChangeLaneSupported;

        void Execute(
            Entity currentEntity,
            ref TrafficObstacleComponent carObstacleComponent,
            in LocalTransform transform,
            in BoundsComponent boundsComponent,
            in TrafficStateComponent trafficStateComponent,
            in TrafficDestinationComponent destinationComponent,
            in TrafficPathComponent trafficPathComponent)
        {
            var currentPathIndex = trafficPathComponent.CurrentGlobalPathIndex;

            if (currentPathIndex == -1)
                return;

            ref var configRef = ref TrafficObstacleConfigReference.Config;

            var currentPosition = transform.Position;
            var raycastMode = TrafficCommonSettingsConfigBlobReference.Reference.Value.DetectObstacleMode == DetectObstacleMode.RaycastOnly;

            var inRangeOfSemaphore = trafficStateComponent.TrafficLightCarState == TrafficLightCarState.InRangeAndInitialized;

            var states = State.None;

            if (inRangeOfSemaphore)
            {
                states = DotsEnumExtension.AddFlag<State>(states, State.InRangeOfSemaphore);
            }

            var linked =
                TrafficNodeLinkedComponentLookup.HasComponent(currentEntity);


            if (TrafficAreaAlignedLookup.HasComponent(currentEntity) || linked)
            {
                states = DotsEnumExtension.AddFlag<State>(states, State.AtTrafficArea);
            }

            if (linked)
            {
                states = DotsEnumExtension.AddFlag<State>(states, State.ParkingCar);
            }

            var distanceToEnd = destinationComponent.DistanceToEndOfPath;

            var currentCarHashData = new CarHashData()
            {
                Entity = currentEntity,
                Position = currentPosition,
                Rotation = transform.Rotation,
                Forward = transform.Forward(),
                Bounds = boundsComponent,
                Destination = destinationComponent.Destination,
                PathIndex = trafficPathComponent.CurrentGlobalPathIndex,
                LocalPathNodeIndex = trafficPathComponent.LocalPathNodeIndex,
                Priority = trafficPathComponent.Priority,
                DistanceToEnd = distanceToEnd,
                DistanceToWaypoint = destinationComponent.DistanceToWaypoint,
                States = states
            };

            var approachData = ApproachData.GetDefault();

            var obstacleResult = TrafficObstacleDefaultPathUtils.CheckPathForObstacle(
                in CarHashMapParallel,
                in configRef,
                in currentCarHashData,
                raycastMode,
                ref approachData);

            if (ChangeLaneSupported && !obstacleResult.HasObstacle && !raycastMode)
            {
                obstacleResult = CheckPathForChangeLaneObstacle(
                    in CarHashMapParallel,
                    in configRef,
                    in currentCarHashData);
            }

            var avoidCrossroadJam = configRef.Value.AvoidCrossroadJam;

            var closeToEnd = distanceToEnd < configRef.Value.MinDistanceToCheckNextPath;

            if (!obstacleResult.HasObstacle && (closeToEnd || (avoidCrossroadJam && inRangeOfSemaphore)))
            {
                obstacleResult = CheckNextPaths(
                    currentEntity,
                    in CarHashMapParallel,
                    in Graph,
                    in configRef,
                    in currentCarHashData,
                    currentCarHashData.PathIndex,
                    avoidCrossroadJam,
                    raycastMode,
                    ref approachData,
                    closeToEnd: closeToEnd);
            }

            if (!obstacleResult.HasObstacle && !raycastMode)
            {
                obstacleResult = CheckForNeighbourPath(
                    in CarHashMapParallel,
                    in Graph,
                    in configRef,
                    in currentCarHashData);
            }

            if (!obstacleResult.HasObstacle)
            {
                obstacleResult = CheckForIntersectionPath(
                    in CarHashMapParallel,
                    in Graph,
                    in configRef,
                    in TrafficAvoidanceConfigReference,
                    in destinationComponent,
                    in currentCarHashData,
                    avoidCrossroadJam);
            }

            carObstacleComponent.ObstacleEntity = obstacleResult.ObstacleEntity;
            carObstacleComponent.ObstacleType = obstacleResult.ObstacleType;
            carObstacleComponent.ApproachSpeed = approachData.Speed;
            carObstacleComponent.ApproachType = approachData.ApproachType;
        }
    }
}
