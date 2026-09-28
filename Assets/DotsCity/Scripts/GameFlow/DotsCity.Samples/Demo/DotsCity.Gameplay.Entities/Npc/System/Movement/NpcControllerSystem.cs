using Spirit604.DotsCity.Simulation.Npc;
using Spirit604.Extensions;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;

namespace Spirit604.DotsCity.Gameplay.Npc
{
    [UpdateInGroup(typeof(FixedStepGroup))]
    [BurstCompile]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial struct NpcControllerSystem : ISystem
    {
        private const float ANIMATOR_WALKING_VALUE = 0.3F;
        private const float MINUMUM_LERP_VALUE = 0.1f;
        private const float LERP_SPEED = 5f;
        private const float LERP_MULTIPLIER = 4F;
        private const float walkingSpeed = 1.8f;
        private const float runningSpeed = 4f;
        private const float BACKWARD_PENALTY_SPEED = 0.7F;

        private EntityQuery updateQuery;

        [BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            updateQuery = SystemAPI.QueryBuilder()
                .WithAll<NpcTag, InputComponent>()
                .Build();

            state.RequireForUpdate(updateQuery);
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var handleJob = new HandleJob()
            {
                DeltaTime = SystemAPI.Time.DeltaTime,
            };

            handleJob.Schedule();
        }

        [WithAll(typeof(NpcTag))]
        [BurstCompile]
        public partial struct HandleJob : IJobEntity
        {
            [ReadOnly]
            public float DeltaTime;

            void Execute(
                ref LocalTransform transform,
                ref PhysicsVelocity physicsVelocity,
                ref AnimatorMovementComponent animamorMovementComponent,
                in NpcCombatStateComponent npcCombatStateComponent,
                in NpcStateComponent npcStateComponent,
                in InputComponent inputComponent)
            {
                if (!npcStateComponent.IsGrounded)
                {
                    if (!npcStateComponent.IsLanded)
                    {
                        physicsVelocity.Linear = new float3(physicsVelocity.Linear.x, -9.81f, physicsVelocity.Linear.z);
                    }

                    return;
                }

                Vector2 movingInput = inputComponent.MovingInput;
                Vector3 forward = transform.Forward();

                float relativeShotAngle = Vector2.SignedAngle(inputComponent.ShootDirection.To2DSpace(), movingInput);
                float sideInput = relativeShotAngle / 90f;

                float sideMax = 1f;

                int moveDirection = 1;

                var animatorMovementComponentTemp = animamorMovementComponent;

                if (math.abs(sideInput) > 1.1f)
                {
                    moveDirection = -1;

                    relativeShotAngle = Vector2.SignedAngle(Quaternion.Euler(0, 0, 180) * ((Vector3)inputComponent.ShootDirection).ToVector2_2DSpace(), movingInput);
                    sideInput = relativeShotAngle / 90f;
                }

                Vector3 movementDirection = Vector3.zero;

                if (movingInput != Vector2.zero)
                {
                    float moveDirectionEulerAngleY = UnsignedAngle(Mathf.Atan2(movingInput.x, movingInput.y) * Mathf.Rad2Deg) - 90;

                    movementDirection = movingInput.ToVector3_3DSpace();

                    float signedAngle = Vector3.SignedAngle(forward, movingInput.ToVector3_3DSpace(), Vector3.up);

                    Vector3 direction = Quaternion.Euler(0, signedAngle, 0) * forward;

                    quaternion targetRotation = default;

                    if (inputComponent.ShootDirection.Equals(float3.zero))
                    {
                        targetRotation = quaternion.LookRotationSafe(direction, new float3(0, 1, 0));
                    }
                    else
                    {
                        targetRotation = quaternion.LookRotation(inputComponent.ShootDirection, new float3(0, 1, 0));
                    }

                    transform.Rotation = targetRotation;

                    animatorMovementComponentTemp.TargetForwardLerp = movingInput.magnitude * moveDirection;

                    if (moveDirection == 1)
                    {
                        animatorMovementComponentTemp.TargetForwardLerp = math.clamp(animatorMovementComponentTemp.TargetForwardLerp, ANIMATOR_WALKING_VALUE, 1f);

                        if (animatorMovementComponentTemp.CurrentForwardLerp < 0)
                        {
                            animatorMovementComponentTemp.CurrentForwardLerp = animatorMovementComponentTemp.CurrentForwardLerp * -1;
                        }
                    }
                    else
                    {
                        animatorMovementComponentTemp.TargetForwardLerp = math.clamp(animatorMovementComponentTemp.TargetForwardLerp, -1, -ANIMATOR_WALKING_VALUE);

                        if (animatorMovementComponentTemp.CurrentForwardLerp > 0)
                        {
                            animatorMovementComponentTemp.CurrentForwardLerp = animatorMovementComponentTemp.CurrentForwardLerp * -1;
                        }
                    }
                }
                else
                {
                    animatorMovementComponentTemp.TargetForwardLerp = 0;
                    quaternion targetRotation = default;

                    if (!inputComponent.ShootDirection.Equals(float3.zero))
                    {
                        targetRotation = quaternion.LookRotation(inputComponent.ShootDirection, new float3(0, 1, 0));
                    }
                    else
                    {
                        if (forward != Vector3.zero)
                        {
                            targetRotation = quaternion.LookRotation(forward, new float3(0, 1, 0));
                        }
                        else
                        {
                            targetRotation = transform.Rotation;
                        }
                    }

                    transform.Rotation = targetRotation;
                }

                sideInput = math.clamp(sideInput, -sideMax, sideMax);

                if (math.abs(animatorMovementComponentTemp.CurrentForwardLerp) > MINUMUM_LERP_VALUE)
                {
                    float penaltyMultiplier = 1;

                    if (npcCombatStateComponent.IsShooting)
                    {
                        penaltyMultiplier = npcCombatStateComponent.ReducationFactor != 0 ? npcCombatStateComponent.ReducationFactor : 1;
                    }

                    float speedMultiplier = moveDirection == 1 ? 1 : BACKWARD_PENALTY_SPEED;

                    penaltyMultiplier *= 1.2f;
                    penaltyMultiplier = math.clamp(penaltyMultiplier, 0, 1f);
                    speedMultiplier *= penaltyMultiplier;
                    speedMultiplier = math.clamp(speedMultiplier, 0.3f, 1f);

                    float movingSpeed = math.lerp(walkingSpeed, runningSpeed * speedMultiplier, Mathf.Abs(animatorMovementComponentTemp.CurrentForwardLerp));

                    physicsVelocity.Linear = movementDirection * movingSpeed;
                }
                else
                {
                    physicsVelocity.Linear = float3.zero;
                }

                float lerpSpeed = LERP_SPEED;

                if (animatorMovementComponentTemp.TargetForwardLerp == 0)
                {
                    lerpSpeed *= LERP_MULTIPLIER;
                }

                animatorMovementComponentTemp.CurrentForwardLerp = math.lerp(animatorMovementComponentTemp.CurrentForwardLerp, animatorMovementComponentTemp.TargetForwardLerp, lerpSpeed * DeltaTime);
                animatorMovementComponentTemp.CurrentSideLerp = sideInput;

                animamorMovementComponent = animatorMovementComponentTemp;

                physicsVelocity.Angular = default;
            }
        }

        public static float UnsignedAngle(float angle)
        {
            return angle < 0f ? angle + 360f : angle;
        }
    }
}