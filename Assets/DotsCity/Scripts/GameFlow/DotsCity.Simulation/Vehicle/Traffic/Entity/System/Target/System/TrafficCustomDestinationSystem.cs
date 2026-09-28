using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Simulation.Car;
using Spirit604.Extensions;
using Spirit604.Gameplay.Road;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Spirit604.DotsCity.Simulation.Traffic
{
    [UpdateInGroup(typeof(SimulationGroup))]
    [BurstCompile]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial struct TrafficCustomDestinationSystem : ISystem
    {
        private const float SidePointDotValue = 0.4f;

        private EntityQuery updateQuery;

        [BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            updateQuery = SystemAPI.QueryBuilder()
                .WithAll<HasDriverTag, TrafficCustomDestinationComponent>()
                .Build();

            state.RequireForUpdate(updateQuery);
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var customDestinationJob = new CustomDestinationJob()
            {
                EventQueue = SystemAPI.GetSingleton<TrafficAvoidanceEventPlaybackSystem.Singleton>(),
                TrafficCustomDestinationConfigReference = SystemAPI.GetSingleton<TrafficCustomDestinationConfigReference>(),
                Time = (float)SystemAPI.Time.ElapsedTime
            };

            customDestinationJob.Schedule();
        }

        [WithNone(typeof(TrafficSwitchTargetNodeRequestTag))]
        [WithAll(typeof(HasDriverTag))]
        [BurstCompile]
        private partial struct CustomDestinationJob : IJobEntity
        {
            [NativeDisableContainerSafetyRestriction]
            public TrafficAvoidanceEventPlaybackSystem.Singleton EventQueue;

            [ReadOnly]
            public TrafficCustomDestinationConfigReference TrafficCustomDestinationConfigReference;

            [ReadOnly]
            public float Time;

            private void Execute(
                 Entity entity,
                 ref TrafficPathComponent trafficPathComponent,
                 ref TrafficCustomDestinationComponent customDestinationComponent,
                 ref SpeedComponent speedComponent,
                 in BoundsComponent boundsComponent,
                 in LocalTransform transform)
            {
                if (customDestinationComponent.Passed)
                    return;

                float3 carPosition = transform.Position;

                var directionToNode = math.normalize(customDestinationComponent.Destination - carPosition);

                if (!customDestinationComponent.Init)
                {
                    customDestinationComponent.Init = true;
                    customDestinationComponent.PreviousDestination = trafficPathComponent.DestinationWayPoint;
                    customDestinationComponent.PreviousSpeedLimit = speedComponent.LaneLimit;
                    trafficPathComponent.DestinationWayPoint = customDestinationComponent.Destination;
                    speedComponent.LaneLimit = TrafficCustomDestinationConfigReference.Config.Value.DefaultSpeedLimit;
                    trafficPathComponent.PathDirection = customDestinationComponent.BackwardDirection ? PathForwardType.Forward : PathForwardType.Backward;

                    if (customDestinationComponent.AchieveDistance == 0)
                    {
                        customDestinationComponent.AchieveDistance = TrafficCustomDestinationConfigReference.Config.Value.DefaultAchieveDistance;
                    }

                    float duration = customDestinationComponent.CustomDuration;

                    if (duration == 0)
                    {
                        duration = TrafficCustomDestinationConfigReference.Config.Value.DefaultDuration;
                    }

                    if (duration != 0)
                    {
                        customDestinationComponent.DisableTimestamp = Time + duration;
                    }
                }

                float dot = math.dot(directionToNode, transform.Forward().Flat());

                switch (customDestinationComponent.VehicleBoundsPoint)
                {
                    case VehicleBoundsPoint.ForwardPoint:
                        carPosition += transform.Forward() * boundsComponent.Extents.z;
                        break;
                    case VehicleBoundsPoint.BackwardPoint:
                        carPosition -= transform.Forward() * boundsComponent.Extents.z;
                        break;
                }

                float distance = math.distance(customDestinationComponent.Destination, carPosition);

                if (TrafficCustomDestinationConfigReference.Config.Value.CheckSidePoint)
                {
                    var isSidePoint = math.abs(dot) < SidePointDotValue;

                    if (isSidePoint && distance < TrafficCustomDestinationConfigReference.Config.Value.SidePointDistance)
                    {
                        speedComponent.LaneLimit = TrafficCustomDestinationConfigReference.Config.Value.SidePointSpeedLimit;
                    }
                }

                bool passed = distance < customDestinationComponent.AchieveDistance;

                if (!passed && customDestinationComponent.DisableTimestamp > 0 && Time >= customDestinationComponent.DisableTimestamp)
                {
                    passed = true;
                }

                if (passed)
                {
                    customDestinationComponent.Passed = true;
                    trafficPathComponent.DestinationWayPoint = customDestinationComponent.PreviousDestination;
                    speedComponent.LaneLimit = customDestinationComponent.PreviousSpeedLimit;

                    trafficPathComponent.PathDirection = customDestinationComponent.BackwardDirection ? PathForwardType.Backward : PathForwardType.Forward;

                    if (!customDestinationComponent.CustomProcess)
                    {
                        EventQueue.RemoveEventTag(entity);
                    }
                }
            }
        }
    }
}