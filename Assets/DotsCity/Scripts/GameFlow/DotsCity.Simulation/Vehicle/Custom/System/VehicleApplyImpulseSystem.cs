using Spirit604.DotsCity.Core;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Extensions;
using Unity.Transforms;

namespace Spirit604.DotsCity.Simulation.Car.Custom
{
    [UpdateAfter(typeof(WheelSimulationSystem))]
    [UpdateInGroup(typeof(PhysicsSimGroup))]
    [BurstCompile]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial struct VehicleApplyImpulseSystem : ISystem
    {
        private EntityQuery updateQuery;

        [BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            updateQuery = SystemAPI.QueryBuilder()
                .WithAllRW<PhysicsVelocity, VehicleOutput>()
                .WithAllRW<SpeedComponent, VelocityComponent>()
                .WithAll<PhysicsWorldIndex, PhysicsMass, VehicleWheel, LocalTransform>()
                .Build();

            updateQuery.SetSharedComponentFilter(new PhysicsWorldIndex() { Value = 0 });

            state.RequireForUpdate(updateQuery);
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var applyImpulseJob = new ApplyImpulseJob()
            {
                WheelOutputLookup = SystemAPI.GetComponentLookup<WheelOutput>(true),
                WheelContactLookup = SystemAPI.GetComponentLookup<WheelContact>(true),
                WheelLookup = SystemAPI.GetComponentLookup<Wheel>(true),
                WheelControllableLookup = SystemAPI.GetComponentLookup<WheelControllable>(true),
            };

            applyImpulseJob.ScheduleParallel(updateQuery);
        }

        [BurstCompile]
        public partial struct ApplyImpulseJob : IJobEntity
        {
            [ReadOnly]
            public ComponentLookup<WheelOutput> WheelOutputLookup;

            [ReadOnly]
            public ComponentLookup<WheelContact> WheelContactLookup;

            [ReadOnly]
            public ComponentLookup<Wheel> WheelLookup;

            [ReadOnly]
            public ComponentLookup<WheelControllable> WheelControllableLookup;

            void Execute(
                ref PhysicsVelocity velocity,
                ref VehicleOutput output,
                ref SpeedComponent speedComponent,
                ref VelocityComponent velocityComponent,
                in DynamicBuffer<VehicleWheel> wheels,
                in PhysicsMass mass,
                in LocalTransform transform)
            {
                output.MaxWheelRotationSpeed = 0.0f;

                for (int i = 0; i < wheels.Length; i++)
                {
                    var wheelEntity = wheels[i].WheelEntity;
                    var wheelOutput = WheelOutputLookup[wheelEntity];
                    var wheelContact = WheelContactLookup[wheelEntity];
                    var wheel = WheelLookup[wheelEntity];
                    var wheelControllable = WheelControllableLookup[wheelEntity];

                    if (wheelContact.IsInContact)
                    {
                        var impulse = wheelOutput.SuspensionImpulse + wheelOutput.FrictionImpulse;
                        var point = wheelContact.Point + new float3(0, wheel.ApplyImpulseOffset, 0);

                        velocity.ApplyImpulse(mass, transform.Position, transform.Rotation, impulse, point);
                    }

                    output.LocalVelocity = math.rotate(math.inverse(transform.Rotation), velocity.Linear);

                    if (wheelControllable.DriveRate > 0)
                    {
                        output.MaxWheelRotationSpeed =
                            math.max(output.MaxWheelRotationSpeed, wheelOutput.RotationSpeed);
                    }

                    velocityComponent.Value = velocity.Linear;
                    speedComponent.Value = math.abs(output.LocalVelocity.z);
                }
            }
        }
    }
}