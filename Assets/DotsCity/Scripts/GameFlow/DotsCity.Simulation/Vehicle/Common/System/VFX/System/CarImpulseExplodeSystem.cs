using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Simulation.Car;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Extensions;
using Unity.Transforms;

namespace Spirit604.DotsCity.Simulation.Traffic
{
    [UpdateInGroup(typeof(PhysicsSimGroup))]
    [BurstCompile]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial struct CarImpulseExplodeSystem : ISystem
    {
        private EntityQuery updateGroup;

        [BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            updateGroup = SystemAPI.QueryBuilder()
                .WithAllRW<PhysicsVelocity, CarStartExplodeComponent>()
                .WithAll<CarExplodeRequestedTag, PhysicsWorldIndex, VelocityComponent, LocalTransform, PhysicsMass>()
                .Build();

            updateGroup.SetSharedComponentFilter(new PhysicsWorldIndex() { Value = 0 });

            state.RequireForUpdate(updateGroup);
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var explodePhysicsJob = new ExplodePhysicsJob()
            {
                CarExplodeConfigReference = SystemAPI.GetSingleton<CarExplodeConfigReference>(),
                Time = (float)SystemAPI.Time.ElapsedTime,
                DeltaTime = SystemAPI.Time.DeltaTime,
            };

            explodePhysicsJob.Schedule(updateGroup);
        }

        [BurstCompile]
        [WithAll(typeof(CarExplodeRequestedTag), typeof(PhysicsWorldIndex))]
        public partial struct ExplodePhysicsJob : IJobEntity
        {
            [ReadOnly]
            public CarExplodeConfigReference CarExplodeConfigReference;

            [ReadOnly]
            public float Time;

            [ReadOnly]
            public float DeltaTime;

            void Execute(
                ref PhysicsVelocity physicsVelocity,
                ref CarStartExplodeComponent carStartExplodeComponent,
                in VelocityComponent velocityComponent,
                in LocalTransform transform,
                in PhysicsMass mass)
            {
                var impulseRateRelativeSourceMass = 1 / (CarExplodeConfigReference.Config.Value.SourceMass * mass.InverseMass);

                if (carStartExplodeComponent.ExplodeIsEnabled == 0)
                {
                    carStartExplodeComponent.IsPooled = true;
                    carStartExplodeComponent.ExplodeIsEnabled = 1;
                    carStartExplodeComponent.EnableTimeStamp = Time;

                    var forward = transform.Forward();
                    var offset = forward * CarExplodeConfigReference.Config.Value.ApplyForceOffset;

                    var explosionPosition = transform.Position + offset;

                    var impulse = new float3(0, CarExplodeConfigReference.Config.Value.InitialYForce, 0) * impulseRateRelativeSourceMass;

                    var impulse2 = forward * CarExplodeConfigReference.Config.Value.InitialForwardForce + (float3)velocityComponent.Value * CarExplodeConfigReference.Config.Value.VelocityMultiplier;

                    impulse2 *= impulseRateRelativeSourceMass;

                    physicsVelocity.ApplyImpulse(mass, transform.Position, transform.Rotation, impulse, explosionPosition);
                    physicsVelocity.ApplyImpulse(mass, transform.Position, transform.Rotation, impulse2, transform.Position);

                    if (CarExplodeConfigReference.Config.Value.ApplyAngularForce)
                    {
                        var angularImpulse = new float3(CarExplodeConfigReference.Config.Value.InitialAngularForce, 0, 0) * impulseRateRelativeSourceMass;

                        physicsVelocity.ApplyAngularImpulse(mass, angularImpulse);
                    }
                }
                else
                {
                    if (CarExplodeConfigReference.Config.Value.ApplyAngularForce)
                    {
                        float passedTime = Time - carStartExplodeComponent.EnableTimeStamp;

                        if (passedTime < CarExplodeConfigReference.Config.Value.ApplyAngularForceTime)
                        {
                            var angularImpulse = new float3(CarExplodeConfigReference.Config.Value.ConstantAngularForce, 0, 0) * impulseRateRelativeSourceMass;
                            physicsVelocity.ApplyAngularImpulse(mass, angularImpulse);
                        }
                    }
                }
            }
        }
    }
}