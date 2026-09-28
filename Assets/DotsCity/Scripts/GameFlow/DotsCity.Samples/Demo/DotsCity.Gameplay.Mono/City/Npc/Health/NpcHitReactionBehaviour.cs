using Spirit604.DotsCity.Hybrid.Core;
using Spirit604.Extensions;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;

namespace Spirit604.Gameplay.Npc
{
    public class NpcHitReactionBehaviour : MonoBehaviour, INpcHitReaction
    {
        private const string HitFrontDirectionKey = "HitFrontDirection";
        private const string HitSideDirectionKey = "HitSideDirection";
        private const string HitReactionTypeKey = "HitReactionType";
        private const string HitKey = "Hit";

        private readonly int HitFrontDirectionHash = Animator.StringToHash(HitFrontDirectionKey);
        private readonly int HitSideDirectionHash = Animator.StringToHash(HitSideDirectionKey);
        private readonly int HitReactionTypeHash = Animator.StringToHash(HitReactionTypeKey);
        private readonly int HitHash = Animator.StringToHash(HitKey);

        private const float IMPULSE_MULTIPLIER = 1.5f;

        [SerializeField][Range(0f, 100f)] private float ragdollHideTime = 3f;

        private bool ragdollActivated;

        private IRagdoll ragdoll;
        private NavMeshAgent navMeshAgent;
        private Animator animator;

        public event Action OnDeathEffectFinished = delegate { };

        protected virtual void Awake()
        {
            ragdoll = GetComponent<IRagdoll>();
            navMeshAgent = GetComponent<NavMeshAgent>();
            animator = GetComponent<Animator>();
        }

        private void OnDisable()
        {
            if (ragdollActivated)
            {
                ragdollActivated = false;
            }
        }

        public void Initialize()
        {
            if (navMeshAgent)
                navMeshAgent.enabled = true;
        }

        public void HandleHitReaction(Vector3 point, Vector3 forceDirection)
        {
            int hitReactionType = 0;
            float hitFrontDirection = 0;
            float hitSideDirection = 0;

            float dot = Vector3.Dot(forceDirection, transform.forward);

            if (dot < -0.45f)
            {
                hitReactionType = UnityEngine.Random.Range(0, 2);

                if (hitReactionType == 1)
                {
                    hitSideDirection = UnityEngine.Random.Range(0, 2);

                    hitSideDirection = hitSideDirection == 0 ? -1 : hitSideDirection;
                }

                hitFrontDirection = 1;
            }
            else if (dot < 0.45f)
            {
                hitSideDirection = dot < 0 ? -1 : 1;
            }
            else
            {
                hitFrontDirection = -1;
            }

            animator.SetFloat(HitFrontDirectionHash, hitFrontDirection);
            animator.SetFloat(HitSideDirectionHash, hitSideDirection);
            animator.SetInteger(HitReactionTypeHash, hitReactionType);
            animator.SetTrigger(HitHash);
        }

        public void ActivateDeathEffect(Vector3 forceDirection)
        {
            if (!ragdollActivated)
            {
                ragdollActivated = true;
                SwitchRagdollState(forceDirection);
                StartCoroutine(HideDelay());
            }
        }

        private IEnumerator HideDelay()
        {
            yield return new WaitForSeconds(ragdollHideTime);
            Hide();
        }

        private void SwitchRagdollState(Vector3 forceDirection)
        {
            if (navMeshAgent)
                navMeshAgent.enabled = false;

            float impulse = forceDirection.y > 0 ? IMPULSE_MULTIPLIER : 1;

            ragdoll.SwitchActiveState(true, forceDirection * impulse);
        }

        private void Hide()
        {
            OnDeathEffectFinished();
            gameObject.ReturnToPool();
        }
    }
}