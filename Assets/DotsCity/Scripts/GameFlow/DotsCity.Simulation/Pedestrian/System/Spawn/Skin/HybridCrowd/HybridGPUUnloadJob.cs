using Spirit604.AnimationBaker.Entities;
using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Hybrid.Core;
using Spirit604.DotsCity.Simulation.Pedestrian.State;
using Spirit604.Extensions;
using Unity.Collections;
using Unity.Entities;
using Unity.Rendering;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Pedestrian
{
    [WithAll(typeof(HybridLegacySkinTag))]
    public partial struct HybridGPUUnloadJob : IJobEntity
    {
        public EntityCommandBuffer CommandBuffer;

        [ReadOnly] public AnimatorDataProviderSystem.Singleton AnimatorDataProvider;
        [ReadOnly] public CrowdSkinProviderSystem.Singleton CrowdSkinProvider;
        [ReadOnly] public ComponentLookup<TakenAnimationDataComponent> TakenAnimationDataLookup;
        [ReadOnly] public ComponentLookup<CustomAnimatorStateTag> CustomAnimatorStateTagLookup;
        [ReadOnly] public float Timestamp;

        void Execute(
            Entity entity,
            Transform transform,
            Animator animator,
            ref SkinUpdateComponent skinUpdateComponent,
            ref PedestrianCommonSettings pedestrianCommonSettings,
            ref AnimationStateComponent animationStateComponent,
            in SkinAnimatorData skinAnimatorData,
            in StateComponent stateComponent)
        {
            if (Timestamp - pedestrianCommonSettings.LoadSkinTimestamp < 0.4f)
                return;

            pedestrianCommonSettings.LoadSkinTimestamp = Timestamp;

            var state = animator.GetCurrentAnimatorStateInfo(0);
            var startTime = state.normalizedTime * state.length;

            if (AnimatorDataProvider.PlayGPUAnimation(ref skinUpdateComponent, animationStateComponent.AnimationState, startTime))
            {
                var animationHash = AnimatorDataProvider.GetGPUAnimationData(animationStateComponent.AnimationState).AnimationHash;
                transform.gameObject.GetComponent<PoolableObjectInfo>().ReturnToPool();

                CommandBuffer.RemoveComponent<Transform>(entity);
                CommandBuffer.RemoveComponent<Animator>(entity);

                CommandBuffer.SetComponentEnabled<CopyTransformToGameObject>(entity, false);
                CommandBuffer.SetComponentEnabled<HybridLegacySkinTag>(entity, false);
                CommandBuffer.SetComponentEnabled<GPUSkinTag>(entity, true);
                CommandBuffer.SetComponentEnabled<MaterialMeshInfo>(entity, true);

                bool updateSkin = true;
                bool customAnimatorState = CustomAnimatorStateTagLookup.HasComponent(entity) && CustomAnimatorStateTagLookup.IsComponentEnabled(entity);

                if (TakenAnimationDataLookup.HasComponent(entity))
                {
                    if (skinAnimatorData.CurrentAnimationHash != animationHash)
                    {
                        var takenMeshIndex = TakenAnimationDataLookup[entity].TakenMeshIndex;
                        CrowdSkinProvider.TryToRemoveIndex(takenMeshIndex);
                        CommandBuffer.RemoveComponent<TakenAnimationDataComponent>(entity);
                    }

                    if (customAnimatorState)
                    {
                        CommandBuffer.SetComponentEnabled<UpdateCustomAnimationTag>(entity, true);
                        animationStateComponent.NewStartPlaybacktime = startTime;
                        animationStateComponent.NewAnimationState = animationStateComponent.AnimationState;
                        updateSkin = false;
                    }
                }
                else
                {
                    if (customAnimatorState)
                    {
                        CommandBuffer.SetComponentEnabled<UpdateCustomAnimationTag>(entity, true);
                        animationStateComponent.NewStartPlaybacktime = startTime;
                        animationStateComponent.NewAnimationState = animationStateComponent.AnimationState;
                        updateSkin = false;
                    }
                }

                if (updateSkin)
                    CommandBuffer.SetComponentEnabled<UpdateSkinTag>(entity, true);

                CommandBuffer.SetSharedComponent(entity, new WorldEntitySharedType(EntityWorldType.PureEntity));
            }
        }
    }
}