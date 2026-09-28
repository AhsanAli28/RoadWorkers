using UnityEngine;

namespace Spirit604.Gameplay.Npc
{
    public abstract class ShootTargetProviderBase : IShootTargetProvider
    {
        protected readonly Vector3 TargetOffset = new Vector3(0, 0.1f);

        public abstract bool HasTarget { get; }

        public virtual bool GetShootDirection(Vector3 sourcePosition, out Vector3 shootDirection)
        {
            shootDirection = default;

            if (HasTarget)
            {
                shootDirection = (GetTarget() + TargetOffset - sourcePosition).normalized;
                return true;
            }

            return false;
        }

        public abstract Vector3 GetTarget();
    }
}
