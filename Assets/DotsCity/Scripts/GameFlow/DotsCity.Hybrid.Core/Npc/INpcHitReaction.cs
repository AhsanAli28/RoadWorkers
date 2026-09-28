using System;
using UnityEngine;

namespace Spirit604.DotsCity.Hybrid.Core
{
    public interface INpcHitReaction
    {
        void HandleHitReaction(Vector3 point, Vector3 forceDirection);

        void ActivateDeathEffect(Vector3 forceDirection);

        event Action OnDeathEffectFinished;
    }
}