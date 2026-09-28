using UnityEngine;

namespace Spirit604.Gameplay.Car
{
    public interface IHitReaction
    {
        void HandleHitReaction(Vector3 hitPosition, Vector3 forceDirection);
        void ActivateDeathVFX();
    }
}