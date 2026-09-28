using Spirit604.DotsCity.Core;
using System;
using UnityEngine;

namespace Spirit604.Gameplay.Car
{
    public abstract class CarHealthBase : HealthBaseWithDelay, IFactionProvider
    {
        [SerializeField] private FactionType factionType;

        private IHitReaction hitReaction;
        private ICarSlots carSlots;

        public FactionType FactionType
        {
            get
            {
                return factionType;
            }
            set
            {
                factionType = value;
            }
        }

        protected override void Awake()
        {
            base.Awake();
            hitReaction = GetComponent<IHitReaction>();
            carSlots = GetComponent<ICarSlots>();
        }

        protected override void HandleHitReaction(Vector3 hitPosition, Vector3 forceDirection)
        {
            base.HandleHitReaction(hitPosition, forceDirection);
            hitReaction?.HandleHitReaction(hitPosition, forceDirection);
        }

        protected override void ActivateDeathVFX()
        {
            if (carSlots != null)
            {
                Action<GameObject, bool> onNpcExit = (exitingNpcEntity, driver) =>
                {
                    Vector3 forceDirection = (Quaternion.AngleAxis(-70, exitingNpcEntity.transform.right) * exitingNpcEntity.transform.forward).normalized;
                    exitingNpcEntity.GetComponent<IHealth>().TakeDamage(9999, exitingNpcEntity.transform.position, forceDirection);
                };

                carSlots.ExitCarAll(true, onNpcExit);
            }

            hitReaction?.ActivateDeathVFX();
            base.ActivateDeathVFX();
        }
    }
}