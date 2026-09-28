using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

namespace Spirit604.DotsCity.Simulation.Level.Props
{
    [UpdateInGroup(typeof(LateEventGroup))]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial struct PropsDamageSystem : ISystem
    {
        private const float MaxDiffDistance = 0.1f;
        private const int MaxAngle = 5;

        private EntityQuery m_Query;

        [BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            m_Query = SystemAPI.QueryBuilder()
                .WithDisabled<PropsDamagedTag>()
                .WithDisabledRW<PropsProcessDamageTag>()
                .WithAll<PhysicsWorldIndex, PropsComponent, LocalTransform>()
                .Build();

            m_Query.SetSharedComponentFilter(new PhysicsWorldIndex() { Value = 0 });

            state.RequireForUpdate(m_Query);
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            new PropsDamageJob
            {
            }.ScheduleParallel(m_Query);
        }

        [WithDisabled(typeof(PropsDamagedTag))]
        [WithAll(typeof(PhysicsWorldIndex))]
        [BurstCompile]
        partial struct PropsDamageJob : IJobEntity
        {
            public void Execute(
                ref PropsComponent propsComponent,
                EnabledRefRW<PropsProcessDamageTag> propsProcessDamageTagRW,
                in LocalTransform transform)
            {
                var diff = propsComponent.InitialPosition - transform.Position;

                float distance = math.lengthsq(diff);

                float angle = UnityEngine.Vector3.Angle(transform.Forward(), propsComponent.InitialForward);

                if (distance > MaxDiffDistance || angle > MaxAngle)
                {
                    propsProcessDamageTagRW.ValueRW = true;
                }
            }
        }
    }
}