using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Spirit604.DotsCity.Simulation.Level.Props
{
    [UpdateInGroup(typeof(LateEventGroup))]
    [BurstCompile]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial struct PropsResetSystem : ISystem
    {
        private EntityQuery updateQuery;

        [BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            updateQuery = SystemAPI.QueryBuilder()
                .WithAll<PropsResetTag, PropsDamagedTag, PropsComponent>()
                .Build();

            state.RequireForUpdate(updateQuery);
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var propsResetJob = new PropsResetJob()
            {
                CommandBuffer = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged),
                PropsCustomResetLookup = SystemAPI.GetComponentLookup<PropsCustomResetTag>(false)
            };

            propsResetJob.Schedule();
        }

        [WithAll(typeof(PropsResetTag), typeof(PropsDamagedTag))]
        [BurstCompile]
        public partial struct PropsResetJob : IJobEntity
        {
            public EntityCommandBuffer CommandBuffer;
            public ComponentLookup<PropsCustomResetTag> PropsCustomResetLookup;

            void Execute(
                Entity entity,
                ref PropsComponent propsComponent,
                EnabledRefRW<PropsResetTag> propsResetTagRW,
                EnabledRefRW<PropsDamagedTag> propsDamagedTagRW)
            {
                var resetTransform = LocalTransform.FromPositionRotation(propsComponent.InitialPosition, quaternion.LookRotation(propsComponent.InitialForward, math.up()));

                CommandBuffer.SetComponent(entity, resetTransform);
                propsResetTagRW.ValueRW = false;

                if (PropsCustomResetLookup.HasComponent(entity))
                {
                    PropsCustomResetLookup.SetComponentEnabled(entity, true);
                }
                else
                {
                    propsDamagedTagRW.ValueRW = false;
                }
            }
        }
    }
}