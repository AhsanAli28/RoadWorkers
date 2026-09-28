using Spirit604.AnimationBaker.Entities;
using Spirit604.DotsCity.Core;
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
    public partial class UnloadHybridGPUSkinSystem : BeginSimulationSystemBase
    {
        private EntityQuery updateQuery;

        protected override void OnCreate()
        {
            base.OnCreate();

            updateQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAbsent<PreventHybridSkinTagTag>()
                .WithDisabled<InViewOfCameraTag, GPUSkinTag>()
                .WithAny<InPermittedRangeTag, PreInitInCameraTag>()
                .WithAllRW<SkinUpdateComponent, PedestrianCommonSettings>()
                .WithAllRW<AnimationStateComponent>()
                .WithAll<HybridLegacySkinTag, Transform, Animator, SkinAnimatorData, StateComponent>()
                .Build(this);

            RequireForUpdate(updateQuery);
            RequireForUpdate<AnimatorDataProviderSystem.Singleton>();
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