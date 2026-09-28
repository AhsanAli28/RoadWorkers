using Rukhanka;
using Spirit604.DotsCity.Simulation.Pedestrian.State;
using Unity.Burst;
using Unity.Entities;

namespace Spirit604.DotsCity.ThirdParty.RukhankaEngine
{
    [UpdateInGroup(typeof(MainThreadInitGroup))]
    [RequireMatchingQueriesForUpdate]
    [BurstCompile]
    public partial struct RukhankaAnimatorSystem : ISystem
    {
        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var animatorDataProvider = SystemAPI.GetSingleton<AnimatorDataProviderSystem.Singleton>();
            var time = (float)SystemAPI.Time.ElapsedTime;

            foreach (var (
                paramAspect,
                stateComponent,
                animationStateComponent,
                movementStateChangedEventTagRW)
                in SystemAPI.Query<
                    AnimatorParametersAspect,
                    RefRO<StateComponent>,
                    RefRW<AnimationStateComponent>,
                    EnabledRefRW<MovementStateChangedEventTag>>()
                .WithNone<CustomAnimatorStateTag>())
            {
                var animState = animatorDataProvider.GetAnimationState(stateComponent.ValueRO.MovementState);
                animationStateComponent.ValueRW.AnimationState = animState;
                animationStateComponent.ValueRW.StartTime = time;

                animatorDataProvider.PlayAnimation(paramAspect, animState);
                movementStateChangedEventTagRW.ValueRW = false;
            }
        }
    }
}
