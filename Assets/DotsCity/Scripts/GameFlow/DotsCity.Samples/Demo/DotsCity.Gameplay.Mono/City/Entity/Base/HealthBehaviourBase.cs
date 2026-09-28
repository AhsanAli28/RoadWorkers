using System;
using UnityEngine;

namespace Spirit604.Gameplay
{
    public abstract class HealthBehaviourBase : MonoBehaviour, IHealth
    {
        public bool IsAlive => CurrentHealth > 0;

        public abstract int CurrentHealth { get; protected set; }

        public abstract int MaxHp { get; }

        protected Vector3 HitPosition { get; set; }
        protected Vector3 ForceDirection { get; set; }

        public Action<HealthBehaviourBase> OnDeath = delegate { };
        public Action<HealthBehaviourBase> OnDeathEffectFinished = delegate { };

        public virtual void TakeDamage(int damage)
        {
            if (!IsAlive)
                return;

            var currentHealth = CurrentHealth;
            currentHealth -= damage;
            currentHealth = Mathf.Clamp(currentHealth, 0, MaxHp);
            CurrentHealth = currentHealth;

            if (currentHealth <= 0)
            {
                PostDeathAction();
            }
            else
            {
                HandleHitReaction(HitPosition, ForceDirection);
            }
        }

        public virtual void TakeDamage(int damage, Vector3 hitPosition, Vector3 forceDirection)
        {
            this.HitPosition = hitPosition;
            this.ForceDirection = forceDirection.normalized;
            TakeDamage(damage);
        }

        public virtual void Initialize(int initialHP)
        {
            CurrentHealth = initialHP;
        }

        protected virtual void HandleHitReaction(Vector3 position, Vector3 forceDirection) { }

        protected virtual void PostDeathAction()
        {
            Death();
            ActivateDeathVFX();
            DeathVfxFinished();
        }

        protected virtual void Death()
        {
            OnDeath(this);
        }

        protected virtual void ActivateDeathVFX()
        {
        }

        protected virtual void DeathVfxFinished()
        {
            OnDeathEffectFinished(this);
        }
    }
}