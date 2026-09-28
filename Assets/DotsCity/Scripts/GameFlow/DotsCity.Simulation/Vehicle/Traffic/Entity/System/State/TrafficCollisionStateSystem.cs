using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Simulation.Road;
using Spirit604.Gameplay.Road;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Transforms;

namespace Spirit604.DotsCity.Simulation.Traffic
{
    [UpdateInGroup(typeof(MainThreadEventGroup))]
    [BurstCompile]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial struct TrafficCollisionStateSystem : ISystem
    {
        private EntityQuery updateGroup;

        [BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            updateGroup = SystemAPI.QueryBuilder()
                .WithAllRW<CarCollisionComponent, TrafficStateComponent>()
                .WithAllRW<TrafficAvoidanceComponent, TrafficObstacleComponent>()
                .WithPresentRW<TrafficIdleTag>()
                .WithAll<TrafficCollidedTag, TrafficTag, BoundsComponent, LocalTransform>()
                .Build();

            state.RequireForUpdate(updateGroup);
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var collisionStateJob = new CollisionStateJob()
            {
                CommandBuffer = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged),
                EventQueue = SystemAPI.GetSingleton<TrafficAvoidanceEventPlaybackSystem.Singleton>(),
                TrafficPathLookup = SystemAPI.GetComponentLookup<TrafficPathComponent>(true),
                TrafficChangingLaneEventLookup = SystemAPI.GetComponentLookup<TrafficChangingLaneEventTag>(true),
                Graph = SystemAPI.GetSingleton<PathGraphSystem.Singleton>(),
                TrafficAvoidanceConfigReference = SystemAPI.GetSingleton<TrafficAvoidanceConfigReference>(),
                TrafficGeneralSettingsReference = SystemAPI.GetSingleton<TrafficGeneralSettingsReference>(),
                TrafficCollisionConfigReference = SystemAPI.GetSingleton<TrafficCollisionConfigReference>(),
                Time = (float)SystemAPI.Time.ElapsedTime,
            };

            collisionStateJob.Run(updateGroup);
        }

        [BurstCompile]
        [WithAll(typeof(TrafficCollidedTag), typeof(TrafficTag))]
        public partial struct CollisionStateJob : IJobEntity
        {
            public EntityCommandBuffer CommandBuffer;

            [NativeDisableContainerSafetyRestriction]
            public TrafficAvoidanceEventPlaybackSystem.Singleton EventQueue;

            [ReadOnly]
            public ComponentLookup<TrafficPathComponent> TrafficPathLookup;

            [ReadOnly]
            public ComponentLookup<TrafficChangingLaneEventTag> TrafficChangingLaneEventLookup;

            [ReadOnly]
            public PathGraphSystem.Singleton Graph;

            [ReadOnly]
            public TrafficAvoidanceConfigReference TrafficAvoidanceConfigReference;

            [ReadOnly]
            public TrafficGeneralSettingsReference TrafficGeneralSettingsReference;

            [ReadOnly]
            public TrafficCollisionConfigReference TrafficCollisionConfigReference;

            [ReadOnly]
            public float Time;

            void Execute(
                Entity entity,
                ref CarCollisionComponent carCollisionComponent,
                ref TrafficStateComponent trafficStateComponent,
                ref TrafficAvoidanceComponent trafficAvoidanceComponent,
                ref TrafficObstacleComponent trafficObstacleComponent,
                EnabledRefRW<TrafficIdleTag> trafficIdleTagRW,
                in BoundsComponent boundsComponent,
                in LocalTransform transform)
            {
                float passedTime = Time - carCollisionComponent.CollisionTime;

                var canContinueMovement = passedTime > TrafficCollisionConfigReference.Config.Value.IdleDuration;

                if (!canContinueMovement && carCollisionComponent.SourceCollisionDirectionType != TrafficCollisionDirectionType.Front)
                {
                    canContinueMovement = true;
                }

                if (TrafficGeneralSettingsReference.Config.Value.AvoidanceSupport &&
                    TrafficCollisionConfigReference.Config.Value.AvoidStuckedCollision &&
                    (Time - carCollisionComponent.LastCalculation > TrafficCollisionConfigReference.Config.Value.CalculationCollisionFrequency) &&
                    carCollisionComponent.CollisionDuration > TrafficCollisionConfigReference.Config.Value.CollisionDuration)
                {
                    carCollisionComponent.LastCalculation = Time;

                    if (carCollisionComponent.SourceCollisionDirectionType == TrafficCollisionDirectionType.Front &&
                        TrafficPathLookup.HasComponent(carCollisionComponent.LastCollisionEntity))
                    {
                        bool hasAvoidance = false;
                        bool forwardMovement = true;

                        if (carCollisionComponent.SourceCollisionDirectionType == TrafficCollisionDirectionType.Front &&
                            carCollisionComponent.TargetCollisionDirectionType == TrafficCollisionDirectionType.Front)
                        {
                            hasAvoidance = true;
                        }

                        if (!hasAvoidance)
                        {
                            var sourcePathComponent = TrafficPathLookup[entity];
                            var targetPathComponent = TrafficPathLookup[carCollisionComponent.LastCollisionEntity];

                            forwardMovement = sourcePathComponent.PathDirection == PathForwardType.Forward;

                            if (sourcePathComponent.CurrentGlobalPathIndex != targetPathComponent.CurrentGlobalPathIndex)
                            {
                                var intersectedPaths = Graph.GetIntersectedPaths(sourcePathComponent.CurrentGlobalPathIndex);

                                for (int i = 0; i < intersectedPaths.Length; i++)
                                {
                                    if (intersectedPaths[i].IntersectedPathIndex == targetPathComponent.CurrentGlobalPathIndex)
                                    {
                                        hasAvoidance = true;
                                        break;
                                    }
                                }
                            }

                            if (!hasAvoidance)
                            {
                                if (sourcePathComponent.CurrentGlobalPathIndex == targetPathComponent.CurrentGlobalPathIndex &&
                                    carCollisionComponent.SourceCollisionDirectionType == TrafficCollisionDirectionType.Front)
                                {
                                    var changingLane = TrafficChangingLaneEventLookup.HasComponent(entity) && TrafficChangingLaneEventLookup.IsComponentEnabled(entity);

                                    if (changingLane)
                                    {
                                        hasAvoidance = true;
                                    }
                                }
                            }
                        }

                        if (hasAvoidance)
                        {
                            carCollisionComponent.LastCalculation = Time + TrafficCollisionConfigReference.Config.Value.RepeatAvoidanceFrequency;

                            canContinueMovement = true;

                            var sign = forwardMovement ? 1 : -1;
                            var destination = transform.Position - sign * transform.Forward() * (boundsComponent.Extents.z + boundsComponent.Extents.x + TrafficCollisionConfigReference.Config.Value.AvoidanceDistance);
                            var boundsPoint = forwardMovement ? VehicleBoundsPoint.BackwardPoint : VehicleBoundsPoint.ForwardPoint;

                            trafficObstacleComponent.IgnoreType = IgnoreType.Collision;

                            EventQueue.AddEvent(
                                 new TrafficAvoidanceEventPlaybackSystem.AvoidanceEventData()
                                 {
                                     Entity = entity,
                                     Destination = destination,
                                     VehicleBoundsPoint = boundsPoint,
                                     BackwardDirection = !forwardMovement,
                                     AchieveDistance = TrafficAvoidanceConfigReference.Config.Value.CustomAchieveDistance
                                 });

                            trafficAvoidanceComponent.State = AvoidanceState.WaitingForBackwardDestination;
                        }
                    }
                }

                if (canContinueMovement)
                {
                    carCollisionComponent.LastCollisionEntity = Entity.Null;
                    carCollisionComponent.LastIdleTime = Time;

                    TrafficStateExtension.RemoveIdleState<TrafficCollidedTag>(ref CommandBuffer, entity, ref trafficStateComponent, ref trafficIdleTagRW, TrafficIdleState.Collided);
                }
            }
        }
    }
}