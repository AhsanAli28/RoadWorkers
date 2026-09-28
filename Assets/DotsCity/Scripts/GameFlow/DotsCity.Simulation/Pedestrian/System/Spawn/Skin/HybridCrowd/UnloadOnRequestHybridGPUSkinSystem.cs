using Spirit604.AnimationBaker.Entities;
using Spirit604.DotsCity.Simulation.Pedestrian.State;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Pedestrian
{
    [UpdateInGroup(typeof(MainThreadEventGroup))]
    [RequireMatchingQueriesForUpdate]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial class UnloadOnRequestHybridGPUSkinSystem : BeginSimulationSystemBase
    {
        private EntityQuery updateQuery;

        protected override void OnCreate()
        {
            base.OnCreate();
            Enabled = false;
            RequireForUpdate<AnimatorDataProviderSystem.Singleton>();

            updateQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithDisabled<GPUSkinTag, PreventHybridSkinTagTag>()
                .WithAllRW<SkinUpdateComponent, PedestrianCommonSettings>()
                .WithAllRW<AnimationStateComponent>()
                .WithAll<HybridLegacySkinTag, Transform, Animator, SkinAnimatorData, StateComponent>()
                .Build(this);

            RequireForUpdate(updateQuery);
        }

        protected override void OnUpdate()
        {
            var commandBuffer = GetCommandBuffer();

            new HybridGPUUnloadJob()
            {
                CommandBuffer = commandBuffer,
                AnimatorDataProvider = SystemAPI.GetSingleton<AnimatorDataProviderSystem.Singleton>(),
                CrowdSkinProvider = SystemAPI.GetSingleton<CrowdSkinProviderSystem.Singleton>(),
                CustomAnimatorStateTagLookup = SystemAPI.GetComponentLookup<CustomAnimatorStateTag>(true),
                TakenAnimationDataLookup = SystemAPI.GetComponentLookup<TakenAnimationDataComponent>(true),
                Timestamp = (float)SystemAPI.Time.ElapsedTime
            }.Run(updateQuery);

            AddCommandBufferForProducer();
        }
    }
}