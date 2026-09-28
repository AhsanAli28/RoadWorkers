using Spirit604.DotsCity.Simulation.Pedestrian.State;
using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Pedestrian
{
    [UpdateInGroup(typeof(MainThreadInitGroup))]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial struct LegacyAnimatorCustomExitStateSystem : ISystem
    {
        private EntityQuery updateQuery;

        void ISystem.OnCreate(ref SystemState state)
        {
            updateQuery = SystemAPI.QueryBuilder()
                .WithAllRW<ExitCustomAnimationTag, AnimationStateComponent>()
                .WithAll<Animator, HasSkinTag>()
                .Build();

            state.RequireForUpdate(updateQuery);
        }

        void ISystem.OnUpdate(ref SystemState state)
        {
            var animatorDataProvider = SystemAPI.GetSingleton<AnimatorDataProviderSystem.Singleton>();

            foreach ((
                SystemAPI.ManagedAPI.UnityEngineComponent<Animator> animator,
                RefRW<AnimationStateComponent> animationStateComponent,
                EnabledRefRW<ExitCustomAnimationTag> exitCustomAnimationTag)
                in SystemAPI.Query<
                    SystemAPI.ManagedAPI.UnityEngineComponent<Animator>,
                    RefRW<AnimationStateComponent>,
                    EnabledRefRW<ExitCustomAnimationTag>>()
                .WithAll<HasSkinTag>())
            {
                var newAnimState = animationStateComponent.ValueRW.PreviousAnimationState;
                animatorDataProvider.ExitAnimation(animator.Value, newAnimState);
                animationStateComponent.ValueRW.PreviousAnimationState = default;

                exitCustomAnimationTag.ValueRW = false;
            }
        }
    }
}