using Spirit604.Gameplay.InputService;
using Spirit604.Gameplay.Npc;
using UnityEngine;

namespace Spirit604.Gameplay.Player
{
    public class PlayerNpcInputBehaviour : MonoBehaviour, IMotionInput, IShootTargetProvider
    {
        private IMotionInput input;
        private IShootTargetProvider targetProvider;
        private Vector3 movementInput;
        private Vector3 fireInput;

        public Vector3 MovementInput => movementInput;
        public Vector3 FireInput => fireInput;

        public bool HasTarget => targetProvider.HasTarget;

        public Vector3 GetTarget()
        {
            return targetProvider.GetTarget();
        }

        private void Update()
        {
            movementInput = input.MovementInput;
            fireInput = input.FireInput;
        }

        public void Initialize(IMotionInput motionInput, IShootTargetProvider targetProvider)
        {
            this.input = motionInput;
            this.targetProvider = targetProvider;
        }

        public bool GetShootDirection(Vector3 sourcePosition, out Vector3 shotDirection)
        {
            return targetProvider.GetShootDirection(sourcePosition, out shotDirection);
        }
    }
}
