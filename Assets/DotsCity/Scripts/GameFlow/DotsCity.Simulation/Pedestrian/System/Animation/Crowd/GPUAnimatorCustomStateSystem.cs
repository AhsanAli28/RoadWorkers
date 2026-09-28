using Spirit604.AnimationBaker.Entities;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Pedestrian.State
{
    [UpdateInGroup(typeof(LateEventGroup))]
    [BurstCompile]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial struct GPUAnimatorCustomStateSystem : ISystem, ISystemStartStop
    {
        private const int StartSitAnimHash = -1880722739; //StartSit hash trigger
        private const int SitoutAnimHash = 218910086; //Sitout hash trigger

        private EntityQuery updateQuery;
        private NativeHashMap<int, Entity> transitionsLocalRef;

        void ISystem.OnCreate(ref SystemState state)
        {
            updateQuery = SystemAPI.QueryBuilder()
                .WithDisabledRW<UpdateSkinTag, HasAnimTransitionTag>()
                .WithAllRW<SkinAnimatorData, AnimationTransitionData>()
                .WithAllRW<UpdateCustomAnimationTag, SkinUpdateComponent>()
                .WithAllRW<AnimationStateComponent>()
                .WithAll<GPUSkinTag>()
                .Build();

            state.RequireForUpdate(updateQuery);
            state.RequireForUpdate<CrowdTransitionProviderSystem.InitTag>();
        }

        void ISystem.OnDestroy(ref SystemState state)
        {
            transitionsLocalRef = default;
        }

        public void OnStartRunning(ref SystemState state)
        {
            transitionsLocalRef = CrowdTransitionProviderSystem.TransitionsStaticRef;
        }

        public void OnStopRunning(ref SystemState state) { }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var sitStateJob = new SitStateJob()
            {
                Transitions = transitionsLocalRef,
                AnimatorDataProvider = SystemAPI.GetSingleton<AnimatorDataProviderSystem.Singleton>(),
                Time = (float)SystemAPI.Time.ElapsedTime
            };

            sitStateJob.Schedule(updateQuery);
        }

        [WithAll(typeof(GPUSkinTag))]
        [BurstCompile]
        public partial struct SitStateJob : IJobEntity
        {
            [ReadOnly]
            public NativeHashMap<int, Entity> Transitions;

            [ReadOnly]
            public AnimatorDataProviderSystem.Singleton AnimatorDataProvider;

            [ReadOnly]
            public float Time;

            void Execute(
                ref SkinAnimatorData skinAnimatorData,
                ref AnimationTransitionData animationTransitionData,
                ref SkinUpdateComponent skinUpdateComponent,
                ref AnimationStateComponent animationStateComponent,
                EnabledRefRW<UpdateSkinTag> updateSkinTagRW,
                EnabledRefRW<UpdateCustomAnimationTag> updateCustomAnimationRW,
                EnabledRefRW<HasAnimTransitionTag> hasAnimTransitionTagRW)
            {
                updateCustomAnimationRW.ValueRW = false;

                var newState = animationStateComponent.NewAnimationState;
                animationStateComponent.AnimationState = newState;
                animationStateComponent.StartTime = Time;

                switch (newState)
                {
                    case AnimationState.StandToSit:
                        {
                            AnimEntitiesUtils.StartAnimationTransition(
                               in Transitions,
                               ref animationTransitionData,
                               ref hasAnimTransitionTagRW,
                               StartSitAnimHash,
                               animationStateComponent.NewStartPlaybacktime);

                            break;
                        }
                    case AnimationState.SittingIdle:
                        {
                            if (skinAnimatorData.CurrentAnimationHash != AnimatorDataProvider.GetGPUAnimationData(AnimationState.SittingIdle).AnimationHash)
                            {
                                AnimatorDataProvider.PlayGPUAnimation(ref skinUpdateComponent, ref updateSkinTagRW, newState);
                            }

                            break;
                        }
                    case AnimationState.SitToStand:
                        {
                            if (skinAnimatorData.CurrentAnimationHash == AnimatorDataProvider.GetGPUAnimationData(AnimationState.SittingIdle).AnimationHash)
                            {
                                AnimEntitiesUtils.StartAnimationTransition(
                                    in Transitions,
                                    ref animationTransitionData,
                                    ref hasAnimTransitionTagRW,
                                    SitoutAnimHash,
                                    animationStateComponent.NewStartPlaybacktime);
                            }

                            break;
                        }
                    default:
                        {
                            AnimatorDataProvider.PlayGPUAnimation(ref skinUpdateComponent, ref updateSkinTagRW, newState);
                            break;
                        }
                }

                animationStateComponent.Reset();
            }
        }
    }
}