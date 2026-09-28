using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Simulation.Car;
using Spirit604.DotsCity.Simulation.TrafficArea;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Spirit604.DotsCity.Simulation.Traffic
{
    [UpdateInGroup(typeof(PreEarlyJobGroup))]
    [BurstCompile]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial struct TrafficMonoCollisionSystem : ISystem
    {
        private EntityQuery trafficGroup;

        [BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            trafficGroup = SystemAPI.QueryBuilder()
                .WithNone<TrafficCustomDestinationComponent>()
                .WithAll<HasDriverTag, TrafficMonoStuckInfoComponent>()
                .Build();

            state.RequireForUpdate(trafficGroup);
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var startAvoidanceJob = new StartAvoidanceJob()
            {
                EventQueue = SystemAPI.GetSingleton<TrafficAvoidanceEventPlaybackSystem.Singleton>().EventQueue.AsParallelWriter(),
                TrafficCollisionConfigReference = SystemAPI.GetSingleton<TrafficCollisionConfigReference>(),
                TrafficAreaAlignedTagLookup = SystemAPI.GetComponentLookup<TrafficAreaAlignedTag>(true),
                CurrentTimestamp = (float)SystemAPI.Time.ElapsedTime,
            };

            startAvoidanceJob.ScheduleParallel();
        }

        [WithNone(typeof(TrafficPlayerControlTag), typeof(TrafficCustomDestinationComponent), typeof(TrafficInitTag), typeof(TrafficCustomMovementTag))]
        [WithAll(typeof(TrafficTag), typeof(HasDriverTag))]
        [BurstCompile]
        public partial struct StartAvoidanceJob : IJobEntity
        {
            [NativeDisableContainerSafetyRestriction]
            public UnsafeQueue<TrafficAvoidanceEventPlaybackSystem.AvoidanceEventData>.ParallelWriter EventQueue;

            [ReadOnly]
            public TrafficCollisionConfigReference TrafficCollisionConfigReference;

            [ReadOnly]
            public ComponentLookup<TrafficAreaAlignedTag> TrafficAreaAlignedTagLookup;

            [ReadOnly]
            public float CurrentTimestamp;

            void Execute(
                Entity entity,
                ref TrafficMonoStuckInfoComponent trafficMonoStuckInfoComponent,
                ref TrafficObstacleComponent trafficObstacleComponent,
                ref TrafficAvoidanceComponent trafficAvoidanceComponent,
                in TrafficPathComponent trafficPathComponent,
                in VehicleInputReader vehicleInputReader,
                in BoundsComponent boundsComponent,
                in TrafficDestinationComponent destinationComponent,
                in LocalTransform transform)
            {
                if (trafficMonoStuckInfoComponent.NextTime > CurrentTimestamp)
                    return;

                if (trafficMonoStuckInfoComponent.Activated)
                {
                    trafficMonoStuckInfoComponent.Activated = false;
                    trafficMonoStuckInfoComponent.NextTime = CurrentTimestamp + TrafficCollisionConfigReference.Config.Value.PostActivationDelay;
                    return;
                }

                float remainDistance = destinationComponent.DistanceToEndOfPath;
                bool stuck = false;
                bool reset = true;

                bool areaTraffic = TrafficAreaAlignedTagLookup.HasComponent(entity);

                if (vehicleInputReader.Throttle != 0)
                {
                    if (trafficMonoStuckInfoComponent.SavedRemainDistance == 0)
                    {
                        trafficMonoStuckInfoComponent.SavedRemainDistance = remainDistance;
                        trafficMonoStuckInfoComponent.SavedTimestamp = CurrentTimestamp;
                    }

                    float diff = math.abs(trafficMonoStuckInfoComponent.SavedRemainDistance - remainDistance);

                    var stuckDistance = !areaTraffic ? TrafficCollisionConfigReference.Config.Value.StuckDistance : 0.2f;
                    var stuckDuration = !areaTraffic ? TrafficCollisionConfigReference.Config.Value.StuckDuration : TrafficCollisionConfigReference.Config.Value.StuckDuration + 2;

                    if (diff < stuckDistance && remainDistance > 3f)
                    {
                        if (CurrentTimestamp - trafficMonoStuckInfoComponent.SavedTimestamp > stuckDuration)
                        {
                            stuck = true;
                        }
                        else
                        {
                            reset = false;
                        }
                    }
                }

                if (reset || stuck)
                {
                    if (trafficMonoStuckInfoComponent.SavedRemainDistance != 0)
                    {
                        trafficMonoStuckInfoComponent.SavedRemainDistance = 0;
                        trafficMonoStuckInfoComponent.SavedTimestamp = 0;
                    }
                }

                if (stuck)
                {
                    trafficMonoStuckInfoComponent.Activated = true;
                    var forwardMovement = trafficPathComponent.PathDirection == Gameplay.Road.PathForwardType.Forward;
                    var sign = forwardMovement ? 1 : -1;
                    var destination = transform.Position - sign * transform.Forward() * (boundsComponent.Extents.z + boundsComponent.Extents.x + TrafficCollisionConfigReference.Config.Value.AvoidanceDistance);
                    var boundsPoint = forwardMovement ? VehicleBoundsPoint.BackwardPoint : VehicleBoundsPoint.ForwardPoint;

                    trafficObstacleComponent.IgnoreType = IgnoreType.Collision;

                    EventQueue.Enqueue(
                         new TrafficAvoidanceEventPlaybackSystem.AvoidanceEventData()
                         {
                             Entity = entity,
                             Destination = destination,
                             VehicleBoundsPoint = boundsPoint,
                             BackwardDirection = !forwardMovement,
                             CustomDuration = TrafficCollisionConfigReference.Config.Value.ReverseDriveMaxDuration
                         });

                    trafficAvoidanceComponent.State = AvoidanceState.WaitingForBackwardDestination;
                }
            }
        }
    }
}