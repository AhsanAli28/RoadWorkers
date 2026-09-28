using Spirit604.DotsCity.Core;
using Unity.Burst;
using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Level.Props
{
    [UpdateInGroup(typeof(LateEventGroup))]
    [BurstCompile]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial struct PropsCullSystem : ISystem
    {
        private EntityQuery updateQuery;

        [BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            updateQuery = SystemAPI.QueryBuilder()
                .WithNone<PropsCustomResetTag>()
                .WithDisabledRW<PropsResetTag>()
                .WithAll<CulledEventTag, PropsDamagedTag>()
                .Build();

            state.RequireForUpdate(updateQuery);
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var propsCullJob = new PropsCullJob()
            {
            };

            propsCullJob.Schedule(updateQuery);
        }

        [WithNone(typeof(PropsCustomResetTag))]
        [WithAll(typeof(CulledEventTag), typeof(PropsDamagedTag))]
        [BurstCompile]
        public partial struct PropsCullJob : IJobEntity
        {
            void Execute(
                EnabledRefRW<PropsResetTag> propsResetTagRW)
            {
                propsResetTagRW.ValueRW = true;
            }
        }
    }
}