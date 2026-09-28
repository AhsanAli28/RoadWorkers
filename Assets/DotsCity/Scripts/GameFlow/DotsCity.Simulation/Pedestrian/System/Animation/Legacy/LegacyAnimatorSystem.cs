using Spirit604.DotsCity.Simulation.Pedestrian.State;
using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Pedestrian
{
    [UpdateInGroup(typeof(MainThreadInitGroup))]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial struct LegacyAnimatorSystem : ISystem
    {
        private EntityQuery updateQuery;

        void ISystem.OnCreate(ref SystemState state)
        {
            updateQuery = SystemAPI.QueryBuilder()
                .WithNone<CustomAnimatorStateTag>()
                .WithAllRW<MovementStateChangedEventTag>()
                .WithAll<Animator, StateComponent>()
                .Build();

            state.RequireForUpdate(updateQuery);
        }

        void ISystem.OnUpdate(ref SystemState state)
        {
            var animatorDataProvider = SystemAPI.GetSingleton<AnimatorDataProviderSystem.Singleton>();
            var time = (float)SystemAPI.Time.ElapsedTime;

            foreach (var (
                animator,
                stateComponent,
                animationStateComponent,
                movementStateChangedEventTagRW)
                in SystemAPI.Query<
                    SystemAPI.ManagedAPI.UnityEngineComponent<Animator>,
                    RefRO<StateComponent>,
                    RefRW<AnimationStateComponent>,
                    EnabledRefRW<MovementStateChangedEventTag>>()
                .WithNone<CustomAnimatorStateTag>())
            {
                var animState = animatorDataProvider.GetAnimationState(stateComponent.ValueRO.MovementState);
                animationStateComponent.ValueRW.AnimationState = animState;
                animationStateComponent.ValueRW.StartTime = time;

                animatorDataProvider.PlayAnimation(animator.Value, animState);
                movementStateChangedEventTagRW.ValueRW = false;
            }
        }
    }
}
