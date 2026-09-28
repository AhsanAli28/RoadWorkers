using UnityEngine;

namespace Spirit604.Gameplay.Npc
{
    public interface IShootTargetProvider
    {
        public bool HasTarget { get; }
        public Vector3 GetTarget();
        public bool GetShootDirection(Vector3 sourcePosition, out Vector3 shotDirection);
    }
}
