using Spirit604.Gameplay.InputService;
using UnityEngine;

namespace Spirit604.Gameplay.Npc
{
    public class EnemyNpcInputBehaviour : MonoBehaviour, IMotionInput, IShootTargetProvider
    {
        public bool HasTarget => default;

        public Vector3 MovementInput => default;

        public Vector3 FireInput => default;

        public Vector3 GetTarget()
        {
            return default;
        }

        public bool GetShootDirection(Vector3 sourcePosition, out Vector3 shotDirection)
        {
            shotDirection = default;
            return default;
        }
    }
}
