using Rukhanka;
using Spirit604.DotsCity.Simulation.Pedestrian.State;
using Unity.Burst;
using Unity.Entities;

namespace Spirit604.DotsCity.ThirdParty.RukhankaEngine
{
    [UpdateInGroup(typeof(MainThreadInitGroup))]
    [RequireMatchingQueriesForUpdate]
    [BurstCompile]
    public partial struct RukhankaAnimatorCustomStateSystem : ISystem
    {
        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var animatorDataProvider = SystemAPI.GetSingleton<AnimatorDataProviderSystem.Singleton>();
            var time = (float)SystemAPI.Time.ElapsedTime;

            foreach ((
                AnimatorParametersAspect animator,
                RefRW<AnimationStateComponent> animationStateComponent,
                EnabledRefRW<UpdateCustomAnimationTag> updateCustomAnimationTagRW)
                in SystemAPI.Query<
                    AnimatorParametersAspect,
                    RefRW<AnimationStateComponent>,
                    EnabledRefRW<UpdateCustomAnimationTag>>())
            {
                var newAnimState = animationStateComponent.ValueRW.NewAnimationState;
                animationStateComponent.ValueRW.PreviousAnimationState = animationStateComponent.ValueRW.AnimationState;
                animationStateComponent.ValueRW.AnimationState = newAnimState;
                animationStateComponent.ValueRW.StartTime = time;
                animationStateComponent.ValueRW.Reset();

                animatorDataProvider.PlayAnimation(animator, newAnimState);

                updateCustomAnimationTagRW.ValueRW = false;
            }
        }
    }
}