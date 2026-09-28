using Spirit604.DotsCity.Simulation.Car;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Physics;

namespace Spirit604.DotsCity.Simulation.Traffic
{
    [UpdateInGroup(typeof(PhysicsSimGroup))]
    [BurstCompile]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial struct CarImpulseExplodeNoPhysicsSystem : ISystem
    {
        private EntityQuery updateGroup;

        [BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            updateGroup = SystemAPI.QueryBuilder()
                .WithNone<PhysicsVelocity>()
                .WithAll<CarExplodeRequestedTag>()
                .Build();

            state.RequireForUpdate(updateGroup);
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var explodeNoPhysicsJob = new ExplodeNoPhysicsJob()
            {
                Time = (float)SystemAPI.Time.ElapsedTime,
            };

            explodeNoPhysicsJob.Schedule();
        }

        [BurstCompile]
        [WithNone(typeof(PhysicsVelocity))]
        [WithAll(typeof(CarExplodeRequestedTag))]
        public partial struct ExplodeNoPhysicsJob : IJobEntity
        {
            [ReadOnly]
            public float Time;

            void Execute(
                ref CarStartExplodeComponent carStartExplodeComponent)
            {
                if (carStartExplodeComponent.ExplodeIsEnabled == 0)
                {
                    carStartExplodeComponent.IsPooled = true;
                    carStartExplodeComponent.ExplodeIsEnabled = 1;
                    carStartExplodeComponent.EnableTimeStamp = Time;
                }
            }
        }
    }
}