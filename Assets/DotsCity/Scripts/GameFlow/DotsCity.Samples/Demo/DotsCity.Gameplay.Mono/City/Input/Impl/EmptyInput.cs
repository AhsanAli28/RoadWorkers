using UnityEngine;

namespace Spirit604.Gameplay.InputService
{
    public class EmptyInput : IMotionInput
    {
        public Vector3 MovementInput => default;

        public Vector3 FireInput => default;
    }
}