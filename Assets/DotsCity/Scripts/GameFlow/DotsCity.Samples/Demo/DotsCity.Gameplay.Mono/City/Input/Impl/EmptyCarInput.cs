using UnityEngine;

namespace Spirit604.Gameplay.InputService
{
    public class EmptyCarInput : ICarMotionInput
    {
        public Vector2 GetMovementInput(Vector3 forward) => default;

        public Vector3 FireInput => default;

        public bool Brake => default;
    }
}