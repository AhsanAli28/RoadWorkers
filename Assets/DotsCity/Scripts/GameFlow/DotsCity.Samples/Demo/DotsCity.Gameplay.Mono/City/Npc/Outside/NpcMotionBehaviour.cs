using Spirit604.Gameplay.InputService;
using UnityEngine;

namespace Spirit604.Gameplay.Npc
{
    [RequireComponent(typeof(CharacterController), typeof(Animator))]
    public class NpcMotionBehaviour : NpcMotionBehaviourBase
    {
        private const string ANIMATOR_MOVEMENT_KEY = "yInput";
        private const string ANIMATOR_SIDE_MOVEMENT_KEY = "xInput";
        private const string ANIMATOR_SPEED_MULTIPLIER_KEY = "SpeedMultiplier";

        private readonly int shootAnimatorAimingKeyId = Animator.StringToHash(NpcBehaviourBase.SHOOT_ANIMATOR_AIMING_KEY);
        private readonly int animatorMovementKeyId = Animator.StringToHash(ANIMATOR_MOVEMENT_KEY);
        private readonly int animatorSideMovementKeyId = Animator.StringToHash(ANIMATOR_SIDE_MOVEMENT_KEY);
        private readonly int animatorSpeedMultiplierKeyId = Animator.StringToHash(ANIMATOR_SPEED_MULTIPLIER_KEY);

        private const float ANIMATOR_WALKING_VALUE = 0.3F;
        private const float MINUMUM_LERP_VALUE = 0.1f;
        private const float LERP_SPEED = 5f;
        private const float LERP_MULTIPLIER = 4F;
        private const float walkingSpeed = 1.8f;
        private const float runningSpeed = 4f;
        private const float BACKWARD_PENALTY_SPEED = 0.7F;

        private CharacterController characterController;
        private Animator animator;
        private NpcBehaviourBase npcBehaviour;

        private float targetForwardLerp;
        private float currentForwardLerp;
        private float currentSideLerp;
        private bool isShooting;

        private IMotionInput input;
        private IShootTargetProvider shootTargetProvider;

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            animator = GetComponent<Animator>();
            npcBehaviour = GetComponent<NpcBehaviourBase>();
            input = GetComponent<IMotionInput>();
            shootTargetProvider = GetComponent<IShootTargetProvider>();
        }

        private void FixedUpdate()
        {
            HandleMotion();
        }

        private void Update()
        {
            HandleAnimator();
        }

        private void HandleMotion()
        {
            if (!characterController.enabled)
                return;

            Vector3 velocity = Physics.gravity * Time.fixedDeltaTime;

            if (!characterController.isGrounded)
            {
                characterController.Move(velocity);
                return;
            }

            Vector3 shootDirection;

            var hasTarget = shootTargetProvider.GetShootDirection(transform.position, out shootDirection);

            Vector3 movingInput = input.MovementInput;
            var forward = transform.forward;

            float relativeShotAngle = Vector2.SignedAngle(shootDirection, movingInput);
            float sideInput = relativeShotAngle / 90f;

            float sideMax = 1f;

            int moveDirection = 1;

            if (Mathf.Abs(sideInput) > 1.1f)
            {
                moveDirection = -1;

                relativeShotAngle = Vector2.SignedAngle(Quaternion.Euler(0, 0, 180) * ((Vector3)shootDirection), movingInput);
                sideInput = relativeShotAngle / 90f;
            }

            Vector3 movementDirection = Vector3.zero;

            if (movingInput != default)
            {
                float moveDirectionEulerAngleY = UnsignedAngle(Mathf.Atan2(movingInput.x, movingInput.y) * Mathf.Rad2Deg) - 90;

                movementDirection = movingInput;

                float signedAngle = Vector3.SignedAngle(forward, movingInput, Vector3.up);

                Vector3 direction = Quaternion.Euler(0, signedAngle, 0) * forward;

                Quaternion targetRotation = default;

                if (!hasTarget)
                {
                    targetRotation = Quaternion.LookRotation(direction, new Vector3(0, 1, 0));
                }
                else
                {
                    targetRotation = Quaternion.LookRotation(shootDirection, new Vector3(0, 1, 0));
                }

                transform.rotation = targetRotation;

                targetForwardLerp = movingInput.magnitude * moveDirection;

                if (moveDirection == 1)
                {
                    targetForwardLerp = Mathf.Clamp(targetForwardLerp, ANIMATOR_WALKING_VALUE, 1f);

                    if (currentForwardLerp < 0)
                    {
                        currentForwardLerp = currentForwardLerp * -1;
                    }
                }
                else
                {
                    targetForwardLerp = Mathf.Clamp(targetForwardLerp, -1, -ANIMATOR_WALKING_VALUE);

                    if (currentForwardLerp > 0)
                    {
                        currentForwardLerp = currentForwardLerp * -1;
                    }
                }
            }
            else
            {
                targetForwardLerp = 0;
                Quaternion targetRotation = default;

                if (hasTarget)
                {
                    targetRotation = Quaternion.LookRotation(shootDirection, new Vector3(0, 1, 0));
                }
                else
                {
                    if (forward != Vector3.zero)
                    {
                        targetRotation = Quaternion.LookRotation(forward, new Vector3(0, 1, 0));
                    }
                    else
                    {
                        targetRotation = transform.rotation;
                    }
                }

                transform.rotation = targetRotation;
            }

            sideInput = Mathf.Clamp(sideInput, -sideMax, sideMax);

            if (Mathf.Abs(currentForwardLerp) > MINUMUM_LERP_VALUE)
            {
                float penaltyMultiplier = 1;

                if (isShooting)
                {
                    penaltyMultiplier = Mathf.Max(npcBehaviour.ReducationFactor, 1);
                }

                float speedMultiplier = moveDirection == 1 ? 1 : BACKWARD_PENALTY_SPEED;

                penaltyMultiplier *= 1.2f;
                penaltyMultiplier = Mathf.Clamp(penaltyMultiplier, 0, 1f);
                speedMultiplier *= penaltyMultiplier;
                speedMultiplier = Mathf.Clamp(speedMultiplier, 0.3f, 1f);

                float movingSpeed = Mathf.Lerp(walkingSpeed, runningSpeed * speedMultiplier, Mathf.Abs(currentForwardLerp));

                velocity += movementDirection * movingSpeed * Time.fixedDeltaTime;
            }

            float lerpSpeed = LERP_SPEED;

            if (targetForwardLerp == 0)
            {
                lerpSpeed *= LERP_MULTIPLIER;
            }

            currentForwardLerp = Mathf.Lerp(currentForwardLerp, targetForwardLerp, lerpSpeed * Time.fixedDeltaTime);
            currentSideLerp = sideInput;

            characterController.Move(velocity);
        }

        private void HandleAnimator()
        {
            animator.SetFloat(animatorMovementKeyId, currentForwardLerp);
            animator.SetFloat(animatorSideMovementKeyId, currentSideLerp);

            isShooting = animator.GetBool(shootAnimatorAimingKeyId);

            float speedMultiplier = 1f;

            if (isShooting)
            {
                speedMultiplier = Mathf.Max(npcBehaviour.ReducationFactor, 1);
            }

            animator.SetFloat(animatorSpeedMultiplierKeyId, speedMultiplier);
        }

        public static float UnsignedAngle(float angle)
        {
            return angle < 0f ? angle + 360f : angle;
        }
    }
}