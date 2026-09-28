using Spirit604.DotsCity.Hybrid.Core;
using Spirit604.Extensions;
using UnityEngine;

namespace Spirit604.Gameplay.Npc
{
    public class NpcHealthBehaviour : HealthBaseWithDelay
    {
        private INpcHitReaction npcHitReaction;

        protected override void Awake()
        {
            npcHitReaction = GetComponent<INpcHitReaction>();
        }

        protected override void ActivateDeathVFX()
        {
            base.ActivateDeathVFX();
            npcHitReaction?.ActivateDeathEffect(ForceDirection);
        }

        protected override void HandleHitReaction(Vector3 point, Vector3 forceDirection)
        {
            npcHitReaction?.HandleHitReaction(point, forceDirection);
        }

        protected override void DeathVfxFinished()
        {
            base.DeathVfxFinished();
            gameObject.ReturnToPool();
        }
    }
}
