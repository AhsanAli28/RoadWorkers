using Spirit604.DotsCity.Simulation.Npc;
using Spirit604.Gameplay.Npc;
using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.Gameplay.Npc
{
    [UpdateInGroup(typeof(MainThreadInitGroup))]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial struct NpcAnimatorMovementSystem : ISystem
    {
        private const string ANIMATOR_MOVEMENT_KEY = "yInput";
        private const string ANIMATOR_SIDE_MOVEMENT_KEY = "xInput";
        private const string ANIMATOR_SPEED_MULTIPLIER_KEY = "SpeedMultiplier";

        private int shootAnimatorAimingKeyId;
        private int animatorMovementKeyId;
        private int animatorSideMovementKeyId;
        private int animatorSpeedMultiplierKeyId;
        private EntityQuery updateQuery;

        void ISystem.OnCreate(ref SystemState state)
        {
            shootAnimatorAimingKeyId = Animator.StringToHash(NpcBehaviourBase.SHOOT_ANIMATOR_AIMING_KEY);
            animatorMovementKeyId = Animator.StringToHash(ANIMATOR_MOVEMENT_KEY);
            animatorSideMovementKeyId = Animator.StringToHash(ANIMATOR_SIDE_MOVEMENT_KEY);
            animatorSpeedMultiplierKeyId = Animator.StringToHash(ANIMATOR_SPEED_MULTIPLIER_KEY);

            updateQuery = SystemAPI.QueryBuilder()
                .WithNone<AnimatorFallingState>()
                .WithAll<NpcTag, NpcCombatStateComponent, AnimatorMovementComponent, Animator>()
                .Build();

            state.RequireForUpdate(updateQuery);
        }

        void ISystem.OnUpdate(ref SystemState state)
        {
            foreach (var (animator, npcCombatStateComponent, animatorMovementComponent)
                in SystemAPI.Query<SystemAPI.ManagedAPI.UnityEngineComponent<Animator>, RefRW<NpcCombatStateComponent>, RefRO<AnimatorMovementComponent>>()
                .WithNone<AnimatorFallingState>()
                .WithAll<NpcTag>())
            {
                animator.Value.SetFloat(animatorMovementKeyId, animatorMovementComponent.ValueRO.CurrentForwardLerp);
                animator.Value.SetFloat(animatorSideMovementKeyId, animatorMovementComponent.ValueRO.CurrentSideLerp);

                npcCombatStateComponent.ValueRW.IsShooting = animator.Value.GetBool(shootAnimatorAimingKeyId);

                float speedMultiplier = 1f;

                if (npcCombatStateComponent.ValueRO.IsShooting)
                {
                    speedMultiplier = npcCombatStateComponent.ValueRO.ReducationFactor != 0 ? npcCombatStateComponent.ValueRO.ReducationFactor : 1;
                }

                animator.Value.SetFloat(animatorSpeedMultiplierKeyId, speedMultiplier);
            }
        }
    }
}