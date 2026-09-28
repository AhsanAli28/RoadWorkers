using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Simulation.Car;
using Spirit604.DotsCity.Simulation.Car.Custom;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

namespace Spirit604.DotsCity.Simulation.Traffic
{
    [UpdateAfter(typeof(TrafficProcessMovementDataSystem))]
    [UpdateInGroup(typeof(TrafficFixedUpdateGroup))]
    [BurstCompile]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial struct RailSimplePhysicsMovementSystem : ISystem
    {
        private EntityQuery updateQuery;

        [BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            updateQuery = SystemAPI.QueryBuilder()
                .WithNone<TrafficAccurateAligmentCustomMovementTag, TrafficIdleTag>()
                .WithAllRW<PhysicsVelocity, LocalTransform>()
                .WithAll<PhysicsWorldIndex, TrafficRailMovementTag, TrafficCustomMovementTag, AliveTag>()
                .WithAll<TrafficPathComponent, TrafficMovementComponent, TrafficSettingsComponent, TrafficObstacleComponent, SpeedComponent>()
                .Build();

            updateQuery.SetSharedComponentFilter(new PhysicsWorldIndex() { Value = 0 });

            state.RequireForUpdate(updateQuery);
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var simpleMovementJob = new RailPhysicsMovementJob()
            {
                TrafficCustomDestinationLookup = SystemAPI.GetComponentLookup<TrafficCustomDestinationComponent>(),
                VehicleWheelLookup = SystemAPI.GetBufferLookup<VehicleWheel>(),
                WheelContactLookup = SystemAPI.GetComponentLookup<WheelContact>(),
                TrafficRailConfigReference = SystemAPI.GetSingleton<TrafficRailConfigReference>(),
                DeltaTime = SystemAPI.Time.DeltaTime,
            };

            simpleMovementJob.Schedule(updateQuery);
        }

        [WithNone(typeof(TrafficAccurateAligmentCustomMovementTag), typeof(TrafficIdleTag))]
        [WithAll(typeof(TrafficRailMovementTag), typeof(TrafficCustomMovementTag), typeof(AliveTag))]
        [BurstCompile]
        private partial struct RailPhysicsMovementJob : IJobEntity
        {
            [ReadOnly]
            public ComponentLookup<TrafficCustomDestinationComponent> TrafficCustomDestinationLookup;

            [ReadOnly]
            public BufferLookup<VehicleWheel> VehicleWheelLookup;

            [ReadOnly]
            public ComponentLookup<WheelContact> WheelContactLookup;

            [ReadOnly]
            public TrafficRailConfigReference TrafficRailConfigReference;

            [ReadOnly]
            public float DeltaTime;

            void Execute(
                Entity entity,
                ref PhysicsVelocity physicsVelocity,
                ref LocalTransform transform,
                in TrafficPathComponent trafficPathComponent,
                in TrafficMovementComponent trafficMovementComponent,
                in TrafficSettingsComponent trafficSettingsComponent,
                in TrafficObstacleComponent trafficObstacleComponent,
                in SpeedComponent speedComponent)
            {
                float3 movingSpeedVector;
                quaternion newRotation;

                bool customDestination = TrafficCustomDestinationLookup.HasComponent(entity);

                TramRailMovementUtils.CalculateRailMovement(
                    in trafficPathComponent,
                    in trafficMovementComponent,
                    in trafficSettingsComponent,
                    in trafficObstacleComponent,
                    in speedComponent,
                    in transform,
                    in TrafficRailConfigReference,
                    DeltaTime,
                    customDestination,
                    out movingSpeedVector,
                    out newRotation);

                if (VehicleWheelLookup.HasBuffer(entity))
                {
                    bool isInContact = false;

                    var wheels = VehicleWheelLookup[entity];

                    for (int i = 0; i < wheels.Length; i++)
                    {
                        if (WheelContactLookup[wheels[i].WheelEntity].IsInContact)
                        {
                            isInContact = true;
                            break;
                        }
                    }

                    if (isInContact)
                    {
                        movingSpeedVector.y += physicsVelocity.Linear.y;
                    }
                }

                physicsVelocity.Linear = movingSpeedVector;
                transform.Rotation = newRotation;
            }
        }
    }
}