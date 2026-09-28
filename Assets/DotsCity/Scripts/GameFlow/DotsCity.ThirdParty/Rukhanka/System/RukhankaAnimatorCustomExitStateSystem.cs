using Rukhanka;
using Spirit604.DotsCity.Simulation.Pedestrian.State;
using Unity.Burst;
using Unity.Entities;

namespace Spirit604.DotsCity.ThirdParty.RukhankaEngine
{
    [UpdateInGroup(typeof(MainThreadInitGroup))]
    [RequireMatchingQueriesForUpdate]
    [BurstCompile]
    public partial struct RukhankaAnimatorCustomExitStateSystem : ISystem
    {
        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var animatorDataProvider = SystemAPI.GetSingleton<AnimatorDataProviderSystem.Singleton>();

            foreach ((
                AnimatorParametersAspect animator,
                RefRW<AnimationStateComponent> animationStateComponent,
                EnabledRefRW<ExitCustomAnimationTag> exitCustomAnimationTag)
                in SystemAPI.Query<
                    AnimatorParametersAspect,
                    RefRW<AnimationStateComponent>,
                    EnabledRefRW<ExitCustomAnimationTag>>())
            {
                var newAnimState = animationStateComponent.ValueRW.PreviousAnimationState;
                animatorDataProvider.ExitAnimation(animator, newAnimState);
                animationStateComponent.ValueRW.PreviousAnimationState = default;

                exitCustomAnimationTag.ValueRW = false;
            }
        }
    }
}