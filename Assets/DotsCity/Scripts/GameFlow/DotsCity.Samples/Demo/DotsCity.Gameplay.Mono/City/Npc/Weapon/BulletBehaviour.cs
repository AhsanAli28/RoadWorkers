using Spirit604.DotsCity.Core;
using UnityEngine;

namespace Spirit604.Gameplay.Weapons
{
    public class BulletBehaviour : BulletBehaviourBase
    {
        private FactionType faction = FactionType.All;

        public override void Initialize(GameObject owner, Vector3 heading, Vector3 spawnPosition, float damageMultiplier = 1f)
        {
            base.Initialize(owner, heading, spawnPosition, damageMultiplier);

            if (owner.TryGetComponent<IFactionProvider>(out var factionProvider))
            {
                faction = factionProvider.FactionType;
            }
        }

        protected override bool ProcessHit(Collider hit, Vector3 point, Vector3 normal)
        {
            if (hit.gameObject.TryGetComponent<IHealth>(out var health))
            {
                bool destroy = true;
                var flag = (int)faction;

                if (hit.gameObject.TryGetComponent<IFactionProvider>(out var factionProvider) && flag != 0)
                {
                    destroy = !factionProvider.FactionType.HasFlag(faction);
                }

                if (destroy)
                {
                    health.TakeDamage(Damage, point, transform.forward);
                    return true;
                }
            }

            return false;
        }
    }
}