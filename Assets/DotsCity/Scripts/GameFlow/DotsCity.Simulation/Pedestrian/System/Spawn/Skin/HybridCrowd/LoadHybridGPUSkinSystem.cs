using Spirit604.AnimationBaker.Entities;
using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Hybrid.Core;
using Spirit604.DotsCity.Simulation.Factory.Pedestrian;
using Spirit604.DotsCity.Simulation.Pedestrian.State;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Pedestrian
{
    [UpdateInGroup(typeof(MainThreadEventGroup))]
    [RequireMatchingQueriesForUpdate]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial class LoadHybridGPUSkinSystem : BeginSimulationSystemBase
    {
        private const float LoadSkinFrequency = 0.4f;

        private PedestrianSkinFactory pedestrianSkinFactory;
        private List<PedestrianEntityRef> entities = new List<PedestrianEntityRef>(100);

        protected override void OnCreate()
        {
            base.OnCreate();

            RequireForUpdate<CrowdSkinProviderSystem.Singleton>();
            RequireForUpdate<AnimatorDataProviderSystem.Singleton>();
            Enabled = false;
        }

        protected override void OnUpdate()
        {
            var commandBuffer = GetCommandBuffer();

            float timestamp = (float)SystemAPI.Time.ElapsedTime;

            entities.Clear();
            var crowdSkinProvider = SystemAPI.GetSingleton<CrowdSkinProviderSystem.Singleton>();
            var animatorDataProvider = SystemAPI.GetSingleton<AnimatorDataProviderSystem.Singleton>();

            Entities
            .WithoutBurst()
            .WithNone<CulledEventTag, PreventHybridSkinTagTag>()
            .WithDisabled<HybridLegacySkinTag>()
            .WithAll<HybridGPUSkinTag, InViewOfCameraTag>()
            .ForEach((
                Entity entity,
                ref DestinationComponent destinationComponent,
                ref PedestrianCommonSettings pedestrianCommonSettings,
                ref HybridGPUSkinData hybridGPULegacySkinData,
                ref AnimationStateComponent animationStateComponent,
                in SkinAnimatorData skinAnimatorData,
                in LocalTransform transform) =>
            {
                bool shouldLoad = (timestamp - pedestrianCommonSettings.LoadSkinTimestamp) >= LoadSkinFrequency;

                if (shouldLoad)
                {
                    if (hybridGPULegacySkinData.InitTime == 0)
                    {
                        var pedestrianSkin = pedestrianSkinFactory.SpawnSkin(pedestrianCommonSettings.SkinIndex).GetComponent<PedestrianEntityRef>();

                        if (pedestrianSkin != null)
                        {
                            bool hasAnimation = false;

                            if (skinAnimatorData.CurrentAnimationHash != 0)
                            {
                                var newAnimState = animatorDataProvider.GetAnimationState(skinAnimatorData.CurrentAnimationHash);

                                if (newAnimState != AnimationState.Default)
                                {
                                    var clipData = crowdSkinProvider.GetClipData(skinAnimatorData.SkinIndex, skinAnimatorData.CurrentAnimationHash);
                                    var clipLength = clipData.ClipLength;

                                    var time = !skinAnimatorData.UniqueAnimation ? timestamp : skinAnimatorData.StartAnimationTime;
                                    var normalizedClipTime = (time % clipLength) / clipLength;

                                    pedestrianSkin.Animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                                    animatorDataProvider.PlayAnimationByGPUHash(pedestrianSkin.Animator, skinAnimatorData.CurrentAnimationHash, time, clipLength);

                                    animationStateComponent.StartTime = time;
                                    animationStateComponent.AnimationState = newAnimState;
                                    hasAnimation = true;
                                }
                            }

                            if (!hasAnimation)
                            {
                                commandBuffer.SetComponentEnabled<MovementStateChangedEventTag>(entity, true);
                            }

                            pedestrianSkin.Transform.SetPositionAndRotation(transform.Position, transform.Rotation);
                            pedestrianSkin.Initialize(entity, EntityManager);
                        }
                        else
                        {
#if UNITY_EDITOR
                            Debug.LogError($"LoadHybridSkinSystem. Pedestrian SkinIndex '{pedestrianCommonSettings.SkinIndex}'. Each pedestrian skin must have a PedestrianEntityRef component.");
#endif
                        }

                        entities.Add(pedestrianSkin);

                        commandBuffer.SetComponentEnabled<CopyTransformToGameObject>(entity, true);
                        commandBuffer.SetSharedComponent(entity, new WorldEntitySharedType(EntityWorldType.HybridEntity));
                        commandBuffer.SetComponentEnabled<GPUSkinTag>(entity, false);
                        commandBuffer.SetComponentEnabled<MaterialMeshInfo>(entity, false);
                        hybridGPULegacySkinData.InitTime = timestamp;
                    }
                    else
                    {
                        if (timestamp - hybridGPULegacySkinData.InitTime > 0.1f)
                        {
                            pedestrianCommonSettings.LoadSkinTimestamp = timestamp;
                            var tr = EntityManager.GetComponentObject<Transform>(entity);
                            tr.GetComponent<Animator>().cullingMode = AnimatorCullingMode.CullCompletely;
                            commandBuffer.SetComponentEnabled<HybridLegacySkinTag>(entity, true);
                            hybridGPULegacySkinData.InitTime = 0;
                        }
                    }
                }

            }).Run();

            for (int i = 0; i < entities.Count; i++)
            {
                var skinData = entities[i];

                if (skinData)
                {
                    EntityManager.AddComponentObject(skinData.RelatedEntity, skinData.Transform);
                    EntityManager.AddComponentObject(skinData.RelatedEntity, skinData.Animator);
                }
            }

            AddCommandBufferForProducer();
        }

        public void Initialize(PedestrianSkinFactory pedestrianSkinFactory)
        {
            this.pedestrianSkinFactory = pedestrianSkinFactory;
            Enabled = true;
        }
    }
}