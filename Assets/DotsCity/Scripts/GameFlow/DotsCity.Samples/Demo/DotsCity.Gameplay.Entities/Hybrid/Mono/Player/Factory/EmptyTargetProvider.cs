using Spirit604.Gameplay.Npc;
using UnityEngine;

namespace Spirit604.DotsCity.Gameplay.Player
{
    public class EmptyTargetProvider : IShootTargetProvider
    {
        public bool HasTarget => false;

        public bool GetShootDirection(Vector3 sourcePosition, out Vector3 shotDirection)
        {
            shotDirection = Vector3.zero;
            return false;
        }

        public Vector3 GetTarget()
        {
            return Vector3.zero;
        }
    }
}
