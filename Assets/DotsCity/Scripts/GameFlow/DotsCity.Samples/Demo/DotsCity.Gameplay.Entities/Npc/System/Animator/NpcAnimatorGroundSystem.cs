using Spirit604.DotsCity.Simulation.Npc;
using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.Gameplay.Npc
{
    [UpdateInGroup(typeof(MainThreadInitGroup))]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial struct NpcAnimatorGroundSystem : ISystem
    {
        private const string Start_Falling_key = "StartFalling";
        private const string Falling_key = "Falling";

        private int start_falling_keyId;
        private int falling_keyId;
        private EntityQuery updateQuery;

        void ISystem.OnCreate(ref SystemState state)
        {
            start_falling_keyId = Animator.StringToHash(Start_Falling_key);
            falling_keyId = Animator.StringToHash(Falling_key);

            updateQuery = SystemAPI.QueryBuilder()
                .WithAll<AnimatorFallingState, NpcTag, Animator>()
                .WithAllRW<AnimatorStateComponent>()
                .Build();

            state.RequireForUpdate(updateQuery);
        }

        void ISystem.OnUpdate(ref SystemState state)
        {
            foreach (var (animator, animatorStateComponent)
                in SystemAPI.Query<SystemAPI.ManagedAPI.UnityEngineComponent<Animator>, RefRW<AnimatorStateComponent>>()
                .WithAll<NpcTag, AnimatorFallingState>())
            {
                if (!animatorStateComponent.ValueRO.IsFalling)
                {
                    animatorStateComponent.ValueRW.IsFalling = true;
                    animator.Value.SetTrigger(start_falling_keyId);

                    if (!animatorStateComponent.ValueRO.ShortFalling)
                    {
                        animator.Value.SetBool(falling_keyId, true);
                    }
                }
                else if (animatorStateComponent.ValueRO.StartedLanding)
                {
                    animator.Value.SetBool(falling_keyId, false);
                }
            }
        }
    }
}