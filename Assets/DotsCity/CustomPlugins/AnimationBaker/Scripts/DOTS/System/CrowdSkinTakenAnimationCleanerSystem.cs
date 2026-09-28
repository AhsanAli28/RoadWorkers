using Unity.Burst;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;

namespace Spirit604.AnimationBaker.Entities
{
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [BurstCompile]
    public partial struct CrowdSkinTakenAnimationCleanerSystem : ISystem
    {
        private EntityQuery npcQuery;

        void ISystem.OnCreate(ref SystemState state)
        {
            npcQuery = SystemAPI.QueryBuilder()
                .WithNone<GPUSkinTag>()
                .WithAll<TakenAnimationDataComponent>()
                .Build();

            state.RequireForUpdate(npcQuery);
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var cleanIndexJob = new CleanIndexJob()
            {
                CommandBuffer = SystemAPI.GetSingleton<EndInitializationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged),
                CrowdSkinProvider = SystemAPI.GetSingleton<CrowdSkinProviderSystem.Singleton>(),
            };

            cleanIndexJob.Schedule();
        }

        [WithNone(typeof(GPUSkinTag))]
        [WithAll(typeof(TakenAnimationDataComponent))]
        [BurstCompile]
        public partial struct CleanIndexJob : IJobEntity
        {
            public EntityCommandBuffer CommandBuffer;

            [NativeDisableContainerSafetyRestriction]
            public CrowdSkinProviderSystem.Singleton CrowdSkinProvider;

            void Execute(
                Entity entity,
                in TakenAnimationDataComponent takenAnimationDataComponent)
            {
                CrowdSkinProvider.TryToRemoveIndex(takenAnimationDataComponent.TakenMeshIndex);
                CommandBuffer.RemoveComponent<TakenAnimationDataComponent>(entity);
            }
        }
    }
}