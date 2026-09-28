using Spirit604.DotsCity.Simulation.Road;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Level.Props
{
    [UpdateInGroup(typeof(LateEventGroup))]
    [BurstCompile]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial struct LightPropResetSystem : ISystem
    {
        private EntityQuery updateQuery;

        [BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            updateQuery = SystemAPI.QueryBuilder()
                .WithAll<LightPropTag, PropsCustomResetTag>()
                .Build();

            state.RequireForUpdate(updateQuery);
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var lightPropResetJob = new LightPropResetJob()
            {
                LightFrameEntityHolderLookup = SystemAPI.GetBufferLookup<LightFrameEntityHolderElement>(true),
                PropsDamagedLookup = SystemAPI.GetComponentLookup<PropsDamagedTag>(false),
            };

            lightPropResetJob.Schedule();
        }

        [WithAll(typeof(PropsCustomResetTag), typeof(LightPropTag))]
        [BurstCompile]
        public partial struct LightPropResetJob : IJobEntity
        {
            [ReadOnly]
            public BufferLookup<LightFrameEntityHolderElement> LightFrameEntityHolderLookup;

            public ComponentLookup<PropsDamagedTag> PropsDamagedLookup;

            void Execute(
                Entity entity,
                EnabledRefRW<PropsCustomResetTag> propsCustomResetTagRW)
            {
                propsCustomResetTagRW.ValueRW = false;
                PropsDamagedLookup.SetComponentEnabled(entity, false);

                var frame = LightFrameEntityHolderLookup[entity];

                for (int i = 0; i < frame.Length; i++)
                {
                    PropsDamagedLookup.SetComponentEnabled(frame[i].FrameEntity, false);
                }
            }
        }
    }
}
