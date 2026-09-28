using Spirit604.AnimationBaker.Entities;
using Spirit604.DotsCity.Simulation.Pedestrian.State;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Pedestrian
{
    [UpdateInGroup(typeof(LateEventGroup))]
    [BurstCompile]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial struct GPUAnimatorSystem : ISystem
    {
        private EntityQuery npcQuery;

        void ISystem.OnCreate(ref SystemState state)
        {
            npcQuery = SystemAPI.QueryBuilder()
                .WithDisabled<UpdateSkinTag>()
                .WithAllRW<MovementStateChangedEventTag>()
                .WithAll<SkinUpdateComponent, HasSkinTag>()
                .Build();

            state.RequireForUpdate(npcQuery);
            state.RequireForUpdate<CrowdSkinProviderSystem.Singleton>();
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var updateAnimHashJob = new UpdateAnimHashJob()
            {
                AnimatorDataProvider = SystemAPI.GetSingleton<AnimatorDataProviderSystem.Singleton>(),
                CrowdSkinProvider = SystemAPI.GetSingleton<CrowdSkinProviderSystem.Singleton>(),
                Time = (float)SystemAPI.Time.ElapsedTime
            };

            updateAnimHashJob.ScheduleParallel();
        }

        [WithNone(typeof(CustomAnimatorStateTag))]
        [WithDisabled(typeof(UpdateSkinTag))]
        [WithAll(typeof(HasSkinTag), typeof(GPUSkinTag))]
        [BurstCompile]
        public partial struct UpdateAnimHashJob : IJobEntity
        {
            [ReadOnly]
            public AnimatorDataProviderSystem.Singleton AnimatorDataProvider;

            [ReadOnly]
            public CrowdSkinProviderSystem.Singleton CrowdSkinProvider;

            [ReadOnly]
            public float Time;

            void Execute(
                Entity entity,
                ref SkinUpdateComponent skinUpdateComponent,
                ref AnimationStateComponent animationStateComponent,
                EnabledRefRW<UpdateSkinTag> updateSkinTagRW,
                EnabledRefRW<MovementStateChangedEventTag> movementStateChangedEventTagRW,
                in StateComponent stateComponent,
                in PedestrianCommonSettings pedestrianCommonSettings)
            {
                var animationData = AnimatorDataProvider.GetGPUAnimationData(stateComponent.MovementState);
                var animationHash = animationData.AnimationHash;

                var skinIndex = pedestrianCommonSettings.SkinIndex;

                if (CrowdSkinProvider.UpdateAnimation(ref skinUpdateComponent, ref updateSkinTagRW, skinIndex, animationHash))
                {
                    animationStateComponent.StartTime = Time;
                    animationStateComponent.PreviousAnimationState = animationStateComponent.AnimationState;
                    animationStateComponent.AnimationState = AnimatorDataProvider.GetAnimationState(stateComponent.MovementState);
                }
                else
                {
#if UNITY_EDITOR
                    UnityEngine.Debug.LogError($"PedestrianGPUAnimatorSystem. No animation hash found. Entity {entity.Index} AnimationHash {animationHash} MovementState {(int)stateComponent.MovementState} index.");
#endif
                }

                movementStateChangedEventTagRW.ValueRW = false;
            }
        }
    }
}
