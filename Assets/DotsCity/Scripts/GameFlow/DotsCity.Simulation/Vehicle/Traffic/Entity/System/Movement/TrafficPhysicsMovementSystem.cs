using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Simulation.Car;
using Spirit604.Extensions;
using Unity.Burst;
using Unity.Entities;
using Unity.Physics;
using Unity.Transforms;

namespace Spirit604.DotsCity.Simulation.Traffic
{
    [UpdateAfter(typeof(TrafficProcessMovementDataSystem))]
    [UpdateInGroup(typeof(TrafficFixedUpdateGroup))]
    [BurstCompile]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial struct TrafficPhysicsMovementSystem : ISystem
    {
        private EntityQuery updateQuery;

        [BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            updateQuery = SystemAPI.QueryBuilder()
                .WithNone<TrafficCustomPhysicsTag, TrafficCustomMovementTag>()
                .WithAllRW<PhysicsVelocity, LocalTransform>()
                .WithAll<PhysicsWorldIndex, TrafficTag, HasDriverTag, AliveTag, TrafficMovementComponent, TrafficSettingsComponent>()
                .Build();

            updateQuery.SetSharedComponentFilter(new PhysicsWorldIndex() { Value = 0 });

            state.RequireForUpdate(updateQuery);
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var movementJob = new PhysicsMovementJob() { };

            movementJob.ScheduleParallel(updateQuery);
        }

        [WithNone(typeof(TrafficCustomPhysicsTag), typeof(TrafficCustomMovementTag))]
        [WithAll(typeof(TrafficTag), typeof(HasDriverTag), typeof(AliveTag))]
        [BurstCompile]
        public partial struct PhysicsMovementJob : IJobEntity
        {
            void Execute(
                ref PhysicsVelocity physicsVelocity,
                ref LocalTransform transform,
                in TrafficMovementComponent trafficMovementComponent,
                in TrafficSettingsComponent trafficSettingsComponent)
            {
                physicsVelocity.Linear = trafficMovementComponent.LinearVelocity;

                if (DotsEnumExtension.HasFlagUnsafe(trafficSettingsComponent.AdditionalSettings, TrafficAdditionalSettings.HasRotationLerp))
                {
                    physicsVelocity.Angular = trafficMovementComponent.AngularVelocity;
                }
                else
                {
                    transform.Rotation = trafficMovementComponent.CurrentCalculatedRotation;
                }
            }
        }
    }
}