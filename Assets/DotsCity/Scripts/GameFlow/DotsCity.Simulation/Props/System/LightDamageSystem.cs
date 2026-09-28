using Spirit604.DotsCity.Simulation.Road;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Rendering;

namespace Spirit604.DotsCity.Simulation.Level.Props
{
    [UpdateInGroup(typeof(LateSimulationGroup))]
    [BurstCompile]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial struct LightPropDamageSystem : ISystem
    {
        private EntityQuery updateQuery;

        [BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            updateQuery = SystemAPI.QueryBuilder()
                .WithAllRW<PropsProcessDamageTag>()
                .WithAll<LightPropTag>()
                .Build();

            state.RequireForUpdate(updateQuery);
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var lightPropDamageJob = new LightPropDamageJob()
            {
                LightFrameEntityHolderLookup = SystemAPI.GetBufferLookup<LightFrameEntityHolderElement>(true),
                LightFrameDataLookup = SystemAPI.GetComponentLookup<LightFrameData>(true),
                PropsDamagedLookup = SystemAPI.GetComponentLookup<PropsDamagedTag>(false),
                MaterialMeshInfoLookup = SystemAPI.GetComponentLookup<MaterialMeshInfo>(false),
                LightFrameHandlerStateLookup = SystemAPI.GetComponentLookup<LightFrameHandlerStateComponent>(false),
            };

            lightPropDamageJob.Schedule();
        }

        [WithAll(typeof(LightPropTag))]
        [BurstCompile]
        public partial struct LightPropDamageJob : IJobEntity
        {
            [ReadOnly]
            public BufferLookup<LightFrameEntityHolderElement> LightFrameEntityHolderLookup;

            [ReadOnly]
            public ComponentLookup<LightFrameData> LightFrameDataLookup;

            public ComponentLookup<PropsDamagedTag> PropsDamagedLookup;
            public ComponentLookup<MaterialMeshInfo> MaterialMeshInfoLookup;
            public ComponentLookup<LightFrameHandlerStateComponent> LightFrameHandlerStateLookup;

            void Execute(
                Entity entity,
                EnabledRefRW<PropsProcessDamageTag> propsProcessDamageTagRW)
            {
                propsProcessDamageTagRW.ValueRW = false;
                PropsDamagedLookup.SetComponentEnabled(entity, true);

                var frames = LightFrameEntityHolderLookup[entity];

                for (int i = 0; i < frames.Length; i++)
                {
                    PropsDamagedLookup.SetComponentEnabled(frames[i].FrameEntity, true);

                    LightFrameHandlerStateLookup[frames[i].FrameEntity] = new LightFrameHandlerStateComponent();

                    var frame = LightFrameDataLookup[frames[i].FrameEntity];

                    if (frame.GreenEntity != Entity.Null)
                    {
                        MaterialMeshInfoLookup.SetComponentEnabled(frame.GreenEntity, false);
                    }

                    if (frame.YellowEntity != Entity.Null)
                    {
                        MaterialMeshInfoLookup.SetComponentEnabled(frame.YellowEntity, false);
                    }

                    if (frame.RedEntity != Entity.Null)
                    {
                        MaterialMeshInfoLookup.SetComponentEnabled(frame.RedEntity, false);
                    }
                }
            }
        }
    }
}
