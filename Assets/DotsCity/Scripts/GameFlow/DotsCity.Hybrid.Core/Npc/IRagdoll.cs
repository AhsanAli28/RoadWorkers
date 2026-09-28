using UnityEngine;

namespace Spirit604.DotsCity.Hybrid.Core
{
    public interface IRagdoll
    {
        void SwitchActiveState(bool isActive, Vector3 forceDirection, float forceMultiplier = 1);
    }
}