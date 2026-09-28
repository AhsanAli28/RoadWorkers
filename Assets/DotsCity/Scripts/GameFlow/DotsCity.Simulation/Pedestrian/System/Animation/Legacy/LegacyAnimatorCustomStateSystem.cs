using Spirit604.DotsCity.Simulation.Pedestrian.State;
using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Pedestrian
{
    [UpdateInGroup(typeof(MainThreadInitGroup))]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial struct LegacyAnimatorCustomStateSystem : ISystem
    {
        private EntityQuery updateQuery;

        void ISystem.OnCreate(ref SystemState state)
        {
            updateQuery = SystemAPI.QueryBuilder()
                .WithAllRW<UpdateCustomAnimationTag, AnimationStateComponent>()
                .WithAll<Animator, HasSkinTag>()
                .Build();

            state.RequireForUpdate(updateQuery);
        }

        void ISystem.OnUpdate(ref SystemState state)
        {
            var animatorDataProvider = SystemAPI.GetSingleton<AnimatorDataProviderSystem.Singleton>();
            var time = (float)SystemAPI.Time.ElapsedTime;

            foreach ((
                SystemAPI.ManagedAPI.UnityEngineComponent<Animator> animator,
                RefRW<AnimationStateComponent> animationStateComponent,
                EnabledRefRW<UpdateCustomAnimationTag> updateCustomAnimationTagRW)
                in SystemAPI.Query<
                    SystemAPI.ManagedAPI.UnityEngineComponent<Animator>,
                    RefRW<AnimationStateComponent>,
                    EnabledRefRW<UpdateCustomAnimationTag>>()
                .WithAll<HasSkinTag>())
            {
                var newAnimState = animationStateComponent.ValueRW.NewAnimationState;
                animationStateComponent.ValueRW.PreviousAnimationState = animationStateComponent.ValueRW.AnimationState;
                animationStateComponent.ValueRW.AnimationState = newAnimState;
                animationStateComponent.ValueRW.StartTime = time;
                animationStateComponent.ValueRW.Reset();
                animatorDataProvider.PlayAnimation(animator.Value, newAnimState);

                updateCustomAnimationTagRW.ValueRW = false;
            }
        }
    }
}