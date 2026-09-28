using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Simulation.Car;
using Spirit604.DotsCity.Simulation.TrafficArea;
using Spirit604.Extensions;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Transforms;

namespace Spirit604.DotsCity.Simulation.Traffic.Obstacle
{
    [WithAll(typeof(TrafficTag))]
    [BurstCompile]
    public partial struct FillHashMapJob : IJobEntity
    {
        [NativeDisableContainerSafetyRestriction]
        public NativeParallelMultiHashMap<int, CarHashData>.ParallelWriter CarHashMapParallel;

        [NativeDisableContainerSafetyRestriction]
        public NativeParallelHashMap<Entity, CarHashData>.ParallelWriter CarHashMapObstacleParallel;

        [NativeDisableContainerSafetyRestriction]
        public NativeParallelMultiHashMap<int, CarChangeLaneEntityComponent>.ParallelWriter CarHashMapChangeLaneParallel;

        [ReadOnly]
        public ComponentLookup<TrafficChangeLaneComponent> TrafficChangeLaneLookup;

        [ReadOnly]
        public ComponentLookup<TrafficRaycastObstacleComponent> TrafficRaycastObstacleLookup;

        [ReadOnly]
        public ComponentLookup<TrafficChangingLaneEventTag> TrafficChangingLaneEventLookup;

        [ReadOnly]
        public ComponentLookup<TrafficAreaAlignedTag> TrafficAreaAlignedLookup;

        [ReadOnly]
        public ComponentLookup<TrafficNodeLinkedComponent> TrafficNodeLinkedComponentLookup;

        void Execute(
            Entity entity,
            in LocalTransform transform,
            in BoundsComponent boundsComponent,
            in SpeedComponent speedComponent,
            in TrafficStateComponent trafficStateComponent,
            in TrafficDestinationComponent destinationComponent,
            in TrafficPathComponent trafficPathComponent,
            in TrafficObstacleComponent trafficObstacleComponent)
        {
            int globalPathIndex = trafficPathComponent.CurrentGlobalPathIndex;

            bool inRangeOfSemaphore = trafficStateComponent.TrafficLightCarState == TrafficLightCarState.InRangeAndInitialized;
            bool isIdle = trafficStateComponent.TrafficIdleState != TrafficIdleState.Default;

            var states = State.None;

            if (isIdle)
            {
                states = DotsEnumExtension.AddFlag<State>(states, State.IsIdle);
            }

            Entity obstacleEntity = trafficObstacleComponent.ObstacleEntity;
            Entity rayObstacleEntity = Entity.Null;

            if (TrafficRaycastObstacleLookup.HasComponent(entity))
            {
                rayObstacleEntity = TrafficRaycastObstacleLookup[entity].ObstacleEntity;
            }

            if (obstacleEntity != Entity.Null || rayObstacleEntity != Entity.Null)
            {
                states = DotsEnumExtension.AddFlag<State>(states, State.HasObstacle);

                if (obstacleEntity != Entity.Null)
                {
                    states = DotsEnumExtension.AddFlag<State>(states, State.HasCalculatedObstacle);
                }
            }

            var changingLaneStates = states;

            if (inRangeOfSemaphore)
            {
                states = DotsEnumExtension.AddFlag<State>(states, State.InRangeOfSemaphore);
            }

            var linked = TrafficNodeLinkedComponentLookup.HasComponent(entity);

            if (TrafficAreaAlignedLookup.HasComponent(entity) || linked)
            {
                states = DotsEnumExtension.AddFlag<State>(states, State.AtTrafficArea);
            }

            if (linked && isIdle)
            {
                states = DotsEnumExtension.AddFlag<State>(states, State.ParkingCar);
            }

            var carHashEntityComponent = new CarHashData()
            {
                Entity = entity,
                Position = transform.Position,
                Rotation = transform.Rotation,
                Forward = transform.Forward(),
                Bounds = boundsComponent,
                Destination = destinationComponent.Destination,
                CurrentNodeIndex = destinationComponent.CurrentNode.Index,
                DestinationNodeIndex = destinationComponent.DestinationNode.Index,
                PathIndex = globalPathIndex,
                LocalPathNodeIndex = trafficPathComponent.LocalPathNodeIndex,
                Priority = trafficPathComponent.Priority,
                Speed = speedComponent.Value,
                DistanceToEnd = destinationComponent.DistanceToEndOfPath,
                DistanceToWaypoint = destinationComponent.DistanceToWaypoint,
                ObstacleEntity = obstacleEntity,
                RayObstacleEntity = rayObstacleEntity,
                States = states
            };

            CarHashMapParallel.Add(globalPathIndex, carHashEntityComponent);

            if (obstacleEntity != Entity.Null)
            {
                CarHashMapObstacleParallel.TryAdd(entity, carHashEntityComponent);
            }

            if (TrafficChangingLaneEventLookup.HasComponent(entity) && TrafficChangingLaneEventLookup.IsComponentEnabled(entity) && TrafficChangeLaneLookup.HasComponent(entity))
            {
                changingLaneStates = DotsEnumExtension.AddFlag<State>(changingLaneStates, State.ChangingLane);

                var trafficChangeLaneComponent = TrafficChangeLaneLookup[entity];

                var carHashChangeLaneEntityComponent = new CarHashData()
                {
                    Entity = entity,
                    Position = transform.Position,
                    Rotation = transform.Rotation,
                    Destination = trafficChangeLaneComponent.Destination,
                    PathIndex = globalPathIndex,
                    LocalPathNodeIndex = trafficPathComponent.LocalPathNodeIndex,
                    Priority = trafficPathComponent.Priority,
                    Speed = speedComponent.Value,
                    DistanceToEnd = destinationComponent.DistanceToEndOfPath,
                    DistanceToWaypoint = destinationComponent.DistanceToWaypoint,
                    ObstacleEntity = obstacleEntity,
                    RayObstacleEntity = rayObstacleEntity,
                    States = changingLaneStates
                };

                var carChangeLaneEntityComponent = new CarChangeLaneEntityComponent()
                {
                    Entity = entity,
                    Position = transform.Position,
                    Rotation = transform.Rotation,
                    Destination = trafficChangeLaneComponent.Destination,
                    SourcePathIndex = trafficPathComponent.CurrentGlobalPathIndex,
                    DistanceToEnd = destinationComponent.DistanceToEndOfPath,
                    ObstacleEntity = obstacleEntity,
                };

                CarHashMapParallel.Add(trafficChangeLaneComponent.TargetPathGlobalIndex, carHashChangeLaneEntityComponent);
                CarHashMapChangeLaneParallel.Add(trafficChangeLaneComponent.TargetPathGlobalIndex, carChangeLaneEntityComponent);
            }
        }
    }
}
