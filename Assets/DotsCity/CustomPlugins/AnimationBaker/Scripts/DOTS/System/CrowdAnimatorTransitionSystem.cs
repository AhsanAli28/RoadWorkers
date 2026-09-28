using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace Spirit604.AnimationBaker.Entities
{
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [BurstCompile]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial struct CrowdAnimatorTransitionSystem : ISystem
    {
        private EntityQuery npcQuery;

        [BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            npcQuery = SystemAPI.QueryBuilder()
                .WithDisabledRW<UpdateSkinTag>()
                .WithAll<GPUSkinTag, HasAnimTransitionTag, SkinAnimatorData, SkinUpdateComponent>()
                .Build();

            state.RequireForUpdate(npcQuery);
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var shaderTime = SystemAPI.GetSingleton<EntityTime>().UnityEngineTime;

            var animTransitionJob = new AnimTransitionJob()
            {
                SkinAnimatorDataLookup = SystemAPI.GetComponentLookup<SkinAnimatorData>(false),
                SkinUpdateLookup = SystemAPI.GetComponentLookup<SkinUpdateComponent>(false),
                TransitionNodeEntityDataLookup = SystemAPI.GetComponentLookup<TransitionNodeEntityData>(true),
                AnimNodeEntityDataLookup = SystemAPI.GetComponentLookup<AnimNodeEntityData>(true),
                AnimConnectedNodeBufferLookup = SystemAPI.GetBufferLookup<AnimConnectedNode>(true),
                CrowdSkinProvider = SystemAPI.GetSingleton<CrowdSkinProviderSystem.Singleton>(),
                AnimationBlobReference = SystemAPI.GetSingleton<AnimationBlobReference>(),
                CurrentTime = (float)SystemAPI.Time.ElapsedTime,
                DeltaTime = SystemAPI.Time.DeltaTime,
                ShaderTime = shaderTime,
            };

            animTransitionJob.Schedule();
        }

        [WithDisabled(typeof(UpdateSkinTag))]
        [WithAll(typeof(GPUSkinTag), typeof(SkinAnimatorData), typeof(SkinUpdateComponent))]
        [BurstCompile]
        public partial struct AnimTransitionJob : IJobEntity
        {
            public ComponentLookup<SkinAnimatorData> SkinAnimatorDataLookup;
            public ComponentLookup<SkinUpdateComponent> SkinUpdateLookup;

            [ReadOnly]
            public ComponentLookup<TransitionNodeEntityData> TransitionNodeEntityDataLookup;

            [ReadOnly]
            public ComponentLookup<AnimNodeEntityData> AnimNodeEntityDataLookup;

            [ReadOnly]
            public BufferLookup<AnimConnectedNode> AnimConnectedNodeBufferLookup;

            [ReadOnly]
            public CrowdSkinProviderSystem.Singleton CrowdSkinProvider;

            [ReadOnly]
            public AnimationBlobReference AnimationBlobReference;

            [ReadOnly]
            public float CurrentTime;

            [ReadOnly]
            public float DeltaTime;

            [ReadOnly]
            public float ShaderTime;

            void Execute(
                Entity entity,
                ref ShaderPlaybackTime shaderPlaybackTime,
                ref ShaderTransitionTime shaderTransitionTime,
                ref ShaderTargetPlaybackTime shaderTargetPlaybackTime,
                ref ShaderTargetFrameOffsetData shaderTargetFrameOffsetData,
                ref ShaderTargetFrameStepInvData shaderTargetFrameStepInvData,
                ref AnimationTransitionData animationTransitionData,
                EnabledRefRW<UpdateSkinTag> updateSkinTagRW,
                EnabledRefRW<HasAnimTransitionTag> hasAnimTransitionTagRW)
            {
                if (animationTransitionData.CurrentAnimationState == Entity.Null)
                {
                    hasAnimTransitionTagRW.ValueRW = false;
                    return;
                }

                var skinAnimatorData = SkinAnimatorDataLookup[entity];
                var skinUpdateComponent = SkinUpdateLookup[entity];

                var currentAnimationStateEntity = animationTransitionData.CurrentAnimationState;
                var currentAnimationNodeData = AnimNodeEntityDataLookup[currentAnimationStateEntity];
                var animationHash = currentAnimationNodeData.AnimHash;

                if (skinAnimatorData.CurrentAnimationHash != animationHash)
                {
                    AnimEntitiesUtils.UpdateAnimation(ref skinUpdateComponent, ref updateSkinTagRW, animationHash, currentAnimationNodeData.UniqueAnimation);
                    shaderPlaybackTime.Value = 0;
                    SkinAnimatorDataLookup[entity] = skinAnimatorData;
                    SkinUpdateLookup[entity] = skinUpdateComponent;
                    return;
                }

                var skinIndex = skinAnimatorData.SkinIndex;

                var animData = CrowdSkinProvider.GetClipData(skinIndex, animationHash);

                bool achievedAnim = false;

                if (!animationTransitionData.IsInitialized)
                {
                    animationTransitionData.IsInitialized = true;

                    TransitionNodeEntityData transitionNodeEntityData = default;

                    SetNextState(ref animationTransitionData, currentAnimationStateEntity);

                    if (animationTransitionData.HasTransitionState)
                    {
                        transitionNodeEntityData = TransitionNodeEntityDataLookup[animationTransitionData.CurrentTransitionState];
                    }

                    shaderPlaybackTime.Value = animationTransitionData.StartTime;
                    animationTransitionData.Speed = 1f;

                    if (animationTransitionData.HasNextState && animationTransitionData.HasTransitionState)
                    {
                        var nextAnimNodeEntityData = AnimNodeEntityDataLookup[animationTransitionData.NextAnimationState];

                        var newAnimationHash = nextAnimNodeEntityData.AnimHash;
                        var targetAnimData = CrowdSkinProvider.GetClipData(skinIndex, newAnimationHash);

                        shaderTargetFrameStepInvData.Value = targetAnimData.FrameStepInv;
                        shaderTargetFrameOffsetData.Value = targetAnimData.FrameOffset;

                        shaderTransitionTime.Value = transitionNodeEntityData.TransitionDuration;
                        shaderTargetPlaybackTime.Value = 0;

                        switch (transitionNodeEntityData.AnimationTransitionType)
                        {
                            case AnimationTransitionType.ToStart:
                                {
                                    break;
                                }
                            case AnimationTransitionType.ToGlobalTimeSync:
                                {
                                    var targetPlayback = (CurrentTime + animData.ClipLength) % targetAnimData.ClipLength;
                                    shaderTargetPlaybackTime.Value = targetPlayback;
                                    break;
                                }
                        }
                    }
                }

                shaderPlaybackTime.Value += DeltaTime * animationTransitionData.Speed;

                if (shaderPlaybackTime.Value >= animData.ClipLength)
                {
                    shaderPlaybackTime.Value = animData.ClipLength;
                    animationTransitionData.IsInitialized = false;
                    achievedAnim = true;
                }

                if (achievedAnim)
                {
                    if (animationTransitionData.HasNextState)
                    {
                        animationTransitionData.CurrentAnimationState = animationTransitionData.NextAnimationState;

                        SetNextState(ref animationTransitionData, animationTransitionData.CurrentAnimationState);
                    }
                    else
                    {
                        animationTransitionData.CurrentAnimationState = Entity.Null;
                    }

                    if (animationTransitionData.CurrentAnimationState != Entity.Null)
                    {
                        var newAnimNodeEntityData = AnimNodeEntityDataLookup[animationTransitionData.CurrentAnimationState];

                        if (!animationTransitionData.HasNextState)
                        {
                            if (!newAnimNodeEntityData.UniqueAnimation)
                            {
                                hasAnimTransitionTagRW.ValueRW = false;
                                animationTransitionData.CurrentAnimationState = Entity.Null;
                            }
                        }

                        AnimEntitiesUtils.UpdateAnimation(ref skinUpdateComponent, ref updateSkinTagRW, newAnimNodeEntityData.AnimHash, newAnimNodeEntityData.UniqueAnimation);
                        animationTransitionData.LastAnimHash = newAnimNodeEntityData.AnimHash;
                        skinUpdateComponent.Timestamp = CurrentTime;
                    }
                    else
                    {
                        hasAnimTransitionTagRW.ValueRW = false;
                    }
                }

                SkinAnimatorDataLookup[entity] = skinAnimatorData;
                SkinUpdateLookup[entity] = skinUpdateComponent;
            }

            private void SetNextState(ref AnimationTransitionData animationTransitionData, Entity currentStateEntity)
            {
                var connectedBuffer = AnimConnectedNodeBufferLookup[currentStateEntity];

                if (connectedBuffer.Length > 0)
                {
                    animationTransitionData.NextAnimationState = connectedBuffer[0].NextState;
                    animationTransitionData.CurrentTransitionState = connectedBuffer[0].TransitionState;
                }
                else
                {
                    animationTransitionData.NextAnimationState = Entity.Null;
                    animationTransitionData.CurrentTransitionState = Entity.Null;
                }
            }
        }
    }
}